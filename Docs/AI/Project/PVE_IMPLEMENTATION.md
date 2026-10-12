# PvE implementation

Canonical owner for what PvE implements and how it reuses the existing game. Approved rules live in [PvE design](PVE_DESIGN.md); delivery and authorization live in [the PvE plan](../Roadmap/PVE.md) and the [brief](../SESSION_BRIEF.md). Return to [project index](../PROJECT_CONTEXT.md).

## Assembly boundary (PVE-D01, amended PVE-D38)

PvE code lives outside `Assets/GuardsDigital`:

- `Assets/GuardsPve/Rules` - `GuardsDigital.Pve`, engine-free (no Unity reference): content model and validation, seeded plan derivation, enemy card factory and encounter composer.
- `Assets/GuardsPve/Presentation` - `GuardsDigital.Pve.Presentation`: content loading (`JsonUtility`), the launcher/setup screen and the launch smoke.
- `Assets/GuardsPve/Editor` - `GuardsDigital.Pve.EditorTools`: the PvE build entry only.
- `Assets/GuardsPve/Scenes/PveLauncher.unity` - the PvE executable's first scene. It is not in Build Settings.
- `Assets/GuardsPve/Data~` - `enemies.json` and `encounter.json`. The `~` folder is not imported; the Editor reads it in place and the PvE build copies it to `PveData/` beside the executable.

Every PvE assembly sets `autoReferenced: false`; runtime/test assemblies retain `UNITY_EDITOR || GUARDS_PVE`. W-045's unified player build supplies that define and copies `PveData/`, replacing the original separate-player inclusion policy under PVE-D38. Shared assemblies still never reference PvE. `PveSharedLobby.Register` registers the mode adapter before scene load through `PrivateLobbyMode`; the optional artifact runner starts only under its explicit smoke flag. No new Resources/StreamingAssets content is added.

The unified executable starts the existing match/lobby scene. The old PvE launcher scene, separate build entry and legacy smoke remain available for historical fixtures, not the unified player's front door. Preserved W024/W035 packages retain their original separate-launcher behavior.

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
| Round-start placement | `ModeRoundStartPlacementRules`, one transition per scheduled round (`mode-round-start-placement/round-N`) | Begun with the ordinary Begin System Transition command and answered with the ordinary answer command. Typed `HeroPlacement` descriptions expose board destinations and hero ownership, without Level Up/minion captions or team votes. Only at turn 1 of its round before anyone commits. Designated spaces fall back to the nearest empty spaces, pausing only on ties. |
| Manual level grant removed | PVE-D39 retires `mode-grant-level/<hero>` and its executable wiring | No manual offer or grant command remains. Ordinary purchased progression is unchanged; PvE still skips round-end leveling and coins. Historical outcome enum value remains reserved. |
| Discard or defeat | `ModeDiscardPolicy` in the chosen-card and owned-card discard handlers, reading the bound rules through `EffectRosterState.ModeRules` | A Red hero discards only a Defense card; with none, the step publishes the defeated-hero fact and the ordinary Defeat follows (PVE-D19). |
| Refusal results | `MatchCommandResult.Refused`, `MatchSessionOperationResult.Refused` | Let the PvE session refuse a command in the session's own terms. |
| Session decorator (W-023) | `ExternalMatchComposition.SessionDecorator`, passed by the root to `ConfiguredMatchSceneRuntime` | Wraps the Director session, so the HUD, Undo and Restart all talk to the encounter session. Only an external composition sets one. |
| Director player side (W-026) | `ExternalMatchComposition.DirectorPlayerSide`, passed by the root to `ConfiguredMatchSceneRuntime`, which sets `DirectorHandsPresenter.PlayerSide`, `RoundActionPresenter.DirectorPlayerSide`, `DecisionWindowPresenter.DirectorPlayerSide` and `BoardInteractionUnityView.PerspectiveSide`; shared reading in `DirectorPlayerSide` | The Director plays one side. The tray follows only that side's heroes and cannot be pointed at another; while the other side owns the standing moment there is no turn control and no decision window; board outlines read from that side. Only an external composition names a side; a seat ignores it. |

## Post-defense counterattack contract

Original-mode Defense riders may declare `CardTextTiming.AfterSuccessfulDefense` (PVE-D37). The incoming attack and its after-attack text finish first; a successful card defense then applies that rider. `CounterattackCapabilities` can request the defense card's declared adjacent text-granted Attack against the original attacker, using the shared borrowed-action/off-turn continuation. The existing card-text remainder preserves the original attack during target/Defense choices and restore; target derivation excludes every other unit. Defeat may already have resolved the original actor's card before the counterattack hands the turn back. Existing-mode definitions declare no such rider.

