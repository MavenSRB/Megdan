<#
.SYNOPSIS
    Regenerates the runtime board-map resource from its canonical reviewed specification.

.DESCRIPTION
    The board map has one source of truth: Docs/AI/Maps/official-single-lane.map.json, the reviewed
    specification that Validate-BoardMapSpecification.ps1 checks. Unity cannot load that path, so a
    byte-identical copy lives at Assets/Resources/Maps/official-single-lane.json, where
    Resources.Load<TextAsset>("Maps/...") can reach it. This script writes the copy from the source.

    Before milestone 6.1d the two files were maintained independently and nothing compared them, so
    a divergence would have been silent: the validator would keep passing against the specification
    while the game loaded something else.

    The .meta file is deliberately never written. Assets/Resources/Maps/official-single-lane.json.meta
    carries the asset GUID Unity resolves the resource by; regenerating it would break the reference.

.PARAMETER Check
    Compare only. Exits non-zero when the copy has drifted, and writes nothing.
#>
[CmdletBinding()]
param(
    [switch] $Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach ($mapName in @(
    'official-single-lane',
    'alternate-small-single-lane',
    'green-water-double-lane',
    'lava-bridge-double-lane')) {
$specificationPath = [System.IO.Path]::Combine($projectRoot, 'Docs', 'AI', 'Maps', ($mapName + '.map.json'))
$resourcePath = [System.IO.Path]::Combine($projectRoot, 'Assets', 'Resources', 'Maps', ($mapName + '.json'))

if (-not (Test-Path -LiteralPath $specificationPath -PathType Leaf)) {
    throw "Canonical board-map specification is missing: $specificationPath"
}

$specificationBytes = [System.IO.File]::ReadAllBytes($specificationPath)

$resourceExists = Test-Path -LiteralPath $resourcePath -PathType Leaf
$resourceBytes = if ($resourceExists) { [System.IO.File]::ReadAllBytes($resourcePath) } else { [byte[]]@() }

$identical = $resourceExists -and
    $resourceBytes.Length -eq $specificationBytes.Length -and
    [System.Linq.Enumerable]::SequenceEqual([byte[]] $resourceBytes, [byte[]] $specificationBytes)

if ($identical) {
    Write-Output "PASS: runtime board-map resource is byte-identical to its specification ($($specificationBytes.Length) bytes)."
    continue
}

if ($Check) {
    if (-not $resourceExists) {
        Write-Error "Runtime board-map resource is missing: $resourcePath. Run Tools/Sync-BoardMapResource.ps1 to regenerate it."
        exit 1
    }

    Write-Error ("Runtime board-map resource has drifted from its specification. " +
        "Specification is $($specificationBytes.Length) bytes, resource is $($resourceBytes.Length) bytes. " +
        'Run Tools/Sync-BoardMapResource.ps1 to regenerate it from the reviewed source.')
    exit 1
}

$resourceDirectory = [System.IO.Path]::GetDirectoryName($resourcePath)
if (-not (Test-Path -LiteralPath $resourceDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $resourceDirectory -Force | Out-Null
}

[System.IO.File]::WriteAllBytes($resourcePath, $specificationBytes)
Write-Output "WROTE: regenerated the runtime board-map resource from its specification ($($specificationBytes.Length) bytes)."
Write-Output 'The .meta file was not touched; the asset GUID is unchanged.'

}
