using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
#if UNITY_EDITOR
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using UnityEngine;
#endif
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112g: Mrak the Rockshaper dealt into both formats, with his rock token, his handlers
    /// and Rock and a Hard Place's trigger registered, the printed three-star, five-role picker row, and
    /// his prepared private art.
    /// </summary>
    public sealed class MrakDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedMrak_DealsTheEighteenPlayableFacesInEitherFormat(int perTeam)
        {
            var mrak = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "mrak");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(26));
            Assert.That(mrak.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.MrakHeroIdValue));
            Assert.That(mrak.DisplayName, Is.EqualTo("Mrak the Rockshaper"));
            Assert.That(mrak.Complexity, Is.EqualTo(HeroComplexity.VeryComplex), "three stars");
            Assert.That(mrak.Roles, Is.EqualTo(HeroRole.Melee | HeroRole.Tokens | HeroRole.Durable |
                HeroRole.Disabler | HeroRole.Tactician));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { mrak }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == mrak.HeroIdValue).ToArray();
            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MrakCardContent.DealtDefinitions.Select(card => card.Id)));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(MrakCardContent.LesserFissure),
                "Lesser Fissure is the p.20 Handicap and returns to the box at setup.");
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MrakCardContent.StartingHand));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck), Is.EqualTo(13));
            Assert.That(HeroLineupComposition.Compose(format, lineup).Heroes.Single(hero =>
                    hero.ContentId.Value == "mrak").SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"));
        }

        [Test]
        public void TheTrustedCatalog_RegistersTheRockHisHandlersAndHisUltimatesTrigger()
        {
            var staged = MatchConfiguration.CreateStagedMrakForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Count(card =>
                card.Id.Value.StartsWith("mrak-", StringComparison.Ordinal)), Is.EqualTo(18));

            var catalogs = MatchContentCatalogs.ForThisBuild();
            Assert.That(catalogs.Components.TryLoadToken(MrakComponentContent.RockReference, out var rock),
                Is.True);
            Assert.That(rock.SupplyCount, Is.EqualTo(3), "MR-Q1");
            foreach (var handler in MrakComponentContent.RegisteredHandlers())
            {
                Assert.That(catalogs.Capabilities.Registered, Does.Contain(handler.Reference),
                    handler.Reference.ToString());
            }

            Assert.That(MrakComponentContent.DeferredHandlers(), Is.Empty, "the deferral is discharged");
            Assert.That(catalogs.TriggeredEffects.Definitions.Any(definition =>
                definition.Id.Equals(MrakCardContent.RockAndAHardPlaceTriggeredEffect.Id)), Is.True);
        }

        [Test]
        public void StagedBoards_DealMrakAloneOnTierThreeAndAgainstTwo()
        {
            var tierThree = MrakCardContent.RockAndAHardPlace;
            var staged = MatchConfiguration.CreateStagedMrakTierThreeForInternalTest(TeamSide.Red);
            var ultimate = staged.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(tierThree));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(staged.CardInstances.Count(card =>
                card.OwnerId.Equals(ultimate.OwnerId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(5));

            var versus = MatchConfiguration.CreateStagedMrakAgainstWaspWithAllyForInternalTest(TeamSide.Red);
            var mrak = versus.CardInstances.First(card =>
                card.DefinitionReference.DefinitionId.Equals(MrakCardContent.Fissure)).OwnerId;
            Assert.That(versus.Participants.Heroes.Count(hero =>
                versus.Participants.GetTeamForHero(hero.Id).Side !=
                    versus.Participants.GetTeamForHero(mrak).Side), Is.EqualTo(2),
                "two enemy heroes, the board Stone Grip and Rock and a Hard Place need");
            Assert.That(QuickGameSetupConfiguration.CreateStagedMrakVersusWaspAndArien().Heroes
                .Single(hero => hero.ContentId.Value == "mrak").HeroId.Value,
                Is.EqualTo(StagedBoardParticipants.RedHeroIdValue));
        }

        #if UNITY_EDITOR
        [Test]
        public void PreparedPrivateArt_LoadsForTheRockTheStandeesAndTheSelectionFront()
        {
            Assert.That(PlacedTokenArtwork.TryLoadTexture(MrakComponentContent.RockId, out var rock), Is.True,
                "run Tools/Prepare-TokenArtwork.ps1 -Hero mrak in this checkout");
            Assert.That(PlacedTokenArtwork.Caption(MrakComponentContent.Rock, true), Is.Empty);
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/mrak-red"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/mrak-blue"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/HeroSelection/hero-mrak"), Is.Not.Null);
            foreach (var definition in MrakCardContent.DealtDefinitions)
            {
                Assert.That(ConfiguredCardArtworkCatalog.Catalog.TryGet(definition.Reference, out _),
                    Is.True, definition.DisplayName);
            }
        }
        #endif
    }
}
