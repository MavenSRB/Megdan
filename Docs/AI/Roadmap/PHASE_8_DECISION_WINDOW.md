# Phase 8 - The decision window

Return to [roadmap](../ROADMAP.md). Current authorization and next route live in [the brief](../SESSION_BRIEF.md).

## Authorization and scope

On 2026-09-18 the user reviewed the prompts the round-action strip shows above the hand - "Who acts first?", a card's "Choose one" buttons reading `MODE #1 RELOAD-BORROW-ARM`, the accept-or-decline banners - and reported that playtesters do not understand what they are choosing and often do not see the controls at all, because that strip reads as status rather than as a question. They designed a replacement row by row, reviewed a mockup drawn over live captures of the seventeen-hero build, and authorized the build: "Ok, get on it then." Two later instructions bind the whole series: nothing new may overlap anything that exists at any resolution, with every text fitting and a defense window holding four candidates; and every new element inherits the HUD's one scaling declaration rather than getting its own. The sequence is **8.46 -> 8.46a -> 8.46b -> 8.46c -> 8.46d -> 8.46e -> 8.46f**, built in a separate worktree while a concurrent agent packages 8.45, and merged after.

### The settled design

One new HUD element, the **decision window**, with two states. **Expanded** covers the open area between the two hero rails, the board included: the card or cards in question, at the tray's own card size, one control per choice, its text saying what happens when pressed. **Docked** is a slim bar in the round dock's own rectangle beside the hand: the question, a running count where one applies, and at most one verb. A button-answered decision opens expanded; a board-answered decision opens docked; the player can toggle either way, and every new decision re-decides the state rather than remembering the toggle (Brogan's Throwing Axe expands for its arm, then docks for its board target). Only the seat that owns the decision gets the window; every other seat keeps the narration line. Team agreements are owned by every participant. On the Director composition the title names the side. The seat undo square stays visible above it.

| Decision | Verdict |
| --- | --- |
| Card "Choose one" modes | Expanded: the card, one button per printed bullet |
| Printed number and colour choices | Expanded: the card, value buttons; Bain's guess shows the target, never the hidden card |
| Direction (Tali's Cold Snap) | On the board: six arrows around the caster, docked question |
| Optional step (`may` nodes) | Expanded: the card, two buttons naming the consequence, never `ACCEPT`/`DECLINE` |
| Tied initiative, seat pick and team agreement | Expanded: the tied cards as faces, one button each; teammates' picks read "Tali prefers Sabina"; re-pressing replaces a pick |
| Level-up upgrade | Expanded, minimisable (user, 2026-09-18 23:00: "players want the same treatment for level up screen"): everything the strip shows now - the hand preview, each upgrade pair as faces, the coins paid and remaining, the set-aside card and its item |
| Defense | Expanded: the attacker's card separated from the offer; four candidate faces with their printed defense; decline small in a corner |
| Removal prevention | Same window as defense |
| Card chooser (discard, retrieve) | Expanded; the purpose line becomes the question |
| Minion Battle removals | Docked, single-verb ratification (below) |
| Displaced-minion correction, respawn | Docked question only; both already resolve on the click |
| Movement, attack target, piece and space targets | Stay in the strip |
| Commitment, End Turn, turn release, fast travel | Stay |

Cross-cutting rules: no undo inside the window (the top seat undo covers a wrong press inside a turn; every answer outside a turn is final on press or restatable already); choose-exactly-one answers in one press; choose-several keeps Done and a chosen control toggles off when pressed again; Withdraw My Agreement survives as "Not yet", the one take-back the seat undo cannot replace; labels name the consequence, never the mechanism; **card faces keep the tray's own footprint** (300.46 by 420 reference units) and are never drawn at a smaller scale, so the expanded window spans the whole open area between the two hero rails - 1,984 units at the reference - rather than the board alone, which is what lets four defense candidates and the attacker sit side by side at full size. **Every control wears the HUD's shared button treatment** - the shared chrome and press-and-hover motion of `HudElements.Control`, the silver swallowtail banner for the one control that moves the game on, the Defense option's scale motion for a card face used as a control, the strip's own selected and unavailable states - and `HudButtonTreatmentTests` gains the window's cases, so no control in the window has a look of its own. **The phase banner (8.47) yields to an expanded window opening on the same refresh** - the window is then the thing nobody misses, and the transition is not owed later - while a docked bar and the banner coexist, since they never share the middle of the screen. **Minion Battle single-verb ratification**: the bar reads "Remove 2 minions" with the count; anyone on the side clicks minions and everyone sees the highlights; when the count is met everyone gets `REMOVE THESE MINIONS`; on a solo side that is the confirm; on a team the first press marks that hero ready ("Waiting for Sabina" plus a small `NOT YET`), teammates read "Arien is ready. Remove these minions?" with the same verb, and the last press resolves. Any board click by anyone reopens the selection, which the authority already does because a proposal is identified by its selected set. The `TEAM AGREEMENT` heading, the agree-or-withdraw labels and the per-hero ledger go.

### 8.46 The series head

Outcome: this shard, the inert label vocabulary both parallel blocks build against (`DecisionLabel` and `DecisionLabelCatalog` in `Core/Cards`, answering nothing until 8.46a fills them), and the mockup evidence. Completes with the series.

### [x] 8.46a Decision labels

Outcome: every Choose arm and every optional-step node that can raise a pause in dealt content carries a declared consequence label, readable through `DecisionLabelCatalog.TryGetLabel(cardId, nodeId)`, so a control can say "Defeat a minion adjacent to you" instead of its node id, and "Move it again" / "Stop here" instead of accept and decline.

Acceptance: a per-hero label table beside each hero's effect content, aggregated by the catalog exactly as `MatchContentCatalogs` aggregates handlers; a `DecisionLabel` carries the chosen text and, for an optional step, the declined text. A content check, run by the offline harness and Edit Mode, derives the set of nodes that need a label from the resolver's own rule - the child arms of every `Choose` node and every `may` node the resolution session opens confirm-or-decline on - and fails when any dealt definition lacks one or when a label names a node that does not exist; the derivation is shared with the session, not a second list. Labels name the consequence in the printed text's own words, upper-cased by presentation rather than authored in capitals. No card definition, digest, snapshot schema or wire revision moves: the digest hashes ids and revisions only, and the table sits beside the definitions rather than inside them. Roughly twenty-six Choose nodes and fifty-seven `may` nodes across the dealt roster.

Non-goals: labels for board-answered candidates; reading printed text at runtime; presentation changes.

### [x] 8.46b One press for a choice of exactly one

Outcome: an effect choice whose minimum and maximum are both one resolves on the selection itself, so no `Done` follows; a choice admitting several keeps Select, Deselect and Done exactly as today.

Acceptance: the rule lives in the resolution session where Select is applied, not in presentation; the pause for an exactly-one choice never stands on Deselect and Confirm; a seat rewind steps back the single accepted command; confirm-or-decline, stop and decline are unchanged. Every seeded expectation that walked through a one-of-N effect choice is re-derived and the count recorded; the offline harness and focused Edit Mode are green. Card-text attack targets named through `set-attack-target@1` still raise the attack wall on that one command.

Non-goals: any change to the command DTO, the pause's projected shape, or the schema; the Minion Battle confirm; defense.

### [x] 8.46c The decision window

Outcome: `DecisionWindowUnityView` and `DecisionWindowPresenter` (an `ISeatScopedPresenter`, composed in `ConfiguredMatchSceneRuntime` and laid out by `ConfiguredMatchHudRenderer`) present every family the table above moves, in both states; the strip stops drawing those families' controls.

Acceptance: expanded is one fixed rectangle from the Red rail's right edge to the Blue rail's left edge, below the status strip and above the tray, each a margin apart; docked is `CalculateStatusDockRect`, every size a reference unit under `MatchHudPanelScaling`, nothing positioned from screen percentages; the state is chosen from the decision's shape and re-decided per decision, with a minimise and expand control; the window opens only for the owning seat, using `AwaitedInputQuery` and `DecisionOwner` rather than a new predicate, and names the side on the Director. Effect choices of Mode, Number, Colour, OptionalStep and Card subjects; both tied-initiative forms; defense; removal prevention; docked Minion Battle with the single verb and "Not yet"; docked correction and respawn with the resolve-without control. Labels come from `DecisionLabelCatalog` with today's label as the fallback when the catalog answers nothing. Direction choices are classified board-answered and open docked. The seat undo square, End Turn, both rails, the status strip and the tray are never covered. A defense of four candidates plus the attacker fits at the reference with every face at the tray's footprint. Focused Edit Mode presenter tests for every family and state, and the existing Play Mode journeys still pass by finding controls through the model rather than by fixed text. Any new Edit Mode fixture that references Presentation is excluded from the harness by name in `Tools/DotnetTests/UnityOnlyTests.props`.

Non-goals: movement, attack target, commitment, End Turn, level-up, fast travel, turn release; the attack spotlight's own staging, which the defense window sits beside rather than over; new authority commands.

### [x] 8.46d Direction on the board

Outcome: a declared `HexDirection` choice is answered by six arrow affordances around the choosing hero on the board, and the docked bar asks "Choose a direction".

Acceptance: the arrows are board interaction, composed like every other board-answered candidate, cued and cleared with the pause; each arrow submits the same Select the button did; the board-visual smoke still passes; a focused Play Mode case drives Cold Snap through an arrow.

Non-goals: any other vocabulary; changing what Cold Snap does.

### [x] 8.46e Geometry, readability and journeys

Outcome: the window is proven clear of every named element and readable as drawn, and the journeys press consequence labels.

Acceptance: a Play Mode gate measures the window's live bounds in both states against the status strip, End Turn, both rails, the DIRECTOR toggle, the tray and the seat undo square at 2560x1440 and at the aspect-ratio gate's narrow and wide cases, asserting no intersection, using `UiPlayer.PixelSnapping`; a four-candidate defense and the longest label in content are laid out and measured to fit their controls without clipping; every journey that pressed `ACCEPT`, `DECLINE`, `DONE` or a `MODE #` control reads the standing label from the model.

Non-goals: new families; packaging.

### [~] 8.46g The level-up upgrade in the window

Authorized 2026-09-18 at 23:00, mid-playtest, in the user's words: "I lied, players want the same tretmant for level up screen, to be minimisable. So we need to put it in new thing you made, and show all information that we show now."

Outcome: rulebook p.15's Upgrade a Card question opens in the decision window, expanded and minimisable, carrying every fact the strip's prompt carries today - the hero and the level reached, the coins paid and remaining, the hand preview at its reduced scale, one control per upgrade pair drawn as the candidate face with the card it replaces, the set-aside card and the item it becomes - and the strip yields the family exactly as it yields every other family the window took. Docked, the bar asks the one-line question with the hero's name.

Acceptance: `DecisionWindowPresenter` composes a `LevelUp` family from `PendingSystemTransitionDescription` through the same `SystemTransitionPromptViewModel` the strip built, so no fact is re-derived; the faces keep the tray's footprint; the geometry gate covers the expanded level-up at every viewport; the level-up journeys press the window's controls; the strip test that pinned the comparison band moves to the window.

Non-goals: the Lane-Push placement prompt, which stays on the board; any change to what p.15 offers.

### [~] 8.46f Integration and evidence

Outcome: the four parallel branches merged onto the series branch and then onto main behind 8.45; complete Edit Mode, Play Mode and offline harness runs (T2 promoted to complete suites because the change crosses Core, Application-facing presentation and Play Mode journeys); baselines and archive rows recorded; the design shard, brief and testing shard updated.

Non-goals: a new package.
