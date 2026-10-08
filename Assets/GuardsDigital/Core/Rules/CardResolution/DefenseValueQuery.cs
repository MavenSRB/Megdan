using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public static class DefenseValueQuery
    {
        /// <summary>
        /// The defender's Defense value: the printed icon, the p.14 minion contributions, p.16's item
        /// bonuses, and every typed modifier currently supplied to that hero.
        /// </summary>
        /// <param name="modifications">
        /// The modifications in effect; <see cref="RuleModificationSet.Empty"/> is the named
        /// no-modification value (milestone 6.8f). Each contributes with its own
        /// source discriminator, so an Active-effect bonus, a token-owned bonus, and a minion's -1 stay
        /// separately attributable inside one total.
        /// </param>
        /// <param name="heroItems">
        /// The match's items (milestone 7.2), or <c>null</c> in a composition that holds no item state -
        /// the same absence the modification set expresses above. p.16's Item Bonus is decided by
        /// <see cref="ItemBonusQuery"/> and not here: this query knows the printed result and the
        /// defender, which is exactly what that rule needs, and nothing about which items exist.
        /// </param>
        public static ValueResult Calculate(
            ActionOffer defenseOffer,
            IEnumerable<CardDefinition> definitions,
            HeroId defenderId,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            CardInstanceId incomingAttackCardId = default,
            CardInstance defendingCard = null) =>
            Calculate(defenseOffer, definitions, defenderId, OnlyBody, participants, board,
                modifications, heroItems, incomingAttackCardId, defendingCard);

        /// <summary>
        /// Milestone 9.21a (RZ-Q3, RZ-Q4). The same value for the named DEFENDING body: the Attack
        /// targeted one body, so p.14's minion modifiers are measured from that body's space.
        /// </summary>
        public static ValueResult Calculate(
            ActionOffer defenseOffer,
            IEnumerable<CardDefinition> definitions,
            HeroId defenderId,
            int defenderBody,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            CardInstanceId incomingAttackCardId = default,
            CardInstance defendingCard = null)
        {
            if (!defenderId.IsDefined)
            {
                throw new ArgumentException("Defense value query requires a defined defender ID.", nameof(defenderId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            // Temporal Punch substitutes the defending CARD's Initiative and matching items, not
            // the attacker's Initiative or a generic Defense modifier. The declaration is sourced
            // by the attack card, so another attack in the same timing window cannot inherit it.
            var substituted = incomingAttackCardId.IsDefined && modifications != null &&
                modifications.Modifications.Any(value =>
                    value.Modification.Kind == RuleModificationKind.DefendingCardUsesInitiative &&
                    value.SourceCardInstanceId.Equals(incomingAttackCardId));
            if (substituted && (defendingCard == null ||
                !defendingCard.Id.Equals(defenseOffer.SourceCardInstanceId) ||
                !defendingCard.OwnerId.Equals(defenderId)))
            {
                throw new ArgumentException(
                    "Initiative substitution requires the defending card instance.", nameof(defendingCard));
            }

            var printed = substituted
                ? PrintedValueQuery.CalculatePrintedInitiative(defendingCard, definitions)
                : PrintedValueQuery.CalculatePrintedValue(
                    defenseOffer, definitions, PrintedValueChannel.Defense);
            var contributions = new List<ValueContribution>(printed.Contributions);

            // p.16's item bonus is asked of the printed result rather than of the total, because the
            // page conditions it on what the card prints: "If an Action card has no matching icon, the
            // item has no effect on that particular Action card." Adding it here, before the minion and
            // modifier contributions, keeps that question about the icon alone - a Defense raised to a
            // positive number by an adjacent friendly melee minion is still a card with no Defense icon.
            if (heroItems != null)
            {
                contributions.AddRange(ItemBonusQuery.Contributions(
                    printed, heroItems, defenderId, modifications ?? RuleModificationSet.Empty));
            }

            var action = ActionOfferQuery.FindActionDefinition(defenseOffer, definitions);
            if (action == null)
            {
                throw new ArgumentException(
                    "Defense value query requires an offer backed by a current action definition.",
                    nameof(defenseOffer));
            }

            if (action.MinionDefenseContributions == MinionDefenseContributionPolicy.IncludeAll)
            {
                contributions.AddRange(CalculateMinionContributions(
                    defenderId, defenderBody, participants, board, modifications));
            }

            // Milestone 8.35z28. p.14's printed `+` on a Defense value, which that page illustrates on
            // Dodger's Vampiric Shield and states outright: "Primary Defense Value 4 (The '+' sign
            // signifies that the value can be increased by the card text.)" It is composed after the
            // minion modifiers and before the modification set for the reason the item bonus is
            // composed before both - each contribution keeps its own provenance inside one total, and
            // this one names the card instance and action whose text supplied it.
            //
            // It is asked of the whole action rather than of the printed result, because unlike p.16's
            // item this is the card's own sentence rather than a rule about what the card prints -
            // which is the same separation the Attack channel has had since milestone 8.19c.
            if (!substituted)
            {
                contributions.AddRange(RosterDynamicValueQuery.DefenseContributions(
                    action, defenseOffer, definitions.ToArray(), board, heroItems,
                    participants, modifications, defenderBody));
            }

            if (modifications != null)
            {
                contributions.AddRange(modifications.ValueContributions(
                    RuleObject.ForHero(defenderId), PrintedValueChannel.Defense));
            }

            return new ValueResult(PrintedValueChannel.Defense, contributions);
        }

        /// <summary>
        /// Unit-type question (6.8g ledger): intrinsic subtype. p.14 assigns every modifier by
        /// subtype - adjacent friendly/enemy melee or heavy, enemy ranged within radius 2 - and a
        /// generic `Counts As` minion declaration carries no subtype, so no p.14 branch can apply to
        /// it; the effective vocabulary deliberately has no subtypes.
        /// </summary>
        public static IReadOnlyList<ValueContribution> CalculateMinionContributions(
            HeroId defenderId,
            MatchParticipants participants,
            AdjacentMoveAuthority board) =>
            CalculateMinionContributions(
                defenderId, OnlyBody, participants, board, RuleModificationSet.Empty);

        public static IReadOnlyList<ValueContribution> CalculateMinionContributions(
            HeroId defenderId,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications) =>
            CalculateMinionContributions(
                defenderId, OnlyBody, participants, board, modifications);

        /// <summary>
        /// Milestone 9.21a. The body a caller that names only the hero means: the hero's only body.
        /// Such a caller is refused for a hero standing on several spaces rather than handed one of
        /// them, because which body was attacked is exactly what it did not say.
        /// </summary>
        private const int OnlyBody = 0;

        /// <summary>
        /// Milestone 9.21a (RZ-Q3). p.14's minion modifiers measured from the attacked BODY's space.
        /// </summary>
        public static IReadOnlyList<ValueContribution> CalculateMinionContributions(
            HeroId defenderId,
            int defenderBody,
            MatchParticipants participants,
            AdjacentMoveAuthority board) =>
            CalculateMinionContributions(
                defenderId, defenderBody, participants, board, RuleModificationSet.Empty);

        public static IReadOnlyList<ValueContribution> CalculateMinionContributions(
            HeroId defenderId,
            int defenderBody,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications)
        {
            if (!defenderId.IsDefined)
            {
                throw new ArgumentException("Defense value query requires a defined defender ID.", nameof(defenderId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var defenderOccupancies = board.Occupancies
                .Where(occupancy => occupancy.HeroId.Equals(defenderId) &&
                    (defenderBody == OnlyBody || occupancy.Body == defenderBody))
                .ToArray();
            if (defenderOccupancies.Length != 1)
            {
                throw new ArgumentException(
                    defenderBody == OnlyBody
                        ? "Defense value query requires the defender to occupy exactly one space, or the " +
                          "defending body to be named (milestone 9.21a)."
                        : $"Defense value query requires body {defenderBody} of the defender to be on the board.",
                    nameof(defenderId));
            }

            var defenderTeam = participants.GetTeamForHero(defenderId).Side;
            var defenderCoordinate = defenderOccupancies[0].Coordinate;
            var contributions = CalculateMinionContributions(
                defenderTeam, defenderCoordinate, board.Minions).ToList();

            // An action-scoped counts-as token is still a token in the board register. Its
            // effective minion type contributes from the ordinary melee profile, but only while
            // the live modification set actually overlays that type.
            if (modifications != null)
            {
                foreach (var token in board.PlacedTokens.OrderBy(value =>
                    value.Id.Value, StringComparer.Ordinal))
                {
                    var type = EffectiveUnitKindQuery.TokenMinionType(
                        RuleObject.ForToken(token.Id), modifications);
                    if (type == StandardMinionType.None)
                        continue;
                    var effectiveController = EffectiveUnitKindQuery.TokenMinionController(
                        RuleObject.ForToken(token.Id), modifications);
                    if (!effectiveController.IsDefined)
                        continue;
                    var controllerTeam = participants.GetTeamForHero(
                        effectiveController).Side;
                    var distance = HexDistanceQuery.Calculate(
                        defenderCoordinate, token.Coordinate);
                    var amount = GetMinionDefenseModifier(
                        defenderTeam, controllerTeam, type, distance);
                    if (amount != 0)
                        contributions.Add(ValueContribution.FromContinuousSource(
                            "effective-minion-token:" + token.Id.Value,
                            PrintedValueChannel.Defense, amount));
                }
            }

            return Array.AsReadOnly(contributions.ToArray());
        }

        /// <summary>
        /// The standard-minion part of p.14, evaluated from public board facts. Both live combat
        /// and an idle hero's modifier preview use this same query; no card is presumed selected.
        /// </summary>
        public static IReadOnlyList<ValueContribution> CalculateMinionContributions(
            TeamSide defenderTeam,
            HexCoordinate defenderCoordinate,
            IEnumerable<MinionOccupancy> minions)
        {
            if (defenderTeam != TeamSide.Red && defenderTeam != TeamSide.Blue)
                throw new ArgumentOutOfRangeException(nameof(defenderTeam));
            if (minions == null) throw new ArgumentNullException(nameof(minions));
            var contributions = new List<ValueContribution>();
            foreach (var minion in minions.OrderBy(value => value.MinionId.Value, StringComparer.Ordinal))
            {
                var distance = HexDistanceQuery.Calculate(defenderCoordinate, minion.Coordinate);
                var amount = GetMinionDefenseModifier(defenderTeam, minion.TeamSide, minion.MinionType, distance);
                if (amount != 0)
                    contributions.Add(new ValueContribution(
                        ValueContributionKind.MinionDefenseModifier, minion.MinionId,
                        PrintedValueChannel.Defense, amount));
            }
            return Array.AsReadOnly(contributions.ToArray());
        }

        /// <summary>
        /// Finding `F-ER2-09`: p.14's <em>Minion Defense Modifiers</em> box is read from the declared
        /// standard-minion content rather than restated here as three branches. Every number and every
        /// distance is unchanged; what moved is where the rule is written down.
        ///
        /// A distance of zero cannot occur - a minion and the defender cannot share a space - but is
        /// not special-cased, because the declared rows are maxima and a nearer piece is covered by the
        /// row that reaches past it.
        /// </summary>
        private static int GetMinionDefenseModifier(
            TeamSide defenderTeam,
            TeamSide minionTeam,
            StandardMinionType minionType,
            int distance)
        {
            var relationship = defenderTeam == minionTeam
                ? GuardsDigital.Core.Rules.Consequences.MinionDefenseRelationship.Friendly
                : GuardsDigital.Core.Rules.Consequences.MinionDefenseRelationship.Enemy;
            return GuardsDigital.Core.Rules.Consequences.StandardMinionProfileCatalog.Rulebook
                .Get(minionType)
                .DefenseModifierAt(relationship, distance);
        }
    }
}
