<#
.SYNOPSIS
    Player-build smoke gate: builds a Windows player, runs it, and proves it reaches a configured match.

.DESCRIPTION
    An Editor test suite cannot observe a player that ships with an unresolved shader, a missing
    resource, or a type the build stripped. This gate builds the real artifact, launches it, and
    requires the running player to report that it composed a configured match and completed one
    scripted authoritative command.

    Three failure modes are deliberately fatal rather than tolerated: a build that does not report
    Succeeded, a player that exits non-zero, and a player log that does not carry the driver's pass
    marker. The last one matters most - a player that silently degrades still exits zero.

    Uses the same fixed parameterless shape as Tools/Invoke-UnityValidation.ps1, and the same
    environment-override Unity discovery.
#>
[CmdletBinding()]
param(
    [int] $PlayerTimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unityEditorVersion = '6000.5.9f1'
$buildLog = [System.IO.Path]::Combine($projectRoot, 'tmp', 'player-build-smoke-build.log')
$playerLog = [System.IO.Path]::Combine($projectRoot, 'tmp', 'player-build-smoke-player.log')
$executablePath = [System.IO.Path]::Combine($projectRoot, 'Builds', 'Win64Smoke', 'GuardsDigitalSmoke.exe')

function Test-IsWindowsHost {
    if (Test-Path -Path 'Variable:\IsWindows') { return $IsWindows }
    return $true
}

function Resolve-ToolPath {
    param(
        [Parameter(Mandatory = $true)] [string] $OverrideVariable,
        [Parameter(Mandatory = $true)] [string[]] $Candidates,
        [Parameter(Mandatory = $true)] [string] $Description
    )

    $override = [System.Environment]::GetEnvironmentVariable($OverrideVariable)
    if (-not [string]::IsNullOrWhiteSpace($override)) {
        if (Test-Path -LiteralPath $override -PathType Leaf) { return $override }
        throw "$OverrideVariable is set to '$override', but no $Description exists there."
    }

    foreach ($candidate in $Candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return $candidate
        }
    }

    throw ("Could not locate the $Description. Set $OverrideVariable to its full path, or install it " +
        "at one of: " + ($Candidates -join '; '))
}

if (Test-IsWindowsHost) {
    $programFiles = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFiles)
    $unityCandidates = @(
        [System.IO.Path]::Combine($programFiles, 'Unity', 'Hub', 'Editor', $unityEditorVersion, 'Editor', 'Unity.exe')
    )
}
else {
    throw 'The Windows player-build smoke gate must run on a Windows host; Win64 is the active reference target.'
}

$unityPath = Resolve-ToolPath -OverrideVariable 'GUARDS_UNITY_PATH' -Candidates $unityCandidates -Description "Unity $unityEditorVersion Editor"

$activeEditor = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue
if ($activeEditor) {
    throw 'A Unity Editor process is already active. Monitor that process instead of starting another build.'
}

New-Item -ItemType Directory -Path ([System.IO.Path]::Combine($projectRoot, 'tmp')) -Force | Out-Null
foreach ($stale in @($buildLog, $playerLog)) {
    if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale -Force }
}
if (Test-Path -LiteralPath $executablePath) { Remove-Item -LiteralPath $executablePath -Force }

Write-Output '== Step 1: build the Windows player =='
$buildArguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $projectRoot),
    '-executeMethod', 'GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildWindowsSmokePlayer',
    '-logFile', ('"{0}"' -f $buildLog)
)
$build = Start-Process -FilePath $unityPath -ArgumentList $buildArguments -Wait -PassThru -WindowStyle Hidden
Write-Output "Build exit code: $($build.ExitCode)"

if ($build.ExitCode -ne 0) {
    Write-Error "GATE FAILED: the player build did not succeed. See $buildLog"
    exit 1
}

if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    Write-Error "GATE FAILED: the build reported success but produced no executable at $executablePath"
    exit 1
}

Write-Output "Built: $executablePath"

Write-Output '== Step 2: run the built player and drive one scripted interaction =='
$playerArguments = @(
    '-batchmode',
    '-nographics',
    '-guardsSmokeTest',
    '-logFile', ('"{0}"' -f $playerLog)
)
$player = Start-Process -FilePath $executablePath -ArgumentList $playerArguments -PassThru -WindowStyle Hidden

