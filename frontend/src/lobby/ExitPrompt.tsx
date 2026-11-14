import {Modal} from './Modal';
import '../exit/exit.css';
import './exitPrompt.css';

/** Same visual confirmation as the standalone exit page, but no cold WebView startup. */
export function ExitPrompt({language,onStay,onExit}:{
  language:'de'|'en';onStay:()=>void;onExit:()=>void;
}){
  const de=language==='de';
  return <Modal className="casino-lobby-exit-modal"
    label={de?'Casino beenden':'Exit casino'} onClose={onStay}>
    <section className="exit-dialog" aria-labelledby="casino-exit-heading"
      aria-describedby="casino-exit-description">
      <div className="exit-crest"><span className="exit-crest-mark" aria-hidden="true">✦</span>
        <span>THE MIDNIGHT CLUB</span><small>{de?'SITZUNG BEENDEN':'END SESSION'}</small>
      </div>
      <h1 id="casino-exit-heading">{de?'Den Club verlassen?':'Leaving the club?'}</h1>
      <p id="casino-exit-description">{de
        ?'Möchtest du deine Casino-Sitzung wirklich beenden? Deine bereits gespeicherten Fortschritte bleiben erhalten.'
        :'Are you sure you want to end your casino session? Your previously saved progress will be retained.'}</p>
      <div className="exit-divider" aria-hidden="true"/>
      <div className="exit-actions">
        <button type="button" className="exit-action exit-stay"
          onClick={onStay}>{de?'Im Casino bleiben':'Stay in the casino'}</button>
        <button type="button" className="exit-action exit-leave"
          onClick={onExit}>{de?'Casino beenden':'Exit casino'}</button>
      </div>
      <p className="exit-keyboard">{de?'ESC zum Zurückkehren':'ESC to return'}</p>
    </section>
  </Modal>;
}
