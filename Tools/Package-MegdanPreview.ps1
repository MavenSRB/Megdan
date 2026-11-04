param(
    [ValidatePattern('^W[0-9]+$')][string] $WorkId = 'W174',
    [string] $BuildStamp = '1.0-W174-megdan-six-heroes+20261104-ee3a8f09'
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $root 'Builds/Win64Smoke'
$destination = Join-Path $root "Builds/${WorkId}MegdanPackage/GuardsDigital"
$archive = Join-Path $root "Builds/Playtest-${WorkId}-megdan-six-heroes.zip"
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath $archive)) { throw 'Package already exists; preserve it.' }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Get-ChildItem -LiteralPath $source | Where-Object { $_.Name -notmatch 'DoNotShip|BackUpThisFolder|BurstDebug|^Logs$' } | Copy-Item -Destination $destination -Recurse
Rename-Item -LiteralPath (Join-Path $destination 'GuardsDigitalSmoke.exe') -NewName 'GuardsDigital.exe'
Rename-Item -LiteralPath (Join-Path $destination 'GuardsDigitalSmoke_Data') -NewName 'GuardsDigital_Data'
$readme = @'
MEGDAN - PRIVATE WINDOWS PREVIEW W174

Run GuardsDigital.exe. Keep all support files beside it.
Host a game, choose a map and 2v2 or 3v3, take a seat and fill the remaining
seats with bots for a local preview. PvE supports one player controlling three heroes.

Selectable heroes: Brogan, Riki (Misa), Rowenna, Tigerclaw, Gydion and Tali.
The full 32-hero rules/content registration remains preserved internally.
4v4/5v5 require more unique heroes than this preview offers; use 2v2 or 3v3.
Includes the current Megdan cards, standees, camera and grass board presentation.
Replacement artwork is a private presentation study; inherited gameplay remains.

All players must use this same build and PveData. TCP port 45677 by default.
Private local preview; no public distribution is authorized.
Stamp: 1.0-W174-megdan-six-heroes+20261104-ee3a8f09
'@
[IO.File]::WriteAllText((Join-Path $destination 'README.txt'), $readme.Replace(
    '1.0-W174-megdan-six-heroes+20261104-ee3a8f09', $BuildStamp).Replace('PREVIEW W174', "PREVIEW $WorkId"))
$manifest = @(Get-ChildItem -LiteralPath $destination -Recurse -File | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{Path=$_.FullName.Substring($destination.Length+1);Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
})
$manifest | Export-Csv -LiteralPath (Join-Path $destination 'FILES.csv') -NoTypeInformation
Compress-Archive -LiteralPath $destination -DestinationPath $archive -CompressionLevel Optimal
$extracted = Join-Path $root ("tmp/" + $WorkId.ToLowerInvariant() + '-extracted')
if (Test-Path -LiteralPath $extracted) { throw 'Extraction evidence exists.' }
Expand-Archive -LiteralPath $archive -DestinationPath $extracted
foreach ($entry in $manifest) {
    $file = Join-Path (Join-Path $extracted 'GuardsDigital') $entry.Path
    if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Archive mismatch: $file" }
}
Get-FileHash -LiteralPath $archive -Algorithm SHA256
Write-Output "Verified $($manifest.Count) payload files. Extracted player: $extracted/GuardsDigital/GuardsDigital.exe"
