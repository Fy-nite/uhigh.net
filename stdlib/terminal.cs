namespace StdLib
{
    /// <summary>
    /// Terminal interface for user interaction
    /// </summary>
    public static class IO
    {

        /// <summary>
        /// Prints the line using the specified value
        /// </summary>
        /// <param name="value">The value</param>
        public static void PrintLine(params object?[] value)
        {
            if (value == null || value.Length == 0)
            {
                Console.WriteLine();
                return;
            }

            var output = string.Join(" ", value.Select(v => v?.ToString() ?? "null"));
            Console.WriteLine(output);
        }

        /// <summary>
        /// Prints the value
        /// </summary>
        /// <param name="value">The value</param>
        public static void Print(params object?[] value)
        {
            if (value == null || value.Length == 0)
            {
                Console.WriteLine();
                return;
            }

            var output = string.Join(" ", value.Select(v => v?.ToString() ?? "null"));
            Console.Write(output);
        }

        /// <summary>
        /// Reads the input using the specified prompt
        /// </summary>
        /// <param name="prompt">The prompt</param>
        /// <returns>The string</returns>
        public static string? ReadInput(string? prompt = null)
        {
            if (prompt != null)
            {
                Console.Write(prompt);
            }
            return Console.ReadLine();
        }
    }
}
