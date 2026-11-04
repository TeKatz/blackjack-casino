using System.IO;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Authored archive copy for the grounded collection tiers. Rare and higher entries stay
/// deliberately sealed until their larger theme stories are designed.
/// </summary>
internal static class CollectionLore
{
    private static readonly IReadOnlyDictionary<string, string> Entries = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["velvet-table-01"] = "Every night at the Velvet Table begins the same way: one card, face down, gliding across green felt beneath a white glove. Regulars say the first deal sets the mood for the whole room. Some players tap the table for luck before they ever look at it.",
        ["velvet-table-02"] = "The house replaces its green felt only when the old surface has carried too many stories to hide. Dealers claim they can still point out the faint places where fortunes changed hands, even after years of brushing, spills, and nervous fingertips.",
        ["velvet-table-03"] = "This jade-and-gold chip was never part of the casino's official inventory. It keeps returning to the cage anyway, usually after an impossible comeback. No one admits to owning it, and the night manager has stopped trying to remove it.",
        ["velvet-table-04"] = "A cut card is supposed to be ordinary plastic. This red one has ended more hot streaks than anyone can explain. Dealers joke that when it appears early in the shoe, the house has already decided it has seen enough.",
        ["velvet-table-05"] = "There is a particular silence when a player doubles down: chips slide forward, conversation stops, and suddenly one card matters twice as much. The old dealers call it the cleanest kind of courage because there is no room left to hesitate.",
        ["velvet-table-06"] = "Two matching aces beside a fresh rose became a private tradition at the Velvet Table after a newlywed couple won enough in one hand to pay for their entire honeymoon. The rose is replaced every evening; the story is not.",
        ["velvet-table-07"] = "A split begins with one hand and ends with two futures. The silver chip pictured here belonged to a player who split against every piece of advice, won both hands, and quietly left before anyone learned their name.",

        ["neon-city-01"] = "Rain makes the alleyways of Neon City brighter, not darker. Every sign becomes two signs in the pavement, and strangers under umbrellas seem to appear only for a few steps before vanishing into blue reflections.",
        ["neon-city-02"] = "Cab 47 has been working the midnight district longer than most of its drivers have been alive. Its meter runs a little slow, its radio only finds old stations, and somehow it always arrives when the rain starts.",
        ["neon-city-03"] = "This streetlight is one of the few fixtures left from the city before the neon boom. It still glows warm amber through violet fog, stubbornly refusing every attempt to replace its bulb with something brighter.",
        ["neon-city-04"] = "The token came from an arcade that closed years ago, yet machines across the district still accept it. Players who find one usually spend it immediately. Keeping it in your pocket overnight is said to make every screen nearby flicker.",
        ["neon-city-05"] = "The glowing koi has hung above the same noodle house for decades. Its tubes buzz louder before storms, and night workers use its orange reflection as an unofficial landmark: if you can see the fish, you are almost home.",
        ["neon-city-06"] = "The rooftop bar opens after sunset and never advertises. Its best table looks over the entire eastern skyline, and the bartender has a habit of serving exactly the drink a guest was about to order before they say a word.",
        ["neon-city-07"] = "Electric Avenue never truly sleeps. Trams, signs, markets, and crowds keep moving until dawn, turning the boulevard into a river of light. Locals measure distance there not in blocks, but in how many songs play before you arrive.",

