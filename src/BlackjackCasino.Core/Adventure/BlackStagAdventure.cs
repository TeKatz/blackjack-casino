namespace BlackjackCasino.Core.Adventure;

internal static class BlackStagAdventure
{
    internal static AdventureDefinition Build()
    {
        static AdventureEffect Fx(AdventureEffectKind kind, string key = "", int value = 0, bool flag = true) => new(kind, key, value, flag);
        static AdventureCondition Req(AdventureConditionKind kind, string key = "", int value = 0, bool expected = true, AdventureAbility? ability = null) => new(kind, key, value, expected, ability);
        static AdventureCheckDefinition Roll(string id, AdventureAbility ability, int dc, string? skill, string success, string failure, string? critSuccess = null, string? critFailure = null, AdventureRollMode mode = AdventureRollMode.Normal) =>
            new(id, ability, dc, skill, RollMode: mode, SuccessSceneId: success, FailureSceneId: failure, CriticalSuccessSceneId: critSuccess, CriticalFailureSceneId: critFailure);
        static AdventureChoiceDefinition Choice(string id, string text, string? next = null, IReadOnlyList<AdventureCondition>? requires = null, AdventureCheckDefinition? check = null, IReadOnlyList<AdventureEffect>? effects = null, IReadOnlyList<AdventureEffect>? success = null, IReadOnlyList<AdventureEffect>? failure = null) =>
            new(id, text, next, requires, check, effects, success, failure);
        static AdventureSceneDefinition Scene(string id, string title, string body, string act, string location, IReadOnlyList<AdventureChoiceDefinition>? choices = null, string? speaker = null, IReadOnlyList<AdventureEffect>? enter = null) =>
            new(id, title, body, speaker, choices, enter, ActId: act, Location: location);
        static AdventureSceneDefinition Ending(string id, string title, string body, string endingId, int xp) =>
            new(id, title, body, IsEnding: true, EndingId: endingId, OnEnterEffects: [Fx(AdventureEffectKind.GainCharacterExperience, value: xp)], ActId: "act-v", Location: "Epilogue");

        var scenes = new List<AdventureSceneDefinition>
        {
            // ACT I — THE LETTER
            Scene("crooked-crown", "The Crooked Crown",
                "Rain needles the windows of the Crooked Crown and turns the road outside into black glass. Mara Vey, the innkeeper, sets a sealed letter beside your cup without asking whether you want it. \"A stranger paid well to make sure this reached you.\" The wax is black. The mark pressed into it is a stag with one antler broken away.",
                "act-i", "The Crooked Crown",
                [
                    Choice("open-letter", "Break the seal and read the letter.", "letter-opened", effects: [Fx(AdventureEffectKind.AddItem, "black-stag-letter")]),
                    Choice("inspect-seal", "Inspect the seal before touching the letter.", check: Roll("seal-investigation", AdventureAbility.Intelligence, 13, "investigation", "forged-seal", "seal-uncertain"), effects: [Fx(AdventureEffectKind.SetFlag, "inspected-seal")]),
                    Choice("question-keeper", "Watch Mara carefully and ask who delivered it.", check: Roll("keeper-insight", AdventureAbility.Wisdom, 12, "insight", "keeper-truth", "keeper-guarded")),
                ], "Mara Vey"),

            Scene("forged-seal", "A False Signet",
                "The wax itself is ordinary, but the impression is wrong. Tiny doubled lines ring the broken antler — the telltale edge of a cast copy. Whoever sent this wanted the symbol recognized without owning the original signet.",
                "act-i", "The Crooked Crown",
                [
                    Choice("open-after-inspect", "Open the letter.", "letter-opened", effects: [Fx(AdventureEffectKind.SetFlag, "seal-forged"), Fx(AdventureEffectKind.AddItem, "black-stag-letter")]),
                    Choice("press-keeper", "Show Mara the flaw and press her for details.", "keeper-truth", effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "mara", 1)]),
                ]),

            Scene("seal-uncertain", "Cold Wax",
                "You turn the seal beneath the lamplight, but the rainlight and soot make every scratch look meaningful. Mara keeps polishing the same glass. Whatever she knows, she has decided to make you earn it.",
                "act-i", "The Crooked Crown",
                [
                    Choice("open-uncertain", "Open the letter anyway.", "letter-opened", effects: [Fx(AdventureEffectKind.AddItem, "black-stag-letter")]),
                    Choice("ask-uncertain", "Ask Mara what she remembers.", "keeper-guarded"),
                ]),

            Scene("keeper-truth", "What Mara Saw",
                "Mara's expression tightens. \"Grey travelling coat. Mud to the knees. Silver tack on the horse — expensive. He kept looking toward the old north road.\" She lowers her voice. \"And he was afraid. Not of me. Of being followed.\"",
                "act-i", "The Crooked Crown",
                [Choice("thank-keeper", "Thank Mara and read the letter.", "letter-opened", effects: [Fx(AdventureEffectKind.SetFlag, "knows-silver-tack"), Fx(AdventureEffectKind.ChangeNpcRelation, "mara", 1), Fx(AdventureEffectKind.AddItem, "black-stag-letter")])], "Mara Vey"),

            Scene("keeper-guarded", "A Short Memory",
                "\"Traveller. Hood up. Paid in silver.\" Mara delivers the answer too quickly. Whether from fear or irritation, she has decided that is all you are getting tonight.",
                "act-i", "The Crooked Crown",
                [Choice("leave-keeper", "Stop pushing and read the letter.", "letter-opened", effects: [Fx(AdventureEffectKind.AddItem, "black-stag-letter")])], "Mara Vey"),

            Scene("letter-opened", "The Invitation",
                "The letter contains only three lines: NORTH ROAD. SAINT ORRA'S BRIDGE. BEFORE MIDNIGHT. COME ALONE IF YOU WANT THE MISSING NAMES. Beneath the words is a charcoal sketch of the broken-antler stag.",
                "act-i", "The Crooked Crown",
                [
                    Choice("take-north-road", "Leave for Saint Orra's bridge.", "north-road"),
                    Choice("search-for-mark", "Search your memory for the broken-antler mark.", check: Roll("stag-lore", AdventureAbility.Intelligence, 14, "history", "remembered-mark", "north-road")),
                    Choice("study-magic", "Test the wax for magical residue.", requires: [Req(AdventureConditionKind.SkillMinimum, "arcana", 2)], check: Roll("seal-arcana", AdventureAbility.Intelligence, 12, "arcana", "seal-residue", "north-road")),
                ]),

            Scene("seal-residue", "A Trace of Old Magic",
                "There is no spell on the letter, but the wax carries a faint residue like cold pine needles after rain. It is too old to belong to the courier. The symbol itself has been used around magic before.",
                "act-i", "The Crooked Crown",
                [Choice("go-after-residue", "Take the north road with that thought in mind.", "north-road", effects: [Fx(AdventureEffectKind.SetFlag, "stag-symbol-older")])]),

            Scene("remembered-mark", "An Older Story",
                "You remember a censored court record: the Black Stag was the name of a courier network that once moved testimony too dangerous to enter the royal courts openly. The network supposedly died twenty years ago. The symbol on the page was older than the network by centuries.",
                "act-i", "The Crooked Crown",
                [Choice("go-informed", "Keep the memory to yourself and take the north road.", "north-road", effects: [Fx(AdventureEffectKind.SetFlag, "knows-black-stag"), Fx(AdventureEffectKind.SetFlag, "stag-symbol-older")])]),

