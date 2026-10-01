using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
#if UNITY_EDITOR
using GuardsDigital.Presentation.Match;
using UnityEngine;
#endif
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Milestone 8.93f: Snorri dealt into both formats, with his four rune markers.</summary>
    public sealed class SnorriDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedSnorri_DealsTheEighteenPlayableFacesInEitherFormat(int perTeam)
        {
            var snorri = RegisteredHeroRoster.ForThisBuild.Single(hero =>
                hero.ContentId.Value == "snorri");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(29));
            Assert.That(snorri.HeroIdValue,
                Is.EqualTo(QuickGameSetupConfiguration.SnorriHeroIdValue));
            Assert.That(snorri.DisplayName, Is.EqualTo("Snorri the Runescribe"));
            Assert.That(snorri.Complexity, Is.EqualTo(HeroComplexity.Wizard));
            Assert.That(snorri.Roles,
                Is.EqualTo(HeroRole.Sniper | HeroRole.Farming | HeroRole.Durable |
                    HeroRole.Damager | HeroRole.Pusher | HeroRole.Healer));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { snorri }.Concat(printed.Red.Skip(1)),
                printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick :
                MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == snorri.HeroIdValue).ToArray();
            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(SnorriCardContent.DealtDefinitions.Select(card => card.Id)));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(SnorriCardContent.ReturnedToBoxAtSetup.Single()),
                "Rune Shards is the p.20 Handicap and returns to the box at setup.");
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(SnorriCardContent.StartingHand));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck),
                Is.EqualTo(13));
            Assert.That(HeroLineupComposition.Compose(format, lineup).Heroes.Single(hero =>
                    hero.ContentId.Value == "snorri").SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"));
        }

        [Test]
        public void StagedBoard_AndTrustedCatalog_RegisterCardsRunesAndRuneMastery()
        {
            var staged = MatchConfiguration.CreateStagedSnorriForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Count(card =>
                card.Id.Value.StartsWith("snorri-", StringComparison.Ordinal)),
                Is.EqualTo(18));

            var catalogs = MatchContentCatalogs.ForThisBuild();
            foreach (var rune in SnorriComponentContent.Runes)
            {
                Assert.That(catalogs.Components.TryLoadMarker(rune.Reference, out var loaded),
                    Is.True, rune.Id.Value);
                Assert.That(loaded.ReturnCondition,
                    Is.EqualTo(MarkerReturnCondition.RetainedAtRoundEndAndDefeat));
            }

            Assert.That(catalogs.TriggeredEffects.Definitions.Any(definition =>
                definition.Id.Equals(SnorriUltimateContent.TriggeredEffect.Id)), Is.True);
        }

        [Test]
        public void JourneyBoards_DealSnorriWithAFriendOrAgainstTwo()
        {
            var friend = MatchConfiguration.CreateStagedSnorriWithAllyForInternalTest(TeamSide.Red);
            var versus = MatchConfiguration.CreateStagedSnorriAgainstWaspWithAllyForInternalTest(TeamSide.Red);
            foreach (var configuration in new[] { friend, versus })
                Assert.That(configuration.CardInstances.Count(card =>
                    card.DefinitionReference.DefinitionId.Value.StartsWith("snorri-",
                        StringComparison.Ordinal)), Is.EqualTo(18));
            HeroId OwnerOf(MatchConfiguration configuration, CardDefinitionId face) =>
                configuration.CardInstances.First(card =>
                    card.DefinitionReference.DefinitionId.Equals(face)).OwnerId;
            Assert.That(friend.Participants.GetTeamForHero(
                    OwnerOf(friend, SnorriSilverFace.InscribeTheRunes)).Side,
                Is.EqualTo(friend.Participants.GetTeamForHero(
                    OwnerOf(friend, ArienCardContent.AllDefinitions[0].Id)).Side));
            Assert.That(versus.Participants.GetTeamForHero(
                    OwnerOf(versus, SnorriSilverFace.InscribeTheRunes)).Side,
                Is.Not.EqualTo(versus.Participants.GetTeamForHero(
                    OwnerOf(versus, ArienCardContent.AllDefinitions[0].Id)).Side));
            Assert.That(QuickGameSetupConfiguration.CreateStagedSnorriVersusWaspAndArien().Heroes,
                Has.Count.EqualTo(3));
            Assert.That(QuickGameSetupConfiguration.CreateStagedWaspVersusSnorriWithAllyOneVsTwo().Heroes,
                Has.Count.EqualTo(3));
        }

        [Test]
        public void TierThreeStaging_UnlocksRuneMasteryFaceUp()
        {
            var staged = MatchConfiguration.CreateStagedSnorriTierThreeForInternalTest(
                TeamSide.Red);
            var mastery = staged.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(SnorriUltimateContent.RuneMastery));
            Assert.That(mastery.State.Location, Is.Not.EqualTo(CardLocation.Deck));
            Assert.That(staged.CardInstances.Count(card =>
                card.OwnerId.Equals(mastery.OwnerId) &&
                card.State.Location == CardLocation.Hand), Is.EqualTo(5));
        }

        #if UNITY_EDITOR
        [Test]
        public void TurnSlotModels_CarryTheRuneAssignedToThatSlotOnly()
        {
            var snorri = new HeroId(QuickGameSetupConfiguration.SnorriHeroIdValue);
            var rune = new MarkerAttachment(new MarkerInstanceId("rune-3"),
                SnorriComponentContent.Runes[2], snorri, snorri,
                new CardInstanceId("silver"), SnorriSilverFace.PrimarySkill, 2);
            var marker = new TurnSlotMarkerViewModel(rune);

            Assert.That(marker.TurnSlotIndex, Is.EqualTo(2));
            Assert.That(marker.ResourcePath, Is.EqualTo("Private/Markers/snorri-rune-3"));
            var slot = new HeroTurnSlotViewModel(2, CardColor.None, false, null, marker);
            Assert.That(slot.TurnSlotMarker, Is.SameAs(marker));
            Assert.That(slot.AssistiveLabel, Does.EndWith("Rune 3"));
            Assert.Throws<ArgumentException>(() =>
                new HeroTurnSlotViewModel(3, CardColor.None, false, null, marker));
            Assert.Throws<ArgumentException>(() => new TurnSlotMarkerViewModel(
                new MarkerAttachment(new MarkerInstanceId("loose"),
                    SnorriComponentContent.Runes[0], snorri, snorri,
                    new CardInstanceId("silver"), SnorriSilverFace.PrimarySkill)));
        }

        [Test]
        public void PreparedRuneArtwork_LoadsForEveryRegisteredRune()
        {
            foreach (var rune in SnorriComponentContent.Runes)
                Assert.That(Resources.Load<Texture2D>("Private/Markers/" + rune.Id.Value),
                    Is.Not.Null,
                    rune.Id.Value + ": run Tools/Prepare-MarkerArtwork.ps1 in this checkout.");
        }
        #endif
    }
}
