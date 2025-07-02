namespace uhigh.StdLib
{
    /// <summary>
    /// The funnies class
    /// </summary>
    public static class Funnies
    {
        /// <summary>
        /// The print attribute class
        /// </summary>
        /// <seealso cref="System.Attribute"/>
        [System.AttributeUsage(System.AttributeTargets.All)]
        public class PrintAttribute : System.Attribute
        {
            /// <summary>
            /// Gets the value of the message
            /// </summary>
            public string Message { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="PrintAttribute"/> class
            /// </summary>
            /// <param name="message">The message</param>
            public PrintAttribute(string message)
            {
                Message = message;
                System.Console.WriteLine(message);
            }
        }
    }
}