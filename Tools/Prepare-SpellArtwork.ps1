<#
.SYNOPSIS
    Prepares every catalogued private spell front and spell back as an ignored Unity resource.

.DESCRIPTION
    Milestone 9.30h. The tracked JSON catalog (Assets/Resources/UI/ConfiguredCardArtworkCatalog.json)
    names each spell definition's front in its `spells` array and the four spell backs the table
    sees (Tier I - which the basic spells share - II, III and Wish's IV) in its `spellBacks` array.
    This command validates both arrays before writing anything, reads the existing ignored
    Tabletopia corpus without mutating it, and copies the verified 372x260 landscape scans beneath:

        Assets/Resources/Private/Spells/<definition-id>.png
        Assets/Resources/Private/Spells/Backs/<I|II|III|IV>.png

    The target is ignored by Git, and the generated spell face and back remain the runtime fallback
    when it is absent. The command does not invoke Git, Unity, a build, a network client, or an
    uploader. Prepare-CardArtwork.ps1 owns the action-card rows (`entries`) and ignores these.

.PARAMETER Check
    Read-only verification: every prepared resource exists, is 372x260, and is byte-identical to
    its source.

.PARAMETER Force
    Replace the prepared set even when every resource already matches its source.

.PARAMETER CorpusRoot
    The private Tabletopia corpus to read. Defaults to this checkout's own ignored
    `Docs/Guards of Atlantis II - Tabletopia Cards`; a git worktree, which has none, names the main
    checkout's corpus here (read only).
#>
[CmdletBinding()]
param(
    [switch] $Check,
    [switch] $Force,
    [string] $CorpusRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$catalogPath = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'UI', 'ConfiguredCardArtworkCatalog.json')
$corpusRoot = if ([string]::IsNullOrWhiteSpace($CorpusRoot)) {
    [System.IO.Path]::Combine($projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards')
} else {
    [System.IO.Path]::GetFullPath($CorpusRoot)
}
$targetRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private', 'Spells')
$backNames = @{ 1 = 'I'; 2 = 'II'; 3 = 'III'; 4 = 'IV' }

function Assert-PathUnder([string] $path, [string] $root, [string] $label) {
    $fullPath = [System.IO.Path]::GetFullPath($path)
    $fullRoot = [System.IO.Path]::GetFullPath($root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $prefix = $fullRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$label resolved outside its declared root: '$fullPath'."
    }

    return $fullPath
}

function Get-PngSize([string] $path) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 24 -or
        $bytes[0] -ne 0x89 -or $bytes[1] -ne 0x50 -or $bytes[2] -ne 0x4E -or
        $bytes[3] -ne 0x47) {
        return $null
    }

    $width = ([int] $bytes[16] -shl 24) -bor ([int] $bytes[17] -shl 16) -bor
        ([int] $bytes[18] -shl 8) -bor [int] $bytes[19]
    $height = ([int] $bytes[20] -shl 24) -bor ([int] $bytes[21] -shl 16) -bor
        ([int] $bytes[22] -shl 8) -bor [int] $bytes[23]
    return [pscustomobject]@{ Width = $width; Height = $height }
}

if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
    throw "The tracked configured card artwork catalog is absent."
}

if (-not (Test-Path -LiteralPath $corpusRoot -PathType Container)) {
    throw "The private Tabletopia card corpus is absent; no prepared image was written."
}

$catalog = Get-Content -Raw -LiteralPath $catalogPath | ConvertFrom-Json
$errors = New-Object 'System.Collections.Generic.List[string]'
$inventory = New-Object 'System.Collections.Generic.List[object]'
$sourcePattern = '^cards/other-cards/spells-tt(?:[2-9]|1[0-9]|2[0-2])?__\d+__(front|back)\.png$'

function Add-Item([string] $label, [string] $sourcePath, [string] $side, [string] $relative) {
    if ($sourcePath -cnotmatch $sourcePattern -or $Matches[1] -cne $side) {
        $errors.Add("$label is not a spell $side scan under cards/other-cards/: '$sourcePath'.")
        return
    }

    $sourceCandidate = [System.IO.Path]::Combine(
        $corpusRoot, $sourcePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $source = Assert-PathUnder $sourceCandidate $corpusRoot 'A spell source'
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        $errors.Add("$label is absent from the private corpus: '$sourcePath'.")
        return
    }

    $size = Get-PngSize $source
    if ($null -eq $size -or $size.Width -ne 372 -or $size.Height -ne 260) {
        $found = if ($null -eq $size) { 'not a PNG' } else { "$($size.Width)x$($size.Height)" }
        $errors.Add("$label is $found; the verified spell scan is 372x260.")
        return
    }

    $destinationCandidate = [System.IO.Path]::Combine(
        $targetRoot, $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar) + '.png')
    $destination = Assert-PathUnder $destinationCandidate $targetRoot 'A prepared spell resource'
    $inventory.Add([pscustomobject]@{
        Label = $label
        SourcePath = $sourcePath
        Source = $source
        Destination = $destination
        ResourcePath = "Private/Spells/$relative"
    })
}

