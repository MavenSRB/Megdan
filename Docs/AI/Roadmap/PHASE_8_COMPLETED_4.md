# Phase 8 - Completed milestone definitions, fourth shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-05 during this required warning-threshold ownership review, prompted by the active shard sitting at 94% of its own hard limit with roughly six more milestone blocks due the same afternoon, while neither existing completed shard had room: `PHASE_8_COMPLETED.md` and `PHASE_8_COMPLETED_2.md` both sat within a few hundred words of their own 85% warning line and `PHASE_8_COMPLETED_3.md`, though it had headroom, was left alone so this checkpoint's provenance stays in one place. Milestones 8.35j through 8.35o moved here verbatim, the oldest definitions the active shard still held in full: the awaited-answer shine family across the top bar, hero trays and cards (8.35j, 8.35m, 8.35m1), the relaunched-player rejoin road to a reserved seat (8.35k), the barred-action refusal that names what barred it (8.35l), the acting-hero narration and board marker (8.35n), and the dedicated host console's typed release command (8.35o). Milestones **8.35p** through **8.35t** followed them here on 2026-09-05 during milestone 8.35z5's own required ownership review - the charge that moves before its attack resolves (8.35p), the spawned test drivers that fill empty seats for solo playtesting (8.35q), the seat-public match log with who the table is waiting on (8.35r), the greyed control that names why in a cursor tooltip (8.35s), and the test driver that dials the address the host actually bound (8.35t) - the same contiguous run carried further in one review, because the tail-batch integration landed five milestone blocks in a single pass and the active shard had roughly 470 words of headroom against its own warning line before it. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.

### [x] 8.35j Light the whole top bar whenever the match is waiting on the viewing player

User-approved amendment of 2026-09-03, from live playtesting: "on players turn, the whole top bar needs to be gold, not just naration bar", refined the same day so the trigger is every moment the match waits on the viewer rather than their action turn alone. The reading it replaces was a fifteen-way disjunction over the controls the round view model happened to expose. That is a fair approximation with one real hole: rulebook p.7's commitment is not one of the eleven declared pending-decision families and exposes no round-action control, because the hand owns it - so the most common wait in the game, every player choosing a card at the top of a round, reached the player as the calm voice. The user's own list names an awaited commitment first.

Observable outcome: The whole top strip - identity row and narration lane together - carries the gold treatment exactly while the match is waiting on the viewing player: their commitment, a decision they own, their turn, their level-up upgrade, a defense answer, a respawn or agreement, and the turn release. Every other state keeps the look it has.

Acceptance checks:

- One derivation answers it, and both surfaces read that one flag: the strip's paint and the narration lane's warm voice cannot disagree about whose move it is, because neither computes the question for itself.
- An awaited commitment from the viewer lights the bar, asserted together with the two facts the previous reading had - no standing decision and no offered turn action - so the case names the hole it closes rather than merely passing.
- A decision standing on another seat leaves this viewer's bar cold. The turn arm reads the projection's own scoped offer set rather than the selected actor, because an attacker is still the selected actor while the defender is the one being asked, and the offer set is withheld for exactly that reason.
- An ended match is not a wait, on the reading milestone 8.30c gave the status line.
- Every declared pending-decision family reaches the bar without the derivation naming one: the standing contract is family-complete by construction, proven against the declared inventory, so a twelfth family lights the bar the day it is declared.
- Presentation-only, viewer scope only. No rule, authority, projection, wire, schema or command change, and no hero-scope or card-scope treatment, which the next milestone reads from this same derivation.

Closing check: `MatchFeaturePresenterTests.AnAwaitedCommitmentFromTheViewer_LightsTheBarTheOldReadingLeftCold` and `TheWholeTopStrip_CarriesTheGoldWhileTheMatchWaitsOnTheViewer` are recorded failing at the pre-fix revision - the commitment arm answering false and the strip painting its ordinary background - and passing after, while the family-conformance case passes on both sides, which is what says the red is this milestone's claim and not a broken suite.

Non-goals: No hero-scope tray treatment and no card-scope highlight or resolution dimming; no narration wording or typography change; no new waiting state invented beyond what the projection already publishes; no rules, authority, projection, wire or schema change.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835j-light-the-whole-top-bar-whenever-the-match-is-waiting-on-the-viewing-player-completion-evidence).

### [x] 8.35k Give a relaunched player a road back to their own reserved seat

User-approved corrective of 2026-09-03, reported from the live playtest verbatim: "rejoin does not work, if user disconnects, its seat stays away, and connection is refused." Milestone 8.34 built the whole return mechanism - a reserved seat, a host-minted capability, a reclaim message, a rotated epoch - and it works. What it never had was a road to it. The capability lived in one private field of one live `PrivateSessionClient`, and three ordinary player actions end that object: the process exiting, the process crashing, and pressing LEAVE. After any of them the lobby could offer only a FIRST join, which the host refuses by name because the seat is reserved. Every reconnect fixture at every tier - the six-client rehearsal included - replaced only a socket beneath a surviving client, which is the one path a real player never walks.

