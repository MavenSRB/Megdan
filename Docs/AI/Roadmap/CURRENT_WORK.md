# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md).

### [~] W-165 Implement illustrated standees and free board camera

User approved in-game standees matching W-164, a WASD/mouse camera, and automatic flat tokens at about 80 degrees above the board versus standees below that angle. Scope: six existing supplied portraits as upright cutouts with circular authoritative team-colour bases, camera pan/orbit/zoom/reset, angle-based visual switching and affected selection/HUD/spotlight compatibility. The user also approved solid creep-token thickness and visual-only upright cutouts for Veljko's bushes and mushrooms on the four grass variants. Grass decorations use the same 79/80-degree hysteresis as heroes: angled views show prepared standees over a clean grass face, while top-down views show the unchanged original flat tile; decorations add no colliders, obstacles or rules state. Validation additionally requires narrow launcher corrections for verified Unity Hub/auth-helper misidentification and Editor-parent waiting; cover the existing launcher contracts and helper regressions. Preserve identities, rules and existing packages; no publication. Closing checks: focused geometry/switch/input and affected camera/token/grass regressions, live Play Mode interaction and inspected angled/top-down captures, continuity/documentation validation, diff check and cohesive local commit.

Camera/hero/creep slice is complete and locally validated; evidence is in `work/W-165-camera-standees`. This task has finished its Unity runs. The separately added grass-decoration scope remains unfinished in the shared workspace and keeps W-165 active; preserve the concurrent W-168 card work. No package was produced.
