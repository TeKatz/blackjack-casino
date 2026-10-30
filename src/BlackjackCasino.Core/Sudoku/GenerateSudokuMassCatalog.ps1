param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Difficulties = @('relaxed', 'easy', 'medium', 'hard', 'expert', 'master')
$Permutations = @(
    @(0, 1, 2), @(0, 2, 1), @(1, 0, 2),
    @(1, 2, 0), @(2, 0, 1), @(2, 1, 0)
)

function Get-PermutationFromRank {
    param([int]$Rank, [object[]]$Items)
    $remaining = [System.Collections.Generic.List[object]]::new()
    foreach ($item in $Items) { $remaining.Add($item) }
    $result = [System.Collections.Generic.List[object]]::new()
    for ($size = $remaining.Count; $size -gt 0; $size--) {
        $factorial = 1
        for ($i = 2; $i -le ($size - 1); $i++) { $factorial *= $i }
        $index = if ($size -eq 1) { 0 } else { [Math]::Floor($Rank / $factorial) }
        $Rank = if ($size -eq 1) { 0 } else { $Rank % $factorial }
        $result.Add($remaining[$index])
        $remaining.RemoveAt($index)
    }
    return ,$result.ToArray()
}

function Get-HashBytes {
    param([string]$Key)
    return [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Key))
}

function Transform-Grid {
    param(
        [string]$Grid,
        [byte[]]$Hash
    )

    $digitRank = [BitConverter]::ToUInt32($Hash, 0) % 362880
    $digitPermutation = Get-PermutationFromRank -Rank $digitRank -Items @('1','2','3','4','5','6','7','8','9')
    $digitMap = @{}
    for ($i = 0; $i -lt 9; $i++) { $digitMap[[string]($i + 1)] = [string]$digitPermutation[$i] }

    $bandOrder = $Permutations[$Hash[4] % 6]
    $stackOrder = $Permutations[$Hash[5] % 6]
    $rowMap = [System.Collections.Generic.List[int]]::new()
    $columnMap = [System.Collections.Generic.List[int]]::new()

    for ($group = 0; $group -lt 3; $group++) {
        $rows = $Permutations[$Hash[6 + $group] % 6]
        foreach ($row in $rows) { $rowMap.Add(3 * [int]$bandOrder[$group] + [int]$row) }
        $columns = $Permutations[$Hash[9 + $group] % 6]
        foreach ($column in $columns) { $columnMap.Add(3 * [int]$stackOrder[$group] + [int]$column) }
    }

    $transpose = ($Hash[12] -band 1) -ne 0
    $builder = [System.Text.StringBuilder]::new(81)
    for ($row = 0; $row -lt 9; $row++) {
        for ($column = 0; $column -lt 9; $column++) {
            if ($transpose) {
                $sourceRow = $columnMap[$column]
                $sourceColumn = $rowMap[$row]
            } else {
                $sourceRow = $rowMap[$row]
                $sourceColumn = $columnMap[$column]
            }
            $value = [string]$Grid[$sourceRow * 9 + $sourceColumn]
            if ($value -eq '0') { [void]$builder.Append('0') }
            else { [void]$builder.Append($digitMap[$value]) }
        }
    }
    return $builder.ToString()
}

$sourceText = [IO.File]::ReadAllText($Source)
$pattern = 'P\("(?<id>(?<pool>normal|daily)-(?<difficulty>relaxed|easy|medium|hard|expert|master)-\d+)",\s*SudokuDifficulty\.[A-Za-z]+,\s*SudokuPuzzlePool\.[A-Za-z]+,\s*"(?<grid>[0-9]{81})"\)'
$matches = [regex]::Matches($sourceText, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
if ($matches.Count -ne 48) {
    throw "Expected 48 seed Sudoku entries in SudokuLibrary.cs, found $($matches.Count)."
}

$normalSeeds = @{}
foreach ($difficulty in $Difficulties) { $normalSeeds[$difficulty] = [System.Collections.Generic.List[string]]::new() }
$seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($match in $matches) {
    $grid = $match.Groups['grid'].Value
    if (-not $seen.Add($grid)) { throw "Duplicate seed Sudoku detected." }
    if ($match.Groups['pool'].Value -ieq 'normal') {
        $normalSeeds[$match.Groups['difficulty'].Value.ToLowerInvariant()].Add($grid)
    }
}
foreach ($difficulty in $Difficulties) {
    if ($normalSeeds[$difficulty].Count -ne 4) {
        throw "Expected four normal seeds for $difficulty, found $($normalSeeds[$difficulty].Count)."
    }
}

$lines = [System.Collections.Generic.List[string]]::new(12954)
function Add-Supplement {
    param([string]$Pool, [string]$Difficulty, [int]$Count)
    $added = 0
    $counter = 0
    while ($added -lt $Count) {
        $hash = Get-HashBytes "midnight-sudoku-mass-v1|$Pool|$Difficulty|$counter"
        $seedIndex = $hash[13] % 4
        $candidate = Transform-Grid -Grid $normalSeeds[$Difficulty][$seedIndex] -Hash $hash
        $counter++
        if (-not $seen.Add($candidate)) { continue }
        $lines.Add("$Pool|$Difficulty|$candidate")
        $added++
        if ($counter -gt 1000000) { throw "Could not build enough unique $Pool/$Difficulty puzzles." }
    }
}

# Keep the first four legacy IDs in each bucket intact. The generated supplement fills
# indices 005..1500 for Free Play and 005..667 for Daily.
foreach ($difficulty in $Difficulties) { Add-Supplement -Pool 'normal' -Difficulty $difficulty -Count 1496 }
foreach ($difficulty in $Difficulties) { Add-Supplement -Pool 'daily' -Difficulty $difficulty -Count 663 }

if ($lines.Count -ne 12954) { throw "Expected 12,954 supplement puzzles, built $($lines.Count)." }
if ($seen.Count -ne 13002) { throw "Expected 13,002 globally distinct puzzles including seeds, found $($seen.Count)." }

$directory = Split-Path -Parent $Output
if ($directory) { [IO.Directory]::CreateDirectory($directory) | Out-Null }
$header = @(
    '# Midnight Club Sudoku mass catalog v1',
    '# Fixed build-time catalog; the application never generates Sudoku puzzles at runtime.',
    '# pool|difficulty|81-character givens'
)
[IO.File]::WriteAllLines($Output, @($header + $lines), [Text.UTF8Encoding]::new($false))
Write-Host "Sudoku catalog: 13,002 total / 9,000 normal / 4,002 daily / 0 overlaps."
