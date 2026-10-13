param([switch]$Check)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$items = @(
    @{
        Source = 'Docs/Guards of Atlantis II - Map References/two-lane-4v4-5v5-melee-spawns-2026-10-05.png'
        Target = 'Assets/Resources/Private/OfficialBoard/green-water-double-lane-board.png'
    },
    @{
        Source = 'Docs/Guards of Atlantis II - Map References/two-lane-4v4-5v5-lava-bridge-melee-spawns-2026-10-05.png'
        Target = 'Assets/Resources/Private/OfficialBoard/lava-bridge-double-lane-board.png'
    }
)
foreach ($item in $items) {
    $source = Join-Path $root $item.Source
    $target = Join-Path $root $item.Target
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Approved private double-lane artwork is missing: $source"
    }
    $same = (Test-Path -LiteralPath $target -PathType Leaf) -and
        ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $target).Hash)
    if ($Check -and -not $same) {
        throw "Private runtime artwork is missing or differs from its approved source: $target"
    }
    if (-not $Check -and -not $same) {
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        Copy-Item -LiteralPath $source -Destination $target
    }
    Write-Output "PASS: $($item.Target) is byte-identical to its approved private source."
}
