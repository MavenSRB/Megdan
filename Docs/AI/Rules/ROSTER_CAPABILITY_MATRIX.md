# Roster Capability Matrix

<!-- operative-claim:F-DOC-024 status:CurrentDemonstrated -->

Canonical planning map for milestone 8.19. Return to [CARD_COVERAGE.md](CARD_COVERAGE.md). Source truth and
exact face text remain in the hero shards; this file owns only capability classification, effect-node
contracts, dependency order, and source-linked scenario IDs for every hero added after Wasp - the five
milestone 8.19 planned, and Dodger, Misa, Silverarrow, Ursafar, Whisper, Tali, Swift and Cutter, added at
milestones 8.35z34, 8.35z38, 8.35z46, 8.35z50, 8.35z53, 8.37, 8.38e and 8.39i from outside that line-up.
Whisper is the first from the Devoted Hero pack rather than from the base box and Tali the second; with Tali
the whole six-hero content wave is dealt, and Swift and Cutter are the two heroes added after that arc
closed - both on the 2026-09-11 user direction that re-opened the roster.

## Reading the matrix

Every face also reuses the implemented immutable card/action definition, typed printed values, contextual
offer, lifecycle, item, upgrade, Hold, ordinary Secondary, and source-verification contracts. Those base
nodes have no effect-frame mutation and are not repeated below. `R` means unchanged core/Wasp reuse, `F`
means a reusable refinement of an implemented capability, `N` means a new reusable primitive, and `U`
means a controlled isolated pure handler. No row authorizes implementation.

Each node entry has the form `node -> capability@version (bindings; policy; result/atomicity;
checkpoints) [scenario]`. `A`, `S`, `T`, `C`, and `X` bind the actor, source card, chosen target, chooser, and
enclosing action facts. `Out(n)` binds a typed output from node `n`; literals are definition data. `M`
means an impossible mandatory step preserves earlier applied nodes and stops the remainder, `May` may be
declined, and `IfAble` silently skips only when infeasible. `One`, `Ordered`, and `Batch` name atomicity.
Every chooser-owned input is an authoritative serializable choice, rederived on answer and restore;
rejection and staleness mutate nothing. Each listed scenario family must contain positive, negative,
boundary, restore/replay, ordered-event, and applicable immunity/Counts-As/obstacle cases. Attack rows also
include the enclosing combat outcome arms; Active effects include creation, applicability, cancellation,
expiry, and p.17 ordering; repeated actions include decline, bound, changed-board, and target-exclusion
arms. These inherited cases are part of every bracketed scenario even when the short ID omits them.

Shared capability identities used below are stable implemented contracts unless a row explicitly says otherwise. Existing IDs retain their current
revision: `synthetic-direct-move@1`, `synthetic-object-push@3`, `synthetic-place@2`,
`synthetic-swap@1`, `synthetic-swap-actor@1`, `synthetic-remove@1`, `synthetic-defeat-hero@1`,
`synthetic-defeat-minion@1`, `synthetic-discard-chosen-card@1/@2`, `synthetic-give-marker@1`,
`synthetic-range-target@1`, `synthetic-radius-target@1`, `synthetic-choose-target@1`, and
`repeat-action@1`. New or refined identities must be introduced by an approved dependency slice and registered before verified content can use them.

Current implementation status: dependency steps 1-4a and all six hero-composition/gate rows are complete; milestone 8.35z34 added a seventh - Dodger, composed at 8.35z28 and dealt, registered and gated at 8.35z34 - milestone 8.35z38 an EIGHTH on the same terms, Misa, composed at 8.35z29 and dealt at 8.35z38 - and milestone 8.35z46 a NINTH, Silverarrow, composed at 8.35z30 and dealt at 8.35z46. Milestone 8.35z50 added a TENTH, Ursafar, composed at 8.35z31 and dealt at 8.35z50, milestone 8.35z53 an ELEVENTH, Whisper, composed at 8.35z33 and dealt at 8.35z53, and milestone 8.37 a TWELFTH, Tali, whose Green line was composed at 8.35z32 and whose other twelve composable faces were composed in her own dealing. **Ursafar's own capability section is still owed here**: his dealing added the hero-composition/gate row and the registrations but never wrote his section into this file, which is recorded rather than quietly filled because writing it is content authoring against his printed set rather than a restatement of this milestone's work. Every capability a configured face names is registered in the default production catalog; deliberately synthetic-only fixtures remain bounded and are not presented as roster consumers. Snapshot schema is v42 (milestone 8.39g took it there; this line read v40 until 8.39i corrected it) and pins the complete registered contract set used by restore, which is why a dealing milestone moves every corpus digest and says why. **Every composed hero is now dealt** as of milestone 8.39i; milestone 8.37a composed the one FACE that remained of the arc - Blizzard's End-of-turn repeat - milestone 8.38e dealt Swift, the THIRTEENTH hero and the first after it, whose three contracts had been held out of the registered set by milestone 7.9's gate since 8.38a, and milestone 8.39i dealt Cutter, the FOURTEENTH, whose two - `synthetic-range-obstacle-target@1` and `claim-solo-victory@1` - had been held out since 8.39b and 8.39g1. So nothing in the build carries an engine deferral, and the only parked faces are the fourteen p.20 Handicap cards p.6 returns to the box at setup. Milestone 8.35z38 recorded the one way that rule reads differently for a SHARED declaration: `choose-line-destination@1` is Misa's and Silverarrow's both, so dealing Misa registered it and Silverarrow's phase-one claim to it expired - a shared contract is deferred only until the FIRST of its sharers is dealt. Milestone 8.35z46 moved her own remaining contract, `fast-travel@1`, across on the ordinary half of the rule: a card a configured match now deals starts it.

