using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Which side of the source a hero stands on. Declared relative to the source's controller for the
    /// reason <see cref="RestrictedActorScope"/>'s relations are: card text says "enemy hero" from the
    /// perspective of the card, so the declaration means the same thing whichever team plays it.
    /// </summary>
    public enum SourceTeamRelation
    {
        None = 0,
        OpposingSource = 1,
        AlliedWithSource = 2,
        AnyTeam = 3
    }

    /// <summary>
    /// One half of a conditional obstacle overlay: which spaces count as obstacles, and where the
    /// acting hero has to be standing for them to.
    ///
    /// The two arms are the two sentences Wasp's Static Barrier prints. A declaration may carry either
    /// or both; both together is a boundary no qualifying hero's action crosses in either direction,
    /// which is what that card means and what makes the reading below stable - a hero that cannot cross
    /// the boundary is on the same side of it at the end of its action as at the start.
    /// </summary>
    public enum ObstacleOverlayArm
    {
        None = 0,

        /// <summary>
        /// "While an enemy hero outside of radius is performing an action, spaces in radius count as
        /// obstacles."
        /// </summary>
        SpacesInsideWhileActorOutside = 1,

        /// <summary>
        /// "While an enemy hero in radius is performing an action, spaces outside of radius count as
        /// obstacles."
        /// </summary>
        SpacesOutsideWhileActorInside = 2
    }

    /// <summary>
    /// A declared rule that makes spaces count as obstacles while a qualifying hero is acting, as
    /// definition content.
    ///
    /// Milestone 8.7 adds it as the second of the two declaration shapes Wasp's persistent texts need,
    /// and it is deliberately not a <see cref="RuleModification"/>. Every member of that family names
    /// an object and changes something about it; this names no object at all - it changes p.18's
    /// classification of spaces - so binding it to an affected object would be a fiction the whole
    /// modification surface would then have to keep asking about.
    ///
    /// What it shares with the modification families is its lifetime and its provenance: it is declared
    /// on an Active-effect definition, supplied only while that instance is active, and re-derived on
    /// every ask, so nothing is stored and a restored effect produces exactly this overlay again.
    ///
    /// Neither the radius nor the anchor is named here. p.17 makes the <em>anchor</em> the moving part -
    /// "an Active effect's Radius moves with its hero" - and it is bound at derivation from the source
    /// hero's current space; the <em>radius</em> is the source card's, so milestone 8.9 moved it up to
    /// <see cref="GuardsDigital.Core.Timing.ActiveEffectDefinition.Radius"/> where the effect's one
    /// disk is stated once. p.9 gives a card one Range/Radius icon, and a definition whose overlay
    /// drew its disk at a different number from its own scope would be describing two cards.
    /// </summary>
    public sealed class ConditionalObstacleOverlay
    {
        private ConditionalObstacleOverlay(
            SourceTeamRelation actingHeroRelation,
            IReadOnlyList<ObstacleOverlayArm> arms)
        {
            ActingHeroRelation = actingHeroRelation;
            Arms = arms;
        }

        public static ConditionalObstacleOverlay Declare(
            SourceTeamRelation actingHeroRelation,
            params ObstacleOverlayArm[] arms)
        {
            if (actingHeroRelation == SourceTeamRelation.None ||
                !Enum.IsDefined(typeof(SourceTeamRelation), actingHeroRelation))
            {
                throw new ArgumentException(
                    "An overlay states which heroes' actions it conditions on; state " +
                    "SourceTeamRelation.AnyTeam when it conditions on every hero's.",
                    nameof(actingHeroRelation));
            }

            if (arms == null || arms.Length == 0)
            {
                throw new ArgumentException(
                    "An overlay that declares no arm makes no space an obstacle and is not a rule.",
                    nameof(arms));
            }

            if (arms.Any(arm => arm == ObstacleOverlayArm.None) ||
                !arms.All(arm => Enum.IsDefined(typeof(ObstacleOverlayArm), arm)) ||
                arms.Distinct().Count() != arms.Length)
            {
                throw new ArgumentException(
                    "An overlay's arms must be defined and each declared once.", nameof(arms));
            }

            return new ConditionalObstacleOverlay(
                actingHeroRelation,
                new ReadOnlyCollection<ObstacleOverlayArm>(arms.ToArray()));
        }

        /// <summary>Whose actions this overlay applies during.</summary>
        public SourceTeamRelation ActingHeroRelation { get; }

        /// <summary>The declared arms, in the order the card prints them.</summary>
        public IReadOnlyList<ObstacleOverlayArm> Arms { get; }

        public bool Declares(ObstacleOverlayArm arm)
        {
            if (arm == ObstacleOverlayArm.None)
            {
                throw new ArgumentException("Coverage requires a named arm.", nameof(arm));
            }

            return Arms.Contains(arm);
        }

        public override string ToString() =>
            $"Obstacle overlay for {ActingHeroRelation} ({string.Join(", ", Arms)})";
    }
}
