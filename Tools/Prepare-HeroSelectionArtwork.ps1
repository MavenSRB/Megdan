param([switch]$Check)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceRoot = Join-Path $projectRoot 'Docs/Guards of Atlantis II - Tabletopia Cards/cards/hero-selection'
$targetRoot = Join-Path $projectRoot 'Assets/Resources/Private/HeroSelection'
# Visually verified source fronts. This maps presentation assets, never gameplay rules.
$sources = [ordered]@{
    arien = 'randomizers-tt2__7723682__front.png'
    brogan = 'randomizers-tt__7723680__front.png'
    sabina = 'randomizers-tt3__7723679__front.png'
    wasp = 'randomizers-tt4__7723678__front.png'
    tigerclaw = 'randomizers-tt6__7723677__front.png'
    xargatha = 'randomizers-tt7__7723699__front.png'
    dodger = 'randomizers-tt5__7723700__front.png'
    misa = 'randomizers-tt21__7723705__front.png'
    silverarrow = 'randomizers-tt19__7723687__front.png'
    ursafar = 'randomizers-tt18__7723703__front.png'
    whisper = 'randomizers-tt20__7723693__front.png'
    # Milestone 8.37. Visually verified like the eleven above: the face prints the name "Tali",
    # three complexity stars (p.21's "Very Complex", the only hero above one star this build deals)
    # and a Tokens role icon, which no other selection face in the set carries.
    tali = 'randomizers-tt22__7723708__front.png'
    # Milestone 8.38e. Visually verified like the twelve above: the face prints the name "SWIFT",
    # three complexity stars - she is the second hero this build deals above one star, beside Tali -
    # and the role icons Sniper, Farming and Tactician, which is a trio no other selection face in
    # the set carries.
    swift = 'randomizers-tt9__7723691__front.png'
    # Milestone 8.39i. SHA-256 matched rather than eyeballed, which is the check the thirteen above
    # record in prose and this line records by measurement: this front is byte-identical to the
    # corpus's own `active-hero-cards/_selection-cards/Cutter__front.png`
    # (0849828287F044B22DE4B4BD8D3B84B38529D28A830966FA4E5C8CE6693244A4), so the face is hers by the
    # extraction's own naming and not by a reading of the picture.
    cutter = 'randomizers-tt15__7723688__front.png'
    # Milestone 8.40j. SHA-256 matched rather than eyeballed, on the measurement Cutter's line above
    # records: this front is byte-identical to the corpus's own
    # `active-hero-cards/_selection-cards/Bain__front.png`
    # (5388D4AFC311CC06292F13487F430F5554EBFB1064DFB4F1DD3CE27694E158B4), so the face is his by the
    # extraction's own naming and not by a reading of the picture.
    bain = 'randomizers-tt14__7723683__front.png'
    # Byte-identical to active-hero-cards/_selection-cards/Garrus__front.png.
    garrus = 'randomizers-tt13__7723690__front.png'
}
foreach ($source in $sources.Values) {
    if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $source) -PathType Leaf)) { throw "Missing source: $source" }
}
if (-not $Check) { New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null }
foreach ($hero in $sources.Keys) {
    $source = Join-Path $sourceRoot $sources[$hero]
    $target = Join-Path $targetRoot ("hero-$hero.png")
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    if (Test-Path -LiteralPath $target) {
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash) { throw "Existing prepared artwork differs; inspect it before replacing: $target" }
    } elseif ($Check) { throw "Missing prepared artwork: $target" }
    else { Copy-Item -LiteralPath $source -Destination $target }
}
"Verified $($sources.Count) byte-identical private hero overview fronts. No source or remote changed."
