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
    /// Milestone 8.89. A differential audit of the hero-local selections against milestone 8.88's
    /// composable printed-icon family: each hero-local capability that reads a printed disk and narrows
    /// it is asked the same question as the family expression of its printed phrase, on seeded
    /// generated boards, and the two answers must agree - or differ exactly where the rules say they
    /// should.
    ///
    /// <para><b>Why this finds defects the per-hero fixtures cannot.</b> Each hero's fixtures were
    /// written against the boards that hero's author imagined. A copied reading of a shared phrase can
    /// drift on a board nobody staged - a token owned by the other side, terrain at the disk's edge, an
    /// immune hero beside a qualifying obstacle - and a comparison over generated boards reaches those
    /// without anyone having to imagine them.</para>
    ///
    /// <para><b>What is compared, and the one rule that separates presence from selection.</b> p.19:
    /// "Effects that check for unit presence will count immune units", while p.18 bars targeting an
    /// immune unit. So a PRESENCE capability must agree with the family's selection being non-empty on
    /// every board without immunity, and may only say "present" where the selection is empty when an
    /// immune hero is the reason. A SELECTION capability must offer exactly the family's candidates.
    /// Immunities are generated as All-activity immunities, so a capability asking two activities (the
    /// enclosing Skill and a Swap) and the family asking one read the same answer.</para>
    ///
    /// <para>The full inventory, including every hero-local selection the family cannot express and
    /// the clause it lacks, is recorded with milestone 8.89 in the roadmap. Unity-independent, so it
    /// runs in the cross-platform harness as well as the Windows tier.</para>
    /// </summary>
    public sealed class SelectionDifferentialAuditTests
    {
        private const int Boards = 300;

        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MapId Map = new MapId("selection-differential");
        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        // -------------------------------------------------------------------------------------------
        // Presence capabilities
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Brynn's `brynn-enemy-hero-in-radius-adjacent-to-obstacles@1` - "an enemy hero in radius
        /// who is adjacent to 3 or more obstacles" asked as a presence - against the Radius hero arm
        /// with `adjacentToThreeOrMoreObstacles`.
        /// </summary>
        [Test]
        public void BrynnsObstaclePresence_AgreesWithTheFamilyOnEveryBoard()
        {
            var family = new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName,
                    EffectValue.Boolean(true))
            };

            ComparePresence(
                BrynnComponentContent.EnemyHeroInRadiusAdjacentToObstaclesV1,
                BrynnComponentContent.PresentOutputName,
                family);
        }

        /// <summary>
        /// The roster's `enemy-hero-with-discard-present@1` - "if an enemy hero in radius has a card in
        /// the discard" - against the Radius hero arm with `hasCardInDiscard`.
        /// </summary>
        [Test]
        public void TheEnemyHeroWithDiscardPresence_AgreesWithTheFamilyOnEveryBoard()
        {
            var family = new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.HasCardInDiscardParameterName, EffectValue.Boolean(true))
            };

            ComparePresence(
                SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1,
                SyntheticRosterCapabilities.PresentOutput,
                family);
        }

        // -------------------------------------------------------------------------------------------
        // Selection capabilities
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Hanu's `hanu-friendly-unit-pair@1` - "two friendly units in radius" - whose POOL is the
        /// Radius unit arm with an allied relation, asked as a Swap. The pair needs two; where the
        /// family offers fewer, the pair is impossible.
        /// </summary>
        [Test]
        public void HanusFriendlyPairPool_IsTheFamilysAlliedUnitsInRadius()
        {
            var family = new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Swap),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.AlliedWithSource)
            };

            var reached = 0;
            ForEveryBoard((board, world, seed) =>
            {
                var expected = FamilyCandidates(SyntheticTargetingCapabilities.RadiusUnitTargetV1, family, world);
                var result = Capability(HanuComponentContent.FriendlyUnitPairV1).Execute(world.Context(board));
                if (expected.Length < 2)
                {
                    Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                        $"seed {seed}: the family offers {expected.Length}, so no pair exists.");
                    return;
                }

                reached++;
                Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice), $"seed {seed}");
                Assert.That(result.Choice.Candidates, Is.EquivalentTo(expected), $"seed {seed}: the pair's pool.");
            });

            Assert.That(reached, Is.GreaterThan(10), "Enough boards offered a pair for the comparison to mean something.");
        }

        /// <summary>
        /// Brynn's `brynn-tracking-swap-subject@1` is a union: "a unit adjacent to you" (a written
        /// distance the family does not express) OR "an enemy hero in radius who is adjacent to 3 or
        /// more obstacles". Its members standing further than one space away must be exactly the
        /// family's Radius hero arm with that clause and `excludeAdjacent`.
        /// </summary>
        [Test]
        public void BrynnsTrackingSwap_ItsRadiusHalfIsTheFamilysObstacleHeroes()
        {
            var family = new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Swap),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(true)),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName,
                    EffectValue.Boolean(true))
            };

            var reached = 0;
            ForEveryBoard((board, world, seed) =>
            {
                var expected = FamilyCandidates(SyntheticTargetingCapabilities.RadiusHeroTargetV1, family, world);
                var result = Capability(BrynnComponentContent.TrackingSwapSubjectV1).Execute(world.Context(board));
                var offered = result.Kind == EffectExecutionResultKind.NeedsChoice
                    ? result.Choice.Candidates.ToArray()
                    : Array.Empty<EffectValue>();
                var radiusHalf = offered
                    .Where(value => value.Kind == EffectValueKind.Hero &&
                        board.TryGetObjectCoordinate(BoardObjectReference.ForHero(value.HeroId), out var at) &&
                        HexDistanceQuery.Calculate(Origin, at) > 1)
                    .ToArray();

                if (expected.Length > 0) reached++;
                Assert.That(radiusHalf, Is.EquivalentTo(expected), $"seed {seed}: the radius half.");
            });

            Assert.That(reached, Is.GreaterThan(5), "Enough boards reached the radius half for the comparison to mean something.");
        }

        // -------------------------------------------------------------------------------------------
        // The comparison machinery
        // -------------------------------------------------------------------------------------------

        private static void ComparePresence(
            EffectCapabilityReference presence, string outputName, EffectInputBinding[] family)
        {
            int agreeing = 0, presentOnly = 0, presentBoards = 0, absentBoards = 0;
            ForEveryBoard((board, world, seed) =>
            {
                var selected = FamilyCandidates(SyntheticTargetingCapabilities.RadiusHeroTargetV1, family, world);
                var result = Capability(presence).Execute(world.Context(board));
                Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied), $"seed {seed}");
                var present = result.Outcomes.Single(fact => fact.OutputName == outputName).Value.BooleanValue;
                if (present) presentBoards++; else absentBoards++;

                if (present == (selected.Length > 0))
                {
                    agreeing++;
                    return;
                }

                // The one legal difference: presence counts an immune hero the selection may not
                // target. Anything else is a disagreement about the printed phrase.
                Assert.That(present && world.AnyImmuneHero, Is.True,
                    $"seed {seed}: presence says {present}, the family selects {selected.Length}, " +
                    $"and no immune hero explains it.");
                presentOnly++;
            });

            Assert.That(agreeing, Is.GreaterThan(Boards / 2), "Most boards agree outright.");
            Assert.That(presentBoards, Is.GreaterThan(10), "Enough boards answer \"present\" to test that arm.");
            Assert.That(absentBoards, Is.GreaterThan(10), "Enough boards answer \"absent\" to test that arm.");
        }

        private static EffectValue[] FamilyCandidates(
            EffectCapabilityReference arm, EffectInputBinding[] bindings, World world)
        {
            var result = Capability(arm).Execute(world.Context(
                world.Board, bindings.ToDictionary(binding => binding.ParameterName, binding => binding.Literal)));
            return result.Kind == EffectExecutionResultKind.NeedsChoice
                ? result.Choice.Candidates.ToArray()
                : Array.Empty<EffectValue>();
        }

        private static IEffectCapabilityHandler Capability(EffectCapabilityReference reference) =>
            MatchContentCatalogs.ForThisBuild().Capabilities.Load(reference);

        private static void ForEveryBoard(Action<AdjacentMoveAuthority, World, int> check)
        {
            for (var seed = 0; seed < Boards; seed++)
            {
                var world = World.Generate(seed);
                check(world.Board, world, seed);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Generated boards
        // -------------------------------------------------------------------------------------------

        private sealed class World
        {
            private static readonly TokenDefinition TestToken = new TokenDefinition(
                new TokenDefinitionId("differential-test-token"),
                1,
                "TEST - Differential token",
                8,
                Array.Empty<TokenPropertyDefinition>(),
                TokenRemovalCondition.RetainedAtEndOfRound);

            private readonly CardDefinition definition;
            private readonly CardInstance[] cards;
            private readonly ActionDefinition action;
            private readonly RuleModificationSet modifications;

            private World(
                AdjacentMoveAuthority board,
                CardDefinition definition,
                CardInstance[] cards,
                ActionDefinition action,
                RuleModificationSet modifications,
                bool anyImmuneHero)
            {
                Board = board;
                this.definition = definition;
                this.cards = cards;
                this.action = action;
                this.modifications = modifications;
                AnyImmuneHero = anyImmuneHero;
            }

            public AdjacentMoveAuthority Board { get; }

            public bool AnyImmuneHero { get; }

            public static World Generate(int seed)
            {
                var random = new Random(seed);
                var radius = 4;
                var spaces = Disk(radius).Where(space => !space.Equals(Origin)).ToList();
                HexCoordinate Take()
                {
                    var index = random.Next(spaces.Count);
                    var chosen = spaces[index];
                    spaces.RemoveAt(index);
                    return chosen;
                }

                // Pieces are drawn close to the actor more often than not, so the Radius 1-3 disks and
                // the three-obstacle clause are both reached on a useful share of boards.
                HexCoordinate Near()
                {
                    var close = spaces.Where(space => HexDistanceQuery.Calculate(Origin, space) <= 3).ToList();
                    var chosen = close.Count > 0 && random.Next(4) > 0 ? close[random.Next(close.Count)] : spaces[random.Next(spaces.Count)];
                    spaces.Remove(chosen);
                    return chosen;
                }

                var terrain = Enumerable.Range(0, random.Next(0, 6)).Select(_ => Near()).ToArray();
                var heroes = new[]
                {
                    new HeroOccupancy(Actor, Origin),
                    new HeroOccupancy(Enemy, Near()),
                    new HeroOccupancy(SecondEnemy, Near())
                };

                var minions = new List<QuickGameMinionSetupState>();
                for (var index = random.Next(0, 4); index > 0; index--)
                {
                    minions.Add(Minion($"red-{index}", Near(), TeamSide.Red));
                }

                for (var index = random.Next(0, 4); index > 0; index--)
                {
                    minions.Add(Minion($"blue-{index}", Near(), TeamSide.Blue));
                }

                var playable = Disk(radius).Where(space => !terrain.Contains(space)).ToArray();
                var board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, playable, terrain),
                    heroes,
                    Array.Empty<ObstacleOccupancy>(),
                    minions.ToArray());

                var owners = new[] { Actor, Enemy, SecondEnemy };
                for (var index = random.Next(0, 5); index > 0; index--)
                {
                    var at = Near();
                    board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId($"differential-token-{seed}-{index}"),
                        TestToken,
                        owners[random.Next(owners.Length)],
                        owners[random.Next(owners.Length)],
                        new CardInstanceId("differential-token-source"),
                        at));
                }

                var action = new ActionDefinition(
                    new ActionDefinitionId("differential-probe-skill"),
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, random.Next(1, 4)) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    ProbeText());
                var definition = new CardDefinition(
                    new CardDefinitionId("differential-probe"),
                    "rev-1",
                    "TEST - differential probe",
                    new InitiativeValue(9),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NotStated,
                    new[] { action });

                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(new CardInstanceId("differential-probe-instance"), definition.Reference, Actor)
                };
                foreach (var hero in owners)
                {
                    if (random.Next(2) == 0)
                    {
                        cards.Add(new CardInstance(
                            new CardInstanceId($"differential-discard-{hero.Value}"),
                            definition.Reference,
                            hero,
                            new CardStateFacts(
                                CardLocation.DiscardPile, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp)));
                    }
                }

                // One board in four carries an All-activity immunity on one enemy hero.
                var immune = random.Next(4) == 0 ? (HeroId?)(random.Next(2) == 0 ? Enemy : SecondEnemy) : null;
                var modifications = immune.HasValue ? ImmunityFor(immune.Value) : RuleModificationSet.Empty;

                return new World(board, definition, cards.ToArray(), action, modifications, immune.HasValue);
            }

            public EffectCapabilityContext Context(
                AdjacentMoveAuthority board, IReadOnlyDictionary<string, EffectValue> inputs = null)
            {
                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                return new EffectCapabilityContext(
                    new EffectNodeId("differential-node"),
                    EffectFailurePolicy.Mandatory,
                    Actor,
                    Actor,
                    cards[0].Id,
                    cards[0].DefinitionReference,
                    action.Id,
                    inputs ?? new Dictionary<string, EffectValue>(),
                    1,
                    1,
                    participants,
                    null,
                    modifications,
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

            private static EffectDefinition ProbeText()
            {
                var node = new EffectNodeId("differential-root");
                return new EffectDefinition(
                    new EffectDefinitionId("differential-probe-text"),
                    "rev-1",
                    node,
                    new[]
                    {
                        new EffectNodeDefinition(
                            node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1,
                            outputs: new[]
                            {
                                new EffectOutputBinding(SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                            })
                    });
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

            private static QuickGameMinionSetupState Minion(string name, HexCoordinate at, TeamSide side) =>
                new QuickGameMinionSetupState(
                    new MinionId($"differential-{name}"),
                    new TeamId(side == TeamSide.Red
                        ? StagedBoardParticipants.RedTeamIdValue
                        : StagedBoardParticipants.BlueTeamIdValue),
                    side,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(Map, new SpawnPointId($"differential-spawn-{name}")),
                    new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                    at);

            private static RuleModificationSet ImmunityFor(HeroId heroId)
            {
                var effect = new ActiveEffectDefinition(
                    "differential-immunity-effect",
                    ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.CardsPlayedAndRevealed,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                    true,
                    "DifferentialImmunity",
                    "differential-immunity",
                    Array.Empty<TimingCheckpoint>(),
                    new[] { RuleModification.ForImmunity(RuleActivity.All) });

                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                var timing = new ActiveEffectLifecycle();
                timing.Create(
                    new ActiveEffectId($"differential-immunity-{heroId.Value}"),
                    effect,
                    heroId,
                    TeamSide.Blue,
                    default,
                    new CardInstanceId("differential-immunity-source"),
                    new ActionDefinitionId("differential-immunity-action"),
                    1,
                    1);
                timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                    new TieBreakerState(TeamSide.Red));

                return RuleModificationQuery.Build(
                    timing,
                    null,
                    participants,
                    new TieBreakerState(TeamSide.Red),
                    UnlockedUltimateSet.None,
                    RuleObject.None,
                    null,
                    null);
            }
        }
    }
}
