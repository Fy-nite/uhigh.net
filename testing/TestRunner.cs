namespace uhigh.Net.Testing
{
    /// <summary>
    /// The test program class
    /// </summary>
    public class TestProgram
    {
        /// <summary>
        /// Tests the main
        /// </summary>
        public static void TestMain()
        {
            Console.WriteLine("Running μHigh Compiler Tests");
            Console.WriteLine("============================");
            Console.WriteLine();

            var testSuites = TestRunner.RunAllTests();
            TestRunner.PrintResults(testSuites);

            var totalFailed = testSuites.Sum(s => s.Counts.Failed);
            Environment.Exit(totalFailed == 0 ? 0 : 1);
        }
    }
}
