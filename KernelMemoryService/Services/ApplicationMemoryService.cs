using KernelMemoryService.Models;
using Microsoft.KernelMemory;

namespace KernelMemoryService.Services;

public class ApplicationMemoryService(IKernelMemory memory, ChatService chatService)
{
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

    public async Task<MemoryResponse?> AskQuestionAsync(Question question)
    {
        // Reformulate the following question taking into account the context of the chat to perform keyword search and embeddings:
        var reformulatedQuestion = await chatService.CreateQuestionAsync(question.ConversationId, question.Text);

        // Ask using the embedding search via kernel memory and the reformulated question.
        var answer = await memory.AskAsync(reformulatedQuestion, minRelevance: 0.76);
        // var answer2 = await memory.AskAsync("what's the project timeline?", filter: new MemoryFilter().ByTag("user", "Blake"));

        if (answer.NoResult == false)
        {
            await chatService.AddInteractionAsync(question.ConversationId, question.Text, answer.Result);

            var response = new MemoryResponse(answer.Result, answer.RelevantSources);
            return response;
        }

        return null;
    }
}