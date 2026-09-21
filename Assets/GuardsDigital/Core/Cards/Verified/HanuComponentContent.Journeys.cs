using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The one contract Hanu the Trickster's three Blue journeys need that this build did not already
    /// have: the hero an end-of-turn body is swapping BACK with. Milestone 8.71d.
    ///
    /// <para><b>The question it answers, and why nothing shipped could answer it.</b> "Swap with an
    /// enemy hero in radius. <b>This turn:</b> That hero is immune. <b>End of turn:</b> Swap with that
    /// hero regardless of radius and immunity." The words "that hero" point back across a whole turn:
    /// whoever the Skill chose is who the body must find, however far away they have walked and
    /// whatever else has happened. The Active effect already CARRIES that hero -
    /// <see cref="ActiveEffectInstance.AffectedHeroId"/>, which is what
    /// <see cref="ActiveEffectAffectedScopeKind.Hero"/> means - but a triggered body is an ordinary
    /// effect frame and an ordinary frame is handed its controller, its source card and its action,
    /// never the occurrence that started it. So the referent exists and nothing could read it.</para>
    ///
    /// <para><b>It re-derives rather than remembering, which is what makes it survive a snapshot.</b>
    /// The handler is given nothing and stores nothing: it asks the lifecycle it is standing in for
    /// the Hero-scoped occurrence whose controller, source card and source action are the ones this
    /// frame is running under, and reads the hero off it. A restore, a replay and a rewind all rebuild
    /// the lifecycle from the payload that already persists those four facts, so all three ask the
    /// same question of the same state and get the same answer - and not one byte is added to snapshot
    /// schema v43. Min's Grenade reads its own token at fire time for exactly this reason.</para>
    ///
    /// <para><b>Why the four facts identify one occurrence.</b> A card instance performs one action
    /// once, and each of the three faces declares its own uniqueness key, so the lifecycle holds at
    /// most one Hero-scoped journey per (controller, card, action). The occurrence has already ENDED
    /// by the time the body runs - an <c>EndOfTurn</c> effect triggers and expires at the same
    /// checkpoint, in that order (milestone 8.35z30) - so the walk deliberately does not ask about
    /// phase; an ended effect is still in the list, which is the same fact milestone 8.67's restored
    /// remainder is derived from.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Hanu
    /// content until 8.71h deals him - milestone 7.9's gate rather than an oversight, because the
    /// registered list is serialized and pinned - so <see cref="JourneyRegisteredHandlers"/> is empty,
    /// <see cref="JourneyDeferredHandlers"/> is the whole list, and <c>HanuJourneyTests</c> asserts
    /// the absence from both sides. This is a new handler TYPE, so the dealing owes it a showcase
    /// consumer as well as a registration.</para>
    /// </summary>
    public static partial class HanuComponentContent
    {
        /// <summary>
        /// "that hero" - the hero the journey standing on this frame's own card is bound to, together
        /// with the space they are standing in when the turn ends.
        /// </summary>
        public static EffectCapabilityId JourneyPartner { get; } =
            new EffectCapabilityId("hanu-journey-partner");

        public static EffectCapabilityReference JourneyPartnerV1 =>
            new EffectCapabilityReference(JourneyPartner, 1);

        /// <summary>Who they are - published because a reader of the outcome should see the name.</summary>
        public const string JourneyPartnerHeroOutputName = "hero";

        /// <summary>
        /// Where they are standing, which is the value p.19's exchange actually consumes: one
        /// obstacle per space makes a space an unambiguous identity for every kind at once, and every
        /// swapping face in this build names its subject that way.
        /// </summary>
        public const string JourneyPartnerSpaceOutputName = "space";

        /// <summary>Nothing, until 8.71h deals him - see the type's own remarks.</summary>
        public static IEnumerable<IEffectCapabilityHandler> JourneyRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The four contracts this block declares and this build withholds: the partner lookup, and
        /// the three Active-effect creations in <see cref="HanuActiveEffectContent"/> - one per face,
        /// because <see cref="ActiveEffectCreationHandler"/> is one registration per definition.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> JourneyDeferredHandlers() =>
            new IEffectCapabilityHandler[] { new JourneyPartnerHandler() }
                .Concat(HanuActiveEffectContent.Handlers());

        /// <summary>
        /// The lookup itself. It performs nothing and chooses nothing: it reads one recorded fact and
        /// publishes it, so a body that cannot find its partner is a mandatory step with no subject
        /// and p.10 stops the effect there - which is exactly what "that hero" means on a board the
        /// hero has left.
        /// </summary>
        private sealed class JourneyPartnerHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => JourneyPartnerV1;

            /// <summary>
            /// NO parameters, and that is the sentence rather than a saving: "that hero" names no
            /// class, no reach and no side - it points at a decision already made, and the only place
            /// that decision lives is the occurrence this frame is running for. A parameter here would
            /// be a second way to say who, which is a way for the two to disagree.
            /// </summary>
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    Array.Empty<EffectCapabilityParameter>(),
                    new[]
                    {
                        new EffectOutputBinding(
                            JourneyPartnerHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            JourneyPartnerSpaceOutputName, EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryFindPartner(context, out var hero, out var at))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        JourneyPartnerHeroOutputName, EffectValue.Hero(hero)),
                    new EffectOutcomeFact(
                        JourneyPartnerSpaceOutputName, EffectValue.Space(at))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryFindPartner(context, out _, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            /// <summary>
            /// The hero this frame's own journey is bound to, and where they are standing now.
            ///
            /// <para>Nothing is published for a hero who is off the board - defeated at the turn's end
            /// and not yet respawned is the case HA-Q11 names - because the exchange takes a space and
            /// a hero standing nowhere has none. The other half of that reading, Hanu himself being
            /// off the board, is refused by <c>synthetic-swap-actor@1</c>, which reads the actor's own
            /// space and resolves no pair without one.</para>
            /// </summary>
            private static bool TryFindPartner(
                EffectCapabilityContext context, out GuardsDigital.Core.Units.Heroes.HeroId hero,
                out HexCoordinate at)
            {
                hero = default;
                at = default;
                var timing = context.Timing;
                if (timing == null || context.Components?.Board == null ||
                    !context.ActorId.IsDefined)
                {
                    return false;
                }

                var journey = timing.Effects
                    .Where(effect =>
                        effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.Hero &&
                        effect.AffectedHeroId.IsDefined &&
                        effect.ControllerHeroId.Equals(context.ActorId) &&
                        effect.SourceCardInstanceId.Equals(context.SourceCardInstanceId) &&
                        effect.SourceActionDefinitionId.Equals(context.ActionDefinitionId))
                    .OrderByDescending(effect => effect.CreationSequence)
                    .FirstOrDefault();
                if (journey == null)
                {
                    return false;
                }

                hero = journey.AffectedHeroId;
                return context.Components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(hero), out at);
            }
        }
    }
}
