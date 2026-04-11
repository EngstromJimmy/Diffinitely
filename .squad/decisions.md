# Squad Decisions

## Active Decisions

### Decision: README Scope for Resolve/Re-open Feature

**Author:** Bruce (Lead)  
**Date:** 2026-04-10  
**Related Work:** README audit against XAML  
**Status:** Approved by Jimmy (explicit instruction)

## Summary

The Resolve and Re-open buttons are present and fully functional in `PRReviewRemoteUserControl.xaml`, but they are intentionally **not documented in README.md**.

## Rationale

Jimmy's explicit instruction: "Resolve button removed from scope — Issue #10 was closed by design: resolving review threads should be done from within GitHub, NOT from the extension. The README should NOT mention a Resolve feature."

This is a product positioning decision: even though the code exists and runs, the documentation should not surface resolve/unresolve as a supported feature. The preferred user flow is to resolve threads in GitHub's web UI.

## Implication

If the Resolve/Re-open buttons are ever removed from the XAML to match this policy, the README requires no further change. If the policy is reversed, add both "Resolve review thread" and "Re-open review thread" to the Features list.

---

### Decision: Use ThemedDialogButtonStyleKey for All Interactive Buttons

**Date:** 2026-04-10  
**Agent:** Selina (Frontend)  
**Status:** Adopted  
**Impact:** All button styles in VS extension Remote UI

## Context

The PR review tool window buttons had persistent bright blue hover colors despite multiple attempts to fix using custom `Style.Triggers` with various VS brush keys:
1. First attempt: `CommandBarHoverKey` → bright blue in dark theme
2. Second attempt: `ToolWindowButtonHoverActiveKey` → still bright blue in dark theme

Root cause: Custom `Style.Triggers` for `IsMouseOver` background changes fight VS's own theming system in the Remote UI model.

## Decision

**Base all interactive button styles on `VsResourceKeys.ThemedDialogButtonStyleKey` instead of defining custom hover/pressed triggers.**

### Pattern

```xaml
<Style x:Key="FlatListButtonStyle" TargetType="Button"
       BasedOn="{StaticResource {x:Static styles:VsResourceKeys.ThemedDialogButtonStyleKey}}">
    <!-- Only override layout/sizing properties -->
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding" Value="4,2"/>
    <Setter Property="HorizontalAlignment" Value="Left"/>
    <Setter Property="Cursor" Value="Hand"/>
</Style>
```

### What NOT to do

❌ **Don't use custom triggers for visual states:**
```xaml
<Style.Triggers>
    <Trigger Property="IsMouseOver" Value="True">
        <Setter Property="Background" Value="{DynamicResource ...}"/>
    </Trigger>
</Style.Triggers>
```

❌ **Don't manually set color properties:**
```xaml
<Setter Property="Background" Value="..."/>
<Setter Property="BorderBrush" Value="..."/>
<Setter Property="Foreground" Value="..."/>
```

### Why This Works

`ThemedDialogButtonStyleKey` provides:
- Correct hover background for dark/light/blue VS themes
- Correct pressed state
- Correct disabled state  
- Correct foreground color
- Correct border behavior
- All visual states work correctly in Remote UI without custom triggers

## Consequences

### Positive
- Buttons automatically match VS theme in all color schemes
- No fighting between custom triggers and VS theming system
- Less code to maintain (no custom visual state logic)
- Consistent with VS platform UX guidelines

### Negative
- Less control over exact hover/pressed colors (must accept VS defaults)
- Requires understanding which VS base styles work in Remote UI vs. standard WPF

## Implementation

Applied to `FlatListButtonStyle` in `PRReviewRemoteUserControl.xaml` (lines 40-48).

## Related

- Previous decision: "Button Visual States Fix" (used custom triggers, incorrect approach)
- Previous decision: "UI Polish 5-Fix Batch" (attempted `ToolWindowButton*` brushes, also incorrect)
- VS Extensibility documentation: Remote UI theming limitations

