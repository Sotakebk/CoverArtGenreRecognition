using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataAcquisition.Shared;
using DataAcquisition.Shared.Readers;
using static DataAcquisition.Shared.ConsoleHelper;

namespace DataAcquisition.ListUnknownTags
{
    internal class Program
    {
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

            Console.WriteLine("Loading tags...");
            var tags = TagReader.Read();
            Console.WriteLine($"Tag count: {tags.Count}");
            Console.WriteLine("Loading release to tag references...");
            var refsA = TagRelationReader.ReadReleaseToTagRelations();
            Console.WriteLine("Loading release group to tag references...");
            var refsB = TagRelationReader.ReadReleaseGroupToTagRelations();
            Console.WriteLine("Calculating reference counts...");
            var refCounter = new Dictionary<int, int>(tags.Count);
            IncrementCounters(refCounter, refsA, tags);
            IncrementCounters(refCounter, refsB, tags);
            Console.WriteLine("Sorting...");
            var entries = refCounter.Select(i => (i.Value, tags[i.Key])).OrderByDescending(i => i.Value);
            Console.WriteLine("Saving...");
            SaveTags(entries);

            QuitImmediately("Work done!");
        }

        private static void SaveTags(IEnumerable<(int, string)> entries)
        {
            var filePath = FilePaths.FilenameToSavePath("known_tags_list.txt");
            var filePath2 = FilePaths.FilenameToSavePath("unknown_tags_list.txt");

            using var fileStream = File.CreateText(filePath);
            using var fileStream2 = File.CreateText(filePath2);
            foreach (var entry in entries)
            {
                bool isKnown = GenreHelper.IsStringInAnyGroup(entry.Item2);

                if (isKnown)
                {
                    fileStream.WriteLine($"{entry.Item1}\t{entry.Item2}");
                }
                else
                {
                    fileStream2.WriteLine($"{entry.Item1}\t{entry.Item2}");
                }
            }
            fileStream.Flush();
            fileStream2.Flush();
        }

        private static void IncrementCounters(Dictionary<int, int> toIncrement, (int, int)[] keys, Dictionary<int, string> tags)
        {
            foreach (var pair in keys)
            {
                var id = pair.Item2;
                if (toIncrement.TryGetValue(id, out var counter))
                {
                    toIncrement[id] = counter + 1;
                }
                else
                {
                    if (tags.ContainsKey(id))
                        toIncrement[id] = 1;
                }
            }
        }
    }
}
