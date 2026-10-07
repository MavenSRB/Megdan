# Phase 9 - The twenty-ninth hero: Gydion

Status: [route](../SESSION_BRIEF.md). Rules: [HERO_GYDION.md](../Rules/HERO_GYDION.md) (created by 9.30).

Authorized 2026-10-01 by the user ("Gydion the Archwizard! ... implement Gydion the Archwizard", after 9.15, 9.28, 9.23 and 9.29 land; "use agents, same as Razzle"). Gydion was the user's pick of the four undealt heroes (Emmitt, Gydion, NebKher, Takahide). Every one of his faces is a Skill that casts a named spell from his spellbook: twenty-two spell cards outside his deck, each with its own printed primary action, removed faceup as cast and returned by Prepare Spells. Three spells grow with the count removed, one places a Familiar token, two grant round-long immunities that a refill ends (the designer's answer), and his Ultimate turns a refill into Wish, three of which win the game for his team. A card zone that is no hand, deck or discard; an action performed from a card that is in none of them; and a team victory from card text are new to the engine.

Eighteen ordinary fronts are dealt; the nineteenth, Cheap Trick, is a p.20 Handicap and stays boxed at setup. The blocks below are mapped by 9.30. Each starts only after the GY questions it names are answered, in the order listed. The user approved the presentation as mockups on 2026-10-01 (`tmp/9-30-gydion-approved-mockups/`): a built screen that matches its mockup lands and its captures wait for the user's review; one that must differ waits for the user.

## Carried lessons

