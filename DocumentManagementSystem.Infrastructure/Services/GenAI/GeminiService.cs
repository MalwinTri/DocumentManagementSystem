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

        private static readonly JsonSerializerOptions JsonReadOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

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

            // Wichtig: Gemini 2.5 produziert sonst "thoughtsTokenCount" und frisst dein Token-Budget
            // -> führt zu finishReason MAX_TOKENS obwohl sichtbarer Text kurz ist.
            // Lösung: thinkingBudget = 0
            var prompt =
                "Erstelle eine verständliche Zusammenfassung dieses Dokuments.\n" +
                "Format (genau so ausgeben):\n" +
                "ZEILE 1: Ein klarer Satz, worum es geht.\n" +
                "DANACH: 4 bis 6 Stichpunkte mit den wichtigsten Inhalten.\n" +
                "Regeln:\n" +
                "- Schreibe konkret, nenne relevante Begriffe/Funktionen/Technologien aus dem Text.\n" +
                "- Keine Klammern.\n" +
                "- Keine Einleitung wie \"Dieses Dokument...\" wenn möglich.\n" +
                "- Kein Markdown, nur Klartext.\n\n" +
                "TEXT:\n" + Clip(text, 10000);

            // 1) normaler Versuch
            var s = await GenerateSummaryInternalAsync(prompt, maxOutputTokens: 700, cancellationToken);
            if (!string.IsNullOrWhiteSpace(s))
                return s;

            // 2) Fallback (falls doch truncation / komisches Format)
            return await GenerateSummaryInternalAsync(prompt, maxOutputTokens: 1200, cancellationToken);
        }

        private async Task<string?> GenerateSummaryInternalAsync(string prompt, int maxOutputTokens, CancellationToken ct)
        {
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
                    MaxOutputTokens = maxOutputTokens,

                    // KEY-FIX: Thinking aus (verhindert MAX_TOKENS durch thoughtsTokenCount)
                    ThinkingConfig = new GeminiThinkingConfig
                    {
                        ThinkingBudget = 0,
                        IncludeThoughts = false
                    }
                }
            };

            var url = $"{_options.BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";

            using var response = await PostJsonAsync(url, request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == (HttpStatusCode)429)
                throw new AiRateLimitException(TryParseRetryDelay(body) ?? TimeSpan.FromSeconds(60), body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini Summary returned {StatusCode}: {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Gemini Summary failed: {(int)response.StatusCode} {response.StatusCode}");
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(body, JsonReadOpts);

            var finishReason = geminiResponse?.Candidates?.FirstOrDefault()?.FinishReason;
            if (string.Equals(finishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Gemini Summary hit MAX_TOKENS (truncated).");
                return null; // -> caller macht fallback mit mehr Tokens
            }

            var summaryText = ExtractTextFromResponse(geminiResponse);
            summaryText = NormalizeSummary(summaryText);

            // kleine Format-Stabilisierung (falls Modell keine Bulletpoints liefert)
            summaryText = ForceSummaryShape(summaryText);

            return string.IsNullOrWhiteSpace(summaryText) ? null : summaryText;
        }

        public async Task<AiExtractionResult?> ExtractMetadataAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var prompt =
                "Extrahiere strukturierte Metadaten aus dem Text.\n" +
                "Gib NUR ein gültiges JSON-Objekt zurück (kein Markdown, keine Erklärungen).\n" +
                "Regeln:\n" +
                "- persons: max 8\n" +
                "- organizations: max 8\n" +
                "- locations: max 8\n" +
                "- keywords: max 12\n" +
                "- Strings kurz halten (max ~80 Zeichen)\n" +
                "- Wenn etwas fehlt: null oder []\n\n" +
                "TEXT:\n" + Clip(text, 10000);

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
                    ResponseJsonSchema = BuildExtractionSchema(),
                    Temperature = 0.0,
                    MaxOutputTokens = 2048,

                    // Auch hier: Thinking aus, damit JSON nicht wegen thoughts gekappt wird
                    ThinkingConfig = new GeminiThinkingConfig
                    {
                        ThinkingBudget = 0,
                        IncludeThoughts = false
                    }
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

            var finishReason = geminiResponse?.Candidates?.FirstOrDefault()?.FinishReason;
            if (string.Equals(finishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Gemini ExtractMetadata hit MAX_TOKENS (JSON likely truncated).");
                throw new InvalidOperationException("Gemini ExtractMetadata output was truncated (MAX_TOKENS)");
            }

            var jsonText = ExtractJsonFromResponse(geminiResponse);

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("Gemini ExtractMetadata returned no JSON (or could not be parsed). Raw body: {Body}", body);
                throw new InvalidOperationException("Gemini ExtractMetadata returned empty JSON");
            }

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
            var content = JsonContent.Create(payload, options: JsonSendOpts);
            return await _httpClient.PostAsync(url, content, ct);
        }

        private static string Clip(string s, int maxChars)
        {
            s = (s ?? "").Trim();
            return s.Length <= maxChars ? s : s[..maxChars];
        }

        private static string NormalizeSummary(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s ?? "";
            s = s.Trim();

            // Klammern komplett entfernen
            s = s.Replace("(", "").Replace(")", "");

            // Mehrfachspaces bereinigen
            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            return s;
        }

        private static string ForceSummaryShape(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s ?? "";

            var lines = s.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            if (lines.Count == 0) return s;

            // Wenn keine Bulletpoints vorhanden sind, erzwinge welche (robust bei OCR/Random Output)
            var hasBullets = lines.Skip(1).Any(l => l.StartsWith("- ") || l.StartsWith("• "));
            if (!hasBullets && lines.Count > 1)
            {
                for (int i = 1; i < lines.Count; i++)
                    lines[i] = "- " + lines[i].TrimStart('-', '•', ' ').Trim();
            }

            // Limit: max 1 Satz + 6 bullets
            var first = lines[0];
            var bullets = lines.Skip(1).Take(6).ToList();
            var result = new List<string> { first };
            result.AddRange(bullets);

            return string.Join("\n", result).Trim();
        }

        // ---- Response parsing helpers ----

        private static string? ExtractTextFromResponse(GeminiResponse? resp)
        {
            var parts = resp?.Candidates?.FirstOrDefault()?.Content?.Parts;
            if (parts == null || parts.Count == 0) return null;

            var combined = string.Join("\n",
                parts.Select(p => p.Text).Where(t => !string.IsNullOrWhiteSpace(t)));

            return string.IsNullOrWhiteSpace(combined) ? null : combined;
        }

        private static string? ExtractJsonFromResponse(GeminiResponse? resp)
        {
            var parts = resp?.Candidates?.FirstOrDefault()?.Content?.Parts;
            if (parts == null || parts.Count == 0) return null;

            var combinedText = string.Join("\n",
                parts.Select(p => p.Text).Where(t => !string.IsNullOrWhiteSpace(t)));

            if (string.IsNullOrWhiteSpace(combinedText))
                return null;

            return ExtractJsonObject(combinedText);
        }

        private static string? ExtractJsonObject(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();

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
                        var s = rd.GetString();
                        if (!string.IsNullOrWhiteSpace(s) && s.EndsWith("s") &&
                            int.TryParse(s.TrimEnd('s'), out var sec))
                            return TimeSpan.FromSeconds(sec);
                    }
                }
            }
            catch { }

            return null;
        }

        private static object BuildExtractionSchema()
        {
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
                    invoiceNumber = new { anyOf = new object[] { new { type = "string", maxLength = 80 }, new { type = "null" } } },
                    iban = new { anyOf = new object[] { new { type = "string", maxLength = 80 }, new { type = "null" } } },

                    persons = new
                    {
                        type = "array",
                        maxItems = 8,
                        items = new { type = "string", maxLength = 80 }
                    },
                    organizations = new
                    {
                        type = "array",
                        maxItems = 8,
                        items = new { type = "string", maxLength = 80 }
                    },
                    locations = new
                    {
                        type = "array",
                        maxItems = 8,
                        items = new { type = "string", maxLength = 80 }
                    },
                    keywords = new
                    {
                        type = "array",
                        maxItems = 12,
                        items = new { type = "string", maxLength = 60 }
                    }
                },
                required = new[] { "documentType", "persons", "organizations", "locations", "keywords" },
                additionalProperties = false
            };
        }
    }
}
