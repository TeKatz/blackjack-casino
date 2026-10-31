namespace BlackjackCasino.Core.Solitaire;

public sealed class SolitaireProgression(ProgressionService progression)
{
    public static long PlayerExperience(SolitaireCompletionRecord completion) => completion.Variant switch
    {
        SolitaireVariant.Klondike => 300,
        SolitaireVariant.FreeCell => 400,
        SolitaireVariant.Spider when completion.SpiderSuits == 4 => 750,
        SolitaireVariant.Spider when completion.SpiderSuits == 2 => 500,
        SolitaireVariant.Spider => 350,
        SolitaireVariant.Pyramid => 250,
        SolitaireVariant.Golf => 225,
        SolitaireVariant.TriPeaks => 250,
        _ => 250,
    };

    public bool ProcessStart(SolitaireStartRecord start)
    {
        ArgumentNullException.ThrowIfNull(start);
        bool recorded = progression.State.Solitaire.RecordStart(start);
        if (recorded) progression.State.LastPlayedAt = DateTimeOffset.UtcNow;
        return recorded;
    }

    public bool ProcessAbandon(SolitaireAbandonRecord abandon)
    {
        ArgumentNullException.ThrowIfNull(abandon);
        bool recorded = progression.State.Solitaire.RecordAbandon(abandon);
        if (recorded) progression.State.LastPlayedAt = DateTimeOffset.UtcNow;
        return recorded;
    }

    public ProgressionAward ProcessWin(SolitaireCompletionRecord completion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        _ = progression.State.Solitaire.RecordStart(new SolitaireStartRecord(
            completion.GameId, completion.Variant, completion.SpiderSuits, completion.DealNumber));
        long xp = PlayerExperience(completion);
        return LoungeProgression.GrantOnce(
            progression,
            () => progression.State.Solitaire.RecordWin(completion),
            xp,
            xp / 2);
    }
}
