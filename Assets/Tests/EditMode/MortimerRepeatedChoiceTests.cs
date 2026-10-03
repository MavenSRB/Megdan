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
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.8b. Mortimer's repeated modal and Master of Puppets, through
    /// <see cref="MortimerRepeatedChoice"/>, measured on a fixture face whose two bullets are "Place a
    /// Zombie token adjacent to you" and "Remove a Zombie token adjacent to you". The printed faces
    /// that use the composition arrive at 9.8c-9.8e and carry their own tests; these prove the
    /// composition: the counts, a bullet repeating, stopping early, each pass reading the board the
    /// last one left, the Ultimate raising only "up to three times", the offer and restore/replay.
    /// </summary>
    public sealed class MortimerRepeatedChoiceTests
    {
        private static readonly HeroId Mortimer = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("mortimer-repeated-choice");
        private const string Prefix = "fixture";

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate[] Neighbours =
        {
            H(1, 0), H(1, -1), H(0, -1), H(-1, 0), H(-1, 1), H(0, 1)
        };

        private static string Place(int pass) => $"fixture-{pass}-place";

        private static string Remove(int pass) => $"fixture-{pass}-remove";

        // --- Master of Puppets ----------------------------------------------------------------------

        [Test]
        public void MasterOfPuppets_IsAPermanentUltimateDeclaration()
        {
            var card = MortimerCardContent.MasterOfPuppetsDefinition;
            Assert.That(card.Progression.IsUltimate, Is.True);
            Assert.That(card.ActionDefinitions, Is.Empty, "p.16: an Ultimate is never played.");
            Assert.That(card.UltimateRule.RuleKind, Is.EqualTo(MortimerCardContent.MasterOfPuppetsRuleKind));
            Assert.That(card.UltimateRule.Modifications, Is.Empty,
                "it changes no value, permission or object type; the faces read the unlock.");
        }

        // --- The counts -----------------------------------------------------------------------------

        [Test]
        public void ChooseOne_IsOneBareModal()
        {
            var round = new Round(printedPasses: 1);
            round.StartPrimarySkill();

            Assert.That(round.PendingNode, Is.EqualTo(MortimerRepeatedChoice.ModalNode(Prefix, 1)),
                "\"Choose one\" opens the modal with no \"up to\" before it.");
            round.TakeBullet(Place(1));
            round.PickSpaces(H(1, 0));

            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(1, 0) }));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void UpToTwo_PerformsTwiceThenEnds()
        {
            var round = new Round(printedPasses: 2);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Pass(Place(2), H(-1, 0));

            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(1, 0), H(-1, 0) }));
            Assert.That(round.ActionEnded, Is.True, "two is the printed most.");
        }

        [Test]
        public void UpToTwo_MayStopAfterOne()
        {
            var round = new Round(printedPasses: 2);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Decline();

            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(1, 0) }));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void UpToThree_ABulletMayRepeat()
        {
            var round = new Round(printedPasses: 3);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Pass(Place(2), H(0, 1));
            round.Pass(Place(3), H(-1, 0));

            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(1, 0), H(0, 1), H(-1, 0) }),
                "the same bullet three times.");
            Assert.That(round.ActionEnded, Is.True,
                "with Master of Puppets locked, three is the most and nothing more is asked.");
        }

        [Test]
        public void EachPass_ReadsTheBoardTheLastOneLeft()
        {
            var round = new Round(printedPasses: 2);
            round.StartPrimarySkill();
            round.Accept();
            Assert.That(round.UnavailableBullets(), Is.EqualTo(new[] { Remove(1) }),
                "no Zombie stands beside him, so the Remove bullet can do nothing on the first pass.");
            round.TakeBullet(Place(1));
            round.PickSpaces(H(1, 0));

            round.Accept();
            Assert.That(round.AvailableBullets(), Is.EquivalentTo(new[] { Place(2), Remove(2) }),
                "the Zombie the first pass placed makes the Remove bullet performable.");
            round.TakeBullet(Remove(2));
            round.PickSpaces(H(1, 0));

            Assert.That(round.Zombies(), Is.Empty);
            Assert.That(round.ActionEnded, Is.True);
        }

        // --- Master of Puppets raises three to five -------------------------------------------------

        [Test]
        public void UpToThree_WithMasterOfPuppetsUnlocked_OffersAFourthAndFifth()
        {
            var round = new Round(printedPasses: 3, puppetsUnlocked: true);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Pass(Place(2), H(0, 1));
            round.Pass(Place(3), H(-1, 0));

            Assert.That(round.PendingNode, Is.EqualTo(MortimerRepeatedChoice.UpToNode(Prefix, 4)),
                "\"you may choose up to five times instead\".");
            round.Pass(Place(4), H(0, -1));
            round.Pass(Remove(5), H(1, 0));

            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(0, 1), H(-1, 0), H(0, -1) }));
            Assert.That(round.ActionEnded, Is.True, "five is the raised most.");
        }

        [Test]
        public void UpToThree_WithMasterOfPuppetsUnlocked_MayKeepThree()
        {
            var round = new Round(printedPasses: 3, puppetsUnlocked: true);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Pass(Place(2), H(0, 1));
            round.Pass(Place(3), H(-1, 0));
            Assert.That(round.PendingNode, Is.EqualTo(MortimerRepeatedChoice.UpToNode(Prefix, 4)));
            round.Decline();

            Assert.That(round.PendingNode, Is.Null, "W10 (9.39): declining the fourth asks no fifth.");
            Assert.That(round.Zombies(), Has.Length.EqualTo(3), "\"may\": he keeps three.");
            Assert.That(round.ActionEnded, Is.True);
        }

        /// <summary>
        /// Milestone 9.39, ruling W10 (the user, 2026-10-03): "Choose up to three times" is one
        /// decision about how many, so declining the second pass asks no third. Before it each pass
        /// was its own "up to" and the third was still opened.
        /// </summary>
        [Test]
        public void UpToThree_DecliningTheSecondPass_AsksNoThird()
        {
            var round = new Round(printedPasses: 3);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            Assert.That(round.PendingNode, Is.EqualTo(MortimerRepeatedChoice.UpToNode(Prefix, 2)));
            round.Decline();

            Assert.That(round.PendingNode, Is.Null, "no third pass is offered.");
            Assert.That(round.Zombies(), Is.EqualTo(new[] { H(1, 0) }), "what the first pass did stands.");
            Assert.That(round.ActionEnded, Is.True);
        }

        /// <summary>
        /// With the Ultimate unlocked, declining the THIRD pass closes the printed count and the gate
        /// behind it: the fourth and fifth are not offered either, because "you may choose up to five
        /// times instead" raises the same one count rather than opening a second.
        /// </summary>
        [Test]
        public void UpToThree_WithMasterOfPuppetsUnlocked_DecliningTheThird_SkipsTheRaisedPasses()
        {
            var round = new Round(printedPasses: 3, puppetsUnlocked: true);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Pass(Place(2), H(0, 1));
            Assert.That(round.PendingNode, Is.EqualTo(MortimerRepeatedChoice.UpToNode(Prefix, 3)));
            round.Decline();

            Assert.That(round.PendingNode, Is.Null, "neither the fourth nor the fifth pass is asked.");
            Assert.That(round.Zombies(), Is.EquivalentTo(new[] { H(1, 0), H(0, 1) }));
            Assert.That(round.ActionEnded, Is.True);
        }

        /// <summary>
        /// The fixture's own root carries the same policy the dealt faces do, so these cases exercise
        /// W10 at the printed count and not only at Master of Puppets' raised passes.
        /// </summary>
        [Test]
        public void TheCountSequence_IsUpToWhenMoreThanOnePassIsPrinted()
        {
            Assert.That(MortimerRepeatedChoice.CountPolicy(1), Is.EqualTo(EffectFailurePolicy.Mandatory),
                "\"Choose one\" is the bare modal.");
            Assert.That(MortimerRepeatedChoice.CountPolicy(2), Is.EqualTo(EffectFailurePolicy.UpTo));
            Assert.That(MortimerRepeatedChoice.CountPolicy(3), Is.EqualTo(EffectFailurePolicy.UpTo));
            var unlocked = FixtureSkill(3).Nodes.Single(node => node.Id.Value == $"{Prefix}-puppets-passes");
            Assert.That(unlocked.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                "the raised passes behind the gate are the same one count.");
        }

        [Test]
        public void UpToTwo_IsUnchangedByMasterOfPuppets()
        {
            var round = new Round(printedPasses: 2, puppetsUnlocked: true);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Pass(Place(2), H(0, 1));

            Assert.That(round.ActionEnded, Is.True, "only \"up to three times\" becomes five.");
        }

        [Test]
        public void ChooseOne_IsUnchangedByMasterOfPuppets()
        {
            var round = new Round(printedPasses: 1, puppetsUnlocked: true);
            round.StartPrimarySkill();
            round.TakeBullet(Place(1));
            round.PickSpaces(H(1, 0));

            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void OnlyUpToThreeFaces_ComposeTheGate()
        {
            for (var printed = 1; printed <= 3; printed++)
            {
                var nodes = FixtureSkill(printed).Nodes.Select(node => node.Id.Value).ToArray();
                Assert.That(nodes.Contains(MortimerRepeatedChoice.PuppetsGateNode(Prefix)), Is.EqualTo(printed == 3),
                    $"{printed} printed pass(es).");
            }
        }

        // --- Offer and answer agree -----------------------------------------------------------------

        [Test]
        public void BoxedIn_UpToThree_IsNotOfferedWhetherOrNotThePuppetsAreUnlocked()
        {
            Assert.That(new Round(printedPasses: 3, minionsAt: Neighbours)
                    .IsOffered(MortimerCardContent.PrimarySkill), Is.False,
                "every pass could only decline, and the locked Ultimate's gate is no printed \"If\" " +
                "(RULING 53), so it does not keep the face offered.");
            Assert.That(new Round(printedPasses: 3, puppetsUnlocked: true, minionsAt: Neighbours)
                    .IsOffered(MortimerCardContent.PrimarySkill), Is.False,
                "five passes that can each do nothing still do nothing.");
        }

        [Test]
        public void WithASpaceBesideHim_UpToThree_IsOffered()
        {
            Assert.That(new Round(printedPasses: 3, minionsAt: Neighbours.Skip(1).ToArray())
                .IsOffered(MortimerCardContent.PrimarySkill), Is.True);
        }

        // --- Restore and replay mid-chain -----------------------------------------------------------

        [Test]
        public void APauseInsideTheFourthPass_RestoresAndReplaysToTheSameBytes()
        {
            var round = new Round(printedPasses: 3, puppetsUnlocked: true);
            round.StartPrimarySkill();
            round.Pass(Place(1), H(1, 0));
            round.Pass(Place(2), H(0, 1));
            round.Pass(Place(3), H(-1, 0));
            round.Accept();
            round.TakeBullet(Place(4));

            round.AssertRestoreReplays(H(0, -1));
        }

        // --- The fixture face -----------------------------------------------------------------------

        private static readonly ActionDefinitionId FixtureSkill_ = MortimerCardContent.PrimarySkill;

        private static CardDefinition FixtureCard(int printedPasses) => new CardDefinition(
            new CardDefinitionId($"mortimer-fixture-{printedPasses}"), "rev-1", "TEST - Repeated choice",
            new InitiativeValue(6), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    FixtureSkill_, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    FixtureSkill(printedPasses))
            });

        private static EffectDefinition FixtureSkill(int printedPasses)
        {
            var nodes = new List<EffectNodeDefinition>();
            var steps = MortimerRepeatedChoice.Compose(Prefix, printedPasses, Bullets, nodes);
            var root = new EffectNodeId("fixture-root");
            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, MortimerRepeatedChoice.CountPolicy(printedPasses), childNodeIds: steps));
            return new EffectDefinition(
                new EffectDefinitionId($"mortimer-fixture-{printedPasses}-skill"), "rev-1", root, nodes);
        }

        /// <summary>"Place a Zombie token adjacent to you." / "Remove a Zombie token adjacent to you."</summary>
        private static (IReadOnlyList<EffectNodeId> Options, IEnumerable<EffectNodeDefinition> Nodes) Bullets(int pass)
        {
            var place = new EffectNodeId(Place(pass));
            var reclaim = new EffectNodeId($"fixture-{pass}-reclaim");
            var put = new EffectNodeId($"fixture-{pass}-put");
            var remove = new EffectNodeId(Remove(pass));
            var pick = new EffectNodeId($"fixture-{pass}-pick");
            var take = new EffectNodeId($"fixture-{pass}-take");
            return (new[] { place, remove }, new[]
            {
                new EffectNodeDefinition(
                    place, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { reclaim, put }),
                new EffectNodeDefinition(
                    reclaim, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    MortimerComponentContent.ReclaimZombieV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    put, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    MortimerComponentContent.PlaceZombieV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.SpawnPointOrAdjacentParameterName,
                            EffectValue.Boolean(true))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(
                    remove, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { pick, take }),
                new EffectNodeDefinition(
                    pick, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.ChooseTargetV4,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                        EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.TokenKindParameterName,
                            EffectValue.TokenKind(MortimerComponentContent.ZombieId)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(take))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(
                    take, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.RemoveV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("subject", pick, "chosen", EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            RemoveEligiblePieceKind.Token)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                        new EffectOutputBinding(RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                    })
            });
        }

        // --- The harness ----------------------------------------------------------------------------

        private static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("mortimer-fixture-enemy-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        /// <summary>One round on a radius-5 board with the fixture face committed and Mortimer to act.</summary>
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
                int printedPasses,
                bool puppetsUnlocked = false,
                IReadOnlyList<HexCoordinate> minionsAt = null)
            {
                var participants = StagedBoardParticipants.Create();
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r)).ToArray();
                var heroes = new List<HeroOccupancy>
                {
                    new HeroOccupancy(Mortimer, H(0, 0)),
                    new HeroOccupancy(Enemy, H(-5, 5))
                };
                minions = (minionsAt ?? Array.Empty<HexCoordinate>())
                    .Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"mortimer-fixture-minion-{index:00}"),
                        new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("fixture-spawn")),
                        new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")), at))
                    .ToArray();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces), heroes,
                    Array.Empty<ObstacleOccupancy>(), minions);

                var face = FixtureCard(printedPasses);
                source = CardInstance.CreateInHand(new CardInstanceId("mortimer-fixture-source"),
                    face.Reference, Mortimer);
                var hold = CardInstance.CreateInHand(new CardInstanceId("mortimer-fixture-enemy-hold"),
                    EnemyHold.Reference, Enemy);
                var puppets = MortimerCardContent.MasterOfPuppetsDefinition;
                var definitionList = new List<CardDefinition> { face, EnemyHold };
                var instanceList = new List<CardInstance> { source, hold };
                if (puppetsUnlocked)
                {
                    definitionList.Add(puppets);
                    instanceList.Add(new CardInstance(
                        new CardInstanceId("mortimer-fixture-puppets"), puppets.Reference, Mortimer,
                        new CardStateFacts(CardLocation.UltimateArea, false, CardResolutionStatus.None,
                            CardFaceOrientation.FaceUp)));
                }

                definitions = definitionList.ToArray();
                instances = instanceList.ToArray();
                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(MortimerComponentContent.Handlers()),
                    new ComponentDefinitionCatalog(
                        MortimerComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var markers = new MarkerAttachmentRegister();
                var actions = new CardActionResolutionAuthority(catalogs, board, markers);
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(actions, board, new ActiveEffectLifecycle(),
                        markers, null, null, null, null, null, null));
                Assert.That(round.TryCommitCard(Mortimer, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(Enemy, hold.Id).Accepted, Is.True);
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Mortimer));
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

            internal bool ActionEnded =>
                Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
                Turn.PendingAttackTargetChoice == null;

            internal HexCoordinate[] Zombies() =>
                board.PlacedTokens
                    .Where(token => token.Definition.Id.Equals(MortimerComponentContent.ZombieId))
                    .Select(token => token.Coordinate).ToArray();

            internal string[] AvailableBullets() =>
                Turn.PendingEffectChoice.SelectInvocations
                    .Select(command => command.Candidate.OptionNodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();

            internal string[] UnavailableBullets() =>
                Turn.PendingEffectChoice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();

            private ActionOffer Offer(ActionDefinitionId id) =>
                ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, Mortimer, source.Id)
                    .Single(offer => offer.ActionDefinitionId.Equals(id));

            internal bool IsOffered(ActionDefinitionId id) =>
                Turn.GetTurnActionOffers().Any(offer =>
                    offer.ActionDefinitionId.Equals(id) && offer.IsAvailable);

            internal void StartPrimarySkill() =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("mortimer-fixture-skill"),
                    Offer(MortimerCardContent.PrimarySkill)).Accepted, Is.True);

            /// <summary>Take one "up to" pass: accept it, choose the bullet, answer its one space.</summary>
            internal void Pass(string bullet, HexCoordinate space)
            {
                Accept();
                TakeBullet(bullet);
                PickSpaces(space);
            }

            internal void Accept()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice?.ConfirmInvocation, Is.Not.Null, "an \"up to\" pass can be taken");
                Answer(choice.ConfirmInvocation);
            }

            internal void Decline()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice?.DeclineInvocation, Is.Not.Null, "an \"up to\" pass can be declined");
                Answer(choice.DeclineInvocation);
            }

            internal void TakeBullet(string optionNode)
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                Answer(choice.SelectInvocations.Single(command =>
                    command.Candidate.OptionNodeId.Value == optionNode));
                ConfirmIfStillOpen(round, choice);
            }

            internal void PickSpaces(params HexCoordinate[] spaces)
            {
                Assert.That(Turn.PendingEffectChoice, Is.Not.Null);
                AnswerSpaces(round, spaces);
            }

            private void Answer(EffectChoiceCommand command) =>
                Assert.That(round.TryAnswerEffectChoice(command).Accepted, Is.True);

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
                Assert.That(restored.Round.CurrentTurnAuthority.PendingEffectChoice?.NodeId,
                    Is.EqualTo(Turn.PendingEffectChoice?.NodeId), "and asks the fifth pass next, as live");

                var prefix = Restore(bytes);
                AnswerSpaces(prefix.Round, answer);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board),
                    Is.EqualTo(live), "re-executing from the retained prefix matches the forward result");
            }

            private RestoredAuthoritativeMatch Restore(string bytes) =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);

            private static void ConfirmIfStillOpen(RoundResolutionAuthority target, PendingEffectChoice asked)
            {
                var open = target.CurrentTurnAuthority.PendingEffectChoice;
                if (open != null && open.ChoiceId.Equals(asked.ChoiceId) && open.ConfirmInvocation != null)
                {
                    Assert.That(target.TryAnswerEffectChoice(open.ConfirmInvocation).Accepted, Is.True);
                }
            }

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

                ConfirmIfStillOpen(target, asked);
            }
        }
    }
}
