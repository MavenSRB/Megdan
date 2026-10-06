using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Verified Emmitt fronts. The set is not dealt until every printed effect is composed.</summary>
    public static partial class EmmittCardContent
    {
        public const string ContentSetId = "emmitt";
        public const string ContentRevision = "emmitt-9-49-rev-1";

        public static ActionDefinitionId PrimaryAttack => new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId PrimarySkill => new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId PrimaryDefense => new ActionDefinitionId("primary-defense");
        public static ActionDefinitionId SecondaryMovement => new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense => new ActionDefinitionId("secondary-defense");

        public static CardDefinitionId ReverseTime => Id("reverse-time");
        public static CardDefinitionId UnstableTimeline => Id("unstable-timeline");
        public static CardDefinitionId TemporalPunch => Id("temporal-punch");
        public static CardDefinitionId TemporalSlam => Id("temporal-slam");
        public static CardDefinitionId Flashback => Id("flashback");
        public static CardDefinitionId TemporalJudgement => Id("temporal-judgement");
        public static CardDefinitionId DejaVu => Id("deja-vu");
        public static CardDefinitionId TimeSnare => Id("time-snare");
        public static CardDefinitionId TimeTrap => Id("time-trap");
        public static CardDefinitionId TimeLoop => Id("time-loop");
        public static CardDefinitionId TimeBomb => Id("time-bomb");
        public static CardDefinitionId TimeWarp => Id("time-warp");
        public static CardDefinitionId TimeWalk => Id("time-walk");
        public static CardDefinitionId FastForward => Id("fast-forward");
        public static CardDefinitionId TimeCapsule => Id("time-capsule");
        public static CardDefinitionId BackToTheFuture => Id("back-to-the-future");
        public static CardDefinitionId FutureProof => Id("future-proof");
        public static CardDefinitionId AlternativeTimelines => Id("alternative-timelines");
        public static CardDefinitionId TearInTime => Id("tear-in-time");

        internal static CardDefinitionId Id(string slug) => new CardDefinitionId(ContentSetId + "-" + slug);

        internal static ActionDefinition Movement(int value) => new ActionDefinition(
            SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        internal static ActionDefinition Defense(int value) => new ActionDefinition(
            SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        private static EffectDefinition GrantEffect(CardDefinitionId card,
            EffectCapabilityReference capability)
        {
            var root = new EffectNodeId(card.Value + "-text-root");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-text"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, capability,
                        outputs: new[]
                        {
                            new EffectOutputBinding(ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        })
                });
        }

        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            foreach (var card in new[] { Flashback, DejaVu })
            {
                yield return DecisionLabelDeclaration.DecisionStep(card,
                    card.Value + "-may-place-glitches",
                    "Place Glitch tokens after the attack", "Do not place Glitch tokens");
                yield return DecisionLabelDeclaration.DecisionStep(card,
                    card.Value + "-up-to-one-enemy",
                    "Choose an enemy hero to swap with a Glitch", "Choose no enemy hero");
            }

            yield return DecisionLabelDeclaration.DecisionArm(TimeWarp, "time-warp-swap-arm",
                "Swap with an enemy hero who resolved a card this turn");
            yield return DecisionLabelDeclaration.DecisionArm(TimeWarp, "time-warp-card-arm",
                "Have an enemy hero swap their unresolved and resolved cards");

            foreach (var card in new[] { TimeCapsule, FutureProof })
            {
                yield return DecisionLabelDeclaration.DecisionStep(card,
                    "time-capsule-self-may",
                    "Retrieve your cards discarded this turn", "Leave your discarded cards");
                yield return DecisionLabelDeclaration.DecisionStep(card,
                    "time-capsule-first-may",
                    "Retrieve this friendly hero's cards discarded this turn",
                    "Leave this friendly hero's discarded cards");
                yield return DecisionLabelDeclaration.DecisionStep(card,
                    "time-capsule-second-may",
                    "Retrieve this friendly hero's cards discarded this turn",
                    "Leave this friendly hero's discarded cards");
            }

            yield return DecisionLabelDeclaration.DecisionArm(BackToTheFuture,
                "back-to-the-future-place-arm",
                "Place a unit into its space at the start of this turn");
            yield return DecisionLabelDeclaration.DecisionArm(BackToTheFuture,
                BackToTheFuture.Value + "-root",
                "Move a stationary enemy hero two spaces in a straight line");

            yield return DecisionLabelDeclaration.DecisionArm(FutureProof,
                "time-capsule-root",
                "You and nearby allies may retrieve cards discarded this turn");
            yield return DecisionLabelDeclaration.DecisionArm(FutureProof,
                FutureProof.Value + "-text-root",
                "Nearby friendly heroes are immune to enemy actions this turn");
        }
    }
}
