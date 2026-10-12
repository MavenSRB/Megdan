# PvE validation recipes

Recipes for the separate PvE executable. Tiers, evidence and full-suite triggers stay in [testing policy](POLICY.md); what PvE implements is owned by [PvE implementation](../Project/PVE_IMPLEMENTATION.md). Return to the [testing index](../TESTING.md).

## Engine-free PvE tests

`Assets/Tests/PveEditMode/*.cs` hold the engine-free PvE fixtures (namespace `GuardsDigital.Tests.Pve`). Unity compiles them as `GuardsDigital.Pve.EditModeTests`. The dotnet harness compiles the same top-level files with its `GuardsDigital.Pve` project, so they use public APIs only. Unity-only PvE Edit Mode tests live under `Assets/Tests/PveEditMode/Unity/`, which the harness does not compile. W-026 adds `PveControlBoundaryTests` (offline: random legal Blue play never meets an automatic step that decides for a Blue hero, and no attack or minion Defense modifier reads a side's own units as enemies) and the Unity-only `PveDirectorPerspectiveTests` (the existing Director presenters with Blue as player side never draw an enemy as the player's own; without a side they still follow every hero).

Focused harness run:

`dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --filter "FullyQualifiedName~GuardsDigital.Tests.Pve"`

Unity Edit Mode: a request filter of `GuardsDigital.Tests.Pve` selects every PvE Edit Mode fixture, including the Unity-only ones.

The encounter fixtures play headlessly through a test driver with a deterministic, deliberately passive player: first placement, first card, Hold, no Defense, the first answer to each prompt. A hero-hunting variant of the placeholder catalog (minion options removed) keeps the creeps alive long enough to reach later rounds. The fixtures prove that mechanics work, not balance or usability.

## Enemy category integration

`PveAimTests` covers PVE-D36's names/face, dormant-to-active Range bonus, both actual range-four attacks, expiry/retrieval, source visibility and restore/Undo. `PveLauncherPlayModeTests` inspects Aim's rendered wording and its shared tray outline while pending, active and expired. These reuse the existing modifier and source-glow contracts rather than introducing PvE-specific timing or colors.

`PveShieldUpTests` covers PVE-D34's authored face, automatic Skill choice, existing immunity and turn expiry, restore/Undo/restart and unchanged default catalog. `PveContentSaveTests` covers typed Skill JSON/editor retention and unknown/mixed-action rejection; the launcher Play Mode fixture hovers Shield Up and captures its rendered wording. Shared catalog/provenance changes also run `CardDefinitionTests`, `EffectSeamConformanceTests` and the composition/identity fixtures below.

`PveEnemyCategoryTests` executes unchanged High Voltage against both Melee and Ranged, rejects Heavy from the minion choice, verifies staged adjacency and the existing discard/Defeat path, and round-trips the pending typed outcome through restore and retained-prefix Undo replay. Restoring that outcome without its trusted mode category must refuse atomically. `ComposableTargetSelectionTests` covers hero/minion/combined category membership, distinct identity, Attack target identity, radius modifiers and guarded removal/atomic minion-to-token replacement. Run these with the unchanged-card, snapshot-tampering, board-operation and feasibility-conformance fixtures when changing category consumers.

## Existing-mode identity

`GuardsDigital.Tests.EditMode.ExistingModeIdentityGoldenTests` drives the official six-player match through seats with a fixed seed and compares authoritative snapshot hashes at fixed command counts with values pinned before the PvE sequence. Run it after any change to shared code. A PvE change never re-pins it. A deliberate existing-mode rule or snapshot change re-pins it in the same commit, stating the reason.

`GuardsDigital.Tests.EditMode.PlayerAccentSideTests` covers the further seat colours. Composition-root and boundary changes also select `Phase4ArchitectureConformanceTests`, `ApplicationArchitectureBoundaryTests`, `OnlineArchitectureConformanceTests` and `ConfiguredMatchCutoverArchitectureTests`.

## Play Mode

`Assets/Tests/PvePlayMode` holds `GuardsDigital.Pve.PlayModeTests` (namespace `GuardsDigital.Tests.Pve.PlayMode`). They load the launcher scene by path in play mode, because it is not in Build Settings. A request filter of `GuardsDigital.Tests.Pve.PlayMode` selects them. They cover:
- the launcher composing into the match scene with the encounter session and overlay;
- the paced enemy commitments after placement, compact public four-slot enemy rails, Heavy defenses, no floating map labels and bidirectional token/rail hover;
- the enemy card editor's fields.

The scene/rail fixtures capture rendered frames using `WaitForEndOfFrame`; select `"batchMode": false` in the launcher request for these visual checks. Batch mode cannot execute that yield.

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

For current co-op packages, run `Tools/Invoke-PveArtifactSmoke.ps1 -Executable <built-player> -EvidenceDirectory <new-workspace-directory>` against that same PvE binary. The opt-in `-guardsPveCoopSmoke` driver exercises the shipped Host/Join, seat, Ready and hero controls in one playing host and two separate joiner processes. It places each owner's hero through authorized commands, verifies chosen-hero identity, private-card isolation, own commitment offers and seven live enemy rows, then waits for all peers before clean exit. Each process renders its board camera; the script checks non-flat pixels, identical framing and matching build stamps. This is a bounded loopback artifact check, not internet/reconnect/full-match evidence. Normal launch never starts the driver. Preserve the evidence directory and inspect a captured board before packaging.