[Razzle's list](PHASE_9_TWENTY_EIGHTH_HERO.md#carried-lessons) binds here unchanged: lessons 1-19 through Mortimer and Razzle's 20-23. Four more bind:

24. **A spell is a card, not a value.** Every spell is a `CardInstance` Gydion owns, with a definition, classification and tier of its own, so the borrow path, items, immunity, Defense windows, the log and the snapshot read it as they read any card. Nothing branches on a spell's name.
25. **The book is a zone with an audience.** Its facedown members reach only Gydion's seat; the faceup pile, the counts and the tier backs reach every seat. Every new card fact is proven through 9.11's completeness checks on `SeatProjectionFactory`, the clones and the wire codecs, and the privacy through a seat projection that must not name a facedown spell.
26. **A refill is a state change.** Returning a spell is the p.17 state change that ends its Active effect (designer, 2026-08-22). Tests stage Shield and Invulnerability running when Prepare Spells resolves, and when the Ultimate's Wish replaces the refill (the effect stays).
27. **Count what the card counts.** "Other spell cards removed" excludes the casting spell and is read when the value is asked; offers read the same count the resolution reads (lesson 18).

### [x] 9.30 Inventory Gydion and map the implementation sequence

Outcome: verify all nineteen fronts, the twenty-two spells, the profile, dashboard, standee and Familiar evidence in the Tabletopia corpus; record printed values and exact text (reminder text included), the item icons by montage, the rulebook intersections (card states, Skill, Attack, Defense, Place, Swap, Remove, Respawn, tokens, Active effects, victory), the designer's two answers and the community corroboration, the existing-capability matches and the new contracts, the approved presentation, and every unresolved interpretation as GY-Qn with the block it blocks. No executable content is registered. Acceptance: nineteen distinct fronts and two back variants accounted for, twenty-two spells and four spell-back variants accounted for; selection roles and complexity matching the profile; GY-Q1-GY-Q6 recorded as the user's answers, GY-Q7-GY-Q11 recorded with recommendations; blocks 9.30a-9.30j mapped; living documentation green. T0.
Completed 2026-10-01. Nineteen byte-distinct fronts with two back variants and twenty-two byte-distinct spell fronts with four tier-back variants verified; the profile (four stars; Sniper, Tactician, Farming, Damager, Pusher, Tokens) and dashboard (Attack 5+1, Initiative 3, Defense 3+2, Movement 1+3; items Attack x3, Defense x3, Initiative x3, Movement, Range, Radius by montage); standee element 8629710 and the Familiar token element 5831117; the designer's two answers recorded at ledger level 4 and the community's three as corroboration. The engine survey found the borrow path, the Choose node, source-card cancellation and most spell capabilities reusable, and named the new contracts: the spellbook zone, casting by definition, a team victory from card text, a regained Life, classification-scoped immunity and discard, and a removed-spell count. GY-Q1-GY-Q6 settled by the user, GY-Q7-GY-Q11 recorded with recommendations in [his shard](../Rules/HERO_GYDION.md); blocks 9.30a-9.30j mapped above.
Historical evidence: [9.30 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930-gydion-inventory-evidence).

### [x] 9.30a Every unlocked Ultimate is a purple card in the items strip

Outcome: the rail plaque's separate ULTIMATE row (`gd-ultimate`, `HeroOverviewsUnityView.cs:695`) is removed for every hero; an unlocked Ultimate is drawn as a small purple mini card inside the items strip (`gd-bonus-strip`), revealing the same full face on a pointer rest and reading `UNLOCKED` as 8.15 requires, with no change to rules, wire or snapshot. Matches approved mockup D (other heroes). Closing check: a view regression that the plaque draws no `gd-ultimate` row and the purple card sits in the strip, failing on the base; `MatchHudPresentationTests`, `HeroRailCoinCheck`, `MinimalHexGridPlayModeTests` and every journey that finds `gd-ultimate-card` (Brogan, Arien, Bain, Garrus, Xargatha, Wuk, Mrak, Mortimer, Wasp, Rowenna, Razzle x2, Trinkets, Tigerclaw, Snorri) moved and green; captures of an unlocked plaque at the three validated viewports for the user's review. T2 with a visual inspection. Independent of every other block.
Completed 2026-10-01. The ULTIMATE row (`gd-ultimate`) is gone for every hero; the unlocked Ultimate is a 17x24 card among the item chips (`gd-item-chips` inside `gd-bonus-strip`), last on a plain rail and first, beside the coin, on a mirrored one, no taller than the coin, still revealing its face and reading `UNLOCKED`, and drawn in `MatchHudTheme.UltimateFace` (the printed Ultimate purple) because an Ultimate's profile states no colour and the colour-keyed face had drawn it, row and strip alike, as an empty slot. `UltimateLabel` is removed; fifteen journeys find the card through `UiPlayer.ItemStripDescendants`. Regressions `TheRail_DrawsTheUnlockedUltimateInsideTheItemsStripWithNoRowOfItsOwn` and `TheRail_DrawsTheStripsUltimateInTheUltimatePurpleNotTheEmptySlotGrey` failed on their bases and pass; `MinimalHexGridPlayModeTests.Scene_UnlockedUltimateSitsInsideTheItemsStripAtTheThreeValidatedViewports` writes the user's review captures to `tmp/9-30a-ultimate-strip/`. Not measured: a plaque holding a marker and an unlocked Ultimate together.
Historical evidence: [9.30a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930a-ultimate-in-the-items-strip-evidence).

### [x] 9.30b Build the spellbook zone

Outcome: a hero may own spell cards in a spellbook. `CardLocation` gains the book (facedown) and the removed pile (faceup, an open zone in `PublicCardZoneQuery`); `CardLifecycleAuthority` moves a spell book -> removed and removed -> book; setup creates a declared set of spell instances in the book (GY-Q1); a public per-hero book count and removed list reach every seat while the book's faces reach the owner only; spell definitions carry their own colour, classification and tier. Snapshot v50 -> v51; wire `/30` -> `/31`. Closing check: lifecycle, setup and privacy tests (a non-owner seat's projection names no facedown spell) failing on the base; 9.11's completeness checks see every new field; snapshot and wire round trips; the harness green. T2. Blocks 9.30c-9.30j.
Completed 2026-10-01. `CardLocation.Spellbook` (facedown) and `RemovedFromSpellbook` (faceup, an open zone in `PublicCardZoneQuery`) appended; `CardLifecycleAuthority.TryRemoveFromSpellbookFaceup` / `TryReturnToSpellbook` refuse every other source; setup creates a declared spell set in the book (`CardInstance.CreateInSpellbook`, `ConfiguredHand.SpellbookInstances`); spells carry `CardProgressionProfile.SpellCard` (`IsSpell`, no Item icon, never an upgrade candidate, no Initiative); `SeatHandSummary.SpellbookCount` reaches every seat and `IMatchReadModel.HandSummaries` lets a network client read it; a non-owner seat's projection and wire payload name no facedown spell. Snapshot v50 -> v51 (v50 refused, sixteen corpus captures re-pinned, `spellbook-v51` added); wire `/30` -> `/31`; every content identity pin moved for the new `IsSpell` property with no content change. Proven on `SyntheticSpellbook`; eight `SpellbookZoneTests` failed first.
Historical evidence: [9.30b completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930b-spellbook-zone-evidence).

