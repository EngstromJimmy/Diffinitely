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

## Current Work

### 2026-03-13 — Issue #10 Frontend (Resolve Button)

**Status:** Assigned — Orchestration log created, waiting for Lucius backend completion.

**Scope:**
- Populate `ReviewThreadId` into each `PrCommentItem` during model construction
- Wire `ResolveCommand` for items with valid thread ID
- Remove unsafe file+line regrouping pass that collapses threads
- Ensure UI never presents clickable no-op actions
- Implement post-resolve refresh/reload from GitHub

**Coordination:**
- Lucius (Backend): Will provide command and model updates
- Renee (Tester): Will validate refresh/filter correctness
- Bruce (Lead): Design authority

**Authority:** Bruce (Lead) — design approved, execution authorized.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- **Dot-folder collapse (Issue #1):** In `AddPath` on `PRReviewViewModel.cs`, `TreeNode.IsExpanded` is set inline at creation time. The pattern `!isLeaf && !segment.StartsWith('.')` is the right place to gate default expansion — no XAML triggers or style setters needed for this kind of initial-state logic.
- **Comments-tab action affordances:** In `PRReviewRemoteUserControl.xaml`, comment-row actions are rendered directly from per-item command properties on `PrCommentItem`. If an action button binds to a nullable command, the view must also reflect capability state (hide or disable it) or the UI will advertise a no-op.
