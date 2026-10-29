# Casino audio overhaul

The original casino compositions now use offline rendered instruments. No audio
was copied from the reference video. The user clarified the direction as chill
hip-hop lo-fi. Finished WAVs are embedded, so there is no runtime synth, network
or extra package dependency. [Music source/credits](../src/BlackjackCasino.Desktop/Assets/Music/README.md)
includes the score renderer and SoundFont attribution.

## Music, revision 2

| Mode | Theme | Tempo / meter | Distinctive sound |
| --- | --- | --- | --- |
| Blackjack | Felt after hours | 72 BPM / 4/4 | Sampled Rhodes, jazz guitar, finger bass and swung hip-hop drums, filtered lo-fi mix |
| Midnight Reels | Brass and lucky sevens | 104 BPM / 4/4 | Muted trumpet melody, piano, nylon guitar, walking upright bass and brushed swing |
| Midnight Manor | The clockwork waltz | 62 BPM / 3/4 | Celesta/harp motif, cello and slow strings in a haunted minor waltz |

Each track has its own score, key, arrangement and instruments. Sampler/reverb
tails are warmed through a repeated score before capturing the loop, with a tiny
endpoint correction to prevent a seam. Playback remains gapless on the shared PCM
sample clock. Track loading is asynchronous and cached; saved volume/mute and
mode-switch behavior remain intact.

## Effects

- Blackjack: new two-stage paper/felt deal, soft ascending win and descending loss.
  Existing chip and push character is retained. No buzzers or harsh alarms.
- Reels: short mechanical spin-button click and muted two-note loss. Other classic
  machine cues retain their existing synthesis.
- Manor: new softer rolling reel cue. A compatibility probe compares SHA-256 for
  every prior cue and confirms the other 26 generated WAVs are byte-for-byte
  unchanged. Their playback now overlaps instead of repeatedly seeking the same
  player to zero and cutting off its decay. Cues are preloaded before play.
- Menus: short quiet tactile tick for navigation buttons/toggles, dropdown choices
  and tab changes. Embedded content/host events are deduplicated; rapid events are
  debounced, and ordinary Deal/Hit/Spin feedback is not doubled. Initialization
  and background refreshes do not deliberately produce menu feedback.

## Playback and settings

`AudioMixer` owns independent cue voices and scene loops, with bounded polyphony,
linear PCM mixing, soft limiting and smoothed gain. `CasinoAudio` sends four
512-frame stereo PCM blocks at 44.1 kHz to one Windows output on an event-driven
worker thread. Buffer reuse follows playback order even when several buffers
finish together across the ring boundary. WPF animation stalls do not restart or
interrupt cues. WAV cues are resampled in memory once, with no first-play file or
codec open. Scene changes release their owners; app exit resets, unprepares and
closes the native buffers. A missing output device remains nonfatal.

Music and effects toggles remain separate. Effect mute fades active owned cues;
music mute ramps its loops to zero. Entering a slot no longer overrides an
intentional all-off preference. The existing maximum music volume remains 32%.
Manor's room/haunt ambience remains separate from its calm musical bed.

Windows output contract references: [waveOutOpen](https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-waveoutopen),
[waveOutWrite](https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-waveoutwrite),
[WAVEHDR](https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/ns-mmeapi-wavehdr).

## Validation and audition

Unit tests verify stereo phrase lengths, finite/quiet signal bounds, loop seams,
actual mixer traversal through two loops, overlapping tails, replacement/stop
isolation, bounded polyphony, WAV round trips and cue releases. The Windows smoke
also deliberately stalls the UI thread and verifies that the audio render clock
continues, then exercises async track preparation, volume/mute and Manor scene
cleanup. Its report records whether a real output device was available; a
headless runner cannot establish the quality of a human speaker/headphone mix.

Run `dotnet run --project tools/AudioRender -c Release -- artifacts/audio-review`
to export the exact three full loops, table/menu/new Reels cues, every Manor cue,
a repeated overlapping-effect sample and measured peak/RMS/seam report. CI
uploads these as `casino-audio-review` for audition. For Windows acceptance,
listen across at least two loop boundaries, change modes and volume, test both
mutes, and use Normal/Fast/Turbo with consecutive Manor stops/wins. Human audition
on the user's output device remains the final judgement of musical taste/mix.
