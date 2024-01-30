using KernelMemoryService.Models;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.ChatCompletion;

namespace KernelMemoryService.Services;

public class ApplicationMemoryService(IKernelMemory memory, IChatCompletionService chatCompletionService)
{
    private readonly ChatHistory chat = [];

    public async Task<string> ImportAsync(Stream content, string name, string? documentId)
    {
        documentId = await memory.ImportDocumentAsync(content, name, documentId);
        return documentId;
    }

    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(string documentId)
    {
        var status = await memory.GetDocumentStatusAsync(documentId);
        return status;
    }

    public async Task DeleteDocumentAsync(string documentId)
        => await memory.DeleteDocumentAsync(documentId);

    public async Task<MemoryResponse?> AskQuestionAsync(string question)
    {
        // Reformulate the following question taking into account the context of the chat to perform keyword search and embeddings:
        var embeddingQuestion = $"""
            Reformulate the following question taking into account the context of the chat to perform embeddings search:
            ---
            {question}
            ---
            You must answer in the same language of the user's question.
            Never add "in this chat", "in the context of this chat", "in the context of our conversation", "search for" or something like that in your answer.
            """;

        chat.AddUserMessage(embeddingQuestion);
        var reformulatedQuestion = await chatCompletionService.GetChatMessageContentAsync(chat)!;
        chat.AddAssistantMessage(reformulatedQuestion.Content!);

        // Ask using the embedding search via kernel memory and the reformulated question.
        var answer = await memory.AskAsync(reformulatedQuestion.Content!, minRelevance: 0.76);
        // var answer2 = await memory.AskAsync("what's the project timeline?", filter: new MemoryFilter().ByTag("user", "Blake"));

        if (answer.NoResult == false)
        {
            chat.AddUserMessage(question);
            chat.AddAssistantMessage(answer.Result);

            var response = new MemoryResponse(answer.Result, answer.RelevantSources);
            return response;
        }

        return null;
    }
}