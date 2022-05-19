using System;
using System.IO;
using System.Linq;

namespace DataAcquisition.Shared
{
    public static class FilePaths
    {
        private const string PathToReleases = @"mbdump\mbdump\release";
        private const string PathToTags = @"mbdump-derived\mbdump\tag";
        private const string PathToReleaseTags = @"mbdump-derived\mbdump\release_tag";
        private const string PathToReleaseGroupTags = @"mbdump-derived\mbdump\release_group_tag";
        private const string PathToCoverArts = @"mbdump-cover-art-archive\mbdump\cover_art";
        private const string PathToCoverArtTypes = @"mbdump-cover-art-archive\mbdump\cover_art_type";

        private static string _readFolder = "";
        private static string _saveFolder = "";

        public static string ReleasesFilePath => Path.Combine(_readFolder, PathToReleases);
        public static string TagsFilePath => Path.Combine(_readFolder, PathToTags);
        public static string ReleaseTagsFilePath => Path.Combine(_readFolder, PathToReleaseTags);
        public static string ReleaseGroupTagsFilePath => Path.Combine(_readFolder, PathToReleaseGroupTags);
        public static string CoverArtsFilePath => Path.Combine(_readFolder, PathToCoverArts);
        public static string CoverArtTypesFilePath => Path.Combine(_readFolder, PathToCoverArtTypes);

        public static void SetUp(string readFolderPath = null, string saveFolderPath = null)
        {
            _readFolder = readFolderPath ?? Environment.CurrentDirectory;
            _saveFolder = saveFolderPath ?? Path.Combine(Environment.CurrentDirectory, "saved");

            Directory.CreateDirectory(_saveFolder);
        }

        public static string[] ListMissingFiles()
        {
            var arr = new[]
            {
                ReleasesFilePath, TagsFilePath, ReleaseTagsFilePath, ReleaseGroupTagsFilePath, CoverArtsFilePath,
                CoverArtTypesFilePath
            };

            return arr.Select(i => Path.GetFullPath(i))
                .Where(i => !File.Exists(i))
                .ToArray();
        }

        public static string FilenameToSavePath(string filename)
        {
            return Path.Combine(_saveFolder, filename);
        }
    }
}