---

### Decision: Comment Item Layout Restructure

**Date:** 2026-04-11  
**Author:** Selina (Frontend Dev)  
**Requested by:** Jimmy Engström

## Context

The comment list in the PR Review tool window (`PRReviewRemoteUserControl.xaml`) showed each comment in a verbose 5-row layout:
- Row 0: file path + line + outdated badge
- Row 1: 16×16 avatar + author name + timestamp (using `<Run>` bindings)
- Row 2: body text + thread replies
- Row 3: action buttons (Resolve, Re-open, Jump to Diff)
- Row 4: reply box

Two problems:
1. The author name (`<Run Text="{Binding Author}"/>`) displayed "Microsoft Visual Studio Platform UI Remote UI ..." garbage because `<Run>` bindings in VS Remote UI can resolve to the DataContext object's `.ToString()` instead of the bound property string.
2. The layout was too verbose; the desired UI is a compact card showing just avatar + filename + author.

## Decision

**Replace the 5-row grid with a compact 2-column grid:**

| Column | Width | Content |
|--------|-------|---------|
| 0 | `Auto` | Avatar `Image`, 40×40, `VerticalAlignment="Center"` |
| 1 | `*` | `StackPanel` with two `TextBlock` rows: `FilePath` (bold, top) and `Author` (FontSize 11, bottom) |

**Fix author binding:** Use `<TextBlock Text="{Binding Author}"/>` — never `<Run Text="{Binding Author}"/>` in VS Remote UI.

## Binding Paths

- Avatar: `{Binding AuthorAvatarUrl}` → `PrCommentItem.AuthorAvatarUrl` (string)
- File name: `{Binding FilePath}` → `PrCommentItem.FilePath` (string)
- Author name: `{Binding Author}` → `PrCommentItem.Author` (string)

## Constraints

- VS Extensibility Remote UI: no `ControlTemplate.Triggers`; `Style.Triggers` only.
- Root element is `<DataTemplate>`, not a `<UserControl>`.
- Only the `ListView.ItemTemplate` DataTemplate was changed.

## Status

Implemented and verified (XML valid).

---

### Decision: Status Bar Placement and Styling

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-11  
**Related Work:** Status bar + rounded tab corners UI polish  
**Status:** Implemented

## Summary

Status text (e.g., "Reply sent", "Resolved", "Unresolved") has been moved from the toolbar to a dedicated bottom status bar that auto-hides when empty. This creates a cleaner separation of concerns and matches Visual Studio's tool window status bar pattern.

## What Changed

### Before:
- Status TextBlock lived in the toolbar DockPanel at Row 0, next to ProgressBar and LoadingText
- Status text appeared inline with toolbar buttons, making the toolbar cluttered
- Status text visibility was controlled by `IsLoading` converter (opposite of loading indicator)

### After:
- Status bar is a separate Border element at Grid.Row="2" (bottom of the tool window)
- Uses VS ToolWindowBackground/WindowText brushes with a top border separator
- Auto-hides completely when Status string is empty (via `StringEmptyToCollapsedConverter`)
- Toolbar ProgressBar + LoadingText remain at top (loading feedback belongs in toolbar)

## Design Rationale

**Why separate status from toolbar:**
- Status messages are persistent state, not actions — they belong at the bottom like VS's built-in status bars
- Toolbar should contain actions (Refresh, View on GitHub), not status feedback
- Bottom placement makes status visible across all tabs without taking vertical space from content

**Why auto-hide:**
- Status bar should only appear when there's something to communicate
- Empty space at bottom looks unfinished; collapsing the row keeps the UI tight
- `StringEmptyToCollapsedConverter` makes this automatic — no extra ViewModel logic needed

**Why ToolWindowBackground/WindowText brushes:**
- `VsBrushes.ToolWindowBackgroundKey` and `VsBrushes.WindowTextKey` are VS's canonical tool window status bar colors
- These brushes automatically adapt to light/dark/blue themes
- Top border separator (`BorderThickness="0,1,0,0"`) visually separates status from content
- Note: `InfoBackgroundKey/InfoTextKey` are for colored info/warning bars, not status bars

