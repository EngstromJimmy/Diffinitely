# Decision: UI Polish — Correct Button Theming and Responsive Layout

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
