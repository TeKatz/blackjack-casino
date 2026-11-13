/** Visible playtest UI translations. C# owns language preference and all rules.
 * German rendering of saved English narration is cosmetic; unknown phrases
 * remain unchanged rather than guessing facts or changing a run command.
 */
export type ManorLanguage = 'en' | 'de';

const en = {
  club: 'THE MIDNIGHT CLUB',
  back: 'Back to lobby', ready: 'THE MANOR AWAKENS',
  connecting: 'CONNECTING TO CASINO', preview: 'BROWSER DESIGN PREVIEW',
  heroEyebrow: 'THE HOUSE REMEMBERS EVERY CHOICE',
  heroTitle: 'The house knows your name.',
  heroText: 'Every turn of the reels stirs the house. Your fortune and every open door will be waiting when you return.',
  playtest: 'WELCOME TO MIDNIGHT MANOR',
  hauntedFive: 'THE HAUNTED FIVE', paylinesHeader: '20 FIXED PAYLINES · 5 × 3',
  lastFree: 'LAST HAUNTED NIGHT FREE SPIN', lastPaid: 'LAST PAID SPIN',
  noSpin: 'THE REELS ARE WAITING',
  paidLines: 'PAID LINES', moons: 'MOONS', doors: 'DOORS',
  viewWinLines: 'View {0} settled winning {1}', lineSingular: 'line', linePlural: 'lines',
  line: 'LINE', matched: 'MATCHED',
  stake: 'BET · PLAY MONEY', freeSpin: 'PLAY FREE SPIN · {0} LEFT',
  quickBet: 'QUICK BET', autoLabel: 'AUTO SPINS', autoRemaining: 'AUTO · {0} LEFT',
  autoCount: 'Start {0} automatic spins', autoStop: 'STOP AUTO',
  notEnough: 'Not enough play money for that wager.',
  saving: 'SAVING RESULT…', spin: 'SPIN THE REELS',
  spinRolling: 'REELS SPINNING', spinSealed: 'RESULT HIDDEN UNTIL THE LAST REEL STOPS',
  runHint: 'The Manor is open · choose your path below',
  freeHint: 'No extra wager is charged for free spins',
  cycleHint: 'Resolve the current feature before a new wager',
  committedHint: 'Follow the flicker of the candlelight',
  invalidStake: 'Choose a whole-dollar bet between $1 and $5,000.',
  timeout: 'The reply is taking longer than expected. Reading the committed state; do not repeat your action.',
  runEyebrow: 'INTERACTIVE BONUS', runTitle: 'Escape the Manor', turn: 'TURN',
  potential: 'Potential', checkpoint: 'Checkpoint', exit: 'Exit',
  keyFound: 'KEY FOUND', noKey: 'NO KEY',
  charmReady: 'CHARM READY', noCharm: 'NO CHARM',
  routeInstruction: 'Every doorway leads somewhere. Entering a room brings the ghost closer. Secured rewards stay yours; a locked shortcut requires a key.',
  candle: 'Use candle', follow: 'Follow recommended route · 1 step',
  cashout: 'Cash out now', resolve: 'Auto-resolve entire run',
  confirmGroup: 'Confirm automatic resolution', confirmQuestion: 'Finish the whole run automatically?',
  confirm: 'Confirm', cancel: 'Cancel',
  account: 'YOUR MANOR ACCOUNT', chandelier: 'Under the chandelier',
  balance: 'Balance', lastStake: 'Last stake',
  lastReturn: 'Last reel return', cycleReturn: 'Current cycle return',
  watching: 'THE HOUSE IS WATCHING',
  runDescription: 'Choose your rooms, reveal the ghost with a candle, bank a checkpoint or leave with your current reward.',
  freeDescription: 'The moon has granted another turn. Your free spins await.',
  cycleDescription: 'A committed bonus is still being resolved.',
  idleDescription: 'A faint draught crosses the hall. The reels wait beneath the chandelier.',
  settings: 'DISPLAY SETTINGS', cabinetAnimation: 'Cabinet animation', reduced: 'Reduce motion',
  godotSettingsHeading: '3D BONUS ADVENTURE',
  godotTestButton: 'Practise bonus · no wager',
  godotEnableLive: '3D bonus enabled',
  godotSettingsHint: 'Real bonus wins open the 3D Manor automatically. Practice uses the exact ghost and item rules, without a wager or payout.',
  godotDemoTag: 'PRACTICE · NO WAGER',
  godotLiveTag: 'ESCAPE THE MANOR',
  godotDemoProgress: 'PRACTICE · TURN {0}',
  godotArea: 'Interactive 3D Manor bonus',
  godotLoading: 'Opening the Manor in 3D…',
  godotLoadingDetail: 'Loading the local 3D scene. Godot runs only during the bonus to keep the reels smooth.',
  godotUnavailable: 'The 3D bonus could not start.',
  godotUnavailableDetail: '3D rendering failed. Retry Godot, or safely finish this run using the C# game rules below.',
  godotHint: 'Choose a door or open the floorplan to plan your next move',
  godotReturn: 'Back to the slot',
  godotRetry: 'Retry 3D scene',
  godotNoPayout: 'Practice mode · winnings are never paid' ,
  godotTurnOf: 'TURN {0} / {1}',
  godotRewardStatus: 'Bonus multipliers and secured checkpoint',
  godotExitTarget: 'Moonlit Gate',
  godotDemoThreat: 'PRACTICE',
  godotDemoGhostHint: 'The ghost follows the same rules as a real bonus.',
  godotEventTitle: 'THE HOUSE RESPONDS',
  godotMapHeading: 'MANOR FLOORPLAN',
  godotMapExpand: 'Enlarge floorplan',
  godotMapComplete: 'ALL ROOMS REVEALED',
  godotMapClose: 'Close map',
  godotMapNote: 'The complete layout is visible. An outlined room is a legal exit from your position; locked passages require a key. The ghost remains hidden unless the candle reveals it. On rooms with four exits, choose the fourth destination here.',
  godotMapExplored: '{0} / {1} rooms visited',
  godotMapCompleteLabel: 'Full Manor floorplan, {0} of {1} rooms visited',
  godotGhostLocated: 'Ghost: {0}',
  godotMapAllRooms: 'ROOM DIRECTORY',
  godotMapRouteCount: '{0} accessible exits',
  godotInventoryHeading: 'INVENTORY',
  godotItemKey: 'Key',
  godotItemCandle: 'Candle',
  godotItemCharm: 'Charm',
  godotInventoryAvailable: 'available',
  godotInventoryMissing: 'not acquired',
  godotPracticeExit: 'Leave practice',
  godotPracticeRestart: 'Restart practice',
  godotAutoMore: 'More actions',
  godotAutoFollow: 'Recommended route · one step',
  godotAutoResolve: 'Auto-resolve run',
  godotAutoWarning: 'Auto-resolve will finish the entire bonus without further room choices. Continue?',
  godotResultEscaped: 'YOU ESCAPED',
  godotResultCaught: 'THE GHOST FOUND YOU',
  godotResultCashedOut: 'REWARD SECURED',
  godotResultExhausted: 'DAWN BREAKS',
  godotResultFinal: 'Final multiplier',
  godotResultPaid: 'Credited to your balance',
  godotResultPractice: 'Practice only · no balance changes',
  godotResultBack: 'Return to the reels',
  godotItemFound: '{0} found',
  godotCharmUsed: 'The protective charm repelled the ghost',
  godotCheckpointSaved: 'Checkpoint secured',
  soundGroup: 'Local Manor sound', sound: 'MANOR SOUND',
  sfx: 'Sound effects', ambience: 'Quiet atmospheric hum', volume: 'Sound volume',
  soundHint: 'The Manor soundtrack follows your casino music settings. Effects begin after the first interaction.',
  diagnosticsToggle: 'Show render diagnostics', speed: 'Reel motion speed',
  normal: 'Normal', fast: 'Fast', turbo: 'Turbo',
  motionHint: 'Reduced motion shortens movement without changing your results.',
  refresh: 'Refresh saved state', 
  diagnostics: 'RENDER DIAGNOSTICS · LOCAL ONLY',
  tickerPaused: 'Ticker paused (reduced motion / animation off)',
  sample: 'Sampling Pixi ticker frames...',
  fpsObserved: 'Observed ~{0} fps · mean {1} ms · p95 {2} ms · {3}/{4} frames >16.7 ms',
  dpiObserved: '{0} × {1} CSS px · {2}× backing ratio',
  diagnosticsHint: 'Measures JavaScript ticker intervals, not GPU time or a hardware benchmark.',
  fatal: 'The save outcome may already be committed. Close and reopen the modern Manor to recover it. Do not place another wager in this window.',
  footer: 'PLAY-MONEY ONLY · THE HOUSE REMEMBERS',
  revision: 'CURRENT REVISION {0}', browserFooter: 'BROWSER DESIGN PREVIEW · NO GAMEPLAY',
  language: 'Language', languageHint: 'This view follows your casino language; switching here is temporary.',
  help: 'Rules & payout table', helpTitle: 'Manor rules and paytable',
  helpClose: 'Close rules',
  helpVersion: 'Payout table · version {0}.',
  helpLegacy: 'This saved feature uses a previous payout table until it finishes.',
  helpCurrent: 'These awards apply to the current cycle or your next paid spin.',
  helpStake: 'Paid spin stake: $1–$5,000 (whole play-money dollars). 20 fixed lines share the total stake equally.',
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
  here: 'YOU ARE HERE', mapPrivacy: 'The map follows your footsteps. Use the room buttons when the floorplan is wider than your window. The ghost remains hidden unless your candle reveals it.',
  ghostRevealed: 'CANDLE REVEAL', ghostDanger: 'GHOST NEARBY',
  ghostApproaching: 'APPROACHING', ghostDistant: 'DISTANT FOOTSTEPS',
  ghostUnknown: 'UNSEEN PRESENCE',
  momentWin: 'WIN', momentNoWin: 'NO WIN', noWinDetail: 'The house falls silent',
  bonusMap: 'View floorplan', bonusReturn: 'Back to chambers', momentBig: 'BIG WIN', momentEpic: 'LEGENDARY WIN',
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
  back: 'Zurück zur Lobby', ready: 'DAS MANOR ERWACHT',
  connecting: 'VERBINDUNG ZUM CASINO', preview: 'BROWSER-DESIGNVORSCHAU',
  heroEyebrow: 'DAS HAUS VERGISST KEINE ENTSCHEIDUNG',
  heroTitle: 'Das Haus kennt deinen Namen.',
  heroText: 'Mit jedem Walzendreh erwacht das Haus. Dein Guthaben und offene Türen warten auf deine Rückkehr.',
  playtest: 'WILLKOMMEN IM MIDNIGHT MANOR',
  hauntedFive: 'DIE FÜNF WALZEN', paylinesHeader: '20 FESTE GEWINNLINIEN · 5 × 3',
  lastFree: 'LETZTER HAUNTED-NIGHT-FREISPIN', lastPaid: 'LETZTER EINSATZ-DREH',
  noSpin: 'DIE WALZEN WARTEN',
  paidLines: 'GEWINNLINIEN', moons: 'MONDE', doors: 'TÜREN',
  viewWinLines: '{0} abgeschlossene Gewinn{1} anzeigen', lineSingular: 'linie', linePlural: 'linien',
  line: 'LINIE', matched: 'TREFFER',
  stake: 'EINSATZ · SPIELGELD', freeSpin: 'FREISPIN SPIELEN · NOCH {0}',
  quickBet: 'SCHNELLEINSATZ', autoLabel: 'AUTO-SPINS', autoRemaining: 'AUTO · NOCH {0}',
  autoCount: '{0} automatische Drehs starten', autoStop: 'AUTO STOPPEN',
  notEnough: 'Für diesen Einsatz reicht dein Spielgeld nicht.',
  saving: 'ERGEBNIS WIRD GESPEICHERT …', spin: 'WALZEN DREHEN',
  spinRolling: 'WALZEN LAUFEN', spinSealed: 'ERGEBNIS ERST NACH DEM LETZTEN WALZENSTOPP',
  runHint: 'Das Manor ist geöffnet · wähle unten deinen Weg',
  freeHint: 'Für Freispiele wird kein weiterer Einsatz abgezogen',
  cycleHint: 'Beende zuerst das laufende Feature',
  committedHint: 'Folge dem Flackern des Kerzenlichts',
  invalidStake: 'Wähle einen ganzzahligen Spieleinsatz zwischen $1 und $5.000.',
  timeout: 'Die Antwort dauert länger. Der gespeicherte Zustand wird abgefragt; bitte die Aktion nicht wiederholen.',
  runEyebrow: 'INTERAKTIVER BONUS', runTitle: 'Escape the Manor', turn: 'ZUG',
  potential: 'Aktuell', checkpoint: 'Gesichert', exit: 'Ausgang',
  keyFound: 'SCHLÜSSEL GEFUNDEN', noKey: 'KEIN SCHLÜSSEL',
  charmReady: 'SCHUTZZAUBER BEREIT', noCharm: 'KEIN SCHUTZZAUBER',
  routeInstruction: 'Jede Tür führt tiefer ins Haus. Mit jedem Raumzug rückt der Geist näher. Gesicherte Belohnungen bleiben dir; verschlossene Abkürzungen brauchen einen Schlüssel.',
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
  freeDescription: 'Der Mond gewährt dir weitere Drehs. Deine Freispiele warten.',
  cycleDescription: 'Ein gespeichertes Bonus-Feature wird noch abgewickelt.',
  idleDescription: 'Ein kühler Luftzug durchstreift die Halle. Die Walzen warten unter dem Kronleuchter.',
  settings: 'ANZEIGEEINSTELLUNGEN', cabinetAnimation: 'Automatenanimation',
  reduced: 'Bewegung reduzieren',
  godotSettingsHeading: '3D-BONUSSPIEL',
  godotTestButton: 'Bonus üben · ohne Einsatz',
  godotEnableLive: '3D-Bonus aktiviert',
  godotSettingsHint: 'Echte Bonusrunden starten automatisch im 3D-Manor. Die Übung nutzt dieselben Geist- und Gegenstandsregeln, ohne Einsatz oder Auszahlung.',
  godotDemoTag: 'ÜBUNG · OHNE EINSATZ',
  godotLiveTag: 'ESCAPE THE MANOR',
  godotDemoProgress: 'ÜBUNG · ZUG {0}',
  godotArea: 'Interaktives 3D-Manor-Bonusspiel',
  godotLoading: 'Das Manor öffnet sich in 3D …',
  godotLoadingDetail: 'Die lokale 3D-Szene wird geladen. Godot läuft nur während des Bonus, damit die Walzen flüssig bleiben.',
  godotUnavailable: 'Das 3D-Bonusspiel konnte nicht starten.',
  godotUnavailableDetail: 'Die 3D-Darstellung ist fehlgeschlagen. Starte Godot erneut oder beende den Lauf sicher über die C#-Spielregeln.',
  godotHint: 'Tür wählen oder Grundriss öffnen, um den nächsten Weg zu planen',
  godotReturn: 'Zurück zum Automaten',
  godotRetry: '3D-Szene erneut starten',
  godotNoPayout: 'Übungsmodus · keine Auszahlung',
  godotTurnOf: 'ZUG {0} / {1}',
  godotRewardStatus: 'Bonusmultiplikatoren und gesicherter Kontrollpunkt',
  godotExitTarget: 'Mondtor',
  godotDemoThreat: 'ÜBUNG',
  godotDemoGhostHint: 'Der Geist folgt denselben Regeln wie im echten Bonus.',
  godotEventTitle: 'DAS HAUS REAGIERT',
  godotMapHeading: 'MANOR-GRUNDRISS',
  godotMapExpand: 'Grundriss vergrößern',
  godotMapComplete: 'ALLE RÄUME AUFGEDECKT',
  godotMapClose: 'Karte schließen',
  godotMapNote: 'Der gesamte Grundriss ist sichtbar. Umrandete Räume sind vom aktuellen Standort erreichbar; gesperrte Wege brauchen einen Schlüssel. Der Geist bleibt verborgen, sofern die Kerze ihn nicht enthüllt. Hat ein Raum vier Ausgänge, kannst du den vierten hier auswählen.',
  godotMapExplored: '{0} / {1} Räume besucht',
  godotMapCompleteLabel: 'Vollständiger Manor-Grundriss, {0} von {1} Räumen besucht',
  godotGhostLocated: 'Geist: {0}',
  godotMapAllRooms: 'RAUMVERZEICHNIS',
  godotMapRouteCount: '{0} erreichbare Wege',
  godotInventoryHeading: 'INVENTAR',
  godotItemKey: 'Schlüssel',
  godotItemCandle: 'Kerze',
  godotItemCharm: 'Schutzzauber',
  godotInventoryAvailable: 'vorhanden',
  godotInventoryMissing: 'nicht gefunden',
  godotPracticeExit: 'Übung verlassen',
  godotPracticeRestart: 'Übung neu starten',
  godotAutoMore: 'Weitere Aktionen',
  godotAutoFollow: 'Empfohlener Weg · ein Schritt',
  godotAutoResolve: 'Bonus automatisch beenden',
  godotAutoWarning: 'Der gesamte Bonus wird automatisch bis zum Ende gespielt. Ohne weitere Raumentscheidungen fortfahren?',
  godotResultEscaped: 'ENTKOMMEN',
  godotResultCaught: 'DER GEIST HAT DICH GEFUNDEN',
  godotResultCashedOut: 'GEWINN GESICHERT',
  godotResultExhausted: 'DIE NACHT ENDET',
  godotResultFinal: 'Endgültiger Multiplikator',
  godotResultPaid: 'Dem Guthaben gutgeschrieben',
  godotResultPractice: 'Nur Übung · keine Änderung des Guthabens',
  godotResultBack: 'Zurück zu den Walzen',
  godotItemFound: '{0} gefunden',
  godotCharmUsed: 'Der Schutzzauber hat den Geist abgewehrt',
  godotCheckpointSaved: 'Kontrollpunkt gesichert',
  soundGroup: 'Lokaler Manor-Sound', sound: 'MANOR-KLANG',
  sfx: 'Soundeffekte', ambience: 'Leise Schlossatmosphäre', volume: 'Lautstärke',
  soundHint: 'Die Manor-Musik folgt deinen Casino-Musikeinstellungen. Effekte starten nach der ersten Interaktion.',
  diagnosticsToggle: 'Renderer-Diagnose anzeigen', speed: 'Walzengeschwindigkeit',
  normal: 'Normal', fast: 'Schnell', turbo: 'Turbo',
  motionHint: 'Weniger Bewegung verkürzt die Animationen, ohne dein Ergebnis zu verändern.',
  refresh: 'Spielstand aktualisieren',
  diagnostics: 'RENDERER-DIAGNOSE · NUR LOKAL',
  tickerPaused: 'Ticker angehalten (Animation / Bewegung deaktiviert)',
  sample: 'Pixi-Framezeiten werden gemessen …',
  fpsObserved: 'Gemessen ~{0} FPS · Mittelwert {1} ms · P95 {2} ms · {3}/{4} Frames >16,7 ms',
  dpiObserved: '{0} × {1} CSS-Pixel · Pixelverhältnis {2}×',
  diagnosticsHint: 'Misst JavaScript-Tickerintervalle, keine GPU-Zeit oder Hardware-Benchmarkwerte.',
  fatal: 'Das Ergebnis könnte bereits gespeichert sein. Schließe und öffne das moderne Manor, um den Spielstand sicher wiederherzustellen. Platziere hier keinen weiteren Einsatz.',
  footer: 'NUR SPIELGELD · DAS HAUS VERGISST NICHT',
  revision: 'AKTUELLE REVISION {0}', browserFooter: 'BROWSER-VORSCHAU · KEIN SPIELBETRIEB',
  language: 'Sprache', languageHint: 'Diese Ansicht übernimmt die Casino-Sprache; eine Änderung hier gilt nur vorübergehend.',
  help: 'Regeln & Gewinnübersicht', helpTitle: 'Manor-Regeln und Auszahlungstabelle',
  helpClose: 'Regeln schließen',
  helpVersion: 'Auszahlungstabelle · Version {0}.',
  helpLegacy: 'Das gespeicherte Feature behält bis zum Abschluss seine ältere Auszahlungstabelle.',
  helpCurrent: 'Diese Werte gelten für den laufenden Zyklus bzw. den nächsten bezahlten Dreh.',
  helpStake: 'Einsatz pro bezahltem Dreh: $1–$5.000, nur ganze Spieldollar. Die 20 festen Linien teilen sich den Gesamteinsatz gleichmäßig.',
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
  here: 'DU BIST HIER', mapPrivacy: 'Die Karte folgt deinen Schritten. Nutze bei schmalem Fenster die Raumwahl. Der Geist bleibt verborgen, bis die Kerze ihn enthüllt.',
  ghostRevealed: 'KERZENENTHÜLLUNG', ghostDanger: 'GEIST IN DER NÄHE',
  ghostApproaching: 'NÄHERKOMMENDER GEIST', ghostDistant: 'FERNE SCHRITTE',
  ghostUnknown: 'UNBEKANNTE PRÄSENZ',
  momentWin: 'GEWINN', momentNoWin: 'KEIN GEWINN', noWinDetail: 'Das Haus schweigt',
  bonusMap: 'Grundriss anzeigen', bonusReturn: 'Zurück zu den Räumen', momentBig: 'GROSSER GEWINN', momentEpic: 'LEGENDÄRER GEWINN',
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
