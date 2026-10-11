# PvE validation recipes

Recipes for the separate PvE executable. Tiers, evidence and full-suite triggers stay in [testing policy](POLICY.md); what PvE implements is owned by [PvE implementation](../Project/PVE_IMPLEMENTATION.md). Return to the [testing index](../TESTING.md).

## Engine-free PvE tests

`Assets/Tests/PveEditMode/*.cs` hold the engine-free PvE fixtures (namespace `GuardsDigital.Tests.Pve`). Unity compiles them as `GuardsDigital.Pve.EditModeTests`. The dotnet harness compiles the same top-level files with its `GuardsDigital.Pve` project, so they use public APIs only. Unity-only PvE Edit Mode tests live under `Assets/Tests/PveEditMode/Unity/`, which the harness does not compile. W-026 adds `PveControlBoundaryTests` (offline: random legal Blue play never meets an automatic step that decides for a Blue hero, and no attack or minion Defense modifier reads a side's own units as enemies) and the Unity-only `PveDirectorPerspectiveTests` (the existing Director presenters with Blue as player side never draw an enemy as the player's own; without a side they still follow every hero).

Focused harness run:

`dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --filter "FullyQualifiedName~GuardsDigital.Tests.Pve"`

Unity Edit Mode: a request filter of `GuardsDigital.Tests.Pve` selects every PvE Edit Mode fixture, including the Unity-only ones.

The encounter fixtures play headlessly through a test driver with a deterministic, deliberately passive player: first placement, first card, Hold, no Defense, the first answer to each prompt. A hero-hunting variant of the placeholder catalog (minion options removed) keeps the creeps alive long enough to reach later rounds. The fixtures prove that mechanics work, not balance or usability.

## Existing-mode identity

`GuardsDigital.Tests.EditMode.ExistingModeIdentityGoldenTests` drives the official six-player match through seats with a fixed seed and compares authoritative snapshot hashes at fixed command counts with values pinned before the PvE sequence. Run it after any change to shared code. A PvE change never re-pins it. A deliberate existing-mode rule or snapshot change re-pins it in the same commit, stating the reason.

`GuardsDigital.Tests.EditMode.PlayerAccentSideTests` covers the further seat colours. Composition-root and boundary changes also select `Phase4ArchitectureConformanceTests`, `ApplicationArchitectureBoundaryTests`, `OnlineArchitectureConformanceTests` and `ConfiguredMatchCutoverArchitectureTests`.

## Play Mode

`Assets/Tests/PvePlayMode` holds `GuardsDigital.Pve.PlayModeTests` (namespace `GuardsDigital.Tests.Pve.PlayMode`). They load the launcher scene by path in play mode, because it is not in Build Settings. A request filter of `GuardsDigital.Tests.Pve.PlayMode` selects them. They cover:
- the launcher composing into the match scene with the encounter session and overlay;
- the paced enemy commitments after placement and the plan strips;
- the enemy card editor's fields.

## Three-player co-op

`GuardsDigital.Tests.Pve.PveCoopMatchTests` covers three Blue seats, public enemy cards, private hero cards, ownership and mode/content identity. `GuardsDigital.Tests.Pve.PveCoopWireTests` drives keyed serialized admission, selection, play, reserved-seat reconnect and host loss. Shared host-policy or selection-composition changes additionally select the affected owner-projection, team-agreement, lobby/hero-selection, admission/reconnect and architecture fixtures, plus the existing-mode identity oracle above.

`GuardsDigital.Tests.Pve.PlayMode.PveCoopLobbyPlayModeTests` uses real loopback TCP and pointer-reachable Host/Join/Ready/hero controls, scene handoff, local reconnect and remote enemy rails. Its headless peers share the Editor process; it does not claim external-network or packaged-player coverage. Retain the solo launcher fixture for changes to the shared launcher/overlay path.

## Build gate

Run the existing gate first, then the PvE gate, so the PvE gate can check isolation:

1. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-PlayerBuildSmoke.ps1`
2. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-PvePlayerBuildSmoke.ps1`

The PvE gate builds once and runs `GuardsDigitalPvE.exe -guardsPveSmokeTest`. It requires:
- the `GUARDS_PVE_SMOKE` markers and a zero exit code;
- the content data in `PveData/`;
- `GuardsDigital.Pve.dll` and `GuardsDigital.Pve.Presentation.dll` in the PvE player;
- no `GuardsDigital.Pve*` assembly in `Builds/Win64Smoke`.

Both gates launch Unity directly, as the existing player-build gate does; run them serially with no other Unity process. After any Unity run, restore `Assets/UniversalRenderPipelineGlobalSettings.asset` if a batch run rewrote it.
