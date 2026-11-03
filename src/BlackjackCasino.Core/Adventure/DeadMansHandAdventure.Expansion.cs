namespace BlackjackCasino.Core.Adventure;

public static partial class DeadMansHandAdventure
{
    private static readonly IReadOnlyList<AdventureItemDefinition> NoirItems =
    [
        new("dmh-key", "Mirabelle Duplicate Key", "Opens room 306 and its locked service cabinet. The clerk wants it back when the case is over.", "key", AdventureItemKind.Utility),
        new("dmh-cufflink", "Bent Brass Cufflink", "An eagle stamped beneath the hinge; show it to people who know the city's uniforms.", "token", AdventureItemKind.Quest),
        new("dmh-photo", "Torn Photograph", "Vane, Moretti and a gloved stranger outside the Ivory Room. A witness may recognize the missing face.", "letter", AdventureItemKind.Quest),
        new("dmh-log", "Switchboard Copy", "A copied call sheet. A bridge clip can recover the route behind the missing entry.", "list", AdventureItemKind.Quest),
        new("dmh-bridge", "Telephone Bridge Clip", "A maintenance clip recovered at the Mirabelle. Use it with the call sheet at the exchange.", "item", AdventureItemKind.Utility),
        new("dmh-pass", "Ivory Room Invitation", "Evelyn's invitation admits one guest through the Monarch's private lift.", "letter", AdventureItemKind.Utility),
        new("dmh-picks", "Lockpick Roll", "A narrow set of tools; useful at a warehouse office or police service door. A forced lock can ruin the set.", "key", AdventureItemKind.Utility),
        new("dmh-aid", "Field Dressing", "One sealed dressing with antiseptic and a pressure pad. Can stabilize the wounded witness.", "potion-red", AdventureItemKind.Consumable),
        new("dmh-carbon", "Shipping Carbon", "The retained copy of a police-protected freight transfer. Independent corroboration for the ledger.", "list", AdventureItemKind.Quest),
        new("dmh-negative", "Uncut Negative", "The uncropped hotel photograph identifies Hale beside Vane. It can buy a candid conversation.", "letter", AdventureItemKind.Quest),
        new("dmh-ledger", "Signed Redevelopment Ledger", "Original transfers signed by Commissioner Rook and Arthur Vane. Can be copied, presented, traded or destroyed.", "ledger", AdventureItemKind.Quest),
        new("dmh-recording", "Switchboard Recording", "A wax dictation cylinder preserved by the exchange operator: Rook orders Vane moved before dawn.", "item", AdventureItemKind.Quest),
        new("dmh-affidavit", "Witness Affidavit", "Lena Vale's signed account, naming the transfer and the man who ordered it. Needs a living witness to stand behind it.", "letter", AdventureItemKind.Quest),
        new("dmh-ticket", "Two Ferry Berths", "Passage out of the city before the morning roadblocks. Present it at Pier Seven.", "letter", AdventureItemKind.Consumable),
    ];

    private static IEnumerable<AdventureSceneDefinition> ExpandOpening(IEnumerable<AdventureSceneDefinition> opening)
    {
        foreach (AdventureSceneDefinition scene in opening)
        {
            AdventureSceneDefinition next = scene;
            if (scene.Id == "clerk-cooperates") next = scene with { OnEnterEffects = [.. scene.OnEnterEffects ?? [], Gain("dmh-key")] };
            if (scene.Id == "switchboard-desk") next = scene with { OnEnterEffects = [.. scene.OnEnterEffects ?? [], Gain("dmh-log")] };
            if (scene.Id == "phone-detail") next = scene with { OnEnterEffects = [.. scene.OnEnterEffects ?? [], Gain("dmh-bridge"), Gain("dmh-log")] };
            if (scene.Id == "room-306") next = scene with
            {
                OnEnterEffects = [.. scene.OnEnterEffects ?? [], Gain("dmh-cufflink"), Gain("dmh-photo")],
                Choices = [.. (scene.Choices ?? []).Select(c => c with
                {
                    Conditions = [Flag("dmh-" + c.Id, false)], Effects = [Mark("dmh-" + c.Id)],
                }), Go("leave-room", "Bag the evidence and decide which thread to follow first.", "first-room-conclusion")],
            };
            if (scene.Id is "cufflink-detail" or "photo-detail" or "phone-detail") next = next with
            {
                Choices = [Go("return-to-room", "Return to the room. There may be more to find.", "room-306")],
            };
            if (scene.Id == "first-room-conclusion") next = scene with
            {
                Choices = [Go("foundation-end", "Take the investigation into the city.", "foundation-checkpoint")],
            };
            if (scene.Id == "foundation-checkpoint") next = Scene("foundation-checkpoint", 2, "The City Collects", "Calder Street · Before dawn",
                "The rain has settled into a cold, steady business. In your coat pocket the cufflink catches on the torn photograph, two pieces of a story someone paid to remove. The Ivory Room is still open. Evelyn has lights in her apartment. At the telephone exchange, the night operator is due to hand over her shift. You cannot make the city tell you everything at once. You can decide which lie to put under a lamp first.",
                [Go("continue-investigation", "Open your notebook and choose the next address.", "city-threads")]);
            yield return next;
        }
    }

