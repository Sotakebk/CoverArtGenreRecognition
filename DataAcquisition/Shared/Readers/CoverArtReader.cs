using DataAcquisition.Shared.Structures;
using System.Collections.Generic;

namespace DataAcquisition.Shared.Readers
{
    public static class CoverArtReader
    {
        public static Dictionary<int, CoverArt> Read()
        {
            var frontCoverIds = ReadFrontCoverIds();

            var filePath = FilePaths.CoverArtsFilePath;
            var count = FileHelper.CountNotEmptyLinesInTextFile(filePath);
            var dict = new Dictionary<int, CoverArt>((int)count);

            FileHelper.DoForEachRowInTextFile(filePath, 12, arr =>
            {
                var id = long.Parse(arr[0]);
                var releaseId = int.Parse(arr[1]);
                var fileType = FileTypeToEnum(arr[7].ToLowerInvariant());

                if (fileType is not (ImageFileType.Png or ImageFileType.Jpg) || !frontCoverIds.Contains(id))
                    return;

                if (dict.ContainsKey(releaseId)) // replace old entry with a newer one
                    dict.Remove(releaseId);

                dict.Add(releaseId, new CoverArt(id, fileType));
            });

            dict.TrimExcess();
            return dict;
        }

        private static HashSet<long> ReadFrontCoverIds()
        {
            var filePath = FilePaths.CoverArtTypesFilePath;
            var count = FileHelper.CountNotEmptyLinesInTextFile(filePath);
            var set = new HashSet<long>((int)count);

            FileHelper.DoForEachRowInTextFile(filePath, 2, arr =>
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
                @"application/pdf" => ImageFileType.Pdf,
                @"image/gif" => ImageFileType.Gif,
                _ => ImageFileType.Unknown
            };
        }
    }
}
