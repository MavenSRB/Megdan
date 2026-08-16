# Phase 6 - Tokens, minions, Lane Push, and end-of-round components

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_6 handoffs](../Archive/Phase-Handoffs/PHASE_6.md).

## Phase 6 - Tokens, minions, Lane Push, and end-of-round components

Phase 6 uses the complete Phase 4 map and the synthetic turn/round controller. Lane behavior is never validated on the reduced test grid.

### [x] 6.1 Resolve the end-of-round Minion Battle

Observable outcome: The authority compares Battle-Zone minions, resolves the losing team's choices with Heavy removed last, and records an end-of-round Lane-Push request if a team loses its last minion.

Acceptance checks:

- Equal counts have no effect.
- The losing team removes the exact difference or all remaining minions when it has fewer than required.
- Choice ownership and Heavy-last ordering are authoritative. A mandatory removal choice is round-owned typed pending state with stable resolution/choice identity, authorized chooser and team, required removal count, legal and currently selected stable `MinionId` values, and the exact typed select/deselect/confirm invocation that resumes the same end-of-round continuation.
- Rejected, stale, under-count, over-count, ineligible, and wrong-chooser input is atomic and preserves the pending choice, cursor, occupancy, events, and command history. Accepted confirmation removes exactly the authoritative selection and advances the owning stage once.
- The pending Minion-Battle choice is not retained in a handler-local flag or reconstructed from presentation. Its stage cursor, identities, candidates, selection, and continuation round-trip through the authoritative snapshot boundary with an explicit schema migrate-or-reject decision and reconstruct equivalently after restore, replay, and retained-prefix Undo.
- The application projection exposes the same immutable pending choice required by milestone 6.1a without acquiring rule ownership.
- Focused authority, session, snapshot-compatibility, restore, replay, and Undo tests prove equal-count/no-choice, deterministic auto-resolution, choice creation, accepted/rejected/stale input, exact resume, and byte-equivalent rebuilt pending state.
- The Lane-Push request is not consumed before token/marker cleanup.

Non-goals: No player-facing removal-choice UI, optional decline of a mandatory removal, Lane-Push consumption, token/marker cleanup, generic all-rules choice framework, production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61-resolve-the-end-of-round-minion-battle-completion-evidence).

## Corrective sequence 6.1c-6.1k

The completed corrective sequence's definitions - the audit preamble, execution-order table, and milestones 6.1c through 6.1p plus 6.1l - moved verbatim to [PHASE_6_COMPLETED.md](PHASE_6_COMPLETED.md) on 2026-08-14 during the required warning-threshold ownership review. The sequence completed on 2026-08-13; its account is maintained in [Project/FOUNDATION_CORRECTIONS.md](../Project/FOUNDATION_CORRECTIONS.md), and per-milestone evidence is addressed by SHA in the [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md).

### [x] 6.1a Add player-facing Minion Battle removal-choice UI

Observable outcome: When milestone 6.1's authority pauses for a Minion Battle removal choice, the configured passive presentation lets the authorized chooser select which eligible minions are removed and resumes the same authoritative end-of-round continuation.

Acceptance checks:

- The immutable application projection identifies the authorized chooser and team, required removal count, currently selected stable minion identities, legal candidates, and the exact typed invocation that can complete or advance the pending choice.
- The configured UI presents a contextual Minion Battle prompt and makes every currently legal candidate distinguishable by team and intrinsic minion type without relying on color alone.
- Selecting, deselecting, or confirming forwards only the projected typed invocation through `IMatchSession`; presentation does not compare minion counts, infer Heavy-last ordering, remove occupancy, or advance the end-of-round cursor.
- Equal-count and other no-choice states expose no removal controls, while rejected or stale input preserves the authoritative pending choice and refreshes the visible selection from the returned projection.
- The choice remains usable after restore, replay, and Undo rebuilds because the UI reconstructs itself from authoritative pending state rather than retaining a parallel local continuation.
- Focused application/presenter tests and a configured-scene Play Mode check prove the prompt, legal-candidate updates, exact-count completion, no-choice path, and passive-view authority boundary.

Non-goals: No Minion Battle rule changes, optional decline of a mandatory removal, Lane-Push consumption, token/marker cleanup, production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61a-add-player-facing-minion-battle-removal-choice-ui-completion-evidence).

### [x] 6.1ao Unify the Minion Battle choice cue on one downward marker

Observable outcome: Both visible minion-choice cue kinds draw the same downward-pointing marker inside the existing oval backing and differ only by color, so the board says "this minion" the same way whether the player is pointing at its control or has already selected it.

Acceptance checks:

- The selected and pointed-at cues render one identical marker glyph at a declared character size, inside milestone 6.1b's unchanged backing proportions, orientation correction, and board anchor.
- Candidate identity still never depends on color: the prompt's controls keep carrying team, intrinsic minion type, and ordinal as text, and the two cue kinds stay separable by color plus the control state that produced them.
- The cue view model stops carrying per-cue text, and no ordinal-formatting helper survives without a production caller.
- Focused presenter coverage plus the configured-scene Play Mode check, including its non-batch Game View capture, prove the shared marker, the retained color separation, and the unchanged cue lifetime.

Non-goals: No Minion Battle rule, pending-choice, projection, or prompt-control change; no new cue kind; no Defense contribution cue change; no production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61ao-unify-the-minion-battle-choice-cue-on-one-downward-marker-completion-evidence).

### [x] 6.1b Correct Defense contribution cue proportions and orientation

Observable outcome: The configured board-facing `+1` and `-1` minion Defense contribution cues use smaller readable numbers, vertically oval dark-grey backings that fit the signed values, and text mirrored around its local vertical axis to correct the inherited token orientation.

Acceptance checks:

- Both signed cues retain their exact projected `+1`/`-1` meaning and existing supporting/hindering color distinction while reducing the number size from the milestone 5.8b presentation.
- The backing is vertically oval rather than circular, uses dark grey rather than near-black, and frames the signed value without crowding.
- Only the number text is inverted around its local vertical axis; the backing remains symmetric and the resulting cue faces the configured top-down board camera correctly.
- Focused view and configured-scene Play Mode coverage proves the exact text, smaller character size, vertical oval proportions, dark-grey backing, horizontal centering plus the reviewed downward optical correction, horizontal inversion, unchanged cue lifetime, and unchanged pointer-pass-through presentation boundary for both signs.

Non-goals: No Defense calculation, contribution projection, cue identity/lifetime, target highlighting, input, Minion Battle UI, production art, animation, or audio changes.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61b-correct-defense-contribution-cue-proportions-and-orientation-completion-evidence).

## ER2-proposed foundation milestones (scheduled 2026-08-13)

These milestones were scheduled by a user-approved roadmap amendment on 2026-08-13 from the [ER2 external review record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2), which owns their findings, evidence, and closing checks. Scheduling is not authorization: each milestone still requires exact user approval before execution. Ordering constraints: 6.1q must complete before 6.2a, 6.1r before 6.2, and 6.1s before 6.4; 6.1t and 6.1u are unordered. 6.1r closes an architectural-direction finding, so per the mechanical-closure rule in [ROADMAP.md](../ROADMAP.md) its closure is verified by the already-scheduled fresh-task review 6.2b rather than by a named check.

### [x] 6.1q Scope Active-effect checkpoints and uniqueness correctly

Observable outcome: An Active effect scoped to a hero is visited at its own checkpoints, including its expiration checkpoint, regardless of which hero is acting, and uniqueness suppression compares the identity scope the definition declares instead of one global key namespace.

Acceptance checks:

- A Hero-scoped effect whose expiration checkpoint is `ActionEnded` ends during another hero's action end rather than outliving its window because `OrderForCheckpoint` filtered it out.
- Uniqueness suppression is scoped to declared controller/affected identity: two controllers activating the same definition coexist unless the verified rule says otherwise, and correctness does not depend on identity strings being embedded in `UniquenessKey` by convention.
- Existing lifecycle, snapshot, restore, replay, and retained-prefix Undo evidence stays green; the serialized format is unchanged or carries an explicit migrate-or-reject decision.

Closing check for `F-ER2-03`: the two focused lifecycle tests named in the ER2 record - cross-hero `ActionEnded` expiration, and same-key different-controller coexistence - observed failing at the pre-fix revision and passing at the post-fix revision.

Non-goals: No concrete card text, no modifier application, no new checkpoint kinds, no 6.2a effect execution.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61q-scope-active-effect-checkpoints-and-uniqueness-correctly-completion-evidence).

### [x] 6.1r Design one board-occupancy seam for every occupant kind

Observable outcome: Heroes, minions, and generic obstacles occupy the board through one authoritative occupancy/collision seam that tokens (6.2) join by declaration, rather than each occupant kind owning a parallel collection, constructor collision block, transit scan, serializer section, and projection field.

Acceptance checks:

- One authoritative occupancy index owns per-space occupancy and the one-obstacle-per-space rule for every occupant kind; adjacent-move, reachability, attack-target, and Fast-Travel queries consume it.
- Adding an occupant kind requires no new hand-written cross-collision block in the board authority's constructor and no new per-kind occupancy scan in commit validation.
- Behavior invariance: the existing movement, attack, minion, and obstacle suites pass unchanged, and the snapshot compatibility digests are byte-identical or carry an explicit migrate-or-reject decision.

Closure for `F-ER2-06`: architectural direction; verified by the already-scheduled fresh-task review 6.2b, whose scope names this seam.

Non-goals: No token or marker behavior, no minion movement (6.1s), no rule change, no board-authority rename beyond what the seam itself requires.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61r-design-one-board-occupancy-seam-for-every-occupant-kind-completion-evidence).

### [x] 6.1s Make minion occupancy mutable and fully serialized

Observable outcome: A minion's board position is authoritative mutable state that snapshots round-trip, so Object Push (6.4), Lane Push (6.9-6.11), and minion Respawn can move minions.

Acceptance checks:

