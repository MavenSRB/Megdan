<#
.SYNOPSIS
    Prepares every configured private card front as an ignored Unity resource.

.DESCRIPTION
    Milestone 8.25f. The tracked JSON catalog is the one definition-to-source map. This command
    validates its complete configured inventory before writing anything, reads the existing ignored
    Tabletopia corpus without mutating it, and copies the verified fronts beneath:

        Assets/Resources/Private/Cards/<content-id>/<definition-local-id>.png

    The target is ignored by Git and the generated face remains the runtime fallback when it is
    absent. The command does not invoke Git, Unity, a build, a network client, or an uploader.

.PARAMETER Check
    Read-only verification. Validates the private source inventory and proves that every prepared
    resource exists, has the declared 372x520 dimensions, and is byte-identical to its source.

.PARAMETER Force
    Replace the prepared set even when all resources already match their sources.
#>
[CmdletBinding()]
param(
    [switch] $Check,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$catalogPath = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'UI', 'ConfiguredCardArtworkCatalog.json')
$corpusRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards')
$privateRoot = [System.IO.Path]::Combine(
    $projectRoot, 'Assets', 'Resources', 'Private')
$targetRoot = [System.IO.Path]::Combine($privateRoot, 'Cards')
$tmpRoot = [System.IO.Path]::Combine($projectRoot, 'tmp')

$heroFolders = [ordered]@{
    wasp = 'Wasp'
    xargatha = 'Xargatha'
    tigerclaw = 'Tigerclaw'
    arien = 'Arien'
    brogan = 'Brogan'
    sabina = 'Sabina'
    dodger = 'Dodger'
    misa = 'Misa'
    silverarrow = 'SilverArrow'
    ursafar = 'Ursafar'
    whisper = 'Whisper'
    tali = 'Tali'
    swift = 'Swift'
    cutter = 'Cutter'
    bain = 'Bain'
    garrus = 'Garrus'
    widget = 'Widget'
    rowenna = 'Rowenna'
    min = 'Min'
}

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

if ($catalog.version -ne 1 -or $null -eq $catalog.entries) {
    $errors.Add('The catalog must be format version 1 and contain an entries array.')
}
else {
    $entries = @($catalog.entries)
    $expectedCount = $heroFolders.Count * 18
    if ($entries.Count -ne $expectedCount) {
        $errors.Add("Expected $expectedCount configured entries; found $($entries.Count).")
    }

    foreach ($group in @($entries | Group-Object { "$($_.definitionId)@$($_.contentRevision)" } |
        Where-Object { $_.Count -gt 1 })) {
        $errors.Add("Definition '$($group.Name)' appears $($group.Count) times.")
    }

    foreach ($group in @($entries | Group-Object sourcePath | Where-Object { $_.Count -gt 1 })) {
        $errors.Add("Source '$($group.Name)' appears $($group.Count) times.")
    }

    foreach ($entry in $entries) {
        $definitionId = [string] $entry.definitionId
        $revision = [string] $entry.contentRevision
        $sourcePath = [string] $entry.sourcePath
        $stable = "$definitionId@$revision from '$sourcePath'"
        $hero = $null
        foreach ($candidate in $heroFolders.Keys) {
            if ($definitionId.StartsWith(
                    "$candidate-", [System.StringComparison]::Ordinal)) {
                $hero = $candidate
                break
            }
        }

        if ($null -eq $hero) {
            $errors.Add("$stable names an unknown configured hero.")
            continue
        }

        if ([string]::IsNullOrWhiteSpace($revision)) {
            $errors.Add("$stable has no content revision.")
            continue
        }

        $prefix = "active-hero-cards/$($heroFolders[$hero])/"
        if (-not $sourcePath.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
            $errors.Add("$stable is outside the declared hero action-card folder.")
            continue
        }

        $fileName = $sourcePath.Substring($prefix.Length)
        $frontPattern = '^' + [regex]::Escape($hero) + '(?:[2-9]|1[0-8])?__\d+__front\.png$'
        if ($fileName -cnotmatch $frontPattern) {
            $errors.Add("$stable is not one of the eighteen playable __front.png sources.")
            continue
        }

        $sourceCandidate = [System.IO.Path]::Combine(
            $corpusRoot,
            $sourcePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        $source = Assert-PathUnder $sourceCandidate $corpusRoot 'A catalog source'
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            $errors.Add("$stable is absent from the private corpus.")
            continue
        }

        $size = Get-PngSize $source
        if ($null -eq $size -or $size.Width -ne 372 -or $size.Height -ne 520) {
            $found = if ($null -eq $size) { 'not a PNG' } else { "$($size.Width)x$($size.Height)" }
            $errors.Add("$stable is $found; the verified source contract is 372x520.")
            continue
        }

        $localDefinitionId = $definitionId.Substring($hero.Length + 1)
        $destinationCandidate = [System.IO.Path]::Combine(
            $targetRoot, $hero, "$localDefinitionId.png")
        $destination = Assert-PathUnder $destinationCandidate $targetRoot 'A prepared card resource'
        $inventory.Add([pscustomobject]@{
            Reference = "$definitionId@$revision"
            Hero = $hero
            SourcePath = $sourcePath
            Source = $source
            Destination = $destination
            ResourcePath = "Private/Cards/$hero/$localDefinitionId"
        })
    }

    foreach ($hero in $heroFolders.Keys) {
        $count = @($inventory | Where-Object { $_.Hero -ceq $hero }).Count
        if ($count -ne 18) {
            $errors.Add("Configured hero '$hero' resolved $count playable fronts; expected 18.")
        }
    }
}

