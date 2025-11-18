namespace DocumentManagementSystem.Infrastructure.Services
{
    public interface IGarageS3Service
    {
        Task UploadPdfAsync(string key, Stream pdfStream, CancellationToken ct = default);
        Task<Stream> GetPdfAsync(string key, CancellationToken ct = default);
    }
}
