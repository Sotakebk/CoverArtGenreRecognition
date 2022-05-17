using DataAcquisition.Shared.Structures;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataAcquisition.Shared.Readers
{
    public static class CoverArtReader
    {
        public static Dictionary<int, CoverArt> Read()
        {
            var IdsToSave = ReadFrontCoverIds();

            var filePath = FilePaths.CoverArtsFilePath;
            var count = FileHelper.CountNonEmptyLinesInTextFile(filePath);
            var dict = new Dictionary<int, CoverArt>((int)count);

            FileHelper.DoForEachRowInTextFile(filePath, 12, (arr) =>
            {
                var id = long.Parse(arr[0]);
                var releaseId = int.Parse(arr[1]);
                var fileType = FileTypeToEnum(arr[7]);
                if (fileType != ImageFileType.Png && fileType != ImageFileType.Jpg)
                    return;

                if (IdsToSave.Contains(id))
                {
                    if (dict.ContainsKey(releaseId))
                        dict.Remove(releaseId);
                    dict.Add(releaseId, new CoverArt(id, fileType));
                }
            });

            dict.TrimExcess();
            return dict;
        }

        private static HashSet<long> ReadFrontCoverIds()
        {
            var filePath = FilePaths.CoverArtTypesFilePath;
            var count = FileHelper.CountNonEmptyLinesInTextFile(filePath);
            var set = new HashSet<long>((int)count);

            FileHelper.DoForEachRowInTextFile(filePath, 2, (arr) =>
            {
                var id = long.Parse(arr[0]);
                var typeId = int.Parse(arr[1]);

                if (typeId == 1 && !set.Contains(id))
                    set.Add(id);
                if (typeId != 1 && set.Contains(id))
                    set.Remove(id);
            });

            set.TrimExcess();
            return set;
        }

        private static ImageFileType FileTypeToEnum(string text)
        {
            return text switch
            {
                @"image/jpeg" => ImageFileType.Jpg,
                @"image/png" => ImageFileType.Png,
                @"application/pdf" => ImageFileType.pdf,
                @"image/gif" => ImageFileType.gif,
                _ => ImageFileType.unknown,
            };
        }
    }
}
