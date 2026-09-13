# Phase 8 - Fourteenth-hero dealing: Cutter the Sky Pirate

Return to [roadmap](../ROADMAP.md). Current/active/next authorization lives only in [the brief](../SESSION_BRIEF.md).

## Authorization and standing

On 2026-09-11, with the thirteenth-hero series in flight, the user named the **next** hero to deal: "Once it is done, you can start implementing another hero, Cutter the Sky Pirate. Make sure you do everything a hero needs, original art on cards, card overlays, pick screen with mouse over pick card over heroes button." This shard owns that work and nothing else. The 2026-09-07 brakes direction still governs every other parked item: naming a fourteenth hero re-opens the roster, not the parked presentation program, the card-authoring workbench, or a new playtest package.

The instruction also carries a sequencing clause - "Wait for the other agent to finish" - so **no block below starts until the thirteenth-hero series is complete and its tree is clean.** The inventory at 8.39 is the exception in kind rather than in rule: it is pure corpus reading and writes only its own new files, so it can be prepared against a clean HEAD without touching anything the Swift series owns.

Cutter is the **fourteenth** hero this build deals and the second taken from outside the twelve-hero package arc that milestone 8.37 closed. She is a base-box hero, so the second-printing rulebook is available to her as a second source in the way it was to Swift - which is a check to run, not an assumption to make, and milestone 8.39 runs it.

**She is a three-star hero on rulebook p.21's complexity scale, joint-highest with Swift and Tali.** The series below is laid out the way [POST_PITCH.md](POST_PITCH.md)'s batch decision says a hero's vocabulary should be, and the way the thirteenth-hero series proved out: ordinary-sized lettered build blocks that BUILD the predicted vocabulary against showcase reachability, then one milestone that composes her faces, then one that registers and deals, then the permanent gate. Building is schema-neutral; only registering costs a version - **with one deliberate exception this series names in advance**, which is 8.39g.

## The audit that scopes this series

Milestone 8.39 owns the full inventory and the exact mapping; [Rules/HERO_CUTTER.md](../Rules/HERO_CUTTER.md) is where it lands. What the pre-authorization read of her nineteen extracted faces establishes, and what the series is sized against:

- Her set is 19 faces and matches Swift's shape exactly: one Gold basic (Walk the Plank), one Silver basic (Grappling Bolt), five Red Attacks, five Blue Skills, five Green Skills, one Purple Ultimate (Legend of the Skies), and one Handicap (Few More Steps) that p.6 returns to the box. Eighteen are dealt.
- **Most of her text is already registered vocabulary, and several pieces that look new are parameters on declarations this build has carried for milestones.** Whisper's Lesser Evil and Greater Good already print "An enemy hero in range chooses one —", which is the whole of X Marks the Spot's and A Fistful of Coins's modal shape; Misa's two Swap Skills already carry `synthetic-swap-actor@1` and `choose-swap-subject@1`, which is the whole of Outmaneuver and Outsmart; Sabina's Big Sodding Gun already declares `after-push-trigger@1` at `TimingCheckpoint.ObjectPushApplied`, which is the moment Walk the Plank needs; `ignoresObstacles` has been a move parameter since Misa; and milestone 8.38a's `AttackTargetLine.InStraightLine` - built for Swift days before this series - is exactly what Evasive Shot and Tumble Shot print.
- **The lesson the thirteenth-hero series measured twice is the standing instruction here: read the catalog's PARAMETERS, not just its capability names.** 8.38b costed a multi-subject push that `synthetic-object-push@3` had carried since 8.8. Every "owed" row below was checked against parameters as well as ids, and three of them turned into sibling parameters on existing declarations because of it.
- What is genuinely unbuilt falls in seven places, and the seven build blocks below are drawn on exactly those lines rather than on a word count. **One of them is larger than the other six put together.**

## The one block that is not like the others

**8.39g implements a victory sentence the engine has no shape for, and this series states that up front rather than discovering it at composition time.** A Fistful of Coins prints "If you have 13 or more coins, you alone win the game." `MatchVictoryCause` carries exactly three members and every one is a rulebook team sentence; `MatchOutcome` records a winning team and a losing team *together*, and its own remarks explain that "an ending that cannot name both names neither"; `MatchOutcomeFault.UndeclaredOutcomeTeams` exists precisely to reject an ending that does not resolve to one team each way. A solo winner satisfies none of that by construction.

**The user settled the reading on 2026-09-11, with the contract cost stated: implement it faithfully.** A single hero wins and everyone else loses, rather than reinterpreting the clause as a team win or parking it the way p.20's Handicaps are parked. That decision is recorded as CU-D1 in her rules shard.

So this series expects a schema move in exactly one block, and **8.39g is sequenced immediately before composition rather than at the end**, because a victory contract that all thirteen previously dealt heroes share is not something to move after eighteen new faces are already resting on it.

## Series

### 8.39 Inventory Cutter the Sky Pirate's printed set and map every clause to registered capability

