using DocumentManagementSystem.Elasticsearch.Models;

namespace DocumentManagementSystem.Elasticsearch.Services;

public interface ISearchIndexService
{
    Task IndexDocumentAsync(DocumentIndex document, CancellationToken ct = default);
}

