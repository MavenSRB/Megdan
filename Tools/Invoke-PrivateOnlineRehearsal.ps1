<#
.SYNOPSIS
    Private-online rehearsal: one dedicated seatless host and six client processes on loopback.

.DESCRIPTION
    The local multi-process runner named by Docs/AI/Testing/PRIVATE_ONLINE.md. It owns three
    topologies against one disposable Win64 player.

    -Topology LoopbackSmoke -FaultPlan None (milestone 8.31)
        Builds the player once, then starts one host process and, in a deterministic order, nine
        client processes against it on the loopback interface: one first seat, one duplicate claim
        of that seat (refused by name while five seats stay open), the remaining five seats, one
        compatible straggler into the full session, and one client offering the wrong session key.
        It proves seating and refusal vocabulary only; it plays no match.

    -Topology SixClient -FaultPlan Full (milestone 8.35c; third cohort added at 8.35k)
        The playtest-build rehearsal. Three cohorts run against one freshly built player, each one
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

          rejoin cohort     milestone 8.35k, and the one the 2026-09-03 playtest needed. The same
                            topology reaches the match, and then red-player-02's PROCESS EXITS -
                            no Goodbye, so the host reserves the seat exactly as it does for a
                            crash. The runner then launches a BRAND-NEW process on the same seat,
                            which carries nothing across the process boundary but what it finds on
                            disk, and it must return by SEAT_RECLAIMED with a rotated epoch and
                            converge on the same outcome as the other five. The match cohort's
                            reconnect above replaces only a socket, under a client object that
                            survives holding its capability - which is the one path a real player
                            never walks, and why every tier's reconnect evidence missed this.

    -Topology Solo -FaultPlan None (THIRTEENTH amendment; bot-filled seats for solo playtesting)
        Proves the shipped practice-driver feature end to end, through its own real controls, not a
        shortcut around them. One dedicated seatless host process, entering the real primitive lobby
        exactly as SixClient's cohorts do, presses its own FILL WITH TEST DRIVERS control the moment
        it starts hosting; one scripted client process joins the one designated seat as "the human"
        would. The five OTHER canonical seats are never launched by this script at all - they exist
        only because the host's own PrivateLobbyDriver spawned them, which is the whole point: this
        is the one cohort that proves a solo player's actual host process can fill a table by itself.
        Six distinct SEAT_CLAIMED lines on the host's own log (one the scripted client's, five the
        spawned drivers') stand in for a run that launched only two processes. Every driver's
        default human-followable think-delay is overridden to a fast one via
        GUARDS_TESTDRIVER_THINK_SECONDS, set on the host's own environment before it presses the
        control and read from there by the host to hand each spawned child - real solo play never
        sets this and keeps the slow, watchable default. Once the host and the scripted client have
        both exited on their own, this cohort's own closing check requires that no spawned
        GuardsDigitalSmoke.exe process is still running - the host's proof that it killed its own
        children rather than leaving them behind.

    8.35t corrective additions to -Topology Solo. Two more proofs run ahead of the main cohort:
    the packaged interactive-lobby command line - exactly the flag shape both shipped ".bat"
    launchers use (-guardsPrivateLobby, no rehearsal-role flags, so no scripted driver ever
    attaches) - still reaches the real lobby's own GUARDS_ONLINE: LOBBY_BEGIN cleanly, since
    nothing headless can drive it further than boot the way a human double-clicking the launcher
    would; and a join dialed at a port nothing is listening on - the interim-4 live defect's exact
    shape - now fails fast and visibly (GUARDS_ONLINE: FAIL test-driver-join-failed, exit 1) instead
    of the old silent hang, read with its own bounded wait rather than the shared helpers every
    other wait in this script uses, because a FAIL line here is the proof, not a reason to abort.

    -Topology SixClient requires -FaultPlan Full, -Topology LoopbackSmoke and -Topology Solo both
    require -FaultPlan None. The 8.35c matrix row names exactly one six-client combination - "full match,
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
    [ValidateSet('LoopbackSmoke', 'SixClient', 'Solo', 'MultiClientSmoke')] [string] $Topology = 'LoopbackSmoke',
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
# THIRTEENTH amendment. Solo proves the shipped bot-seat feature, not a fault-injection matrix row -
# it never carries a fault plan, so a weaker run cannot be mistaken for a stronger one either way.
if ($Topology -eq 'Solo' -and $FaultPlan -ne 'None') {
    Write-Error "REHEARSAL REFUSED: -Topology Solo carries no fault plan; use -FaultPlan None."
    exit 1
}
# Milestone 8.35z54. The package ceremony's required multi-client step carries no fault plan
# either: it is a REACHABILITY gate, not a resilience one, and SixClient/Full remains the only
# combination that proves the matrix row.
if ($Topology -eq 'MultiClientSmoke' -and $FaultPlan -ne 'None') {
    Write-Error ("REHEARSAL REFUSED: -Topology MultiClientSmoke carries no fault plan; use " +
        "-FaultPlan None. The fault matrix belongs to -Topology SixClient -FaultPlan Full.")
    exit 1
}
if ($TimeoutSeconds -le 0) {
    if ($Topology -eq 'SixClient') { $TimeoutSeconds = 2400 }
    # It stops at the first commitment window and plays no match at all, so its whole budget is a
    # build, three process starts, a lobby flow and one composition.
    elseif ($Topology -eq 'MultiClientSmoke') { $TimeoutSeconds = 600 }
    # A truly random-legal match - no strategy, by the amendment's own rule - has no reason to
    # converge quickly: measured at ~1.7 accepted commands/second even at the fast regression
    # pace, and a full six-player 3-Wave Quick Game under aimless play can run into the low
    # thousands of commands before a side runs out of Lives or the Waves end. Budgeted generously
    # rather than optimized for speed; this cohort runs once per verification, not per commit.
    elseif ($Topology -eq 'Solo') { $TimeoutSeconds = 3600 }
    else { $TimeoutSeconds = 420 }
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
            # Milestone 8.35k. @() is load-bearing, not decoration. A pipeline matching exactly ONE
            # line returns that line as a bare string, and [0] on a string is its first CHARACTER -
            # so this reported "[System.Char] does not contain a method named 'Trim'" and threw,
            # replacing the real rehearsal failure with a PowerShell error at the one moment the
            # runner exists to explain what went wrong. Every failure reported here is a first
            # failure, so the single-match case is the COMMON one, not the edge.
            $lines = @($text -split "`r?`n" | Where-Object { $_ -match 'GUARDS_ONLINE: FAIL' })
            if ($lines.Count -gt 0) {
                return "$([string]$lines[0]) (see $path)".Trim()
            }
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
        [Parameter(Mandatory = $true)] [ValidateSet('match', 'host-loss', 'rejoin', 'rejoin-return', 'solo', 'smoke')] [string] $Mode,
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
elseif ($Topology -eq 'MultiClientSmoke') {
    # ---------------------------------------------------------------------------------------------
    # MILESTONE 8.35z54 - THE PACKAGE CEREMONY'S REQUIRED MULTI-CLIENT STEP.
    #
    # WHY IT EXISTS. The 8.35z51 package shipped a lobby whose picked-line-up match was unplayable on
    # all six seats, and every automated tier was green when it shipped, because every lobby journey
    # drives the HOST's own view. Only real processes disagreeing about a seat can see that class of
    # defect. This is the cheapest topology that can: one host, TWO real joiners, the rest of the
    # table filled by the host's own control, stopping at the first commitment window.
    #
    # WHAT IT ASSERTS - the direct negatives of the five 2026-09-08 playtest findings:
    #   1  per-seat authorization  each client's own AuditProjection passes (it fails the process if
    #                              any offered command is not held by that seat) and REHEARSAL_SMOKE_PASS
    #                              reports a non-empty authorized set per seat
    #   2  a pick visibly lands    the two joiners field two DIFFERENT chosen heroes
    #   3  the flow                seatless join -> TAKE SEAT -> READY -> pick -> match, walked through
    #                              the shipped controls, with FILL WITH BOTS supplying the rest
    #   4  nothing acts early      REHEARSAL_PRECOMMIT is version 0 in AwaitingCommitments on every seat
    #                              AND on the host
    #   5  one board               REHEARSAL_BOARDRECT is configured=True and identical across processes
    #
    # WHAT IT DOES NOT DO, so it is never mistaken for the bigger gate: it plays no match, injects no
    # fault, exercises no reconnect and no host loss. Those are -Topology SixClient -FaultPlan Full.
    # ---------------------------------------------------------------------------------------------
    $smokeHostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-smoke-host.log')
    $smokeSeats = @($seats[0], $seats[3])
    $smokePlayers = @()
    $smokeClients = @()

    try {
        if (-not $ReuseBuild) { Build-DisposablePlayer -UnityPath $unityPath }
        elseif (-not (Test-Path -LiteralPath $executablePath)) {
            throw "-ReuseBuild was given but no player exists at $executablePath."
        }

        Write-Output '== Step 2: one seatless host and TWO real joiners on loopback =='
        $smokePort = Get-FreeLoopbackPort
        $smokeKey = [System.Guid]::NewGuid().ToString('N')
        $rehearsalKeys += $smokeKey
        Write-Output "Loopback endpoint: 127.0.0.1:$smokePort"

        $smokeBudget = [Math]::Max(240, $TimeoutSeconds - 120)
        $smokeHost = Start-RehearsalPlayer -Name 'smoke-host' -Role 'host' -Mode 'smoke' `
            -Address '127.0.0.1' -Port $smokePort -Key $smokeKey -BudgetSeconds $smokeBudget -LogPath $smokeHostLog
        $smokePlayers += $smokeHost
        Wait-ForLogMarker -LogPath $smokeHostLog -Pattern 'GUARDS_ONLINE: LOBBY_HOSTING port=' `
            -DeadlineSeconds 180 -Description 'the smoke host to bind its lobby'

        foreach ($seat in $smokeSeats) {
            $log = [System.IO.Path]::Combine($tmpRoot, "private-online-smoke-$seat.log")
            $player = Start-RehearsalPlayer -Name "smoke-$seat" -Role 'client' -Mode 'smoke' -Seat $seat `
                -Address '127.0.0.1' -Port $smokePort -Key $smokeKey -BudgetSeconds $smokeBudget -LogPath $log
            $smokePlayers += $player
            $smokeClients += $player
            $watched = @($smokeHostLog) + @($smokeClients | ForEach-Object { $_.Log })
            # Each joiner must actually TAKE its chair from inside the session before the next one is
            # started: the claim is the flow half this topology exists to walk, and serialising the two
            # keeps the assertion about WHICH seat each process holds exact.
            Wait-ForCohortMarkers -Requirements @(
                    [pscustomobject]@{ Log = $log; Pattern = ('REHEARSAL_CLIENT event=seat-claimed-' + [regex]::Escape($seat)) }
                ) -WatchLogs $watched -DeadlineSeconds 180 `
                -Description ("the joiner for $seat to take its seat from inside the session")
        }

        $smokeLogs = @($smokeHostLog) + @($smokeClients | ForEach-Object { $_.Log })

        Write-Output '== Step 3: the host fills the rest of the table with bots, and the picks run =='
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $smokeHostLog; Pattern = 'REHEARSAL_SMOKE_FILLED claimed=' }
            ) -WatchLogs $smokeLogs -DeadlineSeconds 180 -Description 'the host to press FILL WITH BOTS'

        $reached = @()
        foreach ($player in $smokeClients) {
            $reached += [pscustomobject]@{ Log = $player.Log; Pattern = 'REHEARSAL_SMOKE_PASS seat=' }
        }
        $reached += [pscustomobject]@{ Log = $smokeHostLog; Pattern = 'REHEARSAL_SMOKE_HOST_READY version=' }
        Wait-ForCohortMarkers -Requirements $reached -WatchLogs $smokeLogs `
            -DeadlineSeconds ([Math]::Max(300, $TimeoutSeconds - 180)) `
            -Description 'every process to reach the first commitment window on the chosen line-up'

        Write-Output '== Step 4: the five findings, asserted on the logs =='
        $smokeRects = @()
        foreach ($player in $smokeClients) {
            $text = Get-Content -LiteralPath $player.Log -Raw

            $pass = [regex]::Match($text, 'REHEARSAL_SMOKE_PASS seat=(\S+) hero=(\S+) authorized=(\d+) version=(\d+)')
            if (-not $pass.Success) { throw "SMOKE FAILED: $($player.Seat) never reported its pass line." }
            if ([int]$pass.Groups[3].Value -le 0) {
                throw ("FINDING 1 REPRODUCED: $($player.Seat) was offered NO commands of its own - " +
                    'a seat that cannot act is the z51 defect exactly.')
            }
            Write-Output ("  SEAT {0}: hero={1} authorized={2}" -f `
                $pass.Groups[1].Value, $pass.Groups[2].Value, $pass.Groups[3].Value)

            $pre = [regex]::Match($text, 'REHEARSAL_PRECOMMIT seat=\S+ version=(\d+) phase=(\S+)')
            if (-not $pre.Success) { throw "SMOKE FAILED: $($player.Seat) never reported its first match state." }
            if ([int]$pre.Groups[1].Value -ne 0 -or $pre.Groups[2].Value -ne 'AwaitingCommitments') {
                throw ("FINDING 4 REPRODUCED: $($player.Seat) opened at version $($pre.Groups[1].Value) " +
                    "in phase $($pre.Groups[2].Value); nothing may be accepted before the first " +
                    'commitment window.')
            }

            $rect = [regex]::Match($text, 'REHEARSAL_BOARDRECT seat=\S+ (configured=\S+ ortho=\S+ panel=\S+ upper=\S+ px=\S+)')
            if (-not $rect.Success) { throw "SMOKE FAILED: $($player.Seat) never reported its board framing." }
            if ($rect.Groups[1].Value -match 'configured=False') {
                throw ("FINDING 5 REPRODUCED: $($player.Seat) never received the HUD's layout; its " +
                    'board still wears the framing it was constructed with.')
            }
            $smokeRects += $rect.Groups[1].Value
            Write-Output ("  BOARD {0}: {1}" -f $player.Seat, $rect.Groups[1].Value)
        }

        # FINDING 2. Two joiners must hold two DIFFERENT heroes: one hero on both seats would mean a
        # pick that was accepted and then discarded, which is the shape of the z51 report.
        $heroes = @($smokeClients | ForEach-Object {
            [regex]::Match((Get-Content -LiteralPath $_.Log -Raw),
                'REHEARSAL_SMOKE_PASS seat=\S+ hero=(\S+)').Groups[1].Value })
        if (@($heroes | Sort-Object -Unique).Count -ne $heroes.Count) {
            throw ('FINDING 2 REPRODUCED: two seats are fielding the same hero (' +
                ($heroes -join ', ') + '), so at least one accepted pick did not reach the match.')
        }
        Write-Output ("  FINDING 2 NEGATIVE HELD: each joiner fields its own chosen hero (" + ($heroes -join ', ') + ").")

        if (@($smokeRects | Sort-Object -Unique).Count -ne 1) {
            throw ('FINDING 5 REPRODUCED: the joiners do not agree on the board framing: ' +
                ((@($smokeRects | Sort-Object -Unique)) -join ' | '))
        }
        Write-Output '  FINDING 5 NEGATIVE HELD: every client framed the board identically.'

        $hostText = Get-Content -LiteralPath $smokeHostLog -Raw
        $hostReady = [regex]::Match($hostText, 'REHEARSAL_SMOKE_HOST_READY version=(\d+) phase=(\S+) seats=(\d+)')
        if (-not $hostReady.Success) { throw 'SMOKE FAILED: the host never reported its ready line.' }
        if ([int]$hostReady.Groups[1].Value -ne 0 -or $hostReady.Groups[2].Value -ne 'AwaitingCommitments') {
            throw ("FINDING 4 REPRODUCED ON THE AUTHORITY: the host composed the match at version " +
                "$($hostReady.Groups[1].Value) in phase $($hostReady.Groups[2].Value).")
        }
        Write-Output ("  FINDING 4 NEGATIVE HELD: host and both joiners opened at version 0 in " +
            "AwaitingCommitments, with $($hostReady.Groups[3].Value) seats claimed.")

        Write-Output '== Step 5: every process exits on its own =='
        Assert-PlayerExit -Players $smokePlayers -DeadlineSeconds 240 -Description 'multi-client smoke'

        Write-Output '== Step 6: read every log; exit codes alone are never evidence =='
        foreach ($player in $smokePlayers) {
            $output = Get-Content -LiteralPath $player.Log -Raw
            if ($output -match 'GUARDS_ONLINE: FAIL') {
                $line = ($output -split "`r?`n" | Where-Object { $_ -match 'GUARDS_ONLINE: FAIL' })[0]
                Write-Output "FAIL $($player.Name): $line"
                $rehearsalFailed = $true
            }
            if ($output -notmatch 'GUARDS_ONLINE: EXIT 0') {
                Write-Output "FAIL $($player.Name): did not report a clean exit."
                $rehearsalFailed = $true
            }
        }

        $identityLine = ([regex]::Match($hostText, 'GUARDS_ONLINE: IDENTITY [^\r\n]*')).Value
        if ($identityLine) { Write-Output $identityLine }
        $allLogs += @($smokePlayers | ForEach-Object { $_.Log })
    }
    catch {
        Write-Output "FAIL: $($_.Exception.Message)"
        $rehearsalFailed = $true
        $allLogs += @($smokePlayers | ForEach-Object { $_.Log })
    }
    finally {
        foreach ($process in $processes) {
            try { if (-not $process.HasExited) { $process.Kill() } } catch { }
        }
    }
}
elseif ($Topology -eq 'SixClient') {
    $matchHostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-sixclient-match-host.log')
    $lossHostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-sixclient-hostloss-host.log')
    # Milestone 8.35k. The rejoin cohort's host, and the relaunched seat's own log - a SEPARATE
    # path from the exited process's, because Start-RehearsalPlayer clears the log it is given and
    # the dead process's `rejoin-process-exiting` line is half the evidence.
    $rejoinHostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-sixclient-rejoin-host.log')
    $matchPlayers = @()
    $lossPlayers = @()
    $rejoinPlayers = @()
    $matchClients = @()
    $lossClients = @()
    $rejoinClients = @()
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

        # ---- Milestone 8.35z54: the eleven-hero acceptance's two live findings, asserted here.
        #
        # These read lines every seat wrote at the FIRST FRAME it held a match HUD, which is the only
        # moment either question is still about a fresh match. They are asserted after the cohort has
        # finished rather than waited on, because both lines are already in the logs by the time the
        # first agreement marker appears and a wait would only re-prove that.
        Write-Output '== Step 3: the two live playtest findings, on real processes =='
        $precommit = @()
        $boardRects = @()
        foreach ($player in $matchClients) {
            $text = Get-Content -LiteralPath $player.Log -Raw
            $pre = [regex]::Match($text, 'REHEARSAL_PRECOMMIT seat=\S+ version=(\d+) phase=(\S+) wireversion=(\d+)')
            if (-not $pre.Success) {
                throw "FINDING 4 UNPROVEN: $($player.Seat) never reported its first match state."
            }
            $precommit += [pscustomobject]@{
                Seat = $player.Seat; Version = [int]$pre.Groups[1].Value; Phase = $pre.Groups[2].Value }

            $rect = [regex]::Match($text, 'REHEARSAL_BOARDRECT seat=\S+ (configured=\S+ ortho=\S+ panel=\S+ upper=\S+ px=\S+)')
            if (-not $rect.Success) {
                throw "FINDING 5 UNPROVEN: $($player.Seat) never reported its board framing."
            }
            $boardRects += [pscustomobject]@{ Seat = $player.Seat; Rect = $rect.Groups[1].Value }
        }

        # FINDING 4's DIRECT NEGATIVE. A match that has accepted nothing stands at version 0 in
        # AwaitingCommitments. A hero that moved before the commitment window would have advanced the
        # projection past 0, and the phase names which window the seat is actually in.
        foreach ($entry in $precommit) {
            Write-Output ("  PRECOMMIT {0}: version={1} phase={2}" -f $entry.Seat, $entry.Version, $entry.Phase)
            if ($entry.Version -ne 0) {
                throw ("FINDING 4 REPRODUCED: $($entry.Seat) held a match HUD at projection version " +
                    "$($entry.Version) - something was accepted before the first commitment window.")
            }
            if ($entry.Phase -ne 'AwaitingCommitments') {
                throw ("FINDING 4 REPRODUCED: $($entry.Seat) opened in phase $($entry.Phase) rather " +
                    'than AwaitingCommitments.')
            }
        }
        Write-Output '  FINDING 4 NEGATIVE HELD: every seat opened at version 0 in AwaitingCommitments.'

        # FINDING 5's DIRECT NEGATIVE. Every seat configured its viewport, and every seat agrees on
        # the framing - one distinct value across the whole cohort.
        foreach ($entry in $boardRects) {
            Write-Output ("  BOARDRECT {0}: {1}" -f $entry.Seat, $entry.Rect)
            if ($entry.Rect -match 'configured=False') {
                throw ("FINDING 5 REPRODUCED: $($entry.Seat) never received the HUD's layout - its " +
                    'board is still wearing the framing it was constructed with.')
            }
        }
        $distinctRects = @($boardRects | Select-Object -ExpandProperty Rect | Sort-Object -Unique)
        if ($distinctRects.Count -ne 1) {
            throw ("FINDING 5 REPRODUCED: the cohort does not agree on the board framing; " +
                "$($distinctRects.Count) distinct values: " + ($distinctRects -join ' | '))
        }
        Write-Output "  FINDING 5 NEGATIVE HELD: all six seats framed the board identically ($($distinctRects[0]))."

        Write-Output '== Step 3b: every match-cohort process exits on its own =='
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

        # ------------------------------------------------------------------ rejoin cohort
        # Milestone 8.35k. The cohort the 2026-09-03 playtest needed and no tier had. The match
        # cohort above already cuts a seat mid-match and brings it back - but through
        # InterruptConnectionForRehearsal, which replaces the socket UNDER A SURVIVING CLIENT
        # OBJECT that still holds its capability. A real player relaunches. This cohort differs in
        # exactly one thing: the process dies, and a new one comes back to the same seat.
        Write-Output '== Step 6: rejoin cohort - the cut kills a PROCESS, and a new one returns =='
        $rejoinPort = Get-FreeLoopbackPort
        $rejoinKey = [System.Guid]::NewGuid().ToString('N')
        $rehearsalKeys += $rejoinKey
        Write-Output "Loopback endpoint: 127.0.0.1:$rejoinPort"

        $rejoinBudget = [Math]::Max(300, $TimeoutSeconds - 120)
        $rejoinHost = Start-RehearsalPlayer -Name 'rejoin-host' -Role 'host' -Mode 'rejoin' `
            -Address '127.0.0.1' -Port $rejoinPort -Key $rejoinKey -BudgetSeconds $rejoinBudget -LogPath $rejoinHostLog
        $rejoinPlayers += $rejoinHost
        Wait-ForLogMarker -LogPath $rejoinHostLog -Pattern 'GUARDS_ONLINE: LOBBY_HOSTING port=' `
            -DeadlineSeconds 180 -Description 'the rejoin host to bind its lobby'

        foreach ($seat in $seats) {
            $log = [System.IO.Path]::Combine($tmpRoot, "private-online-sixclient-rejoin-$seat.log")
            $player = Start-RehearsalPlayer -Name $seat -Role 'client' -Mode 'rejoin' -Seat $seat `
                -Address '127.0.0.1' -Port $rejoinPort -Key $rejoinKey -BudgetSeconds $rejoinBudget -LogPath $log
            $rejoinPlayers += $player
            $rejoinClients += $player
        }

        $rejoinLogs = @($rejoinHostLog) + @($rejoinClients | ForEach-Object { $_.Log })
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $rejoinHostLog; Pattern = 'GUARDS_ONLINE: MATCH_STARTED' }
            ) -WatchLogs $rejoinLogs -DeadlineSeconds 420 -Description 'the rejoin cohort match to begin'

        $leaver = $rejoinClients | Where-Object { $_.Seat -eq $reconnectSeat }
        $leaverLog = $leaver.Log
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $leaverLog
                    Pattern = ('REHEARSAL_CLIENT event=rejoin-process-exiting seat=' + [regex]::Escape($reconnectSeat)) }
            ) -WatchLogs $rejoinLogs -DeadlineSeconds 420 `
            -Description ('the ' + $reconnectSeat + ' process to reach its cut point and exit')

        # The process must actually be GONE. If it survived, this cohort is the match cohort again
        # and proves nothing, so this is an assertion rather than a convenience wait.
        if (-not $leaver.Process.WaitForExit(120000)) {
            throw "The $reconnectSeat process did not exit at its cut point; a surviving process would make this cohort a duplicate of the match cohort."
        }
        Write-Output "The $reconnectSeat PROCESS has exited; its client object and its in-memory capability went with it."

        # It crashed, it did not leave: no Goodbye was sent, so the host must HOLD the seat. A
        # SEAT_RELEASED here would mean the teardown said goodbye, and the cohort would be testing
        # forfeiture instead of return.
        Wait-ForLogMarker -LogPath $rejoinHostLog `
            -Pattern ('GUARDS_ONLINE: SEAT_RESERVED ordinal=\d+ seat=' + [regex]::Escape($reconnectSeat)) `
            -DeadlineSeconds 120 -Description 'the host to reserve the crashed seat rather than release it'

        Write-Output "Relaunching $reconnectSeat as a brand-new process, exactly as a player restarts the game."
        $returnLog = [System.IO.Path]::Combine($tmpRoot, "private-online-sixclient-rejoin-return-$reconnectSeat.log")
        $returned = Start-RehearsalPlayer -Name ($reconnectSeat + '-returned') -Role 'client' -Mode 'rejoin-return' `
            -Seat $reconnectSeat -Address '127.0.0.1' -Port $rejoinPort -Key $rejoinKey `
            -BudgetSeconds $rejoinBudget -LogPath $returnLog
        $rejoinPlayers += $returned
        $rejoinClients = @($rejoinClients | Where-Object { $_.Seat -ne $reconnectSeat }) + @($returned)
        $rejoinLogs = @($rejoinHostLog) + @($rejoinClients | ForEach-Object { $_.Log }) + @($leaverLog)

        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $returnLog
                    Pattern = ('REHEARSAL_CLIENT event=rejoin-return-pass seat=' + [regex]::Escape($reconnectSeat)) }
            ) -WatchLogs $rejoinLogs -DeadlineSeconds 420 `
            -Description 'the relaunched process to be seated again holding its own owner-scoped HUD'
        Wait-ForLogMarker -LogPath $rejoinHostLog `
            -Pattern ('GUARDS_ONLINE: SEAT_RECLAIMED ordinal=\d+ seat=' + [regex]::Escape($reconnectSeat) + ' epoch=2') `
            -DeadlineSeconds 120 -Description 'the host to record a RECLAIM with a rotated epoch'
        Write-Output "$reconnectSeat returned by RECLAIM with a rotated epoch, from a process that never held the capability in memory."

        $rejoinOutcomes = @()
        foreach ($player in $rejoinClients) {
            $rejoinOutcomes += [pscustomobject]@{ Log = $player.Log; Pattern = 'GUARDS_ONLINE: REHEARSAL_OUTCOME seat=' }
        }
        $rejoinOutcomes += [pscustomobject]@{ Log = $rejoinHostLog; Pattern = 'GUARDS_ONLINE: REHEARSAL_OUTCOME version=' }
        Wait-ForCohortMarkers -Requirements $rejoinOutcomes -WatchLogs $rejoinLogs `
            -DeadlineSeconds ([Math]::Max(600, $TimeoutSeconds - 300)) `
            -Description 'the rejoin cohort to reach one authoritative outcome, the returned seat included'

        Write-Output '== Step 7: every rejoin-cohort process exits on its own =='
        Assert-PlayerExit -Players $rejoinPlayers -DeadlineSeconds 240 -Description 'rejoin cohort'

        # ------------------------------------------------------------------ evidence
        Write-Output '== Step 8: read every log; exit codes alone are never evidence =='
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

        # -------------------------------------------------- rejoin cohort evidence
        # Milestone 8.35k. The cohort's claim is a pair of facts no single process can state: one
        # process announced it was exiting, and a DIFFERENT one came back to that seat by reclaim.
        $rejoinHostText = Get-LogText -LogPath $rejoinHostLog
        foreach ($marker in @('GUARDS_ONLINE: MATCH_STARTED',
                ('GUARDS_ONLINE: SEAT_RESERVED ordinal=\d+ seat=' + [regex]::Escape($reconnectSeat)),
                ('GUARDS_ONLINE: SEAT_RECLAIMED ordinal=\d+ seat=' + [regex]::Escape($reconnectSeat) + ' epoch=2'),
                'GUARDS_ONLINE: REHEARSAL_OUTCOME version=', 'GUARDS_ONLINE: EXIT 0')) {
            if ($rejoinHostText -notmatch $marker) {
                Write-Output "FAIL: the rejoin host log is missing '$marker'."
                $rehearsalFailed = $true
            }
        }
        # The seat was HELD, not given up: a crash reserves and only a deliberate leave forfeits.
        if ($rejoinHostText -match ('GUARDS_ONLINE: SEAT_RELEASED ordinal=\d+ seat=' + [regex]::Escape($reconnectSeat))) {
            Write-Output "FAIL: the rejoin cohort's cut released seat $reconnectSeat instead of reserving it; a crash must never forfeit."
            $rehearsalFailed = $true
        }
        # The two halves came from two different processes. Same log for both would mean the
        # process never died and the cohort silently degraded into the match cohort.
        $leaverText = Get-LogText -LogPath $leaverLog
        $returnText = Get-LogText -LogPath $returnLog
        if ($leaverText -notmatch ('REHEARSAL_CLIENT event=rejoin-process-exiting seat=' + [regex]::Escape($reconnectSeat))) {
            Write-Output "FAIL: the cut process never announced its exit. See $leaverLog"
            $rehearsalFailed = $true
        }
        if ($returnText -match 'REHEARSAL_CLIENT event=rejoin-process-exiting') {
            Write-Output "FAIL: the relaunched process cut itself; the return must be a return. See $returnLog"
            $rehearsalFailed = $true
        }
        foreach ($required in @(('REHEARSAL_CLIENT event=rejoin-return-pass seat=' + [regex]::Escape($reconnectSeat)),
                'REHEARSAL_CLIENT event=hud', 'GUARDS_ONLINE: REHEARSAL_OUTCOME seat=', 'GUARDS_ONLINE: EXIT 0')) {
            if ($returnText -notmatch $required) {
                Write-Output "FAIL: the relaunched $reconnectSeat process is missing '$required'. See $returnLog"
                $rehearsalFailed = $true
            }
        }
        # It came back by RECLAIM and not by an ordinary claim - the distinction the whole
        # corrective is about, and the one a refused rejoin would fail on.
        $rejoinClaims = [regex]::Matches($rejoinHostText,
            ('SEAT_CLAIMED ordinal=\d+ seat=' + [regex]::Escape($reconnectSeat)))
        if ($rejoinClaims.Count -ne 1) {
            Write-Output "FAIL: expected exactly one first claim of $reconnectSeat in the rejoin cohort; saw $($rejoinClaims.Count). A second claim would mean it returned as a stranger rather than as itself."
            $rehearsalFailed = $true
        }
        if ($rejoinHostText -match 'REJECTED ordinal=\d+ reason=SeatReserved') {
            Write-Output 'FAIL: the rejoin cohort was refused SeatReserved - this is the reported defect, unfixed.'
            $rehearsalFailed = $true
        }
        foreach ($player in $rejoinClients) {
            if ($player.Process.ExitCode -ne 0) {
                Write-Output "FAIL: rejoin seat $($player.Seat) exited $($player.Process.ExitCode)."
                $rehearsalFailed = $true
            }
        }

        $allLogs = @($matchLogs) + @($lossLogs) + @($rejoinLogs)
    }
    catch {
        Write-Output "FAIL: $($_.Exception.Message)"
        $rehearsalFailed = $true
        $allLogs = @($matchHostLog, $lossHostLog, $rejoinHostLog) +
            @($matchClients | ForEach-Object { $_.Log }) + @($lossClients | ForEach-Object { $_.Log }) +
            @($rejoinClients | ForEach-Object { $_.Log })
    }
    finally {
        foreach ($process in $processes) {
            try { if (-not $process.HasExited) { $process.Kill() } } catch { }
        }
    }
}
else {
    # ------------------------------------------------------------------ Solo cohort (THIRTEENTH amendment)
    # One scripted "human" seat plus the host's own FILL WITH TEST DRIVERS control taking the other
    # five - the regression a solo practice session actually needs: proof that a real host process,
    # pressing its own real lobby control, spawns five real keyed clients that play a complete match
    # to one converged outcome, and that every one of them - the five bots included - is gone again
    # once the host's own session ends.
    Write-Output '== Step 2: Solo cohort - one scripted seat, five test-driver seats, one host =='
    $soloSeat = 'red-player-01'
    $soloHostLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-solo-host.log')
    $soloClientLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-solo-client.log')

    try {
        Build-DisposablePlayer -UnityPath $unityPath

        # 8.35t corrective, "packaged interactive-lobby path". The two ".bat" launchers a real
        # invited tester actually double-clicks (Tools/PlaytestLaunchers/*.bat) pass nothing but
        # -guardsPrivateLobby and a log path - no rehearsal-role flags, no address/port/key
        # environment, so MatchSceneCompositionRoot never attaches a PrivateOnlineRehearsalDriver
        # and nothing can press this session's controls headlessly the way a human would. This
        # proves only what a boot-time regression on the literal shipped command line CAN prove
        # without a real mouse: that exact flag shape reaches the real interactive lobby cleanly,
        # with no crash and no degradation pattern, before the scripted cohort below exercises the
        # rest through its rehearsal-flag'd equivalent of that same lobby.
        Write-Output '== Step 1b: the packaged interactive-lobby command line boots cleanly =='
        $packagedLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-solo-packaged-boot.log')
        if (Test-Path -LiteralPath $packagedLog) { Remove-Item -LiteralPath $packagedLog -Force }
        $packagedInfo = New-Object System.Diagnostics.ProcessStartInfo
        $packagedInfo.FileName = $executablePath
        $packagedInfo.WorkingDirectory = [System.IO.Path]::GetDirectoryName($executablePath)
        $packagedInfo.UseShellExecute = $false
        $packagedInfo.CreateNoWindow = $true
        # -batchmode -nographics are this rehearsal box's own headless requirement, absent from the
        # packaged .bat text itself (written for a tester's own monitored machine) - every other
        # topology in this runner carries the same two flags for the same reason.
        $packagedInfo.Arguments = '-batchmode -nographics -guardsPrivateLobby -logFile "' + $packagedLog + '"'
        $packagedProcess = [System.Diagnostics.Process]::Start($packagedInfo)
        $processes += $packagedProcess
        try {
            $packagedDeadline = (Get-Date).AddSeconds(60)
            $packagedSeen = $false
            while ((Get-Date) -lt $packagedDeadline) {
                if ((Get-LogText -LogPath $packagedLog) -match 'GUARDS_ONLINE: LOBBY_BEGIN') { $packagedSeen = $true; break }
                if ($packagedProcess.HasExited) { break }
                Start-Sleep -Milliseconds 250
            }
            if (-not $packagedSeen) {
                Write-Output "FAIL: the packaged interactive-lobby command line never reached LOBBY_BEGIN. See $packagedLog"
                $rehearsalFailed = $true
            }
            else {
                Write-Output ('Confirmed: the packaged -guardsPrivateLobby command line reaches the real ' +
                    'interactive lobby, exactly as both shipped launchers do.')
            }
        }
        finally {
            try { if (-not $packagedProcess.HasExited) { $packagedProcess.Kill() } } catch { }
        }
        $allLogs += $packagedLog

        # 8.35t corrective, "failed-join feedback case". Before this fix, a scripted seat dialing an
        # address nothing was listening on - exactly what the interim-4 live defect did to every one
        # of its spawned bots - pressed Join, got no exception, and sat forever: Press(Join, ...)
        # returns true the instant the control was OFFERED, saying nothing about whether the dial it
        # triggered actually reached anyone. This reproduces that exact shape directly - no host is
        # ever started on this port - and requires the opposite of Wait-ForLogMarker's own default
        # behaviour: a GUARDS_ONLINE: FAIL line here is the PROOF the fix works, not a reason to
        # abort the run, so it is read with its own bounded loop rather than the shared helpers
        # every other wait in this script uses.
        Write-Output '== Step 1c: a join with nothing listening now fails fast, visibly =='
        # 8.35t corrective, first draft's own bug, caught by this exact run: Start-RehearsalPlayer
        # drives PrivateOnlineRehearsalDriver (-guardsRehearsalRole), the CI-only choreography class
        # - a DIFFERENT component from PrivateSeatDriver (-guardsTestDriver), the one
        # FillEmptySeatsWithTestDrivers actually spawns and the one this milestone's fix lives on.
        # The rehearsal driver has its own generic rehearsal-timeout failure, unrelated to
        # LastJoinFailure, so it could never show test-driver-join-failed - confirmed the hard way,
        # not assumed. Launched directly here, matching RealTestDriverProcessLauncher's own exact
        # argument/environment shape, so this really is the code path FILL WITH TEST DRIVERS uses.
        $failPort = Get-FreeLoopbackPort
        $failLog = [System.IO.Path]::Combine($tmpRoot, 'private-online-solo-failed-join.log')
        if (Test-Path -LiteralPath $failLog) { Remove-Item -LiteralPath $failLog -Force }
        $failInfo = New-Object System.Diagnostics.ProcessStartInfo
        $failInfo.FileName = $executablePath
        $failInfo.WorkingDirectory = [System.IO.Path]::GetDirectoryName($executablePath)
        $failInfo.UseShellExecute = $false
        $failInfo.CreateNoWindow = $true
        $failInfo.Arguments = (
            '-guardsPrivateLobby -guardsTestDriver -guardsTestDriverSeat red-player-01 ' +
            '-batchmode -nographics -logFile "' + $failLog + '"')
        $failInfo.EnvironmentVariables['GUARDS_TESTDRIVER_ADDRESS'] = '127.0.0.1'
        $failInfo.EnvironmentVariables['GUARDS_TESTDRIVER_PORT'] = $failPort.ToString([System.Globalization.CultureInfo]::InvariantCulture)
        $failInfo.EnvironmentVariables['GUARDS_TESTDRIVER_SESSION_KEY'] = [System.Guid]::NewGuid().ToString('N')
        $failProcess = [System.Diagnostics.Process]::Start($failInfo)
        $processes += $failProcess
        $failDeadline = (Get-Date).AddSeconds(60)
        $failSeen = $false
        while ((Get-Date) -lt $failDeadline) {
            if ((Get-LogText -LogPath $failLog) -match 'GUARDS_ONLINE: FAIL test-driver-join-failed') { $failSeen = $true; break }
            if ($failProcess.HasExited) { break }
            Start-Sleep -Milliseconds 250
        }
        if (-not $failSeen) {
            Write-Output "FAIL: a join with nothing listening never reported test-driver-join-failed. See $failLog"
            $rehearsalFailed = $true
        }
        elseif (-not $failProcess.WaitForExit(15000)) {
            Write-Output "FAIL: the failed-join client reported the failure but did not exit on its own. See $failLog"
            $rehearsalFailed = $true
            try { $failProcess.Kill() } catch { }
        }
        elseif ($failProcess.ExitCode -ne 1) {
            Write-Output "FAIL: the failed-join client exited $($failProcess.ExitCode); expected 1 (Fail's own Quit code)."
            $rehearsalFailed = $true
        }
        else {
            Write-Output ('Confirmed: a join with nothing listening now fails fast and visibly, exiting 1, ' +
                'instead of the old silent hang.')
        }
        $allLogs += $failLog

        $soloPort = Get-FreeLoopbackPort
        $soloKey = [System.Guid]::NewGuid().ToString('N')
        $rehearsalKeys += $soloKey
        Write-Output "Loopback endpoint: 127.0.0.1:$soloPort"

        # The one inner-loop knob this cohort exists to prove works: the host reads this from its
        # OWN environment and hands each spawned child the equivalent -guardsTestDriverThinkSeconds,
        # so a regression finishes in a bounded window rather than at a human's watching pace. A real
        # solo session never sets this and its bots keep the slow, watchable default.
        $env:GUARDS_TESTDRIVER_THINK_SECONDS = '0.05'
        try {
            $soloBudget = [Math]::Max(300, $TimeoutSeconds - 120)
            $soloHost = Start-RehearsalPlayer -Name 'solo-host' -Role 'host' -Mode 'solo' `
                -Address '127.0.0.1' -Port $soloPort -Key $soloKey -BudgetSeconds $soloBudget -LogPath $soloHostLog
        }
        finally {
            Remove-Item Env:\GUARDS_TESTDRIVER_THINK_SECONDS -ErrorAction SilentlyContinue
        }
        $processes += $soloHost.Process
        Wait-ForLogMarker -LogPath $soloHostLog -Pattern 'GUARDS_ONLINE: LOBBY_HOSTING port=' `
            -DeadlineSeconds 180 -Description 'the solo host to bind its lobby'

        # The scripted seat joins BEFORE the fill, not after - real solo play's own order, and the
        # host's Solo-mode logic now depends on it: a seatless host cannot name which seat is "the
        # human's" to protect, so it waits for some seat's own claim before pressing FILL, and that
        # first claim must be this one or FILL would take the scripted seat too and its own join
        # would be refused SessionFull (measured, not assumed - the first version of this cohort
        # pressed FILL immediately and reproduced exactly that).
        $soloClient = Start-RehearsalPlayer -Name $soloSeat -Role 'client' -Mode 'solo' -Seat $soloSeat `
            -Address '127.0.0.1' -Port $soloPort -Key $soloKey -BudgetSeconds $soloBudget -LogPath $soloClientLog
        $processes += $soloClient.Process
        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $soloHostLog; Pattern = ('SEAT_CLAIMED ordinal=\d+ seat=' + [regex]::Escape($soloSeat)) }
            ) -WatchLogs @($soloHostLog, $soloClientLog) -DeadlineSeconds 120 `
            -Description 'the scripted seat to claim its own seat before the host fills the rest'

        Wait-ForLogMarker -LogPath $soloHostLog -Pattern 'GUARDS_ONLINE: REHEARSAL_SOLO_FILLED' `
            -DeadlineSeconds 60 -Description 'the host to press its own FILL WITH TEST DRIVERS control'
        Write-Output 'Host pressed FILL WITH TEST DRIVERS through its real lobby control.'

        # 8.35t corrective, "must not stack": immediately after the first fill, the host presses
        # the exact same control again, unattended, while every seat it just sent a driver to is
        # still Open (a fresh child has not had time to dial in yet) - precisely the condition a
        # real player mashing the button, out of the frustration the live defect caused, would
        # recreate. The spawn-count assertion below is what actually proves the dedupe fix; this
        # wait only confirms the second press was attempted at all.
        Wait-ForLogMarker -LogPath $soloHostLog -Pattern 'GUARDS_ONLINE: REHEARSAL_SOLO_REFILL_ATTEMPTED' `
            -DeadlineSeconds 60 -Description 'the host to press FILL WITH TEST DRIVERS a second time'
        Write-Output 'Host pressed FILL WITH TEST DRIVERS again immediately - proving dedupe next.'

        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $soloHostLog; Pattern = 'GUARDS_ONLINE: MATCH_STARTED' }
            ) -WatchLogs @($soloHostLog, $soloClientLog) -DeadlineSeconds 300 `
            -Description 'the solo cohort match to begin - the scripted seat plus five spawned test drivers'
        Write-Output 'Match started: the one scripted seat plus five spawned test drivers claimed the rest.'

        Wait-ForCohortMarkers -Requirements @(
                [pscustomobject]@{ Log = $soloHostLog; Pattern = 'GUARDS_ONLINE: REHEARSAL_OUTCOME version=' },
                [pscustomobject]@{ Log = $soloClientLog; Pattern = 'GUARDS_ONLINE: REHEARSAL_OUTCOME seat=' }
            ) -WatchLogs @($soloHostLog, $soloClientLog) -DeadlineSeconds ([Math]::Max(300, $TimeoutSeconds - 300)) `
            -Description 'the solo cohort to reach one authoritative outcome'

        Write-Output '== Step 3: the host and the one scripted client exit on their own =='
        Assert-PlayerExit -Players @($soloHost, $soloClient) -DeadlineSeconds 240 -Description 'solo cohort'

        Write-Output '== Step 4: read the logs; exit codes alone are never evidence =='
        $soloHostText = Get-LogText -LogPath $soloHostLog
        foreach ($marker in @('GUARDS_ONLINE: LOBBY_HOSTING port=', 'GUARDS_ONLINE: REHEARSAL_SOLO_FILLED',
                'GUARDS_ONLINE: REHEARSAL_SOLO_REFILL_ATTEMPTED',
                'GUARDS_ONLINE: MATCH_STARTED', 'GUARDS_ONLINE: REHEARSAL_OUTCOME version=',
                'GUARDS_ONLINE: MATCH_OUTCOME version=', 'GUARDS_ONLINE: EXIT 0')) {
            if ($soloHostText -notmatch $marker) {
                Write-Output "FAIL: the solo host log is missing '$marker'."
                $rehearsalFailed = $true
            }
        }

        # Six distinct seats claimed - one the scripted client's own first claim, five the host's
        # spawned children's - is the cohort's real claim: FILL WITH TEST DRIVERS actually filled.
        $soloClaimed = [regex]::Matches($soloHostText, 'SEAT_CLAIMED ordinal=\d+ seat=([a-z0-9-]+)') |
            ForEach-Object { $_.Groups[1].Value }
        $soloDistinct = @($soloClaimed | Sort-Object -Unique)
        if (@($soloClaimed).Count -ne 6 -or $soloDistinct.Count -ne 6) {
            Write-Output "FAIL: expected exactly six distinct SEAT_CLAIMED lines; saw $(@($soloClaimed).Count) claims over $($soloDistinct.Count) seats."
            $rehearsalFailed = $true
        }
        $soloSpawnedSeats = @($soloDistinct | Where-Object { $_ -ne $soloSeat })
        if ($soloSpawnedSeats.Count -ne 5) {
            Write-Output "FAIL: expected the five non-scripted seats to be the spawned test drivers; saw $($soloSpawnedSeats.Count)."
            $rehearsalFailed = $true
        }

        # 8.35t corrective, "must not stack": the host pressed FILL twice above, back to back, while
        # every one of the five seats was still Open for the second press. Five distinct
        # LOBBY_TEST_DRIVER_SPAWNED lines - not ten - is the actual proof the dedupe fix works; ten
        # would mean the second press spawned a redundant child onto every seat already carrying an
        # outstanding attempt.
        $soloSpawnMarkers = @([regex]::Matches($soloHostText, 'GUARDS_ONLINE: LOBBY_TEST_DRIVER_SPAWNED seat='))
        if ($soloSpawnMarkers.Count -ne 5) {
            Write-Output "FAIL: expected exactly 5 LOBBY_TEST_DRIVER_SPAWNED lines (one press's worth, the second press deduped away); saw $($soloSpawnMarkers.Count)."
            $rehearsalFailed = $true
        }

        $soloClientText = Get-LogText -LogPath $soloClientLog
        if ($soloClientText -notmatch 'GUARDS_ONLINE: EXIT 0') {
            Write-Output "FAIL: the scripted solo seat did not record an orderly exit."
            $rehearsalFailed = $true
        }

        # The host's own children, and the one thing this cohort cannot read from a log: whether it
        # actually killed them. Every GuardsDigitalSmoke.exe this whole topology could have spawned
        # must be gone once the host and the scripted client have both exited on their own above -
        # a leftover means a spawned driver survived the session that owned it.
        Start-Sleep -Milliseconds 500
        $leftover = Get-Process -Name 'GuardsDigitalSmoke' -ErrorAction SilentlyContinue
        if ($leftover) {
            Write-Output ("FAIL: " + @($leftover).Count + " GuardsDigitalSmoke.exe process(es) survived the " +
                "solo cohort's own host and scripted client - the host's own spawned test drivers must be " +
                "killed when its session ends, and were not.")
            $rehearsalFailed = $true
            foreach ($stray in $leftover) {
                try { $stray.Kill() } catch { }
            }
        }
        else {
            Write-Output 'Confirmed: no test-driver process outlived the host session that spawned it.'
        }

        # 8.35t corrective. Appended, not assigned - $allLogs already carries the Step 1b/1c logs
        # from above, and an assignment here would silently drop both from the privacy review below.
        $allLogs += @($soloHostLog, $soloClientLog)
    }
    catch {
        Write-Output "FAIL: $($_.Exception.Message)"
        $rehearsalFailed = $true
        $allLogs += @($soloHostLog, $soloClientLog)
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
    # Milestone 8.35k. The reconnect capability is owner-private, classified alongside the hand,
    # and persisting it on the returning player's machine must not have loosened that. The runner
    # cannot grep for the value - it is host-minted at runtime and deliberately never told to
    # anyone but the one seat - so it greps for the SHAPE a leak would take: every marker in this
    # system is name=value, so a capability that reached a log would arrive as one.
    foreach ($shape in @('capability=', 'reconnectCapability')) {
        if ($output.IndexOf($shape, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Write-Output "FAIL: $log names the reconnect capability ('$shape'); it is owner-private."
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
elseif ($Topology -eq 'MultiClientSmoke') {
    Write-Output ('SMOKE PASSED: one seatless host and TWO real joiners walked the shipped lobby flow - ' +
        'seatless join, TAKE SEAT, READY, FILL WITH BOTS, and p.22 hero picks - and reached the first ' +
        'commitment window on the line-up they chose. Every seat was offered its own decisions and no ' +
        "other seat's, each joiner fields the hero it picked, nothing was accepted before the " +
        'commitment window on any process including the host, and every client framed the board ' +
        'identically.')
    Write-Output ('This is a reachability gate, not a resilience one: no match was played and no fault, ' +
        'reconnect or host loss was exercised. Those belong to -Topology SixClient -FaultPlan Full.')
}
elseif ($Topology -eq 'SixClient') {
    Write-Output ('REHEARSAL PASSED: one seatless host and six seated clients completed a full match with the ' +
        'forced Initiative and Minion Battle agreement flows, a mid-match reconnect, converged outcome, ' +
        'privacy-reviewed logs, and a terminal host-loss cohort.')
    Write-Output 'A same-machine rehearsal proves no relay, NAT traversal, external latency, multi-network, or real-internet behaviour.'
}
else {
    Write-Output ('REHEARSAL PASSED: the packaged interactive-lobby command line boots cleanly, a join with ' +
        'nothing listening now fails fast and visibly instead of hanging, one host pressed its own real FILL ' +
        'WITH TEST DRIVERS control twice back to back and spawned exactly five test-driver clients (not ten) ' +
        'that seated alongside one scripted seat and completed a full match to one converged outcome, and ' +
        'every spawned driver was gone again once the host session ended.')
}
exit 0
