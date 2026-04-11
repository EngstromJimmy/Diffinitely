using Diffinitely.Commands;
using Diffinitely.Models;
using Diffinitely.Services;
using Xunit;

namespace Diffinitely.Tests;

public class ReplyCommandTests
{
    [Fact]
    public void CanExecute_IsFalse_WhenThreadIdIsMissing()
    {
        var command = new ReplyCommand(
            new PrCommentItem(),
            _ => Task.CompletedTask,
            null,
            (_, _, _) => Task.FromResult(ReviewThreadMutationResult.Success()));

        Assert.False(command.CanExecute);
    }

    [Fact]
    public void CanExecute_IsTrue_WhenThreadIdIsPresent()
    {
        var command = new ReplyCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id" },
            _ => Task.CompletedTask,
            null,
            (_, _, _) => Task.FromResult(ReviewThreadMutationResult.Success()));

        Assert.True(command.CanExecute);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotSend_WhenReplyTextIsEmpty()
    {
        var statusUpdates = new List<string>();
        var sendCount = 0;
        var command = new ReplyCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", ReplyText = "" },
            _ => Task.CompletedTask,
            statusUpdates.Add,
            (_, _, _) =>
            {
                sendCount++;
                return Task.FromResult(ReviewThreadMutationResult.Success());
            });

        await InvokeExecuteAsync(command);

        Assert.Equal(0, sendCount);
        Assert.Equal(["Reply text cannot be empty."], statusUpdates);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotSend_WhenReplyTextIsWhitespace()
    {
        var statusUpdates = new List<string>();
        var sendCount = 0;
        var command = new ReplyCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", ReplyText = "   " },
            _ => Task.CompletedTask,
            statusUpdates.Add,
            (_, _, _) =>
            {
                sendCount++;
                return Task.FromResult(ReviewThreadMutationResult.Success());
            });

        await InvokeExecuteAsync(command);

        Assert.Equal(0, sendCount);
        Assert.Equal(["Reply text cannot be empty."], statusUpdates);
    }

    [Fact]
    public async Task ExecuteAsync_SendsAndReloads_WhenSuccessAsync()
    {
        var item = new PrCommentItem { ReviewThreadId = "thread-node-id", ReplyText = "LGTM!" };
        var statusUpdates = new List<string>();
        var reloadCount = 0;
        var receivedThreadId = "";
        var receivedBody = "";

        var command = new ReplyCommand(
            item,
            _ =>
            {
                reloadCount++;
                return Task.CompletedTask;
            },
            statusUpdates.Add,
            (threadId, body, _) =>
            {
                receivedThreadId = threadId;
                receivedBody = body;
                return Task.FromResult(ReviewThreadMutationResult.Success());
            });

        await InvokeExecuteAsync(command);

        Assert.Equal(1, reloadCount);
        Assert.Equal("thread-node-id", receivedThreadId);
        Assert.Equal("LGTM!", receivedBody);
        Assert.Equal("", item.ReplyText);
        Assert.Equal(["Sending reply...", "Reply sent."], statusUpdates);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotReload_WhenMutationFailsAsync()
    {
        var statusUpdates = new List<string>();
        var reloadCount = 0;
        var command = new ReplyCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", ReplyText = "LGTM!" },
            _ =>
            {
                reloadCount++;
                return Task.CompletedTask;
            },
            statusUpdates.Add,
            (_, _, _) => Task.FromResult(ReviewThreadMutationResult.Failure("Network error.")));

        await InvokeExecuteAsync(command);

        Assert.Equal(0, reloadCount);
        Assert.Equal(["Sending reply...", "Network error."], statusUpdates);
    }

    private static async Task InvokeExecuteAsync(ReplyCommand command)
    {
        var executeAsync = typeof(ReplyCommand).GetMethod(nameof(ReplyCommand.ExecuteAsync));
        var task = (Task)executeAsync!.Invoke(command, new object?[] { null, null, CancellationToken.None })!;
        await task;
    }
}
