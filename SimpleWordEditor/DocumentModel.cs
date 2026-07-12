namespace SimpleWordEditor;

public enum ParagraphAlignment { Left, Center, Right }
public sealed record TextRun(string Text, bool Bold = false, bool Italic = false, bool Underline = false, double FontSize = 11);
public sealed class DocumentParagraph
{
    public ParagraphAlignment Alignment { get; set; }
    public List<TextRun> Runs { get; } = [];
}
public sealed class DocumentModel
{
    public List<DocumentParagraph> Paragraphs { get; } = [];
    public bool HasUnsupportedContent { get; set; }
}
