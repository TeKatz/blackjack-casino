using System.Globalization;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireView
{
    /// <summary>
    /// Updates values that legitimately change while the estate is merely running.
    /// This deliberately avoids rebuilding filters, property selectors, room cards,
    /// management/command-center surfaces and responsive layout on every idle tick.
    /// Structural actions still use <see cref="Refresh"/>.
    /// </summary>
    internal void RefreshLive()
    {
        if (disposed) return;

        bool animate = animations() && IsVisible;
        credits.SetAmount(service.Credits, animate);
        production.SetAmount(service.TotalCreditsPerSecond, animate);

        decimal claimable = service.ClaimableDividend;
        dividend.Text = claimable.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        collect.IsEnabled = claimable >= .01m;
        cashRate.Text = $"${service.CashPerHour:0.00} / hour · ${service.DividendCapacity:0} shared reserve";

        // Credits can make the currently selected construction quote affordable without
        // changing any structural state. Keep that affordance live without rebuilding the page.
        if (selected.Length == 0) return;
        EmpireDepartment? room = EmpireCatalog.Find(selected);
        if (room is null) return;

        int level = service.Level(selected);
        if (level >= EmpireRules.MaximumLevel)
        {
            buy.IsEnabled = false;
            return;
        }

        EmpireUpgradeQuote quote = service.QuoteUpgrade(selected, bulk);
        buy.IsEnabled = quote.Unlocked && quote.Levels > 0 && service.Credits >= quote.Cost;
    }
}
