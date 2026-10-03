# Expected-Outcome Tables

Canonical active shard for milestone 9.13's table executor. Return to [../TESTING.md](../TESTING.md). The roadmap owns scope ([Phase 9](../Roadmap/PHASE_9.md)); this shard owns only how a table is written, run and read.

## What it is

A table is one JSON file per hero under `Assets/Tests/ExpectedOutcomes/<hero>.json` (with a hand-written `.json.meta`: `fileFormatVersion: 2` and a fresh 32-hex `guid`, LF, no BOM). Each case says, in words a reader of the printed card can write without knowing the board, what the board is, what is played, how each question is answered and what must follow. `GuardsDigital.Tests.EditMode.ExpectedOutcomeTableTests` runs every case as one NUnit case named `<file>/<id>`. It is pure Core/Application and runs in the dotnet harness and in Unity Edit Mode alike. `_smoke.json` is the executor's own proof: it touches every phrase below and states what the engine does, not a reading of the rules.

## Writing a case

```json
{ "hero": "Wasp", "cases": [ {
  "id": "wasp-shock-defeats-a-defended-hero", "card": "Shock", "action": "Attack",
  "source": ["<card image file>", "rulebook p.13"],
  "setup": { "actor": { "level": 1 }, "pieces": [ { "ref": "enemy-hero-1", "where": "adjacent to actor" } ],
             "enemy-hero-1": { "defends-with": 2 } },
  "answers": [ "enemy-hero-1" ],
  "expect": [ { "offered": true }, { "ref": "enemy-hero-1", "is": "defeated" }, { "enemy-team-lives": 4 } ] } ] }
```

Phrases (format v1 plus the v1.1 additions) and how the executor reads them:

