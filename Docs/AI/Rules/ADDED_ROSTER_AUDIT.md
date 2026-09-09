# Added-roster card audit — 8.36f3

Status: complete. Owner: milestone 8.36f3. Authority and authorization: [private-game corrections](../Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md); rule precedence stays in [RULES_LEDGER.md](../RULES_LEDGER.md).

## Scope and closing checks

Five added playable sets contain 90 dealt faces: Dodger, Misa, Silverarrow, Ursafar and Whisper. Each also inventories one boxed Handicap; none is dealt in the supported even-player formats. Tali is composed but undealt and remains outside this batch. No new heroes are authorized.

Each row requires source/definition branch review, a source-linked authoritative scenario for its distinct behavior, and actual player-input coverage for its interaction shapes. Existing green evidence is reusable only where the tested code and relevant dependencies are unchanged. A matrix naming a skipped journey is not a passing player-path check. The final evidence and limits belong in the milestone commit; this table records conclusions, not repeated run totals.

In the tables below, **source-derived gate row** means the named permanent hero gate re-derived that
face's action/effect branches from the verified declarations and resolved every source-linked EditMode
scenario named on the row. **Active mapped journeys** means the gate resolved each row's interaction
shapes to real, non-Explicit player-input fixtures and the complete PlayMode run executed them. The
full gate finished 448/448 effectively green after the two accounting-only failures were corrected and
their 20 cases rerun; there are no remaining PlayMode skips.

## Findings and dispositions

| Finding | Current evidence | Required closure |
| --- | --- | --- |
| F-CARD-01: multi-target repeat count and history | Confirmed: Blood Frenzy stopped after two attacks rather than six; after correcting its count, the third instance re-offered the first target. Focused uninterrupted/serialized-pause checks and the six-click player journey with Undo/restore now pass. | Closed; schema-41 compatibility and complete regressions are green. |
| F-CARD-02: Swift Justice option scope | Confirmed: the adjacent option incorrectly started the ranged option's forced-movement follow-up. Per-bullet scope now passes both actual UI options and the adjacent-hero rules test, including restore. The empty-discard predicate already existed. | Closed; source description and affected regressions are reconciled. |
| F-CARD-03: parked player journeys | Confirmed fixture debt: the prior suite carried 22 Explicit skips across Silverarrow, Ursafar and Whisper. All were corrected and re-enabled; the five Ursafar setups now establish valid preconditions instead of reusing spent cards. The complete PlayMode gate reports 448 active cases, zero skips, with its only two accounting failures corrected and their 20 cases rerun green. | Closed. Every mapped journey is active and every journey is accounted for by a row. |
| F-CARD-04: progressed configuration order | Confirmed: Undo reordered the upgraded initial card-instance array while rebuilding the dealt/deck split. Preserving the captured flat order passes the focused regression and actual Blood Frenzy Undo/restore journey. | Closed; configuration, replay and complete-suite regressions are green. |
| F-CARD-05: swap subject includes actor | Confirmed: Cruel Twist offered Whisper herself; choosing her stopped at the swap rather than reaching movement. The shared union now excludes the actor. Rules-layer candidate/rejection checks and the actual swap-then-move journey pass. | Closed; shared-capability consumers and player path are green. |
| F-CARD-06: counted move offer | Confirmed: Sanguine Path's count did not publish read-only anticipated outputs, so movement feasibility treated an unbound distance as zero. The publisher correction passes positive, zero-budget and unreachable player paths plus affected count/feasibility checks. | Closed; affected and complete-suite regressions are green. |
| F-CARD-07: enrage lifetime and Ultimate condition | Confirmed: ordinary action completion cancelled the source effect; movement's end checkpoint preceded its after-movement text; condition/borrow queries omitted derived unlocked Ultimate sources. Corrections now pass actual Claws That Catch, Prey Drive, Sniff Out and Apex Predator journeys. | Closed; lifetime, ordering, cancellation, Ultimate-source and restore regressions are green. |
| F-CARD-08: borrowed primary never starts | Confirmed and corrected: the authority now suspends the borrowing Skill, starts the selected printed primary under `TextGranted`, and resumes its parent after Movement, Attack or Skill completion. The Attack continuation reconstitutes only the already-authorized pending borrowed primary rather than querying ordinary discard-ineligible offers. Active player journeys cover all three kinds, nested Skill parents, source preservation, Undo and restore; the Ursafar fixture passes 15/15. | Closed; schema-41 and complete shared-resolver regressions are green. |

## Dodger

