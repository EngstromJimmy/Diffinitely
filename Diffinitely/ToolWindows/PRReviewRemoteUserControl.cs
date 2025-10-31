using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Diffinitely.Models;
using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.ToolWindows
{
    [DataContract]
    internal class PRFlatNode : NotifyPropertyChangedObject
    {
        public PRFlatNode() { }
        public PRFlatNode(string name, bool isFile, int depth, string fullPath)
        { Name = name; IsFile = isFile; Depth = depth; FullPath = fullPath; UpdateDisplay(); }
        [DataMember] public string Name { get; set; } = string.Empty;
        [DataMember] public bool IsFile { get; set; }
        [DataMember] public int Depth { get; set; }
        [DataMember] public string Kind { get; set; } = string.Empty;
        [DataMember] public string FullPath { get; set; } = string.Empty;
        private string _display = string.Empty;
        [DataMember] public string Display => _display;
        private void UpdateDisplay() => _display = new string(' ', Depth *2) + Name; // simple indent
    }

    [DataContract]
    internal class PRReviewData : NotifyPropertyChangedObject
    {
        public PRReviewData() { Nodes = new ObservableList<PRFlatNode>(); }
        [DataMember] public ObservableList<PRFlatNode> Nodes { get; }
        [DataMember] public AsyncCommand? OpenDiffCommand { get; set; }
        [DataMember] public string Title { get => _title; set => SetProperty(ref _title, value); }
        [DataMember] public string Status { get => _status; set => SetProperty(ref _status, value); }
        [DataMember] public string Owner { get; set; } = string.Empty;
        [DataMember] public string Repository { get; set; } = string.Empty;
        [DataMember] public string BaseSha { get; set; } = string.Empty;
        [DataMember] public string HeadSha { get; set; } = string.Empty;
        private string _title = "Loading pull request...";
        private string _status = string.Empty;
    }

    internal class PRReviewRemoteUserControl : RemoteUserControl
    {
        private readonly GitHubPullRequestService _prService = new GitHubPullRequestService();
        private readonly PRReviewData _data;
        private readonly VisualStudioExtensibility _extensibility;

        public PRReviewRemoteUserControl(VisualStudioExtensibility extensibility) : base(new PRReviewData())
        {
            _extensibility = extensibility;
            _data = (PRReviewData)DataContext!;
            _data.OpenDiffCommand = new AsyncCommand(OpenDiffAsync);
        }

        public override async Task ControlLoadedAsync(CancellationToken cancellationToken)
        {
            await base.ControlLoadedAsync(cancellationToken);
            try
            {
                var pr = await _prService.GetCurrentBranchPullRequestAsync(cancellationToken);
                if (pr == null)
                { _data.Title = "PR Review"; _data.Status = "No open pull request for current branch."; return; }
                _data.Title = $"PR #{pr.Id}: {pr.Title}";
                _data.Owner = pr.Owner ?? string.Empty;
                _data.Repository = pr.Repository ?? string.Empty;
                _data.BaseSha = pr.BaseSha ?? string.Empty;
                _data.HeadSha = pr.HeadSha ?? string.Empty;
                BuildFlatTree(pr.ChangedFiles);
                _data.Status = $"Files changed: {pr.ChangedFiles?.Count ??0}";
            }
            catch (Exception ex)
            { _data.Title = "PR Review"; _data.Status = "Error: " + ex.Message; }
        }

        private void BuildFlatTree(IReadOnlyList<ChangedFileInfo> files)
        {
            _data.Nodes.Clear();
            if (files == null) return;
            var folderSet = new HashSet<string>();
            foreach (var file in files.OrderBy(f => f.Path))
            {
                var parts = file.Path.Split('/');
                for (int i =0; i < parts.Length -1; i++)
                {
                    var folderPath = string.Join("/", parts.Take(i +1));
                    if (folderSet.Add(folderPath))
                    {
                        _data.Nodes.Add(new PRFlatNode(parts[i], false, i, folderPath));
                    }
                }
                _data.Nodes.Add(new PRFlatNode(parts.Last(), true, parts.Length -1, file.Path) { Kind = file.Kind.ToString() });
            }
        }

        private async Task OpenDiffAsync(object? parameter, CancellationToken ct)
        {
            if (parameter is not PRFlatNode node || !node.IsFile) return;
            if (string.IsNullOrEmpty(_data.Owner) || string.IsNullOrEmpty(_data.Repository) || string.IsNullOrEmpty(_data.HeadSha)) return;
            try
            {
                var headContent = await _prService.GetFileContentAsync(_data.Owner, _data.Repository, node.FullPath, _data.HeadSha, ct);
                var baseContent = string.Empty;
                if (!string.IsNullOrEmpty(_data.BaseSha))
                    baseContent = await _prService.GetFileContentAsync(_data.Owner, _data.Repository, node.FullPath, _data.BaseSha, ct) ?? string.Empty;
                var fileName = Path.GetFileName(node.FullPath);
                var tempDir = Path.Combine(Path.GetTempPath(), "DiffinitelyDiff");
                Directory.CreateDirectory(tempDir);
                var baseFile = Path.Combine(tempDir, Guid.NewGuid() + "_BASE_" + fileName);
                var headFile = Path.Combine(tempDir, Guid.NewGuid() + "_HEAD_" + fileName);
                File.WriteAllText(baseFile, baseContent ?? string.Empty);
                File.WriteAllText(headFile, headContent ?? string.Empty);
                // Placeholder: VS diff command invocation not available in current SDK surface here; inform user.
                _data.Status = "Diff prepared (base/head temp files). Manual open required.";
                await _extensibility.Shell().ShowPromptAsync("Diff files saved to temp folder. Integrate diff command when API becomes available.", Microsoft.VisualStudio.Extensibility.Shell.PromptOptions.OK, ct);
            }
            catch (Exception ex)
            {
                _data.Status = "Diff error: " + ex.Message;
            }
        }
    }
}
