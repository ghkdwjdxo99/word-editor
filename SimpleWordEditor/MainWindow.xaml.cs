using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace SimpleWordEditor;

public partial class MainWindow : Window
{
    private readonly DocumentState state = new();
    private readonly WordInteropService word = new();
    private bool loading;
    private bool syncingToolbar;
    private readonly FindEngine findEngine = new();
    private FlowDocumentSearchSnapshot? findSnapshot;
    private TextPointer? selectionBeforeFindStart;
    private TextPointer? selectionBeforeFindEnd;

    public MainWindow()
    {
        InitializeComponent();
        CommandBindings.Add(new(ApplicationCommands.Undo, (_, _) => { if (Editor.CanUndo) Editor.Undo(); }, (_, e) => e.CanExecute = Editor.CanUndo));
        CommandBindings.Add(new(ApplicationCommands.Redo, (_, _) => { if (Editor.CanRedo) Editor.Redo(); }, (_, e) => e.CanExecute = Editor.CanRedo));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => NewDocument()), Key.N, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenDocument()), Key.O, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => Save()), Key.S, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => Save(true)), Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => OpenFind()), Key.F, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => Toggle(TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal)), Key.B, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => Toggle(TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal)), Key.I, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => Toggle(Inline.TextDecorationsProperty, TextDecorations.Underline, null)), Key.U, ModifierKeys.Control));
        NewDocument(force: true);
    }

    private void NewDocument(bool force = false)
    {
        if (!force && !ConfirmDiscard()) return;
        loading = true; RichTextConverter.ToEditor(new DocumentModel { Paragraphs = { new DocumentParagraph() } }, Editor); loading = false;
        state.Path = null; state.Format = DocumentFormat.Docx; state.IsDirty = false; state.HasUnsupportedContent = false; state.UnsupportedWarningShown = false; UpdateChrome(); Editor.Focus();
    }
    private void OpenDocument()
    {
        if (!ConfirmDiscard()) return;
        var dialog = new OpenFileDialog { Filter = "Word 문서 (*.docx;*.doc)|*.docx;*.doc|DOCX 문서 (*.docx)|*.docx|DOC 문서 (*.doc)|*.doc" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var isDoc = Path.GetExtension(dialog.FileName).Equals(".doc", StringComparison.OrdinalIgnoreCase);
            string readPath = dialog.FileName; string? temp = null;
            if (isDoc) { if (!word.IsAvailable) throw new NotSupportedException("DOC 파일을 사용하려면 Microsoft Word가 설치되어 있어야 합니다. DOCX 파일은 Word 없이 사용할 수 있습니다."); temp = Path.Combine(Path.GetTempPath(), $"SimpleWordEditor-{Guid.NewGuid():N}.docx"); word.Convert(dialog.FileName, temp, true); readPath = temp; }
            try
            {
                var model = DocxService.Read(readPath); loading = true; RichTextConverter.ToEditor(model, Editor); loading = false;
                state.Path = dialog.FileName; state.Format = isDoc ? DocumentFormat.Doc : DocumentFormat.Docx; state.IsDirty = false; state.HasUnsupportedContent = model.HasUnsupportedContent; state.UnsupportedWarningShown = false; UpdateChrome();
                if (model.HasUnsupportedContent) MessageBox.Show(this, "이 문서에는 지원하지 않는 요소가 있습니다. 본문과 지원 서식만 표시되며 저장하면 나머지 요소가 제거될 수 있습니다.", "호환성 경고", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { if (temp != null && File.Exists(temp)) File.Delete(temp); }
        }
        catch (Exception ex) { loading = false; AppLogger.Error(ex); ShowError("문서를 열지 못했습니다", ex); }
    }

    private bool Save(bool saveAs = false)
    {
        if (state.HasUnsupportedContent && !state.UnsupportedWarningShown)
        {
            var choice = MessageBox.Show(this, "이 문서에는 지원하지 않는 요소가 포함되어 있습니다. 저장하면 해당 요소가 제거될 수 있습니다.\n\n원본 보호를 위해 다른 이름으로 저장하시겠습니까?", "호환성 경고", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Yes);
            if (choice == MessageBoxResult.Cancel) return false; state.UnsupportedWarningShown = true; if (choice == MessageBoxResult.Yes) saveAs = true;
        }
        var target = state.Path;
        if (saveAs || target is null)
        {
            var filter = word.IsAvailable ? "DOCX 문서 (*.docx)|*.docx|DOC 문서 (*.doc)|*.doc" : "DOCX 문서 (*.docx)|*.docx";
            var dialog = new SaveFileDialog { Filter = filter, DefaultExt = ".docx", AddExtension = false, FileName = state.DisplayName == "제목 없음" ? "새 문서.docx" : state.DisplayName };
            if (dialog.ShowDialog(this) != true) return false;
            var selectedFormat = dialog.FilterIndex == 2 ? DocumentFormat.Doc : DocumentFormat.Docx;
            target = ResolveSaveTarget(dialog.FileName, selectedFormat);
            if (target is null) return false;
        }
        else target = ResolveSaveTarget(target, state.Format);
        if (target is null) return false;
        try
        {
            var model = RichTextConverter.FromEditor(Editor);
            var isDoc = Path.GetExtension(target).Equals(".doc", StringComparison.OrdinalIgnoreCase);
            if (isDoc)
            {
                if (!word.IsAvailable) throw new NotSupportedException("DOC 저장에는 Microsoft Word가 필요합니다. DOCX로 저장해 주세요.");
                var docxTemp = Path.Combine(Path.GetTempPath(), $"SimpleWordEditor-{Guid.NewGuid():N}.docx");
                try { DocxService.Write(model, docxTemp); DocxService.Validate(docxTemp); SafeFileSaver.Save(target!, p => word.Convert(docxTemp, p, false), p => { if (!File.Exists(p) || new FileInfo(p).Length == 0) throw new InvalidDataException("DOC 변환 결과가 올바르지 않습니다."); }); }
                finally { if (File.Exists(docxTemp)) File.Delete(docxTemp); }
            }
            else SafeFileSaver.Save(target!, p => DocxService.Write(model, p), DocxService.Validate);
            state.Path = target; state.Format = isDoc ? DocumentFormat.Doc : DocumentFormat.Docx; state.IsDirty = false; UpdateChrome(); return true;
        }
        catch (UnsupportedEditorContentException ex)
        {
            AppLogger.Error(ex);
            MessageBox.Show(this, $"표, 목록 또는 Section처럼 아직 지원하지 않는 블록이 편집 화면에 있습니다 ({string.Join(", ", ex.BlockTypes)}).\n\n화면의 본문이 유실되지 않도록 저장을 중단했습니다. 해당 내용을 일반 텍스트로 다시 붙여넣은 후 저장해 주세요.", "저장 차단", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        catch (Exception ex) { AppLogger.Error(ex); ShowError("문서를 저장하지 못했습니다", ex); return false; }
    }

    private bool ConfirmDiscard()
    {
        if (!state.IsDirty) return true;
        var result = MessageBox.Show(this, "변경 내용을 저장하시겠습니까?", "간단 워드 편집기", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        return result == MessageBoxResult.No || (result == MessageBoxResult.Yes && Save());
    }
    private string? ResolveSaveTarget(string candidate, DocumentFormat format)
    {
        var decision = SaveTargetResolver.Resolve(candidate, format, word.IsAvailable);
        if (decision.Result == SaveTargetResult.UnsupportedDoc) { MessageBox.Show(this, decision.Message!, "지원하지 않는 저장 형식", MessageBoxButton.OK, MessageBoxImage.Information); return null; }
        if (decision.Result == SaveTargetResult.ConfirmationRequired)
        {
            if (MessageBox.Show(this, decision.Message!, "확장자 확인", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return null;
            decision = SaveTargetResolver.Resolve(candidate, format, word.IsAvailable, confirmReplacement: true);
        }
        return decision.Path;
    }
    private void UpdateChrome() { Title = state.WindowTitle; StatusText.Text = $"형식: {state.Format.ToString().ToUpperInvariant()}   |   Word 연동: {(word.IsAvailable ? "사용 가능" : "사용 불가")}"; }
    private void Toggle(DependencyProperty property, object on, object? off) { var current = Editor.Selection.GetPropertyValue(property); Editor.Selection.ApplyPropertyValue(property, Equals(current, on) ? off ?? DependencyProperty.UnsetValue : on); Editor.Focus(); }
    private void Align(TextAlignment value) { foreach (var p in Editor.Selection.Start.Paragraph is { } start ? Editor.Document.Blocks.OfType<Paragraph>().Where(p => p.ContentStart.CompareTo(Editor.Selection.End) <= 0 && p.ContentEnd.CompareTo(Editor.Selection.Start) >= 0) : []) p.TextAlignment = value; Editor.Focus(); }
    private void ApplyFontSize()
    {
        var text = FontSizeBox.Text; if (FontSizeBox.SelectedItem is ComboBoxItem item) text = item.Content?.ToString() ?? text;
        if (double.TryParse(text, out var points) && points is >= 8 and <= 72) Editor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, points * 96 / 72); else MessageBox.Show(this, "글자 크기는 8~72pt 사이여야 합니다.", "글자 크기", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private static void ShowError(string title, Exception ex) { var error = UserErrorClassifier.Classify(ex); MessageBox.Show($"{title}.\n{error.Action}", title, MessageBoxButton.OK, MessageBoxImage.Error); }

    private void OpenFind()
    {
        if (FindPanel.Visibility != Visibility.Visible)
        {
            selectionBeforeFindStart = Editor.Selection.Start;
            selectionBeforeFindEnd = Editor.Selection.End;
            FindPanel.Visibility = Visibility.Visible;
            RefreshFind(moveToFirst: false);
        }
        FindTextBox.Focus(); FindTextBox.SelectAll();
    }

    private void CloseFind()
    {
        FindPanel.Visibility = Visibility.Collapsed;
        if (selectionBeforeFindStart != null && selectionBeforeFindEnd != null)
        {
            try { Editor.Selection.Select(selectionBeforeFindStart, selectionBeforeFindEnd); } catch (InvalidOperationException) { }
        }
        selectionBeforeFindStart = selectionBeforeFindEnd = null;
        Editor.Focus();
    }

    private void RefreshFind(bool moveToFirst)
    {
        findSnapshot = FlowDocumentSearchSnapshot.Create(Editor.Document);
        findEngine.Search(findSnapshot.Text, FindTextBox.Text, MatchCaseCheckBox.IsChecked == true);
        if (string.IsNullOrEmpty(FindTextBox.Text)) { FindStatusText.Text = "검색어를 입력하세요."; return; }
        if (findEngine.Matches.Count == 0) { FindStatusText.Text = "검색 결과가 없습니다."; return; }
        FindStatusText.Text = $"{findEngine.Matches.Count}개 결과";
        if (moveToFirst) MoveFind(next: true);
    }

    private void MoveFind(bool next)
    {
        if (findSnapshot is null || findEngine.Matches.Count == 0) { RefreshFind(moveToFirst: false); if (findEngine.Matches.Count == 0) return; }
        var match = next ? findEngine.Next() : findEngine.Previous();
        if (match is null || !findSnapshot!.Select(Editor, match.Value)) return;
        FindStatusText.Text = $"{findEngine.CurrentIndex + 1}/{findEngine.Matches.Count}";
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e) { if (!loading) { state.IsDirty = true; UpdateChrome(); if (FindPanel.Visibility == Visibility.Visible) RefreshFind(moveToFirst: false); } }
    private void Editor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        var format = EditorFormatState.Read(Editor); syncingToolbar = true;
        try
        {
            BoldButton.IsChecked = format.Bold; ItalicButton.IsChecked = format.Italic; UnderlineButton.IsChecked = format.Underline;
            FontSizeBox.SelectedIndex = -1; FontSizeBox.Text = format.FontSize?.ToString("0.##") ?? "";
            LeftButton.IsChecked = format.Alignment is null ? null : format.Alignment == TextAlignment.Left;
            CenterButton.IsChecked = format.Alignment is null ? null : format.Alignment == TextAlignment.Center;
            RightButton.IsChecked = format.Alignment is null ? null : format.Alignment == TextAlignment.Right;
        }
        finally { syncingToolbar = false; }
    }
    private void New_Click(object s, RoutedEventArgs e) => NewDocument(); private void Open_Click(object s, RoutedEventArgs e) => OpenDocument(); private void Save_Click(object s, RoutedEventArgs e) => Save(); private void SaveAs_Click(object s, RoutedEventArgs e) => Save(true); private void Exit_Click(object s, RoutedEventArgs e) => Close();
    private void Bold_Click(object s, RoutedEventArgs e) => Toggle(TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal); private void Italic_Click(object s, RoutedEventArgs e) => Toggle(TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal); private void Underline_Click(object s, RoutedEventArgs e) => Toggle(Inline.TextDecorationsProperty, TextDecorations.Underline, null);
    private void Left_Click(object s, RoutedEventArgs e) => Align(TextAlignment.Left); private void Center_Click(object s, RoutedEventArgs e) => Align(TextAlignment.Center); private void Right_Click(object s, RoutedEventArgs e) => Align(TextAlignment.Right);
    private void FontSize_Changed(object s, SelectionChangedEventArgs e) { if (IsLoaded && !syncingToolbar) ApplyFontSize(); } private void FontSize_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { ApplyFontSize(); e.Handled = true; } }
    private void Find_Click(object s, RoutedEventArgs e) => OpenFind();
    private void CloseFind_Click(object s, RoutedEventArgs e) => CloseFind();
    private void FindNext_Click(object s, RoutedEventArgs e) => MoveFind(next: true);
    private void FindPrevious_Click(object s, RoutedEventArgs e) => MoveFind(next: false);
    private void FindText_Changed(object s, TextChangedEventArgs e) { if (IsLoaded) RefreshFind(moveToFirst: true); }
    private void FindOptions_Changed(object s, RoutedEventArgs e) { if (IsLoaded) RefreshFind(moveToFirst: true); }
    private void FindText_KeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { CloseFind(); e.Handled = true; }
        else if (e.Key == Key.Enter) { MoveFind(next: (Keyboard.Modifiers & ModifierKeys.Shift) == 0); e.Handled = true; }
    }
    private void Window_Closing(object? s, CancelEventArgs e) { if (!ConfirmDiscard()) e.Cancel = true; }
}

internal sealed class RelayCommand(Action<object?> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute(parameter);
}
