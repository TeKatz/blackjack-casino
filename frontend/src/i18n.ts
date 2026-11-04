/** Visible playtest UI translations. C# owns language preference and all rules.
 * German rendering of saved English narration is cosmetic; unknown phrases
 * remain unchanged rather than guessing facts or changing a run command.
 */
export type ManorLanguage = 'en' | 'de';

const en = {
  club: 'THE MIDNIGHT CLUB',
  back: 'Back to lobby', ready: 'C# ENGINE · CONNECTED',
  connecting: 'CONNECTING TO CASINO', preview: 'BROWSER DESIGN PREVIEW',
  heroEyebrow: 'THE HOUSE REMEMBERS EVERY CHOICE',
  heroTitle: 'The house knows your name.',
  heroText: 'Every spin, payout and room choice comes from the original C# game engine. Your balance and ongoing features are committed to the same saved game as classic Manor.',
  playtest: 'MIGRATION / PHASE 3 PLAYTEST',
  hauntedFive: 'THE HAUNTED FIVE', paylinesHeader: '20 FIXED PAYLINES · 5 × 3',
  lastFree: 'LAST HAUNTED NIGHT FREE SPIN', lastPaid: 'LAST PAID SPIN',
  noSpin: 'NO STORED SPIN · PRESENTATION STAND-IN',
  paidLines: 'PAID LINES', moons: 'MOONS', doors: 'DOORS',
  viewWinLines: 'View {0} settled winning {1}', lineSingular: 'line', linePlural: 'lines',
  line: 'LINE', matched: 'MATCHED',
  stake: 'BET · PLAY MONEY', freeSpin: 'PLAY FREE SPIN · {0} LEFT',
  saving: 'SAVING RESULT…', spin: 'SPIN THE REELS',
  runHint: 'The Manor is open · choose your path below',
  freeHint: 'No extra wager is charged for free spins',
  cycleHint: 'Resolve the current feature before a new wager',
  committedHint: 'C# determines and saves the result before the reels display it',
  invalidStake: 'Choose a whole-dollar bet between $1 and $1,000.',
  timeout: 'The reply is taking longer than expected. Reading the committed state; do not repeat your action.',
  runEyebrow: 'INTERACTIVE BONUS', runTitle: 'Escape the Manor', turn: 'TURN',
  potential: 'Potential', checkpoint: 'Checkpoint', exit: 'Exit',
  keyFound: 'KEY FOUND', noKey: 'NO KEY',
  charmReady: 'CHARM READY', noCharm: 'NO CHARM',
  routeInstruction: 'The bright routes and buttons are legal choices from the C# engine. Entering a room advances the ghost. Collected rewards and checkpoints are committed with your choice. A locked shortcut requires a key.',
  candle: 'Use candle', follow: 'Follow recommended route · 1 step',
  cashout: 'Cash out now', resolve: 'Auto-resolve entire run',
  confirmGroup: 'Confirm automatic resolution', confirmQuestion: 'Finish the whole run automatically?',
  confirm: 'Confirm', cancel: 'Cancel',
  account: 'YOUR MANOR ACCOUNT', chandelier: 'Under the chandelier',
  balance: 'Balance', lastStake: 'Last stake',
  lastReturn: 'Last reel return', cycleReturn: 'Current cycle return',
  watching: 'THE HOUSE IS WATCHING',
  runDescription: 'Choose your rooms, reveal the ghost with a candle, bank a checkpoint or leave with your current reward.',
  freeDescription: 'Continue saved free spins at your own pace. The original C# payout table is used.',
  cycleDescription: 'A committed bonus is still being resolved.',
  idleDescription: 'The C# engine controls all reels, payouts and bonus triggers.',
  settings: 'DISPLAY SETTINGS', cabinetAnimation: 'Cabinet animation', reduced: 'Reduce motion',
  soundGroup: 'Local Manor sound', sound: 'MANOR SOUND',
  sfx: 'Sound effects', ambience: 'Quiet atmospheric hum', volume: 'Sound volume',
  soundHint: 'Offline audio starts only after your first action. Muting never affects game results or your saved state.',
  diagnosticsToggle: 'Show render diagnostics', speed: 'Reel motion speed',
  normal: 'Normal', fast: 'Fast', turbo: 'Turbo',
  motionHint: 'Reduced motion skips reel animation; every speed displays the same saved C# outcome.',
  refresh: 'Refresh saved state', 
  diagnostics: 'RENDER DIAGNOSTICS · LOCAL ONLY',
  tickerPaused: 'Ticker paused (reduced motion / animation off)',
  sample: 'Sampling Pixi ticker frames...',
  fpsObserved: 'Observed ~{0} fps · mean {1} ms · p95 {2} ms · {3}/{4} frames >16.7 ms',
  dpiObserved: '{0} × {1} CSS px · {2}× backing ratio',
  diagnosticsHint: 'Measures JavaScript ticker intervals, not GPU time or a hardware benchmark.',
  fatal: 'The save outcome may already be committed. Close and reopen the modern Manor to recover it. Do not place another wager in this window.',
  footer: 'PLAY-MONEY ONLY · C# SETTLEMENT · DURABLE SAVE',
  revision: 'CURRENT REVISION {0}', browserFooter: 'BROWSER DESIGN PREVIEW · NO GAMEPLAY',
  language: 'Language', languageHint: 'This view follows your casino language; switching here is temporary.',
  help: 'Rules & payout table', helpTitle: 'Manor rules and paytable',
  helpClose: 'Close rules',
  helpVersion: 'Displayed C# payout table: version {0}.',
  helpLegacy: 'This saved feature uses a previous payout table until it finishes.',
  helpCurrent: 'These awards apply to the current cycle or your next paid spin.',
  helpStake: 'Paid spin stake: $1–$1,000 (whole play-money dollars). 20 fixed lines share the total stake equally.',
  helpWin: 'Pay the best matching regular symbol per line, left to right, with 3/4/5 consecutive matches. Wild substitutes regular symbols; Moon and Door do not pay on lines. Free-spin line wins are doubled.',
  helpCredits: 'Line credits (multiplied by total stake ÷ 20)',
  helpSymbol: 'Symbol', helpThree: '3 matches', helpFour: '4 matches', helpFive: '5 matches',
  helpSpecial: 'Wild, Moon, Door', helpSpecialText: 'Mirror Wild replaces regular symbols only. On paid spins 3/4/5 visible Moons award 8/12/20 free spins; 2/3/4/5 Doors open Escape the Manor with a 25×/250×/500×/1000× exit target. These features do not retrigger in free spins.',
  helpLines: 'All 20 lines', helpRows: '0 = top, 1 = middle, 2 = bottom, from left reel to right.',
  helpRun: 'Escape the Manor', helpRunText: 'A route is a real decision: entering a room moves the ghost. Checkpoints protect the banked reward if caught, a candle reveals the ghost for one turn without advancing time, and a charm can repel a catch. Cash out to secure the current multiplier; reaching the Moonlit Gate secures the exit multiplier. At 24 turns the checkpoint is paid.',
  helpRisk: 'A recommended route is a strategy, not a guaranteed win. Do not assume every manual route has the same expected payout. All rewards are play money.',
  mapLegend: 'Map legend', mapPlayer: 'Your location', mapAvailable: 'Available move',
  mapVisited: 'Explored', mapBlocked: 'Requires key',
  chooseRoom: 'CHOOSE YOUR NEXT ROOM', passageSingular: 'passage', passagePlural: 'passages',
  accessible: 'accessible', explored: 'ALREADY EXPLORED', unexplored: 'NOT YET EXPLORED',
  enter: 'ENTER', moveTo: 'Move to {0}', noAdjacent: 'No adjacent room is available. Review your current run options.',
  mapRegion: 'Scrollable Manor floorplan (use horizontal scroll to inspect rooms)',
  floorplan: 'MIDNIGHT MANOR · FLOORPLAN',
  currentRoom: 'current room', nextMove: 'legal next move', needsKey: 'needs the key',
  alreadyVisited: 'already visited', notReachable: 'not reachable now',
  here: 'YOU ARE HERE', mapPrivacy: 'The map follows your current room. Use the destination buttons above when the floorplan is wider than your window. Only passages and moves authorized by the Manor engine are shown. The ghost remains hidden unless your candle reveals it.',
  ghostRevealed: 'CANDLE REVEAL', ghostDanger: 'GHOST NEARBY',
  ghostApproaching: 'APPROACHING', ghostDistant: 'DISTANT FOOTSTEPS',
  ghostUnknown: 'UNSEEN PRESENCE',
  momentWin: 'WIN', momentBig: 'BIG WIN', momentEpic: 'LEGENDARY WIN',
  momentDoor: 'THE MANOR OPENS', momentMoon: 'HAUNTED NIGHT',
  momentRun: 'A NEW CHAMBER', momentCandle: 'CANDLE LIT',
  momentCashout: 'REWARD SECURED', momentRunEnd: 'THE RUN ENDS',
  winDetail: '{0} · {1} winning {2}',
  doorDetail: 'Escape the Manor has opened',
  moonDetail: 'Haunted Night free spins unlocked',
  comboDetail: 'Haunted Night free spins also unlocked',
  savedSpins: '{0} saved free spins',
  castleFooter: 'EST. 1893 · THE HOUSE HOLDS ITS SECRETS',
} as const;

