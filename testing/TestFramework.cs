using System.Diagnostics;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using uhigh.Net.Parser;
using uhigh.Net.Lexer;

namespace uhigh.Net.Testing
{
    /// <summary>
    /// The test status enum
    /// </summary>
    public enum TestStatus
    {
        /// <summary>
        /// The passed test status
        /// </summary>
        Passed,
        /// <summary>
        /// The failed test status
        /// </summary>
        Failed,
        /// <summary>
        /// The skipped test status
        /// </summary>
        Skipped,
        /// <summary>
        /// The error test status
        /// </summary>
        Error
    }

    /// <summary>
    /// The test result class
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Gets or sets the value of the test name
        /// </summary>
        public string TestName { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the passed
        /// </summary>
        public bool Passed { get; set; }
        /// <summary>
        /// Gets the value of the status
        /// </summary>
        public TestStatus Status => Passed ? TestStatus.Passed : TestStatus.Failed;
        /// <summary>
        /// Gets or sets the value of the error message
        /// </summary>
        public string? ErrorMessage { get; set; }
        /// <summary>
        /// Gets or sets the value of the duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        /// <summary>
        /// Gets or sets the value of the exception
        /// </summary>
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// The test suite class
    /// </summary>
    public class TestSuite
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the results
        /// </summary>
        public List<TestResult> Results { get; set; } = new();
        /// <summary>
        /// Gets the value of the passed count
        /// </summary>
        public int PassedCount => Results.Count(r => r.Passed);
        /// <summary>
        /// Gets the value of the failed count
        /// </summary>
        public int FailedCount => Results.Count(r => !r.Passed);
        /// <summary>
        /// Gets the value of the total count
        /// </summary>
        public int TotalCount => Results.Count;
        /// <summary>
        /// Gets the value of the total duration
        /// </summary>
        public TimeSpan TotalDuration => TimeSpan.FromTicks(Results.Sum(r => r.Duration.Ticks));
    }

    /// <summary>
    /// The test attribute class
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute { }

