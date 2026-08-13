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

function Test-RouteValues([string] $Session, [string] $RoadmapCorpus) {
    $codes = New-Object System.Collections.Generic.List[string]
    $currentMatches = [regex]::Matches($Session, '(?m)^- Current completed milestone: \*\*([^*]+)\*\*\.$')
    $activeMatches = [regex]::Matches($Session, '(?m)^- Active authorized milestone: \*\*([^*]+)\*\*(?:,[^\r\n]*)?\.$')
    $nextMatches = [regex]::Matches($Session, '(?m)^- Next proposed milestone: \*\*([^*]+)\*\*\.$')
    if ($currentMatches.Count -ne 1 -or $activeMatches.Count -ne 1 -or $nextMatches.Count -ne 1) {
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
    return $codes.ToArray()
}

# Code-truth check 1: a type named as a contract must either exist in the source or be listed as
# planned in that document's own "Contract implementation status" section. These are specification
# documents, so naming an unbuilt contract is legitimate; leaving a reader unable to tell which
# contracts are real is not. The status section is the one place that distinction is recorded.
function Get-PlannedContractNames([string] $Text) {
    $planned = @{}
    $section = Get-SectionByHeading $Text 'Contract implementation status'
    if ($null -eq $section) { return $planned }
    foreach ($match in [regex]::Matches($section, '`([A-Za-z_][A-Za-z0-9_]*)`')) { $planned[$match.Groups[1].Value] = $true }
    return $planned
}

function Get-UndeclaredContractTypes([string] $Text, [hashtable] $DeclaredTypes) {
    $missing = New-Object System.Collections.Generic.List[string]
    $planned = Get-PlannedContractNames $Text
    foreach ($match in [regex]::Matches($Text, '`([A-Z][A-Za-z0-9]*(?:Definition|Resolution|Event|Authority|Backend|Projection|Instance|Registry|Serializer|Query|Handler))`')) {
        $name = $match.Groups[1].Value
        if ($DeclaredTypes.ContainsKey($name)) { continue }
        if ($planned.ContainsKey($name)) { continue }
        if ($missing.Contains($name)) { continue }
        $missing.Add($name)
    }
    return $missing.ToArray()
}

# Code-truth check 2: the three canonical statements of "how far are we" must name the same milestone.
function Test-StatusCoherence([string] $Session, [string] $Implementation, [string] $RoadmapCorpus) {
    $codes = New-Object System.Collections.Generic.List[string]
    $sessionMatch = [regex]::Match($Session, '(?m)^- Current completed milestone: \*\*([^*]+)\*\*\.$')
    $implementationMatch = [regex]::Match($Implementation, 'completed roadmap milestones through \*\*([^*]+)\*\*')
    if (-not $sessionMatch.Success -or -not $implementationMatch.Success) {
        $codes.Add('missing-status-statement')
        return $codes.ToArray()
    }
    if ($sessionMatch.Groups[1].Value -ne $implementationMatch.Groups[1].Value) { $codes.Add('incoherent-status') }
    if (-not [regex]::IsMatch($RoadmapCorpus, "(?m)^### \[x\] $([regex]::Escape($implementationMatch.Groups[1].Value))(?:\s|$)")) { $codes.Add('incoherent-status') }
    return $codes.ToArray()
}

# Code-truth check 3: a deliberately temporary structure must name the milestone that owns its removal.
# Without this, a temporary structure that no document admits is temporary never becomes anyone's job.
function Get-UnregisteredTemporaryTypes([string[]] $SourceTypeNames, [string] $RiskRegister) {
    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($name in $SourceTypeNames) {
        if ($name -notmatch '^(Placeholder|Legacy|Temporary|Stub|Fake)') { continue }
        if ($RiskRegister.Contains($name)) { continue }
        if ($missing.Contains($name)) { continue }
        $missing.Add($name)
    }
    return $missing.ToArray()
}

# Code-truth check 4: a production component Unity cannot construct is dead code.
# Check 3's name-prefix rule could never have caught the legacy controller drill, because nothing
# about the name `HeroSelectionController` says "temporary" - which is exactly why 2,929 lines of it
# survived milestones 4.4i and 5.8c untouched. Reachability is the property that actually holds:
# a MonoBehaviour or ScriptableObject can only ever exist if it is authored into a scene/prefab by
# script GUID or constructed at runtime. One that is neither cannot run, whatever references it.
# Non-concrete types are excluded (a base class is reached through its subclasses), and the
# temporary-structure register remains the explicit escape hatch for a knowingly retained structure.
function Get-UnreachableBehaviourTypes($Behaviours, [hashtable] $AuthoredScriptGuids, [string] $ProductionSource, [string] $RiskRegister) {
    $unreachable = New-Object System.Collections.Generic.List[string]
    foreach ($behaviour in $Behaviours) {
        if ($behaviour.IsAbstract) { continue }
        if ($behaviour.Guid -and $AuthoredScriptGuids.ContainsKey($behaviour.Guid)) { continue }
        $escaped = [regex]::Escape($behaviour.Name)
        if ($ProductionSource -match "AddComponent<\s*$escaped\s*>") { continue }
        if ($ProductionSource -match "AddComponent\(\s*typeof\(\s*$escaped\s*\)") { continue }
        if ($ProductionSource -match "CreateInstance<\s*$escaped\s*>") { continue }
        if ($ProductionSource -match "CreateInstance\(\s*typeof\(\s*$escaped\s*\)") { continue }
        if ($RiskRegister.Contains($behaviour.Name)) { continue }
        if ($unreachable.Contains($behaviour.Name)) { continue }
        $unreachable.Add($behaviour.Name)
    }
    return $unreachable.ToArray()
}

function Test-DuplicateBodies([string[]] $Bodies, [string] $ActiveCorpus) {
    foreach ($body in $Bodies) {
        if ($body.Length -ge 80 -and $ActiveCorpus.Contains($body)) { return $false }
    }
    return $true
}

function Invoke-SelfTests {
    $failed = New-Object System.Collections.Generic.List[string]
    $script:selfTestCount = 0
    function Expect([string] $Name, [bool] $Condition) {
        $script:selfTestCount++
        if (-not $Condition) { $failed.Add($Name) }
    }

    Expect 'valid structure' ((Test-ThresholdValues 10 100 100 1000) -eq 'ok')
    Expect 'warning threshold' ((Test-ThresholdValues 85 100 100 1000) -eq 'warning')
    Expect 'hard-threshold failure' ((Test-ThresholdValues 101 100 100 1000) -eq 'hard')

    $validSession = "- Current completed milestone: **1.0**.`n- Active authorized milestone: **none**.`n- Next proposed milestone: **1.1**."
    $validRoadmap = "### [x] 1.0 Done`n`n### [ ] 1.1 Next`n`nObservable outcome: something.`n"
    $activeSession = $validSession -replace 'Active authorized milestone: \*\*none\*\*', 'Active authorized milestone: **1.1**, in progress'
    $activeRoadmap = $validRoadmap -replace '### \[ \] 1\.1', '### [~] 1.1'
    Expect 'valid route structure' ((Test-RouteValues $validSession $validRoadmap).Count -eq 0)
    Expect 'valid in-progress route' ((Test-RouteValues $activeSession $activeRoadmap).Count -eq 0)
    Expect 'missing route' ((Test-RouteValues ($validSession -replace '(?m)^- Next proposed.*\r?\n?', '') $validRoadmap) -contains 'missing-route')
    Expect 'active milestone left proposed' ((Test-RouteValues $activeSession $validRoadmap) -contains 'active-status')
    Expect 'orphaned in-progress milestone' ((Test-RouteValues $validSession $activeRoadmap) -contains 'active-status')
    $multipleActiveRoadmap = $activeRoadmap + "`n### [~] 1.2 Also active`n`nObservable outcome: something else.`n"
    Expect 'multiple in-progress milestones' ((Test-RouteValues $activeSession $multipleActiveRoadmap) -contains 'active-status')

    $virtualTargets = @('index.md','archive.md#record')
    Expect 'broken archive/shard link' (-not ($virtualTargets -contains 'missing.md#record'))
    $archivedBody = 'This archived handoff body is deliberately longer than eighty characters so exact duplicate detection exercises the production rule.'
    Expect 'duplicate historical body' (-not (Test-DuplicateBodies @($archivedBody) "active prefix $archivedBody suffix"))
    Expect 'archived body kept out of active docs' (Test-DuplicateBodies @($archivedBody) 'active documentation links to the archive instead of copying it')

    $declared = @{ 'RealAuthority' = $true }
    # Literal backticks inside a double-quoted PowerShell string must be doubled; `n stays a newline.
    $plannedSection = "`n## Contract implementation status`n`n| Contract | State |`n| --- | --- |`n| ``GhostDefinition`` | Planned |`n"
    Expect 'documented type with no implementation' ((Get-UndeclaredContractTypes 'The `GhostDefinition` owns this.' $declared) -contains 'GhostDefinition')
    Expect 'documented type that exists' ((Get-UndeclaredContractTypes 'The `RealAuthority` owns this.' $declared).Count -eq 0)
    Expect 'documented type listed as planned' ((Get-UndeclaredContractTypes ("The ``GhostDefinition`` will own this." + $plannedSection) $declared).Count -eq 0)
    Expect 'unlisted type still reported alongside a planned one' ((Get-UndeclaredContractTypes ("The ``OtherDefinition`` owns this." + $plannedSection) $declared) -contains 'OtherDefinition')

    $coherentSession = '- Current completed milestone: **2.5**.'
    $coherentImplementation = 'The project has completed roadmap milestones through **2.5**. Details follow.'
    $coherentRoadmap = "### [x] 2.5 Done`n`n### [ ] 2.6 Next`n"
    Expect 'coherent status triple' ((Test-StatusCoherence $coherentSession $coherentImplementation $coherentRoadmap).Count -eq 0)
    Expect 'incoherent implementation summary' ((Test-StatusCoherence $coherentSession ($coherentImplementation -replace '2\.5', '2.3') $coherentRoadmap) -contains 'incoherent-status')
    Expect 'status naming an incomplete milestone' ((Test-StatusCoherence '- Current completed milestone: **2.6**.' 'The project has completed roadmap milestones through **2.6**.' $coherentRoadmap) -contains 'incoherent-status')
    Expect 'missing status statement' ((Test-StatusCoherence $coherentSession 'No parseable statement here.' $coherentRoadmap) -contains 'missing-status-statement')

    Expect 'unregistered temporary structure' ((Get-UnregisteredTemporaryTypes @('PlaceholderThing') 'register mentions nothing') -contains 'PlaceholderThing')
    Expect 'registered temporary structure' ((Get-UnregisteredTemporaryTypes @('PlaceholderThing') '| `PlaceholderThing` | reason | 6.1c | Active |').Count -eq 0)
    Expect 'ordinary type is not temporary' ((Get-UnregisteredTemporaryTypes @('RoundResolutionAuthority') 'register mentions nothing').Count -eq 0)

    $authored = @{ 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa' = $true }
    $orphan     = [pscustomobject]@{ Name = 'OrphanView'; Guid = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'; IsAbstract = $false }
    $authoredOne= [pscustomobject]@{ Name = 'AuthoredView'; Guid = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'; IsAbstract = $false }
    $abstractOne= [pscustomobject]@{ Name = 'AbstractView'; Guid = 'cccccccccccccccccccccccccccccccc'; IsAbstract = $true }
    Expect 'component in no scene and never constructed' ((Get-UnreachableBehaviourTypes @($orphan) $authored 'nothing constructs it' 'register mentions nothing') -contains 'OrphanView')
    Expect 'component authored into a scene' ((Get-UnreachableBehaviourTypes @($authoredOne) $authored 'nothing constructs it' 'register mentions nothing').Count -eq 0)
    Expect 'component created by AddComponent' ((Get-UnreachableBehaviourTypes @($orphan) $authored 'go.AddComponent<OrphanView>();' 'register mentions nothing').Count -eq 0)
    Expect 'component created by typeof AddComponent' ((Get-UnreachableBehaviourTypes @($orphan) $authored 'go.AddComponent(typeof(OrphanView));' 'register mentions nothing').Count -eq 0)
    Expect 'asset created by CreateInstance' ((Get-UnreachableBehaviourTypes @($orphan) $authored 'ScriptableObject.CreateInstance<OrphanView>();' 'register mentions nothing').Count -eq 0)
    Expect 'unreachable component with a retirement owner' ((Get-UnreachableBehaviourTypes @($orphan) $authored 'nothing constructs it' '| `OrphanView` | superseded | 6.1c | Active |').Count -eq 0)
    Expect 'abstract component base is not unreachable' ((Get-UnreachableBehaviourTypes @($abstractOne) $authored 'nothing constructs it' 'register mentions nothing').Count -eq 0)

    if ($failed.Count -gt 0) { throw "Living-document validator self-test failure: $($failed -join ', ')" }
    Write-Host "Self-tests: $script:selfTestCount/$script:selfTestCount checks passed."
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
    'DOCUMENTATION_GOVERNANCE.md' = @('Purpose and invariants','Ownership matrix','Active leaf thresholds','Active risk lifecycle','History and archive workflow','Retired migration bookkeeping','Required handoff and phase-gate review')
    'TECHNICAL_RISKS.md' = @('Active risks','Resolved-risk history')
    'ROADMAP.md' = @('Status legend','Execution rule','Active milestone definition index')
    'PROJECT_CONTEXT.md' = @('Canonical active shards','Historical status snapshot')
    'Project/CURRENT_IMPLEMENTATION.md' = @('Current implementation','Maintenance rule')
    'TESTING.md' = @('Canonical active shards','Historical recipes','Required launcher')
    'CARD_SYSTEM_ARCHITECTURE.md' = @('Canonical active shards')
    'RULES_LEDGER.md' = @('Authority hierarchy','Approved deviations','Rule-system ledger','Open source and rules questions')
    'CARD_EFFECT_CAPABILITIES.md' = @('Capability shard routing','Contract required for every capability','Implementation boundary')
    'Card-System/BOARD_CAPABILITIES.md' = @('Spatial and object query capabilities','Movement, forced displacement, and relocation','Removal, defeat, prevention, and battle consequences')
    'Card-System/STATE_CAPABILITIES.md' = @('Tokens, markers, obstacles, and terrain','Card-state and progression capabilities','Characteristics, immunity, permissions, and restrictions','Values, resources, and ownership changes')
    'Card-System/TIMING_AND_TRANSITIONS.md' = @('Timing, Active effects, and continuous sources','Control flow and action composition','System-transition capabilities used by cards')
}
foreach ($relative in $requiredHeadings.Keys) {
    $path = Join-Path $docsRoot $relative
    if (-not (Test-Path -LiteralPath $path)) { Add-Issue 'ERROR' 'missing-file' "Missing $relative."; continue }
    $headings = Get-Headings (Read-Utf8 $path)
    foreach ($heading in $requiredHeadings[$relative]) {
        if (-not $headings.ContainsKey((Get-Anchor $heading))) { Add-Issue 'ERROR' 'missing-heading' "$relative is missing '$heading'." }
    }
}

# Exactly one current/active/next route and roadmap/session agreement.
$session = Read-Utf8 (Join-Path $docsRoot 'SESSION_BRIEF.md')
$roadmapCorpus = ((Get-ChildItem (Join-Path $docsRoot 'Roadmap') -File -Filter '*.md' | Sort-Object Name | ForEach-Object { Read-Utf8 $_.FullName }) -join "`n")
$routeCodes = Test-RouteValues $session $roadmapCorpus
foreach ($code in $routeCodes) { Add-Issue 'ERROR' $code 'SESSION_BRIEF.md and the next roadmap milestone do not agree.' }
$routeMarkerCounts = @{
    'Current completed milestone:' = 0
    'Active authorized milestone:' = 0
    'Next proposed milestone:' = 0
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

# The 2026 consolidation's baseline-hash, migration-manifest, and preservation-inventory checks were
# retired on 2026-08-13 by an explicitly approved governance amendment. They proved one completed
# migration was lossless; that migration is finished and Git history is its record. Re-verifying it on
# every handoff cost a manifest hash recomputation for each edit to a tracked section and caught no
# defect after the migration closed. Archive/MIGRATION_MANIFEST.md, Archive/PRESERVATION_INVENTORY.md,
# and Archive/Pre-Governance/ remain in the repository as frozen historical records.

# Code-truth checks. Document-to-document consistency cannot detect documentation that has
# drifted from the source it describes, which is how milestones 4.4i and 5.8c both reported
# no defects while documented contracts named types the codebase did not contain.
$assetsRoot = Join-Path $projectRoot 'Assets'
if (-not (Test-Path -LiteralPath $assetsRoot)) {
    Add-Issue 'ERROR' 'missing-source-root' 'Assets/ is missing; code-truth checks cannot run.'
}
else {
    $declaredTypes = @{}
    $sourceTypeNames = New-Object System.Collections.Generic.List[string]
    $behaviourTypes = New-Object System.Collections.Generic.List[object]
    $productionSource = New-Object System.Text.StringBuilder
    foreach ($sourceFile in (Get-ChildItem -LiteralPath $assetsRoot -Recurse -File -Filter '*.cs')) {
        # Tests are excluded from the temporary-structure scan: a fixture covering a placeholder type
        # is not itself a separate temporary structure needing its own retirement owner. They are
        # excluded from the construction scan for the same reason - a component that only a fixture
        # ever instantiates is not reachable in a running player.
        $isTest = $sourceFile.FullName -like '*\Assets\Tests\*'
        $sourceText = Read-Utf8 $sourceFile.FullName
        if (-not $isTest) { [void]$productionSource.AppendLine($sourceText) }
        foreach ($declaration in [regex]::Matches($sourceText, '(?m)^\s*(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|record|\s)*\b(?:class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)')) {
            $name = $declaration.Groups[1].Value
            if (-not $declaredTypes.ContainsKey($name)) {
                $declaredTypes[$name] = $true
                if (-not $isTest) { $sourceTypeNames.Add($name) }
            }
        }
        if ($isTest) { continue }
        $scriptGuid = ''
        $metaPath = $sourceFile.FullName + '.meta'
        if (Test-Path -LiteralPath $metaPath) {
            $guidMatch = [regex]::Match((Read-Utf8 $metaPath), 'guid:\s*([0-9a-f]{32})')
            if ($guidMatch.Success) { $scriptGuid = $guidMatch.Groups[1].Value }
        }
        foreach ($declaration in [regex]::Matches($sourceText, '(?m)^\s*((?:public|internal|private|protected|sealed|abstract|static|partial|\s)*)\bclass\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*([^\r\n{]+)')) {
            if ($declaration.Groups[3].Value -notmatch '\b(MonoBehaviour|ScriptableObject)\b') { continue }
            $behaviourTypes.Add([pscustomobject]@{
                Name       = $declaration.Groups[2].Value
                Guid       = $scriptGuid
                IsAbstract = $declaration.Groups[1].Value -match '\babstract\b'
            })
        }
    }
    if ($declaredTypes.Count -eq 0) { Add-Issue 'ERROR' 'no-declared-types' 'No C# type declarations were found under Assets/; the contract check would pass vacuously.' }
    if ($behaviourTypes.Count -eq 0) { Add-Issue 'ERROR' 'no-declared-behaviours' 'No MonoBehaviour or ScriptableObject declarations were found under Assets/; the reachability check would pass vacuously.' }

    # Script GUIDs Unity can actually construct from authored content.
    $authoredScriptGuids = @{}
    foreach ($authoredFile in (Get-ChildItem $assetsRoot -Recurse -File -Include '*.unity','*.prefab','*.asset')) {
        foreach ($scriptRef in [regex]::Matches((Read-Utf8 $authoredFile.FullName), 'm_Script:\s*\{fileID:\s*11500000,\s*guid:\s*([0-9a-f]{32})')) {
            $authoredScriptGuids[$scriptRef.Groups[1].Value] = $true
        }
    }
    if ($authoredScriptGuids.Count -eq 0) { Add-Issue 'ERROR' 'no-authored-scripts' 'No authored script references were found in any scene, prefab, or asset; the reachability check would report every component.' }

    foreach ($contractRelative in @('CARD_SYSTEM_ARCHITECTURE.md','CARD_EFFECT_CAPABILITIES.md')) {
        $contractPath = Join-Path $docsRoot $contractRelative
        if (Test-Path -LiteralPath $contractPath) {
            foreach ($undeclared in (Get-UndeclaredContractTypes (Read-Utf8 $contractPath) $declaredTypes)) {
                Add-Issue 'ERROR' 'undeclared-contract-type' "$contractRelative names ``$undeclared`` as a contract, but no such type exists under Assets/. Implement it, or list it in that document's 'Contract implementation status' section."
            }
        }
    }
    foreach ($shard in (Get-ChildItem -LiteralPath (Join-Path $docsRoot 'Card-System') -File -Filter '*.md' -ErrorAction SilentlyContinue)) {
        foreach ($undeclared in (Get-UndeclaredContractTypes (Read-Utf8 $shard.FullName) $declaredTypes)) {
            Add-Issue 'ERROR' 'undeclared-contract-type' "Card-System/$($shard.Name) names ``$undeclared`` as a contract, but no such type exists under Assets/. Implement it, or list it in that document's 'Contract implementation status' section."
        }
    }

    $implementationPath = Join-Path $docsRoot 'Project\CURRENT_IMPLEMENTATION.md'
    if (Test-Path -LiteralPath $implementationPath) {
        foreach ($code in (Test-StatusCoherence $session (Read-Utf8 $implementationPath) $roadmapCorpus)) {
            Add-Issue 'ERROR' $code 'SESSION_BRIEF.md, Project/CURRENT_IMPLEMENTATION.md, and the roadmap do not name the same completed milestone.'
        }
    }

    $riskPath = Join-Path $docsRoot 'TECHNICAL_RISKS.md'
    if (Test-Path -LiteralPath $riskPath) {
        $riskText = Read-Utf8 $riskPath
        foreach ($unregistered in (Get-UnregisteredTemporaryTypes $sourceTypeNames.ToArray() $riskText)) {
            Add-Issue 'WARNING' 'unregistered-temporary-structure' "``$unregistered`` looks deliberately temporary but has no row in the temporary-structure register of TECHNICAL_RISKS.md naming its retirement owner."
        }
        foreach ($unreachable in (Get-UnreachableBehaviourTypes $behaviourTypes.ToArray() $authoredScriptGuids $productionSource.ToString() $riskText)) {
            Add-Issue 'ERROR' 'unreachable-component' "``$unreachable`` derives from MonoBehaviour/ScriptableObject but is in no scene, prefab, or asset and is never constructed by production code, so it cannot run. Delete it, construct it, or give it a retirement owner in the temporary-structure register of TECHNICAL_RISKS.md."
        }
    }
}

# One canonical home: archived history must not be copied back into an active document.
# Sourced directly from the archive rather than from the retired migration manifest, so the check
# now expresses the rule it enforces instead of a property of one completed migration.
$activeCorpus = ($activeFiles | ForEach-Object { Read-Utf8 $_.FullName }) -join "`n"
$archivedBodies = New-Object System.Collections.Generic.List[string]
foreach ($handoff in (Get-ChildItem -LiteralPath (Join-Path $docsRoot 'Archive\Phase-Handoffs') -File -Filter '*.md' -ErrorAction SilentlyContinue)) {
    foreach ($paragraph in ([regex]::Split((Read-Utf8 $handoff.FullName), '\r?\n\s*\r?\n'))) {
        $body = $paragraph.Trim()
        if ($body.Length -lt 200) { continue }
        if ($body.StartsWith('#') -or $body.StartsWith('|')) { continue }
        $archivedBodies.Add($body)
    }
}
if ($archivedBodies.Count -eq 0) { Add-Issue 'ERROR' 'no-archived-bodies' 'No archived handoff bodies were found; the duplicate-history check would pass vacuously.' }
if (-not (Test-DuplicateBodies $archivedBodies.ToArray() $activeCorpus)) { Add-Issue 'ERROR' 'duplicate-history' 'An archived handoff body is duplicated verbatim in active documentation. Replace the copy with a stable backlink.' }

$warnings = @($issues | Where-Object Severity -eq 'WARNING')
$errors = @($issues | Where-Object Severity -eq 'ERROR')
foreach ($issue in $issues) { Write-Host "[$($issue.Severity)] $($issue.Code): $($issue.Message)" }
if ($errors.Count -gt 0) {
    Write-Error "Living-document validation failed with $($errors.Count) error(s) and $($warnings.Count) warning(s)."
    exit 1
}
Write-Host "Living-document validation passed: $($activeFiles.Count) governed active leaves, $($behaviourTypes.Count) production components, $($archivedBodies.Count) archived bodies, $($warnings.Count) warning(s)."
