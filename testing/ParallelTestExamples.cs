using System.Threading;
using System.Threading.Tasks;

namespace uhigh.Net.Testing
{
    /// <summary>
    /// Examples of thread-safe test patterns
    /// </summary>
    public class ParallelTestExamples
    {
        private static int _sharedCounter = 0;
        private static readonly object _lockObject = new object();

        [Setup]
        public void Setup()
        {
            // Each test gets its own setup call - thread isolated
            TestRunner.CurrentContext["TestStartTime"] = DateTime.Now;
            TestRunner.CurrentContext["ThreadId"] = Thread.CurrentThread.ManagedThreadId;
        }

        [Test]
        public void TestThreadSafety()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var contextThreadId = (int)TestRunner.CurrentContext["ThreadId"];
            
            Assert.AreEqual(threadId, contextThreadId);
        }

        [Test]
        public void TestParallelExecution()
        {
            // This test should run in parallel with others
            var startTime = (DateTime)TestRunner.CurrentContext["TestStartTime"];
            var elapsed = DateTime.Now - startTime;
            
            Assert.IsTrue(elapsed.TotalMilliseconds >= 0);
            
            // Simulate some work
            Thread.Sleep(100);
        }

        [Test]
        public void TestSharedResourceAccess()
        {
            // Example of thread-safe shared resource access
            lock (_lockObject)
            {
                var before = _sharedCounter;
                _sharedCounter++;
                var after = _sharedCounter;
                
                Assert.AreEqual(before + 1, after);
            }
        }

        [Test]
        public void TestAsyncOperation()
        {
            // Tests can use async operations
            var task = Task.Run(() =>
            {
                Thread.Sleep(50);
                return 42;
            });

            var result = task.Result;
            Assert.AreEqual(42, result);
        }

        [Test]
        public void TestLongRunningOperation()
        {
            // This test should complete within the timeout
            var iterations = 1000;
            var sum = 0;
            
            for (int i = 0; i < iterations; i++)
            {
                sum += i;
            }
            
            Assert.AreEqual(iterations * (iterations - 1) / 2, sum);
        }

        [Test]
        public void TestIsolatedData()
        {
            // Each test should have isolated data
            var randomValue = new Random().Next(1000);
            TestRunner.CurrentContext["RandomValue"] = randomValue;
            
            Thread.Sleep(10); // Give other threads time to interfere (they shouldn't)
            
            var retrievedValue = (int)TestRunner.CurrentContext["RandomValue"];
            Assert.AreEqual(randomValue, retrievedValue);
        }

        [Teardown]
        public void Teardown()
        {
            // Clean up thread-local resources
            TestRunner.CurrentContext["TestEndTime"] = DateTime.Now;
        }
    }
}
