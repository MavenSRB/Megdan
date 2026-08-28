using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Victory;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.9's own pseudo-random source, written out rather than taken from the framework.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Random"/> cannot be used here. Its algorithm is not part of its contract and
    /// is known to differ between the Mono runtime the Unity tiers execute on and the .NET runtime the
    /// cross-platform harness executes on, so a seed would name two different command sequences and a
    /// "fixed reported seed" would report nothing. This is xorshift32, which is four lines and is the
    /// same four lines everywhere.
    ///
    /// <see cref="Next"/> takes the remainder rather than rejecting the biased tail. That bias is real
    /// and deliberately accepted: the property this driver needs is that one seed always names one
    /// sequence, not that the sequence is uniform. Nothing here samples a distribution.
    /// </remarks>
    public sealed class MatchLoopRandom
    {
        private uint state;

        public MatchLoopRandom(int seed)
        {
            // xorshift32 is fixed at zero, so a zero seed would produce one constant forever.
            state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public int Next(int exclusiveUpperBound)
        {
            if (exclusiveUpperBound <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveUpperBound), exclusiveUpperBound, "An empty range has nothing to draw.");
            }

            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (int)(state % (uint)exclusiveUpperBound);
        }
    }

    /// <summary>What one seeded run of <see cref="DeterministicMatchLoop"/> did.</summary>
    /// <remarks>
    /// The commands are the whole of it. Everything else here is derived from them and from the
    /// projection they produced, and is carried so a fixture can state what a run reached without
    /// re-walking it - but a replay oracle compares <see cref="Commands"/>, because that is the record
    /// a fresh session can be driven from.
    /// </remarks>
    public sealed class MatchLoopRecord
    {
        public MatchLoopRecord(
            int seed,
            IEnumerable<DirectorCommand> commands,
            int roundsReached,
            MatchOutcome outcome,
            bool exhaustedItsBudget,
            bool stalled,
            IEnumerable<string> chosenEffectOptionNodeIds,
            IEnumerable<DirectorCommandKind> observedCommandKinds,
            IEnumerable<RoundResolutionEventKind> observedEventKinds,
            string finalSnapshot,
            long finalVersion)
        {
            Seed = seed;
            Commands = Array.AsReadOnly((commands ?? throw new ArgumentNullException(nameof(commands))).ToArray());
            RoundsReached = roundsReached;
            Outcome = outcome;
            ExhaustedItsBudget = exhaustedItsBudget;
            Stalled = stalled;
            ChosenEffectOptionNodeIds = Array.AsReadOnly((chosenEffectOptionNodeIds ??
                throw new ArgumentNullException(nameof(chosenEffectOptionNodeIds))).ToArray());
            ObservedCommandKinds = Array.AsReadOnly((observedCommandKinds ??
                throw new ArgumentNullException(nameof(observedCommandKinds))).ToArray());
            ObservedEventKinds = Array.AsReadOnly((observedEventKinds ??
                throw new ArgumentNullException(nameof(observedEventKinds))).ToArray());
            FinalSnapshot = finalSnapshot;
            FinalVersion = finalVersion;
        }

        public int Seed { get; }

        /// <summary>Every command this run accepted, in order. The replayable record.</summary>
        public IReadOnlyList<DirectorCommand> Commands { get; }

        public int RoundsReached { get; }
        public MatchOutcome Outcome { get; }

        /// <summary>True when the run stopped because it hit its command budget rather than an ending.</summary>
        public bool ExhaustedItsBudget { get; }

        /// <summary>
        /// True when the run stopped because the projection offered no legal command while the match
        /// was still live. A gate fixture asserts this is false: a live match a player cannot answer is
        /// exactly the defect a complete-loop driver exists to find.
        /// </summary>
        public bool Stalled { get; }

        /// <summary>
        /// Every effect-option node this run selected, sorted and deduplicated. This is how a fixture
        /// states which Skill branch of which configured card the loop actually reached - the node id
        /// is the branch's own identity, not a name a test invented for it.
        /// </summary>
        public IReadOnlyList<string> ChosenEffectOptionNodeIds { get; }

        public IReadOnlyList<DirectorCommandKind> ObservedCommandKinds { get; }
        public IReadOnlyList<RoundResolutionEventKind> ObservedEventKinds { get; }
        public string FinalSnapshot { get; }
        public long FinalVersion { get; }

        public bool Reached(string effectOptionNodeId) =>
            ChosenEffectOptionNodeIds.Any(value => string.Equals(value, effectOptionNodeId, StringComparison.Ordinal));

        public bool Observed(DirectorCommandKind kind) => ObservedCommandKinds.Any(value => value == kind);

        public bool Observed(RoundResolutionEventKind kind) => ObservedEventKinds.Any(value => value == kind);
    }

    /// <summary>
    /// Milestone 7.9. Plays a configured match by answering, one at a time, whichever decision the
    /// authoritative projection is actually waiting on, choosing among that decision's own projected
    /// options with a fixed seed.
    /// </summary>
    /// <remarks>
    /// This is the gate's driver, and the reason it is written this way is the reason the gate exists.
    /// Every earlier fixture reaches its subject by naming the exact command sequence that gets there,
    /// which proves the subject and proves nothing about what a match does when it is simply played.
    /// This one never names a command: <see cref="EnumerateLegalCommands"/> reads the pending choice
    /// off the projection and offers that choice's own invocations, so a run can only do what the
    /// rules were already offering. A run that stalls has found a decision nothing can answer, and a
    /// run whose command is refused has found an offer the rules do not honour; both fail here rather
    /// than being steered around.
    ///
    /// It derives no legality of its own. Every branch below either reads projected invocations
    /// (the effect choice, removal prevention, the Minion Battle, the transition step, the respawn,
    /// the Clear subsets) or
    /// asks the same query the presentation layer asks (<see cref="GeneralRuleActionOfferQuery"/> for
    /// the general Hold). The one ordering decision it makes is the order candidates are enumerated in,
    /// which must be stable for a seed to mean anything - and every list it enumerates is one the
    /// authority already ordered.
    ///
    /// Deselect invocations are deliberately excluded. They are legal, and a run that could draw them
    /// would spend its budget selecting and unselecting the same candidate without the match moving,
    /// so including them would trade coverage for churn. Everything else a decision offers is here,
    /// including declining, stopping, and confirming nothing.
    /// </remarks>
    public static class DeterministicMatchLoop
    {
        /// <summary>
        /// Plays until the match ends or the budget runs out, and reports which of the two happened.
        /// </summary>
        public static MatchLoopRecord Play(MatchSession session, int seed, int maximumCommands)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (maximumCommands <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCommands), maximumCommands, "A run needs a positive command budget.");
            }

            var random = new MatchLoopRandom(seed);
            var commands = new List<DirectorCommand>();
            var options = new SortedSet<string>(StringComparer.Ordinal);
            var commandKinds = new SortedSet<DirectorCommandKind>();
            var eventKinds = new SortedSet<RoundResolutionEventKind>();
            var stalled = false;

            while (session.Projection.Phase != RoundResolutionPhase.GameEnded)
            {
                if (commands.Count >= maximumCommands)
                {
                    return Record(seed, commands, session, options, commandKinds, eventKinds,
                        exhaustedItsBudget: true, stalled: false);
                }

                var candidates = EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    stalled = true;
                    break;
                }

                var chosen = candidates[random.Next(candidates.Count)];
                var result = session.Execute(chosen);
                Assert.That(result.Accepted, Is.True,
                    $"Seed {seed} command {commands.Count + 1} ({chosen.Kind}) was offered by the projection " +
                    $"and refused by the rules: {result.Detail}");

                commands.Add(chosen);
                commandKinds.Add(chosen.Kind);
                foreach (var ruleEvent in result.Events)
                {
                    eventKinds.Add(ruleEvent.Kind);
                }

                if (chosen.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    chosen.EffectChoice.Candidate.OptionNodeId.IsDefined)
                {
                    options.Add(chosen.EffectChoice.Candidate.OptionNodeId.Value);
                }
            }

            return Record(seed, commands, session, options, commandKinds, eventKinds,
                exhaustedItsBudget: false, stalled: stalled);
        }

        /// <summary>
        /// Every command the projection is currently offering, in a stable order.
        /// </summary>
        /// <remarks>
        /// The order of the branches is the order the match asks its questions in, not a preference:
        /// exactly one of them produces candidates at any live point, because a pending decision
        /// suppresses everything behind it. They are written as a chain of early returns so that stays
        /// true by construction rather than by the branches happening not to overlap.
        /// </remarks>
        public static IReadOnlyList<DirectorCommand> EnumerateLegalCommands(MatchSessionProjection projection)
        {
            if (projection == null)
            {
                throw new ArgumentNullException(nameof(projection));
            }

            if (projection.Phase == RoundResolutionPhase.GameEnded)
            {
                return Array.Empty<DirectorCommand>();
            }

            var candidates = new List<DirectorCommand>();

            var defense = projection.PendingDefenseChoice;
            if (defense != null)
            {
                foreach (var offer in defense.LegalDefenseOffers)
                {
                    candidates.Add(DirectorCommand.ChooseDefenseCard(
                        defense.ResolutionId, defense.ChoiceId, offer.SourceCardInstanceId));
                }

                if (defense.CanDecline)
                {
                    candidates.Add(DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
                }

                return candidates;
            }

            var attackTarget = projection.PendingAttackTargetChoice;
            if (attackTarget != null)
            {
                foreach (var hero in attackTarget.LegalTargetHeroIds)
                {
                    candidates.Add(DirectorCommand.ChooseAttackTarget(
                        attackTarget.ResolutionId, attackTarget.ChoiceId, hero));
                }

                foreach (var minion in attackTarget.LegalTargetMinionIds)
                {
                    candidates.Add(DirectorCommand.ChooseAttackTarget(
                        attackTarget.ResolutionId, attackTarget.ChoiceId, minion));
                }

                return candidates;
            }

            var movement = projection.PendingMovementChoice;
            if (movement != null)
            {
                foreach (var destination in movement.LegalDestinations)
                {
                    candidates.Add(DirectorCommand.ChooseMovementDestination(
                        movement.ResolutionId, movement.ChoiceId, destination.Destination));
                }

                if (movement.CanStop)
                {
                    candidates.Add(DirectorCommand.StopMovement(movement.ResolutionId, movement.ChoiceId));
                }

                return candidates;
            }

            var removalPrevention = projection.PendingRemovalPrevention;
            if (removalPrevention != null)
            {
                foreach (var payment in removalPrevention.Payments)
                {
                    candidates.Add(DirectorCommand.AnswerRemovalPrevention(payment.Invocation));
                }

                candidates.Add(DirectorCommand.AnswerRemovalPrevention(
                    removalPrevention.DeclineInvocation));
                return candidates;
            }

            var effectChoice = projection.PendingEffectChoice;
            if (effectChoice != null)
            {
                foreach (var invocation in effectChoice.SelectInvocations)
                {
                    candidates.Add(DirectorCommand.AnswerEffectChoice(invocation));
                }

                if (effectChoice.ConfirmInvocation != null)
                {
                    candidates.Add(DirectorCommand.AnswerEffectChoice(effectChoice.ConfirmInvocation));
                }

                if (effectChoice.DeclineInvocation != null)
                {
                    candidates.Add(DirectorCommand.AnswerEffectChoice(effectChoice.DeclineInvocation));
                }

                if (effectChoice.StopInvocation != null)
                {
                    candidates.Add(DirectorCommand.AnswerEffectChoice(effectChoice.StopInvocation));
                }

                return candidates;
            }

            var correction = projection.PendingDisplacedMinionCorrection;
            if (correction != null)
            {
                foreach (var invocation in correction.SelectInvocations)
                {
                    candidates.Add(invocation.Kind == GuardsDigital.Core.Rules.Displacement
                            .DisplacedMinionCorrectionCommandKind.SelectSubject
                        ? DirectorCommand.ChooseCorrectionSubject(invocation)
                        : DirectorCommand.ChooseCorrectionDestination(invocation));
                }

                return candidates;
            }

            var respawn = projection.PendingHeroRespawnChoice;
            if (respawn != null)
            {
                foreach (var invocation in respawn.RespawnInvocations)
                {
                    candidates.Add(DirectorCommand.RespawnHero(invocation));
                }

                candidates.Add(DirectorCommand.ResolveCardWithoutRespawn(respawn.DeclineInvocation));
                return candidates;
            }

            var transitionStep = projection.PendingSystemTransitionStep;
            if (transitionStep != null)
            {
                foreach (var invocation in transitionStep.Invocations)
                {
                    candidates.Add(DirectorCommand.AnswerSystemTransitionStep(invocation));
                }

                return candidates;
            }

            var battle = projection.PendingMinionBattleChoice;
            if (battle != null)
            {
                foreach (var invocation in battle.SelectInvocations)
                {
                    candidates.Add(DirectorCommand.SelectMinionBattleRemoval(invocation));
                }

                // p.8 makes the short team remove the difference, so the confirmation is offered only
                // once the selection is the size the battle asks for. The choice states that itself;
                // this driver reads it rather than counting minions.
                if (battle.CanConfirm)
                {
                    candidates.Add(DirectorCommand.ConfirmMinionBattleRemovals(battle.ConfirmInvocation));
                }

                return candidates;
            }

            if (projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                candidates.Add(DirectorCommand.AdvanceEndOfRound());
                return candidates;
            }

            var initiative = projection.PendingInitiativeChoice;
            if (initiative != null)
            {
                foreach (var card in initiative.CandidateCardInstanceIds)
                {
                    candidates.Add(DirectorCommand.ChooseTiedActor(initiative.OwningSide, card));
                }

                return candidates;
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                // One hero at a time, in the projection's own hero order, so a seed always faces the
                // same question. Committing is the whole decision here; which card is the rest of it.
                var pending = projection.Commitments
                    .FirstOrDefault(value => value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                if (!pending.HeroId.IsDefined)
                {
                    return candidates;
                }

                foreach (var card in projection.Cards
                    .Where(card => card.OwnerId.Equals(pending.HeroId) && card.State.Location == CardLocation.Hand)
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal))
                {
                    candidates.Add(DirectorCommand.CommitCard(pending.HeroId, card.Id));
                }

                if (pending.CanPass)
                {
                    candidates.Add(DirectorCommand.Pass(pending.HeroId));
                }

                return candidates;
            }

            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined)
            {
                return candidates;
            }

            foreach (var offer in projection.AvailableTurnActions.Where(offer => offer.IsAvailable))
            {
                candidates.Add(DirectorCommand.StartAction(offer.ActionDefinitionId));
            }

            foreach (var option in projection.AvailableFastTravelOptions)
            {
                foreach (var destination in option.LegalDestinations)
                {
                    // p.7 makes Fast Travel a replacement, so the command names the Movement it
                    // replaces rather than the replacement offer's own id.
                    candidates.Add(DirectorCommand.PerformFastTravel(
                        option.ReplacedMovementActionDefinitionId, destination));
                }
            }

            foreach (var option in projection.AvailableClearOptions)
            {
                foreach (var invocation in option.SubsetInvocations)
                {
                    candidates.Add(invocation);
                }
            }

            // The general Hold, asked of the same query the HUD asks rather than assumed from the
            // phase: p.7 derives it from a selected unresolved card, and this driver must not be the
            // second place that decides what "unresolved" means.
            var hold = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                projection.SelectedActor.CardInstanceId,
                projection.SelectedActor.ActorId,
                projection.Definitions,
                projection.Cards, RuleModificationSet.Empty);
            if (hold.HasValue && hold.Value.IsAvailable)
            {
                candidates.Add(DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
            }

            return candidates;
        }

        /// <summary>
        /// Drives a fresh session through an already-recorded command list, refusing to steer.
        /// </summary>
        /// <remarks>
        /// This is the replay half of the oracle. It executes exactly the recorded commands in order
        /// and asserts each is accepted, which is a stronger claim than "the replay ends in the same
        /// state": a command that the live run could take and the replayed run could not would fail
        /// here at the command that diverged, naming it, rather than showing up as an unequal snapshot
        /// at the end.
        /// </remarks>
        public static void Replay(MatchSession session, IReadOnlyList<DirectorCommand> commands)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            for (var index = 0; index < commands.Count; index++)
            {
                var result = session.Execute(commands[index]);
                Assert.That(result.Accepted, Is.True,
                    $"Replayed command {index + 1} of {commands.Count} ({commands[index].Kind}) was " +
                    $"refused although the live run accepted it: {result.Detail}");
            }
        }

        private static MatchLoopRecord Record(
            int seed,
            List<DirectorCommand> commands,
            MatchSession session,
            SortedSet<string> options,
            SortedSet<DirectorCommandKind> commandKinds,
            SortedSet<RoundResolutionEventKind> eventKinds,
            bool exhaustedItsBudget,
            bool stalled)
        {
            var projection = session.Projection;
            return new MatchLoopRecord(
                seed,
                commands,
                projection.RoundIndex,
                projection.Outcome,
                exhaustedItsBudget,
                stalled,
                options,
                commandKinds,
                eventKinds,
                projection.AuthoritativeSnapshot,
                projection.Version);
        }
    }
}
