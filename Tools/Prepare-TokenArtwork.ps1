<#
.SYNOPSIS
    Prepares a verified hero's placed-token faces as ignored Unity runtime resources.

.DESCRIPTION
    Milestone 8.37. Until Tali the Spiritcaller, no verified hero's printed faces placed a TOKEN on
    the board, so `PlacedTokenView` drew every token as a flat disc under its display name and said
    so in its own remarks: "no catalog lookup and no per-definition art mapping". Her five Green
    faces put pieces on the board, and an unlabelled grey disc is not a board a player can read.

    This script is the only route from the private Tabletopia corpus to something Unity can load,
    and for the hero it is asked about it writes one file per token definition:

        Assets/Resources/Private/Tokens/<token-definition-id>.png

    Those paths are ignored by Git, so a clean checkout has no token art at all and the view keeps
    its generated disc. Nothing else is written, moved, or deleted - the source atlas and the
    extracted corpus are read-only here.

    THE CROP IS NOT GUESSED, on exactly the terms `Prepare-HeroTokens.ps1` establishes. The bundled
    manifest declares, for each element named below, the atlas holding its faces and the sprite
    rectangles inside it; this script reads both and refuses to write anything if the atlas it finds
    does not match the declared dimensions.

    THE PIECE IS DOUBLE-SIDED, AND THAT IS WHY THREE ROLES TAKE TWO FACES. The corpus names its three
    elements `Ice/Totem`, `Ice/Totem2` and `Ice/Totem 3`, and opening both sprites is what explains
    the slash. Each element declares three rectangles. The FIRST is identical across all three
    elements and is a carved totem. The SECOND differs per element and is a frozen hex printed with
    the `-1` hourglass - which is exactly `TaliComponentContent.IceDefinition`'s own Initiative aura,
    and nothing else in her content carries that number. The third is a thin edge strip.

    So this is ONE physical token, Ice on one side and Totem on the other, which is the same fact
    RULING 44 and `SupplyGroupId` already state from the rules side: her three roles share one pile.
    Ice takes the ice face; BOTH totems take the totem face, because both totems ARE that one piece
    and the card that placed it is what decides which rule it carries. Two files would therefore be
    enough, and three are written anyway so the runtime can address every token definition by its own
    id without the view having to know which definitions happen to share art.

    THE FIRST CROP WAS WRONG IN THE WAY THIS NOTE NOW PREVENTS. It read the shared first sprite as a
    card back, took the three per-element seconds as three distinct pieces, and wrote three ice faces
    under three names - two of them for totems. The three seconds ARE distinct (frozen weapons, a
    frozen skeleton, a plain frozen mass) but they are art variants of ONE face, not three pieces.
    Opening them is what settled it; the assertions below are what keep a later corpus change from
    silently re-creating that mistake.

.PARAMETER Hero
    Which hero's tokens to prepare, by the content identity the runtime addresses them with.

.PARAMETER Check
    Read-only verification: proves every declared resource exists at its declared size.

.PARAMETER Force
    Rewrite every prepared face even when all of them already match.
#>
[CmdletBinding()]
param(
    [ValidateSet('tali', 'widget')]
    [string] $Hero = 'tali',
    [switch] $Check,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Pyro's source is a circular standee face. Reuse the validated circle/neutral-field
# extraction, but write only the placed-token identity, never a hero registration.
if ($Hero -eq 'widget') {
    & (Join-Path $PSScriptRoot 'Prepare-HeroTokens.ps1') -Hero widget -Companion -Neutral -Check:$Check -Force:$Force
    exit
}

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$corpusRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
$targetRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')

# The declared mapping, stated rather than searched for, on the same discipline the hero-standee
# table follows. `Face` selects which of the element's two printed sides the definition wears.
$declared = @{
    tali = @{
        # The whole family, so the shared-totem assertion below has something to check across.
        Elements = @(1421878, 1421879, 1421880)
        # The plain frozen mass rather than the frozen-weapons or frozen-skeleton variant: her Ice is
        # placed on an EMPTY space, so a face depicting a frozen unit would contradict the placement.
        IceElement = 1421880
        Tokens = @(
            @{ Face = 'ice'; TokenId = 'tali-ice'; Label = 'Ice' },
            @{ Face = 'totem'; TokenId = 'tali-ancestral-totem'; Label = 'Ancestral Totem' },
            @{ Face = 'totem'; TokenId = 'tali-venerated-totem'; Label = 'Venerated Totem' }
        )
    }
}

if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
    Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
    exit 0
}

$manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
if ($manifests.Count -ne 1) {
    throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
}

$manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json
$plan = $declared[$Hero]

# --- Read the whole element family, and prove the double-sided shape before cropping anything -----

