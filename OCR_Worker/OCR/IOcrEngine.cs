namespace DocumentManagementSystem.OCR_Worker.OCR;

public interface IOcrEngine
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct);
}


