using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The one rulebook p.17 Active effect Tigerclaw's set prints. Milestone 8.23.
    ///
    /// <para>Blend Into Shadows is his only card that declares one - "If you are adjacent to terrain,
    /// place yourself into a space in radius; if you do, <b>Next turn:</b> You are immune to enemy
    /// attack actions." Everything else he prints resolves immediately, and Cloak and Daggers is
    /// p.16's permanent ability rather than an Active effect.</para>
    ///
    /// <para><b>It is the shape milestone 8.19c already proved.</b> That milestone built
    /// <c>synthetic-shadowed-next-turn</c> explicitly as "Tigerclaw's Blend Into Shadows shape: next
    /// turn, immune to enemy Attack", on content that is nobody's card, so what lands here is the
    /// printed declaration on the printed card rather than a new mechanism. The numbers and the actor
    /// class are the face's; the window, the cancellations and the delayed start are p.17's and p.19's,
    /// and are unchanged.</para>
    ///
    /// <para><b>Why the effect is created by a node and not by the Place.</b> The face prints "if you
    /// do", so the protection depends on the Place having applied - a hero already standing where he
    /// wants to be, or with no legal space in radius, gets no immunity. That conditional is in
    /// <see cref="TigerclawEffectContent"/> where the sentence is composed; what is here is only what
    /// the effect means once it exists.</para>
    /// </summary>
    public static class TigerclawActiveEffectContent
    {
        public static EffectCapabilityId ShadowedNextTurn { get; } =
            new EffectCapabilityId("tigerclaw-shadowed-next-turn");

        public static EffectCapabilityReference ShadowedNextTurnV1 =>
            new EffectCapabilityReference(ShadowedNextTurn, 1);

        /// <summary>
        /// p.17's three cancellations for an Active effect on your own card, as milestone 6.8 recorded
        /// them and every hero's content has declared since: the controller is defeated, or the cards
        /// are retrieved at the end of the round. Resolving the source card is deliberately not one of
        /// them - a resolved card's Active effect keeps applying, which is what makes "Next turn"
        /// reachable at all.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>
        /// "<b>Next turn:</b> You are immune to enemy attack actions."
        ///
        /// <para>One activity, because the face names one - p.13's Attack action, and not a Push, a
        /// Move or a Skill that happens to hurt. One actor class, because the face says "enemy": a
        /// friendly hero's Attack is not something the printed sentence protects against, and no rule
        /// in this build lets one happen either, so the qualification costs nothing and states what the
        /// card says.</para>
        ///
        /// <para>p.19's <c>Next Turn</c> is what decides the two checkpoints. The effect is created the
        /// moment the Place completes and becomes applicable at Tigerclaw's next <c>TurnStarted</c>, so
        /// an Attack made against him later in the same round but before that turn is not blocked -
        /// which is the whole difference between this keyword and <c>This round</c>. p.8's end-of-round
        /// card retrieval cancels it if that turn never comes.</para>
        /// </summary>
        public static ActiveEffectDefinition ShadowedDefinition { get; } = new ActiveEffectDefinition(
            "tigerclaw-shadowed-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.NextTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            Cancellations,
            true,
            "TigerclawBlendIntoShadows",
            "tigerclaw-blend-into-shadows",
            new TimingCheckpoint[0],
            new[]
            {
                RuleModification.ForImmunity(
                    new[] { RuleActivity.Attack },
                    RestrictedActorScope.Of(RestrictedActorRelation.HeroesOpposingSource))
            });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(ShadowedNextTurnV1, ShadowedDefinition)
        };
    }
}
