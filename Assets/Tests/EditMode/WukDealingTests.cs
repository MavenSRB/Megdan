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
    /// Milestone 8.114f: Wuk the Grove Keeper dealt into both formats, with his Tree token, his
    /// handlers and his three triggered effects registered, the printed three-star, six-role picker
    /// row, and his prepared private art.
    /// </summary>
    public sealed class WukDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedWuk_DealsTheEighteenPlayableFacesInEitherFormat(int perTeam)
        {
            var wuk = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "wuk");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(30));
            Assert.That(wuk.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.WukHeroIdValue));
            Assert.That(wuk.DisplayName, Is.EqualTo("Wuk the Grove Keeper"));
            Assert.That(wuk.Complexity, Is.EqualTo(HeroComplexity.VeryComplex), "three stars");
            Assert.That(wuk.Roles, Is.EqualTo(HeroRole.Tokens | HeroRole.Pusher | HeroRole.Durable |
                HeroRole.Sniper | HeroRole.Tactician | HeroRole.Healer));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { wuk }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == wuk.HeroIdValue).ToArray();
            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(WukCardContent.DealtDefinitions.Select(card => card.Id)));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(WukCardContent.MakeshiftWeapon),
                "Makeshift Weapon is the p.20 Handicap and returns to the box at setup.");
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(WukCardContent.StartingHand));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck), Is.EqualTo(13));
            Assert.That(HeroLineupComposition.Compose(format, lineup).Heroes.Single(hero =>
                    hero.ContentId.Value == "wuk").SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"));
        }

        [Test]
        public void TheTrustedCatalog_RegistersTheTreeHisHandlersAndHisTriggeredEffects()
        {
            var staged = MatchConfiguration.CreateStagedWukForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Count(card =>
                card.Id.Value.StartsWith("wuk-", StringComparison.Ordinal)), Is.EqualTo(18));

            var catalogs = MatchContentCatalogs.ForThisBuild();
            Assert.That(catalogs.Components.TryLoadToken(WukComponentContent.TreeReference, out var tree),
                Is.True);
            Assert.That(tree.SupplyCount, Is.EqualTo(3), "WU-Q1");
            foreach (var handler in WukComponentContent.RegisteredHandlers())
            {
                Assert.That(catalogs.Capabilities.Registered, Has.Member(handler.Reference),
                    handler.Reference.ToString());
            }

            Assert.That(WukComponentContent.DeferredHandlers(), Is.Empty, "the deferral is discharged");
            Assert.That(WukCardContent.NotYetComposed, Is.Empty);
            foreach (var triggered in WukCardContent.TriggeredEffects())
            {
                Assert.That(catalogs.TriggeredEffects.Definitions.Any(definition =>
                    definition.Id.Equals(triggered.Id)), Is.True, triggered.Id.ToString());
            }
        }

        [Test]
        public void StagedBoards_DealWukAloneOnTierThreeAndWithAnAlly()
        {
            var staged = MatchConfiguration.CreateStagedWukTierThreeForInternalTest(TeamSide.Red);
            var ultimate = staged.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(WukCardContent.MarchOfNature));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(staged.CardInstances.Count(card =>
                card.OwnerId.Equals(ultimate.OwnerId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(5));

            var withAlly = MatchConfiguration.CreateStagedWukWithAllyForInternalTest(TeamSide.Red);
            var wuk = withAlly.CardInstances.First(card =>
                card.DefinitionReference.DefinitionId.Equals(WukCardContent.TreeSlam)).OwnerId;
            Assert.That(withAlly.Participants.Heroes.Count(hero => !hero.Id.Equals(wuk) &&
                withAlly.Participants.GetTeamForHero(hero.Id).Side ==
                    withAlly.Participants.GetTeamForHero(wuk).Side), Is.EqualTo(1),
                "one friendly hero, the board his friendly retrieval and swaps need");
            Assert.That(QuickGameSetupConfiguration.CreateStagedWaspVersusWukWithAllyOneVsTwo().Heroes
                .Single(hero => hero.ContentId.Value == "wuk").HeroId.Value,
                Is.EqualTo(StagedBoardParticipants.BlueHeroIdValue));
        }

        #if UNITY_EDITOR
        [Test]
        public void PreparedPrivateArt_LoadsForTheTreeTheStandeesAndTheSelectionFront()
        {
            Assert.That(PlacedTokenArtwork.TryLoadTexture(WukComponentContent.TreeId, out _), Is.True,
                "run Tools/Prepare-TokenArtwork.ps1 -Hero wuk in this checkout");
            Assert.That(PlacedTokenArtwork.Caption(WukComponentContent.Tree, true), Is.Empty);
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/wuk-red"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/wuk-blue"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/HeroSelection/hero-wuk"), Is.Not.Null);
            foreach (var definition in WukCardContent.DealtDefinitions)
            {
                Assert.That(ConfiguredCardArtworkCatalog.Catalog.TryGet(definition.Reference, out _),
                    Is.True, definition.DisplayName);
            }
        }
        #endif
    }
}
