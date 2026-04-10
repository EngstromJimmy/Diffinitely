using Diffinitely.Models;
using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.Commands;

internal sealed class ReplyCommand : IAsyncCommand
{
    private readonly PrCommentItem _commentItem;
    private readonly Func<CancellationToken, Task> _reloadAsync;
    private readonly Action<string>? _setStatus;
    private readonly Func<string, string, CancellationToken, Task<ReviewThreadMutationResult>> _replyAsync;

    public ReplyCommand(
        GitHubPullRequestService pullRequestService,
        PrCommentItem commentItem,
        Func<CancellationToken, Task> reloadAsync,
        Action<string>? setStatus = null)
        : this(commentItem, reloadAsync, setStatus, pullRequestService.AddReviewThreadReplyAsync)
    {
    }

    internal ReplyCommand(
        PrCommentItem commentItem,
        Func<CancellationToken, Task> reloadAsync,
        Action<string>? setStatus,
        Func<string, string, CancellationToken, Task<ReviewThreadMutationResult>> replyAsync)
    {
        _commentItem = commentItem;
        _reloadAsync = reloadAsync;
        _setStatus = setStatus;
        _replyAsync = replyAsync;
    }

    public bool CanExecute => !string.IsNullOrWhiteSpace(_commentItem.ReviewThreadId);

    public async Task ExecuteAsync(object? parameter, IClientContext clientContext, CancellationToken cancellationToken)
    {
        if (!CanExecute)
            return;

        var body = _commentItem.ReplyText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            _setStatus?.Invoke("Reply text cannot be empty.");
            return;
        }

        _setStatus?.Invoke("Sending reply...");

        var result = await _replyAsync(_commentItem.ReviewThreadId, body, cancellationToken);
        if (!result.Succeeded)
        {
            _setStatus?.Invoke(result.ErrorMessage ?? "Sending the reply failed.");
            return;
        }

        _commentItem.ReplyText = "";

        try
        {
            await _reloadAsync(cancellationToken);
            _setStatus?.Invoke("Reply sent.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _setStatus?.Invoke($"Reply sent, but refresh failed: {ex.Message}");
        }
    }
}
