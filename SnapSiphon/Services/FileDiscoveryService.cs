using System.Text.RegularExpressions;
using SnapSiphon.Models;

namespace SnapSiphon.Services;

public static class FileDiscoveryService
{
    // 2017-03-08_927BFC8B-5158-41E6-8CCE-9979DCD29677-main.jpg
    private static readonly Regex MemoryPattern = new(
        @"^(\d{4}-\d{2}-\d{2})_([0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})-(main|overlay)\.(jpg|jpeg|png|mp4)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> MediaExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".mp4" };

    public static List<DiscoveredFile> DiscoverMemories(IEnumerable<string> myDataDirs)
    {
        var files = new List<DiscoveredFile>();
        foreach (var dir in myDataDirs)
        {
            var memoriesDir = Path.Combine(dir, "memories");
            if (!Directory.Exists(memoriesDir)) continue;

            foreach (var path in Directory.EnumerateFiles(memoriesDir))
            {
                var name = Path.GetFileName(path);
                var m = MemoryPattern.Match(name);
                if (!m.Success) continue;
                if (m.Groups[3].Value.Equals("overlay", StringComparison.OrdinalIgnoreCase)) continue;

                var info = new FileInfo(path);
                files.Add(new DiscoveredFile
                {
                    SourcePath = path,
                    UniqueId = m.Groups[2].Value.ToUpperInvariant(),
                    FileDate = DateOnly.ParseExact(m.Groups[1].Value, "yyyy-MM-dd"),
                    CreationTimeUtc = info.CreationTimeUtc,
                    Extension = ("." + m.Groups[4].Value).ToLowerInvariant(),
                    Category = MediaCategory.Memory
                });
            }
        }

        return [.. files.OrderBy(f => f.CreationTimeUtc)];
    }

    public static List<DiscoveredFile> DiscoverChatMedia(IEnumerable<string> myDataDirs) =>
        DiscoverSubfolder(myDataDirs, "chat_media", MediaCategory.ChatMedia);

    public static List<DiscoveredFile> DiscoverSharedStory(IEnumerable<string> myDataDirs) =>
        DiscoverSubfolder(myDataDirs, "shared_story", MediaCategory.SharedStory);

    private static List<DiscoveredFile> DiscoverSubfolder(
        IEnumerable<string> myDataDirs, string subFolder, MediaCategory category)
    {
        var files = new List<DiscoveredFile>();
        foreach (var dir in myDataDirs)
        {
            var target = Path.Combine(dir, subFolder);
            if (!Directory.Exists(target)) continue;

            foreach (var path in Directory.EnumerateFiles(target))
            {
                var ext = Path.GetExtension(path);
                if (!MediaExtensions.Contains(ext)) continue;

                var info = new FileInfo(path);
                files.Add(new DiscoveredFile
                {
                    SourcePath = path,
                    UniqueId = Path.GetFileName(path),
                    FileDate = DateOnly.FromDateTime(info.CreationTimeUtc),
                    CreationTimeUtc = info.CreationTimeUtc,
                    Extension = ext.ToLowerInvariant(),
                    Category = category
                });
            }
        }
        return files;
    }
}
