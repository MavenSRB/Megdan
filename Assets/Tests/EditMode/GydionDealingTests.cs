using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
#if UNITY_EDITOR
using GuardsDigital.Presentation.Match;
using UnityEngine;
#endif
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30i: Gydion the Archwizard dealt into both formats - eighteen fronts dealt, Cheap
    /// Trick boxed (p.20), all twenty-two spells in his book (GY-Q1) - with every handler his fronts
    /// and spells name registered, the printed four-star, six-role picker row, the casting fronts
    /// composed from the shard's inventory table and Printed clauses, his decision labels, a cast and a
    /// refill from a dealt hand on the staged board, and the bots answering every decision family his
    /// spells raise.
    /// </summary>
    public sealed class GydionDealingTests
    {
        private const string Revision = "gydion-9-30i-rev-1";

        [TestCase(2)]
        [TestCase(3)]
        public void SelectedGydion_DealsEighteenFrontsAndPutsAllTwentyTwoSpellsInHisBook(int perTeam)
        {
            var gydion = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "gydion");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(32));
            Assert.That(gydion.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.GydionHeroIdValue));
            Assert.That(gydion.DisplayName, Is.EqualTo("Gydion the Archwizard"), "GY-02: the dashboard's title.");
            Assert.That(gydion.Complexity, Is.EqualTo(HeroComplexity.Wizard), "GY-01: four stars.");
            Assert.That(gydion.Roles, Is.EqualTo(HeroRole.Sniper | HeroRole.Tactician | HeroRole.Farming |
                HeroRole.Damager | HeroRole.Pusher | HeroRole.Tokens), "GY-01.");

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { gydion }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == gydion.HeroIdValue).ToArray();
            var fronts = cards.Where(card => card.State.Location != CardLocation.Spellbook).ToArray();
            var book = cards.Where(card => card.State.Location == CardLocation.Spellbook).ToArray();

            Assert.That(fronts.Select(card => card.DefinitionReference),
                Is.EquivalentTo(GydionCardContent.DealtDefinitions.Select(card => card.Reference)));
            Assert.That(fronts, Has.Length.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(GydionCardContent.CheapTrick),
                "Cheap Trick is the p.20 Handicap and returns to the box at setup.");
            Assert.That(fronts.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(GydionCardContent.StartingHand));
            Assert.That(fronts.Count(card => card.State.Location == CardLocation.Deck), Is.EqualTo(13));
            Assert.That(book.Select(card => card.DefinitionReference),
                Is.EquivalentTo(GydionCardContent.SpellDefinitions.Select(spell => spell.Reference)),
                "GY-Q1: all twenty-two spells, Wish included, are in the book at setup.");

            // The match composes on the build's own catalogs, and every seat reads his book's size.
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(format, lineup), configuration.Participants, map,
                configuration.TieBreakerState);
            var session = new MatchSession(new MatchSessionInitialState(
                Scenario(configuration, map, setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))),
                setup, new[] { map }));
            var host = new MatchHost(session);
            foreach (var seat in host.Seats.Seats)
            {
                var summary = host.ProjectionForSeat(seat.Id).HandSummaries
                    .Single(value => value.HeroId.Value == gydion.HeroIdValue);
                Assert.That(summary.SpellbookCount, Is.EqualTo(22), seat.HeroId.Value);
                Assert.That(summary.HandCount, Is.EqualTo(5), seat.HeroId.Value);
            }
        }

        [Test]
        public void TheSetIsTheShardsNineteenFronts_AllComposed_UnderHisDealtRevision()
        {
            Assert.That(GydionCardContent.InventoryOrder.Count, Is.EqualTo(19));
            Assert.That(GydionCardContent.NotYetComposed, Is.Empty, "9.30g and 9.30i composed every front.");
            Assert.That(GydionCardContent.ActionCardDefinitions.Select(definition => definition.Id),
                Is.EqualTo(GydionCardContent.InventoryOrder), "every front, in the shard's order.");
            Assert.That(GydionCardContent.AllDefinitions.Select(definition => definition.Id),
                Is.EqualTo(GydionCardContent.InventoryOrder.Concat(GydionCardContent.SpellIds)),
                "the nineteen fronts, then the twenty-two spells");
            Assert.That(GydionCardContent.AllDefinitions.Select(definition => definition.ContentRevision),
                Is.All.EqualTo(Revision));
            Assert.That(GydionCardContent.StartingHand, Is.EqualTo(new[]
            {
                GydionCardContent.Cantrip, GydionCardContent.PrepareSpells, GydionCardContent.ElementaryEvocation,
                GydionCardContent.ElementaryEnchantment, GydionCardContent.ElementaryAbjuration
            }), "p.6: Gold, Silver and each colour's Tier I.");
            Assert.That(GydionCardContent.ReturnedToBoxAtSetup, Is.EqualTo(new[] { GydionCardContent.CheapTrick }));
            Assert.That(GydionCardContent.Deck.Count, Is.EqualTo(13), "nineteen less five in hand less the Handicap.");
            Assert.That(GydionCardContent.Definition(GydionCardContent.TheArchwizard),
                Is.SameAs(GydionCardContent.TheArchwizardDefinition));
            Assert.That(() => GydionCardContent.Definition(new CardDefinitionId("gydion-nothing")),
                Throws.ArgumentException);
        }

        /// <summary>
        /// The shard's inventory table, row by row: Initiative, the ribbon's Defense and Movement, the
        /// item icon, Tier and colour, and no Attack, Range or Radius on any front.
        /// </summary>
        [TestCase("cantrip", "Cantrip", CardColor.Gold, CardTier.None, 11, 1, 0, PrintedValueChannel.None)]
        [TestCase("prepare-spells", "Prepare Spells", CardColor.Silver, CardTier.None, 13, 2, 0, PrintedValueChannel.None)]
        [TestCase("elementary-evocation", "Elementary Evocation", CardColor.Red, CardTier.I, 7, 6, 3, PrintedValueChannel.None)]
        [TestCase("lesser-evocation", "Lesser Evocation", CardColor.Red, CardTier.II, 8, 6, 3, PrintedValueChannel.Defense)]
        [TestCase("lesser-necromancy", "Lesser Necromancy", CardColor.Red, CardTier.II, 8, 6, 3, PrintedValueChannel.Initiative)]
        [TestCase("greater-evocation", "Greater Evocation", CardColor.Red, CardTier.III, 8, 7, 3, PrintedValueChannel.Defense)]
        [TestCase("greater-necromancy", "Greater Necromancy", CardColor.Red, CardTier.III, 8, 7, 3, PrintedValueChannel.Radius)]
        [TestCase("elementary-enchantment", "Elementary Enchantment", CardColor.Blue, CardTier.I, 9, 3, 2, PrintedValueChannel.None)]
        [TestCase("lesser-enchantment", "Lesser Enchantment", CardColor.Blue, CardTier.II, 10, 4, 2, PrintedValueChannel.Defense)]
        [TestCase("lesser-transmutation", "Lesser Transmutation", CardColor.Blue, CardTier.II, 10, 4, 2, PrintedValueChannel.Attack)]
        [TestCase("greater-enchantment", "Greater Enchantment", CardColor.Blue, CardTier.III, 10, 4, 2, PrintedValueChannel.Range)]
        [TestCase("greater-transmutation", "Greater Transmutation", CardColor.Blue, CardTier.III, 10, 4, 2, PrintedValueChannel.Attack)]
        [TestCase("elementary-abjuration", "Elementary Abjuration", CardColor.Green, CardTier.I, 5, 2, 2, PrintedValueChannel.None)]
        [TestCase("lesser-abjuration", "Lesser Abjuration", CardColor.Green, CardTier.II, 4, 3, 2, PrintedValueChannel.Initiative)]
        [TestCase("lesser-conjuration", "Lesser Conjuration", CardColor.Green, CardTier.II, 4, 3, 2, PrintedValueChannel.Attack)]
        [TestCase("greater-abjuration", "Greater Abjuration", CardColor.Green, CardTier.III, 4, 3, 2, PrintedValueChannel.Initiative)]
        [TestCase("greater-conjuration", "Greater Conjuration", CardColor.Green, CardTier.III, 4, 3, 2, PrintedValueChannel.Movement)]
        [TestCase("cheap-trick", "Cheap Trick", CardColor.Gold, CardTier.None, 10, 1, 0, PrintedValueChannel.None)]
        public void EveryFront_PrintsItsInventoryRow(
            string card, string name, CardColor color, CardTier tier, int initiative, int defense, int movement,
            PrintedValueChannel item)
        {
            var face = GydionCardContent.Definition(new CardDefinitionId("gydion-" + card));
            Assert.That(face.DisplayName, Is.EqualTo(name));
            Assert.That(face.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));
            Assert.That(face.Progression.Color, Is.EqualTo(color));
            Assert.That(face.Progression.Tier, Is.EqualTo(tier));
            Assert.That(face.Progression.Classification,
                Is.EqualTo(tier == CardTier.None ? CardClassification.Basic : CardClassification.NonBasic));
            Assert.That(face.Progression.ItemIcon, Is.EqualTo(item));
            Assert.That(face.Initiative.Value, Is.EqualTo(initiative));
            var primary = face.ActionDefinitions.Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill), "every front's primary is a Skill (GY-Q3)");
            Assert.That(primary.BaseValues, Is.Empty, "no front prints an Attack, Range or Radius: they are on the spell");
            Assert.That(Printed(face, ActionKind.Defense, PrintedValueChannel.Defense), Is.EqualTo(defense));
            Assert.That(Printed(face, ActionKind.Movement, PrintedValueChannel.Movement), Is.EqualTo(movement));
            Assert.That(face.SourceContent?.PrintedText, Is.Not.Empty);
        }

        /// <summary>The upgrade lines (shard, "name family", validated against p.15 here).</summary>
        [Test]
        public void TheUpgradeLines_FollowEachSchoolsNameFamily()
        {
            void Upgrades(CardDefinitionId face, params CardDefinitionId[] next) =>
                Assert.That(GydionCardContent.Definition(face).Progression.NextTierCandidateIds,
                    Is.EquivalentTo(next), face.Value);
            Upgrades(GydionCardContent.ElementaryEvocation, GydionCardContent.LesserEvocation, GydionCardContent.LesserNecromancy);
            Upgrades(GydionCardContent.LesserEvocation, GydionCardContent.GreaterEvocation, GydionCardContent.GreaterNecromancy);
            Upgrades(GydionCardContent.LesserNecromancy, GydionCardContent.GreaterEvocation, GydionCardContent.GreaterNecromancy);
            Upgrades(GydionCardContent.ElementaryEnchantment, GydionCardContent.LesserEnchantment, GydionCardContent.LesserTransmutation);
            Upgrades(GydionCardContent.LesserEnchantment, GydionCardContent.GreaterEnchantment, GydionCardContent.GreaterTransmutation);
            Upgrades(GydionCardContent.LesserTransmutation, GydionCardContent.GreaterEnchantment, GydionCardContent.GreaterTransmutation);
            Upgrades(GydionCardContent.ElementaryAbjuration, GydionCardContent.LesserAbjuration, GydionCardContent.LesserConjuration);
            Upgrades(GydionCardContent.LesserAbjuration, GydionCardContent.GreaterAbjuration, GydionCardContent.GreaterConjuration);
            Upgrades(GydionCardContent.LesserConjuration, GydionCardContent.GreaterAbjuration, GydionCardContent.GreaterConjuration);
            foreach (var greater in new[]
            {
                GydionCardContent.GreaterEvocation, GydionCardContent.GreaterNecromancy,
                GydionCardContent.GreaterEnchantment, GydionCardContent.GreaterTransmutation,
                GydionCardContent.GreaterAbjuration, GydionCardContent.GreaterConjuration
            })
            {
                Upgrades(greater);
            }
        }

        /// <summary>
        /// "Choose and cast one spell in the spellbook - X, Y[, Z]" is a mandatory Choose over one cast
        /// arm per listed spell in printed order; "Cast the X spell" is one cast arm as the root. Each
        /// arm reads, off the public graph, as the cast of the spell the front names.
        /// </summary>
        [TestCase("cantrip", "magic-missile", "expeditious-retreat", "shocking-grasp")]
        [TestCase("elementary-evocation", "burning-hands")]
        [TestCase("lesser-evocation", "burning-hands", "fireball")]
        [TestCase("lesser-necromancy", "vampiric-touch", "create-undead")]
        [TestCase("greater-evocation", "burning-hands", "fireball", "sunburst")]
        [TestCase("greater-necromancy", "vampiric-touch", "create-undead", "energy-drain")]
        [TestCase("elementary-enchantment", "suggestion")]
        [TestCase("lesser-enchantment", "suggestion", "dominate-person")]
        [TestCase("lesser-transmutation", "midas-touch", "disintegrate")]
        [TestCase("greater-enchantment", "suggestion", "dominate-person", "power-word-kill")]
        [TestCase("greater-transmutation", "midas-touch", "disintegrate", "polymorph")]
        [TestCase("elementary-abjuration", "shield")]
        [TestCase("lesser-abjuration", "shield", "banishment")]
        [TestCase("lesser-conjuration", "find-familiar", "dimension-door")]
        [TestCase("greater-abjuration", "shield", "banishment", "invulnerability")]
        [TestCase("greater-conjuration", "find-familiar", "dimension-door", "cloud-kill")]
        [TestCase("cheap-trick", "expeditious-retreat")]
        public void EveryCastingFront_CastsTheSpellsItLists_InPrintedOrder(string card, params string[] spells)
        {
            var face = GydionCardContent.Definition(new CardDefinitionId("gydion-" + card));
            var text = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill).EffectRoot;
            Assert.That(text, Is.Not.Null);
            var root = text.Nodes.Single(node => node.Id.Equals(text.RootNodeId));
            var arms = spells.Length == 1
                ? new[] { root.Id }
                : root.ChildNodeIds.ToArray();
            if (spells.Length > 1)
            {
                Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Choose), "\"Choose and cast one\"");
                Assert.That(root.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory));
                Assert.That(root.Chooser, Is.EqualTo(EffectChooserRole.Actor));
                Assert.That(root.MinimumSelections, Is.EqualTo(1));
                Assert.That(root.MaximumSelections, Is.EqualTo(1));
            }

            var cast = arms.Select(arm =>
            {
                Assert.That(SyntheticSpellbookCapabilities.TryReadSpellArm(
                    new[] { text }, arm, GydionCardContent.SpellDefinitions, out var spell, out var kind), Is.True, arm.Value);
                Assert.That(kind, Is.EqualTo(SyntheticSpellbookCapabilities.SpellArmKind.Cast), arm.Value);
                return spell.Id.Value;
            }).ToArray();
            Assert.That(cast, Is.EqualTo(spells.Select(spell => "gydion-" + spell)),
                "the spells the front lists, in printed order");
        }

        [Test]
        public void TheTrustedCatalog_RegistersEveryHandlerHisFrontsAndSpellsName_AndTheFamiliar()
        {
            var staged = MatchConfiguration.CreateStagedGydionForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Count(card =>
                card.Id.Value.StartsWith("gydion-", StringComparison.Ordinal)), Is.EqualTo(40),
                "eighteen dealt fronts and twenty-two spells");

            var catalogs = MatchContentCatalogs.ForThisBuild();
            var named = GydionCardContent.DealtDefinitions.Concat(GydionCardContent.SpellDefinitions)
                .SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(root => root != null)
                .SelectMany(root => root.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();
            Assert.That(named.Length, Is.GreaterThan(40));
            foreach (var capability in named)
            {
                Assert.That(catalogs.Capabilities.Registered, Has.Member(capability), capability.ToString());
            }

            Assert.That(catalogs.Components.RegisteredTokens, Has.Member(GydionComponentContent.Familiar.Reference),
                "GY-Q5: the Familiar is a registered token with a supply of one.");
            Assert.That(catalogs.Capabilities.Registered, Has.Member(SyntheticPlacementCapabilities.PlaceAtMaximumRadiusV1));
            Assert.That(catalogs.Capabilities.Registered,
                Has.Member(SyntheticPlacementCapabilities.RespawnFriendlyMinionInRangeV1));
        }

        [Test]
        public void TheTierThreeStaging_UnlocksTheArchwizard_WithWishInTheBook()
        {
            var staged = MatchConfiguration.CreateStagedGydionTierThreeForInternalTest(TeamSide.Red);
            var ultimate = staged.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(GydionCardContent.TheArchwizard));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(staged.CardInstances.Where(card =>
                    card.OwnerId.Equals(ultimate.OwnerId) && card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(new[]
            {
                GydionCardContent.Cantrip, GydionCardContent.PrepareSpells, GydionCardContent.GreaterEvocation,
                GydionCardContent.GreaterTransmutation, GydionCardContent.GreaterConjuration
            }));
            Assert.That(staged.CardInstances.Single(card =>
                    card.OwnerId.Equals(ultimate.OwnerId) &&
                    card.DefinitionReference.DefinitionId.Equals(GydionCardContent.Wish)).State.Location,
                Is.EqualTo(CardLocation.Spellbook));
        }

        /// <summary>8.46a: the controls his fronts and spells raise are exactly the ones he declares words for.</summary>
        [Test]
        public void EveryControlHisFacesRaise_HasItsWords_AndNoOtherIsDeclared()
        {
            var faces = GydionCardContent.DealtDefinitions.Concat(GydionCardContent.SpellDefinitions).ToArray();
            var required = DecisionLabelRequirementQuery
                .For(faces, MatchContentCatalogs.ForThisBuild())
                .Select(requirement => requirement.CardId.Value + " / " + requirement.NodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var declared = GydionCardContent.DecisionLabels()
                .Select(declaration => declaration.CardId.Value + " / " + declaration.NodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Assert.That(declared, Is.EqualTo(required));
            Assert.That(required, Has.Length.EqualTo(78),
                "33 cast bullets on twelve fronts, Vampiric Touch's retrieval, Find Familiar's 21 removals, " +
                "Wish's 21 casts and Prepare Spells' two arms");
            foreach (var declaration in GydionCardContent.DecisionLabels())
            {
                Assert.That(DecisionLabelCatalog.TryGetLabel(declaration.CardId, declaration.NodeId,
                    out _), Is.True, declaration.CardId.Value + " / " + declaration.NodeId.Value);
            }

            Assert.That(GydionCardContent.CastingCardDecisionLabels()
                    .Where(label => label.CardId.Equals(GydionCardContent.LesserTransmutation))
                    .Select(label => label.Label.Chosen),
                Is.EqualTo(new[] { "Cast Midas' Touch", "Cast Disintegrate" }),
                "the front prints \"Midas Touch\"; the bullet says the spell's own name");
        }

        /// <summary>
        /// RULING 53's "If" pattern (OfferOutcomeTests.OpensWithAnIf) reads the PLAYED card, and none of
        /// his fronts opens a sentence with "If": Suggestion's "If able" is on the spell.
        /// </summary>
        [Test]
        public void TheIfPattern_MatchesNoneOfHisFronts()
        {
            Assert.That(GydionCardContent.ActionCardDefinitions.Where(OfferOutcomeTests.OpensWithAnIf), Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // A dealt match: a cast and a refill from his hand
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// On the staged board his seat plays Elementary Abjuration from his p.6 hand: Shield leaves the
        /// book faceup, every seat counts twenty-one, and his Prepare Spells next turn returns it.
        /// </summary>
        [Test]
        public void ADealtFront_CastsFromHisBook_AndPrepareSpellsReturnsTheSpell()
        {
            var session = StagedSession(tierThree: false, out var wasp, out var gydion);
            ActWith(session, wasp, gydion, GydionCardContent.ElementaryAbjuration);
            Accept(session, StartPrimary(session, GydionCardContent.ElementaryAbjuration));
            var shield = SpellCard(session, gydion, GydionCardContent.Shield);
            Assert.That(shield.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(shield.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
            var host = new MatchHost(session);
            foreach (var seat in host.Seats.Seats)
            {
                Assert.That(host.ProjectionForSeat(seat.Id).HandSummaries
                    .Single(value => value.HeroId.Equals(gydion)).SpellbookCount, Is.EqualTo(21), seat.HeroId.Value);
            }

            FinishTheTurn(session);
            ActWith(session, wasp, gydion, GydionCardContent.PrepareSpells);
            Accept(session, StartPrimary(session, GydionCardContent.PrepareSpells));
            Assert.That(SpellCard(session, gydion, GydionCardContent.Shield).State.Location,
                Is.EqualTo(CardLocation.Spellbook), "\"Add all spell cards to the spellbook.\"");
        }

        /// <summary>
        /// Milestone 9.34 corrective, the user's report: "Gydion when he attacks animation shows
        /// syntetic card for spell attack, it needs to be actual spell card". The defending seat's
        /// window draws the incoming attack's card (8.80), and a spell attack's card is the spell, so
        /// that context card carries the spell's own face - the one every spell surface draws - rather
        /// than a generated hero card. Cast from Cantrip: whichever of its two Attack spells can reach
        /// Wasp on the staged board.
        /// </summary>
        [Test]
        public void ASpellAttacksDefenseWindow_ShowsTheSpellsOwnFace()
        {
            var reached = 0;
            foreach (var spell in new[] { GydionCardContent.ShockingGrasp, GydionCardContent.MagicMissile })
            {
                var session = StagedSession(tierThree: false, out var wasp, out var gydion);
                ActWith(session, wasp, gydion, GydionCardContent.Cantrip);
                Accept(session, StartPrimary(session, GydionCardContent.Cantrip));
                var arm = session.Projection.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Option)
                    .Select(candidate => candidate.OptionNodeId)
                    .Single(node => SyntheticSpellbookCapabilities.TryReadSpellArm(
                            GydionCardContent.Definition(GydionCardContent.Cantrip).ActionDefinitions
                                .Select(action => action.EffectRoot).Where(root => root != null),
                            node, GydionCardContent.SpellDefinitions, out var named, out _) &&
                        named.Id.Equals(spell));
                var asked = session.Projection.PendingEffectChoice.ChoiceId;
                Accept(session, SelectArm(session, arm));
                ConfiguredMatchFixtures.ConfirmStandingEffectChoiceIfOwed(session, asked);
                var targets = session.Projection.PendingAttackTargetChoice;
                if (targets == null || !targets.LegalTargetHeroIds.Contains(wasp))
                {
                    continue;
                }

                Accept(session, DirectorCommand.ChooseAttackTarget(wasp));
                var defense = session.Projection.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null, spell.Value + ": Wasp is asked to defend.");
                Assert.That(defense.SourceDefinitionReference.DefinitionId, Is.EqualTo(spell),
                    "The attack's card is the spell it was cast from (9.30c).");
                Assert.That(DefinitionOf(session, session.Projection.Cards.Single(card =>
                        card.Id.Equals(defense.SourceCardInstanceId))).Progression.IsSpell, Is.True,
                    "and that card is a spell - the window's attacking card is a spell card.");

#if UNITY_EDITOR
                // The window's presenter is Unity-tier (the offline harness compiles no Presentation
                // window), so this half runs in Unity Edit Mode.
                var model = GuardsDigital.Presentation.Match.DecisionWindowPresenter.CreateViewModel(
                    session.Projection, defense.ChooserId);
                Assert.That(model?.Family,
                    Is.EqualTo(GuardsDigital.Presentation.Match.DecisionWindowFamily.Defense));
                Assert.That(model.ContextCards, Has.Count.EqualTo(1), "The attacking card is shown.");
                Assert.That(model.ContextCards[0].Spell?.DefinitionReference.DefinitionId,
                    Is.EqualTo(spell),
                    spell.Value + ": the defender sees the spell's own face, not a generated hero card.");
#endif
                reached++;
            }

            Assert.That(reached, Is.GreaterThan(0),
                "Fixture: one of Cantrip's Attack spells reaches Wasp on the staged board.");
        }

        // -------------------------------------------------------------------------------------------
        // The bots answer every decision family his spells raise
        // -------------------------------------------------------------------------------------------

        [TestCase(1)]
        [TestCase(2)]
        public void TheBots_AnswerTheSpellChoice_AndWhateverTheCastSpellAsks(int seed)
        {
            var session = StagedSession(tierThree: false, out var wasp, out var gydion);
            ActWith(session, wasp, gydion, GydionCardContent.Cantrip);
            Accept(session, StartPrimary(session, GydionCardContent.Cantrip));
            var answered = BotsAnswerUntilTheActionEnds(session, seed);
            Assert.That(answered, Has.Some.EqualTo((GydionCardContent.CastChoice(GydionCardContent.Cantrip).Value, gydion)),
                "his seat's bot chose a spell");
            Assert.That(session.Projection.Cards.Count(card =>
                    card.OwnerId.Equals(gydion) && card.State.Location == CardLocation.RemovedFromSpellbook),
                Is.EqualTo(1), "and the spell it chose was cast");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void TheBots_AnswerSuggestion_TheMovedHerosOwnSeatPickingTheDirection(int seed)
        {
            var session = StagedSession(tierThree: false, out var wasp, out var gydion);
            var waspBefore = session.Projection.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate;
            ActWith(session, wasp, gydion, GydionCardContent.ElementaryEnchantment);
            Accept(session, StartPrimary(session, GydionCardContent.ElementaryEnchantment));
            var answered = BotsAnswerUntilTheActionEnds(session, seed);
            Assert.That(answered, Has.Some.EqualTo(("suggestion-direction", wasp)),
                "GY-Q6: the moved hero's seat picks the direction, and its bot answers");
            Assert.That(answered.Where(entry => entry.Node == "suggestion-direction").Select(entry => entry.Seat),
                Is.All.EqualTo(wasp));
            Assert.That(HexDistanceQuery.Calculate(waspBefore,
                    session.Projection.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate),
                Is.EqualTo(3), "exactly three spaces in a straight line");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void TheBots_AnswerTheArchwizardsRefillOrWish(int seed)
        {
            var session = StagedSession(tierThree: true, out var wasp, out var gydion);
            ActWith(session, wasp, gydion, GydionCardContent.PrepareSpells);
            Accept(session, StartPrimary(session, GydionCardContent.PrepareSpells));
            var answered = BotsAnswerUntilTheActionEnds(session, seed);
            Assert.That(answered, Has.Some.EqualTo((GydionCardContent.PrepareSpellsReplacementChoice.Value, gydion)),
                "his seat's bot chose between the refill and casting Wish (GY-Q7: a full book still would add)");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void TheBots_AnswerFindFamiliarsUpToThreeRemovals(int seed)
        {
            var session = StagedSession(tierThree: true, out var wasp, out var gydion);
            ActWith(session, wasp, gydion, GydionCardContent.GreaterConjuration);
            Accept(session, StartPrimary(session, GydionCardContent.GreaterConjuration));
            Accept(session, SelectArm(session, GydionCardContent.CastBullet(
                GydionCardContent.GreaterConjuration, GydionCardContent.FindFamiliar)));
            var answered = BotsAnswerUntilTheActionEnds(session, seed);
            Assert.That(answered, Has.Some.EqualTo((GydionCardContent.FindFamiliarRemovalChoice.Value, gydion)),
                "his seat's bot answered the up-to-three removal");
            var removed = session.Projection.Cards.Count(card =>
                card.OwnerId.Equals(gydion) && card.State.Location == CardLocation.RemovedFromSpellbook);
            Assert.That(removed, Is.InRange(1, 4), "Find Familiar itself, and up to three others");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void TheBots_AnswerCloudKillsBasicDiscard_OnTheDiscardingHerosSeat(int seed)
        {
            var session = StagedSession(tierThree: true, out var wasp, out var gydion);
            ActWith(session, wasp, gydion, GydionCardContent.GreaterConjuration);
            Accept(session, StartPrimary(session, GydionCardContent.GreaterConjuration));
            var basicsBefore = BasicsInHand(session, wasp);
            Assert.That(basicsBefore, Is.GreaterThan(0), "Fixture precondition: Wasp holds a basic card.");
            Accept(session, SelectArm(session, GydionCardContent.CastBullet(
                GydionCardContent.GreaterConjuration, GydionCardContent.CloudKill)));
            var answered = BotsAnswerUntilTheActionEnds(session, seed);
            Assert.That(answered.Where(entry => entry.Node.StartsWith("cloud-kill", StringComparison.Ordinal)),
                Is.Not.Empty, "a Cloud Kill decision was asked and answered");
            Assert.That(BasicsInHand(session, wasp), Is.EqualTo(basicsBefore - 1),
                "the enemy hero discarded a basic card (9.30f's classified discard)");
        }

        #if UNITY_EDITOR
        [Test]
        public void PreparedPrivateArt_LoadsForTheStandeesTheSelectionFrontEveryDealtFaceAndEverySpell()
        {
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/gydion-red"), Is.Not.Null,
                "run Tools/Prepare-HeroTokens.ps1 -Hero gydion in this checkout");
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/gydion-blue"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/HeroSelection/hero-gydion"), Is.Not.Null);
            foreach (var definition in GydionCardContent.DealtDefinitions)
            {
                Assert.That(ConfiguredCardArtworkCatalog.Catalog.TryGet(definition.Reference, out _),
                    Is.True, definition.DisplayName);
            }
        }
        #endif

        // -------------------------------------------------------------------------------------------
        // The staged board
        // -------------------------------------------------------------------------------------------

        /// <summary>Wasp against Gydion on the engaged staged board his scenes load.</summary>
        internal static MatchSession StagedSession(bool tierThree, out HeroId wasp, out HeroId gydion)
        {
            var configuration = tierThree
                ? MatchConfiguration.CreateStagedGydionTierThreeForInternalTest(TeamSide.Red)
                : MatchConfiguration.CreateStagedGydionForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusGydionOneVsOne(),
                    configuration.Participants, map, configuration.TieBreakerState),
                out _);
            var gydionSetup = setup.Heroes.Single(hero => hero.ContentId.Value == GydionCardContent.ContentSetId);
            gydion = gydionSetup.HeroId;
            var gydionId = gydion;
            wasp = setup.Heroes.Single(hero => !hero.HeroId.Equals(gydionId)).HeroId;
            return new MatchSession(new MatchSessionInitialState(
                Scenario(configuration, map, setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))),
                setup, new[] { map }));
        }

        /// <summary>
        /// Both commit - Wasp her lowest-Initiative card, Gydion <paramref name="card"/> - and Wasp
        /// Holds whenever she acts first, until Gydion's turn with that card is the one being taken.
        /// </summary>
        private static void ActWith(MatchSession session, HeroId wasp, HeroId gydion, CardDefinitionId card)
        {
            var gydionCard = InHand(session, gydion).Single(value => value.DefinitionReference.DefinitionId.Equals(card));
            var waspCard = InHand(session, wasp)
                .OrderBy(value => DefinitionOf(session, value).Initiative.Value)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                .First();
            Accept(session, DirectorCommand.CommitCard(wasp, waspCard.Id));
            Accept(session, DirectorCommand.CommitCard(gydion, gydionCard.Id));
            for (var step = 0; step < 8 && !(session.Projection.IsTurnActive &&
                session.Projection.SelectedActor.ActorId.Equals(gydion)); step++)
            {
                var projection = session.Projection;
                if (projection.PendingInitiativeChoice != null)
                {
                    var tie = projection.PendingInitiativeChoice;
                    var first = tie.CandidateCardInstanceIds.Contains(gydionCard.Id)
                        ? gydionCard.Id
                        : tie.CandidateCardInstanceIds.First();
                    Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide, first));
                }
                else if (projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                }
                else if (projection.IsTurnActive)
                {
                    Accept(session, DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
                }
                else
                {
                    Assert.Fail("Fixture: the round reached no turn for Gydion: " + projection.Phase);
                }
            }

            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(gydion));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(gydionCard.Id));
        }

        /// <summary>Releases the turn and lets the round run to its next commitment.</summary>
        private static void FinishTheTurn(MatchSession session)
        {
            for (var step = 0; step < 16; step++)
            {
                var projection = session.Projection;
                if (projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                }
                else if (projection.IsTurnActive)
                {
                    Accept(session, DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
                }
                else
                {
                    var advance = DeterministicMatchLoop.EnumerateLegalCommands(projection)
                        .FirstOrDefault(command => command.Kind == DirectorCommandKind.AdvanceEndOfRound);
                    if (advance == null) return;
                    Accept(session, advance);
                }
            }
        }

        private static DirectorCommand StartPrimary(MatchSession session, CardDefinitionId card)
        {
            var offer = session.Projection.AvailableTurnActions.Single(value =>
                value.SourceDefinitionReference.DefinitionId.Equals(card) &&
                value.ActionDefinitionId.Equals(GydionCardContent.PrimarySkill));
            Assert.That(offer.IsAvailable, Is.True, card.Value + " is offered");
            return DirectorCommand.StartAction(offer.SourceCardInstanceId, offer.ActionDefinitionId);
        }

        private static DirectorCommand SelectArm(MatchSession session, EffectNodeId arm) =>
            session.Projection.DecisionContract.Commands.Single(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.OptionNodeId.Equals(arm));

        /// <summary>
        /// Every decision the action raises, answered by the bot of the seat that owns it from that
        /// seat's own projection (the lobby policy), until no decision stands. Returns what was asked
        /// and whose seat answered.
        /// </summary>
        private static List<(string Node, HeroId Seat)> BotsAnswerUntilTheActionEnds(MatchSession session, int seed)
        {
            var host = new MatchHost(session);
            var random = new DeterministicBotRandom(seed);
            var answered = new List<(string Node, HeroId Seat)>();
            for (var step = 0; step < 80; step++)
            {
                var rules = session.Projection;
                if (rules.DecisionContract == null)
                {
                    return answered;
                }

                var seat = host.Seats.Seats.First(value => rules.DecisionOwner.IsOwnedBy(value));
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(view.AuthorizedCommands, Is.Not.Empty,
                    $"{seat.HeroId.Value} owns the standing decision ({rules.DecisionOwner}) and is offered no answer");
                var pick = BotPolicy.SelectPrivateSessionTactics(view.AuthorizedCommands, view, seat.HeroId, random);
                Assert.That(pick, Is.Not.Null);
                answered.Add((rules.PendingEffectChoice?.NodeId.Value ?? rules.DecisionOwner.FamilyId.ToString(),
                    seat.HeroId));
                var result = session.Execute(pick);
                Assert.That(result.Accepted, Is.True, $"{seat.HeroId.Value}'s bot answer {pick}: {result.Detail}");
            }

            Assert.Fail("The bots did not finish the action in 80 answers: " +
                string.Join(", ", answered.Select(entry => entry.Node)));
            return answered;
        }

        private static IEnumerable<CardInstance> InHand(MatchSession session, HeroId hero) =>
            session.Projection.Cards.Where(card => card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);

        private static int BasicsInHand(MatchSession session, HeroId hero) =>
            InHand(session, hero).Count(card =>
                DefinitionOf(session, card).Progression.Classification == CardClassification.Basic);

        private static CardInstance SpellCard(MatchSession session, HeroId owner, CardDefinitionId spell) =>
            session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(owner) && card.DefinitionReference.DefinitionId.Equals(spell));

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(definition => definition.Reference.Equals(card.DefinitionReference));

        private static int Printed(CardDefinition face, ActionKind kind, PrintedValueChannel channel) =>
            face.ActionDefinitions.Where(action => action.Kind == kind)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount)
                .SingleOrDefault();

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, $"{command}: {result.Detail}");
        }

        private static InitialScenarioDescription Scenario(
            MatchConfiguration configuration, BoardMapDefinition map, IEnumerable<HeroOccupancy> heroes) =>
            new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                heroes,
                Array.Empty<ObstacleOccupancy>());
    }
}
