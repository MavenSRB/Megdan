# Guards Digital - Snorri Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The twenty-fourth-hero sequence](../Roadmap/PHASE_8_TWENTY_FOURTH_HERO.md) owns acceptance and status.

## Source and rule authority

Snorri the Runescribe is the selected twenty-fourth hero. The curated Tabletopia source `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Snorri/` contains nineteen distinct fronts and nineteen backs. Two byte-distinct back groups (13 and 6) show the same axe artwork; neither variation states a rule. The selection profile shows **four stars** and **Sniper, Farming, Durable, Damager, Pusher, Healer**. The dashboard identifies Snorri and four numbered turn slots. The source manifest names four separate pieces, `Rune1` (6548804), `Rune2` (6548808), `Rune3` (6548807) and `Rune4` (6548805), under its generic `Tokens` element type. The **Silver card itself calls them rune markers**. That printed term, not the manifest storage label, governs their rules kind. No rune occupies a board space or becomes an obstacle merely because the source platform labels a piece a token.

The Silver `Inscribe the Runes` says: "Give yourself 4 rune markers and place them below each one of your turn slots. A rune is active as long as it is below the current turn slot. Rune markers are not removed at the end of round, nor if you are defeated." This exact printed exception outranks the rulebook's ordinary p.8 end-of-round and p.13 defeat marker returns under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy). It is not a turn-limited Active effect, board token, or implicit card-zone effect. The rune-to-slot assignment and its visual representation must survive both checkpoints.

The rulebook's printed pp.6-8, 13-14, 17-20 govern setup, four turn slots, commitment, Attack/Defense, Active effects, markers, swaps and the boxed Handicap. The printed card fronts control each Snorri clause. The inventory below paraphrases only for routing; each front owns its exact punctuation and rune glyph order.

The user settled the Silver's unstated placement order: Snorri's player chooses which of the four distinct runes goes below each slot. The existing choice pop-up must present that as drag-and-drop assignment, with all four slots filled before commitment. The user additionally requires a hero-tray view: each rune appears in Snorri's turn-card slot; a resolved card occupies the same slot with the rune still visible; the resolving card displays that rune above its Initiative number, including the slot's pre-commit state. This is a presentation requirement, not a rule alteration, and [8.93a/f/g](../Roadmap/PHASE_8_TWENTY_FOURTH_HERO.md) owns its implementation and player-path proof.

## Printed inventory

File stems below end in `__front.png` under the Snorri source directory. D/M are the ribbon Defense/Movement icons; A/R/r are the action's Attack/Range/Radius icons. Missing ribbon or action icons are absent, not zero. Items are the independent bottom glyphs. The four different rune glyphs are referred to here by their visible order on the Gold face as **G1-G4** until the physical art-to-identity mapping is confirmed; these are inventory labels, not gameplay names.

| Stem | Face / color / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | ---: | ---: | ---: | --- | --- |
| snorri__7387414 | Rune Sigils / Gold / Basic | 11 | 2 | 1 | Basic Attack - Ranged / A 2+, R 2 | - |
| snorri2__7387420 | Inscribe the Runes / Silver / Basic | 1 | 3 | - | Basic Skill | - |
| snorri3__7728049 | Runic Dagger / Red / I | 7 | 6 | 3 | Attack / A 5 | - |
| snorri4__7728050 | Runic Hammer / Red / II | 7 | 7 | 3 | Attack / A 5 | Initiative |
| snorri5__7728051 | Runecaster / Red / II | 7 | 6 | 3 | Attack - Ranged / A 4, R 3 | Defense |
| snorri6__7728053 | Runic Battleaxe / Red / III | 8 | 7 | 3 | Attack / A 6 | Radius |
| snorri7__7728052 | Runeblaster / Red / III | 8 | 6 | 3 | Attack - Ranged / A 5, R 3 | Movement |
| snorri8__7387431 | Safe Passage / Blue / I | 9 | 5 | - | Movement 3 | - |
| snorri9__7387415 | Hidden Passage / Blue / II | 9 | 6 | 3 | Movement 3 | Attack |
| snorri10__7505429 | Ancestral Boon / Blue / II | 9 | 6 | 3 | Skill / r 4 | Defense |
| snorri11__7387424 | Deep Passage / Blue / III | 10 | 6 | - | Movement 3 | Range |
| snorri12__7505428 | Ancestral Grace / Blue / III | 10 | 6 | 3 | Skill / r 4 | Defense |
| snorri13__7728043 | Oath of Endurance / Green / I | 5 | 2 | 2 | Defense / Block | - |
| snorri14__7728044 | Oath of Fortitude / Green / II | 5 | 2 | 2 | Defense / Block | Initiative |
| snorri15__7728045 | Charged Runestone / Green / II | 5 | 3 | 2 | Skill / r 3 | Attack |
| snorri16__7728046 | Oath of Perseverance / Green / III | 4 | 2 | 2 | Defense / Block | Initiative |
| snorri17__7728048 | Inscribed Runebomb / Green / III | 4 | 4 | 2 | Skill / r 3 | Attack |
| snorri18__7730144 | Rune Mastery / Purple / IV | - | - | - | Ultimate | - |
| snorri19__7387418 | Rune Shards / Gold / Handicap | 10 | 1 | 1 | Basic Skill - Ranged / A 1+, R 2 | - |

