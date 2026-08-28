[CmdletBinding()]
param(
    [switch] $WriteExperiment,
    [switch] $SkipSelfTests,
    [ValidateSet('None', 'OverBudget', 'OverHop', 'DuplicateOwner', 'ExpiredEvidence', 'StaleSource', 'OrphanBody', 'OmittedAuthority', 'WeakenedAnswer')]
    [string] $DeliberateViolation = 'None'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$inputPath = Join-Path $PSScriptRoot 'GovernanceSourceBodyManifest.json'
$outputPath = Join-Path $PSScriptRoot 'GovernanceAccessExperiment.json'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Get-Sha256Hex([byte[]] $Bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return (($sha.ComputeHash($Bytes) | ForEach-Object { $_.ToString('x2') }) -join '') }
    finally { $sha.Dispose() }
}

function Get-OrdinalSortedStrings($Values) {
    [string[]] $items = @($Values | ForEach-Object { [string]$_ } | Select-Object -Unique)
    [System.Array]::Sort($items, [System.StringComparer]::Ordinal)
    return $items
}

function Copy-JsonObject($Value) {
    return ($Value | ConvertTo-Json -Depth 20 | ConvertFrom-Json)
}

function Get-SourceText($Source) {
    $path = Join-Path $projectRoot ([string]$Source.path).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    [byte[]] $bytes = [System.IO.File]::ReadAllBytes($path)
    return [pscustomobject]@{ Bytes = $bytes; Text = $utf8NoBom.GetString($bytes) }
}

function Get-WordCount([string] $Text) {
    return [regex]::Matches($Text, "[\p{L}\p{N}]+(?:['’_-][\p{L}\p{N}]+)*").Count
}

function Get-BodyText($Body, $SourceMap) {
    $record = $SourceMap[[string]$Body.sourcePath]
    return $utf8NoBom.GetString($record.Bytes, [int]$Body.startByte, [int]$Body.byteLength)
}

function Test-SourceRevision($Manifest) {
    $lines = New-Object System.Text.StringBuilder
    foreach ($source in @($Manifest.sources)) {
        $record = Get-SourceText $source
        $hash = Get-Sha256Hex $record.Bytes
        if ($record.Bytes.Length -ne [long]$source.byteLength -or $hash -cne [string]$source.sha256) { return $false }
        [void]$lines.Append([string]$source.path).Append([char]0).Append($record.Bytes.Length).Append([char]0).Append($hash).Append("`n")
    }
    return ("sha256:" + (Get-Sha256Hex $utf8NoBom.GetBytes($lines.ToString()))) -ceq [string]$Manifest.sourceRevision
}

function Select-Bodies($Manifest, $Selectors) {
    $selected = New-Object System.Collections.Generic.List[object]
    foreach ($selector in @($Selectors)) {
        $matches = @($Manifest.bodies | Where-Object {
            $_.sourcePath -ceq [string]$selector.path -and
            ($selector.headings -contains '*' -or $selector.headings -ccontains [string]$_.heading)
        })
        if ($matches.Count -eq 0) { throw "Selector matched no source body: $($selector.path) :: $($selector.headings -join ', ')" }
        foreach ($match in $matches) { if (-not $selected.Contains($match)) { $selected.Add($match) } }
    }
    return @($selected | Sort-Object sourcePath, sequence)
}

function S([string] $Path, [string[]] $Headings) {
    return [pscustomobject]@{ path = $Path; headings = $Headings }
}

