using System.Diagnostics;
using Diffinitely.ToolWindows;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
namespace Diffinitely
{
    [VisualStudioContribution]
    internal class PRReviewCommand : Command
    {
        private readonly TraceSource _trace;

        public PRReviewCommand(TraceSource traceSource)
        {
            _trace = Requires.NotNull(traceSource, nameof(traceSource));
        }

        public override CommandConfiguration CommandConfiguration => new("PR Review")
        {
            Icon = new(ImageMoniker.KnownValues.AboutBox, IconSettings.IconAndText),
            // Show under Extensions menu initially; you can add more placements later.
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
        };

        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            // Show (or create) the tool window.
            await this.Extensibility.Shell().ShowToolWindowAsync<PRReviewToolWindow>(true, cancellationToken);
        }
    }
}