        ["midnight-manor-01"] = "The iron gate opens inward without being touched, but only after sunset. Visitors who arrive during the day find it rusted shut. The manor's oldest ledger simply calls this courtesy 'the house recognizing company.'",
        ["midnight-manor-02"] = "This key was discovered beneath three layers of velvet in a locked writing desk. No matching lock has ever been found. Dust returns to its teeth within minutes of cleaning, as though it has just been taken from somewhere long abandoned.",
        ["midnight-manor-03"] = "The candles in the west library burn without shortening. Servants once tried replacing them and found the new candles extinguished by morning, while the originals stood relit in their old places.",
        ["midnight-manor-04"] = "Every portrait in the long hall has a brass nameplate except one. Guests disagree about who is painted there, and several swear the face changes depending on which direction they walk through the corridor.",
        ["midnight-manor-05"] = "The velvet curtain covers a tall window facing the moonlit grounds. It is always drawn at midnight. Anyone who opens it after the clock strikes twelve hears music from the garden, even when the lawn below is empty.",
        ["midnight-manor-06"] = "The music box plays a waltz no surviving score contains. Its tiny dancers turn only when no one winds the mechanism, and the melody grows clearer the closer the manor gets to midnight.",
        ["midnight-manor-07"] = "The secret stair was found behind a bookcase that had not moved in a century. It descends farther than the manor's foundations should allow. The final landing has never appeared on any architectural plan.",

        ["ocean-depths-01"] = "Pieces of sea glass collect in the same narrow cove after winter storms, polished smooth enough to catch the sunrise. Sailors once traded the rare blue fragments as charms, believing the ocean only softens what it intends to return.",
        ["ocean-depths-02"] = "This red coral grew around the bell of a wrecked survey ship. Divers left it untouched for decades, and the branch slowly took the shape of the bell beneath it, as if the reef had memorized what was lost.",
        ["ocean-depths-03"] = "The shell was found open on the seabed with a pearl already resting inside. No living oyster was attached to it. Since then, the pearl has never lost its soft inner glow, even in complete darkness.",
        ["ocean-depths-04"] = "A storm carried this twisted trunk onto the shore after three days of rough water. Its wood is salt-white, impossibly heavy, and marked with tiny spiral grooves that resemble waves when viewed from a distance.",
        ["ocean-depths-05"] = "This golden jellyfish is seen only in deep water where sunlight should not reach. Its bell glows like a lantern and smaller fish follow beneath it, using the drifting creature as a moving island of light.",
        ["ocean-depths-06"] = "The old sea turtle has been photographed by divers for more than forty years. A pale notch in its shell makes it easy to recognize. Every spring it returns to the same reef, regardless of storms, ships, or changing currents.",
        ["ocean-depths-07"] = "Manta rays gather above the blue trench just before sunrise. Researchers still do not know why. For a few quiet minutes they circle in formation, then disappear into deeper water as if responding to a signal no one else can hear.",

        ["enchanted-forest-01"] = "Travelers use the moss-covered stone as a marker even though no one remembers placing it there. Its green surface stays damp through drought and snow, and tiny flowers bloom around it whenever someone lost finally finds the path.",
        ["enchanted-forest-02"] = "The spiral fern unfurls only after rain. Forest folk say its curled frond keeps one secret for every turn in the spiral, which is why children whisper wishes into it before the leaf opens.",
        ["enchanted-forest-03"] = "These fireflies gather above still water in patterns too deliberate to be random. From the far bank they sometimes resemble letters, but no two observers agree on what the glowing message says.",
        ["enchanted-forest-04"] = "The red-capped mushrooms appear overnight in perfect rings beneath old trees. Animals avoid stepping inside them. People who do are said to lose only a few minutes, though the moss on their boots may suggest much longer.",
        ["enchanted-forest-05"] = "The silver fox is rarely seen twice by the same traveler. Its coat reflects moonlight like frost, and it has a habit of appearing at forks in the trail before choosing the safer path without ever looking back.",
        ["enchanted-forest-06"] = "The ancient oak was already enormous when the first road reached the forest. Lightning has split its crown more than once, yet each spring new leaves grow over the scars. Locals leave small wooden tokens between its roots in thanks.",
        ["enchanted-forest-07"] = "The white stag drinks only from the mountain lake beneath a clear moon. Its antlers seem to catch starlight between their branches. Hunters in old stories who lowered their bows after seeing it always found their way home.",

