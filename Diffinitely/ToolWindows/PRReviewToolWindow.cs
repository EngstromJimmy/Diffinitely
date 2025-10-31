using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;

namespace Diffinitely.ToolWindows
{
    [VisualStudioContribution]
    internal class PRReviewToolWindow : ToolWindow
    {
        public override ToolWindowConfiguration ToolWindowConfiguration => new()
        {
        };

        public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IRemoteUserControl>(new PRReviewRemoteUserControl(this.Extensibility));
        }
    }
}
