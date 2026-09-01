<#
.SYNOPSIS
    Private-online rehearsal: one dedicated seatless host and six client processes on loopback.

.DESCRIPTION
    Milestone 8.31's local multi-process runner. Builds the disposable Win64 player once, then
    starts one host process and, in a deterministic order, nine client processes against it on
    the loopback interface: one first seat, one duplicate claim of that seat (refused by name
    while five seats stay open), the remaining five seats, one compatible straggler into the
    full session, and one client offering the wrong session key. It then requires every process
    to exit on its own and reads every log, because exit codes alone are never evidence.

    -Topology LoopbackSmoke is the only topology this milestone owns; -Topology SixClient and
    any -FaultPlan other than None belong to milestone 8.35c and are refused here. The runner
    never binds a non-loopback interface.

    Privacy is verified, not assumed: no log may carry a remote ip:port other than the
    operator-supplied loopback endpoint, which is the amended architecture record's rule that
    connections are named by opaque ordinals everywhere.
#>
[CmdletBinding()]
param(
    [ValidateSet('LoopbackSmoke', 'SixClient')] [string] $Topology = 'LoopbackSmoke',
    [ValidateSet('None', 'Full')] [string] $FaultPlan = 'None',
    [int] $TimeoutSeconds = 420,
    [switch] $ReuseBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Topology -ne 'LoopbackSmoke') {
    Write-Error "REHEARSAL REFUSED: -Topology $Topology is owned by milestone 8.35c; 8.31 owns LoopbackSmoke only."
    exit 1
}
if ($FaultPlan -ne 'None') {
    Write-Error "REHEARSAL REFUSED: -FaultPlan $FaultPlan is owned by milestone 8.35c; 8.31 runs without fault injection."
    exit 1
}

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unityEditorVersion = '6000.5.9f1'
$tmpRoot = [System.IO.Path]::Combine($projectRoot, 'tmp')
$buildLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-build.log')
$hostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-host.log')
$executablePath = [System.IO.Path]::Combine($projectRoot, 'Builds', 'Win64Smoke', 'GuardsDigitalSmoke.exe')
$seats = @('red-player-01', 'red-player-02', 'red-player-03', 'blue-player-01', 'blue-player-02', 'blue-player-03')
$processes = @()
$clientLogs = @{}

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

function Wait-ForLogMarker {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [string] $Pattern,
        [Parameter(Mandatory = $true)] [int] $DeadlineSeconds,
        [Parameter(Mandatory = $true)] [string] $Description
    )

    $deadline = (Get-Date).AddSeconds($DeadlineSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
            $snapshot = Get-Content -LiteralPath $LogPath -Raw -ErrorAction SilentlyContinue
            if (-not [string]::IsNullOrEmpty($snapshot)) {
                if ($snapshot -match 'GUARDS_ONLINE: FAIL') {
                    $line = ($snapshot -split "`r?`n" | Where-Object { $_ -match 'GUARDS_ONLINE: FAIL' })[0]
                    throw "A process reported a failure while waiting for $Description : $($line.Trim()) (see $LogPath)"
                }
                if ($snapshot -match $Pattern) { return }
            }
        }
        Start-Sleep -Milliseconds 250
    }

    throw "Timed out after $DeadlineSeconds seconds waiting for $Description in $LogPath"
}

function Start-GuardsClient {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Seat,
        [Parameter(Mandatory = $true)] [string] $Address,
        [Parameter(Mandatory = $true)] [string] $Key,
        [Parameter(Mandatory = $true)] [int] $BudgetSeconds
    )

    $log = [System.IO.Path]::Combine($script:tmpRoot, "private-online-client-$Name.log")
    if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force }
    $arguments = @(
        '-batchmode', '-nographics',
        '-guardsPrivateClient',
        '-guardsAddress', $Address,
        '-guardsSeat', $Seat,
        '-guardsSessionKey', $Key,
        '-guardsOnlineTimeoutSeconds', $BudgetSeconds,
        '-logFile', ('"{0}"' -f $log)
    )
    $process = Start-Process -FilePath $script:executablePath -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $script:processes += $process
    $script:clientLogs[$Name] = $log
    return $process
}

if (-not (Test-IsWindowsHost)) {
    throw 'The private-online rehearsal must run on a Windows host; Win64 is the active reference target.'
}

$programFiles = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ProgramFiles)
$unityCandidates = @(
    [System.IO.Path]::Combine($programFiles, 'Unity', 'Hub', 'Editor', $unityEditorVersion, 'Editor', 'Unity.exe')
)
$unityPath = Resolve-ToolPath -OverrideVariable 'GUARDS_UNITY_PATH' -Candidates $unityCandidates -Description "Unity $unityEditorVersion Editor"

New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null
foreach ($stale in @($hostLog)) {
    if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale -Force }
}

