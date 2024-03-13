﻿using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.TextGeneration;
using Npgsql;

namespace YouTubeGPT.Shared;

public static class SemanticKernelExtensions
{
    private const int VectorSize = 1536;

    public static IHostApplicationBuilder AddSemanticKernel(this IHostApplicationBuilder builder)
    {
        builder.AddAzureOpenAIClient(ServiceNames.AzureOpenAI);

        builder.Services.AddSingleton(provider =>
        {
            var client = provider.GetRequiredService<OpenAIClient>();

            var chatCompletions = new AzureOpenAIChatCompletionService(builder.Configuration["Azure:AI:ChatDeploymentName"] ?? "gpt-35-turbo", client);
            return chatCompletions;
        });
        builder.Services.AddSingleton<IChatCompletionService>((provider) => provider.GetRequiredService<AzureOpenAIChatCompletionService>());
        builder.Services.AddSingleton<ITextGenerationService>((provider) => provider.GetRequiredService<AzureOpenAIChatCompletionService>());

        builder.Services.AddKernel();

        return builder;
    }

    public static IHostApplicationBuilder AddSemanticKernelMemory(this IHostApplicationBuilder builder)
    {
        builder.AddKeyedNpgsqlDataSource(ServiceNames.VectorDB, null, builder => builder.UseVector());

        builder.Services.AddScoped(provider =>
        {
            var client = provider.GetRequiredService<OpenAIClient>();
#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            ITextEmbeddingGenerationService embeddingGenerator =
                new AzureOpenAITextEmbeddingGenerationService(builder.Configuration["Azure:AI:EmbeddingDeploymentName"] ?? "text-embedding-ada-002", client);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            return embeddingGenerator;
        });

#pragma warning disable SKEXP0032 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.Services.AddScoped<IMemoryStore, PostgresMemoryStore>(provider =>
        {
            var dataSource = provider.GetRequiredKeyedService<NpgsqlDataSource>(ServiceNames.VectorDB);

            return new(dataSource, VectorSize);
        });
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0032 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        builder.Services.AddScoped(provider =>
        {
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var embeddingGenerator = provider.GetRequiredService<ITextEmbeddingGenerationService>();
            var memoryStore = provider.GetRequiredService<IMemoryStore>();

            var memory = new MemoryBuilder()
            .WithMemoryStore(memoryStore)
            .WithTextEmbeddingGeneration(embeddingGenerator)
            .Build();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            return memory;
        });

        return builder;
    }
}