The eighteen ordinary cards consist of two Basics, three Tier I, six Tier II, six Tier III and one Tier IV Ultimate. `Rune Shards` is the nineteenth p.20 Handicap and is boxed at setup. The twelve item faces give Initiative x3, Defense x3, Attack x3, and Radius, Range and Movement x1 each; no item is an additional action.

### Printed clauses by effect family

- **Rune Sigils:** target an adjacent unit. If a rune is active, G1 permits a minion in Range instead; G2 adds +3 Attack; G3 grants 3 coins if a hero is targeted; G4 repeats once on a different hero in Range.
- **Inscribe the Runes:** give Snorri four rune markers, place one below each turn slot, and retain them through round end and Snorri's defeat; only the current slot's rune is active.
- **Runic Dagger / Runic Hammer / Runic Battleaxe:** adjacent-unit Attacks. Dagger retrieves a discarded card after the Attack if its printed rune is active. Hammer adds an optional pre-Attack Move 1 before the same retrieval. Battleaxe adds the same pre-Attack Move, an optional once-only repeat on an enemy minion after the Attack, and optional discarded-card retrieval, each gated by its distinct printed rune.
- **Runecaster / Runeblaster:** Ranged Attacks. Runecaster targets a unit at maximum Range; Runeblaster targets any unit in Range under its rune, otherwise only at maximum Range. After either Attack, one rune permits Move up to 2 and another makes an enemy hero adjacent to the target's space discard a card or be defeated.
- **Safe / Hidden / Deep Passage:** Movement 3. The printed runes grant, respectively, this-turn obstacle ignoring; Hidden and Deep also this-turn immunity to enemy actions; Deep adds +2 Movement under a third rune.
- **Ancestral Boon / Ancestral Grace:** a friendly hero in Radius may, under distinct runes, swap a resolved card with a card in hand or retrieve all their discarded cards. Grace additionally may swap an item with another card of the same tier and color under its printed rune.
- **Oath of Endurance / Fortitude / Perseverance:** Defense/Block. A printed rune can Block a basic, non-ranged, ranged or non-basic Attack as individually listed on each face; all three print this-turn immunity to enemy actions. Do not turn a Block into a numeric Defense bonus or let an inapplicable category Block.
- **Charged Runestone / Inscribed Runebomb:** target an enemy hero in Radius. The active-rune choice makes that hero discard a card of the printed green/silver/blue or green/silver/blue/gold color, if able. Runebomb explicitly says choose one active rune.
- **Rune Mastery:** each time Snorri performs an action, choose one inactive rune; it counts as active until Snorri's next action.
- **Rune Shards (boxed Handicap):** target an adjacent hero; an active rune adds +3 Attack, grants 2 coins if targeting a hero, or repeats once on a different hero in Range according to its printed glyph.

## Capability mapping and bounded questions

Existing round state has a stable 1-4 `TurnIndex` and `RoundTurnSlot` card assignments. Existing `MarkerAttachmentRegister` owns finite marker supply and snapshot serialization, but `MarkerAttachment` has no slot coordinate and `MarkerReturnCondition` currently admits only ordinary general cleanup; its definition validator rejects the `None` value. Snorri's verified Silver requires a retained return condition and a deterministic marker-to-turn-slot association, not a fake board token or a hero-name branch in cleanup. `EffectRosterState` exposes the round's turn-slot list, but a live slot-index query must not mistake historical assignments for the current rune selection. This is 8.93a's foundation.

Existing typed combat includes Attack/Defense, category-specific Blocks, movement, immunity, discard/retrieval and bounded repeats; the Red and Green milestones must probe where their rune predicate can compose as a typed condition. Existing card-state vocabulary includes resolved-card selection and swap operations, but Ancestral Boon/Grace's exact self/friendly ownership, tier/color item restriction and pending-choice recheck must be verified in 8.93c, not assumed. Rune Mastery's temporary activation ends at the *next action*, not automatically at turn or round end; 8.93e owns that timing and its interaction with the permanent slot marker.

The match HUD has a two-state decision window (`DecisionWindowModels` / `DecisionWindowUnityView`) and separate card-commitment drag state, but no existing four-piece drag-to-slot control was found. 8.93a must add a typed four-rune assignment command and drag/drop interaction to the decision window, with legality and atomicity enforced by the authoritative command rather than UI coordinates. 8.93f must bind the projected assignment into the hero tray and resolving-card Initiative display. This is a reusable typed interaction shape, not a Snorri-name special case in the UI.

**SN-Q1 settled by the user (8.93a):** The printed Silver requires four distinct physical runes below four turn slots but does not state an order. The user confirmed **the player chooses the order**, using drag-and-drop in the existing choice pop-up. Manifest names never prescribe the slot assignment.

**SN-Q2 (8.93f):** The curated manifest identifies four preview/atlas resources, but the corresponding physical-piece raster is not present among the curated standalone PNGs checked at inventory time. Before UI art import, inspect an authoritative piece preview if available or derive legible, faithful glyph artwork from the locally available card fronts; verify that the four card glyphs and four tray/Initiative glyphs are identical by inspection. This is an art-source closing check, not a reason to substitute generic numbers silently.
