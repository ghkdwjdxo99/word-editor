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

        // V100-P06: 선택한 저장 형식과 실제 확장자가 항상 일치한다.
        Check(SaveTargetResolver.Resolve("문서", DocumentFormat.Docx, false).Path.EndsWith("문서.docx"), "V100-P06 무확장 DOCX 보정");
        var wrongExtension = SaveTargetResolver.Resolve("문서.txt", DocumentFormat.Docx, false);
        Check(wrongExtension.Result == SaveTargetResult.ConfirmationRequired && wrongExtension.Path.EndsWith("문서.docx"), "V100-P06 잘못된 확장자 확인 요구");
        Check(SaveTargetResolver.Resolve("문서.txt", DocumentFormat.Docx, false, true).Result == SaveTargetResult.Accepted, "V100-P06 확장자 확인 후 보정");
        Check(SaveTargetResolver.Resolve("문서.doc", DocumentFormat.Doc, false).Result == SaveTargetResult.UnsupportedDoc, "V100-P06 Word 없는 DOC 차단");
        Check(SaveTargetResolver.Resolve("문서.DOC", DocumentFormat.Doc, true).Result == SaveTargetResult.Accepted, "V100-P06 DOC 대소문자 허용");

        // V100-P07: 별표는 마지막 저장 이후 변경된 경우에만 표시한다.
        var state = new DocumentState();
        Check(state.WindowTitle == "제목 없음 - 간단 워드 편집기", "V100-P07 새 빈 문서 별표 없음");
        state.IsDirty = true; Check(state.WindowTitle == "제목 없음* - 간단 워드 편집기", "V100-P07 편집 후 별표 표시");
        state.Path = "저장 문서.docx"; state.IsDirty = false; Check(state.WindowTitle == "저장 문서.docx - 간단 워드 편집기", "V100-P07 저장 후 별표 제거");

        // V100-P08: 선택 영역의 지원 서식과 혼합 상태를 읽는다.
        var formatEditor = new RichTextBox();
        var left = new Paragraph { TextAlignment = System.Windows.TextAlignment.Left };
        left.Inlines.Add(new Run("굵은 밑줄") { FontWeight = System.Windows.FontWeights.Bold, TextDecorations = System.Windows.TextDecorations.Underline, FontSize = 16 * 96 / 72 });
        var right = new Paragraph(new Run("일반")) { TextAlignment = System.Windows.TextAlignment.Right };
        formatEditor.Document = new FlowDocument(); formatEditor.Document.Blocks.Add(left); formatEditor.Document.Blocks.Add(right);
        formatEditor.Selection.Select(left.ContentStart, left.ContentEnd);
        var singleFormat = EditorFormatState.Read(formatEditor);
        Check(singleFormat.Bold == true && singleFormat.Underline == true && singleFormat.FontSize == 16 && singleFormat.Alignment == System.Windows.TextAlignment.Left, "V100-P08 단일 서식 상태");
        Check(singleFormat.FontSize == 16, "BUG-V100-002 16pt 도구 모음 표시", $"Actual={singleFormat.FontSize}");
        formatEditor.Selection.Select(left.ContentStart, right.ContentEnd);
        var mixedFormat = EditorFormatState.Read(formatEditor);
        Check(mixedFormat.Bold is null && mixedFormat.Underline is null && mixedFormat.Alignment is null, "V100-P08 혼합 서식 상태");

        // V100-P09: 감싼 내부 예외까지 확인해 사용자 행동별 오류를 분류한다.
        Check(UserErrorClassifier.Classify(new InvalidDataException("outer", new UnauthorizedAccessException())).Kind == UserErrorKind.Permission, "V100-P09 내부 권한 오류 분류");
        Check(UserErrorClassifier.Classify(new TestIOException(unchecked((int)0x80070020))).Kind == UserErrorKind.FileInUse, "V100-P09 파일 점유 분류");
        Check(UserErrorClassifier.Classify(new TestIOException(unchecked((int)0x80070070))).Kind == UserErrorKind.DiskFull, "V100-P09 공간 부족 분류");
        Check(UserErrorClassifier.Classify(new InvalidDataException()).Kind == UserErrorKind.CorruptDocument, "V100-P09 손상 문서 분류");
        Check(UserErrorClassifier.Classify(new WordInteropException("word", new Exception())).Kind == UserErrorKind.WordInterop, "V100-P09 Word 연동 오류 분류");
        var unsupportedEditorError = UserErrorClassifier.Classify(new UnsupportedEditorContentException(["Table"]));
        Check(unsupportedEditorError.Kind == UserErrorKind.Unsupported && unsupportedEditorError.Action.Contains("Table"), "V100-P02 비지원 편집 블록 사용자 안내", $"Kind={unsupportedEditorError.Kind}, Action={unsupportedEditorError.Action}");

        // V110-F01: UI와 분리된 검색 엔진 및 WPF 선택 어댑터.
        Check(FindEngine.FindAll("한글 찾기 한글", "한글", false).Count == 2, "V110-F01 한글 검색");
        Check(FindEngine.FindAll("English 123 공백 검색", "123", false).Single().Start == 8, "V110-F01 영문/숫자 검색");
        Check(FindEngine.FindAll("공백 포함 검색", "포함 검", false).Count == 1, "V110-F01 공백 검색");
        Check(FindEngine.FindAll("abc", "없음", false).Count == 0, "V110-F01 결과 없음");
        Check(FindEngine.FindAll("abc", "", false).Count == 0, "V110-F01 빈 검색어");
        Check(FindEngine.FindAll("Word word WORD", "word", false).Count == 3 && FindEngine.FindAll("Word word WORD", "word", true).Count == 1, "V110-F01 대소문자 옵션");
        var navigation = new FindEngine(); navigation.Search("하나 둘 하나", "하나", false);
        var first = navigation.Next(); var second = navigation.Next(); var wrappedNext = navigation.Next(); var wrappedPrevious = navigation.Previous();
        Check(first?.Start == 0 && second?.Start == 5, "V110-F01 다음 결과 이동");
        Check(wrappedNext?.Start == 0 && wrappedPrevious?.Start == 5, "V110-F01 앞뒤 순환");

        var searchEditor = new RichTextBox();
        var searchP1 = new Paragraph(); searchP1.Inlines.Add(new Run("앞 ")); searchP1.Inlines.Add(new Bold(new Run("중첩 검색")));
        var searchP2 = new Paragraph(new Run("다음 문단")); searchEditor.Document = new FlowDocument(); searchEditor.Document.Blocks.Add(searchP1); searchEditor.Document.Blocks.Add(searchP2);
        var snapshot = FlowDocumentSearchSnapshot.Create(searchEditor.Document);
        Check(snapshot.Text == "앞 중첩 검색\n다음 문단", "V110-F01 표시 본문 스냅샷 및 문단 경계", $"Actual={snapshot.Text}");
        Check(FindEngine.FindAll(snapshot.Text, "검색다음", false).Count == 0, "V110-F01 문단 경계 비연결");
        var textChangedBySearch = false; searchEditor.TextChanged += (_, _) => textChangedBySearch = true;
        var beforeSearch = snapshot.Text; var searchMatch = FindEngine.FindAll(snapshot.Text, "중첩 검색", false).Single();
        Check(snapshot.Select(searchEditor, searchMatch), "V110-F01 검색 결과 선택");
        var afterSearch = FlowDocumentSearchSnapshot.Create(searchEditor.Document).Text;
        var unchangedState = new DocumentState { IsDirty = false };
        Check(!textChangedBySearch && beforeSearch == afterSearch && !unchangedState.IsDirty, "V110-F01 검색 후 본문/변경 상태 미변경");

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

sealed class TestIOException : IOException
{
    public TestIOException(int hresult) => HResult = hresult;
}
