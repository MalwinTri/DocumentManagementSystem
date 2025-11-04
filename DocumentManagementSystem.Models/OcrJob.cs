namespace DocumentManagementSystem.Models
{
    public sealed class OcrJob
    {
        public Guid DocumentId { get; init; }
        public string S3Key { get; init; } = null!;                 // z.B. "<id>.pdf"
        public string ContentType { get; init; } = "application/pdf";
        public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    }
}
