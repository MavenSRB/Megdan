using System;
using System.Collections.Generic;
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
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19b's shared target, target-set/pair and line-geometry vocabulary. The actors in
    /// these fixtures stand in for different future roster heroes; no verified definition is added.
    /// </summary>
    public sealed class RosterTargetGeometryCapabilityTests
    {
        private static readonly HeroId Actor =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("roster-target-geometry");
        private static readonly EffectCapabilityCatalog Catalog =
            new EffectCapabilityCatalog(SyntheticRosterCapabilities.Handlers());

        [Test]
        public void TargetSetAndPair_UseOneCanonicalTypedContractAcrossIntrinsicAndCountsAsUnits()
        {
            var minion = Minion("blue-a-minion", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(0, 1));
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(1, 0), new[] { minion });
            var countsAs = Modification(
                RuleObject.ForHero(Other),
                RuleModification.ForCountsAs(EffectiveUnitKind.Minion),
                "roster-counts-as");
            var pairContext = Context(
                board,
                TargetInputs(
                    RosterQueryOrigin.Actor,
                    1,
                    RosterUnitKind.Minion,
                    SourceTeamRelation.OpposingSource),
                countsAs);

            var pair = Handler(SyntheticRosterCapabilities.ChooseTargetPairV1);
            var offered = pair.Execute(pairContext);
            var applied = pair.Resume(pairContext, offered.Choice.Candidates.Reverse().ToArray());

            Assert.That(offered.Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Hero(Other), EffectValue.Minion(minion.MinionId) }),
                "Counts As changes eligibility without changing the miniature's typed identity.");
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(applied.Outcomes.Select(value => value.OutputName),
                Is.EqualTo(new[] { "firstHero", "secondMinion" }),
                "Published pair order is canonical, not the chooser's click order.");

            var set = Handler(SyntheticRosterCapabilities.ChooseTargetSetV1);
            var setInputs = TargetInputs(
                RosterQueryOrigin.Actor, 1, RosterUnitKind.Unit,
                SourceTeamRelation.OpposingSource);
            setInputs["minimum"] = EffectValue.Integer(1);
            setInputs["maximum"] = EffectValue.Integer(2);
            var setContext = Context(board, setInputs);
            var setApplied = set.Resume(setContext, new[]
            {
                EffectValue.Minion(minion.MinionId), EffectValue.Hero(Other)
            });
            Assert.That(setApplied.Outcomes.Select(value => value.OutputName),
                Is.EqualTo(new[] { "selectedCount", "firstHero", "secondMinion" }));
        }

        [Test]
        public void ChooseTarget_ExcludesStandingHeavyImmunity_UnlessTheExactNodeOverrideApplies()
        {
            var heavy = Minion("blue-heavy", TeamSide.Blue, StandardMinionType.Heavy,
                new HexCoordinate(1, 0));
            var escort = Minion("blue-escort", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(1, -1));
            var board = BattleZoneBoard(new[] { heavy, escort });
            var inputs = TargetInputs(
                RosterQueryOrigin.Actor, 1, RosterUnitKind.Minion,
                SourceTeamRelation.OpposingSource);
            inputs["activity"] = EffectValue.Integer((int)RuleActivity.DirectMove);
            var ordinary = Handler(SyntheticRosterCapabilities.ChooseTargetV2)
                .Execute(Context(board, inputs));
            var declared = Handler(SyntheticRosterCapabilities.ChooseTargetV2)
                .Execute(Context(
                    board,
                    inputs,
                    immunityOverrides: new[]
                    {
                        new ImmunityOverrideDeclaration(
                            RuleActivity.DirectMove,
                            ImmunityOverrideReason.PrintedHeavyMinionImmunity)
                    }));

            Assert.That(ordinary.Choice.Candidates.Select(value => value.MinionId),
                Is.EqualTo(new[] { escort.MinionId }),
                "The accompanied Heavy is protected by p.13's standing immunity.");
            Assert.That(declared.Choice.Candidates.Select(value => value.MinionId),
                Is.EqualTo(new[] { escort.MinionId, heavy.MinionId }).Or.EqualTo(
                    new[] { heavy.MinionId, escort.MinionId }));

            var explicitImmunity = Modification(
                RuleObject.ForMinion(heavy.MinionId),
                RuleModification.ForImmunity(RuleActivity.DirectMove),
                "roster-explicit-immunity");
            var stillProtected = Handler(SyntheticRosterCapabilities.ChooseTargetV2)
                .Execute(Context(
                    board,
                    inputs,
                    explicitImmunity,
                    immunityOverrides: new[]
                    {
                        new ImmunityOverrideDeclaration(
                            RuleActivity.DirectMove,
                            ImmunityOverrideReason.PrintedHeavyMinionImmunity)
                    }));
            Assert.That(stillProtected.Choice.Candidates.Select(value => value.MinionId),
                Has.No.Member(heavy.MinionId),
                "The exact Heavy exception never suppresses source-declared immunity.");
        }

        [Test]
        public void ExistingRadiusSelection_ConsumesTheDirectMoveOverrideBeforeTheOperationDoes()
        {
            var heavy = Minion("radius-move-heavy", TeamSide.Blue, StandardMinionType.Heavy,
                new HexCoordinate(1, 0));
            var escort = Minion("radius-move-escort", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(1, -1));
            var board = BattleZoneBoard(new[] { heavy, escort });
            var configuration =
                MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var source = configuration.CardInstances
                .Where(card => card.OwnerId.Equals(Actor))
                .Select(card => new
                {
                    Card = card,
                    Definition = configuration.CardDefinitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference))
                })
                .SelectMany(value => value.Definition.ActionDefinitions.Select(action => new
                {
                    value.Card,
                    value.Definition,
                    Action = action
                }))
                .First(value => value.Action.Kind == ActionKind.Skill &&
                    value.Action.BaseValues.Any(printed =>
                        printed.Channel == PrintedValueChannel.Radius));
            var inputs = new Dictionary<string, EffectValue>
            {
                ["includeSelf"] = EffectValue.Boolean(false),
                ["activity"] = EffectValue.Integer((int)RuleActivity.DirectMove)
            };
            var declaration = new[]
            {
                new ImmunityOverrideDeclaration(
                    RuleActivity.DirectMove,
                    ImmunityOverrideReason.PrintedHeavyMinionImmunity)
            };
            EffectCapabilityContext QueryContext(
                RuleModificationSet modifications,
                IReadOnlyList<ImmunityOverrideDeclaration> overrides) =>
                new EffectCapabilityContext(
                    new EffectNodeId("roster-radius-move-selection"),
                    EffectFailurePolicy.Mandatory,
                    Actor,
                    Actor,
                    source.Card.Id,
                    source.Definition.Reference,
                    source.Action.Id,
                    inputs,
                    3,
                    2,
                    configuration.Participants,
                    null,
                    modifications,
                    EffectCardState.Of(
                        configuration.CardDefinitions, configuration.CardInstances),
                    ActionKind.Skill,
                    new MatchComponents(
                        SyntheticComponentDefinitions.CreateCatalog(), board,
                        new MarkerAttachmentRegister(), board, board, board),
                    immunityOverrides: overrides);

            var handler = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticTargetingCapabilities.RadiusTargetV1);
            var ordinary = handler.Execute(QueryContext(RuleModificationSet.Empty, null));
            var declared = handler.Execute(QueryContext(RuleModificationSet.Empty, declaration));

            Assert.That(ordinary.Choice.Candidates.Select(value => value.Coordinate),
                Has.No.Member(new HexCoordinate(1, 0)));
            Assert.That(declared.Choice.Candidates.Select(value => value.Coordinate),
                Does.Contain(new HexCoordinate(1, 0)));

            var explicitImmunity = Modification(
                RuleObject.ForMinion(heavy.MinionId),
                RuleModification.ForImmunity(RuleActivity.DirectMove),
                "roster-radius-direct-move-immunity");
            var stillProtected = handler.Execute(QueryContext(explicitImmunity, declaration));
            Assert.That(stillProtected.Choice.Candidates.Select(value => value.Coordinate),
                Has.No.Member(new HexCoordinate(1, 0)));
        }

        [Test]
        public void ExistingDirectMoveIdentity_ConsumesTheExactHeavyOverrideWithoutARegistryRevision()
        {
            var heavy = Minion("move-heavy", TeamSide.Blue, StandardMinionType.Heavy,
                new HexCoordinate(1, 0));
            var escort = Minion("move-escort", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(1, -1));
            var board = BattleZoneBoard(new[] { heavy, escort });
            var inputs = new Dictionary<string, EffectValue>
            {
                ["mover"] = EffectValue.Space(new HexCoordinate(1, 0)),
                ["distance"] = EffectValue.Integer(1),
                [SyntheticDisplacementCapabilities.DestinationPolicyParameterName] =
                    EffectValue.Integer((int)GuardsDigital.Core.Rules.Displacement
                        .DirectMoveDestinationPolicy.Unrestricted)
            };
            var handler = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticDisplacementCapabilities.DirectMoveV3);
            var ordinary = handler.Anticipate(Context(board, inputs));
            var declared = handler.Anticipate(Context(
                board,
                inputs,
                immunityOverrides: new[]
                {
                    new ImmunityOverrideDeclaration(
                        RuleActivity.DirectMove,
                        ImmunityOverrideReason.PrintedHeavyMinionImmunity)
                }));

            Assert.That(ordinary, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(declared, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(SyntheticDisplacementCapabilities.DirectMoveV3.Revision, Is.EqualTo(3),
                "The compatible extension keeps the registered capability and snapshot identity.");
        }

        [Test]
        public void TargetPairAndExistingSwap_ConsumeOnlyTheExactSwapHeavyOverride()
        {
            var heavy = Minion("swap-heavy", TeamSide.Blue, StandardMinionType.Heavy,
                new HexCoordinate(1, 0));
            var escort = Minion("swap-escort", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(1, -1));
            var board = BattleZoneBoard(new[] { heavy, escort });
            var pairInputs = TargetInputs(
                RosterQueryOrigin.Actor, 1, RosterUnitKind.Minion,
                SourceTeamRelation.OpposingSource);
            var declaration = new[]
            {
                new ImmunityOverrideDeclaration(
                    RuleActivity.Swap,
                    ImmunityOverrideReason.PrintedHeavyMinionImmunity)
            };
            var pair = Handler(SyntheticRosterCapabilities.ChooseTargetPairV1);

            Assert.That(pair.Execute(Context(board, pairInputs)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(pair.Execute(Context(
                    board, pairInputs, immunityOverrides: declaration)).Choice.Candidates,
                Is.EqualTo(new[]
                {
                    EffectValue.Minion(escort.MinionId), EffectValue.Minion(heavy.MinionId)
                }).Or.EqualTo(new[]
                {
                    EffectValue.Minion(heavy.MinionId), EffectValue.Minion(escort.MinionId)
                }));

            var swapInputs = new Dictionary<string, EffectValue>
            {
                ["first"] = EffectValue.Space(new HexCoordinate(1, 0)),
                ["second"] = EffectValue.Space(new HexCoordinate(1, -1)),
                [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                    EffectValue.Integer((int)SwapEligiblePieceKind.Unit)
            };
            var swap = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticExchangeCapabilities.SwapV2);
            Assert.That(swap.Anticipate(Context(board, swapInputs)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(swap.Anticipate(Context(
                    board, swapInputs, immunityOverrides: declaration)),
                Is.EqualTo(EffectExecutionResultKind.Applied));

            var explicitSwapImmunity = Modification(
                RuleObject.ForMinion(heavy.MinionId),
                RuleModification.ForImmunity(RuleActivity.Swap),
                "roster-explicit-swap-immunity");
            Assert.That(swap.Anticipate(Context(
                    board, swapInputs, explicitSwapImmunity,
                    immunityOverrides: declaration)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A printed Heavy exception never bypasses an independent Swap immunity source.");
        }

        [Test]
        public void BehindTargetLine_UsesTheCapturedTargetSpaceAndOffersOnlyHeroesBeyondIt()
        {
            var target = Minion("line-target", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(1, 0));
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(2, 0), new[] { target });
            var enclosing = new EnclosingAttackFacts(
                Actor, default, target.MinionId, AttackRangeKind.NonRanged,
                new HexCoordinate(1, 0));
            var context = Context(
                board,
                new Dictionary<string, EffectValue> { ["length"] = EffectValue.Integer(2) },
                enclosingAttack: enclosing);

            var offered = Handler(SyntheticRosterCapabilities.BehindTargetLineV1).Execute(context);

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offered.Choice.MinimumSelections, Is.Zero);
            Assert.That(offered.Choice.Candidates, Is.EqualTo(new[] { EffectValue.Hero(Other) }));
        }

        [Test]
        public void ChargeLine_AsksDestinationThenTarget_AndBindsBothWithoutMovingTheActor()
        {
            var target = Minion("charge-target", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(3, 0));
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(4, 0), new[] { target });
            var inputs = new Dictionary<string, EffectValue>
            {
                ["minimumDistance"] = EffectValue.Integer(2),
                ["maximumDistance"] = EffectValue.Integer(2),
                [SyntheticRosterCapabilities.UnitKindParameter] =
                    EffectValue.Integer((int)RosterUnitKind.Unit)
            };
            var handler = Handler(SyntheticRosterCapabilities.ChooseChargeLineV1);
            var first = handler.Execute(Context(board, inputs));
            var destination = first.Choice.Candidates.Single();
            var second = handler.Resume(Context(board, inputs), new[] { destination });
            var answered = new[] { (IReadOnlyList<EffectValue>)new[] { destination } };
            var applied = handler.Resume(
                Context(board, inputs, answersAlreadyGiven: answered),
                new[] { EffectValue.Minion(target.MinionId) });

            Assert.That(destination.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(second.Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Minion(target.MinionId) }));
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(applied.Outcomes.Single(value => value.OutputName == "destination")
                .Value.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(applied.Outcomes.Single(value => value.OutputName ==
                SyntheticRosterCapabilities.ChosenMinionOutput).Value.MinionId,
                Is.EqualTo(target.MinionId));
            Assert.That(HeroAt(board, Actor), Is.EqualTo(new HexCoordinate(0, 0)),
                "The later direct-move node owns the authoritative movement commit.");
        }

        [Test]
        public void ChargeLine_RejectsAStraightDestinationWhoseRouteIsBlocked()
        {
            var target = Minion("blocked-charge-target", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(3, 0));
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(4, 0), new[] { target },
                terrain: new[] { new HexCoordinate(1, 0) });
            var inputs = new Dictionary<string, EffectValue>
            {
                ["minimumDistance"] = EffectValue.Integer(2),
                ["maximumDistance"] = EffectValue.Integer(2),
                [SyntheticRosterCapabilities.UnitKindParameter] =
                    EffectValue.Integer((int)RosterUnitKind.Unit)
            };

            var result = Handler(SyntheticRosterCapabilities.ChooseChargeLineV1)
                .Execute(Context(board, inputs));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(HeroAt(board, Actor), Is.EqualTo(new HexCoordinate(0, 0)));
        }

        [Test]
        public void ThroughTargetLine_RevalidatesItsEndpointAndLeavesTheActorStillOnAStaleAnswer()
        {
            var target = Minion("through-target", TeamSide.Blue, StandardMinionType.Melee,
                new HexCoordinate(1, 0));
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(4, 0), new[] { target });
            var inputs = new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.UnitKindParameter] =
                    EffectValue.Integer((int)RosterUnitKind.Unit)
            };
            var handler = Handler(SyntheticRosterCapabilities.ChooseThroughTargetLineV1);
            var context = Context(board, inputs);
            var offered = handler.Execute(context);
            Assert.That(offered.Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Minion(target.MinionId) }));

            PlaceToken(board, new HexCoordinate(2, 0));
            var stale = handler.Resume(context, offered.Choice.Candidates);

            Assert.That(stale.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(HeroAt(board, Actor), Is.EqualTo(new HexCoordinate(0, 0)));
        }

        [Test]
        public void ThroughTargetLine_BindsTheCrossedTargetAndEndpointWithoutMovingTheActor()
        {
            var target = Minion("through-positive-target", TeamSide.Blue,
                StandardMinionType.Melee, new HexCoordinate(1, 0));
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(4, 0), new[] { target });
            var inputs = new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.UnitKindParameter] =
                    EffectValue.Integer((int)RosterUnitKind.Unit)
            };
            var handler = Handler(SyntheticRosterCapabilities.ChooseThroughTargetLineV1);
            var applied = handler.Resume(
                Context(board, inputs),
                new[] { EffectValue.Minion(target.MinionId) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(applied.Outcomes.Single(value => value.OutputName == "destination")
                .Value.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            Assert.That(applied.Outcomes.Single(value => value.OutputName ==
                SyntheticRosterCapabilities.ChosenMinionOutput).Value.MinionId,
                Is.EqualTo(target.MinionId));
            Assert.That(HeroAt(board, Actor), Is.EqualTo(new HexCoordinate(0, 0)),
                "The later direct-move node owns the authoritative movement commit.");
        }

        [Test]
        public void FormerSpaceAndPreConsequenceAdjacency_ReadTheCapturedMoment_NotCurrentOccupancy()
        {
            var board = Board(new HexCoordinate(0, 0), new HexCoordinate(4, 0));
            var captured = new HexCoordinate(1, 0);
            var enclosing = new EnclosingAttackFacts(
                Actor, Other, default, AttackRangeKind.NonRanged, captured);
            var context = Context(board, new Dictionary<string, EffectValue>(),
                enclosingAttack: enclosing);

            var former = Handler(SyntheticRosterCapabilities.FormerTargetSpaceV1).Execute(context);
            var adjacent = Handler(SyntheticRosterCapabilities.PreConsequenceAdjacencyV1)
                .Execute(context);

            Assert.That(former.Outcomes.Single().Value.Coordinate, Is.EqualTo(captured));
            Assert.That(adjacent.Outcomes.Single().Value.BooleanValue, Is.True,
                "The target no longer stands at the captured coordinate, but the pre-consequence " +
                "adjacency fact remains answerable.");
        }

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            Catalog.Load(reference);

        private static Dictionary<string, EffectValue> TargetInputs(
            RosterQueryOrigin origin,
            int distance,
            RosterUnitKind unitKind,
            SourceTeamRelation relation) =>
            new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.OriginParameter] = EffectValue.Integer((int)origin),
                [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(distance),
                [SyntheticRosterCapabilities.UnitKindParameter] = EffectValue.Integer((int)unitKind),
                [SyntheticRosterCapabilities.RelationParameter] = EffectValue.Integer((int)relation)
            };

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            IReadOnlyDictionary<string, EffectValue> inputs,
            RuleModificationSet modifications = null,
            IReadOnlyList<IReadOnlyList<EffectValue>> answersAlreadyGiven = null,
            EnclosingAttackFacts enclosingAttack = default,
            IReadOnlyList<ImmunityOverrideDeclaration> immunityOverrides = null) =>
            new EffectCapabilityContext(
                new EffectNodeId("roster-target-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                new CardInstanceId("roster-target-card"),
                new CardDefinitionReference(new CardDefinitionId("roster-target-definition"), "rev-1"),
                new ActionDefinitionId("roster-target-action"),
                inputs,
                3,
                2,
                StagedBoardParticipants.Create(),
                null,
                modifications ?? RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board),
                answersAlreadyGiven: answersAlreadyGiven,
                enclosingAttack: enclosingAttack,
                immunityOverrides: immunityOverrides);

        private static AdjacentMoveAuthority Board(
            HexCoordinate actorAt,
            HexCoordinate otherAt,
            IEnumerable<QuickGameMinionSetupState> minions = null,
            IEnumerable<HexCoordinate> terrain = null)
        {
            var terrainSet = new HashSet<HexCoordinate>(terrain ?? Array.Empty<HexCoordinate>());
            var playable = Radius(4).Where(value => !terrainSet.Contains(value)).ToArray();
            var map = BoardMapDefinition.CreateSynthetic(
                Map, 1, playable, terrainSet);
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Actor, actorAt), new HeroOccupancy(Other, otherAt) },
                Array.Empty<ObstacleOccupancy>(),
                minions ?? Array.Empty<QuickGameMinionSetupState>());
        }

        private static AdjacentMoveAuthority BattleZoneBoard(
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            var battle = Radius(4).ToArray();
            var map = BoardMapDefinition.CreateSyntheticWithStartingBattleZone(
                Map, 2, battle, new[] { new HexCoordinate(10, 0) },
                Array.Empty<HexCoordinate>());
            return new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Other, new HexCoordinate(3, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                minions);
        }

        private static QuickGameMinionSetupState Minion(
            string id,
            TeamSide side,
            StandardMinionType type,
            HexCoordinate at) =>
            new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId(side == TeamSide.Red
                    ? StagedBoardParticipants.RedTeamIdValue
                    : StagedBoardParticipants.BlueTeamIdValue),
                side,
                type,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId($"spawn-{id}")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        private static RuleModificationSet Modification(
            RuleObject affected,
            RuleModification modification,
            string id) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        modification,
                        affected,
                        new ActiveEffectId(id),
                        Actor,
                        TeamSide.Red,
                        1)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        private static IEnumerable<HexCoordinate> Radius(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                var minimumR = Math.Max(-radius, -q - radius);
                var maximumR = Math.Min(radius, -q + radius);
                for (var r = minimumR; r <= maximumR; r++)
                    yield return new HexCoordinate(q, r);
            }
        }

        private static HexCoordinate HeroAt(AdjacentMoveAuthority board, HeroId id) =>
            board.Occupancies.Single(value => value.HeroId.Equals(id)).Coordinate;

        private static void PlaceToken(AdjacentMoveAuthority board, HexCoordinate at) =>
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("roster-through-stale-token"),
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(
                    SyntheticComponentDefinitions.HazardV4),
                Actor,
                Actor,
                new CardInstanceId("roster-through-token-source"),
                at));
    }
}
