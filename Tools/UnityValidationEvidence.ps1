$ErrorActionPreference = 'Stop'

function Remove-UnityValidationPriorArtifacts {
    param(
        [Parameter(Mandatory = $true)] [string] $ResultsPath,
        [Parameter(Mandatory = $true)] [string] $LogPath
    )

    if ([string]::Equals($ResultsPath, $LogPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The results and log paths must be different files.'
    }

    foreach ($path in @($ResultsPath, $LogPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }

        if (Test-Path -LiteralPath $path) {
            throw "Could not invalidate prior Unity validation artifact: $path"
        }
    }
}

function Get-RequiredNonNegativeIntegerAttribute {
    param(
        [Parameter(Mandatory = $true)] [System.Xml.XmlElement] $Element,
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $ResultsPath
    )

    if (-not $Element.HasAttribute($Name)) {
        throw "Unity results '$ResultsPath' are missing numeric '$Name' on <test-run>."
    }

    $value = 0
    if (-not [int]::TryParse(
        $Element.GetAttribute($Name),
        [System.Globalization.NumberStyles]::None,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [ref] $value) -or $value -lt 0) {
        throw "Unity results '$ResultsPath' have invalid numeric '$Name' on <test-run>."
    }

    return $value
}

function Assert-UnityValidationResults {
    param(
        [Parameter(Mandatory = $true)] [string] $ResultsPath,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedUtc
    )

    if (-not (Test-Path -LiteralPath $ResultsPath -PathType Leaf)) {
        throw "Unity did not recreate results XML for this run: $ResultsPath"
    }

    $resultsFile = Get-Item -LiteralPath $ResultsPath
    if ($resultsFile.LastWriteTimeUtc -lt $RunStartedUtc) {
        throw "Unity results XML is not fresh for this run: $ResultsPath"
    }

    try {
        [xml] $document = Get-Content -LiteralPath $ResultsPath -Raw
    }
    catch {
        throw "Unity results XML is malformed at '$ResultsPath': $($_.Exception.Message)"
    }

    $root = $document.DocumentElement
    if ($null -eq $root -or $root.Name -ne 'test-run') {
        throw "Unity results '$ResultsPath' must have a <test-run> root."
    }

    foreach ($requiredAttribute in @('result', 'total', 'passed', 'failed', 'skipped', 'inconclusive')) {
        if (-not $root.HasAttribute($requiredAttribute)) {
            throw "Unity results '$ResultsPath' are missing '$requiredAttribute' on <test-run>."
        }
    }

    $total = Get-RequiredNonNegativeIntegerAttribute -Element $root -Name 'total' -ResultsPath $ResultsPath
    $passed = Get-RequiredNonNegativeIntegerAttribute -Element $root -Name 'passed' -ResultsPath $ResultsPath
    $failed = Get-RequiredNonNegativeIntegerAttribute -Element $root -Name 'failed' -ResultsPath $ResultsPath
    $skipped = Get-RequiredNonNegativeIntegerAttribute -Element $root -Name 'skipped' -ResultsPath $ResultsPath
    $inconclusive = Get-RequiredNonNegativeIntegerAttribute -Element $root -Name 'inconclusive' -ResultsPath $ResultsPath

    if ($root.GetAttribute('result') -ne 'Passed') {
        throw "Unity results '$ResultsPath' report result='$($root.GetAttribute('result'))', not Passed."
    }
    if ($total -le 0) {
        throw "Unity results '$ResultsPath' discovered zero tests."
    }
    # [Explicit] diagnostics (in the suite since 8.35z22) report as Skipped with label="Explicit";
    # they are the only skip a green run may carry, and every one must be accounted for by name.
    $explicitSkipNodes = @($root.SelectNodes('.//test-case[@result="Skipped"]') | Where-Object { $_.GetAttribute('label') -eq 'Explicit' })
    if (($passed + $skipped) -ne $total -or $failed -ne 0 -or $inconclusive -ne 0 -or $skipped -ne $explicitSkipNodes.Count) {
        throw "Unity results '$ResultsPath' are contradictory or non-green: total=$total passed=$passed failed=$failed skipped=$skipped (explicit-skipped=$($explicitSkipNodes.Count)) inconclusive=$inconclusive."
    }

    $nonGreen = $root.SelectNodes('.//*[@result]') | Where-Object {
        $_.GetAttribute('result') -ne 'Passed' -and
        -not ($_.GetAttribute('result') -eq 'Skipped' -and $_.GetAttribute('label') -eq 'Explicit')
    }
    if ($nonGreen) {
        $first = $nonGreen | Select-Object -First 1
        throw "Unity results '$ResultsPath' contain non-green child result='$($first.GetAttribute('result'))'."
    }

    $errorLabel = $root.SelectNodes('.//*[@label]') | Where-Object {
        $_.GetAttribute('label') -match '(?i)(^|[-_\s])error($|[-_\s])'
    }
    if ($errorLabel) {
        $first = $errorLabel | Select-Object -First 1
        throw "Unity results '$ResultsPath' contain error label='$($first.GetAttribute('label'))'."
    }
}

function Assert-UnityValidationLog {
    param(
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [Parameter(Mandatory = $true)] [datetime] $RunStartedUtc
    )

    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
        throw "Unity did not recreate its log for this run: $LogPath"
    }

    $logFile = Get-Item -LiteralPath $LogPath
    if ($logFile.LastWriteTimeUtc -lt $RunStartedUtc) {
        throw "Unity log is not fresh for this run: $LogPath"
    }
    if ($logFile.Length -eq 0) {
        throw "Unity recreated an empty log: $LogPath"
    }

    $logText = Get-Content -LiteralPath $LogPath -Raw
    $failureSignatures = @(
        '(?im)^.*\berror CS\d{4}:.*$',
        '(?im)^.*Scripts have compiler errors.*$',
        '(?im)^.*Compilation failed.*$',
        '(?im)^.*Asset import failed.*$',
        '(?im)^.*Error importing .*$',
        '(?im)^.*Failed to import .*$',
        '(?im)^.*Aborting batchmode due to failure.*$'
    )

    foreach ($signature in $failureSignatures) {
        $match = [regex]::Match($logText, $signature)
        if ($match.Success) {
            throw "Unity log '$LogPath' contains a compilation/import failure signature: $($match.Value.Trim())"
        }
    }
}