- Runtime minion board state is a Core occupancy record distinct from the immutable `QuickGameMinionSetupState`, which returns to being setup data only.
- The snapshot serializes each remaining minion's identity and current coordinate; restore no longer reconstructs positions by filtering setup placements.
- The snapshot schema advances to v7 with an explicit migrate-or-reject decision and refreshed pinned compatibility fixtures per `RISK-COMPAT-001`.
- A validated authority-internal mutation path exists for a minion's position; no player-facing command is added yet.

Closing check for `F-ER2-02`: a snapshot round-trip fixture asserting a restored match preserves a mutated minion coordinate, observed failing at the pre-fix revision (no mutation path exists) and passing at the post-fix revision.

Non-goals: No Object Push, Lane Push, Spawn, or Respawn rules; no balance change; no presentation change beyond reading the authoritative coordinate.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61s-make-minion-occupancy-mutable-and-fully-serialized-completion-evidence).

### [x] 6.1t Port the cross-platform dotnet test harness

Observable outcome: The Unity-free Core/Application test majority runs on any OS through the dotnet + NUnit harness preserved on branch `er2-external-review-line`, adapted to this line's assemblies and test files.

Acceptance checks:

- `Tools/DotnetTests/GuardsDigital.DotnetTests.csproj` compiles Core, CardState, Application, and the Unity-free Edit Mode test files unmodified by glob, with an explicit exclude list so a Unity-dependent newcomer breaks the harness build rather than being silently skipped.
- The project file is tracked: the blanket `*.csproj` ignore gains a scoped negation, proven by `git check-ignore` and a fresh-checkout build.
- The map-loader shim parity fixture proves the harness loader and the Unity loader produce the same 292/210/82/7/40 map inventory.
- `dotnet test` passes on the Linux workstation and on the Windows workstation.

Closing check for `F-ER2-12`: the harness's own build tripwire plus `dotnet test` green on Linux from a fresh checkout.

Non-goals: No CI service selection or configuration, no test-behavior changes, no Unity batch-mode launcher port.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61t-port-the-cross-platform-dotnet-test-harness-completion-evidence).

### [x] 6.1u Delete dead code and single-source duplicated resolution lookups

Observable outcome: Code with no production caller is removed, the duplicated action-definition lookup has one implementation, and a wedged Lane-Push advance names its real cause.

Acceptance checks:

- `Core/Board/AdjacentDestinationQuery.cs` and its fixture, `MovementSessionCompletion`, the void `Forward*Intent` overloads in `BoardInteractionUnityView`, and the milestone-0 `TestShell*Tests` are deleted with their `.meta` files; repository-wide search proves zero remaining references.
- `FindActionDefinition` has one shared implementation with a focused test; the three per-file copies are gone.
- Advancing end-of-round with a pending Lane-Push request rejects with a typed reason naming the unimplemented consumption checkpoint instead of `MissingRequiredEndOfRoundHandler`, covered by a focused test.

Closing check for `F-ER2-10`: compilation and the directly affected suites green after deletion, plus the shared-lookup and typed-rejection tests observed failing at the pre-fix revision and passing at the post-fix revision.

Non-goals: No behavior change beyond the rejection reason, no refactor of live code paths, no relocation of coin/modifier content (owned by 7.4).

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61u-delete-dead-code-and-single-source-duplicated-resolution-lookups-completion-evidence).

## Validation-integrity milestones from the 6.1s Windows handoff

These milestones were scheduled by a user-approved roadmap amendment on 2026-08-13. Their source is the 6.1s Windows Unity tier and the complete-suite run that followed it, **not** the ER2 external review; do not attribute them to the ER2 record. Each closes a way the validation route can report green while a directly affected fixture was never executed.

6.1x is corrective and blocks the others: an active fixture is failing on `main`, and under [Testing/POLICY.md](../Testing/POLICY.md) an observed failure blocks the handoff. 6.1v and 6.1w are unordered against each other and against the rest of Phase 6.

### [x] 6.1x Re-pin the invariance digests to schema v7

Observable outcome: The complete Edit Mode suite is green, and `CardAggregateInvarianceTests` pins digests that express the schema the serializer actually writes.

Context: milestone 6.1s advanced `CurrentSchemaVersion` from 6 to 7 and re-pinned the five compatibility-corpus fixtures, but `CardAggregateInvarianceTests` pins nine scenario digests of its own, last set at 6.1f. That fixture is in the dotnet harness exclude list, so the Linux harness never ran it, and it was absent from the focused Windows filter, so 6.1s was marked complete without it ever executing. The complete Edit Mode suite at `4253eb9` reported 370/371 with that fixture failing.

Acceptance checks:

- The nine pinned scenario snapshot digests express schema v7, and the paired event digests are re-verified with their changed-or-unchanged status stated rather than assumed.
- The re-pin is justified before it is applied, at the byte level rather than by inference: the payloads either side of the change are decoded and diffed, and every differing byte is attributed to an intended cause. A re-pin that cannot explain its own delta is indistinguishable from pinning over a regression, which is what the fixture's own "do not update to match a failing run" comment exists to prevent.
- The complete Edit Mode suite is green at the post-fix revision, and the known-good baseline table in [Testing/PLAYMODE_AND_SUITES.md](../Testing/PLAYMODE_AND_SUITES.md) records the new totals with per-milestone delta attribution.
- The record states plainly that 6.1s was marked complete on evidence that omitted a directly affected fixture.

Closing check: `CardAggregateInvarianceTests.SixScenarios_ProduceByteIdenticalSnapshotsAndOrderedEvents` observed failing at `4253eb9` and passing at the post-fix revision, with the complete unfiltered Edit Mode suite green at that same revision.

Recorded outcome: the diff found two causes, both intended by 6.1s and neither an unintended state change. The serialized version field moved 6 to 7, and the board section gained an 8-byte coordinate per remaining minion - 96 bytes across this setup's twelve minions, accounting exactly for the payload's 1972 to 2068 byte growth. An intermediate reading that the version field alone was responsible was wrong, because it assumed these scenarios build a minion-free board; `CreateSession` uses `QuickGameSetupAuthority.Create`, which lays out the official setup's twelve minions. Reverting only the version constant therefore produced a hybrid encoding that never shipped, and the byte-level diff is what settled it.

Non-goals: No schema change, no serializer change, no new fixture, no reopening of the v7 migrate-or-reject decision, no change to what the invariance fixture asserts.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61x-re-pin-the-invariance-digests-to-schema-v7-completion-evidence).

### [x] 6.1v Make reflected member names and digest fixtures checkable

Observable outcome: A test that names a production member by string, or pins a snapshot digest, cannot silently drift out of agreement with the source it describes.

Context: 6.1s renamed a private field `setupState` to `occupancy`. Three fixtures resolve that field reflectively by string name, which no compiler can check; `GetField` would have returned `null` and each test would have died on a null dereference. `StandardMinionViewTests` had its assertion updated to `.Occupancy` while its reflection string was missed, so one file disagreed with itself. The 6.1x finding is the same failure mode in a second form: a fixture whose correctness depends on the schema, selected by no rule.

Acceptance checks:

- A code-truth check in `Tools/Validate-LivingDocumentation.ps1` resolves every reflected member name in the test corpus against the declaring type's source, and fails when the member does not exist. It matches the multi-line call form, not only the single-line one.
- A site that deliberately asserts a member's absence carries an explicit source marker and is exempt; the exemption is stated in the source, never inferred.
- The check fails loudly if it finds no reflection sites at all, matching the existing vacuity guards for declared types and behaviours.
- Reflected private members resolve through a helper that fails naming the type and member, instead of returning `null`.
- Every fixture that pins a snapshot digest is named in the snapshot test route, and the recipe states that a change to `CurrentSchemaVersion` requires running all of them. An enumerated focused filter must not be able to omit one again.
- Validator self-tests cover a resolving member, a missing member, an absence-marked site, a member on a partial class declared in a second file, the multi-line form, and the vacuity guard.

Closing check: the new check run against commit `5676c64`, where the three `setupState` references were genuinely stale, observed reporting them; and run at the post-fix revision, observed passing. Historical evidence rather than a synthetic mutation, because the defect existed at that commit.

Non-goals: No change to what any test asserts, no new test assembly (a shared test-utility assembly would escalate this to T3 under the testing policy), no shrinking of the harness exclude list, no removal of reflection from tests that legitimately need it.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61v-make-reflected-member-names-and-digest-fixtures-checkable-completion-evidence).

### [x] 6.1w Route the rules-ledger coverage sections to a shard

Observable outcome: `RULES_LEDGER.md` returns below its warning threshold with no content deleted, abbreviated, or weakened.

Context: the ledger is at 7,720 of its 9,000-word hard limit, so the validator warns and governance requires an ownership review. `Docs/AI/Rules/*.md` is already reserved at the same family threshold, so this needs no governance amendment or validator change.

Acceptance checks:

- The `Card architecture coverage` and `Effect capability coverage` sections move verbatim into an indexed `Docs/AI/Rules/` shard carrying the standard backlink line.
- `RULES_LEDGER.md` keeps a short routing summary and stable link for each moved section, never a copy of the narrative.
- The four required `RULES_LEDGER.md` headings are unaffected, and the shard is reachable from the session brief's routing table.

Closing check: the validator reports the `warning-threshold` warning for `RULES_LEDGER.md` at the pre-fix revision and reports no such warning at the post-fix revision, with the moved text byte-identical in its new home.

Non-goals: No rule content change, no re-verification of rule status, no sharding of the `Rule-system ledger` section itself, which is 5,695 words and belongs to its own later milestone.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61w-route-the-rules-ledger-coverage-sections-to-a-shard-completion-evidence).

## Documentation-structure milestone from the 6.1w handoff

Scheduled by a user-approved roadmap amendment on 2026-08-13. Its source is the growth the 6.1w handoff recorded, **not** a review. The approved decision is that hero content is owned per hero rather than held in one ledger section. 6.1y is unordered against the rest of Phase 6 and must complete before 8.1, which is the first milestone that would otherwise write hero content into `RULES_LEDGER.md`.

### [ ] 6.1y Give each hero its own rules-ledger shard

