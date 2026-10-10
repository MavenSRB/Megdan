using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Transitions
{
    /// <summary>
    /// W-022, PVE-D05/D12. A mode's round-start placement: every hero the mode schedules for this
    /// round that is not on the board is placed, one at a time.
    /// </summary>
    /// <remarks>
    /// <para>A designated placement takes its space when it is empty, otherwise the nearest empty
    /// playable spaces to it, and pauses for a choice only when several are equally near. A
    /// player-chosen placement offers the empty placement spaces, otherwise the nearest empty
    /// spaces to them, and always pauses for the choice. A hero with nowhere to stand is left off
    /// the board.</para>
    /// <para>Which round's placement runs is part of the transition's own identity
    /// (<see cref="MatchModeRules.PlacementTransitionId"/>), so these rules hold no round state and
    /// restore with nothing but the board.</para>
    /// </remarks>
    public sealed class ModeRoundStartPlacementRules : ISystemTransitionStepRules
    {
        private const string ItemPrefix = "mode-place:";
        private const string OptionPrefix = "space:";
        private readonly MatchModeRules rules;
        private readonly AdjacentMoveAuthority board;
        private readonly MatchParticipants participants;

        public ModeRoundStartPlacementRules(
            MatchModeRules rules, AdjacentMoveAuthority board, MatchParticipants participants)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
        }

        public static SystemTransitionItemId ItemFor(HeroId heroId) =>
            new SystemTransitionItemId(ItemPrefix + heroId.Value);

        public static bool TryReadItem(SystemTransitionItemId itemId, out HeroId heroId)
        {
            heroId = default;
            if (!itemId.IsDefined || !itemId.Value.StartsWith(ItemPrefix, StringComparison.Ordinal)) return false;
            heroId = new HeroId(itemId.Value.Substring(ItemPrefix.Length));
            return heroId.IsDefined;
        }

        public static SystemTransitionOptionId OptionFor(HexCoordinate space) =>
            new SystemTransitionOptionId(OptionPrefix +
                space.Q.ToString(CultureInfo.InvariantCulture) + "," + space.R.ToString(CultureInfo.InvariantCulture));

        public static bool TryReadOption(SystemTransitionOptionId optionId, out HexCoordinate space)
        {
            space = default;
            if (!optionId.IsDefined || !optionId.Value.StartsWith(OptionPrefix, StringComparison.Ordinal)) return false;
            var parts = optionId.Value.Substring(OptionPrefix.Length).Split(',');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var q) ||
                !int.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var r))
                return false;
            space = new HexCoordinate(q, r);
            return true;
        }

        /// <summary>The scheduled heroes of <paramref name="round"/> that are still off the board.</summary>
        public IReadOnlyList<ModeHeroPlacement> Outstanding(int round) =>
            rules.RoundStartPlacements
                .Where(placement => placement.Round == round && !board.IsHeroOnBoard(placement.HeroId))
                .ToArray();

        public SystemTransitionStepResolution ResolveStep(SystemTransitionStepContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!MatchModeRules.TryReadPlacementRound(context.Definition.Id, out var round) ||
                !context.Step.Id.Equals(MatchModeRules.PlacementStepId))
            {
                throw new InvalidOperationException(
                    $"Placement step '{context.Step.Id}' of '{context.Definition.Id}' has no rules.");
            }

            var completed = new HashSet<SystemTransitionItemId>(context.CompletedItemIds);
            foreach (var placement in rules.RoundStartPlacements.Where(value => value.Round == round))
            {
                var item = ItemFor(placement.HeroId);
                if (completed.Contains(item))
                {
                    continue;
                }

                var options = OptionsFor(placement);
                if (board.IsHeroOnBoard(placement.HeroId) || options.Count == 0)
                {
                    return SystemTransitionStepResolution.ItemCompleted(item);
                }

                if (placement.Kind == ModeHeroPlacementKind.Designated && options.Count == 1)
                {
                    Commit(placement.HeroId, options[0]);
                    return SystemTransitionStepResolution.ItemCompleted(item);
                }

                return SystemTransitionStepResolution.PausedOn(new[]
                {
                    new SystemTransitionOpenItem(
                        item,
                        participants.GetTeamForHero(placement.HeroId).Side,
                        options.Select(OptionFor))
                });
            }

            return SystemTransitionStepResolution.Completed;
        }

        public void ApplyAnswer(SystemTransitionAnswerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!TryReadItem(context.ItemId, out var heroId) || !TryReadOption(context.OptionId, out var space))
            {
                throw new InvalidOperationException(
                    $"Placement answer '{context.ItemId}'/'{context.OptionId}' is not a placement.");
            }

            var placement = rules.RoundStartPlacements.SingleOrDefault(value => value.HeroId.Equals(heroId));
            if (placement == null || !OptionsFor(placement).Contains(space))
            {
                throw new InvalidOperationException(
                    $"'{SpaceText(space)}' is not a placement this match offers '{heroId}'.");
            }

            Commit(heroId, space);
        }

        /// <summary>The spaces a scheduled hero may be placed on now, in a stable order.</summary>
        public IReadOnlyList<HexCoordinate> OptionsFor(ModeHeroPlacement placement)
        {
            if (placement == null) throw new ArgumentNullException(nameof(placement));
            var references = placement.Kind == ModeHeroPlacementKind.Designated
                ? new[] { placement.DesignatedSpace.Value }
                : rules.PlayerPlacementSpaces.ToArray();
            var open = references.Where(board.IsEmptyPlayableSpace).ToArray();
            if (open.Length > 0)
            {
                return placement.Kind == ModeHeroPlacementKind.Designated ? open : Ordered(open);
            }

            var empty = board.MapDefinition.PlayableCoordinates.Where(board.IsEmptyPlayableSpace).ToArray();
            if (empty.Length == 0)
            {
                return Array.Empty<HexCoordinate>();
            }

            var distances = empty.Select(space => new
            {
                Space = space,
                Distance = references.Min(reference => HexDistanceQuery.Calculate(space, reference))
            }).ToArray();
            var nearest = distances.Min(value => value.Distance);
            return Ordered(distances.Where(value => value.Distance == nearest).Select(value => value.Space));
        }

        private void Commit(HeroId heroId, HexCoordinate space)
        {
            if (!board.CommitValidatedOffBoardHeroPlacement(heroId, space))
            {
                throw new InvalidOperationException(
                    $"A validated placement of '{heroId}' on '{SpaceText(space)}' could not be committed.");
            }
        }

        private static IReadOnlyList<HexCoordinate> Ordered(IEnumerable<HexCoordinate> spaces) =>
            spaces.OrderBy(space => space.R).ThenBy(space => space.Q).ToArray();

        private static string SpaceText(HexCoordinate space) => space.Q + "," + space.R;
    }
}
