using System.Runtime.InteropServices;

namespace SimpleWordEditor;

public sealed class WordInteropService
{
    public bool IsAvailable { get; }
    public WordInteropService() => IsAvailable = Type.GetTypeFromProgID("Word.Application") != null;

    public void Convert(string input, string output, bool toDocx)
    {
        if (!IsAvailable) throw new NotSupportedException("DOC 파일을 사용하려면 Microsoft Word가 설치되어 있어야 합니다. DOCX 파일은 Word 없이 사용할 수 있습니다.");
        object? app = null, docs = null, doc = null;
        try
        {
            var type = Type.GetTypeFromProgID("Word.Application")!;
            app = Activator.CreateInstance(type)!;
            type.InvokeMember("Visible", System.Reflection.BindingFlags.SetProperty, null, app, [false]);
            docs = type.InvokeMember("Documents", System.Reflection.BindingFlags.GetProperty, null, app, null);
            doc = docs!.GetType().InvokeMember("Open", System.Reflection.BindingFlags.InvokeMethod, null, docs, [Path.GetFullPath(input), false, true]);
            // wdFormatDocumentDefault=16 (.docx), wdFormatDocument97=0 (.doc)
            doc!.GetType().InvokeMember("SaveAs2", System.Reflection.BindingFlags.InvokeMethod, null, doc, [Path.GetFullPath(output), toDocx ? 16 : 0]);
        }
        finally
        {
            TryInvoke(doc, "Close", [false]); TryInvoke(app, "Quit", [false]);
            Release(doc); Release(docs); Release(app);
            GC.Collect(); GC.WaitForPendingFinalizers();
        }
    }
    private static void TryInvoke(object? value, string method, object?[] args) { try { value?.GetType().InvokeMember(method, System.Reflection.BindingFlags.InvokeMethod, null, value, args); } catch { } }
    private static void Release(object? value) { if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
}