Observable outcome: Hero-specific rule authority lives in one shard per hero under `Docs/AI/Rules/`, starting with Wasp, and `RULES_LEDGER.md` keeps a routing summary and stable link rather than the content.

Context: 6.1w left the `Rule-system ledger` at 5,695 words and recorded that the ledger re-crosses its threshold as Wasp and the six heroes land. Per-hero shards take the larger half of that growth. The `Wasp content ledger` specifies an implementation-grade inventory of roughly twenty fields per card face; seven heroes of that material would exceed the 9,000-word family limit several times over, and no amount of rule-system sharding would hold it. `Docs/AI/Rules/*.md` is already reserved at the same family threshold by `Get-Limit`, so this needs no validator change and no threshold amendment.

Acceptance checks:

- The `Wasp content ledger` section moves verbatim into `Docs/AI/Rules/HERO_WASP.md` carrying the standard backlink line, and `RULES_LEDGER.md` keeps a short routing summary and stable link, never a copy of the narrative.
- The shard states the per-hero contract explicitly: one shard per approved hero, named `Rules/HERO_<NAME>.md`, owning that hero's inventory, capability mapping, source-verification status, hero-scoped deviations, and hero-scoped open questions. Rule systems that span heroes stay in the ledger's `Rule-system ledger`, and a deviation or open question that affects more than one hero stays in the ledger rather than being copied into each shard.
- The four required `RULES_LEDGER.md` headings are unaffected, and the shard is reachable from the session brief's routing table.
- Every roadmap milestone that currently routes hero content to `RULES_LEDGER.md` names the owning hero shard instead: 8.1's missing-or-ambiguous-content check and 8.18's per-hero equivalent. The first hero inventory must not be able to land in the wrong home by following its own acceptance check.

The governance ownership matrix already names `RULES_LEDGER.md` plus its indexed `Rules/` shards as the canonical home for rule authority. That row had named the ledger alone since 6.1w created `Rules/CARD_COVERAGE.md` without updating it, and was corrected on 2026-08-13 at the user's instruction, ahead of this milestone and independently of it. The threshold row already covered any future rules-ledger shard, so the correction amended no limit and this milestone inherits a matrix that admits its shards.

Closing check: the moved text is verified byte-identical against its pre-move revision from Git, no inbound link to a moved anchor breaks, and the validator passes with zero warnings at the post-fix revision. This milestone is preventive, so no failing pre-fix observation exists to record: `RULES_LEDGER.md` sits at 7,049 of 9,000 words and the validator is currently clean. The mechanical failing-then-passing rule governs review findings, and this is not one; claiming a transition here would be inventing evidence.

Non-goals: No sharding of the `Rule-system ledger` section, which remains its own later milestone. No shard for a hero without an approved roster slot - the five heroes joining Wasp are selected at 8.17. No Wasp inventory content, which is 8.1's work. No rule meaning, status, or verification change, no threshold change, and no validator change.

## Documentation-truth milestones from the 2026-08-14 validator-coverage finding

Scheduled by a user-approved roadmap amendment on 2026-08-14. Their source is a documentation-truth finding raised against the validator's coverage, **not** the ER2 external review and **not** review 5.8c; do not attribute them to either record. Scheduling is not authorization: each milestone requires exact user approval before execution, one at a time.

The finding is that a green validator run establishes link hygiene rather than governing-document truth. [Testing/POLICY.md](../Testing/POLICY.md) states the validator must verify "indexed active shards and archive backlinks"; it verifies neither. The link pass resolves every path and anchor that *is written*, and nothing requires a link to exist. A second class is invisible for a different reason: statements that are structurally well-formed and semantically false - a schema version the serializer no longer writes, a structure a milestone deleted, a retirement assigned to a milestone that already shipped, completion narrative governance routed to commit bodies.

All six were present at `dd535ae`, where the validator reported 51/51 self-tests, 41 governed active leaves, and **zero warnings**. That run is the finding's evidence, not a counterexample to it.

Milestone IDs continue the Phase 6.1 letter sequence into two letters. The unused single letters `j`, `m`, `n`, and `z` would serve equally well; the two-letter form was chosen so the reading order matches the scheduling order, and nothing mechanical depends on the choice.

6.1z is ordered first because the other three depend on nothing it changes, while its backlink corrections touch the same milestone blocks they would otherwise contend with. 6.1aa, 6.1ab, and 6.1ac are unordered against each other and against the rest of Phase 6. Each pairs its new check with the real stale content that check exposes, so closure is an observed failing-then-passing transition against a genuine defect rather than a synthetic mutation - the 6.1v precedent.

**2026-08-14 amendment, approved by the user.** The documentation block's execution order now lives in [PHASE_6_DOCUMENTATION.md](PHASE_6_DOCUMENTATION.md), which defines the block's remaining milestones 6.1ae through 6.1am, the separately approved post-block correction 6.1an, and their applicable authorization records. It supersedes the unordered statement in the paragraph above; 6.1z remains first. These four definitions stay authoritative here.

Semantic checks state their claims in explicitly declared ledgers that the validator reads, never inferring them from prose. This is the existing `absence-intended` and `Tools/ArchiveNarrativeBaseline.txt` pattern. [Testing/POLICY.md](../Testing/POLICY.md) requires stable text rules rather than heuristic semantic rewriting, and inference over active prose would report legitimate milestone-qualified history as a defect.

Every new check carries a vacuity guard that fails when its input set is empty, matching the existing `no-declared-types`, `no-authored-scripts`, `no-reflection-sites`, and `no-archived-bodies` guards. A check that has never been observed failing is not evidence.

### [x] 6.1z Prove the documentation graph is connected

Observable outcome: Every active shard is reachable from its family index, and every completed milestone has a stated evidence destination and a link to it.

Context: the governance ownership matrix assigns each subject a canonical home "plus its indexed shards", and [DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md) requires the active milestone definition to keep its stable `Historical evidence` backlink. Neither is checked. Today 14 of the 99 complete milestones carry no backlink - `0.1`, `6.1c`, `6.1d`, `6.1e`, `6.1k`, `6.1l`, `6.1o`, `6.1p`, `6.1q`, `6.1r`, `6.1s`, `6.1t`, `6.1w`, `6.1x` - and `6.1e` has neither an archive row nor a `## Milestone 6.1e` section, so a completed corrective milestone's closure survives only inside an addendum to another milestone's record. The shard graph happens to be connected today; the invariant is simply unenforced, so a shard added without an index entry passes.

Acceptance checks:

- `orphan-shard` fails when a governed shard is linked from no index in its family. The family table reuses the shape of `Get-Limit` so threshold ownership and index ownership cannot drift apart. `Card-System/` has two indexes owning three shards each, so the check takes their union rather than reporting all six.
- `missing-evidence-backlink` fails when a `### [x]` milestone block carries no `Historical evidence` line. Target and anchor validity remain the existing link pass's job; this check proves only that the link exists.
- `missing-archive-destination` fails when a complete milestone has neither a one-row entry nor a `## Milestone <id>` section in its phase handoff archive. Both forms are legitimate: the row is the post-2026-08-13 policy and the section is the baselined history, and a check that accepted only one would report 85 valid historical records.
- The 14 missing backlinks are added and `6.1e` receives its archive row, written from its completion commit. Milestone `0.1` created the documentation system itself and predates the archive, so it carries a stated exemption rather than a reconstructed evidence row; an invented record is a worse failure than an admitted gap.
- Validator self-tests cover a linked shard, an orphaned shard, a two-index family, a milestone with and without a backlink, both archive destination forms, an exempted milestone, and each vacuity guard.

Closing check: the three checks run at `dd535ae`, observed reporting the 14 backlink gaps and the `6.1e` destination gap; run at the post-fix revision, observed passing.

Non-goals: No rewriting of archive sections, which are append-only. No change to any milestone's status, acceptance checks, or non-goals. No new shard and no relocation of content.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61z-prove-the-documentation-graph-is-connected-completion-evidence).

### [x] 6.1aa Pin documented constants to their source

Observable outcome: A documented value that names a source constant cannot disagree with that constant.

Context: `AuthoritativeSnapshotSerializer.CurrentSchemaVersion` is 7 and has been since 6.1s. [SESSION_BRIEF.md](../SESSION_BRIEF.md) still states v6 as the current persisted schema, as do [Project/ARCHITECTURE.md](../Project/ARCHITECTURE.md) and the Minion Battle status row in [RULES_LEDGER.md](../RULES_LEDGER.md); [Project/CURRENT_IMPLEMENTATION.md](../Project/CURRENT_IMPLEMENTATION.md) states only 6.1q's historical "schema stays v6" and never names the current value. `RISK-COMPAT-001` records the v7 advance correctly, so the register and the documents it governs disagree with each other. No existing check reads a source constant into a documentation comparison: `Test-StatusCoherence` compares documents to documents, and the contract check resolves type names rather than values.

Acceptance checks:

- A declared ledger names each pinned constant, its source symbol and file, and the documents whose current-claim sections must agree. The validator reads the value from source and compares.
- `stale-pinned-constant` fails when a designated current-claim section states a different value. Milestone-qualified historical statements remain legal and untouched, which is already how the corpus writes them - the Phase 5 recipes' schema v4 and v5 references are correct history and must stay.
- The check fails loudly when a declared symbol resolves to no declaration in source, so a renamed constant cannot silently retire its own check.
- The three v6-as-current statements are corrected, and `Project/CURRENT_IMPLEMENTATION.md` states the current schema value in its own right.
- Validator self-tests cover an agreeing document, a disagreeing one, a milestone-qualified historical mention, and the missing-symbol guard.

Closing check: the check run at `dd535ae`, observed reporting the three v6 sites; run at the post-fix revision, observed passing.

Non-goals: No serializer change, no schema change, no reopening of the v7 migrate-or-reject decision, and no rewriting of correctly milestone-qualified history.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61aa-pin-documented-constants-to-their-source-completion-evidence).

### [x] 6.1ab Fail on retired structures and shipped retirement owners

