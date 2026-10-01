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

### [ ] 9.30b Build the spellbook zone

Outcome: a hero may own spell cards in a spellbook. `CardLocation` gains the book (facedown) and the removed pile (faceup, an open zone in `PublicCardZoneQuery`); `CardLifecycleAuthority` moves a spell book -> removed and removed -> book; setup creates a declared set of spell instances in the book (GY-Q1); a public per-hero book count and removed list reach every seat while the book's faces reach the owner only; spell definitions carry their own colour, classification and tier. Snapshot v50 -> v51; wire `/30` -> `/31`. Closing check: lifecycle, setup and privacy tests (a non-owner seat's projection names no facedown spell) failing on the base; 9.11's completeness checks see every new field; snapshot and wire round trips; the harness green. T2. Blocks 9.30c-9.30j.

### [ ] 9.30c Cast a spell, and refill the book

Outcome: a capability names spells by definition and casts one: it moves the spell to the removed pile and performs the spell's printed primary through the borrow path, by the casting hero, with the spell's values and his items (GY-Q3). "Choose and cast one" is a `Choose` whose options are greyed when their spell is not in the book. Prepare Spells returns every removed spell and cancels each returned spell's Active effects (designer). Hanu's delegated chooser is offered the in-book names (RZ-Q14's precedent). Composed on synthetic test content only; no Gydion face is registered. Closing check: cast, refill and cancellation tests failing on the base, including a cast Attack defended through p.14 and a cast whose spell cannot act; `OfferOutcomeTests` style offer-against-outcome checks on the staged content; the harness green. T2. Needs GY-Q7.

### [ ] 9.30d Compose the spells that reuse existing contracts

Outcome: Magic Missile, Shocking Grasp, Expeditious Retreat, Burning Hands, Fireball, Vampiric Touch, Create Undead, Dominate Person, Power Word Kill, Disintegrate, Polymorph and Banishment are composed on the cast path from existing capabilities, read against [PRINTED_WORDING](../Rules/PRINTED_WORDING.md) (W3 for Burning Hands, W5/W9 for Shocking Grasp, Exclusion for Fireball). Closing check: one staged Edit Mode outcome test per spell, each failing before composition; `PrintedWordingConformanceTests` on the new faces. T2. Needs GY-Q8.

### [ ] 9.30e Compose the counting spells and the Familiar

Outcome: a count of other removed spells feeds Sunburst's Attack and Range, Dimension Door's Radius (Place yourself at exactly the Radius) and Midas' Touch's coins (rounded down); the Familiar token (supply one, obstacle, removed at round end, moved by a new Place) and Find Familiar's optional removal of up to three other spells. Closing check: count, value and placement tests failing on the base, offers reading the same count as the resolution (lesson 27). T2.

### [ ] 9.30f Compose the immunity, discard, Life and forced-move spells

Outcome: a round-long immunity scoped by attack classification (Shield: basic; Invulnerability: non-basic), a forced discard filtered by classification (Cloud Kill: basic; Energy Drain: non-basic), a team Life counter regained (Energy Drain, GY-Q11), and Suggestion's exact-3 straight-line move of an enemy hero steered by that hero's seat (GY-Q6). Closing check: tests failing on the base, including a refill ending Shield. T2. Needs GY-Q11.

### [ ] 9.30g The Archwizard and Wish

Outcome: the Ultimate offers Wish in place of a refill when Wish is in the book; Wish casts any spell in the book (GY-Q9); a persisted, public per-team Wish count; the third Wish's resolution wins the game for his team through a new `MatchVictoryCause` and the victory screen names it (GY-Q10). GY-Q2 (Wish removed when cast) is re-checked with the user first. Snapshot and wire move. Closing check: replacement, count, restore and victory tests failing on the base; the victory screen label. T2. Needs GY-Q2, GY-Q9, GY-Q10.

### [ ] 9.30h Present the spellbook

Outcome: approved mockups A-D. The spell choice draws spell faces in the decision window, a removed spell greyed with its face (today greyed candidates are worded controls) and a scaling spell captioned with its current values; the SPELLBOOK drawer (owner: faces; others: tier backs with counts; everyone: the faceup pile); the plaque's "SPELLBOOK n | m" chip in the items strip and "n/3" beside the purple Ultimate; the landscape spell footprint; the Familiar's token art; prepared private spell art with a generated fallback. Closing check: view regressions failing on the base; seat-scoped privacy in the drawer; captures for the user's review. T2 with a visual inspection.

### [ ] 9.30i Deal Gydion with his artwork

Outcome: Gydion registered in the roster, pickable with his profile, standee and art; decision labels; the new-hero checklist ([PRINTED_WORDING](../Rules/PRINTED_WORDING.md#required-for-a-new-hero)) run, `OfferOutcomeTests` included; bots answer his decisions. Closing check: as Razzle's 9.21j. T3.

### [ ] 9.30j Gate Gydion on the player path

Outcome: the permanent player-path gate for Gydion, journeys reading outcomes (lesson 4), full harness, Edit Mode and Play Mode green with zero skips. T3.

## Wave plan (agents, as Razzle)

- 9.30 (inventory) on main first.
- Wave one, in parallel worktrees: 9.30a (presentation only, independent) and 9.30b (engine foundation).
- After 9.30b lands: 9.30c. After 9.30c lands: 9.30d, 9.30e and 9.30f in parallel, then 9.30g; 9.30h after 9.30c (its "n/3" waits on 9.30g).
- 9.30i, then 9.30j, sequential.
- Each lands on main one at a time with its own validation and commit; Unity runs stay serial on the root checkout.
