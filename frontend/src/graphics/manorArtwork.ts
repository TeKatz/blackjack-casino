/** Original offline-only 600x320 SVG textures. Fourteen unique Victorian collectibles.
 * SVG markup is kept in source control as text, decoded once, then shared by Pixi tiles.
 * No network requests, third-party fonts, remote art, procedural payout or RNG.
 */
export const MANOR_SYMBOL_NAMES = [
  'Jack', 'Queen', 'King', 'Ace', 'Candelabrum', 'Watch', 'Raven',
  'Key', 'Portrait', 'Rose', 'Lady', 'Wild', 'Scatter', 'Bonus',
] as const;
export type ManorSymbolName = typeof MANOR_SYMBOL_NAMES[number];

const letterArt = (letter: string, side: string): string =>
  '<path d="M67 112Q87 100 100 117 M233 112Q213 100 200 117" fill="none" stroke="#b69a6e" stroke-width="2"/>' +
  '<path d="M90 32Q150 8 210 32 M90 120Q150 142 210 120" fill="none" stroke="#b69a6e" stroke-width="2"/>' +
  '<path d="M125 18L150 9 175 18 168 30 132 30Z" fill="url(#brass)" stroke="#ffe4ad" stroke-width="1"/>' +
  '<text x="150" y="109" text-anchor="middle" font-family="Georgia,serif" font-size="92" font-weight="bold"' +
  ' fill="url(#ivory)" stroke="#6b4c42" stroke-width="1.8" paint-order="stroke">' + letter + '</text>' +
  '<path d="' + side + '" fill="none" stroke="url(#brass)" stroke-width="2.7" stroke-linecap="round"/>';

