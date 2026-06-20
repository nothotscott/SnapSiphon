namespace SnapSiphon.Models;

public sealed class ProcessingProgress
{
    public int Total { get; init; }
    public int Completed { get; init; }
    public int Errors { get; init; }
    public string CurrentFile { get; init; } = "";
    public string StatusMessage { get; init; } = "";

    public double Fraction => Total > 0 ? (double)Completed / Total : 0;
}
