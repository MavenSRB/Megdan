using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// The complete persisted state of a <see cref="MarkerAttachmentRegister"/>.
    /// </summary>
    /// <remarks>
    /// Same discipline as <c>ActiveEffectLifecycleRestoreState</c>: every member is a required
    /// constructor argument and <see cref="Deconstruct"/> extends that to the write direction, so a
    /// serializer that destructures this state fails on arity when a persisted field is added rather
    /// than silently omitting it from the stream.
    /// </remarks>
    public sealed class MarkerAttachmentRestoreState
    {
        public MarkerAttachmentRestoreState(IEnumerable<MarkerAttachment> attachments)
        {
            Attachments = Array.AsReadOnly(
                (attachments ?? throw new ArgumentNullException(nameof(attachments))).ToArray());
            if (Attachments.Any(attachment => attachment == null))
            {
                throw new ArgumentException("Restored marker attachments must be non-null.", nameof(attachments));
            }

            if (Attachments.Select(attachment => attachment.Id).Distinct().Count() != Attachments.Count)
            {
                throw new ArgumentException("Restored marker attachment IDs must be unique.", nameof(attachments));
            }
        }

        public IReadOnlyList<MarkerAttachment> Attachments { get; }

        public void Deconstruct(out IReadOnlyList<MarkerAttachment> attachments)
        {
            attachments = Attachments;
        }
    }

    /// <summary>
    /// The authoritative record of which markers are attached to which heroes.
    ///
    /// It is the marker counterpart of <c>BoardOccupancyIndex</c>, and owns the one invariant markers
    /// have: a finite supply. <see cref="Give"/> is the single site that can exceed it, so the supply
    /// cannot be overdrawn by a caller that forgot to check, exactly as the occupancy index's single
    /// <c>Add</c> owns one-obstacle-per-space for every board occupant kind.
    ///
    /// It deliberately has no coordinate, no map, and no occupancy concept. A marker is not on the board
    /// (rulebook p.18), so the type that records markers must not be able to say where one is.
    /// </summary>
    public sealed class MarkerAttachmentRegister
    {
        private readonly List<MarkerAttachment> attachments;

        public MarkerAttachmentRegister()
        {
            attachments = new List<MarkerAttachment>();
        }

        private MarkerAttachmentRegister(IEnumerable<MarkerAttachment> existing)
        {
            attachments = new List<MarkerAttachment>(existing);
        }

        public IReadOnlyList<MarkerAttachment> Attachments => attachments.AsReadOnly();

        public IEnumerable<MarkerAttachment> HeldBy(HeroId holderHeroId) =>
            attachments.Where(attachment => attachment.HolderHeroId.Equals(holderHeroId));

        public IEnumerable<MarkerAttachment> GivenBy(GuardsDigital.Core.Cards.CardInstanceId sourceCardInstanceId) =>
            attachments.Where(attachment => attachment.SourceCardInstanceId.Equals(sourceCardInstanceId));

        /// <summary>
        /// Every attachment of one declared definition, wherever it sits and whatever card gave it.
        /// Milestone 8.40b, and the third finder rather than a widening of either existing one: a card
        /// that says "the marker" names a kind of marker, not a giver and not a holder.
        /// </summary>
        public IEnumerable<MarkerAttachment> OfDefinition(MarkerDefinitionReference reference) =>
            attachments.Where(attachment => attachment.Definition.Reference.Equals(reference));

        /// <summary>
        /// How many of this marker are still in the supply, derived from the trusted definition's total
        /// rather than from a stored counter. There is no second number to drift.
        /// </summary>
        public int CountAvailable(MarkerDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return Math.Max(0, definition.SupplyCount - CountHeld(definition.Reference));
        }

        public int CountHeld(MarkerDefinitionReference reference) =>
            attachments.Count(attachment => attachment.Definition.Reference.Equals(reference));

        /// <summary>
        /// Attaches a marker taken from the supply. Throws rather than refusing, because a caller
        /// reaches here only after its own rules check: an exhausted supply at this point is an engine
        /// defect, and the aggregate's commit boundary rolls the whole command back.
        /// </summary>
        public void Give(MarkerAttachment attachment)
        {
            if (attachment == null) throw new ArgumentNullException(nameof(attachment));

            if (attachments.Any(existing => existing.Id.Equals(attachment.Id)))
            {
                throw new ArgumentException("Marker attachment IDs must be unique.", nameof(attachment));
            }

            if (CountAvailable(attachment.Definition) < 1)
            {
                throw new InvalidOperationException(
                    $"Marker '{attachment.Definition.Reference}' has no copy left in the supply to give.");
            }

            attachments.Add(attachment);
        }

        /// <summary>
        /// Detaches a marker and returns it to its supply, yielding the attachment that ended so the
        /// caller can publish which marker left which holder.
        /// </summary>
        public MarkerAttachment Return(MarkerInstanceId markerInstanceId)
        {
            var index = attachments.FindIndex(attachment => attachment.Id.Equals(markerInstanceId));
            if (index < 0)
            {
                throw new InvalidOperationException("No marker with that instance ID is currently attached.");
            }

            var returned = attachments[index];
            attachments.RemoveAt(index);
            return returned;
        }

        /// <summary>
        /// Moves one attached marker from the hero holding it to another hero, as ONE operation.
        /// Milestone 8.40b, for Bain's Close Call - "that hero gives the marker to you".
        ///
        /// <para><b>Not a <see cref="Return"/> followed by a <see cref="Give"/>, and the reason is a
        /// printed card.</b> A give is what his Ultimate watches, and it watches for a give BAIN
        /// performs, so a composition that ran one would fire it on a sentence that never says "you
        /// give". The identity is the second reason: <see cref="MarkerAttachment.Id"/> is what a
        /// restore rebinds against and what every operation that names a marker names it by, so the
        /// physical piece that moves across the table has to keep the one it already has.</para>
        ///
        /// <para>The supply is untouched by construction: the count held is derived from the
        /// attachments, and this replaces one attachment with one attachment. Moving a marker onto the
        /// hero already holding it is a legal no-op rather than a refusal - a card that says "give it
        /// to you" when you already hold it asks for nothing.</para>
        /// </summary>
        public MarkerAttachment Transfer(MarkerInstanceId markerInstanceId, HeroId holderHeroId)
        {
            if (!holderHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A transfer names the hero the marker moves to; a marker held by nobody is in the supply.",
                    nameof(holderHeroId));
            }

            var index = attachments.FindIndex(attachment => attachment.Id.Equals(markerInstanceId));
            if (index < 0)
            {
                throw new InvalidOperationException("No marker with that instance ID is currently attached.");
            }

            var moving = attachments[index];
            if (moving.HolderHeroId.Equals(holderHeroId))
            {
                return moving;
            }

            // Everything but the holder is carried over, so the attachment that arrives is the one that
            // left: same instance identity, same owning supply, same giving card and action. Its
            // property sources are re-derived from the new holder by the constructor, which is what
            // makes "whose values the marker changes" move with it without this method saying so.
            var moved = new MarkerAttachment(
                moving.Id,
                moving.Definition,
                moving.OwnerHeroId,
                holderHeroId,
                moving.SourceCardInstanceId,
                moving.SourceActionDefinitionId);
            attachments[index] = moved;
            return moved;
        }

        public MarkerAttachmentRestoreState CaptureRestoreState() =>
            new MarkerAttachmentRestoreState(attachments);

        public static MarkerAttachmentRegister Restore(MarkerAttachmentRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Deconstruct(out var restored);
            var register = new MarkerAttachmentRegister(restored);
            foreach (var group in restored.GroupBy(attachment => attachment.Definition.Reference))
            {
                if (group.Count() > group.First().Definition.SupplyCount)
                {
                    throw new ArgumentException(
                        $"Restored state holds more '{group.Key}' markers than the trusted supply contains.",
                        nameof(state));
                }
            }

            return register;
        }

        public MarkerAttachmentRegister Clone() => new MarkerAttachmentRegister(attachments);
    }
}
