# Megdan creative presentation

Return to [project context](../PROJECT_CONTEXT.md). Private presentation studies, not implemented game rules or approved final hero designs.

## Current decisions

W-164 concept still: [six illustrated standees](../../../Creative/standees/six-team-standees-v1.png), with upright thin cutout panels, raised circular bases (ram/cat/knight Red; goblin/grandmother/frog Blue) and soft contact shadows. Ram and goblin retain the supplied bust/head crops. Built-in image generation reconstructs the art with minor variation; prompt/provenance is beside the output. Still only, awaiting visual feedback; no runtime integration or 3D models.

Latest six-hero game screenshot: `tmp/w163-six-heroes-live-match.png` (1920x1080, W-163). The ordinary local 3v3 camera/HUD shows Brogan, Riki and Rowenna against Tigerclaw, Gydion and Tali, with Riki's hand open.

Work only in the independent Megdan folder. User wants small visual iterations: show a still of the requested change before another video. Do not automatically regenerate videos.

The knight, sheep, goblin, grandmother/cat, frog and cat-duelist pictures are Veljko's previous portfolio work, not approved Megdan hero concepts. Original inputs are in Art/Inputs. Built-in image generation prepared transparent cat/knight sprites and reconstructed missing parts; this is not a pixel-exact extraction or 3D modelling.

W-158 adds a separate six-image concept-portrait set under `Art/ConceptPortraits`. Each square transparent PNG preserves its source character's orientation, pose, proportions, expression, palette, costume and signature props as closely as the built-in image-generation conversion allows. They are AI-assisted reconstructions, not pixel-exact crops, final production art, approved hero canon or runtime assets. Sources remain untouched and provenance is recorded beside the outputs.

Current presentation: connected grass-variant hex board, allied frog wizard above, cat left and knight right. W-157 animates facedown commitments and joint reveal, then Cold Snap 9, Mad Dash 7 and Focus 3. Challenge Accepted stays uncommitted in the knight's hand for Defense 6 against Attack 6; the view switches to the defender before showing it and then discards it. Focus stays committed and supplies secondary Movement 2 on its own action. Resolved commitments remain in the turn area. This is a labelled 44-second silent concept animation with English captions, not runtime gameplay capture or a final narrated application video. The earlier two-character video is superseded as a scenario guide.

W-160 brings the supplied board direction into the private runtime without changing topology or rules: the Guards board face is retired; Veljko's four grass variants are assigned stably by coordinate; water covers map-edge terrain, lava covers internal terrain and desert covers the two side Zones. Ordinary tile borders are absent, while the three lane Zones that can become the Battle Zone retain thin coloured outlines. Generated tile views, highlights, spawn markers, pieces and interaction coordinates remain intact. The exact inspected runtime capture is `tmp/w160-megdan-grass-board.png`; no package or publication was produced.

W-161 temporarily maps the ram warrior to Brogan, cat duelist to Misa (displayed as Riki), knight to Rowenna, goblin to Tigerclaw, grandmother/cat to Gydion and frog wizard to Tali. Board tokens and hero-selection/profile surfaces use the prepared portrait set, while card art uses crops of Veljko's six original supplied JPG inputs rather than those portraits or token derivatives. All 108 existing synthetic faces retain their typed title, initiative, defense, movement, primary action and rules text. The runtime card frame follows the approved dark rounded frame and cream title/rules panels; its left rail expresses Initiative as a clock, Defense as a shield and Movement as a directional arrow. Red, Blue, Green, Gold and Silver accents come directly from each card's existing typed colour, with the existing neutral treatment preserved. Misa's runtime/content identifier remains `misa`; only presentation reads Riki. The exact inspected captures are `tmp/w161-megdan-card-gallery.png` and `tmp/w161-megdan-board-tokens.png`. Private source only; no package or publication.

Latest runtime card direction: native 372:520 aspect, dark rounded frame, cream title, clock/shield/arrow value rail, original supplied character image, and cream action/effect panel. W-161 implements this structure with existing typed card colours and unchanged synthetic content, carrying W-159's circular initiative-clock language into runtime. W-156 replaced Quick Cut / Steel Guard placeholders with verified Tali/Brogan/Misa prototype cards and source attribution; W-157 integrated the earlier faces into the concept video. These do not establish original Megdan hero rules. Focus uses secondary Movement 2 on the knight's action; its primary grants movement next turn, not immediately, and is not performed in this sequence.

