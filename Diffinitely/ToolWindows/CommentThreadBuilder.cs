using Diffinitely.Models;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.ToolWindows;

internal static class CommentThreadBuilder
{
    internal sealed class CommentSnapshot
    {
        public CommentSnapshot(
            long id,
            string filePath,
            int? line,
            string author,
            DateTimeOffset createdAt,
            string body,
            string authorAvatarUrl,
            long? inReplyToId)
        {
            Id = id;
            FilePath = filePath;
            Line = line;
            Author = author;
            CreatedAt = createdAt;
            Body = body;
            AuthorAvatarUrl = authorAvatarUrl;
            InReplyToId = inReplyToId;
        }

        public long Id { get; }
        public string FilePath { get; }
        public int? Line { get; }
        public string Author { get; }
        public DateTimeOffset CreatedAt { get; }
        public string Body { get; }
        public string AuthorAvatarUrl { get; }
        public long? InReplyToId { get; }
    }

    internal sealed class ReviewThreadState
    {
        public ReviewThreadState(string reviewThreadId, bool isResolved, bool isOutdated = false)
        {
            ReviewThreadId = reviewThreadId;
            IsResolved = isResolved;
            IsOutdated = isOutdated;
        }

        public string ReviewThreadId { get; }
        public bool IsResolved { get; }
        public bool IsOutdated { get; }
    }

    internal static IReadOnlyList<PrCommentItem> Build(
        IEnumerable<CommentSnapshot> comments,
        IReadOnlyDictionary<long, ReviewThreadState> threadStates,
        Func<CommentSnapshot, IAsyncCommand?> createViewCommand,
        Func<PrCommentItem, CommentSnapshot, ReviewThreadState?, IAsyncCommand?> createResolveCommand,
        Func<PrCommentItem, CommentSnapshot, ReviewThreadState?, IAsyncCommand?> createUnresolveCommand,
        Func<PrCommentItem, CommentSnapshot, ReviewThreadState?, IAsyncCommand?>? createReplyCommand = null,
        Func<PrCommentItem, CommentSnapshot, ReviewThreadState?, IAsyncCommand?>? createJumpToDiffCommand = null)
    {
        var orderedComments = comments.OrderBy(comment => comment.CreatedAt).ToList();
        var commentsById = orderedComments.ToDictionary(comment => comment.Id);
        var topLevelItems = new Dictionary<long, PrCommentItem>();

        foreach (var comment in orderedComments.Where(comment => !comment.InReplyToId.HasValue))
        {
            threadStates.TryGetValue(comment.Id, out var threadState);
            var item = new PrCommentItem
            {
                CommentId = comment.Id,
                FilePath = comment.FilePath,
                Line = comment.Line,
                Author = comment.Author,
                CreatedAt = comment.CreatedAt,
                Body = comment.Body,
                AuthorAvatarUrl = comment.AuthorAvatarUrl,
                IsResolved = threadState?.IsResolved ?? false,
                ReviewThreadId = threadState?.ReviewThreadId ?? string.Empty,
                ViewCommand = createViewCommand(comment)
            };

            item.ResolveCommand = createResolveCommand(item, comment, threadState);
            item.UnresolveCommand = createUnresolveCommand(item, comment, threadState);
            item.ReplyCommand = createReplyCommand?.Invoke(item, comment, threadState);
            item.JumpToDiffCommand = createJumpToDiffCommand?.Invoke(item, comment, threadState);
            item.CanResolve = item.ResolveCommand is not null
                && threadState is not null
                && !threadState.IsResolved
                && !string.IsNullOrWhiteSpace(threadState.ReviewThreadId);
            item.CanUnresolve = item.UnresolveCommand is not null
                && threadState is not null
                && threadState.IsResolved
                && !string.IsNullOrWhiteSpace(threadState.ReviewThreadId);
            item.IsOutdated = threadState?.IsOutdated ?? false;
            item.CanReply = item.ReplyCommand is not null;
            item.CanJumpToDiff = item.JumpToDiffCommand is not null;

            topLevelItems[comment.Id] = item;
        }

        foreach (var comment in orderedComments.Where(comment => comment.InReplyToId.HasValue))
        {
            var rootCommentId = FindTopLevelCommentId(comment, commentsById);
            if (!topLevelItems.TryGetValue(rootCommentId, out var rootItem))
            {
                continue;
            }

            rootItem.ThreadReplies.Add(new PrCommentReply
            {
                Author = comment.Author,
                CreatedAt = comment.CreatedAt,
                Body = comment.Body
            });
        }

        return orderedComments
            .Where(comment => !comment.InReplyToId.HasValue)
            .Select(comment => topLevelItems[comment.Id])
            .ToList();
    }

    internal static IReadOnlyList<PrCommentItem> FilterComments(
        IEnumerable<PrCommentItem> comments,
        string? selectedAuthor,
        string? selectedResolutionFilter)
    {
        var filtered = comments;

        if (!string.IsNullOrWhiteSpace(selectedAuthor) && selectedAuthor != "<All>")
        {
            filtered = filtered.Where(comment => comment.Author == selectedAuthor);
        }

        filtered = selectedResolutionFilter switch
        {
            "Resolved" => filtered.Where(comment => comment.IsResolved),
            "Unresolved" => filtered.Where(comment => !comment.IsResolved),
            _ => filtered
        };

        return filtered
            .OrderByDescending(comment => comment.CreatedAt)
            .ToList();
    }

    private static long FindTopLevelCommentId(
        CommentSnapshot comment,
        IReadOnlyDictionary<long, CommentSnapshot> commentsById)
    {
        var current = comment;
        var visitedCommentIds = new HashSet<long> { current.Id };

        while (current.InReplyToId is long parentCommentId
            && commentsById.TryGetValue(parentCommentId, out var parentComment)
            && visitedCommentIds.Add(parentCommentId))
        {
            current = parentComment;
        }

        return current.Id;
    }
}