export type TextKey = keyof typeof en;
const de: Record<TextKey, string> = {
  club: 'THE MIDNIGHT CLUB',
  back: 'Zurück zur Lobby', ready: 'C#-SPIELKERN · VERBUNDEN',
  connecting: 'VERBINDUNG ZUM CASINO', preview: 'BROWSER-DESIGNVORSCHAU',
  heroEyebrow: 'DAS HAUS VERGISST KEINE ENTSCHEIDUNG',
  heroTitle: 'Das Haus kennt deinen Namen.',
  heroText: 'Jeder Dreh, jede Auszahlung und jede Raumwahl kommt aus der bestehenden C#-Spielengine. Dein Guthaben und alle laufenden Features werden im selben Spielstand wie beim klassischen Manor gesichert.',
  playtest: 'MIGRATION / PHASE-3-SPIELTEST',
  hauntedFive: 'DIE FÜNF WALZEN', paylinesHeader: '20 FESTE GEWINNLINIEN · 5 × 3',
  lastFree: 'LETZTER HAUNTED-NIGHT-FREISPIN', lastPaid: 'LETZTER EINSATZ-DREH',
  noSpin: 'NOCH KEIN DREH · DARSTELLUNGSVORSCHAU',
  paidLines: 'GEWINNLINIEN', moons: 'MONDE', doors: 'TÜREN',
  viewWinLines: '{0} abgeschlossene Gewinn{1} anzeigen', lineSingular: 'linie', linePlural: 'linien',
  line: 'LINIE', matched: 'TREFFER',
  stake: 'EINSATZ · SPIELGELD', freeSpin: 'FREISPIN SPIELEN · NOCH {0}',
  saving: 'ERGEBNIS WIRD GESPEICHERT …', spin: 'WALZEN DREHEN',
  runHint: 'Das Manor ist geöffnet · wähle unten deinen Weg',
  freeHint: 'Für Freispiele wird kein weiterer Einsatz abgezogen',
  cycleHint: 'Beende zuerst das laufende Feature',
  committedHint: 'C# bestimmt und speichert jedes Ergebnis vor der Darstellung',
  invalidStake: 'Wähle einen ganzzahligen Spieleinsatz zwischen $1 und $1.000.',
  timeout: 'Die Antwort dauert länger. Der gespeicherte Zustand wird abgefragt; bitte die Aktion nicht wiederholen.',
  runEyebrow: 'INTERAKTIVER BONUS', runTitle: 'Escape the Manor', turn: 'ZUG',
  potential: 'Aktuell', checkpoint: 'Gesichert', exit: 'Ausgang',
  keyFound: 'SCHLÜSSEL GEFUNDEN', noKey: 'KEIN SCHLÜSSEL',
  charmReady: 'SCHUTZZAUBER BEREIT', noCharm: 'KEIN SCHUTZZAUBER',
  routeInstruction: 'Die hervorgehobenen Wege und Schaltflächen sind von C# freigegebene Optionen. Jeder Raumzug lässt den Geist weiterziehen. Belohnungen und Kontrollpunkte werden sofort gespeichert. Gesperrte Abkürzungen erfordern einen Schlüssel.',
  candle: 'Kerze verwenden', follow: 'Empfohlene Route · 1 Schritt',
  cashout: 'Jetzt auszahlen', resolve: 'Gesamten Lauf automatisch beenden',
  confirmGroup: 'Automatischen Abschluss bestätigen',
  confirmQuestion: 'Soll der gesamte Lauf automatisch abgeschlossen werden?',
  confirm: 'Bestätigen', cancel: 'Abbrechen',
  account: 'DEIN MANOR-KONTO', chandelier: 'Unter dem Kronleuchter',
  balance: 'Guthaben', lastStake: 'Letzter Einsatz',
  lastReturn: 'Letzter Walzengewinn', cycleReturn: 'Ertrag des laufenden Zyklus',
  watching: 'DAS HAUS BEOBACHTET DICH',
  runDescription: 'Wähle deine Räume, entdecke den Geist mit der Kerze, sichere einen Kontrollpunkt oder verlasse das Haus mit deinem aktuellen Gewinn.',
  freeDescription: 'Setze deine gespeicherten Freispiele in deinem Tempo fort. Es gilt dieselbe C#-Auszahlungstabelle.',
  cycleDescription: 'Ein gespeichertes Bonus-Feature wird noch abgewickelt.',
  idleDescription: 'C# kontrolliert Walzen, Auszahlungen und alle Bonusauslösungen.',
  settings: 'ANZEIGEEINSTELLUNGEN', cabinetAnimation: 'Automatenanimation',
  reduced: 'Bewegung reduzieren',
  soundGroup: 'Lokaler Manor-Sound', sound: 'MANOR-KLANG',
  sfx: 'Soundeffekte', ambience: 'Leise Schlossatmosphäre', volume: 'Lautstärke',
  soundHint: 'Offline-Audio beginnt erst nach einer Spieleraktion. Stummschaltung beeinflusst weder Ergebnis noch Spielstand.',
  diagnosticsToggle: 'Renderer-Diagnose anzeigen', speed: 'Walzengeschwindigkeit',
  normal: 'Normal', fast: 'Schnell', turbo: 'Turbo',
  motionHint: 'Weniger Bewegung überspringt Walzenanimationen; das gespeicherte C#-Ergebnis bleibt unverändert.',
  refresh: 'Spielstand aktualisieren',
  diagnostics: 'RENDERER-DIAGNOSE · NUR LOKAL',
  tickerPaused: 'Ticker angehalten (Animation / Bewegung deaktiviert)',
  sample: 'Pixi-Framezeiten werden gemessen …',
  fpsObserved: 'Gemessen ~{0} FPS · Mittelwert {1} ms · P95 {2} ms · {3}/{4} Frames >16,7 ms',
  dpiObserved: '{0} × {1} CSS-Pixel · Pixelverhältnis {2}×',
  diagnosticsHint: 'Misst JavaScript-Tickerintervalle, keine GPU-Zeit oder Hardware-Benchmarkwerte.',
  fatal: 'Das Ergebnis könnte bereits gespeichert sein. Schließe und öffne das moderne Manor, um den Spielstand sicher wiederherzustellen. Platziere hier keinen weiteren Einsatz.',
  footer: 'NUR SPIELGELD · C#-ABRECHNUNG · GESICHERTER SPIELSTAND',
  revision: 'AKTUELLE REVISION {0}', browserFooter: 'BROWSER-VORSCHAU · KEIN SPIELBETRIEB',
  language: 'Sprache', languageHint: 'Diese Ansicht übernimmt die Casino-Sprache; eine Änderung hier gilt nur vorübergehend.',
  help: 'Regeln & Gewinnübersicht', helpTitle: 'Manor-Regeln und Auszahlungstabelle',
  helpClose: 'Regeln schließen',
  helpVersion: 'Angezeigte C#-Tabelle: Version {0}.',
  helpLegacy: 'Das gespeicherte Feature behält bis zum Abschluss seine ältere Auszahlungstabelle.',
  helpCurrent: 'Diese Werte gelten für den laufenden Zyklus bzw. den nächsten bezahlten Dreh.',
  helpStake: 'Einsatz pro bezahltem Dreh: $1–$1.000, nur ganze Spieldollar. Die 20 festen Linien teilen sich den Gesamteinsatz gleichmäßig.',
  helpWin: 'Von links nach rechts zahlen je Linie 3/4/5 gleiche reguläre Symbole, mit Wild-Ersatz. Es zählt pro Linie nur der höchste Gewinn. Mond und Tür zahlen nicht auf Linien. Liniengewinne aus Freispielen werden verdoppelt.',
  helpCredits: 'Linien-Credits (multipliziert mit Gesamteinsatz ÷ 20)',
  helpSymbol: 'Symbol', helpThree: '3 Treffer', helpFour: '4 Treffer', helpFive: '5 Treffer',
  helpSpecial: 'Wild, Mond, Tür',
  helpSpecialText: 'Der Spukspiegel (Wild) ersetzt nur reguläre Symbole. Bei bezahlten Drehs bringen 3/4/5 Monde 8/12/20 Freispiele; 2/3/4/5 Türen öffnen das Manor mit einem Ausgangsziel von 25×/250×/500×/1000×. In Freispielen werden diese Features nicht erneut ausgelöst.',
  helpLines: 'Alle 20 Linien', helpRows: '0 = oben, 1 = Mitte, 2 = unten; von der linken zur rechten Walze.',
  helpRun: 'Escape the Manor',
  helpRunText: 'Jede Route ist eine echte Entscheidung: Ein Raumwechsel bewegt den Geist. Kontrollpunkte sichern deinen Gewinn bei Gefangennahme, die Kerze zeigt den Geist einen Zug lang ohne Zeitverlust, und der Schutzzauber kann ihn zurückdrängen. Auszahlen sichert den aktuellen Multiplikator; das Mondtor sichert das Ausgangsziel. Nach 24 Zügen wird der Kontrollpunkt ausgezahlt.',
  helpRisk: 'Die empfohlene Route ist eine Strategie, keine Gewinngarantie. Verschiedene manuelle Wege können unterschiedliche Erwartungen haben. Alle Gewinne bestehen aus Spielgeld.',
  mapLegend: 'Kartenlegende', mapPlayer: 'Deine Position', mapAvailable: 'Begehbarer Raum',
  mapVisited: 'Erkundet', mapBlocked: 'Schlüssel nötig',
  chooseRoom: 'NÄCHSTEN RAUM WÄHLEN', passageSingular: 'Weg', passagePlural: 'Wege',
  accessible: 'erreichbar', explored: 'BEREITS ERKUNDET', unexplored: 'NOCH UNERKUNDET',
  enter: 'BETRETEN', moveTo: '{0} betreten', noAdjacent: 'Kein angrenzender Raum erreichbar. Prüfe deine übrigen Optionen.',
  mapRegion: 'Verschiebbare Manor-Karte (horizontal scrollen, um Räume zu sehen)',
  floorplan: 'MIDNIGHT MANOR · GRUNDRISS',
  currentRoom: 'aktueller Raum', nextMove: 'möglicher nächster Raum', needsKey: 'Schlüssel erforderlich',
  alreadyVisited: 'bereits besucht', notReachable: 'derzeit nicht erreichbar',
  here: 'DU BIST HIER', mapPrivacy: 'Die Karte folgt deinem aktuellen Raum. Nutze bei schmalem Fenster die Raumwahl oberhalb. Angezeigt werden ausschließlich Wege und Züge der C#-Engine. Der Geist bleibt verborgen, bis die Kerze ihn enthüllt.',
  ghostRevealed: 'KERZENENTHÜLLUNG', ghostDanger: 'GEIST IN DER NÄHE',
  ghostApproaching: 'NÄHERKOMMENDER GEIST', ghostDistant: 'FERNE SCHRITTE',
  ghostUnknown: 'UNBEKANNTE PRÄSENZ',
  momentWin: 'GEWINN', momentBig: 'GROSSER GEWINN', momentEpic: 'LEGENDÄRER GEWINN',
  momentDoor: 'DAS MANOR ÖFFNET SICH', momentMoon: 'HAUNTED NIGHT',
  momentRun: 'EINE NEUE KAMMER', momentCandle: 'KERZE ENTZÜNDET',
  momentCashout: 'GEWINN GESICHERT', momentRunEnd: 'DER LAUF ENDET',
  winDetail: '{0} · {1} gewinnende {2}', doorDetail: 'Escape the Manor ist geöffnet',
  moonDetail: 'Haunted-Night-Freispiele freigeschaltet',
  comboDetail: 'Haunted-Night-Freispiele wurden ebenfalls freigeschaltet',
  savedSpins: '{0} gespeicherte Freispiele',
  castleFooter: 'SEIT 1893 · DAS HAUS BEWAHRT SEINE GEHEIMNISSE',
};