function New-BundleDefinitions {
    return @(
        [pscustomobject]@{
            id = 'authorization'; taskClass = 'Ordinary'; currentHops = 3; candidateHops = 1
            currentSources = @('AGENTS.md','Docs/AI/SESSION_BRIEF.md','Docs/AI/ROADMAP.md','Docs/AI/Roadmap/PHASE_8_STABILIZATION.md')
            selectors = @(
                (S 'AGENTS.md' @('Startup and targeted context loading','Governing authority and milestone workflow','Non-negotiable working rules')),
                (S 'Docs/AI/SESSION_BRIEF.md' @('Purpose and authority','Current authorization and next route','Durable boundaries')),
                (S 'Docs/AI/ROADMAP.md' @('Execution rule')),
                (S 'Docs/AI/Roadmap/PHASE_8_STABILIZATION.md' @('[x] 8.25e6b Prototype and measure competing active-access models off-path')))
        },
        [pscustomobject]@{
            id = 'rule-authority'; taskClass = 'Ordinary'; currentHops = 3; candidateHops = 1
            currentSources = @('AGENTS.md','Docs/AI/SESSION_BRIEF.md','Docs/AI/RULES_LEDGER.md','Docs/AI/Rules/RULE_SYSTEMS.md')
            selectors = @(
                (S 'AGENTS.md' @('Startup and targeted context loading','Non-negotiable working rules')),
                (S 'Docs/AI/SESSION_BRIEF.md' @('Durable boundaries','Targeted routing')),
                (S 'Docs/AI/RULES_LEDGER.md' @('Purpose','Authority hierarchy','Primary local sources','Status vocabulary','Rule-system ledger','Open source and rules questions','Change policy')),
                (S 'Docs/AI/Rules/RULE_SYSTEMS.md' @('Rule-system ledger')))
        },
        [pscustomobject]@{
            id = 'card-effect'; taskClass = 'Ordinary'; currentHops = 3; candidateHops = 1
            currentSources = @('AGENTS.md','Docs/AI/SESSION_BRIEF.md','Docs/AI/CARD_SYSTEM_ARCHITECTURE.md','Docs/AI/CARD_EFFECT_CAPABILITIES.md','Docs/AI/Card-System/CORE_MODEL.md','Docs/AI/Card-System/RESOLUTION_AND_EFFECTS.md','Docs/AI/Card-System/VALIDATION_AND_EXTENSION.md')
            selectors = @(
                (S 'AGENTS.md' @('Startup and targeted context loading','Non-negotiable working rules')),
                (S 'Docs/AI/SESSION_BRIEF.md' @('Durable boundaries','Targeted routing')),
                (S 'Docs/AI/CARD_SYSTEM_ARCHITECTURE.md' @('Canonical active shards')),
                (S 'Docs/AI/CARD_EFFECT_CAPABILITIES.md' @('Purpose and status','Capability shard routing','Contract required for every capability','Cross-cutting execution rules','Capability definition validation','Adding a new effect from a verified card','Implementation boundary')),
                (S 'Docs/AI/Card-System/CORE_MODEL.md' @('Purpose and status','Non-negotiable invariants','Authoritative conceptual model','Card definitions','Card instances and state')),
                (S 'Docs/AI/Card-System/RESOLUTION_AND_EFFECTS.md' @('*')),
                (S 'Docs/AI/Card-System/VALIDATION_AND_EXTENSION.md' @('*')))
        },
        [pscustomobject]@{
            id = 'configured-presentation'; taskClass = 'Ordinary'; currentHops = 3; candidateHops = 1
            currentSources = @('AGENTS.md','Docs/AI/SESSION_BRIEF.md','Docs/AI/PROJECT_CONTEXT.md','Docs/AI/Project/PRESENTATION.md','Docs/AI/Project/CONFIGURED_PRESENTATION.md','Docs/AI/Project/MATCH_HUD.md')
            selectors = @(
                (S 'AGENTS.md' @('Startup and targeted context loading','Non-negotiable working rules')),
                (S 'Docs/AI/SESSION_BRIEF.md' @('Durable boundaries','Targeted routing')),
                (S 'Docs/AI/PROJECT_CONTEXT.md' @('*')),
                (S 'Docs/AI/Project/PRESENTATION.md' @('*')),
                (S 'Docs/AI/Project/CONFIGURED_PRESENTATION.md' @('*')),
                (S 'Docs/AI/Project/MATCH_HUD.md' @('*')))
        },
        [pscustomobject]@{
            id = 'testing'; taskClass = 'Ordinary'; currentHops = 3; candidateHops = 1
            currentSources = @('AGENTS.md','Docs/AI/SESSION_BRIEF.md','Docs/AI/TESTING.md','Docs/AI/Testing/POLICY.md')
            selectors = @(
                (S 'AGENTS.md' @('Startup and targeted context loading','Governing authority and milestone workflow','Non-negotiable working rules')),
                (S 'Docs/AI/SESSION_BRIEF.md' @('Targeted routing')),
                (S 'Docs/AI/TESTING.md' @('Canonical active shards','Required launcher')),
                (S 'Docs/AI/Testing/POLICY.md' @('Scope','Risk-tiered validation policy','Required agent entry point','Minimum by change type','Escalation triggers','Evidence and pass criteria','Living-document governance validation')))
        },
        [pscustomobject]@{
            id = 'cross-cutting-privacy'; taskClass = 'CrossCutting'; currentHops = 4; candidateHops = 1
            currentSources = @('__ALL_ACTIVE__')
            selectors = @(
                (S 'AGENTS.md' @('Startup and targeted context loading','Governing authority and milestone workflow','Non-negotiable working rules','Continuity updates')),
                (S 'Docs/AI/SESSION_BRIEF.md' @('Purpose and authority','Current authorization and next route','Durable boundaries','Targeted routing')),
                (S 'Docs/AI/ROADMAP.md' @('Execution rule')),
                (S 'Docs/AI/RULES_LEDGER.md' @('Authority hierarchy','Change policy')),
                (S 'Docs/AI/Project/PRODUCT.md' @('Rights and distribution boundary','Horizon 1 - Private pitch prototype','Rules fidelity','Technology and platforms')),
                (S 'Docs/AI/DOCUMENTATION_GOVERNANCE.md' @('Purpose and invariants','Ownership matrix','Active risk lifecycle','Required handoff and phase-gate review')),
                (S 'Docs/AI/TECHNICAL_RISKS.md' @('Active risks')),
                (S 'Docs/AI/Testing/POLICY.md' @('Risk-tiered validation policy','Evidence and pass criteria','Living-document governance validation')))
        }
    )
}

