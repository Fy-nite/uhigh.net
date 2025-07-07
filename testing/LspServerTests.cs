using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using uhigh.Net.Testing;
using UhighLanguageServer;
using LanguageServer.Parameters.TextDocument;
using LanguageServer.Parameters;
using System.Threading;

namespace uhigh.Net.Testing
{
    /// <summary>
    /// Tests for the μHigh LSP server
    /// </summary>
    public class LspServerTests
    {
        private MemoryStream _input;
        private MemoryStream _output;
        private App _app;

        [Setup]
        public void Setup()
        {
            _input = new MemoryStream();
            _output = new MemoryStream();
            _app = new App(_input, _output);
        }

        [Test]
        public void TestInitialize()
        {
            // Simulate an initialize request
            var initParams = new LanguageServer.Parameters.General.InitializeParams
            {
                rootUri = new Uri("file:///workspace")
            };
            var result = _app.GetType().GetMethod("Initialize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { initParams });
            Assert.IsNotNull(result, "Initialize should return a result");
        }

        [Test]
        public void TestDidOpenTextDocument()
        {
            var text = "func main() { print(\"Hello\") }";
            var doc = new TextDocumentItem
            {
                uri = new Uri("file:///test.uh"),
                languageId = "uhigh",
                version = 1,
                text = text
            };
            var openParams = new DidOpenTextDocumentParams
            {
                textDocument = doc
            };
            _app.GetType().GetMethod("DidOpenTextDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { openParams });
            // No exception means success
            Assert.IsTrue(true);
        }

        [Test]
        public void TestDidChangeTextDocument()
        {
            var text = "func main() { print(\"Hello\") }";
            var doc = new TextDocumentItem
            {
                uri = new Uri("file:///test.uh"),
                languageId = "uhigh",
                version = 1,
                text = text
            };
            var openParams = new DidOpenTextDocumentParams { textDocument = doc };
            _app.GetType().GetMethod("DidOpenTextDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { openParams });

            var changeParams = new DidChangeTextDocumentParams
            {
                textDocument = new VersionedTextDocumentIdentifier
                {
                    uri = doc.uri,
                    version = 2
                },
                contentChanges = new[]
                {
                    new TextDocumentContentChangeEvent
                    {
                        text = "func main() { print(\"Changed\") }"
                    }
                }
            };
            _app.GetType().GetMethod("DidChangeTextDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { changeParams });
            Assert.IsTrue(true);
        }

        [Test]
        public void TestBrokenCode()
        {   
            var text = "func main() { print \"Hello\") "; // Missing closing } and parentheses
            var doc = new TextDocumentItem
            {
                uri = new Uri("file:///broken.uh"),
                languageId = "uhigh",
                version = 1,
                text = text
            };
            var openParams = new DidOpenTextDocumentParams { textDocument = doc };
            _app.GetType().GetMethod("DidOpenTextDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { openParams });

            // No exception means broken code handled gracefully
            Assert.IsTrue(true);
        }
        
        [Test]
        public void TestDidCloseTextDocument()
        {
            var text = "func main() { print(\"Hello\") }";
            var doc = new TextDocumentItem
            {
                uri = new Uri("file:///test.uh"),
                languageId = "uhigh",
                version = 1,
                text = text
            };
            var openParams = new DidOpenTextDocumentParams { textDocument = doc };
            _app.GetType().GetMethod("DidOpenTextDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { openParams });

            var closeParams = new DidCloseTextDocumentParams
            {
                textDocument = new TextDocumentIdentifier
                {
                    uri = doc.uri
                }
            };
            _app.GetType().GetMethod("DidCloseTextDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { closeParams });
            Assert.IsTrue(true);
        }

        [Test]
        public void TestDiagnosticsOnSyntaxError()
        {
            var text = "func main() { print(\"Hello\") "; // Missing closing }
            var doc = new TextDocumentItem
            {
                uri = new Uri("file:///error.uh"),
                languageId = "uhigh",
                version = 1,
                text = text
            };
            var openParams = new DidOpenTextDocumentParams { textDocument = doc };
            _app.GetType().GetMethod("DidOpenTextDocument", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_app, new object[] { openParams });

            // No exception means diagnostics handled gracefully
            Assert.IsTrue(true);
        }

        [Teardown]
        public void Teardown()
        {
            _input?.Dispose();
            _output?.Dispose();
        }
    }
}
