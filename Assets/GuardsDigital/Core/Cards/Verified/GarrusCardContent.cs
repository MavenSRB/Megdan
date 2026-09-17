using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Garrus's nineteen source-verified fronts. HERO_GARRUS owns the transcription;
    /// images and presentation names never participate in authoritative resolution.</summary>
    public static class GarrusCardContent
    {
        public const string ContentRevision = "garrus-8-41d-rev-1";
        public const string ContentSetId = "garrus";
        public static CardDefinitionId AngryStrike { get; } = Id("angry-strike");
        public static CardDefinitionId ChillingHowl { get; } = Id("chilling-howl");
        public static CardDefinitionId Trace { get; } = Id("trace");
        public static CardDefinitionId Chase { get; } = Id("chase");
        public static CardDefinitionId BluntForce { get; } = Id("blunt-force");
        public static CardDefinitionId HuntDown { get; } = Id("hunt-down");
        public static CardDefinitionId SendFlying { get; } = Id("send-flying");
        public static CardDefinitionId Menace { get; } = Id("menace");
        public static CardDefinitionId Threaten { get; } = Id("threaten");
        public static CardDefinitionId FormUp { get; } = Id("form-up");
        public static CardDefinitionId Terrify { get; } = Id("terrify");
        public static CardDefinitionId Testudo { get; } = Id("testudo");
        public static CardDefinitionId HoldGround { get; } = Id("hold-ground");
        public static CardDefinitionId MakeAStand { get; } = Id("make-a-stand");
        public static CardDefinitionId LightPilum { get; } = Id("light-pilum");
        public static CardDefinitionId BattleReady { get; } = Id("battle-ready");
        public static CardDefinitionId HeavyPilum { get; } = Id("heavy-pilum");
        public static CardDefinitionId BattleFury { get; } = Id("battle-fury");
        public static CardDefinitionId AngrySwipe { get; } = Id("angry-swipe");

        public static ActionDefinitionId PrimaryBasicAttack { get; } = new ActionDefinitionId("primary-basic-attack");
        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");
        public static ActionDefinitionId PrimaryBasicSkill { get; } = new ActionDefinitionId("primary-basic-skill");
        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } = new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } = new ActionDefinitionId("secondary-defense");
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
            { AngryStrike, ChillingHowl, Trace, Menace, HoldGround });
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
            { Chase, BluntForce, HuntDown, SendFlying, Threaten, FormUp, Terrify, Testudo,
                MakeAStand, LightPilum, BattleReady, HeavyPilum, BattleFury });
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[] { AngrySwipe });
        private static readonly Lazy<IReadOnlyList<CardDefinition>> All =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(BuildSet().ToArray()));
        private static readonly Lazy<IReadOnlyList<CardDefinition>> Dealt =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(All.Value.Where(card =>
                !ReturnedToBoxAtSetup.Contains(card.Id)).ToArray()));
        public static IReadOnlyList<CardDefinition> AllDefinitions => All.Value;
        public static IReadOnlyList<CardDefinition> DealtDefinitions => Dealt.Value;
        public static CardDefinition Definition(CardDefinitionId id) => AllDefinitions.Single(card => card.Id.Equals(id));
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined) throw new ArgumentException("Garrus's cards require an owner.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(instanceIdPrefix)) throw new ArgumentException("Cards require a per-seat identity prefix.", nameof(instanceIdPrefix));
            return Array.AsReadOnly(DealtDefinitions.Select(card => StartingHand.Contains(card.Id)
                ? CardInstance.CreateInHand(new CardInstanceId(instanceIdPrefix + "-" + card.Id.Value), card.Reference, ownerId)
                : CardInstance.CreateInDeck(new CardInstanceId(instanceIdPrefix + "-" + card.Id.Value), card.Reference, ownerId)).ToArray());
        }
        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => Deck.Contains(card.DefinitionReference.DefinitionId)));
        }
        private static CardDefinitionId Id(string value) => new CardDefinitionId(ContentSetId + "-" + value);
        private static CardProgressionProfile Red(CardTier tier, PrintedValueChannel item) =>
            Progression(CardColor.Red, tier, item, Chase, BluntForce, HuntDown, SendFlying);
        private static CardProgressionProfile Blue(CardTier tier, PrintedValueChannel item) =>
            Progression(CardColor.Blue, tier, item, Threaten, FormUp, Terrify, Testudo);
        private static CardProgressionProfile Green(CardTier tier, PrintedValueChannel item) =>
            Progression(CardColor.Green, tier, item, MakeAStand, LightPilum, BattleReady, HeavyPilum);
        private static CardProgressionProfile Progression(CardColor color, CardTier tier, PrintedValueChannel item,
            CardDefinitionId secondA, CardDefinitionId secondB, CardDefinitionId thirdA, CardDefinitionId thirdB) =>
            tier == CardTier.I ? CardProgressionProfile.NonBasicCard(color, tier, item, secondA, secondB) :
            tier == CardTier.II ? CardProgressionProfile.NonBasicCard(color, tier, item, thirdA, thirdB) :
            CardProgressionProfile.NonBasicCard(color, tier, item);

        private static IEnumerable<CardDefinition> BuildSet()
        {
            yield return Build(AngryStrike, "Angry Strike", 11, 3, 1, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. +1 Attack for every card in your discard.");
            yield return Build(ChillingHowl, "Chilling Howl", 13, 3, 0, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "You may discard one of your resolved cards. This round: Enemy heroes in radius cannot fast travel, or move more than 2 spaces with a movement action.",
                GarrusEffectContent.ChillingHowl(ChillingHowl, ContentRevision));
            yield return Build(Trace, "Trace", 7, 5, 4, 3, PrintedValueChannel.None, 0, Red(CardTier.I, PrintedValueChannel.None),
                PursuitText(1), GarrusEffectContent.PursuitAttack(Trace, ContentRevision, 1), CardTextTiming.BeforeTheAttack, true);
            yield return Build(Chase, "Chase", 8, 5, 4, 4, PrintedValueChannel.None, 0, Red(CardTier.II, PrintedValueChannel.Defense),
                PursuitText(2), GarrusEffectContent.PursuitAttack(Chase, ContentRevision, 2), CardTextTiming.BeforeTheAttack, true);
            yield return Build(BluntForce, "Blunt Force", 8, 7, 4, 7, PrintedValueChannel.None, 0, Red(CardTier.II, PrintedValueChannel.Initiative),
                PushText(1), GarrusEffectContent.MovePushAfterAttack(BluntForce, ContentRevision, 1), CardTextTiming.AfterTheAttack);
            yield return Build(HuntDown, "Hunt Down", 8, 6, 4, 4, PrintedValueChannel.None, 0, Red(CardTier.III, PrintedValueChannel.Range),
                PursuitText(3), GarrusEffectContent.PursuitAttack(HuntDown, ContentRevision, 3), CardTextTiming.BeforeTheAttack, true);
            yield return Build(SendFlying, "Send Flying", 8, 8, 4, 7, PrintedValueChannel.None, 0, Red(CardTier.III, PrintedValueChannel.Radius),
                PushText(2), GarrusEffectContent.MovePushAfterAttack(SendFlying, ContentRevision, 2), CardTextTiming.AfterTheAttack);
            yield return Build(Menace, "Menace", 9, 6, 3, 0, PrintedValueChannel.Range, 2, Blue(CardTier.I, PrintedValueChannel.None),
                UnitMoveText(false, 0), GarrusEffectContent.MoveUnit(Menace, ContentRevision, false, 0));
            yield return Build(Threaten, "Threaten", 10, 6, 3, 0, PrintedValueChannel.Range, 2, Blue(CardTier.II, PrintedValueChannel.Defense),
                UnitMoveText(false, 1), GarrusEffectContent.MoveUnit(Threaten, ContentRevision, false, 1));
            yield return Build(FormUp, "Form Up!", 10, 6, 3, 0, PrintedValueChannel.Range, 3, Blue(CardTier.II, PrintedValueChannel.Attack),
                UnitMoveText(true, 1), GarrusEffectContent.MoveUnit(FormUp, ContentRevision, true, 1));
            yield return Build(Terrify, "Terrify", 10, 7, 3, 0, PrintedValueChannel.Range, 2, Blue(CardTier.III, PrintedValueChannel.Defense),
                UnitMoveText(false, 2), GarrusEffectContent.MoveUnit(Terrify, ContentRevision, false, 2));
            yield return Build(Testudo, "Testudo!", 10, 7, 3, 0, PrintedValueChannel.Range, 3, Blue(CardTier.III, PrintedValueChannel.Attack),
                UnitMoveText(true, 2), GarrusEffectContent.MoveUnit(Testudo, ContentRevision, true, 2));
            yield return Build(HoldGround, "Hold Ground", 5, 3, 2, 0, PrintedValueChannel.Radius, 3, Green(CardTier.I, PrintedValueChannel.None),
                RetrieveText(1), GarrusEffectContent.RetrieveInPresence(HoldGround, ContentRevision, 1));
            yield return Build(MakeAStand, "Make a Stand", 4, 4, 2, 0, PrintedValueChannel.Radius, 4, Green(CardTier.II, PrintedValueChannel.Initiative),
                RetrieveText(1), GarrusEffectContent.RetrieveInPresence(MakeAStand, ContentRevision, 1));
            yield return Build(LightPilum, "Light Pilum", 4, 4, 2, 0, PrintedValueChannel.Range, 2, Green(CardTier.II, PrintedValueChannel.Attack),
                "An enemy hero in range discards a card, if able. You may move 1 space.",
                GarrusEffectContent.Pilum(LightPilum, ContentRevision, false));
            yield return Build(BattleReady, "Battle Ready", 4, 4, 2, 0, PrintedValueChannel.Radius, 4, Green(CardTier.III, PrintedValueChannel.Initiative),
                RetrieveText(2), GarrusEffectContent.RetrieveInPresence(BattleReady, ContentRevision, 2));
            yield return Build(HeavyPilum, "Heavy Pilum", 4, 4, 2, 0, PrintedValueChannel.Range, 2, Green(CardTier.III, PrintedValueChannel.Movement),
                "An enemy hero in range discards a card, or is defeated. You may move up to 2 spaces.",
                GarrusEffectContent.Pilum(HeavyPilum, ContentRevision, true));
            yield return new CardDefinition(BattleFury, ContentRevision, "Battle Fury", default,
                CardContentAuthority.VerifiedOfficial, CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.TriggeredAbility("battle-fury-resolved-discard", "ResolvedDiscardPrimary",
                    GarrusEffectContent.ResolvedDiscardPrimary(BattleFury, ContentRevision).Reference,
                    UltimateTriggerCondition.AfterOwnedResolvedCardDiscarded, new[] { TimingCheckpoint.ResolvedCardDiscarded },
                    Array.Empty<PrintedValueDefinition>()),
                CardSourceContent.Transcribed("Each time after one of your resolved cards is discarded, you may perform its primary action."));
            yield return Build(AngrySwipe, "Angry Swipe", 10, 2, 1, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. +1 Attack for every card in your discard.", boxed: true);
        }
        private static string PursuitText(int distance) => "Choose one - Before the attack: If you have one or more cards in your discard, move up to " +
            distance + (distance == 1 ? " space." : " spaces.") + " Target a hero adjacent to you. Target a unit adjacent to you.";
        private static string PushText(int distance) => "Target a unit adjacent to you. After the attack: You may move up to " + distance +
            (distance == 1 ? " space" : " spaces") + " to a space adjacent to an enemy hero; if you do, push that hero 3 spaces ignoring obstacles.";
        private static string UnitMoveText(bool friendly, int repeats) => "Move " + (friendly ? "a friendly" : "an enemy") +
            " unit in range 1 space to a space " + (friendly ? "closer to you." : "farther away from you.") +
            (repeats == 0 ? "" : repeats == 1 ? " May repeat once." : " May repeat up to two times.");
        private static string RetrieveText(int count) => "If there are at least two enemy heroes in radius, you may retrieve " +
            (count == 1 ? "a discarded card." : "up to two discarded cards.");

        private static CardDefinition Build(CardDefinitionId id, string name, int initiative, int defense, int movement,
            int attack, PrintedValueChannel icon, int reach, CardProgressionProfile progression, string text,
            EffectDefinition effect = null, CardTextTiming timing = CardTextTiming.None, bool modal = false, bool boxed = false)
        {
            var values = new List<PrintedValueDefinition>();
            if (attack > 0) values.Add(new PrintedValueDefinition(PrintedValueChannel.Attack, attack));
            if (icon != PrintedValueChannel.None) values.Add(new PrintedValueDefinition(icon, reach));
            var basic = progression.Classification == CardClassification.Basic;
            var primaryId = attack > 0 ? (basic ? PrimaryBasicAttack : PrimaryAttack) : (basic ? PrimaryBasicSkill : PrimarySkill);
            var target = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged);
            var actions = new List<ActionDefinition>
            {
                new ActionDefinition(primaryId, attack > 0 ? ActionKind.Attack : ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary, values,
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    attack > 0 ? AttackRangeKind.NonRanged : AttackRangeKind.None, default, effect,
                    boxed ? PrintedTextComposition.DeferredUntil("first odd-player-count configuration") : PrintedTextComposition.None,
                    cardTextTiming: timing, attackTargetPolicy: attack > 0 ? (modal ? target.BoundByCardText() : target) : (PrintedAttackTargetPolicy?)null,
                    rosterValueContributions: id.Equals(AngryStrike) ? new[] {
                        new RosterValueContributionDefinition(PrintedValueChannel.Attack, RosterValueQueryKind.OwnedDiscardCount, 1) } : null)
            };
            if (movement > 0) actions.Add(new ActionDefinition(SecondaryMovement, ActionKind.Movement,
                ActionDefinitionOrigin.PrintedSecondary, new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movement) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)));
            if (defense > 0) actions.Add(new ActionDefinition(SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false), AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)));
            return new CardDefinition(id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial, progression, actions, null, CardSourceContent.Transcribed(text));
        }
    }
}
