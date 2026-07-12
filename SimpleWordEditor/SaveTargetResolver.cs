namespace SimpleWordEditor;

public enum SaveTargetResult { Accepted, ConfirmationRequired, UnsupportedDoc }
public sealed record SaveTargetDecision(SaveTargetResult Result, string Path, string? Message = null);

public static class SaveTargetResolver
{
    public static SaveTargetDecision Resolve(string path, DocumentFormat selectedFormat, bool wordAvailable, bool confirmReplacement = false)
    {
        var requiredExtension = selectedFormat == DocumentFormat.Doc ? ".doc" : ".docx";
        if (selectedFormat == DocumentFormat.Doc && !wordAvailable)
            return new(SaveTargetResult.UnsupportedDoc, path, "DOC 저장에는 Microsoft Word가 필요합니다. DOCX 형식을 선택해 주세요.");

        var extension = System.IO.Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension)) return new(SaveTargetResult.Accepted, path + requiredExtension);
        if (extension.Equals(requiredExtension, StringComparison.OrdinalIgnoreCase)) return new(SaveTargetResult.Accepted, path);

        var corrected = System.IO.Path.ChangeExtension(path, requiredExtension);
        if (!confirmReplacement)
            return new(SaveTargetResult.ConfirmationRequired, corrected, $"선택한 형식과 파일 확장자가 다릅니다. '{System.IO.Path.GetFileName(corrected)}'(으)로 저장하시겠습니까?");
        return new(SaveTargetResult.Accepted, corrected);
    }
}
