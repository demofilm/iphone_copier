using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IPhoneCopier.Services;

public sealed record TransferProgress(int Done, int Total, string Current);

public sealed class TransferResult
{
    public int Transferred { get; set; }
    public int Renamed { get; set; }
    public int SkippedDuplicates { get; set; }
    public int RemovedFolders { get; set; }
    public bool Cancelled { get; set; }
    public List<string> Errors { get; } = new();
}

public static class TransferService
{
    public static Task<TransferResult> RunAsync(
        IReadOnlyList<string> files, string destDir, bool move, bool skipIdentical,
        string? sourceRoot, bool deleteEmptyFolders,
        IProgress<TransferProgress>? progress, CancellationToken ct)
        => Task.Run(() => Run(files, destDir, move, skipIdentical, sourceRoot, deleteEmptyFolders, progress, ct), CancellationToken.None);

    public static TransferResult Run(
        IReadOnlyList<string> files, string destDir, bool move, bool skipIdentical,
        string? sourceRoot, bool deleteEmptyFolders,
        IProgress<TransferProgress>? progress, CancellationToken ct)
    {
        var result = new TransferResult();
        Directory.CreateDirectory(destDir);
        var touchedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var destFull = Path.GetFullPath(destDir).TrimEnd('\\', '/');

        for (int i = 0; i < files.Count; i++)
        {
            if (ct.IsCancellationRequested) { result.Cancelled = true; break; }
            var src = files[i];
            progress?.Report(new TransferProgress(i, files.Count, Path.GetFileName(src)));
            try
            {
                var srcDir = Path.GetDirectoryName(Path.GetFullPath(src))!.TrimEnd('\\', '/');
                if (string.Equals(srcDir, destFull, StringComparison.OrdinalIgnoreCase)) continue; // already there

                var target = ResolveTarget(src, destDir, skipIdentical, out var duplicate, out var renamed);
                if (duplicate) { result.SkippedDuplicates++; continue; }

                if (move) File.Move(src, target); else File.Copy(src, target, false);
                result.Transferred++;
                if (renamed) result.Renamed++;
                if (move) touchedDirs.Add(srcDir);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                result.Errors.Add($"{Path.GetFileName(src)}: {e.Message}");
            }
        }
        progress?.Report(new TransferProgress(files.Count, files.Count, ""));

        if (move && deleteEmptyFolders && sourceRoot != null)
            result.RemovedFolders = RemoveEmptyFolders(touchedDirs, sourceRoot, destFull);
        return result;
    }

    /// Finds a free name in destDir. If skipIdentical and an identical file already exists, duplicate=true.
    public static string ResolveTarget(string src, string destDir, bool skipIdentical, out bool duplicate, out bool renamed)
    {
        duplicate = false; renamed = false;
        var name = Path.GetFileNameWithoutExtension(src);
        var ext = Path.GetExtension(src);
        var candidate = Path.Combine(destDir, name + ext);
        for (int n = 1; File.Exists(candidate); n++)
        {
            if (skipIdentical && Identical(src, candidate)) { duplicate = true; return candidate; }
            candidate = Path.Combine(destDir, $"{name}_{n}{ext}");
            renamed = true;
        }
        return candidate;
    }

    public static bool Identical(string a, string b)
    {
        var fa = new FileInfo(a); var fb = new FileInfo(b);
        if (fa.Length != fb.Length) return false;
        using var sa = fa.OpenRead(); using var sb = fb.OpenRead();
        var ba = new byte[81920]; var bb = new byte[81920];
        int ra;
        while ((ra = sa.Read(ba, 0, ba.Length)) > 0)
        {
            int total = 0;
            while (total < ra)
            {
                int rb = sb.Read(bb, total, ra - total);
                if (rb == 0) return false;
                total += rb;
            }
            if (!ba.AsSpan(0, ra).SequenceEqual(bb.AsSpan(0, ra))) return false;
        }
        return true;
    }

    private static int RemoveEmptyFolders(IEnumerable<string> dirs, string sourceRoot, string destFull)
    {
        var root = Path.GetFullPath(sourceRoot).TrimEnd('\\', '/');
        int removed = 0;
        foreach (var d in dirs.OrderByDescending(x => x.Length))
        {
            var cur = d;
            while (cur.Length > root.Length
                   && cur.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(cur, destFull, StringComparison.OrdinalIgnoreCase)
                   && !destFull.StartsWith(cur + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                   && Directory.Exists(cur) && !Directory.EnumerateFileSystemEntries(cur).Any())
            {
                try { Directory.Delete(cur); removed++; } catch { break; }
                cur = Path.GetDirectoryName(cur)!.TrimEnd('\\', '/');
            }
        }
        return removed;
    }
}