## Enemy categories (W-033)

Under amended PVE-D30, Melee and Ranged count as both heroes and minions for targeting/effects; Heavy counts as hero only. Each enemy retains one Hero identity, normal enemy turns, cards, Defense and combat. Combined queries enumerate that identity once. The explicit `PveEnemyType.MinionCategory` is separate from token artwork, travels through editable content and co-op content identity, and binds through `MatchModeRules.MinionCategoryHeroes` on composition, restore and Undo. Existing-mode defaults add no category.

Shared selectors, presence, attack filtering and continuous minion scopes consult the mode category. A minion-only effect output naming such an enemy carries its actual Hero value, never a fabricated MinionId; its consumers and snapshot validator require the trusted mode mapping. Unchanged High Voltage therefore selects and defeats Melee/Ranged through their existing hero defeat path, reads pre-removal adjacency and excludes Heavy from its minion choice. No card definition is changed for category support.

## Weak-attack fallback (W-034)

Each type's lower-value Attack has an ordered Movement fallback after both attack priorities (PVE-D31, amended by PVE-D35/D37): Melee Slash moves 2 toward creeps, stopping adjacent; Ranged Shoot moves 1 toward heroes, stopping at range 2; Heavy Hurl moves 1 as close as possible toward the closest Blue unit (hero or creep), stopping adjacent. The controller picks Movement only when no prior Attack option applies. Typed `PveTargetKind.Unit` combines opposing hero/creep coordinates for Movement only; validation refuses it on Attack options. Ranged Take Position also stops at range 2 with Move 2; both Ranged attacks retain printed Range 3.

## Encounter composition (W-021)

PVE-D37 revises Heavy: Hurl is Attack 2 / Range 2, repeats once on a different target, and retains Move 1 fallback. Hook (Initiative 1) pulls the closest Blue hero or creep along a straight hex line until adjacent or blocked; an unaligned closest unit does not move, and ties use the host's seeded automatic legal choice. Thick Hide remains Defense 3 / Initiative 1 and, after a successful defense and the incoming attack's after-attack text, counterattacks the original attacker once at Attack 1 only if adjacent. Brace is unchanged. The counterattack is a normal, defendable attack, not direct damage.

`PveHookContent` supplies the typed closest-unit selection and delegates displacement to shared Direct Move. Hurl uses shared repeat control. Thick Hide declares a text-granted Attack and a mode-registered `CounterattackCapabilities` request at `CardTextTiming.AfterSuccessfulDefense`; the shared dispatcher retains the original attack in its existing card-text remainder, narrows targets to that attacker and resumes the original turn after the granted attack. No UI-owned resolution or extra snapshot field is introduced; content revision is `pve-content-4`.

`PveEncounterComposer` builds an ordinary `MatchSessionInitialState` through public APIs; the existing-mode setup helpers that require 2v2/3v3 are not used.

- **Participants.** Two teams with one player per hero. The playable launcher requires exactly three chosen Blue heroes, holding their verified printed sets, under PVE-D27. `PveEncounterComposer.ValidatePlayerSetup` owns that limit; generic composition still supports focused test fixtures. All five placement spaces remain available. Each enemy is a Red hero (`pve-enemy-NN`) with the rulebook dashboard.
- **Enemy cards.** Placeholder card definitions from `PveCardFactory` (synthetic-fixture authority, except approved original-mode effect wording):
  - an orange card is one printed primary Attack (adjacent or Ranged) and one printed secondary Movement, neither replaceable;
  - an idle card prints no action;
  - a standalone Skill carries a typed effect root; Shield Up uses `PveSkillKind.ImmuneThisTurn`, Aim uses `RangeNextTurn`, both through `PveSkillContent` (PVE-D34/D36);
  - a purple card prints a reaction Defense discarded on use; a declared text-granted counterattack is not an additional printed action or hand option;
  - each copy of an orange card is its own definition (`pve-<type>-<key>`, then `-2`, `-3` for later copies) with identical content, because the existing HUD's card inspection holds every face of a hero once (W-026: two copies sharing one face made inspecting that enemy throw).
