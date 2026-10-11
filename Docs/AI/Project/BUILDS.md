# Private build record

Return to [project index](../PROJECT_CONTEXT.md). [Testing policy](../Testing/POLICY.md) owns build checks.

Maintain one compact row for each new package: identity/source, included work, validation evidence locator and material limitations. Exact checks/results live in the completion commit body. Older package history remains in phase archives; do not reconstruct it here.

| Package | Identity and included work | Evidence / limitations |
| --- | --- | --- |
| 9.100 | `Builds/Playtest-9.100-ui-playtest.zip`; 32 heroes, wire /49, schema 59; SHA-256 `39c2f120cee059aff641e43020bae872b95b8fcedaf7b86000b5c321c3d74c87`; named seat-ordered trays, Escape dismissal and uniform token rims | [Exact source and validation record](../Roadmap/PHASE_9.md#x-9100-cut-the-integrated-windows-playtest-package). Private Windows package; not new internet or cross-platform evidence. |
| W003 | `Builds/Playtest-W003-inspection-fixes.zip`; source `a1706ee6` plus package version; stamp `1.0-W003-inspection-fixes+20261008185038-05d0560d`; SHA-256 `5a7ff50c254d3e97064aa8e6abaa56b759fccc775b6d7983abea06db48ee86a9`; W-002 spell inspection and player-name fixes | `work/W-003` commit evidence; reused focused source checks, fresh exact-artifact smokes and ZIP integrity. Windows/private only; no new internet, fault or cross-platform claim. |

| W013 | `Builds/Playtest-W013-live-card-values.zip`; source `482322ea` plus package version; stamp `1.0-W013-live-card-values+20261009071025-fdef748c`; SHA-256 `a634505e734030baae93c92f096274aef21ef68826ce18c20b69e83e61bf3c52`; current source including live modifiers, wire /50, schema 59 | `work/W-013` commit evidence; exact-artifact smokes and ZIP integrity. Windows/private only; all players require this package. No proposed breakdown UI; W-004 design gate remains open. |
| W024 PvE | `Builds/Playtest-W024-pve-test.zip` (top folder `GuardsPvE/`); separate PvE executable `GuardsDigitalPvE.exe` with `PveData/` content beside it; source `e1c15654` (W-026) plus package version; stamp `1.0-W024-pve-test+20261011002704-24e33cce`; SHA-256 `ec399c68bd164e34c13c119add1bf7cf63c64d25fd08afa426a77fce0b31f7c1`; first PvE test slice (W-021 to W-026) with placeholder enemy content | `work/W-024` commit evidence; PvE build smoke, exact-artifact launch of the extracted copy, per-file SHA-256 in `tmp/w024-package-hashes.txt`, existing-mode player smoke and PvE-assembly absence. Windows/private only; enemy content untuned, hero token art absent (private corpus not on this workstation). Not an existing-mode package: W013 stays current for multiplayer. |

Packaging uses changed/affected checks and exact-artifact smokes, not unrelated suites.
