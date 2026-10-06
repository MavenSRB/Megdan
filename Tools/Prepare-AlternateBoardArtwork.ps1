param([switch]$Check)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $root 'Docs/Guards of Atlantis II - Map References/alternate-2v2-3v3-ranged-spawns-2026-10-05.png'
$target = Join-Path $root 'Assets/Resources/Private/OfficialBoard/alternate-small-single-lane-board.png'
if (-not (Test-Path -LiteralPath $source)) { throw 'The approved private alternate-map artwork is missing.' }
$same = (Test-Path -LiteralPath $target) -and ((Get-FileHash -LiteralPath $source).Hash -eq (Get-FileHash -LiteralPath $target).Hash)
if ($Check -and -not $same) { throw 'Alternate-map artwork is missing or differs from the approved private source.' }
if (-not $Check -and -not $same) {
    New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
}
Write-Output 'PASS: alternate-map artwork is byte-identical to its approved private source.'
