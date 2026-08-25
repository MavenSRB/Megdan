using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Brogan's four persistent texts, at milestone 8.21: the protection
    /// Bulwark raises over himself and his allies for a turn, and the three round-long guards Shield,
    /// Bolster and Fortify put over the friendly minions standing near him.
    ///
    /// <para><b>These are declarations, not handlers.</b> All four register through
    /// <see cref="ActiveEffectCreationHandler"/>, the shape milestone 6.8 proved and both earlier
    /// heroes reuse, and everything each card does is stated in its definition. No card name is read
    /// anywhere at runtime and nothing here asks the board a question.</para>
    ///
    /// <para><b>The three guards are the first content to buy a minion out of its removal.</b>
    /// Milestone 8.19c1 built that rule against a synthetic shape and recorded that no configured card
    /// could reach it; these are the printed cards that reach it. What each one declares is a price -
    /// <see cref="RemovalPreventionCost"/> - and the price is content rather than payload, so a
    /// restored pause re-derives which cards could pay from the same declaration the live one read.
    /// The colour each face names is the whole difference between the two Tier I/II cards and the Tier
    /// III one, and it is a real upgrade under the 2026-08-23 ruling recorded as Arien's A-03: "a
    /// silver card" is that colour, while "a basic card" is p.15's supertype and spans Gold and
    /// Silver, so Fortify accepts either of Brogan's two Gold cards as well as his Silver one.</para>
    ///
    /// <para><b>Why three definitions and not one parameterised by radius.</b>
    /// <see cref="ActiveEffectDefinition.UniquenessKey"/> is what keeps one card's occurrence from
    /// suppressing another's, and a hero who plays Shield in one round and Fortify in a later one has
    /// played two cards and is owed two rules - the same argument milestone 8.20 recorded for Expert
    /// and Master Duelist.</para>
    /// </summary>
    public static class BroganActiveEffectContent
    {
        /// <summary>
        /// Rulebook p.17's three cancellations for an Active effect on your card, as both earlier hero
        /// sets declare them: the effect stops applying, the controller is defeated, or the card
        /// changes state at the end of the round. Resolving the source card is deliberately not one of
        /// them - p.17 has you turn the card on its side <em>after</em> performing the action precisely
        /// to signal the effect is still running.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId BulwarkProtection { get; } =
            new EffectCapabilityId("brogan-bulwark-protection");

        public static EffectCapabilityId ShieldFriendlyMinions { get; } =
            new EffectCapabilityId("brogan-shield-friendly-minions");

        public static EffectCapabilityId BolsterFriendlyMinions { get; } =
            new EffectCapabilityId("brogan-bolster-friendly-minions");

        public static EffectCapabilityId FortifyFriendlyMinions { get; } =
            new EffectCapabilityId("brogan-fortify-friendly-minions");

        public static EffectCapabilityReference BulwarkProtectionV1 =>
            new EffectCapabilityReference(BulwarkProtection, 1);

        public static EffectCapabilityReference ShieldFriendlyMinionsV1 =>
            new EffectCapabilityReference(ShieldFriendlyMinions, 1);

        public static EffectCapabilityReference BolsterFriendlyMinionsV1 =>
            new EffectCapabilityReference(BolsterFriendlyMinions, 1);

        public static EffectCapabilityReference FortifyFriendlyMinionsV1 =>
            new EffectCapabilityReference(FortifyFriendlyMinions, 1);

        /// <summary>
        /// Bulwark's Basic Skill: "<b>This turn:</b> You and friendly units in radius cannot be moved,
        /// pushed, swapped or placed by enemy heroes."
        ///
        /// <para><b>Four printed clauses, four declarations, and the first word is one of them.</b>
        /// "You and friendly units in radius" is milestone 8.21's
        /// <see cref="ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius"/>: every scope before
        /// this one named a side its own controller could not be on, so the exclusion p.19 gives
        /// "another" decided nothing, and this face is the first that protects its own hero in so many
        /// words. "moved, pushed, swapped or placed" are four of p.18 and p.19's operations, each of
        /// which <see cref="RuleActivity"/> already names for the reason this card needs them named:
        /// they are things the rules do to an object. And "by enemy heroes" is who is barred, which is
        /// <see cref="RestrictedActorRelation.HeroesOpposingSource"/>.</para>
        ///
        /// <para><b>It is an immunity rather than a restriction.</b> p.18 makes immunity a statement
        /// about what may be done <em>to</em> a unit and a restriction a statement about what an actor
        /// may do; this sentence is the first kind, and reading it as the second would bar an enemy
        /// hero from moving anybody at all while any protected unit stood in the disk.</para>
        ///
        /// <para>Wasp's Magnetic Dagger is the only comparable verified card and it is the opposite
        /// shape in both halves - it declares two activities against enemy units rather than four for
        /// friendly ones - which is why nothing here is shared with it.</para>
        /// </summary>
        public static ActiveEffectDefinition BulwarkDefinition { get; } =
            new ActiveEffectDefinition(
                "brogan-bulwark-effect",
                ActiveEffectAffectedScopeKind.ControllerAndUnitsInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "BroganBulwark",
                "brogan-bulwark",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForImmunity(
                        new[]
                        {
                            RuleActivity.DirectMove,
                            RuleActivity.ObjectPush,
                            RuleActivity.Swap,
                            RuleActivity.Place
                        },
                        RestrictedActorScope.Of(RestrictedActorRelation.HeroesOpposingSource))
                },
                null,
                DeclaredRadius.PrintedIcon(4),
                SourceTeamRelation.AlliedWithSource);

        /// <summary>
        /// Shield's Skill: "<b>This round:</b> When any friendly minion in radius is defeated you may
        /// discard a silver card. If you do, the minion is not removed."
        ///
        /// <para><b>The scope is the disk and the disk is what makes the rule move.</b> p.17 moves an
        /// Active effect's Radius with its hero, so which minions are protected is decided by the board
        /// at the moment a removal stage asks - a minion that walks out stops being protected without
        /// the effect changing. It is
        /// <see cref="ActiveEffectAffectedScopeKind.MinionsInRadius"/> and not units, because the face
        /// prints "minion".</para>
        ///
        /// <para><b>The price is the declaration.</b> Milestone 8.19c1's rule refuses a prevention that
        /// costs nothing - that would be p.18's immunity, which is a different rule - and this face
        /// names both what is spent and from where: one silver card out of the hand. The reminder text
        /// it prints, "( The enemy hero still gains the coins for defeating the minion. )", is p.9's
        /// clarification of what the rule already does: the Defeat and its reward stand, and only the
        /// removal is bought off. The rulebook's own p.12 Bolster example says the same thing.</para>
        /// </summary>
        public static ActiveEffectDefinition ShieldDefinition { get; } =
            Guard("brogan-shield-effect", "BroganShield", "brogan-shield", 1, SilverCard);

        /// <summary>
        /// Bolster's Skill, which is Shield's sentence at twice the radius: "<b>This round:</b> When
        /// any friendly minion in radius is defeated, you may discard a silver card; if you do, the
        /// minion is not removed."
        ///
        /// <para>The two faces differ in punctuation and in the printed Radius icon and in nothing
        /// else, which is why they compose through one builder and differ by one number here. p.16's
        /// Radius item can widen either, because both numbers are on an icon.</para>
        /// </summary>
        public static ActiveEffectDefinition BolsterDefinition { get; } =
            Guard("brogan-bolster-effect", "BroganBolster", "brogan-bolster", 2, SilverCard);

        /// <summary>
        /// Fortify's Skill: "<b>This round:</b> When any friendly minion in radius is defeated you may
        /// discard a basic card. If you do, the minion is not removed."
        ///
        /// <para>Bolster's disk with a wider price. Under the 2026-08-23 ruling, "a basic card" is
        /// p.15's supertype rather than a colour, so this face accepts Onslaught and Leap Forward as
        /// well as Bulwark where the two lower Tiers accept Bulwark alone - and that is the whole of
        /// what the Tier III card buys, which is what makes the three otherwise identical sentences
        /// distinguishable at all.</para>
        /// </summary>
        public static ActiveEffectDefinition FortifyDefinition { get; } =
            Guard("brogan-fortify-effect", "BroganFortify", "brogan-fortify", 2, BasicCard);

        /// <summary>
        /// "you may discard a silver card" - the price Shield and Bolster print, stated once because a
        /// second copy could drift from the first.
        /// </summary>
        private static RemovalPreventionCost SilverCard =>
            RemovalPreventionCost.DiscardOwnedCard(
                CardLocation.Hand, new[] { CardColor.Silver });

        /// <summary>
        /// "you may discard a basic card" - Fortify's price, which names p.15's supertype where its two
        /// siblings name a colour.
        /// </summary>
        private static RemovalPreventionCost BasicCard =>
            RemovalPreventionCost.DiscardOwnedCard(
                CardLocation.Hand, admittedClassification: CardClassification.Basic);

        /// <summary>
        /// One round-long guard over the friendly minions in a printed disk, at the price its card
        /// names. Written once because Brogan's Green colour prints the same rule three times and only
        /// the disk and the price differ.
        /// </summary>
        private static ActiveEffectDefinition Guard(
            string definitionId,
            string effectKind,
            string uniquenessKey,
            int printedRadius,
            RemovalPreventionCost cost) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.MinionsInRadius,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                Cancellations,
                false,
                effectKind,
                uniquenessKey,
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForRemovalPrevention(cost) },
                null,
                DeclaredRadius.PrintedIcon(printedRadius),
                SourceTeamRelation.AlliedWithSource);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(BulwarkProtectionV1, BulwarkDefinition),
            new ActiveEffectCreationHandler(ShieldFriendlyMinionsV1, ShieldDefinition),
            new ActiveEffectCreationHandler(BolsterFriendlyMinionsV1, BolsterDefinition),
            new ActiveEffectCreationHandler(FortifyFriendlyMinionsV1, FortifyDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            BulwarkDefinition,
            ShieldDefinition,
            BolsterDefinition,
            FortifyDefinition
        });
    }
}