const artwork: Record<ManorSymbolName, string> = {
  Jack: letterArt('J','M64 43Q50 75 67 93 M236 43Q250 75 233 93 M70 69L85 69 M230 69L215 69'),
  Queen: letterArt('Q','M64 55Q48 68 59 88Q74 95 81 88 M236 55Q252 68 241 88Q226 95 219 88'),
  King: letterArt('K','M60 91L80 80 65 67 84 56 M240 91L220 80 235 67 216 56'),
  Ace: letterArt('A','M68 99Q48 71 71 44 M232 99Q252 71 229 44 M67 33L74 25 81 33 M219 33L226 25 233 33'),
  Candelabrum:
    '<ellipse cx="150" cy="133" rx="65" ry="11" fill="#060a14" opacity=".7"/>' +
    '<path d="M149 47V116 M107 116H192 M117 108L184 108 M133 116L124 129H176L167 116Z" stroke="url(#brass)" stroke-width="8" fill="url(#brass)" stroke-linecap="round"/>' +
    '<path d="M150 85Q106 91 105 61 M150 85Q194 91 195 61" stroke="url(#brass)" stroke-width="7" fill="none"/>' +
    '<path d="M96 62H114 M139 48H161 M186 62H204" stroke="#f6dca8" stroke-width="4" stroke-linecap="round"/>' +
    '<path d="M98 62V48H112V62 M143 48V34H157V48 M188 62V48H202V62" fill="url(#ivory)" stroke="#b99b7e" stroke-width="2"/>' +
    '<path d="M105 45Q90 26 105 15Q121 29 105 45Z M150 31Q136 11 150 5Q165 13 150 31Z M195 45Q180 26 195 15Q211 29 195 45Z" fill="url(#flame)" stroke="#fff0b6" stroke-width="1.5"/>' +
    '<path d="M105 37Q99 29 105 24Q111 30 105 37 M150 24Q145 16 150 12Q155 17 150 24 M195 37Q189 29 195 24Q201 30 195 37" fill="#fff4cc"/>' +
    '<circle cx="150" cy="80" r="6" fill="#f2d399"/><path d="M148 87V116" stroke="#fff0cf" stroke-width="2"/>',
  Watch:
    '<path d="M181 51Q226 22 246 48T266 38" fill="none" stroke="#bd9a5e" stroke-width="4" stroke-dasharray="4 5"/>' +
    '<circle cx="150" cy="83" r="58" fill="url(#brass)" stroke="#fbe8bc" stroke-width="3"/>' +
    '<circle cx="150" cy="83" r="49" fill="#19232e" stroke="#9d8b71" stroke-width="3"/>' +
    '<circle cx="150" cy="83" r="42" fill="url(#midnight)" stroke="#ccb17e" stroke-width="1.7"/>' +
    '<rect x="139" y="11" width="22" height="14" rx="4" fill="url(#brass)" stroke="#f3dfab"/>' +
    '<circle cx="150" cy="8" r="7" fill="none" stroke="#d7b97b" stroke-width="4"/>' +
    '<path d="M150 47V57 M150 109V119 M114 83H124 M176 83H186" stroke="#f0d5a3" stroke-width="3"/>' +
    '<text x="150" y="55" text-anchor="middle" fill="#e2cbac" font-family="Georgia,serif" font-size="10">XII</text>' +
    '<text x="150" y="119" text-anchor="middle" fill="#e2cbac" font-family="Georgia,serif" font-size="10">VI</text>' +
    '<path d="M150 83L150 57 M150 83L173 94" stroke="#fbe8bc" stroke-width="5" stroke-linecap="round"/>' +
    '<circle cx="150" cy="83" r="5" fill="#e0b978"/><circle cx="150" cy="83" r="2" fill="#fff4d3"/>',
  Raven:
    '<circle cx="194" cy="56" r="44" fill="url(#moon)" opacity=".79"/>' +
    '<path d="M37 129Q120 116 257 133" fill="none" stroke="#655c5a" stroke-width="9" stroke-linecap="round"/>' +
    '<path d="M88 115Q106 99 116 77L128 52 148 45 166 24 176 26 188 44 210 50 190 59 181 78 189 107Q168 105 158 99L144 122Q125 111 118 108Z" fill="#080e1b" stroke="#8b94a7" stroke-width="2.5"/>' +
    '<path d="M116 83Q143 66 166 66Q150 97 135 110L126 93Z" fill="#253143" stroke="#697892" stroke-width="2"/>' +
    '<path d="M187 45L210 50 187 56Z" fill="#b6a08b"/><circle cx="177" cy="42" r="3" fill="#fff0b0"/>' +
    '<path d="M148 121L144 133 M155 117L159 133" stroke="#9b8e78" stroke-width="3"/>' +
    '<path d="M111 79Q102 63 105 52 M116 84Q95 77 92 62" fill="none" stroke="#445069" stroke-width="2"/>',
  Key:
    '<g transform="rotate(-34 150 83)">' +
    '<circle cx="109" cy="82" r="35" fill="#172431" stroke="url(#brass)" stroke-width="10"/>' +
    '<circle cx="109" cy="82" r="24" fill="none" stroke="#f6ddb0" stroke-width="2"/>' +
    '<path d="M104 65L109 58 114 65 117 74 109 79 101 74Z" fill="url(#brass)"/>' +
    '<circle cx="109" cy="83" r="10" fill="#7c5a4b" stroke="#e9cc94" stroke-width="3"/>' +
    '<path d="M145 82H230V100H212V91H198V100H181V91H145Z" fill="url(#brass)" stroke="#ffe6b3" stroke-width="3"/>' +
    '<path d="M151 78H222" stroke="#fff0cb" stroke-width="2"/><circle cx="163" cy="82" r="5" fill="#8f624b"/>' +
    '</g>',
  Portrait:
    '<path d="M93 24Q150 -1 207 24V133Q150 156 93 133Z" fill="url(#brass)" stroke="#e8d5ab" stroke-width="3"/>' +
    '<path d="M103 31Q150 13 197 31V123Q150 143 103 123Z" fill="#263142" stroke="#9c846c" stroke-width="3"/>' +
    '<path d="M107 116Q118 94 137 91H164Q184 96 193 117V128Q150 145 107 128Z" fill="#68566c"/>' +
    '<path d="M120 123Q135 100 150 106Q170 100 182 123" fill="#4f3c56" stroke="#a98981" stroke-width="2"/>' +
    '<path d="M127 66Q121 37 150 29Q181 35 173 70L168 86Q152 103 136 83Z" fill="#352935"/>' +
    '<ellipse cx="150" cy="71" rx="20" ry="29" fill="#cab5a9"/>' +
    '<path d="M130 71Q126 38 148 38Q173 40 170 70L167 60Q155 58 148 47Q139 62 129 62Z" fill="#27232b"/>' +
    '<path d="M141 72H146 M154 72H159 M145 86Q150 90 155 86" stroke="#665861" stroke-width="2" stroke-linecap="round"/>' +
    '<path d="M150 26V18 M115 38L106 29 M185 38L194 29" stroke="#fff2c9" stroke-width="2" opacity=".6"/>',
  Rose:
    '<path d="M153 80Q148 112 155 143" fill="none" stroke="#5d886d" stroke-width="8"/>' +
    '<path d="M150 102Q106 88 111 121Q133 127 150 110 M157 118Q198 94 195 129Q172 136 156 124" fill="#426b57" stroke="#81a683" stroke-width="2"/>' +
    '<path d="M152 87Q104 93 113 57Q118 37 143 37Q160 8 180 41Q218 45 187 82Q170 102 152 87Z" fill="#5e1e3d" stroke="#bf6a89" stroke-width="3"/>' +
    '<path d="M141 80Q104 64 129 47Q149 46 154 63 M155 73Q179 38 193 62Q191 83 164 91" fill="#a34d69" stroke="#e6a2ae" stroke-width="2"/>' +
    '<path d="M151 42Q163 26 178 46Q183 63 168 76Q158 61 150 64Q135 53 151 42Z" fill="#76273e" stroke="#f4adb8" stroke-width="2"/>' +
    '<path d="M152 59Q145 50 156 45Q173 52 164 66Q157 71 151 63" fill="#231827" stroke="#bd738c" stroke-width="2"/>' +
    '<path d="M132 93L140 101 127 108 M161 112L170 103 178 111" fill="none" stroke="#8aa585" stroke-width="2"/>',
  Lady:
    '<path d="M112 34Q119 5 147 17Q183 7 189 46L179 90 119 93Z" fill="#171822" stroke="#786782" stroke-width="2"/>' +
    '<path d="M140 29Q150 5 161 29 M130 32L137 12 150 24 164 12 173 32Z" fill="url(#brass)" stroke="#fff0c1" stroke-width="2"/>' +
    '<ellipse cx="150" cy="57" rx="24" ry="32" fill="#d6c6b9" stroke="#8c7281" stroke-width="2"/>' +
    '<path d="M127 50Q125 25 143 26Q173 22 176 54L167 46Q156 39 147 30Q141 47 126 51Z" fill="#171922"/>' +
    '<path d="M138 60L145 60 M155 60L162 60 M144 76Q150 79 156 76" stroke="#806b72" stroke-width="2"/>' +
    '<path d="M132 85L110 105 86 141H214L189 105 168 85 156 101 143 101Z" fill="#45324a" stroke="#be9d9e" stroke-width="3"/>' +
    '<path d="M128 95Q149 114 172 95 M116 128Q150 111 184 128 M99 140L122 106 M201 140L178 106" fill="none" stroke="url(#brass)" stroke-width="3"/>' +
    '<circle cx="150" cy="105" r="6" fill="#92b8bc" stroke="#ffeac0" stroke-width="2"/>',
  Wild:
    '<path d="M94 22Q150 0 206 22V134Q150 158 94 134Z" fill="url(#brass)" stroke="#fbe8c6" stroke-width="3"/>' +
    '<path d="M104 31Q150 13 196 31V125Q150 143 104 125Z" fill="url(#mirror)" stroke="#9ac6cb" stroke-width="4"/>' +
    '<path d="M120 102Q140 78 127 44Q160 64 157 94Q183 64 178 41Q203 93 170 116Z" fill="#93e3e4" opacity=".65"/>' +
    '<path d="M118 38L143 52 130 71 157 83 145 109 M175 29L165 54 183 72 165 94 175 117" stroke="#e9ffff" stroke-width="3" fill="none"/>' +
    '<ellipse cx="152" cy="75" rx="15" ry="28" fill="#c9f5eb" opacity=".3"/>' +
    '<path d="M115 126Q150 111 186 126" stroke="#ecfefe" stroke-width="2" fill="none"/>',
  Scatter:
    '<circle cx="148" cy="80" r="59" fill="url(#moon)" stroke="#c6b7df" stroke-width="3"/>' +
    '<circle cx="172" cy="59" r="50" fill="url(#midnight)"/>' +
    '<path d="M100 56L105 68 117 72 105 77 100 89 95 77 83 72 95 68Z M209 101L213 110 222 114 213 118 209 127 205 118 196 114 205 110Z" fill="#e0d0ff"/>' +
    '<circle cx="215" cy="39" r="3" fill="#f7e4fb"/><circle cx="86" cy="32" r="2.5" fill="#ffeabb"/>' +
    '<path d="M91 124Q149 143 213 124" fill="none" stroke="#b79fc6" stroke-width="2" stroke-dasharray="5 7"/>' +
    '<circle cx="148" cy="80" r="64" fill="none" stroke="#bcb6dc" stroke-width="2" stroke-dasharray="2 8"/>',
  Bonus:
    '<path d="M87 17L150 1 213 17V142H87Z" fill="url(#brass)" stroke="#f8e2b0" stroke-width="3"/>' +
    '<path d="M99 29L150 17 201 29V133H99Z" fill="#47303d" stroke="#93745e" stroke-width="3"/>' +
    '<path d="M110 37L144 28V126H110Z M156 28L190 37V126H156Z" fill="url(#door)" stroke="#bd9163" stroke-width="2.5"/>' +
    '<path d="M116 49L138 43V81L116 85Z M162 43L184 49V85L162 81Z M116 91L138 86V118L116 121Z M162 86L184 91V121L162 118Z" fill="#3d2935" stroke="#886658" stroke-width="1.5"/>' +
    '<path d="M150 18V133 M95 133H205" stroke="#ffe0ad" stroke-width="3"/>' +
    '<circle cx="138" cy="89" r="4.5" fill="#ffdc93"/><circle cx="162" cy="89" r="4.5" fill="#ffdc93"/>' +
    '<path d="M150 4V16" stroke="#fff4c9" stroke-width="2"/>' +
    '<path d="M126 13L118 5 M174 13L182 5" stroke="#e9d8ae" stroke-width="2"/>',
};

