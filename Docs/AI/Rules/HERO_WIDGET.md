# Guards Digital - Widget Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md).

## Widget content ledger

Widget is the seventeenth playable hero. [The completed implementation series](../Roadmap/PHASE_8_SEVENTEENTH_HERO.md) owns acceptance. His nineteen printed definitions, eighteen executable dealt faces, progression, Pyro operations and Skill reuse are composed. Production registration, staged branches, private artwork and the permanent player-path gate are complete.

## Source authority

The curated `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Widget/` contains nineteen fronts and nineteen backs. Every front and the named selection/dashboard fronts were visually inspected. The dashboard names **Widget and Pyro**; the selection card names **Widget**, shows **three stars**, and lists **Tactician, Pusher, Damager**. Dragon Bond names Pyro, a token retained at round end; no marker is named by the set.

WI-01: text extraction of all twenty-four pages of the local second-printing rulebook finds no Widget or Pyro mention. The extracted cards (verified on 2026-09-18 as the printed second edition wherever physical cards could be checked - see the parent ledger's "Edition of the extraction") remain the local authority for individual text, subject to the parent ledger's source hierarchy. General p.9 icon/range rules, p.10 card state/target/mandatory-step rules, p.11 movement, p.16 Ultimates, p.18 placement/obstacles/immunity and p.19 Swap/Remove/straight-line/token rules still govern. The full p.9, p.10, p.11, p.18 and p.19 pages were visually inspected for this inventory.

WI-02: eighteen backs share SHA-256 `EF273AEE01095FDB89D14A2ACFA92050548C77B4427A7B6540CBB4D0F222FF92`; one has `85B7E5E5ABA34415B36EC0A429ECAE84B5270345313D320FC34914F8E8B21A9D`. Do not assume one shared back. The bundled manifest separately identifies `Widget2` (8596255) and `Pyro2` (8596251), both `VerticalTokens`; that extraction category is not their game-rules type. The verified visual mapping and supply declaration are recorded below; manifest names alone establish neither.

WI-03: Pyro is a token, not a hero or minion. Rulebook p.19 keeps tokens in play when their controller is defeated; Dragon Bond overrides ordinary end-round removal. Ordinary token occupancy, Clear and spawning onto tokens still apply. Place, direct Move, Swap, Remove and Defeat remain distinct operations. Existing Pyro can be Placed from the board under p.18; placing is into a different empty space and is not movement.

WI-04: default Range/Radius is measured from Widget. Fiery/Flaming/Scorching Breath additionally require the target hero to lie in a straight line from Pyro; Nibble/Gnaw additionally require adjacency to Pyro. Neither adds line of sight. All Aboard/Take-Off/Safe Landing explicitly require **Pyro in range** as well as a chosen friendly hero in range. Airborne Attack/Assault instead print **Pyro in radius**. Safe Landing rechecks the printed Swap condition after its optional token move. **User ruling 2026-10-02** (asked during the 9.38 report, relayed by the "Agent work status review" session): the user confirmed the engine's reading stands - a Widget Pyro card's printed Range is measured from Widget, and Pyro only fixes the straight line (the Breaths) or the adjacency (Nibble/Gnaw) - so an enemy hero two from Pyro in its line but three from Widget is not offered even by a direct Fiery Breath. Until that date this was an implementation reading; no code change follows from it.

WI-05: Nibble **removes an enemy minion**, while Gnaw **defeats a minion** without the enemy qualifier. Preserve both the operation and target relation distinctions. The following mandatory Remove Pyro is not performed when the preceding mandatory step cannot complete. Friendly excludes self, while the Blue swaps explicitly allow Widget separately.

WI-06: Fight as One borrows a **resolved Skill**; Dragon Knight prints **face-up Skill**. These are different eligibility contracts, and performing another card's primary does not itself retrieve, discard or play that source. Gold's different-unit restriction names the attacked unit, not the token. Exact eligible-pool and coupled-movement interpretation gates are recorded below.

WI-07: designer Arty N. (`nichik`) directly answered the Gold targeting question on 2025-02-17 in the [Widget spotlight discussion](https://boardgamegeek.com/thread/3454879/widget-and-pyro-spotlight). His reply distinguishes the unit originally attacked from a unit affected by the subsequent Skill and explains the restriction as preventing both effects on that original target. This supports an exclusion, not a requirement that a reused Skill must target a unit. The opening strategy article remains community commentary; its older card wording must not replace local faces.

External source lead, not adopted authority: the community [FAQ](https://www.cockeyedandcurious.com/goa2/faq) attributes other Widget comments to Arty on Discord, but those direct messages were not verified. In particular its older All Aboard quotation lacks the explicit Pyro-in-range wording on this local face and must not override it.

## Printed inventory

All file stems below are under the Widget source directory and end in `__front.png`. D/M are Defense/Movement ribbon icons; A is Attack, R Range, r Radius. A missing icon is absent, never zero. Item icons are independent of card values.

| File stem | Face / color / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| widget__7692299 | Fight as One / Gold / Basic | 11 | 2 | 1 | Basic Attack / A 4 | - |
| widget2__7538945 | Dragon Bond / Silver / Basic | 6 | 6 | - | Basic Skill / r 2 | - |
| widget3__7538943 | Diversionary Strike / Red / I | 8 | 6 | 4 | Attack / A 5 | - |
| widget4__7538936 | Diversionary Attack / Red / II | 8 | 6 | 4 | Attack / A 5 | Initiative |
| widget5__7538932 | Airborne Attack / Red / II | 8 | 4 | 4 | Attack / A 3, r 3 | Defense |
| widget6__7538941 | Diversionary Assault / Red / III | 9 | 7 | 4 | Attack / A 6 | Initiative |
| widget7__7538942 | Airborne Assault / Red / III | 9 | 5 | 4 | Attack / A 4, r 3 | Range |
| widget8__7570606 | All Aboard / Blue / I | 9 | 3 | 3 | Skill - Ranged / R 2 | - |
| widget9__7570607 | Take-Off / Blue / II | 10 | 3 | 3 | Skill - Ranged / R 3 | Defense |
| widget10__7540767 | Drag Off / Blue / II | 10 | 3 | 3 | Skill | Attack |
| widget11__7570608 | Safe Landing / Blue / III | 10 | 4 | 3 | Skill - Ranged / R 3 | Defense |
| widget12__7538944 | Carry Away / Blue / III | 10 | 4 | 3 | Skill | Radius |
| widget13__7540657 | Fiery Breath / Green / I | 4 | 1 | 2 | Skill - Ranged / R 2 | - |
| widget14__7540656 | Flaming Breath / Green / II | 4 | 2 | 2 | Skill - Ranged / R 3 | Attack |
| widget15__7540775 | Nibble / Green / II | 4 | 2 | 2 | Skill - Ranged / R 3 | Initiative |
| widget16__7540654 | Scorching Breath / Green / III | 3 | 2 | 2 | Skill - Ranged / R 3 | Attack |
| widget17__7540711 | Gnaw / Green / III | 3 | 2 | 2 | Skill - Ranged / R 3 | Movement |
| widget18__7621750 | Dragon Knight / Purple / IV | - | - | - | Ultimate | - |
| widget19__7692327 | Follow Along / Gold / Handicap | 10 | 1 | 2 | Basic Attack / A 3 | - |

Fiery Breath's banner abbreviates the word as "Skill - Range"; it still prints the ranged icon and value. The wording below preserves rule clauses independently of layout line breaks.

### Exact printed rules

- **Fight as One:** Target a unit adjacent to you. After the attack: You may perform the primary action on your resolved Skill card, targeting a different unit.
- **Dragon Bond:** Choose one - (1) Place Pyro into a space in radius. Pyro counts as a token, but is not removed at the end of round. (2) If Pyro is in play, move both yourself and Pyro up to 2 spaces, in any order.
- **Diversionary Strike:** Target a unit adjacent to you. After the attack: Move Pyro up to 2 spaces.
- **Diversionary Attack:** Target a unit adjacent to you. After the attack: Move Pyro up to 3 spaces.
- **Airborne Attack:** Before the attack: You may swap places with Pyro in radius. Target a unit adjacent to you.
- **Diversionary Assault:** Target a unit adjacent to you. After the attack: Move Pyro up to 4 spaces.
- **Airborne Assault:** Before the attack: You may swap places with Pyro in radius. Target a unit adjacent to you. After the attack: You may swap places with Pyro in radius.
- **All Aboard:** Swap Pyro in range with yourself, or with a friendly hero in range.
- **Take-Off:** Swap Pyro in range with yourself, or with a friendly hero in range.
- **Drag Off:** Move both Pyro, and an enemy unit adjacent to Pyro either 2 or 3 spaces each, in the same direction.
- **Safe Landing:** You may move Pyro in range 1 space. Swap Pyro in range with yourself, or with a friendly hero in range.
- **Carry Away:** Move both Pyro, and an enemy unit adjacent to Pyro either 2 or 3 spaces each, in the same direction, ignoring obstacles.
- **Fiery Breath:** An enemy hero both in range and in a straight line from Pyro discards a card, if able.
- **Flaming Breath:** An enemy hero both in range and in a straight line from Pyro discards a card, if able.
- **Nibble:** Remove an enemy minion in range adjacent to Pyro. Remove Pyro.
- **Scorching Breath:** An enemy hero both in range and in a straight line from Pyro discards a card, or is defeated.
- **Gnaw:** Defeat a minion in range adjacent to Pyro. Remove Pyro.
- **Dragon Knight:** Each time after you perform a movement action, you may perform the primary action on your face-up Skill card.
- **Follow Along:** Target a hero adjacent to you. After the attack: You may perform the primary action on your resolved Skill card, targeting a different unit.

## Setup and progression

Starting hand: Fight as One, Dragon Bond, Diversionary Strike, All Aboard, Fiery Breath. Deck: twelve Tier II/III faces plus Dragon Knight. Each colored Tier I upgrades to either face of its Tier II, then either Tier III; the unchosen face becomes its printed item. Follow Along remains the established p.20 odd-player Handicap deferral, returned to the box during normal setup. Eighteen dealt faces, nineteen inventoried; no engine deferral is allowed on the eighteen when the series closes.

## Capability contract map

These routes identify the composed contracts. Registration at 8.43e makes each reachable from dealt content; 8.43f separately owns complete configured-player coverage.

| Printed clause | Existing foundation / required delta | Owner |
| --- | --- | --- |
| Persistent named token | WI-Q3 confirms one Pyro; `WidgetComponentContent` declares its retained-round lifetime and registered identity/Range/Radius/supply handlers | 8.43a, 8.43e |
| Place Pyro / move actor and token in either order | `WidgetEffectContent.DragonBond` composes supply Place versus identity-preserving board Place, and two chosen direct-Move orders with live second-step state | 8.43a |
| Direct token movement / Swap with Pyro | Ordinary direct-Move riders, optional actor Swap in Radius, and All Aboard/Take-Off/Safe Landing's separate Pyro/hero Range checks; Safe Landing rechecks after its optional exact one-space move | 8.43a |
| Range plus Pyro line/adjacency | Anchored selectors intersect Widget's actual Range Total with Pyro geometry; consumer feasibility, immunity and live typed outputs share the ordinary choice contract | 8.43b, 8.43e |
| Coupled exact 2/3 movement | Coupled direct Move follows WI-Q1; both full paths validate with partner-vacated occupancy, then commit synchronously. Tali's one-space batch is unchanged | 8.43b, 8.43e |
| Discard / Remove / Defeat / Remove Pyro | Breath/Nibble/Gnaw compositions use ordinary discard and staged consequences, preserving chooser, immunity, reward distinction, Lane timing and mandatory-step order | 8.43b, 8.43d |
| Resolved Skill primary, different unit | Owned resolved Skill pool, after-Attack grant and original-unit exclusion throughout the borrowed chain; token-only Skills remain legal | 8.43c |
| Face-up Skill after Movement | Exact Movement completion fact and WI-Q2 public face-up pool use the existing Ultimate/grant pipeline, with source-state preservation | 8.43c |
| Printed set, upgrades, items, registration and artwork | `WidgetCardContent` composes nineteen definitions, five starting cards, thirteen deck cards and the boxed Handicap; only the Handicap is deferred. Five staged branches and official selection deal the set | 8.43d-8.43e; gate 8.43f |

## Integration identity and private artwork

The registration addition leaves schema v42, wire `/15`, and all sixteen prior hero content identities unchanged. Eleven capability references and `widget-pyro@1` add 319 bytes to the recorded catalog. Earlier saved registration sets are rejected by the existing strict comparison, not silently migrated; only the test corpus fingerprints are regenerated. `SyntheticCoupledEscortHand` independently exercises owned Hazard lookup, anchored selection and coupled movement without Pyro.

The selection front is byte-identical to `randomizers-tt25__7723701__front.png` (SHA-256 `3ED989791ACF2C5E808A40A5A2B43C2A4548055C7B92551C8AD68F9F96173CC6`). All eighteen playable fronts retain native 372x520 size. Visually verified Widget/Pyro circular faces come from their WI-02 manifest elements on `Image_1_Atlas`, respectively at `(1769,1577)/(2504,1577)` and `(2676,615)/(2980,615)`, each 302x302. The existing source-preserving portrait preparation supplies Widget's team/neutral resources and Pyro's single neutral placed-token resource; Pyro is never registered as a hero. Prepared fronts, highlights, profiles and portraits remain ignored private resources.

Presentation direction: Pyro's prepared artwork has no overlaid name, as requested on 2026-09-18. Caption metadata changes no identity or rules; other tokens keep their captions, and a missing-art fallback disc remains named.

## Open verification points

- **WI-Q1 (answered 2026-09-18):** The user approved "Yes, move them together as described" for one common distance (2 or 3) and one direction, both moving as a pair, allowing following through the other's vacated spaces, and neither moving if either cannot complete the chosen distance. This is a user-approved interpretation, not a claimed official ruling. Ordinary obstacles still block Drag Off; Carry Away ignores transit obstacles but both endpoints must be legal. Coupled implementation and regression checks are owned by 8.43b.
- **WI-Q2 (resolved 2026-09-18):** Dragon Knight includes owned face-up discarded and resolved Skills, including the Skill card just used for Movement, but excludes in-hand and facedown cards. Its exact text says "your face-up Skill card", not "another" or "resolved". Rulebook p.9 ties card type to its primary action; p.7 reveals the played card before acting, so the current Skill qualifies without assuming that its resolution precedes the Ultimate trigger. Page 10 distinguishes resolved/discarded states and removes type/actions from facedown cards; p.4 identifies cards in hand as hidden. Those full pages and the local Dragon Knight face were visually checked for this answer. The user accepted this source-based reading with "ok keep going then?" after the explanation. This is not a separately verified Widget-specific designer ruling. Do not substitute Gold's resolved-only pool or treat an internal hand-orientation flag as public face-up eligibility. The distinct pool and source-linked continuation checks are implemented by 8.43c; configured player coverage remains with 8.43f.
- **WI-Q3 (answered 2026-09-18):** The user answered "ok, keep going then" to the explicit confirmation that Widget has exactly one Pyro token and "Place Pyro" relocates the same piece when already on the board. Declare one physical piece, preserving its board identity on relocation; this supply confirmation is user-approved rather than a claimed published component count. Verify visual identities at integration; do not treat the manifest's standee type as a second hero.
- Fight as One's borrowed action may affect Pyro/spaces rather than a unit; WI-07 supports applying "different unit" as an original-target exclusion, not an invented requirement to target a unit. Source-linked tests must cover both shapes at 8.43c.
- Build smoke exception approved 2026-09-18: the user answered "Yes, run Widget's local build smoke check" to using `Tools/Invoke-PlayerBuildSmoke.ps1` for the required disposable local build-and-launch check. No package, upload or distribution is authorized.
