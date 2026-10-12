# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

### [!] W-044 Private enemy card faces (revised orange sample approval)

Latest amendment is implemented: Hurl moves toward the closest Blue unit (hero or creep), not just a hero. Typed Unit targeting is valid only for Movement; existing attack priorities remain intact. The user expressly authorized independent orange color/template editing. Native-export limitations are disclosed, not an additional permission gate.

User approved the sample direction with amendments: remove the secondary shield and boot icons, express movement in card text, and make Hurl's Move 1 fallback approach as closely as possible (stop adjacent, not at range 2). The bounded content change is implemented and focused checks are green. No runtime artwork integration or build. Closing checks remain: user review of the revised orange sample, then produce and inspect the remaining private faces under the original sample-first approval sequence.

Original request: after W-043, use https://stats-of-atlantis.org/builder to create one artwork-free Heavy face, show it for approval before the remaining Melee, Ranged and Heavy set, and keep outputs private/separate from runtime. The subsequent Hurl movement request expressly expands the original no-gameplay-change boundary. User request relayed from the side task on 2026-10-12; its duplicate follow-up is cancelled.

W-043 is committed at `work/W-043`. Browser tab 1 holds the revised Hurl sample (R1 frame): Initiative 3, Attack 2, Range 2, hero-first targeting, different-target repeat and Move 1 fallback in text, no artwork. Clearing Movement removes the boot, but Defense remains a shield 0; testing -1 did not hide it and was reverted. No hide control is exposed. The purple frame is Ultimate-only. Download Card previously produced no artifact, its event timed out and content export is unsupported. Preserve original attempts and the revised full screenshot at `tmp/w044-enemy-faces/hurl-text-movement-preview.png`.

The computer-use and imagegen skills first produced `tmp/w044-enemy-faces/hurl-no-secondary-icons-preview.png`, then the corrected orange `tmp/w044-enemy-faces/hurl-orange-closest-unit-preview.png`: AI-edited, tightly framed previews with shield removed and no boot. The latter has visually verified orange accents and closest-Blue-unit wording; Initiative 3, Attack 2, Range 2 and repeat text remain intact. These are not pixel-identical native exports. Prior versions are preserved. No runtime integration or further cards produced. Work remains incomplete pending the revised sample review; implementation/test evidence is in the partial-work commit body.
