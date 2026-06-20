namespace SnapSiphon.Models;

public sealed class MatchedMemory
{
    public DiscoveredFile File { get; init; } = null!;
    public MemoryEntry? JsonEntry { get; init; }
}
