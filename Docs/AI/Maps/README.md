# Board map specifications

## Scope and identity

`official-single-lane.map.json` is the milestone 4.1 reviewed inventory for the complete official single-lane map. The user-supplied coordinate diagram is preserved locally at `Docs/Guards of Atlantis II - Map References/official-single-lane-axial-reference.jpg`; that private reference directory is intentionally excluded from Git.

The schema is map-scoped so future maps can coexist:

- `mapId` identifies the map and `revision` identifies its content revision.
- `spaceId`, `zoneId`, and `spawnPointId` are stable only within their owning map. `spaceId` is canonically derived from the axial coordinate as `q{q}_r{r}`.
- A globally unambiguous child reference composes `mapId` with the applicable child ID. Persisted, catalog, external, and cross-map references must not treat a bare child ID, coordinate, or one static map as globally unique.
- A match selects one immutable definition by `(mapId, revision)`. Coordinate-based rules and queries then operate locally within that selected definition instead of qualifying every internal coordinate individually.
- Zones and spawn points are collections owned by the selected map specification. A future map receives its own zone and spawn collections rather than extending this map through name-based branches.

Milestones 4.1-4.1a defined and hardened the data/validation contract. Milestone 4.2 loaded an immutable selected definition through the reusable catalog boundary, bound it to authoritative board state, and began persisting its `mapId`/revision in the versioned snapshot at that era's schema version 2; the snapshot schema has since advanced to v58, and the map's `(mapId, revision)` identity still rides it. Milestone 4.2a replaced the temporary scene geometry, and milestone 4.3 added zone adjacency and the ordered Lane relationship.

## Alternate small-map preparation

