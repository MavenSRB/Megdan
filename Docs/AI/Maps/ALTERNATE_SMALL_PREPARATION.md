# Alternate small map: implementation preparation

Preparation owner [9.63](../Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md#x-963-prepare-the-alternate-2v23v3-map); implementation owner [9.61](../Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md#x-961-implement-the-alternate-map-for-2v2-and-3v3). Return to [map index](README.md). The session brief owns authorization. This preserves the **review candidate** and its provenance. 9.61 now promotes the unchanged inventory to [canonical runtime source](alternate-small-single-lane.map.json); its source/UI gate is complete; the merged-player build follows separately. The final hero's source and tests are untouched. [9.63a](../Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md#x-963a-complete-the-small-map-terrain-border) corrects the spawn-zone terrain border confirmed by the user.

## Prepared inventory

Candidate identity: `alternate-small-single-lane@1`, display label `Alternate Small Map` (a filing/UI description, not a claimed official name). The files in [Preparation/](Preparation/) contain:

- [Complete candidate map](Preparation/alternate-small-single-lane.candidate.map.json): each axial coordinate, canonical space ID, playable/terrain status, zone and spawn reference, using the existing map specification schema.
- [Artwork registration](Preparation/alternate-small-single-lane.registration.json): source hash, pixel transform, terrain appearance and outer-boundary review markers. Water and rock both remain authoritative terrain; their appearance does not add a rule.
- [Initial minion placement references](Preparation/alternate-small-single-lane.initial-spawns.json): the twelve spawn records in the starting Battle Zone. These are references to the candidate, not copies of current-map coordinates.

| Classification | Cells |
| --- | ---: |
| Blue Throne | 17 |
| Blue Approach | 35 |
| Starting Battle Zone | 52 |
| Red Approach | 35 |
| Red Throne | 17 |
| Terrain: water / rock / spawn-zone border | 30 / 50 / 22 |
| Total / playable | 258 / 156 |

The lane order is Red Throne -> Red Approach -> Starting Battle Zone -> Blue Approach -> Blue Throne. Only consecutive zones share playable borders. This map has one active Battle Zone and no Jungle zones. The whole inventoried graph, playable graph and each playable zone are connected. There are 709 undirected physical edges and 404 playable edges under the six standard axial directions.

Each team has 3 hero, 11 melee, 3 ranged and 3 heavy spawns (40 total). Each side starts with four melee, one ranged and one heavy minion in the Battle Zone, consistent with the existing p.6 setup. The two former towers are **blue ranged at `(0,-6)`** and **red ranged at `(-1,7)`**. Hero spawns are on the corresponding Throne. All spawn coordinates are unique and playable. The complete inventory and spawn types have half-turn symmetry: `(q,r) -> (-q-1,1-r)` with teams exchanged.

## Artwork alignment and review

Use the preserved edited `alternate-2v2-3v3-ranged-spawns-2026-10-05.png`, not the tower-bearing original. Received artwork is 900 x 900; do not claim a higher-resolution source. Images remain in the ignored `Docs/Guards of Atlantis II - Map References/` directory. Full-size private review sheets there are `alternate-small-hex-review-9-63.png` and `alternate-small-spawn-review-9-63.png`; `alternate-small-review-9-63a.py` reproduces them from the corrected candidate. Earlier review images are preserved as `alternate-small-hex-review-before-9-63a.png` and `alternate-small-spawn-review-before-9-63a.png`; the original recipe remains preserved. Original art and prior edits are unchanged.

Pixel origin is top-left, x right/y down. Map centre `(0,0)` is the upper internal central-rock hex. Registration is:

`pixelX = 453.4 + 42.04*q + 29.49*r`

`pixelY = 424.4 - 9.78*q + 31.52*r`

Adjacent-centre spacing is about 43.16 pixels, outer radius 24.92 pixels. Basis vectors have equal lengths and a 60-degree separation to rounding precision. Repeated texture translations were measured separately in upper/lower/full regions and over four/eight spacings; all playable rows and all 40 glyphs were then inspected on the overlay. Painted glyphs may sit slightly off their actual hex centres: use the grid registration, not each glyph's bounding box, for placement and picking.

**Terrain border confirmed at 9.63a:** every playable cell must have all six neighbour coordinates inventoried. The initial candidate missed 11 terrain cells around each Throne; the corrected candidate adds those 22 cells, highlighted yellow on the review overlay. They are terrain with no zone or spawn, including where the picture shows throne machinery or water at the edge. All 156 playable cells, five zones and 40 spawns remain unchanged. Registration metadata lists every added space ID and its user-confirmed provenance.

This image has no publisher/user-supplied axial diagram. At 9.61, the complete overlay, forty magenta outer-rock markers and supported-size runtime artwork were visually reviewed; the prepared inventory is unchanged. The map ends beyond the complete terrain border; decorative peaks, ships and exterior sea do not generate further cells. The complete-neighbour check must fail if any necessary border cell is removed.

## Prepared integration seams

Inspected baseline `6438ab66`; unfinished NebKher changes in the shared checkout are not final gate evidence. Recheck source delta after 9.54, 9.58 and 9.56; choose schema/wire changes from the landed implementation.

| Existing source | Required bounded change at 9.61 |
| --- | --- |
| `Presentation/Board/BoardMapJsonLoader.cs`: `OfficialBoardMapCatalog` | Catalog both map definitions. Resolve the chosen `(mapId, revision)` instead of one static `SelectedMap`; reject unknown/duplicate identities. |
| `Core/Setup/HeroLineupComposition.cs`: `Compose`, `PlacementsFor`; `RegisteredHeroRoster` slots | Resolve hero slots from the selected map's same-team hero spawns. Preserve existing-map positions; define stable per-map slot order. 3v3 uses three slots; 2v2 uses the first two in that reviewed order. Keep hero identity separate from coordinates. |
| `Core/Setup/QuickGameSetup.cs`: `CreateOfficial`, `OfficialMinionPlacements` | Existing production setup contains literal current-map coordinates. Derive this map's initial minions from its starting zone and typed spawns, retaining stable minion IDs; do not merely substitute a map ID around old coordinates. Preserve Wave/Life counts for all four existing Quick/Long formats. |
| `Presentation/Interaction/MatchSceneCompositionRoot.cs` | Carry map selection alongside format/lineup through composition, scenario, available maps, restart and recomposition. Keep deliberately staged diagnostic scenarios on their explicit maps. |
| `Presentation/Interaction/PrivateLobbyViews.cs`: host menu, `BeginHosting`, format-change refusal and inheritance | Add an ordinary **Map** selector on the host screen, beside format settings, offering current map and Alternate Small Map for both 2v2/3v3. Default a fresh host to the existing map. Freeze selection when hosting starts. Show the chosen map to joining players. The map is a standard game option: this selector is directly visible on the host screen, never inside the Experimental menu, and available with all experiments off. |
| `Application/PrivateOnline/PrivateSessionIdentity.cs`, `PrivateSessionFrameCodec.cs`, host admission | Map ID/revision already form identity. Joiners must compose the host's supported map before compatible admission; current format-only inheritance is insufficient. Unknown maps/revisions fail clearly; never suppress identity checks or fall back to a different map. Reconnect preserves map selection. |
| `Application/MatchSessionInitialState.cs`, `MatchAggregate.cs`; `Core/Replay/InitialScenarioDescription.cs`; `Core/Snapshots/AuthoritativeSnapshotSerializer.Setup.cs` | Supply both definitions to restore/replay, retain the chosen immutable map and validate consistency. Existing save/replay map fields are reusable; no version is reserved during preparation. |
| `Presentation/Board/OfficialBoardArtwork.cs`, `OfficialBoardUnderlayView.cs`, `PlaceholderTestGridView.cs` | Resolve artwork/calibration by map reference. Current underlay and camera frame use official-only constants. Apply the prepared transform to both rendering and picking/camera bounds; keep generated geometry as the missing-private-art fallback. |
| `Tools/Sync-BoardMapResource.ps1`, `Validate-BoardMapSpecification.ps1`; `Tools/DotnetTests/Shims/BoardMapCatalogShim.cs` | Extend source/resource parity and offline catalog coverage for the additional reviewed map. Promote only the accepted candidate to canonical/runtime paths. Never hand-edit generated resources or publish private PNGs. |

Paths in the table are relative to `Assets/GuardsDigital/` except `Tools/` paths. Use map-local IDs and existing typed queries throughout; do not branch on a hero/card name. Single-lane progression already has an ordered-zone contract; two-lane changes are outside this milestone.

## Closing checks for implementation

1. Data: exact canonical/resource parity, registration anchors, unique/valid references, symmetry, counts, connected zones and all six neighbour coordinates present for every playable cell (including both Thrones). Load both maps in the actual runtime catalog and offline shim. Deliberately wrong map/revision, duplicate IDs and a spawn on terrain must be rejected.
2. Setup/host: the Map selector is visible without opening Experimental and works with every experiment off; both maps crossed with 2v2/3v3 and Quick/Long; selector changes produce correct heroes/minions and no stale pieces/artwork. Defaults, disabled-after-hosting state, bots and lineup rebuilds preserve selection. Joiners inherit the selected map; unsupported identities fail without loops.
3. Geometry/rules: all five zone borders, path blocking by the four internal rocks and water, adjacent/radius/line queries, Fast Travel, targeting terrain, hero respawn including occupied-spawn fallback, all minion types and Lane Push in both directions through to either Throne. Queries over absent Jungle zones return valid empty results. Audit final NebKher straight-line/board partitioning against this geometry.
4. Persistence/network: save/load, reconnect, replay, ordinary Undo and Director Undo on this map; wrong-map snapshots refused. Timer and coordination on/off combinations use the selected map's legal choices, timeout destinations and highlight positions without leaking opposing hands or stranding a match.
5. Presentation: every tile's centre/corners pick the expected hex, terrain targets remain visible, all 40 spawn glyphs align, camera frames the entire square artwork at supported viewports, and no UI overlap or header growth. Missing private artwork still permits play with generated geometry.
6. Regressions: existing map/format behavior, setup identity, map catalog/shim, artwork, host/join and restore. Existing fixture anchors include `BoardMapDefinitionTests`, `OfficialFourPlayerSetupTests`, `OfficialSixPlayerSetupTests`, `MatchFormatTests`, `MatchFormatAdmissionTests`, `PrivateLobbyFrontDoorTests`, `PrivateLobbyPlayModeTests`, `OfficialBoardArtworkTests`, `FastTravelActionTests`, `HeroRespawnTests`, `LanePushTransitionTests` and `AuthoritativeSnapshotSerializerTests`. Choose the highest applicable tier after final affectedness review; this cross-cutting playable-map release is an explicit player-path gate, not a data-only pass.

Preparation uses the existing `Test-BoardMapSpecificationInvariant` function directly, plus candidate-only reference, connectivity, symmetry and initial-force checks. It does not run the official-map wrapper against a candidate or create a runtime resource. Gameplay/Unity evidence belongs to 9.61, after the preceding authorized milestones.

The border regression `python tmp/map-border-9-63a/check-complete-border.py <candidate-path>` is preserved as private preparation evidence: it checks the complete-neighbour invariant independently of the data update recipe. Promote this invariant into persistent runtime map tests at 9.61.
