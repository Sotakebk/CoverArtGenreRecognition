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
            count = joined.Count(i => i.Item3 != GenreFlags.Empty);
            Console.WriteLine($"Releases with genre: {joined.Count(i => i.Item3 != GenreFlags.Empty)}");
            count = joined.Length - count;
            Console.WriteLine($"Releases without genre: {count}");
            count = joined.Count(i => i.Item2 != null);
            Console.WriteLine($"Releases with cover art: {count}");
            count = joined.Length - count;
            Console.WriteLine($"Releases without cover art: {count}");

            Console.WriteLine("Merging down releases by release groups...");
            releases = null;
            covers = null;
            releaseTags = null;
            var merged = Merge(joined, genres, releaseGroupTags);
            count = merged.Length;
            Console.WriteLine($"Release groups with cover art: {count}");
            count = merged.Count(i => i.Item3 != GenreFlags.Empty);
            Console.WriteLine($"Release groups with cover art and genre: {count}");
            count = merged.Length - count;
            Console.WriteLine($"Release groups with cover art and without genre: {count}");
            Console.WriteLine("Sorting...");
            var sorted = merged.OrderByDescending(i => GenreHelper.CountSetFlags(i.Item3));
            Console.WriteLine("Saving...");
            Save(sorted);

            QuitImmediately("Done!");
        }

        private static void Save(IEnumerable<(Release, CoverArt, GenreFlags)> entries)
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
                $"{string.Join(",", GenreHelper.GetGenreNames())}");

            foreach (var entry in entries)
            {
                if (entry.Item3 == GenreFlags.Empty)
                    continue;

                fileStream.WriteLine(
                    $"{entry.Item1.GroupId}," +
                    $"{entry.Item1.Guid}," +
                    $"{entry.Item2.ImageId}," +
                    $"{(int)entry.Item2.FileType}," +
                    $"{GenreHelper.CountSetFlags(entry.Item3)}," +
                    $"{string.Join(",", GenreHelper.GetGenresAs01Array(entry.Item3))}");
            }

            fileStream.Flush();
        }

        private static (Release, CoverArt, GenreFlags)[] Merge((Release, CoverArt?, GenreFlags)[] joined,
            Dictionary<int, GenreFlags> genres, (int, int)[] releaseGroupTags)
        {
            Array.Sort(joined, (x, y) => x.Item1.GroupId.CompareTo(y.Item1.GroupId));

            var count = Algorithms.CountDistinctInSortedSet(joined,
                (x, y) => x.Item1.GroupId.CompareTo(y.Item1.GroupId));

            var list = new List<(Release, CoverArt, GenreFlags)>((int)count);

            Algorithms.ForEachDistinctGroupInSortedArray(joined, (x, y) => x.Item1.GroupId.CompareTo(y.Item1.GroupId),
                (min, max) =>
                {
                    var group = MergeRows(joined[min..(max + 1)], genres, releaseGroupTags);
                    if (group?.Item2 != null)
                        list.Add(group.Value);
                });

            list.TrimExcess();
            return list.ToArray();
        }

        private static (Release, CoverArt, GenreFlags)? MergeRows(IEnumerable<(Release, CoverArt?, GenreFlags)> rows,
            Dictionary<int, GenreFlags> genres, (int, int)[] releaseGroupTags)
        {
            CoverArt coverArt = default;
            Release release = default;
            var genreFlags = GenreFlags.Empty;
            var coverArtFound = false;

            foreach (var (rowRelease, rowCoverArt, rowGenre) in rows)
            {
                genreFlags |= rowGenre;
                if (coverArtFound || rowCoverArt == null)
                    continue;

                release = rowRelease;
                coverArt = rowCoverArt.Value;
                coverArtFound = true;
            }

            if (!coverArtFound)
                return null;

            var (min, max) = GetIndexRangeInSortedRelationArray(releaseGroupTags, release.GroupId);

            if (min != -1)
                for (var id = min; id <= max; id++)
                    genreFlags |= genres[releaseGroupTags[id].Item2];

            return (release, coverArt, genreFlags);
        }

        private static (Release release, CoverArt? coverArt, GenreFlags genreFlags)[] Join(
            IReadOnlyDictionary<int, Release> releases,
            IReadOnlyDictionary<int, CoverArt> covers,
            IReadOnlyDictionary<int, GenreFlags> genres,
            (int releaseId, int tagId)[] releaseTags)
        {
            var arr = new (Release release, CoverArt? coverArt, GenreFlags genreFlags)[releases.Count];

            var i = 0;
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

                arr[i] = (release, coverArt, genre);
                i++;
            }

            return arr;
        }

        private static (int minId, int maxId) GetIndexRangeInSortedRelationArray(
            (int objectId, int tagId)[] tagRelations, int targetId)
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

        private static (int objectId, int tagId)[] TrimRelations(IReadOnlyDictionary<int, GenreFlags> genres,
            IEnumerable<(int objectId, int tagId)> relations)
        {
            // genres - Index, Genre
            // relations - Index of something, Index of genre
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
