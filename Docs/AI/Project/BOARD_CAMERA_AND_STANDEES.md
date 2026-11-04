# Board camera and illustrated standees

Canonical active subject for W-165. Return to [project context](../PROJECT_CONTEXT.md) and [presentation](PRESENTATION.md). This is local presentation; match rules, identities, snapshots, replay and network messages do not change.

## Camera

The map owns a top-down whole-board home, including the HUD's safe-area fit. The existing serialized camera name remains `Fixed Camera`; composition attaches one `BoardCameraController` to it.

- WASD pans parallel to the board in camera-relative directions; Shift speeds up pan.
- Middle-mouse drag orbits; wheel zooms; Home restores the top-down whole-board home.
- Middle dragging must begin over the board. HUD scrolling does not zoom the camera. Text-field focus suppresses navigation; application focus loss cancels dragging. Right-click retains teammate suggestions.
- Navigation is orthographic and board-focused. Pitch stays 20–90 degrees above the board, zoom stays 0.12–1.7 times the home size, and the focus stays within the map's axis-aligned bounds plus two world units.
- Board redraws and HUD-safe-area changes refresh the home without replacing a manual view. Map replacement resets the view. The attack spotlight temporarily drives the camera; navigation is suspended while it does, and its return restores the prior manual view through the map's frame owner.

## Heroes

The six mapped Megdan heroes use upright illustrated panels on raised circular Red/Blue team bases when angled. At 80 degrees above the board they switch to their existing flat tokens; below 79 degrees they return to standees. The one-degree band retains the current representation to prevent flicker. This is an immediate switch, with no crossfade.

`Tools/Prepare-MegdanStandees.py` copies the supplied transparent portrait PNGs unchanged into private resources and derives thin silhouette wall geometry from their alpha channels. Original bust/head crops remain crops. The panels stay upright and turn only around the vertical axis to face the viewer, preserving the supplied image's left/right orientation. They have 0.045-world-unit edge thickness, maximum width 1.55 and maximum height 1.85; their base retains the existing 0.81 radius and 0.16 thickness. The cutout material template is tracked to retain its shader variant; the character images/derived outlines stay excluded and private.

Standee bases express authoritative team colour even when the seat has another HUD accent. Flat tokens keep their previous seat-accent rim. Missing standee resources retain the existing token/primitive fallback. One hero view owns both representations; board coordinates, root transforms, animation ownership, footprint and target-ring fitting stay unchanged. Picking extends up the standing panel. Generated meshes/materials are released with their owners.

## Grass decorations

The four supplied grass variants have temporary upright bush/mushroom panels in angled views. `GrassDecorationStandees` samples bounded regions of the unchanged original tile textures through `Rendering/GrassDecoration.shader`, rejecting grass-coloured pixels and clipping samples to the original hex footprint. Small patches sampled from the same tile cover the lifted paintings beneath the panels. These are temporary colour-keyed cutouts, with the supplied edge crops retained.

All decorations switch together at 80 degrees to the original flat tile, returning below 79 degrees with the same hysteresis band as heroes. Panels turn around the vertical axis toward the board camera. The layer adds no colliders or rules objects and applies only to tiles using one of the four grass textures. Two meshes and per-decoration materials are shared across the board; rebuild replaces its children and releases owned resources. Missing private grass textures retain the existing board fallback.

Four-variant runtime inspections: `tmp/grass-standees-restart-angled.png` and `tmp/grass-standees-restart-top-down.png`. The abandoned raster-extraction script/output remains private historical work and is not consumed by runtime.

W-170 captures the actual configured match at 25 degrees with the normal HUD: `tmp/w170-grass-low-angle-live-match.png` and the closer `tmp/w170-grass-low-angle-detail.png`. W-171 refreshes these after adding piece clearance. W-172 supersedes its flat-plant fallback: only the intersecting panel disappears; the cleanup patch stays visible so no flat painted plant returns in an angled view. The check follows visible hero, minion and placed-token footprints each frame, including a small margin, and restores upright decorations after a piece moves away. Clearance tests the upright panel's ground segment against each piece footprint; neither the larger cleanup patch nor empty corners of a rotated panel's bounding box hide a bush. Inactive panel bounds are calculated from transforms so hiding cannot invalidate clearance. At 80 degrees the entire decoration layer, including cleanup patches, switches off to reveal the untouched original flat tile. This remains presentation-only.

Current same-camera W-172 stills: `tmp/w172-grass-low-angle-live-match.png` and `tmp/w172-grass-low-angle-detail.png`. Earlier W-170 captures remain preserved for comparison.

## Creeps and evidence

Standard creeps keep their existing sprite faces and board rotation. A circular team-colour body extends 0.16 world units beneath the face, staying above the board and retaining the same footprint, identity and selection path. Removal/respawn follows the existing parent view lifecycle.

Live screenshots: `tmp/w165-angled-live-match.png`, `tmp/w165-top-down-live-match.png`, `tmp/w165-standee-detail.png`, and `tmp/w165-creep-detail.png`. These are runtime captures, separate from W-164's generated product concept. Existing packages remain unchanged; no publication is authorized.

## Defense indicators

A compact pointer-pass-through Defense breakdown appears for the hovered or keyboard-focused legal Defense choice. Moving across choices updates the same preview without changing authoritative state; clicking retains the current one-click typed response. The immutable application projection supplies Attack and per-choice Defense contributions, totals, explicit Block/result meaning, and player-readable source identities, while presentation only formats them. The popup uses a fully opaque background plus explicit `ATTACK:`, `DEFENSE:`, and `RESULT:` sections, avoids the focused control, remains inside the viewport, and uses compact columns when a long breakdown needs bounded vertical space. Every standing hero body carries a tapered shield lying horizontally just above its base, at a fixed left-side rim offset relative to the rotating standee; the shield and its numeral follow the same heading as the standee. The shield is 0.572 token-radius wide and 0.754 long with a black outline and light-grey face centered on its screen-left token rim, matching the card-pip rim centerline without a downward offset, showing its centered signed net standard-minion modifier (`-2`, `0`, `+1`) at all times; three independent default-off Experimental options may add held Defense items, Active effects, and card/continuous effects. It never shows `DEF` or the printed Defense value. The existing token art and player-colour rim are unchanged. Card-state pips keep their 0.12-unit radius and rim centerline, with hand-pip centers 0.28 units apart for a visible 0.04-unit gap. `HeroMinionDefenseQuery` derives immutable per-body readings from the public board using the same `DefenseValueQuery` as combat, once per refreshed projection; joined clients need no private card identities or new wire fields. Ordinary board hover and linked rail hover tint only those contributing minions green for positive and blood red for negative, with no minion numbers. The same reading and hover work during Attack target selection. Hover exit clears the contributor tints while every standing hero keeps its shield. Movement, removal, Undo and restore rebuild the readings from the current public board without submitting a command. Once the Defense response opens, its already-calculated attack-scoped minion sources remain the board's green/red six-corner contribution cues while the player moves across cards or Decline; those cues clear or rebuild only when the pending response or projection changes. Decline previews no Defense Total without changing the attack-scoped cues. Presentation never recalculates adjacency, teams, or Defense.
