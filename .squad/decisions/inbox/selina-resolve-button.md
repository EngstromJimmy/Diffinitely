# Decision: Comments UI should not expose Resolve until it can execute

**Author:** Selina (Frontend)  
**Date:** 2026-03-12  
**Related Issue:** #10 — Resolve button in comments view appears non-functional

## Summary

The comments tab currently shows a `Resolve` button for unresolved threads, but the bound `ResolveCommand` is never assigned in `PRReviewViewModel`. From the UI side, that is a false affordance: the button reads like a supported action while the view model provides no executable behavior.

## Frontend evidence

- `PRReviewRemoteUserControl.xaml` binds the button to `ResolveCommand`.
- `PrCommentItem` exposes `ResolveCommand` as nullable.
- `PRReviewViewModel` sets `ViewCommand` but leaves `ResolveCommand` commented out.
- Visibility tracks `IsResolved`, but enabled/capability state does not track command availability.

## Decision

Until resolve is implemented end-to-end, the UI should stop presenting it as an actionable control. When the feature is implemented, the button should only appear in states where the command can actually run and should give immediate visible feedback after success.
