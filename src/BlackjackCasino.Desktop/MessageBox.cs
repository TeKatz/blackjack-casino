using System.Windows;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Keeps existing call sites compatible while routing Prestige confirmations through
/// the casino's own presentation. Non-Prestige message boxes retain native behavior.
/// </summary>
internal static class MessageBox
{
    public static MessageBoxResult Show(string messageBoxText)
        => System.Windows.MessageBox.Show(messageBoxText);

    public static MessageBoxResult Show(string messageBoxText, string caption)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(Application.Current?.MainWindow, caption, messageBoxText)
            : System.Windows.MessageBox.Show(messageBoxText, caption);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(Application.Current?.MainWindow, caption, messageBoxText)
            : System.Windows.MessageBox.Show(messageBoxText, caption, button);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(Application.Current?.MainWindow, caption, messageBoxText)
            : System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(Application.Current?.MainWindow, caption, messageBoxText)
            : System.Windows.MessageBox.Show(messageBoxText, caption, button, icon, defaultResult);

    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(Application.Current?.MainWindow, caption, messageBoxText)
            : System.Windows.MessageBox.Show(messageBoxText, caption, button, icon, defaultResult, options);

    public static MessageBoxResult Show(Window owner, string messageBoxText)
        => System.Windows.MessageBox.Show(owner, messageBoxText);

    public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(owner, caption, messageBoxText)
            : System.Windows.MessageBox.Show(owner, messageBoxText, caption);

    public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(owner, caption, messageBoxText)
            : System.Windows.MessageBox.Show(owner, messageBoxText, caption, button);

    public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(owner, caption, messageBoxText)
            : System.Windows.MessageBox.Show(owner, messageBoxText, caption, button, icon);

    public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(owner, caption, messageBoxText)
            : System.Windows.MessageBox.Show(owner, messageBoxText, caption, button, icon, defaultResult);

    public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        => IsPrestige(caption)
            ? PremiumConfirmationWindow.ConfirmPrestige(owner, caption, messageBoxText)
            : System.Windows.MessageBox.Show(owner, messageBoxText, caption, button, icon, defaultResult, options);

    private static bool IsPrestige(string caption)
        => caption.Contains("Prestige", StringComparison.OrdinalIgnoreCase);
}
