using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPhoneCopier.Models;
using IPhoneCopier.Services;

namespace IPhoneCopier.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int MaxDestItems = 600;
    private CancellationTokenSource? _srcThumbs, _dstThumbs, _transferCts;

    public Func<Task<string?>>? PickFolder { get; set; }

    public ObservableCollection<FolderNode> Roots { get; } = new();
    public ObservableCollection<MediaItem> SourceItems { get; } = new();
    public ObservableCollection<MediaItem> DestItems { get; } = new();

    [ObservableProperty] private string _sourcePath = "";
    [ObservableProperty] private string _destPath = "";
    [ObservableProperty] private FolderNode? _selectedNode;
    [ObservableProperty] private bool _isMove = true;
    [ObservableProperty] private bool _deleteEmptyFolders = true;
    [ObservableProperty] private bool _skipIdentical = true;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _status = "Kies een bronmap (iPhone-export) en een doelmap.";
    [ObservableProperty] private string _destInfo = "";
    [ObservableProperty] private string _sourceInfo = "";

    public string ActionVerb => IsMove ? "Verplaats" : "Kopieer";
    partial void OnIsMoveChanged(bool value) => OnPropertyChanged(nameof(ActionVerb));

    partial void OnSelectedNodeChanged(FolderNode? value) => _ = ShowSourceFolderAsync(value);

    [RelayCommand]
    private async Task BrowseSourceAsync()
    {
        var p = PickFolder == null ? null : await PickFolder();
        if (p != null) { SourcePath = p; await ScanSourceAsync(); }
    }

    [RelayCommand]
    private async Task BrowseDestAsync()
    {
        var p = PickFolder == null ? null : await PickFolder();
        if (p != null) { DestPath = p; await RefreshDestAsync(); }
    }

    [RelayCommand]
    public async Task ScanSourceAsync()
    {
        if (!Directory.Exists(SourcePath)) { Status = "Bronmap bestaat niet."; return; }
        Status = "Bezig met scannen…";
        var root = await Task.Run(() => FolderScanner.Scan(SourcePath));
        Roots.Clear();
        Roots.Add(root);
        SelectedNode = root;
        var folders = root.Flatten().Count(f => f.FileCount > 0);
        SourceInfo = $"{folders} mappen met bestanden, {root.TotalCount} bestanden";
        Status = "Scan klaar.";
    }

    [RelayCommand]
    public async Task RefreshDestAsync()
    {
        _dstThumbs?.Cancel();
        DestItems.Clear();
        if (!Directory.Exists(DestPath)) { DestInfo = ""; return; }
        var files = await Task.Run(() => FolderScanner.MediaIn(DestPath));
        DestInfo = $"{files.Count} bestanden";
        var cts = _dstThumbs = new CancellationTokenSource();
        foreach (var f in files.Take(MaxDestItems)) DestItems.Add(new MediaItem(f));
        if (files.Count > MaxDestItems) DestInfo += $" (eerste {MaxDestItems} getoond)";
        await LoadThumbsAsync(DestItems.ToList(), cts.Token);
    }

    private async Task ShowSourceFolderAsync(FolderNode? node)
    {
        _srcThumbs?.Cancel();
        SourceItems.Clear();
        if (node == null) return;
        var files = await Task.Run(() => FolderScanner.MediaIn(node.Path));
        var cts = _srcThumbs = new CancellationTokenSource();
        if (cts.IsCancellationRequested) return;
        foreach (var f in files) SourceItems.Add(new MediaItem(f));
        await LoadThumbsAsync(SourceItems.ToList(), cts.Token);
    }

    private static async Task LoadThumbsAsync(List<MediaItem> items, CancellationToken ct)
    {
        await Task.WhenAll(items.Select(i => ThumbnailLoader.LoadAsync(i, ct)));
    }

    [RelayCommand] private void CheckAll() { foreach (var r in Roots) r.IsChecked = true; }
    [RelayCommand] private void UncheckAll() { foreach (var r in Roots) r.IsChecked = false; }

    [RelayCommand]
    private void Cancel() => _transferCts?.Cancel();

    [RelayCommand]
    private async Task TransferCheckedAsync()
    {
        var files = Roots.SelectMany(r => r.Flatten()).Where(n => n.IsChecked)
            .SelectMany(n => FolderScanner.MediaIn(n.Path)).ToList();
        await TransferAsync(files, SourcePath);
    }

    public Task TransferItemsAsync(IEnumerable<MediaItem> items) =>
        TransferAsync(items.Select(i => i.Path).ToList(), SourcePath);

    private async Task TransferAsync(List<string> files, string sourceRoot)
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(DestPath)) { Status = "Kies eerst een doelmap."; return; }
        if (files.Count == 0) { Status = "Niets geselecteerd."; return; }
        var destFull = Path.GetFullPath(DestPath);
        if (Directory.Exists(SourcePath) &&
            string.Equals(destFull.TrimEnd('\\'), Path.GetFullPath(SourcePath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        { Status = "Doelmap mag niet gelijk zijn aan de bronmap."; return; }

        IsBusy = true;
        ProgressValue = 0;
        _transferCts = new CancellationTokenSource();
        var progress = new Progress<TransferProgress>(p =>
        {
            ProgressValue = p.Total == 0 ? 0 : 100.0 * p.Done / p.Total;
            Status = $"{p.Done}/{p.Total}  {p.Current}";
        });
        try
        {
            var r = await TransferService.RunAsync(files, DestPath, IsMove, SkipIdentical,
                sourceRoot, DeleteEmptyFolders, progress, _transferCts.Token);
            var verb = IsMove ? "verplaatst" : "gekopieerd";
            Status = $"{r.Transferred} {verb}, {r.Renamed} hernoemd, {r.SkippedDuplicates} dubbel overgeslagen, " +
                     $"{r.RemovedFolders} lege mappen verwijderd, {r.Errors.Count} fouten" + (r.Cancelled ? " (afgebroken)" : "");
            if (r.Errors.Count > 0) Status += " — " + r.Errors[0];
            ProgressValue = 100;
        }
        finally { IsBusy = false; }
        await ScanSourceAsync();
        await RefreshDestAsync();
    }
}
