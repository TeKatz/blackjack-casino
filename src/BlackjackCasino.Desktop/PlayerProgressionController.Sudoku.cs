using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

internal static class PlayerProgressionControllerSudokuExtensions
{
    internal static bool ProcessSudoku(this PlayerProgressionController controller, SudokuCompletionRecord completion)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(completion);

        lock (controller.Profile)
        {
            bool recorded = controller.Profile.Sudoku.Record(completion);
            if (!recorded)
            {
                return false;
            }

            controller.Profile.LastPlayedAt = DateTimeOffset.UtcNow;
            controller.SaveAll();
            return true;
        }
    }
}
