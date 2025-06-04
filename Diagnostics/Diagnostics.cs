using Wake.Net.Lexer;

namespace Wake.Net.Diagnostics
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
            return FileName != null ? $"{FileName}({Line},{Column})" : $"({Line},{Column})";
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
            var code = Code != null ? $" [{Code}]" : "";
            return $"{location}: {severity}{code}: {Message}";
        }
    }

    public class DiagnosticsReporter
    {
        private readonly List<Diagnostic> _diagnostics = new();
        private readonly bool _verboseMode;
        private readonly string? _sourceFileName;

        public DiagnosticsReporter(bool verboseMode = false, string? sourceFileName = null)
        {
            _verboseMode = verboseMode;
            _sourceFileName = sourceFileName;
        }

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();
        public bool HasErrors => _diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Error);
        public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);
        public int ErrorCount => _diagnostics.Count(d => d.Severity >= DiagnosticSeverity.Error);
        public int WarningCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

        public void ReportInfo(string message, int line = 0, int column = 0, string? code = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Info, message, location, code);
            _diagnostics.Add(diagnostic);
            
            if (_verboseMode)
            {
                Console.WriteLine($"[INFO] {diagnostic}");
            }
        }

        public void ReportWarning(string message, int line = 0, int column = 0, string? code = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Warning, message, location, code);
            _diagnostics.Add(diagnostic);
            
            Console.WriteLine($"[WARNING] {diagnostic}");
        }

        public void ReportError(string message, int line = 0, int column = 0, string? code = null, Exception? exception = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Error, message, location, code, exception);
            _diagnostics.Add(diagnostic);
            
            Console.WriteLine($"[ERROR] {diagnostic}");
            if (_verboseMode && exception != null)
            {
                Console.WriteLine($"Exception details: {exception}");
            }
        }

        public void ReportFatal(string message, int line = 0, int column = 0, string? code = null, Exception? exception = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Fatal, message, location, code, exception);
            _diagnostics.Add(diagnostic);
            
            Console.WriteLine($"[FATAL] {diagnostic}");
            if (exception != null)
            {
                Console.WriteLine($"Exception details: {exception}");
            }
        }

        public void ReportTokenError(string message, Token token, string? code = null, Exception? exception = null)
        {
            ReportError(message, token.Line, token.Column, code, exception);
        }

        public void ReportTokenWarning(string message, Token token, string? code = null)
        {
            ReportWarning(message, token.Line, token.Column, code);
        }

        public void Clear()
        {
            _diagnostics.Clear();
        }

        public void PrintSummary()
        {
            if (_diagnostics.Count == 0)
            {
                Console.WriteLine("Compilation completed successfully with no diagnostics.");
                return;
            }

            Console.WriteLine($"\nCompilation completed with {_diagnostics.Count} diagnostic(s):");
            Console.WriteLine($"  Errors: {ErrorCount}");
            Console.WriteLine($"  Warnings: {WarningCount}");
            Console.WriteLine($"  Info: {_diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info)}");

            if (!_verboseMode && _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Info))
            {
                Console.WriteLine("Run with --verbose to see all diagnostic messages.");
            }
        }

        public void PrintAll()
        {
            foreach (var diagnostic in _diagnostics.OrderBy(d => d.Location?.Line ?? 0).ThenBy(d => d.Location?.Column ?? 0))
            {
                var severityColor = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error or DiagnosticSeverity.Fatal => ConsoleColor.Red,
                    DiagnosticSeverity.Warning => ConsoleColor.Yellow,
                    DiagnosticSeverity.Info => ConsoleColor.Cyan,
                    _ => ConsoleColor.White
                };

                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = severityColor;
                Console.WriteLine(diagnostic);
                Console.ForegroundColor = originalColor;
            }
        }
    }

    // Extension methods for common diagnostic patterns
    public static class DiagnosticsExtensions
    {
        public static void ReportUnexpectedToken(this DiagnosticsReporter diagnostics, Token token, string expected)
        {
            diagnostics.ReportTokenError($"Unexpected token '{token.Value}'. Expected {expected}.", token, "UH001");
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
