using KernelMemoryService.Models;
using KernelMemoryService.Services;
using KernelMemoryService.Settings;
using Microsoft.KernelMemory;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using TinyHelpers.AspNetCore.Extensions;
using TinyHelpers.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// Add services to the container.
var aiSettings = builder.Configuration.GetSection<AzureOpenAISettings>("AzureOpenAI")!;
var appSettings = builder.Services.ConfigureAndGet<AppSettings>(builder.Configuration, nameof(AppSettings))!;

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new()
    {
        LocalCacheExpiration = appSettings.MessageExpiration
    };
});

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

builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<ApplicationMemoryService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Kernel Memory Service API", Version = "v1" });

    options.AddDefaultProblemDetailsResponse();
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
    });
}

app.MapEndpoints();
app.Run();