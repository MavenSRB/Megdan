# Guards Digital

For the current handoff route, the project has completed milestones through **8.35s**, including white-field prepared official hero faces, map-first effect choices, side-by-side optional-step banners above the retained hand, printed item symbols, grouped focused Unity validation, the corrected stationary hand/right-side unavailable fan, the cropped top-strip composition, the shared card perimeter, public teammate agreement for tied Initiative and Minion Battle confirmation, the existing owner-scoped HUD operating over the private network flow, the local six-client playtest-build rehearsal, the refusal of an unkeyed private host on anything but this machine, the automatic end-of-round sequence whose level-up choice belongs to the player whose hero is levelling, the seat HUD that renders the Minion Battle's public agreement stage instead of freezing on it, the per-player undo that steps a turn back one action at a time without ever taking back an attack, the explicit turn release that stops turns ending by themselves, the HUD panel isolation that keeps one broken panel from freezing a whole client, the gold top bar that lights whenever the game is waiting on you, the way back into your own seat after your game closes, every hero's own tray and card now sharing that same gold light, the top bar naming who is acting and marking their board piece so a bystander can see it too, the greying of a swap or place that another hero's standing card has already barred, naming that hero and card rather than offering it and then spending it for nothing, the dedicated host's own console gaining a typed command to release a reserved seat back for reclaiming, Brogan's charges now moving him the moment he picks where to land, before they ask whom he attacks, the host's own lobby now able to fill any empty seat with a spawned test-driver player for solo practice, a record of what has happened in the game - moves, attacks, defences, skills, defeats and end-of-round results - now reaching every player's screen along with who the table is currently waiting on, and every greyed control on screen now names why in a small hover pop-up - blocked by another hero's standing card, nothing left to target, or the plain word for an unstated cause, never a guess. The complete local six-seat player gate began at 8.30c, accepted 8.30d highlighting landed earlier out of numeric order, milestone **8.36** is next proposed, and deferred **8.29c** remains proposed.

A public source repository for a private, noncommercial digital prototype of the board game **Guards of Atlantis II** (2nd printing), built in Unity.

