using System.Text.Json.Serialization;

namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    // =========================
    // generateContent DTOs (Text-only)
    // =========================

    public sealed class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("generationConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    public sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    public sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate> Candidates { get; set; } = new();
    }

    public sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();

        // optional, hilft beim Debuggen (MAX_TOKENS etc.)
        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }

    // ThinkingConfig (wichtig für Gemini 2.5 gegen "thoughtsTokenCount" / MAX_TOKENS)
    public sealed class GeminiThinkingConfig
    {
        // 0 = Thinking komplett aus (spart Token-Budget und verhindert Truncation durch Thoughts)
        [JsonPropertyName("thinkingBudget")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ThinkingBudget { get; set; }

        // Ob Thoughts zurückgegeben werden sollen (meist false / weglassen)
        [JsonPropertyName("includeThoughts")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IncludeThoughts { get; set; }
    }

    public sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResponseMimeType { get; set; } // "application/json"

        [JsonPropertyName("maxOutputTokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxOutputTokens { get; set; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        // (Optional) OpenAPI-Subset schema (restriktiv)
        [JsonPropertyName("responseSchema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? ResponseSchema { get; set; }

        // JSON Schema (hier darf additionalProperties, anyOf, etc.)
        [JsonPropertyName("responseJsonSchema")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? ResponseJsonSchema { get; set; }

        // Thinking Config für Gemini 2.5
        [JsonPropertyName("thinkingConfig")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiThinkingConfig? ThinkingConfig { get; set; }
    }

    // =========================
    // embedContent DTOs
    // =========================

    public sealed class GeminiEmbedRequest
    {
        // embedContent erwartet "content" (singular), NICHT "contents"
        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();

        // z.B. "RETRIEVAL_DOCUMENT"
        [JsonPropertyName("taskType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TaskType { get; set; } = "RETRIEVAL_DOCUMENT";

        // optional: gewünschte Dimensionszahl
        [JsonPropertyName("outputDimensionality")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? OutputDimensionality { get; set; }
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

