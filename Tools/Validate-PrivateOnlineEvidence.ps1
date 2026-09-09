param(
    [Parameter(Mandatory = $true)][string[]]$LogPaths,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Accept only the dedicated public-metadata captures, never general Unity logs. A matching
# bundle proves attributable declarations, not that a human played or a network was external.
function Read-Record([string]$Line, [string]$Kind, [string[]]$Fields) {
    $parts = $Line.Split(' ')
    if ($parts[0] -cne $Kind -or $parts.Count -ne $Fields.Count + 1) { throw "Invalid $Kind record." }
    $record = @{}
    foreach ($part in $parts[1..($parts.Count - 1)]) {
        $pair = $part.Split('=', 2)
        if ($pair.Count -ne 2 -or $Fields -cnotcontains $pair[0] -or $record.ContainsKey($pair[0]) -or
            $pair[1] -notmatch '^(?:[A-Za-z0-9_.~-]|%[A-Fa-f0-9]{2})+$') { throw 'Unexpected or invalid metadata field.' }
        $record[$pair[0]] = [Uri]::UnescapeDataString($pair[1])
    }
    return $record
}
function Seats([string]$Value) {
    if ($Value -ceq '-') { return @() }
    $seats = @($Value.Split(','))
    if (@($seats | Select-Object -Unique).Count -ne $seats.Count) { throw 'Duplicate declared seat.' }
    foreach ($seat in $seats) { if ($seat -cnotmatch '^(blue|red)-player-0[1-3]$') { throw 'Invalid declared seat.' } }
    return $seats
}
if ($LogPaths.Count -lt 1 -or $LogPaths.Count -gt 7) { throw 'A bundle requires one host and at most six clients.' }
$captures = @(foreach ($path in $LogPaths) {
    $file = Get-Item -LiteralPath $path
    if ($file.PSIsContainer -or $file.Length -gt 16384) { throw 'Invalid capture file.' }
    $bytes = [IO.File]::ReadAllBytes($file.FullName)
    if ($bytes.Length -gt 16384) { throw 'Capture exceeds its bound.' }
    $lines = @(([Text.Encoding]::UTF8.GetString($bytes) -replace '\r?\n$','') -split '\r?\n')
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','') } finally { $sha.Dispose() }
    if ($lines.Count -lt 2 -or $lines.Count -gt 3) { throw 'Incomplete or unexpected capture.' }
    $begin = Read-Record $lines[0] 'GD_EVIDENCE_BEGIN' @('run','process','role','seat','protocol','schema','map','format','content','build','topology')
    $end = Read-Record $lines[-1] 'GD_EVIDENCE_END' @('run','process','version','cause','winner')
    if ($begin.run -cnotmatch '^[a-f0-9]{12}-[a-f0-9]{32}$' -or $begin.process -cnotmatch '^[a-f0-9]{32}$' -or
        $end.run -cne $begin.run -or $end.process -cne $begin.process -or
        $begin.schema -notmatch '^\d+$' -or $end.version -notmatch '^\d+$' -or
        $begin.format -cnotmatch '^[23]v[23]-(quick|long)$' -or $begin.format[0] -ne $begin.format[2] -or
        $begin.topology -cne 'private-lobby-tcp' -or $end.winner -cnotmatch '^(blue|red)-team$' -or
        $end.cause -cnotin @('FinalWaveFlipped','LifeCountersExhausted','ThroneCaptured')) { throw 'Invalid capture identity or terminal outcome.' }
    $participants = $null
    if ($begin.role -ceq 'host' -and $begin.seat -ceq '-' -and $lines.Count -eq 3) {
        $participants = Read-Record $lines[1] 'GD_EVIDENCE_PARTICIPANTS' @('human','bot')
    } elseif ($begin.role -cne 'client' -or $lines.Count -ne 2 -or $begin.seat -cnotmatch '^(blue|red)-player-0[1-3]$') {
        throw 'Invalid role or missing participants.'
    }
    [pscustomobject]@{ Begin=$begin; End=$end; Participants=$participants; Source=$file.FullName; Hash=$hash }
})
$hosts = @($captures | Where-Object { $_.Begin.role -ceq 'host' })
if ($hosts.Count -ne 1) { throw 'Exactly one completed host capture is required.' }
$authority = $hosts[0]
foreach ($capture in $captures) {
    foreach ($field in @('run','protocol','schema','map','format','content','build','topology')) {
        if ($capture.Begin[$field] -cne $authority.Begin[$field]) { throw "Mixed $field evidence." }
    }
    foreach ($field in @('version','cause','winner')) {
        if ($capture.End[$field] -cne $authority.End[$field]) { throw "Terminal $field mismatch." }
    }
}
if (@($captures | ForEach-Object { $_.Begin.process } | Select-Object -Unique).Count -ne $captures.Count) { throw 'Duplicate capture identity.' }
$humans = @(Seats $authority.Participants.human)
$bots = @(Seats $authority.Participants.bot)
$expected = @(foreach ($team in @('blue','red')) { foreach ($index in 1..([int]::Parse($authority.Begin.format.Substring(0,1)))) { "$team-player-0$index" } })
$allSeats = @($humans) + @($bots)
if ($allSeats.Count -ne $expected.Count -or @($allSeats | Select-Object -Unique).Count -ne $expected.Count -or
    @(Compare-Object $expected $allSeats).Count -ne 0) { throw 'Participants do not match the declared format.' }
$clients = @($captures | Where-Object { $_.Begin.role -ceq 'client' })
if ($clients.Count -ne $humans.Count) { throw 'Missing or extra human terminal capture.' }
foreach ($seat in $humans) {
    if (@($clients | Where-Object { $_.Begin.seat -ceq $seat }).Count -ne 1) { throw 'Missing or duplicate human seat capture.' }
}
$manifest = [ordered]@{ evidenceSchema=1; identity=$authority.Begin; terminal=$authority.End; humanSeats=$humans; botSeats=$bots;
    captures=@($captures | ForEach-Object { [ordered]@{ file=($_.Begin.process + '.log'); role=$_.Begin.role; seat=$_.Begin.seat; sha256=$_.Hash } }) }
if ($OutputDirectory) {
    # Refuse existing destinations rather than overwrite any prior evidence.
    if (Test-Path -LiteralPath $OutputDirectory) { throw 'Output directory already exists.' }
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
    foreach ($capture in $captures) {
        $target = Join-Path $OutputDirectory ($capture.Begin.process + '.log')
        Copy-Item -LiteralPath $capture.Source -Destination $target
        # Use the same framework hash path used for source captures above. Depending on
        # module auto-loading here made the checker fail in otherwise-valid test and
        # operator shells where Microsoft.PowerShell.Utility was not discoverable.
        $targetBytes = [IO.File]::ReadAllBytes($target)
        $targetSha = [Security.Cryptography.SHA256]::Create()
        try { $targetHash = [BitConverter]::ToString($targetSha.ComputeHash($targetBytes)).Replace('-','') } finally { $targetSha.Dispose() }
        if ($targetHash -cne $capture.Hash) { throw 'Capture changed during collection.' }
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'manifest.json') -Encoding UTF8
}
[pscustomobject]@{ Result='Passed'; Run=$authority.Begin.run; Captures=$captures.Count; Version=$authority.End.version }
