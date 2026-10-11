# PvE implementation

Canonical owner for what the separate PvE executable implements and how it reuses the existing game. Approved rules live in [PvE design](PVE_DESIGN.md); delivery and authorization live in [the PvE plan](../Roadmap/PVE.md) and the [brief](../SESSION_BRIEF.md). Return to [project index](../PROJECT_CONTEXT.md).

## Isolation (PVE-D01)

PvE code lives outside `Assets/GuardsDigital`:

- `Assets/GuardsPve/Rules` - `GuardsDigital.Pve`, engine-free (no Unity reference): content model and validation, seeded plan derivation, enemy card factory and encounter composer.
- `Assets/GuardsPve/Presentation` - `GuardsDigital.Pve.Presentation`: content loading (`JsonUtility`), the launcher/setup screen and the launch smoke.
- `Assets/GuardsPve/Editor` - `GuardsDigital.Pve.EditorTools`: the PvE build entry only.
- `Assets/GuardsPve/Scenes/PveLauncher.unity` - the PvE executable's first scene. It is not in Build Settings.
- `Assets/GuardsPve/Data~` - `enemies.json` and `encounter.json`. The `~` folder is not imported; the Editor reads it in place and the PvE build copies it to `PveData/` beside the executable.

Every PvE assembly sets `autoReferenced: false`. Every runtime and test PvE assembly carries the define constraint `UNITY_EDITOR || GUARDS_PVE`, which only the PvE build supplies, so the existing player never contains a PvE assembly. No existing assembly references PvE. PvE adds nothing under `Resources/` or `StreamingAssets/` and declares no `RuntimeInitializeOnLoadMethod`, because those would ship or run in the existing player.

The PvE executable reuses the existing match scene unchanged: its explicit scene list is the PvE launcher followed by `Assets/Scenes/Milestone1_2_HexGrid.unity`.

## Additive shared hooks

Each hook is unused by every existing composition, which therefore behaves as before. The seeded existing-mode snapshot golden pins that.

| Hook | Where | Behavior |
| --- | --- | --- |
| Validated configuration from hands | `MatchConfiguration.CreateValidatedFromHands` | Composes a configuration from caller-built hands with the same initial-card, item-slot and effect validation every dealt configuration receives. |
| External match composition | `MatchSceneCompositionRoot.NextComposedExternalMatch`, `ExternalMatchComposition` | A one-shot whole initial state (configuration and map included) that replaces the dealt defaults. Heroes the initial state leaves off the board are not drawn at load, so the first refresh shows no false defeat. A callback receives the composed runtime. |
| Further seat colours | `PlayerAccent` | A side's fourth and later seats take colours from that side's own further palette. Each side's first three seats keep their existing colours. |
| Mode rules (W-022) | `MatchModeRules` on `MatchSessionInitialState`; bound by `MatchAggregate` on every rebuild and in its shared deserialize step (snapshot restore, rollback, checkpoint Undo); passed to `AuthoritativeSnapshotSerializer.Deserialize` | Never serialized. A match without them is unchanged. |
| End-of-round steps | `RoundResolutionAuthority.BindModeRules` | Lane Push, Level Up and Pity Coins report not applicable. The Minion Battle step still runs (so pre-battle triggers fire, PVE-D23) and is inert with no Red minions. |
| No coins | `HeroCoinLedger.GainsAddNothing`, bound through the attack backend | Every Defeat award and coin grant is accepted and adds nothing; a card grant still reports done (PVE-D22). |
| Respawn spaces | `HeroRespawnRules.CreateChoice` reference spaces via `CardActionResolutionAuthority.RespawnReferenceSpacesFor` | Blue respawns onto empty placement spaces, otherwise the nearest empty ones (PVE-D18). |
| Round-start placement | `ModeRoundStartPlacementRules`, one transition per scheduled round (`mode-round-start-placement/round-N`) | Begun with the ordinary Begin System Transition command and answered with the ordinary answer command. Only at turn 1 of its round before anyone commits. Designated spaces fall back to the nearest empty spaces, pausing only on ties. |
| Granted level | `HeroLevelUpStepRules` for `mode-grant-level/<hero>`; outcome `LevelGranted` | Raises one level without spending, then the ordinary Upgrade a Card answer. Only before that hero's first commitment of a round (PVE-D14). |
| Discard or defeat | `ModeDiscardPolicy` in the chosen-card and owned-card discard handlers, reading the bound rules through `EffectRosterState.ModeRules` | A Red hero discards only a Defense card; with none, the step publishes the defeated-hero fact and the ordinary Defeat follows (PVE-D19). |
| Refusal results | `MatchCommandResult.Refused`, `MatchSessionOperationResult.Refused` | Let the PvE session refuse a command in the session's own terms. |
| Session decorator (W-023) | `ExternalMatchComposition.SessionDecorator`, passed by the root to `ConfiguredMatchSceneRuntime` | Wraps the Director session, so the HUD, Undo and Restart all talk to the encounter session. Only an external composition sets one. |
| Director player side (W-026) | `ExternalMatchComposition.DirectorPlayerSide`, passed by the root to `ConfiguredMatchSceneRuntime`, which sets `DirectorHandsPresenter.PlayerSide`, `RoundActionPresenter.DirectorPlayerSide`, `DecisionWindowPresenter.DirectorPlayerSide` and `BoardInteractionUnityView.PerspectiveSide`; shared reading in `DirectorPlayerSide` | The Director plays one side. The tray follows only that side's heroes and cannot be pointed at another; while the other side owns the standing moment there is no turn control and no decision window; board outlines read from that side. Only an external composition names a side; a seat ignores it. |

