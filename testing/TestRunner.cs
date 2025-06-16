namespace uhigh.Net.Testing
{
    public class TestProgram
    {
        public static void TestMain()
        {
            Console.WriteLine("Running μHigh Compiler Tests");
            Console.WriteLine("============================");
            Console.WriteLine();

            var testSuites = TestRunner.RunAllTests();
            TestRunner.PrintResults(testSuites);

            var totalFailed = testSuites.Sum(s => s.FailedCount);
            Environment.Exit(totalFailed == 0 ? 0 : 1);
        }
    }
}
