using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The Poison marker, and the two capabilities that give it. Milestone 8.23, and the first
    /// hero-owned rules component in this project.
    ///
    /// <para><b>What the source says.</b> Ledger question 14 makes a hero's card faces the definition
    /// of that hero's tokens and markers, and Tigerclaw is the roster's one positive instance: Poisoned
    /// Dagger prints "Give a hero in range a [Poison] Poison marker. The hero with a poison marker has
    /// -1 Initiative, -1 Attack and -1 Defense", and Poisoned Dart prints the same sentence at -2. The
    /// extraction independently contains the matching readable <c>PoisonMarker</c> component, whose
    /// face is a bottle and skull labelled <c>POISON</c>. The values are the cards'; the component is
    /// the physical piece.</para>
    ///
    /// <para><b>Two definitions, one physical supply, and why that is not a contradiction.</b> The two
    /// faces print one marker at two strengths, so the strengths are two definitions - a marker on a
    /// dashboard has one strength and the card that gave it decides which. They cannot coexist from
    /// Tigerclaw's own deck: p.15's Level Up happens after p.8 has returned every marker, so the Tier
    /// II card and the Tier III card are never both able to have one out. Each therefore declares the
    /// same supply count, which is the one physical stack, rather than the supply being split.</para>
    ///
    /// <para><b>Why the rule is on the marker rather than on the card.</b> The sentence is printed in
    /// the present tense about the holder - "The hero <em>with</em> a poison marker has" - so it is
    /// true while the marker is held and stops when it is returned, which is p.8's end of round and
    /// p.13's holder Defeat. That is a marker property, and milestone 8.23's
    /// <see cref="MarkerPropertyDefinition"/> is the family that carries one. Nothing about the card
    /// that gave it is consulted afterwards.</para>
    /// </summary>
    public static class TigerclawComponentContent
    {
        /// <summary>
        /// The supply both strengths declare. Rulebook p.18 makes a marker supply finite and the
        /// extraction shows one physical stack; six is the count this build gives it, which is enough
        /// for every hero on the board to hold one and for the p.20 odd-player configuration besides.
        /// </summary>
        public const int PoisonSupplyCount = 6;

        public static MarkerDefinitionId PoisonOne { get; } = new MarkerDefinitionId("poison-1");

        public static MarkerDefinitionId PoisonTwo { get; } = new MarkerDefinitionId("poison-2");

        /// <summary>Poisoned Dagger's marker: -1 Initiative, -1 Attack and -1 Defense on its holder.</summary>
        public static MarkerDefinition PoisonOneDefinition { get; } = Poison(PoisonOne, "Poison", 1);

        /// <summary>Poisoned Dart's marker: the same sentence at -2.</summary>
        public static MarkerDefinition PoisonTwoDefinition { get; } = Poison(PoisonTwo, "Poison", 2);

        public static EffectCapabilityId GivePoisonOne { get; } =
            new EffectCapabilityId("tigerclaw-give-poison-marker-1");

        public static EffectCapabilityId GivePoisonTwo { get; } =
            new EffectCapabilityId("tigerclaw-give-poison-marker-2");

        public static EffectCapabilityReference GivePoisonOneV1 =>
            new EffectCapabilityReference(GivePoisonOne, 1);

        public static EffectCapabilityReference GivePoisonTwoV1 =>
            new EffectCapabilityReference(GivePoisonTwo, 1);

        /// <summary>Both marker definitions, for the catalog this build registers.</summary>
        public static IEnumerable<MarkerDefinition> Markers() => new[]
        {
            PoisonOneDefinition, PoisonTwoDefinition
        };

        /// <summary>
        /// The two capabilities that give them.
        ///
        /// <para>One capability per marker, exactly as milestone 6.8's Active-effect content declares
        /// one per effect definition and for the same reason: the definition is bound to the handler
        /// rather than named on the wire, so a payload can name a node but never a different marker,
        /// and the registered list a snapshot pins says which markers this build can actually put on a
        /// dashboard.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new SyntheticComponentCapabilities.MarkerGivingHandler(
                GivePoisonOneV1, PoisonOneDefinition),
            new SyntheticComponentCapabilities.MarkerGivingHandler(
                GivePoisonTwoV1, PoisonTwoDefinition)
        };

        /// <summary>
        /// One strength of the printed marker: the three values its card names, each as its own
        /// modification because one <see cref="RuleModification"/> names one channel.
        /// </summary>
        private static MarkerDefinition Poison(MarkerDefinitionId id, string displayName, int strength) =>
            new MarkerDefinition(
                id,
                1,
                displayName,
                PoisonSupplyCount,
                // p.8 returns every marker at end of round and p.13 returns a defeated hero's. The two
                // are one condition in this vocabulary and neither card prints an earlier one.
                MarkerReturnCondition.GeneralCleanup,
                new[]
                {
                    Property("initiative", PrintedValueChannel.Initiative, strength),
                    Property("attack", PrintedValueChannel.Attack, strength),
                    Property("defense", PrintedValueChannel.Defense, strength)
                });

        private static MarkerPropertyDefinition Property(
            string propertyId, PrintedValueChannel channel, int strength) =>
            new MarkerPropertyDefinition(
                propertyId,
                "value-modifier",
                "while-held",
                RuleModification.ForValue(channel, -strength));

    }
}
