namespace BlackjackCasino.Core;

public enum SlotSpinSpeed { Normal, Fast, Turbo }

public sealed record AutoSpinSettings
{
    // Zero = off, -1 = unlimited. A saved configuration never starts itself.
    public int Count { get; init; }
    public bool StopOnBigWin { get; init; }
    public bool StopOnBonus { get; init; }
    public decimal MinimumBalance { get; init; }

    public AutoSpinSettings Sanitize() => this with
    {
        Count = Count is 0 or 10 or 25 or 50 or 100 or -1 ? Count : 0,
        MinimumBalance = Math.Clamp(MinimumBalance, 0, 1_000_000_000),
    };
}

/// <summary>UI-independent lock and auto-play budget; features belong to the same paid cycle.</summary>
public sealed class SlotPlayController
{
    private AutoSpinSettings settings = new();
    public bool Busy { get; private set; }
    public bool AutoActive { get; private set; }
    public bool Paused { get; private set; }
    public int Remaining { get; private set; }
    public bool CanAutoStart => AutoActive && !Busy && !Paused;

    public void StartAuto(AutoSpinSettings value)
    {
        settings = value.Sanitize();
        Remaining = settings.Count;
        AutoActive = Remaining != 0;
    }

    public void UpdateSettings(AutoSpinSettings value)
    {
        settings=value.Sanitize();
        if(settings.Count==0) CancelAuto();
    }

    public void CancelAuto() { AutoActive = false; Remaining = 0; }
    public void Pause() => Paused = true;
    public void Resume() => Paused = false;

    public bool TryBegin(bool automatic, decimal balance, decimal bet)
    {
        if (Busy || Paused || (automatic && !CanAutoStart)) return false;
        if (balance < bet || (automatic && balance - bet < settings.MinimumBalance))
        {
            CancelAuto();
            return false;
        }
        Busy = true;
        if (automatic && Remaining > 0) Remaining--;
        return true;
    }

    public void Complete(decimal balance, bool bigWin, bool bonus)
    {
        Busy = false;
        Paused = false;
        if (Remaining == 0 || balance < settings.MinimumBalance
            || (bigWin && settings.StopOnBigWin) || (bonus && settings.StopOnBonus)) CancelAuto();
    }
}
