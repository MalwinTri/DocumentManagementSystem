using FluentAssertions;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

public class OcrCliSmokeTests
{
    [Fact]
    public async Task Tesseract_reads_text_from_generated_image()
    {
        // 1) Testbild erzeugen
        using var img = new ImageMagick.MagickImage(ImageMagick.MagickColors.White, 1000, 300);
        new ImageMagick.Drawing.Drawables().FontPointSize(96).FillColor(ImageMagick.MagickColors.Black)
                       .Text(40, 180, "Hallo OCR 123").Draw(img);

        var png = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}.png");
        img.Write(png);

        try
        {
            // 2) tesseract mit robustem Pfad-Resolver starten
            var (stdout, stderr, code) = await RunCliAsync(
                TesseractExe(),
                $"\"{png}\" stdout -l eng+deu --dpi 300",
                extraEnv: TessEnv());

            code.Should().Be(0, because: stderr);

            // 3) Assertions
            var txt = stdout.ToLowerInvariant();
            txt.Should().Contain("hallo").And.Contain("ocr").And.Contain("123");
        }
        finally { TryDelete(png); }
    }

    [Fact]
    public async Task Tesseract_version_is_displayed()
    {
        var (stdout, stderr, code) = await RunCliAsync(TesseractExe(), "--version");
        code.Should().Be(0, because: stderr);

        // robuste Versionserkennung (z.B. "tesseract 5.3.0")
        stdout.ToLowerInvariant().Should().Contain("tesseract");
        Regex.Match(stdout, @"\b(\d+\.\d+(\.\d+)?)\b").Success.Should().BeTrue();
    }

    // ------------- Helpers -------------

    static string TesseractExe()
    {
        if (OperatingSystem.IsWindows())
        {
            var candidates = new[]
            {
                Environment.GetEnvironmentVariable("TESSERACT_EXE"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),     "Tesseract-OCR","tesseract.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tesseract-OCR","tesseract.exe"),
                "tesseract.exe" // über PATH
            };
            var found = candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
            if (found is null)
                throw new InvalidOperationException(
                    "Tesseract nicht gefunden. Setze TESSERACT_EXE auf die tesseract.exe " +
                    "oder installiere Tesseract und/oder starte die Tests im Docker-Container.");
            return found!;
        }
        return "tesseract";
    }

    static Dictionary<string, string> TessEnv()
    {
        // TESSDATA_PREFIX erkennen/setzen
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX"),
            "/usr/share/tesseract-ocr/5/tessdata",
            "/usr/share/tesseract-ocr/4.00/tessdata",
            @"C:\Program Files\Tesseract-OCR\tessdata",
            @"C:\Program Files (x86)\Tesseract-OCR\tessdata"
        };
        var prefix = candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p!));
        return string.IsNullOrWhiteSpace(prefix) ? new() : new() { ["TESSDATA_PREFIX"] = prefix! };
    }

    static async Task<(string stdout, string stderr, int code)> RunCliAsync(string file, string args, Dictionary<string, string>? extraEnv = null)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (extraEnv != null)
            foreach (var kv in extraEnv) psi.Environment[kv.Key] = kv.Value;

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Konnte Prozess nicht starten: {file}");
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (stdout, stderr, p.ExitCode);
    }

    static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
}
