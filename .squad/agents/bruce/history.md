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

### Resolve mutations require thread IDs (2026-03-12)

The GitHub service already reads review-thread resolution state, but it currently throws away the review thread identifier and keeps only comment-level resolution flags. Any future resolve/unresolve action has to carry the GitHub review thread ID from the GraphQL query into the view model; comment IDs alone are not sufficient.

### Issue #10 design review approved (2026-03-13)

- Minimum viable architecture is sound: keep the existing comments UI, but carry the GitHub review thread node ID from GraphQL into the comment model, add a `resolveReviewThread` mutation in `GitHubPullRequestService`, and wire a real `ResolveCommand`.
- The current fallback grouping by `(FilePath, Line)` in `PRReviewViewModel` is structurally unsafe for thread actions because it can merge separate GitHub review threads; thread identity must come from GitHub relationships, not inferred UI grouping.
- For the first implementation, a successful resolve should trigger a reload of the comments data rather than relying on optimistic local mutation alone. That keeps resolved/unresolved filters correct and avoids remote-UI property change surprises.
- Execution can start immediately with a clean split: Lucius owns service/model/command plumbing, Selina owns view-model wiring and command visibility, Renee owns regression coverage for successful resolve, hidden/disabled unsupported states, filter refresh, and failure handling.

### Issue #10 push review (2026-03-13)

- Reviewed the working tree against the approved issue #10 design and found it aligned: thread identity is carried from GraphQL into `PrCommentItem`, command visibility is guarded by actual capability, and comment-thread construction now follows GitHub reply ancestry instead of file+line inference.
- Validation bar for a push on this code path should include `dotnet test .\Diffinitely.slnx --nologo`, not just the targeted test project, because the VSIX build and test assembly reference wiring both matter for this feature.
- One non-blocking gap remains: `ResolveCommand`'s "mutation succeeded but refresh failed" status path is still untested, so future follow-up should cover that defensive branch without holding the push.

### Issue #10 push execution (2026-03-13)

- Pushed `squad/remove-squad-product-workflows` branch to origin; remote status clean and synced.
- Decision documented in `.squad/decisions.md`: "Issue #10 Branch Safe to Push"
- Team cross-sync initiated: Scribe appended completion summary to Lucius, Renee, and Selina history files

### PR Review Thread Features Brainstorm (2026-03-13)

- Completed strategic feature planning for review thread capabilities after Resolve/Unresolve shipped
- Key themes emerged: (1) inline reply/editing for complete conversation flow, (2) reactions for quick acknowledgment, (3) quick jump-to-diff from comments, (4) improved state persistence on refresh, (5) PR-level summary and filtering
- Highest-value/lowest-complexity features identified: jump to diff improvement, reply to threads, mark as outdated detection
- Hard blockers noted: GitHub reactions API has incomplete review-comment support; some features require PRThreadComment object unavailable in current GraphQL queries
- Priority driven by IDE vs web gap: developers reviewing in VS lose conversation flow continuity when they have to switch to GitHub web to reply or see full context
