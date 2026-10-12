<# Runs one already-built PvE artifact as a playing host and two real joiner processes.
   Uses the shipped Host/Join controls, checks owner commitment/hero/privacy and live enemy rails,
   then inspects each player's board-camera capture. Never builds or deletes old evidence. #>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Executable,
    [Parameter(Mandatory = $true)][string] $EvidenceDirectory,
    [int] $Port = 19535,
    [int] $TimeoutSeconds = 180
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$artifactPath = (Resolve-Path -LiteralPath $Executable).Path
$evidencePath = [System.IO.Path]::GetFullPath($EvidenceDirectory)
if (Test-Path -LiteralPath $evidencePath) { throw "Evidence directory already exists: $evidencePath" }
New-Item -ItemType Directory -Path $evidencePath | Out-Null
$players = @()
try {
    foreach ($seat in 1..3) {
        $logPath = Join-Path $evidencePath "player-$seat.log"
        $arguments = @('-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080',
            '-guardsPveCoopSmoke', "$seat", ('"{0}"' -f $evidencePath), "$Port",
            '-logFile', ('"{0}"' -f $logPath))
        $player = Start-Process -FilePath $artifactPath -ArgumentList $arguments -WorkingDirectory (Split-Path $artifactPath) -PassThru -WindowStyle Hidden
        $players += $player
        Write-Output "Started PvE smoke seat $seat, PID $($player.Id)"
        if ($seat -eq 1) {
            $hostDeadline = (Get-Date).AddSeconds(60)
            $hostReady = $false
            while ((Get-Date) -lt $hostDeadline -and -not $player.HasExited) {
                if (Test-Path -LiteralPath $logPath) {
                    $hostReady = [bool](Select-String -LiteralPath $logPath -SimpleMatch 'GUARDS_PVE_COOP_SMOKE: SEATED blue-player-01' -Quiet)
                    if ($hostReady) { break }
                }
                Start-Sleep -Milliseconds 250
            }
            if (-not $hostReady) { throw "Host did not claim its seat. See $logPath" }
        }
    }
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline -and @($players | Where-Object { -not $_.HasExited }).Count -gt 0) {
        Start-Sleep -Milliseconds 500
    }
    foreach ($player in $players) {
        if (-not $player.HasExited) { throw "Smoke player $($player.Id) timed out." }
        $player.WaitForExit()
        if ($player.ExitCode -ne 0) { throw "Smoke player $($player.Id) exited $($player.ExitCode). See $evidencePath" }
    }
    Add-Type -AssemblyName System.Drawing
    $frames = @()
    $identities = @()
    foreach ($seat in 1..3) {
        $logPath = Join-Path $evidencePath "player-$seat.log"
        $output = Get-Content -LiteralPath $logPath -Raw
        foreach ($marker in @('BEGIN', 'SEATED', 'READY', 'BOARD', "PASS blue-player-0$seat")) {
            if ($output -notmatch ('GUARDS_PVE_COOP_SMOKE: ' + [regex]::Escape($marker))) { throw "Seat $seat lacks $marker" }
        }
        if ($output -match 'GUARDS_PVE_COOP_SMOKE: FAIL|NullReferenceException|Shader.*not found|Failed to load') {
            throw "Seat $seat reported a player error."
        }
        $frames += [regex]::Match($output, 'GUARDS_PVE_COOP_SMOKE: BOARD[^\r\n]+').Value
        $identities += Get-Content -LiteralPath (Join-Path $evidencePath "ready-$seat") -Raw
        $bitmap = [System.Drawing.Bitmap]::new((Join-Path $evidencePath "board-$seat.png"))
        try {
            $colors = [System.Collections.Generic.HashSet[int]]::new()
            $nearWhite = 0; $nearBlack = 0; $samples = 0
            for ($y = 270; $y -lt 810; $y += 4) {
                for ($x = 480; $x -lt 1440; $x += 4) {
                    $pixel = $bitmap.GetPixel($x, $y)
                    [void]$colors.Add($pixel.ToArgb())
                    if ($pixel.R -ge 245 -and $pixel.G -ge 245 -and $pixel.B -ge 245) { $nearWhite++ }
                    if ($pixel.R -le 10 -and $pixel.G -le 10 -and $pixel.B -le 10) { $nearBlack++ }
                    $samples++
                }
            }
            if ($colors.Count -lt 12 -or $nearWhite / $samples -gt .5 -or $nearBlack / $samples -gt .5) {
                throw "Seat $seat board is flat or missing."
            }
            Write-Output "Seat $seat board: $($colors.Count) colors, white=$($nearWhite / $samples), black=$($nearBlack / $samples)"
        }
        finally { $bitmap.Dispose() }
    }
    if (@($frames | Select-Object -Unique).Count -ne 1) { throw 'Board framing differs across players.' }
    if (@($identities | Select-Object -Unique).Count -ne 1) { throw 'Build identity differs across players.' }
    Write-Output "PVE ARTIFACT SMOKE PASSED: one host, two real joiners; identity $($identities[0])"
}
finally {
    # Only the exact player processes launched above may be stopped, never an Editor or unrelated player.
    foreach ($player in $players) {
        if (-not $player.HasExited) { Stop-Process -Id $player.Id -Force }
        $player.Dispose()
    }
}
