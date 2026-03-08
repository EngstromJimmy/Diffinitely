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

            var baseContent = repoService.GetFileContentFromCommit(prInfo.RepoRoot, prInfo.BaseSha, fileInfo.Path);
            var leftFile = Path.GetTempFileName() + Path.GetExtension(fileInfo.FullPath);
            File.WriteAllText(leftFile, baseContent);

            if (fileInfo.Kind == Models.ChangeKind.Deleted)
            {
                // File no longer exists on disk — show base content on left, empty file on right.
                var rightFile = Path.GetTempFileName() + Path.GetExtension(fileInfo.FullPath);
                File.WriteAllText(rightFile, string.Empty);

                const __VSDIFFSERVICEOPTIONS deletedOptions =
                    __VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary |
                    __VSDIFFSERVICEOPTIONS.VSDIFFOPT_RightFileIsTemporary |
                    __VSDIFFSERVICEOPTIONS.VSDIFFOPT_DetectBinaryFiles;

                diffService.OpenComparisonWindow2(
                    leftFile,
                    rightFile,
                    "Pull Request",
                    "PR",
                    fileInfo.Path + " (deleted)",
                    "(deleted)",
                    null,
                    null,
                    (uint)deletedOptions);
            }
            else
            {
                const __VSDIFFSERVICEOPTIONS options =
                    __VSDIFFSERVICEOPTIONS.VSDIFFOPT_LeftFileIsTemporary |
                    __VSDIFFSERVICEOPTIONS.VSDIFFOPT_DetectBinaryFiles;

                diffService.OpenComparisonWindow2(
                    leftFile,
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
}
