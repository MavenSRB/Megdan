using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Components;
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
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19c. The roster's persistent rule sources, proven on the synthetic shapes rather
    /// than on any hero's card: multi-activity immunity qualified by the actors it protects against, a
    /// `Next Turn` window, additive effective-type overlays, values counted from live state, and values
    /// qualified to the action class they name.
    ///
    /// Every case asks a production query rather than the modification set, because the set answers
    /// every declaration by construction - it is a lookup - and what these families are about is
    /// whether a rule asks. Each also has its negative arm in the same case, since what is new here is
    /// almost entirely about what a declaration does <em>not</em> reach: the ally it does not bar, the
    /// turn it is not yet active in, the hero it does not stop being, the action it does not raise.
    /// </summary>
    public sealed class RosterPersistentSourceTests
    {
        // -----------------------------------------------------------------------------------------
        // immunity@2: several activities, and the actors the declaration protects against
        // -----------------------------------------------------------------------------------------

        [Test]
        public void MultiActivityImmunity_ReachesEveryNamedActivityAndNoOther()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPersistentSourceCapabilities.GuardAlliesDefinition, fixture.Red);
            var guarded = RuleObject.ForMinion(fixture.AllyMinionId);
            var attacker = RuleObject.ForHero(fixture.Blue);

            foreach (var activity in new[]
            {
                RuleActivity.DirectMove, RuleActivity.ObjectPush, RuleActivity.Swap, RuleActivity.Place
            })
            {
                Assert.That(
                    ImmunityQuery.IsImmuneTo(
                        guarded, activity, attacker, fixture.Board, fixture.Participants, modifications),
                    Is.True,
                    $"Brogan's Bulwark shape names {activity}, so an enemy hero may not do it to a " +
                    "guarded ally. An immunity that reached only its first activity would deliver a " +
                    "quarter of the sentence the card prints.");
            }

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    guarded, RuleActivity.Attack, attacker, fixture.Board, fixture.Participants,
                    modifications),
                Is.False,
                "The declaration names four repositioning activities and no Attack. p.18 is explicit " +
                "that a unit immune to a specific type 'is affected by other effects and actions'.");
        }

        [Test]
        public void MultiActivityImmunity_BarsOnlyTheActorsItNames()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPersistentSourceCapabilities.GuardAlliesDefinition, fixture.Red);
            var guarded = RuleObject.ForMinion(fixture.AllyMinionId);

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    guarded, RuleActivity.Swap, RuleObject.ForHero(fixture.Blue), fixture.Board,
                    fixture.Participants, modifications),
                Is.True,
                "The source's enemy is exactly who the declaration names.");

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    guarded, RuleActivity.Swap, RuleObject.ForHero(fixture.Red), fixture.Board,
                    fixture.Participants, modifications),
                Is.False,
                "Brogan's Bulwark says 'by enemy heroes'. A teammate repositioning a guarded ally is " +
                "not what the card stops, and an immunity that dropped the qualification would stop " +
                "the hero's own side from helping.");

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    guarded, RuleActivity.Swap, RuleObject.None, fixture.Board, fixture.Participants,
                    modifications),
                Is.False,
                "Asked with nobody acting, a qualification about who is acting cannot be met - the " +
                "same reading the restriction family has taken since milestone 8.7.");
        }

        [Test]
        public void AnImmunityNeverReachesTheAffectedObjectsOwnActions()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPersistentSourceCapabilities.ShadowedDefinition, fixture.Red,
                activateNextTurn: true);

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForHero(fixture.Red), RuleActivity.Attack, RuleObject.ForHero(fixture.Red),
                    fixture.Board, fixture.Participants, modifications),
                Is.False,
                "p.18 closes with 'You are never immune to your own actions'. It is a rule rather " +
                "than a scope, so no declaration can name it away.");
        }

        // -----------------------------------------------------------------------------------------
        // NextTurn: dormant when created, applying at the next turn's start
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ANextTurnSource_SuppliesNothingUntilTheNextTurnStarts()
        {
            var fixture = SourceFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Create(lifecycle, SyntheticPersistentSourceCapabilities.ShadowedDefinition,
                fixture.Red, roundIndex: 1, turnIndex: 1);

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForHero(fixture.Red), RuleActivity.Attack, RuleObject.ForHero(fixture.Blue),
                    fixture.Board, fixture.Participants, fixture.Build(lifecycle)),
                Is.False,
                "A Next Turn effect is created dormant, so the turn it was made in is untouched.");

            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2, fixture.Red),
                new TieBreakerState(TeamSide.Red));

            Assert.That(
                ImmunityQuery.IsImmuneTo(
                    RuleObject.ForHero(fixture.Red), RuleActivity.Attack, RuleObject.ForHero(fixture.Blue),
                    fixture.Board, fixture.Participants, fixture.Build(lifecycle)),
                Is.True,
                "The next turn's start is the activation anchor the declaration names.");
        }

        // -----------------------------------------------------------------------------------------
        // effective-type-overlay@1: added, not substituted
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AnEffectiveTypeOverlay_AddsItsTypeAndLeavesTheIntrinsicOneStanding()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPersistentSourceCapabilities.PetrifyDefinition, fixture.Red,
                activateNextTurn: true);
            var petrified = RuleObject.ForHero(fixture.Blue);

            Assert.That(
                EffectiveUnitKindQuery.Of(petrified, modifications),
                Is.EquivalentTo(new[] { EffectiveUnitKind.Hero, EffectiveUnitKind.Terrain }),
                "Xargatha's Stone Gaze says 'count as both heroes and terrain'. A replacement would " +
                "have deleted the hero, which is p.17's other family and not this one.");

            Assert.That(
                TerrainPresenceQuery.IsTerrain(fixture.Board, fixture.BlueAt, modifications), Is.True,
                "A rule that asks about terrain must find the petrified hero standing there.");

            Assert.That(
                BattleZoneQuery.FindEffectiveMinionsInside(
                        fixture.Board, fixture.Participants, modifications)
                    .Any(unit => unit.Unit.Equals(petrified)),
                Is.False,
                "Terrain is not a minion. The overlay adds one answer and invents no others.");

            Assert.That(
                fixture.Participants.GetTeamForHero(fixture.Blue).Side, Is.EqualTo(TeamSide.Blue),
                "Ownership is identity rather than a quality a declaration can replace.");

            Assert.That(
                ActionRestrictionQuery.IsRestrictedFrom(
                    fixture.Blue, ActionKind.Movement, modifications, CardColor.None),
                Is.True,
                "The same sentence bars the Movement action, and both halves must end together.");
        }

        [Test]
        public void AnEffectiveTypeOverlay_EndsWhenItsSourceDoes()
        {
            var fixture = SourceFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Create(lifecycle, SyntheticPersistentSourceCapabilities.PetrifyDefinition,
                fixture.Red, roundIndex: 1, turnIndex: 1);
            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2, fixture.Red),
                new TieBreakerState(TeamSide.Red));
            Assert.That(
                TerrainPresenceQuery.IsTerrain(fixture.Board, fixture.BlueAt, fixture.Build(lifecycle)),
                Is.True,
                "The overlay must be standing before its ending can be observed.");

            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(
                    TimingCheckpoint.ControllerHeroDefeated, 1, 2, fixture.Red),
                new TieBreakerState(TeamSide.Red));

            Assert.That(
                TerrainPresenceQuery.IsTerrain(fixture.Board, fixture.BlueAt, fixture.Build(lifecycle)),
                Is.False,
                "p.13 cancels a defeated hero's Active effects, and an overlay is one of them. " +
                "Nothing about the affected hero was changed, so nothing has to be undone.");
        }

        // -----------------------------------------------------------------------------------------
        // modify-value@2: counted from live state, and qualified to an action class
        // -----------------------------------------------------------------------------------------

        [Test]
        public void ACountedValue_IsRecountedAtEveryDerivationAndAbsentAtZero()
        {
            var fixture = SourceFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Create(
                lifecycle, SyntheticPersistentSourceCapabilities.ScaleWithAdjacentEnemiesDefinition,
                fixture.Red, roundIndex: 1, turnIndex: 1);
            var red = RuleObject.ForHero(fixture.Red);

            var beside = fixture.Build(lifecycle);
            Assert.That(
                beside.ValueContributions(red, PrintedValueChannel.Movement).Sum(value => value.Amount),
                Is.EqualTo(1),
                "One enemy stands adjacent, and the declaration is one Movement for each.");
            Assert.That(
                beside.ValueContributions(red, PrintedValueChannel.Initiative).Sum(value => value.Amount),
                Is.EqualTo(1),
                "Both declared channels are counted from the same fact.");

            fixture.MoveBlueAway();
            var apart = fixture.Build(lifecycle);
            Assert.That(
                apart.ValueContributions(red, PrintedValueChannel.Movement), Is.Empty,
                "A count of zero supplies no modification at all rather than one worth nothing - the " +
                "same statement ForValue makes by refusing a zero amount.");
        }

        [Test]
        public void ACountedValueOnAnUltimate_ReachesTheMovementAllowanceItScales()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.BuildWithUltimates(fixture.DynamicValueUltimates());
            var offer = fixture.OfferOf(ActionKind.Movement);

            var withRule = MovementValueQuery.Calculate(
                offer, fixture.CardDefinitions, null, modifications);
            var withoutRule = MovementValueQuery.Calculate(
                offer, fixture.CardDefinitions, null, RuleModificationSet.Empty);

            Assert.That(withRule.Total, Is.EqualTo(withoutRule.Total + 1),
                "Xargatha's Metamorphosis shape raises the allowance p.11 spends, and the allowance " +
                "is composed in one place so the start restriction judges the raised number.");
        }

        [Test]
        public void AQualifiedValue_ReachesOnlyTheActionClassItNames()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPersistentSourceCapabilities.EmpowerBasicAttackDefinition, fixture.Red);
            var basicAttack = fixture.OfferOf(ActionKind.Attack, CardClassification.Basic);
            var otherAction = fixture.OfferOf(ActionKind.Movement);

            Assert.That(
                AttackValueQuery.Calculate(basicAttack, fixture.CardDefinitions, null, modifications)
                    .Total,
                Is.EqualTo(
                    AttackValueQuery.Calculate(
                        basicAttack, fixture.CardDefinitions, null, RuleModificationSet.Empty).Total + 2),
                "Sabina's Big Sodding Gun shape names the basic attack and raises it.");

            Assert.That(
                RangeValueQuery.Calculate(basicAttack, fixture.CardDefinitions, null, modifications)
                    .Total,
                Is.EqualTo(
                    RangeValueQuery.Calculate(
                        basicAttack, fixture.CardDefinitions, null, RuleModificationSet.Empty).Total + 2),
                "Both declared channels reach the action the sentence names.");

            // The negative arm asks the same two channels for an action the sentence does not name.
            // Asking a third channel would prove nothing: the declaration names none, so it would be
            // unmoved whether or not the qualification was read at all.
            Assert.That(
                AttackValueQuery.Calculate(otherAction, fixture.CardDefinitions, null, modifications)
                    .Total,
                Is.EqualTo(
                    AttackValueQuery.Calculate(
                        otherAction, fixture.CardDefinitions, null, RuleModificationSet.Empty).Total),
                "The qualification is the point: an action the sentence does not name is untouched, " +
                "and a consumer admitting by channel alone would raise every total this hero has.");
            Assert.That(
                RangeValueQuery.Calculate(otherAction, fixture.CardDefinitions, null, modifications)
                    .Total,
                Is.EqualTo(
                    RangeValueQuery.Calculate(
                        otherAction, fixture.CardDefinitions, null, RuleModificationSet.Empty).Total),
                "Both halves of the sentence are qualified, so both must be absent from an action it " +
                "does not name.");
        }

        // -----------------------------------------------------------------------------------------
        // p.17 ordering, across two sources on opposite sides
        // -----------------------------------------------------------------------------------------

        [Test]
        public void SimultaneousSources_AreOrderedByTheRevealedSideFirst()
        {
            var fixture = SourceFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Create(lifecycle, SyntheticPersistentSourceCapabilities.EmpowerBasicAttackDefinition,
                fixture.Blue, roundIndex: 1, turnIndex: 1);
            fixture.Create(lifecycle, SyntheticPersistentSourceCapabilities.ScaleWithAdjacentEnemiesDefinition,
                fixture.Red, roundIndex: 1, turnIndex: 1);

            var redFirst = fixture.Build(lifecycle, new TieBreakerState(TeamSide.Red));
            var blueFirst = fixture.Build(lifecycle, new TieBreakerState(TeamSide.Blue));

            Assert.That(
                redFirst.Modifications.First().ControllerTeamSide, Is.EqualTo(TeamSide.Red),
                "p.17 orders simultaneous Active effects the way p.7 resolves tied Initiative, so the " +
                "revealed side comes first.");
            Assert.That(
                blueFirst.Modifications.First().ControllerTeamSide, Is.EqualTo(TeamSide.Blue),
                "Flipping the revealed side must flip the order, or the ordering is a coincidence of " +
                "creation sequence rather than a reading of the page.");
        }

        // -----------------------------------------------------------------------------------------
        // after-action-trigger@2: every basic action, while the basic-Skill condition stays narrow
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheAfterBasicActionTrigger_ReachesEveryBasicActionKind()
        {
            var fixture = SourceFixture.Create();
            var ultimates = fixture.AfterBasicActionUltimates();

            foreach (var kind in new[] { ActionKind.Skill, ActionKind.Movement, ActionKind.Attack })
            {
                Assert.That(
                    UltimateTriggeredEffectSourceQuery.Derive(
                        ultimates, fixture.Board, fixture.Participants,
                        fixture.BasicActionBy(fixture.Red, kind)),
                    Is.Not.Empty,
                    $"Tigerclaw's Cloak and Daggers says 'after you perform a basic action', so a " +
                    $"completed basic {kind} reaches it.");
            }
        }

        [Test]
        public void TheOlderBasicSkillCondition_StillMeansBasicSkill()
        {
            var fixture = SourceFixture.Create();
            var ultimates = fixture.BasicSkillUltimates();

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    ultimates, fixture.Board, fixture.Participants,
                    fixture.BasicActionBy(fixture.Red, ActionKind.Skill)),
                Is.Not.Empty,
                "Wasp's High Voltage triggers after a basic skill, which is unchanged.");

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    ultimates, fixture.Board, fixture.Participants,
                    fixture.BasicActionBy(fixture.Red, ActionKind.Movement)),
                Is.Empty,
                "Widening the member High Voltage reads would have made her Ultimate fire after her " +
                "Movements, which is why milestone 8.19c added a second condition rather than " +
                "loosening the first.");
        }

        [Test]
        public void TheAfterBasicActionTrigger_SuppliesNothingForAnotherHerosAction()
        {
            var fixture = SourceFixture.Create();

            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    fixture.AfterBasicActionUltimates(), fixture.Board, fixture.Participants,
                    fixture.BasicActionBy(fixture.Blue, ActionKind.Skill)),
                Is.Empty,
                "p.16's ability is the controller's own; 'after you perform' names one hero.");
        }

        // -----------------------------------------------------------------------------------------
        // Milestone 8.23: what the trigger's occurrence carries, and what p.10 lets it repeat
        // -----------------------------------------------------------------------------------------

        [Test]
        public void EachOccurrence_NamesTheCompletedActionItFiredFor()
        {
            var fixture = SourceFixture.Create();
            var completed = fixture.BasicActionBy(fixture.Red, ActionKind.Attack);

            var source = UltimateTriggeredEffectSourceQuery.Derive(
                fixture.AfterBasicActionUltimates(), fixture.Board, fixture.Participants,
                completed).Single();

            Assert.That(source.TriggeringBasicAction, Is.EqualTo(completed),
                "Cloak and Daggers offers to repeat what just ended, and PerformedBasicAction is a " +
                "fact the checkpoint produces rather than state anything keeps - so the occurrence " +
                "has to carry it or nothing downstream can name the subject.");
        }

        [Test]
        public void ARepeatedInstance_StillTriggersButOffersNothingToRepeat()
        {
            var fixture = SourceFixture.Create();
            var repeated = fixture.BasicActionBy(
                fixture.Red, ActionKind.Attack, wasRepeatedInstance: true);

            var source = UltimateTriggeredEffectSourceQuery.Derive(
                fixture.AfterBasicActionUltimates(), fixture.Board, fixture.Participants,
                repeated).Single();

            Assert.That(source.TriggeringBasicAction.WasRepeatedInstance, Is.True,
                "A repeated basic action is still a basic action being performed, so the ability " +
                "fires. Wasp's High Voltage depends on exactly that reading.");
            Assert.That(
                PendingAfterActionRepeat.For(
                    repeated, new ActionResolutionId("probe")),
                Is.Null,
                "p.10: \"You cannot repeat a repeated action!\" - so there is no subject to hold, " +
                "and the ability's repeat node finds nothing to offer and skips in silence rather " +
                "than asking a question that could be confirmed to no effect.");
        }

        [Test]
        public void APrintedInstance_ProducesASubjectNamingWhatItRepeats()
        {
            var fixture = SourceFixture.Create();
            var completed = fixture.BasicActionBy(fixture.Red, ActionKind.Movement);

            var subject = PendingAfterActionRepeat.For(
                completed, new ActionResolutionId("probe"));

            Assert.That(subject, Is.Not.Null);
            Assert.That(subject.Kind, Is.EqualTo(ActionKind.Movement),
                "p.9 makes every action on a basic card basic, so the set is wider than the printed " +
                "icons and the kind is what decides which resolver starts the second instance.");
            Assert.That(subject.ActorId, Is.EqualTo(fixture.Red));
            Assert.That(subject.SourceCardInstanceId, Is.EqualTo(completed.SourceCardInstanceId));
            Assert.That(subject.ActionDefinitionId, Is.EqualTo(completed.ActionDefinitionId));
        }

        [Test]
        public void AnOccurrenceDerivedFromNoCompletedAction_CarriesNoSubject()
        {
            var fixture = SourceFixture.Create();

            Assert.That(
                PendingAfterActionRepeat.For(
                    PerformedBasicAction.None, new ActionResolutionId("probe")),
                Is.Null,
                "An Active-effect trigger fires inside a lifecycle rather than after an action, so " +
                "a repeat node reached from one has nothing to repeat and is skipped.");
        }

        // -----------------------------------------------------------------------------------------
        // Fixture
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// One board with a Red hero, a Red ally and a Blue hero standing where each family needs
        /// them: the ally inside the guarded disk, the Blue hero adjacent to Red so a count and an
        /// overlay both have a subject.
        /// </summary>
        private sealed class SourceFixture
        {
            private readonly PlaceholderOneVsOneTestConfiguration configuration;
            private HexCoordinate blueAt;

            private SourceFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                AdjacentMoveAuthority board,
                HeroId red,
                HeroId blue,
                HexCoordinate redAt,
                HexCoordinate blueAt,
                HexCoordinate distantSpace)
            {
                this.configuration = configuration;
                this.blueAt = blueAt;
                Board = board;
                Red = red;
                Blue = blue;
                RedAt = redAt;
                DistantSpace = distantSpace;
            }

            public AdjacentMoveAuthority Board { get; }
            public MatchParticipants Participants => configuration.Participants;
            public IReadOnlyList<CardDefinition> CardDefinitions => configuration.CardDefinitions;
            public HeroId Red { get; }

            public HeroId Blue { get; }
            public HexCoordinate RedAt { get; }
            public HexCoordinate BlueAt => blueAt;
            public HexCoordinate DistantSpace { get; }

            public static SourceFixture Create()
            {
                var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState);
                var board = new AdjacentMoveAuthority(
                    map,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)).ToArray(),
                    Array.Empty<ObstacleOccupancy>(),
                    setup.Minions);

                var red = configuration.Participants.Heroes
                    .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red)
                    .Id;
                var blue = configuration.Participants.Heroes
                    .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                    .Id;

                // Two adjacent empty spaces well away from the Battle Zone's minions, so the counts
                // below are decided by the two heroes this fixture places and by nobody else.
                var pair = map.PlayableCoordinates
                    .Where(board.IsEmptyPlayableSpace)
                    .Where(candidate => !AnyUnitWithin(board, candidate, 2))
                    .SelectMany(first => first.GetNeighbors()
                        .Where(second => board.IsEmptyPlayableSpace(second) &&
                            !AnyUnitWithin(board, second, 2))
                        .Select(second => new { First = first, Second = second }))
                    .FirstOrDefault();
                Assert.That(pair, Is.Not.Null,
                    "The fixture needs two adjacent empty spaces with no other unit within two, or " +
                    "the counted-value cases would be counting the official setup's minions.");

                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(red), pair.First).Accepted, Is.True);
                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(blue), pair.Second).Accepted, Is.True);

                var distant = map.PlayableCoordinates
                    .Where(board.IsEmptyPlayableSpace)
                    .OrderByDescending(candidate => HexDistanceQuery.Calculate(candidate, pair.First))
                    .First();

                // The guarded ally is one of Red's own minions, standing inside the declared disk.
                var ally = board.Minions
                    .Where(minion => minion.TeamSide == TeamSide.Red)
                    .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                    .First();
                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForMinion(ally.MinionId),
                        pair.First.GetNeighbors().First(space =>
                            board.IsEmptyPlayableSpace(space))).Accepted,
                    Is.True,
                    "The guarded ally has to stand inside the disk the declaration draws.");

                return new SourceFixture(
                    configuration, board, red, blue, pair.First, pair.Second, distant)
                {
                    AllyMinionId = ally.MinionId
                };
            }

            /// <summary>The friendly minion the guarded-disk cases protect.</summary>
            public MinionId AllyMinionId { get; private set; }

            private static bool AnyUnitWithin(
                AdjacentMoveAuthority board, HexCoordinate space, int distance) =>
                board.Occupancies.Any(hero =>
                    HexDistanceQuery.Calculate(hero.Coordinate, space) <= distance) ||
                board.Minions.Any(minion =>
                    HexDistanceQuery.Calculate(minion.Coordinate, space) <= distance);

            public void Create(
                ActiveEffectLifecycle lifecycle,
                ActiveEffectDefinition definition,
                HeroId controller,
                int roundIndex,
                int turnIndex) =>
                lifecycle.Create(
                    new ActiveEffectId($"{definition.DefinitionId}:{controller.Value}"),
                    definition,
                    controller,
                    Participants.GetTeamForHero(controller).Side,
                    default,
                    new CardInstanceId($"persistent-source-probe-{controller.Value}"),
                    new ActionDefinitionId("persistent-source-probe-action"),
                    roundIndex,
                    turnIndex);

            /// <summary>One live set carrying exactly one effect, activated if its window is delayed.</summary>
            public RuleModificationSet WithEffect(
                ActiveEffectDefinition definition, HeroId controller, bool activateNextTurn = false)
            {
                var lifecycle = new ActiveEffectLifecycle();
                Create(lifecycle, definition, controller, 1, 1);
                if (activateNextTurn)
                {
                    lifecycle.ApplyCheckpoint(
                        new ActiveEffectCheckpointContext(TimingCheckpoint.TurnStarted, 1, 2, controller),
                        new TieBreakerState(TeamSide.Red));
                }

                return Build(lifecycle);
            }

            public RuleModificationSet Build(
                ActiveEffectLifecycle lifecycle, TieBreakerState? tieBreaker = null) =>
                RuleModificationQuery.Build(
                    lifecycle,
                    Board,
                    Participants,
                    tieBreaker ?? new TieBreakerState(TeamSide.Red),
                    UnlockedUltimateSet.None,
                    RuleObject.None,
                    null,
                    null);

            public RuleModificationSet BuildWithUltimates(UnlockedUltimateSet ultimates) =>
                RuleModificationQuery.Build(
                    null,
                    Board,
                    Participants,
                    new TieBreakerState(TeamSide.Red),
                    ultimates,
                    RuleObject.None,
                    null,
                    null);

            /// <summary>Moves the Blue hero out of every disk and adjacency this fixture measures.</summary>
            public void MoveBlueAway()
            {
                Assert.That(
                    Board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(Blue), DistantSpace).Accepted, Is.True);
                blueAt = DistantSpace;
            }

            public PerformedBasicAction BasicActionBy(
                HeroId actor, ActionKind kind, bool wasRepeatedInstance = false)
            {
                var offer = OfferOf(kind, CardClassification.Basic, actor);
                return PerformedBasicAction.Of(
                    actor, offer.SourceCardInstanceId, offer.ActionDefinitionId, kind,
                    wasRepeatedInstance);
            }

            public ActionOffer OfferOf(
                ActionKind actionKind,
                CardClassification classification = CardClassification.None,
                HeroId actor = default)
            {
                var owner = actor.IsDefined ? actor : Red;
                var offers = ActionOfferQuery.GetAvailableOffers(
                        CardDefinitions, configuration.CardInstances, owner, ActionOfferWindow.NormalPlay)
                    .Where(candidate => KindOf(candidate) == actionKind)
                    .Where(candidate => classification == CardClassification.None ||
                        ClassificationOf(candidate) == classification)
                    .ToArray();
                Assert.That(offers, Is.Not.Empty,
                    $"The fixture hand no longer prints a {classification} {actionKind}, so the case " +
                    "would answer about nothing.");
                return offers[0];
            }

            /// <summary>The Ultimates a hand carrying milestone 8.19c's counted-value shape unlocks.</summary>
            public UnlockedUltimateSet DynamicValueUltimates() =>
                UnlockFor(SyntheticShowcaseHand.CreateRedWithDynamicValueUltimate(Red));

            public UnlockedUltimateSet AfterBasicActionUltimates() =>
                UnlockFor(SyntheticShowcaseHand.CreateRedWithAfterBasicActionUltimate(Red));

            /// <summary>Milestone 7.3's Ultimate hand, whose trigger condition is the basic Skill.</summary>
            public UnlockedUltimateSet BasicSkillUltimates() =>
                UnlockFor(SyntheticShowcaseHand.CreateRedWithAfterBasicSkillUltimate(Red));

            private UnlockedUltimateSet UnlockFor(ConfiguredHand hand)
            {
                var ultimate = hand.AllInstances.Single(card =>
                    hand.Definitions.Single(definition =>
                            definition.Reference.Equals(card.DefinitionReference))
                        .UltimateRule != null);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True,
                    "p.16 unlocks the Ultimate by moving the card beside the dashboard; the set is " +
                    "derived from that position and from nothing else.");
                return UltimateUnlockQuery.Derive(hand.Definitions, hand.AllInstances);
            }

            private ActionKind KindOf(ActionOffer offer) =>
                CardDefinitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind;

            private CardClassification ClassificationOf(ActionOffer offer) =>
                CardDefinitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .Progression.Classification;
        }
    }
}
