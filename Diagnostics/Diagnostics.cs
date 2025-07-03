using uhigh.Net.Lexer;
using System.Diagnostics;

namespace uhigh.Net.Diagnostics
{
    /// <summary>
    /// The diagnostic severity enum
    /// </summary>
    public enum DiagnosticSeverity
    {
        /// <summary>
        /// The info diagnostic severity
        /// </summary>
        Info,
        /// <summary>
        /// The warning diagnostic severity
        /// </summary>
        Warning,
        /// <summary>
        /// The error diagnostic severity
        /// </summary>
        Error,
        /// <summary>
        /// The fatal diagnostic severity
        /// </summary>
        Fatal
    }

    /// <summary>
    /// The source location class
    /// </summary>
    public class SourceLocation
    {
        /// <summary>
        /// Gets or sets the value of the line
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// Gets or sets the value of the column
        /// </summary>
        public int Column { get; set; }
        /// <summary>
        /// Gets or sets the value of the file name
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SourceLocation"/> class
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <param name="fileName">The file name</param>
        public SourceLocation(int line, int column, string? fileName = null)
        {
            Line = line;
            Column = column;
            FileName = fileName;
        }

        /// <summary>
        /// Returns the string
        /// </summary>
        /// <returns>The string</returns>
        public override string ToString()
        {
            return FileName != null ? $"{FileName}:{Line}:{Column}" : $"{Line}:{Column}";
        }
    }

