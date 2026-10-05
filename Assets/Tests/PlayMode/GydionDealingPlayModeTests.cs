using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GydionUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.30j. Gydion dealt into a loaded scene, on the terms of Razzle's and Mortimer's
    /// dealing fixtures: scenes 142 and 143 through BOTH scene-composition arms (the configuration arm
    /// deals his hand and book, the setup arm names his seat and draws his standee) - which 9.30i
    /// recorded had not yet run in Unity - and a capture case for the user's review at 2560x1440.
    /// </summary>
    public sealed class GydionDealingPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpellbookCardsStayInPlaceWithoutAHoverPopup()
        {
            var oldWidth=(uint)Screen.width; var oldHeight=(uint)Screen.height;
            try
            {
                UiPlayer.SetPlayerViewport(1920,1080,"9.64b spellbook");
                MatchSceneCompositionRoot.NextComposedSeat=null;
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
                var root=Root(); var view=root.Runtime.HeroOverviewsView;
                Assert.That(view.OpenSpellbook(Gydion(root.Runtime)),Is.True);
                root.Hud.Layout(); yield return null; yield return null;
                var cards=UiPlayer.Descendants(view.SpellbookDrawer).Where(e=>e.name=="gd-spellbook-spell").ToArray();
                Assert.That(cards.Length,Is.EqualTo(22));
                foreach(var card in cards.Take(3))
                {
                    var bounds=card.worldBound;
                    UiPlayer.PointAt(card); yield return null;
                    Assert.That(view.HoverPreviewRoot.resolvedStyle.display,Is.EqualTo(DisplayStyle.None),
                        "A readable spellbook card must not spawn a redundant hover face.");
                    Assert.That(card.worldBound,Is.EqualTo(bounds));
                    UiPlayer.PointAway(card);
                }
                yield return UiPlayer.CaptureScreen("tmp/964b-spellbook-no-popup.png");
                UiPlayer.Click(view.SpellbookDrawer.Q<Button>("gd-spellbook-close")); yield return null;
                Assert.That(view.IsSpellbookOpen,Is.False);
            }
            finally { UiPlayer.SetPlayerViewport(oldWidth,oldHeight,"9.64b restore viewport"); }
        }

        [UnityTest]
        public IEnumerator SceneOneFortyTwo_DealsHisStartingHandAFullBookAndHisPreparedFaces()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
            var root = Root();
            var runtime = root.Runtime;

            // The setup arm: his seat is named Gydion and stands on the engaged board against Wasp.
            var seat = runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == GydionCardContent.ContentSetId);
            // A staged one-vs-one seat is named short (QuickGameSetupConfiguration.CreateOneVsOne(..., "Gydion"));
            // the full title is the roster's, which the picker shows.
            Assert.That(seat.DisplayName, Is.EqualTo("Gydion"));
            Assert.That(RegisteredHeroRoster.ForThisBuild
                .Single(hero => hero.ContentId.Value == GydionCardContent.ContentSetId).DisplayName, Is.EqualTo(HisName));
            var gydion = seat.HeroId;
            var wasp = Wasp(runtime);
            Assert.That(SpaceOf(runtime, gydion), Is.EqualTo(GydionSpace));
            Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(WaspSpace));
            var standee = HeroView(gydion);
            Assert.That(standee.HeroContentId.Value.Value, Is.EqualTo(GydionCardContent.ContentSetId));
            Assert.That(standee.HasTokenVisual, Is.True, "His standee wears his prepared team face.");

            // The configuration arm: p.6's five in hand, thirteen in the deck, twenty-two in the book.
            Assert.That(HandOf(runtime, gydion).Select(definition => definition.Id),
                Is.EquivalentTo(GydionCardContent.StartingHand));
            Assert.That(runtime.Cards.Count(card => card.OwnerId.Equals(gydion) && card.State.Location == CardLocation.Deck),
                Is.EqualTo(13));
            Assert.That(InTheBook(runtime), Is.EqualTo(22), "GY-Q1: every spell, Wish included.");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"));
            Assert.That(runtime.Cards.Any(card => card.DefinitionReference.DefinitionId.Equals(GydionCardContent.CheapTrick)),
                Is.False, "Cheap Trick stays boxed (p.20).");

            // His pick card: the prepared selection front, on hovering his rail portrait.
            var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(element =>
                element.name == "gd-hero-rail" && element.userData is HeroId id && id.Equals(gydion));
            UiPlayer.PointAt(rail.Q("gd-hero-medallion"));
            yield return null;
            var face = runtime.HeroOverviewsView.HoverPreviewRoot.Q<Image>("gd-hero-profile-preview");
            Assert.That(face?.image, Is.Not.Null, "His rail portrait previews his pick card.");
            Assert.That(face.image, Is.SameAs(Resources.Load<Texture2D>(
                "Private/HeroSelection/" + QuickGameSetupConfiguration.GydionHeroIdValue)));
            UiPlayer.PointAway(rail.Q("gd-hero-medallion"));
            yield return null;
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator SceneOneFortyThree_DealsTheTierThreeHandWithTheArchwizardFaceupAndWishInTheBook()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedGydion);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            Assert.That(HandOf(runtime, gydion).Select(definition => definition.Id), Is.EquivalentTo(new[]
            {
                GydionCardContent.Cantrip, GydionCardContent.PrepareSpells, GydionCardContent.GreaterEvocation,
                GydionCardContent.GreaterTransmutation, GydionCardContent.GreaterConjuration
            }));
            Assert.That(Card(runtime, gydion, GydionCardContent.TheArchwizard).State.Location, Is.Not.EqualTo(CardLocation.Deck),
                "The Archwizard is unlocked.");
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "The Archwizard stands faceup in his items strip.");
            Assert.That(Spell(runtime, GydionCardContent.Wish).State.Location, Is.EqualTo(CardLocation.Spellbook));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"));

            // Prepare Spells from its control offers the refill or Wish instead (GY-Q7).
            yield return Begin(root, GydionCardContent.PrepareSpells);
            Assert.That(SpellOption(runtime, GydionCardContent.Wish)?.IsAnswerable, Is.True, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(GydionCardContent.PrepareSpellsReplacementChoice));
            Healthy(runtime);
        }

        /// <summary>
        /// The user's review captures at 2560x1440, in <c>tmp/9-30j-gydion/</c>: his starting hand on his
        /// tray, and a cast in progress - Cantrip's three spell faces in the decision window, then Magic
        /// Missile's target lit on the board. The hero picker row is captured by
        /// <c>PrivateLobbyPlayModeTests.GydionsPickerRow_IsCapturedAt2560x1440</c>, the lobby's fixture.
        /// </summary>
        [UnityTest]
        public IEnumerator CapturesHisStartingHandAndACastInProgressAt2560x1440()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                UiPlayer.SetPlayerViewport(2560, 1440, "9.30j Gydion captures");
                MatchSceneCompositionRoot.NextComposedSeat = null;
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
                var root = Root();
                var runtime = root.Runtime;
                var gydion = Gydion(runtime);
                yield return PointTrayAt(root, gydion);
                root.Hud.Layout();
                yield return null;
                yield return null;
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/starting-hand-2560x1440.png");

                yield return Begin(root, GydionCardContent.Cantrip);
                Assert.That(runtime.DecisionWindowView.Model.Controls.Count(control => control.Spell != null), Is.EqualTo(3),
                    Remaining(runtime));
                yield return null;
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/cast-choice-2560x1440.png");

                yield return PickSpellFace(root, GydionCardContent.MagicMissile);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
                Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 21 | 1"));
                yield return null;
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/cast-target-2560x1440.png");
                Healthy(runtime);
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "9.30j restore the viewport");
            }
        }
    }
}
