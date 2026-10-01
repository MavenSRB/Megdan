# Guards Digital - Gydion Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The twenty-ninth-hero sequence](../Roadmap/PHASE_9_TWENTY_NINTH_HERO.md) owns acceptance and status.

## Source and rule authority

Gydion the Archwizard is the selected twenty-ninth hero (user, 2026-10-01). The curated Tabletopia source `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Gydion/` holds nineteen byte-distinct fronts and nineteen backs in two byte variants (`1a0ab2ce...` on eleven, `f437dbe3...` on eight; a per-hero fact, as for Bain, Cutter, Garrus and others). **His twenty-two spell cards are not in that folder:** they are `cards/other-cards/spells-tt*__front.png` (372x260 landscape, Tabletopia names `spells-tt`, `spells-tt2` .. `spells-tt22`, not in action-card order), with four back variants that print the spell's tier (`57e1a240` I, `1cbb193b` II, `c23bbd2c` III, `12933056` the Wish lamp). All fronts, the spells, the profile and the dashboard were read on 2026-10-01; item icons by montage against Wasp's six; Range and Radius icons by montage. These are source references, not distribution permission.

GY-01, profile: `cards/hero-selection/randomizers-tt32__7723694__front.png`, byte-identical to `active-hero-cards/_selection-cards/Gydion__front.png` (SHA-256 `d07b3b61d0a803e6b632e39c749c9311392966b47a3b7674a0cdf53cb2c45420`). **Four stars**; Sniper, Tactician, Farming, Damager, Pusher, Tokens.

GY-02, dashboard: `cards/hero-dashboards/dashboards-tt-resized29-017__7732127__front.png` (element 7732127), title **"Gydion the Archwizard"**. Bottom bars, each of eight, read at 3x against Hanu's recorded values: Attack **5** + **1** brighter, Initiative **3**, Defense **3** + **2** brighter, Movement **1** + **3** brighter (p.21: brighter = conditional). The profile card agrees.

GY-03, standee and token: standee element **8629710** `Gydion` (publicElementId 5019250, VerticalTokens). The Familiar token is element **5831117** `raven` (Tokens, Hexagonal, one element): a white owl holding a book, `Image_0_Atlas` (`extracted/text-assets/g713916_s799373.sprite0__45.jpg`) at x 2058, y 2526, 335x291.

GY-04, rulebook: the local second-printing text names no spell, spellbook, Familiar or Gydion. General authority: p.7 one action per turn; p.9 basic cards (Gold, Silver) and Range/Radius icons; p.10 card states, modal choice, retrieve ("one of your cards"); p.12 Skill; p.13 Attack, Defeat, coins, Lives; p.14 Defense; p.16 items ("as long as your hero is on the board" for the Ultimate); p.17 Active effects ("cancelled ... when the card changes state"); p.18-19 Immune, Obstacle, Place, Remove, Respawn, Swap, Target, Tokens ("All tokens are obstacles"; removed at end of round unless text says otherwise; not removed when the placing hero is defeated); p.20 Handicap.

GY-05, designer and community answers (Discord screenshot supplied by the user on 2026-10-01). **Designer (Arty, OP), ledger level 4:**
- 2026-08-22: when Prepare Spells puts all spells back, an Active effect such as Shield ends - "Correct. Card changes state - becomes facedown." (p.17's state-change cancellation; the spell card is the effect's source).
- 2026-06-13: "Only Gydion can see the spells" does not stop NebKher's gold card from casting from Gydion's spellbook - "Nebkher must choose the spells by name, without looking at them first."
**Community (Tuck9090, not OP), corroboration only** under the ledger's no-unverified-forum-answer rule: Prepare Spells adds *all* spells, so a newly upgraded card's spells are ready unless removed; the Ultimate replaces the silver's refill with Wish; the Familiar is an ordinary obstacle token.

Printed fronts control each clause under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy).

## Printed inventory