Observable outcome: A player whose game died relaunches it, enters the same invitation, and is returned to their own seat with a rotated epoch and a complete projection. A player who deliberately leaves gives that seat up instead. And the host can hand a reserved seat back by a deliberate act, for the evening where a player's machine is gone and no stored capability can exist.

Acceptance checks:

- A brand-new client object for a seat the host holds reserved is returned to it by reclaim, with the epoch rotated and the authoritative version unmoved while the seat was away. The fixture constructs a second, distinct client and copies nothing out of the first, because reusing the object is what every existing reconnect fixture does and is exactly the path that was never broken.
- Leaving forfeits and crashing does not, asserted as one case in both directions: a deliberate Goodbye returns the seat to Open where an ordinary keyed claim takes it, while a dropped link leaves it Reserved and refuses the same claim by name. A build that forfeited every departure and a build that reserved every departure each pass one half alone.
- The retained capability answers for one session correlation and one seat only, so it can be replayed into neither another session nor another seat.
- The host's release control hands back a reserved seat and refuses a claimed one: a live player is never displaced by it, it is never automatic, and it is never on a timer - the reservation's absence of any clock is a feature, and an auto-release would give a dropped player's seat to whoever reconnected first.
- The session key stays an invitation and never an authorization: a client with no retained capability is refused a reserved seat although it holds the key every invited player holds. `HandleReclaim`'s checks, its identical answer to both wrong-capability cases, and the epoch rotation are unchanged.
- The capability is owner-private and stays out of every roster, projection, log line and diagnostic marker; the rehearsal runner greps every returned log for the shape a leak would take, beside the session key it already refused.
- The host log tells the two departures apart, and a refused client records `LOBBY_REFUSED reason=` - the diagnostic gap that left the failed playtest's refusal shown on one screen and written down nowhere.
- No wire, protocol, schema or content change: the protocol stays `guards-private-online/4` and the schema v35. `ReclaimSeat` and the capability mint already existed and are untouched; a released seat reaches every client as the roster's existing `Open` occupancy.
- The six-client rehearsal gains a third cohort whose cut ends a PROCESS rather than a socket, and whose relaunched process must return by `SEAT_RECLAIMED` with a rotated epoch and converge on the same outcome as the other five.

Closing check: `FreshProcessRejoinTests.ABrandNewClientForAReservedSeat_ReclaimsIt_WithARotatedEpoch`, `LeavingForfeitsTheSeat_WhileACrashKeepsItReservedForItsPlayer`, `TheHostCanReleaseAReservedSeatByHand_ButNeverALiveOne`, `ARetainedCapability_AnswersForItsOwnSessionAndSeatOnly` and `TheSessionsEventKinds_AreDistinct_SoAReleaseIsNeverReadAsAMatchStarting`, with `PrivateSessionSeatClaimTests.AGoodbyeFromASeatedConnection_ReleasesItsSeat` and `AReleasedSeat_TakesAnOrdinaryClaim_WhereAReservedOneRefusesIt`, are each recorded failing with the fix disabled at its own site and passing with it restored.

Non-goals: No expiry, timer or automatic release of a reservation; no change to how a capability is minted or checked; no wire, protocol, schema or rules change; no production presentation of the lobby; no reconnect behaviour beyond returning a player to a seat the host is already holding for them.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835k-give-a-relaunched-player-a-road-back-to-their-own-reserved-seat-completion-evidence).

### [x] 8.35l Refuse an action a standing rule modification has already barred, and name what barred it

User-approved corrective of 2026-09-03, the SIXTH amendment's corrective half, from a playtest report of a Swap offered, taken, and cancelled without explanation.

Observable outcome: an action whose Swap or Place a standing restriction has already barred is greyed rather than offered, a barred piece is absent from the candidate list beside legal ones, and the greyed control names the hero and card that barred it. Starting one is refused, so the card is never spent for nothing.

Acceptance checks:

- Every subject barred: the Skill is unavailable, while the same board without the standing effect still offers it and applies - Back to Back, Arcane Whirlpool, and Blend Into Shadows, whose subject arrives from no selection.
- Some subjects barred: the action stands, and only pieces its consumer can act on are named.
- A selection whose floor is two is unavailable where one legal piece remains.
- An unavailable offer names the barring hero, card and activity; an available one names none, and starting a barred action is refused.

Closing check: `SwapAndPlaceRestrictionFeasibilityTests`, failing before the fix and passing after.

Non-goals: no presentation surface, no rules change, no schema move.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835l-refuse-an-action-a-standing-rule-modification-has-already-barred-and-name-what-barred-it-completion-evidence).

### [x] 8.35m Shine every awaited hero's tray and card, and dim the rest of the hand while one resolves

User-approved amendment of 2026-09-03, the SEVENTH amendment's other two scopes: "Make the hero tray of the hero whos turn is it shine gold on his/her time to act... card can have the same shine... just a gold highlight," plus the same day's resolution-focus extension. `AwaitedInputQuery.IsWaitingOnViewer` (milestone 8.35j) answers one question at viewer scope; this reads the SAME question a second and third time, at hero scope and card scope, rather than a fresh derivation - "one predicate, three scopes" in the user's own words.

