# Phase 8 - Fifteenth-hero dealing: Bain the Bounty Hunter

Return to [roadmap](../ROADMAP.md). Current/active/next authorization lives only in [the brief](../SESSION_BRIEF.md).

## Authorization and standing

On 2026-09-13 the user named the **next** hero to deal and asked for him on the terms the two series
before this one were given: "make other agents and have them implement Bain ... Keep pushing them until
hero is fully implemented and tested", and, on what "implemented" means, "fully implemented, pickable,
real cards, pick card on pick screen, all the cards work, the whole thing." This shard owns that work
and nothing else. The 2026-09-07 brakes direction still governs every other parked item: naming a
fifteenth hero re-opens the ROSTER for Bain, not the parked presentation program, the card-authoring
workbench, tier-3 bots, or a new playtest package.

Bain is the **fifteenth** hero this build deals and the third taken from outside the twelve-hero
package arc milestone 8.37 closed, after Swift and Cutter. He is a base-box hero, so the
second-printing rulebook is available to him as a second source in the way it was to them - which is a
check to run rather than an assumption to make, and milestone 8.40 ran it. **It came back differently
from every hero before him**, and that is recorded as BA-01 in his shard: the rulebook names him,
once, in flavour text on printed page 2, and nowhere else.

**He is a TWO-star hero on rulebook p.21's complexity scale**, which makes him the simplest hero this
project has dealt since the package arc - Swift, Tali and Cutter are all three-star. **That does not
make his series short, and the reason is worth stating up front**: his complexity rating is about how
hard he is to PLAY, and what this build has to pay for is how far his printed text sits from vocabulary
the engine already carries. One of his five Blue faces asks for a kind of hidden information no card in
this build has ever asked for.

The series below is laid out the way [POST_PITCH.md](POST_PITCH.md)'s batch decision says a hero's
vocabulary should be, and the way the thirteenth- and fourteenth-hero series proved out: ordinary-sized
lettered build blocks that BUILD the predicted vocabulary against showcase reachability, then one
milestone that composes his faces, then one that registers and deals, then the permanent gate. Building
is schema-neutral; only registering costs a version.

## The audit that scopes this series

Milestone 8.40 owns the full inventory and the exact mapping; [Rules/HERO_BAIN.md](../Rules/HERO_BAIN.md)
is where it landed, and it is the authority every block below is sized against. What that audit
established:

- His set is 19 faces and matches Swift's and Cutter's shape exactly: one Gold basic (Dead or Alive),
  one Silver basic (Get over here!), five Red Attacks, five Blue Skills, five Green faces, one Purple
  Ultimate (A Complicated Profession), and one Handicap (Side Quest) that p.6 returns to the box.
  Eighteen are dealt.
- **His Green line is not shaped like anybody else's.** Three of the five are Blocks and two are
  Movement primaries, so his Green colour prints no Skill at all - and every one of the five reads the
  same board fact, "If a hero in play has a bounty marker".
- **Much of his text is already registered vocabulary, and two clauses that looked new turned out to be
  parameters or enum members on declarations this build has carried for milestones.** His modal Attacks
  are Dodger's two shapes unchanged; his "in a straight line and in range" is Swift's
  `AttackTargetLine.InStraightLine` from 8.38a; his "ignore obstacles" is Silverarrow's Trailblazer
  permission; his Movement-primary-with-text is Ursafar's Cold Ire; and his marker is Tigerclaw's
  `MarkerGivingHandler` with a definition of its own.
- **The lesson the two series before this one measured is the standing instruction here: read the
  catalog's PARAMETERS, not its capability names.** The pre-authorization read of Bain named the wrong
  contract in **seven** places, and the inventory corrected all seven against the schemas. Four are
  worth carrying into the blocks below because they change what gets built: `retrieve-card@1` already
  names a retriever other than the actor, so half a predicted block does not exist; `DirectMoveDestinationPolicy.TowardActor`
  is not a pull and is bound by no card in the build; `RosterValueContributionDefinition` **refuses the
  Movement channel in its constructor**, so the predicted eighth `RosterValueQueryKind` could not have
  been built at all; and `AttackSequenceSubjectNarrowing` is a persisted ENUM rather than a capability
  reference, which makes one predicted contract a four-line member.
- What is genuinely unbuilt falls in eight places, and the eight build blocks below are drawn on
  exactly those lines rather than on a word count. **Every owed row in his shard names exactly one of
  8.40a-8.40h, and each of those eight is named by at least one owed row.** The three blocks after them
  compose, deal and gate, and own no vocabulary.

## The one block that is not like the others

**8.40f asks this build for hidden information it has never had, and this series states that up front
rather than discovering it at composition time.** Three of his Blue faces print the same sentence:
"An enemy hero in radius with two or more cards in hand **chooses one of those cards**; Guess the
card's color, **then reveal the card**."

Every card-valued choice in the build today terminates immediately in a discard, a retrieval, a borrow
or a play. `EffectChoiceRequest` carries a chooser, candidates, two selection bounds, an explicit
chooser hero and a value vocabulary, and **no visibility field at all** - because nothing has ever
needed one. Bain's face needs the answer to survive across the next node, visible to the chooser's seat
and to nobody else, while the controller names a colour against it. Then it needs the opposite: a
public reveal that names one card to every seat.

**Both halves have a near miss and neither is reachable.** The hiding half is already most of the way
there - another hero's hand is ABSENT from a seat projection rather than concealed in it, which is what
`F-ER2-07` means - so what is missing is the answer, not the candidates. The revealing half is worse
than missing: `SeatProjection.IsPublic` already makes a hand card public to every seat exactly when the
bounded public log names it, and `MatchLogEntryKind.CardRevealed` already carries a card instance - but
the only producer is p.7's turn-card commitment flow through `CardLifecycleAuthority.TryReveal`, which
no effect capability can reach.

**And the guess domain is a privacy question wearing a rules costume.** A Game of Chance prints "(You
can only guess colors that could be in that player's hand)", and `name-color@1` offers all six of p.9's
colours from a hard-coded static list whose own remarks record that nothing reads which colours are
live in anyone's hand. The narrowing has to be derived from PUBLIC information - the printed deck, the
discard pile, the turn area, the item slots, the boxed Handicap and the Ultimate area - because
deriving it from the hand would leak the exact fact the clause exists to protect. That reading is
BA-D2, and it is the sharpest instance in this roster of the standing rule that **an authorization
filter is not a projection filter**.

So 8.40f is expected to be larger than the other seven build blocks put together, and it is sequenced
so that a split out of it costs the series nothing: 8.40f1 is named in advance, on the precedent
8.39d1, 8.39f1, 8.39g1 and 8.39g2 set.

## Standing lessons every block below inherits

Restated once rather than in eleven places, because every block is judged against them:

- **Probe before you build, and read the PARAMETERS rather than the capability name.** This series'
  own inventory paid that lesson seven times before a line of code existed.
