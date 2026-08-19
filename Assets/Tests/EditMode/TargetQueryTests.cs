using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8k's reusable targeting contract, the query half of rulebook p.19's target-versus-
    /// presence pair. Targeting applies immunity (p.18: "If a unit is immune, you cannot target that
    /// unit"); presence does not ("effects that check for unit presence ... will count immune units").
    /// The pair is asserted here over the same spaces so the two questions can never drift apart
    /// unnoticed.
    /// </summary>
    public sealed class TargetQueryTests
    {
        private static readonly HeroId Actor = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
        private static readonly MinionId Minion = new MinionId("target-query-minion");
        private static readonly MapId Map = new MapId("synthetic-target-query-map");

        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        [Test]
        public void AnImmuneUnitIsExcludedFromTargeting_WhilePresenceOverTheSameSpacesStillCountsIt()
        {
            // The acceptance pair, stated as one case: p.18 makes immunity a targeting rule and keeps
            // presence separate.
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            var immunity = SkillImmunityFor(Other);

            var targets = TargetQuery.FindTargetsWithin(
                Origin, 1, false, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(), immunity);
            var present = UnitPresenceQuery.FindUnitsWithin(
                Origin, 1, EffectiveUnitKind.Hero, board, immunity);

            Assert.That(
                targets.Select(target => target.HeroId),
                Has.No.Member(Other),
                "The immune unit cannot be targeted.");
            Assert.That(
                present,
                Has.Member(RuleObject.ForHero(Other)),
                "The presence question over the same spaces still counts it.");
        }

        [Test]
        public void WithoutThatImmunity_TheSameUnitIsTargetable()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var targets = TargetQuery.FindTargetsWithin(
                Origin, 1, false, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(),
                RuleModificationSet.Empty);

            Assert.That(targets.Select(target => target.HeroId), Has.Member(Other));
        }

        [Test]
        public void AnImmunityNamingAnotherActivity_DoesNotFilterThisOne()
        {
            // p.18: immunity to a specific action type leaves the unit affected by everything else.
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var targets = TargetQuery.FindTargetsWithin(
                Origin, 1, false, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(),
                ImmunityFor(Other, RuleActivity.Attack));

            Assert.That(targets.Select(target => target.HeroId), Has.Member(Other));
        }

        [Test]
        public void ATokenIsNeverImmunityFiltered_BecauseItIsNotAUnit()
        {
            // p.19: tokens are not units, so no immunity can be addressed to one and the query must
            // not invent a rules object to ask about.
            var board = CreateBoard(otherAt: new HexCoordinate(2, 0));
            PlaceToken(board, new HexCoordinate(1, 0));

            var targets = TargetQuery.FindTargetsWithin(
                Origin, 1, false, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(),
                SkillImmunityFor(Other));

            Assert.That(targets.Single().Kind, Is.EqualTo(BoardObjectKind.Token),
                "The token is targetable regardless of any declared immunity.");
        }

        [Test]
        public void SelfIsAdmittedOnlyWhenTheQuestionIncludesIt()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var without = TargetQuery.FindTargetsWithin(
                Origin, 1, false, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(),
                RuleModificationSet.Empty);
            var with = TargetQuery.FindTargetsWithin(
                Origin, 1, true, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(),
                RuleModificationSet.Empty);

            Assert.That(without.Select(target => target.HeroId), Has.No.Member(Actor),
                "p.19's relationship vocabulary keeps self and another distinct.");
            Assert.That(with.Select(target => target.HeroId), Has.Member(Actor));
        }

        [Test]
        public void TargetsComeBackInCanonicalAxialOrder_AndDistanceCountsThroughObstacles()
        {
            // p.18's Near/Far: distance counts spaces including spaces with obstacles, so the minion
            // two spaces out is in range even with the hero standing between.
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0), minionAt: new HexCoordinate(2, 0));

            var targets = TargetQuery.FindTargetsWithin(
                Origin, 2, true, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(),
                RuleModificationSet.Empty);

            Assert.That(targets.Select(target => target.Kind), Is.EqualTo(new[]
            {
                BoardObjectKind.Hero, BoardObjectKind.Hero, BoardObjectKind.Minion
            }), "Axial order: (0,0) self, (1,0) hero, (2,0) minion.");
        }

        [Test]
        public void TheGuards_RefuseANegativeRangeAndAnUnnamedActivity()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => TargetQuery.FindTargetsWithin(
                Origin, -1, false, RuleObject.ForHero(Actor), RuleActivity.Skill, board,
                PlaceholderOneVsOneParticipants.Create(),
                RuleModificationSet.Empty));
            Assert.Throws<ArgumentException>(() => TargetQuery.FindTargetsWithin(
                Origin, 1, false, RuleObject.ForHero(Actor), RuleActivity.None, board,
                PlaceholderOneVsOneParticipants.Create(),
                RuleModificationSet.Empty));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static AdjacentMoveAuthority CreateBoard(
            HexCoordinate otherAt, HexCoordinate? minionAt = null)
        {
            var coordinates = Enumerable.Range(-3, 8).Select(q => new HexCoordinate(q, 0)).ToArray();
            var map = BoardMapDefinition.CreateSynthetic(Map, 1, coordinates);
            var minions = minionAt == null
                ? Array.Empty<QuickGameMinionSetupState>()
                : new[]
                {
                    new QuickGameMinionSetupState(
                        Minion,
                        new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue),
                        TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("spawn-target-query")),
                        new ConfiguredSpaceReference(
                            Map, new SpaceId($"q{minionAt.Value.Q}_r{minionAt.Value.R}")),
                        minionAt.Value)
                };
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Actor, Origin), new HeroOccupancy(Other, otherAt) },
                Array.Empty<ObstacleOccupancy>(),
                minions);
        }

        private static void PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId($"target-query-token-{coordinate.Q}-{coordinate.R}"),
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(SyntheticComponentDefinitions.HazardV4),
                Actor,
                Actor,
                new CardInstanceId("target-query-token-source"),
                coordinate));

        private static RuleModificationSet SkillImmunityFor(HeroId heroId) =>
            ImmunityFor(heroId, RuleActivity.Skill);

        /// <summary>
        /// A live set carrying one immunity, built the way the match builds it - from an Active effect
        /// in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"target-query-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"TargetQuery{activity}Immunity",
                $"target-query-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"target-query-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("target-query-immunity-source"),
                new ActionDefinitionId("target-query-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                PlaceholderOneVsOneParticipants.Create(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None);
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the cases above prove nothing.");
            return set;
        }
    }
}
