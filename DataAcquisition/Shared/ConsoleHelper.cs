using System;

namespace DataAcquisition.Shared
{
    public static class ConsoleHelper
    {
        public static void QuitImmediately(string message = null, int code = 0)
        {
            if (message != null)
                Console.WriteLine(message);
            Console.WriteLine("Press any button to exit...");
            Console.Read();

            Environment.Exit(code);
        }
    }
}
