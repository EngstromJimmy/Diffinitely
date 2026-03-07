using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Diffinitely.Models;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.ToolWindows;

/// <summary>
/// Pure tree-building logic extracted from PRReviewViewModel so that it can be
/// tested without a running Visual Studio instance.
/// </summary>
internal static class PathTreeBuilder
{
    /// <summary>
    /// Builds a hierarchical tree of <see cref="TreeNode"/> values from a flat list
    /// of changed file paths.
    /// </summary>
    /// <param name="files">Files whose paths form the tree structure.</param>
    /// <param name="leafDecorator">
    /// Optional callback invoked for every leaf (file) node after it is created.
    /// The caller can use this to attach commands that depend on VS services.
    /// </param>
    public static ObservableCollection<TreeNode> Build(
        IEnumerable<ChangedFileInfo> files,
        Action<TreeNode, ChangedFileInfo>? leafDecorator = null)
    {
        var roots = new ObservableCollection<TreeNode>();
        foreach (var f in files)
            AddPath(roots, f, 0, leafDecorator);
        return roots;
    }

    private static void AddPath(
        ObservableCollection<TreeNode> nodes,
        ChangedFileInfo fileInfo,
        int index,
        Action<TreeNode, ChangedFileInfo>? leafDecorator)
    {
        var parts = fileInfo.Path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        bool isLeaf = index == parts.Length - 1;
        string segment = parts[index];

        var node = nodes.FirstOrDefault(n => n.Name == segment);
        if (node == null)
        {
            node = new TreeNode
            {
                Name = segment,
                Icon = GetIconForSegment(segment, isLeaf),
                IsExpanded = !isLeaf && !segment.StartsWith("."),
                FullPath = fileInfo.FullPath
            };

            if (isLeaf)
            {
                node.CommentCount = fileInfo.CommentCount;
                node.ChangeKind = fileInfo.Kind.ToString();
                leafDecorator?.Invoke(node, fileInfo);
            }

            nodes.Add(node);
        }

        if (!isLeaf)
            AddPath(node.Children, fileInfo, index + 1, leafDecorator);
    }

    private static ImageMoniker GetIconForSegment(string name, bool isLeaf)
    {
        if (!isLeaf)
            return ImageMoniker.KnownValues.FolderClosed;

        return Path.GetExtension(name)?.ToLowerInvariant() switch
        {
            ".cs"      => ImageMoniker.KnownValues.CSFileNode,
            ".vb"      => ImageMoniker.KnownValues.VBFileNode,
            ".ts"      => ImageMoniker.KnownValues.TSSourceFile,
            ".tsx"     => ImageMoniker.KnownValues.TSSourceFile,
            ".js"      => ImageMoniker.KnownValues.JSScript,
            ".jsx"     => ImageMoniker.KnownValues.JSScript,
            ".css"     => ImageMoniker.KnownValues.CSSClass,
            ".scss"    => ImageMoniker.KnownValues.CSSClass,
            ".less"    => ImageMoniker.KnownValues.CSSClass,
            ".html"    => ImageMoniker.KnownValues.HTMLFile,
            ".htm"     => ImageMoniker.KnownValues.HTMLFile,
            ".razor"   => ImageMoniker.KnownValues.ASPRazorFile,
            ".cshtml"  => ImageMoniker.KnownValues.ASPRazorFile,
            ".json"    => ImageMoniker.KnownValues.JSONScript,
            ".yml"     => ImageMoniker.KnownValues.YamlFile,
            ".yaml"    => ImageMoniker.KnownValues.YamlFile,
            ".xml"     => ImageMoniker.KnownValues.XMLFile,
            ".config"  => ImageMoniker.KnownValues.SettingsFile,
            ".props"   => ImageMoniker.KnownValues.XMLFile,
            ".targets" => ImageMoniker.KnownValues.XMLFile,
            ".md"      => ImageMoniker.KnownValues.MarkdownFile,
            ".txt"     => ImageMoniker.KnownValues.TextFile,
            ".log"     => ImageMoniker.KnownValues.TextFile,
            ".license" => ImageMoniker.KnownValues.TextFile,
            ".sln"     => ImageMoniker.KnownValues.Solution,
            ".csproj"  => ImageMoniker.KnownValues.CSProjectNode,
            ".vbproj"  => ImageMoniker.KnownValues.VBProjectNode,
            ".fsproj"  => ImageMoniker.KnownValues.FSProjectNode,
            ".proj"    => ImageMoniker.KnownValues.CSProjectNode,
            ".nuspec"  => ImageMoniker.KnownValues.NuGet,
            ".nupkg"   => ImageMoniker.KnownValues.NuGet,
            ".png"     => ImageMoniker.KnownValues.Image,
            ".jpg"     => ImageMoniker.KnownValues.Image,
            ".jpeg"    => ImageMoniker.KnownValues.Image,
            ".gif"     => ImageMoniker.KnownValues.Image,
            ".svg"     => ImageMoniker.KnownValues.Image,
            _          => ImageMoniker.KnownValues.TargetFile
        };
    }
}
