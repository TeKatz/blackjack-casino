import type { ManorRun } from '../bridge/protocol';
import { localizeArea, localizeRoom, t } from '../i18n';
import type { ManorLanguage } from '../i18n';
import { ghostBand } from './manorRunUi';

const AREAS = ['Entrance','Library','Ballroom','Corridor','Attic','Crypt'] as const;
export type ManorSceneArea = typeof AREAS[number];

export function roomSceneInfo(run: ManorRun) {
  const room = run.rooms.find(value => value.id === run.playerRoom);
  return {
    name: room?.name ?? 'Entrance Hall',
    area: AREAS.find(area => area === room?.area) ?? 'Entrance',
    threat: ghostBand(run.ghostClue),
  };
}

/** Authored offline first-person perspective, not a simulated C# room or hidden ghost reveal. */
export function ManorRoomScene({ run, language, reducedMotion }: {
  run: ManorRun; language: ManorLanguage; reducedMotion: boolean;
}) {
  const view = roomSceneInfo(run);
  return (
    <figure className={'manor-vignette area-' + view.area.toLowerCase() +
      ' threat-' + view.threat + (reducedMotion ? ' still' : '')}
      aria-label={t(language,'currentRoom') + ': ' + localizeRoom(view.name,language)}>
      <div className="room-parallax" aria-hidden="true"><i/><i/><i/></div>
      <div className="room-lightfall" aria-hidden="true"/>
      <svg className="manor-vignette-svg" viewBox="0 0 960 280" aria-hidden="true"
        preserveAspectRatio="xMidYMid slice">
        <defs>
          <linearGradient id="v-wall" x1="0" x2="0" y1="0" y2="1">
            <stop stopColor="#181827"/><stop offset=".7" stopColor="#393041"/>
            <stop offset="1" stopColor="#161b28"/>
          </linearGradient>
          <linearGradient id="v-floor" x1="0" x2="0" y1="0" y2="1">
            <stop stopColor="#6a5b62"/><stop offset="1" stopColor="#151922"/>
          </linearGradient>
          <radialGradient id="v-moon">
            <stop stopColor="#fff3d4" stopOpacity=".65"/>
            <stop offset="1" stopColor="#c1b3d1" stopOpacity="0"/>
          </radialGradient>
        </defs>
        <rect width="960" height="280" fill="url(#v-wall)"/>
        <circle cx="730" cy="50" r="175" fill="url(#v-moon)"/>
        <path d="M0 280L250 157H710L960 280Z" fill="url(#v-floor)"/>
        <g fill="none" stroke="#ac9585" strokeOpacity=".35">
          <path d="M480 157V280 M250 157L100 280 M710 157L860 280 M156 230H804"/>
        </g>
        <path d="M244 171V78Q480 -78 716 78V171" fill="#141929"
          stroke="#8f7980" strokeWidth="18"/>
        <path d="M303 172V91Q480 -28 657 91V172Z" fill="#283344"
          stroke="#ac9277" strokeWidth="4"/>
        <path d="M393 172V127Q480 63 567 127V172Z" fill="#111624"
          stroke="#a28c83" strokeWidth="5"/>
        <path d="M480 92V172 M393 136H567" stroke="#7d7681" strokeWidth="3"/>
        <path d="M220 0H250V214H220Z M710 0H740V214H710Z" fill="#544559"
          stroke="#a38974" strokeWidth="3"/>
        {view.area === 'Entrance' && (
          <g fill="none" stroke="#b49c86" strokeWidth="7">
            <path d="M82 12V221 M880 12V221 M76 14Q152 -9 227 14 M734 14Q811 -9 884 14"/>
            <path d="M72 204H204 M756 204H888"/>
          </g>
        )}
        {view.area === 'Library' && (
          <g>
            {[30,125,740,835].map(x=>(
              <g key={x}>
                <rect x={x} y="23" width="92" height="165" fill="#342b36" stroke="#bc9870" strokeWidth="4"/>
                {[0,1,2,3,4,5].map(i=><rect key={i} x={x+8+(i%3)*25}
                  y={46+Math.floor(i/3)*70} width={12+i%3*2} height={39+i%2*8}
                  fill={['#a57974','#6e7c91','#b7a078'][i%3]}/>)}
                <path d={'M'+x+' 108h92'} stroke="#b5916d" strokeWidth="5"/>
              </g>
            ))}
          </g>
        )}
        {view.area === 'Ballroom' && (
          <g>
            {[150,480,810].map(x=>(
              <g key={x} fill="none" stroke="#d6b784" strokeWidth="4">
                <path d={'M'+x+' 0V59 M'+(x-28)+' 74Q'+x+' 92 '+(x+28)+' 74'}/>
                {[-28,0,28].map(dx=><ellipse key={dx} cx={x+dx} cy="66" rx="5" ry="11"
                  fill="#f6d9a3" stroke="#d6b784"/>)}
              </g>
            ))}
            <path d="M90 237Q480 203 870 237" stroke="#ab8a82" strokeWidth="4" fill="none"/>
          </g>
        )}
        {view.area === 'Corridor' && (
          <g fill="none" stroke="#8c768b">
            {[65,138,822,895].map((x,i)=><path key={x} d={'M'+x+' 4V258'}
              strokeWidth={i%2?9:14}/>)}
            <path d="M125 0Q480 182 835 0" strokeWidth="4"/>
          </g>
        )}
        {view.area === 'Attic' && (
          <g>
            <path d="M0 0L480 113L960 0 M190 0L480 113L770 0" stroke="#8a664c"
              strokeWidth="19" fill="none"/>
            <path d="M45 0L480 101L915 0" stroke="#d7ac79" strokeWidth="2" fill="none"/>
            <rect x="93" y="174" width="111" height="83" fill="#514032" stroke="#aa8966" strokeWidth="4"/>
            <path d="M93 219h111M149 174v83" stroke="#a2835b" strokeWidth="3"/>
          </g>
        )}
        {view.area === 'Crypt' && (
          <g>
            {[70,810].map(x=><g key={x}>
              <path d={'M'+x+' 217V113q40-40 80 0v104Z'} fill="#3f3849"
                stroke="#9c8d9d" strokeWidth="5"/>
              <path d={'M'+(x+40)+' 134v57 M'+(x+19)+' 154h42'}
                stroke="#c2b3b9" strokeWidth="4"/>
            </g>)}
            <path d="M0 191H216 M744 191H960" stroke="#847482" strokeWidth="7"/>
          </g>
        )}
        <ellipse cx="480" cy="252" rx="370" ry="31" fill="#e4d5e7"
          opacity=".08" className="manor-vignette-mist"/>
        <path d="M3 3H957V277H3Z" fill="none" stroke="#c9ad86" strokeOpacity=".4" strokeWidth="5"/>
      </svg>
      <figcaption>
        <span>{t(language,'currentRoom')} · {localizeArea(view.area,language)}</span>
        <strong>{localizeRoom(view.name,language)}</strong>
        <small>{t(language,view.threat==='danger'?'ghostDanger':
          view.threat==='approaching'?'ghostApproaching':
          view.threat==='revealed'?'ghostRevealed':
          view.threat==='distant'?'ghostDistant':'ghostUnknown')}</small>
      </figcaption>
    </figure>
  );
}
