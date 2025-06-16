using uhigh.Net.CodeGen;
using uhigh.Net.Parser;
using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Testing
{
    public class CodeGenTests
    {
        private string GenerateCSharp(string source)
        {
            var diagnostics = new DiagnosticsReporter();
            var lexer = new Lexer.Lexer(source, diagnostics);
            var tokens = lexer.Tokenize();
            var parser = new Parser.Parser(tokens, diagnostics);
            var ast = parser.Parse();
            var generator = new CSharpGenerator();
            return generator.Generate(ast, diagnostics);
        }

        [Test]
        public void TestSimpleVariableDeclaration()
        {
            var result = GenerateCSharp("var x = 42");
            
            Assert.IsTrue(result.Contains("var x = 42;"));
            Assert.IsTrue(result.Contains("namespace Generated"));
            Assert.IsTrue(result.Contains("public class Program"));
        }

        [Test]
        public void TestFunctionDeclaration()
        {
            var result = GenerateCSharp("func add(x: int, y: int): int { return x + y }");
            
            Assert.IsTrue(result.Contains("public static int add(int x, int y)"));
            Assert.IsTrue(result.Contains("return x + y;"));
        }

        [Test]
        public void TestClassGeneration()
        {
            var result = GenerateCSharp(@"
                public class Person {
                    private field name: string
                    public func getName(): string {
                        return this.name
                    }
                }");
            
            Assert.IsTrue(result.Contains("public class Person"));
            Assert.IsTrue(result.Contains("private string name;"));
            Assert.IsTrue(result.Contains("public string getName()"));
            Assert.IsTrue(result.Contains("return this.name;"));
        }

        [Test]
        public void TestNamespaceGeneration()
        {
            var result = GenerateCSharp(@"
                namespace MyApp {
                    class TestClass {}
                }");
            
            Assert.IsTrue(result.Contains("namespace MyApp"));
            Assert.IsTrue(result.Contains("public class TestClass"));
        }

        [Test]
        public void TestConstructorCall()
        {
            var result = GenerateCSharp("var person = Person(\"John\", 25)");
            
            Assert.IsTrue(result.Contains("var person = new Person(\"John\", 25);"));
        }

        [Test]
        public void TestMethodCall()
        {
            var result = GenerateCSharp("Console.WriteLine(\"Hello\")");
            
            Assert.IsTrue(result.Contains("Console.WriteLine(\"Hello\");"));
        }

        [Test]
        public void TestBinaryExpressions()
        {
            var result = GenerateCSharp("var result = x + y * 2");
            
            Assert.IsTrue(result.Contains("var result = x + y * 2;"));
        }

        [Test]
        public void TestIfStatement()
        {
            var result = GenerateCSharp(@"
                if x > 5 {
                    print(""greater"")
                } else {
                    print(""less or equal"")
                }");
            
            Assert.IsTrue(result.Contains("if (x > 5)"));
            Assert.IsTrue(result.Contains("else"));
            Assert.IsTrue(result.Contains("print(\"greater\");"));
            Assert.IsTrue(result.Contains("print(\"less or equal\");"));
        }

        [Test]
        public void TestWhileLoop()
        {
            var result = GenerateCSharp(@"
                while i < 10 {
                    print(i)
                    i++
                }");
            
            Assert.IsTrue(result.Contains("while (i < 10)"));
            Assert.IsTrue(result.Contains("print(i);"));
            Assert.IsTrue(result.Contains("i++;"));
        }

        [Test]
        public void TestBuiltInFunctions()
        {
            var result = GenerateCSharp("func main() { print(\"Hello\") }");
            
            Assert.IsTrue(result.Contains("public static void print(object value) => Console.WriteLine(value);"));
            Assert.IsTrue(result.Contains("public static string input() => Console.ReadLine() ?? \"\";"));
        }

        [Test]
        public void TestAttributeSkipping()
        {
            var result = GenerateCSharp(@"
                [dotnetfunc]
                func Console.WriteLine(message: string): void");
            
            // Should not contain the function body since it has [dotnetfunc]
            Assert.IsFalse(result.Contains("public static void Console.WriteLine"));
        }

        [Test]
        public void TestExternalAttributeSkipping()
        {
            var result = GenerateCSharp(@"
                [external]
                func Console.WriteLine(message: string): void");
            
            // Should not contain the function body since it has [external]
            Assert.IsFalse(result.Contains("public static void Console.WriteLine"));
        }

        [Test]
        public void TestExternalClassSkipping()
        {
            var result = GenerateCSharp(@"
                [external]
                class ExternalLibrary {
                    func someMethod(): void
                }");
            
            // Should not contain the class since it has [external]
            Assert.IsFalse(result.Contains("class ExternalLibrary"));
        }

        [Test]
        public void TestTypeConversion()
        {
            var result = GenerateCSharp(@"
                func test(a: int, b: float, c: string, d: bool): void {
                    // test function
                }");
            
            Assert.IsTrue(result.Contains("public static void test(int a, double b, string c, bool d)"));
        }

        [Test]
        public void TestMatchExpression()
        {
            var result = GenerateCSharp(@"
                var result = cmd match {
                    ""help"" => ""Showing help"",
                    ""exit"" => ""Goodbye"", 
                    _ => ""Unknown""
                }");
            
            Assert.IsTrue(result.Contains("cmd switch {"));
            Assert.IsTrue(result.Contains("\"help\" => \"Showing help\""));
            Assert.IsTrue(result.Contains("_ => \"Unknown\""));
        }

        [Test]
        public void TestMatchWithMultiplePatterns()
        {
            var result = GenerateCSharp(@"
                var size = num match {
                    1, 2, 3 => ""small"",
                    _ => ""large""
                }");
            
            Assert.IsTrue(result.Contains("1 or 2 or 3 => \"small\""));
        }
    }
}
