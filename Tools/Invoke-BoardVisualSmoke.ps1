<#
.SYNOPSIS
    Board visual smoke gate: builds a Windows player, photographs its own board, and judges the pixels.

.DESCRIPTION
    Milestone 8.31b. Every other visual check in this project is taken in the Editor, where every
    shader variant and asset is present. That blind spot let the board's blended overlays render as
    opaque white in every built player for as long as the artwork has existed, while the same scene
    was correct in the Editor and every suite stayed green.

    This gate builds the real artifact, launches it windowed - not -nographics, because a player
    that draws nothing cannot be photographed - and has it capture its own frame. It then reads that
    frame and fails when the playfield is one flat colour, which is exactly what an opaque overlay
    over the artwork looks like.

    It judges a central block of the board camera's own frame rather than the whole picture. A flat
    white or flat black block there is a defect however it was produced; a drawn board reads as many
    colours.
#>
[CmdletBinding()]
param(
    [int] $PlayerTimeoutSeconds = 180,
    [double] $MaximumFlatFraction = 0.5,
    [switch] $ReuseBuild,
    [switch] $MegdanBoard
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unityEditorVersion = '6000.5.9f1'
$buildLog = [System.IO.Path]::Combine($projectRoot, 'tmp', 'board-visual-build.log')
$playerLog = [System.IO.Path]::Combine($projectRoot, 'tmp', 'board-visual-player.log')
$capturePath = [System.IO.Path]::Combine($projectRoot, 'tmp', 'board-visual-capture.png')
$executablePath = [System.IO.Path]::Combine($projectRoot, 'Builds', 'Win64Smoke', 'GuardsDigitalSmoke.exe')

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

    throw "Could not locate the $Description. Set $OverrideVariable to its full path."
}

$programFiles = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFiles)
$unityPath = Resolve-ToolPath -OverrideVariable 'GUARDS_UNITY_PATH' -Candidates @(
    [System.IO.Path]::Combine($programFiles, 'Unity', 'Hub', 'Editor', $unityEditorVersion, 'Editor', 'Unity.exe')
) -Description "Unity $unityEditorVersion Editor"

New-Item -ItemType Directory -Path ([System.IO.Path]::Combine($projectRoot, 'tmp')) -Force | Out-Null
foreach ($stale in @($playerLog, $capturePath)) {
    if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale -Force }
}

if ($ReuseBuild -and (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    Write-Output "== Step 1: reusing the existing player at $executablePath =="
}
else {
    Write-Output '== Step 1: build the Windows player =='
    $activeEditor = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue
    if ($activeEditor) {
        throw 'A Unity Editor process is already active. Monitor that process instead of starting another build.'
    }
    if (Test-Path -LiteralPath $buildLog) { Remove-Item -LiteralPath $buildLog -Force }
    $build = Start-Process -FilePath $unityPath -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
        '-batchmode', '-quit',
        '-projectPath', ('"{0}"' -f $projectRoot),
        '-executeMethod', 'GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildWindowsSmokePlayer',
        '-logFile', ('"{0}"' -f $buildLog))
    if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
        Write-Error "GATE FAILED: the player build did not succeed (exit $($build.ExitCode)). See $buildLog"
        exit 1
    }
    Write-Output "Built: $executablePath"
}

Write-Output '== Step 2: launch the player windowed and have it photograph its own board =='
# No -nographics: this gate exists to look at rendered pixels. The window is hidden, which the
# player tolerates because the capture driver opts the process into background execution.
$player = Start-Process -FilePath $executablePath -PassThru -WindowStyle Hidden -ArgumentList @(
    '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
    '-guardsBoardCapture', ('"{0}"' -f $capturePath),
    '-logFile', ('"{0}"' -f $playerLog))

if (-not $player.WaitForExit($PlayerTimeoutSeconds * 1000)) {
    try { $player.Kill() } catch { }
    Write-Error "GATE FAILED: the player did not exit within $PlayerTimeoutSeconds seconds. See $playerLog"
    exit 1
}

Write-Output "Player exit code: $($player.ExitCode)"
$failed = $false
if ($player.ExitCode -ne 0) {
    Write-Output "FAIL: the player exited $($player.ExitCode)."
    $failed = $true
}

