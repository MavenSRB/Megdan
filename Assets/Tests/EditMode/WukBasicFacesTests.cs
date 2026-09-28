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
    /// Milestone 8.114a. Wuk's Tree token (WU-Q1 three, WU-Q2 retained at every round end), Mystic
    /// Saplings' up-to-three placement with p.18's short supply, and Tree Slam's two bullets through a
    /// configured round, with restore/replay of the placement pause.
    /// </summary>
    public sealed class WukBasicFacesTests
    {
        private static readonly HeroId Wuk = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MinionId Minion = new MinionId("wuk-basic-blue-melee");
        private static readonly MapId Map = new MapId("wuk-basic-faces");

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        // --- The Tree token -------------------------------------------------------------------------

        [Test]
        public void Tree_IsAThreeTokenSupplyThatNoRoundEndRemoves()
        {
            Assert.That(WukComponentContent.Tree.SupplyCount, Is.EqualTo(3), "WU-Q1, user-settled: three.");
            Assert.That(WukComponentContent.Tree.RemovalCondition,
                Is.EqualTo(TokenRemovalCondition.RetainedAtEndOfRound),
                "WU-Q2, user-settled: Mystic Saplings' second sentence is every Tree's lifetime.");

            var tree = new PlacedToken(new TokenInstanceId("tree"), WukComponentContent.Tree, Wuk, Wuk,
                new CardInstanceId("card"), H(1, 0));
            var rock = new PlacedToken(new TokenInstanceId("rock"), MrakComponentContent.Rock, Wuk, Wuk,
                new CardInstanceId("card"), H(2, 0));
            Assert.That(ComponentCleanupRules.TokensRemovedAtEndOfRound(new[] { tree, rock }),
                Is.EqualTo(new[] { rock }),
                "p.8 takes an ordinary token back at the round's end and leaves the Tree standing.");
            Assert.That(WukComponentContent.Tree.Properties, Is.Empty,
                "no face gives a Tree a property: it is a bare p.19 obstacle.");
        }

        // --- Mystic Saplings ------------------------------------------------------------------------

        [Test]
        public void MysticSaplings_PlacesThreeTreesAnywhereEmptyInItsPrintedRadius()
        {
            var round = new Round(WukCardContent.MysticSaplingsDefinition, enemyAt: H(1, 0));
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo("mystic-saplings-place-1"),
                "a full pile needs nothing back, so the first question is where the first Tree goes.");
            var offered = round.OfferedSpaces();
            Assert.That(offered, Has.All.Matches<HexCoordinate>(space =>
                HexDistanceQuery.Calculate(space, H(0, 0)) <= 3 && !space.Equals(H(0, 0))),
                "p.9's Radius 3, measured from Wuk.");
            Assert.That(offered, Has.No.Member(H(1, 0)), "a Place goes only into an empty space (p.18).");
            Assert.That(offered, Has.Member(H(0, 3)), "the printed Radius reaches three spaces out.");

            round.PickSpaces(H(0, 3));
            round.PickSpaces(H(-1, 0));
            round.PickSpaces(H(2, -1));

            Assert.That(round.Trees(), Is.EquivalentTo(new[] { H(0, 3), H(-1, 0), H(2, -1) }));
            Assert.That(round.ActionEnded, Is.True, "three placed is the printed most.");
        }

        [Test]
        public void MysticSaplings_UpToThree_StopsWhereWukDeclines()
        {
            var round = new Round(WukCardContent.MysticSaplingsDefinition);
            round.StartPrimarySkill();
            round.PickSpaces(H(0, 1));
            Assert.That(round.PendingNode, Is.EqualTo("mystic-saplings-place-2"));
            round.Decline();

            Assert.That(round.PendingNode, Is.EqualTo("mystic-saplings-place-3"),
                "each placement is its own \"up to\" step.");
            round.Decline();
            Assert.That(round.Trees(), Is.EqualTo(new[] { H(0, 1) }));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void MysticSaplings_AnEmptyPileAsksWhichStandingTreeComesBack()
        {
            var standing = new[] { H(4, -4), H(-4, 4), H(4, 0) };
            var round = new Round(WukCardContent.MysticSaplingsDefinition, treesAt: standing);
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo("mystic-saplings-reclaim-1"),
                "all three Trees stand, so p.18's short supply comes first.");
            Assert.That(round.OfferedSpaces(), Is.EquivalentTo(standing));
            round.PickSpaces(standing[1]);

            Assert.That(round.PendingNode, Is.EqualTo("mystic-saplings-place-1"));
            round.PickSpaces(H(0, 2));
            round.Decline();
            round.Decline();

            Assert.That(round.Trees(), Is.EquivalentTo(new[] { standing[0], standing[2], H(0, 2) }),
                "the named Tree came back to the pile and went out again; the other two stayed.");
        }

        [Test]
        public void MysticSaplings_TheShortSupplyStepStandsAsideWhileThePileHoldsATree()
        {
            var round = new Round(WukCardContent.MysticSaplingsDefinition, treesAt: new[] { H(4, -4), H(-4, 4) });
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo("mystic-saplings-place-1"),
                "one Tree is still in the pile, so the first placement asks nothing first.");
            round.PickSpaces(H(0, 1));
            Assert.That(round.PendingNode, Is.EqualTo("mystic-saplings-reclaim-2"),
                "the pile is empty now, so the second placement starts with p.18's step.");
        }

        [Test]
        public void MysticSaplings_APausedPlacementRestoresAndReplaysToTheSameBytes()
        {
            var round = new Round(WukCardContent.MysticSaplingsDefinition);
            round.StartPrimarySkill();
            round.PickSpaces(H(0, 1));

            round.AssertRestoreReplays(H(1, 1));
        }

        // --- Tree Slam ------------------------------------------------------------------------------

        [Test]
        public void TreeSlam_WithNoTreeBesideWuk_TheTreeBulletIsGreyed()
        {
            var round = new Round(WukCardContent.TreeSlamDefinition, minionAt: H(1, 0), enemyAt: H(0, 2));
            round.StartPrimaryAttack();

            Assert.That(round.PendingNode, Is.EqualTo(WukCardContent.TreeSlamModalNode));
            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { "tree-slam-tree-arm" }),
                "p.10: \"Remove a Tree token\" is mandatory, so with none beside Wuk the bullet can " +
                "do nothing and is greyed rather than taken and spent.");
            Assert.That(round.AvailableBullets(), Is.EqualTo(new[] { "tree-slam-adjacent" }));
        }

        [Test]
        public void TreeSlam_TheAdjacentBulletTargetsAMinionAndNeverAHero()
        {
            var round = new Round(WukCardContent.TreeSlamDefinition, minionAt: H(1, 0), enemyAt: H(0, 1));
            round.StartPrimaryAttack();
            round.TakeBullet("tree-slam-adjacent");

            Assert.That(round.OfferedMinions(), Is.EqualTo(new[] { Minion }),
                "\"Target a minion adjacent to you\" - the enemy hero beside Wuk is not a candidate.");
            Assert.That(round.OfferedHeroes(), Is.Empty);
            round.PickMinion(Minion);

            Assert.That(round.MinionStands(), Is.False, "p.13: an attacked minion is defeated.");
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void TreeSlam_TheTreeBulletRemovesATreeBesideWukThenAttacksAUnitInRange()
        {
            var round = new Round(WukCardContent.TreeSlamDefinition, enemyAt: H(0, 2),
                treesAt: new[] { H(-1, 0), H(3, 0) });
            round.StartPrimaryAttack();
            Assert.That(round.AvailableBullets(), Is.EqualTo(new[] { "tree-slam-tree-arm" }),
                "nothing stands beside Wuk to take the adjacent bullet; the Tree bullet is offered.");
            round.TakeBullet("tree-slam-tree-arm");

            Assert.That(round.PendingNode, Is.EqualTo(WukCardContent.TreeSlamTreeNode));
            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(-1, 0) }),
                "only the Tree adjacent to Wuk - not the one three spaces away.");
            round.PickSpaces(H(-1, 0));

            Assert.That(round.Trees(), Is.EqualTo(new[] { H(3, 0) }), "the chosen Tree is removed.");
            Assert.That(round.OfferedHeroes(), Is.EqualTo(new[] { Enemy }),
                "then a unit in the printed Range 2.");
            round.PickHero(Enemy);

            Assert.That(round.AttackTotal, Is.EqualTo(4), "Tree Slam's printed Basic Attack 4.");
            round.AssertDefenseRestoreReplays();
        }

        [Test]
        public void TreeSlam_TheTreeBulletOffersOnlyTreesBesideWuk()
        {
            var round = new Round(WukCardContent.TreeSlamDefinition, enemyAt: H(0, 2),
                minionAt: H(1, 0), treesAt: new[] { H(-1, 0) }, rocksAt: new[] { H(0, 1) });
            round.StartPrimaryAttack();
            round.TakeBullet("tree-slam-tree-arm");

            Assert.That(round.OfferedSpaces(), Is.EqualTo(new[] { H(-1, 0) }),
                "\"Remove a Tree token\": the enemy's rock and the minion beside Wuk are not Trees.");
        }

        [Test]
        public void TreeSlam_WithATreeButNoUnitInItsPrintedRange_IsNotOffered()
        {
            var round = new Round(WukCardContent.TreeSlamDefinition, enemyAt: H(0, 3),
                treesAt: new[] { H(-1, 0) });

            Assert.That(round.IsOffered(WukCardContent.PrimaryRangedAttack), Is.False,
                "the Tree bullet could remove the Tree but then name nobody within Range 2, and the " +
                "adjacent bullet names nobody at all - an offer must be able to do what it offers.");
        }

        [Test]
        public void TreeSlam_WithATreeAndAUnitInRange_IsOffered()
        {
            var round = new Round(WukCardContent.TreeSlamDefinition, enemyAt: H(0, 2),
                treesAt: new[] { H(-1, 0) });

            Assert.That(round.IsOffered(WukCardContent.PrimaryRangedAttack), Is.True);
        }

        // --- The harness ----------------------------------------------------------------------------

        private static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("wuk-basic-enemy-hold"), "rev-1", "TEST - Hold",
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
            new CardDefinitionId("wuk-basic-enemy-wall"), "rev-1", "TEST - Wall",
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

        /// <summary>One round on a radius-5 board with Wuk's card committed and selected to act.</summary>
        private sealed class Round
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly MatchContentCatalogs catalogs;
            private readonly AdjacentMoveAuthority board;
            private readonly RoundResolutionAuthority round;
            private readonly QuickGameMinionSetupState[] minions;

            internal Round(
                CardDefinition face,
                HexCoordinate? wukAt = null,
                HexCoordinate? enemyAt = null,
                HexCoordinate? minionAt = null,
                IReadOnlyList<HexCoordinate> treesAt = null,
                IReadOnlyList<HexCoordinate> rocksAt = null)
            {
                var participants = StagedBoardParticipants.Create();
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r)).ToArray();
                var heroes = new List<HeroOccupancy>
                {
                    new HeroOccupancy(Wuk, wukAt ?? H(0, 0)),
                    new HeroOccupancy(Enemy, enemyAt ?? H(-5, 5))
                };
                minions = minionAt.HasValue
                    ? new[]
                    {
                        new QuickGameMinionSetupState(
                            Minion, new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue,
                            StandardMinionType.Melee,
                            new ConfiguredSpawnPointReference(Map, new SpawnPointId("wuk-basic-spawn")),
                            new ConfiguredSpaceReference(
                                Map, new SpaceId($"q{minionAt.Value.Q}_r{minionAt.Value.R}")),
                            minionAt.Value)
                    }
                    : Array.Empty<QuickGameMinionSetupState>();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces), heroes,
                    Array.Empty<ObstacleOccupancy>(), minions);
                foreach (var at in treesAt ?? Array.Empty<HexCoordinate>())
                {
                    board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId($"wuk-standing-tree-{at.Q}-{at.R}"),
                        WukComponentContent.Tree, Wuk, Wuk,
                        new CardInstanceId("wuk-earlier-card"), at));
                }

                foreach (var at in rocksAt ?? Array.Empty<HexCoordinate>())
                {
                    board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId($"enemy-rock-{at.Q}-{at.R}"),
                        MrakComponentContent.Rock, Enemy, Enemy,
                        new CardInstanceId("enemy-earlier-card"), at));
                }

                source = CardInstance.CreateInHand(new CardInstanceId("wuk-basic-source"),
                    face.Reference, Wuk);
                var hold = CardInstance.CreateInHand(new CardInstanceId("wuk-basic-enemy-hold"),
                    EnemyHold.Reference, Enemy);
                var wall = CardInstance.CreateInHand(new CardInstanceId("wuk-basic-enemy-wall"),
                    EnemyWall.Reference, Enemy);
                definitions = new[] { face, EnemyHold, EnemyWall };
                instances = new[] { source, hold, wall };
                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(WukComponentContent.DeferredHandlers()),
                    new ComponentDefinitionCatalog(
                        WukComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var markers = new MarkerAttachmentRegister();
                var actions = new CardActionResolutionAuthority(catalogs, board, markers);
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(actions, board, new ActiveEffectLifecycle(),
                        markers, null, null, null, null, null, null));
                Assert.That(round.TryCommitCard(Wuk, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(Enemy, hold.Id).Accepted, Is.True);
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Wuk));
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal int AttackTotal => Turn.PendingDefenseChoice.AttackTotal;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            internal HexCoordinate[] Trees() =>
                board.PlacedTokens
                    .Where(token => token.Definition.Id.Equals(WukComponentContent.TreeId))
                    .Select(token => token.Coordinate).ToArray();

            internal bool MinionStands() =>
                board.TryGetObjectCoordinate(BoardObjectReference.ForMinion(Minion), out _);

            internal HexCoordinate[] OfferedSpaces() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate).ToArray();

            internal HeroId[] OfferedHeroes() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.Value.HeroId).ToArray();

            internal MinionId[] OfferedMinions() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                    .Select(candidate => candidate.Value.MinionId).ToArray();

            internal string[] AvailableBullets() =>
                Turn.PendingEffectChoice.SelectInvocations
                    .Select(command => command.Candidate.OptionNodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();

            internal string[] UnavailableBullets() =>
                Turn.PendingEffectChoice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();

            private ActionOffer Offer(ActionDefinitionId id) =>
                ActionOfferQuery.GetAvailableTurnActionOffers(
                        definitions, instances, Wuk, source.Id)
                    .Single(offer => offer.ActionDefinitionId.Equals(id));

            internal bool IsOffered(ActionDefinitionId id) =>
                Turn.GetTurnActionOffers().Any(offer =>
                    offer.ActionDefinitionId.Equals(id) && offer.IsAvailable);

            internal void StartPrimaryAttack() =>
                Assert.That(round.TryStartAttack(new ActionResolutionId("wuk-basic-attack"),
                    Offer(WukCardContent.PrimaryRangedAttack)).Accepted, Is.True);

            internal void StartPrimarySkill() =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("wuk-basic-skill"),
                    Offer(WukCardContent.PrimarySkill)).Accepted, Is.True);

            internal void TakeBullet(string optionNode)
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                Answer(choice.SelectInvocations.Single(command =>
                    command.Candidate.OptionNodeId.Value == optionNode));
                ConfirmIfStillOpen(choice);
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

            internal void PickMinion(MinionId minion)
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                Answer(choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                    invocation.Candidate.Value.MinionId.Equals(minion)));
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

            internal void Decline()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice?.DeclineInvocation, Is.Not.Null, "an \"up to\" step can be declined");
                Answer(choice.DeclineInvocation);
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
                Defend(round);
                Defend(restored.Round);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
            }

            private static void Defend(RoundResolutionAuthority target)
            {
                var defense = target.CurrentTurnAuthority.PendingDefenseChoice;
                var wall = defense.LegalDefenseOffers.First(offer =>
                    offer.SourceDefinitionReference.Equals(EnemyWall.Reference));
                Assert.That(target.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, wall.SourceCardInstanceId)).Accepted,
                    Is.True);
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
