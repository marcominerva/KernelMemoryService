using KernelMemoryService.Models;
using KernelMemoryService.Services;
using KernelMemoryService.Settings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using MinimalHelpers.OpenApi;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add services to the container.
var aiSettings = builder.Configuration.GetSection<AzureOpenAISettings>("AzureOpenAI")!;
var appSettings = builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings))!;

builder.Services.AddMemoryCache();

builder.Services.AddKernelMemory(options =>
{
    options.WithAzureOpenAITextEmbeddingGeneration(new()
    {
        APIKey = aiSettings.Embedding.ApiKey,
        Deployment = aiSettings.Embedding.Deployment,
        Endpoint = aiSettings.Embedding.Endpoint,
        APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        MaxTokenTotal = aiSettings.Embedding.MaxTokens,
        MaxEmbeddingBatchSize = 16,
        //EmbeddingDimensions = 1536
    })
    .WithAzureOpenAITextGeneration(new()
    {
        APIKey = aiSettings.ChatCompletion.ApiKey,
        Deployment = aiSettings.ChatCompletion.Deployment,
        Endpoint = aiSettings.ChatCompletion.Endpoint,
        APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey,
        MaxTokenTotal = aiSettings.ChatCompletion.MaxTokens
    })
    .WithSearchClientConfig(new()
    {
        EmptyAnswer = "I'm sorry, I haven't found any relevant information that can be used to answer your question",
        MaxMatchesCount = 25,
        AnswerTokens = 800
    })
    .WithCustomTextPartitioningOptions(new()
    {
        MaxTokensPerParagraph = 1000,
        MaxTokensPerLine = 300,
        OverlappingTokens = 100
    })
    // Customize the pipeline to automatically delete files generated during the ingestion process.
    //.With(new KernelMemoryConfig
    //{
    //    DataIngestion = new KernelMemoryConfig.DataIngestionConfig
    //    {
    //        //MemoryDbUpsertBatchSize = 32,
    //        DefaultSteps = [.. Constants.DefaultPipeline, Constants.PipelineStepsDeleteGeneratedFiles]
    //    }
    //})
    .WithSimpleFileStorage(appSettings.StoragePath)
    .WithSimpleVectorDb(appSettings.VectorDbPath)
    // Configure the asynchronous memory.
    .WithSimpleQueuesPipeline(appSettings.QueuePath);
});

// Semantic Kernel is used to reformulate questions taking into account all the previous interactions, so that embeddings can be generated more accurately.
builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(aiSettings.ChatCompletion.Deployment, aiSettings.ChatCompletion.Endpoint, aiSettings.ChatCompletion.ApiKey);

builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ApplicationMemoryService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Kernel Memory Service API", Version = "v1" });

    options.AddDefaultResponse();
    options.MapType<UploadTag>(() => new() { Type = "string", Default = new OpenApiString("Name:Value") });
});

builder.Services.AddDefaultProblemDetails();
builder.Services.AddDefaultExceptionHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = string.Empty;
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kernel Memory Service API v1");
        options.InjectStylesheet("/css/swagger.css");
    });
}

var documentsApiGroup = app.MapGroup("/api/documents");

documentsApiGroup.MapPost(string.Empty, async (IFormFile file, ApplicationMemoryService memory, LinkGenerator linkGenerator, string? documentId = null, [FromQuery(Name = "tag")] UploadTag[]? tags = null, string? index = null) =>
{
    documentId = await memory.ImportAsync(file.OpenReadStream(), file.FileName, documentId, tags, index);
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
    operation.Parameter("tag").Description = "The tags to associate with the document. Use the format 'tagName=tagValue' to define a tag (i.e. ?tag=userId:42&tag=city:Taggia).";

    return operation;
});

documentsApiGroup.MapGet("{documentId}/status", async Task<Results<Ok<DataPipelineStatus>, NotFound>> (string documentId, ApplicationMemoryService memory, string? index = null) =>
{
    var status = await memory.GetDocumentStatusAsync(documentId, index);
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

documentsApiGroup.MapDelete("{documentId}", async (string documentId, ApplicationMemoryService memory, string? index = null) =>
{
    await memory.DeleteDocumentAsync(documentId, index);
    return TypedResults.NoContent();
})
.WithOpenApi(operation =>
{
    operation.Summary = "Delete a document from Kernel Memory";

    operation.Parameter("index").Description = "The index that contains the document. If not provided, the default index will be used ('default').";

    return operation;
});

app.MapPost("/api/search", async (Search search, ApplicationMemoryService memory, double minimumRelevance = 0, string? index = null) =>
{
    var response = await memory.SearchAsync(search, minimumRelevance, index);
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

app.MapPost("/api/ask", async Task<Results<Ok<MemoryResponse>, NotFound>> (Question question, ApplicationMemoryService memory, bool reformulate = true, double minimumRelevance = 0, string? index = null) =>
{
    var response = await memory.AskQuestionAsync(question, reformulate, minimumRelevance, index);
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

app.Run();