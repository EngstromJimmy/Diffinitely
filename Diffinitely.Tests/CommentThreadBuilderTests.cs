using Diffinitely.Commands;
using Diffinitely.Models;
using Diffinitely.Services;
using Diffinitely.ToolWindows;
using Xunit;

namespace Diffinitely.Tests;

public class CommentThreadBuilderTests
{
    [Fact]
    public void TopLevelCommentsOnSameFileAndLine_StaySeparate()
    {
        var items = CommentThreadBuilder.Build(
            [
                Comment(1, @"src\File.cs", 42, "alice", new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero), "First"),
                Comment(2, @"src\File.cs", 42, "bob", new DateTimeOffset(2026, 3, 12, 10, 1, 0, TimeSpan.Zero), "Second")
            ],
            new Dictionary<long, CommentThreadBuilder.ReviewThreadState>(),
            _ => null,
            (_, _, _) => null,
            (_, _, _) => null);

        Assert.Collection(
            items,
            first => Assert.Equal("First", first.Body),
            second => Assert.Equal("Second", second.Body));
    }

    [Fact]
    public void NestedReplies_AreFlattenedUnderTheirTrueTopLevelThread()
    {
        var items = CommentThreadBuilder.Build(
            [
                Comment(10, @"src\File.cs", 7, "alice", new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero), "Root"),
                Comment(11, @"src\File.cs", 7, "bob", new DateTimeOffset(2026, 3, 12, 10, 1, 0, TimeSpan.Zero), "Reply", 10),
                Comment(12, @"src\File.cs", 7, "carol", new DateTimeOffset(2026, 3, 12, 10, 2, 0, TimeSpan.Zero), "Reply to reply", 11)
            ],
            new Dictionary<long, CommentThreadBuilder.ReviewThreadState>(),
            _ => null,
            (_, _, _) => null,
            (_, _, _) => null);

        var root = Assert.Single(items);
        Assert.Collection(
            root.ThreadReplies,
            firstReply => Assert.Equal("Reply", firstReply.Body),
            secondReply => Assert.Equal("Reply to reply", secondReply.Body));
    }

    [Fact]
    public void ResolveAffordance_OnlyExistsWhenThreadCanActuallyResolve()
    {
        var items = CommentThreadBuilder.Build(
            [
                Comment(21, @"src\File.cs", 9, "alice", new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero), "Ready"),
                Comment(22, @"src\File.cs", 10, "bob", new DateTimeOffset(2026, 3, 12, 10, 1, 0, TimeSpan.Zero), "Already resolved"),
                Comment(23, @"src\File.cs", 11, "carol", new DateTimeOffset(2026, 3, 12, 10, 2, 0, TimeSpan.Zero), "Missing thread id")
            ],
            new Dictionary<long, CommentThreadBuilder.ReviewThreadState>
            {
                [21] = new CommentThreadBuilder.ReviewThreadState("thread-21", false),
                [22] = new CommentThreadBuilder.ReviewThreadState("thread-22", true)
            },
            _ => null,
            (item, _, threadState) => threadState is not null && !threadState.IsResolved
                ? new ResolveCommand(item, _ => Task.CompletedTask, null, (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()))
                : null,
            (item, _, threadState) => threadState is not null && threadState.IsResolved
                ? new UnresolveCommand(item, _ => Task.CompletedTask, null, (_, _) => Task.FromResult(ReviewThreadMutationResult.Success()))
                : null);

        Assert.Collection(
            items,
            first =>
            {
                Assert.True(first.CanResolve);
                Assert.NotNull(first.ResolveCommand);
            },
            second =>
            {
                Assert.False(second.CanResolve);
                Assert.Null(second.ResolveCommand);
            },
            third =>
            {
                Assert.False(third.CanResolve);
                Assert.Null(third.ResolveCommand);
            });
    }

    [Fact]
    public void FilterComments_UsesCurrentResolutionState()
    {
        var comments =
            new[]
            {
                new PrCommentItem
                {
                    CommentId = 1,
                    Author = "alice",
                    Body = "Resolved",
                    CreatedAt = new DateTimeOffset(2026, 3, 12, 10, 0, 0, TimeSpan.Zero),
                    IsResolved = true
                },
                new PrCommentItem
                {
                    CommentId = 2,
                    Author = "bob",
                    Body = "Unresolved",
                    CreatedAt = new DateTimeOffset(2026, 3, 12, 11, 0, 0, TimeSpan.Zero),
                    IsResolved = false
                }
            };

        var unresolved = CommentThreadBuilder.FilterComments(comments, "<All>", "Unresolved");
        var resolved = CommentThreadBuilder.FilterComments(comments, "<All>", "Resolved");

        Assert.Single(unresolved);
        Assert.Equal("Unresolved", unresolved[0].Body);
        Assert.Single(resolved);
        Assert.Equal("Resolved", resolved[0].Body);
    }

    private static CommentThreadBuilder.CommentSnapshot Comment(
        long id,
        string filePath,
        int? line,
        string author,
        DateTimeOffset createdAt,
        string body,
        long? inReplyToId = null) =>
        new CommentThreadBuilder.CommentSnapshot(
            id,
            filePath,
            line,
            author,
            createdAt,
            body,
            string.Empty,
            inReplyToId);
}
