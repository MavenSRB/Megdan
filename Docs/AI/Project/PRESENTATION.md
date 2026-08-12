# Presentation and Accessibility Direction

Canonical active shard. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md).

## Presentation direction

- The Unity scene is 3D from the beginning, with the board on the XZ plane.
- Early implementation uses primitive meshes and a fixed camera.
- The minimal-grid validation scene centers its fixed camera directly above the map and looks straight down.
- Pointy-top presentation maps `q` eastward and decreasing `r` northward; the same outer-radius value defines both the regular hex mesh and its center spacing.
- Placeholder hexes use one light-gray fill with a darker gray perimeter for separation; this styling is non-semantic and the logical origin has no unique treatment.
- The polished pitch uses a stylized tabletop presentation.
- Heroes and minions appear as 3D miniatures on clearly marked team bases.
- Miniatures may briefly animate when acting.
- When the camera approaches top-down, units automatically crossfade to readable 2D tokens.
- Separate enter and exit thresholds prevent visual flicker near the switching angle.
- One logical unit owns interchangeable 3D-miniature and 2D-token representations.
- Standard minions use the circular artwork embedded along the top of the private reference atlas `Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg` when they first become visible in milestone 4.4. Steam/orange maps to project Red and Crystal/light-blue maps to project Blue; the round riveted/broad spiked face is Heavy, the narrow helmet with cheek guards/armored shoulders is Melee, and the two-pronged helmet/narrow crystal crest is Ranged. The source manifest's `Melee_Steam` label on the Crystal melee sprite is a known metadata error. Milestones 9.3 and 9.5 retain these images as the 2D side of each minion's interchangeable presentation; authoritative team/type data selects the image, and rules identity is never inferred from art or the incorrect source label.

### Camera

- Board-focused rather than free flight.
- Bounded pan, orbit, and zoom.
- Supports angled tabletop and top-down views.
- Includes a whole-board reset/focus control.
- Cannot travel beneath or far outside the board.

### Animation and audio

- Rules resolve independently of animations.
- The presentation layer consumes ordered rule events and animates them.
- Skipping or accelerating animation never changes game state.
- Target animation coverage for each of the six pitch heroes: idle, movement, attack, hit/defeat, and respawn, reusing shared rigs/clips where suitable without sacrificing hero readability.
- Minions use a smaller shared animation set.
- The pitch receives a small sound-effects pass for UI confirmation, card reveal, movement, attack, hit/defeat, minion battle, and lane push.
- Music, voice acting, announcer systems, and elaborate adaptive audio are out of scope.

## Pitch HUD

- The board is the primary focus.
- Director mode exposes all six hands through clear Red/Blue team and hero grouping. Private online clients expose only the local player's hand plus authorized shared played/revealed state; both modes keep the active resolver readable without presentation owning legality.
- Show a compact read-only 6-hero-by-4-turn card history grouped as three Red and three Blue heroes; each entry derives from the authoritative `HeroId` assignment and reveals no committed card before the joint reveal.
- Show round, turn, active resolver, Lives, Waves, hero levels, and coins.
- Use contextual prompts for actions, targets, and choices.
- Present one contextual player-facing **End Turn** control instead of separate Hold and Movement Stop controls. At ordinary unresolved-card action selection it invokes the exact authoritative Hold offer; while an optional Movement continuation is pending it invokes the exact authoritative Stop choice, ends that Movement action, and therefore ends the current turn. The control is hidden or disabled whenever the authoritative projection exposes no legal turn-ending invocation, including mandatory choices that cannot be bypassed.
- **End Turn** is presentation terminology, not a generic rules command. Its view model carries the exact currently legal typed offer or pending-choice invocation supplied by authority; the view never infers legality from its own phase state. Future turn-ending functionality reuses the control by projecting its own typed invocation, without making Hold, Movement Stop, or later rule operations interchangeable.
- Present eligible Fast Travel as a clickable `FAST TRAVEL` action-mode button only when the immutable application projection supplies a replacement with at least one legal destination. Eligibility alone does not activate the mode: normal actions and an unhighlighted board remain the default until the player selects the button, after which the board exposes only the projected destinations and forwards one selected coordinate through the existing typed command path.
- Provide internal Undo, restart, animation-speed, and camera-reset controls.
- Provide a collapsible event log.
- The configured prototype card panel uses a 1280-by-360 preferred desktop size so four content-sized Defense-card choices plus Decline fit simultaneously at the validated 2048-by-1152 Game View without scrollbars. It remains inside the viewport and scrolls its hands/action body when width or height is insufficient, keeping every projected Defense-card and Decline control reachable on short desktop viewports.
- Each configured legal Defense-card invocation identifies its authoritative card and labels the offer-bound value as `PRINTED DEFENSE`; the exact typed command remains in the projected invocation, while modified Defense Total, eligibility, and response resolution remain outside presentation.
- Defense-choice controls must size or wrap from their complete rendered label so the card name and printed value remain readable; a model containing complete text does not satisfy this requirement when the configured control clips it.
- Planned milestone 5.8b adds a compact pointer-pass-through Defense breakdown above the hovered or keyboard-focused legal Defense choice. Moving across choices updates the same preview without changing authoritative state; clicking retains the current one-click typed response. The immutable application projection supplies Attack and per-choice Defense contributions, totals, explicit Block/result meaning, and player-readable source identities, while presentation only formats them. As soon as the Defense response opens, the board marks the attack-scoped minions in its already-calculated Defense modifier by projected stable identity: supporting and hindering sources receive distinct non-target cues with visible `+1` or `-1` indicators, never color alone. Those cues remain unchanged while the player moves across cards or Decline; they clear or rebuild only when the pending Defense response or projection changes. Decline previews no Defense Total without changing the attack-scoped cues. The popup avoids the focused control and remains inside the viewport.
- No main menu, settings suite, tutorial, collection screen, or account UI is required for the pitch.

## Accessibility baseline

- Team identity never relies on color alone.
- Red and Blue also use distinct symbols, base markings, outlines, and labels.
- Cards and state values must remain readable at common desktop resolutions.
- UI structure must allow later scaling.
- A full accessibility settings suite is deferred.
