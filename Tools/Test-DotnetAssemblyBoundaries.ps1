$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'DotnetTests/GuardsDigital.DotnetTests.csproj'

# A negative compile probe is expected to fail, and only the intended accessibility error
# counts. The final normal build restores the output so no probe enters ordinary test runs.
$probeOutput = & dotnet build $project --no-restore -p:HarnessForbiddenAccessProbe=true -v minimal 2>&1
$probeExit = $LASTEXITCODE
if ($probeExit -eq 0 -or ($probeOutput -join "`n") -notmatch 'ForbiddenCoreAccess.cs.*error CS0122') {
    $probeOutput | Write-Output
    throw 'ForbiddenCoreAccess must fail with CS0122 across the real Core boundary.'
}
Write-Output 'PASS: ForbiddenCoreAccess rejected with CS0122.'
& dotnet test $project --no-restore --filter 'FullyQualifiedName~GuardsDigital.Tests.Harness' -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Allowed assembly boundaries and harness contract checks failed.' }