Observable outcome: Every hero's rail carries the top bar's warm gold treatment exactly while `AwaitedInputQuery.IsHeroAwaited` says the match waits on that hero - several trays at once during a commitment phase, the 8.35g release owner alone, or an undecided team-agreement participant. Every card currently a legal answer to input the match awaits FROM THE VIEWER - their own hand during commitment, an offered Defense, effect-choice, removal-prevention or p.15 upgrade candidate - carries the same gold shine. While the viewer's own card resolves it alone keeps that shine and every other held card dims, lifting once the resolution - and, since 8.35g, the release it now waits behind - ends; a card that becomes a legal answer mid-resolution shines and is never dimmed.

Acceptance checks:

- Hero scope reads three provably public arms only: a hero's own commitment (`Commitments`, the viewer-scope arm generalized), the release owner (`PendingTurnRelease.ActorId`, unscoped since 8.35g), and a standing team agreement's own undecided participant (`TeamAgreement.Participants`, public by its own doc comment). Several heroes shine at once during commitment.
- Hero scope does NOT read the leveling hero, the defense answerer, the respawn chooser, or the acting hero - a genuine gap, not an oversight: `IMatchReadModel` carries no per-hero decision-owner fact today, only the concrete `SeatProjection.DecisionOwner`, which is not on the interface. A case proves a genuinely standing decision does not shine a hero's tray, so the day that fact reaches the interface the case must flip red before the predicate can honestly extend to it.
- Card scope: every card offered for commitment, Defense, an effect choice, removal prevention or a leveling upgrade carries the gold shine; a privacy case proves another seat's own awaited hand never shines on this viewer's screen, because the projection never carries it at all.
- Resolution focus: the viewer's own resolving card shines; every other held card dims to the same opacity `HudElements.MarkUnavailable` already uses for a control the authority offers no command for; the dim lifts once nothing is resolving.
- Presentation-only: no rule, authority, projection, wire or schema change.

Closing check: the hero-scope cases in `MatchFeaturePresenterTests` (`SeveralHeroesOweACommitment_AndSeveralTraysShineAtOnceAtHeroScope`, `TheTurnReleaseOwner_ShinesAtHeroScope_AndNoOtherHeroDoes`, the Initiative/Minion Battle agreement pair, and `AHeroWithAGenuinelyStandingDecision_DoesNotYetShineAtHeroScope_TheFourDeferredArms`), the rendering cases in `MatchHudPresentationTests` (`EveryHeroStillOwingACommitment_ShinesGold_AndOnlyThatHero`, the three card-scope-per-family cases, and `TheResolvingCard_ShinesWhileTheRestOfTheHandDims_UntilItResolvesAndReleases`), and the privacy case in `SeatScopedMatchHudTests` (`AnotherSeatsAwaitedHand_NeverShinesOnThisViewersScreen`) are each recorded failing at the pre-fix revision and passing after.

Non-goals: The four deferred hero-scope arms above stay pinned to the Core track's event-channel milestone, which adds the missing per-hero decision-owner fact to `IMatchReadModel` in the same wire bump as the TENTH amendment's event log; a small follow-up milestone completes the tray once that lands. No narration, typography, END TURN or tooltip change - the FIFTH, THIRD and SIXTH amendments' remaining bullets. No new waiting state invented beyond what the projection already publishes.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835m-shine-every-awaited-heros-tray-and-card-and-dim-the-rest-of-the-hand-while-one-resolves-completion-evidence).

### [x] 8.35m1 Keep an unselected artwork face's awaited-answer shine out of its synthetic-chrome guard

Corrective for a gap 8.35m left standing, found closing the Edit Mode red; approved by the user 2026-09-04, completed in the same task.

`ConfiguredCardArtworkCatalogTests.UnselectedArtworkFace_HasNoSyntheticFrameOrLifecycleStrip` predates the SEVENTH amendment and asserted a fixed four-element set. 8.35m made its fixture, `CurrentRoundCards.DeckCard`, an unconditional `IsAwaitedAnswer` surface, so `ConfiguredCardFaceView.Build` conditionally adds a fifth: `gd-card-awaited-shine`. Fix is the assertion, not `Build`, already honest.

Acceptance checks: closing check failed at pre-fix HEAD `e6a2cd25`, passed after; 8.35m's shine/dim checks in `MatchFeaturePresenterTests`, `MatchHudPresentationTests`, `SeatScopedMatchHudTests` stay green; test-only, no production/rule/projection/wire/schema change. Non-goals: no shine-condition change, no behaviour beyond 8.35m/8.35n.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835m1-keep-an-unselected-artwork-faces-awaited-answer-shine-out-of-its-synthetic-chrome-guard-completion-evidence).

### [x] 8.35n Narrate the acting hero and the standing activity, and mark their piece on the board