## Encounter composition (W-021)

`PveEncounterComposer` builds an ordinary `MatchSessionInitialState` through public APIs; the existing-mode setup helpers that require 2v2/3v3 are not used.

- **Participants.** Two teams with one player per hero. The playable launcher requires exactly three chosen Blue heroes, holding their verified printed sets, under PVE-D27. `PveEncounterComposer.ValidatePlayerSetup` owns that limit; generic composition still supports focused test fixtures. All five placement spaces remain available. Each enemy is a Red hero (`pve-enemy-NN`) with the rulebook dashboard.
- **Enemy cards.** Synthetic-fixture card definitions from `PveCardFactory`:
  - an orange card is one printed primary Attack (adjacent or Ranged) and one printed secondary Movement, neither replaceable;
  - an idle card prints no action;
  - a purple card prints a reaction Defense discarded on use;
  - each copy of an orange card is its own definition (`pve-<type>-<key>`, then `-2`, `-3` for later copies) with identical content, because the existing HUD's card inspection holds every face of a hero once (W-026: two copies sharing one face made inspecting that enemy throw).
- **Creeps.** Blue standard minions on the encounter's creep spaces.
- **Tie Breaker.** Derived from the setup seed.
- **Life and Wave counters.** Exist because the engine counts them, and are never part of a PvE ending.
- **Starting board.** Round-1 enemies stand on their spawns. The heroes and later enemies are participants off the board until the round-start placement puts them on it.
- **Mode rules.** `PveEncounterComposer.ModeRules` states the encounter's rules for the engine (see the hooks above).

`PveSeed` derives plan choices with an explicit SplitMix64 mix of seed, round and enemy identity, so Mono and .NET agree and nothing is stored.

## Encounter rules layer (W-022)

- **`PveEnemyController`** reads the match and names one command at a time. It owns no state, so restart, Undo and replay need nothing from it. It covers:
  - round-start placement and enemy commitments from `PvePlans` (seed, round and enemy; a removed planned card is replaced per PVE-D21);
  - each enemy's first applicable option, holding otherwise;
  - movement to the destination nearest its targets within the stop distance, cheapest first;
  - attack targets in the card's listed order;
  - the weakest sufficient purple Defense, otherwise declining;
  - declining every enemy respawn, and advancing the end of round.
- **Ties.** Equal enemy targets, destinations, blocked spawns and enemy Initiative ties become a `PvePrompt` the player answers. An enemy Initiative tie in which at most one tied enemy stands is settled without asking, because off-board enemies do nothing and only one order is observable.
- **Fallback.** Any other enemy decision takes that enemy's weakest purple card where a card is asked for, otherwise the first command its seat may submit, and is noted `PVE_FALLBACK`.
- **Blue decisions come first.** A standing decision a Blue hero owns - a Removal prevention during an enemy's turn, say - holds the enemy's turn until the player answers it (W-026: the controller had acted past it, the engine refused, and the encounter stalled).
- **`PveEncounterSession`** wraps the Director session. It refuses:
  - commands for enemies, and commitments while a transition is paused;
  - anything except the prompt's answers while a prompt stands;
  - anything while enemies still owe steps;
  - anything after the loss.

  `Step` and `RunAutomatic` take the automatic commands. Undo walks back over automatic commands to the last player command. Restart clears that record. Snapshot restore is refused.
- **Loss.** The encounter is lost when no Blue creep stands, or when the engine ends the match.

## Presentation (W-023)

