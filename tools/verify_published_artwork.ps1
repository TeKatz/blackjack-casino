param(
    [Parameter(Mandatory = $true)][string]$AppDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$app = (Resolve-Path $AppDirectory).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$report = Join-Path $output 'packaged-artwork.json'
$working = Join-Path ([System.IO.Path]::GetTempPath()) ('casino-packaged-artwork-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $working | Out-Null
try {
    $start = [System.Diagnostics.ProcessStartInfo]::new((Join-Path $app 'BlackjackCasino.Desktop.exe'))
    $start.UseShellExecute = $false
    $start.WorkingDirectory = $working
    $start.ArgumentList.Add('--packaged-artwork-smoke')
    $start.ArgumentList.Add($report)
    $process = [System.Diagnostics.Process]::Start($start)
    try {
        if (!$process.WaitForExit(45000)) {
            $process.Kill($true)
            throw 'Published single-file artwork smoke timed out.'
        }
        if (!(Test-Path $report)) { throw 'Published executable did not produce its artwork report.' }
        $result = Get-Content $report -Raw | ConvertFrom-Json
        if ($process.ExitCode -ne 0 -or !$result.Success) {
            throw ('Published single-file artwork smoke failed: ' + $result.Error)
        }
        if (!$result.SingleFile -or $result.EmbeddedImages.Count -ne 16 -or $result.BrowserArtwork.Count -ne 11) {
            throw 'Published artwork report has incomplete embedded/browser coverage.'
        }
        Write-Host 'PASS: published single-file executable decoded all 16 WPF crops/rooms and 11 browser assets from an unrelated working directory.'
    }
    finally { $process.Dispose() }
}
finally { Remove-Item $working -Recurse -Force }
