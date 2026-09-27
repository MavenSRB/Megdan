<#
.SYNOPSIS
    Prepares private dashboard-marker artwork from verified local card fronts.

.DESCRIPTION
    Snorri's four rune pieces have no standalone raster in the curated corpus. The official Rune
    Sigils front prints all four glyphs together, so this tool extracts those exact glyph pixels into
    ignored Unity resources at `Assets/Resources/Private/Markers/snorri-rune-N.png`.

    The source identity, dimensions and crop rectangles are assertions. A changed corpus fails rather
    than silently cutting neighbouring text. Each tile keeps the printed card stock behind its glyph:
    the dark ink stays legible on the dark HUD, and the card and tray use the same official pixels
    rather than a redraw.
#>
[CmdletBinding()]
param(
    [ValidateSet('snorri')]
    [string] $Hero = 'snorri',
    [switch] $Check,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = [IO.Path]::Combine($projectRoot, 'Docs',
    'Guards of Atlantis II - Tabletopia Cards', 'active-hero-cards', 'Snorri',
    'snorri__7387414__front.png')
$targetRoot = [IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private', 'Markers')
$expectedHash = 'A805FC72ADAC281E54018230F98D7A3C37F21DB0E5D4750C73905784BD3235EB'
$crops = @(
    @{ Id = 'snorri-rune-1'; X = 26; Y = 388; Width = 27; Height = 22 },
    @{ Id = 'snorri-rune-2'; X = 26; Y = 409; Width = 27; Height = 22 },
    @{ Id = 'snorri-rune-3'; X = 26; Y = 430; Width = 27; Height = 22 },
    @{ Id = 'snorri-rune-4'; X = 26; Y = 451; Width = 27; Height = 22 }
)

if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    Write-Output "The private Snorri card front is absent. Nothing was written."
    exit 0
}
$actualHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
if ($actualHash -ne $expectedHash) {
    throw "Snorri Rune Sigils source hash changed; inspect before extracting markers."
}

Add-Type -AssemblyName System.Drawing
$card = [Drawing.Bitmap]::FromFile($source)
try {
    if ($card.Width -ne 372 -or $card.Height -ne 520) {
        throw "Snorri Rune Sigils source dimensions changed."
    }
    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }
    foreach ($crop in $crops) {
        $target = [IO.Path]::Combine($targetRoot, "$($crop.Id).png")
        if ($Check -or ((Test-Path -LiteralPath $target -PathType Leaf) -and -not $Force)) {
            if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
                throw "Missing prepared marker artwork: $target"
            }
            $existing = [Drawing.Image]::FromFile($target)
            try {
                if ($existing.Width -ne $crop.Width -or $existing.Height -ne $crop.Height) {
                    throw "Prepared marker dimensions changed: $target"
                }
            } finally { $existing.Dispose() }
            continue
        }

        $bitmap = New-Object Drawing.Bitmap $crop.Width, $crop.Height,
            ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            for ($y = 0; $y -lt $crop.Height; $y++) {
                for ($x = 0; $x -lt $crop.Width; $x++) {
                    $pixel = $card.GetPixel($crop.X + $x, $crop.Y + $y)
                    $bitmap.SetPixel($x, $y, [Drawing.Color]::FromArgb(
                        255, $pixel.R, $pixel.G, $pixel.B))
                }
            }
            $bitmap.Save($target, [Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
    }
} finally { $card.Dispose() }

Write-Output "Verified $($crops.Count) source-faithful private Snorri rune glyphs."