User-approved amendment of 2026-09-03, the FIFTH amendment's remaining two items plus the FOURTH: "ACTING ON INITIATIVE X is not enough... hero name, who is acting, not only initiative... text a bit better, its important, make it look it," and, from interim-2 screenshots, "every viewer's BOARD marks the acting hero's piece." Derivability split exactly as milestone 8.35m's hero-scope shine: a decision the viewer's own projection carries in full reads a rich `NarrationLabels.Own` line naming the family; every other viewer reads only what `IMatchReadModel` already publishes regardless of ownership - the acting hero's name (`SelectedActor`), the release owner (`PendingTurnRelease.ActorId`, unscoped since 8.35g), and who still owes a commitment or a team-agreement answer.

Observable outcome: `RoundActionPresenter.FormatStatus` replaces the bare "Initiative N: choose an action, or end your turn." fallback with a hero-named line for every viewer, and an own-decision line for the eleven declared pending-decision families a viewer's own projection carries. The narration lane draws a bold two-tier headline/detail type scale (`MatchHudTheme.NarrationHeadlineSize`/`NarrationCalmHeadlineSize`/`NarrationDetailSize`) that demotes a secondary fact - Initiative, an attack total, a count still owed - to quieter detail beside the headline (`RoundActionViewModel.StatusDetail`). Every viewer's board additionally marks `SelectedActor`'s own piece with a distinct gold ring (`ActingHeroMarker`, driven by the already-public `BoardInteractionViewModel.FocusedHeroId`), independent of the existing target-cue and hex-corner-highlight systems, and clears the instant no actor stands.

Acceptance checks:

- Own-decision families read `NarrationLabels.Own("<FAMILY>", "<activity>")` - proven for the acting/releasing viewer's own turn, a Defense answer (with the Attack total demoted to `StatusDetail`), and a Movement choice (remaining spaces demoted); the effect-choice and removal-prevention families keep their existing composed sentences under the same family header.
- Every other viewer reads only the public fact: the acting hero's name and Initiative (`NarrationLabels.HeroActing`), the release owner's name (`NarrationLabels.HeroAwaitingRelease`), or a `WAITING ON:` line naming every hero still owed a commitment or a team-agreement answer (`NarrationLabels.WaitingOn`) - proven identical for every seat, because both facts are public and unscoped.
- Privacy: a non-owning viewer's narration never names a defense, leveling or respawn decision standing on the acting hero's own turn - the recorded deferred non-goal, proven from the exact shape a real non-owning seat's projection has (the owned field reads `null`), not a stand-in for one.
- The acting-hero board highlight is present identically for the Director and every seat (`BoardInteractionViewModel.FocusedHeroId` derives from the public `SelectedActor` alone) and clears to undefined the moment no actor stands.
- Presentation-only: no rule, authority, projection, wire or schema change; the marker competes with no existing board-cue state or priority.

Closing check: the narration and privacy cases in `MatchFeaturePresenterTests` (`TheActingHerosOwnScreen_ReadsYourTurnWithInitiativeDemotedToDetail`, `EveryOtherViewer_ReadsTheActingHerosNameAndInitiativeOnly`, `ADefenseAnsweredByTheOwningViewer_ReadsTheFullRichOwnDecisionLine`, `ANonOwningViewer_NeverNamesAnOwnerScopedActivityOnlyTheActingHerosName`, `AMovementChoiceForTheOwningViewer_ReadsTheOwnDecisionLineWithSpacesDemoted`, `TheReleasingHerosOwnScreen_ReadsYourTurnReleaseToContinue`, `EveryOtherViewer_ReadsTheReleasingHerosNameAwaitingRelease`, `TheViewersOwnUnansweredCommitment_ReadsTheOwnDecisionLine`, `AnotherHerosUnansweredCommitment_NamesThemInAWaitingOnLine`, `AnUnansweredAgreementParticipant_IsNamedInAWaitingOnLineForEveryViewer`) and the board-highlight cases (`TheBoardsActingHeroFact_IsPublicAndIdenticalForEveryViewer`, `TheBoardsActingHeroFact_ClearsTheMomentNoActorStands`) are each recorded failing at the pre-fix revision (behaviour reverted, the new vocabulary kept inert and compiled) and passing after; the three own-decision families sharing existing composers (`EffectChoiceStatusNamingTests`, `SeatScopedMatchHudTests.TheNarrationNamesAStandingMinionBattleRatherThanTheRoundItStandsIn`) are updated to the new family-header shape rather than weakened.

Non-goals: No live pre-commit aiming - a transient in-progress selection is never streamed to other viewers, only the committed acting piece. No spectator narration of an owner-scoped activity (that a defense, leveling or respawn decision is standing, and whose) - pinned to the Core track's event-channel milestone that adds the missing per-hero decision-owner fact to `IMatchReadModel` in the same wire bump as the TENTH amendment's event log. No tooltip, END TURN or fizzle-notice change - the SIXTH and THIRD amendments' remaining bullets.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835n-narrate-the-acting-hero-and-the-standing-activity-and-mark-their-piece-on-the-board-completion-evidence).

### [x] 8.35o Give the dedicated host's console a typed release command

