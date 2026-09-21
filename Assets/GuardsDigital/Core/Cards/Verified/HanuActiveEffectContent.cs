using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The three Active effects Hanu the Trickster's Blue journeys create - Unexpected Journey, There
    /// and Back Again and Safe Travels. Milestone 8.71d.
    ///
    /// <para><b>The printed sentence, whole.</b> "Swap with an enemy hero in radius. <b>This turn:</b>
    /// That hero is immune. <b>End of turn:</b> Swap with that hero regardless of radius and
    /// immunity." Two rulebook p.17 keywords and one effect between them, which is the whole of this
    /// file's argument: an <see cref="ActiveEffectDurationKind.EndOfTurn"/> effect stands from the
    /// moment the action creates it until the turn's close, and p.17's own words say a This-turn
    /// clause "comes into effect immediately" and lasts exactly that long. So the two keywords name
    /// ONE window, and one definition carries the immunity while it stands AND names the body that
    /// runs when it fires. Two definitions - one <c>ThisTurn</c> for the immunity and one
    /// <c>EndOfTurn</c> for the swap back - would be two things to cancel, two things to order and two
    /// things to restore, for a card that prints one effect.</para>
    ///
    /// <para><b>The roster's first <see cref="ActiveEffectAffectedScopeKind.Hero"/> occurrence.</b>
    /// Every earlier verified effect is about its own controller, his team, or a disk drawn around
    /// him; this one is about a hero the card CHOSE, and about somebody on the other side. The scope
    /// has existed since milestone 6.2a and <see cref="ActiveEffectInstance"/> has refused an
    /// occurrence of it without a hero for just as long - what milestone 8.71d adds is the way a
    /// composed sentence names one, which is
    /// <see cref="ActiveEffectCreationHandler.AffectedHeroParameterName"/>.</para>
    ///
    /// <para><b>The immunity is EVERY HERO's actions and no system event, which is HA-Q11 and
    /// HA-Q13d.</b> "That hero is immune" prints p.18's keyword unqualified, so it is not Whisper's
    /// Death Seeker - hers prints "enemy actions" and names
    /// <see cref="RestrictedActorRelation.HeroesOpposingSource"/> alone. Naming both hero relations is
    /// how this build spells "every hero": Hanu and his own team by
    /// <see cref="RestrictedActorRelation.HeroesAlliedWithSource"/>, and the swapped hero's whole
    /// side - the source's opponents - by the other. <see cref="RestrictedActorScope.AnyActor"/> was
    /// the other candidate and is deliberately NOT taken: <see cref="RestrictedActorResolution"/>
    /// answers it for an UNDEFINED actor as well, which is how the page's own corrections ask - "a
    /// Place the rules carry out has no acting hero" - so it would have made the swapped hero immune
    /// to the end-of-round Lane Push, to the Minion Battle's own bookkeeping and to a respawn
    /// placement. p.18's Immune is written from an actor's side ("you cannot target that unit, and
    /// that unit is not affected by YOUR actions"), and a rules correction is nobody's action.</para>
    ///
    /// <para><b>Two of p.18's fixed rules are carried by the page rather than by this declaration.</b>
    /// "You are never immune to your own actions" is applied by
    /// <see cref="ImmunityQuery"/> before any declaration is read, so the swapped hero acts on their
    /// own turn exactly as they would have; and "effects that check for unit presence will count
    /// immune units" is true because the presence predicates never ask immunity at all. Neither needs
    /// a clause here, and a clause for either would be this card restating the page.</para>
    ///
    /// <para><b>Cancellations are p.17's two.</b> "If you are defeated" ends it - which is
    /// <see cref="ActiveEffectCancellationCondition.ControllerHeroDefeated"/> and reads HANU, the hero
    /// whose action created it, so an effect aimed at an enemy is not cancelled by that enemy's own
    /// Defeat (milestone 7.4 drew exactly that line). "When the card changes state" is
    /// <see cref="ActiveEffectCancellationCondition.CardsRetrieved"/>. A swapped hero who is defeated
    /// and has not respawned by the turn's end is handled by the body rather than by a cancellation:
    /// it finds nobody to swap with and p.10 stops the step.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Hanu
    /// content until 8.71h deals him - milestone 7.9's gate - so
    /// <see cref="HanuComponentContent.JourneyDeferredHandlers"/> is the whole list until then and
    /// <c>HanuJourneyTests</c> asserts the absence from both sides.</para>
    /// </summary>
    public static class HanuActiveEffectContent
    {
        /// <summary>
        /// p.17's two ways an Active effect ends other than by its own duration - the same pair every
        /// verified hero declares, stated here because a hero's content owns its own declarations.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId UnexpectedJourneyEffect { get; } =
            new EffectCapabilityId("hanu-unexpected-journey-effect");

        public static EffectCapabilityId ThereAndBackAgainEffect { get; } =
            new EffectCapabilityId("hanu-there-and-back-again-effect");

        public static EffectCapabilityId SafeTravelsEffect { get; } =
            new EffectCapabilityId("hanu-safe-travels-effect");

        public static EffectCapabilityReference UnexpectedJourneyEffectV1 =>
            new EffectCapabilityReference(UnexpectedJourneyEffect, 1);

        public static EffectCapabilityReference ThereAndBackAgainEffectV1 =>
            new EffectCapabilityReference(ThereAndBackAgainEffect, 1);

        public static EffectCapabilityReference SafeTravelsEffectV1 =>
            new EffectCapabilityReference(SafeTravelsEffect, 1);

        public static ActiveEffectDefinition UnexpectedJourneyDefinition { get; } =
            JourneyDefinition(
                "hanu-unexpected-journey-effect",
                "HanuUnexpectedJourney",
                "hanu-unexpected-journey",
                HanuEffectContent.UnexpectedJourneyTriggeredEffect.Reference);

        public static ActiveEffectDefinition ThereAndBackAgainDefinition { get; } =
            JourneyDefinition(
                "hanu-there-and-back-again-effect",
                "HanuThereAndBackAgain",
                "hanu-there-and-back-again",
                HanuEffectContent.ThereAndBackAgainTriggeredEffect.Reference);

        public static ActiveEffectDefinition SafeTravelsDefinition { get; } =
            JourneyDefinition(
                "hanu-safe-travels-effect",
                "HanuSafeTravels",
                "hanu-safe-travels",
                HanuEffectContent.SafeTravelsTriggeredEffect.Reference);

        /// <summary>
        /// "That hero is immune" - p.18's whole immunity, against every hero's actions and against no
        /// system event, for the window the effect stands in. See the type's own remarks for why both
        /// hero relations are named and why <see cref="RestrictedActorScope.AnyActor"/> is not.
        /// </summary>
        private static IEnumerable<RuleModification> WholeImmunityFromEveryHero => new[]
        {
            RuleModification.ForImmunity(
                new[] { RuleActivity.All },
                RestrictedActorScope.Of(
                    RestrictedActorRelation.HeroesOpposingSource,
                    RestrictedActorRelation.HeroesAlliedWithSource))
        };

        /// <summary>
        /// The shape all three journeys share: created and activated by the Skill that prints them,
        /// carrying the immunity for as long as they stand, triggering at the turn's own end, and
        /// expiring at that same moment so each fires exactly once.
        ///
        /// <para>No radius is declared, and that is the "regardless of radius" half of the printed
        /// sentence stated structurally: the effect measures no disk at all, because the hero it is
        /// about is NAMED rather than found. The Radius icon is read once, by the selection that
        /// chose them, and never again.</para>
        ///
        /// <para>A uniqueness key per face rather than one shared key, because p.17 lets two different
        /// cards stand at once and two journeys of one hero cannot: the key is what stops a second
        /// Unexpected Journey in one turn becoming a second occurrence, and keeping the three faces
        /// apart is what lets a fixture stand two of them on one board and watch the pipeline order
        /// them.</para>
        /// </summary>
        private static ActiveEffectDefinition JourneyDefinition(
            string definitionId,
            string effectKind,
            string uniquenessKey,
            EffectDefinitionReference triggeredEffect) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.Hero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnEnded,
                Cancellations,
                false,
                effectKind,
                uniquenessKey,
                new[] { TimingCheckpoint.TurnEnded },
                WholeImmunityFromEveryHero,
                triggeredEffect: triggeredEffect);

        /// <summary>The three creations, for the fixtures and for the registration 8.71h will make.</summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(
                UnexpectedJourneyEffectV1, UnexpectedJourneyDefinition),
            new ActiveEffectCreationHandler(
                ThereAndBackAgainEffectV1, ThereAndBackAgainDefinition),
            new ActiveEffectCreationHandler(SafeTravelsEffectV1, SafeTravelsDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            UnexpectedJourneyDefinition,
            ThereAndBackAgainDefinition,
            SafeTravelsDefinition
        });
    }
}
