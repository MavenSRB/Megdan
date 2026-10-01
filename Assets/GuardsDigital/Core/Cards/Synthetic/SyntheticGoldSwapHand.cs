using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>
    /// Milestone 9.32a. Test-only basic cards in the shapes Takahide's basic cards print: three gold
    /// cards, two of which start in the deck, that swap themselves with a different gold card in the
    /// deck, and a silver Skill that swaps "your gold card" with one. Fixture content, not a verified
    /// card or a rule-source interpretation; no Takahide face is registered.
    ///
    /// <list type="bullet">
    /// <item><see cref="BeeId"/> - a melee basic Attack, "After the attack: Swap this card with a
    /// different gold card in your deck." Starts in the deck.</item>
    /// <item><see cref="ButterflyId"/> - a basic Movement whose text, "Swap this card with a different
    /// gold card in your deck", runs after the Movement (TK-Q1). Starts in the hand.</item>
    /// <item><see cref="TigerId"/> - Bee's twin with a higher Attack, so a test can tell which deck card
    /// came in. Starts in the deck.</item>
    /// <item><see cref="BushidoId"/> - a silver basic Skill, "Swap your gold card with a different gold
    /// card in your deck; if you swap a resolved or discarded gold card this way, the new card is
    /// facedown." Starts in the hand.</item>
    /// </list>
    /// </summary>
    public static class SyntheticGoldSwapHand
    {
        public const string ContentRevision = "9.32a-rev-1";
        public const int BeeAttack = 5;
        public const int TigerAttack = 7;
        public const int ButterflyMovement = 5;

        public static readonly CardDefinitionId BeeId = new CardDefinitionId("synthetic-gold-bee");
        public static readonly CardDefinitionId ButterflyId = new CardDefinitionId("synthetic-gold-butterfly");
        public static readonly CardDefinitionId TigerId = new CardDefinitionId("synthetic-gold-tiger");
        public static readonly CardDefinitionId BushidoId = new CardDefinitionId("synthetic-silver-bushido");

        public static readonly ActionDefinitionId PrimaryId = new ActionDefinitionId("primary");
        public static readonly ActionDefinitionId SecondaryDefenseId = new ActionDefinitionId("secondary-defense");

        /// <summary>The four definitions: Bee, Butterfly, Tiger, Bushido.</summary>
        public static IReadOnlyList<CardDefinition> Definitions => LazyDefinitions.Value;

        /// <summary>The handlers this content needs beyond the build's own catalog (unregistered until 9.32h).</summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() => SyntheticDeckSwapCapabilities.Handlers();

        /// <summary>
        /// The owner's four cards in their starting places: Butterfly and Bushido in the hand, Bee and
        /// Tiger in the deck ("This card starts the game in your deck").
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateInstances(HeroId ownerId, string instanceIdPrefix)
        {
            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException("Card instance identities are per-seat.", nameof(instanceIdPrefix));
            }

            var definitions = Definitions;
            return Array.AsReadOnly(new[]
            {
                CardInstance.CreateInDeck(Instance(instanceIdPrefix, BeeId), definitions[0].Reference, ownerId),
                CardInstance.CreateInHand(Instance(instanceIdPrefix, ButterflyId), definitions[1].Reference, ownerId),
                CardInstance.CreateInDeck(Instance(instanceIdPrefix, TigerId), definitions[2].Reference, ownerId),
                CardInstance.CreateInHand(Instance(instanceIdPrefix, BushidoId), definitions[3].Reference, ownerId)
            });
        }

        public static CardInstanceId Instance(string instanceIdPrefix, CardDefinitionId definition) =>
            new CardInstanceId($"{instanceIdPrefix}-{definition.Value}");

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Gold(BeeId, "TEST - Bee", 7, ActionKind.Attack, BeeAttack, CardTextTiming.AfterTheAttack),
                Gold(ButterflyId, "TEST - Butterfly", 8, ActionKind.Movement, ButterflyMovement,
                    CardTextTiming.AfterTheMovement),
                Gold(TigerId, "TEST - Tiger", 9, ActionKind.Attack, TigerAttack, CardTextTiming.AfterTheAttack),
                Bushido()
            }));

        private static CardDefinition Gold(
            CardDefinitionId id, string name, int initiative, ActionKind kind, int value, CardTextTiming timing) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[]
                {
                    new ActionDefinition(
                        PrimaryId, kind, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                kind == ActionKind.Attack ? PrintedValueChannel.Attack : PrintedValueChannel.Movement,
                                value)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        kind == ActionKind.Attack ? AttackRangeKind.NonRanged : AttackRangeKind.None,
                        default,
                        SwapText(id, SyntheticDeckSwapCapabilities.SwapThisCardWithDeckCardV1,
                            EffectFailurePolicy.IfAble, facedownWhenResting: null),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        timing),
                    Defense(2)
                });

        private static CardDefinition Bushido() =>
            new CardDefinition(
                BushidoId, ContentRevision, "TEST - Bushido", new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        PrimaryId, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        SwapText(BushidoId, SyntheticDeckSwapCapabilities.SwapOwnedColorCardWithDeckCardV1,
                            EffectFailurePolicy.Mandatory, facedownWhenResting: true)),
                    Defense(6)
                });

        private static EffectDefinition SwapText(
            CardDefinitionId card, EffectCapabilityReference capability, EffectFailurePolicy policy,
            bool? facedownWhenResting)
        {
            var root = new EffectNodeId("swap-with-deck-gold");
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary(SyntheticDeckSwapCapabilities.ColorParameterName, CardColor.Gold)
            };
            if (facedownWhenResting.HasValue)
            {
                inputs.Add(EffectInputBinding.FromLiteral(
                    SyntheticDeckSwapCapabilities.FacedownWhenRestingParameterName,
                    EffectValue.Boolean(facedownWhenResting.Value)));
            }

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, policy, capability,
                        inputs: inputs,
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticDeckSwapCapabilities.SwappedOutOutputName,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(SyntheticDeckSwapCapabilities.SwappedInOutputName,
                                EffectValueKind.CardInstance)
                        })
                });
        }

        private static ActionDefinition Defense(int value) =>
            new ActionDefinition(
                SecondaryDefenseId, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
    }
}
