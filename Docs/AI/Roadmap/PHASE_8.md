# Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

## Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Phase 8 begins only after the synthetic core match gate. Wasp-specific card work composes proven core actions, keywords, effect operations, queries, modifiers, control flow, and system transitions; it does not defer their first general implementation until a card requires them. The mirrored Wasp setup identities introduced earlier remain an intermediate configuration for the first complete hero gate, not the final pitch roster. After that gate, five additional user-approved heroes enter through source inventory and capability-delta mapping before the final six-distinct-hero 3v3 integration. Once the complete primitive match and URP baseline are proven, a separate private-online track moves the same authority behind a host/session boundary and reaches an invite-only six-human internet playtest before production presentation begins.

The completed definitions of milestones **8.1** through **8.10** - Wasp's source inventory and capability matrix, her verified definitions and both dealt hands, the general card-text seam and the two tray surfaces built on it, her five telekinetic Skills, the declared modifier vocabulary's consumers, the current-card rail, her two kinetic Skills and two basic cards, the tray-faced upgrade grid, the refused empty Clear, and her five printed Attacks - moved verbatim to [PHASE_8_COMPLETED.md](PHASE_8_COMPLETED.md), 8.1 through 8.7a on 2026-08-22 during milestone 8.8a's required warning-threshold ownership review and 8.8 through 8.10 on the same date during milestone 8.11a's. They remain active authority under the same `Roadmap/` threshold; this is sharding, not archiving, and their per-milestone evidence is still addressed by SHA from the [Phase 8 archive](../Archive/Phase-Handoffs/PHASE_8.md). This shard keeps 8.11 onward, which is what the next milestone reads against.

### [x] 8.11 Complete the Wasp Defense cards

Blocked by: milestone 8.8 (discard rider family) - discharged. Milestone 8.5 supplied the Defense text seam.

Observable outcome: Stop Projectiles, Deflect Projectiles, and Reflect Projectiles resolve their complete printed text as p.14 Block cards.

Acceptance checks:

- Block evaluation lands on the reserved 5.8 `DefenseBlockResult` seam at p.14's step 4: the condition - the incoming attack is ranged, read from the attack's typed subtype facts - fulfilled means a successful defense regardless of totals, unfulfilled means the defender is defeated, and no Block ever becomes a sentinel Defense amount.
- Deflect's and Reflect's on-success riders compose the defender-anchored `RangeQuery` from the card's printed Range icon: one enemy hero in range - excluding the attacker on Deflect, any enemy hero on Reflect - chosen by the defender performing the Block per p.10, discards a card, if able.
- A Defense-primary card in the played state offers only its secondary and general-rule actions, because p.14 forbids performing a Defense action on an already played card.
- The pending-defense continuation state the Block evaluation adds is an explicit schema decision with the full ceremony.

Non-goals: no continuous sources, no Ultimate.

- **The Block verdict is a fact the resolution carries, not a Defense number.** `DefenseBlockResult` lands on the seam milestone 5.8 reserved, gains `ConditionUnfulfilled` beside `Fulfilled` and `None`, and is written on the Attack resolution state - which is why this milestone moves the schema. A Block never produces a Defense amount at all: where a Defense card would be totalled, a Block short-circuits the comparison, so no sentinel value can be mistaken for a printed one and p.14's step 5 is skipped rather than fed. Unfulfilled defeats the defender through the same staged p.13 chain 8.10 registered, not through a second copy of the page.
- **p.14's already-played rule holds twice over, and the case says so rather than claiming a mechanism.** A printed Block is available in the Reaction window and nowhere else, and it declares a Hand -> DiscardPile state transition, so a card in the turn area fails both tests independently. The knockout evidence in the handoff found this: removing the window alone left the transition covering the same assertion. The case now pins the outcome and names both rules, and the window's other half - that the Block *is* offered once an Attack is being answered - is what every Block case stands on.
- **The Block keeps its printed origin; the window and the invocation context widened instead.** Milestone 8.3's rule is that a printed primary is recorded as one, so `PrintedPrimary` stays and `ActionOfferQuery` asks the definition rather than the origin. The two sites that had encoded "a Defense is a Reaction" as an origin check - the pending defense-response choice and the completed-defense match - were taught the printed Block instead of relaxed.
- **The attacker exclusion is a parameter of the Range channel and of nothing else.** "another enemy hero" is printed only by Deflect, only on a Range selection, and the attacker it excludes is the enclosing Attack's actor - a fact about the action being answered rather than about anything the text did - so it is read from the same context p.13's subtype is, through `EnclosingAttackFacts`. Declaring it per channel keeps the Radius selections' declarations byte-identical, which the corpus measurement confirms.
- **Milestone 8.4's honesty rule got its refusal case, and its subject is synthetic on purpose.** After this milestone no verified Defense owes a composition, so a case built on Wasp's own cards would assert over nothing; the discriminating pair is one synthetic card offered when its text is composed and withheld when it is not. p.14 makes refusing costless the same way p.12 does, so a withheld Defense costs the defender nothing.
- **A new corpus fixture earned its place the moment it existed.** `paused-block-rider-v24` caught a Conditional root declaring an output its predicate never published - on this milestone's rider, and latently on 8.10's repeat root, where no other fixture looked. That is the case for adding a fixture per pausing section rather than per milestone.
- **8.10's digest re-pin had no recorded cause, and this milestone recorded it.** The aggregate-invariance fixture requires every re-pin to state its byte-level delta in the re-pinning commit; 8.10 moved all nine pairs without one. The delta was measured after the fact in a throwaway worktree at 19a6def8 - schema 22 -> 23 plus four capability registrations, 126 bytes on every carried payload and 180 on the verified-hand one - and written into the fixture beside this milestone's own.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811-complete-the-wasp-defense-cards-completion-evidence).

### [x] 8.11a Official map underlay and transparent interaction overlay

Blocked by: milestone 8.11, so the current rules-content milestone closes before this bounded presentation amendment begins.

Observable outcome: The configured match presents the official `GuardsII_Small_Board` artwork as its
private board underlay while retaining the reviewed 292-space map and every existing board interaction
as a precisely registered transparent overlay.

Acceptance checks:

- A project-local deterministic preparation route reads the private Tabletopia manifest entry named
  `GuardsII_Small_Board`, extracts its declared 1588-by-1588 sprite from the declared
  `Image_17_Atlas` source, and writes only an ignored private Unity runtime asset; it does not alter,
  move, add, or commit the source atlas, extracted corpus, or map-reference directory.
- With the private runtime asset present, the complete official artwork is rendered flat on the board's
  XZ plane beneath units, tokens, spawn markers, highlights, and board-facing cues. Base tile fills and
  debug edges are transparent or visually suppressed without removing the tile views used by existing
  selection and highlighting paths.
- The authoritative 292 spaces in `official-single-lane` revision 1 register to their corresponding
  artwork positions within one declared visual tolerance across the complete board. The check uses
  reviewed map identity and coordinates; Tabletopia's 332 magnetic centres may assist calibration but
  neither create spaces nor become runtime or rules authority.
- Movement, Fast Travel, Attack targeting, effect choices, correction destinations, Hero Respawn,
  Defense contributors, Minion Battle choices, Clear selections, and token/object cues retain their
  existing projected legality and typed command paths. Their visible overlays remain readable against
  the artwork and identify the same stable space or object before and after Undo, restart, snapshot
  restore, replay, and a presentation rebuild.
- Camera framing still contains the whole authoritative map in the board band left by the HUD at the
  validated 2048-by-1152 Game View and the applicable short-viewport fallback. A targeted visual
  inspection confirms that artwork, units, transparent overlays, and the standing HUD remain legible
  without z-fighting, opaque debug tiles, clipped board edges, or the card tray concealing an actionable
  cue.
- With the private runtime asset absent, the tracked project loads and plays through the existing
  generated-board fallback without a missing-reference exception, compile failure, or rules/state
  difference. The private artwork is included only in specifically authorized internal builds and is
  not added to the public source repository or otherwise distributed by this milestone.

Non-goals: No change to the official map specification, space count, topology, zones, spawn points,
rules, commands, snapshots, or replay; no adoption of Tabletopia magnetic centres as game spaces; no
new camera controls, 3D scenery, unit art, animation, HUD redesign, public build, publication-rights
determination, or distribution authorization.

**User-approved scope amendment, 2026-08-22:** board targeting was folded into this milestone. Every choice with a board surface is answered on the board, the way an Attack target always has been, and the prompt keeps only what has no board surface.

