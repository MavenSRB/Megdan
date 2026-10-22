# Megdan vertical-slice production inventory

Return to the [Smart Start preparation owner](SMART_START.md). This document converts the approved complete vertical-slice outcome into quotation units. It is not a budget, purchase order, art brief or implementation authorization. Prices, people and delivery dates remain open until written offers and team capacity are confirmed.

## Inventory boundary

The funded result is one installable, stable, publisher-ready **3v3** match on one original production-quality map. It contains six final slice heroes, all presentation needed to play their ordinary three-player-team decks, the complete hybrid 3D-world/2D-overlay interface, required animation/VFX and audio, and the technical, player and rights evidence already defined in the [delivery plan](SMART_START_PLAN.md).

This inventory counts production deliverables, not every runtime instance. For example, the board can display twelve minions while production may use three minion model families with team variants. Conversely, 108 different playable card faces require 108 final layouts even if the illustration plan deliberately reuses motifs. The costing sheet must preserve that distinction.

## Six-hero content baseline

The public identities are Svarog, Stribog, Morana, Mokoš, Veles and Perun. Their internal source kits are [Brogan](../Rules/HERO_BROGAN.md), [Swift](../Rules/HERO_SWIFT.md), [Misa](../Rules/HERO_MISA.md), [Tali](../Rules/HERO_TALI.md), [Tigerclaw](../Rules/HERO_TIGERCLAW.md) and [Wasp](../Rules/HERO_WASP.md) respectively. The source records for every selected kit contain nineteen printed faces: an ordinary eighteen-card deck plus one Handicap face returned to the box in ordinary setup.

| Deliverable | Required slice quantity | Optional / quotation alternative | Costing note |
| --- | ---: | ---: | --- |
| Playable action-card face layouts | 108 (6 × 18) | — | Fixed for an ordinary 3v3 match. Gameplay values stay unchanged; names, copy presentation, identity and visual design become original. |
| Handicap face layouts | 0 | 6 (1 per hero) | Not used in the bounded 3v3 slice. Quote separately if a complete nineteen-face hero set is desired. Including them makes 114 faces. |
| Unique card illustrations | Not yet fixed | Between a directed reusable/key-art system and as many as 108 playable-face illustrations; up to 114 with Handicaps | A face count is not automatically a unique-illustration count. Choose and quote the art-reuse policy before the budget is approved. |
| Hero deck backs/emblems | 6 | — | One coherent identity per hero; repeated across that hero's deck. |
| Hero dashboard fronts | 6 | — | Final 2D overlay presentation. A common reverse/neutral state may be shared if the interface needs it. |
| Hero selection/profile fronts | 6 | — | Final roster-selection presentation. A common branded back may be shared. |
| Hero concepts/model sheets | 6 | — | One approved original visual identity per hero, suitable for both 2D and 3D production. |

The fixed **108 face layouts** are the sound basis for quoting UI/layout work. The number of commissioned illustrations is a separate art-direction decision and must never be silently assumed to equal 108. The six optional Handicap faces are outside the strict slice and must not enter the grant promise unless scope and budget explicitly include them.

## Rules components and board pieces

Five selected kits add no hero-owned board token supply. Mokoš/Tali and Veles/Tigerclaw are the two component-bearing cases:

- **Mokoš/Tali:** the [verified component definition](../../../Assets/GuardsDigital/Core/Cards/Verified/TaliComponentContent.cs) declares one shared supply of three physical tokens serving Ice, Ancestral Totem and Venerated Totem. The slice therefore needs **three simultaneous 3D token instances** and three unmistakable semantic states. Quote either one reusable model family with three state treatments or distinct meshes where readability/art direction requires them; do not cost nine pieces.
- **Veles/Tigerclaw:** the [verified component definition](../../../Assets/GuardsDigital/Core/Cards/Verified/TigerclawComponentContent.cs) declares one shared supply of **six Poison markers** supporting two rule strengths (-1 and -2). These markers attach to hero state rather than occupy map spaces. They require two clearly readable final UI states and six possible simultaneous instances. They are not automatically six unique 3D models; quote a 2D dashboard/state treatment unless the approved presentation deliberately materializes them in the world.

The selected ordinary single-lane rules use three minion archetypes: **Melee, Ranged and Heavy**. The current 3v3 setup places four Melee, one Ranged and one Heavy per team, or twelve starting instances across both teams. Production baseline is therefore:

- three final minion model/rig families;
- Red/Blue or equivalent original team-readable material/state variants for each family;
- enough runtime instances for the final map's reviewed placement and lane-push supply.

The original map has not yet been designed, so its final minion spawn count and reserve demand cannot be copied from a legacy map or priced as a settled quantity. Map review must close the exact instance maximum while preserving the three archetypes.

## 3D world and camera

### Fixed production units

- six unique final hero models with textures/materials and game-ready rigs;
- three game-ready minion model/rig families with team-readable variants;
- one production-quality original map/environment kit;
- three simultaneous Mokoš token pieces with three readable states;
- world-space selection, legal/illegal target, path/movement, attack, damage/block, defeat/respawn, lane/minion-battle, progression/ultimate and victory feedback;
- collision, selection and readability setup for every interactive world object;
- LOD/performance treatment appropriate to the agreed target PC configuration.

