using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The cards a restriction leaves alone, as definition content.
    ///
    /// <para>Milestone 8.20 adds it because Arien's Spell Break is the first verified text whose
    /// prohibition carves out a card rather than an actor: "Enemy heroes in radius cannot perform Skill
    /// actions, <b>except on the gold cards</b>." Milestone 8.7's <see cref="RestrictedActorScope"/>
    /// answers the other half of the same sentence shape - who is barred - and cannot answer this one,
    /// because the exception is not about anybody's team. Two heroes standing in the same disk with the
    /// same relation to the source are barred or not barred by which card each of them is holding.</para>
    ///
    /// <para><b>Why a colour and not a card.</b> Ledger question A-03, answered by the user on
    /// 2026-08-23 and recorded in <c>RULE_SYSTEMS.md</c>'s card-upgrades row: a card text naming a
    /// colour names that colour, and "Basic" is the supertype spanning Gold <em>and</em> Silver. So the
    /// exception is stated in <see cref="CardColor"/> and a Silver basic Skill is barred - which is the
    /// live consequence the user accepted when they answered it, Wasp's Static Barrier being exactly
    /// such a card.</para>
    ///
    /// <para><b>Why it is a scope object with a named unqualified form</b>, for the reason milestone
    /// 8.7 gave the actor scope and 6.8f gave the modification set: a declaration that excepts nothing
    /// must say so out loud, and <see cref="EveryCard"/> is where that is written. An empty exception
    /// list is refused - it would read as a carve-out while carving nothing out.</para>
    /// </summary>
    public sealed class RestrictedSourceCardScope
    {
        private RestrictedSourceCardScope(IReadOnlyList<CardColor> exceptedColors)
        {
            ExceptedColors = exceptedColors;
        }

        /// <summary>
        /// The unqualified reading: the restriction reaches the activity on every card, which is what
        /// every restriction declared before milestone 8.20 means.
        /// </summary>
        public static RestrictedSourceCardScope EveryCard { get; } =
            new RestrictedSourceCardScope(
                new ReadOnlyCollection<CardColor>(Array.Empty<CardColor>()));

        /// <summary>
        /// A restriction that does not reach an action performed from a card of one of the named
        /// colours. Order is the card's own and is preserved, so a refusal can name the carve-out in
        /// the order the sentence prints it.
        /// </summary>
        public static RestrictedSourceCardScope ExceptColors(params CardColor[] colors)
        {
            if (colors == null)
            {
                throw new ArgumentNullException(nameof(colors));
            }

            if (colors.Length == 0)
            {
                throw new ArgumentException(
                    "A source-card scope names at least one excepted colour; state " +
                    "RestrictedSourceCardScope.EveryCard when the restriction excepts none.",
                    nameof(colors));
            }

            if (colors.Any(color => color == CardColor.None) ||
                !colors.All(color => Enum.IsDefined(typeof(CardColor), color)))
            {
                throw new ArgumentException(
                    "A source-card scope names printed colours; CardColor.None is the absence of a " +
                    "colour rather than a sixth one.",
                    nameof(colors));
            }

            if (colors.Distinct().Count() != colors.Length)
            {
                throw new ArgumentException(
                    "A source-card scope names each colour once.", nameof(colors));
            }

            return new RestrictedSourceCardScope(
                new ReadOnlyCollection<CardColor>(colors.ToArray()));
        }

        /// <summary>The colours this declaration carves out; empty for the unqualified scope.</summary>
        public IReadOnlyList<CardColor> ExceptedColors { get; }

        /// <summary>Whether this scope carves nothing out.</summary>
        public bool IsUnqualified => ExceptedColors.Count == 0;

        /// <summary>
        /// Whether an action performed from a card of <paramref name="sourceCardColor"/> escapes this
        /// restriction.
        ///
        /// <para><see cref="CardColor.None"/> escapes nothing. It is what a caller passes when it holds
        /// no card at all - the general-rule offers for Hold, Clear and Fast Travel are performed from
        /// no card, and rulebook p.11-p.13 give none of them a colour - and a carve-out about the gold
        /// cards cannot be met by an action that is on no card. Answering otherwise would let a
        /// cardless action slip a prohibition that names one.</para>
        /// </summary>
        public bool Excepts(CardColor sourceCardColor) =>
            sourceCardColor != CardColor.None && ExceptedColors.Contains(sourceCardColor);

        public override string ToString() =>
            IsUnqualified
                ? "every card"
                : "except " + string.Join(", ", ExceptedColors);
    }
}
