# Megdan funded-MVP production inventory

Return to the [Smart Start preparation owner](SMART_START.md). This document converts the approved six-hero 3v3 MVP outcome into quotation units. It is not a finished-commercial-product specification, budget, purchase order, art brief or implementation authorization. Prices, people and delivery dates remain open until written offers and team capacity are confirmed.

## Redesign clarification — 31 October 2026

The applicant will replace all art and redesign the heroes, retaining only the card concept. Source hero mappings below describe the private prototype and prior sizing basis, not final retained designs. The six-hero target remains; 108 faces, token supplies and source-specific animation requirements are provisional until the redesigned decks/components are specified. Do not silently implement a rule change or claim those redesigned contents already exist. Reconcile quantities and supplier coverage when designs are fixed.

## Inventory boundary

The funded result is one installable, stable, original and rights-cleared **3v3 MVP** on one coherent original map. It contains six complete playable heroes, the presentation needed to understand and evaluate their ordinary three-player-team decks, a coherent hybrid 3D-world/2D-overlay interface, bounded animation/VFX and functional audio, and the technical, player and rights evidence defined in the [delivery plan](SMART_START_PLAN.md). Premium polish, broader content and launch infrastructure are later commercial-production work.

Compatible rigs, core motions and modular environments are the baseline; extra visual variety is polish. The application uses the existing prototype plus original concepts, with no pre-application 3D requirement. Rights-required redesign can change this source-based inventory and must be recosted explicitly.

This inventory counts production deliverables, not every runtime instance. For example, the board can display twelve minions while production may use three minion model families with team variants. Conversely, 108 different playable card faces require 108 complete layouts and 108 distinct original MVP-quality compositions, even when their production deliberately reuses original models, environments, materials, effects or other source elements. The costing sheet must preserve that distinction without treating them as 108 premium paintings.

## Six-hero content baseline

The public identities are Svarog, Stribog, Morana, Mokoš, Veles and Perun. Their historical prototype reference kits are [Brogan](../Rules/HERO_BROGAN.md), [Swift](../Rules/HERO_SWIFT.md), [Misa](../Rules/HERO_MISA.md), [Tali](../Rules/HERO_TALI.md), [Tigerclaw](../Rules/HERO_TIGERCLAW.md) and [Wasp](../Rules/HERO_WASP.md) respectively. The source records for every selected kit contain nineteen printed faces: an ordinary eighteen-card deck plus one Handicap face returned to the box in ordinary setup.

| Deliverable | Required slice quantity | Optional / quotation alternative | Costing note |
| --- | ---: | ---: | --- |
| Playable action-card face layouts | 108 (6 × 18) | — | Fixed for an ordinary 3v3 match. Current source-based quotation baseline; retained mechanics/content are conditional on rights review. Any necessary redesign must update counts and costs explicitly. Names, wording and visual expression require original/cleared treatment. |
| Handicap face layouts | 0 | 6 (1 per hero) | Not used in the bounded 3v3 slice. Quote separately if a complete nineteen-face hero set is desired. Including them makes 114 faces. |
| Distinct original MVP-quality card compositions | 108 (1 per playable face) | 6 more if Handicaps enter scope | Every playable card receives its own readable composition. Production may reuse rights-cleared original 3D models, poses/rigs, environments, materials, VFX elements and graphic motifs; reuse cannot result in duplicate card art. Premium standalone illustration is not required for each face. |
| Hero deck backs/emblems | 6 | — | One coherent identity per hero; repeated across that hero's deck. |
| Hero dashboard fronts | 6 | — | Coherent MVP 2D overlay presentation. A common reverse/neutral state may be shared if the interface needs it. |
| Hero selection/profile fronts | 6 | — | Coherent MVP roster-selection presentation. A common branded back may be shared. |
| Hero concepts/model sheets | 6 | — | One approved original visual identity per hero, suitable for both 2D and 3D production. |

The provisional source-based sizing baseline is **108 face layouts plus 108 distinct original MVP-quality compositions**, subject to the redesign clarification above. This does not require 108 unrelated paintings made from zero. A costable card system may pose/render the six original hero models, reuse original environment and VFX libraries, composite those sources and apply Veljko's art direction/paintover so that each card still has a unique, readable image. The chosen method must record source files, human contribution, tool terms and secured rights. The six optional Handicap faces/artworks are outside the strict MVP and must not enter the grant promise unless scope and budget explicitly include them.

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

- six distinct MVP-ready hero models with textures/materials and game-ready rigs;
- three game-ready minion model/rig families with team-readable variants;
- one coherent original MVP map/environment kit;
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

