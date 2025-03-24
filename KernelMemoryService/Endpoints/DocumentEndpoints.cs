using KernelMemoryService.Models;
using KernelMemoryService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using MinimalHelpers.OpenApi;

namespace KernelMemoryService.Endpoints;

public class DocumentEndpoints : IEndpointRouteHandlerBuilder
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var documentsApiGroup = endpoints.MapGroup("/api/documents");

        documentsApiGroup.MapPost(string.Empty, async (IFormFile file, ApplicationMemoryService memory, LinkGenerator linkGenerator, CancellationToken cancellationToken, string? documentId = null, [FromQuery(Name = "tag")] UploadTag[]? tags = null, string? index = null) =>
        {
            documentId = await memory.ImportAsync(file.OpenReadStream(), file.FileName, documentId, tags, index, cancellationToken);
            var uri = linkGenerator.GetPathByName("GetDocumentStatus", new { documentId });
            return TypedResults.Accepted(uri, new UploadDocumentResponse(documentId));
        })
        .DisableAntiforgery()
        .WithOpenApi(operation =>
        {
            operation.Summary = "Upload a document to Kernel Memory";
            operation.Description = "Upload a document to Kernel Memory. The document will be indexed and used to answer questions. The documentId is optional, if not provided a new one will be generated. If you specify an existing documentId, the document will be overridden. You can also specify tags to associate with the document.";

            operation.Parameter("documentId").Description = "The unique identifier of the document. If not provided, a new one will be generated. If you specify an existing documentId, the document will be overridden.";
            operation.Parameter("index").Description = "The index to use for the document. If not provided, the default index will be used ('default').";
            operation.Parameter("tag").Description = "The tags to associate with the document. Use the format 'tagName:tagValue' to define a tag (i.e. userId:42).";

            return operation;
        });

        documentsApiGroup.MapGet("{documentId}/status", async Task<Results<Ok<DataPipelineStatus>, NotFound>> (string documentId, ApplicationMemoryService memory, CancellationToken cancellationToken, string? index = null) =>
        {
            var status = await memory.GetDocumentStatusAsync(documentId, index, cancellationToken);
            if (status is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(status);
        })
        .WithName("GetDocumentStatus")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get the ingestion status of a document";

            operation.Parameter("index").Description = "The index that contains the document. If not provided, the default index will be used ('default').";

            return operation;
        });

        documentsApiGroup.MapDelete("{documentId}", async (string documentId, ApplicationMemoryService memory, CancellationToken cancellationToken, string? index = null) =>
        {
            await memory.DeleteDocumentAsync(documentId, index, cancellationToken);
            return TypedResults.NoContent();
        })
        .WithOpenApi(operation =>
        {
            operation.Summary = "Delete a document from Kernel Memory";

            operation.Parameter("index").Description = "The index that contains the document. If not provided, the default index will be used ('default').";

            return operation;
        });
    }
}
