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
    [ValidateSet('tali', 'widget', 'min', 'ignatia', 'trinkets', 'mrak', 'wuk', 'mortimer', 'gydion')]
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

# Trinkets' three Barrier pieces share the one printed Wall_Barrier face. The Turret is an
# obstacle, not a token; its separate standee is prepared by Prepare-HeroTokens.ps1 -Companion.
if ($Hero -eq 'trinkets') {
    $projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $corpusRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
    $targetRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')
    if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
        Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
        exit 0
    }
    $manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
    if ($manifests.Count -ne 1) { throw 'Expected exactly one bundled manifest.' }
    $manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json
    $found = @($manifest | Where-Object { $_.elementId -eq 1421882 })
    if ($found.Count -ne 1 -or $found[0].elementName -ne 'Wall_Barrier' -or
        $found[0].elementType -ne 'Tokens') {
        throw 'Trinkets Barrier source identity changed; nothing was written.'
    }
    $atlas = $found[0].atlases[0]
    $rect = $atlas.sprites[0]
    if ($atlas.resource.id -ne 'Image_0_Atlas' -or $rect.x -ne 2768 -or
        $rect.y -ne 2233 -or $rect.width -ne 337 -or $rect.height -ne 289) {
        throw 'Trinkets Barrier source rectangle changed; nothing was written.'
    }
    $candidate = @(Get-ChildItem -LiteralPath $corpusRoot -Filter '*.sprite0__*.jpg' -File)
    if ($candidate.Count -ne 1) { throw 'Expected exactly one Image_0 atlas image.' }
    $target = [System.IO.Path]::Combine($targetRoot, 'trinkets-barrier.png')
    Add-Type -AssemblyName System.Drawing
    if ($Check -or ((Test-Path -LiteralPath $target -PathType Leaf) -and -not $Force)) {
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
            throw 'Trinkets Barrier resource has not been prepared.'
        }
        $existing = [System.Drawing.Image]::FromFile($target)
        try {
            if ($existing.Width -ne 337 -or $existing.Height -ne 289) {
                throw 'Trinkets Barrier prepared dimensions changed.'
            }
        } finally { $existing.Dispose() }
        Write-Output "PASS: $target (337x289)."
        exit 0
    }
    New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
    $source = [System.Drawing.Image]::FromFile($candidate[0].FullName)
    try {
        if ($source.Width -ne [int]$atlas.width -or
            $source.Height -ne [int]$atlas.height) {
            throw 'Trinkets Barrier atlas dimensions changed.'
        }
        $bitmap = New-Object System.Drawing.Bitmap 337, 289
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.DrawImage($source,
                    (New-Object System.Drawing.Rectangle(0, 0, 337, 289)),
                    (New-Object System.Drawing.Rectangle(2768, 2233, 337, 289)),
                    [System.Drawing.GraphicsUnit]::Pixel)
            } finally { $graphics.Dispose() }
            $bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
    } finally { $source.Dispose() }
    Write-Output "WROTE: $target (337x289); source corpus remains read-only and private."
    exit 0
}

