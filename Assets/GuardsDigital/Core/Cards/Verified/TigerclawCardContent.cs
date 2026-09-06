using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Tigerclaw the Cutpurse's nineteen printed faces as definition content - milestone 8.23, and the
    /// fifth <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.18d and recorded in
    /// <c>Docs/AI/Rules/HERO_TIGERCLAW.md</c>. The tables there and the rows here are meant to be read
    /// side by side: the order, the names and the column meanings match, so a reviewer can audit one
    /// against the other without holding anything in their head. Nothing is derived, for the reason
    /// <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>What is different from the four sets already built, and therefore what this file is the
    /// first to state.</b> All five of his Blue cards are p.14 Blocks and they print <em>both</em> arms
    /// of the page's condition - Dodge, Sidestep and Evade block a ranged attack, Parry and Riposte
    /// block a non-ranged one. Every Block before his printed the ranged arm, which is why milestone
    /// 8.11 could read the condition off the attack directly and this milestone had to move it onto the
    /// card as <see cref="PrintedBlockCondition"/>. He is also the first hero with a marker of his own:
    /// Poisoned Dagger and Poisoned Dart give the Poison marker, whose two strengths are the same one
    /// physical supply. Backstab with a Ballista is the first face to restrict the <em>origin</em> of a
    /// Defense action rather than its value. And Cloak and Daggers is the first Ultimate whose ability
    /// repeats an action from outside it.</para>
    ///
    /// <para><b>Every non-Ultimate face but one prints Movement.</b> Seventeen of the eighteen
    /// non-Ultimate faces carry a secondary Movement icon, so p.11's Fast Travel replacement is
    /// reachable from every card that can enter his hand except Blend Into Shadows - a fact about his
    /// set rather than a rule this file states. Blend Into Shadows prints a Defense secondary instead;
    /// milestone 8.35z20 corrected this file's earlier transposition of the two (recorded in
    /// <c>Docs/AI/Rules/HERO_TIGERCLAW.md</c>'s settled-corrections block).</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class TigerclawCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="SabinaCardContent.ContentRevision"/> has: it moves when Tigerclaw's content does.
        /// Milestone 8.35z20 moved it again to correct Blend Into Shadows' secondary icon (Defense 2,
        /// not Movement 2 - see <c>Docs/AI/Rules/HERO_TIGERCLAW.md</c>'s settled-corrections block).
        /// </summary>
        public const string ContentRevision = "tigerclaw-8-35z20-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself, and the identity his prepared token is addressed by.
        /// </summary>
        public const string ContentSetId = "tigerclaw";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId BlinkStrike { get; } = Id("blink-strike");
        public static CardDefinitionId NimbleStrike { get; } = Id("nimble-strike");
        public static CardDefinitionId BlendIntoShadows { get; } = Id("blend-into-shadows");
        public static CardDefinitionId HitAndRun { get; } = Id("hit-and-run");
        public static CardDefinitionId CombatReflexes { get; } = Id("combat-reflexes");
        public static CardDefinitionId Backstab { get; } = Id("backstab");
        public static CardDefinitionId LeapingStrike { get; } = Id("leaping-strike");
        public static CardDefinitionId BackstabWithABallista { get; } = Id("backstab-with-a-ballista");
        public static CardDefinitionId Dodge { get; } = Id("dodge");
        public static CardDefinitionId Sidestep { get; } = Id("sidestep");
        public static CardDefinitionId Parry { get; } = Id("parry");
        public static CardDefinitionId Evade { get; } = Id("evade");
        public static CardDefinitionId Riposte { get; } = Id("riposte");
        public static CardDefinitionId LightFingered { get; } = Id("light-fingered");
        public static CardDefinitionId PickPocket { get; } = Id("pick-pocket");
        public static CardDefinitionId PoisonedDagger { get; } = Id("poisoned-dagger");
        public static CardDefinitionId MasterThief { get; } = Id("master-thief");
        public static CardDefinitionId PoisonedDart { get; } = Id("poisoned-dart");
        public static CardDefinitionId CloakAndDaggers { get; } = Id("cloak-and-daggers");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the four earlier sets' are. Tigerclaw
        // needs six: he is the first hero printing a Defense primary, and the first printing a Skill
        // that carries a Range icon.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        /// <summary>
        /// p.14's Block, which is a Defense action printed as the card's primary. Its identity is
        /// separate from the ordinary secondary Defense below because the two are different actions on
        /// different cards: no face of his carries both.
        /// </summary>
        public static ActionDefinitionId PrimaryBlock { get; } = new ActionDefinitionId("primary-block");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Blink Strike.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            BlinkStrike, BlendIntoShadows, HitAndRun, Dodge, LightFingered
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            CombatReflexes, Backstab, LeapingStrike, BackstabWithABallista,
            Sidestep, Parry, Evade, Riposte,
            PickPocket, PoisonedDagger, MasterThief, PoisonedDart,
            CloakAndDaggers
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            NimbleStrike
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_TIGERCLAW.md</c>'s printed-value table lists
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
            throw new ArgumentException($"Tigerclaw prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Tigerclaw's cards require a defined owner.", nameof(ownerId));
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

        private static IEnumerable<TigerclawCardProfile> Profiles() => new[]
        {
            // The move-through-then-target shape, and the only face in the verified roster that prints
            // its own mandatory-step reminder. p.10 already applies that rule to every mandatory step;
            // the parenthesis restates it rather than creating it, which is why it composes to nothing
            // of its own.
            new TigerclawCardProfile(
                BlinkStrike, "Blink Strike", 13, 1, 1,
                TigerclawPrimaryKind.Attack, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Before the attack: Move 2 spaces in a straight line through an enemy unit; target " +
                "that unit. (If you cannot make this move, you cannot attack).",
                PrintedTextComposition.None,
                bindsTargetInCardText: true),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward and Sabina's Bang are, by the same 2026-08-21 user decision:
            // p.6 returns it to the box and the six-player pitch never brings it back. Its printed
            // values and words ship here as inventory all the same. The one thing it says that Blink
            // Strike does not is on the row rather than in the deferral: it crosses an enemy HERO where
            // the ordinary Gold card crosses any enemy unit.
            new TigerclawCardProfile(
                NimbleStrike, "Nimble Strike", 12, 1, 1,
                TigerclawPrimaryKind.Attack, 2, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Before the attack: Move 2 spaces in a straight line through an enemy hero; target " +
                "that hero.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                targetableUnits: AttackTargetableUnits.HeroesOnly,
                bindsTargetInCardText: true),

            // His only basic Skill, and his only p.17 Active effect. The immunity starts the moment the
            // Place applies and applies on his next turn; p.8 cancels it at end-of-round card retrieval
            // if that turn never comes. The face's single secondary ribbon icon is a riveted Defense
            // shield printing 2, in the ribbon's lower slot - not a Movement boot. Milestone 8.35z20
            // corrected the transposition the one-icon slot-position trap caused (a lone secondary icon
            // sits in the lower slot whatever its type; the upper-slot convention only holds for a
            // two-icon card). Full evidence: Docs/AI/Rules/HERO_TIGERCLAW.md's settled-corrections block.
            new TigerclawCardProfile(
                BlendIntoShadows, "Blend Into Shadows", 6, 2, 0,
                TigerclawPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "If you are adjacent to terrain, place yourself into a space in radius; if you do, " +
                "Next turn: You are immune to enemy attack actions.",
                PrintedTextComposition.None),

            // The Red line. Hit and Run, Combat Reflexes and Leaping Strike are one family of optional
            // movement around an adjacent Attack; what separates them is which side of the attack the
            // movement is offered on and, for Combat Reflexes, that the second offer is conditioned on
            // the first not having been taken.
            new TigerclawCardProfile(
                HitAndRun, "Hit and Run", 9, 3, 4,
                TigerclawPrimaryKind.Attack, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    CombatReflexes, Backstab),
                "Target a unit adjacent to you. After the attack: You may move 1 space.",
                PrintedTextComposition.None),

            new TigerclawCardProfile(
                CombatReflexes, "Combat Reflexes", 9, 3, 4,
                TigerclawPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    LeapingStrike, BackstabWithABallista),
                "Before the attack: You may move 1 space. Target a unit adjacent to you. After the " +
                "attack: If you did not move before the attack, you may move 1 space.",
                PrintedTextComposition.None),

            // The two faces printing p.14's `+` convention on the Attack value: a number the card's own
            // text raises, which composes to a declared value contribution rather than to an effect
            // node because a conditional addition to a total is not a step.
            new TigerclawCardProfile(
                Backstab, "Backstab", 9, 5, 5,
                TigerclawPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    LeapingStrike, BackstabWithABallista),
                "Target a unit adjacent to you; If a friendly unit is adjacent to the target, +2 " +
                "[Attack] Attack. (A “friendly unit” is another hero or a minion on your team).",
                PrintedTextComposition.None,
                raisedAttack: 2),

            new TigerclawCardProfile(
                LeapingStrike, "Leaping Strike", 10, 4, 4,
                TigerclawPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Before the attack: You may move 1 space. Target a unit adjacent to you. After the " +
                "attack: You may move 1 space.",
                PrintedTextComposition.None),

            // His only Ranged Attack, his only Attack printing a Range icon, and the verified roster's
            // first restriction on the ORIGIN of a Defense action: the target cannot use a primary
            // action to defend, while p.14's ordinary secondary Defense remains legal.
            new TigerclawCardProfile(
                BackstabWithABallista, "Backstab with a Ballista", 10, 6, 5,
                TigerclawPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                "Target a unit in range; If a friendly unit is adjacent to the target +2 [Attack] " +
                "Attack and the target cannot perform a primary action to defend.",
                PrintedTextComposition.None,
                raisedAttack: 2,
                forbidsPrimaryDefense: true),

            // The five Blocks. p.14 resolves each by its own printed condition, and this is the set
            // that prints both arms of it: the Ranged branch scales its rider from a step to a step
            // plus a retrieve, and the non-Ranged branch scales `if able` into p.10's defeat
            // escalation.
            new TigerclawCardProfile(
                Dodge, "Dodge", 10, 0, 3,
                TigerclawPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    Sidestep, Parry),
                "Block a ranged attack.",
                PrintedTextComposition.None,
                blockCondition: PrintedBlockCondition.AttackIsRanged),

            new TigerclawCardProfile(
                Sidestep, "Sidestep", 11, 0, 3,
                TigerclawPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    Evade, Riposte),
                "Block a ranged attack. You may move 1 space.",
                PrintedTextComposition.None,
                blockCondition: PrintedBlockCondition.AttackIsRanged),

            new TigerclawCardProfile(
                Parry, "Parry", 11, 0, 3,
                TigerclawPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    Evade, Riposte),
                "Block a non-ranged attack. The attacker discards a card, if able.",
                PrintedTextComposition.None,
                blockCondition: PrintedBlockCondition.AttackIsNotRanged),

            new TigerclawCardProfile(
                Evade, "Evade", 11, 0, 3,
                TigerclawPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "Block a ranged attack. You may move 1 space. You may retrieve your resolved or " +
                "discarded basic skill card.",
                PrintedTextComposition.None,
                blockCondition: PrintedBlockCondition.AttackIsRanged),

            // Ledger question 13's mandatory "discards a card, or is defeated" construction, answered
            // from source at milestone 8.9: the second arm is the consequence of inability, not a
            // choice. Parry supplies the `if able` contrast in the same colour pair.
            new TigerclawCardProfile(
                Riposte, "Riposte", 11, 0, 3,
                TigerclawPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "Block a non-ranged attack. The attacker discards a card, or is defeated.",
                PrintedTextComposition.None,
                blockCondition: PrintedBlockCondition.AttackIsNotRanged),

            // The Green line's first three: the verified roster's first cards that TAKE coins from an
            // enemy hero rather than gaining them from the supply. Each `if you do` movement depends on
            // the transfer having actually happened.
            new TigerclawCardProfile(
                LightFingered, "Light-Fingered", 2, 1, 3,
                TigerclawPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    PickPocket, PoisonedDagger),
                "You may move 1 space. Take 1 coin from an enemy hero adjacent to you; if you do, " +
                "you may move 1 space.",
                PrintedTextComposition.None),

            new TigerclawCardProfile(
                PickPocket, "Pick Pocket", 2, 1, 3,
                TigerclawPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    MasterThief, PoisonedDart),
                "Move up to 2 spaces. Take 1 coin from an enemy hero adjacent to you; if you do, you " +
                "may move 1 space.",
                PrintedTextComposition.None),

            // The two marker cards. They say "a hero" rather than "an enemy hero", so by p.10 the
            // target may be a friendly or an enemy hero but never Tigerclaw himself.
            new TigerclawCardProfile(
                PoisonedDagger, "Poisoned Dagger", 2, 1, 3,
                TigerclawPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    MasterThief, PoisonedDart),
                "Give a hero in range a [Poison] Poison marker. The hero with a poison marker has -1 " +
                "[Initiative] Initiative, -1 [Attack] Attack and -1 [Defense] Defense.",
                PrintedTextComposition.None),

            new TigerclawCardProfile(
                MasterThief, "Master Thief", 1, 2, 3,
                TigerclawPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Movement),
                "Move up to 2 spaces. Take 1 or 2 coins from an enemy hero adjacent to you; if you " +
                "do, you may move up to 2 spaces.",
                PrintedTextComposition.None),

            new TigerclawCardProfile(
                PoisonedDart, "Poisoned Dart", 1, 2, 3,
                TigerclawPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "Give a hero in range a [Poison] Poison marker. The hero with a poison marker has -2 " +
                "[Initiative] Initiative, -2 [Attack] Attack and -2 [Defense] Defense.",
                PrintedTextComposition.None),

            // p.16's permanent ability. It prints no Initiative, no icons and no actions at all, which
            // is what makes the row's zeros a statement rather than an omission.
            new TigerclawCardProfile(
                CloakAndDaggers, "Cloak and Daggers", 0, 0, 0,
                TigerclawPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "After you perform a basic action you may repeat it once; if you repeat an attack " +
                "action, you cannot target the same unit.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(TigerclawCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == TigerclawPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                profile.Id.Equals(CloakAndDaggers)
                    ? UltimateRuleDefinition.TriggeredAbility(
                        TigerclawEffectContent.CloakAndDaggersRuleId,
                        "TigerclawCloakAndDaggers",
                        TigerclawEffectContent.CloakAndDaggersTriggeredEffect(ContentRevision).Reference,
                        UltimateTriggerCondition.AfterControllerPerformsBasicAction,
                        new[] { Timing.TimingCheckpoint.ActionEnded },
                        Array.Empty<PrintedValueDefinition>())
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(TigerclawCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the four earlier sets express it. His
            // dashes are the five Blocks' Defense column, Blend Into Shadows' Defense column, and the
            // Ultimate's whole row.
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

        private static ActionDefinition BuildPrimary(TigerclawCardProfile profile)
        {
            switch (profile.Primary)
            {
                case TigerclawPrimaryKind.None:
                    return null;

                case TigerclawPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        TigerclawEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        TigerclawEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.NonRanged),
                        RaisedAttack(profile),
                        MinionDefenseContributionPolicy.IncludeAll,
                        DefenseRestriction(profile),
                        PrintedBlockCondition.None,
                        TigerclawEffectContent.SecondAttackTextFor(profile.Id, ContentRevision),
                        TigerclawEffectContent.SecondAttackTextTimingFor(profile.Id));

                case TigerclawPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        TigerclawEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        TigerclawEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged),
                        RaisedAttack(profile),
                        MinionDefenseContributionPolicy.IncludeAll,
                        DefenseRestriction(profile),
                        PrintedBlockCondition.None,
                        TigerclawEffectContent.SecondAttackTextFor(profile.Id, ContentRevision),
                        TigerclawEffectContent.SecondAttackTextTimingFor(profile.Id));

                case TigerclawPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        TigerclawEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case TigerclawPrimaryKind.Block:
                    // p.14's Block card. Milestone 8.11 settled what it spends and when it may be
                    // taken, on Wasp's three: a Defense action discarded from hand in response to an
                    // attack, printed as the card's primary and offered only into that reaction. What
                    // milestone 8.23 adds is the condition, which is this card's own word rather than
                    // p.14's - Parry and Riposte print the arm no earlier Block did.
                    return new ActionDefinition(
                        PrimaryBlock,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        TigerclawEffectContent.DefenseTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.Block,
                        TigerclawEffectContent.DefenseTextTimingFor(profile.Id),
                        blockCondition: profile.BlockCondition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Tigerclaw primary action kind.");
            }
        }

        /// <summary>
        /// p.14's printed <c>+</c>, as the typed declaration that makes it true - or nothing on the
        /// seventeen faces that print a plain number or none at all.
        ///
        /// <para>Backstab and Backstab with a Ballista print <c>5+</c> beside "If a friendly unit is
        /// adjacent to the target, +2 Attack". It is a value contribution rather than an effect node
        /// because it is not a step: nobody chooses anything, nothing happens on the board, and the
        /// clause is read when p.14 computes the Attack Total.</para>
        ///
        /// <para>The query excludes Tigerclaw himself, which is the parenthesised reminder's own
        /// sentence - "A 'friendly unit' is another hero or a minion on your team" - stated once in
        /// <c>RosterValueQueryKind.FriendlyUnitAdjacentToTargetExcludingActor</c> rather than restated
        /// here. It is a presence question and not a targeting one, so p.18's immunity does not enter
        /// it.</para>
        /// </summary>
        private static IEnumerable<RosterValueContributionDefinition> RaisedAttack(
            TigerclawCardProfile profile) =>
            profile.RaisedAttack == 0
                ? Enumerable.Empty<RosterValueContributionDefinition>()
                : new[]
                {
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Attack,
                        RosterValueQueryKind.FriendlyUnitAdjacentToTargetExcludingActor,
                        profile.RaisedAttack)
                };

        /// <summary>
        /// Backstab with a Ballista's "the target cannot perform a primary action to defend", as the
        /// typed restriction milestone 8.19b built for it, and nothing on every other face.
        ///
        /// <para>It names the <em>origin</em> rather than the card or the value, which is what the
        /// sentence says and what leaves p.14's ordinary secondary Defense untouched. Tigerclaw is the
        /// first hero to print it, and his own five Blocks are exactly the cards it would stop if a
        /// Ballista were ever pointed at him.</para>
        /// </summary>
        private static DefenseOfferOriginRestriction DefenseRestriction(TigerclawCardProfile profile) =>
            profile.ForbidsPrimaryDefense
                ? DefenseOfferOriginRestriction.PrintedPrimaryOnly
                : DefenseOfferOriginRestriction.None;

        /// <summary>
        /// What one printed Attack of Tigerclaw's may target, or <c>null</c> where the face narrows
        /// nothing and p.13's own reading for the subtype stands.
        ///
        /// <para>One of his seven Attacks states something p.13 does not: Nimble Strike prints "an
        /// enemy hero", which is the narrowing <see cref="AttackTargetableUnits.HeroesOnly"/> names.
        /// The rest print "a unit", which is p.13's own word.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            TigerclawCardProfile profile, AttackRangeKind attackRange)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            if (profile.TargetableUnits != AttackTargetableUnits.HeroesAndMinions)
            {
                classification = new PrintedAttackTargetPolicy(
                    profile.TargetableUnits, classification.Reach, classification.Line);
            }

            // Milestone 8.23. Blink Strike and Nimble Strike name their own target - "target that
            // unit", the one their step-1 line crossed - so p.14 does not ask the ordinary target
            // question first. That is `attack-target`, which milestone 8.19a minted for Brogan's
            // charges and which these two reach for the same printed reason.
            // Nimble Strike prints the same clause and is excluded, because p.6 returns it to the box
            // and a deferred face composes no step-1 text for the binding to be about. Its target
            // binding ships with its composition, exactly as its nodes do.
            if (profile.BindsTargetInCardText && !profile.TextComposition.IsAwaited)
            {
                return classification.BoundByCardText();
            }

            return profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions
                ? (PrintedAttackTargetPolicy?)null
                : classification;
        }

        /// <summary>The p.9 Range or Radius icon a face prints, or nothing where it prints neither.</summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(TigerclawCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum TigerclawPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3,

            /// <summary>p.14's Block card: a Defense primary that prints no Defense value.</summary>
            Block = 4
        }

        private sealed class TigerclawCardProfile
        {
            public TigerclawCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                TigerclawPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
                int raisedAttack = 0,
                bool forbidsPrimaryDefense = false,
                PrintedBlockCondition blockCondition = PrintedBlockCondition.None,
                bool bindsTargetInCardText = false)
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
                ForbidsPrimaryDefense = forbidsPrimaryDefense;
                BlockCondition = blockCondition;
                BindsTargetInCardText = bindsTargetInCardText;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public TigerclawPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public AttackTargetableUnits TargetableUnits { get; }

            /// <summary>The amount p.14's printed `+` adds when this face's condition holds.</summary>
            public int RaisedAttack { get; }

            /// <summary>Whether this Attack forbids its target a primary Defense action.</summary>
            public bool ForbidsPrimaryDefense { get; }

            /// <summary>The condition p.14 resolves this Block by, or None on every other face.</summary>
            public PrintedBlockCondition BlockCondition { get; }

            /// <summary>Whether this Attack's own step-1 text names the unit it attacks.</summary>
            public bool BindsTargetInCardText { get; }
        }
    }
}