- **An added optional parameter or an added output cannot move a corpus digest**, because the identity
  walker digests a capability's reference and its `ActiveEffectDefinitions` and never its parameter
  schema. **A new member on an enum a payload already writes is free**; **a new FIELD on a shared
  content type is a schema version**, and **a new public PROPERTY on a shared content type such as
  `UltimateRuleDefinition` moves every dealt hero's corpus digest** - which milestone 8.38d measured
  when `GrantedTargetExclusion` moved all twelve at once.
- **A closed vocabulary may be enforced at runtime by a hard-coded guard, so grep the new member's own
  TYPE and not only its readers.** `RosterValueContributionDefinition`'s constructor and
  `ActionDefinition`'s Block pairing are both such guards, and both bite in this series.
- **Never call `EndGame` - or anything that stops a resolution - from inside a live effect frame.** A
  capability writes a fact and returns; the command boundary acts on it.
- **Compose `BainCardContent.AllDefinitions` LAZILY from the first line**, with
  `Lazy<IReadOnlyList<CardDefinition>>` and computed properties. The static-initialization cycle this
  avoids presents as `NUnit failed to load ... .dll` with ZERO tests discovered and a green
  `dotnet build`, which reads like tooling breakage rather than content breakage. Swift and Cutter
  follow this; the twelve before them do not, and are not wrong.
- **A dealing owes THREE scene-composition arms and they ship together** - the staged-setup arm, the
  engagement/restaging arm, and the derived screenshot list - or a staged scene deals his cards to
  another hero's identity seat, or leaves the two heroes fifteen spaces apart.
- **A content-revision bump owes `ConfiguredCardArtworkCatalog.json` and the git-ignored shine
  sprites**, and a green offline harness is structurally blind to both; run the catalog's revision set
  against the content constants offline rather than discovering it in Unity.
- **A filtered Play Mode run cannot see the player-path gate.** Closing evidence is a complete
  unfiltered run.
- **A conformance rule scoped to the REGISTERED capability set says nothing about a deferred
  declaration**, so the dealing is the first tier that can read one - Cutter's CU-15.

## Series

### [x] 8.40 Inventory Bain the Bounty Hunter's printed set and map every clause to registered capability

The inventory pass every hero gets before any of his content is written, on the same terms as
milestones 8.1, 8.18a-8.18e, 8.38 and 8.39: transcribe the printed faces from the extraction, record
what the scan actually prints rather than what a rule would predict, and map each clause to a
registered capability or name it as owed.

Observable outcome: `Rules/HERO_BAIN.md` states his nineteen faces at printed-value fidelity -
Initiative, both secondary icons, colour, Tier, primary action and value, Range or Radius, item icon,
upgrade pair and transcribed text - and his capability contracts are mapped at the same
node-and-parameter fidelity [ROSTER_CAPABILITY_MATRIX.md](../Rules/ROSTER_CAPABILITY_MATRIX.md) uses,
with every owed contract naming its block. This shard is written in the same change as a user-approved
roadmap amendment.

**THE SLOT-POSITION TRAP HAS SIX INSTANCES IN HIS SET, WHICH IS MORE THAN THE REST OF THE ROSTER PUT
TOGETHER.** Milestone 8.35z20 cost four rounds of litigation because a single secondary icon was read
by its slot POSITION rather than its glyph SHAPE; Swift's Bounce and Cutter's Grappling Bolt each
repeated it once. Get over here!, Close Call, Narrow Escape, Vantage Point, Perfect Getaway and High
Ground each print exactly one secondary icon. **All six sit in the LOWER slot - the boot's position -
and three of the six are shields.** The inventory records the glyph-shape verification for each.

**AND EVERY MEASURED CHECK CAME BACK, RE-RUN AT CLOSING RATHER THAN TAKEN FROM THE DRAFTING PASS.**
The second-printing PDF returns **one** hit for `Bain`, **one** for `Bounty Hunter` - the same p.2
flavour line - and **zero** for all nineteen card names, so the extraction is sole authority for a
reason no earlier hero had: a presence that says nothing rather than an absence. His nineteen deck
backs hash to exactly two values, **fifteen and four**, the four being Dead or Alive, Get over here!,
Vantage Point and High Ground, corroborated independently by `cards.csv`'s source-atlas coordinates.
The six single-icon glyphs resolve three shields (Defense 2, 3, 4) and three boots (Movement 2, 2, 2),
in one image against Dead or Alive's and Hunter-Seeker's shield-over-boot pairs. The twelve item icons
resolve three Defense, three Initiative, three Attack, one Movement, one Range and one Radius against
Silverarrow's six declared reference faces, with **all six p.16 channels present in one hero's set**.
Dead or Alive's lower-right glyph is a **Radius 4**, not a Range, resolved at crop resolution against
Get over here!'s Range 4 arrow on the same hero - the one printed value the pre-authorization read could
not settle. His stat profile is **Attack 5, Defense 4 with two brighter conditional sections,
Initiative 4, Movement 5 with one**, calibrated against Brogan's known Initiative 1 and Defense 8;
**two stars**, roles Tactician, Sniper, Healer, Farming and Durable. His pick card is
`randomizers-tt14__7723683__front.png`, SHA-matched to `_selection-cards/Bain__front.png`. His standee
is manifest element **8596242 `BountyHunter`**, the FOURTH to hide under an epithet, carrying two
302x302 faces at (1563,3007) and (1867,3007) in `Image_0_Atlas`. And the corpus holds real art for the
marker itself - element **1421888 `BountyMarker`**, a "WANTED" poster in a riveted ring, sitting
immediately beside Tigerclaw's `PoisonMarker`.

**SEVEN MAPPING CORRECTIONS THE AUDIT MADE AGAINST THE PRE-AUTHORIZATION READ**, each measured against
a schema rather than argued: `retrieve-card@1` takes `owner` AND `chooser` and shipped content already
binds both to another hero, so "have a hero in radius retrieve" is registered and owes nothing;
`DirectMoveDestinationPolicy.TowardActor` is a per-step filter with no straight line and no adjacency
stop, bound by no card in the build, so it is not the pull; `RosterValueContributionDefinition`'s
constructor refuses the Movement channel outright, so the predicted value query for "+N movement" could
not be constructed; `AttackSequenceSubjectNarrowing` is a persisted enum that deliberately refused an
`EffectCapabilityReference`, so the marker bullet predicate is a member rather than a capability;
`PrintedBlockCondition`'s two live members read the ATTACKING card's classification and
`DefenseBlockQuery` is handed no board at all, so a marker-conditioned Block is a bigger change than a
third member; `synthetic-range-obstacle-target@1` blocks on tokens as well as units and terrain, so it
is the wrong shape for the crossbows AND needs widening for the pull; and `name-color@1` offers all six
colours from a hard-coded list, so the printed guess-domain parenthetical is a real narrowing.

