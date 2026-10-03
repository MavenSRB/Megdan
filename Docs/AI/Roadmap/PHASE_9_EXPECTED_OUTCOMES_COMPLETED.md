# Phase 9 - Completed expected-outcome milestone definitions

Canonical active roadmap shard holding the complete definitions for milestones 9.13 through 9.13f. Return to [ROADMAP.md](../ROADMAP.md) or the active Phase 9 shard [PHASE_9.md](PHASE_9.md). Historical completion evidence: [PHASE_9 handoffs](../Archive/Phase-Handoffs/PHASE_9.md).

Created 2026-10-03 at the 9.13f handoff when PHASE_9.md crossed its 15,000-word hard limit. The contiguous expected-outcome corpus moved here losslessly; the active sequence beginning at 9.41 remains in PHASE_9.md.

## Independent expected-outcome corpus, authorized 2026-09-29

Authorized 2026-09-29 by the user ("start 9.13") after the 9.12 cut; 9.13 alone is authorized, the batches 9.13a-9.13e are queued and each needs its own go.

### [x] 9.13 Independent expected-outcome corpus: the format, the executor and the three-hero pilot

Proposed 2026-09-29 by the agent after the audit of which playtest bugs 9.9-9.11 would have caught: the class none of them reaches is a card that does something other than its printed text says (8.49's lives by tier, 8.83's second minion, 8.82's Radius item, 8.118's held Defeats, 8.96's Totem), because every journey so far was written by the agent that implemented the card, reading the text the same way. Outcome: a second reader, shown only the card image and the rulebook page and never the implementation, writes expected-outcome tables ("from this position, after this play, the board is X") for every dealt face, and those tables execute against the engine as fixtures; each numbered rulebook rule that changes lives, coins or levels becomes one invariant read from the page. Closing check: every dealt face has at least one executed table; every disagreement is either a corrected engine defect with a failing-then-passing regression or a recorded ruling; T2. Queued only: recording does not authorize implementation.

Scope of 9.13 itself: (1) a table format a blind reader can write without knowing the board - references (actor, enemy-hero-N, friendly-minion-N, token-N), placements relative to the actor ("adjacent", "distance 2 in a straight line", "adjacent to enemy-hero-1"), the play (printed card name, action, the answers in the order the card asks them) and expectations (defeated, at, moved, pushed, tokens on board, coins, lives, card state, offered); (2) a harness-runnable executor in Assets/Tests/EditMode that stages each table on the official map through the staged-board seam, plays it through Director commands, answers from the table, and compares, reporting an unsupported phrase by name rather than guessing; (3) the reader protocol: a reader sees only the Tabletopia card images and the rulebook text, never Assets/ or Docs/AI/, and cites the card and page for every expectation; (4) the pilot: tables for every face of Mortimer, Mrak and Wuk, executed, with every disagreement dispositioned as engine defect (failing-then-passing regression), reader misreading (corrected with the printed clause) or open question for the user. Closing check: the three pilot heroes' faces each have at least one executed table; the disagreement list is dispositioned; harness green; Unity Edit Mode for the new fixture. T2.

Result: the format (`Docs/Testing/EXPECTED_OUTCOME_TABLES.md`: relative placements, printed card names, answers in printed order, one fact per expectation), the executor (`ExpectedOutcomeExecutor` staging through `InitialScenarioDescription`, `MatchStagedProgression`, the set-aside item path and the aggregate's token commitment, playing through Director commands, refusing any phrase it cannot stage by name) and a 23-case smoke table proving every vocabulary item. Three blind readers, shown only the Tabletopia card images and the rulebook text, wrote 168 cases over the 57 faces of Mortimer (58), Mrak (57) and Wuk (53); each case ends in exactly one disposition recorded in `<hero>.dispositions.json`: 22 agree as written; 22 collide with a settled ruling (MO-Q1, MR-Q3, WU-Q1, WU-Q3, the 8.81 minion leash, p.13's minion Defeat, RULING 53) and now assert the ruling through an `expect-override`, so a ruling regression fails; 34 were executor gaps closed in the pass (straight-line trample walks, another hero's own card choice, "one or both" bullets, the p.19 correction question); 76 are not executable as written (46 run with every executable fact passing and the reader's unsupported note recorded; 30 need setup the reader could only describe, such as an empty enemy hand or a terrain obstacle) plus 8 Handicap faces p.6 boxes; 6 are open questions for the user on one reading. Two engine defects found outside the tables and fixed with regressions failing on f651d09e: two hero Defeats in one Attack shared a `DefeatOutcomeId` and the second reward faulted the command (the hero arm of 8.118's minion fix; `TwoHeroDefeatsInOneAttack_EachPaysItsOwnReward`), and a p.16 item granted from the box named no dealt card so the next Attack or Defense preview faulted (`ABoxItem_CountsInTheNextAttack_WithoutFaultingTheDefensePreview`, Dodger's Darkest Ritual at level 8). Content finding, routed to 9.14: Mortimer's Tier III Attack prints BRAAAAAAAAINS...! with eight a's and the content's display name has seven; the display name feeds the pinned identity digest, so it is corrected with its own re-pin. Diagnostic gap, queued: `RuleEventLogProjection` unpacks no `TriggeredEffectProgressed`, so a triggered-effect Defeat (High Voltage) logs no `MinionDefeated`, and Ignatia's card-text Removal emits no `MinionRemovalCompleted`. Open for the user (OQ-9.13), answered 2026-10-03 as ruling W10 and built at 9.39 (a decline ends the remaining passes): whether declining one pass of "Place up to N" or "choose up to N times" ends the remaining passes (the engine then asked each remaining pass; W2 settles only that the count is the player's) - cases `mortimer-awaken-up-to-declined-after-two`, `mortimer-stalking-scalpers-range-item-reaches-distance-5`, `mortimer-crowd-surf-swap-ignores-blocked-path`, `mortimer-macabre-mayhem-zombie-pushes-friendly-never-actor`, `wuk-mystic-saplings-up-to-one-and-radius-limit`, `wuk-mystic-saplings-short-supply-takes-from-board`. Readers' ambiguity lists live beside the tables' sources in the milestone commit.

Completed 2026-09-29.

Historical evidence: [9.13 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-913-expected-outcome-pilot-evidence).

Authorized 2026-10-03: **9.13a -> 9.13b -> 9.13c -> 9.13d -> 9.13e** ("Go for 9.13a/b", extended to "Up to E"). Execute in order with independent readers, separate T2 validation, disagreement dispositions and local commits.

### [x] 9.13a Tables for Wasp, Arien, Brogan, Sabina and Tigerclaw

Queued: the pilot format applied by blind readers to the first five dealt heroes; same closing check per hero. T2.

Result: three independent source-only readers authored 138 cases across the five heroes; 132 execute, covering every dealt face and each unlocked Ultimate. Dispositions preserve settled-ruling disagreements and name corrected staging/format issues. A per-face guard prevents missing executable coverage. Generic executor additions support explicit roster/coin setup, ranged Defense probes, held-card history, same/next-turn follow-ups, Poison/modifier facts and actual Defense-offer assertions; regression checks refuse omitted follow-ups and malformed inputs. Two source disagreements corrected engine behavior: Blink Strike cannot land on a generic obstacle, and Ballista's primary-Defense prohibition shares its +2 Attack's friendly-adjacency condition. Conditional offers survive paused-Defense restoration. Tigerclaw advances to `tigerclaw-9-13a-rev-1`; the new generic restriction field mechanically changes all identity pins, isolated from the one semantic content change. No snapshot or wire change.

Completed 2026-10-03.

Historical evidence: [9.13a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-913a-first-five-outcome-tables-evidence).

Additional coverage backlog (not part of the per-face acceptance): `arien-liquid-leap-spawn-exclusions` remains **parked, not executed** because the neutral format cannot name an excluded destination. Owner: expected-outcome executor; dependency: a source-relative destination-offer assertion; closing check: reject both a spawn-point landing and a non-spawn landing adjacent to an empty spawn point, with a legal landing as control. Arien's ordinary Liquid Leap outcome still executes. The five boxed Handicap faces remain outside standard dealt play under p.6, not passing action coverage.

### [x] 9.13b Tables for Xargatha, Dodger, Misa, Silverarrow and Ursafar

Queued as 9.13a. T2.

Result: independent source-only readers authored 152 cases; 146 execute, covering every dealt face and each unlocked Ultimate. Dispositions preserve original readings and neutral fixture adaptations. Generic executor checks now cover real reserve respawns and exact empty minion spawn counts, owned item/card facts, explicit delayed checkpoints and declines, same-actor next-turn actions, exact swaps, completed Attack counts, maximum offered Movement and actual obstacle traversal. Wrong-fact controls prove assertions fail on their intended observations. One printed-rule disagreement exposed invalid negative Attack Total guards in the pending Defense choice and network decoder; both now preserve signed totals under Dodger D-08, with stable identity/offer checks and unchanged snapshot/wire shapes.

Completed 2026-10-03.

Historical evidence: [9.13b completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-913b-second-five-outcome-tables-evidence).

Additional coverage backlog: `xargatha-stone-gaze-heroes-also-terrain` remains **parked, not executed**. Owner: expected-outcome executor; dependency: simultaneous object-type and turn-lifetime assertions; closing check: prove the enemy remains a hero and also counts as terrain only during the next turn, with before/after controls. Stone Gaze's Movement prohibition executes separately. The five boxed Handicap cases remain outside standard dealt play under p.6, not passing action coverage.

### [x] 9.13c Tables for Whisper, Tali, Swift, Cutter and Bain

Queued as 9.13a. T2.

Result: independent source-only readers authored 110 cases; one additional obstacle control brings the tables to 111, with 108 executable outcomes covering every ordinary dealt face and each Ultimate. Three boxed Handicap cases are explicitly not executed; Swift and Cutter record theirs separately. Dispositions preserve original readings and fixture adaptations. Generic executor additions cover typed color answers, final positions against original geometry, shared printed token names and real prerequisite Defense/standing plays. Two engine defects closed: obstacle-line queries now include generic occupancy while excluding off-map void, and Totem replaces the would-be Defeat before coins, bounties, Defeat facts and Lane-Push causes, preserving Brogan's removal-only prevention. Both Totem definitions advance to revision 2; obsolete token revisions reject. Card identities, snapshot schema and wire format remain unchanged.

Completed 2026-10-03.

Historical evidence: [9.13c completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-913c-third-five-outcome-tables-evidence).

Additional source-reader coverage backlog (parked, not passing coverage; recording does not authorize expansion). Owner: expected-outcome corpus maintainer. Each ordinary face has a separate executable outcome; these are extra branches/boundaries:

| Extra scenario | Dependency | Closing check |
| --- | --- | --- |
| Swift Mobile Scout fast travel | Exact zone staging and fast-travel answer/fact | Off-file end-turn placement followed by legal travel; enemy-presence blocked control. |
| Swift Bullet Time targeting history | Grant-chain-scoped target observation | Same enemy rejected on the second grant-chain targeting, different enemy accepted; earlier ordinary targeting does not falsely exclude under SW-Q2. |
| Swift Hunting Season bounty ceiling | Three next-turn minion Defeats and isolated bonus income | Exactly two bonus coins on the first two in-radius Defeats, none on the third or outside the next turn/radius. |
| Cutter Walk the Plank push branch | Exact zone-border staging | Same-zone push causes no discard; crossing forces discard if able or Defeat with an empty hand under CU-13. |
| Cutter A Fistful of Coins solo win | Match-outcome and solo-winner assertions | Ten coins plus three wins for Cutter alone, including loss for her teammates; nine plus three does not win under CU-D1. |
| Cutter Legend of the Skies limit | Scoped primary-action/grant history | First primary grants the previous own-slot action once; borrowed primary cannot recurse; turn-one no-previous-slot control. |

### [x] 9.13d Tables for Garrus, Widget, Rowenna, Min and Brynn

Queued as 9.13a. T2.

Result: source-only readings integrated as 95 executed cases covering every ordinary face and each actual Ultimate effect. Dispositions retain original disagreements, source corrections and staging adaptations. Test-only generic grammar now accepts consecutive numbered bullets, exact zero-distance position assertions and explicit confirm answers, with refusal/wrong-fact controls. No production rule, content identity, snapshot or wire changes.

Completed 2026-10-03.

Historical evidence: [9.13d completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-913d-fourth-five-outcome-tables-evidence).

Additional source-reader coverage backlog (parked, not passing coverage; recording does not authorize expansion). Owner: expected-outcome corpus maintainer. Dependencies and closing checks: Garrus's Howl aura needs a follow-up Fast Travel offer and an outside-radius control; Rowenna's Paragon of Grace repeat needs two adjacent enemy units and both Defeats after the coin; Brynn's conditional branches need two distinct Decoy targets, obstacle-qualified remote swap targets, and guides' second different friendly unit, each with its condition absent as a negative control. Ordinary per-face outcomes are separate. Widget paired-move boundary: an exact common-vector assertion with a blocked endpoint control, beyond the current equal-distance/adjacency outcome.

Min's Viper/Cobra staged-Smoke-bomb cases are explicitly synthetic diagnostics, not legal-reachability evidence under settled MI-Q13k; separate no-bomb attacks cover legal ordinary play. The five boxed Handicap faces remain outside standard dealt play under p.6.

### [x] 9.13e Tables for Hanu, Ignatia, Trinkets and Snorri, and the rulebook ledger invariants

Queued: the last four heroes, plus one invariant per numbered rulebook rule that changes lives, coins or levels, read from the page rather than the code. T2.

Result: independent source-only readers froze the four tables and the printed rulebook economy map before implementation inspection. All ordinary faces and actual Ultimates have executable outcomes: Hanu 18, Ignatia 18, Trinkets 20 (including paired Ultimate-Radius controls), Snorri 18. Dispositions retain changed source proposals and settled rulings; none skips an E case. Test-only adapters observe owned obstacles, rewritten Initiative, actual action choosers, the coin/text face and real rune assignments; Defense scenarios can pin a printed attacking card. No production rules, content, identity, snapshot or wire change.

[The source-to-check map](../Testing/RULEBOOK_LEDGER_INVARIANTS.md) owns numbered setup, lane-push and Attack-order coverage and the economy tables, including live no-reward respawn, pity ordering and mandatory level purchases. The four Handicap faces remain boxed; odd-player/two-hero-per-player/double-lane variants are source-only, not passing configured coverage.

Supplemental Snorri table coverage is parked, not authorized or counted as passing. Owner: expected-outcome corpus maintainer. Dependencies: legal glyph assignments and real public turn/action transitions. Closing checks: remaining Attack glyph clauses need exact target, movement and retrieval outcomes; Hidden/Deep Passage need remaining traversal/immunity and expiry probes; Ancestral recovery/swap branches and Grace's item exchange need exact before/after card/item states; Oaths need later same-turn immunity and alternate category Blocks with named attacks; Runestone/Runebomb need remaining glyph-to-color discard and wrong-color controls; Inscribe persistence and Rune Mastery expiry need before/after rune sets across defeat, round and next-action boundaries. These supplement, not replace, the ordinary per-face outcomes and existing hero-specific tests.

Completed 2026-10-03. The approved A-E sequence ends here; further work requires authorization.

Historical evidence: [9.13e completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-913e-final-four-outcome-tables-and-rulebook-evidence).

### [x] 9.13f Close recorded supplemental outcome coverage

Authorized 2026-10-03: "Spin up agent of suitable level to finish the supplemental test gaps while you do that." Scope: the explicit A-E backlog above (Arien, Xargatha, Swift, Cutter, Garrus, Rowenna, Widget, Brynn, Snorri), with source-linked positive/boundary checks and any narrow test-adapter extensions needed for those closing checks. Preserve frozen cases and rulings. No new hero, variant, presentation work or unrelated refactor. T2; focused checks then the enumerated affected corpus and Unity parity. Surface any newly demonstrated production defect for disposition rather than silently broadening the work. Root coordinates serial tests and the local milestone commit. Concurrent user-requested level-up-screen investigation is read-only; any correction needs authorization.

Completed 2026-10-03. The recorded supplemental gaps are now executable or source-linked across the nine named tables; no production-rule defect was demonstrated.

Historical evidence: [9.13f completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-913f-supplemental-outcome-coverage-evidence).
