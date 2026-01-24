namespace DocumentManagementSystem.AccessBatch.Options;

public sealed class AccessBatchOptions
{
    public string InputFolder { get; set; } = "access-input";
    public string ArchiveFolder { get; set; } = "access-archive";
    public string FilePattern { get; set; } = "*.xml";

    public int RunHour { get; set; } = 1;
    public int RunMinute { get; set; } = 0;

    // fürs Testen extrem praktisch (einmal sofort laufen)
    public bool RunOnStart { get; set; } = true;
}