# -------------------------------------------------------------------------------------------------
# Milestone 8.61j. Min the Dragonmonk's FIVE prepared faces, in a self-contained arm.
#
# It is its own block rather than a third row of the `$declared` table below because her pieces are
# not one family: Tali's three roles are one double-sided token in one atlas, and Min's three printed
# KINDS are three separate physical pieces spread across TWO atlases, one of which is a modern hex
# pair and the other a pair of older square-ish mine tiles. A table shaped for the first cannot
# describe the second without becoming a table of arbitrary rectangles, which is the thing every note
# in this file exists to avoid.
#
# WHAT EACH ELEMENT IS, read from the manifest and LOOKED AT rather than inferred from its name:
#
#   * `Smoke_Bomb` 1421883, `Image_0_Atlas` (349,2526) - a blue smoke cloud in a hex. Her Silver
#     basic's piece, and the only one of the five whose manifest name says what it draws.
#   * 1421877, `Image_0_Atlas` (0,2526) - NAMED `Mine_Dud` in the manifest and it is THE GRENADE: a
#     fiery skull-bomb hex, identified by the user on 2026-09-19 and recorded as MI-Q6. It sits
#     beside the Smoke bomb in the modern hex block and looks nothing like the two mine tiles below.
#     The name is the corpus's mistake and this script does not repeat it.
#   * 1134878 / 1134879, `Image_3_Atlas` - the older mine pair. Their FIRST sprite is byte-identical
#     between the two elements at (2677,562) and is a skull-and-crossbones danger sign: that is the
#     FACEDOWN back, which is exactly what `MinComponentContent.MineFacedownDefinition` is for, and
#     the only face any seat but Min's is ever handed. Their SECONDS differ - 1134879's at
#     (3103,562) is a green landmine (the blast) and 1134878's at (3543,562) is a bare grass patch
#     (the dud) - which is the same shared-first / distinct-second shape Tali's family has, and the
#     assertions below check it for the same reason hers do.
#
# MI-Q6 IS SETTLED AND THE CARD GLYPH IS NOT CROPPED. Every face here is a real extracted token.
# -------------------------------------------------------------------------------------------------
if ($Hero -eq 'min') {
    $projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $corpusRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
    $targetRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')

    if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
        Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
        exit 0
    }

    $manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
    if ($manifests.Count -ne 1) {
        throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json

    # Element id, which of its declared sprites, the expected top-left the milestone measured, and
    # the token definition id the runtime addresses the file by. The expected origin is an
    # ASSERTION rather than the crop: the width and height are read from the manifest, so a corpus
    # that re-packs its atlas fails loudly here instead of writing a neighbouring token's pixels.
    $minTokens = @(
        @{ Element = 1421883; Sprite = 0; X = 349;  Y = 2526; TokenId = 'min-smoke-bomb-token'; Label = 'Smoke bomb' },
        @{ Element = 1421877; Sprite = 0; X = 0;    Y = 2526; TokenId = 'min-grenade';          Label = 'Grenade (the misnamed Mine_Dud element - MI-Q6)' },
        @{ Element = 1134878; Sprite = 0; X = 2677; Y = 562;  TokenId = 'min-mine-facedown';    Label = 'Mine, facedown back' },
        @{ Element = 1134879; Sprite = 1; X = 3103; Y = 562;  TokenId = 'min-mine-blast';       Label = 'Mine (blast)' },
        @{ Element = 1134878; Sprite = 1; X = 3543; Y = 562;  TokenId = 'min-mine-dud';         Label = 'Mine (dud)' }
    )

    # The shared facedown back is what the per-seat mask depends on, so it is proved rather than
    # assumed: if the two mine elements stop agreeing on their first sprite, the piece this script
    # calls a back is not one.
    $backs = @()
    foreach ($id in @(1134878, 1134879)) {
        $found = @($manifest | Where-Object { $_.elementId -eq $id })
        if ($found.Count -ne 1) {
            throw "The manifest holds $($found.Count) elements with id $id; one was expected."
        }
        $first = @($found[0].atlases[0].sprites)[0]
        $backs += "$($first.x),$($first.y),$($first.width),$($first.height)"
    }

    $backs = @($backs | Sort-Object -Unique)
    if ($backs.Count -ne 1) {
        throw ("The two mine elements declare different first sprites ($($backs -join '; ')), so " +
            'there is no shared facedown back. Nothing was written.')
    }

    Write-Output "Declared: shared facedown mine back at $($backs[0])."

    Add-Type -AssemblyName System.Drawing
    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }

    $written = 0
    $matched = 0
    foreach ($token in $minTokens) {
        $found = @($manifest | Where-Object { $_.elementId -eq $token.Element })
        if ($found.Count -ne 1) {
            throw "The manifest holds $($found.Count) elements with id $($token.Element); one was expected."
        }

        $element = $found[0]
        if ($element.elementType -ne 'Tokens') {
            throw "Element $($token.Element) is '$($element.elementType)', not 'Tokens'. Nothing was written."
        }

        $atlas = $element.atlases[0]
        $sprites = @($atlas.sprites)
        if ($sprites.Count -lt ($token.Sprite + 1)) {
            throw "Element $($token.Element) declares $($sprites.Count) sprites; index $($token.Sprite) was expected."
        }

        $rect = $sprites[$token.Sprite]
        if ([int] $rect.x -ne $token.X -or [int] $rect.y -ne $token.Y) {
            throw ("Element $($token.Element) sprite $($token.Sprite) is at ($($rect.x),$($rect.y)), " +
                "not the measured ($($token.X),$($token.Y)). Nothing was written.")
        }

        $atlasIndexMatch = [regex]::Match($atlas.resource.id, '^Image_(?<index>\d+)_Atlas$')
        if (-not $atlasIndexMatch.Success) {
            throw "The declared atlas resource id '$($atlas.resource.id)' is not in the expected form."
        }

        $atlasIndex = $atlasIndexMatch.Groups['index'].Value
        $candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
        if ($candidates.Count -ne 1) {
            throw "Expected exactly one extracted image for atlas '$($atlas.resource.id)'; found $($candidates.Count)."
        }

        $x = [int] $rect.x
        $y = [int] $rect.y
        $w = [int] $rect.width
        $h = [int] $rect.height
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

        $source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
        try {
            if ($source.Width -ne [int] $atlas.width -or $source.Height -ne [int] $atlas.height) {
                throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the " +
                    "manifest declares $($atlas.width)x$($atlas.height). Nothing was written.")
            }

            if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $source.Width -or ($y + $h) -gt $source.Height) {
                throw 'A declared face rectangle falls outside the atlas. Nothing was written.'
            }

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
                Write-Output "WROTE: $($token.Label) -> $target (${w}x${h}, from ($x,$y) of $($atlas.resource.id))."
            } finally { $bitmap.Dispose() }
        } finally { $source.Dispose() }
    }

    Write-Output ("Prepared token faces: $written written, $matched already present. The source atlas " +
        'and the extracted corpus were read only. The written resources are ignored by Git and are ' +
        'never distributed.')
    exit 0
}

