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

            // Configure parallel execution
            TestRunner.Configure(config =>
            {
                config.EnableParallelExecution = true;
                config.MaxDegreeOfParallelism = Environment.ProcessorCount;
                config.TestTimeoutMs = 30000; // 30 seconds
                config.ShowDetailedTiming = true;
            });

            Console.WriteLine($"Using {Environment.ProcessorCount} threads for parallel execution");
            Console.WriteLine($"Test timeout: 30 seconds");
            Console.WriteLine();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var testSuites = TestRunner.RunAllTests();
                stopwatch.Stop();
                
                TestRunner.PrintResults(testSuites);
                
                Console.WriteLine($"Total execution time: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");

                var totalFailed = testSuites.Sum(s => s.FailedCount);
                Environment.Exit(totalFailed == 0 ? 0 : 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error during test execution: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
