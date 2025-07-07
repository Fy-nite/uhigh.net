using System;
using System.Threading;
using uhigh.Net.Parser;
using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;
using uhigh.Net.Testing;

namespace uhigh.Net.Testing
{
    /// <summary>
    /// Tests for attribute parsing and handling
    /// </summary>
    public class AttributeTests
    {
        [Setup]
        public void Setup()
        {
            TestRunner.CurrentContext["TestId"] = Guid.NewGuid().ToString();
        }

        private Program ParseSource(string source)
        {
            var diagnostics = new DiagnosticsReporter();
            var lexer = new Lexer.Lexer(source, diagnostics);
            var tokens = lexer.Tokenize();
            var parser = new Parser.Parser(tokens, diagnostics);
            return parser.Parse();
        }

        [Test]
        public void TestExternalAttribute()
        {
            var program = ParseSource(@"
                [external]
                func Console.WriteLine(message: string): void");

            Assert.AreEqual(1, program.Statements.Count);
            var funcDecl = (FunctionDeclaration)program.Statements[0];
            
            Assert.IsNotNull(funcDecl.Attributes);
            Assert.AreEqual(1, funcDecl.Attributes.Count);
            Assert.AreEqual("external", funcDecl.Attributes[0].Name);
            Assert.IsTrue(funcDecl.Attributes[0].IsExternal);
        }

        [Test]
        public void TestAttributeWithArguments()
        {
            var program = ParseSource(@"
                [TestAttribute(""test"", 42)]
                func testFunction(): void {}");

            var funcDecl = (FunctionDeclaration)program.Statements[0];
            
            Assert.IsNotNull(funcDecl.Attributes);
            Assert.AreEqual(1, funcDecl.Attributes.Count);
            Assert.AreEqual("TestAttribute", funcDecl.Attributes[0].Name);
            Assert.AreEqual(2, funcDecl.Attributes[0].Arguments.Count);
        }

        [Test]
        public void TestMultipleAttributes()
        {
            var program = ParseSource(@"
                [external]
                [deprecated]
                func oldFunction(): void");

            var funcDecl = (FunctionDeclaration)program.Statements[0];
            
            Assert.IsNotNull(funcDecl.Attributes);
            Assert.AreEqual(2, funcDecl.Attributes.Count);
            Assert.AreEqual("external", funcDecl.Attributes[0].Name);
            Assert.AreEqual("deprecated", funcDecl.Attributes[1].Name);
        }

        [Teardown]
        public void Teardown()
        {
            TestRunner.CurrentContext["TestEndTime"] = DateTime.Now;
        }
    }
}
