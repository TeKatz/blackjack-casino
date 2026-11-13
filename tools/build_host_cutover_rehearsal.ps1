param(
    [Parameter(Mandatory = $true)][string]$LegacyDirectory,
    [Parameter(Mandatory = $true)][string]$NativeDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$legacy = (Resolve-Path $LegacyDirectory).Path
$native = (Resolve-Path $NativeDirectory).Path
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$output = (Resolve-Path $OutputDirectory).Path
$merged = Join-Path $output 'dual-host-files'
$zip = Join-Path $output 'blackjack-casino-dual-host-rehearsal-win-x64.zip'
if (Test-Path $merged) { throw "Rehearsal destination must be new: $merged" }
if (Test-Path $zip) { throw "Refusing to overwrite a rehearsal archive: $zip" }

foreach ($required in @(
    'BlackjackCasino.Desktop.exe',
    'tools/updater/BlackjackCasino.Updater.exe',
    'ModernFrontend/lobby.html',
    'MidnightDescent/MidnightDescent.exe',
    'MidnightDescent/MidnightDescent.pck'
)) {
    if (!(Test-Path (Join-Path $legacy $required))) {
        throw "The WPF fallback package is incomplete: $required"
    }
}
foreach ($required in @(
    'BlackjackCasino.NativeShell.exe',
    'ModernFrontend/lobby.html',
    'ModernFrontend/lounge.html',
    'MidnightDescent/MidnightDescent.exe',
    'MidnightDescent/MidnightDescent.pck'
)) {
    if (!(Test-Path (Join-Path $native $required))) {
        throw "The native candidate is incomplete: $required"
    }
}

try {
    # The updater's original artifacts/app package is never changed.
    Copy-Item $legacy $merged -Recurse -Force
    foreach ($file in Get-ChildItem $native -File -Recurse) {
        $relative = [System.IO.Path]::GetRelativePath($native, $file.FullName)
        $destination = Join-Path $merged $relative
        if (Test-Path $destination) {
            $oldHash = (Get-FileHash -Algorithm SHA256 $destination).Hash
            $newHash = (Get-FileHash -Algorithm SHA256 $file.FullName).Hash
            if ($oldHash -ne $newHash) {
                throw "Overlapping WPF/NativeShell assets differ: $relative"
            }
            continue
        }
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item $file.FullName $destination
    }
    foreach ($exe in @('BlackjackCasino.Desktop.exe', 'BlackjackCasino.NativeShell.exe')) {
        if (!(Test-Path (Join-Path $merged $exe))) { throw "Missing host executable: $exe" }
    }
    Compress-Archive -Path (Join-Path $merged '*') -DestinationPath $zip -CompressionLevel Optimal
    $checksum = (Get-FileHash $zip -Algorithm SHA256).Hash
    $metadata = [ordered]@{
        rehearsalOnly = $true
        productionApproved = $false
        legacyRestartExecutable = 'BlackjackCasino.Desktop.exe'
        nativeRestartExecutable = 'BlackjackCasino.NativeShell.exe'
        packageFile = (Split-Path $zip -Leaf)
        sha256 = $checksum
        sourceCommit = $env:GITHUB_SHA
        note = 'Offline dual-host rehearsal only. Never published to Development or stable releases.'
    }
    $metadata | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $output 'cutover-rehearsal.json') -Encoding utf8NoBOM
    "$checksum  $(Split-Path $zip -Leaf)" | Set-Content -Path (Join-Path $output 'cutover-rehearsal.sha256') -Encoding ascii
    Write-Host "PASS: both hosts, updater and Godot assets assembled. SHA256: $checksum"
}
finally {
    if (Test-Path $merged) { Remove-Item $merged -Recurse -Force }
}