[9.63 packet](ALTERNATE_SMALL_PREPARATION.md) owns the explicit coordinate/zone/spawn candidate, registration, private review sheets, perimeter review limitation and host-selector integration checks. The user-confirmed spawn-zone terrain border is completed at [9.63a](../Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md#x-963a-complete-the-small-map-terrain-border). It is not loaded by runtime.

## Future map references

Three distinct maps are preserved for future implementation at [9.60](../Roadmap/PHASE_9.md#x-960-preserve-the-future-map-references). The later uploads are preferred references; earlier copies remain preserved. These are private visual references, not reviewed runtime coordinate/zone/spawn specifications. All files stay in the existing Git-ignored `Docs/Guards of Atlantis II - Map References/` directory; tracked preparation data and planning are linked above. Saving does not grant publication or distribution rights. The visual descriptors below are filing aids, not official map names.

### Future alternate map for 2v2 and 3v3

The first pictured map is an additional future option for **2v2 and 3v3**. Implementation [9.61](../Roadmap/PHASE_9.md#961-implement-the-alternate-map-for-2v2-and-3v3) is authorized after 9.54 -> 9.58 -> 9.56, including a host-screen map selector; the existing map remains available. Immediate 9.63 prepares its mapping without changing runtime.

- Preferred file: `Docs/Guards of Atlantis II - Map References/alternate-2v2-3v3-preferred-reference-2026-10-05.png`, copied from `codex-clipboard-ea422bb2-e14e-4d02-aeca-4e270f2057f4.png`.
- Earlier original: `Docs/Guards of Atlantis II - Map References/alternate-2v2-3v3-user-reference-2026-10-05.png`, copied from `codex-clipboard-278a5c29-8648-4cd4-a57c-667b4321932e.png`.
- Both received files are PNG, **900 x 900**, 1,841,279 bytes; SHA-256 `0e2d26886b565b28c60d431fe321cf43da24edece1bb48635a3100b30e30089f`. Although the later upload was described as higher resolution, the received bytes are identical. Both source/copy pairs match exactly.

Do not apply the separate maps' two active Battle Zones or 4v4/5v5 formats to this first image. Review its actual topology, setup and rules before transcription; do not infer additional rules from the artwork.

### Future two-lane maps for 4v4 and 5v5

The second map and the last lava/bridge image are **both two-lane maps for 4v4 and 5v5**. The user's correction explicitly supersedes the last image's initial 2v2 description. Implementation of two-lane support and both map choices is queued as [9.62](../Roadmap/PHASE_9.md#962-implement-the-two-lane-maps-for-4v4-and-5v5), not authorized.

Second map, green terrain around central water:

- Preferred file: `Docs/Guards of Atlantis II - Map References/two-lane-4v4-5v5-preferred-reference-2026-10-05.png`, copied from `codex-clipboard-2243bf55-d77c-4536-9d41-83ae9d63c9ad.png`.
- Earlier original: `Docs/Guards of Atlantis II - Map References/two-lane-4v4-5v5-user-reference-2026-10-05.png`, copied from `codex-clipboard-f7b4ac46-ad1d-45e4-b625-327aa892f202.png`.
- Both received files are PNG, **900 x 900**, 1,952,903 bytes; SHA-256 `86b86900f12c49939d82db423ac40ae14be41bcf1e7637393f782f9a408b4ea6`. The later upload was described as higher resolution, but its received bytes are identical. Both source/copy pairs match exactly.

Last map, ice/lava terrain with a central bridge:

- File: `Docs/Guards of Atlantis II - Map References/two-lane-4v4-5v5-lava-bridge-user-reference-2026-10-05.png`, copied from `codex-clipboard-88a35206-cbd7-4af5-a3f0-f28fc583d5b6.png`.
- PNG, **900 x 900**, 1,776,026 bytes; SHA-256 `0c9f29e6d66a18fe95d3438e250b99c160149f17e122ddb74a359092a1f7543e`. Source and saved copy match exactly. The initially misclassified 2v2 filename was corrected without changing the image bytes.

The agreed two-lane design is **one match with one shared initiative/turn order**, **two separate active Battle Zones**, and **independent pushing of those Battle Zones**. A push in either lane reaching the opposing Throne ends the **whole match**; the other lane need not also reach a Throne. No other rules changes were requested beyond extra players and these lane/Battle Zone semantics. Do not introduce parallel lane turns, separate matches, a requirement to win both lanes or unrequested balance changes.

Before implementation, review each map's coordinate/terrain/zone/spawn inventory and applicable setup values for eight and ten players. Audit references to the Battle Zone, Minion Battles, Lane Pushes and wave handling against two-lane state; resolve ambiguous mapping rather than silently inventing rules. These rules do not change the separate 2v2/3v3 reference or shipped behavior.

### User-edited spawn artwork

At [9.60a](../Roadmap/PHASE_9.md#x-960a-replace-tower-markers-in-private-map-artwork), the user requested same-team **ranged minion** symbols in place of the two towers on the small map and **melee minion** symbols in place of all four towers on each large map. The following private edited copies are the working artwork references for that request; every original/preferred upload above remains unchanged. The symbol types were checked against the printed rulebook p.6 setup legend.

| Format / map | Edited file in `Docs/Guards of Atlantis II - Map References/` |
| --- | --- |
| 2v2/3v3, green | `alternate-2v2-3v3-ranged-spawns-2026-10-05.png` |
| 4v4/5v5, green two-lane | `two-lane-4v4-5v5-melee-spawns-2026-10-05.png` |
| 4v4/5v5, ice/lava two-lane | `two-lane-4v4-5v5-lava-bridge-melee-spawns-2026-10-05.png` |

The current copies retain [9.60b's cleanup](../Roadmap/PHASE_9.md#x-960b-clean-and-centre-the-replacement-map-glyphs) and [9.60c's alignment](../Roadmap/PHASE_9.md#x-960c-align-replacement-glyphs-with-their-lanes): all ten glyphs were checked against hex centres and original same-team lane markers. The four green-map replacements now match their local lane directions; the upper blue glyph again uses its native green-map donor, with its nearby rock excluded. Whole-pixel shifts and quarter-turn rotations retain sharpness. Edits remain confined to the replacement hex interiors. Original uploads, first edits (`*-v1.png`), cleaned edits (`*-v2.png`) and their earlier recipes remain preserved privately. All maps retain 900 x 900 resolution. The private `spawn-marker-alignment-recipe-2026-10-05.json` records source/target centres, rotations, lane references, donor override and hashes; `spawn-marker-alignment-2026-10-05.ps1` reproduces this pass from the cleaned backups. `spawn-marker-alignment-review-2026-10-05.png` shows the final ten close-ups. This changes reference artwork only: no runtime spawn data, setup counts or shipped rules changed. Carry the requested spawn types into the later reviewed map transcription at 9.61/9.62.

## Source of truth and the generated runtime resource

This file is the single source. `Assets/Resources/Maps/official-single-lane.json` is **generated** from it and must never be edited directly.

The copy exists only because Unity cannot load a `Docs/` path: `BoardMapJsonLoader` resolves the map through `Resources.Load<TextAsset>("Maps/{mapId}")`, which requires a real imported asset under `Assets/Resources`. The generated file is therefore committed, not built on demand.

| Path | Role |
| --- | --- |
| `Docs/AI/Maps/official-single-lane.map.json` | Canonical reviewed specification. Edit here. |
| `Assets/Resources/Maps/official-single-lane.json` | Generated byte-identical runtime resource. Never edit. |

Regenerate the runtime resource after any specification change:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Sync-BoardMapResource.ps1
```

`Tools/Sync-BoardMapResource.ps1 -Check` compares without writing. `Validate-BoardMapSpecification.ps1` asserts byte equality against the generated resource on every run and fails on drift, so a divergence cannot reach a build silently; milestone 6.1d added that comparison gate, and its completion record carries the account of the drift it exists to prevent.

The sync writes only the `.json`. `Assets/Resources/Maps/official-single-lane.json.meta` carries the asset GUID Unity resolves the resource by; regenerating that file would break the reference.

## Reviewed inventory

The diagram uses pointy-top integer axial `(q, r)` coordinates and shows 292 cells. Every touching pair of inventoried cells is a physical connection under the six existing standard axial directions. A playable connection is a physical connection whose two endpoints are playable; terrain endpoints are excluded.

| Classification | Cells |
| --- | ---: |
| Red Throne | 19 |
| Red Approach | 41 |
| Starting Battle Zone | 60 |
| Blue Approach | 41 |
| Blue Throne | 19 |
| Top Jungle | 15 |
| Bottom Jungle | 15 |
| Terrain | 82 |
| **Total** | **292** |

The seven colored classifications are represented as stable zones. Rulebook page 3 establishes that the board is divided into Zones, each Throne is a Zone, the Lane connects the two Thrones, and the middle Lane Zone starts as the Battle Zone. This inventory records zone membership and the starting-Battle-Zone flag without pre-implementing zone adjacency or Lane order.

The coordinate inventory derives 809 undirected physical connections and 530 undirected playable connections.

## Spawn points

The diagram contains exactly 40 spawn markers, split symmetrically as 20 Red and 20 Blue. Each team has three Hero, eleven Melee minion, three Ranged minion, and three Heavy minion spawn points.

| Team / type | Coordinates |
| --- | --- |
| Red Hero | `(-2,-5)`, `(-6,-4)`, `(-4,-4)` |
| Blue Hero | `(-3,10)`, `(-1,10)`, `(-5,11)` |
| Red Melee minion | `(-8,-2)`, `(-6,-1)`, `(-5,-1)`, `(-1,-1)`, `(-1,0)`, `(3,2)`, `(-11,4)`, `(-8,4)`, `(-4,4)`, `(2,4)`, `(-1,6)` |
| Blue Melee minion | `(-6,0)`, `(-9,2)`, `(-3,2)`, `(1,2)`, `(4,2)`, `(-10,4)`, `(-6,6)`, `(-6,7)`, `(-2,7)`, `(-1,7)`, `(1,8)` |
| Red Ranged minion | `(-9,1)`, `(-4,2)`, `(0,4)` |
| Blue Ranged minion | `(-7,2)`, `(-3,4)`, `(2,5)` |
| Red Heavy minion | `(-8,0)`, `(1,5)`, `(-7,6)` |
| Blue Heavy minion | `(0,0)`, `(-8,1)`, `(1,6)` |

## Review and validation

The source review covered every coordinate row, color classification, terrain interruption, zone boundary, and spawn symbol. Mechanical pixel/color and symbol-shape checks were used as transcription aids; the JSON, counts, and regenerated debug view remain the reviewed artifacts rather than the image-processing heuristics.

Run the narrow schema/content validator from the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1
```

The validator first applies reusable schema and invariant checks from `Tools/BoardMapSpecification.Validation.ps1`; that layer contains no official map ID, reviewed count, location, or digest. Two bounded in-memory fixtures deliberately reuse coordinates plus `spaceId`, `zoneId`, and `spawnPointId` under different synthetic `mapId` values to prove map scoping without retaining another playable map.

The official source-review layer then requires exact per-zone and terrain totals, direction metadata, derived connection totals, playable spawn locations, exact team/type symmetry, and the reviewed inventory digest. The digest protects all zone identity/kind/team/Throne/starting-Battle-Zone metadata, every space classification and membership, and every spawn identity/location/team/type. A digest change requires another source review rather than automatic acceptance.