- **The registration was settled against the artwork, not against the manifest's magnetic map.** The manifest's 332 magnetic centres fit one hexagonal lattice exactly - 332 distinct indices, worst deviation 10.4% of the 2.4074-unit spacing - and they are stated in the sprite's own frame, which was confirmed by drawing all 332 onto the extracted face and finding every one on a painted hex. What they could not settle is orientation: the twelve lattice symmetries score 272 to 283 of 292, so the best is not meaningfully better than the rest. It was settled by drawing the map's seven zones over the face, where one orientation puts every zone boundary on a painted boundary - Red Throne on the drill, Blue Throne on the crystal, both jungles on the forests, terrain on the rock ring - and no other does. **The highest-scoring lattice match is not that orientation**, which is why `OfficialBoardArtwork` records the reasoning rather than only the numbers.
- **The measured registration is a similarity and nothing looser**, fitted over the 275 spaces that have a magnetic centre: mean deviation 0.031 and worst 0.076 board units, which is 0.054 world units or 5.4% of a space's outer radius, against a declared tolerance of a tenth of a space. No shear and no mirroring, both excluded by test rather than by hope, because a landmark table cannot see either.
- **The camera stands on the board's own axes, by the user's decision.** The board's hex lattice sits about fifteen degrees inside its square, so the square and the lane cannot both be square to the screen. The artwork wins: the square reads upright as it does on the table, and the lane runs corner to corner because on this board it does. `BoardChoiceCue` takes its up from the same statement, so every board-facing cue turns with it - `StandardMinionView` had that rotation written out by hand and was the one thing that stayed crooked until it read the shared construction.
- **Nothing is removed to make room for the artwork; it is suppressed.** Tile fills become one fully transparent material, debug edges and spawn rings switch their renderers off, and every view stays configured - so selection, highlighting and the presentation tests all read exactly what they read before, and a checkout without the artwork puts all of it straight back.
- **A hex you choose is lit; a piece you choose is marked.** Ranges, correction destinations, respawn spaces and empty effect-choice candidates keep the outlined region, because the answer is the space and a region only reads as a region when it is lit. Attack targets and effect candidates naming a unit or token wear the Minion Battle's own marker instead, because the answer is the thing standing there. That split is also the only thing that reads: a tint has to contrast with pale sand, mid brown, jungle and near-black rock at once and no colour does, while the marker carries its own dark backing.
- **A Block was unreachable, and milestone 8.11 shipped it that way.** The defence preview calculated a Defense value for every offer, so a Block previewed as a card worth zero and predicted the defender losing an attack it in fact stops; the prompt then looked for the offer's printed Defense icon, found none on a Block, and threw where it was drawn - so the authority asked and the screen asked nothing. p.14's condition now lives in one `DefenseBlockQuery` that both the resolver and the preview ask, because a preview that disagreed with the resolution would be worse than none. **8.11's cases all drove the authority directly and never built the prompt**, which is the gap `PromptReachabilityConformanceTests` now closes: every offer the authority makes must be answerable on the board or in the prompt, checked from the authority's own legal sets.
- **The board never offers what the rules withhold.** Marking every Battle candidate rather than only the hovered one exposed that `Candidates` is every unit in the zone while `LegalSelectUnits` is p.8's Heavy-removed-last rule applied; the board reads the latter.
- **What the artwork could not be drawn with.** `RuntimeShaderResolutionTests` requires every shader resolved by name to be declared in Graphics settings, so two attempts were made to render the face with the Standard shader: albedo and emission both carrying the texture doubled it, and blacking the albedo left the broad specular and reflection response to this scene's flat blue-grey ambient, which bleached it even with both switched off. `Unlit/Texture` is declared instead, at the file id measured from the Editor rather than guessed into project settings.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811a-official-map-underlay-and-transparent-interaction-overlay-completion-evidence).

### [x] 8.11b Replace Wasp pins and abbreviated card indicators with verified card-table visuals

Blocked by: milestone 8.11, so Wasp's complete printed Defense set and the response path it uses are available before this bounded presentation pass begins.

Observable outcome: the configured mirrored match renders each primitive Wasp pin as the official team-coloured Wasp token, a pending Defense response renders every legal Defense card as the same complete portrait card face used in the ordinary tray, and each hero rail represents resolved and discarded cards with individual mini-card icons whose complete faces appear on mouseover.

Acceptance checks:

- A project-local deterministic preparation route reads the private Tabletopia manifest entry `Wasp` (`VerticalTokens`, element `8597310`), extracts its declared 302-by-302 Red and Blue circular token faces from `Image_1_Atlas`, and writes only ignored private Unity runtime assets. It does not alter, move, add, or commit the source atlas or extracted corpus.
- Each configured mirrored Wasp view selects the matching Red or Blue face from its projected hero content and authoritative team. The token is a flat board-readable disc centred on the hero's authoritative coordinate; art never supplies hero identity, team, occupancy, selection, or legality.
- The token replaces only the primitive prism's visible mesh/material. Existing pointer selection, collider footprint, target and choice cues, movement refresh, Defeat/respawn visibility, Undo, restart, snapshot restore, replay, and presentation rebuild continue to bind the same stable `HeroId` without duplicate or stale views.
- Every legal Defense-card invocation is drawn through the tray's shared complete card-face builder, including Initiative, name, printed values and qualifiers, exact source text, Range or Radius badge when present, and assistive colour reading. The whole card is the selectable control and forwards only its projected response invocation; `DECLINE DEFENSE` remains a distinct non-card control.
- Hover and keyboard focus on a Defense card retain the authority-computed Attack/Defense/result preview, including the offer-bound `PRINTED DEFENSE` reading, without presentation recomputing totals, Block results, eligibility, or predicted outcome. Visible order and keyboard order match the projected response order, and selection changes no card width, wrapping, or row height.
- Each occupied resolved-turn slot is a compact card-shaped icon derived from that exact resolved card. The `1`, `2`, `3`, and `4` text is removed from the slots; their fixed left-to-right positions under `RESOLVED TURNS` retain sequence, while an unoccupied reserved slot remains an empty card outline with no invented card or number.
- The discard row replaces its aggregate number and colour pips with one compact card-shaped icon for each projected discarded card. It displays no numeric count: the visible icons are the count. Their deterministic display order is derived from projected card state rather than from presentation-observed discard events.
- Resting the pointer on any occupied resolved-turn or discard icon opens the same single complete card-face preview used by the tray and current-card rail. Moving between icons replaces that preview, leaving the rail or rebuilding it closes the preview, and empty slots reveal nothing. Visible colour is supplemented by an assistive label naming the exact card and lifecycle location.
- Four legal Defense cards plus Decline remain simultaneously pointer- and keyboard-reachable at the validated 2048-by-1152 Game View, with the existing bounded-scroll fallback on short desktop viewports. A targeted Game View inspection covers both team-coloured Wasp tokens, a numeric Defense face, a Block face, Decline, preview hover/focus, and board cues against the current board presentation.
- Focused `MatchHudPresentationTests` cover shared card-face reuse, exact response forwarding, Defense-preview retention, response order, constrained-viewport reachability, icon-per-card discard representation, number-free resolved/discard slots, full-face rail previews, empty slots, and refresh without stale icons. Directly affected Play Mode coverage proves both Wasp token variants across movement, selection, Defeat/respawn and rebuild, plus the configured pending-Defense and hero-rail interaction paths. T2 validation includes one targeted Game View inspection.
- With the private token assets absent, the tracked project loads and plays through the existing generated primitive hero fallback without a missing-reference exception, compile failure, or rules/state difference. The private artwork is included only in specifically authorized internal builds and is not added to the public source repository or otherwise distributed by this milestone.

Non-goals: no change to hero or team authority, occupancy, movement, targeting, Defense eligibility or resolution, contribution totals, Block semantics, card lifecycle, discard or resolved-turn authority, snapshots or replay; no official-map underlay, 3D miniature, animation, camera change, general Phase 9 hero-art pipeline, broader HUD redesign, public build, publication-rights determination, or distribution authorization.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811b-replace-wasp-pins-and-abbreviated-card-indicators-with-verified-card-table-visuals-completion-evidence).

### [x] 8.11c Answer identity-named effect-choice candidates on the board

Blocked by: milestone 8.11a, which put every space-named candidate on the board and left this one shape behind.

Observable outcome: an effect choice that names a hero, a minion, or a token by identity is answered by clicking that piece, exactly as one that names a space already is, and its control leaves the prompt.

Acceptance checks:

- A candidate's board space is resolved from authoritative occupancy rather than from the candidate's own value kind, so a candidate carrying a `HeroId`, a `MinionId` or a `TokenInstanceId` marks and answers on the piece it names. `EffectChoiceCandidatePreview` covers space-valued candidates only today; extending the preview query is the preferred home, because the preview already exists to say what a candidate means on the board and would then supply the resulting-space reading these candidates currently lack.
- The resolution is total and asserted as such. `IBoardDisplacementAuthority.TryGetObjectAt` returns at most one object per space, so exactly one candidate resolves to any space; two is a violated invariant and fails loudly rather than falling back to a control.
- Every candidate the authority offers is marked on the piece it names or lit on the space it names, and its prompt control is withdrawn - which requires the round view model to carry whether an invocation is answerable on the board, rather than the view inferring it from the candidate's shape. `EffectChoiceDestinations`, `EffectChoiceTargets` and the generic `EffectChoices` list keep their existing meanings.
- Reflect Projectiles' rider is the live subject: it names the attacker by `HeroId`, so clicking the attacking hero answers it. Milestone 8.11a's `PromptReachabilityConformanceTests` tightens from "answerable on the board or in the prompt" to "answerable on the board" for every candidate that names an object, and the pre-fix observation is that case failing.
- Selection, targeting, correction, respawn, Minion Battle and Attack targeting keep their existing projected legality and typed command paths, and identify the same stable space or object before and after Undo, restart, snapshot restore, replay, and a presentation rebuild.

Non-goals: no change to effect-choice authority, candidate sets, legality, or command shapes; no change to the board underlay, registration, camera framing, cue vocabulary, or highlight palette; no schema move, and no new pending state.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811c-answer-identity-named-effect-choice-candidates-on-the-board-completion-evidence).

### [x] 8.11d Replace piece-target triangles with fitted rings and washes

Blocked by: milestone 8.11c, so every identity-named effect-choice candidate first has the same direct board surface whose shared cue this milestone changes.

Observable outcome: every projected hero, minion, or token target is identified by one static identity-keyed cue: a board-facing ring fitted to its base plus an exact-footprint translucent wash over its visible art, making the target immediate while keeping the art legible and reserving one compatible hover treatment for a future pointer-preview surface.

Acceptance checks:

- One shared `PieceTargetCueViewModel` projection replaces the triangle for heroes, minions, and tokens across Attack targeting, Minion Battle choices, effect choices, correction subjects, Clear, and every other existing consumer. It carries one stable board-object identity, cue state, and typed consequence intent; presentation derives no candidate, allegiance, distance, Range, immunity, outcome, or other legality.
- `Eligible` is one thick ring made from exactly eight separated segments. `Selected` is two complete rings. The reserved future `Hovered` treatment is one brighter solid ring with four short connected outward ticks; this milestone constructs and tests it but does not manufacture hover where the direct-click flow projects none. Shape, thickness, count, and luminance distinguish all three without colour or animation alone.
- `Attack` remains typed distinctly from `Removal`, but both use the same blood red because each declares a hostile consequence; `Affect` uses dark green. These intents describe the invoked operation rather than its guaranteed outcome, so prevention never rewrites the cue. Presentation reads the typed intent and never infers it from target kind, card text, display name, or colour.
- The settled consequence colours are green RGB `27/129/71` and blood red RGB `194/10/24`, over one light-neutral keyline at RGB `248/247/242`. The exact-footprint wash repeats the consequence colour at alpha `0.18` for `Eligible`, `0.23` for `Selected`, and the existing bounded-overlay maximum `0.28` for the reserved `Hovered` treatment. Assistive reading names state and consequence, while geometry keeps the distinction independent of colour.
- The cue measures each current renderer through its local circular contour or local convex base contour and transforms that contour into the board plane, rather than fitting a yaw-inflated world AABB or one fixed hero/minion/token radius. The ring's inner keyline meets that contour with no air gap; the wash fills the same contour just above the actual art; and hover ticks connect to the solid ring. The bounded tint leaves official art and labels legible and remains distinct from selectable-destination hex fills.
- Exactly one retained cue instance, mesh, and renderer follows each projected piece view. It adds no collider or input surface and changes no existing piece collider, selector, or hit area. Accepted or refused commands, pointer exit, selection changes, movement, removal, Defeat/respawn, pool reuse, Undo, restart, snapshot restore, replay, and presentation rebuild clear or reconstruct it from the current projection without stale, duplicated, orphaned, or growing native resources.
- Existing signed Defense-contributor badges, current-card/Active indicators, destination highlights, spawn markers, team treatments, and card-hover surfaces retain their meanings and visual priority. A target ring remains readable against representative light, dark, and highly detailed regions of the official board for both team treatments and every configured unit/token footprint.
- Focused presentation tests cover eligible, selected, cleared, moved, removed, restored, and rebuilt cues on a hero, minion, and token; exact circular and convex fitting; wash height, palette, opacity, blending, sorting, and retained resources; typed Attack/Removal/Affect without name or text inference; the reserved hover construction; destination-highlight coexistence; unchanged pointer surfaces; and absence of the former triangle glyph. Directly affected Play Mode paths cover Attack, Minion Battle, and identity-named effect-choice targeting without requiring a currently unreachable hover transition. T2 validation includes one targeted 2048-by-1152 Game View inspection.

Non-goals: no change to target candidates, legality, selection commands, occupancy, pointer hit areas, cue ownership, board registration, card or HUD presentation, camera behavior, animation, production VFX, or team palette. The static wash is part of the existing target cue, not a new animation/VFX system or a mutation of source art or team colour.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811d-replace-piece-target-triangles-with-fitted-rings-and-washes-completion-evidence).

### [ ] 8.11e Make accepted commands and their resulting projections atomic

Blocked by: milestone 8.11d, whose live hero-Attack target defect supplied the concrete failure: authority accepted the target and advanced to Defense, then Defense-preview projection threw and left the public projection and prompt on the already-consumed Attack choice. This correction lands before 8.12 so every later triggered-effect continuation inherits the repaired session boundary rather than another command-specific workaround.

Observable outcome: every configured player input that the match session reports accepted publishes one complete immutable projection of that same committed authority, event history, command history, and version; if construction or validation of the resulting projection faults, the input commits nothing and the previous actionable projection remains public, so authority and the visible prompt can never split.

Acceptance checks:

