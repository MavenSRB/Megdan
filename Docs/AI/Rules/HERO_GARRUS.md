# Guards Digital - Garrus Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md).

## Garrus content ledger

Garrus the Gladiator is the selected sixteenth hero. [The implementation series](../Roadmap/PHASE_8_SIXTEENTH_HERO.md) owns execution and acceptance. This shard owns source facts, inventory and capability mapping; an inventory is not an implemented-content claim.

## Source authority

The local curated `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Garrus/` contains nineteen fronts and nineteen backs. Every front, the named profile and the dashboard were inspected for this inventory. The dashboard names **Garrus the Gladiator**; the profile shows **two stars**, Disabler, Durable and Tactician. No hero-owned token or marker is named by the printed set.

GA-01: extraction of every page of the local second-printing rulebook finds no Garrus, Gladiator, Angry Strike, Chilling Howl or Battle Fury mention. These extracted third-printing playtest cards remain the sole local authority for their individual text; the standing source hierarchy and edition-skew qualification in the parent ledger apply. General p.9 icon/item rules, p.10 card states/choices/Repeat, p.11 Movement, p.13 Defeat, p.14 Attack, p.16 Ultimate and p.17 Active effects still govern.

GA-02: the nineteen backs form two SHA-256 groups: nine `2E01D461C34B16E0C19A3C376377A98377172B89EDBB582071F941A42AB698AA`, ten `8DB9CD48E411A49FC4FC104D14D0F80EE1B43E6847A5186E783E840E3ACE28A7`. A shared back must not be assumed.

GA-03: p.10 explicitly permits discarding cards in different states and says a discarded played card remains played. Milestone 8.41a adds this legal lifecycle path and updates ordinary retrieval/cancellation consumers coherently; the former hand-only discard remains distinct. Battle Fury observes the discard transition, not an action completion or a generic current-discard count, and its trigger remains owned by 8.41c.

GA-04: Chilling Howl says **This round**, **enemy heroes**, **in radius**, and **with a movement action**. Its live radius follows Garrus and uses the Radius icon plus items; it does not prohibit direct Move or Push. Movement restriction is evaluated at action start under the standing contract. Its source is an ordinary cancelable Active effect.

GA-05: Red modal attacks put the discard test inside their first bullet's before-attack sentence. With no discard, that bullet still targets an adjacent hero; it merely grants no move. Blunt Force and Send Flying target an adjacent unit for the Attack, then optionally move beside an enemy hero and Push **that hero**, who need not be the attack target. The destination and the hero it names must remain an explicit player choice when multiple heroes qualify.

GA-06: the Blue cards move a selected unit exactly one space, closer/farther by geometric distance; they do not Push. Repeats are whole new action instances with fresh target choices and normal after-action consequences. Heavy and other immunity apply through the standard direct-Move rules.

GA-07: the Green presence condition counts enemy heroes rather than targeting them; immunity does not remove one from the count. Light Pilum's discard is `if able`. Heavy Pilum says `discards a card, or is defeated`, which uses the same forced-discard-or-Defeat grammar as existing cards; its following optional move survives either outcome.

## Printed inventory

All files below are under the Garrus source directory and end in `__front.png`. D/M are the left-ribbon Defense/Movement icons. A is Attack; R is Range; r is Radius. A missing icon is absent, never zero. Tier II/III item icons are transcribed independently of the card's own values.

