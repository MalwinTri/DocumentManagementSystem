using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Xunit;

public class OcrPdfCliTests
{
    static OcrPdfCliTests()
    {
        // Verhindert die Lizenz-Dialog-Exception von QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task Pdf_to_Tiff_then_Tesseract_returns_text()
    {
        var pdf = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}.pdf");

        Document.Create(c => c.Page(p => p.Content().Padding(40)
            .Text("Hello OCR – Grüezi Österreich! 456").FontSize(28)))
            .GeneratePdf(pdf);

        var tiffPattern = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}_%03d.tiff");

        try
        {
            // PDF -> TIFF (300 DPI, Graustufen)
            var (gsOut, gsErr, gsCode) = await RunCliAsync(GhostscriptExe(),
                $"-q -dNOPAUSE -dBATCH -sDEVICE=tiffgray -r300 -sOutputFile=\"{tiffPattern}\" \"{pdf}\"");
            gsCode.Should().Be(0, $"Ghostscript failed: {gsErr}");

            // TIFF-Seiten mit Tesseract lesen
            var sb = new StringBuilder();
            var dir = Path.GetDirectoryName(tiffPattern)!;
            var pattern = Path.GetFileName(tiffPattern).Replace("%03d", "*");
            foreach (var tiff in Directory.GetFiles(dir, pattern).OrderBy(x => x))
            {
                var (outTxt, err, code) = await RunCliAsync(
                    TesseractExe(),
                    $"\"{tiff}\" stdout -l deu+eng --dpi 300 --psm 6",
                    TessEnv());

                code.Should().Be(0, err);
                sb.AppendLine(outTxt);
            }

            // Robust gegen OCR-Varianten (Umlaute, Satzzeichen, Spacing)
            var folded = FoldForOcrAssertions(sb.ToString());

            // Stabile Kern-Signale
            folded.Should().Contain("hello");
            folded.Should().Contain("ocr");
            folded.Should().Contain("456");

            // Tolerant bei "Grüezi" + "Österreich"
            folded.Should().ContainAny(new[] { "gruezi", "griiezi", "gruzi" });
            folded.Should().ContainAny(new[] { "oesterreich", "osterreich" });
        }
        finally
        {
            TryDelete(pdf);
            foreach (var f in Directory.GetFiles(Path.GetTempPath(), "ocr_*_*.tiff"))
                TryDelete(f);
        }
    }

    [Fact]
    public void TessEnv_Uses_TESSDATA_PREFIX_when_set()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"tessdata_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var old = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
        try
        {
            Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tempDir);
            var dict = OcrCliSmokeTests_Tools.TessEnv();

            dict.Should().ContainKey("TESSDATA_PREFIX");
            dict["TESSDATA_PREFIX"].Should().Be(tempDir);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESSDATA_PREFIX", old);
            try { Directory.Delete(tempDir); } catch { }
        }
    }

    [Fact]
    public void TesseractExe_Uses_env_on_windows_else_returns_tesseract()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"tess_{Guid.NewGuid():N}.exe");
        File.WriteAllText(tmp, string.Empty);
        var old = Environment.GetEnvironmentVariable("TESSERACT_EXE");
        try
        {
            Environment.SetEnvironmentVariable("TESSERACT_EXE", tmp);
            var res = OcrCliSmokeTests_Tools.TesseractExe();
            if (OperatingSystem.IsWindows())
                res.Should().Be(tmp);
            else
                res.Should().Be("tesseract");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESSERACT_EXE", old);
            try { File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void GhostscriptExe_Uses_env_on_windows_else_returns_gs()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"gs_{Guid.NewGuid():N}.exe");
        File.WriteAllText(tmp, string.Empty);
        var old = Environment.GetEnvironmentVariable("GHOSTSCRIPT_EXE");
        try
        {
            Environment.SetEnvironmentVariable("GHOSTSCRIPT_EXE", tmp);
            var res = OcrCliSmokeTests_Tools.GhostscriptExe();
            if (OperatingSystem.IsWindows())
                res.Should().Be(tmp);
            else
                res.Should().Be("gs");
        }
        finally
        {
            Environment.SetEnvironmentVariable("GHOSTSCRIPT_EXE", old);
            try { File.Delete(tmp); } catch { }
        }
    }

    [Fact]
    public void TryDelete_deletes_file_and_does_not_throw_for_missing()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"del_{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tmp, "x");
        OcrCliSmokeTests_Tools.TryDelete(tmp);
        File.Exists(tmp).Should().BeFalse();

        // second call must not throw
        OcrCliSmokeTests_Tools.TryDelete(tmp);
    }

    // --- Delegation an die gemeinsame Tool-Klasse ---
    static string TesseractExe() => OcrCliSmokeTests_Tools.TesseractExe();
    static string GhostscriptExe() => OcrCliSmokeTests_Tools.GhostscriptExe();
    static Dictionary<string, string> TessEnv() => OcrCliSmokeTests_Tools.TessEnv();
    static async Task<(string stdout, string stderr, int code)> RunCliAsync(string file, string args, Dictionary<string, string>? env = null)
        => await OcrCliSmokeTests_Tools.RunCliAsync(file, args, env);
    static void TryDelete(string path) => OcrCliSmokeTests_Tools.TryDelete(path);

    // OCR-freundliche Normalisierung (für den E2E-Test beibehalten)
    static string FoldForOcrAssertions(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var mapped = s
            .Replace("Ä", "Ae").Replace("ä", "ae")
            .Replace("Ö", "Oe").Replace("ö", "oe")
            .Replace("Ü", "Ue").Replace("ü", "ue")
            .Replace("ß", "ss");

        var formD = mapped.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var noMarks = sb.ToString().Normalize(NormalizationForm.FormC);

        noMarks = noMarks.Replace('–', '-').Replace('—', '-');
        noMarks = Regex.Replace(noMarks, @"[\p{P}\p{S}]", " ");
        noMarks = Regex.Replace(noMarks, @"\s+", " ").Trim();

        return noMarks.ToLowerInvariant();
    }
}

