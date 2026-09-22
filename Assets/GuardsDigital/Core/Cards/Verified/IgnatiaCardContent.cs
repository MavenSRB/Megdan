using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's nineteen printed faces as ONE set - the twenty-second hero this project
    /// builds, read off the extracted Tabletopia fronts at the 8.75 inventory pass.
    ///
    /// <para><b>What this file is.</b> The inventory block. Every face's transcribed text, printed
    /// values, colour, Tier, item icon and level-up fork, with NO effect body attached: each primary
    /// carries <see cref="PrintedTextComposition.AwaitedAt"/> (or, for the boxed Handicap,
    /// <see cref="PrintedTextComposition.DeferredUntil"/>) and the Ultimate carries
    /// <see cref="UltimateRuleDefinition.AwaitingComposition"/>, exactly as Wasp's and Dodger's own
    /// inventory milestones did. The composition lands in later 8.75x slices under a content-revision
    /// advance, so no half-implemented text can run in between.</para>
    ///
    /// <para><b>Her identity is a mechanism no dealt hero has.</b> Every action card prints TWO texts -
    /// a sun (☀) line and a moon (☾) line - and the live face of the Tie Breaker coin
    /// (<see cref="GuardsDigital.Core.Setup.TieBreakerState.RevealedSide"/>) selects which applies:
    /// "Apply the text matching the symbol on the Tie Breaker coin." Equilibrium frees the choice for a
    /// round; the Ultimate, Chaos Incarnate, flips the coin and repeats a primary action on the
    /// opposite face. Her tokens are MAGMA tokens - impassable obstacles the Green shard drops on empty
    /// spaces, reusing the obstacle system, and the Blue shard punishes enemy heroes standing adjacent
    /// to one. None of that composition is in this file; the two-text transcription is.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Ignatia
    /// content, so no configured match can deal these cards - milestone 7.9's gate, the same one every
    /// hero sits behind until its dealing block flips it.</para>
    /// </summary>
    public static class IgnatiaCardContent
    {
        /// <summary>The revision all nineteen definitions carry at the inventory pass.</summary>
        public const string ContentRevision = "ignatia-8-75-rev-1";

        /// <summary>p.5's content-set identity - the printed set a seat deals from. Every id is prefixed with it.</summary>
        public const string ContentSetId = "ignatia";

        // -----------------------------------------------------------------------------------------
        // Conventional action identities, local to their cards and shared across the set.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        // -----------------------------------------------------------------------------------------
        // The nineteen card identities.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId ChaosBolt { get; } = Id("chaos-bolt");
        public static CardDefinitionId Equilibrium { get; } = Id("equilibrium");
        public static CardDefinitionId PlayingWithFire { get; } = Id("playing-with-fire");
        public static CardDefinitionId ErraticFireblast { get; } = Id("erratic-fireblast");
        public static CardDefinitionId CrackOfDoom { get; } = Id("crack-of-doom");
        public static CardDefinitionId LooselyAimedFirebolts { get; } = Id("loosely-aimed-firebolts");
        public static CardDefinitionId ImminentEruption { get; } = Id("imminent-eruption");
        public static CardDefinitionId AbruptCombustion { get; } = Id("abrupt-combustion");
        public static CardDefinitionId SpontaneousImmolation { get; } = Id("spontaneous-immolation");
        public static CardDefinitionId SearingHeat { get; } = Id("searing-heat");
        public static CardDefinitionId ViolentConflagration { get; } = Id("violent-conflagration");
        public static CardDefinitionId ScorchingBlaze { get; } = Id("scorching-blaze");
        public static CardDefinitionId PathOfAshes { get; } = Id("path-of-ashes");
        public static CardDefinitionId PathOfCinders { get; } = Id("path-of-cinders");
        public static CardDefinitionId UnstablePortal { get; } = Id("unstable-portal");
        public static CardDefinitionId PathOfFlames { get; } = Id("path-of-flames");
        public static CardDefinitionId ChaosGate { get; } = Id("chaos-gate");
        public static CardDefinitionId ChaosIncarnate { get; } = Id("chaos-incarnate");
        public static CardDefinitionId UnreliableJolt { get; } = Id("unreliable-jolt");

        /// <summary>p.6's five starting-hand cards: the Gold and Silver basics and the three Tier I colours.</summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            ChaosBolt, Equilibrium, PlayingWithFire, AbruptCombustion, PathOfAshes
        });

        /// <summary>The thirteen deck cards: the six Tier II, the six Tier III and the Ultimate.</summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            ErraticFireblast, CrackOfDoom, LooselyAimedFirebolts, ImminentEruption,
            SpontaneousImmolation, SearingHeat, ViolentConflagration, ScorchingBlaze,
            PathOfCinders, UnstablePortal, PathOfFlames, ChaosGate,
            ChaosIncarnate
        });

        /// <summary>p.6 returns the Handicap to the box; p.20 brings it back only for an odd player count.</summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            UnreliableJolt
        });

        /// <summary>All nineteen faces, in the inventory table's order.</summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions { get; } = Array.AsReadOnly(
            Profiles().Select(Build).ToArray());

        /// <summary>The eighteen a match deals from - everything but the boxed Handicap.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions { get; } = Array.AsReadOnly(
            AllDefinitions.Where(d => !ReturnedToBoxAtSetup.Contains(d.Id)).ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(d => d.Id.Equals(id)) ??
            throw new ArgumentException($"Ignatia prints no card '{id}'.", nameof(id));

        private static CardDefinitionId Id(string cardId) =>
            new CardDefinitionId($"{ContentSetId}-{cardId}");

        // -----------------------------------------------------------------------------------------
        // The printed faces, one row per card in inventory order.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<IgnatiaCardProfile> Profiles() => new[]
        {
            // Gold basic. The coin picks the target itself: ☀ a minion adjacent to you, ☾ a hero in range.
            new IgnatiaCardProfile(
                ChaosBolt, "Chaos Bolt", 11, 2, 1,
                IgnatiaPrimaryKind.RangedAttack, 2, PrintedValueChannel.Range, 3,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "☀: Target a minion adjacent to you. ☾: Target a hero in range. " +
                "( Apply the text matching the symbol on the Tie Breaker coin ).",
                PrintedTextComposition.AwaitedAt("8.75a"),
                targetBoundByCardText: true),

            // Silver basic. Frees her from the coin for the round.
            new IgnatiaCardProfile(
                Equilibrium, "Equilibrium", 1, 7, 0,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "This round: Each time you perform or repeat a primary action you may apply either ☀ " +
                "or ☾ card text, regardless of the Tie Breaker coin.",
                PrintedTextComposition.AwaitedAt("8.75b")),

            // Red I, forking into the straight-line line and the adjacent/max-range line.
            new IgnatiaCardProfile(
                PlayingWithFire, "Playing with Fire", 7, 4, 3,
                IgnatiaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    new[] { ErraticFireblast, CrackOfDoom }),
                "☀: Target a unit in range not in a straight line. " +
                "☾: Target a unit in range in a straight line.",
                PrintedTextComposition.AwaitedAt("8.75c"),
                targetBoundByCardText: true),

            new IgnatiaCardProfile(
                ErraticFireblast, "Erratic Fireblast", 7, 5, 3,
                IgnatiaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    new[] { LooselyAimedFirebolts, ImminentEruption }),
                "☀: Target a unit in range not in a straight line. " +
                "☾: Target a unit in range in a straight line.",
                PrintedTextComposition.AwaitedAt("8.75c"),
                targetBoundByCardText: true),

            new IgnatiaCardProfile(
                CrackOfDoom, "Crack of Doom", 7, 6, 3,
                IgnatiaPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 5,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    new[] { LooselyAimedFirebolts, ImminentEruption }),
                "☀: Target a unit adjacent to you. ☾: Target a unit at maximum range.",
                PrintedTextComposition.AwaitedAt("8.75c"),
                targetBoundByCardText: true),

            new IgnatiaCardProfile(
                LooselyAimedFirebolts, "Loosely-Aimed Firebolts", 8, 5, 3,
                IgnatiaPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius,
                    Array.Empty<CardDefinitionId>()),
                "☀: Target a unit in range not in a straight line. " +
                "☾: Target a unit in range in a straight line. May repeat once on a different hero.",
                PrintedTextComposition.AwaitedAt("8.75c"),
                targetBoundByCardText: true),

            new IgnatiaCardProfile(
                ImminentEruption, "Imminent Eruption", 8, 6, 3,
                IgnatiaPrimaryKind.RangedAttack, 6, PrintedValueChannel.Range, 5,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Initiative,
                    Array.Empty<CardDefinitionId>()),
                "☀: Target a unit adjacent to you. May repeat once on a minion. " +
                "☾: Target a unit at maximum range.",
                PrintedTextComposition.AwaitedAt("8.75c"),
                targetBoundByCardText: true),

            // Blue I, forking into the discard line and the push line.
            new IgnatiaCardProfile(
                AbruptCombustion, "Abrupt Combustion", 9, 3, 3,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    new[] { SpontaneousImmolation, SearingHeat }),
                "☀: An enemy hero in radius, adjacent to a token, or a minion, discards a card, if " +
                "able. ☾: Remove an enemy minion in radius adjacent to an enemy hero.",
                PrintedTextComposition.AwaitedAt("8.75d")),

            new IgnatiaCardProfile(
                SpontaneousImmolation, "Spontaneous Immolation", 9, 4, 3,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    new[] { ViolentConflagration }),
                "☀: An enemy hero in radius adjacent to a token or to a minion discards a card, if " +
                "able. ☾: Remove an enemy minion in radius adjacent to an enemy hero.",
                PrintedTextComposition.AwaitedAt("8.75d")),

            new IgnatiaCardProfile(
                SearingHeat, "Searing Heat", 9, 4, 3,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Initiative,
                    new[] { ScorchingBlaze }),
                "☀: Move a friendly hero in radius 2 spaces in a straight line. " +
                "☾: Move an enemy hero in radius 2 spaces in a straight line.",
                PrintedTextComposition.AwaitedAt("8.75d")),

            new IgnatiaCardProfile(
                ViolentConflagration, "Violent Conflagration", 10, 4, 3,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range,
                    Array.Empty<CardDefinitionId>()),
                "☀: An enemy hero in radius, adjacent to a token, or a minion, discards a card, or is " +
                "defeated. ☾: Defeat an enemy minion in radius adjacent to an enemy hero.",
                PrintedTextComposition.AwaitedAt("8.75d")),

            new IgnatiaCardProfile(
                ScorchingBlaze, "Scorching Blaze", 10, 4, 3,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Movement,
                    Array.Empty<CardDefinitionId>()),
                "☀: Move a friendly hero in radius 2 or 3 spaces in a straight line. " +
                "☾: Move an enemy hero in radius 2 or 3 spaces in a straight line.",
                PrintedTextComposition.AwaitedAt("8.75d")),

            // Green I, forking into the magma line and the swap line.
            new IgnatiaCardProfile(
                PathOfAshes, "Path of Ashes", 5, 2, 2,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    new[] { PathOfCinders, UnstablePortal }),
                "☀: Move up to 2 spaces in a straight line. Place a magma token in each empty space " +
                "you moved through, or out of. ☾: Place up to 2 magma tokens in radius.",
                PrintedTextComposition.AwaitedAt("8.75e")),

            new IgnatiaCardProfile(
                PathOfCinders, "Path of Cinders", 5, 2, 2,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Defense,
                    new[] { PathOfFlames }),
                "☀: Move up to 3 spaces in a straight line. Place a magma token in each empty space " +
                "you moved through, or out of. ☾: Place up to 3 magma tokens in radius.",
                PrintedTextComposition.AwaitedAt("8.75e")),

            new IgnatiaCardProfile(
                UnstablePortal, "Unstable Portal", 5, 2, 2,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    new[] { ChaosGate }),
                "☀: Swap with a friendly unit in radius. ☾: Swap with an enemy unit in radius.",
                PrintedTextComposition.AwaitedAt("8.75e")),

            new IgnatiaCardProfile(
                PathOfFlames, "Path of Flames", 4, 3, 2,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Defense,
                    Array.Empty<CardDefinitionId>()),
                "☀: Move up to 4 spaces in a straight line. Place a magma token in each empty space " +
                "you moved through, or out of. ☾: Place up to 4 magma tokens in radius.",
                PrintedTextComposition.AwaitedAt("8.75e")),

            new IgnatiaCardProfile(
                ChaosGate, "Chaos Gate", 4, 3, 2,
                IgnatiaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack,
                    Array.Empty<CardDefinitionId>()),
                "☀: Swap with a friendly unit in radius; you may move that unit 1 space. " +
                "☾: Swap with an enemy unit in radius; you may move 1 space.",
                PrintedTextComposition.AwaitedAt("8.75e")),

            // Purple Ultimate. A triggered ability, awaiting composition.
            new IgnatiaCardProfile(
                ChaosIncarnate, "Chaos Incarnate", 0, 0, 0,
                IgnatiaPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "The first time each turn after you perform a primary action you may flip the Tie " +
                "Breaker coin; if you do, you may perform that action again, choosing different targets.",
                PrintedTextComposition.None),

            // Handicap, deferred exactly as every other hero's boxed card is. Both bullets name a hero.
            new IgnatiaCardProfile(
                UnreliableJolt, "Unreliable Jolt", 10, 1, 1,
                IgnatiaPrimaryKind.RangedAttack, 1, PrintedValueChannel.Range, 2,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "☀: Target a hero adjacent to you. ☾: Target a hero in range, not adjacent to you.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                targetableUnits: AttackTargetableUnits.HeroesOnly,
                targetBoundByCardText: true)
        };

        private static CardDefinition Build(IgnatiaCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == IgnatiaPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                profile.Progression.IsUltimate
                    // Chaos Incarnate's triggered flip-and-repeat is minted in a later 8.75 slice;
                    // its rule kind is declared here as owed so the Ultimate ships whole as inventory.
                    ? UltimateRuleDefinition.AwaitingComposition(
                        "ignatia-chaos-incarnate",
                        "IgnatiaChaosIncarnate",
                        PrintedTextComposition.AwaitedAt("8.75f"),
                        Enumerable.Empty<PrintedValueDefinition>())
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText),
                // Milestone 8.75b. The typed statement a surface reads to say WHICH of this face's two
                // printed texts is live - never the transcription above, which no production type may
                // touch. It is read off the composed body rather than listed here, so a face cannot
                // declare one thing and resolve another; see IgnatiaEffectContent.PrintedTextSelectionOf.
                IgnatiaEffectContent.PrintedTextSelectionOf(
                    IgnatiaEffectContent.PrimaryBody(profile.Id, ContentRevision)));

        private static IEnumerable<ActionDefinition> BuildActions(IgnatiaCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

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

        private static ActionDefinition BuildPrimary(IgnatiaCardProfile profile)
        {
            switch (profile.Primary)
            {
                case IgnatiaPrimaryKind.None:
                    return null;

                case IgnatiaPrimaryKind.RangedAttack:
                {
                    // Milestone 8.75x. A composed body replaces the awaited placeholder as each shard
                    // lands; a card still awaiting composition keeps its owed statement.
                    var body = IgnatiaEffectContent.PrimaryBody(profile.Id, ContentRevision);
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        body,
                        body != null ? PrintedTextComposition.None : profile.TextComposition,
                        PrintedDefenseResolution.None,
                        // A card-text-bound Attack target is chosen at p.14 step 1, so the body applies
                        // before the attack; a face still awaiting composition keeps None.
                        body != null ? CardTextTiming.BeforeTheAttack : CardTextTiming.None,
                        TargetPolicy(profile, AttackRangeKind.Ranged, body != null),
                        null,
                        MinionDefenseContributionPolicy.IncludeAll);
                }

                case IgnatiaPrimaryKind.Skill:
                {
                    var body = IgnatiaEffectContent.PrimaryBody(profile.Id, ContentRevision);
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        body,
                        body != null ? PrintedTextComposition.None : profile.TextComposition);
                }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Ignatia primary action kind.");
            }
        }

        private static PrintedAttackTargetPolicy? TargetPolicy(
            IgnatiaCardProfile profile, AttackRangeKind attackRange, bool hasComposedBody)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            if (profile.TargetableUnits != AttackTargetableUnits.HeroesAndMinions)
            {
                classification = new PrintedAttackTargetPolicy(
                    profile.TargetableUnits, classification.Reach, classification.Line);
            }

            // Her attacks name their own target - the coin picks a minion adjacent or a hero in range,
            // a unit in/out of a straight line, at maximum range - so p.14 reaches step 1 with no
            // target and the composed body binds one through set-attack-target. The card-text binding
            // is declared once the body exists; a face still awaiting composition keeps p.13's default.
            if (profile.TargetBoundByCardText && hasComposedBody)
            {
                return classification.BoundByCardText();
            }

            return profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions
                ? (PrintedAttackTargetPolicy?)null
                : classification;
        }

        private static IEnumerable<PrintedValueDefinition> PrintedIcon(IgnatiaCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum IgnatiaPrimaryKind
        {
            None = 0,
            RangedAttack = 1,
            Skill = 2
        }

        private sealed class IgnatiaCardProfile
        {
            public IgnatiaCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                IgnatiaPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
                bool targetBoundByCardText = false)
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
                TargetBoundByCardText = targetBoundByCardText;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public IgnatiaPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public AttackTargetableUnits TargetableUnits { get; }
            public bool TargetBoundByCardText { get; }
        }
    }
}
