namespace SnapSiphon.Models;

public sealed class MemoryEntry
{
    public DateTime DateUtc { get; init; }
    public string MediaType { get; init; } = "";
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