File stems are under the Gydion source directory and end in `__front.png`. D/M are the ribbon's Defense and Movement icons. **No action card prints an Attack, Range or Radius value or icon: every value is on the spell.**

| Stem | Face / colour / tier | Init | D | M | Primary | Item |
| --- | --- | --- | --- | --- | --- | --- |
| gydion__7435055 | Cantrip / Gold / - | 11 | 1 | - | Basic Skill | - |
| gydion2__7435050 | Prepare Spells / Silver / - | 13 | 2 | - | Basic Skill | - |
| gydion3__7721610 | Elementary Evocation / Red / I | 7 | 6 | 3 | Skill | - |
| gydion4__7721611 | Lesser Evocation / Red / II | 8 | 6 | 3 | Skill | Defense |
| gydion5__7721612 | Lesser Necromancy / Red / II | 8 | 6 | 3 | Skill | Initiative |
| gydion6__7721613 | Greater Evocation / Red / III | 8 | 7 | 3 | Skill | Defense |
| gydion7__7721614 | Greater Necromancy / Red / III | 8 | 7 | 3 | Skill | Radius |
| gydion8__7589829 | Elementary Enchantment / Blue / I | 9 | 3 | 2 | Skill | - |
| gydion9__7589830 | Lesser Enchantment / Blue / II | 10 | 4 | 2 | Skill | Defense |
| gydion10__7589826 | Lesser Transmutation / Blue / II | 10 | 4 | 2 | Skill | Attack |
| gydion11__7589827 | Greater Enchantment / Blue / III | 10 | 4 | 2 | Skill | Range |
| gydion12__7589828 | Greater Transmutation / Blue / III | 10 | 4 | 2 | Skill | Attack |
| gydion13__7721615 | Elementary Abjuration / Green / I | 5 | 2 | 2 | Skill | - |
| gydion14__7721616 | Lesser Abjuration / Green / II | 4 | 3 | 2 | Skill | Initiative |
| gydion15__7721617 | Lesser Conjuration / Green / II | 4 | 3 | 2 | Skill | Attack |
| gydion16__7721618 | Greater Abjuration / Green / III | 4 | 3 | 2 | Skill | Initiative |
| gydion17__7721619 | Greater Conjuration / Green / III | 4 | 3 | 2 | Skill | Movement |
| gydion18__7692480 | The Archwizard / Purple / IV | - | - | - | Ultimate | - |
| gydion19__7692479 | Cheap Trick / Gold / Handicap | 10 | 1 | - | Basic Skill | - |

Items: Attack x3, Defense x3, Initiative x3, Movement, Range, Radius (the usual spread). Cheap Trick's corner letter reads "G" under a gold ribbon; it is boxed at setup (p.20), so recorded only. Upgrade lines (name family, validated against p.15 at dealing):
- Red: Elementary Evocation -> Lesser Evocation or Lesser Necromancy -> Greater Evocation or Greater Necromancy.
- Blue: Elementary Enchantment -> Lesser Enchantment or Lesser Transmutation -> Greater Enchantment or Greater Transmutation.
- Green: Elementary Abjuration -> Lesser Abjuration or Lesser Conjuration -> Greater Abjuration or Greater Conjuration.

### Spells

File stems are under `cards/other-cards/` and end in `__front.png`. A/R/r are Attack, Range and Radius. Range icon (crosshair) and Radius icon (ring) verified by montage.

