# Presentation layer

Shared state templates live in `CasinoControls.xaml`. Read
[the presentation regression contract](../../../docs/presentation-regression.md)
before modifying dropdowns or scaling. Run the contract checker and Windows
presentation smoke; the dedicated workflow and Development publisher enforce them.

Music, cues and native PCM playback are documented in
[the audio overhaul](../../../docs/audio-overhaul.md). Rendered music credits and
source scores live in `Assets/Music`. Financial game state never depends on UI or
audio timing.
