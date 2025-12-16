using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public interface IGenAiService
    {
        Task<string?> GenerateSummaryAsync(string text, CancellationToken cancellationToken = default);
    }
}
