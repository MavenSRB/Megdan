# Phase 8 - Thirteenth-hero dealing: Swift the Sharpshooter

Return to [roadmap](../ROADMAP.md). Current/active/next authorization lives only in [the brief](../SESSION_BRIEF.md).

## Authorization and standing

On 2026-09-11 the user re-opened the throttle the 2026-09-07 brakes direction closed, in their own words: "Can you make proper documentation milestones and implement the next hero? Make sure to use real hero art, add it to the pick screen with pick card showed on mouse over, and making absolutely sure that all the cards work in all scenarios?" Asked which of the twenty undealt heroes the corpus holds, they named **Swift**. This shard owns that work and nothing else: the brakes direction still governs every other parked item, and naming a thirteenth hero re-opens the roster, not the parked presentation program, the card-authoring workbench, or a new playtest package.

Swift is the **thirteenth** hero this build deals and the first taken from outside the twelve-hero package arc that milestone 8.37 closed. She is a base-box hero, so the second-printing rulebook in this repository is available to her as a second source in a way it was not for the two Devoted Hero pack heroes - which is a check to run, not an assumption to make, and milestone 8.38 runs it.

**She is a three-star hero on rulebook p.21's complexity scale, which is the highest this project has dealt.** Tali was the only other hero above one star, and she cost eight new handler types. The series below is therefore laid out the way [POST_PITCH.md](POST_PITCH.md)'s batch decision says a hero's vocabulary should be: an ordinary-sized lettered series that BUILDS the predicted vocabulary against showcase reachability, then one milestone that composes her faces, then one that registers and deals, then the permanent gate. Building is schema-neutral; only registering costs a version.

## The audit that scopes this series

Milestone 8.38 owns the full inventory and the exact mapping. What the pre-authorization read of her nineteen extracted faces already establishes, and what the series is sized against:

- Her set is 19 faces: one Gold basic (Reload!), one Silver basic (Bounce), five Red ranged Attacks, five Blue Skills, five Green Skills, the Purple Ultimate (Bullet Time), and one Handicap (Backup) that p.6 returns to the box. Eighteen are dealt.
- **Most of her text is already registered vocabulary.** `adjacent-terrain-presence@1` (Tigerclaw's Blend Into Shadows) is her three "not adjacent to terrain" Green faces' predicate; `ExactlyPrintedRange` (Silverarrow's RULING 1) is Snipe's "at maximum range"; `fast-travel@1` is Mobile Scout's second sentence; `synthetic-object-push@3`, `synthetic-direct-move@1`, `synthetic-place-in-radius@1`, `synthetic-discard-chosen-card@2`, `synthetic-defeat-hero@1`, `synthetic-defeat-minion@1`, `gain-resource@1` and `borrow-primary-action@1` all already exist and all are named by her faces.
- What is **not** registered is named by three groups of faces, and the three build milestones below are drawn on exactly those lines rather than on a word count.
- **Measured after 8.38a and 8.38b:** two of the three groups were smaller than the audit costed them. The proximity clause is a fourth band on an enum the snapshot already writes rather than a new field, and Drop Trooper's multi-unit push was already built for Wasp. What remains genuinely new is 8.38c's three contracts.

## Series

### [x] 8.38 Inventory Swift the Sharpshooter's printed set and map every clause to registered capability

The inventory pass every hero gets before any of her content is written, on the same terms as milestones 8.1 and 8.18a-8.18e: transcribe the printed faces from the extraction, record what the scan actually prints rather than what a rule would predict, and map each clause to a registered capability or name it as owed.

Observable outcome: `Rules/HERO_SWIFT.md` states her nineteen faces at printed-value fidelity - Initiative, both secondary icons, colour, Tier, primary action and value, Range or Radius, item icon, upgrade pair and transcribed text - and her capability contracts are mapped at the same node-and-parameter fidelity [ROSTER_CAPABILITY_MATRIX.md](../Rules/ROSTER_CAPABILITY_MATRIX.md) uses, with every owed contract named.

**THE SLOT-POSITION TRAP HAS ONE INSTANCE IN HER SET AND IT IS THE FIRST THING THE INVENTORY MUST GET RIGHT.** Milestone 8.35z20 cost four rounds of litigation because a single secondary icon was read by its slot POSITION rather than its glyph SHAPE. Sixteen of Swift's faces print both secondaries with the shield above the boot. **Bounce prints exactly one, and it sits in the LOWER slot - the boot's position - while its glyph is a riveted shield.** It is a Defense 2 with no secondary Movement at all. The inventory records the glyph-shape verification, not the position.

**HER ITEM ICONS ARE IDENTIFIED AGAINST KNOWN FACES, NEVER BY EYE.** Twelve of her faces are Tier II or III and print a p.16 item icon. The recorded technique is a montage of the candidate beside faces whose channel this project has already declared in code, and Silverarrow supplies one reference for each of the six channels - Opportunity Shot (Defense), Long Shot (Initiative), Clear Shot (Radius), Warning Shot (Attack), Treetop Sentinel (Range) and Lead Astray (Movement).

**SHE IS A BASE-BOX HERO, SO THE SECOND SOURCE HAS TO BE CHECKED RATHER THAN ASSUMED ABSENT.** Ursafar's and Tali's shards record that the pack has no second source and that the extraction is therefore sole authority. Swift is not from that pack, so this milestone greps the second-printing rulebook for her name and for all nineteen card names before deciding which authority governs, and records the outcome either way. [Source provenance](../RULES_LEDGER.md) still applies: the cards are third-printing playtest and the rulebook is second printing, so a mismatch is edition skew rather than an error.

**AND THE CHECK CAME BACK EMPTY, WHICH IS A DIFFERENT REASON FOR THE SAME AUTHORITY.** The local second-printing PDF returns zero hits for `Swift`, zero for `Sharpshooter`, and zero for all nineteen card names; its only two hits for the substring `Snipe` are p.21's Sniper ROLE description. That book's worked examples name seven heroes - Wasp, Xargatha, Arien, Brogan, Dodger, Sabina and Tigerclaw - and nobody else, so her absence is its scope rather than evidence about her printing. The extraction is sole authority for her faces, as it is for Ursafar and Tali, and the reason is recorded as hers rather than borrowed from the pack ruling that does not apply to her.

Acceptance checks:

- `Rules/HERO_SWIFT.md` exists, is linked from the hero-shard index in [RULES_LEDGER.md](../RULES_LEDGER.md), and carries her printed-value table, her upgrade pairs, her starting hand, her boxed Handicap, and her transcribed text for all nineteen faces.
- Every printed clause is mapped to a named capability at node-and-parameter fidelity, and each is marked registered or owed; the owed set is exactly what milestones 8.38a-8.38c build.
- The one single-secondary face is recorded with its glyph-shape verification, and every item icon with the reference face it was montaged against.
- The rulebook second-source check is run and its outcome recorded, whichever way it comes out.
- The validator passes and no governed shard crosses a threshold.

Non-goals: No content, no capability, no registration, no artwork, no schema movement. An inventory row authorizes nothing.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-838-inventory-swift-the-sharpshooters-printed-set-and-map-every-clause-to-registered-capability-completion-evidence).

### [x] 8.38a Build the printed-target vocabulary Swift's Red line needs

The first of three ordinary-sized build blocks, on 8.19b's terms: reusable vocabulary proved before any content reaches it, and no schema field moved.

Observable outcome: a printed Attack can declare that its target stands in a straight line from the attacker, and that its target is not adjacent to the attacker, and both narrowings are read by one answer for the offer, the choice and the resolution.

**THE TWO NARROWINGS ARE DECLARATIONS ON `PrintedAttackTargetPolicy`, NOT EFFECTS**, for the reason milestone 8.10 already recorded when it put Wasp's "not in a straight line" there: a sentence about what an Attack may target belongs to the Attack's own target set rather than to milestone 8.5's card-text seam, because that is what keeps the greyed-out offer, the target choice and the resolution reading one answer. Swift prints the complement of Wasp's clause on three faces - Snipe's "at maximum range, and in a straight line", and Prepared Shot's and Killshot's "in range, in a straight line, and not adjacent to you".

**THE PROXIMITY CLAUSE IS A FOURTH `AttackTargetReach` BAND AND NOT A FOURTH DIMENSION, and that is what kept the schema still.** The plan said "the policy gains a fourth dimension for proximity"; building it showed that reading to be both dishonest and expensive. Dishonest, because the reach enum already carries three bands over one printed number - adjacency alone, everything up to the ceiling, and the ceiling exactly - so "in range and not adjacent to you" is the fourth band in that family and not a separate question standing beside it. Expensive, because `AuthoritativeSnapshotSerializer` writes a modal bullet's target policy field by field: a genuinely new field is one more integer per bullet in the payload, which is a schema version and everything a version move owes. A member on an enum the payload already writes is neither. **The check to run before adding a field to a definition type is whether a snapshot writes that type**, and here it does.

**THE REFUSAL THE EXISTING TYPE ALREADY CARRIES HAS A MIRROR HERE, AND IT MUST BE STATED RATHER THAN DISCOVERED.** The type already refuses `NotInStraightLine` paired with `AdjacentOnly`, because p.19 counts an adjacent space as a straight line and the pair could never target anything. The new members carry the same obligation in the other direction: `InStraightLine` paired with an adjacency exclusion is exactly what Prepared Shot prints and must be legal, while a proximity exclusion paired with `AdjacentOnly` is a printed Attack with no possible target and is refused in the constructor.

**THE REFUSAL THE EXISTING TYPE CARRIES HAS A MIRROR, AND THE MIRROR IS A TAUTOLOGY RATHER THAN AN IMPOSSIBILITY.** The type already refuses `NotInStraightLine` beside `AdjacentOnly`, because p.19 counts an adjacent space as a straight line and the pair could never target anything. `InStraightLine` beside `AdjacentOnly` is the same pair from the other side and admits exactly what it would have admitted anyway - so it is refused too, for a reason of its own: `NarrowsTheDefault` would report a narrowing that is not there, and no printed face states the pair.

**THE SHOWCASE HALF OF THE PLAN DOES NOT APPLY HERE, and saying so is better than staging content to satisfy a rule that is not being asked.** `ConfiguredCardMatrixTests`' reachability rule has two halves, both about the effect catalog: a *capability* must be named by some card a match can deal, and a new *handler implementation* must additionally be reachable from a showcase card. This milestone adds neither - no capability id, no handler type, no registration - because both narrowings are enum members on a definition type that a card declares directly. What proves them before content exists is the focused tier: eight cases including the exact partition of p.13's default set by the two line members, and the exact removal of the adjacent units by the band. Printed consumers arrive at 8.38d.

Acceptance checks:

- `AttackTargetLine.InStraightLine` and `AttackTargetReach.PrintedRangeBeyondAdjacent` exist and are read by the same query the offer, the target choice and the resolution all use. **Amended during implementation:** the plan also asked for showcase content declaring them; no capability or handler is added, so the rule that would require it is not engaged, and the focused tier is the evidence instead.
- The two line members partition p.13's default set exactly on one board, and the band removes exactly the adjacent units from it - both asserted as set equalities rather than named units, with each case proving its own board discriminating first.
- The tautological pairing is refused at construction citing p.19, and the pairing Prepared Shot prints is not.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Swift content. No registration of a handler her faces do not reach. No change to any existing face's policy.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-838a-build-the-printed-target-vocabulary-swifts-red-line-needs-completion-evidence).

### [x] 8.38b Build the straight-line and off-line placement vocabulary her Blue line needs

Observable outcome: a face can place its own actor into a space that is in radius and on a straight line from where it stands, or into a space that is in radius and deliberately off every straight line from it.

**HER FIVE BLUE FACES SPLIT TWO AND THREE ON EXACTLY THIS SEAM.** Steam Jump, Assault Jump and Drop Trooper print "Place yourself into a space in a straight line in radius"; Delayed Jump and Mobile Scout print "Place yourself into a space in radius not in a straight line from you". `synthetic-place-in-radius@1` already answers the radius half and `StraightLineQuery` already answers the line half in both directions, so what this block builds is the destination qualifier that composes them, not a new geometry. `synthetic-place-in-radius-in-straight-line@1` and `synthetic-place-in-radius-off-every-straight-line@1` are two declarations rather than one carrying a direction, by the rule this catalog has followed since milestone 8.20 and milestone 8.35z28 generalised: a printed clause that qualifies a declaration belongs in the identity of the thing declared, because a shared declaration gaining a field moves every hero who already referenced it.

**BOTH ARE DEFERRED HANDLERS, WHICH IS WHAT KEEPS THIS BLOCK SCHEMA-NEUTRAL.** Milestone 7.9's gate refuses a registration no dealt card can start, and the registered set is what a snapshot serializes - so a capability built for a hero nobody deals yet moves no pinned byte. `SyntheticPlacementCapabilities.DeferredHandlers()` had been deliberately kept as an empty list against exactly this need; it now holds these two and nothing else, and milestone 8.38e is what moves them across.

**AND THE PUSH HALF OF THE PLAN TURNED OUT TO BE ALREADY BUILT, which is worth recording rather than quietly dropping.** The block predicted that Drop Trooper's "Push up to two enemy units adjacent to you up to 2 spaces" needed a multi-subject push composed out of `choose-target-set@1` and a batch move. It needs neither: `synthetic-object-push@3` has carried a `maximumTargets` parameter since milestone 8.8, minted for Wasp's kinetic Skills, whose Kinetic Repulse prints "Push up to 2 enemy units adjacent to you 3 spaces" - Drop Trooper's sentence with different numbers. Reading the catalog's parameters as well as its capability names is what found it; the audit at 8.38 had read the names.

Acceptance checks:

- Both placement declarations exist, are held in the deferred list, and are absent from the registered one - asserted as the exact pair, so 8.38e moving one and forgetting the other is a failure rather than a half-green.
- The two candidate sets partition the printed disk exactly on one board, and the on-file set is six files cut to the printed Radius, checked against the geometry rather than a remembered coordinate list.
- The off-file set excludes every one of the actor's neighbours, which is p.19's adjacency rule showing through a clause that does not mention it.
- An occupied space is a destination for neither, so the qualifier narrows the emptiness rule rather than replacing it.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Swift content. No change to the existing pushes or to any placement policy a dealt face already names. **Amended during implementation:** the multi-subject push this block planned to build already exists, so it builds nothing for Drop Trooper's second sentence.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-838b-build-the-straight-line-and-off-line-placement-vocabulary-her-blue-line-needs-completion-evidence).

### [ ] 8.38c Build the borrow, bounty and basic-card-resolution vocabulary her Gold, Green and Ultimate lines need

The third build block, and the one carrying what is genuinely new rather than compositional.

Observable outcome: a face can borrow the primary action of the rightmost card its owner has resolved this round; a face can promise a coin for each of the first one or two enemy minions defeated in its radius on the owner's next turn; and an Ultimate can trigger on a basic CARD being resolved rather than on a basic ACTION completing, and can forbid its grant from taking the same enemy hero twice in one turn.

**"RIGHTMOST RESOLVED CARD" IS A SELECTOR, AND `borrow-primary-action@1` IS ALREADY THE VERB.** Milestone 8.35z50 registered the borrow with Ursafar, whose three faces borrow from "one of your active cards with an active effect". Reload! and Backup borrow from a different set, and p.6's turn area is what makes "rightmost" a fact rather than a presentation detail: the cards a hero has resolved this round sit in committed order. The selector is new; the borrow, its suspension of the borrowing Skill, and its resumption after the borrowed action completes are all milestone 8.36f's already-corrected machinery.

**THE BOUNTY IS A NEXT-TURN ACTIVE EFFECT WITH A COUNT, AND THE COUNT IS THE PART WITH NO PRECEDENT.** Mark for Death promises one coin the first time an enemy minion in radius is defeated on her next turn; Hunting Season promises one coin each of the first two times. `gain-resource@1` awards the coin and the Active-effect family already carries next-turn lifetimes, but no existing effect counts its own firings against a printed bound. **A count that must survive a snapshot is the one thing in this series that can move the schema**, and the block decides that question on evidence rather than by preference: an effect whose remaining count is re-derived from the round's own recorded defeats moves nothing, and one that stores a counter does. The block takes the re-derived shape if it is faithful and records why if it is not.

**BULLET TIME TRIGGERS ON A CARD, NOT ON AN ACTION, AND THAT IS A FIFTH `UltimateTriggerCondition` RATHER THAN A WIDENING OF THE SECOND.** Tigerclaw's Cloak and Daggers already reads `AfterControllerPerformsBasicAction` - "After you perform a basic action you may repeat it once" - and widening that member would make Swift's Ultimate fire after a Hold or a Fast Travel spent from a basic card. Her face says "After you resolve a basic card you may perform the primary action on that card", which names the card's own resolution and then borrows that card's PRINTED PRIMARY, which need not be the action that was performed. The same reasoning milestone 8.19c recorded for the second member applies unchanged: two different sentences on two different cards are two conditions.

**AND ITS SECOND CLAUSE IS A RESTRICTION SCOPED TO THE GRANT, NOT TO THE TURN.** "You cannot target the same enemy hero twice in the same turn this way" forbids a repeat target only for actions taken through this Ultimate; an ordinary Attack in the same turn is untouched. The scope is declared on the grant, which is the same seam milestone 8.35l used to make a restriction name its own blocker in the offer.

Acceptance checks:

- The rightmost-resolved-card selector, the counted next-turn bounty and the basic-card-resolution trigger exist and are each reachable from showcase content.
- The selector is empty on a turn with nothing resolved, names one card when one is resolved, and follows the turn area's committed order rather than any presentation order.
- The bounty pays exactly its printed count, pays nothing for a minion defeated outside the radius, and survives a snapshot restore and an Undo with the same remaining count.
- An Ultimate declaring the new trigger fires on a resolved basic card and not on a basic action performed from a non-basic one; the per-grant target exclusion greys a repeat target and names its own reason, and leaves an ordinary action's targets alone.
- Whether the schema moves is decided by measurement and recorded either way.
- Complete unfiltered dotnet harness and Edit Mode pass.

Non-goals: No Swift content. No change to Wasp's, Tigerclaw's or Misa's existing Ultimate triggers. No change to Ursafar's borrow candidate set.

### [ ] 8.38d Compose Swift's eighteen dealt faces and prove them at the focused tier

Observable outcome: all eighteen of her dealt faces carry composed printed text with no `PrintedTextComposition.DeferredUntil`, and her focused authoritative rule tests resolve every branch each face states.

**THE FOCUSED TIER COMES BEFORE THE GATE, IN A THIRTEENTH HERO AS MUCH AS IN A BATCH.** [POST_PITCH.md](POST_PITCH.md) records the measured reason twice over: the cheap tier finds engine defects because it can build the board a printed clause needs, and the expensive tier proves the player path over content that already works. Milestone 8.22's forty-seven-case focused tier found and fixed all four of its engine defects before a pointer met the content.

**HER BOXED HANDICAP IS THE ONE FACE THAT STAYS DEFERRED, on the standing p.20 deferral every hero's Handicap card carries** since the 2026-08-21 user decision. Backup's printed values and words ship as inventory all the same, and its relationship to Reload! is the same one Crackling Dagger has to Magnetic Dagger and Keep the Distance to Shoot and Scoot: the Handicap is the more restrictive of a pair.

**AND A HERO'S CORPUS IS ENUMERATED BY NAME IN ONE FIXTURE, WHICH IS EASY TO MISS.** `AttackTargetBindingConditionalChooseOfferSweepTests` walks each hero's `AllDefinitions` through an explicit `foreach` per hero - twelve of them today - hunting a Conditional or Choose gate hiding a mandatory binding behind it. A thirteenth hero whose line is not added is a corpus the sweep silently does not cover, and because the case is `[Explicit]` no suite run will say so. Found on 2026-09-11 while checking whether milestone 8.38a's new enum members changed that sweep's output; they do not - it names no target-policy type at all - but the per-hero enumeration is a debt this milestone owes.

Acceptance checks:

- Eighteen dealt faces compose; only Backup carries a deferral, and it names the standing p.20 reason.
- Her corpus is added to `AttackTargetBindingConditionalChooseOfferSweepTests`' per-hero enumeration, and the sweep is run by hand once with her content in it.
- Each face's focused authoritative scenarios cover positive, negative, boundary, restore/replay and ordered-event arms, and the Attack faces additionally cover the enclosing combat outcome arms.
- Her progressed hands compose, and her three upgrade lines match the printed Tier structure the inventory recorded.
- Complete unfiltered dotnet harness and Edit Mode pass.

Non-goals: No registration, no artwork, no pick-screen row, no Play Mode journey. A composed face nothing deals is not a dealt hero.

### [ ] 8.38e Deal, register and artwork Swift as the thirteenth hero, and put her pick card under the cursor

The registration milestone the three build blocks were sized to pay for once, plus the artwork and selector work the user asked for by name.

Observable outcome: a player can pick Swift the Sharpshooter in the hero selector, see her real pick card when the cursor rests on her row, see her real printed card fronts in hand, stand on the board as her real official token, and play her whole set.

**THE ARTWORK IS THE REAL CORPUS, THROUGH THE FOUR EXISTING TOOLS, AND NOTHING IS GENERATED.** `Prepare-CardArtwork.ps1` gains her folder and her eighteen dealt fronts; `Prepare-HeroSelectionArtwork.ps1` gains her visually-verified pick card; `Prepare-HeroTokens.ps1` gains her manifest element, read from the bundled manifest rather than guessed; `Prepare-ShineSprites.ps1` gains her sprites and its manifest probe is extended to thirteen heroes rather than left to go stale the way it did at six.

**THE HOVER PICK CARD IS AN EXISTING SEAM AND THE WORK IS TO REACH IT, NOT TO BUILD IT.** Milestone 8.36e already put hover overview cards on the selector, and `PrivateLobbyViews` already loads `Private/HeroSelection/<content-id>` for the hovered row. What a thirteenth hero owes it is the prepared resource under the addressing key the runtime composes, and a check that proves the hover actually resolves her face rather than silently falling back - because an absent private resource is tolerated by design everywhere else in this build, which is exactly what would let this fail quietly.

**A CONTENT-REVISION BUMP OWES THE ARTWORK CATALOG, and a green harness is structurally blind to it.** `ConfiguredCardArtworkCatalog.json` pins `definitionId` plus `contentRevision` per entry and its fixture is Unity-excluded, so the offline check - the catalog's revision set against the content constants - runs in this milestone rather than being discovered by the first unfiltered Edit Mode run.

Acceptance checks:

- Swift is pickable in the live hero selector, her pick card renders on hover, and a configured match composes her hand without throwing.
- Her eighteen dealt fronts, her shine sprites and her two team tokens are prepared from the real Tabletopia corpus rather than generated placeholders, and the artwork bijection is re-pinned from measurement.
- The registered-handler count, every corpus digest and both scenario-digest families are re-pinned from measurement, and any schema or wire movement is stated with its cause.
- Her identity pin is minted from a complete unfiltered run and the twelve prior pins are byte-identical on it.
- A checkout without the private corpus still runs, drawing the generated fallbacks exactly as before.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: No fourteenth hero. No new playtest package. No change to the twelve existing heroes' art or pins beyond re-measurement.

### [ ] 8.38f Player-path gate Swift's whole printed set through real player input

The permanent content-derived gate every dealt hero carries, and the milestone that answers the user's "absolutely sure that all the cards work in all scenarios" on the terms this project can actually prove.

Observable outcome: `SwiftPlayerPathGateTests` derives her interaction shapes from her own declarations and binds every one of them to a named Play Mode journey that drives real controls, with no `[Explicit]` case and no skip.

**A MATRIX NAMING A SKIPPED JOURNEY IS NOT A PASSING PLAYER-PATH CHECK**, which milestone 8.36f3 established by finding twenty-two parked cases across three heroes, and milestone 8.37a re-established by repairing the two Tali's own dealing shipped parked. This milestone ships none: a step that finds no control fails rather than skips.

**A FILTERED PLAY MODE RUN CANNOT SEE THIS GATE**, so the closing evidence is a complete unfiltered run. Two milestones in a row proved the point, and the same run is what re-pins the Play Mode baseline.

Acceptance checks:

- Every interaction shape her declarations produce is bound by name to an active journey; none is `[Explicit]`, none self-skips, and Backup is declared unreachable with the standing p.20 reason.
- The journeys drive real controls through the UI player driver, including the borrowed primary, the counted bounty across a turn boundary, the off-line placement, the multi-unit push, and the Ultimate's per-turn target exclusion.
- Undo and snapshot restore are exercised inside at least one journey of each shape.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision, and the Play Mode total is re-baselined with its cause stated.

Non-goals: No new content and no new capability. A journey that needs one is a defect report against 8.38c or 8.38d, not a widening here.
