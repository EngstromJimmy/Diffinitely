using Diffinitely.Models;
using Octokit;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace Diffinitely.Services
{
    internal class GitHubPullRequestService
    {
        private readonly GitRepositoryService _repoService = new();
        private readonly GitHubClient _client = new(new ProductHeaderValue("DiffinitelyPRHelper"));
        private static string _cachedToken = string.Empty; // in-memory cache

        private async Task<bool> EnsureCredentialsAsync(CancellationToken ct)
        {
            if (_client.Credentials != null && !string.IsNullOrEmpty(_client.Credentials.Password)) return true;
            if (!string.IsNullOrEmpty(_cachedToken))
            {
                _client.Credentials = new Credentials(_cachedToken);
                return true;
            }

            // Try load from disk cache first
            try
            {
                var cachePath = GetTokenCachePath();
                if (File.Exists(cachePath))
                {
                    var tokenOnDisk = File.ReadAllText(cachePath).Trim();
                    if (!string.IsNullOrEmpty(tokenOnDisk))
                    {
                        _cachedToken = tokenOnDisk;
                        _client.Credentials = new Credentials(_cachedToken);
                        return true;
                    }
                }
            }
            catch { }

            // Acquire via git credential fill
            var acquired = TryAcquireViaGitCredentialManager(out var token);
            if (acquired && !string.IsNullOrEmpty(token))
            {
                _cachedToken = token;
                _client.Credentials = new Credentials(token);
                // Persist (best-effort)
                try
                {
                    var path = GetTokenCachePath();
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, token);
                }
                catch { }
                return true;
            }
            return false;
        }

        private string GetTokenCachePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Diffinitely");
            return Path.Combine(dir, "github_token.cache");
        }

        private bool TryAcquireViaGitCredentialManager(out string token)
        {
            token = string.Empty;
            try
            {
                var psi = new ProcessStartInfo("git")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = "credential fill"
                };
                using var p = Process.Start(psi);
                if (p == null) return false;

                var sb = new StringBuilder();
                sb.AppendLine("protocol=https");
                sb.AppendLine("host=github.com");
                sb.AppendLine();
                p.StandardInput.Write(sb.ToString());
                p.StandardInput.Flush();
                p.StandardInput.Close();

                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);
                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("password="))
                    {
                        token = trimmed.Substring("password=".Length).Trim();
                        break;
                    }
                }
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }

        public async Task<PullRequestInfo> GetCurrentBranchPullRequestAsync(CancellationToken ct)
        {
            // Resolve repo info from local .git metadata.
            string repoRoot; try { repoRoot = _repoService.FindRepoRoot(System.Environment.CurrentDirectory); } catch { return null; }
            var branch = _repoService.GetCurrentBranch(repoRoot);
            if (string.IsNullOrEmpty(branch)) return null;
            if (!_repoService.TryGetRemoteOriginUrl(repoRoot, out var owner, out var repo)) return null;
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return null;

            // Ensure we have credentials; if not, proceed unauthenticated (rate-limited) but attempt anyway.
            await EnsureCredentialsAsync(ct);

            var request = new PullRequestRequest { State = ItemStateFilter.All, Head = owner + ":" + branch };

            var prs = await _client.PullRequest.GetAllForRepository(owner, repo, request);
            var pr = prs.Count > 0 ? prs[0] : null; if (pr == null) return null;
            var comments = await _client.PullRequest.ReviewComment.GetAll(owner, repo, pr.Number);
            var files = await _client.PullRequest.Files(owner, repo, pr.Number);
            var changed = new List<ChangedFileInfo>();

            foreach (var f in files)
            {
                var kind = ChangeKind.Modified;
                switch (f.Status) { case "added": kind = ChangeKind.Added; break; case "removed": kind = ChangeKind.Deleted; break; case "renamed": kind = ChangeKind.Renamed; break; }
                var numberOfComments = comments.Count(c => c.Path == f.FileName);
                changed.Add(new ChangedFileInfo { CommentCount = numberOfComments, FullPath = $"{repoRoot}\\{f.FileName}", Path = f.FileName, PreviousPath = f.PreviousFileName, Kind = kind });
            }
            return new PullRequestInfo { Comments = comments, Id = pr.Number.ToString(), Title = pr.Title, ChangedFiles = changed, Owner = owner, Repository = repo, BaseSha = pr.Base?.Sha, HeadSha = pr.Head?.Sha, RepoRoot = repoRoot };
        }

        public async Task<string?> GetFileContentAsync(string owner, string repo, string path, string sha, CancellationToken ct)
        {
            try
            {
                var blobRef = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, sha);
                var first = blobRef.FirstOrDefault();
                return first?.Content; // API returns Base64 decoded content
            }
            catch { return null; }
        }
    }
}