Observable outcome: Documentation cannot describe a deleted structure in the present tense, and cannot assign a retirement to a milestone that already completed.

Context: 6.1h deleted the card-action strategy registry, its marker interface, its pass-through handlers, and its backend interfaces; no `*Registry` type exists under `Assets/`. [Card-System/RESOLUTION_AND_EFFECTS.md](../Card-System/RESOLUTION_AND_EFFECTS.md) was corrected to past tense in that sweep, but [Project/ARCHITECTURE.md](../Project/ARCHITECTURE.md) still states the dispatcher "is decomposed behind an explicit typed strategy registry" as a current architecture constraint. This is a missed site, not a policy disagreement. `Get-UndeclaredContractTypes` structurally cannot reach it: the prose is ordinary English, not a backticked identifier carrying a known suffix, and the check that catches a documented type which never existed is blind to one that existed and was deleted. Separately, the temporary-structure register assigns the shader dependency of `HexFillPlaceholder.mat` and `HexEdgePlaceholder.mat` to milestone 6.1d, which completed; both files still exist and the row is still `Active`.

Acceptance checks:

- A declared retired-structure ledger names each retired structure, the milestone that retired it, and the prose phrase that must no longer appear as a current claim. `retired-structure-language` fails when an active document carries the phrase outside a milestone-qualified historical construction.
- `stale-retirement-owner` fails when a `Retirement owner` cell in the temporary-structure register, or a `Retirement or reassessment` cell in the active risk register, names a milestone that is already `[x]`. A phase-scale owner such as "Phase 9 presentation" stays legal.
- The vacuity guard fails when either register parses to zero rows, so a table rename cannot silently retire the check.
- `Project/ARCHITECTURE.md` is corrected to past tense, matching the correction `Card-System/RESOLUTION_AND_EFFECTS.md` already received. The present-tense handler-registry claim in [CARD_EFFECT_CAPABILITIES.md](../CARD_EFFECT_CAPABILITIES.md) is adjudicated in the same pass: it is either stale in the same way, or a planned 6.2a contract, in which case it belongs in that document's `Contract implementation status` section where the existing check already honours it.
- The shader-dependency retirement owner moves off the completed 6.1d.
- Validator self-tests cover a retired phrase in a current claim, the same phrase in milestone-qualified history, a complete retirement owner, an incomplete one, a phase-scale owner, and both vacuity guards.

Closing check: the two checks run at `dd535ae`, observed reporting the `Project/ARCHITECTURE.md` registry prose and the 6.1d retirement owner; run at the post-fix revision, observed passing.

Non-goals: No deletion of the placeholder materials, which retire at Phase 9. No risk-register lifecycle change beyond correcting the stale owner. No production code change.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61ab-fail-on-retired-structures-and-shipped-retirement-owners-completion-evidence).

### [x] 6.1ac Keep completion history out of active documents

Observable outcome: A completed milestone's narrative lives in its commit body and its archive row, not in the session brief.

Context: governance routes new milestone outcome and validation evidence to the commit message body, gives the archive one row, and leaves the active definition its backlink. [SESSION_BRIEF.md](../SESSION_BRIEF.md) currently carries five multi-sentence completion narratives - 6.1w, 6.1v, 6.1x, 6.1s, and 6.1l - four of which are superseded. `duplicate-history` cannot see them: it matches an archived body copied verbatim into an active document, and since archive rows became one-liners there is no ≥200-character paragraph to match against. The narrative that never left the active document is exactly the case it cannot reach. This is also the growth 6.1x recorded when the brief crossed its warning threshold.

Acceptance checks:

- `completion-history-in-active-doc` fails when `SESSION_BRIEF.md` carries more than one milestone completion statement, or when the one it carries does not name the same ID as `Current completed milestone`. This is the governance rule stated as a countable one, and it reaches what `duplicate-history` cannot.
- The four superseded narratives are routed to their archive rows, and each fact is verified present in its archive row or commit body **before** removal from the brief. Standing operational facts - the player-build smoke gate, repository and authorization state, durable boundaries, approved scheduling amendments - are not completion history and stay.
- The vacuity guard fails when no completion statement is found at all, so a phrasing change cannot silently retire the check.
- Validator self-tests cover one matching statement, one mismatched statement, several statements, and the vacuity guard.

Closing check: the check run at `dd535ae`, observed reporting five completion statements; run at the post-fix revision, observed reporting one.

Non-goals: No loss of any recorded fact. No change to the route markers themselves, whose exactly-one rule is already enforced. No archive rewriting.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-61ac-keep-completion-history-out-of-active-documents-completion-evidence).

## Snapshot decode-boundary milestone from the 2026-08-14 enum-decoding finding

### [x] 6.1ad Reject undefined enum values at the snapshot decode boundary

Scheduled and executed on 2026-08-14 from an engineering-review finding against snapshot enum decoding - **not** ER2, **not** review 5.8c, and **not** the documentation-truth finding behind 6.1z through 6.1ac. Unordered against all of them; it adds a check to the same validator script, so whichever lands second takes the next unused number.

Observable outcome: A snapshot field holding an integer that is not a declared member of its enum is refused where it is read, under its own rejection reason, and no reader in the decoder can accept one.

Context: the decoder cast every payload enum straight out of the stream at 37 sites across 21 enums. Whether an undefined value was caught depended on which constructor consumed it: about half reached an `Enum.IsDefined` guard and surfaced late as `MalformedPayload`, indistinguishable from a garbled byte stream, and the rest built invalid state and carried on. `MatchAggregate.Rollback` runs this decoder on every refused command, and [PHASE_8.md](PHASE_8.md) requires reconnect to converge or enter one explicit failure state, which a decoder that cannot say why it refused cannot support.

Acceptance checks:

- `SnapshotEnumReader.Read<T>` rejects any value that is not a declared member under the new `SnapshotLoadRejectionReason.UnrecognizedEnumValue`, naming the field, the type, and the offending value; `ReadNullable<T>` does the same behind a presence flag.
- All 37 raw casts are converted, and the `AttackTargetKind` ternary becomes a switch mirroring its writer - that ternary's `default` both produced an empty target and skipped the `ReadString` a Hero or Minion target consumes, misaligning every field after it.
- The boundary guarantees membership only. Rejecting `None`, or a member illegal in a given position, stays with the domain constructors that own those rules rather than moving rule meaning into the wire layer.
- Declared members are cached per enum type rather than routed through `Enum.IsDefined`, which is reflection-backed and boxes its argument, because this decoder runs on every refused command.
- Code-truth check 6 fails on any `(<DeclaredEnum>)reader.Read…` under `Core/Snapshots/` in the single-line, multi-line, and nullable forms, with vacuity guards for an empty decoder, an empty enum set, and a decoder containing no typed read.
- The wire format is unchanged, asserted rather than assumed: `CurrentSchemaVersion` stays at 7 and all fifteen pinned digests pass unmoved. A moved digest would mean something was written differently and the milestone was wrong.

Closing check: two observed failing-then-passing transitions against genuine pre-fix defects. Check 6 at `7c65dfa` reported all 37 casts and reports none after. `AuthoritativeSnapshotSerializerTests.Deserialize_RejectsAnUndefinedEnumValueInsteadOfRestoringIt` was observed at `7c65dfa` failing `Expected: UnrecognizedEnumValue, But was: MalformedPayload`, and passing after; it demonstrates the wrong-reason half specifically, since `TeamSide` is one of the sites a constructor did catch.

Historical evidence: [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md).

Non-goals: No schema version change, wire-format change, or migration path, and no reopening of the v7 migrate-or-reject decision. No semantic validation moved into the serializer. No terminal `else` added to `ActiveEffectDefinition.ValidateDuration` and no repair of the unvalidated `SelectedInitiativeActor` constructor: this closes the path a snapshot uses to reach them, and hardening them against in-memory callers is separate work. No change to how `MatchSession.RestoreSnapshot` flattens the typed reason, which is Phase 8 work. No change to the harness exclude list, and no documentation-truth validator work, which 6.1z through 6.1ac own.

## Resolver-timing milestone from the 2026-08-14 milestone 6.2a planning finding

**Sequencing amendment (2026-08-14, user approved).** New milestone 6.1ap completes before 6.2a. The source is the milestone 6.2a planning pass recorded here, **not** the ER2 external review and **not** review 5.8c; ER2 contributed the underlying finding `F-ER2-05`, but the decision to give it its own milestone is this pass's. Ownership of `F-ER2-05` moves from 6.2a to 6.1ap, and the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2)'s disposition row carries the new owner. Every other 6.2a prerequisite is unchanged. Scheduling is not authorization: each milestone still requires exact user approval before execution.

The ground for splitting is that the correction is behavior-preserving and 6.2a is not. `F-ER2-05` closes on its own named conformance check and its own evidence - that every pinned digest stays byte-identical and the schema version does not move. Milestone 6.2a's fifth acceptance check bumps the snapshot schema and re-pins fourteen digests. Landed together, a moved digest has two candidate causes and neither observation proves anything about the other. Landed apart, the timing correction proves the digests unmoved first, and 6.2a's re-pinning then has exactly one cause to explain. This is the `Execution rule` split test in [ROADMAP.md](../ROADMAP.md): genuinely independent risk and separate acceptance evidence, above the handoff-cost floor.

### [x] 6.1ap Emit timing checkpoints from resolver steps

Observable outcome: Timing checkpoints originate from the resolution step that raises them rather than from a post-hoc rescan of the produced event list.

Context: `TurnResolutionAuthority.ApplyActionTiming` takes the event list a card action already produced, reads each entry's `CardActionResolutionEventKind`, and splices `ActionStarted`, `BeforeAttack`, `DefenseResponse`, `AfterAttack`, and `ActionEnded` around it. Timing is therefore inferred by the turn dispatcher from what a resolver happened to emit, rather than declared by the resolver at the moment it acts. An ordered-step effect engine cannot work that way: an effect node's checkpoints are declared by its capability contract and are not recoverable from an event kind, so 6.2a would either extend the rewriter with effect-shaped arms or grow a second parallel mechanism. Both are barred by the `Execution rule`'s prohibition on a shortcut the next card milestone is expected to replace.