    /// <summary>
    /// The setup attribute class
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Method)]
    public class SetupAttribute : Attribute { }

    /// <summary>
    /// The teardown attribute class
    /// </summary>
    /// <seealso cref="Attribute"/>
    [AttributeUsage(AttributeTargets.Method)]
    public class TeardownAttribute : Attribute { }

    /// <summary>
    /// The assert class
    /// </summary>
    public static class Assert
    {
        /// <summary>
        /// Fails the message
        /// </summary>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void Fail(string? message = null)
        {
            throw new AssertionException(message ?? "Assertion failed");
        }
        /// <summary>
        /// Ises the true using the specified condition
        /// </summary>
        /// <param name="condition">The condition</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void IsTrue(bool condition, string? message = null)
        {
            if (!condition)
                throw new AssertionException(message ?? "Expected true but was false");
        }

        /// <summary>
        /// Ises the false using the specified condition
        /// </summary>
        /// <param name="condition">The condition</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void IsFalse(bool condition, string? message = null)
        {
            if (condition)
                throw new AssertionException(message ?? "Expected false but was true");
        }

        /// <summary>
        /// Ares the equal using the specified expected
        /// </summary>
        /// <typeparam name="T">The </typeparam>
        /// <param name="expected">The expected</param>
        /// <param name="actual">The actual</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void AreEqual<T>(T expected, T actual, string? message = null)
        {
            if (!Equals(expected, actual))
                throw new AssertionException(message ?? $"Expected '{expected}' but was '{actual}'");
        }

        /// <summary>
        /// Ares the not equal using the specified expected
        /// </summary>
        /// <typeparam name="T">The </typeparam>
        /// <param name="expected">The expected</param>
        /// <param name="actual">The actual</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void AreNotEqual<T>(T expected, T actual, string? message = null)
        {
            if (Equals(expected, actual))
                throw new AssertionException(message ?? $"Expected not equal to '{expected}' but was '{actual}'");
        }

        /// <summary>
        /// Ises the null using the specified obj
        /// </summary>
        /// <param name="obj">The obj</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void IsNull(object? obj, string? message = null)
        {
            if (obj != null)
                throw new AssertionException(message ?? $"Expected null but was '{obj}'");
        }

        /// <summary>
        /// Ises the not null using the specified obj
        /// </summary>
        /// <param name="obj">The obj</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void IsNotNull(object? obj, string? message = null)
        {
            if (obj == null)
                throw new AssertionException(message ?? "Expected not null but was null");
        }

        /// <summary>
        /// Containses the collection
        /// </summary>
        /// <typeparam name="T">The </typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="item">The item</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null)
        {
            if (!collection.Contains(item))
                throw new AssertionException(message ?? $"Collection does not contain '{item}'");
        }

        /// <summary>
        /// Doeses the not contain using the specified collection
        /// </summary>
        /// <typeparam name="T">The </typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="item">The item</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string? message = null)
        {
            if (collection.Contains(item))
                throw new AssertionException(message ?? $"Collection should not contain '{item}'");
        }

        /// <summary>
        /// Throwses the action
        /// </summary>
        /// <typeparam name="TException">The exception</typeparam>
        /// <param name="action">The action</param>
        /// <param name="message">The message</param>
        /// <exception cref="AssertionException"></exception>
        /// <exception cref="AssertionException"></exception>
        public static void Throws<TException>(Action action, string? message = null) where TException : Exception
        {
            try
            {
                action();
                throw new AssertionException(message ?? $"Expected {typeof(TException).Name} but no exception was thrown");
            }
            catch (TException)
            {
                // Expected exception
            }
            catch (Exception ex)
            {
                throw new AssertionException(message ?? $"Expected {typeof(TException).Name} but got {ex.GetType().Name}");
            }
        }
    }

    /// <summary>
    /// The assertion exception class
    /// </summary>
    /// <seealso cref="Exception"/>
    public class AssertionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssertionException"/> class
        /// </summary>
        /// <param name="message">The message</param>
        public AssertionException(string message) : base(message) { }
    }

    /// <summary>
    /// Test execution configuration
    /// </summary>
    public class TestExecutionConfig
    {
        /// <summary>
        /// Gets or sets the maximum degree of parallelism
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        
        /// <summary>
        /// Gets or sets whether to run tests in parallel
        /// </summary>
        public bool EnableParallelExecution { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the test timeout in milliseconds
        /// </summary>
        public int TestTimeoutMs { get; set; } = 30000; // 30 seconds
        
        /// <summary>
        /// Gets or sets whether to show detailed timing information
        /// </summary>
        public bool ShowDetailedTiming { get; set; } = false;
    }

    /// <summary>
    /// Thread-safe test context for isolated test execution
    /// </summary>
    public class TestContext
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets or sets test data in a thread-safe manner
        /// </summary>
        public object this[string key]
        {
            get
            {
                lock (_lock)
                {
                    return _data.TryGetValue(key, out var value) ? value : null;
                }
            }
            set
            {
                lock (_lock)
                {
                    _data[key] = value;
                }
            }
        }
        
        /// <summary>
        /// Gets the current thread ID
        /// </summary>
        public int ThreadId => Thread.CurrentThread.ManagedThreadId;
    }

    /// <summary>
    /// The test runner class
    /// </summary>
    public class TestRunner
    {
        private static readonly TestExecutionConfig _config = new TestExecutionConfig();
        private static readonly ConcurrentDictionary<int, TestContext> _contexts = new ConcurrentDictionary<int, TestContext>();
        
        /// <summary>
        /// Gets the test context for the current thread
        /// </summary>
        public static TestContext CurrentContext => _contexts.GetOrAdd(Thread.CurrentThread.ManagedThreadId, _ => new TestContext());
        
        /// <summary>
        /// Configures the test execution
        /// </summary>
        public static void Configure(Action<TestExecutionConfig> configure)
        {
            configure(_config);
        }

        /// <summary>
        /// Runs the all tests
        /// </summary>
        /// <returns>The test suites</returns>
        public static List<TestSuite> RunAllTests()
        {
            var testSuites = new ConcurrentBag<TestSuite>();
            var assembly = Assembly.GetExecutingAssembly();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<TestAttribute>() != null))
                .ToList();

            if (_config.EnableParallelExecution)
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = _config.MaxDegreeOfParallelism,
                    CancellationToken = CancellationToken.None
                };

                Parallel.ForEach(testClasses, parallelOptions, testClass =>
                {
                    var suite = RunTestClass(testClass);
                    testSuites.Add(suite);
                });
            }
            else
            {
                foreach (var testClass in testClasses)
                {
                    var suite = RunTestClass(testClass);
                    testSuites.Add(suite);
                }
            }

            return testSuites.OrderBy(s => s.Name).ToList();
        }

        public static TestSuite RunTestClass(Type testClass)
        {
            var suite = new TestSuite { Name = testClass.Name };
            
            var setupMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SetupAttribute>() != null);
            var teardownMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TeardownAttribute>() != null);
            var testMethods = testClass.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null).ToList();

            var results = new ConcurrentBag<TestResult>();
            var semaphore = new SemaphoreSlim(_config.MaxDegreeOfParallelism);

            if (_config.EnableParallelExecution)
            {
                var tasks = testMethods.Select(async testMethod =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var result = await RunSingleTestAsync(testClass, testMethod, setupMethod, teardownMethod);
                        results.Add(result);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToArray();

                Task.WaitAll(tasks);
            }
            else
            {
                foreach (var testMethod in testMethods)
                {
                    var result = RunSingleTestAsync(testClass, testMethod, setupMethod, teardownMethod).Result;
                    results.Add(result);
                }
            }

            suite.Results = results.OrderBy(r => r.TestName).ToList();
            return suite;
        }

        /// <summary>
        /// Runs a single test asynchronously with proper isolation
        /// </summary>
        private static async Task<TestResult> RunSingleTestAsync(Type testClass, MethodInfo testMethod, MethodInfo setupMethod, MethodInfo teardownMethod)
        {
            return await Task.Run(() =>
            {
                var result = new TestResult { TestName = testMethod.Name };
                var stopwatch = Stopwatch.StartNew();
                var threadId = Thread.CurrentThread.ManagedThreadId;
                
                // Create isolated instance for this test
                object instance = null;
                var cancellationTokenSource = new CancellationTokenSource(_config.TestTimeoutMs);

                try
                {
                    // Create fresh instance for thread safety
                    instance = Activator.CreateInstance(testClass);
                    
                    // Set up test context
                    var context = new TestContext();
                    _contexts.TryAdd(threadId, context);
                    
                    // Execute setup
                    try
                    {
                        setupMethod?.Invoke(instance, null);
                    }
                    catch (TargetInvocationException setupEx) when (setupEx.InnerException != null)
                    {
                        throw new Exception($"Setup failed: {setupEx.InnerException.Message}", setupEx.InnerException);
                    }
                    
                    // Execute test method
                    try
                    {
                        testMethod.Invoke(instance, null);
                    }
                    catch (TargetInvocationException testEx) when (testEx.InnerException != null)
                    {
                        throw testEx.InnerException; // Re-throw the actual exception
                    }
                    
                    result.Passed = true;
                }
                catch (OperationCanceledException)
                {
                    result.Passed = false;
                    result.ErrorMessage = $"Test timed out after {_config.TestTimeoutMs}ms";
                    result.Exception = new TimeoutException($"Test timed out after {_config.TestTimeoutMs}ms");
                }
                catch (AssertionException assertEx)
                {
                    result.Passed = false;
                    result.Exception = assertEx;
                    result.ErrorMessage = $"Assertion failed: {assertEx.Message}";
                }
                catch (Exception ex)
                {
                    result.Passed = false;
                    result.Exception = ex;
                    result.ErrorMessage = $"Test failed with {ex.GetType().Name}: {ex.Message}";
                    
                    // Add stack trace for debugging
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        result.ErrorMessage += Environment.NewLine + "Stack trace:" + Environment.NewLine + ex.StackTrace;
                    }
                    
                    // If it's a nested exception, show the full chain
                    var innerEx = ex.InnerException;
                    while (innerEx != null)
                    {
                        result.ErrorMessage += Environment.NewLine + $"Inner exception ({innerEx.GetType().Name}): {innerEx.Message}";
                        if (!string.IsNullOrEmpty(innerEx.StackTrace))
                        {
                            result.ErrorMessage += Environment.NewLine + innerEx.StackTrace;
                        }
                        innerEx = innerEx.InnerException;
                    }
                }
                finally
                {
                    try
                    {
                        // Clean up in finally block
                        teardownMethod?.Invoke(instance, null);
                    }
                    catch (TargetInvocationException teardownEx) when (teardownEx.InnerException != null)
                    {
                        if (result.Passed)
                        {
                            result.Passed = false;
                            result.Exception = teardownEx.InnerException;
                            result.ErrorMessage = "Teardown failed: " + teardownEx.InnerException.Message + Environment.NewLine + teardownEx.InnerException.StackTrace;
                        }
                        else
                        {
                            // Test already failed, just append teardown error
                            result.ErrorMessage += Environment.NewLine + "Additionally, teardown failed: " + teardownEx.InnerException.Message;
                        }
                    }
                    catch (Exception teardownEx)
                    {
                        if (result.Passed)
                        {
                            result.Passed = false;
                            result.Exception = teardownEx;
                            result.ErrorMessage = "Teardown failed: " + teardownEx.Message + Environment.NewLine + teardownEx.StackTrace;
                        }
                        else
                        {
                            // Test already failed, just append teardown error
                            result.ErrorMessage += Environment.NewLine + "Additionally, teardown failed: " + teardownEx.Message;
                        }
                    }
                    finally
                    {
                        // Clean up test context
                        _contexts.TryRemove(threadId, out _);
                        stopwatch.Stop();
                        result.Duration = stopwatch.Elapsed;
                        cancellationTokenSource.Dispose();
                    }
                }

                return result;
            });
        }

        /// <summary>
        /// Prints the results using the specified test suites
        /// </summary>
        /// <param name="testSuites">The test suites</param>
        public static void PrintResults(List<TestSuite> testSuites)
        {
            Console.WriteLine("μHigh Test Results");
            Console.WriteLine("==================");
            
            if (_config.EnableParallelExecution)
            {
                Console.WriteLine($"Parallel execution enabled (Max threads: {_config.MaxDegreeOfParallelism})");
            }
            else
            {
                Console.WriteLine("Sequential execution");
            }
            Console.WriteLine();

            foreach (var suite in testSuites)
            {
                var color = suite.FailedCount == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = color;
                Console.WriteLine($"Test Suite: {suite.Name}");
                Console.ResetColor();

                Console.WriteLine($"  Passed: {suite.PassedCount}");
                Console.WriteLine($"  Failed: {suite.FailedCount}");
                Console.WriteLine($"  Total:  {suite.TotalCount}");
                Console.WriteLine($"  Duration: {suite.TotalDuration.TotalMilliseconds:F2}ms");
                
                if (_config.ShowDetailedTiming && suite.Results.Any())
                {
                    var avgTime = suite.Results.Average(r => r.Duration.TotalMilliseconds);
                    var maxTime = suite.Results.Max(r => r.Duration.TotalMilliseconds);
                    var minTime = suite.Results.Min(r => r.Duration.TotalMilliseconds);
                    Console.WriteLine($"  Avg/Min/Max: {avgTime:F2}ms / {minTime:F2}ms / {maxTime:F2}ms");
                }
                Console.WriteLine();

                // Show failed tests first
                foreach (var result in suite.Results.Where(r => !r.Passed))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ {result.TestName} ({result.Duration.TotalMilliseconds:F1}ms)");
                    Console.ResetColor();
                    Console.WriteLine($"    {result.ErrorMessage}");
                    Console.WriteLine();
                }

                // Show passed tests
                foreach (var result in suite.Results.Where(r => r.Passed))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ {result.TestName} ({result.Duration.TotalMilliseconds:F1}ms)");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            var totalPassed = testSuites.Sum(s => s.PassedCount);
            var totalFailed = testSuites.Sum(s => s.FailedCount);
            var totalTests = testSuites.Sum(s => s.TotalCount);
            var totalDuration = TimeSpan.FromTicks(testSuites.Sum(s => s.TotalDuration.Ticks));

            Console.WriteLine("Summary");
            Console.WriteLine("=======");
            var summaryColor = totalFailed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = summaryColor;
            Console.WriteLine($"Total: {totalTests} tests, {totalPassed} passed, {totalFailed} failed");
            Console.ResetColor();
            Console.WriteLine($"Duration: {totalDuration.TotalMilliseconds:F2}ms");
            
            if (_config.EnableParallelExecution)
            {
                Console.WriteLine($"Parallel efficiency: {(totalDuration.TotalMilliseconds / (testSuites.Count * _config.MaxDegreeOfParallelism)):F2}ms per thread");
            }
        }
    }
}
