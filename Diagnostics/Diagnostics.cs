using uhigh.Net.Lexer;

namespace uhigh.Net.Diagnostics
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error,
        Fatal
    }

    public class SourceLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string? FileName { get; set; }
        
        public SourceLocation(int line, int column, string? fileName = null)
        {
            Line = line;
            Column = column;
            FileName = fileName;
        }

        public override string ToString()
        {
            return FileName != null ? $"{FileName}:{Line}:{Column}" : $"{Line}:{Column}";
        }
    }

    public class Diagnostic
    {
        public DiagnosticSeverity Severity { get; set; }
        public string Message { get; set; }
        public SourceLocation? Location { get; set; }
        public string? Code { get; set; }
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Suggestion { get; set; }
        public string? SourceLine { get; set; }

        public Diagnostic(DiagnosticSeverity severity, string message, SourceLocation? location = null, string? code = null, Exception? exception = null)
        {
            Severity = severity;
            Message = message;
            Location = location;
            Code = code;
            Exception = exception;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            var severity = Severity.ToString().ToLower();
            var location = Location?.ToString() ?? "unknown";
            var code = Code != null ? $"[{Code}]" : "";
            return $"{severity}{code}: {Message}";
        }
    }

    public class DiagnosticsReporter
    {
        private readonly List<Diagnostic> _diagnostics = new();
        private readonly bool _verboseMode;
        private readonly string? _sourceFileName;
        private readonly Dictionary<int, string> _sourceLines = new();

        public DiagnosticsReporter(bool verboseMode = false, string? sourceFileName = null)
        {
            _verboseMode = verboseMode;
            _sourceFileName = sourceFileName;
            LoadSourceLines();
        }

        private void LoadSourceLines()
        {
            if (_sourceFileName != null && File.Exists(_sourceFileName))
            {
                try
                {
                    var lines = File.ReadAllLines(_sourceFileName);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        _sourceLines[i + 1] = lines[i]; // 1-based line numbers
                    }
                    if (_verboseMode)
                    {
                        Console.WriteLine($"Loaded {lines.Length} source lines from {_sourceFileName}");
                    }
                }
                catch (Exception ex)
                {
                    if (_verboseMode)
                    {
                        Console.WriteLine($"Failed to load source lines: {ex.Message}");
                    }
                }
            }
        }

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();
        public bool HasErrors => _diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Error);
        public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);
        public int ErrorCount => _diagnostics.Count(d => d.Severity >= DiagnosticSeverity.Error);
        public int WarningCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

        public void ReportError(string message, int line = 0, int column = 0, string? code = null, Exception? exception = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Error, message, location, code, exception);
            
            if (location != null && _sourceLines.ContainsKey(line))
            {
                diagnostic.SourceLine = _sourceLines[line];
            }
            
            _diagnostics.Add(diagnostic);
            PrintRustStyleDiagnostic(diagnostic);
        }

        public void ReportFatal(string message, int line = 0, int column = 0, string? code = null, Exception? exception = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Fatal, message, location, code, exception);
            
            if (location != null && _sourceLines.ContainsKey(line))
            {
                diagnostic.SourceLine = _sourceLines[line];
            }
            
            _diagnostics.Add(diagnostic);
            PrintRustStyleDiagnostic(diagnostic);
        }

        public void ReportWarning(string message, int line = 0, int column = 0, string? code = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Warning, message, location, code);
            
            if (location != null && _sourceLines.ContainsKey(line))
            {
                diagnostic.SourceLine = _sourceLines[line];
            }
            
            _diagnostics.Add(diagnostic);
            PrintRustStyleDiagnostic(diagnostic);
        }

        public void ReportInfo(string message, int line = 0, int column = 0, string? code = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Info, message, location, code);
            _diagnostics.Add(diagnostic);
            
            if (_verboseMode)
            {
                PrintRustStyleDiagnostic(diagnostic);
            }
        }

        private void PrintRustStyleDiagnostic(Diagnostic diagnostic)
        {
            var originalColor = Console.ForegroundColor;
            try
            {
                // Print the main diagnostic line
                var (severityColor, severityText) = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => (ConsoleColor.Red, "error"),
                    DiagnosticSeverity.Fatal => (ConsoleColor.DarkRed, "error"),
                    DiagnosticSeverity.Warning => (ConsoleColor.Yellow, "warning"),
                    DiagnosticSeverity.Info => (ConsoleColor.Cyan, "info"),
                    _ => (ConsoleColor.White, "note")
                };

                Console.ForegroundColor = severityColor;
                Console.Write(severityText);
                
                if (diagnostic.Code != null)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"[{diagnostic.Code}]");
                }
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(": ");
                Console.WriteLine(diagnostic.Message);

                // Print location and source context if available
                if (diagnostic.Location != null)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("  --> ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(diagnostic.Location.ToString());

                    // Print source line with highlighting
                    if (diagnostic.Location.Line > 0 && _sourceLines.ContainsKey(diagnostic.Location.Line))
                    {
                        var sourceLine = _sourceLines[diagnostic.Location.Line];
                        var lineNumber = diagnostic.Location.Line;
                        var lineNumberWidth = lineNumber.ToString().Length;
                        var padding = new string(' ', lineNumberWidth);
                        
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"{padding} |");
                        Console.Write($"{lineNumber} | ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(sourceLine);
                        
                        // Print caret pointer
                        if (diagnostic.Location.Column > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.Write($"{padding} | ");
                            Console.ForegroundColor = severityColor;
                            var caretPosition = Math.Max(0, diagnostic.Location.Column - 1);
                            Console.WriteLine(new string(' ', caretPosition) + "^");
                        }
                    }
                    else if (_verboseMode)
                    {
                        Console.WriteLine($"  (Source line {diagnostic.Location.Line} not available)");
                    }
                }

                // Print suggestion if available
                if (diagnostic.Suggestion != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("  = help: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(diagnostic.Suggestion);
                }

                Console.WriteLine(); // Empty line after each diagnostic
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        public void Clear()
        {
            _diagnostics.Clear();
        }

        public void PrintSummary()
        {
            if (_diagnostics.Count == 0)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("    Finished");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" μHigh compilation successfully");
                Console.ForegroundColor = originalColor;
                return;
            }

            var originalSummaryColor = Console.ForegroundColor;
            try
            {
                var errorCount = ErrorCount;
                var warningCount = WarningCount;
                
                if (errorCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("error");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(": could not compile `");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(Path.GetFileNameWithoutExtension(_sourceFileName ?? "source"));
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("` due to ");
                    
                    if (errorCount == 1)
                    {
                        Console.Write("previous error");
                    }
                    else
                    {
                        Console.Write($"{errorCount} previous errors");
                    }
                    
                    if (warningCount > 0)
                    {
                        Console.Write($"; {warningCount} warning{(warningCount == 1 ? "" : "s")} emitted");
                    }
                    
                    Console.WriteLine();
                }
                else if (warningCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("warning");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($": `{Path.GetFileNameWithoutExtension(_sourceFileName ?? "source")}` compiled with {warningCount} warning{(warningCount == 1 ? "" : "s")}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("    Finished");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" μHigh compilation successfully");
                }

                if (_verboseMode && _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Info))
                {
                    Console.WriteLine($"\nFor more information about these diagnostics, run with --verbose");
                }
            }
            finally
            {
                Console.ForegroundColor = originalSummaryColor;
            }
        }

        public void PrintAll()
        {
            foreach (var diagnostic in _diagnostics.OrderBy(d => d.Location?.Line ?? 0).ThenBy(d => d.Location?.Column ?? 0))
            {
                PrintRustStyleDiagnostic(diagnostic);
            }
        }
    }

    // Extension methods for common diagnostic patterns
    public static class DiagnosticsExtensions
    {
        public static void ReportTokenError(this DiagnosticsReporter diagnostics, string message, Token token, string? code = null)
        {
            diagnostics.ReportError(message, token.Line, token.Column, code);
        }

        public static void ReportTokenWarning(this DiagnosticsReporter diagnostics, string message, Token token, string? code = null)
        {
            diagnostics.ReportWarning(message, token.Line, token.Column, code);
        }

        public static void ReportUnexpectedToken(this DiagnosticsReporter diagnostics, Token token, string expected)
        {
            var diagnostic = diagnostics.Diagnostics.LastOrDefault();
            if (diagnostic != null)
            {
                diagnostic.Suggestion = $"Expected {expected}";
            }
            diagnostics.ReportTokenError($"Unexpected token '{token.Value}'", token, "UH001");
        }

        public static void ReportUnterminatedString(this DiagnosticsReporter diagnostics, int line, int column)
        {
            diagnostics.ReportError("Unterminated string literal", line, column, "UH002");
        }

        public static void ReportInvalidNumber(this DiagnosticsReporter diagnostics, string value, int line, int column)
        {
            diagnostics.ReportError($"Invalid number format: '{value}'", line, column, "UH003");
        }

        public static void ReportUnknownCharacter(this DiagnosticsReporter diagnostics, char character, int line, int column)
        {
            diagnostics.ReportError($"Unknown character: '{character}'", line, column, "UH004");
        }

        public static void ReportParseError(this DiagnosticsReporter diagnostics, string message, Token token)
        {
            diagnostics.ReportTokenError($"Parse error: {message}", token, "UH100");
        }

        public static void ReportCodeGenWarning(this DiagnosticsReporter diagnostics, string message, string? context = null)
        {
            diagnostics.ReportWarning($"Code generation: {message}" + (context != null ? $" (Context: {context})" : ""), code: "UH200");
        }

        // Method checking errors
        public static void ReportMethodNotFound(this DiagnosticsReporter diagnostics, string methodName, Token token)
        {
            diagnostics.ReportTokenError($"Method '{methodName}' is not defined", token, "UH201");
        }

        public static void ReportParameterMismatch(this DiagnosticsReporter diagnostics, string methodName, int expected, int actual, Token token)
        {
            diagnostics.ReportTokenError($"Method '{methodName}' expects {expected} parameter(s), but {actual} were provided", token, "UH202");
        }

        public static void ReportMethodSuggestion(this DiagnosticsReporter diagnostics, string suggestion, Token token)
        {
            diagnostics.ReportTokenWarning($"Did you mean: {suggestion}?", token, "UH204");
        }
    }
}
