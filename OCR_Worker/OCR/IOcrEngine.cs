namespace DocumentManagementSystem.OCR_Worker.OCR;
public interface IOcrEngine
{
    Task<string> ExtractTextAsync(Stream pdf, CancellationToken ct = default);
}
