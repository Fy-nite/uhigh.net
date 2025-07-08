using System.Diagnostics;
using System.Reflection;

namespace uhigh.Net.Testing
{
    public enum TestStatus {
        Passed, Failed, Skipped
    }

    public class TestResult {
        public string TestName = "";
        public TestStatus Status;
        public TimeSpan Duration;
        public string Message = "uh";
    }

    public class TestSuiteResult {
        public string Name = "(unnamed)";
        public List<TestResult> TestResults = new();
        public TimeSpan TotalTime;
        public TestSuiteCounts Counts = new();
    }

    public class TestSuiteCounts {
        public int Passed;
        public int Failed;
        public int Skipped;
        public int Total;
        public int Ran;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class SetupAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TeardownAttribute : Attribute { }

    public class AssertionException : Exception { public AssertionException(string message) : base(message) {}}
    public static class Assert {
        public static void Fail(string? message = null)                                                 { throw new           AssertionException(message ?? "Assertion failed");                                                }
        public static void IsTrue(bool condition, string? message = null)                               { if (!condition)                   Fail(message ?? "Expected true but was false");                                     }
        public static void IsFalse(bool condition, string? message = null)                              { if (condition)                    Fail(message ?? "Expected false but was true");                                     }
        public static void AreEqual<T>(T expected, T actual, string? message = null)                    { if (!Equals(expected, actual))    Fail(message ?? $"Expected '{expected}' but was '{actual}'");                       }
        public static void AreNotEqual<T>(T expected, T actual, string? message = null)                 { if (Equals(expected, actual))     Fail(message ?? $"Expected not equal to '{expected}' but was '{actual}'");          }
        public static void IsNull(object? obj, string? message = null)                                  { if (obj != null)                  Fail(message ?? $"Expected null but was '{obj}'");                                  }
        public static void IsNotNull(object? obj, string? message = null)                               { if (obj == null)                  Fail(message ?? $"Expected not null but was null");                                 }
        public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null)       { if (!collection.Contains(item))   Fail(message ?? $"Expected collection to contain '{item}'");                        }
        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string? message = null) { if (collection.Contains(item))    Fail(message ?? $"Expected collection to not contain '{item}'");                    }
        public static void Throws<TException>(Action action, string? message = null) where TException : Exception { try { action();         Fail(message ?? $"Expected {typeof(TException).Name} but no exception was thrown"); }
            catch (TException) {} catch (Exception ex)                                                {                                     Fail(message ?? $"Expected {typeof(TException).Name} but got {ex.GetType().Name}"); }
        }
    }

    public class TestRunner {
        public static List<TestSuiteResult> RunAllTests() {
            var testSuites = new List<TestSuiteResult>();
            var assembly = Assembly.GetExecutingAssembly();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<TestAttribute>() != null))
                .ToList();

            foreach (var testClass in testClasses)
            {
                var suite = RunTestSuite(testClass);
                testSuites.Add(suite);
            }

            return testSuites;
        }

        public static TestSuiteResult RunTestSuite(Type testClass) {
            var suite = new TestSuiteResult { Name = testClass.Name };
            
            var setupMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SetupAttribute>() != null);
            var teardownMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TeardownAttribute>() != null);
            var testMethods = testClass.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null);

            foreach (var testMethod in testMethods)
            {
                var result = RunTest(testClass, testMethod, setupMethod, teardownMethod);
                suite.TestResults.Add(result);
                if (result.Status == TestStatus.Passed)
                    suite.Counts.Passed += 1;
                else 
                    suite.Counts.Failed += 1;
                suite.Counts.Ran += 1;
                suite.Counts.Total += 1;
                suite.TotalTime += result.Duration;
            }

            return suite;
        }

        public static TestResult RunTest(Type testClass, MethodInfo test, MethodInfo? setup, MethodInfo? teardown) {
            var stopwatch = Stopwatch.StartNew();
            
            var instance = Activator.CreateInstance(testClass); // Instance of the test class
            var result = new TestResult { TestName = test.Name };

            try {
                setup?.Invoke(instance, null);
                test.Invoke(instance, null);
                result.Status = TestStatus.Passed;
            } catch (Exception ex) when (ex.InnerException != null) {
                result.Status = TestStatus.Failed;
                result.Message = ex.InnerException.Message;
            } catch (Exception ex) {
                result.Status = TestStatus.Failed;
                result.Message = ex.Message;
            } finally {
                try {
                    teardown?.Invoke(instance, null);
                } catch (Exception ex) {
                    if (result.Status == TestStatus.Passed) {
                        result.Status = TestStatus.Failed;
                        result.Message = ex.Message;
                    }
                }
                
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }
            return result;
        }
        public static void PrintResults(List<TestSuiteResult> testSuites)
        {
            Console.WriteLine("μHigh Test Results");
            Console.WriteLine("==================");
            Console.WriteLine();

            foreach (var suite in testSuites)
            {
                var color = suite.Counts.Failed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = color;
                Console.WriteLine($"Test Suite: {suite.Name}");
                Console.ResetColor();
                
                Console.WriteLine($"  Passed: {suite.Counts.Passed}");
                Console.WriteLine($"  Failed: {suite.Counts.Failed}");
                Console.WriteLine($"  Total:  {suite.Counts.Total}");
                Console.WriteLine($"  Duration: {suite.TotalTime.TotalMilliseconds:F2}ms");
                Console.WriteLine();

                foreach (var result in suite.TestResults.Where(r => r.Status == TestStatus.Failed))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ {result.TestName}");
                    Console.ResetColor();
                    Console.WriteLine($"    {result.Message}");
                    Console.WriteLine();
                }

                foreach (var result in suite.TestResults.Where(r => r.Status == TestStatus.Passed))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ {result.TestName} ({result.Duration.TotalMilliseconds:F1}ms)");
                    Console.ResetColor();
                }

                foreach (var result in suite.TestResults.Where(r => r.Status == TestStatus.Skipped))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"   {result.TestName} ({result.Duration.TotalMilliseconds:F1}ms)");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            var totalPassed = testSuites.Sum(s => s.Counts.Passed);
            var totalSkipped = testSuites.Sum(s => s.Counts.Skipped);
            var totalFailed = testSuites.Sum(s => s.Counts.Failed);
            var totalRan = testSuites.Sum(s => s.Counts.Ran);
            var totalTests = testSuites.Sum(s => s.Counts.Total);
            var totalDuration = TimeSpan.FromTicks(testSuites.Sum(s => s.TotalTime.Ticks));

            Console.WriteLine("Summary");
            Console.WriteLine("=======");
            var summaryColor = totalFailed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.ForegroundColor = summaryColor;
            Console.WriteLine($"Total:   {totalTests}\nRan:     {totalRan}\nSkipped: {totalSkipped}\nPassed:  {totalPassed}\nFailed:  {totalFailed}");
            Console.ResetColor();
            Console.WriteLine($"Duration: {totalDuration.TotalMilliseconds:F2}ms");
        }
    }
}