**Swift the Sharpshooter is inventoried at milestone 8.38 and her capability contract map lives in [HERO_SWIFT.md](HERO_SWIFT.md#capability-contract-map) rather than here**, in this file's own notation and readable beside these sections. The reason is a threshold rather than a preference: this shard sits close enough to its 9,000-word warning line that a thirteenth hero's section would cross it, and an ownership review is a milestone of its own rather than a side effect of an inventory pass. Fourteen of her nineteen faces name contracts already registered here; what she is owed falls on three lines that milestones 8.38a-8.38c build.

**The block's own prerequisite claim needs qualifying, and this is now three heroes running:** each of
8.20, 8.20a and 8.21 found printed sentences needing vocabulary no 8.19 slice built - a restriction
carving out the cards it leaves alone, an immunity excepting the one hero its occurrence bound, a cap on
how far one Movement action may go, an Active-effect scope covering the controller as well as the units
around them, a direct Move that names its destination instead of narrowing a set, and a target relation
admitting an enemy unit *or* a token. **A hero milestone should read its own rows against what is
registered rather than against step 4a's completion**, and should budget for minting.

## Arien

Source: [HERO_ARIEN.md](HERO_ARIEN.md). Arien owns no token or marker.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Noble Blade | F, N | `noble-target -> attack-target@2 (A,S; adjacent enemy unit; M/One; AttackStart)`; `noble-other -> choose-target@2 (A,C,T=Out(noble-target), another unit adjacent to T excluding A; May/One; BeforeAttack)`; `noble-move -> synthetic-direct-move@1 (actor=A,target=Out(noble-other),distance=1 exact; May/One; MovementMutation, BeforeAttackComplete)` `[arien/noble-blade]` |
| Ceremonial Blade | F, N | As Noble Blade, but `attack-target@2` admits an adjacent enemy hero and `choose-target@2` admits a minion adjacent to that hero `[arien/ceremonial-blade]`. |
| Spell Break | R, F | `spell-create -> create-active-effect@1 (S,A,ThisTurn,radius=printed,opposing heroes; M/One; SkillEffect)`; source declares `restrict-action@2 (affected=heroes opposing source in live radius, activity=Skill, exception=GoldCard; query-time)` `[arien/spell-break]`. |
| Dangerous Current / Raging Stream / Violent Torrent | R, N | `current-line -> choose-target@2 (A,C, spaces directly behind X.target, length=2/3/5, enemy hero, max=1; UpTo/One; BeforeAttack)`; `current-discard -> synthetic-discard-chosen-card@2 (chooser=Out(current-line), any eligible hand card; M/One)`; `current-defeat -> synthetic-defeat-hero@1 (when discard reported none; M/One; staged Defeat)`; Violent Torrent adds `repeat-action@2 (max=1,different unit target; May/One; ActionEnded)` `[arien/current-{2,3,5}]`. `behind-target-line@1` is a new source/target-oriented geometry query. |
| Rogue Wave / Tidal Blast | R, F | `wave-target -> attack-target@1 (in printed Range)`; `wave-push-target -> synthetic-choose-target@1 (enemy unit adjacent to A; May/One; AfterAttack)`; `wave-push -> synthetic-object-push@3 (source=A,target=Out(wave-push-target),distance=0..2/0..3; May/One; displacement then correction/Lane-Push facts)` `[arien/wave-{2,3}]`. The refinement permits pausing After-Attack text. |
| Aspiring Duelist | F | `duelist-block -> defense-block@1`; `duelist-suppress -> minion-defense-contribution-policy@2 (X.defense calculation, suppress every friendly/enemy minion contribution; M/Batch; DefenseValue)` `[arien/aspiring-duelist]`. |
| Expert Duelist / Master Duelist | R, F | Aspiring Duelist nodes plus `duelist-create -> create-active-effect@1 (ThisTurn/ThisRound; M/One; DefenseText)` declaring `immunity@2 (affected=A, activity=enemy Attack, except attacker=X.actor)` `[arien/expert-master-duelist]`. |
| Slippery Ground / Deluge | R, F | `ground-create -> create-active-effect@1 (ThisTurn, adjacent/radius opposing heroes; M/One)` declaring `restrict-fast-travel@1` and `limit-movement-action@1 (maximum=1, action-start snapshot)` `[arien/slippery-deluge]`. |
| Liquid Leap / Magical Current | R, F | `leap-destination -> place-destination@2 (actor=A, in printed Range, empty, no Spawn point, not adjacent to empty Spawn point; M/One)`; `leap-place -> synthetic-place@2 (piece=A,destination=Out(leap-destination); M/One; Place, occupancy)` `[arien/liquid-magical-leap]`. |
| Stranger Tide | R | Same as Leap without the adjacency exclusion `[arien/stranger-tide]`. |
| Arcane Whirlpool | R | `whirlpool-target -> synthetic-range-target@1 (enemy minion, printed Range)`; `whirlpool-swap -> synthetic-swap-actor@1 (A,Out(target); M/Batch; Swap)` `[arien/arcane-whirlpool]`. |
| Ebb and Flow | R, F | Arcane Whirlpool nodes plus `was-adjacent@1 (pre-swap A/T positions -> bool)` and `repeat-action@2 (if true,max=1; May/One; ActionEnded)` `[arien/ebb-flow]`. |
| Living Tsunami | N | Ultimate continuous source declares `before-action-trigger@1 (controller=A, activity=Attack, once per turn)`; `tsunami-move -> synthetic-direct-move@1 (A,distance=1 exact; May/One; before ActionStart)` `[arien/living-tsunami]`. The trigger pauses before the Attack transaction and resumes the same offer/version rather than starting a second action. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/arien, team slot, deck, profile/dashboard references; validation-only)` and ordinary participant/setup bindings `[arien/components]`. No rules component supply. |

## Brogan

Source: [HERO_BROGAN.md](HERO_BROGAN.md). Brogan owns no token or marker.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Onslaught / Leap Forward | F, N | `onslaught-target -> attack-target@2 (adjacent enemy unit/hero)`; `former-space -> attacked-target-space@1 (X.target space captured before consequences)`; `onslaught-move -> synthetic-direct-move@1 (A,destination=Out(former-space), exact if empty after consequences; M/One; AfterAttack)` `[brogan/onslaught-leap]`. |
| Bulwark | R, F, N | `bulwark-retrieve -> retrieve-card@1 (A, one owned discarded card; May/One)`; `bulwark-create -> create-active-effect@1 (ThisTurn, A and friendly units in live radius; M/One)` declaring `immunity@2 (source relation=enemy hero, activities=DirectMove,ObjectPush,Swap,Place)` `[brogan/bulwark]`. |
| Mad Dash / Bullrush / Furious Charge | F, N | `charge-destination-target -> choose-charge-line@1 (A, exact distance 2 / choice 2..3 / choice 2..4, straight line, empty destination adjacent to enemy unit, binds destination+target; M/One; BeforeAttack)`; `charge-move -> synthetic-direct-move@1 (A,Out(destination), exact; M/One)`; `charge-retarget -> set-attack-target@1 (X.target=Out(target); M/One; before AttackTotal)` `[brogan/charge-{2,3,4}]`. Candidate feasibility proves both move and target before offering. |
| Throwing Axe | N | `axe-mode -> choose@1 (C, adjacent-target branch or discard-for-range branch; M/One)`; adjacent arm uses `attack-target@2`; range arm uses `discard-owned-card@1 (A,May/One)` then `set-attack-target@1 (enemy unit in printed Range; only when discarded)` `[brogan/throwing-axe]`. |
| Throwing Spear | N | Same modal target composition, but discard is optional and the range branch is enabled when A has any card in discard after the optional step `[brogan/throwing-spear]`. |
| Brutal Jab / Mighty Punch / Savage Kick | R, F | Ordered `synthetic-direct-move@1 (A, distance exact optional 1 / up-to 1 / up-to 2; May/One)` then `synthetic-choose-target@1 (adjacent enemy unit or token)` then `synthetic-object-push@3 (distance 0..1 / 0..2 / 0..2; M/One; correction/Lane-Push facts)` `[brogan/jab-punch-kick]`. |
| Shield Bash / Counterattack | R, N | `counter-target -> choose-target@2 (enemy hero adjacent to A whose exact current-turn slot names a card with PrintedPrimary Attack; M/One)`; `counter-discard -> synthetic-discard-chosen-card@1/@2 (IfAble / escalation)`; Counterattack adds `synthetic-defeat-hero@1` on no-card outcome `[brogan/shield-counter]`. |
| Shield / Bolster / Fortify | N | `guard-create -> create-active-effect@1 (ThisRound, friendly minions in live radius; M/One)` declaring `before-removal-prevention@1 (trigger=Defeated, chooser=A, cost=discard owned Silver/Silver/Basic card, May; keep Defeat/reward, report RemovalPrevented; One per outcome; BeforeRemovalDecision)` `[brogan/shield-bolster-fortify]`. Candidate/card cost is rederived at the prevention pause. |
| War Drummer / Master Skald | R, N | `skald-target -> synthetic-range-hero-target@1 (friendly hero excluding A, printed Range; M/One)`; `hero-defeated-this-round@1 (round outcome history -> bool)`; `gain-resource@1 (recipient=Out(target), coin=1/2 or 3/4, exact conditional amount; M/One; ResourceMutation)` `[brogan/war-master-skald]`. |
| One Man Army | U | Ultimate continuous source declares isolated `one-man-army-minion-battle@1`: during Minion Battle Brogan supplies one Heavy strength to his team; if he would be removed, atomically keep Brogan on board and emit the ordinary Lane-Push request against his team instead, recording `PushLost` without Defeat/reward/Life effects. The push still resolves through the shared transition. It reads only typed Minion-Battle facts and emits typed transition facts; no hero-name branch `[brogan/one-man-army]`. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/brogan, team slot, deck, profile/dashboard references; validation-only)` `[brogan/components]`. No rules component supply. |