Corrective for a gap 8.35k left standing, found by the user's interim-2 playtest: `PrivateSessionHost.ReleaseSeat(SeatId)` shipped as the approved manual escape hatch, API-tested, but reachable by no human. Approved as a small follow-up corrective, immediately after the swap-feasibility corrective (8.35l).

Observable outcome: an operator running the dedicated host - the `-guardsPrivateHost` role, or the interactive lobby's host mode that `HOST - Start Server.bat` starts, both of which pump a live `PrivateSessionHost` every frame - types `release <seat>` and a RESERVED seat is handed back exactly as `ReleaseSeat` already does, logged `SEAT_RELEASED seat=<seat> reason=manual`; an occupied/open seat or an unknown name is refused with a logged reason. NOT a kick: an occupied seat stays refused.

Acceptance checks:

- A background-thread `Console.In` reader (`HostConsoleReader`, Presentation/Interaction - never the session layer) queues lines without blocking `Update`; `Start()` returns immediately, and no console attached exits the read thread quietly rather than spinning.
- `HostConsoleCommands.Dispatch` parses `release <seat>` against the session's existing public surface only (`Roster`, `ReleaseSeat`) - no new session-layer member. A Reserved seat releases and logs `reason=manual`, reusing the existing marker via `ReleaseSeat`'s own call site being the only one of that event kind at connection ordinal 0. Claimed, Open, unknown-name and missing-argument are each refused with their own reason.
- Wired into both processes that ever pump a live `PrivateSessionHost`. The dispatch path is exercised end to end by a regression at the host-object level against `PrivateOnlineWireHarness`; the OS-console wiring itself is compile-proven only, recorded as the honest fallback for a corrective this size.

Closing check: `HostConsoleCommandsTests`' release/refuse/unknown-command/no-op cases and the reader's non-blocking start, none of which exist before this milestone.

Non-goals: No kick / no displacing a live seat. No timer or automatic release. No new wire message, schema, or session-layer member. No change to `ReleaseSeat`'s own behavior or its existing tests.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835o-give-the-dedicated-hosts-console-a-typed-release-command-completion-evidence).

### [x] 8.35p Move a charge before its attack resolves, and wall a card-text attack against its own seat's rewind

User-approved amendment of 2026-09-04, the NINTH amendment: "Brogan should move for his charge before the attack resolves, so does tigerclaw with his gold for example. Dunno if other heroes have similar stuff. They move as soon as they select, and then they attack." Worker C's diagnosis measured the cause as effect-graph ordering rather than a rules or rendering defect - both questions were asked inside one capability node placed before the Move node - and measured the affected set as exactly four playable faces, of which only Brogan's three ask twice. Tigerclaw's Blink Strike asks once and is deliberately left alone: its own declaration records that asking twice "would let a player pick a line and then a unit that is not on it". The same diagnosis raised an incidental pre-existing gap in milestone 8.35f's undo bounds, folded in here because it lands on exactly this card class.

Observable outcome: Mad Dash, Bullrush and Furious Charge resolve in the order their sentence prints. `choose-charge-line@1` asks only where the hero lands; the Move commits on that answer, in the same accepted command, so the projection published in reply already shows him standing on the destination; and the unit is named afterwards by an ordinary `choose-target@2` measured from the space he now occupies. Separately, a seat's own rewind is walled above an Attack whose target was bound by card text, which `DirectorCommandKind.ChooseAttackTarget` never announces for this class.

Acceptance checks:

- The candidate set is unchanged by the reorder, asserted rather than argued: for every offered destination, the enemy units within 1 of it before the Move equal the enemy units within 1 of the hero after it, read through the one shared `RosterPresenceQuery` both shapes use. A direct Move of the actor removes no unit and moves no other, and this is what makes the change a reordering.
- No destination is offered that strands the Attack, and rulebook p.10's stop is unmoved: `choose-charge-line@1` still offers only destinations with a legal target beside them, stands before the Move, and is judged normally, so a board with no straight destination beside an enemy still refuses at that step and the Attack is still greyed.
- The Attack stays offered where the card is for. Availability for a `ByCardText` binding walks the card text before any of it has run, so a post-Move adjacency question judged from the pre-Move square answered "nobody": `EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding` no longer judges a board reading standing behind a declared Move it has not taken, which only ever suppresses a refusal and never manufactures one.
- The derived player-path shape table does not move: the charges keep both the board-space and board-unit shapes, the space now contributed by `choose-charge-line` and the unit by `choose-target`, so `HeroPlayerPathMatrix.CapabilityShapes` changes and no row does.
- The seat undo wall rises for a card-text-bound Attack on the same rule as the ordinary path - once the target is named, and not before, so the committed Move stays rewindable exactly as the amendment requires - and it settles the floor above an Attack that binds its target and completes inside one command.
- Content scope: Brogan's content revision alone advances, with his eighteen artwork-catalog entries re-pinned beside it; no capability is registered, the persisted snapshot schema stays at v35, and no wire protocol moves.

