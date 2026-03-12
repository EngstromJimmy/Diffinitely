---
last_updated: 2026-03-07T22:12:47.723Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->
**Pattern:** In item-template action bars, only show or enable buttons when the bound command is actually available. **Context:** WPF/MVVM lists where per-row commands are nullable or depend on backend capability; avoid false affordances like visible no-op buttons.
