namespace BlackjackCasino.Core.Adventure;

/// <summary>
/// Second full Tales from the Backroom campaign. Unlike The Black Stag, this tale is built around
/// investigation, social trust, an advancing tide and embedded tavern-game encounters.
/// </summary>
public static class DrownedBellAdventure
{
    public static AdventureDefinition Build()
    {
        var scenes = new List<AdventureSceneDefinition>
        {
            // ACT I — FOG OVER GREYHAVEN
            S("greyhaven-road", "The Road into Fog",
                "By the time the road falls toward Greyhaven, the sea has disappeared behind a wall of pearl-grey fog. Gulls wheel above fields you can no longer see, and every fence post wears a crust of salt. You came for a quiet night's shelter. The village below looks as though it has forgotten how to offer one.", "act-1", "North Road",
                C("continue-road", "Walk down into Greyhaven.", "village-gate"),
                K("read-weather", "Study the fog before descending.", AdventureAbility.Wisdom, 11, "survival", "fog-signs", "village-gate")),

            S("fog-signs", "Weather That Does Not Move",
                "The fog is wrong. Wind bends the grass inland, but the bank over Greyhaven barely shifts. Beneath the ordinary smell of brine sits something colder: iron, old stone and extinguished candles. Whatever waits below is not only weather.", "act-1", "North Road",
                C("descend-aware", "Keep that in mind and enter the village.", "village-gate", effects: [F("noticed-fog")], xp: 15)),

            S("village-gate", "Greyhaven",
                "Greyhaven is built in layers down the cliff: slate roofs, narrow stairs, black timber and lamps already burning though dusk is an hour away. A weathered board beside the road reads GREYHAVEN — KEEP THE LIGHT. Someone has carved beneath it: IF THE BELL CALLS, DO NOT ANSWER.", "act-1", "Greyhaven",
                C("to-inn", "Find the nearest inn.", "bent-anchor"),
                C("to-harbor", "Walk toward the harbor first.", "harbor-dusk")),

            S("harbor-dusk", "The Empty Harbor",
                "Fishing boats knock gently against the quay. Too many are tied up for a working evening. A young woman in a waxed coat is pinning another missing notice to a piling. She looks at every face that passes, as though comparing it against someone she has memorized.", "act-1", "Greyhaven Harbor",
                C("speak-nell-early", "Ask about the missing notice.", "meet-nell"),
                C("leave-harbor", "Give her space and continue to the inn.", "bent-anchor")),

            S("bent-anchor", "The Bent Anchor",
                "The Bent Anchor is warm, crowded and almost silent. Conversations soften when the door opens, then slowly resume when everyone decides you are merely a stranger. Behind the bar, Mara Holt dries the same glass three times while watching you through its rim.", "act-1", "The Bent Anchor",
                C("ask-room", "Ask Mara for a room and something hot.", "mara-welcome"),
                K("read-room", "Read the room before speaking.", AdventureAbility.Wisdom, 12, "insight", "room-tension", "mara-welcome")),

            S("room-tension", "Everyone Heard It",
                "Three details emerge once you stop looking for conversation and start looking for avoidance: nobody sits near the windows, every table falls quiet when the sea is mentioned, and a brass handbell behind the bar has been wrapped tightly in cloth. Mara notices you noticing it.", "act-1", "The Bent Anchor",
                C("ask-bell", "Ask why the handbell is wrapped.", "mara-welcome", effects: [F("asked-small-bell")], xp: 10)),

            S("mara-welcome", "A Room with the Shutters Closed",
                "Mara takes your coins, pushes a bowl of fish stew across the bar and gives you a room key. 'Shutters stay closed after midnight,' she says. 'If you hear anything from the water, it is wind. If it sounds like a bell, it is still wind.' Her tone makes clear that Greyhaven has agreed on this lie together.", "act-1", "The Bent Anchor",
                C("ask-mara", "Ask what happened here.", "mara-rumor"),
                C("sit-fire", "Take the stew and watch the room.", "nell-enters")),

            S("mara-rumor", "Three Nights Missing",
                "Mara's jaw tightens. Three villagers have disappeared in eight days. One walked from his bed. One vanished from a boat tied to the quay. The third was Nell Rook's brother, Tomas. Each disappearance followed the sound of a bell somewhere beyond the breakwater. 'There is no bell out there,' Mara finishes. 'Not anymore.'", "act-1", "The Bent Anchor",
                C("who-nell", "Ask where to find Nell.", "nell-enters", effects: [F("knows-missing")], xp: 15)),

            S("nell-enters", "Nell Rook",
                "The young woman from the harbor enters carrying rain with her. Nell Rook is tired enough to sway when she stops walking, but anger keeps her upright. She sees the stranger asking questions, sees Mara's expression, and comes straight over. 'If you're another person telling us Tomas ran away, save your breath.'", "act-1", "The Bent Anchor",
                C("promise-help", "Tell Nell you will listen, not judge.", "meet-nell", effects: [R("nell", 1)]),
                C("ask-facts", "Ask for facts first.", "meet-nell"),
                C("stay-out", "Tell her this is not your business.", "first-bell")),

            S("meet-nell", "The Last Thing Tomas Said",
                "Tomas heard the bell before anyone else admitted hearing it. He said it was coming from beneath the old lighthouse, not from the tower. On the night he vanished he left a note: THE NAMES ARE WRONG. Nell has searched the cliff paths every day since.", "act-1", "The Bent Anchor",
                C("take-case", "Promise to help find Tomas.", "rowan-arrives", effects: [F("helping-nell"), R("nell", 1)], xp: 25),
                C("ask-note", "Ask to see Tomas's note.", "tomas-note", effects: [F("helping-nell")])) ,

            S("tomas-note", "The Names Are Wrong",
                "Nell unfolds a damp scrap of paper. Tomas wrote four surnames beneath his warning, all old Greyhaven families. One has been crossed out so violently the paper tore. On the back is a crude drawing of a ship's bell and a small chapel window.", "act-1", "The Bent Anchor",
                C("return-note", "Return the note and agree to investigate.", "rowan-arrives", effects: [F("saw-tomas-note"), R("nell", 1)], xp: 20)),

            S("rowan-arrives", "Father Rowan",
                "A broad-shouldered priest in a salt-stained black coat pauses beside your table. Father Rowan has the look of a man who has not slept because sleep would require surrender. 'If you're involving yourself,' he says, 'come to Saint Orra's tomorrow. There are records Tomas was reading. I would rather someone sensible see them before the village decides to burn them.'", "act-1", "The Bent Anchor",
                C("agree-rowan", "Agree to visit the chapel.", "first-bell", effects: [R("rowan", 1)]),
                K("religious-symbol", "Ask about the chapel symbol on Tomas's note.", AdventureAbility.Intelligence, 11, "religion", "rowan-symbol", "first-bell")),

            S("rowan-symbol", "A Chapel beneath a Chapel",
                "Rowan recognizes the window shape immediately. Saint Orra's current chapel has no such window, but the older foundation beneath it did. That lower chapel was sealed after the Saint Marrow wreck seventy years ago. Rowan stops there, suddenly aware he has said more than he intended.", "act-1", "The Bent Anchor",
                C("keep-confidence", "Do not press him here.", "first-bell", effects: [F("knows-old-chapel"), R("rowan", 1)], xp: 20)),

            S("first-bell", "The First Toll",
                "At 11:47, every conversation in the Bent Anchor dies at once. Far beyond the shuttered windows comes a single low note. It is too deep to be a handbell and too clear to be a buoy. Glass trembles on the shelves. Nell closes her eyes. Mara whispers, 'Don't count them.' A second toll rolls in through the walls.", "act-1", "The Bent Anchor",
                C("open-shutters", "Open the shutters and look toward the sea.", "bell-window"),
                C("listen", "Stay still and listen carefully.", "bell-listen"),
                C("heed-mara", "Leave the shutters closed.", "night-after-bell")),

            S("bell-window", "A Light in the Dead Tower",
                "Fog fills the street like pale smoke. Above the roofs, the abandoned lighthouse should be black. Instead a weak blue-white light circles once behind its cracked lantern glass and vanishes. Nobody else in the room comes to the window.", "act-1", "The Bent Anchor",
                C("mark-light", "Remember exactly where the light appeared.", "night-after-bell", effects: [F("saw-lighthouse-light")], xp: 15)),

            S("bell-listen", "Below the Note",
                "You listen past the bell itself. Beneath it is a second sound: many voices speaking at once, too low to distinguish. One syllable repeats with each toll. It might be a name. It might be many names layered together.", "act-1", "The Bent Anchor",
                C("remember-voices", "Hold onto the sound.", "night-after-bell", effects: [F("heard-names")], xp: 15)),

            S("night-after-bell", "No One Sleeps",
                "The bell stops after seven tolls. Nobody comments on the number. Chairs scrape, doors close and the Bent Anchor empties faster than seems possible. In your room, seawater beads along the inside of the window frame despite the shutters being dry.", "act-1", "The Bent Anchor",
                C("sleep", "Try to sleep before morning.", "morning-greyhaven"),
                K("inspect-water", "Inspect the salt water at the window.", AdventureAbility.Intelligence, 12, "arcana", "salt-trace", "morning-greyhaven")),

            S("salt-trace", "Not Seawater Alone",
                "The droplets contain fine black ash. Arcane residue clings to it in a repeating pattern, not a spell exactly but the memory of one. Something is using a ritual structure to carry sound through water and stone.", "act-1", "The Bent Anchor",
                C("save-sample", "Keep the observation for later.", "morning-greyhaven", effects: [F("knows-bell-ritual")], xp: 20)),

            S("morning-greyhaven", "Act II — The Missing",
                "Morning brings no sunlight, only a brighter shade of fog. Greyhaven offers several trails at once: Tomas's room, Father Rowan's archives, the dead lighthouse and Captain Varro's harbor warehouse. Somewhere among them is the reason the bell has started calling names.", "act-2", "Greyhaven",
                C("begin-investigation", "Start with the village and follow what you find.", "investigation-hub")),

            // ACT II — THE MISSING
            S("investigation-hub", "Four Threads",
                "Greyhaven is small enough to cross in minutes and secretive enough to spend days inside. Each place feels aware of the others. You do not need every answer, but enough pieces may make the pattern impossible to deny.", "act-2", "Greyhaven",
                C("hub-tomas", "Search Tomas Rook's room.", "tomas-room", conditions: [NotFlag("searched-tomas")]),
                C("hub-chapel", "Visit Father Rowan at Saint Orra's.", "chapel-archive", conditions: [NotFlag("searched-chapel")]),
                C("hub-lighthouse", "Climb to the abandoned lighthouse.", "lighthouse-path", conditions: [NotFlag("searched-lighthouse")]),
                C("hub-harbor", "Investigate Captain Varro's warehouse.", "warehouse-front", conditions: [NotFlag("searched-harbor")]),
                C("hub-enough", "Lay out the evidence you have gathered.", "evidence-table", conditions: [Counter("clues", 3)])),

            S("tomas-room", "Tomas's Room",
                "Nell leads you to a narrow room above a net-maker's shop. Tomas left in a hurry, but not randomly. Books lie open to shipping registers. A floorboard beside the bed has been lifted and put back badly.", "act-2", "Rook House",
                K("search-room", "Search the room carefully.", AdventureAbility.Intelligence, 12, "investigation", "tomas-cache", "tomas-obvious"),
                C("ask-nell-room", "Ask Nell what Tomas had been researching.", "tomas-obvious", effects: [R("nell", 1)])),

            S("tomas-cache", "A Page Removed",
                "Under the loose floorboard is a torn shipping ledger page. The vessel name SAINT MARROW survives at the top. Below it, several passenger names have been scraped away and rewritten in newer ink. The handwriting matches none of Tomas's notes.", "act-2", "Rook House",
                C("take-page", "Take the altered manifest page.", "investigation-hub", effects: [I("manifest-fragment"), F("searched-tomas"), N("clues", 1)], xp: 35)),

            S("tomas-obvious", "The Same Four Families",
                "Even without finding a hidden cache, the open books tell you something. Tomas had circled the same four family names from his note again and again across seventy years of harbor records. Captain Varro's family appears most often.", "act-2", "Rook House",
                C("record-families", "Add the family pattern to your notes.", "investigation-hub", effects: [F("searched-tomas"), F("knows-four-families"), N("clues", 1)], xp: 20)),

            S("chapel-archive", "Saint Orra's Chapel",
                "Father Rowan has already laid half the archive across the pews. Births, deaths, storms, wrecks. He points to a gap in the records: the entire winter of the Saint Marrow disaster was recopied twenty years later. The original book is supposedly lost.", "act-2", "Saint Orra's Chapel",
                C("ask-original", "Ask where an original might have survived.", "rowan-crypt"),
                K("compare-records", "Compare the recopied book against older entries.", AdventureAbility.Intelligence, 12, "history", "false-deaths", "rowan-crypt"),
                K("cleric-reading", "Recognize the old funerary notation.", AdventureAbility.Intelligence, 11, "religion", "funeral-code", "rowan-crypt")),

            S("false-deaths", "Deaths without Bodies",
                "The copied register records eleven Saint Marrow passengers as 'lost to sea'. Older parish notation distinguishes bodies recovered from those never found. Here, someone deliberately erased that distinction. Eleven people were declared dead without saying whether Greyhaven ever found them.", "act-2", "Saint Orra's Chapel",
                C("mark-false-deaths", "Mark the discrepancy.", "rowan-crypt", effects: [F("knows-false-deaths"), N("clues", 1)], xp: 25)),

            S("funeral-code", "Eleven Unfinished Rites",
                "The margin marks are not shipping shorthand but unfinished funeral rites. Eleven names were entered for prayers that were never completed. Someone began a memorial and then stopped it deliberately.", "act-2", "Saint Orra's Chapel",
                C("tell-rowan", "Show Rowan what the marks mean.", "rowan-crypt", effects: [F("knows-unfinished-rites"), R("rowan", 2), N("clues", 1)], xp: 30)),

            S("rowan-crypt", "Under Saint Orra's",
                "Rowan unlocks a trapdoor behind the altar. Stone steps descend to the foundation of the older chapel. The air below smells of wet limestone. At the bottom, a bricked arch carries the same window shape Tomas drew.", "act-2", "Old Saint Orra's",
                K("open-crypt", "Find a safe way through the old masonry.", AdventureAbility.Strength, 12, "athletics", "old-chapel", "crypt-blocked"),
                C("use-rope-crypt", "Use rope to reach the side passage.", "old-chapel", conditions: [Item("rope")], effects: [X("rope")]),
                C("leave-crypt", "Do not force the old foundation yet.", "crypt-blocked")),

            S("crypt-blocked", "A Wall for Later",
                "The foundation shifts under pressure. Rowan stops you before the ceiling joins it. Even sealed, the space gives up one detail: a bell-shaped recess in the floor where something heavy once stood.", "act-2", "Old Saint Orra's",
                C("note-recess", "Record the bell-shaped recess and return upstairs.", "investigation-hub", effects: [F("searched-chapel"), N("clues", 1)])),

            S("old-chapel", "The First Memorial",
                "Beyond the arch is a tiny chapel abandoned under the modern one. Eleven names have been scratched into the wall, then covered with limewash. Near them hangs a rusted iron key and a strip of blue ship's ribbon preserved behind wax.", "act-2", "Old Saint Orra's",
                C("take-key", "Take the rusted tide key.", "investigation-hub", effects: [I("tide-key"), I("names-rubbing"), F("searched-chapel"), F("found-eleven-names"), N("clues", 2)], xp: 45)),

            S("lighthouse-path", "The Dead Light",
                "The lighthouse path climbs through thorn and wet grass above a sheer drop. No keeper has lived here in thirty years. Halfway up, you find fresh boot prints going both directions. One set belongs to someone limping.", "act-2", "Lighthouse Cliff",
                K("track-prints", "Follow the limping tracks.", AdventureAbility.Wisdom, 12, "survival", "elias-hut", "lighthouse-door"),
                C("continue-tower", "Continue straight to the tower.", "lighthouse-door")),

            S("elias-hut", "Elias Vane",
                "The tracks lead to a maintenance hut where Elias Vane, Greyhaven's last lighthouse keeper, sits beside a cold stove with a knife across his knees. He is seventy if a day and looks offended that the world has survived him. 'The tower is dead,' he says before you ask. 'The thing below it isn't.'", "act-2", "Keeper's Hut",
                C("ask-elias", "Ask what is below the lighthouse.", "elias-truth", effects: [R("elias", 1)]),
                K("press-elias", "Convince him Tomas may still be alive.", AdventureAbility.Charisma, 12, "persuasion", "elias-truth", "lighthouse-door")),

            S("elias-truth", "The Light Was Put Out",
                "Elias inherited the keeper's private log from his grandfather. On the night Saint Marrow wrecked, Greyhaven's lighthouse did not fail in the storm. Men from the village extinguished it. Elias has spent his life protecting that page because his own family helped them afterward.", "act-2", "Keeper's Hut",
                C("take-log-copy", "Ask Elias for the page.", "lighthouse-door", effects: [I("keeper-log"), F("knows-light-extinguished"), N("clues", 2), R("elias", 1)], xp: 45)),

            S("lighthouse-door", "The Sealed Tower",
                "The tower door is chained from outside, but the chain is new. Someone wants the lighthouse to look abandoned while still controlling access. A narrow drainage arch disappears around the cliff side.", "act-2", "Old Lighthouse",
                K("pick-tower", "Open the new padlock quietly.", AdventureAbility.Dexterity, 12, "sleight-of-hand", "lighthouse-inside", "lighthouse-drain"),
                K("break-chain", "Break the chain.", AdventureAbility.Strength, 13, "athletics", "lighthouse-inside", "lighthouse-drain"),
                C("take-drain", "Use the drainage arch.", "lighthouse-drain")),

            S("lighthouse-drain", "Salt beneath Stone",
                "The drainage arch is barely shoulder-wide and slick with salt. It does not lead into the tower. It slopes downward beneath it. Someone has recently widened the passage with tools.", "act-2", "Lighthouse Drain",
                C("mark-drain", "Do not enter yet; mark it as a route below the lighthouse.", "investigation-hub", effects: [F("knows-cave-route"), F("searched-lighthouse"), N("clues", 1)])),

            S("lighthouse-inside", "A Lantern That Should Not Burn",
                "Dust covers the stairs, but the lantern room above is clean around one pane. A small blue alchemical lamp sits hidden behind the cracked lens — the light you saw last night. Its fuel line runs not to a reservoir but down through the floor toward the cliff.", "act-2", "Old Lighthouse",
                C("trace-line", "Trace the line as far as you safely can.", "lighthouse-mechanism")),

            S("lighthouse-mechanism", "A Signal, Not a Haunting",
                "The line joins a copper speaking tube descending into the rock. Someone has been using the abandoned lighthouse as a signal station. The supernatural bell is real, but at least one human hand has been working around it.", "act-2", "Old Lighthouse",
                C("keep-lamp", "Take the blue salt-lantern and return to town.", "investigation-hub", effects: [I("salt-lantern"), F("searched-lighthouse"), F("knows-human-signal"), N("clues", 2)], xp: 35)),

            S("warehouse-front", "Varro & Sons",
                "Captain Varro's warehouse occupies the best stretch of quay. Two dockhands sit outside a locked office while others move crates despite the grounded fleet. A black raven is branded into several crate lids — not a Greyhaven mark.", "act-2", "Varro Warehouse",
                K("talk-past-guards", "Talk your way inside.", AdventureAbility.Charisma, 13, "persuasion", "warehouse-office", "warehouse-alley"),
                K("sneak-warehouse", "Slip around through the net loft.", AdventureAbility.Dexterity, 13, "stealth", "warehouse-office", "warehouse-alley"),
                C("watch-warehouse", "Watch where the night workers carry crates.", "warehouse-alley")),

            S("warehouse-alley", "Crates for a Dead Route",
                "From the alley you watch two workers carry provisions into a cart labeled for the lighthouse service — a service that has not existed in decades. The cart route climbs toward the cliff after dark.", "act-2", "Varro Warehouse",
                C("record-cart", "Record the false service route.", "investigation-hub", effects: [F("searched-harbor"), F("knows-varro-route"), N("clues", 1)], xp: 20)),

            S("warehouse-office", "Captain's Ledger",
                "Varro's office contains no confession, only numbers. But one ledger lists payments to 'maintenance below the light' and another records food for four people who do not exist on any crew list. A brass key on the desk is stamped with the same tide symbol as Saint Orra's old foundation.", "act-2", "Varro Warehouse",
                C("take-evidence", "Copy the payments and take the brass service key.", "investigation-hub", effects: [I("varro-ledger"), I("service-key"), F("searched-harbor"), N("clues", 2)], xp: 40)),

            S("evidence-table", "The Shape of It",
                "Back at the Bent Anchor, you spread what you have across a corner table. Different routes point to the same conclusion: the lighthouse hides access to something below the cliff, Varro's people have been supplying it, and the Saint Marrow wreck was never the accident Greyhaven recorded.", "act-2", "The Bent Anchor",
                C("ask-mara-varro", "Ask Mara where Varro goes when he wants privacy.", "games-in-lantern"),
                C("confront-varro", "Go directly to Captain Varro.", "varro-confrontation")),

            S("varro-confrontation", "Captain Varro",
                "Varro receives you in the harbor office without pretending surprise. He is neat, grey-haired and tired of being feared by people he considers smaller than himself. 'You have pieces,' he says. 'Pieces make dangerous confidence. If you want the rest, come to the Anchor tonight. Back room.'", "act-2", "Varro Warehouse",
                C("accept-invitation", "Accept the invitation.", "games-in-lantern", effects: [R("varro", 1)]),
                K("intimidate-varro", "Make it clear you are not asking permission.", AdventureAbility.Charisma, 14, "intimidation", "varro-cracks", "games-in-lantern")),

            S("varro-cracks", "A Name He Did Not Mean to Say",
                "Varro's composure slips for one word: 'Marrow.' He corrects himself too late. He knows exactly what lies below the lighthouse. 'Back room,' he repeats, colder now. 'If you still want answers.'", "act-2", "Varro Warehouse",
                C("go-game", "Meet him tonight.", "games-in-lantern", effects: [F("varro-knows-marow"), N("clues", 1)], xp: 25)),

            // ACT III — GAMES IN THE LANTERN
            S("games-in-lantern", "Act III — Games in the Lantern",
                "After midnight Mara unlocks a narrow door behind the pantry. The Bent Anchor's back room is older than the inn above it: green felt, whale-oil lamps, scarred wood and people who stop using surnames. Captain Varro waits at a Blackjack table. In the corner, old Bram Kestrel rattles five bone dice in a cup.", "act-3", "Bent Anchor Back Room",
                C("approach-varro-game", "Sit across from Captain Varro.", "varro-offer"),
                C("approach-bram-game", "Ask Bram about the bone dice.", "bram-offer")),

            S("varro-offer", "One Hand for the Route",
                "Varro places a folded chart beneath his palm. 'One hand. You win, you get the route my men use beneath the lighthouse. I win, you stop breaking into my property until dawn.' Mara deals neither side sympathy.", "act-3", "Bent Anchor Back Room",
                C("play-varro", "Play the hand.", "varro-blackjack"),
                C("leave-varro", "Refuse and speak to Bram instead.", "bram-offer")),

            S("varro-blackjack", "The Captain's Hand",
                "Varro cuts the deck once and slides it back across the felt. The room narrows to cards, breath and the soft tap of Mara's fingernail against the shoe.", "act-3", "Bent Anchor Back Room",
                C("varro-game-win", "[Encounter result: win]", "varro-win", effects: [I("old-sea-chart"), R("varro", 1)]),
                C("varro-game-loss", "[Encounter result: loss]", "varro-loss", effects: [R("varro", -1)]),
                C("varro-game-draw", "[Encounter result: draw]", "varro-draw")),

            S("varro-win", "A Captain Pays",
                "Varro studies the final hand, then pushes the folded chart to you. It shows a service tunnel from the lighthouse drain into a cavern called LOWER CHOIR. One note is underlined twice: PASSABLE ONLY BEFORE HIGH TIDE.", "act-3", "Bent Anchor Back Room",
                C("take-chart", "Pocket the chart and question Bram as well.", "bram-offer", effects: [F("won-varro-game"), N("clues", 1)], xp: 35),
                C("leave-games-win", "Leave with the chart and prepare for the cliff.", "after-games", effects: [F("won-varro-game"), N("clues", 1)], xp: 35)),

            S("varro-loss", "The Captain Collects",
                "Varro gathers the cards. 'A bargain is still a bargain when it hurts.' He will not hand over the chart, but as he folds it you glimpse enough coastline to recognize the lighthouse drain as its starting point.", "act-3", "Bent Anchor Back Room",
                C("accept-loss", "Keep your word and turn to Bram.", "bram-offer", effects: [F("saw-chart-glimpse")]),
                C("leave-games-loss", "Leave before pride costs more.", "after-games", effects: [F("saw-chart-glimpse")])) ,

            S("varro-draw", "No Captain Wins a Push",
                "Varro exhales through his nose. 'Again, or admit the sea has better judgment than either of us.' The chart stays under his hand.", "act-3", "Bent Anchor Back Room",
                C("varro-rematch", "Play another hand.", "varro-blackjack"),
                C("switch-dice", "Leave the push and challenge Bram.", "bram-offer"),
                C("leave-push", "Leave with what you already know.", "after-games")),

            S("bram-offer", "Bram's Bone Dice",
                "Bram's beard reaches his vest and his eyes are much sharper than the rest of him. He places a rolled scrap of oilskin beside the dice cup. 'Tomas gave me this before he went under the cliff. Said Nell was to have it if he didn't return. I dislike sad deliveries. Beat my hand and you can make it yourself.'", "act-3", "Bent Anchor Back Room",
                C("play-bram", "Play Five of a Kind against Bram.", "bram-dice"),
                K("persuade-bram", "Convince Bram that Nell should not depend on a wager.", AdventureAbility.Charisma, 15, "persuasion", "bram-gives-map", "bram-dice"),
                C("leave-bram", "Leave the table and prepare for the cliff.", "after-games")),

            S("bram-dice", "Five Bones",
                "Five bone dice tumble onto black felt. Bram taps two of them with a bent finger. 'Keep what you trust. Throw what you don't. Three rolls. Then the bones tell us which of us is lying to ourselves.'", "act-3", "Bent Anchor Back Room",
                C("bram-game-win", "[Encounter result: win]", "bram-win", effects: [I("tomas-map"), R("bram", 1)]),
                C("bram-game-loss", "[Encounter result: loss]", "bram-loss"),
                C("bram-game-draw", "[Encounter result: draw]", "bram-draw")),

            S("bram-win", "Tomas's Oilskin Map",
                "Bram slides the oilskin over without complaint. Tomas marked a chamber below the lighthouse with eleven short lines around a bell symbol. Beside it he wrote: THEY WANT TO MOVE IT BEFORE THE SPRING TIDE.", "act-3", "Bent Anchor Back Room",
                C("take-tomas-map", "Take the map and prepare to go below the lighthouse.", "after-games", effects: [F("won-bram-game"), F("knows-eleven-bell"), N("clues", 1)], xp: 35)),

            S("bram-gives-map", "A Wager Refused",
                "Bram studies you for a long moment, then pushes the dice aside. 'Good. Tomas would have hated me making Nell pay for his stubbornness.' He gives you the oilskin map anyway and tells you the lowest tunnel floods first.", "act-3", "Bent Anchor Back Room",
                C("thank-bram", "Thank him and prepare for the cliff.", "after-games", effects: [I("tomas-map"), R("bram", 2), F("knows-eleven-bell"), N("clues", 1)], xp: 40)),

            S("bram-loss", "The Bones Keep Their Secret",
                "Bram banks a stronger hand and folds the oilskin away. But disappointment makes him careless: 'If you're going anyway, don't take the lowest tunnel after the third tide mark.' It is not the map, but it is enough to save time.", "act-3", "Bent Anchor Back Room",
                C("take-warning", "Remember the warning and leave.", "after-games", effects: [F("knows-tide-warning")], xp: 15)),

            S("bram-draw", "Bones on Edge",
                "The scores tie. Bram looks delighted. 'That is either fate or poor arithmetic. Again?'", "act-3", "Bent Anchor Back Room",
                C("bram-rematch", "Roll again.", "bram-dice"),
                C("leave-draw", "Call the draw fair and leave.", "after-games")),

            S("after-games", "Before the Tide Turns",
                "By the time you leave the back room, the next bell is less than two hours away. Nell is waiting in the public room with a coil of rope, a lantern and the expression of someone who has already decided she is coming with you.", "act-3", "The Bent Anchor",
                C("bring-nell", "Let Nell come with you.", "cliff-descent", effects: [F("nell-companion"), R("nell", 1)]),
                C("keep-nell-safe", "Ask Nell to stay and be ready to get help.", "cliff-descent", effects: [F("nell-stays"), R("nell", -1), I("rope")])) ,

            // ACT IV — BELOW THE TIDE
            S("cliff-descent", "Act IV — Below the Tide",
                "The lighthouse looms above as you descend the cliff path. The fog is thinner here, shredded by wind. Below, black water moves in and out of the rocks like breath. Every route you discovered points toward the drainage arch under the tower.", "act-4", "Lighthouse Cliff",
                C("enter-drain", "Enter through the lighthouse drain.", "lower-choir", effects: [N("tide", 1)]),
                C("use-service-key", "Use Varro's service key on the cliff hatch.", "service-tunnel", conditions: [Item("service-key")], effects: [N("tide", 1)]),
                C("follow-tomas-map", "Follow Tomas's marked upper route.", "upper-cave", conditions: [Item("tomas-map")], effects: [N("tide", 1)])),

            S("service-tunnel", "The Smuggler's Stair",
                "The brass key opens a disguised iron hatch. Beyond it, cut steps descend through the cliff. Crates, lamp oil and fresh boot marks prove Varro's people have used this route for years. One crate contains bandages and tonic rather than contraband.", "act-4", "Service Tunnel",
                C("take-tonic", "Take a healing tonic and continue.", "lower-choir", effects: [I("healing-potion"), F("found-supplies"), N("tide", 1)]),
                C("move-fast-service", "Leave the supplies and move quickly.", "lower-choir")),

            S("upper-cave", "Tomas's Upper Route",
                "Tomas's oilskin markings lead across a natural shelf above the flooded tunnel. Halfway across, you find his broken lantern and a strip of Nell's family-blue scarf tied to a rock. The knot points deeper in.", "act-4", "Upper Sea Caves",
                C("follow-scarf", "Follow Tomas's markers.", "tomas-alive", effects: [F("found-tomas-trail"), N("tide", 1)]),
                K("inspect-fall", "Check whether Tomas fell from the shelf.", AdventureAbility.Wisdom, 12, "perception", "tomas-alive", "lower-choir")),

            S("lower-choir", "The Lower Choir",
                "The drain opens into a vaulted cavern where each wave produces a different note. Old stonework lines the walls. Someone once built a chapel here, then the sea reclaimed most of it. Water is already ankle-deep and rising.", "act-4", "Lower Choir",
                C("wade-forward", "Wade toward the carved arch.", "cold-crossing", effects: [A("soaked", 4), N("tide", 1)]),
                K("find-dry-route", "Search for a drier route along the wall.", AdventureAbility.Wisdom, 13, "perception", "dry-ledge", "cold-crossing")),

            S("dry-ledge", "Above the Waterline",
                "Behind a curtain of mineral deposits is a narrow mason's ledge. It keeps you above the coldest water and passes three old tide marks cut into the wall. The highest is only a handspan above the current surface.", "act-4", "Lower Choir",
                C("take-ledge", "Follow the ledge deeper.", "fork-below", effects: [F("saw-high-tide-mark"), N("tide", 1)])),

            S("cold-crossing", "Black Water",
                "The water reaches your waist before the floor rises again. Cold punches the air from your lungs. By the far side your clothes are heavy and your fingers refuse fine work.", "act-4", "Lower Choir",
                C("push-through-cold", "Keep moving before the water rises further.", "fork-below", effects: [A("cold", 4), N("tide", 1)]),
                C("drink-tonic-cold", "Use a healing tonic and warm yourself briefly.", "fork-below", conditions: [Item("healing-potion")], effects: [X("healing-potion"), H(4), N("tide", 1)])),

            S("fork-below", "Two Ways Down",
                "The cavern divides. Left, fresh boot marks lead through a reinforced tunnel. Right, old chapel carvings descend toward the sound of water striking bronze. Somewhere ahead, a human voice coughs once.", "act-4", "Lower Choir",
                C("follow-cough", "Follow the human voice.", "tomas-alive", effects: [N("tide", 1)]),
                C("follow-bell", "Follow the chapel carvings toward the bell.", "flooded-gallery", effects: [N("tide", 1)])),

            S("tomas-alive", "Tomas Rook",
                "You find Tomas chained to an iron ring above the tide line, bruised, dehydrated and very much alive. Varro's men caught him photographing the old inscriptions. He laughs once when he sees you. 'Tell Nell I was right first. Then you can rescue me.'", "act-4", "Smuggler Holding Cave",
                C("free-tomas-key", "Use the tide key on the old iron lock.", "tomas-freed", conditions: [Item("tide-key")]),
                K("free-tomas-lock", "Work the lock without a key.", AdventureAbility.Dexterity, 12, "sleight-of-hand", "tomas-freed", "tomas-cost"),
                K("break-tomas-chain", "Break the weakened ring from the stone.", AdventureAbility.Strength, 13, "athletics", "tomas-freed", "tomas-cost")),

            S("tomas-cost", "The Ring Gives Last",
                "The old fastening finally tears free, but not cleanly. Stone and iron rake across your arm as Tomas falls forward. You have him — and a bleeding wound to carry deeper.", "act-4", "Smuggler Holding Cave",
                C("bind-and-move", "Bind the wound and move.", "tomas-freed", effects: [D(-3), A("bleeding", 3), N("tide", 1)])),

            S("tomas-freed", "The Witness",
                "Tomas can walk. More importantly, he knows what Varro's people were doing: they were trying to raise a bronze bell from the drowned chapel before spring tide exposed the chamber to outsiders. Each attempt made the ringing worse. Tomas found the original passenger manifest in a sealed reliquary and hid it before they caught him.", "act-4", "Smuggler Holding Cave",
                C("ask-manifest", "Ask Tomas where he hid the original manifest.", "manifest-route", effects: [F("tomas-rescued"), R("nell", 2), N("clues", 2)], xp: 60)),

            S("manifest-route", "The Reliquary Path",
                "Tomas points toward the old chapel route. The manifest is behind a saint's niche above the flooded gallery. 'If the bell rings while you're in there, don't listen for words,' he warns. 'It knows which name hurts.'", "act-4", "Lower Choir",
                C("send-tomas-back", "Send Tomas toward the surface and continue alone.", "flooded-gallery", effects: [F("tomas-safe"), N("tide", 1)]),
                C("bring-tomas", "Keep Tomas with you as a guide.", "flooded-gallery", effects: [F("tomas-with-player"), N("tide", 1)])),

            S("flooded-gallery", "The Drowned Procession",
                "Stone saints stand waist-deep in seawater along the gallery. Their faces have been chiseled away. When the distant bell shifts in the current, eleven pale reflections appear between them although there are only ten statues.", "act-4", "Drowned Gallery",
                K("face-dread", "Keep your attention on the physical room.", AdventureAbility.Wisdom, 13, "insight", "saints-niche", "dread-gallery"),
                C("use-names-rubbing", "Read the eleven recovered names aloud.", "saints-niche", conditions: [Item("names-rubbing")], effects: [F("honored-names"), R("rowan", 1)], xp: 35)),

            S("dread-gallery", "The Name in Your Voice",
                "One reflection turns toward you and speaks in your own voice. For several breaths you cannot remember why you came here, only that the water would be warmer if you stopped resisting it.", "act-4", "Drowned Gallery",
                C("force-forward", "Force yourself toward the saint's niche.", "saints-niche", effects: [A("dread", 3), N("tide", 1)])),

            S("saints-niche", "The Original Manifest",
                "Behind a cracked statue of Saint Orra is a copper cylinder wrapped in oilskin. Inside lies the original Saint Marrow passenger manifest. Eleven names marked 'survivors landed at Greyhaven' were later crossed through in a different hand. The copied village record called those same people lost at sea.", "act-4", "Drowned Gallery",
                C("take-manifest", "Take the original manifest.", "truth-of-marow", effects: [I("saint-marrow-manifest"), F("has-original-names"), N("clues", 3)], xp: 60)),

            S("truth-of-marow", "What Greyhaven Did",
                "The remaining papers complete the crime. Seventy years ago four village families extinguished the lighthouse to wreck Saint Marrow and steal its payroll chest. Eleven passengers survived the rocks. Greyhaven's conspirators killed them in the lower chapel, rewrote the records and used the stolen gold to rebuild the harbor. The bell was sunk with the bodies.", "act-4", "Drowned Gallery",
                C("accept-truth", "Carry the truth to the bell chamber.", "varro-below", effects: [F("knows-whole-truth"), N("tide", 1)], xp: 75)),

            S("varro-below", "Varro Comes Below",
                "Lanterns appear behind you. Captain Varro enters the gallery with two dockhands and no weapon drawn. 'My grandfather told me enough to hate him,' he says. 'I kept the tunnels quiet because exposing this destroys people who had no part in it. I was moving the bell before someone else found it. That was my mistake.'", "act-4", "Drowned Gallery",
                C("trust-varro", "Accept that he was containing the secret, not creating it.", "bell-threshold", effects: [R("varro", 2), F("varro-cooperates")]),
                C("reject-varro", "Tell Varro the living do not get to choose silence for the dead.", "bell-threshold", effects: [R("varro", -2), F("varro-opposed")]),
                K("read-varro", "Decide whether Varro is lying now.", AdventureAbility.Wisdom, 14, "insight", "varro-honest", "bell-threshold")),

            S("varro-honest", "A Guilty Man, Not That Crime",
                "Varro is hiding plenty — smuggling, bribery, the imprisonment of Tomas — but not this. He did not cause the bell's return and he is genuinely afraid that moving it again will kill someone. His shame is inherited; his recent crimes are entirely his own.", "act-4", "Drowned Gallery",
                C("take-honesty", "Use the truth without forgiving him.", "bell-threshold", effects: [F("knows-varro-honest"), R("varro", 1)], xp: 30)),

            S("bell-threshold", "The Door beneath the Tide",
                "A round bronze door seals the final chamber. Eleven empty nameplates ring its frame. The tide key fits its center. Without the key, a corroded wheel can still be forced. Water curls around your boots and rises another finger-width.", "act-4", "Drowned Chapel",
                C("open-with-key", "Open the bronze door with the tide key.", "bell-chamber", conditions: [Item("tide-key")], effects: [N("tide", 1)]),
                K("force-bronze", "Force the corroded wheel before the tide seals it.", AdventureAbility.Strength, 14, "athletics", "bell-chamber", "tide-crash")),

            S("tide-crash", "High Water",
                "The wheel breaks loose at the same moment a surge fills the gallery. The door opens, but the return route disappears beneath black water. Whatever ending waits here, you will have to earn your way back to the surface.", "act-4", "Drowned Chapel",
                C("enter-high-water", "Enter the bell chamber.", "bell-chamber", effects: [A("soaked", 4), A("cold", 3), F("high-tide-trapped"), N("tide", 2)])),

            // ACT V — THE BELL BENEATH
            S("bell-chamber", "Act V — The Bell Beneath",
                "The chamber is larger than the chapel above it should allow. A bronze ship's bell hangs half-submerged from a stone arch, suspended by chains green with age. Beneath it lies the broken payroll chest. Around the walls, eleven shallow graves have been opened by the tide. When you enter, the bell swings without being touched.", "act-5", "Bell Chamber",
                C("approach-bell", "Approach the bell and listen.", "eleven-voices"),
                C("inspect-gold", "Inspect the opened payroll chest first.", "gold-chest")),

            S("gold-chest", "Greyhaven's Foundation",
                "Most of the Saint Marrow gold vanished generations ago, but enough remains beneath the collapsed chest to change a life. No ghost guards it. No trap protects it. The temptation is entirely ordinary.", "act-5", "Bell Chamber",
                C("leave-gold", "Leave the gold where the dead can see it.", "eleven-voices", effects: [F("refused-gold")]),
                C("take-gold", "Take what remains and leave the old crime to the sea.", "ending-gold", effects: [F("took-gold"), N("tide", 1)])),

            S("eleven-voices", "Eleven Names",
                "The bell tolls once. This close, the sound does not strike your ears; it passes through bone. Eleven voices speak, not asking for vengeance, only repeating names that Greyhaven erased. The dead do not want the village destroyed. They want to have existed.", "act-5", "Bell Chamber",
                C("show-manifest", "Hold up the original manifest and answer with their names.", "names-restored", conditions: [Item("saint-marrow-manifest")], effects: [F("answered-dead")]),
                C("silence-bell", "Decide the safest answer is to silence the bell forever.", "break-bell-choice"),
                C("take-truth-up", "Take the evidence back to Greyhaven and expose everything.", "escape-with-truth")),

            S("names-restored", "The Debt",
                "As each name is spoken, one grave fills with still water instead of churning foam. The final voice does not fade. It asks for the rite that was started and abandoned seventy years ago: name the dead, return the stolen record, toll the bell once for each life, and let Greyhaven hear what it built itself upon.", "act-5", "Bell Chamber",
                C("perform-rite", "Complete the memorial rite.", "rite-preparation", conditions: [Flag("honored-names"), Flag("knows-whole-truth")]),
                C("expose-instead", "Take the manifest to the village instead of trusting the ritual.", "escape-with-truth"),
                C("destroy-after-names", "Give them their names, then destroy the bell.", "break-bell-choice")),

            S("rite-preparation", "A Bell for Each Name",
                "The rite needs no miracle beyond honesty. Eleven names. Eleven tolls. Rowan's unfinished notation supplies the words if you understood it; Tomas's manifest supplies the truth. Above you, Greyhaven waits inside its fog.", "act-5", "Bell Chamber",
                C("ring-eleven", "Ring the bell eleven times and complete the debt.", "ending-debt-paid", conditions: [Flag("has-original-names"), Flag("tomas-rescued"), Relation("nell", 2)]),
                C("not-enough", "You cannot complete every part of the rite; carry the truth upward instead.", "escape-with-truth")),

            S("break-bell-choice", "Bronze and Bone",
                "Destroying the bell will end its call, but it will also end the only force compelling Greyhaven to look at the old crime. The chains can be cut and the bell dropped into the deepest fissure where even spring tide cannot reach it.", "act-5", "Bell Chamber",
                K("break-bell", "Bring the bell down into the fissure.", AdventureAbility.Strength, 13, "athletics", "ending-silent", "escape-with-truth"),
                C("use-rope-bell", "Use the rope and old pulley to drop it safely.", "ending-silent", conditions: [Item("rope")], effects: [X("rope")])) ,

            S("escape-with-truth", "The Water Rises",
                "With the manifest protected beneath your coat, you turn for the surface. The tide has claimed part of the return route. Behind you the bell continues its slow impossible swing. Ahead, Greyhaven's lights are only a pale suggestion through rock and water.", "act-5", "Lower Choir",
                K("escape-swim", "Cross the flooded section before it closes.", AdventureAbility.Constitution, 13, null, "village-reckoning", "ending-drowned", modifier: HasFlagModifier("knows-tide-warning", 1)),
                C("escape-upper-map", "Use Tomas's upper route to avoid the flooded passage.", "village-reckoning", conditions: [Item("tomas-map")]),
                C("escape-chart", "Use Varro's marked service route.", "village-reckoning", conditions: [Item("old-sea-chart")])) ,

            S("village-reckoning", "Greyhaven Hears",
                "Dawn finds the village gathered inside Saint Orra's. Tomas stands beside Nell if you rescued him. Rowan places the original manifest on the altar. Varro remains at the back, expression unreadable. You can still decide how much of the truth belongs to everyone.", "act-5", "Saint Orra's Chapel",
                C("publish-all", "Read the original manifest and keeper's evidence aloud.", "ending-remembers", conditions: [Item("saint-marrow-manifest")]),
                C("quiet-record", "Restore the names to the parish record without publicly naming descendants.", "ending-remembers", conditions: [Item("saint-marrow-manifest"), Relation("rowan", 1)]),
                C("bury-truth", "Give Greyhaven the names but bury the details of the crime.", "ending-silent")),

            // ENDINGS
            E("ending-silent", "Ending — The Silent Bell",
                "The bell never rings again. Greyhaven sleeps. Some people call that peace. The missing are remembered only in fragments, and the oldest families keep most of their secrets. You leave knowing you ended the danger — and wondering whether silence was simply the village's first crime repeated more gently.", "silent-bell", 300),

            E("ending-remembers", "Ending — What the Sea Remembers",
                "Greyhaven hears every surviving fact. The Saint Marrow names return to the parish wall, the old harbor families lose the protection of a shared lie, and Captain Varro faces the crimes he committed while guarding it. The village does not collapse. It changes, painfully and in public. For the first time in seventy years, the lighthouse burns through the fog without shame.", "sea-remembers", 420),

            E("ending-debt-paid", "Ending — A Debt Paid",
                "Eleven tolls roll across Greyhaven before dawn. With each one, Rowan speaks a name from the original manifest and Nell answers it from the shore. On the eleventh, the fog tears open above the harbor. The bell beneath the cliff cracks down its center without a hand touching it. No treasure returns, no ancestor is forgiven, but the dead are named and the living finally know why. The sea does not ring again.", "debt-paid", 500),

            E("ending-drowned", "Ending — The Drowned",
                "The tide reaches the roof of the lower passage before you reach the final stair. Greyhaven's lamps shimmer above the water like another sky. The next morning the bell rings once more, and among its layered voices there is one new name the village does not yet know how to mourn.", "the-drowned", 150),

            E("ending-gold", "Ending — Gold Beneath Greyhaven",
                "You leave the bell to its dead and take what remains of Saint Marrow's gold. It is enough to make the road away from Greyhaven very comfortable. Behind you, the fog closes over the harbor. Weeks later travelers still tell stories of a bell that rings from beneath the sea. You know exactly what it wants. You simply chose not to pay the debt.", "greyhaven-gold", 250),
        };

        IReadOnlyList<AdventureStatusDefinition> conditions = AdventureBlueprintCatalog.StandardConditions
            .Concat(new AdventureStatusDefinition[]
            {
                new("soaked", "Soaked", "Salt water has saturated your clothing and equipment.", "water"),
                new("cold", "Cold", "Cold has settled into your hands and concentration, imposing disadvantage on ability checks.", "snow", DisadvantageOnAbilityChecks: true),
                new("dread", "Dread", "The drowned voices keep pulling your thoughts away from the present.", "fear", DisadvantageOnAbilityChecks: true),
                new("saltburn", "Saltburn", "Raw saltwater wounds cost 1 HP as scenes advance until treated.", "salt", PeriodicHitPointDelta: -1),
            })
            .ToArray();

        var adventure = new AdventureDefinition(
            "drowned-bell",
            1,
            "The Drowned Bell",
            "A coastal mystery of fog, missing names and a bell that rings from beneath the sea.",
            "greyhaven-road",
            scenes,
            Items:
            [
                new("manifest-fragment", "Altered Manifest Page", "A torn Saint Marrow ledger page with names changed in newer ink.", "letter", AdventureItemKind.Quest),
                new("tide-key", "Tide Key", "A rusted key from Old Saint Orra's, marked with a three-wave sigil.", "key", AdventureItemKind.Utility),
                new("names-rubbing", "Eleven Names Rubbing", "A charcoal rubbing preserving eleven names hidden beneath chapel limewash.", "rubbing", AdventureItemKind.Quest),
                new("keeper-log", "Keeper's Log Page", "Proof that Greyhaven's lighthouse was deliberately extinguished on the Saint Marrow night.", "ledger", AdventureItemKind.Quest),
                new("salt-lantern", "Blue Salt-Lantern", "An alchemical signal lamp taken from the abandoned lighthouse.", "lantern", AdventureItemKind.Utility),
                new("varro-ledger", "Varro Ledger Copy", "Payments for maintenance beneath the dead lighthouse.", "ledger", AdventureItemKind.Quest),
                new("service-key", "Brass Service Key", "A newer key used by Varro's crews to enter the cliff service tunnel.", "key", AdventureItemKind.Utility),
                new("old-sea-chart", "Lower Choir Chart", "Varro's marked route through the service tunnels and tidal caves.", "map", AdventureItemKind.Quest),
                new("tomas-map", "Tomas's Oilskin Map", "A hand-drawn map marking the drowned chapel and safer upper route.", "map", AdventureItemKind.Quest),
                new("saint-marrow-manifest", "Saint Marrow Manifest", "The original passenger manifest proving eleven survivors reached Greyhaven alive.", "list", AdventureItemKind.Quest),
                new("rope", "Hemp Rope", "A reliable coil useful on the cliff and around the old bell machinery.", "rope", AdventureItemKind.Utility),
                new("healing-potion", "Healing Tonic", "A bitter red coastal tonic that restores health.", "potion-red", AdventureItemKind.Consumable, 3),
            ],
            Conditions: conditions,
            Acts: new Dictionary<string, string>
            {
                ["act-1"] = "Fog over Greyhaven",
                ["act-2"] = "The Missing",
                ["act-3"] = "Games in the Lantern",
                ["act-4"] = "Below the Tide",
                ["act-5"] = "The Bell Beneath",
            });

        AdventureDefinitionValidator.Validate(adventure);
        AdventureEncounterCatalog.Validate(adventure);
        return adventure;
    }

