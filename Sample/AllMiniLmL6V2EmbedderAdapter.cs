using Microsoft.Extensions.AI;
using SentenceTransformersCSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class AllMiniLmL6V2EmbedderAdapter : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly AllMiniLmL6V2Embedder _embedder;

    public AllMiniLmL6V2EmbedderAdapter(AllMiniLmL6V2Embedder embedder)
    {
        _embedder = embedder;
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();

        foreach (var value in values)
        {
            // 调用你的本地 embedder
            float[] embeddingVector = _embedder.GenerateEmbedding(value).ToArray();
            embeddings.Add(new Embedding<float>(embeddingVector));
        }

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        // 返回 null 表示不支持其他服务类型
        // 如果需要支持特定的服务，可以在这里添加逻辑
        return null;
    }

    public void Dispose()
    {
        // 如果有需要释放的资源，在这里处理
        _embedder?.Dispose();
    }
}

public static class EmbedderExtensions
{
    public static IEmbeddingGenerator<string, Embedding<float>> AsIEmbeddingGenerator(
        this AllMiniLmL6V2Embedder embedder)
    {
        return new AllMiniLmL6V2EmbedderAdapter(embedder);
    }
}