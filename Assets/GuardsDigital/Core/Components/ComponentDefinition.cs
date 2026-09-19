using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// One immutable rule supplied by a placed token for exactly as long as that token remains on the
    /// board. This is definition content: a snapshot names the token definition revision, and the
    /// trusted component catalog supplies these semantics again on restore.
    /// </summary>
    public sealed class TokenPropertyDefinition
    {
        public TokenPropertyDefinition(
            string propertyId,
            string ruleKind,
            string applicabilityKind,
            RuleModification modification = null,
            TokenPropertyAffectedObject affectedObject = TokenPropertyAffectedObject.Controller)
        {
            if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(ruleKind) ||
                string.IsNullOrWhiteSpace(applicabilityKind))
            {
                throw new ArgumentException(
                    "A token property requires stable identity, rule, and applicability.");
            }

            if (!Enum.IsDefined(typeof(TokenPropertyAffectedObject), affectedObject))
            {
                throw new ArgumentException(
                    "A token property names one of the declared affected objects.",
                    nameof(affectedObject));
            }

            PropertyId = propertyId;
            RuleKind = ruleKind;
            ApplicabilityKind = applicabilityKind;
            Modification = modification;
            AffectedObject = affectedObject;
        }

        public string PropertyId { get; }

        public string RuleKind { get; }

        public string ApplicabilityKind { get; }

        /// <summary>
        /// The typed rule this property supplies, or <c>null</c> for a property that only declares its
        /// source and lifetime.
        ///
        /// Milestone 6.3 established the declaration without a concrete rule because none was verified
        /// yet; milestone 6.8 supplies the general typed families it can now carry.
        /// </summary>
        public RuleModification Modification { get; }

        /// <summary>
        /// Who or what this property's rule is addressed to. Milestone 8.35z32.
        ///
        /// <para><b>Why the field exists now and not before.</b> Milestone 6.8 recorded the previous
        /// answer and the condition for changing it in one sentence: "The affected object is the token's
        /// controller ... a token property that modifies something else needs the verified text that
        /// says so, not a member added ahead of it." Tali's Green line is that text, three times over,
        /// and none of the three is about her: <c>Enemy heroes have -1 Initiative for every Ice token
        /// they are adjacent to</c>, <c>If a friendly minion adjacent to a totem would be defeated,
        /// remove the totem instead</c>, and <c>Totem tokens are immune</c>. The default is the old
        /// meaning written down, so every declaration made before this one says exactly what it said.
        /// </para>
        ///
        /// <para><b>Every member is anchored on the token, and that is the point.</b> p.19 gives a
        /// placed token its own lifetime - "Once a token is placed, it retains all its properties until
        /// that token is removed" - so a property that reaches other objects reaches the ones standing
        /// beside <em>it</em>, not the ones standing beside its controller. That is also what makes the
        /// Ice sentence come out right without any counting vocabulary: each Ice token contributes -1 to
        /// the enemy heroes adjacent to it, so a hero adjacent to three of them is reached by three
        /// separate sources and is at -3. "For every Ice token they are adjacent to" is the emergent
        /// reading of per-token sources rather than a query anything has to compute.</para>
        /// </summary>
        public TokenPropertyAffectedObject AffectedObject { get; }
    }

    /// <summary>
    /// What one token property's rule is addressed to, as definition content. Milestone 8.35z32.
    ///
    /// <para>Each member is a printed sentence rather than a shape someone thought a token might want.
    /// The list is exactly as long as Tali's Green line makes it, and a member for an unprinted reading
    /// would be an invented rule - the same discipline <see cref="TokenRemovalCondition"/> keeps.</para>
    /// </summary>
    public enum TokenPropertyAffectedObject
    {
        /// <summary>
        /// The hero controlling the token, which is what every declaration written before milestone
        /// 8.35z32 meant and what the derived <c>ContinuousRuleSource</c> already names.
        /// </summary>
        Controller = 0,

        /// <summary>
        /// Heroes on the team opposing the token's controller, standing adjacent to the token - the
        /// Ice sentence Glacial Barrier, Wall of Frost and Pack Ice all print word for word.
        ///
        /// <para>RULING 25 settles what the rule does at the bottom: there is <b>no floor</b>. A hero's
        /// modified Initiative reaches 0 and below, and p.7's ordering and its Tie Breaker compare the
        /// modified values - which is consistent with the no-floor Attack arithmetic ruled for Dodger's
        /// Weakness. p.7 re-checks Initiative during the turn, so a token placed mid-round changes who
        /// acts next; this is the first board-sourced modifier on the value that orders turns.</para>
        /// </summary>
        EnemyHeroesAdjacentToTheToken = 1,

        /// <summary>
        /// The placed token itself - Venerated Totem's <c>Totem tokens are immune.</c>, and the only
        /// declaration in the project addressed to a non-unit. See <c>RuleObjectKind.Token</c>.
        /// </summary>
        TheTokenItself = 2,

        /// <summary>
        /// Minions on the token controller's own team, standing adjacent to the token - Venerated
        /// Totem's <c>If a friendly minion adjacent to a totem would be defeated, remove the totem
        /// instead.</c>
        /// </summary>
        FriendlyMinionsAdjacentToTheToken = 3,

        /// <summary>
        /// The same set narrowed to p.8's melee miniatures - Ancestral Totem's identical sentence with
        /// one extra printed word, <c>a friendly <b>melee</b> minion</c>.
        ///
        /// <para>It is a fifth member rather than a flag on the fourth because the two are two printed
        /// sentences on two cards, and RULING 24 means only one of them is ever live: p.15's upgrade
        /// replaces Tier II with Tier III, so Ancestral and Venerated Totem never coexist in a deck.
        /// Transcribing each face's own noun is what the standing cards-are-the-definition ruling asks
        /// for; whether the widening was deliberate is not this milestone's question.</para>
        /// </summary>
        FriendlyMeleeMinionsAdjacentToTheToken = 4
    }

    /// <summary>
    /// One immutable rule supplied by a marker for exactly as long as a hero holds it. Milestone 8.23.
    ///
    /// <para><b>The twin of <see cref="TokenPropertyDefinition"/>, and the one difference is the whole
    /// reason it is a separate type.</b> A token property affects the token's <em>controller</em>: p.19
    /// puts the token on the board and the hero who placed it keeps it, so "you" in its text is that
    /// hero. A marker property affects its <em>holder</em>: p.18 describes markers as components
    /// "usually given to other heroes", and Tigerclaw's Poisoned Dagger says so in as many words - "The
    /// hero with a poison marker has -1 Initiative, -1 Attack and -1 Defense". The affected object is
    /// the hero the marker was given to, who is normally the opponent of the hero who owns it.</para>
    ///
    /// <para>Sharing the token type and reinterpreting its affected object per call site would have made
    /// one declaration mean two things depending on where it was read, which is exactly the ambiguity
    /// <c>PrintedBlockCondition</c> was minted to remove elsewhere in this milestone.</para>
    /// </summary>
    public sealed class MarkerPropertyDefinition
    {
        public MarkerPropertyDefinition(
            string propertyId,
            string ruleKind,
            string applicabilityKind,
            RuleModification modification = null)
        {
            if (string.IsNullOrWhiteSpace(propertyId) || string.IsNullOrWhiteSpace(ruleKind) ||
                string.IsNullOrWhiteSpace(applicabilityKind))
            {
                throw new ArgumentException(
                    "A marker property requires stable identity, rule, and applicability.");
            }

            PropertyId = propertyId;
            RuleKind = ruleKind;
            ApplicabilityKind = applicabilityKind;
            Modification = modification;
        }

        public string PropertyId { get; }

        public string RuleKind { get; }

        public string ApplicabilityKind { get; }

        /// <summary>
        /// The typed rule this property supplies to the marker's holder, or <c>null</c> for a property
        /// that only declares its source and lifetime.
        /// </summary>
        public RuleModification Modification { get; }
    }

    /// <summary>
    /// When a marker leaves its holder and goes back to the supply.
    ///
    /// <see cref="GeneralCleanup"/> is the rulebook's baseline and currently the only member: markers
    /// return at the end of every round (p.8, "Remove Tokens, Return Markers") and every marker held by
    /// a defeated hero is returned (p.13). <c>STATE_CAPABILITIES.md</c> records that exact effects may
    /// add an *earlier* return condition but cannot weaken those two checkpoints, so this enum is the
    /// declared seam a later verified card extends. It is deliberately not widened ahead of that text:
    /// an invented member would be an invented rule.
    ///
    /// Milestone 6.2 records the condition and implements the return transition. It wires no automatic
    /// trigger to either checkpoint; end-of-round cleanup belongs to milestone 6.12.
    /// </summary>
    public enum MarkerReturnCondition
    {
        None = 0,

        /// <summary>Returned at end of round, and when the holding hero is defeated.</summary>
        GeneralCleanup = 1
    }

    /// <summary>
    /// When a placed token leaves the board and goes back to the supply on its own.
    ///
    /// The token twin of <see cref="MarkerReturnCondition"/>, and deliberately narrower than it looks.
    /// Rulebook p.19 states the rule with its condition attached - "Unless the card text says otherwise,
    /// tokens are removed at the end of round (See Page 8)" - so <see cref="EndOfRoundCleanup"/> is the
    /// baseline every token declares and the "otherwise" is a member added with the verified card text
    /// that requires it, never ahead of it. Dragon Bond supplies the first explicit override.
    ///
    /// The same page's next sentence is the negative this enum must not be read as covering: "Tokens are
    /// not removed when the hero who placed them is defeated." That is already structural - a token's
    /// owner is provenance rather than lifetime, which <see cref="PlacedToken"/> says outright - so the
    /// p.13 defeat checkpoint returns markers and consults this enum not at all.
    ///
    /// Milestone 6.12 both declares this and wires the cleanup that reads it.
    /// </summary>
    public enum TokenRemovalCondition
    {
        None = 0,

        /// <summary>Removed to supply at the end of every round (p.8, "Remove Tokens, Return Markers").</summary>
        EndOfRoundCleanup = 1,

        /// <summary>Printed override: retained at round end, but still removable by ordinary token operations.</summary>
        RetainedAtEndOfRound = 2
    }

    /// <summary>
    /// The immutable, catalog-owned description of one kind of token: its identity and how many exist.
    ///
    /// The supply count is content, not runtime state. Rulebook p.19 says "tokens are intentionally
    /// limited", and p.18 makes the limit load-bearing - running out is what forces the reclamation
    /// choice rather than a silent failure. Holding the total here and deriving what remains from the
    /// board means the two can never disagree: there is no second number to keep in step.
    ///
    /// Milestone 6.3 adds the explicit property collection. A property is not an Active effect and has
    /// no card/hero cancellation condition; a placed instance exposes it as a token-owned continuous
    /// source until that instance is removed.
    /// </summary>
    public sealed class TokenDefinition
    {
        public TokenDefinition(
            TokenDefinitionId definitionId,
            int revision,
            string displayName,
            int supplyCount,
            IEnumerable<TokenPropertyDefinition> properties,
            TokenRemovalCondition removalCondition,
            string supplyGroupId = null,
            TokenDefinition facedownFace = null)
        {
            Reference = new TokenDefinitionReference(definitionId, revision);

            if (supplyGroupId != null && string.IsNullOrWhiteSpace(supplyGroupId))
            {
                throw new ArgumentException(
                    "A shared supply is named or absent; a blank name is neither.",
                    nameof(supplyGroupId));
            }

            if (facedownFace != null)
            {
                if (facedownFace.HidesItsKind)
                {
                    throw new ArgumentException(
                        "A facedown face names no kind of its own, so it cannot itself hide one; " +
                        "a chain of substitutions would leave 'what does another seat see' with two " +
                        "answers.",
                        nameof(facedownFace));
                }

                if (facedownFace.Id.Equals(definitionId))
                {
                    throw new ArgumentException(
                        "A definition cannot be its own facedown face.", nameof(facedownFace));
                }
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A token definition requires a display name.", nameof(displayName));
            }

            if (supplyCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(supplyCount),
                    "A token definition's supply is finite and non-empty; a token type nobody can place is not content.");
            }

            var materializedProperties = (properties ?? throw new ArgumentNullException(nameof(properties)))
                .ToArray();
            if (materializedProperties.Any(property => property == null) ||
                materializedProperties.Select(property => property.PropertyId)
                    .Distinct(StringComparer.Ordinal).Count() != materializedProperties.Length)
            {
                throw new ArgumentException(
                    "Token properties must be non-null and have unique IDs inside their definition.",
                    nameof(properties));
            }

            if (removalCondition != TokenRemovalCondition.EndOfRoundCleanup &&
                removalCondition != TokenRemovalCondition.RetainedAtEndOfRound)
            {
                throw new ArgumentException(
                    "A token definition requires a supported explicit removal condition; retaining " +
                    "a token at round end must be declared by its printed rules.",
                    nameof(removalCondition));
            }

            DisplayName = displayName;
            SupplyCount = supplyCount;
            Properties = new ReadOnlyCollection<TokenPropertyDefinition>(materializedProperties);
            RemovalCondition = removalCondition;
            SupplyGroupId = supplyGroupId;
            FacedownFace = facedownFace;
        }

        public TokenDefinitionReference Reference { get; }

        public TokenDefinitionId Id => Reference.DefinitionId;

        public int Revision => Reference.Revision;

        public string DisplayName { get; }

        /// <summary>How many of this token exist in the match, in total, placed or not.</summary>
        public int SupplyCount { get; }

        /// <summary>Persistent token-owned rules, queried as continuous sources while placed.</summary>
        public IReadOnlyList<TokenPropertyDefinition> Properties { get; }

        /// <summary>When this token leaves the board on its own (p.19's "unless the card text says otherwise").</summary>
        public TokenRemovalCondition RemovalCondition { get; }

        /// <summary>
        /// The physical pile this definition draws from, shared with every other definition naming the
        /// same group, or <c>null</c> for a token whose supply is its own. Milestone 8.35z32.
        ///
        /// <para><b>Why one hero's tokens needed this and no hero's did before.</b> RULING 44 settles
        /// what Tali actually has: "She has 3 tokens... They are the same token, it just depends what
        /// you use them for (depending on which card you take for level up)." One physical supply of
        /// three, and the placing card decides whether a given copy is currently Ice or a Totem. Her
        /// three roles are three definitions here, because their printed properties differ and
        /// <see cref="Properties"/> is what a placed copy carries - but a per-definition supply of three
        /// would put nine tokens in a box that holds three.</para>
        ///
        /// <para><b>What the group changes, and it is exactly one question.</b> p.18's supply sentence -
        /// "If you need to place more tokens than are available in the supply, first remove up to that
        /// number of tokens of the same type from the board, if able" - is answered against the group
        /// rather than the definition. RULING 44 draws the corollary out loud: with one shared type the
        /// overflow reaches <b>across roles</b>, so a Pack Ice needing three can pull back a standing
        /// totem and a Totem placement can eat standing Ice. Every group member declares the same
        /// <see cref="SupplyCount"/> - the catalog refuses a group that disagrees with itself, because
        /// two numbers for one pile is the drift this type exists to make impossible.</para>
        /// </summary>
        public string SupplyGroupId { get; }

        /// <summary>
        /// Whether this definition shares its supply with others rather than owning one alone.
        /// </summary>
        public bool SharesASupply => !string.IsNullOrWhiteSpace(SupplyGroupId);

        /// <summary>
        /// The definition every seat but the controller's sees in place of this one, or <c>null</c> for
        /// a token whose kind is public - which is every definition written before milestone 8.61d.
        ///
        /// <para><b>Why the fact is here.</b> Min the Dragonmonk's three Blue faces place their mines
        /// <b>facedown</b>, and blast and dud are two definitions of one pile. p.4 makes what a facedown
        /// component is hidden information, so the kind is knowable to the hero who placed it and to
        /// nobody else until the mine is removed. That is a statement about the piece, made by the card
        /// that prints the word, so it is declared by the content that needs it - the shape
        /// <see cref="SupplyGroupId"/> took at milestone 8.35z32 for the same reason, an optional
        /// trailing declaration every earlier definition leaves unset and is unchanged by.</para>
        ///
        /// <para><b>What reads it, and what does not.</b> It is a PROJECTION filter and never an
        /// authorization one (milestone 8.40f's rule): the authority, the Director's read model, the
        /// snapshot, the replay and the match's own rules always hold the real definition, and the
        /// substitution happens where one seat's view is composed. Nothing about the rules consults
        /// this - both kinds of mine print the same properties, so a masked board and the real one
        /// answer every movement, push, Place, Clear and cleanup question identically, which is what
        /// keeps a masked client's legality byte-for-byte the host's.</para>
        ///
        /// <para>A facedown face is an ordinary definition that names no kind. It is registered so a
        /// client can load it by id, and no handler ever places one.</para>
        /// </summary>
        public TokenDefinition FacedownFace { get; }

        /// <summary>
        /// Whether this definition's kind is hidden from every seat but its controller's - p.4's
        /// facedown component, and the question the seat projection asks.
        /// </summary>
        public bool HidesItsKind => FacedownFace != null;

        public override string ToString() =>
            SharesASupply
                ? $"{Reference} x{SupplyCount} (shared supply '{SupplyGroupId}')"
                : $"{Reference} x{SupplyCount}";
    }

    /// <summary>
    /// The immutable, catalog-owned description of one kind of marker.
    ///
    /// Structurally the token definition's twin, and deliberately so: rulebook p.18 describes markers as
    /// components given to heroes rather than placed on the board, which is a statement about where they
    /// go, not about whether they are finite or identified. What differs is that this definition carries
    /// a return condition and no board concept at all.
    /// </summary>
    public sealed class MarkerDefinition
    {
        public MarkerDefinition(
            MarkerDefinitionId definitionId,
            int revision,
            string displayName,
            int supplyCount,
            MarkerReturnCondition returnCondition,
            IEnumerable<MarkerPropertyDefinition> properties = null)
        {
            Reference = new MarkerDefinitionReference(definitionId, revision);

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A marker definition requires a display name.", nameof(displayName));
            }

            if (supplyCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(supplyCount),
                    "A marker definition's supply is finite and non-empty.");
            }

            if (returnCondition == MarkerReturnCondition.None)
            {
                throw new ArgumentException(
                    "A marker definition requires an explicit return condition; a marker that never returns " +
                    "would contradict the general cleanup the rulebook applies to every marker.",
                    nameof(returnCondition));
            }

            var materializedProperties =
                (properties ?? Enumerable.Empty<MarkerPropertyDefinition>()).ToArray();
            if (materializedProperties.Any(property => property == null) ||
                materializedProperties.Select(property => property.PropertyId)
                    .Distinct(StringComparer.Ordinal).Count() != materializedProperties.Length)
            {
                throw new ArgumentException(
                    "A marker definition's properties are non-null and uniquely identified.",
                    nameof(properties));
            }

            DisplayName = displayName;
            SupplyCount = supplyCount;
            ReturnCondition = returnCondition;
            Properties = new ReadOnlyCollection<MarkerPropertyDefinition>(materializedProperties);
        }

        public MarkerDefinitionReference Reference { get; }

        public MarkerDefinitionId Id => Reference.DefinitionId;

        public int Revision => Reference.Revision;

        public string DisplayName { get; }

        /// <summary>How many of this marker exist in the match, in total, held or not.</summary>
        public int SupplyCount { get; }

        public MarkerReturnCondition ReturnCondition { get; }

        /// <summary>
        /// The rules this marker supplies to whichever hero holds it, for as long as they hold it.
        /// Empty for a marker that carries no rule of its own. Milestone 8.23.
        /// </summary>
        public IReadOnlyList<MarkerPropertyDefinition> Properties { get; }

        public override string ToString() => $"{Reference} x{SupplyCount}";
    }
}
