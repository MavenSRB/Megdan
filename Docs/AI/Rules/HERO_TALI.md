# Guards Digital - Tali Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Tali the Spiritcaller is the twelfth and last hero of the package arc this project inventoried,
composed, registered, dealt and player-path gated, the sixth from outside the pitch line-up milestone
8.17 approved, and the second from the **Devoted Hero pack** rather than from the base box. She is
also the first verified hero whose printed faces place **tokens**, the first to print a condition a
card asks about *itself*, and the first above one star on p.21's complexity scale. This shard owns her
content inventory, source-verification status, and any deviation or open question scoped to Tali
alone.

## Tali content ledger

Hero pack: **Devoted Hero pack** (user-approved, PLAN.md TWENTY-FIRST AMENDMENT), fifth and last
of the batch after Dodger, Silverarrow, Whisper and Ursafar.

Current status: **source-verified and capability-mapped at phase 1 (milestone 8.35z32's own
worker, 2026-09-06; composed there: the whole Green line, 5 of 19 faces), the remaining twelve
composable faces composed, the mandatory-prevention wiring closed, `CardColor.Purple` and the
runtime colour-naming capability built, the free-direction batch Move built, and Reign of Winter's
trigger built, and the whole set REGISTERED, DEALT, ARTWORKED, PICKABLE AND PLAYER-PATH GATED at
milestone 8.37 (this milestone)**. Blizzard's second sentence is the one face still deferred; it
needs an End-of-turn repeat that re-invokes a full primary action, and milestone 8.37a owns it. Everything below is read from `TaliCardContent`, `TaliComponentContent` and
`TaliEffectContent` and cross-checked against the general rulebook; the
[capability matrix](#tali-capability-matrix) maps every action onto the implemented contracts.

The stable definitions and typed bindings live in `TaliCardContent`; the token pieces live in
`TaliComponentContent`; the composed effect content lives in `TaliEffectContent`; the token
placement and short-supply machinery lives in `SyntheticTokenCapabilities`; the self-referential
discard predicate lives in `SyntheticConditionCapabilities.SourceCardInDiscardV1`; the runtime
colour-naming and friendly-hero-selection capabilities this milestone built live in
`SyntheticBorrowedActionCapabilities` (`NameColorV1`, `FriendlyHeroInPlayV1`); the free-direction
batch Move lives in `SyntheticDisplacementCapabilities` (`ChooseDirectionV1`,
`BatchDirectMoveV1`); the defended-card colour reader Ice Blast's rider needs lives in
`SyntheticRosterCapabilities.DefendedCardColorV1`. Tali's nineteen printed definitions are the
twelfth of twelve configured sets, and pass the permanent player-path gate
(`TaliPlayerPathGateTests`, content-derived offline and confirmed under Unity at her dealing).

**Single-source caveat (PACK-WIDE, RULING 27).** The second-printing rulebook does not mention
Tali, any of her nineteen card names, or any of her mechanics (tokens, totems, the colour-naming
borrow) - the silence is structural (she is a Devoted Hero pack hero, outside the base game the
local rulebook documents) rather than a search failure. The Tabletopia extraction is therefore
SOLE AUTHORITY for every value and word below; ledger question 1 stays fully open for Tali and
the whole pack.

**Bibliographic gap, recorded rather than papered over.** Unlike Ursafar's and Silverarrow's own
shards, this draft carries no Tabletopia element-ID roll call, pixel-dimension table or
back-face hash for Tali's nineteen faces: phase 1's own report (`worker-tali1/NOTES.md`) did not
record that bibliographic detail either, and this worker did not re-open the raw extraction to
gather it independently under this prep's time budget. Every printed value and every word below is
still verified - it is transcribed in `TaliCardContent.cs` and cross-read against phase 1's own
report - only the *photographic provenance table* (which scan file, which pixel rectangle) is the
open item. Whoever finalises this shard should fill that table from
`Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Tali/` before or shortly after
applying this draft; it does not block dealing her.

## Tali deck composition

Nineteen printed faces: p.6's eighteen-card deck plus the p.20 Handicap card setup returns to the
box. The colour-set shape is Wasp/Arien/Brogan/Ursafar-shaped: one Gold basic, one Silver basic,
one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, one Purple Tier IV
Ultimate, one Gold `H`.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 setup) | Ice Blast, Commune with Spirits, Winter Dagger, Cold Snap, Glacial Barrier |
| Deck, reachable only through p.15's Level Up | six Tier II, six Tier III, and Reign of Winter |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair Level Up does not take |
| Returned to the box at setup, odd player count only | Ice Shard |

