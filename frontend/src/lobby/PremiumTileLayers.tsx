import {isAnimatedTile,type AnimatedTileId} from './TileFxEngine';
import './tileMotion.css';

/** DOM optical layers complement the single shared masked PixiJS WebGL stage. */
export function PremiumTileLayers({id,enabled}:{id:string;enabled:boolean}){
 if(!isAnimatedTile(id))return null;
 return <div className="tile-motion-layers" data-tile-motion={id} data-running={enabled}
  aria-hidden="true">
  <div className="tile-depth-shadow"/>
  <div className="tile-drift-haze"/>
  <div className="tile-moving-beam"/>
  <div className="tile-light-field"/>
  <div className={'tile-signature tile-signature-'+id}>{signature(id)}</div>
  <div className="tile-top-glass"/>
 </div>;
}
function signature(id:AnimatedTileId){
 switch(id){
  case 'midnight-reels':return <><i className="tile-reels-glint"/><i className="tile-reels-flare"/></>;
  case 'neon-vault':return <><i className="tile-vault-orbit"/><i className="tile-vault-orbit inner"/><i className="tile-vault-core"/></>;
  case 'nine-lives':return <><i className="tile-cat-mist one"/><i className="tile-cat-mist two"/><i className="tile-cat-aura"/></>;
  case 'midnight-manor':return <><i className="tile-manor-flame left"/><i className="tile-manor-flame right"/><i className="tile-manor-portal"/></>;
  case 'chill-lounge':return <><i className="tile-lounge-steam first"/><i className="tile-lounge-steam second"/><i className="tile-lounge-reflection"/></>;
  case 'casino-empire':return <><i className="tile-empire-windows"/><i className="tile-empire-sunbeam"/></>;
  case 'kniffel':return <><i className="tile-dice-sheen"/><i className="tile-dice-aura"/></>;
 }
}
