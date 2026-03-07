# Project Context

- **Owner:** Jimmy Engström
- **Project:** Diffinitely — a Visual Studio extension for reviewing GitHub pull requests and diffs
- **Stack:** C#, WPF/XAML, Visual Studio extension SDK (VSIX), GitHub API, Community Toolkit MVVM
- **Created:** 2026-03-07

## Key Files

- `Diffinitely/DiffinitelyPackage.cs` — extension entry point
- `Diffinitely/Commands/` — VS commands (OpenDiffCommand, OpenForReviewCommand)
- `Diffinitely/ToolWindows/` — PR review tool window (WPF XAML + ViewModel)
- `Diffinitely/Services/GitHubPullRequestService.cs` — GitHub API integration
- `Diffinitely/Services/GitRepositoryService.cs` — local git operations
- `Diffinitely/Models/` — data models

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-07 — Release workflow added

- **`.slnx` format**: `nuget restore` does not support the `.slnx` format. Use `dotnet restore <project.csproj>` directly — SDK-style projects support this even for net472 VSSDK targets.
- **VSIX build flags**: Always pass `/p:DeployExtension=false /p:SkipOpenVisualStudio=true` to `msbuild` when building VSIX in CI to prevent it from trying to install or launch Visual Studio.
- **Runner requirement**: VSIX builds (and net472 test runs) require `windows-latest`. Linux/macOS runners will not work.
- **Release naming convention**: Using `v1.0.${{ github.run_number }}` for sequential versioning. Can migrate to semver tags later.
- **Output artifact path**: VSIX lands at `Diffinitely/bin/Release/net472/Diffinitely.vsix`.
