using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TeamAgreementTests
    {
        [Test]
        public void TiedInitiative_PlayerSeatsWaitForUnanimousPublicCandidatePreferenceBeforeChoosingTheActor()
        {
            var host = ReachAgreement(TeamAgreementKind.Initiative);
            var publicProposal = host.Seats.Seats[0].Id;
            var agreement = host.ProjectionForSeat(publicProposal).TeamAgreement;
            var chosen = agreement.Candidates[0].CardInstanceId;
            var startingVersion = host.Version;
            var startingSequence = host.Sequence;

            for (var index = 0; index < agreement.Participants.Count; index++)
            {
                var participant = agreement.Participants[index];
                var seat = host.Seats.Seats.Single(value => value.HeroId.Equals(participant.HeroId));
                var view = host.ProjectionForSeat(seat.Id);
                var command = view.TeamAgreement.AuthorizedCommands.Single(value =>
                    value.PreferredCardInstanceId.Equals(chosen));
                var result = host.ClientForSeat(seat.Id).SubmitAgreement(new TeamAgreementSubmission(
                    new SubmissionId("initiative-unanimity-" + index), view.Version, command));
                Assert.That(result.Accepted, Is.True, result.Detail);

                if (index + 1 < agreement.Participants.Count)
                {
                    Assert.That(host.Version, Is.EqualTo(startingVersion));
                    Assert.That(host.Sequence, Is.EqualTo(startingSequence));
                    Assert.That(host.Projection.PendingInitiativeChoice, Is.Not.Null);
                    foreach (var observer in host.Seats.Seats)
                        Assert.That(host.ProjectionForSeat(observer.Id).TeamAgreement.Participants[index]
                            .PreferredCardInstanceId, Is.EqualTo(chosen));
                }
            }

            Assert.That(host.Projection.PendingInitiativeChoice, Is.Null);
            Assert.That(host.Version, Is.EqualTo(startingVersion + 1));
            Assert.That(host.Sequence, Is.EqualTo(startingSequence + 1),
                "Unanimity submits exactly one existing rules command.");
        }

        /// <summary>
        /// Milestone 9.29. The 2026-09-30 playtest's complaint, as a fixture: one seat's clicks change
        /// only that seat's picks, nothing reaches the authority until every choosing seat holds the
        /// same complete set, and then exactly the existing Selects and the Confirm do.
        /// </summary>
        [Test]
        public void MinionBattle_EachSeatPicksForItself_AndMatchingPicksRemoveThroughTheExistingSelectsAndConfirm()
        {
            var host = ReachAgreement(TeamAgreementKind.MinionBattle);
            var choosers = ChoosingSeats(host);
            Assert.That(choosers.Length, Is.GreaterThan(1), "A six-seat battle is chosen by a team.");
            var required = host.Projection.PendingMinionBattleChoice.RequiredRemovalCount;
            var startingVersion = host.Version;
            var startingSequence = host.Sequence;
            var acceptedBefore = host.Projection.AcceptedCommands.Count;

            var leaderPicks = PickFirstOffered(host, choosers[0], required, "own-picks");
            Assert.That(leaderPicks.Count, Is.EqualTo(required));
            foreach (var teammate in choosers.Skip(1))
            {
                Assert.That(host.ProjectionForSeat(teammate.Id).TeamAgreement.ViewerPicks, Is.Empty,
                    "One seat's picks never move a teammate's.");
                Assert.That(host.ProjectionForSeat(teammate.Id).TeamAgreement.PicksOf(choosers[0].HeroId),
                    Is.EqualTo(leaderPicks), "Every teammate sees what the first seat picked.");
            }

            Assert.That(host.Version, Is.EqualTo(startingVersion), "A pick is a statement, not a move.");
            Assert.That(host.Sequence, Is.EqualTo(startingSequence));
            Assert.That(host.Projection.PendingMinionBattleChoice.SelectedUnits, Is.Empty,
                "The authority's selection is not shared between players any more.");

            for (var index = 1; index < choosers.Length; index++)
            {
                PickSet(host, choosers[index], leaderPicks, "match-" + index);
                if (index + 1 < choosers.Length)
                {
                    Assert.That(host.Projection.PendingMinionBattleChoice, Is.Not.Null,
                        "A seat that has not picked still holds the removal.");
                    Assert.That(host.Version, Is.EqualTo(startingVersion));
                }
            }

            var appended = host.Projection.AcceptedCommands.Skip(acceptedBefore).ToArray();
            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(appended.Take(required).Select(UnitOf), Is.EqualTo(leaderPicks),
                "One existing Select per agreed unit, in the order it was picked.");
            Assert.That(appended.Take(required).All(command =>
                command.Kind == DirectorCommandKind.SelectMinionBattleRemoval), Is.True);
            Assert.That(appended[required].Kind, Is.EqualTo(DirectorCommandKind.ConfirmMinionBattleRemovals));
            Assert.That(appended.Count(command =>
                    command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals),
                Is.EqualTo(1), "The final rules choice reaches the authority exactly once.");
            Assert.That(appended.Skip(required + 1).All(command =>
                command.Kind == DirectorCommandKind.AdvanceEndOfRound), Is.True);
            Assert.That(host.Version, Is.EqualTo(startingVersion + appended.Length));
        }

        [Test]
        public void MinionBattle_DifferentCompleteSetsRemoveNothing_UntilTheyMatch()
        {
            var host = ReachAgreement(TeamAgreementKind.MinionBattle);
            var choosers = ChoosingSeats(host);
            var choice = host.Projection.PendingMinionBattleChoice;
            var required = choice.RequiredRemovalCount;
            var leaderPicks = PickFirstOffered(host, choosers[0], required, "leader");
            var other = host.ProjectionForSeat(choosers[1].Id).TeamAgreement.AuthorizedCommands
                .First(value => value.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval &&
                    !leaderPicks.Contains(value.Unit)).Unit;
            var different = leaderPicks.Skip(1).Concat(new[] { other }).ToArray();
            PickSet(host, choosers[1], different, "different");
            foreach (var rest in choosers.Skip(2))
                PickSet(host, rest, leaderPicks, "rest-" + rest.Id.Value);

            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Not.Null,
                "Two complete, different sets remove nothing.");
            Assert.That(host.Projection.PendingMinionBattleChoice.SelectedUnits, Is.Empty);
            var view = host.ProjectionForSeat(choosers[0].Id).TeamAgreement;
            Assert.That(view.Participants.All(participant => participant.PicksComplete), Is.True);
            Assert.That(view.PicksOf(choosers[1].HeroId), Is.EquivalentTo(different));

            PickSet(host, choosers[1], leaderPicks, "matched");
            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null,
                "The pick that makes every set match removes the minions.");
        }

        [Test]
        public void MinionBattle_APickOnAFullSetReplacesThatSeatsOldestPick()
        {
            var host = ReachAgreement(TeamAgreementKind.MinionBattle);
            var seat = ChoosingSeats(host)[0];
            var required = host.Projection.PendingMinionBattleChoice.RequiredRemovalCount;
            var picks = PickFirstOffered(host, seat, required, "full");
            var agreement = host.ProjectionForSeat(seat.Id).TeamAgreement;
            var swap = agreement.AuthorizedCommands.First(value =>
                value.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval && !picks.Contains(value.Unit));
            Submit(host, seat, swap, "swap");

            var after = host.ProjectionForSeat(seat.Id).TeamAgreement.ViewerPicks;
            Assert.That(after.Count, Is.EqualTo(required), "A full set stays full.");
            Assert.That(after, Has.No.Member(picks[0]), "The oldest pick is the one replaced.");
            Assert.That(after.Last(), Is.EqualTo(swap.Unit));
            Assert.That(after, Is.EquivalentTo(picks.Skip(1).Concat(new[] { swap.Unit })));
        }

        [Test]
        public void MinionBattle_NoSeatIsAuthorizedAMinionBattleRulesCommand_AndTheOtherSideSeesNoPicks()
        {
            var host = ReachAgreement(TeamAgreementKind.MinionBattle);
            var choice = host.Projection.PendingMinionBattleChoice;
            var choosers = ChoosingSeats(host);
            PickFirstOffered(host, choosers[0], 1, "visible-to-team");

            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                Assert.That(projection.AuthorizedCommands, Has.None.Matches<DirectorCommand>(command =>
                    command.Kind == DirectorCommandKind.SelectMinionBattleRemoval ||
                    command.Kind == DirectorCommandKind.DeselectMinionBattleRemoval ||
                    command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals),
                    "Seat " + seat.Id + " answers the battle only through its own picks.");
                var agreement = projection.TeamAgreement;
                Assert.That(agreement, Is.Not.Null, "Both sides are told the battle is being chosen.");
                if (seat.PlaysFor(choice.ChoosingSide))
                {
                    Assert.That(agreement.PicksOf(choosers[0].HeroId), Has.Count.EqualTo(1));
                    Assert.That(agreement.AuthorizedCommands, Is.Not.Empty);
                }
                else
                {
                    Assert.That(agreement.Participants.SelectMany(participant => participant.Picks),
                        Is.Empty, "Which minions a team is weighing up is that team's conversation.");
                    Assert.That(agreement.Participants.Any(participant => participant.PicksComplete),
                        Is.False);
                    Assert.That(agreement.AuthorizedCommands, Is.Empty);
                }
            }
        }

        [Test]
        public void MinionBattle_TheOrderTheAuthorityRemovesInBoundsEachPickersSet()
        {
            // The authority's Heavy-last order (rulebook p.8) asked of one picker's own set: with one
            // non-Heavy and two Heavy candidates and two removals, the non-Heavy is picked first, a
            // Heavy only beside it, and the non-Heavy cannot be taken back while a Heavy stands.
            var melee = RuleObject.ForMinion(new MinionId("blue-melee-91"));
            var heavyOne = RuleObject.ForMinion(new MinionId("blue-heavy-91"));
            var heavyTwo = RuleObject.ForMinion(new MinionId("blue-heavy-92"));
            var choice = new PendingMinionBattleChoice(
                new EndOfRoundResolutionId("r9-minion-battle"),
                new EndOfRoundChoiceId("r9-minion-battle-removals"),
                new TeamId("blue-team"),
                TeamSide.Blue,
                new ZoneId("zone-9"),
                2,
                new[]
                {
                    new MinionBattleCandidate(melee.MinionId, StandardMinionType.Melee),
                    new MinionBattleCandidate(heavyOne.MinionId, StandardMinionType.Heavy),
                    new MinionBattleCandidate(heavyTwo.MinionId, StandardMinionType.Heavy)
                },
                Array.Empty<MinionId>());
            var none = Array.Empty<RuleObject>();

            Assert.That(MinionBattlePickRules.AfterPick(choice, none, heavyOne), Is.Null,
                "A Heavy is not picked before the non-Heavy.");
            var first = MinionBattlePickRules.AfterPick(choice, none, melee);
            Assert.That(first, Is.EqualTo(new[] { melee }));
            var full = MinionBattlePickRules.AfterPick(choice, first, heavyOne);
            Assert.That(full, Is.EqualTo(new[] { melee, heavyOne }));
            Assert.That(MinionBattlePickRules.AfterUnpick(choice, full, melee), Is.Null,
                "The non-Heavy stays while a Heavy is picked.");
            Assert.That(MinionBattlePickRules.AfterUnpick(choice, full, heavyOne), Is.EqualTo(new[] { melee }));
            Assert.That(MinionBattlePickRules.AfterPick(choice, full, heavyTwo), Is.EqualTo(new[] { melee, heavyTwo }),
                "On a full set the swap replaces the oldest pick that leaves a legal set - the Heavy.");
        }

        /// <summary>
        /// Milestone 9.29. Nothing but the host selects on a seat-served match, so a selection the
        /// picks do not name can only be an earlier submission that stopped part-way. Staged here by
        /// the host's own Director door; the matching picks still remove exactly what they name.
        /// </summary>
        [Test]
        public void MinionBattle_MatchingPicksTakeBackAStraySelectionBeforeRemovingWhatTheyName()
        {
            var host = ReachAgreement(TeamAgreementKind.MinionBattle);
            var choice = host.Projection.PendingMinionBattleChoice;
            var stray = host.Projection.DecisionContract.Commands.Last(command =>
                command.Kind == DirectorCommandKind.SelectMinionBattleRemoval);
            Assert.That(host.Submit(MatchSubmission.ForCommand(stray)).Accepted, Is.True);
            var strayUnit = UnitOf(stray);
            Assert.That(host.Projection.PendingMinionBattleChoice.SelectedUnits, Is.EqualTo(new[] { strayUnit }));

            var choosers = ChoosingSeats(host);
            var picks = choice.Candidates
                .Select(candidate => candidate.Unit)
                .Where(unit => !unit.Equals(strayUnit) &&
                    !choice.Candidates.Single(candidate => candidate.Unit.Equals(unit)).IsHeavy)
                .Take(choice.RequiredRemovalCount)
                .ToArray();
            Assert.That(picks.Length, Is.EqualTo(choice.RequiredRemovalCount),
                "The battle offers a complete set that leaves the stray selection out.");
            var acceptedBefore = host.Projection.AcceptedCommands.Count;
            foreach (var seat in choosers)
                PickSet(host, seat, picks, "stray-" + seat.Id.Value);

            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null);
            var appended = host.Projection.AcceptedCommands.Skip(acceptedBefore).ToArray();
            Assert.That(appended[0].Kind, Is.EqualTo(DirectorCommandKind.DeselectMinionBattleRemoval));
            Assert.That(UnitOf(appended[0]), Is.EqualTo(strayUnit), "The stray selection is taken back first.");
            Assert.That(appended.Skip(1).Take(picks.Length).Select(UnitOf), Is.EquivalentTo(picks));
            Assert.That(appended[picks.Length + 1].Kind,
                Is.EqualTo(DirectorCommandKind.ConfirmMinionBattleRemovals));
        }

        [Test]
        public void MinionBattle_APickAgainstAResolvedBattleIsRefusedAsAStaleProposal()
        {
            var host = ReachAgreement(TeamAgreementKind.MinionBattle);
            var choosers = ChoosingSeats(host);
            var required = host.Projection.PendingMinionBattleChoice.RequiredRemovalCount;
            var leaderPicks = PickFirstOffered(host, choosers[0], required, "stale-lead");
            var lastSeat = choosers[choosers.Length - 1];
            var leftover = host.ProjectionForSeat(lastSeat.Id).TeamAgreement.AuthorizedCommands.First(value =>
                value.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval);
            for (var index = 1; index < choosers.Length; index++)
                PickSet(host, choosers[index], leaderPicks, "stale-" + index);
            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null);

            var version = host.Version;
            var refused = host.ClientForSeat(lastSeat.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("stale-pick"), version, leftover));
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(TeamAgreementSubmissionRejectionReason.StaleProposal));
            Assert.That(host.Version, Is.EqualTo(version));
        }

        [Test]
        public void TeamAgreement_RefusesIneligibleOrStaleStatementsAndSubmitsOneFamilyRulesChoice()
        {
            var host = ReachAgreement(TeamAgreementKind.Initiative);
            var eligible = host.Seats.Seats.First(seat =>
                host.ProjectionForSeat(seat.Id).TeamAgreement.AuthorizedCommands.Count > 0);
            var outsider = host.Seats.Seats.First(seat =>
                host.ProjectionForSeat(seat.Id).TeamAgreement.AuthorizedCommands.Count == 0);
            var offered = host.ProjectionForSeat(eligible.Id).TeamAgreement.AuthorizedCommands[0];
            var version = host.Version;
            var sequence = host.Sequence;

            var ineligible = host.ClientForSeat(outsider.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-ineligible"), version, offered));
            Assert.That(ineligible.Accepted, Is.False);
            Assert.That(ineligible.RejectionReason,
                Is.EqualTo(TeamAgreementSubmissionRejectionReason.IneligibleSeat));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));

            var stale = host.ClientForSeat(eligible.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-stale"), version + 1, offered));
            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.RejectionReason,
                Is.EqualTo(TeamAgreementSubmissionRejectionReason.StaleProjection));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));

            var first = host.ClientForSeat(eligible.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-duplicate"), version, offered));
            Assert.That(first.Accepted, Is.True);

            // Milestone 8.34. An identical repeat is answered from what the statement already did
            // - the failure policy's retained-result clause, which the agreement path obeys
            // exactly as the command path does - and records nothing a second time.
            var repeated = host.ClientForSeat(eligible.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-duplicate"), version, offered));
            Assert.That(repeated.Accepted, Is.EqualTo(first.Accepted));
            Assert.That(repeated.RejectionReason, Is.EqualTo(first.RejectionReason));
            Assert.That(repeated.Version, Is.EqualTo(first.Version));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));

            // The same identity carrying a different statement is a used name, and is refused.
            var withdrawal = host.ProjectionForSeat(eligible.Id).TeamAgreement.AuthorizedCommands
                .FirstOrDefault(command => !command.Equals(offered));
            if (withdrawal != null)
            {
                var duplicate = host.ClientForSeat(eligible.Id).SubmitAgreement(
                    new TeamAgreementSubmission(
                        new SubmissionId("agreement-duplicate"), version, withdrawal));
                Assert.That(duplicate.Accepted, Is.False);
                Assert.That(duplicate.RejectionReason,
                    Is.EqualTo(TeamAgreementSubmissionRejectionReason.DuplicateSubmission));
                Assert.That(host.Version, Is.EqualTo(version));
                Assert.That(host.Sequence, Is.EqualTo(sequence));
            }
        }

        /// <summary>
        /// Milestone 8.35e shares this walk rather than re-deriving a second seed: it is the one
        /// place that knows how far a seeded seat-served match has to go to reach each agreement
        /// family, and a second copy would drift from it the next time the walk moves.
        /// </summary>
        internal static IMatchHost ReachAgreement(TeamAgreementKind kind)
        {
            TeamAgreementTestDriver.Reset();
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());

            // Milestone 8.35g re-derived the seed again, offline over seeds 1-20 rather than by
            // bisecting Unity runs. The release consumes a draw of its own even when it is the only
            // legal command, so every trajectory is a different match rather than the old one plus a
            // command per turn: seed 3 reaches no Minion Battle at all inside 3,000 steps now.
            // Seed 6 reached the tied Initiative at step 80 and the Minion Battle at 400 - chosen
            // because it reaches BOTH kinds this helper is asked for, comfortably inside the budget,
            // rather than because it is the lowest number that reaches one.
            //
            // Milestone 8.35z25 re-derived it again, 6 -> 29, on the same selection rule.
            // Combat Reflexes and Leaping Strike now ask their printed move before their targeting,
            // which changes what this walk is offered and therefore where it goes: seed 6 still
            // reaches the tied Initiative (step 80, unchanged) but reaches NO Minion Battle at all
            // inside 2,400 steps. An offline scan of seeds 1-30 against BOTH kinds found seed 29
            // reaches the Initiative at step 67 and the Minion Battle at 323 - the closest match in
            // shape to what seed 6 used to give, and comfortably inside the budget for both.
            var random = new MatchLoopRandom(29);
            for (var step = 0; step < 2400; step++)
            {
                var agreement = host.Seats.Seats.Select(seat => host.ProjectionForSeat(seat.Id).TeamAgreement)
                    .FirstOrDefault(value => value != null);
                if (agreement?.Kind == kind)
                    return host;

                var legal = TeamAgreementTestDriver.EnumerateWalkCommands(host.Projection);
                Assert.That(legal, Is.Not.Empty, "The seeded match ended before reaching " + kind + ".");
                var command = legal[random.Next(legal.Count)];
                if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(
                    host, command, "reach-" + kind + "-" + step))
                    continue;
                var seat = host.Seats.Seats.First(value =>
                    host.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
                var client = host.ClientForSeat(seat.Id);
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId("reach-direct-" + kind + "-" + step),
                    client.Projection.Version,
                    command));
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            throw new AssertionException("The seeded match did not reach " + kind + ".");
        }

        internal static MatchSeat[] ChoosingSeats(IMatchHost host)
        {
            var side = host.Projection.PendingMinionBattleChoice.ChoosingSide;
            return host.Seats.Seats.Where(seat => seat.PlaysFor(side)).ToArray();
        }

        /// <summary>Milestone 9.29. One seat picks the first offered units until it holds <paramref name="count"/>.</summary>
        internal static IReadOnlyList<RuleObject> PickFirstOffered(
            IMatchHost host, MatchSeat seat, int count, string idPrefix)
        {
            for (var index = 0; host.ProjectionForSeat(seat.Id).TeamAgreement.ViewerPicks.Count < count; index++)
            {
                Assert.That(index, Is.LessThan(count + 4), "Picking did not converge.");
                var statement = host.ProjectionForSeat(seat.Id).TeamAgreement.AuthorizedCommands.First(value =>
                    value.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval);
                Submit(host, seat, statement, idPrefix + "-" + index);
            }

            return host.ProjectionForSeat(seat.Id).TeamAgreement.ViewerPicks.ToArray();
        }

        /// <summary>Milestone 9.29. One seat takes back and adds picks until it holds exactly <paramref name="units"/>.</summary>
        internal static void PickSet(
            IMatchHost host, MatchSeat seat, IReadOnlyList<RuleObject> units, string idPrefix)
        {
            for (var index = 0; host.Projection.PendingMinionBattleChoice != null; index++)
            {
                var agreement = host.ProjectionForSeat(seat.Id).TeamAgreement;
                var own = agreement.ViewerPicks;
                if (own.Count == units.Count && own.All(units.Contains))
                    return;
                Assert.That(index, Is.LessThan((units.Count * 2) + 4), "Picking did not converge.");
                var statement = own.Where(value => !units.Contains(value))
                        .Select(value => agreement.AuthorizedCommands.FirstOrDefault(offered =>
                            offered.Kind == TeamAgreementCommandKind.UnpickMinionBattleRemoval &&
                            offered.Unit.Equals(value)))
                        .FirstOrDefault(value => value != null) ??
                    units.Where(value => !own.Contains(value))
                        .Select(value => agreement.AuthorizedCommands.First(offered =>
                            offered.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval &&
                            offered.Unit.Equals(value)))
                        .First();
                Submit(host, seat, statement, idPrefix + "-" + index);
            }
        }

        internal static void Submit(IMatchHost host, MatchSeat seat, TeamAgreementCommand statement, string id)
        {
            var result = host.ClientForSeat(seat.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId(id), host.ProjectionForSeat(seat.Id).Version, statement));
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static RuleObject UnitOf(DirectorCommand command) =>
            command.HeroId.IsDefined
                ? RuleObject.ForHero(command.HeroId)
                : RuleObject.ForMinion(command.MinionId);
    }
}