    private static IReadOnlyList<AdventureSceneDefinition> BuildLaterActs() =>
    [
        Scene("city-threads", 2, "Three Addresses in the Rain", "Calder Street · Investigation routes",
            "The notebook stays open beneath the awning of an all-night chemist. Hotel facts on the left; people who would rather they stayed facts without explanations on the right. Every address is a different kind of risk. None requires you to believe Evelyn's version of her husband. You can revisit a thread while it still has something to offer; the docks remain open when you are ready to follow the freight route.",
            [Go("visit-evelyn", "Revisit Evelyn and ask what Arthur was buying with his casino debts.", "evelyn-apartment", [Flag("dmh-evelyn", false)]),
             Go("visit-ivory", "Go upstairs at the Monarch Casino.", "ivory-door", [Flag("dmh-ivory-finished", false)]),
             Go("visit-exchange", "Visit the telephone exchange before the operator leaves.", "exchange", [Flag("dmh-exchange", false)]),
             Go("visit-cabinet", "Use the duplicate key on room 306's service cabinet.", "hotel-cabinet", [Own("dmh-key"), Flag("dmh-cabinet", false)], [Mark("dmh-cabinet")]),
             Go("visit-supplies", "Buy practical tools at the all-night chemist.", "night-chemist"),
             Go("follow-freight", "Follow the Mirabelle's overnight service van to the river warehouses.", "dock-approach")]),
        Scene("evelyn-apartment", 2, "The Accounts She Kept", "Vane apartment · Calder Heights",
            "Evelyn has taken off the expensive earrings. They sit beside an unpaid electricity bill, absurdly small next to the house around them. Arthur's redevelopment company has been buying condemned blocks through the casino, she says. She signed an invitation for him because Moretti would no longer take his calls. She knew about the debts. She did not know where the families evicted from those blocks were meant to go. When you do not offer absolution, she gives you the invitation and two ferry berths she had booked before she hired you.",
            [Go("evelyn-trust", "Promise to bring Arthur back to answer for himself, rather than promise innocence.", "city-threads", effects: [Mark("dmh-evelyn-trust")]),
             Go("evelyn-distance", "Tell her the evidence will decide what happens to him.", "city-threads")],
            Mark("dmh-evelyn"), Gain("dmh-pass"), Gain("dmh-ticket"), Known("evelyn-vane"), new(AdventureEffectKind.ConfirmTheory, "theory-evelyn-withholding")),
        Scene("hotel-cabinet", 2, "Behind the Guest Wallpaper", "Mirabelle · Room 306 service cabinet",
            "The duplicate turns in a second, smaller lock behind the telephone plate. Inside the service recess is the maintenance clip's mate and a carbon of the erased call sheet. The hotel did not invent the missing minutes; someone borrowed its machinery. You wrap the metal in your handkerchief before closing the cabinet. This is the kind of evidence a man can lose simply by trusting the wrong desk.",
            [Go("cabinet-return", "Take the clip and call-sheet copy to your next lead.", "city-threads")], Gain("dmh-bridge"), Gain("dmh-log"), Known("switchboard-log")),
        Scene("night-chemist", 2, "Tools, Not Miracles", "All-night chemist · Wren Street",
            "The chemist has a wartime medical bag under the till and a tray of locksmith's tools that he insists belong to his cousin. He asks no questions about the hour. Prices are written on cardboard; you check your pocket before making a promise. There is no obligation to buy anything. A steady hand or a patient conversation can still open doors a tool would have opened more quietly.",
            [Go("buy-picks", "Buy the narrow lockpick roll for six coins.", "night-chemist", [Coins(6), Own("dmh-picks", false), Flag("dmh-bought-picks", false)], [Pay(6), Gain("dmh-picks"), Mark("dmh-bought-picks")]),
             Go("buy-dressing", "Buy a sealed field dressing for four coins.", "night-chemist", [Coins(4), Own("dmh-aid", false), Flag("dmh-bought-aid", false)], [Pay(4), Gain("dmh-aid"), Mark("dmh-bought-aid")]),
             Go("leave-chemist", "Close your wallet and return to the investigation.", "city-threads")]),
        Scene("exchange", 2, "The Operator's Memory", "Central telephone exchange · Night desk",
            "Nora Finch remembers the cut line. She remembers every man who told her a city's telephone wires were none of her business. Her supervisor filed a private dictation cylinder when the police borrowed a circuit. The call sheet tells her where to look; the bridge clip tells her whose maintenance cabinet was used. Without both, you will have to persuade her to risk searching the private shelf on your word.",
            [Go("patch-call", "Lay out the call-sheet copy and bridge clip so Nora can reconstruct the circuit.", "exchange-proof", [Own("dmh-log"), Own("dmh-bridge")]),
             Check("ask-nora", "Ask Nora to search the private shelf and let you hear what the city recorded.", "contacts", AdventureAbility.Charisma, 13, "exchange-proof", "exchange-warning"),
             Go("leave-exchange", "Leave the operator out of it and follow another thread.", "city-threads")]),
        Scene("exchange-proof", 2, "A Voice Above the Badge", "Central exchange · Listening booth",
            "On the cylinder, a man gives an order without raising his voice: move Vane from the hotel, hold him at Municipal Pump House Four, recover the signed book before morning. Nora identifies the private line as Commissioner Rook's office. A voice alone can be disputed; the routing record makes it harder. She gives you both, then takes down the name of an attorney. You thank her by writing it in your notebook instead of promising she will not need one.",
            [Go("exchange-return", "Protect Nora's name and take the recording.", "city-threads")], Mark("dmh-exchange"), Gain("dmh-recording"), Known("rook"), Known("pump-house"), Known("rook-recording"), Link("rook-orders"), new(AdventureEffectKind.ResolveLead, "lead-room-call")),
        Scene("exchange-warning", 2, "The Line Is Listening", "Central exchange · Street exit",
            "Nora reaches for the shelf, then sees the supervisor in the glass. She cannot do it tonight. At the door she presses a maintenance schedule into your hand: the Mirabelle's borrowed circuit runs along the river. It is a lead, not proof. Across the street an unlit police car starts its engine. You let a tram pass between you before walking away.",
            [Go("exchange-warning-return", "Keep moving. The river is the next place to look.", "city-threads")], Mark("dmh-exchange"), Heat(), Known("pump-house")),
        Scene("ivory-door", 2, "The Lift With No Button", "Monarch Casino · Private entrance",
            "The lift attendant wears gloves too clean for the street outside. Upstairs, a pianist keeps finding the same tired chord. The attendant asks for a name, an invitation, or an explanation he can afford to believe. Through the lift grille you can see a police raincoat hanging beside the house coats. The private room belongs to Moretti. Its visitors apparently have other employers.",
            [Go("show-pass", "Present Evelyn's invitation and let the lift take you up.", "ivory-floor", [Own("dmh-pass")]),
             Check("talk-upstairs", "Become the kind of guest the attendant expects to see.", "deception", AdventureAbility.Charisma, 13, "ivory-floor", "ivory-service"),
             Go("ivory-retreat", "Do not force the door. Return to the street.", "city-threads")], Known("ivory-room"), Known("vincent-moretti"), Link("moretti-ivory")),
        Scene("ivory-service", 2, "The House Has a Back Door", "Monarch Casino · Service stairs",
            "The lift stays downstairs. On your way out, a porter mistakes you for the man collecting the private room's laundry. You carry the basket far enough to learn the route, set it down outside the pantry, and arrive upstairs with a pair of house eyes already on you. Access has a price even when nobody asks for money.",
            [Go("service-to-floor", "Walk into the room as though you have already been admitted.", "ivory-floor")], Heat()),
        Scene("ivory-floor", 2, "The Ivory Room", "Monarch Casino · Upper salon",
            "The tables are smaller than the debts made around them. Vincent Moretti sits where he can see both exits. Lena Vale sings without looking at the police raincoat on the rack. A cashier closes a book whenever a guest changes tables. You can speak to more than one of them, but every finished conversation changes the way the others look at you.",
            [Go("ask-moretti", "Ask Moretti about Vane's last debt.", "moretti", [Flag("dmh-moretti", false)]),
             Go("ask-lena", "Wait for Lena's set to end and approach the piano.", "lena", [Flag("dmh-lena", false)]),
             Go("watch-cashier", "Follow the cashier's freight receipts rather than the cards.", "cashier", [Flag("dmh-cashier", false)]),
             Go("leave-ivory", "Leave by the front lift with what you have learned.", "city-threads", effects: [Mark("dmh-ivory-finished")])]),
        Scene("moretti", 2, "A Debt With the Wrong Creditor", "Ivory Room · Moretti's booth",
            "Moretti studies your coat before he studies your face. Arthur was not gambling away the company, he says. He was buying back a ledger he had already signed. Rook wanted the book because it listed the compensation paid for empty streets that had still been full of people. Moretti offers the location of a municipal transfer for the name of your client. You can refuse the trade. The bent cufflink may make a more useful introduction.",
            [Go("show-cufflink", "Show him the eagle stamped under the cufflink's hinge.", "moretti-truth", [Own("dmh-cufflink")]),
             Check("read-moretti", "Make him explain why a police commissioner would need a casino's freight van.", "underworld", AdventureAbility.Wisdom, 13, "moretti-truth", "moretti-price"),
             Go("moretti-refuse", "Refuse to name the client and leave his booth.", "ivory-floor", effects: [Mark("dmh-moretti")])]),
        Scene("moretti-truth", 2, "Municipal Property", "Ivory Room · Moretti's booth",
            "The eagle is a retirement-service clasp issued to senior officers, not a casino ornament. Moretti has seen Rook wear it. His driver also saw Hale escort Vane from the hotel alive. 'An escort is not the same thing as an arrest,' he says. Pump House Four is where the city's confiscated freight waits for morning paperwork. Moretti gives you an address because an honest ledger would ruin Rook before it ruined him. That is a motive to help, and a reason not to trust him with the original.",
            [Go("moretti-back", "Write down the address without giving him a promise.", "ivory-floor")], Mark("dmh-moretti"), Known("rook"), Known("pump-house"), Link("rook-cufflink"), new(AdventureEffectKind.ContradictCaseConnection, "cufflink-vane")),
        Scene("moretti-price", 2, "The Name on the Envelope", "Ivory Room · Moretti's booth",
            "Moretti lets the silence do the bargaining. You decline to buy an address with Evelyn's name. On the way out his man deliberately leaves a freight schedule face up: Municipal Pump House Four, before dawn. The gift is a reminder that he knows which direction you will walk. Somewhere behind you, a telephone starts to ring.",
            [Go("moretti-price-back", "Take the lead, and remember it was planted.", "ivory-floor")], Mark("dmh-moretti"), Heat(), Known("pump-house")),
        Scene("lena", 2, "The Woman Outside the Photograph", "Ivory Room · Piano alcove",
            "Lena Vale folds her sheet music very carefully when you say Arthur's name. She says he was frightened, not drunk, when he left the Mirabelle. She does not say how she knows. One glove hangs out of her evening bag. If she is the missing figure in the photograph, she has already watched someone erase her once.",
            [Go("show-photo", "Show Lena the torn photograph and ask for her version before naming a suspect.", "lena-trust", [Own("dmh-photo")]),
             Check("reassure-lena", "Offer a way out of the room without demanding testimony first.", "persuasion", AdventureAbility.Charisma, 13, "lena-trust", "lena-leaves")]),
        Scene("lena-trust", 2, "An Escort, Not an Arrest", "Monarch Casino · Back balcony",
            "Lena is Hale's sister. Arthur asked her to carry a book to prosecutor Ada Mercer, then panicked when Rook arrived. Hale came to protect Lena, she says; afterward he obeyed Rook's order to move Arthur. Both things can be true. The full photograph belongs to a man on the docks who makes passport pictures. Lena agrees to meet you there. You do not ask her to trust a badge. You give her an address with two exits.",
            [Go("lena-trust-back", "Tell her to use the river entrance and keep her name out of your notes.", "ivory-floor")], Mark("dmh-lena"), Mark("dmh-lena-trust"), Known("lena-vale"), Known("photographer"), Link("hale-lena")),
        Scene("lena-leaves", 2, "The Empty Piano Stool", "Ivory Room · Piano alcove",
            "You ask one question too many before offering her anything solid. Lena excuses herself. A waiter tells you she buys her photographs on the river and means it as a kindness; another man hears him. You have a direction to follow, but not the trust that would have made it safer for her.",
            [Go("lena-leaves-back", "Leave her a quiet route out, even if she will not use it yet.", "ivory-floor")], Mark("dmh-lena"), Heat(), Known("lena-vale"), Known("photographer")),
        Scene("cashier", 2, "The Retained Copy", "Ivory Room · Cashier's rail",
            "The cashier is balancing freight costs against police entertainment expenses. It is a poor place to hide a transfer, unless the people reading it already agree not to see it. A carbon sheet protrudes beneath the blotter. Taking it openly would make a scene. Making the cashier explain the numbers might make a better one.",
            [Check("lift-carbon", "Slip the freight carbon out while he counts the table float.", "sleight-of-hand", AdventureAbility.Dexterity, 13, "carbon-found", "cashier-warning"),
             Check("audit-carbon", "Ask why municipal freight is being billed to private gaming accounts.", "investigation", AdventureAbility.Intelligence, 13, "carbon-found", "cashier-warning")]),
        Scene("carbon-found", 2, "Paper Remembers", "Ivory Room · Cloakroom",
            "The carbon names Pump House Four, a police escort, and a crate registered as reclaimed furniture. Under the typed inventory is a handwritten correction: A. Vane, alive. It is not enough to convict anyone. It is enough to contradict the story that Arthur simply walked away. You fold it along the old crease, not through the signature.",
            [Go("carbon-back", "Keep the carbon dry and return to the room.", "ivory-floor")], Mark("dmh-cashier"), Gain("dmh-carbon"), Known("shipping-carbon"), Known("pump-house"), Link("freight-vane")),
        Scene("cashier-warning", 2, "House Security", "Ivory Room · Cashier's rail",
            "The cashier puts a hand on the carbon before yours reaches it. He says Pump House Four clearly enough for the room to hear, then asks you to leave his desk. You keep the destination. He keeps the proof. A man near the lift writes down the color of your coat.",
            [Go("cashier-warning-back", "Return to the room before it becomes an escort.", "ivory-floor")], Mark("dmh-cashier"), Heat(), Known("pump-house")),

        Scene("dock-approach", 3, "Favors and Knives", "River district · Loading gates",
            "The Mirabelle van stops beside a warehouse with municipal locks and private guards. Further downriver a pump beats against its own broken rhythm. The photographer's lamp is the only warm window between them. There is still time to learn who moved Arthur, find what they were carrying, or reach the man himself. Nobody here is going to ask whether you have a warrant until after they have seen whether you are alone.",
            [Go("dock-office", "Search the warehouse dispatch office.", "warehouse-door", [Flag("dmh-warehouse", false)]),
             Go("dock-photo", "Visit the passport photographer and ask for the uncut image.", "photographer", [Flag("dmh-photographer", false)]),
             Go("dock-rescue", "Find the pump house and reach Vane before the morning transfer.", "pump-approach", [Flag("dmh-rescue-attempted", false)]),
             Go("dock-badge", "Take what you have to Hale's precinct and make the city answer on paper.", "precinct")], Known("river-docks")),
        Scene("warehouse-door", 3, "A Municipal Lock", "River warehouse · Dispatch office",
            "Behind the wired glass a dispatch lamp burns over a stack of retained carbons. There is nobody at the desk. A police seal has been put over the door without covering the latch, a useful distinction for a locksmith and a prosecutor. You can spend the tools, risk the noise, or preserve the seal and walk away.",
            [Go("pick-warehouse", "Use the lockpick roll; sacrifice a fine pick to lift the sealed latch.", "warehouse-carbon", [Own("dmh-picks")], [Lose("dmh-picks")]),
             Check("force-warehouse", "Open the latch without disturbing the paper seal.", "lockpicking", AdventureAbility.Dexterity, 14, "warehouse-carbon", "warehouse-seen"),
             Go("leave-warehouse", "Leave the seal intact and try another route.", "dock-approach")]),
        Scene("warehouse-carbon", 3, "Furniture With a Pulse", "Dispatch office · Filing shelf",
            "The warehouse copy says what the casino copy only hints at. A man listed as furniture was held for a transfer signed E. Rook. The dispatcher's notebook puts Hale in the escort car, with Lena Vale ordered to follow. Neither was booked into a cell. Beneath the schedule is a requisition for field dressings. You take a sealed packet from the shelf and leave the rest where a night worker can find them.",
            [Go("warehouse-back", "Take the retained carbon and return to the river.", "dock-approach")], Mark("dmh-warehouse"), Gain("dmh-carbon"), Gain("dmh-aid"), Known("shipping-carbon"), Known("pump-house"), Known("rook"), Link("freight-vane")),
        Scene("warehouse-seen", 3, "A Light Across the Yard", "Warehouse · Loading yard",
            "The latch gives a short metallic cry. A guard's lamp turns toward the dispatch window. You back out before the seal tears and lose him between two freight cars. The route to the pump house is painted on a municipal sign; that much nobody could hide. The dispatch evidence will have to stay where it is.",
            [Go("warehouse-seen-back", "Keep the destination and leave the guarded office behind.", "dock-approach")], Mark("dmh-warehouse"), Heat(), Known("pump-house")),
        Scene("photographer", 3, "The Half They Cut Away", "River district · Bell's photographic studio",
            "Otto Bell still has the negative. He keeps other people's secrets in numbered envelopes and charges for paper, he tells you, not silence. On his contact sheet Hale stands beside Vane and Lena, holding a coat over the book under Arthur's arm. The picture places him there. It does not tell you whose orders he obeyed afterward.",
            [Go("buy-negative", "Pay three coins for the original negative and a dated print.", "negative-found", [Coins(3)], [Pay(3)]),
             Check("credit-negative", "Ask Bell to put the cost on your account and the date on the print.", "contacts", AdventureAbility.Charisma, 12, "negative-found", "photo-refused"),
             Go("photo-leave", "Do not take the negative. Return to the docks.", "dock-approach")], Known("photographer")),
        Scene("negative-found", 3, "A Witness Made of Silver", "Bell's studio · Red-lit workroom",
            "Bell marks the print from the dated plate register, then slides the uncut negative into a dry sleeve. The register dates the photograph to the winter visits Evelyn had omitted. It places Lena, Hale and Vane outside the Monarch together; it does not put them in room 306. Bell offers to testify to the photograph, not to what you wish it meant. That is better than a cooperative liar.",
            [Go("negative-back", "Keep the negative separate from the torn print.", "dock-approach")], Mark("dmh-photographer"), Gain("dmh-negative"), Known("uncut-negative"), Known("elias-hale"), Known("lena-vale"), Link("hale-lena")),
        Scene("photo-refused", 3, "No More Credit", "Bell's studio · Street door",
            "Bell has extended enough credit to men who said a story would pay them. He locks the envelope away. Before the bolt closes he tells you Lena came through the studio and went toward the pump house. She was walking quickly, but not with the man behind her. You leave the negative and follow the living lead.",
            [Go("photo-refused-back", "Return to the river before the trail grows cold.", "dock-approach")], Mark("dmh-photographer"), Known("pump-house")),
        Scene("pump-approach", 3, "Water Under the City", "Municipal Pump House Four",
            "A service door stands open over black water. Arthur Vane is inside, one hand pressed hard beneath his coat. Lena is keeping a freight trolley between him and the armed men beyond the pump. The night watchman waves you away, then sees the street behind you and decides he would rather be somewhere else. Getting them out will require a route the guards cannot control.",
            [Check("pump-quiet", "Bring them out along the maintenance ledge while the pump starts.", "observation", AdventureAbility.Wisdom, 13, "wounded-vane", "pump-pursuit"),
             Go("pump-diversion", "Break the yard's alarm glass and lead the guards away from the service door.", "pump-pursuit")], Known("pump-house"), Known("lena-vale"), Known("arthur-vane")),
        Scene("pump-pursuit", 3, "The Price of an Exit", "Pump house · Flood channel",
            "The guard fires into the gate, not into the dark beyond it. You keep the trolley low while Lena pulls. Brick dust stings your face and somebody loses a shoe in the water. Arthur is breathing when you reach the far culvert, but his hand has slipped from the wound. Escape has bought a few minutes. It has not bought him a doctor.",
            [Go("pursuit-treat", "Get him out of the water and see what can still be done.", "wounded-vane")], Heat(), new(AdventureEffectKind.ChangeHitPoints, Value: -2)),
        Scene("wounded-vane", 3, "A Man, Not an Exhibit", "Flood channel · Dry maintenance landing",
            "Arthur tries to tell you where the ledger is. Lena tells him to save his breath, and you make room for both instructions. His wound needs pressure and clean cloth. In the distance the pump starts again, making the landing tremble. Whatever he signed, he is still a man on a cold floor. You choose what you can do for that man before deciding what his testimony is worth.",
            [Go("dress-vane", "Use the sealed field dressing and keep steady pressure until help can be arranged.", "safehouse", [Own("dmh-aid")], [Lose("dmh-aid"), Mark("dmh-vane-alive")]),
             Check("treat-vane", "Improvise a pressure dressing and stabilize him for the journey.", "medicine", AdventureAbility.Intelligence, 13, "safehouse", "last-witness", success: [Mark("dmh-vane-alive")]),
             Go("summon-ambulance", "Send an anonymous ambulance call and take Lena out before the guards return.", "last-witness")], Mark("dmh-rescue-attempted"), Mark("dmh-witness-safe"), new(AdventureEffectKind.ResolveLead, "lead-where-vane")),
        Scene("safehouse", 3, "What He Signed", "Bell's studio · Upstairs room",
            "The doctor arrives through the rear door. Arthur survives long enough to admit what the ledger contains: compensation stolen from the families his company evicted, police payments made to keep the blocks quiet, and his own signature authorizing both. He tried to sell the book back before he tried to expose it. Lena will sign what she saw, provided her address stays out of the public file. Bringing him back has not made him innocent. It has given him the chance to stop making other people carry his guilt.",
            [Go("safehouse-statement", "Take Lena's statement, arrange protection and return to the remaining dock leads.", "dock-approach")], Gain("dmh-affidavit"), Known("witness-statement"), Mark("dmh-statement"), Link("lena-testimony")),
        Scene("last-witness", 3, "The Witness Who Walks", "River district · Ambulance call box",
            "You leave the precise address with the dispatcher and hear an ambulance engine before you turn the corner. You cannot know whether it will reach Arthur before Rook's men do. Lena can walk, and tonight that is the difference you can still make. In Bell's upstairs room she dictates what she saw and signs beneath it. The empty space where Arthur's signature might have gone stays empty.",
            [Go("last-witness-back", "Keep Lena's affidavit and continue the investigation.", "dock-approach")], Gain("dmh-affidavit"), Known("witness-statement"), Mark("dmh-statement"), Link("lena-testimony")),

        Scene("precinct", 4, "The Badge Has Teeth", "Central precinct · Public counter",
            "At six in the morning the precinct smells of wet wool and yesterday's coffee. Hale is at a desk he has not slept behind. The archive door carries a seal from Rook's office. You can make Hale look at a photograph, try the records route yourself, or demand an interview with the commissioner. Leaving the docks did not close the case. It brought the case into the building that has been editing it.",
            [Go("hale-private", "Ask Hale to speak where the counter clerk cannot hear.", "hale-office", [Flag("dmh-hale", false)]),
             Go("archive-route", "Seek the impound register and Vane's signed book.", "archive-door", [Flag("dmh-archive", false)]),
             Go("rook-route", "Request an interview with Commissioner Rook.", "rook-office", [Flag("dmh-rook", false)]),
             Go("back-to-docks", "Return to the river for the witness or the remaining evidence.", "dock-approach", [Flag("dmh-prepared", false)]),
             Go("precinct-to-editor", "Take the case to the morning paper's night editor.", "editor")], Known("precinct"), Known("elias-hale")),
        Scene("hale-office", 4, "A Brother in Uniform", "Precinct · Hale's office",
            "Hale leaves the door open. He says it is for your protection, then looks at the corridor as though protection has become an awkward word. His coat is still damp at the cuffs. You have enough to accuse him of moving Arthur, but accusation and explanation are different tools. If the photograph means what Lena said it means, this is the point at which you find out whether he is prepared to put his name under it.",
            [Go("show-negative", "Put the uncut negative on his desk and ask whom he was protecting.", "hale-confession", [Own("dmh-negative")]),
             Go("show-affidavit", "Let Hale read Lena's signed account in her own words.", "hale-confession", [Own("dmh-affidavit")]),
             Check("press-hale", "Make him account for the missing seventeen minutes.", "interrogation", AdventureAbility.Charisma, 14, "hale-confession", "hale-silence")]),
        Scene("hale-confession", 4, "An Order Is Still a Choice", "Precinct · Hale's office",
            "Hale admits he moved Vane on Rook's order. He thought the alternative was Lena being booked as an accomplice, and he chose the explanation he could live with for one more hour. The original ledger is in the municipal impound archive; Rook meant to replace its signed pages. Hale will countersign Lena's account and send a clerk to preserve the register. He asks whether that makes him safe. You tell him it makes him a witness. He does not ask again.",
            [Go("hale-confession-back", "Accept the signed account without promising immunity.", "precinct")], Mark("dmh-hale"), Mark("dmh-hale-ally"), Known("witness-statement"), Known("rook"), Link("rook-hale"), new(AdventureEffectKind.ConfirmTheory, "theory-hale-knows-more")),
        Scene("hale-silence", 4, "The Door He Leaves Open", "Precinct · Corridor",
            "Hale answers in procedure until the words have no useful edges left. As you stand, he leaves an archive requisition where you can see it. Vane's book has been impounded, not destroyed. If you want his signature beside an allegation, you will need something stronger than the right suspicion.",
            [Go("hale-silence-back", "Take the archive reference and leave the accusation unfiled.", "precinct")], Mark("dmh-hale"), Known("impound-register")),
        Scene("archive-door", 4, "The Record That Survived", "Precinct · Municipal impound archive",
            "The clerk has a cancellation order but no replacement entry. That distinction can be made to matter. Hale's countersignature would stop the cancellation. The exchange recording or the freight carbon would force the clerk to preserve a disputed exhibit. Without a document, you can try the service door or your knowledge of the procedure the commissioner has skipped.",
            [Go("archive-with-witness", "File Hale's signed preservation request.", "signed-book", [Flag("dmh-hale-ally")]),
             Go("archive-with-carbon", "Use the freight carbon to dispute the cancellation in writing.", "signed-book", [Own("dmh-carbon")]),
             Go("archive-with-recording", "Register the exchange recording as evidence before the exhibit is released.", "signed-book", [Own("dmh-recording")]),
             Go("archive-with-picks", "Use the lockpick roll on the service door, accepting that the tools will stay in the damaged lock.", "signed-book", [Own("dmh-picks")], [Lose("dmh-picks"), Heat()]),
             Check("archive-procedure", "Challenge the unsigned cancellation under the impound rules.", "police-procedure", AdventureAbility.Intelligence, 14, "signed-book", "archive-refusal"),
             Go("archive-back", "Keep the archive reference and find stronger leverage.", "precinct")]),
        Scene("signed-book", 4, "Every Empty House", "Municipal archive · Exhibit shelf",
            "The ledger weighs less than the people in it. Row after row lists compensation that never reached the tenants, emergency removals paid to officers who were never assigned, and invoices passed through Moretti's accounts. Arthur signed the transfers. Rook signed the protection orders. You copy the exhibit number before taking custody. It is the first time tonight the city's own paper has admitted the shape of the crime.",
            [Go("signed-book-back", "Keep the original and return to the precinct.", "precinct")], Mark("dmh-archive"), Gain("dmh-ledger"), Known("signed-ledger"), Known("impound-register"), Known("rook"), Link("rook-ledger")),
        Scene("archive-refusal", 4, "The Seal Wins For Now", "Precinct · Archive counter",
            "The clerk asks for the section you cannot quite remember. By the time you have reconstructed it, Rook's office has called. The exhibit remains in storage, but you will not get another hearing on the same argument this morning. You write down the refusal, the time, and the clerk's name. They belong in the case too, even if they are not the book.",
            [Go("archive-refusal-back", "Return to the precinct and decide how to use what remains.", "precinct")], Mark("dmh-archive"), Heat(), Known("impound-register")),
        Scene("rook-office", 4, "The Man Who Calls It Order", "Commissioner's office · Closed blinds",
            "Commissioner Rook does not deny that people were paid. He calls them relocation costs. He calls Arthur a distressed debtor and Hale a man who needs supervision. He calls you a person with a future, which is the first threat he has delivered plainly. A typed conclusion waits on his desk: Hale acted alone. Beside it is an envelope thick enough to make the conclusion worth signing.",
            [Go("rook-refuse", "Refuse the prepared conclusion and preserve his offer in your notes.", "precinct", effects: [Mark("dmh-rook"), Mark("dmh-refused-rook")]),
             Go("rook-play", "Let him believe you will consider the envelope. Hear the arrangement out.", "rook-terms", effects: [Mark("dmh-rook")])], Known("rook")),
        Scene("rook-terms", 4, "The Offered Version", "Commissioner's office · Guest chair",
            "Rook wants Hale named as the man who took Vane, the ledger handed over if you have it, and Lena's whereabouts left off the public record. In return you get a fee and the assurance that Evelyn will be allowed to leave. He has confused a witness's safety with the price of her silence. You can still leave with the offer unwritten. Keeping it as an option changes what kind of ending you are prepared to choose.",
            [Go("rook-consider", "Keep the offer open without surrendering any evidence yet.", "precinct", effects: [Mark("dmh-rook-offer")]),
             Go("rook-walk", "Close the envelope and leave it on the desk.", "precinct", effects: [Mark("dmh-refused-rook")])]),

        Scene("editor", 5, "Everyone Owes Someone", "The Calder Gazette · Night desk",
            "Ruth Bell makes you put the papers on a table before she listens to the story. A ledger can be stolen. A voice can be imitated. A witness can be frightened into silence. Put them beside an independent freight record or a dated photograph, and the city has to work much harder to pretend nothing happened. She will set type for an accountable story, not a convenient villain. You still have time to protect the witness, preserve a copy, or decide to sell the truth to somebody who already knows it.",
            [Go("copy-dossier", "Use the signed ledger to make a protected duplicate for Ruth's lawyer.", "protected-copy", [Own("dmh-ledger"), Flag("dmh-copy", false)]),
             Go("guard-witness", "Check the safe address and arrange a protected transfer for the witness.", "witness-transfer", [Own("dmh-affidavit"), Flag("dmh-witness-protected", false)]),
             Go("one-last-lead", "Go back to the precinct or river before making the final move.", "precinct", [Flag("dmh-prepared", false)]),
             Go("meet-broker", "Listen to Moretti's offer for the original ledger.", "moretti-offer", [Own("dmh-ledger"), Flag("dmh-moretti-offer", false)]),
             Go("finish-investigation", "Close the notebook. Choose what the city will learn at dawn.", "dawn-decision", effects: [Mark("dmh-prepared")])]),
        Scene("protected-copy", 5, "Two Envelopes", "Gazette · Composing room",
            "Ruth makes one photographic copy for the paper and another for the attorney who will receive the depositions. You number the pages before sealing either envelope. A duplicate does not make witnesses immortal or turn an accusation into a conviction. It does mean that taking the original off you will no longer take the whole case. In the window the first pale seam of morning appears behind the rain.",
            [Go("copy-back", "Leave the duplicates with independent custodians.", "editor")], Mark("dmh-copy"), Known("gazette")),
        Scene("witness-transfer", 5, "An Address Worth Keeping", "Bell's studio · Rear stairs",
            "A car is waiting across from the studio. You do not use the front door. Ruth's attorney has arranged a room above a clinic where the register will use the court's reference rather than Lena's name. If Hale has agreed to testify, he arrives on foot with no badge visible. The safest promise you can give either of them is a specific route, a specific person at the far end, and no public address in tomorrow's paper.",
            [Check("quiet-transfer", "Move the witness through the service alley before the watcher sees you.", "streetwise", AdventureAbility.Wisdom, 13, "witness-secured", "witness-decoy") with
                { Conditions = [new(AdventureConditionKind.CounterMinimum, "dmh-heat", 3, Expected: false)] },
             Check("watched-transfer", "You have drawn several watchers. Lose the tighter tail before moving the witness.", "streetwise", AdventureAbility.Wisdom, 16, "witness-secured", "witness-decoy") with
                { Conditions = [new(AdventureConditionKind.CounterMinimum, "dmh-heat", 3)] },
             Go("transfer-by-cab", "Spend five coins on two cabs and separate routes.", "witness-secured", [Coins(5)], [Pay(5)])]),
        Scene("witness-decoy", 5, "The Coat They Follow", "Calder Street · Tram shelter",
            "The watcher sees your first turn. You lend your coat to a willing press runner and make sure he takes the tram with a conductor on board. The watcher follows the color, not the face. By the time he realizes what has happened, Lena is upstairs at the clinic. Your own walk back takes longer and costs you a bruised shoulder. It is still the better bargain.",
            [Go("decoy-back", "Confirm the clinic has received the witness before returning.", "witness-secured")], Heat(), new(AdventureEffectKind.ChangeHitPoints, Value: -1)),
        Scene("witness-secured", 5, "A Name Under Protection", "Gazette · Attorney's telephone",
            "The attorney confirms the arrival using the phrase you agreed, then reads back the statement's exhibit number. It is not an ending yet. It is a living person the morning cannot quietly misplace. Ruth leaves the witness's address out of the type and keeps the accusation in.",
            [Go("secured-back", "Return to the table and decide what to do with the evidence.", "editor")], Mark("dmh-witness-protected"), Mark("dmh-witness-safe")),
        Scene("moretti-offer", 5, "A Quiet Balance", "Gazette alley · Moretti's car",
            "Moretti sends a driver, then comes himself when you will not approach the car. He can get Vane out of the district and stop the men outside Bell's studio. He wants the original ledger, not a promise of friendship. You can make the trade, refuse it, or tell him that independent copies already exist if you have actually made them. Each answer leaves a different person holding the leverage.",
            [Go("trade-ledger", "Hand over the original in exchange for safe passage and protection.", "editor", effects: [Lose("dmh-ledger"), Mark("dmh-moretti-deal"), Mark("dmh-witness-protected"), Mark("dmh-moretti-offer")]),
             Go("keep-ledger", "Keep the book and refuse his protection.", "editor", effects: [Mark("dmh-moretti-offer")])]),

        Scene("dawn-decision", 6, "Dead Man's Hand", "Calder Gazette · The first edition",
            "The city at dawn looks less innocent than it does at night. You lay out only what you can substantiate. Rook's orders, Arthur's signatures, Hale's choices, Lena's account: none cancels out the others. Ruth is ready to stop the press if you are still making a case. Moretti's car is waiting at the curb. Beyond it, the ferry horn sounds once. You choose where the truth goes, who carries it, and what you are prepared to leave behind.",
            [Go("publish-full", "Publish the corroborated ledger, the signed testimony and the names at the top.", "press-proof", [Flag("dmh-copy"), Own("dmh-affidavit"), Flag("dmh-witness-protected"), Own("dmh-carbon")]),
             Go("publish-recording", "Publish the protected dossier with the exchange recording as independent corroboration.", "press-proof", [Flag("dmh-copy"), Own("dmh-affidavit"), Flag("dmh-witness-protected"), Own("dmh-recording")]),
             Go("file-court", "Give the original ledger and protected witness to prosecutor Ada Mercer.", "court-proof", [Own("dmh-ledger"), Own("dmh-affidavit"), Flag("dmh-witness-protected")]),
             Go("print-ledger", "Print the authenticated ledger without asking the witness to appear publicly.", "press-ledger", [Flag("dmh-copy")]),
             Go("take-bargain", "Accept Moretti's quiet settlement and the protection you purchased.", "bargain-proof", [Flag("dmh-moretti-deal")]),
             Go("take-ferry", "Use the two ferry berths to get Arthur out of the city with Evelyn.", "ferry-proof", [Own("dmh-ticket"), Flag("dmh-vane-alive")], [Lose("dmh-ticket")]),
             Go("blackmail-rook", "Use the original as leverage for a place in Rook's city.", "blackmail-proof", [Own("dmh-ledger"), Flag("dmh-rook-offer")]),
             Go("burn-book", "Destroy the original ledger and close the case on Evelyn's terms.", "ashes-proof", [Own("dmh-ledger"), Flag("dmh-copy", false)], [Lose("dmh-ledger")]),
             Go("name-hale", "File the narrow accusation against Hale and let the institution stand.", "fallguy-proof", [Flag("dmh-hale")]),
             Go("keep-witness", "Put the surviving witness's safety ahead of a case you cannot yet prove.", "witness-proof", [Flag("dmh-witness-safe")]),
             Go("leave-open", "Refuse a convenient accusation. Leave the file open until the missing proof can be found.", "cold-proof")]),
        Scene("press-proof", 6, "Before the Ink Dries", "Gazette · Press floor",
            "Ruth reads the names back to you. Arthur is in the story as a participant who later tried to withdraw, not as a spotless victim. Hale's testimony appears beside his admission. Lena's address does not appear at all. When Rook calls, the first bundles have already gone to three stations. You cannot promise the editor there will be convictions. You can promise the city will no longer be able to lose the question.",
            [Go("press-end", "Let the first edition go.", "ending-open-record")]),
        Scene("court-proof", 6, "Exhibit One", "County courthouse · Mercer's chamber",
            "Mercer takes custody under a written receipt and asks you to distinguish what you saw from what you inferred. That slows the telling and improves it. She can seek a sealed order for Lena, subpoena the archive, and ask Hale the question Rook would rather answer for him. Outside, the newspaper sellers are still waiting for a headline. You choose the slower machinery and make it name every piece it takes from you.",
            [Go("court-end", "Sign the chain-of-custody record and stay for the first hearing.", "ending-court")]),
        Scene("press-ledger", 6, "Names Without Addresses", "Gazette · Editor's table",
            "The ledger can sustain an investigation into payments. It cannot, alone, prove every order given in the missing seventeen minutes. Ruth prints exactly that distinction. You keep the frightened people out of the story and leave the missing-person file unresolved where the evidence does not answer it. The headline is smaller than the one you imagined. It is also one you can defend.",
            [Go("ledger-end", "Publish what the documents establish, and no more.", "ending-paper-trail")]),
        Scene("bargain-proof", 6, "The Car at the Curb", "Gazette alley · Moretti's car",
            "Moretti's men stop watching the studio. A private ambulance receives an unobstructed route out of the river district. The ledger has changed hands; the facts have not. If copies remain elsewhere, Moretti knows his new insurance is not exclusive. He calls the arrangement peace. You write settlement in the notebook and leave enough space beneath it for the interest.",
            [Go("bargain-end", "Accept the protection and the debt that comes with it.", "ending-house-credit")]),
        Scene("ferry-proof", 6, "Two Berths", "Pier Seven · Departure gangway",
            "Arthur boards with a physician's note under his coat and Evelyn's hand locked around his wrist. The ticket gets them through a gate no argument would have opened. He asks what you will tell the police. You tell him leaving a city does not unsign a ledger. The ferry pulls away before he can decide whether that is a warning or the first honest thing anyone has said to him tonight.",
            [Go("ferry-end", "Watch the ferry leave and keep the evidence in your own case file.", "ending-two-berths")]),
        Scene("blackmail-proof", 6, "The Vacant Chair", "Commissioner's office · Morning light",
            "You do not hand Rook the ledger. You let him see the page with his signature and tell him where your next copy will go if the arrangement changes. He moves the envelope toward you. For the first time he uses your name as though it belongs on a door. Outside, a clerk is being told that an independent investigator has endorsed the commissioner's account. The city has found another person who can explain its costs.",
            [Go("blackmail-end", "Take the envelope and sit in the offered chair.", "ending-new-creditor")]),
        Scene("ashes-proof", 6, "An Original Burns Once", "Vane apartment · Cold fireplace",
            "Evelyn does not look away while the signed pages curl. You made no protected duplicate. The signatures are gone in a way testimony may never repair. Perhaps you call it mercy for a frightened household; perhaps you admit you have decided which households are allowed to keep their names off the record. In the hearth one brass staple survives longer than the paper it held.",
            [Go("ashes-end", "Close the grate and tell Evelyn the file is closed.", "ending-ashes")]),
        Scene("fallguy-proof", 6, "The Convenient Arrest", "Central precinct · Complaint desk",
            "Hale did move Vane. A narrow complaint can say so truthfully while leaving out the man who gave the order and the money that paid for the room. Rook accepts the accusation with the gravity of someone attending a funeral he arranged. You watch Hale read it. He asks whether Lena will be safe. The desk sergeant says that is a separate matter.",
            [Go("fallguy-end", "Leave the complaint on the desk with its limits uncorrected.", "ending-fallguy")]),
        Scene("witness-proof", 6, "A Living Name", "Clinic · Side entrance",
            "The file is not strong enough to put the whole city on trial. The person behind the clinic door is still alive because you went to the river instead of waiting for permission. You leave the affidavit with the attorney and keep a copy of the witness's preferred contact arrangement. You have not solved every part of the case. You refuse to let that make the part you did save disappear.",
            [Go("witness-end", "Protect the witness and keep working the unanswered questions.", "ending-living-witness")]),
        Scene("cold-proof", 6, "The Empty Line", "Private office · Morning rain",
            "Back at the office, you write the facts in ink and the suspicions in pencil. There is not enough to name the whole crime, and naming a smaller one for convenience would help whoever designed it. Evelyn gets an honest account of what you know. The city gets no reassuring conclusion from you. You put the unanswered question at the front of the file where tomorrow's hand will find it first.",
            [Go("cold-end", "Keep the case open.", "ending-open-file")]),
        Ending("ending-open-record", "open-record", "The Open Record", "Three editions, a sealed deposition and a preserved archive make the Vane affair public. Rook faces a corruption inquiry; Arthur faces his own signed accounts. Lena remains under protection. Nobody calls the city clean. It has become harder for it to pretend the dirt belongs to only one man."),
        Ending("ending-court", "exhibit-one", "Exhibit One", "The prosecutor opens proceedings under an exhibit number that cannot be erased from a hotel call sheet. A protected witness and the original ledger survive to be challenged in court. The verdict is not yours to write. Getting the right people into a room where questions must be answered is."),
        Ending("ending-paper-trail", "paper-trail", "The Paper Trail", "The compensation scandal reaches the front page with its limits stated plainly. The payments are established; the disappearance remains contested. Ruth keeps the duplicate. You keep the unresolved timeline. A smaller honest case leaves room for the next piece of truth."),
        Ending("ending-house-credit", "house-credit", "House Credit", "The immediate danger recedes under Moretti's protection. He now holds the original record of the city's debts, including one you have incurred yourself. The casino is quieter by morning. The ledger is still a weapon, and it is no longer in your hands."),
        Ending("ending-two-berths", "two-berths", "Two Berths Out", "Arthur and Evelyn leave on the morning ferry. Survival has been purchased; innocence has not. The pages and statements you retained remain in your file, beyond the reach of the people on the boat. Somewhere downstream, Arthur will have to decide whether being alive is another chance to answer or another chance to run."),
        Ending("ending-new-creditor", "new-creditor", "The New Creditor", "You become the person Rook must consult before he spends the city's silence. The families in the ledger receive neither their money nor their names back. The case has made you powerful in precisely the way it first taught you to distrust."),
        Ending("ending-ashes", "ashes", "The Cost of Mercy", "The original signatures are ash and there is no independent duplicate. Evelyn has the private closure she wanted. The tenants whose compensation paid for that closure have not been asked. You will remember the sound of the grate whenever another client tells you a matter should remain in the family."),
        Ending("ending-fallguy", "fallguy", "The Smaller Crime", "Hale is suspended while Rook announces an independent investigation. The accusation contains a fact and omits its cause. There is enough truth in the file to make the lie respectable, which is why the commissioner is so pleased to have received it."),
        Ending("ending-living-witness", "living-witness", "A Living Witness", "The city's full account remains unfinished. One person who was meant to disappear has a lawyer, an address kept off the public record and another morning. You leave the unanswered case on the desk without calling that survival a consolation prize."),
        Ending("ending-open-file", "open-file", "The File Stays Open", "No convenient suspect is offered up to cover the gaps. The missing proof remains missing, and your notes make that plain. A detective's obligation is sometimes to leave an honest question where a powerful man wanted a comfortable answer."),
    ];