function Get-Budget($TaskClass) {
    if ($TaskClass -eq 'CrossCutting') { return [pscustomobject]@{ maxWords = 60000; maxBytes = 460800; maxUnits = 20; maxHops = 4; unit = 'ClaimGroups' } }
    return [pscustomobject]@{ maxWords = 15000; maxBytes = 122880; maxUnits = 4; maxHops = 3; unit = 'ModelSpecific' }
}

function New-Metrics([long] $Words, [long] $Bytes, [int] $Units, [int] $Hops, [long] $NecessaryBytes, $Budget) {
    $within = $Words -le $Budget.maxWords -and $Bytes -le $Budget.maxBytes -and $Units -le $Budget.maxUnits -and $Hops -le $Budget.maxHops
    return [pscustomobject][ordered]@{
        words = $Words; bytes = $Bytes; units = $Units; routingHops = $Hops
        missingAuthorityBodies = 0; unnecessaryBytes = [Math]::Max(0, $Bytes - $NecessaryBytes)
        duplicateOwnership = 0; updateCostOwners = $Units
        budgetState = if ($within) { 'WithinHypothesis' } else { 'ExceedsHypothesis' }
    }
}

function New-Experiment($Manifest) {
    if (-not (Test-SourceRevision $Manifest)) { throw 'Governance experiment input is stale against its source hashes or corpus revision.' }
    $sourceMap = @{}; foreach ($source in @($Manifest.sources)) { $sourceMap[[string]$source.path] = Get-SourceText $source }
    $sourceEntryMap = @{}; foreach ($source in @($Manifest.sources)) { $sourceEntryMap[[string]$source.path] = $source }
    $bodyMap = @{}; foreach ($body in @($Manifest.bodies)) { $bodyMap[[string]$body.id] = $body }

    $bundles = New-Object System.Collections.Generic.List[object]
    foreach ($definition in (New-BundleDefinitions)) {
        $requiredBodies = @(Select-Bodies $Manifest $definition.selectors)
        $requiredIds = @(Get-OrdinalSortedStrings ($requiredBodies | ForEach-Object { $_.id }))
        $necessaryBytes = [long](($requiredBodies | Measure-Object byteLength -Sum).Sum)

        $currentPaths = if ($definition.currentSources -contains '__ALL_ACTIVE__') {
            @($Manifest.sources | Where-Object { $_.sourceKind -eq 'GovernedActive' } | ForEach-Object { $_.path })
        } else { @($definition.currentSources) }
        foreach ($path in $currentPaths) { if (-not $sourceEntryMap.ContainsKey($path)) { throw "Unknown current-model source: $path" } }
        $currentWords = 0L; $currentBytes = 0L
        foreach ($path in $currentPaths) { $currentBytes += $sourceEntryMap[$path].byteLength; $currentWords += Get-WordCount $sourceMap[$path].Text }

        $groups = New-Object System.Collections.Generic.List[object]
        foreach ($path in (Get-OrdinalSortedStrings ($requiredBodies | ForEach-Object { $_.sourcePath }))) {
            $ownedBodies = @($requiredBodies | Where-Object { $_.sourcePath -ceq $path } | Sort-Object sequence)
            $refs = @($ownedBodies | ForEach-Object { [pscustomobject][ordered]@{ bodyId = $_.id; bodySha256 = $_.sha256 } })
            $claimIds = @(Get-OrdinalSortedStrings ($ownedBodies | ForEach-Object { $_.operativeClaimIds }))
            $groups.Add([pscustomobject][ordered]@{
                claimGroupId = "task:$($definition.id):$path"
                canonicalOwner = $path
                sourceRevision = $Manifest.sourceRevision
                sourceSha256 = $sourceEntryMap[$path].sha256
                expiryState = 'FreshUntilSourceOrEvidenceDrift'
                expiresWhen = 'Any referenced source/body/evidence hash changes or the registered reassessment trigger fires.'
                operativeClaimIds = $claimIds
                bodies = $refs
            })
        }
        $candidateWords = 0L
        foreach ($body in $requiredBodies) { $candidateWords += Get-WordCount (Get-BodyText $body $sourceMap) }
        $budget = Get-Budget $definition.taskClass
        $bundles.Add([pscustomobject][ordered]@{
            id = $definition.id; taskClass = $definition.taskClass
            requiredAuthorityBodyIds = $requiredIds
            budgetHypothesis = $budget
            currentShards = [pscustomobject][ordered]@{
                model = 'CurrentShardsWithAggregateWorkingSetEnforcement'
                sourcePaths = @(Get-OrdinalSortedStrings $currentPaths)
                metrics = New-Metrics $currentWords $currentBytes $currentPaths.Count $definition.currentHops $necessaryBytes $budget
            }
            claimRecords = [pscustomobject][ordered]@{
                model = 'ClaimRecordsWithGeneratedTaskManifest'
                authority = 'NonAuthoritative'
                notice = 'Retrieval experiment only; follow each canonicalOwner for authority.'
                groups = $groups.ToArray()
                metrics = New-Metrics $candidateWords $necessaryBytes $groups.Count $definition.candidateHops $necessaryBytes $budget
            }
        })
    }

    $briefSource = $sourceEntryMap['Docs/AI/SESSION_BRIEF.md']; $briefText = $sourceMap['Docs/AI/SESSION_BRIEF.md'].Text
    return [pscustomobject][ordered]@{
        format = 'guards-digital-governance-access-experiment'; schemaVersion = 1
        authority = 'NonAuthoritative'
        notice = 'Off-path measurement only. This artifact is not a router, canonical authority, migration decision, or publication permission.'
        input = [pscustomobject][ordered]@{ provenanceMilestone = '8.25e6a'; sourceRevision = $Manifest.sourceRevision; manifestSha256 = Get-Sha256Hex ([System.IO.File]::ReadAllBytes($inputPath)) }
        expiry = [pscustomobject][ordered]@{ state = 'FreshUntilSourceDrift'; invalidWhen = 'The input sourceRevision, any referenced source/body hash, or evidence expiry changes.' }
        budgetHypotheses = [pscustomobject][ordered]@{
            sessionBrief = [pscustomobject][ordered]@{ maxWords = 1200; maxBytes = 10240; measuredWords = Get-WordCount $briefText; measuredBytes = $briefSource.byteLength; state = if ((Get-WordCount $briefText) -le 1200 -and $briefSource.byteLength -le 10240) { 'WithinHypothesis' } else { 'ExceedsHypothesis' } }
            ordinary = [pscustomobject][ordered]@{ maxWords = 15000; maxBytes = 122880; maxUnits = 4; maxHops = 3 }
            crossCutting = [pscustomobject][ordered]@{ maxWords = 60000; maxBytes = 460800; maxClaimGroups = 20; maxHops = 4 }
        }
        models = @(
            [pscustomobject][ordered]@{ id = 'current-shards'; maintenanceCost = 'Maintain canonical shards, routing indexes, and a new aggregate budget check per representative task.'; failureModes = @('router drift','whole-file overfetch','individually valid shards exceed aggregate budget'); exactAuthorityCompatibility = 'Direct reads preserve source authority; budget enforcement must never truncate an answer.'; dualReadTransition = 'Already authoritative; compare generated manifests without changing reads.'; cutoverGate = 'Not applicable unless another model is selected after parity review.'; rollback = 'Remove only the experimental aggregate-budget check and retain all current routes.' },
            [pscustomobject][ordered]@{ id = 'claim-records'; maintenanceCost = 'Maintain stable owner-bound claim groups, evidence expiry, source/body hashes, and deterministic task-manifest generation.'; failureModes = @('stale generation','omitted authority body','duplicate owner','expired evidence','over-specific task taxonomy'); exactAuthorityCompatibility = 'References exact source bodies but remains non-authoritative until independent answer parity proves it.'; dualReadTransition = 'Generate beside current routes and require both paths to agree before any later cutover proposal.'; cutoverGate = 'Independent 8.25e6c parity plus a separately approved migration with lossless coverage.'; rollback = 'Discard generated manifests and continue reading the unchanged current router and canonical corpus.' }
        )
        bundles = $bundles.ToArray()
    }
}

