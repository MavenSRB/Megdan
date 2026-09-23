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
    /// Milestone 8.90. The user's ruling on open question 36 - "IMMUNE heroes are untargetable as per
    /// rulebook" - applied to the two hostile selections milestone 8.89's sweep found without p.18's
    /// immunity filter: Wasp's High Voltage (`synthetic-hero-adjacent-to-minion@1`, "an enemy hero who
    /// was adjacent to that minion discards a card") and Misa's straight-line riders
    /// (`choose-traversed-unit@1`, "an enemy hero you moved through").
    ///
    /// <para>Each case stages an immune enemy hero and a second enemy hero who is not, both where the
    /// clause would reach them. Before 8.90 both were offered; the ruling makes Shock's reading - the
    /// immune hero is never the one chosen - govern these clauses too. A presence question is not
    /// affected: p.19 still counts immune units there.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class ImmuneHeroSelectionTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Immune = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Exposed = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MapId Map = new MapId("immune-hero-selection");
        private static readonly MinionId Minion = new MinionId("immune-selection-minion");

        [Test]
        public void HighVoltage_NeverOffersAnImmuneHeroBesideTheDefeatedMinion()
        {
            // The minion at (2,0); both enemy heroes stand beside it.
            var board = Board(
                new[]
                {
                    new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Immune, new HexCoordinate(3, 0)),
                    new HeroOccupancy(Exposed, new HexCoordinate(2, 1))
                },
                new[] { MinionAt(new HexCoordinate(2, 0)) });

            var handler = SyntheticTargetingCapabilities.Handlers()
                .Single(candidate => candidate.Reference.Equals(SyntheticTargetingCapabilities.HeroAdjacentToMinionV1));
            var result = handler.Execute(Context(board, new Dictionary<string, EffectValue>
            {
                [SyntheticTargetingCapabilities.MinionParameterName] = EffectValue.Minion(Minion),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            }));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates, Is.EqualTo(new[] { EffectValue.Hero(Exposed) }),
                "The immune hero beside the minion is not offered; the other one still is.");
        }

        [Test]
        public void MisasLineRider_NeverOffersAnImmuneHeroSheMovedThrough()
        {
            // She moved from (0,0) to (3,0); both enemy heroes stand in the line's interior.
            var board = Board(
                new[]
                {
                    new HeroOccupancy(Actor, new HexCoordinate(3, 0)),
                    new HeroOccupancy(Immune, new HexCoordinate(1, 0)),
                    new HeroOccupancy(Exposed, new HexCoordinate(2, 0))
                },
                Array.Empty<QuickGameMinionSetupState>());

            var handler = MatchContentCatalogs.ForThisBuild().Capabilities
                .Load(SyntheticRosterCapabilities.ChooseTraversedUnitV1);
            var result = handler.Execute(Context(board, new Dictionary<string, EffectValue>
            {
                ["origin"] = EffectValue.Space(new HexCoordinate(0, 0)),
                ["destination"] = EffectValue.Space(new HexCoordinate(3, 0)),
                [SyntheticRosterCapabilities.UnitKindParameter] = EffectValue.Integer((int)RosterUnitKind.Hero),
                [SyntheticRosterCapabilities.RelationParameter] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource)
            }));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(result.Choice.Candidates, Is.EqualTo(new[] { EffectValue.Hero(Exposed) }),
                "The immune hero she moved through is not offered; the other one still is.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static AdjacentMoveAuthority Board(
            IEnumerable<HeroOccupancy> heroes, IEnumerable<QuickGameMinionSetupState> minions)
        {
            var spaces = new List<HexCoordinate>();
            for (var q = -4; q <= 4; q++)
            {
                for (var r = -4; r <= 4; r++)
                {
                    if (Math.Abs(q + r) <= 4) spaces.Add(new HexCoordinate(q, r));
                }
            }

            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, spaces, Array.Empty<HexCoordinate>()),
                heroes.ToArray(),
                Array.Empty<ObstacleOccupancy>(),
                minions.ToArray());
        }

        private static QuickGameMinionSetupState MinionAt(HexCoordinate at) =>
            new QuickGameMinionSetupState(
                Minion,
                new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("immune-selection-spawn")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, IReadOnlyDictionary<string, EffectValue> inputs)
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var action = new ActionDefinition(
                new ActionDefinitionId("immune-selection-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                ProbeText());
            var definition = new CardDefinition(
                new CardDefinitionId("immune-selection-probe"),
                "rev-1",
                "TEST - immune selection probe",
                new InitiativeValue(9),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { action });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("immune-selection-instance"), definition.Reference, Actor);

            return new EffectCapabilityContext(
                new EffectNodeId("immune-selection-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                card.Id,
                card.DefinitionReference,
                action.Id,
                inputs,
                1,
                1,
                participants,
                null,
                ImmunityFor(Immune),
                EffectCardState.Of(new[] { definition }, new[] { card }),
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
            var node = new EffectNodeId("immune-selection-root");
            return new EffectDefinition(
                new EffectDefinitionId("immune-selection-text"),
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

        /// <summary>
        /// A live set carrying one All-activity immunity on <paramref name="heroId"/>, built the way a
        /// match builds it - from an Active effect in its active phase.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId)
        {
            var effect = new ActiveEffectDefinition(
                "immune-selection-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                "ImmuneSelection",
                "immune-selection",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(RuleActivity.All) });

            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"immune-selection-{heroId.Value}"),
                effect,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("immune-selection-source"),
                new ActionDefinitionId("immune-selection-action"),
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
                set.IsImmuneTo(RuleObject.ForHero(heroId), RuleActivity.Skill, RuleObject.ForHero(Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the case proves nothing.");
            return set;
        }
    }
}
