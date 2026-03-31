using AI.EnterpriseRAG.Infrastructure.Services.VectorStores;
using System.Text.Json.Serialization;

namespace AI.EnterpriseRAG.Infrastructure.JsonContexts;

[JsonSerializable(typeof(QdrantCreateCollectionRequest))]
[JsonSerializable(typeof(QdrantVectorsConfig))]
[JsonSerializable(typeof(QdrantUpsertPointsRequest))]
[JsonSerializable(typeof(QdrantPoint))]
[JsonSerializable(typeof(QdrantSearchPointsRequest))]
[JsonSerializable(typeof(QdrantSearchResponse))]
[JsonSerializable(typeof(QdrantSearchResult))]
[JsonSerializable(typeof(QdrantDeletePointsRequest))]
[JsonSerializable(typeof(QdrantFilter))]
[JsonSerializable(typeof(QdrantCondition))]
[JsonSerializable(typeof(QdrantMatch))]
[JsonSerializable(typeof(QdrantRange))]
public partial class QdrantJsonContext : JsonSerializerContext
{
}