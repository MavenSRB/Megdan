using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's Smoke bomb - the token itself, the six operations her faces perform on it,
    /// and the one property that makes it more than an obstacle. Milestone 8.61a, the first block of
    /// the nineteenth hero.
    ///
    /// <para><b>What the source says.</b> Her Silver basic prints the whole of it: "Place a [Smoke
    /// bomb] smoke bomb token in radius; enemy heroes cannot target you or another unit if the smoke
    /// bomb token is on a straight line between that enemy hero and their target." Five more of her
    /// faces name the same piece - Viper and Cobra Stance swap with it "in radius", Poof!, Vanish and
    /// Ruse "in range", and Cobra and Ruse may then place it again. See
    /// <c>Docs/AI/Rules/HERO_MIN.md</c>, MI-Q1 and MI-Q2.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <c>MatchContentCatalogs</c> names no Min content, so no
    /// configured match can put this token on a board and nothing reachable can start these
    /// capabilities. That is milestone 7.9's gate rather than an oversight - the registered content
    /// list is serialized, so registering a component before a dealt card can place it moves the pinned
    /// list, every snapshot's bytes and every corpus digest while the content itself never moved.
    /// 8.61j is the block that flips it; until then <see cref="SmokeBombDeferredHandlers"/> is the
    /// whole list and <c>MinSmokeBombTests</c> asserts the deferral from both sides.</para>
    /// </summary>
    public static partial class MinComponentContent
    {
        /// <summary>
        /// MI-Q1's working count, held for the user: <b>one</b> Smoke bomb.
        ///
        /// <para>Her faces point the same way her box is expected to. Every sentence that names it says
        /// "a [Smoke bomb] Smoke bomb" or "the Smoke bomb" in the singular, no face ever places two,
        /// and Cobra's and Ruse's re-place read "<em>the</em> Smoke bomb" - the one that was just
        /// swapped with. The Tabletopia manifest holds a single <c>Smoke_Bomb</c> element (MI-06).
        /// p.18's short-supply sentence covers any shortfall if the user answers otherwise, and with a
        /// supply of one the second Place of a standing bomb is the relocation this file's own place
        /// arm performs rather than a reclaim.</para>
        /// </summary>
        public const int SmokeBombSupplyCount = 1;

        /// <summary>
        /// The applicability her token property declares: p.19's own lifetime sentence, "Once a token
        /// is placed, it retains all its properties until that token is removed." The same string
        /// Tali's three roles declare, because it is the same page's rule.
        /// </summary>
        public const string SmokeBombApplicabilityKind = "WhileTokenPlaced";

        /// <summary>
        /// The piece's identity. It carries the <c>-token</c> suffix her card does not, because her
        /// Silver basic is also called Smoke Bomb and takes <c>min-smoke-bomb</c> as its own
        /// <see cref="CardDefinitionId"/>: the two identities are different types and could not be
        /// confused by the engine, but this project has twice paid for an id that read the same as
        /// another, and a suffix costs nothing.
        /// </summary>
        public static TokenDefinitionId SmokeBomb { get; } =
            new TokenDefinitionId("min-smoke-bomb-token");

        /// <summary>
        /// The Smoke bomb, with the one property the Silver's second clause prints.
        ///
        /// <para><b>The screen is declared on the token and read at one seam.</b>
        /// <see cref="RuleModificationKind.EnemyHeroesCannotTargetThroughThisTokensSpace"/> records why
        /// it is addressed to <see cref="TokenPropertyAffectedObject.TheTokenItself"/> rather than to
        /// the enemy heroes the sentence names, and why it is neither an immunity nor a restriction;
        /// <c>TargetQuery.IsPrevented</c> is the only thing that consults it, which is what makes every
        /// attack target, push subject, swap subject and card-text unit choice inherit it at once
        /// (MI-Q2).</para>
        ///
        /// <para><b>Its lifetime is p.19's baseline and her text says nothing otherwise</b>, so it is
        /// <see cref="TokenRemovalCondition.EndOfRoundCleanup"/>: "Unless the card text says otherwise,
        /// tokens are removed at the end of round." The same page's next sentence is the negative that
        /// needs no declaration - "Tokens are not removed when the hero who placed them is defeated" -
        /// and it is structural here, because a placed token's controller is provenance rather than
        /// lifetime. The screen therefore keeps screening while Min is off the board, which is what the
        /// page says and what <c>MinSmokeBombTests</c> measures.</para>
        ///
        /// <para>It shares no supply group: her mines do (8.61d), and the bomb is its own piece.</para>
        /// </summary>
        public static TokenDefinition SmokeBombDefinition { get; } = new TokenDefinition(
            SmokeBomb,
            1,
            "Smoke bomb",
            SmokeBombSupplyCount,
            new[]
            {
                new TokenPropertyDefinition(
                    "targeting-screen",
                    "enemy-hero-targeting-screen",
                    SmokeBombApplicabilityKind,
                    RuleModification.ForEnemyHeroTargetingScreen(),
                    TokenPropertyAffectedObject.TheTokenItself)
            },
            TokenRemovalCondition.EndOfRoundCleanup);

        /// <summary>Whether her one bomb is on the board - the Silver's place-or-relocate branch.</summary>
        public static EffectCapabilityId SmokeBombPresent { get; } =
            new EffectCapabilityId("min-smoke-bomb-present");

        /// <summary>Where it stands, unbounded - the subject of a re-place after a swap.</summary>
        public static EffectCapabilityId SmokeBombSpace { get; } =
            new EffectCapabilityId("min-smoke-bomb-space");

        /// <summary>Where it stands, when it is within the performing card's printed Range (Poof!, Vanish, Ruse).</summary>
        public static EffectCapabilityId SmokeBombInRange { get; } =
            new EffectCapabilityId("min-smoke-bomb-in-range");

        /// <summary>Where it stands, when it is within the performing card's printed Radius (Viper, Cobra).</summary>
        public static EffectCapabilityId SmokeBombInRadius { get; } =
            new EffectCapabilityId("min-smoke-bomb-in-radius");

        /// <summary>The supply Place her Silver performs, reaching its printed Radius (p.9, p.16).</summary>
        public static EffectCapabilityId PlaceSmokeBombInRadius { get; } =
            new EffectCapabilityId("min-place-smoke-bomb-in-radius");

        /// <summary>
        /// The supply Place a face reaching by printed Range would perform. Declared with its Radius
        /// twin because <see cref="SyntheticTokenCapabilities.PlaceHandler"/> binds the channel into
        /// the handler rather than taking it on the wire, and a Range-reaching supply Place is a
        /// different capability from a Radius-reaching one for the same reason a definition-bound token
        /// handler is: a payload can then name a node but never a different reach.
        /// </summary>
        public static EffectCapabilityId PlaceSmokeBombInRange { get; } =
            new EffectCapabilityId("min-place-smoke-bomb-in-range");

        public static EffectCapabilityReference SmokeBombPresentV1 =>
            new EffectCapabilityReference(SmokeBombPresent, 1);

        public static EffectCapabilityReference SmokeBombSpaceV1 =>
            new EffectCapabilityReference(SmokeBombSpace, 1);

        public static EffectCapabilityReference SmokeBombInRangeV1 =>
            new EffectCapabilityReference(SmokeBombInRange, 1);

        public static EffectCapabilityReference SmokeBombInRadiusV1 =>
            new EffectCapabilityReference(SmokeBombInRadius, 1);

        public static EffectCapabilityReference PlaceSmokeBombInRadiusV1 =>
            new EffectCapabilityReference(PlaceSmokeBombInRadius, 1);

        public static EffectCapabilityReference PlaceSmokeBombInRangeV1 =>
            new EffectCapabilityReference(PlaceSmokeBombInRange, 1);

        /// <summary>
        /// Her one token definition, for the offline content fixtures that compose catalogs of their
        /// own. It reaches no production catalog until 8.61j - see the type's own remarks.
        /// </summary>
        public static IEnumerable<TokenDefinition> SmokeBombTokens() => new[] { SmokeBombDefinition };

        /// <summary>
        /// The six Smoke bomb operations, every one of them deferred.
        ///
        /// <para>Each is one capability per operation per token, which is the pattern milestones 8.23
        /// and 8.35z32 set for a hero's own components: the definition is bound to the handler rather
        /// than named on the wire, so a payload can name a node but never a different token. None is a
        /// new handler TYPE - <c>OwnedTokenFactHandler</c> is milestone 8.35z32's, showcased on the
        /// synthetic hazard, and <c>PlaceHeroTokenHandler</c> is milestone 8.37's, showcased as
        /// <c>showcase-place-hero-token@1</c> - so none of the six owes a showcase consumer at the
        /// dealing; what they owe is registration.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> SmokeBombDeferredHandlers() =>
            new[]
            {
                SyntheticOwnedTokenCapabilities.PresenceHandler(
                    SmokeBombPresentV1, SmokeBombDefinition),
                SyntheticOwnedTokenCapabilities.SpaceHandler(
                    SmokeBombSpaceV1, SmokeBombDefinition),
                SyntheticOwnedTokenCapabilities.SpaceHandler(
                    SmokeBombInRangeV1, SmokeBombDefinition, PrintedValueChannel.Range),
                SyntheticOwnedTokenCapabilities.SpaceHandler(
                    SmokeBombInRadiusV1, SmokeBombDefinition, PrintedValueChannel.Radius),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceSmokeBombInRadiusV1, SmokeBombDefinition, PrintedValueChannel.Radius),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceSmokeBombInRangeV1, SmokeBombDefinition, PrintedValueChannel.Range)
            };

        /// <summary>
        /// Nothing, until 8.61j. The member exists so the fixture that asserts the deferral from both
        /// sides asks a question rather than failing to compile, and so a later "registered or
        /// deferred" sweep that concatenates both lists keeps its shape - the arrangement
        /// <c>BainComponentContent</c> left behind after its own dealing moved four handlers across.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> SmokeBombRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
    }
}
