[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$requestPath = Join-Path $projectRoot 'tmp\unity-validation-request.json'
$unityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe'
$hubPath = 'C:\Program Files\Unity Hub\Unity Hub.exe'

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
$filter = [string] $request.filter
$batchMode = -not ($request.PSObject.Properties.Name -contains 'batchMode') -or [bool] $request.batchMode

if (-not (Test-Path -LiteralPath $unityPath -PathType Leaf)) {
    throw "Unity Editor not found at $unityPath"
}

New-Item -ItemType Directory -Path (Split-Path -Parent $resultsPath) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $logPath) -Force | Out-Null

$activeEditor = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue
if ($activeEditor) {
    throw 'A Unity Editor process is already active. Monitor that process instead of starting another validation run.'
}

$hub = Get-Process -Name 'Unity Hub' -ErrorAction SilentlyContinue
$licensing = Get-Process -Name 'Unity.Licensing.Client' -ErrorAction SilentlyContinue
if (-not $hub -or -not $licensing) {
    if (-not (Test-Path -LiteralPath $hubPath -PathType Leaf)) {
        throw "Unity Hub not found at $hubPath"
    }

    Get-Process -Name 'Unity Hub','Unity.Licensing.Client' -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Process -FilePath $hubPath -WindowStyle Hidden | Out-Null

    $deadline = (Get-Date).AddSeconds(45)
    do {
        Start-Sleep -Seconds 1
        $hub = Get-Process -Name 'Unity Hub' -ErrorAction SilentlyContinue
        $licensing = Get-Process -Name 'Unity.Licensing.Client' -ErrorAction SilentlyContinue
    } until (($hub -and $licensing) -or (Get-Date) -ge $deadline)

    if (-not $hub -or -not $licensing) {
        throw 'Unity Hub/licensing preflight did not become ready.'
    }
}

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

$process = Start-Process -FilePath $unityPath -ArgumentList $unityArguments -Wait -PassThru -WindowStyle Hidden
Write-Output "Unity validation exit code: $($process.ExitCode)"
Write-Output "Results: $resultsPath"
Write-Output "Log: $logPath"
exit $process.ExitCode
