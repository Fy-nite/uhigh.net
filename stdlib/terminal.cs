using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Prints the value
        /// </summary>
        /// <param name="value">The value</param>
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
