using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// A named state a hero can be in that card text tests by name, rather than by asking about a
    /// value, a permission or an object type.
    ///
    /// <para><b>Why this is a vocabulary of its own.</b> Rulebook p.17's Active Effects entry is a
    /// mechanism - a card turned on its side while something it granted is still running - and the six
    /// heroes built before this one only ever granted <em>rules</em> that way: a value contribution, a
    /// restriction, an immunity, a counts-as. Ursafar's set grants something else. Fourteen of his
    /// nineteen faces end with <c>This round: You are enraged.</c>, and all nineteen open by asking
    /// whether he already is. Nothing about being enraged changes a number or forbids an action; the
    /// state exists solely so that his own card text can read it, and <c>enrage</c> appears nowhere in
    /// the rulebook, so the state is defined by those faces and nothing else. That is the standing
    /// "cards are the definition" case, and this is where such a definition lives.</para>
    ///
    /// <para><b>Why a closed vocabulary rather than a string.</b> A condition named by a string would be
    /// a condition any content could invent and any reader could misspell, and the effect frame has no
    /// string-valued input to carry one in the first place - <c>EffectValueKind</c> admits booleans,
    /// integers, pieces, cards, coordinates and node addresses, and deliberately no free text. A closed
    /// enum is what the frame can carry, what a snapshot writes as a bounded integer, and what makes a
    /// condition nobody grants a compile-time question rather than a silent never-true.</para>
    ///
    /// <para>It is a hero-derived member in a shared vocabulary, which is this project's ordinary shape
    /// for a fact one printed set forced - <c>RosterValueQueryKind.TargetPlayedAttackCardThisTurn</c>
    /// and <c>SourceValueQueryKind.EnemyUnitsAdjacentToAffectedObject</c> are the same thing said about
    /// Brogan and Xargatha.</para>
    /// </summary>
    public enum HeroConditionKind
    {
        None = 0,

        /// <summary>
        /// Ursafar's <c>enraged</c>. Fourteen of his faces grant it for the round and all nineteen read
        /// it; his Ultimate asserts it permanently.
        /// </summary>
        Enraged = 1
    }

    /// <summary>
    /// Whether a hero is currently in a named condition, and the two content facts that make one true.
    ///
    /// <para><b>Both sources, because the roster prints both.</b> A condition is asserted either by a
    /// live p.17 Active effect - <c>This round: You are enraged.</c> - or by a standing declaration a
    /// p.16 Ultimate makes - Unbound Fury's "You are always enraged". They are genuinely different
    /// mechanisms with different lifetimes, and a query that read only one would answer the Ultimate's
    /// own sentence wrong, so both are asked here and nowhere else.</para>
    ///
    /// <para><b>Nothing is stored and nothing new is carried.</b> The Active-effect arm matches on
    /// <see cref="ActiveEffectDefinition.EffectKind"/>, a typed string every definition has already
    /// declared since milestone 5.4 and which had no reader until now; the standing arm matches on
    /// <see cref="ContinuousRuleSource.RuleKind"/>, which <c>UnlockedUltimate</c> already builds from
    /// the Ultimate's own declared rule kind. So a condition adds no field to any definition, no
    /// property to any type the content-identity oracle walks, and no byte to any snapshot: it is a
    /// reading of content that is already there.</para>
    ///
    /// <para><b>Pure, and that is the point.</b> Every argument is handed in, so the whole of Ursafar's
    /// build-up rule can be proved at a desk - which matters, because rulebook p.10's written order and
    /// p.17's "after you are done performing the action" together decide whether a card can enrage
    /// itself, and the answer (it cannot) is the difference between two completely different heroes.
    /// </para>
    /// </summary>
    public static class HeroConditionQuery
    {
        /// <summary>
        /// The <see cref="ActiveEffectDefinition.EffectKind"/> a granting Active effect declares for
        /// each condition. Stated here rather than on the granting content so that the grant and the
        /// reading cannot drift apart: one table, two callers.
        /// </summary>
        public static string EffectKind(HeroConditionKind condition)
        {
            switch (condition)
            {
                case HeroConditionKind.Enraged:
                    return "UrsafarEnraged";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(condition), condition,
                        "A named hero condition states the Active-effect kind that grants it.");
            }
        }

        /// <summary>
        /// The <see cref="ContinuousRuleSource.RuleKind"/> of the standing declaration that asserts each
        /// condition permanently, or <c>null</c> where no printed content asserts one that way.
        ///
        /// <para>Unbound Fury is the roster's first, and its clause is unconditional - "You are
        /// <b>always</b> enraged" - so it is read as a source of the state rather than as a grant with a
        /// very long duration. The practical difference is p.17's cancellation set: an Active effect
        /// ends when its card changes state, when the round ends, and when its hero is defeated, and
        /// none of those may end a sentence that says "always".</para>
        /// </summary>
        public static string StandingRuleKind(HeroConditionKind condition)
        {
            switch (condition)
            {
                case HeroConditionKind.Enraged:
                    return "UrsafarUnboundFury";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether <paramref name="heroId"/> is currently in <paramref name="condition"/>.
        ///
        /// <para>An Active effect counts only while it is <see cref="ActiveEffectPhase.Active"/>. A
        /// dormant one has been created and has not begun, and that distinction is exactly what makes
        /// Ursafar's build-up rule true no matter how his card text is ordered: every grant of his is
        /// created when its action starts and activates only at that action's end, so the card doing
        /// the granting cannot read its own grant. A reading that counted dormant instances would turn
        /// his whole set into cards that always fire, and would make his Ultimate's first clause say
        /// nothing.</para>
        /// </summary>
        public static bool IsInCondition(
            HeroConditionKind condition,
            HeroId heroId,
            IEnumerable<ActiveEffectInstance> effects,
            IEnumerable<ContinuousRuleSource> continuousSources)
        {
            if (condition == HeroConditionKind.None)
            {
                throw new ArgumentException(
                    "A condition reading names the condition it is about.", nameof(condition));
            }

            if (!heroId.IsDefined)
            {
                throw new ArgumentException(
                    "A condition reading names the hero it is about.", nameof(heroId));
            }

            var effectKind = EffectKind(condition);
            if ((effects ?? Enumerable.Empty<ActiveEffectInstance>()).Any(effect =>
                    effect != null &&
                    effect.Phase == ActiveEffectPhase.Active &&
                    effect.ControllerHeroId.Equals(heroId) &&
                    string.Equals(
                        effect.Definition.EffectKind, effectKind, StringComparison.Ordinal)))
            {
                return true;
            }

            var standingRuleKind = StandingRuleKind(condition);
            return standingRuleKind != null &&
                (continuousSources ?? Enumerable.Empty<ContinuousRuleSource>()).Any(source =>
                    source != null &&
                    source.ControllerHeroId.Equals(heroId) &&
                    string.Equals(source.RuleKind, standingRuleKind, StringComparison.Ordinal));
        }

        /// <summary>
        /// The same reading taken from a live lifecycle - the shape a capability handler has, where the
        /// two collections are the lifecycle's own.
        /// </summary>
        public static bool IsInCondition(
            HeroConditionKind condition, HeroId heroId, ActiveEffectLifecycle timing) =>
            IsInCondition(
                condition,
                heroId,
                timing == null ? null : timing.Effects,
                timing == null ? null : timing.ContinuousSources);
    }
}
