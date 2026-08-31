<#
.SYNOPSIS
    Prepares a hero's private official circular tokens as ignored Unity runtime resources.

.DESCRIPTION
    Milestone 8.11b, generalised at milestone 8.20. The configured match draws each hero as the
    official circular token rather than as a coloured prism. The artwork is private: it lives in the
    local Tabletopia corpus under Docs/, which .gitignore keeps out of the repository, and it must
    stay there. This script is the only route from that corpus to something Unity can load, and for
    the hero it is asked about it writes exactly two files:

        Assets/Resources/Private/Heroes/<content-id>-red.png
        Assets/Resources/Private/Heroes/<content-id>-blue.png

    Both paths are ignored, so a clean checkout has no token art at all and the hero views keep the
    generated primitive prism. Nothing else is written, moved, or deleted - the source atlas and the
    extracted corpus are read-only to this script.

    The content id is the addressing key, not a label: `HeroTokenArtwork.ResourcePath` composes it
    with the authoritative team, so the two names written here are exactly the two the runtime asks
    for and a hero whose content id changed would silently keep its prism.

    The crop is not guessed. The bundled manifest declares, for the element named below, the atlas
    that holds its faces and the sprite rectangles inside it; this script reads both and refuses to
    write anything if the atlas it finds does not match the declared dimensions. Each element
    declares two 302x302 faces, and which of them is which team is measured rather than taken from
    their order: the reviewed milestone 4.4 mapping - Steam/orange to project Red, Crystal/light-blue
    to project Blue - is applied to each face's own ring colour, and the script stops unless exactly
    one face of each kind is found.

    The sprite is a circle inscribed in its square, on the atlas's own flat background. That was
    measured rather than assumed: the background runs from each corner to within a pixel of the
    inscribed circle, and the four edge midpoints are already ring. The alpha is therefore the
    inscribed disc, antialiased across one pixel, and the script verifies both ends of that claim
    before it writes - the corners must be background and the ring must not be.

    Milestone 8.30i1 also measures the flat team-coloured field inside the official ring. The field
    colour is selected from the dominant low-variance team-coloured cluster in the declared source
    face, never from a hero-specific mask. Only matching pixels inside the protected ring radius are
    replaced with opaque white. The route refuses to write unless the field has a bounded area, the
    portrait retains a substantial non-field area, both official rings still classify correctly,
    and every non-field source pixel remains byte-for-byte faithful in the prepared bitmap.

.PARAMETER Hero
    Which hero's tokens to prepare, by the content identity the runtime addresses them with. The
    manifest element each name maps to is declared in this script rather than searched for, because
    the source set's element names are not the project's: Wasp's element is `Wasp` and Arien's is
    `Arien2`, and a script that matched on the hero's own name would have found nothing for the
    second hero this project added. Brogan's happens to be `Brogan`, which is exactly why the
    declaration stays a table rather than becoming a rule - two of the three names agree with the
    project's and the third does not.

.PARAMETER Check
    Verify only. Rebuilds the expected prepared pixels from the declared source face in memory and
    compares them with the resources, including size, transparent exterior, opaque white field,
    retained team ring, and source-faithful portrait pixels. Writes nothing.

.PARAMETER Force
    Rewrite the resources even when correctly sized ones are already present.