| File stem | Face / color / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| garrus__7583457 | Angry Strike / Gold / Basic | 11 | 3 | 1 | Basic Attack / A 4+ | - |
| garrus2__7502814 | Chilling Howl / Silver / Basic | 13 | 3 | - | Basic Skill / r 3 | - |
| garrus3__7502825 | Trace / Red / I | 7 | 5 | 4 | Attack / A 3 | - |
| garrus4__7502824 | Chase / Red / II | 8 | 5 | 4 | Attack / A 4 | Defense |
| garrus5__7502817 | Blunt Force / Red / II | 8 | 7 | 4 | Attack / A 7 | Initiative |
| garrus6__7502815 | Hunt Down / Red / III | 8 | 6 | 4 | Attack / A 4 | Range |
| garrus7__7502818 | Send Flying / Red / III | 8 | 8 | 4 | Attack / A 7 | Radius |
| garrus8__7502828 | Menace / Blue / I | 9 | 6 | 3 | Skill - Ranged / R 2 | - |
| garrus9__7502812 | Threaten / Blue / II | 10 | 6 | 3 | Skill - Ranged / R 2 | Defense |
| garrus10__7502826 | Form Up! / Blue / II | 10 | 6 | 3 | Skill - Ranged / R 3 | Attack |
| garrus11__7502816 | Terrify / Blue / III | 10 | 7 | 3 | Skill - Ranged / R 2 | Defense |
| garrus12__7502821 | Testudo! / Blue / III | 10 | 7 | 3 | Skill - Ranged / R 3 | Attack |
| garrus13__7502829 | Hold Ground / Green / I | 5 | 3 | 2 | Skill / r 3 | - |
| garrus14__7502819 | Make a Stand / Green / II | 4 | 4 | 2 | Skill / r 4 | Initiative |
| garrus15__7502827 | Light Pilum / Green / II | 4 | 4 | 2 | Skill - Ranged / R 2 | Attack |
| garrus16__7502813 | Battle Ready / Green / III | 4 | 4 | 2 | Skill / r 4 | Initiative |
| garrus17__7502822 | Heavy Pilum / Green / III | 4 | 4 | 2 | Skill - Ranged / R 2 | Movement |
| garrus18__7544040 | Battle Fury / Purple / IV | - | - | - | Ultimate | - |
| garrus19__7583459 | Angry Swipe / Gold / Handicap | 10 | 2 | 1 | Basic Attack / A 3+ | - |

### Exact printed rules

- **Angry Strike:** Target a unit adjacent to you; +1 Attack for every card in your discard.
- **Chilling Howl:** You may discard one of your resolved cards. **This round:** Enemy heroes in radius cannot fast travel, or move more than 2 spaces with a movement action.
- **Trace:** **Choose one -** (1) Before the attack: If you have one or more cards in your discard, move up to 1 space. Target a hero adjacent to you. (2) Target a unit adjacent to you.
- **Chase:** **Choose one -** (1) Before the attack: If you have one or more cards in your discard, move up to 2 spaces. Target a hero adjacent to you. (2) Target a unit adjacent to you.
- **Blunt Force:** Target a unit adjacent to you. After the attack: You may move up to 1 space to a space adjacent to an enemy hero; if you do, push that hero 3 spaces ignoring obstacles.
- **Hunt Down:** **Choose one -** (1) Before the attack: If you have one or more cards in your discard, move up to 3 spaces. Target a hero adjacent to you. (2) Target a unit adjacent to you.
- **Send Flying:** Target a unit adjacent to you. After the attack: You may move up to 2 spaces to a space adjacent to an enemy hero; if you do, push that hero 3 spaces ignoring obstacles.
- **Menace:** Move an enemy unit in range 1 space to a space farther away from you.
- **Threaten:** Move an enemy unit in range 1 space to a space farther away from you. May repeat once.
- **Form Up!:** Move a friendly unit in range 1 space to a space closer to you. May repeat once.
- **Terrify:** Move an enemy unit in range 1 space to a space farther away from you. May repeat up to two times.
- **Testudo!:** Move a friendly unit in range 1 space to a space closer to you. May repeat up to two times.
- **Hold Ground:** If there are at least two enemy heroes in radius, you may retrieve a discarded card.
- **Make a Stand:** If there are at least two enemy heroes in radius, you may retrieve a discarded card.
- **Light Pilum:** An enemy hero in range discards a card, if able. You may move 1 space.
- **Battle Ready:** If there are at least two enemy heroes in radius, you may retrieve up to two discarded cards.
- **Heavy Pilum:** An enemy hero in range discards a card, or is defeated. You may move up to 2 spaces.
- **Battle Fury:** Each time after one of your resolved cards is discarded, you may perform its primary action.
- **Angry Swipe:** Target a hero adjacent to you; +1 Attack for every card in your discard.

