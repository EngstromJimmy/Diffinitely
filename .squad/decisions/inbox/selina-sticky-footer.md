# Decision: Sticky Footer with Progress Bar

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
