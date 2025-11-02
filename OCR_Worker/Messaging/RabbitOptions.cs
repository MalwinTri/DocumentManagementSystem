namespace DocumentManagementSystem.OCR_Worker.Messaging;

public sealed class RabbitOptions
{
    public string Host { get; set; } = "rabbitmq";
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string Queue { get; set; } = "ocr-queue";
    public ushort Prefetch { get; set; } = 1;
    public bool RequeueOnError { get; set; } = false; // bei DLQ: false
}
