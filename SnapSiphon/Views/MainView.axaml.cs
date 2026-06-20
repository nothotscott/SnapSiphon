using Avalonia.Controls;
using Avalonia.Platform.Storage;
using SnapSiphon.ViewModels;

namespace SnapSiphon.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainViewModel vm)
            vm.FolderPickerFunc = PickFolderAsync;
    }

    private async Task<string?> PickFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var results = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Snapchat Data Folder",
            AllowMultiple = false
        });

        return results.Count > 0 ? results[0].Path.LocalPath : null;
    }
}