## Tali printed values

`Sec Def`/`Sec Mov` are the left-ribbon secondary icons; a dash means that secondary action is
unavailable from the card. Card text applies only when the primary action is performed.

| Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Ice Blast | Gold basic | basic | 11 | 1 | 1 | Basic Attack | Attack 3 | Radius 4 | - |
| Ice Shard | Gold `H` | `H` | 10 | 1 | 1 | Basic Attack | Attack 2 | Radius 3 | - |
| Commune with Spirits | Silver basic | basic | 13 | 2 | - | Basic Skill | - | - | - |
| Winter Dagger | Red | I | 8 | 6 | 4 | Attack | Attack 2 | - | - |
| Winter Spear | Red | II | 9 | 6 | 4 | Attack | Attack 3 | - | Initiative |
| Spirit Wolf | Red | II | 9 | 3 | 4 | Ranged Attack | Attack 3 | Range 2 | Defense |
| Winter Scepter | Red | III | 9 | 7 | 4 | Attack | Attack 4 | - | Radius |
| Spirit Bear | Red | III | 9 | 4 | 4 | Ranged Attack | Attack 3 | Range 2 | Defense |
| Cold Snap | Blue | I | 9 | 5 | 3 | Skill | - | Radius 2 | - |
| Snowstorm | Blue | II | 10 | 5 | 3 | Skill | - | Radius 3 | Defense |
| Guardian Spirit | Blue | II | 10 | 5 | 3 | Skill | - | Radius 4 | Attack |
| Blizzard | Blue | III | 10 | 6 | 3 | Skill | - | Radius 3 | Movement |
| Warrior Spirit | Blue | III | 10 | 6 | 3 | Skill | - | Radius 4 | Range |
| Glacial Barrier | Green | I | 4 | 3 | 2 | Skill | - | Radius 3 | - |
| Wall of Frost | Green | II | 3 | 3 | 2 | Skill | - | Radius 3 | Initiative |
| Ancestral Totem | Green | II | 3 | 3 | 2 | Skill | - | Range 2 | Attack |
| Pack Ice | Green | III | 3 | 4 | 2 | Skill | - | Radius 3 | Initiative |
| Venerated Totem | Green | III | 3 | 4 | 2 | Skill | - | Range 2 | Attack |
| Reign of Winter | Purple | IV | - | - | - | Ultimate (no action) | - | Radius 4 | - |