- One application-session transaction owns the authoritative graphs, whole-match and current-round ordered event streams, accepted-command history, version/checkpoint bookkeeping, and resulting `MatchSessionProjection`. A domain plan is not externally accepted until its next projection has been constructed and validated; on success `MatchCommandResult.Projection` is the same object as `MatchSession.Projection`, carries the incremented version and command exactly once, and describes the state and events the command committed.
- A projection constructor, invariant, or derived-preview fault after a domain plan accepted is reported as `MatchSessionRejectionReason.EngineFault`, with the exception type in the detail, rather than escaping `IMatchSession.Execute` or being relabelled as a rules refusal. Authoritative snapshot, diagnostic snapshot/events, current-round events, accepted-command order/count, version, history availability, checkpoint behavior, and pending-choice identity all remain exactly at the pre-command value, and the same previous immutable projection remains published and usable.
- `MatchSessionTests.AcceptedCommandWhoseProjectionFaults_RollsBackAuthorityEventsHistoryVersionAndCheckpointAndReturnsEngineFault` is the closing regression. It uses a one-shot test seam to let a real command pass domain validation and then fault only while its resulting projection is built; it is observed failing at the pre-fix revision and passing afterward. Retrying the same command after the one-shot fault accepts it exactly once, its Undo returns to the pre-command projection, and a fresh replay reaches the same accepted projection. The seam creates no public constructor, runtime diagnostic mode, build-configuration bypass, or Unity-owned session path.
- Ordinary refused, stale, forged, and programming-fault commands retain their distinct existing results: a clean domain refusal moves nothing, a refusal that mutated is an `AtomicityViolation`, and an exception is an `EngineFault`. None appends a command or events, increments the version, changes a checkpoint, clears a prompt, or raises a successful presenter refresh. A later legal retry remains possible from the returned/current projection.
- The configured input inventory is explicit and exercised through the path a player actually uses, not only by calling authority methods: `DirectorHandsPresenter` plus the tray for card selection/commit confirmation and empty-hand Pass; `RoundActionPresenter` plus the HUD for tied Initiative, action/Hold, Defense-card/Block and Decline, HUD-resident effect options, Fast Travel activation, Clear selection/toggle/cancel/confirm, resolve-without-respawn, end-of-round advance, and system-transition options; `BoardInteractionPresenter` plus the configured pointer selectors for Movement destinations, hero/minion Attack targets, piece- and space-valued effect choices, displaced-minion subject/destination correction, Fast Travel destinations, and hero respawn; `MinionChoicePresenter` plus board/HUD candidate and confirm paths for Minion Battle; and `UndoRestartPresenter` for Undo and restart.
- The authoritative pending-choice transition matrix covers every currently projected player-choice family and every answer shape it offers: `PendingInitiativeChoice`; `PendingMovementChoice` continue and Stop; `PendingAttackTargetChoice` hero and minion targets; `PendingDefenseResponseChoice` numeric Defense, fulfilled Block, unfulfilled Block, and Decline together with `PendingDefensePreview`; `PendingEffectChoice` Select, Deselect, Confirm, Decline, and Stop for both ordinary Skill and Attack/Block card-text continuations; `PendingDisplacedMinionCorrection` subject then destination; `PendingMinionBattleChoice` Select, Deselect, and Confirm; `PendingHeroRespawnChoice` respawn and resolve without action; and `PendingSystemTransitionStep` for the configured Lane-Push respawn/Place and Level-Up upgrade prompts. Resolution envelopes and deterministic requests such as `PendingAttackResolution`, `PendingSkillResolution`, and Lane-Push requests are classified as companions rather than invented as extra player choices.
- `ConfiguredPendingChoiceProjectionTests.EveryConfiguredChoiceAnswerPublishesTheCurrentProjectionOrCommitsNothing` drives that matrix and asserts for each row that the old prompt either advances to the exact next authoritative choice or clears, the previous invocation cannot be submitted again, all newly projected invocations belong to the new authority, and board cues, tray/HUD controls, status text, and diagnostic rows refresh from that same projection. `ConfiguredPendingChoiceProjectionTests.EveryConfiguredChoiceFamilyAndAnswerKindHasATransitionCase` is the completeness guard: a later projected choice family or answer kind must be deliberately added to the matrix rather than silently bypassing it.
- The player-reported regression is retained end to end: a configured hero Attack target whose defender has both a legal Block and nonempty minion Defense contributions is clicked through the real Input System, piece collider, board intent handler, and presenter. The target command is accepted once, `PendingAttackTargetChoice` and its ring clear, the `PendingDefenseChoice` projection property publishes its `PendingDefenseResponseChoice` together with the preview, the status advances from Attack targeting to Defense, the Block remains value-free while numeric Defense choices carry the shared minion contributions, and no engine-fault diagnostic is emitted. `MinimalHexGridPlayModeTests.Scene_HeroAttackTargetClickWithBlockAndMinionDefenseAdvancesToDefenseWithoutEngineFault` owns this check; the pure `ConfiguredDefenseCardTests.ABlockPreview_DoesNotBorrowNearbyMinionContributionsFromNumericDefense` remains the semantic companion.
- Presentation-only inputs stay presentation-only: selecting a viewed hero when no authority choice claims the click, hover/focus cues, opening/cancelling local Clear mode before confirm, card previews, and toggling the diagnostic drawer submit no command and change no authority, event stream, history, or projection version. They may repaint from the current projection but cannot manufacture a success refresh or hide an engine fault.
- Each pending family has live, paused-snapshot restore-and-continue, fresh replay, and retained-prefix Undo evidence at the existing durability boundary; restart returns to the one initial projection with no carried pause. Accepted command identity/order, ordered events, projection after the answer, final authoritative snapshot, and history cardinality agree. No speculative overlap of mutually exclusive pending families is manufactured merely to increase the matrix.
- Projection publication is equally atomic outside ordinary `Execute`. One-shot fault regressions cover a valid `RestoreSnapshot`, retained-prefix Undo reconstruction, restart reconstruction, and final fresh-replay candidate adoption. Each operation reaches its real post-reconstruction projection boundary and faults there; aggregate state, whole-match and round events, accepted history, history availability, version, checkpoint set, and the exact prior public `Projection` all remain unchanged. Retrying after the one-shot fault succeeds once and produces the same projection as the corresponding fault-free operation. These checks close the distinct current gaps where restore mutates history/version before projection and reconstruction removes checkpoints before projection; successful restore-and-continue cases alone are not rollback evidence.
- Fault-free accepted commands still build exactly one resulting public projection, authoritative captures retain their current memoized one-per-state behavior, and private replay candidates retain their existing deferred-intermediate-projection optimization. `MatchSessionDiagnosticCostTests`, `UndoCheckpointReconstructionTests`, `CardAggregateInvarianceTests`, and the deterministic complete-loop oracle remain green, with no session-length growth, duplicate serialization, ghost checkpoint, or adopted replay candidate whose final projection could not be built.
- Schema v24, `DirectorCommandKind`, card/effect definitions, legality, and serialized pending meaning remain byte-identical. `SnapshotCompatibilityCorpusTests` and `CardAggregateInvarianceTests` therefore keep their existing digests; a schema or command-shape requirement is a scope change that stops this milestone rather than being folded in. Reassess `RISK-STATE-002` because projection-fault rollback exercises the pre-image boundary more broadly, but do not claim that separate serialized-pre-image residual retired unless its own demonstrated gap is actually removed.
- This is T3 broad-risk validation: the shared `IMatchSession.Execute` command/event/pending-choice boundary reaches every Edit Mode rules consumer and every configured Play Mode presenter/input path, and its downstream blast radius is not safely bounded by the files changed. Run the focused application/atomicity, all pending-family authority, presenter/HUD, pointer, Block/minion-preview, snapshot/replay/Undo, `CompleteCoreMatchLoopTests`, and `ConfiguredEffectLifetimeGateTests` filters first; run `MinimalHexGridPlayModeTests` for the actual input paths; then run both unfiltered Edit Mode and Play Mode suites at the final revision. No non-batch visual capture or player build is required because this milestone changes publication/recovery behavior, not rendering.

Non-goals: no game-rule, verified-card, candidate, legality, action-offer, consequence, command, event, pending-choice, snapshot-schema, or content-revision change; no card/hero/display-name branch or runtime text parsing; no new prompt, error dialog, control, cue, layout, board, HUD, or other UI redesign; no public fault-injection/configuration surface, broad conversion of all in-place authorities to a new state model, replay/checkpoint redesign, networking work, or build artifact; no speculative combinations of pending states the configured authority cannot reach; and no Active-effect/continuous-trigger execution or other part of milestone 8.12.

### [ ] 8.12 Execute Active-effect and continuous trigger windows

Blocked by: milestone 8.11e, so triggered continuations inherit one atomic command/reconstruction-to-projection publication boundary.

Observable outcome: an Active-effect or continuous source that declares a triggered effect runs that effect when its trigger checkpoint arrives, proven on synthetic definitions.

Acceptance checks:

- `ActiveEffectDefinition` and the continuous-source declaration can carry a typed effect reference, and the lifecycle's `Triggered` visit - today one emit site with zero production readers - executes the declared effect through the ordinary frame (closes F-CARD-02; `ActiveEffectLifecycleTests.AnEffectDeclaringATriggerWindow_RunsItsDeclaredEffectWhenTheCheckpointArrives` observed failing pre-fix).
- Simultaneous triggered effects order through p.17's tie procedure without flipping the Tie Breaker; cancellation on defeat, card-state change, and expiry follows the implemented p.17 boundaries.
- A triggered execution that pauses rides the existing continuation contract; a new pending kind is an explicit schema decision with the full ceremony.

Non-goals: no High Voltage composition (8.13), no new trigger vocabulary beyond what the synthetic proof needs.

### [!] 8.13 Complete High Voltage

Blocked by: milestones 8.9 (the basic-action fact), 8.10 (effect-invoked Defeat), and 8.12 (trigger execution).

Observable outcome: the unlocked Ultimate's continuous ability fires after each basic skill Wasp performs, exactly as printed.

Acceptance checks:

- The 7.3 continuous Ultimate source declares the triggered ability; the trigger consumes the 8.9 performed-basic-skill fact, applies only while the hero is on the board, and fires each qualifying time.
- The optional defeat targets one enemy minion in radius through the effect-invoked staged Defeat 8.10 registered, with Heavy-minion immunity filtering targeting, defeat coins per p.17's keyword, and a resulting last-minion removal recording p.8's mid-turn Lane-Push cause exactly as any other defeat.
- The rider reads adjacency against the pre-removal state - `an enemy hero who was adjacent to that minion` - with the actor choosing among the was-adjacent heroes per p.10 and the chosen hero discarding a card, if able.
- Unlock is not claimed here: 8.14 proves the p.15/p.16 path that turns the card faceup; this milestone may stage the unlocked state directly, visibly test-only.