    private static AdventureSceneDefinition Scene(string id, int act, string title, string location, string body,
        AdventureChoiceDefinition[] choices, params AdventureEffect[] effects) =>
        new(id, title, body, Choices: choices, OnEnterEffects: effects, ActId: $"act-{act}", Location: location);
    private static AdventureSceneDefinition Ending(string id, string ending, string title, string body) =>
        new(id, title, body, IsEnding: true, EndingId: ending, ActId: "act-6", Location: "Calder City · After dawn");
    private static AdventureChoiceDefinition Go(string id, string text, string next,
        AdventureCondition[]? conditions = null, AdventureEffect[]? effects = null) => new(id, text, next, Conditions: conditions, Effects: effects);
    private static AdventureChoiceDefinition Check(string id, string text, string skill, AdventureAbility ability, int dc,
        string successScene, string failureScene, AdventureEffect[]? success = null) =>
        new(id, text, Check: new(id, ability, dc, skill, SuccessSceneId: successScene, FailureSceneId: failureScene), SuccessEffects: success);
    private static AdventureCondition Own(string item, bool expected = true) => new(AdventureConditionKind.ItemOwned, item, Expected: expected);
    private static AdventureCondition Flag(string flag, bool expected = true) => new(AdventureConditionKind.Flag, flag, Expected: expected);
    private static AdventureCondition Coins(int amount) => new(AdventureConditionKind.CoinsMinimum, Value: amount);
    private static AdventureEffect Gain(string item) => new(AdventureEffectKind.AddItem, item);
    private static AdventureEffect Lose(string item) => new(AdventureEffectKind.RemoveItem, item);
    private static AdventureEffect Mark(string flag) => new(AdventureEffectKind.SetFlag, flag);
    private static AdventureEffect Heat() => new(AdventureEffectKind.ChangeCounter, "dmh-heat", 1);
    private static AdventureEffect Pay(int amount) => new(AdventureEffectKind.ChangeCoins, Value: -amount);
    private static AdventureEffect Known(string node) => new(AdventureEffectKind.DiscoverCaseNode, node);
    private static AdventureEffect Link(string link) => new(AdventureEffectKind.ConfirmCaseConnection, link);

