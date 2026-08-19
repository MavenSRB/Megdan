using System;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The permanent ability one Ultimate card prints, as immutable definition content.
    ///
    /// Rulebook p.16 states the whole of its lifetime in one sentence: "It provides you with a powerful
    /// permanent ability that is always in effect (as long as your hero is on the board)." Everything
    /// this type does follows from that sentence rather than from a modelling preference.
    ///
    /// <para><b>The applicability is not content.</b> A token property declares its own applicability
    /// because different tokens can have different ones; p.16 states one applicability for every
    /// Ultimate there is, so <see cref="ApplicabilityKind"/> is a constant rather than a constructor
    /// argument. A per-definition field here would be a place for a future card to state something the
    /// page does not allow it to state.</para>
    ///
    /// <para><b>The rule is a declaration, not a lifetime.</b> Like
    /// <c>TokenPropertyDefinition</c>, this carries a typed <see cref="RuleModification"/> and nothing
    /// about when it applies. <c>UltimateUnlockQuery</c> binds it to the hero it affects and the board
    /// question that decides whether it applies right now, and nothing is stored: an unlocked Ultimate
    /// supplies its rule again on restore because the card is still beside the dashboard and the
    /// trusted definition still declares this.</para>
    /// </summary>
    public sealed class UltimateRuleDefinition
    {
        /// <summary>
        /// p.16's one applicability, shared by every Ultimate: the rule applies exactly while its
        /// controlling hero is on the board. It is the string a derived
        /// <c>ContinuousRuleSource</c> carries, so a payload records which question was asked rather
        /// than leaving the answer implicit in the source kind.
        /// </summary>
        public const string ApplicabilityKind = "WhileControllerHeroOnBoard";

        public UltimateRuleDefinition(string ruleId, string ruleKind, RuleModification modification)
        {
            if (string.IsNullOrWhiteSpace(ruleId) || string.IsNullOrWhiteSpace(ruleKind))
            {
                throw new ArgumentException("An Ultimate's ability requires stable identity and a rule kind.");
            }

            RuleId = ruleId;
            RuleKind = ruleKind;

            // Unlike a token property, this may not be declaration-only. A token can legitimately
            // declare a source and a lifetime with no typed rule yet, because milestone 6.3 recorded
            // the seam before any verified text filled it. An Ultimate that supplied nothing would be a
            // card whose whole printed content is an ability, printing no ability.
            Modification = modification ??
                throw new ArgumentNullException(
                    nameof(modification),
                    "An Ultimate's whole printed content is its permanent ability (rulebook p.16); " +
                    "a declaration with no rule would be a card that does nothing.");
        }

        /// <summary>This ability's stable identity inside its card, and the derived source's identity suffix.</summary>
        public string RuleId { get; }

        public string RuleKind { get; }

        /// <summary>The typed rule this Ultimate supplies while its hero is on the board.</summary>
        public RuleModification Modification { get; }

        public override string ToString() => $"{RuleId} ({RuleKind}): {Modification}";
    }
}
