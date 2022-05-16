using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataAcquisition.Shared;
using DataAcquisition.Shared.Readers;

namespace DataAcquisition.ListUnknownTags
{
    internal class Program
    {
        private struct Entry
        {
            public int counter;
            public string text;
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

            Console.WriteLine("Loading tags...");
            var tags = TagReader.Read();
            Console.WriteLine("Filtering tags...");
            tags = FilterTags(tags);
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
            var entries = refCounter.Select(i => new Entry() { counter = i.Value, text = tags[i.Key] }).OrderByDescending(i => i.counter);
            Console.WriteLine("Saving...");
            SaveTags(entries);

            QuitImmediately("Work done!");
        }

        private static void SaveTags(IOrderedEnumerable<Entry> entries)
        {
            var filePath = FilePaths.FilenameToSavePath("unknown_tags_listing.txt");

            using var fileStream = File.CreateText(filePath);
            foreach (var entry in entries)
            {
                fileStream.WriteLine($"{entry.counter}\t{entry.text}");
            }
            fileStream.Flush();
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

        private static Dictionary<int, string> FilterTags(Dictionary<int, string> tags)
        {
            var dict = new Dictionary<int, string>(tags.Count);
            foreach (var pair in tags)
            {
                if (!GenreHelper.IsStringInAnyGroup(pair.Value))
                    dict.Add(pair.Key, pair.Value);
            }
            return dict;
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