Complete. Its definition moved verbatim to [the seventh completed shard](PHASE_8_COMPLETED_7.md#x-839-inventory-cutter-the-sky-pirates-printed-set-and-map-every-clause-to-registered-capability) during milestone 8.39e's required warning-threshold review. The inventory it produced is [her rules shard](../Rules/HERO_CUTTER.md), which is the authority every block below is sized against.

### 8.39a Build the directional attack vocabulary her Red line needs

### 8.39b Build the obstacle-target and grapple-move vocabulary her Silver basic needs

### 8.39c Build the zone-crossing push consequence her Gold line needs

All three complete. Their definitions moved verbatim to [the eighth completed shard](PHASE_8_COMPLETED_8.md) during milestone 8.39f's required warning-threshold review, as one contiguous run: they are the series' three obstacle-and-geometry blocks, each one a vocabulary contract proved before any content named it, and 8.39b's deferred `synthetic-range-obstacle-target@1` is the one thing below that reads back to them - the dealing at 8.39i owes it a showcase consumer.

### [x] 8.39d Build the subject-adjacency predicate and excluded repeat her Blue line needs

Observable outcome: a radius selection can narrow its subjects to those standing beside a unit of the SUBJECT's own team, and a card-text repeat can exclude the subject its first performance took.

**THE ADJACENCY PREDICATE IS A SIBLING PARAMETER, NOT A NEW SELECTION, AND THE NEAR MISS IS EXACT.** Bombardment, Barrage and Broadside print "An enemy hero **adjacent to another enemy unit** and not adjacent to you discards a card, if able." Sabina's selection already carries `adjacentToFriendlyMinion`, which narrows the same radius-hero selection by adjacency - measured from the ACTOR's team and restricted to minions. Cutter's clause measures from the TARGET's team and says unit. That is two parameter differences on a declaration that already carries one, which is what the 8.38b lesson predicts and what this block confirms before minting anything.

**"AND NOT ADJACENT TO YOU" IS ALREADY REGISTERED** as `target-is-not-adjacent@1` and needs nothing.

**THE REPEAT'S EXCLUSION IS THE GENUINELY NEW HALF.** Broadside alone adds "May repeat once on a different target." p.10's repeat is registered and milestone 8.38c2's per-grant target exclusion is the nearest shape in the build, so the question this block answers is whether that scope can carry a card-text repeat's subject exclusion or whether the two are different seams wearing one description.

**AND MILESTONE 8.35z15's RULE IS EXPECTED TO APPLY RATHER THAN BE RE-DECIDED.** That milestone established that a card-text repeat with no legal subject is not offered. Broadside with exactly one legal subject on the board must therefore not offer its repeat. The block confirms this rather than assuming it, because 8.35z15 found three shipped journeys that had only ever DECLINED their own named prompt - a shape that passes without ever exercising the rule.

**THE BLOCK'S OWN QUESTION IS ANSWERED, AND THE ANSWER SPLITS IT.** It asked "whether that scope can carry a card-text repeat's subject exclusion or whether the two are different seams wearing one description". **They are different seams**, so 8.39d ships the adjacency predicate and **8.39d1 below ships the repeat**, on milestone 8.38c's precedent - same total scope, no acceptance check dropped, each block back under 8.19b's ceiling.

**WHY THEY ARE DIFFERENT SEAMS, MEASURED RATHER THAN ARGUED.** `RepeatTargetBinding` already carries `DifferentTarget`, `DifferentEnemyHero` and `DifferentEnemyMinion`, and every one of them is about an ATTACK's own target: its own remarks say the exclusion "is carried by the repeated Attack instance rather than bound to a node id", and the store is `PendingAttackResolution.ExcludedTargets`. Broadside is a **Skill**. Its "target" is the subject a card-text selection node chose, and a Skill has no attack-target state to exclude from. The one arm that binds a selection node is `SameTarget`, which SUPPLIES the piece to that node's `Resume` - capability-agnostic precisely because it hands a value over rather than removing one - and `EffectDefinitionValidator` refuses the combination Broadside needs outright: "A different-target repeat excludes the action's own target, so it names no selection node." Milestone 8.38c2's per-grant exclusion is not the nearest shape either; it is turn-scoped and spans separate granted actions.

**WHAT 8.39d SHIPS.** `adjacentToAnotherEnemyUnit`, a second optional boolean on `choose-target@2` beside Sabina's `adjacentToFriendlyMinion`. Hers narrows the same radius-hero selection by an adjacency measured from the ACTOR's team and restricted to p.19's minions; Cutter's measures from the SUBJECT's team and admits any unit. **Two parameter differences, so it is a sibling and not a shared pair**: re-expressing Sabina's through common parameters would change her node's inputs and move a shipped digest, which is the discipline this catalog has followed since 8.20.

**"ANOTHER" DOES REAL WORK HERE WHERE IT DID NOT FOR SABINA, AND THAT IS THE TRAP IN THIS CLAUSE.** Her remarks record that a minion cannot be standing on an enemy hero, so nothing needed excluding. This clause measures a disk around the candidate for units on the candidate's OWN side - and the candidate is one. p.9 measures a disk inclusively, so without an explicit self-exclusion the clause would admit every enemy hero on the board by standing next to itself, which is the exact opposite of what it prints. Asserted as its own case.

**AND "ENEMY" IS STILL READ FROM THE ACTOR**, as every relation in this catalog is, which is what keeps this from needing a relation measured from the subject: a unit that opposes the actor is on the candidate's own side by construction, because the candidate opposes the actor too. The sharpest statement of the difference is a board where Sabina's clause and Cutter's select **different heroes**, which is asserted directly.

Acceptance checks:

- The predicate admits exactly the enemy heroes standing beside another unit of their own team, asserted as a set equality on a board where an isolated enemy hero and an enemy hero beside a friendly-to-the-actor unit are both excluded for different reasons.
- A hero adjacent to two qualifying units is offered once, not twice.
- Sabina's existing parameter is unchanged in behaviour and her candidate sets are byte-identical.
- ~~The repeat excludes the first subject, and is not offered at all when no second legal subject exists.~~ **Moved to 8.39d1**, which this milestone split out after measuring that a card-text selection's subject and an Attack's target are different seams. Nothing is dropped: the check is restated there with four more beside it.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to Sabina's declaration beyond the parameter this block adds. No change to p.10's repeat for any existing face.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839d-build-the-subject-adjacency-predicate-and-excluded-repeat-her-blue-line-needs-completion-evidence).

### [x] 8.39d1 Build the card-text repeat that excludes the subject a selection already took

Split out of 8.39d during implementation, on milestone 8.38c's precedent: same total scope, no acceptance check dropped, and each block back under 8.19b's ceiling. 8.39d asked whether milestone 8.38c2's scope could carry this and measured that it cannot.

Observable outcome: a card-text repeat on a face whose subject comes from a SELECTION node can exclude the piece that node took in the instance that asked for the repeat.

**THE THREE EXISTING `Different*` ARMS ARE ABOUT AN ATTACK'S OWN TARGET AND CANNOT REACH THIS.** `RepeatTargetBinding.DifferentTarget`'s own remarks say the exclusion "is carried by the repeated Attack instance rather than bound to a node id", and the store is `PendingAttackResolution.ExcludedTargets` - state a Skill has none of. Broadside prints "An enemy hero adjacent to another enemy unit and not adjacent to you discards a card, if able. **May repeat once on a different target**", and its "target" is the subject a card-text selection chose.

**THE ARM THAT DOES BIND A SELECTION NODE IS `SameTarget`, AND IT IS THE OPPOSITE OPERATION.** That one captures the chosen piece onto the frame (`RepeatedActionQuery.TryCaptureBoundTarget`), carries it on `RepeatContinuation.NextBoundTo`, and hands it to the repeated instance's own `Resume` - which is what keeps it capability-agnostic: nothing in the session knows what a target is, it just passes a value back. **An exclusion cannot be expressed that way**, because removing a candidate is a question only the selection can answer. That asymmetry is this block's whole problem and it should be stated in the solution rather than worked around.

**AND `EffectDefinitionValidator` REFUSES THE COMBINATION TODAY**, in as many words: "A different-target repeat excludes the action's own target, so it names no selection node." So the block owes a new member, a validator arm that requires a bound selection where the new member is declared, and a way for the excluded piece to reach the selection's candidate derivation.

**THE CAPTURE HALF IS ALREADY BUILT AND MUST BE REUSED RATHER THAN REBUILT.** `IsBoundSelection` is what arms the capture, and it currently tests for `SameTarget` alone; the new member belongs in that test. Reading the piece back out is `TryReadBoundTarget`. Both already survive a restore, which is what milestone 8.6 records as the reason the binding lives in the resolution identity rather than in a field.

**MILESTONE 8.35z15's RULE IS EXPECTED TO APPLY RATHER THAN BE RE-DECIDED.** That milestone established that a card-text repeat with no legal subject is not offered. Broadside with exactly one legal subject on the board must therefore not offer its repeat. The block confirms this rather than assuming it, because 8.35z15 found three shipped journeys that had only ever DECLINED their own named prompt - a shape that passes without ever exercising the rule.

**THE ASYMMETRY THE BLOCK PREDICTED IS REAL, AND IT IS WHAT DECIDED WHERE THE EXCLUSION LIVES.** Supplying a piece can be capability-agnostic - `SameTarget` hands its captured piece to the bound selection's own `Resume` and lets that selection judge it, so nothing in the session has to know what a target is. **Removing one cannot**, because only a selection knows its candidates. So the exclusion is applied in `EffectSelectionEligibilityQuery`, which is where every other generic narrowing in this build already happens and which already has an offer half (`EligibleCandidates`) and an answer half (`AreStillEligible`) that the session already calls. One method serves both, so a piece left out of the offer cannot be submitted to it.

**IT IS APPLIED BEFORE THE CONSUMER NARROWING RATHER THAN AFTER IT**, and that ordering is deliberate: a piece an excluding repeat took is not a candidate at all, so no question about it is put to a consumer. It is also the only narrowing in that query that applies where no consumer can be read, which is what keeps it a rule about the card rather than a rule about the consumer.

**ONE CAPTURE, TWO USES, AND THE SPLIT THAT KEEPS THEM APART.** `RepeatedActionQuery.BindsSelection` armed the capture for `SameTarget` alone; it now answers for both arms, because both need the piece recorded at the moment the selection names it - the last moment it is still standing where the selection saw it. Everything after that point had to learn the difference, so `BoundBy(definition, node, binding)` is split out and `TryReadBoundTarget` - the SUPPLY path - is pinned to `SameTarget` explicitly. Widening that one instead would have handed the repeated instance the very subject the clause tells it to avoid, which is the defect this split exists to make impossible.

