using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

internal sealed class OpenForReviewCommand : IAsyncCommand
{
    private readonly VisualStudioExtensibility _ext;
    private readonly string _workingPrFilePath;

    public OpenForReviewCommand(VisualStudioExtensibility ext, string workingPrFilePath)
    {
        _ext = ext;
        _workingPrFilePath = workingPrFilePath;
    }

    public bool CanExecute => true;

    // note: correct signature in the new model
    public async Task ExecuteAsync(object? parameter,
                                   IClientContext clientContext,
                                   CancellationToken cancellationToken)
    {
        // open the PR version so it's editable
        var documents = _ext.Documents();
        await documents.OpenDocumentAsync(
            new Uri(_workingPrFilePath, UriKind.Relative),
            cancellationToken);
    }
}
