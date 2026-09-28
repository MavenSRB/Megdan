using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Sabina the Gunslinger's nineteen printed faces as definition content - milestone 8.22, and the
    /// fourth <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.18c and recorded in
    /// <c>Docs/AI/Rules/HERO_SABINA.md</c>. The tables there and the rows here are meant to be read
    /// side by side: the order, the names and the column meanings match, so a reviewer can audit one
    /// against the other without holding anything in their head. Nothing is derived, for the reason
    /// <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>The epithet is a decision, not a reading, and it does not reach this file.</b> The
    /// extraction's dashboard prints <c>SABINA THE COMMANDER</c> and the rulebook's setup step 7 prints
    /// <c>Sabina the Gunslinger</c>; the shard records the conflict as question S-01 and the user
    /// answered it on 2026-08-25 in favour of the rulebook name. What the answer governs is the display
    /// label a player reads. The stable identity below is <c>sabina</c>, every definition id is built
    /// from it, and no rule, capability or test branches on either epithet - so had the answer gone the
    /// other way, one string in <c>SabinaHeroContent</c> would differ and nothing here would.</para>
    ///
    /// <para><b>What is different from the three sets already built, and therefore what this file is
    /// the first to state.</b> Her Gold basic is the first <em>ranged</em> basic Attack - Point Blank
    /// Shot prints <c>Basic Attack - Ranged</c> with a Range 1 icon, while her Handicap card is a plain
    /// melee Basic Attack printing no Range at all, which is the asymmetry question S-02 turns on.
    /// Three of her Red Attacks print p.14's <c>+</c> convention on the Attack value itself - a number
    /// the card's own text raises - which composes to a
    /// <see cref="RosterValueContributionDefinition"/> on the action rather than to an effect node,
    /// because a conditional addition to a total is not a step. And she is the first verified hero with
    /// no p.19 Active effect of any kind: no <c>This turn:</c>, no <c>This round:</c>, anywhere. Every
    /// card of hers resolves immediately, and Big Sodding Gun is p.16's permanent ability.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class SabinaCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="BroganCardContent.ContentRevision"/> has: it moves when Sabina's content changes.
        ///
        /// <para>It moved for the phantom-declaration removal: Troop Movement, Marching Orders and
        /// Path to Victory declared <c>RepeatTargetBinding.DifferentTarget</c> and not one of the three
        /// faces prints a clause supporting it (see
        /// <c>SabinaEffectContent.CreateCommandedMove</c>'s own doc). The declaration was inert, so no
        /// behaviour moved - but a content revision is a claim about the CONTENT, and the content
        /// changed, so the string moves and the pin, the artwork catalog and the digest move with it.
        /// </para>
        /// </summary>
        public const string ContentRevision = "sabina-9-2-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself, and the identity her prepared token is addressed by.
        ///
        /// <para>It is the bare name, carrying no epithet, which is what makes question S-01 a question
        /// about a label rather than about an identity.</para>
        /// </summary>
        public const string ContentSetId = "sabina";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId PointBlankShot { get; } = Id("point-blank-shot");
        public static CardDefinitionId Bang { get; } = Id("bang");
        public static CardDefinitionId BackToBack { get; } = Id("back-to-back");
        public static CardDefinitionId Quickdraw { get; } = Id("quickdraw");
        public static CardDefinitionId Gunslinger { get; } = Id("gunslinger");
        public static CardDefinitionId Shootout { get; } = Id("shootout");
        public static CardDefinitionId DeadShot { get; } = Id("dead-shot");
        public static CardDefinitionId BulletHell { get; } = Id("bullet-hell");
        public static CardDefinitionId ListenUp { get; } = Id("listen-up");
        public static CardDefinitionId RogerRoger { get; } = Id("roger-roger");
        public static CardDefinitionId SteadyAdvance { get; } = Id("steady-advance");
        public static CardDefinitionId ReadyAndWaiting { get; } = Id("ready-and-waiting");
        public static CardDefinitionId UnwaveringResolve { get; } = Id("unwavering-resolve");
        public static CardDefinitionId TroopMovement { get; } = Id("troop-movement");
        public static CardDefinitionId MarchingOrders { get; } = Id("marching-orders");
        public static CardDefinitionId CloseSupport { get; } = Id("close-support");
        public static CardDefinitionId PathToVictory { get; } = Id("path-to-victory");
        public static CardDefinitionId CoveringFire { get; } = Id("covering-fire");
        public static CardDefinitionId BigSoddingGun { get; } = Id("big-sodding-gun");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the three earlier sets' are. Sabina
        // needs four: she prints no Defense primary and no Movement primary, so those two slots never
        // carry one of her icons, and every one of her seven Attacks but the Handicap card is Ranged.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Point Blank
        /// Shot.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            PointBlankShot, BackToBack, Quickdraw, ListenUp, TroopMovement
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            Gunslinger, Shootout, DeadShot, BulletHell,
            RogerRoger, SteadyAdvance, ReadyAndWaiting, UnwaveringResolve,
            MarchingOrders, CloseSupport, PathToVictory, CoveringFire,
            BigSoddingGun
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            Bang
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_SABINA.md</c>'s printed-value table lists
        /// them.
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions { get; } = Array.AsReadOnly(
            Profiles().Select(Build).ToArray());

        /// <summary>The eighteen definitions a match deals from - everything but the boxed Handicap card.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions { get; } = Array.AsReadOnly(
            AllDefinitions
                .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                .ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Sabina prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Sabina's cards require a defined owner.", nameof(ownerId));
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
        // the milestone that owes that text its composition.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<SabinaCardProfile> Profiles() => new[]
        {
            // The first ranged basic Attack in the verified inventory. Its rider is printed in the
            // present tense - "If the target IS adjacent to you" - and applied at p.14's step 7, by
            // which time step 6 may have removed the target; that tense is why it reads live occupancy
            // rather than the captured pre-consequence space Shootout's past-tense rider reads.
            new SabinaCardProfile(
                PointBlankShot, "Point Blank Shot", 12, 2, 1,
                SabinaPrimaryKind.RangedAttack, 2, PrintedValueChannel.Range, 1,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit in range. After the attack: If the target is adjacent to you, push it " +
                "1 space. (A unit pushed into an obstacle will stop. This is a valid push.)",
                PrintedTextComposition.None),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade and Brogan's Leap Forward are, by the same 2026-08-21 user decision: p.6 returns it
            // to the box and the six-player pitch never brings it back. Its printed values and words
            // ship here as inventory all the same, which is what makes the deferral a decision about
            // composition rather than a gap in the inventory. Two things it says that Point Blank Shot
            // does not are stated on the row rather than in the deferral: it targets a hero, and it
            // prints no Range icon at all - which is the whole of question S-02, since Big Sodding Gun
            // grants "+2 Range" to a basic attack whose face states none.
            new SabinaCardProfile(
                Bang, "Bang", 11, 1, 1,
                SabinaPrimaryKind.Attack, 1, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. After the attack: Push the target 1 space.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                targetableUnits: AttackTargetableUnits.HeroesOnly),

            // Her only basic Skill, and the only card of hers that swaps her own piece rather than two
            // other pieces. p.19's Swap "does not count as movement or placement".
            new SabinaCardProfile(
                BackToBack, "Back to Back", 8, 2, 0,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Swap with a friendly minion in radius.",
                PrintedTextComposition.None),

            // The three faces printing p.14's `+` convention. The clause is a conditional addition to
            // the Attack Total rather than a step, so it is a declared value contribution on the action
            // and there is no card text to compose at all - which is why the composition column below
            // is None and the timing stays None with it.
            new SabinaCardProfile(
                Quickdraw, "Quickdraw", 8, 6, 4,
                SabinaPrimaryKind.RangedAttack, 2, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    Gunslinger, Shootout),
                "Target a unit in range. +3 [Attack] Attack if the target played an attack card this " +
                "turn.",
                PrintedTextComposition.None,
                raisedAttack: 3),

            new SabinaCardProfile(
                Gunslinger, "Gunslinger", 9, 6, 4,
                SabinaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    DeadShot, BulletHell),
                "Target a unit in range. +3 [Attack] Attack if the target played an attack card this " +
                "turn.",
                PrintedTextComposition.None,
                raisedAttack: 3),

            // "remove up to one" - p.19's Remove rather than p.13's Defeat, and the parenthesised
            // sentence restates the keyword's own no-coins consequence rather than creating one.
            new SabinaCardProfile(
                Shootout, "Shootout", 9, 6, 4,
                SabinaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    DeadShot, BulletHell),
                "Target a unit in range. After the attack: If the target was adjacent to you remove " +
                "up to one enemy minion adjacent to you. ( You gain no coins for removing a minion, " +
                "only for defeating. )",
                PrintedTextComposition.None),

            new SabinaCardProfile(
                DeadShot, "Dead Shot", 9, 7, 4,
                SabinaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Target a unit in range. +4 [Attack] Attack if the target played an attack card this " +
                "turn.",
                PrintedTextComposition.None,
                raisedAttack: 4),

            // The same rider as Shootout at twice the count and without the reminder sentence, which
            // is the whole of what its Tier buys.
            new SabinaCardProfile(
                BulletHell, "Bullet Hell", 9, 7, 4,
                SabinaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Target a unit in range. After the attack: If the target was adjacent to you remove " +
                "up to two enemy minions adjacent to you.",
                PrintedTextComposition.None),

            // The Blue swap trio. All three swap two minions with each other and leave Sabina where
            // she is; they differ in Radius and, at Tier III, in one printed clause.
            new SabinaCardProfile(
                ListenUp, "Listen Up", 9, 3, 3,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    RogerRoger, SteadyAdvance),
                "Swap two minions in radius.",
                PrintedTextComposition.None),

            new SabinaCardProfile(
                RogerRoger, "Roger Roger", 10, 4, 3,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    ReadyAndWaiting, UnwaveringResolve),
                "Swap two minions in radius.",
                PrintedTextComposition.None),

            new SabinaCardProfile(
                SteadyAdvance, "Steady Advance", 10, 4, 3,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    ReadyAndWaiting, UnwaveringResolve),
                "If there are two or more friendly minions in radius you may retrieve a discarded " +
                "card, then move up to 1 space.",
                PrintedTextComposition.None),

            // The first verified card overriding p.13's heavy minion immunity. Its Tier I and Tier II
            // siblings print the same swap without the clause and differ from it only in Radius, so
            // the override is the whole of what Tier III buys.
            new SabinaCardProfile(
                ReadyAndWaiting, "Ready and Waiting", 10, 4, 3,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "Swap two minions in radius, ignoring heavy minion immunity.",
                PrintedTextComposition.None),

            new SabinaCardProfile(
                UnwaveringResolve, "Unwavering Resolve", 10, 4, 3,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Defense),
                "If there are two or more friendly minions in radius you may retrieve a discarded " +
                "card, then move up to 2 spaces.",
                PrintedTextComposition.None),

            // The Green command trio. "to a space in radius" is the second of the sentence's two
            // radius readings and is the destination bound, not the selection's reach.
            new SabinaCardProfile(
                TroopMovement, "Troop Movement", 4, 2, 2,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    MarchingOrders, CloseSupport),
                "Move a friendly minion in radius 1 space, to a space in radius. May repeat once.",
                PrintedTextComposition.None),

            new SabinaCardProfile(
                MarchingOrders, "Marching Orders", 3, 3, 2,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    PathToVictory, CoveringFire),
                "Move a friendly minion in radius 1 space, to a space in radius. May repeat once.",
                PrintedTextComposition.None),

            // Ledger question 13's construction printed with `if able`, which is the Shock/Electroblast
            // contrast again: this face has no second arm and its Tier III sibling does.
            new SabinaCardProfile(
                CloseSupport, "Close Support", 3, 3, 2,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    PathToVictory, CoveringFire),
                "An enemy hero in radius adjacent to your friendly minion discards a card, if able.",
                PrintedTextComposition.None),

            new SabinaCardProfile(
                PathToVictory, "Path to Victory", 3, 3, 2,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Range),
                "Move a friendly minion in radius 1 space, to a space in radius. May repeat up to two " +
                "times.",
                PrintedTextComposition.None),

            new SabinaCardProfile(
                CoveringFire, "Covering Fire", 3, 3, 2,
                SabinaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "An enemy hero in radius adjacent to your friendly minion discards a card, or is " +
                "defeated.",
                PrintedTextComposition.None),

            // p.16's Ultimate. It prints no Initiative, no action and - like Arien's Living Tsunami and
            // Brogan's One Man Army - neither Range nor Radius, so its whole printed content is the
            // permanent ability. It is the first Ultimate in this build printing two sentences: two
            // standing value declarations and one trigger.
            new SabinaCardProfile(
                BigSoddingGun, "Big Sodding Gun", 0, 0, 0,
                SabinaPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "Your basic attack has +2 [Range] Range and +2 [Attack] Attack. If you push an enemy " +
                "hero, that hero discards a card, or is defeated.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(SabinaCardProfile profile)
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
                    ? UltimateRuleDefinition.AfterPushAbility(
                        SabinaEffectContent.BigSoddingGunRuleId,
                        "SabinaBigSoddingGun",
                        SabinaEffectContent.BigSoddingGunTriggeredEffect(ContentRevision).Reference,
                        SabinaEffectContent.BigSoddingGunTrigger,
                        PrintedIcon(profile),
                        // "Your basic attack has +2 Range and +2 Attack" - two channels of one printed
                        // sentence, each qualified to the action class the sentence names. Without the
                        // qualification a consumer reading the channel alone would hand two extra
                        // Attack to her five Red Attacks, which are not basic, and two extra Range to
                        // every Radius her Blue and Green kit prints.
                        SabinaEffectContent.BigSoddingGunStandingRules)
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(SabinaCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the three earlier sets express it.
            // Sabina's dashes fall in one place only: Back to Back prints no secondary Movement, and
            // the Ultimate prints no secondary anything.
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

        private static ActionDefinition BuildPrimary(SabinaCardProfile profile)
        {
            switch (profile.Primary)
            {
                case SabinaPrimaryKind.None:
                    return null;

                case SabinaPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        SabinaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        SabinaEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.NonRanged),
                        RaisedAttack(profile));

                case SabinaPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        SabinaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        SabinaEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged),
                        RaisedAttack(profile));

                case SabinaPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        SabinaEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Sabina primary action kind.");
            }
        }

        /// <summary>
        /// p.14's printed <c>+</c>, as the typed declaration that makes it true - or nothing on the
        /// sixteen faces that print a plain number.
        ///
        /// <para>Quickdraw, Gunslinger and Dead Shot print <c>2+</c>, <c>3+</c> and <c>3+</c> beside
        /// "+3 / +4 Attack if the target played an attack card this turn". The page states the
        /// convention next to its own worked example: "The <c>+</c> sign signifies that the value can
        /// be increased by the card text." So the marker is not separate content to carry - it is the
        /// existence of this declaration, and presentation derives the glyph from it rather than from a
        /// second field a definition could set inconsistently with the first.</para>
        ///
        /// <para>It is a value contribution rather than an effect node because it is not a step. Nobody
        /// chooses anything, nothing happens on the board, and the clause is read when p.14 computes
        /// the Attack Total - which is exactly what <c>RosterDynamicValueQuery</c> does with it. The
        /// query it names is the same question <c>target-played-attack-this-turn@1</c> asks; Brogan's
        /// Shield Bash asks it of a candidate it is choosing, and these three ask it of a target they
        /// already have.</para>
        ///
        /// <para>The query answers only for a hero target, which is the printed reading rather than a
        /// limitation: p.10 makes a card played the moment it is placed facedown, and a minion places
        /// none. Against a minion the condition is false and the printed value stands.</para>
        /// </summary>
        private static IEnumerable<RosterValueContributionDefinition> RaisedAttack(SabinaCardProfile profile) =>
            profile.RaisedAttack == 0
                ? Enumerable.Empty<RosterValueContributionDefinition>()
                : new[]
                {
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Attack,
                        RosterValueQueryKind.TargetPlayedAttackCardThisTurn,
                        profile.RaisedAttack)
                };

        /// <summary>
        /// What one printed Attack of Sabina's may target, or <c>null</c> where the face narrows
        /// nothing and p.13's own reading for the subtype stands.
        ///
        /// <para>One of her seven Attacks states something p.13 does not: Bang prints "Target a hero
        /// adjacent to you", which is the narrowing <see cref="AttackTargetableUnits.HeroesOnly"/>
        /// names. The other six print "Target a unit in range" - p.13's own sentence for a Ranged
        /// Attack, restated on the face rather than narrowed by it - so they carry no policy and no
        /// card text of hers binds a target. Everything else about the reach is p.13's: adjacent for a
        /// non-Ranged Attack and the printed Range for a Ranged one, taken from
        /// <see cref="PrintedAttackTargetPolicy.ForClassification"/> rather than restated here.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            SabinaCardProfile profile, AttackRangeKind attackRange)
        {
            if (profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions)
            {
                return null;
            }

            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            return new PrintedAttackTargetPolicy(
                profile.TargetableUnits, classification.Reach, classification.Line);
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Two of Sabina's
        /// faces print neither - the Handicap card and the Ultimate - and both absences are content:
        /// Bang's is what question S-02 turns on, and the Ultimate's is why ledger question 12 has no
        /// Sabina instance.
        ///
        /// <para>The shard's Range/Radius column and its item-icon column are read separately here
        /// because they are printed separately there: Path to Victory's item icon is Range while its
        /// Range/Radius column prints a Radius 3, and a reader who conflated the two would give that
        /// card a printed Range it does not have.</para>
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(SabinaCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum SabinaPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3
        }

        private sealed class SabinaCardProfile
        {
            public SabinaCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                SabinaPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
                int raisedAttack = 0)
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
                TargetableUnits = targetableUnits;
                RaisedAttack = raisedAttack;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public SabinaPrimaryKind Primary { get; }

            /// <summary>
            /// The number printed on the primary action's own icon, which for Sabina is always an
            /// Attack value or nothing: she prints no Defense primary and no Movement primary.
            /// </summary>
            public int PrimaryValue { get; }

            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }

            /// <summary>Which units the face's own words let this Attack target.</summary>
            public AttackTargetableUnits TargetableUnits { get; }

            /// <summary>
            /// How much this face's own text adds to its Attack value when the condition holds, or 0
            /// on a face printing a plain number. It is the <c>+</c> the value prints.
            /// </summary>
            public int RaisedAttack { get; }
        }
    }
}
