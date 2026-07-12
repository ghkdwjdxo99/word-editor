using SimpleWordEditor;
using System.IO;
using System.IO.Compression;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

var exitCode = 1;
var thread = new Thread(() => exitCode = RunTests());
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
return exitCode;

static int RunTests()
{
    var failures = new List<string>();
    var passes = 0;
    void Check(bool condition, string name, string? detail = null)
    {
        if (condition) { passes++; Console.WriteLine($"PASS {name}"); }
        else { failures.Add(name); Console.Error.WriteLine($"FAIL {name}{(detail is null ? "" : $": {detail}")}"); }
    }

    var dir = Path.Combine(Path.GetTempPath(), $"SimpleWordEditorTests-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    try
    {
        var model = new DocumentModel();
        var p1 = new DocumentParagraph { Alignment = ParagraphAlignment.Center };
        p1.Runs.Add(new(" 한글 English 123\t탭\n줄바꿈 ", true, true, true, 14));
        model.Paragraphs.Add(p1);
        model.Paragraphs.Add(new DocumentParagraph { Alignment = ParagraphAlignment.Right });
        var path = Path.Combine(dir, "roundtrip.docx");
        SafeFileSaver.Save(path, p => DocxService.Write(model, p), DocxService.Validate);
        Check(File.Exists(path), "DOCX 생성");
        using (var zip = ZipFile.OpenRead(path)) Check(zip.GetEntry("word/document.xml") != null && zip.GetEntry("[Content_Types].xml") != null, "필수 패키지 항목");
        var read = DocxService.Read(path);
        Check(read.Paragraphs.Count == 2, "문단 및 빈 문단 왕복");
        Check(read.Paragraphs[0].Alignment == ParagraphAlignment.Center && read.Paragraphs[1].Alignment == ParagraphAlignment.Right, "정렬 왕복");
        var runs = read.Paragraphs[0].Runs;
        Check(string.Concat(runs.Select(x => x.Text)) == " 한글 English 123\t탭\n줄바꿈 ", "텍스트/탭/줄바꿈 왕복");
        Check(runs.All(x => x.Bold && x.Italic && x.Underline && x.FontSize == 14), "서식 왕복");

        var boundary = new DocumentModel();
        var bp = new DocumentParagraph(); bp.Runs.Add(new("최소", FontSize: 8)); bp.Runs.Add(new("최대", FontSize: 72)); boundary.Paragraphs.Add(bp);
        var boundaryPath = Path.Combine(dir, "font-boundaries.docx");
        SafeFileSaver.Save(boundaryPath, p => DocxService.Write(boundary, p), DocxService.Validate);
        Check(DocxService.Read(boundaryPath).Paragraphs[0].Runs.Select(r => r.FontSize).SequenceEqual([8d, 72d]), "글자 크기 경계 왕복");

        var unsupportedPath = Path.Combine(dir, "unsupported.docx"); File.Copy(path, unsupportedPath);
        using (var zip = ZipFile.Open(unsupportedPath, ZipArchiveMode.Update))
        {
            var entry = zip.GetEntry("word/document.xml")!; string xml;
            using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();
            entry.Delete(); var replacement = zip.CreateEntry("word/document.xml"); using var writer = new StreamWriter(replacement.Open()); writer.Write(xml.Replace("</w:body>", "<w:tbl/><w:drawing/></w:body>"));
        }
        Check(DocxService.Read(unsupportedPath).HasUnsupportedContent, "DOCX 비지원 요소 감지");

        var old = File.ReadAllBytes(path);
        try { SafeFileSaver.Save(path, p => File.WriteAllText(p, "broken"), _ => throw new InvalidDataException()); } catch (InvalidDataException) { }
        Check(File.ReadAllBytes(path).SequenceEqual(old), "기존 파일 검증 실패 시 원본 유지");
        var failedNew = Path.Combine(dir, "failed-new.docx");
        try { SafeFileSaver.Save(failedNew, p => File.WriteAllText(p, "broken"), _ => throw new InvalidDataException()); } catch (InvalidDataException) { }
        Check(!File.Exists(failedNew) && !Directory.EnumerateFiles(dir, ".failed-new.docx.*.tmp").Any(), "신규 파일 검증 실패 시 결과/임시 파일 정리");
        var broken = Path.Combine(dir, "broken.docx"); File.WriteAllText(broken, "not zip");
        try { DocxService.Read(broken); Check(false, "손상 파일 예외"); } catch (InvalidDataException) { Check(true, "손상 파일 예외"); }
        try { DocxService.Read(Path.Combine(dir, "missing.docx")); Check(false, "없는 파일 예외"); } catch (InvalidDataException) { Check(true, "없는 파일 예외"); }

        // V100-P01/P03: 3단계 Span과 컨테이너 상속 서식, 링크 표시 문자열을 보존한다.
        var nestedEditor = new RichTextBox();
        var nestedParagraph = new Paragraph();
        var outer = new Bold { FontSize = 18 * 96 / 72 };
        var middle = new Italic();
        var inner = new Underline(new Run("삼중 중첩"));
        middle.Inlines.Add(inner); outer.Inlines.Add(middle); nestedParagraph.Inlines.Add(outer);
        nestedParagraph.Inlines.Add(new Run(" + "));
        nestedParagraph.Inlines.Add(new Hyperlink(new Run("링크 표시 문자열")));
        nestedEditor.Document = new FlowDocument(nestedParagraph);
        var nestedModel = RichTextConverter.FromEditor(nestedEditor);
        Check(string.Concat(nestedModel.Paragraphs[0].Runs.Select(r => r.Text)) == "삼중 중첩 + 링크 표시 문자열", "V100-P01 중첩/Hyperlink 본문 보존");
        var nestedRun = nestedModel.Paragraphs[0].Runs[0];
        Check(nestedRun.Bold && nestedRun.Italic && nestedRun.Underline && Math.Abs(nestedRun.FontSize - 18) < 0.01, "V100-P01 부모 서식 상속", $"B={nestedRun.Bold}, I={nestedRun.Italic}, U={nestedRun.Underline}, Size={nestedRun.FontSize}");

        var editorRoundTrip = new RichTextBox(); RichTextConverter.ToEditor(nestedModel, editorRoundTrip);
        var convertedAgain = RichTextConverter.FromEditor(editorRoundTrip);
        Check(string.Concat(convertedAgain.Paragraphs[0].Runs.Select(r => r.Text)) == "삼중 중첩 + 링크 표시 문자열", "V100-P03 RichTextBox 왕복 본문");
        var againRun = convertedAgain.Paragraphs[0].Runs[0];
        Check(againRun.Bold && againRun.Italic && againRun.Underline, "V100-P03 RichTextBox 왕복 서식", $"B={againRun.Bold}, I={againRun.Italic}, U={againRun.Underline}");

        // V100-P02/P03: 비지원 블록은 감지하며 변환/저장을 진행하지 않는다.
        foreach (var (name, block) in UnsupportedBlocks())
        {
            var editor = new RichTextBox { Document = new FlowDocument(block) };
            var detected = RichTextConverter.FindUnsupportedBlocks(editor.Document);
            Check(detected.Contains(name), $"V100-P02 {name} 감지");
            try { RichTextConverter.FromEditor(editor); Check(false, $"V100-P02 {name} 저장 차단"); }
            catch (UnsupportedEditorContentException ex) { Check(ex.BlockTypes.Contains(name), $"V100-P02 {name} 저장 차단"); }
        }

        Dispatcher.CurrentDispatcher.InvokeShutdown();
        Console.WriteLine($"총 {passes}건 통과, {failures.Count}건 실패");
        return failures.Count == 0 ? 0 : 1;
    }
    finally
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }
}

static IEnumerable<(string Name, Block Block)> UnsupportedBlocks()
{
    var section = new Section(new Paragraph(new Run("Section 본문")));
    var list = new List(); var item = new ListItem(); item.Blocks.Add(new Paragraph(new Run("목록 본문"))); list.ListItems.Add(item);
    var table = new Table(); var group = new TableRowGroup(); var row = new TableRow(); var cell = new TableCell(new Paragraph(new Run("표 본문"))); row.Cells.Add(cell); group.Rows.Add(row); table.RowGroups.Add(group);
    return [(nameof(Section), section), (nameof(List), list), (nameof(Table), table)];
}
