using Diffinitely.Models;
using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.Commands;

internal sealed class UnresolveCommand : IAsyncCommand
{
    private readonly PrCommentItem _commentItem;
    private readonly Func<CancellationToken, Task> _reloadAsync;
    private readonly Action<string>? _setStatus;
    private readonly Func<string, CancellationToken, Task<ReviewThreadMutationResult>> _unresolveAsync;

    public UnresolveCommand(
        GitHubPullRequestService pullRequestService,
        PrCommentItem commentItem,
        Func<CancellationToken, Task> reloadAsync,
        Action<string>? setStatus = null)
        : this(commentItem, reloadAsync, setStatus, pullRequestService.UnresolveReviewThreadAsync)
    {
    }

    internal UnresolveCommand(
        PrCommentItem commentItem,
        Func<CancellationToken, Task> reloadAsync,
        Action<string>? setStatus,
        Func<string, CancellationToken, Task<ReviewThreadMutationResult>> unresolveAsync)
    {
        _commentItem = commentItem;
        _reloadAsync = reloadAsync;
        _setStatus = setStatus;
        _unresolveAsync = unresolveAsync;
    }

    public bool CanExecute => _commentItem.IsResolved && !string.IsNullOrWhiteSpace(_commentItem.ReviewThreadId);

    public async Task ExecuteAsync(object? parameter, IClientContext clientContext, CancellationToken cancellationToken)
    {
        if (!CanExecute)
        {
            return;
        }

        _setStatus?.Invoke("Unresolving review thread...");

        var result = await _unresolveAsync(_commentItem.ReviewThreadId, cancellationToken);
        if (!result.Succeeded)
        {
            _setStatus?.Invoke(result.ErrorMessage ?? "Unresolving the review thread failed.");
            return;
        }

        try
        {
            await _reloadAsync(cancellationToken);
            _setStatus?.Invoke("Review thread unresolved.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _setStatus?.Invoke($"Review thread unresolved, but refresh failed: {ex.Message}");
        }
    }
}
