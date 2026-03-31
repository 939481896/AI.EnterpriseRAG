using AI.EnterpriseRAG.Infrastructure.Services.VectorStores;
using System.Net;
using System.Text.Json.Serialization;

namespace AI.EnterpriseRAG.Infrastructure.JsonContexts;

[JsonSerializable(typeof(ChromaCreateCollectionRequest))]
[JsonSerializable(typeof(ChromaAddRecordsRequest))]
[JsonSerializable(typeof(ChromaQueryCollectionRequest))]
[JsonSerializable(typeof(ChromaQueryCollectionResponse))]
[JsonSerializable(typeof(ChromaDeleteRecordsRequest))]
[JsonSerializable(typeof(ChromaDeleteRecordsResponse))]
[JsonSerializable(typeof(ChromaGetResponse))]
public partial class ChromaJsonContext : JsonSerializerContext
{
}