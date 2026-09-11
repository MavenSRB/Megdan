using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Swift the Sharpshooter's nineteen printed faces as definition content - milestone 8.38d, and
    /// the thirteenth <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.38's inventory pass and
    /// recorded in the <c>Docs/AI/Rules/HERO_SWIFT.md</c> shard. The tables there and the rows here are
    /// meant to be read side by side: the order, the names and the column meanings match, so a reviewer
    /// can audit one against the other without holding anything in their head. Nothing is derived, for
    /// the reason <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>The second-printing rulebook is silent about her, so the extraction is sole
    /// authority.</b> It returns zero hits for <c>Swift</c>, zero for <c>Sharpshooter</c> and zero for
    /// all nineteen card names; its only two hits for the substring <c>Snipe</c> are p.21's Sniper
    /// ROLE. That is the same conclusion Ursafar's and Tali's sets reach for a different reason - they
    /// are Devoted Hero pack heroes with no second source at all, while she is a base-box hero the one
    /// second source we hold simply does not mention.</para>
    ///
    /// <para><b>What is different from the twelve sets already built.</b> Three of her five Red Attacks
    /// narrow their targets by rulebook p.19's straight line - the complement of the clause both of
    /// Wasp's Boomerangs print - and two of those also exclude the attacker's own neighbours, which is
    /// the printed-Range band milestone 8.38a minted. Her whole Blue line places her, three of them
    /// onto a file through where she stands and two deliberately off every file. Her two Green bounties
    /// are the roster's first standing promise paid by something happening to a THIRD party. And her
    /// Gold basic is the first face to borrow from a pool defined by ORDER rather than by a card's
    /// state.</para>
    ///
    /// <para><b>She owns no token and no marker</b>, and prints no Movement primary and no Defense
    /// primary: every one of her eighteen dealt faces is a Basic Skill, a Ranged Attack, a Skill or the
    /// Ultimate. Her mobility is entirely card text, which is a different thing from a Movement action
    /// and is why none of it reads a Movement value.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class SwiftCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling every verified set has:
        /// it moves when Swift's content does.
        /// </summary>
        public const string ContentRevision = "swift-8-38d-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "swift";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Her Gold basic, and the roster's first face to borrow from a pool defined by ORDER.
        ///
        /// <para>"Perform the primary action of your rightmost resolved card" names exactly one card -
        /// p.6's turn area holds what a hero has resolved this round in committed order - so a card the
        /// borrow refuses empties the pool rather than falling through to the card behind it. Milestone
        /// 8.38c's <see cref="BorrowedActionPool.OwnedRightmostResolvedCard"/> is that reading.</para>
        /// </summary>
        public static CardDefinitionId Reload { get; } = Id("reload");

        /// <summary>
        /// p.20's Handicap card, deferred exactly as every other hero's is and by the same 2026-08-21
        /// user decision: p.6 returns it to the box and the six-player pitch never brings it back. Its
        /// printed values and words ship here as inventory all the same.
        ///
        /// <para>It is the more restrictive half of a pair with <see cref="Reload"/>, which is the
        /// shape Wasp's Crackling Dagger has to Magnetic Dagger and Silverarrow's Keep the Distance to
        /// Shoot and Scoot: same Initiative, same secondaries, and a clause that takes something away -
        /// here the modal's second bullet, plus a bar on borrowing an Attack onto a minion.</para>
        /// </summary>
        public static CardDefinitionId Backup { get; } = Id("backup");

        public static CardDefinitionId Bounce { get; } = Id("bounce");

        public static CardDefinitionId Snipe { get; } = Id("snipe");
        public static CardDefinitionId PreparedShot { get; } = Id("prepared-shot");
        public static CardDefinitionId Shotgun { get; } = Id("shotgun");
        public static CardDefinitionId Killshot { get; } = Id("killshot");
        public static CardDefinitionId SuperShotgun { get; } = Id("super-shotgun");

        public static CardDefinitionId SteamJump { get; } = Id("steam-jump");
        public static CardDefinitionId AssaultJump { get; } = Id("assault-jump");
        public static CardDefinitionId DelayedJump { get; } = Id("delayed-jump");
        public static CardDefinitionId DropTrooper { get; } = Id("drop-trooper");
        public static CardDefinitionId MobileScout { get; } = Id("mobile-scout");

        public static CardDefinitionId Suppress { get; } = Id("suppress");
        public static CardDefinitionId PinDown { get; } = Id("pin-down");
        public static CardDefinitionId MarkForDeath { get; } = Id("mark-for-death");
        public static CardDefinitionId KillingGround { get; } = Id("killing-ground");
        public static CardDefinitionId HuntingSeason { get; } = Id("hunting-season");

        public static CardDefinitionId BulletTime { get; } = Id("bullet-time");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the twelve earlier sets' are. Swift
        // needs four, and prints neither a Movement primary nor a Defense primary.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6's five starting cards: one of each colour a hero holds at setup.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            Reload, Bounce, Snipe, SteamJump, Suppress
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            PreparedShot, Shotgun, Killshot, SuperShotgun,
            AssaultJump, DelayedJump, DropTrooper, MobileScout,
            PinDown, MarkForDeath, KillingGround, HuntingSeason,
            BulletTime
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { Backup });

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
                Array.AsReadOnly(Profiles().Select(Build).ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyAllDefinitions.Value
                    .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                    .ToArray()));

        /// <summary>
        /// All nineteen faces, in the inventory's printed-value table order.
        ///
        /// <para><b>Built lazily, and milestone 8.37's static-initialization cycle is why.</b> Composing
        /// these definitions reaches <see cref="SwiftEffectContent"/>, which reaches
        /// <see cref="SwiftActiveEffectContent"/>, whose own initializers compose effects that name
        /// cards back in this class. An eager field initializer here makes that a cycle the CLR
        /// resolves by handing out half-initialized values, which is exactly what cost milestone 8.37
        /// 1,423 failing cases on one <c>NullReferenceException</c>. Deferring the composition to first
        /// use removes the edge in both directions.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The eighteen definitions a match deals from - everything but the boxed Handicap card.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Swift prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Swift's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat, so the caller names the prefix " +
                    "that distinguishes one seat's cards from another's.",
                    nameof(instanceIdPrefix));
            }

            return Array.AsReadOnly(DealtDefinitions
                .Select(definition =>
                {
                    var instanceId = new CardInstanceId($"{instanceIdPrefix}-{definition.Id.Value}");
                    return StartingHand.Contains(definition.Id)
                        ? CardInstance.CreateInHand(instanceId, definition.Reference, ownerId)
                        : CardInstance.CreateInDeck(instanceId, definition.Reference, ownerId);
                })
                .ToArray());
        }

        /// <summary>
        /// One seat's whole setup as a hand the configured match can be composed from: p.6's five in
        /// hand, the thirteen its deck keeps, and the eighteen definitions both draw on.
        /// </summary>
        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(
                DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)));
        }

        private static CardDefinitionId Id(string cardId) =>
            new CardDefinitionId($"{ContentSetId}-{cardId}");

        // -----------------------------------------------------------------------------------------
        // The printed faces. One row per card, in the shard's table order, with the same columns:
        // Initiative, the two secondary icons (0 where the face prints none), the primary action and
        // its value, the p.9 Range or Radius icon, the progression content, the transcribed text, and
        // whether that text is composed.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<SwiftCardProfile> Profiles() => new[]
        {
            new SwiftCardProfile(
                Reload, "Reload!", 6, 1, 1,
                SwiftPrimaryKind.BasicSkill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Choose one — • Perform the primary action of your rightmost resolved card. " +
                "• Defeat a minion adjacent to you.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                Backup, "Backup", 6, 1, 1,
                SwiftPrimaryKind.BasicSkill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Perform the primary action of your rightmost resolved card. If you perform an attack " +
                "this way, you cannot target minions.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration")),

            // The one single-secondary face in her set, and milestone 8.35z20's slot-position trap in
            // its purest form: its only icon is drawn in the LOWER band where sixteen of her faces put
            // a boot, and its glyph is a riveted shield. Defense 2, no secondary Movement.
            new SwiftCardProfile(
                Bounce, "Bounce", 12, 2, 0,
                SwiftPrimaryKind.BasicSkill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Move 2 spaces in a straight line, ignoring obstacles; if this card is already " +
                "resolved as you perform this action, may repeat once.",
                PrintedTextComposition.None),

            // "at maximum range, and in a straight line" - the exact band milestone 8.35z30 minted for
            // Silverarrow, narrowed further by milestone 8.38a's line member. Both are declarations on
            // the target policy rather than effects, so the offer, the choice and the resolution read
            // one answer.
            new SwiftCardProfile(
                Snipe, "Snipe", 8, 4, 4,
                SwiftPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    PreparedShot, Shotgun),
                "Target a unit at maximum range, and in a straight line.",
                PrintedTextComposition.None,
                SwiftTargetNarrowing.MaximumRangeOnTheLine),

            new SwiftCardProfile(
                PreparedShot, "Prepared Shot", 8, 5, 4,
                SwiftPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    Killshot, SuperShotgun),
                "Target a unit in range, in a straight line, and not adjacent to you.",
                PrintedTextComposition.None,
                SwiftTargetNarrowing.OnTheLineBeyondAdjacent),

            new SwiftCardProfile(
                Shotgun, "Shotgun", 8, 6, 4,
                SwiftPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    Killshot, SuperShotgun),
                "Target a unit in range. Before the attack, an enemy hero adjacent to the target " +
                "discards a card, if able.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                Killshot, "Killshot", 9, 5, 4,
                SwiftPrimaryKind.RangedAttack, 6, PrintedValueChannel.Range, 5,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                "Target a unit in range, in a straight line, and not adjacent to you.",
                PrintedTextComposition.None,
                SwiftTargetNarrowing.OnTheLineBeyondAdjacent),

            // Shotgun's sentence with ledger question 13's escalation in place of its "if able" - the
            // same relationship Wasp's Shock and Electroblast have, and Silverarrow's Warning Shot and
            // Treetop Sentinel.
            new SwiftCardProfile(
                SuperShotgun, "Super-Shotgun", 9, 6, 4,
                SwiftPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Target a unit in range. Before the attack, an enemy hero adjacent to the target " +
                "discards a card, or is defeated.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                SteamJump, "Steam Jump", 9, 3, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    AssaultJump, DelayedJump),
                "Place yourself into a space in a straight line in radius. Push an enemy unit " +
                "adjacent to you up to 1 space.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                AssaultJump, "Assault Jump", 10, 4, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    DropTrooper, MobileScout),
                "Place yourself into a space in a straight line in radius. Push an enemy unit " +
                "adjacent to you up to 2 spaces.",
                PrintedTextComposition.None),

            // The first of her two off-line placements, and the first "End of turn:" face in her set.
            // p.19 counts an adjacent space as a straight line, so "not in a straight line from you"
            // excludes every one of her own neighbours - a consequence the printed words do not say
            // out loud.
            new SwiftCardProfile(
                DelayedJump, "Delayed Jump", 10, 4, 1,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    DropTrooper, MobileScout),
                "End of turn: Place yourself into a space in radius not in a straight line from you.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                DropTrooper, "Drop Trooper", 10, 4, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "Place yourself into a space in a straight line in radius. Push up to two enemy units " +
                "adjacent to you up to 2 spaces.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                MobileScout, "Mobile Scout", 10, 4, 1,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Movement),
                "End of turn: Place yourself into a space in radius not in a straight line from you. " +
                "You may then fast travel once, if able.",
                PrintedTextComposition.None),

            // Transcribed as printed: Suppress prints NO comma after "terrain" and its two siblings
            // do. The relative clause is restrictive in all three, which is the only reading that
            // leaves "who is not adjacent to terrain" doing work, so nothing depends on it - and
            // regularising it silently would assert a correction we have no second source to make.
            new SwiftCardProfile(
                Suppress, "Suppress", 4, 2, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    PinDown, MarkForDeath),
                "An enemy hero in radius, who is not adjacent to terrain discards a card, if able.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                PinDown, "Pin Down", 4, 2, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    KillingGround, HuntingSeason),
                "An enemy hero in radius, who is not adjacent to terrain, discards a card, if able.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                MarkForDeath, "Mark for Death", 4, 2, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    KillingGround, HuntingSeason),
                "Move an enemy minion in radius up to 3 spaces to a space in radius. Next turn: the " +
                "first time an enemy minion in radius is defeated, gain 1 coin.",
                PrintedTextComposition.None),

            new SwiftCardProfile(
                KillingGround, "Killing Ground", 3, 3, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Range),
                "An enemy hero in radius, who is not adjacent to terrain, discards a card, or is " +
                "defeated.",
                PrintedTextComposition.None),

            // Transcribed as printed: this one capitalises "The" where Mark for Death prints "the".
            new SwiftCardProfile(
                HuntingSeason, "Hunting Season", 3, 3, 2,
                SwiftPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "Move up to two enemy minions in radius up to 3 spaces to spaces in radius. Next " +
                "turn: The first two times an enemy minion in radius is defeated, gain 1 coin.",
                PrintedTextComposition.None),

            // p.16's Ultimate. It prints no Initiative, no action, and neither Range nor Radius, so
            // ledger question 12's item-reaches-the-Ultimate reading has nothing to reach here. Its
            // whole printed content is the permanent ability.
            new SwiftCardProfile(
                BulletTime, "Bullet Time", 0, 0, 0,
                SwiftPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "After you resolve a basic card you may perform the primary action on that card; you " +
                "cannot target the same enemy hero twice in the same turn this way.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(SwiftCardProfile profile)
        {
            var isUltimate = profile.Progression.IsUltimate;
            return new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                isUltimate ? default : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                isUltimate
                    ? UltimateRuleDefinition.TriggeredAbility(
                        "bullet-time-after-basic-card",
                        "SwiftBulletTime",
                        SwiftEffectContent.BulletTimeTriggeredEffect(ContentRevision).Reference,
                        UltimateTriggerCondition.AfterControllerResolvesBasicCard,
                        new[] { TimingCheckpoint.ActionEnded },
                        PrintedIcon(profile),
                        // "you cannot target the same enemy hero twice in the same turn this way" -
                        // milestone 8.38d. It is declared on the ABILITY because "this way" names the
                        // grant: her own ordinary Attack on the same enemy in the same turn is
                        // untouched, and so is a primary she borrows through Reload!.
                        grantedTargetExclusion:
                            GrantedTargetExclusion.SameEnemyHeroOncePerTurn)
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(SwiftCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the inventory's Sec Mov or Sec Def column means the face prints no such icon,
            // so that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value. Bounce is the only card in her set where that
            // falls, and it falls on Movement.
            if (profile.SecondaryMovement > 0)
            {
                actions.Add(new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Movement, profile.SecondaryMovement)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)));
            }

            if (profile.SecondaryDefense > 0)
            {
                actions.Add(new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Defense, profile.SecondaryDefense)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)));
            }

            return actions;
        }

        private static ActionDefinition BuildPrimary(SwiftCardProfile profile)
        {
            switch (profile.Primary)
            {
                case SwiftPrimaryKind.None:
                    return null;

                case SwiftPrimaryKind.BasicSkill:
                    return new ActionDefinition(
                        PrimaryBasicSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        SwiftEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case SwiftPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        SwiftEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        SwiftEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile));

                case SwiftPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        SwiftEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Swift primary action kind.");
            }
        }

        /// <summary>
        /// What one of her Attacks may target where its own words say something p.13's default does
        /// not. Three of her five Red faces do; Shotgun and Super-Shotgun print "a unit in range",
        /// which is p.13's own sentence for a Ranged Attack word for word and narrows nothing.
        ///
        /// <para>Both narrowings are milestone 8.38a's and both are declarations rather than effects,
        /// for the reason milestone 8.10 recorded when it put Wasp's complement here: a sentence about
        /// what an Attack may target belongs to the Attack's own target set, which is what keeps the
        /// greyed offer, the target choice and the resolution reading one answer.</para>
        ///
        /// <para>Note what is <b>not</b> declared: no face of hers is
        /// <see cref="AttackTargetBinding.ByCardText"/>, because none moves before it targets, and none
        /// narrows to heroes - her Handicap card takes its hero-only narrowing from the BORROWED
        /// action rather than from an Attack of its own.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(SwiftCardProfile profile)
        {
            if (profile.TargetNarrowing == SwiftTargetNarrowing.None)
            {
                return null;
            }

            var classification = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged);
            switch (profile.TargetNarrowing)
            {
                case SwiftTargetNarrowing.MaximumRangeOnTheLine:
                    return classification.AtExactlyThePrintedRange().RestrictedToTheStraightLine();
                case SwiftTargetNarrowing.OnTheLineBeyondAdjacent:
                    return classification
                        .BeyondAdjacentWithinThePrintedRange()
                        .RestrictedToTheStraightLine();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.TargetNarrowing, "Unknown Swift target narrowing.");
            }
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Four of her faces
        /// print neither - both Gold basics, Bounce and the Ultimate - and the absence is content.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(SwiftCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum SwiftPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,

            /// <summary>p.9's Gold and Silver band - a Skill printed on a basic card.</summary>
            BasicSkill = 1,

            RangedAttack = 2,
            Skill = 3
        }

        /// <summary>
        /// Which printed targeting sentence one of her Red faces states, or
        /// <see cref="None"/> for the two that state p.13's own.
        /// </summary>
        private enum SwiftTargetNarrowing
        {
            None = 0,

            /// <summary>Snipe: "at maximum range, and in a straight line".</summary>
            MaximumRangeOnTheLine = 1,

            /// <summary>Prepared Shot and Killshot: "in range, in a straight line, and not adjacent to you".</summary>
            OnTheLineBeyondAdjacent = 2
        }

        private sealed class SwiftCardProfile
        {
            public SwiftCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                SwiftPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                SwiftTargetNarrowing targetNarrowing = SwiftTargetNarrowing.None)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Primary = primary;
                PrimaryValue = primaryValue;
                IconChannel = iconChannel;
                IconValue = iconValue;
                Progression = progression;
                PrintedText = printedText;
                TextComposition = textComposition;
                TargetNarrowing = targetNarrowing;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public SwiftPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public SwiftTargetNarrowing TargetNarrowing { get; }
        }
    }
}
