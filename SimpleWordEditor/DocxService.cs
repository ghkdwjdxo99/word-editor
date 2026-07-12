using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace SimpleWordEditor;

public static class DocxService
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static DocumentModel Read(string path)
    {
        try
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.GetEntry("word/document.xml") ?? throw new InvalidDataException("필수 문서 XML이 없습니다.");
            using var stream = entry.Open();
            var xml = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            var model = new DocumentModel();
            var unsupported = new HashSet<string> { "tbl", "drawing", "pict", "object", "hyperlink", "sectPr", "fldSimple", "instrText", "sdt", "ins", "del", "commentReference", "footnoteReference", "endnoteReference", "headerReference", "footerReference", "oMath", "oMathPara" };
            model.HasUnsupportedContent = xml.Descendants().Any(e => unsupported.Contains(e.Name.LocalName));
            foreach (var p in xml.Descendants(W + "body").Elements(W + "p"))
            {
                var para = new DocumentParagraph();
                para.Alignment = p.Element(W + "pPr")?.Element(W + "jc")?.Attribute(W + "val")?.Value switch
                { "center" => ParagraphAlignment.Center, "right" or "end" => ParagraphAlignment.Right, _ => ParagraphAlignment.Left };
                foreach (var run in p.Elements(W + "r"))
                {
                    var props = run.Element(W + "rPr");
                    var bold = IsOn(props?.Element(W + "b"));
                    var italic = IsOn(props?.Element(W + "i"));
                    var underline = props?.Element(W + "u")?.Attribute(W + "val")?.Value is not (null or "none" or "0" or "false");
                    var halfPoints = double.TryParse(props?.Element(W + "sz")?.Attribute(W + "val")?.Value, out var sz) ? sz : 22;
                    var text = string.Concat(run.Nodes().OfType<XElement>().Select(e => e.Name == W + "t" ? e.Value : e.Name == W + "tab" ? "\t" : e.Name == W + "br" ? "\n" : ""));
                    if (text.Length > 0) para.Runs.Add(new(text, bold, italic, underline, Math.Clamp(halfPoints / 2, 8, 72)));
                }
                model.Paragraphs.Add(para);
            }
            if (model.Paragraphs.Count == 0) model.Paragraphs.Add(new());
            return model;
        }
        catch (InvalidDataException) { throw; }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        { throw new InvalidDataException("DOCX 파일이 손상되었거나 읽을 수 없습니다.", ex); }
    }

    public static void Write(DocumentModel model, string path)
    {
        using var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var zip = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true);
        Add(zip, "[Content_Types].xml", ContentTypes());
        Add(zip, "_rels/.rels", RootRelationships());
        Add(zip, "word/_rels/document.xml.rels", Relationships());
        var body = new XElement(W + "body", model.Paragraphs.Select(ToXml));
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(W + "document", new XAttribute(XNamespace.Xmlns + "w", W), new XAttribute(XNamespace.Xmlns + "r", R), body));
        Add(zip, "word/document.xml", doc);
    }

    public static void Validate(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        foreach (var name in new[] { "[Content_Types].xml", "_rels/.rels", "word/document.xml", "word/_rels/document.xml.rels" })
        {
            var e = zip.GetEntry(name) ?? throw new InvalidDataException($"필수 항목 누락: {name}");
            using var s = e.Open(); XDocument.Load(s);
        }
    }

    private static XElement ToXml(DocumentParagraph p) => new(W + "p",
        p.Alignment == ParagraphAlignment.Left ? null : new XElement(W + "pPr", new XElement(W + "jc", new XAttribute(W + "val", p.Alignment == ParagraphAlignment.Center ? "center" : "right"))),
        p.Runs.SelectMany(ToXml));

    private static IEnumerable<XElement> ToXml(TextRun r)
    {
        var props = new XElement(W + "rPr", r.Bold ? new XElement(W + "b") : null, r.Italic ? new XElement(W + "i") : null,
            r.Underline ? new XElement(W + "u", new XAttribute(W + "val", "single")) : null,
            new XElement(W + "sz", new XAttribute(W + "val", Math.Round(Math.Clamp(r.FontSize, 8, 72) * 2))));
        var parts = r.Text.Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0) yield return new XElement(W + "r", new XElement(props), new XElement(W + "br"));
            if (parts[i].Length == 0) continue;
            var content = parts[i].Split('\t').SelectMany((x, j) => j == 0 ? new[] { new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), x) } : new[] { new XElement(W + "tab"), new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), x) });
            yield return new XElement(W + "r", new XElement(props), content);
        }
    }
    private static bool IsOn(XElement? e) => e != null && e.Attribute(W + "val")?.Value is not ("0" or "false" or "off");
    private static void Add(ZipArchive zip, string name, XDocument xml) { var e = zip.CreateEntry(name, CompressionLevel.Optimal); using var s = e.Open(); xml.Save(s); }
    private static XDocument ContentTypes() { XNamespace c = "http://schemas.openxmlformats.org/package/2006/content-types"; return new(new XElement(c + "Types", new XElement(c + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")), new XElement(c + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")), new XElement(c + "Override", new XAttribute("PartName", "/word/document.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml")))); }
    private static XDocument RootRelationships() { XNamespace x = "http://schemas.openxmlformats.org/package/2006/relationships"; return new(new XElement(x + "Relationships", new XElement(x + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "word/document.xml")))); }
    private static XDocument Relationships() { XNamespace x = "http://schemas.openxmlformats.org/package/2006/relationships"; return new(new XElement(x + "Relationships")); }
}
