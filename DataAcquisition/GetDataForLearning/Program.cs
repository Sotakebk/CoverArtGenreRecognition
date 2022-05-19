using DataAcquisition.Shared;
using DataAcquisition.Shared.Readers;
using DataAcquisition.Shared.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DataAcquisition.Shared.ConsoleHelper;

namespace DataAcquisition.GetDataForLearning
{
    internal class Program
    {
        private struct ReleaseGroupRow
        {
            public readonly Guid Guid;
            public readonly long? CoverId;
            public readonly int GroupId;
            public readonly GenreFlags Genre;
            public readonly ImageFileType ImageFileType;

            public ReleaseGroupRow(Guid guid, long? coverId, int groupId, GenreFlags genre, ImageFileType imageFileType)
            {
                Guid = guid;
                CoverId = coverId;
                GroupId = groupId;
                Genre = genre;
                ImageFileType = imageFileType;
            }
        }

        private struct ReleaseRow
        {
            public readonly Guid Guid;
            public readonly long? CoverId;
            public readonly int GroupId;
            public readonly GenreFlags Genre;
            public readonly ImageFileType ImageFileType;

            public ReleaseRow(Guid guid, long? coverId, int groupId, GenreFlags genre, ImageFileType imageFileType)
            {
                Guid = guid;
                CoverId = coverId;
                GroupId = groupId;
                Genre = genre;
                ImageFileType = imageFileType;
            }
        }

        private static void Main()
        {
            FilePaths.SetUp();

            if (!TestIfFilesExist()) QuitImmediately("Missing files! Exiting...");

            Console.WriteLine("Loading tags...");
            var tags = TagReader.Read();
            Console.WriteLine("Loading tag relations...");
            var releaseTags = TagRelationReader.ReadReleaseToTagRelations();
            var releaseGroupTags = TagRelationReader.ReadReleaseGroupToTagRelations();
            Console.WriteLine("Translating tags to genres...");
            var genres = TagsToGenres(tags);
            tags = null;
            GC.Collect();

            Console.WriteLine("Trimming tag relations...");
            var count = releaseTags.Length;
            Console.WriteLine($"Release to tag relation count: {count}");
            releaseTags = TrimRelations(genres, releaseTags);
            count = releaseTags.Length;
            Console.WriteLine($"Release to tag relation count after trimming: {count}");
            count = releaseGroupTags.Length;
            Console.WriteLine($"Release group to tag relation count: {count}");
            releaseGroupTags = TrimRelations(genres, releaseGroupTags);
            count = releaseGroupTags.Length;
            Console.WriteLine($"Release group to tag relation count after trimming: {count}");
            Console.WriteLine("Sorting relations...");
            Array.Sort(releaseTags, new TagPairComparer());
            Array.Sort(releaseGroupTags, new TagPairComparer());

            Console.WriteLine("Loading covers...");
            var covers = CoverArtReader.Read();
            Console.WriteLine("Loading releases..");
            var releases = ReleaseReader.Read();
            Console.WriteLine("Joining releases, covers and genres...");
            var joined = Join(releases, covers, genres, releaseTags);
            count = joined.Count(i => i.Genre != GenreFlags.Empty);
            Console.WriteLine($"Releases with genre: {joined.Count(i => i.Genre != GenreFlags.Empty)}");
            count = joined.Length - count;
            Console.WriteLine($"Releases without genre: {count}");
            count = joined.Count(i => i.CoverId != null);
            Console.WriteLine($"Releases with cover art: {count}");
            count = joined.Length - count;
            Console.WriteLine($"Releases without cover art: {count}");

            Console.WriteLine("Merging down releases by release groups...");
            releases = null;
            covers = null;
            releaseTags = null;
            GC.Collect();
            var merged = MergeByGroupId(joined, genres, releaseGroupTags);
            Console.WriteLine($"Release groups: {merged.Length}");
            Console.WriteLine("Release groups with cover art: " +
                              $"{merged.Count(i => i.ImageFileType != ImageFileType.Unknown)}");
            Console.WriteLine("Release groups with cover art and genre: " +
                              $"{merged.Count(i => i.ImageFileType == ImageFileType.Unknown && i.Genre != GenreFlags.Empty)}");
            Console.WriteLine("Release groups with cover art and without genre: " +
                              $"{merged.Count(i => i.ImageFileType is ImageFileType.Jpg or ImageFileType.Png && i.Genre != GenreFlags.Empty)}");
            Console.WriteLine("Sorting...");
            Array.Sort(merged, (a, b) => -GenreHelper.CountSetFlags(a.Genre).CompareTo(GenreHelper.CountSetFlags(b.Genre)));
            Console.WriteLine("Saving...");
            Save(merged);

            QuitImmediately("Done!");
        }

