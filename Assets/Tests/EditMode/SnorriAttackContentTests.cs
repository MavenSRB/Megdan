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
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriAttackContentTests
    {
        private static readonly HeroId Actor =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MinionId EnemyMinion = new MinionId("snorri-enemy-minion");
        private static readonly MapId Map = new MapId("snorri-attacks");

        [Test]
        public void GoldAndFiveRedFacesHavePrintedValuesItemsAndTimedText()
        {
            var definitions = SnorriAttackFaces.Definitions;
            Assert.That(definitions.Count, Is.EqualTo(6));
            Assert.That(definitions.Select(card => card.Id).Distinct().Count(), Is.EqualTo(6));
            Assert.That(definitions.Select(card => card.Initiative.Value),
                Is.EqualTo(new[] { 11, 7, 7, 7, 8, 8 }));
            Assert.That(definitions.Select(card => card.Progression.ItemIcon),
                Is.EqualTo(new[]
                {
                    PrintedValueChannel.None, PrintedValueChannel.None,
                    PrintedValueChannel.Initiative, PrintedValueChannel.Defense,
                    PrintedValueChannel.Radius, PrintedValueChannel.Movement
                }));

            foreach (var card in definitions)
            {
                var attack = card.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Attack);
                Assert.That(attack.EffectRoot, Is.Not.Null);
                Assert.That(card.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Defense).BaseValues.Single().Amount,
                    Is.GreaterThan(0));
                Assert.That(card.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Movement).BaseValues.Single().Amount,
                    Is.EqualTo(card.Id.Equals(SnorriAttackFaces.RuneSigils) ? 1 : 3));
            }

            Assert.That(definitions[0].ActionDefinitions.Single(
                action => action.Kind == ActionKind.Attack).RosterValueContributions.Single()
                .MarkerDefinition,
                Is.EqualTo(SnorriComponentContent.Runes[1].Reference));
            Assert.That(definitions[3].ActionDefinitions.Single(
                action => action.Kind == ActionKind.Attack).AttackTargetPolicy.Value.Reach,
                Is.EqualTo(AttackTargetReach.ExactlyPrintedRange));
            Assert.That(definitions[5].ActionDefinitions.Single(
                action => action.Kind == ActionKind.Attack).AttackTargetPolicy.Value
                .TargetIsBoundByCardText, Is.True);

            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                SnorriComponentContent.Handlers().Concat(
                    SnorriAttackTargetContent.Handlers()));
            foreach (var card in definitions)
            {
                var attack = card.ActionDefinitions.Single(
                    action => action.Kind == ActionKind.Attack);
                var errors = EffectDefinitionValidator.ValidateCardText(
                    attack.EffectRoot, catalog, attack.CardTextTiming);
                Assert.That(errors, Is.Empty,
                    card.DisplayName + ": " + string.Join("; ", errors));
                if (attack.SecondCardTextRoot != null)
                {
                    var afterErrors = EffectDefinitionValidator.ValidateCardText(
                        attack.SecondCardTextRoot, catalog, attack.SecondCardTextTiming);
                    Assert.That(afterErrors, Is.Empty,
                        card.DisplayName + " after attack: " + string.Join("; ", afterErrors));
                }
            }
        }

        [Test]
        public void SigilsFirstRuneWidensOnlyToMinions_ButFourthRuneRepeatNamesHeroesInRange()
        {
            var card = SnorriAttackFaces.Definitions[0];
            var board = Board(new HexCoordinate(1, 0), new HexCoordinate(2, 0),
                new HexCoordinate(0, 2));
            var markers = new MarkerAttachmentRegister();
            var handler = SnorriAttackTargetContent.Handlers().Single(value =>
                value.Reference.Equals(SnorriAttackTargetContent.SigilsTargetV1));

            var ordinary = TargetContext(card, board, markers);
            Assert.That(handler.Execute(ordinary).Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Hero(Enemy) }));

            GiveRune(markers, 1);
            var first = handler.Execute(TargetContext(card, board, markers));
            Assert.That(first.Choice.Candidates,
                Is.EquivalentTo(new[]
                {
                    EffectValue.Hero(Enemy), EffectValue.Minion(EnemyMinion)
                }));
            Assert.That(handler.Resume(TargetContext(card, board, markers),
                    new[] { EffectValue.Hero(SecondEnemy) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Rune 1 grants distant MINION reach, not distant-hero reach.");

            var repeated = handler.Execute(TargetContext(
                card, board, markers, ActionInvocationContext.Repeated));
            Assert.That(repeated.Choice.Candidates,
                Is.EquivalentTo(new[]
                {
                    EffectValue.Hero(Enemy), EffectValue.Hero(SecondEnemy)
                }),
                "Rune 4's repeat states its own different-hero-in-range target set.");
        }

        [Test]
        public void RuneblasterRequiresMaximumRangeUntilFirstRuneIsActive()
        {
            var card = SnorriAttackFaces.Definitions[5];
            var board = Board(new HexCoordinate(1, 0), new HexCoordinate(3, 0),
                new HexCoordinate(0, 3));
            var markers = new MarkerAttachmentRegister();
            var handler = SnorriAttackTargetContent.Handlers().Single(value =>
                value.Reference.Equals(SnorriAttackTargetContent.RuneblasterTargetV1));

            var ordinary = handler.Execute(TargetContext(card, board, markers));
            Assert.That(ordinary.Choice.Candidates,
                Is.EquivalentTo(new[]
                {
                    EffectValue.Hero(SecondEnemy), EffectValue.Minion(EnemyMinion)
                }));
            Assert.That(handler.Resume(TargetContext(card, board, markers),
                    new[] { EffectValue.Hero(Enemy) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));

            GiveRune(markers, 1);
            Assert.That(handler.Execute(TargetContext(card, board, markers)).Choice.Candidates,
                Is.EquivalentTo(new[]
                {
                    EffectValue.Hero(Enemy), EffectValue.Hero(SecondEnemy),
                    EffectValue.Minion(EnemyMinion)
                }));
        }

        [Test]
        public void TargetAnswersAreRecheckedWhenTheActiveRuneChangesDuringAPause()
        {
            var markers = new MarkerAttachmentRegister();
            GiveRune(markers, 1);
            var sigils = SnorriAttackFaces.Definitions[0];
            var sigilsBoard = Board(new HexCoordinate(1, 0),
                new HexCoordinate(2, 0), new HexCoordinate(0, 2));
            var sigilsHandler = SnorriAttackTargetContent.Handlers().Single(value =>
                value.Reference.Equals(SnorriAttackTargetContent.SigilsTargetV1));
            Assert.That(sigilsHandler.Execute(
                TargetContext(sigils, sigilsBoard, markers)).Choice.Candidates,
                Does.Contain(EffectValue.Minion(EnemyMinion)));

            markers.Return(new MarkerInstanceId("snorri-attack-rune-1"));
            Assert.That(sigilsHandler.Resume(
                    TargetContext(sigils, sigilsBoard, markers),
                    new[] { EffectValue.Minion(EnemyMinion) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));

            GiveRune(markers, 1);
            var blaster = SnorriAttackFaces.Definitions[5];
            var blasterBoard = Board(new HexCoordinate(1, 0),
                new HexCoordinate(3, 0), new HexCoordinate(0, 3));
            var blasterHandler = SnorriAttackTargetContent.Handlers().Single(value =>
                value.Reference.Equals(SnorriAttackTargetContent.RuneblasterTargetV1));
            Assert.That(blasterHandler.Execute(
                TargetContext(blaster, blasterBoard, markers)).Choice.Candidates,
                Does.Contain(EffectValue.Hero(Enemy)));

            markers.Return(new MarkerInstanceId("snorri-attack-rune-1"));
            Assert.That(blasterHandler.Resume(
                    TargetContext(blaster, blasterBoard, markers),
                    new[] { EffectValue.Hero(Enemy) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [Test]
        public void RuneTargetSelectionDoesNotOfferAnAttackImmuneHero()
        {
            var immunity = new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        RuleModification.ForImmunity(RuleActivity.Attack),
                        RuleObject.ForHero(Enemy),
                        new ActiveEffectId("snorri-test-attack-immunity"),
                        Enemy, TeamSide.Blue, 1)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                new TieBreakerState(TeamSide.Red));
            var card = SnorriAttackFaces.Definitions[0];
            var board = Board(new HexCoordinate(1, 0), new HexCoordinate(2, 0),
                new HexCoordinate(0, 2));
            var markers = new MarkerAttachmentRegister();
            GiveRune(markers, 1);
            var handler = SnorriAttackTargetContent.Handlers().Single(value =>
                value.Reference.Equals(SnorriAttackTargetContent.SigilsTargetV1));

            Assert.That(handler.Execute(TargetContext(
                    card, board, markers, modifications: immunity)).Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Minion(EnemyMinion) }));
            Assert.That(handler.Resume(TargetContext(
                    card, board, markers, modifications: immunity),
                    new[] { EffectValue.Hero(Enemy) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [Test]
        public void SigilsSecondRuneAddsExactlyThreeAttackInItsCurrentSlotOnly()
        {
            var card = SnorriAttackFaces.Definitions[0];
            var action = card.ActionDefinitions.Single(
                value => value.Kind == ActionKind.Attack);
            var board = Board(new HexCoordinate(1, 0), new HexCoordinate(2, 0),
                new HexCoordinate(0, 2));
            var markers = new MarkerAttachmentRegister();
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("snorri-sigils-bonus"), card.Reference, Actor);
            var offer = new ActionOffer(
                instance.Id, card.Reference, action.Id, Actor,
                ActionInvocationContext.PrintedPrimary, true,
                AttackRangeKind.Ranged, default);
            int Bonus(int turn) => RosterDynamicValueQuery.Contributions(
                    action, offer, AttackTarget.Hero(Enemy),
                    PrintedValueChannel.Attack, new[] { card }, new[] { instance },
                    StagedBoardParticipants.CreateWithBlueAlly(), board,
                    RuleModificationSet.Empty, EffectRosterState.Absent, 1, turn,
                    markers: markers)
                .Sum(value => value.Amount);

            Assert.That(Bonus(1), Is.Zero);
            GiveRune(markers, 2);
            Assert.That(Bonus(1), Is.EqualTo(3));
            Assert.That(Bonus(2), Is.Zero);
            Assert.That(Bonus(4), Is.Zero);
        }

        private static void GiveRune(MarkerAttachmentRegister markers, int ordinal) =>
            markers.Give(new MarkerAttachment(
                new MarkerInstanceId("snorri-attack-rune-" + ordinal),
                SnorriComponentContent.Runes[ordinal - 1],
                Actor, Actor, new CardInstanceId("snorri-inscribe"),
                SnorriSilverFace.PrimarySkill, 1));

        private static EffectCapabilityContext TargetContext(
            CardDefinition card, AdjacentMoveAuthority board,
            MarkerAttachmentRegister markers,
            ActionInvocationContext invocation = ActionInvocationContext.PrintedPrimary,
            RuleModificationSet modifications = null)
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var attack = card.ActionDefinitions.Single(value => value.Kind == ActionKind.Attack);
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("snorri-attack-card"), card.Reference, Actor);
            return new EffectCapabilityContext(
                new EffectNodeId("target"), EffectFailurePolicy.Mandatory,
                Actor, Actor, instance.Id, card.Reference, attack.Id,
                new Dictionary<string, EffectValue>
                {
                    ["activity"] = EffectValue.Integer((int)RuleActivity.Attack),
                    [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                        EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
                },
                1, 1, participants, null, modifications ?? RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, new[] { instance }),
                ActionKind.Attack,
                new MatchComponents(
                    new ComponentDefinitionCatalog(
                        Array.Empty<TokenDefinition>(), SnorriComponentContent.Markers()),
                    board, markers, board, board, board, board),
                new HeroItemState(participants),
                enclosingAttack: invocation == ActionInvocationContext.Repeated
                    ? new EnclosingAttackFacts(
                        Actor, default, default, AttackRangeKind.Ranged, null,
                        repeatedSubject: new RepeatedAttackSubjectNarrowing(
                            BoardObjectKind.Hero, Array.Empty<BoardObjectReference>()))
                    : default,
                declaredOutputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero),
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenMinionOutputName,
                        EffectValueKind.Minion),
                    new EffectOutputBinding(
                        SnorriAttackTargetContent.TargetHeroOutput,
                        EffectValueKind.Boolean)
                },
                invocationContext: invocation);
        }

        private static AdjacentMoveAuthority Board(
            HexCoordinate enemyAt, HexCoordinate secondEnemyAt, HexCoordinate minionAt)
        {
            var spaces = from q in Enumerable.Range(-4, 9)
                         from r in Enumerable.Range(-4, 9)
                         where Math.Abs(q + r) <= 4
                         select new HexCoordinate(q, r);
            var minion = new QuickGameMinionSetupState(
                EnemyMinion, new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                TeamSide.Blue, StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("snorri-minion-spawn")),
                new ConfiguredSpaceReference(
                    Map, new SpaceId($"q{minionAt.Q}_r{minionAt.R}")),
                minionAt);
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, spaces,
                    Array.Empty<HexCoordinate>()),
                new[]
                {
                    new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Enemy, enemyAt),
                    new HeroOccupancy(SecondEnemy, secondEnemyAt)
                },
                Array.Empty<ObstacleOccupancy>(), new[] { minion });
        }
    }
}
