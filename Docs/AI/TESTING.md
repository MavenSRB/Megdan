# Guards Digital - Testing Index

This compact index routes to the exact active validation policy, reusable launcher instructions, focused recipes, gate definitions, and test ownership. Historical run totals remain with milestone evidence in the phase handoff archives.

## Canonical active shards

- [Testing policy and evidence contract](Testing/POLICY.md)
- [Future validation gates and recipes](Testing/FUTURE_GATES.md)
- [Foundation focused test recipes](Testing/RECIPES_FOUNDATION.md)
- [Turn, round, snapshot, and replay test recipes](Testing/RECIPES_TURNS.md)
- [Combat and card-Movement test recipes](Testing/RECIPES_COMBAT_AND_MOVEMENT.md)
- [Play Mode, complete suites, and test ownership](Testing/PLAYMODE_AND_SUITES.md)

## Historical recipes

- [Superseded testing recipes](Archive/TESTING_HISTORY.md)

## Required launcher

All Unity validation uses the exact parameterless project-local entry point defined in the policy shard:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-UnityValidation.ps1
```
