using System.IO;
using System.Text;

namespace BlackjackCasino.Desktop;

internal static class AppCrashLog
{
    private static readonly object Gate = new();

    internal static string LogPath
    {
        get
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "BlackjackCasino", "crash.log");
        }
    }

    internal static void Write(string message, Exception? exception = null)
    {
        try
        {
            string path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var entry = new StringBuilder()
                .AppendLine($"[{DateTimeOffset.Now:O}] {message}");
            if (exception is not null)
            {
                entry.AppendLine(exception.ToString());
            }
            entry.AppendLine(new string('-', 80));

            lock (Gate)
            {
                File.AppendAllText(path, entry.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Diagnostics must never become another startup failure.
        }
    }
}
