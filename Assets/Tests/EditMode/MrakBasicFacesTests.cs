using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112b. Fissure and Stone Grip through a configured round: the rock placements
    /// the user settled (MR-Q2 "Skip it, keep going", MR-Q3 "Fill; place what fits"), p.18's short
    /// supply, and restore/replay of each face's pause.
    /// </summary>
    public sealed class MrakBasicFacesTests
    {
        private static readonly HeroId Mrak = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MinionId Minion = new MinionId("mrak-basic-blue-melee");
        private static readonly MapId Map = new MapId("mrak-basic-faces");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        // --- Fissure ------------------------------------------------------------------------------

        [Test]
        public void Fissure_ASurvivingTargetsSpaceIsSkipped_AndTheLineRunsOnBeyondIt()
        {
            var round = new Round(Fissure, enemyAt: H(0, 1));
            round.StartPrimaryAttack();
            round.Target(Enemy);
            Assert.That(round.AttackTotal, Is.EqualTo(4), "Fissure's printed Basic Attack 4.");
            round.Defend();

            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(0, 2), H(0, 3), H(0, 4) }),
                "The defended target still stands, so its space is not empty and the line's first " +
                "three EMPTY spaces lie beyond it.");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void Fissure_ADefeatedTargetsSpaceIsTheFirstEmptySpace()
        {
            var round = new Round(Fissure, minionAt: H(-1, 0));
            round.StartPrimaryAttack();
            round.Target(Minion);

            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(-1, 0), H(-2, 0), H(-3, 0) }),
                "p.13 removed the minion, so the attacked space is empty and takes the first rock.");
        }

        [Test]
        public void Fissure_SkipsAnOccupiedSpaceAndKeepsGoing()
        {
            var round = new Round(Fissure, minionAt: H(-1, 0), enemyAllyAt: H(-3, 0));
            round.StartPrimaryAttack();
            round.Target(Minion);

            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(-1, 0), H(-2, 0), H(-4, 0) }),
                "MR-Q2, user-settled: \"Skip it, keep going\" - the hero at (-3,0) is stepped over.");
        }

        [Test]
        public void Fissure_TheBoardsEdgeEndsTheLine()
        {
            var round = new Round(Fissure, mrakAt: H(0, 3), minionAt: H(0, 4));
            round.StartPrimaryAttack();
            round.Target(Minion);

            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(0, 4), H(0, 5) }),
                "Only two spaces remain before the board ends, so two rocks and no more.");
        }

        [Test]
        public void Fissure_AShortPileTakesBackEveryStandingRockWhenItNeedsThemAll()
        {
            var round = new Round(Fissure, minionAt: H(-1, 0), rocksAt: new[] { H(3, -3), H(4, -4) });
            round.StartPrimaryAttack();
            round.Target(Minion);

            Assert.That(round.PendingNode, Is.Null, "Two stand and two are needed: nothing to choose.");
            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(-1, 0), H(-2, 0), H(-3, 0) }),
                "p.18: \"first remove up to that number of tokens of the same type from the board\".");
        }

        [Test]
        public void Fissure_AShortPileAsksWhichRocksReturnWhenMoreStandThanItNeeds()
        {
            var standing = new[] { H(3, -3), H(4, -4), H(-4, 4) };
            var round = new Round(Fissure, mrakAt: H(0, 3), minionAt: H(0, 4), rocksAt: standing);
            round.StartPrimaryAttack();
            round.Target(Minion);

            Assert.That(round.PendingNode, Is.EqualTo("fissure-reclaim"));
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(standing),
                "only standing rocks are offered - never the spaces the line is about to fill");
            round.PickSpaces(standing[0], standing[2]);

            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { standing[1], H(0, 4), H(0, 5) }),
                "the two named rocks came back and the two-space line took them; the third stayed.");
        }

        // --- Stone Grip ---------------------------------------------------------------------------

        [Test]
        public void StoneGrip_ATargetInAStraightLineHasExactlyThreeFarthestSpaces()
        {
            var round = new Round(StoneGrip, enemyAt: H(0, 2));
            round.StartPrimarySkill();
            round.PickHero(Enemy);

            Assert.That(round.PendingNode, Is.Null, "Three spaces share the greatest distance: no tie.");
            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(0, 3), H(1, 2), H(-1, 3) }));
        }

        [Test]
        public void StoneGrip_AnOffLineTargetFillsFromTheNextFarthestAndAsksAboutTheTie()
        {
            var round = new Round(StoneGrip, enemyAt: H(1, 1));
            round.StartPrimarySkill();
            round.PickHero(Enemy);

            Assert.That(round.PendingNode, Is.EqualTo("stone-grip-place"),
                "MR-Q3: two spaces are farthest and the third comes from a tied pair - Mrak chooses.");
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(new[] { H(2, 0), H(0, 2) }));
            round.PickSpaces(H(0, 2));

            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(2, 1), H(1, 2), H(0, 2) }));
        }

        [Test]
        public void StoneGrip_FewerThanThreeEmptySpacesPlaceWhatFits()
        {
            var round = new Round(StoneGrip, mrakAt: H(0, 2), enemyAt: H(0, 5), minionAt: H(0, 4));
            round.StartPrimarySkill();
            round.PickHero(Enemy);

            Assert.That(round.Rocks(), Is.EquivalentTo(new[] { H(1, 4), H(-1, 5) }),
                "MR-Q3, user-settled: \"Fill; place what fits\" - two empty spaces take two rocks.");
        }

        // --- Offers are honest (the 8.93f/8.104 lesson) -------------------------------------------

        [Test]
        public void StoneGripIsNotOfferedWithNoEnemyHeroInRange()
        {
            var round = new Round(StoneGrip, enemyAt: H(0, 4));
            Assert.That(round.IsOffered(MrakCardContent.PrimarySkill), Is.False,
                "The only enemy hero is four spaces away and the printed Range is 3: nothing to target.");
            var inRange = new Round(StoneGrip, enemyAt: H(0, 3));
            Assert.That(inRange.IsOffered(MrakCardContent.PrimarySkill), Is.True);
        }

        [Test]
        public void FissureIsNotOfferedWithNoAdjacentUnit()
        {
            var round = new Round(Fissure, enemyAt: H(0, 2));
            Assert.That(round.IsOffered(MrakCardContent.PrimaryAttack), Is.False);
            var adjacent = new Round(Fissure, enemyAt: H(0, 1));
            Assert.That(adjacent.IsOffered(MrakCardContent.PrimaryAttack), Is.True);
        }

        // --- Restore ------------------------------------------------------------------------------

        [Test]
        public void StoneGripsTiePauseRestoresAndReplaysToTheSameBytes()
        {
            var round = new Round(StoneGrip, enemyAt: H(1, 1));
            round.StartPrimarySkill();
            round.PickHero(Enemy);
            round.AssertRestoreReplays(H(2, 0));
        }

        [Test]
        public void FissuresReclaimPauseRestoresAndReplaysToTheSameBytes()
        {
            var standing = new[] { H(3, -3), H(4, -4), H(-4, 4) };
            var round = new Round(Fissure, mrakAt: H(0, 3), minionAt: H(0, 4), rocksAt: standing);
            round.StartPrimaryAttack();
            round.Target(Minion);
            round.AssertRestoreReplays(standing[1], standing[2]);
        }

        [Test]
        public void ASecondaryAttacksDefensePauseRestoresAndReplaysToTheSameBytes()
        {
            // 8.112a's secondary Attack, paused at p.14's Defense window, through the same round.
            var round = new Round(SeismicShape, enemyAt: H(0, 1));
            round.StartSecondaryAttack();
            round.Target(Enemy);
            Assert.That(round.AttackTotal, Is.EqualTo(6));
            round.AssertDefenseRestoreReplays();
        }

        // -------------------------------------------------------------------------------------------

        private static CardDefinition Fissure => MrakCardContent.FissureDefinition;
        private static CardDefinition StoneGrip => MrakCardContent.StoneGripDefinition;

        private static readonly CardDefinition SeismicShape = new CardDefinition(
            new CardDefinitionId("mrak-basic-seismic-shape"), "rev-1", "TEST - Seismic shape",
            new InitiativeValue(7), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("primary-movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)),
                MrakCardContent.SecondaryAttack(6)
            });

        private static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("mrak-basic-enemy-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private static readonly CardDefinition EnemyWall = new CardDefinition(
            new CardDefinitionId("mrak-basic-enemy-wall"), "rev-1", "TEST - Wall",
            new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        /// <summary>One round on a radius-5 board with Mrak's card committed and selected to act.</summary>
        private sealed class Round
        {
            private readonly CardDefinition face;
            private readonly CardDefinition[] definitions;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly MatchContentCatalogs catalogs;
            private readonly AdjacentMoveAuthority board;
            private readonly RoundResolutionAuthority round;
            private readonly QuickGameMinionSetupState[] minions;

            internal Round(
                CardDefinition face,
                HexCoordinate? mrakAt = null,
                HexCoordinate? enemyAt = null,
                HexCoordinate? minionAt = null,
                HexCoordinate? enemyAllyAt = null,
                IReadOnlyList<HexCoordinate> rocksAt = null)
            {
                this.face = face;
                var participants = StagedBoardParticipants.Create();
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r)).ToArray();
                var heroes = new List<HeroOccupancy>
                {
                    new HeroOccupancy(Mrak, mrakAt ?? H(0, 0)),
                    new HeroOccupancy(Enemy, enemyAt ?? H(-5, 5))
                };
                if (enemyAllyAt.HasValue) heroes.Add(new HeroOccupancy(EnemyAlly, enemyAllyAt.Value));
                minions = minionAt.HasValue
                    ? new[]
                    {
                        new QuickGameMinionSetupState(
                            Minion, new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue,
                            StandardMinionType.Melee,
                            new ConfiguredSpawnPointReference(Map, new SpawnPointId("mrak-basic-spawn")),
                            new ConfiguredSpaceReference(
                                Map, new SpaceId($"q{minionAt.Value.Q}_r{minionAt.Value.R}")),
                            minionAt.Value)
                    }
                    : Array.Empty<QuickGameMinionSetupState>();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces), heroes,
                    Array.Empty<ObstacleOccupancy>(), minions);
                foreach (var at in rocksAt ?? Array.Empty<HexCoordinate>())
                {
                    board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId($"mrak-standing-rock-{at.Q}-{at.R}"),
                        MrakComponentContent.Rock, Mrak, Mrak,
                        new CardInstanceId("mrak-earlier-card"), at));
                }

                source = CardInstance.CreateInHand(new CardInstanceId("mrak-basic-source"),
                    face.Reference, Mrak);
                var hold = CardInstance.CreateInHand(new CardInstanceId("mrak-basic-enemy-hold"),
                    EnemyHold.Reference, Enemy);
                var wall = CardInstance.CreateInHand(new CardInstanceId("mrak-basic-enemy-wall"),
                    EnemyWall.Reference, Enemy);
                definitions = new[] { face, EnemyHold, EnemyWall };
                instances = new[] { source, hold, wall };
                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(
                        MrakComponentContent.Handlers().Concat(MrakRockPlacementContent.Handlers())),
                    new ComponentDefinitionCatalog(
                        MrakComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var markers = new MarkerAttachmentRegister();
                var actions = new CardActionResolutionAuthority(catalogs, board, markers);
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(actions, board, new ActiveEffectLifecycle(),
                        markers, null, null, null, null, null, null));
                Assert.That(round.TryCommitCard(Mrak, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(Enemy, hold.Id).Accepted, Is.True);
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Mrak));
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal int AttackTotal => Turn.PendingDefenseChoice.AttackTotal;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            internal HexCoordinate[] Rocks() =>
                board.PlacedTokens
                    .Where(token => token.Definition.Id.Equals(MrakComponentContent.RockId))
                    .Select(token => token.Coordinate).ToArray();

            internal HexCoordinate[] OfferedSpaces() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray();

            private ActionOffer Offer(ActionDefinitionId id) =>
                ActionOfferQuery.GetAvailableTurnActionOffers(
                        definitions, instances, Mrak, source.Id)
                    .Single(offer => offer.ActionDefinitionId.Equals(id));

            internal bool IsOffered(ActionDefinitionId id) =>
                Turn.GetTurnActionOffers().Any(offer =>
                    offer.ActionDefinitionId.Equals(id) && offer.IsAvailable);

            internal void StartPrimaryAttack() =>
                Assert.That(round.TryStartAttack(new ActionResolutionId("mrak-basic-attack"),
                    Offer(MrakCardContent.PrimaryAttack)).Accepted, Is.True);

            internal void StartSecondaryAttack() =>
                Assert.That(round.TryStartAttack(new ActionResolutionId("mrak-basic-attack"),
                    Offer(MrakCardContent.SecondaryAttackId)).Accepted, Is.True);

            internal void StartPrimarySkill() =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("mrak-basic-skill"),
                    Offer(MrakCardContent.PrimarySkill)).Accepted, Is.True);

            internal void Target(HeroId hero)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null);
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, hero)).Accepted, Is.True);
            }

            internal void Target(MinionId minion)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null);
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, minion)).Accepted, Is.True);
            }

            internal void Defend()
            {
                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null);
                var wall = defense.LegalDefenseOffers.First(offer =>
                    offer.SourceDefinitionReference.Equals(EnemyWall.Reference));
                Assert.That(round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, wall.SourceCardInstanceId)).Accepted,
                    Is.True);
            }

            internal void PickHero(HeroId hero)
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                Answer(choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                    invocation.Candidate.Value.HeroId.Equals(hero)));
                ConfirmIfStillOpen(choice);
            }

            internal void PickSpaces(params HexCoordinate[] spaces)
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                foreach (var space in spaces)
                {
                    var open = Turn.PendingEffectChoice;
                    Answer(open.SelectInvocations.Single(invocation =>
                        invocation.Candidate.Value.Equals(EffectValue.Space(space))));
                }

                ConfirmIfStillOpen(choice);
            }

            private void ConfirmIfStillOpen(PendingEffectChoice asked)
            {
                var open = Turn.PendingEffectChoice;
                if (open != null && open.ChoiceId.Equals(asked.ChoiceId) &&
                    open.ConfirmInvocation != null)
                {
                    Answer(open.ConfirmInvocation);
                }
            }

            private void Answer(EffectChoiceCommand command) =>
                Assert.That(round.TryAnswerEffectChoice(command).Accepted, Is.True);

            /// <summary>
            /// Snapshot at the open question, restore it, answer both the same way, and require the
            /// same next question and the same authoritative bytes - then once more from the retained
            /// pre-answer prefix.
            /// </summary>
            internal void AssertRestoreReplays(params HexCoordinate[] answer)
            {
                var pending = Turn.PendingEffectChoice;
                Assert.That(pending, Is.Not.Null);
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var restored = Restore(bytes);
                var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
                Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                    Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));

                PickSpaces(answer);
                AnswerSpaces(restored.Round, answer);
                var live = AuthoritativeSnapshotSerializer.Serialize(round, board);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(live), "the restored pause replays to the same authoritative bytes");

                var prefix = Restore(bytes);
                AnswerSpaces(prefix.Round, answer);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board),
                    Is.EqualTo(live), "re-executing from the retained prefix matches the forward result");
            }

            internal void AssertDefenseRestoreReplays()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var restored = Restore(bytes);
                var pending = restored.Round.CurrentTurnAuthority.PendingDefenseChoice;
                Assert.That(pending, Is.Not.Null);
                Assert.That(pending.AttackTotal, Is.EqualTo(Turn.PendingDefenseChoice.AttackTotal));
                Defend();
                var wall = pending.LegalDefenseOffers.First(offer =>
                    offer.SourceDefinitionReference.Equals(EnemyWall.Reference));
                Assert.That(restored.Round.TryAnswerDefenseResponse(
                    DefenseResponseChoiceCommand.SelectCards(
                        pending.ResolutionId, pending.ChoiceId, wall.SourceCardInstanceId)).Accepted,
                    Is.True);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
            }

            private RestoredAuthoritativeMatch Restore(string bytes) =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);

            private static void AnswerSpaces(RoundResolutionAuthority target, HexCoordinate[] spaces)
            {
                var asked = target.CurrentTurnAuthority.PendingEffectChoice;
                foreach (var space in spaces)
                {
                    var open = target.CurrentTurnAuthority.PendingEffectChoice;
                    Assert.That(target.TryAnswerEffectChoice(open.SelectInvocations.Single(
                        invocation => invocation.Candidate.Value.Equals(EffectValue.Space(space))))
                        .Accepted, Is.True);
                }

                var still = target.CurrentTurnAuthority.PendingEffectChoice;
                if (still != null && still.ChoiceId.Equals(asked.ChoiceId) &&
                    still.ConfirmInvocation != null)
                {
                    Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
                }
            }
        }
    }
}