const palettes: Record<ManorSymbolName, [string, string]> = {
  Jack: ['#9c7f70','#202438'], Queen: ['#b88d9c','#302034'],
  King: ['#c3a172','#29263a'], Ace: ['#eed2a1','#2b2a37'],
  Candelabrum: ['#e8ae69','#3b252c'], Watch: ['#d2a55b','#292438'],
  Raven: ['#9fb5cd','#1b253b'], Key: ['#e0bb78','#352637'],
  Portrait: ['#b7a2c0','#32253b'], Rose: ['#d48ba9','#381d31'],
  Lady: ['#f2dcc1','#31233f'], Wild: ['#97f1ed','#183b48'],
  Scatter: ['#d8caff','#292448'], Bonus: ['#efc68d','#3c2334'],
};

/** Suitable for 2x/4x DPI display without upscaling a 128px bitmap. */
export function manorSymbolSvg(name: ManorSymbolName): string {
  const [accent, base] = palettes[name];
  return '<svg xmlns="http://www.w3.org/2000/svg" width="600" height="320" viewBox="0 0 300 160">' +
    '<defs>' +
    '<radialGradient id="night"><stop stop-color="' + base + '"/><stop offset="1" stop-color="#111522"/></radialGradient>' +
    '<linearGradient id="brass" x1="0" x2="1" y1="0" y2="1"><stop stop-color="#fff0bc"/><stop offset=".28" stop-color="#b88d53"/><stop offset=".54" stop-color="#f1d6a1"/><stop offset="1" stop-color="#846044"/></linearGradient>' +
    '<linearGradient id="ivory" x1="0" x2="0" y1="0" y2="1"><stop stop-color="#ffebc1"/><stop offset="1" stop-color="' + accent + '"/></linearGradient>' +
    '<linearGradient id="flame" x1="0" x2=".8" y1="0" y2="1"><stop stop-color="#fff7dc"/><stop offset=".55" stop-color="#ffc66e"/><stop offset="1" stop-color="#c55d51"/></linearGradient>' +
    '<linearGradient id="midnight" x1="0" x2="1" y1="0" y2="1"><stop stop-color="#24283d"/><stop offset="1" stop-color="#10131c"/></linearGradient>' +
    '<radialGradient id="moon"><stop stop-color="#f8ebcf"/><stop offset=".68" stop-color="#d8d4d2"/><stop offset="1" stop-color="#8a86ac"/></radialGradient>' +
    '<linearGradient id="mirror" x1="0" x2=".9" y1="0" y2="1"><stop stop-color="#81bec9"/><stop offset=".5" stop-color="#263e51"/><stop offset="1" stop-color="#c2e9de"/></linearGradient>' +
    '<linearGradient id="door" x1="0" x2=".7" y1="0" y2="1"><stop stop-color="#77505a"/><stop offset="1" stop-color="#291d2b"/></linearGradient>' +
    '</defs>' +
    '<rect x="3" y="3" width="294" height="154" rx="14" fill="url(#night)" stroke="' + accent + '" stroke-opacity=".72" stroke-width="2"/>' +
    '<rect x="10" y="10" width="280" height="140" rx="10" fill="none" stroke="#d6b986" stroke-opacity=".32" stroke-width="1.5"/>' +
    '<path d="M19 31V20H30 M270 20H281V31 M19 129V140H30 M270 140H281V129" fill="none" stroke="' + accent + '" stroke-width="2.5" stroke-opacity=".9"/>' +
    '<path d="M38 23H100 M200 23H262 M38 137H97 M203 137H262" stroke="' + accent + '" stroke-opacity=".2"/>' +
    '<ellipse cx="150" cy="145" rx="102" ry="9" fill="#050811" opacity=".4"/>' +
    artwork[name] +
    '</svg>';
}

/** Loads each illustration once. Failed SVG decodes keep the existing Pixi fallback. */
export async function loadManorArtwork(): Promise<Map<ManorSymbolName, HTMLImageElement>> {
  const entries = await Promise.all(MANOR_SYMBOL_NAMES.map(async name => {
    const image = new Image();
    try {
      await new Promise<void>((resolve, reject) => {
        image.onload = () => resolve();
        image.onerror = () => reject(new Error('SVG decode failed'));
        image.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(manorSymbolSvg(name));
      });
      return [name, image] as const;
    } catch {
      return null;
    } finally {
      image.onload = null;
      image.onerror = null;
    }
  }));
  return new Map(entries.filter((entry): entry is readonly [ManorSymbolName, HTMLImageElement] => entry !== null));
}