$rehearsalFailed = $false
try {
    if ($ReuseBuild -and (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
        Write-Output "== Step 1: reusing the existing player at $executablePath =="
    }
    else {
        Write-Output '== Step 1: build the disposable Win64 player =='
        $activeEditor = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue
        if ($activeEditor) {
            throw 'A Unity Editor process is already active. Monitor that process instead of starting another build.'
        }
        if (Test-Path -LiteralPath $buildLog) { Remove-Item -LiteralPath $buildLog -Force }
        if (Test-Path -LiteralPath $executablePath) { Remove-Item -LiteralPath $executablePath -Force }
        $buildArguments = @(
            '-batchmode', '-quit',
            '-projectPath', ('"{0}"' -f $projectRoot),
            '-executeMethod', 'GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildWindowsSmokePlayer',
            '-logFile', ('"{0}"' -f $buildLog)
        )
        $build = Start-Process -FilePath $unityPath -ArgumentList $buildArguments -Wait -PassThru -WindowStyle Hidden
        if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
            throw "The player build did not succeed (exit $($build.ExitCode)). See $buildLog"
        }
        Write-Output "Built: $executablePath"
    }

    Write-Output '== Step 2: pick a free loopback port and start the host =='
    $portProbe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $portProbe.Start()
    $port = ([System.Net.IPEndPoint]$portProbe.LocalEndpoint).Port
    $portProbe.Stop()
    $address = "127.0.0.1:$port"
    $sessionKey = [System.Guid]::NewGuid().ToString('N')
    Write-Output "Loopback endpoint: $address"

    $hostArguments = @(
        '-batchmode', '-nographics',
        '-guardsPrivateHost',
        '-guardsBind', '127.0.0.1',
        '-guardsPort', $port,
        '-guardsSessionKey', $sessionKey,
        '-guardsOnlineTimeoutSeconds', [Math]::Max(60, $TimeoutSeconds - 60),
        '-logFile', ('"{0}"' -f $hostLog)
    )
    $hostProcess = Start-Process -FilePath $executablePath -ArgumentList $hostArguments -PassThru -WindowStyle Hidden
    $processes += $hostProcess
    Wait-ForLogMarker -LogPath $hostLog -Pattern 'GUARDS_ONLINE: HOST_LISTENING port=' -DeadlineSeconds 120 -Description 'the host to listen'

    Write-Output '== Step 3: seat the first client, then refuse a duplicate claim by name =='
    $null = Start-GuardsClient -Name $seats[0] -Seat $seats[0] -Address $address -Key $sessionKey -BudgetSeconds 300
    Wait-ForLogMarker -LogPath $hostLog -Pattern ('SEAT_CLAIMED ordinal=\d+ seat=' + [regex]::Escape($seats[0])) -DeadlineSeconds 120 -Description 'the first seat claim'

    $duplicate = Start-GuardsClient -Name 'duplicate' -Seat $seats[0] -Address $address -Key $sessionKey -BudgetSeconds 60
    Wait-ForLogMarker -LogPath $clientLogs['duplicate'] -Pattern 'CLIENT_REJECTED reason=SeatAlreadyClaimed' -DeadlineSeconds 120 -Description 'the duplicate claim refusal'

    Write-Output '== Step 4: seat the remaining five clients =='
    foreach ($seat in $seats[1..5]) {
        $null = Start-GuardsClient -Name $seat -Seat $seat -Address $address -Key $sessionKey -BudgetSeconds 300
    }
    Wait-ForLogMarker -LogPath $hostLog -Pattern 'SEATS_COMPLETE claimed=6/6' -DeadlineSeconds 180 -Description 'all six seats claimed'

    Write-Output '== Step 5: refuse a compatible straggler and a wrong session key by name =='
    $straggler = Start-GuardsClient -Name 'straggler' -Seat $seats[1] -Address $address -Key $sessionKey -BudgetSeconds 60
    Wait-ForLogMarker -LogPath $clientLogs['straggler'] -Pattern 'CLIENT_REJECTED reason=SessionFull' -DeadlineSeconds 120 -Description 'the full-session refusal'

    $uninvited = Start-GuardsClient -Name 'uninvited' -Seat $seats[2] -Address $address -Key 'not-the-key' -BudgetSeconds 60
    Wait-ForLogMarker -LogPath $clientLogs['uninvited'] -Pattern 'CLIENT_REJECTED reason=SessionKeyMismatch' -DeadlineSeconds 120 -Description 'the wrong-key refusal'

    Write-Output '== Step 6: require every process to exit on its own =='
    $exitDeadline = (Get-Date).AddSeconds(120)
    foreach ($process in $processes) {
        $remaining = [int][Math]::Max(1, ($exitDeadline - (Get-Date)).TotalSeconds)
        if (-not $process.WaitForExit($remaining * 1000)) {
            throw "Process id $($process.Id) did not exit on its own inside the bounded window."
        }
    }

    Write-Output '== Step 7: read every log; exit codes alone are never evidence =='
    $hostOutput = Get-Content -LiteralPath $hostLog -Raw
    if ($hostProcess.ExitCode -ne 0) {
        Write-Output "FAIL: the host exited $($hostProcess.ExitCode)."
        $rehearsalFailed = $true
    }
    foreach ($marker in @('GUARDS_ONLINE: HOST_LISTENING', 'GUARDS_ONLINE: SEATS_COMPLETE claimed=6/6',
            'REJECTED ordinal=\d+ reason=SeatAlreadyClaimed', 'REJECTED ordinal=\d+ reason=SessionFull',
            'REJECTED ordinal=\d+ reason=SessionKeyMismatch', 'GUARDS_ONLINE: HOST_STOPPED', 'GUARDS_ONLINE: EXIT 0')) {
        if ($hostOutput -notmatch $marker) {
            Write-Output "FAIL: the host log is missing '$marker'."
            $rehearsalFailed = $true
        }
    }

    $claimed = [regex]::Matches($hostOutput, 'SEAT_CLAIMED ordinal=\d+ seat=([a-z0-9-]+)') |
        ForEach-Object { $_.Groups[1].Value }
    $distinct = $claimed | Sort-Object -Unique
    if (@($claimed).Count -ne 6 -or @($distinct).Count -ne 6) {
        Write-Output "FAIL: expected exactly six distinct SEAT_CLAIMED lines; saw $(@($claimed).Count) claims over $(@($distinct).Count) seats."
        $rehearsalFailed = $true
    }

    foreach ($seat in $seats) {
        $log = $clientLogs[$seat]
        $output = Get-Content -LiteralPath $log -Raw
        if ($output -notmatch ('CLIENT_SEATED seat=' + [regex]::Escape($seat) + ' hero=')) {
            Write-Output "FAIL: client $seat never reported CLIENT_SEATED. See $log"
            $rehearsalFailed = $true
        }
        if ($output -notmatch 'GUARDS_ONLINE: EXIT 0') {
            Write-Output "FAIL: client $seat did not record an orderly exit. See $log"
            $rehearsalFailed = $true
        }
    }
    foreach ($rejected in @(@('duplicate', 'SeatAlreadyClaimed'), @('straggler', 'SessionFull'), @('uninvited', 'SessionKeyMismatch'))) {
        $log = $clientLogs[$rejected[0]]
        $output = Get-Content -LiteralPath $log -Raw
        if ($output -notmatch ('CLIENT_REJECTED reason=' + $rejected[1])) {
            Write-Output "FAIL: client $($rejected[0]) was not refused as $($rejected[1]). See $log"
            $rehearsalFailed = $true
        }
        if ($output -notmatch 'GUARDS_ONLINE: EXIT 3') {
            Write-Output "FAIL: client $($rejected[0]) did not exit with the rejected code. See $log"
            $rehearsalFailed = $true
        }
    }

    # Privacy: connections are opaque ordinals everywhere. The only ip:port any log may carry is
    # the operator-supplied loopback endpoint.
    $allLogs = @($hostLog) + @($clientLogs.Values)
    foreach ($log in $allLogs) {
        $output = Get-Content -LiteralPath $log -Raw
        $endpoints = [regex]::Matches($output, '\b(\d{1,3}\.){3}\d{1,3}:\d+') | ForEach-Object { $_.Value }
        $foreign = $endpoints | Where-Object { $_ -notlike '127.0.0.1:*' } | Sort-Object -Unique
        if (@($foreign).Count -gt 0) {
            Write-Output "FAIL: $log carries a non-operator endpoint: $($foreign -join ', ')"
            $rehearsalFailed = $true
        }
        foreach ($pattern in @('Shader.*not found', 'Failed to load', 'is missing', 'NullReferenceException')) {
            $hits = [regex]::Matches($output, $pattern)
            if ($hits.Count -gt 0) {
                Write-Output "FAIL: $log matched the degradation pattern '$pattern' $($hits.Count) time(s)."
                $rehearsalFailed = $true
            }
        }
    }
}
catch {
    Write-Output "FAIL: $($_.Exception.Message)"
    $rehearsalFailed = $true
}
finally {
    foreach ($process in $processes) {
        try { if (-not $process.HasExited) { $process.Kill() } } catch { }
    }
}

Write-Output "Host log: $hostLog"
foreach ($name in $clientLogs.Keys) {
    Write-Output "Client log ($name): $($clientLogs[$name])"
}

if ($rehearsalFailed) {
    Write-Error 'REHEARSAL FAILED: see the FAIL lines and logs above.'
    exit 1
}

Write-Output 'REHEARSAL PASSED: one seatless host, six distinct seats, and three named refusals on loopback.'
exit 0
