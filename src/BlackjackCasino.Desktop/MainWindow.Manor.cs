using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    // Every historical Manor entry now opens the same modern experience.
    // Legacy ManorWindow code is retained for controlled source-level rollback only.
    private void Manor_Click(object sender, RoutedEventArgs e) => OpenModernManor();
}
