using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.ToolWindows
{
    // View model / data context for the PR review tool window.
    [DataContract]
    internal class PRReviewData : NotifyPropertyChangedObject
    {
        public PRReviewData() => ChangedFiles = new ObservableList<ChangedFileItem>();

        private string _title = "Loading pull request...";
        [DataMember] public string Title { get => _title; set => SetProperty(ref _title, value); }

        private string _status = string.Empty;
        [DataMember] public string Status { get => _status; set => SetProperty(ref _status, value); }

        [DataMember] public ObservableList<ChangedFileItem> ChangedFiles { get; }
    }

    [DataContract]
    internal class ChangedFileItem
    {
        public ChangedFileItem() { }
        public ChangedFileItem(string path, string kind) { Path = path; Kind = kind; }
        [DataMember] public string Path { get; set; } = string.Empty;
        [DataMember] public string Kind { get; set; } = string.Empty;
    }

    internal class PRReviewRemoteUserControl : RemoteUserControl
    {
        private readonly GitHubPullRequestService _prService = new();
        private readonly PRReviewData _data;

        public PRReviewRemoteUserControl() : base(dataContext: new PRReviewData())
        {
            _data = (PRReviewData)DataContext!; // capture reference to vm passed to base
        }

        public override async Task ControlLoadedAsync(CancellationToken cancellationToken)
        {
            await base.ControlLoadedAsync(cancellationToken);

            try
            {
                if (!_prService.HasAuth)
                {
                    _data.Title = "PR Review";
                    _data.Status = "GitHub token not set.";
                    return;
                }
                var pr = await _prService.GetCurrentBranchPullRequestAsync(cancellationToken);
                if (pr == null)
                {
                    _data.Title = "PR Review";
                    _data.Status = "No open pull request for current branch.";
                    return;
                }
                _data.Title = $"PR #{pr.Id}: {pr.Title}";
                _data.Status = $"Files changed: {pr.ChangedFiles?.Count ??0}";
                _data.ChangedFiles.Clear();
                if (pr.ChangedFiles != null)
                {
                    foreach (var f in pr.ChangedFiles)
                        _data.ChangedFiles.Add(new ChangedFileItem(f.Path, f.Kind.ToString()));
                }
            }
            catch (System.Exception ex)
            {
                _data.Title = "PR Review";
                _data.Status = "Error: " + ex.Message;
            }
        }
    }
}
