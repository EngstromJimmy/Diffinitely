using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    // services we already need
    private readonly GitHubPullRequestService _prService;
    private readonly VisualStudioExtensibility _visualStudioExtensibility;
    private readonly GitRepositoryService _repoService;

    // this is the command the Refresh button will bind to
    [DataMember]
    public IAsyncCommand RefreshCommand { get; }
    [DataMember]
    public ObservableCollection<string> AllAuthors { get; } = new();
    // this is just so we can show an icon in the Refresh button (optional but nice)
    [DataMember]
    public ImageMoniker RefreshIcon { get; } = ImageMoniker.KnownValues.Refresh;

    [DataMember]
    public ObservableCollectionEx<PrCommentItem> AllComments { get; } = new();
    [DataMember]
    public ObservableCollectionEx<PrCommentItem> FilteredComments { get; } = new();
    [DataMember]
    public ObservableCollectionEx<TreeNode> Roots { get; } = [];

    // Loading indicator properties
    private bool _isLoading;
    [DataMember]
    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading != value) { _isLoading = value; RaisePropertyChanged(nameof(IsLoading)); } }
    }
    private string _loadingText = "Loading pull request...";
    [DataMember]
    public string LoadingText
    {
        get => _loadingText;
        set { if (_loadingText != value) { _loadingText = value; RaisePropertyChanged(nameof(LoadingText)); } }
    }

    // currently selected author from the ComboBox
    private string? _selectedAuthor;
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

    // resolution status filter
    [DataMember]
    public ObservableCollection<string> AllResolutionFilters { get; } = new() { "<All>", "Unresolved", "Resolved" };

    private string? _selectedResolutionFilter = "<All>";
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
    private void RaisePropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
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
        try
        {
            var pr = await _prService.GetCurrentBranchPullRequestAsync(cancellationToken);
            if (pr == null)
            {
                // no PR? clear tree
                Roots.Clear();
                LoadingText = "No pull request for current branch.";
                return;
            }

            LoadingText = "Loading changed files...";
            var built = BuildTreeFromPaths(pr.ChangedFiles, pr);

            //Add treeview
            Roots.Clear();
            Roots.SupressNotification = true;
            foreach (var node in built)
            {
                Roots.Add(node);
            }
            Roots.SupressNotification = false;

            LoadingText = "Loading comments...";
            //Add comments
            AllComments.Clear();
            FilteredComments.Clear();
            AllComments.SupressNotification = true;
            FilteredComments.SupressNotification = true;

            // Build comment items first (one per raw comment)
            var tempMap = new Dictionary<long, PrCommentItem>();
            foreach (var c in pr.Comments.OrderBy(c => c.CreatedAt))
            {
                var item = new PrCommentItem
                {
                    FilePath = c.Path ?? "",
                    Line = c.Position,
                    Author = c.User?.Login ?? "",
                    CreatedAt = c.CreatedAt,
                    Body = c.Body ?? "",
                    AuthorAvatarUrl = c.User?.AvatarUrl ?? "",
                    IsResolved = pr.ThreadResolution.TryGetValue(c.Id, out var resolved) && resolved,
                    ViewCommand = new OpenForReviewCommand(_visualStudioExtensibility, pr.RepoRoot + "\\" + c.Path),
                    //ResolveCommand = new ResolveCommand(_visualStudioExtensibility, pr.RepoRoot + "\\" + c.Path)
                };
                tempMap[c.Id] = item;
            }
            // Link replies using InReplyToId (GitHub provides direct parent id)
            foreach (var c in pr.Comments.OrderBy(c => c.CreatedAt))
            {
                if (c.InReplyToId.HasValue && tempMap.TryGetValue(c.InReplyToId.Value, out var parent))
                {
                    // Append as reply to parent (flatten multi-level by always attaching to direct parent)
                    var replyItem = tempMap[c.Id];
                    parent.ThreadReplies.Add(new PrCommentReply { Author = replyItem.Author, CreatedAt = replyItem.CreatedAt, Body = replyItem.Body });
                }
            }
            // Add only top-level comments (no InReplyToId) to AllComments
            foreach (var c in pr.Comments.Where(cm => !cm.InReplyToId.HasValue).OrderBy(cm => cm.CreatedAt))
            {
                AllComments.Add(tempMap[c.Id]);
            }

            // build threads (group by file+line)
            foreach (var group in AllComments.GroupBy(cm => (cm.FilePath, cm.Line)))
            {
                var ordered = group.OrderBy(g => g.CreatedAt).ToList();
                if (ordered.Count > 1)
                {
                    // treat first as root, attach rest as replies
                    var root = ordered[0];
                    for (int i = 1; i < ordered.Count; i++)
                    {
                        var reply = ordered[i];
                        root.ThreadReplies.Add(new PrCommentReply
                        {
                            Author = reply.Author,
                            CreatedAt = reply.CreatedAt,
                            Body = reply.Body
                        });
                    }
                }
            }

            // Build authors list ("All" + distinct names)
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

            // Default filter: show all
            SelectedAuthor = "<All>";
            SelectedResolutionFilter = "<All>";
            AllComments.SupressNotification = false;
            FilteredComments.SupressNotification = false;
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
        RaisePropertyChanged(nameof(Roots));
        RaisePropertyChanged(nameof(AllComments));
    }

    //
    // build tree (folders + file leaves)
    //
    public ObservableCollection<TreeNode> BuildTreeFromPaths(IEnumerable<ChangedFileInfo> files, PullRequestInfo prInfo)
    {
        var roots = new ObservableCollection<TreeNode>();

        foreach (var f in files)
        {
            AddPath(roots, f, 0, prInfo);
        }

        return roots;
    }

    //
    // walk "Folder/SubFolder/File.cs" and build nodes
    //
    private void AddPath(ObservableCollection<TreeNode> nodes, ChangedFileInfo fileInfo, int index, PullRequestInfo prInfo)
    {
        // split on / or \ just in case
        var parts = fileInfo.Path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        bool isLeaf = index == parts.Length - 1;
        string segment = parts[index];

        var node = nodes.FirstOrDefault(n => n.Name == segment);
        if (node == null)
        {
            node = new TreeNode
            {
                Name = segment,
                Icon = GetIconForSegment(segment, isLeaf),
                IsExpanded = !isLeaf && !segment.StartsWith('.'),
                FullPath = fileInfo.FullPath
            };

            // If it's a file node, attach the "open file" command
            if (isLeaf)
            {
                //string mainSnapshotPath = WriteTempVersion(
                //baseBranchName: "main",
                //relativePath: fileInfo.Path,
                //baseContent: fileInfo.BaseContentFromMain);
                node.OpenCommentsCommand = new OpenForReviewCommand(_visualStudioExtensibility, fileInfo.FullPath);
                node.OpenCommand = new OpenDiffCommand(fileInfo, prInfo, _repoService);
                node.CommentCount = fileInfo.CommentCount;
                node.ChangeKind = fileInfo.Kind.ToString();
            }

            nodes.Add(node);
        }

        // go deeper if we're not at the file leaf yet
        if (!isLeaf)
        {
            AddPath(node.Children, fileInfo, index + 1, prInfo);
        }
    }

    private static ImageMoniker GetIconForSegment(string name, bool isLeaf)
    {
        if (!isLeaf)
        {
            return ImageMoniker.KnownValues.FolderClosed;
        }

        var ext = Path.GetExtension(name)?.ToLowerInvariant();

        return ext switch
        {
            // Code
            ".cs" => ImageMoniker.KnownValues.CSFileNode,
            ".vb" => ImageMoniker.KnownValues.VBFileNode,
            ".ts" => ImageMoniker.KnownValues.TSSourceFile,
            ".tsx" => ImageMoniker.KnownValues.TSSourceFile,
            ".js" => ImageMoniker.KnownValues.JSScript,
            ".jsx" => ImageMoniker.KnownValues.JSScript,
            ".css" => ImageMoniker.KnownValues.CSSClass,
            ".scss" => ImageMoniker.KnownValues.CSSClass,
            ".less" => ImageMoniker.KnownValues.CSSClass,
            ".html" => ImageMoniker.KnownValues.HTMLFile,
            ".htm" => ImageMoniker.KnownValues.HTMLFile,
            ".razor" => ImageMoniker.KnownValues.ASPRazorFile,
            ".cshtml" => ImageMoniker.KnownValues.ASPRazorFile,

            // Json / yaml / config
            ".json" => ImageMoniker.KnownValues.JSONScript,
            ".yml" => ImageMoniker.KnownValues.YamlFile,
            ".yaml" => ImageMoniker.KnownValues.YamlFile,
            ".xml" => ImageMoniker.KnownValues.XMLFile,
            ".config" => ImageMoniker.KnownValues.SettingsFile,
            ".props" => ImageMoniker.KnownValues.XMLFile,
            ".targets" => ImageMoniker.KnownValues.XMLFile,

            // Markdown / text / docs
            ".md" => ImageMoniker.KnownValues.MarkdownFile,
            ".txt" => ImageMoniker.KnownValues.TextFile,
            ".log" => ImageMoniker.KnownValues.TextFile,
            ".license" => ImageMoniker.KnownValues.TextFile,

            // Project/build
            ".sln" => ImageMoniker.KnownValues.Solution,
            ".csproj" => ImageMoniker.KnownValues.CSProjectNode,
            ".vbproj" => ImageMoniker.KnownValues.VBProjectNode,
            ".fsproj" => ImageMoniker.KnownValues.FSProjectNode,
            ".proj" => ImageMoniker.KnownValues.CSProjectNode,
            ".nuspec" => ImageMoniker.KnownValues.NuGet,
            ".nupkg" => ImageMoniker.KnownValues.NuGet,

            // Images
            ".png" => ImageMoniker.KnownValues.Image,
            ".jpg" => ImageMoniker.KnownValues.Image,
            ".jpeg" => ImageMoniker.KnownValues.Image,
            ".gif" => ImageMoniker.KnownValues.Image,
            ".svg" => ImageMoniker.KnownValues.Image,

            // default / unknown
            _ => ImageMoniker.KnownValues.TargetFile
        };
    }
    private void ApplyFilter()
    {
        FilteredComments.Clear();

        IEnumerable<PrCommentItem> source = AllComments;

        if (!string.IsNullOrEmpty(SelectedAuthor) &&
            SelectedAuthor != "<All>")
        {
            source = source.Where(c => c.Author == SelectedAuthor);
        }

        if (SelectedResolutionFilter == "Resolved")
        {
            source = source.Where(c => c.IsResolved);
        }
        else if (SelectedResolutionFilter == "Unresolved")
        {
            source = source.Where(c => !c.IsResolved);
        }

        // We could sort here too (e.g. newest first)
        foreach (var item in source
            .OrderByDescending(c => c.CreatedAt))
        {
            FilteredComments.Add(item);
        }
    }
}