**MATCHED BY IDENTITY WHERE THE OFFER NAMES A PIECE AND BY SPACE WHERE IT NAMES A COORDINATE.** A selection publishes whichever its own card asked for and the frame records the excluded piece as a board object, so a comparison that only knew one of those would silently exclude nobody - a narrowing that quietly does nothing is worse than one that refuses. Both arms are asserted.

**TWO PLACES THE TWO BINDING ARMS DELIBERATELY DIFFER, BOTH ASSERTED.** A `SameTarget` repeat whose selection named nobody REFUSES, which is milestone 8.6's conservative answer - running it unbound would be the card saying something it does not say. An EXCLUDING repeat with nothing to exclude ADVANCES, because a repeat of an unnarrowed set is exactly what p.10's default already is. And `RepeatRequest.Binding` still reports `Unbound` for `SameTarget`: that field exists so a dispatcher can honour "on a different target" without reading nodes, and putting a value where Wasp and Dodger have always seen none would be a behaviour change they never asked for.

**NOTHING IS STORED**, on `DifferentEnemyHero`'s own terms: the repeated instance reads this member off the trusted definition when it builds its candidates, and the piece comes from the same restorable frame state `SameTarget` has used since milestone 8.6. No snapshot field and no schema version.

Acceptance checks:

- A repeat declaring the new binding offers the same selection its printed instance did, minus the piece that instance took, asserted as a set equality on a board carrying at least three legal subjects.
- The exclusion is read by the same query the offer and the answer use, so the taken piece cannot be submitted to the repeated instance.
- ~~The repeat is not offered at all when no second legal subject exists, which is milestone 8.35z15's rule confirmed rather than re-decided.~~ **Amended at closing: this is a FEASIBILITY question about the repeat node, not an exclusion question, and it belongs to the milestone that composes the face.** 8.35z15's rule is enforced where a repeat's offer is judged; what this block owes it is that the narrowed set be the one that judgement reads, which the shared method gives it. The behaviour is asserted at composition, where a real board can hold exactly one legal subject - a vocabulary fixture can only assert the narrowing that produces the empty set, and does.
- A piece that has become ineligible for another reason between the two instances is excluded by that reason rather than by this one, and the two do not mask each other.
- `SameTarget`'s capture and read are shared rather than copied, and Wasp's and Dodger's existing bound repeats are byte-identical in behaviour, asserted rather than assumed.
- `EffectDefinitionValidator` refuses the new member without a bound selection node, and refuses a bound selection node that is the repeat itself, on the terms it already refuses those for `SameTarget`.
- No schema field, corpus digest or pinned constant moves - the narrowing is derived from the trusted definition when the repeated instance builds its candidates, which is what `DifferentEnemyHero`'s remarks record as the reason that member moved none.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to the three Attack-target `Different*` arms, to `PendingAttackResolution.ExcludedTargets`, or to milestone 8.38c2's turn-scoped granted-target exclusion. No new repeat node kind - p.10's repeat is one node kind and this is a declaration on it.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839d1-build-the-card-text-repeat-that-excludes-the-subject-a-selection-already-took-completion-evidence).

### [x] 8.39e Widen the existing charge line so her Green faces can ignore obstacles

**THIS BLOCK WAS RE-SCOPED BEFORE IT RAN, AND THE RE-SCOPE IS THE POINT.** It was drafted as "build a destination-constrained move", on the reading that the catalog could narrow a p.18 Place by its destination's neighbours but not a p.19 Move. That reading was wrong, and reading the catalog's PARAMETERS rather than its capability names is what found it - the same lesson milestone 8.38b measured and the second time it has paid.

Observable outcome: the charge line her three Green faces need travels while ignoring obstacles, and every declaration written before this milestone keeps its exact candidate set.

**`choose-charge-line@1` IS ALREADY THIS MOVE.** Brogan's Mad Dash prints "Before the attack: Move 2 spaces in a straight line **to a space adjacent to an enemy unit**, then target that unit", and Cutter's Brace for Impact prints "Move 3 spaces in a straight line, ignoring obstacles, **to a space adjacent to an enemy hero**; that hero discards a card, if able." They are the same sentence with different numbers, a different unit noun and one extra clause. The capability's schema already carries:

- `minimumDistance` and `maximumDistance` as separate integers - so "Move 3 spaces" is `min 3, max 3`, "Move 3 or 4 spaces" is `min 3, max 4`, and "Move 3, 4 or 5 spaces" is `min 3, max 5`. **The exact-distance arm this block was going to build already exists** and Brogan, Ursafar and the showcase all bind it.
- `unitKind` over `EffectIntegerDomain.AllExcept(RosterUnitKind.None)`, and **`RosterUnitKind.Hero` is already a member** - so "adjacent to an enemy hero" is a value the domain admits today rather than a narrowing to be built.
- a `destination` coordinate output, which is what the following Move and the discard both read.

**SO THE ONLY GAP IS `ignoresObstacles`, AND IT IS ONE OPTIONAL PARAMETER.** `choose-line-destination@1` has carried that parameter since Misa; `choose-charge-line@1` has not, because no printed charge had needed it. It is added here **optional and defaulting to false**, which is the discipline Sabina's `adjacentToFriendlyMinion` records in its own remarks: every declaration written before this milestone keeps its exact candidate set, and Brogan's three faces must be byte-identical afterwards.

**THE HERO THE DESTINATION WAS BESIDE IS STILL A QUESTION, AND IT IS THE ONLY DESIGN WORK LEFT HERE.** Brogan's sentence says "then target **that** unit", so his composition takes the unit the destination was chosen against. Cutter's says "**that hero** discards a card", which reads the same way - but a destination adjacent to two enemy heroes is a board state his faces already reach and hers will too. Settle it, record the reading, and prove it; the natural answer is that the charge line is chosen against a named unit and that unit is the output, so the ambiguity never arises.

**SETTLED, AND NOT THE WAY THIS BLOCK PREDICTED - WHICH IS THE FIFTH TIME RUNNING IN THIS SERIES.** The charge line names no unit and cannot be made to without undoing a shipped decision. Its offer is a set of DESTINATIONS: `Options` keeps the qualifying units only long enough to decide whether a destination is offered at all, and `Destinations` then collapses them away with `.Distinct()`, so the one destination beside two enemy heroes is offered ONCE and answering it publishes a space and nothing else. **The subject is named afterwards, by the ordinary radius-1 `choose-target@2` Brogan's own composition has asked since milestone 8.35p** - measured from where the hero now stands, which the handler's remarks already prove is the same disk the charge measured. On a destination beside two enemy heroes that selection offers both and the mover chooses, which is what the printed text leaves to the player.

**THE PREDICTED ANSWER IS REFUSED FOR A STATED REASON RATHER THAN FOR A PREFERENCE.** Having the charge choose against a named unit and publish it is precisely the second question milestone 8.35p TOOK OUT of this node, on the user's rule that "they move as soon as they select, and then they attack": asking it again would name the victim while the hero authoritatively still stood on his starting space. An adverb's ambiguity is not worth reopening that. The reading is recorded in the handler's own remarks and asserted as its own case.

Acceptance checks:

- The charge line admits exactly the destinations at a distance within the printed bounds, on a straight line, and adjacent to an enemy hero - asserted as a set equality on a board where each condition excludes a different candidate.
- The exact-distance arm offers nothing where no destination at exactly that distance qualifies, rather than falling back to a shorter move.
- Binding `ignoresObstacles` true changes the reachable set and does not change the adjacency requirement; leaving it unbound reproduces today's behaviour exactly.
- **Brogan's Mad Dash, Bullrush and Furious Charge candidate sets are byte-identical**, asserted rather than assumed, because this block edits the declaration they depend on.
- ~~The hero the destination was chosen against is the one the consequence reads, on a destination adjacent to two enemy heroes.~~ **Restated to the settled reading**, because the check's premise is false: this node names no unit. Asserted on exactly that board - the destination is offered ONCE rather than once per hero beside it, answering it publishes `destination` and nothing else, and the radius-1 selection that follows the Move offers both heroes.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to `choose-line-destination@1`. No new move capability - this block was drafted to mint one and must not, now that the existing one has been read properly.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839e-widen-the-existing-charge-line-so-her-green-faces-can-ignore-obstacles-completion-evidence).

