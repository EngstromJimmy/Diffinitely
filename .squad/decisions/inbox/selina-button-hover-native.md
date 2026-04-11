# Decision: Use ThemedDialogButtonStyleKey for All Interactive Buttons

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
