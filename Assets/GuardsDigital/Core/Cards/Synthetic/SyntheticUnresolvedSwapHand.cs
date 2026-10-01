using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>
    /// Milestone 9.32b. Test-only Attacks in the shape of Takahide's Set an Example and Hold my Saké:
    /// "Target a unit adjacent to you. After the attack: A friendly hero in radius may swap their
    /// unresolved card with a card in their hand[, or in their discard]." Fixture content, not a
    /// verified card or a rule-source interpretation; no Takahide face is registered.
    ///
    /// <para>The composition is the one 9.32d will give the printed faces: Takahide chooses the
    /// friendly hero (a selection that names the swap as its consumer, so only a hero the swap can act
    /// on is offered - 6.8l), then that hero's <c>May</c>, answered on their seat, then the swap, whose
    /// card that hero chooses.</para>
    /// </summary>
    public static class SyntheticUnresolvedSwapHand
    {
        public const string ContentRevision = "9.32b-rev-1";
        public const int Radius = 3;
        public const int AttackValue = 2;

        public static readonly CardDefinitionId RallyId = new CardDefinitionId("synthetic-red-rally");
        public static readonly CardDefinitionId RallySakeId = new CardDefinitionId("synthetic-red-rally-sake");
        public static readonly ActionDefinitionId PrimaryId = new ActionDefinitionId("primary-attack");

        public static readonly EffectNodeId FriendNode = new EffectNodeId("friendly-hero-in-radius");
        public static readonly EffectNodeId MayNode = new EffectNodeId("may-swap");
        public static readonly EffectNodeId SwapNode = new EffectNodeId("swap-unresolved-card");

        /// <summary>Rally ("with a card in their hand") and Rally Saké ("... or in their discard").</summary>
        public static IReadOnlyList<CardDefinition> Definitions => LazyDefinitions.Value;

        /// <summary>The handlers this content needs beyond the build's own catalog (unregistered until 9.32h).</summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() => SyntheticUnresolvedSwapCapabilities.Handlers();

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Rally(RallyId, "TEST - Rally", fromDiscard: false),
                Rally(RallySakeId, "TEST - Rally Sake", fromDiscard: true)
            }));

        private static CardDefinition Rally(CardDefinitionId id, string name, bool fromDiscard) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(11),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        PrimaryId, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, AttackValue),
                            new PrintedValueDefinition(PrintedValueChannel.Radius, Radius)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        SwapText(id, fromDiscard),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheAttack)
                });

        private static EffectDefinition SwapText(CardDefinitionId card, bool fromDiscard)
        {
            var root = new EffectNodeId("after-the-attack");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { FriendNode, MayNode }),
                    new EffectNodeDefinition(
                        FriendNode, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false)),
                            // "A friendly hero" never includes the actor (p.10).
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(SwapNode))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        MayNode, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { SwapNode },
                        inputs: new[] { ChosenHero(EffectNodeDefinition.ChooserParameterName) },
                        chooser: EffectChooserRole.BoundHero),
                    new EffectNodeDefinition(
                        SwapNode, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticUnresolvedSwapCapabilities.SwapUnresolvedCardV1,
                        inputs: new[]
                        {
                            ChosenHero(SyntheticUnresolvedSwapCapabilities.OwnerParameterName),
                            EffectInputBinding.FromLiteral(
                                SyntheticUnresolvedSwapCapabilities.FromDiscardParameterName,
                                EffectValue.Boolean(fromDiscard))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticUnresolvedSwapCapabilities.SwappedOutOutputName, EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticUnresolvedSwapCapabilities.SwappedInOutputName, EffectValueKind.CardInstance)
                        })
                });
        }

        private static EffectInputBinding ChosenHero(string parameter) =>
            EffectInputBinding.FromNodeOutput(
                parameter, FriendNode, SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero);
    }
}
