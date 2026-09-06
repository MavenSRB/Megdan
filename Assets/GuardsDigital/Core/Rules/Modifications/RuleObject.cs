using System;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// What a rules object intrinsically is, independent of anything that changes its effective
    /// characteristics. Rulebook p.19 defines units as heroes and minions and states that tokens are
    /// not units, so those two are the objects a restriction, permission, immunity, or
    /// <c>Counts As</c> can currently name.
    ///
    /// Tokens were deliberately absent until milestone 8.35z32. Milestone 6.4 recorded the reason
    /// and the condition for lifting it in one sentence - "a pushed token holds no immunity of its own
    /// because p.19 states tokens are not units, so the rules-object vocabulary has no token member" -
    /// and Tali's Venerated Totem is the verified text that condition was waiting for: <c>Totem tokens
    /// are immune.</c> See <see cref="Token"/>.
    /// </summary>
    public enum RuleObjectKind
    {
        None = 0,
        Hero = 1,
        Minion = 2,

        /// <summary>
        /// Milestone 8.35z32, and the roster's first token immunity. One placed token, named as the
        /// object a modification is addressed to.
        ///
        /// <para><b>The tension the member resolves, stated precisely.</b> p.18's <c>Immune</c> entry is
        /// written entirely about units - "If a <em>unit</em> is immune, you cannot target that unit,
        /// and that unit is not affected by your actions, unless the card text says otherwise" - and
        /// p.19 says outright that "Tokens are not units". So the glossary entry Venerated Totem invokes
        /// does not, on its own face, apply to the thing that card applies it to. The card is what
        /// extends it, which is exactly the licence p.18's own closing clause grants; the member exists
        /// because a printed card said so, not because the vocabulary looked incomplete.</para>
        ///
        /// <para><b>It is the smallest widening that serves the printed sentence.</b> A token is still
        /// not a unit anywhere else in this engine: it is not counted by p.8's Minion Battle, it is not
        /// a target of p.13's Defeat, it has no team side, and <c>IntrinsicKindQuery</c> answers about
        /// it without ever calling it one. What the member buys is a single thing - that
        /// <c>ImmunityQuery</c> can be asked about a token and answer, so that RULING 26's line holds:
        /// player actions including p.13's Clear are refused, while the system-level removals p.8 and
        /// p.19 perform - the lane push's spawn clearing and the end-of-round cleanup - are not asking
        /// this question at all and still take the token off the board.</para>
        /// </summary>
        Token = 3
    }

    /// <summary>
    /// The stable identity of one object a modification is addressed to.
    ///
    /// It carries the intrinsic kind and exactly one typed identity, so a modification names a hero or a
    /// minion without either an untyped string key or a separate parallel list per kind.
    /// </summary>
    public readonly struct RuleObject : IEquatable<RuleObject>
    {
        private RuleObject(
            RuleObjectKind kind, HeroId heroId, MinionId minionId, TokenInstanceId tokenInstanceId)
        {
            Kind = kind;
            HeroId = heroId;
            MinionId = minionId;
            TokenInstanceId = tokenInstanceId;
        }

        /// <summary>
        /// The named absence of an object: nothing is named, which is a different question from
        /// naming something.
        ///
        /// Milestone 6.8i made the actor and the self-exclusion required wherever they decide a rules
        /// answer, and this is what a genuinely actorless or self-less call site writes. It matters
        /// most at <see cref="RuleModificationSet.IsImmuneTo"/>: p.18's "You are never immune to your
        /// own actions" can only apply when an actor is named, so asking with this value asks the
        /// question in the abstract - no actor is performing anything - rather than asking about an
        /// actor who happens to be nobody.
        /// </summary>
        public static RuleObject None => default;

        public static RuleObject ForHero(HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A hero rules object requires a defined hero ID.", nameof(heroId));
            }

            return new RuleObject(RuleObjectKind.Hero, heroId, default, default);
        }

        public static RuleObject ForMinion(MinionId minionId)
        {
            if (!minionId.IsDefined)
            {
                throw new ArgumentException("A minion rules object requires a defined minion ID.", nameof(minionId));
            }

            return new RuleObject(RuleObjectKind.Minion, default, minionId, default);
        }

        /// <summary>
        /// One placed token, as the object a modification is addressed to. Milestone 8.35z32; see
        /// <see cref="RuleObjectKind.Token"/> for why the member exists and how narrow it is.
        ///
        /// <para>It names the <em>instance</em> and not the definition, because that is what the rule
        /// is about: Venerated Totem's sentence applies to the totems on the board, and p.19's "Once a
        /// token is placed, it retains all its properties until that token is removed" makes each
        /// placed copy the thing that carries a rule.</para>
        /// </summary>
        public static RuleObject ForToken(TokenInstanceId tokenInstanceId)
        {
            if (!tokenInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "A token rules object requires a defined token instance ID.",
                    nameof(tokenInstanceId));
            }

            return new RuleObject(RuleObjectKind.Token, default, default, tokenInstanceId);
        }

        public RuleObjectKind Kind { get; }

        public HeroId HeroId { get; }

        public MinionId MinionId { get; }

        /// <summary>The placed token this object names, or the undefined value for a unit.</summary>
        public TokenInstanceId TokenInstanceId { get; }

        public bool IsDefined => Kind != RuleObjectKind.None;

        public bool Equals(RuleObject other) =>
            Kind == other.Kind && HeroId.Equals(other.HeroId) && MinionId.Equals(other.MinionId) &&
            TokenInstanceId.Equals(other.TokenInstanceId);

        public override bool Equals(object obj) => obj is RuleObject other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ HeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ MinionId.GetHashCode();
                hashCode = (hashCode * 397) ^ TokenInstanceId.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case RuleObjectKind.Hero:
                    return $"Hero {HeroId.Value}";
                case RuleObjectKind.Minion:
                    return $"Minion {MinionId.Value}";
                case RuleObjectKind.Token:
                    return $"Token {TokenInstanceId.Value}";
                default:
                    return "Undefined rules object";
            }
        }
    }
}
