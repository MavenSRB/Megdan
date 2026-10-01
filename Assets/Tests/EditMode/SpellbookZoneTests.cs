using System;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30b: the spellbook zone. A hero may own spell cards that setup creates facedown
    /// in a spellbook; a spell moves from the book to a faceup removed pile and back, and nowhere
    /// else; every seat counts the book and reads the removed faces, while only the owner reads the
    /// book's faces; the zone survives the snapshot (schema v51) and the wire (protocol /31).
    /// Proven on synthetic spells (<see cref="SyntheticSpellbook"/>); no Gydion face is registered.
    /// </summary>
    public sealed class SpellbookZoneTests
    {
        private static readonly HeroId Owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        // -------------------------------------------------------------------------------------------
        // Definitions
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ASpellDefinition_PrintsItsOwnColourClassificationAndTier_AndNoInitiative()
        {
            var definitions = SyntheticSpellbook.Definitions;
            Assert.That(definitions.Select(definition => definition.Progression.IsSpell), Is.All.True);
            Assert.That(definitions.Select(definition =>
                    definition.Progression.Color + "/" + definition.Progression.Classification + "/" +
                    definition.Progression.Tier),
                Is.EqualTo(new[] { "Gold/Basic/None", "Red/NonBasic/I", "Purple/NonBasic/IV" }),
                "A spell carries its printed colour, classification and Tier: the Gold spells are " +
                "basic, and a Tier IV purple spell (Wish) is a non-basic card, not an Ultimate.");
            Assert.That(definitions.Select(definition => definition.Initiative.IsDefined), Is.All.False,
                "A spell prints no Initiative; it is never played.");
            Assert.That(definitions.Select(definition => definition.Progression.HasItemIcon), Is.All.False);
            Assert.That(definitions.SelectMany(definition => definition.Progression.NextTierCandidateIds),
                Is.Empty);
            Assert.That(definitions.Select(definition => definition.Progression.IsUltimate), Is.All.False);

            Assert.That(() => new CardDefinition(
                    new CardDefinitionId("spell-with-initiative"), "rev", "Spell", new InitiativeValue(5),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.SpellCard(CardColor.Red, CardTier.I)),
                Throws.ArgumentException, "A spell that printed an Initiative would print a value it does not have.");
            Assert.That(() => CardProgressionProfile.SpellCard(CardColor.Gold, CardTier.II),
                Throws.ArgumentException);
            Assert.That(() => CardProgressionProfile.SpellCard(CardColor.Red, CardTier.None),
                Throws.ArgumentException);
            Assert.That(CardProgressionProfile.BasicCard(CardColor.Gold).IsSpell, Is.False);

            // A spell is never upgraded and never upgraded into: the set validates with no candidates
            // on a Tier I spell, and refuses an action card that names a spell as its next Tier.
            var tierTwoSpell = new CardDefinition(
                new CardDefinitionId("tier-two-spell"), "rev", "Spell II", default,
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.SpellCard(CardColor.Red, CardTier.II));
            Assert.That(() => CardProgressionValidator.Validate(definitions.Concat(new[] { tierTwoSpell })),
                Throws.Nothing);
            var namesASpell = new CardDefinition(
                new CardDefinitionId("names-a-spell"), "rev", "Upgrade", new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    new CardDefinitionId("tier-two-spell"), new CardDefinitionId("names-a-spell-two")));
            var second = new CardDefinition(
                new CardDefinitionId("names-a-spell-two"), "rev", "Upgrade Two", new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.II, PrintedValueChannel.Attack));
            Assert.That(() => CardProgressionValidator.Validate(new[] { namesASpell, second, tierTwoSpell }),
                Throws.ArgumentException.With.Message.Contains("spell"),
                "A same-colour Tier II spell is still no upgrade: a spell is never upgraded into.");
        }

        // -------------------------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheLifecycle_RemovesASpellFaceup_AndReturnsItFacedown()
        {
            var spell = Spell("lifecycle");
            Assert.That(spell.State, Is.EqualTo(CardStateFacts.InitialInSpellbook()));

            Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(spell), Is.True);
            Assert.That(spell.State, Is.EqualTo(new CardStateFacts(
                CardLocation.RemovedFromSpellbook, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp)),
                "Removed faceup, unplayed: a cast spell is never a played card.");

            Assert.That(CardLifecycleAuthority.TryReturnToSpellbook(spell), Is.True);
            Assert.That(spell.State, Is.EqualTo(CardStateFacts.InitialInSpellbook()),
                "Returned facedown, in exactly the state setup put it in.");

            Assert.That(spell.Id, Is.EqualTo(new CardInstanceId("lifecycle")));
            Assert.That(spell.OwnerId, Is.EqualTo(Owner));
            Assert.That(spell.DefinitionReference, Is.EqualTo(SyntheticSpellbook.Definitions[1].Reference),
                "The same instance moves; its identity, owner and definition never change.");
        }

        [Test]
        public void TheLifecycle_TakesASpellOnlyFromItsOwnPlace_AndNoOtherTransitionMovesOne()
        {
            var others = new[]
            {
                CardStateFacts.InitialInHand(),
                CardStateFacts.InitialInDeck(),
                CardStateFacts.DiscardedUnplayed(),
                new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Unresolved, CardFaceOrientation.FaceDown),
                new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Unresolved, CardFaceOrientation.FaceUp),
                new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp),
                new CardStateFacts(CardLocation.DiscardPile, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp),
                new CardStateFacts(CardLocation.SetAside, false, CardResolutionStatus.None, CardFaceOrientation.FaceDown),
                new CardStateFacts(CardLocation.ItemSlot, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp),
                new CardStateFacts(CardLocation.UltimateArea, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp)
            };
            var removed = new CardStateFacts(
                CardLocation.RemovedFromSpellbook, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp);

            foreach (var state in others.Concat(new[] { removed }))
            {
                var card = Card(state);
                Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(card), Is.False, state.Location.ToString());
                Assert.That(card.State, Is.EqualTo(state), "A refusal changes nothing.");
            }

            foreach (var state in others.Concat(new[] { CardStateFacts.InitialInSpellbook() }))
            {
                var card = Card(state);
                Assert.That(CardLifecycleAuthority.TryReturnToSpellbook(card), Is.False, state.Location.ToString());
                Assert.That(card.State, Is.EqualTo(state), "A refusal changes nothing.");
            }

            // Not vacuous: each transition takes the one place it names.
            Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(Card(CardStateFacts.InitialInSpellbook())), Is.True);
            Assert.That(CardLifecycleAuthority.TryReturnToSpellbook(Card(removed)), Is.True);

            // A spell is never in a hand, deck, discard, turn slot, item slot or Ultimate area: no other
            // transition admits a card from either of its two places.
            foreach (var state in new[] { CardStateFacts.InitialInSpellbook(), removed })
            {
                Func<CardInstance, bool>[] elsewhere =
                {
                    CardLifecycleAuthority.TryPlayFacedown,
                    CardLifecycleAuthority.TryReveal,
                    CardLifecycleAuthority.TryResolveAfterAction,
                    CardLifecycleAuthority.TryDiscardFromHand,
                    CardLifecycleAuthority.TryDiscardResolvedCard,
                    CardLifecycleAuthority.TryReturnToDeck,
                    CardLifecycleAuthority.TryTakeFromDeckIntoHand,
                    CardLifecycleAuthority.TrySetAsideFromDeck,
                    CardLifecycleAuthority.TryPlaceAsItemFromSetAside,
                    CardLifecycleAuthority.TryUnlockUltimateFromDeck,
                    CardLifecycleAuthority.TryRetrieveEligibleCard,
                    CardLifecycleAuthority.TryRetrieveResolvingCard,
                    card => CardLifecycleAuthority.TrySwapResolvedWithHand(
                        Card(new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Resolved,
                            CardFaceOrientation.FaceUp), "resolved"), card),
                    card => CardLifecycleAuthority.TrySwapItemWithRestingCard(
                        Card(new CardStateFacts(CardLocation.ItemSlot, false, CardResolutionStatus.None,
                            CardFaceOrientation.FaceUp), "item"), card)
                };
                foreach (var transition in elsewhere)
                {
                    var card = Card(state);
                    Assert.That(transition(card), Is.False, state.Location + " via " + transition.Method.Name);
                    Assert.That(card.State, Is.EqualTo(state));
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // Setup
        // -------------------------------------------------------------------------------------------

        [Test]
        public void Setup_CreatesTheDeclaredSpellsInTheBook_OwnedByTheirHero()
        {
            var configuration = MatchConfiguration.CreateForSpellbookFixture(TeamSide.Blue);
            var session = CreateSession(false);
            var spells = session.Projection.Cards
                .Where(card => SyntheticSpellbook.Definitions.Any(definition =>
                    definition.Reference.Equals(card.DefinitionReference)))
                .ToArray();

            Assert.That(spells.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(SyntheticSpellbook.Definitions.Select(definition => definition.Id)),
                "One instance of every declared spell.");
            Assert.That(spells.Select(card => card.OwnerId), Is.All.EqualTo(Owner));
            Assert.That(spells.Select(card => card.State), Is.All.EqualTo(CardStateFacts.InitialInSpellbook()),
                "Every spell starts in the book: facedown and unplayed.");
            foreach (var definition in SyntheticSpellbook.Definitions)
            {
                // The later borrow path resolves a spell with `instances.Single`.
                Assert.That(session.Projection.Cards.Single(card =>
                    card.OwnerId.Equals(Owner) && card.DefinitionReference.Equals(definition.Reference)),
                    Is.Not.Null);
                Assert.That(session.Projection.Definitions, Has.Member(definition));
            }

            Assert.That(HeroHandQuery.Count(session.Projection.Cards, Owner), Is.EqualTo(
                HeroHandQuery.Count(session.Projection.Cards, Other)),
                "The book is no part of the hand: both showcase heroes hold the same hand.");
            var hand = configuration.RedHand;
            Assert.That(hand.SpellbookInstances.Select(card => card.DefinitionReference),
                Is.EqualTo(SyntheticSpellbook.Definitions.Select(definition => definition.Reference)));
            Assert.That(hand.DealtDefinitions.Any(definition => definition.Progression.IsSpell), Is.False,
                "A spell is not dealt.");
            Assert.That(configuration.BlueHand.SpellbookInstances, Is.Empty);

            // The restore path rebuilds the same split (a setup fact read off each definition).
            var restored = MatchConfiguration.Restore(configuration.CaptureRestoreState());
            Assert.That(restored.RedHand.SpellbookInstances.Select(card => card.Id),
                Is.EqualTo(hand.SpellbookInstances.Select(card => card.Id)));
            Assert.That(restored.RedHand.DealtDefinitions.Select(definition => definition.Id),
                Is.EqualTo(hand.DealtDefinitions.Select(definition => definition.Id)));

            // Exactly that location, for exactly spells: a spell dealt to the hand and an action card
            // put in the book are both refused at setup.
            var showcase = configuration.CardDefinitions;
            var spellInHand = CardInstance.CreateInHand(
                new CardInstanceId("spell-in-hand"), SyntheticSpellbook.Definitions[0].Reference, Owner);
            Assert.That(() => CardInitialSetupValidator.Validate(showcase, new[] { spellInHand }),
                Throws.ArgumentException);
            var actionCard = configuration.CardInstances.First(card => card.State.IsInitialInHand);
            var actionInBook = CardInstance.CreateInSpellbook(
                new CardInstanceId("action-in-book"), actionCard.DefinitionReference, Owner);
            Assert.That(() => CardInitialSetupValidator.Validate(showcase, new[] { actionInBook }),
                Throws.ArgumentException);
        }

        // -------------------------------------------------------------------------------------------
        // Privacy and counts
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ANonOwnerSeat_NamesNoFacedownSpell_YetCountsTheBookAndReadsTheRemovedFaces()
        {
            var host = new MatchHost(CreateSession(true));
            var bookIds = host.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Spellbook)
                .Select(card => card.Id.Value)
                .ToArray();
            var removed = host.Projection.Cards.Single(card =>
                card.State.Location == CardLocation.RemovedFromSpellbook);
            Assert.That(bookIds, Has.Length.EqualTo(2));

            var seat = host.Seats.FindByHero(Other);
            var view = host.ProjectionForSeat(seat.Id);

            Assert.That(view.Cards.Select(card => card.Id.Value).Intersect(bookIds), Is.Empty,
                "A facedown spell is named to its owner's seat alone.");
            var publicRemoved = view.PublicCards.Single(card => card.Id.Equals(removed.Id));
            Assert.That(publicRemoved.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(publicRemoved.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
            Assert.That(publicRemoved.DefinitionReference, Is.EqualTo(removed.DefinitionReference),
                "The removed pile is an open zone: every seat reads a cast spell's face (a defender " +
                "must see the Attack it is defending).");
            Assert.That(PublicCardZoneQuery.IsAnOpenZone(CardLocation.RemovedFromSpellbook), Is.True);
            Assert.That(PublicCardZoneQuery.IsAnOpenZone(CardLocation.Spellbook), Is.False);

            var summary = view.HandSummaries.Single(value => value.HeroId.Equals(Owner));
            Assert.That(summary.SpellbookCount, Is.EqualTo(2), "Every seat counts the book.");
            Assert.That(view.HandSummaries.Single(value => value.HeroId.Equals(Other)).SpellbookCount,
                Is.Zero);
            Assert.That(view.HandSummaries.Select(value => value.HandCount),
                Is.EqualTo(host.Projection.HandSummaries.Select(value => value.HandCount)));

            // The seat's whole wire payload names no facedown spell either.
            var payload = Encoding.UTF8.GetString(NetworkSeatProjectionCodec.Encode(view));
            foreach (var id in bookIds)
            {
                Assert.That(payload, Does.Not.Contain(id), "The wire carries " + id + " to a seat that may not see it.");
            }

            Assert.That(payload, Does.Contain(removed.Id.Value));
        }

        [Test]
        public void TheOwnerSeat_ReadsEverySpellFace()
        {
            var host = new MatchHost(CreateSession(true));
            var view = host.ProjectionForSeat(host.Seats.FindByHero(Owner).Id);
            var spells = host.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Spellbook ||
                    card.State.Location == CardLocation.RemovedFromSpellbook)
                .ToArray();

            Assert.That(spells, Has.Length.EqualTo(3));
            foreach (var spell in spells)
            {
                var own = view.OwnCards.Single(card => card.Id.Equals(spell.Id));
                Assert.That(own.DefinitionReference, Is.EqualTo(spell.DefinitionReference));
                Assert.That(own.State, Is.EqualTo(spell.State));
            }

            Assert.That(view.HandSummaries.Single(value => value.HeroId.Equals(Owner)).SpellbookCount,
                Is.EqualTo(2));
        }

        // -------------------------------------------------------------------------------------------
        // Snapshot and wire
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheSpellbook_SurvivesTheSnapshot_AndV50IsRefused()
        {
            var session = CreateSession(true);
            var snapshot = session.CaptureSnapshot();

            var restored = CreateSession(false);
            var result = restored.RestoreSnapshot(snapshot);
            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(snapshot), "Byte-identical round trip.");
            Assert.That(restored.Projection.Cards.Count(card => card.State.Location == CardLocation.Spellbook),
                Is.EqualTo(2));
            var removed = restored.Projection.Cards.Single(card =>
                card.State.Location == CardLocation.RemovedFromSpellbook);
            Assert.That(removed.DefinitionReference.DefinitionId, Is.EqualTo(SyntheticSpellbook.TierOneSpellId));
            Assert.That(removed.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));

            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(51));
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(snapshot, out var envelope), Is.True);
            var v50 = RewriteSchemaVersion(envelope.AuthoritativePayload, 50);
            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    v50,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap }));
            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion),
                "A v50 reader knows no spellbook location, so v50 is refused rather than migrated.");
        }

        [Test]
        public void TheSpellbook_CrossesTheWire_ToEverySeat()
        {
            var host = new MatchHost(CreateSession(true));
            var catalogs = MatchContentCatalogs.ForThisBuild();
            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(view), out var record), Is.True, seat.Id.ToString());
                Assert.That(record.HandSummaries.Select(row => row.HeroId + ":" + row.SpellbookCount),
                    Is.EqualTo(view.HandSummaries.Select(value => value.HeroId.Value + ":" + value.SpellbookCount)));
                Assert.That(record.HandSummaries.Single(row => row.HeroId == Owner.Value).SpellbookCount,
                    Is.EqualTo(2));

                var network = new NetworkMatchReadModel(host.Projection, catalogs, record, true);
                Assert.That(network.HandSummaries.Select(value => value.HeroId + ":" + value.SpellbookCount),
                    Is.EqualTo(view.HandSummaries.Select(value => value.HeroId + ":" + value.SpellbookCount)));
                Assert.That(network.Cards.Select(card => card.Id.Value + "@" + card.State.Location + "/" +
                        card.State.FaceOrientation),
                    Is.EqualTo(view.Cards.Select(card => card.Id.Value + "@" + card.State.Location + "/" +
                        card.State.FaceOrientation)));
                Assert.That(network.Cards.Any(card => card.State.Location == CardLocation.RemovedFromSpellbook),
                    Is.True, "Every seat holds the removed spell.");
            }

            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(32));
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The two-hero staged board of <see cref="MatchConfiguration.CreateForSpellbookFixture"/>,
        /// optionally with the Tier I spell staged in the removed pile - moved there through the
        /// lifecycle's own transition before the match is composed, so every rebuild, restart and
        /// rewind starts from the same state (the sandbox staging rule).
        /// </summary>
        internal static MatchSession CreateSession(bool withOneRemoved)
        {
            var configuration = MatchConfiguration.CreateForSpellbookFixture(TeamSide.Blue);
            if (withOneRemoved)
            {
                var spell = configuration.CardInstances.Single(card =>
                    card.DefinitionReference.DefinitionId.Equals(SyntheticSpellbook.TierOneSpellId));
                Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(spell), Is.True);
            }

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

        private static CardInstance Spell(string id) =>
            CardInstance.CreateInSpellbook(
                new CardInstanceId(id), SyntheticSpellbook.Definitions[1].Reference, Owner);

        private static CardInstance Card(CardStateFacts state, string id = "card") =>
            new CardInstance(new CardInstanceId(id), SyntheticSpellbook.Definitions[1].Reference, Owner, state);

        private static string RewriteSchemaVersion(string payload, int schemaVersion)
        {
            var bytes = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(bytes, true))
            {
                using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    reader.ReadString();
                }

                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(schemaVersion);
                }
            }

            return Convert.ToBase64String(bytes);
        }
    }
}
