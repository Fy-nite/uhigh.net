namespace uhigh.StdLib
{
    public static class Funnies
    {
        [System.AttributeUsage(System.AttributeTargets.All)]
        public class PrintAttribute : System.Attribute
        {
            public string Message { get; }

            public PrintAttribute(string message)
            {
                Message = message;
                System.Console.WriteLine(message);
            }
        }
    }
}