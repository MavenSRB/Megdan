<#
.SYNOPSIS
    Prepares the private official Wasp hero tokens as ignored Unity runtime resources.

.DESCRIPTION
    Milestone 8.11b. The configured mirrored match draws each Wasp as the official circular token
    rather than as a coloured prism. The artwork is private: it lives in the local Tabletopia corpus
    under Docs/, which .gitignore keeps out of the repository, and it must stay there. This script is
    the only route from that corpus to something Unity can load, and it writes exactly two files:

        Assets/Resources/Private/Heroes/wasp-red.png
        Assets/Resources/Private/Heroes/wasp-blue.png

    Both paths are ignored, so a clean checkout has no token art at all and the hero views keep the
    generated primitive prism. Nothing else is written, moved, or deleted - the source atlas and the
    extracted corpus are read-only to this script.

    The crop is not guessed. The bundled manifest declares, for the element named Wasp
    (elementId 8597310, elementType VerticalTokens), the atlas that holds its faces and the sprite
    rectangles inside it; this script reads both and refuses to write anything if the atlas it finds
    does not match the declared dimensions. The element declares two 302x302 faces, and which of them
    is which team is measured rather than taken from their order: the reviewed milestone 4.4 mapping
    - Steam/orange to project Red, Crystal/light-blue to project Blue - is applied to each face's own
    ring colour, and the script stops unless exactly one face of each kind is found.

    The sprite is a circle inscribed in its square, on the atlas's own flat background. That was
    measured rather than assumed: the background runs from each corner to within a pixel of the
    inscribed circle, and the four edge midpoints are already ring. The alpha is therefore the
    inscribed disc, antialiased across one pixel, and the script verifies both ends of that claim
    before it writes - the corners must be background and the ring must not be.

.PARAMETER Check
    Verify only. Reports whether the prepared resources exist and match the declared sprite size,
    and writes nothing. Exits non-zero when either resource is missing or has the wrong dimensions.

.PARAMETER Force
    Rewrite the resources even when correctly sized ones are already present.
#>
[CmdletBinding()]
param(
    [switch] $Check,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$elementId = 8597310
$elementName = 'Wasp'
$elementType = 'VerticalTokens'
$declaredFaceSize = 302
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$corpusRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
$resourceDirectory = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private', 'Heroes')
$resources = [ordered]@{
    Red  = [System.IO.Path]::Combine($resourceDirectory, 'wasp-red.png')
    Blue = [System.IO.Path]::Combine($resourceDirectory, 'wasp-blue.png')
}

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
        Write-Error "The private Tabletopia corpus is absent: $corpusRoot. The prepared hero tokens cannot be verified against their source."
        exit 1
    }

    throw "The private Tabletopia corpus is absent: $corpusRoot"
}

$manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
if ($manifests.Count -ne 1) {
    throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
}

$manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json
$entries = @($manifest | Where-Object { $_.elementId -eq $elementId })
if ($entries.Count -ne 1) {
    throw "Expected exactly one manifest element $elementId; found $($entries.Count)."
}

$entry = $entries[0]
if ($entry.elementName -cne $elementName -or $entry.elementType -cne $elementType) {
    throw "Manifest identity changed for element ${elementId}: found '$($entry.elementName)' / '$($entry.elementType)'."
}

$atlas = $entry.atlases[0]
$atlasId = $atlas.resource.id
$atlasWidth = [int] $atlas.width
$atlasHeight = [int] $atlas.height
$faces = @($atlas.sprites | Where-Object { $_.width -eq $declaredFaceSize -and $_.height -eq $declaredFaceSize })
if ($faces.Count -ne 2) {
    throw "The manifest entry for '$elementName' declares $($faces.Count) ${declaredFaceSize}x${declaredFaceSize} faces in '$atlasId'; two were expected."
}

Write-Output "Declared: element '$elementName' ($elementId) takes two ${declaredFaceSize}x${declaredFaceSize} faces from '$atlasId' (${atlasWidth}x${atlasHeight}) at ($($faces[0].x),$($faces[0].y)) and ($($faces[1].x),$($faces[1].y))."

