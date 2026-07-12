namespace SimpleWordEditor;

public static class SafeFileSaver
{
    public static void Save(string destination, Action<string> writer, Action<string> validator)
    {
        var full = Path.GetFullPath(destination);
        var dir = Path.GetDirectoryName(full) ?? throw new IOException("저장 폴더를 찾을 수 없습니다.");
        Directory.CreateDirectory(dir);
        var temp = Path.Combine(dir, $".{Path.GetFileName(full)}.{Guid.NewGuid():N}.tmp");
        try
        {
            writer(temp); validator(temp);
            if (File.Exists(full)) File.Replace(temp, full, null, ignoreMetadataErrors: true);
            else File.Move(temp, full);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
