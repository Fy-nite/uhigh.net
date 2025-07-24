using uhigh.Net.Parser;
using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;
using System;

namespace DebugTest
{
    public class DebugPostfixMatch
    {
        public static void Main()
        {
            // Test with first var and match
            var source = @"
var cmd = ""test""
match cmd {
    ""help"" => print(""Help"")
}";

            var diagnostics = new DiagnosticsReporter();
            var lexer = new Lexer(source, diagnostics);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens, diagnostics);
            var program = parser.Parse();

            Console.WriteLine("Tokens:");
            for (int i = 0; i < tokens.Count; i++)
            {
                Console.WriteLine($"{i}: {tokens[i].Type} = '{tokens[i].Value}' at {tokens[i].Line}:{tokens[i].Column}");
            }
        }
    }
}