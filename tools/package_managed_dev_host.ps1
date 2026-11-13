param(
    [Parameter(Mandatory = $true)][string]$LegacyDirectory,
    [Parameter(Mandatory = $true)][string]$NativeDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$legacy = (Resolve-Path $LegacyDirectory).Path
$native = (Resolve-Path $NativeDirectory).Path
$required = @(
    'BlackjackCasino.Desktop.exe',
    'BlackjackCasino.NativeShell.exe',
    'BlackjackCasino.HostCutoverLauncher.exe',
    'tools/updater/BlackjackCasino.Updater.exe',
    'ModernFrontend/lobby.html',
    'ModernFrontend/roulette.html',
    'MidnightDescent/MidnightDescent.exe',
    'MidnightDescent/MidnightDescent.pck'
)

if (!(Test-Path (Join-Path $legacy 'BlackjackCasino.Desktop.exe')) -or
    !(Test-Path (Join-Path $native 'BlackjackCasino.NativeShell.exe')) -or
    !(Test-Path (Join-Path $native 'BlackjackCasino.HostCutoverLauncher.exe'))) {
    throw 'Cannot enable managed Development NativeShell: host binaries are missing.'
}
if (Test-Path (Join-Path $legacy 'native-development-default.flag')) {
    throw 'Development NativeShell flag already exists; refusing to patch an already modified payload.'
}

# Copy the separate, smoke-tested NativeShell into the ACTUAL development
# updater payload, not only into an Actions-only rehearsal zip.
# Keep the WPF fallback and never replace shared frontend or Godot resources
# with files from a different version. On overlapping files require exact hash
# identity, so the updater cannot ship a mixed frontend build.
foreach ($file in Get-ChildItem $native -File -Recurse) {
    $relative = [System.IO.Path]::GetRelativePath($native, $file.FullName)
    $dest = Join-Path $legacy $relative
    if (Test-Path $dest) {
        $expected = (Get-FileHash -Path $dest -Algorithm SHA256).Hash
        $candidate = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash
        if ($expected -ne $candidate) {
            throw "NativeShell and WPF published files are incompatible: $relative"
        }
        continue
    }
    $parent = Split-Path $dest -Parent
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    Copy-Item $file.FullName $dest
}

# This marker exists ONLY in the Development updater package. A stable
# installation has neither marker nor auto-forwarding and stays WPF.
'NativeShell development default; use BlackjackCasino.Desktop.exe --legacy for WPF fallback.' |
    Set-Content -Path (Join-Path $legacy 'native-development-default.flag') -Encoding ascii

foreach ($path in $required) {
    if (!(Test-Path (Join-Path $legacy $path))) {
        throw "Managed Development package lacks: $path"
    }
}
$desktopVersion = (Get-Item (Join-Path $legacy 'BlackjackCasino.Desktop.exe')).VersionInfo.ProductVersion
$nativeVersion = (Get-Item (Join-Path $legacy 'BlackjackCasino.NativeShell.exe')).VersionInfo.ProductVersion
if ($desktopVersion.Split('+')[0] -ne $nativeVersion.Split('+')[0]) {
    throw "Installed WPF and NativeShell do not share a release version: $desktopVersion vs $nativeVersion"
}

Write-Host 'PASS: managed NativeShell and WPF fallback included in the actual Development updater payload.'
