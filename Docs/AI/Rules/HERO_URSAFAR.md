# Guards Digital - Ursafar Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Ursafar the Savage is the tenth hero this project inventoried, composed, registered, dealt and
player-path gated, and the fourth from outside the pitch line-up milestone 8.17 approved. This shard owns
his content inventory, source-verification status, and any deviation or open question scoped to Ursafar
alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here.

## Ursafar content ledger

Hero pack: **Devoted Hero pack** (user-approved, PLAN.md TWENTY-FIRST AMENDMENT), fourth of five after Dodger.

Current status: **source-verified and capability-mapped at phase 1 (milestone 8.35z31's own worker,
2026-09-06), composed at 8.35z31 (17 of 19 faces; Jaws That Bite parked at p.20's standing deferral;
Unbound Fury prints no action), and dealt, registered, gated and made pickable at milestone 8.35z50**,
which also closed the target-survived shortfall and widened the borrow's four dispatch guards.
Everything below is read from the extracted faces named under [Ursafar source faces](#ursafar-source-faces)
and cross-checked against the general rulebook; the [capability matrix](#ursafar-capability-matrix) maps
every action onto the implemented contracts.

His identity is pinned at content revision `ursafar-8-35z50-rev-1`, measured rather than predicted by
`VerifiedContentIdentityTests` on a complete unfiltered run. That is his tenth-pin entry; the nine pins
above it came back byte-identical in the same run, which is what attributes the movement to his content
and to nothing else. The revision is **re-based rather than incremented**, on the reading milestone
8.35z34 settled for Dodger and 8.35z38 and 8.35z46 repeated: his phase-one revision was minted under the
placeholder id `8.35x-ursadeal-prep`, which is a claim rather than a milestone, and renaming it re-pins
the string while re-measuring nothing, because the digest is computed over the content and not over the
revision.

His dealing registers **three new handler types** - `hero-condition@1`, `borrow-primary-action@1` and
`attack-target-survived@1` - the largest such set any dealing has needed, each proved reachable from a
showcase card that is nobody's before the gate admits the faces that name it. The first two are shared
with Tali and register here on ledger row 25's rule: a contract two heroes name is deferred only until
the FIRST of its sharers is dealt.

The stable definitions and typed bindings live in `UrsafarEffectContent` and `UrsafarCardContent`;
per-card Active-effect grants live in `UrsafarActiveEffectContent`; the borrow contract - shared with
Tali's Commune with Spirits - lives in `BorrowedPrimaryAction.cs`; the hero-condition predicate lives in
`HeroConditionQuery`/`SyntheticConditionCapabilities`; the new target-survived predicate this milestone
built lives in `SyntheticAttackTargetSurvivalCapabilities`. Ursafar's nineteen printed definitions will be
one of the ten configured sets once dealt, and are staged to pass the permanent player-path gate
(`UrsafarPlayerPathGateTests`, offline-derived and journeyed but not yet Unity-verified).

**Single-source caveat (PACK-WIDE, RULING 27).** The second-printing rulebook does not mention Ursafar,
any of his nineteen card names, or the word "enrage" in any form - the silence is structural (he is a
Devoted Hero pack hero, outside the base game the local rulebook documents) rather than a search failure.
The Tabletopia extraction is therefore SOLE AUTHORITY for every value and word below; ledger question 1
stays fully open for Ursafar and the whole pack.

**Source provenance note (2026-09-06 amendment, CORRECTED 2026-09-18).** The 2026-09-06 reading -
that the Tabletopia scans were pre-release 3rd-printing playtest material ahead of the 2nd-printing
rulebook - was wrong for every card that could be checked. On 2026-09-18 the user photographed
physical 2nd-printing cards: Angry Roar prints exactly what our scan prints (it does NOT start with
"you may" and carries NO replacement-action help text), and Unbound Fury, Wasp's Static Barrier and
Crackling Dagger and Tigerclaw's Cloak and Daggers likewise match the scan on every rules-bearing
word (punctuation differs by a comma, in either direction). The scan is therefore the printed 2nd
edition, the same edition as the rulebook. Robby's "starts with 'you may'" answer and Arty's
June-2026 help text describe a NEWER text: Tabletopia's current setups (bundle s804629, 2026-09-18)
carry the help text alongside other 3rd-printing-direction revisions on seven decks - see
`Docs/Guards of Atlantis II - Tabletopia Cards/CARD-DIFF-s804629-vs-s799373.md`. Composition follows
the scan; the designer texts are recorded as forthcoming-printing changes, not implemented.

## Ursafar source faces

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Ursafar/ursafar*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Ursafar/ursafar*__*__back.png` | 19 | 372x520 | Yes - all nineteen byte-identical (SHA-256 `8718E70C...`), unique among the 32 decks |
| Hero dashboard front | `active-hero-cards/_dashboards/Ursafar__front.png` | 1 | 850x445 | Yes - title, level track, item-slot legend and four stat bars all read |
| Hero dashboard back | `active-hero-cards/_dashboards/Ursafar__back.png` | 1 | 903x491 | **No** - blank Tabletopia placeholder shared by eleven heroes; ledger question 14's ruling makes this "no tokens or markers" rather than a gap, since no face names one |
| Hero profile card front | `active-hero-cards/_selection-cards/Ursafar__front.png` | 1 | 372x260 | Yes - name, two complexity stars, four roles read; stat bars resolve to exact totals |
| Hero profile card back | `active-hero-cards/_selection-cards/Ursafar__back.png` | 1 | 372x260 | Yes - the common game-logo back |

Element-ID roll call: 7384245, 7384250, 7384251, 7384255, 7384256, 7384259, 7384260, 7618899, 7715502,
7715503, 7721463, 7721464, 7721466, 7733766, 7733767, **7734786** (indexed under `rsafar11` - a real
extraction misspelling, already reconciled by the curation), 7762573, 8436588, 8436589.

## Ursafar deck composition

Nineteen printed faces: p.6's eighteen-card deck plus the p.20 Handicap card setup returns to the box.
The colour-set shape is Wasp/Arien/Brogan-shaped exactly: one Gold basic, one Silver basic, one Tier I
with two Tier II and two Tier III in each of Red, Blue and Green, one Purple Tier IV Ultimate, one Gold `H`.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 setup) | Claws That Catch, Angry Roar, Prey Drive, Prowling Brute, Sniff Out |
| Deck, reachable only through p.15's Level Up | six Tier II, six Tier III, and Unbound Fury |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair Level Up does not take |
| Returned to the box at setup, odd player count only | Jaws That Bite |

## Ursafar printed values

`Sec Def`/`Sec Mov` are the left-ribbon secondary icons; a dash means that secondary action is
unavailable from the card. Card text applies only when the primary action is performed.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ursafar` | 7384259 | Claws That Catch | Gold `D` | basic | 12 | 2 | 1 | Basic Attack | Attack 4 | - | - |
| `ursafar19` | 7384260 | Jaws That Bite | Gold `D` | `H` | 11 | 1 | 1 | Basic Attack | Attack 3 | - | - |
| `ursafar2` | 7384250 | Angry Roar | Silver `S` | basic | 7 | 4 | - | Basic Skill | - | - | - |
| `ursafar3` | 7721463 | Prey Drive | Red `R` | I | 8 | 7 | 3 | Attack | Attack 5 | Radius 3 | - |
| `ursafar4` | 7721464 | Prey Abundance | Red `R` | II | 9 | 7 | 3 | Attack | Attack 6 | Radius 4 | Defense |
| `ursafar5` | 8436588 | Rip | Red `R` | II | 9 | 6 | 3 | Attack | Attack 5 | - | Initiative |
| `ursafar6` | 7721466 | Feeding Frenzy | Red `R` | III | 9 | 8 | 3 | Attack | Attack 6 | Radius 4 | Defense |
| `ursafar7` | 8436589 | Tear | Red `R` | III | 9 | 7 | 3 | Attack | Attack 5 | - | Initiative |
| `ursafar8` | 7733766 | Prowling Brute | Blue `B` | I | 9 | 5 | - | **Movement** | Movement 1 | - | - |
| `ursafar9` | 7733767 | Rampaging Beast | Blue `B` | II | 10 | 6 | - | **Movement** | Movement 1 | - | Defense |
| `ursafar10` | 7384245 | Cold Ire | Blue `B` | II | 10 | 6 | - | **Movement** | Movement **1+** | - | Attack |
| `ursafar11` | 7734786 | Unstoppable Force | Blue `B` | III | 10 | 6 | - | **Movement** | Movement 1 | - | Range |
| `ursafar12` | 7762573 | Eyes of Flame | Blue `B` | III | 10 | 6 | - | **Movement** | Movement **1+** | - | Radius |
| `ursafar13` | 7384256 | Sniff Out | Green `G` | I | 4 | 2 | 2 | Skill - Ranged | - | Range 2 | - |
| `ursafar14` | 7384255 | Eyes on the Prey | Green `G` | II | 3 | 3 | 2 | Skill - Ranged | - | Range 3 | Attack |
| `ursafar15` | 7715502 | Instinctive Reaction | Green `G` | II | 3 | 3 | 2 | Skill | - | - | Initiative |
| `ursafar16` | 7384251 | Apex Predator | Green `G` | III | 3 | 3 | 2 | Skill - Ranged | - | Range 3 | Attack |
| `ursafar17` | 7715503 | Evolutionary Response | Green `G` | III | 3 | 3 | 2 | Skill | - | - | Movement |
| `ursafar18` | 7618899 | Unbound Fury | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

Supertype follows p.9 without exception: the two Gold cards and the one Silver card are basic; every
other card is non-basic. **Ursafar's only basic Skill is Angry Roar's primary and his only basic Attack
is Claws That Catch's** (Jaws That Bite's when handicapped). **The Blue line is this roster's first
verified primary Movement action** - all five Blue cards print a `MOVEMENT` band; consistently, no Blue
card prints a secondary Movement icon. No Ursafar card is a p.14 Block, and no card ever applies text
while defending: every non-Ultimate card carries a secondary Defense value.

## Ursafar exact rules text

Transcribed verbatim; printed line breaks joined with a single space. **Bold** marks the p.17/p.19
Active-effect keyword. `[Life]`/`[Movement]` mark inline printed icons.

| Card | Printed text |
| --- | --- |
| Claws That Catch | Before the attack: If enraged, you may move 1 space to a space adjacent to an enemy hero; target a unit adjacent to you. **This round:** You are enraged. |
| Jaws That Bite | Before the attack: If enraged, you may move 1 space to a space adjacent to an enemy hero; target a hero adjacent to you. **This round:** You are enraged. |
| Angry Roar | If enraged, perform the primary action on one of your active cards with an active effect. **This round:** You are enraged. |
| Prey Drive | Target a unit adjacent to you. After the attack: If enraged, and the target was not removed, remove up to 1 enemy minion in radius. **This round:** You are enraged. |
| Prey Abundance | Target a unit adjacent to you. After the attack: If enraged, and the target was not removed, remove up to 1 enemy minion in radius. **This round:** You are enraged. |
| Rip | Target a unit adjacent to you. After the attack: If enraged, gain 1 coin. **This round:** You are enraged. |
| Feeding Frenzy | Target a unit adjacent to you. After the attack: If enraged, and the target was not removed, remove up to 2 enemy minions in radius. **This round:** You are enraged. |
| Tear | Target a unit adjacent to you. After the attack: If enraged, gain 2 coins; if you defeated a hero, that hero spends 1 additional `[Life]` Life counter. **This round:** You are enraged. |
| Prowling Brute | If enraged, after movement, you may swap with a unit or a token adjacent to you. **This round:** You are enraged. |
| Rampaging Beast | If enraged, after movement, you may swap with a unit or a token adjacent to you; if you do, move up to 1 additional space. **This round:** You are enraged. |
| Cold Ire | If enraged, gain +1 `[Movement]` Movement; **This round:** You are enraged. |
| Unstoppable Force | If enraged, after movement, you may swap with a unit or a token adjacent to you; if you do, move up to 2 additional spaces. **This round:** You are enraged. |
| Eyes of Flame | If enraged, gain +2 `[Movement]` Movement. **This round:** You are enraged. |
| Sniff Out | If enraged, an enemy hero in range discards a card, if able. |
| Eyes on the Prey | If enraged, an enemy hero in range discards a card, if able. |
| Instinctive Reaction | If enraged, **choose one** - Perform the primary action on one of your discarded cards. / You may retrieve a discarded card. |
| Apex Predator | If enraged, an enemy hero in range discards a card, or is defeated. |
| Evolutionary Response | If enraged, **choose one, or both** - Perform the primary action on one of your discarded cards. / You may retrieve a discarded card. |
| Unbound Fury | You are always enraged and all your resolved cards count as active. |

**Thirteen of the nineteen faces both read and grant `enraged`** (both Gold, the Silver, all five Red,
all five Blue); the five Green cards read it without granting it, and the Ultimate replaces the whole
mechanism with a permanent one. `enraged` is defined entirely by these faces - it appears nowhere in the
rulebook.

## Ursafar upgrade candidates and item destinations

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Prey Drive | Prey Abundance (+1 Defense) / Rip (+1 Initiative) | Feeding Frenzy (+1 Defense) / Tear (+1 Initiative) |
| Blue | Prowling Brute | Rampaging Beast (+1 Defense) / Cold Ire (+1 Attack) | Unstoppable Force (+1 Range) / Eyes of Flame (+1 Radius) |
| Green | Sniff Out | Eyes on the Prey (+1 Attack) / Instinctive Reaction (+1 Initiative) | Apex Predator (+1 Attack) / Evolutionary Response (+1 Movement) |

Twelve item icons cover all six of p.16's icons (identical distribution to Brogan's). Item reach:
+1 Attack 7 cards, +1 Defense 18, +1 Initiative 18, +1 Movement 17 (five Blue primaries plus every
secondary Movement icon; RULING 40 makes a Movement item raise the primary too), +1 Range 3, +1 Radius 3.
Unbound Fury prints neither Range nor Radius; ledger question 12 has no Ursafar instance.

