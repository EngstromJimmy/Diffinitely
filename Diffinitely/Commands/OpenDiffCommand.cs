using System.IO;
using Diffinitely.Models;
using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Diffinitely.Commands
{
    internal sealed class OpenDiffCommand(ChangedFileInfo fileInfo, PullRequestInfo prInfo, GitRepositoryService repoService) : IAsyncCommand
    {
        public bool CanExecute => true;

        public async Task ExecuteAsync(
            object? parameter,
            IClientContext clientContext,
            CancellationToken cancellationToken)
        {
            // Must be on main thread to talk to shell/UI.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Ask VS for the diff service
            var diffServiceObj = ServiceProvider.GlobalProvider.GetService(typeof(SVsDifferenceService));
            var diffService = diffServiceObj as IVsDifferenceService;
            if (diffService is null)
            {
                // If this happens, we're somehow not really in-proc.
                return;
            }

            const __VSDIFFSERVICEOPTIONS options =
                __VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary |
                __VSDIFFSERVICEOPTIONS.VSDIFFOPT_DetectBinaryFiles;

            var baseContent = repoService.GetFileContentFromCommit(prInfo.RepoRoot, prInfo.BaseSha, fileInfo.Path);

            var fileName = Path.GetTempFileName() + Path.GetExtension(fileInfo.FullPath);
            File.WriteAllText(fileName, baseContent);

            diffService.OpenComparisonWindow2(
               fileName,
               fileInfo.FullPath,
                "Pull Request",
                "PR",
                "Main",
                "PR",
                null,
                null,
                (uint)options);
        }
    }
}