# -------------------------------------------------------------------------------------------------
# Milestone 8.75b prep. Ignatia the Mad's ONE prepared face, in its own arm for the reason Min's is:
# her piece is not a family. She prints exactly one token, `ignatia-magma`, and the corpus draws it
# with exactly one picture.
#
# WHAT THE ELEMENT IS, read from the manifest and LOOKED AT rather than inferred from its name:
# `Magma` 1401024 (publicElementId 737031, `"elementType":"Tokens"`), `Image_3_Atlas` 4096x4096, and
# it declares THREE rectangles like every other token element - but its first TWO are the SAME
# rectangle, (2250,562,413x357), and only the third (2665,562,10x357) is the thin edge strip. So this
# piece is SINGLE-SIDED: both printed faces are one picture, where Tali's family is one piece with
# two different faces and Min's mines are two pieces sharing a back. The assertion below states that
# outright, so a corpus that starts drawing a second magma face stops here rather than having its
# back silently written as its front.
#
# THE PICTURE IS THE RULE. It is a lava hex - molten red with yellow cracks - overprinted with a
# white crossed-circle "no entry" sign, which is the corpus itself drawing ID-Q1's settled reading:
# "All tokens are obstacles... unless specified units cannot enter them". Nothing about that reading
# is taken FROM the picture - `IgnatiaMagmaContent` derives it from p.19 and the printed faces - but
# it is worth recording that the two agree, because a magma token rendered as an unlabelled grey disc
# would have hidden the one property that makes her Green line playable.
#
# Her piece is NOT 413x357 by accident either: Min's hexes are 320x278-ish and Tali's are their own
# size again, so nothing here assumes a square or a shared token footprint. The width and height are
# read from the manifest and only the ORIGIN is asserted, exactly as Min's arm does it.
# -------------------------------------------------------------------------------------------------
if ($Hero -eq 'ignatia') {
    $projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $corpusRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
    $targetRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')

    if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
        Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
        exit 0
    }

    $manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
    if ($manifests.Count -ne 1) {
        throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json

    $ignatiaTokens = @(
        @{ Element = 1401024; Sprite = 0; X = 2250; Y = 562; TokenId = 'ignatia-magma'; Label = 'Magma' }
    )

    Add-Type -AssemblyName System.Drawing
    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }

    $written = 0
    $matched = 0
    foreach ($token in $ignatiaTokens) {
        $found = @($manifest | Where-Object { $_.elementId -eq $token.Element })
        if ($found.Count -ne 1) {
            throw "The manifest holds $($found.Count) elements with id $($token.Element); one was expected."
        }

        $element = $found[0]
        if ($element.elementType -ne 'Tokens') {
            throw "Element $($token.Element) is '$($element.elementType)', not 'Tokens'. Nothing was written."
        }

        $atlas = $element.atlases[0]
        $sprites = @($atlas.sprites)
        if ($sprites.Count -ne 3) {
            throw ("Element $($token.Element) declares $($sprites.Count) sprites; three were " +
                'expected (front, back, edge strip). Nothing was written.')
        }

        # The single-sided claim, proved rather than assumed: see the note above this arm.
        if ([int] $sprites[0].x -ne [int] $sprites[1].x -or
            [int] $sprites[0].y -ne [int] $sprites[1].y) {
            throw ("Element $($token.Element) declares two DIFFERENT printed faces at " +
                "($($sprites[0].x),$($sprites[0].y)) and ($($sprites[1].x),$($sprites[1].y)), so it " +
                'is not the single-sided piece this mapping depends on. Nothing was written.')
        }

        $rect = $sprites[$token.Sprite]
        if ([int] $rect.x -ne $token.X -or [int] $rect.y -ne $token.Y) {
            throw ("Element $($token.Element) sprite $($token.Sprite) is at ($($rect.x),$($rect.y)), " +
                "not the measured ($($token.X),$($token.Y)). Nothing was written.")
        }

        $atlasIndexMatch = [regex]::Match($atlas.resource.id, '^Image_(?<index>\d+)_Atlas$')
        if (-not $atlasIndexMatch.Success) {
            throw "The declared atlas resource id '$($atlas.resource.id)' is not in the expected form."
        }

        $atlasIndex = $atlasIndexMatch.Groups['index'].Value
        $candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
        if ($candidates.Count -ne 1) {
            throw "Expected exactly one extracted image for atlas '$($atlas.resource.id)'; found $($candidates.Count)."
        }

        $x = [int] $rect.x
        $y = [int] $rect.y
        $w = [int] $rect.width
        $h = [int] $rect.height
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

        $source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
        try {
            if ($source.Width -ne [int] $atlas.width -or $source.Height -ne [int] $atlas.height) {
                throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the " +
                    "manifest declares $($atlas.width)x$($atlas.height). Nothing was written.")
            }

            if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $source.Width -or ($y + $h) -gt $source.Height) {
                throw 'A declared face rectangle falls outside the atlas. Nothing was written.'
            }

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
                Write-Output "WROTE: $($token.Label) -> $target (${w}x${h}, from ($x,$y) of $($atlas.resource.id))."
            } finally { $bitmap.Dispose() }
        } finally { $source.Dispose() }
    }

    Write-Output ("Prepared token faces: $written written, $matched already present. The source atlas " +
        'and the extracted corpus were read only. The written resources are ignored by Git and are ' +
        'never distributed.')
    exit 0
}