Acceptance checks:

- Each card-action backend emits one ordered stream of facts and checkpoints, and the turn authority materializes that stream without inspecting any fact's kind. `ApplyActionTiming` is deleted rather than extended, and no replacement branches on `CardActionResolutionEventKind`.
- A compiled-member conformance test in the 6.1k style proves checkpoint events originate from resolver step members rather than a post-hoc rescan, paired with a positive rule proving each backend actually references the checkpoint emitter so the negative rule cannot pass vacuously, and with the deliberate-violator and compliant-twin samples the existing conformance fixture requires of every rule.
- The emitted checkpoint order for Movement, Attack with a Defense response, Attack against a minion, and Fast Travel is pinned by a fixture running on a session that actually carries Active effects, and is unchanged across the correction. A fixture on the placeholder configuration alone would be vacuous, because that configuration creates no Active effect and the checkpoint call returns nothing either way.
- The Active-effect lifecycle mutation order changes deliberately and is asserted: action end now runs before source-card cancellation, which is the order [RESOLUTION_AND_EFFECTS.md](../Card-System/RESOLUTION_AND_EFFECTS.md) already specifies and the rewriter's late placement inverted.
- The wire format is unchanged, asserted rather than assumed: `CurrentSchemaVersion` stays at 7 and all fifteen pinned digests pass unmoved.

Closing check: two observed failing-then-passing transitions against genuine pre-fix defects. `ResolverEmittedCheckpointConformanceTests` at `2e47c97` reported exactly one subject, `TurnResolutionAuthority.ApplyActionTiming` reaching `CardActionResolutionEvent.get_Kind`, and reports none after. `TurnTimingCheckpointOrderTests.ActionEndCheckpoint_ReachesAnEffectBeforeItsSourceCardCancellation` was observed at `2e47c97` failing with the effect already cancelled - `Triggered@TurnStarted, Triggered@ActionStarted, Cancelled@SourceCardStateChanged` and no action-end trigger - and passing after; the four order pins and the vacuity guard in that same fixture pass at both revisions, which is what makes the pair evidence that only the lifecycle application order moved.

Historical evidence: [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md).

Non-goals: No Skill action, effect node, capability catalog, or execution frame, which 6.2a owns. No schema version, wire-format, or migration change. No new `TimingCheckpoint` member and no change to which checkpoints exist or what they mean. No change to Movement, Attack/Defense, or Fast-Travel rules behavior beyond where a checkpoint is raised, and no change to the harness exclude list.

## Phase 6 gameplay milestones (continued)

**Sequencing amendment (2026-08-14, user approved).** Milestone 6.2a completes before 6.2, and 6.2 completes before 6.2b. The source is this sequencing finding, **not** the ER2 external review and **not** review 5.8c; do not attribute it to either record. The existing 6.1r-before-6.2 and 6.1q-before-6.2a constraints are unchanged, and 6.3 stays gated on 6.2b. Scheduling is not authorization: each milestone still requires exact user approval before execution.

6.2's third acceptance check makes token placement, reclamation, and marker attachment authoritative choices, and a stable-node serializable `Choose` frame is exactly what 6.2a builds. Under the former order those choices had nowhere to live. The source holds five bespoke pending-choice families and no shared contract, `DirectorCommandKind` is a closed enum over a fixed field union, and the pending card-action discriminator is `None | Movement | Attack`. Tokens would have bought a sixth pending-choice class, new command kinds, a new projection field, and a fourth discriminator arm - four shortcuts 6.2a is chartered to replace, and the `Execution rule` in [ROADMAP.md](../ROADMAP.md) already bars a narrow milestone from introducing a shortcut the next card milestone is expected to replace. The occupancy half was never at risk: 6.1r's index admits a token occupant by declaration.

This supersedes the accepted dependency order recorded in the [post-5.8c amendment](../Archive/Phase-Handoffs/PHASE_5.md#post-58c-approved-phase-6-roadmap-amendment) - 6.1, 6.1a, 6.2, 6.2a, 6.2b, then 6.3 onward. That review judged 6.2 unblocked by `F58C-02` and was right about the board state; the ground for revisiting it is acceptance check 3, which its disposition did not weigh. The archive is append-only and is not edited, so this paragraph is where the supersession is recorded.

6.2 precedes 6.2b rather than following it because `F-ER2-06`'s wording is that tokens would add a fourth per-kind collection and markers a fifth. A review closing that finding before any token exists verifies the prediction; one running after 6.2 verifies the outcome.

Milestone IDs are unchanged and 6.2 keeps its position in numeric order below, so the ER2 disposition table, the risk register, and existing cross-references stay valid. Execution order is carried by this paragraph, as it is for the 6.1c-6.1k corrective sequence above.

**Sequencing amendment (2026-08-14, user approved).** After milestone 6.2b closes its gate, the dependency-safe gameplay order begins **6.3 -> 6.8 -> 6.4**. Milestone 6.4 consumes the targeting, presence, immunity, permission, and obstacle contracts owned by 6.8, so 6.8 is an explicit prerequisite of 6.4 rather than later work. Numeric placement below is unchanged; execution follows these prerequisite declarations. Scheduling is not authorization: each milestone still requires exact user approval before execution.

**Scheduling amendment (2026-08-14, user approved).** Review 6.2b's two proposals are scheduled. Corrective micro-milestone 6.2c closes findings `F62B-01` through `F62B-04` on the named checks in its block; it does not gate 6.3, and it must complete before milestone 6.4 begins, because `F62B-04` corrects the restore re-derivation context that 6.4's input-dependent choices would expose and `F62B-03` closes the Core-seam gap that 6.7's second caller would widen. Milestone 6.10a splits the reusable system-transition continuation contract out of 6.11 per the review's determination, sitting between 6.10's durable Battle-Zone state and the respawn transition; 6.11 keeps its observable outcome and gains the corresponding prerequisite. Every closure in this sequence is a named check or 6.2a-style acceptance evidence, so no further verification milestone is scheduled. Scheduling is not authorization: each milestone still requires exact user approval before execution.

**Mechanics-showcase scheduling amendment (2026-08-14, user approved).** Six existing configured cards become staged Skill integrations after their reusable dependencies. The exact execution order, durable hand shape, acceptance checks, and non-goals for milestones 6.8a, 6.8b, 6.5a, 6.5b, 6.6a, and 6.7a live in the indexed [mechanics-showcase subshard](PHASE_6_SHOWCASE.md). The next proposed milestone remains 6.8, and scheduling still does not authorize any showcase implementation.

### [x] 6.2 Add token and marker instances, supplies, placement, and attachment

Observable outcome: Synthetic tokens and markers have stable authoritative identity without requiring Wasp content.

Prerequisite: milestone 6.2a has established the typed serializable effect frame, so this milestone's placement, reclamation, and attachment choices execute as `Choose` and effect nodes on that frame instead of becoming a sixth bespoke pending-choice family. Milestone 6.1r remains the separate board-occupancy prerequisite.

Acceptance checks:

- Tokens use the authoritative finite supply and occupy board spaces as obstacles.
- Markers are attachments rather than board occupants and have explicit return state.
- Token placement/reclamation and marker attachment are authoritative choices/transitions.
- Token and marker lifetime is not inferred from art or UI objects.

Closing check: acceptance evidence rather than a failing-then-passing pair, for the reason 6.2a recorded - this milestone adds components that did not exist rather than correcting a defect. `TokenAndMarkerComponentTests` proves a placed token blocks movement and adjacent moves through the shared occupancy index, that the supply is the catalog total minus what is on the board, and that a marker record has no coordinate to occupy a space with. `MatchSessionComponentDispatchTests` drives placement, the short-supply reclamation choice, and marker attachment end to end through `IMatchSession` with restore, replay, and retained-prefix Undo. `ComponentLifetimeOwnershipConformanceTests` is proved against a deliberate violator and a compliant twin, and pairs its negative rule with a positive one naming where component lifetime does happen.

Historical evidence: [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md).

Scope note: the user approved the three dispositions this milestone's non-goals left open. Tokens surface nowhere in the projection, so 6.2b decides how a token occupant joins presentation rather than inheriting a decision. Schema v8 is rejected rather than migrated, which was defensible to migrate for the first time and refused anyway to keep one read path per version. The marker return transition exists but neither general cleanup checkpoint is wired: end-of-round cleanup is 6.12's, and the p.13 defeat return stays with it rather than shipping alone.

Non-goals: No token-specific `DirectorCommandKind` member, pending-choice class, `MatchSessionProjection` property, or `CardActionContinuationKind` arm - the supply-exhaustion reclamation choice and the marker give/return operations ride 6.2a's frame, and a token-shaped copy of the existing per-action pending state is the exact shortcut this ordering exists to prevent. No token property behavior or Clear (6.3), displacement (6.4), Place or Spawn (6.5), Swap/Replace/Remove (6.6), or end-of-round cleanup (6.12). No Wasp content, printed-text parsing, card-name/hero-name branch, production presentation, art, animation, or audio.

### [x] 6.2a Establish typed Skill and serializable effect execution

Observable outcome: A bounded synthetic definition can offer and execute a first-class Skill through a stable-node, serializable non-Repeat effect frame before Clear or displacement effects extend the production action seam.

Acceptance checks:

- Skill is a first-class typed action offer and invocation with the same authoritative source, actor, lifecycle, command, event, timing, completion, and stale-input boundaries as existing card actions; presentation never infers Skill legality from printed text.
- Stable-node `Sequence`, `Optional`, `Conditional`, and `Choose` structures preserve typed node identity, chooser ownership, inputs, outcomes, and the exact continuation required to resume. Mandatory failure, `may`, `up to`, and `if able` policies remain explicit rather than collapsing into one failure behavior.
- One bounded synthetic capability fixture executes end to end through `IMatchSession`, the round/turn authorities, the registered action/effect dispatch seam, snapshots, restore, replay, and retained-prefix Undo. Rejected input is atomic and accepted execution emits deterministic ordered facts.
- Adding the Skill/effect handler does not require editing Movement, Attack/Defense, or Fast-Travel backend algorithms. Architecture tests mechanically prevent action-specific algorithms or effect-node switches from accumulating in the outer session/round/turn dispatcher seams.
- Pending effect state and stable node/choice identities cross the versioned snapshot boundary with an explicit migrate-or-reject decision. Restore rebinds every pending or Active effect's definition ID/revision and capability ID/revision to the trusted immutable match-definition catalog and registered capability revision; snapshot payloads supply runtime state, not definition semantics, and cannot redefine effect kind, checkpoints, duration, triggers, uniqueness, or other capability behavior. Missing or incompatible trusted revisions and payloads carrying conflicting semantic data are rejected atomically, with focused tampering tests; accepted snapshots reconstruct equivalently after restore, replay, and Undo.

Closing check: acceptance evidence rather than a failing-then-passing pair, because this milestone builds a frame that did not exist rather than correcting a defect. `MatchSessionSkillDispatchTests` drives the synthetic Skill end to end through `IMatchSession`; `EffectSnapshotTamperingTests` proves a restored Active effect is reference-equal to the catalog definition and that missing or mismatched capability revisions are refused atomically; `EffectSeamConformanceTests` and the widened dispatcher rule are each proved against a deliberate violator and a compliant twin.

Historical evidence: [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md).

Non-goals: No Repeat, Clear, direct Move, Object Push, Place, Spawn, Swap, Replace, Remove, Wasp definition, production presentation, printed-text parser, universal scripting language, or source-unverified special case.

Scope note: this milestone built the four control-node kinds its second acceptance check names. `PerformAction`, `ReplaceAction`, and `SimultaneousBatch` were never in scope despite an earlier reading of the [timing shard](../Card-System/TIMING_AND_TRANSITIONS.md) status row, which is corrected here; they are unowned and reassessed at 6.2b.

ER2 note: corrective milestones 6.1q and 6.1ap must both land first, so this engine builds on neither mis-scoped checkpoints nor an inferred timing seam. `F-ER2-05` moved to 6.1ap in the 2026-08-14 sequencing amendment above; this milestone consumes the resolver-emitted checkpoint stream that milestone establishes rather than owning the correction itself. See the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2).

