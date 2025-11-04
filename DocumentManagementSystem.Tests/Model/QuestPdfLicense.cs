using QuestPDF.Infrastructure;
using System.Runtime.CompilerServices;

public static class QuestPdfLicense
{
    [ModuleInitializer]
    public static void Init()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
