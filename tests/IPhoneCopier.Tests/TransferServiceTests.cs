using IPhoneCopier.Services;
using Xunit;

namespace IPhoneCopier.Tests;

public class TransferServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ipc_" + Guid.NewGuid().ToString("N"));
    private string Src => Path.Combine(_root, "src");
    private string Dst => Path.Combine(_root, "dst");

    public TransferServiceTests() { Directory.CreateDirectory(Src); }
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private string Make(string folder, string name, string content)
    {
        var d = Path.Combine(Src, folder); Directory.CreateDirectory(d);
        var p = Path.Combine(d, name); File.WriteAllText(p, content); return p;
    }

    private TransferResult Run(IReadOnlyList<string> files, bool move = true, bool skip = true) =>
        TransferService.Run(files, Dst, move, skip, Src, true, null, CancellationToken.None);

    [Fact]
    public void Move_flattens_and_removes_empty_folders()
    {
        var a = Make("100APPLE", "IMG_1.JPG", "a");
        var b = Make("101APPLE", "IMG_2.JPG", "b");
        var r = Run(new[] { a, b });
        Assert.Equal(2, r.Transferred);
        Assert.True(File.Exists(Path.Combine(Dst, "IMG_1.JPG")));
        Assert.True(File.Exists(Path.Combine(Dst, "IMG_2.JPG")));
        Assert.Equal(2, r.RemovedFolders);
        Assert.True(Directory.Exists(Src));
        Assert.Empty(Directory.GetDirectories(Src));
    }

    [Fact]
    public void Same_name_different_content_is_renamed()
    {
        var a = Make("100APPLE", "IMG_1.JPG", "first");
        var b = Make("101APPLE", "IMG_1.JPG", "second");
        var r = Run(new[] { a, b });
        Assert.Equal(1, r.Renamed);
        Assert.Equal("first", File.ReadAllText(Path.Combine(Dst, "IMG_1.JPG")));
        Assert.Equal("second", File.ReadAllText(Path.Combine(Dst, "IMG_1_1.JPG")));
    }

    [Fact]
    public void Identical_duplicate_is_skipped_and_source_kept()
    {
        var a = Make("100APPLE", "IMG_1.JPG", "same");
        var b = Make("101APPLE", "IMG_1.JPG", "same");
        var r = Run(new[] { a, b });
        Assert.Equal(1, r.Transferred);
        Assert.Equal(1, r.SkippedDuplicates);
        Assert.True(File.Exists(b));
        Assert.Single(Directory.GetFiles(Dst));
    }

    [Fact]
    public void Rerun_is_idempotent()
    {
        var a = Make("100APPLE", "IMG_1.JPG", "first");
        var b = Make("101APPLE", "IMG_1.JPG", "second");
        Run(new[] { a, b }, move: false);
        var r = Run(new[] { a, b }, move: false);
        Assert.Equal(0, r.Transferred);
        Assert.Equal(2, r.SkippedDuplicates);
        Assert.Equal(2, Directory.GetFiles(Dst).Length);
    }

    [Fact]
    public void Copy_leaves_source_intact()
    {
        var a = Make("100APPLE", "IMG_1.JPG", "a");
        Run(new[] { a }, move: false);
        Assert.True(File.Exists(a));
        Assert.True(File.Exists(Path.Combine(Dst, "IMG_1.JPG")));
    }

    [Fact]
    public void Folder_with_remaining_files_is_not_deleted()
    {
        var a = Make("100APPLE", "IMG_1.JPG", "a");
        Make("100APPLE", "notes.txt", "keep");
        Run(new[] { a });
        Assert.True(Directory.Exists(Path.Combine(Src, "100APPLE")));
    }
}
