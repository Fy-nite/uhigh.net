namespace uhigh.StdLib
{
    /// <summary>
    /// Terminal interface for user interaction
    /// </summary>
    public static class Terminal
    {
        /// <summary>
        /// Prints a line to the console, handling null values gracefully.
        /// </summary>
        /// <param name="value">The value to print. If null, prints "null".</param>
        /// <remarks>
        /// This method ensures that null values are printed as "null" instead of throwing an exception.
        /// It uses the ToString() method of the value if it is not null.
        /// </remarks>
        /// <example>
        /// Terminal.PrintLine("Hello, World!"); // Outputs: Hello, World!
        /// Terminal.PrintLine(null); // Outputs: null
        /// </example>
        /// <seealso cref="Print(object?)"/>
        /// <seealso cref="ReadInput(string?)"/>

        public static void PrintLine(object? value)
        {
            if (value == null)
            {
                Console.WriteLine("null");
            }
            else
            {
                Console.WriteLine(value.ToString());
            }
        }
        /// <summary>
        /// Prints a value to the console without a newline, handling null values gracefully.
        /// </summary>
        /// <param name="value">The value to print. If null, prints "null".</param>
        /// <remarks>
        /// This method ensures that null values are printed as "null" instead of throwing an exception.
        /// It uses the ToString() method of the value if it is not null.
        /// </remarks>
        /// <example>
        /// Terminal.Print("Hello, World!"); // Outputs: Hello, World!
        /// Terminal.Print(null); // Outputs: null
        /// </example>
        /// <seealso cref="PrintLine(object?)"/>
        /// <seealso cref="ReadInput(string?)"/>
        /// <returns>None</returns>
        /// <exception cref="ArgumentNullException">Thrown if the value is null and cannot be converted to a string.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the value's ToString() method throws an exception.</exception>
        /// <remarks>
        /// This method is useful for printing values in a more compact format without line breaks.
        /// </remarks>
        /// <seealso cref="PrintLine(object?)"/>
        /// <seealso cref="ReadInput(string?)"/>
        public static void Print(object? value)
        {
            if (value == null)
            {
                Console.Write("null");
            }
            else
            {
                Console.Write(value.ToString());
            }
        }
        /// <summary>
        /// Reads input from the console with an optional prompt.       
        /// </summary>
        /// <param name="prompt">An optional prompt to display before reading input. If null, no prompt is displayed.</param>
        /// <returns>The input read from the console as a string, or null if no input was provided.</returns>
        /// <remarks>        This method allows you to prompt the user for input, making it more user-friendly.
        /// If a prompt is provided, it will be displayed before waiting for input. If no prompt is provided, it simply waits for input without displaying anything.
        /// </remarks>
        /// <example>
        /// var name = Terminal.ReadInput("Enter your name: ");
        /// if name != null {
        ///   Terminal.PrintLine($"Hello, {name}!");
        /// /// } else {
        ///  Terminal.PrintLine("No input provided.");
        /// /// }
        /// </example>
        /// <seealso cref="PrintLine(object?)"/>
        /// <seealso cref="Print(object?)"/>
        /// <returns>The input read from the console as a string, or null if no input was provided.</returns>
        /// <exception cref="IOException">Thrown if there is an error reading from the console.</exception>
        
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