if ($catalog.version -ne 1 -or
    $null -eq $catalog.PSObject.Properties['spells'] -or
    $null -eq $catalog.PSObject.Properties['spellBacks']) {
    $errors.Add('The catalog must be format version 1 and contain spells and spellBacks arrays.')
}
else {
    $spells = @($catalog.spells)
    $backs = @($catalog.spellBacks)
    if ($spells.Count -ne 22) {
        $errors.Add("Expected 22 spell fronts; found $($spells.Count).")
    }

    if ($backs.Count -ne 4) {
        $errors.Add("Expected 4 spell backs; found $($backs.Count).")
    }

    foreach ($group in @(@($spells | ForEach-Object { $_.sourcePath }) + @($backs | ForEach-Object { $_.sourcePath }) |
        Group-Object | Where-Object { $_.Count -gt 1 })) {
        $errors.Add("Source '$($group.Name)' appears $($group.Count) times.")
    }

    foreach ($spell in $spells) {
        $definitionId = [string] $spell.definitionId
        if ($definitionId -cnotmatch '^[a-z0-9]+(-[a-z0-9]+)+$') {
            $errors.Add("Spell row '$definitionId' does not name a definition id.")
            continue
        }

        Add-Item "Spell '$definitionId'" ([string] $spell.sourcePath) 'front' $definitionId
    }

    foreach ($back in $backs) {
        $tier = [int] $back.back
        if (-not $backNames.ContainsKey($tier)) {
            $errors.Add("Spell back '$tier' is not Tier I to IV.")
            continue
        }

        Add-Item "Spell back $($backNames[$tier])" ([string] $back.sourcePath) 'back' "Backs/$($backNames[$tier])"
    }
}

if ($errors.Count -gt 0) {
    throw ("Spell artwork inventory failed before any prepared image was written:`n - " +
        ($errors -join "`n - "))
}

Write-Output "PASS: the tracked catalog resolves $($inventory.Count) unique 372x260 spell scans (22 fronts, 4 backs)."

function Test-PreparedSet([switch] $ReportErrors) {
    $preparedErrors = New-Object 'System.Collections.Generic.List[string]'
    foreach ($item in $inventory) {
        if (-not (Test-Path -LiteralPath $item.Destination -PathType Leaf)) {
            $preparedErrors.Add("Prepared resource '$($item.ResourcePath)' is absent.")
            continue
        }

        $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item.Source).Hash
        $preparedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item.Destination).Hash
        if ($sourceHash -cne $preparedHash) {
            $preparedErrors.Add("Prepared resource '$($item.ResourcePath)' differs from its source.")
        }
    }

    if ($ReportErrors -and $preparedErrors.Count -gt 0) {
        throw ("Prepared spell artwork verification failed:`n - " + ($preparedErrors -join "`n - "))
    }

    return $preparedErrors.Count -eq 0
}

if ($Check) {
    [void] (Test-PreparedSet -ReportErrors)
    Write-Output "PASS: all $($inventory.Count) prepared spell resources are present and byte-identical to their private sources."
    exit 0
}

if (-not $Force -and (Test-PreparedSet)) {
    Write-Output "SKIP: all $($inventory.Count) prepared spell resources already match. Pass -Force to replace them."
    exit 0
}

foreach ($item in $inventory) {
    $parent = Split-Path -Parent $item.Destination
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    # Copy-Item replaces the file and leaves any Unity .meta beside it untouched.
    Copy-Item -LiteralPath $item.Source -Destination $item.Destination -Force
}

[void] (Test-PreparedSet -ReportErrors)
Write-Output "WROTE: $($inventory.Count) ignored Unity spell resources under Assets/Resources/Private/Spells. The private corpus was read only; no pixel was Git-staged, uploaded, or distributed."
