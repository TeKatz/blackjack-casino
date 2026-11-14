param(
    [Parameter(Mandatory = $true)][string]$AppDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$app = (Resolve-Path $AppDirectory).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$reportFile = Join-Path $output 'packaged-artwork.json'
$source = Join-Path $PSScriptRoot '../src/BlackjackCasino.Assets'
$checked = [System.Collections.Generic.List[object]]::new()

function Require-PackageFile([string]$relative, [string]$expectedSha = '') {
    $path = Join-Path $app $relative
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Native release is missing $relative"
    }
    $file = Get-Item -LiteralPath $path
    if ($file.Length -le 0) { throw "Native release has an empty file: $relative" }
    $sha = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if ($expectedSha -and $sha -ne $expectedSha) {
        throw "Published art checksum mismatch: $relative"
    }
    $checked.Add([ordered]@{ path = $relative; bytes = $file.Length; sha256 = $sha })
}

try {
    Require-PackageFile 'BlackjackCasino.NativeShell.exe'
    Require-PackageFile 'BlackjackCasino.Desktop.exe'
    Require-PackageFile 'ModernFrontend/lobby.html'
    Require-PackageFile 'ModernFrontend/index.html'
    Require-PackageFile 'ModernFrontend/empire.html'
    Require-PackageFile 'ModernFrontend/reels.html'
    $manifest = Get-Content (Join-Path $source 'CollectionArt/manifest.json') -Raw |
        ConvertFrom-Json
    if ($manifest.Version -ne 1 -or @($manifest.Atlases).Count -ne 16) {
        throw 'Authoritative album atlas manifest changed unexpectedly.'
    }
    foreach ($atlas in $manifest.Atlases) {
        Require-PackageFile "ModernFrontend/album/atlas/$($atlas.File)" $atlas.Sha256
    }
    $empireImages = @(Get-ChildItem (Join-Path $source 'Empire') -Filter '*.png' -File)
    if ($empireImages.Count -ne 11) { throw 'Expected eleven original Empire images.' }
    foreach ($art in $empireImages) {
        $relative = "ModernFrontend/empire-art/$($art.Name)"
        Require-PackageFile $relative (Get-FileHash $art.FullName -Algorithm SHA256).Hash
    }
    $nineLives = @(Get-ChildItem (Join-Path $source 'NineLives') -Filter '*.jpg' -File)
    if ($nineLives.Count -ne 8) { throw 'Expected eight original Nine Lives images.' }
    foreach ($art in $nineLives) {
        Require-PackageFile "ModernFrontend/nine-lives-art/$($art.Name)" (Get-FileHash $art.FullName -Algorithm SHA256).Hash
    }
    $soundtracks = @(Get-ChildItem (Join-Path $source 'Music/Licensed') -Filter '*.mp3' -File)
    if ($soundtracks.Count -lt 4) { throw 'Original licensed soundtrack set is incomplete.' }
    foreach ($audio in $soundtracks) {
        Require-PackageFile "Music/Licensed/$($audio.Name)" (Get-FileHash $audio.FullName -Algorithm SHA256).Hash
    }
    $report = [ordered]@{
        Success = $true
        Host = 'NativeShell'
        AtlasCount = @($manifest.Atlases).Count
        EmpireCount = $empireImages.Count
        NineLivesCount = $nineLives.Count
        LicensedSoundtracks = $soundtracks.Count
        Files = $checked.ToArray()
    }
    $report | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $reportFile -Encoding utf8NoBOM
    Write-Host "PASS: NativeShell package contains $($checked.Count) verified frontend, artwork and audio files."
}
catch {
    $report = [ordered]@{ Success = $false; Error = $_.Exception.Message; Files = $checked.ToArray() }
    $report | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $reportFile -Encoding utf8NoBOM
    throw
}
