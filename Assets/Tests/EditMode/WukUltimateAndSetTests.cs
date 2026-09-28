using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.WukRound;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114e. March of Nature - "Each time after you resolve a card, you may place a Tree
    /// token in radius" - through a configured round, and Wuk's printed set reconciled against his
    /// shard's inventory: values read against the fronts, p.15's upgrade lines, the starting hand, the
    /// deck and the boxed Handicap.
    /// </summary>
    public sealed class WukUltimateAndSetTests
    {
        private static CardDefinition Face(CardDefinitionId id) => WukCardContent.Definition(id);

        // --- March of Nature ------------------------------------------------------------------------

        [Test]
        public void MarchOfNature_AfterWukResolvesAColouredCard_MayPlaceATreeInItsPrintedRadius()
        {
            var round = new WukRound(Face(WukGreenFaces.TossAway), enemyMinionsAt: new[] { H(1, 0) },
                withUltimate: true);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(-3, 0));

            Assert.That(round.PendingNode, Is.EqualTo(WukCardContent.MarchOfNaturePlaceNode),
                "Toss Away resolved: the Ultimate asks where a Tree may go (a full pile needs nothing back).");
            Assert.That(round.OfferedSpaces(), Has.All.Matches<HexCoordinate>(space =>
                HexDistanceQuery.Calculate(space, H(0, 0)) <= 3), "the Ultimate's printed Radius 3.");
            Assert.That(round.OfferedSpaces(), Has.Member(H(0, 3)));
            round.PickSpaces(H(0, 3));

            Assert.That(round.Trees(), Is.EqualTo(new[] { H(0, 3) }));
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void MarchOfNature_ABasicCardResolvingFiresItToo()
        {
            var round = new WukRound(WukCardContent.MysticSaplingsDefinition, withUltimate: true);
            round.StartPrimarySkill();
            round.Decline();
            round.Decline();
            round.Decline();

            Assert.That(round.PendingNode, Is.EqualTo(WukCardContent.MarchOfNaturePlaceNode),
                "\"a card\" - his Silver basic, resolved while placing nothing, is still a card resolved.");
        }

        [Test]
        public void MarchOfNature_IsAMay()
        {
            var round = new WukRound(Face(WukGreenFaces.TossAway), enemyMinionsAt: new[] { H(1, 0) },
                withUltimate: true);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(-3, 0));
            round.Decline();

            Assert.That(round.Trees(), Is.Empty);
            Assert.That(round.ActionEnded, Is.True);
        }

        [Test]
        public void MarchOfNature_WithTheWholePileStanding_AsksWhichTreeComesBackFirst()
        {
            var standing = new[] { H(4, -4), H(-4, 4), H(4, 0) };
            var round = new WukRound(Face(WukGreenFaces.TossAway), enemyMinionsAt: new[] { H(1, 0) },
                treesAt: standing, withUltimate: true);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(-3, 0));

            Assert.That(round.PendingNode, Is.EqualTo(WukCardContent.MarchOfNatureReclaimNode),
                "p.18's short supply before the placement it makes room for.");
            round.PickSpaces(standing[0]);
            round.PickSpaces(H(0, 3));
            Assert.That(round.Trees(), Is.EquivalentTo(new[] { standing[1], standing[2], H(0, 3) }));
        }

        [Test]
        public void MarchOfNature_ThatIsNotUnlocked_DoesNothing()
        {
            var round = new WukRound(Face(WukGreenFaces.TossAway), enemyMinionsAt: new[] { H(1, 0) });
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(-3, 0));

            Assert.That(round.ActionEnded, Is.True, "no Ultimate, no question after the card resolves.");
        }

        [Test]
        public void MarchOfNature_APausedPlacementRestoresAndReplaysToTheSameBytes()
        {
            var round = new WukRound(Face(WukGreenFaces.TossAway), enemyMinionsAt: new[] { H(1, 0) },
                withUltimate: true);
            round.StartPrimarySkill();
            round.PickSpaces(H(1, 0));
            round.PickSpaces(H(-3, 0));

            round.AssertRestoreReplays(H(0, 2));
        }

        [Test]
        public void TheResolutionFact_IsTheCardResolvingOnly_NeverADefenseARepeatOrAGrant()
        {
            var source = new CardInstanceId("card");
            var action = new ActionDefinitionId("primary-skill");
            foreach (var context in System.Enum.GetValues(typeof(ActionInvocationContext))
                         .Cast<ActionInvocationContext>().Where(value => value != ActionInvocationContext.None))
            {
                var fact = PerformedCardResolution.ForCompletedAction(
                    Wuk, source, action, BasicActionQuery.IsCardResolution(context));
                var resolves = context == ActionInvocationContext.PrintedPrimary ||
                    context == ActionInvocationContext.PrintedSecondary ||
                    context == ActionInvocationContext.GeneralRuleSecondary ||
                    context == ActionInvocationContext.Replacement;
                Assert.That(fact.IsDefined, Is.EqualTo(resolves),
                    context + ": p.10 resolves a played card by performing an action on it - a Defense " +
                    "discards it, and a repeat or a granted action is performed off a card already resolved.");
            }
        }

        [Test]
        public void EveryControlHisComposedFacesRaise_HasItsWords_AndNoOtherIsDeclared()
        {
            var catalogs = new MatchContentCatalogs(
                GuardsDigital.Core.Effects.Synthetic.SyntheticEffectCapabilities.CreateCatalog(
                    WukComponentContent.RegisteredHandlers()),
                new GuardsDigital.Core.Components.ComponentDefinitionCatalog(
                    WukComponentContent.Tokens(), System.Array.Empty<GuardsDigital.Core.Components.MarkerDefinition>()),
                new GuardsDigital.Core.Effects.TriggeredEffectCatalog(WukCardContent.TriggeredEffects()));
            var required = DecisionLabelRequirementQuery.For(WukCardContent.DealtDefinitions, catalogs)
                .Select(requirement => requirement.CardId.Value + " / " + requirement.NodeId.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal).ToArray();
            var declared = WukCardContent.DecisionLabels()
                .Select(declaration => declaration.CardId.Value + " / " + declaration.NodeId.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal).ToArray();

            Assert.That(declared, Is.EqualTo(required),
                "the controls his dealt faces raise are exactly the ones he declares words for - derived " +
                "from the resolver's own rule, so the dealing's one catalog line cannot land a face short.");
            Assert.That(required.Length, Is.EqualTo(22));
        }

        // --- The set --------------------------------------------------------------------------------

        [Test]
        public void TheSetIsTheShardsNineteenRows_AllComposed()
        {
            Assert.That(WukCardContent.InventoryOrder.Count, Is.EqualTo(19));
            Assert.That(WukCardContent.NotYetComposed, Is.Empty,
                "8.114b1 composed the trampling moves and Dominance; the dealing requires none owed.");
            Assert.That(WukCardContent.AllDefinitions.Select(definition => definition.Id),
                Is.EqualTo(WukCardContent.InventoryOrder),
                "every face, in the shard's order.");
            Assert.That(WukCardContent.StartingHand, Is.EqualTo(new[]
            {
                WukCardContent.TreeSlam, WukCardContent.MysticSaplings, WukRedFaces.NaturesProtector,
                WukBlueFaces.GiftsOfNature, WukGreenFaces.TossAway
            }), "p.6: Gold, Silver and each colour's Tier I.");
            Assert.That(WukCardContent.ReturnedToBoxAtSetup, Is.EqualTo(new[] { WukCardContent.MakeshiftWeapon }));
            Assert.That(WukCardContent.Deck.Count, Is.EqualTo(13), "nineteen less five in hand less the Handicap.");
        }

        /// <summary>
        /// Carried lesson 10: every printed value read from the definitions against the shard's
        /// inventory table (which was read from the fronts by shape, not slot).
        /// Columns: id, Initiative, secondary Defense, secondary Movement (0 = none), item.
        /// </summary>
        [Test]
        public void EveryComposedFace_CarriesItsPrintedValues()
        {
            var table = new Dictionary<CardDefinitionId, (int Initiative, int Defense, int Movement, PrintedValueChannel Item)>
            {
                [WukCardContent.TreeSlam] = (11, 3, 1, PrintedValueChannel.None),
                [WukCardContent.MysticSaplings] = (4, 4, 0, PrintedValueChannel.None),
                [WukRedFaces.NaturesProtector] = (7, 7, 4, PrintedValueChannel.None),
                [WukRedFaces.NaturesGuardian] = (7, 8, 4, PrintedValueChannel.Initiative),
                [WukRedFaces.NaturesChampion] = (8, 8, 4, PrintedValueChannel.Initiative),
                [WukBlueFaces.GiftsOfNature] = (8, 5, 2, PrintedValueChannel.None),
                [WukBlueFaces.TreeOfPlenty] = (9, 6, 2, PrintedValueChannel.Attack),
                [WukBlueFaces.Abundance] = (9, 6, 2, PrintedValueChannel.Range),
                [WukGreenFaces.TossAway] = (6, 4, 2, PrintedValueChannel.None),
                [WukGreenFaces.MightyThrow] = (5, 4, 2, PrintedValueChannel.Attack),
                [WukGreenFaces.IntoTheCanopy] = (5, 4, 2, PrintedValueChannel.Defense),
                [WukGreenFaces.MonstrousThrow] = (5, 5, 2, PrintedValueChannel.Attack),
                [WukGreenFaces.TreetopRide] = (5, 5, 2, PrintedValueChannel.Defense),
                [WukCardContent.MakeshiftWeapon] = (10, 2, 1, PrintedValueChannel.None)
            };

            foreach (var row in table)
            {
                var face = Face(row.Key);
                Assert.That(face.Initiative.Value, Is.EqualTo(row.Value.Initiative), row.Key.Value + " Initiative");
                Assert.That(Secondary(face, ActionKind.Defense, PrintedValueChannel.Defense),
                    Is.EqualTo(row.Value.Defense), row.Key.Value + " Defense");
                Assert.That(Secondary(face, ActionKind.Movement, PrintedValueChannel.Movement),
                    Is.EqualTo(row.Value.Movement), row.Key.Value + " Movement (0 = no icon)");
                Assert.That(face.Progression.ItemIcon, Is.EqualTo(row.Value.Item), row.Key.Value + " item");
            }
        }

        [Test]
        public void EveryUpgradeLine_NamesBothFacesOfTheNextTierOfItsColour()
        {
            var lines = new Dictionary<CardDefinitionId, CardDefinitionId[]>
            {
                [WukRedFaces.NaturesProtector] = new[] { WukRedFaces.NaturesGuardian, WukRedFaces.Trample },
                [WukRedFaces.NaturesGuardian] = new[] { WukRedFaces.NaturesChampion, WukRedFaces.AngryStampede },
                [WukBlueFaces.GiftsOfNature] = new[] { WukBlueFaces.TreeOfPlenty, WukBlueFaces.ClaimDominance },
                [WukBlueFaces.TreeOfPlenty] = new[] { WukBlueFaces.Abundance, WukBlueFaces.AssertDominance },
                [WukGreenFaces.TossAway] = new[] { WukGreenFaces.MightyThrow, WukGreenFaces.IntoTheCanopy },
                [WukGreenFaces.MightyThrow] = new[] { WukGreenFaces.MonstrousThrow, WukGreenFaces.TreetopRide },
                [WukGreenFaces.IntoTheCanopy] = new[] { WukGreenFaces.MonstrousThrow, WukGreenFaces.TreetopRide }
            };

            foreach (var line in lines)
            {
                Assert.That(Face(line.Key).Progression.NextTierCandidateIds, Is.EquivalentTo(line.Value),
                    line.Key.Value + " (p.15: a Tier upgrades to either face of the next Tier of its colour)");
            }
        }

        private static int Secondary(CardDefinition face, ActionKind kind, PrintedValueChannel channel) =>
            face.ActionDefinitions
                .Where(action => action.Kind == kind &&
                    (action.Origin == ActionDefinitionOrigin.PrintedSecondary ||
                        action.Origin == ActionDefinitionOrigin.Reaction))
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount)
                .DefaultIfEmpty(0)
                .Single();
    }
}