# -------------------------------------------------------------------------------------------------
# Milestone 8.112g. Mrak the Rockshaper's ONE token, the rock, on Ignatia's terms. The manifest's
# `Rockkk` element 7355742 (publicElementId 4609784, Tokens, `Image_0_Atlas`) declares two face
# rectangles side by side, (3131,2233) and (3470,2233), 337x289 each, and a 10-pixel edge strip.
# Unlike Ignatia's magma the two faces are two POSITIONS - but cropped and compared by eye they print
# the SAME picture (two grey standing stones on a grass hex), so the first is the face and nothing
# about the rule is read from it: MrakComponentContent defines the obstacle from p.19.
# -------------------------------------------------------------------------------------------------
if ($Hero -eq 'mrak') {
    $projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $corpusRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
    $targetRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')

    if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
        Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
        exit 0
    }

    $manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
    if ($manifests.Count -ne 1) {
        throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json

    $mrakTokens = @(
        @{ Element = 7355742; Sprite = 0; X = 3131; Y = 2233; TokenId = 'mrak-rock'; Label = 'Rock' }
    )

    Add-Type -AssemblyName System.Drawing
    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }

    $written = 0
    $matched = 0
    foreach ($token in $mrakTokens) {
        $found = @($manifest | Where-Object { $_.elementId -eq $token.Element })
        if ($found.Count -ne 1) {
            throw "The manifest holds $($found.Count) elements with id $($token.Element); one was expected."
        }

        $element = $found[0]
        if ($element.elementType -ne 'Tokens') {
            throw "Element $($token.Element) is '$($element.elementType)', not 'Tokens'. Nothing was written."
        }

        $atlas = $element.atlases[0]
        $sprites = @($atlas.sprites)
        if ($sprites.Count -ne 3) {
            throw ("Element $($token.Element) declares $($sprites.Count) sprites; three were " +
                'expected (front, back, edge strip). Nothing was written.')
        }

        # Two face positions side by side on one row, printing one picture: see the note above this
        # arm. The shape is checked here; the pictures were compared by eye at 8.112g.
        if ([int] $sprites[0].y -ne [int] $sprites[1].y -or
            [int] $sprites[0].x -eq [int] $sprites[1].x) {
            throw ("Element $($token.Element) no longer declares its two faces side by side on one " +
                'row. Nothing was written.')
        }

        $rect = $sprites[$token.Sprite]
        if ([int] $rect.x -ne $token.X -or [int] $rect.y -ne $token.Y) {
            throw ("Element $($token.Element) sprite $($token.Sprite) is at ($($rect.x),$($rect.y)), " +
                "not the measured ($($token.X),$($token.Y)). Nothing was written.")
        }

        $atlasIndexMatch = [regex]::Match($atlas.resource.id, '^Image_(?<index>\d+)_Atlas$')
        if (-not $atlasIndexMatch.Success) {
            throw "The declared atlas resource id '$($atlas.resource.id)' is not in the expected form."
        }

        $atlasIndex = $atlasIndexMatch.Groups['index'].Value
        $candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
        if ($candidates.Count -ne 1) {
            throw "Expected exactly one extracted image for atlas '$($atlas.resource.id)'; found $($candidates.Count)."
        }

        $x = [int] $rect.x
        $y = [int] $rect.y
        $w = [int] $rect.width
        $h = [int] $rect.height
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

        $source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
        try {
            if ($source.Width -ne [int] $atlas.width -or $source.Height -ne [int] $atlas.height) {
                throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the " +
                    "manifest declares $($atlas.width)x$($atlas.height). Nothing was written.")
            }

            if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $source.Width -or ($y + $h) -gt $source.Height) {
                throw 'A declared face rectangle falls outside the atlas. Nothing was written.'
            }

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
                Write-Output "WROTE: $($token.Label) -> $target (${w}x${h}, from ($x,$y) of $($atlas.resource.id))."
            } finally { $bitmap.Dispose() }
        } finally { $source.Dispose() }
    }

    Write-Output ("Prepared token faces: $written written, $matched already present. The source atlas " +
        'and the extracted corpus were read only. The written resources are ignored by Git and are ' +
        'never distributed.')
    exit 0
}

