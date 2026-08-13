# Guards Digital

A private digital adaptation of the board game **Guards of Atlantis II** (2nd printing), built in Unity.

This repository is private and not distributable. See [Rights and privacy](#rights-and-privacy) before sharing anything from it.

## What this is

A rules-accurate implementation of the board game, built as a pure C# rules engine with Unity acting only as passive presentation. The current build plays a synthetic vertical slice: a 1v1 match on the official single-lane map with placeholder cards, movement, attack/defense, minions, and an end-of-round minion battle. No real hero card content is implemented yet.

Progress is tracked as numbered micro-milestones. As of the last update the project has completed milestones through **6.1e**.

## Requirements

- **Unity `6000.5.7f1`** — the exact version matters; the project is pinned to it in `ProjectSettings/ProjectVersion.txt`.
- **Windows** for the validation tooling. macOS and iOS are declared target platforms but are not yet supported; making the toolchain cross-platform is scheduled as part of milestone 6.1d.
- Built-In Render Pipeline (not URP/HDRP). A URP migration is scheduled much later, at milestone 8.28.

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

Two validators run without Unity and are fast:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1
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

`Docs/AI/DOCUMENTATION_GOVERNANCE.md` explains why the documentation is structured the way it is: every fact has exactly one canonical home, and history moves to `Docs/AI/Archive/` rather than being deleted. `Tools/Validate-LivingDocumentation.ps1` enforces that mechanically, including three checks that the documentation matches the actual source.

## Working agreements

If you contribute here, whether human or agent:

- Work on one approved milestone at a time. Don't drift into adjacent features because they look easy.
- Never invent, simplify, or reinterpret a game rule. The 2nd-printing rulebook and verified card data are authoritative; record any approved deviation in the rules ledger.
- Every behavior change gets focused tests; every bug fix gets a regression test. A failing test blocks the handoff — it is never dismissed as pre-existing.
- Keep rules and state independent of Unity objects, presentation, animation, and audio.
- Commit locally after each completed milestone. Do not add a Git remote or push without explicit authorization.

## Rights and privacy

Guards of Atlantis II is the property of its rights holders. This is an unaffiliated private implementation.

- The rulebook PDF and the extracted card corpus are deliberately excluded from Git. Preserve them at their documented paths; never commit or distribute them.
- Do not publish, upload, market, monetize, or distribute any part of this project — including builds, assets, and screenshots — without explicit authorization from both the project owner and the relevant rights holder.
- No remote is configured on this repository, and that is intentional.
