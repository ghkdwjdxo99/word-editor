using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SimpleWordEditor;

public sealed record EditorFormatState(bool? Bold, bool? Italic, bool? Underline, double? FontSize, TextAlignment? Alignment)
{
    public static EditorFormatState Read(RichTextBox editor)
    {
        var selection = editor.Selection;
        return new(
            ReadToggle(selection, TextElement.FontWeightProperty, FontWeights.Bold),
            ReadToggle(selection, TextElement.FontStyleProperty, FontStyles.Italic),
            ReadUnderline(selection),
            ReadFontSize(selection),
            ReadAlignment(selection));
    }

    private static bool? ReadToggle(TextRange selection, DependencyProperty property, object enabled)
    {
        var value = selection.GetPropertyValue(property);
        return value == DependencyProperty.UnsetValue ? null : Equals(value, enabled);
    }

    private static bool? ReadUnderline(TextRange selection)
    {
        var value = selection.GetPropertyValue(Inline.TextDecorationsProperty);
        if (value == DependencyProperty.UnsetValue) return null;
        return value is TextDecorationCollection decorations && decorations.Any(x => x.Location == TextDecorationLocation.Underline);
    }

    private static double? ReadFontSize(TextRange selection)
    {
        var value = selection.GetPropertyValue(TextElement.FontSizeProperty);
        if (value == DependencyProperty.UnsetValue || value is not double pixels) return null;
        var points = pixels * 72 / 96;
        // WPF는 일부 DPI 글자 크기를 1 DIP 단위로 양자화한다(16pt → 21 DIP → 15.75pt).
        // 정수 포인트와 0.25pt 이내인 경우 사용자가 지정한 정수 크기로 표시한다.
        var nearestInteger = Math.Round(points, MidpointRounding.AwayFromZero);
        return Math.Abs(points - nearestInteger) <= 0.25 + double.Epsilon
            ? nearestInteger
            : Math.Round(points, 2, MidpointRounding.AwayFromZero);
    }

    private static TextAlignment? ReadAlignment(TextRange selection)
    {
        var paragraphs = selection.Start.Paragraph is null
            ? []
            : selection.Start.Paragraph.Parent is FlowDocument document
                ? document.Blocks.OfType<Paragraph>().Where(p => p.ContentStart.CompareTo(selection.End) <= 0 && p.ContentEnd.CompareTo(selection.Start) >= 0).ToArray()
                : [selection.Start.Paragraph];
        if (paragraphs.Length == 0) return null;
        var first = paragraphs[0].TextAlignment;
        return paragraphs.All(p => p.TextAlignment == first) ? first : null;
    }
}
