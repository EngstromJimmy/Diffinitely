using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.Commands;

public class NoopCommand : IAsyncCommand
{
    public bool CanExecute => true;

    public Task ExecuteAsync(object? parameter,
                             IClientContext context,
                             CancellationToken token)
    {
        System.Diagnostics.Debug.WriteLine("Clicked");
        return Task.CompletedTask;
    }
}
