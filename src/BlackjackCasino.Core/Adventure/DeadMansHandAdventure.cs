namespace BlackjackCasino.Core.Adventure;

/// <summary>A six-act noir investigation with persistent evidence, usable items and consequential endings.</summary>
public static partial class DeadMansHandAdventure
{
    public const string AdventureId = "dead-mans-hand";

    public static AdventureDefinition Build()
    {
        AdventureCharacterCatalogDefinition characters = BuildCharacterCatalog();
        CaseBoardDefinition caseBoard = ExpandCaseBoard(BuildCaseBoard());

        var acts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["act-1"] = "Act I · Rain on Glass",
            ["act-2"] = "Act II · The City Collects",
            ["act-3"] = "Act III · Favors and Knives",
            ["act-4"] = "Act IV · The Badge Has Teeth",
            ["act-5"] = "Act V · Everyone Owes Someone",
            ["act-6"] = "Act VI · Dead Man's Hand",
        };

        AdventureSceneDefinition[] scenes =
        [
            new(
                "rain-office",
                "A Knock After Midnight",
                "Rain writes crooked lines down the office glass. The city outside is all wet pavement, tired neon and the kind of traffic that never quite stops. At twelve seventeen, Evelyn Vane walks in without an appointment. Her husband, Arthur Vane, left the Hotel Mirabelle yesterday evening and never came home. The police logged it as a rich man taking a private night away. Evelyn does not believe them. Neither does the envelope of cash she sets on your desk.",
                Speaker: "Evelyn Vane",
                Choices:
                [
                    new(
                        "ask-timeline",
                        "Start with the timeline. Make her account precise.",
                        Check: new AdventureCheckDefinition(
                            "read-evelyn-timeline",
                            AdventureAbility.Wisdom,
                            10,
                            "investigation",
                            SuccessSceneId: "timeline-clean",
                            FailureSceneId: "timeline-frayed")),
                    new(
                        "ask-ivory-room",
                        "Ask why the Ivory Room name is written on the back of Arthur's hotel card.",
                        Check: new AdventureCheckDefinition(
                            "press-evelyn-ivory",
                            AdventureAbility.Charisma,
                            12,
                            "interrogation",
                            SuccessSceneId: "ivory-name",
                            FailureSceneId: "client-closes")),
                    new(
                        "take-case",
                        "Take the case and go straight to the Hotel Mirabelle.",
                        "hotel-lobby"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "arthur-vane"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "evelyn-vane"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "lead-where-vane"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "vane-marriage"),
                    new AdventureEffect(AdventureEffectKind.ConfirmCaseConnection, "vane-marriage"),
                ],
                ActId: "act-1",
                Location: "Private Office · Calder Street"),

            new(
                "timeline-clean",
                "Seventeen Missing Minutes",
                "Evelyn answers in the clipped rhythm of someone who has rehearsed the story because nobody official listened the first time. Arthur checked into room 306 at 6:42 p.m. He called home at 8:03. A hotel porter saw him return from the street at 9:11. At 9:28, the switchboard recorded an outgoing connection from his room. After that, seventeen minutes are missing from the hotel's internal log. Evelyn never mentions those seventeen minutes until you make her walk through the sequence twice.",
                Choices:
                [
                    new("timeline-to-hotel", "Go to the Mirabelle before the night staff changes.", "hotel-lobby"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "hotel-mirabelle"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "lead-room-call"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "vane-hotel"),
                    new AdventureEffect(AdventureEffectKind.ConfirmCaseConnection, "vane-hotel"),
                ],
                ActId: "act-1",
                Location: "Private Office · Calder Street"),

            new(
                "timeline-frayed",
                "A Story With Soft Edges",
                "The sequence refuses to settle. Evelyn remembers the call, the hotel, the rain, and the fact that Arthur sounded afraid, but exact times slide away from her. Whether that is grief, exhaustion or concealment is impossible to tell tonight. You still get one useful fact: room 306, Hotel Mirabelle.",
                Choices:
                [
                    new("frayed-to-hotel", "Work with what you have. Go to the Mirabelle.", "hotel-lobby"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "hotel-mirabelle"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "vane-hotel"),
                ],
                ActId: "act-1",
                Location: "Private Office · Calder Street"),

            new(
                "ivory-name",
                "The Room Behind the Casino",
                "Evelyn's expression changes before her answer does. The Ivory Room is not a hotel bar. It is a private gaming room above the Monarch Casino, technically invitation-only and practically owned by Vincent Moretti. Arthur had sworn he stopped going there months ago. Evelyn found the name on his hotel card this morning, written in his own hand.",
                Choices:
                [
                    new("ivory-to-hotel", "Pocket the name. Start where Arthur was last seen: the Mirabelle.", "hotel-lobby"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "ivory-room"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "vincent-moretti"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "lead-why-ivory"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "moretti-ivory"),
                    new AdventureEffect(AdventureEffectKind.ConfirmCaseConnection, "moretti-ivory"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "vane-ivory"),
                ],
                ActId: "act-1",
                Location: "Private Office · Calder Street"),

            new(
                "client-closes",
                "The First Lie",
                "For half a second Evelyn looks at the card instead of you. Then she says the Ivory Room means nothing to her. It is a bad lie, but not a careless one. Push harder now and she may walk. Let it sit and the lie becomes something you can return to later.",
                Choices:
                [
                    new("close-to-hotel", "Let the lie stand for now. Go to the Mirabelle.", "hotel-lobby"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "hotel-mirabelle"),
                    new AdventureEffect(AdventureEffectKind.UnlockTheory, "theory-evelyn-withholding"),
                ],
                ActId: "act-1",
                Location: "Private Office · Calder Street"),

            new(
                "hotel-lobby",
                "The Mirabelle",
                "The Hotel Mirabelle was built for people who wanted marble to make their sins look expensive. At one in the morning, the lobby is quiet enough to hear the rain ticking against the brass revolving door. The night clerk recognizes Arthur Vane's name immediately, then tries very hard not to.",
                Choices:
                [
                    new(
                        "talk-clerk",
                        "Convince the night clerk that helping you is safer than obstructing you.",
                        Check: new AdventureCheckDefinition(
                            "hotel-clerk-persuasion",
                            AdventureAbility.Charisma,
                            13,
                            "persuasion",
                            SuccessSceneId: "clerk-cooperates",
                            FailureSceneId: "service-hall")),
                    new(
                        "watch-lobby",
                        "Stay quiet and watch what the staff are trying not to look at.",
                        Check: new AdventureCheckDefinition(
                            "hotel-observation",
                            AdventureAbility.Wisdom,
                            12,
                            "observation",
                            SuccessSceneId: "switchboard-desk",
                            FailureSceneId: "service-hall")),
                    new(
                        "bribe-clerk",
                        "Put five coins on the desk and ask for room 306 without another word.",
                        "room-306",
                        Conditions: [new AdventureCondition(AdventureConditionKind.CoinsMinimum, Value: 5)],
                        Effects: [new AdventureEffect(AdventureEffectKind.ChangeCoins, Value: -5)]),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "hotel-mirabelle"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "vane-hotel"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Grand Lobby"),

            new(
                "clerk-cooperates",
                "A Key That Wasn't Returned",
                "The clerk's resistance gives way all at once. Arthur Vane checked in alone. Nobody signed the visitor book for 306. The room key was never returned, yet housekeeping found the door unlocked at dawn. The clerk produces the duplicate key from beneath the desk and insists you never got it from him.",
                Choices:
                [
                    new("take-key", "Take the duplicate key and go upstairs.", "room-306"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "key-306"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "key-hotel"),
                    new AdventureEffect(AdventureEffectKind.ConfirmCaseConnection, "key-hotel"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Front Desk"),

            new(
                "switchboard-desk",
                "The Missing Line",
                "The clerk keeps glancing toward the switchboard alcove. The operator has stepped away, leaving the night's call sheet under a glass paperweight. Room 306 appears twice. The second entry has been cut out with a razor so cleanly that the missing strip looks deliberate rather than damaged.",
                Choices:
                [
                    new("copy-log", "Copy the surviving entries and head upstairs.", "room-306"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "switchboard-log"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "lead-room-call"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "log-hotel"),
                    new AdventureEffect(AdventureEffectKind.ConfirmCaseConnection, "log-hotel"),
                    new AdventureEffect(AdventureEffectKind.UnlockTheory, "theory-record-erased"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Switchboard"),

            new(
                "service-hall",
                "The Way Around",
                "The front desk closes ranks. Hotels have two maps, though: the one guests see and the one staff use. A service corridor behind the elevators runs past linen storage, maintenance stairs and, eventually, the third floor. The lock on the stair door is old but not decorative.",
                Choices:
                [
                    new(
                        "pick-service-lock",
                        "Work the service-door lock without drawing attention.",
                        Check: new AdventureCheckDefinition(
                            "service-lock",
                            AdventureAbility.Dexterity,
                            12,
                            "lockpicking",
                            SuccessSceneId: "room-306",
                            FailureSceneId: "hale-arrives")),
                    new(
                        "force-service-door",
                        "Put your shoulder into it and accept the noise.",
                        Check: new AdventureCheckDefinition(
                            "service-force",
                            AdventureAbility.Strength,
                            13,
                            SuccessSceneId: "room-306",
                            FailureSceneId: "hale-arrives")),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Service Corridor"),

            new(
                "hale-arrives",
                "Detective Hale",
                "The lock wins the argument. Before you can decide whether to try again, a voice behind you says your name. Detective Elias Hale stands under the corridor's weak service light with his coat still wet from outside. He says Vane is not a police matter. Then he asks what Evelyn told you. The order of those two statements matters.",
                Speaker: "Detective Elias Hale",
                Choices:
                [
                    new(
                        "read-hale",
                        "Study Hale before answering him.",
                        Check: new AdventureCheckDefinition(
                            "read-hale",
                            AdventureAbility.Wisdom,
                            13,
                            "observation",
                            SuccessSceneId: "hale-slip",
                            FailureSceneId: "hale-lets-pass")),
                    new("answer-hale", "Give him nothing useful and ask why he is here.", "hale-lets-pass"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "elias-hale"),
                    new AdventureEffect(AdventureEffectKind.UnlockTheory, "theory-hale-knows-more"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Service Corridor"),

            new(
                "hale-slip",
                "Room Three-Oh-Six",
                "Hale's eyes move toward the elevator when you mention Vane, then to the third-floor indicator before you ever say the room number. He catches himself a second too late. When you ask how he knows 306, his expression turns flat. 'Everybody knows now,' he says. Nobody in the lobby did.",
                Choices:
                [
                    new("hale-slip-room", "Remember the mistake. Go to 306.", "room-306"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.SupportTheory, "theory-hale-knows-more"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Service Corridor"),

            new(
                "hale-lets-pass",
                "Professional Courtesy",
                "Hale does not answer the question. He steps aside instead, the gesture too deliberate to be kindness. 'Five minutes,' he says. 'Then you were never on this floor.' Whether he is helping you, controlling the scene or waiting to see what you find remains open.",
                Choices:
                [
                    new("hale-pass-room", "Take the five minutes. Enter room 306.", "room-306"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "elias-hale"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Third Floor"),

            new(
                "room-306",
                "Room 306",
                "The room has been cleaned badly rather than carefully. One chair is wrong for the table. A dark crescent remains in the carpet where somebody scrubbed until the fibers changed color. A brass cufflink sits beneath the radiator. Half a photograph is trapped behind the wastebasket. There is no body, no suitcase and no sign that Arthur Vane packed before leaving.",
                Choices:
                [
                    new(
                        "study-cufflink",
                        "Examine the brass cufflink and the cleaned stain together.",
                        Check: new AdventureCheckDefinition(
                            "cufflink-investigation",
                            AdventureAbility.Intelligence,
                            12,
                            "investigation",
                            SuccessSceneId: "cufflink-detail",
                            FailureSceneId: "first-room-conclusion")),
                    new(
                        "study-photo",
                        "Work out what is missing from the torn photograph.",
                        Check: new AdventureCheckDefinition(
                            "photo-observation",
                            AdventureAbility.Wisdom,
                            12,
                            "observation",
                            SuccessSceneId: "photo-detail",
                            FailureSceneId: "first-room-conclusion")),
                    new(
                        "study-phone",
                        "Trace the telephone cable and compare it with the switchboard story.",
                        Check: new AdventureCheckDefinition(
                            "phone-investigation",
                            AdventureAbility.Intelligence,
                            13,
                            "investigation",
                            SuccessSceneId: "phone-detail",
                            FailureSceneId: "first-room-conclusion")),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "cufflink"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "torn-photo"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "lead-where-vane"),
                    new AdventureEffect(AdventureEffectKind.UnlockTheory, "theory-vane-knew-visitor"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "vane-room-evidence"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Room 306"),

            new(
                "cufflink-detail",
                "Not Lost By Accident",
                "The cufflink is expensive, but the interesting part is the bent stem. It was torn sideways under force. The cleaned stain sits exactly where someone standing near the radiator would have fallen or been driven down. Whatever happened here was physical. Whoever cleaned the room missed the one object that rolled out of sight.",
                Choices:
                [
                    new("cufflink-conclude", "Pin the cufflink to the board and reconsider the room.", "first-room-conclusion"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "cufflink-vane"),
                    new AdventureEffect(AdventureEffectKind.SupportTheory, "theory-vane-knew-visitor"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Room 306"),

            new(
                "photo-detail",
                "The Missing Half",
                "The photograph shows Arthur Vane outside the Ivory Room beside Vincent Moretti. A third person has been torn away so precisely that only one gloved hand remains on Vane's shoulder. The picture is old enough to contradict Evelyn's claim that Arthur stopped visiting months ago, but recent enough that the Monarch's winter marquee is visible behind them.",
                Choices:
                [
                    new("photo-conclude", "Keep the photograph. The casino is now part of the case.", "first-room-conclusion"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "ivory-room"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "vincent-moretti"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "lead-why-ivory"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "photo-ivory"),
                    new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "vane-ivory"),
                    new AdventureEffect(AdventureEffectKind.SupportTheory, "theory-evelyn-withholding"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Room 306"),

            new(
                "phone-detail",
                "A Line That Goes Nowhere",
                "The room phone has been opened and reassembled. One screw is fresh. Inside, the line pair carries a tiny bridge clip that would let someone monitor or reroute a call from the service junction. This was not done by a guest improvising with a pocketknife. Somebody with access prepared the room before Vane arrived.",
                Choices:
                [
                    new("phone-conclude", "The missing call was engineered. Add it to the board.", "first-room-conclusion"),
                ],
                OnEnterEffects:
                [
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "switchboard-log"),
                    new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "lead-room-call"),
                    new AdventureEffect(AdventureEffectKind.ResolveLead, "lead-room-call"),
                    new AdventureEffect(AdventureEffectKind.UnlockTheory, "theory-record-erased"),
                    new AdventureEffect(AdventureEffectKind.SupportTheory, "theory-record-erased"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Room 306"),

            new(
                "first-room-conclusion",
                "The Case Changes Shape",
                "By the time you leave room 306, this is no longer a simple disappearance. Someone prepared the room, someone cleaned it, and at least one person with institutional access wants the timeline softened. The case board has its first real shape now: Vane, the Mirabelle, the Ivory Room, Hale, the erased call and the objects the cleaner missed. None of it proves who took Arthur Vane. It proves only that several people need you to believe nothing happened.",
                Choices:
                [
                    new("foundation-end", "Close the current foundation preview.", "foundation-checkpoint"),
                ],
                ActId: "act-1",
                Location: "Hotel Mirabelle · Third Floor"),

            new(
                "foundation-checkpoint",
                "End of the First Thread",
                "The rain is still falling when you step back into the street. The next move can point toward the Ivory Room, the police department, Evelyn Vane's omissions or the missing seventeen minutes. Those routes belong to the next content expansion; the campaign foundation, custom roster and live investigation board are now in place.",
                IsEnding: true,
                EndingId: "foundation-preview",
                ActId: "act-1",
                Location: "Calder Street · 2:14 A.M."),
        ];

        return new AdventureDefinition(
            AdventureId,
            2,
            "Dead Man's Hand",
            "Everyone lies. Some people just get paid for it.",
            "rain-office",
            ExpandOpening(scenes).Concat(BuildLaterActs()).ToArray(),
            Items: NoirItems,
            Acts: acts,
            CharacterCatalog: characters,
            CaseBoard: caseBoard);
    }

    private static AdventureCharacterCatalogDefinition BuildCharacterCatalog()
    {
        var labels = new Dictionary<AdventureAbility, string>
        {
            [AdventureAbility.Strength] = "Brawn",
            [AdventureAbility.Dexterity] = "Reflex",
            [AdventureAbility.Constitution] = "Nerve",
            [AdventureAbility.Intelligence] = "Intellect",
            [AdventureAbility.Wisdom] = "Insight",
            [AdventureAbility.Charisma] = "Presence",
        };
        var shortLabels = new Dictionary<AdventureAbility, string>
        {
            [AdventureAbility.Strength] = "BRAWN",
            [AdventureAbility.Dexterity] = "REFLEX",
            [AdventureAbility.Constitution] = "NERVE",
            [AdventureAbility.Intelligence] = "INT",
            [AdventureAbility.Wisdom] = "INSIGHT",
            [AdventureAbility.Charisma] = "PRES",
        };
        AdventureSkillDefinition[] skills =
        [
            new("investigation", "Investigation", AdventureAbility.Intelligence),
            new("observation", "Observation", AdventureAbility.Wisdom),
            new("interrogation", "Interrogation", AdventureAbility.Charisma),
            new("persuasion", "Persuasion", AdventureAbility.Charisma),
            new("deception", "Deception", AdventureAbility.Charisma),
            new("intimidation", "Intimidation", AdventureAbility.Strength),
            new("streetwise", "Streetwise", AdventureAbility.Wisdom),
            new("underworld", "Underworld", AdventureAbility.Wisdom),
            new("police-procedure", "Police Procedure", AdventureAbility.Intelligence),
            new("research", "Research", AdventureAbility.Intelligence),
            new("medicine", "Medicine", AdventureAbility.Intelligence),
            new("firearms", "Firearms", AdventureAbility.Dexterity),
            new("driving", "Driving", AdventureAbility.Dexterity),
            new("gambling", "Gambling", AdventureAbility.Wisdom),
            new("sleight-of-hand", "Sleight of Hand", AdventureAbility.Dexterity),
            new("lockpicking", "Lockpicking", AdventureAbility.Dexterity),
            new("disguise", "Disguise", AdventureAbility.Charisma),
            new("contacts", "Contacts", AdventureAbility.Charisma),
        ];

        AdventureClassDefinition[] classes =
        [
            Archetype(
                "private-eye", "Private Eye",
                "Classic investigator. Reads rooms, contradictions and people before committing to a theory.",
                18, 12, Stats(11, 13, 13, 15, 16, 12),
                Skills(("investigation", 3), ("observation", 3), ("interrogation", 2), ("streetwise", 2)),
                "hunch", "Hunch", ["detective", "conspiracy"]),
            Archetype(
                "fixer", "The Fixer",
                "A social connector who solves problems through favors, introductions and debts that always come due.",
                17, 11, Stats(10, 12, 13, 13, 14, 16),
                Skills(("contacts", 3), ("persuasion", 3), ("deception", 2), ("underworld", 3)),
                "favors", "Favors", ["underworld", "blackmail"]),
            Archetype(
                "reporter", "The Reporter",
                "Information specialist with cultivated sources and a dangerous habit of asking one question too many.",
                15, 11, Stats(9, 12, 12, 16, 15, 14),
                Skills(("investigation", 3), ("research", 3), ("observation", 2), ("persuasion", 2)),
                "sources", "Sources", ["detective", "conspiracy", "publication"]),
            Archetype(
                "ex-cop", "The Ex-Cop",
                "Knows procedure, crime scenes and exactly which doors inside the department are never written down.",
                20, 13, Stats(13, 13, 15, 14, 14, 12),
                Skills(("police-procedure", 3), ("investigation", 2), ("interrogation", 2), ("firearms", 2)),
                "reputation", "Reputation", ["badge", "fugitive"]),
            Archetype(
                "grifter", "The Grifter",
                "Con artist and gambler. Wins access by becoming whoever the room expects to see.",
                15, 12, Stats(9, 16, 12, 13, 14, 16),
                Skills(("deception", 3), ("gambling", 3), ("sleight-of-hand", 3), ("disguise", 2)),
                "heat", "Heat", ["underworld", "blackmail", "casino"]),
            Archetype(
                "veteran", "The Veteran",
                "Hard to frighten and harder to stop. Opens forceful and protective solutions when subtle plans collapse.",
                23, 14, Stats(16, 14, 16, 10, 14, 11),
                Skills(("firearms", 3), ("intimidation", 3), ("driving", 2), ("observation", 2)),
                "resolve", "Resolve", ["fugitive", "underworld", "protection"]),
        ];

        return new AdventureCharacterCatalogDefinition(classes, labels, shortLabels, skills);
    }

    private static CaseBoardDefinition BuildCaseBoard()
    {
        CaseNodeDefinition[] nodes =
        [
            new("arthur-vane", CaseNodeCategory.Person, "Arthur Vane", "Industrialist. Missing after checking into room 306 at the Hotel Mirabelle.", "Missing person", X: 105, Y: 110),
            new("evelyn-vane", CaseNodeCategory.Person, "Evelyn Vane", "Arthur's wife and the person who hired you. She is withholding at least part of the story.", "Client", X: 390, Y: 70),
            new("elias-hale", CaseNodeCategory.Person, "Detective Elias Hale", "City detective who insists Vane is not a police matter while knowing details he was never told.", "Police", X: 690, Y: 90),
            new("vincent-moretti", CaseNodeCategory.Person, "Vincent Moretti", "Organizer, casino power broker and practical owner of the Ivory Room.", "Underworld", X: 1040, Y: 105),
            new("hotel-mirabelle", CaseNodeCategory.Location, "Hotel Mirabelle", "Arthur Vane's last confirmed location. Room 306 is the center of the opening timeline.", "Last known location", X: 155, Y: 350),
            new("ivory-room", CaseNodeCategory.Location, "The Ivory Room", "Invitation-only gaming room above the Monarch Casino.", "Private casino room", X: 1000, Y: 350),
            new("key-306", CaseNodeCategory.Evidence, "Duplicate Key · 306", "Hotel duplicate for room 306. The original key was never returned.", "Physical evidence", X: 365, Y: 325),
            new("switchboard-log", CaseNodeCategory.Evidence, "Switchboard Log", "The second room-306 call entry was removed cleanly with a blade.", "Document", X: 575, Y: 360),
            new("cufflink", CaseNodeCategory.Evidence, "Bent Brass Cufflink", "Recovered beneath the radiator in room 306. The stem was torn sideways under force.", "Physical evidence", X: 300, Y: 610),
            new("torn-photo", CaseNodeCategory.Evidence, "Torn Photograph", "Arthur Vane and Vincent Moretti outside the Ivory Room. A third person was deliberately removed.", "Photograph", X: 925, Y: 605),
            new("lead-where-vane", CaseNodeCategory.Lead, "Where is Arthur Vane?", "The original disappearance. No body, luggage or voluntary departure has been established.", "Open question", X: 85, Y: 760),
            new("lead-room-call", CaseNodeCategory.Lead, "Who called room 306?", "A call exists in the timeline, but its switchboard entry was removed.", "Open question", X: 545, Y: 655),
            new("lead-why-ivory", CaseNodeCategory.Lead, "Why was Vane tied to the Ivory Room?", "Arthur claimed he stopped going. The surviving evidence says otherwise.", "Open question", X: 1110, Y: 760),
            new("theory-evelyn-withholding", CaseNodeCategory.Theory, "Evelyn is withholding part of Arthur's casino history.", "A working thought, not a fact.", "Working theory", X: 450, Y: 205),
            new("theory-hale-knows-more", CaseNodeCategory.Theory, "Hale knows more than he admitted.", "His knowledge of room 306 may predate the official investigation.", "Working theory", X: 760, Y: 230),
            new("theory-vane-knew-visitor", CaseNodeCategory.Theory, "Vane may have known whoever entered room 306.", "The scene lacks signs of forced entry but shows evidence of violence.", "Working theory", X: 480, Y: 780),
            new("theory-record-erased", CaseNodeCategory.Theory, "Someone deliberately altered the Mirabelle record.", "The missing call entry and modified telephone line suggest preparation, not panic cleanup.", "Working theory", X: 760, Y: 760),
        ];

        CaseConnectionDefinition[] connections =
        [
            new("vane-marriage", "arthur-vane", "evelyn-vane", CaseConnectionType.Relationship, Label: "married"),
            new("vane-hotel", "arthur-vane", "hotel-mirabelle", CaseConnectionType.Location, Label: "room 306"),
            new("moretti-ivory", "vincent-moretti", "ivory-room", CaseConnectionType.Ownership, Label: "controls"),
            new("vane-ivory", "arthur-vane", "ivory-room", CaseConnectionType.Relationship, Label: "recent visits"),
            new("key-hotel", "key-306", "hotel-mirabelle", CaseConnectionType.Ownership, Label: "duplicate room key"),
            new("log-hotel", "switchboard-log", "hotel-mirabelle", CaseConnectionType.Communication, Label: "room 306 call sheet"),
            new("cufflink-vane", "cufflink", "arthur-vane", CaseConnectionType.Ownership, Label: "possible ownership"),
            new("photo-ivory", "torn-photo", "ivory-room", CaseConnectionType.Location, Label: "photographed outside"),
            new("vane-room-evidence", "arthur-vane", "cufflink", CaseConnectionType.Witnessed, Label: "found in his room"),
        ];

        return new CaseBoardDefinition(nodes, connections, "The Vane Case");
    }

    private static AdventureClassDefinition Archetype(
        string id,
        string name,
        string description,
        int hitPoints,
        int armor,
        IReadOnlyDictionary<AdventureAbility, int> abilities,
        IReadOnlyDictionary<string, int> skills,
        string mechanicId,
        string mechanicName,
        IReadOnlyList<string> routeTags) =>
        new(id, name, description, hitPoints, armor, abilities, skills, mechanicId, mechanicName, routeTags);

    private static IReadOnlyDictionary<AdventureAbility, int> Stats(
        int brawn,
        int reflex,
        int nerve,
        int intellect,
        int insight,
        int presence) =>
        new Dictionary<AdventureAbility, int>
        {
            [AdventureAbility.Strength] = brawn,
            [AdventureAbility.Dexterity] = reflex,
            [AdventureAbility.Constitution] = nerve,
            [AdventureAbility.Intelligence] = intellect,
            [AdventureAbility.Wisdom] = insight,
            [AdventureAbility.Charisma] = presence,
        };

    private static IReadOnlyDictionary<string, int> Skills(params (string Id, int Bonus)[] values) =>
        values.ToDictionary(value => value.Id, value => value.Bonus, StringComparer.OrdinalIgnoreCase);
}