- **Session and overlay.** The launcher's external composition wraps the Director session in `PveEncounterSession` and attaches `PveEncounterView` to the composed runtime. The existing HUD drives Blue heroes as in Director mode, with Blue as its player side (W-026): it never follows, offers or outlines an enemy as the player's own. Its commands pass through the session's guard.
- **`PveEncounterView`** draws its own overlay panel:
  - round, turn, seed and what happened last;
  - placement buttons for the round-1 placement, and `+1 level` buttons in the round-start window (the upgrade choice itself appears in the HUD's existing level-up region);
  - answer buttons for enemy tie prompts, under an `ENEMY DECISION - you choose for ...` banner naming the enemy, whose plan strip is edged while the prompt stands (W-026);
  - a loss banner with Restart (same seed) and New setup.
- **Plans beside tokens.** A plan strip beside each standing enemy's token shows its four planned Initiatives and actions, with the current turn bracketed. Hovering it lists every orange and purple card with its numbers.
- **Pacing.** Enemy steps run one at a time at `StepSeconds` intervals, each followed by a refresh. From W-026 a step also waits, for at most three seconds, until `ConfiguredMatchSceneRuntime.IsClockInputReady` reports the board's animations and attack spotlight settled; the launch smoke turns the wait off.
- **Rails and tokens (W-027).** The overlay replaces the three-seat enemy rail with a scrollable left rail using `HeroOverviewsUnityView`: the same hero plaque, four card slots, discard area, card inspection and full-card hover. Its optional `TurnSlotsOverride` supplies all four public planned cards at round start; past occupied slots use actual resolved cards. Each plaque adds standing/acting/defeated/spawn status and remaining Defense count; no item/coin/reward display. Board and rail hover share identity highlighting. Melee/Ranged/Heavy artwork is selected by typed `tokenType` content, saved by the card editor, and applied through the optional `PrimitiveHeroView.SetTokenFaceOverride` presentation hook. Enemy identity and rules stay those of heroes. A Blue rail of more than three heroes is scaled to fit, from outside the HUD.
- **Enemy card editor.** The setup screen's enemy card editor (`PveCardEditor`, user request 2026-10-10) edits each card's Initiative, Movement, Attack, Range and Defense, validates the catalog and saves `enemies.json` through `PveContentLoader.SaveCatalog`. A 0 Movement or Attack removes that option.

## Content data

`enemies.json` lists enemy types:
- id, display name, optional token type (melee/ranged/heavy) and level (this slice supports level 1);
- orange cards: key, name, Initiative, copies, Attack, Range, Movement, idle flag, and ordered options of `move`/`attack` with a `hero`/`minion` target and a stop distance;
- purple cards: key, name, Initiative, Defense;
- plans, one space-separated string per plan.

`encounter.json` names the map revision, creep spaces and types, hero placement spaces and spawns (round, space, type). Spaces are written `"q,r"`. Absent numbers are written as 0.

`PveContentValidator` refuses invalid content with readable messages before composition. The launcher shows them instead of starting.

The shipped placeholder catalog contains Melee, Ranged and Heavy. Each has exactly two attack cards, one movement-only card and one idle card. Only Heavy has Defense cards: two, with values 3 and 5; Melee and Ranged have none. Melee pursues and attacks Blue creeps first; Ranged attacks heroes first, with range 3 and movement stopping at distance 3. Heavy keeps the original Brute priorities. PVE-D24 and its PVE-D25/D26 amendments in the design owner state the approved values, plans and spawn mix.

## Launch, smoke and build

### Co-op integration (PVE-D28, W-031)

The separate launcher adds a Host/Join menu. A playing host and two joiners claim three Blue seats, ready and choose distinct heroes. The host controls enemies and team-wide choices; placement and level-up remain hero-owned. Host and joiners enter the same seed and use identical PvE data. Existing private TCP provides keyed admission, reserved-seat reconnect and owner projections. An empty key binds loopback only; LAN/internet hosting requires a key and a reachable host port. No relay or host migration is added.

`PveCoopMatch` composes the encounter session behind a seat-serving host. The optional `MatchSeatPolicy` limits player seats to Blue, exposes Red's public cards, filters hero-owned commands and leaves continuation/Undo/team decisions with the mode. The null policy preserves existing-mode behavior. `PrivateSessionHost.SeatComposition` is an optional asymmetric seat-pick callback; ordinary PvP still uses `IDeferredMatchComposition` and the equal-team `HeroLineup` contract. `PveCoopLobby` owns transport pumping, host automation and the local player's ordinary online client. `PveEncounterView.AttachCoop` reads the online projection for map/rail status and gives shared-choice controls only to the local host.

Compatibility: shared wire /52 and snapshot v59 layouts stay unchanged. PvE co-op uses a distinct `pve-coop-1-<seed>` match-format identity and a digest including the PvE catalog and encounter data, so PvP, differing seeds and differing editable content cannot enter the same session. No full snapshots or Director commands are exposed as a client port. Initial content is composed locally from compatible files; subsequent mutable state comes only from owner-scoped network projections.

- **Launch.** The launcher loads and validates content, offers every configured hero (up to the number of placement spaces) and a seed, composes the encounter and loads the match scene with the external composition. The match then runs in the existing Director mode.
- **Smoke.** `-guardsPveSmokeTest` composes a fixed two-hero encounter and reports `GUARDS_PVE_SMOKE: BEGIN`, `COMPOSED` and `PASS` (or `FAIL`), then quits with its exit code.
- **Build.** `PveBuildEntryPoint.BuildWindowsPvePlayer` builds `Builds/PveWin64/GuardsDigitalPvE.exe`. It sets the product name and version stamp for that build only and restores both before saving assets.

## Current limits

Presentation is functional rather than finished: placements and prompts are buttons rather than board clicks, map plan-strip hover remains a text reading of card values and priorities, and the existing HUD still shows Life and Wave counters that PvE does not use. Placeholder content is untuned. A creep, being an ordinary minion, falls to any single hit (PVE-D04). Equal-Initiative enemies of one type tie often, so the player is asked to order them.