export const localizedKeys = () => Object.keys(en) as TextKey[];
export const germanKeys = () => Object.keys(de) as TextKey[];

export function t(language: ManorLanguage, key: TextKey, ...values: (string | number)[]): string {
  const template: string = language === 'de' ? de[key] : en[key];
  return template.replace(/\{(\d+)\}/g, (_, n: string) => String(values[Number(n)] ?? ''));
}

const rooms: Record<string, string> = {
  'Entrance Hall': 'Eingangshalle', "Servants' Corridor": 'Dienstbotengang',
  'Candle Landing': 'Kerzenabsatz', 'Grand Staircase': 'Große Treppe',
  Library: 'Bibliothek', 'Reading Room': 'Lesesaal', Study: 'Studierzimmer',
  Ballroom: 'Ballsaal', 'Music Room': 'Musikzimmer',
  'Winter Garden': 'Wintergarten', Attic: 'Dachboden',
  'Forbidden Wing': 'Verbotener Flügel', Crypt: 'Krypta',
  'Moonlit Landing': 'Mondlichtabsatz', 'Secret Passage': 'Geheimgang',
  Courtyard: 'Innenhof', 'Moonlit Gate': 'Mondtor',
  'Distant Vestibule': 'Ferne Vorhalle', 'Rear Foyer': 'Hinteres Foyer',
};
const areas: Record<string, string> = {
  Entrance: 'Eingang', Library: 'Bibliothek', Ballroom: 'Ballsaal',
  Corridor: 'Gang', Attic: 'Dachboden', Crypt: 'Verbotener Flügel',
};
export function localizeRoom(value: string, language: ManorLanguage): string {
  return language === 'de' ? (rooms[value] ?? value) : value;
}
export function localizeArea(value: string, language: ManorLanguage): string {
  return language === 'de' ? (areas[value] ?? value) : value;
}
export function localizeFeature(value: string, language: ManorLanguage): string {
  if (language === 'en') return value;
  if (value === 'The house awaits') return 'Das Haus wartet';
  if (value === 'Saved cycle in progress') return 'Gespeicherter Spielzyklus läuft';
  if (value === 'Escape the Manor · choose your path') return 'Escape the Manor · Wähle deinen Weg';
  if (value === 'Browser design preview · no wagers executed') return 'Browser-Vorschau · keine Einsätze möglich';
  const free = /^Haunted Night · (\d+) free spins saved$/.exec(value);
  if (free) return 'Haunted Night · ' + free[1] + ' Freispiele gespeichert';
  const ending = /^Escape the Manor · (Escaped|Caught|CashedOut|Exhausted)$/.exec(value);
  if (ending) {
    const outcomes: Record<string,string> = {
      Escaped: 'Entkommen', Caught: 'Gefangen',
      CashedOut: 'Ausgezahlt', Exhausted: 'Zuglimit erreicht',
    };
    return 'Escape the Manor · ' + outcomes[ending[1]];
  }
  return value;
}
const directions: Record<string, string> = {
  west: 'Westen', east: 'Osten', north: 'Norden', south: 'Süden', nearby: 'der Nähe',
};
/** Only translates known C# narrative templates, keeping unknown strings intact. */
export function localizeRunText(value: string, language: ManorLanguage): string {
  if (language === 'en') return value;
  const revealed = /^Candle reveals: (.+)\.$/.exec(value);
  if (revealed) return 'Die Kerze enthüllt: ' + localizeRoom(revealed[1], language) + '.';
  const close = /^Cold air creeps from the (west|east|north|south|nearby)\. The ghost is CLOSE \(within two rooms\)\.$/.exec(value);
  if (close) return 'Kälte strömt aus ' + (close[1] === 'nearby' ? 'der Nähe' : 'dem ' + directions[close[1]]) +
    '. Der Geist ist NAH (höchstens zwei Räume entfernt).';
  const approaching = /^A door slams to the (west|east|north|south|nearby)\. The ghost is approaching\.$/.exec(value);
  if (approaching) return 'Eine Tür schlägt ' + (approaching[1] === 'nearby' ? 'in der Nähe' : 'im ' + directions[approaching[1]]) +
    ' zu. Der Geist kommt näher.';
  if (value === 'Distant footsteps echo through the Manor. The ghost is far away.')
    return 'Ferne Schritte hallen durch das Manor. Der Geist ist weit entfernt.';
  if (value === 'The entrance seals behind you. The Moonlit Gate lies to the east.')
    return 'Der Eingang verriegelt sich hinter dir. Das Mondtor liegt im Osten.';
  if (value === 'The candle reveals her position through the walls for one turn.')
    return 'Die Kerze zeigt ihre Position für einen Zug durch die Wände.';
  if (value === 'The night closes in. Your checkpoint is secured.')
    return 'Die Nacht holt dich ein. Dein Kontrollpunkt ist gesichert.';
  const entering = /^You enter (.+?)\.(.*)$/.exec(value);
  if (entering) {
    let suffix = entering[2].replace('Checkpoint secured.', 'Kontrollpunkt gesichert.')
      .replace('Key found.', 'Schlüssel gefunden.')
      .replace('Candle found.', 'Kerze gefunden.')
      .replace('Holy charm found.', 'Schutzzauber gefunden.')
      .replace('Your charm repels her one room and buys a turn.', 'Dein Schutzzauber stößt sie einen Raum zurück und gewinnt einen Zug.');
    return 'Du betrittst ' + localizeRoom(entering[1], language) + '.' + suffix;
  }
  const reached = /^You reached the Moonlit Gate\. (.+) secured\.$/.exec(value);
  if (reached) return 'Du hast das Mondtor erreicht. ' + reached[1] + ' gesichert.';
  const cashout = /^You leave the Manor with (.+)\.$/.exec(value);
  if (cashout) return 'Du verlässt das Manor mit ' + cashout[1] + '.';
  const caught = /^The ghost catches you\. Checkpoint pays (.+)\.$/.exec(value);
  if (caught) return 'Der Geist erwischt dich. Der Kontrollpunkt zahlt ' + caught[1] + '.';
  return value;
}