# --- Verify-only path ------------------------------------------------------

if ($Check) {
    $missing = @($resources.Keys | Where-Object { -not (Test-Path -LiteralPath $resources[$_] -PathType Leaf) })
    if ($missing.Count -gt 0) {
        Write-Error "Prepared Wasp hero tokens are absent: $($missing -join ', '). Run Tools/Prepare-WaspHeroTokens.ps1 to write them."
        exit 1
    }

    foreach ($team in $resources.Keys) {
        $size = Get-PngSize $resources[$team]
        if ($null -eq $size -or $size.Width -ne $declaredFaceSize -or $size.Height -ne $declaredFaceSize) {
            Write-Error "The prepared $team Wasp token does not match the declared ${declaredFaceSize}x${declaredFaceSize}. Run Tools/Prepare-WaspHeroTokens.ps1 -Force to rewrite it."
            exit 1
        }
    }

    Write-Output "PASS: both prepared Wasp hero tokens present at the declared ${declaredFaceSize}x${declaredFaceSize}."
    exit 0
}

$present = @($resources.Keys | Where-Object {
    $path = $resources[$_]
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $false }
    $size = Get-PngSize $path
    $null -ne $size -and $size.Width -eq $declaredFaceSize -and $size.Height -eq $declaredFaceSize
})
if ($present.Count -eq $resources.Count -and -not $Force) {
    Write-Output 'SKIP: both prepared Wasp hero tokens are already present at the declared size. Pass -Force to rewrite them.'
    exit 0
}

