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

### [x] 8.39 Inventory Cutter the Sky Pirate's printed set and map every clause to registered capability

The inventory pass every hero gets before any of her content is written, on the same terms as milestones 8.1, 8.18a-8.18e and 8.38: transcribe the printed faces from the extraction, record what the scan actually prints rather than what a rule would predict, and map each clause to a registered capability or name it as owed.

Observable outcome: `Rules/HERO_CUTTER.md` states her nineteen faces at printed-value fidelity - Initiative, both secondary icons, colour, Tier, primary action and value, Range or Radius, item icon, upgrade pair and transcribed text - and her capability contracts are mapped at the same node-and-parameter fidelity [ROSTER_CAPABILITY_MATRIX.md](../Rules/ROSTER_CAPABILITY_MATRIX.md) uses, with every owed contract named.

**THE SLOT-POSITION TRAP HAS ONE INSTANCE IN HER SET AND IT IS THE FIRST THING THE INVENTORY MUST GET RIGHT.** Milestone 8.35z20 cost four rounds of litigation because a single secondary icon was read by its slot POSITION rather than its glyph SHAPE, and Swift's Bounce repeated the shape at 8.38. Sixteen of Cutter's faces print both secondaries with the shield above the boot. **Grappling Bolt prints exactly one, and it sits in the LOWER slot - the boot's position - while its glyph is a riveted shield.** It is a Defense 2 with no secondary Movement at all. The inventory records the glyph-shape verification, not the position.

**HER ITEM ICONS ARE IDENTIFIED AGAINST KNOWN FACES, NEVER BY EYE.** Twelve of her faces are Tier II or III and print a p.16 item icon. The recorded technique is a montage of the candidate beside faces whose channel this project has already declared in code, and Silverarrow supplies one reference for each of the six channels - Opportunity Shot (Defense), Long Shot (Initiative), Clear Shot (Radius), Warning Shot (Attack), Treetop Sentinel (Range) and Lead Astray (Movement).

**HER STAT BARS ARE CALIBRATED AGAINST A HERO WHOSE VALUES THIS PROJECT ALREADY STATES, NOT AGAINST A REMEMBERED PITCH.** Brogan's shard records Initiative 1 and Defense 8 - the minimum and maximum of p.21's scale - so his profile card fixes both the pixel pitch and the bar origin from known values, and every other bar is then read against that. A colour-based classifier does not generalise across heroes, because the frame tint bleeds into the fill; a luminance-against-trough reading does.

**SHE IS A BASE-BOX HERO, SO THE SECOND SOURCE HAS TO BE CHECKED RATHER THAN ASSUMED ABSENT**, on exactly Swift's terms. This milestone greps the second-printing rulebook for her name, her epithet and all nineteen card names before deciding which authority governs, and records the outcome either way. [Source provenance](../RULES_LEDGER.md) still applies: the cards are third-printing playtest and the rulebook is second printing, so a mismatch is edition skew rather than an error.

**AND THE DECK-BACK SENTENCE MUST BE MEASURED RATHER THAN COPIED.** Swift's shard says all nineteen of her faces share one deck back. The corpus-reading note records that nine heroes have TWO distinct deck-back hashes and names Cutter as one of them, so the sentence that is true of Swift is false of her and copying it would ship a wrong fact. Hash the backs.

**AND EVERY MEASURED CHECK CAME BACK AS HER SHARD STATES IT**, re-run at closing rather than taken from the drafting pass. The local second-printing PDF returns zero hits for `Cutter`, zero for `Sky Pirate` and zero for all nineteen card names, so the extraction is sole authority for Swift's reason rather than Ursafar's. Her nineteen deck backs hash to exactly two values - fourteen alike and five alike differently, the five being Daring Strike, Bold Thrust, Fearless Lunge, Legend of the Skies and Few More Steps - so the one-back sentence that is true of Swift is false of her, exactly as the corpus-reading note predicted. Grappling Bolt prints one secondary, drawn at the boot's height and shaped as a riveted shield: Defense 2 with no secondary Movement, resolved in one image against Walk the Plank's and Daring Strike's shield-over-boot pairs. `cards/hero-selection/randomizers-tt15__7723688__front.png` and `_selection-cards/Cutter__front.png` share one SHA-256. And the bundled manifest's element 8583041 `Airship_Captain` is a `VerticalTokens` element carrying exactly two 302x302 sprites, at (3249,2526) and (3553,2526) in `Image_0_Atlas`, which is the invariant `Prepare-HeroTokens.ps1` declares as `$declaredFaceSize = 302`.