            Scene("north-road", "Saint Orra's Bridge",
                "The bridge rises from the fog like a broken rib. A hooded courier waits beside the parapet. Before either of you can speak, boots scrape stone behind you. A hunter in a dark coat blocks the road back, one hand already on his sword.",
                "act-i", "Saint Orra's Bridge",
                [
                    Choice("draw-steel", "Meet the hunter head-on.", check: Roll("bridge-fight", AdventureAbility.Strength, 14, "athletics", "hunter-defeated", "wounded-escape", "hunter-disarmed"), failure: [Fx(AdventureEffectKind.ChangeHitPoints, value: -4), Fx(AdventureEffectKind.ApplyCondition, "bleeding", 4)]),
                    Choice("dive-cover", "Vault the parapet and use the lower maintenance ledge.", check: Roll("bridge-acrobatics", AdventureAbility.Dexterity, 13, "acrobatics", "lower-ledge", "wounded-escape"), failure: [Fx(AdventureEffectKind.ChangeHitPoints, value: -3), Fx(AdventureEffectKind.ApplyCondition, "bleeding", 3)]),
                    Choice("talk-hunter", "\"If you wanted the courier dead, you would have struck already.\"", check: Roll("bridge-persuasion", AdventureAbility.Charisma, 15, "persuasion", "hunter-hesitates", "wounded-escape")),
                ]),

            Scene("hunter-disarmed", "Steel in the Fog",
                "The hunter commits too early. You catch his wrist, turn the strike, and send the sword spinning across wet stone. The courier laughs once in disbelief, then thrusts a leather packet toward you.",
                "act-i", "Saint Orra's Bridge",
                [Choice("take-ledger-clean", "Take the packet and demand the truth.", "ledger-revealed", effects: [Fx(AdventureEffectKind.SetFlag, "hunter-disarmed"), Fx(AdventureEffectKind.AddItem, "black-stag-ledger")])]),

            Scene("hunter-defeated", "A Narrow Opening",
                "The exchange is fast and ugly, but you force the hunter back far enough for the courier to reach you. \"No time,\" she says. \"Take this. If I fall, don't give it to anyone wearing a crown.\"",
                "act-i", "Saint Orra's Bridge",
                [Choice("take-ledger", "Take the leather packet.", "ledger-revealed", effects: [Fx(AdventureEffectKind.AddItem, "black-stag-ledger")])], "Courier"),

            Scene("lower-ledge", "Below the Bridge",
                "You drop beneath the parapet as steel flashes overhead. The courier follows, landing badly but keeping hold of a leather packet. A maintenance door waits ten paces away above a black river.",
                "act-i", "Saint Orra's Bridge",
                [
                    Choice("force-door", "Force the swollen maintenance door.", check: Roll("maintenance-strength", AdventureAbility.Strength, 12, "athletics", "ledger-revealed", "wounded-escape"), success: [Fx(AdventureEffectKind.SetFlag, "escaped-below"), Fx(AdventureEffectKind.AddItem, "black-stag-ledger")], failure: [Fx(AdventureEffectKind.ApplyCondition, "bleeding", 3)]),
                    Choice("take-packet", "Take the packet and run the ledge.", "ledger-revealed", effects: [Fx(AdventureEffectKind.AddItem, "black-stag-ledger")]),
                ]),

            Scene("hunter-hesitates", "The Question Behind the Blade",
                "The hunter's grip loosens by a fraction. \"You don't know what she's carrying.\" The courier answers first: \"Names. Judges, officers, merchants. Everyone who bought a place in the disappearances.\"",
                "act-i", "Saint Orra's Bridge",
                [
                    Choice("choose-courier", "Side with the courier and take the packet.", "ledger-revealed", effects: [Fx(AdventureEffectKind.SetFlag, "spared-hunter"), Fx(AdventureEffectKind.AddItem, "black-stag-ledger")]),
                    Choice("hear-hunter", "Ask the hunter why the list must disappear.", "hunter-warning"),
                ]),

            Scene("hunter-warning", "A Dangerous Ledger",
                "\"Because half the names are bait,\" the hunter says. \"The real list is shorter. Someone wants a purge, not justice.\" The courier says nothing, which is answer enough.",
                "act-i", "Saint Orra's Bridge",
                [Choice("take-ledger-warning", "Take the ledger. Decide after you verify it.", "ledger-revealed", effects: [Fx(AdventureEffectKind.SetFlag, "knows-ledger-may-be-false"), Fx(AdventureEffectKind.AddItem, "black-stag-ledger")])], "Edrin Vale"),

            Scene("wounded-escape", "Blood on the Stones",
                "The plan collapses into noise. You escape the hunter, but only just. When the fog finally swallows the bridge, the courier is beside you and there is blood running warm beneath your sleeve.",
                "act-i", "Saint Orra's Bridge",
                [Choice("continue-wounded", "Keep moving. Ask what this was all for.", "ledger-revealed", effects: [Fx(AdventureEffectKind.SetFlag, "wounded-at-bridge"), Fx(AdventureEffectKind.AddItem, "black-stag-ledger")])]),

            Scene("ledger-revealed", "The Missing Names",
                "Inside the packet is a coded ledger of dates, payments and names — including people officially declared dead years ago. One entry is fresh. Tonight's date. Your own name is written beside it. The courier points to a place called Grayfen and says one person there can tell you which names are real.",
                "act-i", "North Road",
                [
                    Choice("keep-ledger", "Take the ledger and disappear before either side can reclaim it.", "ending-ledger"),
                    Choice("verify-first", "Go to Grayfen and verify the ledger before trusting anyone.", "grayfen-road", requires: [Req(AdventureConditionKind.Flag, "knows-ledger-may-be-false")], effects: [Fx(AdventureEffectKind.SetFlag, "seeking-verification")]),
                    Choice("follow-stag", "Follow the courier's instructions toward Grayfen.", "grayfen-road", requires: [Req(AdventureConditionKind.Flag, "knows-black-stag")], effects: [Fx(AdventureEffectKind.SetFlag, "open-to-stag")]),
                    Choice("go-grayfen", "If your name is on the page, you need answers. Go to Grayfen.", "grayfen-road"),
                ]),

            Ending("ending-ledger", "Ending — A Name in Ink",
                "You leave the road before dawn with the ledger hidden beneath your coat and no promises made. Somewhere behind you, both the Black Stag and its hunter begin searching for the same missing book. This time, however, you are the one deciding where the trail leads.", "name-in-ink", 100),

            // ACT II — THE MISSING NAMES
            Scene("grayfen-road", "Into Grayfen",
                "By morning the rain has become a cold mist. Grayfen sits between black pines and flooded meadows, its roofs low against the sky. A figure waits on a fallen oak above the road: a wood elf in a moss-dark cloak, bow unstrung but close to hand. She studies you with the patience of someone who has already decided running is pointless.",
                "act-ii", "Grayfen Road",
                [Choice("meet-isi", "Stop where she can see your hands.", "isi-first-meeting")]),

