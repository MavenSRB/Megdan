# Guards Digital - Razzle Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The twenty-eighth-hero sequence](../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md) owns acceptance and status.

## Source and rule authority

Razzle is the selected twenty-eighth hero. The curated Tabletopia source `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Razzle/` holds nineteen byte-distinct fronts and nineteen backs in one byte variant (MD5 `b24e04deb5397d8d9d013b15582088f3`). The fronts sit on `Image_10_Atlas` (manifest group "Razzle the Fay", group 8213155). All nineteen fronts, the selection profile and the dashboard were read at double resolution on 2026-09-30, and the item icons by montage. The Razzle deck is identical in the corpus bundle (s799373) and the later s804629 bundle (`CARD-DIFF-s804629-vs-s799373.md`). These are source references, not distribution permission.

RZ-01, profile: `cards/hero-selection/randomizers-tt30__7723698__front.png`, SHA-256-identical to `active-hero-cards/_selection-cards/Razzle__front.png` (`d526f6a363664df3750d1fab8c3c8a7aef54cffb9240694bfa2aa9f505373c25`). It shows **four stars** and **Tactician, Melee**.

RZ-02, dashboard: `cards/hero-dashboards/Razzle__7732137__front.png` (element 7732137) prints the title **"Razzle the Ringmaster"** (the manifest group name is "Razzle the Fay"; the printed dashboard is displayed), four ordinary turn slots and the eight-level track. Its back is a Tabletopia placeholder, so the p.6 dashboard back (miniature, tokens) is not in the corpus. A centre-scanline pixel reading of the bottom bars (about 10 px per pip, confirmed on an enlargement and against the profile card) gives Attack **3**, Defense **1** plus **3** brighter pips, Initiative **8**, Movement **5** plus **1** brighter pip, each of eight. p.21: brighter sections are conditional.

RZ-03, standee (`extracted/text-assets/manifest-bundled__42.json`, and the same element in s804629): **one** element, **8596252** `Razzle` (publicElementId 5917483, VerticalTokens, tokenType Standee). It carries two 302x302 round faces on `Image_1_Atlas` (x 1293 and 1597, y 1096; orange and light-blue rings) and two 429x479 upright figures (x 431 and 862, y 1096). No other element is his: the hexagonal `clone` token (5366754, `Image_3_Atlas` x 0 y 1043) was inspected and shows another character (bowler hat, goggles), not Razzle. The manifest states no piece counts, so **the corpus does not say how many Razzle bodies the physical game provides** (RZ-Q1).

RZ-04, rulebook: the local second-printing text names no Razzle, Stunt Double or clone. General authority:
- p.7: respawn when not on the board; one action per turn.
- p.8: minion battle (minions only), tokens removed at round end, Lane Push.
- p.9: basic cards and basic actions (Gold and Silver); Range/Radius icons.
- p.10: mandatory and optional steps, choices, modal and repeat, card states, retrieve.
- p.11: Movement, Fast Travel, movement in card text; Movement primary text "applied even if you choose to move zero spaces".
- p.12: Skill, Hold.
- p.13: Attack, Defeat, coins and Life by level and lowest Tier.
- p.14: Defense, Defense/Skill cards, minion defense modifiers.
- p.16: items; the Ultimate works "as long as your hero is on the board".
- p.17: Friendly ("never includes you"), Hero, Active effects.
- p.18-19: Immune, Obstacle, Place, Push, Remove, Spawn, Swap, Target, Unit.
- p.20: Handicap.

Printed fronts control each clause under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy).

## Printed inventory

File stems below are under the Razzle source directory and end in `__front.png`. D/M are the ribbon's Defense and Movement icons; A/R/r are the primary's Attack, Range and Radius icons. A missing icon is absent, never zero. The Gold, Silver and Red faces print the ring-with-two-arrows Radius icon; the Blue and Green Ranged faces print the crosshair-and-arrow Range icon. Magic Trick and Aaaand it's gone! are Skills with no icon.