# Watch the log while waiting rather than only after. A player that throws during Awake never
# reaches the smoke driver, so it never calls Application.Quit and never exits on its own - waiting
# out the full timeout would report "did not exit" and say nothing about why. The first fatal
# exception is the real diagnosis, so surface that and stop.
$fatalPattern = 'InvalidOperationException|NullReferenceException|GUARDS_SMOKE: FAIL'
$deadline = (Get-Date).AddSeconds($PlayerTimeoutSeconds)
$fatalLine = $null
while (-not $player.HasExited -and (Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    if (-not (Test-Path -LiteralPath $playerLog -PathType Leaf)) { continue }

    $snapshot = Get-Content -LiteralPath $playerLog -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrEmpty($snapshot)) { continue }

    $match = [regex]::Match($snapshot, "^.*(?:$fatalPattern).*$", 'Multiline')
    if ($match.Success) { $fatalLine = $match.Value.Trim(); break }
}

if ($fatalLine) {
    try { if (-not $player.HasExited) { $player.Kill() } } catch { }
    Write-Output "FAIL: the player reported a fatal error before completing:"
    Write-Output "      $fatalLine"
    Write-Error "GATE FAILED: the built player did not reach a configured match. See $playerLog"
    exit 1
}

if (-not $player.HasExited) {
    try { $player.Kill() } catch { }
    Write-Error "GATE FAILED: the player did not exit within $PlayerTimeoutSeconds seconds. See $playerLog"
    exit 1
}

$player.WaitForExit()
Write-Output "Player exit code: $($player.ExitCode)"

Write-Output '== Step 3: verify what the player actually reported =='
if (-not (Test-Path -LiteralPath $playerLog -PathType Leaf)) {
    Write-Error "GATE FAILED: the player produced no log at $playerLog"
    exit 1
}

$playerOutput = Get-Content -LiteralPath $playerLog -Raw

# Exit code alone is not evidence. A player that degrades silently still exits zero, which is the
# exact failure mode this gate exists to catch, so the pass marker is required independently.
$failed = $false
if ($player.ExitCode -ne 0) {
    Write-Output "FAIL: the player exited $($player.ExitCode)."
    $failed = $true
}

# Milestone 7.9 added ROUNDS_PLAYED and PAUSE_ANSWERED. COMMAND_ACCEPTED proves a typed command
# survived the build; those two prove the built player can actually be played - it crossed a round
# boundary and answered p.15's Upgrade a Card, which milestone 7.6 established an ordinary end of
# round stops on from the second round onward.
foreach ($marker in @('GUARDS_SMOKE: BEGIN', 'GUARDS_SMOKE: COMPOSED', 'GUARDS_SMOKE: COMMAND_ACCEPTED', 'GUARDS_SMOKE: PAUSE_ANSWERED', 'GUARDS_SMOKE: ROUNDS_PLAYED', 'GUARDS_SMOKE: PASS')) {
    if ($playerOutput -notmatch [regex]::Escape($marker)) {
        Write-Output "FAIL: the player log is missing the marker '$marker'."
        $failed = $true
    }
}

if ($playerOutput -match 'GUARDS_SMOKE: FAIL') {
    foreach ($line in ($playerOutput -split "`r?`n" | Where-Object { $_ -match 'GUARDS_SMOKE: FAIL' })) {
        Write-Output "FAIL: $($line.Trim())"
    }
    $failed = $true
}

# A player that resolved no shader logs this rather than crashing, so it must be fatal here.
foreach ($pattern in @('Shader.*not found', 'Failed to load', 'is missing', 'NullReferenceException')) {
    $hits = [regex]::Matches($playerOutput, $pattern)
    if ($hits.Count -gt 0) {
        Write-Output "FAIL: the player log matched the degradation pattern '$pattern' $($hits.Count) time(s)."
        $failed = $true
    }
}

if ($failed) {
    Write-Error "GATE FAILED. Build log: $buildLog. Player log: $playerLog"
    exit 1
}

Write-Output 'GATE PASSED: the built player reached a configured match and completed the scripted interaction.'
Write-Output "Build log: $buildLog"
Write-Output "Player log: $playerLog"
exit 0
