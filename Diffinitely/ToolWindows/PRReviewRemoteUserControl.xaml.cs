using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.ToolWindows
{
    using System.Threading;
    using System.Threading.Tasks;

    internal class PRReviewRemoteUserControl : RemoteUserControl
    {
        private readonly GitHubPullRequestService _prService = new GitHubPullRequestService();
        private readonly VisualStudioExtensibility _extensibility;
        public PRReviewViewModel ViewModel { get; internal set; }
        public PRReviewRemoteUserControl(VisualStudioExtensibility extensibility, GitHubPullRequestService prService, GitRepositoryService repoService) : base(dataContext: new PRReviewViewModel(prService, repoService, extensibility))
        {
            _extensibility = extensibility;
            ViewModel = (PRReviewViewModel)this.DataContext;
        }
        public override async Task ControlLoadedAsync(CancellationToken cancellationToken)
        {
            await ViewModel.LoadPullRequestAsync(cancellationToken);
            await base.ControlLoadedAsync(cancellationToken);
        }
    }
}
