using KernelMemoryService.Extensions;
using KernelMemoryService.Models;
using Microsoft.KernelMemory;

namespace KernelMemoryService.Services;

public class ApplicationMemoryService(IKernelMemory memory, ChatService chatService)
{
    public async Task<string> ImportAsync(Stream content, string? name = null, string? documentId = null, IEnumerable<UploadTag>? tags = null, string? index = null, CancellationToken cancellationToken = default)
    {
        documentId = await memory.ImportDocumentAsync(content, name, documentId, tags.ToTagCollection(), index, cancellationToken: cancellationToken);
        return documentId;
    }

    public async Task<DataPipelineStatus?> GetDocumentStatusAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
    {
        var status = await memory.GetDocumentStatusAsync(documentId, index, cancellationToken);
        return status;
    }

    public async Task DeleteDocumentAsync(string documentId, string? index = null, CancellationToken cancellationToken = default)
        => await memory.DeleteDocumentAsync(documentId, index, cancellationToken);

    public async Task<MemoryResponse?> AskQuestionAsync(Question question, bool reformulate = true, double minimumRelevance = 0, string? index = null, CancellationToken cancellationToken = default)
    {
        // Reformulate the following question taking into account the context of the chat to perform keyword search and embeddings:
        var reformulatedQuestion = reformulate ? await chatService.CreateQuestionAsync(question.ConversationId, question.Text, cancellationToken) : question.Text;

        // Ask using the embedding search via Kernel Memory and the reformulated question.
        // If tags are provided, use them as filters with OR logic.
        var answer = await memory.AskAsync(reformulatedQuestion, index, filters: question.Tags.ToMemoryFilters(), minRelevance: minimumRelevance, cancellationToken: cancellationToken);

        // If you want to use an AND logic, set the "filter" parameter (instead of "filters").
        //var answer = await memory.AskAsync(reformulatedQuestion, index, filter: question.Tags.ToMemoryFilter(), minRelevance: minimumRelevance, cancellationToken: cancellationToken);

        if (answer.NoResult != false)
        {
            return null;
        }

        // The answer has been found: add the interaction to the chat, so that it will be used for the next reformulation.
        await chatService.AddInteractionAsync(question.ConversationId, reformulatedQuestion, answer.Result, cancellationToken);

        var response = new MemoryResponse(answer.Question, answer.Result, answer.RelevantSources);
        return response;
    }

    public async Task<SearchResult?> SearchAsync(Search search, double minimumRelevance = 0, string? index = null, CancellationToken cancellationToken = default)
    {
        // Search using the embedding search via Kernel Memory.
        // If tags are provided, use them as filters with OR logic.
        var searchResult = await memory.SearchAsync(search.Text.TrimEnd([' ', '?']), index, filters: search.Tags.ToMemoryFilters(), minRelevance: minimumRelevance, limit: 50, cancellationToken: cancellationToken);

        // If you want to use an AND logic, set the "filter" parameter (instead of "filters").
        //var searchResult = await memory.SearchAsync(search.Text.TrimEnd([' ', '?']), index, filter: search.Tags.ToMemoryFilter(), minRelevance: minimumRelevance, limit: 50, cancellationToken: cancellationToken);

        return searchResult;
    }
}