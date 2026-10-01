# Phase 9 - Historical Handoffs

Return to the [active Phase 9 definitions](../../Roadmap/PHASE_9.md).

## Milestone 9.15 Triggered Defeat and card-text Removal rows evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.15 | A Defeat inside a triggered effect and a minion removed by card text both reach the rule-event log and the seat-public rows like their card-action siblings, the executor's scoreboard fallback retired; two regressions failing on the base; wire and schema unmoved. [Definition](../../Roadmap/PHASE_9.md#x-915-the-match-log-records-triggered-effect-defeats-and-card-text-removals). | `milestone/9.15` |

## Milestone 9.19 Card-text discard row evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.19 | Every card-text discard writes a seat-public match-log row naming the card's owner and the card, narrated and printed in every seat, on the reveal row's path; wire /28 -> /29; proven by a Runestone regression failing on the base and the node-to-row derivation cases. [Definition](../../Roadmap/PHASE_9.md#x-919-a-card-text-discard-is-narrated-and-logged). | `milestone/9.19` |

## Milestone 9.27 Package evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.27 | `Builds/Playtest-9.27-razzle` cut from `17c2072f` with stamp `1.0-9.27-razzle+20260930224123-a5f67598`, protocol /28, schema 50, 28 heroes with Razzle dealt and gated, after the T3 gate (harness 5513 at the root; Edit 6561 with 37 established skips and Play 1016 reused from the 9.21k landing on identical code; lobby 31) and the build-and-launch smoke. [Definition](../../Roadmap/PHASE_9.md#x-927-package-the-2026-10-01-build-with-razzle). | `milestone/9.27` |

## Milestone 9.21k Player-path gate evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21k | Razzle passes the permanent player-path gate: his content-derived table, nineteen journeys reading what each answer changed on the board, his dealing fixture, and a recorded six-seat match reaching the acting-body pick; three journey defects fixed in test code. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921k-gate-razzle-on-the-player-path). | `milestone/9.21k` |

## Milestone 9.21j Dealing evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21j | Razzle the Ringmaster dealt as the twenty-eighth hero with his eighteen faces, six handlers, body supply of four, staged boards, private art and the acting body on the wire (/28), every pin moved with its cause. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921j-deal-razzle-with-his-artwork). | `milestone/9.21j` |

## Milestone 9.21i Standees and acting cue evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21i | One standee per body with pips, the acting-body pick by a click on a standee, the gold acting ring on the settled body and a faint one on his other bodies, cues and fades per body. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921i-present-several-standees-and-the-acting-cue). | `milestone/9.21i` |

## Milestone 9.21h Different-body repeat evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21h | Twin Strike and Spectacle as "may" repeats performed by a different body, measured from that body with the first target excluded, and the Active-effect radius read from any body (RZ-Q13). [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921h-implement-the-repeat-by-a-different-body). | `milestone/9.21h` |

## Milestone 9.21g Counting faces evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21g | Crowd Control's Defense counts his other bodies from the attacked body as a shared value contribution; Rummage and Ransack retrieve behind a generic other-bodies-in-radius read; proven on the staged round. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921g-implement-crowd-controls-defense-and-the-counting-faces). | `milestone/9.21g` |

## Milestone 9.21f Body moves evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21f | The Alleyoop, Tightrope and Magic Trick families and Theatrics composed on the existing swap, push and move vocabulary plus two generic body capabilities; RZ-Q5 enforced in the printed-icon selections; proven on a staged round. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921f-implement-the-moves-and-swaps-of-his-bodies). | `milestone/9.21f` |

## Milestone 9.21e Removal faces evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21e | Phantom Strike, Hit and Gone, Into Thin Air and Crowd Control's Skill remove his own bodies through one generic Removal that is never a Defeat, the acting body included and the lowest remaining body taking over; proven on the staged round. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921e-implement-the-removal-faces). | `milestone/9.21e` |

## Milestone 9.21d Stunt Doubles evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21d | Stunt Doubles and the boxed Stunted Doubles composed: a generic spawn of the actor's bodies from his supply into empty spaces within the printed Radius of the acting body, widened by the Radius item, 0-3 per play, proven on a staged round including restore and replay mid-spawn. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921d-implement-stunt-doubles-and-the-spawn). | `milestone/9.21d` |

