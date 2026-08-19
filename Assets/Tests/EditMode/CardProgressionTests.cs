using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.1: card colours, Tiers, upgrade relationships, the queries that read them, and the
    /// hero Level state they exist to serve.
    ///
    /// Organised by acceptance check rather than by type, because several checks are about how two
    /// things relate - a colour that is not a team, a query that composes with a mutation - and a test
    /// grouped by type would have to assert half of each.
    /// </summary>
    public sealed class CardProgressionTests
    {
        private static readonly CardDefinitionId RedBulwark = new CardDefinitionId("placeholder-red-bulwark");
        private static readonly CardDefinitionId RedLancer = new CardDefinitionId("placeholder-red-lancer");
        private static readonly CardDefinitionId RedNeedle = new CardDefinitionId("placeholder-red-needle");
        private static readonly CardDefinitionId BlueSentinel = new CardDefinitionId("placeholder-blue-sentinel");

        // ----- Acceptance check 2: every definition states a colour or a verified absence -----

        [Test]
        public void EveryConfiguredDefinition_StatesProgressionOrItsExplicitAbsence()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();

            foreach (var definition in configuration.CardDefinitions)
            {
                Assert.That(definition.Progression, Is.Not.Null, definition.Id.Value);
                Assert.That(definition.Progression.StatesProgression, Is.True, definition.Id.Value);
                Assert.That(definition.Progression.Color, Is.Not.EqualTo(CardColor.None), definition.Id.Value);
            }

            // The absence is reachable and is a statement, not a default: the constructor refuses a
            // definition with no profile at all, so a definition that carries NotStated said so.
            Assert.Throws<ArgumentNullException>(() => new CardDefinition(
                new CardDefinitionId("no-profile"),
                "rev-1",
                "No Profile",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                null));

            var unstated = CardProgressionProfile.NotStated;
            Assert.That(unstated.StatesProgression, Is.False);
            Assert.That(unstated.Color, Is.EqualTo(CardColor.None));
            Assert.That(unstated.Tier, Is.EqualTo(CardTier.None));
            Assert.That(unstated.NextTierCandidateIds, Is.Empty);
        }

        [Test]
        public void ConfiguredHands_HoldFiveDistinctColoursWithTheBasicPairAtDifferentSlots()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();

            var red = configuration.RedHand.DealtDefinitions;
            var blue = configuration.BlueHand.DealtDefinitions;

            // p.15: "You should now have exactly 5 cards, 1 of each color, in your hand", and p.6 deals
            // that hand as three Tier I cards plus the two Basic cards with no Tier.
            foreach (var hand in new[] { red, blue })
            {
                Assert.That(hand.Select(definition => definition.Progression.Color).Distinct().Count(),
                    Is.EqualTo(5));
                Assert.That(hand.Count(definition => definition.Progression.IsBasic), Is.EqualTo(2));
                Assert.That(
                    hand.Where(definition => definition.Progression.IsBasic)
                        .Select(definition => definition.Progression.Color)
                        .OrderBy(color => color),
                    Is.EqualTo(new[] { CardColor.Gold, CardColor.Silver }));
                Assert.That(
                    hand.Where(definition => definition.Progression.IsNonBasic)
                        .Select(definition => definition.Progression.Tier),
                    Is.All.EqualTo(CardTier.I));
            }

            // Not the hand slot: the Basic pair sits at different positions in the two hands.
            Assert.That(BasicSlots(red), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(BasicSlots(blue), Is.EqualTo(new[] { 2, 4 }));
        }

        [Test]
        public void CardColour_IsNeitherTeamIdentityNorThePrintedActionIcon()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var red = configuration.RedHand.DealtDefinitions;
            var blue = configuration.BlueHand.DealtDefinitions;

            // Both teams hold a Red-coloured card and a Blue-coloured card, so no reading of TeamSide
            // reproduces either column. CardColor.Red and TeamSide.Red are different types on purpose
            // and this is the assertion that says so.
            foreach (var hand in new[] { red, blue })
            {
                Assert.That(hand.Any(definition => definition.Progression.Color == CardColor.Red), Is.True);
                Assert.That(hand.Any(definition => definition.Progression.Color == CardColor.Blue), Is.True);
            }

            // Not the display name either: every card in a hand shares that hand's word while the five
            // colours differ, so a name-derived rule would have to answer the same for all five.
            Assert.That(red, Has.All.Property("DisplayName").StartsWith("TEST - Red "));
            Assert.That(blue, Has.All.Property("DisplayName").StartsWith("TEST - Blue "));

            // And not the printed primary action, which p.7 ties to colour for official cards. Red
            // Lancer prints a Ranged Attack and is Gold; Red Bulwark prints a Skill and is Blue.
            var lancer = red.Single(definition => definition.Id.Equals(RedLancer));
            Assert.That(lancer.Progression.Color, Is.EqualTo(CardColor.Gold));
            Assert.That(
                lancer.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary).Kind,
                Is.EqualTo(ActionKind.Attack));

            var bulwark = red.Single(definition => definition.Id.Equals(RedBulwark));
            Assert.That(bulwark.Progression.Color, Is.EqualTo(CardColor.Blue));
            Assert.That(
                bulwark.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary).Kind,
                Is.EqualTo(ActionKind.Skill));
        }

        // ----- Acceptance checks 1 and 5: Basic has no Tier; non-Basic states explicit candidates -----

        [Test]
        public void BasicCards_HaveNoTierAndNoUpgradeCandidates()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var basics = configuration.CardDefinitions
                .Where(definition => definition.Progression.IsBasic)
                .ToArray();

            Assert.That(basics, Is.Not.Empty);
            foreach (var basic in basics)
            {
                Assert.That(basic.Progression.Tier, Is.EqualTo(CardTier.None), basic.Id.Value);
                Assert.That(basic.Progression.HasTier, Is.False, basic.Id.Value);
                Assert.That(
                    CardCharacteristicQuery.FindUpgradeCandidates(basic.Id, configuration.CardDefinitions),
                    Is.Empty,
                    basic.Id.Value);
            }

            // p.15 names the pair by colour, and the profile refuses any other reading of "Basic".
            Assert.Throws<ArgumentException>(() => CardProgressionProfile.BasicCard(CardColor.Red));
            Assert.Throws<ArgumentException>(() =>
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.None, PrintedValueChannel.None));
        }

        [Test]
        public void EachTierOneCard_UpgradesIntoTwoSameColourTierTwoCardsThatShareTwoTierThreeCards()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var definitions = configuration.CardDefinitions;
            var bulwark = definitions.Single(definition => definition.Id.Equals(RedBulwark));

            var tierTwo = CardCharacteristicQuery.FindUpgradeCandidates(RedBulwark, definitions);
            Assert.That(tierTwo, Has.Count.EqualTo(2));
            Assert.That(tierTwo.Select(candidate => candidate.Progression.Tier), Is.All.EqualTo(CardTier.II));
            Assert.That(
                tierTwo.Select(candidate => candidate.Progression.Color),
                Is.All.EqualTo(bulwark.Progression.Color));

            // p.15 sends the player back to the same deck with the same instruction, and by then the
            // other Tier II card has been set aside as an item, so both Tier II cards name the same
            // two Tier III cards.
            var firstTierThree = CardCharacteristicQuery.FindUpgradeCandidates(tierTwo[0].Id, definitions);
            var secondTierThree = CardCharacteristicQuery.FindUpgradeCandidates(tierTwo[1].Id, definitions);
            Assert.That(firstTierThree.Select(candidate => candidate.Id),
                Is.EqualTo(secondTierThree.Select(candidate => candidate.Id)));
            Assert.That(firstTierThree, Has.Count.EqualTo(2));
            Assert.That(firstTierThree.Select(candidate => candidate.Progression.Tier),
                Is.All.EqualTo(CardTier.III));
            Assert.That(
                firstTierThree.Select(candidate => candidate.Progression.Color),
                Is.All.EqualTo(bulwark.Progression.Color));

            // Tier III names none: p.16 unlocks the Tier IV Ultimate rather than upgrading into it.
            Assert.That(
                CardCharacteristicQuery.FindUpgradeCandidates(firstTierThree[0].Id, definitions),
                Is.Empty);

            // Every non-Basic definition in the configured content answers the same shape.
            foreach (var definition in definitions.Where(value => value.Progression.IsNonBasic))
            {
                var expected = definition.Progression.Tier == CardTier.III ? 0 : 2;
                Assert.That(
                    CardCharacteristicQuery.FindUpgradeCandidates(definition.Id, definitions),
                    Has.Count.EqualTo(expected),
                    definition.Id.Value);
            }
        }

        [Test]
        public void UpgradeRelationships_AreCheckedAgainstColourAndTierRatherThanInferredFromThem()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            Assert.DoesNotThrow(() => CardProgressionValidator.Validate(configuration.CardDefinitions));

            // A candidate of a different colour, a candidate that is not one Tier higher, a candidate
            // the set does not supply, and the wrong number of candidates are each refused - which is
            // what makes the relationship explicit content rather than something colour implies.
            var green = Definition("green-i", CardProgressionProfile.NonBasicCard(
                CardColor.Green, CardTier.I, PrintedValueChannel.None,
                new CardDefinitionId("wrong-colour"), new CardDefinitionId("wrong-tier")));
            var wrongColour = Definition("wrong-colour",
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.II, PrintedValueChannel.Attack));
            var wrongTier = Definition("wrong-tier",
                CardProgressionProfile.NonBasicCard(CardColor.Green, CardTier.III, PrintedValueChannel.Movement));

            Assert.Throws<ArgumentException>(() =>
                CardProgressionValidator.Validate(new[] { green, wrongColour, wrongTier }));
            Assert.Throws<ArgumentException>(() => CardProgressionValidator.Validate(new[] { green }));
            Assert.Throws<ArgumentException>(() => CardProgressionValidator.Validate(new[]
            {
                Definition("lonely", CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    new CardDefinitionId("only-one"))),
                Definition("only-one", CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack))
            }));
        }

        // ----- Acceptance check 3 and 9: context-aware, deterministic, information-suppressing -----

        [Test]
        public void ColourFilteredCandidates_RespectOwnershipLocationLifecycleAndOrdering()
        {
            var fixture = new ProgressionFixture();

            var redsBlue = CardCharacteristicQuery.FindCandidates(
                new CardCharacteristicCriteria(
                    ownerId: fixture.RedHeroId,
                    colors: new[] { CardColor.Blue },
                    locations: new[] { CardLocation.Hand }),
                fixture.Definitions,
                fixture.Instances);
            Assert.That(redsBlue, Is.EqualTo(new[] { fixture.CardOf(RedBulwark).Id }));

            // The same colour, asked of every hand rather than one hero's, reaches both teams: colour is
            // a card characteristic and ownership is a separate predicate.
            var blueCriteria = new CardCharacteristicCriteria(
                colors: new[] { CardColor.Blue }, locations: new[] { CardLocation.Hand });
            var everyBlue = CardCharacteristicQuery.FindCandidates(
                blueCriteria, fixture.Definitions, fixture.Instances);
            Assert.That(everyBlue, Has.Count.EqualTo(2));
            Assert.That(
                everyBlue.Select(id => fixture.Instance(id).OwnerId).Distinct().Count(),
                Is.EqualTo(2));

            // Deterministic: the same state and criteria answer identically whatever order the caller
            // supplies its collections in.
            var reversed = CardCharacteristicQuery.FindCandidates(
                blueCriteria,
                fixture.Definitions.Reverse(),
                fixture.Instances.Reverse());
            Assert.That(reversed, Is.EqualTo(everyBlue));

            // An empty answer is an ordinary answer.
            Assert.That(
                CardCharacteristicQuery.FindCandidates(
                    new CardCharacteristicCriteria(
                        ownerId: fixture.RedHeroId,
                        colors: new[] { CardColor.Gold },
                        locations: new[] { CardLocation.Deck }),
                    fixture.Definitions,
                    fixture.Instances),
                Is.Empty);

            // Lifecycle state narrows independently of colour and location.
            var lancer = fixture.CardOf(RedLancer);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(lancer), Is.True);
            Assert.That(
                CardCharacteristicQuery.FindCandidates(
                    new CardCharacteristicCriteria(
                        ownerId: fixture.RedHeroId, hasBeenPlayed: true),
                    fixture.Definitions,
                    fixture.Instances),
                Is.EqualTo(new[] { lancer.Id }));
        }

        [Test]
        public void AFacedownCard_CannotSatisfyAColourTierOrClassificationPredicate()
        {
            var fixture = new ProgressionFixture();
            var lancer = fixture.CardOf(RedLancer);
            Assert.That(lancer.DefinitionReference.DefinitionId, Is.EqualTo(RedLancer));

            var goldInHand = new CardCharacteristicCriteria(
                ownerId: fixture.RedHeroId, colors: new[] { CardColor.Gold });
            Assert.That(
                CardCharacteristicQuery.FindCandidates(goldInHand, fixture.Definitions, fixture.Instances),
                Is.EqualTo(new[] { lancer.Id }));

            Assert.That(CardLifecycleAuthority.TryPlayFacedown(lancer), Is.True);
            Assert.That(lancer.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));

            // The definition data is still inspectable here - that is exactly the point. What changed is
            // that the acting rules context may no longer read it.
            Assert.That(fixture.DefinitionOf(RedLancer).Progression.Color, Is.EqualTo(CardColor.Gold));
            Assert.That(
                CardCharacteristicQuery.FindCandidates(goldInHand, fixture.Definitions, fixture.Instances),
                Is.Empty);
            Assert.That(
                CardCharacteristicQuery.FindCandidates(
                    new CardCharacteristicCriteria(
                        ownerId: fixture.RedHeroId, classification: CardClassification.Basic),
                    fixture.Definitions,
                    fixture.Instances)
                    .Contains(lancer.Id),
                Is.False);

            // A criteria that asks nothing about printed characteristics still finds it, because a
            // facedown card is still somewhere and still belongs to someone.
            Assert.That(
                CardCharacteristicQuery.FindCandidates(
                    new CardCharacteristicCriteria(
                        ownerId: fixture.RedHeroId, locations: new[] { CardLocation.TurnArea }),
                    fixture.Definitions,
                    fixture.Instances),
                Is.EqualTo(new[] { lancer.Id }));

            // Revealing restores the reading, so the suppression is about orientation and not about
            // having been played.
            Assert.That(CardLifecycleAuthority.TryReveal(lancer), Is.True);
            Assert.That(
                CardCharacteristicQuery.FindCandidates(
                    new CardCharacteristicCriteria(colors: new[] { CardColor.Gold },
                        ownerId: fixture.RedHeroId),
                    fixture.Definitions,
                    fixture.Instances),
                Is.EqualTo(new[] { lancer.Id }));
        }

        // ----- Acceptance check 8: the lowest non-Basic Tier -----

        [Test]
        public void LowestNonBasicTier_ReadsTheScopeItIsGivenAndIgnoresTheBasicPair()
        {
            var fixture = new ProgressionFixture();
            var hand = new CardCharacteristicCriteria(
                ownerId: fixture.RedHeroId, locations: new[] { CardLocation.Hand });

            Assert.That(
                CardCharacteristicQuery.TryGetLowestNonBasicTier(
                    hand, fixture.Definitions, fixture.Instances, out var tier),
                Is.True);
            Assert.That(tier, Is.EqualTo(CardTier.I));

            // A hand of Basic cards alone has no lowest Tier, and that is an answer rather than a
            // failure - p.15's reminder is that the Basic pair is "not considered when checking".
            var basicsOnly = new CardCharacteristicCriteria(
                ownerId: fixture.RedHeroId,
                classification: CardClassification.Basic,
                locations: new[] { CardLocation.Hand });
            Assert.That(
                CardCharacteristicQuery.TryGetLowestNonBasicTier(
                    basicsOnly, fixture.Definitions, fixture.Instances, out var noTier),
                Is.False);
            Assert.That(noTier, Is.EqualTo(CardTier.None));

            // The scope is the caller's, and a different scope is a different answer: this hero's deck
            // holds the Tier II and Tier III cards their hand will grow into, so its lowest is II.
            var deck = new CardCharacteristicCriteria(
                ownerId: fixture.RedHeroId, locations: new[] { CardLocation.Deck });
            Assert.That(
                CardCharacteristicQuery.TryGetLowestNonBasicTier(
                    deck, fixture.Definitions, fixture.Instances, out var deckTier),
                Is.True);
            Assert.That(deckTier, Is.EqualTo(CardTier.II));

            // A location nothing occupies answers false rather than throwing.
            Assert.That(
                CardCharacteristicQuery.TryGetLowestNonBasicTier(
                    new CardCharacteristicCriteria(
                        ownerId: fixture.RedHeroId, locations: new[] { CardLocation.ItemSlot }),
                    fixture.Definitions,
                    fixture.Instances,
                    out _),
                Is.False);

            // A Tier II card in the same scope does not lower the answer; a Tier I card is still lowest.
            var upgraded = fixture.UpgradeIntoHand(RedBulwark);
            Assert.That(upgraded.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(
                CardCharacteristicQuery.TryGetLowestNonBasicTier(
                    hand, fixture.Definitions, fixture.Instances, out var stillLowest),
                Is.True);
            Assert.That(stillLowest, Is.EqualTo(CardTier.I));
        }

        // ----- Acceptance check 6: identity survives the upgrade transitions -----

        [Test]
        public void HandDeckAndSetAsideInstances_KeepOwnerAndDefinitionRevisionThroughUpgradeTransitions()
        {
            var fixture = new ProgressionFixture();
            var returned = fixture.CardOf(RedBulwark);
            var owner = returned.OwnerId;
            var reference = returned.DefinitionReference;

            // p.15: "Return this card to your deck."
            Assert.That(CardLifecycleAuthority.TryReturnToDeck(returned), Is.True);
            Assert.That(returned.State.Location, Is.EqualTo(CardLocation.Deck));
            Assert.That(returned.OwnerId, Is.EqualTo(owner));
            Assert.That(returned.DefinitionReference, Is.EqualTo(reference));
            Assert.That(returned.State.HasBeenPlayed, Is.False);
            Assert.That(CardLifecycleAuthority.TryReturnToDeck(returned), Is.False,
                "A card already in the deck is not returned to it again.");

            var candidates = CardCharacteristicQuery.FindUpgradeCandidates(
                RedBulwark, fixture.Definitions);
            var taken = fixture.DeckInstanceOf(candidates[0].Id);
            var setAside = fixture.DeckInstanceOf(candidates[1].Id);
            var takenReference = taken.DefinitionReference;
            var setAsideReference = setAside.DefinitionReference;

            // p.15: "Take one of those cards into your hand and set the other one aside, Facedown."
            Assert.That(CardLifecycleAuthority.TryTakeFromDeckIntoHand(taken), Is.True);
            Assert.That(taken.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(taken.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(taken.OwnerId, Is.EqualTo(owner));
            Assert.That(taken.DefinitionReference, Is.EqualTo(takenReference));

            Assert.That(CardLifecycleAuthority.TrySetAsideFromDeck(setAside), Is.True);
            Assert.That(setAside.State.Location, Is.EqualTo(CardLocation.SetAside));
            Assert.That(setAside.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));
            Assert.That(setAside.OwnerId, Is.EqualTo(owner));
            Assert.That(setAside.DefinitionReference, Is.EqualTo(setAsideReference));

            // Facedown beside the dashboard, its colour is no longer rules-facing.
            Assert.That(
                CardCharacteristicQuery.FindCandidates(
                    new CardCharacteristicCriteria(
                        colors: new[] { CardColor.Blue }, locations: new[] { CardLocation.SetAside }),
                    fixture.Definitions,
                    fixture.Instances),
                Is.Empty);

            // A played card is not a resting card, so none of the three transitions admits one.
            var played = fixture.CardOf(RedNeedle);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(played), Is.True);
            Assert.That(CardLifecycleAuthority.TryReturnToDeck(played), Is.False);
            Assert.That(CardLifecycleAuthority.TrySetAsideFromDeck(played), Is.False);
            Assert.That(CardLifecycleAuthority.TryTakeFromDeckIntoHand(played), Is.False);
        }

        // ----- Acceptance check 4: query, pending choice, and DiscardCard compose -----

        [Test]
        public void AColourRestrictedDiscard_ChoosesThroughATypedPendingChoiceAndMutatesOnlyWhatWasChosen()
        {
            var fixture = new ProgressionFixture();
            var criteria = new CardCharacteristicCriteria(
                ownerId: fixture.BlueHeroId,
                colors: new[] { CardColor.Red },
                locations: new[] { CardLocation.Hand });

            var choice = ColorRestrictedDiscard.Offer(
                criteria, EffectChooserRole.OpposingHero, fixture.Definitions, fixture.Instances);
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Chooser, Is.EqualTo(EffectChooserRole.OpposingHero));
            Assert.That(choice.MinimumSelections, Is.EqualTo(1));
            Assert.That(choice.MaximumSelections, Is.EqualTo(1));
            Assert.That(choice.Candidates.Select(candidate => candidate.Kind),
                Is.All.EqualTo(EffectValueKind.CardInstance));
            Assert.That(
                choice.Candidates.Select(candidate => candidate.CardInstanceId),
                Is.EqualTo(new[] { fixture.CardOf(BlueSentinel).Id }));

            // An answer naming a card the criteria does not select is refused without mutating it -
            // here a card of the wrong colour that is otherwise perfectly discardable.
            var wrongColour = fixture.CardOf(new CardDefinitionId("placeholder-blue-hammer"));
            var refused = ColorRestrictedDiscard.Resolve(
                criteria, wrongColour.Id, fixture.Definitions, fixture.Instances);
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.Rejection, Is.EqualTo(ColorRestrictedDiscardRejection.ChoiceNotOffered));
            Assert.That(wrongColour.State.Location, Is.EqualTo(CardLocation.Hand));

            // The mutation itself decides none of that: handed the same card directly, it discards it,
            // because whose card it is and what colour it is were never its questions.
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(wrongColour), Is.True);
            Assert.That(wrongColour.State.Location, Is.EqualTo(CardLocation.DiscardPile));

            var chosen = fixture.CardOf(BlueSentinel);
            var accepted = ColorRestrictedDiscard.Resolve(
                criteria, chosen.Id, fixture.Definitions, fixture.Instances);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.DiscardedCardId, Is.EqualTo(chosen.Id));
            Assert.That(chosen.State.Location, Is.EqualTo(CardLocation.DiscardPile));

            // With the one candidate gone the criteria selects nothing, and the offer says so rather
            // than producing an empty choice for a player to stare at.
            Assert.That(
                ColorRestrictedDiscard.Offer(
                    criteria, EffectChooserRole.OpposingHero, fixture.Definitions, fixture.Instances),
                Is.Null);
            var noCandidate = ColorRestrictedDiscard.Resolve(
                criteria, chosen.Id, fixture.Definitions, fixture.Instances);
            Assert.That(noCandidate.Rejection, Is.EqualTo(ColorRestrictedDiscardRejection.NoCandidate));
        }

        // ----- Acceptance check 7: authoritative hero Level -----

        [Test]
        public void EveryHero_StartsAtLevelOneAndTheStateRefusesAnIncompleteRestore()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var progression = new HeroProgressionState(configuration.Participants);

            Assert.That(progression.Levels, Has.Count.EqualTo(configuration.Participants.Heroes.Count));
            foreach (var hero in configuration.Participants.Heroes)
            {
                Assert.That(progression.GetLevel(hero.Id).Value, Is.EqualTo(HeroLevel.Minimum));
            }

            Assert.Throws<KeyNotFoundException>(() => progression.GetLevel(new HeroId("nobody")));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeroLevel(HeroLevel.Minimum - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeroLevel(HeroLevel.Maximum + 1));
            Assert.That(default(HeroLevel).IsDefined, Is.False);

            // A non-default Level survives capture and restore, so the state persists a value rather
            // than a default. Nothing in this milestone can produce that Level in a live match -
            // milestone 7.5 owns the purchase - which is exactly why it is proved here.
            var heroes = configuration.Participants.Heroes.Select(hero => hero.Id).ToArray();
            var restored = HeroProgressionState.Restore(
                configuration.Participants,
                new HeroProgressionRestoreState(new[]
                {
                    new KeyValuePair<HeroId, HeroLevel>(heroes[0], new HeroLevel(3)),
                    new KeyValuePair<HeroId, HeroLevel>(heroes[1], new HeroLevel(HeroLevel.Maximum))
                }));
            Assert.That(restored.GetLevel(heroes[0]).Value, Is.EqualTo(3));
            Assert.That(restored.GetLevel(heroes[1]).Value, Is.EqualTo(HeroLevel.Maximum));

            // Coverage is exact: a missing hero, a duplicate, an unknown one, and an undefined Level
            // are each refused rather than filled in.
            Assert.Throws<ArgumentException>(() => HeroProgressionState.Restore(
                configuration.Participants,
                new HeroProgressionRestoreState(new[]
                {
                    new KeyValuePair<HeroId, HeroLevel>(heroes[0], HeroLevel.Starting)
                })));
            Assert.Throws<ArgumentException>(() => HeroProgressionState.Restore(
                configuration.Participants,
                new HeroProgressionRestoreState(new[]
                {
                    new KeyValuePair<HeroId, HeroLevel>(heroes[0], HeroLevel.Starting),
                    new KeyValuePair<HeroId, HeroLevel>(heroes[0], HeroLevel.Starting)
                })));
            Assert.Throws<ArgumentException>(() => HeroProgressionState.Restore(
                configuration.Participants,
                new HeroProgressionRestoreState(new[]
                {
                    new KeyValuePair<HeroId, HeroLevel>(heroes[0], HeroLevel.Starting),
                    new KeyValuePair<HeroId, HeroLevel>(new HeroId("nobody"), HeroLevel.Starting)
                })));
            Assert.Throws<ArgumentException>(() => HeroProgressionState.Restore(
                configuration.Participants,
                new HeroProgressionRestoreState(new[]
                {
                    new KeyValuePair<HeroId, HeroLevel>(heroes[0], HeroLevel.Starting),
                    new KeyValuePair<HeroId, HeroLevel>(heroes[1], default)
                })));
        }

        // ----- Phase 7's durability rule, applied to the one new piece of authoritative state -----

        [Test]
        public void HeroLevels_SurviveSnapshotRestoreReplayUndoAndARefusedCommand()
        {
            var session = CreateSession();
            var beforeAnyCommand = session.CaptureSnapshot();

            // A refused command changes nothing, the new field included: the whole snapshot - which now
            // carries every hero's Level - is byte-identical either side of it.
            var rejected = session.Execute(DirectorCommand.StopMovement());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeAnyCommand));

            var red = session.Projection.Cards.First(card =>
                card.OwnerId.Value == PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blue = session.Projection.Cards.First(card =>
                card.OwnerId.Value == PlaceholderOneVsOneParticipants.BlueHeroIdValue);
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

            // Restart replays from the start, so the initial Levels are reconstructed rather than
            // retained.
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

            // And the Levels themselves come back off the wire rather than being re-derived: the
            // restored round reports one Level per participating hero.
            var envelopeAccepted = RuleEventLogSnapshotEnvelope.TryDeserialize(
                afterBothCommits, out var envelope, out _);
            Assert.That(envelopeAccepted, Is.True);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                envelope.AuthoritativePayload,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
            Assert.That(
                restored.Round.HeroProgression.Levels.Select(entry => entry.Value.Value),
                Is.All.EqualTo(HeroLevel.Minimum));
            Assert.That(
                restored.Round.HeroProgression.Levels.Select(entry => entry.Key.Value),
                Is.EqualTo(session.Projection.Participants.Heroes
                    .Select(hero => hero.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)));
        }

        private static int[] BasicSlots(IReadOnlyList<CardDefinition> hand) =>
            Enumerable.Range(0, hand.Count)
                .Where(index => hand[index].Progression.IsBasic)
                .ToArray();

        private static CardDefinition Definition(string id, CardProgressionProfile progression) =>
            new CardDefinition(
                new CardDefinitionId(id),
                "rev-1",
                $"TEST - {id}",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                progression);

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
        /// The configured content plus one deck instance per upgrade-target definition.
        ///
        /// The deck instances live here rather than in the configured hands because milestone 7.1 moves
        /// no card between hand and deck in a live match - the purchase that does is milestone 7.5's -
        /// and dealing them into the configured scene would change the five-card hand every pinned
        /// digest and card-matrix fixture measures for behaviour this milestone does not add. What the
        /// checks need is instances in all three resting places, and this supplies exactly that.
        /// </summary>
        private sealed class ProgressionFixture
        {
            private readonly PlaceholderOneVsOneTestConfiguration configuration;
            private readonly List<CardInstance> instances;

            public ProgressionFixture()
            {
                configuration = PlaceholderOneVsOneTestConfiguration.Create();
                RedHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
                BlueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
                instances = configuration.CardInstances.ToList();

                foreach (var definition in configuration.CardDefinitions.Where(value =>
                    value.Progression.IsNonBasic && value.Progression.Tier != CardTier.I))
                {
                    var owner = definition.Id.Value.Contains("-red-") ? RedHeroId : BlueHeroId;
                    var card = new CardInstance(
                        new CardInstanceId($"{definition.Id.Value}-card"),
                        definition.Reference,
                        owner,
                        new CardStateFacts(
                            CardLocation.Deck, false, CardResolutionStatus.None,
                            CardFaceOrientation.FaceUp));
                    instances.Add(card);
                }
            }

            public HeroId RedHeroId { get; }
            public HeroId BlueHeroId { get; }
            public IReadOnlyList<CardDefinition> Definitions => configuration.CardDefinitions;
            public IReadOnlyList<CardInstance> Instances => instances;

            public CardInstance Instance(CardInstanceId instanceId) =>
                instances.Single(instance => instance.Id.Equals(instanceId));

            public CardDefinition DefinitionOf(CardDefinitionId definitionId) =>
                Definitions.Single(definition => definition.Id.Equals(definitionId));

            public CardInstance CardOf(CardDefinitionId definitionId) =>
                instances.Single(instance =>
                    instance.DefinitionReference.DefinitionId.Equals(definitionId) &&
                    instance.State.Location == CardLocation.Hand);

            public CardInstance DeckInstanceOf(CardDefinitionId definitionId) =>
                instances.Single(instance =>
                    instance.DefinitionReference.DefinitionId.Equals(definitionId) &&
                    instance.State.Location == CardLocation.Deck);

            /// <summary>
            /// p.15's three transitions performed in order for one card, with no purchase around them.
            /// Returns the definition of the card that entered the hand.
            /// </summary>
            public CardDefinition UpgradeIntoHand(CardDefinitionId tierOneId)
            {
                var candidates = CardCharacteristicQuery.FindUpgradeCandidates(tierOneId, Definitions);
                Assert.That(CardLifecycleAuthority.TryReturnToDeck(CardOf(tierOneId)), Is.True);
                Assert.That(
                    CardLifecycleAuthority.TryTakeFromDeckIntoHand(DeckInstanceOf(candidates[0].Id)),
                    Is.True);
                Assert.That(
                    CardLifecycleAuthority.TrySetAsideFromDeck(DeckInstanceOf(candidates[1].Id)),
                    Is.True);
                return candidates[0];
            }
        }
    }
}
