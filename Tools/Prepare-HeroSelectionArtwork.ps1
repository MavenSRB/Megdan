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
    # Byte-identical to active-hero-cards/_selection-cards/Widget__front.png.
    widget = 'randomizers-tt25__7723701__front.png'
    # Milestone 8.57g. SHA-256 matched rather than eyeballed, on the measurement Cutter's and Bain's
    # lines above record: this front is byte-identical to the corpus's own
    # `active-hero-cards/_selection-cards/Rowenna__front.png`
    # (2595D15D122227E1C50D2CB1BF618064DD74CED40E72F736CF62211D9002B2D9), so the face is hers by the
    # extraction's own naming and not by a reading of the picture.
    rowenna = 'randomizers-tt28__7723702__front.png'
    # Milestone 8.61j. SHA-256 matched rather than eyeballed, on the measurement Cutter's, Bain's and
    # Rowenna's lines above record: this front is byte-identical to the corpus's own
    # `active-hero-cards/_selection-cards/Min__front.png`
    # (83E8B26DB9C01111C74623E980C22FBB9751874D94DB64EFBA6B79E1CE86CE6B), so the face is hers by the
    # extraction's own naming and not by a reading of the picture.
    min = 'randomizers-tt8__7729915__front.png'
    # Milestone 8.69g. SHA-256 matched rather than eyeballed, on the measurement Cutter's, Bain's,
    # Rowenna's and Min's lines above record: this front is byte-identical to the corpus's own
    # `active-hero-cards/_selection-cards/Brynn__front.png`
    # (1E9BFCB798492DD134A810C88213119C8DBA6C03171C14C6BBFD05F6F53ABAD3), so the face is his by the
    # extraction's own naming and not by a reading of the picture.
    brynn = 'randomizers-tt23__7723707__front.png'
    # Milestone 8.71h. SHA-256 matched rather than eyeballed, on the measurement Cutter's, Bain's,
    # Rowenna's, Min's and Brynn's lines above record: this front is byte-identical to the corpus's
    # own `active-hero-cards/_selection-cards/Hanu__front.png`
    # (F300A8B894B85EBBAEC64C0018EC993C06734B6AC2B0479CF185029F93861C17), so the face is his by the
    # extraction's own naming and not by a reading of the picture. It is the front the three stars
    # and the Tactician/Sniper/Pusher icons were read off for his roster row.
    hanu = 'randomizers-tt10__7723692__front.png'
    # Milestone 8.75b prep. SHA-256 matched rather than eyeballed, on the measurement Cutter's,
    # Bain's, Rowenna's, Min's, Brynn's and Hanu's lines above record: this front is byte-identical
    # to the corpus's own `active-hero-cards/_selection-cards/Ignatia__front.png`
    # (1D4C0B4B694F19A79101EA19E7F28C2B7DB6E02BD935BDCFF3363F54C699FC57), so the face is hers by the
    # extraction's own naming and not by a reading of the picture. It is the front the FOUR stars -
    # the first four-star hero this build prepares - and the Sniper/Damager/Tokens/Tactician/Pusher
    # icons were read off for her roster row.
    #
    # THIS ROW LANDS BEFORE HER ROSTER ROW DOES, which is a first for this file: her artwork is
    # prepared by its own block ahead of the dealing. Until that dealing,
    # `ConfiguredCardArtworkCatalogTests
    # .EveryHeroTheSelectorOffers_HasAPreparedHoverFrontUnderTheKeyTheRuntimeComposes` reports this
    # row as one declared hero the selector does not offer, which is the guard working rather than
    # failing.
    ignatia = 'randomizers-tt12__7729913__front.png'
    # SHA-256-identical to the private named Trinkets selection front.
    trinkets = 'randomizers-tt16__7724782__front.png'
    # SHA-256-identical to active-hero-cards/_selection-cards/Snorri__front.png.
    snorri = 'randomizers-tt31__7723704__front.png'
    # Milestone 8.112g. SHA-256-identical to active-hero-cards/_selection-cards/Mrak__front.png
    # (1f478496...c9, the profile hash recorded in HERO_MRAK.md).
    mrak = 'randomizers-tt29__7723696__front.png'
    # Milestone 8.114f. SHA-256-identical to active-hero-cards/_selection-cards/Wuk__front.png
    # (3231da35...23): the front his three stars and six role icons were read off (inventory 8.114).
    wuk = 'randomizers-tt11__7723684__front.png'
    # Milestone 9.8f. SHA-256-identical to active-hero-cards/_selection-cards/Mortimer__front.png
    # (51853D90...AF0): the front his three stars and five role icons were read off (inventory 9.8).
    mortimer = 'randomizers-tt24__7723681__front.png'
    # Milestone 9.21j. SHA-256-identical to active-hero-cards/_selection-cards/Razzle__front.png
    # (d526f6a3...5c25, RZ-01): the front his four stars and the Tactician and Melee icons were read
    # off (inventory 9.21).
    razzle = 'randomizers-tt30__7723698__front.png'
    # Milestone 9.30i. SHA-256-identical to active-hero-cards/_selection-cards/Gydion__front.png
    # (d07b3b61...5420, GY-01): the front his four stars and the Sniper, Tactician, Farming, Damager,
    # Pusher and Tokens icons were read off (inventory 9.30).
    gydion = 'randomizers-tt32__7723694__front.png'
    # Milestone 9.32h. SHA-256-identical to active-hero-cards/_selection-cards/Takahide__front.png
    # (8ff7a6df...634d, inventory 9.32): the front his four stars and the Durable, Sniper, Tactician,
    # Damager and Farming icons were read off.
    takahide = 'randomizers-tt26__7723697__front.png'
    # SHA-256-identical to active-hero-cards/_selection-cards/Emmitt__front.png
    # (E843CFC143EE489F7AC7DF9EE91548D09D114BDE58F342AA067D8C7659344CBE; EM-01).
    emmitt = 'randomizers-tt27__7723686__front.png'
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
