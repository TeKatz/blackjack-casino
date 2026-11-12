param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$version = "4.5.1"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "games/starship-explorer"
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$work = Join-Path ([System.IO.Path]::GetTempPath()) "starship-explorer-godot-$version"
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
        if ($null -eq $entry) { throw "Godot Windows x64 export template not found." }
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $windowsTemplate, $true)
    } finally { $archive.Dispose() }
    Remove-Item $templatesZip -Force
}

function Invoke-Godot([string[]]$GodotArgs) {
    $lines = @(& $editor @GodotArgs 2>&1)
    $exitCode = $LASTEXITCODE
    $lines | ForEach-Object { Write-Host $_ }
    $diagnostics = $lines -join [Environment]::NewLine
    if ($exitCode -ne 0) { throw "Godot failed with exit code $exitCode for $($GodotArgs -join ' ')." }
    if ($diagnostics -match "(?m)(SCRIPT ERROR:|Parse Error:|Failed to load script|^ERROR:)") { throw "Godot reported a script or runtime error for $($GodotArgs -join ' ')." }
    return $lines
}

Invoke-Godot @("--headless", "--path", $project, "--editor", "--import")
$smoke = Invoke-Godot @("--headless", "--path", $project, "--script", "res://smoke_test.gd")
if (($smoke -join [Environment]::NewLine) -notmatch "Starship explorer smoke PASS") {
    throw "The Starship explorer smoke PASS marker was not found."
}
Invoke-Godot @("--headless", "--path", $project, "--export-release", "Windows Desktop", (Join-Path $output "StarshipExplorer.exe"))
foreach ($file in @("StarshipExplorer.exe", "StarshipExplorer.pck")) {
    if (!(Test-Path (Join-Path $output $file))) { throw "Missing export file: $file" }
}
Copy-Item (Join-Path $root "games/midnight-descent/licenses/GODOT-LICENSE.txt") $output -Force
Copy-Item (Join-Path $root "games/midnight-descent/licenses/GODOT-COPYRIGHT.txt") $output -Force
Write-Host "Starship explorer exported to $output"