    /// <summary>
    /// The diagnostic class
    /// </summary>
    public class Diagnostic
    {
        /// <summary>
        /// Gets or sets the value of the severity
        /// </summary>
        public DiagnosticSeverity Severity { get; set; }
        /// <summary>
        /// Gets or sets the value of the message
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Gets or sets the value of the location
        /// </summary>
        public SourceLocation? Location { get; set; }
        /// <summary>
        /// Gets or sets the value of the code
        /// </summary>
        public string? Code { get; set; }
        /// <summary>
        /// Gets or sets the value of the exception
        /// </summary>
        public Exception? Exception { get; set; }
        /// <summary>
        /// Gets or sets the value of the timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Gets or sets the value of the suggestion
        /// </summary>
        public string? Suggestion { get; set; }
        /// <summary>
        /// Gets or sets the value of the source line
        /// </summary>
        public string? SourceLine { get; set; }
        /// <summary>
        /// Gets or sets the value of the caller info
        /// </summary>
        public string? CallerInfo { get; set; }
        /// <summary>
        /// Gets or sets the value of the stack trace
        /// </summary>
        public List<string> StackTrace { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Diagnostic"/> class
        /// </summary>
        /// <param name="severity">The severity</param>
        /// <param name="message">The message</param>
        /// <param name="location">The location</param>
        /// <param name="code">The code</param>
        /// <param name="exception">The exception</param>
        public Diagnostic(DiagnosticSeverity severity, string message, SourceLocation? location = null, string? code = null, Exception? exception = null)
        {
            Severity = severity;
            Message = message;
            Location = location;
            Code = code;
            Exception = exception;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Returns the string
        /// </summary>
        /// <returns>The string</returns>
        public override string ToString()
        {
            var severity = Severity.ToString().ToLower();
            var location = Location?.ToString() ?? "unknown";
            var code = Code != null ? $"[{Code}]" : "";
            var caller = CallerInfo != null ? $" (Called from: {CallerInfo})" : "";
            return $"{severity}{code}: {Message}{caller}";
        }
    }

    /// <summary>
    /// The diagnostics reporter class
    /// </summary>
    public class DiagnosticsReporter
    {
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly List<Diagnostic> _diagnostics = new();
        /// <summary>
        /// The verbose mode
        /// </summary>
        private readonly bool _verboseMode;
        /// <summary>
        /// The source file name
        /// </summary>
        private readonly string? _sourceFileName;
        /// <summary>
        /// The source lines
        /// </summary>
        private readonly Dictionary<int, string> _sourceLines = new();
        /// <summary>
        /// The suppress output
        /// </summary>
        private readonly bool _suppressOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsReporter"/> class
        /// </summary>
        /// <param name="verboseMode">The verbose mode</param>
        /// <param name="sourceFileName">The source file name</param>
        /// <param name="suppressOutput">The suppress output</param>
        public DiagnosticsReporter(bool verboseMode = false, string? sourceFileName = null, bool suppressOutput = false)
        {
            _verboseMode = verboseMode;
            _sourceFileName = sourceFileName;
            _suppressOutput = suppressOutput;
            LoadSourceLines();
        }

        /// <summary>
        /// Loads the source lines
        /// </summary>
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
                    if (_verboseMode && !_suppressOutput)
                    {
                        Console.WriteLine($"Loaded {lines.Length} source lines from {_sourceFileName}");
                    }
                }
                catch (Exception ex)
                {
                    if (_verboseMode && !_suppressOutput)
                    {
                        Console.WriteLine($"Failed to load source lines: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the value of the diagnostics
        /// </summary>
        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();
        /// <summary>
        /// Gets the value of the has errors
        /// </summary>
        public bool HasErrors => _diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Error);
        /// <summary>
        /// Gets the value of the has warnings
        /// </summary>
        public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);
        /// <summary>
        /// Gets the value of the error count
        /// </summary>
        public int ErrorCount => _diagnostics.Count(d => d.Severity >= DiagnosticSeverity.Error);
        /// <summary>
        /// Gets the value of the warning count
        /// </summary>
        public int WarningCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

        /// <summary>
        /// Adds the caller info to diagnostic using the specified diagnostic
        /// </summary>
        /// <param name="diagnostic">The diagnostic</param>
        private void AddCallerInfoToDiagnostic(Diagnostic diagnostic)
        {
            if (!_verboseMode) return;

            var stackTrace = new System.Diagnostics.StackTrace(true);
            var frames = stackTrace.GetFrames();
            
            if (frames != null)
            {
                var relevantFrames = frames
                    .Skip(2) // Skip this method and the immediate caller
                    .Where(f => f.GetMethod() != null)
                    .Take(10) // Limit to 10 frames
                    .Select(f => {
                        var method = f.GetMethod();
                        var fileName = f.GetFileName();
                        var lineNumber = f.GetFileLineNumber();
                        var methodName = $"{method?.DeclaringType?.Name}.{method?.Name}";
                        
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            return $"{methodName} at {Path.GetFileName(fileName)}:{lineNumber}";
                        }
                        return methodName;
                    })
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                diagnostic.StackTrace = relevantFrames;
                
                if (relevantFrames.Count > 0)
                {
                    diagnostic.CallerInfo = relevantFrames.First();
                }
            }
        }

        /// <summary>
        /// Reports the error using the specified message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <param name="code">The code</param>
        /// <param name="exception">The exception</param>
        public void ReportError(string message, int line = 0, int column = 0, string? code = null, Exception? exception = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Error, message, location, code, exception);
            
            if (location != null && _sourceLines.ContainsKey(line))
            {
                diagnostic.SourceLine = _sourceLines[line];
            }
            
            AddCallerInfoToDiagnostic(diagnostic);
            _diagnostics.Add(diagnostic);
            PrintRustStyleDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports the fatal using the specified message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <param name="code">The code</param>
        /// <param name="exception">The exception</param>
        public void ReportFatal(string message, int line = 0, int column = 0, string? code = null, Exception? exception = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Fatal, message, location, code, exception);
            
            if (location != null && _sourceLines.ContainsKey(line))
            {
                diagnostic.SourceLine = _sourceLines[line];
            }
            
            AddCallerInfoToDiagnostic(diagnostic);
            _diagnostics.Add(diagnostic);
            PrintRustStyleDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports the warning using the specified message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <param name="code">The code</param>
        public void ReportWarning(string message, int line = 0, int column = 0, string? code = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Warning, message, location, code);
            
            if (location != null && _sourceLines.ContainsKey(line))
            {
                diagnostic.SourceLine = _sourceLines[line];
            }
            
            AddCallerInfoToDiagnostic(diagnostic);
            _diagnostics.Add(diagnostic);
            PrintRustStyleDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports the info using the specified message
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <param name="code">The code</param>
        public void ReportInfo(string message, int line = 0, int column = 0, string? code = null)
        {
            var location = line > 0 ? new SourceLocation(line, column, _sourceFileName) : null;
            var diagnostic = new Diagnostic(DiagnosticSeverity.Info, message, location, code);
            AddCallerInfoToDiagnostic(diagnostic);
            _diagnostics.Add(diagnostic);
            
            if (_verboseMode)
            {
                PrintRustStyleDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Prints the rust style diagnostic using the specified diagnostic
        /// </summary>
        /// <param name="diagnostic">The diagnostic</param>
        private void PrintRustStyleDiagnostic(Diagnostic diagnostic)
        {
            if (_suppressOutput) return; // Don't print anything if output is suppressed
            
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

                // Print caller information if available and in verbose mode
                if (_verboseMode && diagnostic.CallerInfo != null)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("  --> Called from: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(diagnostic.CallerInfo);
                }

                // Print stack trace if available and in verbose mode
                if (_verboseMode && diagnostic.StackTrace.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("  --> Call stack:");
                    foreach (var frame in diagnostic.StackTrace.Take(5)) // Limit to first 5 frames
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine($"      {frame}");
                    }
                }

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

        /// <summary>
        /// Clears this instance
        /// </summary>
        public void Clear()
        {
            _diagnostics.Clear();
        }

        /// <summary>
        /// Prints the summary
        /// </summary>
        public void PrintSummary()
        {
            if (_suppressOutput) return; // Don't print anything if output is suppressed
            
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

        /// <summary>
        /// Prints the all
        /// </summary>
        public void PrintAll()
        {
            foreach (var diagnostic in _diagnostics.OrderBy(d => d.Location?.Line ?? 0).ThenBy(d => d.Location?.Column ?? 0))
            {
                PrintRustStyleDiagnostic(diagnostic);
            }
        }
    }

    // Extension methods for common diagnostic patterns
    /// <summary>
    /// The diagnostics extensions class
    /// </summary>
    public static class DiagnosticsExtensions
    {
        /// <summary>
        /// Reports the token error using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="message">The message</param>
        /// <param name="token">The token</param>
        /// <param name="code">The code</param>
        public static void ReportTokenError(this DiagnosticsReporter diagnostics, string message, Token token, string? code = null)
        {
            diagnostics.ReportError(message, token.Line, token.Column, code);
        }

        /// <summary>
        /// Reports the token warning using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="message">The message</param>
        /// <param name="token">The token</param>
        /// <param name="code">The code</param>
        public static void ReportTokenWarning(this DiagnosticsReporter diagnostics, string message, Token token, string? code = null)
        {
            diagnostics.ReportWarning(message, token.Line, token.Column, code);
        }

        /// <summary>
        /// Reports the unexpected token using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="token">The token</param>
        /// <param name="expected">The expected</param>
        public static void ReportUnexpectedToken(this DiagnosticsReporter diagnostics, Token token, string expected)
        {
            // Get detailed stack trace for debugging
            var stackTrace = new System.Diagnostics.StackTrace(true);
            var frames = stackTrace.GetFrames();
            
            var callerChain = "";
            if (frames != null && frames.Length > 2)
            {
                var relevantFrames = frames
                    .Skip(1) // Skip this method
                    .Take(5) // Take next 5 frames
                    .Where(f => f.GetMethod() != null)
                    .Select(f => {
                        var method = f.GetMethod();
                        var fileName = f.GetFileName();
                        var lineNumber = f.GetFileLineNumber();
                        var methodName = $"{method?.DeclaringType?.Name}.{method?.Name}";
                        
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            return $"{methodName}({Path.GetFileName(fileName)}:{lineNumber})";
                        }
                        return methodName;
                    })
                    .Where(s => !string.IsNullOrEmpty(s));
                
                callerChain = string.Join(" -> ", relevantFrames);
            }

            var message = $"Unexpected token '{token.Value}' at {token.Line}:{token.Column}. Expected: {expected}";
            if (!string.IsNullOrEmpty(callerChain))
            {
                message += $"\n  Call chain: {callerChain}";
            }

            diagnostics.ReportError(message, token.Line, token.Column, "UH001");
            
            // Set suggestion on the last diagnostic
            var diagnostic = diagnostics.Diagnostics.LastOrDefault();
            if (diagnostic != null)
            {
                diagnostic.Suggestion = $"Expected {expected}";
            }
        }

        /// <summary>
        /// Reports the unterminated string using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        public static void ReportUnterminatedString(this DiagnosticsReporter diagnostics, int line, int column)
        {
            diagnostics.ReportError("Unterminated string literal", line, column, "UH002");
        }

        /// <summary>
        /// Reports the invalid number using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="value">The value</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        public static void ReportInvalidNumber(this DiagnosticsReporter diagnostics, string value, int line, int column)
        {
            diagnostics.ReportError($"Invalid number format: '{value}'", line, column, "UH003");
        }

        /// <summary>
        /// Reports the unknown character using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="character">The character</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        public static void ReportUnknownCharacter(this DiagnosticsReporter diagnostics, char character, int line, int column)
        {
            diagnostics.ReportError($"Unknown character: '{character}'", line, column, "UH004");
        }

        /// <summary>
        /// Reports the parse error using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="message">The message</param>
        /// <param name="token">The token</param>
        public static void ReportParseError(this DiagnosticsReporter diagnostics, string message, Token token)
        {
            diagnostics.ReportTokenError($"Parse error: {message}", token, "UH100");
        }        /// <summary>
/// Reports the code gen warning using the specified diagnostics
/// </summary>
/// <param name="diagnostics">The diagnostics</param>
/// <param name="message">The message</param>
/// <param name="context">The context</param>
public static void ReportCodeGenWarning(this DiagnosticsReporter diagnostics, string message, string? context = null)
        {
            diagnostics.ReportWarning($"Code generation: {message}" + (context != null ? $" (Context: {context})" : ""), code: "UH200");
        }

        // Method checking errors
        /// <summary>
        /// Reports the method not found using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="methodName">The method name</param>
        /// <param name="token">The token</param>
        public static void ReportMethodNotFound(this DiagnosticsReporter diagnostics, string methodName, Token token)
        {
            diagnostics.ReportTokenError($"Method '{methodName}' is not defined", token, "UH201");
        }

        /// <summary>
        /// Reports the method parameter mismatch using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="methodName">The method name</param>
        /// <param name="expected">The expected</param>
        /// <param name="actual">The actual</param>
        /// <param name="token">The token</param>
        public static void ReportMethodParameterMismatch(this DiagnosticsReporter diagnostics, string methodName, int expected, int actual, Token token)
        {
            if (expected == actual)
            {
                // Same count but type mismatch
                diagnostics.ReportError($"Method '{methodName}' parameter types do not match the provided arguments", token.Line, token.Column, "UH203");
            }
            else
            {
                diagnostics.ReportError($"Method '{methodName}' expects {expected} parameter(s), but {actual} were provided", token.Line, token.Column, "UH202");
            }
        }

        /// <summary>
        /// Reports the method suggestion using the specified diagnostics
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="suggestion">The suggestion</param>
        /// <param name="token">The token</param>
        public static void ReportMethodSuggestion(this DiagnosticsReporter diagnostics, string suggestion, Token token)
        {
            diagnostics.ReportTokenWarning($"Did you mean: {suggestion}?", token, "UH204");
        }
    }
}