## Milestone 9.21c Roster reading evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21c | Every capability the dealt roster composes classified and proven for a many-bodied hero: bodies as spaces, the hero counted once, presence over every body, the attacked body for "the target", a body-naming `BoardObjectReference`; a restore-fidelity defect fixed. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921c-make-every-other-hero-read-a-many-bodied-hero). | `milestone/9.21c` |

## Milestone 9.21b Acting body evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21b | A twelfth pending family, "which of you acts", raised before the action when more than one body stands, owned and delegated like the action itself, settling the acting body for every read; snapshot v50, wire /27, bot answer, status lines; proven on the staged four-body hero. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921b-choose-the-acting-body-for-each-action). | `milestone/9.21b` |

## Milestone 9.22 Adjacent target evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.22 | Get over here! and Grappling Bolt offer an already-adjacent target under ruling W9 (superseding W6), through a shared zero-space-move reading in the selection consumer and the offer check; three regressions, the 9.6-era pins restored, a 188-selection sweep naming only these two faces. [Definition](../../Roadmap/PHASE_9.md#x-922-get-over-here-and-grappling-bolt-offer-an-already-adjacent-target). | `milestone/9.22` |

## Milestone 9.21a Many-bodies model evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21a | A hero may stand on several spaces as one hero: body ordinals on occupancy, targets and commands, a declared supply, the acting-body seam, every named site body-aware or refusing by name, snapshot v49 and wire /26, proven on a staged four-body hero. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921a-build-the-many-bodies-model-in-the-engine). | `milestone/9.21a` |

## Milestone 9.21 Razzle inventory evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.21 | Razzle's nineteen fronts, profile, dashboard and standee evidence inventoried; the one-space-per-hero engine seams named; RZ-Q1-RZ-Q12 recorded with recommendations; blocks 9.21a-9.21k mapped. [Definition](../../Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md#x-921-inventory-razzle-and-map-the-implementation-sequence). | `milestone/9.21` |

## Milestone 9.24 Package evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.24 | `Builds/Playtest-9.24-roster27` cut from `0cbbb652` with stamp `1.0-9.24-roster27+20260930174509-e2ca6150`, protocol /27, schema 50, 27 heroes and everything landed since 9.12 through 9.25, after the T3 gate (harness 5487, Edit 6529 with 37 established skips, Play 985) and the build-and-launch smoke. [Definition](../../Roadmap/PHASE_9.md#x-924-package-the-2026-09-30-build-without-razzle). | `milestone/9.24` |

## Milestone 9.25 Token captions and size evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.25 | Tali's Ice and both Totems drawn with no caption over their prepared faces and every placed token, the Turret with it, at 0.78 of its space instead of 0.68 with the outline still inside the space's corners, proven by a caption regression failing on the base and a content-derived fit regression, with the capture sheet sent to the user. [Definition](../../Roadmap/PHASE_9.md#x-925-talis-pieces-lose-their-captions-and-every-token-grows). | `milestone/9.25` |

## Milestone 9.20 Rune marks evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.20 | Snorri's rune marks centred on their turn slots as clean printed-glyph pictures at a size that fits the card, the squashed marker import fixed, approved by the user on the before/after sheet. [Definition](../../Roadmap/PHASE_9.md#x-920-snorris-rune-marks-are-centred-on-their-turn-slots-and-show-only-their-glyph). | `milestone/9.20` |

## Milestone 9.18 Token outline evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.18 | Every catalog token kind drawn with one thin black outline of the shared width, proven by a content-derived view regression and approved by the user on the before/after sheet. [Definition](../../Roadmap/PHASE_9.md#x-918-every-token-piece-carries-a-thin-black-outline). | `milestone/9.18` |

## Milestone 9.17a Runestone binding evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.17a | Charged Runestone's blue clause bound to the anvil rune (3) and Inscribed Runebomb's gold/blue unswapped, as a Snorri content correction with a whole-match regression replaying the logged play; 9.17's reading withdrawn; 9.19 queued for the unlogged card-text discard. [Definition](../../Roadmap/PHASE_9.md#x-917a-the-runestones-target-and-discard-are-visible-when-they-happen). | `milestone/9.17a` |

## Milestone 9.16 Turret piece evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.16 | Trinkets' Turret drawn as its outlined cut-out figure on the token footprint, upright and camera-facing, with the baked white ring, the opaque-material black square and the roll defect gone; the user picked treatment A with a thin black outline from four captured treatments. [Definition](../../Roadmap/PHASE_9.md#x-916-trinkets-turret-piece-reads-as-a-turret). | `milestone/9.16` |

