using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// Milestone 8.40c. The one reading of "a hero has a [bounty] bounty marker" - Bain the Bounty
    /// Hunter's five Green faces, his Hand Crossbow and his Hunter-Seeker.
    ///
    /// <para><b>Why a query rather than three filters.</b> The clause is printed on three different
    /// kinds of declaration and each of them carries a different thing: a capability node can be handed
    /// its marker DEFINITION at construction, a <c>choose-target@2</c> declaration can bind parameters
    /// but has no value kind that names a marker, and a persisted
    /// <c>AttackSequenceSubjectNarrowing</c> is a closed enum that may carry no content at all. Three
    /// filters would have been three places the same printed sentence could come to mean different
    /// things, which is exactly the divergence
    /// <c>AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard</c> avoids by consulting
    /// <c>HeroDiscardPileQuery</c> rather than counting a pile of its own.</para>
    ///
    /// <para><b>Everything here matches by DEFINITION</b>, because a hero wearing Tigerclaw's Poison is
    /// not wearing a bounty and a card that took either would be stating a rule no face prints.
    /// <see cref="HoldersOf"/> is the only place that decides what "holds it" means, and the two
    /// actor-derived members below are that same method asked about the definitions
    /// <see cref="DeclaredBy"/> yields - a composition rather than a second reading.</para>
    ///
    /// <para><b>"His own marker" is read off <see cref="MarkerAttachment.OwnerHeroId"/>, which is the
    /// engine's name for the hero whose card put the marker out.</b> That is what the printed article
    /// means: Dead or Alive and Side Quest are the only faces in the build that give a bounty, they are
    /// Bain's, and a transfer (milestone 8.40b) moves the attachment without minting a new one - so the
    /// owner stays the hero whose content declares it however many times the marker changes hands. A
    /// hero whose content declares two markers has both read, which is what "a poison marker" would
    /// mean on a face of Tigerclaw's own.</para>
    ///
    /// <para>It is deliberately outside <see cref="MarkerAttachmentRegister"/>: the register owns the
    /// supply invariant and the attachment list, and a printed clause about that list is a rules
    /// question asked of it rather than a member of it - the same division <c>ComponentCleanupRules</c>
    /// keeps for p.8's and p.13's returns.</para>
    /// </summary>
    public static class HeldMarkerQuery
    {
        /// <summary>
        /// Every hero holding a marker of <paramref name="definition"/>, in a stable order so two reads
        /// of one board name the same hero first. This is the whole reading; everything else in this
        /// type composes it.
        /// </summary>
        public static IReadOnlyList<HeroId> HoldersOf(
            IEnumerable<MarkerAttachment> attachments, MarkerDefinitionReference definition)
        {
            if (attachments == null) throw new ArgumentNullException(nameof(attachments));
            if (!definition.IsDefined)
            {
                return Array.Empty<HeroId>();
            }

            return attachments
                .Where(attachment =>
                    attachment != null &&
                    attachment.Definition != null &&
                    attachment.Definition.Reference.Equals(definition) &&
                    attachment.HolderHeroId.IsDefined)
                .Select(attachment => attachment.HolderHeroId)
                .Distinct()
                .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>Whether this one hero holds a marker of that definition.</summary>
        public static bool Holds(
            IEnumerable<MarkerAttachment> attachments,
            HeroId hero,
            MarkerDefinitionReference definition) =>
            hero.IsDefined && HoldersOf(attachments, definition).Any(holder => holder.Equals(hero));

        /// <summary>
        /// The definitions of the markers currently out that <paramref name="ownerHeroId"/>'s own
        /// content put there - which is how a declaration that cannot name a marker names one anyway.
        ///
        /// <para>A hero who has given nothing declares nothing here, and the answer is the empty set
        /// rather than every definition on the board: a clause narrowed to a marker that is not out
        /// admits nobody, which is the same set it would have admitted had the definition been named
        /// explicitly.</para>
        /// </summary>
        public static IReadOnlyList<MarkerDefinitionReference> DeclaredBy(
            IEnumerable<MarkerAttachment> attachments, HeroId ownerHeroId)
        {
            if (attachments == null) throw new ArgumentNullException(nameof(attachments));
            if (!ownerHeroId.IsDefined)
            {
                return Array.Empty<MarkerDefinitionReference>();
            }

            return attachments
                .Where(attachment =>
                    attachment != null &&
                    attachment.Definition != null &&
                    attachment.OwnerHeroId.Equals(ownerHeroId))
                .Select(attachment => attachment.Definition.Reference)
                .Distinct()
                .OrderBy(reference => reference.DefinitionId.Value, StringComparer.Ordinal)
                .ThenBy(reference => reference.Revision)
                .ToArray();
        }

        /// <summary>
        /// Every hero holding a marker whose definition <paramref name="ownerHeroId"/>'s content owns -
        /// <see cref="HoldersOf"/> asked about each of <see cref="DeclaredBy"/>'s definitions, and never
        /// a second reading of "holds it".
        /// </summary>
        public static IReadOnlyList<HeroId> HoldersOfAMarkerDeclaredBy(
            IEnumerable<MarkerAttachment> attachments, HeroId ownerHeroId)
        {
            var materialized = attachments as IReadOnlyList<MarkerAttachment> ??
                (attachments ?? throw new ArgumentNullException(nameof(attachments))).ToArray();

            return DeclaredBy(materialized, ownerHeroId)
                .SelectMany(definition => HoldersOf(materialized, definition))
                .Distinct()
                .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>Whether this one hero holds a marker that hero's content owns.</summary>
        public static bool HoldsAMarkerDeclaredBy(
            IEnumerable<MarkerAttachment> attachments, HeroId hero, HeroId ownerHeroId) =>
            hero.IsDefined &&
            HoldersOfAMarkerDeclaredBy(attachments, ownerHeroId).Any(holder => holder.Equals(hero));
    }
}