**THE OWED SET IS STATED AS A SET RATHER THAN ASSERTED AS ONE.** Five of the shard's owed rows carried a bare `owed` when this block closed, which left the "exactly what 8.39a-8.39g build" check unverifiable from the shard alone; each now names its block, and the owed rows read 8.39a, 8.39a, 8.39b, 8.39b, 8.39c, 8.39d, 8.39d, 8.39e, 8.39f and 8.39g - the seven build blocks and no eighth.

Acceptance checks:

- `Rules/HERO_CUTTER.md` exists, is linked from the hero-shard index in [RULES_LEDGER.md](../RULES_LEDGER.md), and carries her printed-value table, her upgrade pairs, her starting hand, her boxed Handicap, and her transcribed text for all nineteen faces.
- Every printed clause is mapped to a named capability at node-and-parameter fidelity, and each is marked registered or owed; the owed set is exactly what milestones 8.39a-8.39g build.
- The one single-secondary face is recorded with its glyph-shape verification, and every item icon with the reference face it was montaged against.
- The rulebook second-source check is run and its outcome recorded, whichever way it comes out.
- The deck-back grouping is measured by hash and stated as measured.
- Her standee element and her selection card are identified from the bundled manifest and by SHA-256 respectively, not by eye, and her standee's two 302x302 faces are confirmed against `Prepare-HeroTokens.ps1`'s invariant.
- The validator passes and no governed shard crosses a threshold.

Non-goals: No content, no capability, no registration, no artwork, no schema movement. An inventory row authorizes nothing.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-839-inventory-cutter-the-sky-pirates-printed-set-and-map-every-clause-to-registered-capability-completion-evidence).

### [ ] 8.39a Build the directional attack vocabulary her Red line needs

The first of the build blocks, on 8.19b's terms: reusable vocabulary proved before any content reaches it, and no schema field moved.

Observable outcome: a printed Attack can narrow its target set to the continuation of a line its own earlier step travelled, and a printed Attack can move its actor back along the reverse of its own attack line afterwards.

**THE TWO CLAUSES ARE ABOUT A LINE THE ACTION ITSELF ESTABLISHED, WHICH IS WHY THEY BELONG IN ONE BLOCK.** Daring Strike, Bold Thrust and Fearless Lunge print "Before the attack: Move 1 space in a straight line. Target a hero adjacent to you **in the direction of the move**"; Evasive Shot and Tumble Shot print "After the attack: Move up to 2 spaces **in the opposite direction**." Neither is a property of the board or of the target - each reads a heading an earlier step of the same action fixed.

**HALF OF THE LUNGE IS ALREADY BUILT, AND 8.39e's RE-SCOPE IS WHY THIS BLOCK SAYS SO UP FRONT.** `choose-charge-line@1` already carries `minimumDistance` and `maximumDistance` as separate integers, so "Move 1 space" is `min 1, max 1`, "Move 1 or 2 spaces" is `min 1, max 2` and "Move 1, 2 or 3 spaces" is `min 1, max 3` - the enumerated distances need nothing. It already returns both a `destination` and the unit the line was chosen against, which is the shape the lunge's target needs. **Read that schema before minting anything**, because the first draft of this block and of 8.39e both assumed a move-destination narrowing did not exist, and it does.

**WHAT IS ACTUALLY NEW IS THE NARROWING FROM "ADJACENT" TO "ALONG THE SAME LINE".** Brogan's Mad Dash lets the destination be adjacent to an enemy unit in any of p.19's six directions and targets that unit. Cutter's faces are a strict subset: after moving along heading D, the target must be adjacent **and** on the continuation of D, which is exactly one space. So the candidate set is one where Brogan's is up to six, and that narrowing is the block's real work - probably another optional parameter on the declaration that already returns the pair, on 8.39e's discipline of defaulting to the behaviour every existing declaration has today.

**THE OPPOSITE-DIRECTION MOVE IS THE GENUINELY UNBUILT HALF, AND ITS NEAR MISS IS REAL.** `attacked-target-space@1` already captures the target's space before p.14's consequences run - Brogan's step-7 walk uses it to move the attacker INTO that space - so the actor's space and the attacked space together already name the line. What does not exist is travelling the reverse of it. Probe whether that composes out of the existing pair plus a direct move before minting a declaration.