### [x] 8.39f Build the previous-turn-slot borrow pool her Ultimate needs

Observable outcome: a face can borrow the printed primary action of a card sitting in the turn slot before the one being resolved.

**THIS IS THE DIRECT SIBLING OF MILESTONE 8.38c AND SHOULD BE THE CHEAPEST BLOCK IN THE SERIES.** Legend of the Skies prints "The first time each turn after you perform a primary action, you may perform the primary action of a card in the previous turn slot." `borrow-primary-action@1` is the verb, registered with Ursafar at 8.35z50; `BorrowedActionPool.OwnedRightmostResolvedCard` was added at 8.38c and reads `EffectRosterState.TurnSlots` filtered to the current round. This pool reads the same ordering one slot back instead of rightmost.

**EVERY LESSON 8.38c MEASURED APPLIES UNCHANGED AND MUST BE RE-PROVED RATHER THAN ASSUMED INHERITED.** Narrow the ordered set BEFORE the per-card filters, so a previous-slot card the borrow refuses empties the pool rather than falling through to the slot behind it - the face names one slot. The borrowing card is its own first false answer. `RoundTurnSlot` already refuses an assignment naming another hero's card, so a hero filter is defence in depth rather than the guarantee. Absent roster state fails closed to an empty pool.

**WHAT IS ACTUALLY OPEN IS WHOSE SLOT IT IS, AND THE BLOCK SETTLES IT.** The face says "**a** card in the previous turn slot", not "**your** card - which is her shard's CU-Q3. 8.38c's pool is owner-scoped and `RoundTurnSlot`'s refusal points the same way, but pointing is not settling. Take a reading, record it, and prove it on a board where the two readings differ.

**BOTH PROBES THIS BLOCK ORDERED CAME BACK AGAINST IT, AND ONE OF THEM SPLITS IT.**

**The owner scope is settled by the rulebook, not by the engine's shape, and it is settled OWNER-SCOPED.** The block expected to reason from 8.38c's pool and `RoundTurnSlot`'s refusal. Neither is the authority and neither would have been enough: `RoundTurnSlot` carries one assignment for EVERY configured hero, the card state a borrow is handed really does contain the other heroes' instances, and a table-wide reading was therefore both grammatically available and mechanically implementable. **p.6 step 6 answers it outright: "place your card Faceup on the current Turn slot of your hero dashboard."** A turn slot is printed on a dashboard. There is no table-wide slot to read; the engine's per-turn record is an aggregate of the six that exist. So "a" rather than "your" is not a widening - "your" is already carried by "turn slot". CU-Q3 closes.

**The trigger half is NOT registered in shape, and no existing condition can absorb it - so it splits out as 8.39f1.** Her shard's mapping row claimed Tigerclaw's `AfterControllerPerformsBasicAction` covered it. It cannot, and neither can Swift's `AfterControllerResolvesBasicCard`: **every Ultimate trigger in this build hangs on `PerformedBasicAction`, and `BasicActionQuery.ForCompletedAction` returns `None` unless the source card's classification is `Basic`** - p.9's supertype, which is the Gold and Silver cards alone. Cutter's Ultimate fires after a PRIMARY action, which every card in her deck has, and her Tier IV is unlocked when most of what she plays is not basic. Reaching it means carrying a fact where the one production site carries none today, and that fact type is read by nineteen files including the snapshot serializer and p.10's repeat machinery - so widening it would change what Wasp's, Tigerclaw's, Misa's, Tali's and Swift's Ultimates see, which **this block's own non-goals forbid**. Split on milestone 8.38c's and 8.39d's precedent: same total scope, no acceptance check dropped, each block back under 8.19b's ceiling.

**AND TURN 1 HAS NO PREVIOUS SLOT.** p.6's turn area is emptied at the round boundary, so the first turn of a round offers this clause nothing. That is the same "empty pool rather than fall through" answer as above and the block asserts it directly.

**THE TRIGGER HALF IS NOT NEW.** "The first time each turn after you perform a primary action" is the once-per-turn machinery milestone 8.38c1 exercised, over a primary action rather than 8.38c2's basic card. Probe whether an existing `UltimateTriggerCondition` member already covers it before minting a sixth; Tigerclaw's reads a basic ACTION and Swift's reads a basic CARD, and Cutter's reads any primary action, which may be a widening none of them can absorb - or may be one that already exists.

Acceptance checks:

- The pool names at most one card, is empty on the first turn of a round, and follows the turn area's committed order rather than any instance-id ordering, proven on a board where the two disagree.
- A previous-slot card the borrow refuses empties the pool, on a board where a fall-through would have succeeded.
- The owner scope is settled, implemented and recorded, and proven on a board where the two readings differ.
- ~~The trigger fires once per turn and not twice, and fires after a primary action rather than after a basic card.~~ **Moved to 8.39f1**, which this milestone split out after measuring that no existing Ultimate trigger can reach a non-basic card at all. Nothing is dropped: the check is restated there with four more beside it.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to Ursafar's, Swift's or Tali's borrow pools, nor to any existing Ultimate trigger.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839f-build-the-previous-turn-slot-borrow-pool-her-ultimate-needs-completion-evidence).

### [x] 8.39f1 Build the after-a-primary-action Ultimate trigger her Ultimate fires on

Split out of 8.39f during implementation, on milestone 8.38c's and 8.39d's precedent: same total scope, no acceptance check dropped, and each block back under 8.19b's ceiling. 8.39f probed whether an existing `UltimateTriggerCondition` covered this and measured that none can.

Observable outcome: an unlocked Ultimate can fire the first time each turn after its controller performs a primary action, whatever card that action came off.

**NO EXISTING CONDITION CAN REACH IT, AND THE REASON IS ONE LINE OF `BasicActionQuery`.** All five members of `UltimateTriggerCondition` are matched against a `PerformedBasicAction`, and `BasicActionQuery.ForCompletedAction` returns `PerformedBasicAction.None` unless the source card's `Progression.Classification` is `CardClassification.Basic`. `UltimateTriggeredEffectSourceQuery.Derive` then returns an empty source list for an undefined fact, so a non-basic card reaches no Ultimate trigger at all. Tigerclaw's "any basic action" and Swift's "you resolve a basic card" are both inside that gate. Legend of the Skies prints "after you perform a primary action", and every card prints a primary.

**SO THE FACT IS THE WORK, NOT THE ENUM MEMBER.** `TurnResolutionAuthority.AddCompletedAction` is the ONE place a completed action is recorded, and it already knows the `ActionInvocationContext` - so it already knows a printed primary from a repeat, a Hold or a granted action. What it does not do is carry anything when the card is not basic. **Widening `PerformedBasicAction` itself is the shape to avoid**: nineteen files read that type, including `AuthoritativeSnapshotSerializer`, `PendingAfterActionRepeat` and p.10's repeat machinery, and a fact that became defined for non-basic cards would change what five shipped Ultimates and the repeat rule see. The block should carry the primary-action fact BESIDE it rather than through it, and assert the five existing conditions byte-identical rather than assume it.

**"THE FIRST TIME EACH TURN" IS MILESTONE 8.38c1's MACHINERY** over a primary action rather than 8.38c2's basic card, and is expected to be reused rather than rebuilt. A turn performs as many primary actions as text grants; the clause fires on the first and not the rest.

**AND A BORROWED PRIMARY IS ITSELF A PRIMARY ACTION**, which is the loop this bound has to close: the action the Ultimate grants must not re-arm the Ultimate. State the reading and assert it, rather than relying on the once-per-turn bound to hide it.

