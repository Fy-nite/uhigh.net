using System.Collections.Generic;
using System.Text;

namespace uhigh.Net.Preprocessor
{
    public class Preprocessor
    {
        private readonly HashSet<string> _defines;
        public Preprocessor(IEnumerable<string> defines)
        {
            _defines = new HashSet<string>(defines);
        }

        public string Process(string source)
        {
            var output = new StringBuilder();
            var lines = source.Split('\n');
            var stack = new Stack<bool>();
            bool include = true;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("#ifdef "))
                {
                    var symbol = line.Substring(7).Trim();
                    stack.Push(include);
                    include = include && _defines.Contains(symbol);
                }
                else if (line.StartsWith("#ifndef "))
                {
                    var symbol = line.Substring(8).Trim();
                    stack.Push(include);
                    include = include && !_defines.Contains(symbol);
                }
                else if (line.StartsWith("#else"))
                {
                    if (stack.Count > 0)
                    {
                        var prev = stack.Pop();
                        stack.Push(prev);
                        include = prev && !include;
                    }
                }
                else if (line.StartsWith("#endif"))
                {
                    if (stack.Count > 0)
                        include = stack.Pop();
                }
                else if (include)
                {
                    output.AppendLine(rawLine);
                }
            }
            return output.ToString();
        }
    }
}