#>
[CmdletBinding()]
param(
    [ValidateSet('wasp', 'arien', 'brogan', 'sabina', 'tigerclaw', 'xargatha')]
    [string] $Hero = 'wasp',
    [switch] $Check,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The declared source element for each hero this project has prepared. Element identity is checked
# against the manifest below, so a corpus whose ids or names have moved stops here rather than
# cropping whatever now sits at those coordinates.
$declaredHeroes = @{
    wasp   = @{ ElementId = 8597310; ElementName = 'Wasp';   DisplayName = 'Wasp' }
    arien  = @{ ElementId = 8596244; ElementName = 'Arien2'; DisplayName = 'Arien' }
    brogan = @{ ElementId = 8596274; ElementName = 'Brogan'; DisplayName = 'Brogan' }
    # Milestone 8.22. Hers agrees with the project's name too, which is the second of four; the
    # declaration stays a table because Arien's does not.
    sabina = @{ ElementId = 8596284; ElementName = 'Sabina'; DisplayName = 'Sabina' }
    # Milestone 8.23. His element sits next to Wasp's in the corpus rather than beside the other
    # heroes' - 8597311 to her 8597310 - which is why the id is declared and never searched for.
    tigerclaw = @{ ElementId = 8597311; ElementName = 'Tigerclaw'; DisplayName = 'Tigerclaw' }
    # Milestone 8.24. Hers is the sixth and last of the 8.17 roster. The element is the ordinary
    # standee the manifest calls a `VerticalTokens` entry - Tabletopia's label for the hero
    # miniature, not rulebook p.19's token category - and its id is recorded in
    # Docs/AI/Rules/HERO_XARGATHA.md beside the atlas it is cut from.
    xargatha = @{ ElementId = 8597214; ElementName = 'Xargatha'; DisplayName = 'Xargatha' }
}

$declared = $declaredHeroes[$Hero]
$elementId = $declared.ElementId
$elementName = $declared.ElementName
$heroLabel = $declared.DisplayName
$elementType = 'VerticalTokens'
$declaredFaceSize = 302
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$corpusRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
$resourceDirectory = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private', 'Heroes')
$resources = [ordered]@{
    Red  = [System.IO.Path]::Combine($resourceDirectory, "$Hero-red.png")
    Blue = [System.IO.Path]::Combine($resourceDirectory, "$Hero-blue.png")
}

function Get-ColourDistanceSquared($left, $right) {
    return [Math]::Pow([int] $left.R - [int] $right.R, 2) +
        [Math]::Pow([int] $left.G - [int] $right.G, 2) +
        [Math]::Pow([int] $left.B - [int] $right.B, 2)
}

function Test-TeamColour($pixel, [string] $team) {
    if ($team -eq 'Red') { return ([int] $pixel.R - [int] $pixel.B) -ge 40 }
    return ([int] $pixel.B - [int] $pixel.R) -ge 40
}

function Get-TeamFromRing([System.Drawing.Bitmap] $bitmap, [int] $faceSize) {
    $centre = ($faceSize - 1) / 2.0
    $radius = $faceSize / 2.0
    $red = 0.0; $green = 0.0; $blue = 0.0; $samples = 0
    for ($angle = 0; $angle -lt 360; $angle += 5) {
        $radians = $angle * [Math]::PI / 180.0
        $sampleX = [int] [Math]::Round($centre + ($radius * 0.96 * [Math]::Cos($radians)))
        $sampleY = [int] [Math]::Round($centre + ($radius * 0.96 * [Math]::Sin($radians)))
        $pixel = $bitmap.GetPixel($sampleX, $sampleY)
        $red += $pixel.R; $green += $pixel.G; $blue += $pixel.B; $samples++
    }

    $average = [System.Drawing.Color]::FromArgb(
        255,
        [int] [Math]::Round($red / $samples),
        [int] [Math]::Round($green / $samples),
        [int] [Math]::Round($blue / $samples))
    $team = if (([int] $average.R - [int] $average.B) -ge 40) {
        'Red'
    } elseif (([int] $average.B - [int] $average.R) -ge 40) {
        'Blue'
    } else {
        $null
    }
    return [pscustomobject]@{ Team = $team; Average = $average }
}

function Get-PreparedFace([System.Drawing.Bitmap] $square, [int] $faceSize, [string] $team) {
    $centre = ($faceSize - 1) / 2.0
    $radius = $faceSize / 2.0
    $protectedRingStart = $radius * 0.90
    $bins = @{}

    # JPEG compression leaves a handful of neighbouring values around a visually flat colour.
    # Eight-value bins find that field without assuming orange/light-blue numbers from one atlas.
    for ($pixelY = 0; $pixelY -lt $faceSize; $pixelY++) {
        for ($pixelX = 0; $pixelX -lt $faceSize; $pixelX++) {
            $distance = [Math]::Sqrt(
                [Math]::Pow($pixelX - $centre, 2) + [Math]::Pow($pixelY - $centre, 2))
            if ($distance -ge $protectedRingStart) { continue }
            $pixel = $square.GetPixel($pixelX, $pixelY)
            if (-not (Test-TeamColour $pixel $team)) { continue }
            $key = '{0},{1},{2}' -f (
                [Math]::Floor($pixel.R / 8)), (
                [Math]::Floor($pixel.G / 8)), (
                [Math]::Floor($pixel.B / 8))
            if (-not $bins.ContainsKey($key)) {
                $bins[$key] = [pscustomobject]@{ Count = 0; Red = 0L; Green = 0L; Blue = 0L }
            }
            $bin = $bins[$key]
            $bin.Count++
            $bin.Red += $pixel.R; $bin.Green += $pixel.G; $bin.Blue += $pixel.B
        }
    }

    if ($bins.Count -eq 0) {
        throw "The $team face has no separable team-coloured interior field. Nothing was written."
    }
    $fieldBin = @($bins.Values | Sort-Object Count -Descending)[0]
    $fieldColour = [System.Drawing.Color]::FromArgb(
        255,
        [int] [Math]::Round($fieldBin.Red / $fieldBin.Count),
        [int] [Math]::Round($fieldBin.Green / $fieldBin.Count),
        [int] [Math]::Round($fieldBin.Blue / $fieldBin.Count))

    $token = New-Object System.Drawing.Bitmap(
        $faceSize, $faceSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $fieldPixels = 0
    $portraitPixels = 0
    $fieldDistanceSquared = 32 * 32
    for ($pixelY = 0; $pixelY -lt $faceSize; $pixelY++) {
        for ($pixelX = 0; $pixelX -lt $faceSize; $pixelX++) {
            $distance = [Math]::Sqrt(
                [Math]::Pow($pixelX - $centre, 2) + [Math]::Pow($pixelY - $centre, 2))
            $coverage = [Math]::Min(1.0, [Math]::Max(0.0, $radius - $distance))
            if ($coverage -le 0.0) {
                $token.SetPixel($pixelX, $pixelY, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
                continue
            }

            $pixel = $square.GetPixel($pixelX, $pixelY)
            $isField = $distance -lt $protectedRingStart -and
                (Test-TeamColour $pixel $team) -and
                (Get-ColourDistanceSquared $pixel $fieldColour) -le $fieldDistanceSquared
            if ($isField) {
                $token.SetPixel($pixelX, $pixelY, [System.Drawing.Color]::White)
                $fieldPixels++
            } else {
                $token.SetPixel(
                    $pixelX,
                    $pixelY,
                    [System.Drawing.Color]::FromArgb(
                        [int] [Math]::Round($coverage * 255), $pixel.R, $pixel.G, $pixel.B))
                if ($distance -lt $protectedRingStart) { $portraitPixels++ }
            }
        }
    }

    $interiorArea = [Math]::PI * [Math]::Pow($protectedRingStart, 2)
    $fieldRatio = $fieldPixels / $interiorArea
    $portraitRatio = $portraitPixels / $interiorArea
    if ($fieldRatio -lt 0.02 -or $fieldRatio -gt 0.60 -or $portraitRatio -lt 0.30) {
        $token.Dispose()
        throw ("The $team face cannot distinguish its flat field from portrait/ring pixels: " +
            "field=$([Math]::Round($fieldRatio * 100, 1))%, portrait=$([Math]::Round($portraitRatio * 100, 1))%. Nothing was written.")
    }

    $preparedRing = Get-TeamFromRing $token $faceSize
    if ($preparedRing.Team -ne $team) {
        $token.Dispose()
        throw "The prepared face no longer retains the official $team ring. Nothing was written."
    }

    return [pscustomobject]@{
        Bitmap = $token
        FieldColour = $fieldColour
        FieldPixels = $fieldPixels
        PortraitPixels = $portraitPixels
        FieldRatio = $fieldRatio
        PortraitRatio = $portraitRatio
    }
}

function Assert-BitmapMatches([System.Drawing.Bitmap] $actual, [System.Drawing.Bitmap] $expected, [string] $label) {
    if ($actual.Width -ne $expected.Width -or $actual.Height -ne $expected.Height) {
        throw "$label has the wrong dimensions."
    }
    for ($pixelY = 0; $pixelY -lt $expected.Height; $pixelY++) {
        for ($pixelX = 0; $pixelX -lt $expected.Width; $pixelX++) {
            if ($actual.GetPixel($pixelX, $pixelY).ToArgb() -ne $expected.GetPixel($pixelX, $pixelY).ToArgb()) {
                throw "$label differs from its deterministic source-derived preparation at ($pixelX,$pixelY)."
            }
        }
    }
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

        # Which team this face belongs to, measured. Nothing at runtime asks a pixel what team a hero
        # is on - this decides only which file the face is written to.
        $ring = Get-TeamFromRing $square $declaredFaceSize
        $team = $ring.Team
        if ($null -eq $team) {
            $square.Dispose()
            throw ("The face at ($x,$y) has an average ring colour of " +
                "R$($ring.Average.R) G$($ring.Average.G) B$($ring.Average.B), which is neither Steam/orange nor Crystal/light-blue. " +
                'Nothing was written.')
        }

        if ($prepared.ContainsKey($team)) {
            $square.Dispose()
            throw "Two declared faces both measured as $team. Nothing was written."
        }

        # The background is the atlas's own flat fill outside the inscribed circle, which is what the
        # alpha removes. Both ends of that claim are checked here rather than trusted: a corner must
        # be that fill, and the ring must not be.
        $centre = ($declaredFaceSize - 1) / 2.0
        $corner = $square.GetPixel(0, 0)
        $edge = $square.GetPixel([int] [Math]::Round($centre), 1)
        $cornerToEdge = [Math]::Sqrt(
            [Math]::Pow($corner.R - $edge.R, 2) + [Math]::Pow($corner.G - $edge.G, 2) + [Math]::Pow($corner.B - $edge.B, 2))
        if ($cornerToEdge -lt 64) {
            $square.Dispose()
            throw ("The face at ($x,$y) has no separable background: its corner and its top edge midpoint " +
                "differ by only $([int]$cornerToEdge). Nothing was written.")
        }

        $facePreparation = Get-PreparedFace $square $declaredFaceSize $team
        $square.Dispose()
        $prepared[$team] = $facePreparation.Bitmap
        Write-Output ("Measured: the face at ($x,$y) has an average ring colour of R$($ring.Average.R) G$($ring.Average.G) B$($ring.Average.B), " +
            "which is project $team; its source-derived flat field is R$($facePreparation.FieldColour.R) G$($facePreparation.FieldColour.G) B$($facePreparation.FieldColour.B) " +
            "across $($facePreparation.FieldPixels) pixels ($([Math]::Round($facePreparation.FieldRatio * 100, 1))%), while $($facePreparation.PortraitPixels) protected interior pixels remain source-faithful.")
    }

    if ($Check -or -not $Force) {
        $missing = @($resources.Keys | Where-Object {
            -not (Test-Path -LiteralPath $resources[$_] -PathType Leaf)
        })
        if ($missing.Count -eq 0) {
            foreach ($team in $resources.Keys) {
                $actual = [System.Drawing.Bitmap]::FromFile($resources[$team])
                try { Assert-BitmapMatches $actual $prepared[$team] "$team $heroLabel token" }
                finally { $actual.Dispose() }
            }
            if ($Check) {
                Write-Output "PASS: PreparedHeroTokens_UseWhiteInteriorFieldsWhilePreservingPortraitsTeamRingsAndSharedRuntimeAddressing ($heroLabel Red/Blue)."
                exit 0
            }
            Write-Output "SKIP: both prepared $heroLabel hero tokens already match the deterministic white-field preparation. Pass -Force to rewrite them."
            exit 0
        }
        if ($Check) {
            throw "Prepared $heroLabel hero tokens are absent: $($missing -join ', '). Run Tools/Prepare-HeroTokens.ps1 -Hero $Hero to write them."
        }
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
