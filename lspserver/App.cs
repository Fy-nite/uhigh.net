using LanguageServer;
using LanguageServer.Client;
using LanguageServer.Parameters;
using LanguageServer.Parameters.General;
using LanguageServer.Parameters.TextDocument;
using LanguageServer.Parameters.Workspace;
using uhigh.Net.Lexer;

namespace UhighLanguageServer
{
    public class App : ServiceConnection
    {
        private Uri _workerSpaceRoot;
        private int _maxNumberOfProblems = 1000;
        private TextDocumentManager _documents;
        private uhigh.Net.Parser.ReflectionTypeResolver _typeResolver;
        private uhigh.Net.Parser.ReflectionMethodResolver _methodResolver;

        public App(Stream input, Stream output)
            : base(input, output)
        {
            _documents = new TextDocumentManager();
            _documents.Changed += Documents_Changed;
            // Initialize reflection resolvers
            var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter();
            _typeResolver = new uhigh.Net.Parser.ReflectionTypeResolver(diagnostics);
            _methodResolver = new uhigh.Net.Parser.ReflectionMethodResolver(diagnostics);
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
                try
                {
                    if (Proxy != null)
                    {
                        Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
                        {
                            uri = document.uri,
                            diagnostics = diagnostics.ToArray()
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"Failed to publish diagnostics: {ex}");
                }
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
                                start = new Position { line = Math.Max(0, token.Line), character = Math.Max(0, token.Column) },
                                end = new Position { line = Math.Max(0, token.Line), character = Math.Max(0, token.Column + 1) }
                            },
                            message = "Unmatched closing brace '}'",
                            source = "scoping"
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
                            start = new Position { line = Math.Max(0, line), character = 0 },
                            end = new Position { line = Math.Max(0, line), character = 1 }
                        },
                        message = "Unclosed block: missing '}'",
                        source = "scoping"
                    });
                }
            }

            // Clamp all diagnostics to valid line/character ranges
            int maxLine = Math.Max(0, lines.Length - 1);
            foreach (var diag in diagnostics)
            {
                diag.range.start.line = Math.Max(0, Math.Min(diag.range.start.line, maxLine));
                diag.range.end.line = Math.Max(0, Math.Min(diag.range.end.line, maxLine));
                diag.range.start.character = Math.Max(0, diag.range.start.character);
                diag.range.end.character = Math.Max(0, diag.range.end.character);
            }

            try
            {
                if (Proxy != null)
                {
                    if (diagnostics.Count == 0)
                    {
                        diagnostics.Add(new Diagnostic
                        {
                            severity = DiagnosticSeverity.Information,
                            range = new LanguageServer.Parameters.Range
                            {
                                start = new Position { line = 0, character = 0 },
                                end = new Position { line = 0, character = 1 }
                            },
                            message = "No issues found",
                            source = "parser"
                        });
                    }
                    Logger.Instance.Log($"Publishing {diagnostics.Count} diagnostics for {document.uri}");
                    // Publish diagnostics to the client
                    Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
                    {
                        uri = document.uri,
                        diagnostics = diagnostics.ToArray()
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to publish diagnostics: {ex}");
            }
        }

        protected override void DidChangeWatchedFiles(DidChangeWatchedFilesParams @params)
        {
            Logger.Instance.Log("We received an file change event");
        }

        protected override Result<CompletionResult, ResponseError> Completion(CompletionParams @params)
        {
            // μHigh keywords (static)
            var keywordItems = new[]
            {
                new CompletionItem
                {
                    label = "func",
                    kind = CompletionItemKind.Keyword,
                    detail = "Function declaration",
                    documentation = "Defines a function",
                    insertText = "func ",
                    data = 1001
                },
                new CompletionItem
                {
                    label = "var",
                    kind = CompletionItemKind.Keyword,
                    detail = "Variable declaration",
                    documentation = "Declares a variable",
                    insertText = "var ",
                    data = 1002
                },
                new CompletionItem
                {
                    label = "class",
                    kind = CompletionItemKind.Keyword,
                    detail = "Class declaration",
                    documentation = "Declares a class",
                    insertText = "class ",
                    data = 1003
                },
                new CompletionItem
                {
                    label = "if",
                    kind = CompletionItemKind.Keyword,
                    detail = "If statement",
                    documentation = "Conditional statement",
                    insertText = "if ",
                    data = 1004
                },
                new CompletionItem
                {
                    label = "while",
                    kind = CompletionItemKind.Keyword,
                    detail = "While loop",
                    documentation = "Loop while condition is true",
                    insertText = "while ",
                    data = 1005
                },
                new CompletionItem
                {
                    label = "for",
                    kind = CompletionItemKind.Keyword,
                    detail = "For loop",
                    documentation = "For or for-in loop",
                    insertText = "for ",
                    data = 1006
                },
                new CompletionItem
                {
                    label = "return",
                    kind = CompletionItemKind.Keyword,
                    detail = "Return statement",
                    documentation = "Returns a value from a function",
                    insertText = "return ",
                    data = 1007
                }
            };

            // Get types from reflection
            var typeItems = _typeResolver.GetAllTypeNames()
                .Distinct()
                .OrderBy(t => t)
                .Select((typeName, idx) => new CompletionItem
                {
                    label = typeName,
                    kind = CompletionItemKind.Class,
                    detail = "Type",
                    documentation = $"Type: {typeName}",
                    insertText = typeName,
                    data = 2000 + idx
                });

            // Get methods from reflection
            var methodItems = _typeResolver.GetAllMethodNames()
                .Distinct()
                .OrderBy(m => m)
                .Select((methodName, idx) => new CompletionItem
                {
                    label = methodName,
                    kind = CompletionItemKind.Method,
                    detail = "Method",
                    documentation = $"Method: {methodName}",
                    insertText = methodName + "(",
                    data = 3000 + idx
                });

            // Combine all completion items
            var allItems = keywordItems
                .Concat(typeItems)
                .Concat(methodItems)
                .ToArray();

            return Result<CompletionResult, ResponseError>.Success(allItems);
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
            // Always return a valid result object (not null)
            return VoidResult<ResponseError>.Success();
        }
    }
}