Closing check: `ChargeSequencingTests.AnsweringWhereHeLandsMovesHim_AndOnlyThenIsTheVictimAsked`, `.AChargeIsOfferedWhenTheEnemyItCanReachIsNotAlreadyBesideHim`, `.ACardTextBoundAttackWallsItsOwnSeatsRewindOnceTheVictimIsNamed` and `RosterTargetGeometryCapabilityTests.ChargeLine_AsksOnlyWhereHeLands_AndCommitsNoMovementOfItsOwn` are each recorded failing at the revision that isolates their own fix and passing after; `RosterTargetGeometryCapabilityTests.ChargeLine_OffersOnlyDestinationsWhoseTargetSetSurvivesTheMove` passes on both sides, which is the evidence the target set never moved.

Non-goals: No rules change - adjacency and targeting were already measured from the destination and still are. No change to Tigerclaw's Blink Strike, whose one question determines both facts. No presentation work: the move is visible because it is committed, not because anything was animated. Nimble Strike and Leap Forward stay deferred and compose nothing.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835p-move-a-charge-before-its-attack-resolves-and-wall-a-card-text-attack-against-its-own-seats-rewind-completion-evidence).

### [x] 8.35q Fill empty seats with spawned test drivers for solo playtesting

THIRTEENTH amendment: "I find it really hard to test running 6 clients at once. Can you make AI play the game or something so i just play with one client only?" Answer: expose the existing rehearsal/SixClient machinery's real-lobby, real-HUD, offers-intersected-with-AuthorizedCommands pattern as a shipped, user-facing practice affordance - explicitly NOT an AI opponent (durable non-goal unchanged).

Observable outcome: the host's lobby (`Tools/PlaytestLaunchers/HOST - Start Server.bat`, the interactive lobby a real solo player actually runs, per 8.35o's own finding) offers FILL WITH TEST DRIVERS whenever a seat is still Open and the match has not started. Pressing it spawns one real keyed client process per Open seat - the same running executable, re-launched with a distinct log path - each of which joins the real lobby, claims its seat, and answers whatever it is offered (commitments, defenses, agreements, leveling, releases) with a random legal choice at a human-followable pace (2 s default, overridable). Every spawned process is the host's own OS child, killed when its session ends.

Design:

- Host affordance is a lobby control only (`PrivateLobbyControl.FillWithTestDrivers`), not a launcher flag - the raw `-guardsPrivateHost` CLI role is CI/rehearsal-only and no real tester ever touches it.
- Reuses `PrivateOnlineRehearsalDriver.AuthorizedProjectedCommands` (bumped `internal`) directly rather than duplicating it; the new, much simpler `PrivateSeatDriver` (new file) carries the shipped behavior, deliberately kept separate from the 1184-line CI-only rehearsal driver rather than growing it. New CLI vocabulary (`-guardsTestDriver`, `-guardsTestDriverSeat`, optional think-seconds/seed), endpoint/key via env vars only - the same command-line-secrecy rule the rehearsal driver already follows.
- Spawning goes through `ITestDriverProcessLauncher`, a small seam so the spawning DECISION is Edit-Mode-testable without ever starting a real process; `RealTestDriverProcessLauncher` is production. Labeling (`PrivateLobbySeatRow.IsTestDriverSeat`) is deliberately host-screen-only, sourced from the host's own local spawn record rather than the roster - putting it on the wire would be the protocol change the amendment says isn't expected.
- No new session-layer member, no protocol change - verified by grep, and by the regression completing under the unmoved wire version.

Acceptance checks:

- `PrivateLobbyModel.ForHost` offers `FillWithTestDrivers` exactly while a seat is Open and the match has not started, and labels a host-spawned seat's own row `IsTestDriverSeat` from the caller-supplied set alone, never the roster.
- `PrivateLobbyPresenter.Press(FillWithTestDrivers, ...)` raises `FillWithTestDriversRequested` without touching any session itself - spawning stays the one thing only the driver may know how to do.
- `PrivateSeatDriverOptions.TryParse` accepts a well-formed line and refuses a missing seat, a missing lobby flag, a non-loopback address, and an empty session key, each with its own named reason.
- Regression: a solo cohort in `Invoke-PrivateOnlineRehearsal.ps1 -Topology Solo -FaultPlan None` - one scripted "human" seat plus the host's own real FILL control filling the other five - completes a full six-player match to one converged outcome from only two launched processes, and no spawned test-driver process outlives the host session that spawned it.

Non-goals: No AI opponent, strategy, or difficulty - every legal answer is equally likely by design, a durable property. No wire/protocol change. No in-process second driving path - every seat is an ordinary keyed process. No change to the CI-only rehearsal driver's own scripted cohorts beyond the new `Solo` mode that drives this regression.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835q-fill-empty-seats-with-spawned-test-drivers-for-solo-playtesting-completion-evidence).

### [x] 8.35r Publish the seat-public match log and who the table is waiting on

TENTH amendment: "Make a log ... Its to tell players what happened previously in the game. But player facing: Moves, Attacks, Defenses, Skills used." The coordinator's scope ruling gathers ALL remaining wire growth into this one protocol bump, so the log and the SEVENTH amendment's blocked tray arms cross together rather than in two versions.

