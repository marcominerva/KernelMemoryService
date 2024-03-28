using KernelMemoryService.Extensions;
using KernelMemoryService.Models;
using Microsoft.KernelMemory;

namespace KernelMemoryService.Services;

public class ApplicationMemoryService(IKernelMemory memory, ChatService chatService)
{
    public async Task<string> ImportAsync(Stream content, string? name = null, string? documentId = null, IEnumerable<UploadTag>? tags = null, string? index = null)
    {
        documentId = await memory.ImportDocumentAsync(content, name, documentId, tags.ToTagCollection(), index);
        return documentId;
    }

    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(string documentId, string? index = null)
    {
        var status = await memory.GetDocumentStatusAsync(documentId, index);
        return status;
    }

    public async Task DeleteDocumentAsync(string documentId, string? index = null)
        => await memory.DeleteDocumentAsync(documentId, index);

    public async Task<MemoryResponse?> AskQuestionAsync(Question question, bool reformulate = true, double minimumRelevance = 0, string? index = null)
    {
        // Reformulate the following question taking into account the context of the chat to perform keyword search and embeddings:
        var reformulatedQuestion = reformulate ? await chatService.CreateQuestionAsync(question.ConversationId, question.Text) : question.Text;

        // Ask using the embedding search via Kernel Memory and the reformulated question.
        // If tags are provided, use them as filters with OR logic.
        var answer = await memory.AskAsync(reformulatedQuestion.TrimEnd([' ', '?']), index, filters: question.Tags.ToMemoryFilters(), minRelevance: minimumRelevance);

        // If you want to use an AND logic, set the "filter" parameter (instead of "filters").
        //var answer = await memory.AskAsync(reformulatedQuestion.TrimEnd([' ', '?'], index, filter: question.Tags.ToMemoryFilter(), minRelevance: minimumRelevance);

        if (answer.NoResult == false)
        {
            // If the answer has been found, add the interaction to the chat, so that it will be used for the next reformulation.
            await chatService.AddInteractionAsync(question.ConversationId, reformulatedQuestion, answer.Result);

            var response = new MemoryResponse(answer.Question, answer.Result, answer.RelevantSources);
            return response;
        }

        return null;
    }

    public async Task<SearchResult?> SearchAsync(Search search, double minimumRelevance = 0, string? index = null)
    {
        // Search using the embedding search via Kernel Memory .
        // If tags are provided, use them as filters with OR logic.
        var searchResult = await memory.SearchAsync(search.Text.TrimEnd([' ', '?']), index, filters: search.Tags.ToMemoryFilters(), minRelevance: minimumRelevance, limit: 50);

        // If you want to use an AND logic, set the "filter" parameter (instead of "filters").
        //var searchResult = await memory.SearchAsync(search.Text.TrimEnd([' ', '?']), index, filter: search.Tags.ToMemoryFilter(), minRelevance: minimumRelevance);

        return searchResult;
    }
}