function Invoke-UnityValidationProcessAndRequireEvidence {
    param(
        [Parameter(Mandatory = $true)] [string] $UnityPath,
        [Parameter(Mandatory = $true)] [object[]] $UnityArguments,
        [Parameter(Mandatory = $true)] [string] $ResultsPath,
        [Parameter(Mandatory = $true)] [string] $LogPath,
        [scriptblock] $ProcessInvoker
    )

    Remove-UnityValidationPriorArtifacts -ResultsPath $ResultsPath -LogPath $LogPath
    $runStartedUtc = [datetime]::UtcNow

    if ($null -eq $ProcessInvoker) {
        $process = Start-Process -FilePath $UnityPath -ArgumentList $UnityArguments -Wait -PassThru -WindowStyle Hidden
    }
    else {
        # Test injection lives below the fixed parameterless production entry point. The launcher
        # never reads this seam from its request or environment and never passes an override here.
        $process = & $ProcessInvoker $UnityPath $UnityArguments $ResultsPath $LogPath
    }

    if ($null -eq $process -or $null -eq $process.ExitCode) {
        throw 'Unity validation process did not return an exit code.'
    }
    if ([int] $process.ExitCode -ne 0) {
        throw "Unity validation process exited with code $($process.ExitCode)."
    }

    Assert-UnityValidationResults -ResultsPath $ResultsPath -RunStartedUtc $runStartedUtc
    Assert-UnityValidationLog -LogPath $LogPath -RunStartedUtc $runStartedUtc
    return $process
}
