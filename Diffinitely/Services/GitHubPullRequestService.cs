using Diffinitely.Models;
using Octokit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Diffinitely.Services
{
    internal class GitHubPullRequestService
    {
        private readonly GitRepositoryService _repoService = new GitRepositoryService();
        private GitHubClient _client = new GitHubClient(new ProductHeaderValue("DiffinitelyPRHelper"));

        public bool HasAuth => _client.Credentials != null && !string.IsNullOrEmpty(_client.Credentials.Password);

        public void SetToken(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
                _client.Credentials = new Credentials(token);
        }

        public async Task<PullRequestInfo> GetCurrentBranchPullRequestAsync(CancellationToken ct)
        {
            var cwd = System.Environment.CurrentDirectory;
            string repoRoot;
            try { repoRoot = _repoService.FindRepoRoot(cwd); } catch { return null; }
            var branch = _repoService.GetCurrentBranch(repoRoot);
            if (string.IsNullOrEmpty(branch)) return null;
            if (!_repoService.TryGetRemoteOriginUrl(repoRoot, out var owner, out var repo)) return null;
            var request = new PullRequestRequest { State = ItemStateFilter.Open, Head = owner + ":" + branch };
            var prs = await _client.PullRequest.GetAllForRepository(owner, repo, request);
            var pr = prs.Count >0 ? prs[0] : null;
            if (pr == null) return null;
            var files = await _client.PullRequest.Files(owner, repo, pr.Number);
            var changed = new List<ChangedFileInfo>();
            foreach (var f in files)
            {
                var kind = ChangeKind.Modified;
                switch (f.Status)
                {
                    case "added": kind = ChangeKind.Added; break;
                    case "removed": kind = ChangeKind.Deleted; break;
                    case "renamed": kind = ChangeKind.Renamed; break;
                }
                changed.Add(new ChangedFileInfo { Path = f.FileName, PreviousPath = f.PreviousFileName, Kind = kind });
            }
            return new PullRequestInfo { Id = pr.Number.ToString(), Title = pr.Title, ChangedFiles = changed };
        }
    }
}
