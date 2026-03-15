using Diffinitely.Commands;
using Diffinitely.Models;
using Diffinitely.Services;
using Xunit;

namespace Diffinitely.Tests;

public class ResolveCommandTests
{
    [Fact]
    public void CanExecute_IsFalse_WhenThreadIdIsMissing()
    {
        var command = new ResolveCommand(
            new PrCommentItem(),
            _ => Task.CompletedTask,
            null,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()));

        Assert.False(command.CanExecute);
    }

    [Fact]
    public void CanExecute_IsFalse_WhenCommentIsAlreadyResolved()
    {
        var command = new ResolveCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id", IsResolved = true },
            _ => Task.CompletedTask,
            null,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()));

        Assert.False(command.CanExecute);
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesAndReloads_WhenMutationSucceedsAsync()
    {
        var statusUpdates = new List<string>();
        var reloadCount = 0;
        var command = new ResolveCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id" },
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
        Assert.Equal(["Resolving review thread...", "Review thread resolved."], statusUpdates);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotReload_WhenMutationFailsAsync()
    {
        var statusUpdates = new List<string>();
        var reloadCount = 0;
        var command = new ResolveCommand(
            new PrCommentItem { ReviewThreadId = "thread-node-id" },
            _ =>
            {
                reloadCount++;
                return Task.CompletedTask;
            },
            statusUpdates.Add,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Failure("GraphQL failed.")));

        await InvokeExecuteAsync(command);

        Assert.Equal(0, reloadCount);
        Assert.Equal(["Resolving review thread...", "GraphQL failed."], statusUpdates);
    }

    private static async Task InvokeExecuteAsync(ResolveCommand command)
    {
        var executeAsync = typeof(ResolveCommand).GetMethod(nameof(ResolveCommand.ExecuteAsync));
        var task = (Task)executeAsync!.Invoke(command, new object?[] { null, null, CancellationToken.None })!;
        await task;
    }
}