### [x] 6.2b Independently verify the Phase 6 action/effect foundation

Observable outcome: A fresh-task independent review determines whether milestones 6.1 and 6.2a actually close `F58C-01` and `F58C-02` before milestone 6.3 can begin.

Acceptance checks:

- Inspect the actual final source, tests, snapshots/compatibility fixtures, replay/Undo paths, and raw validation evidence rather than relying on implementation handoff summaries.
- Trace accepted, rejected, stale, paused, restored, replayed, and Undo-truncated Minion-Battle and synthetic Skill/effect paths through the application, round/turn, action/effect, and serialization boundaries.
- Confirm mechanically that the first real end-of-round choice has no handler-local or presentation-owned continuation, and that adding the production Skill/effect handler did not deepen unrelated action-family branching.
- Determine whether Lane Push's typed resumable intra-stage continuation can remain inside milestone 6.11 or carries enough independent persistence/architecture risk to warrant a separately approved milestone 6.10a between durable Battle-Zone state and the completed respawn transition; if split, preserve 6.11's observable outcome and name the closing validation rather than pre-authorizing implementation.
- Classify every observation as a demonstrated defect, measured or plausible risk, or optional improvement; assign the smallest corrective owner and required validation for each actionable finding.
- Reassess the dependency-safe sequence and scope of milestones 6.3 through 6.13. Milestone 6.3 remains blocked until this review closes both findings, or the user approves a documented corrective disposition. Any corrective sequence this review schedules closes on named checks per [ROADMAP.md](../ROADMAP.md); it schedules a further verification milestone only for a finding whose closure cannot be expressed as a check.

Non-goals: No runtime, test, scene, package, assembly, content, or rule change; no implementation of a discovered correction inside the review milestone.

ER2 note: this review's scope also verifies the 6.1r unified board-occupancy seam (`F-ER2-06`), the architectural-direction finding the mechanical-closure rule assigns to fresh-task verification. Under the 2026-08-14 sequencing amendment above, milestone 6.2 lands first, so that verification observes tokens and markers actually declared on the seam rather than the prediction that they would be. One sub-question is named rather than left implicit: 6.1r's acceptance checks cover the constructor collision block and the commit-validation scan, while its observable outcome also names the parallel collection, serializer section, and projection field. Whether a token occupant joins by declaration across all five is this review's to determine. This sharpens a scope the review already holds; it widens none.

Review outcome (2026-08-14): `F58C-01`, `F58C-02`, and `F-ER2-06` are closed, so 6.3's prerequisite is satisfied. On the five-site sub-question: the collision block and commit-validation scan are joined by declaration; the per-kind identity collection and serializer section are additive with no cross-kind logic; the projection deliberately has no component field, and how a token occupant reaches a view belongs to the first presentation milestone that needs one. Five findings `F62B-01` through `F62B-07` were classified with dispositions in the completion commit; none is gate-blocking and every closure is a named check, so no further verification milestone is scheduled. Proposed at completion and scheduled by the 2026-08-14 scheduling amendment above: corrective micro-milestone 6.2c (Skill seam parity and vocabulary gaps, with `F62B-04` needed before 6.4 and `F62B-03` before 6.7) and a 6.10a split owning the reusable system-transition continuation contract before 6.11. `SimultaneousBatch` is assigned to 6.4; `ReplaceAction` and `PerformAction` stay specified with named triggers per the timing shard's status row. The amended 6.3-6.13 order was reassessed and kept, at an estimated 12-20 remaining focused implementation days.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-62b-independently-verify-the-phase-6-actioneffect-foundation-completion-evidence).

### [x] 6.2c Close the Skill seam parity and vocabulary gaps

Observable outcome: Skill's application, turn, and restore seams meet the same projection-gating, typed-vocabulary, and Core-revalidation bar as the Movement and Attack families, closing review findings `F62B-01` through `F62B-04` on their named checks.

Acceptance checks:

- `F62B-01`: the projection exposes no Fast-Travel option while a Skill frame is paused, matching the suppression paused Movement and Attack already get. Closing check: a focused application-session case asserting empty Fast-Travel options during a paused Skill, observed failing at the pre-fix revision.
- `F62B-02`: a refused Skill start and a refused effect-choice answer report Skill-specific typed rejection reasons, and Skill facts materialize under a Skill-specific turn event kind rather than `MovementActionProgressed`. Closing check: focused tests pinning the typed reason and event kind, observed failing pre-fix.
- `F62B-03`: starting a Skill revalidates the offer against the authoritative offer set, availability, and invocation context inside Core exactly as the shared Movement/Attack prevalidation does, and the effect-answer path prechecks source-card state so a bad state rejects instead of faulting. Closing check: a Core-seam test passing a non-authoritative Skill offer, accepted pre-fix and rejected post-fix.
- `F62B-04`: snapshot restore re-derives a paused capability's candidates against the same resolved input bindings live execution supplied, rebuilt from the persisted completed outcomes. Closing check: a test-local synthetic capability whose candidates depend on a bound input, paused across a snapshot round-trip, observed failing pre-fix.
- The authoritative wire format is unchanged: `CurrentSchemaVersion` stays 9, and the closing-check capability lives in a test-local catalog so the registered set the corpus pins does not move. If a pinned diagnostic digest moves because a Skill event's kind value changed, the re-pin identifies that as its sole cause per the standing digest discipline.

Non-goals: No new `DirectorCommandKind` member, pending-choice class, `MatchSessionProjection` property, or `CardActionContinuationKind` arm; no rule change; no schema version change; no production capability or content; no change to Movement, Attack/Defense, or Fast-Travel behavior.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-62c-close-the-skill-seam-parity-and-vocabulary-gaps-completion-evidence).

### [x] 6.3 Add core token interactions, persistent properties, and Clear

Observable outcome: Synthetic tokens preserve their properties while supporting the general board interactions needed by the core loop.

Prerequisite: milestone 6.2b has independently closed the post-5.8c action/effect foundation findings or recorded an approved correction-plus-verification disposition.

Acceptance checks:

- Token properties are continuous sources, not ordinary card Active effects.
- Clear replaces an entire Attack and may remove any chosen subset of adjacent tokens.
- A unit spawning on a token-covered Spawn point removes that token first.
- Token lifetime is defined independently from the source hero's on-board or defeated state.

Closing check: acceptance evidence rather than a failing-then-passing defect pair. `TokenAndMarkerComponentTests` proves catalog-owned token properties project as `TokenProperty` continuous sources only while the stable token instance remains placed, never enter `ActiveEffectLifecycle`, survive restoration without the source hero on the board, and are removed before a validated unit Spawn uses their covered Spawn point. `ClearActionTests` proves the value-free, text-suppressing replacement revalidates one current replaceable Attack, accepts any adjacent-token subset including none, rejects duplicate/non-adjacent/stale selections atomically, removes exactly the chosen stable identities, and emits Clear rather than Attack facts between resolver-owned action checkpoints. `MatchSessionComponentDispatchTests` carries the typed `PerformClear` command through turn/card resolution, snapshot restore, retained-prefix Undo, and byte-identical replay.

Compatibility decision: schema remains v9. The synthetic Hazard definition moves from revision 1 to revision 2 because its trusted catalog content now owns the persistent property declaration. Existing v9 payloads naming the unavailable revision 1 are rejected rather than silently acquiring revision 2 semantics; no persisted state exists outside the test corpus. The seven compatibility-corpus hashes and nine aggregate-invariance pairs are explicitly re-pinned to that one content-registration delta, with their mutation/restore/replay equality groups unchanged.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-63-add-core-token-interactions-persistent-properties-and-clear-completion-evidence).

Non-goals: No generic `ReplaceAction` effect node, pending Clear continuation, token projection or player-facing Clear UI, Wasp definition, printed-text parsing, displacement, general Place/Spawn, Swap/Replace/Remove, end-of-round cleanup, presentation, art, animation, or audio. The Spawn work is only the mandatory token-removal pre-commit seam; milestone 6.5 still owns the complete reusable Spawn capability.

