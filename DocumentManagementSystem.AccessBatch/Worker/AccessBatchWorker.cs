using DocumentManagementSystem.AccessBatch.Options;
using DocumentManagementSystem.AccessBatch.Services;
using DocumentManagementSystem.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.AccessBatch.Worker;

public sealed class AccessBatchWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccessBatchWorker> _logger;
    private readonly AccessBatchOptions _opt;

    public AccessBatchWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AccessBatchOptions> opt,
        ILogger<AccessBatchWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_opt.InputFolder);
        Directory.CreateDirectory(_opt.ArchiveFolder);

        _logger.LogInformation("AccessBatchWorker started. Input={Input} Pattern={Pattern} Archive={Archive} RunOnStart={RunOnStart}",
            _opt.InputFolder, _opt.FilePattern, _opt.ArchiveFolder, _opt.RunOnStart);

        // 1) Optional: einmal sofort laufen (für Demo/Testing)
        if (_opt.RunOnStart && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("RunOnStart enabled -> running once immediately.");
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunOnStart RunOnce failed (continuing with schedule).");
            }
        }

        // 2) Danach normaler Daily-Scheduler
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun(_opt.RunHour, _opt.RunMinute);
            _logger.LogInformation("Next scheduled run in {Delay}.", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (TaskCanceledException) { break; }

            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled RunOnce failed.");
            }
        }
    }


    private async Task RunOnce(CancellationToken ct)
    {
        var files = Directory.EnumerateFiles(_opt.InputFolder, _opt.FilePattern).OrderBy(f => f).ToList();
        if (files.Count == 0)
        {
            _logger.LogInformation("No files found.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();

        foreach (var file in files)
        {
            _logger.LogInformation("Processing file {File}", file);

            try
            {
                var xml = await File.ReadAllTextAsync(file, ct);
                var entries = AccessXmlParser.Parse(xml);

                foreach (var e in entries)
                {
                    var exists = await db.Documents.AnyAsync(d => d.Id == e.DocumentId, ct);
                    if (!exists)
                    {
                        _logger.LogWarning("Document {DocId} not found. Skipping.", e.DocumentId);
                        continue;
                    }

                    await db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ""DocumentDailyAccesses"" (""DocumentId"", ""Day"", ""Count"")
                    VALUES ({e.DocumentId}, {e.Day}, {e.Count})
                    ON CONFLICT (""DocumentId"", ""Day"")
                    DO UPDATE SET ""Count"" = ""DocumentDailyAccesses"".""Count"" + EXCLUDED.""Count"";
                    ", ct);
                }

                var destName = Path.GetFileNameWithoutExtension(file) + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".xml";
                var destPath = Path.Combine(_opt.ArchiveFolder, destName);

                File.Move(file, destPath, overwrite: true);
                _logger.LogInformation("Archived to {Dest}", destPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing file {File}. Leaving it in place.", file);
            }
        }
    }

    private static TimeSpan GetDelayToNextRun(int hour, int minute)
    {
        var now = DateTime.Now;
        var next = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        if (next <= now) next = next.AddDays(1);
        return next - now;
    }
}