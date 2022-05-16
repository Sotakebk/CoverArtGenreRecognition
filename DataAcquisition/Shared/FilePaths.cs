using System;
using System.Collections.Generic;
using System.IO;

namespace DataAcquisition.Shared
{
    public static class FilePaths
    {
        private static readonly string pathToReleases = @"mbdump\mbdump\release";
        private static readonly string pathToTags = @"mbdump-derived\mbdump\tag";
        private static readonly string pathToReleaseTags = @"mbdump-derived\mbdump\release_tag";
        private static readonly string pathToReleaseGroupTags = @"mbdump-derived\mbdump\release_group_tag";
        private static readonly string pathToCoverArts = @"mbdump-cover-art-archive\mbdump\cover_art_archive.cover_art";
        private static readonly string pathToCoverArtTypes = @"mbdump-cover-art-archive\mbdump\cover_art_archive.cover_art_type";

        private static string readFolder = "";
        private static string saveFolder = "";

        public static void SetUp(string readFolderPath = null, string saveFolderPath = null)
        {
            readFolder = readFolderPath ?? Environment.CurrentDirectory;
            saveFolder = saveFolderPath ?? Path.Combine(Environment.CurrentDirectory, "saved");

            Directory.CreateDirectory(saveFolder);
        }

        public static bool TestFilePresence(out string[] missingFiles)
        {
            var arr = new[] { ReleasesFilePath, TagsFilePath, ReleaseTagsFilePath, ReleaseGroupTagsFilePath, CoverArtsFilePath, CoverArtTypesFilePath };
            var missing = new List<string>(arr.Length);

            foreach (var path in arr)
            {
                if (!File.Exists(path))
                    missing.Add(Path.GetFullPath(path));
            }

            missingFiles = missing.ToArray();
            return missing.Count == 0;
        }

        public static string ReleasesFilePath => Path.Combine(readFolder, pathToReleases);
        public static string TagsFilePath => Path.Combine(readFolder, pathToTags);
        public static string ReleaseTagsFilePath => Path.Combine(readFolder, pathToReleaseTags);
        public static string ReleaseGroupTagsFilePath => Path.Combine(readFolder, pathToReleaseGroupTags);
        public static string CoverArtsFilePath => Path.Combine(readFolder, pathToCoverArts);
        public static string CoverArtTypesFilePath => Path.Combine(readFolder, pathToCoverArtTypes);

        public static string FilenameToSavePath(string filename) => Path.Combine(saveFolder, filename);
    }
}
