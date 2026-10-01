using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    internal static class TeamAgreementTestDriver
    {
        // Milestone 9.29. The units a walk or a Director recording has selected so far in the Minion
        // Battle it is answering through the seats, keyed by that battle's own identity so a host replaced
        // from a snapshot mid-battle still confirms the same set.
        private static readonly Dictionary<string, List<RuleObject>> recordedRemovals =
            new Dictionary<string, List<RuleObject>>();

        // Milestone 9.29. A pick does not move the host's version or sequence, so a walk's own step
        // count can repeat across several picks; this keeps every pick's submission identity distinct.
        private static long pickOrdinal;

        /// <summary>
        /// Whether a seat answers this Director command through the host agreement rather than by
        /// submitting it: the tied Initiative's final choice, and - since milestone 9.29, when each
        /// choosing seat began picking for itself - every Minion Battle command.
        /// </summary>
        public static bool IsFinalAgreementCommand(DirectorCommand command) =>
            command != null &&
            (command.Kind == DirectorCommandKind.ChooseTiedActor ||
             command.Kind == DirectorCommandKind.SelectMinionBattleRemoval ||
             command.Kind == DirectorCommandKind.DeselectMinionBattleRemoval ||
             command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals);

        public static bool SubmitFinalAgreementCommand(
            IMatchHost host,
            DirectorCommand finalCommand,
            string idPrefix,
            Action<MatchSeat> onStatementAccepted = null)
        {
            if (!IsFinalAgreementCommand(finalCommand))
                return false;

            if (finalCommand.Kind != DirectorCommandKind.ChooseTiedActor)
                return ReplayMinionBattleCommand(host, finalCommand, idPrefix, onStatementAccepted);

            var initial = host.Seats.Seats
                .Select(seat => new { Seat = seat, Projection = host.ProjectionForSeat(seat.Id) })
                .FirstOrDefault(value => value.Projection.TeamAgreement != null);
            Assert.That(initial, Is.Not.Null, "A final team choice must publish an agreement proposal.");

            var participants = initial.Projection.TeamAgreement.Participants
                .Select(value => value.HeroId)
                .ToArray();
            foreach (var heroId in participants)
            {
                var seat = host.Seats.Seats.Single(value => value.HeroId.Equals(heroId));
                var projection = host.ProjectionForSeat(seat.Id);
                var agreement = projection.TeamAgreement;
                Assert.That(agreement, Is.Not.Null);
                var statement = agreement.AuthorizedCommands.Single(value =>
                    value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                    value.PreferredCardInstanceId.Equals(finalCommand.CardInstanceId));
                var result = host.ClientForSeat(seat.Id).SubmitAgreement(
                    new TeamAgreementSubmission(
                        new SubmissionId(idPrefix + "-" + seat.Id.Value),
                        projection.Version,
                        statement));
                Assert.That(result.Accepted, Is.True, result.Detail);
                onStatementAccepted?.Invoke(seat);
            }

            return true;
        }


        /// <summary>
        /// Milestone 9.29. Carries a walk's or a recording's Minion Battle command into a seat-served
        /// host. A Select (or Deselect) is only noted: the authority's own selection now stays empty
        /// until every seat's picks match, so what the walk has selected lives here, and
        /// <see cref="EnumerateWalkCommands"/> offers the walk exactly the list the shared selection
        /// used to - which keeps every seeded walk on the trajectory it always took. The Confirm is
        /// where every choosing seat picks that set, and the host then submits the same Selects in
        /// the same order and the Confirm.
        /// </summary>
        private static bool ReplayMinionBattleCommand(
            IMatchHost host,
            DirectorCommand command,
            string idPrefix,
            Action<MatchSeat> onStatementAccepted)
        {
            var choice = host.Projection.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null, "A walk's Minion Battle command needs a standing battle.");
            var confirming = command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals;
            var version = host.Projection.Version;
            var target = confirming
                ? RecordedRemovals(choice, version)
                : RecordRemoval(choice, version, command);
            if (confirming)
            {
                Assert.That(target.Count, Is.EqualTo(choice.RequiredRemovalCount),
                    "The walk confirmed a selection the driver did not see completed.");
            }

            // Every choosing seat but the last follows the walk's selection as it is made, so the
            // states between the walk's steps carry partial and complete picks; the last seat picks
            // only at the confirmation, which is where the walk's own Confirm stood.
            var choosers = host.Seats.Seats.Where(value => value.PlaysFor(choice.ChoosingSide)).ToArray();
            foreach (var seat in confirming ? choosers : choosers.Take(choosers.Length - 1))
            {
                while (host.Projection.PendingMinionBattleChoice != null)
                {
                    var projection = host.ProjectionForSeat(seat.Id);
                    var agreement = projection.TeamAgreement;
                    Assert.That(agreement, Is.Not.Null, "A choosing seat is served the battle's agreement.");
                    var statement = NextPickTowards(agreement, agreement.ViewerPicks, target);
                    if (statement == null)
                        break;
                    var result = host.ClientForSeat(seat.Id).SubmitAgreement(
                        new TeamAgreementSubmission(
                            new SubmissionId(idPrefix + "-" + seat.Id.Value + "-pick-" + pickOrdinal++),
                            projection.Version,
                            statement));
                    Assert.That(result.Accepted, Is.True, result.Detail);
                    onStatementAccepted?.Invoke(seat);
                }
            }

            if (!confirming)
            {
                Assert.That(host.Projection.PendingMinionBattleChoice, Is.Not.Null,
                    "A walk's select removes nothing while its last chooser has not picked.");
                return true;
            }

            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null,
                "Every choosing seat picked the walk's selection, so the minions must be removed.");
            ForgetRemovals(choice, version);
            return true;
        }

        /// <summary>
        /// Milestone 9.29. The commands a seeded walk over a seat-served host is offered: the
        /// authority's own, except at a Minion Battle the walk has begun selecting, where it is offered
        /// what that selection would offer - the remaining Selects and, once complete, the Confirm -
        /// exactly as the shared selection did before each seat picked for itself.
        /// </summary>
        internal static IReadOnlyList<DirectorCommand> EnumerateWalkCommands(MatchSessionProjection projection)
        {
            var choice = projection?.PendingMinionBattleChoice;
            if (choice != null && choice.SelectedUnits.Count == 0 &&
                recordedRemovals.TryGetValue(Key(choice, projection.Version), out var recorded) &&
                recorded.Count > 0 && recorded.Count <= choice.RequiredRemovalCount &&
                recorded.All(unit => choice.Candidates.Any(candidate => candidate.Unit.Equals(unit))))
            {
                var selection = choice.WithSelected(recorded);
                var commands = selection.SelectInvocations
                    .Select(DirectorCommand.SelectMinionBattleRemoval)
                    .ToList();
                if (selection.CanConfirm)
                    commands.Add(DirectorCommand.ConfirmMinionBattleRemovals(selection.ConfirmInvocation));
                return commands;
            }

            return DeterministicMatchLoop.EnumerateLegalCommands(projection);
        }

        /// <summary>Milestone 9.29. Starts a walk with nothing recorded, whatever an earlier one left.</summary>
        internal static void Reset() => recordedRemovals.Clear();

        /// <summary>
        /// Milestone 9.29. One pick or unpick that moves <paramref name="own"/> towards
        /// <paramref name="target"/>, or <c>null</c> when it already holds it.
        /// </summary>
        internal static TeamAgreementCommand NextPickTowards(
            TeamAgreementProjection agreement,
            IReadOnlyList<RuleObject> own,
            IReadOnlyList<RuleObject> target)
        {
            if (own.Count == target.Count && own.All(target.Contains))
                return null;

            var statement =
                own.Where(value => !target.Contains(value))
                    .Select(value => agreement.AuthorizedCommands.FirstOrDefault(offered =>
                        offered.Kind == TeamAgreementCommandKind.UnpickMinionBattleRemoval &&
                        offered.Unit.Equals(value)))
                    .FirstOrDefault(value => value != null) ??
                target.Where(value => !own.Contains(value))
                    .Select(value => agreement.AuthorizedCommands.FirstOrDefault(offered =>
                        offered.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval &&
                        offered.Unit.Equals(value)))
                    .FirstOrDefault(value => value != null);
            Assert.That(statement, Is.Not.Null, "No pick is offered towards the walk's selection.");
            return statement;
        }

        /// <summary>
        /// Milestone 9.29. Notes one walk Minion Battle command against the battle it answers: a
        /// select adds its unit, a deselect takes it out.
        /// </summary>
        internal static IReadOnlyList<RuleObject> RecordRemoval(
            PendingMinionBattleChoice choice, long version, DirectorCommand command)
        {
            var key = Key(choice, version);
            var unit = command.HeroId.IsDefined
                ? RuleObject.ForHero(command.HeroId)
                : RuleObject.ForMinion(command.MinionId);
            if (!recordedRemovals.TryGetValue(key, out var target))
                recordedRemovals[key] = target = new List<RuleObject>();
            if (command.Kind == DirectorCommandKind.SelectMinionBattleRemoval)
            {
                if (target.Contains(unit) || target.Count >= choice.RequiredRemovalCount)
                    target.Clear();
                target.Add(unit);
            }
            else if (command.Kind == DirectorCommandKind.DeselectMinionBattleRemoval)
            {
                target.Remove(unit);
            }

            return target.ToArray();
        }

        internal static IReadOnlyList<RuleObject> RecordedRemovals(PendingMinionBattleChoice choice, long version) =>
            recordedRemovals.TryGetValue(Key(choice, version), out var target)
                ? target.ToArray()
                : Array.Empty<RuleObject>();

        internal static void ForgetRemovals(PendingMinionBattleChoice choice, long version) =>
            recordedRemovals.Remove(Key(choice, version));

        // The battle's own identity and the version it stands at: a walk's selects and every pick leave
        // the version where it is, so a record from another walk - or from before a rewind - cannot be
        // mistaken for this one.
        private static string Key(PendingMinionBattleChoice choice, long version) =>
            choice.ResolutionId.Value + "/" + choice.ChoiceId.Value + "@" + version;
    }
}
