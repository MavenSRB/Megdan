<#
.SYNOPSIS
    Prepares the private official board artwork as an ignored Unity runtime resource.

.DESCRIPTION
    Milestone 8.11a. The configured match draws the official board as a flat underlay beneath the
    reviewed 292-space map. The artwork itself is private: it lives in the local Tabletopia corpus
    under Docs/, which .gitignore keeps out of the repository, and it must stay there. This script is
    the only route from that corpus to something Unity can load, and it writes exactly one file:

        Assets/Resources/Private/OfficialBoard/official-single-lane-board.png

    That path is ignored too, so a clean checkout has no artwork and the board falls back to its
    generated presentation. Nothing else is written, moved, or deleted - the source atlas, the
    extracted corpus and the map-reference directory are read-only to this script.

    The crop is not guessed. The bundled Tabletopia manifest declares, for the element named
    GuardsII_Small_Board, the atlas that holds its face and the sprite rectangle inside that atlas;
    this script reads both and refuses to write anything if the atlas it finds does not match the
    declared dimensions. The rectangle is stated from the top-left, which was confirmed against the
    pixels rather than assumed.

.PARAMETER Check
    Verify only. Reports whether the prepared resource exists and matches the declared sprite size,
    and writes nothing. Exits non-zero when the resource is missing or has the wrong dimensions.

.PARAMETER Force
    Rewrite the resource even when a correctly sized one is already present.
#>
[CmdletBinding()]
param(
    [switch] $Check,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$elementName = 'GuardsII_Small_Board'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$corpusRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
$resourcePath = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private', 'OfficialBoard', 'official-single-lane-board.png')

function Get-PngSize([string] $path) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 24) { return $null }
    $width = ($bytes[16] -shl 24) -bor ($bytes[17] -shl 16) -bor ($bytes[18] -shl 8) -bor $bytes[19]
    $height = ($bytes[20] -shl 24) -bor ($bytes[21] -shl 16) -bor ($bytes[22] -shl 8) -bor $bytes[23]
    return [pscustomobject]@{ Width = $width; Height = $height }
}

# --- The declared crop, read from the manifest rather than assumed ----------

if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
    if ($Check) {
        Write-Error "The private Tabletopia corpus is absent: $corpusRoot. The prepared artwork cannot be verified against its source."
        exit 1
    }

    throw "The private Tabletopia corpus is absent: $corpusRoot"
}

$manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
if ($manifests.Count -ne 1) {
    throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
}

$manifest = [System.IO.File]::ReadAllText($manifests[0].FullName)
$elementPattern = '"elementName":"' + [regex]::Escape($elementName) + '"(?<body>.*?)(?="elementId"|$)'
$elementMatch = [regex]::Match($manifest, $elementPattern, 'Singleline')
if (-not $elementMatch.Success) {
    throw "The manifest declares no element named '$elementName'."
}

$body = $elementMatch.Groups['body'].Value
$atlasMatch = [regex]::Match(
    $body,
    '"atlases":\[\{"height":(?<ah>\d+),"width":(?<aw>\d+),"resource":\{"id":"(?<id>[^"]+)"[^}]*\},"sprites":\[\{"x":(?<sx>\d+),"y":(?<sy>\d+),"height":(?<sh>\d+),"width":(?<sw>\d+)\}')
if (-not $atlasMatch.Success) {
    throw "The manifest entry for '$elementName' does not declare an atlas and sprite rectangle in the expected shape."
}

$atlasId = $atlasMatch.Groups['id'].Value
$atlasWidth = [int] $atlasMatch.Groups['aw'].Value
$atlasHeight = [int] $atlasMatch.Groups['ah'].Value
$spriteX = [int] $atlasMatch.Groups['sx'].Value
$spriteY = [int] $atlasMatch.Groups['sy'].Value
$spriteWidth = [int] $atlasMatch.Groups['sw'].Value
$spriteHeight = [int] $atlasMatch.Groups['sh'].Value

Write-Output "Declared: element '$elementName' takes a ${spriteWidth}x${spriteHeight} sprite at ($spriteX,$spriteY) from '$atlasId' (${atlasWidth}x${atlasHeight})."

# --- Verify-only path ------------------------------------------------------

