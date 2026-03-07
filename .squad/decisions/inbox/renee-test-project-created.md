# Decision: Test Project Created at Diffinitely.Tests/

**Author:** Renee (Tester)  
**Date:** 2026-03-07  
**Related Issue:** #1 — Treeview dot-prefixed folders should not be expanded by default

## Summary

A new xUnit test project has been created at `Diffinitely.Tests/`.

## Details

- **Project file:** `Diffinitely.Tests/Diffinitely.Tests.csproj`
- **Target framework:** `net472` (matches the main project — required for project reference compatibility)
- **Test framework:** xUnit 2.9 + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk
- **Added to solution:** `Diffinitely.slnx`

## What was tested

`TreeViewTests.cs` covers `PathTreeBuilder.Build` (the extracted tree-building helper):

| Test | Scenario |
|------|----------|
| `DotPrefixedRootFolder_IsCollapsed` | `.squad/agents/bruce/charter.md` → root `.squad` node is collapsed |
| `DotPrefixedNestedFolder_IsCollapsed_ParentIsExpanded` | `src/.hidden/file.cs` → `src` expanded, `.hidden` collapsed |
| `NormalFolders_AreExpanded` | `src/Models/TreeNode.cs` → both folder nodes expanded |
| `LeafNode_IsNeverExpanded` | File nodes always have `IsExpanded = false` |
| `MultipleDotFolderFiles_ShareOneSingleCollapsedNode` | Three files under `.git/` → single collapsed node, 3 children |

## Refactoring performed

To make the tree logic testable without a running VS instance, `AddPath`/`GetIconForSegment` were extracted from `PRReviewViewModel` into a new `internal static class PathTreeBuilder`. `PRReviewViewModel.BuildTreeFromPaths` now delegates to it, passing a leaf-decorator lambda for VS-specific commands.

A compile error in Selina's fix (`StartsWith('.')` char overload not available on net472) was corrected to `StartsWith(".")` as part of getting the build green.
