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

## Creeps and evidence

Standard creeps keep their existing sprite faces and board rotation. A circular team-colour body extends 0.16 world units beneath the face, staying above the board and retaining the same footprint, identity and selection path. Removal/respawn follows the existing parent view lifecycle.

Live screenshots: `tmp/w165-angled-live-match.png`, `tmp/w165-top-down-live-match.png`, `tmp/w165-standee-detail.png`, and `tmp/w165-creep-detail.png`. These are runtime captures, separate from W-164's generated product concept. Existing packages remain unchanged; no publication is authorized.
