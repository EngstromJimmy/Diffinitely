# Decision: VS Extensibility Remote UI Button Visual Feedback Pattern

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-10  
**Related Work:** Button visual states fix  
**Status:** Approved pattern for all future button work

## Summary

In `Microsoft.VisualStudio.Extensibility` Remote UI, button visual states (hover, pressed) MUST be implemented using `Style.Triggers`, not `ControlTemplate.Triggers`. The latter are silently ignored by the remote UI rendering engine.

## The Problem

Buttons in the tool window had no visual feedback:
- No hover state (background color didn't change on mouse-over)
- No pressed state (no visual indication when clicked)
- No cursor change (didn't show hand cursor)
- Poor affordance — users couldn't tell if elements were clickable

The previous `FlatListButtonStyle` used a custom `ControlTemplate` with a Border named "Bg" but had no triggers inside it. This is the standard WPF pattern, but it doesn't work in VS Extensibility Remote UI.

## The Solution

Replace `ControlTemplate`-based styles with `Style.Triggers` directly:

```xaml
<Style x:Key="FlatListButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent"/>
    <Setter Property="BorderBrush" Value="{DynamicResource {x:Static styles:VsBrushes.ToolWindowBorderKey}}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Foreground" Value="{DynamicResource {x:Static styles:VsBrushes.WindowTextKey}}"/>
    
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{DynamicResource {x:Static styles:VsBrushes.CommandBarHoverKey}}"/>
            <Setter Property="BorderBrush" Value="{DynamicResource {x:Static styles:VsBrushes.CommandBarBorderKey}}"/>
        </Trigger>
        <Trigger Property="IsPressed" Value="True">
            <Setter Property="Background" Value="{DynamicResource {x:Static styles:VsBrushes.CommandBarSelectedKey}}"/>
            <Setter Property="BorderBrush" Value="{DynamicResource {x:Static styles:VsBrushes.CommandBarBorderKey}}"/>
        </Trigger>
        <Trigger Property="IsEnabled" Value="False">
            <Setter Property="Foreground" Value="{DynamicResource {x:Static styles:VsBrushes.GrayTextKey}}"/>
            <Setter Property="Opacity" Value="0.5"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

## Why This Works

- **`Style.Triggers`** are evaluated by the Remote UI host and propagate to the rendered WPF controls
- **`ControlTemplate.Triggers`** are NOT evaluated — they exist in the XAML but have no runtime effect
- VS-themed brushes (`VsBrushes.*`) dynamically adapt to light/dark themes
- `Cursor="Hand"` provides immediate affordance

## Recommended VS Brushes for Buttons

| State | Background Brush | Border Brush |
|-------|------------------|--------------|
| Default | `Transparent` or `VsBrushes.ToolWindowBackgroundKey` | `VsBrushes.ToolWindowBorderKey` |
| Hover | `VsBrushes.CommandBarHoverKey` | `VsBrushes.CommandBarBorderKey` |
| Pressed | `VsBrushes.CommandBarSelectedKey` | `VsBrushes.CommandBarBorderKey` |
| Disabled | (keep default) | (keep default) |

For foreground:
- Default: `VsBrushes.WindowTextKey`
- Disabled: `VsBrushes.GrayTextKey`

## Files Changed

- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml` — Updated `FlatListButtonStyle`, added `TextBox` focus style

## Testing

- All 38 tests pass
- Build succeeded with no errors
- Visual inspection confirms hover, pressed, and disabled states now render correctly

## Governance

This pattern is now the standard for all button styles in VS Extensibility Remote UI XAML files. If you need custom button visuals, fork `FlatListButtonStyle` and modify the trigger setters — never use `ControlTemplate.Triggers`.
