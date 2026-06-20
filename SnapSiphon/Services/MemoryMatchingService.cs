using SnapSiphon.Models;

namespace SnapSiphon.Services;

public static class MemoryMatchingService
{
    /// <summary>
    /// Correlates memory files to JSON entries by grouping on date and pairing by
    /// sorted order within each day. JSON Date (UTC) aligns with the date in the filename.
    /// </summary>
    public static (List<MatchedMemory> Matched, int UnmatchedFiles, int UnmatchedEntries) Match(
        IReadOnlyList<MemoryEntry> jsonEntries,
        IReadOnlyList<DiscoveredFile> files)
    {
        var jsonByDate = jsonEntries
            .GroupBy(e => DateOnly.FromDateTime(e.DateUtc))
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.DateUtc).ToList());

        var filesByDate = files
            .GroupBy(f => f.FileDate)
            .ToDictionary(g => g.Key, g => g.OrderBy(f => f.CreationTimeUtc).ToList());

        var result = new List<MatchedMemory>(files.Count);
        int unmatchedFiles = 0, unmatchedEntries = 0;

        var allDates = jsonByDate.Keys.Union(filesByDate.Keys).OrderBy(d => d);

        foreach (var date in allDates)
        {
            var dayEntries = jsonByDate.TryGetValue(date, out var j) ? j : [];
            var dayFiles = filesByDate.TryGetValue(date, out var f) ? f : [];

            int count = Math.Max(dayEntries.Count, dayFiles.Count);
            for (int i = 0; i < count; i++)
            {
                var file = i < dayFiles.Count ? dayFiles[i] : null;
                var entry = i < dayEntries.Count ? dayEntries[i] : null;

                if (file is null) { unmatchedEntries++; continue; }

                if (entry is null) unmatchedFiles++;
                result.Add(new MatchedMemory { File = file, JsonEntry = entry });
            }
        }

        return (result, unmatchedFiles, unmatchedEntries);
    }
}
