namespace SnapSiphon.Models;

public enum MediaCategory { Memory, ChatMedia, SharedStory }

public sealed class DiscoveredFile
{
    public string SourcePath { get; init; } = "";
    public string UniqueId { get; init; } = "";
    public DateOnly FileDate { get; init; }
    public DateTime CreationTimeUtc { get; init; }
    public string Extension { get; init; } = "";
    public MediaCategory Category { get; init; }
}
