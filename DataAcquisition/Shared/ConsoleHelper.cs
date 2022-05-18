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

        public static bool TestIfFilesExist()
        {
            var missingFiles = FilePaths.ListMissingFiles();

            if (missingFiles.Length == 0)
                return true;

            foreach (var file in missingFiles) Console.WriteLine($"Missing file: {file}");

            return false;
        }
    }
}
