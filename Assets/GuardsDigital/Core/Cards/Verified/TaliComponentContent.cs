using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Tali's tokens - the first tokens any verified hero in this project actually has. Milestone
    /// 8.35z32.
    ///
    /// <para><b>What the source says.</b> Ledger question 14 makes a hero's card faces the definition
    /// of that hero's tokens, and every hero verified before Tali named none, so under the same ruling
    /// they had none. Her Green line names two: an <c>[Ice]</c> token placed by Glacial Barrier, Wall
    /// of Frost and Pack Ice, and a <c>[Totem]</c> token placed by Ancestral Totem and Venerated Totem.
    /// The glyphs are distinct and each is pixel-consistent across the cards naming it. What the faces
    /// do <em>not</em> say is how many exist, and p.18 makes that number a rules input rather than
    /// flavour - which is why RULING 44 had to settle it.</para>
    ///
    /// <para><b>RULING 44, and the shape the whole file follows from it.</b> The user's answer was that
    /// she has "3 tokens... They are the same token, it just depends what you use them for (depending
    /// on which card you take for level up)". So there is <b>one physical supply of three</b> and the
    /// <b>placing card decides each copy's role</b>. That is why the three roles below are three
    /// <see cref="TokenDefinition"/>s - their printed properties differ, and p.19 gives a placed copy
    /// the properties it was placed with, "until that token is removed" - while all three name the same
    /// <see cref="TokenDefinition.SupplyGroupId"/>, so the pile they draw from is one pile of three
    /// rather than three piles of three. It also explains the single Ice glyph on her profile card's
    /// Tokens role icon: there is one physical token to draw.</para>
    ///
    /// <para><b>The overflow reaches across roles, and that is the corollary the ruling drew out
    /// loud.</b> p.18: "If you need to place more tokens than are available in the supply, first remove
    /// up to that number of tokens of the same type from the board, if able." With one shared type, a
    /// Pack Ice needing three can pull back a standing totem and a Totem placement can eat standing
    /// Ice. <c>ComponentDefinitionCatalog.ReclaimableForPlacement</c> is where that sentence is
    /// answered, and it answers over the group.</para>
    ///
    /// <para><b>Two totem definitions, and RULING 24 is why they can both exist here without ever
    /// meeting.</b> Ancestral Totem protects a friendly <em>melee</em> minion and Venerated Totem
    /// protects any friendly minion and adds <c>Totem tokens are immune</c>. The user's correction is
    /// that p.15's upgrade <em>replaces</em> the lower tier - "You can only have one green active. If
    /// you level up to tier 3 you lose tier 2" - so the two cards never coexist in a deck and every
    /// totem on the board in a round carries the one live card's rule. The per-instance-properties
    /// question the inventory raised as T-06 cannot arise. Both definitions ship because both faces are
    /// printed; only one is ever reachable from a given deck.</para>
    ///
    /// <para><b>RULING 6 is what makes "a totem" mean what it says.</b> Neither Totem face scopes its
    /// protective sentence to the token it just placed - both say "adjacent to <b>a</b> totem" - and
    /// the user's answer was that totems are a token <em>type</em> and Tali's are the only totems, so
    /// "a totem" means any totem on the board and another hero's tokens are a different type that never
    /// satisfies it. <see cref="TotemDefinitionIds"/> is that type identity, named once here so a
    /// reader of either card finds the same set.</para>
    ///
    /// <para><b>What is deliberately absent.</b> No art, no colour, no sprite. RULING 44's white
    /// token art and its runtime team colour are presentation items owed by the dealing phase, and a
    /// definition here is its supply, its rules and its lifetime.</para>
    /// </summary>
    public static class TaliComponentContent
    {
        /// <summary>
        /// RULING 44's number: three, and one pile. It is declared once and shared by all three role
        /// definitions, which the catalog refuses to let disagree.
        /// </summary>
        public const int SupplyCount = 3;

        /// <summary>
        /// The one physical pile all three roles draw from - the identity that makes p.18's "tokens of
        /// the same type" reach across Ice and Totem alike.
        /// </summary>
        public const string SupplyGroupId = "tali-spirit-tokens";

        /// <summary>
        /// The applicability every one of her token properties declares: p.19's own lifetime sentence,
        /// "Once a token is placed, it retains all its properties until that token is removed."
        /// </summary>
        public const string ApplicabilityKind = "WhileTokenPlaced";

        public static TokenDefinitionId Ice { get; } = new TokenDefinitionId("tali-ice");

        /// <summary>Ancestral Totem's totem: the melee-minion save, and no immunity.</summary>
        public static TokenDefinitionId AncestralTotem { get; } =
            new TokenDefinitionId("tali-totem-ancestral");

        /// <summary>Venerated Totem's totem: any friendly minion, and immune.</summary>
        public static TokenDefinitionId VeneratedTotem { get; } =
            new TokenDefinitionId("tali-totem-venerated");

        /// <summary>
        /// The Ice token. Its one property is the aura Glacial Barrier, Wall of Frost and Pack Ice all
        /// print word for word: "Enemy heroes have -1 Initiative for every Ice token they are adjacent
        /// to."
        ///
        /// <para><b>The counting is emergent and nothing counts.</b> The property contributes -1 to
        /// each enemy hero standing beside <em>this</em> token, so a hero adjacent to three Ice tokens
        /// is reached by three separate sources and is at -3. Writing it as a counted query would have
        /// made each of the three tokens contribute -3.</para>
        ///
        /// <para><b>It is the first board-sourced modifier on Initiative, and RULING 25 says it has no
        /// floor.</b> Values reach 0 and below, and p.7's ordering and its Tie Breaker compare the
        /// modified values - the same no-floor arithmetic ruled for Dodger's Weakness. p.7 re-checks
        /// Initiative during the turn ("After the player who acted first has resolved their card, check
        /// the Initiative again"), so a token placed mid-round changes who acts next; because nothing
        /// here is stored and the adjacency is re-measured every time the live set is derived, that
        /// re-check sees the aura without anything having to tell it to.</para>
        /// </summary>
        public static TokenDefinition IceDefinition { get; } = new TokenDefinition(
            Ice,
            1,
            "Ice",
            SupplyCount,
            new[]
            {
                new TokenPropertyDefinition(
                    "initiative-aura",
                    "value-modifier",
                    ApplicabilityKind,
                    RuleModification.ForValue(PrintedValueChannel.Initiative, -1),
                    TokenPropertyAffectedObject.EnemyHeroesAdjacentToTheToken)
            },
            // p.19's baseline, and neither face says otherwise: "Unless the card text says otherwise,
            // tokens are removed at the end of round."
            TokenRemovalCondition.EndOfRoundCleanup,
            SupplyGroupId);

        /// <summary>
        /// Ancestral Totem's totem (Green Tier II): "If a friendly melee minion adjacent to a totem
        /// would be defeated, remove the totem instead."
        /// </summary>
        public static TokenDefinition AncestralTotemDefinition { get; } = Totem(
            AncestralTotem,
            TokenPropertyAffectedObject.FriendlyMeleeMinionsAdjacentToTheToken,
            immune: false);

        /// <summary>
        /// Venerated Totem's totem (Green Tier III): the same save widened to any friendly minion, plus
        /// the sentence that makes this the first immune token in the game.
        /// </summary>
        public static TokenDefinition VeneratedTotemDefinition { get; } = Totem(
            VeneratedTotem,
            TokenPropertyAffectedObject.FriendlyMinionsAdjacentToTheToken,
            immune: true);

        /// <summary>
        /// Every token definition this hero's content registers - the three roles of her one pile.
        /// </summary>
        public static IEnumerable<TokenDefinition> Tokens() => new[]
        {
            IceDefinition, AncestralTotemDefinition, VeneratedTotemDefinition
        };

        /// <summary>
        /// The definitions that are totems, which is what RULING 6's "a totem" names. Read by both
        /// Totem faces and by nothing else: another hero's tokens are a different type and never
        /// satisfy the word.
        /// </summary>
        public static IReadOnlyList<TokenDefinitionId> TotemDefinitionIds { get; } =
            System.Array.AsReadOnly(new[] { AncestralTotem, VeneratedTotem });

        /// <summary>Whether one placed token is a totem in RULING 6's sense.</summary>
        public static bool IsATotem(PlacedToken token) =>
            token != null && TotemDefinitionIds.Contains(token.Definition.Id);

        /// <summary>
        /// One Totem role: the save its card prints, and - on Venerated Totem alone - the immunity.
        ///
        /// <para><b>The save replaces the would-be Defeat, and its price is the totem.</b> p.10's
        /// Mandatory and Optional Steps rule is the whole argument: "Steps which include 'you may',
        /// 'up to' and 'if able' are non-mandatory, all other steps are mandatory", and this sentence
        /// carries none of the three. So nobody is asked, nobody can decline, and what pays is the
        /// specific piece whose property is speaking -
        /// <see cref="RemovalPreventionCost.RemoveTheSourceToken"/>, which is why that cost kind is a
        /// second <em>shape</em> and not just a second price beside Brogan's Bolster family.</para>
        ///
        /// <para><b>The immunity is p.18's unqualified one, and RULING 26 is where its edge is.</b>
        /// "Totem tokens are immune" names no activity, so it is <see cref="RuleActivity.All"/> - which
        /// p.18 states as "you cannot target that unit, and that unit is not affected by your actions".
        /// The ruling draws the line the card leaves implicit: it blocks <b>player actions including
        /// p.13's Clear</b>, so an opponent standing beside a totem can no longer simply remove it,
        /// while <b>system-level removals still work</b> - p.8's lane push clearing a spawn point and
        /// p.8's end-of-round cleanup are not actions anybody performs, ask no immunity question, and
        /// remain the counterplay. That boundary is a fact about which code paths consult
        /// <c>ImmunityQuery</c>, and this milestone's tests measure it rather than asserting it.</para>
        ///
        /// <para>Ancestral Totem prints no immunity sentence, so it declares none - the standing
        /// cards-are-the-definition ruling, applied to the difference between two faces that are
        /// otherwise one card.</para>
        /// </summary>
        private static TokenDefinition Totem(
            TokenDefinitionId id, TokenPropertyAffectedObject savedMinions, bool immune)
        {
            var properties = new List<TokenPropertyDefinition>
            {
                new TokenPropertyDefinition(
                    "minion-save",
                    "defeat-replacement",
                    ApplicabilityKind,
                    RuleModification.ForDefeatReplacement(
                        RemovalPreventionCost.RemoveTheSourceToken),
                    savedMinions)
            };

            if (immune)
            {
                properties.Add(new TokenPropertyDefinition(
                    "token-immunity",
                    "immunity",
                    ApplicabilityKind,
                    RuleModification.ForImmunity(RuleActivity.All),
                    TokenPropertyAffectedObject.TheTokenItself));
            }

            return new TokenDefinition(
                id,
                2,
                "Totem",
                SupplyCount,
                properties,
                TokenRemovalCondition.EndOfRoundCleanup,
                SupplyGroupId);
        }
    }
}