| Stem | Spell / colour / tier | Primary | Values | Text |
| --- | --- | --- | --- | --- |
| spells-tt__7721627 | Magic Missile / Gold / basic | Basic Attack - Ranged | A 1, R 3 | Target a unit in range and not adjacent to you. |
| spells-tt2__7711636 | Shocking Grasp / Gold / basic | Basic Attack | A 3 | Target a unit adjacent to you. After the attack: Move the target up to 1 space. |
| spells-tt3__7711634 | Expeditious Retreat / Gold / basic | Basic Movement | M 5 | Move only in a straight line. |
| spells-tt4__7711639 | Burning Hands / Red / I | Attack | A 5 | Target a unit adjacent to you. Before the attack: Up to 1 enemy hero adjacent to the target discards a card, if able. |
| spells-tt5__7711641 | Fireball / Red / II | Attack - Ranged | A 5, R 3 | Target a unit in range, not adjacent to you or to your friendly unit. |
| spells-tt6__7711651 | Sunburst / Red / III | Attack - Ranged | A 0+, R 0+ | Target a unit at maximum range. +1 Attack and +1 Range for each other spell card removed from the spellbook. |
| spells-tt7__7711640 | Vampiric Touch / Red (skull) / II | Attack | A 5 | Target a unit adjacent to you. After the attack: You may retrieve a discarded card. |
| spells-tt8__7711645 | Create Undead / Red (skull) / II | Skill - Ranged | R 3 | Respawn a friendly minion in an empty friendly spawn point in range in the battle zone. |
| spells-tt9__7711652 | Energy Drain / Red (skull) / III | Skill - Ranged | R 3 | An enemy hero in range discards a non-basic card, if able. Your team regains 1 spent Life counter. |
| spells-tt10__7711638 | Suggestion / Blue (eye) / I | Skill | r 3 | If able, an enemy hero in radius moves 3 spaces in a straight line. |
| spells-tt11__7711644 | Dominate Person / Blue (eye) / II | Skill | r 3 | Target an enemy hero in radius. Defeat an enemy minion in radius adjacent to the target. |
| spells-tt12__7711650 | Power Word Kill / Blue (eye) / III | Skill | r 3 | Defeat an enemy hero in radius with no cards in hand. |
| spells-tt13__7711646 | Midas' Touch / Blue (ring) / II | Skill | - | Gain 1 coin for every two other spell cards removed from the spellbook. |
| spells-tt14__7711647 | Disintegrate / Blue (ring) / II | Skill | - | Remove a token or an enemy non-heavy minion adjacent to you. |
| spells-tt15__7711654 | Polymorph / Blue (ring) / III | Skill | r 3 | Swap an enemy hero in radius with a token in radius or with an enemy minion in radius. |
| spells-tt16__7711635 | Shield / Green (hand) / I | Skill | - | **This round:** You are immune to basic attacks. |
| spells-tt17__7711642 | Banishment / Green (hand) / II | Skill | r 3 | Place a unit or a token adjacent to you into a space in radius. |
| spells-tt18__7711649 | Invulnerability / Green (hand) / III | Skill | - | **This Round:** You are immune to non-basic attacks. |
| spells-tt19__7711648 | Find Familiar / Green (diamond) / II | Skill | r 3 | Place a [Familiar] token in radius. You may remove up to three other spell cards from the spell book faceup. |
| spells-tt20__7725887 | Dimension Door / Green (diamond) / II | Skill | r 0+ | Place yourself into a space at maximum radius. +1 [Radius] for each other spell card removed from the spellbook. |
| spells-tt21__7711653 | Cloud Kill / Green (diamond) / III | Skill | r 3 | An enemy hero in radius discards a basic card, if able. |
| spells-tt22__7711655 | Wish / Purple (lamp) / IV | Skill | - | Cast any spell in the Spellbook. After you cast the "Wish" spell three times your team wins the game. |

### Printed clauses (action cards)

Quoted from the fronts; tests use the front, not this routing copy. Reminder text in parentheses.