        private static void Save(IEnumerable<ReleaseGroupRow> entries)
        {
            var filePath = FilePaths.FilenameToSavePath("valid_data_list.csv");

            using var fileStream = File.CreateText(filePath);

            // header
            fileStream.WriteLine(
                "GroupID," +
                "ReleaseGUID," +
                "CoverArtID," +
                "ImageType," +
                "GenreCount," +
                "Class," +
                $"{string.Join(",", GenreHelper.GetGenreNames())}");

            foreach (var entry in entries)
            {
                fileStream.WriteLine(
                    $"{entry.GroupId}," +
                    $"{entry.Guid}," +
                    $"{entry.CoverId ?? 0}," +
                    $"{(int)entry.ImageFileType}," +
                    $"{GenreHelper.CountSetFlags(entry.Genre)}," +
                    $"{(int)entry.Genre}," +
                    $"{string.Join(",", GenreHelper.GetGenresAs01Array(entry.Genre))}");
            }

            fileStream.Flush();
        }

        private static ReleaseGroupRow[] MergeByGroupId(ReleaseRow[] joinedTables, Dictionary<int, GenreFlags> genres, (int, int)[] releaseGroupTags)
        {
            static int Comparison(ReleaseRow x, ReleaseRow y) => x.GroupId.CompareTo(y.GroupId);

            Array.Sort(joinedTables, Comparison);
            var count = Algorithms.CountDistinctInSortedSet(joinedTables, Comparison);
            var list = new List<ReleaseGroupRow>((int)count);

            Algorithms.ForEachDistinctGroupInSortedArray(joinedTables, Comparison,
                (min, max) =>
                {
                    var group = MergeReleases(joinedTables[min..(max + 1)], genres, releaseGroupTags);
                    list.Add(group);
                });

            list.TrimExcess();
            return list.ToArray();
        }

        private static ReleaseGroupRow MergeReleases(ReleaseRow[] rows, Dictionary<int, GenreFlags> genres, (int, int)[] releaseGroupTags)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (rows.Length == 0)
                throw new ArgumentException("Array of length 0 provided", nameof(rows));

            var guid = rows[0].Guid;
            var groupId = rows[0].GroupId;
            var genreFlags = GenreFlags.Empty;
            var imageFileType = ImageFileType.Unknown;
            long? coverId = null;

            foreach (var row in rows)
            {
                genreFlags |= row.Genre;

                if (coverId == null && row.CoverId != null)
                {
                    coverId = row.CoverId;
                    guid = row.Guid;
                    imageFileType = row.ImageFileType;
                }
            }

            var (min, max) = GetIndexRangeInSortedRelationArray(releaseGroupTags, groupId);

            if (min != -1)
                for (var id = min; id <= max; id++)
                    genreFlags |= genres[releaseGroupTags[id].Item2];

            return new ReleaseGroupRow(guid, coverId, groupId, genreFlags, imageFileType);
        }

        private static ReleaseRow[] Join(Dictionary<int, Release> releases, Dictionary<int, CoverArt> covers,
            Dictionary<int, GenreFlags> genres, (int releaseId, int tagId)[] releaseTags)
        {
            var list = new List<ReleaseRow>(releases.Count);

            foreach (var (releaseId, release) in releases)
            {
                CoverArt? coverArt = null;
                if (covers.ContainsKey(releaseId))
                    coverArt = covers[releaseId];

                var genre = GenreFlags.Empty;
                var (min, max) = GetIndexRangeInSortedRelationArray(releaseTags, releaseId);

                if (min != -1)
                    for (var id = min; id <= max; id++)
                        genre |= genres[releaseTags[id].tagId];

                list.Add(new ReleaseRow(release.Guid, coverArt?.ImageId, release.GroupId, genre, coverArt?.FileType ?? ImageFileType.Unknown));
            }

            return list.ToArray();
        }

        private static (int minId, int maxId) GetIndexRangeInSortedRelationArray((int objectId, int tagId)[] tagRelations, int targetId)
        {
            var index = Array.BinarySearch(tagRelations, (targetId, -1), new TagPairComparer());

            if (index < 0)
                return (-1, -1);

            var min = index;
            var max = index;

            var newMin = index;
            while (newMin >= 0 && tagRelations[newMin].objectId == targetId)
            {
                min = newMin;
                newMin--;
            }

            var newMax = index;
            while (newMax < tagRelations.Length && tagRelations[newMax].objectId == targetId)
            {
                min = newMin;
                newMax++;
            }

            return (min, max);
        }

        private static Dictionary<int, GenreFlags> TagsToGenres(Dictionary<int, string> tags)
        {
            var dict = new Dictionary<int, GenreFlags>(tags.Count);

            foreach (var (key, value) in tags)
            {
                var genre = GenreHelper.GetGenreFromString(value);
                if (genre != GenreFlags.Empty)
                    dict.Add(key, genre);
            }

            dict.TrimExcess();
            return dict;
        }

        // remove relations which point to GenreFlags.Empty anyway
        private static (int objectId, int tagId)[] TrimRelations(Dictionary<int, GenreFlags> genres, (int objectId, int tagId)[] relations)
        {
            var list = new List<(int objectId, int tagId)>(genres.Count);
            foreach (var pair in relations)
                if (genres.TryGetValue(pair.tagId, out var genre) && genre != GenreFlags.Empty)
                    list.Add(pair);

            list.TrimExcess();
            return list.ToArray();
        }

        private class TagPairComparer : IComparer<(int, int)>
        {
            public int Compare((int, int) x, (int, int) y)
            {
                return Comparer<int>.Default.Compare(x.Item1, y.Item1);
            }
        }
    }
}
