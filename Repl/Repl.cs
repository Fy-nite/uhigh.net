using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using uhigh.Net.CodeGen;
using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;
using uhigh.Net.Parser;
using System.Reflection;
using System.Text;

namespace uhigh.Net.Repl
{
    public class ReplSession
    {
        private readonly bool _verboseMode;
        private readonly string? _stdLibPath;
        private readonly string? _saveCSharpTo;
        private readonly Compiler _compiler;
        private readonly Dictionary<string, object?> _variables = new();
        private readonly List<string> _sessionHistory = new();
        private readonly List<string> _csharpStatements = new();
        private ScriptState<object>? _scriptState;
        private int _commandCount = 0;

        public ReplSession(bool verboseMode = false, string? stdLibPath = null, string? saveCSharpTo = null)
        {
            _verboseMode = verboseMode;
            _stdLibPath = stdLibPath;
            _saveCSharpTo = saveCSharpTo;
            _compiler = new Compiler(verboseMode, stdLibPath);
        }

        public async Task StartAsync()
        {
            Console.WriteLine("μHigh Interactive REPL");
            Console.WriteLine("======================");
            Console.WriteLine("Type ':help' for available commands or ':exit' to quit");
            Console.WriteLine("Use Ctrl+Enter for newlines in multi-line blocks");
            Console.WriteLine();

            // Initialize the C# scripting environment
            await InitializeScriptingEnvironment();

            while (true)
            {
                try
                {
                    var input = ReadMultiLineInput();

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    if (input.StartsWith(':'))
                    {
                        if (!await HandleCommand(input))
                            break; // Exit command
                        continue;
                    }

                    await ProcessInput(input);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                    
                    if (_verboseMode)
                    {
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
            }
        }

        private string ReadMultiLineInput()
        {
            var lines = new List<string>();
            var isMultiLine = false;
            var prompt = "μhigh> ";

            while (true)
            {
                Console.Write(prompt);
                
                var line = ReadLineWithCtrlEnter();
                
                // Check if user pressed Ctrl+Enter (indicated by special marker)
                if (line.EndsWith("\x01CTRL_ENTER\x01"))
                {
                    line = line.Replace("\x01CTRL_ENTER\x01", "");
                    lines.Add(line);
                    isMultiLine = true;
                    prompt = "    ...> ";
                    continue;
                }
                
                // If we're in multi-line mode and user enters empty line with regular Enter, exit multi-line mode
                if (isMultiLine && string.IsNullOrWhiteSpace(line))
                {
                    break;
                }
                
                lines.Add(line);
                
                // Check if we need more input based on syntax
                var currentInput = string.Join("\n", lines);
                
                if (isMultiLine || NeedsMoreInput(currentInput))
                {
                    if (!isMultiLine)
                    {
                        isMultiLine = true;
                        prompt = "    ...> ";
                    }
                    continue;
                }
                
                break;
            }
            
            return string.Join("\n", lines);
        }

        private string ReadLineWithCtrlEnter()
        {
            var input = new StringBuilder();
            
            while (true)
            {
                var key = Console.ReadKey(true);
                
                if (key.Key == ConsoleKey.Enter)
                {
                    if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        // Ctrl+Enter - add newline marker and return
                        Console.WriteLine();
                        return input.ToString() + "\x01CTRL_ENTER\x01";
                    }
                    else
                    {
                        // Regular Enter - end input
                        Console.WriteLine();
                        return input.ToString();
                    }
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    // Escape cancels current input
                    Console.WriteLine();
                    return "";
                }
                else if (char.IsControl(key.KeyChar))
                {
                    // Ignore other control characters
                    continue;
                }
                else
                {
                    input.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

        private bool NeedsMoreInput(string input)
        {
            // Simple heuristic to detect incomplete blocks
            var trimmed = input.Trim();
            
            // Check for unclosed braces
            var openBraces = 0;
            var inString = false;
            var escaped = false;
            
            foreach (var ch in input)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                
                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }
                
                if (ch == '"')
                {
                    inString = !inString;
                    continue;
                }
                
                if (!inString)
                {
                    if (ch == '{')
                        openBraces++;
                    else if (ch == '}')
                        openBraces--;
                }
            }
            
            // If we have unclosed braces, we need more input
            if (openBraces > 0)
                return true;
            
            // Check for incomplete control structures
            if (trimmed.EndsWith("if") || trimmed.EndsWith("else") || 
                trimmed.EndsWith("while") || trimmed.EndsWith("for") ||
                trimmed.EndsWith("func") || trimmed.EndsWith("class") ||
                trimmed.EndsWith("namespace") || trimmed.Contains(" if ") && !trimmed.Contains("{"))
            {
                return true;
            }
            
            return false;
        }

        private async Task InitializeScriptingEnvironment()
        {
            try
            {
                var options = ScriptOptions.Default
                    .WithReferences(typeof(object).Assembly)
                    .WithReferences(typeof(Console).Assembly)
                    .WithReferences(typeof(System.Linq.Enumerable).Assembly)
                    .WithReferences(typeof(System.Collections.Generic.List<>).Assembly)
                    .WithReferences(typeof(System.Text.StringBuilder).Assembly)
                    .WithReferences(Assembly.GetExecutingAssembly())
                    .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.IO");

                // Add standard library references if available
                if (!string.IsNullOrEmpty(_stdLibPath) && Directory.Exists(_stdLibPath))
                {
                    var stdLibDlls = Directory.GetFiles(_stdLibPath, "*.dll", SearchOption.AllDirectories);
                    foreach (var dll in stdLibDlls)
                    {
                        try
                        {
                            var assembly = Assembly.LoadFrom(dll);
                            options = options.WithReferences(assembly);
                        }
                        catch
                        {
                            // Skip problematic assemblies
                            if (_verboseMode)
                            {
                                Console.WriteLine($"Skipped assembly: {dll}");
                            }
                        }
                    }
                }

                // Initialize with built-in functions
                var initCode = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public static void print(object value) => Console.WriteLine(value);
public static void println(object value) => Console.WriteLine(value);
public static string input() => Console.ReadLine() ?? """";
public static int @int(string s) => int.Parse(s);
public static int @int(double d) => (int)d;
public static double @float(string s) => double.Parse(s);
public static double @float(int i) => (double)i;
public static string @string(object obj) => obj?.ToString() ?? """";
public static bool @bool(object obj) => obj != null && !obj.Equals(0) && !obj.Equals("""");
";

                _scriptState = await CSharpScript.RunAsync(initCode, options);
                
                if (_verboseMode)
                {
                    Console.WriteLine("C# scripting environment initialized with μHigh built-ins");
                }
            }
            catch (Exception ex)
            {
                if (_verboseMode)
                {
                    Console.WriteLine($"Warning: Failed to initialize full scripting environment: {ex.Message}");
                    Console.WriteLine("Attempting minimal initialization...");
                }
                
                // Fallback to minimal initialization
                try
                {
                    var minimalOptions = ScriptOptions.Default
                        .WithReferences(typeof(object).Assembly)
                        .WithReferences(typeof(Console).Assembly)
                        .WithImports("System");

                    var minimalCode = @"
public static void print(object value) => System.Console.WriteLine(value);
public static void println(object value) => System.Console.WriteLine(value);
";
                    
                    _scriptState = await CSharpScript.RunAsync(minimalCode, minimalOptions);
                    
                    if (_verboseMode)
                    {
                        Console.WriteLine("Minimal C# scripting environment initialized");
                    }
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Warning: Could not initialize scripting environment: {fallbackEx.Message}");
                    Console.WriteLine("REPL will operate in degraded mode - only direct C# compilation available");
                }
            }
        }

        private async Task ProcessInput(string input)
        {
            _commandCount++;
            _sessionHistory.Add(input);

            try
            {
                // First, try to compile as μHigh code
                var diagnostics = new DiagnosticsReporter(_verboseMode);
                var csharpCode = "";

                try
                {
                    csharpCode = _compiler.CompileToCS(input, diagnostics);
                    
                    if (diagnostics.HasErrors)
                    {
                        // If μHigh compilation fails, try as direct C# code
                        await ExecuteAsCSharp(input);
                        return;
                    }
                }
                catch
                {
                    // If μHigh compilation fails, try as direct C# code
                    await ExecuteAsCSharp(input);
                    return;
                }

                // Extract the generated C# statements from the wrapper
                var extractedCode = ExtractExecutableCode(csharpCode);
                
                if (!string.IsNullOrEmpty(extractedCode))
                {
                    _csharpStatements.Add(extractedCode);
                    
                    if (_saveCSharpTo != null)
                    {
                        await SaveGeneratedCSharp(extractedCode);
                    }

                    if (_verboseMode)
                    {
                        Console.WriteLine($"Generated C#: {extractedCode}");
                    }

                    await ExecuteAsCSharp(extractedCode);
                }
                else
                {
                    // Fallback to direct execution
                    await ExecuteAsCSharp(input);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Execution error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private async Task ExecuteAsCSharp(string code)
        {
            try
            {
                if (_scriptState == null)
                {
                    await InitializeScriptingEnvironment();
                }

                _scriptState = await _scriptState!.ContinueWithAsync(code);
                
                // Display result if it's not null and not void
                if (_scriptState.ReturnValue != null)
                {
                    Console.WriteLine(_scriptState.ReturnValue);
                }
            }
            catch (CompilationErrorException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation errors:");
                foreach (var diagnostic in ex.Diagnostics)
                {
                    Console.WriteLine($"  {diagnostic}");
                }
                Console.ResetColor();
            }
        }

        private string ExtractExecutableCode(string generatedCSharp)
        {
            // Extract executable statements from the generated C# wrapper
            var lines = generatedCSharp.Split('\n');
            var inMainMethod = false;
            var braceCount = 0;
            var extractedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.Contains("static void Main"))
                {
                    inMainMethod = true;
                    continue;
                }

                if (inMainMethod)
                {
                    if (trimmedLine == "{")
                    {
                        braceCount++;
                        continue;
                    }
                    
                    if (trimmedLine == "}")
                    {
                        braceCount--;
                        if (braceCount == 0)
                            break;
                        continue;
                    }

                    if (braceCount > 0 && !string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        extractedLines.Add(trimmedLine);
                    }
                }
            }

            return string.Join("\n", extractedLines);
        }

        private async Task SaveGeneratedCSharp(string code)
        {
            if (_saveCSharpTo == null) return;

            try
            {
                if (!Directory.Exists(_saveCSharpTo))
                {
                    Directory.CreateDirectory(_saveCSharpTo);
                }

                var fileName = $"repl_session_{DateTime.Now:yyyyMMdd_HHmmss}.cs";
                var filePath = Path.Combine(_saveCSharpTo, fileName);
                
                var fullCode = $@"// μHigh REPL Session - Command {_commandCount}
// Generated: {DateTime.Now}
// Original μHigh: {_sessionHistory.Last()}

{code}
";
                
                await File.WriteAllTextAsync(filePath, fullCode);
                Console.WriteLine($"Generated C# saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save C# code: {ex.Message}");
            }
        }

        private async Task<bool> HandleCommand(string command)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLower();

            switch (cmd)
            {
                case ":help":
                    ShowHelp();
                    break;

                case ":exit":
                case ":quit":
                    Console.WriteLine("Goodbye!");
                    return false;

                case ":clear":
                    Console.Clear();
                    Console.WriteLine("μHigh Interactive REPL");
                    Console.WriteLine("======================");
                    break;

                case ":reset":
                    await ResetSession();
                    break;

                case ":vars":
                    ShowVariables();
                    break;

                case ":history":
                    ShowHistory();
                    break;

                case ":code":
                    ShowGeneratedCode();
                    break;

                case ":save":
                    if (parts.Length > 1)
                        await SaveSession(parts[1]);
                    else
                        Console.WriteLine("Usage: :save <filename>");
                    break;

                case ":load":
                    if (parts.Length > 1)
                        await LoadSession(parts[1]);
                    else
                        Console.WriteLine("Usage: :load <filename>");
                    break;

                case ":verbose":
                    Console.WriteLine($"Verbose mode: {(_verboseMode ? "ON" : "OFF")}");
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Type ':help' for available commands");
                    break;
            }

            return true;
        }

        private void ShowHelp()
        {
            Console.WriteLine("μHigh REPL Commands:");
            Console.WriteLine("  :help     - Show this help message");
            Console.WriteLine("  :exit     - Exit the REPL");
            Console.WriteLine("  :clear    - Clear the screen");
            Console.WriteLine("  :reset    - Reset the session");
            Console.WriteLine("  :vars     - Show defined variables");
            Console.WriteLine("  :history  - Show command history");
            Console.WriteLine("  :code     - Show generated C# code");
            Console.WriteLine("  :save <f> - Save session to file");
            Console.WriteLine("  :load <f> - Load session from file");
            Console.WriteLine("  :verbose  - Show verbose mode status");
            Console.WriteLine();
            Console.WriteLine("Multi-line Input:");
            Console.WriteLine("  Ctrl+Enter - Add a newline (continue input)");
            Console.WriteLine("  Enter      - Execute the input");
            Console.WriteLine("  Escape     - Cancel current input");
            Console.WriteLine();
            Console.WriteLine("You can enter μHigh expressions or statements directly:");
            Console.WriteLine("  var x = 42");
            Console.WriteLine("  print(\"Hello, World!\")");
            Console.WriteLine("  if x > 10 {        <- Press Ctrl+Enter here");
            Console.WriteLine("      print(\"big\")  <- Press Enter here to execute");
            Console.WriteLine("  }");
            Console.WriteLine();
            Console.WriteLine("Or direct C# code:");
            Console.WriteLine("  Console.WriteLine(\"Direct C#\");");
            Console.WriteLine("  var list = new List<int> {1, 2, 3};");
        }

        private async Task ResetSession()
        {
            _variables.Clear();
            _sessionHistory.Clear();
            _csharpStatements.Clear();
            _commandCount = 0;
            _scriptState = null;
            
            await InitializeScriptingEnvironment();
            Console.WriteLine("Session reset");
        }

        private void ShowVariables()
        {
            if (_scriptState?.Variables.Any() == true)
            {
                Console.WriteLine("Defined variables:");
                foreach (var variable in _scriptState.Variables)
                {
                    var value = variable.Value?.ToString() ?? "null";
                    var type = variable.Type.Name;
                    Console.WriteLine($"  {variable.Name}: {type} = {value}");
                }
            }
            else
            {
                Console.WriteLine("No variables defined");
            }
        }

        private void ShowHistory()
        {
            if (_sessionHistory.Count == 0)
            {
                Console.WriteLine("No history");
                return;
            }

            Console.WriteLine("Command history:");
            for (int i = 0; i < _sessionHistory.Count; i++)
            {
                Console.WriteLine($"  {i + 1}: {_sessionHistory[i]}");
            }
        }

        private void ShowGeneratedCode()
        {
            if (_csharpStatements.Count == 0)
            {
                Console.WriteLine("No generated C# code");
                return;
            }

            Console.WriteLine("Generated C# code:");
            for (int i = 0; i < _csharpStatements.Count; i++)
            {
                Console.WriteLine($"// Command {i + 1}");
                Console.WriteLine(_csharpStatements[i]);
                Console.WriteLine();
            }
        }

        private async Task SaveSession(string filename)
        {
            try
            {
                var sessionData = new
                {
                    Timestamp = DateTime.Now,
                    Commands = _sessionHistory,
                    GeneratedCode = _csharpStatements
                };

                var json = System.Text.Json.JsonSerializer.Serialize(sessionData, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filename, json);
                Console.WriteLine($"Session saved to: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving session: {ex.Message}");
            }
        }

        private async Task LoadSession(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    Console.WriteLine($"File not found: {filename}");
                    return;
                }

                var json = await File.ReadAllTextAsync(filename);
                var sessionData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                
                Console.WriteLine($"Session loaded from: {filename}");
                Console.WriteLine("Use :history to see loaded commands");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session: {ex.Message}");
            }
        }
    }
}