## Implementation Notes

- Grid outer layout: Row 0 (toolbar, Auto), Row 1 (TabControl, *), Row 2 (status bar, Auto)
- Status bar padding: 4,2 (horizontal padding 4 for compact alignment, vertical padding 2 for compact height)
- FontSize: 11 (slightly smaller than body text, matches VS status bars)
- Status text source: `PRReviewViewModel.Status` property (already in use, no model changes needed)

## Bonus Change: Rounded Tab Corners

Added `CornerRadius="3,3,0,0"` to TabItem Border template. This gives tabs rounded top corners to match VS's native tab style. Because `CornerRadius` is a static property (not a trigger), it works perfectly in VS Remote UI with no compatibility concerns.

## Governance

This pattern is now standard for persistent status feedback in the PR review tool window:
- Use the bottom status bar for state messages
- Use toolbar ProgressBar for active loading
- Status bar should always auto-hide when empty

## Files Modified

- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml`

## Testing

- Build: 0 errors (24 pre-existing warnings)
- Tests: all 38 passing
- Visual verification: Status bar appears at bottom when Status is set, collapses when empty

---

### Decision: Sticky Footer with Progress Bar

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-14  
**Status:** Complete

## Summary

The loading indicator and status text are now unified in a permanent sticky footer at the bottom of the tool window (Row 2). The toolbar (Row 0) no longer contains any loading UI.

## Before

- Row 0 toolbar had an inline `<StackPanel>` with a `ProgressBar` (Height="14") and a `LoadingText` TextBlock, both bound to `IsLoading` via `BooleanToVisibleWhenTrueConverter`
- Row 2 status bar was a `<Border>` with `Visibility="{Binding Status, Converter={StaticResource StringEmptyToCollapsedConverter}}"` — it collapsed when Status was empty, causing visible layout jumps each time loading started/finished

## After

- Row 0 toolbar: `<StackPanel>` removed entirely
- Row 2 footer: always visible (sticky), containing a `DockPanel`:
  - `ProgressBar` docked left: `Width="120"`, `Height="4"` (thin), `IsIndeterminate="True"`, visible only when `IsLoading` is true
  - `TextBlock` fills remaining space: bound to `Status`, `FontSize="11"`, `VerticalAlignment="Center"`
  - `Border` wrapping the footer has no `Visibility` binding — it is always present with consistent height
  - Preserved: `BorderThickness="0,1,0,0"` top separator, `Padding="4,2"`

## Why

Layout stability: when the status border collapsed/expanded on every load cycle, the content area would shift height. A fixed-height sticky footer eliminates this entirely. The thin progress bar (`Height="4"`) also matches VS's own indeterminate progress bar aesthetic — the previous `Height="14"` in the toolbar looked out of place.

## Files Changed

- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml`

## Testing

- Build: 0 errors
- All 39 tests pass

---

### Decision: VS Remote UI StringFormat Constraint (DateTimeOffset Bindings)

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-11  
**Status:** Documented pattern for all future bindings  

## Problem

In `PRReviewRemoteUserControl.xaml`, comment author/date headers displayed as:
```
AuthorName — Microsoft Visual Studio Platform UI ...
```

Root cause: VS Remote UI does NOT support `StringFormat` on `Run.Text` bindings for non-primitive types. When binding `DateTimeOffset` with `StringFormat={}{0:yyyy-MM-dd HH:mm}`, the binding falls through to the proxy object's `.ToString()` which returns the full .NET Remote UI type name.

## The Pattern

**NEVER use StringFormat in XAML for non-primitive types in VS Remote UI.**

Instead, expose a pre-formatted computed property on the model:

```csharp
[DataContract]
public class PrCommentItem
{
    [DataMember]
    public DateTimeOffset CreatedAt { get; set; }
    
    // DO NOT add [DataMember] - this is a derived property
    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}
```

