using System.Diagnostics;

namespace BlackjackCasino.NativeShell;

/// <summary>Dedicated, whitelisted installed Godot executable. No arbitrary command line.</summary>
internal static class NativeDescentLaunch
{
    internal static string Executable(string hostDirectory) =>
        Path.Combine(hostDirectory, "MidnightDescent", "MidnightDescent.exe");

    internal static bool IsPackaged(string hostDirectory)
    {
        string executable = Executable(hostDirectory);
        return File.Exists(executable) &&
            File.Exists(Path.ChangeExtension(executable, ".pck"));
    }

    internal static ProcessStartInfo CreateStartInfo(string hostDirectory, bool german)
    {
        if (!IsPackaged(hostDirectory))
            throw new FileNotFoundException("Midnight Descent executable or .pck is missing.");

        string executable = Executable(hostDirectory);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
        };
        // Godot flags precede the application-argument separator.
        start.ArgumentList.Add("--fullscreen");
        start.ArgumentList.Add("--");
        start.ArgumentList.Add(german ? "--language=de" : "--language=en");
        return start;
    }
}