- **Creeps.** Blue standard minions on the encounter's creep spaces.
- **Tie Breaker.** Derived from the setup seed.
- **Life and Wave counters.** Exist because the engine counts them, and are never part of a PvE ending.
- **Starting board.** Round-1 enemies stand on their spawns. The heroes and later enemies are participants off the board until the round-start placement puts them on it.
- **Mode rules.** `PveEncounterComposer.ModeRules` states the encounter's rules for the engine (see the hooks above).

`PveSeed` derives a uniform permutation index with an explicit SplitMix64 mix of seed, round and enemy identity. `PvePlans` maps it to a shuffle of all four orange card copies (24 possible orders for distinct cards), including Idle and excluding Defense (PVE-D32). No movement-first constraint or board-dependent selection remains. Mono and .NET agree; repeated reads, Undo and restart reproduce the same round's order without stored mutable random state.

## Encounter rules layer (W-022)

- **`PveEnemyController`** reads the match and names one command at a time. It owns no state, so restart, Undo and replay need nothing from it. It covers:
  - round-start placement and enemy commitments from `PvePlans` (seed, round and enemy; a removed planned card is replaced per PVE-D21);
  - each enemy's first applicable option, holding otherwise;
  - movement to the destination nearest its targets within the stop distance, cheapest first;
  - attack targets in the card's listed order, gated by authoritative availability and effective reach. The local authority's `ModeCardRuleValues` supplies all rule modifiers independently of display preferences; this derived read port adds no stored state or wire payload;
  - the weakest sufficient purple Defense, otherwise declining;
  - declining every enemy respawn, and advancing the end of round.
- **Ties (PVE-D39).** Equal enemy targets, destinations, blocked spawns and enemy Initiative ties choose a legal answer automatically. The host hashes the encounter seed and authoritative Core snapshot, so restore/Undo/replay reproduce the choice without another mutable RNG. Blue-owned choices are never automated. An enemy Initiative tie with at most one standing enemy remains unobservable and is settled directly.
- **Fallback.** Other enemy decisions take the weakest purple card where asked, prefer confirmation when available, otherwise select a seeded legal command; `PVE_FALLBACK` remains the diagnostic marker.
- **Blue decisions come first.** A standing decision a Blue hero owns - a Removal prevention during an enemy's turn, say - holds the enemy's turn until the player answers it (W-026: the controller had acted past it, the engine refused, and the encounter stalled).
- **`PveEncounterSession`** wraps the Director session. It refuses:
  - commands for enemies, and commitments while a transition is paused;
  - anything while enemies still owe steps;
  - anything after the loss.

  `Step` and `RunAutomatic` take the automatic commands. Undo walks back over automatic commands to the last player command. Restart clears that record. Snapshot restore is refused.
- **Loss.** The encounter is lost when no Blue creep stands, or when the engine ends the match.

## Presentation (W-023)

- **Session and overlay.** The launcher's external composition wraps the Director session in `PveEncounterSession` and attaches `PveEncounterView` to the composed runtime. The existing HUD drives Blue heroes as in Director mode, with Blue as its player side (W-026): it never follows, offers or outlines an enemy as the player's own. Its commands pass through the session's guard.
- **Shared interactions (PVE-D39).** No persistent round/turn/seed, ownership or enemy-choice popup. Initial placement publishes typed destinations through the same highlighted board/click path as respawn. A contextual host control remains only for Blue-owned team choices. The existing end-game screen shows only Blue rows, using the original no-creeps loss or an already-published engine ending; this adds no victory rule.
- **Unlabelled map tokens (W-032).** No floating name/plan strips above enemies. Public plans and Defense cards are read in the compact left rail. Only currently standing enemies have rows: removal/defeat removes the row, and future-wave rows appear on spawn.
- **Content-sized enemy box (W-037/W-047).** No ENEMIES heading or reserved header height. Each live tray uses its unchanged 64-unit row and 5-unit gap; miniature numbers are 13 rather than 11 units. The box shrinks/grows with occupancy and hides when empty. Height is capped at the viewport minus the existing 110-unit top offset and 330-unit hand clearance; excess rows scroll vertically. Resize and restored projections recompute the same geometry in solo and co-op.
- **Pacing.** Enemy steps run one at a time at `StepSeconds` intervals, each followed by a refresh. From W-026 a step also waits, for at most three seconds, until `ConfiguredMatchSceneRuntime.IsClockInputReady` reports the board's animations and attack spotlight settled; the launch smoke turns the wait off.
- **Rails and tokens (W-027/W-032).** The overlay replaces the three-seat enemy rail with a scrollable left rail using `HeroOverviewsUnityView` token identity, four card miniatures, card inspection and full-card hover. Rows are 64 units high, without hero name/level/item/discard sections. `TurnSlotsOverride` supplies all four public planned cards at round start; past occupied slots use actual resolved cards. Heavy's two Defense miniatures share the full-card preview and dim when no longer in hand; Melee/Ranged have none. Identity and standing/acting/defeated/spawn status remain in the row's assistive reading. Board-to-rail and rail-to-board hover share identity highlighting in solo and co-op. Melee/Ranged/Heavy artwork is selected by typed `tokenType` content, saved by the card editor, and applied through the optional `PrimitiveHeroView.SetTokenFaceOverride` presentation hook. Enemy identity and rules stay those of heroes. A Blue rail of more than three heroes is scaled to fit, from outside the HUD.
- **Enemy card editor.** The setup screen's enemy card editor (`PveCardEditor`, user request 2026-10-10) edits each card's Initiative, Movement, Attack, Range and Defense, validates the catalog and saves `enemies.json` through `PveContentLoader.SaveCatalog`. A 0 Movement or Attack removes that option.

