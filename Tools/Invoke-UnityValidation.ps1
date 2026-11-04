[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'UnityValidationEvidence.ps1')
. (Join-Path $PSScriptRoot 'UnityValidationPreflight.ps1')
. (Join-Path $PSScriptRoot 'UnityValidationRequest.ps1')

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$requestPath = [System.IO.Path]::Combine($projectRoot, 'tmp', 'unity-validation-request.json')

$unityEditorVersion = '6000.5.9f1'

function Test-IsWindowsHost {
    # $IsWindows exists on PowerShell Core only; Windows PowerShell 5.1 predates it and is
    # Windows by definition.
    if (Test-Path -Path 'Variable:\IsWindows') { return $IsWindows }
    return $true
}

<#
.SYNOPSIS
    Locates an executable through an explicit environment override, then platform-appropriate probing.

.DESCRIPTION
    The override always wins, so a machine with a non-standard install location needs no edit to this
    script. Probing only covers the documented default install locations for each declared
    development platform. Discovery failure names both the override variable and every probed path,
    because "Unity not found" without the search list is unactionable.
#>
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
    $hubCandidates = @(
        [System.IO.Path]::Combine($programFiles, 'Unity Hub', 'Unity Hub.exe')
    )
}
else {
    # $home is a read-only automatic variable under PowerShell 7; use a distinct name.
    $userProfile = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)
    $unityCandidates = @(
        # macOS Hub layout, then the common Linux Hub layout.
        [System.IO.Path]::Combine('/Applications', 'Unity', 'Hub', 'Editor', $unityEditorVersion, 'Unity.app', 'Contents', 'MacOS', 'Unity'),
        [System.IO.Path]::Combine($userProfile, 'Unity', 'Hub', 'Editor', $unityEditorVersion, 'Editor', 'Unity'),
        [System.IO.Path]::Combine('/opt', 'unity', 'editors', $unityEditorVersion, 'Editor', 'Unity')
    )
    $hubCandidates = @(
        [System.IO.Path]::Combine('/Applications', 'Unity Hub.app', 'Contents', 'MacOS', 'Unity Hub'),
        [System.IO.Path]::Combine('/usr', 'bin', 'unityhub'),
        [System.IO.Path]::Combine('/opt', 'unityhub', 'unityhub')
    )
}

$unityPath = Resolve-ToolPath -OverrideVariable 'GUARDS_UNITY_PATH' -Candidates $unityCandidates -Description "Unity $unityEditorVersion Editor"

function Resolve-ProjectOutputPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath,

        [Parameter(Mandatory = $true)]
        [string] $FieldName
    )

    if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "$FieldName must be relative to the project root."
    }

    $resolved = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $RelativePath))
    $allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'tmp')) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$FieldName must resolve beneath the project's tmp directory."
    }

    return $resolved
}

if (-not (Test-Path -LiteralPath $requestPath -PathType Leaf)) {
    throw "Missing validation request: $requestPath"
}

$request = Get-Content -LiteralPath $requestPath -Raw | ConvertFrom-Json
if ($request.platform -notin @('EditMode', 'PlayMode')) {
    throw "platform must be EditMode or PlayMode."
}

$resultsPath = Resolve-ProjectOutputPath -RelativePath ([string] $request.results) -FieldName 'results'
$logPath = Resolve-ProjectOutputPath -RelativePath ([string] $request.log) -FieldName 'log'
if ([string]::Equals($resultsPath, $logPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'results and log must resolve to different files.'
}
$filter = Resolve-UnityValidationFilterExpression -Request $request
$batchMode = -not ($request.PSObject.Properties.Name -contains 'batchMode') -or [bool] $request.batchMode

if (-not (Test-Path -LiteralPath $unityPath -PathType Leaf)) {
    throw "Unity Editor not found at $unityPath"
}

New-Item -ItemType Directory -Path (Split-Path -Parent $resultsPath) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $logPath) -Force | Out-Null

$hubPath = Resolve-ToolPath -OverrideVariable 'GUARDS_UNITY_HUB_PATH' -Candidates $hubCandidates -Description 'Unity Hub'
$activeEditor = Get-UnityValidationActiveEditors -HubPath $hubPath
if ($activeEditor) {
    throw 'A Unity Editor process is already active. Monitor that process instead of starting another validation run.'
}

Restart-UnityValidationSupportProcesses -HubPath $hubPath

$unityArguments = @()
if ($batchMode) {
    $unityArguments += '-batchmode'
}

$unityArguments += @(
    '-runTests',
    '-projectPath', ('"{0}"' -f $projectRoot),
    '-testPlatform', [string] $request.platform,
    '-testResults', ('"{0}"' -f $resultsPath),
    '-logFile', ('"{0}"' -f $logPath)
)

if (-not [string]::IsNullOrWhiteSpace($filter)) {
    $unityArguments += @('-testFilter', $filter)
}

$process = Invoke-UnityValidationProcessAndRequireEvidence `
    -UnityPath $unityPath `
    -UnityArguments $unityArguments `
    -ResultsPath $resultsPath `
    -LogPath $logPath
Write-Output "Unity validation exit code: $($process.ExitCode)"
Write-Output "Results: $resultsPath"
Write-Output "Log: $logPath"
exit 0
