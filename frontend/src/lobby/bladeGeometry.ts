/**
 * Blade Timing's painted knives are drawn pointing down the local +Y axis,
 * then rotated by the same world angle used for collisions.
 * The aiming reticle must project that *painted* tip, not an assumed
 * conventional polar angle. Gameplay/overlap detection remains untouched.
 */
export const BLADE_RIM_RADIUS=113;
export const BLADE_PENETRATION=32;
export function bladePaintedTip(cx:number,cy:number,worldAngle:number,radius=BLADE_RIM_RADIUS){
 const tipFromCenter=radius-BLADE_PENETRATION;
 return {x:cx-Math.sin(worldAngle)*tipFromCenter,
         y:cy+Math.cos(worldAngle)*tipFromCenter};
}
