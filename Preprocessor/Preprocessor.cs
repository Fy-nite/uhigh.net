using System.Collections.Generic;
using System.Text;

namespace uhigh.Net.Preprocessor
{
    public static class Preprocessor
    {
        private static readonly HashSet<string> _defines = new HashSet<string>();

        /// <summary>
        /// Processes the given source code, applying preprocessor directives.
        /// </summary>
        /// <param name="source">The source code to process.</param>
        /// <returns>The processed source code.</returns>
        public static string Process(string source)
        {
            var output = new StringBuilder();
            var lines = source.Split('\n');
            var stack = new Stack<bool>();
            bool include = true;

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
                    stack.Push(include);
                    include = include && _defines.Contains(symbol);
                }
                // #ifndef SYMBOL
                else if (line.StartsWith("#ifndef "))
                {
                    var symbol = line.Substring(8).Trim();
                    stack.Push(include);
                    include = include && !_defines.Contains(symbol);
                }
                // #if SYMBOL
                else if (line.StartsWith("#if "))
                {
                    var symbol = line.Substring(4).Trim();
                    stack.Push(include);
                    include = include && _defines.Contains(symbol);
                }
                // #elif SYMBOL
                else if (line.StartsWith("#elif "))
                {
                    if (stack.Count > 0)
                    {
                        var prev = stack.Pop();
                        stack.Push(prev);
                        var symbol = line.Substring(6).Trim();
                        include = prev && _defines.Contains(symbol);
                    }
                }
                // #else
                else if (line.StartsWith("#else"))
                {
                    if (stack.Count > 0)
                    {
                        var prev = stack.Pop();
                        stack.Push(prev);
                        include = prev && !include;
                    }
                }
                // #endif
                else if (line.StartsWith("#endif"))
                {
                    if (stack.Count > 0)
                        include = stack.Pop();
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
                _ => target.ToUpperInvariant()
            };
        }
    }
}