## Sabina

Source: [HERO_SABINA.md](HERO_SABINA.md). S-01 was answered on 2026-08-25 - the player-facing epithet is
`Sabina the Gunslinger` - and it never reached this table: stable content identity uses `sabina` and no
capability branches on either epithet.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Point Blank Shot / Bang | R, F | `shot-target -> attack-target@1/@2 (Range unit / adjacent hero)`; Point Blank uses `target-is-adjacent@1` at step 7 from current occupancy; `shot-push -> synthetic-object-push@3 (target=X.target,distance=1 exact; M/One; AfterAttack, only if condition true)` `[sabina/point-blank-bang]`. A removed Point Blank target is no longer adjacent, while Bang's unconditional Push is infeasible; neither resurrects it. |
| Back to Back | R | `back-target -> synthetic-radius-target@1 (friendly minion)`; `back-swap -> synthetic-swap-actor@1 (A,Out(target); M/Batch)` `[sabina/back-to-back]`. |
| Quickdraw / Gunslinger / Dead Shot | R, N | `target-played-attack-this-turn@1 (X.target, current-turn played-card identity -> bool)` feeding `modify-attack-value@1 (+3/+3/+4, card-text contribution; AttackTotal)` `[sabina/quickdraw-gunslinger-dead-shot]`. |
| Shootout / Bullet Hell | R, N | `shootout-adjacent@1 (pre-consequence A/T -> bool)`; `shootout-targets -> choose-target-set@1 (enemy minions adjacent to A,count=0..1/0..2; UpTo/One choice)`; `shootout-remove -> synthetic-remove@1 (each chosen target, stable chosen order; Ordered; removal decision and immediate Lane Push after each, stop on game end)` `[sabina/shootout-bullet-hell]`. |
| Listen Up / Roger Roger | R, N | `pair -> choose-target-pair@1 (two distinct minions in printed Radius, unordered canonical pair; M/One choice)`; `swap -> synthetic-swap@1 (Out(pair.a),Out(pair.b); M/Batch)` `[sabina/listen-roger]`. |
| Ready and Waiting | F, N | Same pair/swap nodes with `immunity-override@1 (activity=Swap, reason=printed Heavy override)` applied in candidate derivation and immediately before commit `[sabina/ready-waiting]`. |
| Steady Advance / Unwavering Resolve | R, N | `friendly-minion-count@1 (live radius,presence,minimum=2)`; conditional `retrieve-card@1 (A,one discarded owned card; May/One)` then `synthetic-direct-move@1 (A,distance=0..1/0..2; May/One)` `[sabina/steady-unwavering]`. Retrieval decline does not skip Move. |
| Troop Movement / Marching Orders / Path to Victory | R, F | `troop-target -> synthetic-radius-target@1 (friendly minion)`; `troop-move -> synthetic-direct-move@1 (target=Out(target),distance=1 exact,destination must remain in A's live radius; M/One)`; `repeat-action@2 (max=1/1/2; May/One; correction then repeated Skill)` `[sabina/troop-marching-path]`. |
| Close Support / Covering Fire | R, N | `supported-enemy -> choose-target@2 (enemy hero in A radius with friendly-minion adjacency presence; M/One)`; `support-discard -> synthetic-discard-chosen-card@1/@2`; Covering Fire adds `synthetic-defeat-hero@1` on no-card outcome `[sabina/close-covering-support]`. |
| Big Sodding Gun | R, N | Ultimate source declares `modify-value@2 (controller=A, action=Basic Attack, Range +2 and Attack +2)` and `after-push-trigger@1 (source actor=A,target enemy hero,push Applied)`; trigger runs `synthetic-discard-chosen-card@2` then `synthetic-defeat-hero@1` on no-card outcome before later action nodes continue `[sabina/big-sodding-gun]`. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/sabina, neutral stable ID plus unresolved display label; validation-only)` `[sabina/components]`. No rules component supply. |

## Tigerclaw

Source: [HERO_TIGERCLAW.md](HERO_TIGERCLAW.md).

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Blink Strike / Nimble Strike | F, N | `blink-line -> choose-through-target-line@1 (A, exact 2-space straight path through enemy unit/hero, empty endpoint, binds crossed target; M/One)`; `blink-move -> synthetic-direct-move@1 (A,Out(endpoint),exact 2; M/One)`; `blink-retarget -> set-attack-target@1 (Out(crossed target); M/One)` `[tigerclaw/blink-nimble]`. |
| Blend Into Shadows | R, F | `terrain-adjacent@1 (A presence)`; conditional `synthetic-place@2 (A,empty space in Radius; M/One)` then `create-active-effect@1 (NextTurn)` declaring `immunity@2 (A,enemy Attack)` `[tigerclaw/blend-shadows]`. The effect is created only if Place applied. |
| Hit and Run | R, F | Ordinary adjacent Attack plus pausable After-Attack `synthetic-direct-move@1 (A,distance=1 exact; May/One)` `[tigerclaw/hit-run]`. |
| Combat Reflexes / Leaping Strike | R, F | Before-Attack `synthetic-direct-move@1 (A,1 exact; May)`, then adjacent `set-attack-target@1`; After-Attack Move is conditional on the first node's Applied/no-relocation result for Combat Reflexes and unconditional May for Leaping Strike `[tigerclaw/combat-leaping]`. |
| Backstab | R, N | `friendly-unit-adjacent-target@1 (presence excluding A)` feeding `modify-attack-value@1 (+2)` `[tigerclaw/backstab]`. |
| Backstab with a Ballista | R, N | Backstab value nodes plus `restrict-defense-offer@1 (defender, origin=PrintedPrimary only)`; ordinary Secondary Defense remains offered `[tigerclaw/backstab-ballista]`. |
| Dodge / Sidestep | R, F | `defense-block@1 (Ranged)`; Sidestep adds defender-anchored `synthetic-direct-move@1 (A,1 exact; May/One; DefenseText)` `[tigerclaw/dodge-sidestep]`. |
| Parry / Riposte | R, F | `defense-block@1 (NonRanged)`; `synthetic-discard-chosen-card@1/@2 (target=X.attacker)`; Riposte adds `synthetic-defeat-hero@1` on no-card outcome `[tigerclaw/parry-riposte]`. |
| Evade | R, F, N | Ranged Block and optional Move as Sidestep, then `retrieve-card@1 (A, owned Basic Skill in Resolved or Discarded state; May/One)` `[tigerclaw/evade]`. |
| Light-Fingered / Pick Pocket / Master Thief | R, N | optional/up-to pre-Move via `synthetic-direct-move@1`; `coin-target -> choose-target@2 (adjacent enemy hero with at least requested coin minimum)`; `transfer-resource@1 (coin,from=Out(target),to=A,amount=1 / choice 1..2 capped by available; M/Batch)`; conditional post-Move exact/up-to 1/1/2 if transfer applied `[tigerclaw/thief-{1,1,2}]`. Transfer plans debit/credit together and cannot mint or go negative. |
| Poisoned Dagger / Poisoned Dart | R, F | `poison-target -> synthetic-range-hero-target@1 (any hero printed Range)`; `poison-give -> synthetic-give-marker@1 (definition=poison-1/poison-2,holder=Out(target); M/One)`; marker source declares `modify-value@2 (Initiative,Attack,Defense all -1/-2, holder-scoped)` `[tigerclaw/poison-{1,2}]`. |
| Cloak and Daggers | N | Ultimate source declares `after-action-trigger@2 (controller=A, completed Basic action, once per action)` then `repeat-action@2 (same action,max=1; May; repeated Attack excludes original target; no replacement)` `[tigerclaw/cloak-daggers]`. It composes typed action identity rather than parsing card colour/name. |
| Poison marker definitions and finite supply | R, F | `marker-definition@1 (poison-1 and poison-2 faces share the one physical supply identity; end-round/holder-defeat return; attachment source values above)`; the upgrade transition cannot leave both strengths active from Tigerclaw's own deck `[tigerclaw/poison-component]`. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/tigerclaw, team slot, deck, profile/dashboard references; validation-only)` `[tigerclaw/components]`. |

