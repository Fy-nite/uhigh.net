using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Testing
{
    /// <summary>
    /// The lexer tests class
    /// </summary>
    public class LexerTests
    {
        /// <summary>
        /// Creates the lexer using the specified source
        /// </summary>
        /// <param name="source">The source</param>
        /// <returns>The lexer lexer</returns>
        private Lexer.Lexer CreateLexer(string source)
        {
            var diagnostics = new DiagnosticsReporter();
            return new Lexer.Lexer(source, diagnostics);
        }

        /// <summary>
        /// Tests that test basic tokens
        /// </summary>
        [Test]
        public void TestBasicTokens()
        {
            var lexer = CreateLexer("var x = 42");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(5, tokens.Count); // var, x, =, 42, EOF
            Assert.AreEqual(TokenType.Var, tokens[0].Type);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual(TokenType.Assign, tokens[2].Type);
            Assert.AreEqual(TokenType.Number, tokens[3].Type);
            Assert.AreEqual(TokenType.EOF, tokens[4].Type);
        }

        /// <summary>
        /// Tests that test string literals
        /// </summary>
        [Test]
        public void TestStringLiterals()
        {
            var lexer = CreateLexer("\"Hello, World!\"");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(2, tokens.Count); // string, EOF
            Assert.AreEqual(TokenType.String, tokens[0].Type);
            Assert.AreEqual("Hello, World!", tokens[0].Value);
        }

        /// <summary>
        /// Tests that test number literals
        /// </summary>
        [Test]
        public void TestNumberLiterals()
        {
            var lexer = CreateLexer("42 3.14 0.5");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(4, tokens.Count); // 42, 3.14, 0.5, EOF
            Assert.AreEqual(TokenType.Number, tokens[0].Type);
            Assert.AreEqual("42", tokens[0].Value);
            Assert.AreEqual(TokenType.Number, tokens[1].Type);
            Assert.AreEqual("3.14", tokens[1].Value);
            Assert.AreEqual(TokenType.Number, tokens[2].Type);
            Assert.AreEqual("0.5", tokens[2].Value);
        }

        /// <summary>
        /// Tests version number parsing doesn't fail
        /// </summary>
        [Test]
        public void TestVersionNumberParsing()
        {
            var lexer = CreateLexer("1.0.0");
            var tokens = lexer.Tokenize();

            // Should tokenize as: 1.0, ., 0, EOF
            Assert.AreEqual(4, tokens.Count);
            Assert.AreEqual(TokenType.Number, tokens[0].Type);
            Assert.AreEqual("1.0", tokens[0].Value);
            Assert.AreEqual(TokenType.Dot, tokens[1].Type);
            Assert.AreEqual(TokenType.Number, tokens[2].Type);
            Assert.AreEqual("0", tokens[2].Value);
            Assert.AreEqual(TokenType.EOF, tokens[3].Type);
        }

        /// <summary>
        /// Tests that test keywords
        /// </summary>
        [Test]
        public void TestKeywords()
        {
            var lexer = CreateLexer("func if else while for return");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(7, tokens.Count); // 6 keywords + EOF
            Assert.AreEqual(TokenType.Func, tokens[0].Type);
            Assert.AreEqual(TokenType.If, tokens[1].Type);
            Assert.AreEqual(TokenType.Else, tokens[2].Type);
            Assert.AreEqual(TokenType.While, tokens[3].Type);
            Assert.AreEqual(TokenType.For, tokens[4].Type);
            Assert.AreEqual(TokenType.Return, tokens[5].Type);
        }

        /// <summary>
        /// Tests that test operators
        /// </summary>
        [Test]
        public void TestOperators()
        {
            var lexer = CreateLexer("+ - * / % == != < > <= >=");
            var tokens = lexer.Tokenize();

            var expectedTypes = new[]
            {
                TokenType.Plus, TokenType.Minus, TokenType.Multiply, TokenType.Divide, TokenType.Modulo,
                TokenType.Equal, TokenType.NotEqual, TokenType.Less, TokenType.Greater,
                TokenType.LessEqual, TokenType.GreaterEqual
            };

            Assert.AreEqual(expectedTypes.Length + 1, tokens.Count); // operators + EOF
            for (int i = 0; i < expectedTypes.Length; i++)
            {
                Assert.AreEqual(expectedTypes[i], tokens[i].Type);
            }
        }

        /// <summary>
        /// Tests that test generic types
        /// </summary>
        [Test]
        public void TestGenericTypes()
        {
            var lexer = CreateLexer("TimestampedEvent<string> Dictionary<string, int>");
            var tokens = lexer.Tokenize();

            // Should tokenize as: TimestampedEvent, <, string, >, Dictionary, <, string, ,, int, >, EOF
            Assert.IsTrue(tokens.Count >= 11);
            Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
            Assert.AreEqual("TimestampedEvent", tokens[0].Value);
            Assert.AreEqual(TokenType.Less, tokens[1].Type);
            Assert.AreEqual(TokenType.StringType, tokens[2].Type);
            Assert.AreEqual(TokenType.Greater, tokens[3].Type);
        }

        /// <summary>
        /// Tests that test comments
        /// </summary>
        [Test]
        public void TestComments()
        {
            var lexer = CreateLexer("var x = 42 // This is a comment\nvar y = 10");
            var tokens = lexer.Tokenize();

            // Comments should be skipped
            var tokenTypes = tokens.Select(t => t.Type).ToArray();
            Assert.DoesNotContain(tokenTypes, TokenType.Comment);
            
            // Should have: var, x, =, 42, var, y, =, 10, EOF
            Assert.AreEqual(9, tokens.Count);
        }

        /// <summary>
        /// Tests that test qualified identifiers
        /// </summary>
        [Test]
        public void TestQualifiedIdentifiers()
        {
            var lexer = CreateLexer("Console.WriteLine System.IO.File");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(3, tokens.Count); // Console.WriteLine, System.IO.File, EOF
            Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
            Assert.AreEqual("Console.WriteLine", tokens[0].Value);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual("System.IO.File", tokens[1].Value);
        }

        /// <summary>
        /// Tests that test punctuation
        /// </summary>
        [Test]
        public void TestPunctuation()
        {
            var lexer = CreateLexer("() {} [] , ; :");
            var tokens = lexer.Tokenize();

            var expectedTypes = new[]
            {
                TokenType.LeftParen, TokenType.RightParen,
                TokenType.LeftBrace, TokenType.RightBrace,
                TokenType.LeftBracket, TokenType.RightBracket,
                TokenType.Comma, TokenType.Semicolon, TokenType.Colon
            };

            Assert.AreEqual(expectedTypes.Length + 1, tokens.Count); // punctuation + EOF
            for (int i = 0; i < expectedTypes.Length; i++)
            {
                Assert.AreEqual(expectedTypes[i], tokens[i].Type);
            }
        }

        /// <summary>
        /// Tests that test line and column tracking
        /// </summary>
        [Test]
        public void TestLineAndColumnTracking()
        {
            var lexer = CreateLexer("var x\n  = 42");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(1, tokens[0].Line); // var
            Assert.AreEqual(1, tokens[0].Column);
            Assert.AreEqual(1, tokens[1].Line); // x
            Assert.AreEqual(5, tokens[1].Column);
            Assert.AreEqual(2, tokens[2].Line); // =
            Assert.AreEqual(3, tokens[2].Column);
            Assert.AreEqual(2, tokens[3].Line); // 42
            Assert.AreEqual(5, tokens[3].Column);
        }

        /// <summary>
        /// Tests that test modifier keywords
        /// </summary>
        [Test]
        public void TestModifierKeywords()
        {
            var lexer = CreateLexer("public private static readonly");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(5, tokens.Count); // 4 modifiers + EOF
            Assert.AreEqual(TokenType.Public, tokens[0].Type);
            Assert.AreEqual(TokenType.Private, tokens[1].Type);
            Assert.AreEqual(TokenType.Static, tokens[2].Type);
            Assert.AreEqual(TokenType.Readonly, tokens[3].Type);
        }

        /// <summary>
        /// Tests that test increment decrement
        /// </summary>
        [Test]
        public void TestIncrementDecrement()
        {
            var lexer = CreateLexer("++ --");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(3, tokens.Count); // ++, --, EOF
            Assert.AreEqual(TokenType.Increment, tokens[0].Type);
            Assert.AreEqual(TokenType.Decrement, tokens[1].Type);
        }

        /// <summary>
        /// Tests that test assignment operators
        /// </summary>
        [Test]
        public void TestAssignmentOperators()
        {
            var lexer = CreateLexer("+= -= *= /=");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(5, tokens.Count); // 4 assignment ops + EOF
            Assert.AreEqual(TokenType.PlusAssign, tokens[0].Type);
            Assert.AreEqual(TokenType.MinusAssign, tokens[1].Type);
            Assert.AreEqual(TokenType.MultiplyAssign, tokens[2].Type);
            Assert.AreEqual(TokenType.DivideAssign, tokens[3].Type);
        }

        /// <summary>
        /// Tests that test complex expression
        /// </summary>
        [Test]
        public void TestComplexExpression()
        {
            var lexer = CreateLexer("func calculate(x: int, y: float): double { return x + y * 2.5 }");
            var tokens = lexer.Tokenize();

            Assert.IsTrue(tokens.Count > 15); // Should have many tokens
            Assert.AreEqual(TokenType.Func, tokens[0].Type);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual("calculate", tokens[1].Value);
            Assert.AreEqual(TokenType.LeftParen, tokens[2].Type);
            Assert.AreEqual(TokenType.EOF, tokens.Last().Type);
        }

        /// <summary>
        /// Tests that test match keywords
        /// </summary>
        [Test]
        public void TestMatchKeywords()
        {
            var lexer = CreateLexer("match => _");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(4, tokens.Count); // match, =>, _, EOF
            Assert.AreEqual(TokenType.Match, tokens[0].Type);
            Assert.AreEqual(TokenType.Arrow, tokens[1].Type);
            Assert.AreEqual(TokenType.Underscore, tokens[2].Type);
        }

        /// <summary>
        /// Tests that test range keyword
        /// </summary>
        [Test]
        public void TestRangeKeyword()
        {
            var lexer = CreateLexer("range(10)");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(5, tokens.Count); // range, (, 10, ), EOF
            Assert.AreEqual(TokenType.Range, tokens[0].Type);
            Assert.AreEqual(TokenType.LeftParen, tokens[1].Type);
            Assert.AreEqual(TokenType.Number, tokens[2].Type);
            Assert.AreEqual(TokenType.RightParen, tokens[3].Type);
        }

        /// <summary>
        /// Tests that test for in syntax
        /// </summary>
        [Test]
        public void TestForInSyntax()
        {
            var lexer = CreateLexer("for var i in range(5)");
            var tokens = lexer.Tokenize();

            var expectedTypes = new[]
            {
                TokenType.For, TokenType.Var, TokenType.Identifier, TokenType.In,
                TokenType.Range, TokenType.LeftParen, TokenType.Number, TokenType.RightParen
            };

            Assert.AreEqual(expectedTypes.Length + 1, tokens.Count); // +1 for EOF
            for (int i = 0; i < expectedTypes.Length; i++)
            {
                Assert.AreEqual(expectedTypes[i], tokens[i].Type);
            }
        }

        /// <summary>
        /// Tests that test array type syntax
        /// </summary>
        [Test]
        public void TestArrayTypeSyntax()
        {
            var lexer = CreateLexer("string[] int[] List<string>[]");
            var tokens = lexer.Tokenize();

            // Should now tokenize as: string[], int[], List, <, string, >, [], EOF
            Assert.IsTrue(tokens.Count >= 8);
            Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
            Assert.AreEqual("string[]", tokens[0].Value);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual("int[]", tokens[1].Value);
            Assert.AreEqual(TokenType.Identifier, tokens[2].Type);
            Assert.AreEqual("List", tokens[2].Value);
        }

        /// <summary>
        /// Tests that test generic array types
        /// </summary>
        [Test]
        public void TestGenericArrayTypes()
        {
            var lexer = CreateLexer("List<string>[] Dictionary<string, int>[]");
            var tokens = lexer.Tokenize();

            // Should properly tokenize generic types with array brackets
            Assert.IsTrue(tokens.Count >= 12);
            Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
            Assert.AreEqual("List", tokens[0].Value);
            Assert.AreEqual(TokenType.Less, tokens[1].Type);
            Assert.AreEqual(TokenType.StringType, tokens[2].Type);
            Assert.AreEqual(TokenType.Greater, tokens[3].Type);
            Assert.AreEqual(TokenType.LeftBracket, tokens[4].Type);
            Assert.AreEqual(TokenType.RightBracket, tokens[5].Type);
        }

        /// <summary>
        /// Tests that test attribute tokenization
        /// </summary>
        [Test]
        public void TestAttributeTokenization()
        {
            var lexer = CreateLexer(@"[PrintAttribute(""message"")]");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(7, tokens.Count);
            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual("PrintAttribute", tokens[1].Value);
            Assert.AreEqual(TokenType.LeftParen, tokens[2].Type);
            Assert.AreEqual(TokenType.String, tokens[3].Type);
            Assert.AreEqual("message", tokens[3].Value);
            Assert.AreEqual(TokenType.RightParen, tokens[4].Type);
            Assert.AreEqual(TokenType.RightBracket, tokens[5].Type);
            Assert.AreEqual(TokenType.EOF, tokens[6].Type);
        }

        /// <summary>
        /// Tests simple attribute tokenization
        /// </summary>
        [Test]
        public void TestSimpleAttributeTokenization()
        {
            var lexer = CreateLexer(@"[external]");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(4, tokens.Count);
            Assert.AreEqual(TokenType.LeftBracket, tokens[0].Type);
            Assert.AreEqual(TokenType.Identifier, tokens[1].Type);
            Assert.AreEqual("external", tokens[1].Value);
            Assert.AreEqual(TokenType.RightBracket, tokens[2].Type);
            Assert.AreEqual(TokenType.EOF, tokens[3].Type);
        }
    }
}
