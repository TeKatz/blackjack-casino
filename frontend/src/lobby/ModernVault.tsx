import type {VaultSnapshot,VaultReward} from './recordsBridge';
type Props={data:VaultSnapshot;loaded:boolean;language:'en'|'de';pending:boolean;
 reward:VaultReward|null;notice:string|null;onBack:()=>void;onOpen:()=>void;onAlbum:()=>void};
export function ModernVault({data,loaded,language,pending,reward,notice,onBack,onOpen,onAlbum}:Props){
 const de=language==='de';
 const percent=Math.min(100,Math.max(0,Math.round(data.progress*1000)/10));
 const tiers=['bronze','silver','gold','platinum','diamond'];
 return <main className="record-page vault-page"><div className="record-page-scroll">
 <header className="record-hero vault-hero">
  <div className="record-hero-copy">
   <button type="button" className="shop-back" onClick={onBack}>{de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
   <span className="shop-eyebrow">THE MIDNIGHT CLUB / HOUSE RESERVE</span>
   <h1>Casino Vault</h1>
   <p>{de?'Ein Teil deiner Einsätze füllt den Tresor. Sobald er voll ist, wird er mit einem Bonus ausgezahlt.':
    'A portion of eligible wagers fills the reserve. Break it open at 100% for an additional bonus.'}</p>
  </div>
  <div className="record-hero-side"><span>{de?'AKTUELLE STUFE':'CURRENT TIER'}</span>
  <strong className="vault-tier-name">{data.tier}</strong>
  <small>{de?'Freigeschaltet':'Unlocked'} · {data.unlockedTier}</small></div>
 </header>
 {!loaded?<p className="record-empty" role="status">{de?'Tresor wird geladen…':'Loading the Vault…'}</p>:<>
 <section className={'vault-chamber '+(data.isFull?'is-ready ':'')+(reward?'vault-opened':'')} style={{'--vault-accent':data.accent} as React.CSSProperties}>
 <div className="vault-chamber-art" data-tier={data.tierId} aria-hidden="true"><div className="vault-safe-body"><div className="vault-door"><span className="vault-safe-wheel">✥</span><i/><b>{data.tier}</b></div></div></div>
 <div className="vault-chamber-details">
  <span className="shop-eyebrow">{data.payoutPending?(de?'AUSZAHLUNG AUSSTEHEND':'PAYOUT RECOVERY'):
   data.isFull?(de?'BEREIT ZUM ÖFFNEN':'READY TO UNLOCK'):(de?'TRESOR FÜLLT SICH':'FILLING THE VAULT')}</span>
  <h2>{data.current} <small>/ {data.capacity}</small></h2>
  <div className="vault-progress-track" role="progressbar" aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100}
    aria-label={de?'Vault-Fortschritt':'Vault progress'}><span style={{width:percent+'%'}}/></div>
  <div className="vault-progress-label"><strong>{percent.toLocaleString(language)}%</strong>
  <span>{de?'Bonus bei 100 %':'Bonus at 100%'} · +{Math.round(data.bonusRate*100)}%</span></div>
  <button className="vault-break" disabled={!data.isFull||data.payoutPending||pending||reward!==null}
    type="button" onClick={onOpen}>{pending?(de?'WIRD GEÖFFNET…':'OPENING…'):
    reward?(de?'GEÖFFNET':'OPENED'):data.payoutPending?(de?'AUSZAHLUNG AUSSTEHEND':'PAYOUT PENDING'):
    data.isFull?(de?'TRESOR AUFBRECHEN':'BREAK OPEN VAULT'):(de?'NOCH GESPERRT':'VAULT LOCKED')}</button>
 </div></section>
 {notice&&<p className="vault-notice" role="status">{notice}</p>}
 {reward&&<section className="vault-reward" role="status" aria-label={de?'Tresor geöffnet':'Vault opened'}>
 <span className="shop-eyebrow">VAULT UNLOCKED · {reward.tier}</span>
 <h2>{de?'Deine Auszahlung':'Your reward'}</h2>
 <div><span>{de?'Grundbetrag':'Base payout'}</span><strong>{reward.baseAmount}</strong></div>
 <div><span>{de?'Vollbonus':'Full-Vault bonus'}</span><strong>{reward.bonusAmount}</strong></div>
 <div className="vault-total"><span>{de?'Gesamt':'Total payout'}</span><strong>{reward.total}</strong></div>
 <p>{reward.pack} {de?'Kartenpaket ins Album gelegt.':'card pack added to your collection.'}</p>
 <button className="record-claim-all" type="button" onClick={onAlbum}>{de?'Zum Album':'Go to Album'}</button>
 </section>}
 <div className="vault-tier-track" aria-label={de?'Tresorstufen':'Vault tiers'}>
 {tiers.map(tier=><span key={tier} className={data.tierId===tier?'active':''}>{tier.toUpperCase()}</span>)}
 </div>
 <section className="vault-details-grid">
 <div><span>{de?'Beitragsrate':'Contribution rate'}</span><strong>{Math.round(data.contributionRate*100)}%</strong></div>
 <div><span>{de?'Tresore geöffnet':'Vaults opened'}</span><strong>{data.vaultsOpened.toLocaleString(language)}</strong></div>
 <div><span>{de?'Gesamt eingezahlt':'Total contributed'}</span><strong>{data.lifetimeContributions}</strong></div>
 <div><span>{de?'Gesamt ausgezahlt':'Total payouts'}</span><strong>{data.lifetimePayouts}</strong></div>
 <div><span>{de?'Bester Tresor':'Largest payout'}</span><strong>{data.biggestPayout}</strong></div>
 <div><span>{de?'Nächste Stufe':'Next tier'}</span><strong>{data.nextTier??(de?'Maximal':'Maximum')}</strong>
  {data.nextTierWager&&<small>{data.nextTierWager} {de?'lebenslange Einsätze':'lifetime wager'}</small>}</div>
 </section>
 </>}
 </div></main>;
}
