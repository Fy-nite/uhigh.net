using System.Diagnostics;
using System.Reflection;

namespace uhigh.Net.Testing
{
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public bool Passed { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public Exception? Exception { get; set; }
    }

    public class TestSuite
    {
        public string Name { get; set; } = "";
        public List<TestResult> Results { get; set; } = new();
        public int PassedCount => Results.Count(r => r.Passed);
        public int FailedCount => Results.Count(r => !r.Passed);
        public int TotalCount => Results.Count;
        public TimeSpan TotalDuration => TimeSpan.FromTicks(Results.Sum(r => r.Duration.Ticks));
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class SetupAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TeardownAttribute : Attribute { }

    public static class Assert
    {
        public static void Fail(string? message = null)
        {
            throw new AssertionException(message ?? "Assertion failed");
        }
        public static void IsTrue(bool condition, string? message = null)
        {
            if (!condition)
                throw new AssertionException(message ?? "Expected true but was false");
        }

        public static void IsFalse(bool condition, string? message = null)
        {
            if (condition)
                throw new AssertionException(message ?? "Expected false but was true");
        }

        public static void AreEqual<T>(T expected, T actual, string? message = null)
        {
            if (!Equals(expected, actual))
                throw new AssertionException(message ?? $"Expected '{expected}' but was '{actual}'");
        }

        public static void AreNotEqual<T>(T expected, T actual, string? message = null)
        {
            if (Equals(expected, actual))
                throw new AssertionException(message ?? $"Expected not equal to '{expected}' but was '{actual}'");
        }

        public static void IsNull(object? obj, string? message = null)
        {
            if (obj != null)
                throw new AssertionException(message ?? $"Expected null but was '{obj}'");
        }

        public static void IsNotNull(object? obj, string? message = null)
        {
            if (obj == null)
                throw new AssertionException(message ?? "Expected not null but was null");
        }

        public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null)
        {
            if (!collection.Contains(item))
                throw new AssertionException(message ?? $"Collection does not contain '{item}'");
        }

        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string? message = null)
        {
            if (collection.Contains(item))
                throw new AssertionException(message ?? $"Collection should not contain '{item}'");
        }

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

    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    public class TestRunner
    {
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
