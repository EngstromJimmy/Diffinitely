using Diffinitely.Commands;
using Diffinitely.Models;
using Diffinitely.Services;
using Xunit;

namespace Diffinitely.Tests;

/// <summary>
/// Tests for mutual exclusivity between Resolve and Unresolve commands.
/// Ensures that when IsResolved=true, only Unresolve is available, and vice versa.
/// Also verifies that both commands are suppressed when ReviewThreadId is missing.
/// </summary>
public class CommentActionAvailabilityTests
{
    [Fact]
    public void ResolveAvailable_UnresolveNotAvailable_WhenCommentIsUnresolved()
    {
        var item = new PrCommentItem
        {
            ReviewThreadId = "thread-123",
            IsResolved = false
        };

        var resolveCommand = CreateResolveCommand(item);
        var unresolveCommand = CreateUnresolveCommand(item);

        Assert.True(resolveCommand.CanExecute, "Resolve should be available when comment is unresolved");
        Assert.False(unresolveCommand.CanExecute, "Unresolve should NOT be available when comment is unresolved");
    }

    [Fact]
    public void UnresolveAvailable_ResolveNotAvailable_WhenCommentIsResolved()
    {
        var item = new PrCommentItem
        {
            ReviewThreadId = "thread-123",
            IsResolved = true
        };

        var resolveCommand = CreateResolveCommand(item);
        var unresolveCommand = CreateUnresolveCommand(item);

        Assert.False(resolveCommand.CanExecute, "Resolve should NOT be available when comment is resolved");
        Assert.True(unresolveCommand.CanExecute, "Unresolve should be available when comment is resolved");
    }

    [Fact]
    public void BothCommandsUnavailable_WhenThreadIdIsNull()
    {
        var item = new PrCommentItem
        {
            ReviewThreadId = null,
            IsResolved = false
        };

        var resolveCommand = CreateResolveCommand(item);
        var unresolveCommand = CreateUnresolveCommand(item);

        Assert.False(resolveCommand.CanExecute, "Resolve should NOT be available when ReviewThreadId is null");
        Assert.False(unresolveCommand.CanExecute, "Unresolve should NOT be available when ReviewThreadId is null");
    }

    [Fact]
    public void BothCommandsUnavailable_WhenThreadIdIsEmpty()
    {
        var item = new PrCommentItem
        {
            ReviewThreadId = "",
            IsResolved = true
        };

        var resolveCommand = CreateResolveCommand(item);
        var unresolveCommand = CreateUnresolveCommand(item);

        Assert.False(resolveCommand.CanExecute, "Resolve should NOT be available when ReviewThreadId is empty");
        Assert.False(unresolveCommand.CanExecute, "Unresolve should NOT be available when ReviewThreadId is empty");
    }

    [Fact]
    public void BothCommandsUnavailable_WhenThreadIdIsWhitespace()
    {
        var item = new PrCommentItem
        {
            ReviewThreadId = "   ",
            IsResolved = false
        };

        var resolveCommand = CreateResolveCommand(item);
        var unresolveCommand = CreateUnresolveCommand(item);

        Assert.False(resolveCommand.CanExecute, "Resolve should NOT be available when ReviewThreadId is whitespace");
        Assert.False(unresolveCommand.CanExecute, "Unresolve should NOT be available when ReviewThreadId is whitespace");
    }

    private static ResolveCommand CreateResolveCommand(PrCommentItem item)
    {
        return new ResolveCommand(
            item,
            _ => Task.CompletedTask,
            null,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()));
    }

    private static UnresolveCommand CreateUnresolveCommand(PrCommentItem item)
    {
        return new UnresolveCommand(
            item,
            _ => Task.CompletedTask,
            null,
            (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()));
    }
}
