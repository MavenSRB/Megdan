using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;

namespace GuardsDigital.Core.Rules.CardResolution
{
    [Flags]
    public enum CardValuePreviewSources
    {
        None = 0,
        Items = 1,
        Minions = 2,
        ActiveEffects = 4,
        CardEffects = 8,
        All = Items | Minions | ActiveEffects | CardEffects
    }

    /// <summary>
    /// Reads the existing value queries, without performing actions or predicting unchosen targets,
    /// then exposes only the contribution categories selected for presentation.
    /// </summary>
    public static class CardValuePreviewQuery
    {
        public static IReadOnlyList<CardValuePreview> Read(
            IReadOnlyList<CardDefinition> definitions, IReadOnlyList<CardInstance> cards,
            MatchParticipants participants, AdjacentMoveAuthority board, HeroItemState items,
            RuleModificationSet modifications, EffectRosterState roster, int roundIndex,
            int turnIndex, GuardsDigital.Core.Components.MarkerAttachmentRegister markers,
            PendingAttackResolution attack, CardValuePreviewSources sources = CardValuePreviewSources.All)
        {
            if ((sources & ~CardValuePreviewSources.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(sources));
            if (sources == CardValuePreviewSources.None)
                return Array.Empty<CardValuePreview>();

            var results = new List<CardValuePreview>();
            foreach (var card in cards)
            {
                var definition = definitions.Single(value => value.Reference.Equals(card.DefinitionReference));
                // Zero deltas are implicit. Keep the seat/wire projection sparse even late in a match.
                void Add(PrintedValueChannel channel, ValueResult value)
                {
                    var bonus = DisplayedBonus(value.Contributions, sources);
                    if (bonus != 0)
                        results.Add(new CardValuePreview(card.OwnerId, definition.Reference, channel, bonus));
                }

                if (definition.Initiative.IsDefined)
                {
                    var printed = PrintedValueQuery.CalculatePrintedInitiative(card, definitions);
                    var withItems = ItemBonusQuery.Apply(printed, items, card.OwnerId, modifications);
                    Add(PrintedValueChannel.Initiative, new ValueResult(
                        PrintedValueChannel.Initiative,
                        withItems.Contributions.Concat(modifications.ValueContributions(
                            RuleObject.ForHero(card.OwnerId), PrintedValueChannel.Initiative))));
                }

                foreach (var action in definition.ActionDefinitions)
                {
                    var offer = new ActionOffer(card.Id, definition.Reference, action.Id, card.OwnerId,
                        action.Kind == ActionKind.Defense ? ActionInvocationContext.Reaction :
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary
                            ? ActionInvocationContext.PrintedPrimary : ActionInvocationContext.PrintedSecondary,
                        true, action.AttackRange, default);
                    foreach (var printed in action.BaseValues)
                    {
                        ValueResult value;
                        switch (printed.Channel)
                        {
                            case PrintedValueChannel.Attack:
                                var boundAttack = attack != null && attack.SourceCardInstanceId.Equals(card.Id) &&
                                    attack.ActionDefinitionId.Equals(action.Id) && attack.AttackValue != null;
                                value = boundAttack ? attack.AttackValue :
                                    AttackValueQuery.Calculate(offer, definitions, items, modifications);
                                if (!boundAttack)
                                    value = new ValueResult(printed.Channel, value.Contributions.Concat(
                                        RosterDynamicValueQuery.Contributions(action, offer, default,
                                            printed.Channel, definitions, cards, participants, board,
                                            modifications, roster, roundIndex, turnIndex,
                                            spaces: board, obstacles: board, markers: markers,
                                            beforeTargetSelection: true)));
                                break;
                            case PrintedValueChannel.Defense:
                                var bodies = board.GetHeroBodies(card.OwnerId);
                                var body = attack != null && attack.TargetHeroId.Equals(card.OwnerId)
                                    ? attack.Target.HeroBody
                                    : board.TryGetActingBody(card.OwnerId, out var actingBody) ? actingBody
                                    : bodies.Count == 1 ? bodies[0].Body : 0;
                                value = body > 0 && bodies.Any(b => b.Body == body)
                                    ? DefenseValueQuery.Calculate(offer, definitions, card.OwnerId, body,
                                        participants, board, modifications, items,
                                        attack != null && attack.TargetHeroId.Equals(card.OwnerId)
                                            ? attack.SourceCardInstanceId : default, card)
                                    : Basic(offer, definitions, items, modifications, printed.Channel);
                                break;
                            case PrintedValueChannel.Movement:
                                value = MovementValueQuery.Calculate(offer, definitions, items, modifications);
                                break;
                            case PrintedValueChannel.Range:
                                value = AttackSequenceBulletSubjectQuery.RangeValue(offer, definitions,
                                    participants, board, items, roster, modifications, cards);
                                break;
                            case PrintedValueChannel.Radius:
                                value = RadiusValueQuery.Calculate(offer, definitions, items, modifications);
                                value = new ValueResult(printed.Channel, value.Contributions.Concat(
                                    RosterDynamicValueQuery.CardStateContributions(
                                        action, card.Id, printed.Channel, cards)));
                                break;
                            default:
                                value = Basic(offer, definitions, items, modifications, printed.Channel);
                                break;
                        }
                        Add(printed.Channel, value);
                    }
                }

                if (definition.UltimateRule != null)
                    foreach (var printed in definition.UltimateRule.PrintedValues)
                    {
                        var offer = new ActionOffer(card.Id, definition.Reference,
                            definition.UltimateRule.EffectSourceId, card.OwnerId,
                            ActionInvocationContext.TextGranted, true);
                        Add(printed.Channel, Basic(offer, definitions, items, modifications, printed.Channel));
                    }
            }
            return Array.AsReadOnly(results.ToArray());
        }

        private static int DisplayedBonus(
            IEnumerable<ValueContribution> contributions, CardValuePreviewSources sources) =>
            (contributions ?? throw new ArgumentNullException(nameof(contributions)))
                .Where(contribution => IsVisible(contribution.Kind, sources))
                .Sum(contribution => contribution.Amount);

        private static bool IsVisible(ValueContributionKind kind, CardValuePreviewSources sources) =>
            kind == ValueContributionKind.ItemModifier && sources.HasFlag(CardValuePreviewSources.Items) ||
            kind == ValueContributionKind.MinionDefenseModifier && sources.HasFlag(CardValuePreviewSources.Minions) ||
            kind == ValueContributionKind.ActiveEffectModifier && sources.HasFlag(CardValuePreviewSources.ActiveEffects) ||
            (kind == ValueContributionKind.CardTextModifier ||
             kind == ValueContributionKind.ContinuousSourceModifier) &&
                sources.HasFlag(CardValuePreviewSources.CardEffects);

        private static ValueResult Basic(ActionOffer offer, IReadOnlyList<CardDefinition> definitions,
            HeroItemState items, RuleModificationSet modifications, PrintedValueChannel channel)
        {
            var printed = PrintedValueQuery.CalculatePrintedValue(offer, definitions, channel);
            var value = ItemBonusQuery.Apply(printed, items, offer.ActorId, modifications);
            return new ValueResult(channel, value.Contributions.Concat(
                modifications.ValueContributions(RuleObject.ForHero(offer.ActorId), channel)));
        }
    }
}
