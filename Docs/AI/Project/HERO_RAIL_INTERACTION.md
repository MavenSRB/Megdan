# Guards Digital - Hero-Rail Interactions

Canonical interaction owner, split from [the match HUD](MATCH_HUD.md) to preserve its precise contracts within the active-leaf limit. Return to [PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md).

## Identity and selection

Hero identity is permanently linked across surfaces: pointing at a plaque gives that hero's board piece the warm-ivory six-corner glow, while pointing at the board piece changes only the plaque's reserved two-unit top edge to ivory. The same channel also accepts a tied-Initiative hero button and survives ordinary rail/view recomposition without submitting a command.

Clicking a plaque routes its projected hero identity through the same local tray-selection boundary as an unused board-token click. A player seat accepts only its admitted home hero, rendering only that hero's tray even when foreign cards are public; an unbound seat renders no fallback tray. The Director retains inspection/pinning of other heroes until the awaited hero changes. Selection changes no authority, command, visibility rule or serialized state. Public rail history remains available.

## Shared hover preview

Resting the pointer on an occupied turn slot, on a discard icon, or on a revealed marker reveals that card's whole printed face beside the rail it belongs to - the 2026-08-21 decision. Since 9.36 the face is placed off the rail column's own edge, out from the left rail and in from the right; 8.42b had placed it off the hovered icon's edge, which drew it over the owner's tray and plaque. The hero-profile hover keeps its place beside the portrait. It is drawn by the tray's one artwork-aware complete-face builder, so one card cannot look like two: the exact private front appears when prepared and the generated face appears otherwise. One preview element serves all three, so two faces never stand at once. The reveal is a reading of the model the rail was already rendered from: it submits nothing, adds no focus stop, is dropped whenever the rail is redrawn, and an empty slot or a facedown marker reveals nothing rather than an empty card. Each icon's assistive label names the exact card and where it is in its lifecycle, so the fact is available without a pointer.

The same preview also serves the Ultimate icon and the hero medallion. Hovering the medallion displays the existing private hero-picker profile, resolved by registered content identity rather than the match's possibly mirrored hero instance ID. Its aspect ratio is preserved, width capped at 744 reference units and size/position clamped inside the rail region; geometry changes reposition it. It is pointer-transparent, leaves the board identity channel intact, and disappears on pointer exit or model replacement. Missing private profile art yields no preview, while the portrait's name/team assistive reading remains. No profile resource is generated or added by this interaction.
