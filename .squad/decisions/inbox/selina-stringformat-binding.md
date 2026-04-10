# VS Remote UI StringFormat Constraint (DateTimeOffset Bindings)

**Author:** Selina (Frontend Dev)  
**Date:** 2026-04-11  
**Status:** Documented pattern for all future bindings  

## Problem

In `PRReviewRemoteUserControl.xaml`, comment author/date headers displayed as:
```
AuthorName — Microsoft Visual Studio Platform UI ...
```

Root cause: VS Remote UI does NOT support `StringFormat` on `Run.Text` bindings for non-primitive types. When binding `DateTimeOffset` with `StringFormat={}{0:yyyy-MM-dd HH:mm}`, the binding falls through to the proxy object's `.ToString()` which returns the full .NET Remote UI type name.

## The Pattern

**NEVER use StringFormat in XAML for non-primitive types in VS Remote UI.**

Instead, expose a pre-formatted computed property on the model:

```csharp
[DataContract]
public class PrCommentItem
{
    [DataMember]
    public DateTimeOffset CreatedAt { get; set; }
    
    // DO NOT add [DataMember] - this is a derived property
    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm");
}
```

Then bind directly:
```xml
<Run Text="{Binding FormattedCreatedAt}" />
```

## Why This Works

- Computed properties return plain strings, not Remote UI proxy objects
- Format conversion happens in C# memory before crossing the Remote UI boundary
- No `[DataMember]` needed — these are derived, not serialized
- Works with all non-primitive types (DateTimeOffset, TimeSpan, custom value objects, etc.)

## Files Changed

- `Diffinitely/Models/PRCommentItem.cs` — Added `FormattedCreatedAt` to `PrCommentItem` and `PrCommentReply`
- `Diffinitely/ToolWindows/PRReviewRemoteUserControl.xaml` — Replaced two `StringFormat` bindings with direct property bindings

## Testing

- Build: succeeded (38 tests passing)
- Visual verification: Date displays correctly in comment list

## Governance

This is the standard pattern for ALL future non-primitive bindings in VS Extensibility Remote UI XAML. StringFormat is only safe for primitive types (int, double, etc.). For DateTimeOffset, DateTime, TimeSpan, or custom types, always expose a pre-formatted string property.
