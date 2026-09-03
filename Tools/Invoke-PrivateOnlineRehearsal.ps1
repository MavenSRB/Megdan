<#
.SYNOPSIS
    Private-online rehearsal: one dedicated seatless host and six client processes on loopback.

.DESCRIPTION
    The local multi-process runner named by Docs/AI/Testing/PRIVATE_ONLINE.md. It owns two
    topologies against one disposable Win64 player.

    -Topology LoopbackSmoke -FaultPlan None (milestone 8.31)
        Builds the player once, then starts one host process and, in a deterministic order, nine
        client processes against it on the loopback interface: one first seat, one duplicate claim
        of that seat (refused by name while five seats stay open), the remaining five seats, one
        compatible straggler into the full session, and one client offering the wrong session key.
        It proves seating and refusal vocabulary only; it plays no match.

    -Topology SixClient -FaultPlan Full (milestone 8.35c)
        The playtest-build rehearsal. Two cohorts run against one freshly built player, each one
        dedicated seatless host process plus six client processes on the six canonical seats,
        every process entering through the real primitive lobby:

          match cohort      the six clients ready up, the host begins the match, and the cohort
                            plays a complete match through the owner-scoped HUD. It forces one
                            same-team tied-Initiative disagreement followed by a revised unanimous
                            preference, one changed Minion Battle removal proposal after partial
                            agreement followed by unanimous confirmation, and one mid-match
                            disconnect and lobby-driven reconnect on red-player-02. Every seat
                            audits its own projection for privacy and authority, and host and all
                            six clients must report the identical outcome cause and winner.

          host-loss cohort  the same topology reaches the match and stops. The runner then
                            terminates the host process - this is the ONE deliberate external kill
                            in the whole runner, and it is the fault the plan names as
                            "terminate the host"; every other process must always exit on its own.
                            Each client must observe terminal host loss, find its input disabled,
                            and exit zero.

    -Topology SixClient requires -FaultPlan Full, and -Topology LoopbackSmoke requires
    -FaultPlan None. The 8.35c matrix row names exactly one six-client combination - "full match,
    selected fault plan, reconnect/resync, terminal host loss probe" - so a six-client run without
    the fault plan is not the milestone's gate, and permitting it would let a weaker run be
    mistaken for one. The deterministic 80-case Full catalog itself is the typed
    PrivateOnlineRehearsalPlan.FullCases inventory, executed in-process by the Edit Mode
    fault/synchronization/reconnect suites the same matrix row lists; these cohorts prove the same
    plan's live six-process half.

    The runner never binds a non-loopback interface. The endpoint and the session key reach each
    player through that child process's own environment block and never through a command line, so
    Unity's command-line banner cannot disclose the invitation. Privacy is verified, not assumed:
    no log may carry a remote ip:port other than the operator-supplied loopback endpoint, may carry
    the generated session key, or may match a degradation pattern.
#>
[CmdletBinding()]
param(
    [ValidateSet('LoopbackSmoke', 'SixClient')] [string] $Topology = 'LoopbackSmoke',
    [ValidateSet('None', 'Full')] [string] $FaultPlan = 'None',
    [int] $TimeoutSeconds = 0,
    [int] $Seed = 21,
    [switch] $ReuseBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Topology -eq 'LoopbackSmoke' -and $FaultPlan -ne 'None') {
    Write-Error "REHEARSAL REFUSED: -Topology LoopbackSmoke runs without fault injection; use -FaultPlan None."
    exit 1
}
if ($Topology -eq 'SixClient' -and $FaultPlan -ne 'Full') {
    Write-Error ("REHEARSAL REFUSED: -Topology SixClient is milestone 8.35c's gate and its matrix row " +
        "names one combination; use -FaultPlan Full.")
    exit 1
}
if ($TimeoutSeconds -le 0) {
    if ($Topology -eq 'SixClient') { $TimeoutSeconds = 2400 } else { $TimeoutSeconds = 420 }
}

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unityEditorVersion = '6000.5.9f1'
$tmpRoot = [System.IO.Path]::Combine($projectRoot, 'tmp')
$buildLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-build.log')
$hostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-host.log')
$executablePath = [System.IO.Path]::Combine($projectRoot, 'Builds', 'Win64Smoke', 'GuardsDigitalSmoke.exe')
$seats = @('red-player-01', 'red-player-02', 'red-player-03', 'blue-player-01', 'blue-player-02', 'blue-player-03')
$reconnectSeat = 'red-player-02'
$processes = @()
$clientLogs = @{}
$rehearsalKeys = @()

