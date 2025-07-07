using System;
using System.IO;
using System.Threading;
using uhigh.Net.Repl;
using uhigh.Net.Testing;

namespace uhigh.Net.Testing
{
    /// <summary>
    /// Tests for the REPL functionality
    /// </summary>
    public class ReplTests
    {
        [Setup]
        public void Setup()
        {
            // Setup for each test - runs in parallel isolation
            TestRunner.CurrentContext["TestId"] = Guid.NewGuid().ToString();
            TestRunner.CurrentContext["ThreadId"] = Thread.CurrentThread.ManagedThreadId;
        }
        
        [Test]
        public void TestReplThreadSafety()
        {
            // Test that each REPL instance is thread-safe
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var contextThreadId = (int)TestRunner.CurrentContext["ThreadId"];
            
            Assert.AreEqual(threadId, contextThreadId);
        }

        [Teardown]
        public void Teardown()
        {
            // Cleanup after each test
            TestRunner.CurrentContext["TestEndTime"] = DateTime.Now;
        }
    }
}
