# Guards Digital - Removed and Superseded Roadmap Definitions

This append-only archive preserves roadmap definitions removed or superseded after approval but before completion. The active route remains authoritative in [ROADMAP.md](../ROADMAP.md) and its indexed phase shards.

## Removed proposed milestone 8.30e: Compose the card fronts digitally from the verified content

Removed from the active Phase 8 route at the user's direction on 2026-08-30. The user chose to keep milestone 8.30d's completed scan-backed highlighting treatment, so no digital-front rebuild, hand-traced glyph template, or 108-card composition/sign-off work remains scheduled. Milestone 8.30e was never started or completed, and no private/reference files were deleted as part of its removal.

The definition below is preserved verbatim as it stood immediately before removal.

### [ ] 8.30e Compose the card fronts digitally from the verified content

User-approved roadmap amendment (2026-08-30), following 8.30d's precedent: 8.30d's corrective round proved that pixel-tracing badge silhouettes out of the licensed scans cannot reach the approved shine quality - the scans print glyphs onto same-hue banners, shade them into their own drop shadows, and blur at HUD zoom - so the user directed rebuilding the fronts from digital assets instead, with the shine geometry then exact by construction. The user approved running this before 8.30c, so the six-seat gate validates the composed fronts; 8.31 remains gated on 8.30c alone. Parked by the user the same day after the first composed template: the badge treatment needs a user-built template card before composition can be mechanised (the extracted rulebook glyph assets, page renders, extraction tooling, and the first Charm draft live in `tmp/8.30d-shine-design/` and `tmp/8.30e-cards/`), and the 8.30d shine corrective proceeds on the scanned fronts meanwhile.

Blocked by: Milestone 8.30d must provide the lit-choice surfaces and the shine sprite pipeline.

Observable outcome: Every prepared card front is composed digitally from that card's typed verified definition - frame, panel, banner, badge glyphs, value digits, and rules text rendered from clean shared assets, with the licensed scan surviving only as the cropped illustration - at the same private path, name, and 372x520 geometry, so the runtime, the suites, and the shine pipeline consume the new fronts unchanged and every badge highlight hugs a silhouette that is known exactly because it was drawn.

Acceptance checks:

- One layout specification - frame, panel and its border, banner arcs, badge slots, rail positions, illustration box, and text metrics - is measured once from the scans, recorded as constants beside the compositor, and every front renders from it plus typed content alone; no per-card pixel measurement survives.
- The badge glyph assets (swords, fan, book, boot, shield) derive from the user's hand-traced silhouettes, recolored per the card's typed colour; per-card value digits, the `+` suffix, and the Block `!` are typeset at the badge's value position.
- Illustrations are cropped from the licensed scans into the same gitignored private tree, and composed fronts replace the scans at exactly the existing `Assets/Resources/Private/Cards/<hero>/<card>.png` paths and dimensions; the compositor, like the shine generator, never touches Git, Unity, a build, or a network client.
- The user approves one composed template card against its scan side by side before mass generation, and a full side-by-side audit sheet over all 108 fronts carries the final sign-off.
- The shine generator consumes the compositor's own badge masks directly - its scan-tracing passes retire - and 8.30d's lit-choice acceptance stands on the composed fronts, re-verified in Game View at the supported validation resolutions with user sign-off on the result.
- The Edit and Play suites pass; generated-fallback faces and the missing-resource degradation path are untouched.

Non-goals: No card content or wording changes, no board-surface work, no new illustrations, no localization, no animation, no other HUD styling, no Phase 9 presentation suite; the licensed scans stay private and uncommitted as the illustration source.
