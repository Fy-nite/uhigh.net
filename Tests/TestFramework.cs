// TODO: convert threading to list of tests and then have 32 threads that when free pop a test off the test stack



using System.Diagnostics;
using System.Reflection;

namespace uhigh.Net.Testing
{
    public enum TestStatus
    {
        Passed, Failed, Skipped
    }

    public class TestResult
    {
        public string TestName = "";
        public TestStatus Status;
        public TimeSpan Duration;
        public string Message = "uh";
    }

    public class TestSuiteResult
    {
        public string Name = "(unnamed)";
        public List<TestResult> TestResults = new();
        public TimeSpan TotalTime;
        public TestSuiteCounts Counts = new();
        public List<Thread> threads = new();
    }

    public class TestSuiteCounts
    {
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

    public class AssertionException : Exception { public AssertionException(string message) : base(message) { } }
    public static class Assert
    {
        public static void Fail(string? message = null) { throw new AssertionException(message ?? "Assertion failed"); }
        public static void IsTrue(bool condition, string? message = null) { if (!condition) Fail(message ?? "Expected true but was false"); }
        public static void IsFalse(bool condition, string? message = null) { if (condition) Fail(message ?? "Expected false but was true"); }
        public static void AreEqual<T>(T expected, T actual, string? message = null) { if (!Equals(expected, actual)) Fail(message ?? $"Expected '{expected}' but was '{actual}'"); }
        public static void AreNotEqual<T>(T expected, T actual, string? message = null) { if (Equals(expected, actual)) Fail(message ?? $"Expected not equal to '{expected}' but was '{actual}'"); }
        public static void IsNull(object? obj, string? message = null) { if (obj != null) Fail(message ?? $"Expected null but was '{obj}'"); }
        public static void IsNotNull(object? obj, string? message = null) { if (obj == null) Fail(message ?? $"Expected not null but was null"); }
        public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null) { if (!collection.Contains(item)) Fail(message ?? $"Expected collection to contain '{item}'"); }
        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string? message = null) { if (collection.Contains(item)) Fail(message ?? $"Expected collection to not contain '{item}'"); }
        public static void Throws<TException>(Action action, string? message = null) where TException : Exception
        {
            try { action(); Fail(message ?? $"Expected {typeof(TException).Name} but no exception was thrown"); }
            catch (TException) { }
            catch (Exception ex) { Fail(message ?? $"Expected {typeof(TException).Name} but got {ex.GetType().Name}"); }
        }
    }

    public static class TestRunnerConfig
    {
        public static int multithreaded_max_tests = Environment.ProcessorCount - 1;
        public static bool multithreaded = true;
    }

    public class TestRunner
    {
        public static List<TestRunnerData> to_run_tests = new();

        public class TestRunnerData
        {
            Type testClass;
            MethodInfo? testMethod;
            MethodInfo? setupMethod;
            MethodInfo? teardownMethod;
            TestSuiteResult? suite;
            public bool is_exit = false;
            public TestRunnerData(Type testClass, MethodInfo? test, MethodInfo? setup, MethodInfo? teardown, TestSuiteResult? suite, bool is_exit = false)
            {
                this.testClass = testClass;
                this.setupMethod = setup;
                this.testMethod = test;
                this.teardownMethod = teardown;
                this.suite = suite;
                if (is_exit) this.is_exit = true; else this.is_exit = false;
            }
            public void Run()
            {
                if (testMethod == null || suite == null)
                {
                    throw new Exception("Uhhhhnsedfvlijsndflkjvdsfakjnsdflkvjsndlfkjvnsdkfnvskldjfnvsdkjfnvlkjdfnvlskjfvnlskdjfnlskjdnfvlkjsdnfvlkjsdnfvlksdjfnvlksdjfvnlsdkjfnvlskdjfnvksldjfnvlskdjfnvlsdkfvnsldkjfvnsldkjfnvlskdjfnvlkjdsfnfvlkjsdnfvlkjsdnfvlkjsdnfvlksdfnvlsdkjfnvlskdjfnvlsdkfjnvsldfkjnvlsdkfjnvsdlkfjvfn");
                }
                var result = RunTest(testClass, testMethod, setupMethod, teardownMethod);
                lock (suite)
                {
                    suite.TestResults.Add(result);

                    if (result.Status == TestStatus.Passed)
                        suite.Counts.Passed += 1;
                    else if (result.Status == TestStatus.Skipped)
                        suite.Counts.Skipped += 1;
                    else
                        suite.Counts.Failed += 1;
                    if (result.Status != TestStatus.Skipped) suite.Counts.Ran += 1;

                    suite.Counts.Total += 1;
                    suite.TotalTime += result.Duration;
                }
            }
        }

        public static TestResult RunTest(Type testClass, MethodInfo test, MethodInfo? setup, MethodInfo? teardown)
        {
            var stopwatch = Stopwatch.StartNew();

            var instance = Activator.CreateInstance(testClass); // Instance of the test class
            var result = new TestResult { TestName = test.Name };

            try
            {
                setup?.Invoke(instance, null);
                test.Invoke(instance, null);
                result.Status = TestStatus.Passed;
            }
            catch (Exception ex) when (ex.InnerException != null)
            {
                result.Status = TestStatus.Failed;
                result.Message = ex.InnerException.Message;
            }
            catch (Exception ex)
            {
                result.Status = TestStatus.Failed;
                result.Message = ex.Message;
            }
            finally
            {
                try
                {
                    teardown?.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    if (result.Status == TestStatus.Passed)
                    {
                        result.Status = TestStatus.Failed;
                        result.Message = ex.Message;
                    }
                }

                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }
            return result;
        }

        private static Lock run_test_lock = new();
        // Function to get tests from to_run_tests and run them, exiting on special test
        public static void TesterThread()
        {
            while (true)
            {
                bool is_test_to_run = false;
                while (!is_test_to_run)
                {
                    run_test_lock.EnterScope();
                    if (to_run_tests.Count == 0)
                    {
                        run_test_lock.Exit();
                    }
                    else
                    {
                        is_test_to_run = true;
                    }
                }
                TestRunnerData data = to_run_tests.First();
                if (data.is_exit == true)
                {
                    run_test_lock.Exit();
                    return;
                }
                to_run_tests.RemoveAt(0);
                run_test_lock.Exit(); // Exit the lock because we are done with the to_run_tests list
                data.Run();
            }
        }
        public static List<TestSuiteResult> RunAllTests() { return RunAllTests(new List<string>()); }

        public static List<TestSuiteResult> RunAllTests(List<string> skip_tests)
        {
            Console.Write("Running Tests ");
            List<Thread> task_threads = new();
            if (TestRunnerConfig.multithreaded)
            {
                Console.WriteLine($"With {TestRunnerConfig.multithreaded_max_tests} threads");
                for (int i = 0; i < TestRunnerConfig.multithreaded_max_tests; i++)
                {
                    Thread thread = new Thread(new ThreadStart(TesterThread));
                    thread.Start();
                    task_threads.Add(thread);
                }
            }
            else
                Console.WriteLine("In a Single Thread");
            var testSuites = new List<TestSuiteResult>();
            var assembly = Assembly.GetExecutingAssembly();
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<TestAttribute>() != null))
                .ToList();
            var stopwatch = Stopwatch.StartNew();
            foreach (var testClass in testClasses)
            {
                var suite = RunTestSuite(testClass, skip_tests);
                testSuites.Add(suite);
            }
            to_run_tests.Add(new TestRunnerData(typeof(TestRunner), null, null, null, null, true));
            // Wait for threads to exit
            TesterThread();
            var waiting = TestRunnerConfig.multithreaded;
            while (waiting)
            {
                waiting = false;
                foreach (var thread in task_threads)
                {
                    if (thread.IsAlive)
                    {
                        waiting = true;
                        continue;
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Duration: {stopwatch.ElapsedMilliseconds:F2}ms");
            return testSuites;
        }


        public static TestSuiteResult RunTestSuite(Type testClass, List<string> skip_tests)
        {
            var suite = new TestSuiteResult { Name = testClass.Name };

            var setupMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<SetupAttribute>() != null);
            var teardownMethod = testClass.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TeardownAttribute>() != null);
            var testMethods = testClass.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null);

            foreach (var testMethod in testMethods)
            {
                if (skip_tests.Any(r => r.Equals(testMethod.Name)))
                {
                    lock (suite)
                    {
                        suite.Counts.Skipped += 1;
                        suite.Counts.Total += 1;
                        var result = new TestResult { TestName = testMethod.Name };
                        result.Status = TestStatus.Skipped;
                        Console.WriteLine(testMethod.Name);
                        suite.TestResults.Add(result);
                        continue;
                    }
                }
                lock (run_test_lock)
                {
                    TestRunnerData data = new TestRunnerData(testClass, testMethod, setupMethod, teardownMethod, suite);
                    to_run_tests.Add(data); // Queue test
                }
            }

            return suite;
        }

        public static void PrintResults(List<TestSuiteResult> testSuites)
        {
            Console.WriteLine("μHigh Test Results");
            Console.WriteLine("==================");
            Console.WriteLine();

            foreach (var suite in testSuites.OrderBy(r => ((double)r.Counts.Failed / (double)r.Counts.Ran)).ToList())
            {
                var color = suite.Counts.Failed == 0 ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = color;
                Console.WriteLine($"Test Suite: {suite.Name}");
                Console.ResetColor();

                Console.WriteLine($"  Passed: {suite.Counts.Passed}");
                Console.WriteLine($"  Failed: {suite.Counts.Failed}");
                Console.WriteLine($"  Skipped: {suite.Counts.Skipped}");
                Console.WriteLine($"  Total:  {suite.Counts.Total}");
                Console.WriteLine($"  Duration: {suite.TotalTime.TotalMilliseconds:F2}ms");
                Console.WriteLine();

                foreach (var result in suite.TestResults)
                {
                    if (result.Status == TestStatus.Failed)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"  {result.TestName}");
                        Console.ResetColor();
                        Console.WriteLine(new string(' ', 40 - result.TestName.Length) + $"{result.Message}");
                        //Console.WriteLine();
                    }
                    else if (result.Status == TestStatus.Passed)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  {result.TestName} ({result.Duration.TotalMilliseconds:F1}ms)");
                        Console.ResetColor();
                    }
                    else if (result.Status == TestStatus.Skipped)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  {result.TestName} ({result.Duration.TotalMilliseconds:F1}ms)");
                        Console.ResetColor();
                    }
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

            //Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Total:   {totalTests}");
            Console.WriteLine($"Ran:     {totalRan}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Skipped: {totalSkipped}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Passed:  {totalPassed}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed:  {totalFailed}");
            Console.ResetColor();
            Console.WriteLine($"Duration: {totalDuration.TotalMilliseconds:F2}ms");
        }
    }
}