Beyond the six hero sets, quote one coherent MVP UI system covering title/loading, host/join and six-seat lobby state, hero selection, in-match hand and hero panels, turn/initiative/current-card presentation, card inspection, choices and targeting prompts, scoreboard/team/victory state, settings, reconnect/error feedback and necessary tooltips. Include typography, iconography, buttons, panels, cursors and responsive states required by the supported target resolution.

This is a screen/state inventory, not permission to redesign rules. The current prototype proves interactions; the final UI must preserve comprehension, seat ownership, hidden information and reconnect/resynchronization state. Localization production beyond the submission/demo language is outside the slice, but layouts should avoid needless lock-in.

## Audio work package

The prototype has no game audio. Build the functional MVP layer from the smallest coherent beneficiary-licensed stock set and a recorded event map:

- functional MVP music for menu/lobby and match flow, with transition/loop requirements;
- one map ambience system;
- UI feedback for navigation, confirmation, invalid action, card/choice/turn, timer/reconnect and match result;
- gameplay SFX for movement, melee/ranged actions, impacts, defense/damage, defeat/respawn, minions/lane resolution, hero/token/marker states, progression/ultimate and victory;
- Stefan-owned selection, trimming/layering where licensed, integration, loudness/basic mix, looping, ducking and in-build review.

An audio event map must turn those families into exact clip/variation counts before purchase. The provisional asset-license ceiling is RSD 30,000 excluding VAT. Select standard-license, non-restricted assets in the beneficiary's name; preserve listing, publisher, invoice, EULA/license version, entity, asset version/hash and exact used-clip records. Do not redistribute source files to a publisher or contractor unless their license permits it. **Voice/barks, custom recording, custom composition and a separate audio contractor are excluded from the funded MVP.** No audio work is expected before award.

## Project equipment and test infrastructure

Budget one **new display drawing tablet for Veljko** as the priority small-equipment item for nine months of illustration, card composition, UI and paintover work. Procurement specification: brand-neutral 15.6–16-inch fully laminated QHD-class display; approximately 99% sRGB coverage; tilt-capable battery-free pen; Windows compatibility; necessary cables; adjustable stand or equivalent ergonomics; documented local warranty/service; beneficiary invoice. The dated Serbian-market scan supports an **RSD 85,000 eligible-cost ceiling excluding VAT** (approximately RSD 102,000 gross at 20% VAT). Replace the ceiling with the accepted compliant quotation and record ownership/acceptance. Quote a **new computer** separately as optional: include it only if quotation-backed eligible capacity remains after core external production, audio, hosting and support. Do not infer residual capacity, specify premium equipment without workload evidence or buy used/refurbished goods.

Budget **time-bounded multiplayer test-server hosting** only for development and structured validation: one nine-month VPS with a minimum 2 vCPU/4 GB RAM/40 GB storage class and headroom up to 4 vCPU/8 GB/80 GB, one public IP, compatible Linux or Windows environment, bounded backup/snapshot/monitoring and retained diagnostics. It supports repeatable six-player sessions and reconnect/resynchronization checks, not public matchmaking, commercial live-service capacity or post-project hosting. The provisional ceiling is RSD 60,000 excluding VAT. Activate it only after Stefan demonstrates a compatible dedicated test-host route and the Fund/accountant confirms the eligible category; the current ordinary playing-host topology alone does not justify a VPS invoice. Replace the ceiling with a provider, region, exact resources, term, retention and shutdown quotation.

## Engineering, evidence and delivery

The production quote and schedule must retain non-art completion work:

- integrate approved MVP 2D/3D/audio assets without moving rules authority into presentation;
- support install, host/join, one complete six-player 3v3 match and terminal victory on the defined Windows target;
- verify affected reconnect/resynchronization, deterministic state, snapshot and replay/Undo behavior;
- profile the MVP map, camera, animation/VFX and UI against agreed performance and readability targets;
- run structured usability and target-player sessions with recorded denominators and limitations;
- produce the installable build, reproducible technical/player evidence summary, known-issues disposition and publisher demonstration;
- maintain a rights manifest for original work, contractors, dependencies, fonts, audio, AI-assisted placeholders and final replacement status.

Public matchmaking/service infrastructure, ranked play, additional maps/modes, the approximately twenty later heroes, full localization, console/mobile ports and launch marketing remain outside this inventory.

## Production ownership matrix