### Map quantities that require an approved design

The quotation cannot responsibly fix the number of hex/space meshes, zone treatments, spawn points, structures, terrain sets, environmental props or set-dressing pieces until the original 3v3 topology and art direction are approved. The map brief must provide:

1. reviewed topology and exact playable-space/zone/spawn inventory;
2. modular-versus-unique environment strategy;
3. prop list and reuse counts;
4. navigation/collision/occlusion requirements for the free camera;
5. performance target and representative target hardware.

The final map may reuse modular pieces, but it cannot reuse protected expression from the private reference map. “One map” is the fixed deliverable; its asset count is the quotation blocker.

### Camera acceptance baseline

The free camera needs mouse/keyboard movement, rotation/orbit and zoom; bounded traversal that cannot lose the playable board; focus on the selected/acting unit and relevant action; unobstructed return/home framing; readable transitions during attacks, movement, lane resolution and victory; and settings/usability checks for speed, inversion or equivalent accessibility needs where supported. Cinematic replay, spectator production tools and controller/mobile controls are outside scope unless separately approved.

## Animation and VFX quotation units

Each hero requires a reusable locomotion/combat set sufficient for idle, movement, basic attack/action, receiving a hit, defeat and return/respawn, plus bounded signature ability presentations. Each minion family requires idle, movement, attack, hit and defeat, with any genuinely distinct Heavy/Ranged behavior itemized. The environment requires only interactions visible in the final match.

The number of hero-specific signature clips is not derivable from the 108 card faces: several cards can share motion language, while one complex ability may require several clips. Before requesting offers, create an action-presentation matrix that maps all 108 playable faces to:

- shared base animation only;
- shared animation plus unique VFX/state treatment;
- one or more hero-specific signature clips;
- no additional world animation because the effect is entirely UI/rules state.

Quote the resulting unique clips and VFX packages, not “one animation per card.” Reusable VFX families should cover selection/targeting, melee impact, ranged projectile, movement/path, damage/defense, defeat/respawn, token/totem state, poison state, lane/minion battle, level-up/ultimate and match victory. Exact variants follow the matrix and approved visual language.

## 2D overlay and UX

Beyond the six hero sets, quote one coherent final slice UI system covering title/loading, host/join and six-seat lobby state, hero selection, in-match hand and hero panels, turn/initiative/current-card presentation, card inspection, choices and targeting prompts, scoreboard/team/victory state, settings, reconnect/error feedback and necessary tooltips. Include typography, iconography, buttons, panels, cursors and responsive states required by the supported target resolution.

This is a screen/state inventory, not permission to redesign rules. The current prototype proves interactions; the final UI must preserve comprehension, seat ownership, hidden information and reconnect/resynchronization state. Localization production beyond the submission/demo language is outside the slice, but layouts should avoid needless lock-in.

## Audio work package

The prototype has no game audio. Quote future production from zero as a separately auditable package:

- final music for menu/lobby and match flow, with transition/loop requirements;
- one map ambience system;
- UI feedback for navigation, confirmation, invalid action, card/choice/turn, timer/reconnect and match result;
- gameplay SFX for movement, melee/ranged actions, impacts, defense/damage, defeat/respawn, minions/lane resolution, hero/token/marker states, progression/ultimate and victory;
- integration, loudness/mix pass, looping, ducking and in-build review.

An audio event map must turn those families into exact clip/variation counts before offers are compared. **Voice/barks are excluded from the baseline** and may enter only as a separate, explicit quotation with language, casting, recording, editing and implementation assumptions. No audio work is expected before award.

## Engineering, evidence and delivery

The production quote and schedule must retain non-art completion work:

- integrate final 2D/3D/audio assets without moving rules authority into presentation;
- support install, host/join, one complete six-player 3v3 match and terminal victory on the defined Windows target;
- verify affected reconnect/resynchronization, deterministic state, snapshot and replay/Undo behavior;
- profile the final map, camera, animation/VFX and UI against agreed performance and readability targets;
- run structured usability and target-player sessions with recorded denominators and limitations;
- produce the installable build, reproducible technical/player evidence summary, known-issues disposition and publisher demonstration;
- maintain a rights manifest for original work, contractors, dependencies, fonts, audio, AI-assisted placeholders and final replacement status.

Public matchmaking/service infrastructure, ranked play, additional maps/modes, the approximately twenty later heroes, full localization, console/mobile ports and launch marketing remain outside this inventory.

## Decisions required before a priced budget

1. Approve the original map topology/art-direction brief and its exact space, zone, spawn and prop inventory.
2. Choose the card-illustration reuse level; then count unique commissioned illustrations independently of 108 layouts.
3. Complete the 108-face action-presentation matrix and count unique animation/VFX units.
4. Confirm whether the six Handicap faces are included or remain post-slice.
5. Confirm Veljko's deliverable ownership and capacity across concept, 2D, 3D, rigging and animation; quote uncovered disciplines.
6. Complete the audio event map and obtain a baseline no-voice offer plus an optional voice/barks offer.
7. Fix target PC/resolution, performance criteria and the precise camera acceptance test.
8. Obtain comparable written offers, map each cost to an eligible Smart Start category and exclude all self-funded/pre-contract work.

Only after those decisions should the illustrative numbers in the delivery plan be replaced by a requested budget.
