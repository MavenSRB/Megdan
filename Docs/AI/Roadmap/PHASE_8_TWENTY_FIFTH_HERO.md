# Phase 8 - Twenty-fifth hero: Mrak the Rockshaper

Return to [roadmap](../ROADMAP.md). [The session brief](../SESSION_BRIEF.md) owns the active and next route.

## Authorization and scope

On 2026-09-27 the user said "you are cleared to go and implement the next hero, just be careful, look at all the recent bugs we found, learn from them and dont keep repeating the same mistakes." The user named no hero, as on 2026-09-25 ("ok, you pick then"), so the agent chose from the eight undealt heroes: **Mrak the Rockshaper**, three stars, whose one token kind and terrain-adjacency clauses were already named by 8.88's composable selection. Razzle (withdrawn once by the user), Gydion (22 spell cards), Takahide (21 fronts) and the four-star heroes were set aside as heavier. This authorizes the bounded sequence **8.112 -> 8.112a -> 8.112b -> 8.112c -> 8.112d -> 8.112e -> 8.112f -> 8.112g -> 8.112h** through a fully selectable, artworked Mrak, every dealt printed face and the permanent player-path gate. Execute one milestone at a time with its own validation and local commit. A block whose open question is unanswered stops at that question; other blocks do not wait on it. The user settled MR-Q1 to MR-Q4 on 2026-09-27. No other hero, package, remote publication or rule invention is authorized.

