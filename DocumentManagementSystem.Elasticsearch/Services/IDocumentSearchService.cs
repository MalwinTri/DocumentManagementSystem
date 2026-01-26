using DocumentManagementSystem.Elasticsearch.Models;

namespace DocumentManagementSystem.Elasticsearch.Services;

public interface IDocumentSearchService
{
    Task<IReadOnlyCollection<DocumentIndex>> SearchAsync(string query, CancellationToken ct = default);
}