Supertype follows p.9 without exception: the two Gold cards and the one Silver card are basic;
every other card is non-basic. **Tali's only basic Attack is Ice Blast's primary and her only
basic Skill is Commune with Spirits'.** She is the first verified hero to print the rulebook's `+`
value cue on an **Attack** value (Winter Dagger/Spear/Scepter's "+3 Attack if this card is not in
the discard" - p.13/p.14 illustrate the cue only on Range and Defense). **Four of her five Attacks
are melee (Range 0) and two (Spirit Wolf, Spirit Bear) print a Range icon feeding a modal bullet**
- see the exact-text table. No Tali card is a p.14 Block, and no card ever applies text while
defending. Commune with Spirits' Sec Mov column is her one dash outside the Ultimate's empty row -
the one-icon-ribbon shape phase 1's inventory verified hardest.

## Tali exact rules text

Transcribed verbatim (`TaliCardContent.cs`'s own `printedText` strings). **Bold** marks the p.17/
p.19 Active-effect keyword - Blizzard's is the only one on the whole set.

| Card | Printed text |
| --- | --- |
| Ice Blast | Target a unit adjacent to you. If the target discards a card to defend, After the attack: Another enemy hero in radius discards a card of the same color, if able. |
| Ice Shard | Target a hero adjacent to you. If the target discards a card to defend, After the attack: Another enemy hero in radius discards a card of the same color, if able. |
| Commune with Spirits | A friendly hero in play names a color; you may discard a card of that color. Perform the primary action on a card of that color in your discard. |
| Winter Dagger | Target a unit adjacent to you. +3 Attack if this card is not in the discard. |
| Winter Spear | Target a unit adjacent to you. +3 Attack if this card is not in the discard. |
| Spirit Wolf | Choose one - If this card is not in the discard, target a unit in range. Target a hero adjacent to you. |
| Winter Scepter | Target a unit adjacent to you. +3 Attack if this card is not in the discard. |
| Spirit Bear | Choose one, or both, on different targets - If this card is not in the discard, target a unit in range. Target a hero adjacent to you. |
| Cold Snap | Choose a direction. Move each enemy unit in radius 1 space in that direction, if able. |
| Snowstorm | Choose a direction. Move each enemy unit in radius 1 space in that direction, if able. |
| Guardian Spirit | Up to 1 friendly hero in radius may retrieve a discarded card. If Guardian Spirit is in your discard, retrieve it. |
| Blizzard | Choose a direction. Move each enemy unit in radius 1 space in that direction, if able. **If Blizzard is in your discard, End of turn:** May repeat once. |
| Warrior Spirit | Up to 1 friendly hero in radius may retrieve a discarded card. If Warrior Spirit is in your discard, you may retrieve a discarded card. |
| Glacial Barrier | Place an Ice token in radius. Enemy heroes have -1 Initiative for every Ice token they are adjacent to. |
| Wall of Frost | Place up to 2 Ice tokens in radius. Enemy heroes have -1 Initiative for every Ice token they are adjacent to. |
| Ancestral Totem | Place a Totem token in range. If a friendly melee minion adjacent to a totem would be defeated, remove the totem instead. |
| Pack Ice | Place up to 3 Ice tokens in radius. Enemy heroes have -1 Initiative for every Ice token they are adjacent to. |
| Venerated Totem | Place a Totem token in range. If a friendly minion adjacent to a totem would be defeated, remove the totem instead. Totem tokens are immune. |
| Reign of Winter | Each time after you defeat a minion with your basic attack, an enemy hero in radius discards a card of the color of your choice, if able. |

**Eight of the nineteen faces read their own discard state** (`TaliCardContent.ReadsItsOwnDiscardState`):
Winter Dagger, Winter Spear, Winter Scepter, Spirit Wolf, Spirit Bear, Guardian Spirit, Warrior
Spirit, Blizzard - the organising fact of the set, and the internal evidence that Commune with
Spirits' borrow is the hero rather than a corner case. **Tali prints no reminder text on any face
and no `This turn:`/`This round:` Active effect at all** - her only Active-effect keyword is
Blizzard's `End of turn:`.

## Tali upgrade candidates and item destinations

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Winter Dagger | Winter Spear (+1 Initiative) / Spirit Wolf (+1 Defense) | Winter Scepter (+1 Radius) / Spirit Bear (+1 Defense) |
| Blue | Cold Snap | Snowstorm (+1 Defense) / Guardian Spirit (+1 Attack) | Blizzard (+1 Movement) / Warrior Spirit (+1 Range) |
| Green | Glacial Barrier | Wall of Frost (+1 Initiative) / Ancestral Totem (+1 Attack) | Pack Ice (+1 Initiative) / Venerated Totem (+1 Attack) |

Twelve item icons cover all six of p.16's icons: +1 Attack 3 cards (Guardian Spirit, Ancestral
Totem, Venerated Totem), +1 Defense 3 (Spirit Wolf, Spirit Bear, Snowstorm), +1 Initiative 3
(Winter Spear, Wall of Frost, Pack Ice), +1 Movement 1 (Blizzard), +1 Range 1 (Warrior Spirit),
+1 Radius 1 (Winter Scepter). **RULING 24 means the two Totem faces (Ancestral/Venerated) never
coexist in one deck** - p.15's upgrade replaces Tier II with Tier III, so a hand is always on
exactly one of the Ice-heavy or Totem/Spirit tracks per colour at a time.

## Tali token system (phase 1, unchanged this milestone)

**She is the first verified hero who uses tokens at all.** Five faces place them - Glacial
Barrier, Wall of Frost, Pack Ice, Ancestral Totem, Venerated Totem - and `TaliComponentContent`
supplies the placed piece's properties.

**SUPPLY (RULING 44).** One physical pile of three, shared across both the Ice role and the Totem
role: three `TokenDefinition`s (Ice, Ancestral Totem, Venerated Totem) all name the same
`TokenDefinition.SupplyGroupId`, so an Ice placement short of supply may reclaim a standing totem
and vice versa - measured by `TaliTokenSystemTests.TheShortSupplyStepReachesAcrossRoles`.

**AURA (RULING 25).** "-1 Initiative for every Ice token they are adjacent to" is a flat per-token
contribution with no floor: a hero adjacent to three Ice tokens is reached by three separate -1
sources, for -3 total, not a single counted -3 modifier
(`TaliTokenSystemTests.AHeroAdjacentToThreeIceTokensIsAtMinusThree_WithNoFloorAndNothingCounting`).

**SAVE.** The totem's save is mandatory with nobody asked - a `RemovalPreventionCostKind.RemoveTheSourceToken`
price read by the new `RemovalPreventionQuery.Automatic`, not the card-priced `Offered` family a
decision opens. This milestone closed the wiring: `RemovalStageDecision.IsAutomaticallyPrevented`
is asked ahead of the offered family at both call sites in `AttackDefenseCardActionStrategyBackend`
and in `CardActionResolutionAuthority.AnswerRemovalPrevention`
(`TaliTokenSystemTests.TheMandatoryTotemSaveIsReadByRemovalStageDecisionItself_NotOnlyByTheBareQuery`).

**IMMUNITY (RULING 26).** Only Venerated Totem prints "Totem tokens are immune." - Ancestral Totem
does not, so an enemy's Clear removes an Ancestral totem and is refused against a Venerated one
(`TaliTokenSystemTests.AnEnemysClearIsRefusedAgainstAVeneratedTotemAndAllowedAgainstAnAncestralOne`).
System-level removals (p.8 end-of-round cleanup, lane-push spawn clearing) still take an immune
token - the immunity is asked only where p.19 says a player-initiated removal is asked, never by
cleanup.

## Tali capability matrix

Produced across phase 1 (8.35z32, the Green line) and this milestone (8.37, the
remaining twelve composable faces plus the mandatory-prevention wiring) against
[CARD_EFFECT_CAPABILITIES.md](../CARD_EFFECT_CAPABILITIES.md). Deferred until she is dealt
(registering now would move the pinned handler list before a hero this build does not deal names
it).

| Card / action | Composes (implemented) | New or refined (owner) |
| --- | --- | --- |
| Ice Blast - Basic Attack | Printed Attack contracts; a trivial first body (`SyntheticPlacementCapabilities.ActorSpaceV1`, unconsumed) satisfies the second-body-requires-a-first rule | **THIS MILESTONE**: the rider (`SyntheticRosterCapabilities.DefendedCardColorV1`) reads `EnclosingAttackFacts.DefenseCardInstanceId` - a fact Whisper's own dealing prep had already added for `defended-card-forced-move@1`, closing this debt smaller than phase 1 estimated - then gates a `DiscardChosenCardV1` on the defender's own discarded colour |
| Ice Shard | (parked, p.20 Handicap, box) | - |
| Commune with Spirits - Basic Skill | `borrow-primary-action@1` (Ursafar's contract, consumed as documented) over `OwnedDiscardedCards` | **THIS MILESTONE**: `friendly-hero-in-play` (`FriendlyHeroInPlayV1`, board-unit selection among the actor's own team) and `name-color` (`NameColorV1`, directed at the SELECTED hero via `chooserHeroId`, not the actor) - the borrow's colour is now a runtime output rather than a composition-time literal |
| Winter Dagger / Spear / Scepter - Attack | Printed Attack contracts; "+3 Attack if not in the discard" is a `RosterValueContributionDefinition` (`RosterValueQueryKind.SourceCardNotInDiscard`, new member), a value contribution and never a node, on Sabina's Quickdraw template | **THIS MILESTONE**: `SourceCardNotInDiscard` and the reader in `RosterSharedVocabulary` |
| Spirit Wolf / Spirit Bear - Ranged Attack | `Choose` over a `Conditional`-gated ranged bullet and a plain adjacent-hero bullet, on the multi-sequence contract Dodger's Middlefinger and Whisper's Swift Justice proved | **THIS MILESTONE**: `synthetic-source-card-in-discard` (`SyntheticConditionCapabilities.SourceCardInDiscardV1`, negatable) gates the first bullet; the gated leaf sits inside the Conditional's own then-branch, not as a Sequence sibling, so this composition does not reach the mandatory-Conditional-before-a-sibling-binding shape the Ursafar executor's `WalkToAttackTargetBinding` fix is for |
| Cold Snap / Snowstorm - Skill | `synthetic-choose-direction` (free, unconditional, all six directions) + `synthetic-batch-direct-move@1` (one mandatory node, simultaneity by capturing every candidate's pre-effect coordinate before any commits) | **THIS MILESTONE**: both capabilities, in `SyntheticDisplacementCapabilities` |
| Guardian Spirit / Warrior Spirit - Skill | `synthetic-radius-hero-target@1` (allied, self included) feeding `retrieve-card@1` (May policy) - Silverarrow's Nature's Blessing template, minus the reward; a self-gate (`SourceCardInDiscardV1`, un-negated) feeds a second, Mandatory `retrieve-card@1` restricted to the source card itself via the new `RestrictToCardParameter` | **THIS MILESTONE**: `RetrieveCardHandler`'s `RestrictToCardParameter` (bypasses `NeedsChoice` when bound to one card) |
| Blizzard | (deferred - see below) | - |
| Glacial Barrier / Wall of Frost / Pack Ice - Skill | `synthetic-place-token@1` / `synthetic-reclaim-token@1`, phase 1 | (unchanged this milestone) |
| Ancestral Totem / Venerated Totem - Skill | as above, plus the mandatory save now wired live | **THIS MILESTONE**: the wiring only - no new capability |
| Reign of Winter - Ultimate | `UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack` (new member) at `TimingCheckpoint.ActionEnded`, then the identical select/name/discard shape Ice Blast's rider composes | **THIS MILESTONE**: the trigger condition, `PerformedBasicAction.DefeatedMinionWithBasicAttack` (new fact, set in `TurnResolutionAuthority.MaterializeSteps` from whether the completed Attack's own events include a `MinionRemovalCompleted`) |

**Blizzard stays deferred.** Its first sentence (the free direction, the batch Move) is composed
and proven on Cold Snap and Snowstorm, which print nothing else. Blizzard alone carries a second,
independent clause - "If Blizzard is in your discard, End of turn: May repeat once" - asking the
primary Skill to run again from a turn-ended trigger rather than from a card-text node inside its
own resolution. `ActionDefinition`'s constructor refuses a definition carrying both a composed
`EffectRoot` and an awaited `PrintedTextComposition` simultaneously, so the two sentences cannot
ship split - the whole face stays deferred until the repeat-from-trigger shape exists.

**Delta register (this milestone).** New capabilities: `synthetic-source-card-in-discard`,
`friendly-hero-in-play`, `name-color`, `synthetic-choose-direction`, `synthetic-batch-direct-move`,
`defended-card-color`. New enum members: `CardColor.Purple`, `UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack`,
`RosterValueQueryKind.SourceCardNotInDiscard`. New facts: `PerformedBasicAction.DefeatedMinionWithBasicAttack`
(not part of `Equals`/`GetHashCode`, matching `PlacedSelf`'s own precedent), `RemovalStageDecision.IsAutomaticallyPrevented`.
`HeroPlayerPathMatrix` gained `PlayerInteractionShape.DirectionChoice` and `.ColorNamingChoice`,
plus fourteen new `CapabilityShapes` entries. Zero schema/wire movement - every new fact is
re-derived live rather than persisted, on `EffectRosterState`'s own standing principle.

## Tali hero profile

| Field | Printed |
| --- | --- |
| Hero name | Tali on the profile card; **Tali the Spiritcaller** on the dashboard |
| Complexity rating | **three stars, "Very Complex"** - the first hero above one star in the verified roster (per `TaliCardContent`'s own remarks; not independently re-read from the dashboard scan by this worker) |
| Roles, stat profile | Not independently verified by this worker - inherited as an open item alongside the bibliographic gap above; fill from the dashboard scan when the shard is finalised |
| Level track | generic 1-8 track, not hero-scoped data |

## Tali source observations

- No edition corroboration exists: the extraction is sole authority for the whole Devoted Hero
  pack, matching Ursafar's and Silverarrow's own caveat.
- **She is the first verified hero whose profile card's Tokens role icon is printed at all.** The
  icon shows only the Ice glyph even though she has two token roles (Ice and Totem) - p.22
  promises the icon corresponds to the hero's tokens, not one icon per role, and RULING 44
  explains it: there is one physical token to draw, and the placing card decides what it currently
  is. Her presentation still owes RULING 44's white token art with a runtime team colour - not
  built by this prep (Presentation, correctly deferred to the dealing phase).
- Ice Blast and Ice Shard differ in exactly one word (unit -> hero) plus the ordinary Handicap
  value reduction, the same shape as Wasp's Magnetic/Crackling Dagger pair.
- Winter Dagger, Spear and Scepter print identical text apart from their Attack value; Spirit Wolf
  and Spirit Bear differ only in Spirit Bear's "or both, on different targets" clause.
- RULING 15's turn-start sampling does not apply to her: she prints no `This turn:`/`This round:`
  Active effect at all, and the Ice aura is a token property re-measured live rather than a
  membership sampled at turn start.
- RULING 24 dissolves the "per-instance totem properties" question rather than answering it: both
  Totem definitions ship because both faces are printed, but they never coexist in one deck.

## Tali open questions

Scoped to Tali alone. See [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions)
for the pack-wide and roster-wide questions her faces settled (RULINGS 21-26, 36 and 44 among
them; see the ledger rows below).

- **Bibliographic provenance table** - see the caveat above. Needs a pass over the raw Tabletopia
  extraction this prep did not open.
- **Dashboard roles and stat-bar totals** - not independently re-read by this worker; see the
  hero-profile table above.
- **RULING 21's Purple naming, presentation only.** The engine treats Purple as a fully legal,
  inert nameable colour (RULING 21). No UI affordance for drawing a Purple swatch in the naming
  control was built or verified by any worker - the six candidates are offered as
  `EffectValue.Integer` payloads and the presentation layer's own colour-swatch mapping is what a
  later pass should confirm covers all six, Purple included, rather than falling back to a
  placeholder for the one value no other hero's content ever named before this one.