if (-not (Test-Path -LiteralPath $capturePath -PathType Leaf)) {
    Write-Error "GATE FAILED: the player wrote no capture at $capturePath. See $playerLog"
    exit 1
}

$playerOutput = Get-Content -LiteralPath $playerLog -Raw
if ($playerOutput -match 'GUARDS_VISUAL: FAIL') {
    foreach ($line in ($playerOutput -split "`r?`n" | Where-Object { $_ -match 'GUARDS_VISUAL: FAIL' })) {
        Write-Output "FAIL: $($line.Trim())"
    }
    $failed = $true
}

$artworkMatch = [regex]::Match($playerOutput, 'GUARDS_VISUAL: BOARD artwork=(true|false)')
if (-not $artworkMatch.Success) {
    Write-Output 'FAIL: the player did not report whether the board artwork was underneath.'
    $failed = $true
}
elseif ($artworkMatch.Groups[1].Value -ne $(if ($MegdanBoard) { 'false' } else { 'true' })) {
    # Without artwork the board is generated debug colour and this gate would judge the wrong thing.
    Write-Output 'FAIL: the built player board artwork does not match the selected board presentation.'
    $failed = $true
}

Write-Output '== Step 3: judge the captured pixels =='
Add-Type -AssemblyName System.Drawing
$bitmap = [System.Drawing.Bitmap]::FromFile($capturePath)
try {
    $width = $bitmap.Width
    $height = $bitmap.Height
    Write-Output "Capture: $width x $height"

    # A central block of the board camera's own frame. The capture is rendered through that camera
    # rather than grabbed off the screen, so it holds the board and none of the overlay HUD, and the
    # centre of it is board at every supported desktop shape.
    $left = [int]($width * 0.40)
    $right = [int]($width * 0.65)
    $top = [int]($height * 0.30)
    $bottom = [int]($height * 0.70)
    $stepX = [Math]::Max(1, [int](($right - $left) / 64))
    $stepY = [Math]::Max(1, [int](($bottom - $top) / 64))

    $sampled = 0
    $nearWhite = 0
    $nearBlack = 0
    $distinct = @{}
    for ($x = $left; $x -lt $right; $x += $stepX) {
        for ($y = $top; $y -lt $bottom; $y += $stepY) {
            $pixel = $bitmap.GetPixel($x, $y)
            $sampled++
            if ($pixel.R -gt 240 -and $pixel.G -gt 240 -and $pixel.B -gt 240) { $nearWhite++ }
            if ($pixel.R -lt 12 -and $pixel.G -lt 12 -and $pixel.B -lt 12) { $nearBlack++ }
            $key = '{0}-{1}-{2}' -f [int]($pixel.R / 16), [int]($pixel.G / 16), [int]($pixel.B / 16)
            $distinct[$key] = $true
        }
    }

    $whiteFraction = if ($sampled -gt 0) { $nearWhite / $sampled } else { 1 }
    $blackFraction = if ($sampled -gt 0) { $nearBlack / $sampled } else { 1 }
    Write-Output ("Playfield samples: {0}; near-white {1:P1}; near-black {2:P1}; distinct colours {3}" -f `
            $sampled, $whiteFraction, $blackFraction, $distinct.Count)

    if ($whiteFraction -gt $MaximumFlatFraction) {
        Write-Output ("FAIL: {0:P1} of the playfield is flat near-white, above the {1:P0} bound. An opaque overlay is covering the board artwork." -f `
                $whiteFraction, $MaximumFlatFraction)
        $failed = $true
    }
    if ($blackFraction -gt $MaximumFlatFraction) {
        Write-Output ("FAIL: {0:P1} of the playfield is flat near-black, above the {1:P0} bound. The player rendered no board." -f `
                $blackFraction, $MaximumFlatFraction)
        $failed = $true
    }
    if ($distinct.Count -lt 12) {
        Write-Output "FAIL: the playfield holds only $($distinct.Count) distinct colours; a drawn board holds many."
        $failed = $true
    }
}
finally {
    $bitmap.Dispose()
}

Write-Output "Capture: $capturePath"
Write-Output "Player log: $playerLog"

if ($failed) {
    Write-Error 'GATE FAILED: the built player did not draw the board correctly.'
    exit 1
}

Write-Output 'GATE PASSED: the built player drew the selected board presentation without flat white/black degradation.'
exit 0
