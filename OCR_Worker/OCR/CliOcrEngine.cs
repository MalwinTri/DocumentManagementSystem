using System.Diagnostics;
using System.Text;

namespace DocumentManagementSystem.OCR_Worker.OCR;

public class CliOcrEngine : IOcrEngine
{
    public async Task<string> ExtractTextAsync(Stream pdf, CancellationToken ct = default)
    {
        var work = Directory.CreateTempSubdirectory("ocr");
        var pdfPath = Path.Combine(work.FullName, "input.pdf");
        await using (var fs = File.Create(pdfPath)) { await pdf.CopyToAsync(fs, ct); }

        var tiffPattern = Path.Combine(work.FullName, "page-%03d.tiff");
        await Run("gs", $"-q -dNOPAUSE -dBATCH -sDEVICE=tiffgray -r300 -sOutputFile={tiffPattern} {pdfPath}", ct);

        var sb = new StringBuilder();
        foreach (var tiff in Directory.GetFiles(work.FullName, "page-*.tiff").OrderBy(x => x))
        {
            var outTxt = await Run("tesseract", $"{tiff} stdout -l deu+eng --dpi 300", ct);
            sb.AppendLine(outTxt);
        }

        try { Directory.Delete(work.FullName, true); } catch { /* ignore */ }
        return sb.ToString().Trim();
    }

    private static async Task<string> Run(string file, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var p = Process.Start(psi)!;
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0) throw new InvalidOperationException($"{file} failed: {stderr}");
        return stdout;
    }
}
