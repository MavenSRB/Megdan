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

## Encounter composition (W-021)

`PveEncounterComposer` builds an ordinary `MatchSessionInitialState` through public APIs; the existing-mode setup helpers that require 2v2/3v3 are not used.

- **Participants.** Two teams with one player per hero. The chosen heroes are Blue, holding their verified printed sets. Each enemy is a Red hero (`pve-enemy-NN`) with the rulebook dashboard.
- **Enemy cards.** Synthetic-fixture card definitions from `PveCardFactory`:
  - an orange card is one printed primary Attack (adjacent or Ranged) and one printed secondary Movement, neither replaceable;
  - an idle card prints no action;
  - a purple card prints a reaction Defense discarded on use.
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
- **`PveEncounterSession`** wraps the Director session. It refuses:
  - commands for enemies, and commitments while a transition is paused;
  - anything except the prompt's answers while a prompt stands;
  - anything while enemies still owe steps;
  - anything after the loss.

  `Step` and `RunAutomatic` take the automatic commands. Undo walks back over automatic commands to the last player command. Restart clears that record. Snapshot restore is refused.
- **Loss.** The encounter is lost when no Blue creep stands, or when the engine ends the match.

## Content data

`enemies.json` lists enemy types:
- id, display name and level (this slice supports level 1);
- orange cards: key, name, Initiative, copies, Attack, Range, Movement, idle flag, and ordered options of `move`/`attack` with a `hero`/`minion` target and a stop distance;
- purple cards: key, name, Initiative, Defense;
- plans, one space-separated string per plan.

`encounter.json` names the map revision, creep spaces and types, hero placement spaces and spawns (round, space, type). Spaces are written `"q,r"`. Absent numbers are written as 0.

`PveContentValidator` refuses invalid content with readable messages before composition. The launcher shows them instead of starting.

## Launch, smoke and build

- **Launch.** The launcher loads and validates content, offers every configured hero (up to the number of placement spaces) and a seed, composes the encounter and loads the match scene with the external composition. The match then runs in the existing Director mode.
- **Smoke.** `-guardsPveSmokeTest` composes a fixed two-hero encounter and reports `GUARDS_PVE_SMOKE: BEGIN`, `COMPOSED` and `PASS` (or `FAIL`), then quits with its exit code.
- **Build.** `PveBuildEntryPoint.BuildWindowsPvePlayer` builds `Builds/PveWin64/GuardsDigitalPvE.exe`. It sets the product name and version stamp for that build only and restores both before saving assets.

## Current limits

W-022 adds the rules and the session but no presentation: the launcher still hands the match to the existing HUD without the encounter session, overlays or pacing, which arrive in W-023. Placeholder content is untuned. A creep, being an ordinary minion, falls to any single hit (PVE-D04), so a passive player usually loses within the first round. Equal-Initiative enemies of one type tie often, so the player is asked to order them.