## Content data

`enemies.json` lists enemy types:
- id, display name, optional token type (melee/ranged/heavy) and level (this slice supports level 1);
- orange cards: key, name, Initiative, copies, Attack, Range, Movement, idle flag, and ordered options of `move`/`attack` with a `hero`/`minion` target and a stop distance; standalone `skill: "immune-this-turn"` or `"range-next-turn"` supplies the approved Skill without attack/movement options;
- purple cards: key, name, Initiative, Defense;
- legacy plans, one space-separated string per plan: retained and validated for file/editor compatibility, but ignored by runtime ordering under PVE-D32.

`encounter.json` names the map revision, creep spaces and types, hero placement spaces and spawns (round, space, type). Spaces are written `"q,r"`. Absent numbers are written as 0.

`PveContentValidator` refuses invalid content with readable messages before composition. The launcher shows them instead of starting.

The shipped placeholder catalog contains Melee, Ranged and Heavy. Each has exactly two attack cards and one movement-only card. Melee's fourth card is Shield Up, Initiative 9: "This unit is immune this turn." Heavy's fourth card is Hook. PVE-D34 uses the existing unqualified Immune rule through a typed Skill: protection starts at resolution and expires at the normal This Turn checkpoint. Mode-only capability registration survives restore/Undo/restart without changing default catalogs; original wording appears on the shared card face without claiming official provenance. Only Heavy has Defense cards: two, with values 3 and 5; Melee and Ranged have none. Melee pursues and attacks Blue creeps first; Ranged attacks heroes first, with printed range 3 and movement stopping at distance 2 (PVE-D35). Heavy keeps the original Brute priorities. PVE-D24 and its PVE-D25/D26 amendments in the design owner state the approved values, plans and spawn mix.

PVE-D36 renames Ranged's movement card Take Position (Initiative 8), and replaces Idle with Aim (Initiative 1): "Next turn, have +1 Range." Aim uses the existing dormant/active Next Turn lifecycle and defeat/retrieval cancellation; either attack can actually reach 4 during the bonus. The compact tray preserves the shared purple source-card outline while pending or active, clearing when the effect ends. These original Skills use content revision `pve-content-3`.

## Launch, smoke and build

### Unified lobby (PVE-D38, W-045)

The shared host form adds **PvE — Defend the creeps** under Map. Selecting it hides Experimental and PvP setup counters/team size. `PveSharedLobby` reuses `PrivateLobbyUnityView` and `PrivateLobbyModel.ForLocalPlayers` for the same Take Seats and hero-pick screens. Exactly three Blue seats remain; each locally owned seat has a distinct ordinary authenticated connection, with independent capability, owner projection and command checks. Ready applies to this player's seats; the selection view follows the next locally owned pick. Owned hero tray/token clicks select that admitted seat and rebind all seat-scoped presenters. Other-player heroes remain inspectable without transferring authority. Automatic selection follows an owned hero needing input, retaining a manually selected actionable hero and moving to the next unfinished commitment in stable seat order. Pending board/card decisions take precedence. The in-match Control/Reconnect/Leave panel is removed; shared recovery and cleanup remain.

Host chooses a fresh seed. A normal Join initially uses the PvP front door, then recognizes a supported PvE map/format hint and retries full admission on the same endpoint once with the host seed and local PvE content. No wrong-key/build/content bypass is added. The host continues to own enemy automation and shared choices. Reconnect retries each locally retained seat capability; a terminal host departure does not transfer authority.

