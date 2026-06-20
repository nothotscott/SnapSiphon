using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapSiphon.Models;
using SnapSiphon.Services;

namespace SnapSiphon.ViewModels;

public enum AppState { Idle, Processing, Done }

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartProcessingCommand))]
    private string _inputFolderPath = "";

    [ObservableProperty]
    private string _outputFolderName = "output";

    [ObservableProperty]
    private string _filePrefix = "Snapchat-";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsProcessing), nameof(IsDone))]
    private AppState _state = AppState.Idle;

    [ObservableProperty]
    private string _statusMessage = "Select your Snapchat Data folder to get started.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressFraction))]
    private int _totalFiles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressFraction))]
    private int _processedFiles;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private string _currentFile = "";

    public bool IsIdle => State == AppState.Idle;
    public bool IsProcessing => State == AppState.Processing;
    public bool IsDone => State == AppState.Done;
    public double ProgressFraction => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles : 0;

    public Func<Task<string?>>? FolderPickerFunc { get; set; }

    private CancellationTokenSource? _cts;
    private readonly MediaProcessingService _processor = new();

    [RelayCommand]
    private async Task BrowseInputFolder()
    {
        var path = FolderPickerFunc is not null
            ? await FolderPickerFunc()
            : null;

        if (!string.IsNullOrEmpty(path))
            InputFolderPath = path;
    }

    private bool CanStartProcessing() =>
        !string.IsNullOrWhiteSpace(InputFolderPath) && State == AppState.Idle;

    [RelayCommand(CanExecute = nameof(CanStartProcessing))]
    private async Task StartProcessing()
    {
        State = AppState.Processing;
        ProcessedFiles = 0;
        TotalFiles = 0;
        ErrorCount = 0;
        CurrentFile = "";

        _cts = new CancellationTokenSource();

        var progressHandler = new Progress<ProcessingProgress>(p =>
        {
            if (!string.IsNullOrEmpty(p.StatusMessage))
                StatusMessage = p.StatusMessage;

            if (p.Total > 0)
            {
                TotalFiles = p.Total;
                ProcessedFiles = p.Completed;
                ErrorCount = p.Errors;
                CurrentFile = p.CurrentFile;
            }
        });

        try
        {
            await _processor.ProcessAsync(
                new ProcessingOptions
                {
                    InputRootPath = InputFolderPath,
                    OutputFolderName = OutputFolderName,
                    FilePrefix = FilePrefix
                },
                progressHandler,
                _cts.Token);

            StatusMessage = ErrorCount > 0
                ? $"Done — {ProcessedFiles:N0} files saved, {ErrorCount} errors."
                : $"Done — {ProcessedFiles:N0} files saved successfully.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            State = AppState.Done;
            _cts?.Dispose();
            _cts = null;
            StartProcessingCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void CancelProcessing() => _cts?.Cancel();

    [RelayCommand]
    private void OpenOutputFolder()
    {
        var path = Path.Combine(InputFolderPath, OutputFolderName);
        if (Directory.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [RelayCommand]
    private void StartOver()
    {
        State = AppState.Idle;
        StatusMessage = "Select your Snapchat Data folder to get started.";
        ProcessedFiles = 0;
        TotalFiles = 0;
        ErrorCount = 0;
        CurrentFile = "";
        StartProcessingCommand.NotifyCanExecuteChanged();
    }
}