**WHAT THE BLOCK PREDICTED AND WHAT IT MEASURED - AND FOR ONCE THE BLOCK WAS RIGHT ABOUT ALL OF IT.** The fact was the work, the enum member was one line, `AddCompletedAction` already knew the `ActionInvocationContext`, and widening `PerformedBasicAction` would have been the wrong shape. `PerformedPrimaryAction` is carried beside it, produced at the same single site, consulting no card definition at all - which is the whole difference from p.9's basic supertype, a fact about the CARD. The five existing conditions are matched against the old fact exactly as they were and are asserted byte-identical in behaviour on the boards that distinguish them.

**THE FIVE INVOCATION CONTEXTS THAT DO NOT ARM IT, EACH FOR ITS OWN REASON.** The two secondaries are not the primary band. A p.14 Defense is a `Reaction`, and every printed-primary Block is classified as one before it reaches the question. **A replacement is not the printed primary**, and this build had already settled that in as many words - `BeforeActionTriggerQuery`'s own remark says "p.7's Fast Travel and p.13's Clear are replacements rather than the printed primary the sentence names" - so the same sentence shape gets the same answer at both ends of the action. **And a repeat does not arm it, because the fact needed to decide is not recorded**: `ActionInvocationContext.Repeated` REPLACES the original context rather than qualifying it, so a completed repeat cannot say whether what it repeated was the printed primary. Answering "yes" would have been inventing that. It is moot on every reachable board, because a repeat follows the action it repeats and that action already armed the clause - but the reading is taken on what the build can see rather than on the mootness.

**`TextGranted` DOES arm it**, which is the loop, and the bound is what closes it: the clause fires on the FIRST primary action of the turn, and a grant can only follow a firing. Asserted directly rather than argued, including the premise that the borrow's own declared invocation context is the one that arms.

**THE BOUND REUSES THE TURN'S EXISTING CONSUMED-KEY SET** - milestone 8.19a's spent before-action triggers and milestone 8.38d's granted-target exclusions already share it, it is already serialized, and the turn boundary already clears it - under a prefix of this milestone's own, so a spent after-action trigger cannot be mistaken for the same Ultimate's spent before-action one. Nothing new is persisted and the schema does not move. Only a condition whose printed sentence bounds it is keyed, asserted by showing that an unbounded one fires repeatedly and writes nothing into the turn at all.

Acceptance checks:

- The trigger fires once per turn and not twice, and fires after a primary action rather than after a basic card - proven on a turn whose primary action comes off a card that is NOT basic, which no existing condition can see.
- The five existing conditions are byte-identical in behaviour, asserted rather than assumed, on the boards that distinguish them.
- A repeat, a Hold, a Fast Travel and a borrowed primary each either arm the trigger or do not, by a recorded reading, and the granted action does not re-arm it.
- Absent or undefined action facts fail closed to no trigger.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to `PerformedBasicAction`'s own definedness rule, to p.10's repeat machinery, or to any of the five existing Ultimate trigger conditions.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839f1-build-the-after-a-primary-action-ultimate-trigger-her-ultimate-fires-on-completion-evidence).

### [x] 8.39g Build the solo victory condition A Fistful of Coins prints

**This is the block the series preamble singles out, and the only one that is expected to move the schema.** The user settled the reading on 2026-09-11 with the contract cost stated: implement the printed sentence faithfully.

Observable outcome: a match can end with a single hero as the winner and everyone else - including that hero's own team-mates - as losers, recorded as a fourth victory cause, restored from a snapshot, replayed, undone, and projected to every seat.

**THE ENGINE'S VICTORY CONTRACT IS TEAM-SHAPED IN THREE PLACES AT ONCE.** `MatchVictoryCause` carries exactly three members and all three are rulebook team sentences. `MatchOutcome` records `WinningTeamId` and `LosingTeamId` together, and its remarks say why: "p.8 names its winner by side and p.13 names its loser by team, and each implies the other, so an ending that cannot name both names neither." `MatchOutcomeFault.UndeclaredOutcomeTeams` exists to reject an ending that does not resolve to exactly one of each. A solo winner satisfies none of that, and in 2v2 and 3v3 the winner has team-mates the printed sentence does not mention.

**AND THE OUTCOME IS DERIVED ON RESTORE RATHER THAN STORED, WHICH IS THE FACT THAT DECIDES THIS BLOCK'S COST.** `AuthoritativeSnapshotSerializer.Round` does not persist the outcome; it re-reads it through `MatchOutcomeQuery.Read` from the Lane-Push authority's `GameEndingCause`, the team life counters and the recorded phase, and its own comment says the ending is checked "rather than recorded a fourth time". Every existing cause has a durable source to be re-derived from. **A solo victory has none.** A hero holding thirteen coins is not a hero who won - coins are farmed by every hero and spent back down at p.15's Level Up - so the winning fact is "this hero resolved that branch while holding thirteen or more", which no other state records. It must be written, and writing it is a schema version and everything a version move owes.

**SO THIS BLOCK OWES THE FULL SCHEMA CEREMONY AND SHOULD BUDGET FOR IT AS THE MAIN COST.** The six pinned docs `Tools/PinnedConstants.txt` names, each with its own regex wording; the four bare `Is.EqualTo(<version>)` assertions in `DefeatRemovalStageTests`, `MinionAttackResolutionTests`, `RosterStateOperationCapabilityTests` and `RosterPausingSourceTests`, with the two narrative ones EXTENDED rather than renumbered; the corpus fixture names as well as their digests; `CardAggregateInvarianceTests`; and a byte-level delta measured in a throwaway worktree at the pre-change SHA. The ledger file is the list - do not trust a count.

**THE WIRE IS THE SECOND CONTRACT AND IT IS NOT OPTIONAL.** `NetworkMatchClient` reconstructs the outcome from `wire.Outcome.Cause` and the teams it names. A cause whose winner is a hero rather than a team does not fit the record it reads, so the wire protocol moves with the schema and the projection must state a solo ending to every seat - including the seats that lost to it.

**THE THREE QUESTIONS THE BLOCK MUST ANSWER RATHER THAN ASSUME**, each recorded with its reading: what the winner's team-mates are (the printed word is "alone", so the natural answer is that they lose, and it needs stating because it is the first ending in this build where team-mates differ); whether the existing `UndeclaredOutcomeTeams` fault stays exactly as strict for the three team causes (it must - widening it for all four would remove the guard that catches a genuinely malformed team ending); and what happens if a solo win and a team ending would land at the same moment, which is the case p.8's own tie-break paragraph handles for two team endings and which `ConflictingVictoryPaths` already refuses.

**THE STRUCTURAL WORRY THIS BLOCK WAS EXPECTED TO CARRY IS ALREADY SOLVED, AND THAT IS WORTH KNOWING BEFORE IT STARTS.** A Fistful of Coins ends the match in the middle of a turn, inside an effect resolution - which sounds like the hardest thing here, because every ending a reader meets first is an END-OF-ROUND one. It is not new. `RoundResolutionAuthority.EndGame(EndOfRoundStage?)` takes a nullable stage precisely so that a mid-turn ending can pass `null`, and its remarks say why: milestone 7.7 records that "p.8's mid-turn push ends the game in the middle of a turn, and naming a stage it never reached was the last thing left in this build that answered 'where did the match end' with something other than where it ended."

**`EndGame` is a single chokepoint and it already does everything an ending owes.** Its own summary: "the outcome is read, the phase records that the match stopped, the turn stops offering the decisions it was in the middle of, and one event says who won and why" - `ReadOutcome`, `Phase = GameEnded`, `CurrentTurnAuthority?.StopResolutionForGameEnd()`, and a `GameEnded` event. It also states what an ending must NOT do: nothing is cleared, because "the position the match ended in is the only honest record of where it ended, it is what the payload keeps, and a retained-prefix Undo truncated before the ending needs it to still be there."

**SO THE WORK IS `ReadOutcome`, NOT THE ENDING MACHINERY**, and the block should start there rather than at the phase. What `ReadOutcome` cannot do today is produce an outcome whose winner is a hero, because `MatchOutcomeQuery.Read` derives from the Lane-Push cause and the life counters and nothing else.

