namespace SimpleWordEditor;

public enum UserErrorKind { FileNotFound, Permission, FileInUse, CorruptDocument, DiskFull, Unsupported, WordInterop, Unknown }
public sealed record UserError(UserErrorKind Kind, string Action);

public static class UserErrorClassifier
{
    public static UserError Classify(Exception exception)
    {
        foreach (var ex in Chain(exception))
        {
            if (ex is WordInteropException) return new(UserErrorKind.WordInterop, "Microsoft Word가 응답하는지 확인하고 문서를 닫은 뒤 다시 시도해 주세요.");
            if (ex is UnsupportedEditorContentException unsupported)
                return new(UserErrorKind.Unsupported, $"지원하지 않는 편집 요소({string.Join(", ", unsupported.BlockTypes)})가 있습니다. 일반 텍스트로 다시 붙여넣어 주세요.");
            if (ex is NotSupportedException) return new(UserErrorKind.Unsupported, ex.Message);
            if (ex is FileNotFoundException or DirectoryNotFoundException) return new(UserErrorKind.FileNotFound, "파일이 이동되거나 삭제되었는지 확인해 주세요.");
            if (ex is UnauthorizedAccessException) return new(UserErrorKind.Permission, "파일 또는 폴더의 쓰기 권한을 확인하거나 다른 위치를 선택해 주세요.");
            if (ex is IOException io && IsDiskFull(io.HResult)) return new(UserErrorKind.DiskFull, "디스크의 여유 공간을 확보한 뒤 다시 저장해 주세요.");
            if (ex is IOException locked && IsSharingViolation(locked.HResult)) return new(UserErrorKind.FileInUse, "다른 프로그램에서 이 파일을 닫은 뒤 다시 시도해 주세요.");
        }
        if (exception is InvalidDataException) return new(UserErrorKind.CorruptDocument, "DOCX가 손상되었거나 암호화되었을 수 있습니다. 원본을 다른 프로그램에서 확인해 주세요.");
        return new(UserErrorKind.Unknown, "다시 시도하거나 다른 파일을 선택해 주세요.");
    }

    private static IEnumerable<Exception> Chain(Exception ex) { for (Exception? current = ex; current != null; current = current.InnerException) yield return current; }
    private static bool IsDiskFull(int hresult) => (hresult & 0xFFFF) is 0x27 or 0x70;
    private static bool IsSharingViolation(int hresult) => (hresult & 0xFFFF) is 0x20 or 0x21;
}

public sealed class WordInteropException(string message, Exception innerException) : Exception(message, innerException);
