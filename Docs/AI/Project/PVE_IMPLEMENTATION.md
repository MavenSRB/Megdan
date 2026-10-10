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
- **Static encounter (W-021).** Heroes stand on the placement spaces in the order chosen; round-1 enemies stand on their spawns; round-2 enemies are participants off the board.

`PveSeed` derives plan choices with an explicit SplitMix64 mix of seed, round and enemy identity, so Mono and .NET agree and nothing is stored.

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

W-021 is the static skeleton only: enemies take no decisions, and nothing yet blocks commands for Red heroes. Placement, granted levels, spawning, enemy plans and their display, the remaining PvE rules, ties and the loss ending arrive in W-022 and W-023. The encounter currently runs on the existing HUD unchanged, whose rails are laid out for three heroes per side.
