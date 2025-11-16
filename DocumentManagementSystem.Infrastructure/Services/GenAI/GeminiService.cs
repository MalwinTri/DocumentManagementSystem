using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public class GeminiService : IGenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiService> _logger;

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

            var prompt = $"Fasse den folgenden Text kurz und verständlich in ein paar Sätzen zusammen, nicht länger als 25 Wörter:\n\n{text}";

            var request = new GeminiRequest
            {
                Contents =
            {
                new GeminiContent
                {
                    Parts = { new GeminiPart { Text = prompt } }
                }
            }
            };

            var url = $"{_options.BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";

            try
            {
                _logger.LogInformation("Sending text to Gemini for summarization (length: {Length})", text.Length);

                using var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
                    return null; // or throw a custom exception
                }

                var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);

                var summary = geminiResponse?
                    .Candidates?
                    .FirstOrDefault()?
                    .Content?
                    .Parts?
                    .FirstOrDefault()?
                    .Text;

                _logger.LogInformation("Received summary from Gemini (length: {Length})", summary?.Length ?? 0);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return null; // or throw
            }
        }
    }
}
