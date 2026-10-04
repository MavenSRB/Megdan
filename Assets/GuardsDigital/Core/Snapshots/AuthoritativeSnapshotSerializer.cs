using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Snapshots
{
    public static partial class AuthoritativeSnapshotSerializer
    {
        private const string Magic = "GUARDS-AUTHORITATIVE-SNAPSHOT";
        private const int MaximumDecodedBytes = 4 * 1024 * 1024;
        private const int MaximumBase64Characters = ((MaximumDecodedBytes + 2) / 3) * 4;
        // v20: milestone 7.4 puts every team's remaining Life counters in the round section, immediately
        // after the items - one count-prefixed team id and counter total per participating team. It is
        // the first milestone in which a Life counter can change: p.13's "Defeated hero's team spends
        // the number of Life counters, equal to that hero's lowest card Tier" is the only rule that
        // moves one, and until this milestone the count existed solely as the setup constant
        // `QuickGameTeamSetupState.LifeCount` that the HUD renderer printed.
        // v19 is rejected rather than migrated, and here the reason is the sharpest of the series: a
        // v19 payload records no counters, and the only reading available for their absence is "nobody
        // has spent any", which is a statement about a resource whose exhaustion loses the game. It is
        // right for every payload a v19 build could produce, because no v19 rule spends one - and that
        // is precisely why it must not be written into the reader, since the first payload it would be
        // wrong about is the first one this milestone writes, and a restored match would come back with
        // a full pile and a team that had already lost. No persisted state exists outside the
        // version-controlled corpus to strand.
        //
        // v19: milestone 7.2a puts every hero's four rulebook p.16 item slots in the setup section,
        // beside the hero id and player id already there, and splits an item's single slot field into
        // the slot it occupies and the icon it supplies. The user chose the wire over rebinding the
        // slots from configuration at restore, so a restore refuses a build whose dashboards differ the
        // way it already refuses one whose card content differs; the refusal is its own reason,
        // HeroDashboardMismatch, rather than a malformed payload, because the bytes parse and describe
        // content this build does not have.
        // v18 is rejected rather than migrated, and here the reason is not that the absence is
        // unreadable - a v18 payload has no dashboards at all, and every hero this build supplies has
        // the same four slots, so reading one as "p.16's layout" would be right for every payload that
        // exists. It is refused for the reason v17 and v16 were: the guess is a statement about heroes
        // the payload never made, and the first payload it would be wrong about is the first one a
        // verified hero with a different dashboard writes, by which time the migration would look
        // established. The item split is the separable second cause and is unmigratable in the
        // ordinary way: a v18 item names a slot by its icon, which cannot say which of the fourth
        // slot's three icons it supplies. No persisted state exists outside the version-controlled
        // corpus to strand.
        //
        // v18: milestone 7.2 adds every hero's items to the round section, immediately after the Levels
        // - one count-prefixed list of owner, source card instance, slot icon and order. v17 is rejected
        // rather than migrated, and the reason is not the shape of the absence but what the absence
        // would have to be read as. A v17 payload records no items at all, and reading one as "nobody
        // has any" is a guess about state the page makes authoritative the moment a hero levels: p.16
        // gives an item on every ordinary level purchase, so a match restored from a v17 payload written
        // after milestone 7.5 exists would come back with heroes whose dashboards had been emptied.
        // That the guess is right for every payload this build can produce - nothing here gains an item,
        // since the purchase is 7.5's - is exactly why it must not be written into the reader, which is
        // the same argument v17 made for Level one milestone earlier. No persisted state exists outside
        // the version-controlled corpus to strand.
        //
        // v17: milestone 7.1 adds every hero's current Level to the round section, beside the paused
        // system transition - one count-prefixed hero id and Level per participant. v16 is rejected
        // rather than migrated, and the reason is the same one v14 gave for the Battle Zone rather
        // than the weaker "we have one read path per version": a v16 payload records no Level at all,
        // and reading one as "everybody is at Level 1" would be a guess about a value the page makes
        // authoritative from its first sentence. It is a guess that happens to be right for every
        // payload this build can produce, because milestone 7.1 adds no way to change a Level, and
        // that is exactly why it must not be written into the reader: the first payload it would be
        // wrong about is the first one milestone 7.5 writes, and by then the migration would look
        // established. No persisted state exists outside the version-controlled corpus to strand.
        //
        // v15: milestone 6.10a adds the system-transition continuation to the round section - one
        // boolean saying whether a multi-step transition is stopped, and when it is, which transition,
        // which step, who chooses, what they may choose, and which steps have already happened. v14 is
        // rejected rather than migrated: a v14 payload has no field for a paused transition, and
        // reading one as "nothing is running" would restore a match that had silently skipped a step a
        // player still owes. What the steps mean stays in the trusted catalog, so a payload naming a
        // transition this build no longer supplies is refused rather than reinterpreted.
        //
        // v14: milestone 6.10 puts rulebook p.8's Battle Zone on the wire, beside the map reference it
        // is scoped to, because it is now authoritative state the Lane-Push Zone stage advances rather
        // than a derivation from the map's starting-Battle-Zone flag. v13 is rejected rather than
        // migrated, and here the reason is unusually concrete: a v13 payload records where every piece
        // stands and nothing about which Zone the Lane has reached, and no derivation can recover it -
        // an emptied Battle Zone and a Zone the Lane never reached look identical in occupancy, and the
        // flag names only where the match began. Reading a v13 payload as "still the starting Zone"
        // would answer the Minion Battle, the Heavy-immunity accompaniment, the displaced-minion
        // correction and the Lane-Push cause about the wrong Zone for the rest of the match.
        //
        // v12: milestone 6.7's bounded Repeat adds an optional pending repeated-action record to the
        // card-action section's Correction continuation, naming the second instance an action asked for
        // while the correction between the two instances is paused. v11 is rejected rather than
        // migrated: a v11 correction payload cannot state whether the action it belongs to asked to
        // repeat, and reading one as "did not" would silently drop half of a card's printed text.
        //
        // v11: milestone 6.5aa widens Minion Battle candidates and selections from intrinsic minion
        // IDs to stable effective-unit identities, so a counts-as-minion hero can be removed and the
        // resulting off-board hero occupancy can survive restore. v10 is rejected rather than
        // migrated because it cannot encode the widened pending continuation.
        //
        // v10: milestone 6.4a's paused after-action displaced-minion correction joins the card-action
        // section as continuation kind 4, carrying the owning action's identity, the stage, the
        // deciding team, and candidate identities only - routes, costs, and coordinates of subjects are
        // never on the wire, because the correction is a positional predicate the restore re-derives
        // from the board and the Tie Breaker, refusing any payload the derivation does not reproduce.
        // v9 is rejected rather than migrated, per the standing decision: a v9 payload could not
        // represent a paused correction, the engine has one read path per version, and no persisted
        // state exists outside the version-controlled corpus to strand.
        //
        // v9: placed tokens join the board section and attached markers join the round section, each
        // carrying identity, provenance and position only; supply size, display name and marker return
        // condition come from the registered component catalog, which the payload also records so a
        // restore can refuse a build whose component content differs. v8 is explicitly rejected rather
        // than migrated (milestone 6.2 migrate-or-reject decision); v8 rejected v7 because the
        // Active-effect section left semantics on the wire, and v7 rejected v6 for the analogous
        // reason at milestone 6.1s.
        // v21: which heroes bought a level in the current round joins the round section, after the Life
        // counters. Milestone 7.6's Pity Coin step reads the negation of it - rulebook p.8 pays "All
        // players who have NOT leveled up this round" - and that step is a stage later than the Level Up
        // that produces the answer, so a match saved between the two has to carry it. v20 is rejected
        // rather than migrated: the only reading available for the section's absence is "nobody levelled
        // this round", which is right for every payload a v20 build could produce, because no v20 rule
        // records one, and wrong for the first payload this milestone writes - a hero that had already
        // levelled would come back eligible and collect a coin the page denies it.
        // v22: a repeat continuation - how many repeats an instance's chain has already run, and the
        // target it inherited - joins both the pending Skill frame and the owed repeated action.
        // Milestone 8.6's Center of Mass is the first verified card whose repeat count is above one, so
        // "am I a repeat" stopped being the whole answer, and its two siblings print "on the same
        // target", which has to cross an action boundary a snapshot can fall inside. v21 is rejected
        // rather than migrated for the reason v20 was: the only reading available for the section's
        // absence is "this is the first instance and nothing is inherited", which is right for every
        // payload a v21 build could produce - no v21 content declares a maximum above one or a target
        // binding - and wrong for the first payload this milestone writes, where a second repeat would
        // come back believing it was the first and could ask for one more than the card allows.
        // Milestone 8.10 moves it to 23, for two sections a v22 build cannot read and a v22 payload
        // cannot describe. The first is a continuation kind: card text applied inside rulebook p.14 can
        // now stop on a question, and a match holding one persists the paused frame, the moment it
        // stopped at, and the attack whose remaining steps are owed. The second rides the Attack
        // resolution itself - the unit a repeated instance may not take, which Thunder Boomerang prints
        // as "on a different target".
        //
        // v22 is rejected rather than migrated, for the reason v21 was: the only reading available for
        // the new continuation's absence is "no card text is paused", which is true of every payload a
        // v22 build could produce and false of the first payload this milestone writes. The excluded
        // target has the same shape of answer - its absence reads as "this instance may take anyone",
        // which is right for every v22 Attack and wrong for a restored repeat.
        // Milestone 8.11 moves it to 24 for one field a v23 payload cannot describe: what rulebook
        // p.14's Block did. The page decides it at step 3, where the defender reveals their card, and
        // reads it at step 6, and every pause the page allows sits between those two - the defender's
        // own card text among them, which Wasp's Deflect and Reflect stop on.
        //
        // v23 is rejected rather than migrated on the standing ground. The only reading available for
        // the field's absence is "no Block was performed", which is true of every payload a v23 build
        // could write - none of its Defense cards could Block - and false of the first payload this
        // milestone writes. It is also the one field whose absence cannot be recovered from anything
        // else on the wire: a Block reaches the comparison with no Defense Total, so a restored
        // resolution missing it would be indistinguishable from a malformed one.
        // Milestone 8.19a moves it to 25 for the pause altitudes the roster's printed text needs, and
        // for the one target fact those pauses make unreadable from the board. Four things a v24
        // payload cannot describe: an outer pre-action continuation - a frame standing outside an
        // action that has not started, holding the exact validated offer it will start from - the side
        // of a primary action the actor placed that frame's work on, the before-action triggers a turn
        // has already consumed, and the space an Attack's target stood on before rulebook p.14's
        // consequences removed it.
        //
        // v24 is rejected rather than migrated on the standing ground. Three of the four have no
        // reading available for their absence that is true of both builds: a v24 payload has no
        // pre-action frame because no content could produce one, so "absent" and "none" coincide there
        // - but the target space does not coincide, because every v24 Attack had one and simply did
        // not write it. Recovering it would mean reading a piece off the restored board, and the whole
        // reason the field exists is that p.14 may already have taken that piece off.
        // Milestone 8.19a1 moves it to 26 for one round-scoped rules fact v25 cannot state: which
        // typed p.13 hero-Defeat outcomes have completed in the current round. The diagnostic event
        // stream is not rules authority, while Team Life and the coin ledger are cumulative and cannot
        // distinguish this round from an earlier one. The sparse ordered list carries an opaque outcome
        // id plus the defeated HeroId and resets at the round boundary.
        //
        // v25 is rejected rather than migrated. Treating the absent section as empty would make a save
        // taken after a hero Defeat answer Brogan's "if any hero was defeated this round" condition as
        // false after restore, and neither the cumulative state nor an opaque resolution id can recover
        // the lost round scope without inventing authority.
        // Milestone 8.19c1 moves it to 27 for three stopped decisions v26 has nowhere to put. A trigger
        // running inside the effect that pushed needs two effect positions at once, and a
        // PendingResolution holds one; a Defeat waiting at its removal stage needs the recorded Defeat
        // and the coin operations that paid it, which cannot be re-derived once the ledger has moved;
        // and rulebook p.8's moment before the Minion Battle needs a round-scoped record that it has
        // already run, which the stage cursor cannot give because the cursor is unchanged either way.
        //
        // v26 is rejected rather than migrated. Every one of the three is a decision a player has been
        // asked and has not answered, and a payload that never held the question cannot be restored
        // into a match that is waiting for its answer: the trigger would resume as the effect that
        // pushed, the Defeat would remove a miniature its controller had been offered the chance to
        // keep, and the round would fire its pre-battle cards a second time.
        // Milestone 8.20 moves it to 28 for one runtime binding v27 has no field for: the hero an
        // Active-effect occurrence bound with the word "this". Arien's Expert and Master Duelist print
        // "immune to attack actions of all enemy heroes, except this attacker", and which hero that is
        // was decided at rulebook p.14's step 4 by an Attack that has ended by the time anything asks.
        // The definition declares where to look (ActiveEffectBoundActorSource); the occurrence is the
        // only thing that knows what it found.
        //
        // v27 is rejected rather than migrated, on the same ground as v25 before it: there is no
        // reading of the absent field that is true of both builds. Absent means "bound nobody", which
        // an occurrence of a definition that binds nobody genuinely is - but a v27 payload holding a
        // live Master Duelist would restore as immunity against the attacker as well, which is a
        // stronger card than the printed one and one no later state could detect.
        // Milestone 8.21 moves it to 29, and this one is not about a field at all. A payload pins the
        // registered capability contract set and a restore refuses a build whose set differs, because
        // the set is the identity a snapshot is read back against. Brogan's printed faces are the first
        // content to reach twelve contracts this build had registered nowhere - milestone 8.19b's five
        // roster ones, the three his modal Ranged Attacks needed minted, and his four Active effects - so
        // every v28 payload names a strictly shorter list than an 8.21 build registers and is refused
        // on sight.
        //
        // v28 is rejected rather than migrated for the reason the list exists: `RISK-COMPAT-001` is
        // about a payload being reinterpreted rather than refused, and "the build registers more than
        // this payload knew about" is exactly the case where a silent acceptance would let a restored
        // match resolve a card against contracts its own capture never had. The refusal is already
        // correct; the version is what tells a reader why.
        // Milestone 8.22 moves it to 30 for the same cause in the same place, and the repetition is
        // the point rather than an oversight. Sabina's printed faces are the first content to reach
        // five more contracts this build had registered nowhere - milestone 8.19b's `choose-target-set`,
        // `choose-target-pair`, `presence-count` and `pre-consequence-adjacency`, and the
        // `target-is-adjacent` her present-tense rider needed minted - so every v29 payload names a
        // strictly shorter list than an 8.22 build registers. Nothing about a field moved: the six
        // typed declarations this milestone also added are content on a policy, a role and three
        // parameters, and none of them is written down anywhere.
        //
        // v29 is rejected rather than migrated on exactly the ground v28 was. What makes the repeat
        // worth recording is that it is now the expected shape of a hero milestone: each of the five
        // remaining roster heroes is the first printed content to name some of milestone 8.19b's
        // vocabulary, so each of them lengthens this list and each of them costs a version. That is the
        // list doing its job rather than churn.
        // Milestone 8.23 moves it to 32, and this one is a field again: the completed basic action a
        // permanent after-action ability may still be asked to repeat. Tigerclaw's Cloak and Daggers -
        // "After you perform a basic action you may repeat it once" - opens its decision at p.8's
        // ActionEnded checkpoint, which is after the action it is about has finished. Every other
        // repeat in this build is asked from inside the action it repeats and therefore needs nothing
        // written down; this one stands outside it, and PerformedBasicAction is deliberately a fact
        // the checkpoint produces rather than state anything keeps, so the subject has to cross the
        // pause on the wire.
        //
        // v31 is rejected rather than migrated, and the reading that is missing is the reason. Absent
        // means "no ability is standing", which is true of every payload written before this
        // milestone - but a v31 payload captured while Cloak and Daggers held its question would
        // restore as a match with the question standing and nothing to answer it about, and confirming
        // would then do nothing at all. A `may` that can be confirmed to no effect is the one outcome
        // p.10's repeat must never produce.
        // Milestone 8.23 moves it to 33, its second field and the same milestone's second cause: where
        // the attacking hero stood when an Attack started. Tigerclaw's Combat Reflexes offers a move
        // at p.14's step 1 and asks at step 7 whether it was taken - "If you did not move before the
        // attack" - and the frame that offered it has completed and been discarded by then, with the
        // defender's whole response and every pause inside it in between. It is the space rather than
        // a flag because that is what the printed question is about.
        //
        // v32 is rejected rather than migrated for the reason v25 was: there is no reading of the
        // absent field true of both builds. Absent means "nothing recorded where he was", which is the
        // honest answer for every Attack whose card never asks - but a v32 payload holding a paused
        // Combat Reflexes would restore reporting that he had not moved whether or not he had, and the
        // page would then hand him a second move the card does not give.
        //
        // Milestone 8.35u moves it to 36: rulebook p.15 levels every hero at the same time, so a
        // paused system transition writes a repeated open-item group - item, chooser and options once
        // per hero still choosing - where v35 wrote exactly one of each. The single resume location
        // beneath it is unmoved, which is the point: several questions, still one record of where the
        // transition is.
        //
        // v35 is rejected rather than migrated, and for once the reason is not a missing reading but a
        // layout one: the count this version writes sits exactly where v35 wrote its item-present
        // boolean, so a v35 payload read as v36 does not fail to mean something - it means something
        // else, and would run off the end of the pause. There is no reading of the old bytes that is
        // true of both builds.
        // Milestone 8.35z21 moves it to 37: the card-action section now writes the two repeat instances
        // that have been decided and not yet started, with the target each may not take. Both used to
        // be settled and started inside one command and so could not be observed by a restore; 8.35z21
        // drains rulebook p.8's mid-turn Lane Push at the end of each action instance, and a push that
        // stops on p.8's sidebar Place holds a decided repeat across a player's answer. Because every
        // command captures a pre-image and a refused one is rolled back by deserializing it, an
        // unwritten decision there is a repeat that disappears at the next refusal.
        //
        // v36 is rejected rather than migrated, on v25's and v32's reasoning: the absent field has no
        // reading true of both builds. Absent would mean "no repeat is owed", which a v36 payload
        // taken at a paused mid-turn push cannot promise - it is exactly the state whose owed instance
        // the old build did not record - so a migrated payload would silently drop p.10's second
        // instance and finish an action the rules say is not over.
        // Milestone 8.35z27 moves it to 38: the card-action section now writes the modal
        // Attack a "choose one, or both" opened, part-way through its plan - the action that opened it,
        // the chosen bullets with the printed targeting sentence of each, whether the card forbids two
        // of them pointing at one unit, how many sequences have started, and the units the earlier ones
        // bound. It is written because nothing else can say it: the pending resolution knows an Attack
        // is standing at a target choice and knows neither which bullet's sentence produced that
        // choice nor which units are already spoken for, and the board cannot be asked because a target
        // that successfully defended is still standing.
        //
        // v37 is rejected rather than migrated, on the reasoning v25, v32 and v36 were: absent would
        // have to mean "no modal Attack is running", which a v37 payload taken between two sequences
        // cannot promise - it is exactly the state the old build could not record. A migrated payload
        // would resume a half-finished action as a finished one, dropping the sequence Ruling 5 says is
        // still owed and, with it, a whole p.14.
        //
        // Milestone 8.35z45 moves it to 40, and BOTH halves of that number are deliberate.
        //
        // WHY IT MOVES AT ALL. Each bullet of a persisted modal Attack plan now writes a fifth enum -
        // the predicate the bullet prints about its candidate, which is what Middlefinger of Death's
        // "a hero in range WHO HAS one or more cards in the discard" needs and what the multi-sequence
        // contract named as its one open extension point. The corpus does not move: no fixture in it
        // carries a modal Attack chain, so all sixteen payloads are byte-identical in LENGTH and the
        // only reason their digests move is this version field's own value. That measurement was taken
        // BEFORE the decision and it is deliberately not the decision: a v38 payload that DOES carry a
        // chain - which a shipped v38 build produces, because Whisper's Grim Reaper composes four
        // bullets today - has one fewer integer per bullet, so a v40 reader walking it would take the
        // next bullet's id string out of the middle of this bullet's policy. That is corruption, not a
        // short read, and it is precisely what the version guard exists to turn into a refusal.
        //
        // REJECT rather than migrate, on the reasoning every bump since v25 has used. A v38 payload's
        // missing field would have to be read as "this bullet prints no predicate about its candidate".
        // That reading is true of every v38 capture EXCEPT one taken mid-chain on a card that prints
        // one - which is exactly the payload a migration would have to be right about, and exactly the
        // payload this milestone creates. A migrated capture of Middlefinger's second sequence would
        // resume offering every enemy hero in range, including the ones whose empty discard the card
        // says are not targets, and the player would spend a Tier III card on a unit it never admitted.
        // Nor is the absence recoverable from the board: the pile is live state and says nothing about
        // which sentence the paused sequence was derived from.
        //
        // WHY 40 AND NOT 39. v39 is spoken for. Misa's deferred EffectInterruptKind candidate claimed
        // it, recorded in the multi-sequence worker's own hand-off ("this v38 lands first - Misa's
        // deferred EffectInterruptKind candidate takes v39") and restated at the Dodger dealing. That
        // claimant is an unrelated payload shape with its own migrate-or-reject decision still to make,
        // and two milestones landing "both v39" is the collision the queue exists to prevent. Taking
        // the next free number rather than the next number leaves the claim intact and costs nothing:
        // the guard is an equality test, so 39 being unused is not a hole a reader can fall into - a
        // payload claiming it is refused exactly as one claiming 38 is.
        // v43: milestone 8.53 puts the match scoreboard in the round section, immediately after the
        // round's leveling record - one count-prefixed hero id and six counters per participating
        // hero: kills, deaths, attacks at heroes, times defended, minions killed, coins earned.
        //
        // v42 IS REJECTED RATHER THAN MIGRATED, and the reason is the one this series keeps making
        // rather than a new one. A v42 payload records no scoreboard, and the only reading available
        // for its absence is "nothing has happened to anybody yet" - all six columns zero for every
        // hero. That reading is right for every payload a v42 BUILD could produce, because no v42
        // rule counts any of it; and that is precisely why it must not be written into the reader,
        // because the first payload it would be wrong about is the first one this milestone writes.
        // A match restored mid-play from a migrated v42 payload would come back with every hero at
        // zero kills and zero coins earned and then keep counting from there, and the victory screen
        // - the whole point of the number - would print a board that is quietly wrong in a way
        // nothing in the match could detect. It is the same argument v20 made for the Life counters
        // and v17 made for the Level, and it is stronger here than for either, because those two are
        // resources a later rule moves while this is a HISTORY that cannot be recovered from any
        // live state: the board says what happened, and nothing else in the payload remembers.
        //
        // The coins column is not derivable from the coin balances already in the payload either,
        // which is worth saying because the balances are right there: a balance is what a hero HOLDS
        // after every p.15 purchase has spent it back down, and the column is what they COLLECTED.
        // No persisted state exists outside the version-controlled corpus to strand.
        //
        // v44: milestone 8.71a puts Hanu the Trickster's rewritten printed Initiative values in the
        // card section - a SPARSE list immediately after the card rows, each entry a card instance id,
        // the number its printed icon now reads, and the card instance whose text wrote it. Hurry up!
        // prints "Set the printed [Initiative] Initiative value of an unresolved card of a hero in
        // range to 11, until it is resolved or otherwise changes state", and nothing in this build
        // before it replaced a printed value: the three sources the Initiative selection composes are
        // all additive and the two that are not the icon are keyed on the HERO, so there was no shape
        // here that could carry a fact about one CARD.
        //
        // It is a bump of exactly four bytes in every payload that carries no rewrite - one Int32
        // count reading zero - and thirty-odd more for each card that carries one. The sparse shape is
        // why: a flag on each card row would have spent a byte per card, in every payload ever
        // written, to say "no" about a fact true of at most one card at a time.
        //
        // v43 IS REJECTED RATHER THAN MIGRATED, on this series' standing argument. A v43 payload
        // records no rewrites, and "no rewrites" is the right reading for every payload a v43 BUILD
        // could produce - no v43 rule can write one - which is exactly why it must not be written into
        // the reader: the first payload it would be wrong about is the first one this milestone
        // writes. A match restored mid-round from a migrated v43 payload would come back with the
        // rewritten card reading its printed icon again, and the ROUND ORDER would silently differ
        // from the one the match was playing - p.7 re-checks the Initiative after every resolution, so
        // the wrong number is consulted at the very next selection and nothing in the match could
        // detect it. No persisted state exists outside the version-controlled corpus to strand.
        // v45: marker attachments carry an optional numbered dashboard turn slot. Snorri's four
        // distinct runes persist through round end and defeat, so their assignment is match-wide
        // rules state, not presentation placement. v44 lacks that association and is rejected.
        // v46: a turn-slot marker can count as active until its holder's next action.
        // v47 (milestone 8.114b1): the board carries this round's Minion-Battle exclusions (Wuk's
        // Dominance) and the last completed Movement action walk (Trample). v46 lacks both and is
        // rejected: a v46 payload taken between Dominance's choice and the battle would restore a
        // battle that counts the excluded minion.
        // v48 (milestone 8.118): a removal-prevention decision carries the further minion Defeats the
        // same text still owes behind it (Wuk's Angry Stampede naming two minions, the first guarded).
        // v47 lacks the list and is rejected: a v47 payload taken on that question would restore a
        // match that never defeats the second minion.
        // v49 (milestone 9.21a): a hero may stand on several spaces as one hero (Razzle, RZ-Q1-RZ-Q3).
        // Each hero occupancy row carries its body ordinal, written in (hero, body) order, and the board
        // carries the heroes that declare more than one body with their supply. v48 lacks both and is
        // rejected: a v48 reader would take the body ordinal for the coordinate, and a v48 payload
        // cannot say that a second occupancy of one hero is a second body rather than a duplicate.
        // v50 (milestone 9.21b): the acting body of a many-bodied hero's current action (RZ-Q4). Each
        // hero body supply row ends with the body settled to perform that hero's current action - his
        // pick of "which of you acts" or the one body standing - or 0 for none. v49 lacks it and is
        // rejected: a v49 reader would take the acting body for the next row's hero id, and a v49
        // payload restored at a standing action could not say which body performs it.
        // v51 (milestone 9.30b): the spellbook zone. A card row's location may name a hero's spellbook
        // (facedown) or the spells removed from it (faceup), and a hero's spell instances are part of
        // the persisted card list from setup. The row's shape is unchanged; the location domain grew.
        // v50 is rejected rather than migrated: a v50 reader meeting either new location would refuse
        // the card section mid-read as an unrecognized enum value, and the header refuses it first -
        // the same reasoning wire /29 recorded for a new log kind. No fixture without a spellbook
        // changes length.
        // v52 (milestone 9.30g): Gydion's Wish (GY-Q10). The round cursor ends, after the solo winner,
        // with the team a printed team victory was claimed for (the empty string for none) and every
        // team's count toward it (a count of rows, then team id, count and printed threshold). Neither
        // is derivable - a cast Wish lies faceup only until the next refill - and the claim is the
        // ending MatchVictoryCause.WishesCast reads. v51 is rejected: a v51 reader would take the claim
        // for the first turn slot's count, and a v51 payload cannot say how many Wishes a team has
        // cast. Every fixture grows by five authoritative bytes (an empty string and a zero count).
        // v53 (milestone 9.30f): each team Life row ends with the counters that team BEGAN the match
        // with. Gydion's Energy Drain regains a SPENT counter (GY-Q11, never above the starting Lives),
        // which is the difference between the two, and v52 recorded only the remaining count - so a
        // restored match could not tell a team that had spent a counter from a full one. v52 is
        // rejected rather than migrated: reading its rows as "began with what it holds" is right for
        // every v52 payload a match with no spent counter wrote and wrong for every one that had spent
        // one, which is precisely the first payload the regain would misread. Every fixture grows by
        // one int32 per team. The wire is unmoved: a seat reads the remaining count, which the wire
        // already carries, and the admission tuple's snapshot schema advances to 53 with it.
        // v54 (milestone 9.32a): every turn-slot assignment ends with whether its card was placed
        // facedown. Takahide's Bushido swaps a resolved or discarded gold card for a deck card that
        // "is facedown" (p.10), and a facedown card is hidden information (p.4) - but a card instance id
        // names its definition, and the slot is read by every seat and kept in the completed-round
        // history after the card has gone back to the hand, where its own state no longer says it was
        // ever facedown. The slot is the one place the fact can live. v53 is rejected: a v53 reader
        // would take the flag for the next assignment's hero id. Every slot-bearing fixture grows by
        // one byte per assignment.
        // v55 (milestone 9.46): board unit movement revisions and two turn-start checkpoints,
        // plus the round's resolved-card heroes, cannot be reconstructed from current occupancy or
        // card location. v54 is rejected rather than silently restoring false immobility/history.
        // v56 (milestone 9.48): private two-card commitments and the post-reveal retrieval choice
        // cannot be inferred from the remaining card alone; v55 is rejected.
        // v57 (milestone 9.49): a card's current location cannot reveal whether it was discarded
        // earlier this turn and then retrieved. Persist that history; reject v56.
        public const int CurrentSchemaVersion = 57;

        /// <summary>
        /// Instrumentation, not production state. Milestone 6.1i asserts the exact number of full
        /// state serializations an accepted and a rejected command perform, which finding `F613-08`
        /// requires to be measured rather than inspected. The project is single-threaded, so a plain
        /// counter is sufficient; a fixture resets it and reads it around a known command.
        /// </summary>
        public static long SerializeCallCount { get; private set; }

        /// <summary>Total encoded characters produced. See the envelope's counterpart.</summary>
        /// <remarks>
        /// Since milestone 7.9b this counts payload materializations rather than captures: a
        /// capture writes bytes and encodes nothing, and the accepted-command path takes exactly
        /// one capture and materializes no payload at all. <see cref="CapturedByteCount"/> is the
        /// counter that sees what a capture writes.
        /// </remarks>
        public static long SerializedCharacterCount { get; private set; }

        /// <summary>
        /// Total bytes a capture actually wrote: the two current-state spans, plus each completed
        /// round's fragment once, when it first freezes. This is the per-command serialization cost
        /// under milestone 7.9b's capture, so it is what the diagnostic growth fixture measures.
        /// </summary>
        public static long CapturedByteCount { get; private set; }

        public static void ResetSerializeCallCount()
        {
            SerializeCallCount = 0;
            SerializedCharacterCount = 0;
            CapturedByteCount = 0;
        }

        internal static void CountMaterializedPayload(int encodedCharacters)
        {
            SerializedCharacterCount += encodedCharacters;
        }

        internal static void CountCapturedBytes(int bytes)
        {
            CapturedByteCount += bytes;
        }

        public static string Serialize(
            RoundResolutionAuthority round,
            AdjacentMoveAuthority board)
        {
            return Capture(round, board, null).Payload;
        }

        /// <summary>
        /// One authoritative serialization as its parts: the current state written fresh, and the
        /// completed-round history shared from <paramref name="completedRoundsCache"/> rather than
        /// re-encoded. The capture's <see cref="AuthoritativeSnapshotCapture.Payload"/> is
        /// byte-identical to <see cref="Serialize"/>, which runs through this method itself, so
        /// there is exactly one write path for the format.
        /// </summary>
        /// <remarks>
        /// Milestone 7.9b. This is what makes the per-command rollback pre-image cost independent
        /// of match length: current state is bounded, so writing it is flat, while the history the
        /// payload must still carry grows every round and is written once per round instead of
        /// once per command. A null cache serves one capture at full cost.
        /// </remarks>
        /// <remarks>
        /// Milestone 8.3. The registration sections are written from the catalogs the round's own
        /// dispatcher was composed with, not from a pair of optional arguments that fell back to the
        /// synthetic set when a caller passed none - which is what the configured match did, since
        /// <c>MatchAggregate</c> never passed any. A snapshot now records what the match it came from
        /// actually ran on, and there is no argument through which a caller could make it record
        /// something else.
        /// </remarks>
        public static AuthoritativeSnapshotCapture Capture(
            RoundResolutionAuthority round,
            AdjacentMoveAuthority board,
            CompletedRoundsSerializationCache completedRoundsCache)
        {
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (board == null) throw new ArgumentNullException(nameof(board));

            // A round composed without a dispatcher states no content, and the registration sections
            // below are exactly what a restore rebinds against - so this refuses by name rather than
            // faulting on a null. Every round a match composes has one; the shape this catches is a
            // fixture composed for one narrow question and then asked for a whole snapshot.
            var cardActions = round.CardActions;
            if (cardActions == null)
            {
                throw new InvalidOperationException(
                    "A round with no card-action dispatcher names no capability or component " +
                    "catalogs, and a snapshot cannot record registrations the match does not have.");
            }

            var catalogs = cardActions.ContentCatalogs;

            SerializeCallCount++;
            var cache = completedRoundsCache ?? new CompletedRoundsSerializationCache();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(CurrentSchemaVersion);
                WriteDefinitions(writer, round.Definitions);
                WriteCapabilityRegistrations(writer, catalogs.Capabilities);
                WriteComponentRegistrations(writer, catalogs.Components);
                WriteParticipants(writer, round.Participants);
                WriteBoard(writer, board);
                WriteCards(writer, round.Instances);

                var state = round.CaptureRestoreState();
                WriteRoundOpening(writer, state);
                writer.Flush();
                var prefixLength = checked((int)stream.Position);
                WriteRoundRemainder(writer, round, state);
                writer.Flush();

                var buffer = stream.GetBuffer();
                var total = checked((int)stream.Length);
                var prefix = new byte[prefixLength];
                Array.Copy(buffer, prefix, prefixLength);
                var suffix = new byte[total - prefixLength];
                Array.Copy(buffer, prefixLength, suffix, 0, suffix.Length);
                CountCapturedBytes(total);

                return new AuthoritativeSnapshotCapture(
                    prefix,
                    cache.GetFragment(state.History.CompletedRounds),
                    suffix);
            }
        }

        public static RestoredAuthoritativeMatch Deserialize(
            MatchContentCatalogs catalogs,
            string payload,
            IEnumerable<CardDefinition> currentDefinitions,
            IEnumerable<BoardMapDefinition> currentMapDefinitions)
        {
            return Deserialize(
                catalogs,
                payload,
                currentDefinitions,
                currentMapDefinitions,
                Enumerable.Empty<QuickGameMinionSetupState>());
        }

        /// <param name="catalogs">
        /// The trusted capability and component content this build registers, which a restore rebinds
        /// the payload's registration sections against.
        ///
        /// Milestone 8.3 made it required and moved it to the front. It used to be two optional
        /// arguments defaulting to the synthetic catalogs, under a comment claiming "the configured
        /// session always supplies the real ones" - which was false in the most consequential case:
        /// <c>MatchAggregate</c>, the configured session's own aggregate, passed none, so every
        /// configured restore rebound against a default nobody had chosen. A required argument makes
        /// the content a restore trusts a decision the caller states.
        /// </param>
        public static RestoredAuthoritativeMatch Deserialize(
            MatchContentCatalogs catalogs,
            string payload,
            IEnumerable<CardDefinition> currentDefinitions,
            IEnumerable<BoardMapDefinition> currentMapDefinitions,
            IEnumerable<QuickGameMinionSetupState> minions,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog transitions = null)
        {
            if (catalogs == null) throw new ArgumentNullException(nameof(catalogs));
            var capabilities = catalogs.Capabilities;
            var components = catalogs.Components;
            // Milestone 6.11 widened this default from the synthetic transition alone to the set this
            // build actually supplies, because a real one joined it: a restore that rebound against the
            // synthetic catalog refused a paused Lane Push as a transition this build no longer has,
            // which is the right refusal for the wrong reason.
            transitions = transitions ??
                GuardsDigital.Core.Rules.Transitions.MatchSystemTransitions.CreateCatalog();
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.MalformedPayload,
                    "Snapshot payload must not be blank.");
            }

            if (payload.Length > MaximumBase64Characters)
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.MalformedPayload,
                    "Snapshot payload exceeds the supported size limit.");
            }

            try
            {
                var bytes = Convert.FromBase64String(payload);
                if (bytes.Length > MaximumDecodedBytes)
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.MalformedPayload,
                        "Snapshot payload exceeds the supported size limit.");
                }
                using (var stream = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(stream))
                {
                    if (!string.Equals(reader.ReadString(), Magic, StringComparison.Ordinal))
                    {
                        throw Malformed("Snapshot header is not recognized.");
                    }

                    var schemaVersion = reader.ReadInt32();
                    if (schemaVersion != CurrentSchemaVersion)
                    {
                        throw new SnapshotLoadException(
                            SnapshotLoadRejectionReason.UnsupportedSchemaVersion,
                            $"Snapshot schema version {schemaVersion} is unsupported; expected {CurrentSchemaVersion}.");
                    }

                    var mapCatalog = new BoardMapCatalog(
                        currentMapDefinitions ?? throw new ArgumentNullException(nameof(currentMapDefinitions)));
                    var definitions = ValidateDefinitions(reader, currentDefinitions);
                    ValidateCapabilityRegistrations(reader, capabilities);
                    ValidateComponentRegistrations(reader, components);
                    var participants = ReadParticipants(reader);
                    var board = ReadBoard(reader, mapCatalog, minions, components);
                    var cards = ReadCards(reader, definitions, participants);
                    var round = ReadRound(
                        reader, definitions, cards, participants, board, catalogs, components,
                        transitions);
                    if (stream.Position != stream.Length)
                    {
                        throw Malformed("Snapshot contains unexpected trailing data.");
                    }

                    return new RestoredAuthoritativeMatch(
                        round,
                        board,
                        participants,
                        Array.AsReadOnly(cards));
                }
            }
            catch (SnapshotLoadException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Milestone 8.35i names the cause in the MESSAGE rather than only chaining it.
                // Callers surface a refusal by its `Message` alone - `MatchSession`'s replay refusal
                // reads exactly that and nothing else - so an inner exception reachable only through
                // `InnerException` is a cause nobody ever reads. The corrective this belongs to spent
                // a day on a refusal whose whole text was that something, somewhere, was wrong; the
                // rejection reason and the refusal behaviour are unchanged.
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.MalformedPayload,
                    "Snapshot payload is malformed or internally inconsistent: " +
                    exception.GetType().Name + ": " + exception.Message,
                    exception);
            }
        }

        private static void WriteCommonResolution(BinaryWriter writer, ActionResolutionId resolutionId,
            CardInstanceId cardId, CardDefinitionReference definitionReference, ActionDefinitionId actionId,
            HeroId actorId, ActionInvocationContext context, ActionResolutionStepId stepId)
        {
            writer.Write(resolutionId.Value); writer.Write(cardId.Value); WriteDefinitionReference(writer, definitionReference);
            writer.Write(actionId.Value); writer.Write(actorId.Value); writer.Write((int)context); writer.Write(stepId.Value);
        }

        private static CommonResolution ReadCommonResolution(BinaryReader reader)
        {
            return new CommonResolution(
                new ActionResolutionId(reader.ReadString()), new CardInstanceId(reader.ReadString()),
                ReadDefinitionReference(reader), new ActionDefinitionId(reader.ReadString()),
                new HeroId(reader.ReadString()),
                SnapshotEnumReader.Read<ActionInvocationContext>(reader, "resolution invocation context"),
                new ActionResolutionStepId(reader.ReadString()));
        }

        private static void WritePrintedValue(BinaryWriter writer, ValueResult value)
        {
            writer.Write(value != null);
            if (value == null) return;
            writer.Write((int)value.Channel);
            writer.Write(value.Contributions.Count);
            foreach (var contribution in value.Contributions)
            {
                writer.Write((int)contribution.Kind);
                writer.Write(contribution.HasSourceMinionId
                    ? contribution.SourceMinionId.Value
                    : contribution.HasSourceActiveEffectId
                        ? contribution.SourceActiveEffectId.Value
                        : contribution.HasSourceContinuousSourceId
                            ? contribution.SourceContinuousSourceId
                            : contribution.SourceCardInstanceId.Value);
                writer.Write(contribution.HasSourceActionDefinitionId);
                if (contribution.HasSourceActionDefinitionId) writer.Write(contribution.SourceActionDefinitionId.Value);
                writer.Write((int)contribution.Channel); writer.Write(contribution.Amount);
            }
        }

        private static ValueResult ReadPrintedValue(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            var channel = SnapshotEnumReader.Read<PrintedValueChannel>(reader, "printed value channel");
            var contributions = ReadMany(reader, "printed contributions", () =>
            {
                var kind = SnapshotEnumReader.Read<ValueContributionKind>(reader, "value contribution kind");
                var card = new CardInstanceId(reader.ReadString());
                var hasAction = reader.ReadBoolean();
                var action = hasAction ? new ActionDefinitionId(reader.ReadString()) : default;
                var contributionChannel = SnapshotEnumReader.Read<PrintedValueChannel>(reader, "value contribution channel");
                var amount = reader.ReadInt32();
                if (kind == ValueContributionKind.MinionDefenseModifier)
                {
                    if (hasAction)
                    {
                        throw Malformed("A minion Defense contribution cannot reference a card action.");
                    }

                    return new ValueContribution(
                        kind,
                        new MinionId(card.Value),
                        contributionChannel,
                        amount);
                }
                if (kind == ValueContributionKind.ActiveEffectModifier)
                {
                    if (hasAction)
                    {
                        throw Malformed("An Active-effect contribution cannot reference a card action.");
                    }

                    return new ValueContribution(
                        kind,
                        new ActiveEffectId(card.Value),
                        contributionChannel,
                        amount);
                }
                if (kind == ValueContributionKind.ContinuousSourceModifier)
                {
                    if (hasAction)
                    {
                        throw Malformed("A continuous-source contribution cannot reference a card action.");
                    }

                    return ValueContribution.FromContinuousSource(
                        card.Value,
                        contributionChannel,
                        amount);
                }
                return hasAction
                    ? new ValueContribution(kind, card, action, contributionChannel, amount)
                    : new ValueContribution(kind, card, contributionChannel, amount);
            });
            return new ValueResult(channel, contributions);
        }

        private static void WriteActionOffer(BinaryWriter writer, ActionOffer offer)
        {
            writer.Write(offer.SourceCardInstanceId.Value); WriteDefinitionReference(writer, offer.SourceDefinitionReference);
            writer.Write(offer.ActionDefinitionId.Value); writer.Write(offer.ActorId.Value);
            writer.Write((int)offer.InvocationContext); writer.Write(offer.IsAvailable); writer.Write((int)offer.AttackRange);
            writer.Write(offer.SourceCardStateTransition.IsDefined);
            if (offer.SourceCardStateTransition.IsDefined)
            {
                writer.Write((int)offer.SourceCardStateTransition.RequiredLocation);
                writer.Write((int)offer.SourceCardStateTransition.DestinationLocation);
            }
        }

        private static ActionOffer ReadActionOffer(BinaryReader reader)
        {
            var card = new CardInstanceId(reader.ReadString()); var reference = ReadDefinitionReference(reader);
            var action = new ActionDefinitionId(reader.ReadString()); var actor = new HeroId(reader.ReadString());
            var context = SnapshotEnumReader.Read<ActionInvocationContext>(reader, "action offer invocation context");
            var available = reader.ReadBoolean();
            var range = SnapshotEnumReader.Read<AttackRangeKind>(reader, "action offer attack range");
            var hasTransition = reader.ReadBoolean();
            var transition = hasTransition
                ? new ActionCardStateTransition(
                    SnapshotEnumReader.Read<CardLocation>(reader, "transition required location"),
                    SnapshotEnumReader.Read<CardLocation>(reader, "transition destination location"))
                : default;
            return new ActionOffer(card, reference, action, actor, context, available, range, transition);
        }

        private static void WriteSlots(BinaryWriter writer, IReadOnlyList<RoundTurnSlot> slots)
        {
            writer.Write(slots.Count);
            foreach (var slot in slots)
            {
                writer.Write(slot.RoundIndex); writer.Write(slot.TurnIndex); writer.Write(slot.Assignments.Count);
                foreach (var assignment in slot.Assignments)
                {
                    writer.Write(assignment.HeroId.Value); writer.Write(assignment.CardInstanceId.HasValue);
                    if (assignment.CardInstanceId.HasValue) writer.Write(assignment.CardInstanceId.Value.Value);
                    writer.Write(assignment.PassedWithEmptyHand);
                    writer.Write(assignment.IsFacedown);
                }
            }
        }

        private static RoundTurnSlot[] ReadSlots(BinaryReader reader, IReadOnlyList<CardInstance> cards, MatchParticipants participants)
        {
            return ReadMany(reader, "round slots", () =>
            {
                var round = reader.ReadInt32(); var turn = reader.ReadInt32();
                var assignments = ReadMany(reader, "turn assignments", () =>
                {
                    var hero = new HeroId(reader.ReadString());
                    var card = reader.ReadBoolean() ? (CardInstanceId?)new CardInstanceId(reader.ReadString()) : null;
                    var passed = reader.ReadBoolean();
                    return new TurnCardAssignment(hero, card, passed, reader.ReadBoolean());
                });
                return new RoundTurnSlot(round, turn, assignments, cards, participants);
            });
        }

        private static void WriteOutcomes(BinaryWriter writer, IReadOnlyList<EndOfRoundStageOutcome> outcomes)
        {
            writer.Write(outcomes.Count);
            foreach (var outcome in outcomes) { writer.Write((int)outcome.Stage); writer.Write((int)outcome.Kind); }
        }

        private static EndOfRoundStageOutcome[] ReadOutcomes(BinaryReader reader)
        {
            return ReadMany(reader, "end-of-round outcomes", () => new EndOfRoundStageOutcome(
                SnapshotEnumReader.Read<EndOfRoundStage>(reader, "end-of-round outcome stage"),
                SnapshotEnumReader.Read<EndOfRoundStageOutcomeKind>(reader, "end-of-round outcome kind")));
        }

        private static void WriteDefinitionReference(BinaryWriter writer, CardDefinitionReference reference)
        { writer.Write(reference.DefinitionId.Value); writer.Write(reference.ContentRevision); }
        private static CardDefinitionReference ReadDefinitionReference(BinaryReader reader)
        { return new CardDefinitionReference(new CardDefinitionId(reader.ReadString()), reader.ReadString()); }
        private static void WriteCoordinate(BinaryWriter writer, HexCoordinate coordinate)
        { writer.Write(coordinate.Q); writer.Write(coordinate.R); }
        private static HexCoordinate ReadCoordinate(BinaryReader reader)
        { return new HexCoordinate(reader.ReadInt32(), reader.ReadInt32()); }
        private static void WriteCoordinates(BinaryWriter writer, IEnumerable<HexCoordinate> values)
        { var array = values.ToArray(); writer.Write(array.Length); foreach (var value in array) WriteCoordinate(writer, value); }
        private static HexCoordinate[] ReadCoordinates(BinaryReader reader, string name)
        { return ReadMany(reader, name, () => ReadCoordinate(reader)); }
        private static void WriteStrings(BinaryWriter writer, IEnumerable<string> values)
        { var array = values.ToArray(); writer.Write(array.Length); foreach (var value in array) writer.Write(value); }
        private static string[] ReadStrings(BinaryReader reader, string name)
        { return ReadMany(reader, name, reader.ReadString); }
        private static T[] ReadMany<T>(BinaryReader reader, string name, Func<T> read)
        { var count = ReadCount(reader, name); var values = new T[count]; for (var i = 0; i < count; i++) values[i] = read(); return values; }
        private static int ReadCount(BinaryReader reader, string name)
        { var count = reader.ReadInt32(); if (count < 0 || count > 100000) throw Malformed($"Invalid {name} count."); return count; }
        private static SnapshotLoadException Malformed(string message)
        { return new SnapshotLoadException(SnapshotLoadRejectionReason.MalformedPayload, message); }

        private readonly struct CommonResolution
        {
            public CommonResolution(ActionResolutionId resolutionId, CardInstanceId cardId,
                CardDefinitionReference definitionReference, ActionDefinitionId actionId, HeroId actorId,
                ActionInvocationContext invocationContext, ActionResolutionStepId stepId)
            { ResolutionId = resolutionId; CardId = cardId; DefinitionReference = definitionReference; ActionId = actionId; ActorId = actorId; InvocationContext = invocationContext; StepId = stepId; }
            public ActionResolutionId ResolutionId { get; }
            public CardInstanceId CardId { get; }
            public CardDefinitionReference DefinitionReference { get; }
            public ActionDefinitionId ActionId { get; }
            public HeroId ActorId { get; }
            public ActionInvocationContext InvocationContext { get; }
            public ActionResolutionStepId StepId { get; }
        }
    }
}
