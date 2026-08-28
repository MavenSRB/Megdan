# Guards Digital

For the current handoff route, the project has completed milestones through **8.28a1**; independent readiness review **8.28a** remains active.

A public source repository for a private, noncommercial digital prototype of the board game **Guards of Atlantis II** (2nd printing), built in Unity.

The project owner states that the public repository and included rulebook are shared with the relevant permission. Builds and excluded reference material are not public releases. See [Rights and distribution](#rights-and-distribution).

## What this is

A rules-accurate implementation of the board game, built as a pure C# rules engine with Unity acting only as passive presentation. The current build sets up the rulebook's official six-player match on the official single-lane map - three heroes a side, each dealt their own typed printed-card definitions - with movement, attack/defense, minions, and an end-of-round minion battle. Each card's printed text is composed slice by slice, and a primary action whose text is not composed yet is offered greyed and refused rather than performed without it.

Progress is tracked as numbered micro-milestones. The project has completed milestone **8.28a1** while parent review **8.28a** remains active. The loaded match is the rulebook's six-player Quick Game with six distinct hero sets, three Waves, five Lives per team, twelve minions, and 108 playable cards. All six heroes pass the same permanent real-input completion gate, and all 114 roster fronts are independently source-verified; their reachable effect semantics are identity-covered, their enum-like integer bindings are content-load validated, and the scaled HUD identifies each hero and current decision. The typed rules engine preserves card/effect continuations, repeats, progression, board operations, replay, Undo, and schema-v34 restore. Every complete card face now uses the same readable approximately 300.46-by-420 source-ratio footprint: prepared private fronts stand alone without shell or distortion, and absent resources take the typed generated fallback. Five ordinary faces overlap in a clickable borderless hand while the full-screen board fits every playable hex around the actual HUD; the adjacent status/action dock keeps one fixed largest-state footprint as cards are selected and revealed. Facedown retains its information boundary, all private fronts remain outside tracked publication, and the fixed Unity launcher accepts only fresh parseable all-green evidence recreated by its own run.

## Requirements

- **Unity `6000.5.7f1`** — the exact version matters; the project is pinned to it in `ProjectSettings/ProjectVersion.txt`.
- **Windows** for the validation tooling and for the player-build smoke gate, which targets Win64. macOS and iOS are declared target platforms but are not yet supported. The toolchain was made portable at milestone 6.1d — paths are separator-agnostic and the Unity/Hub locations come from `GUARDS_UNITY_PATH` and `GUARDS_UNITY_HUB_PATH` or platform probing — and was confirmed on a non-Windows host (Linux, PowerShell 7) at milestone 6.1l, which corrected the three portability defects the first non-Windows runs exposed.
- URP 17.5, with one checked-in pipeline active globally and at every quality level.

## Getting started

1. Open the project folder in Unity Hub with editor version `6000.5.7f1`.
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
