using uhigh.Net.CodeGen;
using uhigh.Net.Parser;
using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Testing
{
    /// <summary>
    /// The code gen tests class
    /// </summary>
    public class CodeGenTests
    {
        /// <summary>
        /// Generates the c sharp using the specified source
        /// </summary>
        /// <param name="source">The source</param>
        /// <returns>The string</returns>
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

        /// <summary>
        /// Tests that test simple variable declaration
        /// </summary>
        [Test]
        public void TestSimpleVariableDeclaration()
        {
            var result = GenerateCSharp("var x = 42");
            
            Assert.IsTrue(result.Contains("var x = 42;"));
            Assert.IsTrue(result.Contains("namespace Generated"));
            Assert.IsTrue(result.Contains("public class Program"));
        }

        /// <summary>
        /// Tests that test function declaration
        /// </summary>
        [Test]
        public void TestFunctionDeclaration()
        {
            var result = GenerateCSharp("func add(x: int, y: int): int { return x + y }");
            
            Assert.IsTrue(result.Contains("public static int add(int x, int y)"));
            Assert.IsTrue(result.Contains("return x + y;"));
        }

        /// <summary>
        /// Tests that test class generation
        /// </summary>
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

        /// <summary>
        /// Tests that test namespace generation
        /// </summary>
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

        /// <summary>
        /// Tests that test constructor call
        /// </summary>
        [Test]
        public void TestConstructorCall()
        {
            var result = GenerateCSharp("var person = Person(\"John\", 25)");
            
            Assert.IsTrue(result.Contains("var person = new Person(\"John\", 25);"));
            // Verify it's all on one line (no unexpected newlines)
            Assert.IsFalse(result.Contains("new\nPerson"));
            Assert.IsFalse(result.Contains("new \nPerson"));
        }

        /// <summary>
        /// Tests that test method call
        /// </summary>
        [Test]
        public void TestMethodCall()
        {
            var result = GenerateCSharp("Console.WriteLine(\"Hello\")");
            
            Assert.IsTrue(result.Contains("Console.WriteLine(\"Hello\");"));
        }

        /// <summary>
        /// Tests that test binary expressions
        /// </summary>
        [Test]
        public void TestBinaryExpressions()
        {
            var result = GenerateCSharp("var result = x + y * 2");
            
            Assert.IsTrue(result.Contains("var result = x + y * 2;"));
        }

        /// <summary>
        /// Tests that test if statement
        /// </summary>
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

        /// <summary>
        /// Tests that test while loop
        /// </summary>
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

        /// <summary>
        /// Tests that test built in functions
        /// </summary>
        [Test]
        public void TestBuiltInFunctions()
        {
            var result = GenerateCSharp("func main() { print(\"Hello\") }");
            
            Assert.IsTrue(result.Contains("public static void print(object value) => Console.WriteLine(value);"));
            Assert.IsTrue(result.Contains("public static string input() => Console.ReadLine() ?? \"\";"));
        }

        /// <summary>
        /// Tests that test attribute skipping
        /// </summary>
        [Test]
        public void TestAttributeSkipping()
        {
            var result = GenerateCSharp(@"
                [dotnetfunc]
                func Console.WriteLine(message: string): void");
            
            // Should not contain the function body since it has [dotnetfunc]
            Assert.IsFalse(result.Contains("public static void Console.WriteLine"));
        }

        /// <summary>
        /// Tests that test external attribute skipping
        /// </summary>
        [Test]
        public void TestExternalAttributeSkipping()
        {
            var result = GenerateCSharp(@"
                [external]
                func Console.WriteLine(message: string): void");
            
            // Should not contain the function body since it has [external]
            Assert.IsFalse(result.Contains("public static void Console.WriteLine"));
        }

        /// <summary>
        /// Tests that test external class skipping
        /// </summary>
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

        /// <summary>
        /// Tests that test type conversion
        /// </summary>
        [Test]
        public void TestTypeConversion()
        {
            var result = GenerateCSharp(@"
                func test(a: int, b: float, c: string, d: bool): void {
                    // test function
                }");
            
            Assert.IsTrue(result.Contains("public static void test(int a, double b, string c, bool d)"));
        }

        /// <summary>
        /// Tests that test match expression
        /// </summary>
        [Test]
        public void TestMatchExpression()
        {
            var result = GenerateCSharp(@"
                var result = cmd match {
                    ""help"" => ""Showing help"",
                    ""exit"" => ""Goodbye"", 
                    _ => ""Unknown""
                }");
            
            Assert.IsTrue(result.Contains("cmd switch"));
            Assert.IsTrue(result.Contains("\"help\" => \"Showing help\""));
            Assert.IsTrue(result.Contains("_ => \"Unknown\""));
        }

        /// <summary>
        /// Tests that test match expression with blocks
        /// </summary>
        [Test]
        public void TestMatchExpressionWithBlocks()
        {
            var result = GenerateCSharp(@"
                var result = cmd match {
                    ""help"" => {
                        print(""Showing help"")
                        return ""help displayed""
                    },
                    ""exit"" => ""Goodbye"", 
                    _ => {
                        print(""Unknown command: "" + cmd)
                        return ""error""
                    }
                }");
            
            Assert.IsTrue(result.Contains("cmd switch"));
            Assert.IsTrue(result.Contains("\"help\" => (() => {"));
            Assert.IsTrue(result.Contains("\"exit\" => \"Goodbye\""));
            Assert.IsTrue(result.Contains("_ => (() => {"));
        }

        /// <summary>
        /// Tests that test match statement with blocks
        /// </summary>
        [Test]
        public void TestMatchStatementWithBlocks()
        {
            var result = GenerateCSharp(@"
                cmd match {
                    ""help"" => {
                        print(""Showing help"")
                        showHelp()
                    },
                    ""exit"" => exitProgram(),
                    _ => {
                        print(""Unknown command: "" + cmd)
                        showError()
                    }
                }");
            
            Assert.IsTrue(result.Contains("switch (cmd)"));
            Assert.IsTrue(result.Contains("case \"help\":"));
            Assert.IsTrue(result.Contains("print(\"Showing help\")"));
            Assert.IsTrue(result.Contains("showHelp()"));
            Assert.IsTrue(result.Contains("case \"exit\":"));
            Assert.IsTrue(result.Contains("exitProgram()"));
            Assert.IsTrue(result.Contains("default:"));
            Assert.IsTrue(result.Contains("showError()"));
        }

        /// <summary>
        /// Tests that test match expression in assignment
        /// </summary>
        [Test]
        public void TestMatchExpressionInAssignment()
        {
            var result = GenerateCSharp(@"
                var message: string
                message = status match {
                    0 => ""OK"",
                    _ => ""Error""
                }");
            
            Assert.IsTrue(result.Contains("message = (status switch"));
            Assert.IsTrue(result.Contains("0 => \"OK\""));
            Assert.IsTrue(result.Contains("_ => \"Error\""));
        }

        
    }
}