Acceptance checks:

- `Rules/HERO_BAIN.md` exists, is linked from the hero-shard index in [RULES_LEDGER.md](../RULES_LEDGER.md),
  and carries his printed-value table, his upgrade pairs, his starting hand, his boxed Handicap, and his
  transcribed text for all nineteen faces.
- Every printed clause is mapped to a named capability at node-and-parameter fidelity, and each is
  marked registered or owed; the owed set is exactly what milestones 8.40a-8.40h build, and every one of
  those eight is named by at least one owed row. **8.40f1 is the one entry below that no owed row names,
  and it is named in advance as a conditional split of 8.40f rather than as a ninth block** - if 8.40f
  ships the failure modal inside itself, 8.40f1 closes as discharged rather than executed.
- All six single-secondary faces are recorded with their glyph-shape verification, and every item icon
  with the reference face it was montaged against.
- The rulebook second-source check is run and its outcome recorded, whichever way it comes out.
- The deck-back grouping is measured by hash and stated as measured.
- His standee element and his selection card are identified from the bundled manifest and by SHA-256
  respectively, not by eye, and his standee's two 302x302 faces are confirmed against
  `Prepare-HeroTokens.ps1`'s invariant.
- This shard exists, is linked from the roadmap index, and quotes the user's instruction.
- The validator passes and no governed shard crosses a threshold.

Non-goals: No content, no capability, no registration, no artwork, no schema movement. An inventory row
authorizes nothing.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-840-inventory-bain-the-bounty-hunters-printed-set-and-map-every-clause-to-registered-capability-completion-evidence).

### [ ] 8.40a Build the bounty marker's defeat cost, charged to the hero who wears it

Observable outcome: a hero holding a declared marker spends one Life counter more than rulebook p.13's
schedule charges when that hero is defeated, wherever the defeat is resolved from, and a hero holding
none spends exactly what the page prints.

**THE FAMILY EXISTS AND THE READ DOES NOT, WHICH IS THE WHOLE BLOCK.**
`RuleModificationKind.AdditionalDefeatLifeCost` was minted at 8.35z31 for Ursafar's Tear and its own
remarks say it "is declared about the *attacker*"; `RuleModificationSet.AdditionalDefeatLifeCost` takes
one `RuleObject attacker` and counts declarations against it; and p.13's spend site asks it about
`completedResolution.ActorId` and nobody else. Bain's two marker-giving faces print the mirror sentence
- the cost is the DEFEATED holder's - and `RuleModificationQuery.FromHeldMarkers` already makes every
marker property affect `RuleObject.ForHero(marker.HolderHeroId)`, so the declaration lands on the right
object and is simply never consulted.

**THE FAMILY SAYS "ONE" AND TWO DECLARATIONS CHARGE TWO, AND THIS BLOCK MUST NOT BREAK THAT.** The
2026-09-06 ruling behind the enum member is explicit: the charge is computed once as Tier + 1 and spent
once, and counting rather than flagging is what keeps a second source from being silently swallowed.
So a hero wearing Bain's marker who is defeated by Ursafar while Tear stands charges TWO, by
construction and without either card saying so. Assert it rather than reason about it.

**AND THE SPEND HAS MORE THAN ONE DOORWAY, WHICH IS BA-Q4.** p.13's Defeat is reached from card effects
as well as from p.14's step 6, and `HeroDefeatRewardSchedule.LifeCounters` is called from exactly one
site today. The printed sentence attaches no cause to "when defeated". Take the reading, name every
site the block had to reach, and prove a marker-wearing hero defeated by an effect rather than by an
Attack.

**THE MARKER DEFINITION IS CONTENT AND ARRIVES WITH IT.** `BainComponentContent` is
`TigerclawComponentContent`'s shape: a `MarkerDefinition` with `MarkerReturnCondition.GeneralCleanup`
(p.8's end-of-round return and p.13's defeated-holder return are one condition in this vocabulary), one
`MarkerPropertyDefinition` carrying the new read, and a `MarkerGivingHandler` bound to it. Its supply
count is **BA-D1** and this block implements the recorded reading. Registration waits for the dealing
on milestone 7.9's gate; the handler TYPE is already proved, so it owes no showcase consumer.

Acceptance checks:

- A hero wearing the marker spends Tier + 1 when defeated and a hero wearing none spends Tier, asserted
  on one board at the same Tier.
- Two standing declarations charge two, asserted with Ursafar's attacker-side declaration and this
  marker together.
- The cost is charged at every site p.13's spend is computed, by a recorded reading, with a case for a
  defeat that no Attack caused.
- A returned marker charges nothing, and a marker returned by p.13's own return step does not cancel the
  charge for the defeat that returned it - the order is stated and asserted.
- Ursafar's Tear is byte-identical in behaviour, asserted rather than assumed.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain card content and no registration - his marker definition is written here and dealt at
8.40j. No change to what `AdditionalDefeatLifeCost` means for the attacker. No new Life-counter rule.

### [ ] 8.40b Build the marker movements his Green Blocks and his Gold basic need

Observable outcome: a declared marker can be taken from one hero and given to another as ONE operation,
can be returned to the supply by a card that did not give it, and can be moved by a give when the supply
is empty because it is already on the board.

**THREE SENTENCES, THREE GAPS, AND ONE REASON THEY ARE ONE BLOCK.** Close Call prints "that hero gives
the marker to you"; Narrow Escape prints "retrieve the marker"; and Dead or Alive prints a give that
BA-D1 reads as moving the marker when another hero already holds it. All three are operations on an
attachment that exists, and the catalog has only two: `synthetic-give-marker@1`, which mints a NEW
attachment from the supply and refuses when the supply is empty, and `synthetic-return-marker@1`, whose
finder is `components.Markers.GivenBy(context.SourceCardInstanceId)` - the marker **this source card
gave**, which on Narrow Escape is none.

**THE TRANSFER MUST NOT BE COMPOSED AS RETURN-THEN-GIVE, AND THE REASON IS A CARD IN THIS SAME SET.**
A give is what A Complicated Profession watches (8.40h), and it is watched for a give BAIN performs -
BA-D5. Close Call's transfer is another hero giving the marker to Bain, so a composition that ran a
give would fire his own Ultimate on a sentence that never says "you give". It would also mint a second
`MarkerInstanceIdentity`, which is the identity a restore rebinds against.

**"RETRIEVE" IS THE RULEBOOK'S WORD FOR RETURNING TO THE SUPPLY, NOT FOR TAKING INTO A HAND**, and the
block states that before it builds anything. p.8's own step is "Remove Tokens, **Return** Markers ...
Return all markers back into the supply", and p.13's is "Any markers they had are returned". Narrow
Escape's "retrieve the marker" is that same return performed early by card text, which is the reading
Close Call's contrast makes unavoidable: Close Call MOVES the marker and says so in different words.

