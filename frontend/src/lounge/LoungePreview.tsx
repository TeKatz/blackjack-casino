import type {LoungeId} from './bridge';

/** Eight independent, scalable editorial illustrations. No duplicate icon mosaics. */
export function LoungePreview({id}:{id:LoungeId}){
 const className=id==='tower-defense'?'lounge-bastion-preview':
   id==='minesweeper'?'lounge-sweep-preview':'lounge-editorial-preview';
 return <img className={className} src={'/lounge/art/'+id+'.svg'} alt=""
   aria-hidden="true" draggable={false} decoding="async"/>;
}
