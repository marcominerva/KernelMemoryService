using KernelMemoryService.Models;
using KernelMemoryService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using MinimalHelpers.OpenApi;

namespace KernelMemoryService.Endpoints;

public class AskEndpoints : IEndpointRouteHandlerBuilder
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ask", async Task<Results<Ok<MemoryResponse>, NotFound>> (Question question, ApplicationMemoryService memory, CancellationToken cancellationToken, bool reformulate = true, double minimumRelevance = 0, string? index = null) =>
        {
            var response = await memory.AskQuestionAsync(question, reformulate, minimumRelevance, index, cancellationToken);
            if (response is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(response);
        })
        .WithOpenApi(operation =>
        {
            operation.Summary = "Ask a question to the Kernel Memory Service";
            operation.Description = "Ask a question to the Kernel Memory Service using the provided question and optional tags. The question will be reformulated taking into account the context of the chat identified by the given ConversationId. If tags are provided, they will be used as filters with OR logic.";

            operation.Parameter("reformulate").Description = "If true, the question will be reformulated taking into account the context of the chat identified by the given ConversationId.";
            operation.Parameter("minimumRelevance").Description = "The minimum Cosine Similarity required.";
            operation.Parameter("index").Description = "The index in which to search for documents. If not provided, the default index will be used ('default').";

            return operation;
        });
    }
}
