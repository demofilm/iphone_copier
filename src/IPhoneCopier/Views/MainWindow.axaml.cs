using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using IPhoneCopier.Models;
using IPhoneCopier.ViewModels;

namespace IPhoneCopier.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm) vm.PickFolder = PickFolderAsync;
        };
    }

    private async System.Threading.Tasks.Task<string?> PickFolderAsync()
    {
        var res = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        return res.Count > 0 ? res[0].TryGetLocalPath() : null;
    }

    private async void TransferSelected_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.TransferItemsAsync(SourceList.SelectedItems!.OfType<MediaItem>().ToList());
    }
}
