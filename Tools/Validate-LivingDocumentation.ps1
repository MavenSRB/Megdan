param(
    [switch] $SkipSelfTests
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$docsRoot = Join-Path $projectRoot 'Docs\AI'
$issues = New-Object System.Collections.Generic.List[object]
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Add-Issue([string] $Severity, [string] $Code, [string] $Message) {
    $issues.Add([pscustomobject]@{ Severity = $Severity; Code = $Code; Message = $Message })
}

function Read-Utf8([string] $Path) {
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Get-TextHash([string] $Text) {
    $bytes = $utf8NoBom.GetBytes($Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Get-FileHashLower([string] $Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-WordCount([string] $Text) {
    return ([regex]::Matches($Text, '\b[\p{L}\p{N}][\p{L}\p{N}_''.-]*\b')).Count
}

function Get-Anchor([string] $Heading) {
    $anchor = $Heading.ToLowerInvariant()
    $anchor = [regex]::Replace($anchor, '[^\p{L}\p{N}\s-]', '')
    return [regex]::Replace($anchor.Trim(), '\s+', '-')
}

function Get-RelativePath([string] $BasePath, [string] $TargetPath) {
    $baseUri = New-Object System.Uri(($BasePath.TrimEnd('\') + '\'))
    $targetUri = New-Object System.Uri($TargetPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '\')
}

function Get-Limit([string] $RelativePath) {
    $p = $RelativePath.Replace('\', '/')
    if ($p -eq 'Docs/AI/SESSION_BRIEF.md') { return @(2000, 20KB) }
    if ($p -eq 'Docs/AI/PROJECT_CONTEXT.md' -or $p -like 'Docs/AI/Project/*.md') { return @(4500, 40KB) }
    if ($p -eq 'Docs/AI/ROADMAP.md' -or $p -like 'Docs/AI/Roadmap/*.md') { return @(15000, 128KB) }
    if ($p -eq 'Docs/AI/TESTING.md' -or $p -like 'Docs/AI/Testing/*.md') { return @(7000, 80KB) }
    if ($p -eq 'Docs/AI/CARD_SYSTEM_ARCHITECTURE.md' -or $p -eq 'Docs/AI/CARD_EFFECT_CAPABILITIES.md' -or $p -like 'Docs/AI/Card-System/*.md') { return @(7000, 64KB) }
    if ($p -eq 'Docs/AI/RULES_LEDGER.md' -or $p -like 'Docs/AI/Rules/*.md') { return @(9000, 80KB) }
    if ($p -eq 'Docs/AI/DOCUMENTATION_GOVERNANCE.md' -or $p -eq 'Docs/AI/TECHNICAL_RISKS.md') { return @(4500, 40KB) }
    return $null
}

function Test-ThresholdValues([int] $Words, [int] $Bytes, [int] $WordLimit, [int] $ByteLimit) {
    if ($Words -gt $WordLimit -or $Bytes -gt $ByteLimit) { return 'hard' }
    if ($Words -ge [math]::Ceiling($WordLimit * 0.85) -or $Bytes -ge [math]::Ceiling($ByteLimit * 0.85)) { return 'warning' }
    return 'ok'
}

function Get-Headings([string] $Text) {
    $result = @{}
    foreach ($match in [regex]::Matches($Text, '(?m)^(#{1,6})\s+(.+?)\s*$')) {
        $anchor = Get-Anchor $match.Groups[2].Value
        $result[$anchor] = $true
    }
    return $result
}

function Get-SectionByHeading([string] $Text, [string] $Heading) {
    $escaped = [regex]::Escape($Heading)
    $match = [regex]::Match($Text, "(?m)^(#{1,6})\s+$escaped\s*$")
    if (-not $match.Success) { return $null }
    $level = $match.Groups[1].Value.Length
    $tailStart = $match.Index + $match.Length
    $next = [regex]::Match($Text.Substring($tailStart), "(?m)^#{1,$level}\s+")
    $end = if ($next.Success) { $tailStart + $next.Index } else { $Text.Length }
    return $Text.Substring($match.Index, $end - $match.Index).TrimEnd()
}

function Get-CompletionBody([string] $RoadmapText, [string] $Milestone) {
    $escaped = [regex]::Escape($Milestone)
    $milestoneMatch = [regex]::Match($RoadmapText, "(?m)^### \[[^\]]+\] $escaped\s*$")
    if (-not $milestoneMatch.Success) { return $null }
    $tailStart = $milestoneMatch.Index + $milestoneMatch.Length
    $next = [regex]::Match($RoadmapText.Substring($tailStart), '(?m)^### \[')
    $end = if ($next.Success) { $tailStart + $next.Index } else { $RoadmapText.Length }
    $block = $RoadmapText.Substring($tailStart, $end - $tailStart)
    $note = [regex]::Match($block, '(?ms)^Completion note[^\r\n]*(?:\r?\n(?!\r?\n|### |## ).*)*')
    if (-not $note.Success) { return $null }
    return $note.Value.TrimEnd()
}

function Test-RouteValues([string] $Session, [string] $RoadmapCorpus, [string] $Config) {
    $codes = New-Object System.Collections.Generic.List[string]
    $currentMatches = [regex]::Matches($Session, '(?m)^- Current completed milestone: \*\*([^*]+)\*\*\.$')
    $activeMatches = [regex]::Matches($Session, '(?m)^- Active authorized milestone: \*\*([^*]+)\*\*(?:,[^\r\n]*)?\.$')
    $nextMatches = [regex]::Matches($Session, '(?m)^- Next proposed milestone: \*\*([^*]+)\*\*\.$')
    $profileMatches = [regex]::Matches($Session, '(?m)^- Required Codex profile for next milestone: \*\*GPT-5\.6 (Sol|Terra) with ([^*]+) reasoning\*\*\.$')
    if ($currentMatches.Count -ne 1 -or $activeMatches.Count -ne 1 -or $nextMatches.Count -ne 1 -or $profileMatches.Count -ne 1) {
        $codes.Add('missing-route')
        return $codes.ToArray()
    }
    $current = $currentMatches[0].Groups[1].Value
    $active = $activeMatches[0].Groups[1].Value
    $next = $nextMatches[0].Groups[1].Value
    if (-not [regex]::IsMatch($RoadmapCorpus, "(?m)^### \[x\] $([regex]::Escape($current))(?:\s|$)")) { $codes.Add('current-status') }
    $inProgressMatches = [regex]::Matches($RoadmapCorpus, '(?m)^### \[~\] (\S+)(?:\s|$)')
    if ($active -eq 'none') {
        if ($inProgressMatches.Count -ne 0) { $codes.Add('active-status') }
    }
    elseif ($inProgressMatches.Count -ne 1 -or $inProgressMatches[0].Groups[1].Value -ne $active -or $active -ne $next) {
        $codes.Add('active-status')
    }
    $nextBlock = [regex]::Match($RoadmapCorpus, "(?ms)^### \[[ !~]\] $([regex]::Escape($next))(?:\s[^\r\n]*)?\r?\n(.*?)(?=^### |\z)")
    if (-not $nextBlock.Success) {
        $codes.Add('next-status')
        return $codes.ToArray()
    }
    $expectedModel = if ($profileMatches[0].Groups[1].Value -eq 'Sol') { 'gpt-5.6-sol' } else { 'gpt-5.6-terra' }
    $expectedReasoning = $profileMatches[0].Groups[2].Value.Trim().ToLowerInvariant().Replace(' ', '')
    $roadmapProfile = [regex]::Match($nextBlock.Value, '(?m)^Required Codex profile: GPT-5\.6 (Sol|Terra) with ([^.\r\n]+) reasoning\.$')
    $configModel = [regex]::Match($Config, '(?m)^model\s*=\s*"([^"]+)"').Groups[1].Value
    $configReasoning = [regex]::Match($Config, '(?m)^model_reasoning_effort\s*=\s*"([^"]+)"').Groups[1].Value
    $configMilestone = [regex]::Match($Config, '(?m)^# Milestone:\s*(\S+)').Groups[1].Value
    if (-not $roadmapProfile.Success -or $roadmapProfile.Groups[1].Value -ne $profileMatches[0].Groups[1].Value -or $roadmapProfile.Groups[2].Value.Trim().ToLowerInvariant().Replace(' ', '') -ne $expectedReasoning -or $configModel -ne $expectedModel -or $configReasoning -ne $expectedReasoning -or $configMilestone -ne $next) {
        $codes.Add('mismatched-profile')
    }
    return $codes.ToArray()
}

function Test-DuplicateBodies([string[]] $Bodies, [string] $ActiveCorpus) {
    foreach ($body in $Bodies) {
        if ($body.Length -ge 80 -and $ActiveCorpus.Contains($body)) { return $false }
    }
    return $true
}

function Test-PreservationCount([string] $Name, [int] $Before, [int] $After) {
    return $After -ge $Before
}

function Invoke-SelfTests {
    $failed = New-Object System.Collections.Generic.List[string]
    function Expect([string] $Name, [bool] $Condition) {
        if (-not $Condition) { $failed.Add($Name) }
    }

    Expect 'valid structure' ((Test-ThresholdValues 10 100 100 1000) -eq 'ok')
    Expect 'warning threshold' ((Test-ThresholdValues 85 100 100 1000) -eq 'warning')
    Expect 'hard-threshold failure' ((Test-ThresholdValues 101 100 100 1000) -eq 'hard')

    $validSession = "- Current completed milestone: **1.0**.`n- Active authorized milestone: **none**.`n- Next proposed milestone: **1.1**.`n- Required Codex profile for next milestone: **GPT-5.6 Sol with medium reasoning**."
    $validRoadmap = "### [x] 1.0 Done`n`n### [ ] 1.1 Next`n`nRequired Codex profile: GPT-5.6 Sol with medium reasoning.`n"
    $activeSession = $validSession -replace 'Active authorized milestone: \*\*none\*\*', 'Active authorized milestone: **1.1**, in progress'
    $activeRoadmap = $validRoadmap -replace '### \[ \] 1\.1', '### [~] 1.1'
    $validConfig = "# Milestone: 1.1`nmodel = `"gpt-5.6-sol`"`nmodel_reasoning_effort = `"medium`""
    Expect 'valid route structure' ((Test-RouteValues $validSession $validRoadmap $validConfig).Count -eq 0)
    Expect 'valid in-progress route' ((Test-RouteValues $activeSession $activeRoadmap $validConfig).Count -eq 0)
    Expect 'missing route' ((Test-RouteValues ($validSession -replace '(?m)^- Next proposed.*\r?\n?', '') $validRoadmap $validConfig) -contains 'missing-route')
    Expect 'mismatched profile' ((Test-RouteValues $validSession $validRoadmap ($validConfig -replace 'medium', 'high')) -contains 'mismatched-profile')
    Expect 'active milestone left proposed' ((Test-RouteValues $activeSession $validRoadmap $validConfig) -contains 'active-status')
    Expect 'orphaned in-progress milestone' ((Test-RouteValues $validSession $activeRoadmap $validConfig) -contains 'active-status')
    $multipleActiveRoadmap = $activeRoadmap + "`n### [~] 1.2 Also active`n`nRequired Codex profile: GPT-5.6 Sol with medium reasoning.`n"
    Expect 'multiple in-progress milestones' ((Test-RouteValues $activeSession $multipleActiveRoadmap $validConfig) -contains 'active-status')

    $virtualTargets = @('index.md','archive.md#record')
    Expect 'broken archive/shard link' (-not ($virtualTargets -contains 'missing.md#record'))
    Expect 'duplicate historical body' (-not (Test-DuplicateBodies @('This historical completion body is deliberately longer than eighty characters so exact duplicate detection exercises the production rule.') 'active prefix This historical completion body is deliberately longer than eighty characters so exact duplicate detection exercises the production rule. suffix'))

    $manifestDestinations = @('archive.md#record')
    Expect 'missing manifest destination' (-not ($manifestDestinations -contains 'missing.md#record'))
    $goodHash = Get-TextHash 'verbatim'
    Expect 'bad verbatim hash' ($goodHash -ne (Get-TextHash 'changed'))
    $requiredInventory = @('milestones','rules','tests')
    $fixtureInventory = @('milestones','rules')
    Expect 'missing preservation-inventory item' (@($requiredInventory | Where-Object { $fixtureInventory -notcontains $_ }).Count -eq 1)
    Expect 'preserved structural baseline' (Test-PreservationCount 'milestones' 10 10)
    Expect 'additive milestone definition' (Test-PreservationCount 'milestones' 10 11)
    Expect 'additive acceptance check' (Test-PreservationCount 'acceptance' 10 11)
    Expect 'additive non-goal' (Test-PreservationCount 'non-goals' 10 11)
    Expect 'lost milestone definition' (-not (Test-PreservationCount 'milestones' 10 9))
    Expect 'lost acceptance check' (-not (Test-PreservationCount 'acceptance' 10 9))
    Expect 'lost non-goal' (-not (Test-PreservationCount 'non-goals' 10 9))
    Expect 'new milestone profile declaration' (Test-PreservationCount 'profiles' 10 11)
    Expect 'lost milestone profile declaration' (-not (Test-PreservationCount 'profiles' 10 9))

    if ($failed.Count -gt 0) { throw "Living-document validator self-test failure: $($failed -join ', ')" }
    Write-Host 'Self-tests: 11/11 fixture classes passed.'
}

if (-not $SkipSelfTests) { Invoke-SelfTests }

# Active leaf thresholds.
$activeFiles = Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter '*.md' | Where-Object { $_.FullName -notlike '*\Archive\*' -and $_.FullName -notlike '*\Maps\*' }
foreach ($file in $activeFiles) {
    $relative = Get-RelativePath $projectRoot $file.FullName
    $limit = Get-Limit $relative
    if ($null -eq $limit) { continue }
    $text = Read-Utf8 $file.FullName
    $words = Get-WordCount $text
    $state = Test-ThresholdValues $words $file.Length $limit[0] $limit[1]
    if ($state -eq 'hard') { Add-Issue 'ERROR' 'hard-threshold' "$relative is $words words/$($file.Length) bytes; hard limit is $($limit[0])/$($limit[1])." }
    elseif ($state -eq 'warning') { Add-Issue 'WARNING' 'warning-threshold' "$relative is at or above 85%: $words words/$($file.Length) bytes." }
}

# Required headings and canonical indexes.
$requiredHeadings = @{
    'SESSION_BRIEF.md' = @('Purpose and authority','Current implementation','Current authorization and next route','Durable boundaries','Targeted routing')
    'DOCUMENTATION_GOVERNANCE.md' = @('Purpose and invariants','Ownership matrix','Active leaf thresholds','Active risk lifecycle','History and archive workflow','Manifest and preservation rules','Required handoff and phase-gate review')
    'TECHNICAL_RISKS.md' = @('Active risks','Resolved-risk history')
    'ROADMAP.md' = @('Status legend','Execution rule','Active milestone definition index')
    'PROJECT_CONTEXT.md' = @('Canonical active shards','Historical status snapshot')
    'TESTING.md' = @('Canonical active shards','Historical recipes','Required launcher')
    'CARD_SYSTEM_ARCHITECTURE.md' = @('Canonical active shards')
    'RULES_LEDGER.md' = @('Authority hierarchy','Approved deviations','Rule-system ledger','Open source and rules questions')
    'CARD_EFFECT_CAPABILITIES.md' = @('Contract required for every capability','Implementation boundary')
}
foreach ($relative in $requiredHeadings.Keys) {
    $path = Join-Path $docsRoot $relative
    if (-not (Test-Path -LiteralPath $path)) { Add-Issue 'ERROR' 'missing-file' "Missing $relative."; continue }
    $headings = Get-Headings (Read-Utf8 $path)
    foreach ($heading in $requiredHeadings[$relative]) {
        if (-not $headings.ContainsKey((Get-Anchor $heading))) { Add-Issue 'ERROR' 'missing-heading' "$relative is missing '$heading'." }
    }
}

# Exactly one current/active/next/profile route and roadmap/session/config agreement.
$session = Read-Utf8 (Join-Path $docsRoot 'SESSION_BRIEF.md')
$roadmapCorpus = ((Get-ChildItem (Join-Path $docsRoot 'Roadmap') -File -Filter '*.md' | Sort-Object Name | ForEach-Object { Read-Utf8 $_.FullName }) -join "`n")
$config = Read-Utf8 (Join-Path $projectRoot '.codex\config.toml')
$routeCodes = Test-RouteValues $session $roadmapCorpus $config
foreach ($code in $routeCodes) { Add-Issue 'ERROR' $code 'SESSION_BRIEF.md, the next roadmap milestone, and .codex/config.toml do not agree.' }
$routeMarkerCounts = @{
    'Current completed milestone:' = 0
    'Active authorized milestone:' = 0
    'Next proposed milestone:' = 0
    'Required Codex profile for next milestone:' = 0
}
foreach ($file in $activeFiles) {
    $text = Read-Utf8 $file.FullName
    foreach ($marker in @($routeMarkerCounts.Keys)) { $routeMarkerCounts[$marker] += ([regex]::Matches($text, [regex]::Escape($marker))).Count }
}
foreach ($marker in $routeMarkerCounts.Keys) { if ($routeMarkerCounts[$marker] -ne 1) { Add-Issue 'ERROR' 'duplicate-route' "Expected exactly one '$marker' route, found $($routeMarkerCounts[$marker])." } }

# Markdown paths and anchors for active indexes and archives.
$linkFiles = Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter '*.md' | Where-Object { $_.FullName -notlike '*\Archive\Pre-Governance\*' }
foreach ($file in $linkFiles) {
    $text = Read-Utf8 $file.FullName
    foreach ($match in [regex]::Matches($text, '(?<!\!)\[[^\]]+\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value.Trim()
        if ($target -match '^(https?:|mailto:)' -or $target.StartsWith('#')) { continue }
        $parts = $target.Split('#', 2)
        $targetPath = Join-Path $file.DirectoryName $parts[0]
        if (-not (Test-Path -LiteralPath $targetPath)) { Add-Issue 'ERROR' 'broken-link' "$(Get-RelativePath $projectRoot $file.FullName) -> $target"; continue }
        if ($parts.Count -eq 2 -and $parts[1].Length -gt 0 -and (Get-Item $targetPath).PSIsContainer -eq $false) {
            $headings = Get-Headings (Read-Utf8 $targetPath)
            if (-not $headings.ContainsKey($parts[1])) { Add-Issue 'ERROR' 'broken-anchor' "$(Get-RelativePath $projectRoot $file.FullName) -> $target" }
        }
    }
}

# Immutable baseline inventory.
$baselineRoot = Join-Path $docsRoot 'Archive\Pre-Governance'
$sumsPath = Join-Path $baselineRoot 'SHA256SUMS.md'
$sumText = Read-Utf8 $sumsPath
$sumRows = [regex]::Matches($sumText, '(?m)^\| `([^`]+)` \| (\d+) \| `([0-9a-f]{64})` \|$')
if ($sumRows.Count -ne 9) { Add-Issue 'ERROR' 'baseline-inventory' "Expected 9 immutable baseline rows, found $($sumRows.Count)." }
foreach ($row in $sumRows) {
    $path = Join-Path $baselineRoot $row.Groups[1].Value
    if (-not (Test-Path -LiteralPath $path)) { Add-Issue 'ERROR' 'baseline-missing' "Missing preserved $($row.Groups[1].Value)."; continue }
    $file = Get-Item $path
    if ($file.Length -ne [int64]$row.Groups[2].Value -or (Get-FileHashLower $path) -ne $row.Groups[3].Value) { Add-Issue 'ERROR' 'baseline-hash' "Immutable baseline mismatch: $($row.Groups[1].Value)." }
}

# Migration destinations and exact hashes.
$manifestPath = Join-Path $docsRoot 'Archive\MIGRATION_MANIFEST.md'
$manifestText = Read-Utf8 $manifestPath
$manifestRows = [regex]::Matches($manifestText, '(?m)^\| `([^`]+)` \| (.*?) \| `([^`]+)` \| `(verbatim archive|authoritative shard|non-authoritative routing-summary replacement)` \| `([0-9a-f]*)` \| `([0-9a-f]*)` \|$')
if ($manifestRows.Count -lt 150) { Add-Issue 'ERROR' 'manifest-coverage' "Migration manifest has only $($manifestRows.Count) parsed rows." }
$completionBodies = New-Object System.Collections.Generic.List[string]
foreach ($row in $manifestRows) {
    $sourceRelative = $row.Groups[1].Value
    $heading = $row.Groups[2].Value.Replace('\|','|')
    $destination = $row.Groups[3].Value
    $treatment = $row.Groups[4].Value
    $sourceHash = $row.Groups[5].Value
    $destinationHash = $row.Groups[6].Value
    $destinationParts = $destination.Split('#', 2)
    $destinationPath = Join-Path $docsRoot $destinationParts[0]
    if (-not (Test-Path -LiteralPath $destinationPath)) { Add-Issue 'ERROR' 'manifest-destination' "Missing manifest destination $destination."; continue }
    if ($destinationParts.Count -eq 2 -and -not (Get-Headings (Read-Utf8 $destinationPath)).ContainsKey($destinationParts[1])) { Add-Issue 'ERROR' 'manifest-anchor' "Missing manifest anchor $destination."; continue }
    if ($treatment -eq 'verbatim archive' -or ($sourceHash.Length -gt 0 -and $destinationHash.Length -gt 0)) {
        $baselinePath = Join-Path $baselineRoot ([IO.Path]::GetFileName($sourceRelative))
        if (-not (Test-Path -LiteralPath $baselinePath)) { Add-Issue 'ERROR' 'manifest-source' "Missing baseline source for $sourceRelative / $heading."; continue }
        $baselineText = Read-Utf8 $baselinePath
        $sourceSection = $null
        if ($sourceRelative -eq 'ROADMAP.md' -and $heading.EndsWith(' / Completion note')) {
            $milestone = $heading.Substring(0, $heading.Length - ' / Completion note'.Length)
            $sourceSection = Get-CompletionBody $baselineText $milestone
            if ($null -ne $sourceSection) { $completionBodies.Add($sourceSection) }
        }
        else { $sourceSection = Get-SectionByHeading $baselineText $heading }
        if ($null -eq $sourceSection) { Add-Issue 'ERROR' 'manifest-source-section' "Cannot find baseline section $sourceRelative / $heading."; continue }
        if ((Get-TextHash $sourceSection) -ne $sourceHash) { Add-Issue 'ERROR' 'manifest-source-hash' "$sourceRelative / $heading source hash mismatch." }
        $destinationText = Read-Utf8 $destinationPath
        $destinationSection = $null
        if ($sourceRelative -eq 'ROADMAP.md' -and $heading.EndsWith(' / Completion note')) {
            $destinationHeading = $null
            foreach ($match in [regex]::Matches($destinationText, '(?m)^## (.+)$')) { if ((Get-Anchor $match.Groups[1].Value.Trim()) -eq $destinationParts[1]) { $destinationHeading = $match.Groups[1].Value.Trim(); break } }
            if ($null -ne $destinationHeading) {
                $wrapped = Get-SectionByHeading $destinationText $destinationHeading
                $destinationSection = [regex]::Replace($wrapped, '(?s)^##[^\r\n]*\r?\n\s*', '').TrimEnd()
            }
        }
        else { $destinationSection = Get-SectionByHeading $destinationText $heading }
        if ($null -eq $destinationSection) { Add-Issue 'ERROR' 'manifest-destination-section' "Cannot find destination section $destination."; continue }
        $actualDestinationHash = Get-TextHash $destinationSection
        if ($actualDestinationHash -ne $destinationHash) { Add-Issue 'ERROR' 'manifest-destination-hash' "$destination hash mismatch." }
        if ($treatment -eq 'verbatim archive' -and ($sourceHash -ne $destinationHash -or $sourceHash.Length -ne 64)) { Add-Issue 'ERROR' 'verbatim-hash' "$sourceRelative / $heading is not lossless." }
    }
}

# Completed-note bodies must not be copied into active noncanonical files.
$activeCorpus = ($activeFiles | ForEach-Object { Read-Utf8 $_.FullName }) -join "`n"
if (-not (Test-DuplicateBodies $completionBodies.ToArray() $activeCorpus)) { Add-Issue 'ERROR' 'duplicate-history' 'An archived completion-note body is duplicated in active documentation.' }

# Preservation category coverage and stable counts.
$inventoryPath = Join-Path $docsRoot 'Archive\PRESERVATION_INVENTORY.md'
if (-not (Test-Path -LiteralPath $inventoryPath)) { Add-Issue 'ERROR' 'missing-preservation-inventory' 'Archive/PRESERVATION_INVENTORY.md is missing.' }
else {
    $inventoryText = Read-Utf8 $inventoryPath
    $requiredCategories = @('milestone-definitions','milestone-profiles','acceptance-checks','non-goals','rule-authority-and-status','deviations-and-open-questions','card-contracts','effect-contracts','test-commands-and-gates','historical-results','rights-and-privacy','active-leaf-measurements')
    foreach ($category in $requiredCategories) {
        if (-not [regex]::IsMatch($inventoryText, "(?m)^\| ``$([regex]::Escape($category))`` \|.*\| ``PASS`` \|$")) { Add-Issue 'ERROR' 'preservation-coverage' "Missing PASS inventory row: $category." }
    }
}

# Structural baseline floors prove that sharding did not delete milestone checks or assigned profiles while allowing newly authorized milestones, acceptance checks, non-goals, and profile declarations to be added.
$baselineRoadmap = Read-Utf8 (Join-Path $baselineRoot 'ROADMAP.md')
$countPatterns = @{
    'milestones' = '(?m)^### \[[x!~ ]\] \S+'
    'profiles' = '(?m)^Required Codex profile:'
    'acceptance' = '(?m)^Acceptance checks:'
    'non-goals' = '(?m)^Non-goals:'
}
foreach ($name in $countPatterns.Keys) {
    $before = ([regex]::Matches($baselineRoadmap, $countPatterns[$name])).Count
    $after = ([regex]::Matches($roadmapCorpus, $countPatterns[$name])).Count
    if (-not (Test-PreservationCount $name $before $after)) { Add-Issue 'ERROR' 'roadmap-preservation' "$name count changed from $before to $after." }
}
if ($completionBodies.Count -ne ([regex]::Matches($baselineRoadmap, '(?m)^Completion note')).Count) { Add-Issue 'ERROR' 'history-preservation' 'Not every baseline completion note has a hashed archive manifest row.' }

$warnings = @($issues | Where-Object Severity -eq 'WARNING')
$errors = @($issues | Where-Object Severity -eq 'ERROR')
foreach ($issue in $issues) { Write-Host "[$($issue.Severity)] $($issue.Code): $($issue.Message)" }
if ($errors.Count -gt 0) {
    Write-Error "Living-document validation failed with $($errors.Count) error(s) and $($warnings.Count) warning(s)."
    exit 1
}
Write-Host "Living-document validation passed: $($activeFiles.Count) governed active leaves, $($manifestRows.Count) manifest rows, $($completionBodies.Count) archived completion records, $($warnings.Count) warning(s)."