The words Attack in the two bonus sentences represent the printed Attack icon followed by its label. Punctuation/case above retain the printed clauses; layout line breaks are not rules.

## Setup and progression

Starting hand: Angry Strike, Chilling Howl, Trace, Menace, Hold Ground. Deck: the twelve Tier II/III faces and Battle Fury. Each colored Tier I upgrades to either face of that color's Tier II, then either Tier III; the other choice becomes its printed item. Angry Swipe is returned to the box in normal even-player setup, following the standing p.20 Handicap deferral. Eighteen dealt faces, nineteen inventoried; no engine deferral is permitted on the eighteen when the series closes.

## Capability contract map

These are checked implementation routes, with missing pieces explicitly owned by the sequence. Composition must validate actual parameter/output schemas again.

| Printed clause | Existing contract / required delta | Owner |
| --- | --- | --- |
| Owned resolved-card discard | `discard-resolved-card@1` adds actor-owned selection, played identity, Active cancellation and retrieval; built vocabulary remains deferred until dealing | 8.41a |
| +1 Attack per owned discard | `RosterValueQueryKind.OwnedDiscardCount` counts live owned discard locations; ordinary `RosterValueContributionDefinition` owns the bonus, with no new production consumer yet | 8.41b |
| Discard present | `owned-card-present@1`, `hero` and `location=DiscardPile`; already registered | 8.41d |
| Modal before-attack move / hero or unit target | `GarrusEffectContent.PursuitAttack` composes ordinary Choose, discard presence conditional, direct Move and typed target binding; no new attack-sequence contract needed | 8.41b, 8.41d |
| Move beside named enemy, then Push it | `GarrusEffectContent.MovePushAfterAttack` composes the deferred `move-adjacent-to-enemy-hero-for-push@1`, explicit adjacent-hero selection and ordinary three-space obstacle-ignoring Push; zero is applied under GA-Q1 | 8.41b |
| This-round enemy-hero aura | `HeroesInRadius`, `ThisRound`, printed radius, opposing relation, FastTravel restriction and Movement activity limit 2 all exist | 8.41d |
| Ranged unit moved closer/farther | Printed Range unit selector + direct Move `TowardActor` / `AwayFromActor`, exact distance 1 | 8.41d |
| Repeat once/twice | Existing `RepeatAction` control node with `maximumRepeats`; fresh selection and consequence boundary each time | 8.41d |
| Two enemy heroes in printed radius | `presence-count@1` threshold 2 with a composed printed-Radius value, presence rather than immunity-filtered targets | 8.41d |
| Retrieve one/up to two discarded cards | `retrieve-card@1` now admits the played-discard state; compose optional repeated selection | 8.41a, 8.41d |
| Enemy discards if able / or is defeated | Printed Range hero selection, `synthetic-discard-chosen-card@2` reports inability, ordinary hero Defeat on that branch | 8.41d |
| Optional self Move | Existing exact/up-to direct Move + optional control node | 8.41d |
| Battle Fury | New resolved-card-discard timing fact and exact-card primary grant; use existing resumable trigger/grant infrastructure | 8.41c |

## Open verification points

**GA-Q1 - answered by user-approved interpretation, 2026-09-17:** After viewing both exact card texts, the user ruled "I think yes, he can move 0." Choosing a zero-space move on Blunt Force or Send Flying while already adjacent to an eligible enemy hero satisfies `if you do` and permits that hero's Push. Declining the whole optional clause still performs neither Move nor Push. This is authority-hierarchy level 5, not an official ruling; a higher-authority clarification supersedes it. The earlier [community Garrus FAQ](https://www.cockeyedandcurious.com/goa2/heroes/garrus) lead was inconclusive: its attributed reply only referred to another discussion. This question is also indexed as Q35 in [OPEN_QUESTIONS.md](OPEN_QUESTIONS.md).

Battle Fury's interrupt placement must be established against p.17 timing and every actual discard producer before implementation; the name of an existing action trigger is insufficient evidence of suitability.