if ($Check) {
    if (-not (Test-Path -LiteralPath $resourcePath -PathType Leaf)) {
        Write-Error "The prepared board artwork is absent: $resourcePath. Run Tools/Prepare-OfficialBoardArtwork.ps1 to write it."
        exit 1
    }

    $size = Get-PngSize $resourcePath
    if ($null -eq $size -or $size.Width -ne $spriteWidth -or $size.Height -ne $spriteHeight) {
        Write-Error ("The prepared board artwork is ${($size.Width)}x${($size.Height)}, but the manifest declares ${spriteWidth}x${spriteHeight}. " +
            'Run Tools/Prepare-OfficialBoardArtwork.ps1 -Force to rewrite it.')
        exit 1
    }

    Write-Output "PASS: prepared board artwork present at the declared ${spriteWidth}x${spriteHeight}."
    exit 0
}

if ((Test-Path -LiteralPath $resourcePath -PathType Leaf) -and -not $Force) {
    $size = Get-PngSize $resourcePath
    if ($null -ne $size -and $size.Width -eq $spriteWidth -and $size.Height -eq $spriteHeight) {
        Write-Output "SKIP: prepared board artwork is already present at the declared size. Pass -Force to rewrite it."
        exit 0
    }
}

# --- Resolve the atlas image ----------------------------------------------
# The extraction names an atlas file after its index, so 'Image_17_Atlas' is '*.sprite17__*'. The
# dimensions the manifest declares are what actually confirms the match, and a mismatch stops here.

$atlasIndexMatch = [regex]::Match($atlasId, '^Image_(?<index>\d+)_Atlas$')
if (-not $atlasIndexMatch.Success) {
    throw "The declared atlas resource id '$atlasId' is not in the expected 'Image_<index>_Atlas' form."
}

$atlasIndex = $atlasIndexMatch.Groups['index'].Value
$candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
if ($candidates.Count -ne 1) {
    throw "Expected exactly one extracted image for atlas '$atlasId' under $corpusRoot; found $($candidates.Count)."
}

Add-Type -AssemblyName System.Drawing
$atlas = [System.Drawing.Image]::FromFile($candidates[0].FullName)
try {
    if ($atlas.Width -ne $atlasWidth -or $atlas.Height -ne $atlasHeight) {
        throw ("Atlas '$($candidates[0].Name)' is $($atlas.Width)x$($atlas.Height), but the manifest declares " +
            "${atlasWidth}x${atlasHeight} for '$atlasId'. Nothing was written.")
    }

    # The atlas rectangle is stated from the TOP-left, the same origin System.Drawing uses. This was
    # confirmed against the pixels rather than assumed: reading it bottom-up puts a neighbouring
    # sprite along the top edge of the crop and cuts the board off at the bottom.
    $top = $spriteY
    if ($spriteX -lt 0 -or $top -lt 0 -or
        ($spriteX + $spriteWidth) -gt $atlasWidth -or ($top + $spriteHeight) -gt $atlasHeight) {
        throw "The declared sprite rectangle falls outside the atlas. Nothing was written."
    }

    $cropped = New-Object System.Drawing.Bitmap($spriteWidth, $spriteHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($cropped)
        try {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
            $destination = New-Object System.Drawing.Rectangle 0, 0, $spriteWidth, $spriteHeight
            $source = New-Object System.Drawing.Rectangle $spriteX, $top, $spriteWidth, $spriteHeight
            $graphics.DrawImage($atlas, $destination, $source, [System.Drawing.GraphicsUnit]::Pixel)
        }
        finally { $graphics.Dispose() }

        $resourceDirectory = [System.IO.Path]::GetDirectoryName($resourcePath)
        if (-not (Test-Path -LiteralPath $resourceDirectory -PathType Container)) {
            New-Item -ItemType Directory -Path $resourceDirectory -Force | Out-Null
        }

        $cropped.Save($resourcePath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $cropped.Dispose() }
}
finally { $atlas.Dispose() }

$written = Get-Item -LiteralPath $resourcePath
Write-Output "WROTE: $resourcePath ($($written.Length) bytes, ${spriteWidth}x${spriteHeight}) from $($candidates[0].Name) at ($spriteX,$top) top-left."
Write-Output 'The source atlas and the extracted corpus were read only. The written resource is ignored by Git and is never distributed.'
