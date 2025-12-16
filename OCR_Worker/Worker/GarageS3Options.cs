namespace DocumentManagementSystem.OCR_Worker.Worker;

public sealed class GarageS3Options
{
    public string Endpoint { get; set; } = "";
    public string Region { get; set; } = "garage";
    public string Bucket { get; set; } = "documents";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
}
