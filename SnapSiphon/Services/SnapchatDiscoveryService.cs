using System.IO.Compression;
using System.Text.RegularExpressions;

namespace SnapSiphon.Services;

public static class SnapchatDiscoveryService
{
    private static readonly Regex MyDataPattern =
        new(@"^mydata~\d+(-\d+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<string> FindMyDataDirectories(string rootPath) =>
        Directory.EnumerateDirectories(rootPath)
            .Where(d => MyDataPattern.IsMatch(Path.GetFileName(d)))
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string? FindMemoriesJson(IEnumerable<string> myDataDirs)
    {
        foreach (var dir in myDataDirs)
        {
            var path = Path.Combine(dir, "json", "memories_history.json");
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public static async Task ExtractZipsAsync(
        string rootPath,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var zips = Directory.EnumerateFiles(rootPath, "mydata~*.zip", SearchOption.TopDirectoryOnly);
        foreach (var zip in zips)
        {
            ct.ThrowIfCancellationRequested();
            var extractDir = Path.Combine(rootPath, Path.GetFileNameWithoutExtension(zip));
            if (Directory.Exists(extractDir)) continue;

            progress?.Report($"Extracting {Path.GetFileName(zip)}…");
            await Task.Run(() => ZipFile.ExtractToDirectory(zip, extractDir, overwriteFiles: false), ct);
        }
    }
}
