# Phase 9 - Rules fidelity and hero intake hardening

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_9 handoffs](../Archive/Phase-Handoffs/PHASE_9.md).

## Phase 9 - Rules fidelity and hero intake hardening

On 2026-09-28 the user closed Phase 8 and started Phase 9 with this work ("Stop with phas 8. Lets start phase 9 from this point forward"), replacing the earlier presentation-and-pitch plan, which is retired to [the archive](../Archive/PHASE_9_PRESENTATION_PLAN_RETIRED.md) and authorizes nothing.

Authorized 2026-09-28 by the user after reviewing the playtest record of Whisper, Ursafar, Swift, Trinkets and Tali ("Write it up and start doing it. Also make sure to leave proper stuff so the next heroes ... dont suffer from same issues"). The measured cause families: an effect that asks and then does nothing (8.82, 8.97, 8.48, 8.96); an offer that disagrees with its resolution (8.104, 8.97); a printed word composed as a different rule (8.42, 8.83, 8.115); and evidence taken where no player stands (8.80, 8.102). Bounded sequence 9.1 -> 9.2 -> 9.3 -> 9.4 -> 9.5, executed one at a time with separate validation and commits. Every finding is a failing-then-passing regression or an allow-list row quoting the clause that justifies it; a wording with two honest readings is recorded as an open question for the user and not decided here. Card text stays inert to rules: only tests read `CardSourceContent.PrintedText`.

### [x] 9.1 Roster-derived wording corpus and the optionality family

Outcome: one test-side corpus derived from the roster rather than a hand list - every dealt hero, every verified face with its printed text, every effect body it composes including its Active effects' trigger bodies - which fails if a dealt hero is missing; and the first conformance family on it: "may" / "you may" (an optional surface), "up to" (the distance or count is the decision), "if able" (skipped when impossible) and their absence, checked face by face between printed wording and composed declarations. `RosterMoveLeashSweepTests` moves onto the shared corpus. Closing check: every mismatch fixed with a failing-then-passing regression or allow-listed with its quoted clause; harness and the affected hero suites green.

Completed 2026-09-28. `PrintedWordingCorpus` derives every dealt face from `QuickGameSetupConfiguration.DealtHeroes` through the match's own hand factory and walks each face's card-text bodies, the Active effects they raise with those effects' trigger bodies, and its Ultimate's triggered effect (26 heroes, 468 faces). `PrintedWordingConformanceTests` holds printed "may", "up to", "if able" and "repeat" to the composition, with an allow-list whose rows each quote their reason and must still trip their rule. Its first run flagged eight faces and nothing else; the user settled the three wordings behind them as rulings W1-W3 ([PRINTED_WORDING](../Rules/PRINTED_WORDING.md)), and the eight were corrected: Rowenna's two Paragons and Mrak's Epicentre repeat automatically (W1), Whisper's Blood Frenzy asks after each repeat (W2), Min's two Grenades and Trinkets' Self-Destruct and Emergency Protocol hit every enemy hero present (W3). Composing W3 exposed an engine defect: an `if able` Optional entered a branch whose mandatory selection had nobody to name, so the whole text stopped (the Grenade stayed on the board); an `if able` branch that cannot be performed is now skipped at the Optional, as `may` and `up to` already were. Five content revisions moved to `-9-1-rev-1` with their artwork rows and identity pins; decision labels, dealing counts, seven gate rows and six journeys follow the rulings, and a new journey stops Blood Frenzy at two attacks. `RosterMoveLeashSweepTests` keeps its hand-written list for now; 9.2 moves it onto the shared corpus. T3; evidence in the milestone commit.

Historical evidence: [9.1 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-91-wording-corpus-and-optionality-evidence).

### [x] 9.2 Targeting-wording family

Outcome: on the same corpus, "another" / "different" / "other" (an exclusion is declared), "in radius" / "in range" (bound to the card's printed icon with items, not a literal), "adjacent", "enemy" / "friendly" / "any" (the declared relation), "hero" / "minion" / "unit" (the declared kind). Closing check as 9.1.

Completed 2026-09-28. Four targeting families join `PrintedWordingConformanceTests`: every minion Defeat names an enemy minion (W4); "enemy" and "friendly" match the declared relation (attack targets are enemies by p.13, and a side named inside a condition is read off that condition); "another", "different" and "other" declare an exclusion or are allow-listed with their counting or reminder reading; and a selection's reach is the printed Radius or a number the sentence writes. `RosterMoveLeashSweepTests` now walks `PrintedWordingCorpus`, which surfaced two move bodies it had never seen (Living Tsunami, Focus's step), both correct. Asked what the rulebook says, the user confirmed W4 from the Defeat entry ("No exceptions."): Whisper's Lesser Evil and Greater Good and Wuk's Trample and Angry Stampede no longer offer friendly minions, WU-Q4 is replaced, and the engine refuses a friendly minion Defeat loudly. Rulebook reading R1 (p.9, p.16): six selections wrote their printed Radius as a literal, so a Radius item did not widen them; `choose-target` gains `measuresPrintedRadius`, read inside the selection so the offer stays exact (a separate radius node had made Close Support offerable with nobody to name). Sabina's, Cutter's, Whisper's and Wuk's revisions moved to `-9-2-rev-1`. T3; evidence in the milestone commit.

