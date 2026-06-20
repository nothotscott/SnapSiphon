using System.Security.Cryptography;
using SnapSiphon.Models;

namespace SnapSiphon.Services;

public static class DeduplicationService
{
    /// <summary>
    /// Identifies duplicate files by content hash. Returns the set of source paths
    /// that should be skipped (all but the oldest copy of each unique file).
    /// Groups by file size first to avoid hashing files that can't possibly match.
    /// </summary>
    public static async Task<HashSet<string>> FindDuplicatesAsync(
        IReadOnlyList<(DiscoveredFile File, DateTime Timestamp)> files,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var sizeGroups = files
            .GroupBy(f => new FileInfo(f.File.SourcePath).Length)
            .Where(g => g.Count() > 1)
            .ToList();

        int candidates = sizeGroups.Sum(g => g.Count());
        if (candidates == 0) return duplicates;

        int processed = 0;
        foreach (var group in sizeGroups)
        {
            var hashMap = new Dictionary<string, (string Path, DateTime Timestamp)>();

            foreach (var (file, timestamp) in group)
            {
                ct.ThrowIfCancellationRequested();

                var hash = await ComputeHashAsync(file.SourcePath, ct);

                if (hashMap.TryGetValue(hash, out var existing))
                {
                    if (timestamp < existing.Timestamp)
                    {
                        duplicates.Add(existing.Path);
                        hashMap[hash] = (file.SourcePath, timestamp);
                    }
                    else
                    {
                        duplicates.Add(file.SourcePath);
                    }
                }
                else
                {
                    hashMap[hash] = (file.SourcePath, timestamp);
                }

                processed++;
                if (processed % 50 == 0)
                    progress?.Report(new ProcessingProgress
                    {
                        StatusMessage = $"Checking for duplicates… {processed:N0}/{candidates:N0}"
                    });
            }
        }

        return duplicates;
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash);
    }
}
