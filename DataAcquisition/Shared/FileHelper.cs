using System;
using System.IO;

namespace DataAcquisition.Shared
{
    public static class FileHelper
    {
        public static long CountNonEmptyLinesInTextFile(string filePath)
        {
            long count = 0;
            using (var fileStream = File.OpenText(filePath))
            {
                do
                {
                    var line = fileStream.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                        count++;
                }
                while (!fileStream.EndOfStream);
            }
            return count;
        }

        public static void DoForEachLineInTextFile(string filePath, Action<string> action)
        {
            using (var fileStream = File.OpenText(filePath))
            {
                while (!fileStream.EndOfStream)
                {
                    var line = fileStream.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    action(line);
                }
            }
        }

        public static void DoForEachRowInTextFile(string filePath, int columns, Action<string[]> action, string separator = "\t")
        {
            using (var fileStream = File.OpenText(filePath))
            {
                while (!fileStream.EndOfStream)
                {
                    var line = fileStream.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var arr = line.Split(separator, StringSplitOptions.TrimEntries);
                    if (arr.Length != columns)
                        throw new InvalidDataException();

                    action(arr);
                }
            }
        }
    }
}
