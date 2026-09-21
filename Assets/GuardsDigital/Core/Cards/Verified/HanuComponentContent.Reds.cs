using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's contracts for the clauses milestone 8.71c owns - and the whole of this file
    /// is the statement that there are none.
    ///
    /// <para><b>The block mints no capability, and that is the composition rather than an omission.</b>
    /// His four graded Reds and his Gold basic print four clauses, and each of them is a contract a
    /// dealt hero already starts:</para>
    ///
    /// <list type="bullet">
    /// <item>"Choose one[, or both, in any order]" is <c>attack-sequence@1</c> under an ordinary
    /// <see cref="EffectNodeKind.Choose"/> - Bain's Hand Crossbow and Hunter-Seeker at 1..1 and 1..2,
    /// and the order the actor names is already the order the sequences run in (milestone
    /// 8.35z29).</item>
    /// <item>"Adjacent to your friendly hero[ and not adjacent to you]" is TWO MEMBERS on
    /// <c>AttackSequenceSubjectNarrowing</c>, which is a closed vocabulary and not a capability - the
    /// design that type's own remarks record and that milestone 8.35z45 minted it for. The noun the
    /// bullet targets is one member on <c>AttackTargetableUnits</c>, likewise closed.</item>
    /// <item>"If the target is not defeated" is <c>attack-target-survived@1</c> over
    /// <c>synthetic-is-flagged@1</c> - Ursafar's three removal Reds and Min's Stances.</item>
    /// <item>"If able, move 3 spaces in a straight line" is <c>actor-space@1</c> into
    /// <c>choose-line-destination@1</c> into <c>direct-move@3</c> - Swift's Bounce with its obstacle
    /// clause taken off, because this card prints none.</item>
    /// </list>
    ///
    /// <para>So <see cref="RedDeferredHandlers"/> is EMPTY, and so is
    /// <see cref="RedRegisteredHandlers"/>. Both members exist anyway rather than being left out: the
    /// series' sweeps read "registered or deferred" per block, an empty deferral list is what a block
    /// owing the gate nothing looks like, and 8.71g concatenates the five blocks' pairs whether or not
    /// any of them has an entry. Nothing of his is in
    /// <see cref="MatchContentCatalogs"/> until 8.71h deals him, which is milestone 7.9's gate: the
    /// registered content list is serialized and pinned, so registering a contract before a dealt card
    /// can reach it would move that list, every snapshot's bytes and every corpus digest while the
    /// content itself never moved. <c>HanuRedsTests</c> asserts the absence from both sides.</para>
    /// </summary>
    public static partial class HanuComponentContent
    {
        /// <summary>
        /// Nothing. See this file's own remarks: every clause of this block's six faces composes on a
        /// contract a dealt hero already starts, so there is nothing for 8.71h to register.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RedDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// Nothing, and nothing is owed: a block that minted no contract has none to register when the
        /// dealing comes. The member sits beside <see cref="RedDeferredHandlers"/> so a sweep asking
        /// "registered or deferred" finds the pair this series' every block declares.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RedRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
    }
}
