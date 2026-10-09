# Live card modifiers

Canonical W-012 subject owner. Return to [match HUD](MATCH_HUD.md) or [current implementation](CURRENT_IMPLEMENTATION.md).

## Live modifiers on card faces (W-012)

The shared hero-card and spell-card renderers place white signed nonzero deltas immediately to the right of the corresponding printed number, inside the face. `Assets/Resources/UI/CardValueAnchors.csv` contains individually inspected source-pixel anchors for all 578 hero fronts and 22 spells; it is not generated from image recognition or card text. Digit width and remaining right-edge space determine the fitted font size. An implicit text-only radius on Takahide's Spinning Blade has no printed number and therefore no overlay. Generated fallback faces put the same deltas beside their typed value labels.

Hand/decision faces, inspection, token-pip and rail hover previews, spellbook and casting companions share the treatment. Level-up candidates, their comparison hand and their spell companions remain unmodified. A changed live value refreshes the face, including after Undo; inspection keeps its scroll position. Zero modifiers disappear rather than leaving `+0` labels.

Values come from the host's existing typed Core contributions and are filtered by four independent, default-off per-match Experimental options: **Items**, **Minion contribution**, **Active effects**, and **Card effects**. Card effects include typed card-text contributions and persistent card/continuous rule sources. Enabling several categories sums them; zero results remain absent. This is a display filter only: authoritative action and combat totals still compose every applicable contribution. Only already-visible faces receive values in a seat projection; no hidden card instance is added. Wire /52 carries the immutable options and the resulting sparse value projection; experimental snapshot envelope 6 preserves their match identity while Core snapshot schema 59 remains unchanged.

## Resolved animation values (W-015)

Attack spotlight hero/spell faces capture the same Experimental-category-filtered overlay visible when the scene was built. A public resolved Attack total remains a match-log fact, but it does not replace the face overlay or bypass disabled categories. The captured face survives projection refreshes and queued animation playback, and is discarded with its scene on exit/Undo. This does not extend an effect's gameplay lifetime.

Shared modifier numerals use modest white outline weight with a dark shadow for contrast. Anchors, right-edge fitting and level-up exclusion are unchanged.

## Printed value ownership

Which region a value belongs to is decided by the action it is printed on and by its `PrintedValueChannel`, never by the card's name and never by reading the sentence: the primary action is the one whose origin says `PrintedPrimary`, the secondary strip is every other action the card itself prints, the two qualifiers a label can carry are p.13's Ranged classification and p.14's Block, and p.16's Ultimate - which prints no Initiative and has no action at all - reads its icon from the permanent ability instead. That rule is what keeps this build without a printed-text parser, and `MatchHudPresentationTests` asserts the four regions partition the definition's printed values exactly, each drawn once.

## Printed ink and source text

The words themselves are never colour-coded, exactly as a printed card's are not, and `CardSourceContent` still distinguishes transcribed words, a read face printing none, and a synthetic fixture with no source face. Transcriptions display verbatim; [RULES_LEDGER.md](../RULES_LEDGER.md) owns their current source-verification status. Every plate a card draws - flag, bands, badge - shifts the card's own colour away from its text colour, so measured contrast can only rise.

## Wire and visibility

Wire protocol `guards-private-online/50` adds visibility-scoped nonzero card-value deltas. Rows reuse already-transmitted card identities where possible, with explicit owner/versioned-face identities for authorized inspection-only faces. Compact signed values retain an Int32 escape; existing frame limits and retained history are unchanged. Earlier peers are rejected by the admission tuple. Snapshot schema remains v59. See [online architecture](PRIVATE_ONLINE_ARCHITECTURE.md) for transport and historical compatibility amendments.

Current wire `/51` additionally carries nullable `ResolvedAttackTotal` on public match-log rows. A reserved kind bit signals an appended Int32, avoiding an extra presence byte on every retained row. Absence remains distinct from a resolved zero or negative total. Earlier peers are refused before decoding; snapshot schema, retained history and frame budgets remain unchanged.
