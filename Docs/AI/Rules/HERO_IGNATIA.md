# Guards Digital - Ignatia Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md).

## Ignatia content ledger

Ignatia is the twenty-second playable hero. [The implementation series](../Roadmap/PHASE_8_TWENTY_SECOND_HERO.md) owns acceptance. This shard records her nineteen printed definitions, the general rules they lean on, the four printed mechanisms none of the twenty-one heroes before her carried, the capability map and the open readings. Her inventory, coin seam and seventeen composed bodies landed together at 8.75; the Ultimate (8.75a), the dealing (8.75b) and the gate (8.75c) update the relevant rows here as they land.

## Source authority

The curated `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Ignatia/` contains nineteen fronts and nineteen backs, stored internally under the Tabletopia name `ignatia` (`ignatia`, `ignatia2` .. `ignatia19`). Every front, the selection card and the dashboard were visually inspected on 2026-09-22 (the full table was read off the 372x520 fronts; the item icons and the stat bars were montaged and read against the dashboard). The dashboard names **Ignatia the Mad**; the selection card names **Ignatia**, shows **four stars** (p.21 "Wizard") and the role icons **Sniper, Damager, Tokens, Tactician, Pusher**.

IG-01: text extraction of all twenty-four pages of the local second-printing rulebook finds no Ignatia, Mad or card-name mention. The extracted cards (verified on 2026-09-18 as the printed second edition wherever physical cards could be checked - see the parent ledger's "Edition of the extraction") are the local authority for individual text. General p.7 (the Tie Breaker coin: ties in Initiative are broken by the coin's revealed side and the coin is flipped after the tied card resolves), p.8 (end of round: "Remove all tokens from the board"), p.10 (card states; "You make all the choices in your card's text"; a card's text is applied when its action is performed; p.10's bar on repeating a repeat), p.13-14 (the Attack sequence; step 1 chooses the target), p.16 (the Ultimate is a permanent ability while she is on the board), p.17 ("Friendly" never includes you; "This round"), p.18 (Defeat pays a coin, Remove does not; tokens are limited - "When a card tells you to place a token, take it from the supply first. If you need to place more tokens than are available in the supply, first remove up to that number of tokens of the same kind from the board"; Immune; Obstacles; Push; Straight Line), p.19 (Swap is neither movement nor placement; Target; "Tokens ... All tokens are obstacles"; "Once a token is placed, it retains all its properties until that token is removed") are the rules her faces lean on.

IG-02: all nineteen backs share one MD5 (`05695bbc64454fd47ca4457f954ff729`); this set has ONE deck back. The selection front is byte-identical to `cards/hero-selection/randomizers-tt12__7729913__front.png` (MD5 `141ab67098edc242e36549b7bdc36840`). Her Tabletopia card group is `Ignatia_The_Mad` (elements 7635279, 7635273, 7641660-7641666, 7641669, 7694856, 7590071, 7729004, 7729007-7729011, 7729013).

IG-03: every corner letter matches its ribbon (D, S, R x5, B x5, G x5, P, D-H); there is no printed colour conflict in this set.

IG-04: stat bars read Attack **5**, Initiative **2**, Defense **6**, Movement **5** plus **1** brighter pip (p.21: her Movement item). The dashboard's bottom bars agree.

IG-05: Unreliable Jolt, the Handicap, prints a Gold band over an Attack-Ranged 1 with Range 2 and two hero-only texts. It is the boxed p.20 Handicap and is never dealt, so nothing runs on it; it is composed like every other face so a future Handicap configuration finds it whole.

IG-06: absent icons. Equilibrium prints Defense 7 and no Movement, no Range and no Radius. The seven attacks print Range with the Ranged band and no Radius; the ten Skills print Radius and no Range; Chaos Incarnate prints no values at all. A missing icon is absent, never zero.