The completed milestone **6.4** definition - reusable direct Move and Object Push - moved verbatim to [PHASE_6_COMPLETED.md](PHASE_6_COMPLETED.md) on 2026-08-16 during the required warning-threshold ownership review; its evidence stays addressed from the [Phase 6 archive](../Archive/Phase-Handoffs/PHASE_6.md#milestone-64-implement-reusable-direct-move-and-object-push-effects-completion-evidence).

### [x] 6.4a Integrate after-action displaced-minion correction with core Push

**Corrective amendment (2026-08-15, user approved, revised the same day).** The amendment's first version promised two additions here. Implementation refuted both in their original form, and the revision records what was actually found rather than leaving the checks unsatisfiable.

The subject widening moves to **6.8k**. Direct Move must be able to displace a minion before it can exercise the correction authority at all - a hero moved out of a space displaces nothing, and heroes are never corrected - so this milestone genuinely depends on it. But the capability's subject cannot widen until a definition has a legal way to name a non-hero one, and it has none: role bindings resolve from the execution frame, which holds no board reference, so there is no actor's-space role to bind; and naming a minion or token needs the coordinate identity Object Push uses for its targets, which needs a target-selection node. That node is reusable targeting machinery, which is milestone 6.8's declared subject rather than this milestone's, so the user directed it there. The port and the reachability query were widened under this milestone at `44c256e` and are already general; only the capability parameter waits.

The record correction becomes a **deletion**. 6.4's `DisplacementRecord` and `DisplacementKind` shipped with no producer and no consumer, appearing only in their own file, and the first version of this amendment promised to wire them to the correction authority. That was wrong: p.18 corrects "any minion miniature [that] **ends up** outside the Battle Zone after you perform an action", which is a positional predicate evaluated at the action boundary, not a query over what moved. The correction reads the board, so giving the record a consumer here would be inventing one to justify a type. The types are removed instead, and 6.4's acceptance that "the effect records the displacement" is satisfied where it always was - in the node-output facts the two capabilities publish. 6.4's evidence row is corrected in this milestone's commit. The dead contract was not mechanically detectable: `unreachable-component` scans only MonoBehaviour and ScriptableObject types, never a plain Core record.

**Correction-order amendment (2026-08-16, user approved).** The rulebook orders corrections across teams - the Tie-Breaker side resolves first, per the check below - but says nothing about the order within one team, and order matters: correcting one minion can free or block a space that changes another's shortest path. The user decided the owning team chooses. When one team has several displaced minions, the correction pauses on a subject-selection choice owned by that team before each route or fallback-Place choice; a team with exactly one displaced minion gets no such prompt, per the no-unnecessary-prompt doctrine 6.4b states. The within-team order is thereby a player decision rather than an authority-fixed ordinal, recorded as open rules question 9 in [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions) pending an official ruling.

Prerequisite: milestone **6.8k** must be complete, so direct Move can name a minion subject and satisfy the correction-exercising check below.

Observable outcome: A completed synthetic direct Move or Object Push action invokes the general displaced-minion correction once after all of that action's effect nodes and simultaneous batches finish.

Acceptance checks:

- Correction applies only to intrinsic minion miniatures outside the Battle Zone after the whole action, and is derived from board position at that boundary rather than from a record of what moved.
- A displaced token and a hero counting as a minion are both left where they are, because p.18 corrects minion miniatures only.
- Battle-Zone membership is resolved through one authority-owned query shared with Minion Battle, Heavy immunity, and Lane Push; correction does not derive or cache its own zone boundary.
- A minion follows a shortest empty path; its team chooses among equally short valid paths.
- When one team has several displaced minions, that team chooses which of its minions corrects next; a team with exactly one displaced minion sees no subject prompt.
- With no path or legal move, the minion is Placed into a nearest empty Battle-Zone space.
- The fallback implements the minimum general Place capability through typed policy/outcome data and emits Place rather than Movement facts.
- When cross-team order matters, the Tie-Breaker-side team resolves first without flipping the coin.
- Direct Move and single-/multi-target Object Push exercise the same correction authority through configured match-session commands, snapshots, restore, replay, Undo, projections, and ordered events.
- Later core displacement operations reuse this correction authority rather than creating operation-specific return logic.
- End-to-end ordering between separate Repeat instances remains with milestone 6.7, which implements Repeat as separate bounded action instances.

Non-goals: No new displacement operation, Wasp definition, card-name/hero-name branch, Lane Push request, minion Respawn, production presentation, or Repeat implementation.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-64a-integrate-after-action-displaced-minion-correction-with-core-push-completion-evidence).

### [x] 6.4b Add player-facing displaced-minion correction choices

Observable outcome: When milestone 6.4a's authority pauses for a displaced-minion route or fallback-Place choice, the configured passive presentation reuses milestone 6.1a's general authority-owned minion-choice interface so the authorized team can choose among the exact legal candidates and resume the same authoritative after-action continuation.

Acceptance checks:

- Milestone 6.1a's shared minion-choice presentation contract is the baseline: chooser/team identity, prompt/context, stable subject identity, legal candidates, current selection, confirm/advance invocation, stale refresh, and continuation status use the same application/presenter/view interface rather than a second correction-only prompt framework.
- The immutable correction projection supplies that shared interface with the displaced stable minion identity, correction kind, exact equal-shortest route or equally-nearest fallback-Place candidates, current selection, and typed invocation needed to continue.
- The shared configured UI clearly distinguishes Minion Battle removal from correction and, within correction, a correcting Move from fallback Place. Its correction-specific board adapter makes every legal route endpoint or Place destination selectable without calculating paths, distance, Battle-Zone membership, occupancy, or Tie-Breaker ordering.
- Selecting or confirming uses the same shared minion-choice presenter boundary established by 6.1a and forwards only the projected typed invocation through `IMatchSession`; presentation never moves/removes occupancy, chooses for a team, advances to another minion, or completes the owning continuation.
- Deterministic no-choice corrections expose no unnecessary prompt, while rejected or stale input preserves the authoritative pending choice and refreshes selection/highlights from the returned projection.
- Consecutive cross-team corrections visibly follow authoritative Tie-Breaker-side ordering without presentation flipping or interpreting the coin.
- Restore, replay, Undo, and restart reconstruct the prompt and highlights from authoritative pending state rather than retaining a parallel local continuation.
- Focused application/presenter tests prove both 6.1a removal and 6.4b correction use the same interface without mode leakage; a configured-scene Play Mode check covers removal-to-correction prompt replacement plus route choice, fallback Place choice, no-choice auto-continuation, stale rejection, multi-minion progression, and passive-view authority boundaries.

Non-goals: No correction-rule change, redesign of 6.1a's removal authority, one universal rules-choice framework beyond this minion-choice family, pathfinding or nearest-space calculation in presentation, optional decline, new displacement operation, Wasp definition, production art, animation, or audio.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-64b-add-player-facing-displaced-minion-correction-choices-completion-evidence).

### [x] 6.5 Complete reusable Place and Spawn effects

Observable outcome: Synthetic effects use general typed Place and Spawn operations for eligible board pieces and supplies before verified hero content is introduced.

Acceptance checks:

- Place declares its source container, eligible piece type, destination query, chooser, and no-destination policy and never emits Movement or Spawn facts.
- Spawn accepts only an eligible supply object, validates the invoking rule's destination constraints, and removes a token first when a unit legally Spawns on a token-covered Spawn point.
- Unit, minion, token, supply, occupancy, obstacle, and empty-space rules compose shared queries; presentation and art do not determine eligibility.
- Atomic rejection, pending choices, typed outcomes, snapshot state, replay, and Undo are covered with synthetic definitions on the full map.
- Milestone 6.4a's minimum minion-correction Place path is migrated to or proven conformant with the completed general Place capability without changing its verified outcome.

Non-goals: No Wasp definition, hero Respawn eligibility/timing, Lane-Push minion Respawn, card-name/hero-name branch, or production presentation.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-65-complete-reusable-place-and-spawn-effects-completion-evidence).

### [ ] 6.6 Complete reusable board-object Swap, Replace, and Remove effects

Observable outcome: Synthetic effects atomically exchange, substitute, or remove authoritative board objects through distinct reusable operations before verified hero content is introduced.

Acceptance checks:

- Swap validates both objects and resulting occupancies against one pre-effect state and emits one semantic transaction; it is neither Movement nor Place.
- Replace declares the incoming source, outgoing destination, property-transfer policy, and observable triggers and is not silently decomposed into Remove plus Spawn.
- Remove records its destination and removal/prevention outcome without Defeat rewards and remains distinct from Clear and Defeat.
- Moving, pushing, or swapping the last Battle-Zone minion never qualifies for Lane Push; a successful standalone Remove records a qualifying cause while a prevented Remove does not. Milestone 6.9 owns live request emission.
- Swap and Replace reuse milestone 6.4a's after-action correction authority whenever their completed action leaves an intrinsic minion outside the Battle Zone.
- Synthetic accepted, rejected, prevented, snapshot, replay, and Undo scenarios prove atomic state and deterministic typed outcomes.

Non-goals: No Wasp definition, card-state swap or upgrade replacement, hero Defeat consequences, specialized Respawn, card-name/hero-name branch, or production presentation.

### [ ] 6.7 Add bounded Repeat action control flow

Observable outcome: Synthetic definitions can Repeat an eligible action as a separate bounded action instance through milestone 6.2a's typed effect frame, with each instance completing displaced-minion correction before the next begins.

Acceptance checks:

- Repeat creates a separate bounded action instance with the original definition and explicit repeated context, cannot recursively Repeat, and applies the verified replacement and Active-effect restrictions.
- Each repeated action reaches its own after-action displaced-minion correction boundary before the next Repeat instance begins.
- A repeated action uses the same legality, typed choices, timing, completion, and effect outcomes as an ordinary instance; later instances may make different legal choices without mutating the original action definition.
- Declined, impossible, prevented, interrupted, restored, replayed, and Undo-truncated synthetic scenarios produce deterministic state and ordered events without recursive execution or duplicate completion.
- Focused integration tests cover Repeat across direct Move/Object Push displacement, the milestone 6.4a correction checkpoint, pending milestone 6.4b player choice, snapshot restore, replay, and retained-prefix Undo.

