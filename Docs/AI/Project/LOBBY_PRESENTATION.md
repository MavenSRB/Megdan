# Private lobby presentation

Canonical current lobby/picker contract. Return to [project context](../PROJECT_CONTEXT.md).

## Experimental menu

The host screen now exposes an **Experimental** button with a per-match teammate coordination toggle, reset off for a new hosted game. [The owning milestone](../Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md#x-958-implement-and-gate-experimental-teammate-coordination) records its gate status. The [accepted timer settings](EXPERIMENTAL_FEATURES.md#experimental-menu) follow separately at 9.56 and appear under their own **Turn timer** heading rather than reading as part of Card Modifiers; the normal map selector remains outside Experimental.

## Host invitation address

A keyed host displays the public IPv4 and bound port guests should use. Discovery first asks the local router through UPnP and, when that optional feature cannot supply a public address, uses a bounded HTTPS lookup against fixed public-IP endpoints. Every response is treated as untrusted and displayed only when it is exactly one valid public IPv4. If both routes fail, the lobby labels the LAN address as a fallback rather than implying it is internet-reachable. This discovers an address only; the host still owns firewall and port-forwarding setup.

## Setup and selection

The entry presenter retains its known match-format label when a return/error call omits it; explicit empty still denotes a setup that offers no format controls. Host/Join, Leave, failed joins and retries therefore preserve the applicable 2v2/3v3 and Short/Long setup controls without restarting. Authoritative session state continues to decide which actions are offered.

A clicked hero receives an immediate gold border (no text suffix since 8.76; a taken hero is greyed with no reason text anywhere, its hover still opening the pick card), retained across refresh while the choice remains legal and it is the player's turn. PICK HERO confirms that candidate through the existing authorized protocol. Losing the pick turn or availability clears the provisional highlight. Seat rows name only heroes whose roster entry has an actual confirmed pick, never composition placeholders.

The pre-match lobby loops the supplied silent 1920x1080 sword-scene video, with a prepared still fallback. Main offers Play and Options; Play opens Host, Join and Back. Options currently identifies settings as coming soon. The supplied transparent logo and wooden action-button PNGs retain their aspect, with authored hover sprites also used for keyboard focus. Main/Play artwork aligns to the cover-scaled scene; longer forms and the connected lobby remain centered and scrollable. Warm cream labels, dark brown panels and amber-edged fields carry the menu style through setup and selection. Host/Join forms retain their existing values, validation and presenter invocations; Cancel uses the supplied Back artwork and returns to Play. The video pauses during match handoff and resumes when the menu returns; its decoder and render target are released on destruction. Runtime ownership is `MegdanMenuVideo`; UI layout remains in `PrivateLobbyUnityView`.

## Source overview cards

Hovering or focusing a hero option displays its original overview front, including unavailable options. One non-interactive aspect-preserving image is anchored beside the option and clamped to the screen. Leaving, changing layout or leaving selection hides it. Missing private resources hide the preview rather than inventing artwork or blocking selection.

[Prepare-HeroSelectionArtwork.ps1](../../../Tools/Prepare-HeroSelectionArtwork.ps1) owns its verified source-to-resource mappings from the local Docs hero-selection corpus. It copies byte-identical fronts to `Assets/Resources/Private/HeroSelection`, refuses mismatched existing targets, and supports a read-only `-Check`. The existing configured artwork importer preserves source dimensions without mipmaps. W-020 tracks the prepared runtime artwork through Git LFS in the private project repository; the source corpus remains local and excluded.

Focused presentation/input regressions live in `PrivateLobbyPlayModeTests`; model state and draft rules remain owned by their application fixtures. Follow [testing policy](../Testing/POLICY.md), including non-batch visual inspection for changed output.
