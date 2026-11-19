using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IPhoneCopier.Models;

public partial class FolderNode : ObservableObject
{
    public FolderNode(string name, string path, int fileCount)
    {
        Name = name;
        Path = path;
        FileCount = fileCount;
    }

    public string Name { get; }
    public string Path { get; }
    public int FileCount { get; }
    public ObservableCollection<FolderNode> Children { get; } = new();
    public int TotalCount => FileCount + Children.Sum(c => c.TotalCount);
    public string Label => $"{Name}  ({FileCount})";

    [ObservableProperty]
    private bool _isChecked = true;

    partial void OnIsCheckedChanged(bool value)
    {
        foreach (var c in Children) c.IsChecked = value;
    }

    public System.Collections.Generic.IEnumerable<FolderNode> Flatten()
    {
        yield return this;
        foreach (var c in Children)
            foreach (var d in c.Flatten())
                yield return d;
    }
}
