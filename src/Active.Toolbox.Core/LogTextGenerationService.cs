using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace Active.Toolbox.Core;

public class LogTextGenerationService(ITextGenerationService textGenerationService) : ITextGenerationService
{
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return textGenerationService.GetTextContentsAsync(prompt, executionSettings, kernel, cancellationToken);
    }

    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
    {
        return textGenerationService.GetStreamingTextContentsAsync(prompt, executionSettings, kernel, cancellationToken);
    }
}