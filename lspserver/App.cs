using LanguageServer;
using LanguageServer.Client;
using LanguageServer.Parameters;
using LanguageServer.Parameters.General;
using LanguageServer.Parameters.TextDocument;
using LanguageServer.Parameters.Workspace;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using uhigh.Net.Lexer;

namespace UhighLanguageServer
{
    public class App : ServiceConnection
    {
        private Uri _workerSpaceRoot;
        private int _maxNumberOfProblems = 1000;
        private TextDocumentManager _documents;

        public App(Stream input, Stream output)
            : base(input, output)
        {
            _documents = new TextDocumentManager();
            _documents.Changed += Documents_Changed;
        }

        private void Documents_Changed(object sender, TextDocumentChangedEventArgs e)
        {
            ValidateTextDocument(e.Document);
        }

        protected override Result<InitializeResult, ResponseError<InitializeErrorData>> Initialize(InitializeParams @params)
        {
            _workerSpaceRoot = @params.rootUri;
            var result = new InitializeResult
            {
                capabilities = new ServerCapabilities
                {
                    textDocumentSync = TextDocumentSyncKind.Full,
                    completionProvider = new CompletionOptions
                    {
                        resolveProvider = true
                    }
                }
            };
            return Result<InitializeResult, ResponseError<InitializeErrorData>>.Success(result);
        }

        protected override void DidOpenTextDocument(DidOpenTextDocumentParams @params)
        {
            _documents.Add(@params.textDocument);
            Logger.Instance.Log($"{@params.textDocument.uri} opened.");
        }

        protected override void DidChangeTextDocument(DidChangeTextDocumentParams @params)
        {
            _documents.Change(@params.textDocument.uri, @params.textDocument.version, @params.contentChanges);
            Logger.Instance.Log($"{@params.textDocument.uri} changed.");
        }

        protected override void DidCloseTextDocument(DidCloseTextDocumentParams @params)
        {
            _documents.Remove(@params.textDocument.uri);
            Logger.Instance.Log($"{@params.textDocument.uri} closed.");
        }

        protected override void DidChangeConfiguration(DidChangeConfigurationParams @params)
        {
            _maxNumberOfProblems = @params?.settings?.languageServerExample?.maxNumberOfProblems ?? _maxNumberOfProblems;
            Logger.Instance.Log($"maxNumberOfProblems is set to {_maxNumberOfProblems}.");
            foreach (var document in _documents.All)
            {
                ValidateTextDocument(document);
            }
        }

        private void ValidateTextDocument(TextDocumentItem document)
        {
            var diagnostics = new List<Diagnostic>();
            var lines = document.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var lexer = new Lexer(document.text);
            var tokens = lexer.Tokenize();
            var parser = new uhigh.Net.Parser.Parser(tokens);
            var program = parser.Parse();
            if (program == null)
            {
                diagnostics.Add(new Diagnostic
                {
                    severity = DiagnosticSeverity.Error,
                    range = new LanguageServer.Parameters.Range
                    {
                        start = new Position { line = 0, character = 0 },
                        end = new Position { line = 0, character = 1 }
                    },
                    message = "Syntax error in the document",
                    source = "parser"
                });
                Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
                {
                    uri = document.uri,
                    diagnostics = diagnostics.ToArray()
                });
                return;
            }
            var problems = 0;
            // Simple block scoping: track nesting and warn if too deep or unbalanced
            int blockDepth = 0;
            int maxBlockDepth = 0;
            Stack<int> blockStartLines = new();

            foreach (var token in tokens)
            {
                // Track block depth for '{' and '}'
                if (token.Type == uhigh.Net.Lexer.TokenType.LeftBrace)
                {
                    blockDepth++;
                    blockStartLines.Push(token.Line);
                    if (blockDepth > maxBlockDepth)
                        maxBlockDepth = blockDepth;
                }
                else if (token.Type == uhigh.Net.Lexer.TokenType.RightBrace)
                {
                    if (blockDepth > 0)
                    {
                        blockDepth--;
                        blockStartLines.Pop();
                    }
                    else
                    {
                        // Unmatched closing brace
                        diagnostics.Add(new Diagnostic
                        {
                            severity = DiagnosticSeverity.Error,
                            range = new LanguageServer.Parameters.Range
                            {
                                start = new Position { line = token.Line, character = token.Column },
                                end = new Position { line = token.Line, character = token.Column + 1 }
                            },
                            message = "Unmatched closing brace '}'",
                            source = "scoping"
                        });
                        problems++;
                    }
                }

                // Example: warn if string literal is too long
                if (token.Type == uhigh.Net.Lexer.TokenType.String)
                {
                    if (token.Value.Length > 10)
                    {
                        diagnostics.Add(new Diagnostic
                        {
                            severity = DiagnosticSeverity.Warning,
                            range = new LanguageServer.Parameters.Range
                            {
                                start = new Position { line = token.Line, character = token.Column },
                                end = new Position { line = token.Line, character = token.Column + token.Value.Length }
                            },
                            message = "test: String length exceeds 10 characters",
                            source = "lexer"
                        });
                        problems++;
                    }
                }
            }

            // Warn if block depth is too deep (arbitrary threshold, e.g., 5)
            if (maxBlockDepth > 5)
            {
                diagnostics.Add(new Diagnostic
                {
                    severity = DiagnosticSeverity.Warning,
                    range = new LanguageServer.Parameters.Range
                    {
                        start = new Position { line = 0, character = 0 },
                        end = new Position { line = 0, character = 1 }
                    },
                    message = $"Block nesting is too deep ({maxBlockDepth} levels)",
                    source = "scoping"
                });
            }

            // Warn if there are unclosed blocks
            if (blockDepth > 0 && blockStartLines.Count > 0)
            {
                foreach (var line in blockStartLines)
                {
                    diagnostics.Add(new Diagnostic
                    {
                        severity = DiagnosticSeverity.Error,
                        range = new LanguageServer.Parameters.Range
                        {
                            start = new Position { line = line, character = 0 },
                            end = new Position { line = line, character = 1 }
                        },
                        message = "Unclosed block: missing '}'",
                        source = "scoping"
                    });
                }
            }

            Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                uri = document.uri,
                diagnostics = diagnostics.ToArray()
            });
        }

        protected override void DidChangeWatchedFiles(DidChangeWatchedFilesParams @params)
        {
            Logger.Instance.Log("We received an file change event");
        }

        protected override Result<CompletionResult, ResponseError> Completion(CompletionParams @params)
        {
            var array = new[]
            {
                new CompletionItem
                {
                    label = "TypeScript",
                    kind = CompletionItemKind.Text,
                    data = 1
                },
                new CompletionItem
                {
                    label = "JavaScript",
                    kind = CompletionItemKind.Text,
                    data = 2
                }
            };
            return Result<CompletionResult, ResponseError>.Success(array);
        }

        protected override Result<CompletionItem, ResponseError> ResolveCompletionItem(CompletionItem @params)
        {
            if (@params.data == 1)
            {
                @params.detail = "TypeScript details";
                @params.documentation = "TypeScript documentation";
            }
            else if (@params.data == 2)
            {
                @params.detail = "JavaScript details";
                @params.documentation = "JavaScript documentation";
            }
            return Result<CompletionItem, ResponseError>.Success(@params);
        }

        protected override VoidResult<ResponseError> Shutdown()
        {
            Logger.Instance.Log("Language Server is about to shutdown.");
            // WORKAROUND: Language Server does not receive an exit notification.
            Task.Delay(1000).ContinueWith(_ => Environment.Exit(0));
            return VoidResult<ResponseError>.Success();
        }
    }
}
