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
- **True review-thread rendering (Issue #10):** In the comments pane, nested replies need to be attached by walking `InReplyToId` back to the true top-level comment. Grouping separate top-level comments by `FilePath + Line` is unsafe because it invents threads GitHub does not actually have.
- **Re-open button (Unresolve UI):** The Re-open button mirrors the Resolve button with opposite visibility logic. `CanUnresolve` is set in `CommentThreadBuilder.cs` when `threadState.IsResolved == true` and valid `ReviewThreadId` exists. The button binds to `UnresolveCommand` and uses `BooleanToVisibleWhenTrueConverter` on `CanUnresolve`. XAML: `PRReviewRemoteUserControl.xaml` line 437-439. Model: `PrCommentItem.cs` added `CanUnresolve` property. Builder: `CommentThreadBuilder.cs` line 86-89 sets `CanUnresolve`.
- **VS Extensibility Remote UI button triggers:** In `Microsoft.VisualStudio.Extensibility` Remote UI, `ControlTemplate.Triggers` are silently ignored. For interactive visual feedback (hover, pressed states), ALWAYS use `Style.Triggers` directly on the button style. Use VS-themed brushes like `VsBrushes.CommandBarHoverKey` and `VsBrushes.CommandBarSelectedKey`. Always set `Cursor="Hand"` for affordance.

---

## Issue #10 Team Completion Summary

**Completed:** 2026-03-13  
**Implementation batch:** Lucius (backend), Selina (frontend), Renee (testing) — all work streams synchronized and passing.

**Team outcomes:**
- Lucius delivered `ResolveCommand`, thread-ID capture, and GraphQL mutation wiring
- Selina delivered comments-pane ancestry rendering, action visibility, and post-resolve refresh with filter preservation
- Renee delivered regression coverage for happy path, failure handling, refresh/filter correctness, and thread ancestry protection
- All acceptance criteria met; feature production-ready
- **Filter-safe refresh after resolve:** When a comment action changes server-backed resolution state, preserve the current author/status filters before reloading and restore them afterward. That keeps resolved/unresolved views honest without requiring fragile per-item property-change plumbing.

**Push authorized and executed:** Bruce reviewed working tree against approved design, ran `dotnet test` successfully, and pushed `squad/remove-squad-product-workflows` to origin. Remote status clean. Documented in decisions.md as "Issue #10 Branch Safe to Push."

**Non-blocking caveat:** `ResolveCommand` resolve-success + reload-failure path remains untested; follow-up should cover this defensive branch.

---

## Unresolve/Re-open Button Implementation

**Completed:** 2026-04-10  
**Requested by:** Jimmy Engström

**Summary:** Added Re-open button UI that mirrors Resolve button with opposite visibility logic. Users can now re-open previously resolved GitHub review threads from the PR comments view.

**Changes:**

1. **`PrCommentItem` (Models/PrCommentItem.cs):**
   - Added `CanUnresolve` property with `[DataMember]` for VS extensibility
   - Drives Re-open button visibility in XAML

2. **`CommentThreadBuilder` (ToolWindows/CommentThreadBuilder.cs):**
   - Updated `Build` method signature to accept `createUnresolveCommand` factory function
   - Added logic to set `CanUnresolve = true` when:
     - `UnresolveCommand` is not null
     - `threadState` is not null
     - Thread **IS** resolved (`threadState.IsResolved == true`)
     - Valid `ReviewThreadId` exists

3. **`PRReviewRemoteUserControl.xaml` (ToolWindows/):**
   - Added Re-open button after Resolve button (line 437-439)
   - Button properties:
     - `Command="{Binding UnresolveCommand}"`
     - `Visibility="{Binding CanUnresolve, Converter={StaticResource BooleanToVisibleWhenTrueConverter}}"`
     - `ToolTip="Re-open this review thread"`
     - `Text="Re-open"` (matches GitHub's UX language)
     - Uses same `FlatListButtonStyle` and sizing as Resolve button

**Visibility Logic:**
Only one button visible at a time:
- Thread unresolved → Show Resolve, hide Re-open
- Thread resolved → Hide Resolve, show Re-open
- No thread ID → Hide both

**Testing:**
- `dotnet build` — no errors
- All 23 tests pass — `dotnet test`

**Pattern Notes:**
- Mirrors existing Resolve pattern exactly
- Capability property drives visibility
- Command wiring happens in `CommentThreadBuilder` via factory function
- ViewModel passes service-layer dependencies to command constructor
- XAML binds to capability property and command
- Post-action reload preserves current filters

**Orchestration:** Completed in parallel with Lucius (backend) and Renee (tests). All 23 tests passing.

---

## Button Visual States Fix

**Completed:** 2026-04-10  
**Requested by:** Jimmy Engström

**Summary:** Fixed all buttons in the VS extension tool window to provide proper visual feedback (hover, pressed, and disabled states). Previously, buttons were completely unresponsive visually — no hover state, no pressed state, no cursor change.

**Problem:** The extension uses Microsoft.VisualStudio.Extensibility Remote UI model. Standard WPF `ControlTemplate.Triggers` do not work in this environment — you must use `Style.Triggers` directly on the style.

**Solution:**

1. **Replaced `FlatListButtonStyle` in `PRReviewRemoteUserControl.xaml`:**
   - Removed custom `ControlTemplate` that had no triggers
   - Added `Style.Triggers` with `IsMouseOver`, `IsPressed`, and `IsEnabled` triggers
   - Used VS-themed brushes: `VsBrushes.CommandBarHoverKey` for hover, `VsBrushes.CommandBarSelectedKey` for pressed
   - Added `Cursor="Hand"` for proper affordance
   - Added subtle border (`BorderThickness="1"`) to make buttons clearly identifiable

2. **Added `TextBox` style with focus indicator:**
   - Used `IsFocused` trigger to change border color to `VsBrushes.AccentBorderKey`
   - Increased border thickness on focus from 1 to 2 for clear visual feedback

**Result:**
- All buttons now show hover state (background color change)
- All buttons show pressed state (different background)
- All buttons show hand cursor on hover
- TextBox (reply input) shows accent border when focused
- All 38 tests still pass
- Build succeeded with no errors

**Pattern for VS Extensibility Remote UI buttons:**
```xaml
<Style x:Key="FlatListButtonStyle" TargetType="Button">
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{DynamicResource {x:Static styles:VsBrushes.CommandBarHoverKey}}"/>
        </Trigger>
        <Trigger Property="IsPressed" Value="True">
            <Setter Property="Background" Value="{DynamicResource {x:Static styles:VsBrushes.CommandBarSelectedKey}}"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

**Key Learning:** In VS Extensibility Remote UI, NEVER use `ControlTemplate.Triggers` — they are silently ignored. Always use `Style.Triggers` for interactive visual feedback.

---

## UI Polish (5-Fix Batch)

**Completed:** 2026-04-10  
**Requested by:** Jimmy Engström

**Summary:** Applied 5 targeted UI polish fixes to the PR review tool window to improve layout efficiency, visual consistency, and correct theming.

**Changes:**

1. **Consolidated toolbar layout:**
   - Moved "View on GitHub" button from standalone row into toolbar DockPanel next to Refresh button
   - Removed GitHub link row definition; reduced outer Grid from 3 rows to 2
   - Updated link button to use themed window text color (removed hardcoded blue `#FF0969DA`)

2. **Fixed hover/pressed colors:**
   - Changed `FlatListButtonStyle` from incorrect `CommandBarHoverKey`/`CommandBarSelectedKey` (bright blue in dark theme) to correct `ToolWindowButtonHoverActiveKey`/`ToolWindowButtonDownKey` (subtle gray)
   - These `ToolWindowButton*` brushes are the VS-native colors for interactive content inside tool windows

3. **Removed obsolete "View" button:**
   - Deleted "View" button from comment action bar (Row 3 in comment DataTemplate)
   - `ViewCommand` stays in ViewModel but is no longer exposed in UI

4. **Made filter dropdowns responsive:**
   - Replaced fixed-width DockPanel filter bar with Grid layout using star sizing
   - Author ComboBox: `Width="*"` with `MinWidth="60" MaxWidth="160"`
   - Status ComboBox: `Width="*"` with `MinWidth="60" MaxWidth="120"`
   - Dropdowns now shrink gracefully when window is narrow instead of clipping

5. **Removed default borders from tree items:**
   - Changed `FlatListButtonStyle` default `BorderBrush` from `ToolWindowBorderKey` to `Transparent`
   - Keeps 1px border slot so layout doesn't shift on hover
   - Added `BorderThickness="0"` override to comment count badge Button (which wraps its own styled Border element)
   - File/folder buttons and badges no longer show box borders at rest

**Pattern Notes:**
- The correct hover brushes for tool window content are `ToolWindowButton*`, not `CommandBar*`
- `CommandBar*` brushes are for main VS toolbar/menu chrome and render wrong in tool window contexts
- Star-sized Grid columns with MinWidth/MaxWidth are the right pattern for responsive filter bars
- For buttons that wrap styled inner elements (like the badge), override `BorderThickness="0"` directly on the Button instance

**Files Modified:** `PRReviewRemoteUserControl.xaml` (XAML-only changes)

**Testing:** Build succeeded; all structural changes verified; no C# touched.

---

## Button Hover Color Fix (ThemedDialogButtonStyleKey Base)

**Completed:** 2026-04-10  
**Requested by:** Jimmy Engström

**Summary:** Fixed persistent bright blue hover color on buttons by switching from custom Style.Triggers to VS built-in `ThemedDialogButtonStyleKey` base style.

**Problem:** Previous attempts to fix hover colors using custom `IsMouseOver` triggers with `CommandBarHoverKey` and then `ToolWindowButtonHoverActiveKey` both rendered as bright blue in VS dark theme. Root cause: custom `Style.Triggers` for background color changes fight VS's own theming system in the Remote UI model.

**Solution:**
- Replaced `FlatListButtonStyle` custom triggers with `BasedOn="{StaticResource {x:Static styles:VsResourceKeys.ThemedDialogButtonStyleKey}}"`
- `ThemedDialogButtonStyleKey` is the VS built-in button style that handles hover, pressed, and disabled states correctly in ALL themes (dark, light, blue) automatically
- Removed all custom `Background`, `BorderBrush`, `Foreground`, and `Style.Triggers` setters
- Kept only layout properties: `BorderThickness`, `Padding`, `HorizontalAlignment`, `HorizontalContentAlignment`, `VerticalContentAlignment`, `Cursor`

**Key Learning:** For VS Extensibility Remote UI buttons, `ThemedDialogButtonStyleKey` is the correct base style. Custom `IsMouseOver` triggers on `Background` do not work reliably and fight VS theming. Use the built-in themed style and only override layout/sizing properties.

**Files Modified:** `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml` (lines 40-48)

---
