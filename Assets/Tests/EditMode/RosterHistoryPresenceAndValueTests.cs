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
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19b's history/presence readers and typed Attack, Range and Defense declarations.
    /// Two different synthetic hero owners consume the same value-query seam.
    /// </summary>
    public sealed class RosterHistoryPresenceAndValueTests
    {
        private static readonly HeroId Red =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("roster-history-presence");
        private static readonly EffectCapabilityCatalog RosterCatalog =
            new EffectCapabilityCatalog(SyntheticRosterCapabilities.Handlers());

        [Test]
        public void PlayedAttackHistoryConsumer_UsesTheApprovedRosterMatrixIdentity()
        {
            Assert.That(
                SyntheticRosterCapabilities.PlayedAttackThisTurnV1.ToString(),
                Is.EqualTo("target-played-attack-this-turn@1"));
        }

        [Test]
        public void HistoryCapabilities_ReadTheExactTurnAndTheRoundOwnedDefeatRecord()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var redAttack = configuration.CardInstances.First(card =>
                card.OwnerId.Equals(Red) && PrimaryKind(configuration.CardDefinitions, card) == ActionKind.Attack);
            var blueOther = configuration.CardInstances.First(card =>
                card.OwnerId.Equals(Blue) && PrimaryKind(configuration.CardDefinitions, card) != ActionKind.Attack);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(redAttack), Is.True);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(blueOther), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(redAttack), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(blueOther), Is.True);
            var slot = new RoundTurnSlot(
                4,
                2,
                new[]
                {
                    new TurnCardAssignment(Red, redAttack.Id, false),
                    new TurnCardAssignment(Blue, blueOther.Id, false)
                },
                configuration.CardInstances,
                configuration.Participants);
            var history = new RoundHeroDefeatHistory(configuration.Participants);
            var ledger = new HeroCoinLedger(configuration.Participants);
            var roster = EffectRosterState.Of(new[] { slot }, history, ledger);
            var cards = EffectCardState.Of(
                configuration.CardDefinitions, configuration.CardInstances);

            var played = Handler(SyntheticRosterCapabilities.PlayedAttackThisTurnV1).Execute(
                Context(
                    null,
                    new Dictionary<string, EffectValue> { ["hero"] = EffectValue.Hero(Red) },
                    cards,
                    roster,
                    roundIndex: 4,
                    turnIndex: 2));
            var wrongTurn = Handler(SyntheticRosterCapabilities.PlayedAttackThisTurnV1).Execute(
                Context(
                    null,
                    new Dictionary<string, EffectValue> { ["hero"] = EffectValue.Hero(Red) },
                    cards,
                    roster,
                    roundIndex: 4,
                    turnIndex: 1));

            Assert.That(played.Outcomes.Single().Value.BooleanValue, Is.True);
            Assert.That(wrongTurn.Outcomes.Single().Value.BooleanValue, Is.False);
            Assert.That(Handler(SyntheticRosterCapabilities.PlayedAttackThisTurnV1).Anticipate(
                    Context(
                        null,
                        new Dictionary<string, EffectValue> { ["hero"] = EffectValue.Hero(Red) },
                        EffectCardState.Absent,
                        EffectRosterState.Absent)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Pure feasibility names unavailable authority instead of treating it as empty history.");

            history.Record(new DefeatOutcome(
                new DefeatOutcomeId("roster-history-defeat"),
                new DefeatTargetId(Blue.Value),
                configuration.Participants.GetTeamForHero(Blue).Id,
                Red,
                1,
                0));
            var defeated = Handler(SyntheticRosterCapabilities.HeroDefeatedThisRoundV1).Execute(
                Context(null, new Dictionary<string, EffectValue>(), cards, roster));
            Assert.That(defeated.Outcomes.Single().Value.BooleanValue, Is.True);
            history.BeginRound();
            Assert.That(Handler(SyntheticRosterCapabilities.HeroDefeatedThisRoundV1)
                .Execute(Context(null, new Dictionary<string, EffectValue>(), cards, roster))
                .Outcomes.Single().Value.BooleanValue, Is.False);
        }

        [Test]
        public void PresenceCount_UsesEffectiveKindsButDoesNotConfuseImmunityWithAbsence()
        {
            var board = Board(
                new HexCoordinate(0, 0),
                new HexCoordinate(1, 0),
                Array.Empty<QuickGameMinionSetupState>());
            var immune = Modification(
                RuleObject.ForHero(Blue),
                RuleModification.ForImmunity(RuleActivity.Skill),
                "roster-presence-immunity");
            var heroInputs = PresenceInputs(RosterUnitKind.Hero);
            var immunePresence = Handler(SyntheticRosterCapabilities.PresenceCountV1)
                .Execute(Context(board, heroInputs, modifications: immune));

            Assert.That(immunePresence.Outcomes.Single(value => value.OutputName == "count")
                .Value.IntegerValue, Is.EqualTo(1));
            Assert.That(immunePresence.Outcomes.Single(value => value.OutputName == "present")
                .Value.BooleanValue, Is.True,
                "p.18 immunity controls targeting, not whether a unit is present.");

            var countsAs = Modification(
                RuleObject.ForHero(Blue),
                RuleModification.ForCountsAs(EffectiveUnitKind.Minion),
                "roster-presence-counts-as");
            var minionPresence = Handler(SyntheticRosterCapabilities.PresenceCountV1)
                .Execute(Context(board, PresenceInputs(RosterUnitKind.Minion), modifications: countsAs));
            Assert.That(minionPresence.Outcomes.Single(value => value.OutputName == "count")
                .Value.IntegerValue, Is.EqualTo(1));

            var malformed = PresenceInputs(RosterUnitKind.Unit);
            malformed[SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(-1);
            Assert.That(Handler(SyntheticRosterCapabilities.PresenceCountV1)
                .Anticipate(Context(board, malformed)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [Test]
        public void TerrainPresence_ReadsDeclaredTerrainRatherThanOffMapCoordinates()
        {
            var playable = Radius(2).Where(value => !value.Equals(new HexCoordinate(1, 0)));
            var map = BoardMapDefinition.CreateSynthetic(
                Map,
                2,
                playable,
                new[] { new HexCoordinate(1, 0) });
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Blue, new HexCoordinate(2, 0))
                },
                Array.Empty<ObstacleOccupancy>());

            var result = Handler(SyntheticRosterCapabilities.AdjacentTerrainPresenceV1)
                .Execute(Context(board, new Dictionary<string, EffectValue>()));

            Assert.That(result.Outcomes.Single().Value.BooleanValue, Is.True);
        }

        [Test]
        public void TwoSyntheticHeroes_UseTheSameTypedDynamicValueQueryForAttackAndRange()
        {
            var participants = StagedBoardParticipants.Create();
            var blueMinion = Minion("dynamic-blue", TeamSide.Blue, new HexCoordinate(0, 1));
            var redMinion = Minion("dynamic-red", TeamSide.Red, new HexCoordinate(1, -1));
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(1, 0),
                new[] { blueMinion, redMinion });
            var redAction = AttackAction(
                "red-dynamic-attack",
                AttackRangeKind.NonRanged,
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack,
                    RosterValueQueryKind.EnemyUnitsAdjacentToActorExcludingTarget,
                    2));
            var blueAction = AttackAction(
                "blue-dynamic-range",
                AttackRangeKind.Ranged,
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Range,
                    RosterValueQueryKind.EnemyUnitsAdjacentToActor,
                    1));
            var redDefinition = Definition("red-dynamic-card", redAction);
            var blueDefinition = Definition("blue-dynamic-card", blueAction);
            var definitions = new[] { redDefinition, blueDefinition };
            var redCard = CardInstance.CreateInHand(
                new CardInstanceId("red-dynamic-instance"), redDefinition.Reference, Red);
            var blueCard = CardInstance.CreateInHand(
                new CardInstanceId("blue-dynamic-instance"), blueDefinition.Reference, Blue);
            var instances = new[] { redCard, blueCard };
            var roster = EffectRosterState.Of(
                Array.Empty<RoundTurnSlot>(),
                new RoundHeroDefeatHistory(participants),
                new HeroCoinLedger(participants));
            var redOffer = Offer(redCard, redAction, Red, AttackRangeKind.NonRanged);
            var blueOffer = Offer(blueCard, blueAction, Blue, AttackRangeKind.Ranged);

            var attack = RosterDynamicValueQuery.Contributions(
                redAction,
                redOffer,
                AttackTarget.Hero(Blue),
                PrintedValueChannel.Attack,
                definitions,
                instances,
                participants,
                board,
                RuleModificationSet.Empty,
                roster,
                1,
                1);
            var range = RosterDynamicValueQuery.Contributions(
                blueAction,
                blueOffer,
                default,
                PrintedValueChannel.Range,
                definitions,
                instances,
                participants,
                board,
                RuleModificationSet.Empty,
                roster,
                1,
                1);

            Assert.That(attack.Single().Amount, Is.EqualTo(2),
                "The target is excluded, leaving the adjacent enemy minion as one match at +2.");
            Assert.That(range.Single().Amount, Is.EqualTo(2),
                "The other synthetic hero sees the adjacent enemy hero and minion through the same query.");
            Assert.That(attack.Single().Kind, Is.EqualTo(ValueContributionKind.CardTextModifier));
            Assert.That(range.Single().Channel, Is.EqualTo(PrintedValueChannel.Range));

            var redAuthority = new CardActionResolutionAuthority(
                MatchContentCatalogs.ForThisBuild(), board);
            var redStart = redAuthority.StartAttackResolution(
                new ActionResolutionId("roster-dynamic-attack-resolution"),
                redOffer,
                definitions,
                instances,
                participants,
                RuleModificationSet.Empty);
            var redAnswer = redAuthority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    redStart.PendingTargetChoice.ResolutionId,
                    redStart.PendingTargetChoice.ChoiceId,
                    Blue),
                definitions,
                instances,
                participants,
                RuleModificationSet.Empty);
            Assert.That(redAnswer.PendingResolution.AttackTotal, Is.EqualTo(4),
                "The live Attack total composes the typed +2 card-text contribution.");

            var rangeBoard = Board(
                new HexCoordinate(3, 0),
                new HexCoordinate(0, 0),
                new[] { Minion("dynamic-range-red", TeamSide.Red, new HexCoordinate(1, 0)) });
            var blueAuthority = new CardActionResolutionAuthority(
                MatchContentCatalogs.ForThisBuild(), rangeBoard);
            var blueStart = blueAuthority.StartAttackResolution(
                new ActionResolutionId("roster-dynamic-range-resolution"),
                blueOffer,
                definitions,
                instances,
                participants,
                RuleModificationSet.Empty);
            Assert.That(blueStart.PendingTargetChoice.LegalTargetHeroIds,
                Does.Contain(Red),
                "The live Range total reaches distance 3 after one adjacent enemy contributes +1.");
        }

        [Test]
        public void DynamicDeclarations_RejectWrongActionAndRangeShapes()
        {
            var attackContribution = new RosterValueContributionDefinition(
                PrintedValueChannel.Attack,
                RosterValueQueryKind.EnemyUnitsAdjacentToActor,
                1);
            var rangeContribution = new RosterValueContributionDefinition(
                PrintedValueChannel.Range,
                RosterValueQueryKind.EnemyUnitsAdjacentToActor,
                1);

            Assert.That(() => new ActionDefinition(
                    new ActionDefinitionId("skill-with-attack-query"),
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    Array.Empty<PrintedValueDefinition>(),
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    rosterValueContributions: new[] { attackContribution }),
                Throws.ArgumentException);
            Assert.That(() => AttackAction(
                    "non-ranged-with-range-query", AttackRangeKind.NonRanged, rangeContribution),
                Throws.ArgumentException);
            Assert.That(() => new ActionDefinition(
                    new ActionDefinitionId("default-query"),
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default,
                    rosterValueContributions: new[] { default(RosterValueContributionDefinition) }),
                Throws.ArgumentException);
        }

        [Test]
        public void DefenseDeclarations_SuppressMinionContributionsAndForbidOnlyPrintedPrimaryBlocks()
        {
            var participants = StagedBoardParticipants.Create();
            var defense = DefenseAction(
                "suppressed-defense", MinionDefenseContributionPolicy.SuppressAll);
            var defenseDefinition = Definition("suppressed-defense-card", defense);
            var defenseCard = CardInstance.CreateInHand(
                new CardInstanceId("suppressed-defense-instance"), defenseDefinition.Reference, Blue);
            var defenseOffer = ActionOfferQuery.GetAvailableOffers(
                new[] { defenseDefinition }, new[] { defenseCard }, Blue,
                ActionOfferWindow.Reaction).Single();
            var minions = new[]
            {
                Minion("friendly-defense-minion", TeamSide.Blue, new HexCoordinate(1, 1)),
                Minion("enemy-defense-minion", TeamSide.Red, new HexCoordinate(0, 1))
            };
            var board = Board(
                new HexCoordinate(0, 0), new HexCoordinate(1, 0), minions);
            var total = DefenseValueQuery.Calculate(
                defenseOffer,
                new[] { defenseDefinition },
                Blue,
                participants,
                board,
                RuleModificationSet.Empty,
                null);
            Assert.That(total.Total, Is.EqualTo(3));
            Assert.That(total.Contributions.Any(value =>
                value.Kind == ValueContributionKind.MinionDefenseModifier), Is.False);

            var block = BlockAction();
            var blockDefinition = Definition("printed-block-card", block);
            var ordinary = DefenseAction(
                "ordinary-defense", MinionDefenseContributionPolicy.IncludeAll);
            var ordinaryDefinition = Definition("ordinary-defense-card", ordinary);
            var blockCard = CardInstance.CreateInHand(
                new CardInstanceId("printed-block-instance"), blockDefinition.Reference, Blue);
            var ordinaryCard = CardInstance.CreateInHand(
                new CardInstanceId("ordinary-defense-instance"), ordinaryDefinition.Reference, Blue);
            var incoming = AttackAction(
                "block-restricting-attack",
                AttackRangeKind.NonRanged,
                defenseRestriction: DefenseOfferOriginRestriction.PrintedPrimaryOnly);
            var definitions = new[] { blockDefinition, ordinaryDefinition };
            var instances = new[] { blockCard, ordinaryCard };

            var unrestricted = DefenseOfferQuery.Offers(
                definitions, instances, Blue, RuleModificationSet.Empty);
            var restricted = DefenseOfferQuery.Offers(
                definitions, instances, Blue, RuleModificationSet.Empty, incoming);

            Assert.That(unrestricted.Select(value => value.SourceCardInstanceId),
                Is.EquivalentTo(new[] { blockCard.Id, ordinaryCard.Id }));
            Assert.That(restricted.Select(value => value.SourceCardInstanceId),
                Is.EqualTo(new[] { ordinaryCard.Id }),
                "PrintedPrimary-only is an origin restriction, not a blanket Defense restriction.");
        }

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            RosterCatalog.Load(reference);

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            IReadOnlyDictionary<string, EffectValue> inputs,
            EffectCardState cards = null,
            EffectRosterState roster = null,
            RuleModificationSet modifications = null,
            int roundIndex = 1,
            int turnIndex = 1) =>
            new EffectCapabilityContext(
                new EffectNodeId("roster-history-node"),
                EffectFailurePolicy.Mandatory,
                Red,
                Red,
                new CardInstanceId("roster-history-card"),
                new CardDefinitionReference(new CardDefinitionId("roster-history-definition"), "rev-1"),
                new ActionDefinitionId("roster-history-action"),
                inputs,
                roundIndex,
                turnIndex,
                StagedBoardParticipants.Create(),
                null,
                modifications ?? RuleModificationSet.Empty,
                cards ?? EffectCardState.Absent,
                ActionKind.Skill,
                board == null
                    ? null
                    : new MatchComponents(
                        SyntheticComponentDefinitions.CreateCatalog(), board,
                        new MarkerAttachmentRegister(), board, board, board),
                rosterState: roster);

        private static Dictionary<string, EffectValue> PresenceInputs(RosterUnitKind kind) =>
            new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.OriginParameter] =
                    EffectValue.Integer((int)RosterQueryOrigin.Actor),
                [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(1),
                [SyntheticRosterCapabilities.UnitKindParameter] = EffectValue.Integer((int)kind),
                [SyntheticRosterCapabilities.RelationParameter] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                ["excludeActor"] = EffectValue.Boolean(true),
                ["excludeTarget"] = EffectValue.Boolean(false)
            };

        private static ActionKind PrimaryKind(
            IReadOnlyList<CardDefinition> definitions,
            CardInstance card) =>
            definitions.Single(value => value.Reference.Equals(card.DefinitionReference))
                .ActionDefinitions.Single(value =>
                    value.Origin == ActionDefinitionOrigin.PrintedPrimary).Kind;

        private static ActionDefinition AttackAction(
            string id,
            AttackRangeKind range,
            RosterValueContributionDefinition? contribution = null,
            DefenseOfferOriginRestriction defenseRestriction = default)
        {
            var values = range == AttackRangeKind.Ranged
                ? new[]
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                    new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                }
                : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 2) };
            return new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                values,
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                range,
                default,
                rosterValueContributions: contribution.HasValue
                    ? new[] { contribution.Value }
                    : Array.Empty<RosterValueContributionDefinition>(),
                defenseOfferRestriction: defenseRestriction);
        }

        private static ActionDefinition DefenseAction(
            string id,
            MinionDefenseContributionPolicy policy) =>
            new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                minionDefenseContributions: policy);

        private static ActionDefinition BlockAction()
        {
            var node = new EffectNodeId("roster-block-node");
            var effect = new EffectDefinition(
                new EffectDefinitionId("roster-block-effect"),
                "rev-1",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node,
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.NoteActorV1)
                });
            return new ActionDefinition(
                new ActionDefinitionId("printed-block"),
                ActionKind.Defense,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                effect,
                defenseResolution: PrintedDefenseResolution.Block,
                cardTextTiming: CardTextTiming.DefenseAction,
                blockCondition: PrintedBlockCondition.AttackIsRanged);
        }

        private static CardDefinition Definition(string id, params ActionDefinition[] actions) =>
            new CardDefinition(
                new CardDefinitionId(id),
                "rev-1",
                $"TEST - {id}",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                actions);

        private static ActionOffer Offer(
            CardInstance card,
            ActionDefinition action,
            HeroId actor,
            AttackRangeKind range) =>
            new ActionOffer(
                card.Id,
                card.DefinitionReference,
                action.Id,
                actor,
                ActionInvocationContext.PrintedPrimary,
                true,
                range,
                default);

        private static AdjacentMoveAuthority Board(
            HexCoordinate redAt,
            HexCoordinate blueAt,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            var map = BoardMapDefinition.CreateSynthetic(Map, 1, Radius(3));
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Red, redAt), new HeroOccupancy(Blue, blueAt) },
                Array.Empty<ObstacleOccupancy>(),
                minions);
        }

        private static QuickGameMinionSetupState Minion(
            string id,
            TeamSide side,
            HexCoordinate at) =>
            new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId(side == TeamSide.Red
                    ? StagedBoardParticipants.RedTeamIdValue
                    : StagedBoardParticipants.BlueTeamIdValue),
                side,
                StandardMinionType.Melee,
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
                        Red,
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
    }
}
