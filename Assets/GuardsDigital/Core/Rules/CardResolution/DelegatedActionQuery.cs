using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.71b. One hero's next action being performed by another hero's player: who the
    /// action belongs to, and whose seat decides it.
    ///
    /// <para>The whole of it is derived, so nothing here is ever stored, projected or sent - see
    /// <see cref="DelegatedActionQuery"/> for why that is the printed sentence rather than a
    /// saving.</para>
    /// </summary>
    public readonly struct DelegatedAction : IEquatable<DelegatedAction>
    {
        internal DelegatedAction(
            HeroId delegatedHeroId,
            HeroId decidingHeroId,
            CardInstanceId delegatedCardInstanceId,
            CardInstanceId decidingCardInstanceId)
        {
            DelegatedHeroId = delegatedHeroId;
            DecidingHeroId = decidingHeroId;
            DelegatedCardInstanceId = delegatedCardInstanceId;
            DecidingCardInstanceId = decidingCardInstanceId;
        }

        /// <summary>No delegation stands, which is every board in this build until one is composed.</summary>
        public static DelegatedAction None => default;

        /// <summary>The hero whose action it is: the one whose card carries the rewrite.</summary>
        public HeroId DelegatedHeroId { get; }

        /// <summary>The hero whose seat chooses that action and answers every choice inside it.</summary>
        public HeroId DecidingHeroId { get; }

        /// <summary>The rewritten card - the one whose next state change ends this delegation.</summary>
        public CardInstanceId DelegatedCardInstanceId { get; }

        /// <summary>The card whose text performed the rewrite, and so named the hero.</summary>
        public CardInstanceId DecidingCardInstanceId { get; }

        public bool IsDefined => DelegatedHeroId.IsDefined && DecidingHeroId.IsDefined;

        /// <summary>Whether this delegation is about <paramref name="heroId"/>'s own action.</summary>
        public bool Delegates(HeroId heroId) => IsDefined && DelegatedHeroId.Equals(heroId);

        public bool Equals(DelegatedAction other) =>
            DelegatedHeroId.Equals(other.DelegatedHeroId) &&
            DecidingHeroId.Equals(other.DecidingHeroId) &&
            DelegatedCardInstanceId.Equals(other.DelegatedCardInstanceId) &&
            DecidingCardInstanceId.Equals(other.DecidingCardInstanceId);

        public override bool Equals(object obj) => obj is DelegatedAction other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DelegatedHeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ DecidingHeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ DelegatedCardInstanceId.GetHashCode();
                return (hashCode * 397) ^ DecidingCardInstanceId.GetHashCode();
            }
        }

        public override string ToString() => !IsDefined
            ? "no delegated action"
            : DecidingHeroId.Value + " decides " + DelegatedHeroId.Value + "'s action (" +
                DelegatedCardInstanceId.Value + " rewritten by " + DecidingCardInstanceId.Value + ")";
    }

    /// <summary>
    /// Milestone 8.71b. Hanu the Trickster's The Ultimate Trick, rulebook p.16's permanent ability:
    /// "You choose the next action, and how it is performed for a hero you target with the
    /// 'Hurry Up!'."
    ///
    /// <para><b>Nothing is stored, and that is the sentence rather than an optimisation.</b> The
    /// only thing "a hero you target with the Hurry Up!" can mean on a live board is a hero whose
    /// card carries the rewrite that sentence writes - milestone 8.71a's
    /// <see cref="PrintedInitiativeOverride"/>, which already names the card text that wrote it. So
    /// the delegation is not a second fact to create, persist, project, send and expire beside the
    /// rewrite: it IS the rewrite, read in the company of p.16's unlocked ability. This is the same
    /// shape <see cref="UnlockedUltimate"/> itself has - "nothing is stored: an unlocked Ultimate
    /// supplies its rule again on restore because the card is still beside the dashboard".</para>
    ///
    /// <para><b>What that derivation buys is every one of HA-Q4's lapse rules, for free and
    /// exactly.</b> The rewrite ends at the first state change of the card it was written on
    /// (HA-Q2), and <c>CardInstance.ApplyState</c> is the one place that happens:
    /// <list type="bullet">
    /// <item>the delegated action COMPLETES - the turn authority resolves the card after the whole
    /// action, including its repeats, grants and borrows, so the delegation covers exactly "the
    /// next action, and how it is performed" and not one command more;</item>
    /// <item>the hero is DEFEATED before acting - p.13 discards their unresolved card;</item>
    /// <item>the ROUND ends - every played card is resolved or discarded by p.8;</item>
    /// <item>p.12's Hold, Fast Travel and Clear each resolve the card they are taken on, so each
    /// spends the delegation exactly as a printed action does.</item>
    /// </list>
    /// A second Hurry up! on the same hero SETS the rewrite again rather than stacking it
    /// (<c>CardLifecycleAuthority.TrySetPrintedInitiativeOverride</c>), so there is one delegation
    /// per rewritten card and never two.</para>
    ///
    /// <para><b>And it needs no wire and no schema.</b> Both facts it reads already cross both: the
    /// rewrite is schema v44's card block and wire <c>guards-private-online/20</c>'s
    /// <c>NetworkCardRow.RewrittenInitiative</c>, and every card it reads is in one of p.4's open
    /// zones - the rewritten card and the rewriting card are both in the turn area from the moment
    /// they are revealed, and p.16 puts the Ultimate faceup beside its dashboard. So every seat
    /// derives the same delegation from its own projection, which is what "the delegation is
    /// visible to every seat" means when the rule is one rule rather than a carried claim about
    /// one.</para>
    ///
    /// <para><b>HA-Q13b, settled by this milestone.</b> The Ultimate names its own controller's
    /// Hurry up! and nothing else, so what this matches is a rewrite whose SOURCE CARD belongs to a
    /// hero whose unlocked Ultimate declares <see cref="DelegatingRuleKind"/>. It never asks which
    /// card definition performed the rewrite: the printed sentence's subject is "you", and the one
    /// sentence in this build that rewrites a printed Initiative is that hero's own Silver.</para>
    ///
    /// <para><b>HA-Q13c, settled by this milestone.</b> p.16 gives an Ultimate its ability "as long
    /// as your hero is on the board", so a delegation lapses the moment its DECIDING hero is
    /// defeated, even though the rewrite it reads survives - the rewrite is a fact about the
    /// targeted hero's card and p.10 ends it only at that card's own state change. The targeted
    /// player takes their own action back, at Initiative 11.</para>
    /// </summary>
    public static class DelegatedActionQuery
    {
        /// <summary>
        /// The <see cref="UltimateRuleDefinition.RuleKind"/> an Ultimate declares to hand its
        /// controller the next action of every hero their own card text has rewritten.
        ///
        /// <para>Spelled here as well as on the content, and asserted equal to it by
        /// <c>HanuUltimateTrickTests</c> - the discipline <c>BorrowedPrimaryAction</c>'s
        /// <c>ResolvedCardsCountAsActiveRuleKind</c> already follows for Ursafar's Unbound Fury,
        /// which is the other standing declaration in this build that a rule reads by name.</para>
        /// </summary>
        public const string DelegatingRuleKind = "HanuTheUltimateTrick";

        /// <summary>
        /// The delegation standing over <paramref name="actorId"/>'s own action right now, or
        /// <see cref="DelegatedAction.None"/>.
        /// </summary>
        /// <remarks>
        /// <para>Pure, and cheap on the board it has nothing to say about: the first pass is a scan
        /// for a rewritten card belonging to this hero, and every board that no Hurry up! has
        /// touched leaves through it without reading a definition.</para>
        /// <para>The answer is taken in card-instance-id order where a hero somehow holds two
        /// rewritten cards, so two derivations of one collection agree - which restore, replay,
        /// rewind and every seat's own copy each require. p.10 gives a hero one unresolved card, so
        /// the ordering decides nothing in a match and states the tie anyway.</para>
        /// </remarks>
        public static DelegatedAction ForActor(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> cards,
            IEnumerable<HeroOccupancy> heroesOnBoard,
            HeroId actorId)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (heroesOnBoard == null) throw new ArgumentNullException(nameof(heroesOnBoard));
            if (!actorId.IsDefined)
            {
                return DelegatedAction.None;
            }

            var instances = cards as IReadOnlyList<CardInstance> ?? cards.ToArray();
            var rewritten = (CardInstance)null;
            foreach (var card in instances)
            {
                if (card == null || !card.InitiativeOverride.IsDefined ||
                    !card.OwnerId.Equals(actorId))
                {
                    continue;
                }

                if (rewritten == null || string.CompareOrdinal(
                        card.Id.Value, rewritten.Id.Value) < 0)
                {
                    rewritten = card;
                }
            }

            if (rewritten == null)
            {
                return DelegatedAction.None;
            }

            var sourceId = rewritten.InitiativeOverride.SourceCardInstanceId;
            var source = instances.FirstOrDefault(card =>
                card != null && card.Id.Equals(sourceId));

            // A rewrite whose source card this reader cannot see names no hero, so it delegates
            // nothing. It is unreachable in a match - a rewriting card is played, revealed and
            // sitting in p.4's open turn area for the rest of the round, so every seat holds it -
            // and it is answered rather than thrown because a query is not the place to fail a
            // match over a card list somebody narrowed.
            if (source == null || source.OwnerId.Equals(actorId))
            {
                return DelegatedAction.None;
            }

            // p.16: "a powerful permanent ability that is always in effect (as long as your hero is
            // on the board)". A defeated Hanu stops deciding; the rewrite he already wrote stands,
            // because p.10 ends that at the rewritten CARD's own state change and at nothing else.
            if (!heroesOnBoard.Any(hero => hero.HeroId.Equals(source.OwnerId)))
            {
                return DelegatedAction.None;
            }

            var ultimate = UltimateUnlockQuery.Derive(definitions, instances).Ultimates
                .FirstOrDefault(candidate => candidate.OwnerId.Equals(source.OwnerId));
            if (ultimate == null || !string.Equals(
                    ultimate.Rule.RuleKind, DelegatingRuleKind, StringComparison.Ordinal))
            {
                return DelegatedAction.None;
            }

            return new DelegatedAction(actorId, source.OwnerId, rewritten.Id, source.Id);
        }

        /// <summary>
        /// The hero whose seat performs <paramref name="actorId"/>'s action: that hero, unless a
        /// delegation stands over it.
        /// </summary>
        public static HeroId DecidingHeroFor(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> cards,
            IEnumerable<HeroOccupancy> heroesOnBoard,
            HeroId actorId)
        {
            var delegation = ForActor(definitions, cards, heroesOnBoard, actorId);
            return delegation.IsDefined ? delegation.DecidingHeroId : actorId;
        }
    }
}
