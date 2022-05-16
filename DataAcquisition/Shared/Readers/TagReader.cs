using System;
using System.Collections.Generic;
using System.IO;

namespace DataAcquisition.Shared.Readers
{
    public static class TagReader
    {
        public static Dictionary<int, string> Read()
        {
            var filepath = FilePaths.TagsFilePath;
            var count = FileHelper.CountNonEmptyLinesInTextFile(filepath);
            var dict = new Dictionary<int, string>((int)count);

            FileHelper.DoForEachRowInTextFile(filepath, 3, (arr) =>
            {
                var id = int.Parse(arr[0]);
                var text = arr[1];

                dict.Add(id, text);
            });

            dict.TrimExcess();
            return dict;
        }
    }
}
