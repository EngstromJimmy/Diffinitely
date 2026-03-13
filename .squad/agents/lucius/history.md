# Project Context

- **Owner:** Jimmy Engström
- **Project:** Diffinitely — a Visual Studio extension for reviewing GitHub pull requests and diffs
- **Stack:** C#, WPF/XAML, Visual Studio extension SDK (VSIX), GitHub API, Community Toolkit MVVM
- **Created:** 2026-03-07

## Key Files

- `Diffinitely/Services/GitHubPullRequestService.cs` — GitHub API integration
- `Diffinitely/Services/GitRepositoryService.cs` — local Git operations
- `Diffinitely/Models/` — data models
- `Diffinitely/DiffinitelyPackage.cs` — extension package entry point
- `Diffinitely/ExtensionEntrypoint.cs` — extension initialization
- `Diffinitely/Commands/OpenDiffCommand.cs` — open diff command
- `Diffinitely/Commands/OpenForReviewCommand.cs` — open for review command
- `Diffinitely/PRReviewCommand.cs` — PR review command

## Current Work

### 2026-03-13 — Issue #10 Backend (Resolve Button)

**Status:** Assigned — Orchestration log created, ready to execute.

**Scope:**
- Extend `GitHubPullRequestService.GetReviewThreadResolutionAsync` to capture thread node IDs
- Update `PullRequestInfo` model with thread metadata dictionary
- Add `ReviewThreadId` field to `PrCommentItem`
- Implement `resolveReviewThread` GraphQL mutation method
- Create `ResolveCommand` class implementing `IAsyncCommand`

**Coordination:**
- Selina (Frontend): Will wire command in ViewModel post-completion
- Renee (Tester): Will cover success/failure/refresh with regression tests
- Bruce (Lead): Design authority

**Authority:** Bruce (Lead) — design approved, execution authorized.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
