using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IPhoneCopier.Models;

public partial class MediaItem : ObservableObject
{
    public MediaItem(string path)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        Extension = System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        try { Size = new FileInfo(path).Length; } catch { Size = 0; }
    }

    public string Path { get; }
    public string Name { get; }
    public string Extension { get; }
    public long Size { get; }
    public string SizeText => Size >= 1_048_576 ? $"{Size / 1_048_576.0:F1} MB" : $"{Size / 1024.0:F0} KB";

    [ObservableProperty]
    private Bitmap? _thumbnail;
}
