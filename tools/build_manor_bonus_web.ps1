param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$version = "4.5.1"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "games/manor-bonus-3d"
$output = [IO.Path]::GetFullPath($OutputDirectory)
# Share the pinned editor/template download with Midnight Descent.
$work = Join-Path ([IO.Path]::GetTempPath()) "midnight-descent-godot-$version"
$editorDir = Join-Path $work "editor"
$editorZip = Join-Path $work "editor.zip"
$editor = Join-Path $editorDir "Godot_v$version-stable_win64_console.exe"
$templates = Join-Path $env:APPDATA "Godot/export_templates/$version.stable"
$webTemplate = Join-Path $templates "web_nothreads_release.zip"
$winTemplate = Join-Path $templates "windows_release_x86_64.exe"
$base = "https://github.com/godotengine/godot-builds/releases/download/$version-stable"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
New-Item -ItemType Directory -Force $work, $editorDir, $output, $templates | Out-Null

if (!(Test-Path $editor)) {
    Invoke-WebRequest "$base/Godot_v$version-stable_win64.exe.zip" -OutFile $editorZip
    Expand-Archive $editorZip $editorDir -Force
}
if (!(Test-Path $webTemplate) -or !(Test-Path $winTemplate)) {
    $templateArchivePath = Join-Path $work "godot-templates.tpz"
    Invoke-WebRequest "$base/Godot_v$version-stable_export_templates.tpz" -OutFile $templateArchivePath
    $archive = [IO.Compression.ZipFile]::OpenRead($templateArchivePath)
    try {
        foreach ($name in @("web_nothreads_release.zip", "windows_release_x86_64.exe")) {
            $destination = Join-Path $templates $name
            if (Test-Path $destination) { continue }
            $entry = $archive.GetEntry("templates/$name")
            if ($null -eq $entry) { throw "Missing Godot $version export template: $name" }
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $true)
        }
    } finally { $archive.Dispose() }
    Remove-Item $templateArchivePath -Force
}

function Invoke-Godot([string[]]$Parameters) {
    $lines = @(& $editor @Parameters 2>&1)
    $status = $LASTEXITCODE
    $lines | ForEach-Object { Write-Host $_ }
    if ($status -ne 0) { throw "Godot exited with code $status for $($Parameters -join ' ')." }
    if (($lines -join [Environment]::NewLine) -match "(?m)(SCRIPT ERROR:|Parse Error:|Failed to load script|^ERROR:)") {
        throw "Godot reported script/import errors for $($Parameters -join ' ')."
    }
    return $lines
}

Invoke-Godot @("--headless", "--path", $project, "--editor", "--import")
$testOutput = Invoke-Godot @("--headless", "--path", $project, "--script", "res://smoke_test.gd")
if (($testOutput -join [Environment]::NewLine) -notmatch "Manor 3D web bonus smoke PASS") {
    throw "Manor 3D runtime smoke did not pass."
}
$roomTest = Invoke-Godot @("--headless", "--path", $project, "--script", "res://room_geometry_test.gd")
if (($roomTest -join [Environment]::NewLine) -notmatch "Manor 3D room geometry PASS") {
    throw "Manor room geometry and texture contract did not pass."
}
Invoke-Godot @("--headless", "--path", $project, "--export-release", "Web", (Join-Path $output "index.html"))

foreach ($name in @("index.html", "index.js", "index.wasm", "index.pck")) {
    if (!(Test-Path (Join-Path $output $name))) {
        throw "Godot Web export is incomplete: $name"
    }
}
if ((Get-Item (Join-Path $output "index.wasm")).Length -lt 1000000) {
    throw "The Godot WebAssembly engine is suspiciously small."
}
Copy-Item (Join-Path $root "games/midnight-descent/licenses/GODOT-LICENSE.txt") $output -Force
Copy-Item (Join-Path $root "games/midnight-descent/licenses/GODOT-COPYRIGHT.txt") $output -Force
Write-Host "Validated single-threaded Manor 3D web bonus at $output"
