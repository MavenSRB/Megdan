using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30f. Gydion's immunity, discard, Life and forced-move spells - Shield,
    /// Invulnerability, Cloud Kill, Energy Drain and Suggestion. Each is performed by the hero casting
    /// it through 9.30c's borrow path, with the spell's own values and the caster's items; the
    /// spell's own classification is what an Attack against him is measured by (GY-Q3).
    ///
    /// <para>Four engine contracts are new and each is shared vocabulary, not a Gydion branch:
    /// immunity scoped by the attacking card's p.9 classification
    /// (<see cref="RuleActivity.BasicAttack"/> / <see cref="RuleActivity.NonBasicAttack"/>); a forced
    /// discard narrowed by classification
    /// (<see cref="SyntheticCardStateCapabilities.ClassificationParameter"/>); a spent Life counter
    /// regained (<see cref="SyntheticTeamLifeCapabilities.RegainSpentLifeV1"/>); and a straight line
    /// whose end another hero's seat picks
    /// (<see cref="SyntheticRosterCapabilities.LineChooserParameter"/>).</para>
    ///
    /// <para><b>Registered with the dealing (9.30i)</b>: <see cref="ImmunityDiscardLifeMoveHandlers"/>
    /// and <see cref="SyntheticTeamLifeCapabilities.RegisteredHandlers"/>.</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        /// <summary>Energy Drain: "Your team regains 1 spent Life counter."</summary>
        public const int EnergyDrainLivesRegained = 1;

        /// <summary>Suggestion: "moves 3 spaces in a straight line" - exactly three (GY-Q6).</summary>
        public const int SuggestionSpaces = 3;

        /// <summary>Shield's "This round: You are immune to basic attacks" - its capability.</summary>
        public static EffectCapabilityReference ShieldImmunityV1 =>
            new EffectCapabilityReference(new EffectCapabilityId(Shield.Value + "-immunity"), 1);

        /// <summary>Invulnerability's "This Round: You are immune to non-basic attacks".</summary>
        public static EffectCapabilityReference InvulnerabilityImmunityV1 =>
            new EffectCapabilityReference(new EffectCapabilityId(Invulnerability.Value + "-immunity"), 1);

        /// <summary>
        /// Shield's Active effect: "<b>This round:</b> You are immune to basic attacks."
        ///
        /// <para><b>Immune to an Attack from a basic card</b> - p.9's classification of the ATTACKING
        /// card's definition, read where the Attack's targets are found
        /// (<see cref="RuleActivityQuery.ForAttack"/>). From any actor: the face names none, and p.18's
        /// "never immune to your own actions" is a rule applied before any scope.</para>
        ///
        /// <para><b>Its source is the SPELL</b>, the card the borrow performs (9.30c stamps the cast's
        /// Active effect with the spell's instance), so returning the spell to the book - Prepare
        /// Spells, p.17's "card changes state - becomes facedown" (GY-05 designer) - cancels it. It also
        /// ends at the end of the round (p.19's "This round") and when Gydion is defeated (p.17).</para>
        /// </summary>
        public static ActiveEffectDefinition ShieldDefinition => LazyShieldDefinition.Value;

        /// <summary>
        /// Invulnerability's Active effect: "<b>This Round:</b> You are immune to non-basic attacks."
        /// <see cref="ShieldDefinition"/>'s complement - an Attack from a Tier card or an Ultimate.
        /// </summary>
        public static ActiveEffectDefinition InvulnerabilityDefinition => LazyInvulnerabilityDefinition.Value;

        // Lazy, because the spell ids are another partial's static initializers and C# orders those
        // across files by no rule this file can rely on.
        private static readonly Lazy<ActiveEffectDefinition> LazyShieldDefinition =
            new Lazy<ActiveEffectDefinition>(() => SpellImmunity(Shield, RuleActivity.BasicAttack));

        private static readonly Lazy<ActiveEffectDefinition> LazyInvulnerabilityDefinition =
            new Lazy<ActiveEffectDefinition>(() => SpellImmunity(Invulnerability, RuleActivity.NonBasicAttack));

        /// <summary>
        /// The capabilities this file's spells name that are theirs alone: the two immunities'
        /// Active-effect creations, registered with the dealing (9.30i) beside
        /// <see cref="SyntheticTeamLifeCapabilities.RegisteredHandlers"/> (Energy Drain's regain).
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> ImmunityDiscardLifeMoveHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActiveEffectCreationHandler(ShieldImmunityV1, ShieldDefinition),
                new ActiveEffectCreationHandler(InvulnerabilityImmunityV1, InvulnerabilityDefinition)
            };

        /// <summary>
        /// What these five spells' decision controls say, DERIVED from the content: the controls are
        /// exactly <see cref="DecisionLabelRequirementQuery"/>'s - a <c>Choose</c> arm or a
        /// confirm-or-decline step - and none of the five prints either. A hero, a card and a space
        /// are asked as candidates, which carry their own names. Empty, and asserted to stay so against
        /// the query; 9.30i aggregates it with the others.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> ImmunityDiscardLifeMoveDecisionLabels() =>
            Array.Empty<DecisionLabelDeclaration>();

        /// <summary>The five spells this file composes, in book order.</summary>
        public static IReadOnlyList<CardDefinitionId> ImmunityDiscardLifeMoveSpells => new[]
        {
            EnergyDrain, Suggestion, Shield, Invulnerability, CloudKill
        };

        /// <summary>Shield (Green I, Skill, no icon).</summary>
        private static ActionDefinition ShieldPrimary() =>
            SelfImmunityPrimary(Shield, ShieldImmunityV1);

        /// <summary>Invulnerability (Green III, Skill, no icon).</summary>
        private static ActionDefinition InvulnerabilityPrimary() =>
            SelfImmunityPrimary(Invulnerability, InvulnerabilityImmunityV1);

        /// <summary>
        /// Cloud Kill (Green III, Skill, Radius 3): "An enemy hero in radius discards a basic card, if
        /// able."
        ///
        /// <para>Gydion picks an enemy hero in radius - the printed Radius with his items (R1) - and
        /// THAT hero picks which basic card to discard, as every enemy discard is the discarding hero's
        /// choice (<c>synthetic-discard-chosen-card@1</c> names the hero as its chooser). "If able"
        /// governs the sentence (an <c>IfAble</c> Optional): when no enemy hero in radius could discard
        /// a basic card - none stands there, or none holds one - nothing is asked and nothing happens.
        /// Otherwise a hero holding no basic card is still a legal choice who gives nothing (Rowenna's
        /// Opening Shots and Ignatia's sun arm read it the same way, so the selection declares no
        /// consumer).</para>
        /// </summary>
        private static ActionDefinition CloudKillPrimary()
        {
            var root = new EffectNodeId("cloud-kill-root");
            var sentence = new EffectNodeId("cloud-kill-sentence");
            var select = new EffectNodeId("cloud-kill-enemy-hero");
            var discard = new EffectNodeId("cloud-kill-discard");
            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(
                    new EffectDefinitionId(CloudKill.Value + "-skill"), ContentRevision, root,
                    new[]
                    {
                        new EffectNodeDefinition(root, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                            childNodeIds: new[] { sentence }),
                        new EffectNodeDefinition(sentence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { select, discard }),
                        EnemyHeroInPrintedReach(select, SyntheticTargetingCapabilities.RadiusHeroTargetV1),
                        ClassifiedDiscard(discard, select, CardClassification.Basic)
                    }));
        }

        /// <summary>
        /// Energy Drain (Red III, Skill - Ranged, Range 3): "An enemy hero in range discards a non-basic
        /// card, if able. Your team regains 1 spent [Life] Life counter."
        ///
        /// <para><b>Two sentences, two steps.</b> The first is Cloud Kill's shape on the printed Range,
        /// narrowed to a non-basic card. The second is independent of it (GY-Q11, user: "whether or not
        /// the enemy hero discards"): his team regains one SPENT counter, so only when it has spent one
        /// and never above its starting Lives - a full team regains nothing. Both are <c>IfAble</c>, so
        /// the regain still happens with no enemy hero in range.</para>
        /// </summary>
        private static ActionDefinition EnergyDrainPrimary()
        {
            var root = new EffectNodeId("energy-drain-root");
            var drain = new EffectNodeId("energy-drain-if-able");
            var sentence = new EffectNodeId("energy-drain-discard-sentence");
            var select = new EffectNodeId("energy-drain-enemy-hero");
            var discard = new EffectNodeId("energy-drain-discard");
            var regain = new EffectNodeId("energy-drain-regain-life");
            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Range, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(
                    new EffectDefinitionId(EnergyDrain.Value + "-skill"), ContentRevision, root,
                    new[]
                    {
                        new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { drain, regain }),
                        new EffectNodeDefinition(drain, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                            childNodeIds: new[] { sentence }),
                        new EffectNodeDefinition(sentence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { select, discard }),
                        EnemyHeroInPrintedReach(select, SyntheticTargetingCapabilities.RangeHeroTargetV1),
                        ClassifiedDiscard(discard, select, CardClassification.NonBasic),
                        new EffectNodeDefinition(regain, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                            SyntheticTeamLifeCapabilities.RegainSpentLifeV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromLiteral(
                                    SyntheticTeamLifeCapabilities.AmountParameter,
                                    EffectValue.Integer(EnergyDrainLivesRegained))
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticTeamLifeCapabilities.RegainedOutput, EffectValueKind.Integer)
                            })
                    }));
        }

        /// <summary>
        /// Suggestion (Blue I, Skill, Radius 3): "If able, an enemy hero in radius moves 3 spaces in a
        /// straight line." GY-Q6 (user, 2026-10-01), Whisper Rulings 16/16a.
        ///
        /// <list type="bullet">
        /// <item><description><b>Gydion picks the hero</b> - an enemy hero in the printed Radius with his
        /// items - and only a hero the line can move is offered (6.8l: the selection's consumer is the
        /// line, Ignatia's Searing Heat shape).</description></item>
        /// <item><description><b>That hero's seat picks the direction</b>: the line's
        /// <see cref="SyntheticRosterCapabilities.LineChooserParameter"/> is bound to the chosen
        /// hero.</description></item>
        /// <item><description><b>Exactly 3 or not at all</b> (Ruling 16a): the line's floor and ceiling
        /// are both 3, so a direction an obstacle or the edge stops short is not offered.</description></item>
        /// <item><description><b>A plain move, not a Movement action</b>: the card does not say
        /// "movement action", so it is <c>synthetic-direct-move@3</c> pinned to the space the line
        /// named - p.18's obstacles still stop it, and no Movement-action restriction, raise or trigger
        /// reads it.</description></item>
        /// <item><description><b>"If able"</b> governs the sentence: with no such hero nothing
        /// happens.</description></item>
        /// </list>
        /// </summary>
        private static ActionDefinition SuggestionPrimary()
        {
            var root = new EffectNodeId("suggestion-root");
            var sentence = new EffectNodeId("suggestion-sentence");
            var select = new EffectNodeId("suggestion-enemy-hero");
            var line = new EffectNodeId("suggestion-direction");
            var move = new EffectNodeId("suggestion-move");
            const string destination = "destination";
            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(
                    new EffectDefinitionId(Suggestion.Value + "-skill"), ContentRevision, root,
                    new[]
                    {
                        new EffectNodeDefinition(root, EffectNodeKind.Optional, EffectFailurePolicy.IfAble,
                            childNodeIds: new[] { sentence }),
                        new EffectNodeDefinition(sentence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { select, line, move }),
                        new EffectNodeDefinition(select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                                EffectInputBinding.FromClosedVocabulary(
                                    SyntheticTargetingCapabilities.TargetRelationParameterName,
                                    SourceTeamRelation.OpposingSource),
                                EffectInputBinding.FromLiteral(
                                    SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                    EffectValue.Boolean(false)),
                                EffectInputBinding.FromLiteral(
                                    SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                    EffectValue.Boolean(false)),
                                EffectInputBinding.FromLiteral(
                                    EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(line))
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                                new EffectOutputBinding(
                                    SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                    EffectValueKind.Coordinate)
                            }),
                        new EffectNodeDefinition(line, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticRosterCapabilities.ChooseLineDestinationV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromLiteral(
                                    "maximumDistance", EffectValue.Integer(SuggestionSpaces)),
                                EffectInputBinding.FromLiteral(
                                    "minimumDistance", EffectValue.Integer(SuggestionSpaces)),
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticRosterCapabilities.LineSubjectSpaceParameter, select,
                                    SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                    EffectValueKind.Coordinate),
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticRosterCapabilities.LineChooserParameter, select,
                                    SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                            },
                            outputs: new[] { new EffectOutputBinding(destination, EffectValueKind.Coordinate) }),
                        new EffectNodeDefinition(move, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticDisplacementCapabilities.DirectMoveV3,
                            inputs: new[]
                            {
                                EffectInputBinding.FromNodeOutput(
                                    "mover", select, SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                    EffectValueKind.Coordinate),
                                EffectInputBinding.FromLiteral("distance", EffectValue.Integer(SuggestionSpaces)),
                                EffectInputBinding.FromLiteral(
                                    SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                    EffectValue.Integer(SuggestionSpaces)),
                                EffectInputBinding.FromClosedVocabulary(
                                    SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                    DirectMoveDestinationPolicy.NamedSpace),
                                EffectInputBinding.FromNodeOutput(
                                    SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                    line, destination, EffectValueKind.Coordinate)
                            },
                            outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) })
                    }));
        }

        /// <summary>"You are immune to ..." - one node creating the spell's Active effect on the caster.</summary>
        private static ActionDefinition SelfImmunityPrimary(
            CardDefinitionId spell, EffectCapabilityReference immunity)
        {
            var root = new EffectNodeId(spell.Value.Substring(ContentSetId.Length + 1) + "-immunity");
            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(
                    new EffectDefinitionId(spell.Value + "-skill"), ContentRevision, root,
                    new[]
                    {
                        new EffectNodeDefinition(root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            immunity,
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                            })
                    }));
        }

        /// <summary>
        /// p.19's "This round" on the controller, with p.17's cancellations: the source card changing
        /// state (the spell returned to the book), the controller defeated, and the end-of-round
        /// retrieval; "at the end of the round" is the duration's own expiry.
        /// </summary>
        private static ActiveEffectDefinition SpellImmunity(CardDefinitionId spell, RuleActivity attacks) =>
            new ActiveEffectDefinition(
                spell.Value + "-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                new[]
                {
                    ActiveEffectCancellationCondition.SourceCardStateChanged,
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved
                },
                false,
                "GydionSpellImmunity",
                spell.Value + "-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForImmunity(new[] { attacks }, RestrictedActorScope.AnyActor)
                });

        /// <summary>"An enemy hero in radius/range", picked by the caster, on the spell's printed icon.</summary>
        private static EffectNodeDefinition EnemyHeroInPrintedReach(
            EffectNodeId select, EffectCapabilityReference printedIconHeroTarget)
        {
            var radius = printedIconHeroTarget.Equals(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            var inputs = new List<EffectInputBinding>
            {
                // p.12: a Skill's effect is the action, so the immunity question is the Skill's own.
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticTargetingCapabilities.TargetRelationParameterName, SourceTeamRelation.OpposingSource)
            };
            inputs.AddRange(radius
                ? new[]
                {
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName, EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.IncludeSelfParameterName, EffectValue.Boolean(false))
                }
                : new[]
                {
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName, EffectValue.Boolean(false))
                });
            return new EffectNodeDefinition(select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                printedIconHeroTarget,
                inputs: inputs,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                });
        }

        /// <summary>"... discards a basic / non-basic card, if able" - the discarding hero chooses which.</summary>
        private static EffectNodeDefinition ClassifiedDiscard(
            EffectNodeId discard, EffectNodeId select, CardClassification classification) =>
            new EffectNodeDefinition(discard, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                SyntheticCardStateCapabilities.DiscardChosenCardV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticCardStateCapabilities.HeroParameter, select,
                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticCardStateCapabilities.ClassificationParameter, classification)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticCardStateCapabilities.DiscardedOutput, EffectValueKind.CardInstance)
                });
    }
}
