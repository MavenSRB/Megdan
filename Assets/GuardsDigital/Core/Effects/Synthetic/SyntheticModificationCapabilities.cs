using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// The bounded synthetic content milestone 6.8 proves the modifier, restriction, permission,
    /// immunity, and effective-characteristic contracts against.
    ///
    /// One capability per declared family, each creating exactly one Active effect on the acting hero.
    /// They are separate registrations rather than one capability with a mode parameter because
    /// <see cref="IEffectCapabilityHandler.ActiveEffectDefinitions"/> means "every definition this
    /// capability can create", and a mode selector would either make that list a lie or smuggle the
    /// choice through an untyped payload.
    ///
    /// The four delayed lifetime forms are all represented, and each is declared rather than inferred:
    /// creation, activation, and expiration checkpoints, the cross-round flag, and the cancellation
    /// conditions are definition data the lifecycle already owns. None of these values or texts is
    /// official content; milestones 6.8a and 6.8b bind configured cards to these same contracts.
    /// </summary>
    public static class SyntheticModificationCapabilities
    {
        public static EffectCapabilityId GrantDefenseBonus { get; } =
            new EffectCapabilityId("synthetic-grant-defense-bonus");

        public static EffectCapabilityId RestrictFastTravel { get; } =
            new EffectCapabilityId("synthetic-restrict-fast-travel");

        public static EffectCapabilityId PermitMoveThroughObstacles { get; } =
            new EffectCapabilityId("synthetic-permit-move-through-obstacles");

        public static EffectCapabilityId GrantPushImmunity { get; } =
            new EffectCapabilityId("synthetic-grant-push-immunity");

        public static EffectCapabilityId CountAsMinion { get; } =
            new EffectCapabilityId("synthetic-count-as-minion");

        public static EffectCapabilityReference GrantDefenseBonusV1 =>
            new EffectCapabilityReference(GrantDefenseBonus, 1);

        public static EffectCapabilityReference RestrictFastTravelV1 =>
            new EffectCapabilityReference(RestrictFastTravel, 1);

        public static EffectCapabilityReference PermitMoveThroughObstaclesV1 =>
            new EffectCapabilityReference(PermitMoveThroughObstacles, 1);

        public static EffectCapabilityReference GrantPushImmunityV1 =>
            new EffectCapabilityReference(GrantPushImmunity, 1);

        public static EffectCapabilityReference CountAsMinionV1 =>
            new EffectCapabilityReference(CountAsMinion, 1);

        /// <summary>
        /// Rulebook p.17 lists three cancellations for an Active effect on your card: it no longer
        /// applies, you are defeated, or the card changes state "including at the end of the round".
        /// Resolving the source card is deliberately not one of them - p.17 has you turn the card on its
        /// side <em>after</em> performing the action precisely to signal that its effect is still
        /// running - so these declare the defeat and end-of-round retrieval conditions and leave the
        /// duration's own expiration checkpoint to end them normally.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] GeneralCancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        /// <summary>`This Turn: +1 Defense`, applied to the acting hero.</summary>
        public static ActiveEffectDefinition DefenseBonusDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-defense-bonus-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            GeneralCancellations,
            false,
            "SyntheticDefenseBonus",
            "synthetic-defense-bonus",
            Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForValue(PrintedValueChannel.Defense, 1) });

        /// <summary>`This Round: you cannot Fast Travel`, applied to the acting hero.</summary>
        public static ActiveEffectDefinition FastTravelRestrictionDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-fast-travel-restriction-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisRound,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.RoundEnd,
            GeneralCancellations,
            false,
            "SyntheticFastTravelRestriction",
            "synthetic-fast-travel-restriction",
            Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForRestriction(RuleActivity.FastTravel) });

        /// <summary>`Next Turn: you may Move Through obstacles`, applied to the acting hero.</summary>
        public static ActiveEffectDefinition MoveThroughPermissionDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-move-through-permission-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.NextTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            GeneralCancellations,
            true,
            "SyntheticMoveThroughPermission",
            "synthetic-move-through-permission",
            Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles) });

        /// <summary>
        /// `Next Turn, after playing cards: you are immune to Push`, applied to the acting hero. The
        /// immunity names Object Push, whose effect arrives at milestone 6.4; the declaration and its
        /// distinct activation anchor are what milestone 6.8 owns.
        /// </summary>
        public static ActiveEffectDefinition PushImmunityDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-push-immunity-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.NextTurnAfterPlayingCards,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.CardsPlayedAndRevealed,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            GeneralCancellations,
            true,
            "SyntheticPushImmunity",
            "synthetic-push-immunity",
            Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForImmunity(RuleActivity.ObjectPush) });

        /// <summary>`This Round: you count as a minion`, applied to the acting hero.</summary>
        public static ActiveEffectDefinition CountsAsMinionDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-counts-as-minion-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisRound,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.RoundEnd,
            GeneralCancellations,
            false,
            "SyntheticCountsAsMinion",
            "synthetic-counts-as-minion",
            Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForCountsAs(EffectiveUnitKind.Minion) });

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ModificationEffectHandler(GrantDefenseBonusV1, DefenseBonusDefinition),
            new ModificationEffectHandler(RestrictFastTravelV1, FastTravelRestrictionDefinition),
            new ModificationEffectHandler(PermitMoveThroughObstaclesV1, MoveThroughPermissionDefinition),
            new ModificationEffectHandler(GrantPushImmunityV1, PushImmunityDefinition),
            new ModificationEffectHandler(CountAsMinionV1, CountsAsMinionDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            DefenseBonusDefinition,
            FastTravelRestrictionDefinition,
            MoveThroughPermissionDefinition,
            PushImmunityDefinition,
            CountsAsMinionDefinition
        });

        /// <summary>
        /// Creates its one declared Active effect on the acting hero.
        ///
        /// The handler stays a pure rules executor: it decides that the effect exists, and the lifecycle
        /// owns when it activates, when it expires, and what cancels it. Creating an effect that already
        /// exists is a legal no-op rather than a failure, which is what <c>Applied</c> means, and the
        /// lifecycle's own uniqueness rule refuses a duplicate independently.
        /// </summary>
        private sealed class ModificationEffectHandler : IEffectCapabilityHandler
        {
            private readonly ActiveEffectDefinition definition;

            internal ModificationEffectHandler(
                EffectCapabilityReference reference,
                ActiveEffectDefinition definition)
            {
                Reference = reference;
                this.definition = definition;
                ActiveEffectDefinitions = Array.AsReadOnly(new[] { definition });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("affected", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var affected = new[] { new EffectOutcomeFact("affected", EffectValue.Hero(context.ActorId)) };
                if (context.Timing == null || context.Participants == null)
                {
                    // Outside a live match there is no lifecycle to create into. A legal rules no-op.
                    return EffectExecutionResult.Applied(affected);
                }

                var effectId = ActiveEffectInstanceIdentity.Mint(context, definition);
                if (context.Timing.Effects.All(effect => !effect.Id.Equals(effectId)))
                {
                    context.Timing.Create(
                        effectId,
                        definition,
                        context.ActorId,
                        context.Participants.GetTeamForHero(context.ActorId).Side,
                        default,
                        context.SourceCardInstanceId,
                        context.ActionDefinitionId,
                        Math.Max(context.RoundIndex, 1),
                        Math.Max(context.TurnIndex, 1));
                }

                return EffectExecutionResult.Applied(affected);
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Always applies, so the answer needs to read nothing - which is the point of asking here
            /// rather than executing: this handler's own execution creates the effect and mints its
            /// instance identity, and a probe that ran it would grant the modification it was asked
            /// whether the actor could grant.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }
    }
}
