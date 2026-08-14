using System;
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
    /// Tokens are deliberately absent. A token is an obstacle by the general p.18 obstacle rule rather
    /// than by a modification addressed to it, and no verified text yet declares one as the affected
    /// object of one of these families; adding the member ahead of that text would be inventing a rule.
    /// </summary>
    public enum RuleObjectKind
    {
        None = 0,
        Hero = 1,
        Minion = 2
    }

    /// <summary>
    /// The stable identity of one object a modification is addressed to.
    ///
    /// It carries the intrinsic kind and exactly one typed identity, so a modification names a hero or a
    /// minion without either an untyped string key or a separate parallel list per kind.
    /// </summary>
    public readonly struct RuleObject : IEquatable<RuleObject>
    {
        private RuleObject(RuleObjectKind kind, HeroId heroId, MinionId minionId)
        {
            Kind = kind;
            HeroId = heroId;
            MinionId = minionId;
        }

        public static RuleObject ForHero(HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A hero rules object requires a defined hero ID.", nameof(heroId));
            }

            return new RuleObject(RuleObjectKind.Hero, heroId, default);
        }

        public static RuleObject ForMinion(MinionId minionId)
        {
            if (!minionId.IsDefined)
            {
                throw new ArgumentException("A minion rules object requires a defined minion ID.", nameof(minionId));
            }

            return new RuleObject(RuleObjectKind.Minion, default, minionId);
        }

        public RuleObjectKind Kind { get; }

        public HeroId HeroId { get; }

        public MinionId MinionId { get; }

        public bool IsDefined => Kind != RuleObjectKind.None;

        public bool Equals(RuleObject other) =>
            Kind == other.Kind && HeroId.Equals(other.HeroId) && MinionId.Equals(other.MinionId);

        public override bool Equals(object obj) => obj is RuleObject other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ HeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ MinionId.GetHashCode();
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
                default:
                    return "Undefined rules object";
            }
        }
    }
}