The project owner states that the public repository and included rulebook are shared with the relevant permission. Builds and excluded reference material are not public releases. See [Rights and distribution](#rights-and-distribution).

## What this is

A rules-accurate implementation of the board game, built as a pure C# rules engine with Unity acting only as passive presentation. The current build sets up the rulebook's official six-player match on the official single-lane map - three heroes a side, each dealt their own typed printed-card definitions - with movement, attack/defense, minions, and an end-of-round minion battle. Each card's printed text is composed slice by slice, and a primary action whose text is not composed yet is offered greyed and refused rather than performed without it.

Progress is tracked as numbered micro-milestones. The project has completed milestone **8.35s**: nothing is ever greyed without saying why. Hovering a control the game will not let you take shows a small pop-up naming the reason, read from the authority's own answer rather than guessed by the screen: a swap or place another hero's standing card has already barred names that hero and card, an action with nothing left to target says so, and every other cause not yet named this way reads a plain, honest "unavailable" instead of an invented explanation. The reason a barred board target draws no ring of its own, and the case where insufficient coins would grey a control, are recorded as open gaps rather than papered over - there is no player-facing coin-priced control to grey yet, and naming one blocked candidate among several would need a fact the game does not publish per-candidate today.
It rests on **8.35p**, where Brogan's three charge cards now move him as soon as he chooses where to land, and only then ask which unit beside him the attack takes - the order the card prints - and an attack whose target its own text named can no longer be taken back by the player who resolved it.
It rests on **8.35n**, where the top bar names the acting hero and what they are doing - your own turn, defense, level up and the rest read in full on your own screen, everyone else's read as who is acting or still owed an answer - and every viewer's board marks that hero's own piece with a gold ring so a bystander can see who is doing something, not only read it. It rests on **8.35j**, where the whole top bar turns gold whenever the game is waiting on you. It was only the narration strip inside it before, and it lit from whether the screen happened to be showing you a control - a fair guess that missed the commonest wait of all, the moment at the top of every round when everyone is choosing a card, because the hand owns that and no control appears. Now one reading answers the question for the bar and the narration together: your commitment, your turn, a decision that is yours to make, a level-up, a defence, a respawn, an agreement, or a finished turn waiting for you to release it. Somebody else's decision leaves your bar dark. It rests on **8.35i**, where a saved game paused in the middle of an attack can be loaded again. Some cards ask a question while an attack is still running - "you may move another unit next to the target" - and saving at exactly that moment produced a file the game then refused to read back, which also broke undo once a match got long enough. The game rebuilds such a question when it loads rather than storing the answer list, and it had been rebuilding it without telling itself which attack was running, so the card had nothing to point at. It rests on **8.35h**, where a broken panel of the HUD began breaking alone. The player's screen is drawn by seven independent panels, and one of them failing used to stop every panel behind it from updating - which is why a single display fault in the 2026-09-03 invited playtest showed up as a whole frozen client while the game itself kept accepting everybody's moves. Each panel now refreshes on its own and a failure is named rather than swallowed, so the board, the card tray, END TURN and UNDO keep working while the broken one sits there. It rests on **8.35g**, where turns no longer end by themselves: when a player finishes resolving their card, the turn stops and waits for them - END TURN, pressed a second time, is what hands it on to the next hero. Until they press it their UNDO still works, and it now reaches back into the action they have just finished, which is the whole point: the misplayed final movement that used to end the turn before anything could be done about it is now the one an undo can still reach. Everybody else waits the moment longer, as they would at a table. It rests on **8.35f**, where each player got an undo of their own - while their cards are resolving, an UNDO button beside END TURN steps that turn back one action at a time, as far as the moment the turn began and no further, never into the commitments, never into somebody else's turn, and never through an attack, because once a target has been chosen the defender has already shown their answer and it cannot be unshown; on **8.35e**, where every seat of a hosted session began rendering the Minion Battle's public team-agreement stage - the choosing side with its own agreement choices, the other side read-only - instead of throwing the presentation exception that froze all six clients of the 2026-09-03 invited playtest, and a seat's hand tray began rendering a hero whose revealed commitment card the seat's privacy-scoped card list deliberately withholds during end-of-round retrieval; on **8.35d**, where in a hosted six-seat session the end-of-round sequence began running by itself, one rulebook stage at a time, stopping only where a stage asks somebody a question, with the level-up card upgrade offered to the one player whose hero bought the level rather than to that player's whole team - nobody presses a shared continue button any more, and nobody can answer a teammate's upgrade; on **8.35a1**, where hosting a private session from the in-game lobby began refusing an unkeyed bind on any address but this machine's own, exactly as the command-line host role always has, because an unkeyed session's link is unencrypted and private per-player data may cross an unencrypted link only inside one machine; and on **8.35c**, where one dedicated seatless host process and six client processes of one disposable Windows build take the six seats through the primitive address/port/session-key lobby and play a complete private match on this machine, using the same build, content, host/session boundary, owner-scoped projections and HUD the invited playtest will use. Each client shows only the seat's private hand and authorized shared state, preserves public tied-Initiative and Minion Battle agreement, disables input while an answer or fresh reconnect projection is outstanding, and gives bounded waiting, delayed, rejection, synchronization, disconnect, reconnect and host-loss feedback. The current private wire is `guards-private-online/5`; protocols 1, 2, 3 and 4 are all rejected, because the seat projection has gained the fact that says whether a seat may step back and then the finished turn that is waiting for its own player, and milestone 8.35g takes the snapshot schema to v35 for that waiting turn it must persist. A same-machine rehearsal is not internet evidence: it proves nothing about relay or NAT traversal, external latency, multi-network connectivity, or real-internet reliability. Milestone **8.36**, the invited six-human internet match, is next proposed and **8.29c** remains deferred pending a reproducible report. The loaded match is the rulebook's six-player Quick Game with six distinct hero sets, three Waves, five Lives per team, twelve minions, and 108 playable cards. All six heroes pass the permanent real-input completion gate, and all 114 roster fronts are independently source-verified. The typed rules engine preserves card/effect continuations, progression, board operations, replay, Undo, and schema-v35 restore. The scaled HUD keeps the readable source-ratio card footprint and shared black perimeter, separates the playable hand from the right-side unavailable fan, retains optional-step context, and links hero plaques and map pieces on hover. Tied Initiative and Minion Battle wait for the applicable public unanimous teammate agreement before submitting their existing rules commands. Facedown information and private fronts remain protected, and the fixed Unity launcher accepts only fresh parseable all-green evidence.

## Requirements

- **Unity `6000.5.9f1`** — the exact version matters; the project is pinned to it in `ProjectSettings/ProjectVersion.txt`.
- **Windows** for the validation tooling and for the player-build smoke gate, which targets Win64. macOS and iOS are declared target platforms but are not yet supported. The toolchain was made portable at milestone 6.1d — paths are separator-agnostic and the Unity/Hub locations come from `GUARDS_UNITY_PATH` and `GUARDS_UNITY_HUB_PATH` or platform probing — and was confirmed on a non-Windows host (Linux, PowerShell 7) at milestone 6.1l, which corrected the three portability defects the first non-Windows runs exposed.
- URP 17.5, with one checked-in pipeline active globally and at every quality level.

## Getting started

1. Open the project folder in Unity Hub with editor version `6000.5.9f1`.
2. Open the only scene: `Assets/Scenes/Milestone1_2_HexGrid.unity`.
3. Press Play. The scene's composition root builds the match session, board, and HUD at runtime — most of what you see is generated, not authored, so the scene looks nearly empty when not running.

The composition root does not run outside Play Mode unless you opt in: `MatchSceneCompositionRoot` has a serialized `composeInEditMode` flag, off by default. Enable it only to preview the board in the editor, and do not save the scene while it is enabled — edit-mode composition regenerates the whole board on every domain reload and can serialize generated objects into the scene.

## Running the tests

Tests are split into Edit Mode and Play Mode assemblies under `Assets/Tests/`. Run them from Unity's **Window → General → Test Runner**, or through the project's fixed launcher:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-UnityValidation.ps1
```

Unity validation can take well over the default command timeout. Give it at least 15 minutes and do not kill the process if a wrapper times out.

The Unity-free test majority (272 tests at milestone 6.1t) also runs on any OS with a .NET 10 SDK, no Unity required:

```
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj
```

Two validators run without Unity and are fast:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1
```

The player-build smoke gate builds a real Windows player, runs it, and requires it to reach a configured match and complete one authoritative command. It catches what no Editor test can — an unresolved shader, a missing resource, a stripped type — because those only appear in a built player:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-PlayerBuildSmoke.ps1
```

## How the code is organized

| Path | Role |
| --- | --- |
| `Assets/GuardsDigital/Core/` | Pure C# rules, state, board, cards, snapshots, replay. No Unity references — enforced by assembly definition and by test. |
| `Assets/GuardsDigital/Application/` | Command dispatch, projections, the `IMatchSession` port. Also Unity-free. |
| `Assets/GuardsDigital/Presentation/` | Unity views, presenters, input. Passive: it forwards typed commands and renders immutable projections. It never decides rules. |
| `Assets/Tests/` | Edit Mode and Play Mode test assemblies. |
| `Tools/` | Validation and build scripts. |
| `Docs/AI/` | The project's living documentation. |

The one rule worth knowing before changing anything: **presentation never computes a rule outcome.** It submits a typed command through `IMatchSession` and renders whatever projection comes back. There is architecture-conformance test coverage that enforces this mechanically.

## Documentation

The documentation is written primarily for AI coding agents doing targeted lookups, so it is dense and heavily cross-linked. Start here depending on what you need:

| You want | Read |
| --- | --- |
| Current status, what's next | [`Docs/AI/SESSION_BRIEF.md`](Docs/AI/SESSION_BRIEF.md) |
| What the code actually does now | [`Docs/AI/Project/CURRENT_IMPLEMENTATION.md`](Docs/AI/Project/CURRENT_IMPLEMENTATION.md) |
| Milestone definitions and sequencing | [`Docs/AI/ROADMAP.md`](Docs/AI/ROADMAP.md) |
| Game-rule authority and open questions | [`Docs/AI/RULES_LEDGER.md`](Docs/AI/RULES_LEDGER.md) |
| Testing policy and exact recipes | [`Docs/AI/TESTING.md`](Docs/AI/TESTING.md) |
| Known risks and temporary structures | [`Docs/AI/TECHNICAL_RISKS.md`](Docs/AI/TECHNICAL_RISKS.md) |
| How agents are expected to work here | [`AGENTS.md`](AGENTS.md) |

`Docs/AI/DOCUMENTATION_GOVERNANCE.md` explains why the documentation is structured the way it is: every fact has exactly one canonical home, and history moves to `Docs/AI/Archive/` rather than being deleted. `Tools/Validate-LivingDocumentation.ps1` enforces that mechanically, including a family of code-truth checks that the documentation matches the actual source; the named list is in `Docs/AI/Testing/POLICY.md`.

## Working agreements

If you contribute here, whether human or agent:

- Work on one approved milestone at a time. Don't drift into adjacent features because they look easy.
- Never invent, simplify, or reinterpret a game rule. The 2nd-printing rulebook and verified card data are authoritative; record any approved deviation in the rules ledger.
- Every behavior change gets focused tests; every bug fix gets a regression test. A failing test blocks the handoff — it is never dismissed as pre-existing.
- Keep rules and state independent of Unity objects, presentation, animation, and audio.
- Commit locally after each completed milestone. Do not add a Git remote or push without explicit authorization.

## Rights and distribution

Guards of Atlantis II is the property of its rights holders. This is an unaffiliated private implementation.

- The rulebook PDF is included through Git LFS under the project owner's stated distribution permission. The extracted card corpus and map references remain deliberately excluded from Git and must not be distributed without separate permission.
- The public source repository and tracked rulebook are the only currently authorized publication. Builds and other excluded or untracked material remain private unless both the project owner and relevant rights holder authorize their distribution.
- No Git remote is configured. The previously authorized `maven/guards-digital` Gitea `origin` was removed at the project owner's direction on 2026-08-15; the repository is maintained locally.
