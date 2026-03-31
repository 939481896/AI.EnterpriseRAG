using System.Text.Json.Serialization;

namespace AI.EnterpriseRAG.Infrastructure.JsonContexts;

[JsonSerializable(typeof(float[]))]
[JsonSerializable(typeof(List<float[]>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<Dictionary<string, object>>))]
public partial class BaseJsonContext : JsonSerializerContext
{
}