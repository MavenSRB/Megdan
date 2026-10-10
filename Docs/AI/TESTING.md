# Guards Digital - Testing Index

This compact index routes to the exact active validation policy, reusable launcher instructions, focused recipes, gate definitions, and test ownership. Historical run totals live in milestone commit bodies and the indexed archives, never in active shards.

## Canonical active shards

- [Testing policy and evidence contract](Testing/POLICY.md)
- [Cross-platform dotnet harness](Testing/DOTNET_HARNESS.md)
- [Future validation gates and recipes](Testing/FUTURE_GATES.md)
- [Private-online validation recipes](Testing/PRIVATE_ONLINE.md)
- [Foundation focused test recipes](Testing/RECIPES_FOUNDATION.md)
- [Turn, round, snapshot, and replay test recipes](Testing/RECIPES_TURNS.md)
- [Combat and card-Movement test recipes](Testing/RECIPES_COMBAT_AND_MOVEMENT.md)
- [Play Mode, complete suites, and test ownership](Testing/PLAYMODE_AND_SUITES.md)
- [Expected-outcome tables: writing, running, and reading them](Testing/EXPECTED_OUTCOME_TABLES.md)
- [Rulebook ledger invariants and source-to-check map](Testing/RULEBOOK_LEDGER_INVARIANTS.md)
- [PvE validation recipes](Testing/PVE.md)

## Historical recipes

- [Superseded testing recipes and the milestone coverage narrative](Archive/TESTING_HISTORY.md)

## Required launcher

All Unity validation uses the exact parameterless project-local entry point defined in the policy shard:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-UnityValidation.ps1
```

The Unity-free Core/CardState/Application test majority also runs on any OS through the dotnet harness (see [the harness shard](Testing/DOTNET_HARNESS.md)):

```
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj
```
