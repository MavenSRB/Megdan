[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'UnityValidationEvidence.ps1')
. (Join-Path $PSScriptRoot 'UnityValidationPreflight.ps1')

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixtureRoot = [System.IO.Path]::Combine($projectRoot, 'tmp', 'unity-validation-launcher-contract')
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null

$script:passed = 0
$script:failed = 0

function Assert-ContractCase {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [string] $Reason = 'contract condition was false'
    )

    if ($Condition) {
        $script:passed++
        Write-Output "PASS $Name"
        return
    }

    $script:failed++
    Write-Output "FAIL $Name - $Reason"
}

function Write-FixtureArtifacts {
    param(
        [Parameter(Mandatory = $true)] [string] $ResultsPath,
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [string] $Xml,
        [string] $Log = 'Clean Unity validation log.'
    )

    [System.IO.File]::WriteAllText($ResultsPath, $Xml)
    [System.IO.File]::WriteAllText($LogPath, $Log)
}

function Invoke-ContractCase {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [scriptblock] $FakeProcess,
        [bool] $ShouldPass = $false,
        [string] $ExpectedReason,
        [scriptblock] $Arrange
    )

    $safeName = $Name -replace '[^A-Za-z0-9_-]', '-'
    $resultsPath = Join-Path $fixtureRoot ($safeName + '-results.xml')
    $logPath = Join-Path $fixtureRoot ($safeName + '.log')

    if ($Arrange) {
        & $Arrange $resultsPath $logPath
    }

    $didPass = $false
    $reason = $null
    try {
        Invoke-UnityValidationProcessAndRequireEvidence `
            -UnityPath 'fake-unity' `
            -UnityArguments @('-runTests') `
            -ResultsPath $resultsPath `
            -LogPath $logPath `
            -ProcessInvoker $FakeProcess | Out-Null
        $didPass = $true
    }
    catch {
        $reason = $_.Exception.Message
    }

    $reasonMatches = $ShouldPass -or (
        -not [string]::IsNullOrWhiteSpace($reason) -and
        (-not $ExpectedReason -or $reason -match $ExpectedReason))
    if ($didPass -eq $ShouldPass -and $reasonMatches) {
        $script:passed++
        Write-Output "PASS $Name"
        return
    }

    $script:failed++
    if ($ShouldPass) {
        Write-Output "FAIL $Name - expected success, got: $reason"
    }
    else {
        Write-Output "FAIL $Name - invalid evidence was accepted or rejected for the wrong reason: $reason"
    }
}

$greenXml = '<test-run result="Passed" total="1" passed="1" failed="0" skipped="0" inconclusive="0"><test-suite result="Passed" /></test-run>'
$writeGreen = {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml $greenXml
    [pscustomobject]@{ ExitCode = 0 }
}

Write-Output 'Test-UnityValidationLauncher::AlwaysRestartsHubAndLicensingBeforeLaunch'

$preflightCalls = [System.Collections.Generic.List[string]]::new()
$supportReady = $true
Restart-UnityValidationSupportProcesses -HubPath 'fake-hub' `
    -ProcessLookup {
        param($name)
        $preflightCalls.Add("lookup:$name")
        if ($supportReady) { return [pscustomobject]@{ Name = $name } }
    } `
    -StopSupportProcesses {
        $preflightCalls.Add('stop')
        $script:supportReady = $false
    } `
    -StartHub {
        param($path)
        $preflightCalls.Add("start:$path")
        $script:supportReady = $true
    } `
    -WaitOneSecond { $preflightCalls.Add('wait') } `
    -ReadClock { [datetime]'2026-09-01T00:00:00Z' }

Assert-ContractCase `
    -Name 'healthy-support-processes-are-stopped-and-restarted' `
    -Condition (($preflightCalls -join ',') -eq 'stop,start:fake-hub,wait,lookup:Unity Hub,lookup:Unity.Licensing.Client') `
    -Reason "unexpected preflight call order: $($preflightCalls -join ',')"

Write-Output 'Test-UnityValidationLauncher::RejectsStaleMissingMalformedAndNonGreenResults'

Invoke-ContractCase -Name 'stale-green-exit-zero-no-writes' -FakeProcess {
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'did not recreate results XML' -Arrange {
    param($results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml $greenXml -Log 'Stale clean log.'
}
Invoke-ContractCase -Name 'missing-log' -FakeProcess {
    param($unity, $arguments, $results, $log)
    [System.IO.File]::WriteAllText($results, $greenXml)
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'did not recreate its log'
Invoke-ContractCase -Name 'missing-xml' -FakeProcess {
    param($unity, $arguments, $results, $log)
    [System.IO.File]::WriteAllText($log, 'Clean log.')
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'did not recreate results XML'
Invoke-ContractCase -Name 'malformed-xml' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'malformed'
Invoke-ContractCase -Name 'wrong-root-deliberate-parser-violation' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-suite result="Passed" total="1" passed="1" failed="0" skipped="0" inconclusive="0" />'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason '<test-run> root'
Invoke-ContractCase -Name 'missing-count-attribute' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run result="Passed" total="1" passed="1" failed="0" skipped="0" />'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason "missing 'inconclusive'"
Invoke-ContractCase -Name 'zero-discovery' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run result="Passed" total="0" passed="0" failed="0" skipped="0" inconclusive="0" />'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'discovered zero tests'
Invoke-ContractCase -Name 'failed-descendant' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run result="Passed" total="1" passed="1" failed="0" skipped="0" inconclusive="0"><test-case result="Failed" /></test-run>'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'non-green child'
Invoke-ContractCase -Name 'error-labelled-descendant' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run result="Passed" total="1" passed="1" failed="0" skipped="0" inconclusive="0"><test-case result="Passed" label="Error" /></test-run>'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'error label'
Invoke-ContractCase -Name 'skipped-result' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run result="Passed" total="1" passed="1" failed="0" skipped="1" inconclusive="0" />'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'contradictory or non-green'
Invoke-ContractCase -Name 'inconclusive-result' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run result="Passed" total="1" passed="1" failed="0" skipped="0" inconclusive="1" />'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'contradictory or non-green'
Invoke-ContractCase -Name 'contradictory-counts' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml '<test-run result="Passed" total="2" passed="1" failed="0" skipped="0" inconclusive="0" />'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'contradictory or non-green'
Invoke-ContractCase -Name 'nonzero-process-exit' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml $greenXml
    [pscustomobject]@{ ExitCode = 3 }
} -ExpectedReason 'exited with code 3'
Invoke-ContractCase -Name 'compiler-log-signature' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml $greenXml -Log 'Assets/Test.cs(1,1): error CS1002: ; expected'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'compilation/import failure signature'
Invoke-ContractCase -Name 'import-log-signature' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml $greenXml -Log 'Asset import failed for Assets/Broken.asset'
    [pscustomobject]@{ ExitCode = 0 }
} -ExpectedReason 'compilation/import failure signature'
Invoke-ContractCase -Name 'arbitrary-exception-word-is-allowed' -FakeProcess {
    param($unity, $arguments, $results, $log)
    Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml $greenXml -Log 'Expected test output mentions exception handling.'
    [pscustomobject]@{ ExitCode = 0 }
} -ShouldPass $true
Invoke-ContractCase -Name 'fresh-green-results-and-clean-log' -FakeProcess $writeGreen -ShouldPass $true

Write-Output "Contract cases: passed=$script:passed failed=$script:failed"
if ($script:failed -ne 0) {
    throw "$script:failed Unity validation launcher contract case(s) failed."
}
