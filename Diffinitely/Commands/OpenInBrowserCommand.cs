using System.Diagnostics;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.Commands;

internal sealed class OpenInBrowserCommand : IAsyncCommand
{
    private readonly string _url;

    public OpenInBrowserCommand(string url)
    {
        _url = url;
    }

    public bool CanExecute => !string.IsNullOrWhiteSpace(_url);

    public Task ExecuteAsync(object? parameter, IClientContext clientContext, CancellationToken cancellationToken)
    {
        if (!CanExecute)
        {
            return Task.CompletedTask;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }
        catch
        {
            // Silent failure - opening browser is best-effort
        }

        return Task.CompletedTask;
    }
}
