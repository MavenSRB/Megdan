[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'UnityValidationEvidence.ps1')
. (Join-Path $PSScriptRoot 'UnityValidationPreflight.ps1')

$requestContractPath = Join-Path $PSScriptRoot 'UnityValidationRequest.ps1'
if (Test-Path -LiteralPath $requestContractPath -PathType Leaf) {
    . $requestContractPath
}

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

Write-Output 'Test-UnityValidationLauncher::CompositeFocusedFilter_UsesOneUnityProcessAndPreservesEveryRequestedFixture'

$compositeFixtures = @(
    'GuardsDigital.Tests.EditMode.MatchSessionTests',
    'GuardsDigital.Tests.EditMode.DirectorCommandReplayTests',
    'GuardsDigital.Tests.EditMode.AuthoritativeSnapshotSerializerTests'
)
$compositeLaunches = 0
$compositeArguments = $null
$compositeNames = @()
$compositeTotal = 0
$compositeError = $null

try {
    if (-not (Get-Command -Name Resolve-UnityValidationFilterExpression -ErrorAction SilentlyContinue)) {
        throw 'The validation request contract does not support an ordered filters array.'
    }

    $compositeRequest = @{ filters = $compositeFixtures } | ConvertTo-Json | ConvertFrom-Json
    $compositeFilter = Resolve-UnityValidationFilterExpression -Request $compositeRequest
    $compositeResults = Join-Path $fixtureRoot 'composite-focused-filter-results.xml'
    $compositeLog = Join-Path $fixtureRoot 'composite-focused-filter.log'
    $compositeUnityArguments = @('-runTests', '-testFilter', $compositeFilter)

    Invoke-UnityValidationProcessAndRequireEvidence `
        -UnityPath 'fake-unity' `
        -UnityArguments $compositeUnityArguments `
        -ResultsPath $compositeResults `
        -LogPath $compositeLog `
        -ProcessInvoker {
            param($unity, $arguments, $results, $log)
            $script:compositeLaunches++
            $script:compositeArguments = @($arguments)
            $cases = $compositeFixtures | ForEach-Object {
                '<test-case fullname="{0}.SelectedCase" result="Passed" />' -f $_
            }
            $xml = '<test-run result="Passed" total="3" passed="3" failed="0" skipped="0" inconclusive="0">{0}</test-run>' -f ($cases -join '')
            Write-FixtureArtifacts -ResultsPath $results -LogPath $log -Xml $xml
            [pscustomobject]@{ ExitCode = 0 }
        } | Out-Null

    [xml] $compositeDocument = Get-Content -LiteralPath $compositeResults -Raw
    $compositeTotal = [int] $compositeDocument.DocumentElement.GetAttribute('total')
    $compositeNames = @($compositeDocument.SelectNodes('//test-case') | ForEach-Object { $_.GetAttribute('fullname') })
}
catch {
    $compositeError = $_.Exception.Message
}

$compositeArgumentCount = if ($null -eq $compositeArguments) { 0 } else { @($compositeArguments).Count }
$testFilterIndexes = @(for ($index = 0; $index -lt $compositeArgumentCount; $index++) {
    if ($compositeArguments[$index] -eq '-testFilter') { $index }
})
$hasOneOrderedFilter = $false
if ($testFilterIndexes.Count -eq 1 -and $testFilterIndexes[0] + 1 -lt $compositeArgumentCount) {
    $hasOneOrderedFilter = $compositeArguments[$testFilterIndexes[0] + 1] -ceq ($compositeFixtures -join ';')
}
$hasEveryFixtureCase = $compositeTotal -eq $compositeFixtures.Count -and
    $compositeNames.Count -eq $compositeFixtures.Count -and
    -not @($compositeFixtures | Where-Object {
        $fixture = $_
        -not ($compositeNames | Where-Object { $_ -ceq "$fixture.SelectedCase" })
    }).Count

Assert-ContractCase `
    -Name 'CompositeFocusedFilter_UsesOneUnityProcessAndPreservesEveryRequestedFixture' `
    -Condition ($compositeLaunches -eq 1 -and $hasOneOrderedFilter -and $hasEveryFixtureCase) `
    -Reason $(if ($compositeError) { $compositeError } else { 'composite launch, ordered filter, or NUnit case evidence was incorrect' })

Write-Output 'Test-UnityValidationLauncher::RejectsMalformedCompositeFilterRequestsBeforeLaunch'

function Assert-RequestContractRejection {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [psobject] $Request,
        [Parameter(Mandatory = $true)] [string] $ExpectedReason
    )

    $reason = $null
    try {
        Resolve-UnityValidationFilterExpression -Request $Request | Out-Null
    }
    catch {
        $reason = $_.Exception.Message
    }

    Assert-ContractCase `
        -Name $Name `
        -Condition (-not [string]::IsNullOrWhiteSpace($reason) -and $reason -match $ExpectedReason) `
        -Reason $(if ($reason) { "unexpected rejection: $reason" } else { 'malformed request was accepted' })
}

Assert-RequestContractRejection `
    -Name 'mixed-filter-and-filters' `
    -Request ([pscustomobject]@{ filter = 'FixtureA'; filters = @('FixtureB') }) `
    -ExpectedReason "both 'filter' and 'filters'"
Assert-RequestContractRejection `
    -Name 'filters-must-be-an-array' `
    -Request ([pscustomobject]@{ filters = 'FixtureA' }) `
    -ExpectedReason 'JSON array'
Assert-RequestContractRejection `
    -Name 'filters-array-cannot-be-empty' `
    -Request ([pscustomobject]@{ filters = @() }) `
    -ExpectedReason 'at least one'
Assert-RequestContractRejection `
    -Name 'filters-entry-cannot-be-empty' `
    -Request ([pscustomobject]@{ filters = @('FixtureA', ' ') }) `
    -ExpectedReason 'nonempty'
Assert-RequestContractRejection `
    -Name 'filters-entry-cannot-be-non-string' `
    -Request ([pscustomobject]@{ filters = @('FixtureA', 42) }) `
    -ExpectedReason 'must be a string'
Assert-RequestContractRejection `
    -Name 'filters-entry-cannot-have-surrounding-whitespace' `
    -Request ([pscustomobject]@{ filters = @(' FixtureA') }) `
    -ExpectedReason 'leading or trailing whitespace'
Assert-RequestContractRejection `
    -Name 'filters-entry-cannot-contain-semicolon' `
    -Request ([pscustomobject]@{ filters = @('FixtureA;FixtureB') }) `
    -ExpectedReason 'semicolon'
Assert-RequestContractRejection `
    -Name 'filters-entries-must-be-distinct' `
    -Request ([pscustomobject]@{ filters = @('FixtureA', 'FixtureA') }) `
    -ExpectedReason 'distinct'

$unfilteredExpression = Resolve-UnityValidationFilterExpression -Request ([pscustomobject]@{ filter = '' })
$legacyCompositeExpression = Resolve-UnityValidationFilterExpression -Request ([pscustomobject]@{ filter = 'FixtureA;FixtureB' })
Assert-ContractCase `
    -Name 'scalar-filter-remains-compatible-for-unfiltered-and-legacy-requests' `
    -Condition ($unfilteredExpression -ceq '' -and $legacyCompositeExpression -ceq 'FixtureA;FixtureB') `
    -Reason 'the existing scalar filter contract changed'

Write-Output "Contract cases: passed=$script:passed failed=$script:failed"
if ($script:failed -ne 0) {
    throw "$script:failed Unity validation launcher contract case(s) failed."
}