# The rehearsal driver reads these three from its own environment block, never from argv.
$addressVariable = 'GUARDS_REHEARSAL_ADDRESS'
$portVariable = 'GUARDS_REHEARSAL_PORT'
$keyVariable = 'GUARDS_REHEARSAL_SESSION_KEY'

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

function Get-LogText {
    param([Parameter(Mandatory = $true)] [string] $LogPath)
    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) { return '' }
    $text = Get-Content -LiteralPath $LogPath -Raw -ErrorAction SilentlyContinue
    if ($null -eq $text) { return '' }
    return $text
}

function Get-FirstFailure {
    param([Parameter(Mandatory = $true)] [string[]] $LogPaths)
    foreach ($path in $LogPaths) {
        $text = Get-LogText -LogPath $path
        if ($text -match 'GUARDS_ONLINE: FAIL') {
            $line = ($text -split "`r?`n" | Where-Object { $_ -match 'GUARDS_ONLINE: FAIL' })[0]
            return "$($line.Trim()) (see $path)"
        }
    }
    return $null
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
        $snapshot = Get-LogText -LogPath $LogPath
        if ($snapshot.Length -gt 0) {
            if ($snapshot -match 'GUARDS_ONLINE: FAIL') {
                $line = ($snapshot -split "`r?`n" | Where-Object { $_ -match 'GUARDS_ONLINE: FAIL' })[0]
                throw "A process reported a failure while waiting for $Description : $($line.Trim()) (see $LogPath)"
            }
            if ($snapshot -match $Pattern) { return }
        }
        Start-Sleep -Milliseconds 250
    }

    throw "Timed out after $DeadlineSeconds seconds waiting for $Description in $LogPath"
}

<#
    Waits until every (log, pattern) requirement is satisfied, aborting the moment any process in
    the cohort reports a failure. Exit codes are never the evidence; the markers are.
#>
function Wait-ForCohortMarkers {
    param(
        [Parameter(Mandatory = $true)] [object[]] $Requirements,
        [Parameter(Mandatory = $true)] [string[]] $WatchLogs,
        [Parameter(Mandatory = $true)] [int] $DeadlineSeconds,
        [Parameter(Mandatory = $true)] [string] $Description
    )

    $deadline = (Get-Date).AddSeconds($DeadlineSeconds)
    while ((Get-Date) -lt $deadline) {
        $failure = Get-FirstFailure -LogPaths $WatchLogs
        if ($null -ne $failure) {
            throw "A process reported a failure while waiting for $Description : $failure"
        }

        $outstanding = @()
        foreach ($requirement in $Requirements) {
            $text = Get-LogText -LogPath $requirement.Log
            if ($text -notmatch $requirement.Pattern) {
                $outstanding += ("{0}: {1}" -f (Split-Path -Leaf $requirement.Log), $requirement.Pattern)
            }
        }

        if ($outstanding.Count -eq 0) { return }
        Start-Sleep -Milliseconds 500
    }

    $missing = @()
    foreach ($requirement in $Requirements) {
        $text = Get-LogText -LogPath $requirement.Log
        if ($text -notmatch $requirement.Pattern) {
            $missing += ("{0}: {1}" -f (Split-Path -Leaf $requirement.Log), $requirement.Pattern)
        }
    }

    throw ("Timed out after $DeadlineSeconds seconds waiting for $Description. Still missing: " +
        ($missing -join '; '))
}

function Get-FreeLoopbackPort {
    $probe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $probe.Start()
    $port = ([System.Net.IPEndPoint]$probe.LocalEndpoint).Port
    $probe.Stop()
    return $port
}

