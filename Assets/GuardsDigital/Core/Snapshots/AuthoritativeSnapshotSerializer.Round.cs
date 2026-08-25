using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Snapshots
{
    public static partial class AuthoritativeSnapshotSerializer
    {
        /// <summary>
        /// The round section up to where the completed-round history block begins: the cursor and
        /// the current round's slots. Milestone 7.9b splits the section here and after the block,
        /// so a capture can write the current state fresh while sharing the history's bytes; the
        /// stream contents are identical to the unsplit section by construction, because these are
        /// the same writes in the same order.
        /// </summary>
        private static void WriteRoundOpening(BinaryWriter writer, RoundResolutionRestoreState state)
        {
            var (roundIndex, turnIndex, phase, tieBreakerState) = state.Cursor;
            writer.Write(roundIndex);
            writer.Write(turnIndex);
            writer.Write((int)phase);
            writer.Write((int)tieBreakerState.RevealedSide);
            WriteSlots(writer, state.History.TurnSlots);
            WriteHeroDefeatHistory(writer, state.History.HeroDefeats);
        }

        /// <summary>
        /// One frozen round's bytes, exactly as the history block writes them after its count.
        /// Internal so the fragment cache serializes a newly frozen record through the one writer
        /// the format has.
        /// </summary>
        internal static void WriteCompletedRound(BinaryWriter writer, CompletedRoundRecord completed)
        {
            writer.Write(completed.RoundIndex);
            WriteSlots(writer, completed.TurnSlots);
            WriteOutcomes(writer, completed.EndOfRoundOutcomes);
        }

        /// <summary>The round section after the completed-round history block.</summary>
        private static void WriteRoundRemainder(
            BinaryWriter writer,
            RoundResolutionAuthority round,
            RoundResolutionRestoreState state)
        {
            var (_, _, endOfRound, roundCardActions, systemTransitions, heroProgression,
                heroItems, teamLives, roundLeveling) = state;
            var (currentStage, outcomes, pendingMinionBattleChoice, pendingLanePushRequest) = endOfRound;
            writer.Write(currentStage.HasValue);
            if (currentStage.HasValue) writer.Write((int)currentStage.Value);
            WriteOutcomes(writer, outcomes);

            // Milestone 8.19c1, schema v27. Whether this round has already run rulebook p.8's moment
            // before its Minion Battle. Nothing else records it - the stage cursor says the round is at
            // its Minion Battle either way - so a restore without it would fire every card that
            // declared the window a second time, on a battle it had already interrupted once.
            writer.Write(endOfRound.PreMinionBattleTriggersVisited);
            WritePendingMinionBattleChoice(writer, pendingMinionBattleChoice);
            WritePendingLanePushRequest(writer, pendingLanePushRequest);
            WriteTiming(writer, round.Timing.CaptureRestoreState());
            WriteMarkers(writer, round.Markers.CaptureRestoreState());
            WriteLanePush(writer, round.LanePush?.LanePush);
            WritePendingSystemTransitionStep(writer, systemTransitions);
            WriteHeroProgression(writer, heroProgression);
            WriteHeroItems(writer, heroItems);
            WriteTeamLives(writer, teamLives);
            WriteRoundLeveling(writer, roundLeveling);

            var (hasCardActions, roundCoinLedger) = roundCardActions;
            writer.Write(hasCardActions);
            WriteCoinLedger(writer, roundCoinLedger);

            writer.Write(round.CurrentCommitmentAuthority != null);
            if (round.CurrentCommitmentAuthority != null)
            {
                WriteCommitment(writer, round.CurrentCommitmentAuthority.CaptureRestoreState());
            }

            writer.Write(round.CurrentTurnAuthority != null);
            if (round.CurrentTurnAuthority != null) WriteTurn(writer, round.CurrentTurnAuthority);
        }

        private static RoundResolutionAuthority ReadRound(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            MatchContentCatalogs catalogs,
            ComponentDefinitionCatalog components,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog transitions)
        {
            var roundIndex = reader.ReadInt32();
            var turnIndex = reader.ReadInt32();
            var phase = SnapshotEnumReader.Read<RoundResolutionPhase>(reader, "round resolution phase");
            var tieBreaker = new TieBreakerState(
                SnapshotEnumReader.Read<TeamSide>(reader, "round tie-breaker revealed side"));
            var slots = ReadSlots(reader, cards, participants);
            var heroDefeats = ReadHeroDefeatHistory(reader);
            var completed = ReadMany(reader, "completed rounds", () =>
            {
                var completedIndex = reader.ReadInt32();
                return new CompletedRoundRecord(
                    completedIndex,
                    ReadSlots(reader, cards, participants),
                    ReadOutcomes(reader));
            });
            var stage = SnapshotEnumReader.ReadNullable<EndOfRoundStage>(reader, "current end-of-round stage");
            var outcomes = ReadOutcomes(reader);
            var preMinionBattleTriggersVisited = reader.ReadBoolean();
            if (preMinionBattleTriggersVisited && stage != EndOfRoundStage.MinionBattle &&
                (!stage.HasValue || (int)stage.Value < (int)EndOfRoundStage.MinionBattle))
            {
                throw Malformed(
                    "Snapshot records rulebook p.8's pre-Minion-Battle moment as visited in a round " +
                    "that has not reached its Minion Battle.");
            }

            var pendingMinionBattleChoice = ReadPendingMinionBattleChoice(reader);
            var pendingLanePushRequest = ReadPendingLanePushRequest(reader);
            var timing = ActiveEffectLifecycle.Restore(ReadTiming(reader, catalogs.Capabilities));
            var markers = MarkerAttachmentRegister.Restore(ReadMarkers(reader, components, participants));

            // The Lane Push is read before the continuation that may be paused inside it, because a
            // paused respawn is re-derived against this board and these Wave counters rather than
            // trusted from the payload.
            var lanePushAuthority = ReadLanePush(reader, board);

            // Milestone 7.5. The leveling rules are composed before the continuation is read, for the
            // reason the Lane Push above them is: a paused level-up step is re-derived against these
            // cards rather than trusted from the payload. What that re-derivation reads is the hands and
            // decks alone - a pause exists only while a purchased level is waiting to be spent, and
            // which card may be upgraded is a question about the cards - so it needs nothing this reader
            // has not read yet. The three states a purchase moves are bound by the round authority
            // below, which restores them from this same payload.
            var levelUpRules = new GuardsDigital.Core.Rules.Progression.HeroLevelUpStepRules(
                participants, definitions, cards);
            var transitionRules = lanePushAuthority == null
                ? GuardsDigital.Core.Rules.Transitions.DeclaredSystemTransitionStepRules.Instance
                : GuardsDigital.Core.Rules.Transitions.MatchSystemTransitions.CreateRules(
                    lanePushAuthority, levelUpRules);
            var systemTransitions = ReadSystemTransitions(reader, transitions, transitionRules);
            var heroProgression = ReadHeroProgression(reader);
            var heroItems = ReadHeroItems(reader);
            var teamLives = ReadTeamLives(reader);
            var roundLeveling = ReadRoundLeveling(reader);

            // Milestone 7.2. The paused Attack continuation below verifies a recorded Defense total by
            // re-deriving it, and p.16's item bonus is part of that total, so the items have to exist
            // before the continuation is read. This instance is exactly that: the one the verification
            // uses. The round authority restores its own from the same restore state a few lines later
            // and binds it to the dispatcher, so exactly one instance survives the restore and it is the
            // one the match owns.
            var verifyingHeroItems = GuardsDigital.Core.Rules.Progression.HeroItemState.Restore(
                participants, heroItems);
            var lanePush = lanePushAuthority == null
                ? null
                : new GuardsDigital.Core.Rules.LanePush.LanePushConsumption(
                    lanePushAuthority, systemTransitions);
            var hasRoundCardActions = reader.ReadBoolean();
            var roundCoinLedger = ReadCoinLedger(reader);
            var verifyingRosterState = roundCoinLedger.IsPresent
                ? EffectRosterState.Of(
                    slots,
                    RoundHeroDefeatHistory.Restore(participants, heroDefeats),
                    HeroCoinLedger.Restore(participants, roundCoinLedger.State))
                : EffectRosterState.Absent;

            TurnCardCommitmentAuthority commitment = null;
            if (reader.ReadBoolean())
            {
                commitment = TurnCardCommitmentAuthority.Restore(cards, participants, ReadCommitment(reader));
            }

            CardActionResolutionAuthority cardActions = null;
            TurnResolutionAuthority turn = null;
            if (reader.ReadBoolean())
            {
                // Milestone 8.16 passes the phase's own answer down: a turn stopped by p.8's mid-turn
                // ending has nothing left deciding who acts next, and only the phase read above says
                // that is legitimate rather than a truncated payload.
                turn = ReadTurn(reader, definitions, cards, participants, board, timing, markers,
                    roundIndex, turnIndex, catalogs, lanePush, verifyingHeroItems,
                    verifyingRosterState, phase == RoundResolutionPhase.GameEnded, out cardActions);
            }
            else if (hasRoundCardActions)
            {
                // No pending action means nothing here consumes the set today, but the site states
                // the honest live value rather than an implicit none, exactly as the pending-turn
                // path does; if this arm ever gains a consumer it is already correct.
                cardActions = CardActionResolutionAuthority.Restore(
                    catalogs,
                    board,
                    CardActionResolutionRestoreState.WithoutPendingAction(roundCoinLedger),
                    null,
                    null,
                    null,
                    definitions,
                    cards,
                    participants,
                    markers,
                    RuleModificationQuery.Build(
                        timing,
                        board,
                        participants,
                        tieBreaker,
                        GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(definitions, cards),
                        // Nothing is in flight at this restore point - the pending action is
                        // absent by construction - so no hero is performing an action for a
                        // conditional overlay to apply during.
                        GuardsDigital.Core.Rules.Modifications.RuleObject.None,
                        // Milestone 8.9: the same items the cross-check below reads, because an
                        // Active effect's declared Radius icon is raised by them exactly as a
                        // performed card's is.
                        verifyingHeroItems,
                        // Milestone 8.23: and from the same markers, so a value a held Poison
                        // marker lowered is the same value after a restore as before it.
                        markers),
                    verifyingHeroItems,
                    rosterState: verifyingRosterState);
            }

            // Milestone 7.7. The match's ending is derived from the two authorities that hold its facts
            // rather than recorded a fourth time, so this is the check that single record costs - the
            // same bargain milestone 7.3 struck for the unlocked Ultimate. A payload claiming two
            // endings, two eliminated teams, an ending whose sides this match does not have, or a phase
            // that disagrees with both paths is refused here, because none of those states is one a live
            // match could have reached: the phase stops every later command the moment an ending is
            // recorded, so the second ending would have had nothing left to end.
            var outcomeReading = GuardsDigital.Core.Rules.Victory.MatchOutcomeQuery.Read(
                participants,
                lanePushAuthority,
                GuardsDigital.Core.Rules.Consequences.TeamLifeState.Restore(participants, teamLives),
                phase == RoundResolutionPhase.GameEnded);
            if (!outcomeReading.IsCoherent)
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.IncoherentGameOutcome,
                    "Snapshot does not describe one ending for this match " +
                    $"({outcomeReading.Fault}).");
            }

            return RoundResolutionAuthority.Restore(
                definitions,
                cards,
                participants,
                new RoundResolutionRestoreState(
                    new RoundCursorRestoreState(roundIndex, turnIndex, phase, tieBreaker),
                    new RoundHistoryRestoreState(slots, heroDefeats, completed),
                    new EndOfRoundRestoreState(
                        stage,
                        outcomes,
                        pendingMinionBattleChoice,
                        pendingLanePushRequest,
                        preMinionBattleTriggersVisited),
                    new RoundCardActionsRestoreState(hasRoundCardActions, roundCoinLedger),
                    systemTransitions.CaptureRestoreState(),
                    heroProgression,
                    heroItems,
                    teamLives,
                    roundLeveling),
                commitment,
                turn,
                cardActions,
                timing,
                markers,
                board,
                systemTransitions,
                lanePush,
                lanePushAuthority == null ? null : levelUpRules);
        }

        /// <summary>
        /// Milestone 7.1. Every hero's current Level, ordered by hero id.
        ///
        /// It is written even though no rule in this build can change it, and that is the point rather
        /// than an oversight: p.15 gives every hero a Level from the first sentence, milestone 7.5 is
        /// the milestone that moves one, and Phase 7's durability rule requires the milestone that adds
        /// the state to close its persistence boundary rather than leaving a later integration gate to
        /// discover the field was never on the wire. The reader refuses a payload whose Levels do not
        /// cover exactly the participating heroes, so this can never silently restore as a default.
        ///
        /// Nothing here is derived. The hero id is written rather than an index into the participant
        /// list, for the same reason the coin balances beside it are: an ordering the payload does not
        /// state is an ordering a later build can change.
        /// </summary>
        private static void WriteHeroProgression(
            BinaryWriter writer,
            GuardsDigital.Core.Rules.Progression.HeroProgressionRestoreState state)
        {
            state.Deconstruct(out var levels);
            writer.Write(levels.Count);
            foreach (var entry in levels)
            {
                writer.Write(entry.Key.Value);
                writer.Write(entry.Value.Value);
            }
        }

        private static GuardsDigital.Core.Rules.Progression.HeroProgressionRestoreState ReadHeroProgression(
            BinaryReader reader) =>
            new GuardsDigital.Core.Rules.Progression.HeroProgressionRestoreState(
                ReadMany(reader, "hero Levels", () =>
                {
                    var heroId = new HeroId(reader.ReadString());
                    var level = reader.ReadInt32();
                    if (level < GuardsDigital.Core.Rules.Progression.HeroLevel.Minimum ||
                        level > GuardsDigital.Core.Rules.Progression.HeroLevel.Maximum)
                    {
                        throw Malformed($"Hero Level {level} is outside the legal range.");
                    }

                    return new KeyValuePair<HeroId, GuardsDigital.Core.Rules.Progression.HeroLevel>(
                        heroId, new GuardsDigital.Core.Rules.Progression.HeroLevel(level));
                }));

        /// <summary>
        /// Milestone 7.2. Every item tucked under a hero dashboard, in the state's own canonical order.
        ///
        /// It is one flat list rather than a per-hero grouping, and that is the honest shape of what p.16
        /// creates: a hero with no items is the ordinary case, so a per-hero section would write a count
        /// of zero for every hero in the match to say nothing at all. The Level section above does group
        /// per hero, because there the coverage is the point - every hero has a Level and a payload
        /// missing one is malformed - while here the coverage rule is the opposite, and what the reader
        /// enforces instead is that every item names a participant, that no card became two items, and
        /// that each hero's orders are exactly 1..n. <c>HeroItemState.Restore</c> owns all three.
        ///
        /// Every field is written rather than derived. The slot in particular is on the wire even though
        /// this build could re-derive it from the source card's definition, because p.16 attaches a
        /// consequence to which slot an item was actually placed under, and a re-derived slot is one that
        /// can never disagree with the card - which would make the payload unable to describe the case
        /// the page penalizes.
        ///
        /// Milestone 7.2a splits that one field into two, the slot position and the icon the item
        /// supplies, because the fourth slot accepts three icons and neither field determines the other
        /// there. The reader checks the pair against the owner's own dashboard rather than accepting it:
        /// <c>HeroItemState.Restore</c> refuses an item whose slot does not accept its icon.
        /// </summary>
        private static void WriteHeroItems(
            BinaryWriter writer,
            GuardsDigital.Core.Rules.Progression.HeroItemRestoreState state)
        {
            state.Deconstruct(out var items);
            writer.Write(items.Count);
            foreach (var item in items)
            {
                writer.Write(item.OwnerId.Value);
                writer.Write(item.SourceCardInstanceId.Value);
                writer.Write(item.Slot.Index);
                writer.Write((int)item.Icon);
                writer.Write(item.Order);
            }
        }

        private static GuardsDigital.Core.Rules.Progression.HeroItemRestoreState ReadHeroItems(
            BinaryReader reader) =>
            new GuardsDigital.Core.Rules.Progression.HeroItemRestoreState(
                ReadMany(reader, "hero items", () =>
                {
                    var ownerId = new HeroId(reader.ReadString());
                    var sourceCardInstanceId = new CardInstanceId(reader.ReadString());
                    var slot = reader.ReadInt32();
                    var icon = SnapshotEnumReader.Read<PrintedValueChannel>(reader, "item icon");
                    var order = reader.ReadInt32();
                    if (slot < 1 || slot > HeroDashboard.SlotCount)
                    {
                        throw Malformed($"Item slot {slot} is outside the legal range.");
                    }

                    if (icon == PrintedValueChannel.None)
                    {
                        throw Malformed("An item supplies one of rulebook p.16's six icons.");
                    }

                    if (order < 1)
                    {
                        throw Malformed($"Item order {order} is outside the legal range.");
                    }

                    return new GuardsDigital.Core.Rules.Progression.HeroItem(
                        ownerId,
                        sourceCardInstanceId,
                        new HeroDashboardSlotPosition(slot),
                        icon,
                        order);
                }));

        /// <summary>
        /// Milestone 7.4. Every team's remaining Life counters, ordered by team id.
        ///
        /// It rides beside the Levels rather than beside the setup's team states for the reason those
        /// Levels do: this is the count as the match has it now, and p.13 changes it - "Defeated hero's
        /// team spends the number of Life counters" - while the setup's <c>LifeCount</c> is the count
        /// the match began with and never moves. A payload that recorded only the starting number would
        /// restore a team the counters it had already spent.
        ///
        /// Zero is a legal recorded value and the one p.13's last sentence produces, so the reader
        /// refuses a negative rather than a zero; <c>TeamLifeState.Restore</c> owns the coverage rule
        /// that every participating team appears exactly once.
        /// </summary>
        private static void WriteTeamLives(
            BinaryWriter writer,
            GuardsDigital.Core.Rules.Consequences.TeamLifeRestoreState state)
        {
            writer.Write(state.Counters.Count);
            foreach (var entry in state.Counters)
            {
                writer.Write(entry.Key.Value);
                writer.Write(entry.Value);
            }
        }

        private static GuardsDigital.Core.Rules.Consequences.TeamLifeRestoreState ReadTeamLives(
            BinaryReader reader) =>
            new GuardsDigital.Core.Rules.Consequences.TeamLifeRestoreState(
                ReadMany(reader, "team Life counters", () =>
                {
                    var teamId = new TeamId(reader.ReadString());
                    var counters = reader.ReadInt32();
                    if (counters < 0)
                    {
                        throw Malformed($"Team Life counter total {counters} is outside the legal range.");
                    }

                    return new KeyValuePair<TeamId, int>(teamId, counters);
                }));

        /// <summary>
        /// Milestone 7.6. Which heroes bought a level in the round the payload was taken in, which is
        /// what rulebook p.8's Pity Coin step reads the negation of.
        ///
        /// A sparse list of names rather than a flag per hero, in the shape the items above it use and
        /// for the same reason: for most of a match nobody has levelled, so a per-hero section would
        /// write an entry for every hero to say nothing at all. An empty list is therefore both the
        /// common case and the one a fresh round starts from, and it is written rather than omitted,
        /// because a missing section would be a round whose answer nobody wrote rather than a round in
        /// which nobody levelled. <c>RoundLevelingRecord.Restore</c> owns the two rules the reader does
        /// not: that every name is a participant, and that no hero appears twice.
        /// </summary>
        private static void WriteRoundLeveling(
            BinaryWriter writer,
            GuardsDigital.Core.Rules.Progression.RoundLevelingRestoreState state)
        {
            state.Deconstruct(out var leveledThisRound);
            writer.Write(leveledThisRound.Count);
            foreach (var heroId in leveledThisRound)
            {
                writer.Write(heroId.Value);
            }
        }

        private static GuardsDigital.Core.Rules.Progression.RoundLevelingRestoreState ReadRoundLeveling(
            BinaryReader reader) =>
            new GuardsDigital.Core.Rules.Progression.RoundLevelingRestoreState(
                ReadMany(reader, "heroes that levelled this round", () => new HeroId(reader.ReadString())));

        /// <summary>
        /// Milestone 8.19a1. The p.13 hero-Defeat outcomes that have completed in the current round.
        ///
        /// The target is a typed hero id rather than a parsed suffix of the outcome id. Both are
        /// written: the outcome makes repeated entries incoherent and the hero answers the rule without
        /// consulting cumulative coin or Life state. The owning round supplies the scope, so no round
        /// index is duplicated per entry.
        /// </summary>
        private static void WriteHeroDefeatHistory(
            BinaryWriter writer,
            RoundHeroDefeatHistoryRestoreState state)
        {
            state.Deconstruct(out var entries);
            writer.Write(entries.Count);
            foreach (var entry in entries)
            {
                writer.Write(entry.OutcomeId.Value);
                writer.Write(entry.DefeatedHeroId.Value);
            }
        }

        private static RoundHeroDefeatHistoryRestoreState ReadHeroDefeatHistory(BinaryReader reader) =>
            new RoundHeroDefeatHistoryRestoreState(
                ReadMany(reader, "hero Defeats this round", () =>
                    new HeroDefeatHistoryEntry(
                        new DefeatOutcomeId(reader.ReadString()),
                        new HeroId(reader.ReadString()))));

        /// <summary>
        /// Milestone 6.11. Rulebook p.8's shared Wave counters, the victory sentence that ended the game
        /// if either has, and the push now running.
        ///
        /// Milestone 6.10 wrote none of this and said why: nothing a live match could reach read a Wave
        /// counter, so a persisted one would have been a wire field no restore could disagree about.
        /// The transition is reachable now, so a restore that defaulted the pool would hand a match back
        /// counters it had already spent, and one that forgot the running push's sides would resume a
        /// transition that no longer knew who had won it.
        /// </summary>
        private static void WriteLanePush(
            BinaryWriter writer, GuardsDigital.Core.Rules.LanePush.LanePushTransitionAuthority lanePush)
        {
            writer.Write(lanePush != null);
            if (lanePush == null) return;

            var state = lanePush.CaptureRestoreState();
            state.Deconstruct(
                out var remainingWaveCounters, out var gameEndingCause, out var winningSide,
                out var running);
            writer.Write(remainingWaveCounters);
            writer.Write((int)gameEndingCause);
            writer.Write((int)winningSide);
            writer.Write(running != null);
            if (running == null) return;

            writer.Write((int)running.WinningSide);
            writer.Write((int)running.LosingSide);
            writer.Write((int)running.TieBreakerSide);
            writer.Write(running.PreviousBattleZoneId.HasValue);
            if (running.PreviousBattleZoneId.HasValue)
            {
                writer.Write(running.PreviousBattleZoneId.Value.Value);
            }

            writer.Write(running.Outcomes.Count);
            foreach (var outcome in running.Outcomes)
            {
                writer.Write((int)outcome.Stage);
                writer.Write((int)outcome.Kind);
                writer.Write((int)outcome.GameEndingCause);
            }
        }

        private static GuardsDigital.Core.Rules.LanePush.LanePushTransitionAuthority ReadLanePush(
            BinaryReader reader, AdjacentMoveAuthority board)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var remainingWaveCounters = reader.ReadInt32();
            var gameEndingCause = SnapshotEnumReader.Read<
                GuardsDigital.Core.Rules.LanePush.LanePushGameEndingCause>(
                reader, "Lane-Push game-ending cause");
            var winningSide = SnapshotEnumReader.Read<TeamSide>(reader, "Lane-Push winning side");
            GuardsDigital.Core.Rules.LanePush.RunningLanePush running = null;
            if (reader.ReadBoolean())
            {
                var runningWinner = SnapshotEnumReader.Read<TeamSide>(reader, "running push winner");
                var runningLoser = SnapshotEnumReader.Read<TeamSide>(reader, "running push loser");
                var tieBreakerSide = SnapshotEnumReader.Read<TeamSide>(reader, "running push tie-breaker");
                ZoneId? previousBattleZoneId = reader.ReadBoolean()
                    ? new ZoneId(reader.ReadString())
                    : (ZoneId?)null;
                var outcomes = ReadMany(reader, "Lane-Push stage outcomes", () =>
                    new GuardsDigital.Core.Rules.LanePush.LanePushStageOutcome(
                        SnapshotEnumReader.Read<
                            GuardsDigital.Core.Rules.LanePush.LanePushTransitionStage>(
                            reader, "Lane-Push stage"),
                        SnapshotEnumReader.Read<
                            GuardsDigital.Core.Rules.LanePush.LanePushStageOutcomeKind>(
                            reader, "Lane-Push stage outcome"),
                        SnapshotEnumReader.Read<
                            GuardsDigital.Core.Rules.LanePush.LanePushGameEndingCause>(
                            reader, "Lane-Push stage victory cause")));
                running = new GuardsDigital.Core.Rules.LanePush.RunningLanePush(
                    runningWinner, runningLoser, tieBreakerSide, previousBattleZoneId, outcomes);
            }

            return GuardsDigital.Core.Rules.LanePush.LanePushTransitionAuthority.Restore(
                new GuardsDigital.Core.Rules.LanePush.LanePushRestoreState(
                    remainingWaveCounters, gameEndingCause, winningSide, running),
                board);
        }

        /// <summary>
        /// Milestone 6.10a. A paused transition writes identity and recorded facts only - which
        /// transition, which step, which item of it, who chooses, what they may choose, and which steps
        /// and items have already happened. What those steps *are* stays in the catalog, for the reason
        /// the Active-effect section states at v8 and the marker section repeats: a payload may say which
        /// content this is and what has happened to it, never what that content means.
        ///
        /// The single leading boolean is the whole cost for a match with no transition running.
        /// </summary>
        private static void WritePendingSystemTransitionStep(
            BinaryWriter writer,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState state)
        {
            state.Deconstruct(out var pending);
            writer.Write(pending != null);
            if (pending == null) return;

            writer.Write(pending.InstanceId.Value);
            writer.Write(pending.DefinitionId.Value);
            writer.Write(pending.StepId.Value);

            // Milestone 6.11's item, written as a present/absent pair rather than a blank string,
            // because "this pause is about no item" and "this pause is about an item whose name is
            // empty" are different claims and only the first is legal.
            writer.Write(pending.ItemId.IsDefined);
            if (pending.ItemId.IsDefined)
            {
                writer.Write(pending.ItemId.Value);
            }

            writer.Write((int)pending.ChoosingSide);
            WriteStrings(writer, pending.Options.Select(option => option.Value));
            WriteStrings(writer, pending.CompletedStepIds.Select(step => step.Value));
            WriteStrings(writer, pending.CompletedItemIds.Select(item => item.Value));
        }

        private static GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority ReadSystemTransitions(
            BinaryReader reader,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog transitions,
            GuardsDigital.Core.Rules.Transitions.ISystemTransitionStepRules transitionRules)
        {
            GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep pending = null;
            if (reader.ReadBoolean())
            {
                var instanceId =
                    new GuardsDigital.Core.Rules.Transitions.SystemTransitionInstanceId(reader.ReadString());
                var definitionId =
                    new GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId(reader.ReadString());
                var stepId =
                    new GuardsDigital.Core.Rules.Transitions.SystemTransitionStepId(reader.ReadString());
                var itemId = reader.ReadBoolean()
                    ? new GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId(reader.ReadString())
                    : GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId.None;
                pending = new GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep(
                    instanceId,
                    definitionId,
                    stepId,
                    itemId,
                    SnapshotEnumReader.Read<TeamSide>(reader, "transition choosing side"),
                    ReadStrings(reader, "transition options")
                        .Select(value => new GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionId(value)),
                    ReadStrings(reader, "transition completed steps")
                        .Select(value => new GuardsDigital.Core.Rules.Transitions.SystemTransitionStepId(value)),
                    ReadStrings(reader, "transition completed items")
                        .Select(value => new GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId(value)));
            }

            // The authority's own restore is what refuses a payload the trusted definitions do not
            // reproduce, so the reader states no second opinion about what a legal continuation is.
            // The rules travel with it because milestone 6.11's derived steps have no catalog options
            // to be compared with, and re-deriving them over the restored board is what takes their
            // place.
            return GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority.Restore(
                new GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState(pending),
                transitions,
                transitionRules);
        }

        /// <summary>
        /// Milestone 6.2. An attached marker writes identity, provenance, and who holds it. Its supply
        /// size and return condition are absent from the wire and rebound from the component catalog,
        /// for the reason the Active-effect section states at v8: a payload may not say what a piece of
        /// content means. That is what makes the marker's return state trusted rather than asserted.
        /// </summary>
        private static void WriteMarkers(BinaryWriter writer, MarkerAttachmentRestoreState state)
        {
            // Explicit Deconstruct rather than tuple syntax: C# has no one-element deconstruction, and
            // the point of destructuring here is the arity break when a persisted field is added.
            state.Deconstruct(out var attachments);
            writer.Write(attachments.Count);
            foreach (var attachment in attachments.OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                writer.Write(attachment.Id.Value);
                writer.Write(attachment.Definition.Id.Value);
                writer.Write(attachment.Definition.Revision);
                writer.Write(attachment.OwnerHeroId.Value ?? string.Empty);
                writer.Write(attachment.HolderHeroId.Value);
                writer.Write(attachment.SourceCardInstanceId.Value ?? string.Empty);
                writer.Write(attachment.SourceActionDefinitionId.Value ?? string.Empty);
            }
        }

        private static MarkerAttachmentRestoreState ReadMarkers(
            BinaryReader reader,
            ComponentDefinitionCatalog components,
            MatchParticipants participants)
        {
            return new MarkerAttachmentRestoreState(ReadMany(reader, "marker attachments", () =>
            {
                var id = new MarkerInstanceId(reader.ReadString());
                var reference = new MarkerDefinitionReference(
                    new MarkerDefinitionId(reader.ReadString()), reader.ReadInt32());
                var owner = ReadOptionalHeroId(reader);
                var holder = new HeroId(reader.ReadString());
                // A marker is held by a hero, so a payload naming one this match does not have is
                // refused here rather than producing an attachment nothing can ever return.
                participants.GetHero(holder);
                var sourceCard = ReadOptionalCardInstanceId(reader);
                var sourceActionValue = reader.ReadString();
                return new MarkerAttachment(
                    id,
                    LoadTrustedMarkerDefinition(components, reference),
                    owner,
                    holder,
                    sourceCard,
                    string.IsNullOrEmpty(sourceActionValue) ? default : new ActionDefinitionId(sourceActionValue));
            }));
        }

        private static MarkerDefinition LoadTrustedMarkerDefinition(
            ComponentDefinitionCatalog components,
            MarkerDefinitionReference reference)
        {
            try
            {
                return components.LoadMarker(reference);
            }
            catch (ComponentDefinitionLoadException exception)
            {
                throw new SnapshotLoadException(
                    exception.Reason == ComponentDefinitionLoadRejectionReason.MissingDefinition
                        ? SnapshotLoadRejectionReason.ComponentDefinitionMissing
                        : SnapshotLoadRejectionReason.ComponentDefinitionRevisionMismatch,
                    exception.Message,
                    exception);
            }
        }

        private static void WritePendingMinionBattleChoice(
            BinaryWriter writer,
            PendingMinionBattleChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write(choice.ResolutionId.Value);
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.LosingTeamId.Value);
            writer.Write((int)choice.ChoosingSide);
            writer.Write(choice.BattleZoneId.Value);
            writer.Write(choice.RequiredRemovalCount);
            writer.Write(choice.Candidates.Count);
            foreach (var candidate in choice.Candidates)
            {
                writer.Write((int)candidate.Unit.Kind);
                writer.Write(candidate.Unit.Kind == RuleObjectKind.Hero
                    ? candidate.HeroId.Value
                    : candidate.MinionId.Value);
                writer.Write((int)candidate.MinionType);
            }

            writer.Write(choice.SelectedUnits.Count);
            foreach (var unit in choice.SelectedUnits)
            {
                writer.Write((int)unit.Kind);
                writer.Write(unit.Kind == RuleObjectKind.Hero ? unit.HeroId.Value : unit.MinionId.Value);
            }
        }

        private static PendingMinionBattleChoice ReadPendingMinionBattleChoice(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var resolutionId = new EndOfRoundResolutionId(reader.ReadString());
            var choiceId = new EndOfRoundChoiceId(reader.ReadString());
            var losingTeamId = new TeamId(reader.ReadString());
            var choosingSide = SnapshotEnumReader.Read<TeamSide>(reader, "Minion Battle choosing side");
            var battleZoneId = new ZoneId(reader.ReadString());
            var requiredRemovalCount = reader.ReadInt32();
            var candidates = ReadMany(reader, "Minion Battle candidates", () =>
            {
                var kind = SnapshotEnumReader.Read<RuleObjectKind>(reader, "Minion Battle candidate kind");
                var identity = reader.ReadString();
                var type = SnapshotEnumReader.Read<StandardMinionType>(
                    reader, "Minion Battle candidate type");
                return kind == RuleObjectKind.Hero
                    ? new MinionBattleCandidate(new HeroId(identity))
                    : new MinionBattleCandidate(new MinionId(identity), type);
            });
            var selected = ReadMany(reader, "selected Minion Battle units", () =>
            {
                var kind = SnapshotEnumReader.Read<RuleObjectKind>(reader, "selected Minion Battle unit kind");
                var identity = reader.ReadString();
                return kind == RuleObjectKind.Hero
                    ? RuleObject.ForHero(new HeroId(identity))
                    : RuleObject.ForMinion(new MinionId(identity));
            });
            return new PendingMinionBattleChoice(
                resolutionId,
                choiceId,
                losingTeamId,
                choosingSide,
                battleZoneId,
                requiredRemovalCount,
                candidates,
                selected);
        }

        private static void WritePendingLanePushRequest(
            BinaryWriter writer,
            PendingLanePushRequest request)
        {
            writer.Write(request != null);
            if (request == null)
            {
                return;
            }

            writer.Write(request.RequestId.Value);
            writer.Write(request.RoundIndex);
            writer.Write(request.BattleZoneId.Value);
            writer.Write(request.WinningTeamId.Value);
            writer.Write((int)request.WinningSide);
            writer.Write(request.LosingTeamId.Value);
            writer.Write((int)request.LosingSide);
            writer.Write(request.CauseResolutionId.Value);
        }

        private static PendingLanePushRequest ReadPendingLanePushRequest(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            return new PendingLanePushRequest(
                new LanePushRequestId(reader.ReadString()),
                reader.ReadInt32(),
                new ZoneId(reader.ReadString()),
                new TeamId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "Lane Push winning side"),
                new TeamId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "Lane Push losing side"),
                new EndOfRoundResolutionId(reader.ReadString()));
        }

        private static void WriteCommitment(BinaryWriter writer, TurnCardCommitmentRestoreState state)
        {
            var (commitments, passes, isRevealed) = state;
            writer.Write(isRevealed);
            writer.Write(commitments.Count);
            foreach (var pair in commitments)
            {
                writer.Write(pair.Key.Value);
                writer.Write(pair.Value.Value);
            }
            writer.Write(passes.Count);
            foreach (var hero in passes)
            {
                writer.Write(hero.Value);
            }
        }

        private static TurnCardCommitmentRestoreState ReadCommitment(BinaryReader reader)
        {
            var revealed = reader.ReadBoolean();
            var commitments = ReadMany(reader, "commitments", () => new KeyValuePair<HeroId, CardInstanceId>(
                new HeroId(reader.ReadString()),
                new CardInstanceId(reader.ReadString())));
            var passes = ReadMany(reader, "passes", () => new HeroId(reader.ReadString()));
            return new TurnCardCommitmentRestoreState(commitments, passes, revealed);
        }

    }
}
