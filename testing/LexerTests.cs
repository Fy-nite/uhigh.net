using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Testing
{
    public class LexerTests
    {
        private Lexer.Lexer CreateLexer(string source)
        {
            var diagnostics = new DiagnosticsReporter();
            return new Lexer.Lexer(source, diagnostics);
        }

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

        [Test]
        public void TestStringLiterals()
        {
            var lexer = CreateLexer("\"Hello, World!\"");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(2, tokens.Count); // string, EOF
            Assert.AreEqual(TokenType.String, tokens[0].Type);
            Assert.AreEqual("Hello, World!", tokens[0].Value);
        }

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

        [Test]
        public void TestIncrementDecrement()
        {
            var lexer = CreateLexer("++ --");
            var tokens = lexer.Tokenize();

            Assert.AreEqual(3, tokens.Count); // ++, --, EOF
            Assert.AreEqual(TokenType.Increment, tokens[0].Type);
            Assert.AreEqual(TokenType.Decrement, tokens[1].Type);
        }

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
    }
}
