using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentManagementSystem.DAL.Postgres;
using DocumentManagementSystem.Database;
using DocumentManagementSystem.Infrastructure.Services.GenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.GenAI_Worker.AiWorker
{
    public class GenAiWorkerService : BackgroundService
    {
        private readonly ILogger<GenAiWorkerService> _logger;
        private readonly DmsDbContext _dbContext;
        private readonly IGenAiService _genAiService;

        public GenAiWorkerService(
            ILogger<GenAiWorkerService> logger,
            DmsDbContext dbContext,
            IGenAiService genAiService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _genAiService = genAiService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GenAI Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Find one document whose OCR text is present but summary is still missing
                    var doc = await _dbContext.Documents
                        .Where(d => d.OcrText != null && d.Summary == null)
                        .OrderBy(d => d.CreatedAt)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (doc == null)
                    {
                        // nothing to do, wait a bit
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Generating summary for document {DocumentId}", doc.Id);

                    var summary = await _genAiService.GenerateSummaryAsync(doc.OcrText!, stoppingToken);

                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        doc.Summary = summary;
                        await _dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Summary stored for document {DocumentId}", doc.Id);
                    }
                    else
                    {
                        _logger.LogWarning("No summary generated for document {DocumentId}", doc.Id);
                        // Optionally mark a 'failed' status or retry counter here
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GenAI worker loop");
                    // Backoff a bit on hard failures
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("GenAI Worker stopping");
        }
    }
}