[Mrak's rules shard](../Rules/HERO_MRAK.md) owns source facts, inventory, capability mapping and questions MR-Q1 to MR-Q4. Eighteen ordinary fronts are dealt; the nineteenth, Lesser Fissure, is a p.20 Handicap and stays boxed at setup.

## Carried lessons

These are the defects of 8.92-8.106 that reached a player or a late gate. Each is an explicit acceptance item in the block that could repeat it, not advice.

1. **Read icons by shape against known references, never by slot position** (8.93f: three Oaths carried an unprinted Defense 2). 8.112 verified every item, Range/Radius and ribbon glyph by montage; 8.112g re-checks every definition's values against the fronts.
2. **Artwork catalog by contact sheet** (8.93f: two fronts swapped). 8.112g renders filename beside printed title for all nineteen fronts and inspects it.
3. **p.15 upgrade list and closed vocabularies are only validated once dealt** (8.93f). 8.112f asserts both Tier III successors per Tier II and every integer parameter's declared vocabulary before 8.112g registers.
4. **An offer must be able to do what it offers** (8.93f's rune-gated Attack spent the card doing nothing; 8.104's legal Attack was not offered after a conditional move). Every block proves offer and answer agree: no offered action with no legal completion, no withheld action with one.
5. **Shared auto-confirm must not skip a DONE** (8.93f). Any multi-pick Mrak prompt (Stone Grip's three spaces) is driven through the real window on a local and a network seat.
6. **A rendered piece must face the camera** (8.102: the Turret was back-face culled). 8.112g asserts the rock token's rendered face normal against the camera and inspects a captured frame.
7. **The deciding seat must draw its own controls** (8.58, 8.72) and **a paused projection must not erase a standing window** (8.106). Discard choices addressed to enemy heroes and Stone Carapace's substitution are exercised on the addressed network seat.
8. **Journeys assert the board changed and exercise the shape they bind** (8.86, the Swift lesson). 8.112h audits every journey for a state assertion and measures the staged boards before writing journeys.
9. **A green harness is not Unity evidence, and a blocked harness voids later harness claims** (8.101-8.106). Each block states which tiers actually ran, from their result files and mtimes.
10. **Mappings are checked against print, not memory** (8.105). The secondary Attack's value, the push/follow distances and every rock count are read from the definitions against the fronts in tests.

### [x] 8.112 Inventory Mrak and map the implementation sequence

Outcome: verify the nineteen fronts, profile, dashboard, rock and standee manifest pieces and relevant rulebook pages; record printed values, exact clauses, existing-capability matches, new contracts and bounded questions. No executable content is registered.

Acceptance: all nineteen distinct fronts and the one shared back accounted for; items, Range/Radius and ribbon glyphs verified by montage against recorded references; star/role profile and stat bars recorded; every novel effect probed against actual engine seams; open questions have an owner and block only their dependent block. T0 documentation validation.

Non-goals: gameplay code, art import or a package.

Completed 2026-09-27. The nineteen fronts, one shared back, profile (three stars; Melee, Tokens, Durable, Disabler, Tactician), stat bars, dashboard and the `Mrak` standee and `Rockkk` token manifest pieces are inventoried in [his rules shard](../Rules/HERO_MRAK.md); items, Range/Radius and ribbon glyphs were verified by montage against Trinkets' recorded icons. The probe found the roster's first printed secondary Attack (six Red faces), a resolved-card discard and Defense substitution, line-scan and farthest-space rock placement, a straight-line move with an empty-space budget and an after-place trigger as new contracts; push ignoring obstacles, obstacle-ignoring direct moves, terrain/token adjacency selection, discard-or-defeat, different-target repeats, retrieval and Clear's replacement exist. MR-Q1 (rock supply), MR-Q2 (Fissure's line), MR-Q3 (Stone Grip's farthest spaces) and MR-Q4 (Carapace with an empty hand) are with the user. T0; no executable content or Unity run.

Historical evidence: [8.112 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8112-mrak-inventory-evidence).

### [x] 8.112a Implement rock tokens and the printed secondary Attack

Outcome: a rock token definition (obstacle, supply 3 per MR-Q1, p.18 short-supply reclaim, p.8 cleanup, Clear) and a printed secondary Attack: non-Ranged, printed value plus Attack items, no card text, replaceable by Clear, offered to players and bots and drawn as a ribbon control on the prepared rail and the generated face.

Acceptance: a synthetic card of Seismic Slam's printed shape proves the secondary Attack through a configured turn: offered beside the secondary Movement, adjacent enemy hero (Defense window at the printed value, no card text) and minion (defeated), refusal at distance 2 even beside a Radius icon, item arithmetic through the Attack total's own queries, Clear replacement; the prepared-rail hit surfaces cover the ink measured on Mrak's fronts for three-badge and Defense-and-Attack rails without moving any earlier card's boot, and the badge has its own shine slot and generated chip; rock supply exhaustion and reclaim candidates, obstacle blocking, cleanup and Clear. Schema or wire movement promotes this block to T3.

Amended 2026-09-27 before implementation finished, recording what the probe found: the after-place fact moves to 8.112f, because the only reader of that fact is the Ultimate and the engine's trigger seam (`MarkerGivenTo` -> `AfterMarkerGivenTriggerQuery` shape) is built with its consumer - a report nobody reads would be the declared-and-unread shape. Configured-match snapshot/restore and both-seat projection of a paused secondary Attack need Mrak dealt and are owned by 8.112g/8.112h; the serializer writes the invocation context as a declared enum member, so no persisted field is new.

Non-goals: Mrak's printed faces or dealing.

Completed 2026-09-27. `MrakComponentContent` defines the three-rock `mrak-rock` token (bare impassable obstacle, end-of-round cleanup) and its p.18 reclamation; `MrakCardContent.SecondaryAttack` declares the printed secondary Attack. The probe showed the engine's Attack path was already generic over a PrintedSecondary invocation - offer, adjacency-only targeting beside a Radius icon, Defense at the printed value with no card text, minion defeat, Clear and item arithmetic all pass through a configured turn with no Core change - so the production work is Presentation: prepared-rail hit surfaces for three-badge and Defense-and-Attack rails (measured ink, typed by the card's secondary count, no earlier card's boot moved), a `SecondaryAttack` shine slot and generated chip, the circle treatment for any secondary badge, and a secondary Attack's CLEAR beside its badge. All unregistered until 8.112g. T2; evidence in the milestone commit.

Historical evidence: [8.112a completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8112a-mrak-rock-token-and-secondary-attack-evidence).

### [x] 8.112b Compose Fissure and Stone Grip

Outcome: the Gold Basic Attack places rocks in the first three empty spaces of the Attack's line after the Attack (MR-Q2); the Silver Basic Skill places exactly three rocks adjacent to an enemy hero in Range, farthest from Mrak (MR-Q3), with a player choice when distances tie.

Acceptance: both readings as the user settled them, tested on straight and off-line targets, board edges, terrain and occupied spaces, defeat-emptied target spaces, supply shortage, immunity, the three-pick prompt's DONE, restore and replay. T2 minimum.

Non-goals: other faces, dealing.

Completed 2026-09-27. `MrakRockPlacementContent` adds four capabilities: p.18's short-supply step for Fissure's line and for Stone Grip's farthest spaces (declines when the pile covers the demand, takes back every standing rock when all are needed, otherwise asks which), and the two placements. Each face is two nodes with at most one question each, because a second question asked from `Resume` could not be rebuilt by a restored snapshot. Fissure reads the attacked space through `attacked-target-space@1` and walks outward past occupied spaces and terrain to the board's edge (MR-Q2); Stone Grip takes the three farthest empty adjacent spaces with the next-farthest filling in and a question only for a tie that does not fit whole, placing what fits (MR-Q3). A configured round proves surviving and defeated targets, skipping, the board edge, both short-supply branches, all three Stone Grip shapes, offers withheld with no legal target, and restore/replay of both pauses plus 8.112a's paused secondary Attack. Unregistered until 8.112g. T2; evidence in the milestone commit.

Historical evidence: [8.112b completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8112b-mrak-fissure-and-stone-grip-evidence).

### [ ] 8.112c Compose the Red Seismic and Carapace faces

Outcome: Seismic Slam, Seismic Assault and Epicentre select an enemy hero in Radius adjacent to terrain or a rock token who discards or is defeated, Epicentre repeating on a different target; Stone Carapace and Rock Solid move Mrak, Rock Solid may retrieve, and both create the This round substitution (MR-Q4) that ends when the card changes state.

Acceptance: terrain-versus-rock-versus-neither boundaries, immunity, empty-hand defeat, different-target exhaustion; the substitution offered on every hand discard addressed to Mrak (enemy discard effects, own costs) and as a Defense card with its printed Defense, cancelled after one use, at round end and on defeat, restored from snapshot, and decided on Mrak's own network seat; all six secondary Attacks and Movements. T2 minimum.

Non-goals: dealing.

### [ ] 8.112d Compose the Blue push and stomp faces

Outcome: Boulder Rush, Blitz and Boulderdozer push a token or enemy unit adjacent to Mrak a chosen 1..N spaces ignoring obstacles, then offer Mrak an optional move up to N along the push's direction ignoring obstacles; Stomping Step and Ground Shaker move a unit in Radius adjacent to terrain or a rock 1 space and place a rock in the vacated space, Ground Shaker repeating on a different target.

Acceptance: pushes through obstacles to the first reachable empty space, short and zero pushes, a pushed rock and a pushed hero, follow-move directions and blocked endings, the follow still offered after a zero-space push (the direction is fixed by the chosen target, and the text sets no condition); stomp with no legal step not offered, rock supply shortage, the Ultimate fact recorded; restore. T2 minimum.

Non-goals: dealing.

### [ ] 8.112e Compose the Green move-a-unit and rolling faces

Outcome: Treacherous Ground, Rockslide and Avalanche optionally move a unit in Range 1 space to a space adjacent to terrain or a rock, Avalanche allowing one repeat; Rolling Stone and Strolling Stone move Mrak in a straight line ignoring obstacles with at most one or two empty intermediate spaces.

Acceptance: destination filter boundaries (terrain, rock, neither), immunity, the optional decline, Avalanche's same-unit repeat; rolling lines over terrain, units and tokens, budget boundaries, never ending on an obstacle, board edges; restore. T2 minimum.

Non-goals: dealing.

### [ ] 8.112f Compose Rock and a Hard Place and reconcile the set

Outcome: the Ultimate discards a card from each enemy hero adjacent to rocks Mrak's placement step just placed, once per turn - through a typed after-place report on the placing step (moved here from 8.112a) and a trigger query in the shape the marker-given Ultimate uses; all eighteen dealt definitions and the boxed Handicap are reconciled with the inventory.

Acceptance: the placement report across pause and restore; triggers from Fissure (Attack text), Stone Grip and Stomping Step placements; no trigger from pushes, moves or another hero's tokens; one trigger per turn with several heroes; immunity; snapshot/replay; p.15 upgrade lists and closed integer vocabularies asserted; every definition's printed values compared with the inventory table. T2 minimum.

Non-goals: dealing or artwork.

### [ ] 8.112g Deal Mrak and present his artwork

Outcome: register the eighteen playable definitions, the rock token and handlers; add Mrak to legal pickers and configurations with three stars and Melee, Tokens, Durable, Disabler, Tactician; prepare card fronts, profile, standee, rock token art and hover catalog.

Acceptance: stable identities, setup, hand and upgrades, selection/filter/spawn and both seat projections; contact sheet of catalog filename against printed title; the rock token's rendered face toward the camera in a captured frame; the secondary Attack ribbon control reachable on a real hand, its CLEAR chip beside the badge, and a captured frame of both three-badge and Defense-and-Attack rails; `Tools/Prepare-ShineSprites.ps1` traces the three-badge rail's boot and the swords badge (its Movement trace assumes the two-badge position today); private art boundaries; content-revision and artwork-catalog agreement; focused Unity input and visual checks plus the affected regression tier. T2 minimum.

Non-goals: a package or another hero.

### [ ] 8.112h Gate Mrak through the complete player path

Outcome: a content-derived matrix and real-input journeys cover every dealt face, the rock token, the secondary Attack and the substitution, with no parked or skipped required journey.

Acceptance: every declared interaction shape mapped to named journeys that assert a board or state change; a paused secondary Attack's snapshot/restore and both-seat projection; items, progression, defeat/respawn, round cleanup of rocks, stale input, snapshot/restore, retained-prefix Undo, network projection agreement, whole-face footprint; complete unfiltered harness, Edit Mode and Play Mode green with zero new skips. T3.

Non-goals: a package or the next hero.