| Stem | Face / colour / tier | Init | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| razzle__7475655 | Stunt Doubles / Gold / - | 13 | 1 | 1 | Basic Attack / A 2, r 1 | - |
| razzle2__7518579 | Crowd Control / Silver / - | 13 | - | - | Basic Defense/Skill / Defense 1+, r 3 | - |
| razzle3__7475666 | Phantom Strike / Red / I | 9 | 3 | 4 | Attack / A 4 | - |
| razzle4__7475670 | Hit and Gone / Red / II | 9 | 4 | 4 | Attack / A 4 | Defense |
| razzle5__7486207 | Rummage / Red / II | 9 | 4 | 4 | Attack / A 4, r 3 | Initiative |
| razzle6__7486209 | Into Thin Air / Red / III | 10 | 4 | 4 | Attack / A 5 | Range |
| razzle7__7486210 | Ransack / Red / III | 10 | 4 | 4 | Attack / A 5, r 3 | Movement |
| razzle8__7647987 | Alleyoop / Blue / I | 10 | 2 | 3 | Skill - Ranged / R 3 | - |
| razzle9__7647988 | Group Performance / Blue / II | 11 | 3 | 3 | Skill - Ranged / R 4 | Attack |
| razzle10__7647928 | Magic Trick / Blue / II | 11 | 3 | 3 | Skill | Defense |
| razzle11__7647989 | Team Spirit / Blue / III | 11 | 3 | 3 | Skill - Ranged / R 4 | Radius |
| razzle12__7647931 | Aaaand it's gone! / Blue / III | 11 | 3 | 3 | Skill | Defense |
| razzle13__7709373 | Tightrope / Green / I | 2 | 1 | - | Movement / 2 | - |
| razzle14__7709372 | High Wire / Green / II | 2 | 2 | - | Movement / 2 | Initiative |
| razzle15__7655534 | Theatrics / Green / II | 2 | 2 | 2 | Skill - Ranged / R 2 | Attack |
| razzle16__7709371 | Wire Dancers / Green / III | 1 | 2 | - | Movement / 2 | Initiative |
| razzle17__7706372 | Spectacle / Green / III | 1 | 2 | 2 | Skill - Ranged / R 2 | Attack |
| razzle18__7709414 | Twin Strike / Purple / IV | - | - | - | Ultimate | - |
| razzle19__7647985 | Stunted Doubles / Gold / Handicap | 12 | 1 | 1 | Basic Attack / A 1, r 1 | - |

Items: Attack x3, Defense x3, Initiative x3, Movement x1, Range x1, Radius x1. Crowd Control's ribbon is blank; its "1+" is the Defense value of its Defense/Skill primary (p.14: a Skill when played, a Defense when discarded to defend, and its text applies either way). The three Movement cards print no Movement ribbon icon; their 2 is the primary Movement value. Upgrade lines (by name family, validated against p.15 at dealing):
- Red: Phantom Strike -> Hit and Gone or Rummage -> Into Thin Air or Ransack.
- Blue: Alleyoop -> Group Performance or Magic Trick -> Team Spirit or Aaaand it's gone!.
- Green: Tightrope -> High Wire or Theatrics -> Wire Dancers or Spectacle.

### Printed clauses

Quoted from the fronts; each implementation test uses the front, not this routing copy.

- **Stunt Doubles:** "Target a unit adjacent to you. After the attack: Spawn up to 3 more of you in radius; each of you is the same hero, except when actions are performed. If defeated, remove all of you."
- **Stunted Doubles (boxed Handicap):** the same, but "Target a hero adjacent to you."
- **Crowd Control:** "When used as a defense action, +2 [Defense icon] Defense for each other you in radius. When used as a skill action, remove all other you in play."
- **Phantom Strike:** "Target a unit adjacent to you. After the attack: If there is more than one of you in play, you may remove one of you."
- **Hit and Gone:** "Target a unit adjacent to you. After the attack: You may remove one or more of you in play, except the last one of you."
- **Rummage:** "Target a unit adjacent to you. After the attack: If there's another you in radius, you may retrieve a discarded card. (All of you share cards in hand, deck, and discard)."
- **Into Thin Air:** "Target a unit adjacent to you. After the attack: You may remove one or more of you in play, including all of you. (If all are removed, you are not defeated. Respawn as normal )."
- **Ransack:** "Target a unit adjacent to you. After the attack: For every other you in radius you may retrieve a discarded card."
- **Alleyoop:** "Swap places with a friendly hero in range. Move one other you 1 space." **Group Performance / Team Spirit:** the same, ending "up to 2 / up to 3 spaces."
- **Magic Trick / Aaaand it's gone!:** "Push a unit adjacent to you up to 2 / 3 spaces; for every space the target moved, move this one of you one space in the opposite direction."
- **Tightrope:** "After you move you may move another one of you 1 space." **High Wire / Wire Dancers:** the same, ending "up to 2 / up to 3 spaces."
- **Theatrics:** "Swap places with a minion in range."
- **Spectacle:** "Swap with a minion in range. May repeat once by a different one of you, targeting a different minion."
- **Twin Strike:** "After you perform a basic attack action a different you may repeat it once, targeting a different unit."

## Readings the rulebook and the cards settle

