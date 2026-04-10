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

## Current Work

### 2026-03-13 — Issue #10 Testing (Resolve Button)

**Status:** Completed — resolve-flow regression coverage added and passing.

**Scope:**
- Test resolve action availability (with/without thread ID, resolved/unresolved states)
- Test successful resolve execution, mutation, and response handling
- Test post-resolve refresh and filter updates
- Test failure paths (missing auth, GraphQL error, metadata gaps)
- Regression test preventing accidental file+line thread merging
- Verify no false affordances in UI

**Coordination:**
- Lucius (Backend): Will provide command and mutation implementation
- Selina (Frontend): Will provide command wiring and refresh handler
- Bruce (Lead): Design authority — acceptance bar set

**Authority:** Bruce (Lead) — design approved, execution authorized.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-13 — Resolve flow coverage can stay fast without VS-hosted tests

- The resolve feature can be covered with plain xUnit by faking `GitHubPullRequestService`, constructing Octokit review comments directly, and invoking the internal view model via reflection. That exercises reload/filter behavior without needing a running Visual Studio host.
- For issue #10 specifically, the regression bar now includes: successful resolve refresh, failed resolve preserving unresolved UI state, hidden resolve affordances when thread metadata is missing, and protection against merging separate top-level threads that happen to share file/line coordinates.

### 2026-03-12 — Comment action wiring needs explicit coverage

- The comments list can present actions that are only partially implemented. `PRReviewRemoteUserControl.xaml` binds a visible `Resolve` button to `PrCommentItem.ResolveCommand`, but `PRReviewViewModel` currently leaves that command unset.
- Existing tests only cover tree-building. There is no automated coverage for comment action wiring, command nullability, or resolved/unresolved behavior after a user action.

---

## Issue #10 Team Completion Summary

**Completed:** 2026-03-13  
**Implementation batch:** Lucius (backend), Selina (frontend), Renee (testing) — all work streams synchronized and passing.

**Team outcomes:**
- Lucius delivered `ResolveCommand`, thread-ID capture, and GraphQL mutation wiring
- Selina delivered comments-pane ancestry rendering, action visibility, and post-resolve refresh with filter preservation
- Renee delivered regression coverage for happy path, failure handling, refresh/filter correctness, and thread ancestry protection
- All acceptance criteria met; feature production-ready

**Push authorized and executed:** Bruce reviewed working tree against approved design, ran `dotnet test` successfully, and pushed `squad/remove-squad-product-workflows` to origin. Remote status clean. Documented in decisions.md as "Issue #10 Branch Safe to Push."

**Non-blocking caveat:** `ResolveCommand` resolve-success + reload-failure path remains untested; follow-up should cover this defensive branch.

- The main project targets **net472** (not .NET 8). Test projects must also target net472 to use a project reference; targeting net8.0 is incompatible.
- `PRReviewViewModel` is `internal`. Added `InternalsVisibleTo("Diffinitely.Tests")` via an `<AssemblyAttribute>` item in the main `.csproj` (SDK-style; no AssemblyInfo.cs needed).
- VS Extensibility types (`VisualStudioExtensibility`, `ImageMoniker`, etc.) are available in test projects that reference the main project only if `PrivateAssets` is NOT set to `all` on those packages, or if the test project adds its own reference. In practice, `PRReviewViewModel` was too coupled to VS types to instantiate in tests — the right fix was extracting the tree logic into a `PathTreeBuilder` static helper.
- `String.StartsWith(char)` overload exists only in .NET 5+. On net472, always use `StartsWith(string)` (e.g. `StartsWith("."))`.
- `PathTreeBuilder` uses an `Action<TreeNode, ChangedFileInfo>? leafDecorator` pattern so that VS-specific command wiring stays out of the testable core.
- All 5 xUnit tests pass cleanly with `dotnet test` on net472.

### 2026-03-13 — Unresolve/Re-open test coverage mirrors Resolve pattern exactly

- Added comprehensive test coverage for Unresolve/Re-open functionality following the exact same patterns as existing Resolve tests
- Created `UnresolveCommandTests.cs` (5 tests) covering: command gating (only executable when `IsResolved == true` and `ReviewThreadId` valid), happy path (successful mutation + reload), failure path (no reload on mutation failure), and thread ID validation
- Created `CommentActionAvailabilityTests.cs` (5 tests) verifying mutual exclusivity: when comment is resolved, only Unresolve available; when unresolved, only Resolve available; when thread ID missing, both suppressed
- All tests use internal constructor injection pattern to provide mock mutation delegates, avoiding need for actual GitHub API calls
- Test suite now at 23 tests, all passing. Unresolve feature fully covered at same rigor level as Resolve.

### 2026-04-10 — Unresolve/Re-open Feature Complete (Team Synchronization)

**Status:** Complete and merged to decisions.

**Summary:** Full end-to-end unresolve/re-open feature delivered by three-agent team (Lucius backend, Selina frontend, Renee testing). All 23 tests passing. Feature is production-ready.

**Test Coverage:**

**UnresolveCommandTests.cs** (5 tests):
- ✅ `CanExecute` gating: Executable only when `IsResolved == true` AND `ReviewThreadId` valid
- ✅ Happy path: GitHub API call succeeds, comments reload, status message displayed
- ✅ Failure path: Mutation fails, no reload occurs, error surfaces to user
- ✅ Thread ID validation: Command blocked when ID missing/null/empty
- ✅ Cancellation: Token propagated; operation stops when cancelled

**CommentActionAvailabilityTests.cs** (5 tests):
- ✅ Mutual exclusivity: `IsResolved=true` → only Unresolve available; `IsResolved=false` → only Resolve
- ✅ No thread ID suppression: Both commands unavailable when thread ID is null/empty/whitespace
- ✅ Cross-command verification: Single comment instantiates both; exactly one available at a time
- ✅ State flip blocking: Unresolve cannot flip unresolved threads; Resolve cannot flip resolved threads
- ✅ Reload on success validation: Comments reload after unresolve; filters preserved

**Risk Coverage:**
- ✅ Unresolve cannot flip resolved comments locally without GitHub confirmation
- ✅ Failed unresolve mutation surfaces error; does not hide failure via reload
- ✅ Comments without thread metadata cannot show resolve or unresolve affordances
- ✅ Resolved and unresolved states are mutually exclusive at command level
- ✅ Status messages guide user through operation lifecycle

**Integration:**
- **Lucius:** `UnresolveCommand` + `GitHubPullRequestService.UnresolveReviewThreadAsync` ✅
- **Selina:** Re-open button XAML + `CanUnresolve` property + builder wiring ✅
- **Renee:** Comprehensive test coverage ✅

**Files affected:**
- Commands/UnresolveCommand.cs (new)
- Services/GitHubPullRequestService.cs (unresolve method added)
- Models/PrCommentItem.cs (UnresolveCommand + CanUnresolve properties)
- ToolWindows/CommentThreadBuilder.cs (build signature extended)
- ToolWindows/PRReviewViewModel.cs (unresolve factory added)
- ToolWindows/PRReviewRemoteUserControl.xaml (Re-open button added)
- Tests/UnresolveCommandTests.cs (new)
- Tests/CommentActionAvailabilityTests.cs (new)

**Next:** Scribe orchestration log and decision consolidation complete. Ready for git commit.

