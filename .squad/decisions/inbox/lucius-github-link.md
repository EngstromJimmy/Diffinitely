# View on GitHub Link Implementation

**Author:** Lucius (Backend Dev)  
**Date:** 2025-01-24  
**Status:** Complete

## Summary

Added a clickable "View on GitHub" link at the top of the PR review tool window that opens the current pull request on GitHub.com in the user's default browser.

## What Changed

1. **Data model:** `PullRequestInfo.HtmlUrl` now captures the GitHub web URL from Octokit `PullRequest.HtmlUrl`
2. **Command:** `OpenInBrowserCommand` — minimal IAsyncCommand that opens URL with `Process.Start` + `UseShellExecute = true`
3. **ViewModel:** `PRReviewViewModel.PrHtmlUrl` property + `OpenInBrowserCommand` dynamically created when PR loaded
4. **UI:** New Row 0 in XAML with button styled as hyperlink (underline, blue color, emoji 🔗)
5. **Converter:** `StringEmptyToCollapsedConverter` hides link when no PR is loaded

## Why This Approach

- **Button vs Hyperlink:** VS extensibility Remote UI does not reliably support WPF `Hyperlink` navigation handlers. A button styled to look like a link is the recommended pattern.
- **Command pattern:** Follows existing command architecture (`ResolveCommand`, `UnresolveCommand`, etc.)
- **Dynamic command creation:** Command only exists when URL is non-empty; prevents dead clicks
- **Best-effort opening:** Browser launch failures are silent — opening browser is convenience, not critical path

## Risks Covered

- **No PR loaded:** Link hidden via visibility binding
- **Empty URL:** CanExecute gate prevents execution
- **Browser launch failure:** Caught silently; not critical to extension functionality

## Files Modified

- `Diffinitely/Models/PullRequestInfo.cs` (added HtmlUrl property)
- `Diffinitely/Services/GitHubPullRequestService.cs` (populate HtmlUrl from Octokit)
- `Diffinitely/Commands/OpenInBrowserCommand.cs` (new file)
- `Diffinitely/ToolWindows/PRReviewViewModel.cs` (PrHtmlUrl + command wiring)
- `Diffinitely/ToolWindows/StringEmptyToCollapsedConverter.cs` (new file)
- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml` (UI link at top)

## Testing

- Build: ✅ succeeded with no errors
- Tests: ✅ all 38 tests passed
- Manual validation: Not performed (extension UI requires Visual Studio host)

## Notes

Link appears at the very top of the tool window (Row 0), above the Refresh toolbar. Text is styled as hyperlink (blue, underlined) with emoji for visual clarity: "🔗 View on GitHub".
