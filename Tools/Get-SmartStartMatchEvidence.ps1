param([string]$BuildRoot = (Join-Path $PSScriptRoot '../Builds'))
$ErrorActionPreference = 'Stop'
$files = @(Get-ChildItem -LiteralPath $BuildRoot -Recurse -File -Filter '*.log')
$records = @()
$online = @()
foreach ($file in $files) {
    $lines = @(Get-Content -LiteralPath $file.FullName)
    foreach ($line in @($lines | Where-Object { $_ -match '^GD_EVIDENCE_BEGIN ' })) {
        $fields = @{}
        foreach ($pair in ($line -split ' ' | Select-Object -Skip 1)) {
            $parts = $pair -split '=',2
            $fields[$parts[0]] = [Uri]::UnescapeDataString($parts[1])
        }
        $online += [pscustomobject]@{
            Run = $fields.run; Role = $fields.role; Format = $fields.format; Build = $fields.build
            Path = $file.FullName.Substring((Resolve-Path $BuildRoot).Path.Length + 1)
            Completed = [bool]@($lines | Where-Object { $_ -match '^GD_EVIDENCE_END ' })
            ModeEnded = [bool]@($lines | Where-Object { $_ -match '^GD_EVIDENCE_MODE_END ' })
        }
    }
    if (!$lines.Count -or $lines[0] -notmatch '^# guards-gameplay-log/') { continue }
    $starts = @($lines | Where-Object { $_ -match ' SESSION MatchStarted \|' })
    $ends = @($lines | Where-Object { $_ -match ' #\d+ GameEnded \|' })
    if ($starts.Count -gt 1) { throw "Multiple starts require manual segmentation: $($file.FullName)" }
    $claims = @($lines | Where-Object { $_ -match ' SESSION SeatClaimed \|' })
    $seats = @($claims | ForEach-Object { if ($_ -match 'seat=(\S+)') { $Matches[1] } } | Sort-Object -Unique)
    $start = $null; $end = $null; $minutes = $null
    if ($starts.Count) { $start = [DateTimeOffset]::Parse(($starts[0] -split ' ')[0]) }
    if ($ends.Count) { $end = [DateTimeOffset]::Parse(($ends[0] -split ' ')[0]) }
    $dated = @($lines | Where-Object { $_ -match '^\d{4}-\d\d-\d\dT' })
    if ($start -and $dated.Count) {
        $last = [DateTimeOffset]::Parse(($dated[-1] -split ' ')[0])
        if ($end) { $last = $end }
        $minutes = ($last - $start).TotalMinutes
    }
    $records += [pscustomobject]@{
        Path = $file.FullName.Substring((Resolve-Path $BuildRoot).Path.Length + 1)
        Hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        Start = $start; End = $end; Minutes = $minutes; Seats = $seats.Count
        LocalClaims = @($claims | Where-Object { $_ -match 'ordinal=-' }).Count
        EndSignature = if ($ends.Count) { $ends[0] -replace '^\S+ ', '' } else { $null }
    }
}
# Exact files first; start timestamp collisions are flagged, never silently counted twice.
$unique = @($records | Group-Object Hash | ForEach-Object { $_.Group[0] })
$started = @($unique | Where-Object Start)
$collisions = @($started | Group-Object { $_.Start.ToString('o') } | Where-Object Count -gt 1)
if ($collisions.Count) { throw 'Repeated start timestamps require session-level review.' }
$completed = @($started | Where-Object End)
# Descriptive long-session cohort, NOT a classifier proving human participation.
$long = @($completed | Where-Object { $_.Seats -eq 6 -and $_.Minutes -ge 30 })
$unfinishedLong = @($started | Where-Object { !$_.End -and $_.Seats -eq 6 -and $_.Minutes -ge 30 })
[pscustomobject]@{
    LogFiles = $files.Count; GameplayFiles = $records.Count; UniqueGameplayFiles = $unique.Count
    Started = $started.Count; Completed = $completed.Count
    TerminalWithoutStart = @($unique | Where-Object { $_.End -and !$_.Start }).Count
    LongCompleted = $long.Count; LongCompletedHours = ($long | Measure-Object Minutes -Sum).Sum / 60
    LongUnfinished = $unfinishedLong.Count
    OnlineFiles = $online.Count
    OnlineRuns = @($online | Group-Object Run).Count
    OnlineCompletedRuns = @($online | Where-Object Completed | Group-Object Run).Count
    OnlineCompletedOutsideSmoke = @($online | Where-Object { $_.Completed -and $_.Path -notmatch '^Win64Smoke[\\/]' } | Group-Object Run).Count
    OnlineModeEndedRuns = @($online | Where-Object ModeEnded | Group-Object Run).Count
    Online = $online
    Records = @($unique | Sort-Object Start,Path)
} | ConvertTo-Json -Depth 4
