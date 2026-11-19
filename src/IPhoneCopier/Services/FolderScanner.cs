using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IPhoneCopier.Models;

namespace IPhoneCopier.Services;

public static class FolderScanner
{
    public static FolderNode Scan(string root)
    {
        var node = new FolderNode(Path.GetFileName(root.TrimEnd('\\', '/')) is { Length: > 0 } n ? n : root, root, CountMedia(root));
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(root).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                node.Children.Add(Scan(dir));
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException) { }
        return node;
    }

    public static IReadOnlyList<string> MediaIn(string dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir).Where(MediaFiles.IsMedia)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            return Array.Empty<string>();
        }
    }

    private static int CountMedia(string dir) => MediaIn(dir).Count;
}
