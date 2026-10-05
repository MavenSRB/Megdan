# Private lobby presentation

Canonical current lobby/picker contract. Return to [project context](../PROJECT_CONTEXT.md).

## Experimental menu

The host screen now exposes an **Experimental** button with a per-match teammate coordination toggle, reset off for a new hosted game. [The owning milestone](../Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md#x-958-implement-and-gate-experimental-teammate-coordination) records its gate status. The [accepted timer settings](EXPERIMENTAL_FEATURES.md#experimental-menu) follow separately at 9.56; the normal map selector remains outside Experimental.

## Setup and selection

The entry presenter retains its known match-format label when a return/error call omits it; explicit empty still denotes a setup that offers no format controls. Host/Join, Leave, failed joins and retries therefore preserve the applicable 2v2/3v3 and Short/Long setup controls without restarting. Authoritative session state continues to decide which actions are offered.

A clicked hero receives an immediate gold border (no text suffix since 8.76; a taken hero is greyed with no reason text anywhere, its hover still opening the pick card), retained across refresh while the choice remains legal and it is the player's turn. PICK HERO confirms that candidate through the existing authorized protocol. Losing the pick turn or availability clears the provisional highlight. Seat rows name only heroes whose roster entry has an actual confirmed pick, never composition placeholders.

The pre-match lobby uses an opaque dark backdrop and a gold game-title wordmark rather than showing the map. After the match starts the backdrop/title disappear and normal match handoff/reconnect presentation applies. Its cloned panel settings use width-based scaling before the match, restoring the shared match-HUD scaling afterward. The lobby frame scrolls vertically within the viewport; short windows must not miniaturize the controls or hide the confirmation path.

## Source overview cards

Hovering or focusing a hero option displays its original overview front, including unavailable options. One non-interactive aspect-preserving image is anchored beside the option and clamped to the screen. Leaving, changing layout or leaving selection hides it. Missing private resources hide the preview rather than inventing artwork or blocking selection.

[Prepare-HeroSelectionArtwork.ps1](../../../Tools/Prepare-HeroSelectionArtwork.ps1) owns the exact eleven verified source-to-resource mappings from the private Docs hero-selection corpus. It copies byte-identical fronts to ignored `Assets/Resources/Private/HeroSelection`, refuses mismatched existing targets, and supports a read-only `-Check`. The existing configured artwork importer preserves source dimensions without mipmaps. Prepared artwork remains private and excluded from tracked publication.

Focused presentation/input regressions live in `PrivateLobbyPlayModeTests`; model state and draft rules remain owned by their application fixtures. Follow [testing policy](../Testing/POLICY.md), including non-batch visual inspection for changed output.
