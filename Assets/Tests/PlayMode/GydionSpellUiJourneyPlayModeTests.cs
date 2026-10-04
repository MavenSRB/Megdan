using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GydionUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.30j. The rest of Gydion's twenty-two spells, each cast from the front that lists it
    /// by a click on its face, on the staged board <see cref="GydionUiJourneyPlayModeTests"/> measured:
    /// every journey reads what the spell did - a unit moved, swapped, placed, respawned or defeated, a
    /// card discarded or retrieved, coins gained - and the spell faceup on every seat.
    /// </summary>
    public sealed class GydionSpellUiJourneyPlayModeTests
    {
        /// <summary>Beside Red's ranged minion on (-4, 2) and two spaces from him.</summary>
        private static readonly HexCoordinate WaspBesideRedRanged = new HexCoordinate(-5, 2);

        /// <summary>
        /// Spaces in Banishment's Radius 3 that keep a Red minion in the battle zone, measured headlessly:
        /// a minion placed outside it raises p.18's displaced-minion correction, which is not this
        /// spell's question.
        /// </summary>
        private static readonly HexCoordinate[] InTheBattleZone =
        {
            new HexCoordinate(-2, 0), new HexCoordinate(-1, 1), new HexCoordinate(0, 1),
            new HexCoordinate(-5, 3), new HexCoordinate(-5, 2), new HexCoordinate(-2, 3)
        };

        /// <summary>Answers a printed "may" on the decision window's own pair, found by command kind.</summary>
        private static IEnumerator May(MatchSceneCompositionRoot root, bool accept)
        {
            Assert.That(root.Runtime.PendingEffectChoice, Is.Not.Null, Remaining(root.Runtime));
            yield return UiPlayer.WaitForDecisionWindow(root.Runtime, root.Hud);
            UiPlayer.ClickEffectControl(root.Runtime, root.Hud,
                accept ? EffectChoiceCommandKind.Confirm : EffectChoiceCommandKind.Decline);
            yield return null;
            yield return null;
        }

        private static IEnumerator Cast(MatchSceneCompositionRoot root, CardDefinitionId front, CardDefinitionId spell)
        {
            yield return Begin(root, front);
            if (root.Runtime.PendingEffectChoice != null)
                yield return UiPlayer.WaitForDecisionWindow(root.Runtime, root.Hud);
            if (SpellOption(root.Runtime, spell) != null)
            {
                yield return PickSpellFace(root, spell);
            }

            Assert.That(Spell(root.Runtime, spell).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook),
                spell.Value + " is cast: removed from the book faceup. " + Remaining(root.Runtime));
            AssertFaceupOnEverySeat(root.Runtime, spell);
        }

        [UnityTest]
        public IEnumerator ShockingGraspHitsAnAdjacentHeroAndMovesHerOneSpace()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.Cantrip, GydionCardContent.PrepareSpells }, wasp: WaspBesideHim);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            yield return Cast(root, GydionCardContent.Cantrip, GydionCardContent.ShockingGrasp);
            yield return TargetHero(root, wasp);

            // Milestone 9.34 corrective, the same report as the spotlight's: the window Wasp defends in
            // shows the attacking card (8.80), and a spell attack's card is the spell - drawn by its
            // own face, not as a generated hero card.
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Where(element => element.name == "gd-decision-attack-card")
                    .SelectMany(UiPlayer.Descendants)
                    .Any(element => element.name == "gd-spell-face"), Is.True,
                "The defender's window shows Shocking Grasp as its spell face. " + Remaining(runtime));
            yield return ClickDefenseFace(root, WaspCardContent.Shock);
            Assert.That(Card(runtime, wasp, WaspCardContent.Shock).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "Her Shock's Defense 6 holds against Attack 3.");
            Assert.That(OnBoard(runtime, wasp), Is.True);

            // "After the attack: Move the target up to 1 space" - he picks the space.
            var spaces = runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate).ToArray();
            Assert.That(spaces, Is.Not.Empty, "The move is asked after the attack. " + Remaining(runtime));
            Assert.That(spaces.All(space => HexDistanceQuery.Calculate(WaspBesideHim, space) <= 1), Is.True);
            var end = ReachableSpace(root, spaces.Where(space => !space.Equals(WaspBesideHim)));
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(end),
                "One space on.");
            Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(end), "Wasp is moved one space. " + Remaining(runtime));
            Assert.That(HeroView(wasp).Coordinate, Is.EqualTo(end));

            // Milestone 9.34, from live play: "Gydion when he attacks animation shows syntetic card for
            // spell attack, it needs to be actual spell card". The attack completes once its after-
            // the-attack move is answered, and the spotlight on it carries Shocking Grasp's own spell
            // face, drawn where the attacker's card stands.
            // The surface builds the faces once the stage is granted and the spotlight is up, which
            // can trail the scene's arrival; wall-clock, as the other spotlight journeys wait.
            var deadline = UnityEngine.Time.realtimeSinceStartup + 5f;
            while (!runtime.SpotlightView.Model.IsActive && UnityEngine.Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return null;
            var spotlit = runtime.SpotlightView.Model.Scene;
            Assert.That(runtime.SpotlightView.Model.IsActive, Is.True,
                "The completed spell attack's spotlight is up. Phase: " + runtime.SpotlightView.Model.Phase);
            Assert.That(spotlit, Is.Not.Null, "The completed spell attack opens the spotlight.");
            Assert.That(spotlit.AttackSpell?.DefinitionReference.DefinitionId,
                Is.EqualTo(GydionCardContent.ShockingGrasp),
                "The spotlight shows the spell the attack was cast from. Scene: " + spotlit);
            Assert.That(spotlit.AttackCard, Is.Null, "and no generated hero card for it.");
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Where(element => element.name == "gd-spotlight-attacker-card")
                    .SelectMany(UiPlayer.Descendants)
                    .Any(element => element.name == "gd-spell-face"), Is.True,
                "The attacker's card in the spotlight is drawn as the spell face.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator ExpeditiousRetreatWalksOnlyInAStraightLine()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.Cantrip, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            yield return Cast(root, GydionCardContent.Cantrip, GydionCardContent.ExpeditiousRetreat);

            // Its text applies before the walk: a Movement of five, only in a straight line.
            var retreat = Spell(runtime, GydionCardContent.ExpeditiousRetreat);
            Assert.That(runtime.IsActiveEffectSource(retreat.Id), Is.True, "\"Move only in a straight line\" runs. " + Remaining(runtime));
            var walk = runtime.PendingMovementChoice;
            Assert.That(walk, Is.Not.Null, Remaining(runtime));
            Assert.That(walk.LegalDestinations.All(value =>
                    value.Destination.Q == GydionSpace.Q || value.Destination.R == GydionSpace.R ||
                    value.Destination.Q + value.Destination.R == GydionSpace.Q + GydionSpace.R), Is.True,
                "Every destination lies on one of his six straight lines.");
            Assert.That(walk.LegalDestinations.Max(value => value.Cost), Is.EqualTo(5), "Movement 5.");
            var far = ReachableSpace(root, walk.LegalDestinations.OrderByDescending(value => value.Cost)
                .Select(value => value.Destination));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(far).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(SpaceOf(runtime, gydion), Is.EqualTo(far), "He walks the line he clicked. " + Remaining(runtime));
            Assert.That(HeroView(gydion).Coordinate, Is.EqualTo(far));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator BurningHandsMakesAHeroBesideItsTargetDiscard()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.ElementaryEvocation, GydionCardContent.PrepareSpells }, wasp: WaspBesideRedRanged);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            // "Cast the Burning Hands spell" names one spell: no face is asked.
            yield return Cast(root, GydionCardContent.ElementaryEvocation, GydionCardContent.BurningHands);
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetMinionIds, Is.EqualTo(new[] { RedRanged }), Remaining(runtime));
            yield return TargetMinion(root, RedRanged);

            // "Before the attack: Up to 1 enemy hero adjacent to the target discards a card, if able."
            Assert.That(MinionStands(runtime, RedRanged), Is.True, "The text runs before the attack.");
            yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "Wasp, beside the target.");
            var discard = runtime.PendingEffectChoice;
            Assert.That(discard?.ChooserId, Is.EqualTo(wasp), "She picks the card. " + Remaining(runtime));
            var hand = HandCount(runtime, wasp);
            var taken = discard.Candidates.First().Value.CardInstanceId;
            yield return Pick(root, value => value.Kind == EffectValueKind.CardInstance && value.CardInstanceId.Equals(taken),
                "Her first card.");
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(taken)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(HandCount(runtime, wasp), Is.EqualTo(hand - 1));
            Assert.That(MinionStands(runtime, RedRanged), Is.False, "Then Attack 5 defeats the minion.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator FireballPassesOverAUnitBesideHisFriendlyMinion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserEvocation, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            yield return Cast(root, GydionCardContent.LesserEvocation, GydionCardContent.Fireball);

            // "Target a unit in range, not adjacent to you or to your friendly unit": Wasp stands beside
            // his Blue melee minion, so only Red's melee minion on (-1, -1) is offered.
            var target = runtime.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null, Remaining(runtime));
            Assert.That(target.LegalTargetHeroIds, Has.No.Member(wasp));
            Assert.That(target.LegalTargetMinionIds, Is.EqualTo(new[] { new GuardsDigital.Core.Setup.MinionId("red-melee-01") }));
            yield return TargetMinion(root, new GuardsDigital.Core.Setup.MinionId("red-melee-01"));
            Assert.That(MinionStands(runtime, new GuardsDigital.Core.Setup.MinionId("red-melee-01")), Is.False,
                "Attack 5 defeats it.");
            Assert.That(OnBoard(runtime, wasp), Is.True);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator VampiricTouchRetrievesADiscardedCardAfterTheAttack()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserNecromancy, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            yield return Cast(root, GydionCardContent.LesserNecromancy, GydionCardContent.VampiricTouch);
            yield return TargetMinion(root, RedRanged);
            Assert.That(MinionStands(runtime, RedRanged), Is.False, "Attack 5 defeats the minion beside him.");

            // "After the attack: You may retrieve a discarded card."
            var hand = HandCount(runtime, gydion);
            var discarded = DiscardCount(runtime, gydion);
            Assert.That(discarded, Is.GreaterThan(0), "His staged discard holds his p.6 cards.");
            yield return May(root, true);
            var taken = runtime.PendingEffectChoice?.Candidates.First().Value.CardInstanceId;
            Assert.That(taken.HasValue, Is.True, Remaining(runtime));
            yield return Pick(root, value => value.Kind == EffectValueKind.CardInstance && value.CardInstanceId.Equals(taken.Value),
                "A card of his discard.");
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(taken.Value)).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(HandCount(runtime, gydion), Is.EqualTo(hand + 1));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator CreateUndeadRespawnsHisDefeatedMinion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserNecromancy, GydionCardContent.Cantrip, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var minionSpace = MinionAt(runtime, BlueMelee);

            // Turn 1: her Magnetic Dagger (12) acts first and defeats his Blue melee minion.
            CommitBoth(runtime, GydionCardContent.Cantrip, WaspCardContent.MagneticDagger);
            yield return null;
            yield return WaspStartsHerAttack(root);
            yield return TargetMinion(root, BlueMelee);
            Assert.That(MinionStands(runtime, BlueMelee), Is.False);
            UiPlayer.EndAnyTurnInProgress(runtime);
            yield return null;

            // Turn 2: "Respawn a friendly minion in an empty friendly spawn point in range in the battle zone."
            yield return Cast(root, GydionCardContent.LesserNecromancy, GydionCardContent.CreateUndead);
            var points = runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate).ToArray();
            Assert.That(points, Is.EqualTo(new[] { minionSpace }), "Its own empty spawn point, in Range 3. " + Remaining(runtime));
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(minionSpace),
                "The empty spawn point.");
            Assert.That(MinionStands(runtime, BlueMelee), Is.True, "His melee minion is back. " + Remaining(runtime));
            Assert.That(MinionAt(runtime, BlueMelee), Is.EqualTo(minionSpace));
            Assert.That(MinionView(BlueMelee).Occupancy.Coordinate, Is.EqualTo(minionSpace), "Its miniature stands there.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator DominatePersonDefeatsAMinionBesideTheEnemyHero()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserEnchantment, GydionCardContent.PrepareSpells }, wasp: WaspBesideHim);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            var wasp = Wasp(runtime);
            var coins = runtime.CoinsOf(gydion);
            yield return Cast(root, GydionCardContent.LesserEnchantment, GydionCardContent.DominatePerson);
            yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "An enemy hero in radius with an enemy minion beside her.");
            yield return Pick(root, value => value.Kind == EffectValueKind.Minion && value.MinionId.Equals(RedMeleeInTheMiddle),
                "The Red minion beside Wasp.");
            Assert.That(MinionStands(runtime, RedMeleeInTheMiddle), Is.False, "Defeated. " + Remaining(runtime));
            Assert.That(runtime.CoinsOf(gydion), Is.GreaterThan(coins), "A p.13 Defeat by him pays its coins.");
            Assert.That(OnBoard(runtime, wasp), Is.True);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator PowerWordKillDefeatsAnEnemyHeroWithAnEmptyHand()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.GreaterEnchantment, GydionCardContent.PrepareSpells },
                waspHand: new[] { WaspCardContent.StopProjectiles });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            var redLives = root.Hud.Header.RedLifeLabel;
            yield return Cast(root, GydionCardContent.GreaterEnchantment, GydionCardContent.PowerWordKill);
            Assert.That(HandCount(runtime, wasp), Is.EqualTo(0), "She committed her only card.");
            yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "An enemy hero in radius with no cards in hand.");
            Assert.That(OnBoard(runtime, wasp), Is.False, "Defeated outright. " + Remaining(runtime));
            Assert.That(root.Hud.Header.RedLifeLabel, Is.Not.EqualTo(redLives), "Her team's Life heart drops.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator LesserTransmutationGainsCoinsAndDisintegratesAMinion()
        {
            // Midas' Touch: "Gain 1 coin for every two other spell cards removed" - four out, two coins.
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserTransmutation, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            StageTheBook(runtime, new[] { GydionCardContent.Fireball, GydionCardContent.Shield,
                GydionCardContent.Suggestion, GydionCardContent.Wish });
            yield return null;
            var coins = runtime.CoinsOf(gydion);
            yield return Begin(root, GydionCardContent.LesserTransmutation);
            Assert.That(SpellOption(runtime, GydionCardContent.MidasTouch)?.Spell.Caption,
                Is.EqualTo("MIDAS' TOUCH - 4 OUT: 2 COINS"), Remaining(runtime));
            yield return PickSpellFace(root, GydionCardContent.MidasTouch);
            Assert.That(Spell(runtime, GydionCardContent.MidasTouch).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            AssertFaceupOnEverySeat(runtime, GydionCardContent.MidasTouch);
            Assert.That(runtime.CoinsOf(gydion), Is.EqualTo(coins + 2), "Two coins.");
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(gydion)).Coins,
                Is.EqualTo(coins + 2), "His plaque's coin count follows.");
            Healthy(runtime);

            // Disintegrate: "Remove a token or an enemy non-heavy minion adjacent to you" - no Defeat, no coin.
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserTransmutation, GydionCardContent.PrepareSpells });
            root = Root();
            runtime = root.Runtime;
            coins = runtime.CoinsOf(gydion);
            yield return Cast(root, GydionCardContent.LesserTransmutation, GydionCardContent.Disintegrate);
            var subject = MinionAt(runtime, RedRanged);
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(subject),
                "Red's ranged minion beside him.");
            Assert.That(MinionStands(runtime, RedRanged), Is.False, "Removed. " + Remaining(runtime));
            Assert.That(runtime.CoinsOf(gydion), Is.EqualTo(coins), "A removal is not a Defeat: no coins.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator PolymorphSwapsTheEnemyHeroWithAMinion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.GreaterTransmutation, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            var minionSpace = MinionAt(runtime, RedRanged);
            yield return Cast(root, GydionCardContent.GreaterTransmutation, GydionCardContent.Polymorph);
            yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "An enemy hero in radius.");
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(minionSpace),
                "An enemy minion in radius.");
            Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(minionSpace), "Wasp takes the minion's space. " + Remaining(runtime));
            Assert.That(MinionAt(runtime, RedRanged), Is.EqualTo(WaspSpace), "and the minion hers.");
            Assert.That(HeroView(wasp).Coordinate, Is.EqualTo(minionSpace));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator BanishmentPlacesAnAdjacentMinionIntoRadius()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserAbjuration, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var subject = MinionAt(runtime, RedRanged);
            yield return Cast(root, GydionCardContent.LesserAbjuration, GydionCardContent.Banishment);
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(subject),
                "A unit adjacent to him: Red's ranged minion.");
            var offered = runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate).ToArray();
            Assert.That(offered, Is.Not.Empty, Remaining(runtime));
            Assert.That(offered.All(space => HexDistanceQuery.Calculate(GydionSpace, space) <= 3), Is.True, "Radius 3.");
            var to = ReachableSpace(root, InTheBattleZone.Where(offered.Contains));
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(to),
                "A space in radius.");
            Assert.That(MinionAt(runtime, RedRanged), Is.EqualTo(to), "Placed there. " + Remaining(runtime));
            Assert.That(MinionView(RedRanged).Occupancy.Coordinate, Is.EqualTo(to));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator CloudKillMakesTheEnemyDiscardABasicCard()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.GreaterConjuration, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            yield return Cast(root, GydionCardContent.GreaterConjuration, GydionCardContent.CloudKill);
            yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "An enemy hero in radius 3.");
            var discard = runtime.PendingEffectChoice;
            Assert.That(discard?.ChooserId, Is.EqualTo(wasp), Remaining(runtime));
            Assert.That(discard.Candidates.Select(value => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(runtime.Cards.Single(card => card.Id.Equals(value.Value.CardInstanceId))
                        .DefinitionReference)).Progression.Classification).Distinct(),
                Is.EqualTo(new[] { CardClassification.Basic }), "Only her basic cards.");
            var hand = HandCount(runtime, wasp);
            var taken = discard.Candidates.First().Value.CardInstanceId;
            yield return Pick(root, value => value.Kind == EffectValueKind.CardInstance && value.CardInstanceId.Equals(taken),
                "Her first basic card.");
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(taken)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(HandCount(runtime, wasp), Is.EqualTo(hand - 1));
            Healthy(runtime);
        }
    }
}
