using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.Models;

[DataContract]
public class PrCommentItem
{
    [DataMember]
    public long CommentId { get; set; }

    [DataMember]
    public string FilePath { get; set; } = "";

    [DataMember]
    public int? Line { get; set; }

    [DataMember]
    public string Author { get; set; } = "";

    [DataMember]
    public DateTimeOffset CreatedAt { get; set; }

    [DataMember]
    public string Body { get; set; } = "";

    [DataMember]
    public string AuthorAvatarUrl { get; set; } = "";

    [DataMember]
    public bool IsResolved { get; set; }

    [DataMember]
    public string ReviewThreadId { get; set; } = "";

    [DataMember]
    public bool CanResolve { get; set; }

    [DataMember]
    public bool CanUnresolve { get; set; }

    [DataMember]
    public ObservableCollection<PrCommentReply> ThreadReplies { get; set; } = new();

    [DataMember] public IAsyncCommand? ResolveCommand { get; set; }
    [DataMember] public IAsyncCommand? UnresolveCommand { get; set; }
    [DataMember] public IAsyncCommand? ViewCommand { get; set; }
}

[DataContract]
public class PrCommentReply
{
    [DataMember] public string Author { get; set; } = "";
    [DataMember] public DateTimeOffset CreatedAt { get; set; }
    [DataMember] public string Body { get; set; } = "";
}