“Owner” means accountable for the deliverable and its acceptance evidence; it does not imply that one person performs every task. Provisional rows need evidence or a named provider before the budget is approved.

| Work package | Planning owner | Status and budget treatment |
| --- | --- | --- |
| Technical architecture, gameplay/network implementation, tools, asset integration, builds and technical QA | Stefan | Confirmed full-time nine-month commitment, with freelance work fitted around Megdan. Planning basis: 1.0 FTE, approximately 173.33 hours/month and 1,560 hours total. Prior RSD 240,000/month and RSD 2,160,000 total remain unconfirmed envelopes; actual contract and payroll cost require professional calculation and any program-specific confirmation. |
| Hero concepts, 2D art direction, 108 distinct original MVP-quality card compositions, card layouts, deck identities, dashboards/profiles and coherent MVP 2D UI assets | Veljko | Confirmed ownership at 40 hours/week across nine months: 1.0 FTE, approximately 173.33 hours/month and 1,560 hours total; preferred route is nine-month fixed-term full-time employment at provisional RSD 270,000 employer total (`bruto 2`)/month and RSD 2,430,000 total. His reviewed portfolio and follow-ups support illustration, graphic/UI/UX design, promotional composition and art-direction relevance, including reported complete-concept ownership of the Wacky Sheep illustration and Serbian Games Week poster. Production method and acceptance batches remain required; the same deliverables must not also be bought from a contractor. |
| Character/minion/environment 3D modelling and texturing; rigging and bounded animation | Veljko: visual lead/models; external supplier: commissioned animation; Stefan: integration; required rigging/skinning included in supplier package per applicant | Veljko retains visual direction and model/texture responsibility; commissioned animation is external, with required rigging/skinning included in supplier package per applicant. The reviewed 10-second turntable shows a completed stylized character with coherent form, costume/accessory construction and material/colour treatment from all angles, supporting a bounded modelling/texturing capability. It does not expose topology/process, rigging, deformation, animation, VFX or production throughput. His map → six portraits → minions → 108 cards sequence also omits those acceptance details. The monthly plan therefore adds a Month 1 representative pipeline gate and interleaved complete hero packages. Obtain a pre-application quote for a funded-period representative rigging/animation pilot: essential idle, movement, attack and hit motions verified in Unity. Veljko defines model/rig/visual requirements and Stefan gameplay/Unity acceptance. Reuse compatible rigs and core motions; RSD 835,000 is allocated provisionally to external animation, with no contractor selected. Required rigging/skinning and animation are included per applicant; clip coverage, delivery and rights need written quote documentation; payroll scope must not duplicate purchased work. |
| Production VFX | Veljko | Confirmed planning ownership within the complete visual package. Define and count the bounded MVP effects before acceptance; any additional specialist scope needs a separate budget decision and non-overlapping deliverable; the former reserve is allocated to animation. |
| Licensed music/ambience and UI/gameplay SFX; integration/basic mix | Stefan; marketplace licensor supplies source assets | Provisional RSD 30,000 excluding-VAT asset ceiling. Stefan owns event mapping, selection, permitted edit/layer work, integration and in-build acceptance. No voice, custom composition/recording or audio contractor. Confirm eligible category and preserve the complete license/provenance record. |
| Original game/rules design and map topology | Stefan; Veljko owns visual realization | Confirmed founder ownership. Stefan approves gameplay, topology and rules readability; Veljko owns the map's visual direction, environment modelling and presentation within the complete visual package. |
| Target-user research, playtest validation, business model and publisher-readiness narrative | Stefan during the funded project; Veljko supports presentation/testing as needed; mentor-supported | Confirmed project-stage ownership. Mentorship challenges and guides but does not replace consented external participants, reproducible data or Stefan's responsibility for understanding and acting on the evidence. Once a suitable agreement exists, a publisher is expected to lead or co-lead further commercial validation, market strategy, acquisition, distribution and launch planning under the eventual agreement; no publisher is assumed today. |
| Playtest operations, onboarding and product QA | Stefan; Veljko supports visual clarity | Quick-start cards/rounds/victory guide and simplest-hero recommendations; gradually reduce assistance. Group-selected timers; scheduled tests with 12–18 new external players as a conservative target. Record unsolicited requests and actual return separately from prompted attendance. No paid QA provider selected. |
| Veljko display drawing tablet, optional computer and multiplayer test hosting | Stefan procures/administers; Veljko approves equipment fitness | One new display tablet has a defined neutral 15.6–16-inch QHD class and provisional RSD 85,000 excluding-VAT ceiling. Test hosting has a separate RSD 60,000 excluding-VAT ceiling and is included only after dedicated-host and eligibility gates. Replace both with compliant quotations. A new computer is optional only from residual eligible capacity. Exclude used/refurbished equipment and launch-service infrastructure. |
| Rights/provenance manifest and contractor acceptance records | Stefan coordinates; every creator supplies evidence | Confirmed founder ownership. Stefan maintains the complete register, contracts/acceptance links and final replacement status; contributors remain responsible for truthful source and rights evidence. Specialist IP/legal review supports rather than replaces this duty. |
| Fund administration, reporting, cash-flow control and company representation | Stefan | Confirmed planned representative and accountable founder. Accounting/legal services may support eligible work but do not replace beneficiary responsibility. Formal company/portal records must match before submission. |
| Accounting and project-finance administration | External registered provider; Stefan owns inputs and acceptance | Provisional RSD 225,000 excluding-VAT ceiling across nine months for bookkeeping, Veljko payroll, Stefan compensation/founder filings, tax/bank records and Fund-payment/report support. The Fund FAQ expressly permits directly project-related accounting. Replace with an itemized quote; Stefan remains accountable for project administration. |
| Legal and specialist IP review | External professional provider; Stefan coordinates | Provisional RSD 175,000 excluding-VAT ceiling for bounded project-period contributor/contractor/IP-assignment documents, name/trademark and reusable-code/asset review, AI-tool/provenance terms and financing/agreement advice. Exclude pre-contract incorporation costs, public fees and taxes. Replace with an itemized quote; do not treat any opinion or clearance as completed. |

