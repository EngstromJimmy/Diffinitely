# Project Context

- **Owner:** Jimmy Engström
- **Project:** Diffinitely — a Visual Studio extension for reviewing GitHub pull requests and diffs
- **Stack:** C#, WPF/XAML, Visual Studio extension SDK (VSIX), GitHub API, Community Toolkit MVVM
- **Created:** 2026-03-07

## Key Files

- `Diffinitely/Services/GitHubPullRequestService.cs` — GitHub API integration
- `Diffinitely/Services/GitRepositoryService.cs` — local Git operations
- `Diffinitely/Models/` — data models
- `Diffinitely/DiffinitelyPackage.cs` — extension package entry point
- `Diffinitely/ExtensionEntrypoint.cs` — extension initialization
- `Diffinitely/Commands/OpenDiffCommand.cs` — open diff command
- `Diffinitely/Commands/OpenForReviewCommand.cs` — open for review command
- `Diffinitely/PRReviewCommand.cs` — PR review command

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
