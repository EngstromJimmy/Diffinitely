using Diffinitely.Commands;
using Diffinitely.Models;
using Diffinitely.Services;
using Diffinitely.ToolWindows;
using Microsoft.VisualStudio.Extensibility.UI;
using Xunit;

namespace Diffinitely.Tests;

/// <summary>
/// Tests for Tier 1 features: Outdated badges, Reply availability, and Jump to Diff availability.
/// </summary>
public class Tier1FeatureTests
{
    // ─── Outdated badge ───────────────────────────────────────────────────────

    [Fact]
    public void IsOutdated_IsFalse_WhenThreadStateHasNoOutdatedFlag()
    {
        var items = Build(
            threadStates: new Dictionary<long, CommentThreadBuilder.ReviewThreadState>
            {
                [1] = new CommentThreadBuilder.ReviewThreadState("thread-1", false)
            });

        Assert.False(items[0].IsOutdated);
    }

    [Fact]
    public void IsOutdated_IsTrue_WhenThreadStateIsOutdated()
    {
        var items = Build(
            threadStates: new Dictionary<long, CommentThreadBuilder.ReviewThreadState>
            {
                [1] = new CommentThreadBuilder.ReviewThreadState("thread-1", false, isOutdated: true)
            });

        Assert.True(items[0].IsOutdated);
    }

    [Fact]
    public void IsOutdated_IsFalse_WhenNoThreadStatePresent()
    {
        var items = Build(
            threadStates: new Dictionary<long, CommentThreadBuilder.ReviewThreadState>());

        Assert.False(items[0].IsOutdated);
    }

    // ─── Reply availability ───────────────────────────────────────────────────

    [Fact]
    public void CanReply_IsTrue_WhenReplyCommandIsWired()
    {
        var items = Build(
            threadStates: new Dictionary<long, CommentThreadBuilder.ReviewThreadState>
            {
                [1] = new CommentThreadBuilder.ReviewThreadState("thread-1", false)
            },
            createReplyCommand: (item, _, _) => new ReplyCommand(
                item, _ => Task.CompletedTask, null,
                (_, _, _) => Task.FromResult(ReviewThreadMutationResult.Success())));

        Assert.True(items[0].CanReply);
        Assert.NotNull(items[0].ReplyCommand);
    }

    [Fact]
    public void CanReply_IsFalse_WhenNoReplyCommandFactory()
    {
        var items = Build(
            threadStates: new Dictionary<long, CommentThreadBuilder.ReviewThreadState>
            {
                [1] = new CommentThreadBuilder.ReviewThreadState("thread-1", false)
            });

        Assert.False(items[0].CanReply);
        Assert.Null(items[0].ReplyCommand);
    }

    // ─── Jump to Diff availability ────────────────────────────────────────────

    [Fact]
    public void CanJumpToDiff_IsTrue_WhenJumpToDiffCommandIsWired()
    {
        var items = Build(
            threadStates: new Dictionary<long, CommentThreadBuilder.ReviewThreadState>(),
            createJumpToDiffCommand: (_, _, _) => new StubAsyncCommand());

        Assert.True(items[0].CanJumpToDiff);
        Assert.NotNull(items[0].JumpToDiffCommand);
    }

    [Fact]
    public void CanJumpToDiff_IsFalse_WhenNoJumpToDiffCommandFactory()
    {
        var items = Build(
            threadStates: new Dictionary<long, CommentThreadBuilder.ReviewThreadState>());

        Assert.False(items[0].CanJumpToDiff);
        Assert.Null(items[0].JumpToDiffCommand);
    }

    // ─── PrCommentItem.ReplyText property change ──────────────────────────────

    [Fact]
    public void ReplyText_RaisesPropertyChanged_WhenSet()
    {
        var item = new PrCommentItem();
        var changedProperties = new List<string>();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName ?? "");

        item.ReplyText = "hello";

        Assert.Contains("ReplyText", changedProperties);
    }

    [Fact]
    public void ReplyText_DoesNotRaisePropertyChanged_WhenSameValueSet()
    {
        var item = new PrCommentItem { ReplyText = "hello" };
        var raised = false;
        item.PropertyChanged += (_, _) => raised = true;

        item.ReplyText = "hello";

        Assert.False(raised);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static IReadOnlyList<PrCommentItem> Build(
        IReadOnlyDictionary<long, CommentThreadBuilder.ReviewThreadState> threadStates,
        Func<PrCommentItem, CommentThreadBuilder.CommentSnapshot, CommentThreadBuilder.ReviewThreadState?, IAsyncCommand?>? createReplyCommand = null,
        Func<PrCommentItem, CommentThreadBuilder.CommentSnapshot, CommentThreadBuilder.ReviewThreadState?, IAsyncCommand?>? createJumpToDiffCommand = null)
    {
        return CommentThreadBuilder.Build(
            [
                new CommentThreadBuilder.CommentSnapshot(
                    1, @"src\File.cs", 10, "alice",
                    new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero),
                    "A comment", string.Empty, null)
            ],
            threadStates,
            _ => null,
            (_, _, _) => null,
            (_, _, _) => null,
            createReplyCommand,
            createJumpToDiffCommand);
    }

    private sealed class StubAsyncCommand : IAsyncCommand
    {
        public bool CanExecute => true;
        public Task ExecuteAsync(object? parameter, Microsoft.VisualStudio.Extensibility.IClientContext clientContext, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
