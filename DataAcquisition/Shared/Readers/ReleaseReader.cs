using DataAcquisition.Shared.Structures;
using System;
using System.Collections.Generic;

namespace DataAcquisition.Shared.Readers
{
    public static class ReleaseReader
    {
        public static Dictionary<int, Release> Read()
        {
            var filePath = FilePaths.ReleasesFilePath;
            var count = FileHelper.CountNotEmptyLinesInTextFile(filePath);
            var dict = new Dictionary<int, Release>((int)count);

            FileHelper.DoForEachRowInTextFile(filePath, 14, arr =>
            {
                var id = int.Parse(arr[0]);
                var guid = Guid.Parse(arr[1]);
                var groupId = int.Parse(arr[4]);

                if (!dict.ContainsKey(id))
                    dict.Add(id, new Release(guid, groupId));
            });

            dict.TrimExcess();
            return dict;
        }
    }
}
