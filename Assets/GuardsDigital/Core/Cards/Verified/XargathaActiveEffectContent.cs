using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Xargatha's five rulebook p.17 Active effects, at milestone 8.24: the
    /// three petrification disks her Blue line lays down, and the two end-of-round Defeats her Green
    /// Skills leave standing.
    ///
    /// <para><b>These are declarations, not handlers.</b> All five register through
    /// <see cref="ActiveEffectCreationHandler"/> - the one capability shape whose whole content is an
    /// Active-effect definition - and everything each card does is stated in that definition. No card
    /// name is read anywhere at runtime, and nothing here asks the board a question.</para>
    ///
    /// <para><b>Why the petrifications are one sentence carrying two declarations.</b> "Enemy heroes in
    /// radius count as both heroes and terrain, and cannot perform movement actions" states an addition
    /// and a prohibition, and they have to end together because the card ends them together. The
    /// overlay is the half no earlier hero prints: an affected hero keeps every hero rule - still on
    /// their team, still occupying their space as a hero, still taking their turns - and gains one
    /// answer they did not have. That is <see cref="RuleModification.ForEffectiveTypeOverlay"/> rather
    /// than <see cref="RuleModification.ForCountsAs"/>, which p.17 defines as a <em>replacement</em> of
    /// an object's rules and qualities and which would take the hero out of the game instead.</para>
    ///
    /// <para><b>Their disks are the printed icon's, and that is a decision with a consequence.</b> p.9
    /// says "If the card text mentions Range/Radius, it refers to this value", and all three faces say
    /// "in radius" while printing 2, 3 and 4. So p.16's Radius item raises them - which is Lethal Spin,
    /// the card her own Red Tier III pair leaves behind - and p.17 moves them with her, which Stone
    /// Gaze's reminder spells out: "If you move, the radius 'moves' with you."</para>
    ///
    /// <para><b>Why the two end-of-round Skills are Active effects at all.</b> Neither does anything
    /// when it is played. "<b>End of round:</b> Defeat an enemy melee minion adjacent to you" is a
    /// sentence about a moment that has not arrived, so what the Skill does is leave a source standing
    /// until that moment - and the moment is p.8's, before the Minion Battle, which Constrict's
    /// reminder names outright and which p.9 then gives Final Embrace too, reminder text being
    /// clarification rather than a new rule.</para>
    /// </summary>
    public static class XargathaActiveEffectContent
    {
        /// <summary>
        /// The revision every declaration here carries. It is <see cref="XargathaCardContent"/>'s own
        /// constant, spelled as a const so it is resolved at compile time - a property read would make
        /// this file's static initializer depend on that one's, which builds its definitions by asking
        /// this file for these.
        /// </summary>
        private const string ContentRevision = XargathaCardContent.ContentRevision;

        /// <summary>
        /// p.17's cancellations for an Active effect on your own card, as every hero's content has
        /// declared them since milestone 6.8: the controller is defeated, or the cards are retrieved at
        /// the end of the round. Resolving the source card is deliberately not one of them - p.17 has
        /// you turn the card on its side <em>after</em> performing the action precisely to signal the
        /// effect is still running, which is what makes "Next turn" reachable at all.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId StoneGaze { get; } =
            new EffectCapabilityId("xargatha-stone-gaze");

        public static EffectCapabilityId PetrifyingStare { get; } =
            new EffectCapabilityId("xargatha-petrifying-stare");

        public static EffectCapabilityId TurnIntoStatues { get; } =
            new EffectCapabilityId("xargatha-turn-into-statues");

        public static EffectCapabilityId Constrict { get; } =
            new EffectCapabilityId("xargatha-constrict");

        public static EffectCapabilityId FinalEmbrace { get; } =
            new EffectCapabilityId("xargatha-final-embrace");

        public static EffectCapabilityReference StoneGazeV1 =>
            new EffectCapabilityReference(StoneGaze, 1);

        public static EffectCapabilityReference PetrifyingStareV1 =>
            new EffectCapabilityReference(PetrifyingStare, 1);

        public static EffectCapabilityReference TurnIntoStatuesV1 =>
            new EffectCapabilityReference(TurnIntoStatues, 1);

        public static EffectCapabilityReference ConstrictV1 =>
            new EffectCapabilityReference(Constrict, 1);

        public static EffectCapabilityReference FinalEmbraceV1 =>
            new EffectCapabilityReference(FinalEmbrace, 1);

        /// <summary>
        /// "<b>Next turn:</b> Enemy heroes in radius count as both heroes and terrain, and cannot
        /// perform movement actions." - Stone Gaze at Radius 2.
        ///
        /// <para>p.19's <c>Next Turn</c> decides the two checkpoints: the effect is created the moment
        /// the Skill resolves and becomes applicable at Xargatha's next <c>TurnStarted</c>, so a hero
        /// who moves later in the same round is not stopped - which is the whole difference between
        /// this keyword and <c>This turn</c>. p.8's end-of-round card retrieval cancels it if that turn
        /// never comes.</para>
        ///
        /// <para><b>What the prohibition prohibits is starting the action.</b> p.11 checks availability
        /// when a Movement action would begin, so a petrified hero cannot start one at all - not even
        /// to apply a primary Movement's card text while moving zero spaces. It says nothing about
        /// movement written inside another card's text, and p.19's Swap is not movement, so a petrified
        /// hero may still be swapped and may still be pushed.</para>
        /// </summary>
        public static ActiveEffectDefinition StoneGazeDefinition { get; } =
            PetrificationDefinition("xargatha-stone-gaze-effect", "XargathaStoneGaze", 2);

        /// <summary>The identical sentence at Radius 3 - Petrifying Stare.</summary>
        public static ActiveEffectDefinition PetrifyingStareDefinition { get; } =
            PetrificationDefinition("xargatha-petrifying-stare-effect", "XargathaPetrifyingStare", 3);

        /// <summary>The identical sentence at Radius 4 - Turn Into Statues.</summary>
        public static ActiveEffectDefinition TurnIntoStatuesDefinition { get; } =
            PetrificationDefinition("xargatha-turn-into-statues-effect", "XargathaTurnIntoStatues", 4);

        /// <summary>
        /// "<b>End of round:</b> Defeat an enemy melee minion adjacent to you." - Constrict.
        ///
        /// <para><b>The duration is This Round and the window is inside it.</b> p.17 expires a This
        /// Round effect at Round End, and p.8 puts Round End after the Minion Battle, so the effect is
        /// still standing when its own moment - the one before that battle - arrives. A Next Turn
        /// duration would have been a different card: this one never reaches another turn.</para>
        ///
        /// <para>The scope is the controller because the sentence measures from her - "adjacent to
        /// you" - and the effect's own trigger is what asks the board who that is when the moment
        /// comes, rather than a set recorded when the Skill resolved.</para>
        /// </summary>
        public static ActiveEffectDefinition ConstrictDefinition { get; } = new ActiveEffectDefinition(
            "xargatha-constrict-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisRound,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.RoundEnd,
            Cancellations,
            false,
            "XargathaConstrict",
            "xargatha-constrict",
            new[] { TimingCheckpoint.EndOfRoundBeforeMinionBattle },
            triggeredEffect: ConstrictTriggeredEffect.Reference);

        /// <summary>
        /// "<b>End of round:</b> Defeat an enemy melee or ranged minion adjacent to you." - Final
        /// Embrace, whose face omits Constrict's parenthesis and has the same timing anyway, because
        /// p.9 makes reminder text clarification rather than a rule the other card owns.
        /// </summary>
        public static ActiveEffectDefinition FinalEmbraceDefinition { get; } =
            new ActiveEffectDefinition(
                "xargatha-final-embrace-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                Cancellations,
                false,
                "XargathaFinalEmbrace",
                "xargatha-final-embrace",
                new[] { TimingCheckpoint.EndOfRoundBeforeMinionBattle },
                triggeredEffect: FinalEmbraceTriggeredEffect.Reference);

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            StoneGazeDefinition,
            PetrifyingStareDefinition,
            TurnIntoStatuesDefinition,
            ConstrictDefinition,
            FinalEmbraceDefinition
        });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(StoneGazeV1, StoneGazeDefinition),
            new ActiveEffectCreationHandler(PetrifyingStareV1, PetrifyingStareDefinition),
            new ActiveEffectCreationHandler(TurnIntoStatuesV1, TurnIntoStatuesDefinition),
            new ActiveEffectCreationHandler(ConstrictV1, ConstrictDefinition),
            new ActiveEffectCreationHandler(FinalEmbraceV1, FinalEmbraceDefinition)
        };

        /// <summary>
        /// The one petrification sentence at the radius the face prints. Three definitions rather than
        /// one, because each card's disk is its own printed value and p.16 raises each separately; one
        /// factory, because the sentence is identical on all three and a reader comparing them should
        /// find nothing to compare.
        /// </summary>
        private static ActiveEffectDefinition PetrificationDefinition(
            string definitionId, string effectKind, int printedRadius) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.NextTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                true,
                effectKind,
                definitionId,
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForEffectiveTypeOverlay(EffectiveUnitKind.Terrain),
                    RuleModification.ForRestriction(RuleActivity.Movement)
                },
                null,
                DeclaredRadius.PrintedIcon(printedRadius),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// What Constrict runs when p.8's pre-battle moment arrives: one adjacent enemy melee minion,
        /// defeated.
        ///
        /// <para>It lives here rather than in <see cref="XargathaEffectContent"/> for the reason
        /// <c>SyntheticPausingSourceCapabilities</c> keeps its own: the definition above needs this
        /// reference to exist while it is being constructed, and a static reference into the file that
        /// composes her card text would make two static initializers wait on each other.</para>
        ///
        /// <para><b>The step is mandatory and the selection is what may be empty.</b> The card says
        /// "Defeat", not "you may defeat", so a round that ends with a qualifying minion beside her
        /// defeats one; a round that ends with none is a mandatory step nobody can perform, which p.10
        /// completes by stopping rather than by asking. That is the difference this pair draws against
        /// every "you may" in her set.</para>
        /// </summary>
        internal static class ConstrictTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                EndOfRoundDefeat("xargatha-constrict-defeat", RosterMinionClass.Melee);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>What Final Embrace runs at the same moment, admitting ranged minions as well.</summary>
        internal static class FinalEmbraceTriggeredEffect
        {
            public static EffectDefinition Definition { get; } =
                EndOfRoundDefeat("xargatha-final-embrace-defeat", RosterMinionClass.RangedOrMelee);

            public static EffectDefinitionReference Reference => Definition.Reference;
        }

        /// <summary>
        /// "Defeat an enemy [class] minion adjacent to you", as nodes: choose one, then defeat it.
        ///
        /// <para>The reach is one space written in the card's own text - neither face prints a Radius
        /// icon - so p.16's Radius item does not widen it, which is exactly what that page says about
        /// values in card text.</para>
        /// </summary>
        private static EffectDefinition EndOfRoundDefeat(
            string effectId, RosterMinionClass admittedClass)
        {
            var root = new EffectNodeId($"{effectId}-root");
            var subject = new EffectNodeId($"{effectId}-subject");
            var defeat = new EffectNodeId($"{effectId}-defeat");

            return new EffectDefinition(
                new EffectDefinitionId(effectId),
                ContentRevision,
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { subject, defeat }),
                    new EffectNodeDefinition(
                        subject, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.OriginParameter,
                                EffectValue.Integer((int)RosterQueryOrigin.Actor)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                EffectValue.Integer((int)RosterUnitKind.Minion)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.RelationParameter,
                                EffectValue.Integer((int)SourceTeamRelation.OpposingSource)),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.AdmittedMinionClassParameter,
                                EffectValue.Integer((int)admittedClass))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(
                        defeat, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatMinionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticConsequenceCapabilities.MinionParameter,
                                subject,
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                                EffectValueKind.Minion)
                        })
                });
        }
    }
}
