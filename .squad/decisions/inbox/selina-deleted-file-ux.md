# Decision: Deleted File UX (Issue #5)

**Author:** Selina (Frontend Dev)  
**Date:** 2026-03-07  
**Related Issue:** #5 — Deleted files should show strikethrough and open their pre-deletion content

## Summary

Two changes to the PR file tree for files with `ChangeKind == Deleted`:

1. File names render with `TextDecorations="Strikethrough"` in the TreeView.
2. Clicking a deleted file opens a VS diff view showing the pre-deletion content (left) vs. an empty temp file (right), instead of failing silently.

## Details

### IsDeleted on TreeNode

A new `bool IsDeleted` property was added to `TreeNode` (with `[DataMember]` and full `INotifyPropertyChanged` notification). It is set to `true` in `PathTreeBuilder.AddPath` when `fileInfo.Kind == ChangeKind.Deleted`. The existing `ChangeKind` string property is kept for other potential consumers.

### XAML Strikethrough

A local `Style` with a `DataTrigger` was added to the `TextBlock` rendering the file name inside the `HierarchicalDataTemplate`:

```xml
<TextBlock.Style>
    <Style TargetType="TextBlock">
        <Setter Property="Foreground" Value="{DynamicResource ...WindowTextKey}"/>
        <Style.Triggers>
            <DataTrigger Binding="{Binding IsDeleted}" Value="True">
                <Setter Property="TextDecorations" Value="Strikethrough"/>
            </DataTrigger>
        </Style.Triggers>
    </Style>
</TextBlock.Style>
```

The local style re-applies the VS-themed `Foreground` setter so the global `TextBlock` style (which also sets `Foreground`) is not lost when the local style takes over.

### OpenDiffCommand — deleted file handling

`OpenDiffCommand.ExecuteAsync` now branches on `fileInfo.Kind == ChangeKind.Deleted`. For deleted files it:
- Writes the base content to a left temp file (same as before).
- Writes an empty string to a **right** temp file.
- Passes `VSDIFFOPT_RightFileIsTemporary` in addition to `VSDIFFOPT_LeftFileIsTemporary`.
- Uses `fileInfo.Path + " (deleted)"` as the caption so users can see context.

Non-deleted files follow the existing path unchanged.

## Governance note

`TreeNode` properties should always carry both `[DataMember]` and `INotifyPropertyChanged` to work correctly with the VS extensibility XAML remote-UI engine.