- **One hero, one hand, one level.** "Each of you is the same hero": one dashboard, hand, deck, discard (Rummage's reminder states it), coins, level, items, Ultimate and card states. Every body is one of "you", never "another unit" of his.
- **Every body is a unit on the board and an obstacle** (p.18-19 Unit, Obstacle). Presence reads see each body; how targeting and counting see them is RZ-Q2.
- **"You", "other you" and "one of you" in his text.** The words name bodies. "This one of you" (Magic Trick) is the body performing the action. "Other you", "another you" and "another one of you" are the bodies other than that one. "One of you" and "all of you" include it (RZ-Q6 asks about removing it). "A different you" / "a different one of you" is a body other than the one that performed the first instance.
- **Spawn takes from the supply** (p.19: "the spawned object must be taken from the supply and cannot be taken from the board"). Stunt Doubles has no reclaim, unlike a token Place. Each spawn goes into an empty space within the printed Radius (1, raised by Radius items per R1) of the acting body, and "up to 3" admits 0-3. The supply size is RZ-Q1.
- **Remove is not Defeat** (p.19 Remove: "Do not collect any coins"). Removing bodies never pays coins, spends Life or counts as a Defeat. Into Thin Air's reminder confirms that removing every body is not a Defeat.
- **Off the board, he respawns as one body.** `HeroRespawn` already keys p.7's offer on board absence, not on Defeat. After Into Thin Air or a Defeat he respawns as a single body in his Throne Zone. While no body stands, p.16's Ultimate is inactive ("as long as your hero is on the board").
- **Bodies are not tokens.** p.8's round-end token removal and p.13 Clear never touch them, so bodies persist across rounds until removed or defeated.
- **The minion battle and Lane Push count minions only** (p.8, p.18 Minion Battle). Bodies change neither, and nothing on p.8 reads hero occupancy of a Throne or lane.
- **Fast Travel** (p.11) is blocked by "enemy heroes or minions in the Zone"; that is a presence check (p.18 Immune: presence checks count), so every body blocks an enemy's Fast Travel in its Zone. His own Fast Travel is the acting body's (RZ-Q4).
- **Hold** (p.12) does nothing and needs no body; p.7 respawn is offered first when no body stands.
- **Immunity is addressed to the hero** (`RuleObject.ForHero(HeroId)`), so an immunity on Razzle covers every body.
- **"Swap places with a minion in range"** names any minion, either team (p.19 Swap; heavy minion immunity p.13 still applies to an enemy heavy). A minion leaving the Battle Zone returns by p.18.
- **"Push a unit adjacent to you up to N"** is p.18's Push measured from the acting body; W5 lets it move 0.
- **Rummage's "If there's another you in radius"** is a printed "If" (RULING 53: false is a legal do-nothing). **Ransack's "For every other you in radius you may retrieve"** counts the other bodies within Radius 3 of the acting body when the step starts; each retrieve is optional.
- **Crowd Control is a Defense/Skill card** (p.14). Its text is split by how it is used, so each half applies only to its own use.
- **Twin Strike is a p.16 permanent Ultimate.** "Basic attack action" is p.9's basic Attack primary: only Stunt Doubles' (and the boxed Stunted Doubles') Basic Attack, never a Red Attack.
- **Spectacle's "May repeat once"** is W2's optional repeat.

## Capability mapping

Probed 2026-09-30 against the build at `ae16072e` (schema v48, `AuthoritativeSnapshotSerializer.CurrentSchemaVersion`; wire `/25`, `PrivateSessionProtocol.Version`). Each block re-probes before claiming reuse.

**The engine has one space per hero, enforced in several places:**
- `AdjacentMoveAuthority` keeps `List<HeroOccupancy> occupancies` (`HeroOccupancy` is `HeroId` + `HexCoordinate`). Its constructor throws "Initial occupancies must have unique hero IDs". `CommitValidatedHeroRespawn` refuses a hero already present. `CommitValidatedHeroRemoval` removes the one entry. `TryGetHeroCoordinate(HeroId)` returns one coordinate.
- `BoardOccupancyIndex` is keyed by coordinate. `BoardOccupant.ForHero(HeroId)` could stand on several spaces; the per-hero rule lives in the authority.
- `BoardObjectReference.ForHero(HeroId)` names a hero with no body. `TryGetObjectCoordinate` over it has 4 callers. `TryGetHeroCoordinate` has 38 call sites in 22 files, and `.Occupancies` is read at 36 sites in 17 files.
- **Hard single-space failures:** `DefenseValueQuery` throws unless the defender "occupy exactly one space". `AttackTargetQuery.FindLegalEnemyTargets` reads the attacker with `SingleOrDefault` and a target with `Single`, so a second body throws.
- `HeroDefeatResolution.RemovedHero` is one `HeroOccupancy?`, and the attack target is a `HeroId` (`TurnResolutionAuthority` `attack.TargetHeroId`).
- `RuleObject.ForHero(HeroId)` addresses modifications to the hero. That is the right grain for "the same hero" (immunity, Counts As), and it stays hero-level.
- `SourceTeamRelation` (`OpposingSource`, `AlliedWithSource`, `AnyTeam`, ...) is a team relation to the source hero. Where a selection excludes the actor, it compares against `BoardObjectReference.ForHero(actorId)` (for example the printed-icon disk in `SyntheticTargetingCapabilities.Composition.cs`), which carries no body. Every body would therefore compare equal to the actor, which matches RZ-Q5's recommended reading. A body-carrying reference must keep that equality at hero grain for this use.
- `EffectCapabilityContext.ActorId` is a `HeroId`. Content reads the actor's space through `TryGetObjectCoordinate(BoardObjectReference.ForHero(context.ActorId))` (for example `MrakRockPlacementContent.TryActorSpace`), with `ProspectiveActorSpace` for offers.
- The snapshot's `WriteBoard` writes hero occupancies ordered by `HeroId` alone (`AuthoritativeSnapshotSerializer.Setup.cs`), and the reader rebuilds `HeroOccupancy` rows.
- The wire's `NetworkHeroRow(heroId, q, r)` has no body, and `NetworkMatchClient.Heroes` maps rows back to `HeroOccupancy`.
- `SeatProjectionFactory` passes `source.Heroes` whole into `SeatProjection.Heroes`. The board is open information, so bodies need no per-seat filter, only the row shape.
- Presentation: one `PrimitiveHeroView` per `HeroId`. `MatchSceneCompositionRoot.EnsureViewsForConfiguredSeats` skips a seat that has a view. `UnityMatchFeatureViews.HeroViewFor(HeroId)` returns the first match, `BoardInteractionViewModel` carries `IEnumerable<HeroId> attackTargets`, and `PrimitiveHeroPointerInput` hovers a `HeroId`. `PlacedTokenView` draws tokens, not heroes. `HeroTokenArtwork` / `Prepare-HeroTokens.ps1` prepare the 302x302 faces.

**Existing contracts to reuse** (each re-probed by its block):
- Swap with a friendly hero or a minion in range: `synthetic-swap-actor@1` (the actor is read from the frame, so it follows the acting body once the actor's space does).
- Push adjacent up to N: the Push family with W5. The retreat "in the opposite direction" is Cutter's Evasive Shot shape: `choose-line-destination@1` narrowed by 8.39a's `continuedLineFrom`, fed by a former-target space. The realised count is `SyntheticDisplacementCapabilities.RealisedDistanceOutputName` on direct Move (8.35z30). Whether the push publishes one is re-probed at 9.21f.
- Retrieve a discarded card, printed "If" gates (RULING 53), and "for each" counts.
- A Defense-text value contribution (Vampiric Shield's shape through `DefenseValueQuery`'s contributions).
- The after-basic-action Ultimate repeat: Tigerclaw's `CloakAndDaggersTriggeredEffect` (`RepeatAction` / `May` / `RepeatTargetBinding.DifferentTarget`, fed by `PerformedBasicAction`).
- Respawn on board absence (`HeroRespawn`); the Handicap returning to the box (`MatchConfiguration.CreateHand`).

**New contracts,** each owned by one block:

| Need | Faces | Block |
| --- | --- | --- |
| A hero with several bodies: a body identity on occupancy rows and board occupants, a per-hero body supply (RZ-Q1), and the unique-hero rules relaxed only for a hero that declares bodies. Every `Single`/`SingleOrDefault` over a `HeroId` answers a body; an attack targets a body; Defense is measured from the attacked body; one Defeat removes every body (RZ-Q3) | Stunt Doubles; every enemy Attack on him | 9.21a |
| The acting body: one body per performed action, so the actor-space reads above answer it unchanged. A decision "which of you acts", its projection, wire and bot answer (RZ-Q4) | every face | 9.21b |
| How every other hero's text reads a many-bodied hero: hero-counting selections, "each"/"up to N heroes"/"different hero", presence checks, Fast Travel (RZ-Q2) | the dealt roster | 9.21c |
| Spawn a body from the supply into empty spaces in radius (p.19 Spawn, no reclaim) | Stunt Doubles, Stunted Doubles | 9.21d |
| Remove bodies without Defeat; all removed means off the board, not defeated (RZ-Q6, RZ-Q7) | Phantom Strike, Hit and Gone, Into Thin Air, Crowd Control (Skill) | 9.21e |
| Selections over his own bodies ("other you", "another one of you", "this one of you") feeding direct Move; the retreat by the realised push distance (RZ-Q5, RZ-Q8-RZ-Q10) | Alleyoop, Group Performance, Team Spirit, Tightrope, High Wire, Wire Dancers, Magic Trick, Aaaand it's gone!, Theatrics | 9.21f |
| A count of other bodies in radius of the defending or acting body as a Defense contribution and a retrieve count | Crowd Control (Defense), Rummage, Ransack | 9.21g |
| A repeat performed by a different body (the acting body switched for the repeated instance) with a different target (RZ-Q11, RZ-Q12) | Twin Strike, Spectacle | 9.21h |

**Expected schema and wire moves** (each block records the moves it actually made):
- **9.21a:** snapshot v48 -> v49 (hero occupancy rows gain a body ordinal, written in (hero, body) order, plus the per-hero supply). Wire `/25` -> `/26` (`NetworkHeroRow` gains the body; the attack target and Defeat's removed bodies name bodies). Milestone 9.11's completeness checks must see every new field carried.
- **9.21b:** the acting-body decision and the current acting body, probably v50 and `/27` unless 9.21a lands them.
- **9.21e:** a "hero left the board without Defeat" fact on the match log, if the event channel cannot already say it.
- Other blocks are expected to move neither, except where a new pending choice must cross the wire.

**What 9.21a built** (the many-bodies model; no Razzle content registered):
- **Body identity.** `HeroOccupancy` carries `Body` (ordinal from 1; `HeroOccupancy.FirstBody`). The two-argument constructor is body 1, so every single-bodied hero's occupancy, equality and ordering are unchanged. `HeroBodySupply` (hero, bodies) declares a supply (RZ-Q1: four for Razzle). `AdjacentMoveAuthority` relaxes the unique-hero guard only for a hero with a declared supply above one, and refuses a body above the supply or the same body twice. `AttackTarget` carries `HeroBody` (`AttackTarget.Hero(id)` is body 1; `HeroBodyOf(id, body)`), and `IsSameUnit` compares at hero grain for "different target" exclusions (RZ-Q2). `DirectorCommand.HeroBody` is 0 for an answer naming the hero alone; the session accepts that only when one body of the hero is a legal target and otherwise refuses it ("must name the body"), and the decision contract names the body only where two bodies are legal, so every existing command is byte- and equality-identical.
- **The acting body.** `TryGetHeroCoordinate` answers the hero's only body, or the acting body when several stand. Until 9.21b's pick it is body 1; with several bodies and no body 1 the read throws a named `InvalidOperationException`. It is not persisted (9.21b adds the pick and its state). The explicit per-body reads are `TryGetHeroBodyCoordinate`, `GetHeroBodies` and `IsHeroOnBoard`. `TryGetSoleHeroCoordinate(hero, reading)` refuses loudly for a many-bodied hero where no RZ reading settles the body.
- **Staging seam.** `InitialScenarioDescription` takes optional `HeroBodySupply` rows beside its hero occupancies, so a staged two-body board restarts, rewinds and replays as it began (`HeroBodiesBoard` in `HeroBodiesTests`).

The one-space-per-hero sites (lesson 20), each with its treatment:

| Treatment | Sites |
| --- | --- |
| Presence of any body (`IsHeroOnBoard`) | `HeroRespawnRules.CreateChoice`; `TurnResolutionAuthority` (actor defeated before its action); `ClearActionQuery` presence; `RuleModificationQuery`, `AfterMarkerGiven`, `AfterPush`, `AfterTokensPlaced`, `BeforeAction` (three) and `UltimateTriggeredEffectSource` Ultimate/controller/defender presence; `ResolvedCardDiscardTriggerQuery`; Brynn's split-attack target presence; `RoundResolutionAuthority` counts-as-minion hero in the Battle Zone (any body there) |
| The attacked body (`TryGetHeroBodyCoordinate`) | `TurnResolutionAuthority` and `AttackDefenseCardActionStrategyBackend.FinishAfterDefenderRemovedBeforeDefense` (target removed before the Defense); the backend's `TargetSpaceOf`; the Defeat's p.8 origin; `DefenseValueQuery` (the hero-only overloads refuse a many-bodied defender, the body overloads measure p.14's minion modifiers from the attacked body); `MatchSession` Defense and Attack-target previews |
| Every body (body-set) | `AttackTargetQuery.FindLegalEnemyHeroBodies` (a target per body in reach; `FindLegalEnemyHeroes` names each hero once); `ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles` (any body); `AfterTokensPlacedTriggerQuery` adjacent enemy heroes; `CommitValidatedHeroRemoval` (a Defeat removes every body, RZ-Q3; `HeroDefeatResolution.RemovedBodies` replaces `RemovedHero`) |
| Counted once per hero | `UnitPresenceQuery` (both), `BattleZoneQuery` counting heroes, `RosterSharedVocabulary` presence (each body measured from its own space), `RuleModificationQuery` aura targets, adjacent-enemy count and token-adjacent heroes, `SyntheticTargetingCapabilities` adjacent heroes, `SyntheticBorrowedActionCapabilities` friendly heroes |
| The acting body (unchanged call) | the synthetic component, obstacle and token capabilities' `context.ActorId` reads; `CardActionResolutionAuthority` held offer; `ClearActionQuery` actor space; `OwnedTokenReachQuery`; `RuleModificationQuery` acting object; `BeforeActionTriggerQuery` actor; the attacker in `AttackTargetQuery`; `FastTravelActionQuery`; `MovementSession`; the backend's `ActorSpaceOf`; `TryCommitRoute` and Fast Travel commits. `TryCommit` moves the body standing on the command's origin |
| Refused loudly (`TryGetSoleHeroCoordinate`) | an Active effect's radius anchor (`RuleModificationQuery` twice, `BeforeActionTriggerQuery`), `MinionDefeatBountyQuery`'s controller, and the defender a Defense card moves (`CardActionResolutionAuthority`, twice) |
| Respawn | `CommitValidatedHeroRespawn` refuses while any body stands and places body 1 |

Moves made: snapshot **v48 -> v49** (hero rows carry the body ordinal in (hero, body) order; the board ends with the declared supplies; a persisted hero Attack target names its body; v48 rejected) and wire **`/25` -> `/26`** (`NetworkHeroRow.Body`, `NetworkAttackTargetRow.HeroBody`, the Attack-target Defense preview row's `HeroBody`, and the Attack-target answer's body).

Left to later blocks: a `BoardObjectReference` names a hero, not a body, so a card-text selection or displacement of a many-bodied hero reads and moves the acting body (9.21c); a text-bound Attack target is body 1 (9.21c); Brynn's "target adjacent to three obstacles" reads any body (9.21c); match-log rows and events name the hero, not the body, and Presentation keeps one view per hero, with two `Single` reads over the projection's heroes in `UnityMatchFeatureViews` that would throw on two bodies (9.21i).

**What 9.21b built** (the acting-body pick, RZ-Q4 (A); no Razzle content registered):
- **The decision.** A twelfth pending family, `PendingDecisionFamilies.ActingBody` (`acting-body`). `PendingActingBodyChoice` (hero, card, the standing bodies in body order, one `ActingBodyCommand` per body) is derived by `TurnResolutionAuthority.PendingActingBodyChoice` through `ActingBodyRules.CreateChoice`, like the respawn choice: it stands while the selected actor declares a supply above one, more than one of his bodies stands, and no acting body is settled. It precedes every action of the card: `GetCommonRejection` refuses every start and Hold (`ActingBodyChoicePending`), and the projection offers no action, Hold, Fast Travel or Clear until it is answered. The answer is `DirectorCommand.ChooseActingBody` (kind 25; hero, card, `HeroBody`), dispatched to `TryAnswerActingBody`, which emits `ActingBodyChosen` with the body's space.
- **The acting body.** `AdjacentMoveAuthority` holds the settled acting body per many-bodied hero (`TryGetActingBody`, `ActingBodies`); `ActingBodyIndex`, and so `TryGetHeroCoordinate` and every read in the 9.21a "acting body" row (before/after text, `MovementSession`, `FastTravelActionQuery` and its commit, `ClearActionQuery`, the backend's actor space, the attacker in `AttackTargetQuery`, the synthetic capabilities' actor), answers it while it stands. Each time the turn selects an actor the settled bodies are cleared; a many-bodied actor with one body standing has that body settled at once (and again after a respawn), so a body added during the action cannot change who acts. With none settled (another hero's turn) the 9.21a fallback holds: body 1, or a named refusal. A Defeat clears his settled body. A Defense never asks: it is not the selected actor's decision, and 9.21a's attacked-body reads already answer it.
- **Seat and delegation.** The owner is the selected hero (`OwnerOfStandingDecision`); the family is on `SeatProjectionFactory.DelegatedDecisionFamilies`, because which body performs the action is how it is performed (HA-Q3) and the start commands already go to the deciding seat. It is not a hero-claiming command kind: the hero it names is the subject, as `StartAction`'s is. The typed choice is on `IMatchReadModel` for the owning seat only; a network client re-derives it from that seat's `DecisionContract` (each answer names the hero, card and body) and the hero rows, so no projection row carries it. The status bar names it: the owner reads "YOUR ACTING BODY — CHOOSE WHICH OF YOU ACTS", every other seat the owning hero and "CHOOSING WHICH OF THEM ACTS".
- **Wire, snapshot, bot.** Protocol **`/26` -> `/27`**: the new command kind in `NetworkCommandCodec`; no projection row moved. Snapshot **v49 -> v50**: each hero body supply row ends with the settled acting body (0 for none), carried by `BoardRestoreState.ActingBodies`; no fixture declares a supply, so the sixteen captures kept their lengths and moved only by the version. `BotPolicy` scores each body under the Geometry family (`ActingBodyDelta`): nearest-enemy proximity from that body plus the Attack start's weight when an enemy unit is adjacent; ties go to the seeded die.
- **Evidence.** `HeroActingBodyTests` on `HeroBodiesBoard` (`CreateSessionAtTheActingBodyPick`, `bodyTwoAtSpawn`); the family joins the conformance inventory (lifecycle: Undo, restart, replay, restore), the transport ownership walk, the codec specimens and the completeness walks. Its visible surface in the conformance check is **parked for 9.21i** (`ParkedSurfaceFamilies`).

Left to later blocks after 9.21b: the standee pick itself and the acting cue (9.21i; the conformance surface arm is parked until then); a Stunt Doubles spawn during the action must keep the settled body (9.21d; the settle-at-selection is its guard); a removal of the acting body mid-action (Phantom Strike, RZ-Q6) leaves the settled body not standing and the read falls back to 9.21a's rule (9.21e decides what the rest of that action reads); another hero's borrowed or delegated action performed BY Razzle off his own turn has no pick (9.21c); `BotEvaluation` still reads a hero's first row as its position.
**What 9.21c built** (every other hero reads a many-bodied hero, RZ-Q2 (A); no Razzle content registered):
- **A space names the body on it.** `BoardObjectReference.HeroBody` (0 = the hero, no body named); `ForHeroBody(hero, body)`. Equality and the hash stay at hero grain, so "another unit", "not the actor", a Swap with oneself and every count still see him once. `TryGetObjectAt` names the body standing on a many-bodied hero's space and `TryGetObjectCoordinate` reads a named body there, so a push, swap, move, place or removal of a space a selection picked acts on that body; a reference naming only the hero answers the acting (default) body as before. Never persisted or projected: a repeat's bound piece is captured at hero grain (the snapshot persists the identity, not a body) and an effect-choice preview's occupant is normalized to the hero (its candidate space names the body), both declared in 9.11's completeness checks.
- **Selections.** A selection that reads the picked unit's space (it declares a coordinate output) offers a many-bodied hero once per body in reach, each as the space it stands on, and refuses the bare hero; a selection that only names heroes offers him once. Count-once is hero-grain `Distinct` on the chosen units, so two of his bodies are never two of "up to N units" or a pair. Shared as `RosterPresenceQuery.OfferUnits`/`ResolveUnit` (`FindUnitsWithin(..., everyBody: true)` for a selection; the presence form still counts him once and now names a body in reach), used by the printed-icon family's hero and unit arms, the roster's `choose-target@2`, `choose-target-set@1`, `choose-target-trio@1`, `choose-through-target-line@1`, `choose-traversed-unit@1`, `choose-swap-subject@1`, the unit pairs and Hanu's friendly pair. Hero-once dedupe: `behind-target-line@1` (Dangerous Current under W3), the anchored selections (Min's grenades), Ignatia's magma/minion discard, Snorri's and Ignatia's Attack-target selections, Wuk's trampled heroes.
- **The attacked body.** `EnclosingAttackFacts.HeroTargetBody` / `HeroTargetReference` (derived from the persisted Attack target), read by card text about "the target" (adjacency, distance, the attacked space, Noble Blade's "adjacent to the target", Whisper's forced Movement of the defender - the 9.21a `TryGetSoleHeroCoordinate` site is now the attacked body, RZ-Q4) and by `ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(..., body)` for Brynn's split attacks and every Attack-target contribution (closing 9.21a's "reads any body" item).
- **"A different target" in card text** (`RepeatTargetBinding.DifferentSelectionSubject` over spaces) removes every space the excluded hero stands on; the producing repeat's subject narrowing reads the unit on a space.
- **Restore fidelity.** A restored pending selection is re-asked with its node's declared outputs, as live execution asks it (found by the many-bodied soak: without it a restored space-reading selection offered the hero instead of his bodies).
- **Evidence.** `HeroBodiesRosterSweepTests`: the content-derived sweep classifying every handler the dealt faces compose (NoHeroOrUnit, ActingBody, AttackedBody, BodyAsSpace, HeroOnce, PresenceEveryBody, HeroLevelResult, SpaceNamesBody, OpenBodyNotChosen) and staged two-body cases per shape; `ExcludedSubjectRepeatTests` (different target over spaces); `RandomLineupSoakTests.AManyBodiedLineup_...` (line-up 0 with Sabina on four spaces). No schema or wire move (v50, `/27`).

Left to later blocks after 9.21c: a card-text Attack target is bound as the hero, so `set-attack-target@1` fed by Ignatia's range selections or Snorri's rune targets attacks body 1 (a body the text did not pick); Brynn's tracking swap subject reads the first qualifying body in radius, not the player's; Hanu's journey partner is re-read at the partner's acting (default) body because the Active effect records the hero; a radius anchored on a many-bodied CONTROLLER (an aura "in radius" of "you" - Arien's Deluge, Brogan's bolsters, Garrus's howl, Wasp's barrier, Trinkets' disruptors, Xargatha's stares), `MinionDefeatBountyQuery`'s controller and `BeforeActionTriggerQuery`'s anchor remain 9.21a's named refusals, reached only by a many-bodied hero's OWN faces (Razzle prints none; a borrowed action performed by Razzle could reach one), and the session reports them as an engine fault naming the reading; a card-text "on the same target" repeat of a many-bodied hero is supplied his acting (default) body's space, because the bound piece is captured at hero grain (9.21h owns his repeats, RZ-Q11); another hero's borrowed or delegated action performed BY Razzle still has no acting-body pick.

## Presentation needs

- Several standees for one hero, all drawn from element 8596252's team face. A quiet per-body mark keeps them apart in the log and the pick. Hover, the target cue (8.62) and the Defeat fade apply per body.
- The pick: when more than one body stands and he acts, the player clicks the standee that acts before the action choice (RZ-Q4). Bots answer the same decision.
- The acting cue: the acting body is marked for the whole action and moves to the other body on a "different you" repeat.
- The rail, overview and victory screen show one hero. `HeroOverviewsUnityView` must not list a hero once per body.
- Every attack or selection cue naming Razzle names a body.

## Open questions

Recorded by 9.21. **All twelve settled by the user on 2026-09-30, one by one, each as the recommended reading:** RZ-Q1 (a) four bodies in all; RZ-Q2 (A) one hero on several spaces for everyone else; RZ-Q3 (A) an Attack targets one body and a Defeat removes all; RZ-Q4 (A) the acting body is picked when the card resolves and stays fixed for the action, a Defense by the attacked body; RZ-Q5 (A) his other bodies are never a unit or friendly hero for his own text; RZ-Q6 (A) "one of you" may be the acting body, removal is never a Defeat, Into Thin Air leaves him off the board to respawn normally; RZ-Q7 (A) Crowd Control as a Skill with no other body is greyed; RZ-Q8 (A) "Move one other you" reaches any body; RZ-Q9 (A) Tightrope applies after a 0-space Movement and never after a Fast Travel; RZ-Q10 (A) Magic Trick retreats exactly the pushed distance, straight back, stopping if blocked; RZ-Q11 (A) Twin Strike repeats the whole action from another body, spawn included; RZ-Q12 (A) Spectacle's repeat measures Range from the second body. Each entry below keeps its candidate readings for the record.

- **RZ-Q1 - how many bodies can exist? (blocks 9.21a supply, 9.21d)** Printed: "Spawn up to 3 more of you in radius". p.19 Spawn takes from the supply only. The corpus has one standee element and no counts (RZ-03).
  - (a) Four bodies in all (the hero plus three), so a spawn with four standing does nothing.
  - (b) No limit beyond "up to 3" per spawn.
  - (c) The physical box's count, if it differs.
  - *Recommended:* (a), confirmed against the user's physical game.
- **RZ-Q2 - how do other heroes' actions see him? (blocks 9.21c)** Printed: "each of you is the same hero, except when actions are performed".
  - (A) The exception is his own actions. For anyone else he is one hero on several spaces. A selection picks a body as the space it reads, but a step that counts or names heroes ("each enemy hero in radius discards", "up to 2 enemy heroes", "a different hero", a repeat that "cannot target the same unit") counts him once. A hero-level result (discard, coins taken, marker, Defeat) happens once. Presence checks and obstacles see every body.
  - (B) Every body is a separate unit whenever any action is performed, so "each enemy hero in radius discards" makes him discard once per body in radius.
  - *Recommended:* (A).
- **RZ-Q3 - attacking a body and his Defeat. (blocks 9.21a, 9.21g)** Printed: "If defeated, remove all of you".
  - (A) An Attack targets one body. p.14's minion modifiers and Crowd Control's "each other you in radius" are measured from that body, and "other you" excludes it. A failed defense is one Defeat of the hero (coins by level, Life by lowest Tier, assists once) and every body is removed.
  - (B) Only the attacked body is removed, and the hero is defeated when the last body falls.
  - *Recommended:* (A); the printed sentence says all.
- **RZ-Q4 - choosing the acting body. (blocks 9.21b)** Printed: "except when actions are performed"; Magic Trick's "this one of you".
  - (A) With more than one body standing, he picks the body when his card resolves, before choosing the action. It is fixed through before/after text, secondary Movement, Fast Travel, Clear and Hold. A Defense is performed by the attacked body.
  - (B) Body and action are chosen together; the offer is the union over bodies.
  - *Recommended:* (A).
- **RZ-Q5 - are his other bodies ever "a unit", "a hero" or "a friendly hero" for his own text? (blocks 9.21f)** Alleyoop: "Swap places with a friendly hero in range"; Magic Trick: "Push a unit adjacent to you".
  - (A) No: p.17 "Friendly ... never includes you", p.10 "it will not affect you" unless it says so, and every body is you.
  - (B) Yes: a body is a separate unit while actions are performed, so Alleyoop could swap two bodies.
  - *Recommended:* (A). The allies' side (an ally's "friendly hero" choosing a Razzle body) follows RZ-Q2.
- **RZ-Q6 - removal faces. (blocks 9.21e)** Printed: Phantom Strike "you may remove one of you"; Hit and Gone "except the last one of you"; Into Thin Air "including all of you".
  - (A) "One of you" may be the acting body. Removal is never a Defeat. After Into Thin Air removes all, he is off the board, not defeated, and respawns by p.7 at his next action.
  - (B) Only other bodies may be removed.
  - *Recommended:* (A); the faces print "one of you", not "another".
- **RZ-Q7 - Crowd Control as a Skill with no other body. (blocks 9.21e)** Printed: "remove all other you in play".
  - (A) A mandatory step with nothing to act on, so it is greyed (Rule 14; Hold stays available).
  - (B) Offered as a do-nothing play.
  - *Recommended:* (A); RULING 53 covers only a printed "If".
- **RZ-Q8 - reach of "Move one other you". (blocks 9.21f)** Alleyoop family.
  - (A) Any other body anywhere; the Range bounds only the swap.
  - (B) Only a body in range.
  - With no other body the swap still resolves and the move step is not performed (p.10); the offer is judged on the swap.
  - *Recommended:* (A).
- **RZ-Q9 - Tightrope family timing. (blocks 9.21f)** Printed: "After you move you may move another one of you".
  - (A) Applies after the Movement even when he moved 0 (p.11), never after a Fast Travel replacing it (p.11), and reaches any other body.
  - (B) Needs at least one space moved.
  - *Recommended:* (A).
- **RZ-Q10 - Magic Trick's retreat. (blocks 9.21f)** Printed: "for every space the target moved, move this one of you one space in the opposite direction".
  - (A) The acting body moves straight away from the pushed target, along the push's line, exactly as many spaces as the target moved (0 after a 0 push). Blocked, it stops (p.11).
  - (B) The body moves as far as possible up to that count.
  - *Recommended:* (A).
- **RZ-Q11 - Twin Strike's repeat. (blocks 9.21h)** Printed: "a different you may repeat it once, targeting a different unit".
  - (A) After Stunt Doubles' Basic Attack, he may choose another body. That body performs the whole action again (p.10 "the entire action"), adjacent to itself, against a different unit, including the After-the-attack spawn (bounded by RZ-Q1).
  - (B) Only the Attack repeats, without the spawn.
  - *Recommended:* (A).
- **RZ-Q12 - Spectacle's repeat. (blocks 9.21h)** Printed: "May repeat once by a different one of you, targeting a different minion".
  - (A) The repeat's Range 2 is measured from the second body, and the minion differs from the first swap's.
  - (B) Range stays measured from the first body.
  - *Recommended:* (A).
