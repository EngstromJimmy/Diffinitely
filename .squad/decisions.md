# Squad Decisions

## Active Decisions

# Decision: CI/CD Release Workflow for VSIX

**Author:** Bruce (Lead)  
**Date:** 2026-03-07  
**PR:** #3 — feat: add CI/CD release workflow for VSIX

## Summary

A GitHub Actions release workflow has been added at `.github/workflows/release.yml`.

## Details

- **Trigger:** Push to `main` (i.e., every merged PR)
- **Runner:** `windows-latest` — required for VSSDK/MSBuild and net472 test execution
- **Restore:** `dotnet restore Diffinitely/Diffinitely.csproj` — `nuget restore` does not support the `.slnx` format
- **Build:** `msbuild` with `/p:DeployExtension=false /p:SkipOpenVisualStudio=true`
- **Test:** `dotnet test` with TRX logger; results uploaded as artifact even on failure
- **Release:** `softprops/action-gh-release@v2`; release only created if tests pass
- **Versioning:** `v1.0.{run_number}` — sequential build numbers; can migrate to semver tags later
- **Artifact:** `Diffinitely/bin/Release/net472/Diffinitely.vsix` attached to release

## Rationale

Solo project — single-job workflow is simpler than multi-job. Tests are ordered before the release step so a test failure aborts the release. The `contents: write` permission is scoped at the workflow level.

## Future Considerations

- Switch to semver tagging (e.g., push `vX.Y.Z` tags manually) when versioning discipline is needed
- Add code signing step when the extension is published to the VS Marketplace

---

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

