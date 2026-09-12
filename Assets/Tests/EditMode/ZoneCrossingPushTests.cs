using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39c. The zone-crossing consequence Cutter's Gold line prints, proved as vocabulary
    /// before any of her content exists: "Push an enemy hero adjacent to you up to 4 spaces; <b>if that
    /// hero is pushed into another zone</b>, that hero discards a card, or is defeated."
    ///
    /// <para><b>It is Wasp's Kinetic Repulse shape, not Sabina's Big Sodding Gun shape</b>, and that
    /// correction is what sized this block. Kinetic Repulse prints "Push up to 2 enemy units adjacent
    /// to you 3 spaces; if a pushed hero is stopped by an obstacle, that hero discards a card, if able"
    /// - a push and a rider about a hero the push did something to, read off the push's own published
    /// pair. Cutter's sentence is that shape with a different rider, so this milestone adds a second
    /// pair beside the stop causes rather than a predicate anywhere else. `after-push-trigger@1` is how
    /// an unlocked ULTIMATE watches a push some other card performed - Big Sodding Gun is Sabina's p.16
    /// permanent ability and performs no push of its own - and nothing here touches it.</para>
    ///
    /// <para><b>The board's zones interleave along every file on purpose.</b> Distance 0-1 and 4-5 are
    /// one zone and 2-3 are another, so a push of three leaves a zone and re-enters it. That is the one
    /// board state that tells "judged on origin against final destination" apart from "judged on what
    /// it crossed", and the reading this milestone takes is the first.</para>
    /// </summary>
    [TestFixture]
    public sealed class ZoneCrossingPushTests
    {
        private static readonly ActionDefinitionId PlankAction = new ActionDefinitionId("probe-plank");
        private static readonly CardDefinitionId PlankCard = new CardDefinitionId("probe-plank-card");
        private static readonly CardInstanceId PlankInstance =
            new CardInstanceId("probe-plank-instance");

        private static readonly MapId Map = new MapId("zone-crossing-board");
        private static readonly ZoneId Inner = new ZoneId("zone-crossing-inner");
        private static readonly ZoneId Outer = new ZoneId("zone-crossing-outer");

        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);
        private static readonly HeroId Pusher = new HeroId("zone-crossing-pusher");
        private static readonly HeroId Victim = new HeroId("zone-crossing-victim");
        private static readonly TeamId RedTeam = new TeamId("zone-crossing-red");
        private static readonly TeamId BlueTeam = new TeamId("zone-crossing-blue");

        private static HexCoordinate East(int steps) =>
            StraightLineQuery.Step(Origin, HexDirection.East, steps);

        private static HexCoordinate West(int steps) =>
            StraightLineQuery.Step(Origin, HexDirection.West, steps);

        /// <summary>
        /// The premise every case below rests on, asserted rather than assumed: East travels along
        /// +Q, which is what makes the zone bands line up with the push.
        /// </summary>
        [Test]
        public void TheFixturesOwnAxis_IsTheOneTheZonesAreBandedOn()
        {
            Assert.That(East(1).Q, Is.EqualTo(1));
            Assert.That(West(1).Q, Is.EqualTo(-1));
            Assert.That(ZoneAt(East(1)), Is.EqualTo(Inner));
            Assert.That(ZoneAt(East(2)), Is.EqualTo(Outer));
            Assert.That(ZoneAt(East(4)), Is.EqualTo(Inner));
        }

        // -------------------------------------------------------------------------------------------
        // The clause
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The consequence fires when and only when the pushed hero's origin and destination zones
        /// differ - proven with two pushes of the SAME distance on one board, one that crosses and one
        /// that does not.
        /// </summary>
        [Test]
        public void TheClause_FiresOnAZoneChangeAndNotOnDistance()
        {
            // Pusher at 0, victim at 1 (inner), pushed one space to 2 (outer): a crossing.
            var crossing = Push(pusherAt: Origin, victimAt: East(1), distance: 1);
            Assert.That(Crossed(crossing), Is.True);
            Assert.That(CrossingHero(crossing), Is.EqualTo(Victim));

            // Pusher at -1, victim at 0 (inner), pushed one space to 1 (inner): the SAME distance on
            // the same map, and no crossing.
            var within = Push(pusherAt: West(1), victimAt: Origin, distance: 1);
            Assert.That(Crossed(within), Is.False,
                "The clause reads zones, not spaces travelled.");
            Assert.That(
                within.Outcomes.Any(fact => fact.OutputName ==
                    SyntheticDisplacementCapabilities.FirstZoneCrossingHeroOutputName),
                Is.False,
                "and names no hero when there was no crossing, exactly as the stopped pair does.");
        }

        /// <summary>
        /// The reading this milestone takes, stated as a case: a push that leaves a zone and re-enters
        /// it has carried nobody into another zone. Origin against final destination, never the spaces
        /// between.
        /// </summary>
        [Test]
        public void APushThatLeavesAZoneAndComesBack_IsNotAZoneCrossing()
        {
            // Victim at 1 (inner), pushed three spaces to 4 (inner again), crossing 2 and 3 (outer).
            var result = Push(pusherAt: Origin, victimAt: East(1), distance: 3);

            Assert.That(Crossed(result), Is.False,
                "It crossed the outer zone and ended back in the inner one, which is not being " +
                "'pushed into another zone'.");
        }

        /// <summary>
        /// p.18's "if you push" is what happened rather than what was tried, so a hero the geometry
        /// left standing has crossed nothing - even where the space beyond the blocker is in another
        /// zone.
        /// </summary>
        [Test]
        public void APushThatDisplacesNothing_FiresNothing()
        {
            var result = Push(
                pusherAt: Origin, victimAt: East(1), distance: 1, blockerAt: East(2));

            Assert.That(Crossed(result), Is.False,
                "The victim never left the inner zone, because the victim never left.");
        }

        /// <summary>
        /// A push that travels its whole distance across a boundary reports the crossing, which is the
        /// ordinary case Walk the Plank prints at up to four spaces.
        /// </summary>
        [Test]
        public void AFullDistancePushAcrossABoundary_ReportsTheCrossing()
        {
            var result = Push(pusherAt: Origin, victimAt: East(1), distance: 2);

            Assert.That(Crossed(result), Is.True);
            Assert.That(CrossingHero(result), Is.EqualTo(Victim));
        }

        // -------------------------------------------------------------------------------------------
        // Nothing written before this milestone moves
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The claim every added output owes: a node that does not declare the new pair is published
        /// nothing new, so every push written before this milestone produces byte-identical facts.
        /// Asserted against the stop-cause pair, which is what those nodes do declare.
        /// </summary>
        [Test]
        public void APushThatDeclaresNoZoneOutputs_PublishesNoneOfThem()
        {
            var result = Push(
                pusherAt: Origin,
                victimAt: East(1),
                distance: 1,
                outputs: new[]
                {
                    new EffectOutputBinding("moved", EffectValueKind.Integer),
                    new EffectOutputBinding(
                        SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName,
                        EffectValueKind.Boolean)
                });

            Assert.That(
                result.Outcomes.Select(fact => fact.OutputName),
                Is.EquivalentTo(new[]
                {
                    "moved",
                    SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName
                }),
                "A push publishes exactly what its node asked for, which is why adding an output " +
                "cannot move a definition written before it existed.");
        }

        /// <summary>
        /// And the stop-cause pair still answers its own question on the same board, so the rider Wasp
        /// already prints is unchanged by the rider this milestone adds.
        /// </summary>
        [Test]
        public void TheStopCausePair_IsUnchanged()
        {
            // Victim at 1 wants two spaces to 3; the blocker at 3 stops it at 2, which is the outer
            // zone - so this one push is both stopped and a crossing.
            var blocked = Push(
                pusherAt: Origin, victimAt: East(1), distance: 2, blockerAt: East(3));

            Assert.That(
                blocked.Outcomes.Single(fact => fact.OutputName ==
                    SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName).Value.BooleanValue,
                Is.True,
                "An obstacle two spaces along still stops the first hero, and still says so.");
            Assert.That(Crossed(blocked), Is.True,
                "and the hero stopped one space short is still in the outer zone, so both riders are " +
                "true of one push at once - which is why they are two pairs rather than one.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static bool Crossed(EffectExecutionResult result) =>
            result.Outcomes.Single(fact => fact.OutputName ==
                SyntheticDisplacementCapabilities.FirstHeroCrossedAZoneOutputName).Value.BooleanValue;

        private static HeroId CrossingHero(EffectExecutionResult result) =>
            result.Outcomes.Single(fact => fact.OutputName ==
                SyntheticDisplacementCapabilities.FirstZoneCrossingHeroOutputName).Value.HeroId;

        private static EffectExecutionResult Push(
            HexCoordinate pusherAt,
            HexCoordinate victimAt,
            int distance,
            HexCoordinate? blockerAt = null,
            IReadOnlyList<EffectOutputBinding> outputs = null)
        {
            var handler = SyntheticDisplacementCapabilities.Handlers()
                .Single(value => value.Reference.Equals(
                    SyntheticDisplacementCapabilities.ObjectPushV3));
            var context = Context(pusherAt, victimAt, distance, blockerAt, outputs);
            var opened = handler.Execute(context);

            // The batch push asks which of the units in reach it takes before it moves anything, so
            // these cases answer that question with the one enemy hero on the board and read the
            // facts the answer publishes.
            Assert.That(opened.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "A batch push opens with its own selection.");
            Assert.That(opened.Choice.Candidates.Count, Is.EqualTo(1),
                "and this board carries exactly one enemy hero, so the answer is not a choice these " +
                "cases have to make.");

            var result = handler.Resume(context, new[] { opened.Choice.Candidates[0] });
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "A push with a legal target applies; these cases are about what it then says.");
            return result;
        }

        private static EffectCapabilityContext Context(
            HexCoordinate pusherAt,
            HexCoordinate victimAt,
            int distance,
            HexCoordinate? blockerAt,
            IReadOnlyList<EffectOutputBinding> outputs)
        {
            var board = CreateBoard(pusherAt, victimAt, blockerAt);
            return new EffectCapabilityContext(
                new EffectNodeId("probe-plank-node"),
                EffectFailurePolicy.Mandatory,
                Pusher,
                Pusher,
                PlankInstance,
                new CardDefinitionReference(PlankCard, "probe-rev-1"),
                PlankAction,
                new Dictionary<string, EffectValue>
                {
                    ["distance"] = EffectValue.Integer(distance),
                    [SyntheticDisplacementCapabilities.LeastDistanceParameterName] =
                        EffectValue.Integer(distance),
                    ["ignoresObstacles"] = EffectValue.Boolean(false),
                    [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                        EffectValue.Integer((int)PushEligiblePieceKind.Unit),
                    [SyntheticDisplacementCapabilities.TargetRelationParameterName] =
                        EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                    [SyntheticDisplacementCapabilities.MaximumTargetsParameterName] =
                        EffectValue.Integer(1)
                },
                1,
                1,
                Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                declaredOutputs: outputs ?? DefaultOutputs());

        }

        private static IReadOnlyList<EffectOutputBinding> DefaultOutputs() =>
            new[]
            {
                new EffectOutputBinding("moved", EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName,
                    EffectValueKind.Boolean),
                new EffectOutputBinding(
                    SyntheticDisplacementCapabilities.FirstStoppedHeroOutputName,
                    EffectValueKind.Hero),
                new EffectOutputBinding(
                    SyntheticDisplacementCapabilities.FirstHeroCrossedAZoneOutputName,
                    EffectValueKind.Boolean),
                new EffectOutputBinding(
                    SyntheticDisplacementCapabilities.FirstZoneCrossingHeroOutputName,
                    EffectValueKind.Hero)
            };

        /// <summary>
        /// A radius-5 disk whose zones interleave by distance: 0-1 and 4-5 are the inner zone, 2-3 the
        /// outer one. Every file behaves alike, so a case names a distance rather than a direction.
        /// </summary>
        private static BoardMapDefinition CreateMap()
        {
            var spaces = Disk(5)
                .Select(at => new BoardMapSpace(
                    SpaceIdFor(at), at, BoardMapSpaceKind.Playable, ZoneAt(at)))
                .ToArray();

            return new BoardMapDefinition(
                Map, 1, "Zone Crossing Board", spaces,
                new[]
                {
                    new BoardMapZone(Inner, "Inner", "lane", null, false, true),
                    new BoardMapZone(Outer, "Outer", "lane", null, false, true)
                },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        /// <summary>
        /// Zones banded ACROSS the push axis and deliberately interleaved: Q up to 1 and Q from 4 are
        /// the inner zone, Q of 2 or 3 the outer one. A push travels East along +Q, so the bands give
        /// a one-space push that crosses, a one-space push that does not, and a three-space push that
        /// leaves a zone and comes back.
        /// </summary>
        private static ZoneId ZoneAt(HexCoordinate at) =>
            at.Q >= 2 && at.Q <= 3 ? Outer : Inner;

        private static AdjacentMoveAuthority CreateBoard(
            HexCoordinate pusherAt, HexCoordinate victimAt, HexCoordinate? blockerAt) =>
            new AdjacentMoveAuthority(
                CreateMap(),
                new[] { new HeroOccupancy(Pusher, pusherAt), new HeroOccupancy(Victim, victimAt) },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                blockerAt.HasValue
                    ? new[]
                    {
                        new QuickGameMinionSetupState(
                            new MinionId("zone-crossing-blocker"),
                            BlueTeam,
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            default,
                            new ConfiguredSpaceReference(Map, SpaceIdFor(blockerAt.Value)),
                            blockerAt.Value)
                    }
                    : Array.Empty<QuickGameMinionSetupState>());

        private static MatchParticipants Participants()
        {
            var pusherPlayer = new PlayerId("zone-crossing-pusher-player");
            var victimPlayer = new PlayerId("zone-crossing-victim-player");
            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                new[] { new Player(pusherPlayer, RedTeam), new Player(victimPlayer, BlueTeam) },
                new[]
                {
                    new Hero(Pusher, pusherPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Victim, victimPlayer, HeroDashboard.RulebookLayout)
                });
        }

        private static SpaceId SpaceIdFor(HexCoordinate at) => new SpaceId($"q{at.Q}_r{at.R}");

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    yield return new HexCoordinate(dq, dr);
                }
            }
        }
    }
}
