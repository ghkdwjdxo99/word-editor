using SimpleWordEditor;

if (args.Length != 2 || args[0] is not ("generate" or "inspect"))
{
    Console.Error.WriteLine("Usage: FixtureTool <generate|inspect> <docx-path>");
    return 2;
}

var path = Path.GetFullPath(args[1]);
if (args[0] == "generate")
{
    var model = new DocumentModel();
    var left = new DocumentParagraph { Alignment = ParagraphAlignment.Left };
    left.Runs.Add(new("  한글 English 123 앞뒤 공백  ", FontSize: 11));
    left.Runs.Add(new("탭\t뒤\n줄바꿈", FontSize: 11));
    model.Paragraphs.Add(left);
    model.Paragraphs.Add(new DocumentParagraph());
    var center = new DocumentParagraph { Alignment = ParagraphAlignment.Center };
    center.Runs.Add(new("굵게", Bold: true, FontSize: 14));
    center.Runs.Add(new(" 기울임", Italic: true, FontSize: 14));
    center.Runs.Add(new(" 밑줄", Underline: true, FontSize: 14));
    model.Paragraphs.Add(center);
    var right = new DocumentParagraph { Alignment = ParagraphAlignment.Right };
    right.Runs.Add(new("오른쪽 16pt", FontSize: 16));
    model.Paragraphs.Add(right);
    SafeFileSaver.Save(path, p => DocxService.Write(model, p), DocxService.Validate);
    Console.WriteLine(path);
    return 0;
}

var read = DocxService.Read(path);
Console.WriteLine($"Paragraphs={read.Paragraphs.Count};Unsupported={read.HasUnsupportedContent}");
for (var i = 0; i < read.Paragraphs.Count; i++)
{
    var p = read.Paragraphs[i];
    Console.WriteLine($"P{i + 1}|Alignment={p.Alignment}|Text={Escape(string.Concat(p.Runs.Select(r => r.Text)))}");
    foreach (var r in p.Runs)
        Console.WriteLine($" R|Text={Escape(r.Text)}|B={r.Bold}|I={r.Italic}|U={r.Underline}|Size={r.FontSize:0.##}");
}
return 0;

static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");
