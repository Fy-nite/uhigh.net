using System.Diagnostics;
using System.Reflection;
using uhigh.Net.Parser; // Add this using statement
using uhigh.Net.Lexer;  // Add this using statement

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
    /// The test runner class
    /// </summary>
    public class TestRunner
    {
        /// <summary>
        /// Runs the all tests
        /// </summary>
        /// <returns>The test suites</returns>
        public static List<TestSuite> RunAllTests()
        {
            var testSuites = new List<TestSuite>();
            var assembly = Assembly.GetExecutingAssembly();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<TestAttribute>() != null))
                .ToList();

            foreach (var testClass in testClasses)
            {
                var suite = RunTestClass(testClass);
                testSuites.Add(suite);
            }

            return testSuites;
        }

        /// <summary>
        /// Runs the test using the specified test class
        /// </summary>
        /// <param name="testClass">The test</param>
        /// <returns>The suite</returns>
        public static TestSuite RunTestClass(Type testClass)
        {
            var suite = new TestSuite { Name = testClass.Name };
            var instance = Activator.CreateInstance(testClass);
            
            var setupMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SetupAttribute>() != null);
            var teardownMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TeardownAttribute>() != null);
            var testMethods = testClass.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null);

            foreach (var testMethod in testMethods)
            {
                var result = RunSingleTest(instance, testMethod, setupMethod, teardownMethod);
                suite.Results.Add(result);
            }

            return suite;
        }

        /// <summary>
        /// Runs the single test using the specified instance
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="testMethod">The test method</param>
        /// <param name="setupMethod">The setup method</param>
        /// <param name="teardownMethod">The teardown method</param>
        /// <returns>The result</returns>
        private static TestResult RunSingleTest(object? instance, MethodInfo testMethod, MethodInfo? setupMethod, MethodInfo? teardownMethod)
        {
            var result = new TestResult { TestName = testMethod.Name };
            var stopwatch = Stopwatch.StartNew();

            try
            {
                setupMethod?.Invoke(instance, null);
                testMethod.Invoke(instance, null);
                result.Passed = true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                result.Passed = false;
                result.Exception = ex.InnerException;
                result.ErrorMessage = ex.InnerException.Message;
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Exception = ex;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                try
                {
                    teardownMethod?.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    if (result.Passed)
                    {
                        result.Passed = false;
                        result.Exception = ex;
                        result.ErrorMessage = ex.Message;
                    }
                }
                
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }
            
            return result;
        }

        /// <summary>
        /// Prints the results using the specified test suites
        /// </summary>
        /// <param name="testSuites">The test suites</param>
        public static void PrintResults(List<TestSuite> testSuites)
        {
            Console.WriteLine("μHigh Test Results");
            Console.WriteLine("==================");
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
                Console.WriteLine();

                foreach (var result in suite.Results.Where(r => !r.Passed))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ {result.TestName}");
                    Console.ResetColor();
                    Console.WriteLine($"    {result.ErrorMessage}");
                    Console.WriteLine();
                }

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
        }
    }
}
