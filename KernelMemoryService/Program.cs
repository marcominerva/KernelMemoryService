using KernelMemoryService.Models;
using KernelMemoryService.Services;
using KernelMemoryService.Settings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.KernelMemory;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using MinimalHelpers.OpenApi;
using TinyHelpers.AspNetCore.ExceptionHandlers;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add services to the container.
var settings = builder.Configuration.GetSection<AzureOpenAISettings>("AzureOpenAI")!;
builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings));

builder.Services.AddMemoryCache();

var kernelBuilder = builder.Services.AddKernel()
    .AddAzureOpenAIChatCompletion(settings.ChatCompletion.Deployment, settings.ChatCompletion.Endpoint, settings.ChatCompletion.ApiKey);

var kernelMemory = new KernelMemoryBuilder(builder.Services)
    .WithAzureOpenAITextGeneration(new()
    {
        APIKey = settings.ChatCompletion.ApiKey,
        Deployment = settings.ChatCompletion.Deployment,
        Endpoint = settings.ChatCompletion.Endpoint,
        APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey
    })
    .WithAzureOpenAITextEmbeddingGeneration(new()
    {
        APIKey = settings.Embedding.ApiKey,
        Deployment = settings.Embedding.Deployment,
        Endpoint = settings.Embedding.Endpoint,
        APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
        Auth = AzureOpenAIConfig.AuthTypes.APIKey,
    })
    .WithSimpleFileStorage(@"D:\Memory\_files")
    .WithSimpleVectorDb(@"D:\Memory\_vectors")
    .WithSearchClientConfig(new()
    {
        EmptyAnswer = "I'm sorry, I haven't found any relevant information that can be used to answer your question",
        MaxMatchesCount = 10,
        AnswerTokens = 800
    })
    .WithCustomTextPartitioningOptions(new()
    {
        MaxTokensPerParagraph = 1000,
        MaxTokensPerLine = 300,
        OverlappingTokens = 100
    })
    // Configure the asynchronous memory.    
    .WithSimpleQueuesPipeline(@"D:\Memory\_queues")
    .Build<MemoryService>();  // Asynchronous memory with pipelines.

builder.Services.AddSingleton<IKernelMemory>(kernelMemory);

builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ApplicationMemoryService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Kernel Memory Service API", Version = "v1" });

    options.AddDefaultResponse();
    options.AddFormFile();
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

documentsApiGroup.MapPost("upload", async (IFormFile file, ApplicationMemoryService memory, LinkGenerator linkGenerator, string? documentId = null) =>
{
    documentId = await memory.ImportAsync(file.OpenReadStream(), file.FileName, documentId);
    var uri = linkGenerator.GetPathByName("GetDocumentStatus", new { documentId });
    return TypedResults.Accepted(uri, new UploadDocumentResponse(documentId));
})
.DisableAntiforgery()
.WithOpenApi();

documentsApiGroup.MapGet("{documentId}/status", async Task<Results<Ok<DataPipelineStatus>, NotFound>> (string documentId, ApplicationMemoryService memory) =>
{
    var status = await memory.GetDocumentStatusAsync(documentId);
    if (status is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(status);
})
.WithName("GetDocumentStatus")
.WithOpenApi();

documentsApiGroup.MapDelete("{documentId}", async (string documentId, ApplicationMemoryService memory) =>
{
    await memory.DeleteDocumentAsync(documentId);
    return TypedResults.NoContent();
})
.WithOpenApi();

documentsApiGroup.MapPost("ask", async Task<Results<Ok<MemoryResponse>, NotFound>> (Question question, ApplicationMemoryService memory) =>
{
    var response = await memory.AskQuestionAsync(question);
    if (response is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(response);
})
.WithOpenApi();

app.Run();