function Get-ExperimentIssues($Experiment, $Manifest) {
    $issues = New-Object System.Collections.Generic.List[string]
    if ($Experiment.format -cne 'guards-digital-governance-access-experiment' -or [int]$Experiment.schemaVersion -ne 1 -or $Experiment.authority -cne 'NonAuthoritative') { $issues.Add('invalid-header') }
    if ($Experiment.input.sourceRevision -cne $Manifest.sourceRevision) { $issues.Add('stale-source') }
    if ($Experiment.input.manifestSha256 -cne (Get-Sha256Hex ([System.IO.File]::ReadAllBytes($inputPath)))) { $issues.Add('stale-manifest') }
    if ($Experiment.expiry.state -cne 'FreshUntilSourceDrift') { $issues.Add('expired-evidence') }
    $bodyMap = @{}; foreach ($body in @($Manifest.bodies)) { $bodyMap[[string]$body.id] = $body }
    $sourceMap = @{}; foreach ($source in @($Manifest.sources)) { $sourceMap[[string]$source.path] = $source }
    foreach ($bundle in @($Experiment.bundles)) {
        $required = @($bundle.requiredAuthorityBodyIds)
        $seen = @{}; $owners = @{}
        foreach ($group in @($bundle.claimRecords.groups)) {
            if ($group.expiryState -cne 'FreshUntilSourceOrEvidenceDrift') { $issues.Add("expired-evidence:$($bundle.id)") }
            if (-not $sourceMap.ContainsKey([string]$group.canonicalOwner)) { $issues.Add("unknown-owner:$($bundle.id)"); continue }
            if ($group.sourceRevision -cne $Manifest.sourceRevision -or $group.sourceSha256 -cne $sourceMap[[string]$group.canonicalOwner].sha256) { $issues.Add("stale-source:$($bundle.id)") }
            foreach ($reference in @($group.bodies)) {
                $id = [string]$reference.bodyId
                if (-not $bodyMap.ContainsKey($id)) { $issues.Add("orphan-body:$($bundle.id):$id"); continue }
                if ($bodyMap[$id].sourcePath -cne [string]$group.canonicalOwner) { $issues.Add("wrong-owner:$($bundle.id):$id") }
                if ($reference.bodySha256 -cne $bodyMap[$id].sha256) { $issues.Add("weakened-answer:$($bundle.id):$id") }
                if ($owners.ContainsKey($id) -and $owners[$id] -cne [string]$group.canonicalOwner) { $issues.Add("duplicate-owner:$($bundle.id):$id") }
                elseif ($seen.ContainsKey($id)) { $issues.Add("duplicate-owner:$($bundle.id):$id") }
                else { $owners[$id] = [string]$group.canonicalOwner; $seen[$id] = $true }
            }
        }
        foreach ($id in $required) { if (-not $seen.ContainsKey([string]$id)) { $issues.Add("missing-authority:$($bundle.id):$id") } }
        foreach ($id in $seen.Keys) { if ($required -cnotcontains $id) { $issues.Add("unnecessary-candidate-body:$($bundle.id):$id") } }
        foreach ($model in @($bundle.currentShards, $bundle.claimRecords)) {
            $units = if ($model.model -eq 'ClaimRecordsWithGeneratedTaskManifest') { @($model.groups).Count } else { @($model.sourcePaths).Count }
            $shouldBeWithin = [long]$model.metrics.words -le [long]$bundle.budgetHypothesis.maxWords -and [long]$model.metrics.bytes -le [long]$bundle.budgetHypothesis.maxBytes -and $units -le [long]$bundle.budgetHypothesis.maxUnits -and [long]$model.metrics.routingHops -le [long]$bundle.budgetHypothesis.maxHops
            $expectedState = if ($shouldBeWithin) { 'WithinHypothesis' } else { 'ExceedsHypothesis' }
            if ($model.metrics.budgetState -cne $expectedState) { $issues.Add("over-budget-or-hop:$($bundle.id):$($model.model)") }
            if ([int]$model.metrics.routingHops -lt 0) { $issues.Add("invalid-hop:$($bundle.id)") }
        }
    }
    $expectedJson = (New-Experiment $Manifest | ConvertTo-Json -Depth 20)
    $actualJson = ($Experiment | ConvertTo-Json -Depth 20)
    if ($actualJson -cne $expectedJson) { $issues.Add('non-reproducible-artifact') }
    return $issues.ToArray()
}

