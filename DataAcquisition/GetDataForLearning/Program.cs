using DataAcquisition.Shared;
using DataAcquisition.Shared.Readers;
using DataAcquisition.Shared.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataAcquisition.GetDataForLearning
{
    internal class Program
    {
        private struct DataRow
        {
            public Guid ReleaseGuid;
            public long ImageId;
            public Genre genre;
        }

        private static void Main()
        {
            FilePaths.SetUp();
            if (!FilePaths.TestFilePresence(out var arr))
            {
                foreach (var file in arr)
                {
                    Console.WriteLine($"Missing file: {file}");
                }
                QuitImmediately("Missing files! Exiting...");
            }
            var count = 0;

            Console.WriteLine("Loading tags...");
            var tags = TagReader.Read();
            Console.WriteLine("Loading tag relations...");
            var releaseTags = TagRelationReader.ReadReleaseToTagRelations();
            var releaseGroupTags = TagRelationReader.ReadReleaseGroupToTagRelations();
            Console.WriteLine("Translating tags to genres...");
            var genres = TagsToGenres(tags);
            tags = null;

            Console.WriteLine("Trimming tag relations...");
            count = releaseTags.Length;
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
            var ordered = releases.OrderBy(k => k.Key);
            Console.WriteLine("Joining releases, covers and genres...");
            var joined = Join(releases, covers, genres, releaseTags);
            count = joined.Count(i => i.Item3 != Genre.Empty);
            Console.WriteLine($"Releases with genre: {joined.Count(i => i.Item3 != Genre.Empty)}");
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
            count = merged.Count(i => i.Item3 != Genre.Empty);
            Console.WriteLine($"Release groups with genre: {count}");
            count = merged.Length - count;
            Console.WriteLine($"Release groups without genre: {count}");
            Console.WriteLine("Sorting...");
            var sorted = merged.OrderByDescending(i => GenreHelper.CountSetFlags(i.Item3));
            Console.WriteLine("Saving...");
            Save(sorted);

            QuitImmediately("Done!");
        }

        private static void Save(IEnumerable<(Release, CoverArt, Genre)> entries)
        {
            var filePath = FilePaths.FilenameToSavePath("valid_data_list.txt");

            using var fileStream = File.CreateText(filePath);
            foreach (var entry in entries)
            {
                if (entry.Item3 != Genre.Empty)
                    fileStream.WriteLine($"{entry.Item1.Guid}\t{entry.Item2.ImageId}\t{(int)entry.Item2.FileType}\t{(int)entry.Item3}");
            }
            fileStream.Flush();
        }

        private static (Release, CoverArt, Genre)[] Merge((Release, CoverArt?, Genre)[] joined, Dictionary<int, Genre> genres, (int, int)[] releaseGroupTags)
        {
            var groups = joined.GroupBy(i => i.Item1.GroupId);
            var list = new List<(Release, CoverArt, Genre)>(groups.Count());
            foreach (var group in groups)
            {
                var merged = MergeRows(group, genres, releaseGroupTags);
                if (merged == null)
                    continue;
                else
                    list.Add(merged.Value);
            }

            return list.ToArray();
        }

        private static (Release, CoverArt, Genre)? MergeRows(IEnumerable<(Release, CoverArt?, Genre)> rows, Dictionary<int, Genre> genres, (int, int)[] releaseGroupTags)
        {
            CoverArt coverArt = default;
            Release release = default;
            Genre genre = Genre.Empty;
            bool coverArtFound = false;

            foreach (var row in rows)
            {
                genre |= row.Item3;
                if (!coverArtFound && row.Item2 != null)
                {
                    release = row.Item1;
                    coverArt = row.Item2.Value;
                    coverArtFound = true;
                }
            }

            if (!coverArtFound)
                return null;

            (int min, int max) = GetIndexRangeInSortedRelationArray(releaseGroupTags, release.GroupId);

            if (min != -1)
            {
                for (int z = min; z <= max; z++)
                    genre |= genres[releaseGroupTags[z].Item2];
            }

            return (release, coverArt, genre);
        }

        private static (Release, CoverArt?, Genre)[] Join(
            Dictionary<int, Release> releases,
            Dictionary<int, CoverArt> covers,
            Dictionary<int, Genre> genres,
            (int, int)[] releaseTags)
        {
            var arr = new (Release, CoverArt?, Genre)[releases.Count];

            int i = 0;
            foreach (var pair in releases)
            {
                var release = pair.Value;
                CoverArt? coverArt = null;
                if (covers.ContainsKey(pair.Key))
                    coverArt = covers[pair.Key];

                var genre = Genre.Empty;
                (int min, int max) = GetIndexRangeInSortedRelationArray(releaseTags, pair.Key);

                if (min != -1)
                {
                    for (int z = min; z <= max; z++)
                        genre |= genres[releaseTags[z].Item2];
                }

                arr[i] = (release, coverArt, genre);
                i++;
            }

            return arr;
        }

        private class TagPairComparer : IComparer<(int, int)>
        {
            public int Compare((int, int) x, (int, int) y) => Comparer<int>.Default.Compare(x.Item1, y.Item1);
        }

        private static (int, int) GetIndexRangeInSortedRelationArray((int, int)[] tagRelations, int targetId)
        {
            var index = Array.BinarySearch(tagRelations, (targetId, -1), new TagPairComparer());

            if (index < 0)
                return (-1, -1);

            var min = index;
            var max = index;

            var newMin = index;
            while (newMin >= 0 && tagRelations[newMin].Item1 == targetId)
            {
                min = newMin;
                newMin--;
            }
            var newMax = index;
            while (newMax < tagRelations.Length && tagRelations[newMax].Item1 == targetId)
            {
                min = newMin;
                newMax++;
            }

            return (min, max);
        }

        private static Dictionary<int, Genre> TagsToGenres(Dictionary<int, string> tags)
        {
            var dict = new Dictionary<int, Genre>(tags.Count);

            foreach (var pair in tags)
            {
                var genre = GenreHelper.GetGenreFromString(pair.Value);
                if (genre != Genre.Empty)
                    dict.Add(pair.Key, genre);
            }

            dict.TrimExcess();
            return dict;
        }

        private static (int, int)[] TrimRelations(Dictionary<int, Genre> genres, (int, int)[] relations)
        {
            // genres - Index, Genre
            // relations - Index of something, Index of genre
            var list = new List<(int, int)>(genres.Count);
            foreach (var pair in relations)
            {
                if (genres.TryGetValue(pair.Item2, out var genre))
                {
                    if (genre != Genre.Empty)
                        list.Add(pair);
                }
            }

            return list.ToArray();
        }

        private static void QuitImmediately(string message = null, int code = 0)
        {
            if (message != null)
                Console.WriteLine(message);
            Console.WriteLine("Press any button to exit...");
            Console.Read();

            Environment.Exit(code);
        }
    }
}
