using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.61g. The two printed faces of Min the Dragonmonk's whose text is the borrowed rider:
    /// her Gold basic Fast as Lightning, and rulebook p.20's boxed Handicap Chop Down.
    ///
    /// <para><b>This partial declares its own two faces and no set.</b> Five blocks compose her
    /// nineteen faces in parallel, so <c>AllDefinitions</c>, <c>DealtDefinitions</c>, the starting hand,
    /// the deck and the one real <c>ContentRevision</c> are 8.61i's to mint when the set becomes whole.
    /// Until then every body composed here carries <see cref="BorrowedRiderRevision"/>, which is
    /// deliberately not a value that could be mistaken for a real revision.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <c>MatchContentCatalogs</c> names no Min content, so no
    /// configured match can deal these faces and nothing reachable can start them - milestone 7.9's
    /// gate, which 8.61j flips. Fast as Lightning's rider names
    /// <c>borrow-after-attack-text@1</c>, which this build does not register either, and
    /// <c>MinBorrowedRiderTests</c> asserts both sides of that gate.</para>
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, band and icon below is her
    /// shard's transcription of the extracted faces <c>min__7582886</c> and <c>min19__7348760</c>, read
    /// at milestone 8.61's inventory pass. Nothing is derived, and a missing icon is absent rather than
    /// zero - which is why neither face declares a Radius at all.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>
        /// The placeholder revision 8.61g's two faces and their one body carry. It is malformed on
        /// purpose - the project's rule is that a placeholder must never be a value that could be
        /// mistaken for a real one - and 8.61i replaces it with the set's single real revision.
        /// </summary>
        public const string BorrowedRiderRevision = "min-8-61g-borrowed-rider";

        /// <summary>
        /// Her Gold basic. Initiative 13, secondary Defense 1, secondary Movement 1, "BASIC ATTACK -
        /// RANGED" with Attack 2 and Range 1, no printed item.
        /// </summary>
        public static CardDefinitionId FastAsLightning { get; } =
            new CardDefinitionId("min-fast-as-lightning");

        /// <summary>
        /// Her Gold Handicap - rulebook p.20's card. Initiative 12, secondary Defense 1, secondary
        /// Movement 1, "BASIC ATTACK - RANGED" with Attack 1 and Range 1.
        ///
        /// <para>Inventory rather than deck content: p.6's setup returns it to the box and p.20 brings
        /// it back only for an odd player count, which is why it is in
        /// <see cref="BorrowedRiderReturnedToBoxAtSetup"/>. Its printed values, words, colour and both
        /// secondary icons ship here all the same - what the established deferral defers is the
        /// COMPOSITION of its text, not the card.</para>
        /// </summary>
        public static CardDefinitionId ChopDown { get; } = new CardDefinitionId("min-chop-down");

        /// <summary>
        /// p.9's Gold band with a Ranged Attack primary - "BASIC ATTACK - RANGED" on both faces.
        ///
        /// <para>The member is named after this block rather than after the band because five partials
        /// compose her set in parallel and a shared member declared twice is a compile error, not a
        /// merge. Its VALUE is the conventional one, so 8.61i reconciles the names without moving a
        /// single identity.</para>
        /// </summary>
        public static ActionDefinitionId BorrowedRiderPrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        /// <summary>p.9's lower-band boot, suppressed text: a secondary Movement performs none.</summary>
        public static ActionDefinitionId BorrowedRiderSecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        /// <summary>p.14's numeric Defense, spent from hand.</summary>
        public static ActionDefinitionId BorrowedRiderSecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6's setup returns the Handicap card to the box; p.20 brings it back only for an odd player
        /// count. Stated per block because the aggregate list is 8.61i's.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> BorrowedRiderReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { ChopDown });

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyBorrowedRiderDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
                Array.AsReadOnly(BorrowedRiderProfiles().Select(BuildBorrowedRiderFace).ToArray()));

        /// <summary>
        /// The two faces 8.61g composes, in her shard's own table order. It is a per-block list rather
        /// than <c>AllDefinitions</c> for the reason the type's remarks give.
        /// </summary>
        public static IReadOnlyList<CardDefinition> BorrowedRiderDefinitions =>
            LazyBorrowedRiderDefinitions.Value;

        /// <summary>One of this block's two faces, by its printed identity.</summary>
        public static CardDefinition BorrowedRiderDefinition(CardDefinitionId cardId) =>
            BorrowedRiderDefinitions.Single(definition => definition.Id.Equals(cardId));

        // -----------------------------------------------------------------------------------------
        // The printed faces. One row per card, with the same columns as the shard's table: Initiative,
        // the two secondary icons, the primary Attack value and the printed Range.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<BorrowedRiderProfile> BorrowedRiderProfiles() => new[]
        {
            // "Target a unit in range. After the attack: Apply the "After the attack" text of your
            // resolved or discarded red card. (If it has radius, use that card's value)."
            //
            // "Target a unit in range" is rulebook p.13's own sentence for a Ranged Attack, so it
            // narrows nothing and the target policy is the page's default rather than typed content -
            // the reading MI-05 records for the printed band. Her reach is therefore the printed Range
            // 1 with p.16's Range items composed onto it, which is adjacency until an item raises it.
            new BorrowedRiderProfile(
                FastAsLightning, "Fast as Lightning", 13, 1, 1, 2, 1,
                "Target a unit in range. After the attack: Apply the \"After the attack\" text of " +
                "your resolved or discarded red card. (If it has radius, use that card's value).",
                boxed: false),

            // "Target a hero adjacent to you. After the attack: Apply the "After the attack" text of
            // your resolved or discarded red card."
            //
            // Parked under the established p.20 deferral - Arien's Ceremonial Blade, Widget's Follow
            // Along, Swift's Backup, Bain's Side Quest and Rowenna's Earn the Spurs each carry the same
            // string. What the deferral covers is the WHOLE printed sentence, its "Target a HERO" and
            // its "adjacent to you" included, which is why this row declares no hero-only narrowing and
            // no non-Ranged reclassification: composing half a card is what every earlier Handicap
            // refused to do, and doing it here would make Min the one hero whose boxed face is partly
            // live.
            new BorrowedRiderProfile(
                ChopDown, "Chop Down", 12, 1, 1, 1, 1,
                "Target a hero adjacent to you. After the attack: Apply the \"After the attack\" " +
                "text of your resolved or discarded red card.",
                boxed: true)
        };

        private static CardDefinition BuildBorrowedRiderFace(BorrowedRiderProfile profile) =>
            new CardDefinition(
                profile.Id,
                BorrowedRiderRevision,
                profile.DisplayName,
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                BuildBorrowedRiderActions(profile),
                null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildBorrowedRiderActions(
            BorrowedRiderProfile profile)
        {
            yield return new ActionDefinition(
                BorrowedRiderPrimaryRangedAttack,
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[]
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, profile.Attack),
                    new PrintedValueDefinition(PrintedValueChannel.Range, profile.Range)
                },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.Ranged,
                default,
                // The parked face composes no body at all. A deferred face that shipped one would apply
                // a shortened version of itself, which is the fault
                // BorrowedCardTextRefusal.PrintedTextNotComposed exists to refuse one layer up.
                profile.Boxed
                    ? null
                    : MinEffectContent.BorrowedAfterAttackRider(profile.Id, BorrowedRiderRevision),
                profile.TextComposition,
                PrintedDefenseResolution.None,
                profile.Boxed ? CardTextTiming.None : CardTextTiming.AfterTheAttack);

            yield return new ActionDefinition(
                BorrowedRiderSecondaryMovement,
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedSecondary,
                new[]
                {
                    new PrintedValueDefinition(
                        PrintedValueChannel.Movement, profile.SecondaryMovement)
                },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

            yield return new ActionDefinition(
                BorrowedRiderSecondaryDefense,
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[]
                {
                    new PrintedValueDefinition(
                        PrintedValueChannel.Defense, profile.SecondaryDefense)
                },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
        }

        private sealed class BorrowedRiderProfile
        {
            public BorrowedRiderProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                int attack,
                int range,
                string printedText,
                bool boxed)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Attack = attack;
                Range = range;
                PrintedText = printedText;
                Boxed = boxed;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public int Attack { get; }
            public int Range { get; }
            public string PrintedText { get; }
            public bool Boxed { get; }

            public PrintedTextComposition TextComposition =>
                Boxed
                    ? PrintedTextComposition.DeferredUntil("first odd-player-count configuration")
                    : PrintedTextComposition.None;
        }
    }
}
