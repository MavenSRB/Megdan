using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Wasp's nineteen printed faces as definition content - milestone 8.3, and the first
    /// <see cref="CardContentAuthority.VerifiedOfficial"/> content this project has ever built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.1 and recorded in
    /// <c>Docs/AI/Rules/HERO_WASP.md</c>, which milestone 6.1y made the owner of Wasp's inventory. The
    /// tables there and the rows here are meant to be read side by side: the order, the names and the
    /// column meanings match, so a reviewer can audit one against the other without holding anything in
    /// their head. Nothing here is derived - not the colour from the printed icon, not the Tier from the
    /// Initiative, not the item icon from the colour - because the faces state each of them separately
    /// and Wasp's own set would break any such derivation.</para>
    ///
    /// <para><b>What is deliberately absent.</b> No action carries an <c>EffectRoot</c>. Each card's
    /// text composition lands at its own 8.5-8.13 slice under a content-revision advance, so no
    /// half-implemented card text can run in between, and every printed primary states the milestone
    /// that owes it through <see cref="PrintedTextComposition"/> rather than passing for a card that
    /// prints nothing. The transcription itself rides <see cref="CardDefinition.SourceContent"/>, where
    /// no rule may read it.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class WaspCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry. It moves when Wasp's content changes, which is
        /// what each 8.5-8.13 slice does when it attaches its card's composition - the same coupling
        /// every synthetic revision in this build already has, and the reason a slice's compatibility
        /// decision is a real decision rather than a formality.
        /// </summary>
        public const string ContentRevision = "wasp-8-3-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity. It is not a
        /// <see cref="HeroId"/>: a HeroId names a seat in a match and two mirrored Wasps hold two of
        /// them, while this names the printed set both seats deal from.
        /// </summary>
        public const string ContentSetId = "wasp";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId MagneticDagger { get; } = Id("magnetic-dagger");
        public static CardDefinitionId CracklingDagger { get; } = Id("crackling-dagger");
        public static CardDefinitionId StaticBarrier { get; } = Id("static-barrier");
        public static CardDefinitionId Shock { get; } = Id("shock");
        public static CardDefinitionId Electrocute { get; } = Id("electrocute");
        public static CardDefinitionId ChargedBoomerang { get; } = Id("charged-boomerang");
        public static CardDefinitionId Electroblast { get; } = Id("electroblast");
        public static CardDefinitionId ThunderBoomerang { get; } = Id("thunder-boomerang");
        public static CardDefinitionId LiftUp { get; } = Id("lift-up");
        public static CardDefinitionId ControlGravity { get; } = Id("control-gravity");
        public static CardDefinitionId KineticRepulse { get; } = Id("kinetic-repulse");
        public static CardDefinitionId CenterOfMass { get; } = Id("center-of-mass");
        public static CardDefinitionId KineticBlast { get; } = Id("kinetic-blast");
        public static CardDefinitionId StopProjectiles { get; } = Id("stop-projectiles");
        public static CardDefinitionId DeflectProjectiles { get; } = Id("deflect-projectiles");
        public static CardDefinitionId Telekinesis { get; } = Id("telekinesis");
        public static CardDefinitionId ReflectProjectiles { get; } = Id("reflect-projectiles");
        public static CardDefinitionId MassTelekinesis { get; } = Id("mass-telekinesis");
        public static CardDefinitionId HighVoltage { get; } = Id("high-voltage");

        // -----------------------------------------------------------------------------------------
        // Local action identities. Local to a card, so the same six names recur across the set and a
        // reader can ask "what is this card's primary" without knowing which card it is.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId PrimaryBlock { get; } = new ActionDefinitionId("primary-block");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.5's starting hand: "the three Tier I cards and the two basic cards with no Tier". The
        /// Handicap card is already out of the box by then, so the Gold card here is Magnetic Dagger.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            MagneticDagger, StaticBarrier, Shock, LiftUp, StopProjectiles
        });

        /// <summary>
        /// The thirteen cards p.5 leaves in the deck, reachable only through p.15's Level Up and p.16's
        /// unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            Electrocute, ChargedBoomerang, Electroblast, ThunderBoomerang,
            ControlGravity, KineticRepulse, CenterOfMass, KineticBlast,
            DeflectProjectiles, Telekinesis, ReflectProjectiles, MassTelekinesis,
            HighVoltage
        });

        /// <summary>
        /// p.5's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart from
        /// both: a match that instantiated it would be dealing a card the page removed.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            CracklingDagger
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_WASP.md</c>'s printed-value table lists them.
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
            throw new ArgumentException($"Wasp prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.5's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        ///
        /// The Handicap card is not instantiated, because p.5 puts it in the box - a match holding an
        /// instance of it would be one Level Up away from dealing a card that is not in play. Milestone
        /// 8.4 is what puts these into the director turn loop; here they exist so verified definitions
        /// can be exercised through the same lifecycle every synthetic instance already uses.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Wasp's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat, so the caller names the prefix " +
                    "that distinguishes one mirrored Wasp's cards from the other's.",
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

        private static CardDefinitionId Id(string cardId) =>
            new CardDefinitionId($"{ContentSetId}-{cardId}");

        // -----------------------------------------------------------------------------------------
        // The printed faces. One row per card, in the shard's table order, with the same columns:
        // Initiative, the two secondary icons (0 where the face prints none), the primary action and
        // its value, the p.9 Range or Radius icon, the progression content, the transcribed text, and
        // the milestone that owes that text its composition.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<WaspCardProfile> Profiles() => new[]
        {
            new WaspCardProfile(
                MagneticDagger, "Magnetic Dagger", 12, 2, 1,
                WaspPrimaryKind.Attack, 3, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. After the attack: This turn: Enemy units in radius " +
                "cannot be swapped or placed by themselves, or by enemy heroes.",
                PrintedTextComposition.AwaitedAt("8.9")),

            // p.20's Handicap card. Its behavior is deferred by the 2026-08-21 user decision under the
            // named trigger below, because p.5 returns it to the box and the six-player pitch never
            // brings it back; its printed values and words ship here as inventory all the same, which is
            // what makes the deferral a decision about composition rather than a gap in the inventory.
            // It is a Basic Gold card by p.9's supertype and p.15's Gold-and-Silver pair - the "H" the
            // face prints where a Tier would go is a membership marker for p.20's rule, and CardTier
            // deliberately declares no member for it.
            new WaspCardProfile(
                CracklingDagger, "Crackling Dagger", 11, 1, 1,
                WaspPrimaryKind.Attack, 2, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. After the attack: This turn: Enemy units in radius " +
                "cannot be swapped or placed by themselves, or by enemy heroes.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration")),

            new WaspCardProfile(
                StaticBarrier, "Static Barrier", 13, 2, 0,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "This turn: While an enemy hero outside of radius is performing an action, spaces in " +
                "radius count as obstacles. While an enemy hero in radius is performing an action, " +
                "spaces outside of radius count as obstacles.",
                PrintedTextComposition.AwaitedAt("8.9")),

            new WaspCardProfile(
                Shock, "Shock", 8, 6, 4,
                WaspPrimaryKind.Attack, 5, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    Electrocute, ChargedBoomerang),
                "Target a unit adjacent to you. After the attack: An enemy hero in radius and not " +
                "adjacent to you discards a card, if able.",
                PrintedTextComposition.AwaitedAt("8.10")),

            new WaspCardProfile(
                Electrocute, "Electrocute", 9, 6, 4,
                WaspPrimaryKind.Attack, 5, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    Electroblast, ThunderBoomerang),
                "Target a unit adjacent to you. After the attack: An enemy hero in radius and not " +
                "adjacent to you discards a card, if able.",
                PrintedTextComposition.AwaitedAt("8.10")),

            // The one card whose face and the second-printing rulebook differ, and only in p.9 reminder
            // text: the rulebook's p.9 illustration reads "( Units adjacent to you are in a straight
            // line from you. )". Card text outranks the rulebook under the ledger's authority hierarchy,
            // so the face's wording is what is transcribed here; the two say the same thing.
            new WaspCardProfile(
                ChargedBoomerang, "Charged Boomerang", 9, 3, 4,
                WaspPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    Electroblast, ThunderBoomerang),
                "Target a unit in range and not in a straight line. ( Adjacent units are in a straight " +
                "line as well. )",
                PrintedTextComposition.AwaitedAt("8.10")),

            new WaspCardProfile(
                Electroblast, "Electroblast", 9, 7, 4,
                WaspPrimaryKind.Attack, 6, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Initiative),
                "Target a unit adjacent to you. After the attack: An enemy hero in radius and not " +
                "adjacent to you discards a card, or is defeated.",
                PrintedTextComposition.AwaitedAt("8.10")),

            // The face prints no comma after "hero"; the transcription keeps its punctuation rather
            // than correcting it.
            new WaspCardProfile(
                ThunderBoomerang, "Thunder Boomerang", 9, 4, 4,
                WaspPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Target a unit in range and not in a straight line. After the attack: If you targeted " +
                "a hero may repeat once on a different target.",
                PrintedTextComposition.AwaitedAt("8.10")),

            new WaspCardProfile(
                LiftUp, "Lift Up", 10, 5, 3,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    ControlGravity, KineticRepulse),
                "Move a unit, or a token, in radius 1 space, without moving it away from you or closer " +
                "to you. May repeat once on the same target.",
                PrintedTextComposition.AwaitedAt("8.6")),

            new WaspCardProfile(
                ControlGravity, "Control Gravity", 10, 5, 3,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    CenterOfMass, KineticBlast),
                "Move a unit, or a token, in radius 1 space, without moving it away from you or closer " +
                "to you. May repeat once on the same target.",
                PrintedTextComposition.AwaitedAt("8.6")),

            new WaspCardProfile(
                KineticRepulse, "Kinetic Repulse", 10, 5, 3,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    CenterOfMass, KineticBlast),
                "Push up to 2 enemy units adjacent to you 3 spaces; if a pushed hero is stopped by an " +
                "obstacle, that hero discards a card, if able.",
                PrintedTextComposition.AwaitedAt("8.8")),

            new WaspCardProfile(
                CenterOfMass, "Center of Mass", 11, 6, 3,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Defense),
                "Move a unit, or a token, in radius 1 space, without moving it away from you or closer " +
                "to you. May repeat up to two times on the same target.",
                PrintedTextComposition.AwaitedAt("8.6")),

            new WaspCardProfile(
                KineticBlast, "Kinetic Blast", 11, 6, 3,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "Push up to 2 enemy units adjacent to you 3 or 4 spaces; if a pushed hero is stopped " +
                "by an obstacle, that hero discards a card, if able.",
                PrintedTextComposition.AwaitedAt("8.8")),

            new WaspCardProfile(
                StopProjectiles, "Stop Projectiles", 3, 0, 2,
                WaspPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    DeflectProjectiles, Telekinesis),
                "Block a ranged attack.",
                PrintedTextComposition.AwaitedAt("8.11")),

            new WaspCardProfile(
                DeflectProjectiles, "Deflect Projectiles", 3, 0, 2,
                WaspPrimaryKind.Block, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    ReflectProjectiles, MassTelekinesis),
                "Block a ranged attack; if you do, an enemy hero in range, other than the attacker, " +
                "discards a card, if able.",
                PrintedTextComposition.AwaitedAt("8.11")),

            new WaspCardProfile(
                Telekinesis, "Telekinesis", 3, 3, 2,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    ReflectProjectiles, MassTelekinesis),
                "Place a unit or a token in range, which is not in a straight line, into a space " +
                "adjacent to you.",
                PrintedTextComposition.AwaitedAt("8.6")),

            new WaspCardProfile(
                ReflectProjectiles, "Reflect Projectiles", 2, 0, 2,
                WaspPrimaryKind.Block, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Radius),
                "Block a ranged attack; if you do, an enemy hero in range discards a card, if able.",
                PrintedTextComposition.AwaitedAt("8.11")),

            new WaspCardProfile(
                MassTelekinesis, "Mass Telekinesis", 2, 4, 2,
                WaspPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "Place a unit or a token in range, which is not in a straight line, into a space " +
                "adjacent to you. May repeat once.",
                PrintedTextComposition.AwaitedAt("8.6")),

            // p.16's Ultimate. It prints no Initiative and no action at all - the card is placed beside
            // the dashboard and its ability "is always in effect" - so its whole printed content is the
            // permanent ability and the Radius that ability reads. The ability is a triggered one, which
            // none of RuleModification's five families can express, so it ships as an explicit
            // outstanding composition owed to milestone 8.13 rather than as a stand-in rule.
            new WaspCardProfile(
                HighVoltage, "High Voltage", 0, 0, 0,
                WaspPrimaryKind.None, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.UltimateCard(),
                "Each time after you perform a basic skill, you may defeat an enemy minion in radius; " +
                "an enemy hero who was adjacent to that minion discards a card, if able.",
                PrintedTextComposition.AwaitedAt("8.13"))
        };

        private static CardDefinition Build(WaspCardProfile profile)
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
                    ? UltimateRuleDefinition.AwaitingComposition(
                        "high-voltage-basic-skill-trigger",
                        "WaspHighVoltage",
                        profile.TextComposition,
                        PrintedIcon(profile))
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(WaspCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card. It is expressed by the action not
            // existing rather than by a zero value: zero is a number the face does not print, and
            // PrintedValueDefinition would accept it.
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

        private static ActionDefinition BuildPrimary(WaspCardProfile profile)
        {
            switch (profile.Primary)
            {
                case WaspPrimaryKind.None:
                    return null;

                case WaspPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.AttackValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        null,
                        profile.TextComposition);

                case WaspPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.AttackValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        null,
                        profile.TextComposition);

                case WaspPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        null,
                        profile.TextComposition);

                case WaspPrimaryKind.Block:
                    // p.14's Block card. It states no card-state transition and cannot be replaced:
                    // p.12-13's replacements replace the primary action a hero performs on their own
                    // turn, and p.14 performs a Block in reaction to an attack, so neither the
                    // replacement nor the in-hand discard a secondary Defense declares is this action's.
                    // Milestone 8.11 decides what a Block spends when it evaluates one; guessing here
                    // would put a transition into content that no rule asked for.
                    return new ActionDefinition(
                        PrimaryBlock,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        default,
                        null,
                        profile.TextComposition,
                        PrintedDefenseResolution.Block);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Wasp primary action kind.");
            }
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Three of Wasp's
        /// cards print neither - Kinetic Repulse, Kinetic Blast and Stop Projectiles - and that absence
        /// is content: their text asks about adjacency, which needs no icon.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(WaspCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum WaspPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3,

            /// <summary>p.14's Block card: a Defense primary that prints no Defense value.</summary>
            Block = 4
        }

        private sealed class WaspCardProfile
        {
            public WaspCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                WaspPrimaryKind primary,
                int attackValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Primary = primary;
                AttackValue = attackValue;
                IconChannel = iconChannel;
                IconValue = iconValue;
                Progression = progression;
                PrintedText = printedText;
                TextComposition = textComposition;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public WaspPrimaryKind Primary { get; }
            public int AttackValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
        }
    }
}
