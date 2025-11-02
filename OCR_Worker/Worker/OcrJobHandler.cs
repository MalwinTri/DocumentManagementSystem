using System.Text;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.OCR_Worker.OCR;
using Microsoft.Extensions.Logging;

namespace DocumentManagementSystem.OCR_Worker.Worker;

public class OcrJobHandler
{
    private readonly IOcrEngine _ocr;
    private readonly ILogger<OcrJobHandler> _log;

    public OcrJobHandler(IOcrEngine ocr, ILogger<OcrJobHandler> log)
        => (_ocr, _log) = (ocr, log);

    public async Task HandleAsync(OcrJob job, CancellationToken ct)
    {
        if (!File.Exists(job.LocalPath))
            throw new FileNotFoundException($"PDF not found: {job.LocalPath}");

        await using var pdf = File.OpenRead(job.LocalPath);
        var text = await _ocr.ExtractTextAsync(pdf, ct);

        _log.LogInformation("OCR result for {Path}: {Preview}",
            job.LocalPath, text.Length > 200 ? text[..200] + "..." : text);

        var txtPath = Path.ChangeExtension(job.LocalPath, ".txt")!;
        await File.WriteAllTextAsync(txtPath, text, Encoding.UTF8, ct);
        _log.LogInformation("Wrote OCR text to {Txt}", txtPath);
    }
}