**`actor-moved-this-action@1` IS NOT THE ANSWER AND THE BLOCK SHOULD NOT REACH FOR IT.** It is Tigerclaw's Combat Reflexes predicate - a boolean saying the attacker stands somewhere other than where the Attack started. It carries no heading, so it cannot narrow anything by direction.

**THE DEGENERATE CASES ARE THE POINT OF THE FOCUSED TIER HERE.** A one-space move fixes a heading unambiguously; a move p.19 stops early may not have travelled its printed distance; and the second branch of each lunge ("Target a unit adjacent to you") performs no move at all and so has no heading to read. The block settles each and records the reading rather than leaving it to the composing milestone.

Acceptance checks:

- An Attack declaring the direction narrowing offers exactly the hero on the continuation of the heading just travelled, asserted as a set equality on a board where the unnarrowed adjacent set holds several heroes.
- The narrowing is read by the same query the offer, the target choice and the resolution all use.
- A move stopped early still fixes the heading it travelled, and the branch that performs no move offers no directional target rather than offering all of them.
- Brogan's three charge faces are byte-identical in candidate set, asserted rather than assumed, if this block touches the declaration they share.
- The opposite-direction move travels only along the reverse of the attack's own line, stops at the first thing p.19 stops it at, and is declinable where the card prints "up to".
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to any existing face's target policy. No new Attack primary. No new move capability where an existing one takes a parameter.

### [ ] 8.39b Build the obstacle-target and grapple-move vocabulary her Silver basic needs

Observable outcome: a printed face can target an obstacle rather than one of p.19's units, require the line to it to be clear, ignore the immunity that would otherwise protect it, and travel that line until it stands adjacent.

**GRAPPLING BOLT IS THE ONLY FACE IN THE DEALT ROSTER THAT TARGETS SOMETHING THAT IS NOT A UNIT**, and that is the one genuinely new thing in this block. Every existing target declaration selects among p.19's units. `ObstacleQuery` knows what an obstacle is, but as a rule about whether a space may be ENTERED rather than as a thing that can be chosen.

**THE OTHER THREE CLAUSES HAVE MORE MACHINERY BEHIND THEM THAN THE FIRST DRAFT OF THIS BLOCK CREDITED, AND THE BLOCK MUST PROBE BEFORE IT MINTS.** The re-scope of 8.39a and 8.39e came from reading schemas rather than names, and the same reading changes this block's shape too:

- **"Ignore immunity" is an existing declaration mechanism, not a new one.** `EffectNodeDefinition.ImmunityOverrides` lets a node declare the activities it overrides immunity for; `IEffectImmunityOverrideCapabilityHandler.SupportedImmunityOverrideActivities` is how a handler declares which it accepts; and `EffectDefinitionValidator` already rejects the mismatch as `UnsupportedImmunityOverride`. Swap and direct move both support it today behind a `supportsImmunityOverrides` flag. `RuleActivity.All` is documented as "p.18's unqualified 'if a unit is immune'", which is the arm this clause most likely names. **So the cost here is the new handler implementing an interface that already exists**, not a mechanism.
- **"Move towards that obstacle until you are adjacent" is charge-line semantics.** `choose-charge-line@1` already chooses a straight-line destination that is adjacent to a named thing, between a `minimumDistance` and a `maximumDistance`, and returns both the destination and the thing. Grappling Bolt's destination is exactly "the space on this line adjacent to the target". The blocker is that its `unitKind` parameter ranges over `RosterUnitKind`, whose members are Hero, Minion and Unit. **Probe whether an `Obstacle` member on that enum is the honest reading and what it costs** - Swift's 8.38a measured that a member on an enum the payload already writes is free where a new field is a schema version, and this is the same question.
- **"With no obstacles between you" may compose rather than be built.** `StraightLineQuery` answers the line and `ObstacleQuery` answers what stands on it. Check whether any existing declaration already requires a clear line before minting a predicate.

**SO THIS BLOCK'S REAL SIZE IS UNKNOWN UNTIL IT PROBES, AND IT SHOULD PROBE FIRST.** If the three reductions above hold, it is one new target declaration plus an enum member plus an interface implementation, which is ordinary. If they do not, it is four contracts and exceeds 8.19b, the ceiling this series inherits - and then it sheds its tail into **8.39b1** the way 8.38c shed 8.38c1 and 8.38c2, changing no total scope and no acceptance check.

