<#
.SYNOPSIS
    PvE player-build smoke gate: builds the separate PvE executable, runs it and proves it composes
    the encounter into the existing match scene.

.DESCRIPTION
    W-021. The PvE twin of Tools/Invoke-PlayerBuildSmoke.ps1, with the same fixed parameterless
    shape and Unity discovery. It builds through GuardsDigital.Pve.EditorTools.PveBuildEntryPoint,
    launches Builds/PveWin64/GuardsDigitalPvE.exe with -guardsPveSmokeTest, and requires the
    player's GUARDS_PVE_SMOKE markers and exit code: the encounter composed with its session and
    overlay, an enemy acted, and play reached round 2's spawns or the encounter's loss (a passive
    player may lose; the gate proves the mode runs, not its balance).

    It also checks the isolation contract (PVE-D01): the PvE player carries the PvE assemblies and
    its content data, and an existing-mode player under Builds/Win64Smoke - when one is present -
    carries no PvE assembly.
#>
[CmdletBinding()]
param(
    [int] $PlayerTimeoutSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unityEditorVersion = '6000.5.9f1'
$buildLog = [System.IO.Path]::Combine($projectRoot, 'tmp', 'pve-build-smoke-build.log')
$playerLog = [System.IO.Path]::Combine($projectRoot, 'tmp', 'pve-build-smoke-player.log')
$outputDirectory = [System.IO.Path]::Combine($projectRoot, 'Builds', 'PveWin64')
$executablePath = [System.IO.Path]::Combine($outputDirectory, 'GuardsDigitalPvE.exe')
$existingManaged = [System.IO.Path]::Combine($projectRoot, 'Builds', 'Win64Smoke', 'GuardsDigitalSmoke_Data', 'Managed')
$requiredMarkers = @('GUARDS_PVE_SMOKE: BEGIN', 'GUARDS_PVE_SMOKE: COMPOSED', 'GUARDS_PVE_SMOKE: ENEMY_ACTED', 'GUARDS_PVE_SMOKE: PASS')

$override = [System.Environment]::GetEnvironmentVariable('GUARDS_UNITY_PATH')
if (-not [string]::IsNullOrWhiteSpace($override)) {
    if (-not (Test-Path -LiteralPath $override -PathType Leaf)) { throw "GUARDS_UNITY_PATH is set to '$override', but no Unity Editor exists there." }
    $unityPath = $override
}
else {
    $programFiles = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFiles)
    $unityPath = [System.IO.Path]::Combine($programFiles, 'Unity', 'Hub', 'Editor', $unityEditorVersion, 'Editor', 'Unity.exe')
    if (-not (Test-Path -LiteralPath $unityPath -PathType Leaf)) { throw "Could not locate Unity $unityEditorVersion at $unityPath. Set GUARDS_UNITY_PATH." }
}

if (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue) {
    throw 'A Unity Editor process is already active. Monitor that process instead of starting another build.'
}

New-Item -ItemType Directory -Path ([System.IO.Path]::Combine($projectRoot, 'tmp')) -Force | Out-Null
foreach ($stale in @($buildLog, $playerLog)) {
    if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale -Force }
}
if (Test-Path -LiteralPath $executablePath) { Remove-Item -LiteralPath $executablePath -Force }

Write-Output '== Step 1: build the PvE player =='
$buildArguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $projectRoot),
    '-executeMethod', 'GuardsDigital.Pve.EditorTools.PveBuildEntryPoint.BuildWindowsPvePlayer',
    '-logFile', ('"{0}"' -f $buildLog)
)
$build = Start-Process -FilePath $unityPath -ArgumentList $buildArguments -Wait -PassThru -WindowStyle Hidden
Write-Output "Build exit code: $($build.ExitCode)"
if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    Write-Error "GATE FAILED: the PvE build did not succeed. See $buildLog"
    exit 1
}
Write-Output "Built: $executablePath"

