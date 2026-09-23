using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.88. The printed-icon selection family as ONE composable selection: every clause it
    /// knows is a parameter on every arm it can mean something on, refused by name on the rest, and the
    /// four targeting shapes the survey of the ten undealt heroes found are expressed as definition data
    /// with no new handler.
    ///
    /// <para><b>What changed and what did not.</b> Before 8.88 each clause lived on the one arm whose
    /// face printed it, so a phrase pairing a known clause with another arm minted a capability. Now the
    /// clauses compose. No capability was registered, so no content digest, snapshot schema or wire
    /// protocol moves - a parameter added to a registered capability is not part of the registration
    /// identity. The required parameters of every arm are exactly the ones it required before.</para>
    ///
    /// <para><b>The four shapes</b>, each written as a definition node, validated at content load, and
    /// then asked through the registered handler:</para>
    /// <list type="bullet">
    /// <item><description>Mortimer's "another enemy hero in radius, adjacent to a Zombie token"
    /// (adjacency to a kind, with a count);</description></item>
    /// <item><description>Mrak's "an enemy hero in radius adjacent to terrain, or to a rock token"
    /// (adjacency to a union of kinds);</description></item>
    /// <item><description>Trinkets' "a unit in range of both you and the Turret" and "an enemy hero in
    /// radius of the Turret" (a second centre, and a moved centre);</description></item>
    /// <item><description>"a token, or an enemy unit" and "a Zombie token in range" (unions and
    /// narrowings of the object kinds).</description></item>
    /// </list>
    ///
    /// <para>Ownership names a token's kind here - see
    /// <see cref="SyntheticTargetingCapabilities.AdjacentToOwnTokenParameterName"/>. Unity-independent,
    /// so it runs in the cross-platform harness as well as the Windows tier.</para>
    /// </summary>
    public sealed class ComposableTargetSelectionTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MapId Map = new MapId("composable-selection");
        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        private static readonly MinionId EnemyMinion = new MinionId("composable-enemy-minion");
        private static readonly MinionId FriendlyMinion = new MinionId("composable-friendly-minion");

        private static readonly EffectCapabilityReference[] Arms =
        {
            SyntheticTargetingCapabilities.RadiusTargetV1,
            SyntheticTargetingCapabilities.RangeTargetV1,
            SyntheticTargetingCapabilities.RadiusHeroTargetV1,
            SyntheticTargetingCapabilities.RangeHeroTargetV1,
            SyntheticTargetingCapabilities.RadiusMinionTargetV1,
            SyntheticTargetingCapabilities.RangeMinionTargetV1,
            SyntheticTargetingCapabilities.RadiusUnitTargetV1,
            SyntheticTargetingCapabilities.RangeUnitTargetV1
        };

        // -------------------------------------------------------------------------------------------
        // Conformance: every clause on every arm, or refused by name
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryArm_DeclaresEachClauseItCanMean_AndRefusesTheRestByName()
        {
            foreach (var arm in Arms)
            {
                var names = Handler(arm).Schema.Parameters.Select(parameter => parameter.Name).ToArray();
                foreach (var clause in SyntheticTargetingCapabilities.PrintedIconFilterParameterNames)
                {
                    var refusal = SyntheticTargetingCapabilities.PrintedIconFilterRefusal(arm, clause);
                    Assert.That(names.Count(name => name == clause), Is.EqualTo(refusal == null ? 1 : 0),
                        $"{arm} declares '{clause}' exactly when it does not refuse it by name.");
                }
            }
        }

        /// <summary>
        /// The refusals are statements that a clause cannot be true of anything an arm offers - never
        /// "no face prints it" - so only the minion, hero and unit arms refuse anything, and the object
        /// arm, which can offer every kind, refuses nothing.
        /// </summary>
        [Test]
        public void TheRefusals_AreExactlyTheClausesAnArmsKindsCannotSatisfy()
        {
            string[] Refused(EffectCapabilityReference arm) =>
                SyntheticTargetingCapabilities.PrintedIconFilterParameterNames
                    .Where(clause => SyntheticTargetingCapabilities.PrintedIconFilterRefusal(arm, clause) != null)
                    .ToArray();

            var objectKinds = new[]
            {
                SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                SyntheticTargetingCapabilities.OwnTokensOnlyParameterName,
                // Milestone 8.91: the token-kind narrowing, like the other two, chooses among kinds.
                SyntheticTargetingCapabilities.TokenKindParameterName
            };

            Assert.That(Refused(SyntheticTargetingCapabilities.RangeTargetV1), Is.Empty);
            Assert.That(Refused(SyntheticTargetingCapabilities.RadiusTargetV1), Is.Empty);
            Assert.That(Refused(SyntheticTargetingCapabilities.RangeUnitTargetV1), Is.EquivalentTo(objectKinds));
            Assert.That(
                Refused(SyntheticTargetingCapabilities.RadiusHeroTargetV1),
                Is.EquivalentTo(objectKinds.Concat(new[]
                {
                    SyntheticTargetingCapabilities.ExcludedMinionParameterName,
                    SyntheticTargetingCapabilities.AdmittedMinionClassParameterName
                })));
            Assert.That(
                Refused(SyntheticTargetingCapabilities.RangeMinionTargetV1),
                Is.EquivalentTo(objectKinds.Concat(new[]
                {
                    SyntheticTargetingCapabilities.IncludeSelfParameterName,
                    SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                    SyntheticTargetingCapabilities.ExcludedHeroParameterName,
                    SyntheticTargetingCapabilities.HasCardInDiscardParameterName,
                    SyntheticTargetingCapabilities.HoldsAnUnresolvedCardParameterName,
                    SyntheticTargetingCapabilities.MinimumCardsInHandParameterName,
                    SyntheticTargetingCapabilities.RequiresEmptyHandParameterName,
                    SyntheticTargetingCapabilities.BothDiscardPilesEmptyParameterName,
                    SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName
                })));
        }

        /// <summary>
        /// Behaviour preservation at the contract: every arm requires exactly what it required before
        /// 8.88, so every definition written against it still binds everything it must and nothing it
        /// may not. Everything 8.88 added is optional.
        /// </summary>
        [Test]
        public void EachArm_RequiresExactlyWhatItRequiredBefore()
        {
            var expected = new Dictionary<EffectCapabilityReference, string[]>
            {
                [SyntheticTargetingCapabilities.RadiusTargetV1] = new[] { "activity", "includeSelf" },
                [SyntheticTargetingCapabilities.RangeTargetV1] =
                    new[] { "activity", "includeSelf", "excludeStraightLine" },
                [SyntheticTargetingCapabilities.RadiusHeroTargetV1] =
                    new[] { "activity", "relation", "excludeAdjacent" },
                [SyntheticTargetingCapabilities.RangeHeroTargetV1] =
                    new[] { "activity", "relation", "excludeTheAttacker" },
                [SyntheticTargetingCapabilities.RadiusMinionTargetV1] = new[] { "activity", "relation" },
                [SyntheticTargetingCapabilities.RangeMinionTargetV1] = new[] { "activity", "relation" },
                [SyntheticTargetingCapabilities.RadiusUnitTargetV1] = new[] { "activity", "relation" },
                [SyntheticTargetingCapabilities.RangeUnitTargetV1] = new[] { "activity", "relation" }
            };

            foreach (var arm in Arms)
            {
                Assert.That(
                    Handler(arm).Schema.Parameters.Where(parameter => parameter.IsRequired)
                        .Select(parameter => parameter.Name),
                    Is.EquivalentTo(expected[arm]),
                    $"{arm} requires what it always required, and nothing 8.88 added.");
            }
        }

        /// <summary>
        /// "Legal on every arm" measured at content load: a node binding every clause an arm declares,
        /// each to a value of its declared kind, passes the definition validator with nothing refused.
        /// </summary>
        [Test]
        public void EveryDeclaredClause_BindsAtContentLoad()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;
            foreach (var arm in Arms)
            {
                var bindings = Handler(arm).Schema.Parameters
                    .Where(parameter => parameter.ValueKind != EffectValueKind.Node)
                    .Select(LegalBinding)
                    .ToArray();

                Assert.That(
                    EffectDefinitionValidator.Validate(OneNode(arm, bindings), catalog),
                    Is.Empty,
                    $"Every clause {arm} declares binds at content load.");
            }
        }

        [Test]
        public void ARefusedClause_IsRejectedAtContentLoad_ByName()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;
            var refusedAnywhere = 0;
            foreach (var arm in Arms)
            {
                foreach (var clause in SyntheticTargetingCapabilities.PrintedIconFilterParameterNames
                    .Where(name => SyntheticTargetingCapabilities.PrintedIconFilterRefusal(arm, name) != null))
                {
                    refusedAnywhere++;
                    var bindings = RequiredBindings(arm)
                        .Concat(new[] { EffectInputBinding.FromLiteral(clause, EffectValue.Boolean(true)) })
                        .ToArray();

                    var rejections = EffectDefinitionValidator.Validate(OneNode(arm, bindings), catalog);
                    Assert.That(
                        rejections.Any(rejection =>
                            rejection.Reason == EffectDefinitionRejectionReason.UndeclaredParameter &&
                            rejection.Detail.Contains("'" + clause + "'")),
                        Is.True,
                        $"{arm} refuses '{clause}' at content load, naming it.");
                }
            }

            Assert.That(refusedAnywhere, Is.GreaterThan(0), "The sweep reached at least one refusal.");
        }

        // -------------------------------------------------------------------------------------------
        // Shape 1 - Mortimer: "another enemy hero in radius, adjacent to a Zombie token"
        // -------------------------------------------------------------------------------------------

        private static EffectInputBinding[] Mortimer(int atLeast = 0)
        {
            var bindings = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                    SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AdjacentToOwnTokenParameterName, EffectValue.Boolean(true))
            };
            if (atLeast > 0)
            {
                bindings.Add(EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AdjacentAtLeastParameterName, EffectValue.Integer(atLeast)));
            }

            return bindings.ToArray();
        }

        [Test]
        public void Mortimer_OffersTheEnemyHeroBesideTheActorsOwnToken()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)));
            PlaceToken(board, new HexCoordinate(3, 0), Actor);

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mortimer(), board, radius: 3),
                Is.EqualTo(new[] { EffectValue.Hero(Enemy) }),
                "The hero beside the token is offered; the one standing in the open is not.");
        }

        [Test]
        public void Mortimer_ATokenSomebodyElseOwns_QualifiesNobody()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)));
            PlaceToken(board, new HexCoordinate(3, 0), SecondEnemy);

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mortimer(), board, radius: 3),
                Is.Empty,
                "\"a Zombie token\" is the actor's own kind; another hero's token is not one.");
        }

        [Test]
        public void Mortimer_TheCountForm_NeedsThatManyQualifyingNeighbours()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)));
            PlaceToken(board, new HexCoordinate(3, 0), Actor);

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mortimer(atLeast: 2), board, radius: 3),
                Is.Empty,
                "One token is not \"two or more\".");

            PlaceToken(board, new HexCoordinate(3, -1), Actor);
            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mortimer(atLeast: 2), board, radius: 3),
                Is.EqualTo(new[] { EffectValue.Hero(Enemy) }),
                "Two tokens beside him are.");
        }

        [Test]
        public void Mortimer_AnImmuneHero_IsNotOffered()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)));
            PlaceToken(board, new HexCoordinate(3, 0), Actor);

            Assert.That(
                Offered(
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mortimer(), board, radius: 3,
                    modifications: ImmunityFor(Enemy, RuleActivity.Skill)),
                Is.Empty,
                "The clause narrows a TARGETING question, so p.18's immunity still filters it.");
        }

        [Test]
        public void Mortimer_TheAnswerIsRederived_AfterTheTokenIsGone()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)));
            var token = PlaceToken(board, new HexCoordinate(3, 0), Actor);
            var probe = Probe(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mortimer(), PrintedValueChannel.Radius, 3);
            var handler = Handler(SyntheticTargetingCapabilities.RadiusHeroTargetV1);

            Assert.That(
                handler.Execute(probe.Context(board)).Kind,
                Is.EqualTo(EffectExecutionResultKind.NeedsChoice));

            board.CommitValidatedTokenReclamation(token);
            Assert.That(
                handler.Resume(probe.Context(board), new[] { EffectValue.Hero(Enemy) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "What was true when the question opened is not what the answer applies to.");
        }

        // -------------------------------------------------------------------------------------------
        // Shape 2 - Mrak: "an enemy hero in radius adjacent to terrain, or to a rock token"
        // -------------------------------------------------------------------------------------------

        private static EffectInputBinding[] Mrak() => new[]
        {
            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
            EffectInputBinding.FromClosedVocabulary(
                SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
            EffectInputBinding.FromLiteral(
                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
            EffectInputBinding.FromLiteral(
                SyntheticTargetingCapabilities.AdjacentToTerrainParameterName, EffectValue.Boolean(true)),
            EffectInputBinding.FromLiteral(
                SyntheticTargetingCapabilities.AdjacentToOwnTokenParameterName, EffectValue.Boolean(true))
        };

        [Test]
        public void Mrak_EitherKindOfNeighbour_Qualifies()
        {
            var board = Board(
                Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)),
                terrain: new[] { new HexCoordinate(3, 0) });
            PlaceToken(board, new HexCoordinate(1, 2), Actor);

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mrak(), board, radius: 3),
                Is.EquivalentTo(new[] { EffectValue.Hero(Enemy), EffectValue.Hero(SecondEnemy) }),
                "One printed \"or\": the hero beside terrain and the hero beside a rock both qualify.");
        }

        [Test]
        public void Mrak_NeitherKindOfNeighbour_QualifiesNobody()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)));
            PlaceToken(board, new HexCoordinate(1, 2), Enemy);

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mrak(), board, radius: 3),
                Is.Empty,
                "No terrain beside either, and the only token is not the actor's.");
        }

        [Test]
        public void Mrak_TheDiskStillBoundsTheOffer()
        {
            var board = Board(
                Heroes(enemyAt: new HexCoordinate(3, 0), secondEnemyAt: new HexCoordinate(0, 2)),
                terrain: new[] { new HexCoordinate(4, 0) },
                radius: 5);

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mrak(), board, radius: 3),
                Is.EqualTo(new[] { EffectValue.Hero(Enemy) }),
                "At the disk's edge he is in.");
            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, Mrak(), board, radius: 2),
                Is.Empty,
                "One space further than the Radius he is out, whatever stands beside him.");
        }

        // -------------------------------------------------------------------------------------------
        // Shape 3 - Trinkets: a second centre, and a moved centre
        // -------------------------------------------------------------------------------------------

        private static readonly HexCoordinate Turret = new HexCoordinate(4, 0);

        [Test]
        public void Trinkets_AUnitInRangeOfBothYouAndTheTurret()
        {
            var board = Board(
                Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)),
                minions: new[] { Minion(EnemyMinion, new HexCoordinate(2, -1), TeamSide.Blue) },
                radius: 5);
            PlaceToken(board, Turret, Actor);

            var bindings = new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AlsoWithinReachOfParameterName, EffectValue.Space(Turret))
            };

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeUnitTargetV1, bindings, board, range: 3),
                Is.EqualTo(new[] { EffectValue.Hero(Enemy), EffectValue.Minion(EnemyMinion) }),
                "Both are within three spaces of Trinkets and of the Turret; the hero four spaces from " +
                "the Turret is in his range and out of its.");
        }

        [Test]
        public void Trinkets_AnEnemyHeroInRadiusOfTheTurret()
        {
            var board = Board(
                Heroes(enemyAt: new HexCoordinate(5, -1), secondEnemyAt: new HexCoordinate(-1, 1)),
                radius: 6);
            PlaceToken(board, Turret, Actor);

            var bindings = new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.MeasuredFromParameterName, EffectValue.Space(Turret))
            };

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RadiusHeroTargetV1, bindings, board, radius: 2),
                Is.EqualTo(new[] { EffectValue.Hero(Enemy) }),
                "The disk is the Turret's: the hero beside it is in, the hero beside Trinkets is out.");
        }

        [Test]
        public void Trinkets_AMovedCentre_NeverOffersTheActor_UnlessTheSentenceIncludesThem()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(0, 3), secondEnemyAt: new HexCoordinate(-3, 3)));
            PlaceToken(board, new HexCoordinate(1, 0), Actor);

            EffectInputBinding[] Bindings(bool includeSelf) => new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(includeSelf)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeStraightLineParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.MeasuredFromParameterName,
                    EffectValue.Space(new HexCoordinate(1, 0)))
            };

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1, Bindings(false), board, range: 1),
                Is.EqualTo(new[] { EffectValue.Space(new HexCoordinate(1, 0)) }),
                "The object on the moved centre is offered; the actor beside it is not.");
            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1, Bindings(true), board, range: 1),
                Is.EquivalentTo(new[]
                {
                    EffectValue.Space(Origin), EffectValue.Space(new HexCoordinate(1, 0))
                }),
                "and the actor is offered only where the sentence includes them.");
        }

        [Test]
        public void Trinkets_TheTurretsSpace_BindsFromAnEarlierNodesOutput()
        {
            var first = new EffectNodeId("composable-turret");
            var second = new EffectNodeId("composable-both");
            var root = new EffectNodeId("composable-root");
            var definition = new EffectDefinition(
                new EffectDefinitionId("composable-node-output"),
                "rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { first, second }),
                    new EffectNodeDefinition(
                        first, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeStraightLineParameterName,
                                EffectValue.Boolean(false))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        second, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeUnitTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticTargetingCapabilities.AlsoWithinReachOfParameterName,
                                first, "chosen", EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                        })
                });

            Assert.That(
                EffectDefinitionValidator.Validate(definition, MatchContentCatalogs.ForThisBuild().Capabilities),
                Is.Empty,
                "A second centre published by an earlier node binds at content load.");
        }

        // -------------------------------------------------------------------------------------------
        // Shape 4 - unions and narrowings of the object kinds
        // -------------------------------------------------------------------------------------------

        private static EffectInputBinding[] ObjectSelection(params EffectInputBinding[] clauses) =>
            new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeStraightLineParameterName, EffectValue.Boolean(false))
            }.Concat(clauses).ToArray();

        private static AdjacentMoveAuthority MixedBoard()
        {
            var board = Board(
                Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(-4, 4)),
                minions: new[]
                {
                    Minion(EnemyMinion, new HexCoordinate(0, 2), TeamSide.Blue),
                    Minion(FriendlyMinion, new HexCoordinate(-2, 0), TeamSide.Red)
                },
                radius: 5);
            PlaceToken(board, new HexCoordinate(0, -2), Actor);
            PlaceToken(board, new HexCoordinate(2, -2), Enemy);
            return board;
        }

        [Test]
        public void ATokenOrAnEnemyUnit_IsOneRelation()
        {
            var bindings = ObjectSelection(EffectInputBinding.FromClosedVocabulary(
                SyntheticTargetingCapabilities.TargetRelationParameterName,
                SourceTeamRelation.OpposingSourceOrUnaligned));

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1, bindings, MixedBoard(), range: 2),
                Is.EquivalentTo(new[]
                {
                    EffectValue.Space(new HexCoordinate(2, 0)),
                    EffectValue.Space(new HexCoordinate(0, 2)),
                    EffectValue.Space(new HexCoordinate(0, -2)),
                    EffectValue.Space(new HexCoordinate(2, -2))
                }),
                "The enemy hero, the enemy minion and both tokens - and not the friendly minion.");
        }

        [Test]
        public void AZombieTokenInRange_IsTheActorsOwnTokensAlone()
        {
            var bindings = ObjectSelection(
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                    PrintedIconObjectKind.TokensOnly),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.OwnTokensOnlyParameterName, EffectValue.Boolean(true)));

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1, bindings, MixedBoard(), range: 2),
                Is.EqualTo(new[] { EffectValue.Space(new HexCoordinate(0, -2)) }),
                "No unit, and not the enemy's token.");
        }

        [Test]
        public void UnitsOnly_OffersNoToken()
        {
            var bindings = ObjectSelection(EffectInputBinding.FromClosedVocabulary(
                SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                PrintedIconObjectKind.UnitsOnly));

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1, bindings, MixedBoard(), range: 2),
                Is.EquivalentTo(new[]
                {
                    EffectValue.Space(new HexCoordinate(2, 0)),
                    EffectValue.Space(new HexCoordinate(0, 2)),
                    EffectValue.Space(new HexCoordinate(-2, 0))
                }),
                "Every unit in range, of either side, and no token.");
        }

        [Test]
        public void TheUnboundObjectSelection_IsExactlyTheWholeDisk()
        {
            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1, ObjectSelection(), MixedBoard(), range: 2),
                Is.EquivalentTo(new[]
                {
                    EffectValue.Space(new HexCoordinate(2, 0)),
                    EffectValue.Space(new HexCoordinate(0, 2)),
                    EffectValue.Space(new HexCoordinate(-2, 0)),
                    EffectValue.Space(new HexCoordinate(0, -2)),
                    EffectValue.Space(new HexCoordinate(2, -2))
                }),
                "A selection binding none of 8.88's clauses offers what it always offered.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.91 - a token named by its kind, and the kind a facedown token hides
        // -------------------------------------------------------------------------------------------

        private static EffectInputBinding[] TokensOfKind(TokenDefinitionId kind) =>
            ObjectSelection(
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                    PrintedIconObjectKind.TokensOnly),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.TokenKindParameterName, EffectValue.TokenKind(kind)));

        [Test]
        public void AFacedownToken_ShowsItsFacedownFace_ToEveryoneButItsOwner()
        {
            var mineAt = new HexCoordinate(2, 0);
            var enemysMine = Board(Heroes(enemyAt: new HexCoordinate(-3, 3), secondEnemyAt: new HexCoordinate(0, 3)));
            PlaceDefinition(enemysMine, mineAt, Enemy, MinComponentContent.MineBlastDefinition);

            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1,
                    TokensOfKind(MinComponentContent.MineBlastDefinition.Id), enemysMine, range: 2),
                Is.Empty,
                "Another hero's card cannot find which facedown piece is a live Mine by asking for one.");
            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1,
                    TokensOfKind(MinComponentContent.MineFacedownDefinition.Id), enemysMine, range: 2),
                Is.EqualTo(new[] { EffectValue.Space(mineAt) }),
                "It sees the facedown face, which is what the table sees.");

            var ownMine = Board(Heroes(enemyAt: new HexCoordinate(-3, 3), secondEnemyAt: new HexCoordinate(0, 3)));
            PlaceDefinition(ownMine, mineAt, Actor, MinComponentContent.MineBlastDefinition);
            Assert.That(
                Offered(SyntheticTargetingCapabilities.RangeTargetV1,
                    TokensOfKind(MinComponentContent.MineBlastDefinition.Id), ownMine, range: 2),
                Is.EqualTo(new[] { EffectValue.Space(mineAt) }),
                "Its owner knows what she placed.");
        }

        private static void PlaceDefinition(
            AdjacentMoveAuthority board, HexCoordinate at, HeroId owner, TokenDefinition definition) =>
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId($"composable-{definition.Id.Value}-{at.Q}-{at.R}"),
                definition,
                owner,
                owner,
                new CardInstanceId("composable-kind-source"),
                at));

        // -------------------------------------------------------------------------------------------
        // The geometry clauses on an arm that never had them
        // -------------------------------------------------------------------------------------------

        [Test]
        public void StraightLineAndMaximumReach_ComposeOnTheRadiusUnitArm()
        {
            var board = Board(
                Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(1, 1)),
                minions: new[] { Minion(EnemyMinion, new HexCoordinate(0, -2), TeamSide.Blue) });

            EffectInputBinding[] Bindings(params EffectInputBinding[] clauses) => new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource)
            }.Concat(clauses).ToArray();

            Assert.That(
                Offered(
                    SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                    Bindings(EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.RequireStraightLineParameterName, EffectValue.Boolean(true))),
                    board, radius: 2),
                Is.EqualTo(new[] { EffectValue.Hero(Enemy), EffectValue.Minion(EnemyMinion) }),
                "(1,1) shares no file with the actor; the other two do.");
            Assert.That(
                Offered(
                    SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                    Bindings(EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.AtMaximumReachParameterName, EffectValue.Boolean(true))),
                    board, radius: 2),
                Is.EqualTo(new[] { EffectValue.Hero(SecondEnemy), EffectValue.Hero(Enemy), EffectValue.Minion(EnemyMinion) }),
                "Every one of them stands exactly two spaces away.");
            Assert.That(
                Offered(
                    SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                    Bindings(EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.AtMaximumReachParameterName, EffectValue.Boolean(true))),
                    board, radius: 3),
                Is.Empty,
                "and none of them three.");
        }

        [Test]
        public void NoCardsInHand_ComposesOnTheRadiusHeroArm()
        {
            var board = Board(Heroes(enemyAt: new HexCoordinate(2, 0), secondEnemyAt: new HexCoordinate(0, 2)));
            var bindings = new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.RequiresEmptyHandParameterName, EffectValue.Boolean(true))
            };

            var enemyCard = CardInstance.CreateInHand(
                new CardInstanceId("composable-enemy-hand"), ProbeReference, Enemy);

            Assert.That(
                Offered(
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1, bindings, board, radius: 3,
                    extraCards: new[] { enemyCard }),
                Is.EqualTo(new[] { EffectValue.Hero(SecondEnemy) }),
                "Gydion's \"with no cards in hand\": the hero holding a card is not offered.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticTargetingCapabilities.Handlers().Single(handler => handler.Reference.Equals(reference));

        /// <summary>
        /// A binding of <paramref name="parameter"/> to a legal value of its declared kind - through its
        /// closed vocabulary where it declares one, which is the only way content may bind an enum.
        /// </summary>
        private static EffectInputBinding LegalBinding(EffectCapabilityParameter parameter)
        {
            if (!parameter.IntegerDomain.IsDefined)
            {
                return EffectInputBinding.FromLiteral(parameter.Name, LegalValue(parameter));
            }

            var vocabulary = parameter.IntegerDomain.VocabularyType;
            return (EffectInputBinding)typeof(EffectInputBinding)
                .GetMethod(nameof(EffectInputBinding.FromClosedVocabulary))
                .MakeGenericMethod(vocabulary)
                .Invoke(null, new[]
                {
                    parameter.Name,
                    Enum.ToObject(vocabulary, parameter.IntegerDomain.LegalValues.Last())
                });
        }

        private static EffectValue LegalValue(EffectCapabilityParameter parameter)
        {
            switch (parameter.ValueKind)
            {
                case EffectValueKind.Boolean:
                    return EffectValue.Boolean(false);
                case EffectValueKind.Integer:
                    return EffectValue.Integer(parameter.IntegerDomain.IsDefined
                        ? parameter.IntegerDomain.LegalValues.Last()
                        : 0);
                case EffectValueKind.Hero:
                    return EffectValue.Hero(Enemy);
                case EffectValueKind.Minion:
                    return EffectValue.Minion(EnemyMinion);
                case EffectValueKind.Coordinate:
                    return EffectValue.Space(Origin);
                case EffectValueKind.TokenKind:
                    return EffectValue.TokenKind(new TokenDefinitionId("composable-test-token"));
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameter), parameter.ValueKind, null);
            }
        }

        private static EffectInputBinding[] RequiredBindings(EffectCapabilityReference arm) =>
            Handler(arm).Schema.Parameters
                .Where(parameter => parameter.IsRequired)
                .Select(LegalBinding)
                .ToArray();

        private static EffectDefinition OneNode(EffectCapabilityReference arm, IEnumerable<EffectInputBinding> bindings)
        {
            var node = new EffectNodeId("composable-select");
            return new EffectDefinition(
                new EffectDefinitionId("composable-shape"),
                "rev-1",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, arm,
                        inputs: bindings)
                });
        }

        /// <summary>
        /// The candidates a node declared from <paramref name="bindings"/> offers on
        /// <paramref name="board"/>, after asserting the node binds at content load - so every shape
        /// here is definition data a card could carry, and nothing reaches the handler that a card
        /// could not.
        /// </summary>
        private static EffectValue[] Offered(
            EffectCapabilityReference arm,
            EffectInputBinding[] bindings,
            AdjacentMoveAuthority board,
            int radius = 0,
            int range = 0,
            RuleModificationSet modifications = null,
            IEnumerable<CardInstance> extraCards = null)
        {
            var probe = Probe(
                arm,
                bindings,
                radius > 0 ? PrintedValueChannel.Radius : PrintedValueChannel.Range,
                radius > 0 ? radius : range,
                extraCards);
            var result = Handler(arm).Execute(probe.Context(board, modifications));
            return result.Kind == EffectExecutionResultKind.NeedsChoice
                ? result.Choice.Candidates.ToArray()
                : Array.Empty<EffectValue>();
        }

        private static readonly CardDefinitionReference ProbeReference =
            new CardDefinitionReference(new CardDefinitionId("composable-probe"), "rev-1");

        private static ProbeCard Probe(
            EffectCapabilityReference arm,
            EffectInputBinding[] bindings,
            PrintedValueChannel channel,
            int value,
            IEnumerable<CardInstance> extraCards = null)
        {
            var definitionText = OneNode(arm, bindings);
            Assert.That(
                EffectDefinitionValidator.Validate(definitionText, MatchContentCatalogs.ForThisBuild().Capabilities),
                Is.Empty,
                "The shape is definition data that binds at content load.");

            var action = new ActionDefinition(
                new ActionDefinitionId("composable-probe-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(channel, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                definitionText);
            var definition = new CardDefinition(
                ProbeReference.DefinitionId,
                ProbeReference.ContentRevision,
                "TEST - composable selection probe",
                new InitiativeValue(9),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { action });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("composable-probe-instance"), definition.Reference, Actor);

            var inputs = bindings.ToDictionary(binding => binding.ParameterName, binding => binding.Literal);
            return new ProbeCard(
                definition,
                new[] { card }.Concat(extraCards ?? Array.Empty<CardInstance>()).ToArray(),
                action,
                inputs);
        }

        private sealed class ProbeCard
        {
            private readonly CardDefinition definition;
            private readonly CardInstance[] cards;
            private readonly ActionDefinition action;
            private readonly IReadOnlyDictionary<string, EffectValue> inputs;

            public ProbeCard(
                CardDefinition definition,
                CardInstance[] cards,
                ActionDefinition action,
                IReadOnlyDictionary<string, EffectValue> inputs)
            {
                this.definition = definition;
                this.cards = cards;
                this.action = action;
                this.inputs = inputs;
            }

            public EffectCapabilityContext Context(
                AdjacentMoveAuthority board, RuleModificationSet modifications = null)
            {
                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                return new EffectCapabilityContext(
                    new EffectNodeId("composable-select"),
                    EffectFailurePolicy.Mandatory,
                    Actor,
                    Actor,
                    cards[0].Id,
                    cards[0].DefinitionReference,
                    action.Id,
                    inputs,
                    1,
                    1,
                    participants,
                    null,
                    modifications ?? RuleModificationSet.Empty,
                    EffectCardState.Of(new[] { definition }, cards),
                    ActionKind.Skill,
                    new MatchComponents(
                        SyntheticComponentDefinitions.CreateCatalog(),
                        board,
                        new MarkerAttachmentRegister(),
                        board,
                        board,
                        board,
                        board),
                    new HeroItemState(participants));
            }
        }

        private static HeroOccupancy[] Heroes(HexCoordinate enemyAt, HexCoordinate secondEnemyAt) =>
            new[]
            {
                new HeroOccupancy(Actor, Origin),
                new HeroOccupancy(Enemy, enemyAt),
                new HeroOccupancy(SecondEnemy, secondEnemyAt)
            };

        private static AdjacentMoveAuthority Board(
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<HexCoordinate> terrain = null,
            IEnumerable<QuickGameMinionSetupState> minions = null,
            int radius = 4)
        {
            var terrainSpaces = (terrain ?? Array.Empty<HexCoordinate>()).ToArray();
            var playable = Disk(radius).Where(space => !terrainSpaces.Contains(space)).ToArray();
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, playable, terrainSpaces),
                heroes.ToArray(),
                Array.Empty<ObstacleOccupancy>(),
                (minions ?? Array.Empty<QuickGameMinionSetupState>()).ToArray());
        }

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius) yield return new HexCoordinate(q, r);
                }
            }
        }

        private static QuickGameMinionSetupState Minion(MinionId id, HexCoordinate coordinate, TeamSide side) =>
            new QuickGameMinionSetupState(
                id,
                new TeamId(side == TeamSide.Red
                    ? StagedBoardParticipants.RedTeamIdValue
                    : StagedBoardParticipants.BlueTeamIdValue),
                side,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId($"composable-spawn-{id.Value}")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);

        /// <summary>A test token with supply enough for every board here; its kind means nothing.</summary>
        private static readonly TokenDefinition TestToken = new TokenDefinition(
            new TokenDefinitionId("composable-test-token"),
            1,
            "TEST - Composable token",
            6,
            Array.Empty<TokenPropertyDefinition>(),
            TokenRemovalCondition.RetainedAtEndOfRound);

        private static TokenInstanceId PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate, HeroId owner)
        {
            var id = new TokenInstanceId($"composable-token-{coordinate.Q}-{coordinate.R}");
            board.CommitValidatedTokenPlacement(new PlacedToken(
                id,
                TestToken,
                owner,
                owner,
                new CardInstanceId("composable-token-source"),
                coordinate));
            return id;
        }

        /// <summary>
        /// A live modification set carrying one immunity, built the way a match builds it - from an
        /// Active effect in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"composable-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"Composable{activity}Immunity",
                $"composable-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"composable-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("composable-immunity-source"),
                new ActionDefinitionId("composable-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                participants,
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None,
                RuleObject.None,
                null,
                null);
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the case proves nothing.");
            return set;
        }
    }
}
