using System.Text;
using ImageMagick;
using Tesseract;

namespace DocumentManagementSystem.OCR_Worker.OCR;

public sealed class OcrEngine : IOcrEngine
{
    private readonly string _language;
    private static readonly string[] Probe =
    {
        "/usr/share/tesseract-ocr/4.00/tessdata",
        "/usr/share/tessdata",
        "/usr/share/tesseract-ocr/tessdata",
        @"C:\Program Files\Tesseract-OCR\tessdata",
        @"C:\Program Files (x86)\Tesseract-OCR\tessdata"
    };

    public OcrEngine(string language = "eng+deu") => _language = language;

    public Task<string> ExtractTextAsync(Stream pdf, CancellationToken ct = default)
    {
        // PDF -> Bilder @300 DPI
        var settings = new MagickReadSettings { Density = new Density(300, 300) };

        using var pages = new MagickImageCollection();
        pages.Read(pdf, settings);  // benötigt Ghostscript zur Laufzeit

        var tessdata = Environment.GetEnvironmentVariable("TESSDATA_PREFIX")
            ?? new[] {
            "/usr/share/tesseract-ocr/4.00/tessdata",
            "/usr/share/tessdata",
            "/usr/share/tesseract-ocr/tessdata",
            @"C:\Program Files\Tesseract-OCR\tessdata",
            @"C:\Program Files (x86)\Tesseract-OCR\tessdata"
            }.FirstOrDefault(Directory.Exists)
            ?? "/usr/share/tesseract-ocr/4.00/tessdata";

        using var engine = new TesseractEngine(tessdata, "eng+deu", EngineMode.Default);
        var sb = new StringBuilder();

        foreach (var p in pages)
        {
            ct.ThrowIfCancellationRequested();
            using var ms = new MemoryStream();
            p.Format = MagickFormat.Png;
            p.Write(ms);
            using var pix = Pix.LoadFromMemory(ms.ToArray());
            using var page = engine.Process(pix);
            sb.AppendLine(page.GetText());
        }
        return Task.FromResult(sb.ToString());
    }

}