Observable outcome: every seat, and every spectator of a seat, can read what has happened - moves, fast travels, resolved attacks and defences, skills, clears, defeats, respawns, end-of-round results, level-ups, round and turn boundaries - and can see who the table is currently waiting on, including while another player levels up, answers a defence or chooses a respawn. No presentation surface ships here: the drop-down, the tray completion and the narration completion are separate UX milestones consuming these facts.

Design:

- The log is DERIVED from the retained rule-event log, never stored. That source is forced rather than preferred: `MatchAggregate.Restore` installs an empty round-event stream while restoring the rule-event entries in full, so a channel built on round events would come back blank after a snapshot restore. Undo, replay and resync redraw it for free because `MatchSession.Undo` reconstructs from the retained command prefix.
- Selection is exactly `tmp/8-35d-8-35e-multiplayer-corrections/worker-b/EVENT_LOG_CONTRACT.md` §1, typed as `MatchLogEntryKind` so no surface parses diagnostic prose, with event names built from the rule-event enum members via `nameof` so a renamed kind is a compile error rather than a line that silently stops being selected.
- The seat filter is an IDENTITY, and that is the privacy proof: §1 admits only events the whole table can already see, so every seat's log equals the Director's and there is nothing to redact.
- The log rides inside the seat projection message beside that seat's record - not in the record, which has 2,173 B of headroom under the sweep's own guard, and not in its own message kind, which consumed a gameplay sequence and absorbed the gaps a dropped projection must leave for client recovery.
- `DecisionOwner` joins `IMatchReadModel` and moves no bytes: the seat record has carried it since 8.35's wire slice and only the read-model contract lacked it. Who is awaited is table-public; what they may choose stays owner-private on `DecisionContract`.
- `UndoMarker` is a published read-model fact rather than a synthetic log entry, because an undo records no command and no event and a minted entry would have to be stored.

Acceptance checks:

- `MatchLogChannelTests` proves the vocabulary names nothing the contract excludes, that every seat's log equals the Director's, that the selection carries exactly its source events over a driven match, that a multi-hex move folds to one line from first origin to last destination, that an attack and a defence each fold to one line with their outcome, that a blocked step names no blocker, that a clear is one line, and that a driven rewind drops its lines while the marker names who took them back and counts a run.
- `PrivateOnlineEventChannelTests` proves every kind round-trips the wire with completeness asserted against the enum, that a truncated payload is refused at every prefix, that the projection-and-log frame stays inside the sealed bound, that a returning seat receives the same log as one that never left, that every client converges on the authority's log across the seeded wire walk, that a version 5 peer is refused before any payload interpretation, that who-is-awaited is public to every seat for the levelling, defence and respawn arms while the contract stays owner-private, that a decision standing on the actor's turn is visible as such, and that the encoded log names no card the table has never seen.
- `PrivateOnlineSynchronizationTests.EveryReadModelMember_IsCarriedOrNamedExcluded` carries all three new members with the exclusion floor unmoved at 7.
- `OnlineMatchHudRulesPrivacyTests.SeatWireMeaning_UsesProtocolSix_AndRejectsEveryOlderFrame` refuses versions 1 through 5 from the bounded header.

