using System.Text.Json.Serialization;

namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    // generateContent config
    public sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResponseMimeType { get; set; }     // "application/json"

        [JsonPropertyName("maxOutputTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxOutputTokens { get; set; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        // ✅ RICHTIG: responseSchema (kein _responseJsonSchema)
        [JsonPropertyName("responseSchema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? ResponseSchema { get; set; }
    }

    // embedContent request
    public sealed class GeminiEmbedRequest
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();

        [JsonPropertyName("taskType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TaskType { get; set; } = "RETRIEVAL_DOCUMENT";

        [JsonPropertyName("outputDimensionality")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? OutputDimensionality { get; set; } = null;
    }

    public sealed class GeminiEmbedResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbedding? Embedding { get; set; }
    }

    public sealed class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public List<float> Values { get; set; } = new();
    }
}