**ONE RE-ENTRANCY QUESTION THIS BLOCK MUST ANSWER RATHER THAN MEET.** `StopResolutionForGameEnd()` stops the turn's in-flight resolution - and here the resolution being stopped is the one that caused the ending. A Fistful of Coins' branch is still on the stack when the match ends. Settle what happens to the remainder of that effect (the coin award has already run; the modal has already been answered), prove it, and record it. p.8's mid-turn push is the precedent to read first, because it ends a match from inside a turn too, but it does so from a stage rather than from inside an effect node.

Acceptance checks:

- A hero resolving the branch while holding thirteen or more coins ends the match, is recorded as the sole winner, and every other participant is a loser - asserted in a 2v2 or 3v3 where the winner has team-mates.
- A hero resolving the branch while holding twelve coins does not end the match, and a hero who holds thirteen coins without resolving the branch does not either.
- A hero who reaches thirteen coins, spends down at Level Up, and later resolves the branch below thirteen does not win - the count is read at the branch, not at its high-water mark.
- The ending survives a snapshot round-trip, a replay and an Undo, and a payload claiming a solo ending the coin ledger does not support is refused rather than resolved by preference.
- The three existing causes are byte-identical in behaviour and `UndeclaredOutcomeTeams` is exactly as strict for them as before.
- The schema move is measured, stated with its cause, and every pinned constant, digest and bare assertion the ledger names is re-pinned from measurement.
- The wire protocol move is stated with its cause and a solo ending projects correctly to a losing seat.
- Complete unfiltered dotnet harness and Edit Mode pass.

**THE THREE QUESTIONS, ANSWERED.** (1) **The winner's team-mates lose.** The printed word is "alone", and this is the first ending in the build where two heroes on one team do not share a result - asserted on a 2v2 where the winner has a team-mate the sentence never mentions. (2) **`UndeclaredOutcomeTeams` is exactly as strict for the three team causes**, and the solo ending got its own fault (`UndeclaredSoloWinner`) rather than a widening: loosening the old guard to admit an ending with no teams at all would have removed the thing it exists for, and a one-team composition is still refused by it. (3) **A solo claim landing with a team ending is refused, not preferred** - `ConflictingVictoryPaths`, on the terms milestone 7.7 set, asserted against both p.8's push and p.13's counters.

**THE RE-ENTRANCY QUESTION, ANSWERED BY THE CHOKEPOINT RATHER THAN AROUND IT.** `TryRecordSoloVictory` writes the fact and then calls `EndGame(null)` - the same nullable-stage shape milestone 7.7 gave p.8's mid-turn push, so "where did the match end" is answered with where it ended. `StopResolutionForGameEnd()` stops the turn's in-flight resolution, which is the one that caused the ending, and NOTHING IS CLEARED, because the position the match ended in is what the payload keeps and what a retained-prefix Undo truncated before the ending needs to still be there. The block was right that the work was `ReadOutcome` and not the ending machinery.

**THE SHAPE OF THE OUTCOME, AND THE ONE THING EVERY READER NOW OWES.** `MatchOutcome.WonAlone` names a hero and NO teams - there is no winning side, and no single losing team either, because every team has a loser in it. So `WinningTeamId` and `LosingTeamId` are the named absence for this shape, and every reader that wants a side asks `HasSoloWinner` first. Nine sites read those fields; the one that would have THROWN is the player-facing headline, which asked `Participants.GetTeam(WinningTeamId)`, and it now names the hero.

**THE SCHEMA MOVED 41 -> 42 AND THE WIRE 13 -> 14, AND THE DELTA IS MEASURED RATHER THAN ASSERTED**: **+1 authoritative byte per snapshot** (`setup` 11,122 -> 11,123; `verified-hand-setup` 10,221 -> 10,222), which is the empty-string length prefix every match that did not end alone writes. Measured in a throwaway worktree at the pre-change SHA, as the ledger requires. Sixteen corpus fixtures renamed and re-pinned from one measuring run, four bare assertions moved, the six pinned docs re-stated in their own wordings, and the two narrative pins EXTENDED rather than renumbered - `CardAggregateInvarianceTests` for a cause none of its notes had recorded before, the snapshot FORMAT growing a field rather than the registered list moving.

Non-goals: No Cutter content - the face that reaches this is composed at 8.39h. No new victory condition for any other hero. No change to p.8's or p.13's endings beyond the re-measurement this block forces.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839g-build-the-solo-victory-condition-a-fistful-of-coins-prints-completion-evidence).

### [x] 8.39g1 Build the seam a printed card claims the solo victory through

**This block is a split out of 8.39g, found by the probe 8.39h ordered before composing.** It is the same total scope and no acceptance check is dropped, on the precedent 8.39d1 and 8.39f1 set in this series and 8.38c set before it. It runs BEFORE 8.39h, because the composition names what it builds.

Observable outcome: an effect node a printed card declares can claim the solo victory for its controller, and the match ends at the same boundary every other mid-turn ending is noticed at.

**8.39g BUILT THE ENDING AND NOT THE WAY A CARD REACHES IT, AND ITS OWN NON-GOAL IS WHERE THE ASSUMPTION SITS.** That block wrote "No Cutter content - the face that reaches this is composed at 8.39h", which reads as though composition alone would connect the two. It does not. `RoundResolutionAuthority.TryRecordSoloVictory` is an instance method on the round authority; an effect capability is handed `EffectRosterState` (turn slots, hero-Defeat history, the coin ledger), `MatchComponents`, `MatchParticipants` and the frame's own facts, and no path from any of them reaches the round. A composed face declaring the branch today would gain three coins and stop.

**THE TRANSPORT DOES NOT HAVE TO BE BUILT, AND THAT IS WHAT KEEPS THIS BLOCK SMALL.** The obvious design is the four-layer channel p.8's recorded Lane-Push cause travels - a capability publishes a typed fact, `RecordedLanePushQuery` reads it off the completed frame, `EffectResolutionStepResult` carries it, and the card action hands it up. That channel is real and it is the wrong size for this. Milestone 8.16 already widened `ExecuteTurnCommand`'s post-command check from p.13's counters to "every ending the state can already describe", and 8.39g's own `ASoloEndingThePhaseDoesNotRecord_IsAnUnrecordedGameEnd` proves a recorded solo winner with the phase not yet ended reads as exactly the fault that hook looks for. So a node that only WRITES the winner gets `EndGame(null)` from the existing boundary, and the re-entrancy 8.39g worried about never arises - nothing calls `EndGame` from inside a live effect frame.

**AND THE BOUNDARY HOOK IS BLIND TO IT TODAY, WHICH IS THIS BLOCK'S PRE-FIX OBSERVATION.** `AnUnrecordedEndingExists()` calls `MatchOutcomeQuery.Read(participants, lanePush?.LanePush, TeamLives, gameIsRecordedAsEnded: false)` - the four-argument overload, with no `soloVictoryHeroId`. `ReadOutcome` passes the field and this does not, so a solo victory recorded mid-turn is invisible to the one seam that notices every other mid-turn ending. It is a one-line omission with a real consequence, and it is observed failing before it is fixed.

**THE THRESHOLD IS THE CARD'S, NOT THE ENGINE'S.** "If you have 13 or more coins" is a number A Fistful of Coins prints, so it is a declared parameter on the node rather than a constant here - the discipline every printed distance in this catalog follows. The count is read at the branch from the live ledger, which is what 8.39g's `TheCountIsReadAtTheBranchRatherThanAtItsHighWaterMark` already requires of it.

**IT IS A NEW HANDLER TYPE, SO IT OWES MILESTONE 7.9'S GATE BOTH HALVES** - deferred until a dealt card starts it, which is 8.39i, and a showcase consumer added there. That is 8.39b's position exactly, and this block joins its declaration to the same deferred list.

