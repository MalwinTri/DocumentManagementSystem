using Amazon.S3;
using Amazon.S3.Model;
using DocumentManagementSystem.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Diagnostics;
using System.Text;

namespace DocumentManagementSystem.OCR_Worker.Worker;

public sealed class OcrJobHandler
{
    private readonly ILogger<OcrJobHandler> _log;
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string _connectionString;
    private readonly string _lang;
    private readonly int _dpi;
    private readonly string? _tessdataPrefix;

    public OcrJobHandler(
        ILogger<OcrJobHandler> log,
        IAmazonS3 s3,
        string bucket,
        string connectionString,
        string lang,
        int dpi,
        string? tessdataPrefix)
    {
        _log = log;
        _s3 = s3;
        _bucket = string.IsNullOrWhiteSpace(bucket) ? "documents" : bucket;
        _connectionString = connectionString;
        _lang = lang;
        _dpi = dpi;
        _tessdataPrefix = tessdataPrefix;
    }

    public async Task HandleAsync(OcrJob job, CancellationToken ct)
    {
        var s3Key = string.IsNullOrWhiteSpace(job.S3Key)
            ? job.DocumentId + ".pdf"
            : job.S3Key;

        _log.LogInformation("Processing OCR job. DocumentId={Id}, S3Key={Key}", job.DocumentId, s3Key);

        // 1) PDF aus Garage holen
        await using var pdfStream = await DownloadPdfAsync(s3Key, ct);

        // 2) OCR -> Text
        var text = await OcrPdfStreamAsync(pdfStream, ct);

        // 3) Text zurück ins S3 (.txt)
        var txtKey = Path.ChangeExtension(s3Key, ".txt")!;
        await UploadTextAsync(txtKey, text, ct);

        _log.LogInformation(
            "OCR done for {Id}. Uploaded: s3://{Bucket}/{Key} (len={Len})",
            job.DocumentId, _bucket, txtKey, text.Length);

        // 4
        // ) Nur OCR-Text in Postgres speichern (Summary macht GenAI-Worker)
        await SaveToDatabaseAsync(job.DocumentId, text, ct);
    }

    // ======================= S3 =======================

    private async Task<Stream> DownloadPdfAsync(string key, CancellationToken ct)
    {
        using var resp = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _bucket,
            Key = key
        }, ct);

        var ms = new MemoryStream();
        await resp.ResponseStream.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    private async Task UploadTextAsync(string key, string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await using var ms = new MemoryStream(bytes);

        var put = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = ms,
            ContentType = "text/plain; charset=utf-8",
            UseChunkEncoding = false
        };
        put.Headers.ContentLength = ms.Length;

        await _s3.PutObjectAsync(put, ct);
    }

    // ======================= OCR (Ghostscript + Tesseract) =======================

    private async Task<string> OcrPdfStreamAsync(Stream pdfStream, CancellationToken ct)
    {
        var work = Directory.CreateTempSubdirectory("ocr");
        var pdfPath = Path.Combine(work.FullName, "input.pdf");

        await using (var fs = File.Create(pdfPath))
        {
            pdfStream.Position = 0;
            await pdfStream.CopyToAsync(fs, ct);
        }

        var tiffPattern = Path.Combine(work.FullName, "page-%03d.tiff");

        try
        {
            // PDF -> TIFF (grau, 300dpi) via Ghostscript
            await RunCli("gs",
                $"-q -dNOPAUSE -dBATCH -sDEVICE=tiffgray -r{_dpi} -sOutputFile=\"{tiffPattern}\" \"{pdfPath}\"");

            var sb = new StringBuilder();

            foreach (var tiff in Directory.GetFiles(work.FullName, "page-*.tiff").OrderBy(x => x))
            {
                var env = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(_tessdataPrefix))
                    env["TESSDATA_PREFIX"] = _tessdataPrefix;

                var pageText = await RunCli("tesseract", $"\"{tiff}\" stdout -l {_lang} --dpi {_dpi}", env);
                sb.AppendLine(pageText);
            }

            return sb.ToString().Trim();
        }
        finally
        {
            try
            {
                Directory.Delete(work.FullName, true);
            }
            catch
            {
                // ignorieren
            }
        }
    }

    private static async Task<string> RunCli(
        string file,
        string args,
        IDictionary<string, string>? extraEnv = null)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        if (extraEnv != null)
        {
            foreach (var kv in extraEnv)
                psi.Environment[kv.Key] = kv.Value;
        }

        using var p = Process.Start(psi)!;
        var stdout = await p.StandardOutput.ReadToEndAsync();
        var stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
            throw new InvalidOperationException($"{file} failed: {stderr}");

        return stdout;
    }

    // ======================= DB =======================

    private async Task SaveToDatabaseAsync(
        Guid documentId,
        string ocrText,
        CancellationToken ct)
    {
        await using var con = new NpgsqlConnection(_connectionString);
        await con.OpenAsync(ct);

        const string sql = @"
            UPDATE ""Documents""
            SET ""OcrText"" = @ocr
            WHERE ""Id"" = @id;
        ";

        await using var cmd = new NpgsqlCommand(sql, con);
        cmd.Parameters.AddWithValue("ocr", ocrText);
        cmd.Parameters.AddWithValue("id", documentId);

        var rows = await cmd.ExecuteNonQueryAsync(ct);

        if (rows == 0)
            _log.LogWarning("DocumentId={Id} nicht in DB gefunden, konnte OCR-Text nicht speichern.", documentId);
    }
}