function Build-DisposablePlayer {
    param([Parameter(Mandatory = $true)] [string] $UnityPath)

    if ($script:ReuseBuild -and (Test-Path -LiteralPath $script:executablePath -PathType Leaf)) {
        Write-Output "== Step 1: reusing the existing player at $script:executablePath =="
        Write-Output 'NOTE: -ReuseBuild is an inner-loop convenience and is not milestone evidence.'
        return
    }

    Write-Output '== Step 1: build the disposable Win64 player =='
    $activeEditor = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue
    if ($activeEditor) {
        throw 'A Unity Editor process is already active. Monitor that process instead of starting another build.'
    }
    if (Test-Path -LiteralPath $script:buildLog) { Remove-Item -LiteralPath $script:buildLog -Force }
    if (Test-Path -LiteralPath $script:executablePath) { Remove-Item -LiteralPath $script:executablePath -Force }
    $buildArguments = @(
        '-batchmode', '-quit',
        '-projectPath', ('"{0}"' -f $script:projectRoot),
        '-executeMethod', 'GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildWindowsSmokePlayer',
        '-logFile', ('"{0}"' -f $script:buildLog)
    )
    $build = Start-Process -FilePath $UnityPath -ArgumentList $buildArguments -Wait -PassThru -WindowStyle Hidden
    if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $script:executablePath -PathType Leaf)) {
        throw "The player build did not succeed (exit $($build.ExitCode)). See $script:buildLog"
    }
    Write-Output "Built: $script:executablePath"
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

function Format-PlayerArgument {
    param([Parameter(Mandatory = $true)] [string] $Value)
    if ($Value -match '[\s"]') { return '"' + $Value.Replace('"', '\"') + '"' }
    return $Value
}

<#
    Starts one rehearsal player. The role, mode, seat and seed are ordinary launch data and travel
    on the command line; the address, port and session key are the invitation and travel only in
    this child's own environment block, which no Unity banner prints.
#>
function Start-RehearsalPlayer {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [ValidateSet('host', 'client')] [string] $Role,
        [Parameter(Mandatory = $true)] [ValidateSet('match', 'host-loss')] [string] $Mode,
        [Parameter(Mandatory = $false)] [string] $Seat = '',
        [Parameter(Mandatory = $true)] [string] $Address,
        [Parameter(Mandatory = $true)] [int] $Port,
        [Parameter(Mandatory = $true)] [string] $Key,
        [Parameter(Mandatory = $true)] [int] $BudgetSeconds,
        [Parameter(Mandatory = $true)] [string] $LogPath
    )

    if (Test-Path -LiteralPath $LogPath) { Remove-Item -LiteralPath $LogPath -Force }

    $arguments = @(
        '-batchmode', '-nographics',
        '-guardsPrivateLobby',
        '-guardsOnlineTimeoutSeconds', $BudgetSeconds.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        '-guardsRehearsalRole', $Role,
        '-guardsRehearsalMode', $Mode,
        '-guardsRehearsalSeed', $script:Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    )
    if (-not [string]::IsNullOrWhiteSpace($Seat)) {
        $arguments += @('-guardsRehearsalSeat', $Seat)
    }
    $arguments += @('-logFile', $LogPath)

    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $script:executablePath
    $info.WorkingDirectory = [System.IO.Path]::GetDirectoryName($script:executablePath)
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.Arguments = (($arguments | ForEach-Object { Format-PlayerArgument -Value $_ }) -join ' ')
    $info.EnvironmentVariables[$script:addressVariable] = $Address
    $info.EnvironmentVariables[$script:portVariable] = $Port.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    $info.EnvironmentVariables[$script:keyVariable] = $Key

    $process = [System.Diagnostics.Process]::Start($info)
    $script:processes += $process
    return [pscustomobject]@{ Name = $Name; Process = $process; Log = $LogPath; Seat = $Seat }
}