Use Veljko's supplied frog wizard, not an invented human wizard. Original: Art/Inputs/veljko-frog-wizard-original.jpg. Board/card derivative: veljko-frog-wizard-cutout-v1.png, an AI-assisted background extraction, not a pixel-exact cutout. Full prompt/provenance sits beside it. The rejected generated human wizard and v8 still remain superseded private history only. Do not generate substitute hero designs when supplied artwork exists.

W-162 refines the runtime stat rail into continuous vector silhouettes with matching cream rims: a twelve-tick initiative dial, curved pointed shield and broad diagonal movement arrow. Larger outlined numbers form a separate foreground layer; the arrow's shape is independent of its overlaid number. Existing typed colours and printed values remain authoritative. Runtime captures: `tmp/w162-megdan-card-gallery.png` at game size and `tmp/w162-card-icons-detail.png` enlarged for inspection.

Discussed 3D direction: thick hex tiles, angled camera, upright illustrated characters with shadows. Full 3D heroes need models and rigs; none have been created in this work.

W-154: Five supplied WhatsApp JPEGs are preserved in Art/Inputs/Grass (originals also retained in Downloads); two are byte-identical, leaving four distinct grass variants. All four are composed into the existing board still using preview-grass.py, adapting flat-top source hexes to the existing point-top lattice while retaining upright decorations. W-160 later prepared the available grass and biome references for the private runtime. No game-rule change.

## Latest artifacts

- [Initiative clock-badge card still](../../../Creative/gameplay-mockup-cutout/three-heroes-cards-v10.png) — W-159; four native-aspect verified prototype faces with prominent initiative dials. Still only; not integrated into the existing video.
- [Six minimally altered concept portraits](../../../Art/ConceptPortraits/README.txt) — W-158; transparent square PNGs preserving the six supplied characters' existing orientations. Private concept assets only; no runtime or publication.
- [Initiative sequence video](../../../Creative/gameplay-mockup-cutout/Megdan-initiative-sequence-v10.mp4) — W-157, 44 seconds, 1920x1200/24fps, silent concept animation. Renderer: render-initiative.py; focused staging/timeline checks: check-initiative.py.
- [Three heroes and verified cards](../../../Creative/gameplay-mockup-cutout/three-heroes-cards-v9.png) — W-156; renderer preview-three-heroes.py; Veljko frog wizard restored after user correction. Await visual feedback before video.
- [Board with illustrated cards](../../../Creative/gameplay-mockup-cutout/board-cards-v7.png) — W-155 replaces generic side buttons with the approved native-aspect Quick Cut / Steel Guard concept faces. Lower hand silhouettes are layout placeholders; stats/effects remain undefined. Renderer: preview-real-cards.py. Await user feedback; not integrated into video.
- [Grass variations still](../../../Creative/gameplay-mockup-cutout/grass-variants-v6.png)
- [Card concepts](../../../Creative/gameplay-mockup-cutout/card-faces-v5.png)
- [Attack and defense clip](../../../Creative/gameplay-mockup-cutout/Megdan-attack-and-defense-v4.mp4)
- [Side cards](../../../Creative/gameplay-mockup-cutout/side-hands-v3.png)
- [Sizing and connected hexes](../../../Creative/gameplay-mockup-cutout/sizing-map-v2.png)
- [Generated atlas](../../../Creative/gameplay-mockup-cutout/sprite-atlas.png)

Python/Pillow scripts and FFmpeg exports are in Creative. Windows Arial fonts remain an OS dependency. Active script paths point to Megdan, not the former Guards directory or OS temporary attachments. Earlier outputs and scripts are preserved.

## Latest grant decisions

Veljko creates models/textures for six heroes and three minion types. External contractor supplies rigging/animation within the provisional RSD 835,000 envelope; no quote or commission claimed. Detailed engagement/tax calculations are deferred to contracting if funded, not a new pre-submission requirement. The final English video remains unrecorded; both members must appear. Gameplay footage uses original art; prior portfolio studies are labelled. Inat account request submitted, approval/login/project ID unconfirmed.