Source owner: [Dodger rules](HERO_DODGER.md). Declarations: `DodgerCardContent`; permanent source/player gate: `DodgerPlayerPathGateTests`. Boxed Handicap: Rusty Razor (not instantiated; preserved).

| Card | Authoritative branch evidence | Player-input evidence | Conclusion |
| --- | --- | --- | --- |
| Dread Razor | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Death Trap | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Littlefinger of Death | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Finger of Death | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Burning Skull | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Middlefinger of Death | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Blazing Skull | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Shield of Decay | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Vampiric Shield | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Weakness | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Aegis of Doom | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Enfeeblement | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Dark Ritual | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Darker Ritual | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Necromancy | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Darkest Ritual | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Necromastery | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Tide of Darkness | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |

## Misa

Source owner: [Misa rules](HERO_MISA.md). Declarations: `MisaCardContent`; permanent source/player gate: `MisaPlayerPathGateTests`. Boxed Handicap: Leaf on the Wind (not instantiated; preserved).

| Card | Authoritative branch evidence | Player-input evidence | Conclusion |
| --- | --- | --- | --- |
| Watch How I Soar | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Swoop In | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Challenge Accepted | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Matter of Honor | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Power Shot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Worthy Opponent | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Thunder Shot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Sudden Breeze | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Gust of Wind | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Dash and Slash | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Crushing Squall | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Death from Above | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Focus | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Discipline | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Living Tornado | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Mastery | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Storm Spirit | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Power Overwhelming | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |

## Silverarrow

Source owner: [Silverarrow rules](HERO_SILVERARROW.md). Declarations: `SilverarrowCardContent`; permanent source/player gate: `SilverarrowPlayerPathGateTests`. Boxed Handicap: Keep the Distance (not instantiated; preserved).

| Card | Authoritative branch evidence | Player-input evidence | Conclusion |
| --- | --- | --- | --- |
| Shoot and Scoot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Trailblazer | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Snap Shot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Opportunity Shot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Long Shot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Clear Shot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Rain of Arrows | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Grasping Roots | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Entangling Vines | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Warning Shot | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Grappling Branches | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Treetop Sentinel | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Disorient | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Divert Attention | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Fae Healing | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Lead Astray | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Nature's Blessing | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Wild Hunt | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |

## Ursafar

Source owner: [Ursafar rules](HERO_URSAFAR.md). Declarations: `UrsafarCardContent`; permanent source/player gate: `UrsafarPlayerPathGateTests`. Boxed Handicap: Jaws That Bite (not instantiated; preserved).

| Card | Authoritative branch evidence | Player-input evidence | Conclusion |
| --- | --- | --- | --- |
| Claws That Catch | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Angry Roar | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-08 closed |
| Prey Drive | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Prey Abundance | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Rip | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Feeding Frenzy | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Tear | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Prowling Brute | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Rampaging Beast | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Cold Ire | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Unstoppable Force | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Eyes of Flame | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Sniff Out | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Eyes on the Prey | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Instinctive Reaction | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-08 closed |
| Apex Predator | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-07 closed |
| Evolutionary Response | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-08 closed |
| Unbound Fury | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |

## Whisper

Source owner: [Whisper rules](HERO_WHISPER.md). Declarations: `WhisperCardContent`; permanent source/player gate: `WhisperPlayerPathGateTests`. Boxed Handicap: Vile Accusation (not instantiated; preserved).

| Card | Authoritative branch evidence | Player-input evidence | Conclusion |
| --- | --- | --- | --- |
| Swift Justice | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-02 closed |
| Death Seeker | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Crimson Trail | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Sanguine Path | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-06 closed |
| Blood Fury | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Blood Pilgrimage | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-06 closed |
| Blood Frenzy | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-01/F-CARD-04 closed |
| Seeds of Fear | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Sprouts of Panic | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Lesser Evil | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Blooming Nightmare | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Greater Good | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Shadow Step | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Shadow Walk | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Cruel Twist | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | F-CARD-05 closed |
| Creeping Shadow | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Sealed Fate | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |
| Grim Reaper | Source-derived gate row; named authoritative scenarios resolved | Active mapped journeys | Reviewed — pass |

## Preserved source and rules questions

The audit introduced no new interpretation, rebalance, or content. Existing source limitations and open
questions remain where their hero owners and `RULES_LEDGER.md` record them; in particular, the five
boxed Handicap faces remain returned to the box in every supported even-player format, and their
deferred text was not treated as dealt-card coverage. Ursafar's single-extraction authority limitation
and unresolved printed stat split remain explicit in `HERO_URSAFAR.md`. Tali remains composed but
undealt and was not silently pulled into this five-hero batch.
