using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriBlueItemSwapTests
    {
        private static readonly HeroId Owner =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);

        [TestCase(CardLocation.Hand)]
        [TestCase(CardLocation.Deck)]
        [TestCase(CardLocation.DiscardPile)]
        [TestCase(CardLocation.TurnArea)]
        public void SameTierAndColorCardCanReplaceItemFromAnyRestingZone(
            CardLocation location)
        {
            var definitions = SnorriBlueMovementFaces.Definitions
                .Concat(SnorriBlueSupportFaces.Definitions).ToArray();
            var items = new HeroItemState(StagedBoardParticipants.Create());
            var source = CardInstance.CreateInDeck(
                new CardInstanceId("boon-item"),
                SnorriBlueSupportFaces.AncestralBoon.Reference, Owner);
            Assert.That(CardLifecycleAuthority.TrySetAsideFromDeck(source), Is.True);
            Assert.That(items.TryPlaceSetAsideCard(source, definitions).IsPlaced, Is.True);

            var replacement = new CardInstance(
                new CardInstanceId("hidden-replacement"),
                SnorriBlueMovementFaces.Definitions[1].Reference, Owner,
                location == CardLocation.TurnArea
                    ? new CardStateFacts(
                        CardLocation.TurnArea, true, CardResolutionStatus.Resolved,
                        CardFaceOrientation.FaceUp)
                    : new CardStateFacts(
                        location, false, CardResolutionStatus.None,
                        CardFaceOrientation.FaceUp));
            var prior = replacement.State;

            Assert.That(items.CanSwapItemWithCard(source, replacement, definitions), Is.True);
            Assert.That(items.TrySwapItemWithCard(source, replacement, definitions), Is.True);
            Assert.That(source.State, Is.EqualTo(prior));
            Assert.That(replacement.State.Location, Is.EqualTo(CardLocation.ItemSlot));
            Assert.That(items.GetItems(Owner).Single().SourceCardInstanceId,
                Is.EqualTo(replacement.Id));
            Assert.That(items.GetItems(Owner).Single().Icon,
                Is.EqualTo(PrintedValueChannel.Attack));
        }

        [Test]
        public void DifferentTierOrUnresolvedCardCannotReplaceItem()
        {
            var definitions = SnorriBlueMovementFaces.Definitions
                .Concat(SnorriBlueSupportFaces.Definitions).ToArray();
            var items = new HeroItemState(StagedBoardParticipants.Create());
            var source = CardInstance.CreateInDeck(
                new CardInstanceId("boon-item"),
                SnorriBlueSupportFaces.AncestralBoon.Reference, Owner);
            Assert.That(CardLifecycleAuthority.TrySetAsideFromDeck(source), Is.True);
            Assert.That(items.TryPlaceSetAsideCard(source, definitions).IsPlaced, Is.True);
            var wrongTier = CardInstance.CreateInHand(
                new CardInstanceId("deep-wrong-tier"),
                SnorriBlueMovementFaces.Definitions[2].Reference, Owner);
            var unresolved = new CardInstance(
                new CardInstanceId("hidden-unresolved"),
                SnorriBlueMovementFaces.Definitions[1].Reference, Owner,
                new CardStateFacts(
                    CardLocation.TurnArea, true, CardResolutionStatus.Unresolved,
                    CardFaceOrientation.FaceUp));

            Assert.That(items.CanSwapItemWithCard(source, wrongTier, definitions), Is.False);
            Assert.That(items.CanSwapItemWithCard(source, unresolved, definitions), Is.False);
            Assert.That(items.GetItems(Owner).Single().SourceCardInstanceId,
                Is.EqualTo(source.Id));
        }

        [Test]
        public void FriendOwnedItemChoicesOfferEveryRestingZoneAndRejectAStaleAnswer()
        {
            var participants = StagedBoardParticipants.Create();
            var definitions = SnorriBlueMovementFaces.Definitions
                .Concat(SnorriBlueSupportFaces.Definitions).ToArray();
            var items = new HeroItemState(participants);
            var source = CardInstance.CreateInDeck(
                new CardInstanceId("boon-item"),
                SnorriBlueSupportFaces.AncestralBoon.Reference, Owner);
            Assert.That(CardLifecycleAuthority.TrySetAsideFromDeck(source), Is.True);
            Assert.That(items.TryPlaceSetAsideCard(source, definitions).IsPlaced, Is.True);
            CardInstance Candidate(string id, CardLocation location) =>
                new CardInstance(
                    new CardInstanceId(id),
                    SnorriBlueMovementFaces.Definitions[1].Reference, Owner,
                    location == CardLocation.TurnArea
                        ? new CardStateFacts(
                            location, true, CardResolutionStatus.Resolved,
                            CardFaceOrientation.FaceUp)
                        : new CardStateFacts(
                            location, false, CardResolutionStatus.None,
                            CardFaceOrientation.FaceUp));
            var hand = Candidate("replacement-hand", CardLocation.Hand);
            var deck = Candidate("replacement-deck", CardLocation.Deck);
            var discard = Candidate("replacement-discard", CardLocation.DiscardPile);
            var resolved = Candidate("replacement-resolved", CardLocation.TurnArea);
            var instances = new[] { source, hand, deck, discard, resolved };
            var port = new ItemPort(items, source, instances, definitions);
            var roster = EffectRosterState.Of(
                Array.Empty<RoundTurnSlot>(), new RoundHeroDefeatHistory(participants),
                new HeroCoinLedger(participants), itemCardSwap: port);
            EffectCapabilityContext Context(CardInstanceId item = default) =>
                new EffectCapabilityContext(
                    new EffectNodeId("item-exchange"), EffectFailurePolicy.Mandatory,
                    new HeroId(StagedBoardParticipants.BlueHeroIdValue),
                    new HeroId(StagedBoardParticipants.BlueHeroIdValue),
                    new CardInstanceId("skill-source"),
                    SnorriBlueSupportFaces.AncestralGrace.Reference,
                    SnorriBlueSupportFaces.PrimarySkill,
                    item.IsDefined
                        ? new System.Collections.Generic.Dictionary<string, EffectValue>
                        {
                            ["owner"] = EffectValue.Hero(Owner),
                            ["item"] = EffectValue.Card(item)
                        }
                        : new System.Collections.Generic.Dictionary<string, EffectValue>
                        {
                            ["owner"] = EffectValue.Hero(Owner)
                        },
                    1, 1, participants, null, RuleModificationSet.Empty,
                    EffectCardState.Of(definitions, instances), ActionKind.Skill,
                    heroItems: items, rosterState: roster);
            var choose = SnorriBlueSupportCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(SnorriBlueSupportCapabilities.ChooseOwnedItemV1));
            Assert.That(choose.Execute(Context()).Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Card(source.Id) }));
            var swap = SnorriBlueSupportCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(SnorriBlueSupportCapabilities.SwapItemWithCardV1));
            var offered = swap.Execute(Context(source.Id));
            Assert.That(offered.Choice.ChooserHeroId, Is.EqualTo(Owner));
            Assert.That(offered.Choice.Candidates.Select(value => value.CardInstanceId),
                Is.EquivalentTo(new[] { hand.Id, deck.Id, discard.Id, resolved.Id }));
            Assert.That(CardLifecycleAuthority.TrySetAsideFromDeck(deck), Is.True);
            Assert.That(swap.Resume(Context(source.Id),
                new[] { EffectValue.Card(deck.Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(port.Calls, Is.Zero);
            Assert.That(swap.Resume(Context(source.Id),
                new[] { EffectValue.Card(hand.Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(port.Calls, Is.EqualTo(1));
            Assert.That(source.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(hand.State.Location, Is.EqualTo(CardLocation.ItemSlot));
        }

        private sealed class ItemPort : IItemCardSwap
        {
            private readonly HeroItemState items;
            private readonly CardInstance source;
            private readonly CardInstance[] instances;
            private readonly CardDefinition[] definitions;

            public ItemPort(
                HeroItemState items, CardInstance source,
                CardInstance[] instances, CardDefinition[] definitions)
            {
                this.items = items;
                this.source = source;
                this.instances = instances;
                this.definitions = definitions;
            }

            public int Calls { get; private set; }

            public bool TrySwapItemWithCard(
                HeroId owner, CardInstanceId itemCard, CardInstanceId replacementCard)
            {
                if (!owner.Equals(Owner) || !itemCard.Equals(source.Id))
                    return false;
                Calls++;
                return items.TrySwapItemWithCard(
                    source, instances.Single(card => card.Id.Equals(replacementCard)),
                    definitions);
            }
        }
    }
}