        ["desert-gold-01"] = "One grain of desert sand means nothing until sunlight catches it. Then an entire dune can glitter like spilled gold. Caravan guides say the desert teaches this lesson often: value depends on where you stand and when you look.",
        ["desert-gold-02"] = "Clay lanterns like this one mark the edges of night camps. The star-shaped holes cast small constellations across tent walls, giving travelers a familiar sky even when sandstorms hide the real one.",
        ["desert-gold-03"] = "The date palms at the canyon spring have fed travelers for generations. No map marks the grove, yet old trade routes bend toward it as though every caravan somehow knew exactly where shade would be waiting.",
        ["desert-gold-04"] = "The desert rose is not a flower but a crystal shaped slowly by sand, salt, and time. Merchants once carried them as proof that even the driest ground could create something delicate.",
        ["desert-gold-05"] = "At sunset, the last camels of a caravan become black silhouettes against the gold horizon. Guides keep the lead bell ringing after dark; in open desert, that small sound can hold an entire company together.",
        ["desert-gold-06"] = "This oasis sits below red cliffs and can be heard before it is seen. Birds gather in the palms, water cools the air, and travelers traditionally leave one cup untouched for whoever reaches the spring next.",
        ["desert-gold-07"] = "The golden dune moves several meters every year, swallowing tracks almost as soon as they are made. Old guides still navigate by its curved crest, insisting that something so restless can remain a reliable landmark.",

        ["celestial-voyage-01"] = "Stardust sounds poetic until it strikes a viewport at impossible speed. The glittering cloud pictured here was collected from a region between mapped systems, where every grain carried traces of elements older than the nearby stars.",
        ["celestial-voyage-02"] = "This lunar stone was taken from the rim of a crater facing Earth. Its dark surface looks ordinary until sunlight touches the embedded glass, revealing tiny silver flashes left by impacts millions of years apart.",
        ["celestial-voyage-03"] = "The survey satellite was built for a five-year mission and has worked for thirty-two. Engineers stopped predicting its failure long ago. Each year it transmits one perfectly timed burst on the anniversary of its launch.",
        ["celestial-voyage-04"] = "The brass star map combines observation with tradition: measured coordinates beside hand-drawn constellations. Navigators keep it even though modern instruments are more accurate, because machines can tell you where you are but not why a place has a name.",
        ["celestial-voyage-05"] = "A comet's tail can stretch farther than worlds and still be thinner than laboratory air. Pilots who pass near one describe the same strange sight: sunlight scattered through dust like a luminous road pointing away from the Sun.",
        ["celestial-voyage-06"] = "The ringed planet dominates every window when a ship enters the outer system. Its bands look motionless from afar, though each is made from countless pieces of ice racing around the world at extraordinary speed.",
        ["celestial-voyage-07"] = "Solar sails trade engines for patience. This one crossed millions of kilometers by catching nothing more solid than light. Its crew called the voyage slow only until they remembered what, exactly, was pushing them.",

        ["frost-kingdom-01"] = "No two snowflakes are promised to be unique, but the palace archivists have tried to prove it anyway. For three centuries they have pressed perfect crystals into black glass, and no matching pair has yet survived long enough to settle the argument.",
        ["frost-kingdom-02"] = "The red leaf froze into lake ice before winter could bleach its color. Children return each year looking for it beneath the clear surface, convinced that as long as the leaf remains bright, spring is guaranteed.",
        ["frost-kingdom-03"] = "Blue crystals grow in the northern caves where ordinary ice should crack under its own weight. They ring softly when touched. Miners learned quickly that removing too many at once makes the entire cavern answer.",
        ["frost-kingdom-04"] = "Winter lanterns are hung outside every occupied home during the longest nights. Their warm light is practical, but the custom is older than the roads: a lit lantern means any traveler caught in the cold may knock.",
        ["frost-kingdom-05"] = "The arctic fox survives by becoming almost invisible. This one is known by the grey mark above its left eye and by its complete lack of fear around the palace kitchens, where several cooks insist it has learned the dinner schedule.",
        ["frost-kingdom-06"] = "Snow owls nest on the western cliffs above the frozen valley. Couriers once watched their flight before crossing the pass; when the owls stayed low, experienced riders stayed home.",
        ["frost-kingdom-07"] = "The Glacier Gate was carved through blue ice before the present kingdom had a name. Each winter the arch thickens, each summer it thins, yet the opening has remained exactly wide enough for two riders to pass side by side.",

