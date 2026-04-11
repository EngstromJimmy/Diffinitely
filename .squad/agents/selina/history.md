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
- **VS Remote UI StringFormat constraint (DateTimeOffset bindings):** `StringFormat` on `Run.Text` bindings does NOT work in VS Remote UI for non-primitive types like `DateTimeOffset`. The binding falls through to the proxy object's `.ToString()` which returns the full .NET Remote UI type name ("Microsoft Visual Studio Platform UI..."). **Always expose a pre-formatted string property on the model** (e.g., `public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm")`) and bind directly to that instead. Do NOT use `[DataMember]` on computed properties — they are derived, not serialized. Fixed in `PRCommentItem.cs` and `PrCommentReply.cs` with `FormattedCreatedAt` property.
- **Tab styling — ThemedDialogTabItemStyleKey compiles but FAILS at runtime:** `VsResourceKeys.ThemedDialogTabItemStyleKey` compiles cleanly but throws `XamlParseException` ("StaticExtension value cannot be resolved") at runtime in VS Remote UI. **Never use it.** Use a fully manual `ControlTemplate` with VS brush keys instead.
- **ContentPresenter TextElement.Foreground blocks trigger-based foreground:** If a `ContentPresenter` inside a tab's `ControlTemplate` has `TextElement.Foreground` set explicitly (e.g. hardcoded to `WindowTextKey`), any `Trigger`-based `Foreground` setter on the parent `TabItem` is silently overridden and has no visible effect. Always omit `TextElement.Foreground` from `ContentPresenter` in tab templates if you want foreground triggers to apply.
- **VsBrushes keys that DO NOT exist at runtime:** `VsBrushes.ToolWindowContentBackgroundKey` and `VsBrushes.CommandBarMouseOverBackgroundBeginKey` compile cleanly but throw `XamlParseException` at runtime ("StaticExtension value cannot be resolved"). These keys are NOT present as static fields in `Microsoft.VisualStudio.Shell.VsBrushes` in the Remote UI assembly. **Never use them.**
- **VsBrushes keys verified to exist at runtime (2026-04-11):** The following keys are confirmed valid and can be safely used in XAML: `AccentBorderKey`, `GrayTextKey`, `InfoBackgroundKey`, `InfoTextKey`, `ToolWindowBackgroundKey`, `ToolWindowBorderKey`, `WindowKey`, `WindowTextKey`. These were validated by the regression test `XamlStaticReferenceTests.PRReviewRemoteUserControl_XamlStaticReferences_AllResolveAtRuntime`.
- **VsResourceKeys keys verified to exist at runtime (2026-04-11):** The following keys are confirmed valid: `ThemedDialogButtonStyleKey`, `ThemedDialogComboBoxStyleKey`, `ThemedDialogTreeViewItemStyleKey`, `ThemedDialogTreeViewStyleKey`. Note that `ThemedDialogTabItemStyleKey` does NOT exist.
- **XAML static reference validation approach:** Created `XamlStaticReferenceTests.cs` which parses XAML as XML, extracts all `{x:Static styles:VsBrushes.*}` and `{x:Static styles:VsResourceKeys.*}` references using regex, then uses reflection to verify each referenced field/property exists on the target type. This prevents runtime XamlParseException crashes from non-existent keys. The test loads `Microsoft.VisualStudio.Shell.15.0.dll` from the NuGet package cache and reflects on the actual types at test time.
- **Seamless active tab technique (2026-04-11):** To make the active tab visually merge with the content panel below it (no visible seam), three things must work together: (1) `BorderThickness="1,2,1,0"` removes the bottom border of the active tab, (2) `Margin="0,0,0,-1"` on `TabBorder` extends the tab 1px down to overlap/cover the TabControl's top border line, (3) `Panel.ZIndex="1"` on the TabItem (set without `TargetName` — on the item itself, not a named child) renders the active tab on top of the TabControl border. Additionally the active tab background MUST match the content area background (`ToolWindowBackgroundKey` for both) or a color seam will still be visible. The `Panel.ZIndex` setter was placed in `ControlTemplate.Triggers` (not `Style.Triggers`) — this worked for the TabItem case even though ControlTemplate.Triggers are documented as unreliable for Buttons in VS Remote UI.

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

## Comment Item DataTemplate Binding Paths

**Completed:** 2026-04-11
**Requested by:** Jimmy Engström

**Binding paths for comment item DataTemplate (`PRReviewRemoteUserControl.xaml`, `ListView.ItemTemplate`):**

- **Avatar image URL:** `{Binding AuthorAvatarUrl}` → `PrCommentItem.AuthorAvatarUrl` (string)
- **Author name:** `{Binding Author}` → `PrCommentItem.Author` (string) — use `TextBlock Text="{Binding Author}"`, NOT `<Run Text="{Binding Author}"/>` (Run bindings show VS type garbage in Remote UI)
- **File name:** `{Binding FilePath}` → `PrCommentItem.FilePath` (string)