This matrix is the bridge to the budget. Each priced line must name one accountable owner, the maker/provider, quantity, acceptance evidence, timing and whether the cost is payroll or external service. The same deliverable cannot appear under both Veljko's compensation and an external invoice.

## Decisions required before a priced budget

1. Approve the original map topology/art-direction brief and its exact space, zone, spawn and prop inventory.
2. Approve and quote the 108-composition production method: fully painted, 3D render/composite/paintover, another rights-cleared method, or a controlled mixture. Define what makes each MVP composition distinct and readable and preserve provenance; do not require premium standalone paintings for every face.
3. Complete the 108-face action-presentation matrix and count unique animation/VFX units.
4. Confirm whether the six Handicap faces are included or remain post-slice.
5. Use the approved [monthly founder delivery and acceptance plan](SMART_START_PLAN.md#monthly-founder-delivery-and-acceptance-plan): Month 1 must prove the representative source-to-build pipeline, Months 2–7 each close one complete hero package, Month 8 closes shared production and optimization, and Month 9 permits acceptance fixes rather than new scope. Add supporting task evidence for the parts not directly demonstrated by the reviewed portfolio. Plan the allocated external animation package against these gates, with a representative pilot first and a non-duplicative owner split. Additional external visual scope is not funded automatically.
6. Complete the audio event map, select the smallest coherent standard-license stock set within the RSD 30,000 excluding-VAT ceiling and confirm its eligible category. Voice/barks and bespoke audio remain outside the MVP.
7. Fix target PC/resolution, performance criteria and the precise camera acceptance test.
8. Obtain a compliant quotation within the provisional RSD 85,000 excluding-VAT ceiling for the defined 15.6–16-inch QHD display-tablet class; quote a new computer separately as optional and include it only from demonstrated residual eligible capacity. Exclude used/refurbished goods.
9. Demonstrate the dedicated test-host route, obtain a quotation within the RSD 60,000 excluding-VAT hosting ceiling and confirm its eligible budget category before submission; otherwise remove the line.
10. Obtain itemized accounting and legal/IP offers within the provisional RSD 225,000 and RSD 175,000 excluding-VAT ceilings, with project-period, non-overlapping deliverables and excluded fees/taxes separated.
11. Obtain comparable written offers, map each cost to an eligible Smart Start category and exclude all self-funded/pre-contract work.

The remaining RSD 835,000 is allocated to external animation, per the applicant on 31 October 2026. Obtain an itemized excluding-VAT quote and delivery/rights terms for the bounded MVP package, including its representative pilot. The applicant confirms the package covers required rigging/skinning and animation; exact clips need written itemization. This replaces the generic contingency; there is no unallocated computer or VFX budget. No purchase or supplier engagement is authorized.

## Confirmed delivery split, 3 November 2026

Veljko creates models and textures for six heroes and three minion types. External contractor supplies rigging and animation within the provisional RSD 835,000 envelope. This is a scope agreement, not a received quotation or commission.
