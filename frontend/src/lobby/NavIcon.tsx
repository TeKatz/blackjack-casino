import type {ReactNode} from 'react';

/** Gold-etched pictograms are tied to destination IDs, never menu position. */
export type NavIconName='news'|'all'|'tables'|'slots'|'relax'|'empire'|'collections'|
  'shop'|'stats'|'season'|'challenges'|'vault'|'daily'|'profile'|'settings'|'updates';

const shapes:Record<NavIconName,ReactNode>={
 news:<><rect x="4" y="4" width="16" height="16" rx="2"/><path d="M8 8h5v5H8zM15 9h2M15 12h2M8 16h9"/></>,
 all:<><path d="m3 10 9-7 9 7M5.5 9v11h13V9M10 20v-7h4v7"/></>,
 tables:<><rect x="4" y="5" width="12" height="16" rx="2"/><path d="M8 9c-1 2 0 3 2 4 2-1 3-2 2-4l-2-2-2 2zM9 15h2M18 5h2v12h-2"/></>,
 slots:<><rect x="3" y="6" width="17" height="14" rx="2"/><path d="M6 9h11v7H6zM10 9v7M14 9v7M7.8 12.5h.1M11.8 12.5h.1M15.8 12.5h.1M20 10h2V7M9 20v2M17 20v2"/></>,
 relax:<><path d="M4 9h13v5a6 6 0 0 1-12 0V9zM17 10h2a2 2 0 0 1 0 4h-2M4 20h15M8 3c-2 2 2 3 0 5M13 3c-2 2 2 3 0 5"/></>,
 empire:<><path d="M3 21h18M5 21V9l4 2V5l4-2 3 3v15M16 10h3v11M9 15h2M9 18h2M13 9v2M13 15v2M19 13h2v8"/></>,
 collections:<><rect x="6" y="4" width="14" height="16" rx="2"/><path d="M4 7H3v14h14v-1M13 8l1.2 2.5 2.8.5-2 2 0.5 2.7L13 14.3l-2.5 1.4.5-2.7-2-2 2.8-.5z"/></>,
 shop:<><path d="M5 9h14l-1 12H6L5 9zM9 10V7a3 3 0 0 1 6 0v3M9 14h.1M15 14h.1"/></>,
 stats:<><path d="M3 21h18M5 18v-5h3v5M11 18V9h3v9M17 18V4h3v14"/></>,
 season:<><path d="M8 4h8v6a4 4 0 0 1-8 0V4zM8 6H5v2a3 3 0 0 0 3 3M16 6h3v2a3 3 0 0 1-3 3M12 14v4M9 21h6M9 18h6v3H9"/></>,
 challenges:<><circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1.5"/><path d="m15 9 5-5"/></>,
 vault:<><rect x="3" y="4" width="18" height="16" rx="3"/><circle cx="11" cy="12" r="4"/><path d="M11 8v8M7 12h8M18 9v6M7 20v2M17 20v2"/></>,
 daily:<><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M7 3v4M17 3v4M3 10h18m-13 5 3 3 5-5"/></>,
 profile:<><circle cx="12" cy="12" r="9"/><circle cx="12" cy="9" r="3"/><path d="M6.5 18a6.5 6.5 0 0 1 11 0"/></>,
 settings:<><path d="M4 6h16M4 12h16M4 18h16"/><circle cx="9" cy="6" r="2" fill="#0d3024"/><circle cx="16" cy="12" r="2" fill="#0d3024"/><circle cx="11" cy="18" r="2" fill="#0d3024"/></>,
 updates:<><path d="M20 11a8 8 0 0 0-14-5L4 9M4 4v5h5M4 13a8 8 0 0 0 14 5l2-3M20 20v-5h-5"/></>,
};
export function NavIcon({name}:{name:NavIconName}){
 return <svg className="nav-icon" viewBox="0 0 24 24" aria-hidden="true"
  data-nav-icon={name} focusable="false">{shapes[name]}</svg>;
}