Non-goals: no leveling or item integration.

### [!] 8.14 Integrate Wasp progression, items, and the Ultimate unlock

Blocked by: milestones 8.6, 8.8, 8.9, 8.10, 8.11, and 8.13 - every upgraded card a Level Up can put in hand must already resolve its text.

Observable outcome: the complete p.15/p.16 progression runs over Wasp's real eighteen-card deck, from the first Level Up through the Ultimate unlock, with items composing onto verified printed icons.

Acceptance checks:

- p.15's mandatory end-of-round leveling runs over the verified upgrade pairs the 8.1 inventory records, through the implemented 7.5 transition, with the set-aside card of each pair becoming the p.16 item under its verified icon and slot.
- The `SetAside` sweep is scoped by the transition's own completed items (closes F-PROG-08; the extended `HeroLevelUpTests` item-step case observed failing pre-fix).
- Item bonuses compose per icon onto verified cards, including the 2026-08-21 approved interpretation of ledger question 12: an item's +1 reaches a matching printed icon on the Ultimate, so a +1 Radius item makes High Voltage's Radius 4; the interpretation is recorded in `RULES_LEDGER.md` and superseded by any later official ruling.
- Upgrading every non-basic card to Tier III and leveling once more unlocks the Ultimate - faceup beside the dashboard, no further levels, coins still collectable - and 8.13's ability is reachable end-to-end from an ordinary match.

Non-goals: no rebalance, no second hero.

### [!] 8.15 Complete and verify every Wasp card behavior

Observable outcome: Every Wasp definition and legal action at each pitch-relevant tier passes source-linked focused scenarios after its re-baselined dependencies are available.

Acceptance checks:

- Every offer/context from the milestone 8.2 matrix is implemented; a blocked entry keeps this milestone incomplete.
- No placeholder value, display-name branch, runtime text parsing, or UI-owned rule path remains.
- Interaction tests cover each required timing, target, modifier, replacement, repeat, state change, Active effect, and continuous source.
- Any unresolved source question prevents completion rather than being guessed.

### [!] 8.16 Complete the deterministic Wasp mirror integration gate

Observable outcome: A Wasp mirror match can start, play repeated turns/rounds, and end through every victory path with primitive visuals and debug UI.

Acceptance checks:

- Every implemented Wasp rule links to an authoritative source and focused tests.
- Approved deviations are the only known departures.
- Undo, restart, snapshots, event log, and Tie Breaker override work internally.
- A broader source-linked Wasp integration suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

### [!] 8.16a Independently review Wasp-driven card-engine extensibility

Blocked by: Milestone 8.16 must pass its source-linked Wasp integration gate.

Observable outcome: A fresh analysis-only review identifies what the first complete verified hero revealed about the generic card/effect architecture before five additional heroes multiply its extension costs.

Acceptance checks:

- The review traces every implemented Wasp capability to its source, typed contract, handler or composition, serialized continuation/state, ordered events, and focused/integration evidence.
- It searches for card-name or hero-name branching, runtime printed-text parsing, untyped universal payloads, Wasp-specific behavior disguised as a generic capability, duplicated patterns that now justify a shared capability, and speculative abstractions supported by only one exceptional card.
- It evaluates effect/query/modifier/continuous-source performance and test maintainability using actual Wasp scenarios rather than synthetic assumptions alone.
- Findings follow the common independent-review evidence contract and distinguish legitimate isolated custom handlers from leakage that would make the next five heroes increasingly expensive.
- Any correction is proposed separately; the review changes no rules, content, runtime implementation, card values, or source interpretation.

Non-goals: No selection, inventory, or implementation of another hero; no Wasp rebalance; no generic framework merely to eliminate every custom handler.

Hard gate: Additional pitch-hero inventory and implementation do not begin until milestone 8.16a and every gate-blocking corrective milestone are reviewed and approved. Production visual work remains blocked until the six-hero 3v3 gate at milestone 8.27, the render-pipeline migration gate at milestone 8.28 with its independent review at 8.28a, and the private online playtest gate at milestone 8.36 with its independent review at 8.36a are reviewed and approved, matching the sibling gate statements.

### [!] 8.17 Select the five additional pitch heroes

Observable outcome: The user approves five specific heroes that join Wasp as six different heroes divided into three Red and three Blue roster slots for the private pitch.

Acceptance checks:

- The proposed roster contains no duplicate hero and identifies each hero's intended team slot without treating team assignment as a card-rule branch.
- Implementation-grade card, dashboard, token/marker, miniature/presentation, and applicable official-ruling sources are confirmed available or explicitly recorded as blockers.
- Selection considers effect-family and presentation breadth so the roster demonstrates the reusable architecture without claiming equal implementation cost for every hero.
- The exact five-hero choice receives explicit user approval before inventory begins.

Non-goals: No card transcription, effect implementation, production art, balance adjustment, or seventh pitch hero.

Milestone 8.18 was split five ways by the 8.2 re-baseline (2026-08-21, user approved, applying the 7.9a recommendation): its own acceptance contract says a missing component blocks the affected hero, but one milestone made a single blocked hero hold four others, and the one-shard-per-hero contract already exists. Each slice below applies the same inventory contract to the one hero milestone 8.17 assigns to its slot: every required face and component present and readable; structured data manually checked against source images and applicable official rulings; missing, ambiguous, or conflicting content recorded in that hero's own `Rules/HERO_<NAME>.md` shard per the milestone 6.1y contract, blocking only that hero rather than being guessed, with content affecting more than one hero staying in `RULES_LEDGER.md`; and no implementation from unverified filenames, OCR, display text, or assumed cross-hero similarity.

### [!] 8.18a Inventory and verify pitch hero slot A

Observable outcome: the slot-A hero's cards, tiers, values, action types, text, item possibilities, Ultimate, tokens/markers, and other hero-owned components are traceable to source material under the shared 8.18 contract above.

### [!] 8.18b Inventory and verify pitch hero slot B

Observable outcome: the slot-B hero's complete set is source-verified under the shared 8.18 contract above.

### [!] 8.18c Inventory and verify pitch hero slot C

Observable outcome: the slot-C hero's complete set is source-verified under the shared 8.18 contract above.

### [!] 8.18d Inventory and verify pitch hero slot D

Observable outcome: the slot-D hero's complete set is source-verified under the shared 8.18 contract above.

### [!] 8.18e Inventory and verify pitch hero slot E

Observable outcome: the slot-E hero's complete set is source-verified under the shared 8.18 contract above.

### [!] 8.19 Map the five heroes to capabilities and re-baseline the 3v3 pitch work

Observable outcome: Every verified action and hero-owned component maps to existing or new typed capabilities, and the remaining hero, 3v3 integration, and presentation milestones are re-baselined from evidence.

Acceptance checks:

- A roster-wide matrix distinguishes reused core/Wasp capabilities, reusable refinements, new primitives, and genuinely unique isolated handlers for every verified hero face and component.
- Every effect node maps to a capability ID/version, typed bindings, policy, failure/atomicity rule, checkpoints, and source-linked tests.
- The designated roadmap audit applies the matrix to milestones 8.20-8.36 and Phase 9, recommending justified keep/merge/split/defer/remove decisions and recording an updated remaining-effort range.
- Approved implementation slices are dependency ordered; a shared capability precedes the first hero that needs it, and no hero-name rule branch or runtime text parsing is scheduled.
- Any schema change is explicit and migration-tested rather than hidden inside a hero milestone.

Non-goals: No effect implementation or production art.

### [!] 8.20 Complete and verify additional pitch hero slot A

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.21 Complete and verify additional pitch hero slot B

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.22 Complete and verify additional pitch hero slot C

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.23 Complete and verify additional pitch hero slot D

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.24 Complete and verify additional pitch hero slot E

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.25 Replace the staged 1v1 setup with the official six-player Quick Game configuration

Observable outcome: Configuration data places three different Red heroes and three different Blue heroes with their own hands and components using the official six-player Quick Game baseline.

Acceptance checks:

