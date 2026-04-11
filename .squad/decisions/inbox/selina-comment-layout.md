# Decision: Comment Item Layout Restructure

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
