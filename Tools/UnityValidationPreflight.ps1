$ErrorActionPreference = 'Stop'

function Get-UnityValidationActiveEditors {
    param([string] $HubPath, [object[]] $Candidates = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue))
    # Recent Hub versions run resources/unity.exe serve. Only that exact executable
    # is excluded; missing/inaccessible paths conservatively retain the Editor guard.
    $helperPath = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $HubPath) 'resources/unity.exe'))
    $brokerPath = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Unity/bin/unity.exe'))
    foreach ($candidate in $Candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate.Path) -or
            (-not [string]::Equals([IO.Path]::GetFullPath($candidate.Path), $helperPath, [StringComparison]::OrdinalIgnoreCase) -and
             -not [string]::Equals([IO.Path]::GetFullPath($candidate.Path), $brokerPath, [StringComparison]::OrdinalIgnoreCase))) {
            $candidate
        }
    }
}

function Restart-UnityValidationSupportProcesses {
    param(
        [Parameter(Mandatory = $true)] [string] $HubPath,
        [int] $ReadyTimeoutSeconds = 45,
        [scriptblock] $ProcessLookup = {
            param($Name)
            Get-Process -Name $Name -ErrorAction SilentlyContinue
        },
        [scriptblock] $StopSupportProcesses = {
            Get-Process -Name 'Unity Hub','Unity.Licensing.Client' -ErrorAction SilentlyContinue |
                Stop-Process -Force
        },
        [scriptblock] $StartHub = {
            param($Path)
            Start-Process -FilePath $Path -WindowStyle Hidden | Out-Null
        },
        [scriptblock] $WaitOneSecond = {
            Start-Sleep -Seconds 1
        },
        [scriptblock] $ReadClock = {
            Get-Date
        }
    )

    & $StopSupportProcesses
    & $StartHub $HubPath

    $deadline = (& $ReadClock).AddSeconds($ReadyTimeoutSeconds)
    do {
        & $WaitOneSecond
        $hub = & $ProcessLookup 'Unity Hub'
        $licensing = & $ProcessLookup 'Unity.Licensing.Client'
    } until (($hub -and $licensing) -or (& $ReadClock) -ge $deadline)

    if (-not $hub -or -not $licensing) {
        throw 'Unity Hub/licensing preflight did not become ready.'
    }
}