## Ursafar capability matrix

Produced across phase 1 (8.35z31) and the dealing (8.35z50) against
[CARD_EFFECT_CAPABILITIES.md](../CARD_EFFECT_CAPABILITIES.md). Registered at 8.35z50; until then the
handlers sat in their files' `DeferredHandlers` lists, because the registered list is serialized into
every snapshot and registering before a dealt card can start a contract moves pinned bytes while the
content has not moved.

| Card / action | Composes (implemented) | New or refined (owner) |
| --- | --- | --- |
| Claws That Catch - Basic Attack | Printed Attack contracts; p.19a's step-1 move-then-target trio (Brogan's Mad Dash shape, reused) | Grant at step 7 (after the mandatory target step); RULING 46/47 govern the move-then-target order and its non-Movement-action status. **Composed at 8.35z31** |
| Angry Roar - Basic Skill | `borrow-primary-action@1` over `OwnedActiveCardsWithAnActiveEffect` | **THE BORROW CONTRACT** (`BorrowedPrimaryAction.cs`), shared with Tali. Milestone 8.36f3 completed the `TextGranted` continuation: the selected printed primary now runs through its ordinary Movement, Attack or Skill authority while the borrowing Skill is suspended, then resumes without changing the borrowed card's state |
| Prey Drive / Prey Abundance / Feeding Frenzy - Attack | `choose-target-set@1` + `synthetic-remove@2` (Sabina's `CreateFollowUpRemoval` shape, reused verbatim) over `synthetic-card-radius@1` reach | **THIS MILESTONE**: `attack-target-survived@1` (`SyntheticAttackTargetSurvivalCapabilities`) - reads `EnclosingAttackFacts.TargetWasRemoved`, itself read live off board occupancy at `CardActionResolutionAuthority.EnclosingAttackOf`, closing phase 1's named shortfall with zero schema movement |
| Rip / Tear - Attack | `gain-resource@1`; Tear also declares `RuleModificationKind.AdditionalDefeatLifeCost` (new, payload-free) | RULING 35: Tier + 1 computed once, spent once, at the defeat |
| Prowling Brute / Rampaging Beast / Unstoppable Force - Movement | `synthetic-choose-target` (Blue swap target - **this milestone found it unclassified in `HeroPlayerPathMatrix` and added it**) + `synthetic-swap-actor@1` + `synthetic-direct-move@1` | `CardTextTiming.AfterTheMovement` (new enum member, already declared pre-8.35z31; first consumed here) |
| Cold Ire / Eyes of Flame - Movement | `MovementValueQuery` raise via `UrsafarActiveEffectContent.MovementRaiseFor` | RULING 40: printed `1+` is a plus-cue, not a new value shape |
| Sniff Out / Eyes on the Prey / Apex Predator - Skill, Ranged | `synthetic-range-hero-target@1` + discard machinery; Apex Predator escalates to `synthetic-defeat-hero` | Ledger question 13's fifth verified printing |
| Instinctive Reaction / Evolutionary Response - Skill | `Choose` (1..1 / 1..2 - free, no new vocabulary) over the borrow bullet and `retrieve-card@1` | Roster's first `choose one, or both` |
| Unbound Fury - Ultimate | `HeroConditionQuery` standing declaration + `BorrowedPrimaryActionDeclaration.ResolvedCardsCountAsActive` | Prints no action; widens Angry Roar's pool post-Ultimate (RULING 43) |

**Delta register (this milestone).** `EnclosingAttackFacts.TargetWasRemoved` (new field, re-derived live,
zero schema movement); `CardActionResolutionAuthority.EnclosingAttackOf` widened to take a board and
compute it; `attack-target-survived@1` (new deferred capability); the four borrow-dispatch guards
(`AttackActionResolutionState`, `AttackDefenseCardActionStrategyBackend`, `MovementCardActionStrategyBackend`,
`CardActionResolutionAuthority.PrepareStart`) widened to admit `ActionInvocationContext.TextGranted`;
`HeroPlayerPathMatrix` gained `PlayerInteractionShape.AfterMovementTextPause`, a `SecondCardTextRoot` walk
(Claws That Catch and Tear are the first faces to declare one), and four new `CapabilityShapes` entries
(`synthetic-choose-target`, `synthetic-card-radius`, `attack-target-survived`, `borrow-primary-action`).

**What phase 1's costed design turned out to need revising.** The documented four-piece design for the
target-survived fact assumed persisting a new field on `PendingAttackResolution` (a snapshot-serialized
type), which would have moved the schema. Verification against the current engine found a schema-safe
alternative already load-bearing elsewhere in this codebase (`EffectRosterState`'s own stated principle -
"re-derives against the same state on answer, replay and restore without adding a snapshot field"): read
board occupancy live, at the exact moment the fact is asked, rather than storing it. Three pieces built
instead of four; see `UrsafarEffectContent.RemovalGatedOnTargetSurvival`'s doc comment for the full account.

**Borrow dispatch completed at 8.36f3.** `BorrowedActionQuery` reads the selected card from the
completed effect frame; `CardActionResolutionAuthority` suspends the borrowing Skill, constructs the
borrowed card's exact printed-primary `TextGranted` offer, and dispatches it through the ordinary
Movement, Attack or Skill authority. Completion restores the parent frame, including nested borrowed
Skills. The borrowed card remains in its original state, and schema 41 preserves the suspended-parent
stack through restore/replay.

## Ursafar hero profile

| Field | Printed |
| --- | --- |
| Hero name | Ursafar on the profile card; **Ursafar the Savage** on the dashboard |
| Complexity rating | **two stars - "Complex"**, the first two-star hero in the verified roster |
| Roles | Durable, Pusher, Farming, **Melee** (first of its kind in the verified roster) |
| Stat profile | Attack 6, Initiative 5, Defense 6, Movement 5 (totals; conditional split not resolvable - U-09, recorded as approximate on purpose, like W-02) |
| Level track | generic 1-8 track, not hero-scoped data |

## Ursafar source observations

- No edition corroboration exists (U-08): the extraction is sole authority for the whole Devoted Hero pack.
- The removal clauses grant no coins (p.19's Remove entry is explicit); Ursafar's Farming role rests on
  Rip and Tear alone.
- A swap is not movement (p.19); the "move up to N additional space(s)" after a Blue swap is a distinct
  displacement.
- Fast Travel silences a Blue card entirely (p.11), forgoing both the swap clause and the enrage grant.
- Claws That Catch and Jaws That Bite differ in exactly one word (unit -> hero), the same shape as
  Wasp's Magnetic/Crackling Dagger pair.
- Prey Drive and Prey Abundance print identical card text, the same shape as Wasp's Shock/Electrocute.
- No Ursafar card places or removes a token or marker of his own; the Blue swap clauses act on a token
  or unit already on the board.

## Ursafar open questions

Scoped to Ursafar alone. See [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions) for the
pack-wide and roster-wide questions this hero's faces settled (U-01 through U-07 became Rulings 2, 3, 35,
36, 40, 43 and the designer-channel corroborations; see the ledger rows below).

- **U-08 - edition.** No located source evidence for Ursafar at all; ledger question 1 stays open for
  the whole Devoted Hero pack. See "Single-source caveat" above.
- **U-09 - conditional stat-bar split does not separate.** p.21 selection metadata no rule consumes,
  recorded as approximate on purpose (like W-02), needs no answer before implementation.
- **Two printing-currency questions - CLOSED 2026-09-18 by the physical card, not by an answer.**
  (a) The printed 2nd-edition Angry Roar does NOT start with "you may"; Robby's answer describes a
  text that is not this edition's. (b) The printed Silver carries NO "(You may choose to perform a
  replacement action instead)" help text; that sentence exists only on the newer Tabletopia text
  (bundle s804629), which is where the 3rd printing is heading. Composition follows the scan as
  extracted, which IS the print; nothing here admits an unprinted replacement-action allowance. The
  reworded forward-looking question (are those newer texts the 3rd-printing changes?) stays in
  ASK-ARTY as a note, not as an open item of this inventory.
