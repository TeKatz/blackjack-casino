// Palette and identifying ornament of the corresponding WPF PlayingCardTheme.
// No external textures are needed to display an equipped collection faithfully.
export type CardSkin={
  faceA:string;faceB:string;border:string;black:string;red:string;
  backA:string;backB:string;accent:string;mark:string;dark?:boolean;premium?:boolean;
};
const classic:CardSkin={faceA:'#f8f8f5',faceB:'#eeeee8',border:'#d8d8d2',black:'#171a18',
  red:'#b82931',backA:'#163d29',backB:'#0a2319',accent:'#d4a84f',mark:'♠'};
const skins:Record<string,CardSkin>={
  'card-classic':classic,
  'card-art-deco':{faceA:'#f7efd5',faceB:'#e4d8b5',border:'#362e22',black:'#1c1c1a',red:'#972b2f',backA:'#1b1d1f',backB:'#0a0c0f',accent:'#cfab5a',mark:'◇'},
  'card-crimson-velvet':{faceA:'#faefe0',faceB:'#e7d8cf',border:'#803944',black:'#261f22',red:'#8b2332',backA:'#661927',backB:'#290d16',accent:'#d8c0b9',mark:'◆'},
  'card-emerald-house':{faceA:'#f4f1dd',faceB:'#dae4cd',border:'#366848',black:'#192d22',red:'#a22f36',backA:'#124b31',backB:'#072218',accent:'#e1c063',mark:'♣'},
  'card-midnight-blue':{faceA:'#e8eff6',faceB:'#cbd8e8',border:'#4c678b',black:'#1b2b43',red:'#9a3545',backA:'#1a3358',backB:'#09142a',accent:'#b8cfe9',mark:'☾'},
  'card-midnight-sovereign':{faceA:'#1f2022',faceB:'#0d0f12',border:'#d6a94c',black:'#efd9a6',red:'#de6568',backA:'#101013',backB:'#05070a',accent:'#f2c660',mark:'✦',dark:true,premium:true},
  'card-linen':{faceA:'#faf4e7',faceB:'#e8ddc7',border:'#a89a80',black:'#343531',red:'#a33f42',backA:'#c6b594',backB:'#8a795c',accent:'#fff5de',mark:'◇'},
  'card-harbor':{faceA:'#f4f9fd',faceB:'#dce9f2',border:'#507a9e',black:'#183b5b',red:'#b33f56',backA:'#245578',backB:'#122e4b',accent:'#b7dceb',mark:'≋'},
  'card-rosewood':{faceA:'#fff1dc',faceB:'#e5ccae',border:'#b47959',black:'#3d2825',red:'#9e3742',backA:'#683a34',backB:'#2e191b',accent:'#e4b18a',mark:'◆'},
  'card-jade-filigree':{faceA:'#eff7e8',faceB:'#cfdecc',border:'#92945f',black:'#1d463d',red:'#ac4a4b',backA:'#306a58',backB:'#112f2b',accent:'#e1cf91',mark:'❖'},
  'card-celestial-opal':{faceA:'#f9f1ff',faceB:'#d9eaef',border:'#a8bedd',black:'#383359',red:'#9b3763',backA:'#514b79',backB:'#163e4d',accent:'#d7fff5',mark:'✧',premium:true},
  'card-solar-eclipse':{faceA:'#29232b',faceB:'#100f18',border:'#d2af69',black:'#f8e5b9',red:'#ff9fa2',backA:'#251d25',backB:'#080d18',accent:'#ffe0a0',mark:'☀',dark:true,premium:true},
};
export function resolveCardSkin(variant:string):CardSkin{return skins[variant]??classic;}
export function availableCardVariants():string[]{return Object.keys(skins);}