- References: `actor`, `enemy-hero-N`, `friendly-hero-N` (the 3v3 line-up's seats in setup order; the actor is Red with the next two dealt heroes, the following three are Blue), `enemy-minion-N`, `friendly-minion-N` (`type` melee/ranged/heavy, default melee: the official setup's minions of that side and type), `token-N` (`kind` by the token's printed name, `owner` actor/enemy/friendly), `obstacle-N` (no `kind`: a staged obstacle; `terrain`: one of the map's own terrain spaces; `rock`/`tree`: that token). `turret` is refused.
- Placements, relative and resolved in order: `adjacent to X`, `adjacent to X and Y`, `distance D from X`, `... in a straight line`, `... in a straight line through Y`, `... off every straight line`, `distance D beyond Y from X`, `on the straight line through X and Y`, `empty spawn point within distance D of X`, `<placement> and <placement>`, and a trailing `toward X` / `away from X` preference. The actor stands on the most open space of the official map (`BeforeActionStepPositions.Centre`); each placement takes the first matching free space, straight lines first, rotating one direction per placement made from the same piece, then by `(q, r)`. If a placement cannot be met there the next most open space is tried as the anchor. Minions always stand in the current Battle Zone (p.19 corrects any other minion as soon as an action ends); `battle-zone: true` puts any piece there. Unnamed heroes stand eight or more spaces away; every unnamed minion is removed.
- Setup: `actor.level` 1-8 (Level 8 also unlocks the Ultimate), `actor.coins` (staged through `MatchStagedProgression`), `actor.items` (`Attack +1` etc.: a deck card printing that item icon is set aside and placed as p.16's level-up places it), `actor.ultimate`, `hand`/`discard` for the actor or `<ref>.hand`/`<ref>.discard` (the named card is always put in hand; the default hand is the printed starting hand with the case card replacing the card of its colour), `team-lives`, `enemy-team-lives`, `<ref>.defends-with` (a Defense value, a card name, or `none`), `standing` (`actor played <card> last turn` plays a whole earlier turn; `<ref> played <card> this turn` plays that card before the actor, and fails when its Initiative does not come first; the standing card's own questions are answered the doing way), and for `"action": "Defend"` the attack `{ "by": <enemy hero>, "value": N }` (that hero's own card printing Attack N, preferring non-ranged and textless; its own questions target the actor, the rest the doing way).
- Play: `action` `Skill`, `Attack`, `Movement` (primary, else secondary), `Hold` (p.12), `Defend`/`Defense`. `offered` is read from the real offer; the play then starts normally, or through `StartActionIgnoringFeasibilityForInternalTest` only when the table expects `offered: false` and lists consequences.
- Answers, consumed in order: a reference; `decline` (also closes a multi-selection); `distance D` (a Movement walk of cost D, a printed number, or a space D from the piece that moves; `distance 0` stops); `toward X`/`away from X` (a direction, or the nearest/farthest offered space; paired with an adjacent `distance D` it is one walk); `space:`/`place:` `<placement>`; `option: <bullet text>` (matched to the printed bullet, `•` or ` / `-separated, by position among the Choose node's arms); `card: <name>`; `defense: <value|name|none>`; `<ref>: discard <card>` and `<ref>: take defeat` for that hero's own question. A confirm-or-decline question not answered `decline` is confirmed without consuming an answer, as is a question offering one command.
- Expectations: `offered`; `is` defeated / not defeated / removed; `at`; `moved` (the walked Movement cost, else start-to-end distance); `pushed`; `tokens` with `on-board`, `supply` (the actor's), `on-board-after-round-end`; `coins`; `level`; `hand` (change from the play's start); `attack` (the actor's resolved Attack total); `team-lives`, `enemy-team-lives`; `card` with `state` hand/played/resolved/discarded/retrieved; `targetable` (in any targeting question asked); `chose` discard/defeated; `bullet` with `offered`; `minion-battle` red/blue/friendly/enemy/tie and `lane-pushed` (both run the round out: every hero Holds, the end of round is answered by its first legal command); a `token-N`/`obstacle-N` the setup never placed names a token the play placed, matched as a set against every such `at`.
- A Defeat is read from the match log, else from the scoreboard's Deaths and minion kills (the log is silent on some card-text Defeats and Removals); `removed` means the piece left the board without a Defeat. `immune`, `minions-in-lane` and `handicap` are refused by name.

## Executor defaults

### Batch extensions (9.13a)

- Another hero's setup may name `hero` (printed name or content ID) and `coins`. An explicit `attack.ranged` boolean selects a matching printed Attack and roster hero; without it the attack-value fixture can choose any matching hero. Geometry and prerequisites must still make that actual printed card legal. Every staged hero starts at Level 1 unless the actor's level is stated.
- `standing` may append `using Hold` to its existing phrase. This resolves the named card without applying its primary text, for a scenario about the type of card played rather than its action's consequences.
- A non-Defense case may add `follow-up: {hero, card, action, answers, offered}` for another hero later that turn, or `turn: "next"` for the next turn. That printed card is reserved in hand, committed on the specified turn, and executed through ordinary commands, even when the original case only asserts its offer. The intervening heroes Hold. `offered` must be boolean and `answers` a string array; `offered: false` probes an unavailable action without forcing it. Movement selects the primary or secondary Movement printed on the card. Expectations read the resulting board; the original case's `offered` still describes its original action. Defense follow-ups are refused, not ignored.
- A Defend case defaults to using its named card when offered, otherwise declining; explicit `defense:` answers take precedence. The primary Defense's availability is checked separately from a secondary value on the same card.
- `{ref, marker: "Poison"}` checks an attached marker by its displayed name; `{ref, "attack-modifier": -1, "defense-modifier": -1, "initiative-modifier": -1}` reads live value contributions from a restored snapshot. `{ref, "defense-offered": {"Aspiring Duelist": false}}` checks named Defense cards in the actual question; a missing question fails rather than proving exclusion vacuously.
- `EveryBatchFace_HasAnExecutableOutcomeTable` requires each completed batch's ordinary dealt face to have an executable table with an outcome, and its Ultimate to have a scenario with the Ultimate unlocked. The per-case tests execute those outcomes. Boxed Handicap faces and explicit extra coverage gaps are reported separately, never counted as executed coverage.

Each is taken only where the table says nothing, and the trace names it:

- An enemy seat with a numeric `defends-with` is given, in roster order, the first dealt hero (never the actor's) whose deck prints a card with that Defense.
- Another hero's own which-card question (an enemy's discard, the card a friendly hero retrieves) the answers do not name takes that hero's first card in hand; p.19's displaced-minion correction takes the first space offered.
- A printed card name must be spelled as the content spells it; a spelling the content does not print is reported as an unresolved card. (9.13 bridged one such name, Mortimer's BRAAAAAAAAINS...! printed with eight a's against a seven-a display name; 9.14 corrected the display name and retired the bridge.)
- `handicap: true` is not executed (p.6 boxes the Handicap cards). A placement's parenthetical is ignored. `action: Defense` without `attack` fails by name.
- Staging prefers open ground clear of terrain (unless terrain is staged) and puts every "in a straight line" piece from one piece on its first file, relaxing each only when no anchor meets it. A `toward X` walk follows X's file, so a straight-line trample runs through the named pieces.
- A trailing `decline` of a question never asked declines nothing; a selection at its maximum closes itself; a "one or both" bullet listed later is selected where the choice asks it; a play expected not offered that the rules refuse even past the offer leaves the board unchanged.

## Dispositions

A failing case is dispositioned in `<hero>.dispositions.json` (+ `.meta`), an array of `{ "id", "kind", "ref", "note" }`:

- `ruling` - the reader's reading collides with a settled ruling (`ref`); `expect-override` (and, where the ruling moves a question, `answers-override`) replaces the case's facts, so the case fails if the ruling's behaviour regresses.
- `engine-defect`, `executor-gap` - fixed in the engine or the executor; the case runs as written.
- `not-executable` - with `"execute": false` the case is not run (why in `ref`/`note`); otherwise it runs and must pass, and the reader's own note is recorded instead of failing.
- `open-question` - not executed; `EveryDisposition_NamesACase_AndTheOpenQuestionsAreCounted` prints each for the user under its own count.

A case with no disposition runs as written, and a reader's `unsupported` note fails it.

## Running

Run the dotnet-harness filter `ExpectedOutcomeTableTests`; add `Name~<case id>` to run one case, for example:

```
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --filter "FullyQualifiedName~ExpectedOutcomeTableTests&Name~wasp-shock-defeats" --logger "console;verbosity=detailed"
```

The detailed logger prints the case trace: the line-up, the anchor and every piece's space, the board at the play, each question with the answer that met it (or `implicit`), the board and match log after it.

## Reading a failure

Every message starts with `<file>/<id>:`. `unsupported <kind> "<phrase>"`, `unknown card name`, `unresolvable placement` and `setup ...` are the executor refusing a phrase it cannot stage; `the answers do not cover the <question>` and `answer "<a>" does not answer the <question>` name the engine's question and its candidates; `expected {fact} - <what the engine did>` is a disagreement to disposition (engine defect, reader misreading, or open question); `answers never asked for` and `defends-with was never asked` mean the engine asked less than the reader expected; `DECLARED UNSUPPORTED (not executed)` repeats a reader's own `unsupported` note, so a case is never counted as agreeing with a fact nobody checked.