**"IGNORE IMMUNITY" IS STILL AN OPEN READING AND THIS BLOCK SETTLES IT.** p.18's immunity is about units, and an obstacle is not a unit, so either the clause is defensive boilerplate that does nothing here or it reaches whatever protection an obstacle carries. Her shard records it as CU-Q2. Note that the mechanism existing does not settle the reading - it only makes whichever reading wins cheap to express.

**"UNTIL YOU ARE ADJACENT" DOES NOT GUARANTEE ARRIVAL.** p.19 stops a move at the first thing that stops it, so a unit standing in the line ends the move early and the actor never reaches the obstacle. The block proves that rather than assuming the clause promises arrival.

Acceptance checks:

- A face declaring the obstacle target offers exactly the obstacles in range, on a straight line, with a clear intervening line - asserted as a set equality against a board where each of those three conditions excludes a different candidate.
- An obstacle behind another obstacle is not offered, which is what "with no obstacles between you" means.
- The immunity reading is settled, implemented through the existing override declaration, and recorded with its reason.
- The move travels the line and stops adjacent to the target, and stops earlier where p.19 stops it, leaving the actor short of the obstacle rather than teleporting it.
- If `RosterUnitKind` gains a member, the payload cost is measured and stated, and every existing declaration over that parameter is byte-identical.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to `ObstacleQuery`'s enterability rule. No general widening of unit targeting to non-units beyond the declaration this block mints.

### [ ] 8.39c Build the zone-crossing push consequence her Gold line needs

Observable outcome: a face that pushes an enemy hero can make a consequence conditional on the push having carried that hero out of the zone it started in.

**THE MOMENT AND THE TRIGGER BOTH ALREADY EXIST, AND ONLY THE QUALIFIER IS NEW.** Walk the Plank and Few More Steps print "Push an enemy hero adjacent to you up to 4 spaces; **if that hero is pushed into another zone**, that hero discards a card, or is defeated." `after-push-trigger@1` with `AfterPushTriggerSubject.EnemyHero` is Sabina's Big Sodding Gun and is exactly the declaration this needs; `TimingCheckpoint.ObjectPushApplied` is the moment. `BoardMapDefinition.GetZoneForSpace` answers which zone a space is in, and `AdjacentMoveAuthority` already compares an origin zone against a destination zone. **Nothing here is a new observation point.**

**THE QUALIFIER MUST NOT BE A FIELD ON `AfterPushTrigger`.** Sabina references that type today, and the rule this catalog has followed since 8.20 - generalised at 8.35z28 and re-stated at 8.38b - is that a shared declaration gaining a field moves every hero who already referenced it. The zone test is a predicate beside the trigger, not a widening of it.

**IT FIRES ON DISPLACEMENT, NOT ON THE ATTEMPT, AND THE EXISTING TYPE ALREADY SAYS SO.** `AfterPushTrigger`'s own remarks record that p.18's "if you push" is what happened rather than what was tried, so a push that moved nothing reaches no trigger. The zone test inherits that for free and the block asserts it rather than restating it: a hero pushed zero spaces has not been pushed into another zone.

**"DISCARDS A CARD, OR IS DEFEATED" IS ALREADY SETTLED AND IS NOT PART OF THIS BLOCK'S NEW WORK.** Rule-system ledger question 13 settled it on Arien's identical sentence and Whisper's Greater Good composes it unchanged: one mandatory step with two ways to complete. This block reuses that composition and proves it reaches the pushed hero rather than the actor.

Acceptance checks:

- The consequence fires when and only when the pushed hero's origin and destination zones differ, proven on one board with a push that crosses and a push of the same distance that does not.
- A push that displaces nothing fires nothing.
- A push that crosses a zone boundary and comes back within the same push is judged on origin versus final destination, and the block states that reading explicitly.
- Sabina's existing after-push declaration is byte-identical in behaviour, asserted rather than assumed.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to `AfterPushTrigger`'s shape. No new push capability - `synthetic-object-push@3` already carries the distances both faces print.

### [ ] 8.39d Build the subject-adjacency predicate and excluded repeat her Blue line needs

Observable outcome: a radius selection can narrow its subjects to those standing beside a unit of the SUBJECT's own team, and a card-text repeat can exclude the subject its first performance took.