        ["clockwork-01"] = "The first lesson in every clockmaker's workshop is that a single gear is useless by itself. The second is that the wrong single gear can stop an entire machine. Apprentices usually remember the second lesson longer.",
        ["clockwork-02"] = "Copper wire runs through the workshop walls like veins. The oldest coils have darkened with age but still carry current better than their replacements, leading the master engineers to label them simply: 'Do not improve.'",
        ["clockwork-03"] = "This pocket watch keeps excellent time except at 3:17 in the morning, when it pauses for exactly nine seconds. Three generations of watchmakers have dismantled it looking for the cause and returned every piece without an answer.",
        ["clockwork-04"] = "Steam valves are designed to release pressure before something more expensive does. Workshop crews paint the handles bright brass and teach apprentices to respect any machine that begins hissing before its operator does.",
        ["clockwork-05"] = "Automaton Seven was built to carry tools, not make decisions. During a factory fire it ignored its route, opened three locked doors, and led six workers outside. Its designer never managed to reproduce the behavior.",
        ["clockwork-06"] = "The clockwork bird was a gift meant to sing one mechanical melody. After years near an open window it began copying real birds instead, adding notes its tiny brass cylinder had never been engraved to play.",
        ["clockwork-07"] = "The first passenger airship was mocked as a floating boiler wrapped in silk. It crossed the capital anyway. By the time it returned, every rooftop was crowded with people pretending they had supported the idea from the beginning.",

        ["sakura-garden-01"] = "A single fallen petal can travel across the pond without sinking for several minutes. Gardeners watch where it finally stops; an old custom says the place a petal chooses is where the next season's first blossom will open.",
        ["sakura-garden-02"] = "Tea in the garden is poured slowly enough for the steam to disappear before anyone drinks. The painted cup pictured here has been repaired twice with gold, because replacing something beloved is considered poorer manners than admitting it was broken.",
        ["sakura-garden-03"] = "Paper fans are carried for summer heat, ceremony, dance, and occasionally privacy. This one bears cranes beneath the folds; when fully opened, their wings form a continuous scene that cannot be seen from any single closed panel.",
        ["sakura-garden-04"] = "The stone lantern is lit at dusk even during festivals bright enough to make it unnecessary. Its small flame marks the oldest path through the garden, a path designed for walking slowly rather than arriving quickly.",
        ["sakura-garden-05"] = "The koi pond has no fence because the fish have nowhere they would rather be. Several are older than the gardeners who feed them. The largest red koi answers to three different names and ignores all of them equally.",
        ["sakura-garden-06"] = "The red bridge is arched higher than the stream requires. Its builder wanted anyone crossing it to pause at the top, where mountains, blossoms, water, and temple roofs briefly fit into the same view.",
        ["sakura-garden-07"] = "A pair of cranes returns to the marsh beyond the garden every spring. Their mirrored courtship dance inspired generations of painters and dancers, none of whom have quite captured how unexpectedly awkward the first few steps are.",