- All six stable player/hero identities, ownership relationships, hero spawn positions, hands, dashboards, and hero-owned components resolve without assuming one hero per team.
- Every authoritative turn slot contains exactly six ownership-validated assignments—one for each configured `HeroId`—and snapshot round-trip preserves their deterministic order and committed-card-or-empty-hand-Pass facts; controlling `PlayerId` and team resolve through the participant graph.
- Setup uses three Wave counters, five Life counters per team, the normal Tie Breaker flip, and the verified official board/minion setup.
- Wasp appears once; all six heroes are different and assigned to exactly one team/player.
- DEV-001 and DEV-002 are marked retired; only approved director/debug deviations remain.
- Snapshots round-trip the complete six-player setup deterministically.
- `RISK-SCALE-001`'s payload half is measured rather than computed: the six-player decoded payload is recorded against the staged 1v1 reference of 6,633 bytes and compared with the standing thresholds in `Testing/FUTURE_GATES.md`. This milestone is the first at which 108 card instances and 108 definitions exist, and the risk's own figure for them is arithmetic on measured unit costs rather than a measurement.
- The player-build smoke gate's pinned `heroes=2 cards=36` markers move to the six-player values in `Testing/FUTURE_GATES.md` in this milestone - the marker line this configuration breaks now has an owner, per the 7.9a documentation correction the 8.2 re-baseline applied.

Non-goals: No networking, hidden per-player devices, AI, matchmaking, or balance changes. No reassessment of `RISK-SCALE-001`'s ordering half, which needs a round boundary this milestone does not reach.

### [!] 8.26 Integrate six-hero director control and team interactions

Observable outcome: One director can play and resolve a complete turn for all six independently owned heroes while the rules preserve normal 3v3 ownership, Initiative, target, assist, reaction, and team relationships.

Acceptance checks:

- The director can access each hero's correct hand; for every hero with a nonempty hand the director must choose exactly one owned card, while an empty hand records a Pass. The complete turn reveals only after all six heroes have responses, then resolves every played card in Initiative order without transferring ownership.
- Friendly/enemy queries, tied Initiative decisions, assists, reactions, hero-specific pending choices, and actor switching use authoritative team/player/hero relationships.
- Played/revealed cards and the active resolver remain readable without requiring presentation to own turn legality.
- A read-only primitive round-history matrix groups all three Red and all three Blue heroes and shows each hero's authoritative revealed card or empty-hand Pass for turns 1-4; current-turn Facedown commitments remain hidden until the joint reveal.
- Every displayed history entry resolves through its stable assignment and card definition, and played or Defense-discarded cards cannot reappear merely because presentation rebuilds.
- Undo, restart, snapshot restore, and the event log reproduce the same six-hero turn.

Non-goals: No hidden-information enforcement, table-talk enforcement, networking, AI, or six separate input devices.

### [!] 8.27 Complete the deterministic six-hero 3v3 pitch match

Observable outcome: Wasp and the five other distinct verified heroes can start from the official six-player Quick Game setup, play repeated turns and rounds, and end through every victory path with primitive visuals and director/debug UI.

Acceptance checks:

- Every implemented hero rule links to an authoritative source and focused tests; unresolved source or capability entries keep the gate incomplete.
- The complete six-hero roster exercises legal team interactions, combat, minions, Lane Push, progression, defeat/respawn, and victory without card-name/hero-name branches outside isolated approved handlers.
- The official six-player setup and only the remaining approved director/debug deviations are represented.
- Automated configuration and integration checks prove that staged one-hero-per-team, mirrored-definition, fixed-two-response, and controller-as-game-entity assumptions are absent: six distinct heroes map to six distinct controlling player/seats, exactly three heroes belong to each team, and director tooling remains outside match authority.
- Undo, restart, snapshots, event log, and Tie Breaker override remain deterministic across the complete 3v3 loop.
- A broader source-linked six-hero integration suite passes.
- `RISK-SCALE-001` is reassessed in full and then retired with its recorded 3v3 bound or its residuals explicitly accepted. This is the first milestone whose acceptance checks reach repeated rounds and progression with six heroes, so it is the first at which rulebook p.15's simultaneous Level Up can be observed as the serialized one-question-at-a-time step the continuation contract makes it, and the first at which the leveling stage's per-round derivation can be sampled against the standing thresholds rather than reasoned about.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: The networking architecture decision does not begin until the deterministic 3v3 match, URP migration, and independent readiness review at milestone 8.28a are reviewed and approved. Production visual work remains blocked through milestone 8.36a.

### [!] 8.28 Render-pipeline migration gate

Observable outcome: The complete primitive six-hero match is ported from Unity's Built-In Render Pipeline to URP, with its existing director/debug presentation and Win64 reference path intact, before production visual assets are introduced.

Planning rationale: Unity deprecated the Built-In Render Pipeline in Unity 6.5, guarantees its availability through at least Unity 6.7 LTS, and has not announced a final removal version. This project can retain Built-In rendering through the primitive-rule phases, but must not begin production presentation work on a pipeline Unity has directed new projects away from. [Unity's 2026 render-pipeline strategy](https://unity.com/topics/render-pipelines-strategy-for-2026).

Acceptance checks:

- URP is the active render pipeline for every configured quality level; the project no longer relies on a Built-In-only default pipeline setting.
- The existing primitive board, miniatures/tokens, card/HUD panels, highlighting, pointer selection, fixed camera, and event-driven presentation remain readable and functional after conversion.
- Materials, shaders, lighting, and post-processing used by the retained primitive presentation are URP-compatible; no Built-In-only visual dependency is carried into Phase 9.
- The authoritative rules assemblies and their deterministic state/event results are unchanged; the migration does not alter game rules, command handling, or timing.
- Focused Play Mode coverage for the affected presentation paths, a targeted visual inspection, a disposable internal Win64 development-build launch smoke, and both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No production board art, hero/minion production models, animation coverage, final lighting/art direction, new game rules, or player-facing build.

Hard gate: The private-online track uses this accepted URP baseline after milestone 8.28a independently reviews its six-hero and networking-readiness implications. Phase 9 production visual milestones do not begin until milestone 8.36a is also reviewed and approved.

### [!] 8.28a Independently review six-hero, URP, and networking readiness

Blocked by: Milestones 8.27 and 8.28 must pass the deterministic six-hero and URP migration gates.

Observable outcome: A fresh analysis-only review converts measured six-hero match, projection, replay/snapshot, and URP behavior into explicit technical constraints for the private-online architecture decision.

Acceptance checks:

- The review measures or verifies representative command/event volume, immutable projection and snapshot size, restore/replay cost, full-match allocation/resource behavior, and frame-time characteristics for the complete primitive 3v3 configuration.
- It inspects sources of nondeterminism, Unity-thread or scene coupling, host-authority suitability, stable identity/version boundaries, pending-choice transport needs, and current public/private information ownership.
- It distinguishes actual scaling or confidentiality blockers from concerns that the later networking milestones already own, and does not select a provider or invent a transport design prematurely.
- Findings follow the common independent-review evidence contract and supply concrete constraints and risks to milestone 8.29's source-backed architecture comparison.
- Any prerequisite correction is proposed as a separate milestone; no networking, optimization, rendering, rules, or projection implementation occurs here.

Non-goals: No provider/SDK selection, network prototype, public service, production rendering work, speculative scale target beyond the six invited players, or implementation fix.

Hard gate: Milestone 8.29 does not begin until milestone 8.28a and every networking-readiness blocker are reviewed and given an approved disposition.

### [x] 8.28b Establish a seeded, replayable randomness seam

Scheduled by the user-approved 2026-08-13 ER2 roadmap amendment; findings and evidence live in the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2). It touches only Core setup, so it may be approved and executed at any earlier point; it must complete before 8.29 begins the online track. The 8.2 re-baseline (2026-08-21, user approved, applying the 7.9a recommendation) resequences it directly after 8.2 in execution order - it is already unblocked, its closing determinism oracle exists since 7.9, and it is a schema/corpus change cheapest while the corpus is eight fixtures rather than after 8.25 - so it is the route's next proposal ahead of 8.3.

Observable outcome: The one Core randomness source - `TieBreakerSetupAuthority.CreateNormalSetupState`, which draws non-seeded `RandomNumberGenerator` output outside the command history - becomes a seeded seam whose draw is recorded in authoritative setup state, so deterministic replay and a future online authority reproduce normal setup exactly.

Acceptance checks:

- Normal setup's coin flip derives from a recorded seed or recorded outcome carried in immutable setup state and the snapshot, not from an unrecorded draw.
- Replay of a normal-setup match reproduces the identical Tie Breaker side from recorded state alone.
- The DEV-005 deterministic internal override is unchanged, and normal match flow still flips randomly from the player's perspective per the rulebook.
- Future randomness consumers have one seam to join; no second unrecorded randomness source is introduced.

