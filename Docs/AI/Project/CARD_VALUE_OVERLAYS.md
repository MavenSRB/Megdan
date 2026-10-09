# Live card modifiers

Canonical W-012 subject owner. Return to [match HUD](MATCH_HUD.md) or [current implementation](CURRENT_IMPLEMENTATION.md).

## Live modifiers on card faces (W-012)

The shared hero-card and spell-card renderers place white signed nonzero deltas immediately to the right of the corresponding printed number, inside the face. `Assets/Resources/UI/CardValueAnchors.csv` contains individually inspected source-pixel anchors for all 578 hero fronts and 22 spells; it is not generated from image recognition or card text. Digit width and remaining right-edge space determine the fitted font size. An implicit text-only radius on Takahide's Spinning Blade has no printed number and therefore no overlay. Generated fallback faces put the same deltas beside their typed value labels.

Hand/decision faces, inspection, token-pip and rail hover previews, spellbook and casting companions share the treatment. Level-up candidates, their comparison hand and their spell companions remain unmodified. A changed live value refreshes the face, including after Undo; inspection keeps its scroll position. Zero modifiers disappear rather than leaving `+0` labels.

Values come from the host's existing Core queries: items, applicable active modifications, card-declared contributions and body-specific minion Defense. A bound attack uses its recorded total and the targeted defender body. Before a target or a multi-body actor is selected, no target/body-dependent bonus is promised; unresolved action text is not simulated by presentation. The labels are current-state deltas, not a prediction of every later choice. Only already-visible faces receive values in a seat projection; no hidden card instance is added. Snapshot/replay rules remain unchanged. Wire /50 carries a sparse, compact value projection.

## Printed value ownership

Which region a value belongs to is decided by the action it is printed on and by its `PrintedValueChannel`, never by the card's name and never by reading the sentence: the primary action is the one whose origin says `PrintedPrimary`, the secondary strip is every other action the card itself prints, the two qualifiers a label can carry are p.13's Ranged classification and p.14's Block, and p.16's Ultimate - which prints no Initiative and has no action at all - reads its icon from the permanent ability instead. That rule is what keeps this build without a printed-text parser, and `MatchHudPresentationTests` asserts the four regions partition the definition's printed values exactly, each drawn once.

## Wire and visibility

Wire protocol `guards-private-online/50` adds visibility-scoped nonzero card-value deltas. Rows reuse already-transmitted card identities where possible, with explicit owner/versioned-face identities for authorized inspection-only faces. Compact signed values retain an Int32 escape; existing frame limits and retained history are unchanged. Earlier peers are rejected by the admission tuple. Snapshot schema remains v59. See [online architecture](PRIVATE_ONLINE_ARCHITECTURE.md) for transport and historical compatibility amendments.
