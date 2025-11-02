namespace DocumentManagementSystem.Models;

public record OcrJob(
    Guid DocumentId,
    string LocalPath,                 // Pfad zur PDF – wir arbeiten erstmal mit Dateisystem
    string? ContentType = null,
    DateTime? UploadedAt = null
);
