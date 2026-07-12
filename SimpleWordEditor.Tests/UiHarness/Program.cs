using SimpleWordEditor;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

internal static class Program
{
    private const uint BM_CLICK = 0x00F5;
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder text, int max);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);

    [STAThread]
    private static void Main()
    {
        var app = new Application();
        var window = new MainWindow(); window.Show(); window.Activate();
        var editor = (RichTextBox)window.FindName("Editor");
        var state = (DocumentState)typeof(MainWindow).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        var save = typeof(MainWindow).GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var path = Path.Combine(Path.GetTempPath(), "SimpleWordEditor-ui-original.docx");
        var original = Encoding.UTF8.GetBytes("ORIGINAL-CONTENT");
        foreach (var kind in new[] { "Table", "List", "Section" })
        {
            editor.Document = CreateDocument(kind); File.WriteAllBytes(path, original);
            state.Path = path; state.Format = DocumentFormat.Docx; state.IsDirty = true;
            string captured = "";
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (_, _) => { if (TryCaptureDialog(out captured)) timer.Stop(); };
            timer.Start(); var result = (bool)save.Invoke(window, new object[] { false })!;
            Console.WriteLine($"{kind}|Save={result}|Cause={captured.Contains(kind)}|Resolution={captured.Contains("일반 텍스트") || captured.Contains("제거")}|Original={File.ReadAllBytes(path).SequenceEqual(original)}|Text={captured}");
        }

        editor.Document = new FlowDocument(new Paragraph(new Run("수동 저장 본문")));
        var seed = new DocumentModel(); var seedParagraph = new DocumentParagraph(); seedParagraph.Runs.Add(new TextRun("이전 본문")); seed.Paragraphs.Add(seedParagraph); DocxService.Write(seed, path);
        state.Path = path; state.Format = DocumentFormat.Docx; state.IsDirty = true;
        var saveExisting = (bool)save.Invoke(window, new object[] { false })!;
        Console.WriteLine($"Save|Result={saveExisting}|Dirty={state.IsDirty}|Content={string.Concat(DocxService.Read(path).Paragraphs.SelectMany(p => p.Runs).Select(r => r.Text))}");

        var newDocument = typeof(MainWindow).GetMethod("NewDocument", BindingFlags.Instance | BindingFlags.NonPublic)!;
        editor.Document = new FlowDocument(new Paragraph(new Run("취소 보존"))); state.IsDirty = true;
        string choiceText = ""; var cancelTimer = DialogTimer("취소", x => choiceText = x); cancelTimer.Start(); newDocument.Invoke(window, new object[] { false });
        Console.WriteLine($"DiscardCancel|Dialog={choiceText.Contains("저장")}|Preserved={EditorText(editor).Contains("취소 보존")}|Dirty={state.IsDirty}");
        var noTimer = DialogTimer("아니요", _ => { }); noTimer.Start(); newDocument.Invoke(window, new object[] { false });
        Console.WriteLine($"DiscardNo|Cleared={!EditorText(editor).Contains("취소 보존")}|Dirty={state.IsDirty}|PathNull={state.Path is null}");
        editor.Document = new FlowDocument(new Paragraph(new Run("저장 선택 본문"))); state.Path = path; state.Format = DocumentFormat.Docx; state.IsDirty = true;
        var yesTimer = DialogTimer("예", _ => { }); yesTimer.Start(); newDocument.Invoke(window, new object[] { false });
        Console.WriteLine($"DiscardYes|Saved={string.Concat(DocxService.Read(path).Paragraphs.SelectMany(p => p.Runs).Select(r => r.Text)).Contains("저장 선택 본문")}|NewDocument={!EditorText(editor).Contains("저장 선택 본문")}|Dirty={state.IsDirty}");

        var saveAsText = ""; var saveAsTimer = DialogTimer("취소", x => saveAsText = x); saveAsTimer.Start(); var saveAs = (bool)save.Invoke(window, new object[] { true })!;
        Console.WriteLine($"SaveAs|DialogShown={!string.IsNullOrWhiteSpace(saveAsText)}|Cancelled={!saveAs}");
        var open = typeof(MainWindow).GetMethod("OpenDocument", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var openText = ""; var openTimer = DialogTimer("취소", x => openText = x); openTimer.Start(); open.Invoke(window, null);
        Console.WriteLine($"Open|DialogShown={!string.IsNullOrWhiteSpace(openText)}");

        var p1 = new Paragraph(new Run("A") { FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic, TextDecorations = TextDecorations.Underline, FontSize = 16 * 96 / 72 }) { TextAlignment = TextAlignment.Center };
        var p2 = new Paragraph(new Run("B")) { TextAlignment = TextAlignment.Right };
        editor.Document = new FlowDocument(); editor.Document.Blocks.Add(p1); editor.Document.Blocks.Add(p2);
        editor.Selection.Select(p1.ContentStart, p1.ContentEnd); Pump();
        Console.WriteLine($"ToolbarSingle|Bold={Toggle(window,"BoldButton")}|Italic={Toggle(window,"ItalicButton")}|Underline={Toggle(window,"UnderlineButton")}|Size={((ComboBox)window.FindName("FontSizeBox")).Text}|Center={Toggle(window,"CenterButton")}");
        editor.Selection.Select(p1.ContentStart, p2.ContentEnd); Pump();
        Console.WriteLine($"ToolbarMixed|Bold={Toggle(window,"BoldButton")}|Italic={Toggle(window,"ItalicButton")}|Underline={Toggle(window,"UnderlineButton")}|SizeEmpty={string.IsNullOrEmpty(((ComboBox)window.FindName("FontSizeBox")).Text)}|Left={Toggle(window,"LeftButton")}|Center={Toggle(window,"CenterButton")}|Right={Toggle(window,"RightButton")}");

        editor.Document = new FlowDocument(new Paragraph(new Run("찾기 Search 찾기 123"))); state.IsDirty = false;
        var findSnapshot = FlowDocumentSearchSnapshot.Create(editor.Document); findSnapshot.Select(editor, FindEngine.FindAll(findSnapshot.Text, "Search", true).Single());
        var ctrlF = window.InputBindings.OfType<System.Windows.Input.KeyBinding>().Single(x => x.Key == System.Windows.Input.Key.F && x.Modifiers == System.Windows.Input.ModifierKeys.Control);
        ctrlF.Command.Execute(null); Pump();
        var findPanel = (FrameworkElement)window.FindName("FindPanel"); var findBox = (TextBox)window.FindName("FindTextBox"); var findStatus = (TextBlock)window.FindName("FindStatusText");
        findBox.Text = "찾기"; Pump();
        var moveFind = typeof(MainWindow).GetMethod("MoveFind", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var firstFind = editor.Selection.Text; moveFind.Invoke(window, new object[] { true }); Pump();
        var secondFind = editor.Selection.Text; moveFind.Invoke(window, new object[] { false }); Pump();
        var previousFind = editor.Selection.Text;
        Console.WriteLine($"Find|CtrlF={findPanel.Visibility == Visibility.Visible}|First={firstFind}|Next={secondFind}|Previous={previousFind}|Status={findStatus.Text}|Dirty={state.IsDirty}|Body={EditorText(editor).Contains("찾기 Search 찾기 123")}");
        typeof(MainWindow).GetMethod("CloseFind", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null); Pump();
        Console.WriteLine($"FindClose|Closed={findPanel.Visibility == Visibility.Collapsed}|SelectionRestored={editor.Selection.Text == "Search"}|Dirty={state.IsDirty}|Body={EditorText(editor).Contains("찾기 Search 찾기 123")}");

        state.IsDirty = true; var closeCancel = DialogTimer("취소", _ => { }); closeCancel.Start(); window.Close(); Console.WriteLine($"CloseCancel|Visible={window.IsVisible}");
        var closeNo = DialogTimer("아니요", _ => { }); closeNo.Start(); window.Close(); Console.WriteLine($"CloseNo|Closed={!window.IsVisible}");
        app.Shutdown();
    }

    private static FlowDocument CreateDocument(string kind)
    {
        Block block;
        if (kind == "Table") { var t = new Table(); var g = new TableRowGroup(); var r = new TableRow(); r.Cells.Add(new TableCell(new Paragraph(new Run("표 본문")))); g.Rows.Add(r); t.RowGroups.Add(g); block = t; }
        else if (kind == "List") { var l = new List(); var i = new ListItem(); i.Blocks.Add(new Paragraph(new Run("목록 본문"))); l.ListItems.Add(i); block = l; }
        else block = new Section(new Paragraph(new Run("섹션 본문")));
        return new FlowDocument(block);
    }

    private static bool TryCaptureDialog(out string text)
    {
        var pieces = new List<string>(); IntPtr button = IntPtr.Zero; IntPtr dialog = IntPtr.Zero;
        EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var pid); if (pid == Environment.ProcessId && Class(h) == "#32770") { dialog = h; return false; } return true; }, IntPtr.Zero);
        if (dialog == IntPtr.Zero) { text = ""; return false; }
        EnumChildWindows(dialog, (h, _) => { var value = Text(h); if (!string.IsNullOrWhiteSpace(value)) pieces.Add(value); if (button == IntPtr.Zero && Class(h) == "Button") button = h; return true; }, IntPtr.Zero);
        text = string.Join(" | ", pieces.Distinct()); if (button != IntPtr.Zero) SendMessage(button, BM_CLICK, IntPtr.Zero, IntPtr.Zero); return true;
    }
    private static DispatcherTimer DialogTimer(string wanted, Action<string> captured)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) => { if (TryDialogButton(wanted, out var text)) { captured(text); timer.Stop(); } };
        return timer;
    }
    private static bool TryDialogButton(string wanted, out string text)
    {
        var pieces = new List<string>(); var buttons = new List<(IntPtr Handle,string Text)>(); IntPtr dialog = IntPtr.Zero;
        EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var pid); if (pid == Environment.ProcessId && Class(h) == "#32770") { dialog = h; return false; } return true; }, IntPtr.Zero);
        if (dialog == IntPtr.Zero) { text = ""; return false; }
        EnumChildWindows(dialog, (h, _) => { var value = Text(h); if (!string.IsNullOrWhiteSpace(value)) pieces.Add(value); if (Class(h) == "Button") buttons.Add((h,value)); return true; }, IntPtr.Zero);
        text = string.Join(" | ", pieces.Distinct()); var button = buttons.FirstOrDefault(x => x.Text.Contains(wanted)); if (button.Handle == IntPtr.Zero && wanted == "취소") button = buttons.LastOrDefault();
        if (button.Handle == IntPtr.Zero) return false; SendMessage(button.Handle, BM_CLICK, IntPtr.Zero, IntPtr.Zero); return true;
    }
    private static string EditorText(RichTextBox editor) => new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text;
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
    private static string Toggle(MainWindow window, string name) { var value = ((System.Windows.Controls.Primitives.ToggleButton)window.FindName(name)).IsChecked; return value.HasValue ? value.Value.ToString() : "Mixed"; }
    private static string Text(IntPtr h) { var b = new StringBuilder(1024); GetWindowText(h, b, b.Capacity); return b.ToString(); }
    private static string Class(IntPtr h) { var b = new StringBuilder(128); GetClassName(h, b, b.Capacity); return b.ToString(); }
}
