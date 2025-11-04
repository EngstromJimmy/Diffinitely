using Diffinitely.Models;
using Octokit;

namespace Diffinitely.Services
{
    internal class GitHubPullRequestService
    {
        private readonly GitRepositoryService _repoService = new();
        private readonly GitHubClient _client = new(new ProductHeaderValue("DiffinitelyPRHelper"));

        public async Task<PullRequestInfo> GetCurrentBranchPullRequestAsync(CancellationToken ct)
        {
            // Resolve repo info from local .git metadata.
            string repoRoot; try { repoRoot = _repoService.FindRepoRoot(System.Environment.CurrentDirectory); } catch { return null; }
            var branch = _repoService.GetCurrentBranch(repoRoot);
            if (string.IsNullOrEmpty(branch)) return null;
            if (!_repoService.TryGetRemoteOriginUrl(repoRoot, out var owner, out var repo)) return null;

            // If remote URL parsing failed earlier, try to extract owner/repo again directly from origin URL string.
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return null;

            var request = new PullRequestRequest { State = ItemStateFilter.All, Head = owner + ":" + branch };
            var tokenAuth = new Credentials("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"); // This can be a PAT or an OAuth token.
            _client.Credentials = tokenAuth;

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
