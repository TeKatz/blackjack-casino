namespace BlackjackCasino.Core.Adventure;

public static class AdventureBlueprintCatalog
{
    public static IReadOnlyList<AdventureStatusDefinition> StandardConditions { get; } =
    [
        new("blinded", "Blinded", "Vision is lost or severely impaired until the effect ends.", "eye-slash"),
        new("charmed", "Charmed", "Your judgment has been magically or emotionally compromised by another creature.", "heart-sigil"),
        new("deafened", "Deafened", "You cannot rely on hearing while this condition is active.", "ear"),
        new("exhaustion", "Exhaustion", "Accumulated fatigue makes demanding actions harder.", "moon", Stackable: true, DisadvantageOnAbilityChecks: true),
        new("frightened", "Frightened", "Fear disrupts your confidence and decision-making.", "fear", DisadvantageOnAbilityChecks: true),
        new("grappled", "Grappled", "Your movement is being physically controlled by another creature.", "chains", AdventureDurationUnit.Turn),
        new("incapacitated", "Incapacitated", "You are temporarily unable to take meaningful actions.", "broken-sword", AdventureDurationUnit.Turn),
        new("invisible", "Invisible", "You are hidden from ordinary sight until the effect ends.", "eye-hidden", Beneficial: true),
        new("paralyzed", "Paralyzed", "Your body is locked in place and cannot respond normally.", "lightning", AdventureDurationUnit.Turn),
        new("petrified", "Petrified", "Your body is transformed into an inert, stone-like state.", "stone", AdventureDurationUnit.Permanent),
        new("poisoned", "Poisoned", "Toxins cloud your focus and make difficult actions less reliable.", "poison", DisadvantageOnAbilityChecks: true),
        new("prone", "Prone", "You have been knocked down and must recover your footing.", "prone", AdventureDurationUnit.Turn),
        new("restrained", "Restrained", "Bindings or force sharply limit your movement.", "bindings", AdventureDurationUnit.Turn),
        new("stunned", "Stunned", "You are reeling and briefly unable to react properly.", "stun", AdventureDurationUnit.Turn),
        new("unconscious", "Unconscious", "You are unaware of your surroundings and cannot act.", "sleep", AdventureDurationUnit.Permanent),

        // Adventure-native conditions. These intentionally live beside the standard condition vocabulary so
        // later stories can reuse the same UI, duration and feedback systems without inventing new plumbing.
        new("bleeding", "Bleeding", "An open wound costs 1 HP whenever a story scene advances until treated.", "blood", PeriodicHitPointDelta: -1),
        new("blessed", "Blessed", "A protective blessing steadies you for the duration of the effect.", "sun", Beneficial: true),
        new("hidden", "Hidden", "You have broken line of sight and are difficult to locate.", "hood", Beneficial: true),
        new("marked", "Marked", "Someone or something has placed a supernatural mark on you.", "rune", AdventureDurationUnit.Permanent),
    ];

    public static IReadOnlyList<AdventureItemDefinition> BlackStagItems { get; } =
    [
        new("black-stag-letter", "Black Stag Letter", "A terse invitation sealed with the broken-antler sigil.", "letter", AdventureItemKind.Quest),
        new("brass-key", "Brass Key", "A heavy old key stamped with the crest of Saint Orra's hospice.", "key", AdventureItemKind.Utility),
        new("rope", "Hemp Rope", "Fifty feet of weathered but reliable rope.", "rope", AdventureItemKind.Utility),
        new("healing-potion", "Healing Tonic", "A red herbal tonic that restores health when consumed.", "potion-red", AdventureItemKind.Consumable, 3),
        new("antidote", "Antidote", "A bitter green draught used to purge common poisons.", "potion-green", AdventureItemKind.Consumable, 3),
        new("courier-token", "Courier Token", "A blackened silver token bearing one intact antler and one broken one.", "token", AdventureItemKind.Quest),
        new("black-stag-ledger", "Black Stag Ledger", "A coded ledger of payments, disappearances and names that should belong to the dead.", "ledger", AdventureItemKind.Quest),
        new("chapel-rubbing", "Chapel Rubbing", "A charcoal rubbing of an old stag motif found beneath Saint Orra's altar.", "rubbing", AdventureItemKind.Quest),
        new("ashgrove-charm", "Ashgrove Charm", "A carved wooden charm given by Isi. The grain forms the outline of a running stag.", "leaf-charm", AdventureItemKind.Relic),
        new("royal-seal-copy", "Copied Royal Seal", "A cast impression linking the conspiracy to a royal administrative office.", "seal", AdventureItemKind.Quest),
        new("witness-list", "Witness List", "A verified list separating genuine victims from planted names.", "list", AdventureItemKind.Quest),
    ];
}
