using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 9.21h. Which bodies may perform a repeat declared
    /// <see cref="RepeatActor.DifferentBody"/> - "a different you may repeat it once" (Twin Strike),
    /// "May repeat once by a different one of you" (Spectacle).
    ///
    /// <para><b>One derivation, asked by the offer and by the answer.</b> The node's question lists
    /// these spaces, and the answer is re-checked against the same list when it arrives, so an offer
    /// can never name a body the answer would refuse, nor the answer take one the offer left out
    /// (lesson 18).</para>
    ///
    /// <para><b>"A different you" is every standing body of the actor except the one acting</b> (RZ-Q4:
    /// the acting body performs the instance that asked; RZ-Q11 and RZ-Q12: the repeat is performed by
    /// another). A one-bodied actor has none, which is why the repeat is never offered to him: p.10's
    /// silent skip, and the "no repeat with one body" the block's acceptance names.</para>
    ///
    /// <para><b>Each body is kept only where the repeat can be performed from it</b>, asked through
    /// the resolution's own repeat probe with that body acting
    /// (<see cref="EffectResolutionContext.RepeatCanBePerformedBy"/>), so Twin Strike's "targeting a
    /// different unit" is judged from the second body: a body with no other unit adjacent to it is
    /// not offered.</para>
    /// </summary>
    public static class RepeatPerformerQuery
    {
        /// <summary>
        /// The spaces of the actor's standing bodies other than the acting body from which the repeat
        /// this node asks for can be performed, in body order.
        /// </summary>
        public static IReadOnlyList<HexCoordinate> Candidates(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectResolutionContext context)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var tokens = context.Components?.Tokens;
            if (node.RepeatActor != RepeatActor.DifferentBody || tokens == null ||
                !tokens.TryGetHeroCoordinate(frame.ActorId, out var acting))
            {
                return Array.Empty<HexCoordinate>();
            }

            return tokens.GetHeroBodies(frame.ActorId)
                .Where(body => !body.Coordinate.Equals(acting))
                .OrderBy(body => body.Body)
                .Where(body => context.RepeatCanBePerformedBy(
                    node.RepeatTargetBinding, body.Body, InheritedContinuation(frame, node)))
                .Select(body => body.Coordinate)
                .ToArray();
        }

        /// <summary>
        /// The continuation the requested instance would start from - its count, and the piece a
        /// bound repeat carries - read exactly as <see cref="RepeatedActionQuery.Requested"/> will
        /// build it, so the probe judges the instance the answer would start.
        /// </summary>
        private static RepeatContinuation InheritedContinuation(
            PendingResolution frame, EffectNodeDefinition node)
        {
            var completed = frame.Repeat.CompletedRepeats + 1;
            return (node.RepeatTargetBinding == RepeatTargetBinding.SameTarget ||
                    node.RepeatTargetBinding == RepeatTargetBinding.DifferentSelectionSubject) &&
                frame.Repeat.HasBoundTarget
                ? RepeatContinuation.NextBoundTo(completed, frame.Repeat.BoundTarget)
                : RepeatContinuation.Next(completed);
        }
    }
}
