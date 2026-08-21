using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Wasp's two persistent basic texts, at milestone 8.9: the pinning field
    /// Magnetic Dagger leaves behind its Attack, and the barrier Static Barrier's Basic Skill raises.
    ///
    /// <para><b>These are declarations, not handlers.</b> Both register through
    /// <see cref="ActiveEffectCreationHandler"/>, the one capability shape whose whole content is an
    /// Active-effect definition, and everything either card does is stated in that definition -
    /// milestone 8.7 built both consumption paths and proved them on definitions with no numbers in
    /// them, and this is the same path with Wasp's numbers filled in. No card name is read anywhere at
    /// runtime, and nothing here asks the board a question.</para>
    ///
    /// <para><b>Why they are Active effects at all and not something the action does.</b> p.17's
    /// keyword is printed on both faces: "<b>This turn:</b>". An Active effect is the only shape in
    /// this build that outlives the action that created it and still ends at the right moment, and
    /// p.17 also gives the pair their moving centre - "an Active effect's Radius moves with its hero" -
    /// which is why neither declaration names a space and both name a radius.</para>
    ///
    /// <para><b>What the radius means.</b> p.9: "If the card text mentions Range/Radius, it refers to
    /// this value", the value on the printed icon - so both are
    /// <see cref="DeclaredRadius.PrintedIcon"/> and p.16's matching item raises them, exactly as it
    /// raises the same icon on the same card while it is being performed. Neither number is written
    /// into the sentence, and the transcriptions in <c>HERO_WASP.md</c> show why: both say "radius"
    /// rather than a figure.</para>
    /// </summary>
    public static class WaspActiveEffectContent
    {
        /// <summary>
        /// Rulebook p.17's three cancellations for an Active effect on your card: it no longer applies,
        /// you are defeated, or the card changes state "including at the end of the round". Resolving
        /// the source card is deliberately not one of them - p.17 has you turn the card on its side
        /// <em>after</em> performing the action precisely to signal the effect is still running - so
        /// the pair declare defeat and end-of-round retrieval, and the `This turn` duration's own
        /// expiration checkpoint ends them normally.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] BasicCardCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId PinEnemyUnitsInRadius { get; } =
            new EffectCapabilityId("wasp-pin-enemy-units-in-radius");

        public static EffectCapabilityId CloseBarrierInRadius { get; } =
            new EffectCapabilityId("wasp-close-barrier-in-radius");

        public static EffectCapabilityReference PinEnemyUnitsInRadiusV1 =>
            new EffectCapabilityReference(PinEnemyUnitsInRadius, 1);

        public static EffectCapabilityReference CloseBarrierInRadiusV1 =>
            new EffectCapabilityReference(CloseBarrierInRadius, 1);

        /// <summary>
        /// Magnetic Dagger's After-the-attack text: "<b>This turn:</b> Enemy units in radius cannot be
        /// swapped or placed by themselves, or by enemy heroes."
        ///
        /// <para><b>Three sentences, three declarations, and nothing left over.</b> "Enemy units in
        /// radius" is the affected scope - <see cref="ActiveEffectAffectedScopeKind.UnitsInRadius"/>
        /// opposing the source, re-derived at every ask so a unit that walks out of the disk walks out
        /// of the rule. "cannot be swapped or placed" is two restrictions, because p.19's Swap and
        /// p.18's Place are two effect families and a declaration that named one would drop half the
        /// card. "by themselves, or by enemy heroes" is the actor scope milestone 8.7 added, and it is
        /// what keeps Wasp's own teammates able to reposition the units she pinned - a card that barred
        /// everyone would be a stronger card than this one.</para>
        ///
        /// <para><b>Whose enemies.</b> Both halves are read from Wasp's side of the board, because
        /// that is the side card text is written from: the pinned units are the ones opposing her, and
        /// the heroes barred from moving them are the heroes opposing her - which, for a unit that
        /// already opposes her, is its own team. The declaration says this once, through
        /// <see cref="SourceTeamRelation"/> and <see cref="RestrictedActorRelation"/>, so the same
        /// definition means the same thing whichever team plays the card.</para>
        ///
        /// <para><b>It is created at rulebook p.14's step 7 and activates there.</b> "After the attack"
        /// is the printed keyword and step 7 is where the page applies it - after the target has
        /// defended or been defeated and removed - so that is both this effect's creation and its
        /// activation checkpoint, and the lifecycle activates it on the spot rather than leaving it
        /// dormant for a checkpoint that has already passed.</para>
        ///
        /// <para>Crackling Dagger prints this sentence and this Radius icon exactly, so the p.20
        /// handicap card composes onto this same definition when its deferral is lifted; nothing here
        /// is Magnetic Dagger's alone.</para>
        /// </summary>
        public static ActiveEffectDefinition PinnedEnemyUnitsDefinition { get; } =
            new ActiveEffectDefinition(
                "wasp-pinned-enemy-units-effect",
                ActiveEffectAffectedScopeKind.UnitsInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                BasicCardCancellations,
                false,
                "WaspPinnedEnemyUnits",
                "wasp-pinned-enemy-units",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForRestriction(
                        RuleActivity.Swap, MagneticActors),
                    RuleModification.ForRestriction(
                        RuleActivity.Place, MagneticActors)
                },
                null,
                DeclaredRadius.PrintedIcon(3),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// Static Barrier's Basic Skill: "<b>This turn:</b> While an enemy hero outside of radius is
        /// performing an action, spaces in radius count as obstacles. While an enemy hero in radius is
        /// performing an action, spaces outside of radius count as obstacles."
        ///
        /// <para>Two printed sentences, one declaration with both arms, because they are two halves of
        /// one boundary rather than two rules: an enemy hero outside cannot get in and an enemy hero
        /// inside cannot get out, which is what makes the side it stands on stable for the whole of its
        /// own action. p.18 owns what an obstacle then does to movement, to a push and to a Place, and
        /// milestone 8.7's overlay reaches all three through the one classification, so this card
        /// declares no consumer of its own.</para>
        ///
        /// <para>It conditions on heroes opposing the source and on nobody else: Wasp and her
        /// teammates walk through their own barrier, and a moment with no hero performing an action -
        /// a round boundary, p.18's own after-action correction - has nobody for the condition to hold
        /// of, so the spaces are ordinary.</para>
        ///
        /// <para>A Skill's effect is the action (p.12), so it is created and activated at
        /// <see cref="TimingCheckpoint.ActionStarted"/> rather than at one of p.14's Attack
        /// moments.</para>
        /// </summary>
        public static ActiveEffectDefinition StaticBarrierDefinition { get; } =
            new ActiveEffectDefinition(
                "wasp-static-barrier-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                BasicCardCancellations,
                false,
                "WaspStaticBarrier",
                "wasp-static-barrier",
                Array.Empty<TimingCheckpoint>(),
                null,
                new[]
                {
                    ConditionalObstacleOverlay.Declare(
                        SourceTeamRelation.OpposingSource,
                        ObstacleOverlayArm.SpacesInsideWhileActorOutside,
                        ObstacleOverlayArm.SpacesOutsideWhileActorInside)
                },
                DeclaredRadius.PrintedIcon(2));

        /// <summary>
        /// "by themselves, or by enemy heroes", stated once because Magnetic Dagger's two restrictions
        /// name the same actors and a second copy could drift from the first.
        /// </summary>
        private static RestrictedActorScope MagneticActors =>
            RestrictedActorScope.Of(
                RestrictedActorRelation.AffectedObjectItself,
                RestrictedActorRelation.HeroesOpposingSource);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(PinEnemyUnitsInRadiusV1, PinnedEnemyUnitsDefinition),
            new ActiveEffectCreationHandler(CloseBarrierInRadiusV1, StaticBarrierDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            PinnedEnemyUnitsDefinition,
            StaticBarrierDefinition
        });
    }
}
