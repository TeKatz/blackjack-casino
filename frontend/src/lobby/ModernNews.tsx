import {Modal} from './Modal';
import type {LobbyNewsItem} from './lobbyBridge';
import './news.css';

type Props={
  entries:LobbyNewsItem[];
  installedVersion:string;
  releaseSpecific:boolean;
  language:'de'|'en';
  motionEnabled:boolean;
  onClose:()=>void;
};

const categoryName=(name:string,de:boolean):string=>name==='feature'
  ?(de?'NEU':'NEW'):name==='fix'
    ?(de?'BEHOBEN':'FIXED'):(de?'VERBESSERT':'IMPROVED');

export function ModernNews({entries,installedVersion,releaseSpecific,language,
  motionEnabled,onClose}:Props){
  const de=language==='de';
  return <Modal className="casino-news casino-news-editorial"
    label={de?'Neuigkeiten im Casino':'Casino news'} onClose={onClose}>
    <div className="news-editorial-shell" data-motion={motionEnabled?'on':'off'}>
      <div className="news-editorial-glow" aria-hidden="true"/>
      <header className="news-editorial-head">
        <div className="news-editorial-heading">
          <span className="news-editorial-eyebrow">
            THE MIDNIGHT CLUB <span aria-hidden="true">◆</span>
            {releaseSpecific?(de?'NEU IN DIESEM UPDATE':'LATEST UPDATE'):
              (de?'HIGHLIGHTS IM CASINO':'CASINO HIGHLIGHTS')}
          </span>
          <h2>{de?'Neu im Casino':'What’s new'}</h2>
          <p>{releaseSpecific
            ?de?'Das hat sich seit dem letzten Update für dich geändert.':
              'Here’s what changed for you in the latest update.'
            :de?'Entdecke ausgewählte Neuerungen und Möglichkeiten im Midnight Club.':
              'Explore some of the latest additions to the Midnight Club.'}</p>
          <span className="news-editorial-version">
            {de?'VERSION':'VERSION'} · {installedVersion}
          </span>
        </div>
        <button type="button" className="news-editorial-close" onClick={onClose}
          aria-label={de?'Neuigkeiten schließen':'Close news'}>×</button>
      </header>
      <section className="news-editorial-body" aria-label={de?'Neuerungen':'What changed'}>
        {entries.length>0?
          <div className="news-editorial-list">
            {entries.map((note,index)=><article className="news-editorial-item"
              key={index} style={{'--news-accent':note.accent} as React.CSSProperties}>
              <div className="news-editorial-num" aria-hidden="true">
                {String(index+1).padStart(2,'0')}
              </div>
              <div className="news-editorial-copy">
                <span className="news-editorial-kind">{categoryName(note.category??'improvement',de)}</span>
                <h3>{de?(note.titleDe??note.title):note.title}</h3>
                <p>{de?(note.descriptionDe??note.description):note.description}</p>
              </div>
              <span className="news-editorial-twinkle" aria-hidden="true">✦</span>
            </article>)}
          </div>
          :<p className="news-editorial-empty">
            {de?'Für diese Version sind momentan keine Neuigkeiten hinterlegt.':
              'There are no news items for this version yet.'}
          </p>}
      </section>
      <footer className="news-editorial-footer">
        <span>{de?'Viel Spaß im Midnight Club!':'Enjoy your stay at the Midnight Club!'}</span>
        <button type="button" className="news-editorial-enter" onClick={onClose}>
          {de?'ZUM CASINO':'ENTER CASINO'} <span aria-hidden="true">›</span>
        </button>
      </footer>
    </div>
  </Modal>;
}
