using KernelMemoryService.Models;
using Microsoft.KernelMemory;

namespace KernelMemoryService.Services;

public class ApplicationMemoryService(IKernelMemory memory, ChatService chatService)
{
    public async Task<string> ImportAsync(Stream content, string? name = null, string? documentId = null, string? index = null)
    {
        documentId = await memory.ImportDocumentAsync(content, name, documentId, index: index);
        return documentId;
    }

    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(string documentId, string? index = null)
    {
        var status = await memory.GetDocumentStatusAsync(documentId, index);
        return status;
    }

    public async Task DeleteDocumentAsync(string documentId, string? index = null)
        => await memory.DeleteDocumentAsync(documentId, index);

    public async Task<MemoryResponse?> AskQuestionAsync(Question question, string? index = null)
    {
        // Reformulate the following question taking into account the context of the chat to perform keyword search and embeddings:
        var reformulatedQuestion = await chatService.CreateQuestionAsync(question.ConversationId, question.Text);

        // Ask using the embedding search via kernel memory and the reformulated question.
        var answer = await memory.AskAsync(reformulatedQuestion, index, minRelevance: 0.76);

        if (answer.NoResult == false)
        {
            await chatService.AddInteractionAsync(question.ConversationId, question.Text, answer.Result);

            var response = new MemoryResponse(answer.Result, answer.RelevantSources);
            return response;
        }

        return null;
    }
}