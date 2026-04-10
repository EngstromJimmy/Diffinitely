using Octokit;

namespace Diffinitely.Models
{
    internal class PullRequestInfo
    {

        public string Id { get; set; }
        public string Title { get; set; }
        public string HtmlUrl { get; set; }
        public IReadOnlyList<ChangedFileInfo> ChangedFiles { get; set; }
        public string Owner { get; set; }
        public string Repository { get; set; }
        public string BaseSha { get; set; }
        public string HeadSha { get; set; }
        public string RepoRoot { get; set; } // local repository root path
        public IReadOnlyList<PullRequestReviewComment> Comments { get; set; } = [];
        public IReadOnlyDictionary<long, PullRequestReviewThread> ReviewThreads { get; set; } = new Dictionary<long, PullRequestReviewThread>();
    }
}
