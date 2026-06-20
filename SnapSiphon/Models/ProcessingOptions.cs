namespace SnapSiphon.Models;

public sealed class ProcessingOptions
{
    public string InputRootPath { get; init; } = "";
    public string OutputFolderName { get; init; } = "output";
    public string FilePrefix { get; init; } = "Snapchat-";
}