**THE ADJACENCY PREDICATE IS A SIBLING PARAMETER, NOT A NEW SELECTION, AND THE NEAR MISS IS EXACT.** Bombardment, Barrage and Broadside print "An enemy hero **adjacent to another enemy unit** and not adjacent to you discards a card, if able." Sabina's selection already carries `adjacentToFriendlyMinion`, which narrows the same radius-hero selection by adjacency - measured from the ACTOR's team and restricted to minions. Cutter's clause measures from the TARGET's team and says unit. That is two parameter differences on a declaration that already carries one, which is what the 8.38b lesson predicts and what this block confirms before minting anything.

**"AND NOT ADJACENT TO YOU" IS ALREADY REGISTERED** as `target-is-not-adjacent@1` and needs nothing.

**THE REPEAT'S EXCLUSION IS THE GENUINELY NEW HALF.** Broadside alone adds "May repeat once on a different target." p.10's repeat is registered and milestone 8.38c2's per-grant target exclusion is the nearest shape in the build, so the question this block answers is whether that scope can carry a card-text repeat's subject exclusion or whether the two are different seams wearing one description.

**AND MILESTONE 8.35z15's RULE IS EXPECTED TO APPLY RATHER THAN BE RE-DECIDED.** That milestone established that a card-text repeat with no legal subject is not offered. Broadside with exactly one legal subject on the board must therefore not offer its repeat. The block confirms this rather than assuming it, because 8.35z15 found three shipped journeys that had only ever DECLINED their own named prompt - a shape that passes without ever exercising the rule.

Acceptance checks:

- The predicate admits exactly the enemy heroes standing beside another unit of their own team, asserted as a set equality on a board where an isolated enemy hero and an enemy hero beside a friendly-to-the-actor unit are both excluded for different reasons.
- A hero adjacent to two qualifying units is offered once, not twice.
- Sabina's existing parameter is unchanged in behaviour and her candidate sets are byte-identical.
- The repeat excludes the first subject, and is not offered at all when no second legal subject exists.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to Sabina's declaration beyond the parameter this block adds. No change to p.10's repeat for any existing face.

### [ ] 8.39e Widen the existing charge line so her Green faces can ignore obstacles

**THIS BLOCK WAS RE-SCOPED BEFORE IT RAN, AND THE RE-SCOPE IS THE POINT.** It was drafted as "build a destination-constrained move", on the reading that the catalog could narrow a p.18 Place by its destination's neighbours but not a p.19 Move. That reading was wrong, and reading the catalog's PARAMETERS rather than its capability names is what found it - the same lesson milestone 8.38b measured and the second time it has paid.

Observable outcome: the charge line her three Green faces need travels while ignoring obstacles, and every declaration written before this milestone keeps its exact candidate set.

**`choose-charge-line@1` IS ALREADY THIS MOVE.** Brogan's Mad Dash prints "Before the attack: Move 2 spaces in a straight line **to a space adjacent to an enemy unit**, then target that unit", and Cutter's Brace for Impact prints "Move 3 spaces in a straight line, ignoring obstacles, **to a space adjacent to an enemy hero**; that hero discards a card, if able." They are the same sentence with different numbers, a different unit noun and one extra clause. The capability's schema already carries:

- `minimumDistance` and `maximumDistance` as separate integers - so "Move 3 spaces" is `min 3, max 3`, "Move 3 or 4 spaces" is `min 3, max 4`, and "Move 3, 4 or 5 spaces" is `min 3, max 5`. **The exact-distance arm this block was going to build already exists** and Brogan, Ursafar and the showcase all bind it.
- `unitKind` over `EffectIntegerDomain.AllExcept(RosterUnitKind.None)`, and **`RosterUnitKind.Hero` is already a member** - so "adjacent to an enemy hero" is a value the domain admits today rather than a narrowing to be built.
- a `destination` coordinate output, which is what the following Move and the discard both read.

**SO THE ONLY GAP IS `ignoresObstacles`, AND IT IS ONE OPTIONAL PARAMETER.** `choose-line-destination@1` has carried that parameter since Misa; `choose-charge-line@1` has not, because no printed charge had needed it. It is added here **optional and defaulting to false**, which is the discipline Sabina's `adjacentToFriendlyMinion` records in its own remarks: every declaration written before this milestone keeps its exact candidate set, and Brogan's three faces must be byte-identical afterwards.

