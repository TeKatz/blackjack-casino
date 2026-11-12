import type {DicePalette} from './bridge';

export const DICE_SET_SKINS={
 'dice-ivory':{name:'House Ivory',de:'Haus-Elfenbein',surface:'ivory'},
 'dice-obsidian':{name:'Obsidian Gold',de:'Obsidian-Gold',surface:'glass'},
 'dice-jade':{name:'Jade Palace',de:'Jade-Palast',surface:'stone'},
 'dice-violet':{name:'Royal Amethyst',de:'Königlicher Amethyst',surface:'crystal'},
 'dice-ice':{name:'Moonstone',de:'Mondstein',surface:'crystal'},
 'dice-ruby':{name:'Ruby Velvet',de:'Rubin-Samt',surface:'velvet'},
 'dice-aurora':{name:'Aurora Prism',de:'Aurora-Prisma',surface:'crystal'},
 'dice-porcelain':{name:'Porcelain Blue',de:'Porzellanblau',surface:'porcelain'},
 'dice-forest':{name:'Forest Resin',de:'Waldharz',surface:'resin'},
 'dice-copper':{name:'Copper Forge',de:'Kupferschmiede',surface:'metal'},
 'dice-sapphire':{name:'Sapphire Inlay',de:'Saphir-Intarsie',surface:'gem'},
 'dice-astral-opal':{name:'Astral Opal',de:'Astral-Opal',surface:'opal'},
 'dice-solar-crown':{name:'Solar Crown',de:'Sonnenkrone',surface:'metal'},
} as const;
export type DiceVariant=keyof typeof DICE_SET_SKINS;
export const isDiceVariant=(v:string):v is DiceVariant=>Object.prototype.hasOwnProperty.call(DICE_SET_SKINS,v);
export function diceSkin(v:string){
 return DICE_SET_SKINS[isDiceVariant(v)?v:'dice-ivory'];
}
/** UI-only skin identity. No effect on C# dice rolls or saved game semantics. */
export function diceAppearance(variant:string,palette:DicePalette){
 const surface=diceSkin(variant).surface;
 return {surface,edge:palette.edge,glow:palette.glow??palette.edge,
  translucent:['glass','crystal','gem','opal'].includes(surface)};
}
