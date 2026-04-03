namespace WinXCorners.App;

internal static class AppLogger
{
    private static readonly List<string> Entries = [];
    private static readonly object Sync = new();

    internal static event Action<string>? EntryAdded;

    internal static IReadOnlyList<string> GetEntries()
    {
        lock (Sync)
        {
            return Entries.ToArray();
        }
    }

    internal static void Log(string message)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";

        lock (Sync)
        {
            if (Entries.Count >= 500)
            {
                Entries.RemoveAt(0);
            }

            Entries.Add(entry);
        }

        EntryAdded?.Invoke(entry);
    }
}
