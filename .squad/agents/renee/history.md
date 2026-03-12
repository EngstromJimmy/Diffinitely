# Project Context

- **Owner:** Jimmy Engström
- **Project:** Diffinitely — a Visual Studio extension for reviewing GitHub pull requests and diffs
- **Stack:** C#, WPF/XAML, Visual Studio extension SDK (VSIX), GitHub API, Community Toolkit MVVM
- **Created:** 2026-03-07

## Work Completed

### 2026-03-07 — Dot-Folder Collapse Testing (Issue #1)
- Created `Diffinitely.Tests/` xUnit project targeting net472; added to Diffinitely.slnx
- Extracted `PathTreeBuilder` from `PRReviewViewModel` for testability
- Built 5 test cases covering: dot-root collapse, nested dot-folder collapse, normal folder expansion, leaf nodes, multi-file dot folders
- Fixed compile errors: `StartsWith(char)` → `StartsWith(string)` for net472
- All 5 tests passing; coordinated with Selina; ready for team review

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-12 — Comment action wiring needs explicit coverage

- The comments list can present actions that are only partially implemented. `PRReviewRemoteUserControl.xaml` binds a visible `Resolve` button to `PrCommentItem.ResolveCommand`, but `PRReviewViewModel` currently leaves that command unset.
- Existing tests only cover tree-building. There is no automated coverage for comment action wiring, command nullability, or resolved/unresolved behavior after a user action.

### 2026-03-07 — Test project bootstrap

- The main project targets **net472** (not .NET 8). Test projects must also target net472 to use a project reference; targeting net8.0 is incompatible.
- `PRReviewViewModel` is `internal`. Added `InternalsVisibleTo("Diffinitely.Tests")` via an `<AssemblyAttribute>` item in the main `.csproj` (SDK-style; no AssemblyInfo.cs needed).
- VS Extensibility types (`VisualStudioExtensibility`, `ImageMoniker`, etc.) are available in test projects that reference the main project only if `PrivateAssets` is NOT set to `all` on those packages, or if the test project adds its own reference. In practice, `PRReviewViewModel` was too coupled to VS types to instantiate in tests — the right fix was extracting the tree logic into a `PathTreeBuilder` static helper.
- `String.StartsWith(char)` overload exists only in .NET 5+. On net472, always use `StartsWith(string)` (e.g. `StartsWith("."))`.
- `PathTreeBuilder` uses an `Action<TreeNode, ChangedFileInfo>? leafDecorator` pattern so that VS-specific command wiring stays out of the testable core.
- All 5 xUnit tests pass cleanly with `dotnet test` on net472.

