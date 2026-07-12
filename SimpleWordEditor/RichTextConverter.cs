using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SimpleWordEditor;

public sealed class UnsupportedEditorContentException(IReadOnlyList<string> blockTypes)
    : InvalidOperationException($"지원하지 않는 편집 블록이 있습니다: {string.Join(", ", blockTypes)}")
{
    public IReadOnlyList<string> BlockTypes { get; } = blockTypes;
}

public static class RichTextConverter
{
    public static DocumentModel FromEditor(RichTextBox editor)
    {
        var unsupported = FindUnsupportedBlocks(editor.Document);
        if (unsupported.Count > 0) throw new UnsupportedEditorContentException(unsupported);
        var model = new DocumentModel();
        foreach (var paragraph in editor.Document.Blocks.OfType<Paragraph>()) model.Paragraphs.Add(ConvertParagraph(paragraph));
        if (model.Paragraphs.Count == 0) model.Paragraphs.Add(new());
        return model;
    }

    public static IReadOnlyList<string> FindUnsupportedBlocks(FlowDocument document)
    {
        var types = new HashSet<string>(StringComparer.Ordinal);
        InspectBlocks(document.Blocks, types);
        return types.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private static void InspectBlocks(BlockCollection blocks, ISet<string> types)
    {
        foreach (var block in blocks)
        {
            if (block is Paragraph) continue;
            types.Add(block.GetType().Name);
            switch (block)
            {
                case Section section: InspectBlocks(section.Blocks, types); break;
                case List list:
                    foreach (var item in list.ListItems) InspectBlocks(item.Blocks, types);
                    break;
                case Table table:
                    foreach (var group in table.RowGroups)
                    foreach (var row in group.Rows)
                    foreach (var cell in row.Cells) InspectBlocks(cell.Blocks, types);
                    break;
            }
        }
    }

    private static DocumentParagraph ConvertParagraph(Paragraph block)
    {
        var paragraph = new DocumentParagraph { Alignment = block.TextAlignment switch { TextAlignment.Center => ParagraphAlignment.Center, TextAlignment.Right => ParagraphAlignment.Right, _ => ParagraphAlignment.Left } };
        foreach (var inline in block.Inlines) AppendInline(inline, paragraph.Runs, false);
        return paragraph;
    }

    private static void AppendInline(Inline inline, ICollection<TextRun> output, bool inheritedUnderline)
    {
        var underline = inheritedUnderline || HasUnderline(inline);
        switch (inline)
        {
            case Run run:
                if (run.Text.Length > 0) output.Add(new(run.Text, IsBold(run), IsItalic(run), underline, PointSize(run.FontSize)));
                break;
            case LineBreak:
                output.Add(new("\n", IsBold(inline), IsItalic(inline), underline, PointSize(inline.FontSize)));
                break;
            case Span span:
                foreach (var child in span.Inlines) AppendInline(child, output, underline);
                break;
            default:
                var text = new TextRange(inline.ContentStart, inline.ContentEnd).Text;
                if (text.Length > 0) output.Add(new(text, IsBold(inline), IsItalic(inline), underline, PointSize(inline.FontSize)));
                break;
        }
    }

    private static bool IsBold(TextElement element) => element.FontWeight == FontWeights.Bold;
    private static bool IsItalic(TextElement element) => element.FontStyle == FontStyles.Italic;
    private static bool HasUnderline(Inline inline) => inline.TextDecorations?.Any(x => x.Location == TextDecorationLocation.Underline) == true;
    private static double PointSize(double pixels) => Math.Clamp(pixels * 72 / 96, 8, 72);

    public static void ToEditor(DocumentModel model, RichTextBox editor)
    {
        var doc = new FlowDocument { FontFamily = new FontFamily("맑은 고딕"), FontSize = 11 * 96 / 72, PagePadding = new Thickness(20) };
        foreach (var source in model.Paragraphs)
        {
            var p = new Paragraph { Margin = new Thickness(0), TextAlignment = source.Alignment switch { ParagraphAlignment.Center => TextAlignment.Center, ParagraphAlignment.Right => TextAlignment.Right, _ => TextAlignment.Left } };
            foreach (var r in source.Runs)
            {
                var pieces = r.Text.Split('\n');
                for (var i = 0; i < pieces.Length; i++)
                {
                    if (i > 0) p.Inlines.Add(new LineBreak());
                    if (pieces[i].Length > 0) p.Inlines.Add(new Run(pieces[i]) { FontWeight = r.Bold ? FontWeights.Bold : FontWeights.Normal, FontStyle = r.Italic ? FontStyles.Italic : FontStyles.Normal, TextDecorations = r.Underline ? TextDecorations.Underline : null, FontSize = Math.Clamp(r.FontSize, 8, 72) * 96 / 72 });
                }
            }
            doc.Blocks.Add(p);
        }
        editor.Document = doc;
    }
}
