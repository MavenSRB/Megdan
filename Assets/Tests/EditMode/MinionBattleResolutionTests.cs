using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MinionBattleResolutionTests
    {
        [Test]
        public void EqualBattleZoneCounts_CompleteWithoutChoiceRemovalOrLanePush()
        {
            var session = CreateSession(minions => minions);
            AdvanceToEndOfRound(session);
            var beforeIds = session.Projection.Minions.Select(value => value.MinionId).ToArray();

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events.Select(value => value.Kind),
                Is.EqualTo(new[] { RoundResolutionEventKind.EndOfRoundStageResolved }));
            Assert.That(result.Events.Single().EndOfRoundOutcomeKind,
                Is.EqualTo(EndOfRoundStageOutcomeKind.Completed));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null);
            Assert.That(session.Projection.Minions.Select(value => value.MinionId), Is.EqualTo(beforeIds));
        }

        [Test]
        public void RemovalChoice_EnforcesOneAtATimeHeavyLastIdentityChooserAndAtomicConfirmation()
        {
            var session = CreateSession(RemoveOneBlueRanged);
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var choice = session.Projection.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.RequiredRemovalCount, Is.EqualTo(1));
            Assert.That(choice.ChoosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(choice.LosingTeamId,
                Is.EqualTo(session.Projection.Participants.Teams.Single(value => value.Side == TeamSide.Blue).Id));
            Assert.That(choice.SelectedMinionIds, Is.Empty);
            Assert.That(choice.LegalSelectMinionIds, Is.Not.Empty);
            Assert.That(choice.LegalSelectMinionIds, Has.None.Matches<MinionId>(id =>
                choice.Candidates.Single(value => value.MinionId.Equals(id)).MinionType == StandardMinionType.Heavy));
            Assert.That(choice.SelectInvocations.Select(value => value.MinionId),
                Is.EqualTo(choice.LegalSelectMinionIds));

            AssertAtomicRejection(
                session,
                DirectorCommand.ConfirmMinionBattleRemovals(choice.ConfirmInvocation));

            var legal = choice.SelectInvocations[0];
            var stale = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                new EndOfRoundResolutionId("stale-minion-battle"),
                choice.ChoiceId,
                choice.ChoosingSide,
                legal.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(stale));

            var wrongChooser = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                choice.ResolutionId,
                choice.ChoiceId,
                TeamSide.Red,
                legal.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(wrongChooser));

            var heavy = choice.Candidates.Single(value => value.MinionType == StandardMinionType.Heavy);
            var ineligibleHeavy = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.ChoosingSide,
                heavy.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(ineligibleHeavy));

            var selectResult = session.Execute(DirectorCommand.SelectMinionBattleRemoval(legal));
            Assert.That(selectResult.Accepted, Is.True);
            Assert.That(selectResult.Events, Has.Count.EqualTo(1));
            Assert.That(selectResult.Events[0].Kind,
                Is.EqualTo(RoundResolutionEventKind.MinionBattleSelectionChanged));
            Assert.That(selectResult.Events[0].MinionBattleChoiceCommandKind,
                Is.EqualTo(MinionBattleChoiceCommandKind.Select));
            var selected = session.Projection.PendingMinionBattleChoice;
            Assert.That(selected.SelectedMinionIds, Is.EqualTo(new[] { legal.MinionId }));
            Assert.That(selected.CanConfirm, Is.True);
            Assert.That(selected.LegalSelectMinionIds, Is.Empty);

            var other = choice.Candidates.First(value => !value.MinionId.Equals(legal.MinionId));
            var overCount = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.ChoosingSide,
                other.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(overCount));

            var deselect = selected.DeselectInvocations.Single(value => value.MinionId.Equals(legal.MinionId));
            var deselectResult = session.Execute(DirectorCommand.DeselectMinionBattleRemoval(deselect));
            Assert.That(deselectResult.Accepted, Is.True);
            Assert.That(deselectResult.Events, Has.Count.EqualTo(1));
            Assert.That(deselectResult.Events[0].MinionBattleChoiceCommandKind,
                Is.EqualTo(MinionBattleChoiceCommandKind.Deselect));
            Assert.That(session.Projection.PendingMinionBattleChoice.SelectedMinionIds, Is.Empty);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(legal)).Accepted, Is.True);

            var beforeCount = session.Projection.Minions.Count;
            var confirm = session.Execute(DirectorCommand.ConfirmMinionBattleRemovals(
                session.Projection.PendingMinionBattleChoice.ConfirmInvocation));

            Assert.That(confirm.Accepted, Is.True);
            Assert.That(confirm.Events.Select(value => value.Kind), Is.EqualTo(new[]
            {
                RoundResolutionEventKind.MinionBattleMinionRemoved,
                RoundResolutionEventKind.EndOfRoundStageResolved
            }));
            Assert.That(confirm.Events[0].MinionId, Is.EqualTo(legal.MinionId));
            Assert.That(session.Projection.Minions, Has.Count.EqualTo(beforeCount - 1));
            Assert.That(session.Projection.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(legal.MinionId));
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
        }

        [Test]
        public void PendingChoice_RoundTripsAndRebuildsByteIdenticallyThroughRestoreAndUndoPrefixes()
        {
            var session = CreateSession(RemoveOneBlueRanged);
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var select = session.Projection.PendingMinionBattleChoice.SelectInvocations[0];
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(select)).Accepted, Is.True);
            var selectedSnapshot = session.CaptureSnapshot();
            var selectedCommands = session.Projection.AcceptedCommands.ToArray();

            var replayed = CreateSession(RemoveOneBlueRanged);
            foreach (var command in selectedCommands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True);
            }

            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(selectedSnapshot));
            AssertChoicesEquivalent(
                session.Projection.PendingMinionBattleChoice,
                replayed.Projection.PendingMinionBattleChoice);

            var restored = CreateSession(RemoveOneBlueRanged);
            Assert.That(restored.RestoreSnapshot(selectedSnapshot).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(selectedSnapshot));
            AssertChoicesEquivalent(
                session.Projection.PendingMinionBattleChoice,
                restored.Projection.PendingMinionBattleChoice);

            Assert.That(session.Execute(DirectorCommand.ConfirmMinionBattleRemovals(
                session.Projection.PendingMinionBattleChoice.ConfirmInvocation)).Accepted, Is.True);
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(selectedSnapshot));
            AssertChoicesEquivalent(
                restored.Projection.PendingMinionBattleChoice,
                session.Projection.PendingMinionBattleChoice);

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.PendingMinionBattleChoice.SelectedMinionIds, Is.Empty);
            Assert.That(session.Projection.CurrentEndOfRoundStage, Is.EqualTo(EndOfRoundStage.MinionBattle));
        }

        [Test]
        public void AutomaticRemoval_EmitsHeavyLastAndDefersRecordedLanePushUntilAfterCleanup()
        {
            var session = CreateSession(KeepOneBlueOfEachType);
            AdvanceToEndOfRound(session);

            var battle = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(battle.Accepted, Is.True);
            var removals = battle.Events
                .Where(value => value.Kind == RoundResolutionEventKind.MinionBattleMinionRemoved)
                .ToArray();
            Assert.That(removals, Has.Length.EqualTo(3));
            var removedTypes = removals.Select(value => session.Projection.Setup.Minions
                .Single(minion => minion.MinionId.Equals(value.MinionId)).MinionType).ToArray();
            Assert.That(removedTypes.Take(2), Has.None.EqualTo(StandardMinionType.Heavy));
            Assert.That(removedTypes.Last(), Is.EqualTo(StandardMinionType.Heavy));
            Assert.That(battle.Events.Select(value => value.Kind), Does.Contain(RoundResolutionEventKind.LanePushRequested));
            Assert.That(session.Projection.PendingLanePushRequest, Is.Not.Null);
            Assert.That(session.Projection.PendingLanePushRequest.LosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(session.Projection.PendingLanePushRequest.ConsumptionCheckpoint,
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));

            var requestId = session.Projection.PendingLanePushRequest.RequestId;
            var requestSnapshot = session.CaptureSnapshot();
            var restored = CreateSession(KeepOneBlueOfEachType);
            Assert.That(restored.RestoreSnapshot(requestSnapshot).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(requestSnapshot));
            Assert.That(restored.Projection.PendingLanePushRequest.RequestId, Is.EqualTo(requestId));
            Assert.That(restored.Projection.PendingLanePushRequest.CauseResolutionId,
                Is.EqualTo(session.Projection.PendingLanePushRequest.CauseResolutionId));
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush));
            Assert.That(session.Projection.PendingLanePushRequest.RequestId, Is.EqualTo(requestId));
            AssertAtomicRejection(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(session.Projection.PendingLanePushRequest.RequestId, Is.EqualTo(requestId));
        }

        // -----------------------------------------------------------------------------------------
        // Milestone 6.1k, finding F613-13: coverage proportionate to the subsystem's statefulness.
        //
        // The four tests above drive a whole MatchSession over the configured setup, which has exactly
        // one Heavy per side. That makes one live branch of PendingMinionBattleChoice unreachable from
        // any of them: with a single Heavy, a pending choice can never legally offer it, because the
        // choice only appears when the required count is below the non-Heavy count. The interactive
        // Heavy-last path therefore had no coverage at all.
        //
        // The cases below reach it two ways - directly against the pure choice type, and through the
        // round authority over a synthetic Battle Zone whose minion shape is chosen per case.
        // -----------------------------------------------------------------------------------------

        [Test]
        public void RemovalChoice_RequiresMoreUniqueCandidatesThanItRemoves()
        {
            // Exact-count boundary. A choice that removes every candidate is not a choice; the
            // authority resolves that shape automatically instead, which the plan-level cases below
            // prove. One candidate above the required count is the first shape that is a real choice.
            Assert.Throws<ArgumentException>(() => NewChoice("MM", 2),
                "Candidates equal to the removal count leave nothing to decide.");
            Assert.Throws<ArgumentException>(() => NewChoice("MM", 3));
            Assert.DoesNotThrow(() => NewChoice("MMM", 2));

            Assert.Throws<ArgumentOutOfRangeException>(() => NewChoice("MMM", 0));
            Assert.Throws<ArgumentException>(() => new PendingMinionBattleChoice(
                ResolutionId,
                ChoiceId,
                LosingTeamId,
                TeamSide.Blue,
                BattleZoneId,
                1,
                new[]
                {
                    new MinionBattleCandidate(MinionAt(1), StandardMinionType.Melee),
                    new MinionBattleCandidate(MinionAt(1), StandardMinionType.Ranged)
                },
                Array.Empty<MinionId>()),
                "Duplicate candidate identities are not two candidates.");
        }

        [Test]
        public void RemovalChoice_BoundsSelectionToTheExactRequiredCount()
        {
            var choice = NewChoice("MMMM", 2);

            Assert.That(choice.CanConfirm, Is.False);
            Assert.That(choice.WithSelected(new[] { MinionAt(1) }).CanConfirm, Is.False);
            Assert.That(choice.WithSelected(new[] { MinionAt(1), MinionAt(2) }).CanConfirm, Is.True);
            Assert.That(choice.WithSelected(new[] { MinionAt(1), MinionAt(2) }).LegalSelectMinionIds, Is.Empty,
                "At the exact count there is nothing further to select.");

            Assert.Throws<ArgumentException>(() =>
                choice.WithSelected(new[] { MinionAt(1), MinionAt(2), MinionAt(3) }),
                "Selecting beyond the required count is not a selection the rules allow.");
            Assert.Throws<ArgumentException>(() => choice.WithSelected(new[] { MinionAt(1), MinionAt(1) }),
                "The same minion cannot be removed twice.");
            Assert.Throws<ArgumentException>(() => choice.WithSelected(new[] { new MinionId("not-a-candidate") }));
        }

        [Test]
        public void RemovalChoice_OffersHeavyOnlyAfterEveryNonHeavyIsSelected()
        {
            // Two Heavies and a deficit that must reach one of them. The configured setup cannot
            // produce this shape, so no existing fixture exercised the escalation below.
            var choice = NewChoice("MRHH", 3);

            Assert.That(choice.LegalSelectMinionIds, Is.EqualTo(new[] { MinionAt(1), MinionAt(2) }),
                "While a non-Heavy is unselected, no Heavy is a legal removal.");
            Assert.That(choice.SelectInvocations.Select(value => value.MinionId),
                Is.EqualTo(choice.LegalSelectMinionIds));

            var nonHeavySelected = choice.WithSelected(new[] { MinionAt(1), MinionAt(2) });
            Assert.That(nonHeavySelected.LegalSelectMinionIds, Is.EqualTo(new[] { MinionAt(3), MinionAt(4) }),
                "Once every non-Heavy is selected, and only then, the Heavies become legal.");
            Assert.That(nonHeavySelected.LegalDeselectMinionIds, Is.EqualTo(new[] { MinionAt(1), MinionAt(2) }));
            Assert.That(nonHeavySelected.CanConfirm, Is.False);

            var complete = nonHeavySelected.WithSelected(new[] { MinionAt(1), MinionAt(2), MinionAt(3) });
            Assert.That(complete.CanConfirm, Is.True);
            Assert.That(complete.LegalSelectMinionIds, Is.Empty);
            Assert.That(complete.LegalDeselectMinionIds, Is.EqualTo(new[] { MinionAt(3) }),
                "Heavy-last runs backwards too: the Heavy is the only removal that may be taken back " +
                "while non-Heavies remain selected.");

            Assert.Throws<ArgumentException>(() => choice.WithSelected(new[] { MinionAt(3) }),
                "A Heavy selected beside an unselected non-Heavy violates Heavy-last ordering.");
            Assert.Throws<ArgumentException>(() => choice.WithSelected(new[] { MinionAt(1), MinionAt(3) }));
        }

        [Test]
        public void RemovalChoice_ProjectsOrdinalOrderRegardlessOfCandidateInputOrder()
        {
            var ordered = NewChoice("MHRM", 2);
            var shuffled = new PendingMinionBattleChoice(
                ResolutionId,
                ChoiceId,
                LosingTeamId,
                TeamSide.Blue,
                BattleZoneId,
                2,
                CandidatesFor("MHRM").Reverse(),
                Array.Empty<MinionId>());

            AssertChoicesEquivalent(ordered, shuffled);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void RemovalChoice_PreservesItsInvariantsAcrossBoundedRandomShapes(int seed)
        {
            var random = new Random(seed);
            for (var iteration = 0; iteration < 40; iteration++)
            {
                var candidateCount = random.Next(2, 8);
                var heavyCount = random.Next(0, candidateCount + 1);
                var required = random.Next(1, candidateCount);
                var shape = BuildShape(candidateCount, heavyCount, random);
                var context = $"seed {seed}, iteration {iteration}, shape '{shape}', required {required}";

                var choice = NewChoice(shape, required);
                Assert.That(choice.Candidates.Select(value => value.MinionId.Value),
                    Is.Ordered.Using<string>(StringComparer.Ordinal), context);

                // Take the legal selections one at a time, as the authority requires. Reaching the
                // required count in exactly that many steps is the property; violating Heavy-last on
                // the way would throw inside WithSelected.
                var current = choice;
                var steps = 0;
                var selected = new List<MinionId>();
                while (!current.CanConfirm)
                {
                    Assert.That(current.LegalSelectMinionIds, Is.Not.Empty, context);
                    Assert.That(current.SelectInvocations.Select(value => value.MinionId),
                        Is.EqualTo(current.LegalSelectMinionIds), context);

                    var unselectedNonHeavyRemains = current.Candidates.Any(candidate =>
                        candidate.MinionType != StandardMinionType.Heavy &&
                        !current.SelectedMinionIds.Contains(candidate.MinionId));
                    if (unselectedNonHeavyRemains)
                    {
                        Assert.That(
                            current.LegalSelectMinionIds.All(id => current.Candidates
                                .Single(candidate => candidate.MinionId.Equals(id))
                                .MinionType != StandardMinionType.Heavy),
                            Is.True,
                            $"Heavy offered while a non-Heavy was unselected: {context}");
                    }

                    var before = current;
                    var next = current.LegalSelectMinionIds[random.Next(current.LegalSelectMinionIds.Count)];
                    selected.Add(next);
                    current = current.WithSelected(selected);
                    steps++;

                    // Deselecting the step just taken returns the exact previous projection.
                    Assert.That(current.LegalDeselectMinionIds, Does.Contain(next), context);
                    AssertChoicesEquivalent(before, current.WithSelected(selected.Take(steps - 1)));
                    Assert.That(steps, Is.LessThanOrEqualTo(required), context);
                }

                Assert.That(steps, Is.EqualTo(required), context);
                Assert.That(current.LegalSelectMinionIds, Is.Empty, context);
                Assert.That(current.SelectedMinionIds.Select(value => value.Value),
                    Is.Ordered.Using<string>(StringComparer.Ordinal), context);
            }
        }

        [Test]
        public void BattleZonePlan_RemovesEveryLosingMinionHeavyLastWhenTheDeficitTakesThemAll()
        {
            var fixture = CreateBattleZoneFixture("MMMMM", "MH");

            var result = AdvanceToMinionBattle(fixture);

            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingMinionBattleChoice, Is.Null,
                "A deficit that takes every candidate leaves nothing to choose.");
            Assert.That(RemovedTypes(fixture, result), Is.EqualTo(new[]
            {
                StandardMinionType.Melee,
                StandardMinionType.Heavy
            }));
            Assert.That(fixture.Authority.PendingLanePushRequest, Is.Not.Null,
                "Clearing the losing side out of the Battle Zone records a Lane-Push request.");
            Assert.That(fixture.Authority.PendingLanePushRequest.LosingSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void BattleZonePlan_RemovesExactlyTheNonHeaviesWhenTheDeficitEqualsTheirCount()
        {
            var fixture = CreateBattleZoneFixture("MMMMM", "MRH");

            var result = AdvanceToMinionBattle(fixture);

            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingMinionBattleChoice, Is.Null,
                "When the deficit is exactly the non-Heavy count, Heavy-last determines the removals " +
                "without a choice.");
            Assert.That(RemovedTypes(fixture, result), Is.EqualTo(new[]
            {
                StandardMinionType.Melee,
                StandardMinionType.Ranged
            }));
            Assert.That(fixture.Board.Minions.Count(minion => minion.TeamSide == TeamSide.Blue), Is.EqualTo(1));
            Assert.That(fixture.Authority.PendingLanePushRequest, Is.Null,
                "The surviving Heavy keeps the losing side in the Battle Zone, so no Lane Push is due.");
        }

        [Test]
        public void BattleZonePlan_PausesForAChoiceWhoseLegalRemovalsEscalateToAHeavy()
        {
            // required 2, one non-Heavy, two Heavies: the choice must reach a Heavy, and only after
            // the non-Heavy is taken. No configured setup can produce this shape.
            var fixture = CreateBattleZoneFixture("MMMMM", "MHH");

            var paused = AdvanceToMinionBattle(fixture);

            Assert.That(paused.Accepted, Is.True);
            var choice = fixture.Authority.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.RequiredRemovalCount, Is.EqualTo(2));
            Assert.That(choice.ChoosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(TypesOf(fixture, choice.LegalSelectMinionIds),
                Is.EqualTo(new[] { StandardMinionType.Melee }),
                "Only the non-Heavy is legal first.");

            var nonHeavy = choice.SelectInvocations.Single();
            Assert.That(fixture.Authority.TryAnswerMinionBattleChoice(nonHeavy).Accepted, Is.True);

            var escalated = fixture.Authority.PendingMinionBattleChoice;
            Assert.That(TypesOf(fixture, escalated.LegalSelectMinionIds),
                Is.EqualTo(new[] { StandardMinionType.Heavy, StandardMinionType.Heavy }),
                "With every non-Heavy selected, both Heavies become legal removals.");
            Assert.That(escalated.CanConfirm, Is.False);

            var heavy = escalated.SelectInvocations.First();
            Assert.That(fixture.Authority.TryAnswerMinionBattleChoice(heavy).Accepted, Is.True);
            Assert.That(fixture.Authority.PendingMinionBattleChoice.CanConfirm, Is.True);

            var confirm = fixture.Authority.TryAnswerMinionBattleChoice(
                fixture.Authority.PendingMinionBattleChoice.ConfirmInvocation);

            Assert.That(confirm.Accepted, Is.True);
            Assert.That(RemovedTypes(fixture, confirm), Is.EqualTo(new[]
            {
                StandardMinionType.Melee,
                StandardMinionType.Heavy
            }), "Confirmed removals still emit Heavy last.");
            Assert.That(fixture.Authority.PendingMinionBattleChoice, Is.Null);
            Assert.That(fixture.Authority.PendingLanePushRequest, Is.Null,
                "One Heavy survives in the Battle Zone, so no Lane Push is due.");
        }

        [Test]
        public void BattleZonePlan_ExcludesHeavyFromAChoiceThatCannotReachIt()
        {
            var fixture = CreateBattleZoneFixture("MMMMM", "MMMH");

            Assert.That(AdvanceToMinionBattle(fixture).Accepted, Is.True);

            var choice = fixture.Authority.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.RequiredRemovalCount, Is.EqualTo(1));
            Assert.That(choice.Candidates, Has.Count.EqualTo(4));
            Assert.That(TypesOf(fixture, choice.LegalSelectMinionIds),
                Has.None.EqualTo(StandardMinionType.Heavy));
            Assert.That(choice.LegalSelectMinionIds, Has.Count.EqualTo(3));
        }

        [Test]
        public void BattleZonePlan_CompletesWithoutRemovalWhenCountsMatchOrTheLosingSideIsAbsent()
        {
            var equal = CreateBattleZoneFixture("MRH", "MRH");
            var equalResult = AdvanceToMinionBattle(equal);

            Assert.That(equalResult.Accepted, Is.True);
            Assert.That(equal.Authority.PendingMinionBattleChoice, Is.Null);
            Assert.That(equal.Authority.PendingLanePushRequest, Is.Null);
            Assert.That(RemovedTypes(equal, equalResult), Is.Empty);
            Assert.That(equal.Board.Minions.Count, Is.EqualTo(6));

            // A side with nothing in the Battle Zone has a deficit but no candidates: the required
            // count clamps to zero rather than removing minions that are not there.
            var absent = CreateBattleZoneFixture("MM", string.Empty);
            var absentResult = AdvanceToMinionBattle(absent);

            Assert.That(absentResult.Accepted, Is.True);
            Assert.That(absent.Authority.PendingMinionBattleChoice, Is.Null);
            Assert.That(absent.Authority.PendingLanePushRequest, Is.Null,
                "No minion was removed, so nothing was cleared and no Lane Push is due.");
            Assert.That(RemovedTypes(absent, absentResult), Is.Empty);
            Assert.That(absent.Board.Minions.Count, Is.EqualTo(2));
        }

        [Test]
        public void BattleZonePlan_ResolvesTheSameShapeIdenticallyEveryTime()
        {
            Assert.That(AutomaticResolutionFacts("MMMMM", "MRH"), Is.EqualTo(AutomaticResolutionFacts("MMMMM", "MRH")));
            Assert.That(AutomaticResolutionFacts("MMMMM", "MH"), Is.EqualTo(AutomaticResolutionFacts("MMMMM", "MH")));
            Assert.That(AutomaticResolutionFacts("MMMMM", "MRH"),
                Is.Not.EqualTo(AutomaticResolutionFacts("MMMMM", "MH")),
                "Two different shapes must not produce the same record, or the comparison proves nothing.");
        }

        private static IReadOnlyList<string> AutomaticResolutionFacts(string redShape, string blueShape)
        {
            var fixture = CreateBattleZoneFixture(redShape, blueShape);
            var result = AdvanceToMinionBattle(fixture);
            Assert.That(result.Accepted, Is.True);
            return result.Events
                .Select(value => $"{value.Kind}:{value.MinionId}:{value.EndOfRoundStage}:{value.EndOfRoundOutcomeKind}")
                .ToArray();
        }

        private static MatchSession CreateSession(
            Func<IReadOnlyList<QuickGameMinionSetupState>, IEnumerable<QuickGameMinionSetupState>> selectMinions)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                selectMinions(fullSetup.Minions));
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static IEnumerable<QuickGameMinionSetupState> RemoveOneBlueRanged(
            IReadOnlyList<QuickGameMinionSetupState> minions)
        {
            var removed = minions.First(value =>
                value.TeamSide == TeamSide.Blue && value.MinionType == StandardMinionType.Ranged);
            return minions.Where(value => !value.MinionId.Equals(removed.MinionId));
        }

        private static IEnumerable<QuickGameMinionSetupState> KeepOneBlueOfEachType(
            IReadOnlyList<QuickGameMinionSetupState> minions)
        {
            var retainedBlue = minions
                .Where(value => value.TeamSide == TeamSide.Blue)
                .GroupBy(value => value.MinionType)
                .Select(group => group.OrderBy(value => value.MinionId.Value, StringComparer.Ordinal).First())
                .Select(value => value.MinionId)
                .ToArray();
            return minions.Where(value =>
                value.TeamSide == TeamSide.Red || retainedBlue.Contains(value.MinionId));
        }

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);
                Assert.That(session.Execute(DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }
        }

        private static void CommitUnequalCards(MatchSession session)
        {
            var red = EligibleCards(session, TeamSide.Red).OrderBy(value => Initiative(session, value)).First();
            var blue = EligibleCards(session, TeamSide.Blue).OrderByDescending(value => Initiative(session, value)).First();
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
        }

        private static IEnumerable<CardInstance> EligibleCards(MatchSession session, TeamSide side)
        {
            var heroId = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
            return session.Projection.Cards.Where(value =>
                value.OwnerId.Equals(heroId) && value.State.Location == CardLocation.Hand);
        }

        private static int Initiative(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).Initiative.Value;

        private static void AssertAtomicRejection(MatchSession session, DirectorCommand command)
        {
            var snapshot = session.CaptureSnapshot();
            var history = session.Projection.AcceptedCommands.ToArray();
            var choice = session.Projection.PendingMinionBattleChoice;
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(session.Projection.AcceptedCommands, Is.EqualTo(history));
            if (choice != null)
            {
                AssertChoicesEquivalent(choice, session.Projection.PendingMinionBattleChoice);
            }
        }

        // ---- Pure choice-shape helpers -----------------------------------------------------------
        //
        // A shape is a compact string: 'M' melee, 'R' ranged, 'H' heavy. Candidate identities are
        // assigned in shape order and sort ordinally, so `MinionAt(1)` is the first character's minion
        // and expectations stay readable.

        private static readonly EndOfRoundResolutionId ResolutionId = new EndOfRoundResolutionId("round-1-minion-battle");
        private static readonly EndOfRoundChoiceId ChoiceId = new EndOfRoundChoiceId("round-1-minion-battle-removals");
        private static readonly TeamId LosingTeamId = new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue);
        private static readonly ZoneId BattleZoneId = new ZoneId("synthetic-battle-zone");

        private static MinionId MinionAt(int position) => new MinionId($"minion-{position:00}");

        private static StandardMinionType TypeOfSymbol(char symbol)
        {
            switch (symbol)
            {
                case 'H': return StandardMinionType.Heavy;
                case 'R': return StandardMinionType.Ranged;
                case 'M': return StandardMinionType.Melee;
                default: throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Unknown minion shape symbol.");
            }
        }

        private static IReadOnlyList<MinionBattleCandidate> CandidatesFor(string shape) =>
            shape.Select((symbol, index) =>
                new MinionBattleCandidate(MinionAt(index + 1), TypeOfSymbol(symbol))).ToArray();

        private static PendingMinionBattleChoice NewChoice(string shape, int required) =>
            new PendingMinionBattleChoice(
                ResolutionId,
                ChoiceId,
                LosingTeamId,
                TeamSide.Blue,
                BattleZoneId,
                required,
                CandidatesFor(shape),
                Array.Empty<MinionId>());

        private static string BuildShape(int candidateCount, int heavyCount, Random random)
        {
            var symbols = Enumerable.Repeat('H', heavyCount)
                .Concat(Enumerable.Range(0, candidateCount - heavyCount)
                    .Select(index => random.Next(2) == 0 ? 'M' : 'R'))
                .ToArray();

            for (var index = symbols.Length - 1; index > 0; index--)
            {
                var swap = random.Next(index + 1);
                var held = symbols[index];
                symbols[index] = symbols[swap];
                symbols[swap] = held;
            }

            return new string(symbols);
        }

        // ---- Synthetic Battle-Zone authority fixture ----------------------------------------------
        //
        // MinionBattleRules.Plan is internal and there is no InternalsVisibleTo, so it is exercised
        // through its real caller. This composes the round authority over a one-zone map flagged as the
        // starting Battle Zone, which is far cheaper than a MatchSession and, unlike the configured
        // setup, can express any minion shape - including the two-Heavy shapes that make the
        // interactive Heavy-last path reachable.

        private sealed class BattleZoneFixture
        {
            public BattleZoneFixture(
                RoundResolutionAuthority authority,
                AdjacentMoveAuthority board,
                IReadOnlyList<QuickGameMinionSetupState> setupMinions,
                CardInstance red,
                CardInstance blue)
            {
                Authority = authority;
                Board = board;
                SetupMinions = setupMinions;
                Red = red;
                Blue = blue;
            }

            public RoundResolutionAuthority Authority { get; }
            public AdjacentMoveAuthority Board { get; }

            /// <summary>The shape as built. A removed minion is gone from the board, so its intrinsic
            /// type is recovered from here rather than from live state.</summary>
            public IReadOnlyList<QuickGameMinionSetupState> SetupMinions { get; }
            public CardInstance Red { get; }
            public CardInstance Blue { get; }
        }

        private static BattleZoneFixture CreateBattleZoneFixture(string redShape, string blueShape)
        {
            var participants = PlaceholderOneVsOneParticipants.Create();
            var mapId = new MapId("synthetic-battle-zone-map");
            var total = redShape.Length + blueShape.Length;
            var coordinates = Enumerable.Range(0, Math.Max(total, 1))
                .Select(index => new HexCoordinate(index, 0))
                .ToArray();
            var zone = new BoardMapZone(BattleZoneId, "Synthetic Battle Zone", "lane", null, false, true);
            var spaces = coordinates
                .Select(coordinate => new BoardMapSpace(
                    SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, zone.Id))
                .ToArray();
            var map = new BoardMapDefinition(
                mapId, 1, "Synthetic Battle Zone Map", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());

            var minions = new List<QuickGameMinionSetupState>();
            var position = 0;
            foreach (var entry in new[]
                     {
                         new { Shape = redShape, Side = TeamSide.Red },
                         new { Shape = blueShape, Side = TeamSide.Blue }
                     })
            {
                var team = participants.Teams.Single(value => value.Side == entry.Side);
                foreach (var symbol in entry.Shape)
                {
                    var coordinate = coordinates[position];
                    minions.Add(new QuickGameMinionSetupState(
                        new MinionId($"{entry.Side.ToString().ToLowerInvariant()}-minion-{position:00}"),
                        team.Id,
                        entry.Side,
                        TypeOfSymbol(symbol),
                        new ConfiguredSpawnPointReference(mapId, new SpawnPointId($"spawn-{position:00}")),
                        new ConfiguredSpaceReference(mapId, SpaceIdFor(coordinate)),
                        coordinate));
                    position++;
                }
            }

            var board = new AdjacentMoveAuthority(
                map, Array.Empty<HeroOccupancy>(), Array.Empty<ObstacleOccupancy>(), minions);

            var redDefinition = SyntheticDefinition("battle-red", 7);
            var blueDefinition = SyntheticDefinition("battle-blue", 4);
            var red = CardInstance.CreateInHand(
                new CardInstanceId("battle-red-card"),
                redDefinition.Reference,
                participants.GetHeroesForTeam(new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue)).Single().Id);
            var blue = CardInstance.CreateInHand(
                new CardInstanceId("battle-blue-card"),
                blueDefinition.Reference,
                participants.GetHeroesForTeam(new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue)).Single().Id);

            var authority = new RoundResolutionAuthority(
                new[] { redDefinition, blueDefinition },
                new[] { red, blue },
                participants,
                new TieBreakerState(TeamSide.Red),
                1,
                new RoundResolutionComposition(
                    null, board, null, EndOfRoundFixtureCapabilities.ConfiguredMatch(), null));

            return new BattleZoneFixture(authority, board, minions, red, blue);
        }

        private static SpaceId SpaceIdFor(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

        private static CardDefinition SyntheticDefinition(string id, int initiative) =>
            new CardDefinition(
                new CardDefinitionId(id),
                "minion-battle-test-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture);

        /// <summary>Plays one round to its end so the Minion Battle stage is the next thing to resolve,
        /// then resolves it.</summary>
        private static RoundResolutionResult AdvanceToMinionBattle(BattleZoneFixture fixture)
        {
            var authority = fixture.Authority;
            Assert.That(authority.TryCommitCard(fixture.Red.OwnerId, fixture.Red.Id).Accepted, Is.True);
            Assert.That(authority.TryCommitCard(fixture.Blue.OwnerId, fixture.Blue.Id).Accepted, Is.True);
            while (authority.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                Assert.That(authority.TryPerformHold(
                    authority.CurrentTurnAuthority.SelectedActor.CardInstanceId).Accepted, Is.True);
            }

            for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                Assert.That(authority.TryPass(fixture.Red.OwnerId).Accepted, Is.True);
                Assert.That(authority.TryPass(fixture.Blue.OwnerId).Accepted, Is.True);
            }

            Assert.That(authority.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(authority.CurrentEndOfRoundStage, Is.EqualTo(EndOfRoundStage.MinionBattle));
            return authority.TryAdvanceEndOfRound();
        }

        /// <summary>The intrinsic types of the minions this result removed, in emission order, so
        /// Heavy-last is asserted as an ordering rather than as a set.</summary>
        private static IReadOnlyList<StandardMinionType> RemovedTypes(
            BattleZoneFixture fixture,
            RoundResolutionResult result) =>
            result.Events
                .Where(value => value.Kind == RoundResolutionEventKind.MinionBattleMinionRemoved)
                .Select(value => TypeOf(fixture, value.MinionId))
                .ToArray();

        private static IReadOnlyList<StandardMinionType> TypesOf(
            BattleZoneFixture fixture,
            IEnumerable<MinionId> minionIds) =>
            minionIds.Select(id => TypeOf(fixture, id)).ToArray();

        private static StandardMinionType TypeOf(BattleZoneFixture fixture, MinionId minionId) =>
            fixture.SetupMinions.Single(minion => minion.MinionId.Equals(minionId)).MinionType;

        private static void AssertChoicesEquivalent(
            PendingMinionBattleChoice expected,
            PendingMinionBattleChoice actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.ResolutionId, Is.EqualTo(expected.ResolutionId));
            Assert.That(actual.ChoiceId, Is.EqualTo(expected.ChoiceId));
            Assert.That(actual.LosingTeamId, Is.EqualTo(expected.LosingTeamId));
            Assert.That(actual.ChoosingSide, Is.EqualTo(expected.ChoosingSide));
            Assert.That(actual.RequiredRemovalCount, Is.EqualTo(expected.RequiredRemovalCount));
            Assert.That(actual.Candidates, Is.EqualTo(expected.Candidates));
            Assert.That(actual.SelectedMinionIds, Is.EqualTo(expected.SelectedMinionIds));
            Assert.That(actual.LegalSelectMinionIds, Is.EqualTo(expected.LegalSelectMinionIds));
            Assert.That(actual.LegalDeselectMinionIds, Is.EqualTo(expected.LegalDeselectMinionIds));
            Assert.That(actual.SelectInvocations, Is.EqualTo(expected.SelectInvocations));
            Assert.That(actual.DeselectInvocations, Is.EqualTo(expected.DeselectInvocations));
            Assert.That(actual.ConfirmInvocation, Is.EqualTo(expected.ConfirmInvocation));
        }
    }
}