**WHOSE MARKER, STATED RATHER THAN ASSUMED.** Each of the three operates on a marker the DEFINITION
names rather than on any attachment on the board - a hero wearing Tigerclaw's Poison is not wearing a
bounty, and a card that took either would be stating a rule no face prints. Assert it on a board
carrying both.

Acceptance checks:

- A transfer moves one attachment from one holder to another, keeps its instance identity, changes
  whose values the property affects, and publishes both heroes.
- A transfer is observed NOT to fire whatever watches a give, asserted directly rather than by
  inspection of the composition.
- A return-any detaches the one attachment of its declared definition wherever it sits, and reports
  impossible when none is out; `synthetic-return-marker@1`'s source-card reading is byte-identical.
- A give with the supply empty and the marker held moves it, by BA-D1's recorded reading, and a give
  with the supply empty and no marker out is still impossible.
- None of the three touches an attachment of a different definition, asserted on a board carrying two.
- Every operation survives a snapshot round-trip and a replay, and Undo restores the previous holder.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain card content. No change to `synthetic-give-marker@1`'s existing behaviour when the
supply can pay. No marker rendering.

### [ ] 8.40c Build the marker predicates his whole set reads

Observable outcome: a card can ask whether any hero in play holds a declared marker and learn WHICH
hero, can narrow a written-distance target selection to marker holders, and can narrow a p.14 attack
bullet the same way.

**NOTHING IN THE CATALOG READS MARKER STATE AS A QUESTION.** The only marker readers today are the
supply check and re-entrancy guard inside the giving handler and the finder inside the return; a sweep
of every `EffectCapabilityParameter` name in the synthetic catalogs finds no component, marker, token-
held or attachment predicate at all. The narrowings that exist are board, geometry and card-state only.

**THE PREDICATE PUBLISHES A HERO AS WELL AS A BOOLEAN, AND THAT IS NOT A CONVENIENCE.** Five of his
faces open with "If a hero in play has a bounty marker", and Close Call's consequence is about **that**
hero. A predicate answering only true/false would leave the transfer at 8.40b with nothing to bind, and
a second selection asked afterwards would let the player name a hero the sentence never chose.
`hero-condition@1` is the near miss in shape - a Boolean a `Conditional` names directly, asked of the
match rather than of the frame - and the wrong one in content: its `condition` is a closed vocabulary
whose one live member is `Enraged`, and it asks about the ACTOR.

**THE SELECTION NARROWING IS A SEVENTH OPTIONAL PARAMETER ON `choose-target@2` AND NOTHING MORE.** That
declaration already carries `excludeTarget`, `playedAttackThisTurn`, `adjacentToFriendlyMinion`,
`adjacentToAnotherEnemyUnit`, `excludeAdjacent` and `continuedLineFrom`, every one optional and
default-off, and this series has already measured that an added parameter cannot move a corpus digest.
Every declaration written before this milestone must keep its exact candidate set, asserted rather than
assumed.

**THE BULLET NARROWING IS AN ENUM MEMBER, AND THE TYPE'S OWN REMARKS SAY WHY IT MUST BE.**
`AttackSequenceBullet` is PERSISTED, and its predicate is `AttackSequenceSubjectNarrowing` - `None`,
`HasOneOrMoreCardsInTheDiscard`, `HasAnEmptyDiscard`. The sketched alternative, an optional
`EffectCapabilityReference` resolved through the capability catalog, was measured and REFUSED because a
persisted payload naming a capability id is "a payload choosing which code runs". The same remarks
record that an added member here is not a content-revision event. **A fourth member, and the two
existing ones byte-identical.**

**ONE QUESTION THE BLOCK ANSWERS RATHER THAN MEETS.** The predicate and the narrowing must agree about
what "has a marker" means - whose definition, and whether a marker Bain himself wears counts. BA-D5
records the reading (any hero on the board, Bain included, holding his own marker); prove the predicate
and the narrowing give the same answer on the same board, because two readings of one clause is exactly
the divergence a shared label must never hide.

Acceptance checks:

- The predicate answers true and publishes the holder when one hero wears the declared marker, answers
  false and publishes nothing when none does, and is unaffected by a marker of a different definition.
- The predicate counts a marker the actor himself wears, by the recorded reading, asserted on that board.
- The `choose-target@2` narrowing admits exactly the holders and nothing else; unbound, it reproduces
  today's candidate set exactly, asserted over declarations written before this milestone.
- The fourth `AttackSequenceSubjectNarrowing` member narrows a bullet's subjects to holders, the two
  existing members are byte-identical, and a bullet declaring none is unaffected.
- The predicate and the narrowing are asserted to agree on one board rather than by inspection.
- No schema field, corpus digest or pinned constant moves - the enum grows by one with the existing
  members keeping their values, so no payload written earlier decodes as a different predicate.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain content. No widening of `hero-condition@1`. No predicate about tokens.

### [ ] 8.40d Build the unit-and-terrain clear line his five crossbows print

Observable outcome: a printed Attack can require its file to be clear of units and terrain while
allowing a token to stand on it, and every Attack written before this milestone keeps its exact target
set.

**p.9 STATES NO LINE-OF-SIGHT RULE, SO THE CARD HAS TO, AND `AttackTargetLine` CANNOT SAY IT.** That
enum has three members - `AnyLine`, `NotInStraightLine`, `InStraightLine` - and every one says only
which FILE a target stands on, never what stands on it. This is the same reason
`PrintedBlockCondition` exists: a printed narrowing of a default that admits everything is a fact about
the card.

**THE CLAUSE IS DELIBERATELY NARROWER THAN p.18's OBSTACLE LIST AND THE PROOF IS IN HIS OWN DECK.** Get
over here! prints "no **obstacles** between you and the target"; the five crossbows print "no **units,
or terrain**, between you". p.18's Obstacle keyword lists "Units, terrain (including printed on the
board), tokens, as well as any objects represented by miniatures", so a token blocks the pull and does
not block a shot. **BA-03 records this as the most consequential source fact in the set**, and this
block must not collapse the two.