# -------------------------------------------------------------------------------------------------
# Milestone 8.114f. Wuk the Grove Keeper's ONE token, the Tree (WU-Q1: three in supply). The manifest
# declares three `Sapling` elements - 1421886, 1421885, 1421884 (Tokens, `Image_0_Atlas`, 335x291 at
# y=2526 and x=1372/1035/686) - each printing a DIFFERENT tree, and each declaring the same rectangle
# for both faces. The rule is one token definition (WukComponentContent), so one face is prepared:
# `Sapling`, the first. Nothing about the rule is read from the picture.
# -------------------------------------------------------------------------------------------------
if ($Hero -eq 'wuk') {
    $projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $corpusRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
    $targetRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')

    if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
        Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
        exit 0
    }

    $manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
    if ($manifests.Count -ne 1) {
        throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json

    $mrakTokens = @(
        @{ Element = 1421886; Sprite = 0; X = 1372; Y = 2526; TokenId = 'wuk-tree'; Label = 'Tree' }
    )

    Add-Type -AssemblyName System.Drawing
    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }

    $written = 0
    $matched = 0
    foreach ($token in $mrakTokens) {
        $found = @($manifest | Where-Object { $_.elementId -eq $token.Element })
        if ($found.Count -ne 1) {
            throw "The manifest holds $($found.Count) elements with id $($token.Element); one was expected."
        }

        $element = $found[0]
        if ($element.elementType -ne 'Tokens') {
            throw "Element $($token.Element) is '$($element.elementType)', not 'Tokens'. Nothing was written."
        }

        $atlas = $element.atlases[0]
        $sprites = @($atlas.sprites)
        if ($sprites.Count -ne 3) {
            throw ("Element $($token.Element) declares $($sprites.Count) sprites; three were " +
                'expected (front, back, edge strip). Nothing was written.')
        }

        # One face position declared twice: front and back print the same tree. See the note above.
        if ([int] $sprites[0].x -ne [int] $sprites[1].x -or [int] $sprites[0].y -ne [int] $sprites[1].y) {
            throw ("Element $($token.Element) no longer declares one face for both sides. " +
                'Nothing was written.')
        }

        $rect = $sprites[$token.Sprite]
        if ([int] $rect.x -ne $token.X -or [int] $rect.y -ne $token.Y) {
            throw ("Element $($token.Element) sprite $($token.Sprite) is at ($($rect.x),$($rect.y)), " +
                "not the measured ($($token.X),$($token.Y)). Nothing was written.")
        }

        $atlasIndexMatch = [regex]::Match($atlas.resource.id, '^Image_(?<index>\d+)_Atlas$')
        if (-not $atlasIndexMatch.Success) {
            throw "The declared atlas resource id '$($atlas.resource.id)' is not in the expected form."
        }

        $atlasIndex = $atlasIndexMatch.Groups['index'].Value
        $candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
        if ($candidates.Count -ne 1) {
            throw "Expected exactly one extracted image for atlas '$($atlas.resource.id)'; found $($candidates.Count)."
        }

        $x = [int] $rect.x
        $y = [int] $rect.y
        $w = [int] $rect.width
        $h = [int] $rect.height
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

        $source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
        try {
            if ($source.Width -ne [int] $atlas.width -or $source.Height -ne [int] $atlas.height) {
                throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the " +
                    "manifest declares $($atlas.width)x$($atlas.height). Nothing was written.")
            }

            if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $source.Width -or ($y + $h) -gt $source.Height) {
                throw 'A declared face rectangle falls outside the atlas. Nothing was written.'
            }

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
                Write-Output "WROTE: $($token.Label) -> $target (${w}x${h}, from ($x,$y) of $($atlas.resource.id))."
            } finally { $bitmap.Dispose() }
        } finally { $source.Dispose() }
    }

    Write-Output ("Prepared token faces: $written written, $matched already present. The source atlas " +
        'and the extracted corpus were read only. The written resources are ignored by Git and are ' +
        'never distributed.')
    exit 0
}