        ["pirate-cove-01"] = "The rope is older than the pier it lies on. Salt has stiffened it, storms have frayed it, and no sensible sailor would trust it with a ship. The harbor crew keeps it anyway because it once held during a hurricane when newer lines snapped.",
        ["pirate-cove-02"] = "This silver coin was recovered from black sand below the old fort. One side bears a king whose name has been scratched away; the other carries a tiny skull added much later by someone with a knife and plenty of confidence.",
        ["pirate-cove-03"] = "The torn map shows three islands, two reefs, and half of an X. Treasure hunters have spent years arguing about the missing half. Tavern regulars suspect the original cartographer tore it deliberately just to enjoy the argument.",
        ["pirate-cove-04"] = "A good compass points north. This one points north everywhere except inside the cove, where the needle drifts several degrees toward the cliffs. Captains who trust it anyway tend to discover why the rocks have so many names.",
        ["pirate-cove-05"] = "The scarlet parrot has outlived at least two captains and possibly three ships. It knows several hundred words, most of them unsuitable for polite company, and becomes mysteriously quiet whenever customs officers are nearby.",
        ["pirate-cove-06"] = "The captain's hat is more ornament than protection: black felt, red feathers, gold braid, and a jewel far too large to be practical. Its owner claimed a captain should be identifiable during mutiny, celebration, and cannon smoke alike.",
        ["pirate-cove-07"] = "Sailors recognize the Black Sail before they recognize the ship beneath it. The cloth was dyed with a recipe the crew refused to share and remains dark even against moonlight, making the vessel seem like a moving hole in the horizon.",

        ["royal-court-01"] = "The royal seal turns paper into command with a single press of red wax. Palace clerks keep several molds under lock, but old documents reveal tiny imperfections that make each ruler's seal as recognizable as a signature.",
        ["royal-court-02"] = "This silver spoon belonged to the smallest place setting at state dinners: the seat reserved for an unexpected guest. Court tradition demanded that one place remain available, because diplomacy has an inconvenient habit of arriving without notice.",
        ["royal-court-03"] = "Court masks hide less than people imagine. Feather choice, gemstone color, and even which side is tied first can announce allegiance before anyone speaks. The most skilled courtiers learned to wear misleading masks on purpose.",
        ["royal-court-04"] = "Purple silk ribbons hang from balconies during royal celebrations. After one famous victory, citizens cut them down by the hundreds and tied pieces around their wrists. The palace eventually stopped asking for the ribbons back.",
        ["royal-court-05"] = "The knight of the violet banner rides at the front of ceremonial processions and at the rear of real retreats. The position sounds less glorious in songs, but veterans consider it the better measure of courage.",
        ["royal-court-06"] = "The Rose Throne was commissioned for a queen who disliked cold marble halls. Its carved roses are surrounded by living vines tended through hidden channels in the stone, keeping the seat framed in bloom even during winter.",
        ["royal-court-07"] = "The crown jewels are displayed together only once each year. Jewelers spend the previous night checking every clasp and setting. The old master of the vault says the gems are less dangerous than the people who believe owning them would change who they are.",
    };

    static CollectionLore()
    {
        CollectionCard[] expected = CollectionCatalog.Cards
            .Where(card => !CollectionCatalog.Theme(card).UltraRare && card.Rarity is CollectionRarity.Common or CollectionRarity.Uncommon)
            .ToArray();
        if (Entries.Count != expected.Length || expected.Any(card => !Entries.ContainsKey(card.Id)))
            throw new InvalidDataException("Common and Uncommon collection lore must cover every regular-theme card exactly once.");
        if (Entries.Keys.Any(id => CollectionCatalog.Find(id) is not { Rarity: CollectionRarity.Common or CollectionRarity.Uncommon }))
            throw new InvalidDataException("The grounded lore pass must not author Rare, Epic or Legendary cards yet.");
    }

    internal static int AuthoredCount => Entries.Count;
    internal static bool Has(CollectionCard card) => Entries.ContainsKey(card.Id);
    internal static string BodyFor(CollectionCard card)
    {
        if (Entries.TryGetValue(card.Id, out string? body))
            return Localization.Language == UiLanguagePreference.German
                ? Localization.T($"Album.Lore.{card.Id}")
                : body;
        return Localization.Language == UiLanguagePreference.German
            ? "Dieser Archiveintrag ist noch versiegelt. Geschichten für seltene, epische und legendäre Karten folgen später."
            : "This archive entry remains sealed. Rare, Epic and Legendary stories will be catalogued in a later lore pass.";
    }
}
