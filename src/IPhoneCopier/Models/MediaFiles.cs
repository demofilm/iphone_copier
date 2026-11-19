using System;
using System.Collections.Generic;
using System.IO;

namespace IPhoneCopier.Models;

public static class MediaFiles
{
    private static readonly HashSet<string> Media = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".heic", ".heif", ".gif", ".tif", ".tiff", ".bmp", ".webp", ".dng",
        ".mov", ".mp4", ".m4v", ".aae", ".3gp", ".avi"
    };

    // Formats Avalonia can decode out of the box (HEIC/video get a placeholder tile).
    private static readonly HashSet<string> Decodable = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
    };

    public static bool IsMedia(string path) => Media.Contains(Path.GetExtension(path));
    public static bool CanDecode(string path) => Decodable.Contains(Path.GetExtension(path));
}