Non-goals: No new Skill, Sequence, Optional, Conditional, or Choose foundation; no new leaf effect merely to exercise Repeat; no Wasp definition or text interpretation, invented scripting language, card-name/hero-name branch, production presentation, or source-unverified special case.

### [x] 6.8 Implement reusable modifiers, permissions, immunity, and effective characteristics

Observable outcome: Synthetic effects and continuous sources apply the general typed modifier, restriction, permission, immunity, and effective-characteristic contracts before verified hero content is introduced.

Prerequisite: milestone 6.3 has established the core token interactions and continuous token-property sources that these general contracts must compose with.

Acceptance checks:

- Typed value modifiers retain source and channel provenance and compose deterministically with printed, minion, item-ready, and Active/continuous contributions.
- Restrictions and permissions identify their affected actor, action/effect/query, scope, lifetime, and precedence; a narrow permission does not silently disable unrelated restrictions.
- Immunity rejects targeting and automatically affected target queries while remaining visible to presence queries where the rule requires it.
- `Counts As` changes effective rules characteristics without erasing intrinsic representation, identity, supply, correction, or exception behavior.
- Serializable Active/continuous sources, simultaneous ordering, snapshot compatibility, replay, Undo, and focused accepted/rejected interaction matrices are covered with synthetic definitions.

Non-goals: No Wasp definition, source-unverified modifier, card-name/hero-name branch, production presentation, or claim that future verified cards cannot require a versioned extension.

Historical evidence: [Phase 6 handoff](../Archive/Phase-Handoffs/PHASE_6.md#milestone-68-implement-reusable-modifiers-permissions-immunity-and-effective-characteristics-completion-evidence).

**Corrective amendment (2026-08-15, user approved).** Proving milestones 6.8a and 6.8c against a card a player can hold exposed defects in the mechanisms those cards consume rather than in the cards. Milestone 6.8c and the corrections it surfaced - 6.8d, 6.8e, 6.8f, and 6.8g - live in the indexed [corrections subshard](PHASE_6_CORRECTIONS.md), which was created during the warning-threshold ownership review this amendment's additions triggered here. Their execution order, acceptance checks, and non-goals are authoritative there; scheduling still authorizes nothing.

**Corrective amendment (2026-08-16, user approved).** Milestone 6.5c is added ahead of 6.5b: converting Red Needle proved both branches through the session without giving either the surface a player answers it on, because the option/value split in an effect choice reaches no presentation. Its block is authoritative in the [corrections subshard](PHASE_6_CORRECTIONS.md). Scheduling still authorizes nothing.

**Corrective amendment (2026-08-16, user approved).** Milestone 6.5aa is added after 6.5c and ahead of 6.5b. Hands-on validation proved shared standing-immunity, exact consuming-activity targeting, effective Minion Battle, and removed-hero respawn gaps; its exact approved scope is authoritative in the [corrections subshard](PHASE_6_CORRECTIONS.md).

**Reordering amendment (2026-08-15, user approved).** Milestone 6.8b moves behind 6.4 to the slot immediately before 6.5a, and corrective milestone 6.8g is added ahead of it, because neither of Blue Feint's modes had a consumer where the card was scheduled: this milestone's `Counts As` contract reaches no production caller, and its immunity contract is consumed only for Attack while Object Push arrives at 6.4. The reordering and its reasoning are authoritative in the [mechanics-showcase subshard](PHASE_6_SHOWCASE.md); 6.8g's block is authoritative in the [corrections subshard](PHASE_6_CORRECTIONS.md). Scheduling still authorizes nothing.

### [ ] 6.9 Emit an immediate Lane-Push request during a turn

Acceptance checks:

- A recorded Defeat or successful standalone Remove of the last relevant enemy Battle-Zone minion emits the request.
- Ordinary Movement, direct Move, Object Push, Place, Spawn, Swap, Replace, and prevented Remove never emit the request; conformance reuses the implemented core capabilities rather than deferred assumptions.
- A Defeat still emits the request when its later removal is prevented.
- The request reaches the immediate checkpoint before a later Initiative action can continue.
- This milestone does not expose a partial Lane transition.

### [ ] 6.10 Resolve Lane Push Wave and Battle-Zone stages

Builds the non-executable ordered prefix: flip exactly one Wave counter and stop on final-Wave victory; otherwise advance one verified Zone toward the losing team's Throne and stop on Throne capture.

Acceptance checks:

- The incomplete prefix remains internal and cannot consume a live request.
- Authoritative `CurrentBattleZoneId` identifies the active verified Zone independently of presentation or inferred minion positions, advances exactly once at the Zone stage, and is the input to the shared Battle-Zone query used by Minion Battle, Heavy immunity, displaced-minion correction, and Lane Push.
- `CurrentBattleZoneId` crosses snapshots, restore, replay, retained-prefix Undo, restart, and compatibility handling with stable map-scoped identity; malformed, missing, or map-incompatible zone identities are rejected atomically rather than reconstructed from setup or occupancy.
- Wave and Zone stages emit deterministic typed outcomes/events.
- An early game-ending result prevents later stages.

### [ ] 6.10a Establish the reusable system-transition continuation contract

Observable outcome: A multi-step authoritative system transition pauses on a typed, serializable, authority-owned intra-stage continuation and resumes exactly where it stopped, proven on a bounded synthetic transition before milestone 6.11's respawn consumes the same contract.

Acceptance checks:

- The continuation records the owning transition identity, stable current step identity, chooser, legal choice context, and exact resume location, and is authority-owned state rather than a handler-local callback or presentation state.
- Accepted, rejected, stale, restored, replayed, and Undo-truncated resumptions cannot skip, repeat, or restart a completed step.
- The continuation crosses the versioned snapshot boundary with an explicit migrate-or-reject decision and reconstructs equivalently after restore, replay, and retained-prefix Undo.
- A conformance rule proves no transition holds handler-local continuation state, shown against a deliberate violator and a compliant twin.
- The contract's first consumer is a bounded synthetic multi-step transition fixture; milestone 6.11's Lane-Push respawn and milestone 7.5's progression are the named production consumers and are not implemented here.

Closing check: acceptance evidence in the 6.2a style, because this milestone builds a contract that did not exist - the paused synthetic transition driven through snapshot restore, replay, and retained-prefix Undo, plus the conformance rule proved against its violator and compliant twin.

Non-goals: No Lane Push Wave/Zone/respawn rules (6.10 and 6.11 own those), no minion Respawn behavior, no progression content (7.5), no production presentation, no schema change beyond the continuation itself.

### [ ] 6.11 Complete Lane Push minion respawn and transition

Completes the transition by removing all minions from the previous Battle Zone and respawning the matching type at every verified Spawn point in the new Battle Zone.

Prerequisite: milestone 6.10a has established the reusable system-transition continuation contract; this milestone's respawn continuation consumes that contract rather than defining its own shape.

Acceptance checks:

- Transition order is Wave/final-Wave check, Zone/Throne check, then old-zone removal and new-zone respawn.
- A token-covered Spawn point removes its token before respawn.
- A non-token obstacle makes the owning team Place that minion in a chosen nearest empty Battle-Zone space.
- When cross-team order matters, the Tie-Breaker-side team starts without flipping the coin.
- Respawn progress uses a typed, serializable intra-stage continuation that records the owning Lane-Push transition, stable current step/minion identity, chooser, legal choice context, and exact resume location; accepted, rejected, stale, restored, replayed, and Undo-truncated choices cannot skip, repeat, or restart Wave, Zone, removal, token removal, or prior respawns.
- The continuation is a reusable system-transition contract rather than a Lane-Push handler-local callback or presentation state, so milestone 7.5 can use the same continuation shape for multi-hero and multi-level progression.
- An immediate transition completes before later Initiative resolution continues.
- Repeat-boundary integration is deferred until the first verified Repeat implementation.

### [ ] 6.12 Remove tokens and return markers at end of round

Runs the general cleanup checkpoint: remove all board tokens and return all markers to supply except where exact verified text provides an override.

An end-of-round Lane-Push request remains pending until this cleanup finishes.

### [ ] 6.13 Retrieve cards and cancel applicable Active effects

Observable outcome: The milestone 3.8c retrieval subset is completed and integrated with the full Phase 6 state so each hero retrieves only its own eligible cards and all applicable effects end through authoritative state changes at the established checkpoint.

Acceptance checks:

- Retrieval never takes another hero's card.
- Every eligible card returns regardless of its prior eligible state, while ineligible/set-aside/item cards do not.
- Applicable Active effects, including relevant Next Turn effects, cancel through authoritative events.
- The actor-affecting sources created by Red Bulwark's configured Skill under `This Turn`, `This Round`, and `Next Turn`, plus those created by Blue Feint's configured Skill under `This Round` and `Next Turn after Playing Cards`, each expire or cancel only under their declared policy across action completion, turn/round advancement, card retrieval, source-card state change, and any applicable early-cancellation checkpoint; a focused multi-turn scenario distinguishes all four lifetime forms before the Phase 6 gate while retaining the resolved card only as the source.
- The populated retrieval/cancellation handler replaces the synthetic fixture's limited availability declaration without changing the end-of-round stage identity, continuation, or event-order contract.
- Focused tests cover resolved, discarded, facedown eligible, other-owner, and active-effect cases.
- The Phase 6 gate performs a read-only folder/file housekeeping audit covering stale generated or temporary outputs, empty or misplaced directories, duplicate artifacts, and tracked-file reference integrity. It records a bounded proposed action list; deletion, broad reorganization, and structural moves remain separately approved work, and excluded rulebook/card-corpus sources plus Unity asset/`.meta` identity must be preserved.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the focused Phase 6 scenarios.
