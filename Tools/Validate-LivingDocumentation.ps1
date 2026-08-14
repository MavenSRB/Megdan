param(
    [switch] $SkipSelfTests
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$docsRoot = [System.IO.Path]::Combine($projectRoot, 'Docs', 'AI')
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
    # Forward slashes on every platform. The result is display text and a Get-Limit lookup key, never
    # a path handed back to the filesystem, so one separator everywhere keeps messages identical
    # across hosts instead of varying with the runner.
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $baseUri = New-Object System.Uri(($BasePath.TrimEnd('\', '/') + $separator))
    $targetUri = New-Object System.Uri($TargetPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
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

# Code-truth check 2: the canonical statements of "how far are we" must name the same milestone.
# README.md joined this set at 6.1o. It had drifted four milestones behind - it still said 6.1d after
# 6.1h shipped - precisely because it was the one status statement no check read.
function Test-StatusCoherence([string] $Session, [string] $Implementation, [string] $RoadmapCorpus, [string] $Readme) {
    $codes = New-Object System.Collections.Generic.List[string]
    $sessionMatch = [regex]::Match($Session, '(?m)^- Current completed milestone: \*\*([^*]+)\*\*\.$')
    $implementationMatch = [regex]::Match($Implementation, 'completed roadmap milestones through \*\*([^*]+)\*\*')
    if (-not $sessionMatch.Success -or -not $implementationMatch.Success) {
        $codes.Add('missing-status-statement')
        return $codes.ToArray()
    }
    if ($sessionMatch.Groups[1].Value -ne $implementationMatch.Groups[1].Value) { $codes.Add('incoherent-status') }
    if (-not [regex]::IsMatch($RoadmapCorpus, "(?m)^### \[x\] $([regex]::Escape($implementationMatch.Groups[1].Value))(?:\s|$)")) { $codes.Add('incoherent-status') }
    if ($PSBoundParameters.ContainsKey('Readme')) {
        $readmeMatch = [regex]::Match($Readme, 'completed milestones through \*\*([^*]+)\*\*')
        if (-not $readmeMatch.Success) { $codes.Add('missing-status-statement') }
        elseif ($readmeMatch.Groups[1].Value -ne $implementationMatch.Groups[1].Value) { $codes.Add('incoherent-status') }
    }
    return $codes.ToArray()
}

# Governance check: a milestone's archive section carries one row, and its evidence lives in the
# commit body. The policy was adopted on 2026-08-13 and then ignored by every milestone that
# followed - 6.1c, 6.1d, 6.1f, 6.1g, 6.1h, 6.1i, and 6.1k each appended pages of narrative under it -
# because nothing read it.
#
# The rule can only ever be forward-looking: the archive is append-only, so sections already written
# are exempted rather than rewritten. The exemptions are a frozen baseline file rather than a literal
# here, because there are 85 of them and a list that long inside a predicate stops being readable.
function Get-NonConformingArchiveSections([string] $Text, [string] $FileName, [hashtable] $Baseline) {
    $offenders = New-Object System.Collections.Generic.List[string]
    foreach ($section in [regex]::Matches($Text, '(?ms)^## Milestone (\S+)[^\n]*completion evidence[^\n]*\n(.*?)(?=^## |\z)')) {
        $id = $section.Groups[1].Value
        if ($Baseline.ContainsKey("$FileName|$id")) { continue }
        foreach ($line in ($section.Groups[2].Value -split '\r?\n')) {
            $trimmed = $line.Trim()
            if ($trimmed -eq '' -or $trimmed.StartsWith('|')) { continue }
            if (-not $offenders.Contains($id)) { $offenders.Add($id) }
            break
        }
    }
    return $offenders.ToArray()
}

function Read-ArchiveNarrativeBaseline([string] $Path) {
    $baseline = @{}
    if (-not (Test-Path -LiteralPath $Path)) { return $baseline }
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        $entry = $line.Trim()
        if ($entry -eq '' -or $entry.StartsWith('#')) { continue }
        $baseline[$entry] = $true
    }
    return $baseline
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

# Code-truth check 5: a test that names a production member by string must name one that exists.
#
# Milestone 6.1s renamed a private field `setupState` to `occupancy`. Three fixtures resolve that
# field reflectively by string name, and no compiler can check a string: `GetField` would have
# returned `null` and each test would have died on a null dereference far from the cause.
# `StandardMinionViewTests` had even had its assertion updated to `.Occupancy` while its reflection
# string was missed, so one file disagreed with itself and nothing noticed. The fixtures involved sit
# in the dotnet harness exclude list, so the Linux harness cannot reach them either.
#
# This is a text scan rather than a parser, deliberately: it runs anywhere PowerShell does, including
# the non-Windows host where Unity cannot run, which is the only way it catches the drift before a
# Windows tier. It answers exactly one question - does the named type's own source declare this name -
# which is the question a rename gets wrong.
function Get-ReflectionSites([string] $TestSource) {
    $sites = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($TestSource)) { return $sites.ToArray() }
    $lines = $TestSource -split "`n"

    # `\s` spans newlines, so this matches the single-line form and both multi-line forms: a break
    # before the `.`, and a break after the opening parenthesis. A pattern anchored to `GetField("`
    # matches only the single-line form and silently misses the rest, which is how this class hides.
    # Two idioms, both scanned. The raw form is what most fixtures use; the second is the project's
    # ReflectedMember helper, which fails naming the type and member instead of returning null. Routing
    # a call through that helper must not remove it from this scan - a guard that hides sites from the
    # other guard is worse than neither.
    $memberPatterns = @(
        'typeof\(\s*([A-Za-z_]\w*)\s*\)\s*\.\s*Get(?:Field|Property|Method)\s*\(\s*"([^"]+)"',
        'RequiredInstance(?:Field|Property|Method)\s*\(\s*typeof\(\s*([A-Za-z_]\w*)\s*\)\s*,\s*"([^"]+)"'
    )
    foreach ($pattern in $memberPatterns) {
        foreach ($match in [regex]::Matches($TestSource, $pattern)) {
            $sites.Add([pscustomobject]@{
                Kind   = 'member'
                Type   = $match.Groups[1].Value
                Name   = $match.Groups[2].Value
                Exempt = Test-AbsenceIntended $TestSource $lines $match
            })
        }
    }

    foreach ($match in [regex]::Matches($TestSource, '\.GetType\(\s*"([^"]+)"')) {
        $full = $match.Groups[1].Value
        $sites.Add([pscustomobject]@{
            Kind   = 'type'
            Type   = $full
            Name   = $full.Split('.')[-1]
            Exempt = Test-AbsenceIntended $TestSource $lines $match
        })
    }

    return $sites.ToArray()
}

# The exemption is stated in the source, never inferred, because a check cannot tell "this member was
# renamed" from "this test asserts the member does not exist" - and both exist here.
# `ConfiguredMatchCutoverArchitectureTests` asserts one `OnGUI` is present and another is absent, four
# lines apart. The window is the site's own lines plus one, so a marker cannot leak onto its neighbour.
function Test-AbsenceIntended([string] $TestSource, [string[]] $Lines, $Match) {
    $startLine = ($TestSource.Substring(0, $Match.Index) -split "`n").Count - 1
    $endLine = ($TestSource.Substring(0, $Match.Index + $Match.Length) -split "`n").Count - 1
    for ($i = $startLine; $i -le [Math]::Min($endLine + 1, $Lines.Count - 1); $i++) {
        if ($Lines[$i] -match 'absence-intended') { return $true }
    }
    return $false
}

function Get-UnresolvedReflectedMembers($Sites, [hashtable] $TypeSources, [hashtable] $DeclaredTypes) {
    $unresolved = New-Object System.Collections.Generic.List[string]
    foreach ($site in $Sites) {
        if ($site.Exempt) { continue }

        if ($site.Kind -eq 'type') {
            if (-not $DeclaredTypes.ContainsKey($site.Name)) {
                $entry = "$($site.Type) (reflected type)"
                if (-not $unresolved.Contains($entry)) { $unresolved.Add($entry) }
            }
            continue
        }

        # An unknown type is not this check's finding: the reflected name may belong to a Unity or BCL
        # type the project does not declare, and flagging those would make the check unusable.
        if (-not $TypeSources.ContainsKey($site.Type)) { continue }

        $escaped = [regex]::Escape($site.Name)
        # A declaration, not a use. `(?<![.\w])` rejects `.occupancy` and `myoccupancy`; the trailing
        # set covers a field (`occupancy;`), an initialised field (`occupancy =`), a method or
        # parameter (`occupancy(`), an auto-property (`Occupancy {`), and an expression body
        # (`Occupancy =>`).
        if ($TypeSources[$site.Type] -notmatch "(?<![.\w])$escaped\s*(?:[;=({,]|=>)") {
            $entry = "$($site.Type).$($site.Name)"
            if (-not $unresolved.Contains($entry)) { $unresolved.Add($entry) }
        }
    }
    return $unresolved.ToArray()
}

# Code-truth check 6: the snapshot decoder must not cast a raw integer into an enum.
#
# `(SomeEnum)reader.ReadInt32()` accepts every int the payload can hold. Whether that is caught
# depends entirely on which constructor happens to consume the value, and that coverage was
# inconsistent by accident rather than by design: about half the sites reached an `Enum.IsDefined`
# guard and failed late as `MalformedPayload` - indistinguishable from a garbled byte stream - and
# the rest built semantically invalid state and carried on. `AttackTargetKind` was the worst of them:
# an undefined value fell through a ternary to `default` *and* skipped its `ReadString`, desyncing
# the stream for everything after it.
#
# The runtime half is `SnapshotEnumReader.Read<T>`, which rejects an undefined value where it is read
# and names the field. This is the half no compiler can enforce: a reader added later can always
# reintroduce a cast, and nothing but a scan would notice.
#
# A text scan for the same reason check 5 is one - it runs on the non-Windows host, ahead of any
# Unity tier. It answers exactly one question: does a cast to a declared enum sit directly in front
# of a `reader.Read` call.
function Get-RawEnumCastSites($SnapshotFiles, [hashtable] $DeclaredEnums) {
    $sites = New-Object System.Collections.Generic.List[object]
    foreach ($file in $SnapshotFiles) {
        if ([string]::IsNullOrEmpty($file.Text)) { continue }
        # `\s` spans newlines, so a break between the cast and the call is matched too. `\??` covers
        # the nullable form `(TimingCheckpoint?)reader.ReadInt32()`. The double-cast form
        # `(EndOfRoundStage?)(EndOfRoundStage)reader.ReadInt32()` matches once, on the inner cast:
        # the outer one is followed by `(`, not `reader`.
        foreach ($match in [regex]::Matches($file.Text, '\(\s*([A-Za-z_]\w*)\s*\??\s*\)\s*reader\s*\.\s*Read')) {
            $name = $match.Groups[1].Value
            # A cast to `int`, `byte`, or any non-enum type is not this check's finding.
            if (-not $DeclaredEnums.ContainsKey($name)) { continue }
            $sites.Add([pscustomobject]@{
                File = $file.Name
                Line = ($file.Text.Substring(0, $match.Index) -split "`n").Count
                Type = $name
            })
        }
    }
    return $sites.ToArray()
}

# Documentation-graph checks (milestone 6.1z): the ownership matrix assigns every subject a canonical
# home "plus its indexed shards", and the history workflow requires every completed milestone
# definition to keep a stable Historical evidence backlink. Neither was enforced: the link pass
# resolves every link that is written, and nothing required a link to exist, so a shard added without
# an index entry - or a completed milestone whose evidence was never linked - passed. The family table
# deliberately reuses the path shapes of Get-Limit so threshold ownership and index ownership cannot
# drift apart. A family with more than one index takes their union, because Card-System/ is
# legitimately owned by two indexes and Project/ names two owners in the matrix.
function Get-ShardFamilies {
    return @(
        [pscustomobject]@{ Pattern = 'Project/*.md';     Indexes = @('PROJECT_CONTEXT.md', 'Project/CURRENT_IMPLEMENTATION.md') }
        [pscustomobject]@{ Pattern = 'Roadmap/*.md';     Indexes = @('ROADMAP.md') }
        [pscustomobject]@{ Pattern = 'Testing/*.md';     Indexes = @('TESTING.md') }
        [pscustomobject]@{ Pattern = 'Card-System/*.md'; Indexes = @('CARD_SYSTEM_ARCHITECTURE.md', 'CARD_EFFECT_CAPABILITIES.md') }
        [pscustomobject]@{ Pattern = 'Rules/*.md';       Indexes = @('RULES_LEDGER.md') }
    )
}

function Get-OrphanShards($Families, [string[]] $ShardPaths, [hashtable] $IndexTexts) {
    $orphans = New-Object System.Collections.Generic.List[string]
    foreach ($family in $Families) {
        foreach ($shard in ($ShardPaths | Where-Object { $_ -like $family.Pattern })) {
            $fileName = [regex]::Escape(($shard -split '/')[-1])
            $isLinked = $false
            foreach ($index in $family.Indexes) {
                if (-not $IndexTexts.ContainsKey($index)) { continue }
                if ([regex]::IsMatch($IndexTexts[$index], "\]\([^)]*$fileName(#[^)]*)?\)")) { $isLinked = $true; break }
            }
            if (-not $isLinked) { $orphans.Add($shard) }
        }
    }
    return $orphans.ToArray()
}

function Get-CompleteMilestoneBlocks([string] $RoadmapCorpus) {
    $blocks = New-Object System.Collections.Generic.List[object]
    foreach ($match in [regex]::Matches($RoadmapCorpus, '(?ms)^### \[x\] (\S+)[^\r\n]*\r?\n(.*?)(?=^#{2,3} |\z)')) {
        $blocks.Add([pscustomobject]@{ Id = $match.Groups[1].Value; Body = $match.Groups[2].Value })
    }
    return $blocks.ToArray()
}

function Get-MilestonesMissingEvidenceBacklink($Blocks) {
    return @($Blocks | Where-Object { $_.Body -notmatch '(?m)^Historical evidence:' } | ForEach-Object { $_.Id })
}

# Both destination forms are legitimate: the one-row entry is the post-2026-08-13 policy and the
# "## Milestone <id> ..." section is the baselined earlier history; a check accepting only one form
# would report dozens of valid historical records. An exemption is declared in the block itself
# ("Historical evidence: exempt - ..."), never inferred: milestone 0.1 created the documentation
# system and predates the archive, and an invented record is a worse failure than an admitted gap.
function Get-MilestonesMissingArchiveDestination($Blocks, [hashtable] $ArchiveTexts) {
    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($block in $Blocks) {
        if ($block.Body -match '(?m)^Historical evidence:\s*exempt\b') { continue }
        $phase = ($block.Id -split '\.')[0]
        $archive = if ($ArchiveTexts.ContainsKey($phase)) { $ArchiveTexts[$phase] } else { '' }
        $escaped = [regex]::Escape($block.Id)
        $hasSection = [regex]::IsMatch($archive, "(?m)^##+ Milestone $escaped\b")
        $hasRow = [regex]::IsMatch($archive, "(?m)^\|\s*$escaped\s*\|")
        if (-not ($hasSection -or $hasRow)) { $missing.Add($block.Id) }
    }
    return $missing.ToArray()
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

    $coherentReadme = 'As of the last update the project has completed milestones through **2.5**.'
    Expect 'coherent status quadruple' ((Test-StatusCoherence $coherentSession $coherentImplementation $coherentRoadmap $coherentReadme).Count -eq 0)
    Expect 'stale README status' ((Test-StatusCoherence $coherentSession $coherentImplementation $coherentRoadmap ($coherentReadme -replace '2\.5', '2.3')) -contains 'incoherent-status')
    Expect 'README with no status statement' ((Test-StatusCoherence $coherentSession $coherentImplementation $coherentRoadmap 'No parseable statement here.') -contains 'missing-status-statement')

    $oneRow = "## Milestone 9.9 Do the thing completion evidence`n`n| ID | Outcome | Commit |`n| --- | --- | --- |`n| 9.9 | It was done. | ``abc1234`` |`n"
    $withNarrative = $oneRow + "`n### 2026-01-01 addendum: what really happened`n`nSeveral paragraphs of evidence.`n"
    $noBaseline = @{}
    Expect 'conforming one-row archive section' ((Get-NonConformingArchiveSections $oneRow 'PHASE_9.md' $noBaseline).Count -eq 0)
    Expect 'archive section carrying narrative' ((Get-NonConformingArchiveSections $withNarrative 'PHASE_9.md' $noBaseline) -contains '9.9')
    Expect 'baselined archive section' ((Get-NonConformingArchiveSections $withNarrative 'PHASE_9.md' @{ 'PHASE_9.md|9.9' = $true }).Count -eq 0)
    Expect 'baseline entry is scoped to its file' ((Get-NonConformingArchiveSections $withNarrative 'PHASE_9.md' @{ 'PHASE_8.md|9.9' = $true }) -contains '9.9')
    Expect 'narrative section bounded by the next milestone' ((Get-NonConformingArchiveSections ($oneRow + "`n" + $withNarrative.Replace('9.9', '9.8')) 'PHASE_9.md' $noBaseline) -contains '9.8')

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

    # Code-truth check 5.
    $reflectSources = @{ 'ViewType' = 'private MinionOccupancy occupancy;' }
    $reflectTypes = @{ 'ViewType' = $true; 'BackendType' = $true }
    Expect 'reflected member that exists resolves' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'typeof(ViewType).GetField("occupancy", flags)') $reflectSources $reflectTypes).Count -eq 0)
    Expect 'renamed reflected member is reported' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'typeof(ViewType).GetField("setupState", flags)') $reflectSources $reflectTypes) -contains 'ViewType.setupState')
    Expect 'absence-intended site is exempt' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'typeof(ViewType).GetMethod("OnGUI"), Is.Null); // absence-intended') $reflectSources $reflectTypes).Count -eq 0)
    Expect 'multi-line reflection form is matched' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites "typeof(ViewType).GetField(`n    `"setupState`",`n    flags)") $reflectSources $reflectTypes) -contains 'ViewType.setupState')
    Expect 'reflection form broken before the dot is matched' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites "typeof(ViewType)`n    .GetField(`"setupState`", flags)") $reflectSources $reflectTypes) -contains 'ViewType.setupState')
    Expect 'member declared in a second partial file resolves' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'typeof(PartialType).GetField("later", flags)') @{ 'PartialType' = "public partial class PartialType { }`nprivate int later;" } $reflectTypes).Count -eq 0)
    Expect 'member access is not a declaration' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'typeof(ViewType).GetField("setupState", flags)') @{ 'ViewType' = 'var x = other.setupState;' } $reflectTypes) -contains 'ViewType.setupState')
    Expect 'undeclared reflected type is reported' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'assembly.GetType("Guards.Core.GoneType")') $reflectSources $reflectTypes).Count -eq 1)
    Expect 'declared reflected type resolves' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'assembly.GetType("Guards.Core.BackendType")') $reflectSources $reflectTypes).Count -eq 0)
    Expect 'type the project does not declare is not a finding' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'typeof(SomeUnityType).GetField("anything", flags)') $reflectSources $reflectTypes).Count -eq 0)
    Expect 'source with no reflection site yields none' ((Get-ReflectionSites 'var x = 1;').Count -eq 0)
    Expect 'helper form is scanned too' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites 'ReflectedMember.RequiredInstanceField(typeof(ViewType), "setupState")') $reflectSources $reflectTypes) -contains 'ViewType.setupState')
    Expect 'helper form resolving member passes' ((Get-UnresolvedReflectedMembers (Get-ReflectionSites "ReflectedMember.RequiredInstanceField(`n    typeof(ViewType),`n    `"occupancy`")") $reflectSources $reflectTypes).Count -eq 0)

    # Code-truth check 6. `@()` around every call: the function returns object records rather than
    # strings, and Windows PowerShell unwraps a one-element array to a bare PSCustomObject whose
    # `.Count` is null - so an unwrapped single finding would read as no finding at all.
    $enumNames = @{ 'TeamSide' = $true; 'TimingCheckpoint' = $true; 'EndOfRoundStage' = $true }
    function SnapshotFile([string] $Text) { return @([pscustomobject]@{ Name = 'Probe.cs'; Text = $Text }) }
    Expect 'raw enum cast is reported' (@(Get-RawEnumCastSites (SnapshotFile 'var side = (TeamSide)reader.ReadInt32();') $enumNames).Count -eq 1)
    Expect 'raw enum cast names its type' (@(Get-RawEnumCastSites (SnapshotFile 'var side = (TeamSide)reader.ReadInt32();') $enumNames)[0].Type -eq 'TeamSide')
    Expect 'nullable raw enum cast is reported' (@(Get-RawEnumCastSites (SnapshotFile 'var at = (TimingCheckpoint?)reader.ReadInt32();') $enumNames).Count -eq 1)
    Expect 'double cast is reported once' (@(Get-RawEnumCastSites (SnapshotFile 'var s = has ? (EndOfRoundStage?)(EndOfRoundStage)reader.ReadInt32() : null;') $enumNames).Count -eq 1)
    Expect 'multi-line raw enum cast is matched' (@(Get-RawEnumCastSites (SnapshotFile "var side = (TeamSide)`n    reader.ReadInt32();") $enumNames).Count -eq 1)
    Expect 'typed reader call is not a finding' (@(Get-RawEnumCastSites (SnapshotFile 'var side = SnapshotEnumReader.Read<TeamSide>(reader, "team side");') $enumNames).Count -eq 0)
    Expect 'primitive cast is not a finding' (@(Get-RawEnumCastSites (SnapshotFile 'var n = (int)reader.ReadInt32();') $enumNames).Count -eq 0)
    Expect 'cast to an undeclared type is not a finding' (@(Get-RawEnumCastSites (SnapshotFile 'var x = (SomeUnityEnum)reader.ReadInt32();') $enumNames).Count -eq 0)
    Expect 'enum cast not in front of a read is not a finding' (@(Get-RawEnumCastSites (SnapshotFile 'var side = (TeamSide)stored.Value;') $enumNames).Count -eq 0)
    Expect 'cast reports the line it sits on' (@(Get-RawEnumCastSites (SnapshotFile "using System;`nvar side = (TeamSide)reader.ReadInt32();") $enumNames)[0].Line -eq 2)
    Expect 'source with no cast yields none' (@(Get-RawEnumCastSites (SnapshotFile 'var n = reader.ReadInt32();') $enumNames).Count -eq 0)
    Expect 'two casts in one file are both reported' (@(Get-RawEnumCastSites (SnapshotFile "var a = (TeamSide)reader.ReadInt32();`nvar b = (EndOfRoundStage)reader.ReadInt32();") $enumNames).Count -eq 2)

    # Documentation-graph checks (milestone 6.1z).
    $graphFamilies = @([pscustomobject]@{ Pattern = 'Sub/*.md'; Indexes = @('INDEX_A.md', 'INDEX_B.md') })
    $graphIndexes = @{ 'INDEX_A.md' = 'See [the shard](Sub/LINKED.md) here.'; 'INDEX_B.md' = 'Also [other](Sub/OTHER.md#anchor).' }
    Expect 'linked shard passes' (@(Get-OrphanShards $graphFamilies @('Sub/LINKED.md') $graphIndexes).Count -eq 0)
    Expect 'orphaned shard is reported' (@(Get-OrphanShards $graphFamilies @('Sub/ORPHAN.md') $graphIndexes) -contains 'Sub/ORPHAN.md')
    Expect 'second index of a two-index family links it' (@(Get-OrphanShards $graphFamilies @('Sub/OTHER.md') $graphIndexes).Count -eq 0)
    Expect 'empty shard set yields no orphans for the vacuity guard to catch' (@(Get-OrphanShards $graphFamilies @() $graphIndexes).Count -eq 0)
    $graphBlocks = Get-CompleteMilestoneBlocks "### [x] 2.1 Done`nBody.`nHistorical evidence: [row](x.md).`n### [x] 2.2 Also done`nBody only.`n### [ ] 2.3 Open`nNot complete.`n"
    Expect 'complete blocks are parsed and open ones skipped' ($graphBlocks.Count -eq 2)
    Expect 'backlink present passes' (@(Get-MilestonesMissingEvidenceBacklink $graphBlocks) -notcontains '2.1')
    Expect 'missing backlink is reported' (@(Get-MilestonesMissingEvidenceBacklink $graphBlocks) -contains '2.2')
    Expect 'corpus with no complete blocks parses to zero for the vacuity guard' (@(Get-CompleteMilestoneBlocks 'no blocks here').Count -eq 0)
    $sectionArchive = @{ '2' = "## Milestone 2.1 Done completion evidence`n| 2.1 | outcome | ``abc`` |`n" }
    $rowArchive = @{ '2' = "| ID | Outcome | Commit |`n| --- | --- | --- |`n| 2.2 | outcome | ``def`` |`n" }
    Expect 'section form is a destination' (@(Get-MilestonesMissingArchiveDestination (Get-CompleteMilestoneBlocks "### [x] 2.1 Done`nHistorical evidence: [x](y.md).`n") $sectionArchive).Count -eq 0)
    Expect 'row form is a destination' (@(Get-MilestonesMissingArchiveDestination (Get-CompleteMilestoneBlocks "### [x] 2.2 Done`nHistorical evidence: [x](y.md).`n") $rowArchive).Count -eq 0)
    Expect 'missing destination is reported' (@(Get-MilestonesMissingArchiveDestination (Get-CompleteMilestoneBlocks "### [x] 2.9 Done`nHistorical evidence: [x](y.md).`n") $sectionArchive) -contains '2.9')
    Expect 'a longer id does not satisfy a shorter one' (@(Get-MilestonesMissingArchiveDestination (Get-CompleteMilestoneBlocks "### [x] 2.1 Done`nHistorical evidence: [x](y.md).`n") @{ '2' = "## Milestone 2.1c Other completion evidence`n| 2.1c | outcome | ``abc`` |`n" }) -contains '2.1')
    Expect 'a declared exemption skips the destination check' (@(Get-MilestonesMissingArchiveDestination (Get-CompleteMilestoneBlocks "### [x] 0.1 Bootstrap`nHistorical evidence: exempt - predates the phase-handoff archive.`n") @{}).Count -eq 0)

    if ($failed.Count -gt 0) { throw "Living-document validator self-test failure: $($failed -join ', ')" }
    Write-Host "Self-tests: $script:selfTestCount/$script:selfTestCount checks passed."
}

if (-not $SkipSelfTests) { Invoke-SelfTests }

# Active leaf thresholds.
$activeFiles = Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter '*.md' | Where-Object { $_.FullName.Replace('\', '/') -notlike '*/Archive/*' -and $_.FullName.Replace('\', '/') -notlike '*/Maps/*' }
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
    'Project/FOUNDATION_CORRECTIONS.md' = @('Audit findings and corrective sequence','Maintenance rule')
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
$linkFiles = Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter '*.md' | Where-Object { $_.FullName.Replace('\', '/') -notlike '*/Archive/Pre-Governance/*' }
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

# Documentation-graph checks (milestone 6.1z): every governed shard is reachable from its family
# index, and every completed milestone states its evidence destination and links to it.
$docRelativeTexts = @{}
foreach ($file in $activeFiles) {
    $docRelativeTexts[(Get-RelativePath $projectRoot $file.FullName).Replace('Docs/AI/', '')] = Read-Utf8 $file.FullName
}
$shardFamilies = Get-ShardFamilies
$governedShardPaths = @($docRelativeTexts.Keys | Where-Object { $rel = $_; @($shardFamilies | Where-Object { $rel -like $_.Pattern }).Count -gt 0 })
if ($governedShardPaths.Count -eq 0) { Add-Issue 'ERROR' 'no-indexed-shards' 'No governed shards were found under any family directory; the orphan-shard check would pass vacuously.' }
foreach ($orphan in (Get-OrphanShards $shardFamilies $governedShardPaths $docRelativeTexts)) {
    Add-Issue 'ERROR' 'orphan-shard' "Docs/AI/$orphan is linked from no index in its family. Add the index link, or the shard is unreachable authority."
}
$completeMilestoneBlocks = Get-CompleteMilestoneBlocks $roadmapCorpus
if ($completeMilestoneBlocks.Count -eq 0) { Add-Issue 'ERROR' 'no-complete-milestones' 'No complete milestone blocks were found in the roadmap corpus; the backlink and archive-destination checks would pass vacuously.' }
foreach ($id in (Get-MilestonesMissingEvidenceBacklink $completeMilestoneBlocks)) {
    Add-Issue 'ERROR' 'missing-evidence-backlink' "Complete milestone $id carries no 'Historical evidence' line. The active definition must keep its stable backlink, or state its exemption."
}
$phaseArchiveTexts = @{}
foreach ($handoff in (Get-ChildItem -LiteralPath ([System.IO.Path]::Combine($docsRoot, 'Archive', 'Phase-Handoffs')) -File -Filter 'PHASE_*.md' -ErrorAction SilentlyContinue)) {
    if ($handoff.BaseName -match '^PHASE_(\d+)$') { $phaseArchiveTexts[$Matches[1]] = Read-Utf8 $handoff.FullName }
}
foreach ($id in (Get-MilestonesMissingArchiveDestination $completeMilestoneBlocks $phaseArchiveTexts)) {
    Add-Issue 'ERROR' 'missing-archive-destination' "Complete milestone $id has neither a one-row entry nor a 'Milestone $id' section in its phase handoff archive."
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
    $declaredEnums = @{}
    $sourceTypeNames = New-Object System.Collections.Generic.List[string]
    $behaviourTypes = New-Object System.Collections.Generic.List[object]
    $productionSource = New-Object System.Text.StringBuilder
    $testSource = New-Object System.Text.StringBuilder
    # The snapshot decoder, kept separate so the raw-enum-cast check scans exactly the files that
    # decode a payload and reports a real file and line rather than an offset into a concatenation.
    $snapshotFiles = New-Object System.Collections.Generic.List[object]
    # Every file declaring a type, concatenated, so a member added by a second partial file resolves.
    $typeSources = @{}
    foreach ($sourceFile in (Get-ChildItem -LiteralPath $assetsRoot -Recurse -File -Filter '*.cs')) {
        # Tests are excluded from the temporary-structure scan: a fixture covering a placeholder type
        # is not itself a separate temporary structure needing its own retirement owner. They are
        # excluded from the construction scan for the same reason - a component that only a fixture
        # ever instantiates is not reachable in a running player.
        $isTest = $sourceFile.FullName.Replace('\', '/') -like '*/Assets/Tests/*'
        $sourceText = Read-Utf8 $sourceFile.FullName
        if (-not $isTest) { [void]$productionSource.AppendLine($sourceText) }
        else { [void]$testSource.AppendLine($sourceText) }
        if ($sourceFile.FullName.Replace('\', '/') -like '*/Assets/GuardsDigital/Core/Snapshots/*') {
            $snapshotFiles.Add([pscustomobject]@{ Name = $sourceFile.Name; Text = $sourceText })
        }
        foreach ($enumDeclaration in [regex]::Matches($sourceText, '(?m)^\s*(?:public|internal|private|protected|static|\s)*\benum\s+([A-Za-z_][A-Za-z0-9_]*)')) {
            $declaredEnums[$enumDeclaration.Groups[1].Value] = $true
        }
        foreach ($declaration in [regex]::Matches($sourceText, '(?m)^\s*(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|record|\s)*\b(?:class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)')) {
            $name = $declaration.Groups[1].Value
            if ($typeSources.ContainsKey($name)) { $typeSources[$name] += "`n" + $sourceText }
            else { $typeSources[$name] = $sourceText }
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

    $implementationPath = [System.IO.Path]::Combine($docsRoot, 'Project', 'CURRENT_IMPLEMENTATION.md')
    $readmePath = Join-Path $projectRoot 'README.md'
    if (Test-Path -LiteralPath $implementationPath) {
        $statusCodes = if (Test-Path -LiteralPath $readmePath) {
            Test-StatusCoherence $session (Read-Utf8 $implementationPath) $roadmapCorpus (Read-Utf8 $readmePath)
        } else {
            Test-StatusCoherence $session (Read-Utf8 $implementationPath) $roadmapCorpus
        }
        foreach ($code in $statusCodes) {
            Add-Issue 'ERROR' $code 'SESSION_BRIEF.md, Project/CURRENT_IMPLEMENTATION.md, README.md, and the roadmap do not name the same completed milestone.'
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

    $reflectionSites = Get-ReflectionSites $testSource.ToString()
    if ($reflectionSites.Count -eq 0) {
        Add-Issue 'ERROR' 'no-reflection-sites' 'No reflected member or type names were found under Assets/Tests/; the reflected-name check would pass vacuously.'
    }
    foreach ($unresolved in (Get-UnresolvedReflectedMembers $reflectionSites $typeSources $declaredTypes)) {
        Add-Issue 'ERROR' 'unresolved-reflected-member' "A test reflects on ``$unresolved``, but the declaring type's source does not declare that name. The compiler cannot check a string, so a rename leaves the test resolving to null and failing on a dereference. Update the name, or mark a deliberate absence assertion with an ``absence-intended`` comment on the site."
    }

    # Code-truth check 6.
    if ($snapshotFiles.Count -eq 0) {
        Add-Issue 'ERROR' 'no-snapshot-decoder' 'No source files were found under Assets/GuardsDigital/Core/Snapshots/; the raw-enum-cast check would pass vacuously.'
    }
    elseif ($declaredEnums.Count -eq 0) {
        Add-Issue 'ERROR' 'no-declared-enums' 'No C# enum declarations were found under Assets/; the raw-enum-cast check would pass vacuously.'
    }
    else {
        $snapshotCorpus = ($snapshotFiles | ForEach-Object { $_.Text }) -join "`n"
        if ($snapshotCorpus -notmatch 'SnapshotEnumReader\s*\.\s*Read') {
            Add-Issue 'ERROR' 'no-typed-enum-reads' 'The snapshot decoder contains no SnapshotEnumReader read; either the typed reader was removed or the check is scanning the wrong files. A check that cannot observe the thing it enforces is not evidence.'
        }
        foreach ($site in (Get-RawEnumCastSites $snapshotFiles.ToArray() $declaredEnums)) {
            Add-Issue 'ERROR' 'raw-enum-cast' "$($site.File):$($site.Line) casts a raw integer into ``$($site.Type)``. A snapshot enum must be read through ``SnapshotEnumReader.Read<$($site.Type)>(reader, ""<field>"")``, which rejects an undefined value where it is read instead of leaving it to whichever constructor happens to consume it."
        }
    }
}

# One canonical home: archived history must not be copied back into an active document.
# Sourced directly from the archive rather than from the retired migration manifest, so the check
# now expresses the rule it enforces instead of a property of one completed migration.
$activeCorpus = ($activeFiles | ForEach-Object { Read-Utf8 $_.FullName }) -join "`n"

$archiveNarrativeBaseline = Read-ArchiveNarrativeBaseline (Join-Path $PSScriptRoot 'ArchiveNarrativeBaseline.txt')
if ($archiveNarrativeBaseline.Count -eq 0) { Add-Issue 'ERROR' 'missing-archive-baseline' 'Tools/ArchiveNarrativeBaseline.txt is missing or empty, so the one-row archive check would report every historical section.' }
$archivedBodies = New-Object System.Collections.Generic.List[string]
foreach ($handoff in (Get-ChildItem -LiteralPath ([System.IO.Path]::Combine($docsRoot, 'Archive', 'Phase-Handoffs')) -File -Filter '*.md' -ErrorAction SilentlyContinue)) {
    foreach ($offender in (Get-NonConformingArchiveSections (Read-Utf8 $handoff.FullName) $handoff.Name $archiveNarrativeBaseline)) {
        Add-Issue 'ERROR' 'archive-narrative' "Archive/Phase-Handoffs/$($handoff.Name) carries body prose in the section for milestone $offender. Governance gives each milestone one row - ID, one-line outcome, commit SHA - and its evidence belongs in the commit message body, addressed by SHA."
    }
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
