using Diffinitely.Commands;
using Diffinitely.Models;
using Diffinitely.Services;
using Xunit;

namespace Diffinitely.Tests;

public class UnresolveCommandTests
{
    [Fact]
    public void CanExecute_IsFalse_WhenThreadIdIsMissing()
    {
        var command = new UnresolveCommand(
            new PrCommentItem(),
            _ => Task.CompletedTask,
            null,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()));

        Assert.False(command.CanExecute);
    }

    [Fact]
    public void CanExecute_IsFalse_WhenCommentIsNotResolved()
    {
        var command = new UnresolveCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", IsResolved = false },
            _ => Task.CompletedTask,
            null,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()));

        Assert.False(command.CanExecute);
    }

    [Fact]
    public void CanExecute_IsTrue_WhenCommentIsResolvedAndHasThreadId()
    {
        var command = new UnresolveCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", IsResolved = true },
            _ => Task.CompletedTask,
            null,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()));

        Assert.True(command.CanExecute);
    }

    [Fact]
    public async Task ExecuteAsync_UnresolvesAndReloads_WhenMutationSucceedsAsync()
    {
        var statusUpdates = new List<string>();
        var reloadCount = 0;
        var command = new UnresolveCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", IsResolved = true },
            _ =>
            {
                reloadCount++;
                return Task.CompletedTask;
            },
            statusUpdates.Add,
            (threadId, _) =>
            {
                Assert.Equal("thread-node-id", threadId);
                return Task.FromResult(ReviewThreadMutationResult.Success());
            });

        await InvokeExecuteAsync(command);

        Assert.Equal(1, reloadCount);
        Assert.Equal(["Unresolving review thread...", "Review thread unresolved."], statusUpdates);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotReload_WhenMutationFailsAsync()
    {
        var statusUpdates = new List<string>();
        var reloadCount = 0;
        var command = new UnresolveCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", IsResolved = true },
            _ =>
            {
                reloadCount++;
                return Task.CompletedTask;
            },
            statusUpdates.Add,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Failure("GraphQL failed.")));

        await InvokeExecuteAsync(command);

        Assert.Equal(0, reloadCount);
        Assert.Equal(["Unresolving review thread...", "GraphQL failed."], statusUpdates);
    }

    private static async Task InvokeExecuteAsync(UnresolveCommand command)
    {
        var executeAsync = typeof(UnresolveCommand).GetMethod(nameof(UnresolveCommand.ExecuteAsync));
        var task = (Task)executeAsync!.Invoke(command, new object?[] { null, null, CancellationToken.None })!;
        await task;
    }
}