IG-07: Tabletopia manifest elements (`extracted/text-assets/manifest-bundled__42.json`): standee **`Ignatia`** element 8597496 (publicElementId 714734, VerticalTokens, `Image_1_Atlas` sprites (2463,3501) and (2159,3501), 302x302 - the dealing block verifies both by crop against her selection portrait, Hanu's HA-07 lesson); token element **`Magma`** (`Tokens`) is her magma token's art. No other element is hers.

IG-08: every action card prints TWO texts under one heading - a ☀ (sun) line and a ☾ (moon) line - and the printed reminder on the Gold basic states the rule for all of them: "( Apply the text matching the symbol on the Tie Breaker coin )". The ☀ and ☾ are the two physical faces of the Tie Breaker coin, which this engine models only as the side it shows (`TieBreakerState.RevealedSide`): USER-CORRECTED FROM LIVE PLAY 2026-09-26 - ☀ is the BLUE face, ☾ the RED face; the earlier 2026-09-22 colour reading was inverted.

## Printed inventory

All file stems below are under the Ignatia source directory and end in `__front.png`. D/M are Defense/Movement ribbon icons; A is Attack, R Range, r Radius. A missing icon is absent, never zero. Item icons are independent of card values.

| File stem | Face / color / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| ignatia__7635279 | Chaos Bolt / Gold / Basic | 11 | 2 | 1 | Basic Attack - Ranged / A 2, R 3 | - |
| ignatia2__7694856 | Equilibrium / Silver / Basic | 1 | 7 | - | Basic Skill | - |
| ignatia3__7729010 | Playing with Fire / Red / I | 7 | 4 | 3 | Attack - Ranged / A 3, R 2 | - |
| ignatia4__7729009 | Erratic Fireblast / Red / II | 7 | 5 | 3 | Attack - Ranged / A 3, R 3 | Defense |
| ignatia5__7729008 | Crack of Doom / Red / II | 7 | 6 | 3 | Attack - Ranged / A 5, R 5 | Initiative |
| ignatia6__7729007 | Loosely-Aimed Firebolts / Red / III | 8 | 5 | 3 | Attack - Ranged / A 4, R 3 | Radius |
| ignatia7__7729004 | Imminent Eruption / Red / III | 8 | 6 | 3 | Attack - Ranged / A 6, R 5 | Initiative |
| ignatia8__7641669 | Abrupt Combustion / Blue / I | 9 | 3 | 3 | Skill / r 3 | - |
| ignatia9__7729011 | Spontaneous Immolation / Blue / II | 9 | 4 | 3 | Skill / r 4 | Attack |
| ignatia10__7729013 | Searing Heat / Blue / II | 9 | 4 | 3 | Skill / r 3 | Initiative |
| ignatia11__7641666 | Violent Conflagration / Blue / III | 10 | 4 | 3 | Skill / r 4 | Range |
| ignatia12__7641665 | Scorching Blaze / Blue / III | 10 | 4 | 3 | Skill / r 3 | Movement |
| ignatia13__7641664 | Path of Ashes / Green / I | 5 | 2 | 2 | Skill / r 3 | - |
| ignatia14__7641663 | Path of Cinders / Green / II | 5 | 2 | 2 | Skill / r 4 | Defense |
| ignatia15__7641662 | Unstable Portal / Green / II | 5 | 2 | 2 | Skill / r 4 | Attack |
| ignatia16__7641661 | Path of Flames / Green / III | 4 | 3 | 2 | Skill / r 4 | Defense |
| ignatia17__7641660 | Chaos Gate / Green / III | 4 | 3 | 2 | Skill / r 4 | Attack |
| ignatia18__7590071 | Chaos Incarnate / Purple / IV | - | - | - | Ultimate | - |
| ignatia19__7635273 | Unreliable Jolt / Gold / Handicap | 10 | 1 | 1 | Basic Attack - Ranged / A 1, R 2 | - |

### Exact printed rules

Every action face prints its ☀ line and its ☾ line; the live coin face selects one (IG-08).

- **Chaos Bolt:** ☀ Target a minion adjacent to you. ☾ Target a hero in range. ( Apply the text matching the symbol on the Tie Breaker coin ).
- **Equilibrium:** This round: Each time you perform or repeat a primary action you may apply either ☀ or ☾ card text, regardless of the Tie Breaker coin.
- **Playing with Fire / Erratic Fireblast:** ☀ Target a unit in range not in a straight line. ☾ Target a unit in range in a straight line.
- **Crack of Doom:** ☀ Target a unit adjacent to you. ☾ Target a unit at maximum range.
- **Loosely-Aimed Firebolts:** ☀ Target a unit in range not in a straight line. ☾ Target a unit in range in a straight line. May repeat once on a different hero.
- **Imminent Eruption:** ☀ Target a unit adjacent to you. May repeat once on a minion. ☾ Target a unit at maximum range.
- **Abrupt Combustion / Spontaneous Immolation:** ☀ An enemy hero in radius, adjacent to a token, or a minion, discards a card, if able. ☾ Remove an enemy minion in radius adjacent to an enemy hero.
- **Searing Heat:** ☀ Move a friendly hero in radius 2 spaces in a straight line. ☾ Move an enemy hero in radius 2 spaces in a straight line.
- **Violent Conflagration:** ☀ An enemy hero in radius, adjacent to a token, or a minion, discards a card, or is defeated. ☾ Defeat an enemy minion in radius adjacent to an enemy hero.
- **Scorching Blaze:** ☀ Move a friendly hero in radius 2 or 3 spaces in a straight line. ☾ Move an enemy hero in radius 2 or 3 spaces in a straight line.
- **Path of Ashes / Path of Cinders / Path of Flames:** ☀ Move up to 2 (3, 4) spaces in a straight line. Place a magma token in each empty space you moved through, or out of. ☾ Place up to 2 (3, 4) magma tokens in radius.
- **Unstable Portal:** ☀ Swap with a friendly unit in radius. ☾ Swap with an enemy unit in radius.
- **Chaos Gate:** ☀ Swap with a friendly unit in radius; you may move that unit 1 space. ☾ Swap with an enemy unit in radius; you may move 1 space.
- **Chaos Incarnate (Ultimate):** The first time each turn after you perform a primary action you may flip the Tie Breaker coin; if you do, you may perform that action again, choosing different targets.
- **Unreliable Jolt (Handicap):** ☀ Target a hero adjacent to you. ☾ Target a hero in range, not adjacent to you.

## Setup and progression

Starting hand: Chaos Bolt, Equilibrium, Playing with Fire, Abrupt Combustion, Path of Ashes. Deck: twelve Tier II/III faces plus Chaos Incarnate. Each colored Tier I upgrades to either face of its Tier II, then either Tier III (Red to Erratic Fireblast or Crack of Doom then Loosely-Aimed Firebolts or Imminent Eruption; Blue to Spontaneous Immolation or Searing Heat then Violent Conflagration or Scorching Blaze; Green to Path of Cinders or Unstable Portal then Path of Flames or Chaos Gate); the unchosen face becomes its printed item (Initiative x3, Defense x3, Attack x3, Radius, Range, Movement - the same distribution as Min's, Brynn's and Hanu's). Unreliable Jolt remains the established odd-player Handicap deferral (p.20). She places MAGMA tokens (three in her supply - USER-SETTLED 2026-09-22 "Magma token count is 3") and gives no marker.

## The four mechanisms her set is built on

1. **Coin-selected card text on every card** (IG-08). No dealt hero reads the Tie Breaker coin from inside an action; the coin lives in `InitiativeResolutionAuthority.TieBreakerState`, is passed by value into `CardActionResolutionAuthority`, and was flipped in exactly one place (after a tied selection resolves). Her seam: `EffectResolutionContext` / `EffectCapabilityContext` carry the revealed side, `ignatia-coin-sun@1` publishes "the live face is ☀", and every one of her action bodies is ONE `Conditional` on it - then the ☀ subtree, else the ☾ subtree - so the coin picks the text exactly where every other Conditional picks its branch and no card holds coin geometry. Her attacks bind their own target by card text (`BoundByCardText`, `CardTextTiming.BeforeTheAttack`), because the coin picks the TARGET SET (a minion adjacent vs a hero in range; a unit in vs not in a straight line; adjacent vs at maximum range) - ID-Q4.
2. **A round-long free choice of face** (Equilibrium). A round-scoped marker on the acting hero (`IgnatiaFreeCoinChoice`, an `ActiveEffectCreationHandler` over a `ThisRound` definition cancelled on her defeat); while it stands the coin predicate PAUSES and asks her which face to apply (1 = ☀, 0 = ☾) instead of reading the coin, on every primary action AND every repeat of one, exactly as printed.
3. **A coin flip that repeats the action on the opposite face** (Chaos Incarnate). Cutter's Legend of the Skies already reads "The first time each turn after you perform a primary action" (`UltimateTriggerCondition.AfterControllerPerformsPrimaryAction`, keyed once per turn by `UltimateTriggeredEffectSourceQuery`) and Tigerclaw's Cloak and Daggers already repeats the completed action from a triggered body (`RepeatAction`, `DifferentTarget`). What is new: a capability that FLIPS the coin from inside a card action and has that flip reach the authoritative round state before the repeat starts - so the repeated action's Conditional reads the opposite face and every seat's rail banner shows it - without new persisted state (8.75a).
4. **Magma tokens as obstacles** (the three Paths, read by the three discard faces). USER-SETTLED (ID-Q1): tokens are obstacles unless a card says otherwise and the cards define them 100%; p.19 says the same ("All tokens are obstacles"). The token `ignatia-magma` is impassable, is taken from a supply of three under p.18's short-supply rule, is placed on EMPTY spaces (a wake along a straight move - "each empty space you moved through, or out of" - or up to N spaces in radius), and is removed at the end of the round (p.8). It declares no passive of its own; the Blue discard line reads adjacency to it ("adjacent to a token").

Everything else composes on shipped contracts: `choose-target@2` and `synthetic-range-hero-target@1` for the attacks' arms, `set-attack-target@1` to bind the chosen unit; `synthetic-radius-hero-target@1` and `synthetic-direct-move@3` for the pushes; `SyntheticResolvedDiscardCapabilities` under `IfAble` with Electroblast's discard-or-defeat escalation for Violent Conflagration; `synthetic-swap` (`SwapV2`) and a trailing `synthetic-direct-move@3` for the swaps; Min's and Brynn's placement and obstacle-adjacency queries for the magma.

## Capability contract map

These routes identify the composed contracts. Registration at the dealing block makes each reachable from dealt content; the gate block separately owns complete configured-player coverage.

| Printed clause | Existing foundation / required delta | Owner |
| --- | --- | --- |
| Apply the text matching the symbol on the Tie Breaker coin (every action face) | NEW `ignatia-coin-sun@1` predicate reading the revealed side threaded through the resolver (`TieBreakerRevealedSide`), one `Conditional` per body - ID-Q5 | 8.75 (seam); 8.75a threads the live authorities |
| ☀ minion adjacent / ☾ hero in range (Chaos Bolt); ☀ hero adjacent / ☾ hero in range not adjacent (Unreliable Jolt) | `choose-target@2` (adjacent) and `synthetic-range-hero-target@1`; NEW `ignatia-range-hero-not-adjacent@1`; `set-attack-target@1` under `BoundByCardText` - ID-Q4 | 8.75 |
| ☀ unit in range not in a straight line / ☾ in a straight line (Playing with Fire, Erratic Fireblast, Loosely-Aimed Firebolts) | NEW `ignatia-range-unit-in-straight-line@1` / `ignatia-range-unit-not-in-straight-line@1` (one handler type); "May repeat once on a different hero" = `RepeatTargetBinding.DifferentEnemyHero` | 8.75 |
| ☀ unit adjacent / ☾ unit at maximum range (Crack of Doom, Imminent Eruption) | `choose-target@2`; NEW `ignatia-range-unit-at-maximum-range@1` (exactly the printed Range with items); "May repeat once on a minion" = `DifferentEnemyMinion` | 8.75 |
| ☀ an enemy hero in radius adjacent to a token or a minion discards a card, if able / ☾ remove an enemy minion in radius adjacent to an enemy hero (Abrupt Combustion, Spontaneous Immolation) | NEW `ignatia-enemy-hero-adjacent-to-magma-or-minion@1` and `ignatia-enemy-minion-adjacent-to-enemy-hero@1` selections; `SyntheticResolvedDiscardCapabilities` under `IfAble`; p.18 Remove | 8.75 |
| ... discards a card, or is defeated / ☾ defeat an enemy minion ... (Violent Conflagration) | the same selections; Electroblast's escalation (discard, else `defeat-hero`); p.18 Defeat | 8.75 |
| ☀ move a friendly hero / ☾ move an enemy hero in radius 2 (2 or 3) spaces in a straight line (Searing Heat, Scorching Blaze) | `synthetic-radius-hero-target@1` (Allied / Opposing) into `synthetic-direct-move@3` at the printed distances, straight line, obstacles honoured | 8.75 |
| ☀ move up to N straight placing a magma token in each empty space moved through or out of / ☾ place up to N magma tokens in radius (the three Paths) | NEW token `ignatia-magma` (impassable obstacle, supply 3, end-of-round removal); NEW `ignatia-place-magma-wake@1` (a new handler type); `ignatia-place-magma-in-radius@1` over the shared PlaceHandler - ID-Q1 | 8.75 |
| ☀ swap with a friendly unit / ☾ with an enemy unit in radius [; you may move that unit 1 / you may move 1] (Unstable Portal, Chaos Gate) | `SwapV2` over the radius unit selection; the trailing optional `synthetic-direct-move@3` (UpTo 1) on the named mover | 8.75 |
| This round: each time you perform or repeat a primary action you may apply either ☀ or ☾ (Equilibrium) | NEW `ignatia-equilibrium@1` (an `ActiveEffectCreationHandler` over the `ThisRound` marker `IgnatiaFreeCoinChoice`); the coin predicate pausing with Integer candidates 1/0 while the marker stands - ID-Q2 names the labels | 8.75; the labels 8.75b |
| The first time each turn after you perform a primary action you may flip the Tie Breaker coin; if you do, you may perform that action again, choosing different targets (Chaos Incarnate) | `UltimateRuleDefinition.TriggeredAbility` with `AfterControllerPerformsPrimaryAction` (Cutter's once-per-turn key); NEW `ignatia-flip-tie-breaker@1` applying the flip to the authoritative coin before the repeat; `RepeatAction` (May, once, `DifferentTarget`) - ID-Q3, ID-Q6a, ID-Q7a | 8.75a |
| Printed set, upgrades, items, registration and artwork | `IgnatiaCardContent` composes nineteen definitions, five starting cards, thirteen deck cards and the boxed Handicap; only the Handicap is deferred; the roster row carries FOUR stars and Sniper/Damager/Tokens/Tactician/Pusher for the 8.35z86 picker filters; her cards show which line is live (ID-Q2) | 8.75b; gate 8.75c |

## Integration identity and private artwork

Recorded at 8.75b: schema v44 and wire `/20` unmoved through the whole series; content revision `ignatia-8-75-rev-1`, identity pin #22 `166986e315339edf015beb4eb6286ada58420819ee8d89df97f02a7bc86d5300`; the pick card (SHA-256 `1d4c0b4b694f19a79101ea19e7f28c2b7db6e02bd935bdcff3363f54c699fc57`) as `Assets/Resources/Private/HeroSelection/hero-ignatia.png`; the standee pair `Heroes/ignatia-red.png` / `ignatia-blue.png` from element 8597496; eighteen prepared fronts and 84 shine slots under `Cards/ignatia/`; the magma token face `Tokens/ignatia-magma.png` from element `Magma` 1401024. Eleven registered ids over six new handler types (`CoinSunHandler`, `FlipTieBreakerHandler`, `IgnatiaRangeAttackTargetHandler`, the two discard narrowings, `PlaceMagmaWakeHandler`), handler types 97 -> 103, the magma token in the component catalog; the roster row `HeroComplexity.Wizard` (the first four-star hero), `HeroRole.Sniper | Damager | Tokens | Tactician | Pusher` (the first five-role hero), display name "Ignatia the Mad", proven on the rendered lobby; her cards show the live face (`gd-card-live-text-face`). Gated at 8.75c: Play Mode 825 with zero skips.

## Open verification points

- **ID-Q1 (user, settled 2026-09-22 "I think tokens are obstacles so unless specified untis cannot enter them. And cards are king there, they define the tokens 100%."):** magma tokens are obstacles (p.19 agrees); no unit enters a space holding one unless a card says otherwise; nothing about them is invented beyond what the three Paths and the three discard faces print. They are limited to her supply of three (p.18) and leave the board at the end of the round (p.8).
- **ID-Q2 (user, settled 2026-09-22 "Ok"; presentation corrected at 8.103 on "dont say 'sun' 'moon' ... put icons from the card"):** the Tie Breaker coin is already visible on the HUD at all times, so no new coin-face surface is needed; her cards render WHICH of their two lines is currently live, and under Equilibrium the choice uses the printed `☀` / `☾` marks rather than bare integers or the translated face names.
- **ID-Q3 (user, settled 2026-09-22 "Q3 is correct"):** Chaos Incarnate repeats the WHOLE primary action once - the flipped coin selects the opposite text, so the repeat's target set and effect are that text's - on the first primary action of each of her turns only.
- **ID-Q4 (user, settled 2026-09-22):** the Gold basic's legal target set flips with the coin (a minion adjacent under ☀, a hero in range under ☾), exactly like every other attack of hers; the offer greys when the live text names nobody.
- **ID-Q5 (user, corrected 2026-09-26 after live play):** ☀ is the coin's Blue face, ☾ its Red face; the earlier colour reading was inverted. One constant carries the mapping.
- **ID-Q6a / ID-Q7a:** settled by 8.75a - what "choosing different targets" excludes on a Skill repeat, and whether the Ultimate's repeat stands after a card-text repeat of the same action under p.10's bar.
- **ID-Q8c (settled at 8.75):** "adjacent to a token" on the Blue discard line means ANY token - the printed noun carries no qualifier - while the capability id keeps its "magma" spelling as an identity.
- **ID-Q9c (settled at 8.75c):** that clause's token half is unreachable through configured play (only her magma exists, only her Green line places it, one primary action per turn, p.8 removes tokens at the round's end), so the gate drives the minion half and the token half is proved by `IgnatiaDiscardTests`.
- **ID-Q10c (recorded at 8.75c, owed to 8.75d):** Loosely-Aimed Firebolts' and Imminent Eruption's printed repeat clauses narrow the repeated instance in p.13's own target question, but a card-text-bound attack re-runs its printed subject whole, so the repeat's OFFER still admits the hero the first instance attacked; pressing him stops the Attack without a target. Both journeys assert the gap by name.