$family = @()
foreach ($id in $plan.Elements) {
    $found = @($manifest | Where-Object { $_.elementId -eq $id })
    if ($found.Count -ne 1) {
        throw "The manifest holds $($found.Count) elements with id $id; one was expected."
    }

    $element = $found[0]
    if ($element.elementType -ne 'Tokens') {
        throw "Element $id is '$($element.elementType)', not 'Tokens'. Nothing was written."
    }

    $atlas = $element.atlases[0]
    $sprites = @($atlas.sprites)
    if ($sprites.Count -ne 3) {
        throw ("Element $id declares $($sprites.Count) sprites; three were expected (totem face, " +
            'ice face, edge strip). Nothing was written.')
    }

    $family += [pscustomobject]@{
        Element = $id
        AtlasId = $atlas.resource.id
        AtlasWidth = [int] $atlas.width
        AtlasHeight = [int] $atlas.height
        Totem = $sprites[0]
        Ice = $sprites[1]
    }
}

$atlasIds = @($family | ForEach-Object { $_.AtlasId } | Sort-Object -Unique)
if ($atlasIds.Count -ne 1) {
    throw "The declared elements span $($atlasIds.Count) atlases; one was expected."
}

# The totem face is shared. If it stops being shared, the first sprite is no longer the side this
# script thinks it is, and both totems would be written from whatever now sits there.
$totemRects = @($family | ForEach-Object { "$($_.Totem.x),$($_.Totem.y)" } | Sort-Object -Unique)
if ($totemRects.Count -ne 1) {
    throw ("The first sprite differs across the family ($($totemRects -join '; ')), so it is not the " +
        'shared totem face this mapping depends on. Nothing was written.')
}

# The ice faces are the per-element variants. If they stop differing, the second sprite is not the
# side this script thinks it is either.
$iceRects = @($family | ForEach-Object { "$($_.Ice.x),$($_.Ice.y)" } | Sort-Object -Unique)
if ($iceRects.Count -ne $family.Count) {
    throw ("The second sprite repeats across the family ($($iceRects -join '; ')), so it is not the " +
        'per-element ice variant this mapping depends on. Nothing was written.')
}

$chosen = @($family | Where-Object { $_.Element -eq $plan.IceElement })
if ($chosen.Count -ne 1) {
    throw "The declared ice element $($plan.IceElement) is not in the family. Nothing was written."
}

Write-Output ("Declared: family $($plan.Elements -join ', ') on '$($atlasIds[0])'; shared totem face " +
    "at $($totemRects[0]); ice variants at $($iceRects -join '; '); ice taken from " +
    "$($plan.IceElement).")

# --- Resolve the atlas image ---------------------------------------------------------------------

$atlasIndexMatch = [regex]::Match($atlasIds[0], '^Image_(?<index>\d+)_Atlas$')
if (-not $atlasIndexMatch.Success) {
    throw "The declared atlas resource id '$($atlasIds[0])' is not in the expected form."
}

$atlasIndex = $atlasIndexMatch.Groups['index'].Value
$candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
if ($candidates.Count -ne 1) {
    throw "Expected exactly one extracted image for atlas '$($atlasIds[0])'; found $($candidates.Count)."
}

Add-Type -AssemblyName System.Drawing
$source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
try {
    if ($source.Width -ne $chosen[0].AtlasWidth -or $source.Height -ne $chosen[0].AtlasHeight) {
        throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the manifest " +
            "declares $($chosen[0].AtlasWidth)x$($chosen[0].AtlasHeight). Nothing was written.")
    }

    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }

    $written = 0
    $matched = 0
    foreach ($token in $plan.Tokens) {
        $rect = if ($token.Face -eq 'ice') { $chosen[0].Ice } else { $chosen[0].Totem }
        $x = [int] $rect.x
        $y = [int] $rect.y
        $w = [int] $rect.width
        $h = [int] $rect.height
        if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $source.Width -or ($y + $h) -gt $source.Height) {
            throw 'A declared face rectangle falls outside the atlas. Nothing was written.'
        }

        $target = [System.IO.Path]::Combine($targetRoot, "$($token.TokenId).png")
        if ((Test-Path -LiteralPath $target) -and -not $Force) {
            $existing = [System.Drawing.Image]::FromFile($target)
            try { $same = $existing.Width -eq $w -and $existing.Height -eq $h }
            finally { $existing.Dispose() }
            if ($same) {
                $matched++
                Write-Output "OK: $($token.Label) already prepared at ${w}x${h}: $target"
                continue
            }
        }

        if ($Check) { throw "Missing or stale prepared token face: $target" }

        $bitmap = New-Object System.Drawing.Bitmap $w, $h
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $graphics.DrawImage(
                    $source,
                    (New-Object System.Drawing.Rectangle 0, 0, $w, $h),
                    (New-Object System.Drawing.Rectangle $x, $y, $w, $h),
                    [System.Drawing.GraphicsUnit]::Pixel)
            } finally { $graphics.Dispose() }

            $bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
            $written++
            Write-Output "WROTE: $($token.Label) ($($token.Face) face) -> $target (${w}x${h}, from ($x,$y))."
        } finally { $bitmap.Dispose() }
    }

    Write-Output ("Prepared token faces: $written written, $matched already present. The source atlas " +
        'and the extracted corpus were read only. The written resources are ignored by Git and are ' +
        'never distributed.')
} finally {
    $source.Dispose()
}