Closing check for `F-ER2-04`: a determinism fixture replaying normal setup from recorded state and matching the flip result, observed failing at the pre-fix revision (the draw is unrecorded) and passing at the post-fix revision.

Non-goals: No networking, no new randomness consumers, no rulebook deviation, no change to the tie-breaker's gameplay meaning.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-828b-establish-a-seeded-replayable-randomness-seam-completion-evidence).

### [!] 8.29 Select the minimum private-online architecture and re-baseline the track

Observable outcome: A source-backed architecture decision defines the smallest supportable way for six invited Windows players on the internet to join one private 3v3 match, with one authoritative host/session and one independently owned hero per player.

Acceptance checks:

- Current Unity-supported transport, relay/NAT traversal, hosting, cost, licensing, privacy, and operational constraints are compared before a provider or SDK is selected.
- The trust boundary, host authority, seat/hero ownership, client-visible projections, version compatibility, join flow, disconnect policy, and failure model are explicit, including duplicate, replayed, stale, malformed, oversized, unauthorized, and out-of-order inputs plus disconnects during each pending-choice family.
- A lightweight threat model identifies trusted and untrusted processes, per-seat command authority, public/team/owner-private information, diagnostic-log sensitivity, host-tampering limitations, and the required safe rejection or terminal behavior. This is scoped to six invited players and does not claim production anti-cheat or public-service security.
- The selected Unity editor, packages/SDKs, transport/service versions, licenses, private-distribution constraints, deterministic/authoritative-code reach, removal/upgrade path, and build compatibility are pinned in a version-controlled dependency decision record. Upgrades that can affect authority, serialization, networking, rendering, or builds require an explicit compatibility assessment and applicable gate rerun.
- The track remains private and invite-only; accounts, public matchmaking, public server browsing, telemetry, monetization, and public distribution remain out of scope.
- Milestones 8.30-8.36, including the lettered local-UI and rehearsal slices, receive evidence-based implementation boundaries and validation commands before the first implementation milestone is authorized.

Planning note: This scope amendment adds a major cross-system track rather than one playtest task. The provisional planning range is thirteen implementation/validation micro-milestones and roughly 4-8 focused engineering weeks, excluding approval latency, external-service outages, and corrective work discovered by local or real-internet testing. The added milestone boundaries isolate session/projection, player-UI, transport, and internet defects; they do not by themselves expand the intended product scope. Milestone 8.29 must replace this estimate with evidence from the selected architecture.

### [!] 8.30 Move match authority behind a transport-neutral session boundary

Observable outcome: The existing local director adapter and a future network host can submit the same typed commands to, and consume the same ordered events and snapshots from, one authoritative match session without moving rules into Unity views or transport callbacks.

### [!] 8.30a Add locally testable owner-scoped projections and command authorization

Blocked by: Milestone 8.30 must establish the transport-neutral session boundary.

Observable outcome: Before any real transport is introduced, the local session can expose six distinct seat-scoped client projections and authorize each submitted command against its stable player/hero ownership.

Acceptance checks:

- A development-only local seat harness can select each of the six configured seats outside match authority and consume the same owner-scoped projection contract intended for network clients.
- Each seat receives its own hand and pre-reveal commitment state plus only the authorized public or team-shared state; another seat's hidden hand or commitment identity is absent rather than merely concealed by a view.
- Every player submission reaches the authoritative session as a gameplay-command payload plus trusted connection-or-local-seat context, a unique command identity, and the expected projection version supplied outside payload claims. The session derives the submitting `PlayerId`, owned hero, and team through that trusted seat binding and authoritative `MatchParticipants`; a `HeroId`, `TeamSide`, or other stable identity carried by `DirectorCommand` identifies the requested action or choice and is never accepted as caller identity.
- The authoritative session rejects cross-seat hero commands, unauthorized team choices, stale expected projection versions, replayed or duplicated command identities, mismatches between payload targets and ownership derived through `MatchParticipants`, and forged payload identities without partial mutation.
- Snapshot restore and session replacement rebuild every seat's authorized projection deterministically without transferring ownership or revealing another seat's private state.
- Director-only Undo, restart, hidden-state inspection, and Tie Breaker override remain outside the player projection and command surface.

Non-goals: No network SDK, transport envelope, host/join flow, relay/NAT traversal, reconnect policy, production security claim, or polished UI.

ER2 note (`F-ER2-07`): `MatchSessionProjection` is currently one global read model exposing every owner's cards and full state; this milestone is the recorded owner of the per-viewer seam, and every projection property added before it raises its cost.

### [!] 8.30b Seat-scope and complete the player HUD prompt flow

Re-scoped by the 8.2 re-baseline (2026-08-21, user approved, applying the 7.9a recommendation): milestone 7.8a already delivered the player-facing runtime UI Toolkit match HUD this block's original "build" wording restated. What remains is scoping that proven surface to one authorized seat and completing the states a multi-seat session adds.

Blocked by: Milestone 8.30a must prove the local owner-scoped projection and authorization contract.

Observable outcome: The proven 7.8a HUD, driven by one selected seat's authorized projection and typed invocations alone, can complete the full six-hero match through the local session.

Acceptance checks:

- The HUD shows the selected seat's own hand and commitments, authorized shared reveal/history, active resolver, round/turn, Lives, Waves, hero levels, coins, and connection-independent session status without querying rules objects or director-only state.
- Every action, target, Defense, tied-Initiative, team-owned, optional/mandatory continuation, and completed-match state required by the complete pitch roster has a readable contextual prompt and forwards the exact projected typed invocation.
- Waiting, rejected-input, stale-view, session-replacement, recoverable-error, and terminal states are explicit; presentation never guesses legality or silently leaves a control enabled after its offer expires.
- Rebuilding after snapshot restore or local session replacement produces the same authorized HUD and prompt state without retaining stale callbacks, selections, hidden data, or presentation-owned continuation state.
- The development-only seat switcher is visibly non-player-facing, cannot mutate seat ownership, and is unavailable from private-online client projections.
- The primitive layout keeps all required cards, prompts, and state reachable at the supported desktop validation resolutions without introducing the deferred production HUD, general settings suite, or accessibility suite.

Non-goals: No transport, lobby, reconnect implementation, production styling, animation overhaul, main menu, account UI, tutorial, or Phase 9 presentation work.

### [!] 8.30c Validate the complete six-seat player experience against the local session

Blocked by: Milestone 8.30b must provide the primitive local player HUD and prompt flow.

Observable outcome: A complete deterministic 3v3 match can be played locally while exercising all six seat perspectives, establishing that session, privacy, HUD, and prompt defects are isolated before networking begins.

Acceptance checks:

- A recorded local scenario completes a full match while switching among all six development seats and covers every roster-required player prompt family, team-owned choice, reveal boundary, and victory path.
- Automated projection and presentation checks cover each seat's private/public visibility, cross-seat rejection, stale controls, snapshot/session rebuild, completed-match state, and supported validation resolutions.
- Repeated seat switching and session replacement do not leak hidden information, transfer control, duplicate input handlers, retain stale selections, or grow presentation-owned resources monotonically.
- Findings are classified as authoritative-session, projection/authorization, HUD/prompt, or presentation-lifecycle defects and every blocking defect is corrected and rerun green before transport work is authorized.
- Validation records explicitly state that this in-process local gate proves no claim about serialization envelopes, network faults, relay/NAT traversal, latency, disconnects, or real-internet behavior.

Non-goals: No network process, provider integration, lobby, reconnect, public distribution, production visuals, or invited playtest.

### [!] 8.31 Establish the private host and six player seats

Blocked by: Milestone 8.30c must pass the complete local six-seat player-experience gate.

Observable outcome: One host creates an invite-only session and six compatible Windows clients can occupy exactly one stable player/hero seat each, with duplicates, unauthorized seat changes, and incompatible content/schema revisions rejected.

### [!] 8.32 Enforce owner-scoped projections and command authorization across the network boundary

Blocked by: Milestone 8.31 must establish the private host and six network seats.

Observable outcome: Each network client receives the already-proven seat-scoped projection and can submit commands only for its owned hero or an explicitly team-owned choice; untrusted transport input cannot bypass host-side revalidation.

Acceptance checks:

