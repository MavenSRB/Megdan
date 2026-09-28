using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
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
    /// <summary>
    /// Milestone 9.2. A selection bounded by the card's Radius reads the printed icon with p.16's items,
    /// not a number. Cutter's Bombardment bound its disk as a literal 3 - the icon's number, which no
    /// Radius item raises - the shape milestone 8.83 corrected on moves and 9.2's audit found on six
    /// selections (Sabina's Close Support and Covering Fire; Cutter's Bombardment, Barrage, Broadside
    /// and X Marks the Spot).
    /// </summary>
    public sealed class PrintedRadiusSelectionTests
    {
        private static readonly HeroId Cutter = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("printed-radius-selection");
        private static readonly HexCoordinate CutterAt = new HexCoordinate(0, 0);

        /// <summary>Four spaces out on a straight file: outside Radius 3, inside Radius 3 + 1.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(4, 0);

        /// <summary>"adjacent to another enemy unit": a Blue minion beside the enemy hero.</summary>
        private static readonly HexCoordinate MinionAt = new HexCoordinate(5, 0);

        [TestCase(0, false)]
        [TestCase(1, true)]
        public void Bombardment_ReachesFurther_WithARadiusItem(int radiusItems, bool offered)
        {
            var offer = CandidateHeroes(CutterCardContent.Bombardment, radiusItems);

            Assert.That(offer.Contains(Enemy), Is.EqualTo(offered),
                radiusItems == 0
                    ? "Bombardment prints Radius 3, and the enemy hero stands four spaces away."
                    : "A +1 Radius item makes it 4 (p.16), and the selection reads the icon (p.9).");
        }

        private static IReadOnlyList<HeroId> CandidateHeroes(CardDefinitionId cardId, int radiusItems)
        {
            var definition = CutterCardContent.Definition(cardId);
            var action = definition.ActionDefinitions.Single(
                candidate => candidate.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var participants = StagedBoardParticipants.Create();
            var playable = (from q in Enumerable.Range(-8, 17)
                            from r in Enumerable.Range(-8, 17)
                            where Math.Abs(q + r) <= 8
                            select new HexCoordinate(q, r)).ToArray();
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, playable),
                new[] { new HeroOccupancy(Cutter, CutterAt), new HeroOccupancy(Enemy, EnemyAt) },
                Array.Empty<ObstacleOccupancy>(),
                new[]
                {
                    new QuickGameMinionSetupState(
                        new MinionId("printed-radius-minion"),
                        new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                        TeamSide.Blue,
                        StandardMinionType.Melee,
                        default,
                        new ConfiguredSpaceReference(Map, new SpaceId($"q{MinionAt.Q}_r{MinionAt.R}")),
                        MinionAt)
                });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("printed-radius-probe"), definition.Reference, Cutter);
            var context = new EffectResolutionContext(
                action.EffectRoot,
                MatchContentCatalogs.ForThisBuild().Capabilities,
                participants,
                new ActiveEffectLifecycle(),
                1,
                1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { definition }, new[] { card }),
                action.Kind,
                default,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board),
                Items(participants, radiusItems));

            var step = EffectResolutionSession.Start(
                new EffectResolutionId("printed-radius-resolution"),
                card.Id,
                definition.Reference,
                action.Id,
                Cutter,
                Cutter,
                ActionInvocationContext.PrintedPrimary,
                context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            var choice = step.Frame?.PendingChoice;
            return choice == null
                ? Array.Empty<HeroId>()
                : choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.Value.HeroId)
                    .ToArray();
        }

        private static HeroItemState Items(MatchParticipants participants, int radiusItems)
        {
            if (radiusItems <= 0)
            {
                return new HeroItemState(participants);
            }

            Assert.That(
                HeroDashboard.RulebookLayout.TryFindSlotAccepting(
                    PrintedValueChannel.Radius, out var slot),
                Is.True);
            return HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(Enumerable.Range(0, radiusItems)
                    .Select(index => new HeroItem(
                        Cutter,
                        new CardInstanceId($"cutter-radius-item-{index}"),
                        slot.Position,
                        PrintedValueChannel.Radius,
                        index + 1))
                    .ToArray()));
        }
    }
}