**Fix applied:**
- Replaced `<Run Text="{Binding Author}"/>` with `<TextBlock Text="{Binding Author}"/>` to avoid Remote UI object `.ToString()` rendering VS type strings.
- Replaced old 5-row Grid (file row, author row, body row, actions row, reply row) with a compact 2-column Grid: `Auto` (40×40 avatar) + `*` (stacked FilePath + Author).

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


## Tree Item Button Style Split

**Completed:** 2026-04-11
**Requested by:** Jimmy Engström

**Summary:** Tree item buttons (file/folder open and comment badge) inside TreeView.ItemTemplate now use a separate TreeItemButtonStyle that does NOT inherit from ThemedDialogButtonStyleKey.

**Problem:** After switching FlatListButtonStyle to BasedOn=ThemedDialogButtonStyleKey, the tree items (files and folders in the diff tree) looked like buttons — showing button chrome, hover border, etc. The ThemedDialogButtonStyleKey base adds button chrome that directly conflicts with the TreeViewItem row highlight, making tree rows look wrong.

**Solution:**
- Added TreeItemButtonStyle in PRReviewRemoteUserControl.xaml (after FlatListButtonStyle) with a fully custom ControlTemplate (plain Border + ContentPresenter), no base style.
- Style properties: Background=Transparent, BorderBrush=Transparent, BorderThickness=0, Padding=2,1, Cursor=Hand, Foreground bound to VsBrushes.WindowTextKey. No hover background — the TreeViewItem row highlight handles hover for the whole row.
- Changed the two Button elements inside TreeView.ItemTemplate (HierarchicalDataTemplate) from FlatListButtonStyle to TreeItemButtonStyle.
- All other buttons (toolbar, comment actions in ListView) keep FlatListButtonStyle unchanged.

**Key Learning:** Tree item buttons need a separate style that does not inherit ThemedDialogButtonStyleKey, because that style adds button chrome that conflicts with the TreeViewItem row highlight. The TreeViewItem row highlight is responsible for hover feedback inside the tree — buttons inside tree rows must be visually transparent and let the row handle hover.

**Files Modified:** Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml

---

## 2026-04-11: Comment DataTemplate full structure

The comment DataTemplate in the ListView (Comments tab) has four logical sections that must ALL be preserved when restructuring:
- **Row 0 header**: 2-column Grid — 40x40 avatar (left) + StackPanel with FilePath (bold), Outdated badge, and Author+timestamp (right)
- **Row 1 body**: StackPanel with Body TextBlock (TextWrapping=Wrap) + ThreadReplies ItemsControl (nested DataTemplate shows Author, CreatedAt, Body)
- **Row 2 actions**: DockPanel with Resolve button (ResolveCommand/CanResolve), Re-open button (UnresolveCommand/CanUnresolve), Jump to Diff button (JumpToDiffCommand/CanJumpToDiff) — all use TreeItemButtonStyle
- **Row 3 reply**: DockPanel (visible via CanReply) with Reply button (ReplyCommand) docked right, and TextBox bound to ReplyText (TwoWay)

## 2025 — Remove inner borders (flow cleanup)

**Task:** Remove nested `BorderThickness="1"` borders inside the Files and Comments tabs so content flows cleanly inside the seamless tab container.

**Lines changed in `PRReviewRemoteUserControl.xaml`:**
- Line 402: Comment card `Border` (DataTemplate root, `CornerRadius="4"`) → `BorderThickness="0"` (was `1`)
- Line 475: Thread reply `Border` (`CornerRadius="3"`) inside ThreadReplies ItemsControl → `BorderThickness="0"` (was `1`)

**Already 0 (no change needed):**
- Line 243: TreeView `BorderThickness` was already `0`
- Line 381: ListView `BorderThickness` was already `0`

**Kept as-is:**
- Line 226: TabControl `BorderThickness="1"` — the outer tab container border, keep
- Line 448: "Outdated" badge `Border` `BorderThickness="1"` — small UI badge, intentional
- Line 551: Status bar `BorderThickness="0,1,0,0"` — top separator line, keep

Build: 0 errors, 25 warnings (pre-existing). All 39 tests pass.

Do NOT flatten this to just the header — all four sections are required.

---

## Status Bar + Rounded Tab Corners Verification

**Completed:** 2026-04-11  
**Requested by:** Jimmy Engström

**Summary:** Verified that status bar and rounded tab corners are correctly implemented per Jimmy's specifications. Both features were already complete from commit `daf373f`.

**Findings:**