## Milestone 9.17 Charged Runestone evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.17 | The live round-2 Charged Runestone was a legal do-nothing play under rune 3 (the card prints no clause for it); a whole-match regression pins the round-2 rune reading; no engine change; OQ-9.17 for the user. [Definition](../../Roadmap/PHASE_9.md#x-917-charged-runestone-asks-for-its-target-when-a-rune-is-active). | `milestone/9.17` |

## Milestone 9.13 Expected-outcome pilot evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.13 | The expected-outcome table format, its harness-runnable executor, and 168 blind-reader cases over Mortimer, Mrak and Wuk each ending in one recorded disposition; two engine defects fixed (double hero Defeat reward fault, box-granted item fault); one content finding and one diagnostic gap queued; six open questions for the user. [Definition](../../Roadmap/PHASE_9.md#x-913-independent-expected-outcome-corpus-the-format-the-executor-and-the-three-hero-pilot). | `milestone/9.13` |

## Milestone 9.12 Playtest package evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.12 | `Builds/Playtest-9.12-bugnet` (Windows, stamp `1.0-9.12-bugnet+20260929164834-72f7de21`, protocol `/25`, schema 48, 27 heroes) cut from `e92318e6` behind the full harness, Edit Mode and Play Mode gate, the build-and-launch smoke and a packaged-folder launch. [Definition](../../Roadmap/PHASE_9.md#x-912-package-the-2026-09-29-playtest-build). | `milestone/9.12` |
## Milestone 9.9a Direction-bound targets and standing-effect steps evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.9a | Cutter's "in the direction of the move" Attacks greyed when no answer reaches a target, by following the direction-bound selection through the move; Misa's Next-turn step and Trinkets' Disruptor effects derived and staged as a fourth before-action shape, no finding. [Definition](../../Roadmap/PHASE_9.md#x-99a-the-three-items-99-recorded-direction-bound-targets-and-standing-effect-steps). | `milestone/9.9a` |

## Milestone 9.10 Random line-up soak evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.10 | Ten random six-hero line-ups restored, rewound and replayed at every command with ledger, ownership and occupancy invariants; restored and rewound projections now list the board and held markers in one order. [Definition](../../Roadmap/PHASE_9.md#x-910-random-line-ups-replay-restore-and-rewind-into-the-same-match). | `milestone/9.10` |

## Milestone 9.11 Copy and codec completeness evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.11 | Reflection completeness over every projection copy, seat record and wire codec; three dropped client fields carried, two of them moving the wire to `/25`. [Definition](../../Roadmap/PHASE_9.md#x-911-every-copy-projection-and-wire-form-carries-every-field). | `milestone/9.11` |

## Milestone 9.9 Before-the-action offers evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.9 | 154 content-derived faces with a before-the-action step staged in 770 positions and read at the rules, seat and HUD layers; a standing before-action ability no longer greys the play it exists for. [Definition](../../Roadmap/PHASE_9.md#x-99-a-play-reached-only-through-its-own-before-the-action-step-is-offered-at-every-layer). | `milestone/9.9` |

## Milestone 9.8g Player-path gate evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8g | Mortimer gated on the player path: seventeen real-input journeys for eighteen faces and five components, on a headlessly measured board. The twenty-seventh hero series is complete. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98g-gate-mortimer-on-the-player-path). | `milestone/9.8g` |

## Milestone 9.8f Dealing evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8f | Mortimer dealt, registered and pickable with his private art. Awaken! corrected to a Defense/Skill card whose text applies when discarded to defend. W8: a push target moving 0 spaces stays legal and the card is judged whole, read through `IEffectInertDisplacementCapabilityHandler`. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98f-deal-mortimer-with-his-artwork). | `milestone/9.8f` |

## Milestone 9.8e Green Skills evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8e | Mortimer's five Green Skills: an exact Zombie move, a Swap with a Zombie, and "once per turn" Replace of an enemy minion beside two Zombies with a Zombie. It adds p.19's minion-to-token Replace (removed, no coins, Lane-Push cause) and the `onceAcrossTheGroup` clause. Unregistered until 9.8f. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98e-implement-the-five-green-skills). | `milestone/9.8e` |

## Milestone 9.8d Blue Skills evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8d | Mortimer's five Blue Skills: a Zombie moves up to one space and may push a piece beside it from its own space (new `pusherSpace` push input), the coin only with a Zombie to move (MO-Q3), and "Move 1 space". Unregistered until 9.8f. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98d-implement-the-five-blue-skills). | `milestone/9.8d` |

