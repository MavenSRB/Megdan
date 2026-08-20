using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.2a: rulebook p.16's four dashboard item slots as hero content, and the Item Bonus
    /// composed into every value total this build computes.
    ///
    /// Organised by acceptance check, as milestone 7.2's fixture is. Two things it proves are
    /// deliberately negative and easy to lose: an item whose icon no slot accepts is refused rather
    /// than placed, and a card that prints no icon of a channel gains nothing there even when its owner
    /// holds an item for that channel. Both are p.16's own statements rather than defensive coding.
    /// </summary>
    public sealed class DashboardSlotsAndComposedItemTotalsTests
    {
        private static readonly PrintedValueChannel[] PageSixteenIcons =
        {
            PrintedValueChannel.Initiative,
            PrintedValueChannel.Attack,
            PrintedValueChannel.Defense,
            PrintedValueChannel.Movement,
            PrintedValueChannel.Range,
            PrintedValueChannel.Radius
        };

        // ----- Check 2: each hero declares p.16's four slots, and a slot is not an icon -----

        [Test]
        public void EveryHeroDeclaresThePageSixteenSlots_AndTheFourthCarriesThreeIcons()
        {
            var participants = PlaceholderOneVsOneTestConfiguration.Create().Participants;
            Assert.That(participants.Heroes, Is.Not.Empty);

            foreach (var hero in participants.Heroes)
            {
                var dashboard = hero.Dashboard;
                Assert.That(dashboard, Is.EqualTo(HeroDashboard.RulebookLayout));
                Assert.That(dashboard.Slots, Has.Count.EqualTo(HeroDashboard.SlotCount));
                Assert.That(
                    dashboard.Slots.Select(slot => slot.Position.Index),
                    Is.EqualTo(new[] { 1, 2, 3, 4 }));

                // p.16 names the first and the third itself; the user supplied the second and fourth
                // from the component on 2026-08-19.
                Assert.That(
                    dashboard.Slots[0].AcceptedIcons,
                    Is.EqualTo(new[] { PrintedValueChannel.Attack }));
                Assert.That(
                    dashboard.Slots[1].AcceptedIcons,
                    Is.EqualTo(new[] { PrintedValueChannel.Defense }));
                Assert.That(
                    dashboard.Slots[2].AcceptedIcons,
                    Is.EqualTo(new[] { PrintedValueChannel.Initiative }));
                Assert.That(
                    dashboard.Slots[3].AcceptedIcons,
                    Is.EqualTo(new[]
                    {
                        PrintedValueChannel.Range,
                        PrintedValueChannel.Movement,
                        PrintedValueChannel.Radius
                    }));

                // A slot is not an icon: the fourth accepts three, so no icon names a slot on its own.
                foreach (var icon in PageSixteenIcons)
                {
                    Assert.That(dashboard.TryFindSlotAccepting(icon, out var slot), Is.True, icon.ToString());
                    Assert.That(dashboard.GetSlot(slot.Position), Is.SameAs(slot));
                }

                Assert.That(dashboard.Accepts(PrintedValueChannel.None), Is.False);
                Assert.That(dashboard.Slots[3].Accepts(PrintedValueChannel.Attack), Is.False);
                Assert.Throws<ArgumentException>(() => dashboard.GetSlot(default));
            }
        }

        [Test]
        public void ADashboard_RefusesALayoutPageSixteenDoesNotDescribe()
        {
            // Three slots is not four.
            Assert.Throws<ArgumentException>(() =>
                new HeroDashboard(HeroDashboard.RulebookLayout.Slots.Take(3)));

            // The positions are 1..4 in order, so a dashboard cannot skip or repeat one.
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeroDashboardSlotPosition(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HeroDashboardSlotPosition(HeroDashboard.SlotCount + 1));
            Assert.Throws<ArgumentException>(() => new HeroDashboard(new[]
            {
                Slot(1, PrintedValueChannel.Attack),
                Slot(1, PrintedValueChannel.Defense),
                Slot(3, PrintedValueChannel.Initiative),
                Slot(4, PrintedValueChannel.Movement)
            }));

            // Two slots accepting one icon would make p.16's match ambiguous.
            Assert.Throws<ArgumentException>(() => new HeroDashboard(new[]
            {
                Slot(1, PrintedValueChannel.Attack),
                Slot(2, PrintedValueChannel.Attack),
                Slot(3, PrintedValueChannel.Initiative),
                Slot(4, PrintedValueChannel.Movement)
            }));

            // A slot accepts at least one named icon, and names each of them once.
            Assert.Throws<ArgumentException>(() =>
                new HeroDashboardSlot(new HeroDashboardSlotPosition(1)));
            Assert.Throws<ArgumentException>(() =>
                Slot(1, PrintedValueChannel.None));
            Assert.Throws<ArgumentException>(() =>
                Slot(1, PrintedValueChannel.Attack, PrintedValueChannel.Attack));

            // A dashboard need not cover all six icons: a hero with nowhere to put a Radius item is
            // legal content, and refusing that item is this milestone's rule rather than a type error.
            var partial = RestrictedDashboard();
            Assert.That(partial.Accepts(PrintedValueChannel.Movement), Is.False);
            Assert.That(partial.Accepts(PrintedValueChannel.Range), Is.True);
        }

        // ----- Check 3: an item records its slot and its icon, and the bonus reads the icon -----

        [Test]
        public void TwoItemsUnderTheFourthSlot_RaiseTheirOwnIconsAndNotEachOther()
        {
            var participants = PlaceholderOneVsOneTestConfiguration.Create().Participants;
            var owner = participants.Heroes[0].Id;
            var fourth = HeroDashboard.RulebookLayout.Slots[3].Position;
            var items = HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
            {
                new HeroItem(owner, new CardInstanceId("move-item"), fourth, PrintedValueChannel.Movement, 1),
                new HeroItem(owner, new CardInstanceId("range-item"), fourth, PrintedValueChannel.Range, 2)
            }));

            var held = items.GetItems(owner);
            Assert.That(held.Select(item => item.Slot), Is.EqualTo(new[] { fourth, fourth }));
            Assert.That(
                held.Select(item => item.Icon),
                Is.EqualTo(new[] { PrintedValueChannel.Movement, PrintedValueChannel.Range }));

            // This is the case milestone 7.2's slot-as-icon model could not represent: one slot, two
            // items, two different totals raised by one each.
            var movement = ItemBonusQuery.Apply(PrintedIcon(PrintedValueChannel.Movement, 2), items, owner);
            var range = ItemBonusQuery.Apply(PrintedIcon(PrintedValueChannel.Range, 2), items, owner);
            var radius = ItemBonusQuery.Apply(PrintedIcon(PrintedValueChannel.Radius, 2), items, owner);

            Assert.That(movement.Total, Is.EqualTo(3));
            Assert.That(range.Total, Is.EqualTo(3));
            Assert.That(radius.Total, Is.EqualTo(2), "The third icon of that slot has no item.");
            Assert.That(
                movement.Contributions.Single(value => value.Kind == ValueContributionKind.ItemModifier)
                    .SourceCardInstanceId,
                Is.EqualTo(new CardInstanceId("move-item")));
            Assert.That(
                range.Contributions.Single(value => value.Kind == ValueContributionKind.ItemModifier)
                    .SourceCardInstanceId,
                Is.EqualTo(new CardInstanceId("range-item")));
        }

        // ----- Check 1: every total this build computes composes through the one query -----

        [Test]
        public void AnInitiativeItem_RaisesTheValueTheTurnOrderReads()
        {
            var participants = PlaceholderOneVsOneTestConfiguration.Create().Participants;
            var red = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blue = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var redDefinition = InitiativeDefinition("red-card-definition", 5);
            var blueDefinition = InitiativeDefinition("blue-card-definition", 6);
            var redCard = RevealedCard("red-card", redDefinition, red);
            var blueCard = RevealedCard("blue-card", blueDefinition, blue);
            var cards = new[] { redCard, blueCard };
            var definitions = new[] { redDefinition, blueDefinition };

            // Without an item the higher printed Initiative acts first, and it is Blue's.
            var withoutItem = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Red));
            Assert.That(
                withoutItem.TrySelectNextActor(definitions, cards, participants).Accepted, Is.True);
            Assert.That(withoutItem.SelectedActor.ActorId, Is.EqualTo(blue));
            Assert.That(withoutItem.SelectedActor.InitiativeTotal, Is.EqualTo(6));

            // Two Initiative items raise the Red card past it. p.16 raises the printed icon, so the
            // order the turn reads moves with it.
            var withItems = new InitiativeResolutionAuthority(new TieBreakerState(TeamSide.Red));
            Assert.That(
                withItems.TrySelectNextActor(
                    definitions,
                    cards,
                    participants,
                    ItemsFor(participants, red, PrintedValueChannel.Initiative, PrintedValueChannel.Initiative))
                    .Accepted,
                Is.True);
            Assert.That(withItems.SelectedActor.ActorId, Is.EqualTo(red));
            Assert.That(withItems.SelectedActor.InitiativeTotal, Is.EqualTo(7));
        }

        [Test]
        public void AnInitiativeItem_ReachesTheLiveTurnThroughTheRoundsOwnBinding()
        {
            // The composition above is the query's; this is the binding's. A round hands its item state
            // to the turn it starts, so a hero who owns an Initiative item acts in the order the item
            // produces rather than the printed one.
            var fixture = new CompositionFixture();
            var redCard = fixture.HandCard(fixture.RedHeroId, "placeholder-red-bulwark");
            var blueCard = fixture.HandCard(fixture.BlueHeroId, "placeholder-blue-feint");
            Assert.That(
                PrintedInitiativeOf(fixture, redCard), Is.LessThan(PrintedInitiativeOf(fixture, blueCard)),
                "The fixture needs the Red card to lose on printed Initiative alone.");

            fixture.PlaceItems(fixture.RedHeroId, PrintedValueChannel.Initiative, 2);
            Assert.That(fixture.Round.TryCommitCard(fixture.RedHeroId, redCard.Id).Accepted, Is.True);
            Assert.That(fixture.Round.TryCommitCard(fixture.BlueHeroId, blueCard.Id).Accepted, Is.True);

            var selected = fixture.Round.CurrentTurnAuthority.SelectedActor;
            Assert.That(selected.ActorId, Is.EqualTo(fixture.RedHeroId));
            Assert.That(selected.InitiativeTotal, Is.EqualTo(PrintedInitiativeOf(fixture, redCard) + 2));
        }

        [Test]
        public void AnAttackItem_RaisesTheAttackTotalTheOutcomeComparisonReads()
        {
            var withoutItem = new CompositionFixture();
            var offer = withoutItem.AttackOffer(AttackRangeKind.NonRanged);
            var printed = withoutItem.PrintedValueOf(offer, PrintedValueChannel.Attack);
            Assert.That(withoutItem.StartAttack(offer).PendingResolution.AttackTotal, Is.EqualTo(printed));

            var withItem = new CompositionFixture();
            withItem.PlaceItems(withItem.RedHeroId, PrintedValueChannel.Attack, 1);
            var started = withItem.StartAttack(withItem.AttackOffer(AttackRangeKind.NonRanged));

            Assert.That(started.PendingResolution.AttackTotal, Is.EqualTo(printed + 1));
            var contributions = started.PendingResolution.AttackValue.Contributions
                .Where(value => value.Kind == ValueContributionKind.ItemModifier)
                .ToArray();
            Assert.That(contributions, Has.Length.EqualTo(1));
            Assert.That(contributions[0].Amount, Is.EqualTo(ItemBonusQuery.BonusPerItem));
            Assert.That(contributions[0].Channel, Is.EqualTo(PrintedValueChannel.Attack));
        }

        [Test]
        public void ARangeItem_ExtendsARangedAttacksLegalTargetsByExactlyOneSpace()
        {
            var printedRange = new CompositionFixture().PrintedValueOf(
                new CompositionFixture().AttackOffer(AttackRangeKind.Ranged), PrintedValueChannel.Range);
            Assert.That(printedRange, Is.GreaterThan(0));

            // The defender stands one space beyond the printed Range, so only an item can reach them.
            var withoutItem = new CompositionFixture(printedRange + 1);
            Assert.That(
                withoutItem.LegalHeroTargets(withoutItem.AttackOffer(AttackRangeKind.Ranged))
                    .Contains(withoutItem.BlueHeroId),
                Is.False,
                "Without the item the defender is outside the printed Range.");

            var withItem = new CompositionFixture(printedRange + 1);
            withItem.PlaceRangeItem(withItem.RedHeroId);
            Assert.That(
                withItem.LegalHeroTargets(withItem.AttackOffer(AttackRangeKind.Ranged))
                    .Contains(withItem.BlueHeroId),
                Is.True);

            // One space and no further: two spaces beyond the printed Range stays out of reach.
            var stillOutOfReach = new CompositionFixture(printedRange + 2);
            stillOutOfReach.PlaceRangeItem(stillOutOfReach.RedHeroId);
            Assert.That(
                stillOutOfReach.LegalHeroTargets(stillOutOfReach.AttackOffer(AttackRangeKind.Ranged))
                    .Contains(stillOutOfReach.BlueHeroId),
                Is.False);
        }

        /// <summary>
        /// Milestone 7.8a1. The availability of a Ranged Attack follows the same targets a p.16 Range
        /// item extends, because it is the same query: an Attack that reaches nobody is offered
        /// unavailable, and the item that brings the defender into reach makes it available again.
        ///
        /// It lives here rather than beside the other availability cases because the thing it proves is
        /// this file's subject - what an item changes - and because the fixture that can place one is
        /// this file's. The case above it proves the targets move; this proves the offer moved with
        /// them, which is what stops the two from being separate readings of Range.
        /// </summary>
        [Test]
        public void ARangeItem_MakesAnOtherwiseUnavailableRangedAttackAvailable()
        {
            var printedRange = new CompositionFixture().PrintedValueOf(
                new CompositionFixture().AttackOffer(AttackRangeKind.Ranged), PrintedValueChannel.Range);

            var withoutItem = new CompositionFixture(printedRange + 1);
            Assert.That(withoutItem.AttackHasATarget(withoutItem.AttackOffer(AttackRangeKind.Ranged)),
                Is.False,
                "The only enemy stands one space beyond the printed Range, so the Attack has nobody.");

            var withItem = new CompositionFixture(printedRange + 1);
            withItem.PlaceRangeItem(withItem.RedHeroId);
            Assert.That(withItem.AttackHasATarget(withItem.AttackOffer(AttackRangeKind.Ranged)), Is.True,
                "p.16's item brings the defender into reach, so the same offer is available again.");

            var stillOutOfReach = new CompositionFixture(printedRange + 2);
            stillOutOfReach.PlaceRangeItem(stillOutOfReach.RedHeroId);
            Assert.That(
                stillOutOfReach.AttackHasATarget(stillOutOfReach.AttackOffer(AttackRangeKind.Ranged)),
                Is.False,
                "One space and no further, exactly as the target set moves.");

            // A non-Ranged Attack prints no Range icon, so p.16 leaves it alone here too: adjacency is
            // a rule rather than a Range of one, and the item changes neither its targets nor its offer.
            var adjacent = new CompositionFixture();
            adjacent.PlaceRangeItem(adjacent.RedHeroId);
            Assert.That(adjacent.AttackHasATarget(adjacent.AttackOffer(AttackRangeKind.NonRanged)), Is.True);
            Assert.That(
                new CompositionFixture(2).AttackHasATarget(
                    new CompositionFixture(2).AttackOffer(AttackRangeKind.NonRanged)),
                Is.False,
                "and a non-adjacent enemy leaves a non-Ranged Attack with nobody, item or not.");
        }

        [Test]
        public void AMovementItem_RaisesTheAllowanceTheMovementActionStartsWith()
        {
            var withoutItem = new CompositionFixture();
            var offer = withoutItem.MovementOffer();
            var printed = withoutItem.PrintedValueOf(offer, PrintedValueChannel.Movement);
            Assert.That(
                withoutItem.StartMovement(offer).PendingChoice.RemainingAllowance, Is.EqualTo(printed));

            var withItem = new CompositionFixture();
            withItem.PlaceItems(withItem.RedHeroId, PrintedValueChannel.Movement, 1);
            Assert.That(
                withItem.StartMovement(withItem.MovementOffer()).PendingChoice.RemainingAllowance,
                Is.EqualTo(printed + 1));
        }

        [Test]
        public void ADefenseItem_RaisesTheDefenseTotalThroughTheSameQuery()
        {
            var fixture = new CompositionFixture();
            var offer = fixture.DefenseOffer(fixture.BlueHeroId);
            var printed = fixture.PrintedValueOf(offer, PrintedValueChannel.Defense);

            Assert.That(fixture.DefenseTotal(offer, fixture.BlueHeroId), Is.EqualTo(printed));
            fixture.PlaceItems(fixture.BlueHeroId, PrintedValueChannel.Defense, 1);
            Assert.That(fixture.DefenseTotal(offer, fixture.BlueHeroId), Is.EqualTo(printed + 1));
        }

        [Test]
        public void ACardPrintingNoIconOfAChannel_GainsNothingThereFromAnItem()
        {
            // p.16: "if the card has no Range icon, a +1 Range item would have no effect". The
            // non-Ranged Attack prints no Range icon at all, so its reach stays adjacency.
            var beyondReach = new CompositionFixture(2);
            beyondReach.PlaceRangeItem(beyondReach.RedHeroId);
            var melee = beyondReach.AttackOffer(AttackRangeKind.NonRanged);
            Assert.That(
                beyondReach.PrintedValueOf(melee, PrintedValueChannel.Range), Is.Zero,
                "A non-Ranged Attack prints no Range icon.");
            Assert.That(
                beyondReach.LegalHeroTargets(melee).Contains(beyondReach.BlueHeroId), Is.False);

            var adjacent = new CompositionFixture();
            adjacent.PlaceRangeItem(adjacent.RedHeroId);
            Assert.That(
                adjacent.LegalHeroTargets(adjacent.AttackOffer(AttackRangeKind.NonRanged))
                    .Contains(adjacent.BlueHeroId),
                Is.True);
        }

        // ----- Check 4: a card no slot accepts is refused, distinguishably -----

        [Test]
        public void ASetAsideCardNoSlotAccepts_IsRefusedWithoutMovingAnything()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var restricted = ParticipantsWithRestrictedRedDashboard();
            var items = new HeroItemState(restricted);

            var movementCard = SetAsideCard(configuration, PrintedValueChannel.Movement, redHeroId);
            var refused = items.TryPlaceSetAsideCard(movementCard, configuration.CardDefinitions);

            Assert.That(refused.IsPlaced, Is.False);
            Assert.That(
                refused.RejectionReason,
                Is.EqualTo(HeroItemPlacementRejectionReason.NoSlotAcceptsIcon));
            Assert.That(movementCard.State.Location, Is.EqualTo(CardLocation.SetAside),
                "A refusal decides before anything moves.");
            Assert.That(items.GetItems(redHeroId), Is.Empty);

            // Distinguishable from a card that prints no Item icon at all, which is a fact about the
            // card rather than about the dashboard.
            var tierOne = SetAsideTierOneCard(configuration, redHeroId);
            Assert.That(
                items.TryPlaceSetAsideCard(tierOne, configuration.CardDefinitions).RejectionReason,
                Is.EqualTo(HeroItemPlacementRejectionReason.NoItemIcon));

            // The same dashboard accepts an Attack card, so the refusal is about the icon and not the
            // hero.
            var attackCard = SetAsideCard(configuration, PrintedValueChannel.Attack, redHeroId);
            var placed = items.TryPlaceSetAsideCard(attackCard, configuration.CardDefinitions);
            Assert.That(placed.IsPlaced, Is.True);
            Assert.That(placed.Item.Icon, Is.EqualTo(PrintedValueChannel.Attack));
        }

        // ----- Check 5: setup refuses content a hero could be forced to set aside and never place ---

        [Test]
        public void SetupValidation_RefusesAHeroWhoCanReachACardNoSlotOfTheirsAccepts()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();

            // The configured content is exactly what the check is about: every hero's upgrade tree
            // leads to Attack, Initiative, Movement and Defense icons, and p.16's layout accepts all
            // four.
            Assert.DoesNotThrow(() => HeroItemSlotSetupValidator.Validate(
                configuration.Participants, configuration.CardDefinitions, configuration.CardInstances));

            // A Red hero whose fourth slot drops Movement can still be made to set aside the Movement
            // card their own Tier II cards upgrade into, so the content is refused rather than the
            // placement being left to discover it.
            var exception = Assert.Throws<ArgumentException>(() => HeroItemSlotSetupValidator.Validate(
                ParticipantsWithRestrictedRedDashboard(),
                configuration.CardDefinitions,
                configuration.CardInstances));
            Assert.That(exception.Message, Does.Contain("Movement"));

            // Reachability is per hero: a Blue-only card is not something the Red hero can be made to
            // set aside, so a dashboard is judged against the cards its own hero can reach.
            var redOnly = configuration.CardInstances
                .Where(instance => instance.OwnerId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue)
                .ToArray();
            Assert.That(redOnly, Is.Not.Empty);
            Assert.DoesNotThrow(() => HeroItemSlotSetupValidator.Validate(
                ParticipantsWithRestrictedRedDashboard(),
                configuration.CardDefinitions,
                configuration.CardInstances
                    .Where(instance =>
                        instance.OwnerId.Value != PlaceholderOneVsOneParticipants.RedHeroIdValue)
                    .ToArray()));
        }

        // ----- The phase-wide durability rule -----

        [Test]
        public void TheDashboards_SurviveSnapshotRestoreReplayAndRetainedPrefixUndo()
        {
            var session = CreateSession();
            var start = session.CaptureSnapshot();

            var red = session.Projection.Cards.First(card =>
                card.OwnerId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blue = session.Projection.Cards.First(card =>
                card.OwnerId.Value == PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            var afterFirst = session.CaptureSnapshot();
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            var afterBoth = session.CaptureSnapshot();

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterFirst));
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterBoth));
            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(start));

            var restoredSession = CreateSession();
            Assert.That(restoredSession.RestoreSnapshot(afterBoth).Accepted, Is.True);
            Assert.That(restoredSession.CaptureSnapshot(), Is.EqualTo(afterBoth));

            // The slots are read back as content rather than defaulted: the restored participants carry
            // them, and every hero's four are the ones the payload was written with.
            var restored = Deserialize(session, afterBoth);
            foreach (var hero in restored.Participants.Heroes)
            {
                Assert.That(hero.Dashboard, Is.EqualTo(HeroDashboard.RulebookLayout));
            }
        }

        [Test]
        public void ARestore_RefusesAPayloadWhoseDashboardsAreNotThisBuilds()
        {
            var session = CreateSession();
            var snapshot = session.CaptureSnapshot();
            Assert.That(
                RuleEventLogSnapshotEnvelope.TryDeserialize(snapshot, out var envelope, out _), Is.True);

            var payload = Convert.FromBase64String(envelope.AuthoritativePayload);
            var declared = LayoutBytes(HeroDashboard.RulebookLayout);
            var swapped = LayoutBytes(new HeroDashboard(new[]
            {
                Slot(1, PrintedValueChannel.Defense),
                Slot(2, PrintedValueChannel.Attack),
                Slot(3, PrintedValueChannel.Initiative),
                Slot(4, PrintedValueChannel.Range, PrintedValueChannel.Movement, PrintedValueChannel.Radius)
            }));
            var offset = IndexOf(payload, declared);
            Assert.That(offset, Is.GreaterThanOrEqualTo(0), "The payload states a dashboard.");
            Array.Copy(swapped, 0, payload, offset, swapped.Length);

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    Convert.ToBase64String(payload),
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap },
                    session.Projection.Setup.Minions));
            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.HeroDashboardMismatch));
        }

        [Test]
        public void APlacedItem_RoundTripsWithBothItsSlotAndItsIcon()
        {
            var participants = PlaceholderOneVsOneTestConfiguration.Create().Participants;
            var owner = participants.Heroes[0].Id;
            var fourth = HeroDashboard.RulebookLayout.Slots[3].Position;
            var captured = HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
            {
                new HeroItem(owner, new CardInstanceId("a"), fourth, PrintedValueChannel.Range, 1),
                new HeroItem(owner, new CardInstanceId("b"), fourth, PrintedValueChannel.Movement, 2)
            })).CaptureRestoreState();

            var restored = HeroItemState.Restore(participants, captured);
            Assert.That(restored.Items, Is.EqualTo(captured.Items));
            Assert.That(
                restored.GetItems(owner).Select(item => item.Icon),
                Is.EqualTo(new[] { PrintedValueChannel.Range, PrintedValueChannel.Movement }));
            Assert.That(
                restored.GetItems(owner).Select(item => item.Slot), Is.EqualTo(new[] { fourth, fourth }));
        }

        // ----- Helpers -----

        private static HeroDashboardSlot Slot(int position, params PrintedValueChannel[] icons) =>
            new HeroDashboardSlot(new HeroDashboardSlotPosition(position), icons);

        /// <summary>A dashboard whose fourth slot takes Range and Radius but not Movement.</summary>
        private static HeroDashboard RestrictedDashboard() =>
            new HeroDashboard(new[]
            {
                Slot(1, PrintedValueChannel.Attack),
                Slot(2, PrintedValueChannel.Defense),
                Slot(3, PrintedValueChannel.Initiative),
                Slot(4, PrintedValueChannel.Range, PrintedValueChannel.Radius)
            });

        private static MatchParticipants ParticipantsWithRestrictedRedDashboard()
        {
            var participants = PlaceholderOneVsOneTestConfiguration.Create().Participants;
            return new MatchParticipants(
                participants.Teams,
                participants.Players,
                participants.Heroes.Select(hero =>
                    hero.Id.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue
                        ? new Hero(hero.Id, hero.PlayerId, RestrictedDashboard())
                        : hero));
        }

        private static HeroItemState ItemsFor(
            MatchParticipants participants,
            HeroId owner,
            params PrintedValueChannel[] icons) =>
            HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(icons.Select((icon, index) =>
                {
                    Assert.That(
                        HeroDashboard.RulebookLayout.TryFindSlotAccepting(icon, out var slot), Is.True);
                    return new HeroItem(
                        owner, new CardInstanceId($"fixture-item-{index}"), slot.Position, icon, index + 1);
                })));

        private static ValueResult PrintedIcon(PrintedValueChannel channel, int amount) =>
            new ValueResult(channel, new[]
            {
                new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    new CardInstanceId("printed-card"),
                    new ActionDefinitionId("printed-action"),
                    channel,
                    amount)
            });

        private static CardDefinition InitiativeDefinition(string id, int initiative) =>
            new CardDefinition(
                new CardDefinitionId(id),
                "phase-7-2a-fixture-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated);

        private static CardInstance RevealedCard(string cardId, CardDefinition definition, HeroId owner)
        {
            var card = CardInstance.CreateInHand(
                new CardInstanceId(cardId), definition.Reference, owner);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            return card;
        }

        private static int PrintedInitiativeOf(CompositionFixture fixture, CardInstance card) =>
            fixture.Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .Initiative.Value;

        /// <summary>A set-aside instance of a configured definition printing the named Item icon.</summary>
        private static CardInstance SetAsideCard(
            PlaceholderOneVsOneTestConfiguration configuration,
            PrintedValueChannel icon,
            HeroId owner)
        {
            var definition = configuration.CardDefinitions.First(candidate =>
                candidate.Progression.ItemIcon == icon &&
                candidate.Id.Value.Contains("-red-"));
            return SetAsideInstance(definition, owner);
        }

        private static CardInstance SetAsideTierOneCard(
            PlaceholderOneVsOneTestConfiguration configuration,
            HeroId owner) =>
            SetAsideInstance(
                configuration.CardDefinitions.First(definition =>
                    definition.Progression.Tier == CardTier.I),
                owner);

        private static CardInstance SetAsideInstance(CardDefinition definition, HeroId owner) =>
            new CardInstance(
                new CardInstanceId($"{definition.Id.Value}-set-aside"),
                definition.Reference,
                owner,
                new CardStateFacts(
                    CardLocation.SetAside, false, CardResolutionStatus.None, CardFaceOrientation.FaceDown));

        private static byte[] LayoutBytes(HeroDashboard dashboard)
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(dashboard.Slots.Count));
            foreach (var slot in dashboard.Slots)
            {
                bytes.AddRange(BitConverter.GetBytes(slot.Position.Index));
                bytes.AddRange(BitConverter.GetBytes(slot.AcceptedIcons.Count));
                foreach (var icon in slot.AcceptedIcons)
                {
                    bytes.AddRange(BitConverter.GetBytes((int)icon));
                }
            }

            return bytes.ToArray();
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (var index = 0; index <= haystack.Length - needle.Length; index++)
            {
                var found = true;
                for (var offset = 0; offset < needle.Length && found; offset++)
                {
                    found = haystack[index + offset] == needle[offset];
                }

                if (found)
                {
                    return index;
                }
            }

            return -1;
        }

        private static RestoredAuthoritativeMatch Deserialize(MatchSession session, string snapshot)
        {
            Assert.That(
                RuleEventLogSnapshotEnvelope.TryDeserialize(snapshot, out var envelope, out _), Is.True);
            return AuthoritativeSnapshotSerializer.Deserialize(
                envelope.AuthoritativePayload,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// The configured content on a hand-built board, with a round that owns the item state and a
        /// dispatcher bound to it.
        ///
        /// Items are placed through the round's own state rather than injected, because that binding is
        /// half of what these tests are about: every total below reads the items the match records.
        /// </summary>
        private sealed class CompositionFixture
        {
            private readonly PlaceholderOneVsOneTestConfiguration configuration;
            private readonly List<CardInstance> instances;
            private readonly List<CardDefinition> definitions;
            private readonly AdjacentMoveAuthority board;

            public CompositionFixture(int defenderDistance = 1)
            {
                configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
                RedHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
                BlueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
                definitions = configuration.CardDefinitions.ToList();
                definitions.Add(RangeItemDefinition);
                instances = configuration.CardInstances.ToList();
                board = new AdjacentMoveAuthority(
                    Coordinates(6),
                    new[]
                    {
                        new HeroOccupancy(RedHeroId, new HexCoordinate(0, 0)),
                        new HeroOccupancy(BlueHeroId, new HexCoordinate(defenderDistance, 0))
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                CardActions = new CardActionResolutionAuthority(board);
                Round = new RoundResolutionAuthority(
                    definitions,
                    instances,
                    configuration.Participants,
                    configuration.TieBreakerState,
                    1,
                    RoundResolutionComposition.ForCardActionsOnly(CardActions));
            }

            public HeroId RedHeroId { get; }

            public HeroId BlueHeroId { get; }

            public CardActionResolutionAuthority CardActions { get; }

            public RoundResolutionAuthority Round { get; }

            public IReadOnlyList<CardDefinition> Definitions => definitions;

            /// <summary>A Tier III fixture card printing p.16's Range Item icon, which no generated
            /// upgrade target carries - milestone 7.2 gave its four branches the other four icons.</summary>
            private static CardDefinition RangeItemDefinition { get; } = new CardDefinition(
                new CardDefinitionId("fixture-range-item"),
                "phase-7-2a-fixture-rev-1",
                "TEST - Range Item",
                new InitiativeValue(1),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Range));

            public CardInstance HandCard(HeroId owner, string definitionId) =>
                instances.Single(instance =>
                    instance.OwnerId.Equals(owner) &&
                    instance.DefinitionReference.DefinitionId.Value == definitionId &&
                    instance.State.Location == CardLocation.Hand);

            /// <summary>
            /// Places <paramref name="count"/> items supplying <paramref name="icon"/> under the owner's
            /// dashboard, from that hero's own upgrade tree - the cards p.15's upgrade step would have
            /// set aside for them.
            /// </summary>
            public void PlaceItems(HeroId owner, PrintedValueChannel icon, int count)
            {
                var ownTree = owner.Equals(RedHeroId) ? "-red-" : "-blue-";
                var matching = definitions
                    .Where(definition =>
                        definition.Progression.ItemIcon == icon &&
                        definition.Id.Value.Contains(ownTree))
                    .Take(count)
                    .ToArray();
                Assert.That(matching, Has.Length.EqualTo(count), $"content prints {count} {icon} icons");
                foreach (var definition in matching)
                {
                    Place(definition, owner);
                }
            }

            public void PlaceRangeItem(HeroId owner) => Place(RangeItemDefinition, owner);

            public ActionOffer AttackOffer(AttackRangeKind rangeKind) =>
                Offers(RedHeroId).First(offer => offer.AttackRange == rangeKind);

            public ActionOffer MovementOffer() =>
                Offers(RedHeroId).First(offer => KindOf(offer) == ActionKind.Movement);

            public ActionOffer DefenseOffer(HeroId defender) =>
                ActionOfferQuery
                    .GetAvailableOffers(definitions, instances, defender, ActionOfferWindow.Reaction)
                    .First(offer => KindOf(offer) == ActionKind.Defense);

            /// <summary>
            /// What kind of action an offer names, read off the definitions this fixture holds. The
            /// production lookup is internal to the rules assembly, which Unity's split assemblies keep
            /// closed to tests, so the fixture asks the definition itself.
            /// </summary>
            private ActionKind KindOf(ActionOffer offer) =>
                definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind;

            public int PrintedValueOf(ActionOffer offer, PrintedValueChannel channel) =>
                PrintedValueQuery.CalculatePrintedValue(offer, definitions, channel).Total;

            public AttackActionResolutionResult StartAttack(ActionOffer offer)
            {
                var result = CardActions.StartAttackResolution(
                    new ActionResolutionId("composed-attack"),
                    offer,
                    definitions,
                    instances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                Assert.That(result.Accepted, Is.True);
                var opened = CardActions.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        result.PendingTargetChoice.ResolutionId,
                        result.PendingTargetChoice.ChoiceId,
                        BlueHeroId),
                    definitions,
                    instances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                Assert.That(opened.Accepted, Is.True);
                return opened;
            }

            /// <summary>
            /// The enemy heroes this offer may target, through the production start. An Attack with
            /// nothing in reach is refused as <c>TargetUnavailable</c> rather than started with an empty
            /// choice, so that refusal is the "no legal target" answer.
            /// </summary>
            /// <summary>Milestone 7.8a1's judgement, asked of the same composition.</summary>
            public bool AttackHasATarget(ActionOffer offer) =>
                CardActions.CanAttackFindTarget(
                    offer, definitions, configuration.Participants, RuleModificationSet.Empty);

            public IReadOnlyList<HeroId> LegalHeroTargets(ActionOffer offer)
            {
                var result = CardActions.StartAttackResolution(
                    new ActionResolutionId("composed-targets"),
                    offer,
                    definitions,
                    instances,
                    configuration.Participants,
                    RuleModificationSet.Empty);
                if (!result.Accepted)
                {
                    Assert.That(
                        result.RejectionReason,
                        Is.EqualTo(CardActionResolutionRejectionReason.TargetUnavailable));
                    return Array.Empty<HeroId>();
                }

                return Array.AsReadOnly(result.PendingTargetChoice.LegalTargets
                    .Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => target.HeroId)
                    .ToArray());
            }

            public CardActionResolutionResult StartMovement(ActionOffer offer)
            {
                var result = CardActions.StartMovementResolution(
                    new ActionResolutionId("composed-movement"),
                    offer,
                    definitions,
                    instances,
                    RuleModificationSet.Empty);
                Assert.That(result.Accepted, Is.True);
                return result;
            }

            public int DefenseTotal(ActionOffer offer, HeroId defender) =>
                DefenseValueQuery.Calculate(
                    offer,
                    definitions,
                    defender,
                    configuration.Participants,
                    board,
                    RuleModificationSet.Empty,
                    Round.HeroItems).Total;

            private IReadOnlyList<ActionOffer> Offers(HeroId actor) =>
                ActionOfferQuery.GetAvailableOffers(
                    definitions, instances, actor, ActionOfferWindow.NormalPlay);

            private void Place(CardDefinition definition, HeroId owner)
            {
                var card = SetAsideInstance(definition, owner);
                instances.Add(card);
                var placed = Round.HeroItems.TryPlaceSetAsideCard(card, definitions);
                Assert.That(placed.IsPlaced, Is.True, placed.RejectionReason.ToString());
            }

            private static IReadOnlyList<HexCoordinate> Coordinates(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates;
            }
        }
    }
}
