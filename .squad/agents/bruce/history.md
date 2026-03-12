# Project Context

- **Owner:** Jimmy Engström
- **Project:** Diffinitely — a Visual Studio extension for reviewing GitHub pull requests and diffs
- **Stack:** C#, WPF/XAML, Visual Studio extension SDK (VSIX), GitHub API, Community Toolkit MVVM
- **Created:** 2026-03-07

## Key Files

- `Diffinitely/DiffinitelyPackage.cs` — extension entry point
- `Diffinitely/Commands/` — VS commands (OpenDiffCommand, OpenForReviewCommand)
- `Diffinitely/ToolWindows/` — PR review tool window (WPF XAML + ViewModel)
- `Diffinitely/Services/GitHubPullRequestService.cs` — GitHub API integration
- `Diffinitely/Services/GitRepositoryService.cs` — local git operations
- `Diffinitely/Models/` — data models

## Learnings

### Resolve Button Investigation (2026-03-07, Completed 2026-03-12)

**Finding:** The Resolve button in PR comments is **UNWIRED** — no implementation exists.

**Evidence:**
1. **XAML:** Button bound to `{Binding ResolveCommand}` in `PRReviewRemoteUserControl.xaml` (visible when `IsResolved == false`)
2. **Model:** `PrCommentItem.ResolveCommand` property declared but **never set** (line 164 in `PRReviewViewModel.cs` shows commented code: `//ResolveCommand = new ResolveCommand(...)`)
3. **No command class exists:** Only `OpenForReviewCommand` and `OpenDiffCommand` in `/Commands/` — no `ResolveCommand.cs`
4. **Backend API available:** GitHub GraphQL has `resolveReviewThread` mutation (confirmed in docs); REST API insufficient (can't resolve threads directly)
5. **Missing identifiers:** `PrCommentItem` captures only comment ID; resolving requires **review thread ID** (not populated from `PullRequestReviewComment` in Octokit library)

**Root cause:** Incomplete feature. Wire was planned but never implemented — likely deprioritized.

**Recommended fix (3-phase, low risk):**
- **Phase 1:** Add `ReviewThreadId` to `PrCommentItem`; extend `GetReviewThreadResolutionAsync` to return thread IDs
- **Phase 2:** Create `ResolveCommand(GitHubPullRequestService, PullRequestInfo)` implementing `IAsyncCommand` with GraphQL mutation
- **Phase 3:** Wire in `PRReviewViewModel.ReloadTreeInternalAsync`; test end-to-end

**Team decision:** Resolve button must not ship as no-op. Either wire end-to-end or hide until supported. Acceptance bar: after successful resolve, item updates without user guesswork; add automated tests for comment action wiring.

**Status:** Findings documented in `.squad/decisions.md`; Scribe merged all inbox decisions (deduped). Ready for prioritization.