static class OcrCliSmokeTests_Tools
{
    public static string TesseractExe()
    {
        if (OperatingSystem.IsWindows())
        {
            var c = new[]
            {
                Environment.GetEnvironmentVariable("TESSERACT_EXE"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tesseract-OCR","tesseract.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tesseract-OCR","tesseract.exe"),
                "tesseract.exe" // via PATH
            };
            var f = c.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
            if (f is null) throw new InvalidOperationException("Tesseract nicht gefunden (setze TESSERACT_EXE oder installiere es).");
            return f!;
        }
        return "tesseract";
    }

    public static string GhostscriptExe()
    {
        if (OperatingSystem.IsWindows())
        {
            var env = Environment.GetEnvironmentVariable("GHOSTSCRIPT_EXE");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;

            static string? Probe(string root)
                => Directory.Exists(root)
                    ? Directory.GetDirectories(root)
                        .OrderByDescending(x => x)
                        .Select(x => Path.Combine(x, "bin", "gswin64c.exe"))
                        .FirstOrDefault(File.Exists)
                    : null;

            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var pfx = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            return Probe(Path.Combine(pf, "gs"))
                   ?? Probe(Path.Combine(pfx, "gs"))
                   ?? "gswin64c.exe"; // Fallback: via PATH
        }
        return "gs"; // Linux/Mac
    }

    public static Dictionary<string, string> TessEnv()
    {
        var c = new[]
        {
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX"),
            "/usr/share/tesseract-ocr/5/tessdata",
            "/usr/share/tesseract-ocr/4.00/tessdata",
            @"C:\Program Files\Tesseract-OCR\tessdata",
            @"C:\Program Files (x86)\Tesseract-OCR\tessdata"
        };
        var p = c.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && Directory.Exists(x!));
        return string.IsNullOrWhiteSpace(p) ? new() : new() { ["TESSDATA_PREFIX"] = p! };
    }

    public static async Task<(string stdout, string stderr, int code)> RunCliAsync(string file, string args, Dictionary<string, string>? extraEnv = null)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (extraEnv != null)
            foreach (var kv in extraEnv)
                psi.Environment[kv.Key] = kv.Value;

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Konnte Prozess nicht starten: {file}");
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (stdout, stderr, p.ExitCode);
    }

    public static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