Non-goals: No log drop-down, no tray or narration completion, no blocker named on a blocked-action line (milestone 8.35l's offer-side `ActionUnavailability` stays the single provenance path), and no permanent positioned marker for a rewind already played past - that needs storage the engine does not have.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835r-publish-the-seat-public-match-log-and-who-the-table-is-waiting-on-completion-evidence).

### [x] 8.35s Name why a greyed control cannot be taken, in a cursor tooltip

SIXTH amendment, cursor-tooltip delivery: a fixed short vocabulary composed from the authority's own answer, never re-derived in presentation. `ActionOffer.Unavailability` (milestone 8.35l) already crosses Core through the wire into `PrintedActionRegionViewModel.Offer`, unread until now.

Observable outcome: an offer a standing rule modification has barred hovers `BLOCKED BY <HERO> — <CARD>`; Clear's offer, unavailable for exactly one reason, hovers `NO TARGET`; every other unstated cause - `StampAvailability`'s remaining clauses, which conflate an empty target, an uncomposed text, an unthreaded restriction and the card's other action already under way behind one bit - and every unperformable effect-choice mode hover the recorded generic word rather than a guessed one.

Acceptance checks:

- `ActionUnavailabilityLabels` composes each word from an already-resolved name; blank names refuse to compose.
- A real Sabina-versus-Wasp fixture (8.35l's own board) hovers `BLOCKED BY WASP — MAGNETIC DAGGER`.
- The same fixture's narrowed candidate list names only the free minion; the barred partner draws no invented ring or reason.
- A real Skill greyed for want of a target, no modification involved, hovers the generic word, never a guessed `NO TARGET`.
- Pre-fix red: vocabulary inert, the three composition sites reverted, three cases fail in isolation; the narrowed-candidate case stays green throughout.

Closing check: `ActionUnavailabilityTooltipTests`, plus extended `ClearPresentationTests` and `MatchFeaturePresenterTests` cases.

Named gaps: "NOT ENOUGH COINS" has no wired site - Level Up auto-purchases whenever affordable; a board refused ring for one barred candidate has no per-candidate reason anywhere, and an available offer's `Unavailability` is `None` by construction - outside this milestone's scope; the generic case conflates four authority-side causes behind one bit.

Non-goals: no fizzle notice, no omnipresent END TURN, no rule or wire change.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835s-name-why-a-greyed-control-cannot-be-taken-in-a-cursor-tooltip-completion-evidence).

### [x] 8.35t Dial the address the host actually bound, not a hardcoded loopback constant

Corrective, live-defect-driven. Milestone 8.35q's FILL WITH TEST DRIVERS spawned every test-driver child with its dial address hardcoded to `"127.0.0.1"`, regardless of what the host itself had bound - so a host bound to this machine's own real network address (the shape the tester instructions' own solo-practice paragraph invites, and the one a host rehearsing its real port-forwarded setup even alone requires) spawned children that dialed an address nothing was listening on. Each pressed Join, and each then sat forever: `PrivateLobbyPresenter.Press` returns true the instant the control is offered, saying nothing about whether the join it triggered ever reached anyone, so nothing ever told the driver its dial had failed - the live report named 34 orphaned `GuardsDigitalSmoke.exe` processes left running after repeated FILL presses tried to work around it.

Observable outcome: a spawned test driver dials whatever address the host actually bound - loopback by default, this machine's own address when the host bound one, falling back to loopback only for a wildcard bind, since every spawned child runs on the host's own machine regardless. A join that cannot reach anyone now fails within its own dial attempt, is logged and reported on the host's own screen by name, and the driver process exits promptly instead of hanging. A second FILL press no longer spawns a redundant child onto a seat already carrying an outstanding attempt, and every spawned child now also watches its own spawning host's OS process id and exits if that host disappears without a clean Leave.

Design:

- `PrivateSessionEndpoint.ResolveLocalDialAddress()` (Network) is the one place a wildcard bind resolves to loopback and any concrete address is returned verbatim - confirmed directly against a real `TcpListener`/`TcpClient` pair, not assumed. It lives in the adapter, not beside its one caller in Presentation, because that caller may name no socket type of its own (`PrivateSessionArchitectureConformanceTests.SocketTypes_LiveOnlyInTheNetworkAdapter`).
- `PrivateSeatDriverOptions.TryParse` no longer confines a spawned driver's dial address to loopback - the assumption the live defect disproved. `-guardsPrivateClient` has always been free to dial any address with any key, and the session key is what actually gates entry here too; only a numeric, well-formed endpoint is still required.
- `PrivateLobbyDriver.BeginJoining`'s two silent failure paths - a malformed endpoint and a failed `Dial()` - now set `LastJoinFailure` and log through `PrivateOnlineMarkers.Fail`, which a spawned `PrivateSeatDriver` checks immediately after pressing Join and fails loudly on if set.
- `FillEmptySeatsWithTestDrivers` tracks a 12-second per-seat join deadline: a seat still open when its deadline passes is reported by name, in the log and the lobby's own feedback line, and freed for a later FILL to retry; a seat already carrying a live deadline is skipped by a repeated press rather than sent a second child.
- Every spawned driver also carries its parent host's OS process id and polls it independently of `Leave()`/`OnDestroy()`, so a host that dies without a clean shutdown still orphans nothing.

Acceptance checks:

- `PrivateSeatDriverTests`: `ResolveLocalDialAddress` returns a concrete bind verbatim, falls back to loopback for both the IPv4 and IPv6 wildcard; a non-loopback dial address is now accepted where it was refused, a malformed one is still refused; a parent process id parses when present and leaves the watchdog off when absent or unparsable. Pre-fix red, isolated (both call sites behavior-reverted in place, new vocabulary kept present and inert, per this repo's own ceremony rule): exactly the two cases pinning the defect fail, 18/20; restored, 20/20.
- `PrivateLobbyPlayModeTests.AJoinToAHostThatIsNotThere_IsDrawnOnTheEntryForm` now also expects the new host-log FAIL line the fix introduces (`LogAssert.Expect`), a companion fixture update this milestone owes since it added that line.
- `Invoke-PrivateOnlineRehearsal.ps1 -Topology Solo -FaultPlan None`: the packaged interactive-lobby command line (both shipped `.bat` launchers' own flag shape) still boots cleanly to its real lobby; a join dialed at a port nothing is listening on now fails fast and visibly (exit 1) instead of hanging; the host presses FILL WITH TEST DRIVERS twice back to back and spawns exactly five test-driver clients, not ten, all five dialing the host's own address, before all six seats claim and the match begins.

Non-goals: No wire, protocol, or schema change. No change to the host's own bind rules (8.35a1's unkeyed-bind refusal is untouched). No AI, strategy, or difficulty change to the test-driver's own random-legal play.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835t-dial-the-address-the-host-actually-bound-not-a-hardcoded-loopback-constant-completion-evidence).
