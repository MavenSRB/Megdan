using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.37a. Tali the Spiritcaller's one Active effect, and the reason she has exactly one.
    ///
    /// <para><b>Milestone 8.37 recorded her as declaring none, and that was true of the set it
    /// shipped.</b> Her nineteen faces print no <c>This turn:</c> and no <c>This round:</c> at all -
    /// the one Active-effect keyword anywhere on her set is Blizzard's <c>End of turn:</c>, and
    /// Blizzard was the single face that milestone left deferred. Composing it is what gives her an
    /// Active effect, so this file arrives with that face rather than beside the other eleven heroes'
    /// at her own dealing.</para>
    ///
    /// <para><b>The shape is Silverarrow's, unchanged.</b> Her two <c>End of turn:</c> Skills
    /// established it at milestone 8.35z46: an effect created and activated by the Skill that prints
    /// it, triggering at the turn's own end and expiring at that same moment, so it fires exactly
    /// once. Nothing about that shape is Tali's, which is the point - what differs is only the
    /// triggered effect it names.</para>
    ///
    /// <para><b>It affects nobody while it stands.</b> No radius is declared and no rule is modified:
    /// the effect exists purely to hold a checkpoint, and every question the clause asks - whether
    /// Blizzard is in the discard, which direction, which units - is asked by
    /// <see cref="TaliEffectContent.BlizzardRepeatTriggeredEffect"/> when the checkpoint arrives.</para>
    /// </summary>
    public static class TaliActiveEffectContent
    {
        /// <summary>
        /// The two conditions that end it early, on exactly Silverarrow's terms: her own defeat, and
        /// p.8's card retrieval, which takes back the card the clause is about.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId BlizzardEndOfTurnRepeat { get; } =
            new EffectCapabilityId("tali-blizzard-end-of-turn-repeat");

        public static EffectCapabilityReference BlizzardEndOfTurnRepeatV1 =>
            new EffectCapabilityReference(BlizzardEndOfTurnRepeat, 1);

        /// <summary>
        /// "If Blizzard is in your discard, <b>End of turn:</b> May repeat once." - the effect the
        /// Skill arms as its last step, which the turn's end then asks.
        /// </summary>
        public static ActiveEffectDefinition BlizzardRepeatDefinition { get; } =
            new ActiveEffectDefinition(
                "tali-blizzard-repeat-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.EndOfTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnEnded,
                Cancellations,
                false,
                "TaliBlizzardRepeat",
                "tali-blizzard-repeat",
                new[] { TimingCheckpoint.TurnEnded },
                triggeredEffect: TaliEffectContent
                    .BlizzardRepeatTriggeredEffect(TaliCardContent.ContentRevision)
                    .Reference);

        /// <summary>
        /// Her one creation, on the type this build has had since milestone 6.8 - so it owes milestone
        /// 7.9's gate no showcase card of its own, on the sentence five earlier dealings have written.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(BlizzardEndOfTurnRepeatV1, BlizzardRepeatDefinition)
        };

        /// <summary>Every definition this capability can create, for a fixture that needs them all.</summary>
        public static IEnumerable<ActiveEffectDefinition> Definitions() => new[]
        {
            BlizzardRepeatDefinition
        };
    }
}
