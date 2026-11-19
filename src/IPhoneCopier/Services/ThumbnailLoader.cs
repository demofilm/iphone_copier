using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using IPhoneCopier.Models;

namespace IPhoneCopier.Services;

public static class ThumbnailLoader
{
    private static readonly SemaphoreSlim Gate = new(4);

    public static async Task LoadAsync(MediaItem item, CancellationToken ct)
    {
        if (!MediaFiles.CanDecode(item.Path)) return;
        try
        {
            await Gate.WaitAsync(ct);
            try
            {
                var bmp = await Task.Run(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    using var fs = File.OpenRead(item.Path);
                    return Bitmap.DecodeToWidth(fs, 160, BitmapInterpolationMode.MediumQuality);
                }, ct);
                if (!ct.IsCancellationRequested) item.Thumbnail = bmp;
            }
            finally { Gate.Release(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* corrupt image: keep placeholder */ }
    }
}