## Xargatha

Source: [HERO_XARGATHA.md](HERO_XARGATHA.md). Xargatha owns no token, marker, alternate form, or statue.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Cleave / Desperate Cleave | R, F | Adjacent unit/hero Attack then `repeat-action@2 (max=1, repeated target enemy hero different from original target; May/One; AfterAttack)` `[xargatha/cleave-desperate]`. Cleave permits a minion first target without weakening the repeated hero-only policy. |
| Siren's Call | R, F | `siren-target -> synthetic-range-unit-target@1 (enemy unit inside the printed Range with the adjacent ring excluded)`; `siren-move -> synthetic-direct-move@1 (mover=Out(target),distance=0..3,destination WithinActorRadius 1; IfAble/One)` `[xargatha/sirens-call]`. An immovable target remains a legal selection because the movement clause alone says `if able`. **Corrected at milestone 8.24**: this row previously mapped the selection through `attack-target@2` and ordered the move "before AttackTotal". The face prints `Basic Skill - Ranged` with a Range icon and **no Attack value**, so nothing is attacked and there is no Attack Total for the move to precede - the face is what is composed, by the standing ruling that a hero's cards are the definition. |
| Dangerous Slash / Deadly Swipe / Lethal Spin | R, N | `other-enemy-adjacent-count@1 (presence around A, exclude X.target)` feeding `modify-attack-value@1 (multiplier +1/+2/+3)` `[xargatha/slash-swipe-spin]`. |
| Long Thrust | R, N | `enemy-adjacent-count@1 (presence around A)` feeding `modify-range-value@1 (+1 each, recalculated for target derivation)` `[xargatha/long-thrust]`. |
| Rapid Thrusts | R, N | Long Thrust nodes plus `repeat-action@2 (max=1,different enemy hero; May; AfterAttack)`; the repeated target query recalculates Range from changed occupancy `[xargatha/rapid-thrusts]`. |
| Stone Gaze / Petrifying Stare / Turn Into Statues | F, N | `create-active-effect@1 (NextTurn, enemy heroes in live Radius 2/3/4)` declaring `effective-type-overlay@1 (preserve Hero and add Terrain)` and `restrict-action@2 (Movement)` `[xargatha/gaze-stare-statues]`. Overlay affects type-based rules without changing intrinsic identity/occupancy and disappears with the Active effect. |
| Fresh Converts / Devoted Followers | R, N | `adjacent-presence@1 (enemy minion/unit)` then conditional `retrieve-card@1 (A, one owned discarded card; May/One)` `[xargatha/fresh-devoted]`. |
| Charm / Control / Dominate | R, N | `green-order -> choose@1 (move enemy minion before or after enclosing Movement; **Mandatory/One** - the card offers two sides, not three, so this decision carries no decline)`; `green-may -> the printed "you may", carrying the confirm and the decline`; `green-target -> synthetic-radius-minion-target@1 (enemy Ranged / Ranged-or-Melee / any minion)`; `green-move -> synthetic-direct-move@1 (mover=Out(target),distance=0..2)`. Dominate supplies `immunity-override@1 (DirectMove, Heavy)` on both the selection and the move - a piece filtered out of the candidates is not one to offer and then refuse `[xargatha/charm-control-dominate]`. `around-primary-action@1` owns the before/after continuation and executes the ordinary Movement once. **Corrected at milestone 8.24**: the `May` belongs to the move rather than to the placement, and a Capability node may name no chooser, so the printed `you may` is its own optional node above the selection. |
| Constrict / Final Embrace | R, N | `create-active-effect@1 (EndOfRound, before MinionBattle, enemy Melee / Melee-or-Ranged minion adjacent to A)`; trigger uses `synthetic-defeat-minion@1 (automatic when one candidate; chooser=A when several; M/One; staged reward/removal/immediate Lane Push)` `[xargatha/constrict-embrace]`. |
| Metamorphosis | R, N | Ultimate source declares `enemy-adjacent-count@1`, `modify-value@2 (Movement +1 and Initiative +1 per count, recalculated at each owning query)`, and existing Move-Through permission `[xargatha/metamorphosis]`. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/xargatha, team slot, deck, profile/dashboard references; validation-only)` `[xargatha/components]`. No rules component supply. |

## Dodger

Source: [HERO_DODGER.md](HERO_DODGER.md). Dodger owns no token, marker, alternate form, or statue. She was
added at milestone 8.35z34, after this file's own six, and is the first hero here from outside milestone
8.17's line-up.

**Nine of her nineteen faces read ONE derived board quantity**, and the rows below name it once rather than
per face: `EmptyMinionSpawnPointQuery` is that derivation, reached three ways because a Defense value is a
number, a ritual is a step, and a Silver basic's clause is a narrowing on who may be picked. All nine read
`SpawnPointOverlay.ForActor`, which is her Ultimate's switch.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Dread Razor / Rusty Razor | R, F | p.10 modal over two printed bullets: adjacent-unit target, or `empty-minion-spawn-points-adjacent@1 (minimum=1)` gating a printed-Range unit target. Both bind at p.14 step 1. Rusty Razor is p.20's Handicap and carries the standing deferral `[dodger/razors]`. |
| Death Trap | R, N | `synthetic-radius-hero-target@1 (enemy hero in printed Radius, narrowed by adjacentToEmptyMinionSpawnPoint)` then `synthetic-discard-chosen-card@1 (IfAble)`. Two bounds measured from two places - the disk from Dodger, the adjacency from each candidate `[dodger/death-trap]`. |
| Littlefinger / Finger of Death | R, F | p.10 modal: adjacent-unit target, or `synthetic-range-hero-target@1 (hasCardInDiscard=true)`. The pile is bound to the named hero by "who has", and p.10 empties it at the round boundary `[dodger/fingers]`. |
| Middlefinger of Death | R, F | **Composed at milestone 8.35z45.** Its first bullet composes against milestone 8.35z27's `attack-sequence@1`; its second declares that capability's new `SubjectNarrowing` parameter, a closed enum on `AttackSequenceBullet` evaluated per candidate over the targets p.13 already admits. The widening went on the BULLET rather than on the printed target policy every Attack declares, so no dealt hero was re-identified; it moved the snapshot schema alone, v38 to v40. |
| Burning Skull / Blazing Skull | R, N | Printed-Range unit Attack then step-7 `synthetic-direct-move@3 (mover=chosen adjacent minion, 1 space, destination NotAdjacentToActor; UpTo)`, once or twice `[dodger/skulls]`. |
| Shield of Decay / Vampiric Shield / Aegis of Doom | R, N | Primary Defense declaring `RosterValueContributionDefinition (Defense +2/+2/+4)` against `TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius`. p.14 prints the `+` on the number, so the whole sentence is a value and there is no step - the first conditional printed Defense in this build `[dodger/shields]`. |
| Weakness / Enfeeblement | F, N | `create-active-effect@1 (ThisTurn, HeroesInRadius, OpposingSource)` declaring `ForValue(Attack, -4/-6)`, and Enfeeblement additionally `ForRestriction(RuleActivity.Repeat)`. The drain is a signed contribution on the Attack **Total** with no floor at zero `[dodger/drains]`. |
| Dark / Darker / Darkest Ritual | R, N | `empty-minion-spawn-points-in-radius@1 (minimum=2)` gating `gain-resource@1 (1/2/2 coins)`. Darkest Ritual adds a SECOND independent gate - `ultimate-unlocked@1` feeding `gain-item-from-the-box@1 (icon=Attack)` - side by side rather than nested, which is what the user's D-04 ruling makes of the semicolon `[dodger/rituals]`. |
| Necromancy / Necromastery | R, N | `synthetic-respawn-friendly-minion-adjacent@1` / `-in-radius@1`. p.19's eligibility is stated once in `MinionRespawnEligibilityQuery`. Under RULING 48 the node asks up to TWO questions: which empty space in reach, and - at a point the Ultimate granted - which eligible type comes back `[dodger/necromancy]`. |
| Tide of Darkness | R, N | p.16 permanent ability declaring one `RuleModification` member, `AllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone`. The whole rule lives in the member's NAME rather than in a field beside it, because adding a property to a shared declaration struct moves every hero who reaches it `[dodger/tide]`. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/dodger, team slot, deck, profile/dashboard references; validation-only)` `[dodger/components]`. No rules component supply. |

