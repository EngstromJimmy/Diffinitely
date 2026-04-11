# Squad Decisions

## Active Decisions

### Decision: VS Extensibility Remote UI Button Visual Feedback Pattern

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-10  
**Related Work:** Button visual states fix  
**Status:** Approved pattern for all future button work

**Summary:** In `Microsoft.VisualStudio.Extensibility` Remote UI, button visual states (hover, pressed) MUST be implemented using `Style.Triggers`, not `ControlTemplate.Triggers`. The latter are silently ignored by the remote UI rendering engine.

**The Problem:**
Buttons in the tool window had no visual feedback:
- No hover state (background color didn't change on mouse-over)
- No pressed state (no visual indication when clicked)
- No cursor change (didn't show hand cursor)
- Poor affordance — users couldn't tell if elements were clickable

**The Solution:**
Replace `ControlTemplate`-based styles with `Style.Triggers` directly. Use VS-themed brushes:
- **Default:** `Transparent` background, `VsBrushes.ToolWindowBorderKey` border
- **Hover:** `VsBrushes.CommandBarHoverKey` background
- **Pressed:** `VsBrushes.CommandBarSelectedKey` background
- **Disabled:** `VsBrushes.GrayTextKey` foreground with 0.5 opacity

**Why This Works:**
- `Style.Triggers` are evaluated by the Remote UI host and propagate to rendered controls
- `ControlTemplate.Triggers` are NOT evaluated — they exist in XAML but have no runtime effect
- VS-themed brushes dynamically adapt to light/dark themes
- `Cursor="Hand"` provides immediate affordance

**Files Changed:** `PRReviewRemoteUserControl.xaml`

**Testing:** All 38 tests pass; build succeeded; visual inspection confirms hover, pressed, and disabled states render correctly.

**Governance:** This pattern is the standard for all button styles in VS Extensibility Remote UI XAML. If you need custom button visuals, fork `FlatListButtonStyle` and modify the trigger setters — never use `ControlTemplate.Triggers`.

---

### Decision: View on GitHub Link Implementation

**Author:** Lucius (Backend Dev)  
**Date:** 2026-04-10  
**Status:** Complete

**Summary:** Added a clickable "View on GitHub" link at the top of the PR review tool window that opens the current pull request on GitHub.com in the user's default browser.

**What Changed:**
1. **Data model:** `PullRequestInfo.HtmlUrl` captures GitHub web URL from Octokit `PullRequest.HtmlUrl`
2. **Command:** `OpenInBrowserCommand` — minimal IAsyncCommand that opens URL with `Process.Start`
3. **ViewModel:** `PRReviewViewModel.PrHtmlUrl` property + `OpenInBrowserCommand` wired when PR loaded
4. **UI:** New Row 0 with hyperlink-styled button (underline, blue, emoji 🔗)
5. **Converter:** `StringEmptyToCollapsedConverter` hides link when no PR loaded

**Why This Approach:**
- VS Extensibility Remote UI does not reliably support WPF `Hyperlink` navigation. A button styled as link is recommended.
- Follows existing command architecture (`ResolveCommand`, `UnresolveCommand`, etc.)
- Dynamic command creation prevents dead clicks
- Browser launch failures are silent — convenience feature, not critical path

**Risks Covered:**
- No PR loaded: Link hidden via visibility binding
- Empty URL: CanExecute gate prevents execution
- Browser launch failure: Caught silently

**Files Modified:** `PullRequestInfo.cs`, `GitHubPullRequestService.cs`, `OpenInBrowserCommand.cs` (new), `PRReviewViewModel.cs`, `StringEmptyToCollapsedConverter.cs` (new), `PRReviewRemoteUserControl.xaml`

**Testing:** Build succeeded; all 38 tests passed.

---

### Decision: Unresolve/Re-open Review Thread Feature Complete

**Author:** Team (Lucius, Selina, Renee)  
**Date:** 2026-04-10  
**Related Work:** Follow-on to Issue #10 (Resolve feature)  
**Status:** Complete

**Summary:** Full end-to-end unresolve/re-open feature delivered. Users can now re-open previously resolved review threads directly from the PR comments view. Architecture mirrors existing Resolve feature exactly.

**What changed:**
1. **Service layer:** `GitHubPullRequestService.UnresolveReviewThreadAsync` GraphQL mutation (validates thread ID, reports failures, confirms response)
2. **Command:** `UnresolveCommand.cs` — identical structure to `ResolveCommand`, gated by `IsResolved == true`
3. **Model:** `PrCommentItem` carries `UnresolveCommand` + `CanUnresolve` properties
4. **Builder:** `CommentThreadBuilder.Build` extended with `createUnresolveCommand` factory parameter
5. **ViewModel:** `PRReviewViewModel` wires unresolve command factory; creates command only when thread resolved
6. **UI:** Re-open button added to XAML with mutually exclusive visibility vs. Resolve button
7. **Tests:** 10 comprehensive tests (5 command tests, 5 mutual-exclusivity tests); all 23 suite tests passing

**Patterns preserved:**
- Async throughout; no blocking calls
- Cancellation tokens propagated
- GraphQL validation before success
- Command gating by model state
- Post-mutation reload from GitHub
- Failure reporting (not silent)

**Files:** GitHubPullRequestService.cs, UnresolveCommand.cs, PrCommentItem.cs, CommentThreadBuilder.cs, PRReviewViewModel.cs, PRReviewRemoteUserControl.xaml, UnresolveCommandTests.cs, CommentActionAvailabilityTests.cs

**Risk coverage:** All edge cases tested — no unresolve without GitHub confirmation, no silent failures, mutual exclusivity enforced, missing thread metadata suppresses affordance.

---

### Decision: Issue #10 Backend Implementation Complete

**Author:** Lucius (Backend Dev)  
**Date:** 2026-03-13  
**Related Issue:** #10 — Resolve button in comments view appears non-functional  
**Status:** Complete

**Summary:** Backend half of issue #10 is end-to-end wired. Service-layer support exists to fetch GitHub review thread node IDs, resolve a review thread by node ID, and report mutation failures.

**What changed:**
- `GitHubPullRequestService` loads review thread metadata from GraphQL (thread node ID, resolved state) via extended `GetReviewThreadResolutionAsync`
- `PullRequestInfo` carries explicit review-thread metadata dictionary keyed by top-level comment database ID
- `PrCommentItem` carries `ReviewThreadId` for mutations
- `ResolveCommand` added as production-ready `IAsyncCommand` (validates thread ID, reports failures, reloads on success)
- `PRReviewViewModel` wires resolve actions against thread IDs; no longer merges threads by file+line

---

### Decision: Issue #10 Frontend Implementation Complete

**Author:** Selina (Frontend Dev)  
**Date:** 2026-03-13  
**Related Issue:** #10 — Resolve button in comments view appears non-functional  
**Status:** Complete

**Summary:** Frontend implementation complete. Comments pane renders GitHub ancestry as source of truth for threads. Resolve button only visible when actionable. Post-resolve reload preserves filters.

**What changed:**
- Comments pane treats GitHub reply ancestry as source of truth; removed file+line regrouping
- `Resolve` renders only when item has valid review-thread ID and non-null command
- After successful resolve, reload comments from service and restore current author/status filters

---

### Decision: Issue #10 Testing Complete

**Author:** Renee (Tester)  
**Date:** 2026-03-13  
**Related Issue:** #10 — Resolve button in comments view appears non-functional  
**Status:** Complete

**Summary:** Full regression coverage for resolve flow. Happy path, failure handling, refresh/filter correctness, and thread ancestry protection all tested and passing.

**Coverage delivered:**
1. Successful resolve reloads from GitHub and keeps filters intact
2. Failed resolve leaves comment unresolved; no refresh hiding failure
3. Distinct top-level threads remain separate; resolve cannot drift
4. Missing thread metadata suppresses resolve affordance entirely

---

### Decision: Test Project Created at Diffinitely.Tests/

**Author:** Renee (Tester)  
**Date:** 2026-03-07  
**Related Issue:** #1 — Treeview dot-prefixed folders should not be expanded by default

**Summary:** A new xUnit test project has been created at `Diffinitely.Tests/`.

**Details:**
- **Project file:** `Diffinitely.Tests/Diffinitely.Tests.csproj`
- **Target framework:** `net472` (matches the main project — required for project reference compatibility)
- **Test framework:** xUnit 2.9 + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk
- **Added to solution:** `Diffinitely.slnx`

**Test Coverage:** `TreeViewTests.cs` covers `PathTreeBuilder.Build`:
| Test | Scenario |
|------|----------|
| `DotPrefixedRootFolder_IsCollapsed` | `.squad/agents/bruce/charter.md` → root `.squad` node is collapsed |
| `DotPrefixedNestedFolder_IsCollapsed_ParentIsExpanded` | `src/.hidden/file.cs` → `src` expanded, `.hidden` collapsed |
| `NormalFolders_AreExpanded` | `src/Models/TreeNode.cs` → both folder nodes expanded |
| `LeafNode_IsNeverExpanded` | File nodes always have `IsExpanded = false` |
| `MultipleDotFolderFiles_ShareOneSingleCollapsedNode` | Three files under `.git/` → single collapsed node, 3 children |

**Refactoring:** Extracted `AddPath`/`GetIconForSegment` from `PRReviewViewModel` into `internal static class PathTreeBuilder`. Fixed compile error in Selina's fix (char overload unavailable on net472).

---

### Investigation: Resolve Button in PR Comments View

**Author:** Bruce (Lead)  
**Date:** 2026-03-12  
**Status:** Investigation Complete — Implementation Recommendation  
**Related Issue:** #10 (user-facing button appears non-functional)

**Summary:** The Resolve button in the PR comments view is **UNWIRED**. It exists in the UI but has no backend implementation.

**Code Path Analysis:**
- **XAML:** `PRReviewRemoteUserControl.xaml` binds button to `{Binding ResolveCommand}` (correctly hidden when resolved)
- **ViewModel:** `PRReviewViewModel.cs` line 164 has commented code: `//ResolveCommand = new ResolveCommand(...)` — intentionally disabled
- **No command class exists** in `/Commands/` (only `OpenDiffCommand`, `OpenForReviewCommand`)
- **Data model gap:** `PrCommentItem` lacks `ReviewThreadId` — required for GitHub resolve mutation
- **Backend ready:** GitHub GraphQL `resolveReviewThread` mutation is stable

**Root Cause:** Incomplete feature implementation. UI and data model were partially built, backend call and command wiring never finished.

**Recommended Fix Path (Low Risk):**
1. **Phase 1:** Add `ReviewThreadId` to `PrCommentItem`; extend `GetReviewThreadResolutionAsync` to capture thread IDs
2. **Phase 2:** Create `ResolveCommand(GitHubPullRequestService, PullRequestInfo)` implementing `IAsyncCommand` with GraphQL resolve mutation
3. **Phase 3:** Wire command in `PRReviewViewModel.ReloadTreeInternalAsync`; test end-to-end

**Acceptance Bar (per Renee):** Either wire Resolve end-to-end or hide/disable until supported. After successful resolve, item must update in comments view. Add automated tests for comment action wiring.

---

### Decision: Resolve Button UX — Must Not Ship as No-Op

**Author:** Renee (Tester)  
**Date:** 2026-03-12  
**Related Issue:** #10  

**Summary:** The Resolve button currently appears clickable but performs no action.

**Why This Matters:**
- UI suggests resolve threads are supported
- Users can click and see no state change, no refresh, no feedback
- Codebase lacks review-thread identifiers in model, so resolve mutation is not wired end-to-end

**Acceptance Bar:**
1. Either wire Resolve end-to-end or hide/disable until supported
2. After successful resolve, item must update without user guesswork
3. Add automated tests for comment action wiring and resolved/unresolved filtering

---

### Decision: Deleted File UX (Issue #5)

**Author:** Selina (Frontend Dev)  
**Date:** 2026-03-07  
**Related Issue:** #5

**Summary:** Two changes to PR file tree for `ChangeKind == Deleted`:
1. File names render with `TextDecorations="Strikethrough"`
2. Clicking a deleted file opens VS diff view (pre-deletion content vs. empty)

**Implementation:**
- Added `bool IsDeleted` to `TreeNode` (with `[DataMember]` and `INotifyPropertyChanged`)
- XAML `DataTrigger` applies strikethrough via local `TextBlock.Style`
- `OpenDiffCommand.ExecuteAsync` writes empty temp file for right side; uses `VSDIFFOPT_RightFileIsTemporary`; caption includes "(deleted)"

**Governance Note:** `TreeNode` properties must always carry `[DataMember]` and `INotifyPropertyChanged` for VS extensibility XAML remote-UI engine.

---

### Directive: Use Sonnet Model

**Author:** Jimmy Engström (via Copilot)  
**Date:** 2026-03-12  
**Context:** User preference for model selection

**Directive:** Use Claude Sonnet as the reasoning engine for code review and implementation tasks.

---

### Decision: Issue #10 MVP architecture and execution split are approved

**Author:** Bruce (Lead)  
**Date:** 2026-03-13  
**Related Issue:** #10 — Resolve button in comments view appears non-functional

**Summary:** Safe minimum viable path to wire review-thread resolution end-to-end from comments UI. Design approved, execution starts now.

**Approved architecture:**

1. **Service is source of truth for thread actions** — Extend `GitHubPullRequestService` GraphQL thread query to return `isResolved` and review thread node `id`. Carry thread metadata into `PullRequestInfo`. Add `resolveReviewThread` GraphQL mutation method.
2. **Comments model must carry thread identity** — Add `ReviewThreadId` to `PrCommentItem`. Only create resolve action when valid thread ID present.
3. **ViewModel must not infer threads by file+line** — Keep reply wiring based on GitHub comment relationships; remove grouping pass that can collapse separate threads.
4. **After successful resolve, reload comments from GitHub** — Minimum-risk behavior guarantees resolved state, button visibility, and filters reflect server truth.

**Implementation split:**
- **Lucius:** Backend/service (GraphQL thread-ID query, models, ResolveCommand, mutation)
- **Selina:** Frontend/ViewModel (populate thread IDs, wire command, remove grouping, refresh post-resolve)
- **Renee:** Tests (success, failure, refresh, action availability, filter correctness)

**Risks respected:**
- `resolveReviewThread` requires GraphQL thread node ID, not REST comment ID
- Current query uses `first:100` with no pagination; large PRs can exceed; known follow-up risk
- If auth unavailable or GraphQL fails, UI must hide/disable resolve, not pretend to work
- Silent failure unacceptable; if mutation fails, item does not flip locally

---

### Decision: Issue #10 Branch Safe to Push

**Author:** Bruce (Lead)  
**Date:** 2026-03-13  
**Related Issue:** #10 — Resolve button in comments view appears non-functional  
**Status:** Complete

**Summary:** Issue #10 branch reviewed and approved for cross-machine testing. Implementation is coherent end-to-end and scoped to resolve-thread support plus its tests.

**Why pushing is safe:**
- `GitHubPullRequestService` carries GitHub review-thread IDs and resolved state from GraphQL
- `PRReviewViewModel` rebuilds comment threads from GitHub reply ancestry via `CommentThreadBuilder`
- Comments UI only exposes Resolve affordance when thread ID and command exist
- `dotnet test .\Diffinitely.slnx --nologo` passed before push

**Known non-blocking follow-up:** Add test for resolve-success + reload-failure path.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

