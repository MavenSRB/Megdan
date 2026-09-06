using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Whether a hero may take rulebook p.10's Repeat offer at all. Milestone 8.35z28.
    ///
    /// <para>Dodger the Warlock's Enfeeblement is what asks: "<b>This turn:</b> Enemy heroes in radius
    /// have -6 [Attack] Attack <b>and cannot repeat actions</b>." The user's 2026-09-06 ruling on D-09
    /// scopes that to p.10's Repeat keyword alone - replacement actions such as Fast Travel, and the
    /// Clear that substitutes for an Attack, stay available - so this asks one question and no
    /// wider one.</para>
    ///
    /// <para><b>Why it is a query rather than two inline checks.</b> p.10's Repeat reaches a hero by two
    /// routes: a repeat owed after a completed basic action, and a repeat printed inside a card's own
    /// text at p.14. Both are "repeat actions" in the sentence's words, so one clause cannot answer only
    /// one of them - and two inline copies of the same test are exactly the pair that drifts. Both gates
    /// in <c>CardActionResolutionAuthority</c> ask here.</para>
    ///
    /// <para><b>It is asked where the offer is built, never where the answer arrives.</b> The project's
    /// discipline is to remove an unavailable offer rather than reject the command afterwards -
    /// milestone 6.8 set it for the Fast-Travel restriction and 8.35l re-learned it the hard way - so a
    /// hero under Enfeeblement is never shown the repeat.</para>
    ///
    /// <para><b><see cref="ActionRestrictionQuery"/> cannot answer it</b>, which is why this sits beside
    /// that one rather than inside it: that query is keyed to an <see cref="Cards.ActionKind"/>, and
    /// p.10's Repeat is a control-flow modifier on an action rather than an action anybody performs. No
    /// action kind maps to <see cref="RuleActivity.Repeat"/>, by design.</para>
    /// </summary>
    public static class RepeatRestrictionQuery
    {
        /// <summary>
        /// Whether <paramref name="actorId"/> is currently barred from repeating an action.
        ///
        /// <para><paramref name="modifications"/> is required rather than defaulted, by milestone
        /// 6.8f's rule: a caller that forgot the live set would silently offer a repeat a source has
        /// taken away. <see cref="RuleModificationSet.Empty"/> is how a caller says there are none.</para>
        /// </summary>
        public static bool IsRestrictedFromRepeating(
            HeroId actorId, RuleModificationSet modifications)
        {
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            return actorId.IsDefined &&
                modifications.IsRestricted(RuleObject.ForHero(actorId), RuleActivity.Repeat);
        }
    }
}