# -------------------------------------------------------------------------------------------------
# Milestone 9.8f. Mortimer the Awakener's ONE token, the Zombie (MO-Q1: four in supply). The manifest
# declares one `Zombieeee` element, 7694669 (Tokens, `Image_3_Atlas`, 427x369 at y=1043), whose two
# faces sit at x=441 and x=870 and print the SAME art - a Zombie's hand rising from a grave - which was
# cropped and looked at on 2026-09-29. The rule is one token definition (MortimerComponentContent), so
# one face is prepared: the front. Nothing about the rule is read from the picture.
# -------------------------------------------------------------------------------------------------
if ($Hero -eq 'mortimer') {
    $projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $corpusRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
    $targetRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')

    if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
        Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
        exit 0
    }

    $manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
    if ($manifests.Count -ne 1) {
        throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json

    $mrakTokens = @(
        @{ Element = 7694669; Sprite = 0; X = 441; Y = 1043; TokenId = 'mortimer-zombie'; Label = 'Zombie' }
    )

    Add-Type -AssemblyName System.Drawing
    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }

    $written = 0
    $matched = 0
    foreach ($token in $mrakTokens) {
        $found = @($manifest | Where-Object { $_.elementId -eq $token.Element })
        if ($found.Count -ne 1) {
            throw "The manifest holds $($found.Count) elements with id $($token.Element); one was expected."
        }

        $element = $found[0]
        if ($element.elementType -ne 'Tokens') {
            throw "Element $($token.Element) is '$($element.elementType)', not 'Tokens'. Nothing was written."
        }

        $atlas = $element.atlases[0]
        $sprites = @($atlas.sprites)
        if ($sprites.Count -ne 3) {
            throw ("Element $($token.Element) declares $($sprites.Count) sprites; three were " +
                'expected (front, back, edge strip). Nothing was written.')
        }

        # Two face positions that print the same art; the front is prepared. See the note above.
        if ([int] $sprites[1].x -ne 870 -or [int] $sprites[1].y -ne 1043) {
            throw ("Element $($token.Element) no longer declares its back face where it was inspected. " +
                'Nothing was written.')
        }

        $rect = $sprites[$token.Sprite]
        if ([int] $rect.x -ne $token.X -or [int] $rect.y -ne $token.Y) {
            throw ("Element $($token.Element) sprite $($token.Sprite) is at ($($rect.x),$($rect.y)), " +
                "not the measured ($($token.X),$($token.Y)). Nothing was written.")
        }

        $atlasIndexMatch = [regex]::Match($atlas.resource.id, '^Image_(?<index>\d+)_Atlas$')
        if (-not $atlasIndexMatch.Success) {
            throw "The declared atlas resource id '$($atlas.resource.id)' is not in the expected form."
        }

        $atlasIndex = $atlasIndexMatch.Groups['index'].Value
        $candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
        if ($candidates.Count -ne 1) {
            throw "Expected exactly one extracted image for atlas '$($atlas.resource.id)'; found $($candidates.Count)."
        }

        $x = [int] $rect.x
        $y = [int] $rect.y
        $w = [int] $rect.width
        $h = [int] $rect.height
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

        $source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
        try {
            if ($source.Width -ne [int] $atlas.width -or $source.Height -ne [int] $atlas.height) {
                throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the " +
                    "manifest declares $($atlas.width)x$($atlas.height). Nothing was written.")
            }

            if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $source.Width -or ($y + $h) -gt $source.Height) {
                throw 'A declared face rectangle falls outside the atlas. Nothing was written.'
            }

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
                Write-Output "WROTE: $($token.Label) -> $target (${w}x${h}, from ($x,$y) of $($atlas.resource.id))."
            } finally { $bitmap.Dispose() }
        } finally { $source.Dispose() }
    }

    Write-Output ("Prepared token faces: $written written, $matched already present. The source atlas " +
        'and the extracted corpus were read only. The written resources are ignored by Git and are ' +
        'never distributed.')
    exit 0
}