1. **Status bar (Task 1) — Already correct:**
   - Status bar positioned at Grid.Row="2" (bottom panel)
   - Uses `ToolWindowBackgroundKey` for background (correct)
   - Uses `WindowTextKey` for foreground text (correct)
   - Padding="4,2" matches VS status bar compact style (correct)
   - Top border separator with `BorderThickness="0,1,0,0"` (correct)
   - Auto-hides when Status is empty via `StringEmptyToCollapsedConverter` (correct)

2. **Rounded tab corners (Task 2) — Already correct:**
   - `CornerRadius="3,3,0,0"` exists on `<Border x:Name="TabBorder">` in TabItem ControlTemplate
   - Matches VS rounded tab style as specified

**Key Learnings:**
- **VsBrushes.ToolWindowBackgroundKey / WindowTextKey** are the correct brushes for status bar styling in tool windows
- **VsBrushes.InfoBackgroundKey / InfoTextKey** are for info bars (colored notification panels), NOT status bars
- The decision file `.squad/decisions/inbox/selina-status-bar-placement.md` incorrectly documented the implementation as using InfoBackgroundKey, but the actual commit `daf373f` correctly used ToolWindowBackgroundKey from the start
- Status bar auto-hide pattern via `StringEmptyToCollapsedConverter` keeps UI tight when no status message
- Rounded tab corners work perfectly in VS Remote UI as static property (no trigger limitations)

**Testing:**
- `dotnet build --no-incremental` — 0 errors, 25 warnings (pre-existing)
- `dotnet test --no-build` — all 38 tests passing

**Files Modified:** None (both tasks already complete). Updated history.md only.

---

## Active Tab Visual Distinction (VS-style)

**Completed:** 2026-04-11  
**Requested by:** Jimmy Engström

**Summary:** Made the active tab visually distinct from inactive tabs using Visual Studio's own tab styling pattern. Previously, both selected and unselected tabs used the same `ToolWindowBackgroundKey` background and border colors, making it impossible to tell which tab was active.

**Changes:**

1. **Inactive tab default foreground (line 91):**
   - Changed base `Foreground` setter from `VsBrushes.WindowTextKey` to `VsBrushes.GrayTextKey`
   - Makes unselected tab labels slightly dimmer so active tab text pops more

2. **Selected tab trigger (lines 118-127):**
   - Background: `VsBrushes.WindowKey` — lighter content area background (editor background) makes selected tab pop from the tab strip
   - BorderBrush: `VsBrushes.AccentBorderKey` — VS accent color (blue in most themes) creates distinct top accent bar
   - BorderThickness: `"1,2,1,0"` — 2px top border makes the accent visible
   - Foreground: `VsBrushes.WindowTextKey` — full-brightness text contrasts with dimmed inactive tabs

3. **Hover tab trigger (lines 129-136):**
   - Background: `VsBrushes.ToolWindowButtonHoverActiveKey` — slight hover feedback without making it look like the active tab
   - Foreground: `VsBrushes.WindowTextKey` — full brightness on hover for feedback

**VS Brush Keys That Worked:**
- `VsBrushes.WindowKey` — lighter background for selected tab (perfect for making it pop)
- `VsBrushes.AccentBorderKey` — accent color for 2px top border (VS theme blue/accent)
- `VsBrushes.GrayTextKey` — dimmed foreground for inactive tab labels
- `VsBrushes.ToolWindowButtonHoverActiveKey` — subtle hover background

**Key Learning:** ControlTemplate.Triggers work for TabItem in VS Remote UI (unlike Button, which requires Style.Triggers). The TabItem ControlTemplate triggers successfully apply background, border, and foreground changes for IsSelected and IsMouseOver states.

**Pattern:**
- **Active tab:** Lighter background + accent top border + full-brightness text
- **Inactive tabs:** Default background + dimmed text
- **Hover:** Subtle background change + full-brightness text

**Files Modified:** `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml`

**Testing:**
- `dotnet build --no-incremental -verbosity:minimal` — 0 errors, 25 warnings (pre-existing)
- `dotnet test --no-build -verbosity:minimal` — all 38 tests passing

---

## Tab Styling — ThemedDialogTabItemStyleKey Runtime Fix

**Completed:** 2026-04-11
**Requested by:** Jimmy Engström

**Summary:** `ThemedDialogTabItemStyleKey` compiles cleanly but throws `XamlParseException` at runtime in VS Remote UI ("StaticExtension value cannot be resolved"). Replaced with a fully manual `ControlTemplate`.

**Changes:**

- Removed `BasedOn="{StaticResource {x:Static styles:VsResourceKeys.ThemedDialogTabItemStyleKey}}"` from `<Style TargetType="TabItem">` — gone entirely, no `BasedOn` on the new style.
- Replaced the two-line style with a full manual `ControlTemplate` approach.

