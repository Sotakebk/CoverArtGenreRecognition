using DataAcquisition.Shared;
using DataAcquisition.Shared.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using static DataAcquisition.Shared.ConsoleHelper;

namespace DataAcquisition.Downloader
{
    internal class Program
    {
        private struct ImageInfo
        {
            public Guid Guid;
            public long ImageId;
            public ImageFileType Type;

            public ImageInfo(Guid guid, long imageId, ImageFileType type)
            {
                Guid = guid;
                ImageId = imageId;
                Type = type;
            }
        }

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
            var missingCount = CountMissingFiles(rows, directoryName);
            Console.WriteLine($"Missing files count: {missingCount}");
            Console.WriteLine($"Downloading files...");
            DownloadFiles(rows, directoryName);

            QuitImmediately("Work done!");
        }

        private static void DownloadFiles(ImageInfo[] imageInfos, string directoryName)
        {
            using var webClient = new WebClient();
            foreach (var i in imageInfos)
            {
                var path = Path.Join(directoryName, GetFilenameFromImageInfo(i));

                if (!File.Exists(path))
                {
                    if (!TryDownloadFile(webClient, i, path))
                    {
                        Console.WriteLine($"Failed to download image {i.ImageId}");
                    }
                }
            }
        }

        private static bool TryDownloadFile(WebClient wc, ImageInfo imageInfo, string targetPath)
        {
            var attempts = 0;
            for (int i = 0; i < 4; i++) // 404 loop
            {
                for (int x = 0; x < 5; x++) // failed request loop
                {
                    attempts++;
                    Console.WriteLine($"Downloading file: {imageInfo.ImageId} attempt: {attempts}");
                    var uri = GetUri(imageInfo, i);
                    try
                    {
                        wc.DownloadFile(uri, targetPath);
                        return true;
                    }
                    catch (WebException ex) when (ex.Response is HttpWebResponse wr && wr.StatusCode == HttpStatusCode.NotFound)
                    {
                        Console.WriteLine(ex.Message);
                        File.Delete(targetPath);
                        break; // increment 404
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        File.Delete(targetPath);
                    }
                }
            }
            return false;
        }

        private static string GetUri(ImageInfo i, int scale)
        {
            var format = (i.Type == ImageFileType.Png) ? "png" : "jpg";

            return $"https://" +
                $"archive.org/download/mbid-{i.Guid.ToString().ToLower()}/mbid-{i.Guid.ToString().ToLower()}-{i.ImageId}{GetScaleString(scale)}.{format}";
        }

        private static string GetScaleString(int scale)
        {
            switch (scale)
            {
                case 0:
                    return "_thumb250";

                case 1:
                    return "_thumb500";

                case 2:
                    return "_thumb1200";

                default:
                    return "";
            }
        }

        private static int CountMissingFiles(ImageInfo[] imageInfos, string directoryName)
        {
            var counter = 0;
            foreach (var i in imageInfos)
            {
                var path = Path.Join(directoryName, GetFilenameFromImageInfo(i));

                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Length == 0)
                    {
                        File.Delete(path);
                        counter++;
                    }
                }
                else
                {
                    counter++;
                }
            }
            return counter;
        }

        private static string GetFilenameFromImageInfo(ImageInfo i)
        {
            var format = (i.Type == ImageFileType.Png) ? "png" : "jpg";
            return $"{i.ImageId}.{format}";
        }

        private static ImageInfo[] LoadFile(string filename)
        {
            var count = FileHelper.CountNonEmptyLinesInTextFile(filename);
            var list = new List<ImageInfo>((int)count);

            FileHelper.DoForEachRowInTextFile(filename, 23, separator: ",", action: (arr) =>
            {
                if (arr[0] == "GroupID") // header line
                    return;

                var guid = Guid.Parse(arr[1]);
                var imageId = long.Parse(arr[2]);
                var imageType = (ImageFileType)byte.Parse(arr[3]);

                list.Add(new ImageInfo(guid, imageId, imageType));
            });

            return list.ToArray();
        }

        private static string HandleFileName(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                if (File.Exists(args[0]))
                    return Path.GetFullPath(args[0]);
                else
                    QuitImmediately($"{args[0]} is not a valid file!");
            }
            else
            {
                while (true)
                {
                    Console.WriteLine("Please input file path or name:");
                    var line = Console.ReadLine();
                    if (File.Exists(line))
                        return Path.GetFullPath(line);
                }
            }
            return null;
        }
    }
}