- The host constructs each submission's trusted caller context from the authenticated connection-to-seat binding established by milestone 8.31, attaches a unique command identity and expected projection version outside the untrusted gameplay-command payload, and derives player/hero/team ownership through authoritative `MatchParticipants`. Payload fields, including any `HeroId`, `TeamSide`, or forged identity/version claim, never establish or override the caller's identity, seat, command identity, or expected version.
- Automated negative cases cover cross-seat hero commands, unauthorized team choices, hidden-hand/pre-reveal leakage, replayed or duplicated command identities, stale expected projection versions, mismatches between payload targets and ownership derived through `MatchParticipants`, malformed/oversized envelopes, and forged payload identities or versions without partial host mutation.
- Diagnostic output needed for support does not expose another seat's owner-private hand or pre-reveal commitment identity.
- Network serialization and delivery preserve the local projection's absence of unauthorized fields rather than sending complete director state and relying on client-side hiding.

### [!] 8.33 Synchronize commands, events, pending choices, and snapshots

Blocked by: Milestone 8.32 must enforce owner-scoped projections and command authorization across the network boundary.

Observable outcome: All clients reproduce the host's public match state and their authorized private projection across full turns, nested Defense reactions, tied Initiative choices, and other pending resolutions without client-owned rules authority.

Acceptance checks:

- Repeatable fault-injection scenarios exercise permitted delay/jitter, duplication, loss, reordering, stale messages, corrupt/incompatible snapshots, and interrupted synchronization; clients either converge on the host's authorized version or enter one explicit recoverable/terminal failure state.
- Host and client diagnostics correlate session/content/schema version, command identity, authoritative projection version, and recovery decision without logging prohibited private information.

### [!] 8.34 Add disconnect, reconnect, resynchronization, and host-loss policy

Blocked by: Milestone 8.33 must prove authorized synchronization under fault injection.

Observable outcome: A temporarily disconnected player can return to the same compatible private session and rebuild the correct authorized projection; stale commands, unrecoverable host loss, and version mismatch fail explicitly without corrupting the match.

Acceptance checks:

- Fixed scenarios disconnect and reconnect during commitment, joint reveal, same-team Initiative choice, Movement continuation, Defense reaction, and end-of-round progression, then prove convergence or the documented terminal result.
- Reconnect to stale, future, incompatible, or unauthorized session state cannot transfer seat control, reveal private information, or partially replace authoritative state.

### [!] 8.35a Add the primitive private host/join lobby

Blocked by: Milestones 8.31-8.34 must establish seats, authorized synchronization, and reconnect/host-loss policy.

Observable outcome: Without production styling, invited players can host or join by the selected private invitation mechanism, identify their seat and team, observe compatibility and connection state, become ready, enter the match, leave, and begin an authorized reconnect.

Acceptance checks:

- The lobby exposes exactly the selected architecture's minimum host/join and invitation flow, six stable seats, team/hero identity, readiness, compatibility/version rejection, and transition-to-match state.
- Duplicate claims, unauthorized seat changes, full/incompatible sessions, host loss, join failure, and reconnect eligibility receive explicit bounded feedback without exposing another player's private match state or sensitive diagnostic data.
- Team identity and status never rely on color alone, and all required controls remain reachable at the supported desktop validation resolutions.
- Lobby state remains presentation-only over the session contracts and cannot assign authoritative match ownership by mutating Unity views.

Non-goals: No account system, friends list, public matchmaking, server browser, text/voice chat, spectator mode, ranking, moderation, general main menu, or production styling.

### [!] 8.35b Integrate the proven player HUD with online match flow

Blocked by: Milestone 8.35a must establish the primitive private lobby.

Observable outcome: The locally proven player HUD operates over the network client/session adapter with clear waiting, synchronization, latency, disconnect, reconnect, rejection, host-loss, and terminal feedback while preserving the same authoritative invocations and visibility boundaries.

Acceptance checks:

- Invited clients enter the existing owner-scoped HUD from the lobby, see only their own hand and authorized shared state, answer every legal prompt, and return to an explicit recoverable or terminal state when the session policy requires it.
- Network waiting, stale-view rejection, resynchronization, temporary disconnect, incompatible restore, reconnect success/failure, and host loss cannot leave stale actionable controls or presentation-owned continuation state.
- Equivalent local and network projections produce equivalent player HUD and prompt meanings; transport metadata does not become gameplay authority or fork a second rules-facing UI path.
- Reconnect and resynchronization rebuild the HUD without hidden-information leakage, seat-control transfer, duplicated input handlers, stale selections, or monotonic presentation-resource growth.

Non-goals: No production HUD redesign, public-service UX, matchmaking, account recovery, telemetry dashboard, tutorial, chat, or Phase 9 polish.

### [!] 8.35c Pass a local six-client playtest-build rehearsal

Blocked by: Milestone 8.35b must complete primitive online match-flow UI integration.

Observable outcome: The exact intended private-playtest topology and disposable Win64 build path complete a six-client match locally, including bounded fault and reconnect scenarios, before friends are asked to test over the internet.

Acceptance checks:

- Six local client processes occupy six distinct seats through the primitive lobby and complete a full match using the same build, content/schema revision, host/session boundary, owner-scoped projections, and UI intended for milestone 8.36.
- The rehearsal exercises the approved local fault-injection, disconnect/reconnect, stale/version rejection, host-loss, and resynchronization cases and records host/client convergence plus privacy-reviewed diagnostics.
- No client receives another seat's hidden hand, controls another hero, retains stale actionable prompts after resynchronization, or diverges from the host's authoritative public outcome.
- Every observed blocking rules/session, projection/privacy, HUD/prompt, transport/synchronization, build, or diagnostic defect is corrected and rerun green before the internet playtest is authorized.
- The evidence explicitly states that a same-machine or local-network rehearsal does not prove relay/NAT traversal, external latency behavior, multi-network connectivity, or real-internet reliability.

Non-goals: No invited external tester, public distribution, production service, account system, matchmaking, production presentation, or substitution for milestone 8.36.

### [!] 8.36 Validate a private six-human internet 3v3 and run the friends playtest

Blocked by: Milestone 8.35c must pass the local six-client playtest-build rehearsal.

Observable outcome: Six humans on separate clients complete an invite-only full 3v3 match over the internet using a disposable private Win64 playtest build and primitive UI before production presentation begins.

Acceptance checks:

- The exact build and content/schema revision are recorded and privately distributed only to the invited testers.
- The session proves host/join, six stable seats, private hands, all normal player and team-owned choices, complete turns/rounds, reconnect/resync, match completion, and explicit failure handling across at least two external networks.
- Host and clients converge on the same authoritative public outcome; owner-private projections and command authorization show no cross-player leakage or control transfer.
- The playtest includes the approved bounded latency/disconnect/reconnect cases and preserves a privacy-reviewed diagnostic bundle sufficient to correlate failures without exposing another player's hidden hand.
- Crashes, desynchronization, latency symptoms, confusing flows, unreadable state, and tester feedback are recorded and triaged without changing rules or code during the milestone.
- Any gate-blocking defect becomes a separately approved corrective milestone; the reviewed findings re-baseline Phase 9 HUD, prompt, feedback, and readability work.
- T3 online hard gate: focused network/session tests, a multi-client integration run, the private internet playtest, and both complete unfiltered Edit Mode and Play Mode suites pass at the applicable final revision.

Non-goals: No public release, dedicated production service, accounts, matchmaking, anti-cheat certification, competitive operations, or production presentation.

### [!] 8.36a Independently review the private-online implementation and playtest evidence

Blocked by: Milestone 8.36 must complete the invited six-human internet gate and record its unmodified playtest evidence.

Observable outcome: A fresh analysis-only review correlates observed playtest failures and friction with the networking, authorization, synchronization, reconnect, diagnostics, and primitive presentation implementation before production polish begins.

Acceptance checks:

- The review examines actual session logs, test/build identity, tester observations, failure handling, host/client convergence evidence, reconnect/resync behavior, latency-sensitive choices, and relevant source paths.
- It checks private-information leakage, command authorization, stale/version-mismatched input, host-loss policy, diagnostic sufficiency, runtime stability, and whether symptoms indicate correctness, architecture, performance, operational, or presentation causes.
- Findings follow the common independent-review evidence contract; uncertain causes remain explicitly uncertain rather than being converted into unsupported fixes.
- Gate-blocking defects and later presentation/UX findings are separated into appropriately scoped proposed corrective or Phase 9 milestones.
- No code, rules, networking configuration, build, or presentation implementation changes during the review.

Non-goals: No second playtest, production hosting/service design, accounts, matchmaking, telemetry, public distribution, or bundled fix implementation.

Hard gate: Phase 9 does not begin until milestone 8.36a and every gate-blocking corrective milestone are reviewed and approved.
