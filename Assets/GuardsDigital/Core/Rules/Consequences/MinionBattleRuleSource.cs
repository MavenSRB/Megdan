using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// The stable identity of one trusted rule that changes how rulebook p.8's Minion Battle counts or
    /// removes.
    ///
    /// It is a typed reference rather than a bare string for the reason every capability reference in
    /// this engine is: the catalog is keyed by it, a definition names it, and a definition naming one
    /// nothing registers is refused before a match starts rather than falling through to no rule in the
    /// middle of an end of round.
    /// </summary>
    public readonly struct MinionBattleRuleReference : IEquatable<MinionBattleRuleReference>
    {
        public MinionBattleRuleReference(string ruleId, int version)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                throw new ArgumentException(
                    "A Minion-Battle rule reference requires a stable rule ID.", nameof(ruleId));
            }

            if (version < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(version), version, "A Minion-Battle rule reference is versioned from 1.");
            }

            RuleId = ruleId;
            Version = version;
        }

        public string RuleId { get; }
        public int Version { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(RuleId);

        public bool Equals(MinionBattleRuleReference other) =>
            string.Equals(RuleId, other.RuleId, StringComparison.Ordinal) && Version == other.Version;

        public override bool Equals(object obj) => obj is MinionBattleRuleReference other && Equals(other);

        public override int GetHashCode() =>
            (StringComparer.Ordinal.GetHashCode(RuleId ?? string.Empty) * 397) ^ Version;

        public override string ToString() => $"{RuleId}@{Version}";
    }

    /// <summary>What one rule source adds to its team's side of the Minion Battle count.</summary>
    public enum MinionBattleStrength
    {
        /// <summary>Nothing; the hero is not part of the count at all.</summary>
        None = 0,

        /// <summary>
        /// One Heavy minion's worth. p.8 counts minions to decide the battle and removes Heavies last,
        /// and a card that says "you count as a heavy minion" is saying both halves at once.
        /// </summary>
        HeavyMinion = 1
    }

    /// <summary>What happens instead when this rule's own unit would be removed by the battle.</summary>
    public enum MinionBattleRemovalSubstitution
    {
        /// <summary>Nothing is substituted; the unit is removed as p.8 says.</summary>
        None = 0,

        /// <summary>
        /// The unit stays on the board and its team loses p.8's push instead - Brogan's One Man Army,
        /// "if you would be removed, lose the push instead". It is not a Defeat: no coins are awarded,
        /// no reward eligibility opens, and no Team Life moves, exactly as an ordinary Minion-Battle
        /// removal awards none of those either.
        /// </summary>
        PushLost = 1
    }

    /// <summary>
    /// One trusted declaration that a hero takes part in p.8's Minion Battle under a named rule.
    ///
    /// It is definition content and carries no hero: the identity says which rule this is, and the
    /// participant is bound at derivation from whoever has the source unlocked and on the board. That
    /// is what keeps <c>one-man-army-minion-battle@1</c> a rule rather than a Brogan branch - nothing
    /// here, in the catalog, or in the battle reads a hero name, a display name, or a card name.
    /// </summary>
    public sealed class MinionBattleRuleDeclaration
    {
        public MinionBattleRuleDeclaration(
            MinionBattleRuleReference reference,
            MinionBattleStrength strength,
            MinionBattleRemovalSubstitution removalSubstitution)
        {
            if (!reference.IsDefined)
            {
                throw new ArgumentException(
                    "A Minion-Battle rule declaration requires its reference.", nameof(reference));
            }

            if (strength == MinionBattleStrength.None &&
                removalSubstitution == MinionBattleRemovalSubstitution.None)
            {
                throw new ArgumentException(
                    "A Minion-Battle rule that adds no strength and substitutes no removal changes " +
                    "nothing about the battle, so it is not a rule.",
                    nameof(strength));
            }

            if (!Enum.IsDefined(typeof(MinionBattleStrength), strength) ||
                !Enum.IsDefined(typeof(MinionBattleRemovalSubstitution), removalSubstitution))
            {
                throw new ArgumentException("A Minion-Battle rule declaration states defined outcomes.");
            }

            Reference = reference;
            Strength = strength;
            RemovalSubstitution = removalSubstitution;
        }

        public MinionBattleRuleReference Reference { get; }
        public MinionBattleStrength Strength { get; }
        public MinionBattleRemovalSubstitution RemovalSubstitution { get; }

        public override string ToString() => $"{Reference}: {Strength}, {RemovalSubstitution}";
    }

    /// <summary>
    /// One hero taking part in this round's Minion Battle under a declared rule, bound to the facts the
    /// battle already holds.
    ///
    /// Everything a handler may see is here and nothing else: which hero, which side, and whether that
    /// hero is standing in the current Battle Zone. No card, no display name, no dashboard, and no
    /// board - a rule that could reach the board could answer a question p.8 does not ask.
    /// </summary>
    public readonly struct MinionBattleParticipant
    {
        public MinionBattleParticipant(
            MinionBattleRuleDeclaration declaration,
            HeroId heroId,
            TeamSide teamSide,
            bool isInsideBattleZone)
        {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            if (!heroId.IsDefined)
            {
                throw new ArgumentException(
                    "A Minion-Battle participant names its hero.", nameof(heroId));
            }

            if (teamSide != TeamSide.Red && teamSide != TeamSide.Blue)
            {
                throw new ArgumentException(
                    "A Minion-Battle participant stands on one of p.8's two sides.", nameof(teamSide));
            }

            HeroId = heroId;
            TeamSide = teamSide;
            IsInsideBattleZone = isInsideBattleZone;
        }

        public MinionBattleRuleDeclaration Declaration { get; }
        public HeroId HeroId { get; }
        public TeamSide TeamSide { get; }
        public bool IsInsideBattleZone { get; }

        public RuleObject Unit => RuleObject.ForHero(HeroId);
    }

    /// <summary>
    /// A rule that changes how p.8's Minion Battle counts or removes, isolated from ordinary
    /// action/effect dispatch.
    ///
    /// <para>It is deliberately not an <c>IEffectCapabilityHandler</c>. A capability is reached by an
    /// effect node inside an action a hero performs; this is reached by the end of round, which nobody
    /// performs, and it answers two questions the effect frame has no vocabulary for - how many minions
    /// a side has, and what happens instead of a removal. Milestone 8.19's audit named it the roster's
    /// one controlled isolated handler for exactly that reason.</para>
    ///
    /// <para>Both members are pure. The battle applies what they answer; a handler that could remove,
    /// award, or push would be a second Minion Battle.</para>
    /// </summary>
    public interface IMinionBattleRuleHandler
    {
        MinionBattleRuleReference Reference { get; }

        /// <summary>What this participant adds to its side's count right now.</summary>
        MinionBattleStrength Contribute(MinionBattleParticipant participant);

        /// <summary>What happens instead when this participant's own unit would be removed.</summary>
        MinionBattleRemovalSubstitution SubstituteRemoval(MinionBattleParticipant participant);
    }

    /// <summary>
    /// Brogan's One Man Army as a rule: "During minion battle you count as a heavy minion; if you would
    /// be removed, lose the push instead."
    ///
    /// The two sentences are the two members, and both are conditioned on the one fact p.8 makes them
    /// conditional on: the hero has to be in the Battle Zone to be one of the minions in it. A hero
    /// standing elsewhere adds nothing and substitutes nothing, which is the same answer the page gives
    /// a minion standing elsewhere.
    ///
    /// The handler reads its own declaration rather than restating it, so a second card printing the
    /// same rule - or the same card at a different strength - is a declaration change rather than a
    /// second handler.
    /// </summary>
    public sealed class OneManArmyMinionBattleHandler : IMinionBattleRuleHandler
    {
        public static MinionBattleRuleReference RuleReference { get; } =
            new MinionBattleRuleReference("one-man-army-minion-battle", 1);

        public MinionBattleRuleReference Reference => RuleReference;

        public MinionBattleStrength Contribute(MinionBattleParticipant participant) =>
            participant.IsInsideBattleZone
                ? participant.Declaration.Strength
                : MinionBattleStrength.None;

        public MinionBattleRemovalSubstitution SubstituteRemoval(MinionBattleParticipant participant) =>
            participant.IsInsideBattleZone
                ? participant.Declaration.RemovalSubstitution
                : MinionBattleRemovalSubstitution.None;
    }

    /// <summary>
    /// The registered Minion-Battle rules, keyed by rule identity.
    ///
    /// A declaration names a reference and the catalog answers with the handler, so the battle never
    /// switches on anything: it asks the catalog for the rule the trusted definition named. A reference
    /// nothing registers is a content error the catalog reports, in the shape
    /// <c>EffectCapabilityCatalog</c> already uses for effect capabilities.
    /// </summary>
    public sealed class MinionBattleRuleCatalog
    {
        private readonly Dictionary<MinionBattleRuleReference, IMinionBattleRuleHandler> handlers;

        public MinionBattleRuleCatalog(IEnumerable<IMinionBattleRuleHandler> registered)
        {
            var declared = (registered ?? throw new ArgumentNullException(nameof(registered))).ToArray();
            if (declared.Any(handler => handler == null))
            {
                throw new ArgumentException("Registered Minion-Battle rules must be non-null.", nameof(registered));
            }

            if (declared.Select(handler => handler.Reference).Distinct().Count() != declared.Length)
            {
                throw new ArgumentException(
                    "Each Minion-Battle rule identity is registered once.", nameof(registered));
            }

            handlers = declared.ToDictionary(handler => handler.Reference);
            References = new ReadOnlyCollection<MinionBattleRuleReference>(
                declared.Select(handler => handler.Reference)
                    .OrderBy(reference => reference.RuleId, StringComparer.Ordinal)
                    .ThenBy(reference => reference.Version)
                    .ToArray());
        }

        /// <summary>Every Minion-Battle rule this build implements.</summary>
        public static MinionBattleRuleCatalog Rulebook { get; } = new MinionBattleRuleCatalog(
            new IMinionBattleRuleHandler[] { new OneManArmyMinionBattleHandler() });

        public IReadOnlyList<MinionBattleRuleReference> References { get; }

        public bool TryGet(MinionBattleRuleReference reference, out IMinionBattleRuleHandler handler) =>
            handlers.TryGetValue(reference, out handler);

        public IMinionBattleRuleHandler Get(MinionBattleRuleReference reference)
        {
            if (!TryGet(reference, out var handler))
            {
                throw new InvalidOperationException(
                    $"Minion-Battle rule '{reference}' is not registered in this build.");
            }

            return handler;
        }
    }
}
