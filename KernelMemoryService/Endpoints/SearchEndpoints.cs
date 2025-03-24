using KernelMemoryService.Models;
using KernelMemoryService.Services;
using MinimalHelpers.OpenApi;

namespace KernelMemoryService.Endpoints;

public class SearchEndpoints : IEndpointRouteHandlerBuilder
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/search", async (Search search, ApplicationMemoryService memory, CancellationToken cancellationToken, double minimumRelevance = 0, string? index = null) =>
        {
            var response = await memory.SearchAsync(search, minimumRelevance, index, cancellationToken);
            return TypedResults.Ok(response);
        })
        .WithOpenApi(operation =>
        {
            operation.Summary = "Search into Kernel Memory";
            operation.Description = "Search into Kernel Memory using the provided question and optional tags. If tags are provided, they will be used as filters with OR logic.";

            operation.Parameter("minimumRelevance").Description = "The minimum Cosine Similarity required.";
            operation.Parameter("index").Description = "The index in which to search for documents. If not provided, the default index will be used ('default').";

            return operation;
        });
    }
}
