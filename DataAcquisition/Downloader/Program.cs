﻿using DataAcquisition.Shared;
using DataAcquisition.Shared.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static DataAcquisition.Shared.ConsoleHelper;

namespace DataAcquisition.Downloader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var filename = HandleFileName(args);
            Console.WriteLine("Reading file...");
            var rows = LoadFile(filename);

            var directoryName = Path.Join(Path.GetDirectoryName(filename), "downloadedCovers");
            if (!Directory.Exists(directoryName))
            {
                Console.WriteLine("Creating missing downloads directory...");
                Directory.CreateDirectory(directoryName);
            }

            Console.WriteLine("Counting missing images...");
            rows = ListMissingFiles(rows, directoryName);
            Console.WriteLine($"Missing files count: {rows.Length}");

            Console.WriteLine("Downloading files...");
            DownloadFiles(rows, directoryName);

            QuitImmediately("Work done!");
        }

        private static void DownloadFiles(Row[] rows, string directoryName)
        {
            const int threadCount = 3;
            var bag = new ConcurrentBag<WebClient>();
            var estimator = new TimeEstimator(30);
            var _lock = new object();
            var count = 0;

            for (int i = 0; i < threadCount; i++)
                bag.Add(new WebClient());

            Parallel.For(0, rows.Length,
                new ParallelOptions { MaxDegreeOfParallelism = threadCount },
                (i) =>
                {
                    WebClient client = null;
                    int t = 0;
                    try
                    {
                        while (!bag.TryTake(out client))
                        {
                            t++;
                            if (t > 4)
                                throw new Exception();
                        }
                        var start = DateTime.Now;
                        var path = Path.Join(directoryName, GetFilenameFromRow(rows[i]));
                        var task = TryDownloadFile(client, rows[i], path);
                        task.Wait();

                        if (task.Result == false)
                        {
                            Console.WriteLine($"Failed to download image {rows[i].ImageId}");
                            return;
                        }

                        var end = DateTime.Now;
                        estimator.AddTimeSpan(end - start);
                        var ETA = estimator.Estimate(rows.Length - (count / threadCount));
                        Console.Title = $"{count}/{rows.Length}, ETA: {ETA:dd\\.hh\\:mm\\:ss}";
                    }
                    finally
                    {
                        if (client != null)
                            bag.Add(client);

                        lock (_lock)
                            count++;
                    }
                });
        }

        private static async Task<bool> TryDownloadFile(WebClient wc, Row row, string targetPath)
        {
            if (File.Exists(targetPath) && new FileInfo(targetPath).Length != 0)
                return true;

            var attempts = 0;
            for (var i = 0; i < 4; i++) // 404 loop
                for (var x = 0; x < 5; x++) // failed request loop
                {
                    attempts++;
                    Console.WriteLine($"Downloading file: {row.ImageId} attempt: {attempts}");
                    var uri = GetUri(row, i);
                    try
                    {
                        var arr = wc.DownloadData(uri);
                        await File.WriteAllBytesAsync(targetPath, arr);
                        return true;
                    }
                    catch (WebException ex) when
                    (ex.Response is HttpWebResponse { StatusCode: HttpStatusCode.NotFound }
                                 or HttpWebResponse { StatusCode: HttpStatusCode.Forbidden })
                    {
                        Console.WriteLine(ex.Message);
                        if (File.Exists(targetPath))
                            File.Delete(targetPath);
                        break; // increment 404 or 403, try a different image size
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        if (File.Exists(targetPath))
                            File.Delete(targetPath);
                    }
                }

            return false;
        }

        private static string GetUri(Row i, int scale)
        {
            var format = i.Type == ImageFileType.Png ? "png" : "jpg";

            return "https://archive.org/" +
                   $"download/mbid-{i.Guid.ToString().ToLower()}/mbid-{i.Guid.ToString().ToLower()}-{i.ImageId}{GetScaleString(scale)}.{format}";
        }

        private static string GetScaleString(int scale)
        {
            return scale switch
            {
                0 => "_thumb250",
                1 => "_thumb500",
                2 => "_thumb1200",
                _ => ""
            };
        }

        private static Row[] ListMissingFiles(Row[] rows, string directory)
        {
            var list = new List<Row>(rows.Count());
            foreach (var i in rows)
            {
                var path = Path.Join(directory, GetFilenameFromRow(i));
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists && fileInfo.Length == 0)
                {
                    File.Delete(path);
                }
                if (!fileInfo.Exists)
                {
                    list.Add(i);
                }
            }
            return list.ToArray();
        }

        private static string GetFilenameFromRow(Row i)
        {
            var format = i.Type == ImageFileType.Png ? "png" : "jpg";
            return $"{i.ImageId}.{format}";
        }

        private static Row[] LoadFile(string filename)
        {
            var count = FileHelper.CountNotEmptyLinesInTextFile(filename);
            var list = new List<Row>((int)count);

            FileHelper.DoForEachRowInTextFile(filename, 23, separator: ",", action: arr =>
            {
                if (arr[0] == "GroupID") // header line
                    return;

                var guid = Guid.Parse(arr[1]);
                var imageId = long.Parse(arr[2]);
                var imageType = (ImageFileType)byte.Parse(arr[3]);

                list.Add(new Row(guid, imageId, imageType));
            });

            return list.ToArray();
        }

        private static string HandleFileName(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                if (File.Exists(args[0]))
                    return Path.GetFullPath(args[0]);

                QuitImmediately($"{args[0]} is not a valid file!");
            }
            else
            {
                while (true)
                {
                    Console.WriteLine("Please input file path or name:");
                    var line = Console.ReadLine();
                    if (File.Exists(line))
                        return Path.GetFullPath(line ?? "");
                }
            }

            return null;
        }

        private struct Row
        {
            public readonly Guid Guid;
            public readonly long ImageId;
            public readonly ImageFileType Type;

            public Row(Guid guid, long imageId, ImageFileType type)
            {
                Guid = guid;
                ImageId = imageId;
                Type = type;
            }
        }
    }
}
