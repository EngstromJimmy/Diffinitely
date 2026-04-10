namespace Diffinitely.Models;

internal sealed class PullRequestReviewThread
{
    public string Id { get; set; } = "";

    public bool IsResolved { get; set; }

    public bool IsOutdated { get; set; }
}
