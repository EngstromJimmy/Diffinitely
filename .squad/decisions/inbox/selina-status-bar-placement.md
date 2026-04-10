# Decision: Status Bar Placement and Styling

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