function Apply-DeliberateViolation($Experiment, [string] $Violation) {
    if ($Violation -eq 'None') { return $Experiment }
    $copy = Copy-JsonObject $Experiment
    $bundle = $copy.bundles[0]
    switch ($Violation) {
        'OverBudget' { $bundle.budgetHypothesis.maxWords = 1 }
        'OverHop' { $bundle.budgetHypothesis.maxHops = 0 }
        'DuplicateOwner' { $bundle.claimRecords.groups[1].bodies += @($bundle.claimRecords.groups[0].bodies[0]) }
        'ExpiredEvidence' { $bundle.claimRecords.groups[0].expiryState = 'Expired' }
        'StaleSource' { $copy.input.sourceRevision = 'sha256:' + ('0' * 64) }
        'OrphanBody' { $bundle.claimRecords.groups[0].bodies += @([pscustomobject]@{ bodyId = 'body:missing:0'; bodySha256 = ('0' * 64) }) }
        'OmittedAuthority' { $bundle.claimRecords.groups[0].bodies = @($bundle.claimRecords.groups[0].bodies | Select-Object -Skip 1) }
        'WeakenedAnswer' { $bundle.claimRecords.groups[0].bodies[0].bodySha256 = ('0' * 64) }
    }
    return $copy
}

function Invoke-SelfTests($Manifest) {
    $base = New-Experiment $Manifest
    $clean = @(Get-ExperimentIssues $base $Manifest)
    if ($clean.Count -ne 0) { throw "Governance access experiment clean self-test failed: $($clean -join ', ')" }
    $expectations = [ordered]@{
        OverBudget = 'over-budget-or-hop:*'; OverHop = 'over-budget-or-hop:*'; DuplicateOwner = 'duplicate-owner:*'
        ExpiredEvidence = 'expired-evidence:*'; StaleSource = 'stale-source'; OrphanBody = 'orphan-body:*'
        OmittedAuthority = 'missing-authority:*'; WeakenedAnswer = 'weakened-answer:*'
    }
    $passed = 1
    foreach ($name in $expectations.Keys) {
        $issues = @(Get-ExperimentIssues (Apply-DeliberateViolation $base $name) $Manifest)
        if (@($issues | Where-Object { $_ -like $expectations[$name] }).Count -eq 0) { throw "Governance access experiment did not reject $name as $($expectations[$name])." }
        $passed++
    }
    Write-Host "Governance access experiment self-tests: $passed/$passed checks passed."
}

