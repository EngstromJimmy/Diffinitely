# Project Context

- **Owner:** Jimmy Engström
- **Project:** Diffinitely — a Visual Studio extension for reviewing GitHub pull requests and diffs
- **Stack:** C#, WPF/XAML, Visual Studio extension SDK (VSIX), GitHub API, Community Toolkit MVVM
- **Created:** 2026-03-07

## Key Files

- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml` — main PR review UI
- `Diffinitely/ToolWindows/PRReviewViewModel.cs` — ViewModel for PR review
- `Diffinitely/ToolWindows/BooleanToCollapsedWhenTrueConverter.cs` — converter
- `Diffinitely/ToolWindows/BooleanToVisibleWhenTrueConverter.cs` — converter
- `Diffinitely/ToolWindows/ZeroToCollapsedConverter.cs` — converter

## Work Completed

### 2026-03-07 — Dot-Folder Collapse (Issue #1)
- Fixed `IsExpanded` logic in `PRReviewViewModel.cs`; gate expansion with `!isLeaf && !segment.StartsWith(".")`
- Extracted tree-building logic from `PRReviewViewModel` into testable `PathTreeBuilder` static helper
- Pattern enables clean separation of concerns; VS-specific leaf decoration is injectable via action delegate
- Coordinated with Renee; 5 tests passing, feature complete

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **Dot-folder collapse (Issue #1):** In `AddPath` on `PRReviewViewModel.cs`, `TreeNode.IsExpanded` is set inline at creation time. The pattern `!isLeaf && !segment.StartsWith('.')` is the right place to gate default expansion — no XAML triggers or style setters needed for this kind of initial-state logic.
- **Comments-tab action affordances:** In `PRReviewRemoteUserControl.xaml`, comment-row actions are rendered directly from per-item command properties on `PrCommentItem`. If an action button binds to a nullable command, the view must also reflect capability state (hide or disable it) or the UI will advertise a no-op.