if ($errors.Count -gt 0) {
    throw ("Card artwork inventory failed before any prepared image was written:`n - " +
        ($errors -join "`n - "))
}

Write-Output "PASS: the tracked catalog resolves $($inventory.Count) unique playable 372x520 fronts, 18 for each of $($heroFolders.Count) configured heroes."

function Test-PreparedSet([switch] $ReportErrors) {
    $preparedErrors = New-Object 'System.Collections.Generic.List[string]'
    foreach ($item in $inventory) {
        if (-not (Test-Path -LiteralPath $item.Destination -PathType Leaf)) {
            $preparedErrors.Add("Prepared resource '$($item.ResourcePath)' is absent.")
            continue
        }

        $size = Get-PngSize $item.Destination
        if ($null -eq $size -or $size.Width -ne 372 -or $size.Height -ne 520) {
            $preparedErrors.Add("Prepared resource '$($item.ResourcePath)' is not 372x520.")
            continue
        }

        $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item.Source).Hash
        $preparedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item.Destination).Hash
        if ($sourceHash -cne $preparedHash) {
            $preparedErrors.Add("Prepared resource '$($item.ResourcePath)' differs from its source.")
        }
    }

    if ($ReportErrors -and $preparedErrors.Count -gt 0) {
        throw ("Prepared card artwork verification failed:`n - " +
            ($preparedErrors -join "`n - "))
    }

    return $preparedErrors.Count -eq 0
}

if ($Check) {
    [void] (Test-PreparedSet -ReportErrors)
    Write-Output "PASS: all $($inventory.Count) prepared resources are present, 372x520, and byte-identical to their private sources."
    exit 0
}

if (-not $Force -and (Test-PreparedSet)) {
    Write-Output "SKIP: all $($inventory.Count) prepared resources already match. Pass -Force to replace them atomically."
    exit 0
}

if (-not (Test-Path -LiteralPath $tmpRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null
}
if (-not (Test-Path -LiteralPath $privateRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $privateRoot -Force | Out-Null
}

$operationId = [Guid]::NewGuid().ToString('N')
$stagingCandidate = [System.IO.Path]::Combine(
    $tmpRoot, "private-card-artwork-stage-$operationId")
$stagingRoot = Assert-PathUnder $stagingCandidate $tmpRoot 'The staging directory'
$backupCandidate = [System.IO.Path]::Combine(
    $privateRoot, "Cards-backup-$operationId")
$backupRoot = Assert-PathUnder $backupCandidate $privateRoot 'The backup directory'

try {
    New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null
    # Preserve existing Unity metadata, shine overlays, and any other private files.
    # Importing another hero must not replace these with a fronts-only directory.
    if (Test-Path -LiteralPath $targetRoot -PathType Container) {
        foreach ($existing in Get-ChildItem -LiteralPath $targetRoot -Force) {
            Copy-Item -LiteralPath $existing.FullName -Destination $stagingRoot -Recurse -Force
        }
    }
    foreach ($item in $inventory) {
        $stagedCandidate = [System.IO.Path]::Combine(
            $stagingRoot,
            $item.Hero,
            [System.IO.Path]::GetFileName($item.Destination))
        $staged = Assert-PathUnder $stagedCandidate $stagingRoot 'A staged card resource'
        $parent = Split-Path -Parent $staged
        if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }
        Copy-Item -LiteralPath $item.Source -Destination $staged

        $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $item.Source).Hash
        $stagedHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $staged).Hash
        if ($sourceHash -cne $stagedHash) {
            throw "Staging changed '$($item.Reference)'; the prepared target was not replaced."
        }
    }

    if (Test-Path -LiteralPath $targetRoot -PathType Container) {
        Move-Item -LiteralPath $targetRoot -Destination $backupRoot
    }

    try {
        Move-Item -LiteralPath $stagingRoot -Destination $targetRoot
    }
    catch {
        if (Test-Path -LiteralPath $backupRoot -PathType Container) {
            Move-Item -LiteralPath $backupRoot -Destination $targetRoot
        }
        throw
    }

    if (Test-Path -LiteralPath $backupRoot -PathType Container) {
        Remove-Item -LiteralPath $backupRoot -Recurse -Force
    }
}
finally {
    if (Test-Path -LiteralPath $stagingRoot -PathType Container) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}

[void] (Test-PreparedSet -ReportErrors)
Write-Output "WROTE: $($inventory.Count) ignored Unity card resources. Existing metadata/overlays were preserved. The private corpus was read only; no pixel was Git-staged, uploaded, or distributed."
