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
        public Exception? Exception;
    }

    public class TestSuite
    {
        public string Name = "(unnamed)";
        public List<TestResult> TestResults = new();
        public TimeSpan TotalTime;
        public TestSuiteCounts Counts = new();
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

    public static class Assert
    {
        public static void Fail(string? message = null)                                                 {                                   throw new AssertionException(message ?? "Assertion failed"); }
        public static void IsTrue(bool condition, string? message = null)                               { if (!condition)                   throw new AssertionException(message ?? "Expected true but was false"); }
        public static void IsFalse(bool condition, string? message = null)                              { if (condition)                    throw new AssertionException(message ?? "Expected false but was true"); }
        public static void AreEqual<T>(T expected, T actual, string? message = null)                    { if (!Equals(expected, actual))    throw new AssertionException(message ?? $"Expected '{expected}' but was '{actual}'"); }
        public static void AreNotEqual<T>(T expected, T actual, string? message = null)                 { if (Equals(expected, actual))     throw new AssertionException(message ?? $"Expected not equal to '{expected}' but was '{actual}'"); }
        public static void IsNull(object? obj, string? message = null)                                  { if (obj != null)                  throw new AssertionException(message ?? $"Expected null but was '{obj}'"); }
        public static void IsNotNull(object? obj, string? message = null)                               { if (obj == null)                  throw new AssertionException(message ?? $"Expected not null but was null"); }
        public static void Contains<T>(IEnumerable<T> collection, T item, string? message = null)       { if (!collection.Contains(item))   throw new AssertionException(message ?? $"Expected collection to contain '{item}'"); }
        public static void DoesNotContain<T>(IEnumerable<T> collection, T item, string? message = null) { if (collection.Contains(item))    throw new AssertionException(message ?? $"Expected collection to not contain '{item}'"); }
        public static void Throws<TException>(Action action, string? message = null) where TException : Exception
        {
            try
            {
                action();
                throw new AssertionException(message ?? $"Expected {typeof(TException).Name} but no exception was thrown");
            } catch (TException) {} catch (Exception ex)
            {
                throw new AssertionException(message ?? $"Expected {typeof(TException).Name} but got {ex.GetType().Name}");
            }
        }
    }
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}