Write-Output '== Step 2: run the PvE player =='
$playerArguments = @('-batchmode', '-nographics', '-guardsPveSmokeTest', '-logFile', ('"{0}"' -f $playerLog))
$player = Start-Process -FilePath $executablePath -ArgumentList $playerArguments -PassThru -WindowStyle Hidden
$fatalPattern = 'InvalidOperationException|NullReferenceException|GUARDS_PVE_SMOKE: FAIL'
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
    Write-Output "FAIL: $fatalLine"
    Write-Error "GATE FAILED: the PvE player reported a fatal error. See $playerLog"
    exit 1
}
if (-not $player.HasExited) {
    try { $player.Kill() } catch { }
    Write-Error "GATE FAILED: the PvE player did not exit within $PlayerTimeoutSeconds seconds. See $playerLog"
    exit 1
}
$player.WaitForExit()
Write-Output "Player exit code: $($player.ExitCode)"

Write-Output '== Step 3: verify markers, content data and assembly isolation =='
$failed = $false
$playerOutput = if (Test-Path -LiteralPath $playerLog -PathType Leaf) { Get-Content -LiteralPath $playerLog -Raw } else { '' }
if ($player.ExitCode -ne 0) { Write-Output "FAIL: the PvE player exited $($player.ExitCode)."; $failed = $true }
foreach ($marker in $requiredMarkers) {
    if ($playerOutput -notmatch [regex]::Escape($marker)) { Write-Output "FAIL: missing marker '$marker'."; $failed = $true }
}
if ($playerOutput -notmatch 'GUARDS_PVE_SMOKE: (ROUND_2_SPAWNED|LOST)') {
    Write-Output "FAIL: the encounter reached neither round 2's spawns nor its loss."; $failed = $true
}
foreach ($line in ($playerOutput -split "`r?`n" | Where-Object { $_ -match 'GUARDS_PVE_SMOKE: (COMPOSED|ROUND_2_SPAWNED|LOST|FAIL)' })) {
    Write-Output "  $($line.Trim())"
}
foreach ($pattern in @('Shader.*not found', 'Failed to load', 'NullReferenceException')) {
    $hits = [regex]::Matches($playerOutput, $pattern)
    if ($hits.Count -gt 0) { Write-Output "FAIL: the player log matched '$pattern' $($hits.Count) time(s)."; $failed = $true }
}

foreach ($data in @('enemies.json', 'encounter.json')) {
    if (-not (Test-Path -LiteralPath ([System.IO.Path]::Combine($outputDirectory, 'PveData', $data)) -PathType Leaf)) {
        Write-Output "FAIL: PveData\$data is missing beside the executable."; $failed = $true
    }
}

$pveManaged = [System.IO.Path]::Combine($outputDirectory, 'GuardsDigitalPvE_Data', 'Managed')
foreach ($assembly in @('GuardsDigital.Pve.dll', 'GuardsDigital.Pve.Presentation.dll')) {
    if (-not (Test-Path -LiteralPath ([System.IO.Path]::Combine($pveManaged, $assembly)) -PathType Leaf)) {
        Write-Output "FAIL: the PvE player lacks $assembly."; $failed = $true
    }
}

if (Test-Path -LiteralPath $existingManaged -PathType Container) {
    $leaked = @(Get-ChildItem -LiteralPath $existingManaged -Filter 'GuardsDigital.Pve*' -File)
    if ($leaked.Count -gt 0) {
        Write-Output "FAIL: the existing-mode player carries PvE assemblies: $($leaked.Name -join ', ')"; $failed = $true
    }
    else {
        Write-Output 'Existing-mode player under Builds/Win64Smoke carries no PvE assembly.'
    }
}
else {
    Write-Output 'NOTE: no existing-mode player under Builds/Win64Smoke; run Tools/Invoke-PlayerBuildSmoke.ps1 to check isolation.'
}

if ($failed) {
    Write-Error "GATE FAILED. Build log: $buildLog. Player log: $playerLog"
    exit 1
}

Write-Output 'GATE PASSED: the PvE player composed the encounter into the existing match scene.'
Write-Output "Build log: $buildLog"
Write-Output "Player log: $playerLog"
exit 0
