param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$version = "4.5.1"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "games/first-person-room"
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$work = Join-Path ([System.IO.Path]::GetTempPath()) "first-person-room-godot-$version"
$editorDir = Join-Path $work "editor"
$editorZip = Join-Path $work "editor.zip"
$editor = Join-Path $editorDir "Godot_v$version-stable_win64_console.exe"
$templates = Join-Path $env:APPDATA "Godot/export_templates/$version.stable"
$windowsTemplate = Join-Path $templates "windows_release_x86_64.exe"
$base = "https://github.com/godotengine/godot-builds/releases/download/$version-stable"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
New-Item -ItemType Directory -Force $work, $editorDir, $output | Out-Null

if (!(Test-Path $editor)) {
    Invoke-WebRequest "$base/Godot_v$version-stable_win64.exe.zip" -OutFile $editorZip
    Expand-Archive $editorZip $editorDir -Force
}
if (!(Test-Path $windowsTemplate)) {
    $templatesZip = Join-Path $work "templates.tpz"
    Invoke-WebRequest "$base/Godot_v$version-stable_export_templates.tpz" -OutFile $templatesZip
    New-Item -ItemType Directory -Force $templates | Out-Null
    $archive = [System.IO.Compression.ZipFile]::OpenRead($templatesZip)
    try {
        $entry = $archive.GetEntry("templates/windows_release_x86_64.exe")
        if ($null -eq $entry) { throw "The Windows x64 Godot export template is missing." }
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $windowsTemplate, $true)
    } finally { $archive.Dispose() }
    Remove-Item $templatesZip -Force
}

function Invoke-Godot([string[]]$GodotArgs) {
    $lines = @(& $editor @GodotArgs 2>&1)
    $exitCode = $LASTEXITCODE
    $lines | ForEach-Object { Write-Host $_ }
    if ($exitCode -ne 0) { throw "Godot exited with code $exitCode ($($GodotArgs -join ' '))." }
    return $lines
}

Invoke-Godot @("--headless", "--path", $project, "--editor", "--import")
$smoke = Invoke-Godot @("--headless", "--path", $project, "--script", "res://smoke_test.gd")
if (($smoke -join [Environment]::NewLine) -notmatch "First person room smoke PASS") {
    throw "The first-person-room smoke-test PASS marker was not found."
}
Invoke-Godot @("--headless", "--path", $project, "--export-release", "Windows Desktop", (Join-Path $output "FirstPersonRoom.exe"))
foreach ($file in @("FirstPersonRoom.exe", "FirstPersonRoom.pck")) {
    if (!(Test-Path (Join-Path $output $file))) { throw "Godot export missing: $file" }
}
Copy-Item (Join-Path $root "games/midnight-descent/licenses/GODOT-LICENSE.txt") $output -Force
Copy-Item (Join-Path $root "games/midnight-descent/licenses/GODOT-COPYRIGHT.txt") $output -Force
Write-Host "First person room exported to $output"