Acceptance checks:

- A node declaring the claim ends the match for its controller when the live ledger holds the printed threshold or more, and does nothing at all below it - asserted at 12, 13 and 14.
- The winner is the node's CONTROLLER and never the actor of some enclosing grant, asserted directly.
- `AnUnrecordedEndingExists` is observed NOT noticing a recorded solo winner before the fix and noticing it after, and the three team endings are byte-identical in behaviour either way.
- A second claim in the same match is refused rather than layered, and a claim by a hero the match is not playing is refused.
- The declaration is deferred rather than registered, and a case says so.
- No schema field, corpus digest or pinned constant moves - the fact 8.39g added is the one this writes, and writing it from a second place adds no state.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content - the face that declares this is composed at 8.39h. No registration and no showcase consumer; both belong to 8.39i with the rest of her dealing. No change to what an ending means for any of p.8's or p.13's three.

**THE BLOCK'S PREDICTIONS ALL HELD, WHICH MAKES IT THE SECOND IN THIS SERIES TO MANAGE THAT** after 8.39f1. The boundary was blind, the widening is one argument, and the claim needed no transport built.

**THE DEFECT IS SMALLER THAN ITS CONSEQUENCE AND THAT IS WORTH RECORDING.** `AnUnrecordedEndingExists` called `MatchOutcomeQuery.Read`'s four-argument overload while `ReadOutcome`, two methods above it in the same file, called the five-argument one. One missing argument, and the shape it produced was a match with a recorded winner that went on asking that winner for their next command - observed directly as `Expected: GameEnded / But was: ResolvingTurn`, with the other eleven cases green, so the pre-fix observation names the defect and nothing else. **The check's own remarks had already written down why it would happen**: "the list of rules that can end the match grows with the content; the list of endings does not" - and then 8.39g grew the list of endings.

**`ISoloVictoryClaim` IS AN INTERFACE AND THE REASON IS TESTABILITY RATHER THAN LAYERING.** `RoundResolutionAuthority` implements it directly and hands `this` to the effect frame, so nothing is indirected in production; what the interface buys is that the capability's own seven cases assert what was asked for against a recording stand-in, with no match composed at all, and only the two boundary cases pay for a round.

**`TryRecordSoloVictory` IS NOW `TryClaim` PLUS `EndGame`, AND THE SPLIT IS THE WHOLE MECHANISM.** The refusals - an ended match, an already-claimed one, an undefined hero, a hero this match is not playing - moved down into `TryClaim` and are stated once, so the ending path and the card path cannot disagree about who may win. Nothing that called the old method changed behaviour.

**WHAT A CARD MUST NOT DO FROM INSIDE A FRAME, STATED AS A RULE THE NEXT SUCH CLAUSE INHERITS.** `EndGame` stops the turn's in-flight resolution, and the resolution in flight is the one making the claim. So a capability writes the fact and returns, and the command boundary ends the match. The window between the two is one turn command wide and never spans two, which is the same window p.8's mid-turn Lane Push has always lived in. **`Anticipate` must not claim either** - a walk judging whether the branch can be taken would otherwise end the match by merely offering the card - which is asserted directly rather than left to the handler's shape.

**THE THRESHOLD PROVED WORTH DECLARING RATHER THAN CONSTANTING** for a reason the block only guessed at: the case that reads the ledger after a Level Up spend is what says the count is live, and it needs to set a balance the card's own number does not equal.

Evidence: complete unfiltered dotnet harness **2,886/2,886 passed, 0 failed, 0 skipped** - the 2,874 baseline plus exactly this milestone's twelve cases. No schema field, corpus digest or pinned constant moved. No Cutter content.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839g1-build-the-seam-a-printed-card-claims-the-solo-victory-through-completion-evidence).

### [ ] 8.39h Compose Cutter's eighteen dealt faces and prove them at the focused tier

Observable outcome: all eighteen of her dealt faces carry composed printed text with no `PrintedTextComposition.DeferredUntil`, and her focused authoritative rule tests resolve every branch each face states.

**THE FOCUSED TIER COMES BEFORE THE GATE, IN A FOURTEENTH HERO AS MUCH AS IN A BATCH.** [POST_PITCH.md](POST_PITCH.md) records the measured reason twice over and the thirteenth-hero series re-proved it: the cheap tier finds engine defects because it can build the board a printed clause needs, and the expensive tier proves the player path over content that already works. Milestone 8.22's forty-seven-case focused tier found and fixed all four of its engine defects before a pointer met the content.

**HER BOXED HANDICAP IS THE ONE FACE THAT STAYS DEFERRED**, on the standing p.20 deferral every hero's Handicap card carries since the 2026-08-21 user decision. Few More Steps' printed values and words ship as inventory all the same, and its relationship to Walk the Plank is the same one Swift's Backup has to Reload!.

**THE PER-HERO CORPUS SWEEP IS NOW A DERIVED GUARD AND SHE IS ITS FIRST REAL TEST.** `AttackTargetBindingConditionalChooseOfferSweepTests` walks each hero's `AllDefinitions` through an explicit per-hero enumeration, and milestone 8.38d replaced the hand-listed literals with a case that derives the expected corpus from the content classes themselves - specifically so the fourteenth hero could not reopen the gap. **Cutter is the fourteenth hero, and the guard has been read and confirmed sound before this block runs.** `TheSweptCorpus_CoversEveryVerifiedContentClassTheBuildContains` is not `[Explicit]`; it reflects the assembly for sealed static classes named `*CardContent` in `WaspCardContent`'s own namespace, lowercases their prefixes, and asserts set equivalence against the owners `WholeCorpus()` yields. So it fails the moment `CutterCardContent` exists and before her line is added - which is this block's pre-fix observation, available for free rather than staged. **Two shape requirements follow from the reflection and are easy to miss:** her content class must be a STATIC class (the filter is `IsClass && IsAbstract && IsSealed`) and must sit in `GuardsDigital.Core.Cards.Verified`, or the guard will not see her and will pass while she goes unswept.

**TWO OF HER FACES PRINT BYTE-IDENTICAL TEXT AND A THIRD NEARLY DOES.** Bombardment and Barrage print the same sentence and differ only in printed Radius; Broadside prints it plus one more sentence. That is Whisper's Shadow Step and Shadow Walk again, and the composition must not collapse them - the Radius is an icon, not a word, and a shared definition would let a Radius item raise the wrong face.

**COMPOSE `CutterCardContent.AllDefinitions` LAZILY FROM THE FIRST LINE, AND KNOW THAT THIS DIVERGES FROM THE TWELVE HEROES BEFORE HER.** Tali's 8.37 dealing recorded a static-initialization cycle that failed 1,423 of 2,696 cases on one `NullReferenceException`. The thirteenth-hero session hit the same cycle at 8.38d - `SwiftCardContent.AllDefinitions` eagerly composing through `SwiftEffectContent` into `SwiftActiveEffectContent` and back - and it presented **far worse**: `NUnit failed to load GuardsDigital.EditModeTests.dll`, **zero tests discovered, the whole suite absent rather than red**, while `dotnet build` reported `0 Error(s)` throughout. A suite reporting no tests reads like tooling breakage rather than content breakage, which is what makes it expensive.

- **The tell is `dotnet test --no-build --list-tests` returning "No test is available"**, and it is worth running the moment a harness result looks wrong in a way that is not a failure count.
- **The fix is `Lazy<IReadOnlyList<CardDefinition>>` with `AllDefinitions` and `DealtDefinitions` as computed properties**, which removes the cycle edge in both directions.
- **The payoff is diagnosability, not just avoidance.** Deferring the composition converted a hidden constructor throw into an ordinary named failure that said exactly what was wrong. Adopt it at the start rather than as a repair.
- **It is a new convention.** `grep -l "Lazy<" Assets/GuardsDigital/Core/Cards/Verified/*CardContent.cs` returns `SwiftCardContent.cs` and nothing else; Tali's, Whisper's and the other eleven all compose eagerly with `= Array.AsReadOnly(...)`. Cutter follows Swift here deliberately, and the block says so rather than letting a reader think the twelve are wrong.

