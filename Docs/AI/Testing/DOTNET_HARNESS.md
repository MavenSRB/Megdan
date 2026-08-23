# Cross-platform dotnet harness

Canonical active shard for the Unity-free validation tier introduced by milestone 6.1t. Return to [TESTING.md](../TESTING.md).

## Scope and what it proves

`Tools/DotnetTests/GuardsDigital.DotnetTests.csproj` compiles the complete `Assets/GuardsDigital/Core` (including the nested `Core/CardState` assembly sources) and `Assets/GuardsDigital/Application` sources plus every Unity-free Edit Mode test file, unmodified, under net10.0 with `LangVersion` pinned to 9.0 (Unity 6000.x's C# level) so net-only syntax cannot leak into shared source. At milestone 6.1t it runs 272 tests in seconds on any OS with a .NET 10 SDK, including the full snapshot compatibility corpus, the replay/Undo determinism suites, the `MatchAggregate` atomicity fixture, and all authority fixtures. The single-assembly compile makes `GuardsDigital.Application`'s `InternalsVisibleTo` grant inert but harmless. It also hides the opposite case, which is not harmless: `GuardsDigital.Core` grants the Edit Mode test assembly nothing, so a test calling a Core `internal` member compiles and passes here and fails to compile at all under Unity's real assembly split. Milestone 8.12 introduced exactly that and it stood until milestone 8.14 next ran the Windows launcher, because the milestones between were green here and truthfully recorded that no production Unity-facing path had changed. Exercise an internal through its real caller, as the existing fixtures do, and run the launcher whenever an Edit Mode test file changes.

It does NOT prove: Presentation compilation or behavior, `[UnityTest]`/Play Mode scenarios, assembly-boundary reflection checks, IL conformance over the real assembly split, scene or asset integrity, or anything requiring the Unity editor. Those remain owned by the Unity suites through the Windows launcher in [POLICY.md](POLICY.md).

## Exclude-list maintenance rule

Unity-free test files are included by glob; the Unity- and Presentation-dependent Edit Mode files are named in the csproj `Exclude` attribute. The count is deliberately not stated here: nothing checks it mechanically, and it was wrong in two directions at milestone 7.9a - stale by three, and describing every entry as Unity-dependent when several are excluded for reaching `GuardsDigital.Presentation`, which this harness does not compile. Read the attribute for the list. A newly added Edit Mode test therefore compiles into the harness by default, and a Unity-dependent newcomer breaks the harness build immediately. That build failure is the tripwire: resolve it by adding the file to the `Exclude` list only when it genuinely requires Unity, never by weakening the harness. Deleting an excluded file must remove its `Exclude` entry.

## The shim boundary (known drift risk)

`Tools/DotnetTests/Shims/BoardMapCatalogShim.cs` stands in for `Assets/GuardsDigital/Presentation/Board/BoardMapJsonLoader.cs` (which is bound to `UnityEngine.JsonUtility` and `Resources.Load`) so map-dependent tests compile without Unity. It is the harness's only production stand-in and must keep the same public contract and DTO field names as the Unity loader.

Because a shim can drift from production wiring, the boundary is guarded twice:

- `Tools/DotnetTests/HarnessTests/BoardMapShimParityTests.cs` asserts the shim-loaded official map yields the same authoritative inventory the Unity-side `BoardMapDefinitionTests` assert through `Resources.Load` (292/210/82 spaces, 7 zones, one starting Battle Zone, 40/20/20 spawn points, exact ordered lane, revision 1).
- The Windows workstation's routine unfiltered Edit Mode runs execute the Unity-loader fixtures, proving the contract in both directions.

Retiring the shim entirely by moving map parsing onto a Unity-free parser in Core remains a possible separately proposed milestone.

## Commands

| Task | Linux | Windows |
| --- | --- | --- |
| Full harness | `dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj` | same |
| Focused filter | append `--filter "FullyQualifiedName~<FixtureName>"` | same |
| Living-doc validator | `pwsh -NoProfile -File ./Tools/Validate-LivingDocumentation.ps1` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1` |
| Board-map validator | `pwsh -NoProfile -File ./Tools/Validate-BoardMapSpecification.ps1` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1` |
| Unity Edit/Play suites | not available; recorded as Windows-pending | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-UnityValidation.ps1` |

One-time Linux setup: a .NET 10 SDK and pwsh (`sudo snap install powershell --classic`, or rootless `dotnet tool install --global PowerShell`). First harness run needs network for NuGet restore.

## Tier placement

For pure Core/CardState/Application changes, a green full harness run plus the applicable focused filters satisfies the Edit Mode portion of the [POLICY.md](POLICY.md) T1/T2 evidence on any OS. Any change touching Presentation, assets, scenes, assemblies, or another Unity-facing path still requires the applicable Unity tier through the Windows launcher before its milestone closes; until then the milestone records that run as pending.
