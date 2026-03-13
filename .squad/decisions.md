# Squad Decisions

## Active Decisions

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

