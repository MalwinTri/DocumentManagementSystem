using DocumentManagementSystem.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public class GeminiService : IGenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiService> _logger;

        // fürs Parsen (Responses)
        private static readonly JsonSerializerOptions JsonReadOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        // fürs Senden (Nulls NICHT mitsenden)
        private static readonly JsonSerializerOptions JsonSendOpts = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public GeminiService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var prompt =
                "Fasse den folgenden Text kurz und verständlich zusammen (max. 25 Wörter, 1-2 Sätze).\n\n" +
                Clip(text, 12000);

            var request = new GeminiRequest
            {
                Contents =
                {
                    new GeminiContent
                    {
                        Parts = { new GeminiPart { Text = prompt } }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.2,
                    MaxOutputTokens = 250
                    // KEIN ResponseSchema setzen (und dank JsonIgnore wird auch nichts Null mitgesendet)
                }
            };

            var url = $"{_options.BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";

            using var response = await PostJsonAsync(url, request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // 429 sauber behandeln → Worker kann Backoff speichern
            if (response.StatusCode == (HttpStatusCode)429)
                throw new AiRateLimitException(TryParseRetryDelay(body) ?? TimeSpan.FromSeconds(60), body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini Summary returned {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Gemini Summary failed: {(int)response.StatusCode} {response.StatusCode}");
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(body, JsonReadOpts);

            var summary = geminiResponse?
                .Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .FirstOrDefault()?
                .Text;

            summary = summary?.Trim();

            // NICHT einfach null returnen, sonst spammt dein Worker wieder sofort.
            if (string.IsNullOrWhiteSpace(summary))
                throw new InvalidOperationException("Gemini Summary returned empty text");

            return summary;
        }

        public async Task<AiExtractionResult?> ExtractMetadataAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var prompt =
                "Extrahiere strukturierte Metadaten aus dem Text. " +
                "Gib NUR JSON zurück (kein Markdown, keine Erklärungen). " +
                "Wenn etwas fehlt: null oder leere Liste.\n\nTEXT:\n" +
                Clip(text, 18000);

            var request = new GeminiRequest
            {
                Contents =
                {
                    new GeminiContent
                    {
                        Parts = { new GeminiPart { Text = prompt } }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseSchema = BuildExtractionSchema(), 
                    Temperature = 0.0,
                    MaxOutputTokens = 900
                }
            };

            var url = $"{_options.BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";

            using var response = await PostJsonAsync(url, request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == (HttpStatusCode)429)
                throw new AiRateLimitException(TryParseRetryDelay(body) ?? TimeSpan.FromSeconds(60), body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini ExtractMetadata returned {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Gemini ExtractMetadata failed: {(int)response.StatusCode} {response.StatusCode}");
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(body, JsonReadOpts);

            var jsonText = geminiResponse?
                .Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .FirstOrDefault()?
                .Text;

            jsonText = ExtractJsonObject(jsonText);

            if (string.IsNullOrWhiteSpace(jsonText))
                throw new InvalidOperationException("Gemini ExtractMetadata returned empty JSON");

            var result = JsonSerializer.Deserialize<AiExtractionResult>(jsonText, JsonReadOpts)
                         ?? throw new InvalidOperationException("Gemini ExtractMetadata JSON could not be parsed");

            result.RawJson = jsonText;
            return result;
        }

        public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var req = new GeminiEmbedRequest
            {
                Content = new GeminiContent
                {
                    Parts = { new GeminiPart { Text = Clip(text, 20000) } }
                },
                TaskType = "RETRIEVAL_DOCUMENT",
                OutputDimensionality = null
            };

            var url = $"{_options.BaseUrl}/{_options.EmbeddingModel}:embedContent?key={_options.ApiKey}";

            using var response = await PostJsonAsync(url, req, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == (HttpStatusCode)429)
                throw new AiRateLimitException(TryParseRetryDelay(body) ?? TimeSpan.FromSeconds(60), body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini Embed returned {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Gemini Embed failed: {(int)response.StatusCode} {response.StatusCode}");
            }

            var parsed = JsonSerializer.Deserialize<GeminiEmbedResponse>(body, JsonReadOpts);
            var values = parsed?.Embedding?.Values;

            if (values == null || values.Count == 0)
                throw new InvalidOperationException("Gemini Embed returned no values");

            return values.ToArray();
        }

        private async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T payload, CancellationToken ct)
        {
            // sorgt dafür, dass null-Felder NICHT gesendet werden (zusätzlich zu JsonIgnore)
            var content = JsonContent.Create(payload, options: JsonSendOpts);
            return await _httpClient.PostAsync(url, content, ct);
        }

        private static string Clip(string s, int maxChars)
        {
            s = (s ?? "").Trim();
            return s.Length <= maxChars ? s : s[..maxChars];
        }

        // Falls Gemini doch ```json ...``` liefert → JSON sauber rausziehen
        private static string? ExtractJsonObject(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();

            // code fences entfernen
            if (s.StartsWith("```"))
            {
                var firstNewline = s.IndexOf('\n');
                if (firstNewline >= 0) s = s[(firstNewline + 1)..];
                var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0) s = s[..lastFence];
                s = s.Trim();
            }

            var start = s.IndexOf('{');
            var end = s.LastIndexOf('}');
            if (start < 0 || end < 0 || end <= start) return null;

            return s.Substring(start, end - start + 1).Trim();
        }

        // 429 body enthält oft retryDelay: "49s"
        private static TimeSpan? TryParseRetryDelay(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("error", out var err)) return null;
                if (!err.TryGetProperty("details", out var details)) return null;

                foreach (var d in details.EnumerateArray())
                {
                    if (d.TryGetProperty("@type", out var type) &&
                        type.GetString()?.EndsWith("RetryInfo") == true &&
                        d.TryGetProperty("retryDelay", out var rd))
                    {
                        var s = rd.GetString(); // "49s"
                        if (!string.IsNullOrWhiteSpace(s) && s.EndsWith("s") &&
                            int.TryParse(s.TrimEnd('s'), out var sec))
                            return TimeSpan.FromSeconds(sec);
                    }
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private static object BuildExtractionSchema()
        {
            // JSON Schema Objekt (simple)
            return new
            {
                type = "object",
                properties = new
                {
                    documentType = new { type = "string", @enum = new[] { "INVOICE", "CONTRACT", "REMINDER", "OTHER" } },
                    issueDate = new
                    {
                        anyOf = new object[]
                        {
                            new { type = "string", format = "date" },
                            new { type = "null" }
                        }
                    },
                    invoiceNumber = new { anyOf = new object[] { new { type = "string" }, new { type = "null" } } },
                    iban = new { anyOf = new object[] { new { type = "string" }, new { type = "null" } } },
                    persons = new { type = "array", items = new { type = "string" }, maxItems = 20 },
                    organizations = new { type = "array", items = new { type = "string" }, maxItems = 20 },
                    locations = new { type = "array", items = new { type = "string" }, maxItems = 20 },
                    keywords = new { type = "array", items = new { type = "string" }, maxItems = 20 }
                },
                required = new[] { "documentType", "keywords" },
                additionalProperties = false
            };
        }
    }
}
