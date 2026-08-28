[CmdletBinding()]
param(
    [switch] $WriteManifest,
    [switch] $SkipSelfTests,
    [ValidateSet('None', 'Omission', 'Overlap', 'DuplicateDestination', 'StaleHash', 'UnregisteredSource')]
    [string] $DeliberateViolation = 'None'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$docsRoot = Join-Path $projectRoot 'Docs/AI'
$archiveRoot = Join-Path $docsRoot 'Archive'
$manifestPath = Join-Path $PSScriptRoot 'GovernanceSourceBodyManifest.json'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$strictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)

function Get-RelativeProjectPath([string] $Path) {
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $baseUri = New-Object System.Uri(($projectRoot.TrimEnd('\', '/') + $separator))
    $targetUri = New-Object System.Uri([System.IO.Path]::GetFullPath($Path))
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
}

function Get-Sha256Hex([byte[]] $Bytes, [int] $Offset = 0, [int] $Length = -1) {
    if ($Length -lt 0) { $Length = $Bytes.Length - $Offset }
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return (($sha256.ComputeHash($Bytes, $Offset, $Length) | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    finally { $sha256.Dispose() }
}

function Get-ObjectsSortedOrdinal($Objects, [string] $Property) {
    [object[]] $items = @($Objects)
    $comparison = [System.Comparison[object]]{
        param($left, $right)
        return [System.StringComparer]::Ordinal.Compare(
            [string]$left.PSObject.Properties[$Property].Value,
            [string]$right.PSObject.Properties[$Property].Value)
    }
    [System.Array]::Sort($items, $comparison)
    return $items
}

function Get-UniqueStringsSortedOrdinal($Values) {
    $unique = New-Object System.Collections.Generic.List[string]
    foreach ($value in @($Values)) {
        if ($null -eq $value) { continue }
        $text = [string]$value
        if ($text.Length -eq 0) { continue }
        if (-not $unique.Contains($text)) { $unique.Add($text) }
    }
    [string[]] $items = $unique.ToArray()
    [System.Array]::Sort($items, [System.StringComparer]::Ordinal)
    return $items
}

function New-SourceRecord([string] $Path, [string] $Kind) {
    [byte[]] $bytes = [System.IO.File]::ReadAllBytes($Path)
    try { $text = $strictUtf8.GetString($bytes) }
    catch { throw "Governed source is not strict UTF-8: $(Get-RelativeProjectPath $Path). $($_.Exception.Message)" }
    return [pscustomobject]@{
        Path = (Get-RelativeProjectPath $Path).Replace('\', '/')
        Kind = $Kind
        Bytes = $bytes
        Text = $text
    }
}

function New-VirtualSourceRecord([string] $Path, [string] $Kind, [string] $Text) {
    return [pscustomobject]@{
        Path = $Path.Replace('\', '/')
        Kind = $Kind
        Bytes = $utf8NoBom.GetBytes($Text)
        Text = $Text
    }
}

function Get-GovernedSources {
    $sources = New-Object System.Collections.Generic.List[object]
    foreach ($file in (Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter '*.md' |
        Where-Object { $_.FullName.Replace('\', '/') -notlike '*/Archive/*' })) {
        $sources.Add((New-SourceRecord $file.FullName 'GovernedActive'))
    }

    $agentsPath = Join-Path $projectRoot 'AGENTS.md'
    if (-not (Test-Path -LiteralPath $agentsPath -PathType Leaf)) { throw 'Missing governed source AGENTS.md.' }
    $sources.Add((New-SourceRecord $agentsPath 'GovernedActive'))

    foreach ($file in (Get-ChildItem -LiteralPath $archiveRoot -Recurse -File -Filter '*.md')) {
        $sources.Add((New-SourceRecord $file.FullName 'IndexedArchive'))
    }

    return @(Get-ObjectsSortedOrdinal $sources.ToArray() 'Path')
}

function Get-LinkedMarkdownPaths([string] $IndexPath) {
    $found = New-Object System.Collections.Generic.List[string]
    $text = [System.IO.File]::ReadAllText($IndexPath, [System.Text.Encoding]::UTF8)
    foreach ($match in [regex]::Matches($text, '(?<!\!)\[[^\]]+\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value.Trim()
        if ($target -match '^(?:https?:|mailto:|#)') { continue }
        $pathPart = ($target -split '#', 2)[0]
        if ([string]::IsNullOrWhiteSpace($pathPart) -or [System.IO.Path]::GetExtension($pathPart) -ne '.md') { continue }
        $resolved = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $IndexPath) $pathPart))
        $found.Add((Get-RelativeProjectPath $resolved).Replace('\', '/'))
    }
    return $found.ToArray()
}

function Get-SourceRegistrationIssues($Sources) {
    $issues = New-Object System.Collections.Generic.List[string]
    $paths = @{}
    foreach ($source in $Sources) { $paths[$source.Path] = $true }

    $roadmapIndex = Join-Path $docsRoot 'ROADMAP.md'
    $roadmapLinks = @{}
    foreach ($path in (Get-LinkedMarkdownPaths $roadmapIndex)) { $roadmapLinks[$path] = $true }
    foreach ($source in $Sources | Where-Object { $_.Path -like 'Docs/AI/Roadmap/*.md' }) {
        if (-not $roadmapLinks.ContainsKey($source.Path)) { $issues.Add("unindexed-roadmap-source:$($source.Path)") }
    }

    $archiveIndex = Join-Path $archiveRoot 'README.md'
    $archiveRegistrations = @{ 'Docs/AI/Archive/README.md' = $true }
    foreach ($path in (Get-LinkedMarkdownPaths $archiveIndex)) {
        if ($path -like 'Docs/AI/Archive/*.md' -or $path -like 'Docs/AI/Archive/*/*.md') {
            $archiveRegistrations[$path] = $true
        }
    }

    $preGovernanceInventory = Join-Path $archiveRoot 'Pre-Governance/SHA256SUMS.md'
    if (Test-Path -LiteralPath $preGovernanceInventory -PathType Leaf) {
        $inventoryText = [System.IO.File]::ReadAllText($preGovernanceInventory, [System.Text.Encoding]::UTF8)
        foreach ($match in [regex]::Matches($inventoryText, '(?m)^\|\s*`([^`]+\.md)`\s*\|')) {
            $child = Join-Path (Split-Path -Parent $preGovernanceInventory) $match.Groups[1].Value
            $archiveRegistrations[(Get-RelativeProjectPath $child).Replace('\', '/')] = $true
        }
    }

    foreach ($source in $Sources | Where-Object { $_.Kind -eq 'IndexedArchive' }) {
        if (-not $archiveRegistrations.ContainsKey($source.Path)) { $issues.Add("unindexed-archive-source:$($source.Path)") }
    }
    foreach ($registered in $archiveRegistrations.Keys) {
        if (-not $paths.ContainsKey($registered)) { $issues.Add("missing-indexed-archive-source:$registered") }
    }
    return $issues.ToArray()
}

function Get-MarkdownHeadingStarts([string] $Text) {
    $headings = New-Object System.Collections.Generic.List[object]
    $lineStart = 0
    $insideFence = $false
    $fenceCharacter = ''
    $fenceLength = 0

    while ($lineStart -lt $Text.Length) {
        $newline = $Text.IndexOf("`n", $lineStart)
        $lineEnd = if ($newline -lt 0) { $Text.Length } else { $newline }
        $contentEnd = $lineEnd
        if ($contentEnd -gt $lineStart -and $Text[$contentEnd - 1] -eq "`r") { $contentEnd-- }
        $line = $Text.Substring($lineStart, $contentEnd - $lineStart)
        $candidate = $line
        $candidateStart = $lineStart
        if ($lineStart -eq 0 -and $candidate.Length -gt 0 -and $candidate[0] -eq [char]0xFEFF) {
            $candidate = $candidate.Substring(1)
            $candidateStart++
        }

        $fence = [regex]::Match($candidate, '^ {0,3}(`{3,}|~{3,})')
        if ($fence.Success) {
            $token = $fence.Groups[1].Value
            if (-not $insideFence) {
                $insideFence = $true
                $fenceCharacter = $token.Substring(0, 1)
                $fenceLength = $token.Length
            }
            elseif ($token.Substring(0, 1) -eq $fenceCharacter -and $token.Length -ge $fenceLength) {
                $insideFence = $false
                $fenceCharacter = ''
                $fenceLength = 0
            }
        }
        elseif (-not $insideFence) {
            $heading = [regex]::Match($candidate, '^ {0,3}(#{1,6})[ \t]+(.+?)(?:[ \t]+#+)?[ \t]*$')
            if ($heading.Success) {
                $headings.Add([pscustomobject]@{
                    CharStart = $candidateStart
                    Level = $heading.Groups[1].Value.Length
                    Heading = $heading.Groups[2].Value.Trim()
                })
            }
        }

        if ($newline -lt 0) { break }
        $lineStart = $newline + 1
    }

    $lastChar = 0
    $lastByte = 0
    foreach ($heading in $headings) {
        if ($heading.CharStart -gt $lastChar) {
            $lastByte += $utf8NoBom.GetByteCount($Text.Substring($lastChar, $heading.CharStart - $lastChar))
        }
        Add-Member -InputObject $heading -NotePropertyName StartByte -NotePropertyValue $lastByte
        $lastChar = $heading.CharStart
    }
    return $headings.ToArray()
}

function Get-SourceSegments($Source) {
    $headings = @(Get-MarkdownHeadingStarts $Source.Text)
    $segments = New-Object System.Collections.Generic.List[object]
    $preambleEnd = if ($headings.Count -gt 0) { [int]$headings[0].StartByte } else { $Source.Bytes.Length }
    $segments.Add([pscustomobject]@{
        Id = "body:$($Source.Path):0"
        Sequence = 0
        Kind = 'Preamble'
        HeadingLevel = $null
        Heading = ''
        MilestoneStatus = ''
        StartByte = 0
        EndByte = $preambleEnd
    })
    $milestoneStatus = ''
    $milestoneLevel = 0
    for ($i = 0; $i -lt $headings.Count; $i++) {
        $start = [int]$headings[$i].StartByte
        $end = if ($i + 1 -lt $headings.Count) { [int]$headings[$i + 1].StartByte } else { $Source.Bytes.Length }
        $marker = [regex]::Match($headings[$i].Heading, '^\[(x| |!|~)\]\s+')
        if ($marker.Success) {
            $milestoneStatus = $marker.Groups[1].Value
            $milestoneLevel = $headings[$i].Level
        }
        elseif ($milestoneStatus -ne '' -and $headings[$i].Level -le $milestoneLevel) {
            $milestoneStatus = ''
            $milestoneLevel = 0
        }
        $segments.Add([pscustomobject]@{
            Id = "body:$($Source.Path):$($i + 1)"
            Sequence = $i + 1
            Kind = 'HeadingBody'
            HeadingLevel = $headings[$i].Level
            Heading = $headings[$i].Heading
            MilestoneStatus = $milestoneStatus
            StartByte = $start
            EndByte = $end
        })
    }
    return $segments.ToArray()
}

function Read-OperativeClaims([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw 'Tools/OperativeClaims.tsv is missing.' }
    $claims = New-Object System.Collections.Generic.List[object]
    foreach ($line in [System.IO.File]::ReadAllLines($Path, [System.Text.Encoding]::UTF8)) {
        $trimmed = $line.Trim()
        if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }
        $parts = $line -split "`t", 6
        if ($parts.Count -ne 6) { throw "Malformed operative-claim row: $line" }
        $claims.Add([pscustomobject]@{
            Id = $parts[0].Trim()
            Owner = $parts[1].Trim()
            Status = $parts[2].Trim()
            Evidence = $parts[3].Trim()
            Trigger = $parts[4].Trim()
            Marker = $parts[5].Trim()
        })
    }
    if ($claims.Count -eq 0) { throw 'Tools/OperativeClaims.tsv has no registered claims.' }
    return $claims.ToArray()
}

function Initialize-SourceFacts($Sources) {
    foreach ($source in $Sources) {
        Add-Member -InputObject $source -NotePropertyName Sha256 -NotePropertyValue (Get-Sha256Hex $source.Bytes) -Force
        Add-Member -InputObject $source -NotePropertyName Segments -NotePropertyValue @(Get-SourceSegments $source) -Force
    }
}

function Get-CorpusRevision($Sources) {
    $builder = New-Object System.Text.StringBuilder
    foreach ($source in (Get-ObjectsSortedOrdinal $Sources 'Path')) {
        [void]$builder.Append($source.Path)
        [void]$builder.Append([char]0)
        [void]$builder.Append($source.Bytes.Length)
        [void]$builder.Append([char]0)
        [void]$builder.Append($source.Sha256)
        [void]$builder.Append("`n")
    }
    return 'sha256:' + (Get-Sha256Hex $utf8NoBom.GetBytes($builder.ToString()))
}

function Get-ClaimBindings($Sources, $Claims) {
    $sourceMap = @{}
    foreach ($source in $Sources) { $sourceMap[$source.Path] = $source }
    $bindings = @{}
    foreach ($claim in $Claims) {
        $ownerPath = ('Docs/AI/' + $claim.Owner.TrimStart('/', '\')).Replace('\', '/')
        if (-not $sourceMap.ContainsKey($ownerPath)) { throw "Operative claim $($claim.Id) has unknown source owner $ownerPath." }
        $source = $sourceMap[$ownerPath]
        $matches = [regex]::Matches($source.Text, [regex]::Escape($claim.Marker))
        if ($matches.Count -ne 1) { throw "Operative claim $($claim.Id) marker occurs $($matches.Count) times in $ownerPath." }
        $markerByte = $utf8NoBom.GetByteCount($source.Text.Substring(0, $matches[0].Index))
        $segment = @($source.Segments | Where-Object { $_.StartByte -le $markerByte -and $_.EndByte -gt $markerByte })
        if ($segment.Count -ne 1) { throw "Operative claim $($claim.Id) marker binds to $($segment.Count) bodies in $ownerPath." }
        if (-not $bindings.ContainsKey($segment[0].Id)) { $bindings[$segment[0].Id] = New-Object System.Collections.Generic.List[object] }
        $bindings[$segment[0].Id].Add($claim)
    }
    return $bindings
}

function Get-SourceRouteTags($Source) {
    $tags = @()
    if ($Source.Kind -eq 'IndexedArchive') { $tags += @('historical', 'archive') } else { $tags += 'governed-active' }
    switch -Wildcard ($Source.Path) {
        'AGENTS.md' { $tags += @('startup', 'process') }
        'Docs/AI/SESSION_BRIEF.md' { $tags += @('startup', 'authorization') }
        'Docs/AI/ROADMAP.md' { $tags += @('roadmap', 'index') }
        'Docs/AI/Roadmap/*.md' { $tags += @('roadmap', 'phase-definition') }
        'Docs/AI/Testing/*.md' { $tags += 'validation' }
        'Docs/AI/TESTING.md' { $tags += @('validation', 'index') }
        'Docs/AI/Rules/*.md' { $tags += 'rules' }
        'Docs/AI/RULES_LEDGER.md' { $tags += @('rules', 'index') }
        'Docs/AI/Card-System/*.md' { $tags += 'card-contract' }
        'Docs/AI/CARD_*.md' { $tags += @('card-contract', 'index') }
        'Docs/AI/Project/*.md' { $tags += 'project' }
        'Docs/AI/PROJECT_CONTEXT.md' { $tags += @('project', 'index') }
        'Docs/AI/Maps/*.md' { $tags += 'map' }
        'Docs/AI/DOCUMENTATION_GOVERNANCE.md' { $tags += 'governance' }
        'Docs/AI/TECHNICAL_RISKS.md' { $tags += 'risk' }
        'Docs/AI/Archive/README.md' { $tags += 'index' }
        'Docs/AI/Archive/Phase-Handoffs/*.md' { $tags += @('milestone-evidence', 'phase-archive') }
        'Docs/AI/Archive/Pre-Governance/*.md' { $tags += @('frozen', 'preservation') }
        'Docs/AI/Archive/MIGRATION_MANIFEST.md' { $tags += @('frozen', 'migration') }
        'Docs/AI/Archive/PRESERVATION_INVENTORY.md' { $tags += @('frozen', 'preservation') }
    }
    return @($tags | Select-Object -Unique)
}

function Get-BodyMetadata($Source, $Segment, $BoundClaims) {
    $classification = 'GovernedActiveBody'
    $status = 'Current'
    if ($Segment.Kind -eq 'Preamble') { $classification = 'RawUtf8Preamble' }
    if ($Source.Kind -eq 'IndexedArchive') { $classification = if ($Segment.Kind -eq 'Preamble') { 'RawUtf8Preamble' } else { 'IndexedHistoricalBody' }; $status = 'Historical' }
    elseif ($Source.Path -like 'Docs/AI/Roadmap/*.md') {
        if ($Segment.MilestoneStatus -eq 'x') { $classification = 'CompletedMilestoneDefinition'; $status = 'Historical' }
        elseif ($Segment.MilestoneStatus -eq ' ') { $classification = 'ProposedMilestoneDefinition'; $status = 'Proposed' }
        elseif ($Segment.MilestoneStatus -eq '!') { $classification = 'BlockedMilestoneDefinition'; $status = 'Blocked' }
        elseif ($Segment.MilestoneStatus -eq '~') { $classification = 'AuthorizedMilestoneDefinition'; $status = 'ActiveAuthorized' }
    }

    $claimArray = @($BoundClaims | Where-Object { $null -ne $_ })
    if ($claimArray.Count -gt 0) {
        $classification = 'RegisteredOperativeClaimBody'
        $statuses = @(Get-UniqueStringsSortedOrdinal ($claimArray | ForEach-Object { $_.Status }))
        $status = if ($statuses.Count -eq 1) { $statuses[0] } else { 'MixedRegisteredStatuses' }
    }

    $tags = New-Object System.Collections.Generic.List[string]
    foreach ($tag in (Get-SourceRouteTags $Source)) { $tags.Add($tag) }
    if ($claimArray.Count -gt 0) { $tags.Add('registered-operative-claim') }
    $triggers = if ($claimArray.Count -gt 0) {
        @(Get-UniqueStringsSortedOrdinal ($claimArray | ForEach-Object { $_.Trigger }))
    }
    elseif ($Source.Kind -eq 'IndexedArchive') {
        @('Reassess only when an explicit append-only correction, addendum, or index change alters this historical source body.')
    }
    else {
        @('Reassess when this source body, its canonical ownership, route classification, or authorized disposition changes.')
    }

    return [pscustomobject]@{
        Classification = $classification
        CanonicalOwner = $Source.Path
        Status = $status
        SourceLocator = "$($Source.Path)@bytes=$($Segment.StartByte)..$($Segment.EndByte)"
        EvidenceLocators = @(Get-UniqueStringsSortedOrdinal ($claimArray | ForEach-Object { $_.Evidence }))
        ReassessmentTriggers = $triggers
        RouteTags = $tags.ToArray()
        Destination = "retain://$($Source.Path)?sequence=$($Segment.Sequence)&bytes=$($Segment.StartByte)-$($Segment.EndByte)"
    }
}

function New-GovernanceManifest($Sources, $Claims) {
    Initialize-SourceFacts $Sources
    $revision = Get-CorpusRevision $Sources
    $bindings = Get-ClaimBindings $Sources $Claims
    $manifestSources = New-Object System.Collections.Generic.List[object]
    $manifestBodies = New-Object System.Collections.Generic.List[object]
    $manifestClaims = New-Object System.Collections.Generic.List[object]
    $claimToBody = @{}

    foreach ($source in (Get-ObjectsSortedOrdinal $Sources 'Path')) {
        $manifestSources.Add([pscustomobject][ordered]@{
            path = $source.Path
            sourceKind = $source.Kind
            sourceRevision = $revision
            byteLength = $source.Bytes.Length
            sha256 = $source.Sha256
            bodyCount = @($source.Segments).Count
            indexRoles = @(Get-SourceRouteTags $source)
        })
        foreach ($segment in $source.Segments) {
            $bound = if ($bindings.ContainsKey($segment.Id)) { @($bindings[$segment.Id].ToArray()) } else { @() }
            $metadata = Get-BodyMetadata $source $segment $bound
            $length = $segment.EndByte - $segment.StartByte
            $manifestBodies.Add([pscustomobject][ordered]@{
                id = $segment.Id
                sourceRevision = $revision
                sourcePath = $source.Path
                sequence = $segment.Sequence
                bodyKind = $segment.Kind
                headingLevel = $segment.HeadingLevel
                heading = $segment.Heading
                milestoneStatus = $segment.MilestoneStatus
                startByte = $segment.StartByte
                endByte = $segment.EndByte
                byteLength = $length
                sha256 = Get-Sha256Hex $source.Bytes $segment.StartByte $length
                classification = $metadata.Classification
                canonicalOwner = $metadata.CanonicalOwner
                status = $metadata.Status
                sourceLocator = $metadata.SourceLocator
                evidenceLocators = $metadata.EvidenceLocators
                reassessmentTriggers = $metadata.ReassessmentTriggers
                routeTags = $metadata.RouteTags
                disposition = [pscustomobject][ordered]@{
                    kind = 'RetainSourceBody'
                    destination = $metadata.Destination
                    reason = 'Milestone 8.25e6a authorizes mapping only; no migration, candidate selection, or authority change.'
                }
                operativeClaimIds = @(Get-UniqueStringsSortedOrdinal ($bound | ForEach-Object { $_.Id }))
            })
            foreach ($claim in $bound) { $claimToBody[$claim.Id] = $segment.Id }
        }
    }

    foreach ($claim in (Get-ObjectsSortedOrdinal $Claims 'Id')) {
        $manifestClaims.Add([pscustomobject][ordered]@{
            id = $claim.Id
            bodyId = $claimToBody[$claim.Id]
            canonicalOwner = $claim.Owner
            status = $claim.Status
            evidenceLocator = $claim.Evidence
            reassessmentTrigger = $claim.Trigger
            marker = $claim.Marker
        })
    }

    $bodyBytes = ($manifestBodies | Measure-Object -Property byteLength -Sum).Sum
    $sourceBytes = ($manifestSources | Measure-Object -Property byteLength -Sum).Sum
    return [pscustomobject][ordered]@{
        format = 'guards-digital-governance-source-body-manifest'
        schemaVersion = 1
        authority = 'NonAuthoritative'
        notice = 'Inventory and experiment input only. Canonical authority remains in each recorded source; retention entries authorize no migration or generated active authority.'
        sourceRevisionAlgorithm = 'sha256 over ordinal path + NUL + byte length + NUL + source SHA-256 + LF for every discovered source'
        sourceRevision = $revision
        discovery = [pscustomobject][ordered]@{
            governedActive = 'AGENTS.md plus every Docs/AI/**/*.md outside Docs/AI/Archive, matching Validate-LivingDocumentation.ps1'
            roadmapBodies = 'Every Docs/AI/Roadmap/*.md source registered by Docs/AI/ROADMAP.md'
            historicalBodies = 'Every Docs/AI/Archive/**/*.md source registered by Docs/AI/Archive/README.md or its pre-governance preservation inventory'
        }
        balance = [pscustomobject][ordered]@{
            sourceCount = $manifestSources.Count
            bodyCount = $manifestBodies.Count
            claimCount = $manifestClaims.Count
            sourceByteCount = $sourceBytes
            bodyByteCount = $bodyBytes
        }
        sources = $manifestSources.ToArray()
        bodies = $manifestBodies.ToArray()
        claims = $manifestClaims.ToArray()
    }
}

function Test-StringArrayEqual($Left, $Right) {
    $a = @($Left)
    $b = @($Right)
    if ($a.Count -ne $b.Count) { return $false }
    for ($i = 0; $i -lt $a.Count; $i++) { if ([string]$a[$i] -cne [string]$b[$i]) { return $false } }
    return $true
}

function Test-ByteArrayEqual([byte[]] $Left, [byte[]] $Right) {
    if ($Left.Length -ne $Right.Length) { return $false }
    for ($i = 0; $i -lt $Left.Length; $i++) { if ($Left[$i] -ne $Right[$i]) { return $false } }
    return $true
}

function Get-GovernanceManifestIssues($Manifest, $Sources, $Claims) {
    $issues = New-Object System.Collections.Generic.List[string]
    $expected = New-GovernanceManifest $Sources $Claims
    if ($Manifest.format -cne $expected.format -or [int]$Manifest.schemaVersion -ne 1 -or $Manifest.authority -cne 'NonAuthoritative') { $issues.Add('invalid-manifest-header') }
    if ($Manifest.sourceRevision -cne $expected.sourceRevision) { $issues.Add('stale-source-revision') }

    $actualSourceMap = @{}
    foreach ($source in $Sources) { $actualSourceMap[$source.Path] = $source }
    $expectedSourceMap = @{}
    foreach ($source in $expected.sources) { $expectedSourceMap[$source.path] = $source }
    $manifestSourceMap = @{}
    foreach ($source in @($Manifest.sources)) {
        if ($manifestSourceMap.ContainsKey([string]$source.path)) { $issues.Add("duplicate-source:$($source.path)") }
        else { $manifestSourceMap[[string]$source.path] = $source }
        if (-not $actualSourceMap.ContainsKey([string]$source.path)) { $issues.Add("unknown-source:$($source.path)") }
    }
    foreach ($path in $actualSourceMap.Keys) { if (-not $manifestSourceMap.ContainsKey($path)) { $issues.Add("missing-source:$path") } }
    foreach ($path in $expectedSourceMap.Keys) {
        if (-not $manifestSourceMap.ContainsKey($path)) { continue }
        $left = $manifestSourceMap[$path]; $right = $expectedSourceMap[$path]
        if ($left.sourceKind -cne $right.sourceKind -or $left.sourceRevision -cne $right.sourceRevision -or
            [long]$left.byteLength -ne [long]$right.byteLength -or $left.sha256 -cne $right.sha256 -or
            [long]$left.bodyCount -ne [long]$right.bodyCount -or -not (Test-StringArrayEqual $left.indexRoles $right.indexRoles)) {
            $issues.Add("stale-source-entry:$path")
        }
    }

    $expectedBodyMap = @{}
    foreach ($body in $expected.bodies) { $expectedBodyMap[$body.id] = $body }
    $manifestBodyMap = @{}
    $destinationOwners = @{}
    $sequenceOwners = @{}
    foreach ($body in @($Manifest.bodies)) {
        $id = [string]$body.id
        if ($manifestBodyMap.ContainsKey($id)) { $issues.Add("duplicate-body:$id") } else { $manifestBodyMap[$id] = $body }
        if (-not $actualSourceMap.ContainsKey([string]$body.sourcePath)) { $issues.Add("unknown-body-source:$id") }
        $sequenceKey = "$($body.sourcePath)|$($body.sequence)"
        if ($sequenceOwners.ContainsKey($sequenceKey)) { $issues.Add("duplicate-body-sequence:$sequenceKey") } else { $sequenceOwners[$sequenceKey] = $id }
        $destination = [string]$body.disposition.destination
        if ([string]::IsNullOrWhiteSpace($destination)) { $issues.Add("missing-destination:$id") }
        elseif ($destinationOwners.ContainsKey($destination)) { $issues.Add("duplicate-destination:$destination") }
        else { $destinationOwners[$destination] = $id }
        if (@('RetainSourceBody', 'CandidateDestination') -notcontains [string]$body.disposition.kind) { $issues.Add("invalid-disposition:$id") }
        foreach ($field in @('classification', 'canonicalOwner', 'status', 'sourceLocator')) {
            if ([string]::IsNullOrWhiteSpace([string]$body.$field)) { $issues.Add("missing-$field`:$id") }
        }
        if (@($body.reassessmentTriggers).Count -eq 0 -or @($body.routeTags).Count -eq 0) { $issues.Add("missing-routing-metadata:$id") }
    }
    foreach ($id in $expectedBodyMap.Keys) { if (-not $manifestBodyMap.ContainsKey($id)) { $issues.Add("omitted-body:$id") } }
    foreach ($id in $manifestBodyMap.Keys) { if (-not $expectedBodyMap.ContainsKey($id)) { $issues.Add("unknown-body:$id") } }

    foreach ($id in $expectedBodyMap.Keys) {
        if (-not $manifestBodyMap.ContainsKey($id)) { continue }
        $left = $manifestBodyMap[$id]; $right = $expectedBodyMap[$id]
        $scalarMismatch =
            $left.sourceRevision -cne $right.sourceRevision -or $left.sourcePath -cne $right.sourcePath -or
            [long]$left.sequence -ne [long]$right.sequence -or $left.bodyKind -cne $right.bodyKind -or
            [string]$left.headingLevel -cne [string]$right.headingLevel -or $left.heading -cne $right.heading -or
            $left.milestoneStatus -cne $right.milestoneStatus -or
            [long]$left.startByte -ne [long]$right.startByte -or [long]$left.endByte -ne [long]$right.endByte -or
            [long]$left.byteLength -ne [long]$right.byteLength -or $left.sha256 -cne $right.sha256 -or
            $left.classification -cne $right.classification -or $left.canonicalOwner -cne $right.canonicalOwner -or
            $left.status -cne $right.status -or $left.sourceLocator -cne $right.sourceLocator -or
            $left.disposition.kind -cne $right.disposition.kind -or $left.disposition.destination -cne $right.disposition.destination
        if ($scalarMismatch -or -not (Test-StringArrayEqual $left.evidenceLocators $right.evidenceLocators) -or
            -not (Test-StringArrayEqual $left.reassessmentTriggers $right.reassessmentTriggers) -or
            -not (Test-StringArrayEqual $left.routeTags $right.routeTags) -or
            -not (Test-StringArrayEqual $left.operativeClaimIds $right.operativeClaimIds)) {
            $issues.Add("stale-body-entry:$id")
        }
    }

    foreach ($path in $actualSourceMap.Keys) {
        $source = $actualSourceMap[$path]
        $mapped = @($Manifest.bodies | Where-Object { $_.sourcePath -ceq $path } | Sort-Object { [long]$_.sequence })
        $cursor = 0L
        $reconstructed = New-Object System.IO.MemoryStream
        try {
            foreach ($body in $mapped) {
                $start = [long]$body.startByte; $end = [long]$body.endByte
                if ($start -gt $cursor) { $issues.Add("byte-gap:$path@$cursor..$start") }
                elseif ($start -lt $cursor) { $issues.Add("byte-overlap:$path@$start..$cursor") }
                if ($start -lt 0 -or $end -lt $start -or $end -gt $source.Bytes.Length) { $issues.Add("invalid-byte-range:$($body.id)"); continue }
                $length = [int]($end - $start)
                if ([long]$body.byteLength -ne $length) { $issues.Add("stale-byte-length:$($body.id)") }
                elseif ((Get-Sha256Hex $source.Bytes ([int]$start) $length) -cne [string]$body.sha256) { $issues.Add("stale-body-hash:$($body.id)") }
                $reconstructed.Write($source.Bytes, [int]$start, $length)
                $cursor = $end
            }
            if ($cursor -ne $source.Bytes.Length) { $issues.Add("byte-gap:$path@$cursor..$($source.Bytes.Length)") }
            if (-not (Test-ByteArrayEqual $source.Bytes $reconstructed.ToArray())) { $issues.Add("reconstruction-mismatch:$path") }
        }
        finally { $reconstructed.Dispose() }
    }

    $expectedClaimMap = @{}; foreach ($claim in $expected.claims) { $expectedClaimMap[$claim.id] = $claim }
    $manifestClaimMap = @{}
    foreach ($claim in @($Manifest.claims)) {
        if ($manifestClaimMap.ContainsKey([string]$claim.id)) { $issues.Add("multiply-bound-claim:$($claim.id)") } else { $manifestClaimMap[[string]$claim.id] = $claim }
        if (-not $expectedClaimMap.ContainsKey([string]$claim.id)) { $issues.Add("unknown-claim:$($claim.id)") }
    }
    foreach ($id in $expectedClaimMap.Keys) {
        if (-not $manifestClaimMap.ContainsKey($id)) { $issues.Add("missing-claim:$id"); continue }
        $left = $manifestClaimMap[$id]; $right = $expectedClaimMap[$id]
        if ($left.bodyId -cne $right.bodyId -or $left.canonicalOwner -cne $right.canonicalOwner -or
            $left.status -cne $right.status -or $left.evidenceLocator -cne $right.evidenceLocator -or
            $left.reassessmentTrigger -cne $right.reassessmentTrigger -or $left.marker -cne $right.marker) {
            $issues.Add("stale-claim-binding:$id")
        }
    }
    $bodyClaimCounts = @{}
    foreach ($body in @($Manifest.bodies)) {
        foreach ($id in @($body.operativeClaimIds)) {
            if (-not $bodyClaimCounts.ContainsKey([string]$id)) { $bodyClaimCounts[[string]$id] = 0 }
            $bodyClaimCounts[[string]$id]++
        }
    }
    foreach ($id in $expectedClaimMap.Keys) {
        if (-not $bodyClaimCounts.ContainsKey($id)) { $issues.Add("missing-body-claim-binding:$id") }
        elseif ($bodyClaimCounts[$id] -ne 1) { $issues.Add("multiply-bound-body-claim:$id") }
    }
    foreach ($id in $bodyClaimCounts.Keys) { if (-not $expectedClaimMap.ContainsKey($id)) { $issues.Add("unknown-body-claim:$id") } }

    $manifestSources = @($Manifest.sources); $manifestBodies = @($Manifest.bodies); $manifestClaims = @($Manifest.claims)
    $manifestSourceBytes = ($manifestSources | Measure-Object -Property byteLength -Sum).Sum
    $manifestBodyBytes = ($manifestBodies | Measure-Object -Property byteLength -Sum).Sum
    if ([long]$Manifest.balance.sourceCount -ne $manifestSources.Count -or [long]$Manifest.balance.sourceCount -ne $expected.sources.Count) { $issues.Add('unbalanced-source-count') }
    if ([long]$Manifest.balance.bodyCount -ne $manifestBodies.Count -or [long]$Manifest.balance.bodyCount -ne $expected.bodies.Count) { $issues.Add('unbalanced-body-count') }
    if ([long]$Manifest.balance.claimCount -ne $manifestClaims.Count -or [long]$Manifest.balance.claimCount -ne $expected.claims.Count) { $issues.Add('unbalanced-claim-count') }
    if ([long]$Manifest.balance.sourceByteCount -ne [long]$manifestSourceBytes -or [long]$Manifest.balance.sourceByteCount -ne [long]$expected.balance.sourceByteCount) { $issues.Add('unbalanced-source-bytes') }
    if ([long]$Manifest.balance.bodyByteCount -ne [long]$manifestBodyBytes -or [long]$Manifest.balance.bodyByteCount -ne [long]$expected.balance.bodyByteCount -or [long]$manifestBodyBytes -ne [long]$manifestSourceBytes) { $issues.Add('unbalanced-body-bytes') }
    return $issues.ToArray()
}

function Copy-JsonObject($Object) {
    return ($Object | ConvertTo-Json -Depth 12 | ConvertFrom-Json)
}

function Apply-DeliberateViolation($Manifest, [ref] $Sources, [string] $Violation) {
    if ($Violation -eq 'None') { return $Manifest }
    $copy = Copy-JsonObject $Manifest
    switch ($Violation) {
        'Omission' { $copy.bodies = @($copy.bodies | Select-Object -Skip 1) }
        'Overlap' {
            $target = @($copy.bodies | Where-Object { [long]$_.sequence -gt 1 -and [long]$_.byteLength -gt 1 } | Select-Object -First 1)[0]
            $target.startByte = [long]$target.startByte - 1
        }
        'DuplicateDestination' { $copy.bodies[1].disposition.destination = $copy.bodies[0].disposition.destination }
        'StaleHash' { $copy.bodies[1].sha256 = ('0' * 64) }
        'UnregisteredSource' {
            $Sources.Value = @($Sources.Value) + @(New-VirtualSourceRecord 'Docs/AI/Fixtures/UNREGISTERED.md' 'GovernedActive' "# Unregistered fixture`n")
        }
    }
    return $copy
}

function Invoke-ManifestSelfTests {
    $failed = New-Object System.Collections.Generic.List[string]
    $count = 0
    function Expect([string] $Name, [bool] $Condition) { $script:manifestSelfTestCount++; if (-not $Condition) { $failed.Add($Name) } }
    $script:manifestSelfTestCount = 0

    $marker = '<!-- operative-claim:F-DOC-T status:Unresolved -->'
    $fixtureText = ([string][char]0xFEFF) + "# Owner`n`n$marker`nUncertain body.`n`n## Next`nCurrent body.`n`n~~~text`n# not a heading`n~~~`n"
    $source = New-VirtualSourceRecord 'Docs/AI/OWNER.md' 'GovernedActive' $fixtureText
    $archive = New-VirtualSourceRecord 'Docs/AI/Archive/HISTORY.md' 'IndexedArchive' "# History`n`nExact evidence.`n"
    $claim = [pscustomobject]@{ Id = 'F-DOC-T'; Owner = 'OWNER.md'; Status = 'Unresolved'; Evidence = 'Docs/AI/OWNER.md#owner'; Trigger = 'Reassess when evidence changes.'; Marker = $marker }
    $sources = @($source, $archive)
    $manifest = New-GovernanceManifest $sources @($claim)
    $ordinalProbe = @(Get-ObjectsSortedOrdinal @(
        [pscustomobject]@{ Path = 'Docs/AI/Project/Z.md' },
        [pscustomobject]@{ Path = 'Docs/AI/PROJECT_CONTEXT.md' },
        [pscustomobject]@{ Path = 'Docs/AI/Project/A.md' }) 'Path')
    Expect 'source ordering is ordinal rather than host-culture order' (($ordinalProbe.Path -join '|') -ceq 'Docs/AI/PROJECT_CONTEXT.md|Docs/AI/Project/A.md|Docs/AI/Project/Z.md')
    Expect 'corpus revision is independent of discovery enumeration order' ((Get-CorpusRevision $sources) -ceq (Get-CorpusRevision @($archive, $source)))
    $completeIssues = @(Get-GovernanceManifestIssues $manifest $sources @($claim))
    if ($completeIssues.Count -gt 0) { Write-Host "Self-test complete-map diagnostics: $($completeIssues -join ', ')" }
    Expect 'CompleteSourceDestinationManifestCoversEveryGovernedBody passes on a complete map' ($completeIssues.Count -eq 0)
    $ownerPreamble = @($manifest.bodies | Where-Object { $_.sourcePath -eq 'Docs/AI/OWNER.md' -and $_.bodyKind -eq 'Preamble' })[0]
    Expect 'raw UTF-8 BOM is a three-byte preamble' ([long]$ownerPreamble.byteLength -eq 3)
    Expect 'fenced hash text is not a heading body' (@($manifest.bodies | Where-Object { $_.heading -eq 'not a heading' }).Count -eq 0)

    $omission = Copy-JsonObject $manifest; $omission.bodies = @($omission.bodies | Select-Object -Skip 1)
    Expect 'CompleteSourceDestinationManifestCoversEveryGovernedBody fails on a deliberate omission' (@(@(Get-GovernanceManifestIssues $omission $sources @($claim)) | Where-Object { $_ -like 'omitted-body:*' }).Count -gt 0)
    $overlap = Copy-JsonObject $manifest; $overlap.bodies[2].startByte = [long]$overlap.bodies[2].startByte - 1
    Expect 'overlap is rejected' (@(@(Get-GovernanceManifestIssues $overlap $sources @($claim)) | Where-Object { $_ -like 'byte-overlap:*' }).Count -gt 0)
    $duplicateDestination = Copy-JsonObject $manifest; $duplicateDestination.bodies[1].disposition.destination = $duplicateDestination.bodies[0].disposition.destination
    Expect 'duplicate destination is rejected' (@(@(Get-GovernanceManifestIssues $duplicateDestination $sources @($claim)) | Where-Object { $_ -like 'duplicate-destination:*' }).Count -gt 0)
    $staleHash = Copy-JsonObject $manifest; $staleHash.bodies[1].sha256 = ('0' * 64)
    Expect 'stale body hash is rejected' (@(@(Get-GovernanceManifestIssues $staleHash $sources @($claim)) | Where-Object { $_ -like 'stale-body-hash:*' }).Count -gt 0)
    $extraSources = $sources + @(New-VirtualSourceRecord 'Docs/AI/EXTRA.md' 'GovernedActive' "# Extra`n")
    Expect 'unregistered source fixture is rejected' (@(@(Get-GovernanceManifestIssues $manifest $extraSources @($claim)) | Where-Object { $_ -like 'missing-source:*' }).Count -gt 0)
    $missingClaim = Copy-JsonObject $manifest; $missingClaim.claims = @()
    Expect 'missing registered claim is rejected' (@(Get-GovernanceManifestIssues $missingClaim $sources @($claim)) -contains 'missing-claim:F-DOC-T')
    $multiplyBoundClaim = Copy-JsonObject $manifest; $multiplyBoundClaim.claims = @($multiplyBoundClaim.claims[0], $multiplyBoundClaim.claims[0])
    Expect 'multiply bound registered claim is rejected' (@(Get-GovernanceManifestIssues $multiplyBoundClaim $sources @($claim)) -contains 'multiply-bound-claim:F-DOC-T')

    if ($failed.Count -gt 0) { throw "Governance source/body manifest self-test failure: $($failed -join ', ')" }
    Write-Host "Governance source/body manifest self-tests: $($script:manifestSelfTestCount)/$($script:manifestSelfTestCount) checks passed."
}

if (-not $SkipSelfTests) { Invoke-ManifestSelfTests }

$sources = @(Get-GovernedSources)
$registrationIssues = @(Get-SourceRegistrationIssues $sources)
if ($registrationIssues.Count -gt 0) {
    throw "CompleteSourceDestinationManifestCoversEveryGovernedBody failed source registration: $($registrationIssues -join ', ')"
}
$claims = @(Read-OperativeClaims (Join-Path $PSScriptRoot 'OperativeClaims.tsv'))

if ($WriteManifest) {
    $generated = New-GovernanceManifest $sources $claims
    $json = ($generated | ConvertTo-Json -Depth 12).Replace("`r`n", "`n") + "`n"
    [System.IO.File]::WriteAllText($manifestPath, $json, $utf8NoBom)
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'CompleteSourceDestinationManifestCoversEveryGovernedBody failed: Tools/GovernanceSourceBodyManifest.json is missing.'
}

try { $manifest = [System.IO.File]::ReadAllText($manifestPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json }
catch { throw "CompleteSourceDestinationManifestCoversEveryGovernedBody failed: manifest JSON is invalid. $($_.Exception.Message)" }

$sourceReference = [ref]$sources
$manifest = Apply-DeliberateViolation $manifest $sourceReference $DeliberateViolation
$sources = @($sourceReference.Value)
$issues = @(Get-GovernanceManifestIssues $manifest $sources $claims)
if ($issues.Count -gt 0) {
    throw "CompleteSourceDestinationManifestCoversEveryGovernedBody failed with $($issues.Count) issue(s): $($issues -join ', ')"
}

Write-Host "CompleteSourceDestinationManifestCoversEveryGovernedBody passed: $($manifest.balance.sourceCount) sources, $($manifest.balance.bodyCount) bodies, $($manifest.balance.claimCount) claims, $($manifest.balance.bodyByteCount) bytes reconstructed at $($manifest.sourceRevision)."
