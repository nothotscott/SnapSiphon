using SnapSiphon.Models;

namespace SnapSiphon.Services;

public sealed class MediaProcessingService
{
    public async Task ProcessAsync(
        ProcessingOptions options,
        IProgress<ProcessingProgress> progress,
        CancellationToken ct = default)
    {
        Report(progress, "Checking for ZIP archives…");
        await SnapchatDiscoveryService.ExtractZipsAsync(
            options.InputRootPath,
            new Progress<string>(msg => Report(progress, msg)),
            ct);

        Report(progress, "Scanning data directories…");
        var myDataDirs = SnapchatDiscoveryService.FindMyDataDirectories(options.InputRootPath);
        if (myDataDirs.Count == 0)
            throw new InvalidOperationException(
                "No Snapchat data directories (mydata~*) found in the selected folder.");

        var jsonPath = SnapchatDiscoveryService.FindMemoriesJson(myDataDirs);
        List<MemoryEntry> jsonEntries = [];
        if (jsonPath is not null)
        {
            Report(progress, "Reading memories metadata…");
            jsonEntries = await MemoriesJsonParser.ParseAsync(jsonPath, ct);
        }

        Report(progress, "Discovering media files…");
        var memories = FileDiscoveryService.DiscoverMemories(myDataDirs);
        var chatMedia = FileDiscoveryService.DiscoverChatMedia(myDataDirs);
        var sharedStory = FileDiscoveryService.DiscoverSharedStory(myDataDirs);

        var (matched, _, _) = MemoryMatchingService.Match(jsonEntries, memories);

        // Build flat work list: (file, metadata entry or null, output subfolder)
        var work = new List<(DiscoveredFile File, MemoryEntry? Entry, string SubDir)>(
            matched.Count + chatMedia.Count + sharedStory.Count);

        foreach (var m in matched)      work.Add((m.File, m.JsonEntry, "memories"));
        foreach (var f in chatMedia)    work.Add((f, null, "chat_media"));
        foreach (var f in sharedStory)  work.Add((f, null, "shared_story"));

        // Deduplicate by content hash, keeping the oldest copy
        Report(progress, "Checking for duplicates…");
        var dedupInput = work
            .Select(w => (w.File, Timestamp: w.Entry?.DateUtc ?? w.File.CreationTimeUtc))
            .ToList();
        var duplicates = await DeduplicationService.FindDuplicatesAsync(dedupInput, progress, ct);

        if (duplicates.Count > 0)
        {
            work.RemoveAll(w => duplicates.Contains(w.File.SourcePath));
            Report(progress, $"Skipping {duplicates.Count:N0} duplicate files…");
        }

        var outputRoot = Path.Combine(options.InputRootPath, options.OutputFolderName);
        int total = work.Count, completed = 0, errors = 0;

        foreach (var (file, entry, subDir) in work)
        {
            ct.ThrowIfCancellationRequested();

            var outputDir = Path.Combine(outputRoot, subDir);
            Directory.CreateDirectory(outputDir);

            var outputName = BuildOutputName(file, options.FilePrefix);
            var outputPath = ResolveUniqueOutputPath(outputDir, outputName);

            try
            {
                System.IO.File.Copy(file.SourcePath, outputPath, overwrite: false);

                if (entry?.Latitude is not null && entry.Longitude is not null)
                    MetadataWriterService.TryWriteGps(outputPath, entry.Latitude.Value, entry.Longitude.Value);

                var timestamp = entry?.DateUtc ?? file.CreationTimeUtc;
                MetadataWriterService.ApplyTimestamp(outputPath, timestamp);
            }
            catch
            {
                errors++;
                if (System.IO.File.Exists(outputPath))
                    try { System.IO.File.Delete(outputPath); } catch { /* best effort */ }
            }

            completed++;
            progress.Report(new ProcessingProgress
            {
                Total = total,
                Completed = completed,
                Errors = errors,
                CurrentFile = outputName
            });

            if (completed % 20 == 0)
                await Task.Yield();
        }
    }

    private static string BuildOutputName(DiscoveredFile file, string prefix)
    {
        if (file.Category == MediaCategory.Memory)
            return $"{prefix}{file.FileDate:yyyy-MM-dd}_{file.UniqueId}{file.Extension}";

        // Chat media and shared story: prefix + original filename
        return $"{prefix}{Path.GetFileName(file.SourcePath)}";
    }

    private static string ResolveUniqueOutputPath(string dir, string name)
    {
        var path = Path.Combine(dir, name);
        if (!System.IO.File.Exists(path)) return path;

        // Conflict: append counter before extension
        var stem = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (int i = 2; ; i++)
        {
            path = Path.Combine(dir, $"{stem}_{i}{ext}");
            if (!System.IO.File.Exists(path)) return path;
        }
    }

    private static void Report(IProgress<ProcessingProgress> progress, string message) =>
        progress.Report(new ProcessingProgress { StatusMessage = message });
}
