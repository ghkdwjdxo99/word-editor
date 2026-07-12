using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SimpleWordEditor;

public sealed class FlowDocumentSearchSnapshot
{
    private readonly IReadOnlyList<TextPointer> starts;
    private readonly IReadOnlyList<TextPointer> ends;
    public string Text { get; }

    private FlowDocumentSearchSnapshot(string text, IReadOnlyList<TextPointer> starts, IReadOnlyList<TextPointer> ends)
    { Text = text; this.starts = starts; this.ends = ends; }

    public static FlowDocumentSearchSnapshot Create(FlowDocument document)
    {
        var text = new StringBuilder();
        var starts = new List<TextPointer>(); var ends = new List<TextPointer>();
        var paragraphs = document.Blocks.OfType<Paragraph>().ToArray();
        for (var p = 0; p < paragraphs.Length; p++)
        {
            foreach (var inline in paragraphs[p].Inlines) AppendInline(inline, text, starts, ends);
            if (p < paragraphs.Length - 1) Add("\n", paragraphs[p].ContentEnd, paragraphs[p + 1].ContentStart, text, starts, ends);
        }
        return new(text.ToString(), starts, ends);
    }

    public bool Select(RichTextBox editor, FindMatch match)
    {
        if (match.Start < 0 || match.Length <= 0 || match.Start + match.Length > starts.Count) return false;
        editor.Selection.Select(starts[match.Start], ends[match.Start + match.Length - 1]);
        editor.Selection.Start.Paragraph?.BringIntoView();
        return true;
    }

    private static void AppendInline(Inline inline, StringBuilder text, List<TextPointer> starts, List<TextPointer> ends)
    {
        switch (inline)
        {
            case Run run:
                for (var i = 0; i < run.Text.Length; i++)
                    Add(run.Text[i].ToString(), run.ContentStart.GetPositionAtOffset(i)!, run.ContentStart.GetPositionAtOffset(i + 1)!, text, starts, ends);
                break;
            case LineBreak lineBreak:
                Add("\n", lineBreak.ElementStart, lineBreak.ElementEnd, text, starts, ends);
                break;
            case Span span:
                foreach (var child in span.Inlines) AppendInline(child, text, starts, ends);
                break;
        }
    }

    private static void Add(string value, TextPointer start, TextPointer end, StringBuilder text, List<TextPointer> starts, List<TextPointer> ends)
    { text.Append(value); starts.Add(start); ends.Add(end); }
}
