using System;
using System.IO;

namespace DataAcquisition.Shared
{
    public static class FileHelper
    {
        public static void DoForEachNotEmptyLineInTextFile(string filePath, Action<string> action)
        {
            using var fileStream = File.OpenText(filePath);
            while (!fileStream.EndOfStream)
            {
                var line = fileStream.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                action(line);
            }
        }

        public static long CountNotEmptyLinesInTextFile(string filePath)
        {
            long count = 0;
            DoForEachNotEmptyLineInTextFile(filePath, _ => { count++; });
            return count;
        }

        public static void DoForEachRowInTextFile(string filePath, int columns, Action<string[]> action,
            string separator = "\t")
        {
            DoForEachNotEmptyLineInTextFile(filePath, line =>
            {
                var arr = line.Split(separator, StringSplitOptions.TrimEntries);
                if (arr.Length != columns)
                    throw new InvalidDataException();

                action(arr);
            });
        }
    }
}
