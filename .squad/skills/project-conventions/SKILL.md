---
name: "project-conventions"
description: "Core conventions and patterns for this codebase"
domain: "project-conventions"
confidence: "medium"
source: "template"
---

## Context

> **This is a starter template.** Replace the placeholder patterns below with your actual project conventions. Skills train agents on codebase-specific practices — accurate documentation here improves agent output quality.

## Patterns

### GitHub thread actions need thread-level identity

For comment-thread actions such as resolve/unresolve, carry the GitHub review thread node ID from the service layer into the UI model explicitly. Do not try to reconstruct action targets from file path, line number, or comment ID alone; GitHub mutations operate on thread identity, and UI grouping can otherwise point the action at the wrong thread.

### Refresh from server after action-based state changes

When a UI action changes server-backed state that also drives filtering or visibility, prefer reloading the affected data from the service after success unless the model already has reliable property-change wiring. In this codebase, that is the safer default for comment resolution because the comments pane filters on resolved/unresolved state and must reflect GitHub truth immediately.

### Error Handling

<!-- Example: How does your project handle errors? -->
<!-- - Use try/catch with specific error types? -->
<!-- - Log to a specific service? -->
<!-- - Return error objects vs throwing? -->

### Testing

- Test framework: xUnit in `Diffinitely.Tests/`, currently targeting `net472` to match the VSIX project.
- Run targeted tests with `dotnet test Diffinitely.Tests\Diffinitely.Tests.csproj --nologo --no-restore`.
- For view-model-driven UI actions, test both presentation and wiring: if a button is visible for a state, the backing command must be non-null and the resulting state transition must be asserted.
- For GitHub-backed comment actions, assert that the model carries every identifier required by the mutation before the action is shown. Review-thread actions require thread-level IDs, not just comment IDs.

### Code Style

<!-- Example: Linting, formatting, naming conventions -->
<!-- - Linter: ESLint config? -->
<!-- - Formatter: Prettier? -->
<!-- - Naming: camelCase, snake_case, etc.? -->

### File Structure

<!-- Example: How is the project organized? -->
<!-- - src/ — Source code -->
<!-- - test/ — Tests -->
<!-- - docs/ — Documentation -->

## Examples

```
// Add code examples that demonstrate your conventions
```

## Anti-Patterns

<!-- List things to avoid in this codebase -->
- **[Anti-pattern]** — Explanation of what not to do and why.
- **Visible no-op actions** — Do not expose a button in XAML unless the view model reliably supplies a command or intentionally disables/hides it. Test the unhappy path too, especially around GitHub-backed actions such as comment resolution.
