using System.Collections.Generic;
using Diffinitely.Models;
using Diffinitely.ToolWindows;
using Xunit;

namespace Diffinitely.Tests;

/// <summary>
/// Tests for PathTreeBuilder.Build focusing on IsExpanded behaviour for
/// dot-prefixed folder nodes.
///
/// These tests cover the fix in Issue #1: dot-prefixed folders (e.g. .git,
/// .squad) must NOT be expanded by default.
/// </summary>
public class TreeViewTests
{
    private static ChangedFileInfo File(string path) =>
        new ChangedFileInfo { Path = path, FullPath = path, Kind = ChangeKind.Modified };

    // -------------------------------------------------------------------------
    // a. Dot-prefixed ROOT folder must be collapsed
    // -------------------------------------------------------------------------
    [Fact]
    public void DotPrefixedRootFolder_IsCollapsed()
    {
        var roots = PathTreeBuilder.Build([File(".squad/agents/bruce/charter.md")]);

        var dotSquad = roots[0];
        Assert.Equal(".squad", dotSquad.Name);
        Assert.False(dotSquad.IsExpanded,
            "A dot-prefixed root folder should NOT be expanded by default.");
    }

    // -------------------------------------------------------------------------
    // b. Dot-prefixed NESTED folder must be collapsed; parent stays expanded
    // -------------------------------------------------------------------------
    [Fact]
    public void DotPrefixedNestedFolder_IsCollapsed_ParentIsExpanded()
    {
        var roots = PathTreeBuilder.Build([File("src/.hidden/file.cs")]);

        var src = roots[0];
        Assert.Equal("src", src.Name);
        Assert.True(src.IsExpanded, "A normal parent folder should be expanded.");

        var hidden = src.Children[0];
        Assert.Equal(".hidden", hidden.Name);
        Assert.False(hidden.IsExpanded,
            "A dot-prefixed nested folder should NOT be expanded by default.");
    }

    // -------------------------------------------------------------------------
    // c. Normal folder nodes must be expanded
    // -------------------------------------------------------------------------
    [Fact]
    public void NormalFolders_AreExpanded()
    {
        var roots = PathTreeBuilder.Build([File("src/Models/TreeNode.cs")]);

        var src = roots[0];
        Assert.Equal("src", src.Name);
        Assert.True(src.IsExpanded, "'src' should be expanded.");

        var models = src.Children[0];
        Assert.Equal("Models", models.Name);
        Assert.True(models.IsExpanded, "'Models' should be expanded.");
    }

    // -------------------------------------------------------------------------
    // d. File (leaf) node must never be expanded — regardless of name
    // -------------------------------------------------------------------------
    [Fact]
    public void LeafNode_IsNeverExpanded()
    {
        var roots = PathTreeBuilder.Build([File("src/Models/TreeNode.cs")]);

        var leaf = roots[0].Children[0].Children[0];
        Assert.Equal("TreeNode.cs", leaf.Name);
        Assert.False(leaf.IsExpanded, "A file (leaf) node must never be expanded.");
    }

    // -------------------------------------------------------------------------
    // e. Multiple files under a dot folder produce one collapsed node (no dups)
    // -------------------------------------------------------------------------
    [Fact]
    public void MultipleDotFolderFiles_ShareOneSingleCollapsedNode()
    {
        var roots = PathTreeBuilder.Build([
            File(".git/config"),
            File(".git/HEAD"),
            File(".git/ORIG_HEAD")]);

        // Exactly one root node named ".git"
        Assert.Single(roots, n => n.Name == ".git");
        var gitNode = roots[0];
        Assert.Equal(".git", gitNode.Name);
        Assert.False(gitNode.IsExpanded,
            "The shared .git folder node should NOT be expanded by default.");

        // Three children — one per file
        Assert.Equal(3, gitNode.Children.Count);
    }
}