## Milestone 9.8c Knife and the Red Attacks evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8c | Knife of the Living Dead and the five Red Attacks. It adds the selection group behind "each enemy hero can only be targeted once", `excludeTheAttackTarget`, presence-count token adjacency and a per-pass "+1 Attack". It fixes the Attack total after step 1's text, which missed a value the text's last step created. Unregistered until 9.8f. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98c-implement-knife-of-the-living-dead-and-the-five-red-attacks). | `milestone/9.8c` |

## Milestone 9.8b Repeated choice and Master of Puppets evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8b | Mortimer's "choose one / up to two / up to three times" composition, with Master of Puppets raising only "up to three times" to five behind an unlock gate whose explicit else keeps RULING 53's exemption from applying; Master of Puppets as a permanent declaration. Unregistered until 9.8f. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98b-implement-choose-up-to-n-times-and-master-of-puppets). | `milestone/9.8b` |

## Milestone 9.8a Zombie token and Awaken! evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8a | Mortimer's Zombie token (four, retained at round end) and Awaken! placing up to four into empty spawn points in radius or spaces beside him, with p.18's reclaim; the shared `spawnPointOrAdjacent` placement clause. Unregistered until 9.8f. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98a-implement-the-zombie-token-and-awaken). | `milestone/9.8a` |

## Milestone 9.8 Mortimer inventory evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.8 | Mortimer's nineteen fronts, profile, stat bars, dashboard, standee and Zombie token inventoried; MO-Q1-MO-Q4 recorded; engine seams probed and each new contract assigned to a block. [Definition](../../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md#x-98-inventory-mortimer-and-map-the-implementation-sequence). | `milestone/9.8` |

## Milestone 9.7 Legal action evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.7 | Every greyed printed primary is taken anyway through a test-only path; none could have changed the game once starts refuse a restricted action as the offer does (Spell Break had been enforced on offers only). [Definition](../../Roadmap/PHASE_9.md#x-97-every-legal-action-is-offered). | `milestone/9.7` |

## Milestone 9.6 Offer against outcome evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.6 | Every offered printed primary does something, checked by whole-match self-play against Holding the same card; the offer walk now reads predicates, pure value reads, unbound inputs and followable selections; rulings W6 and W7; Swift's terrain narrowing and Tali's "if able" batch composed as printed. [Definition](../../Roadmap/PHASE_9.md#x-96-every-offered-action-does-something). | `milestone/9.6` |

## Milestone 9.5 Future hero evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.5 | The new-hero checklist in PRINTED_WORDING, carried lesson 17 for the next dealing, and a test holding the newest dealt hero to every family; dry run on Wuk. [Definition](../../Roadmap/PHASE_9.md#x-95-carry-it-to-every-future-hero). | `milestone/9.5` |

## Milestone 9.4 Journey outcome evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.4 | Every player-path journey that answers a card's question reads what it changed, enforced by a source scan; Tali's Reign of Winter and Ursafar's two Angry Roar journeys extended. [Definition](../../Roadmap/PHASE_9.md#x-94-journeys-assert-outcomes-not-prompts). | `milestone/9.4` |

## Milestone 9.3 Condition wording evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.3 | "If you do", printed value changes and "each" held to their declarations; every such face read; ruling W5 for "up to" moves; no content change needed. [Definition](../../Roadmap/PHASE_9.md#x-93-repeat-and-condition-family). | `milestone/9.3` |

## Milestone 9.2 Targeting wording evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.2 | Targeting words held to their declarations (W4 minion Defeats, enemy/friendly relations, exclusions, printed-Radius reach); eight faces corrected across Whisper, Wuk, Sabina and Cutter; the 8.83 sweep moved onto the roster-derived corpus. [Definition](../../Roadmap/PHASE_9.md#x-92-targeting-wording-family). | `milestone/9.2` |

## Milestone 9.1 Wording corpus and optionality evidence

| Milestone | Outcome and definition | Commit |
| --- | --- | --- |
| 9.1 | A roster-derived corpus of every dealt face and a printed-wording conformance test for "may", "up to", "if able" and "repeat"; the user's rulings W1-W3 applied to eight faces; an `if able` branch that cannot be performed is skipped. [Definition](../../Roadmap/PHASE_9.md#x-91-roster-derived-wording-corpus-and-the-optionality-family). | `milestone/9.1` |