## Misa

Source: [HERO_MISA.md](HERO_MISA.md). Misa owns no token, marker, alternate form, or statue. She was added
at milestone 8.35z38, after Dodger, and is the second hero here from outside milestone 8.17's line-up.

**Six of her nineteen faces read ONE geometric quantity as an EQUALITY rather than as a bound**, and the
rows below name it once rather than per face: the user's RULING 1 reads "at maximum range/radius" as
exactly the printed number, so her candidate sets are rings and equalities where every earlier hero's are
disks. That is what makes an empty offer an ordinary outcome for her, and what makes each of these a
capability id of its own rather than a parameter on an existing one - a reader auditing a node learns the
shape from the reference it carries.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Watch How I Soar / Leaf on the Wind | R, F | p.10 modal over two printed bullets: `synthetic-place-at-maximum-range@1` (printed Range as a RING, RULING 1), or `choose-target@2` on a minion adjacent on either side then `synthetic-defeat-minion@1`. Leaf on the Wind prints only the first bullet and carries p.20's standing Handicap deferral `[misa/gold]`. |
| Swoop In | R, N | `synthetic-place-in-radius-beside-two-or-more-enemies@1` then `retrieve-card@1 (May)`. The first destination test in this build that qualifies the SPACE being considered rather than the space the actor stands in; the threshold is in the capability NAME, per the identity-oracle rule `[misa/swoop]`. |
| Challenge Accepted / Matter of Honor / Worthy Opponent | R, N | Adjacent-unit Attack then step-7 `create-active-effect@1 (ThisTurn, ControllerHero)` declaring `ForValue(Defense, +2/+3/+5)`. One sentence with one number, and the only thing that changes between the three `[misa/melee]`. |
| Power Shot / Thunder Shot | R, N | Printed-Range unit Attack then step-7 `target-was-at-maximum-range@1` / `target-is-not-adjacent@1` gating `synthetic-direct-move@3 (1 space, DestinationPolicy AwayFromActor; May)`. **Two capability ids over ONE handler**, because RULING 32 makes the tense difference a difference of MOMENT rather than of phrasing `[misa/ranged]`. |
| Sudden Breeze / Gust of Wind / Crushing Squall | R, N | `choose-line-destination@1 (maximumDistance 3/4/5, ignoresObstacles=true)`, `synthetic-direct-move@3 (NamedSpace, movesThroughObstacles)`, then `choose-traversed-unit@1 (Unit, OpposingSource; IfAble)` and `synthetic-place@2 (May)`. RULING 29 was ALREADY the movement rule's behaviour since 6.8e - measured rather than implemented; only the chooser is new `[misa/blue-place]`. |
| Dash and Slash / Death from Above | R, N | The same line and Move, then `choose-traversed-unit@1 (Hero, OpposingSource)` and `discard-owned-card@1 (IfAble)`. The unit KIND is the only difference between her two Blue riders `[misa/blue-discard]`. |
| Focus / Discipline / Mastery | F, N | `create-active-effect@1 (NextTurn, ControllerHero, carried)` declaring `TimingCheckpoint.BeforePrimaryAction` against a triggered `synthetic-direct-move@3` of 1 / up to 2 / up to 3. RULING 31 fires it ONCE; the spent latch is the existing one. The cancellation is `TurnOnlyCancellationBeforeTurnEnded`, which is what carries it across the turn boundary `[misa/steps]`. |
| Living Tornado / Storm Spirit | R, N | `choose-unit-pair-at-maximum-radius@1` / `choose-unit-pair-at-equal-distance-in-radius@1` then `synthetic-swap@1`. Two ids over ONE handler again; the second is the build's **first relational pair constraint** - the admissible second member depends on the first `[misa/swaps]`. |
| Power Overwhelming | R, N | p.16 permanent ability declaring `RuleModificationKind.ChoiceCardinality` - the ninth member and the first that modifies a CONTROL-FLOW node rather than a fact about a piece - plus `UltimateTriggerCondition.AfterControllerPlacesSelf` feeding a `choose-target@2` on an adjacent enemy hero and `discard-owned-card@1 (IfAble)`. Watch How I Soar is the raiser's only trigger in her whole set, and in a handicapped game it has none `[misa/ultimate]`. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/misa, team slot, deck, profile/dashboard references; validation-only)` `[misa/components]`. No rules component supply. |

## Silverarrow

Source: [HERO_SILVERARROW.md](HERO_SILVERARROW.md). Silverarrow owns no token, marker, alternate form,
or statue - and no face of hers moves one either, where Wasp's telekinetic Skills read "a unit, or a
token". She was added at milestone 8.35z46 and is the third hero here from outside milestone 8.17's
line-up.

