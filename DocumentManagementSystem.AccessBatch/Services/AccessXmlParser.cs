using System.Xml.Linq;

namespace DocumentManagementSystem.AccessBatch.Services;

public static class AccessXmlParser
{
    public static IReadOnlyList<AccessEntry> Parse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root ?? throw new FormatException("Missing root element.");

        if (root.Name.LocalName != "accessStatistics")
            throw new FormatException("Root must be <accessStatistics>.");

        var dayAttr = (string?)root.Attribute("day")
            ?? throw new FormatException("Missing day attribute on <accessStatistics>.");

        var day = DateOnly.Parse(dayAttr);

        var list = new List<AccessEntry>();
        foreach (var el in root.Elements().Where(e => e.Name.LocalName == "access"))
        {
            var docIdStr = (string?)el.Attribute("documentId") ?? throw new FormatException("Missing documentId.");
            var countStr = (string?)el.Attribute("count") ?? "0";

            var docId = Guid.Parse(docIdStr);
            var count = int.Parse(countStr);

            list.Add(new AccessEntry(docId, day, count));
        }

        return list;
    }
}