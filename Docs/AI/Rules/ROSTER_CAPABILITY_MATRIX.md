# Six-Hero Roster Capability Matrix

<!-- operative-claim:F-DOC-024 status:CurrentDemonstrated -->

Canonical planning map for milestone 8.19. Return to [CARD_COVERAGE.md](CARD_COVERAGE.md). Source truth and
exact face text remain in the six hero shards; this file owns only capability classification, effect-node
contracts, dependency order, and source-linked scenario IDs for the five heroes added after Wasp.

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

Current implementation status: dependency steps 1-4a and all six hero-composition/gate rows are complete. Every capability a configured face names is registered in the default production catalog; deliberately synthetic-only fixtures remain bounded and are not presented as roster consumers. Snapshot schema is v34 and pins the complete registered contract set used by restore.

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
