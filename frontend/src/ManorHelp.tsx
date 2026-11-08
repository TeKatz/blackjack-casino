import { useEffect, useRef } from 'react';
import type { ManorRulesInfo } from './bridge/protocol';
import { t } from './i18n';
import type { ManorLanguage } from './i18n';

type Props = {
  language: ManorLanguage;
  rules: ManorRulesInfo;
  cycleActive: boolean;
  onClose: () => void;
};

const germanNames: Record<string, string> = {
  Jack: 'Viktorianisches J', Queen: 'Viktorianisches Q',
  King: 'Viktorianisches K', Ace: 'Viktorianisches A',
  Candelabrum: 'Kerzenleuchter', Watch: 'Taschenuhr', Raven: 'Rabe',
  Key: 'Antiker Schlüssel', Portrait: 'Spukporträt', Rose: 'Schwarze Rose',
  Lady: 'Herrin des Manor', Wild: 'Spukspiegel / Wild',
  Scatter: 'Manor-Mond / Scatter', Bonus: 'Manor-Tür / Bonus',
};

export function ManorHelp({ language, rules, cycleActive, onClose }: Props) {
  const close = useRef<HTMLButtonElement>(null);
  const lastFocus = useRef<HTMLElement | null>(null);
  useEffect(() => {
    lastFocus.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    close.current?.focus();
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') { event.preventDefault(); onClose(); }
      else if (event.key === 'Tab') {
        // The help view has one interactive item; keep keyboard focus in the dialog.
        event.preventDefault();
        close.current?.focus();
      }
    };
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('keydown', onKey);
      lastFocus.current?.focus();
    };
  }, [onClose]);

  return (
    <div className="manor-help-scrim"
      onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}>
      <section className="manor-help-sheet" role="dialog" aria-modal="true"
        aria-labelledby="manor-rules-heading">
        <div className="manor-help-heading">
          <div>
            <span className="eyebrow">{t(language, 'help')}</span>
            <h2 id="manor-rules-heading">{t(language, 'helpTitle')}</h2>
          </div>
          <button className="ghost-button" type="button" ref={close} onClick={onClose}>
            {t(language, 'helpClose')}
          </button>
        </div>
        <p className="rules-version">{t(language, 'helpVersion', rules.version)}
          {' '}{t(language, cycleActive && rules.version < 2 ? 'helpLegacy' : 'helpCurrent')}</p>
        <div className="manor-rules-copy">
          <p>{t(language, 'helpStake')}</p>
          <p>{t(language, 'helpWin')}</p>
          <h3>{t(language, 'helpCredits')}</h3>
          <div className="manor-paytable-scroll" tabIndex={0}>
            <table className="manor-paytable">
              <thead>
                <tr><th scope="col">{t(language, 'helpSymbol')}</th>
                  <th scope="col">{t(language, 'helpThree')}</th>
                  <th scope="col">{t(language, 'helpFour')}</th>
                  <th scope="col">{t(language, 'helpFive')}</th></tr>
              </thead>
              <tbody>
                {rules.symbols.map(entry => (
                  <tr key={entry.symbol}>
                    <th scope="row">{language === 'de'
                      ? germanNames[entry.symbol] ?? entry.name : entry.name}</th>
                    <td>{entry.three > 0 ? entry.three : '—'}</td>
                    <td>{entry.four > 0 ? entry.four : '—'}</td>
                    <td>{entry.five > 0 ? entry.five : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <h3>{t(language, 'helpSpecial')}</h3>
          <p>{t(language, 'helpSpecialText')}</p>
          <h3>{t(language, 'helpLines')}</h3>
          <p>{t(language, 'helpRows')}</p>
          <div className="manor-payline-gallery">
            {rules.paylines.map((line, number) => (
              <div className="manor-line-card" key={number}
                aria-label={t(language, 'line') + ' ' + (number + 1) + ': ' + line.join(', ')}>
                <strong>{number + 1}</strong>
                <div className="manor-mini-line" aria-hidden="true">
                  {line.map((row, reel) => (
                    <div className="manor-mini-reel" key={reel}>
                      {[0, 1, 2].map(point => (
                        <span className={point === row ? 'marked' : ''} key={point} />
                      ))}
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
          <h3>{t(language, 'helpRun')}</h3>
          <p>{t(language, 'helpRunText')}</p>
          <p className="rules-caution">{t(language, 'helpRisk')}</p>
        </div>
      </section>
    </div>
  );
}