**Six of her seven Attacks read ONE printed narrowing**, and the rows below name it once rather than
per face: RULING 1's `AttackTargetReach.ExactlyPrintedRange` is a BAND, so a unit nearer than the
printed Range Total is not a legal target of that Attack at all. It is a target-policy declaration and
not an effect, which is what makes it one answer for the offer, the choice and the resolution.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Shoot and Scoot / Keep the Distance | R, N | Band-narrowed Gold basic Attack, then p.14 step 7 `fast-travel@1 (scope=AdjacentZone; May)`. Keep the Distance additionally narrows to heroes and carries the standing p.20 deferral `[silverarrow/gold-basics]`. |
| Trailblazer | R, N | `fast-travel@1 (scope=Anywhere p.11 admits; May)` then `create-active-effect@1 (ThisRound, ControllerAndHeroesInRadius, AlliedWithSource)` declaring `ForPermission(MoveThroughObstacles, whilePerforming: Movement)`. RULING 47 is the `whilePerforming` and SL-07 the live membership `[silverarrow/trailblazer]`. |
| Snap Shot / Opportunity Shot / Clear Shot | R, N | ONE candidate set, `AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou`, not a p.10 modal: the two printed bullets name the same set under every reading of "any other unit", proved on a purpose-built board. RULING 39 falls out - there is no arm to carry a type `[silverarrow/red-modals]`. |
| Long Shot | R | Band only, no effect root - the plainest Attack in her set `[silverarrow/long-shot]`. |
| Rain of Arrows | R, N | Band, then `Sequence[ Conditional(synthetic-attack-took-a-hero@1) -> RepeatAction(DifferentEnemyHero, IfAble), RepeatAction(DifferentEnemyMinion, IfAble) ]`. TWO repeat clauses on one card, the first in the roster; the chain is bounded by printed clause position rather than by a counter, in `CardTextRepeatChain` `[silverarrow/rain]`. |
| Grasping Roots / Entangling Vines / Grappling Branches | R, N | `create-active-effect@1 (ThisTurn, HeroesInRadius, OpposingSource)` declaring `ForRestriction(RuleActivity.FastTravel)` and `ForActivityLimit(RuleActivity.Movement, 1)`. Arien's Deluge word for word; the three differ only in the printed Radius `[silverarrow/restriction-blues]`. |
| Warning Shot / Treetop Sentinel | R, N | `create-active-effect@1 (EndOfTurn, ControllerHero)` triggering AND expiring at `TimingCheckpoint.TurnEnded`, whose triggered effect is `synthetic-radius-hero-target@1 (OpposingSource)` then `synthetic-discard-chosen-card@1` - `IfAble` for the first, escalating to `synthetic-defeat-hero` for the second. SL-06 is that the disk is derived at FIRING time `[silverarrow/end-of-turn]`. |
| Disorient | R, N | `synthetic-object-push@3 (adjacent enemy unit, exactly 1, Mandatory)` then `synthetic-direct-move@3 (actor, 1, May)` with NO line. Its two printed differences from its siblings are both here `[silverarrow/disorient]`. |
| Divert Attention / Lead Astray | R, N | `synthetic-object-push@3 (adjacent enemy unit, UpTo 2/3)` publishing its REALISED `distance`, bound into `choose-line-destination@1`'s `maximumDistance` and thence into a straight-line `synthetic-direct-move@3`. RULING 37 is that binding `[silverarrow/straight-lines]`. |
| Fae Healing / Nature's Blessing | R, N | Printed-Radius hero selection with `includeSelf` and no team narrowing - RULING 8 - then `retrieve-card@1 (owner AND chooser = the named hero; May)` publishing `retrievedBy` on success, bound into `gain-resource@1 (1/2 coins)`. The "if they do" is that binding being undefined on a decline `[silverarrow/retrievals]`. |
| Wild Hunt | R, N | p.16 permanent ability declaring `BeforeActionTrigger(ActionKind.None, BeforeActionTriggerFrequency.EveryAction)`, whose effect is `choose-line-destination@1 (maximumDistance 2)` then a straight-line `synthetic-direct-move@3`. The first Ultimate in this build that fires before EVERY action, and the first printing neither Range nor Radius `[silverarrow/wild-hunt]`. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/silverarrow, team slot, deck, profile/dashboard references; validation-only)` `[silverarrow/components]`. No rules component supply. |

## Whisper

Source: [HERO_WHISPER.md](HERO_WHISPER.md). Whisper owns no token, marker, alternate form, or statue.
She was added at milestone `8.35z53`, after Ursafar, and is the fifth hero here from outside
milestone 8.17's line-up - and the first from the **Devoted Hero pack** rather than from the base box,
which is why PLAN Ruling 27 makes the Tabletopia extraction the sole authority for her faces.

**Five of her nineteen faces read an enemy hero's DISCARD PILE and a sixth reads the negative of it**,
which is more discard-conditioned text than any hero here prints, and the rows below name the
consequence once rather than per face: a pile fills by exactly two routes in this build - a card spent
as a Defense, and a card some text made a hero discard - so her kit opens only after the other player
has paid for something. **Two of her sentences hand a decision to somebody who is not the actor**, in
two different ways: a modal put to an enemy hero she named (`EffectChooserRole.BoundHero`), and a target
made to PERFORM a move (`defended-card-forced-move@1`). No hero above her does either.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Swift Justice / Vile Accusation | R, F | p.10 modal over two `attack-sequence@1` leaves, `distinctTargets:false`: a printed-Range hero, or an adjacent unit. Each leaf performs nothing and publishes its bullet's printed targeting sentence, so one chosen bullet is one complete p.14 order. Step-7 rider `defended-card-forced-move@1` under `IfAble`. **The roster's first face carrying a p.14 timing keyword INSIDE a modal bullet.** Vile Accusation is p.20's Handicap, narrowed to heroes on its second bullet, and carries the standing deferral. |
| Death Seeker | R, N | `Conditional(enemy-hero-with-discard-present@1)` gating `Choose(Actor, 1..1)` over `whisper-death-seeker-immunity@1` and `Optional(May) -> retrieve-card@1`. The gate is why RULING 42 applies: no pile holds anything on round 1's first turn. |
| Crimson Trail / Sanguine Path / Blood Pilgrimage | R, F | Actor space, then a Spawn-point read - a `Conditional` on Crimson Trail, a counted `empty-minion-spawn-points-in-radius@1 (maximumCounted 2/3)` on the other two - then `synthetic-direct-move@3 (UpTo)`, then `choose-target@2 (Actor, distance 1, Unit, Opposing)`, then `set-attack-target@1`. Target bound in card text under row 18's printed reading order. |
| Blood Fury / Blood Frenzy | R, N | `Conditional(enemy-hero-with-discard-present@1)` gating `RepeatAction(IfAble, max 1 / 5, DifferentTarget)` at p.14 step 7. The condition is a live query inside a body that runs per instance, which is what makes a defender's own spent card arm the instances after it. Blood Frenzy is the first face in the roster where pairwise-distinct diverges from the engine's single slot - see ledger row 27. |
| Seeds of Fear / Sprouts of Panic / Blooming Nightmare | R, N | `synthetic-radius-unit-target@1 (occupiesSpawnPoint=true, Opposing)` then `synthetic-direct-move@3` at 1 (Mandatory, EXACT) / 2 / 3 (UpTo). "A spawn point" here is p.19's broad term - these three print neither "minion" nor "in the battle zone" where seven other faces of hers print both. |
| Lesser Evil / Greater Good | R, F | `synthetic-range-hero-target@1 (Opposing)` then `Choose(**BoundHero**, 1..1)` over a price bullet and a Defeat bullet. Lesser Evil's price is `synthetic-discard-chosen-card@2 (IfAble)` and its Defeat is `Optional(May) -> choose-target@2 + synthetic-defeat-minion@1`; Greater Good's price is Mandatory with `synthetic-is-flagged@1 -> synthetic-defeat-hero@1` behind it, and its Defeat is `choose-target-trio@1 (minimum 0, maximum 3)` feeding three `IfAble` Defeats. The minions are of EITHER team, because neither face prints "enemy". |
| Shadow Step / Shadow Walk / Creeping Shadow | R, N | `synthetic-place-in-range@1` with `PlaceSpawnPointExclusion`, the inverse of Arien's three Greens: his carve Spawn-point spaces OUT of the disk, hers admit only those spaces and - on Creeping Shadow - the ring beside one. Mandatory, so rule 14 refuses the action rather than wasting the card. |
| Cruel Twist / Sealed Fate | R, N | `choose-swap-subject@1` over the union of an adjacency arm (p.19's ring, any team) and a printed-Range hero arm (Opposing, requiring a discard), then `synthetic-swap-actor@1`, then `synthetic-direct-move@3 (UpTo 2/3)` from the space the exchange published. |
| Grim Reaper | R | p.16 permanent ability declaring one `RuleModification.ForChoiceCardinality(2, ControllerBasicAction)`. It states no fact about the board: it raises how many options a modal on one of her own BASIC cards admits, which reaches Swift Justice and Death Seeker and no Red, Blue or Green face of hers. |
| Profile, dashboard, standee | R | `hero-content-definition@1 (hero-content/whisper, team slot, deck, profile/dashboard references; validation-only)`. No rules component supply - her nineteen faces place no token and give no marker. |

## Tali

Source: [HERO_TALI.md](HERO_TALI.md). Tali is the **first hero here who owns a rules component**
other than Tigerclaw's marker: three TOKEN definitions over one shared supply. She was added at
milestone `8.37`, after Whisper, is the sixth hero here from outside milestone 8.17's line-up, the
second from the **Devoted Hero pack** (so PLAN Ruling 27 makes the Tabletopia extraction the sole
authority for her faces), and the twelfth and last of the package arc.

**Eight of her nineteen faces read their OWN discard state**, which no hero above her does at all:
every other predicate in this file asks about a hero, a board space or an opponent's pile, and hers
ask about the card whose text is currently running. That is one capability,
`synthetic-source-card-in-discard@1`, negatable because her faces print both directions - "if this
card is not in the discard" scales the three Winter Attacks and gates both Spirit Attacks' bullet,
"if this card is in the discard" arms Guardian Spirit's and Warrior Spirit's retrieval. **Her one
basic Skill hands the decision to an ally**: Commune with Spirits selects a friendly hero and that
hero, not Tali, names the colour she then borrows a primary action from. And she is the only hero
here whose primary Skills move a SET of units at once - resolved simultaneously against the
pre-effect board, per question 33.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Ice Blast / Ice Shard | G, F | Her only basic Attack, and the first face in the roster to print the rulebook's `+` value cue on an ATTACK value (p.13 and p.14 illustrate it only on Range and Defense). Step-7 rider reads `defended-card-color@1` - one node publishing `defended` and `color` together off `EnclosingAttackFacts.DefenseCardInstanceId`, the fact Whisper's own dealing captured - and a `Conditional` on it gates a colour-restricted `synthetic-discard-chosen-card@1`. Ice Shard is p.20's Handicap and carries the standing deferral. |
| Commune with Spirits | G | Her only basic Skill and the hero's signature. `friendly-hero-in-play@1` (actor included, p.19 board membership, no printed icon anchors it) then `name-color@1` directed at that hero through `EffectChoiceRequest.ChooserHeroId` rather than at the controller, then `Optional(May) -> synthetic-discard-chosen-card@1`, then `borrow-primary-action@1` binding its colour `FromNodeOutput` instead of `FromClosedVocabulary`. **The first borrow in the build whose colour is a runtime output**; Ursafar's three faces all bind a composition-time literal. The naming is Mandatory and the discard optional, per the designer's own answer. |
| Winter Dagger / Winter Spear / Winter Scepter | G, F | `RosterValueQueryKind.SourceCardNotInDiscard` contributing a dynamic +3 to the printed Attack value - a value contribution on Sabina's Quickdraw template, not an effect root, which is why these three are composed and carry no `EffectRoot` at all. |
| Spirit Wolf / Spirit Bear | G, F | Ranged Attacks whose step-1 text is a p.10 modal over two bullets, the in-range bullet gated by `synthetic-source-card-in-discard@1 (negate)` INSIDE the Conditional's own then-branch rather than as a Sequence sibling. |
| Cold Snap / Snowstorm | G, F | `synthetic-choose-direction@1` (free, unconditional, all six of p.17's directions) then `synthetic-batch-direct-move@1` over every enemy unit in radius 1/2. One mandatory node: every candidate's pre-effect coordinate is captured before any commit, which is question 33's simultaneity by construction rather than by ordering discipline. Always `Applied` - zero eligible units is a legitimate empty batch, never `Impossible`. |
| Blizzard | G | The batch Move above, plus a second independent "End of turn: May repeat once" clause, composed at milestone 8.37a. **It is not a p.10 repeat**: `RepeatAction` re-runs an enclosing ACTION and there is none at `TimingCheckpoint.TurnEnded`, so the Skill arms an End-of-turn Active effect on Silverarrow's shape whose trigger re-runs the same two nodes once under the card's own "May". A card forbidding an action repeat therefore does not read it, which is a decision on the record rather than an accident. |
| Guardian Spirit / Warrior Spirit | G, F | `Conditional(synthetic-source-card-in-discard@1)` gating `retrieve-card@1` with the new `RestrictToCardParameter`, which bypasses `NeedsChoice` for the one-candidate "retrieve IT" reading Silverarrow's Nature's Blessing had no need of. |
| Glacial Barrier / Wall of Frost / Pack Ice | G, F | `tali-place-ice-token@1` at 1 / 2 / 3 placements against her printed Radius, with `tali-reclaim-for-ice-token@1` for p.18's short-supply step over the one shared supply. Both are `BoardSpaceSelection`: a reclaim asks exactly the placement's own question over standing tokens of the group. |
| Ancestral Totem / Venerated Totem | G, F | `tali-place-ancestral-totem@1` / `tali-place-venerated-totem@1` against printed Range, each with its own reclaim sibling. The save is a token PROPERTY, not an effect step - p.19 gives a placed token its properties "until that token is removed" - declared as `RuleModificationKind.RemovalPrevention` priced `RemoveTheSourceToken`, and read by `RemovalStageDecision.IsAutomaticallyPrevented` ahead of the card-priced `Offered` family so a mandatory save opens no pause. |
| Reign of Winter | G | p.16 permanent ability whose trigger is `UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack` - the third member beside Wasp's basic Skill and Tigerclaw's basic action, and the first that reads a CONSEQUENCE of the action rather than its kind. The fact is stated by the resolver step that raises the Attack's own completion checkpoint, never inferred from the produced event stream, per the 6.1ap conformance rule. It runs a `name-color@1` discard. |
| Profile, dashboard, standee, tokens | G | `hero-content-definition@1 (hero-content/tali, team slot, deck, profile/dashboard references; validation-only)` plus **three token definitions over one shared supply** - Ice, Ancestral Totem, Venerated Totem - which is the first content in this build to reach `SyntheticComponentDefinitions`' token list. |


## Swift

Source: [HERO_SWIFT.md](HERO_SWIFT.md). Swift is the **thirteenth hero and the first added after the
package arc closed**, on the 2026-09-11 user direction that re-opened the roster. She was added at
milestone `8.38e` with her set composed at `8.38d` and her vocabulary built across `8.38a`, `8.38b`,
`8.38c`, `8.38c1` and `8.38c2` - the longest build run any hero here has had, and the reason is the
one the series preamble gives: three genuinely new contracts in one block exceeded `8.19b`, the
ceiling this project names, so the block that carried them was split rather than shipped.

She is the **first hero whose Ultimate GRANTS AN ACTION**, and that is the engine fact her dealing
turns on rather than a flourish of her text: every borrow before hers was asked for by card text from
inside a resolution that is suspended and resumed around the instance, and a p.16 permanent ability
has already run to completion when it asks. The seam that lets a trigger grant at all is milestone
8.38d's.

She is also the **first hero whose two dealt basics both print SKILL primaries**, which is what makes
Bullet Time's second sentence reach an Attack only through Reload!'s onward borrow, and the reason the
per-grant narrowing follows the grant's whole chain rather than its first instance.

| Faces / component | Class | Effect nodes and exact contract |
| --- | --- | --- |
| Reload! / Backup | G, F | Her Gold basic and p.20's Handicap sibling. `borrow-primary-action@1` over `BorrowedActionPool.OwnedRightmostResolvedCard`, milestone 8.38c's third pool and **the first defined by ORDER rather than by a card's state**: it reads p.6's turn area through `EffectRosterState.TurnSlots`, filtered to the current round, and drops the asking card so a borrow cannot name itself. Reload! wraps it in p.10's `Choose one` beside `defeat-adjacent-minion`; Backup stays on the standing p.20 deferral. |
| Bounce | G, F | Her Silver basic. `choose-line-destination@1` at exactly two spaces ignoring obstacles, then `direct-move@3`; its "if this card is already resolved as you perform this action, may repeat once" gate is `synthetic-source-card-in-discard@1` over a `RepeatAction`, and it can only ever hold for a second performance off a card whose own turn is over - a borrow through Reload! or a grant through Bullet Time. |
| Snipe / Prepared Shot / Killshot | G, F | Ranged Attacks narrowed by DECLARATION rather than by card text: milestone 8.38a's `PrintedAttackTargetPolicy` gains `AttackTargetLine.InStraightLine` and `AttackTargetReach.PrintedRangeBeyondAdjacent`, so "at maximum range, and in a straight line" and "in range, in a straight line, and not adjacent to you" are read once by the offer, the target choice and the resolution and cannot disagree. The constructor refuses `InStraightLine` beside `AdjacentOnly` as a tautology, citing p.19. |
| Shotgun / Super-Shotgun | G, F | `choose-target@2` anchored on the enclosing Attack's own bound TARGET rather than on her, so the bystander who discards stands beside whatever she is shooting however far away that is; `discard-owned-card@1` under `IfAble`. Super-Shotgun adds ledger question 13's escalation as a branch reached only where the discard could not complete, and p.10 gives nobody a decision at it. |
| Steam Jump / Assault Jump / Drop Trooper | G, F | `actor-space@1` then milestone 8.38b's `synthetic-place-in-radius-in-straight-line@1` - a further `PlaceDestinationQualifier` over the placement handler registered since 6.6, never a new handler type. Drop Trooper's multi-unit push reuses the exchange vocabulary already built rather than minting one. |
| Delayed Jump / Mobile Scout | G, F | The only "End of turn:" faces in her set, standing `ActiveEffectDurationKind`-scoped promises whose bodies run `synthetic-place-in-radius-off-every-straight-line@1` - the COMPLEMENT of the band above, read from the same straight-line membership question so a card that demands the file and a card that forbids it cannot disagree about what one is. Mobile Scout adds `fast-travel@1` at `FastTravelZoneScope.SameOrAdjacentZone`. |
| Suppress / Pin Down / Killing Ground | G, N | Her three Green faces, and the one place milestone 8.38's own audit was wrong. They qualify the ENEMY they reach by that enemy's footing - "an enemy hero not adjacent to terrain" - which `adjacent-terrain-presence@1` cannot answer because it takes no subject and asks about the ACTOR. `subject-adjacent-terrain-presence@1` is minted apart rather than added as a parameter, on the rule this file has followed since 8.20: a shared declaration gaining a field moves every hero who already references it, and Tigerclaw does. Both read one `TerrainPresenceQuery`, so a hero petrified into terrain is terrain to stand beside for hers exactly as for his. |
| Mark for Death / Hunting Season | G, F | Milestone 8.38c1's counted next-turn minion bounty - one promise and two. **The count is instance multiplicity rather than persisted state**: each promise is spent once, so "how many payments remain" is "how many are still live", already recorded, restored and undone by the lifecycle. Spent at the site that already pays for the Defeat, so p.13's Bolster example - a prevented removal that still collects - pays exactly as a completed one does. The radius is read from the source card through `RadiusValueQuery`, because an Active effect may declare a radius only when its own scope is measured from one. |
| Bullet Time | G, N | p.16 permanent ability on milestone 8.38c2's `UltimateTriggerCondition.AfterControllerResolvesBasicCard` - a fifth condition rather than a widening of Tigerclaw's, because it asks whether the CARD resolved and grants that card's PRINTED PRIMARY, which need not be the action performed. It cannot re-trigger itself structurally: a granted action carries `ActionInvocationContext.TextGranted`, which is not a card resolution. Its second sentence is `GrantedTargetExclusion.SameEnemyHeroOncePerTurn`, declared on the ABILITY because "this way" names the grant - her ordinary Attack on the same enemy in the same turn is untouched, and so is a primary she borrows through Reload!. |
| Profile, dashboard, standee | G | `hero-content-definition@1 (hero-content/swift, team slot, deck, profile/dashboard references; validation-only)`. She owns no rules component: no marker, no token, no supply. |

## Cutter

Source: [HERO_CUTTER.md](HERO_CUTTER.md), and **her capability contract map lives in that shard's own
[Capability contract map](HERO_CUTTER.md#capability-contract-map) rather than here**, in this file's
notation and readable beside these sections. The reason is the threshold this file already records for
Swift, one hero later and now measured: this shard stands at roughly 8,300 words against a 9,000-word hard
limit whose warning line at 7,650 it crossed some milestones ago, so a fourteenth full section would take
it over. An ownership review is a milestone of its own rather than a side effect of a dealing, and the
pointer is the honest form of the entry until one happens.

Cutter is the **fourteenth hero** and the second added after the package arc closed, inventoried at
milestone `8.39`, composed at `8.39h` and dealt at `8.39i`, with her vocabulary built across `8.39a`
through `8.39g2` - the longest build run any hero here has had. **Her set names twenty-one contracts and
mints none**: every one was registered by a lettered block of that series or already carried by an earlier
hero, which is what those blocks were for. Two of the twenty-one are the ones her dealing registers, both
new handler TYPES: `synthetic-range-obstacle-target@1`, the p.18 obstacle her Grappling Bolt grapples to -
its candidates are SPACES rather than board objects, because obstacles include terrain and p.18 allows
only one per space - and `claim-solo-victory@1`, the only reader in this build that ends a match for ONE
hero, which is the clause that took the snapshot schema to v42 and the wire to /14 at `8.39g`.

## Delta register and dependency order

No custom handler is justified by card names. The only `U` entry is One Man Army's isolated typed
Minion-Battle rule-source handler; its identity describes the rule, not Brogan, and it cannot be reached
from ordinary action/effect dispatch.

1. **Continuation/schema foundation:** `attack-target@2`, pausable Before/After-Attack and Defense text,
   `set-attack-target@1`, `before-action-trigger@1`, and `around-primary-action@1`. Persist the enclosing
   offer, action facts, node path, pre-consequence target facts, and any pre-action frame in explicit
   snapshot schema v25; add v24 rejection or a tested migration, tamper rejection, byte-identical
   round-trips, restore/Undo/replay, and projection rebuilding. This precedes every hero.
2. **Turn/round rules-history schema foundation:** derive `target-played-attack-this-turn@1` from the
   exact current-turn assignment and printed-primary card identity, with no second state record; retain
   completed p.13 hero-Defeat outcomes as typed current-round authority. Snapshot schema v26 persists the
   latter and rejects v25 because cumulative coins/Life and diagnostic events cannot recover round scope.
3. **Reusable query/value/state foundation (completed 8.19b):** the target pair/set, source-oriented line, consumers of
   the played-card/hero-Defeat facts above, count/presence, former-space, retrieve,
   gain/transfer-resource, contribution suppression, defense-offer restriction, and typed
   immunity-override contracts. These add no snapshot fields: their answers are derived from existing
   authority and their choices use the continuation above.
4. **Persistent/system foundation:** multi-activity immunity qualified by actor, `NextTurn`, dynamic and
   action-qualified value sources, additive effective-type overlays, the after-action source, and the
   isolated One Man Army transition rule. Trusted definition and digest changes are explicit; no rule
   declaration is serialized as payload data and no snapshot field is added.
4a. **Pausing sources (completed 8.19c1):** before-removal prevention, pre-Minion-Battle end-of-round
   trigger execution, and the after-Push source. Split out of step 4 by the 2026-08-24 amendment because
   each stops the game on a player decision at an altitude the snapshot could not hold, which is an
   explicit schema boundary rather than more of the same vocabulary. Snapshot schema v27 persists all
   three and rejects v26. The removal prevention covers minions only, and says why: p.13 removes a
   defeated hero on its first line and pays its coins on its fourth, so a hero has no moment between the
   reward and the removal for the family to stand in.
5. **Hero composition:** Arien, Brogan, Sabina, Tigerclaw, then Xargatha. That order proves ordinary
   continuation/refinement consumers first, the unique Minion-Battle interaction next, multi-target board
   composition third, markers/general action Repeat fourth, and additive effective types/end-of-round
   timing last. Each hero milestone defines, deals, source-tests, drives through the player projection, and
   runs its complete printed progression; no later hero may retroactively supply a dependency. Arien
   completed at milestone 8.20, whose split leaves the permanent player-path gate to 8.20a.

The five inventories contain no need for runtime printed-text parsing, a universal expression evaluator,
hero/display-name branching, UI-owned resolution, or a second pending-choice protocol. All new choices
reuse the authoritative effect/action continuation and every source definition remains trusted content.
