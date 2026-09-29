using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>The Oaths' Defense text and the two color-discard Skills.</summary>
    public static class SnorriGreenEffectContent
    {
        public static EffectCapabilityReference SelectRuneV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("snorri-select-disruption-rune"), 1);

        private static EffectNodeId Id(string value) => new EffectNodeId(value);

        public static EffectDefinition Oath(CardDefinitionId card, string revision) =>
            new EffectDefinition(
                new EffectDefinitionId(card.Value + "-defense-text"),
                revision, Id("root"),
                new[]
                {
                    new EffectNodeDefinition(
                        Id("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Id("block-condition"), Id("rune-gate") }),
                    new EffectNodeDefinition(
                        Id("block-condition"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SnorriGreenBlockContent.ConditionFor(card)),
                    new EffectNodeDefinition(
                        Id("rune-gate"), EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        SnorriComponentContent.AnySupportRuneV1,
                        childNodeIds: new[] { Id("immunity") },
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("mask", SnorriRuneMask.Runes1234)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("active", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        Id("immunity"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SnorriGreenBlockContent.ImmunityFor(card),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        })
                });

        /// <summary>
        /// The printed glyphs, by their order on Rune Sigils (the marker ordinals): the hook is rune 4
        /// (green), the axe rune 2 (silver) and the anvil rune 3 (blue). Milestone 9.17a: blue had been
        /// given to rune 1, the winged glyph this face does not print.
        /// </summary>
        public static EffectDefinition ChargedRunestone(string revision)
        {
            var children = new[] { Id("target"), Id("rune-4"), Id("rune-2"),
                Id("rune-3") };
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    Id("root"), EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory,
                    SnorriComponentContent.AnySupportRuneV1,
                    childNodeIds: new[] { Id("body") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("mask", SnorriRuneMask.Runes234)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("active", EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(
                    Id("body"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: children),
                Target()
            };
            AddRuneDiscard(nodes, 4, CardColor.Green);
            AddRuneDiscard(nodes, 2, CardColor.Silver);
            AddRuneDiscard(nodes, 3, CardColor.Blue);
            return new EffectDefinition(
                new EffectDefinitionId("snorri-charged-runestone-skill-text"),
                revision, Id("root"), nodes);
        }

        public static EffectDefinition InscribedRunebomb(string revision) =>
            new EffectDefinition(
                new EffectDefinitionId("snorri-inscribed-runebomb-skill-text"),
                revision, Id("root"),
                new[]
                {
                    new EffectNodeDefinition(
                        Id("root"), EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        SnorriComponentContent.AnySupportRuneV1,
                        childNodeIds: new[] { Id("body") },
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("mask", SnorriRuneMask.Runes1234)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("active", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        Id("body"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Id("choose-rune"), Id("target"), Id("discard") }),
                    new EffectNodeDefinition(
                        Id("choose-rune"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SelectRuneV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding("color", EffectValueKind.Integer)
                        }),
                    Target(),
                    new EffectNodeDefinition(
                        Id("discard"), EffectNodeKind.Capability,
                        EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV1,
                        inputs: new[]
                        {
                            HeroInput(),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.ColorParameter,
                                Id("choose-rune"), "color", EffectValueKind.Integer,
                                EffectIntegerDomain.All<CardColor>())
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance)
                        })
                });

        private static void AddRuneDiscard(
            ICollection<EffectNodeDefinition> nodes, int rune, CardColor color)
        {
            var gate = Id("rune-" + rune);
            var discard = Id("discard-" + rune);
            nodes.Add(new EffectNodeDefinition(
                gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SnorriComponentContent.ActiveRuneV1,
                childNodeIds: new[] { discard },
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("rune", (SnorriRuneGlyph)rune)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("active", EffectValueKind.Boolean)
                }));
            nodes.Add(new EffectNodeDefinition(
                discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticCardStateCapabilities.DiscardChosenCardV1,
                inputs: new[]
                {
                    HeroInput(),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticCardStateCapabilities.ColorParameter, color)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticCardStateCapabilities.DiscardedOutput,
                        EffectValueKind.CardInstance)
                }));
        }

        private static EffectNodeDefinition Target() =>
            new EffectNodeDefinition(
                Id("target"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(
                        "activity", RuleActivity.Skill),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                        SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName,
                        EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                        EffectValue.Boolean(false))
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero)
                });

        private static EffectInputBinding HeroInput() =>
            EffectInputBinding.FromNodeOutput(
                SyntheticCardStateCapabilities.HeroParameter,
                Id("target"), SyntheticTargetingCapabilities.ChosenHeroOutputName,
                EffectValueKind.Hero);

        public sealed class SelectRuneHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SelectRuneV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding("color", EffectValueKind.Integer)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var choices = ActiveOrdinals(context);
                if (choices.Length == 0) return EffectExecutionResult.ImpossibleMandatory();
                return choices.Length == 1
                    ? Selected(choices[0])
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, choices.Select(EffectValue.Integer),
                        1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var choices = ActiveOrdinals(context);
                return selected != null && selected.Count == 1 &&
                    selected[0].Kind == EffectValueKind.Integer &&
                    choices.Contains(selected[0].IntegerValue)
                    ? Selected(selected[0].IntegerValue)
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                ActiveOrdinals(context).Length == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : ActiveOrdinals(context).Length == 1
                        ? EffectExecutionResultKind.Applied
                        : EffectExecutionResultKind.NeedsChoice;

            private static int[] ActiveOrdinals(EffectCapabilityContext context)
            {
                if (context?.Components == null || !context.Components.IsLive ||
                    !context.ActorId.IsDefined || context.TurnIndex < 1 ||
                    context.TurnIndex > RoundResolutionAuthority.TurnsPerRound)
                    return Array.Empty<int>();
                return Enumerable.Range(1, SnorriComponentContent.Runes.Length)
                    .Where(ordinal => SnorriComponentContent.IsRuneActive(
                        context.Components.Markers, context.TurnIndex, ordinal)).ToArray();
            }

            private static EffectExecutionResult Selected(int ordinal)
            {
                CardColor color;
                switch (ordinal)
                {
                    // Milestone 9.17a. By the glyphs on Rune Sigils: the winged rune 1 prints gold and
                    // the anvil rune 3 blue on Inscribed Runebomb (the two had been transposed).
                    case 1: color = CardColor.Gold; break;
                    case 2: color = CardColor.Silver; break;
                    case 3: color = CardColor.Blue; break;
                    case 4: color = CardColor.Green; break;
                    default: return EffectExecutionResult.ImpossibleMandatory();
                }
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("color", EffectValue.Integer((int)color))
                });
            }
        }
    }
}
