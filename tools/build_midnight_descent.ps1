param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
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
    # The official archive includes every platform (over 1 GB). Extract only Windows x64.
    New-Item -ItemType Directory -Force $templates | Out-Null
    $archive = [System.IO.Compression.ZipFile]::OpenRead($templateZip)
    try {
        $entry = $archive.GetEntry("templates/windows_release_x86_64.exe")
        if ($null -eq $entry) { throw "Windows release template missing from official archive." }
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $templates "windows_release_x86_64.exe"), $true)
    } finally { $archive.Dispose() }
    Remove-Item $templateZip -Force
}
function Invoke-Godot([string[]]$GodotArgs) {
    & $editor @GodotArgs
    if ($LASTEXITCODE -ne 0) { throw "Godot failed: $GodotArgs ($LASTEXITCODE)" }
}
Invoke-Godot @("--headless", "--path", $project, "--editor", "--import")
$smoke = Invoke-Godot @("--headless", "--path", $project, "--quit-after", "240", "--", "--smoke-test")
$smoke | Write-Host
if (($smoke -join "`n") -notmatch "Midnight Descent menu smoke PASS") { throw "Godot menu smoke did not complete." }
$runSmoke = Invoke-Godot @("--headless", "--path", $project, "--quit-after", "240", "res://run.tscn", "--", "--run-smoke")
$runSmoke | Write-Host
if (($runSmoke -join "`n") -notmatch "Midnight Descent gameplay scene smoke PASS") { throw "Godot gameplay scene smoke did not complete." }
Invoke-Godot @("--headless", "--path", $project, "--export-release", "Windows Desktop", (Join-Path $output "MidnightDescent.exe"))
foreach ($file in @("MidnightDescent.exe", "MidnightDescent.pck")) {
    if (!(Test-Path (Join-Path $output $file))) { throw "Missing Godot export: $file" }
}
Copy-Item (Join-Path $project "licenses/GODOT-LICENSE.txt") $output -Force
Copy-Item (Join-Path $project "licenses/GODOT-COPYRIGHT.txt") $output -Force
Write-Host "Midnight Descent exported to $output"
