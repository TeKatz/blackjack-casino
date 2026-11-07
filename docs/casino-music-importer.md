# Casino Music Importer

This lightweight workflow imports one CC0 OpenGameArt audio file at a time without Codex or a local build.

## Using it

1. Open GitHub **Actions > Casino Music Importer > Run workflow**.
2. Leave the workflow branch on **main**. The importer intentionally commits audio to **feature/manor-modernization-phase-1**, not main.
3. Paste the **direct audio download link** from OpenGameArt (HTTPS, under /sites/default/files/, MP3/OGG/OGA/OPUS/WAV/FLAC). An ordinary /content/ page is not a direct download.
4. Paste that track's **OpenGameArt source /content/ page** and enter the artist/creator.
5. Choose one of the currently integrated locations: midnight-manor, chill-lounge, lobby, midnight-dice.
6. Read the license on OpenGameArt. Only tick **CC0 confirmed** when the source actually offers CC0; the workflow cannot authenticate authorship or licensing.
7. Check **Replace existing track** when replacing one of the four existing files.
8. Optionally check **Publish Development** if you want the expensive Windows build/release to run after the import.

The workflow downloads into a temporary directory on the standard Ubuntu runner, verifies source host and redirects, checks audio format/length/size, converts non-MP3 sources with FFmpeg, saves the MP3 to the current desktop music assets and writes the source/license claim and SHA-256 to `src/BlackjackCasino.Desktop/Assets/Music/Licensed/credits.json`.

A commit made using the GitHub Actions token **does not automatically trigger other push-based workflows**. The optional build checkbox explicitly dispatches the existing Development Release workflow. Otherwise, manually run it when ready.

Current choices are limited to the four destinations actually wired in `LicensedSoundtracks.cs`. Additional games need corresponding C# audio routing before expanding the importer.

The four music files initially uploaded by hand do not have verified source page/license records. Do not infer CC0 solely from their filenames; collect the OpenGameArt pages before wider redistribution.
