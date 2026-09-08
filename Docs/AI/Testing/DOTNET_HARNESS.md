# Cross-platform dotnet harness

Canonical active shard for the Unity-free validation tier introduced by milestone 6.1t. Return to [TESTING.md](../TESTING.md).

The superseded single-assembly contract is preserved in [the archive](../Archive/DOTNET_HARNESS_BEFORE_CLEANUP.md).

## Scope and what it proves

The harness compiles unmodified production sources into separate `GuardsDigital.Core.CardState`, `GuardsDigital.Core`, `GuardsDigital.Application`, and `GuardsDigital.Network` assemblies targeting netstandard2.1/C# 9. The net10.0 test assembly is named `GuardsDigital.EditModeTests`, matching Unity and preserving Application's existing, deliberately narrow internal-access grant. Core grants no such access. Project-reference/identity checks compare the harness projects with their corresponding asmdefs.

The pure `ActiveEffectVisibility.cs` Presentation slice is compiled separately under its actual assembly identity. This is not the entire Presentation assembly. No Unity API is shimmed for that source: adding one makes the offline build fail. The map loader remains the one explicit stand-in below.

`Tools/Test-DotnetAssemblyBoundaries.ps1` deliberately compiles forbidden Core access and requires compiler error CS0122, then builds/runs the public-boundary and shim checks. Before 8.35z66 the same forbidden source compiled successfully in the single assembly; it now fails across the actual boundary. Production asmdefs and visibility grants are unchanged.

This does **not** prove full Presentation, Unity lifecycle/serialization/assets, Play Mode, or Unity's complete assembly/package graph. NUnit remains the existing offline package (3.14); Unity supplies its own version, so tests using new framework APIs still need Unity compilation. The presentation compile probe is also still a single-assembly convenience check, not a substitute for this boundary guard or Unity.

## Exclude-list maintenance rule

Unity-free test files are included by glob; the Unity- and Presentation-dependent Edit Mode files are named one per entry in `Tools/DotnetTests/UnityOnlyTests.props`. The count is deliberately not stated here: nothing checks it mechanically, and it was wrong in two directions at milestone 7.9a - stale by three, and describing every entry as Unity-dependent when several are excluded for reaching `GuardsDigital.Presentation`, which this harness does not compile. Read that file for the list; the harness guard rejects duplicates, globs, and missing paths. A newly added Edit Mode test therefore compiles into the harness by default, and a Unity-dependent newcomer breaks the harness build immediately. That build failure is the tripwire: resolve it by adding the file to the explicit exclusion list only when it genuinely requires Unity, never by weakening the harness. Deleting an excluded file must remove its entry.

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
| Assembly-boundary guard | `pwsh -NoProfile -File Tools/Test-DotnetAssemblyBoundaries.ps1` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\Tools\\Test-DotnetAssemblyBoundaries.ps1` |
| Focused filter | append `--filter "FullyQualifiedName~<FixtureName>"` | same |
| Living-doc validator | `pwsh -NoProfile -File ./Tools/Validate-LivingDocumentation.ps1` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1` |
| Board-map validator | `pwsh -NoProfile -File ./Tools/Validate-BoardMapSpecification.ps1` | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1` |
| Unity Edit/Play suites | not available; recorded as Windows-pending | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-UnityValidation.ps1` |

One-time Linux setup: a .NET 10 SDK and pwsh (`sudo snap install powershell --classic`, or rootless `dotnet tool install --global PowerShell`). First harness run needs network for NuGet restore.

## Tier placement

For pure Core/CardState/Application changes, green focused behavior tests plus directly affected regressions satisfies the Edit Mode portion of the [POLICY.md](POLICY.md) T1/T2 evidence on any OS. Any change touching Presentation, assets, scenes, assemblies, or another Unity-facing path still requires the applicable Unity tier through the Windows launcher before its milestone closes; until then the milestone records that run as pending.