- **Cantrip:** "Choose and cast one spell in the spellbook — "Magic Missile" (To blast at short range!) • "Expeditious Retreat" (To dart a great distance!) • "Shocking Grasp" (To repel an adjacent assailant!)"
- **Prepare Spells:** "Add all spell cards to the spellbook. Only Gydion can look at the cards in the spellbook. As the spell is cast, remove it from the spellbook faceup. The action on the spell card is performed by the hero casting the spell."
- **Elementary Evocation:** "Cast the "Burning Hands" spell in the spellbook. (Burn multiple opponents in near vicinity.)"
- **Lesser / Greater Evocation:** "Choose and cast one spell in the spellbook — "Burning Hands" • "Fireball" [• "Sunburst"]" (Wait for your allies to clear the area before hurling a fireball! / Sunburst's reach and power grows with every cast spell.)
- **Lesser / Greater Necromancy:** "... "Vampiric Touch" • "Create Undead" [• "Energy Drain"]" (Harm your foe to heal yourself. Raise an undead servant. / Drain energy from a foe in sight to restore your vitality.)
- **Elementary Enchantment:** "Cast "Suggestion" spell in the spellbook. (Compel the opposing champion to take a few extra steps.)"
- **Lesser / Greater Enchantment:** "... "Suggestion" • "Dominate Person" [• "Power Word Kill"]" (Turn the dominated person against their own troops. / Command a weakened foe within earshot to perish instantly.)
- **Lesser / Greater Transmutation:** "... "Midas Touch" • "Disintegrate" [• "Polymorph"]" (Create gold. Turn objects and lesser creatures into dust. / Polymorph enemies into creatures or objects and vice versa.) The action cards print "Midas Touch"; the spell prints "Midas' Touch". One spell.
- **Elementary Abjuration:** "Cast the "Shield" spell in the spellbook. (The magic shield protects against mundane threats.)"
- **Lesser / Greater Abjuration:** "... "Shield" • "Banishment" [• "Invulnerability"]" (Touch anything to have it banished where you please. / Invulnerability makes you impervious to deadly perils.)
- **Lesser / Greater Conjuration:** "... "Find Familiar" • "Dimension Door" [• "Cloud Kill"]" (Summon a familiar to assist you. Travel to a faraway place. / Create a deadly cloud within distance to weaken your foe.)
- **The Archwizard:** "Whenever you would add cards to the spellbook, you may cast the "Wish" spell in the spellbook instead."
- **Cheap Trick (boxed Handicap):** "Cast "Expeditious Retreat" spell in the spellbook. (To dart a great distance!)"

## Readings the rulebook, the cards and the designer settle

- **The spellbook is Gydion's zone of twenty-two spell cards**, never a hand, deck, discard or resolved slot. A spell is either in the book (facedown) or removed (faceup, public). Spells are not his cards for p.10's retrieve, Defense, discard counts or hand-size reads; Vampiric Touch retrieves an action card.
- **Casting** (Prepare Spells' text): the named spell must be in the book; it is removed faceup as it is cast; its printed primary action is then performed by the caster with the spell's own printed values. Removed spells stay removed across rounds, levels and his Defeat until Prepare Spells (or Find Familiar removes more).
- **"Other spell cards removed"** (Sunburst, Dimension Door, Midas' Touch) counts the faceup pile excluding the spell being cast; **Find Familiar's "other"** excludes itself. Midas' Touch rounds down.
- **A spell's Active effect** (Shield, Invulnerability) has the spell card as its source and ends when that spell returns to the book (GY-05 designer), as well as at round end.
- **Hidden information is shallow:** every spell is either in the book or faceup, so the book's contents follow from public facts. Other seats see tier backs and counts; offering a non-Gydion chooser exactly the in-book names leaks nothing (Arty: NebKher chooses "by name, without looking").
- **Defeat and coins:** Dominate Person and Power Word Kill are ordinary p.13 Defeats by Gydion (coins, assists, Lives by Tier). Disintegrate is p.19 Remove (no coins). Polymorph is p.19 Swap (heavy minion immune). Banishment is p.18 Place (empty space; heavy minion immune).
- **The Familiar** is one token (GY-Q5): an obstacle, removed at round end (p.19), not removed on his Defeat; Find Familiar with it on the board moves it; its spell-removal clause works even when the token cannot be placed.
- **Wish** is a Skill that casts another spell; the three-Wish team victory is a new ending beside p.8's (GY-Q10).

## Capability mapping

Probed 2026-10-01 against main `da35b72d` (snapshot schema v50, `AuthoritativeSnapshotSerializer.cs:413`); re-read at `44129353`, where 9.29 has moved the wire to `/30` (`PrivateSessionProtocol.Version`), so Gydion's first wire move takes `/31`. Each block re-probes before claiming reuse.

**Reused as they stand:**
- **Performing another card's printed primary:** the borrow path (`BorrowedPrimaryAction.cs`, `borrow-primary-action@1` in `SyntheticBorrowedActionCapabilities.cs`, started by `CardActionResolutionAuthority.TakeOwedBorrowedAction`). A borrowed Attack takes values, Range, target policy and text from the borrowed definition. Items are the caster's, applied to the spell's printed icons (`AttackValueQuery.cs:49-57`). Defense and immunity follow the ordinary path. "Played an Attack card" reads the turn-slot card (GY-Q3: false for a cast). Basic/non-basic is the spell definition's own classification. The borrowed card must be a `CardInstance` in the match, so **each spell is an instance owned by Gydion**.
- **"Choose and cast one":** `EffectNodeKind.Choose` with `EffectOptionFeasibilityQuery` (8.35z35 two-tier rule, W7). A spell not in the book is Blocked (greyed); a castable spell that would do nothing stays selectable unless the whole card is empty.
- **Source-card cancellation:** `ActiveEffectCancellationCondition.SourceCardStateChanged` / `ActiveEffectLifecycle.CancelForSourceCardStateChange`. A cast's Active effect is stamped with the spell's instance (`ActiveEffectCreationHandler.cs:138`), so returning that spell cancels it (GY-05 designer).
- **Respawn a friendly minion in range in the battle zone:** Dodger's `synthetic-respawn-friendly-minion-in-radius` family (`SyntheticPlacementCapabilities.cs:352/364`), on the Range channel.
- **At maximum range:** `atMaximumReach` on the shared targeting family (`SyntheticTargetingCapabilities.Composition.cs:52`), as Ignatia.
- **Place a unit or token adjacent to you into a space in radius:** `synthetic-place@2` with `PlaceEligiblePieceKind.UnitOrToken` (Wasp's Telekinesis is the inverse).
- **Swap an enemy hero with a token or an enemy minion:** `synthetic-swap@2` with `choose-swap-subject@1`.
- **Defeat an enemy hero with no cards in hand:** `choose-target@2` `requiresEmptyHand` (Rowenna) and `synthetic-defeat-hero`.
- **Defeat an enemy minion adjacent to the target:** `synthetic-defeat-minion` with `adjacentToSpace`.
- **Retrieve, gain coins:** `retrieve-card@1`, `gain-resource@1`.
- **Single-supply token kept on the board and moved by a new Place:** Widget's Pyro (`WidgetComponentContent.cs:14-36`, `SyntheticOwnedTokenCapabilities`); the Familiar differs only in leaving at round end.

**New contracts,** each owned by one block:

| Need | Faces | Block |
| --- | --- | --- |
| The spellbook: `CardLocation` members for "in the book" (facedown) and "removed" (faceup, a public zone in `PublicCardZoneQuery`), lifecycle transitions both ways, setup that creates the twenty-two spell instances in the book (`CardInitialSetupValidator.cs:66` widened), a public book count (as `SeatHandSummary`), spell definitions with their own classification and tier, snapshot v51, wire `/31` | every face | 9.30b |
| Casting: a capability naming spells by definition (no card-definition `EffectValueKind` exists, `EffectBinding.cs:18-42`) that moves the spell book -> removed and then performs its primary through the borrow path (`BorrowedActionQuery.Requested` learns the new capability). The borrow's owner filter (`BorrowedPrimaryAction.cs:481`) stays for now, keyed so a later non-owner caster (NebKher) can be added; Hanu's delegated chooser is offered the in-book names. Prepare Spells: return every removed spell, cancelling each one's Active effects | Cantrip, Prepare Spells, every coloured face | 9.30c |
| A count of other spells removed (`RosterValueQueryKind` member) feeding Attack, Range, Radius and coins; Place yourself at exactly the Radius (`EmptySpacesAtExactlyRadius` on the Radius channel); the Familiar token and Find Familiar's "remove up to three other spells" | Sunburst, Dimension Door, Midas' Touch, Find Familiar | 9.30e |
| Immunity by attack classification (basic / non-basic) for this round (`RuleModification.ForImmunity` has no classification qualifier today); a forced discard filtered by classification (`CardCharacteristicQuery` already reads it); a Life counter regained (`TeamLifeState` has `TrySpend` only); an enemy hero's exact-3 straight-line move steered by that hero's seat (GY-Q6) | Shield, Invulnerability, Cloud Kill, Energy Drain, Suggestion | 9.30f |
| The Ultimate's replacement of a refill by Wish; Wish's "cast any spell in the book"; a persisted per-team Wish count and a team-winning claim with a new `MatchVictoryCause` (today's claim, `ISoloVictoryClaim`, wins alone) | The Archwizard, Wish | 9.30g |
| Composing the reuse-only spells on the cast path | Magic Missile, Shocking Grasp, Expeditious Retreat, Burning Hands, Fireball, Vampiric Touch, Create Undead, Dominate Person, Power Word Kill, Disintegrate, Polymorph, Banishment | 9.30d |

**Expected schema and wire moves:** 9.30b moves the snapshot to v51 (new card locations, spell instances) and the wire to `/31` (the book count, the removed zone). 9.30g adds the Wish count (snapshot, wire). Others move neither unless a new pending choice must cross the wire (9.30f's foreign-steered move is expected to reuse Whisper's).

**Built by 9.30b** (engine only, proven on synthetic spells): `CardLocation` members `Spellbook` (facedown) and `RemovedFromSpellbook` (faceup, open in `PublicCardZoneQuery`); `CardLifecycleAuthority.TryRemoveFromSpellbookFaceup` and `TryReturnToSpellbook`, each refusing every other source; `CardInstance.CreateInSpellbook` with `ConfiguredHand.SpellbookInstances`, the setup validator admitting exactly a spell in the book. A spell definition states `CardProgressionProfile.SpellCard(colour, tier)`: `IsSpell`, basic with no Tier (Gold or Silver) or non-basic with a Tier (Tier IV and Purple admitted), no Item icon, no next-Tier candidates, no Initiative; no card may name a spell as an upgrade. Every seat reads `SeatHandSummary.SpellbookCount` on `IMatchReadModel.HandSummaries`; the removed spells reach every seat as open-zone cards. Snapshot v51, wire `/31`.

**Built by 9.30c** (engine only, proven on synthetic spells; registered with the dealing at 9.30i): `SyntheticSpellbookCapabilities`. A spell is named by a per-spell naming step, `SpellInBookHandler(reference, spellDefinitionId)` (the definition bound at registration, as a token definition is), which publishes the book owner's one instance while it lies in the book and is impossible otherwise. `cast-spell@1` checks the named spell through `BorrowedPrimaryActionQuery.EligibleCards` with the pool `OwnedSpellbookSpells` and the caster as the borrower (the owner filter a non-owner caster such as NebKher would change), moves it book -> removed as its own transition, and publishes it as the borrowed card; `BorrowedActionQuery.Requested` learns `cast-spell@1`, so the caster performs the spell's printed primary through the borrow path (GY-Q3 proven: Skill played, Attack defended at p.14 with the spell named in the Defense window, Movement a Movement action, "played an Attack card" false, the spell's own classification, the caster's items). `CastArm(arm, spellInBook)` composes one bullet (a mandatory Sequence of the naming step, owner = actor, and the cast): a bullet whose spell is removed is greyed; a castable spell whose primary cannot act is answered as declined, so it stays selectable beside one that can and a card it is the only hope of is greyed (W7). `refill-spellbook@1` returns every spell the actor has removed and cancels each returned spell's Active effects (`CancelForSourceCardStateChange`); a cast's Active effect is stamped with the spell's instance. Spells are named, never offered as cards: `borrow-primary-action@1` answers the new pool with nothing, so no facedown identity reaches a chooser, Hanu's delegated seat included. Bullet labels derive from each spell's printed name. Audit: `GuessableColors` and sandbox hand staging no longer count spellbook cards. No snapshot or wire move.

## Settled questions

All settled by the user on 2026-10-01 as recommended ("as recommended"):
- **GY-Q1:** all twenty-two spells, Wish included, are in the book at setup.
- **GY-Q2:** Wish is removed when cast, like every spell, so the next refill must be an ordinary one (settled 2026-10-01, no longer provisional).
- **GY-Q3:** the played card is a Skill action; the spell's action is then performed as its own printed type with his items (an Attack opens Defense; Expeditious Retreat is a Movement action). Spell Break-style restrictions read the played card; immunities read the spell's action type; "played an Attack card" reads the played card (a Skill).
- **GY-Q4:** his seat sees the book; every seat sees tier backs, counts and the faceup pile. The spell choice is in the decision window as spell faces; the drawer and plaque per the approved mockups.
- **GY-Q5:** one Familiar; placing it again moves it; an ordinary obstacle token.
- **GY-Q6:** Suggestion follows Whisper's Rulings 16/16a: Gydion picks the hero, that hero's seat picks the direction, exactly 3 spaces or not at all, a plain move (not a Movement action - the card does not say "movement action").

Settled by the user on 2026-10-01, one by one:

- **GY-Q7 (the user, against the recommendation):** Prepare Spells is always playable, even with nothing removed. With the Ultimate unlocked a full book still counts as "would add cards", so he may cast Wish instead. An empty refill changes nothing, so 9.30i's offer-against-outcome check carries an allow-list row quoting this ruling.
- **GY-Q8:** Dominate Person offers only an enemy hero with an enemy minion in radius adjacent to it (6.8l: a selection offers what its step can act on).
- **GY-Q9:** Wish casts any spell still in the book, of any tier, including spells none of his cards list.
- **GY-Q10:** the Wish count rises when Wish is cast; his team wins once the third Wish's whole action, its inner spell included, has resolved; an ending the inner spell already caused stands. The count is public and survives rounds and his Defeat.
- **GY-Q11:** Energy Drain regains a Life only when his team has a spent one (never above its starting Lives), whether or not the enemy hero discards.
- **GY-Q12 (raised at 9.30c):** a cast is playable whenever its spell is in the book, even when the spell's action then does nothing: removing the spell faceup is the cast's own effect and changes what Sunburst, Dimension Door and Midas' Touch count. A card whose every listed spell is removed is greyed.
- **9.30a presentation:** the strip's Ultimate card stays 17x24, beside the coin on a mirrored rail.

## Presentation (approved by the user on 2026-10-01 as mockups)

The approved mockups are preserved at `tmp/9-30-gydion-approved-mockups/APPROVE-A..D-*.png`:
- **A:** the spell choice in the decision window: the played card on the left, the listed spells as spell faces, a spell not in the book greyed and captioned "REMOVED", a scaling spell captioned with its current values.
- **B:** the SPELLBOOK drawer on his seat: "IN THE BOOK - n" (faces) and "REMOVED, FACEUP - n", CLOSE.
- **C:** the drawer on every other seat: tier backs stacked with counts, the faceup pile; the Familiar on the board as an outlined hex token.
- **D:** the plaque's items strip: **every hero's unlocked Ultimate is a small purple card in the items strip** (the separate ULTIMATE row is removed for all heroes); Gydion's "SPELLBOOK 17 | 5" chip (in the book | removed) lives there too and opens the drawer; after Tier IV his Wish count reads "1/3" beside the purple card.
A built screen that matches its mockup lands and its in-game captures wait for the user's review; one that must differ waits for the user.
