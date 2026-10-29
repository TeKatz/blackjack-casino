# Casino audio overhaul

The audio is original offline synthesis, with no streamed assets, external sample
packs or additional packages. Blackjack rules, slot outcomes, bankroll and XP are
unchanged. Audio is never a clock for financial transitions.

## Music

| Mode | Track | Tempo | Loop |
| --- | --- | ---: | ---: |
| Blackjack | Laid-back hip-hop: swung hats, soft backbeat, round kick/bass, warm FM electric piano and jazz extensions | 75 BPM | 51.2 s / 16 bars |
| Midnight Reels | Casino lounge quartet: walking upright bass, brushed drums, bright keys and vibraphone | 96 BPM | 40 s / 16 bars |
| Midnight Manor | Calm Halloween chamber score: minor harmony, slow string/organ bed, celesta and felt plucks | 70 BPM | 54.857 s / 16 bars |

Notes and feed-forward room reflections are rendered circularly so the last bar's
tails carry into the first. Playback wraps on the PCM sample clock, with no file
reopening, UI timer or MediaEnded seek. Music is prepared asynchronously, cached
once per track, and quietly faded on mode/volume changes. Reels now has its own
music in both embedded and standalone entry paths; Blackjack is muted during
either slot and restored on return. Saved music enable/volume settings persist.

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
