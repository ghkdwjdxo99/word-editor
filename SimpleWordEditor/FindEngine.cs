namespace SimpleWordEditor;

public readonly record struct FindMatch(int Start, int Length);

public sealed class FindEngine
{
    private IReadOnlyList<FindMatch> matches = [];
    private int currentIndex = -1;

    public IReadOnlyList<FindMatch> Matches => matches;
    public int CurrentIndex => currentIndex;

    public void Search(string text, string query, bool matchCase)
    {
        matches = FindAll(text, query, matchCase);
        currentIndex = -1;
    }

    public FindMatch? Next()
    {
        if (matches.Count == 0) return null;
        currentIndex = (currentIndex + 1) % matches.Count;
        return matches[currentIndex];
    }

    public FindMatch? Previous()
    {
        if (matches.Count == 0) return null;
        currentIndex = currentIndex <= 0 ? matches.Count - 1 : currentIndex - 1;
        return matches[currentIndex];
    }

    public static IReadOnlyList<FindMatch> FindAll(string text, string query, bool matchCase)
    {
        if (string.IsNullOrEmpty(query)) return [];
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var result = new List<FindMatch>();
        for (var start = 0; start <= text.Length - query.Length;)
        {
            var index = text.IndexOf(query, start, comparison);
            if (index < 0) break;
            result.Add(new(index, query.Length));
            start = index + Math.Max(query.Length, 1);
        }
        return result;
    }
}
