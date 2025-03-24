using KernelMemoryService.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;

namespace KernelMemoryService.Services;

public class ChatService(IChatCompletionService chatCompletionService, HybridCache cache, IOptions<AppSettings> appSettingsOptions)
{
    private readonly AppSettings appSettings = appSettingsOptions.Value;

    public async Task<string> CreateQuestionAsync(Guid conversationId, string question, CancellationToken cancellationToken = default)
    {
        var chat = await GetChatHistoryAsync(conversationId, cancellationToken);

        var embeddingQuestion = $"""
            Reformulate the following question taking into account the context of the chat to perform embeddings search:
            ---
            {question}
            ---
            The reformulation must always explicitly contain the subject of the question.            
            You must reformulate the question in the same language of the user's question. For example, it the user asks a question in English, the answer must be in English.
            Never add "in this chat", "in the context of this chat", "in the context of our conversation", "search for" or something like that in your answer.
            """;

        chat.AddUserMessage(embeddingQuestion);

        var reformulatedQuestion = await chatCompletionService.GetChatMessageContentAsync(chat, cancellationToken: cancellationToken)!;
        chat.AddAssistantMessage(reformulatedQuestion.Content!);

        await UpdateCacheAsync(conversationId, chat, cancellationToken);

        return reformulatedQuestion.Content!;
    }

    public Task AddInteractionAsync(Guid conversationId, string question, string answer, CancellationToken cancellationToken = default)
        => SetChatHistoryAsync(conversationId, question, answer, cancellationToken);
    private async Task UpdateCacheAsync(Guid conversationId, ChatHistory chat, CancellationToken cancellationToken)
    {
        if (chat.Count > appSettings.MessageLimit)
        {
            chat.RemoveRange(0, chat.Count - appSettings.MessageLimit);
        }

        await cache.SetAsync(conversationId.ToString(), chat, cancellationToken: cancellationToken);
    }

    private async Task<ChatHistory> GetChatHistoryAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var historyCache = await cache.GetOrCreateAsync(conversationId.ToString(), (cancellationToken) =>
        {
            return ValueTask.FromResult<ChatHistory>([]);
        }, cancellationToken: cancellationToken);

        var chat = new ChatHistory(historyCache);
        return chat;
    }

    private async Task SetChatHistoryAsync(Guid conversationId, string question, string answer, CancellationToken cancellationToken)
    {
        var history = await GetChatHistoryAsync(conversationId, cancellationToken);

        history.AddUserMessage(question);
        history.AddAssistantMessage(answer);

        await UpdateCacheAsync(conversationId, history, cancellationToken);
    }
}
