# Development Workflow, References, and Terminology

<!-- operative-claim:F-DOC-017 status:CurrentDemonstrated -->

Canonical active shard. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md).

## Development workflow

[ROADMAP.md](../ROADMAP.md) owns work lifecycle and review; [testing policy](../Testing/POLICY.md) owns validation breadth and evidence. Iterate on the existing private game and design PvE. Follow bounded requests through completion without repeated approval for included steps. Keep one active work item and route additional findings through the shared backlog.

Current facts belong to subject owners. Startup is a compact route, completed work moves to history, and exact evidence lives once in commit bodies. Build requests use changed/affected tests and artifact smokes; they do not automatically trigger unfiltered suites.

Preserve pure authority, passive presentation, typed legal offers/commands, deterministic replay, Undo, snapshot/content compatibility and host/seat information boundaries. Resolve ambiguous existing rules through the ledger and original PvE mechanics through approved design decisions. Use focused real-input/visual checks for changed player paths.

The former workflow, foundation chronology and obsolete scope restrictions are preserved in [the transition snapshot](../Archive/WORKFLOW_AND_TERMS_BEFORE_ITERATION_2026_10_08.txt). They no longer govern current scope; current subject contracts remain binding.

## Durable engineering safeguards

- Architecture boundaries that can be stated mechanically are enforced by project-local checks: pure rules/application code remains Unity-free, presentation uses the match-session port, passive views do not own authoritative state, and forbidden name/text parsing or service-location shortcuts fail validation.
- Deterministic scenarios compare command, rejection, event, per-command projection, snapshot, and completed-history evidence across live execution, replay, Undo-prefix replay, and restore/continuation. Bounded generated scenarios always report fixed reproduction seeds.
- Durable snapshot/content compatibility uses a small versioned fixture corpus and an explicit migrate-or-reject decision for every incompatible schema or stable content-identity change; old data must never deserialize silently into a different meaning.
- Performance captures become regression evidence through noise-tolerant thresholds and retained rationale, not premature production targets. Threshold crossings require investigation and disposition; they do not authorize speculative optimization.
- External editor/package/SDK/service dependencies are pinned with purpose, version, license/private-distribution constraints, authority/determinism reach, and upgrade/removal path. Material upgrades receive an explicit compatibility assessment and the applicable gate rerun.

## Available reference material

- `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` - primary rulebook, tracked through Git LFS under the user's 2026-08-13 confirmation of relevant public-distribution permission.
- `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` - project architecture contract derived from the general card/action rules; it is not a substitute for card-specific source verification.
- `Docs/AI/CARD_EFFECT_CAPABILITIES.md` - effect and keyword capability index/catalog, routing to capability shards where needed; it is not evidence that a capability or card is implemented.
- `Docs/Guards of Atlantis II - Tabletopia Cards/` - private reference extraction containing hero-card material and supporting scripts/assets.
- The extracted card corpus has not been accepted as complete or fully verified for implementation.
- Do not assume filenames, duplicates, image faces, or extraction completeness are authoritative without an explicit inventory milestone.
- The tracked rulebook is the sole public reference authorized on 2026-08-13. Do not distribute the extracted card corpus, map references, or other excluded source material without separate explicit authorization and appropriate rights.

## Shared terminology

- **Director mode:** One human can see and control both teams for testing and demonstration. This is not opponent AI and not the final multiplayer interaction model.
- **Pitch prototype:** Historical six-hero delivery objective; no current pitch is planned.
- **Private online playtest:** An invite-only pre-production Windows test in which six remote humans each control one hero through a host-authoritative session; it is not a public or production live service.
- **Authoritative state:** The validated rules state from which all visuals are derived.
- **Command:** A requested player or debug action sent to the rules authority.
- **Rule event:** An ordered, validated state change produced by the rules authority and consumed by presentation.
- **Card definition:** Immutable, versioned structured rules content for a printed card face; never live match state.
- **Card instance:** One stable owned runtime card that references a definition and carries its current lifecycle facts.
- **Action definition:** Immutable structured content for a printed or general-rule action before current match context is applied.
- **Action offer:** One context-specific legal use of a card or general rule, selected separately from the card itself.
- **Effect capability:** One versioned typed mutation, query, modifier, control-flow, timing, persistent-source, or system-transition contract referenced by structured card rules.
- **Pending choice:** Serializable authoritative input required to continue a paused action resolution.
- **Work item:** Bounded approved outcome with proportionate acceptance and a local commit; milestones are meaningful feature/integration outcomes.
- **Rules ledger:** The traceability record connecting rule systems to sources, implementation, tests, and deviations.


## Current explicit non-goals

Public release/commercialization, public matchmaking/services, a universal runtime card-text parser, and unrequested content/refactors remain outside scope. Existing bots follow [PRIVATE_SESSION_BOTS.md](PRIVATE_SESSION_BOTS.md); PvE design does not implicitly authorize the parked advanced bot program or hidden-information access. Music, voice, tutorials, accounts, cloud saves, telemetry and platform expansion require separate scope. The former six-hero ceiling and blanket prohibition on designing PvE are retired.
