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
    /// Milestone 7.2: rulebook p.16's items - the icon a Tier II or Tier III card prints, the state that
    /// records one under a dashboard slot, the Item Bonus it supplies, and the persistence boundary
    /// Phase 7's durability rule requires the milestone that adds the state to close.
    ///
    /// Organised by acceptance check, as milestone 7.1's fixture is, because two of the three checks are
    /// about a relationship - between an item's slot and a card's printed icons, and between a
    /// contribution and the item that produced it - which a test grouped by type would have to split.
    /// </summary>
    public sealed class ItemStateAndBonusTests
    {
        private static readonly CardDefinitionId RedBulwark = new CardDefinitionId("placeholder-red-bulwark");

        // ----- Check 1: ownership, source card, slot and order are authoritative data -----

        [Test]
        public void EveryTierTwoAndThreeDefinition_PrintsAnItemIcon_AndNoOtherDefinitionDoes()
        {
            var configuration = MatchConfiguration.CreateForShowcaseFixture();
            var withIcon = new List<CardDefinitionId>();

            foreach (var definition in configuration.CardDefinitions)
            {
                var progression = definition.Progression;
                var printsIcon = progression.Tier == CardTier.II || progression.Tier == CardTier.III;

                // p.16 states the presence exhaustively - "On the bottom of every Tier II and Tier III
                // Action card, there is an Item icon" - so both directions are asserted.
                Assert.That(progression.HasItemIcon, Is.EqualTo(printsIcon), definition.Id.Value);
                if (printsIcon)
                {
                    Assert.That(progression.ItemIcon, Is.Not.EqualTo(PrintedValueChannel.None));
                    withIcon.Add(definition.Id);
                }
            }

            Assert.That(withIcon, Is.Not.Empty);

            // The four branches of one upgrade tree carry four different icons, which is what makes
            // p.15's choice between them a decision with a consequence - the shape p.16 illustrates with
            // its own example, where one candidate's item gives +1 Attack and the other's +1 Initiative.
            var branchIcons = configuration.CardDefinitions
                .Where(definition => definition.Id.Value.StartsWith(
                    RedBulwark.Value + "-t", StringComparison.Ordinal))
                .Select(definition => definition.Progression.ItemIcon)
                .ToArray();
            Assert.That(branchIcons, Has.Length.EqualTo(4));
            Assert.That(branchIcons.Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void TheProfile_RefusesAMissingItemIconAndASurplusOne()
        {
            // A Tier II or Tier III card without an icon could be set aside and then never placed.
            Assert.Throws<ArgumentException>(() =>
                CardProgressionProfile.NonBasicCard(CardColor.Green, CardTier.II, PrintedValueChannel.None));
            Assert.Throws<ArgumentException>(() =>
                CardProgressionProfile.NonBasicCard(CardColor.Green, CardTier.III, PrintedValueChannel.None));

            // A Tier I card with one claims an icon p.16 does not print on it.
            Assert.Throws<ArgumentException>(() =>
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.Attack));

            // A Basic card and a definition stating nothing carry the absence without being asked.
            Assert.That(CardProgressionProfile.BasicCard(CardColor.Gold).HasItemIcon, Is.False);
            Assert.That(CardProgressionProfile.NotStated.HasItemIcon, Is.False);
        }

        [Test]
        public void PlacingASetAsideCard_RecordsOwnerSourceSlotAndOrder_AndTurnsItFaceup()
        {
            var fixture = new ItemFixture();
            var setAside = fixture.SetAsideCardOf(RedBulwark, 0);
            var icon = fixture.DefinitionOf(setAside).Progression.ItemIcon;

            var result = fixture.Items.TryPlaceSetAsideCard(setAside, fixture.Definitions);

            Assert.That(result.IsPlaced, Is.True);
            Assert.That(result.RejectionReason, Is.EqualTo(HeroItemPlacementRejectionReason.None));
            Assert.That(result.Item.OwnerId, Is.EqualTo(fixture.RedHeroId));
            Assert.That(result.Item.SourceCardInstanceId, Is.EqualTo(setAside.Id));
            Assert.That(result.Item.Icon, Is.EqualTo(icon));
            Assert.That(result.Item.Slot, Is.EqualTo(SlotFor(icon)));
            Assert.That(result.Item.Order, Is.EqualTo(1));

            // p.16: "tuck it Faceup under your hero dashboard".
            Assert.That(setAside.State.Location, Is.EqualTo(CardLocation.ItemSlot));
            Assert.That(setAside.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(setAside.OwnerId, Is.EqualTo(fixture.RedHeroId));

            Assert.That(fixture.Items.GetItems(fixture.RedHeroId), Is.EqualTo(new[] { result.Item }));
            Assert.That(fixture.Items.GetItems(fixture.BlueHeroId), Is.Empty);
        }

        [Test]
        public void MultipleItems_MayOccupyOneSlot_AndKeepTheirOwnOrder()
        {
            var fixture = new ItemFixture();

            // p.16: "Multiple items can be placed under the same item slot." Two cards from different
            // upgrade trees whose branches print the same icon land in the same slot.
            var first = fixture.SetAsideCardOf(RedBulwark, 0);
            var second = fixture.SetAsideCardOfMatchingIcon(
                fixture.DefinitionOf(first).Progression.ItemIcon, first.Id);

            Assert.That(fixture.Items.TryPlaceSetAsideCard(first, fixture.Definitions).IsPlaced, Is.True);
            Assert.That(fixture.Items.TryPlaceSetAsideCard(second, fixture.Definitions).IsPlaced, Is.True);

            var items = fixture.Items.GetItems(fixture.RedHeroId);
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].Slot, Is.EqualTo(items[1].Slot));
            Assert.That(items.Select(item => item.Order), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(items[0].SourceCardInstanceId, Is.EqualTo(first.Id));
            Assert.That(items[1].SourceCardInstanceId, Is.EqualTo(second.Id));
        }

        [Test]
        public void EveryRefusedPlacement_NamesItsReasonAndMovesNothing()
        {
            var fixture = new ItemFixture();

            // A card still in hand is not one "set aside during the Upgrade card step".
            var inHand = fixture.HandCardOf(RedBulwark);
            var refusedLocation = fixture.Items.TryPlaceSetAsideCard(inHand, fixture.Definitions);
            Assert.That(refusedLocation.IsPlaced, Is.False);
            Assert.That(refusedLocation.RejectionReason,
                Is.EqualTo(HeroItemPlacementRejectionReason.NotSetAside));
            Assert.That(inHand.State.Location, Is.EqualTo(CardLocation.Hand));

            // A Tier I card carries no Item icon, so nothing can say which slot it belongs under.
            var tierOneSetAside = fixture.SetAsideTierOneCard();
            var refusedIcon = fixture.Items.TryPlaceSetAsideCard(tierOneSetAside, fixture.Definitions);
            Assert.That(refusedIcon.RejectionReason,
                Is.EqualTo(HeroItemPlacementRejectionReason.NoItemIcon));
            Assert.That(tierOneSetAside.State.Location, Is.EqualTo(CardLocation.SetAside),
                "A refusal decides before anything moves.");

            // A definition this build cannot resolve is refused rather than placed under a guessed slot.
            var unresolved = fixture.SetAsideCardOf(RedBulwark, 1);
            var refusedDefinition = fixture.Items.TryPlaceSetAsideCard(
                unresolved,
                fixture.Definitions.Where(definition =>
                    !definition.Reference.Equals(unresolved.DefinitionReference)));
            Assert.That(refusedDefinition.RejectionReason,
                Is.EqualTo(HeroItemPlacementRejectionReason.UnresolvedDefinition));
            Assert.That(unresolved.State.Location, Is.EqualTo(CardLocation.SetAside));

            // A hero the match does not have owns nothing.
            var stranger = new CardInstance(
                new CardInstanceId("stranger-card"),
                fixture.DefinitionOf(unresolved).Reference,
                new HeroId("nobody"),
                new CardStateFacts(
                    CardLocation.SetAside, false, CardResolutionStatus.None, CardFaceOrientation.FaceDown));
            Assert.That(
                fixture.Items.TryPlaceSetAsideCard(stranger, fixture.Definitions).RejectionReason,
                Is.EqualTo(HeroItemPlacementRejectionReason.NotOwnedByHero));

            // One card becomes at most one item, and the second attempt does not move it again.
            Assert.That(fixture.Items.TryPlaceSetAsideCard(unresolved, fixture.Definitions).IsPlaced, Is.True);
            Assert.That(unresolved.State.Location, Is.EqualTo(CardLocation.ItemSlot));
            Assert.That(
                fixture.Items.TryPlaceSetAsideCard(unresolved, fixture.Definitions).RejectionReason,
                Is.EqualTo(HeroItemPlacementRejectionReason.NotSetAside));
            Assert.That(fixture.Items.GetItems(fixture.RedHeroId), Has.Count.EqualTo(1));

            Assert.Throws<ArgumentException>(() =>
                HeroItemPlacementResult.Rejected(HeroItemPlacementRejectionReason.None));
        }

        // ----- Check 2: +1 only to the matching printed icon -----

        [Test]
        public void AnItem_AddsExactlyOneToItsOwnChannelOnEveryOneOfThePageSixIcons()
        {
            var participants = MatchConfiguration.CreateForShowcaseFixture().Participants;
            var owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var channels = new[]
            {
                PrintedValueChannel.Initiative,
                PrintedValueChannel.Attack,
                PrintedValueChannel.Defense,
                PrintedValueChannel.Movement,
                PrintedValueChannel.Range,
                PrintedValueChannel.Radius
            };

            foreach (var slot in channels)
            {
                var items = ItemsHolding(participants, owner, slot);

                foreach (var channel in channels)
                {
                    var printed = PrintedIcon(channel, 3);
                    var composed = ItemBonusQuery.Apply(printed, items, owner);

                    if (channel == slot)
                    {
                        Assert.That(composed.Total, Is.EqualTo(4), $"{slot} item on a {channel} icon");
                        Assert.That(composed.Contributions, Has.Count.EqualTo(2));
                        Assert.That(composed.Contributions[1].Amount, Is.EqualTo(ItemBonusQuery.BonusPerItem));
                    }
                    else
                    {
                        // "Each item increases the value of the matching icon" - and only that one.
                        Assert.That(composed, Is.SameAs(printed), $"{slot} item on a {channel} icon");
                        Assert.That(composed.Total, Is.EqualTo(3));
                    }
                }
            }

            // Two items in one slot are two contributions of one each, not one of two.
            var doubled = ItemBonusQuery.Apply(
                PrintedIcon(PrintedValueChannel.Attack, 2),
                ItemsHolding(participants, owner, PrintedValueChannel.Attack, PrintedValueChannel.Attack),
                owner);
            Assert.That(doubled.Total, Is.EqualTo(4));
            Assert.That(
                doubled.Contributions.Count(value => value.Kind == ValueContributionKind.ItemModifier),
                Is.EqualTo(2));

            // Another hero's items are not this hero's.
            Assert.That(
                ItemBonusQuery.Contributions(
                    PrintedIcon(PrintedValueChannel.Attack, 2),
                    ItemsHolding(participants, owner, PrintedValueChannel.Attack),
                    new HeroId(StagedBoardParticipants.BlueHeroIdValue)),
                Is.Empty);
        }

        [Test]
        public void AnItem_HasNoEffectOnACardThatPrintsNoMatchingIcon()
        {
            var participants = MatchConfiguration.CreateForShowcaseFixture().Participants;
            var owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var items = ItemsHolding(participants, owner, PrintedValueChannel.Range);

            // p.16 spells this case out: "if the card has no Range icon, a +1 Range item would have no
            // effect. Only Ranged cards with a Range icon would benefit from a +1 Range item." A card
            // printing no Range answers zero, and the item leaves it at zero rather than raising it to
            // one - which is the difference between adding to an icon and inventing one.
            var noRangeIcon = new ValueResult(PrintedValueChannel.Range, Array.Empty<ValueContribution>());
            Assert.That(noRangeIcon.Total, Is.Zero);

            var composed = ItemBonusQuery.Apply(noRangeIcon, items, owner);
            Assert.That(composed.Total, Is.Zero);
            Assert.That(composed.Contributions, Is.Empty);

            // A printed zero is still a printed icon, and the page raises the icon's value rather than
            // its size: a card that prints Range 0 does benefit.
            var printedZero = PrintedIcon(PrintedValueChannel.Range, 0);
            Assert.That(ItemBonusQuery.Apply(printedZero, items, owner).Total, Is.EqualTo(1));
        }

        // ----- Check 3: never card text, and every contribution keeps its provenance -----

        [Test]
        public void ItemContributions_NameTheirItemCard_AndNeverRaiseACardTextNumber()
        {
            var participants = MatchConfiguration.CreateForShowcaseFixture().Participants;
            var owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var itemCardId = new CardInstanceId("item-card-1");
            var items = HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(owner, itemCardId, SlotFor(PrintedValueChannel.Movement), PrintedValueChannel.Movement, 1)
                }));

            var printed = PrintedIcon(PrintedValueChannel.Movement, 2);
            var contributions = ItemBonusQuery.Contributions(printed, items, owner);

            Assert.That(contributions, Has.Count.EqualTo(1));
            Assert.That(contributions[0].Kind, Is.EqualTo(ValueContributionKind.ItemModifier));
            Assert.That(contributions[0].SourceCardInstanceId, Is.EqualTo(itemCardId));
            Assert.That(contributions[0].HasSourceActionDefinitionId, Is.False,
                "An item contribution identifies its item card, not an action definition.");
            Assert.That(contributions[0].Channel, Is.EqualTo(PrintedValueChannel.Movement));

            // p.16: "Items never affect values in the card text, only the value of the Initiative,
            // Attack, Defense, Movement, Range, or Radius icons on a card are affected." A channel whose
            // only entry came from card text is not a printed icon, so it gains nothing - and the
            // card-text contribution itself is left exactly as it was.
            var cardText = new ValueContribution(
                ValueContributionKind.CardTextModifier,
                new CardInstanceId("text-card"),
                new ActionDefinitionId("primary-movement"),
                PrintedValueChannel.Movement,
                2);
            var textOnly = new ValueResult(PrintedValueChannel.Movement, new[] { cardText });
            var composed = ItemBonusQuery.Apply(textOnly, items, owner);
            Assert.That(composed.Contributions, Is.EqualTo(new[] { cardText }));
            Assert.That(composed.Total, Is.EqualTo(2));
        }

        // ----- The production consumer: p.16's bonus inside a real Defense total -----

        [Test]
        public void ADefenseItem_ReachesTheDefenseTotalThroughTheDefenseValueQuery()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var defenderId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var board = new AdjacentMoveAuthority(
                RadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(1, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
            var cardActions = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board);

            // The Blue hero holds one Defense-slot item. Its set-aside card is a Tier III branch of a
            // Blue upgrade tree, which is where the Defense icon is printed; nothing in this build deals
            // it, so the fixture places it exactly as milestone 7.5's purchase will.
            var itemCard = DefenseItemCardFor(configuration, defenderId);
            var instances = configuration.CardInstances.Concat(new[] { itemCard }).ToArray();
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                instances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForCardActionsOnly(cardActions));
            Assert.That(
                round.HeroItems.TryPlaceSetAsideCard(itemCard, configuration.CardDefinitions).IsPlaced,
                Is.True);

            var attackOffer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    instances,
                    attackerId,
                    ActionOfferWindow.NormalPlay)
                .First(offer => offer.AttackRange != AttackRangeKind.None);
            var started = cardActions.StartAttackResolution(
                new ActionResolutionId("item-defense"),
                attackOffer,
                configuration.CardDefinitions,
                instances,
                configuration.Participants,
                RuleModificationSet.Empty);
            var opened = cardActions.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    started.PendingTargetChoice.ResolutionId,
                    started.PendingTargetChoice.ChoiceId,
                    defenderId),
                configuration.CardDefinitions,
                instances,
                configuration.Participants,
                RuleModificationSet.Empty);

            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers[0];
            var printed = PrintedValueQuery.CalculatePrintedValue(
                defenseOffer, configuration.CardDefinitions, PrintedValueChannel.Defense);
            var answered = cardActions.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                configuration.CardDefinitions,
                instances,
                configuration.Participants,
                RuleModificationSet.Empty);

            Assert.That(answered.Accepted, Is.True);
            Assert.That(answered.PendingResolution.DefenseTotal, Is.EqualTo(printed.Total + 1));

            var itemContributions = answered.PendingResolution.DefenseValue.Contributions
                .Where(value => value.Kind == ValueContributionKind.ItemModifier)
                .ToArray();
            Assert.That(itemContributions, Has.Length.EqualTo(1));
            Assert.That(itemContributions[0].SourceCardInstanceId, Is.EqualTo(itemCard.Id));
            Assert.That(itemContributions[0].Amount, Is.EqualTo(1));
        }

        // ----- The phase-wide durability rule -----

        [Test]
        public void TheItemState_RefusesAnIncoherentRestore()
        {
            var participants = MatchConfiguration.CreateForShowcaseFixture().Participants;
            var heroes = participants.Heroes.Select(hero => hero.Id).ToArray();

            // The ordinary case is a hero with no items, and it is not an error.
            var empty = HeroItemState.Restore(
                participants, new HeroItemRestoreState(Array.Empty<HeroItem>()));
            Assert.That(empty.Items, Is.Empty);
            Assert.That(empty.GetItems(heroes[0]), Is.Empty);
            Assert.Throws<KeyNotFoundException>(() => empty.GetItems(new HeroId("nobody")));

            // An item belongs to a participant.
            Assert.Throws<ArgumentException>(() => HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(new HeroId("nobody"), new CardInstanceId("c1"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 1)
                })));

            // One card becomes at most one item.
            Assert.Throws<ArgumentException>(() => HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(heroes[0], new CardInstanceId("c1"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 1),
                    new HeroItem(heroes[0], new CardInstanceId("c1"), SlotFor(PrintedValueChannel.Defense), PrintedValueChannel.Defense, 2)
                })));

            // A hero's orders are exactly 1..n, so an order the payload never stated cannot be restored.
            Assert.Throws<ArgumentException>(() => HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(heroes[0], new CardInstanceId("c1"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 1),
                    new HeroItem(heroes[0], new CardInstanceId("c2"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 3)
                })));

            // Milestone 7.2a. An item's slot and its icon agree with the owner's dashboard: a Defense
            // icon under the Attack slot is a pair no dashboard in this build has, and it is refused
            // rather than restored as an item that would raise a channel its slot does not carry.
            Assert.Throws<ArgumentException>(() => HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(
                        heroes[0],
                        new CardInstanceId("c1"),
                        SlotFor(PrintedValueChannel.Attack),
                        PrintedValueChannel.Defense,
                        1)
                })));

            // An item names a slot, an icon and a positive order at construction.
            Assert.Throws<ArgumentException>(() =>
                new HeroItem(
                    heroes[0],
                    new CardInstanceId("c1"),
                    SlotFor(PrintedValueChannel.Attack),
                    PrintedValueChannel.None,
                    1));
            Assert.Throws<ArgumentException>(() =>
                new HeroItem(
                    heroes[0], new CardInstanceId("c1"), default, PrintedValueChannel.Attack, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HeroItem(heroes[0], new CardInstanceId("c1"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 0));
            Assert.Throws<ArgumentException>(() =>
                new HeroItem(default, new CardInstanceId("c1"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 1));

            // Two heroes each number their own items from one.
            var shared = HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(heroes[1], new CardInstanceId("c2"), SlotFor(PrintedValueChannel.Defense), PrintedValueChannel.Defense, 1),
                    new HeroItem(heroes[0], new CardInstanceId("c1"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 1)
                }));
            Assert.That(shared.GetItems(heroes[0]), Has.Count.EqualTo(1));
            Assert.That(shared.GetItems(heroes[1]), Has.Count.EqualTo(1));
            Assert.That(
                shared.Items.Select(item => item.OwnerId.Value),
                Is.EqualTo(heroes.Select(hero => hero.Value).OrderBy(value => value, StringComparer.Ordinal)));
        }

        [Test]
        public void HeroItems_SurviveSnapshotRestoreReplayUndoAndARefusedCommand()
        {
            var session = CreateSession();
            var beforeAnyCommand = session.CaptureSnapshot();

            // A refused command changes nothing, the new section included: the whole snapshot is
            // byte-identical either side of it, which is this milestone's rollback evidence.
            Assert.That(session.Execute(DirectorCommand.StopMovement()).Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeAnyCommand));

            var red = session.Projection.Cards.First(card =>
                card.OwnerId.Value == StagedBoardParticipants.RedHeroIdValue);
            var blue = session.Projection.Cards.First(card =>
                card.OwnerId.Value == StagedBoardParticipants.BlueHeroIdValue);
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            var afterFirstCommit = session.CaptureSnapshot();
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            var afterBothCommits = session.CaptureSnapshot();

            // Retained-prefix Undo truncates back to the earlier state, and replaying the same command
            // reconstructs the later one rather than carrying anything across it.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterFirstCommit));
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterBothCommits));

            // Restart replays from the start, so the empty item state is reconstructed rather than kept.
            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeAnyCommand));

            // Restore-and-continue through the same public command path reproduces the same payload.
            var restoredSession = CreateSession();
            Assert.That(restoredSession.RestoreSnapshot(afterFirstCommit).Accepted, Is.True);
            Assert.That(restoredSession.CaptureSnapshot(), Is.EqualTo(afterFirstCommit));
            Assert.That(
                restoredSession.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted,
                Is.True);
            Assert.That(restoredSession.CaptureSnapshot(), Is.EqualTo(afterBothCommits));

            // The section is on the wire and reads back as the state itself, not as a default: the
            // restored round carries an item register covering the participating heroes.
            Assert.That(
                RuleEventLogSnapshotEnvelope.TryDeserialize(afterBothCommits, out var envelope, out _),
                Is.True);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                envelope.AuthoritativePayload,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
            Assert.That(restored.Round.HeroItems, Is.Not.Null);
            Assert.That(restored.Round.HeroItems.Items, Is.Empty);
            foreach (var hero in session.Projection.Participants.Heroes)
            {
                Assert.That(restored.Round.HeroItems.GetItems(hero.Id), Is.Empty);
            }
        }

        [Test]
        public void APlacedItem_SurvivesCaptureAndRestoreWithItsSlotAndOrder()
        {
            // Nothing in this build gains an item in a live match - milestone 7.5's purchase is what
            // will - so the round trip is proved through the round's own restore contract, exactly as
            // milestone 7.1 proved a non-default hero Level.
            var configuration = MatchConfiguration.CreateForShowcaseFixture();
            var heroes = configuration.Participants.Heroes.Select(hero => hero.Id).ToArray();
            var captured = HeroItemState.Restore(
                configuration.Participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(heroes[0], new CardInstanceId("item-a"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 1),
                    new HeroItem(heroes[0], new CardInstanceId("item-b"), SlotFor(PrintedValueChannel.Attack), PrintedValueChannel.Attack, 2),
                    new HeroItem(heroes[1], new CardInstanceId("item-c"), SlotFor(PrintedValueChannel.Radius), PrintedValueChannel.Radius, 1)
                })).CaptureRestoreState();

            var round = HeroItemState.Restore(configuration.Participants, captured);
            Assert.That(round.Items, Is.EqualTo(captured.Items));
            Assert.That(
                round.GetItems(heroes[0]).Select(item => item.SourceCardInstanceId.Value),
                Is.EqualTo(new[] { "item-a", "item-b" }));
            Assert.That(round.GetItems(heroes[1])[0].Icon, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(
                round.GetItems(heroes[1])[0].Slot, Is.EqualTo(SlotFor(PrintedValueChannel.Radius)));
            Assert.That(round.CaptureRestoreState().Items, Is.EqualTo(captured.Items));
        }

        // ----- Helpers -----

        private static ValueResult PrintedIcon(PrintedValueChannel channel, int amount)
        {
            var cardId = new CardInstanceId("printed-card");
            var contribution = channel == PrintedValueChannel.Initiative
                ? new ValueContribution(ValueContributionKind.PrintedIcon, cardId, channel, amount)
                : new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    cardId,
                    new ActionDefinitionId("printed-action"),
                    channel,
                    amount);
            return new ValueResult(channel, new[] { contribution });
        }

        private static HeroItemState ItemsHolding(
            MatchParticipants participants,
            HeroId owner,
            params PrintedValueChannel[] icons) =>
            HeroItemState.Restore(
                participants,
                new HeroItemRestoreState(icons.Select((icon, index) =>
                    new HeroItem(
                        owner, new CardInstanceId($"item-{index}"), SlotFor(icon), icon, index + 1))));

        /// <summary>
        /// The slot rulebook p.16's layout accepts that icon under. Every hero in this build declares
        /// that layout, so a fixture that wants "an item supplying this icon" asks the dashboard where
        /// it sits rather than assuming the slot and the icon are the same thing.
        /// </summary>
        private static HeroDashboardSlotPosition SlotFor(PrintedValueChannel icon)
        {
            Assert.That(
                HeroDashboard.RulebookLayout.TryFindSlotAccepting(icon, out var slot),
                Is.True,
                $"p.16's layout accepts {icon}");
            return slot.Position;
        }

        private static CardInstance DefenseItemCardFor(
            MatchConfiguration configuration,
            HeroId owner)
        {
            var definition = configuration.CardDefinitions.First(candidate =>
                candidate.Progression.ItemIcon == PrintedValueChannel.Defense &&
                candidate.Id.Value.Contains("-blue-"));
            return new CardInstance(
                new CardInstanceId($"{definition.Id.Value}-item"),
                definition.Reference,
                owner,
                new CardStateFacts(
                    CardLocation.SetAside, false, CardResolutionStatus.None, CardFaceOrientation.FaceDown));
        }

        private static IReadOnlyList<HexCoordinate> RadiusThreeCoordinates()
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -3; q <= 3; q++)
            {
                for (var r = -3; r <= 3; r++)
                {
                    if (Math.Abs(q + r) <= 3)
                    {
                        coordinates.Add(new HexCoordinate(q, r));
                    }
                }
            }

            return coordinates;
        }

        private static MatchSession CreateSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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
        /// The configured content plus set-aside instances of the upgrade-target definitions.
        ///
        /// They live here rather than in the configured scene for the reason milestone 7.1's deck
        /// instances do: nothing in this build moves a card into a set-aside pile, and dealing them into
        /// the configured hands would change what every pinned digest and card-matrix fixture measures.
        /// </summary>
        private sealed class ItemFixture
        {
            private readonly MatchConfiguration configuration;
            private readonly List<CardInstance> instances = new List<CardInstance>();

            public ItemFixture()
            {
                configuration = MatchConfiguration.CreateForShowcaseFixture();
                RedHeroId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                BlueHeroId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Items = new HeroItemState(configuration.Participants);
                instances.AddRange(configuration.CardInstances);

                foreach (var definition in configuration.CardDefinitions.Where(value =>
                    value.Progression.HasItemIcon))
                {
                    instances.Add(SetAside(definition, definition.Id.Value.Contains("-red-")
                        ? RedHeroId
                        : BlueHeroId));
                }
            }

            public HeroId RedHeroId { get; }
            public HeroId BlueHeroId { get; }
            public HeroItemState Items { get; }
            public IReadOnlyList<CardDefinition> Definitions => configuration.CardDefinitions;

            public CardDefinition DefinitionOf(CardInstance instance) =>
                Definitions.Single(definition =>
                    definition.Reference.Equals(instance.DefinitionReference));

            public CardInstance HandCardOf(CardDefinitionId definitionId) =>
                instances.Single(instance =>
                    instance.DefinitionReference.DefinitionId.Equals(definitionId) &&
                    instance.State.Location == CardLocation.Hand);

            /// <summary>One set-aside upgrade target of the named Tier I card, by branch index.</summary>
            public CardInstance SetAsideCardOf(CardDefinitionId tierOneId, int branchIndex) =>
                instances
                    .Where(instance =>
                        instance.State.Location == CardLocation.SetAside &&
                        instance.DefinitionReference.DefinitionId.Value.StartsWith(
                            tierOneId.Value + "-t", StringComparison.Ordinal))
                    .OrderBy(instance => instance.Id.Value, StringComparer.Ordinal)
                    .ElementAt(branchIndex);

            /// <summary>A second Red-owned set-aside card printing the same icon as an existing one.</summary>
            public CardInstance SetAsideCardOfMatchingIcon(PrintedValueChannel icon, CardInstanceId exclude) =>
                instances.First(instance =>
                    instance.State.Location == CardLocation.SetAside &&
                    !instance.Id.Equals(exclude) &&
                    instance.OwnerId.Equals(RedHeroId) &&
                    DefinitionOf(instance).Progression.ItemIcon == icon);

            /// <summary>A Tier I card set aside, which p.16 prints no Item icon on.</summary>
            public CardInstance SetAsideTierOneCard()
            {
                var definition = Definitions.First(value => value.Progression.Tier == CardTier.I);
                var card = SetAside(definition, RedHeroId);
                instances.Add(card);
                return card;
            }

            private static CardInstance SetAside(CardDefinition definition, HeroId owner) =>
                new CardInstance(
                    new CardInstanceId($"{definition.Id.Value}-set-aside"),
                    definition.Reference,
                    owner,
                    new CardStateFacts(
                        CardLocation.SetAside,
                        false,
                        CardResolutionStatus.None,
                        CardFaceOrientation.FaceDown));
        }
    }
}
