using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string TimeSnareText =
            "An enemy hero in range, who has already resolved a card this turn, discards a card, if able.";

        public const string TimeTrapText = TimeSnareText;

        public const string TimeLoopText =
            "Swap with an enemy hero in range who has already resolved a card this turn.";

        public const string TimeBombText =
            "An enemy hero in range, who has already resolved a card this turn, " +
            "discards a card, or is defeated.";

        public static CardDefinition TimeSnareDefinition { get; } = BlueSkill(
            TimeSnare, "Time Snare", CardTier.I, 8, 3, 2, PrintedValueChannel.None,
            new[] { TimeTrap, TimeLoop }, ResolvedHeroDiscardsIfAble(TimeSnare), TimeSnareText);

        public static CardDefinition TimeTrapDefinition { get; } = BlueSkill(
            TimeTrap, "Time Trap", CardTier.II, 9, 4, 3, PrintedValueChannel.Defense,
            new[] { TimeBomb, TimeWarp }, ResolvedHeroDiscardsIfAble(TimeTrap), TimeTrapText);

        public static CardDefinition TimeLoopDefinition { get; } = BlueSkill(
            TimeLoop, "Time Loop", CardTier.II, 9, 4, 4, PrintedValueChannel.Attack,
            new[] { TimeBomb, TimeWarp }, SwapWithResolvedHero(TimeLoop), TimeLoopText);

        public static CardDefinition TimeBombDefinition { get; } = BlueSkill(
            TimeBomb, "Time Bomb", CardTier.III, 9, 4, 3, PrintedValueChannel.Movement,
            Array.Empty<CardDefinitionId>(), ResolvedHeroDiscardsOrIsDefeated(), TimeBombText);

        private static CardDefinition BlueSkill(CardDefinitionId id, string name, CardTier tier,
            int initiative, int defense, int range, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> upgrades, EffectDefinition effect, string text) =>
            new CardDefinition(id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, upgrades),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, effect),
                    Movement(2), Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition ResolvedHeroDiscardsIfAble(CardDefinitionId card)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var target = new EffectNodeId(card.Value + "-resolved-enemy-in-range");
            var discard = new EffectNodeId(card.Value + "-discard-if-able");
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { target, discard }),
                    new EffectNodeDefinition(target, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ResolvedCardThisTurnParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(discard, EffectNodeKind.Capability,
                        EffectFailurePolicy.IfAble,
                        SyntheticCardStateCapabilities.DiscardChosenCardV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter, target,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        })
                });
        }

        private static EffectDefinition SwapWithResolvedHero(CardDefinitionId card)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var target = new EffectNodeId(card.Value + "-resolved-enemy-in-range");
            var swap = new EffectNodeId(card.Value + "-swap");
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { target, swap }),
                    new EffectNodeDefinition(target, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ResolvedCardThisTurnParameterName,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(swap))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(swap, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapWithActorV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput("subject", target,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                SwapEligiblePieceKind.Unit)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                        })
                });
        }

        private static EffectDefinition ResolvedHeroDiscardsOrIsDefeated()
        {
            var root = new EffectNodeId("time-bomb-root");
            var target = new EffectNodeId("time-bomb-resolved-enemy-in-range");
            var discard = new EffectNodeId("time-bomb-discard");
            var escalation = new EffectNodeId("time-bomb-cannot-discard");
            var defeat = new EffectNodeId("time-bomb-defeat");
            return new EffectDefinition(new EffectDefinitionId(TimeBomb.Value + "-skill"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { target, discard, escalation }),
                    new EffectNodeDefinition(target, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ResolvedCardThisTurnParameterName,
                                EffectValue.Boolean(true))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(discard, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticCardStateCapabilities.DiscardChosenCardV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticCardStateCapabilities.HeroParameter, target,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.DiscardedOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(escalation, EffectNodeKind.Conditional,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { defeat },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput("flag", discard,
                                SyntheticCardStateCapabilities.UnableToDiscardOutput,
                                EffectValueKind.Boolean)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("value", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(defeat, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatHeroV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticConsequenceCapabilities.HeroParameter, target,
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                                EffectValueKind.Hero)
                        })
                });
        }
    }
}
