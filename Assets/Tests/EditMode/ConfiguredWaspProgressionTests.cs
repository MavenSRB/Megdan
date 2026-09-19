using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.14: rulebook p.15's Leveling Up and p.16's Gain an Item and Ultimate, run over
    /// Wasp's real eighteen-card deck.
    ///
    /// <para><see cref="HeroLevelUpTests"/> proves the same transition against the mechanics-showcase
    /// content, whose upgrade tree is generated and whose four item icons were chosen to exercise the
    /// machinery. This fixture proves it against the cards milestone 8.1 transcribed: the six upgrade
    /// pairs <c>Docs/AI/Rules/HERO_WASP.md</c> records, the item icon each untaken card prints, and the
    /// Tier IV card p.16 unlocks once the other twelve have been upgraded away. The two are separate
    /// because they answer different questions - one asks whether the rule is implemented, this one
    /// asks whether the printed content it is pointed at agrees with the shard that transcribed
    /// it.</para>
    ///
    /// <para>Coins are awarded through p.13's Defeat, which is the only source this build has and the
    /// same entry point <see cref="HeroLevelUpTests"/> uses; nothing here writes a balance.</para>
    /// </summary>
    public sealed class ConfiguredWaspProgressionTests
    {
        // The six answers below take one card of every verified pair and leave the other to become an
        // item, chosen so that Wasp's six items are her six distinct printed icons - which is the
        // shard's own claim about this hero ("Wasp's twelve item icons cover all six of p.16's icons,
        // so they reach all four dashboard slots") played out through one hero's whole progression.
        private static readonly string[] SixUpgradesToTierThree =
        {
            "red-wasp-stop-projectiles->red-wasp-telekinesis",
            "red-wasp-shock->red-wasp-electrocute",
            "red-wasp-lift-up->red-wasp-control-gravity",
            "red-wasp-telekinesis->red-wasp-mass-telekinesis",
            "red-wasp-electrocute->red-wasp-electroblast",
            "red-wasp-control-gravity->red-wasp-center-of-mass"
        };

        [Test]
        public void TheFirstLevel_OffersBothPrintedCandidatesOfEveryTierOneCardWithTheItemEachSetsAside()
        {
            var fixture = new WaspProgressionFixture(coins: 1);
            fixture.Begin();

            // p.15: "Choose a non-basic Action card in your hand with the lowest card Tier or tied for
            // the lowest", then "Find two Action cards in your deck that have the same color as the
            // chosen card but are one Tier higher". At Level 1 the hand's three Tier I cards are tied,
            // and each names the pair the shard's upgrade table prints for its colour.
            Assert.That(
                fixture.Transitions.PendingStep.Options.Select(option => option.Value),
                Is.EqualTo(new[]
                {
                    "red-wasp-lift-up->red-wasp-control-gravity",
                    "red-wasp-lift-up->red-wasp-kinetic-repulse",
                    "red-wasp-shock->red-wasp-electrocute",
                    "red-wasp-shock->red-wasp-charged-boomerang",
                    "red-wasp-stop-projectiles->red-wasp-deflect-projectiles",
                    "red-wasp-stop-projectiles->red-wasp-telekinesis"
                }),
                "Wasp's Tier I cards are Lift Up (Blue), Shock (Red) and Stop Projectiles (Green), and " +
                "each names two Tier II cards of its own colour.");

            // The card the player does not take is the item they gain, and the prompt says so before
            // the choice is made: p.16's icon and the slot it matches, per option.
            var described = fixture.Describe().Options.ToDictionary(
                option => option.OptionId.Value,
                option => (option.SetAsideCardInstanceId.Value, option.ItemIcon, option.ItemSlot.Index));
            Assert.That(described["red-wasp-lift-up->red-wasp-control-gravity"],
                Is.EqualTo(("red-wasp-kinetic-repulse", PrintedValueChannel.Attack, 1)));
            Assert.That(described["red-wasp-lift-up->red-wasp-kinetic-repulse"],
                Is.EqualTo(("red-wasp-control-gravity", PrintedValueChannel.Defense, 2)));
            Assert.That(described["red-wasp-shock->red-wasp-electrocute"],
                Is.EqualTo(("red-wasp-charged-boomerang", PrintedValueChannel.Defense, 2)));
            Assert.That(described["red-wasp-shock->red-wasp-charged-boomerang"],
                Is.EqualTo(("red-wasp-electrocute", PrintedValueChannel.Initiative, 3)));
            Assert.That(described["red-wasp-stop-projectiles->red-wasp-deflect-projectiles"],
                Is.EqualTo(("red-wasp-telekinesis", PrintedValueChannel.Attack, 1)));
            Assert.That(described["red-wasp-stop-projectiles->red-wasp-telekinesis"],
                Is.EqualTo(("red-wasp-deflect-projectiles", PrintedValueChannel.Initiative, 3)));
        }

        [Test]
        public void SixUpgrades_TakeOneCardOfEachVerifiedPairAndFileTheOtherUnderItsPrintedIcon()
        {
            var fixture = new WaspProgressionFixture(coins: 28);
            fixture.LevelToTheUltimate(SixUpgradesToTierThree);

            // p.15 closes its own step with the count: "You should now have exactly 5 cards, 1 of each
            // color, in your hand." Wasp's five are the Gold and Silver basics she never upgrades and
            // one Tier III card of each colour.
            Assert.That(fixture.HandDefinitionIds(),
                Is.EqualTo(new[]
                {
                    WaspCardContent.MagneticDagger.Value,
                    WaspCardContent.StaticBarrier.Value,
                    WaspCardContent.Electroblast.Value,
                    WaspCardContent.CenterOfMass.Value,
                    WaspCardContent.MassTelekinesis.Value
                }.OrderBy(value => value, StringComparer.Ordinal)));

            var items = fixture.Items.GetItems(fixture.HeroId);
            Assert.That(
                items.Select(item =>
                    (item.SourceCardInstanceId.Value, item.Icon, item.Slot.Index, item.Order)),
                Is.EqualTo(new[]
                {
                    ("red-wasp-charged-boomerang", PrintedValueChannel.Defense, 2, 1),
                    ("red-wasp-deflect-projectiles", PrintedValueChannel.Initiative, 3, 2),
                    ("red-wasp-kinetic-blast", PrintedValueChannel.Range, 4, 3),
                    ("red-wasp-kinetic-repulse", PrintedValueChannel.Attack, 1, 4),
                    ("red-wasp-reflect-projectiles", PrintedValueChannel.Radius, 4, 5),
                    ("red-wasp-thunder-boomerang", PrintedValueChannel.Movement, 4, 6)
                }),
                "Each item is the card of a verified pair the hero did not take, under the icon that " +
                "card prints and the slot p.16's layout matches it to.");

            Assert.That(items.Select(item => item.Icon).Distinct().Count(), Is.EqualTo(6),
                "Wasp's twelve item icons cover all six of p.16's, and one progression can reach them.");
            Assert.That(items.Select(item => item.Slot.Index).Distinct().OrderBy(index => index),
                Is.EqualTo(new[] { 1, 2, 3, 4 }),
                "p.16's fourth slot takes Range, Movement and Radius together, so six items fill four " +
                "slots.");
            foreach (var item in items)
            {
                Assert.That(fixture.LocationOf(item.SourceCardInstanceId),
                    Is.EqualTo(CardLocation.ItemSlot));
                Assert.That(fixture.CardOf(item.SourceCardInstanceId).State.FaceOrientation,
                    Is.EqualTo(CardFaceOrientation.FaceUp),
                    "p.16: \"tuck it Faceup ... Your items must be clearly visible to the opposing team.\"");
            }

            Assert.That(fixture.Items.GetItems(fixture.EnemyHeroId), Is.Empty,
                "The mirrored Wasp collected no coin, so p.15 bought it no level and p.16 gave it no " +
                "item.");
        }

        [Test]
        public void TheSixItems_RaiseTheMatchingPrintedIconsOnWaspsOwnCardsAndNothingElse()
        {
            var fixture = new WaspProgressionFixture(coins: 28);
            fixture.LevelToTheUltimate(SixUpgradesToTierThree);

            // Ledger question 12, answered 2026-08-21 by user decision as an approved interpretation:
            // an item's +1 reaches a matching printed icon on an Ultimate. High Voltage prints Radius 3
            // on the permanent ability itself, and the +1 Radius item Reflect Projectiles became makes
            // the disk its trigger offers a Radius 4 one. This is the same call the printed-icon
            // selection makes when the ability fires, with the same item state.
            Assert.That(fixture.PrintedRadiusOfHighVoltage(withItems: false), Is.EqualTo(3),
                "The Ultimate's own printed Radius icon.");
            Assert.That(fixture.PrintedRadiusOfHighVoltage(withItems: true), Is.EqualTo(4),
                "A +1 Radius item makes Wasp's High Voltage Radius 4.");

            // p.16's three sentences over verified cards: every icon an item matches is raised by one,
            // and a card printing no icon of that channel gains nothing at all.
            Assert.That(fixture.Total(WaspCardContent.Shock, WaspCardContent.PrimaryAttack,
                PrintedValueChannel.Radius), Is.EqualTo(2 + 1));
            Assert.That(fixture.Total(WaspCardContent.MassTelekinesis, WaspCardContent.PrimarySkill,
                PrintedValueChannel.Range), Is.EqualTo(3 + 1));
            Assert.That(fixture.Total(WaspCardContent.MagneticDagger, WaspCardContent.PrimaryAttack,
                PrintedValueChannel.Attack), Is.EqualTo(3 + 1));
            Assert.That(fixture.Total(WaspCardContent.MagneticDagger, WaspCardContent.SecondaryDefense,
                PrintedValueChannel.Defense), Is.EqualTo(2 + 1));
            Assert.That(fixture.Total(WaspCardContent.MagneticDagger, WaspCardContent.SecondaryMovement,
                PrintedValueChannel.Movement), Is.EqualTo(1 + 1));
            Assert.That(fixture.InitiativeTotal(WaspCardContent.MagneticDagger), Is.EqualTo(12 + 1));

            Assert.That(fixture.Total(WaspCardContent.CenterOfMass, WaspCardContent.PrimarySkill,
                PrintedValueChannel.Range), Is.EqualTo(0),
                "p.16: \"If an Action card has no matching icon, the item has no effect on that " +
                "particular Action card\" - Center of Mass prints a Radius icon and no Range one.");
        }

        [Test]
        public void TheSeventhLevel_UnlocksTheUltimateEndsLevelingAndStillCollectsCoins()
        {
            // 1+2+3+4+5+6 = 21 buys the six upgrades and 7 more buys the level p.16 spends on the
            // Ultimate instead. Two spare coins prove the page's closing sentence.
            var fixture = new WaspProgressionFixture(coins: 30);
            fixture.LevelToTheUltimate(SixUpgradesToTierThree);

            Assert.That(fixture.LocationOf(fixture.UltimateCardInstanceId),
                Is.EqualTo(CardLocation.UltimateArea),
                "p.16: \"Place it Faceup to the left side of your hero dashboard.\"");
            Assert.That(fixture.CardOf(fixture.UltimateCardInstanceId).State.FaceOrientation,
                Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(fixture.Levels.GetLevel(fixture.HeroId).Value, Is.EqualTo(HeroLevel.Maximum));
            Assert.That(fixture.Coins, Is.EqualTo(2));
            Assert.That(
                fixture.Outcomes.Count(outcome =>
                    outcome.Kind == HeroLevelUpOutcomeKind.CardUpgraded),
                Is.EqualTo(6),
                "p.16 unlocks the Ultimate \"instead of upgrading a card and gaining an item\".");

            // "You cannot gain any additional levels, but can still gain coins."
            fixture.AwardCoins(20);
            var again = fixture.Begin("round-2");
            Assert.That(again.Accepted, Is.True);
            Assert.That(again.TransitionCompleted, Is.True);
            Assert.That(fixture.Levels.GetLevel(fixture.HeroId).Value, Is.EqualTo(HeroLevel.Maximum));
            Assert.That(fixture.Coins, Is.EqualTo(22));
            Assert.That(fixture.Items.GetItems(fixture.HeroId), Has.Count.EqualTo(6),
                "A leveling that bought nothing sets nothing aside and so gains no seventh item.");
        }

        [Test]
        public void AnOrdinaryMatchWhoseHeroLevelledToTheUltimate_FiresHighVoltageAfterItsBasicSkill()
        {
            // Milestone 8.13 proved the printed ability with the Ultimate staged beside the dashboard,
            // which its own block allowed because the p.15/p.16 path was this milestone's. Nothing is
            // staged here: the card reached the Ultimate area through the seven levels above, and the
            // match is composed from those very card instances. p.16's unlock is the card's own
            // position, so an ordinary match reads the ability straight off it.
            var fixture = new WaspProgressionFixture(coins: 28);
            fixture.LevelToTheUltimate(SixUpgradesToTierThree);

            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorInRadiusOfAMinion(
                fixture.Configuration, out var redHeroId, out var blueHeroId, out var targetId);
            Assert.That(session.Projection.Cards
                .Single(card => card.Id.Equals(fixture.UltimateCardInstanceId)).State.Location,
                Is.EqualTo(CardLocation.UltimateArea));

            Commit(session, redHeroId, WaspCardContent.StaticBarrier);
            Commit(session, blueHeroId, WaspCardContent.StopProjectiles);
            Accept(session, DirectorCommand.StartAction(WaspCardContent.PrimarySkill));

            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "p.16's continuous ability fires after the basic Skill, from a card no test put there.");
            // Milestone 8.46b leaves this exactly as it was: nothing was selected into this question, so
            // it is p.10s own confirm-or-decline being taken on purpose rather than a selection being
            // closed - and the one-press rule touches neither.
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var minion = session.Projection.PendingEffectChoice.SelectInvocations.Single(value =>
                value.Candidate.Value.MinionId.Equals(targetId));
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, minion);
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, 
                session.Projection.PendingEffectChoice.SelectInvocations.Single());
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, 
                session.Projection.PendingEffectChoice.SelectInvocations.First());

            Assert.That(session.Projection.Minions.Any(value => value.MinionId.Equals(targetId)),
                Is.False,
                "The Ultimate the leveling unlocked defeated the enemy minion it offered.");
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
        }

        private static void Commit(MatchSession session, HeroId ownerId, CardDefinitionId definitionId)
        {
            var card = session.Projection.Cards.Single(value => value.OwnerId.Equals(ownerId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Accept(session, DirectorCommand.CommitCard(ownerId, card.Id));
        }

        /// <summary>
        /// The configured Wasp mirror with p.15's leveling composed over it directly, so a whole
        /// progression can be run without the fourteen coin-earning rounds a live match would need to
        /// pay for it.
        ///
        /// <para>The content is the configured one - the same eighteen definitions and the same card
        /// instances the scene deals - so the transition is pointed at Wasp's printed deck rather than
        /// at a fixture's idea of one. The Red seat levels; the Blue seat is a mirror that collects
        /// nothing, which is what makes it the control for p.16's scope.</para>
        /// </summary>
        private sealed class WaspProgressionFixture
        {
            private readonly List<HeroLevelUpOutcome> outcomes = new List<HeroLevelUpOutcome>();
            private int awardCount;

            public WaspProgressionFixture(int coins)
            {
                Configuration = MatchConfiguration
                    .CreateVerifiedMirrorForInternalTest(TeamSide.Red);
                HeroId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                EnemyHeroId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Levels = new HeroProgressionState(Configuration.Participants);
                Items = new HeroItemState(Configuration.Participants);
                CoinLedger = new HeroCoinLedger(Configuration.Participants);
                Rules = new HeroLevelUpStepRules(
                    Configuration.Participants,
                    Configuration.CardDefinitions,
                    Configuration.CardInstances);
                Rules.Bind(Levels, Items, CoinLedger);
                Transitions = new SystemTransitionAuthority(
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(null, Rules));
                if (coins > 0)
                {
                    AwardCoins(coins);
                }
            }

            public MatchConfiguration Configuration { get; }

            public HeroId HeroId { get; }

            public HeroId EnemyHeroId { get; }

            public HeroProgressionState Levels { get; }

            public HeroItemState Items { get; }

            public HeroCoinLedger CoinLedger { get; }

            public HeroLevelUpStepRules Rules { get; }

            public SystemTransitionAuthority Transitions { get; }

            public IReadOnlyList<HeroLevelUpOutcome> Outcomes => outcomes;

            public int Coins => CoinLedger.GetCoins(HeroId);

            public CardInstanceId UltimateCardInstanceId => CardWithDefinition(
                HeroId, WaspCardContent.HighVoltage).Id;

            public void AwardCoins(int amount)
            {
                var enemyTeam = Configuration.Participants.Teams.Single(team => !team.Id.Equals(
                    Configuration.Participants.GetTeamForHero(HeroId).Id));
                var awarded = CoinLedger.Award(new DefeatOutcome(
                    new DefeatOutcomeId($"wasp-progression-award-{++awardCount}"),
                    new DefeatTargetId($"wasp-progression-target-{awardCount}"),
                    enemyTeam.Id,
                    HeroId,
                    amount,
                    0));
                Assert.That(awarded.Accepted, Is.True, awarded.RejectionReason.ToString());
            }

            public SystemTransitionResult Begin(string occasion = "round-1")
            {
                outcomes.Clear();
                var result = Transitions.TryBegin(
                    HeroLevelUpSystemTransition.DefinitionId,
                    new SystemTransitionInstanceId(
                        $"{occasion}-{HeroLevelUpSystemTransition.DefinitionId.Value}"));
                outcomes.AddRange(Rules.DrainOutcomes());
                return result;
            }

            /// <summary>
            /// One end of round in which the hero can afford every level p.15's table has: the six
            /// upgrades the caller names, in order, and then the level p.16 spends on the Ultimate.
            /// </summary>
            public void LevelToTheUltimate(IReadOnlyList<string> upgrades)
            {
                Begin();
                foreach (var upgrade in upgrades)
                {
                    Assert.That(Transitions.PendingStep, Is.Not.Null,
                        "Every ordinary level stops on p.15's Upgrade a Card step.");
                    var invocation = Transitions.PendingStep.Invocations.SingleOrDefault(value =>
                        value.OptionId.Value == upgrade);
                    Assert.That(invocation, Is.Not.Null,
                        $"'{upgrade}' is not among the options this level offers: " +
                        string.Join(", ", Transitions.PendingStep.Options.Select(o => o.Value)));
                    var result = Transitions.TryAnswer(invocation);
                    Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                    outcomes.AddRange(Rules.DrainOutcomes());
                }

                Assert.That(Transitions.IsRunning, Is.False,
                    "The seventh purchase unlocks the Ultimate, which asks nobody anything.");
            }

            public SystemTransitionStepDescription Describe() => Rules.Describe(Transitions.PendingStep);

            public CardInstance CardOf(CardInstanceId instanceId) => Configuration.CardInstances
                .Single(card => card.Id.Equals(instanceId));

            public CardLocation LocationOf(CardInstanceId instanceId) => CardOf(instanceId).State.Location;

            public IEnumerable<string> HandDefinitionIds() => Configuration.CardInstances
                .Where(card => card.OwnerId.Equals(HeroId) && card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal);

            /// <summary>
            /// p.16's Radius on the Ultimate's permanent ability, asked exactly as the printed-icon
            /// selection asks it when the ability fires - which is why the item state is the parameter
            /// rather than the card.
            /// </summary>
            public int PrintedRadiusOfHighVoltage(bool withItems)
            {
                var card = CardOf(UltimateCardInstanceId);
                var definition = DefinitionOf(WaspCardContent.HighVoltage);
                return RadiusValueQuery.Calculate(
                    card.Id,
                    card.DefinitionReference,
                    definition.UltimateRule.EffectSourceId,
                    Configuration.CardDefinitions,
                    HeroId,
                    withItems ? Items : null,
                    RuleModificationSet.Empty).Total;
            }

            public int Total(
                CardDefinitionId definitionId,
                ActionDefinitionId actionDefinitionId,
                PrintedValueChannel channel)
            {
                var card = CardWithDefinition(HeroId, definitionId);
                var printed = PrintedValueQuery.CalculatePrintedValue(
                    card.Id, card.DefinitionReference, actionDefinitionId,
                    Configuration.CardDefinitions, channel);
                return ItemBonusQuery.Apply(printed, Items, HeroId, ItemBonusQuery.BonusPerItem).Total;
            }

            public int InitiativeTotal(CardDefinitionId definitionId)
            {
                var card = CardWithDefinition(HeroId, definitionId);
                var printed = PrintedValueQuery.CalculatePrintedInitiative(
                    card, Configuration.CardDefinitions);
                return ItemBonusQuery.Apply(printed, Items, HeroId, ItemBonusQuery.BonusPerItem).Total;
            }

            private CardDefinition DefinitionOf(CardDefinitionId definitionId) =>
                Configuration.CardDefinitions.Single(definition => definition.Id.Equals(definitionId));

            private CardInstance CardWithDefinition(HeroId ownerId, CardDefinitionId definitionId) =>
                Configuration.CardInstances.Single(card => card.OwnerId.Equals(ownerId) &&
                    card.DefinitionReference.DefinitionId.Equals(definitionId));
        }
    }
}