Then bind directly:
```xml
<Run Text="{Binding FormattedCreatedAt}" />
```

## Why This Works

- Computed properties return plain strings, not Remote UI proxy objects
- Format conversion happens in C# memory before crossing the Remote UI boundary
- No `[DataMember]` needed — these are derived, not serialized
- Works with all non-primitive types (DateTimeOffset, TimeSpan, custom value objects, etc.)

## Files Changed

- `Diffinitely/Models/PRCommentItem.cs` — Added `FormattedCreatedAt` to `PrCommentItem` and `PrCommentReply`
- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml` — Replaced two `StringFormat` bindings with direct property bindings

## Testing

- Build: succeeded (38 tests passing)
- Visual verification: Date displays correctly in comment list

## Governance

This is the standard pattern for ALL future non-primitive bindings in VS Extensibility Remote UI XAML. StringFormat is only safe for primitive types (int, double, etc.). For DateTimeOffset, DateTime, TimeSpan, or custom types, always expose a pre-formatted string property.

---

### Decision: Use Separate `TreeItemButtonStyle` for Buttons Inside Tree Rows

**Date:** 2026-04-10  
**Author:** Selina (Frontend Dev)  
**Requested by:** Jimmy Engström

## Context

After `FlatListButtonStyle` was updated to use `BasedOn="{StaticResource {x:Static styles:VsResourceKeys.ThemedDialogButtonStyleKey}}"`, buttons inside the diff file tree started rendering with button chrome (hover border, pressed background, etc.). This conflicted with the `TreeViewItem` row highlight that the VS-themed tree provides.

## Decision

Introduce a second style, `TreeItemButtonStyle`, for all `Button` elements that live inside `TreeView` item templates (i.e., inside `HierarchicalDataTemplate` or any tree-row `DataTemplate`).

### `TreeItemButtonStyle` properties

- `Background="Transparent"` — no fill at rest
- `BorderBrush="Transparent"` — no border at rest
- `BorderThickness="0"` — no border space
- `Padding="2,1"` — compact
- `Cursor="Hand"` — affordance
- `Foreground` bound to `VsBrushes.WindowTextKey`
- Custom `ControlTemplate` with plain `Border` + `ContentPresenter` — **no base style inheritance**
- **No hover background** — the `TreeViewItem` row highlight handles hover feedback for the whole row

### What keeps `FlatListButtonStyle`

- Toolbar buttons (Refresh, View on GitHub)
- Comment action buttons (Resolve, Re-open, Jump to Diff, Reply) in the `ListView.ItemTemplate`

## Rationale

`ThemedDialogButtonStyleKey` is correct for standalone action buttons. It adds button chrome (border on hover, background change) which looks right in toolbars and comment action bars. But inside a `TreeViewItem`, the entire row already has hover highlight from VS theming. Layering button chrome on top of tree-row highlight makes tree rows look broken — files and folders appear as individual button widgets rather than selectable rows. The two concerns must be separated: one style for standalone action buttons, one for tree-internal interactive elements.

## Files Affected

- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml`
  - Added `TreeItemButtonStyle` resource
  - Changed two `Button` elements in `TreeView.ItemTemplate` from `FlatListButtonStyle` to `TreeItemButtonStyle`

---

### Decision: UI Polish — Correct Button Theming and Responsive Layout

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-10  
**Status:** Complete  
**Related Work:** 5-fix UI polish batch

## Summary

Applied 5 targeted UI improvements to the PR review tool window. Key architectural decisions: use `ToolWindowButton*` brushes (not `CommandBar*`) for tool window content, prefer Grid with star sizing for responsive filter bars, and make button borders transparent at rest to reduce visual noise in tree views.

## Decisions

### 1. Correct VS Brush Choices for Tool Window Buttons