**THE HERO THE DESTINATION WAS BESIDE IS STILL A QUESTION, AND IT IS THE ONLY DESIGN WORK LEFT HERE.** Brogan's sentence says "then target **that** unit", so his composition takes the unit the destination was chosen against. Cutter's says "**that hero** discards a card", which reads the same way - but a destination adjacent to two enemy heroes is a board state his faces already reach and hers will too. Settle it, record the reading, and prove it; the natural answer is that the charge line is chosen against a named unit and that unit is the output, so the ambiguity never arises.

Acceptance checks:

- The charge line admits exactly the destinations at a distance within the printed bounds, on a straight line, and adjacent to an enemy hero - asserted as a set equality on a board where each condition excludes a different candidate.
- The exact-distance arm offers nothing where no destination at exactly that distance qualifies, rather than falling back to a shorter move.
- Binding `ignoresObstacles` true changes the reachable set and does not change the adjacency requirement; leaving it unbound reproduces today's behaviour exactly.
- **Brogan's Mad Dash, Bullrush and Throwing Axe candidate sets are byte-identical**, asserted rather than assumed, because this block edits the declaration they depend on.
- The hero the destination was chosen against is the one the consequence reads, on a destination adjacent to two enemy heroes.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to `choose-line-destination@1`. No new move capability - this block was drafted to mint one and must not, now that the existing one has been read properly.

### [ ] 8.39f Build the previous-turn-slot borrow pool her Ultimate needs

Observable outcome: a face can borrow the printed primary action of a card sitting in the turn slot before the one being resolved.

**THIS IS THE DIRECT SIBLING OF MILESTONE 8.38c AND SHOULD BE THE CHEAPEST BLOCK IN THE SERIES.** Legend of the Skies prints "The first time each turn after you perform a primary action, you may perform the primary action of a card in the previous turn slot." `borrow-primary-action@1` is the verb, registered with Ursafar at 8.35z50; `BorrowedActionPool.OwnedRightmostResolvedCard` was added at 8.38c and reads `EffectRosterState.TurnSlots` filtered to the current round. This pool reads the same ordering one slot back instead of rightmost.

**EVERY LESSON 8.38c MEASURED APPLIES UNCHANGED AND MUST BE RE-PROVED RATHER THAN ASSUMED INHERITED.** Narrow the ordered set BEFORE the per-card filters, so a previous-slot card the borrow refuses empties the pool rather than falling through to the slot behind it - the face names one slot. The borrowing card is its own first false answer. `RoundTurnSlot` already refuses an assignment naming another hero's card, so a hero filter is defence in depth rather than the guarantee. Absent roster state fails closed to an empty pool.

**WHAT IS ACTUALLY OPEN IS WHOSE SLOT IT IS, AND THE BLOCK SETTLES IT.** The face says "**a** card in the previous turn slot", not "**your** card - which is her shard's CU-Q3. 8.38c's pool is owner-scoped and `RoundTurnSlot`'s refusal points the same way, but pointing is not settling. Take a reading, record it, and prove it on a board where the two readings differ.

**AND TURN 1 HAS NO PREVIOUS SLOT.** p.6's turn area is emptied at the round boundary, so the first turn of a round offers this clause nothing. That is the same "empty pool rather than fall through" answer as above and the block asserts it directly.

**THE TRIGGER HALF IS NOT NEW.** "The first time each turn after you perform a primary action" is the once-per-turn machinery milestone 8.38c1 exercised, over a primary action rather than 8.38c2's basic card. Probe whether an existing `UltimateTriggerCondition` member already covers it before minting a sixth; Tigerclaw's reads a basic ACTION and Swift's reads a basic CARD, and Cutter's reads any primary action, which may be a widening none of them can absorb - or may be one that already exists.

Acceptance checks:

- The pool names at most one card, is empty on the first turn of a round, and follows the turn area's committed order rather than any instance-id ordering, proven on a board where the two disagree.
- A previous-slot card the borrow refuses empties the pool, on a board where a fall-through would have succeeded.
- The owner scope is settled, implemented and recorded, and proven on a board where the two readings differ.
- The trigger fires once per turn and not twice, and fires after a primary action rather than after a basic card.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Cutter content. No change to Ursafar's, Swift's or Tali's borrow pools, nor to any existing Ultimate trigger.

### [ ] 8.39g Build the solo victory condition A Fistful of Coins prints

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

Non-goals: No Cutter content - the face that reaches this is composed at 8.39h. No new victory condition for any other hero. No change to p.8's or p.13's endings beyond the re-measurement this block forces.

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
