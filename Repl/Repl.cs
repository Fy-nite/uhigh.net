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
    /// <summary>
    /// The repl session class
    /// </summary>
    public class ReplSession
    {
        /// <summary>
        /// The verbose mode
        /// </summary>
        private readonly bool _verboseMode;
        /// <summary>
        /// The std lib path
        /// </summary>
        private readonly string? _stdLibPath;
        /// <summary>
        /// The save sharp to
        /// </summary>
        private readonly string? _saveCSharpTo;
        /// <summary>
        /// The compiler
        /// </summary>
        private readonly Compiler _compiler;
        /// <summary>
        /// The variables
        /// </summary>
        private readonly Dictionary<string, object?> _variables = new();
        /// <summary>
        /// The session history
        /// </summary>
        private readonly List<string> _sessionHistory = new();
        /// <summary>
        /// The csharp statements
        /// </summary>
        private readonly List<string> _csharpStatements = new();
        /// <summary>
        /// The script state
        /// </summary>
        private ScriptState<object>? _scriptState;
        /// <summary>
        /// The command count
        /// </summary>
        private int _commandCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplSession"/> class
        /// </summary>
        /// <param name="verboseMode">The verbose mode</param>
        /// <param name="stdLibPath">The std lib path</param>
        /// <param name="saveCSharpTo">The save sharp to</param>
        public ReplSession(bool verboseMode = false, string? stdLibPath = null, string? saveCSharpTo = null)
        {
            _verboseMode = verboseMode;
            _stdLibPath = stdLibPath;
            _saveCSharpTo = saveCSharpTo;
            _compiler = new Compiler(verboseMode, stdLibPath);
        }

        /// <summary>
        /// Starts this instance
        /// </summary>
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

        /// <summary>
        /// Reads the multi line input
        /// </summary>
        /// <returns>The string</returns>
        private string ReadMultiLineInput()
        {
            var lines = new List<string> { "" };
            var currentLineIndex = 0;
            var cursorPosition = 0;
            var isMultiLine = false;
            var basePrompt = "μhigh> ";
            var continuationPrompt = "    ...> ";
            
            // Display initial prompt
            Console.Write(basePrompt);
            var startTop = Console.CursorTop;
            var startLeft = basePrompt.Length;

            while (true)
            {
                var key = Console.ReadKey(true);
                
                if (key.Key == ConsoleKey.Enter)
                {
                    if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        // Ctrl+Enter - add new line and continue
                        lines.Add("");
                        currentLineIndex++;
                        cursorPosition = 0;
                        isMultiLine = true;
                        RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                        continue;
                    }
                    else
                    {
                        // Regular Enter - check if we need more input
                        var fullInput = string.Join("\n", lines.Where(l => !string.IsNullOrWhiteSpace(l) || lines.Count == 1));
                        
                        if (isMultiLine && string.IsNullOrWhiteSpace(lines[currentLineIndex]) && !NeedsMoreInput(fullInput))
                        {
                            // Empty line in multiline mode and no more input needed - execute
                            Console.WriteLine();
                            return fullInput.Trim();
                        }
                        else if (!isMultiLine && !NeedsMoreInput(fullInput))
                        {
                            // Single line complete - execute
                            Console.WriteLine();
                            return fullInput.Trim();
                        }
                        else
                        {
                            // Need more input - add new line
                            if (!string.IsNullOrWhiteSpace(lines[currentLineIndex]))
                            {
                                lines.Add("");
                                currentLineIndex++;
                                cursorPosition = 0;
                            }
                            isMultiLine = true;
                            RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                            continue;
                        }
                    }
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (cursorPosition > 0)
                    {
                        // Remove character before cursor in current line
                        lines[currentLineIndex] = lines[currentLineIndex].Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                    }
                    else if (currentLineIndex > 0)
                    {
                        // Backspace at beginning of line - merge with previous line
                        cursorPosition = lines[currentLineIndex - 1].Length;
                        lines[currentLineIndex - 1] += lines[currentLineIndex];
                        lines.RemoveAt(currentLineIndex);
                        currentLineIndex--;
                        if (lines.Count == 1 && currentLineIndex == 0)
                        {
                            isMultiLine = false;
                        }
                    }
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.Delete)
                {
                    if (cursorPosition < lines[currentLineIndex].Length)
                    {
                        // Remove character at cursor in current line
                        lines[currentLineIndex] = lines[currentLineIndex].Remove(cursorPosition, 1);
                    }
                    else if (currentLineIndex < lines.Count - 1)
                    {
                        // Delete at end of line - merge with next line
                        lines[currentLineIndex] += lines[currentLineIndex + 1];
                        lines.RemoveAt(currentLineIndex + 1);
                        if (lines.Count == 1)
                        {
                            isMultiLine = false;
                        }
                    }
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                    }
                    else if (currentLineIndex > 0)
                    {
                        // Move to end of previous line
                        currentLineIndex--;
                        cursorPosition = lines[currentLineIndex].Length;
                    }
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.RightArrow)
                {
                    if (cursorPosition < lines[currentLineIndex].Length)
                    {
                        cursorPosition++;
                    }
                    else if (currentLineIndex < lines.Count - 1)
                    {
                        // Move to beginning of next line
                        currentLineIndex++;
                        cursorPosition = 0;
                    }
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    if (currentLineIndex > 0)
                    {
                        currentLineIndex--;
                        // Try to maintain cursor position, but clamp to line length
                        cursorPosition = Math.Min(cursorPosition, lines[currentLineIndex].Length);
                        RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    if (currentLineIndex < lines.Count - 1)
                    {
                        currentLineIndex++;
                        // Try to maintain cursor position, but clamp to line length
                        cursorPosition = Math.Min(cursorPosition, lines[currentLineIndex].Length);
                        RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                    }
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        // Ctrl+Home - go to beginning of entire input
                        currentLineIndex = 0;
                        cursorPosition = 0;
                    }
                    else
                    {
                        // Home - go to beginning of current line
                        cursorPosition = 0;
                    }
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.End)
                {
                    if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        // Ctrl+End - go to end of entire input
                        currentLineIndex = lines.Count - 1;
                        cursorPosition = lines[currentLineIndex].Length;
                    }
                    else
                    {
                        // End - go to end of current line
                        cursorPosition = lines[currentLineIndex].Length;
                    }
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    // Escape cancels current input
                    ClearMultiLineInput(lines.Count, startTop);
                    Console.WriteLine();
                    return "";
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    // Tab - insert 4 spaces
                    var spaces = "    ";
                    lines[currentLineIndex] = lines[currentLineIndex].Insert(cursorPosition, spaces);
                    cursorPosition += spaces.Length;
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.PageUp)
                {
                    // Page Up - go to first line
                    currentLineIndex = 0;
                    cursorPosition = Math.Min(cursorPosition, lines[currentLineIndex].Length);
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (key.Key == ConsoleKey.PageDown)
                {
                    // Page Down - go to last line
                    currentLineIndex = lines.Count - 1;
                    cursorPosition = Math.Min(cursorPosition, lines[currentLineIndex].Length);
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
                else if (char.IsControl(key.KeyChar))
                {
                    // Handle other control characters
                    if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control)
                    {
                        // Ctrl+C - cancel input
                        ClearMultiLineInput(lines.Count, startTop);
                        Console.WriteLine("^C");
                        return "";
                    }
                    else if (key.Key == ConsoleKey.A && key.Modifiers == ConsoleModifiers.Control)
                    {
                        // Ctrl+A - go to beginning of line
                        cursorPosition = 0;
                        RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                    }
                    else if (key.Key == ConsoleKey.E && key.Modifiers == ConsoleModifiers.Control)
                    {
                        // Ctrl+E - go to end of line
                        cursorPosition = lines[currentLineIndex].Length;
                        RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                    }
                    // Ignore other control characters
                    continue;
                }
                else
                {
                    // Insert character at cursor position
                    lines[currentLineIndex] = lines[currentLineIndex].Insert(cursorPosition, key.KeyChar.ToString());
                    cursorPosition++;
                    RedrawMultiLineInput(lines, currentLineIndex, cursorPosition, basePrompt, continuationPrompt, startTop);
                }
            }
        }

        /// <summary>
        /// Redraws the multi line input using the specified lines
        /// </summary>
        /// <param name="lines">The lines</param>
        /// <param name="currentLineIndex">The current line index</param>
        /// <param name="cursorPosition">The cursor position</param>
        /// <param name="basePrompt">The base prompt</param>
        /// <param name="continuationPrompt">The continuation prompt</param>
        /// <param name="startTop">The start top</param>
        private void RedrawMultiLineInput(List<string> lines, int currentLineIndex, int cursorPosition, 
            string basePrompt, string continuationPrompt, int startTop)
        {
            try
            {
                // Save current position
                var currentTop = Console.CursorTop;
                
                // Clear existing lines - be more conservative about clearing
                var maxLinesToClear = Math.Max(lines.Count, currentTop - startTop + 1);
                for (int i = 0; i < maxLinesToClear && (startTop + i) < Console.BufferHeight; i++)
                {
                    try
                    {
                        Console.SetCursorPosition(0, startTop + i);
                        Console.Write(new string(' ', Math.Min(Console.WindowWidth - 1, 120)));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Ignore cursor position errors near buffer boundaries
                        break;
                    }
                }
                
                // Redraw all lines
                for (int i = 0; i < lines.Count; i++)
                {
                    try
                    {
                        var targetTop = startTop + i;
                        if (targetTop >= Console.BufferHeight) break;
                        
                        Console.SetCursorPosition(0, targetTop);
                        var prompt = i == 0 ? basePrompt : continuationPrompt;
                        var lineContent = prompt + lines[i];
                        
                        // Truncate if line is too long for console
                        if (lineContent.Length >= Console.WindowWidth)
                        {
                            lineContent = lineContent.Substring(0, Console.WindowWidth - 1);
                        }
                        
                        Console.Write(lineContent);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Handle console boundary issues gracefully
                        break;
                    }
                }
                
                // Position cursor correctly
                try
                {
                    var targetTop = startTop + currentLineIndex;
                    var prompt = currentLineIndex == 0 ? basePrompt : continuationPrompt;
                    var targetLeft = prompt.Length + cursorPosition;
                    
                    // Ensure cursor position is within bounds
                    if (targetTop < Console.BufferHeight && targetLeft < Console.WindowWidth)
                    {
                        Console.SetCursorPosition(targetLeft, targetTop);
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Fallback to a safe position
                    try
                    {
                        Console.SetCursorPosition(0, Math.Min(startTop + currentLineIndex, Console.BufferHeight - 1));
                    }
                    catch
                    {
                        // Ultimate fallback - just continue
                    }
                }
            }
            catch (Exception)
            {
                // If all else fails, don't crash the REPL
                // Just continue without redrawing
            }
        }

        /// <summary>
        /// Clears the multi line input using the specified line count
        /// </summary>
        /// <param name="lineCount">The line count</param>
        /// <param name="startTop">The start top</param>
        private void ClearMultiLineInput(int lineCount, int startTop)
        {
            try
            {
                for (int i = 0; i < lineCount && (startTop + i) < Console.BufferHeight; i++)
                {
                    Console.SetCursorPosition(0, startTop + i);
                    Console.Write(new string(' ', Math.Min(Console.WindowWidth - 1, 120)));
                }
                Console.SetCursorPosition(0, startTop);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Ignore positioning errors
            }
        }

        /// <summary>
        /// Gets the current prompt
        /// </summary>
        /// <returns>The string</returns>
        private string GetCurrentPrompt()
        {
            // This should match the prompt being used in ReadMultiLineInput
            // We'll need to track this better, but for now use a simple heuristic
            if (Console.CursorLeft > 10) // Likely a continuation prompt
                return "    ...> ";
            else
                return "μhigh> ";
        }

        /// <summary>
        /// Determines if the input is incomplete and needs more lines (e.g., unclosed braces or parentheses).
        /// </summary>
        private bool NeedsMoreInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;
                
            int paren = 0, brace = 0, bracket = 0;
            bool inString = false, inChar = false, escape = false;

            foreach (char c in input)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (c == '\\')
                {
                    escape = true;
                    continue;
                }
                if (inString)
                {
                    if (c == '"') inString = false;
                    continue;
                }
                if (inChar)
                {
                    if (c == '\'') inChar = false;
                    continue;
                }
                if (c == '"') { inString = true; continue; }
                if (c == '\'') { inChar = true; continue; }
                if (c == '(') paren++;
                if (c == ')') paren--;
                if (c == '{') brace++;
                if (c == '}') brace--;
                if (c == '[') bracket++;
                if (c == ']') bracket--;
            }
            
            // If any are unclosed, need more input
            return paren > 0 || brace > 0 || bracket > 0;
        }

       
        /// <summary>
        /// Redraws the current line using the specified input
        /// </summary>
        /// <param name="input">The input</param>
        /// <param name="cursorPosition">The cursor position</param>
        private void RedrawCurrentLine(string input, int cursorPosition)
        {
            var prompt = GetCurrentPrompt();
            var currentLeft = Console.CursorLeft;
            var currentTop = Console.CursorTop;
            
            // Move to beginning of current line
            Console.SetCursorPosition(0, currentTop);
            
            // Clear the line
            Console.Write(new string(' ', Console.WindowWidth - 1));
            
            // Move back to beginning
            Console.SetCursorPosition(0, currentTop);
            
            // Write prompt and input
            Console.Write(prompt + input);
            
            // Position cursor correctly
            Console.SetCursorPosition(prompt.Length + cursorPosition, currentTop);
        }

        /// <summary>
        /// Initializes the scripting environment
        /// </summary>
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

        /// <summary>
        /// Processes the input using the specified input
        /// </summary>
        /// <param name="input">The input</param>
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

        /// <summary>
        /// Executes the as c sharp using the specified code
        /// </summary>
        /// <param name="code">The code</param>
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

        /// <summary>
        /// Extracts the executable code using the specified generated c sharp
        /// </summary>
        /// <param name="generatedCSharp">The generated sharp</param>
        /// <returns>The string</returns>
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

                // Detect the start of Main, even if { is on the same line
                if (!inMainMethod && trimmedLine.Contains("static void Main"))
                {
                    inMainMethod = true;
                    // If { is on the same line, increment braceCount
                    var openBraceIndex = trimmedLine.IndexOf('{');
                    if (openBraceIndex >= 0)
                    {
                        braceCount++;
                        // If there's code after the opening brace on the same line, extract it
                        var codeAfterBrace = trimmedLine.Substring(openBraceIndex + 1).Trim();
                        if (!string.IsNullOrEmpty(codeAfterBrace) && codeAfterBrace != "}")
                        {
                            extractedLines.Add(codeAfterBrace);
                        }
                    }
                    continue;
                }

                if (inMainMethod)
                {
                    // Count braces on this line
                    var openBraces = line.Count(c => c == '{');
                    var closeBraces = line.Count(c => c == '}');
                    
                    braceCount += openBraces;
                    braceCount -= closeBraces;

                    // Only add lines that are inside the Main method body
                    if (braceCount > 0 && !string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        // Exclude standalone opening/closing braces
                        if (trimmedLine != "{" && trimmedLine != "}")
                        {
                            extractedLines.Add(trimmedLine);
                        }
                    }

                    // If we've closed all opened braces, we're done
                    if (braceCount == 0)
                        break;
                }
            }

            return string.Join("\n", extractedLines);
        }

        /// <summary>
        /// Saves the generated c sharp using the specified code
        /// </summary>
        /// <param name="code">The code</param>
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

        /// <summary>
        /// Handles the command using the specified command
        /// </summary>
        /// <param name="command">The command</param>
        /// <returns>A task containing the bool</returns>
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

        /// <summary>
        /// Shows the help
        /// </summary>
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
            Console.WriteLine("Keyboard Shortcuts:");
            Console.WriteLine("  Ctrl+Enter     - Add a newline (continue input)");
            Console.WriteLine("  Enter          - Execute the input");
            Console.WriteLine("  Escape         - Cancel current input");
            Console.WriteLine("  Ctrl+C         - Cancel current input");
            Console.WriteLine("  ←/→ arrows     - Move cursor left/right");
            Console.WriteLine("  ↑/↓ arrows     - Move up/down between lines");
            Console.WriteLine("  Home/End       - Move to beginning/end of line");
            Console.WriteLine("  Ctrl+Home/End  - Move to beginning/end of input");
            Console.WriteLine("  PageUp/Down    - Jump to first/last line");
            Console.WriteLine("  Backspace      - Delete character before cursor");
            Console.WriteLine("  Delete         - Delete character at cursor");
            Console.WriteLine("  Tab            - Insert 4 spaces");
            Console.WriteLine("  Ctrl+A         - Go to beginning of line");
            Console.WriteLine("  Ctrl+E         - Go to end of line");
            Console.WriteLine();
            Console.WriteLine("Multi-line editing:");
            Console.WriteLine("  - Use ↑/↓ arrows to navigate between lines");
            Console.WriteLine("  - Backspace at line start merges with previous line");
            Console.WriteLine("  - Delete at line end merges with next line");
            Console.WriteLine("  - Empty line with Enter exits multi-line mode");
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

        /// <summary>
        /// Resets the session
        /// </summary>
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

        /// <summary>
        /// Shows the variables
        /// </summary>
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

        /// <summary>
        /// Shows the history
        /// </summary>
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

        /// <summary>
        /// Shows the generated code
        /// </summary>
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

        /// <summary>
        /// Saves the session using the specified filename
        /// </summary>
        /// <param name="filename">The filename</param>
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

        /// <summary>
        /// Loads the session using the specified filename
        /// </summary>
        /// <param name="filename">The filename</param>
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