**THE NEAREST EXISTING WALK IS `synthetic-range-obstacle-target@1`'s AND IT IS WRONG TWICE.** It blocks
on `board.TryGetObjectAt` - any board object, tokens included - as well as on terrain and on standing
overlays, and it TARGETS the first obstacle rather than something past it. Reuse what it proves about
geometry (p.19's six directions, the printed Range Total, the first-blocker stop) and not its blocking
set.

**AND A STANDING OVERLAY MAY MAKE A SPACE COUNT AS TERRAIN, WHICH THIS BLOCK MUST DECIDE ABOUT RATHER
THAN INHERIT.** Xargatha's Stone Gaze makes enemy heroes count as both heroes and terrain, and
`EffectiveTypeOverlay` is how. A hero is already a unit, so the overlay changes nothing for this clause
- but a future overlay on a space would, and the block states which query it asks and why.

**THE POLICY NEEDS A BUILDER, ON 8.38a's PRECEDENT.** `PrintedAttackTargetPolicy` exposes one static
factory and seven instance builders; Swift's line arrived as `RestrictedToTheStraightLine()`. Bain's
arrives the same way so the composition calls a method rather than constructing a policy by hand. The
new dimension must also reach `attack-sequence@1`, whose `line` parameter is declared over this very
enum, because Hand Crossbow's and Hunter-Seeker's ranged bullets print the crossbow clause too.

Acceptance checks:

- The clause admits a target whose file carries a token and refuses one whose file carries a unit or
  terrain, asserted as a set equality on a board where each condition excludes a different candidate.
- An obstacle standing ON the target's own space is not "between", and an obstacle on the actor's own
  space is not either - both stated and asserted.
- Adjacent targets are unaffected, because no space lies between.
- Every Attack declaration written before this milestone keeps its exact target set, asserted over the
  corpus rather than over a remembered list.
- The clause reaches an `attack-sequence@1` bullet as well as a whole-action policy, asserted both ways.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain content. No change to `synthetic-range-obstacle-target@1`'s blocking set - 8.40e owns
that family. No general line-of-sight rule: this is one printed narrowing, not p.9 gaining a page.

### [ ] 8.40e Build the pull his Silver basic prints

Observable outcome: a card can name a unit or a token on a clear straight file within range and move it
along that file toward the actor until it is adjacent, leaving everything an existing obstacle selection
offers unchanged.

**HALF OF THIS IS ALREADY BUILT AND THE AUDIT FOUND WHICH HALF.** `synthetic-direct-move@3`'s subject is
a typed coordinate naming "a hero, a minion, or a token", and `DirectMoveDestinationPolicy.NamedSpace`
takes a space an earlier node published and opens no choice - which is exactly how Cutter's Grappling
Bolt performs the mirror-image move of the ACTOR toward a named obstacle. So the pull is a selection
that publishes the right pair of spaces plus a move that already exists.

**`DirectMoveDestinationPolicy.TowardActor` IS NOT THE CONTRACT, AND THIS IS RECORDED BECAUSE THE NAME
SAYS OTHERWISE.** It is a per-step filter - "only spaces strictly closer to the acting hero" - bounded
by the node's own `distance`, not constrained to a straight line, with no adjacency stop condition, and
it is referenced by NO card content in the build. Composing the pull from it would move the subject a
printed number of spaces in any closer direction, which is not the sentence.

**THE SELECTION IS A WIDENING OF 8.39b's FAMILY, NOT A NEW ONE.**
`synthetic-range-obstacle-target@1` already walks p.19's six directions to the printed Range Total,
already stops at the first obstacle on each file, and already blocks on exactly p.18's full list -
which is what Get over here!'s "no obstacles" asks for. What it does not do is restrict the TARGETABLE
set to units and tokens (its walk offers terrain), and its `obstacleApproachSpace` output is one step
back from the obstacle rather than one step out from the actor. Both are additions to a declaration
that is still DEFERRED or newly registered depending on where 8.39b's contract sits when this runs -
check, and state which.

**"UNTIL YOU ARE ADJACENT" IS A DESTINATION, NOT A DISTANCE**, so the node that moves is handed a space
and asks nobody anything. A target already adjacent has nowhere to go and the clause is satisfied
without movement, which the block states rather than treats as a failure.

**AND A TOKEN IS A LEGAL SUBJECT, WHICH IS THE ONE THING NO OTHER PRINTED PULL IN THE BUILD HAS ASKED
FOR.** p.19's Target keyword admits a unit, a token or a space, and this face prints "a unit **or a
token**". Prove the token arm directly, including that the moved token keeps its instance identity and
its owner.

Acceptance checks:

- The selection offers exactly the first unit or token on each of the six files within the printed Range
  Total, and offers no terrain, asserted as a set equality.
- A file whose first obstacle is terrain offers nothing on that file, rather than offering what stands
  behind it.
- The published destination is the space adjacent to the ACTOR along the chosen file, and the move lands
  the subject there; an already-adjacent subject is legal and moves nowhere.
- A token subject is moved and keeps its identity and owner; a hero and a minion subject each work too.
- Immunity is asked of things that can carry it and not of terrain or tokens, exactly as the family
  already does.
- Every existing declaration of the obstacle-selection family keeps its exact candidate set and outputs.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain content. No push - p.18's Push has its own stop rules and this is a Move. No change
to `DirectMoveDestinationPolicy`.

### [ ] 8.40f Build the hidden card choice, the reveal and the public guess domain his Blue line prints

**This is the block the series preamble singles out, and it is expected to be larger than the other
seven put together.**

Observable outcome: a card can have a chosen hero pick one of their own hand cards so that the answer is
visible to that seat alone, have another hero name a colour from a domain public information admits,
reveal the picked card to every seat, and act on whether the two agree.

**FOUR CONTRACTS, AND EACH HAS A NEAR MISS WORTH NAMING SO THE BLOCK DOES NOT REBUILD ONE.**

- **The hand-size narrowing.** `owned-card-present@1` takes `hero` and `location` and answers "at least
  one" through `.Any(...)`; `hasCardInDiscard` on the printed-icon family is a presence flag about the
  discard. Nothing reads a hand CARDINALITY anywhere in the build, and the clause narrows a SELECTION
  rather than answering a predicate.
- **The hidden choice.** `EffectValueKind.CardInstance` and `EffectChooserRole.BoundHero` both exist,
  and another seat's hand is already ABSENT from a projection rather than concealed in it. What does not
  exist is an ANSWER that stays private: `EffectChoiceRequest` has no visibility field, and every
  card-valued choice in the build terminates immediately in a discard, retrieval, borrow or play.
- **The reveal.** `MatchLogEntryKind.CardRevealed` carries a card instance, and `SeatProjection.IsPublic`
  already makes a HAND card public to every seat exactly when the bounded public log names it - the
  memory it uses for a retrieved card. The producer is p.7's commitment flow through
  `CardLifecycleAuthority.TryReveal`, which no capability handler can reach.
- **The guess domain.** `name-color@1` takes one required `chooser` (Hero) and publishes `namedColor`
  (Integer) with `EffectChoiceValueVocabulary.CardColor`; its candidates are the hard-coded static list
  of all six colours and its own remarks record that nothing reads which colours are live in a hand.

**THE DOMAIN IS DERIVED FROM PUBLIC INFORMATION AND NEVER FROM THE HAND, WHICH IS BA-D2 AND IS THE
SHARPEST INSTANCE OF THE STANDING RULE IN THIS ROSTER.** p.9's Purple is the Ultimate and never enters a
hand; a hero's printed deck is public; and every zone but the hand and the deck is visible. So the legal
guesses are the colours the chooser's printed deck contains minus those fully accounted for elsewhere.
**Deriving the set from the hand would leak the exact fact the clause protects**, and an authorization
filter that admitted only the true colour would leak it just as surely - the two are different filters
and 8.35u's lesson is that they must not be confused.

**THE REVEAL IS PERMANENT, BY BA-D3 AND BY PRECEDENT RATHER THAN BY CONVENIENCE.** After a wrong guess
the card returns to a hand every player has now seen it in, and `IsPublic`'s own remarks already take
that reading for a retrieved card: "the bounded public log, not a rules-facing card flag, is the memory
of its revealed ID." Use the same memory rather than adding a second.

**AND THE FAILURE MODAL IS NAMED IN ADVANCE AS 8.40f1 RATHER THAN DISCOVERED.** We're Not Done Yet!
prints "otherwise may repeat once **or** gain 2 coins", which is this build's first `Choose one` reached
only on one arm of a comparison, over a repeat of a text that asks an ENEMY to choose. BA-Q2 is the
question it settles: what a second run may ask, of whom, and whether the revealed card is excluded. If
the implementation measures it as a separate seam, split it on the precedent 8.39d1, 8.39f1, 8.39g1 and
8.39g2 set - same total scope, no acceptance check dropped.

**WHAT THIS BLOCK MUST NOT DO.** It must not make a hidden answer a general capability of every choice -
one printed sentence asks for it, and a visibility field on every request would be a vocabulary no card
has asked for. It must not widen `IsPublic`'s fail-closed switch. And nothing it writes may reach a
projection before the reveal, asserted from a LOSING seat's projection rather than from the host's state.

Acceptance checks:

- The selection admits exactly the enemy heroes in radius holding two or more hand cards, asserted at
  one, two and three cards.
- The chooser's answer is visible in the chooser's own seat projection and absent from every other seat's
  - including the controller's - asserted directly on the projection rather than on the authority.
- The guess domain contains exactly the colours public information admits, excludes Purple always, and
  excludes a colour the public zones fully account for; it is identical whether the true card's colour is
  in it or not, asserted by running the same board with two different hidden cards.
- The reveal names the card to every seat, and the card stays public afterwards even when it returns to
  the hand.
- A correct guess discards that card and only that card; a wrong guess awards the printed coins.
- The whole sequence survives a snapshot round-trip and a replay with the hidden answer standing, and an
  Undo taken after the guess and before the reveal does not leak it.
- The failure modal offers both its bullets, and BA-Q2 is settled, recorded and proven.
- No dealt hero's behaviour changes; the schema moves only if a hidden answer must be persisted, and any
  move is stated with its cause and every pinned constant re-measured.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain content - the faces that declare this are composed at 8.40i. No general hidden-choice
vocabulary beyond the one printed sentence. No change to p.7's turn-card commitment flow.

### [ ] 8.40f1 Build the failure modal We're Not Done Yet! prints

Named in advance rather than left to be discovered, on the precedent this project set four times in the
fourteenth-hero series. **It exists only if 8.40f measures the modal as a separate seam**; if that block
ships it inside itself, this entry closes as discharged rather than executed, with the reason recorded.

Observable outcome: the failure branch of a guess offers a choice between repeating the whole clause once
and taking the printed coins, and a repeat with no legal subject is not offered at all.

**THE THREE QUESTIONS, WHICH ARE BA-Q2.** Whether a second run may pick a different enemy hero, whether
it must, and whether the first chooser's now-revealed card is excluded from a second choice by the same
hero. Milestone 8.35z15 settles only the degenerate case - a card-text repeat with no legal subject is
not offered - and 8.39d1's per-selection exclusion is the nearest shape for the third.

Acceptance checks:

- The modal is reached only on the failure branch and never on success.
- Both bullets are offered when both are legal; the repeat is not offered when no legal subject remains,
  and the coin bullet still is.
- BA-Q2's three readings are recorded and each proven on a board where the alternatives differ.
- The repeat runs once and not twice.
- No schema field, corpus digest or pinned constant moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain content. No change to p.10's repeat machinery.

### [ ] 8.40g Build the board-conditioned Block his three Green Defenses print

Observable outcome: a printed Block can be resolved by a condition about the BOARD rather than about the
attacking card, the defence preview and the resolution agree about it, and every Block written before
this milestone resolves exactly as it did.

**THE CONDITION TYPE IS THE WRONG SHAPE AND SO IS THE QUERY THAT ASKS IT.** `PrintedBlockCondition` has
two live members - `AttackIsRanged`, `AttackIsNotRanged` - and its own remarks say the answer is read off
the attacking card's printed classification "and off nothing the defender holds".
`DefenseBlockQuery.Evaluate` is handed the defence offer, the definitions and the ATTACK's reference:
no board, no components, no participants, no modifications. A board fact cannot be answered there at all
without widening the signature.

**AND HIS THREE BLOCKS PRINT NO RANGED CLAUSE**, so the new condition is unconditional in the attack and
conditional in the board - which is a second axis rather than a third member of the first. Decide the
shape deliberately: a third member that means "any attack, if the board says so" states two things in
one word, and `ActionDefinition` already enforces the Block/condition pairing in both directions, so
whatever is chosen must keep a Block that names nothing refusing to construct.

**THE PREVIEW IS NOT OPTIONAL AND THE FILE SAYS WHY.** `DefenseBlockQuery` has exactly two callers, the
Attack resolver and the defence preview a defender is shown before choosing, and 8.11a exists because
they disagreed once: "a preview that disagreed with the resolution would be worse than no preview at
all: it is the thing a player decides from." Both must be handed the same facts.

**THE DEFENSE TEXT'S TIMING IS THE SECOND HALF AND IT IS SETTLED HERE.** Close Call prints "block the
attack **and** that hero gives the marker to you" and Narrow Escape "block the attack **and** retrieve
the marker", so a consequence runs when the Block succeeds. Defense card text is registered in shape -
Arien's, Tigerclaw's and Wasp's Defense actions carry it - but no Block in the build has ever carried a
consequence. State where in p.14's Defense window it runs, and prove that a Block whose condition is
unfulfilled runs nothing.

**PERFECT GETAWAY IS THE CONTROL AND IT IS THE POINT OF HAVING THREE.** It prints the condition and no
consequence at all, so it proves the condition alone; Close Call proves the transfer arm; Narrow Escape
proves the return arm. Compose the cases so each isolates one.

Acceptance checks:

- A Block declaring the board condition succeeds when the board fact holds and fails when it does not,
  regardless of the attack's classification, asserted on a ranged and a non-ranged attack alike.
- The preview and the resolution give the same verdict on every board the cases build, asserted directly.
- Wasp's three Blocks and Tigerclaw's five are byte-identical in behaviour, asserted over the corpus.
- A Block that names no condition still refuses to construct, and a non-Block that names one still does.
- A Block's consequence runs when and only when the Block succeeds, at a stated point in p.14's window.
- No schema field, corpus digest or pinned constant moves; if the condition's shape forces one, it is
  stated with its cause and every pinned constant is re-measured.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain content. No change to p.14's Defense window for a numeric Defense. No new Defense
resolution kind.

### [ ] 8.40h Build the after-a-marker-is-given trigger his Ultimate fires on

Observable outcome: an unlocked Ultimate can fire immediately after its controller gives a declared
marker, learning which hero received it, and fires on no other way a marker changes hands.

**`after-push-trigger@1` IS THE EXACT SHAPE AND THE BLOCK SHOULD START THERE.** A p.16 permanent ability
watching a moment is `UltimateRuleDefinition.AfterPush` plus
`TimingCheckpoint.ObjectPushApplied` plus a tiny enum naming which subject makes it fire, and that
type's own remarks say why the two are separate: "a checkpoint is a position in the rules, and which
objects at that position concern a card is the card's own words." A marker given is a position in the
rules that `TimingCheckpoint`'s nineteen members do not contain.

**THE COST IS STATED IN ADVANCE BECAUSE THIS BUILD HAS MEASURED IT EXACTLY ONCE.** Milestone 8.38d added
`GrantedTargetExclusion` to `UltimateRuleDefinition` and it was **the first change ever to move all
twelve corpus digests**, because `VerifiedContentIdentityTests` walks every public instance property of
every object it reaches. A new property here moves all fifteen. **The reusable probe is on record**:
temporarily add the new property to the walker's own exclusion list, run the identity fixture alone, and
confirm every pin measures byte-identical - that isolates the cause before a pin is touched. **And the
revision strings must NOT follow**, because bumping them asserts fifteen heroes' content changed, which
is false, and drags the artwork catalog and the git-ignored shine sprites with it.

**ASK WHETHER IT CAN LIVE SOMEWHERE CHEAPER FIRST**, which is the standing rule that memory records: a
new enum member or a new entry in an existing collection moves only its owner. `TriggerCheckpoints` is
already a collection on this type, so a new `TimingCheckpoint` member may carry the moment with no new
property at all, leaving only the subject to express. Probe that before adding a property.

**"AFTER **YOU** GIVE" EXCLUDES THREE THINGS AND THE BLOCK ASSERTS ALL THREE.** Close Call's transfer is
another hero giving (BA-D5). A marker returning to the supply is not a give. And a marker Bain gives to
a hero who already wears one - BA-D1's move - is still a give he performed and DOES fire it, which is the
edge worth stating rather than leaving to fall out.

**THE CONSEQUENCE IS REGISTERED.** `synthetic-discard-chosen-card@1` is the `if able` arm that declines
silently, which is exactly what "discards a card, if able" prints, with the recipient as `hero`.

Acceptance checks:

- The trigger fires after a give its controller performed, publishes the receiving hero, and the
  consequence reaches that hero.
- It does not fire on a transfer, on a return, or on a give another hero performed.
- It fires on a give that moved an already-held marker.
- It fires once per give and not once per checkpoint, and an Ultimate that is not unlocked fires not at
  all.
- The five existing Ultimate trigger declarations and both existing moment-watching declarations are
  byte-identical in behaviour.
- The digest cost is measured with the isolation probe before any pin is touched, stated with its cause,
  and no hero's content REVISION string moves.
- Complete unfiltered dotnet harness passes.

Non-goals: No Bain content. No change to `after-push-trigger@1` or `before-action-trigger@1`. No new
Ultimate trigger condition for any other hero.

### [ ] 8.40i Compose Bain's eighteen dealt faces and prove them at the focused tier

Observable outcome: all eighteen of his dealt faces carry composed printed text with no
`PrintedTextComposition.DeferredUntil`, and his focused authoritative rule tests resolve every branch
each face states.

**THE FOCUSED TIER COMES BEFORE THE GATE, IN A FIFTEENTH HERO AS MUCH AS IN A BATCH.** The cheap tier
finds engine defects because it can build the board a printed clause needs; the expensive tier proves the
player path over content that already works.

**HIS BOXED HANDICAP IS THE ONE FACE THAT STAYS DEFERRED**, on the standing p.20 deferral every hero's
Handicap carries since the 2026-08-21 user decision. Side Quest's printed values and words ship as
inventory all the same, and BA-14 records how its give differs from Dead or Alive's.

**THE PER-HERO CORPUS SWEEP IS A DERIVED GUARD AND IT WILL FAIL BEFORE HE IS ADDED, WHICH IS THIS
BLOCK'S FREE PRE-FIX OBSERVATION.** `TheSweptCorpus_CoversEveryVerifiedContentClassTheBuildContains`
reflects the assembly for sealed static classes named `*CardContent` in `WaspCardContent`'s namespace and
asserts set equivalence. **Two shape requirements follow and are easy to miss**: his content class must
be a STATIC class and must sit in `GuardsDigital.Core.Cards.Verified`, or the guard will not see him and
will pass while he goes unswept.

**FIVE COMPOSITIONS THAT NEED NO NEW VOCABULARY AND SHOULD NOT GROW ANY.** Vantage Point and High Ground
are Ursafar's Movement-primary-with-text plus Silverarrow's obstacle permission plus Ursafar's Movement
raise, gated by 8.40c's predicate - BA-Q3 is the scope question they settle. Drinking Buddies and Another
One! are `retrieve-card@1` with `owner` and `chooser` both bound to the selected hero, then
`synthetic-is-flagged@1`, then `retrieve-card@1` bound to the actor; BA-Q1 is the question they settle.
Another One!'s "End of turn: May repeat once" pays Tali's four content costs - a hero-owned capability
id, a hero-owned `ActiveEffectDefinition` at `EndOfTurn`, an `ActiveEffectCreationHandler` registration
and a hand-written duplicate of the node graph in a triggered effect - because `EffectNodeKind.RepeatAction`
re-runs an enclosing ACTION and there is none at `TimingCheckpoint.TurnEnded`.

**COMPOSE `BainCardContent.AllDefinitions` LAZILY FROM THE FIRST LINE.** See the standing lessons above:
the cycle this avoids presents as a suite that is absent rather than red.

**AND TWO OF HIS BLUE FACES PRINT BYTE-IDENTICAL FIRST SENTENCES AT THE SAME RADIUS.** Drinking Buddies
and Another One! differ only in Another One!'s extra keyword sentence - BA-12 - so unlike Cutter's
Bombardment/Barrage pair the reach does not separate them. The composition must not collapse them.

Acceptance checks:

- Eighteen dealt faces compose; only Side Quest carries a deferral, and it names the standing p.20 reason.
- The derived corpus-sweep guard demands his line and is observed failing before it is satisfied.
- Each face's focused authoritative scenarios cover positive, negative, boundary and ordered-event arms,
  with the split between behavioural and structural coverage stated rather than implied, and the Attack
  faces additionally cover the enclosing combat outcome arms.
- Every printed value in the composition matches the inventory table, asserted rather than transcribed.
- His progressed hands compose, and his three upgrade lines match the printed Tier structure the
  inventory recorded.
- BA-Q1 and BA-Q3 are settled, recorded in his shard, and each proven on a board where the readings
  differ.
- Every contract his set names is registered or on a declared deferred list, asserted from his own
  fixture so the dealing cannot move one and forget another.
- Complete unfiltered dotnet harness and Edit Mode pass.

Non-goals: No registration, no artwork, no pick-screen row, no Play Mode journey. A composed face nothing
deals is not a dealt hero.

### [ ] 8.40j Deal, register and artwork Bain as the fifteenth hero, and put his pick card under the cursor

The registration milestone the build blocks were sized to pay for once, plus the artwork and selector work
the user asked for by name.

Observable outcome: a player can pick Bain the Bounty Hunter in the hero selector, see his real pick card
when the cursor rests on his row, see his real printed card fronts in hand, stand on the board as his real
official token, see his bounty marker on the hero rail, and play his whole set.

**THE ARTWORK IS THE REAL CORPUS, THROUGH THE FOUR EXISTING TOOLS, AND NOTHING IS GENERATED.**
`Prepare-CardArtwork.ps1` gains his folder and his eighteen dealt fronts; `Prepare-HeroSelectionArtwork.ps1`
gains his SHA-matched pick card (`randomizers-tt14__7723683__front.png`); `Prepare-HeroTokens.ps1` gains
manifest element **8596242 `BountyHunter`**, whose two 302x302 faces the inventory already confirmed
against the tool's invariant; `Prepare-ShineSprites.ps1` gains his sprites and its manifest probe is
extended to fifteen heroes rather than left to go stale.

**THE HERO RAIL ALREADY RENDERS A MARKER AND HIS IS THE SECOND, WHICH MAKES IT A CHECK RATHER THAN A
BUILD.** Tigerclaw's Poison draws with a caption and a value; Bain's carries no value at all, so the rail
must read a property-less marker without drawing an empty number. Prove it live rather than by inspection,
and record whether the corpus's `BountyMarker` face (BA-17) is used or the generated treatment is kept.

**A CONTENT-REVISION BUMP OWES THE ARTWORK CATALOG, and a green harness is structurally blind to it.**
`ConfiguredCardArtworkCatalog.json` pins `definitionId` plus `contentRevision` per entry and its fixture is
Unity-excluded, so the offline check - the catalog's revision set against the content constants - runs in
this milestone rather than being discovered by the first unfiltered Edit Mode run.

**THE THREE SCENE-COMPOSITION ARMS SHIP TOGETHER**, and the third one is now derived rather than
hand-written, which 8.39i fixed - so a fifteenth hero should be covered by his content class alone. Verify
that rather than assume it: it is the first time that claim can be tested.

**AND THIS DEALING REGISTERS A MARKER-GIVING CAPABILITY, WHICH IS A SECOND ENTRY ON A PROVED TYPE.**
`MarkerGivingHandler` is Tigerclaw's; Bain's instance owes no showcase consumer, and the genuinely new
handler TYPES 8.40b-8.40h mint each owe one here, on milestone 7.9's gate.

Acceptance checks:

- Bain is pickable in the live hero selector, his pick card renders on hover, and a configured match
  composes his hand without throwing.
- His eighteen dealt fronts, his shine sprites and his two team tokens are prepared from the real
  Tabletopia corpus rather than generated placeholders, and the artwork bijection is re-pinned from
  measurement.
- His bounty marker renders on the hero rail with no empty value, proven live.
- Every contract held on a deferred list moves into the registered set with a showcase consumer each, and
  the paired assertion from 8.40i is observed doing its job.
- The registered-handler count, every corpus digest and both scenario-digest families are re-pinned from
  measurement, and any schema or wire movement is stated with its cause.
- His identity pin is minted from a complete unfiltered run and the fourteen prior pins are byte-identical
  on it.
- A checkout without the private corpus still runs, drawing the generated fallbacks exactly as before.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: No sixteenth hero. No new playtest package. No change to the fourteen existing heroes' art or
pins beyond re-measurement.

### [ ] 8.40k Player-path gate Bain's whole printed set through real player input

The permanent content-derived gate every dealt hero carries, and the milestone that answers the user's
"Keep pushing them until hero is fully implemented and tested" on the terms this project can actually
prove.

Observable outcome: `BainPlayerPathGateTests` derives his interaction shapes from his own declarations and
binds every one of them to a named Play Mode journey that drives real controls, with no `[Explicit]` case
and no skip.

**A MATRIX NAMING A SKIPPED JOURNEY IS NOT A PASSING PLAYER-PATH CHECK.** A step that finds no control
fails rather than skips.

**A FILTERED PLAY MODE RUN CANNOT SEE THIS GATE**, so the closing evidence is a complete unfiltered run,
and the same run re-pins the Play Mode baseline.

**THE GATE CANNOT VERIFY THAT A JOURNEY EXERCISES THE SHAPE IT BINDS - AUDIT IT YOURSELF.** A row bound to
a journey that never reaches its control passes the matrix and proves nothing; Swift's dealing recorded
this and it is the one check the derivation cannot make.

**THREE SHAPES HERE HAVE NEVER BEEN DRIVEN BY ANY JOURNEY IN THIS BUILD.** A card chosen from an ENEMY's
seat and held hidden while the controller answers a different question; a REVEAL that changes what every
other seat can see mid-resolution; and a Block that succeeds or fails on a board fact the defender can
read before choosing. The first two must be driven from at least two seats at once - the chooser's and a
losing seat's - because a hidden answer verified only from the host's state is not verified.

**AND THE MARKER IS A PIECE OF STATE A PLAYER MUST BE ABLE TO SEE.** A journey should confirm that the
rail shows who wears the bounty before a Green face's condition is answered, because that condition is
the only thing three of his five Green faces do and a player who cannot see it cannot play them.

Acceptance checks:

- Every interaction shape his declarations produce is bound by name to an active journey; none is
  `[Explicit]`, none self-skips, and Side Quest is declared unreachable with the standing p.20 reason.
- The journeys drive real controls through the UI player driver, including the give and its Ultimate
  discard, the hidden card choice answered from the enemy's own seat, the colour guess, the reveal seen
  from a third seat, the marker-conditioned Block from the defender's seat, the transfer and the return,
  the pull of a token, the clear-line shot past a token, the one-or-both modal, and the conditional
  movement bonus.
- Each journey is audited to prove it actually exercises the shape its row binds, and the audit is
  recorded.
- Undo and snapshot restore are exercised inside at least one journey of each shape, including one taken
  between the hidden choice and the reveal.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision, and the Play
  Mode total is re-baselined with its cause stated.

Non-goals: No new content and no new capability. A journey that needs one is a defect report against
8.40a-8.40i, not a widening here.