    private static AdventureSceneDefinition S(string id, string title, string body, string act, string location, params AdventureChoiceDefinition[] choices) =>
        new(id, title, body, Choices: choices, ActId: act, Location: location);

    private static AdventureSceneDefinition E(string id, string title, string body, string endingId, int xp) =>
        new(id, title, body, OnEnterEffects: [new AdventureEffect(AdventureEffectKind.GainCharacterExperience, Value: xp)], IsEnding: true, EndingId: endingId, ActId: "act-5", Location: "Greyhaven");

    private static AdventureChoiceDefinition C(
        string id,
        string text,
        string next,
        IReadOnlyList<AdventureCondition>? conditions = null,
        IReadOnlyList<AdventureEffect>? effects = null,
        int xp = 0)
    {
        IReadOnlyList<AdventureEffect>? merged = xp <= 0
            ? effects
            : (effects ?? []).Concat([new AdventureEffect(AdventureEffectKind.GainCharacterExperience, Value: xp)]).ToArray();
        return new AdventureChoiceDefinition(id, text, next, Conditions: conditions, Effects: merged);
    }

    private static AdventureChoiceDefinition K(
        string id,
        string text,
        AdventureAbility ability,
        int dc,
        string? skill,
        string success,
        string failure,
        int modifier = 0) =>
        new(id, text, Check: new AdventureCheckDefinition(id + "-check", ability, dc, skill, modifier, SuccessSceneId: success, FailureSceneId: failure));