**Problem:** `FlatListButtonStyle` used `CommandBarHoverKey` and `CommandBarSelectedKey` brushes, which render as bright blue in VS dark theme. These brushes are designed for main VS toolbar/menu chrome, not tool window content.

**Decision:** Use `ToolWindowButton*` brushes instead:
- **Hover:** `ToolWindowButtonHoverActiveKey` + `ToolWindowButtonHoverActiveBorderKey`
- **Pressed:** `ToolWindowButtonDownKey` + `ToolWindowButtonDownBorderKey`

**Rationale:** `ToolWindowButton*` brushes provide the correct subtle gray highlights for interactive content inside tool windows. They match VS native tool window affordance patterns.

**Governance:** All button styles in tool window XAML should use `ToolWindowButton*` brushes for hover/pressed states, not `CommandBar*` brushes.

---

### 2. Transparent Default Borders for Tree Item Buttons

**Problem:** Every button in the file tree (file names, folder names, comment count badges) showed a visible box border at rest due to `BorderBrush=ToolWindowBorderKey` and `BorderThickness="1"` in `FlatListButtonStyle`. This created excessive visual clutter.

**Decision:** 
- Change default `BorderBrush` to `Transparent` in `FlatListButtonStyle`
- Keep `BorderThickness="1"` to prevent layout shift on hover
- For buttons wrapping styled inner elements (like comment badge with its own `Border`), add `BorderThickness="0"` override directly on the Button instance

**Rationale:** Borders-at-rest are appropriate for toolbar buttons, but in tree views they create noise. Transparent default with visible hover border provides cleaner resting state while preserving interactive feedback.

---

### 3. Responsive Filter Bars via Grid with Star Sizing

**Problem:** Comments tab filter bar used fixed-width ComboBoxes (`Width="160"` and `Width="120"`). When window narrowed, dropdowns clipped and became unusable.

**Decision:** Replace DockPanel with Grid layout using star-sized columns:
```xaml
<ColumnDefinition Width="*" MinWidth="60" MaxWidth="160"/>  <!-- Author -->
<ColumnDefinition Width="*" MinWidth="60" MaxWidth="120"/>  <!-- Status -->
```

**Rationale:** Star sizing with MinWidth/MaxWidth constraints allows dropdowns to shrink gracefully in narrow windows while capping growth in wide windows. This is the standard WPF pattern for responsive horizontal layouts.

**Governance:** Use Grid with star-sized columns (+ MinWidth/MaxWidth) for any horizontal filter bars or control groups that need responsive behavior. Avoid fixed widths on interactive controls.

---

### 4. Consolidated Toolbar Layout

**Problem:** "View on GitHub" link occupied its own Grid row, wasting vertical space and separating it from other global actions.

**Decision:** 
- Remove GitHub link's standalone row definition
- Move "View on GitHub" button into toolbar DockPanel next to Refresh button
- Update outer Grid from 3 rows to 2 (toolbar + main content)

**Rationale:** Related actions should be grouped together. GitHub link is a global PR action, not header-level metadata. Consolidating rows saves vertical space in a typically narrow tool window.

---

### 5. Removed Obsolete "View" Button from Comment Action Bar

**Problem:** Comment action bar (Row 3 in DataTemplate) had a "View" button (docked right) with no clear function. The ViewCommand exists in ViewModel but its purpose is unclear.

**Decision:** Remove "View" button from XAML. Leave ViewCommand in ViewModel code for now (removal is a backend/ViewModel concern).

**Rationale:** UI should not expose affordances without clear user value. If ViewCommand is later deemed necessary, it can be re-introduced with proper naming and tooltip.

---

## Files Modified

- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml`

## Testing

- Build succeeded with no errors
- All 5 changes are XAML-only (no C# modified)
- Visual structure verified via XAML review

## Governance

- **ToolWindowButton brushes are mandatory** for hover/pressed states in tool window content
- **Responsive filter bars use Grid + star columns** with MinWidth/MaxWidth
- **Transparent default borders in tree buttons** reduce visual clutter while preserving hover affordance

---

### Decision: Verified VsBrushes and VsResourceKeys for VS Remote UI

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-11  
**Status:** Reference Document

## Summary

This document lists VsBrushes and VsResourceKeys that have been verified to exist at runtime in `Microsoft.VisualStudio.Shell.15.0` for VS Extensibility Remote UI. These keys are safe to use in XAML with `{x:Static styles:VsBrushes.*}` or `{x:Static styles:VsResourceKeys.*}`.

## Problem Context

Some VS SDK keys compile cleanly but throw `System.Windows.Markup.XamlParseException` at runtime with error: "StaticExtension value cannot be resolved to an enumeration, static field, or static property." This happens because the key does not actually exist as a static member in the assembly loaded by the Remote UI host.

## Keys Verified to EXIST (Safe to Use)

### VsBrushes (all confirmed 2026-04-11)

- `VsBrushes.AccentBorderKey` — Blue accent border (used for focus states)
- `VsBrushes.GrayTextKey` — Disabled/inactive text color
- `VsBrushes.InfoBackgroundKey` — Info message background (yellow-ish in light themes)
- `VsBrushes.InfoTextKey` — Info message text color
- `VsBrushes.ToolWindowBackgroundKey` — Standard tool window background
- `VsBrushes.ToolWindowBorderKey` — Standard tool window border
- `VsBrushes.WindowKey` — Window content background (lighter than ToolWindowBackground)
- `VsBrushes.WindowTextKey` — Primary text color

### VsResourceKeys (all confirmed 2026-04-11)

- `VsResourceKeys.ThemedDialogButtonStyleKey` — VS-themed button style
- `VsResourceKeys.ThemedDialogComboBoxStyleKey` — VS-themed combo box style
- `VsResourceKeys.ThemedDialogTreeViewItemStyleKey` — VS-themed tree view item style
- `VsResourceKeys.ThemedDialogTreeViewStyleKey` — VS-themed tree view style

## Keys Verified to NOT EXIST (Never Use)

### VsBrushes

- `VsBrushes.ToolWindowContentBackgroundKey` ❌ — Compiles but throws at runtime
- `VsBrushes.CommandBarMouseOverBackgroundBeginKey` ❌ — Compiles but throws at runtime

### VsResourceKeys

- `VsResourceKeys.ThemedDialogTabItemStyleKey` ❌ — Compiles but throws at runtime

## Validation Method

Keys are validated by:
1. Parsing `PRReviewRemoteUserControl.xaml` as XML
2. Extracting all `{x:Static styles:VsBrushes.*}` and `{x:Static styles:VsResourceKeys.*}` references using regex
3. Using reflection on `Microsoft.VisualStudio.Shell.15.0.dll` to verify each referenced member exists
4. This is automated in the test: `Diffinitely.Tests/XamlStaticReferenceTests.cs`

## Usage Recommendations

**For tab styling:**
- Selected tab background: Use `VsBrushes.WindowKey` (lighter than ToolWindowBackgroundKey)
- Inactive tab background: Use `VsBrushes.ToolWindowBackgroundKey`
- Hover state: Use `VsBrushes.WindowKey` (same as selected for consistency)
- Borders: Use `VsBrushes.ToolWindowBorderKey`

**For text colors:**
- Active text: `VsBrushes.WindowTextKey`
- Disabled text: `VsBrushes.GrayTextKey`

**For focus/accent:**
- Focus border: `VsBrushes.AccentBorderKey` (blue in most themes)

## Governance

Before using any new VsBrushes or VsResourceKeys key:
1. Add it to the XAML
2. Run `dotnet test` to ensure `XamlStaticReferenceTests` passes
3. If the test fails, the key does not exist — find an alternative
4. Update this document with newly verified keys

## Related Files

- Test: `Diffinitely.Tests/XamlStaticReferenceTests.cs`
- XAML: `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml`
- History: `.squad/agents/selina/history.md`

---

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

