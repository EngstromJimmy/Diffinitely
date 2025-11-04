using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid("0005633D-BF8A-4B8A-A711-0741CCA434D7")]
public sealed class DiffinitelyPackage : AsyncPackage
{
    internal static DiffinitelyPackage? Instance { get; private set; }

    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        // optional: await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
    }
}
