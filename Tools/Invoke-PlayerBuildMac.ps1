<#
.SYNOPSIS
    Builds the macOS player (a universal Mono .app) from this Windows host.

.DESCRIPTION
    Milestone 8.77. The Mac counterpart of Tools/Invoke-PlayerBuildSmoke.ps1's first step. It builds
    Builds/MacBuild/GuardsDigital.app through the fixed entry point
    GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildMacPlayer and proves the build report and the
    bundle's layout (the Contents/MacOS binary and the Data folder). It cannot LAUNCH the player - a
    macOS bundle runs only on a Mac - so it is a build gate, not a build-and-launch gate; the
    Windows smoke on the same commit is what proves the content composes a match.

    Requires Unity 6000.5.9f1 with the Mac Build Support (Mono) module installed
    (Editor/Data/PlaybackEngines/MacStandaloneSupport). Same Unity discovery as the smoke gate.

    NOTE for whoever ships the .app: zipping on Windows drops the Unix executable bit, so the
    read-me packaged beside the bundle tells the Mac player to run
    `chmod -R +x "GuardsDigital.app/Contents/MacOS"` and `xattr -cr GuardsDigital.app` once; the
    binary inside is named after the product ("Guards 0.1").
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unityEditorVersion = '6000.5.9f1'
$buildLog = [System.IO.Path]::Combine($projectRoot, 'tmp', 'player-build-mac-build.log')
$bundlePath = [System.IO.Path]::Combine($projectRoot, 'Builds', 'MacBuild', 'GuardsDigital.app')

$programFiles = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFiles)
$unityPath = [System.Environment]::GetEnvironmentVariable('GUARDS_UNITY_PATH')
if ([string]::IsNullOrWhiteSpace($unityPath)) {
    $unityPath = [System.IO.Path]::Combine($programFiles, 'Unity', 'Hub', 'Editor', $unityEditorVersion, 'Editor', 'Unity.exe')
}
if (-not (Test-Path -LiteralPath $unityPath -PathType Leaf)) {
    throw "Could not locate the Unity $unityEditorVersion Editor at '$unityPath'. Set GUARDS_UNITY_PATH."
}
$macSupport = [System.IO.Path]::Combine((Split-Path -Parent $unityPath), 'Data', 'PlaybackEngines', 'MacStandaloneSupport')
if (-not (Test-Path -LiteralPath $macSupport -PathType Container)) {
    throw "The Mac Build Support module is not installed for Unity $unityEditorVersion (expected $macSupport)."
}

$activeEditor = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue
if ($activeEditor) {
    throw 'A Unity Editor process is already active. Monitor that process instead of starting another build.'
}

New-Item -ItemType Directory -Path ([System.IO.Path]::Combine($projectRoot, 'tmp')) -Force | Out-Null
if (Test-Path -LiteralPath $buildLog) { Remove-Item -LiteralPath $buildLog -Force }
if (Test-Path -LiteralPath $bundlePath) { Remove-Item -LiteralPath $bundlePath -Recurse -Force }

Write-Output '== Build the macOS player (universal, Mono) =='
$buildArguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $projectRoot),
    '-buildTarget', 'OSXUniversal',
    '-executeMethod', 'GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildMacPlayer',
    '-logFile', ('"{0}"' -f $buildLog)
)
$build = Start-Process -FilePath $unityPath -ArgumentList $buildArguments -Wait -PassThru -WindowStyle Hidden
Write-Output "Build exit code: $($build.ExitCode)"

if ($build.ExitCode -ne 0) {
    Write-Error "MAC BUILD FAILED: the player build did not succeed. See $buildLog"
    exit 1
}

$binaryDir = [System.IO.Path]::Combine($bundlePath, 'Contents', 'MacOS')
$dataDir = [System.IO.Path]::Combine($bundlePath, 'Contents', 'Resources', 'Data')
if (-not (Test-Path -LiteralPath $bundlePath -PathType Container)) {
    Write-Error "MAC BUILD FAILED: the build reported success but produced no bundle at $bundlePath"
    exit 1
}
if (-not (Test-Path -LiteralPath $binaryDir -PathType Container) -or -not (Get-ChildItem -LiteralPath $binaryDir -File)) {
    Write-Error "MAC BUILD FAILED: the bundle has no Contents/MacOS binary"
    exit 1
}
if (-not (Test-Path -LiteralPath $dataDir -PathType Container)) {
    Write-Error "MAC BUILD FAILED: the bundle has no Contents/Resources/Data folder"
    exit 1
}

$stamp = Select-String -LiteralPath $buildLog -Pattern 'GUARDS_BUILD: STAMP (.+)$' | Select-Object -First 1
if ($stamp) { Write-Output ("Stamp: " + $stamp.Matches[0].Groups[1].Value.Trim()) }
Write-Output "Built: $bundlePath"
Write-Output 'MAC BUILD PASSED (build report and bundle layout; launch is proven on a Mac).'
exit 0
