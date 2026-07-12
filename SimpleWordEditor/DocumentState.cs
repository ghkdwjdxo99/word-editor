namespace SimpleWordEditor;

public enum DocumentFormat { Docx, Doc }
public sealed class DocumentState
{
    public string? Path { get; set; }
    public DocumentFormat Format { get; set; } = DocumentFormat.Docx;
    public bool IsDirty { get; set; }
    public bool HasUnsupportedContent { get; set; }
    public bool UnsupportedWarningShown { get; set; }
    public string DisplayName => Path is null ? "제목 없음" : System.IO.Path.GetFileName(Path);
}