PvE composition always enables teammate coordination, including allied hand inspection and typed suggestions, while other experiments remain off. The outer mode host receives the same immutable options as the inner encounter authority, and external scene composition preserves them. PvP defaults and schema/wire layouts are unchanged. The legacy launcher/co-op adapter described below remains only for existing fixture/tool compatibility; W-045 is the normal player route.

Coordination-enabled host snapshots wrap the Core payload with immutable options. When the timer is disabled, its checkpoint is canonical and empty: decision-window measurement bookkeeping must not leak into snapshot identity or produce response-stack references to nonexistent clocks. Enabled timer snapshots retain their existing state and restore behavior.

### Transport and legacy adapter (PVE-D28, W-031)

PvE defaults to the shared PvP TCP port 45677 (W-038). Internet hosting requires a session key, host firewall allowance and router forwarding/reachable routing; joiners enter the host's public IP, port and key, with matching build/data. Empty keys remain loopback-only. PvP/PvE cannot host on the same port concurrently. No relay or automatic port mapping is added; keyed loopback validation is not proof of external internet reachability.

The host alone enters the seed (PVE-D33, W-039); Join ignores the local seed field and automatically inherits a different host seed from the existing public format-mismatch hint. Only a canonical nonzero seed for the current PvE format is accepted, and only before admission, once per initial Join. It then retries full keyed/build/map/content admission on the same endpoint. Wrong-key/content failures and unsupported formats remain terminal, never retry loops; admitted sessions/reconnect retain their seed. The effective seed is shown in lobby status. No wire/schema change.

The separate launcher adds a Host/Join menu. A playing host and two joiners claim three Blue seats, ready and choose distinct heroes. The host controls enemies and team-wide choices; placement remains hero-owned. Joiners inherit the host seed and use identical PvE data. Existing private TCP provides keyed admission, reserved-seat reconnect and owner projections. An empty key binds loopback only; LAN/internet hosting requires a key and a reachable host port. No relay or host migration is added.

`PveCoopMatch` composes the encounter session behind a seat-serving host. The optional `MatchSeatPolicy` limits player seats to Blue, exposes Red's public cards, filters hero-owned commands and leaves continuation/Undo/team decisions with the mode. The null policy preserves existing-mode behavior. `PrivateSessionHost.SeatComposition` is an optional asymmetric seat-pick callback; ordinary PvP still uses `IDeferredMatchComposition` and the equal-team `HeroLineup` contract. `PveCoopLobby` owns transport pumping, host automation and the local player's ordinary online client. `PveEncounterView.AttachCoop` reads the online projection for map/rail status and gives shared-choice controls only to the local host.

Compatibility: shared wire /52 and snapshot v59 layouts stay unchanged. The typed transition-description enum adds `HeroPlacement`, emitted only by the new PvE composition and decoded through the existing option payload. PvE co-op uses a distinct `pve-coop-3-<seed>` match-format identity and a digest including the PvE catalog and encounter data, so older enemy-choice/manual-grant builds, PvP, differing seeds and differing editable content cannot enter the same session. No full snapshots or Director commands are exposed as a client port. Initial content is composed locally from compatible files; subsequent mutable state comes only from owner-scoped network projections.

- **Launch.** The launcher loads and validates content, offers every configured hero (up to the number of placement spaces) and a seed, composes the encounter and loads the match scene with the external composition. The match then runs in the existing Director mode.
- **Smoke.** `-guardsPveSmokeTest` composes a fixed three-hero encounter and reports `GUARDS_PVE_SMOKE: BEGIN`, `COMPOSED` and `PASS` (or `FAIL`), then quits with its exit code. The opt-in `-guardsPveCoopSmoke` path exercises the shipped lobby in three separate player processes and captures their boards; [the recipe](../Testing/PVE.md#build-gate) owns its bounded claims. Neither driver runs during normal play.
- **Build.** `PveBuildEntryPoint.BuildWindowsPvePlayer` builds `Builds/PveWin64/GuardsDigitalPvE.exe`. It sets the product name and version stamp for that build only and restores both before saving assets.

## Current limits

Presentation is functional rather than finished: enemy cards use generated faces, and the existing HUD still shows Life and Wave counters that PvE does not use. Placeholder content is untuned. A creep, being an ordinary minion, falls to any single hit (PVE-D04). No new PvE victory condition is introduced.