### [x] 9.30c Cast a spell, and refill the book

Outcome: a capability names spells by definition and casts one: it moves the spell to the removed pile and performs the spell's printed primary through the borrow path, by the casting hero, with the spell's values and his items (GY-Q3). "Choose and cast one" is a `Choose` whose options are greyed when their spell is not in the book. Prepare Spells returns every removed spell and cancels each returned spell's Active effects (designer). Hanu's delegated chooser is offered the in-book names (RZ-Q14's precedent). Composed on synthetic test content only; no Gydion face is registered. Closing check: cast, refill and cancellation tests failing on the base, including a cast Attack defended through p.14 and a cast whose spell cannot act; `OfferOutcomeTests` style offer-against-outcome checks on the staged content; the harness green. T2. Needs GY-Q7.
Completed 2026-10-01. `cast-spell@1` names a spell through a per-spell naming step (`SpellInBookHandler`, the definition bound at registration), checks it through `BorrowedPrimaryActionQuery.EligibleCards` with the new pool `OwnedSpellbookSpells` (owner and caster are separate inputs; only the owner filter would change for a non-owner caster), moves it book -> removed as its own transition and performs its primary through the borrow path, which `BorrowedActionQuery.Requested` now recognises; `refill-spellbook@1` returns every removed spell and cancels each one's Active effects. By the user's rulings a cast is playable whenever its spell is in the book (GY-Q12) and an empty refill is always playable (GY-Q7, against the recommendation: the recommended grey was built, reverted and kept in history). A cast Attack is defended through p.14 and restores at its Defense pause; Hanu's delegated seat is offered the in-book names and never a facedown id; `GuessableColors` and sandbox staging no longer count spellbook cards. No schema or wire move. Proven on synthetic spells; registration is 9.30i's.
Historical evidence: [9.30c completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930c-cast-and-refill-evidence).

### [x] 9.30d Compose the spells that reuse existing contracts

Outcome: Magic Missile, Shocking Grasp, Expeditious Retreat, Burning Hands, Fireball, Vampiric Touch, Create Undead, Dominate Person, Power Word Kill, Disintegrate, Polymorph and Banishment are composed on the cast path from existing capabilities, read against [PRINTED_WORDING](../Rules/PRINTED_WORDING.md) (W3 for Burning Hands, W5/W9 for Shocking Grasp, Exclusion for Fireball). Closing check: one staged Edit Mode outcome test per spell, each failing before composition; `PrintedWordingConformanceTests` on the new faces. T2. Needs GY-Q8.
Completed 2026-10-01. The twelve spells compose on the cast path; five needed small vocabulary the survey had not predicted (Fireball's not-adjacent-to-your-friendly-unit target, Create Undead on the Range channel, Disintegrate's minion class, Polymorph's minions-and-tokens subjects, Expeditious Retreat's straight-line-only Movement), each recorded in [his shard](../Rules/HERO_GYDION.md) under "Built by 9.30d". No schema or wire move.
Historical evidence: [9.30d completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930d-reuse-spells-evidence).

### [x] 9.30e Compose the counting spells and the Familiar

Outcome: a count of other removed spells feeds Sunburst's Attack and Range, Dimension Door's Radius (Place yourself at exactly the Radius) and Midas' Touch's coins (rounded down); the Familiar token (supply one, obstacle, removed at round end, moved by a new Place) and Find Familiar's optional removal of up to three other spells. Closing check: count, value and placement tests failing on the base, offers reading the same count as the resolution (lesson 27). T2.
Completed 2026-10-01. Scope widened, within the authorized sequence, to declare all twenty-two spell definitions (so 9.30d, f, g and h key on stable ids) while composing only Sunburst, Dimension Door, Midas' Touch and Find Familiar; the removed-spell count, the Familiar token and its private face are as [his shard](../Rules/HERO_GYDION.md) records under "Built by 9.30e". By GY-Q12 each of the four is castable with nothing to do. A shared change: the Attack Range path reads the cards, so a full Play Mode run checked every hero's attacks. No schema, wire, registration or content-digest move.
Historical evidence: [9.30e completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930e-counting-spells-and-familiar-evidence).

### [x] 9.30f Compose the immunity, discard, Life and forced-move spells

Outcome: a round-long immunity scoped by attack classification (Shield: basic; Invulnerability: non-basic), a forced discard filtered by classification (Cloud Kill: basic; Energy Drain: non-basic), a team Life counter regained (Energy Drain, GY-Q11), and Suggestion's exact-3 straight-line move of an enemy hero steered by that hero's seat (GY-Q6). Closing check: tests failing on the base, including a refill ending Shield. T2. Needs GY-Q11.
Completed 2026-10-01. Shield and Invulnerability are classification-scoped Attack immunities sourced on the spell; Cloud Kill and Energy Drain discard by classification; Energy Drain regains only a spent Life (GY-Q11); Suggestion's exact-three line is chosen by the moved hero's seat (GY-Q6). Snapshot v52 -> v53 (each team's starting Life counters). Detail in [his shard](../Rules/HERO_GYDION.md) under "Built by 9.30f".
Historical evidence: [9.30f completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930f-immunity-discard-life-and-move-evidence).

### [x] 9.30g The Archwizard and Wish

Outcome: the Ultimate offers Wish in place of a refill when Wish is in the book; Wish casts any spell in the book (GY-Q9); a persisted, public per-team Wish count; the third Wish's resolution wins the game for his team through a new `MatchVictoryCause` and the victory screen names it (GY-Q10). GY-Q2 (Wish removed when cast) is re-checked with the user first. Snapshot and wire move. Closing check: replacement, count, restore and victory tests failing on the base; the victory screen label. T2. Needs GY-Q2, GY-Q9, GY-Q10.
Completed 2026-10-01. Wish casts any in-book spell and counts toward a per-team tally; the third Wish's resolution wins for his team (`MatchVictoryCause.WishesCast`); The Archwizard offers Wish in place of a refill, a full book included (GY-Q7). Snapshot v51 -> v52, wire /32 -> /33. It also fixed a pre-existing defect: a match ended by a card claim could not be restored. Detail in [his shard](../Rules/HERO_GYDION.md) under "Built by 9.30g".
Historical evidence: [9.30g completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930g-archwizard-and-wish-evidence).

### [x] 9.30h Present the spellbook

Outcome: approved mockups A-D. The spell choice draws spell faces in the decision window, a removed spell greyed with its face (today greyed candidates are worded controls) and a scaling spell captioned with its current values; the SPELLBOOK drawer (owner: faces; others: tier backs with counts; everyone: the faceup pile); the plaque's "SPELLBOOK n | m" chip in the items strip and "n/3" beside the purple Ultimate; the landscape spell footprint; the Familiar's token art; prepared private spell art with a generated fallback. Closing check: view regressions failing on the base; seat-scoped privacy in the drawer; captures for the user's review. T2 with a visual inspection.
Completed 2026-10-01. Approved mockups A-D built: spell faces in the decision window (removed greyed, counting spells captioned with the value the cast will use, items included), Find Familiar's up-to-three pick, the SPELLBOOK drawer and chip, tier backs on every seat, log rows for casts, removals and refills (wire /31 -> /32), the landscape spell footprint and the private spell art. Unity found each spell drawn twice and removed spells in Find Familiar's pick; both fixed before landing. Detail in [his shard](../Rules/HERO_GYDION.md) under "Built by 9.30h".
Historical evidence: [9.30h completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930h-spellbook-presentation-evidence).

### [x] 9.30h1 Spell faces keep their printed proportions and read larger

Authorized 2026-10-01 by the user from the 9.30h captures ("looks to me scaling is a bit off for those spells, and that they might be too small"), a user-directed amendment to the approved mockups. Outcome: every spell face and back keeps the printed 372:260 wherever it is drawn; the spell choice and the drawer draw faces at the action card's width (about 300x210), Find Familiar's pick at two thirds in rows of at most seven; the drawer stays above the hand's band and scrolls. Closing check: a ratio regression failing on the base, minimum widths per surface, in-game captures for the user. T2 with a visual inspection.
Completed 2026-10-01. The squeeze had two causes: Unity imported the 372x260 scans at 256x256 (the non-power-of-two rescale; `ConfiguredCardArtImporter` now covers `Private/Spells`, version 3) and the face fitted that square scan inside its 372:260 box (now `StretchToFill`). Faces are full size in the choice and the drawer (`DrawerScale` 1), two thirds in the pick, greyed faces dimmed less; the drawer is bounded to the reserved rail columns with a scrolling body. At 2560x540 the SPELLBOOK chip spilled out of the items strip because the chips row measured itself on one line and wrapped at another width; the row now takes the strip's width beside the coin (`flexBasis 0`, `flexGrow 1`) and every strip word is `NoWrap`. Captures `tmp/9-30h1-spellbook/` sent to the user.
Historical evidence: [9.30h1 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930h1-spell-face-proportions-and-size-evidence).

### [x] 9.30i Deal Gydion with his artwork

Outcome: Gydion registered in the roster, pickable with his profile, standee and art; decision labels; the new-hero checklist ([PRINTED_WORDING](../Rules/PRINTED_WORDING.md#required-for-a-new-hero)) run, `OfferOutcomeTests` included; bots answer his decisions. Closing check: as Razzle's 9.21j. T3.

Completed 2026-10-01. `hero-gydion` joins the roster as "Gydion the Archwizard" (four stars; Sniper, Tactician, Farming, Damager, Pusher, Tokens): Cantrip and the fifteen coloured fronts compose "Choose and cast one" as a `Choose` of cast arms in printed order, Cheap Trick boxed; the p.6 hand Cantrip, Prepare Spells and the three Elementaries over a thirteen-card deck, twenty-two spells in the book (GY-Q1). Revision `gydion-9-30i-rev-1`; every deferred list registered (handler types 134 -> 143, capabilities 312 -> 350, the Familiar token), 78 decision labels, staged scenes 142 and 143, bots answering every new family, the new-hero checklist run with `OfferOutcomeTests` allow-listing the empty refill under GY-Q7. Corpus re-pinned (+1,208 bytes, no schema move); private art `Cards/gydion/` (18), `HeroSelection/hero-gydion.png`, `Heroes/gydion-red|blue.png`; shine sprites regenerated from the Edit run's manifest (66 written). Detail in [his shard](../Rules/HERO_GYDION.md) under "Built by 9.30i".
Historical evidence: [9.30i completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930i-dealing-evidence).

### [x] 9.30j Gate Gydion on the player path

Outcome: the permanent player-path gate for Gydion, journeys reading outcomes (lesson 4), full harness, Edit Mode and Play Mode green with zero skips. T3.

Completed 2026-10-01. `GydionPlayerPathGateTests` maps 41 face rows and 6 components (piece, session, footprint, items, spellbook, Familiar; Cheap Trick boxed) to 31 real-input journeys in `GydionUiJourneyPlayModeTests` and `GydionSpellUiJourneyPlayModeTests` plus `GydionDealingPlayModeTests`, every one reading what its answer changed; `HeroPlayerPathMatrix` gains `SpellCastFromTheBook` and `SpellFaceChoice` with every earlier hero's rows unchanged, and Suggestion's enemy-seat direction is answered on that seat's own HUD. The board was measured first by a headless probe. The journeys found two presentation defects, fixed with regressions: Find Familiar's third pick auto-confirmed (a spell pick now waits for CONFIRM, as the user ruled), and Prepare Spells' Wish-instead face lost its caption and its window was worded as a spell choice (now "THE ARCHWIZARD", "Add all spell cards to the spellbook, or cast Wish instead."). Not covered: Hanu's delegated seat answering a Gydion spell choice in Play Mode (proved at the rules layer by `SpellCastTests`).
Historical evidence: [9.30j completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930j-gate-evidence).

### [x] 9.30k Make Gydion's spell access and casting-card hover explicit

Authorized 2026-10-07 by the user. Outcome: the hand spellbook shortcut uses the same live `SPELLBOOK n | m` visual language as Gydion's hero-tray chip and remains available to Gydion's joined non-host seat; resting the pointer on any projected casting card shows the spell faces declared by that card's typed cast arms, with spells in the public removed-from-spellbook pile faded and captioned `REMOVED`. The reading stays definition-driven and generic: no hero-name, card-name, or printed-text branch. Closing checks: pure presentation regressions for typed cast-arm order and removed-state fading; view regressions for the shared shortcut treatment and attached spell faces; a seat-scoped Play Mode journey proving the joined non-host Gydion seat sees and opens the shortcut. T2 with a visual inspection at the existing validated viewports.

Completed 2026-10-07. The above-hand shortcut now carries the tray chip's gold treatment, live `SPELLBOOK n | m` label and assistive count; Gydion's owner-scoped seat retains it after the same host-tool transition online admission uses and opens the existing local drawer without changing match state. Every tray card projects the spells declared by its typed cast arms in definition order, so compact hand previews, full-size hand cards, turn slots, current cards and discards share one `CAN CAST` companion; a spell in the public faceup removed pile is faded and captioned `REMOVED`, with no facedown instance exposed and no Gydion/card-name/printed-text branch. The Play Mode run exposed and closed the wide-layout gap where full-size cards deliberately raise no detached preview. Focused `SpellbookPresentationTests` passed 18/18; `GydionDealingPlayModeTests` passed 6/6; the joined-seat Cantrip capture was inspected at the live viewport. No rule, snapshot or wire change.

Historical evidence: [9.30k completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-930k-gydion-casting-hover-completion).

### [x] 9.74 Stack Gydion's castable-spell hover above its casting card

Authorized 2026-10-07 by the user's review of the 9.30k Cantrip hover capture. Outcome: the
definition-driven `CAN CAST` companion stacks its declared spell faces vertically above the hovered
casting card instead of extending beside it in a horizontal row. The companion remains attached to
the complete casting-card preview on compact and wide hand surfaces, stays pointer-transparent and
inside the validated viewport, and preserves typed cast-arm order. A spell already in the public
removed-from-spellbook pile remains faded and is captioned `REMOVED`. No rule, card content, command,
snapshot or wire change. Closing checks: focused presentation regressions for vertical geometry,
above-card placement and removed-state treatment; the joined non-host Gydion Play Mode hover journey;
fresh 1920x1080 captures with all declared spells in the book and with one declared spell already
removed. T2.

Completed 2026-10-07. `CastableSpellHoverView` is now one face wide and computes the full vertical
height of its ordered spell column, centred immediately above the casting card. Compact hand and rail
previews reserve that height when they clamp the complete face, while wide hand faces attach the same
pointer-transparent companion directly. Public removed spells retain their faded face and `REMOVED`
caption. The geometry/removed-state regression passed inside 154/154 affected Edit Mode checks; the
joined non-host Gydion fixture passed 6/6 Play Mode checks and wrote fresh 1920x1080 all-in-book and
one-removed captures. The initial focused run exposed a Play Mode fixture-only assembly-access compile
error; the fixture now reaches the existing internal staging helper through reflection, and both the
focused rerun and final affected runs are green. No gameplay, content, command, snapshot, wire or
package changed.

Historical evidence: [9.74 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-974-vertical-castable-spell-hover-completion).

## Wave plan (agents, as Razzle)

- 9.30 (inventory) on main first.
- Wave one, in parallel worktrees: 9.30a (presentation only, independent) and 9.30b (engine foundation).
- After 9.30b lands: 9.30c. After 9.30c lands: 9.30d, 9.30e and 9.30f in parallel, then 9.30g; 9.30h after 9.30c (its "n/3" waits on 9.30g).
- 9.30i, then 9.30j, sequential.
- Each lands on main one at a time with its own validation and commit; Unity runs stay serial on the root checkout.
