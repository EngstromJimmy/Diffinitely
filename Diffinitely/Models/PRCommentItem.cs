using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.Models;

[DataContract]
public class PrCommentItem : INotifyPropertyChanged
{
    private string _replyText = "";

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
    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm");

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
    public bool IsOutdated { get; set; }

    [DataMember]
    public bool CanReply { get; set; }

    [DataMember]
    public bool CanJumpToDiff { get; set; }

    [DataMember]
    public string ReplyText
    {
        get => _replyText;
        set
        {
            if (_replyText != value)
            {
                _replyText = value;
                RaisePropertyChanged();
            }
        }
    }

    [DataMember]
    public ObservableCollection<PrCommentReply> ThreadReplies { get; set; } = new();

    [DataMember] public IAsyncCommand? ResolveCommand { get; set; }
    [DataMember] public IAsyncCommand? UnresolveCommand { get; set; }
    [DataMember] public IAsyncCommand? ViewCommand { get; set; }
    [DataMember] public IAsyncCommand? ReplyCommand { get; set; }
    [DataMember] public IAsyncCommand? JumpToDiffCommand { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? propName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}

[DataContract]
public class PrCommentReply
{
    [DataMember] public string Author { get; set; } = "";
    [DataMember] public DateTimeOffset CreatedAt { get; set; }
    [DataMember] public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm");
    [DataMember] public string Body { get; set; } = "";
}
