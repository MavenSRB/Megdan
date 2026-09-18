using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Nineteen source-verified fronts; HERO_WIDGET owns the literal source inventory.</summary>
    public static class WidgetCardContent
    {
        public const string ContentRevision = "widget-8-43d-rev-1";
        public const string ContentSetId = "widget";
        public static CardDefinitionId FightAsOne { get; } = Id("fight-as-one");
        public static CardDefinitionId DragonBond { get; } = Id("dragon-bond");
        public static CardDefinitionId DiversionaryStrike { get; } = Id("diversionary-strike");
        public static CardDefinitionId DiversionaryAttack { get; } = Id("diversionary-attack");
        public static CardDefinitionId AirborneAttack { get; } = Id("airborne-attack");
        public static CardDefinitionId DiversionaryAssault { get; } = Id("diversionary-assault");
        public static CardDefinitionId AirborneAssault { get; } = Id("airborne-assault");
        public static CardDefinitionId AllAboard { get; } = Id("all-aboard");
        public static CardDefinitionId TakeOff { get; } = Id("take-off");
        public static CardDefinitionId DragOff { get; } = Id("drag-off");
        public static CardDefinitionId SafeLanding { get; } = Id("safe-landing");
        public static CardDefinitionId CarryAway { get; } = Id("carry-away");
        public static CardDefinitionId FieryBreath { get; } = Id("fiery-breath");
        public static CardDefinitionId FlamingBreath { get; } = Id("flaming-breath");
        public static CardDefinitionId Nibble { get; } = Id("nibble");
        public static CardDefinitionId ScorchingBreath { get; } = Id("scorching-breath");
        public static CardDefinitionId Gnaw { get; } = Id("gnaw");
        public static CardDefinitionId DragonKnight { get; } = Id("dragon-knight");
        public static CardDefinitionId FollowAlong { get; } = Id("follow-along");
        public static ActionDefinitionId PrimaryBasicAttack { get; } = new ActionDefinitionId("primary-basic-attack");
        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId PrimaryBasicSkill { get; } = new ActionDefinitionId("primary-basic-skill");
        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } = new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } = new ActionDefinitionId("secondary-defense");
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
            { FightAsOne, DragonBond, DiversionaryStrike, AllAboard, FieryBreath });
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
            { DiversionaryAttack, AirborneAttack, DiversionaryAssault, AirborneAssault, TakeOff, DragOff,
                SafeLanding, CarryAway, FlamingBreath, Nibble, ScorchingBreath, Gnaw, DragonKnight });
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[] { FollowAlong });
        private static readonly Lazy<IReadOnlyList<CardDefinition>> All =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(BuildSet().ToArray()));
        private static readonly Lazy<IReadOnlyList<CardDefinition>> Dealt =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(All.Value.Where(c => !ReturnedToBoxAtSetup.Contains(c.Id)).ToArray()));
        public static IReadOnlyList<CardDefinition> AllDefinitions => All.Value;
        public static IReadOnlyList<CardDefinition> DealtDefinitions => Dealt.Value;
        public static CardDefinition Definition(CardDefinitionId id) => AllDefinitions.Single(c => c.Id.Equals(id));
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined) throw new ArgumentException("Cards require an owner.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(instanceIdPrefix)) throw new ArgumentException("Cards require a per-seat identity prefix.", nameof(instanceIdPrefix));
            return Array.AsReadOnly(DealtDefinitions.Select(c => StartingHand.Contains(c.Id)
                ? CardInstance.CreateInHand(new CardInstanceId(instanceIdPrefix + "-" + c.Id.Value), c.Reference, ownerId)
                : CardInstance.CreateInDeck(new CardInstanceId(instanceIdPrefix + "-" + c.Id.Value), c.Reference, ownerId)).ToArray());
        }
        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(DealtDefinitions, cards.Where(c => StartingHand.Contains(c.DefinitionReference.DefinitionId)),
                cards.Where(c => Deck.Contains(c.DefinitionReference.DefinitionId)));
        }
        private static CardDefinitionId Id(string value) => new CardDefinitionId(ContentSetId + "-" + value);
        private static CardProgressionProfile Red(CardTier tier, PrintedValueChannel item) =>
            Progression(CardColor.Red, tier, item, DiversionaryAttack, AirborneAttack, DiversionaryAssault, AirborneAssault);
        private static CardProgressionProfile Blue(CardTier tier, PrintedValueChannel item) =>
            Progression(CardColor.Blue, tier, item, TakeOff, DragOff, SafeLanding, CarryAway);
        private static CardProgressionProfile Green(CardTier tier, PrintedValueChannel item) =>
            Progression(CardColor.Green, tier, item, FlamingBreath, Nibble, ScorchingBreath, Gnaw);
        private static CardProgressionProfile Progression(CardColor color, CardTier tier, PrintedValueChannel item,
            CardDefinitionId secondA, CardDefinitionId secondB, CardDefinitionId thirdA, CardDefinitionId thirdB) =>
            tier == CardTier.I ? CardProgressionProfile.NonBasicCard(color, tier, item, secondA, secondB) :
            tier == CardTier.II ? CardProgressionProfile.NonBasicCard(color, tier, item, thirdA, thirdB) :
            CardProgressionProfile.NonBasicCard(color, tier, item);

        private static IEnumerable<CardDefinition> BuildSet()
        {
            yield return Build(FightAsOne, "Fight as One", 11, 2, 1, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. After the attack: You may perform the primary action on your resolved Skill card, targeting a different unit.",
                WidgetEffectContent.FightAsOne(FightAsOne, ContentRevision), CardTextTiming.AfterTheAttack);
            yield return Build(DragonBond, "Dragon Bond", 6, 6, 0, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Choose one - (1) Place Pyro into a space in radius. Pyro counts as a token, but is not removed at the end of round. (2) If Pyro is in play, move both yourself and Pyro up to 2 spaces, in any order.",
                WidgetEffectContent.DragonBond(DragonBond, ContentRevision));
            yield return Build(DiversionaryStrike, "Diversionary Strike", 8, 6, 4, 5, PrintedValueChannel.None, 0,
                Red(CardTier.I, PrintedValueChannel.None), DiversionText(2),
                WidgetEffectContent.MovePyroAfterAttack(DiversionaryStrike, ContentRevision, 2), CardTextTiming.AfterTheAttack);
            yield return Build(DiversionaryAttack, "Diversionary Attack", 8, 6, 4, 5, PrintedValueChannel.None, 0,
                Red(CardTier.II, PrintedValueChannel.Initiative), DiversionText(3),
                WidgetEffectContent.MovePyroAfterAttack(DiversionaryAttack, ContentRevision, 3), CardTextTiming.AfterTheAttack);
            yield return Build(AirborneAttack, "Airborne Attack", 8, 4, 4, 3, PrintedValueChannel.Radius, 3,
                Red(CardTier.II, PrintedValueChannel.Defense), AirborneText(false),
                WidgetEffectContent.AirborneBeforeAttack(AirborneAttack, ContentRevision), CardTextTiming.BeforeTheAttack, bindsTarget: true);
            yield return Build(DiversionaryAssault, "Diversionary Assault", 9, 7, 4, 6, PrintedValueChannel.None, 0,
                Red(CardTier.III, PrintedValueChannel.Initiative), DiversionText(4),
                WidgetEffectContent.MovePyroAfterAttack(DiversionaryAssault, ContentRevision, 4), CardTextTiming.AfterTheAttack);
            yield return Build(AirborneAssault, "Airborne Assault", 9, 5, 4, 4, PrintedValueChannel.Radius, 3,
                Red(CardTier.III, PrintedValueChannel.Range), AirborneText(true),
                WidgetEffectContent.AirborneBeforeAttack(AirborneAssault, ContentRevision), CardTextTiming.BeforeTheAttack, bindsTarget: true,
                after: WidgetEffectContent.SwapWithPyro(AirborneAssault, ContentRevision, "after-attack"));
            yield return Build(AllAboard, "All Aboard", 9, 3, 3, 0, PrintedValueChannel.Range, 2,
                Blue(CardTier.I, PrintedValueChannel.None), AboardText, WidgetEffectContent.AllAboard(AllAboard, ContentRevision, false));
            yield return Build(TakeOff, "Take-Off", 10, 3, 3, 0, PrintedValueChannel.Range, 3,
                Blue(CardTier.II, PrintedValueChannel.Defense), AboardText, WidgetEffectContent.AllAboard(TakeOff, ContentRevision, false));
            yield return Build(DragOff, "Drag Off", 10, 3, 3, 0, PrintedValueChannel.None, 0,
                Blue(CardTier.II, PrintedValueChannel.Attack), DragText(false), WidgetEffectContent.DragOff(DragOff, ContentRevision, false));
            yield return Build(SafeLanding, "Safe Landing", 10, 4, 3, 0, PrintedValueChannel.Range, 3,
                Blue(CardTier.III, PrintedValueChannel.Defense), "You may move Pyro in range 1 space. " + AboardText,
                WidgetEffectContent.AllAboard(SafeLanding, ContentRevision, true));
            yield return Build(CarryAway, "Carry Away", 10, 4, 3, 0, PrintedValueChannel.None, 0,
                Blue(CardTier.III, PrintedValueChannel.Radius), DragText(true), WidgetEffectContent.DragOff(CarryAway, ContentRevision, true));
            yield return Build(FieryBreath, "Fiery Breath", 4, 1, 2, 0, PrintedValueChannel.Range, 2,
                Green(CardTier.I, PrintedValueChannel.None), BreathText(false), WidgetEffectContent.Breath(FieryBreath, ContentRevision, false));
            yield return Build(FlamingBreath, "Flaming Breath", 4, 2, 2, 0, PrintedValueChannel.Range, 3,
                Green(CardTier.II, PrintedValueChannel.Attack), BreathText(false), WidgetEffectContent.Breath(FlamingBreath, ContentRevision, false));
            yield return Build(Nibble, "Nibble", 4, 2, 2, 0, PrintedValueChannel.Range, 3,
                Green(CardTier.II, PrintedValueChannel.Initiative), "Remove an enemy minion in range adjacent to Pyro. Remove Pyro.",
                WidgetEffectContent.Nibble(Nibble, ContentRevision, false));
            yield return Build(ScorchingBreath, "Scorching Breath", 3, 2, 2, 0, PrintedValueChannel.Range, 3,
                Green(CardTier.III, PrintedValueChannel.Attack), BreathText(true), WidgetEffectContent.Breath(ScorchingBreath, ContentRevision, true));
            yield return Build(Gnaw, "Gnaw", 3, 2, 2, 0, PrintedValueChannel.Range, 3,
                Green(CardTier.III, PrintedValueChannel.Movement), "Defeat a minion in range adjacent to Pyro. Remove Pyro.",
                WidgetEffectContent.Nibble(Gnaw, ContentRevision, true));
            yield return new CardDefinition(DragonKnight, ContentRevision, "Dragon Knight", default,
                CardContentAuthority.VerifiedOfficial, CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.TriggeredAbility("dragon-knight-movement", "MovementSkillPrimary",
                    WidgetEffectContent.DragonKnight(DragonKnight, ContentRevision).Reference,
                    UltimateTriggerCondition.AfterControllerPerformsMovementAction, new[] { TimingCheckpoint.ActionEnded },
                    Array.Empty<PrintedValueDefinition>()),
                CardSourceContent.Transcribed("Each time after you perform a movement action, you may perform the primary action on your face-up Skill card."));
            yield return Build(FollowAlong, "Follow Along", 10, 1, 2, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. After the attack: You may perform the primary action on your resolved Skill card, targeting a different unit.", boxed: true);
        }
        private const string AboardText = "Swap Pyro in range with yourself, or with a friendly hero in range.";
        private static string DiversionText(int distance) => "Target a unit adjacent to you. After the attack: Move Pyro up to " + distance + " spaces.";
        private static string AirborneText(bool after) => "Before the attack: You may swap places with Pyro in radius. Target a unit adjacent to you." +
            (after ? " After the attack: You may swap places with Pyro in radius." : "");
        private static string DragText(bool ignores) => "Move both Pyro, and an enemy unit adjacent to Pyro either 2 or 3 spaces each, in the same direction" +
            (ignores ? ", ignoring obstacles." : ".");
        private static string BreathText(bool defeat) => "An enemy hero both in range and in a straight line from Pyro discards a card, " +
            (defeat ? "or is defeated." : "if able.");

        private static CardDefinition Build(CardDefinitionId id, string name, int initiative, int defense, int movement,
            int attack, PrintedValueChannel icon, int reach, CardProgressionProfile progression, string text,
            EffectDefinition effect = null, CardTextTiming timing = CardTextTiming.None, bool bindsTarget = false,
            EffectDefinition after = null, bool boxed = false)
        {
            var values = new List<PrintedValueDefinition>();
            if (attack > 0) values.Add(new PrintedValueDefinition(PrintedValueChannel.Attack, attack));
            if (icon != PrintedValueChannel.None) values.Add(new PrintedValueDefinition(icon, reach));
            var basic = progression.Classification == CardClassification.Basic;
            var primaryId = attack > 0 ? (basic ? PrimaryBasicAttack : PrimaryAttack) : (basic ? PrimaryBasicSkill : PrimarySkill);
            var target = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged);
            var actions = new List<ActionDefinition>
            {
                new ActionDefinition(primaryId, attack > 0 ? ActionKind.Attack : ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary, values,
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    attack > 0 ? AttackRangeKind.NonRanged : AttackRangeKind.None, default, effect,
                    boxed ? PrintedTextComposition.DeferredUntil("first odd-player-count configuration") : PrintedTextComposition.None,
                    cardTextTiming: timing, attackTargetPolicy: attack > 0 ? (bindsTarget ? target.BoundByCardText() : target) : (PrintedAttackTargetPolicy?)null,
                    secondCardTextRoot: after, secondCardTextTiming: after == null ? CardTextTiming.None : CardTextTiming.AfterTheAttack)
            };
            if (movement > 0) actions.Add(new ActionDefinition(SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movement) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)));
            if (defense > 0) actions.Add(new ActionDefinition(SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false), AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)));
            return new CardDefinition(id, ContentRevision, name, new InitiativeValue(initiative), CardContentAuthority.VerifiedOfficial,
                progression, actions, null, CardSourceContent.Transcribed(text));
        }
    }
}