    private static CaseBoardDefinition ExpandCaseBoard(CaseBoardDefinition original)
    {
        CaseNodeDefinition[] extra =
        [
            new("rook", CaseNodeCategory.Person, "Commissioner Edmund Rook", "The protection orders and the proposed official conclusion lead to his office.", "Police commissioner"),
            new("lena-vale", CaseNodeCategory.Person, "Lena Vale", "Ivory Room singer and Hale's sister. Her account can be preserved without publishing her safe address.", "Witness"),
            new("photographer", CaseNodeCategory.Location, "Bell's Studio", "Otto Bell holds the uncut, dated negative and offers a room with a rear exit.", "River district"),
            new("river-docks", CaseNodeCategory.Location, "River Warehouses", "Municipal freight moves behind private guards. Retained carbons survive here.", "Freight route"),
            new("pump-house", CaseNodeCategory.Location, "Pump House Four", "The site of Vane's off-book detention and the planned morning transfer.", "Missing-person lead"),
            new("precinct", CaseNodeCategory.Location, "Central Precinct", "Hale's office and the municipal impound archive. Procedure can preserve evidence or conceal it.", "Police archive"),
            new("gazette", CaseNodeCategory.Location, "The Calder Gazette", "Ruth Bell can preserve independent copies and publish a corroborated account.", "Independent custodian"),
            new("shipping-carbon", CaseNodeCategory.Evidence, "Retained Shipping Carbon", "An independent freight record lists Vane as a live transfer under municipal protection.", "Document"),
            new("uncut-negative", CaseNodeCategory.Evidence, "Uncut Dated Negative", "Hale, Vane and Lena appear together. Presence does not, by itself, establish who gave the order.", "Photographic evidence"),
            new("signed-ledger", CaseNodeCategory.Evidence, "Signed Redevelopment Ledger", "Arthur's transfers and Rook's protection orders connect the stolen compensation to official action.", "Original document"),
            new("rook-recording", CaseNodeCategory.Evidence, "The Exchange Recording", "The preserved voice order and its routing record name Pump House Four.", "Recorded order"),
            new("witness-statement", CaseNodeCategory.Evidence, "Signed Witness Account", "A signed account must be distinguished from inference and supported by protection for its living source.", "Testimony"),
            new("impound-register", CaseNodeCategory.Evidence, "Municipal Impound Register", "The ledger has an exhibit number and a cancellation order. Chain of custody matters.", "Official record"),
        ];
        string[] openingCluster = ["arthur-vane", "evelyn-vane", "lead-where-vane", "hotel-mirabelle", "key-306", "switchboard-log"];
        CaseNodeDefinition[] nodes = original.Nodes.Concat(extra)
            .OrderBy(node => Array.IndexOf(openingCluster, node.Id) is int index && index >= 0 ? index : openingCluster.Length)
            .Select((node, i) => node with
        {
            X = 115 + (i % 6) * 350, Y = 115 + (i / 6) * 220,
        }).ToArray();
        return original with
        {
            Nodes = nodes,
            Connections = [.. original.Connections,
                new("rook-orders", "rook", "pump-house", CaseConnectionType.Communication, Label: "recorded transfer order"),
                new("rook-cufflink", "rook", "cufflink", CaseConnectionType.Ownership, Label: "officer's eagle clasp"),
                new("hale-lena", "elias-hale", "lena-vale", CaseConnectionType.Relationship, Label: "siblings"),
                new("freight-vane", "shipping-carbon", "arthur-vane", CaseConnectionType.Timeline, Label: "off-book live transfer"),
                new("rook-hale", "rook", "elias-hale", CaseConnectionType.Relationship, Label: "order and admitted compliance"),
                new("rook-ledger", "rook", "signed-ledger", CaseConnectionType.Financial, Label: "signed protection payments"),
                new("lena-testimony", "lena-vale", "witness-statement", CaseConnectionType.Witnessed, Label: "signed account")],
        };
    }
}