Historical evidence: [9.2 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-92-targeting-wording-evidence).

### [x] 9.3 Repeat and condition family

Outcome: "repeat" (a repeat node, its once/count and its target narrowing), "if you do" / "if" / "if this card is" (the gate reads the fact the sentence names), "each" / "all" (every subject, not one). Closing check as 9.1.

Completed 2026-09-28. Three condition families join `PrintedWordingConformanceTests`: "if you do" (a skippable step followed by a mandatory one that reads none of its output is flagged unless listed with its reading), printed value changes (each "+N"/"-N" is a declared contribution, a value modifier, or the token or marker's own property), and "each" (every face that prints it is read in a register with the shape that reaches every subject). Repeats were settled at 9.1 (W1/W2). All nineteen "if you do" faces were read; every one is gated by a mandatory predecessor, the earlier step's own output, a nested Optional or a removal-prevention cost - except Garrus's Blunt Force and Send Flying, where a 0-space "up to" move still pushes. The user settled that as ruling W5: "up to" makes 0 a real choice, so the build stands. All forty value changes and eleven "each" faces are declared. A stale note in Silverarrow's Fae Healing (that its "may" went to Silverarrow) was corrected: a May capability's decline rides on the retrieving hero's own card prompt. No card content changed. T1; evidence in the milestone commit.

Historical evidence: [9.3 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-93-condition-wording-evidence).

### [x] 9.4 Journeys assert outcomes, not prompts

Outcome: every hero's player-path journeys are audited for the 8.82 shape - a question asserted and its consequence never read. Each journey that answers a card-text question asserts the board, hand, tokens or coins the printed sentence changes, including after a decline. Closing check: the audit table recorded, every prompt-only journey extended, the affected Play Mode fixtures green.

Completed 2026-09-28. `JourneyOutcomeAuditTests` scans every player-path journey (532 across the twenty-six heroes' fixtures): after its first card-text answer, some assertion must read game state - the board, a hand, a pile, tokens, coins or a value. The audit table, measured before any change: four journeys answered without reading anything. Tali's Reign of Winter journey named the first colour offered and never saw a discard; it now names a colour the enemy holds and asserts that card of that colour reached the discard pile on the enemy's own choice. Ursafar's Angry Roar and Unbound Fury journeys walked the borrowed move and then declined its text without reading the board; both now assert the declined text moved nobody. Ignatia's undo-and-replay journey is listed: its claim is the reproduced offers, and its faces' outcomes are their own journeys'. The scan is a floor, not a proof that the right state is read; the face journeys' own assertions carry that. T2; evidence in the milestone commit.

Historical evidence: [9.4 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-94-journey-outcome-evidence).

### [x] 9.5 Carry it to every future hero

Outcome: the intake procedure and the dealing checklist name the four families, the roster-derived corpus and the outcome rule as required steps of a new hero's composition milestone; the corpus and gate fail on a new hero without them. Closing check: a documented dry run against the most recent hero, and the living-documentation validation green.

Completed 2026-09-28. [PRINTED_WORDING](../Rules/PRINTED_WORDING.md) now closes with "Required for a new hero": read every face against the families and rulings first (a wording outside W1-W5 and R1, or with two honest readings, goes to the user), run the conformance and leash-sweep tests, register every "each" face, make every journey read what changed, and give every behaviour fix its own regression. The newest carried-lessons list, which the next dealing binds, gains lesson 17 pointing there. The mechanical half needs no step from anybody: the corpus derives from the dealt roster, every family and the journey audit run over every dealt hero, and `TheNewestDealtHero_IsHeldToEveryFamily` checks whoever was dealt last. Dry run on Wuk, the newest: 18 dealt faces and 22 bodies in the corpus, both Dominance trigger bodies walked, and 9.2's W4 check is what caught his trample offering his own minions. T1; evidence in the milestone commit. The approved Phase 9 sequence is complete.

Historical evidence: [9.5 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-95-future-hero-evidence).

## Offer against outcome, approved 2026-09-28

Authorized 2026-09-28 by the user ("do it!") after the review of which playtest bugs 9.1-9.5 would have caught: the largest remaining family is an offer that disagrees with what the action can do - Whisper's Crimson Trail not offered when legal (8.104), Ursafar's Angry Roar offering a dormant card and fizzling (8.97), Misa's free step not offered before a Skill (8.95), Bounce's repeat never offered through Reload! (8.48). Bounded sequence 9.6 -> 9.7, one at a time with separate validation and commits.

### [~] 9.6 Every offered action does something

Outcome: a seeded self-play check over matches whose line-ups are drawn from every dealt hero. At each turn it copies the game once per available primary action, performs the action with a policy that takes every offered step, and records an action that leaves the board, every hand and pile, tokens, coins and Active effects exactly as they were; it also records every command the projection offered and the rules refused. Each finding is corrected with a failing-then-passing regression or listed with the printed clause that makes it legitimately empty. Closing check: the check green over its fixed seeds, every dealt hero exercised, the affected suites green.

### [ ] 9.7 Every legal action is offered

Outcome: the converse over the same states - an action the offer greys out is performed anyway through a test-only path, and one that can change the game is a greyed-out legal play (8.104's shape). Closing check as 9.6.
