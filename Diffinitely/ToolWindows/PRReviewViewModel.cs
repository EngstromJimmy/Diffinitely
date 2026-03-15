using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using Diffinitely.Commands;
using Diffinitely.Models;
using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.ToolWindows;

[DataContract]
internal class PRReviewViewModel : INotifyPropertyChanged
{
    private readonly GitHubPullRequestService _prService;
    private readonly VisualStudioExtensibility _visualStudioExtensibility;
    private readonly GitRepositoryService _repoService;

    private bool _isLoading;
    private string _loadingText = "Loading pull request...";
    private string _status = string.Empty;
    private string? _selectedAuthor;
    private string? _selectedResolutionFilter = "<All>";

    [DataMember]
    public IAsyncCommand RefreshCommand { get; }

    [DataMember]
    public ObservableCollection<string> AllAuthors { get; } = new();

    [DataMember]
    public ImageMoniker RefreshIcon { get; } = ImageMoniker.KnownValues.Refresh;

    [DataMember]
    public ObservableCollectionEx<PrCommentItem> AllComments { get; } = new();

    [DataMember]
    public ObservableCollectionEx<PrCommentItem> FilteredComments { get; } = new();

    [DataMember]
    public ObservableCollectionEx<TreeNode> Roots { get; } = [];

    [DataMember]
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                RaisePropertyChanged(nameof(IsLoading));
            }
        }
    }

    [DataMember]
    public string LoadingText
    {
        get => _loadingText;
        set
        {
            if (_loadingText != value)
            {
                _loadingText = value;
                RaisePropertyChanged(nameof(LoadingText));
            }
        }
    }

    [DataMember]
    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                RaisePropertyChanged(nameof(Status));
            }
        }
    }

    [DataMember]
    public string? SelectedAuthor
    {
        get => _selectedAuthor;
        set
        {
            if (_selectedAuthor != value)
            {
                _selectedAuthor = value;
                RaisePropertyChanged(nameof(SelectedAuthor));
                ApplyFilter();
            }
        }
    }

    [DataMember]
    public ObservableCollection<string> AllResolutionFilters { get; } = new() { "<All>", "Unresolved", "Resolved" };

    [DataMember]
    public string? SelectedResolutionFilter
    {
        get => _selectedResolutionFilter;
        set
        {
            if (_selectedResolutionFilter != value)
            {
                _selectedResolutionFilter = value;
                RaisePropertyChanged(nameof(SelectedResolutionFilter));
                ApplyFilter();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PRReviewViewModel(
        GitHubPullRequestService prService,
        GitRepositoryService repoService,
        VisualStudioExtensibility visualStudioExtensibility)
    {
        _prService = prService;
        _visualStudioExtensibility = visualStudioExtensibility;
        _repoService = repoService;
        RefreshCommand = new AsyncCommand(ExecuteRefreshAsync);
    }

    public async Task LoadPullRequestAsync(CancellationToken cancellationToken)
    {
        await ReloadTreeInternalAsync(cancellationToken);
    }

    public ObservableCollection<TreeNode> BuildTreeFromPaths(IEnumerable<ChangedFileInfo> files, PullRequestInfo prInfo)
    {
        return PathTreeBuilder.Build(files, (node, fi) =>
        {
            node.OpenCommentsCommand = new OpenForReviewCommand(_visualStudioExtensibility, fi.FullPath);
            node.OpenCommand = new OpenDiffCommand(fi, prInfo, _repoService);
        });
    }

    private void RaisePropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private async Task ExecuteRefreshAsync(
        object? parameter,
        IClientContext clientContext,
        CancellationToken cancellationToken)
    {
        await ReloadTreeInternalAsync(cancellationToken);
    }

    private async Task ReloadTreeInternalAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        LoadingText = "Loading pull request...";
        Status = string.Empty;

        var selectedAuthor = string.IsNullOrWhiteSpace(SelectedAuthor) ? "<All>" : SelectedAuthor!;
        var selectedResolutionFilter = string.IsNullOrWhiteSpace(SelectedResolutionFilter)
            ? "<All>"
            : SelectedResolutionFilter!;

        try
        {
            var pr = await _prService.GetCurrentBranchPullRequestAsync(cancellationToken);
            if (pr == null)
            {
                Roots.Clear();
                LoadingText = "No pull request for current branch.";
                return;
            }

            LoadingText = "Loading changed files...";
            var built = BuildTreeFromPaths(pr.ChangedFiles, pr);

            Roots.Clear();
            Roots.SupressNotification = true;
            foreach (var node in built)
            {
                Roots.Add(node);
            }
            Roots.SupressNotification = false;

            LoadingText = "Loading comments...";
            AllComments.Clear();
            FilteredComments.Clear();
            AllComments.SupressNotification = true;
            FilteredComments.SupressNotification = true;

            var commentItems = CommentThreadBuilder.Build(
                pr.Comments.Select(comment => new CommentThreadBuilder.CommentSnapshot(
                    comment.Id,
                    comment.Path ?? string.Empty,
                    comment.Position,
                    comment.User?.Login ?? string.Empty,
                    comment.CreatedAt,
                    comment.Body ?? string.Empty,
                    comment.User?.AvatarUrl ?? string.Empty,
                    comment.InReplyToId)),
                pr.ReviewThreads.ToDictionary(
                    entry => entry.Key,
                    entry => new CommentThreadBuilder.ReviewThreadState(entry.Value.Id, entry.Value.IsResolved)),
                comment => string.IsNullOrWhiteSpace(comment.FilePath)
                    ? null
                    : new OpenForReviewCommand(_visualStudioExtensibility, $"{pr.RepoRoot}\\{comment.FilePath}"),
                (item, _, threadState) =>
                {
                    if (threadState is null ||
                        threadState.IsResolved ||
                        string.IsNullOrWhiteSpace(threadState.ReviewThreadId))
                    {
                        return null;
                    }

                    return new ResolveCommand(
                        _prService,
                        item,
                        ReloadTreeInternalAsync,
                        message => Status = message);
                });

            foreach (var item in commentItems)
            {
                AllComments.Add(item);
            }

            AllAuthors.Clear();
            AllAuthors.Add("<All>");
            foreach (var authorName in AllComments
                         .Select(c => c.Author)
                         .Where(a => !string.IsNullOrWhiteSpace(a))
                         .Distinct()
                         .OrderBy(a => a))
            {
                AllAuthors.Add(authorName);
            }

            AllComments.SupressNotification = false;
            FilteredComments.SupressNotification = false;
            _selectedAuthor = AllAuthors.Contains(selectedAuthor) ? selectedAuthor : "<All>";
            _selectedResolutionFilter = AllResolutionFilters.Contains(selectedResolutionFilter)
                ? selectedResolutionFilter
                : "<All>";
            RaisePropertyChanged(nameof(SelectedAuthor));
            RaisePropertyChanged(nameof(SelectedResolutionFilter));
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }

        RaisePropertyChanged(nameof(Roots));
        RaisePropertyChanged(nameof(AllComments));
    }

    private void ApplyFilter()
    {
        FilteredComments.Clear();

        foreach (var item in CommentThreadBuilder.FilterComments(
                     AllComments,
                     SelectedAuthor,
                     SelectedResolutionFilter))
        {
            FilteredComments.Add(item);
        }
    }
}
