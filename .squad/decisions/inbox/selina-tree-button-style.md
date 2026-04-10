# Decision: Use Separate `TreeItemButtonStyle` for Buttons Inside Tree Rows

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
