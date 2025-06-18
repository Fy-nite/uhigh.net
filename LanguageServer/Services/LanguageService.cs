using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;
using uhigh.Net.Parser;
using uhigh.Net.LanguageServer.Protocol;
using uhigh.Net.LanguageServer.Core;
using LSPDiagnostic = uhigh.Net.LanguageServer.Protocol.Diagnostic;

namespace uhigh.Net.LanguageServer.Services
{
    public class LanguageService
    {
        private readonly DocumentManager _documentManager;
        
        public LanguageService(DocumentManager documentManager)
        {
            _documentManager = documentManager;
        }
        
        public LSPDiagnostic[] GetDiagnostics(string uri)
        {
            var document = _documentManager.GetDocument(uri);
            if (document == null)
                return Array.Empty<LSPDiagnostic>();
            
            var diagnostics = new List<LSPDiagnostic>();
            var diagnosticsReporter = new DiagnosticsReporter(verboseMode: false, sourceFileName: null, suppressOutput: true);
            
            try
            {
                // Lexical analysis
                var lexer = new Lexer.Lexer(document.Text, diagnosticsReporter);
                var tokens = lexer.Tokenize();
                
                // Parse the tokens
                var parser = new Parser.Parser(tokens, diagnosticsReporter);
                var program = parser.Parse();
                
                // Convert diagnostics to LSP format
                foreach (var diag in diagnosticsReporter.Diagnostics)
                {
                    var line = diag.Location?.Line ?? 1;
                    var column = diag.Location?.Column ?? 1;
                    
                    diagnostics.Add(new LSPDiagnostic
                    {
                        Range = new Protocol.Range
                        {
                            Start = new Position { Line = line - 1, Character = column - 1 },
                            End = new Position { Line = line - 1, Character = column + diag.Message.Length - 1 }
                        },
                        Severity = diag.Severity switch
                        {
                            Diagnostics.DiagnosticSeverity.Error => Protocol.DiagnosticSeverity.Error,
                            Diagnostics.DiagnosticSeverity.Warning => Protocol.DiagnosticSeverity.Warning,
                            Diagnostics.DiagnosticSeverity.Info => Protocol.DiagnosticSeverity.Information,
                            _ => Protocol.DiagnosticSeverity.Error
                        },
                        Message = diag.Message,
                        Source = "uhigh-language-server"
                    });
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new LSPDiagnostic
                {
                    Range = new Protocol.Range
                    {
                        Start = new Position { Line = 0, Character = 0 },
                        End = new Position { Line = 0, Character = 0 }
                    },
                    Severity = Protocol.DiagnosticSeverity.Error,
                    Message = $"Internal error: {ex.Message}",
                    Source = "uhigh-language-server"
                });
            }
            
            return diagnostics.ToArray();
        }
        
        public CompletionList GetCompletions(string uri, Position position)
        {
            var document = _documentManager.GetDocument(uri);
            if (document == null)
                return new CompletionList();
            
            var completions = new List<CompletionItem>();
            
            // Get the word at cursor position
            var currentWord = document.GetWordAtPosition(position);
            
            // Add keyword completions
            var keywords = new[]
            {
                "var", "const", "func", "if", "else", "while", "for", "return",
                "class", "namespace", "import", "from", "this", "true", "false",
                "public", "private", "protected", "static", "readonly", "async",
                "int", "float", "string", "bool", "void"
            };
            
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrEmpty(currentWord) || keyword.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                {
                    completions.Add(new CompletionItem
                    {
                        Label = keyword,
                        Kind = CompletionItemKind.Keyword,
                        Detail = $"Keyword: {keyword}",
                        InsertText = keyword
                    });
                }
            }
            
            return new CompletionList
            {
                IsIncomplete = false,
                Items = completions.ToArray()
            };
        }
        
        public Hover? GetHover(string uri, Position position)
        {
            var document = _documentManager.GetDocument(uri);
            if (document == null)
                return null;
            
            var word = document.GetWordAtPosition(position);
            if (string.IsNullOrEmpty(word))
                return null;
            
            var wordRange = document.GetWordRangeAtPosition(position);
            
            var hoverContent = GetBasicHoverContent(word);
            if (hoverContent != null)
            {
                return new Hover
                {
                    Contents = new MarkupContent
                    {
                        Kind = "markdown",
                        Value = hoverContent
                    },
                    Range = wordRange
                };
            }
            
            return null;
        }
        
        private string? GetBasicHoverContent(string word)
        {
            return word switch
            {
                "var" => "**var** - Declares a mutable variable",
                "const" => "**const** - Declares an immutable constant",
                "func" => "**func** - Declares a function",
                "class" => "**class** - Declares a class",
                "if" => "**if** - Conditional statement",
                "else" => "**else** - Else clause for conditional statement",
                "while" => "**while** - While loop",
                "for" => "**for** - For loop",
                "return" => "**return** - Returns a value from a function",
                "int" => "**int** - 32-bit signed integer type",
                "float" => "**float** - Floating-point number type",
                "string" => "**string** - String type",
                "bool" => "**bool** - Boolean type (true/false)",
                "void" => "**void** - Void type (no return value)",
                "true" => "**true** - Boolean literal: true",
                "false" => "**false** - Boolean literal: false",
                "this" => "**this** - Reference to the current instance",
                "public" => "**public** - Public access modifier",
                "private" => "**private** - Private access modifier",
                "static" => "**static** - Static modifier",
                _ => null
            };
        }
        
        public DocumentSymbol[] GetDocumentSymbols(string uri)
        {
            var document = _documentManager.GetDocument(uri);
            if (document == null)
                return Array.Empty<DocumentSymbol>();
            
            try
            {
                var diagnosticsReporter = new DiagnosticsReporter(verboseMode: false, sourceFileName: null, suppressOutput: true);
                var lexer = new Lexer.Lexer(document.Text, diagnosticsReporter);
                var tokens = lexer.Tokenize();
                var parser = new Parser.Parser(tokens, diagnosticsReporter);
                var program = parser.Parse();
                
                var symbols = new List<DocumentSymbol>();
                
                foreach (var statement in program.Statements)
                {
                    var symbol = ConvertToDocumentSymbol(statement);
                    if (symbol != null)
                        symbols.Add(symbol);
                }
                
                return symbols.ToArray();
            }
            catch
            {
                return Array.Empty<DocumentSymbol>();
            }
        }
        
        private DocumentSymbol? ConvertToDocumentSymbol(Statement statement)
        {
            return statement switch
            {
                FunctionDeclaration func => new DocumentSymbol
                {
                    Name = func.Name,
                    Kind = SymbolKind.Function,
                    Detail = $"func {func.Name}",
                    Range = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } },
                    SelectionRange = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } }
                },
                ClassDeclaration cls => new DocumentSymbol
                {
                    Name = cls.Name,
                    Kind = SymbolKind.Class,
                    Detail = $"class {cls.Name}",
                    Range = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } },
                    SelectionRange = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } }
                },
                VariableDeclaration var => new DocumentSymbol
                {
                    Name = var.Name,
                    Kind = SymbolKind.Variable,
                    Detail = $"var {var.Name}",
                    Range = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } },
                    SelectionRange = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } }
                },
                NamespaceDeclaration ns => new DocumentSymbol
                {
                    Name = ns.Name,
                    Kind = SymbolKind.Namespace,
                    Detail = $"namespace {ns.Name}",
                    Range = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } },
                    SelectionRange = new Protocol.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } }
                },
                _ => null
            };
        }
    }
}
