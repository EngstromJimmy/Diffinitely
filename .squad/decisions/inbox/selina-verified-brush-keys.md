# Verified VsBrushes and VsResourceKeys for VS Remote UI

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
