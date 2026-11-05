/** Localized presentation text. C# owns the actual language preference. */
export type BlackjackLanguage='en'|'de';
export const bjText={
 en:{
  lobby:'Casino lobby',bankroll:'BANKROLL',dealer:'DEALER',shoe:'SHOE',cards:'CARDS',
  waiting:'Waiting for your bet',seat:'YOUR SEAT',hand:'HAND',yourHand:'YOUR HAND',
  yourMove:'Your move',soft:'soft',empty:'Place a wager and deal to begin.',
  wager:'YOUR WAGER',exact:'Exact wager',clear:'Clear wager',deal:'DEAL',limits:'LIMITS',
  table:'TABLE CONFIGURATION',mode:'Game mode',solo:'Solo',full:'Full table (AI)',
  aiSeats:'AI seats',preset:'Table preset',custom:'Custom',holeCard:'Hole card',
  editRules:'Edit custom rules',equipped:'Equipped cards',felt:'Felt',
  green:'Green',blue:'Midnight blue',burgundy:'Burgundy',sound:'Sound',
  animations:'Animations',reset:'Reset play-money bankroll',
  hit:'HIT',stand:'STAND',double:'DOUBLE',split:'SPLIT',surrender:'SURRENDER',
  yesInsurance:'TAKE INSURANCE',noInsurance:'NO INSURANCE',net:'NET',
  statusReady:'Ready for your next hand.',statusInsurance:'Dealer shows an Ace. Insurance?',
  statusTurn:'Your move.',statusSettled:'Round settled.',
  loading:'Opening your table…',browser:'Open Blackjack in the desktop application.',
  finish:'Finish the current hand before returning to the lobby.',
  late:'Confirmation missing. Reopen the table before any further action.',
  invalid:'Enter a valid wager.',large:'Wager exceeds the table limit or available bankroll.',
  resetTitle:'Reset local bankroll?',resetDetail:'Your play-money bankroll will return to $1,000. This cannot be undone.',
  cancel:'Cancel',resetConfirm:'Reset bankroll',
  rulesTitle:'Custom table rules',rulesDesc:'Changes apply to the next round. The native game engine remains authoritative.',
  closeRules:'Close rules',decks:'Decks',soft17:'Dealer on soft 17',
  stand17:'Stand (S17)',hit17:'Hit (H17)',dealerHole:'Dealer hole card',
  usHole:'US hole card',europeanHole:'European no-hole-card',dealerPeek:'Dealer peeks for blackjack',
  insurance:'Insurance available',lateSurrender:'Late surrender',payout:'Blackjack payout',
  doubleDown:'Double down',anyTwo:'Any first two',afterSplit:'Double after split',
  resplitAces:'Resplit aces',hitSplitAces:'Hit split aces',maxSplit:'Max split hands',
  penetration:'Deck penetration',minBet:'Minimum wager',maxBet:'Maximum wager',
  invalidLimits:'Use valid limits between 0.01 and 1,000,000 (maximum at least minimum).',
  useCustom:'Use custom rules',
 },
 de:{
  lobby:'Casino-Lobby',bankroll:'SPIELGUTHABEN',dealer:'DEALER',shoe:'SCHUH',cards:'KARTEN',
  waiting:'Warte auf deinen Einsatz',seat:'DEIN PLATZ',hand:'HAND',yourHand:'DEINE HAND',
  yourMove:'Du bist am Zug',soft:'weich',empty:'Setze einen Betrag und gib Karten.',
  wager:'DEIN EINSATZ',exact:'Exakter Einsatz',clear:'Einsatz löschen',deal:'GEBEN',limits:'TISCHLIMITS',
  table:'TISCHEINSTELLUNGEN',mode:'Spielmodus',solo:'Solo',full:'Voller Tisch (KI)',
  aiSeats:'KI-Plätze',preset:'Regelvorlage',custom:'Eigene Regeln',holeCard:'Verdeckte Karte',
  editRules:'Eigene Regeln bearbeiten',equipped:'Ausgerüstetes Kartenset',felt:'Tischfarbe',
  green:'Grün',blue:'Mitternachtsblau',burgundy:'Bordeaux',sound:'Soundeffekte',
  animations:'Animationen',reset:'Spielguthaben zurücksetzen',
  hit:'KARTE',stand:'STEHEN',double:'VERDOPPELN',split:'TEILEN',surrender:'AUFGEBEN',
  yesInsurance:'VERSICHERN',noInsurance:'KEINE VERSICHERUNG',net:'ERGEBNIS',
  statusReady:'Bereit für die nächste Runde.',statusInsurance:'Dealer zeigt ein Ass. Versichern?',
  statusTurn:'Du bist am Zug.',statusSettled:'Runde abgeschlossen.',
  loading:'Dein Tisch wird geöffnet…',browser:'Öffne Blackjack in der Desktop-Anwendung.',
  finish:'Beende zuerst die aktuelle Hand, bevor du zur Lobby zurückkehrst.',
  late:'Bestätigung fehlt. Öffne den Tisch erneut, bevor du eine neue Aktion ausführst.',
  invalid:'Gib einen gültigen Einsatz ein.',large:'Der Einsatz übersteigt das Tischlimit oder dein Guthaben.',
  resetTitle:'Spielguthaben zurücksetzen?',resetDetail:'Dein Spielguthaben wird auf $1.000 zurückgesetzt. Das kann nicht rückgängig gemacht werden.',
  cancel:'Abbrechen',resetConfirm:'Guthaben zurücksetzen',
  rulesTitle:'Eigene Tischregeln',rulesDesc:'Änderungen gelten ab der nächsten Runde. Die Spiellogik bleibt in C#.',
  closeRules:'Regeln schließen',decks:'Decks',soft17:'Dealer bei weicher 17',
  stand17:'Stehen (S17)',hit17:'Ziehen (H17)',dealerHole:'Verdeckte Dealer-Karte',
  usHole:'US-Regel mit verdeckter Karte',europeanHole:'Europäische Regel ohne verdeckte Karte',
  dealerPeek:'Dealer prüft auf Blackjack',insurance:'Versicherung erlauben',
  lateSurrender:'Späte Aufgabe',payout:'Blackjack-Auszahlung',
  doubleDown:'Verdoppeln',anyTwo:'Beliebige erste zwei Karten',
  afterSplit:'Verdoppeln nach Teilen',resplitAces:'Asse erneut teilen',
  hitSplitAces:'Geteilte Asse ziehen',maxSplit:'Maximale Teilhände',
  penetration:'Schuh-Eindringtiefe',minBet:'Mindesteinsatz',maxBet:'Höchsteinsatz',
  invalidLimits:'Gültige Limits von 0,01 bis 1.000.000 eingeben (Maximum mindestens Minimum).',
  useCustom:'Eigene Regeln übernehmen',
 }
};
const outcomes:Record<string,string>={Win:'Gewonnen',Loss:'Verloren',Push:'Unentschieden',
  Blackjack:'Blackjack',Surrender:'Aufgegeben',Surrendered:'Aufgegeben',Bust:'Überkauft'};
export function bjOutcome(raw:string|null,language:BlackjackLanguage):string{
 return raw?(language==='de'?(outcomes[raw]??raw):raw):'';
}
export function bjStatus(language:BlackjackLanguage,phase:string|undefined):string{
 const t=bjText[language];
 return phase==='InsuranceDecision'?t.statusInsurance:
  phase==='PlayerTurn'?t.statusTurn:phase==='Settled'?t.statusSettled:t.statusReady;
}
