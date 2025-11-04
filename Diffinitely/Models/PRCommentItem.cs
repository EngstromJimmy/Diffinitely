using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.Models;

[DataContract]
public class PrCommentItem
{
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

    // commands
    [DataMember] public IAsyncCommand? ReplyCommand { get; set; }
    [DataMember] public IAsyncCommand? ResolveCommand { get; set; }
    [DataMember] public IAsyncCommand? ReopenCommand { get; set; }
    [DataMember] public IAsyncCommand? ViewThreadCommand { get; set; }
}

