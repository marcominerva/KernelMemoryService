using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;

namespace KernelMemoryService.Extensions;

public static class KernelMemoryExtensions
{
    public static IKernelMemoryBuilder WithDefaultHostedHandlers(this IKernelMemoryBuilder kernelMemoryBuilder, IServiceCollection services)
    {
        services.AddHandlerAsHostedService<TextExtractionHandler>(Constants.PipelineStepsExtract);
        services.AddHandlerAsHostedService<TextPartitioningHandler>(Constants.PipelineStepsPartition);
        services.AddHandlerAsHostedService<GenerateEmbeddingsHandler>(Constants.PipelineStepsGenEmbeddings);
        services.AddHandlerAsHostedService<SaveRecordsHandler>(Constants.PipelineStepsSaveRecords);
        services.AddHandlerAsHostedService<SummarizationHandler>(Constants.PipelineStepsSummarize);
        services.AddHandlerAsHostedService<DeleteDocumentHandler>(Constants.PipelineStepsDeleteDocument);
        services.AddHandlerAsHostedService<DeleteIndexHandler>(Constants.PipelineStepsDeleteIndex);
        services.AddHandlerAsHostedService<DeleteGeneratedFilesHandler>(Constants.PipelineStepsDeleteGeneratedFiles);

        return kernelMemoryBuilder;
    }
}