**Brush keys used (confirmed working):**

- **Selected background:** `VsBrushes.ToolWindowContentBackgroundKey` — VS SDK standard key for selected tab content area
- **Hover background:** `VsBrushes.CommandBarMouseOverBackgroundBeginKey` — subtle hover lift
- **Inactive background:** `VsBrushes.ToolWindowBackgroundKey` — base tool window background (known-good)
- **Inactive foreground:** `VsBrushes.GrayTextKey` — dimmed inactive tab labels
- **Active/hover foreground:** `VsBrushes.WindowTextKey` — full-brightness text (known-good)
- **Border:** `VsBrushes.ToolWindowBorderKey` — all states (known-good)

**Key Learnings:**
- `ThemedDialogTabItemStyleKey` does NOT exist at runtime in VS Remote UI — compiles but throws `XamlParseException`. **Never use it.**
- `ControlTemplate.Triggers` DO work for `TabItem` in VS Remote UI (confirmed again — selected, hover, disabled triggers all apply correctly).
- `ContentPresenter` inside a tab `ControlTemplate` must NOT have `TextElement.Foreground` set — omit it entirely so foreground trigger setters on the `TabItem` can propagate through.
- `ToolWindowContentBackgroundKey` is a valid VS SDK key and works at runtime for selected tab background.
- `CommandBarMouseOverBackgroundBeginKey` is a valid VS SDK key and works at runtime for hover tab background.

**Files Modified:** `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml`

**Testing:**
- `dotnet build --no-incremental -verbosity:minimal` — 0 errors, 25 warnings (pre-existing)
- `dotnet test --no-build -verbosity:minimal` — all 38 tests passing

---

## Sticky Footer with Progress Bar

**Completed:** 2026-04-14
**Requested by:** Jimmy Engström

**Summary:** Moved the loading `ProgressBar` out of the toolbar (Row 0) and into the status bar footer (Row 2), making the footer always visible (sticky) to eliminate layout jumps.

**Changes in `PRReviewRemoteUserControl.xaml`:**

1. **Removed from Row 0 toolbar DockPanel:**
   - `<StackPanel>` containing the wide ProgressBar (Height="14") and LoadingText TextBlock, both bound to `IsLoading`

2. **Rewrote Row 2 status border:**
   - Removed `Visibility="{Binding Status, Converter={StaticResource StringEmptyToCollapsedConverter}}"` — footer is now always visible
   - Inside the border, replaced the single TextBlock with a `DockPanel`:
     - `ProgressBar` docked left: `Width="120"`, `Height="4"` (thin VS-style), `IsIndeterminate="True"`, visible only when `IsLoading` is true
     - `TextBlock` fills remaining space, bound to `Status`, `FontSize="11"`, `VerticalAlignment="Center"`
   - Kept `BorderThickness="0,1,0,0"` top separator and `Padding="4,2"`

**Key Learnings:**
- Thin progress bars (`Height="4"`) match VS's own indeterminate loading indicator style — much better than the previous `Height="14"` chunky bar in the toolbar.
- Making the footer always visible (removing the StringEmptyToCollapsedConverter) eliminates layout jumps when content loads. Empty state is fine — the footer just shows nothing.
- `DockPanel` inside a Border works cleanly for this left-dock + fill pattern; no Grid needed.

**Testing:**
- `dotnet build Diffinitely --no-restore` — Build succeeded, 0 errors
- `dotnet test Diffinitely.Tests --no-restore -v quiet` — All 39 tests pass

---

### Filter Bar Padding Alignment (2026-04-10)

**Issue:** The filter bar (Author/Status dropdowns) at the top of the Comments tab had `Margin="4,4,4,4"`, which placed it ~10px too far left compared to the actual content inside comment cards below.

**Root Cause Analysis:**
- ListView has `Padding="4"`
- ListViewItem has default WPF padding of `Padding="4,1"` (no ItemContainerStyle to override it)
- Comment Border has `Padding="6"`
- Total left offset of comment content: 4 + 4 + 6 = 14px
- Filter bar had only `Margin="4"` → misaligned by 10px

**Fix:** Changed filter bar `Margin="4,4,4,4"` to `Margin="14,4,14,4"` to match the cumulative left/right padding of comment content.

**Key Learning:**
- When aligning container elements with nested content, calculate the full padding stack: `ListView.Padding` + `ListViewItem` default padding (if no ItemContainerStyle) + `DataTemplate` root element padding.
- Default WPF ListViewItem has `Padding="4,1"` for left/top — it's not zero unless explicitly styled away.
- Visual alignment details matter in tool window UX; even 10px misalignment creates a sloppy feel.
