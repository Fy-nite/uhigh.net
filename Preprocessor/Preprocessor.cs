using System.Collections.Generic;
using System.Text;

namespace uhigh.Net.Preprocessor
{
    public static class Preprocessor
    {
        private static readonly HashSet<string> _defines = new HashSet<string>();

        /// <summary>
        /// Clears all defined preprocessor symbols.
        /// </summary>
        public static void ClearDefines()
        {
            _defines.Clear();
        }

        /// <summary>
        /// Processes the given source code, applying preprocessor directives.
        /// </summary>
        /// <param name="source">The source code to process.</param>
        /// <returns>The processed source code.</returns>
        public static string Process(string source)
        {
            var output = new StringBuilder();
            var lines = source.Split('\n');
            // Stack of (parentInclude, branchTaken)
            var condStack = new Stack<(bool parentInclude, bool branchTaken)>();
            bool include = true;
            bool branchTaken = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                var line = rawLine.Trim();

                // #define SYMBOL
                if (line.StartsWith("#define "))
                {
                    var symbol = line.Substring(8).Trim();
                    _defines.Add(symbol);
                    continue;
                }
                // #undef SYMBOL
                else if (line.StartsWith("#undef "))
                {
                    var symbol = line.Substring(7).Trim();
                    _defines.Remove(symbol);
                    continue;
                }
                // #ifdef SYMBOL
                else if (line.StartsWith("#ifdef "))
                {
                    var symbol = line.Substring(7).Trim();
                    condStack.Push((include, branchTaken));
                    include = include && _defines.Contains(symbol);
                    branchTaken = include;
                }
                // #ifndef SYMBOL
                else if (line.StartsWith("#ifndef "))
                {
                    var symbol = line.Substring(8).Trim();
                    condStack.Push((include, branchTaken));
                    include = include && !_defines.Contains(symbol);
                    branchTaken = include;
                }
                // #if SYMBOL
                else if (line.StartsWith("#if "))
                {
                    var symbol = line.Substring(4).Trim();
                    condStack.Push((include, branchTaken));
                    include = include && _defines.Contains(symbol);
                    branchTaken = include;
                }
                // #elif SYMBOL
                else if (line.StartsWith("#elif "))
                {
                    if (condStack.Count > 0)
                    {
                        var (parentInclude, prevBranchTaken) = condStack.Peek();
                        if (!prevBranchTaken && parentInclude)
                        {
                            var symbol = line.Substring(6).Trim();
                            include = parentInclude && _defines.Contains(symbol);
                            branchTaken = include;
                        }
                        else
                        {
                            include = false;
                        }
                    }
                }
                // #else
                else if (line.StartsWith("#else"))
                {
                    if (condStack.Count > 0)
                    {
                        var (parentInclude, prevBranchTaken) = condStack.Peek();
                        if (!prevBranchTaken && parentInclude)
                        {
                            include = parentInclude;
                            branchTaken = true;
                        }
                        else
                        {
                            include = false;
                        }
                    }
                }
                // #endif
                else if (line.StartsWith("#endif"))
                {
                    if (condStack.Count > 0)
                    {
                        var (parentInclude, _) = condStack.Pop();
                        include = parentInclude;
                        branchTaken = false;
                    }
                }
                // #error MESSAGE
                else if (line.StartsWith("#error "))
                {
                    if (include)
                        throw new Exception("Preprocessor error: " + line.Substring(7).Trim());
                }
                // #warning MESSAGE
                else if (line.StartsWith("#warning "))
                {
                    if (include)
                        Console.WriteLine("Preprocessor warning: " + line.Substring(9).Trim());
                }
                else if (include)
                {
                    output.AppendLine(rawLine);
                }
            }
            return output.ToString();
        }

        /// <summary>
        /// Maps a target language name to a preprocessor define symbol.
        /// </summary>
        public static string TargetLanguageToDefine(string target)
        {
            return target.ToLowerInvariant() switch
            {
                "csharp" or "cs" => "CSHARP",
                "javascript" or "js" => "JAVASCRIPT",
                "cpp" or "c++" => "CPP",
                "llvm" => "LLVM",
                "vala" => "VALA",
                "java" or "java8" or "java11" or "java17" or "java21" => "JAVA",
                "python" or "py" => "PYTHON",
                _ => target.ToUpperInvariant()
            };
        }

        /// <summary>
        /// Sets the preprocessor symbol for the target language (for conditional compilation).
        /// </summary>
        public static void SetTargetLanguage(string target)
        {
            _defines.Add(TargetLanguageToDefine(target));
        }
    }
}