function Assert-PlayerExit {
    param(
        [Parameter(Mandatory = $true)] [object[]] $Players,
        [Parameter(Mandatory = $true)] [int] $DeadlineSeconds,
        [Parameter(Mandatory = $true)] [string] $Description
    )

    $deadline = (Get-Date).AddSeconds($DeadlineSeconds)
    foreach ($player in $Players) {
        $remaining = [int][Math]::Max(1, ($deadline - (Get-Date)).TotalSeconds)
        if (-not $player.Process.WaitForExit($remaining * 1000)) {
            throw "$Description : $($player.Name) (pid $($player.Process.Id)) did not exit on its own inside the bounded window."
        }
    }
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

$rehearsalFailed = $false
$allLogs = @()

if ($Topology -eq 'LoopbackSmoke') {
    foreach ($stale in @($hostLog)) {
        if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale -Force }
    }

    try {
        Build-DisposablePlayer -UnityPath $unityPath

        Write-Output '== Step 2: pick a free loopback port and start the host =='
        $port = Get-FreeLoopbackPort
        $address = "127.0.0.1:$port"
        $sessionKey = [System.Guid]::NewGuid().ToString('N')
        $rehearsalKeys += $sessionKey
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
            if ($output -notmatch ('CLIENT_SEATED seat=' + [regex]::Escape($seat) + '(\r|\n|$)')) {
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

        $allLogs = @($hostLog) + @($clientLogs.Values)
    }
    catch {
        Write-Output "FAIL: $($_.Exception.Message)"
        $rehearsalFailed = $true
        $allLogs = @($hostLog) + @($clientLogs.Values)
    }
    finally {
        foreach ($process in $processes) {
            try { if (-not $process.HasExited) { $process.Kill() } } catch { }
        }
    }
}
else {
    $matchHostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-sixclient-match-host.log')
    $lossHostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-sixclient-hostloss-host.log')
    $matchPlayers = @()
    $lossPlayers = @()
    $matchClients = @()
    $lossClients = @()
    $identityLine = ''

    try {
        Build-DisposablePlayer -UnityPath $unityPath

        # ------------------------------------------------------------------ match cohort
        Write-Output '== Step 2: match cohort - one seatless host and six seated clients on loopback =='
        $matchPort = Get-FreeLoopbackPort
        $matchKey = [System.Guid]::NewGuid().ToString('N')
        $rehearsalKeys += $matchKey
        Write-Output "Loopback endpoint: 127.0.0.1:$matchPort"

        $clientBudget = [Math]::Max(300, $TimeoutSeconds - 120)
        $matchHost = Start-RehearsalPlayer -Name 'match-host' -Role 'host' -Mode 'match' `
            -Address '127.0.0.1' -Port $matchPort -Key $matchKey -BudgetSeconds $clientBudget -LogPath $matchHostLog
        $matchPlayers += $matchHost
        Wait-ForLogMarker -LogPath $matchHostLog -Pattern 'GUARDS_ONLINE: LOBBY_HOSTING port=' `
            -DeadlineSeconds 180 -Description 'the rehearsal host to bind its lobby'
        Wait-ForLogMarker -LogPath $matchHostLog -Pattern 'GUARDS_ONLINE: REHEARSAL_HOST seed=' `
            -DeadlineSeconds 60 -Description 'the rehearsal host identity line'

        foreach ($seat in $seats) {
            $log = [System.IO.Path]::Combine($tmpRoot, "private-online-sixclient-match-$seat.log")
            $player = Start-RehearsalPlayer -Name $seat -Role 'client' -Mode 'match' -Seat $seat `
                -Address '127.0.0.1' -Port $matchPort -Key $matchKey -BudgetSeconds $clientBudget -LogPath $log
            $matchPlayers += $player
            $matchClients += $player
        }

        $matchLogs = @($matchHostLog) + @($matchClients | ForEach-Object { $_.Log })
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $matchHostLog; Pattern = 'GUARDS_ONLINE: REHEARSAL_TOPOLOGY host=1 clients=6' },
                [pscustomobject]@{ Log = $matchHostLog; Pattern = 'GUARDS_ONLINE: MATCH_STARTED' }
            ) -WatchLogs $matchLogs -DeadlineSeconds 420 -Description 'the six-client match to begin'
        Write-Output 'Match started: host=1 clients=6.'

        $handoff = @()
        foreach ($player in $matchClients) {
            $handoff += [pscustomobject]@{ Log = $player.Log; Pattern = ('REHEARSAL_CLIENT event=hud seat=' + [regex]::Escape($player.Seat)) }
        }
        Wait-ForCohortMarkers -Requirements $handoff -WatchLogs $matchLogs -DeadlineSeconds 300 `
            -Description 'every seat to receive the owner-scoped HUD handoff'
        Write-Output 'All six seats hold the owner-scoped HUD.'

        # The forced agreement flows, then the reconnect, then the ending. Each is bounded on its
        # own, so a stall names the flow it stalled in rather than the whole match.
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $matchClients[0].Log; Pattern = 'REHEARSAL_CLIENT event=initiative-disagreement' }
            ) -WatchLogs $matchLogs -DeadlineSeconds 420 -Description 'the forced same-team tied-Initiative disagreement'
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $matchClients[0].Log; Pattern = 'REHEARSAL_CLIENT event=initiative-pass' }
            ) -WatchLogs $matchLogs -DeadlineSeconds 300 -Description 'the revised unanimous Initiative preference'
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = ($matchClients | Where-Object { $_.Seat -eq $reconnectSeat }).Log
                    Pattern = 'REHEARSAL_CLIENT event=reconnect-pass' }
            ) -WatchLogs $matchLogs -DeadlineSeconds 420 -Description ('the ' + $reconnectSeat + ' disconnect and lobby reconnect')

        $outcomes = @()
        foreach ($player in $matchClients) {
            $outcomes += [pscustomobject]@{ Log = $player.Log; Pattern = 'GUARDS_ONLINE: REHEARSAL_OUTCOME seat=' }
        }
        $outcomes += [pscustomobject]@{ Log = $matchHostLog; Pattern = 'GUARDS_ONLINE: REHEARSAL_OUTCOME version=' }
        Wait-ForCohortMarkers -Requirements $outcomes -WatchLogs $matchLogs -DeadlineSeconds ([Math]::Max(600, $TimeoutSeconds - 300)) `
            -Description 'the complete match to reach one authoritative outcome on every process'

        Write-Output '== Step 3: every match-cohort process exits on its own =='
        Assert-PlayerExit -Players $matchPlayers -DeadlineSeconds 240 -Description 'match cohort'

        # ------------------------------------------------------------------ host-loss cohort
        Write-Output '== Step 4: host-loss cohort - the same topology, then the host is terminated =='
        $lossPort = Get-FreeLoopbackPort
        $lossKey = [System.Guid]::NewGuid().ToString('N')
        $rehearsalKeys += $lossKey
        Write-Output "Loopback endpoint: 127.0.0.1:$lossPort"

        $lossBudget = 600
        $lossHost = Start-RehearsalPlayer -Name 'hostloss-host' -Role 'host' -Mode 'host-loss' `
            -Address '127.0.0.1' -Port $lossPort -Key $lossKey -BudgetSeconds $lossBudget -LogPath $lossHostLog
        Wait-ForLogMarker -LogPath $lossHostLog -Pattern 'GUARDS_ONLINE: LOBBY_HOSTING port=' `
            -DeadlineSeconds 180 -Description 'the host-loss host to bind its lobby'

        foreach ($seat in $seats) {
            $log = [System.IO.Path]::Combine($tmpRoot, "private-online-sixclient-hostloss-$seat.log")
            $player = Start-RehearsalPlayer -Name $seat -Role 'client' -Mode 'host-loss' -Seat $seat `
                -Address '127.0.0.1' -Port $lossPort -Key $lossKey -BudgetSeconds $lossBudget -LogPath $log
            $lossPlayers += $player
            $lossClients += $player
        }

        $lossLogs = @($lossHostLog) + @($lossClients | ForEach-Object { $_.Log })
        $ready = @([pscustomobject]@{ Log = $lossHostLog; Pattern = 'GUARDS_ONLINE: REHEARSAL_HOSTLOSS_READY version=' })
        foreach ($player in $lossClients) {
            $ready += [pscustomobject]@{ Log = $player.Log
                Pattern = ('REHEARSAL_CLIENT event=host-loss-ready seat=' + [regex]::Escape($player.Seat)) }
        }
        Wait-ForCohortMarkers -Requirements $ready -WatchLogs $lossLogs -DeadlineSeconds 420 `
            -Description 'the host-loss cohort to reach the match and stand ready'

        # THE ONE DELIBERATE EXTERNAL KILL IN THIS RUNNER. The fault plan's "terminate the host"
        # case has no in-process expression: a host that shut itself down politely would be an
        # orderly close, which is a different row. Every other process in both cohorts must always
        # exit on its own, and the checks below still require that of all six clients here.
        Write-Output "Terminating the host process (pid $($lossHost.Process.Id)) - the fault plan's host-termination case."
        $lossHost.Process.Kill()
        $null = $lossHost.Process.WaitForExit(60000)

        $lossPass = @()
        foreach ($player in $lossClients) {
            $lossPass += [pscustomobject]@{ Log = $player.Log
                Pattern = ('REHEARSAL_CLIENT event=host-loss-pass seat=' + [regex]::Escape($player.Seat)) }
        }
        Wait-ForCohortMarkers -Requirements $lossPass -WatchLogs $lossLogs -DeadlineSeconds 240 `
            -Description 'every client to observe terminal host loss with its input disabled'

        Write-Output '== Step 5: every host-loss client exits on its own =='
        Assert-PlayerExit -Players $lossClients -DeadlineSeconds 180 -Description 'host-loss cohort'

        # ------------------------------------------------------------------ evidence
        Write-Output '== Step 6: read every log; exit codes alone are never evidence =='
        $matchHostText = Get-LogText -LogPath $matchHostLog

        $identityMatch = [regex]::Match($matchHostText, 'GUARDS_ONLINE: REHEARSAL_HOST seed=\d+ mode=match (?<identity>.+)')
        if (-not $identityMatch.Success) {
            Write-Output 'FAIL: the host never recorded the build/protocol/schema/map/content identity.'
            $rehearsalFailed = $true
        }
        else {
            $identityLine = $identityMatch.Groups['identity'].Value.Trim()
            Write-Output "Rehearsed identity: $identityLine"
        }

        foreach ($marker in @('GUARDS_ONLINE: LOBBY_HOSTING port=', 'GUARDS_ONLINE: REHEARSAL_TOPOLOGY host=1 clients=6',
                'GUARDS_ONLINE: SEATS_COMPLETE claimed=6/6', 'GUARDS_ONLINE: MATCH_STARTED',
                'GUARDS_ONLINE: REHEARSAL_OUTCOME version=', 'GUARDS_ONLINE: MATCH_OUTCOME version=',
                'GUARDS_ONLINE: REHEARSAL_HOST_COMPLETE', 'GUARDS_ONLINE: EXIT 0')) {
            if ($matchHostText -notmatch $marker) {
                Write-Output "FAIL: the match host log is missing '$marker'."
                $rehearsalFailed = $true
            }
        }

        $claimed = [regex]::Matches($matchHostText, 'SEAT_CLAIMED ordinal=\d+ seat=([a-z0-9-]+)') |
            ForEach-Object { $_.Groups[1].Value }
        $distinct = @($claimed | Sort-Object -Unique)
        if (@($claimed).Count -ne 6 -or $distinct.Count -ne 6) {
            Write-Output "FAIL: expected exactly six distinct SEAT_CLAIMED lines in the match cohort; saw $(@($claimed).Count) claims over $($distinct.Count) seats."
            $rehearsalFailed = $true
        }
        foreach ($seat in $seats) {
            if ($distinct -notcontains $seat) {
                Write-Output "FAIL: the match cohort never claimed the canonical seat $seat."
                $rehearsalFailed = $true
            }
        }

        $hostOutcome = [regex]::Match($matchHostText, 'REHEARSAL_OUTCOME version=\d+ cause=(?<cause>\S+) winner=(?<winner>\S+)')
        if (-not $hostOutcome.Success) {
            Write-Output 'FAIL: the host recorded no authoritative outcome cause and winner.'
            $rehearsalFailed = $true
        }

        # Milestone 8.36. The same ending read off the ordinary lobby path rather than off the
        # rehearsal driver's own marker. An invited playtest has no rehearsal driver attached, so
        # this is the line the returned tester logs will actually carry and it has to be proven by
        # a run that still has both to compare.
        $hostPlainOutcome = [regex]::Match($matchHostText, 'MATCH_OUTCOME version=\d+ cause=(?<cause>\S+) winner=(?<winner>\S+)')
        if (-not $hostPlainOutcome.Success) {
            Write-Output 'FAIL: the host wrote no ordinary-path MATCH_OUTCOME line.'
            $rehearsalFailed = $true
        }
        elseif ($hostOutcome.Success -and
            ($hostPlainOutcome.Groups['cause'].Value -ne $hostOutcome.Groups['cause'].Value -or
                $hostPlainOutcome.Groups['winner'].Value -ne $hostOutcome.Groups['winner'].Value)) {
            Write-Output 'FAIL: the host ordinary-path outcome disagrees with its own rehearsal outcome.'
            $rehearsalFailed = $true
        }

        # Convergence: the host's authoritative ending, restated by every seat.
        $seenReconnect = $false
        $cohortMarkers = @{}
        foreach ($player in $matchClients) {
            $text = Get-LogText -LogPath $player.Log
            foreach ($required in @('REHEARSAL_CLIENT event=hud', 'REHEARSAL_CLIENT event=initiative-pass',
                    'REHEARSAL_CLIENT event=tie-breaker-pass', 'REHEARSAL_CLIENT event=minion-pass',
                    'REHEARSAL_CLIENT event=privacy-pass', 'GUARDS_ONLINE: EXIT 0')) {
                if ($text -notmatch $required) {
                    Write-Output "FAIL: seat $($player.Seat) is missing '$required'. See $($player.Log)"
                    $rehearsalFailed = $true
                }
            }
            foreach ($cohort in @('initiative-disagreement', 'initiative-revision', 'minion-partial',
                    'minion-proposal-invalidated')) {
                if ($text -match ('REHEARSAL_CLIENT event=' + $cohort)) { $cohortMarkers[$cohort] = $true }
            }
            if ($player.Seat -eq $reconnectSeat) {
                foreach ($required in @('REHEARSAL_CLIENT event=reconnect-input-cleared',
                        'REHEARSAL_CLIENT event=reconnect-pass')) {
                    if ($text -notmatch $required) {
                        Write-Output "FAIL: seat $($player.Seat) is missing '$required'. See $($player.Log)"
                        $rehearsalFailed = $true
                    }
                    else { $seenReconnect = $true }
                }
            }
            if ($player.Process.ExitCode -ne 0) {
                Write-Output "FAIL: seat $($player.Seat) exited $($player.Process.ExitCode)."
                $rehearsalFailed = $true
            }

            $seatOutcome = [regex]::Match($text, 'REHEARSAL_OUTCOME seat=(?<seat>\S+) version=\d+ cause=(?<cause>\S+) winner=(?<winner>\S+)')
            if (-not $seatOutcome.Success) {
                Write-Output "FAIL: seat $($player.Seat) recorded no outcome. See $($player.Log)"
                $rehearsalFailed = $true
            }
            elseif ($hostOutcome.Success) {
                if ($seatOutcome.Groups['cause'].Value -ne $hostOutcome.Groups['cause'].Value -or
                    $seatOutcome.Groups['winner'].Value -ne $hostOutcome.Groups['winner'].Value) {
                    Write-Output ("FAIL: seat $($player.Seat) diverged from the host: " +
                        "$($seatOutcome.Groups['cause'].Value)/$($seatOutcome.Groups['winner'].Value) " +
                        "against $($hostOutcome.Groups['cause'].Value)/$($hostOutcome.Groups['winner'].Value).")
                    $rehearsalFailed = $true
                }
            }

            # Milestone 8.36. The seat's own ordinary-path line, named by its seat so six returned
            # logs can be told apart, and compared with the host's ordinary-path line.
            $seatPlainOutcome = [regex]::Match($text, 'MATCH_OUTCOME seat=(?<seat>\S+) version=\d+ cause=(?<cause>\S+) winner=(?<winner>\S+)')
            if (-not $seatPlainOutcome.Success) {
                Write-Output "FAIL: seat $($player.Seat) wrote no ordinary-path MATCH_OUTCOME line. See $($player.Log)"
                $rehearsalFailed = $true
            }
            else {
                if ($seatPlainOutcome.Groups['seat'].Value -ne $player.Seat) {
                    Write-Output ("FAIL: seat $($player.Seat) wrote an outcome naming " +
                        "$($seatPlainOutcome.Groups['seat'].Value).")
                    $rehearsalFailed = $true
                }
                if ($hostPlainOutcome.Success -and
                    ($seatPlainOutcome.Groups['cause'].Value -ne $hostPlainOutcome.Groups['cause'].Value -or
                        $seatPlainOutcome.Groups['winner'].Value -ne $hostPlainOutcome.Groups['winner'].Value)) {
                    Write-Output ("FAIL: seat $($player.Seat) diverged from the host on the ordinary path: " +
                        "$($seatPlainOutcome.Groups['cause'].Value)/$($seatPlainOutcome.Groups['winner'].Value) " +
                        "against $($hostPlainOutcome.Groups['cause'].Value)/$($hostPlainOutcome.Groups['winner'].Value).")
                    $rehearsalFailed = $true
                }
            }
        }

        foreach ($cohort in @('initiative-disagreement', 'initiative-revision', 'minion-partial',
                'minion-proposal-invalidated')) {
            if (-not $cohortMarkers.ContainsKey($cohort)) {
                Write-Output "FAIL: no seat in the match cohort recorded '$cohort'."
                $rehearsalFailed = $true
            }
        }
        if (-not $seenReconnect) {
            Write-Output "FAIL: the $reconnectSeat reconnect was never recorded."
            $rehearsalFailed = $true
        }
        if ($matchHost.Process.ExitCode -ne 0) {
            Write-Output "FAIL: the match host exited $($matchHost.Process.ExitCode)."
            $rehearsalFailed = $true
        }

        $lossHostText = Get-LogText -LogPath $lossHostLog
        if ($lossHostText -notmatch 'GUARDS_ONLINE: REHEARSAL_HOSTLOSS_READY version=') {
            Write-Output 'FAIL: the host-loss host never reached the match.'
            $rehearsalFailed = $true
        }
        if ($lossHostText -notmatch 'GUARDS_ONLINE: MATCH_STARTED') {
            Write-Output 'FAIL: the host-loss cohort never started its match.'
            $rehearsalFailed = $true
        }
        foreach ($player in $lossClients) {
            $text = Get-LogText -LogPath $player.Log
            foreach ($required in @('REHEARSAL_CLIENT event=hud', 'REHEARSAL_CLIENT event=host-loss-ready',
                    'REHEARSAL_CLIENT event=host-loss-pass', 'GUARDS_ONLINE: EXIT 0')) {
                if ($text -notmatch $required) {
                    Write-Output "FAIL: host-loss seat $($player.Seat) is missing '$required'. See $($player.Log)"
                    $rehearsalFailed = $true
                }
            }
            if ($player.Process.ExitCode -ne 0) {
                Write-Output "FAIL: host-loss seat $($player.Seat) exited $($player.Process.ExitCode)."
                $rehearsalFailed = $true
            }
        }

        $allLogs = @($matchLogs) + @($lossLogs)
    }
    catch {
        Write-Output "FAIL: $($_.Exception.Message)"
        $rehearsalFailed = $true
        $allLogs = @($matchHostLog, $lossHostLog) +
            @($matchClients | ForEach-Object { $_.Log }) + @($lossClients | ForEach-Object { $_.Log })
    }
    finally {
        foreach ($process in $processes) {
            try { if (-not $process.HasExited) { $process.Kill() } } catch { }
        }
    }
}

# ---------------------------------------------------------------------------- privacy review
# Connections are opaque ordinals everywhere. The only ip:port any log may carry is the
# operator-supplied loopback endpoint, no log may carry the invitation's session key, and no log
# may match a degradation pattern.
foreach ($log in ($allLogs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
    if (-not (Test-Path -LiteralPath $log -PathType Leaf)) {
        Write-Output "FAIL: expected log $log was never written."
        $rehearsalFailed = $true
        continue
    }

    $output = Get-LogText -LogPath $log
    $endpoints = [regex]::Matches($output, '\b(\d{1,3}\.){3}\d{1,3}:\d+') | ForEach-Object { $_.Value }
    $foreign = @($endpoints | Where-Object { $_ -notlike '127.0.0.1:*' } | Sort-Object -Unique)
    if ($foreign.Count -gt 0) {
        Write-Output "FAIL: $log carries a non-operator endpoint: $($foreign -join ', ')"
        $rehearsalFailed = $true
    }
    foreach ($key in $rehearsalKeys) {
        if ($output.IndexOf($key, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Output "FAIL: $log discloses the session key."
            $rehearsalFailed = $true
        }
    }
    foreach ($pattern in @('Shader.*not found', 'Failed to load', 'is missing', 'NullReferenceException')) {
        $hits = [regex]::Matches($output, $pattern)
        if ($hits.Count -gt 0) {
            Write-Output "FAIL: $log matched the degradation pattern '$pattern' $($hits.Count) time(s)."
            $rehearsalFailed = $true
        }
    }
}

foreach ($log in ($allLogs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)) {
    Write-Output "Log: $log"
}

if ($rehearsalFailed) {
    Write-Error 'REHEARSAL FAILED: see the FAIL lines and logs above.'
    exit 1
}

if ($Topology -eq 'LoopbackSmoke') {
    Write-Output 'REHEARSAL PASSED: one seatless host, six distinct seats, and three named refusals on loopback.'
}
else {
    Write-Output ('REHEARSAL PASSED: one seatless host and six seated clients completed a full match with the ' +
        'forced Initiative and Minion Battle agreement flows, a mid-match reconnect, converged outcome, ' +
        'privacy-reviewed logs, and a terminal host-loss cohort.')
    Write-Output 'A same-machine rehearsal proves no relay, NAT traversal, external latency, multi-network, or real-internet behaviour.'
}
exit 0