if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) { throw 'Tools/GovernanceSourceBodyManifest.json is missing.' }
$manifest = [System.IO.File]::ReadAllText($inputPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
if (-not $SkipSelfTests) { Invoke-SelfTests $manifest }

if ($WriteExperiment) {
    $generated = New-Experiment $manifest
    $json = ($generated | ConvertTo-Json -Depth 20).Replace("`r`n", "`n") + "`n"
    [System.IO.File]::WriteAllText($outputPath, $json, $utf8NoBom)
}
if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) { throw 'GovernanceAccessExperiment.json is missing.' }
$experiment = [System.IO.File]::ReadAllText($outputPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$experiment = Apply-DeliberateViolation $experiment $DeliberateViolation
$issues = @(Get-ExperimentIssues $experiment $manifest)
if ($issues.Count -gt 0) { throw "Governance access experiment failed with $($issues.Count) issue(s): $($issues -join ', ')" }

Write-Host "Governance access experiment passed: $($experiment.bundles.Count) equivalent bundles across 2 non-authoritative models at $($experiment.input.sourceRevision)."
foreach ($bundle in @($experiment.bundles)) {
    Write-Host ("  {0}: current {1} words/{2} bytes/{3} files/{4} hops [{5}]; candidate {6} words/{7} bytes/{8} groups/{9} hops [{10}]" -f
        $bundle.id, $bundle.currentShards.metrics.words, $bundle.currentShards.metrics.bytes, $bundle.currentShards.metrics.units, $bundle.currentShards.metrics.routingHops, $bundle.currentShards.metrics.budgetState,
        $bundle.claimRecords.metrics.words, $bundle.claimRecords.metrics.bytes, $bundle.claimRecords.metrics.units, $bundle.claimRecords.metrics.routingHops, $bundle.claimRecords.metrics.budgetState)
}
