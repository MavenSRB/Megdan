$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$atlasPath = Join-Path $projectRoot 'Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg'
$manifestPath = Join-Path $projectRoot 'Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/manifest-bundled__42.json'
$outputRoot = Join-Path $projectRoot 'Assets/Resources/Minions'

if (-not (Test-Path -LiteralPath $atlasPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'The private minion atlas and manifest are unavailable at their documented local paths.'
}

$reviewed = @(
    [pscustomobject]@{ File = 'red-heavy.png'; ElementId = 896365; Name = 'Heavy_Steam'; X = 1217; Y = 0; Hash = '891242a0fdd5d3ad5b1a667a3e97fdd6ea6c24803d0fc4e93fbb0283ecbd5540' }
    [pscustomobject]@{ File = 'red-melee.png'; ElementId = 896366; Name = 'Melee_Steam'; X = 1747; Y = 0; Hash = '0d967a4025d86b4cc755110e47d73d91db8ca33d086a28cd1979a9df282c1274' }
    [pscustomobject]@{ File = 'red-ranged.png'; ElementId = 8630841; Name = 'Ranged_Steam_v2'; X = 3322; Y = 0; Hash = '4b77fe91b290e7685db584692d48ce1fda81e4d8e547f91a603cbb8878b4522e' }
    [pscustomobject]@{ File = 'blue-heavy.png'; ElementId = 896367; Name = 'Heavy_Crystal'; X = 2521; Y = 0; Hash = '05effadc3dcbaec3be52ebe7cf1dba0b4c2781050f675050e899e639f978e7a1' }
    # Element 896369 is the reviewed Crystal/Blue melee face despite the source metadata error.
    [pscustomobject]@{ File = 'blue-melee.png'; ElementId = 896369; Name = 'Melee_Steam'; X = 3052; Y = 0; Hash = 'b80742d1947a878729db2aee911d9934e05a829900d43ff910cff638130b2aa0' }
    [pscustomobject]@{ File = 'blue-ranged.png'; ElementId = 8630847; Name = 'Ranged_Crystal2'; X = 432; Y = 533; Hash = '8b7b039319c30ec714a9ba1f0f35d31749ae44e8116842461572f24f585de835' }
)

$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
Add-Type -AssemblyName System.Drawing
$atlas = [System.Drawing.Image]::FromFile($atlasPath)
try {
    if ($atlas.Width -ne 4096 -or $atlas.Height -ne 4096) {
        throw "Expected the reviewed atlas to be 4096x4096; found $($atlas.Width)x$($atlas.Height)."
    }

    foreach ($expected in $reviewed) {
        $entry = @($manifest | Where-Object { $_.elementId -eq $expected.ElementId })
        if ($entry.Count -ne 1 -or $entry[0].elementName -cne $expected.Name) {
            throw "Manifest identity changed for $($expected.File)."
        }

        $sprite = @($entry[0].atlases[0].sprites | Where-Object {
            $_.x -eq $expected.X -and $_.y -eq $expected.Y -and $_.width -eq 256 -and $_.height -eq 256
        })
        if ($entry[0].atlases[0].resource.id -cne 'Image_0_Atlas' -or $sprite.Count -eq 0) {
            throw "Reviewed sprite coordinates changed for $($expected.File)."
        }

        $outputPath = Join-Path $outputRoot $expected.File
        if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
            throw "Generated standard-minion art is missing: $($expected.File)."
        }

        $actualHash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actualHash -cne $expected.Hash) {
            throw "Generated standard-minion art changed: $($expected.File)."
        }

        $image = [System.Drawing.Image]::FromFile($outputPath)
        try {
            if ($image.Width -ne 256 -or $image.Height -ne 256 -or
                $image.PixelFormat -notmatch 'Alpha|Argb') {
                throw "Generated standard-minion art has the wrong dimensions or alpha format: $($expected.File)."
            }
        }
        finally {
            $image.Dispose()
        }
    }
}
finally {
    $atlas.Dispose()
}

Write-Output 'PASS: six reviewed standard-minion sprites, atlas coordinates, hashes, alpha, and Crystal melee label correction.'