# --- Resolve the atlas image ----------------------------------------------
# The extraction names an atlas file after its index, so 'Image_1_Atlas' is '*.sprite1__*'. The
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
$source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
$prepared = @{}
try {
    if ($source.Width -ne $atlasWidth -or $source.Height -ne $atlasHeight) {
        throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the manifest declares " +
            "${atlasWidth}x${atlasHeight} for '$atlasId'. Nothing was written.")
    }

    # The atlas rectangle is stated from the TOP-left, the same origin System.Drawing uses - the same
    # reading milestone 8.11a confirmed against the pixels for the board face in this same corpus.
    foreach ($face in $faces) {
        $x = [int] $face.x
        $y = [int] $face.y
        if ($x -lt 0 -or $y -lt 0 -or
            ($x + $declaredFaceSize) -gt $atlasWidth -or ($y + $declaredFaceSize) -gt $atlasHeight) {
            throw "A declared face rectangle falls outside the atlas. Nothing was written."
        }

        $square = New-Object System.Drawing.Bitmap($declaredFaceSize, $declaredFaceSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($square)
        try {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
            $destination = New-Object System.Drawing.Rectangle 0, 0, $declaredFaceSize, $declaredFaceSize
            $rectangle = New-Object System.Drawing.Rectangle $x, $y, $declaredFaceSize, $declaredFaceSize
            $graphics.DrawImage($source, $destination, $rectangle, [System.Drawing.GraphicsUnit]::Pixel)
        }
        finally { $graphics.Dispose() }

        # Which team this face belongs to, measured. The ring is sampled all the way round at 96% of
        # the radius and averaged, so one dark passage of the portrait cannot decide it; the reviewed
        # milestone 4.4 mapping then reads that average. Nothing at runtime asks a pixel what team a
        # hero is on - this decides only which file the face is written to.
        $centre = ($declaredFaceSize - 1) / 2.0
        $radius = $declaredFaceSize / 2.0
        $red = 0.0; $green = 0.0; $blue = 0.0; $samples = 0
        for ($angle = 0; $angle -lt 360; $angle += 5) {
            $radians = $angle * [Math]::PI / 180.0
            $sampleX = [int] [Math]::Round($centre + ($radius * 0.96 * [Math]::Cos($radians)))
            $sampleY = [int] [Math]::Round($centre + ($radius * 0.96 * [Math]::Sin($radians)))
            $pixel = $square.GetPixel($sampleX, $sampleY)
            $red += $pixel.R; $green += $pixel.G; $blue += $pixel.B; $samples++
        }

        $red = $red / $samples; $green = $green / $samples; $blue = $blue / $samples
        $team = if ($red - $blue -ge 40) { 'Red' } elseif ($blue - $red -ge 40) { 'Blue' } else { $null }
        if ($null -eq $team) {
            $square.Dispose()
            throw ("The face at ($x,$y) has an average ring colour of " +
                "R$([int]$red) G$([int]$green) B$([int]$blue), which is neither Steam/orange nor Crystal/light-blue. " +
                'Nothing was written.')
        }

        if ($prepared.ContainsKey($team)) {
            $square.Dispose()
            throw "Two declared faces both measured as $team. Nothing was written."
        }

        # The background is the atlas's own flat fill outside the inscribed circle, which is what the
        # alpha removes. Both ends of that claim are checked here rather than trusted: a corner must
        # be that fill, and the ring must not be.
        $corner = $square.GetPixel(0, 0)
        $edge = $square.GetPixel([int] [Math]::Round($centre), 1)
        $cornerToEdge = [Math]::Sqrt(
            [Math]::Pow($corner.R - $edge.R, 2) + [Math]::Pow($corner.G - $edge.G, 2) + [Math]::Pow($corner.B - $edge.B, 2))
        if ($cornerToEdge -lt 64) {
            $square.Dispose()
            throw ("The face at ($x,$y) has no separable background: its corner and its top edge midpoint " +
                "differ by only $([int]$cornerToEdge). Nothing was written.")
        }

        # The inscribed disc, antialiased across the outermost pixel so the token's edge is clean
        # against the board underneath it rather than stepped.
        $token = New-Object System.Drawing.Bitmap($declaredFaceSize, $declaredFaceSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        for ($pixelY = 0; $pixelY -lt $declaredFaceSize; $pixelY++) {
            for ($pixelX = 0; $pixelX -lt $declaredFaceSize; $pixelX++) {
                $distance = [Math]::Sqrt([Math]::Pow($pixelX - $centre, 2) + [Math]::Pow($pixelY - $centre, 2))
                $coverage = [Math]::Min(1.0, [Math]::Max(0.0, $radius - $distance))
                if ($coverage -le 0.0) {
                    $token.SetPixel($pixelX, $pixelY, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
                    continue
                }

                $pixel = $square.GetPixel($pixelX, $pixelY)
                $token.SetPixel(
                    $pixelX,
                    $pixelY,
                    [System.Drawing.Color]::FromArgb([int] [Math]::Round($coverage * 255), $pixel.R, $pixel.G, $pixel.B))
            }
        }

        $square.Dispose()
        $prepared[$team] = $token
        Write-Output ("Measured: the face at ($x,$y) has an average ring colour of R$([int]$red) G$([int]$green) B$([int]$blue), " +
            "which is project $team under the reviewed Steam/orange-to-Red and Crystal/light-blue-to-Blue mapping.")
    }

    if (-not (Test-Path -LiteralPath $resourceDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $resourceDirectory -Force | Out-Null
    }

    foreach ($team in $resources.Keys) {
        if (-not $prepared.ContainsKey($team)) {
            throw "No declared face measured as $team. Nothing was written."
        }

        $prepared[$team].Save($resources[$team], [System.Drawing.Imaging.ImageFormat]::Png)
        $written = Get-Item -LiteralPath $resources[$team]
        Write-Output "WROTE: $($resources[$team]) ($($written.Length) bytes, ${declaredFaceSize}x${declaredFaceSize})."
    }
}
finally {
    foreach ($token in $prepared.Values) { $token.Dispose() }
    $source.Dispose()
}

Write-Output "The source atlas and the extracted corpus were read only. The written resources are ignored by Git and are never distributed."
