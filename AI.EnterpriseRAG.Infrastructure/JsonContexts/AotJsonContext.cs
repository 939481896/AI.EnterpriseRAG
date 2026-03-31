
using AI.EnterpriseRAG.Infrastructure.Services;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using AI.EnterpriseRAG.Infrastructure.Services.VectorStores;
using System.Text.Json.Serialization;

namespace AI.EnterpriseRAG.Infrastructure;

// ========================
// 正确 AOT 源生成上下文
// ========================
[JsonSerializable(typeof(QdrantCreateCollectionRequest))]
[JsonSerializable(typeof(QdrantUpsertPointsRequest))]
[JsonSerializable(typeof(QdrantSearchPointsRequest))]
[JsonSerializable(typeof(QdrantSearchResponse))]
[JsonSerializable(typeof(QdrantDeletePointsRequest))]

[JsonSerializable(typeof(ChromaCreateCollectionRequest))]
[JsonSerializable(typeof(ChromaAddRecordsRequest))]
[JsonSerializable(typeof(ChromaQueryCollectionRequest))]
[JsonSerializable(typeof(ChromaQueryCollectionResponse))]
[JsonSerializable(typeof(ChromaDeleteRecordsRequest))]
[JsonSerializable(typeof(ChromaGetResponse))]

[JsonSerializable(typeof(UnstructuredResponse))]
[JsonSerializable(typeof(List<UnstructuredChunk>))]

[JsonSerializable(typeof(float[]))]
public partial class AotJsonContext : JsonSerializerContext
{
}