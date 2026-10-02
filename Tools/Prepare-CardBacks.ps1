<#
.SYNOPSIS
    Prepares each configured hero's private card back as an ignored Unity resource.

.DESCRIPTION
    Milestone 9.32h. A facedown card another seat may not name (Takahide's Bushido, p.10) shows its
    owner's card back on hover. This command reads the ignored Tabletopia corpus without mutating it
    and copies each declared back to:

        Assets/Resources/Private/CardBacks/<hero-id>.png

    The target is ignored by Git; the HUD draws its flat card-back colour when the file is absent.
    The command does not invoke Git, Unity, a build, a network client, or an uploader.

.PARAMETER Check
    Read-only verification: every prepared back exists, is 372x520 and is byte-identical to its source.
#>
[CmdletBinding()]
param([switch] $Check)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$corpusRoot = [System.IO.Path]::Combine($projectRoot, 'Docs', 'Guards of Atlantis II - Tabletopia Cards')
$targetRoot = [System.IO.Path]::Combine($projectRoot, 'Assets', 'Resources', 'Private', 'CardBacks')

# Every back in a hero's folder shares one image (Takahide's twenty-one backs share MD5
# 7566e55107c3d3f31ecb4e786f0309ad, inventory 9.32); any one of them is the back.
$backs = [ordered]@{
    'hero-takahide' = 'active-hero-cards/Takahide/takahide__7653240__back.png'
}

Add-Type -AssemblyName System.Drawing
foreach ($hero in $backs.Keys) {
    $source = [System.IO.Path]::Combine($corpusRoot, $backs[$hero])
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing card back source for ${hero}: $source"
    }

    $image = [System.Drawing.Image]::FromFile($source)
    try {
        if ($image.Width -ne 372 -or $image.Height -ne 520) {
            throw "Card back for $hero is $($image.Width)x$($image.Height), not 372x520: $source"
        }
    }
    finally {
        $image.Dispose()
    }

    $target = [System.IO.Path]::Combine($targetRoot, $hero + '.png')
    $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash
    if ($Check) {
        if (-not (Test-Path -LiteralPath $target -PathType Leaf) -or
            (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash -ne $sourceHash) {
            throw "Card back for $hero is missing or differs from its source: $target"
        }

        Write-Output "OK $hero"
        continue
    }

    [System.IO.Directory]::CreateDirectory($targetRoot) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Output "Prepared $hero -> $target"
}
