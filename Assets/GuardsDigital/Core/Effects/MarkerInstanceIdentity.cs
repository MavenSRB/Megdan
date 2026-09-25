using System;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Milestone 6.8h: the one place a marker attachment's instance ID is composed.
    ///
    /// A marker-shaped sibling of <see cref="ActiveEffectInstanceIdentity"/> rather than a reuse of it.
    /// The two share the activation anchors - source card, action, node, round, and turn - but differ in
    /// their last term, because what distinguishes two marker attachments made by one activation is who
    /// holds them, while what distinguishes two Active effects is which definition they instantiate.
    /// Composing both from one method would need a term neither family always has.
    ///
    /// The identity distinguishes activations, not merely give sites. Before this contract the
    /// capability composed card, node, and holder alone: a marker given, returned to the supply, and
    /// given again to the same holder by replaying the same card composed a byte-identical ID, so two
    /// genuinely different attachments shared one identity. Nothing refused it - the register drops a
    /// returned attachment, so its uniqueness guard saw no duplicate - which made the collision silent,
    /// and every operation that names a marker names it by this ID.
    ///
    /// The round and turn anchors are pure functions of the accepted-command prefix, so the identity is
    /// deterministic under snapshot restore, replay, and retained-prefix Undo: no counter, clock, or
    /// GUID. Unlike the effect contract they are not clamped, because a give reaches this composition
    /// only with live components - that is, inside a real round and turn - and clamping a value that
    /// cannot occur would fold a hypothetical round 0 onto round 1 and manufacture the very collision
    /// this contract exists to remove.
    /// </summary>
    public static class MarkerInstanceIdentity
    {
        /// <summary>
        /// Multiple distinct marker definitions given to one holder by one activation need
        /// distinct physical identities without letting each capability invent an ID shape.
        /// </summary>
        public static MarkerInstanceId Mint(
            EffectCapabilityContext context, HeroId holder,
            MarkerDefinitionReference definition)
        {
            if (!definition.IsDefined)
                throw new ArgumentException("A marker identity requires its definition.",
                    nameof(definition));
            return new MarkerInstanceId(
                Mint(context, holder).Value + ":" + definition.DefinitionId.Value +
                ":v" + definition.Revision);
        }

        public static MarkerInstanceId Mint(EffectCapabilityContext context, HeroId holder)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!holder.IsDefined)
            {
                throw new ArgumentException(
                    "A marker instance identity requires the hero who receives it.", nameof(holder));
            }

            return new MarkerInstanceId(
                $"{context.SourceCardInstanceId.Value}:{context.ActionDefinitionId.Value}" +
                $":{context.NodeId.Value}" +
                $":r{context.RoundIndex}t{context.TurnIndex}" +
                $":{holder.Value}");
        }
    }
}