            Scene("isi-first-meeting", "The Elf on the Road",
                "\"Isi,\" she says after you give your name. Her eyes move to the shape of the ledger beneath your coat. \"If that book brought you here, then someone has already lied to you. The question is whether it was the Stag, the Crown, or both.\" She offers to guide you through Grayfen — if you prove you are not another collector of names.",
                "act-ii", "Grayfen Road",
                [
                    Choice("isi-honest", "Show Isi the ledger and tell her exactly how you got it.", "isi-camp", effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 2), Fx(AdventureEffectKind.SetFlag, "isi-saw-ledger")]),
                    Choice("isi-cautious", "Tell her enough to earn a conversation, but keep the ledger hidden.", check: Roll("isi-insight-contest", AdventureAbility.Charisma, 13, "persuasion", "isi-camp", "isi-sees-lie"), success: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)]),
                    Choice("isi-threat", "Tell her to move or be moved.", check: Roll("isi-intimidation", AdventureAbility.Charisma, 15, "intimidation", "isi-camp", "isi-sees-lie"), effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", -2)]),
                ], "Isi"),

            Scene("isi-sees-lie", "A Poor Beginning",
                "Isi lets the silence sit until it becomes uncomfortable. \"You are either lying, frightened, or both. I can work with frightened. Liars cost more.\" She steps off the log and starts toward the trees without checking whether you follow.",
                "act-ii", "Grayfen Road",
                [Choice("follow-isi-anyway", "Follow her.", "isi-camp", effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", -1)])], "Isi"),

            Scene("isi-camp", "Ash and Pine",
                "Isi's camp is little more than a lean-to, a smokeless fire and a map scratched onto treated hide. Three places are circled: an abandoned mill, Saint Orra's chapel, and Grayfen market. \"People vanished from all three trails,\" she says. \"Find two truths that agree and I'll show you where the Black Stag used to meet.\"",
                "act-ii", "Isi's Camp",
                [
                    Choice("accept-charm", "Accept the carved Ashgrove charm she offers.", "grayfen-hub", effects: [Fx(AdventureEffectKind.AddItem, "ashgrove-charm"), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)]),
                    Choice("skip-charm", "Leave the charm. Ask about the three leads instead.", "grayfen-hub"),
                    Choice("treat-bleeding", "Use a healing tonic on the wound before moving on.", "grayfen-hub", requires: [Req(AdventureConditionKind.ActiveCondition, "bleeding"), Req(AdventureConditionKind.ItemOwned, "healing-potion")], effects: [Fx(AdventureEffectKind.RemoveItem, "healing-potion"), Fx(AdventureEffectKind.RemoveCondition, "bleeding"), Fx(AdventureEffectKind.ChangeHitPoints, value: 6)]),
                ], "Isi"),

            Scene("grayfen-hub", "Three Roads Through Grayfen",
                "From Isi's camp, three trails split through the settlement. The mill road follows the river. The chapel path climbs through old graves. The market lies downhill where smoke and wet wool turn the air sour. Isi watches which direction you choose first.",
                "act-ii", "Grayfen",
                [
                    Choice("to-mill", "Investigate the abandoned mill.", "mill-yard", requires: [Req(AdventureConditionKind.Flag, "mill-clue", expected: false)]),
                    Choice("to-chapel", "Search Saint Orra's chapel.", "chapel-gate", requires: [Req(AdventureConditionKind.Flag, "chapel-clue", expected: false)]),
                    Choice("to-market", "Ask questions in Grayfen market.", "market-square", requires: [Req(AdventureConditionKind.Flag, "market-clue", expected: false)]),
                    Choice("enough-clues", "You have enough pieces. Ask Isi where they point.", "isi-connects-clues", requires: [Req(AdventureConditionKind.CounterMinimum, "clues", 2)]),
                ]),

            Scene("mill-yard", "The Silent Mill",
                "The wheel no longer turns, though the river still pushes against it. Fresh boot marks cross the rotten yard. Someone has nailed a royal tax notice over an older door carved with a shallow stag motif.",
                "act-ii", "Old Mill",
                [
                    Choice("mill-tracks", "Follow the fresh tracks around the building.", check: Roll("mill-survival", AdventureAbility.Wisdom, 12, "survival", "mill-hidden-door", "mill-front-door")),
                    Choice("mill-force", "Force the front door before whoever is inside can react.", check: Roll("mill-athletics", AdventureAbility.Strength, 13, "mill-front-door", "mill-fall")),
                    Choice("mill-sneak", "Circle to the upper loading window.", requires: [Req(AdventureConditionKind.SkillMinimum, "stealth", 2)], check: Roll("mill-stealth", AdventureAbility.Dexterity, 12, "mill-hidden-door", "mill-front-door")),
                ]),

            Scene("mill-fall", "Rotten Boards",
                "The frame gives before the lock does. You hit the floor hard enough to drive the breath from your lungs, and a nail tears your palm on the way down.",
                "act-ii", "Old Mill",
                [Choice("mill-rise", "Get up and keep moving.", "mill-front-door", effects: [Fx(AdventureEffectKind.ChangeHitPoints, value: -2), Fx(AdventureEffectKind.ApplyCondition, "bleeding", 2)])]),

            Scene("mill-front-door", "The Miller's Office",
                "Dust lies thick over the millstones, but the office has been used recently. A locked drawer sits beneath a desk, and a coil of serviceable rope hangs from a wall peg.",
                "act-ii", "Old Mill",
                [
                    Choice("take-rope", "Take the rope before searching the drawer.", "mill-drawer", effects: [Fx(AdventureEffectKind.AddItem, "rope")]),
                    Choice("open-drawer", "Ignore the rope and inspect the locked drawer.", "mill-drawer"),
                ]),

            Scene("mill-hidden-door", "Behind the Wheelhouse",
                "The tracks end at a disguised door behind the wheelhouse. Inside is a narrow records room. On a hook hangs a brass key stamped with Saint Orra's crest. A purse of loose coins sits beside it, untouched.",
                "act-ii", "Old Mill",
                [Choice("take-mill-cache", "Take the key and the coins, then read the records.", "mill-records", effects: [Fx(AdventureEffectKind.AddItem, "brass-key"), Fx(AdventureEffectKind.ChangeCoins, value: 8)])]),

            Scene("mill-drawer", "Names Beneath Flour Dust",
                "The drawer yields to patience more easily than force. Inside are delivery slips bearing dates that match the ledger. Several shipments were redirected not to a warehouse, but to Saint Orra's old hospice outside town.",
                "act-ii", "Old Mill",
                [
                    Choice("pick-drawer", "Work the old lock carefully.", requires: [Req(AdventureConditionKind.SkillMinimum, "sleight-of-hand", 2)], check: Roll("drawer-pick", AdventureAbility.Dexterity, 11, "sleight-of-hand", "mill-records", "mill-records"), success: [Fx(AdventureEffectKind.AddItem, "brass-key")]),
                    Choice("record-clue", "Memorize the hospice reference and return to Isi.", "mill-records"),
                ]),

            Scene("mill-records", "The Hospice Route",
                "The records are mundane enough to be convincing: grain, lamp oil, linen, medicine. But the quantities are too large for an abandoned hospice. Someone has been provisioning people there for years.",
                "act-ii", "Old Mill",
                [Choice("mill-return", "Return to Isi with the records.", "grayfen-hub", effects: [Fx(AdventureEffectKind.SetFlag, "mill-clue"), Fx(AdventureEffectKind.ChangeCounter, "clues", 1)])]),

            Scene("chapel-gate", "Saint Orra's Chapel",
                "The chapel smells of wet stone and extinguished candles. Its bell rope has been cut. Every grave in the yard faces east except one, which faces the forest. Isi notices it at the same moment you do.",
                "act-ii", "Saint Orra's Chapel",
                [
                    Choice("chapel-grave", "Examine the grave that faces the forest.", check: Roll("grave-investigation", AdventureAbility.Intelligence, 12, "investigation", "chapel-crypt", "chapel-altar")),
                    Choice("chapel-rite", "Study the old funerary symbols before entering.", requires: [Req(AdventureConditionKind.SkillMinimum, "religion", 2)], "chapel-rubbing", effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)]),
                    Choice("chapel-magic", "Follow the magical residue in the stonework.", requires: [Req(AdventureConditionKind.SkillMinimum, "arcana", 2)], check: Roll("chapel-arcana", AdventureAbility.Intelligence, 13, "arcana", "chapel-rubbing", "chapel-altar")),
                ]),

            Scene("chapel-altar", "Under the Altar",
                "The altar has been moved recently. Beneath it, a cavity holds old bandages, dried herbs and a stoppered green vial. When you lift the vial, a needle hidden in the stone clicks beneath your thumb.",
                "act-ii", "Saint Orra's Chapel",
                [
                    Choice("avoid-needle", "Pull your hand back before the mechanism fires.", check: Roll("chapel-trap", AdventureAbility.Dexterity, 13, "perception", "chapel-rubbing", "chapel-poisoned"), success: [Fx(AdventureEffectKind.AddItem, "antidote")]),
                    Choice("take-vial", "Take the vial and accept the risk.", "chapel-poisoned", effects: [Fx(AdventureEffectKind.AddItem, "antidote")]),
                ]),

            Scene("chapel-poisoned", "A Bitter Needle",
                "The needle barely breaks skin, but the numbness comes too quickly. Isi swears under her breath and catches your wrist before you can rub the wound.",
                "act-ii", "Saint Orra's Chapel",
                [
                    Choice("drink-antidote", "Drink the antidote immediately.", "chapel-rubbing", requires: [Req(AdventureConditionKind.ItemOwned, "antidote")], effects: [Fx(AdventureEffectKind.RemoveItem, "antidote"), Fx(AdventureEffectKind.RemoveCondition, "poisoned")]),
                    Choice("endure-poison", "Save the antidote and push on.", "chapel-rubbing", effects: [Fx(AdventureEffectKind.ApplyCondition, "poisoned", 4)]),
                ], "Isi"),

            Scene("chapel-crypt", "The Forest-Facing Grave",
                "The grave marker is a false lid over narrow steps. Below, the crypt wall carries hundreds of tiny carved names. Several match the ledger. At the center is the broken-antler stag — carved into stone older than the chapel itself.",
                "act-ii", "Saint Orra's Crypt",
                [Choice("copy-crypt", "Make a charcoal rubbing of the oldest carving.", "chapel-rubbing", effects: [Fx(AdventureEffectKind.AddItem, "chapel-rubbing"), Fx(AdventureEffectKind.SetFlag, "stag-symbol-ancient")])]),

            Scene("chapel-rubbing", "The Names in Stone",
                "The chapel records confirm that many of the supposedly dead names passed through Saint Orra's care. Some were refugees. Some were witnesses. Some were children. The Black Stag did not merely carry testimony; it moved people beyond the Crown's reach.",
                "act-ii", "Saint Orra's Chapel",
                [Choice("chapel-return", "Return to Isi with the chapel evidence.", "grayfen-hub", effects: [Fx(AdventureEffectKind.SetFlag, "chapel-clue"), Fx(AdventureEffectKind.ChangeCounter, "clues", 1)])]),

            Scene("market-square", "Grayfen Market",
                "Grayfen's market is a strip of canvas roofs, smoked fish and guarded conversations. Isi points out three useful people: an apothecary who asks no names, a fence who sells information by the sentence, and a royal clerk drinking alone before noon.",
                "act-ii", "Grayfen Market",
                [
                    Choice("visit-apothecary", "Visit the apothecary.", "market-apothecary"),
                    Choice("visit-fence", "Approach the fence.", "market-fence"),
                    Choice("watch-clerk", "Watch the royal clerk before speaking to anyone.", check: Roll("clerk-perception", AdventureAbility.Wisdom, 12, "perception", "market-clerk", "market-fence")),
                ]),

            Scene("market-apothecary", "Bitterglass Apothecary",
                "Shelves of dark bottles climb to the ceiling. The apothecary recognizes the blood on your clothes without comment. \"Six coins for red. Five for green. Rope is not my trade, but the chandlery two doors down has some.\"",
                "act-ii", "Grayfen Market",
                [
                    Choice("buy-healing", "Buy a healing tonic for 6 coins.", "market-square", requires: [Req(AdventureConditionKind.CoinsMinimum, value: 6)], effects: [Fx(AdventureEffectKind.ChangeCoins, value: -6), Fx(AdventureEffectKind.AddItem, "healing-potion")]),
                    Choice("buy-antidote", "Buy an antidote for 5 coins.", "market-square", requires: [Req(AdventureConditionKind.CoinsMinimum, value: 5)], effects: [Fx(AdventureEffectKind.ChangeCoins, value: -5), Fx(AdventureEffectKind.AddItem, "antidote")]),
                    Choice("buy-rope", "Buy rope from the chandlery for 4 coins.", "market-square", requires: [Req(AdventureConditionKind.CoinsMinimum, value: 4), Req(AdventureConditionKind.ItemOwned, "rope", expected: false)], effects: [Fx(AdventureEffectKind.ChangeCoins, value: -4), Fx(AdventureEffectKind.AddItem, "rope")]),
                    Choice("leave-shop", "Leave without buying anything.", "market-square"),
                ], "Apothecary Senn"),

            Scene("market-fence", "The Price of a Name",
                "The fence calls himself Pell and never looks directly at the ledger. He does recognize the copied seal sketched in its margin. \"Royal supply office, east archive. They stopped using that stamp after a clerk vanished.\" He wants five coins for the clerk's name.",
                "act-ii", "Grayfen Market",
                [
                    Choice("pay-pell", "Pay Pell five coins.", "market-clue", requires: [Req(AdventureConditionKind.CoinsMinimum, value: 5)], effects: [Fx(AdventureEffectKind.ChangeCoins, value: -5), Fx(AdventureEffectKind.SetFlag, "knows-royal-office")]),
                    Choice("persuade-pell", "Convince him that surviving the week is worth more than five coins.", check: Roll("pell-persuasion", AdventureAbility.Charisma, 14, "persuasion", "market-clue", "market-clue"), success: [Fx(AdventureEffectKind.SetFlag, "knows-royal-office"), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)]),
                    Choice("intimidate-pell", "Make the price feel negotiable.", check: Roll("pell-intimidation", AdventureAbility.Charisma, 13, "intimidation", "market-clue", "market-clue"), effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", -1)], success: [Fx(AdventureEffectKind.SetFlag, "knows-royal-office")]),
                ], "Pell"),

            Scene("market-clerk", "The Clerk with Silver Cuffs",
                "The royal clerk keeps rubbing a pale line around his wrist where a manacle once sat. He is not drinking from boredom. He is drinking because he recognizes Isi and hopes she has not recognized him.",
                "act-ii", "Grayfen Market",
                [
                    Choice("clerk-kind", "Sit down without threatening him.", check: Roll("clerk-insight", AdventureAbility.Wisdom, 13, "insight", "market-clue", "market-clue"), success: [Fx(AdventureEffectKind.SetFlag, "knows-royal-office"), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)]),
                    Choice("clerk-direct", "Put the ledger on the table and ask which names are false.", "market-clue", effects: [Fx(AdventureEffectKind.SetFlag, "ledger-seen-in-market")]),
                ]),

            Scene("market-clue", "The East Archive",
                "Every version of the story points toward the same place: the Crown's east archive supplied the hospice, copied the seal and altered death records. Someone inside the bureaucracy did not merely hunt the Black Stag. They helped manufacture it.",
                "act-ii", "Grayfen Market",
                [Choice("market-return", "Return to Isi with what you learned.", "grayfen-hub", effects: [Fx(AdventureEffectKind.SetFlag, "market-clue"), Fx(AdventureEffectKind.ChangeCounter, "clues", 1)])]),

            Scene("isi-connects-clues", "Where the Trails Meet",
                "Isi lays your evidence beside the ledger. The mill supplied the hospice. The chapel hid witnesses. The market points to the royal archive. She taps a faded trail north of Grayfen. \"The hospice is still there. If anyone survived the last purge, they are there — or whoever killed them is.\"",
                "act-ii", "Isi's Camp",
                [
                    Choice("go-hospice", "Leave for Saint Orra's hospice before nightfall.", "hospice-gate"),
                    Choice("ask-isi-why", "Ask why this matters to her personally.", "isi-personal"),
                ], "Isi"),

            Scene("isi-personal", "What Grayfen Took",
                "Isi stares at the fire for a long time. \"Three people from my grove vanished on this road. One returned without a voice. One came back under a different name. The third never came back. I stopped believing lists were only ink after that.\"",
                "act-ii", "Isi's Camp",
                [Choice("promise-isi", "Promise that the missing matter more than the politics.", "hospice-gate", effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 2), Fx(AdventureEffectKind.SetFlag, "promised-missing")])], "Isi"),

            Scene("hospice-gate", "Saint Orra's Hospice",
                "The hospice stands in a hollow where the trees refuse to grow straight. Its front doors are chained, but the side service entrance bears the same crest as the brass key from the mill.",
                "act-ii", "Saint Orra's Hospice",
                [
                    Choice("use-brass-key", "Use the brass key on the service entrance.", "hospice-archive", requires: [Req(AdventureConditionKind.ItemOwned, "brass-key")]),
                    Choice("force-hospice", "Break the chain at the front door.", check: Roll("hospice-force", AdventureAbility.Strength, 14, "athletics", "hospice-archive", "hospice-alarm")),
                    Choice("find-hospice-way", "Circle the building for another entrance.", check: Roll("hospice-perception", AdventureAbility.Wisdom, 13, "perception", "hospice-survivor", "hospice-alarm")),
                ]),

            Scene("hospice-alarm", "The Bell Without a Rope",
                "A mechanism inside the wall snaps. Somewhere above you a bell rings despite the cut rope at the chapel. Isi's head turns toward the tree line. \"We have minutes. Maybe less.\"",
                "act-ii", "Saint Orra's Hospice",
                [Choice("hospice-hurry", "Search the archive first.", "hospice-archive", effects: [Fx(AdventureEffectKind.SetFlag, "hospice-alarm")])]),

            Scene("hospice-archive", "Records of the Living Dead",
                "The hospice archive is intact. Death certificates are stacked beside new identities, travel permits and payments. The ledger's false entries are marked with a tiny notch at the bottom of each page. There are fewer false names than Edrin claimed — but enough to prove someone planned to use the ledger as a weapon.",
                "act-ii", "Saint Orra's Hospice",
                [Choice("take-witness-list", "Separate the verified names from the planted ones.", "hospice-survivor", effects: [Fx(AdventureEffectKind.AddItem, "witness-list"), Fx(AdventureEffectKind.SetFlag, "ledger-verified")])]),

            Scene("hospice-survivor", "The Woman Behind the Wall",
                "A tapping sound leads to a hidden room. Inside waits an elderly human woman with a Black Stag token around her neck. She calls herself Sister Vael. When Isi steps into the light, Vael begins to cry.",
                "act-ii", "Saint Orra's Hospice",
                [
                    Choice("listen-vael", "Let Sister Vael tell the story in her own time.", "act2-reveal", effects: [Fx(AdventureEffectKind.AddItem, "courier-token"), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)]),
                    Choice("press-vael", "Ask who falsified the ledger before the hunters arrive.", check: Roll("vael-persuasion", AdventureAbility.Charisma, 12, "persuasion", "act2-reveal", "act2-reveal"), success: [Fx(AdventureEffectKind.SetFlag, "vael-names-archivist")]),
                ], "Sister Vael"),

            Scene("act2-reveal", "The Missing Were Hidden",
                "Vael confirms the truth: the Black Stag began as a courier network, but became a chain of safe houses for witnesses, deserters and families marked for disappearance. Then someone inside the network began inserting false names. Someone wanted the Crown to answer a crime with a massacre. Vael knows where the surviving Stag council is hiding.",
                "act-ii", "Saint Orra's Hospice",
                [Choice("follow-vael-map", "Take Isi and follow Vael's map to the old Stag refuge.", "stag-safehouse")], "Sister Vael"),

            // ACT III — THE BROKEN ANTLERS
            Scene("stag-safehouse", "The House Beneath Roots",
                "The refuge is built beneath the roots of an ancient ash tree. No banners hang inside. No uniforms. Only tired people, sealed letters and weapons kept within reach. The broken-antler sigil is carved above a stone door that predates every beam in the room.",
                "act-iii", "Black Stag Refuge",
                [
                    Choice("show-token", "Show Vael's courier token and ask for the council.", "stag-council", requires: [Req(AdventureConditionKind.ItemOwned, "courier-token")]),
                    Choice("show-ledger", "Place the ledger on the table and demand answers.", "stag-council", effects: [Fx(AdventureEffectKind.SetFlag, "entered-with-ledger")]),
                ]),

            Scene("stag-council", "A Council Without a Crown",
                "Three people speak for the refuge: Vael's former captain Seran, a young courier named Lio, and Archivist Merrow — the same name hidden in several royal supply records. Merrow argues that exposing the Crown now will save thousands. Seran insists the planted names will turn justice into slaughter.",
                "act-iii", "Black Stag Refuge",
                [
                    Choice("question-merrow", "Ask Merrow why his name appears in royal supply records.", "merrow-answer"),
                    Choice("question-seran", "Ask Seran why Edrin was hunting the courier.", "seran-answer"),
                    Choice("ask-isi", "Step aside with Isi before choosing whom to trust.", "isi-confession"),
                ]),

            Scene("merrow-answer", "The Archivist's Defense",
                "Merrow does not deny it. He worked inside the east archive for twelve years, forging deaths to save people the Crown meant to erase. \"Every false death bought a real life.\" When you ask about the planted names, his calm finally cracks. \"I did not add them. But I know who could.\"",
                "act-iii", "Black Stag Refuge",
                [Choice("merrow-next", "Ask who had access.", "truth-fractured", effects: [Fx(AdventureEffectKind.SetFlag, "merrow-cooperated")])], "Archivist Merrow"),

            Scene("seran-answer", "Why Edrin Hunts",
                "Seran tells you Edrin Vale was once Black Stag. He left after a rescue became an execution. Since then he has hunted both royal agents and Stag extremists. \"He thinks burning the network is the only way to stop us becoming what we fought.\"",
                "act-iii", "Black Stag Refuge",
                [Choice("seran-next", "Ask what happened during the failed rescue.", "truth-fractured", effects: [Fx(AdventureEffectKind.SetFlag, "knows-edrin-history")])], "Seran"),

            Scene("isi-confession", "Isi's Third Name",
                "Isi waits until the council cannot hear. \"The third person from my grove was my brother. His death certificate is in Merrow's handwriting. I came here to learn whether that means he was saved, murdered, or turned into a useful lie.\" She meets your eyes. \"If I ask you to choose him over the ledger, I need to know what kind of person will answer.\"",
                "act-iii", "Black Stag Refuge",
                [
                    Choice("isi-person-first", "Tell her you will look for her brother before deciding the network's fate.", "truth-fractured", effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 2), Fx(AdventureEffectKind.SetFlag, "isi-brother-priority")]),
                    Choice("isi-truth-first", "Tell her the wider truth has to come first.", "truth-fractured", effects: [Fx(AdventureEffectKind.ChangeNpcRelation, "isi", -1), Fx(AdventureEffectKind.SetFlag, "truth-first")]),
                ], "Isi"),

            Scene("truth-fractured", "Two Ledgers, One Hand",
                "Merrow reveals that a second ledger exists in the Crown's east archive. The public ledger records deaths. The hidden one records who ordered them. Someone copied pieces of both to create the book in your possession. To prove anything, you need the original seal matrix kept behind the refuge's ancient stone door and the royal ledger in the capital.",
                "act-iii", "Black Stag Refuge",
                [
                    Choice("open-antler-vault", "Open the ancient stone door and recover the original seal matrix.", "antler-vault"),
                    Choice("skip-vault", "Leave the old relics alone and focus on the royal archive.", "hunter-return"),
                ]),

            Scene("antler-vault", "The Door Beneath the Roots",
                "The stone door has no lock. When you touch the broken-antler carving, the chamber answers with a pulse of cold that is not air. For a moment you see a stag moving between trees that have been dead for centuries. Isi sees it too.",
                "act-iii", "Antler Vault",
                [
                    Choice("resist-mark", "Pull your hand away and study the vision rather than following it.", check: Roll("vault-wisdom", AdventureAbility.Wisdom, 14, "insight", "vault-seal", "vault-marked")),
                    Choice("follow-vision", "Let the vision take you deeper.", requires: [Req(AdventureConditionKind.SkillMinimum, "arcana", 2)], check: Roll("vault-arcana", AdventureAbility.Intelligence, 14, "arcana", "vault-vision", "vault-marked")),
                ]),

            Scene("vault-marked", "The Mark Beneath Skin",
                "The vision breaks with the sensation of antlers scraping across the inside of your skull. A black branchlike mark appears beneath the skin of your wrist. It does not hurt. That is worse.",
                "act-iii", "Antler Vault",
                [Choice("accept-mark", "Keep moving. Whatever the mark is, you need the truth first.", "vault-seal", effects: [Fx(AdventureEffectKind.ApplyCondition, "marked")])]),

            Scene("vault-vision", "Before the Couriers",
                "The vision is not a memory you recognize. People in ancient clothing kneel beside a wounded stag while soldiers burn a village beyond the trees. Someone whispers a promise: names will be carried where kings cannot reach. The symbol predates the courier network by far more than centuries of bureaucracy can explain.",
                "act-iii", "Antler Vault",
                [Choice("take-vision", "Remember the promise and search the chamber.", "vault-seal", effects: [Fx(AdventureEffectKind.SetFlag, "saw-first-stag")])]),

            Scene("vault-seal", "The Original Matrix",
                "Inside a cedar box lies the original metal seal matrix. Its broken antler is crisp, nothing like the doubled copy on your letter. Beside it is a royal counterseal impression from the east archive.",
                "act-iii", "Antler Vault",
                [Choice("take-seal-copy", "Take a wax copy of the royal counterseal.", "hunter-return", effects: [Fx(AdventureEffectKind.AddItem, "royal-seal-copy")])]),

            Scene("hunter-return", "Edrin at the Door",
                "Edrin Vale arrives before you can leave. This time his sword remains sheathed. He drops a bloodstained royal dispatch on the council table. \"They know the hospice was opened. Hunters are already on the road. Decide what you believe while you still have a refuge to believe it in.\"",
                "act-iii", "Black Stag Refuge",
                [
                    Choice("trust-edrin", "Hear Edrin's plan.", "edrin-plan", effects: [Fx(AdventureEffectKind.SetFlag, "edrin-ally")]),
                    Choice("trust-stag", "Stand with the refuge and prepare to move the witnesses.", "refuge-evacuation", effects: [Fx(AdventureEffectKind.SetFlag, "stag-ally")]),
                    Choice("trust-isi", "Let Isi decide which route keeps the missing alive.", "isi-route", requires: [Req(AdventureConditionKind.NpcRelationMinimum, "isi", 2)]),
                ], "Edrin Vale"),

            Scene("edrin-plan", "Burn the Trail, Save the Names",
                "Edrin wants to abandon the refuge, scatter the witnesses and enter the capital through forgotten aqueduct tunnels. He does not care whether the Black Stag survives as an organization. \"People matter. Symbols don't.\"",
                "act-iii", "Black Stag Refuge",
                [Choice("take-edrin-route", "Take Edrin's route toward the capital.", "ash-road", effects: [Fx(AdventureEffectKind.SetFlag, "route-edrin")])], "Edrin Vale"),

            Scene("refuge-evacuation", "The Refuge Moves",
                "Seran orders every letter burned and every witness divided into groups of four. You help hide names inside ordinary trade manifests. By dusk, the refuge looks abandoned enough to fool anyone who does not know where to look.",
                "act-iii", "Black Stag Refuge",
                [Choice("take-stag-route", "Escort the last group through the forest road.", "ash-road", effects: [Fx(AdventureEffectKind.SetFlag, "route-stag")])]),

            Scene("isi-route", "The Path with No Marker",
                "Isi chooses neither plan. She knows an old Ashgrove trail that crosses the ravine above the royal road. It is slower, but it avoids the checkpoints and passes near the place her brother was last seen.",
                "act-iii", "Black Stag Refuge",
                [Choice("take-isi-route", "Trust Isi and take the Ashgrove trail.", "ash-road", effects: [Fx(AdventureEffectKind.SetFlag, "route-isi"), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)])], "Isi"),

            // ACT IV — THE HUNT
            Scene("ash-road", "The Hunt Begins",
                "The forest changes after sundown. Birds stop calling. Twice, Isi finds boot prints where none should be. The royal hunters are not following your trail; they are moving ahead of it.",
                "act-iv", "Ash Road",
                [
                    Choice("set-ambush", "Turn the hunt around and set an ambush.", check: Roll("hunt-stealth", AdventureAbility.Dexterity, 14, "stealth", "ambush-success", "ambush-failure")),
                    Choice("move-fast", "Push for the ravine before they close the road.", check: Roll("hunt-survival", AdventureAbility.Wisdom, 13, "survival", "ravine", "ambush-failure")),
                ]),

            Scene("ambush-success", "Hunters in the Ferns",
                "Your ambush catches two royal scouts without raising an alarm. One carries orders signed by the east archive. The other carries a vial of black poison and a map marking a prison wagon headed toward the capital.",
                "act-iv", "Ash Road",
                [Choice("take-scout-supplies", "Take the antidote and follow the prison-wagon route.", "ravine", effects: [Fx(AdventureEffectKind.AddItem, "antidote"), Fx(AdventureEffectKind.SetFlag, "knows-prison-wagon")])]),

            Scene("ambush-failure", "The First Bolt",
                "A crossbow snaps from the dark. The bolt only grazes you, but the wound burns immediately. Isi breaks the shaft and smells the metal. \"Poison. Move.\"",
                "act-iv", "Ash Road",
                [
                    Choice("use-antidote-hunt", "Drink an antidote while Isi covers you.", "ravine", requires: [Req(AdventureConditionKind.ItemOwned, "antidote")], effects: [Fx(AdventureEffectKind.RemoveItem, "antidote"), Fx(AdventureEffectKind.RemoveCondition, "poisoned")]),
                    Choice("carry-poison", "Save your supplies and keep moving.", "ravine", effects: [Fx(AdventureEffectKind.ChangeHitPoints, value: -2), Fx(AdventureEffectKind.ApplyCondition, "poisoned", 4)]),
                ]),

            Scene("ravine", "The Broken Crossing",
                "The Ashgrove trail ends at a ravine where the old rope bridge has been cut. The opposite ledge is close enough to tempt you and far enough to kill you. Below, lanterns move through the trees: hunters.",
                "act-iv", "Ashgrove Ravine",
                [
                    Choice("use-rope", "Anchor your rope and cross the broken span.", "prison-road", requires: [Req(AdventureConditionKind.ItemOwned, "rope")], effects: [Fx(AdventureEffectKind.SetFlag, "safe-ravine-crossing")]),
                    Choice("jump-ravine", "Make the jump before the hunters arrive.", check: Roll("ravine-athletics", AdventureAbility.Strength, 15, "athletics", "prison-road", "ravine-fall")),
                    Choice("find-way-down", "Climb into the ravine and use the streambed.", check: Roll("ravine-survival", AdventureAbility.Wisdom, 14, "survival", "prison-road", "ravine-fall")),
                ]),

            Scene("ravine-fall", "Stone and Blood",
                "The descent becomes a fall. You stop hard against wet stone. Isi reaches you seconds later, furious and relieved in equal measure.",
                "act-iv", "Ashgrove Ravine",
                [Choice("ravine-recover", "Get back on your feet and keep moving.", "prison-road", effects: [Fx(AdventureEffectKind.ChangeHitPoints, value: -4), Fx(AdventureEffectKind.ApplyCondition, "bleeding", 3), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 1)])], "Isi"),

            Scene("prison-road", "The Wagon in the Rain",
                "Near midnight you find the prison wagon. Through a barred slit, a wood elf raises his head when Isi whispers a childhood name. Her brother is alive. He is thinner than memory and wearing the uniform of a royal archivist.",
                "act-iv", "Old Royal Road",
                [
                    Choice("free-brother", "Free Isi's brother before going anywhere near the capital.", "wagon-rescue", effects: [Fx(AdventureEffectKind.SetFlag, "rescued-isi-brother")]),
                    Choice("follow-wagon", "Follow the wagon. It may lead straight into the archive.", "wagon-follow", effects: [Fx(AdventureEffectKind.SetFlag, "followed-wagon")]),
                    Choice("ask-isi", "Let Isi choose.", "wagon-rescue", requires: [Req(AdventureConditionKind.NpcRelationMinimum, "isi", 2)], effects: [Fx(AdventureEffectKind.SetFlag, "rescued-isi-brother")]),
                ], "Isi"),

            Scene("wagon-rescue", "A Name Returned",
                "The rescue is quick only because Isi has imagined it for years. Her brother, Cael, can barely stand, but he knows the archive. He forged some of the false deaths himself under threat. He also knows which official ordered the planted names: Chancellor Orven, keeper of the east archive.",
                "act-iv", "Old Royal Road",
                [Choice("cael-route", "Take Cael's service route into the capital.", "city-gates", effects: [Fx(AdventureEffectKind.SetFlag, "cael-alive"), Fx(AdventureEffectKind.SetFlag, "knows-orven"), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", 3)])], "Cael"),

            Scene("wagon-follow", "Behind the Prison Wagon",
                "You follow at a distance until the wagon passes through an archive service gate. Isi says nothing for most of the journey. When she finally does, her voice is flat. \"If he dies in there, I need you to understand that I will remember whose plan this was.\"",
                "act-iv", "Capital Outskirts",
                [Choice("follow-into-city", "Use the wagon's route to enter the capital.", "city-gates", effects: [Fx(AdventureEffectKind.SetFlag, "knows-service-gate"), Fx(AdventureEffectKind.ChangeNpcRelation, "isi", -2)])], "Isi"),

            // ACT V — THE CROWN
            Scene("city-gates", "The Crown's Shadow",
                "The capital rises through rain and chimney smoke, all towers and watchfires. The east archive is built into the old palace wall. Somewhere inside are the original ledger, the seal records and perhaps the only proof strong enough to survive daylight.",
                "act-v", "The Capital",
                [
                    Choice("service-entry", "Use the archive service route.", "archive-entry", requires: [Req(AdventureConditionKind.Flag, "knows-service-gate")]),
                    Choice("cael-entry", "Follow Cael's knowledge of the staff passages.", "archive-entry", requires: [Req(AdventureConditionKind.Flag, "cael-alive")]),
                    Choice("stag-entry", "Use the Black Stag contact hidden among the court messengers.", "archive-entry", requires: [Req(AdventureConditionKind.ItemOwned, "courier-token")]),
                    Choice("break-entry", "Enter through the old aqueduct beneath the wall.", check: Roll("archive-entry-check", AdventureAbility.Dexterity, 14, "stealth", "archive-entry", "archive-captured")),
                ]),

            Scene("archive-captured", "Chains Under the Palace",
                "The aqueduct exit is watched. You wake with your wrists bound beneath an archive stairwell. Isi is somewhere beyond the door, arguing with a guard in a language you do not recognize.",
                "act-v", "East Archive Cells",
                [
                    Choice("escape-bindings", "Work your hands free before the guard returns.", check: Roll("restraint-escape", AdventureAbility.Dexterity, 14, "sleight-of-hand", "archive-entry", "archive-entry"), effects: [Fx(AdventureEffectKind.ApplyCondition, "restrained", 2)], success: [Fx(AdventureEffectKind.RemoveCondition, "restrained")]),
                ]),

            Scene("archive-entry", "The East Archive",
                "Rows of shelves vanish into darkness beneath the palace. Every shelf is a weapon disguised as paperwork: births, debts, pardons, deaths. Isi points to a locked iron door marked with the same office crest as your copied seal.",
                "act-v", "East Archive",
                [
                    Choice("use-seal-copy", "Use the copied seal to bluff your way past the record keeper.", "archive-vault", requires: [Req(AdventureConditionKind.ItemOwned, "royal-seal-copy")], check: Roll("seal-bluff", AdventureAbility.Charisma, 13, "persuasion", "archive-vault", "archive-fight")),
                    Choice("pick-vault", "Open the iron door quietly.", requires: [Req(AdventureConditionKind.SkillMinimum, "sleight-of-hand", 2)], check: Roll("vault-lock", AdventureAbility.Dexterity, 14, "sleight-of-hand", "archive-vault", "archive-fight")),
                    Choice("force-vault", "Force the lock and accept the noise.", check: Roll("vault-force", AdventureAbility.Strength, 15, "athletics", "archive-vault", "archive-fight")),
                ]),

            Scene("archive-fight", "Steel Between Shelves",
                "The record keeper shouts before the lock gives. Guards flood the aisle. The fight is too close for clean movement; shelves topple, papers ignite and someone drives you to the floor.",
                "act-v", "East Archive",
                [
                    Choice("fight-up", "Drive the nearest guard back and regain your feet.", check: Roll("archive-combat", AdventureAbility.Strength, 14, "athletics", "archive-vault", "archive-vault"), effects: [Fx(AdventureEffectKind.ApplyCondition, "prone", 1)], success: [Fx(AdventureEffectKind.RemoveCondition, "prone")], failure: [Fx(AdventureEffectKind.ChangeHitPoints, value: -3)]),
                ]),

            Scene("archive-vault", "The Ledger Beneath the Crown",
                "The hidden ledger is exactly where Cael said it would be. It records not only disappearances, but the people who paid for them. Judges. Officers. Merchants. Chancellor Orven. And beneath several older entries, a symbol drawn in ink instead of stamped: the intact stag, both antlers whole.",
                "act-v", "East Archive Vault",
                [
                    Choice("compare-ledgers", "Compare the royal ledger with your verified witness list.", "orven-arrives", requires: [Req(AdventureConditionKind.ItemOwned, "witness-list")], effects: [Fx(AdventureEffectKind.SetFlag, "proof-complete")]),
                    Choice("take-ledger-final", "Take the royal ledger and leave before anyone can stop you.", "orven-arrives", effects: [Fx(AdventureEffectKind.SetFlag, "proof-partial")]),
                ]),

            Scene("orven-arrives", "Chancellor Orven",
                "Orven enters without guards. He is older than you expected and completely unsurprised to see the Black Stag ledger in your hands. \"You think this is corruption,\" he says. \"It is containment. Every generation discovers that names can topple kingdoms. Every generation needs someone willing to decide which names survive.\"",
                "act-v", "East Archive Vault",
                [
                    Choice("orven-question", "Ask what the intact stag symbol means.", "whole-stag"),
                    Choice("orven-refuse", "Tell him no office gets to own the dead.", "final-choice", effects: [Fx(AdventureEffectKind.SetFlag, "rejected-orven")]),
                    Choice("orven-listen", "Hear his offer before deciding.", "orven-offer"),
                ], "Chancellor Orven"),

            Scene("whole-stag", "The Antler That Was Never Broken",
                "Orven looks at the intact stag and, for the first time, loses control of his expression. \"That mark predates the Crown. We did not create the Stag. We broke its antler because people fear a wounded symbol less than a whole one.\" Isi's hand closes around her Ashgrove charm.",
                "act-v", "East Archive Vault",
                [Choice("whole-stag-choice", "Keep the older mystery for another night. Decide what to do with the proof.", "final-choice", effects: [Fx(AdventureEffectKind.SetFlag, "whole-stag-known")])], "Chancellor Orven"),

            Scene("orven-offer", "A Quiet Place at the Table",
                "Orven offers you a place inside the archive. Not gold. Not rank. Access. The power to erase a hunted child's name, expose a murderer, or bury a war before it starts. \"Someone will choose. Better someone who has seen the cost.\"",
                "act-v", "East Archive Vault",
                [
                    Choice("reject-offer", "Reject the offer and decide the ledger's fate yourself.", "final-choice"),
                    Choice("accept-offer", "Accept the archive key and become part of the machine.", "ending-watchful", effects: [Fx(AdventureEffectKind.SetFlag, "accepted-orven")]),
                ], "Chancellor Orven"),

            Scene("final-choice", "What Survives the Night",
                "You have enough truth to ruin people, save people, or do both badly. Isi stands beside you, silent. Somewhere below, bells begin to ring. There is no clean ending left — only the one you are willing to own.",
                "act-v", "East Archive Vault",
                [
                    Choice("expose-crown", "Release the verified evidence and expose the Crown's disappearances.", "ending-crown-exposed", requires: [Req(AdventureConditionKind.Flag, "proof-complete")]),
                    Choice("save-stag", "Destroy the planted names, preserve the witness routes, and let the Black Stag survive.", "ending-stag", requires: [Req(AdventureConditionKind.Flag, "stag-ally")]),
                    Choice("save-missing", "Use the archive to recover the missing before exposing anyone.", "ending-missing-return", requires: [Req(AdventureConditionKind.Flag, "promised-missing")]),
                    Choice("isi-ending", "Give Isi the choice over the records tied to her grove.", "ending-missing-return", requires: [Req(AdventureConditionKind.NpcRelationMinimum, "isi", 3)]),
                    Choice("burn-both", "Burn both ledgers. Deny the Crown and the Stag the power of the list.", "ending-ashes"),
                    Choice("bury-truth", "Hide the true ledger and leave the official story intact.", "ending-quiet-lie"),
                ]),

            Ending("ending-crown-exposed", "Ending — The Crown Exposed",
                "Copies of the verified ledger reach temples, guilds and foreign courts before sunrise. The Crown calls them forgeries until three judges flee the city in the same hour. The Black Stag becomes a symbol again, but not one anyone fully controls. Isi returns to Grayfen carrying names that are finally allowed to belong to the living.", "crown-exposed", 420),

            Ending("ending-stag", "Ending — The Black Stag Survives",
                "You destroy every planted name and preserve the routes that hid real witnesses. The network loses its council, its refuge and most of its certainty, but not its purpose. Months later, a letter arrives with no seal. Inside is a single line in Isi's hand: STILL CARRYING THEM.", "black-stag", 380),

            Ending("ending-missing-return", "Ending — The Missing Return",
                "You do not give the city a scandal. You give families doors opening at midnight, old names returned, new names protected and people thought dead walking home. It is slower than justice and less satisfying than revenge. Isi calls it the first honest thing anyone has done with the ledger.", "missing-return", 450),

            Ending("ending-ashes", "Ending — Ashes of the Crown",
                "The two ledgers burn together. By morning, neither the Crown nor the Black Stag can prove who belonged to whom. Some monsters escape judgment. Some innocent people escape a purge. History will call it cowardice, mercy or sabotage depending on who gets to write first.", "ashes-of-the-crown", 330),

            Ending("ending-quiet-lie", "Ending — The Quiet Lie",
                "You hide the true ledger where neither faction can reach it and allow the public record to stand. The disappearances remain rumors. The network fractures. The Crown survives. You keep the names because somebody must — and discover that carrying a secret can resemble serving it.", "quiet-lie", 300),

            Ending("ending-watchful", "Ending — The Watchful Hand",
                "You accept Orven's key, but not his certainty. From inside the archive you begin changing small things: a warrant delayed, a child erased from a hunter's list, a corrupt official's sealed file copied twice. Whether you joined the machine or infected it will depend on what you become next.", "watchful-hand", 400),
        };

        var acts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["act-i"] = "Act I — The Letter",
            ["act-ii"] = "Act II — The Missing Names",
            ["act-iii"] = "Act III — The Broken Antlers",
            ["act-iv"] = "Act IV — The Hunt",
            ["act-v"] = "Act V — The Crown",
        };

        var adventure = new AdventureDefinition(
            "black-stag",
            2,
            "The Black Stag",
            "A dark five-act mystery of hidden witnesses, broken loyalties and a symbol older than the kingdom that fears it.",
            "crooked-crown",
            scenes,
            AdventureBlueprintCatalog.BlackStagItems,
            AdventureBlueprintCatalog.StandardConditions,
            acts);
        AdventureDefinitionValidator.Validate(adventure);
        return adventure;
    }
}
