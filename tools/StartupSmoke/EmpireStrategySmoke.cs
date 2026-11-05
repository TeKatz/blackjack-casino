using BlackjackCasino.Desktop;

internal static class EmpireStrategySmoke
{
    internal static Task Run(MainWindow window, string output) => EmpireWebSmoke.Run(window, output, benchmark: true);
}
