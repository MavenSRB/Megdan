# Phase 9 - Twenty-seventh hero: Mortimer the Awakener

Return to [roadmap](../ROADMAP.md). [The session brief](../SESSION_BRIEF.md) owns the active and next route.

## Authorization and scope

On 2026-09-29 the user named the next hero - "Mortimer is my choice for next hero, can you read his cards in case you have questions while im still here?" - and, before sleeping, "If you are done before I wake up, start next hero work", to follow 9.6 -> 9.7. The user answered MO-Q1 to MO-Q4 the same night, before 9.8 began. This authorizes the bounded sequence **9.8 -> 9.8a -> 9.8b -> 9.8c -> 9.8d -> 9.8e -> 9.8f -> 9.8g** through a dealt, artworked and gated Mortimer, executed one milestone at a time with separate validation and commits. No package is authorized.

His rules shard (created at 9.8) will own source facts, inventory, capability mapping and questions. Eighteen ordinary fronts are dealt; the nineteenth, Shallow Graves, is a p.20 Handicap and stays boxed at setup.

The four answers, to be recorded in the shard as MO-Q1 to MO-Q4:
- **MO-Q1:** four Zombie tokens.
- **MO-Q2:** Awaken places into any empty spawn point in radius, minion or hero, plus empty spaces adjacent to him; it is not limited to the battle zone.
- **MO-Q3:** Robbing Zombies' and Stalking Scalpers' coin needs a Zombie in range (p.10: a mandatory step that cannot happen stops the action; W5 still lets a present Zombie move 0).
- **MO-Q4:** every Zombie token stays at the end of round, including one a Replace put down.

## Carried lessons

[Wuk's list](PHASE_8_TWENTY_SIXTH_HERO.md#carried-lessons) binds here unchanged, including lesson 17 (every face is held to its printed words, per [PRINTED_WORDING](../Rules/PRINTED_WORDING.md)'s "Required for a new hero"). Two more bind:

18. **Every offer is checked against its outcome** (9.6, 9.7). `OfferOutcomeTests` plays every dealt hero, so a newly dealt Mortimer is swept the moment he is pickable. A face offered where it can do nothing is a defect unless RULING 53 covers it (a false printed "If"); a greyed face that could act is a defect too.
19. **Rulings can live in tests.** RULING 53 was recorded only in Whisper's journeys until 9.6; before composing, grep the tests as well as the shards for a ruling on the same shape.

### [x] 9.8 Inventory Mortimer and map the implementation sequence

Outcome: verify the nineteen fronts, profile, dashboard, Zombie token and standee manifest pieces and the relevant rulebook pages. Record printed values, exact clauses, existing-capability matches, new contracts and the user's answers. No executable content is registered.

Acceptance:
- All nineteen fronts and the back variants are accounted for.
- Items, Range/Radius and ribbon glyphs are verified by montage.
- The star/role profile and stat bars are recorded.
- Every novel effect is probed against actual engine seams: "choose up to N times" with a repeatable bullet, the Ultimate's five, a token moved by card text, a token that pushes, swap with a token, Replace a minion with a token, and a hero adjacent to a token.
- MO-Q1-MO-Q4 are recorded as user-settled.
- T0 documentation validation.

Non-goals: gameplay code, art import or a package.

Completed 2026-09-29.
- The nineteen fronts (byte-distinct), the one back variant, the profile (three stars; Melee, Tokens, Farming, Durable, Pusher), the stat bars (Attack 8, Defense 7, Initiative 2, Movement 4) and the dashboard are inventoried in [his rules shard](../Rules/HERO_MORTIMER.md).
- So are the standee (element 8596249, `Morty`) and the Zombie token (element 7694669, two faces).
- Items and Range/Radius were verified by montage.
- MO-Q1 to MO-Q4 are recorded.
- The engine probe maps every novel shape. Reused: the repeatable modal (Treetop Ride's composition), moving and swapping a token, retention, supply and the adjacency count. Each new contract is assigned to one block: spawn-point placement, five passes, the target-excluding token-adjacent hero selection, a multi-hero exclusion across passes, a token as pusher, minion-to-token Replace, and a once-per-turn bullet.

Historical evidence: [9.8 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-98-mortimer-inventory-evidence).

### [x] 9.8a Implement the Zombie token and Awaken!

Outcome: a Zombie token definition (an obstacle; supply 4 per MO-Q1; p.19 reclaim; retained at every round end per MO-Q4), and Awaken! placing up to four into empty spawn points in radius or spaces adjacent to him (MO-Q2).

Acceptance:
- A configured round places 0-4 Zombies, reclaims when short and keeps every Zombie across the round end.
- A paused placement restores and replays.
- Offer and answer agree.

Unregistered until 9.8f.

Completed 2026-09-29.
- `MortimerComponentContent` declares the Zombie (four, retained at every round end, no properties) and binds the hero-token place and reclaim handlers to it.
- `MortimerCardContent.AwakenDefinition` composes Awaken! as four optional reclaim-then-place pairs, Mystic Saplings' shape, each place declaring the new `spawnPointOrAdjacent` clause.
- The clause is a shared placement clause on the printed-icon space family: unbound it narrows nothing, so no other face changes.
- `MortimerCardContent.AllDefinitions` grows block by block, so the corpus sweeps that read content classes cover him before he is dealt. `RosterMoveLeashSweepTests` read only the dealt roster (a 9.2 change no undealt hero had met), so its corpus now adds an undealt content class's own faces.
- `MortimerAwakenTests` (14) prove the offered spaces, 0-4 placements, the short-supply reclaim, retention, restore/replay, and that Awaken! is greyed only when every placement could do nothing.

Historical evidence: [9.8a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-98a-zombie-token-and-awaken-evidence).

### [~] 9.8b Implement "choose up to N times" and Master of Puppets

Outcome: the modal that repeats a bullet up to N times, with each choice made against the board the last one left, and the Ultimate raising three to five.

Acceptance: the counts hold, a bullet may repeat, stopping early is allowed, and the Ultimate changes only "up to three times" faces. Restore and replay mid-chain.

### [ ] 9.8c Implement Knife of the Living Dead and the five Red Attacks

Outcome: the before-attack and after-attack Zombie moves in radius. Also:
- Remove a Zombie adjacent to the target for +1 Attack.
- Another enemy hero in radius, adjacent to a Zombie, discards, if able, with "each enemy hero can only be targeted once".
- Retrieve if an enemy hero in radius is adjacent to a Zombie.

### [ ] 9.8d Implement the five Blue Skills

Outcome: a Zombie in range moves up to 1 space and may push a unit or token adjacent to it 1 space. The coin bullet (MO-Q3), and "Move 1 space".

### [ ] 9.8e Implement the five Green Skills

Outcome: a Zombie in range moves 1 space. Also swap with a Zombie in range, and "once per turn" Replace an enemy minion in range adjacent to two or more Zombies with a Zombie.

### [ ] 9.8f Deal Mortimer with his artwork

Outcome: he is pickable with his profile, standee, card art and Zombie token art, and the corpus, conformance and offer checks cover him.

### [ ] 9.8g Gate Mortimer on the player path

Outcome: the permanent player-path gate passes for him, with every face's journey reading what its answer changed.
