using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.17. The 9.12 playtest's Charged Runestone, replayed in a whole configured match:
    /// Snorri inscribes the runes on round 1's first turn, the round runs out, and on round 2's first
    /// turn he plays the face with Bain in Radius - the host log's line-up and spaces, (-2, 1) and
    /// (-1, 2). Rune 4 sits below slot 2 in every case (the log's round-2 Rune Sigils repeated on a
    /// different hero, which is rune 4's clause); the rune below slot 1 is not in the log, so each of
    /// the other three is played.
    ///
    /// <para>The finding: across the round boundary the slot-1 rune is active again, and Charged
    /// Runestone asks for its enemy hero exactly when rune 1, 2 or 4 is - the three runes the face
    /// prints a clause for. Under rune 3 it asks nothing and discards nothing, the play the log shows.
    /// Inscribed Runebomb, which prints a clause for all four, asks under every rune.</para>
    /// </summary>
    public sealed class SnorriRoundTwoRuneTests
    {
        private static readonly HexCoordinate SnorriAt = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate BainAt = new HexCoordinate(-1, 2);

        /// <summary>
        /// The second three replay the log's round more closely: the minions stay on the board, and
        /// Snorri starts at Level 1 holding the coin for Level 2, buying it at round 1's end and
        /// upgrading his Green into Charged Runestone there, as the log's v113-v117 did.
        /// </summary>
        [TestCase(1, true, false)]
        [TestCase(2, true, false)]
        [TestCase(3, false, false)]
        [TestCase(1, true, true)]
        [TestCase(2, true, true)]
        [TestCase(3, false, true)]
        public void RoundTwoTurnOne_ChargedRunestone_AsksForItsTargetExactlyWhenItsPrintedRuneIsActive(
            int runeBelowSlotOne, bool expectsTarget, bool levelsUpAtRoundOnesEnd)
        {
            var run = new Run(SnorriGreenFaces.ChargedRunestone, runeBelowSlotOne, levelsUpAtRoundOnesEnd);
            run.ReachRoundTwoFace();
            RunesStayBelowTheirSlots(run, runeBelowSlotOne);

            var choice = Start(run);
            if (expectsTarget)
            {
                Assert.That(HeroCandidates(choice), Does.Contain(run.Bain),
                    $"with rune {runeBelowSlotOne} active, Charged Runestone asks for the enemy hero in Radius");
            }
            else
            {
                Assert.That(choice, Is.Null,
                    "rune 3 prints no Charged Runestone clause, so the play asks nothing");
                Assert.That(run.Session.Projection.PendingTurnRelease, Is.Not.Null,
                    "and resolves at once, as the log's v147 SkillUsed did");
            }
        }

        /// <summary>
        /// With no enemy hero in Radius the face is greyed under a printed rune (rule 14: its mandatory
        /// target has nothing to name) and offered as RULING 53's do-nothing play under rune 3.
        /// </summary>
        [TestCase(1, false)]
        [TestCase(3, true)]
        public void RoundTwoTurnOne_ChargedRunestone_WithNoEnemyInRadius_IsGreyedUnlessItsGateIsFalse(
            int runeBelowSlotOne, bool offered)
        {
            var run = new Run(SnorriGreenFaces.ChargedRunestone, runeBelowSlotOne, false,
                new HexCoordinate(-2, 6));
            run.ReachRoundTwoFace();
            Assert.That(Offer(run).IsAvailable, Is.EqualTo(offered));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void RoundTwoTurnOne_InscribedRunebomb_AsksForItsTargetUnderEveryRune(int runeBelowSlotOne)
        {
            var run = new Run(SnorriGreenFaces.InscribedRunebomb, runeBelowSlotOne, false);
            run.ReachRoundTwoFace();
            RunesStayBelowTheirSlots(run, runeBelowSlotOne);
            Assert.That(HeroCandidates(Start(run)), Does.Contain(run.Bain),
                $"with rune {runeBelowSlotOne} active, Inscribed Runebomb asks for the enemy hero in Radius");
        }

        private static void RunesStayBelowTheirSlots(Run run, int runeBelowSlotOne)
        {
            var projection = run.Session.Projection;
            Assert.That(projection.RoundIndex, Is.EqualTo(2));
            Assert.That(projection.TurnIndex, Is.EqualTo(1));
            Assert.That(run.Session.aggregate.Round.Markers.Attachments
                    .Where(marker => marker.TurnSlotIndex.HasValue)
                    .OrderBy(marker => marker.TurnSlotIndex.Value)
                    .Select(marker => marker.Definition.Reference.DefinitionId.Value),
                Is.EqualTo(run.ExpectedSlots.Select(rune => "snorri-rune-" + rune)),
                "the four runes stay below the slots round 1 put them under");
            Assert.That(SnorriComponentContent.IsRuneActive(
                    run.Session.aggregate.Round.Markers, projection.TurnIndex, runeBelowSlotOne), Is.True,
                "the rune inscribed below slot 1 in round 1 is active on round 2's first turn");
        }

        private static ActionOffer Offer(Run run) =>
            run.Session.Projection.AvailableTurnActions.Single(value =>
                value.SourceCardInstanceId.Equals(run.Face) &&
                value.InvocationContext == ActionInvocationContext.PrintedPrimary);

        private static PendingEffectChoice Start(Run run)
        {
            var offer = Offer(run);
            Assert.That(offer.IsAvailable, Is.True, "the face is offered");
            var started = run.Session.Execute(
                DirectorCommand.StartAction(offer.SourceCardInstanceId, offer.ActionDefinitionId));
            Assert.That(started.Accepted, Is.True, started.Detail);
            return run.Session.Projection.PendingEffectChoice;
        }

        private static HeroId[] HeroCandidates(PendingEffectChoice choice) =>
            choice?.Candidates
                .Where(value => value.Value.Kind == EffectValueKind.Hero)
                .Select(value => value.Value.HeroId)
                .ToArray() ?? Array.Empty<HeroId>();

        private sealed class Run
        {
            private readonly int runeBelowSlotOne;
            private readonly HeroId snorri;
            private readonly CardInstanceId silver;
            private int snorriCommitments;

            internal MatchSession Session { get; }
            internal HeroId Bain { get; }
            internal CardInstanceId Face { get; }

            internal IReadOnlyList<int> ExpectedSlots => new[] { runeBelowSlotOne, 4 }
                .Concat(Enumerable.Range(1, 3).Where(rune => rune != runeBelowSlotOne))
                .ToArray();

            internal Run(
                CardDefinitionId face, int runeBelowSlotOne, bool levelsUpAtRoundOnesEnd,
                HexCoordinate? bainAt = null)
            {
                this.runeBelowSlotOne = runeBelowSlotOne;
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                RegisteredHero Hero(string id) =>
                    QuickGameSetupConfiguration.DealtHeroes.Single(value => value.ContentId.Value == id);
                var lineup = new HeroLineup(
                    new[] { Hero("snorri"), Hero("trinkets"), Hero("whisper") },
                    new[] { Hero("bain"), Hero("mortimer"), Hero("brogan") });
                var format = MatchFormat.ThreeVersusThreeQuick;
                var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                    format, lineup, TeamSide.Red);
                var setup = QuickGameSetupAuthority.Create(
                    HeroLineupComposition.Compose(format, lineup), configuration.Participants, map,
                    configuration.TieBreakerState);
                snorri = setup.Heroes.Single(value => value.ContentId.Value == "snorri").HeroId;
                Bain = setup.Heroes.Single(value => value.ContentId.Value == "bain").HeroId;

                // The face takes the place of the starting Green in hand, at the Level p.15 reaches its
                // Tier by: the log's Snorri upgraded his Green into Charged Runestone at round 1's end.
                // The level-up variant leaves it in the deck and has him buy Level 2 and upgrade into it
                // at round 1's end instead.
                CardDefinition Definition(CardInstance card) => configuration.CardDefinitions
                    .Single(value => value.Reference.Equals(card.DefinitionReference));
                var owned = configuration.CardInstances.Where(card => card.OwnerId.Equals(snorri)).ToArray();
                var faceCard = owned.Single(card =>
                    card.DefinitionReference.DefinitionId.Equals(face));
                var startingGreen = owned.Single(card => card.State.Location == CardLocation.Hand &&
                    Definition(card).Progression.Color == CardColor.Green);
                if (!levelsUpAtRoundOnesEnd)
                {
                    Assert.That(CardLifecycleAuthority.TryReturnToDeck(startingGreen), Is.True);
                    Assert.That(CardLifecycleAuthority.TryTakeFromDeckIntoHand(faceCard), Is.True);
                }

                Face = faceCard.Id;
                silver = owned.Single(card =>
                    card.DefinitionReference.Equals(SnorriSilverFace.Silver.Reference)).Id;

                var far = map.Spaces
                    .Where(space => space.Kind == BoardMapSpaceKind.Playable &&
                        HexDistanceQuery.Calculate(space.Coordinate, SnorriAt) >= 8)
                    .Select(space => space.Coordinate)
                    .OrderBy(space => space.Q).ThenBy(space => space.R)
                    .ToList();
                var farChosen = new List<HexCoordinate>();
                foreach (var space in far)
                {
                    if (farChosen.All(other => HexDistanceQuery.Calculate(other, space) >= 3))
                    {
                        farChosen.Add(space);
                    }
                }

                var farIndex = 0;
                var at = setup.Heroes.ToDictionary(seat => seat.HeroId, seat =>
                    seat.HeroId.Equals(snorri) ? SnorriAt
                    : seat.HeroId.Equals(Bain) ? bainAt ?? BainAt
                    : farChosen[farIndex++]);
                ConfiguredSpaceReference SpaceAt(HexCoordinate coordinate) =>
                    new ConfiguredSpaceReference(map.Id,
                        map.Spaces.Single(space => space.Coordinate.Equals(coordinate)).Id);
                var staged = new QuickGameSetupState(
                    setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams,
                    setup.Heroes.Select(seat => new QuickGameHeroSetupState(
                        seat.HeroId, seat.ContentId, seat.DisplayName, seat.TeamId, seat.TeamSide,
                        seat.SpawnPoint, SpaceAt(at[seat.HeroId]), at[seat.HeroId])),
                    levelsUpAtRoundOnesEnd
                        ? setup.Minions.Where(minion => !at.Values.Contains(minion.Coordinate)).ToArray()
                        : Array.Empty<QuickGameMinionSetupState>());
                var progression = new MatchStagedProgression(
                    setup.Heroes.Select(seat => new KeyValuePair<HeroId, HeroLevel>(
                        seat.HeroId, new HeroLevel(!seat.HeroId.Equals(snorri) || levelsUpAtRoundOnesEnd ? 1
                            // p.15's table: Tier II from Level 2, Tier III from Level 5.
                            : Definition(faceCard).Progression.Tier == CardTier.III ? 5 : 2))),
                    setup.Heroes.Select(seat => new KeyValuePair<HeroId, int>(seat.HeroId,
                        seat.HeroId.Equals(snorri) && levelsUpAtRoundOnesEnd
                            ? HeroLevelSchedule.LevelUpCost(new HeroLevel(1))
                            : 0)));
                var scenario = new InitialScenarioDescription(
                    configuration.Participants, configuration.CardDefinitions,
                    configuration.CardInstances, configuration.TieBreakerState,
                    configuration.SetupSeed, map.Reference,
                    staged.Heroes.Select(seat => new HeroOccupancy(seat.HeroId, seat.Coordinate)),
                    Array.Empty<ObstacleOccupancy>());
                Session = new MatchSession(new MatchSessionInitialState(
                    scenario, staged, new[] { map }, null, null, progression));
            }

            /// <summary>
            /// Every other hero Holds whatever it commits; Snorri inscribes on round 1's first turn and
            /// Holds his other three; every other question takes its first legal answer.
            /// </summary>
            internal void ReachRoundTwoFace()
            {
                var inscribing = false;
                for (var step = 0; step < 800; step++)
                {
                    var projection = Session.Projection;
                    Assert.That(projection.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded));
                    DirectorCommand next = null;
                    if (projection.Phase == RoundResolutionPhase.AwaitingCommitments &&
                        projection.DecisionContract == null)
                    {
                        var asked = projection.Commitments.First(value =>
                            value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                        next = DirectorCommand.CommitCard(asked.HeroId, Commit(projection, asked.HeroId));
                    }
                    else if (projection.PendingTurnRelease != null && projection.DecisionContract == null)
                    {
                        next = DirectorCommand.ReleaseTurn();
                    }
                    else if (projection.IsTurnActive && projection.SelectedActor.IsDefined &&
                        projection.DecisionContract == null)
                    {
                        var selected = projection.SelectedActor.CardInstanceId;
                        if (selected.Equals(Face))
                        {
                            return;
                        }

                        if (selected.Equals(silver) && !inscribing)
                        {
                            var primary = projection.AvailableTurnActions.Single(offer =>
                                offer.SourceCardInstanceId.Equals(silver) &&
                                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
                            next = DirectorCommand.StartAction(
                                primary.SourceCardInstanceId, primary.ActionDefinitionId);
                            inscribing = true;
                        }
                        else if (projection.AvailableHoldOffer.HasValue &&
                            projection.AvailableHoldOffer.Value.IsAvailable)
                        {
                            next = DirectorCommand.PerformHold(selected);
                        }
                    }
                    else if (inscribing && projection.PendingEffectChoice != null &&
                        projection.PendingEffectChoice.ValueVocabulary ==
                            EffectChoiceValueVocabulary.RuneTurnSlot)
                    {
                        next = InscribeAnswer(projection);
                    }
                    else
                    {
                        // Round 1's level-up (p.15): the upgrade takes the face, as the log's took
                        // Charged Runestone. An upgrade option names the upgraded card and the card taken.
                        next = DeterministicMatchLoop.EnumerateLegalCommands(projection)
                            .FirstOrDefault(command =>
                                command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                                command.SystemTransitionChoice.OptionId.Value != null &&
                                command.SystemTransitionChoice.OptionId.Value.EndsWith(
                                    Face.Value, StringComparison.Ordinal));
                    }

                    if (next == null)
                    {
                        var legal = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                        Assert.That(legal, Is.Not.Empty, $"no legal command ({projection.Phase})");
                        next = legal[0];
                    }

                    var result = Session.Execute(next);
                    Assert.That(result.Accepted, Is.True, next.Kind + ": " + result.Detail + "; log: " +
                        string.Join(" | ", projection.MatchLog.Skip(Math.Max(0, projection.MatchLog.Count - 30))));
                }

                Assert.Fail("round 2's turn for the face was not reached");
            }

            private CardInstanceId Commit(MatchSessionProjection projection, HeroId owner)
            {
                var hand = projection.Cards
                    .Where(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand)
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
                if (!owner.Equals(snorri))
                {
                    return hand.First().Id;
                }

                var turn = snorriCommitments++;
                if (turn == 0) return silver;
                if (turn == 4) return Face;
                return hand.First(card => !card.Id.Equals(Face) && !card.Id.Equals(silver)).Id;
            }

            /// <summary>Slot 1 the case's rune, slot 2 rune 4, slots 3 and 4 the other two.</summary>
            private DirectorCommand InscribeAnswer(MatchSessionProjection projection)
            {
                var runes = new[] { runeBelowSlotOne, 4 }
                    .Concat(Enumerable.Range(1, 3).Where(rune => rune != runeBelowSlotOne))
                    .ToArray();
                var choice = projection.PendingEffectChoice;
                var chosen = choice.SelectedCandidates.Select(candidate => candidate.Value).ToArray();
                var contract = projection.DecisionContract;
                for (var slot = 1; slot <= 4; slot++)
                {
                    var value = EffectValue.Integer((runes[slot - 1] - 1) * 4 + slot);
                    if (chosen.Contains(value)) continue;
                    var invocation = choice.SelectInvocations.Single(command =>
                        command.Candidate.Value.Equals(value));
                    return contract.Commands.Single(command =>
                        command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        command.EffectChoice.Equals(invocation));
                }

                return contract.Commands.Single(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Equals(choice.ConfirmInvocation));
            }
        }
    }
}
