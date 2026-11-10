param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = "Stop"
$version = "4.5.1"
$tag = "$version-stable"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "games/midnight-descent"
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$work = Join-Path ([System.IO.Path]::GetTempPath()) "midnight-descent-godot-$version"
New-Item -ItemType Directory -Force $work, $output | Out-Null
$base = "https://github.com/godotengine/godot-builds/releases/download/$tag"
$editorZip = Join-Path $work "editor.zip"
$editorDir = Join-Path $work "editor"
$templates = Join-Path $env:APPDATA "Godot/export_templates/$version.stable"
$editor = Join-Path $editorDir "Godot_v$version-stable_win64_console.exe"
if (!(Test-Path $editor)) {
    Invoke-WebRequest "$base/Godot_v$version-stable_win64.exe.zip" -OutFile $editorZip
    Expand-Archive $editorZip $editorDir -Force
}
if (!(Test-Path (Join-Path $templates "windows_release_x86_64.exe"))) {
    $templateZip = Join-Path $work "templates.zip"
    Invoke-WebRequest "$base/Godot_v$version-stable_export_templates.tpz" -OutFile $templateZip
    Expand-Archive $templateZip (Join-Path $work "templates") -Force
    New-Item -ItemType Directory -Force $templates | Out-Null
    Copy-Item (Join-Path $work "templates/templates/windows_release_x86_64.exe") $templates -Force
}
function Invoke-Godot([string[]]$GodotArgs) {
    & $editor @GodotArgs
    if ($LASTEXITCODE -ne 0) { throw "Godot failed: $GodotArgs ($LASTEXITCODE)" }
}
Invoke-Godot @("--headless", "--path", $project, "--editor", "--import")
$smoke = Invoke-Godot @("--headless", "--path", $project, "--quit-after", "240", "--", "--smoke-test")
$smoke | Write-Host
if (($smoke -join "`n") -notmatch "Midnight Descent menu smoke PASS") { throw "Godot menu smoke did not complete." }
Invoke-Godot @("--headless", "--path", $project, "--export-release", "Windows Desktop", (Join-Path $output "MidnightDescent.exe"))
foreach ($file in @("MidnightDescent.exe", "MidnightDescent.pck")) {
    if (!(Test-Path (Join-Path $output $file))) { throw "Missing Godot export: $file" }
}
Write-Host "Midnight Descent exported to $output"
