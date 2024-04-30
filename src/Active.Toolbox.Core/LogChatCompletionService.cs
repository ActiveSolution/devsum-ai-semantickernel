using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Active.Toolbox.Core;

public class LogChatCompletionService(IChatCompletionService chatCompletionService) : IChatCompletionService
{
    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null, CancellationToken cancellationToken = new CancellationToken())
    {
        return chatCompletionService.GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
    }
}