# -------------------------------------------------------------------------------------------------
# Milestone 9.30e. Gydion the Archwizard's ONE token, the Familiar (GY-Q5: one in supply). The manifest
# declares one `raven` element, 5831117 (Tokens, `Image_0_Atlas`, 335x291 at (2058,2526)), whose two
# face sprites are the SAME rectangle - a white owl holding a book - which was cropped and looked at on
# 2026-10-01 (GY-03). The rule is one token definition (GydionComponentContent), so one face is
# prepared, with no caption over it. Nothing about the rule is read from the picture.
# -------------------------------------------------------------------------------------------------
if ($Hero -eq 'gydion') {
    $projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $corpusRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards', 'extracted', 'text-assets')
    $targetRoot = [System.IO.Path]::Combine(
        $projectRoot, 'Assets', 'Resources', 'Private', 'Tokens')

    if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
        Write-Output "The private Tabletopia corpus is absent: $corpusRoot. Nothing was written."
        exit 0
    }

    $manifests = @(Get-ChildItem -LiteralPath $corpusRoot -Filter 'manifest-bundled__*.json' -File)
    if ($manifests.Count -ne 1) {
        throw "Expected exactly one bundled manifest under $corpusRoot; found $($manifests.Count)."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifests[0].FullName | ConvertFrom-Json

    $gydionTokens = @(
        @{ Element = 5831117; Sprite = 0; X = 2058; Y = 2526; TokenId = 'gydion-familiar'; Label = 'Familiar' }
    )

    Add-Type -AssemblyName System.Drawing
    if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }

    $written = 0
    $matched = 0
    foreach ($token in $gydionTokens) {
        $found = @($manifest | Where-Object { $_.elementId -eq $token.Element })
        if ($found.Count -ne 1) {
            throw "The manifest holds $($found.Count) elements with id $($token.Element); one was expected."
        }

        $element = $found[0]
        if ($element.elementType -ne 'Tokens') {
            throw "Element $($token.Element) is '$($element.elementType)', not 'Tokens'. Nothing was written."
        }

        $atlas = $element.atlases[0]
        $sprites = @($atlas.sprites)
        if ($sprites.Count -ne 3) {
            throw ("Element $($token.Element) declares $($sprites.Count) sprites; three were " +
                'expected (front, back, edge strip). Nothing was written.')
        }

        # Both faces are the one rectangle; the front is prepared. See the note above.
        if ([int] $sprites[1].x -ne 2058 -or [int] $sprites[1].y -ne 2526) {
            throw ("Element $($token.Element) no longer declares its back face where it was inspected. " +
                'Nothing was written.')
        }

        $rect = $sprites[$token.Sprite]
        if ([int] $rect.x -ne $token.X -or [int] $rect.y -ne $token.Y) {
            throw ("Element $($token.Element) sprite $($token.Sprite) is at ($($rect.x),$($rect.y)), " +
                "not the measured ($($token.X),$($token.Y)). Nothing was written.")
        }

        $atlasIndexMatch = [regex]::Match($atlas.resource.id, '^Image_(?<index>\d+)_Atlas$')
        if (-not $atlasIndexMatch.Success) {
            throw "The declared atlas resource id '$($atlas.resource.id)' is not in the expected form."
        }

        $atlasIndex = $atlasIndexMatch.Groups['index'].Value
        $candidates = @(Get-ChildItem -LiteralPath $corpusRoot -Filter "*.sprite$atlasIndex`__*.jpg" -File)
        if ($candidates.Count -ne 1) {
            throw "Expected exactly one extracted image for atlas '$($atlas.resource.id)'; found $($candidates.Count)."
        }

        $x = [int] $rect.x
        $y = [int] $rect.y
        $w = [int] $rect.width
        $h = [int] $rect.height
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

        $source = [System.Drawing.Image]::FromFile($candidates[0].FullName)
        try {
            if ($source.Width -ne [int] $atlas.width -or $source.Height -ne [int] $atlas.height) {
                throw ("Atlas '$($candidates[0].Name)' is $($source.Width)x$($source.Height), but the " +
                    "manifest declares $($atlas.width)x$($atlas.height). Nothing was written.")
            }

            if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $source.Width -or ($y + $h) -gt $source.Height) {
                throw 'A declared face rectangle falls outside the atlas. Nothing was written.'
            }

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
                Write-Output "WROTE: $($token.Label) -> $target (${w}x${h}, from ($x,$y) of $($atlas.resource.id))."
            } finally { $bitmap.Dispose() }
        } finally { $source.Dispose() }
    }

    Write-Output ("Prepared token faces: $written written, $matched already present. The source atlas " +
        'and the extracted corpus were read only. The written resources are ignored by Git and are ' +
        'never distributed.')
    exit 0
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
            @{ Face = 'totem'; TokenId = 'tali-totem-ancestral'; Label = 'Ancestral Totem' },
            @{ Face = 'totem'; TokenId = 'tali-totem-venerated'; Label = 'Venerated Totem' }
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
