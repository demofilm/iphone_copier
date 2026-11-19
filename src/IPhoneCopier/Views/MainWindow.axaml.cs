using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using IPhoneCopier.Models;
using IPhoneCopier.Services;
using IPhoneCopier.ViewModels;

namespace IPhoneCopier.Views;

public partial class MainWindow : Window
{
    private Point _pressPoint;
    private PointerPressedEventArgs? _pressArgs;
    private List<string>? _pending;   // files that would be dragged
    private List<string>? _dragging;  // files of the drag in progress

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PickFolder = PickFolderAsync;
                vm.Confirm = msg => new ConfirmDialog(msg).ShowDialog<bool>(this);
            }
        };

        WireDragSource(SourceList, ResolveListItems);
        WireDragSource(SourceTree, ResolveTreeFolder);

        DestPanel.AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            e.DragEffects = _dragging != null ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        });
        DestPanel.AddHandler(DragDrop.DropEvent, OnDropAsync);
    }

    private async Task<string?> PickFolderAsync()
    {
        var res = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        return res.Count > 0 ? res[0].TryGetLocalPath() : null;
    }

    private async void TransferSelected_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.TransferItemsAsync(SourceList.SelectedItems!.OfType<MediaItem>().ToList());
    }

    private async void Beer_Click(object? sender, RoutedEventArgs e)
    {
        try { await Launcher.LaunchUriAsync(new Uri("https://www.paypal.com/paypalme/lordleuter")); } catch { }
    }

    // ---- drag & drop ----

    private void WireDragSource(Control c, Func<Control, PointerPressedEventArgs, List<string>?> resolve)
    {
        c.AddHandler(PointerPressedEvent, (_, e) =>
        {
            _pending = null;
            if (!e.GetCurrentPoint(c).Properties.IsLeftButtonPressed) return;
            _pressPoint = e.GetPosition(c);
            _pressArgs = e;
            _pending = resolve(c, e);
        }, RoutingStrategies.Tunnel);

        c.AddHandler(PointerMovedEvent, async (_, e) =>
        {
            if (_pending == null || _dragging != null) return;
            if (!e.GetCurrentPoint(c).Properties.IsLeftButtonPressed) { _pending = null; return; }
            var d = e.GetPosition(c) - _pressPoint;
            if (Math.Abs(d.X) + Math.Abs(d.Y) < 10) return;

            _dragging = _pending;
            _pending = null;
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText($"{_dragging.Count} bestanden"));
            try { await DragDrop.DoDragDropAsync(_pressArgs!, data, DragDropEffects.Copy); }
            finally { _dragging = null; }
        });

        c.AddHandler(PointerReleasedEvent, (_, _) => _pending = null, RoutingStrategies.Tunnel);
    }

    private List<string>? ResolveListItems(Control list, PointerPressedEventArgs e)
    {
        var item = (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true)?.DataContext as MediaItem;
        if (item == null) return null;
        var sel = SourceList.SelectedItems!.OfType<MediaItem>().ToList();
        return (sel.Contains(item) ? sel : new List<MediaItem> { item }).Select(i => i.Path).ToList();
    }

    private List<string>? ResolveTreeFolder(Control tree, PointerPressedEventArgs e)
    {
        var node = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>(true)?.DataContext as FolderNode;
        if (node == null) return null;
        var files = node.Flatten().SelectMany(n => FolderScanner.MediaIn(n.Path)).ToList();
        return files.Count == 0 ? null : files;
    }

    private async void OnDropAsync(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        var files = _dragging;
        if (files == null || DataContext is not MainViewModel vm) return;
        if (string.IsNullOrWhiteSpace(vm.DestPath)) { vm.Status = "Kies eerst een doelmap."; return; }

        await vm.TransferPathsAsync(files);
    }
}



