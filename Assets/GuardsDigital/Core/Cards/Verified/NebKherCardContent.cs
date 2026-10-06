using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// NebKher's verified second-printing fronts, composed and registered at 9.53.
    /// </summary>
    public static partial class NebKherCardContent
    {
        public const string ContentSetId = "nebkher";
        public const string ContentRevision = "nebkher-9-53-rev-1";

        public static ActionDefinitionId PrimaryAttack => new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId PrimarySkill => new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement => new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense => new ActionDefinitionId("secondary-defense");

        public static CardDefinitionId MindGrip => Id("mind-grip");
        public static CardDefinitionId DiabolicalLaughter => Id("diabolical-laughter");
        public static CardDefinitionId PhantasmalSentry => Id("phantasmal-sentry");
        public static CardDefinitionId PhantasmalWarrior => Id("phantasmal-warrior");
        public static CardDefinitionId TwistFate => Id("twist-fate");
        public static CardDefinitionId PhantasmalChampion => Id("phantasmal-champion");
        public static CardDefinitionId DeviousScheme => Id("devious-scheme");
        public static CardDefinitionId ImbueDoubt => Id("imbue-doubt");
        public static CardDefinitionId TimeToReconsider => Id("time-to-reconsider");
        public static CardDefinitionId CrackInReality => Id("crack-in-reality");
        public static CardDefinitionId AnIllusionOfChoice => Id("an-illusion-of-choice");
        public static CardDefinitionId ShiftReality => Id("shift-reality");
        public static CardDefinitionId FleetingImage => Id("fleeting-image");
        public static CardDefinitionId MultipleProjections => Id("multiple-projections");
        public static CardDefinitionId IllusionaryForce => Id("illusionary-force");
        public static CardDefinitionId MasterOfIllusions => Id("master-of-illusions");
        public static CardDefinitionId IllusionaryArmy => Id("illusionary-army");
        public static CardDefinitionId WhatTheHellAreYou => Id("what-the-hell-are-you");
        public static CardDefinitionId MindProbe => Id("mind-probe");

        private static CardDefinitionId Id(string slug) =>
            new CardDefinitionId(ContentSetId + "-" + slug);

        private static ActionDefinition Movement(int value) => new ActionDefinition(
            SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        private static ActionDefinition Defense(int value) => new ActionDefinition(
            SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
    }
}
