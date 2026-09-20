using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.38d. Swift the Sharpshooter's nineteen printed faces as composed content: the values
    /// the inventory recorded, the declarations her text makes, and the effect bodies it composes to.
    ///
    /// <para><b>This is the cheap tier, and it runs before her player-path gate for the reason
    /// [POST_PITCH.md] records twice over:</b> the focused tier finds engine defects because it can
    /// build the board a printed clause needs, and the expensive tier proves the player path over
    /// content that already works. Milestone 8.22's forty-seven-case tier found and fixed all four of
    /// its engine defects before a pointer met the content.</para>
    /// </summary>
    [TestFixture]
    public sealed class SwiftPrintedSetTests
    {
        // -------------------------------------------------------------------------------------------
        // The set itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Nineteen faces, eighteen dealt, one boxed - and the colour shape p.5 gives every hero: one
        /// Gold basic, one Silver basic, five of each of Red, Blue and Green, one Purple Ultimate and
        /// one Gold Handicap.
        /// </summary>
        [Test]
        public void HerSet_IsNineteenFacesInThePrintedColourShape()
        {
            Assert.That(SwiftCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(SwiftCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                SwiftCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { SwiftCardContent.Backup }));

            var byColour = SwiftCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2), "Reload! and the Handicap Backup.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1));
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5));

            // The face prints p.9's sixth colour and the PROGRESSION carries None, which is the
            // engine's deliberate reading rather than a gap: CardColor.Purple's own remarks say it is
            // "printed only on an Ultimate card and never on one in a hand ... inert everywhere a
            // hand-colour is read". Every verified hero's Ultimate is modelled the same way, so
            // asserting Purple here would have made Swift the one hero out of step.
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "Bullet Time.");
            Assert.That(
                SwiftCardContent.Definition(SwiftCardContent.BulletTime).Progression.Classification,
                Is.EqualTo(CardClassification.Ultimate));
        }

        /// <summary>
        /// p.6's five starting cards, and the thirteen the deck keeps. The two sets partition her dealt
        /// eighteen exactly, so no card is both and none is neither.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHerDealtEighteen()
        {
            Assert.That(SwiftCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(SwiftCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(SwiftCardContent.StartingHand.Intersect(SwiftCardContent.Deck), Is.Empty);
            Assert.That(
                SwiftCardContent.StartingHand.Concat(SwiftCardContent.Deck),
                Is.EquivalentTo(SwiftCardContent.DealtDefinitions.Select(card => card.Id)));
        }

        /// <summary>
        /// The one single-secondary face, and milestone 8.35z20's slot-position trap: Bounce's only
        /// icon is a Defense, drawn where sixteen of her faces put a boot.
        /// </summary>
        [Test]
        public void Bounce_PrintsADefenseSecondaryAndNoMovementSecondary()
        {
            var bounce = SwiftCardContent.Definition(SwiftCardContent.Bounce);

            Assert.That(
                bounce.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense), Is.True,
                "Its single glyph is a riveted shield.");
            Assert.That(
                bounce.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement), Is.False,
                "and it prints no boot at all, which is why no Movement action exists on it.");
            Assert.That(
                SwiftCardContent.AllDefinitions.Count(definition =>
                    definition.ActionDefinitions.All(action => action.Kind != ActionKind.Movement)),
                Is.EqualTo(2),
                "Bounce, whose single glyph is a shield, and the Ultimate, which prints no ribbon at " +
                "all. Every other face of hers prints a boot.");
        }

        /// <summary>
        /// She prints no Movement primary and no Defense primary: every printed primary in her set is a
        /// Basic Skill, a Ranged Attack, a Skill or nothing at all.
        /// </summary>
        [Test]
        public void NoFaceOfHers_PrintsAMovementOrDefensePrimary()
        {
            var primaries = SwiftCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .Select(action => action.Kind)
                .Distinct()
                .ToArray();

            Assert.That(primaries, Is.EquivalentTo(new[] { ActionKind.Skill, ActionKind.Attack }));
        }

        // -------------------------------------------------------------------------------------------
        // The Red line's declarations
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Snipe's whole printed text is its targeting, so it declares both of milestone 8.38a's
        /// narrowings and composes no effect at all.
        /// </summary>
        [Test]
        public void Snipe_DeclaresTheExactBandOnTheFileAndComposesNoEffect()
        {
            var attack = Primary(SwiftCardContent.Snipe);

            Assert.That(attack.AttackTargetPolicy, Is.Not.Null);
            Assert.That(
                attack.AttackTargetPolicy.Value.Reach,
                Is.EqualTo(AttackTargetReach.ExactlyPrintedRange),
                "\"at maximum range\" is RULING 1's band.");
            Assert.That(
                attack.AttackTargetPolicy.Value.Line, Is.EqualTo(AttackTargetLine.InStraightLine));
            Assert.That(attack.EffectRoot, Is.Null,
                "A targeting sentence is a declaration, not an effect.");
            Assert.That(attack.TextComposition.IsAwaited, Is.False);
        }

        /// <summary>
        /// Prepared Shot and Killshot print one sentence and differ only in their Attack and Range, so
        /// both declare the same pair and neither composes anything.
        /// </summary>
        [Test]
        public void PreparedShotAndKillshot_DeclareTheFileAndTheAdjacentExclusion()
        {
            foreach (var cardId in new[] { SwiftCardContent.PreparedShot, SwiftCardContent.Killshot })
            {
                var attack = Primary(cardId);

                Assert.That(
                    attack.AttackTargetPolicy.Value.Reach,
                    Is.EqualTo(AttackTargetReach.PrintedRangeBeyondAdjacent), cardId.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.Line,
                    Is.EqualTo(AttackTargetLine.InStraightLine), cardId.Value);
                Assert.That(attack.EffectRoot, Is.Null, cardId.Value);
            }
        }

        /// <summary>
        /// The two scatter Attacks narrow nothing - "a unit in range" is p.13's own sentence - and
        /// apply their rider at p.14's step 1, which is what "Before the attack," names.
        /// </summary>
        [Test]
        public void TheTwoShotguns_NarrowNothingAndApplyTheirRiderBeforeTheAttack()
        {
            foreach (var cardId in new[] { SwiftCardContent.Shotgun, SwiftCardContent.SuperShotgun })
            {
                var attack = Primary(cardId);

                // A Ranged Attack always carries a policy - ForClassification is p.13's own default -
                // so "narrows nothing" is read off NarrowsTheDefault rather than off a null. These two
                // print "a unit in range", which IS that default word for word.
                Assert.That(
                    attack.AttackTargetPolicy.Value.NarrowsTheDefault(AttackRangeKind.Ranged),
                    Is.False, cardId.Value);
                Assert.That(attack.EffectRoot, Is.Not.Null, cardId.Value);
                Assert.That(
                    attack.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack), cardId.Value);
            }
        }

        /// <summary>
        /// Super-Shotgun is Shotgun's sentence with ledger question 13's escalation: it reaches a
        /// Defeat where the discard could not complete, and Shotgun does not.
        /// </summary>
        [Test]
        public void OnlySuperShotgun_EscalatesAnImpossibleDiscardToADefeat()
        {
            Assert.That(
                Capabilities(Primary(SwiftCardContent.Shotgun).EffectRoot),
                Has.No.Member(SyntheticConsequenceCapabilities.DefeatHeroV1),
                "Shotgun prints \"if able\" and stops there.");
            Assert.That(
                Capabilities(Primary(SwiftCardContent.SuperShotgun).EffectRoot),
                Does.Contain(SyntheticConsequenceCapabilities.DefeatHeroV1),
                "and Super-Shotgun prints \"or is defeated\".");
        }

        // -------------------------------------------------------------------------------------------
        // The Blue line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her Blue line splits three and two on milestone 8.38b's seam, and this is the case that
        /// keeps the two placement declarations attached to the right faces.
        /// </summary>
        [Test]
        public void HerBlueLine_SplitsThreeOnTheFileAndTwoOffIt()
        {
            foreach (var cardId in new[]
                     {
                         SwiftCardContent.SteamJump, SwiftCardContent.AssaultJump,
                         SwiftCardContent.DropTrooper
                     })
            {
                Assert.That(
                    Capabilities(Primary(cardId).EffectRoot),
                    Does.Contain(SyntheticPlacementCapabilities.PlaceInRadiusInStraightLineV1),
                    cardId.Value);
            }

            foreach (var cardId in new[] { SwiftCardContent.DelayedJump, SwiftCardContent.MobileScout })
            {
                var effects = SwiftActiveEffectContent.SwiftTriggeredEffects.All
                    .SelectMany(definition => definition.Nodes)
                    .Select(node => node.Capability)
                    .ToArray();

                Assert.That(
                    effects,
                    Does.Contain(SyntheticPlacementCapabilities.PlaceInRadiusOffEveryStraightLineV1),
                    cardId.Value + " places off every file, at the end of the turn.");
            }
        }

        /// <summary>
        /// Only Drop Trooper pushes two. The count is the push's own declared bound rather than a
        /// second node, which is the parameter milestone 8.8 minted for Wasp's kinetic Skills.
        /// </summary>
        [Test]
        public void OnlyDropTrooper_DeclaresATwoUnitPush()
        {
            Assert.That(MaximumPushTargets(SwiftCardContent.SteamJump), Is.EqualTo(1));
            Assert.That(MaximumPushTargets(SwiftCardContent.AssaultJump), Is.EqualTo(1));
            Assert.That(MaximumPushTargets(SwiftCardContent.DropTrooper), Is.EqualTo(2));
        }

        /// <summary>
        /// Only Mobile Scout travels. Both End-of-turn faces place off the file; the second sentence is
        /// the whole difference between them.
        /// </summary>
        [Test]
        public void OnlyMobileScout_FollowsItsPlacementWithAFastTravel()
        {
            var delayed = SwiftActiveEffectContent.SwiftTriggeredEffects.DelayedJump.Nodes
                .Select(node => node.Capability).ToArray();
            var scout = SwiftActiveEffectContent.SwiftTriggeredEffects.MobileScout.Nodes
                .Select(node => node.Capability).ToArray();

            Assert.That(delayed, Has.No.Member(SyntheticRosterCapabilities.FastTravelV1));
            Assert.That(scout, Does.Contain(SyntheticRosterCapabilities.FastTravelV1));
        }

        // -------------------------------------------------------------------------------------------
        // The Green line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// All three terrain faces ask about the SUBJECT, which is ledger row SW-11's correction: the
        /// actor-scoped predicate Tigerclaw names asks a different question, and none of her faces may
        /// reach it.
        /// </summary>
        [Test]
        public void HerThreeTerrainFaces_AskAboutTheSubjectAndNotTheActor()
        {
            foreach (var cardId in new[]
                     {
                         SwiftCardContent.Suppress, SwiftCardContent.PinDown,
                         SwiftCardContent.KillingGround
                     })
            {
                var capabilities = Capabilities(Primary(cardId).EffectRoot);

                Assert.That(
                    capabilities,
                    Does.Contain(SyntheticRosterCapabilities.SubjectAdjacentTerrainPresenceV1),
                    cardId.Value);
                Assert.That(
                    capabilities,
                    Has.No.Member(SyntheticRosterCapabilities.AdjacentTerrainPresenceV1),
                    cardId.Value + " must not reach the actor-scoped predicate - SW-11.");
            }
        }

        /// <summary>
        /// Killing Ground is Pin Down's sentence with the escalation, exactly as Super-Shotgun is
        /// Shotgun's.
        /// </summary>
        [Test]
        public void OnlyKillingGround_EscalatesAmongTheTerrainFaces()
        {
            Assert.That(
                Capabilities(Primary(SwiftCardContent.Suppress).EffectRoot),
                Has.No.Member(SyntheticConsequenceCapabilities.DefeatHeroV1));
            Assert.That(
                Capabilities(Primary(SwiftCardContent.PinDown).EffectRoot),
                Has.No.Member(SyntheticConsequenceCapabilities.DefeatHeroV1));
            Assert.That(
                Capabilities(Primary(SwiftCardContent.KillingGround).EffectRoot),
                Does.Contain(SyntheticConsequenceCapabilities.DefeatHeroV1));
        }

        /// <summary>
        /// Milestone 8.66, the correction Silverarrow's sweep found on this set. A selection names the
        /// node it feeds so a candidate the consumer could do nothing with is never offered - which is
        /// right for Suppress and Pin Down, whose step ends "if able", and exactly wrong for Killing
        /// Ground, whose step ends "or is defeated".
        ///
        /// <para>Declared on the escalating face, the discard narrowed every empty-handed hero out of
        /// the selection, so the one hero the second half of the sentence exists for was the one hero
        /// the first half refused to offer, and the face could never defeat anybody. Min's Grenade,
        /// Widget's Scorching Breath and Silverarrow's Treetop Sentinel all declare none for this
        /// shape; this is the fourth.</para>
        /// </summary>
        [Test]
        public void OnlyTheTwoIfAbleTerrainFaces_DeclareTheirDiscardAsTheSubjectsConsumer()
        {
            Assert.That(SubjectConsumerOf(SwiftCardContent.Suppress), Is.EqualTo("suppress-discard"));
            Assert.That(SubjectConsumerOf(SwiftCardContent.PinDown), Is.EqualTo("pin-discard"));
            Assert.That(SubjectConsumerOf(SwiftCardContent.KillingGround), Is.Null,
                "\"or is defeated\" makes an empty-handed hero a legal subject rather than an " +
                "unusable candidate.");
        }

        private static string SubjectConsumerOf(CardDefinitionId cardId)
        {
            var root = Primary(cardId).EffectRoot;
            var subject = root.Nodes.Single(node => node.Id.Value.EndsWith("-subject"));
            return GuardsDigital.Core.Effects.EffectSelectionDeclaration.TryReadConsumer(
                subject, out var consumer)
                ? consumer.Value
                : null;
        }

        /// <summary>
        /// The printed count is instance multiplicity: Mark for Death stands one promise and Hunting
        /// Season two. This is the case that would fail if the two Hunting Season declarations were
        /// collapsed into one.
        /// </summary>
        [Test]
        public void TheTwoBountyFaces_StandOneAndTwoPromises()
        {
            Assert.That(BountyNodeCount(SwiftCardContent.MarkForDeath), Is.EqualTo(1));
            Assert.That(BountyNodeCount(SwiftCardContent.HuntingSeason), Is.EqualTo(2));
        }

        /// <summary>
        /// The two Hunting Season promises carry different uniqueness keys, or the lifecycle would
        /// suppress the second as a duplicate and the card would pay half what it prints.
        /// </summary>
        [Test]
        public void HuntingSeasonsTwoPromises_CarryDifferentUniquenessKeys()
        {
            var keys = SwiftActiveEffectContent.Definitions
                .Where(definition => definition.EffectKind ==
                    GuardsDigital.Core.Rules.Consequences.MinionDefeatBountyQuery.EffectKind)
                .Select(definition => definition.UniquenessKey)
                .ToArray();

            Assert.That(keys.Length, Is.EqualTo(3), "One for Mark for Death, two for Hunting Season.");
            Assert.That(keys.Distinct().Count(), Is.EqualTo(3));
        }

        /// <summary>
        /// Every bounty declares the explicit cancellation that spending it is. A promise that did not
        /// could never be spent, which would make both faces pay forever.
        /// </summary>
        [Test]
        public void EveryBounty_DeclaresTheExplicitCancellationThatSpendingItIs()
        {
            foreach (var definition in SwiftActiveEffectContent.Definitions
                         .Where(candidate => candidate.EffectKind ==
                             GuardsDigital.Core.Rules.Consequences.MinionDefeatBountyQuery.EffectKind))
            {
                Assert.That(
                    definition.CancellationConditions,
                    Does.Contain(GuardsDigital.Core.Timing.ActiveEffectCancellationCondition.Explicit),
                    definition.DefinitionId);
                Assert.That(
                    definition.DurationKind,
                    Is.EqualTo(GuardsDigital.Core.Timing.ActiveEffectDurationKind.NextTurn),
                    definition.DefinitionId + " promises NEXT turn.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The borrow faces and the Ultimate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Reload! is a real modal: its two bullets do different things, so no union could state both -
        /// unlike Silverarrow's Red modals, whose arms differ only in which unit becomes the target.
        /// </summary>
        [Test]
        public void Reload_ComposesAModalWithABorrowArmAndADefeatArm()
        {
            var root = Primary(SwiftCardContent.Reload).EffectRoot;
            var capabilities = Capabilities(root);

            Assert.That(
                root.Nodes.Any(node => node.Kind == EffectNodeKind.Choose), Is.True,
                "\"Choose one —\" is a decision, and p.10 gives it to the card's controller.");
            Assert.That(
                capabilities,
                Does.Contain(SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1));
            Assert.That(
                capabilities, Does.Contain(SyntheticConsequenceCapabilities.DefeatMinionV1));
        }

        /// <summary>
        /// Both borrowing faces name the pool milestone 8.38c minted, and neither narrows by colour
        /// because neither prints one.
        /// </summary>
        [Test]
        public void BothBorrowingFaces_NameTheRightmostResolvedPoolAndNoColour()
        {
            var roots = new[]
            {
                Primary(SwiftCardContent.Reload).EffectRoot,
                SwiftEffectContent.BulletTimeTriggeredEffect(SwiftCardContent.ContentRevision)
            };

            foreach (var root in roots)
            {
                var borrow = root.Nodes.Single(node =>
                    node.Capability.Equals(
                        SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1));

                Assert.That(
                    (BorrowedActionPool)borrow.Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticBorrowedActionCapabilities.PoolParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo(BorrowedActionPool.OwnedRightmostResolvedCard));
                Assert.That(
                    (CardColor)borrow.Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticBorrowedActionCapabilities.ColorParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo(CardColor.None));
            }
        }

        /// <summary>
        /// The Ultimate declares milestone 8.38c2's fifth condition, not Tigerclaw's fourth. Getting
        /// this wrong would fire her grant after every repeat and every borrowed action.
        /// </summary>
        [Test]
        public void BulletTime_DeclaresTheCardResolutionTriggerAndGrantsOptionally()
        {
            var ultimate = SwiftCardContent.Definition(SwiftCardContent.BulletTime).UltimateRule;

            Assert.That(ultimate, Is.Not.Null);
            Assert.That(
                ultimate.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerResolvesBasicCard));

            var borrow = SwiftEffectContent
                .BulletTimeTriggeredEffect(SwiftCardContent.ContentRevision).Nodes
                .Single(node => node.Capability.Equals(
                    SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1));

            Assert.That(borrow.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                "\"you MAY perform the primary action on that card\".");
        }

        // -------------------------------------------------------------------------------------------
        // Composition debt
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Exactly one face carries a composition debt, and it is p.20's Handicap card - which p.6
        /// returns to the box and the supported even-player formats never deal.
        /// </summary>
        [Test]
        public void OnlyTheHandicapCard_CarriesAComposition()
        {
            var awaiting = SwiftCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions
                    .Any(action => action.TextComposition.IsAwaited))
                .Select(definition => definition.Id)
                .ToArray();

            Assert.That(awaiting, Is.EquivalentTo(new[] { SwiftCardContent.Backup }));
            Assert.That(
                SwiftCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(SwiftCardContent.Backup),
                "and it is not dealt, so no dealt face of hers carries one at all.");
        }

        /// <summary>
        /// Every dealt face that prints text beyond a targeting sentence composes a body for it. The
        /// case is written over the whole set rather than over a list, so a face added later cannot
        /// slip through with its text unimplemented and no deferral recorded.
        /// </summary>
        [Test]
        public void EveryDealtFaceWithARider_ComposesABodyForIt()
        {
            var declaredWithoutBody = new[]
            {
                SwiftCardContent.Snipe, SwiftCardContent.PreparedShot, SwiftCardContent.Killshot
            };

            foreach (var definition in SwiftCardContent.DealtDefinitions)
            {
                var primary = definition.ActionDefinitions
                    .SingleOrDefault(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                if (primary == null)
                {
                    Assert.That(definition.Id, Is.EqualTo(SwiftCardContent.BulletTime),
                        "The Ultimate is the only dealt face with no printed action.");
                    continue;
                }

                if (declaredWithoutBody.Contains(definition.Id))
                {
                    Assert.That(primary.EffectRoot, Is.Null, definition.Id.Value);
                    Assert.That(primary.AttackTargetPolicy, Is.Not.Null,
                        definition.Id.Value + " states its whole text as a declaration.");
                    continue;
                }

                Assert.That(
                    primary.EffectRoot, Is.Not.Null,
                    definition.Id.Value + " prints text beyond its targeting and must compose it.");
            }
        }

        /// <summary>
        /// Her four progressed arms compose, and each holds p.15's five: the two basics plus the three
        /// upgraded cards of that Tier. The case walks all four rather than sampling one, because the
        /// branches divide her kit along the lines her new vocabulary falls on and neither stands for
        /// the other.
        /// </summary>
        [Test]
        public void HerFourProgressedArms_EachHoldTheTwoBasicsAndThreeUpgrades()
        {
            var owner = new HeroId("swift-progressed-owner");

            foreach (var branch in new[]
                     {
                         SwiftUpgradeBranch.FirstSibling, SwiftUpgradeBranch.SecondSibling
                     })
            {
                foreach (var tier in new[] { 2, 3 })
                {
                    var hand = tier == 2
                        ? SwiftProgressedHands.CreateTierTwoHand(owner, "swift-progressed", branch)
                        : SwiftProgressedHands.CreateTierThreeHand(owner, "swift-progressed", branch);
                    var label = $"{branch} tier {tier}";

                    Assert.That(hand.Instances.Count, Is.EqualTo(5), label);
                    Assert.That(hand.DeckInstances.Count, Is.EqualTo(13), label);

                    var held = hand.Instances
                        .Select(card => card.DefinitionReference.DefinitionId)
                        .ToArray();

                    Assert.That(held, Does.Contain(SwiftCardContent.Reload), label);
                    Assert.That(held, Does.Contain(SwiftCardContent.Bounce), label);
                    Assert.That(
                        held, Has.No.Member(SwiftCardContent.Backup),
                        label + ": p.6 has already boxed the Handicap card.");
                    Assert.That(
                        held, Has.No.Member(SwiftCardContent.BulletTime),
                        label + ": p.6 deals the Ultimate to the deck, and p.16's unlock is what " +
                        "moves it.");

                    var upgrades = held
                        .Where(id => !id.Equals(SwiftCardContent.Reload) &&
                            !id.Equals(SwiftCardContent.Bounce))
                        .ToArray();
                    Assert.That(upgrades.Length, Is.EqualTo(3), label);
                    Assert.That(
                        upgrades.Select(id => SwiftCardContent.Definition(id).Progression.Tier)
                            .Distinct()
                            .Single(),
                        Is.EqualTo(tier == 2 ? CardTier.II : CardTier.III),
                        label + ": all three upgrades are of that Tier.");
                    Assert.That(
                        upgrades.Select(id => SwiftCardContent.Definition(id).Progression.Color)
                            .Distinct()
                            .Count(),
                        Is.EqualTo(3),
                        label + ": one of each colour, which is what p.15's per-colour pair means.");
                }
            }
        }

        /// <summary>
        /// The two branches are disjoint at both Tiers: no card appears in both arms, which is what
        /// makes running both arms cover her kit rather than repeat it.
        /// </summary>
        [Test]
        public void HerTwoBranches_ShareNoUpgradedCard()
        {
            var owner = new HeroId("swift-branch-owner");

            foreach (var tier in new[] { 2, 3 })
            {
                var first = Upgrades(owner, tier, SwiftUpgradeBranch.FirstSibling);
                var second = Upgrades(owner, tier, SwiftUpgradeBranch.SecondSibling);

                Assert.That(first.Intersect(second), Is.Empty, "tier " + tier);
                Assert.That(first.Concat(second).Distinct().Count(), Is.EqualTo(6), "tier " + tier);
            }
        }

        private static IReadOnlyList<CardDefinitionId> Upgrades(
            HeroId owner, int tier, SwiftUpgradeBranch branch)
        {
            var hand = tier == 2
                ? SwiftProgressedHands.CreateTierTwoHand(owner, "swift-branch", branch)
                : SwiftProgressedHands.CreateTierThreeHand(owner, "swift-branch", branch);
            return hand.Instances
                .Select(card => card.DefinitionReference.DefinitionId)
                .Where(id => !id.Equals(SwiftCardContent.Reload) &&
                    !id.Equals(SwiftCardContent.Bounce))
                .ToArray();
        }

        /// <summary>
        /// One seat's setup composes: eighteen instances, five in hand and thirteen in the deck, each
        /// owned by the seat that dealt them.
        /// </summary>
        [Test]
        public void HerHand_ComposesAtPageSixSetup()
        {
            var owner = new HeroId("swift-printed-set-owner");
            var hand = SwiftCardContent.CreateHand(owner, "swift-printed-set");

            Assert.That(hand.Definitions.Count, Is.EqualTo(18));
            Assert.That(hand.Instances.Count, Is.EqualTo(5));
            Assert.That(hand.DeckInstances.Count, Is.EqualTo(13));
            Assert.That(
                hand.AllInstances.All(card => card.OwnerId.Equals(owner)),
                Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static ActionDefinition Primary(CardDefinitionId cardId) =>
            SwiftCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        private static IReadOnlyList<EffectCapabilityReference> Capabilities(EffectDefinition root) =>
            root == null
                ? Array.Empty<EffectCapabilityReference>()
                : root.Nodes
                    .Select(node => node.Capability)
                    .Where(capability => capability.IsDefined)
                    .ToArray();

        private static int MaximumPushTargets(CardDefinitionId cardId) =>
            Primary(cardId).EffectRoot.Nodes
                .Single(node => node.Capability.Equals(
                    SyntheticDisplacementCapabilities.ObjectPushV3))
                .Inputs
                .Single(input => input.ParameterName ==
                    SyntheticDisplacementCapabilities.MaximumTargetsParameterName)
                .Literal.IntegerValue;

        private static int BountyNodeCount(CardDefinitionId cardId) =>
            Primary(cardId).EffectRoot.Nodes
                .Count(node => node.Capability.Equals(
                        SwiftActiveEffectContent.MarkForDeathBountyV1) ||
                    node.Capability.Equals(SwiftActiveEffectContent.HuntingSeasonFirstBountyV1) ||
                    node.Capability.Equals(SwiftActiveEffectContent.HuntingSeasonSecondBountyV1));
    }
}
