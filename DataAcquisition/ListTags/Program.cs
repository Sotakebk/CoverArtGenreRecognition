using DataAcquisition.Shared;
using DataAcquisition.Shared.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DataAcquisition.Shared.ConsoleHelper;

namespace DataAcquisition.ListTags
{
    internal class Program
    {
        private static void Main()
        {
            FilePaths.SetUp();

            if (!TestIfFilesExist()) QuitImmediately("Missing files! Exiting...");

            Console.WriteLine("Loading tags...");
            var tags = TagReader.Read();
            Console.WriteLine($"Tag count: {tags.Count}");

            Console.WriteLine("Loading release to tag references...");
            var releaseReferences = TagRelationReader.ReadReleaseToTagRelations();
            Console.WriteLine("Loading release group to tag references...");
            var releaseGroupReferences = TagRelationReader.ReadReleaseGroupToTagRelations();

            {
                Console.WriteLine("Calculating reference counts for all releases and groups...");
                var referenceCounts = new Dictionary<int, int>(tags.Count);
                IncrementCounters(referenceCounts, releaseReferences, tags);
                IncrementCounters(referenceCounts, releaseGroupReferences, tags);

                Console.WriteLine("Sorting...");
                var entries = referenceCounts.Select(i => (count: i.Value, tag: tags[i.Key]))
                    .OrderByDescending(i => i.count)
                    .ToArray();

                Console.WriteLine("Saving...");
                SaveTags(entries.Where(i => GenreHelper.IsStringInAnyGroup(i.tag)), "known_tags_all_references.txt");
                SaveTags(entries.Where(i => !GenreHelper.IsStringInAnyGroup(i.tag)), "unknown_tags_all_references.txt");
            }

            {
                Console.WriteLine("Calculating reference counts for releases and groups with no known genre...");
                var releasesWithKnownGenre = GetSetOfObjectsWithKnownGenre(releaseReferences, tags);
                var releaseGroupsWithKnownGenre = GetSetOfObjectsWithKnownGenre(releaseGroupReferences, tags);

                var releaseWithNoGenreRefs = releaseReferences.Where(tuple => !releasesWithKnownGenre.Contains(tuple.releaseId)).ToArray();
                var releaseGroupsWithNoGenreRefs = releaseGroupReferences.Where(tuple => !releaseGroupsWithKnownGenre.Contains(tuple.groupId)).ToArray();

                var referenceCounts = new Dictionary<int, int>(tags.Count);
                IncrementCounters(referenceCounts, releaseWithNoGenreRefs, tags);
                IncrementCounters(referenceCounts, releaseGroupsWithNoGenreRefs, tags);

                Console.WriteLine("Sorting...");
                var entries = referenceCounts.Select(i => (count: i.Value, tag: tags[i.Key]))
                    .OrderByDescending(i => i.count)
                    .ToArray();

                Console.WriteLine("Saving...");
                SaveTags(entries.Where(i => GenreHelper.IsStringInAnyGroup(i.tag)), "known_tags_unknown_genre_references.txt");
                SaveTags(entries.Where(i => !GenreHelper.IsStringInAnyGroup(i.tag)), "unknown_tags_unknown_genre_references.txt");
            }

            QuitImmediately("Work done!");
        }

        private static HashSet<int> GetSetOfObjectsWithKnownGenre((int, int)[] references, Dictionary<int, string> tags)
        {
            var set = new HashSet<int>();
            foreach (var (objectId, tagId) in references)
            {
                var tag = tags[tagId];
                if (GenreHelper.IsStringInAnyGroup(tag))
                {
                    set.Add(objectId);
                }
            }

            return set;
        }

        private static void SaveTags(IEnumerable<(int, string)> entries, string file)
        {
            var filePath = FilePaths.FilenameToSavePath(file);

            using var fileStream = File.CreateText(filePath);
            foreach (var (count, text) in entries) fileStream.WriteLine($"{count}\t{text}");
            fileStream.Flush();
        }

        private static void IncrementCounters(IDictionary<int, int> countDictionary,
            IEnumerable<(int, int)> referencePair, IReadOnlyDictionary<int, string> tags)
        {
            foreach (var pair in referencePair)
            {
                var id = pair.Item2;
                if (countDictionary.ContainsKey(id))
                {
                    countDictionary[id] += 1;
                }
                else
                {
                    if (tags.ContainsKey(id))
                        countDictionary[id] = 1;
                }
            }
        }
    }
}
