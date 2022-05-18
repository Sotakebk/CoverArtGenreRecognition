using System.Collections.Generic;

namespace DataAcquisition.Shared.Readers
{
    public static class TagRelationReader
    {
        public static (int releaseId, int tagId)[] ReadReleaseToTagRelations()
        {
            var filepath = FilePaths.ReleaseTagsFilePath;
            var count = FileHelper.CountNotEmptyLinesInTextFile(filepath);
            var list = new List<(int, int)>((int)count);

            FileHelper.DoForEachRowInTextFile(filepath, 4, arr =>
            {
                var id = int.Parse(arr[0]);
                var id2 = int.Parse(arr[1]);

                list.Add((id, id2));
            });

            return list.ToArray();
        }

        public static (int groupId, int tagId)[] ReadReleaseGroupToTagRelations()
        {
            var filepath = FilePaths.ReleaseGroupTagsFilePath;
            var count = FileHelper.CountNotEmptyLinesInTextFile(filepath);
            var list = new List<(int, int)>((int)count);

            FileHelper.DoForEachRowInTextFile(filepath, 4, arr =>
            {
                var id = int.Parse(arr[0]);
                var id2 = int.Parse(arr[1]);

                list.Add((id, id2));
            });

            return list.ToArray();
        }
    }
}
