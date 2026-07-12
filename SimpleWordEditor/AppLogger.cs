namespace SimpleWordEditor;
public static class AppLogger
{
    public static void Error(Exception ex)
    {
        try { var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleWordEditor"); Directory.CreateDirectory(dir); File.AppendAllText(Path.Combine(dir, "error.log"), $"{DateTime.Now:O} {ex.GetType().Name}: {ex.Message}{Environment.NewLine}"); } catch { }
    }
}
