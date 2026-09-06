using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Whisper's one persistent text, at milestone <c>8.35z33</c>: the turn-long protection
    /// Death Seeker's first bullet raises over her.
    ///
    /// <para><b>One definition, because her set prints one Active-effect keyword.</b> The inventory's
    /// sweep of all nineteen faces found exactly one p.17 keyword anywhere - Death Seeker's
    /// <b>This turn:</b> - and it sits inside a modal bullet. Every other card of hers resolves
    /// immediately, and Grim Reaper is p.16's continuous permanent ability rather than an Active
    /// effect.</para>
    ///
    /// <para><b>This is a declaration, not a handler</b>, registering through
    /// <see cref="ActiveEffectCreationHandler"/> - the shape milestone 6.8 proved and every hero since
    /// reuses. No card name is read at runtime and nothing here asks the board a question.</para>
    /// </summary>
    public static class WhisperActiveEffectContent
    {
        /// <summary>
        /// Rulebook p.17's cancellations for an Active effect on your card, as every earlier hero set
        /// declares them: the controller is defeated, or the card changes state at the end of the
        /// round. Resolving the source card is deliberately not one of them - p.17 has you turn the
        /// card on its side <em>after</em> performing the action precisely to signal the effect is
        /// still running.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId DeathSeekerImmunity { get; } =
            new EffectCapabilityId("whisper-death-seeker-immunity");

        public static EffectCapabilityReference DeathSeekerImmunityV1 =>
            new EffectCapabilityReference(DeathSeekerImmunity, 1);

        /// <summary>
        /// Death Seeker's first bullet: "<b>This turn:</b> You are immune to enemy actions."
        ///
        /// <para><b>It composes with no new vocabulary at all, and that is worth stating because the
        /// inventory forecast predicted otherwise.</b> Every immunity the project had built before this
        /// names <em>which</em> activities it covers - Brogan's Bulwark names four - and this face
        /// names none, scoping by who is acting instead. The forecast therefore costed it as needing
        /// "at most an every-activity arm". Measuring first showed the arm already exists:
        /// <see cref="RuleActivity.All"/> has been a member since the enum was written,
        /// <see cref="RuleModification.ForImmunity"/> documents it in as many words - "state
        /// RuleActivity.All when it names them all" - and
        /// <see cref="AppliedRuleModification"/>'s reach test already reads it. So this declaration is
        /// Bulwark's with the four-activity list replaced by one member and the disk removed.</para>
        ///
        /// <para><b>Who is protected: her, and only her.</b> The sentence says "You", so the scope is
        /// <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/> and no disk is drawn - the same
        /// reading Arien's Expert Duelist takes of the same pronoun. Bulwark's wider scope comes from
        /// its own printed "and friendly units in radius", which this face does not print.</para>
        ///
        /// <para><b>Who is barred, and what still reaches her.</b> The 2026-09-06 ruling reads "enemy
        /// actions" as <em>all enemy hero actions</em> - attack, skill, forced move, push, swap, place,
        /// discard-forcing: an enemy hero can neither target nor affect her this turn. System events
        /// are not anybody's action and still reach her, which is what
        /// <see cref="RestrictedActorRelation.HeroesOpposingSource"/> says by naming heroes: the
        /// end-of-round Minion Battle, p.8's Lane Push and the round's own cleanup have no acting hero
        /// to be opposed to, so none of them is filtered by this declaration. That is the ruling built
        /// in rather than restated - there is no clause here excluding system events, because an
        /// actor-scoped immunity never reached them in the first place.</para>
        ///
        /// <para><b>p.18's own exception needs no clause either.</b> "You are never immune to your own
        /// actions" and "effects that check for unit presence will count immune units" are the page's
        /// rules about immunity rather than this card's, and
        /// <see cref="RuleActivity"/>'s reach test plus the source relation already carry both.</para>
        /// </summary>
        public static ActiveEffectDefinition DeathSeekerDefinition { get; } =
            new ActiveEffectDefinition(
                "whisper-death-seeker-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "WhisperDeathSeeker",
                "whisper-death-seeker",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForImmunity(
                        new[] { RuleActivity.All },
                        RestrictedActorScope.Of(RestrictedActorRelation.HeroesOpposingSource))
                },
                null,
                DeclaredRadius.None,
                SourceTeamRelation.None);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(DeathSeekerImmunityV1, DeathSeekerDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            DeathSeekerDefinition
        });
    }
}