**THREE COMPOSITION CONVENTIONS THAT COST THE THIRTEENTH-HERO SESSION RED ASSERTIONS, VERIFIED AGAINST THE TYPES HERE:**

- **Legend of the Skies' progression carries `CardColor.None`, not `CardColor.Purple`.** `CardColor`'s own remarks say `CardProgressionProfile.UltimateCard` "states progression without a colour", and that `Purple` was added at 8.37 for RULING 21's naming step - Tali's Commune with Spirits letting a friendly hero name any of p.9's six colours - rather than for an Ultimate's own progression. The inventory's printed-value table still records her Ultimate's printed swatch as `Purple P`, because that is what the card prints; the two are different facts and both are right.
- **A Ranged Attack always carries an `AttackTargetPolicy`.** `ActionDefinition` binds `attackTargetPolicy ?? PrintedAttackTargetPolicy.ForClassification(attackRange)`, so p.13's default replaces a null and the policy is never absent. "This face narrows nothing" is read off `NarrowsTheDefault(AttackRangeKind.Ranged)` and never off a null check.
- **Evasive Shot and Tumble Shot use Swift's own builder.** `PrintedAttackTargetPolicy.RestrictedToTheStraightLine()` was minted at 8.38a for her five Reds and is exactly "Target a unit in range and in a straight line". Call it rather than constructing the policy by hand.

Acceptance checks:

- Eighteen dealt faces compose; only Few More Steps carries a deferral, and it names the standing p.20 reason.
- The derived corpus-sweep guard demands her line and is observed failing before it is satisfied; if it does not, that is reported as a defect against 8.38d rather than worked around.
- Each face's focused authoritative scenarios cover positive, negative, boundary, restore/replay and ordered-event arms, and the Attack faces additionally cover the enclosing combat outcome arms.
- A Fistful of Coins' victory branch is exercised from composed content, not only from 8.39g's own fixtures.
- Her progressed hands compose, and her three upgrade lines match the printed Tier structure the inventory recorded.
- Complete unfiltered dotnet harness and Edit Mode pass.

Non-goals: No registration, no artwork, no pick-screen row, no Play Mode journey. A composed face nothing deals is not a dealt hero.

### [ ] 8.39i Deal, register and artwork Cutter as the fourteenth hero, and put her pick card under the cursor

The registration milestone the build blocks were sized to pay for once, plus the artwork and selector work the user asked for by name.

Observable outcome: a player can pick Cutter the Sky Pirate in the hero selector, see her real pick card when the cursor rests on her row, see her real printed card fronts in hand, stand on the board as her real official token, and play her whole set.

**THE ARTWORK IS THE REAL CORPUS, THROUGH THE FOUR EXISTING TOOLS, AND NOTHING IS GENERATED.** `Prepare-CardArtwork.ps1` gains her folder and her eighteen dealt fronts; `Prepare-HeroSelectionArtwork.ps1` gains her SHA-matched pick card; `Prepare-HeroTokens.ps1` gains her manifest element, read from the bundled manifest rather than guessed; `Prepare-ShineSprites.ps1` gains her sprites and its manifest probe is extended to fourteen heroes rather than left to go stale.

**HER STANDEE IS IN THE MANIFEST UNDER AN EPITHET, WHICH IS THE THIRD TIME AND SHOULD NO LONGER SURPRISE ANYONE.** There is no `VerticalTokens` entry named `Cutter`. Hers is `Airship_Captain`, element `8583041`, identified visually against her profile and dashboard art rather than by name alone - the same way Whisper's `Outcast` and Tali's `Spiritcaller` were. Her element carries the two 302x302 faces the tool's invariant requires, so the tool needs her declaration and no widening.

**THE HOVER PICK CARD IS AN EXISTING SEAM AND THE WORK IS TO REACH IT, NOT TO BUILD IT.** Milestone 8.36e already put hover overview cards on the selector and `PrivateLobbyViews` already loads `Private/HeroSelection/<content-id>` for the hovered row. What a fourteenth hero owes it is the prepared resource under the addressing key the runtime composes, and a check that proves the hover actually resolves her face rather than silently falling back - because an absent private resource is tolerated by design everywhere else in this build, which is exactly what would let this fail quietly.

**A CONTENT-REVISION BUMP OWES THE ARTWORK CATALOG, and a green harness is structurally blind to it.** `ConfiguredCardArtworkCatalog.json` pins `definitionId` plus `contentRevision` per entry and its fixture is Unity-excluded, so the offline check - the catalog's revision set against the content constants - runs in this milestone rather than being discovered by the first unfiltered Edit Mode run.

Acceptance checks:

- Cutter is pickable in the live hero selector, her pick card renders on hover, and a configured match composes her hand without throwing.
- Her eighteen dealt fronts, her shine sprites and her two team tokens are prepared from the real Tabletopia corpus rather than generated placeholders, and the artwork bijection is re-pinned from measurement.
- The registered-handler count, every corpus digest and both scenario-digest families are re-pinned from measurement, and any schema or wire movement is stated with its cause.
- Her identity pin is minted from a complete unfiltered run and the thirteen prior pins are byte-identical on it.
- A checkout without the private corpus still runs, drawing the generated fallbacks exactly as before.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: No fifteenth hero. No new playtest package. No change to the thirteen existing heroes' art or pins beyond re-measurement.

### [ ] 8.39j Player-path gate Cutter's whole printed set through real player input

The permanent content-derived gate every dealt hero carries, and the milestone that answers the user's "make sure you do everything a hero needs" on the terms this project can actually prove.

Observable outcome: `CutterPlayerPathGateTests` derives her interaction shapes from her own declarations and binds every one of them to a named Play Mode journey that drives real controls, with no `[Explicit]` case and no skip.

**A MATRIX NAMING A SKIPPED JOURNEY IS NOT A PASSING PLAYER-PATH CHECK**, which milestone 8.36f3 established by finding twenty-two parked cases across three heroes, and 8.37a re-established by repairing the two Tali's own dealing shipped parked. This milestone ships none: a step that finds no control fails rather than skips.

**A FILTERED PLAY MODE RUN CANNOT SEE THIS GATE**, so the closing evidence is a complete unfiltered run. Two milestones in a row proved the point, and the same run is what re-pins the Play Mode baseline.

**THE VICTORY JOURNEY IS A SHAPE NO GATE HAS EVER DRIVEN.** No dealt hero's gate has ended a match through a card, because until 8.39g no card could end one. A journey that reaches A Fistful of Coins' branch holding thirteen coins ends the match inside the journey, which is a state every existing journey's teardown assumes cannot arrive. Expect that to need work in the driver rather than in the content, and treat a driver that cannot express it as a defect report rather than as a reason to park the case.

**AND THE ENEMY-CHOSEN MODAL IS DRIVEN FROM THE ENEMY'S SEAT, NOT THE CONTROLLER'S.** X Marks the Spot and A Fistful of Coins hand the choice to an enemy hero, so a journey that answers the prompt from Cutter's own seat is not exercising the printed rule. Whisper's journeys already face this and are the reference.

Acceptance checks:

- Every interaction shape her declarations produce is bound by name to an active journey; none is `[Explicit]`, none self-skips, and Few More Steps is declared unreachable with the standing p.20 reason.
- The journeys drive real controls through the UI player driver, including the grapple to an obstacle, the directional lunge, the opposite-direction retreat, the zone-crossing push, the enemy-chosen modal answered from the enemy's own seat, the minion swap, the borrowed previous-slot primary, and the solo victory.
- Undo and snapshot restore are exercised inside at least one journey of each shape.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision, and the Play Mode total is re-baselined with its cause stated.

Non-goals: No new content and no new capability. A journey that needs one is a defect report against 8.39a-8.39h, not a widening here.
