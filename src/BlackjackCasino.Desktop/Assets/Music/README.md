# Original casino themes, revision 2

| Mode | Theme | Score | Instrumentation |
| --- | --- | --- | --- |
| Blackjack | Felt after hours | F-minor chill hip-hop / lo-fi, 72 BPM, 16 bars of 4/4 | Sampled Rhodes, jazz guitar, finger bass, swung closed hats, soft snare and kick, filtered top end and quiet tape dust |
| Midnight Reels | Brass and lucky sevens | F-major casino swing, 104 BPM, 16 bars of 4/4 | Muted trumpet hook, acoustic piano, nylon-guitar comping, walking upright bass and brushed kit |
| Midnight Manor | The clockwork waltz | D-minor haunted chamber waltz, 62 BPM, 16 bars of 3/4 | Celesta melody, harp, cello and slow strings; no hip-hop drum loop |

These are original scores written for this project. They replace the earlier
shared oscillator-based compositions. The user's video reference could not be
played in the build environment; the user clarified the intended direction as
“Chill Hip Hop Lofi”. No audio was copied from that video.

The instrument performances were rendered offline with FluidSynth 2.3.4 and
**GeneralUser GS 2.0.3 by S. Christian Collins**, then mixed to 32kHz stereo PCM.
GeneralUser GS permits music creation and distribution of recordings; its full
license is included alongside these files. Source:
https://github.com/mrbumpy409/GeneralUser-GS at
`684543d5e5efaef08d02be50dcda8d552478fa60`.

Only finished WAVs are shipped; the app has no SoundFont, synth-library, Python,
network or new package requirement. `CasinoMusicSynthesis` reads embedded WAVs,
resamples them once to the shared mixer rate and retains their individual themes.
The same resources are embedded in the tests and audio audition tool.

`tools/ComposeMusic/render_tracks.py` contains the complete reproducible scores,
mix processing and render instructions. It requires NumPy/SciPy and the external
FluidSynth library/SoundFont on the render machine only. Each score is repeated
to warm instrument release and reverb tails before taking its loop. A 3ms endpoint
correction removes a DC seam without inserting silence or shortening a bar.
`render-report.json` records lengths and signal levels. Runtime looping remains
on the PCM sample clock, independent of WPF timers.


## Midnight Dice — A quiet roll

The dice table adds an independent original 68 BPM / 64-beat composition. It uses
warm electric-piano-style chords, sparse plucked melody, mellow bass and very light
brush percussion. `DiceLoungeSynthesis.cs` contains the complete reproducible score
and timbres, rendered once off the UI thread and cached in memory. It needs no
sample library or downloaded assets. Circular room reflections preserve note tails
at the loop boundary. Existing WAV-based soundtracks are unchanged. The audio
review tool exports `dicelounge-loop.wav` and the quiet `diceroll.wav` cue alongside
the existing audition files.
