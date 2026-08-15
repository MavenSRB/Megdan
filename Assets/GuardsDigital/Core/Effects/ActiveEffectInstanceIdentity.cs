using System;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Milestone 6.8d: the one place an Active-effect instance ID is composed.
    ///
    /// The identity distinguishes activations, not merely definitions. The same card node re-executed
    /// inside one resolution composes the same ID, so a creating handler's existing already-created
    /// pre-check keeps that re-execution a legal no-op; the same mode played again on a later round or
    /// turn composes a fresh ID that no spent instance can collide with. Before this contract each
    /// capability composed card, node, and definition alone, all of which repeat on a replay, and
    /// <see cref="ActiveEffectLifecycle.Create"/> refuses a duplicate ID including a spent one - so a
    /// card played a second time silently created nothing.
    ///
    /// The round and turn anchors are pure functions of the accepted-command prefix, the same values
    /// the action resolution ID is minted from, so the identity is deterministic under snapshot
    /// restore, replay, and retained-prefix Undo: no counter, clock, or GUID. The clamps mirror what
    /// the handlers already pass to <see cref="ActiveEffectLifecycle.Create"/> outside a live round.
    ///
    /// A capability that later re-runs one node as several distinct activations inside a single
    /// resolution (the bounded Repeat planned at 6.5) threads its iteration discriminator through the
    /// context and changes only this composition.
    /// </summary>
    public static class ActiveEffectInstanceIdentity
    {
        public static ActiveEffectId Mint(
            EffectCapabilityContext context,
            ActiveEffectDefinition definition)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new ActiveEffectId(
                $"{context.SourceCardInstanceId.Value}:{context.ActionDefinitionId.Value}" +
                $":{context.NodeId.Value}" +
                $":r{Math.Max(context.RoundIndex, 1)}t{Math.Max(context.TurnIndex, 1)}" +
                $":{definition.DefinitionId}");
        }
    }
}