    private static AdventureEffect F(string key) => new(AdventureEffectKind.SetFlag, key);
    private static AdventureEffect I(string key) => new(AdventureEffectKind.AddItem, key);
    private static AdventureEffect X(string key) => new(AdventureEffectKind.RemoveItem, key);
    private static AdventureEffect R(string key, int value) => new(AdventureEffectKind.ChangeNpcRelation, key, value);
    private static AdventureEffect N(string key, int value) => new(AdventureEffectKind.ChangeCounter, key, value);
    private static AdventureEffect A(string key, int duration) => new(AdventureEffectKind.ApplyCondition, key, duration);
    private static AdventureEffect H(int value) => new(AdventureEffectKind.ChangeHitPoints, Value: value);
    private static AdventureEffect D(int value) => new(AdventureEffectKind.ChangeHitPoints, Value: value);

    private static AdventureCondition Flag(string key) => new(AdventureConditionKind.Flag, key);
    private static AdventureCondition NotFlag(string key) => new(AdventureConditionKind.Flag, key, Expected: false);
    private static AdventureCondition Item(string key) => new(AdventureConditionKind.ItemOwned, key);
    private static AdventureCondition Counter(string key, int value) => new(AdventureConditionKind.CounterMinimum, key, value);
    private static AdventureCondition Relation(string key, int value) => new(AdventureConditionKind.NpcRelationMinimum, key, value);

    // AdventureCheckDefinition currently accepts a static modifier. This helper documents the intended content
    // hook without coupling checks to arbitrary flags; the route itself still benefits from any acquired map/chart.
    private static int HasFlagModifier(string key, int value) => 0;
}
