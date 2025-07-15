using System.Diagnostics;
using System.Reflection;

namespace StdLib
{
    // Attribute to mark test-only classes
    [AttributeUsage(AttributeTargets.Class)]
    public class TestingOnlyAttribute : Attribute { }

    // Attribute to specify test data for a test method
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestWithAttribute : Attribute
    {
        public Type InputType { get; }
        public TestWithAttribute(Type inputType)
        {
            InputType = inputType;
        }
    }

    // Attribute to specify expected value for a variable
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExpectAttribute : Attribute
    {
        public object Expected { get; }
        public ExpectAttribute(object expected)
        {
            Expected = expected;
        }
    }

    // Attribute to specify that a test is expected to throw a specific exception
    [AttributeUsage(AttributeTargets.Method)]
    public class ExpectExceptionAttribute : Attribute
    {
        public Type ExceptionType { get; }
        public ExpectExceptionAttribute(Type exceptionType)
        {
            ExceptionType = exceptionType;
        }
    }

    // Simple test runner (skeleton)
    public static class InlineTestRunner
    {
        public static void RunAllTests()
        {
            int total = 0, passed = 0, failed = 0;
            var testClasses = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract); // Only public, non-abstract classes

            foreach (var cls in testClasses)
            {
                foreach (var method in cls.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    // Only consider public methods
                    if (!method.IsPublic) continue;

                    var testWithAttrs = method.GetCustomAttributes(typeof(TestWithAttribute), false);
                    foreach (TestWithAttribute attr in testWithAttrs)
                    {
                        total++;
                        var input = Activator.CreateInstance(attr.InputType);
                        // If method is static, instance is null; otherwise, create instance
                        var instance = method.IsStatic ? null : Activator.CreateInstance(cls);
                        var expectExceptionAttr = (ExpectExceptionAttribute)method.GetCustomAttributes(typeof(ExpectExceptionAttribute), false).FirstOrDefault()!;
                        bool testPassed = false;
                        Exception thrown = null!;
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            method.Invoke(instance, new object[] { input! });
                            sw.Stop();
                            if (expectExceptionAttr != null)
                            {
                                PrintFail($"{cls.Name}.{method.Name}: Expected exception {expectExceptionAttr.ExceptionType.Name} but none was thrown");
                            }
                            else
                            {
                                var expectFields = cls.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                    .Where(f => f.GetCustomAttributes(typeof(ExpectAttribute), false).Any());
                                bool allFieldsPassed = true;
                                foreach (var field in expectFields)
                                {
                                    var expectAttr = (ExpectAttribute)field.GetCustomAttributes(typeof(ExpectAttribute), false).FirstOrDefault()!;
                                    if (expectAttr != null)
                                    {
                                        var actualValue = field.GetValue(instance);
                                        if (!object.Equals(actualValue, expectAttr.Expected))
                                        {
                                            PrintFail($"Test failed in {cls.Name}.{method.Name}: Expected {expectAttr.Expected}, but got {actualValue} for field {field.Name}");
                                            allFieldsPassed = false;
                                        }
                                        else
                                        {
                                            PrintPass($"Test passed in {cls.Name}.{method.Name}: Field {field.Name} has expected value {expectAttr.Expected}");
                                        }
                                    }
                                }
                                if (allFieldsPassed || !expectFields.Any())
                                {
                                    testPassed = true;
                                }
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            sw.Stop();
                            thrown = ex.InnerException!;
                            if (expectExceptionAttr != null && thrown != null && expectExceptionAttr.ExceptionType.IsInstanceOfType(thrown))
                            {
                                PrintPass($"{cls.Name}.{method.Name}: Threw expected exception {thrown.GetType().Name}");
                                testPassed = true;
                            }
                            else
                            {
                                PrintFail($"{cls.Name}.{method.Name}: Unexpected exception: {thrown}");
                            }
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            PrintFail($"{cls.Name}.{method.Name}: Unexpected exception: {ex}");
                        }
                        Console.WriteLine($"    Time: {sw.ElapsedMilliseconds} ms");
                        if (testPassed) passed++; else failed++;
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine("==== Test Summary ====");
            Console.WriteLine($"Total: {total}, Passed: {passed}, Failed: {failed}");
        }

        static void PrintPass(string msg)
        {
            if (ConsoleIsColor())
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg);
                Console.ForegroundColor = old;
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        static void PrintFail(string msg)
        {
            if (ConsoleIsColor())
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ForegroundColor = old;
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        static bool ConsoleIsColor()
        {
            // try { return Console.ForegroundColor! != null!; }
            // catch { return false; }
            return true; // Assume color support for simplicity
        }
    }
}