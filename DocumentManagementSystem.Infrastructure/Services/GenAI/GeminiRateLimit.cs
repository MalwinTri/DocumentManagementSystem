using DocumentManagementSystem.Infrastructure.Exceptions;
using System.Net;
using System.Text.Json;

namespace DocumentManagementSystem.Infrastructure.Services.GenAI;

internal static class GeminiRateLimit
{
    public static async Task ThrowIfRateLimitedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode != (HttpStatusCode)429)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);

        // 1) Wenn Daily quota: bis nächste UTC-Nacht warten (damit es nicht dauernd failt)
        var untilNextDay = TryParseDailyQuotaBackoff(body);
        if (untilNextDay != null)
            throw new AiRateLimitException(untilNextDay.Value, body);

        // 2) sonst: RetryInfo retryDelay (z.B. "49s") oder Retry-After Header
        var retryAfter = TryParseRetryDelay(body)
                         ?? TryParseRetryAfterHeader(response)
                         ?? TimeSpan.FromSeconds(60);

        // mindestens 10s (kleiner Schutz)
        if (retryAfter < TimeSpan.FromSeconds(10))
            retryAfter = TimeSpan.FromSeconds(10);

        throw new AiRateLimitException(retryAfter, body);
    }

    private static TimeSpan? TryParseRetryAfterHeader(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var v = values.FirstOrDefault();
            if (int.TryParse(v, out var seconds))
                return TimeSpan.FromSeconds(seconds);
        }
        return null;
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
                    var s = rd.GetString(); // "49s"
                    if (s != null && s.EndsWith("s") && int.TryParse(s.TrimEnd('s'), out var sec))
                        return TimeSpan.FromSeconds(sec);
                }
            }
        }
        catch { }

        return null;
    }

    private static TimeSpan? TryParseDailyQuotaBackoff(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("error", out var err)) return null;
            if (!err.TryGetProperty("details", out var details)) return null;

            foreach (var d in details.EnumerateArray())
            {
                if (d.TryGetProperty("@type", out var type) &&
                    type.GetString()?.EndsWith("QuotaFailure") == true &&
                    d.TryGetProperty("violations", out var violations))
                {
                    foreach (var v in violations.EnumerateArray())
                    {
                        if (v.TryGetProperty("quotaId", out var quotaIdEl))
                        {
                            var quotaId = quotaIdEl.GetString() ?? "";
                            if (quotaId.Contains("PerDay", StringComparison.OrdinalIgnoreCase))
                            {
                                // bis morgen (UTC) + 5 Minuten
                                var next = DateTime.UtcNow.Date.AddDays(1).AddMinutes(5);
                                return next - DateTime.UtcNow;
                            }
                        }
                    }
                }
            }
        }
        catch { }

        return null;
    }
}

