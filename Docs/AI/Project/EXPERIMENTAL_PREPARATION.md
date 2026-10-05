# Experimental features implementation preparation

Preparation outcome [9.59](../Roadmap/PHASE_9.md#x-959-prepare-the-experimental-features-for-implementation). Return to [project context](../PROJECT_CONTEXT.md). [EXPERIMENTAL_FEATURES.md](EXPERIMENTAL_FEATURES.md) owns the accepted product decisions; this packet owns the source map, proposed technical approach and unexecuted checks. It does not change rules or authorize implementation. The [session brief](../SESSION_BRIEF.md) alone owns active work.

## Baseline and execution order

Inspected committed baseline: `65f86183` (the 9.57 documentation commit, with committed hero behavior through 9.51). The shared checkout also contains unfinished 9.52 code, including snapshot v59 and the `BoardPartitionAxis` choice vocabulary. Those observations are provisional, not a completed hero gate. No runtime file or test was edited for this packet.

Approved 2026-10-05 order is **9.58 teammate coordination with the shared Experimental menu, then 9.56 timer/header**, after NebKher completes 9.54, followed by 9.61 small-map/host-selector implementation. These remain two outcomes with separate validation and commits. The timer is not a dependency of coordination. Build settings once and extend them for the timer; do not create a second host-options path. No additional milestone per mechanical step is proposed.

Immediately after the hero lands: verify the brief and final hero evidence; compare the source locations below with this baseline; audit new pending families, effect vocabularies, controllers and restore fields; select schema/wire versions from the landed source, not this packet. Do not transplant an old snapshot reader or reserve version numbers now.

## Source map

All paths below are live source references, not proposed new files. Symbols identify bounded read locations if line numbers move.

| Concern | Existing owner and integration point |
| --- | --- |
| Host menu | [PrivateLobbyViews.cs](../../../Assets/GuardsDigital/Presentation/Interaction/PrivateLobbyViews.cs): `PrivateLobbyUnityView`, `HostMenuName`, host-menu construction; [PrivateLobbySurface.cs](../../../Assets/GuardsDigital/Presentation/Interaction/PrivateLobbySurface.cs): `PrivateLobbyPresenter.Press` and host/setup requests. Add the Experimental control here. |
| Match composition | [DeferredMatchComposition.cs](../../../Assets/GuardsDigital/Application/PrivateOnline/DeferredMatchComposition.cs): `IDeferredMatchComposition.ComposeForLineup`; [MatchSceneCompositionRoot.cs](../../../Assets/GuardsDigital/Presentation/Interaction/MatchSceneCompositionRoot.cs): existing setup choices and composition. Carry one immutable match-options value across this boundary; preserve seat identities. |
| Trusted submissions | [MatchHost.cs](../../../Assets/GuardsDigital/Application/MatchHost.cs): `ProjectionForSeat`, `SubmitForSeat`, `SubmitAgreementForSeat`, seat Undo watermark and `ContinueEndOfRound`. This is the serialized host boundary, not a presenter callback. |
| Private cards/decision owners | [SeatProjection.cs](../../../Assets/GuardsDigital/Application/SeatProjection.cs): `SeatProjectionFactory.ForSeat`, `OwnerOfStandingDecision`, `DecidingActor`, `AuthorizedCommands`. Current `ownCards` is owner-only; `publicCards` contains only already-public foreign cards. Delegation is already represented. |
| Wire/read model | [NetworkSeatProjection.cs](../../../Assets/GuardsDigital/Application/PrivateOnline/NetworkSeatProjection.cs), [NetworkProjectionDetailCodec.cs](../../../Assets/GuardsDigital/Application/PrivateOnline/NetworkProjectionDetailCodec.cs), [NetworkSubmissionMessages.cs](../../../Assets/GuardsDigital/Application/PrivateOnline/NetworkSubmissionMessages.cs): `NetworkMessageCodec`; [NetworkMatchClient.cs](../../../Assets/GuardsDigital/Application/PrivateOnline/NetworkMatchClient.cs): `NetworkMatchReadModel` and selection auto-confirm. Extend bounded typed data, not CLR object serialization. |
| Host lifecycle | [PrivateSessionHost.cs](../../../Assets/GuardsDigital/Application/PrivateOnline/PrivateSessionHost.cs): frame dispatch, `Sweep`, `IsPaused`, republishing; [PrivateOnlineDrivers.cs](../../../Assets/GuardsDigital/Presentation/Interaction/PrivateOnlineDrivers.cs) and `PrivateLobbyViews.cs` drive the host. Cover both dedicated and playing-host loops. |
| Hand inspection/input | [DirectorHandsPresenter.cs](../../../Assets/GuardsDigital/Presentation/Match/DirectorHandsPresenter.cs): `SelectViewedHero` currently refuses foreign heroes for player seats; [DirectorHandsUnityView.cs](../../../Assets/GuardsDigital/Presentation/Match/DirectorHandsUnityView.cs): card pointer callbacks. Add read-only allied inspection without widening action ownership. |
| Board input/overlays | [PrimitiveHeroPointerInput.cs](../../../Assets/GuardsDigital/Presentation/Interaction/PrimitiveHeroPointerInput.cs): `TryHandlePointerRay`, `FindTileUnderPointer`, UI blocker; [UnityMatchFeatureViews.cs](../../../Assets/GuardsDigital/Presentation/Match/UnityMatchFeatureViews.cs): passive `BoardInteractionUnityView`; [HexTileView.cs](../../../Assets/GuardsDigital/Presentation/Board/HexTileView.cs): independent overlay patterns. |
| Exact token reuse | [HeroOverviewsUnityView.cs](../../../Assets/GuardsDigital/Presentation/Match/HeroOverviewsUnityView.cs): `BuildHeroPortrait`; [HeroTokenArtwork.cs](../../../Assets/GuardsDigital/Presentation/Units/Heroes/HeroTokenArtwork.cs): `ResolveToken`; [RuntimeTeamIdentity.cs](../../../Assets/GuardsDigital/Application/RuntimeTeamIdentity.cs). [MinionBattlePickBadges.cs](../../../Assets/GuardsDigital/Presentation/Board/MinionBattlePickBadges.cs) supplies badge-spacing precedent, not permission to replace the tray rim/background with its black badge outline. |
| Decision taxonomy | [MatchSessionProjection.cs](../../../Assets/GuardsDigital/Application/MatchSessionProjection.cs): `PendingDecisionFamilies`, `PendingDecisionContract.Create`; [EffectChoice.cs](../../../Assets/GuardsDigital/Core/Rules/EffectResolution/EffectChoice.cs): choice kind, candidate/value vocabulary and Select/Deselect/Confirm/Decline/Stop. |
| Existing votes | [TeamAgreement.cs](../../../Assets/GuardsDigital/Application/TeamAgreement.cs) supports Initiative and MinionBattle only. Lane Push agreement is new work; coordination markers are never votes. |
| Undo/atomicity | [MatchSession.cs](../../../Assets/GuardsDigital/Application/MatchSession.cs): `Execute`, `Undo`, `Reconstruct`, retained checkpoints; [MatchAggregate.cs](../../../Assets/GuardsDigital/Application/MatchAggregate.cs): `MatchPreImage`; [AuthoritativeSnapshotSerializer.cs](../../../Assets/GuardsDigital/Core/Snapshots/AuthoritativeSnapshotSerializer.cs); [DirectorCommand.cs](../../../Assets/GuardsDigital/Core/Replay/DirectorCommand.cs). |
| Playback and header | [KineticConductor.cs](../../../Assets/GuardsDigital/Presentation/Match/KineticConductor.cs), [ConfiguredMatchSceneRuntime.cs](../../../Assets/GuardsDigital/Presentation/Match/ConfiguredMatchSceneRuntime.cs); [MatchStatusBarUnityView.cs](../../../Assets/GuardsDigital/Presentation/Match/MatchStatusBarUnityView.cs); [ConfiguredMatchHudRenderer.cs](../../../Assets/GuardsDigital/Presentation/Match/ConfiguredMatchHudRenderer.cs): 56-unit identity row plus 40-unit narration row = **96 units**, with Undo/End Turn geometry tied to it. |

## Proposed shared options contract

Use a Unity-free immutable options value: coordination enabled, timer enabled, and the five named duration values. Fresh host creation constructs disabled options; reconnect resumes the existing match's options. Freeze the effective value at composition and include it in authoritative restore/admission consistency. Do not use static last-used settings or player preferences that leak an experiment into the next host game. Joined seats read the effective settings but cannot change them.

Keep the two flags independent. If 9.58 ships first, keep timer controls unavailable until the timer works; do not ship an enabled checkbox that does nothing. Duration units, bounds and defaults require the disposition below. Validate finite values/overflow before composition and provide field-level feedback.

Protocol changes must update both encode/decode bounds, version rejection and the identity tuple consistently. Do not assume a new field is safe because both local clients happen to have the same build. Assess the composed options' snapshot ownership once; never maintain divergent setup and runtime copies.

## Proposed coordination contract

**Separate observation from authority.** Add an explicitly read-only allied-hand collection alongside existing own/public cards. Filter by bound seat, current team and enabled option before creating the seat projection or wire payload. Do not append allied cards to `OwnCards`, globally redefine `IsPublic`, or widen `nameableCardIds`/`AuthorizedCommands`: seeing a card is not permission to use it in a pending answer. Enemy cards, deck order and ungranted facedown information remain masked.

**Separate communication from rules commands.** Model host-owned, team-filtered marker state keyed by indicating seat/hero, with one hex target and one card target. A hex carries map identity plus coordinate; a card carries owner hero plus card-instance identity. Derive sender identity from the admitted connection, not a client-supplied hero. Use request IDs and a communication revision separate from gameplay `Version`; marker updates must not stale an unrelated action or enter gameplay Undo/replay history. Carry typed bounded messages through both local and network clients; do not reuse `TeamAgreementCommand` or `DirectorCommand` for a ping.

**Do not send ambiguous toggles over a retrying transport.** UI translates right-click to explicit set/clear against the observed marker revision. Duplicate set/clear is idempotent; old updates cannot erase a newer marker. Same target clears locally by intent, a new target replaces the same slot. Revalidate card ownership/location and map membership on receipt. Keep valid markers across ordinary phases; clear stale card-instance targets, departed senders and match changes. After Director restore, revalidate communication state against the restored cards; markers do not roll the rules state forward. Reconnect receives a current team-filtered snapshot, not a replay of old clicks.

**Input path.** Add an independent secondary-button intent to the passive board view. Hit-test the actual hex beneath hero/minion pieces rather than taking the left-click piece-selection path, and honor the existing UI pointer blocker so a card right-click cannot also mark a hex underneath. Card callbacks consume only the suggestion gesture; do not trigger left-click selection, drag, commit or auto-confirm. Read-only cards must still receive right-click even when gameplay controls are disabled. The owner remains the only gameplay controller.

**Presentation path.** Reuse the tray's token resolver, crop, rim and asset fallback; extract only the smallest shared portrait builder needed by tray and suggestion if necessary. Do not generate art or package private resources. Keep the board badge smaller. Put the white communication outline in a separate overlay channel outside rules highlights; never call a tile-wide clear/set operation that erases another owner's highlight. Render same-target badges in stable player order, spaced like Minion Battle. Use one shared card-face builder for allied/own cards and an explicit read-only mode, including My Cards navigation.

## Proposed timer contract

**Ownership and state.** Put a pure timing aggregate beside the match session, included in its atomic pre-image, snapshot and restore/Undo boundaries. It contains immutable settings, per-team remaining bank, wave-reset identity, active/paused decision frames, timeout random state and a timing revision. Inject monotonic elapsed time at the application boundary. Do not read Unity time or platform entropy in Core; do not persist absolute process timestamps. Rebase remaining durations on restore.

Each frame needs a stable scope key (match generation, resolution/transition item, actual deciding seat/team and nested choice identity), allowance kind, remaining duration, running/bank/suspended state and parent scope. Changing a candidate selection or opening the next choice in the same resolution does not mint a new allowance. `PendingDecisionContract` represents at most one family, but a family can contain multiple outstanding items; commitments and action selection also sit outside it. Enumerate all eligible owners, not only `DecisionOwner.HeroId`'s first hero. Use the existing delegation query for another hero controlling the action.

**Event-driven accounting.** Charge elapsed intervals only to actionable frames. Split elapsed time at allowance exhaustion, answer/pause/resume and wave boundaries; compute each team's bank charge from the union of intervals in which that team has at least one active bank user. A simple count of bank users multiplied by elapsed time is wrong. Publish timing revision/samples separately from gameplay version, interpolate display only, and serialize timeout submissions with player commands in the host executor. Cosmetic countdown ticks must not grow the command log or invalidate a pending command.

**Races.** Key a timeout attempt by scope generation and exhaustion state, then rederive current commands immediately before dispatch. First accepted answer closes that scope; stale timeout/manual duplicates cannot affect its successor. A timer policy may submit only current typed invocations, with an explicit internal cause that cannot be requested by an untrusted peer. Reuse the existing host continuation after an accepted answer. Never fake several human votes to get through a unanimous agreement gate.

**Undo/replay.** Existing Director Undo reconstructs a command prefix; replaying only commands cannot reconstruct historical elapsed time. Record enough timing checkpoint/delta information at accepted commands and explicit timing transitions for reconstruction and snapshot equivalence. Director Undo restores both clocks and game state; ordinary player/timeout Undo preserves spent time and carries the surviving resolution budget through the legal rewind. Do not implement these as one indistinguishable refunding Undo. Replay consumes recorded timing outcomes without waiting for wall time or redrawing random answers.

**Randomness.** Extend the existing [SetupRandomnessAuthority](../../../Assets/GuardsDigital/Core/Setup/SetupRandomness.cs) seam with a named timeout purpose and persisted draw position or equivalent deterministic recorded choice. Existing setup randomness declares only the coin purpose. No `UnityEngine.Random`, fresh `System.Random` or bot-policy random stream for timeout decisions. Stable candidate ordering and one recorded answer must survive reconnect, retries and Director restoration.

### Timeout dispatch map

Use the [accepted coverage matrix](EXPERIMENTAL_FEATURES.md#choice-coverage-matrix) as policy; this is its implementation routing, not a different ranking rule.

| Surface | Concrete route / trap to close |
| --- | --- |
| Commitment outside pending contract | Current `CommitCard`, `CommitTwoCards` or legal `Pass` offers; choose lowest initiative without turning an optional extra into a required card. |
| Action selection / acting body | Legal projected Hold/End Turn, ordinary cancellation or bounded seat Undo first. `ChooseActingBody` currently exposes body answers: prove a legal route to end turn instead of assuming a generic EndTurn command exists. |
| Movement / attack target | Use exact Stop/cancellation where legal; locked attacks need a legal target answer. Preserve already-performed movement and attack commitments. |
| Defense / removal prevention / respawn | Use projected decline invocations, including `ResolveCardWithoutRespawn`; do not substitute numeric zero by convention. |
| Effect / borrowed action | `AnswerEffectChoice`: decline optional; otherwise construct a complete legal selection and Confirm/Stop as offered. Do not pick uniformly among Select/Deselect/Confirm buttons. |
| Displaced-minion correction | Select the required subject, then its eligible destination under the agreed ranking; rederive after each step. |
| Minion Battle / shared board agreement | Read current per-seat preferences, plurality, random tied leaders; finalize with legal authority commands and required removal order. Lane Push needs an actual agreement surface before there are votes to count. |
| Initiative / reveal retrieval | Initiative stays universal random legal under the accepted design; post-reveal retrieval uses lowest eligible initiative. Do not silently import plurality into initiative. |
| System transition | Classify typed transition definition/item: card upgrade gets its separate allowance and lowest offered initiative; Lane Push gets response time and shared voting. Multiple upgrade items can remain outstanding. |
| Turn release / automatic stages | `ReleaseTurn` and host continuation; automatic stages receive no thinking allowance. |

A completion planner must account for accepted picks, required count, exclusions and permutation/order constraints. Reuse candidate legality/query primitives; bot policies are strategy, not the timeout specification. For example, Snorri's 16 rune/slot values must become a valid remaining permutation, not independent random selections. Preserve typed distinctions among cards, minions, spaces and generic options; closest is measured from the acting unit only when that ranking applies. An occupied hex click is still a hex marker in coordination and has no bearing on timeout ranking.

No-stall proof walks real commands to the next owner/stage/game end. After each accepted command, compare decision/resolution identity and progress, not only version. A retry cap, refused command, empty candidate set without legal Stop, or loop detector firing is a failed test requiring correction; it is not a successful fallback or authority to bypass a rule.

## Items requiring a disposition before timer implementation closes

1. **Animation readiness:** `KineticConductor` is deliberately downstream and authority-free; its scene/acknowledgement holds are not a host clock gate. Implement a bounded, version-keyed readiness protocol at the application/adapter boundary, with a host-defined presentation window and completion for the actual deciding seat(s). An unbounded client “busy” flag permits a frozen match; host-only animation duration can charge a remote player before their controls appear. Prototype both slow-joining playback and missing/late acknowledgements, keep Core independent, and resolve the finite hold/failure behavior before calling this seam complete. This is technical work within the timer gate, not an excuse to count through animations.
2. **Transport pause:** `PrivateSessionHost.IsPaused` currently freezes play while a seat is reserved after disconnection. Preserve that existing pause and freeze clocks consistently; resume remaining time, not a new allowance. Do not silently add disconnect forfeiture or immunity beyond the current session policy.
3. **Display of mixed states:** the approved normal-to-bank mockups do not fully specify one team using bank while the other still has normal allowance, or several unequal response deadlines. Prepare these real-state previews during implementation; show every outstanding time truthfully within 96 units, with no merged/restarted budgets. The final arrangement needs visual review if it extends the approved mockup.
4. **Numeric defaults/ranges:** the user selected configurable durations, not actual values. Use explicit validated configuration until defaults are selected. The fixture numbers below are test inputs only. Do not publish mockup digits as product defaults.
5. **NebKher final delta:** re-audit all fronts/Ultimate, Illusion substitution and absent actors, borrowed primary/secondary/Attack/Defense, laughter, and divided-board filtering. The observed new `BoardPartitionAxis` vocabulary is a generic mandatory option (random legal when legal end-turn/decline does not apply). Never rank its integer encoding as a distance.

## Ready-to-transcribe acceptance cases

Case IDs below are preparation identifiers, not existing tests or claimed passes. Timer examples use commitment 20s, resolution 30s, response 10s, upgrade 15s and bank 60s solely as fixtures.

| ID | Arrange / act | Required result |
| --- | --- | --- |
| O1 | All four toggle combinations; leave and create a new host | Features independent; new host disabled; reconnect retains effective settings. |
| C1 | Red A inspects Red B through token and tray; Blue requests B | Both red routes show B's current hand read-only; Blue receives no private bytes. |
| C2 | Suggest while own turn, opponent turn, commitment, upgrade and animation play | One suggestion per gesture; no gameplay command, vote, cancellation or timer mutation. |
| C3 | Right-click card over board; repeat/delay network delivery | Only card target changes; explicit set/clear remains idempotent and newest revision wins. |
| C4 | Existing red/blue/white effect ring, two teammates mark same hex | Underlying cue unchanged; separate white outer outline and attributable tray-token badges. |
| C5 | Suggested card leaves hand, different card takes same visual slot | Stale marker removed; replacement never inherits it. Include swaps, discard/retrieve and restore. |
| C6 | Declared/mirrored/multiple-body hero identities; resources absent | Exact tray resolver/crop/rim or its existing fallback; no display-name branches or invented artwork. |
| C7 | Disconnect/reconnect and Director restore with markers present | Valid current team view, no ghosts/private leak; gameplay replay/outcome unaffected. |
| T1 | Two red decisions have 3s and 5s remaining; advance 7s | Red bank ends at 56s: union of bank-use intervals is 4s, not 6s. Blue stays 60s. |
| T2 | Red resolution runs 12s, then Blue response runs 6s | Red resolution frozen at 18s; Blue response at 4s; banks unchanged. |
| T3 | Same red pause, Blue response runs 14s then answers | Blue bank 56s; red resumes at 18s; red bank 60s. Repeat for a same-team and nested response. |
| T4 | Resolution has 1s left; 5s animation pause, then 2s actionable | Pause costs zero; allowance exhausted; bank 59s. Late/missing readiness cannot freeze forever. |
| T5 | Three red bank users and one blue bank user, advance 2s | Each active team loses exactly 2s, irrespective of teammate count. |
| T6 | Bank zero, new 20s commitment begins | Full normal allowance exists; next wave resets each bank exactly once to 60s. |
| T7 | Manual answer races timeout; replay either arrival order | One accepted outcome; no successor answered by stale timeout; clients converge. |
| T8 | Director checkpoint has resolution 18s, bank 44s, suspended response 4s | Restore exact checkpoint state, including owners/frames; long wall-time gap adds no charge. Player Undo separately refunds nothing. |
| T9 | Partially confirmed rune assignment, multi-card choice, constrained removals | Preserve accepted valid picks; legal complete answer; no Deselect loop or duplicate slot. |
| T10 | Same seed/state and tied lowest cards or zero-vote destinations | Replay/restore reproduce one legal recorded result; retry does not draw again. |
| T11 | Expired resolution chains into another mandatory prompt or turn release | Immediate legal completion/progression; no fresh resolution allowance or wait for another display tick. |
| T12 | All pending families, non-pending offers and final NebKher choices expire | Reach next legitimate owner/stage/end within bounded work; no refusal, exception or loop-cap “pass.” |
| H1 | Gold simultaneous, single-team, bank promotion, mixed deadlines, longest narration | Icon-only clocks, largest fitting status/hearts, correct owners, 96-unit bar and unchanged board bounds. |

## Regression and handoff recipe

Existing reusable fixtures (verified by source; not run during preparation):

- Options/coordination: `PrivateLobbyEntryPagesTests`, `PrivateLobbyFrontDoorTests`, `DeferredMatchCompositionTests`, `OwnerScopedProjectionTests`, `NetworkAuthorizationBoundaryTests`, `NetworkSubmissionCodecTests`, `NetworkMatchClientReviewTests`, `SeatScopedMatchHudTests`, `MinionBattlePickBoardTests`, `HeroRailPortraitTests`, `HeroTokenBindingTests`.
- Timer authority: `ConfiguredPendingChoiceProjectionTests`, `PendingDecisionContractConformanceTests`, `TeamAgreementTests`, `EndOfRoundContinuationTests`, `SeatBoundedUndoTests`, `UndoCheckpointReconstructionTests`, `DirectorCommandReplayTests`, `AuthoritativeSnapshotSerializerTests`, `SnapshotCompatibilityCorpusTests`, `SetupRandomnessConformanceTests`, `PendingDecisionReconnectTests`.
- Presentation/network: `KineticConductorTests`, `MatchHudPanelScalingTests`, `MatchHudAspectRatioFitTests`, `PrivateLobbyPlayModeTests`, `OnlineMatchHudPlayModeTests`, `SeatScopedMatchHudPlayModeTests`, `HeroTrayRefinementTests`. The token fixture already asserts white interior fields, preserved portrait/team rings and shared addressing.

Add focused behavioral tests for the case IDs at their owning boundary, not tests mirroring proposed class structure. A starter pure-boundary filter is:

```powershell
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --filter "FullyQualifiedName~OwnerScopedProjectionTests|FullyQualifiedName~NetworkAuthorizationBoundaryTests|FullyQualifiedName~TeamAgreementTests|FullyQualifiedName~SeatBoundedUndoTests|FullyQualifiedName~UndoCheckpointReconstructionTests"
```

This is an initial regression selection, not the complete gate. Read [testing policy](../Testing/POLICY.md) and [harness rules](../Testing/DOTNET_HARNESS.md) before execution. Unity runs only through the parameterless `Tools/Invoke-UnityValidation.ps1` request route, serially. No tests or Unity launch are authorized by this preparation packet.

Plan T3 for timer's snapshot/replay/cross-system gate; coordination needs at least T2 and the highest applicable tier if its wire/shared serialization changes trigger T3. Run focused cases first, affected regressions next, then required final suites once at the final candidate. Check real host/join views with the final roster and supported desktop viewports; missing private artwork in a clean checkout is not visual acceptance. Do not upload or commit extracted art.

Ready handoff: read the brief, this packet and accepted design; confirm hero 9.54 evidence and implementation authorization; reconcile the final source delta and the five dispositions; execute 9.58 then 9.56 only within the approved scope, with separate validation/commits. Preserve future work as queued if authorization has not been given. Preparation saves discovery and test-design work; it does not replace integration evidence.
