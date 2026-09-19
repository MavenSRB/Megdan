using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.61h. Min the Dragonmonk's three Discipline faces: Inner Strength and Perfect Self -
    /// "This round: Double your item bonuses", with Perfect Self's second bullet taking a Tier II card
    /// out of her deck as a permanent item - and her Purple Tier IV, Flurry of Blows, "Each time after
    /// you perform an attack action you may repeat it once on a different target."
    ///
    /// <para><b>What each half needs proving about, and where.</b> The doubling changes rulebook
    /// p.16's own arithmetic, which eight production readings compose, so the cases below ask every one
    /// of them: the five promoted value totals, p.7's turn order, an Active effect's declared Radius,
    /// and the hero rail's projected badge. The Ultimate changes which completed actions p.16's
    /// permanent ability fires after, so the cases ask the fact, the derivation and the repeat subject
    /// - including the two negatives p.10 states, a Skill that is not an attack action and a repeated
    /// instance that may not be repeated again.</para>
    ///
    /// <para><b>Why these cases read the production queries rather than drive a configured match.</b>
    /// Nothing of Min's is registered - milestone 7.9's gate holds her content out of
    /// <see cref="MatchContentCatalogs"/> until 8.61j deals her - so no composed match can put one of
    /// these cards in a hand and take p.14's order from a played Skill. That is the boundary every
    /// composition block before this one recorded, and it is why the seams are proved here over the
    /// same production types a match reaches: the live rules set through
    /// <see cref="RuleModificationQuery"/>, the effect body through
    /// <see cref="EffectResolutionSession"/> over a catalog composed from the deferred handlers, and
    /// the trigger through <see cref="TriggeredEffectResolutionAuthority"/>. Milestone 8.61j runs the
    /// same chain from a dealt hand, and 8.61k from real input.</para>
    ///
    /// <para><b>Faces of other heroes appear on purpose.</b> The doubling is a rule about ITEMS and
    /// says nothing about which card is being read, so the totals below are taken off whichever printed
    /// icons the corpus already has - Rowenna's composed Attack, Range and Radius faces - rather than
    /// off cards written here to agree. MI-Q5's level-up case uses her Green ladder for the same
    /// reason: p.15's Upgrade step is hero-neutral, and Min's own ladder is not whole until 8.61i.</para>
    /// </summary>
    public sealed class MinItemAndFlurryTests
    {
        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static readonly HexCoordinate MinAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);

        // -------------------------------------------------------------------------------------------
        // The declaration itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Double your item bonuses" is one rule with one number, and the number is a multiplier on
        /// p.16's +1 rather than a contribution to a channel. A declaration below the page's own
        /// arithmetic is refused: nothing printed reduces an item bonus, and a multiplier of one is the
        /// page unmodified rather than a rule.
        /// </summary>
        [Test]
        public void TheDoubling_IsOneMultiplierFamilyThatNamesNoChannel()
        {
            var modification = RuleModification.ForItemBonusMultiplier(2);

            Assert.That(modification.Kind, Is.EqualTo(RuleModificationKind.ItemBonusMultiplier));
            Assert.That(modification.Amount, Is.EqualTo(2));
            Assert.That(
                modification.Channel, Is.EqualTo(PrintedValueChannel.None),
                "\"Your item bonuses\" names all six of p.16's icons at once; a channel here would be " +
                "a card that doubled only that icon's items.");
            Assert.That(modification.Activities, Is.Empty);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => RuleModification.ForItemBonusMultiplier(1),
                "One is rulebook p.16 unmodified, which is not a rule a card declares.");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RuleModification.ForItemBonusMultiplier(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RuleModification.ForItemBonusMultiplier(-2));
        }

        /// <summary>
        /// Both faces print the same sentence, so both declare the same family at the same amount, on
        /// a This-round effect scoped to their own controller. They carry two uniqueness keys because
        /// p.10 counts one instance of an active effect per CARD.
        /// </summary>
        [Test]
        public void BothDoublingFaces_DeclareOneThisRoundRuleOnTheirOwnController()
        {
            foreach (var definition in MinComponentContent.DisciplineDefinitions)
            {
                Assert.That(
                    definition.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero),
                    "\"YOUR item bonuses\" is a sentence about the hero whose card it is.");
                Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisRound));
                Assert.That(definition.ExpirationCheckpoint, Is.EqualTo(TimingCheckpoint.RoundEnd));
                Assert.That(definition.CreationCheckpoint, Is.EqualTo(TimingCheckpoint.ActionStarted));
                Assert.That(
                    definition.ActivationCheckpoint, Is.EqualTo(TimingCheckpoint.ActionStarted),
                    "A Skill's effect is the action (p.12), so the doubling is up for this very turn.");
                Assert.That(
                    definition.EffectKind,
                    Is.EqualTo(MinComponentContent.DoubledItemsEffectKind));

                var modification = definition.Modifications.Single();
                Assert.That(modification.Kind, Is.EqualTo(RuleModificationKind.ItemBonusMultiplier));
                Assert.That(
                    modification.Amount,
                    Is.EqualTo(MinComponentContent.DoubledItemBonusMultiplier));
            }

            Assert.That(
                MinComponentContent.DisciplineDefinitions.Select(value => value.UniquenessKey).Distinct()
                    .Count(),
                Is.EqualTo(2),
                "p.10 allows one instance of an active effect per card, so two cards owe two keys.");
        }

        // -------------------------------------------------------------------------------------------
        // The multiplier, read as the rules read it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The live set is where the doubling becomes true, and it is true for the hero it is declared
        /// about and for nobody else.
        /// </summary>
        [Test]
        public void TheMultiplier_IsOneUntilTheEffectStands_AndThenTwoForItsOwnHeroAlone()
        {
            var world = World.Create();

            Assert.That(
                ItemBonusMultiplierQuery.For(Min, world.Modifications()),
                Is.EqualTo(ItemBonusMultiplierQuery.PrintedMultiplier));
            Assert.That(
                ItemBonusMultiplierQuery.BonusPerItem(Min, world.Modifications()),
                Is.EqualTo(ItemBonusQuery.BonusPerItem));

            world.PlayInnerStrength();

            Assert.That(ItemBonusMultiplierQuery.For(Min, world.Modifications()), Is.EqualTo(2));
            Assert.That(
                ItemBonusMultiplierQuery.BonusPerItem(Min, world.Modifications()),
                Is.EqualTo(2 * ItemBonusQuery.BonusPerItem));
            Assert.That(
                ItemBonusMultiplierQuery.For(Enemy, world.Modifications()),
                Is.EqualTo(ItemBonusMultiplierQuery.PrintedMultiplier),
                "The declaration is about its own controller; nobody else's items are doubled.");
        }

        /// <summary>
        /// p.15 replaces the lower Tier, so Inner Strength and Perfect Self can never sit in one deck -
        /// and even standing together they double rather than quadruple. The largest wins; nothing
        /// multiplies.
        /// </summary>
        [Test]
        public void TwoDoublingDeclarations_TakeTheLargestRatherThanStacking()
        {
            var world = World.Create();
            world.PlayInnerStrength();
            world.PlayPerfectSelfDoublingDirectly();

            Assert.That(
                world.LiveEffectCount(MinComponentContent.DoubledItemsEffectKind), Is.EqualTo(2),
                "Two cards are two instances - p.10 counts them per card.");
            Assert.That(
                ItemBonusMultiplierQuery.For(Min, world.Modifications()), Is.EqualTo(2),
                "\"Double\" twice is still double: the largest declaration wins and no product is taken.");
        }

        /// <summary>p.19's "This round" ends at the round's end, and the item bonus returns to p.16's.</summary>
        [Test]
        public void TheDoubling_EndsWhenTheRoundDoes()
        {
            var world = World.Create();
            world.EarnItems(PrintedValueChannel.Attack);
            world.PlayInnerStrength();
            Assert.That(
                world.AttackTotalOf(RowennaCardContent.ParagonOfGrace),
                Is.EqualTo(PrintedAttack + 2));
            Assert.That(ItemBonusMultiplierQuery.For(Min, world.Modifications()), Is.EqualTo(2));

            world.EndTheRound();

            Assert.That(
                ItemBonusMultiplierQuery.For(Min, world.Modifications()),
                Is.EqualTo(ItemBonusMultiplierQuery.PrintedMultiplier));
            Assert.That(
                world.AttackTotalOf(RowennaCardContent.ParagonOfGrace), Is.EqualTo(PrintedAttack + 1),
                "One Attack item, one point, exactly as the page has it when nothing is standing.");
        }

        // -------------------------------------------------------------------------------------------
        // Every one of p.16's six channels, at the production reading that composes it
        // -------------------------------------------------------------------------------------------

        /// <summary>Paragon of Grace's printed Attack, the roster's Attack icon this fixture reads.</summary>
        private const int PrintedAttack = 6;

        /// <summary>Its printed Radius.</summary>
        private const int PrintedRadius = 3;

        /// <summary>Protect the Weak's printed Range.</summary>
        private const int PrintedRange = 4;

        /// <summary>Inner Strength's own printed Movement ribbon.</summary>
        private const int PrintedMovement = 3;

        /// <summary>Inner Strength's own printed Defense ribbon.</summary>
        private const int PrintedDefense = 2;

        /// <summary>
        /// One item, one point, on every channel p.16 names - and two points on each of them once the
        /// doubling stands. The five promoted totals and p.7's turn order are composed here exactly as
        /// the production seams compose them, so no channel can be doubled in one reading and not in
        /// another.
        /// </summary>
        [Test]
        public void TheDoubling_ReachesEveryChannelPageSixteenNames()
        {
            var world = World.Create();
            world.EarnOneItemOfEveryIcon();

            Assert.That(world.AttackTotalOf(RowennaCardContent.ParagonOfGrace),
                Is.EqualTo(PrintedAttack + 1));
            Assert.That(world.RadiusTotalOf(RowennaCardContent.ParagonOfGrace),
                Is.EqualTo(PrintedRadius + 1));
            Assert.That(world.RangeTotalOf(RowennaCardContent.ProtectTheWeak),
                Is.EqualTo(PrintedRange + 1));
            Assert.That(world.MovementTotalOfInnerStrength(), Is.EqualTo(PrintedMovement + 1));
            Assert.That(world.DefenseTotalOfInnerStrength(), Is.EqualTo(PrintedDefense + 1));

            world.PlayInnerStrength();

            Assert.That(world.AttackTotalOf(RowennaCardContent.ParagonOfGrace),
                Is.EqualTo(PrintedAttack + 2), "Attack Total.");
            Assert.That(world.RadiusTotalOf(RowennaCardContent.ParagonOfGrace),
                Is.EqualTo(PrintedRadius + 2), "Radius Total.");
            Assert.That(world.RangeTotalOf(RowennaCardContent.ProtectTheWeak),
                Is.EqualTo(PrintedRange + 2), "Range Total.");
            Assert.That(world.MovementTotalOfInnerStrength(),
                Is.EqualTo(PrintedMovement + 2), "Movement allowance.");
            Assert.That(world.DefenseTotalOfInnerStrength(),
                Is.EqualTo(PrintedDefense + 2), "Defense Total.");
        }

        /// <summary>
        /// Every doubled point is still a contribution of its own item's, so a total can still say
        /// which item raised it - which is what <see cref="ValueResult"/> exists for. Two Attack items
        /// under one slot are two contributions of two, never one of four.
        /// </summary>
        [Test]
        public void EachDoubledPoint_StaysAttributableToTheItemCardThatSuppliedIt()
        {
            var world = World.Create();
            world.EarnItems(PrintedValueChannel.Attack, PrintedValueChannel.Attack);
            world.PlayInnerStrength();

            var total = world.AttackValueOf(RowennaCardContent.ParagonOfGrace);
            var items = total.Contributions
                .Where(value => value.Kind == ValueContributionKind.ItemModifier)
                .ToArray();

            Assert.That(items.Length, Is.EqualTo(2), "Two items are two contributions.");
            Assert.That(items.Select(value => value.Amount), Is.All.EqualTo(2));
            Assert.That(
                items.Select(value => value.SourceCardInstanceId).Distinct().Count(), Is.EqualTo(2),
                "Each names the item card that supplied it, exactly as an undoubled one does.");
            Assert.That(total.Total, Is.EqualTo(PrintedAttack + 4));
        }

        /// <summary>
        /// p.16: "If an Action card has no matching icon, the item has no effect on that particular
        /// Action card." Doubling nothing is still nothing - the condition is asked of the printed
        /// result before the multiplier is applied, so a card printing no Range icon gains no Range at
        /// all however many Range items its owner holds.
        /// </summary>
        [Test]
        public void TheDoubling_AddsNothingToACardPrintingNoMatchingIcon()
        {
            var world = World.Create();
            world.EarnItems(PrintedValueChannel.Range);
            world.PlayInnerStrength();

            Assert.That(
                world.RangeTotalOf(RowennaCardContent.ParagonOfGrace), Is.Zero,
                "Her Attack face prints no Range icon at all, and p.16 raises icons rather than " +
                "inventing them.");
        }

        /// <summary>A hero holding no item has nothing to double, and the totals do not move.</summary>
        [Test]
        public void TheDoubling_ChangesNothingForAHeroHoldingNoItems()
        {
            var world = World.Create();
            world.PlayInnerStrength();

            Assert.That(world.AttackTotalOf(RowennaCardContent.ParagonOfGrace),
                Is.EqualTo(PrintedAttack));
            Assert.That(world.MovementTotalOfInnerStrength(), Is.EqualTo(PrintedMovement));
            Assert.That(world.DefenseTotalOfInnerStrength(), Is.EqualTo(PrintedDefense));
        }

        /// <summary>
        /// p.7 orders the turn by the Initiative total, which p.16's item raises - so the doubling can
        /// decide the order. Min's Inner Strength prints Initiative 2 and the enemy's card prints 3;
        /// one Initiative item ties them, and the doubled item puts her ahead.
        /// </summary>
        [Test]
        public void TheDoubling_IsReadByPageSevensTurnOrder()
        {
            var world = World.Create();
            world.EarnItems(PrintedValueChannel.Initiative);

            Assert.That(
                world.InitiativeTotalOfInnerStrength(), Is.EqualTo(InnerStrengthInitiative + 1),
                "One Initiative item, one point - the tie p.7 would break by the Tie Breaker.");

            world.PlayInnerStrength();

            Assert.That(
                world.InitiativeTotalOfInnerStrength(), Is.EqualTo(InnerStrengthInitiative + 2),
                "The doubled item is read where p.7 chooses the highest candidate, not after it.");
            Assert.That(
                world.HighestInitiativeCandidateIsMin(), Is.True,
                "A tie broken by the doubled item is the whole reason the reading is composed first.");
        }

        /// <summary>Inner Strength's printed Initiative.</summary>
        private const int InnerStrengthInitiative = 2;

        /// <summary>
        /// p.17's Active effect measures its disk from a printed Radius icon, and p.16's Radius item
        /// raises it - so the doubling widens a standing effect's disk too. It is read inside the
        /// derivation of the very set it belongs to, which is why the multiplier is taken off the
        /// lifecycle there rather than off a bound set.
        /// </summary>
        [Test]
        public void TheDoubling_WidensAnActiveEffectsOwnDeclaredRadius()
        {
            var world = World.Create();
            world.EarnItems(PrintedValueChannel.Radius);

            Assert.That(world.PetrifiedEnemiesWithin(2), Is.True, "Radius 2 plus one item reaches 3.");
            Assert.That(world.PetrifiedEnemiesWithin(4), Is.False);

            world.PlayInnerStrength();

            Assert.That(
                world.PetrifiedEnemiesWithin(4), Is.True,
                "Radius 2 plus a doubled item reaches 4, and the disk is measured from the same rule " +
                "the card's own Radius Total is.");
        }

        /// <summary>
        /// Milestone 8.55's badge reads the printed number and the item bonus as two readings - so a
        /// doubled bonus shows `+2` for one item. The seat derives the multiplier from the same
        /// live-effect facts it draws the auras from; no projection is widened and nothing crosses the
        /// wire.
        /// </summary>
        [Test]
        public void TheBadge_ReadsTheDoubledItemBonusFromWhatASeatAlreadyHolds()
        {
            var world = World.Create();
            var printed = World.PrintedInitiative(InnerStrengthInitiative);

            Assert.That(
                ItemBonusQuery.ProjectedItemBonus(printed, 1, world.ProjectedBonusPerItem()),
                Is.EqualTo(1));

            world.PlayInnerStrength();

            Assert.That(
                world.ProjectedBonusPerItem(), Is.EqualTo(2),
                "The badge asks the same question the rules do, from the cards a seat can name.");
            Assert.That(
                ItemBonusQuery.ProjectedItemBonus(printed, 1, world.ProjectedBonusPerItem()),
                Is.EqualTo(2),
                "`6+1` becomes `6+2`, which is the reading the user settled at 8.55.");
            Assert.That(
                ItemBonusQuery.TotalWithProjectedItems(printed, 1, world.ProjectedBonusPerItem()),
                Is.EqualTo(InnerStrengthInitiative + 2));
            Assert.That(
                ItemBonusQuery.ProjectedItemBonus(printed, 0, world.ProjectedBonusPerItem()),
                Is.Zero,
                "No item is no bonus, doubled or not.");
        }

        /// <summary>
        /// A composition that was handed no capability catalog reads rulebook p.16 unmodified rather
        /// than reaching for the build's own - the rule milestone 8.35z63 set for the same derivation's
        /// auras.
        /// </summary>
        [Test]
        public void TheBadge_ReadsThePageUnmodifiedWhenItsCompositionNamesNoCatalog()
        {
            var world = World.Create();
            world.PlayInnerStrength();

            Assert.That(
                world.ProjectedBonusPerItemWithoutACatalog(),
                Is.EqualTo(ItemBonusQuery.BonusPerItem));
        }

        // -------------------------------------------------------------------------------------------
        // Perfect Self
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Choose one or both" is p.10's modal cardinality written out: minimum one, maximum two, and
        /// the chooser is the hero performing the card because p.10 hands her every decision the text
        /// does not assign.
        /// </summary>
        [Test]
        public void PerfectSelf_IsAOneOrBothModalOverItsTwoPrintedBullets()
        {
            var effect = MinEffectContent.PerfectSelfSkillEffect(MinCardContent.DisciplineRevision);
            var modal = effect.Nodes.Single(node => node.Kind == EffectNodeKind.Choose);

            Assert.That(modal.MinimumSelections, Is.EqualTo(1));
            Assert.That(modal.MaximumSelections, Is.EqualTo(2));
            Assert.That(modal.Chooser, Is.EqualTo(EffectChooserRole.Actor));
            Assert.That(
                modal.ChildNodeIds.Select(id => id.Value),
                Is.EqualTo(new[]
                {
                    MinEffectContent.PerfectSelfDoubleArmNode,
                    MinEffectContent.PerfectSelfTakeArmNode
                }));

            Assert.That(
                MinEffectContent.DisciplineDecisionLabels().Select(label => label.NodeId.Value),
                Is.EquivalentTo(new[]
                {
                    MinEffectContent.PerfectSelfDoubleArmNode,
                    MinEffectContent.PerfectSelfTakeArmNode
                }),
                "Every arm the window can show says what taking it does, in the card's own words.");

            Assert.That(
                MinEffectContent.InnerStrengthSkillEffect(MinCardContent.DisciplineRevision).Nodes
                    .Any(node => node.Kind == EffectNodeKind.Choose),
                Is.False,
                "Inner Strength prints one mandatory sentence and opens no decision at all.");
        }

        /// <summary>
        /// The first bullet alone: the doubling stands and the deck is untouched.
        /// </summary>
        [Test]
        public void PerfectSelf_TakingOnlyTheDoubling_LeavesTheDeckWhole()
        {
            var world = World.Create();
            var deckBefore = world.DeckCardIds();

            world.ResolvePerfectSelf(MinEffectContent.PerfectSelfDoubleArmNode);

            Assert.That(ItemBonusMultiplierQuery.For(Min, world.Modifications()), Is.EqualTo(2));
            Assert.That(world.DeckCardIds(), Is.EqualTo(deckBefore));
            Assert.That(world.Items(Min), Is.Empty);
        }

        /// <summary>
        /// The second bullet alone: one Tier II card leaves the deck for good and becomes an item under
        /// the slot its own printed icon matches. Nothing about p.16's bonus is doubled, because that
        /// bullet was not taken.
        /// </summary>
        [Test]
        public void PerfectSelf_TakingOnlyTheDeckCard_MakesItAPermanentItemUnderItsOwnIcon()
        {
            var world = World.Create();

            var chosen = world.ResolvePerfectSelf(MinEffectContent.PerfectSelfTakeArmNode);

            var item = world.Items(Min).Single();
            Assert.That(item.SourceCardInstanceId, Is.EqualTo(chosen));
            Assert.That(
                item.Icon, Is.EqualTo(world.ItemIconOf(chosen)),
                "p.16 files the card under the slot matching the icon IT prints.");
            Assert.That(
                world.LocationOf(chosen), Is.EqualTo(CardLocation.ItemSlot),
                "It left the deck and it does not come back: p.16's item is permanent.");
            Assert.That(world.DeckCardIds(), Has.No.Member(chosen));
            Assert.That(
                ItemBonusMultiplierQuery.For(Min, world.Modifications()),
                Is.EqualTo(ItemBonusMultiplierQuery.PrintedMultiplier));
        }

        /// <summary>
        /// "One or BOTH", in either order - p.10 lets the chooser apply the bullets she took in any
        /// order she likes, and both orders end in the same place: the doubling stands and the item is
        /// under its slot.
        /// </summary>
        [Test]
        public void PerfectSelf_TakingBoth_AppliesBothInEitherOrder()
        {
            foreach (var order in new[]
            {
                new[]
                {
                    MinEffectContent.PerfectSelfDoubleArmNode, MinEffectContent.PerfectSelfTakeArmNode
                },
                new[]
                {
                    MinEffectContent.PerfectSelfTakeArmNode, MinEffectContent.PerfectSelfDoubleArmNode
                }
            })
            {
                var world = World.Create();
                var chosen = world.ResolvePerfectSelf(order);

                Assert.That(
                    ItemBonusMultiplierQuery.For(Min, world.Modifications()), Is.EqualTo(2),
                    string.Join(" then ", order));
                Assert.That(world.Items(Min).Single().SourceCardInstanceId, Is.EqualTo(chosen));
            }
        }

        /// <summary>
        /// The item the second bullet made raises its own channel on the next card - and the first
        /// bullet, taken beside it, doubles that very bonus for the rest of the round. Nothing states
        /// that interaction; it falls out of both sentences being true at once.
        /// </summary>
        [Test]
        public void PerfectSelf_TheItemItMade_IsRaisedByTheDoublingItStandsBeside()
        {
            var world = World.Create();
            world.ResolvePerfectSelf(
                MinEffectContent.PerfectSelfTakeArmNode, MinEffectContent.PerfectSelfDoubleArmNode);

            var icon = world.Items(Min).Single().Icon;
            Assert.That(
                world.TotalOnChannel(icon), Is.EqualTo(world.PrintedOnChannel(icon) + 2),
                "One item under a standing doubling adds two, on whichever icon the card printed.");
        }

        /// <summary>
        /// "A Tier II card from your DECK": the candidates are that Tier, that zone and that owner, and
        /// nothing else. A Tier II card returned to the deck by a Tier III upgrade is in the deck, so it
        /// qualifies - p.15 says the upgraded card "returns it to the deck", and the sentence asks
        /// where a card is rather than how it got there (MI-Q5).
        /// </summary>
        [Test]
        public void PerfectSelf_OffersTheTierTwoCardsInHerDeckAndNothingElse()
        {
            var world = World.Create();
            var returned = world.ReturnATierTwoCardToTheDeckByUpgrading();

            var offered = world.PerfectSelfCandidates();

            Assert.That(offered, Is.Not.Empty);
            Assert.That(
                offered.Select(world.TierOf), Is.All.EqualTo(CardTier.II),
                "Tier I and Tier III cards in the same deck are not what the sentence names.");
            Assert.That(offered.Select(world.LocationOf), Is.All.EqualTo(CardLocation.Deck));
            Assert.That(offered.Select(world.OwnerOf), Is.All.EqualTo(Min));
            Assert.That(
                offered, Has.Member(returned),
                "A Tier II returned to the deck by a Tier III upgrade is a Tier II card in the deck.");
            Assert.That(
                offered, Has.No.Member(world.TierTwoCardInHand()),
                "A card in her hand is not a card in her deck.");
            Assert.That(
                offered.Intersect(world.EnemyDeckCardIds()), Is.Empty,
                "\"Your deck\" is hers.");
        }

        /// <summary>
        /// A deck with no Tier II card left makes the bullet an impossible step, and p.10 skips one -
        /// so the modal never offers it. The offer and the answer therefore admit the same set, which
        /// is milestone 8.40g's law.
        /// </summary>
        [Test]
        public void PerfectSelf_DoesNotOfferTheDeckBulletWhenNoTierTwoCardIsLeft()
        {
            var world = World.Create();
            world.EmptyTheDeckOfTierTwoCards();

            Assert.That(world.PerfectSelfCandidates(), Is.Empty);
            Assert.That(
                world.PerfectSelfTakeArmIsFeasible(), Is.False,
                "A bullet nothing can perform is not offered rather than offered and then refused.");
            Assert.That(
                world.PerfectSelfDoubleArmIsFeasible(), Is.True,
                "The other bullet is always performable, so the modal still has a legal answer.");
        }

        /// <summary>
        /// The offer is hers: the choice names Min as the hero who answers it, which is what scopes the
        /// pause to her seat. p.4's open-information boundary then withholds a decision from every seat
        /// that does not own it, which is <c>SeatPendingSurface</c>'s one rule for every effect choice
        /// there is.
        /// </summary>
        [Test]
        public void PerfectSelf_PutsTheDeckChoiceToMinAlone()
        {
            var world = World.Create();
            var request = world.PerfectSelfDeckChoiceRequest();

            Assert.That(request.ChooserHeroId, Is.EqualTo(Min));
            Assert.That(request.MinimumSelections, Is.EqualTo(1));
            Assert.That(request.MaximumSelections, Is.EqualTo(1));
            Assert.That(
                request.Candidates.Select(value => value.CardInstanceId),
                Is.EquivalentTo(world.PerfectSelfCandidates()),
                "The pause offers exactly the cards the query selects - one list, not two.");
        }

        /// <summary>
        /// An answer is checked against the deck as it stands when it arrives, not against the deck the
        /// offer was built from: a card that has moved is refused rather than taken from wherever it
        /// got to.
        /// </summary>
        [Test]
        public void PerfectSelf_RefusesAnAnswerNamingACardThatHasLeftTheDeck()
        {
            var world = World.Create();

            Assert.That(world.PerfectSelfAnswerIsAccepted(world.TierTwoCardInHand()), Is.False);
            Assert.That(world.Items(Min), Is.Empty);
            Assert.That(
                world.LocationOf(world.TierTwoCardInHand()), Is.EqualTo(CardLocation.Hand),
                "A refused answer mutates nothing.");
        }

        /// <summary>
        /// MI-Q5's other half, and it is p.15's rule rather than Min's: a level-up of a colour whose
        /// deck holds only ONE of the two next-Tier candidates takes that one into hand and sets
        /// nothing aside, so no item is gained. The page assumes both cards are there and says what to
        /// do with the one it names; the absent sibling is an answer rather than a refusal.
        /// </summary>
        [Test]
        public void ALevelUpWithOneCandidateLeft_IsLegalAndSetsNothingAside()
        {
            var fixture = OneCandidateLevelUp.Create();

            var begun = fixture.Begin();

            Assert.That(begun.Accepted, Is.True, "A one-candidate ladder is still a legal purchase.");
            Assert.That(
                fixture.Options.Count, Is.EqualTo(1),
                "One card in the deck is one option, not none.");

            fixture.Answer(0);

            Assert.That(
                fixture.LocationOf(fixture.TakenCardId), Is.EqualTo(CardLocation.Hand),
                "p.15 takes the chosen card into the hand.");
            Assert.That(
                fixture.SetAsideCount, Is.Zero,
                "There is no sibling to set aside, so nothing is set aside (MI-Q5).");
            Assert.That(
                fixture.ItemsGained, Is.Zero,
                "p.16 makes an item out of a set-aside card, and there is none.");
        }

        // -------------------------------------------------------------------------------------------
        // Flurry of Blows
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The Ultimate prints no Initiative, no icons and no action, and its whole content is p.16's
        /// permanent ability: one repeat, may, at most once, on a different target, after every attack
        /// action.
        /// </summary>
        [Test]
        public void FlurryOfBlows_IsOneRepeatNodeUnderAnAfterAttackTrigger()
        {
            var card = World.DefinitionOf(MinCardContent.FlurryOfBlows);

            Assert.That(card.ActionDefinitions, Is.Empty, "It is never played and never defends.");
            Assert.That(card.Progression.IsUltimate, Is.True);
            Assert.That(card.UltimateRule, Is.Not.Null);
            Assert.That(
                card.UltimateRule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerPerformsAttackAction));
            Assert.That(
                card.UltimateRule.TriggerCheckpoints,
                Is.EqualTo(new[] { TimingCheckpoint.ActionEnded }));

            var repeat = MinEffectContent
                .FlurryOfBlowsTriggeredEffect(MinCardContent.DisciplineRevision).Nodes.Single();
            Assert.That(repeat.Kind, Is.EqualTo(EffectNodeKind.RepeatAction));
            Assert.That(repeat.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May), "\"you may\".");
            Assert.That(repeat.MaximumRepeats, Is.EqualTo(1), "\"once\".");
            Assert.That(
                repeat.RepeatTargetBinding, Is.EqualTo(RepeatTargetBinding.DifferentTarget),
                "\"on a different target\".");
            Assert.That(repeat.Chooser, Is.EqualTo(EffectChooserRole.Actor));
        }

        /// <summary>
        /// "An attack action" is p.13's Attack and nothing else. The fact is produced only for that
        /// kind, so a Skill, a Movement, a Defense, p.12's Hold and the two replacements p.13 and p.11
        /// name - Clear and Fast Travel - reach nothing.
        /// </summary>
        [Test]
        public void ThePerformedAttackFact_IsProducedForAnAttackAndForNoOtherKind()
        {
            foreach (ActionKind kind in Enum.GetValues(typeof(ActionKind)))
            {
                if (kind == ActionKind.None)
                {
                    continue;
                }

                var fact = PerformedAttackAction.ForCompletedAction(
                    Min, World.SomeCardInstanceId, World.SomeActionDefinitionId, kind);

                Assert.That(
                    fact.IsDefined, Is.EqualTo(kind == ActionKind.Attack),
                    $"{kind} is {(kind == ActionKind.Attack ? string.Empty : "not ")}an attack action.");
            }

            Assert.That(
                PerformedAttackAction.ForCompletedAction(
                    default, World.SomeCardInstanceId, World.SomeActionDefinitionId,
                    ActionKind.Attack).IsDefined,
                Is.False,
                "A completion that names no hero records nothing rather than faulting.");
        }

        /// <summary>
        /// p.10: "You cannot repeat a repeated action!" The fact is still produced for the repeated
        /// instance - a repeated Attack is an Attack being performed, so the trigger still fires - and
        /// what the page forbids is carried beside it, which is exactly the reading
        /// <c>PerformedBasicAction</c> already settled.
        /// </summary>
        [Test]
        public void ARepeatedAttack_StillRecordsTheFactButOffersNoSecondRepeat()
        {
            var first = PerformedAttackAction.ForCompletedAction(
                Min, World.SomeCardInstanceId, World.SomeActionDefinitionId, ActionKind.Attack);
            var repeated = PerformedAttackAction.ForCompletedAction(
                Min, World.SomeCardInstanceId, World.SomeActionDefinitionId, ActionKind.Attack,
                wasRepeatedInstance: true);

            Assert.That(first.CanBeRepeated, Is.True);
            Assert.That(repeated.IsDefined, Is.True, "The trigger still fires after a repeat.");
            Assert.That(repeated.CanBeRepeated, Is.False, "And finds nothing it may repeat.");

            Assert.That(
                PendingAfterActionRepeat.For(first, World.RepeatResolutionId), Is.Not.Null);
            Assert.That(
                PendingAfterActionRepeat.For(repeated, World.RepeatResolutionId), Is.Null,
                "A repeated instance produces no subject to hold, so the node skips in silence.");
        }

        /// <summary>
        /// The subject the attack fact builds is indistinguishable from the one a basic Attack builds -
        /// same card, same action, same kind - so the same resolver starts the second instance and a
        /// basic Attack cannot get two different ones depending on which ability read it.
        /// </summary>
        [Test]
        public void TheRepeatSubject_IsTheSameWhicheverFactNamedTheAttack()
        {
            var attack = PerformedAttackAction.ForCompletedAction(
                Min, World.SomeCardInstanceId, World.SomeActionDefinitionId, ActionKind.Attack);
            var basic = PerformedBasicAction.Of(
                Min, World.SomeCardInstanceId, World.SomeActionDefinitionId, ActionKind.Attack);

            var fromAttack = PendingAfterActionRepeat.For(attack, World.RepeatResolutionId);
            var fromBasic = PendingAfterActionRepeat.For(basic, World.RepeatResolutionId);

            Assert.That(fromAttack.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(fromAttack.Kind, Is.EqualTo(fromBasic.Kind));
            Assert.That(
                fromAttack.SourceCardInstanceId, Is.EqualTo(fromBasic.SourceCardInstanceId));
            Assert.That(fromAttack.ActionDefinitionId, Is.EqualTo(fromBasic.ActionDefinitionId));
            Assert.That(fromAttack.ActorId, Is.EqualTo(fromBasic.ActorId));
        }

        /// <summary>
        /// The derivation fires after her basic Attack AND after a coloured one, which is the whole
        /// reason the condition exists: p.9's basic supertype is a fact about the card, and five of her
        /// six Attack faces are not basic cards.
        /// </summary>
        [Test]
        public void FlurryOfBlows_FiresAfterABasicAttackAndAfterAColouredOne()
        {
            var world = World.Create();
            world.UnlockFlurryOfBlows();

            Assert.That(
                world.TriggeredSourcesAfterAttackOn(World.BasicCard).Count, Is.EqualTo(1),
                "A basic Attack.");
            Assert.That(
                world.TriggeredSourcesAfterAttackOn(World.ColouredCard).Count, Is.EqualTo(1),
                "A coloured Attack, which no earlier condition could see at all.");
        }

        /// <summary>
        /// And after nothing else. A Skill, a Movement and a Defense are not attack actions however
        /// they are performed, and a Clear taken instead of the Attack is p.13's own replacement with
        /// its own kind.
        /// </summary>
        [Test]
        public void FlurryOfBlows_FiresAfterNoOtherKindOfAction()
        {
            var world = World.Create();
            world.UnlockFlurryOfBlows();

            foreach (var kind in new[]
            {
                ActionKind.Skill, ActionKind.Movement, ActionKind.Defense,
                ActionKind.Hold, ActionKind.FastTravel, ActionKind.Clear
            })
            {
                Assert.That(
                    world.TriggeredSourcesAfter(World.BasicCard, kind), Is.Empty, kind.ToString());
            }
        }

        /// <summary>
        /// "YOU perform an attack action": an enemy's Attack is not hers, and p.16 ends a permanent
        /// ability while its hero is off the board.
        /// </summary>
        [Test]
        public void FlurryOfBlows_FiresForItsOwnHeroOnTheBoardAndForNobodyElse()
        {
            var world = World.Create();
            world.UnlockFlurryOfBlows();

            Assert.That(
                world.TriggeredSourcesAfterTheEnemysAttack(), Is.Empty,
                "The clause says \"you\".");
            Assert.That(
                world.TriggeredSourcesAfterAttackWithMinOffTheBoard(), Is.Empty,
                "p.16: the ability applies as long as your hero is on the board.");
        }

        /// <summary>
        /// The new fact reaches the new condition and no other. Tigerclaw's Cloak and Daggers reads
        /// p.9's basic supertype and is unchanged by an attack fact arriving beside it - which is the
        /// regression the whole shape of this change exists to avoid.
        /// </summary>
        [Test]
        public void TheAttackFact_ChangesNothingAboutTheConditionsWrittenBeforeIt()
        {
            var world = World.Create();
            world.UnlockFlurryOfBlows();
            world.UnlockCloakAndDaggersForTheEnemy();

            var afterHerColouredAttack = world.TriggeredSourcesAfterAttackOn(World.ColouredCard);

            Assert.That(afterHerColouredAttack.Count, Is.EqualTo(1));
            Assert.That(
                afterHerColouredAttack.Single().ControllerHeroId, Is.EqualTo(Min),
                "Only the hero who acted is derived a source, and only her condition matched.");
            Assert.That(
                world.CloakAndDaggersFiresAfterTheEnemysBasicSkill(), Is.True,
                "Tigerclaw's own condition still fires on the fact it has always read.");
        }

        /// <summary>
        /// End to end through the trigger authority: her unlocked Ultimate fires at p.8's Action Ended
        /// checkpoint and opens the repeat decision, carrying the attack it fired for - and the
        /// repeated instance's own firing finds nothing to offer, which is p.10's rule enforced on the
        /// subject rather than on the trigger.
        /// </summary>
        [Test]
        public void FlurryOfBlows_OpensTheRepeatDecision_AndNotAfterTheRepeatItself()
        {
            var world = World.Create();
            world.UnlockFlurryOfBlows();

            var opened = world.RunTheTriggerAfterAttack(wasRepeatedInstance: false);
            Assert.That(
                opened, Is.Not.Null,
                "\"you may repeat it once\" is a decision, so the frame pauses on it.");
            Assert.That(opened.ChooserId, Is.EqualTo(Min));

            Assert.That(
                world.RunTheTriggerAfterAttack(wasRepeatedInstance: true), Is.Null,
                "p.10: you cannot repeat a repeated action, so the node finds nothing and skips.");
        }

        // -------------------------------------------------------------------------------------------
        // The deferral, from both sides
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate. Nothing of Min's is registered until 8.61j deals her: the build's
        /// catalog names none of her capabilities and none of the contract this block minted, and the
        /// deferred lists name all three. Asserted from both sides, because a list that only said what
        /// is deferred could not notice a registration arriving early.
        /// </summary>
        [Test]
        public void EverythingThisBlockMints_IsDeferredUntilTheDealing()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            foreach (var reference in new[]
            {
                MinComponentContent.InnerStrengthDoubleItemBonusesV1,
                MinComponentContent.PerfectSelfDoubleItemBonusesV1,
                SyntheticRosterCapabilities.TakeDeckCardAsItemV1
            })
            {
                Assert.That(
                    registered.TryLoad(reference, out _), Is.False,
                    $"{reference} is reachable from no card a configured match deals.");
            }

            Assert.That(MinComponentContent.DisciplineRegisteredHandlers(), Is.Empty);
            Assert.That(
                MinComponentContent.DisciplineDeferredHandlers().Select(handler => handler.Reference),
                Is.EquivalentTo(new[]
                {
                    MinComponentContent.InnerStrengthDoubleItemBonusesV1,
                    MinComponentContent.PerfectSelfDoubleItemBonusesV1
                }));
            Assert.That(
                MinComponentContent.DisciplineHandlers().Count(), Is.EqualTo(2),
                "Registered plus deferred is everything this file declares.");

            Assert.That(
                SyntheticRosterCapabilities.RegisteredHandlers()
                    .Select(handler => handler.Reference),
                Has.No.Member(SyntheticRosterCapabilities.TakeDeckCardAsItemV1));
            Assert.That(
                SyntheticRosterCapabilities.DeferredHandlers().Select(handler => handler.Reference),
                Has.Member(SyntheticRosterCapabilities.TakeDeckCardAsItemV1));
        }

        /// <summary>
        /// And her Ultimate's own triggered effect is not in the build's triggered-effect catalog
        /// either, which is the other half of the same gate: a registered effect must be reachable from
        /// a card a configured match puts beside a dashboard.
        /// </summary>
        [Test]
        public void HerUltimatesTriggeredEffect_IsNotInTheBuildsOwnCatalog()
        {
            var registered = MatchContentCatalogs.ForThisBuild().TriggeredEffects;
            var flurry = MinEffectContent
                .FlurryOfBlowsTriggeredEffect(MinCardContent.DisciplineRevision).Reference;

            Assert.That(
                registered.TryGet(flurry, out _), Is.False,
                "No dealt card of hers exists to reach it, and 7.9's gate refuses a registration " +
                "nothing can start.");
        }

        /// <summary>
        /// The three printed faces, transcribed. Every value here is the shard's row, and a missing
        /// icon is absent rather than zero (MI-05).
        /// </summary>
        [Test]
        public void TheThreeComposedFaces_CarryTheirPrintedValues()
        {
            var innerStrength = World.DefinitionOf(MinCardContent.InnerStrength);
            Assert.That(innerStrength.Initiative.Value, Is.EqualTo(2));
            Assert.That(innerStrength.Progression.Color, Is.EqualTo(CardColor.Green));
            Assert.That(innerStrength.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(
                innerStrength.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(
                innerStrength.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill)
                    .BaseValues,
                Is.Empty,
                "MI-05: it prints neither a Range nor a Radius icon.");

            var perfectSelf = World.DefinitionOf(MinCardContent.PerfectSelf);
            Assert.That(perfectSelf.Initiative.Value, Is.EqualTo(1));
            Assert.That(perfectSelf.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(perfectSelf.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Range));

            foreach (var definition in new[] { innerStrength, perfectSelf })
            {
                Assert.That(
                    definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                        .BaseValues.Single().Amount,
                    Is.EqualTo(definition.Id.Equals(MinCardContent.InnerStrength) ? 2 : 3));
                Assert.That(
                    definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement)
                        .BaseValues.Single().Amount,
                    Is.EqualTo(3));
            }
        }

        // -------------------------------------------------------------------------------------------
        // The board and the content these cases are asked over
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Min and one enemy hero on a synthetic hex board, her three composed faces plus Rowenna's and
        /// Tigerclaw's composed sets in card instances, and the capability catalog this block's
        /// deferred handlers compose onto. Everything a case asks is a production query or a production
        /// frame over exactly these, so no case depends on anything being registered.
        /// </summary>
        private sealed class World
        {
            internal static readonly CardInstanceId SomeCardInstanceId =
                new CardInstanceId("min-flurry-probe-card");

            internal static readonly ActionDefinitionId SomeActionDefinitionId =
                new ActionDefinitionId("primary-attack");

            internal static readonly ActionResolutionId RepeatResolutionId =
                new ActionResolutionId("min-flurry-probe-repeat");

            /// <summary>A basic card of hers - Rowenna's Gold, standing in until 8.61i composes Min's.</summary>
            internal static readonly CardDefinitionId BasicCard = RowennaCardContent.CodeOfChivalry;

            /// <summary>A coloured Attack card, which p.9's basic supertype does not reach.</summary>
            internal static readonly CardDefinitionId ColouredCard = RowennaCardContent.ParagonOfGrace;

            private static readonly MapId Map = new MapId("min-items-and-flurry-map");
            private static readonly ZoneId Zone = new ZoneId("min-items-and-flurry-zone");

            /// <summary>
            /// An Active effect whose scope is a disk of printed Radius 2 around its controller, drawn
            /// around the enemies inside it. It is a fixture definition rather than a shipped one
            /// because what is being asked is p.16's arithmetic on a PRINTED Radius icon, which is
            /// every such effect's reading and not one card's: the shipped definitions that declare a
            /// disk each activate at a checkpoint of their own, and borrowing one would have made this
            /// case about that card's timing.
            /// </summary>
            private static readonly ActiveEffectDefinition RadiusProbeDefinition =
                new ActiveEffectDefinition(
                    "min-radius-probe-effect",
                    ActiveEffectAffectedScopeKind.HeroesInRadius,
                    ActiveEffectDurationKind.ThisRound,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.RoundEnd,
                    new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated },
                    false,
                    "MinRadiusProbe",
                    "min-radius-probe",
                    Array.Empty<TimingCheckpoint>(),
                    new[] { RuleModification.ForRestriction(RuleActivity.Movement) },
                    null,
                    DeclaredRadius.PrintedIcon(2),
                    SourceTeamRelation.OpposingSource);

            private static readonly IReadOnlyList<CardDefinition> AllDefinitions =
                MinCardContent.DisciplineDefinitions
                    .Concat(RowennaCardContent.AllDefinitions)
                    .Concat(TigerclawCardContent.AllDefinitions)
                    .ToArray();

            private readonly MatchParticipants participants = StagedBoardParticipants.Create();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly BoardMapDefinition map = BuildMap();
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly HeroItemState heroItems;
            private readonly EffectCapabilityCatalog capabilities;
            private HexCoordinate minAt = MinAt;
            private int createdEffects;

            private World()
            {
                heroItems = new HeroItemState(participants);
                capabilities = SyntheticEffectCapabilities.CreateCatalog(
                    SyntheticRosterCapabilities.DeferredHandlers()
                        .Concat(MinComponentContent.DisciplineDeferredHandlers()));

                // Her hand holds the two composed Green Skills and one Tier II card of another colour;
                // everything else of hers sits in the deck, which is where p.6 leaves it and where
                // Perfect Self's second bullet reads.
                foreach (var cardId in new[]
                {
                    MinCardContent.InnerStrength,
                    MinCardContent.PerfectSelf,
                    RowennaCardContent.FairShare
                })
                {
                    instances.Add(CardInstance.CreateInHand(
                        Instance(Min, cardId), Reference(cardId), Min));
                }

                foreach (var cardId in AllDefinitions
                    .Select(definition => definition.Id)
                    .Where(cardId => !instances.Any(card =>
                        card.OwnerId.Equals(Min) &&
                        card.DefinitionReference.DefinitionId.Equals(cardId))))
                {
                    instances.Add(CardInstance.CreateInDeck(
                        Instance(Min, cardId), Reference(cardId), Min));
                }

                // The enemy seat holds a basic Skill and the Ultimate that fires after one, so the
                // regression case can ask whether an older condition still reads its own fact.
                foreach (var cardId in new[]
                {
                    TigerclawCardContent.BlendIntoShadows,
                    TigerclawCardContent.CloakAndDaggers,
                    RowennaCardContent.CodeOfChivalry,

                    // Printed Initiative 3, one above Inner Strength's 2: one item of Min's ties the
                    // two and the doubled item puts her ahead, which is what p.7's order is asked.
                    RowennaCardContent.OpeningVolley
                })
                {
                    instances.Add(CardInstance.CreateInDeck(
                        Instance(Enemy, cardId), Reference(cardId), Enemy));
                }
            }

            internal static World Create() => new World();

            internal static CardDefinition DefinitionOf(CardDefinitionId cardId) =>
                AllDefinitions.Single(definition => definition.Id.Equals(cardId));

            internal static ValueResult PrintedInitiative(int amount) =>
                new ValueResult(
                    PrintedValueChannel.Initiative,
                    new[]
                    {
                        new ValueContribution(
                            ValueContributionKind.PrintedIcon,
                            SomeCardInstanceId,
                            SomeActionDefinitionId,
                            PrintedValueChannel.Initiative,
                            amount)
                    });

            // ----- items -----

            internal void EarnItems(params PrintedValueChannel[] icons)
            {
                foreach (var icon in icons)
                {
                    Assert.That(
                        heroItems.TryGrantItemFromTheBox(Min, icon).IsPlaced, Is.True,
                        $"p.16's dashboard accepts a {icon} item.");
                }
            }

            internal void EarnOneItemOfEveryIcon() => EarnItems(
                PrintedValueChannel.Attack,
                PrintedValueChannel.Defense,
                PrintedValueChannel.Movement,
                PrintedValueChannel.Range,
                PrintedValueChannel.Radius,
                PrintedValueChannel.Initiative);

            internal IReadOnlyList<HeroItem> Items(HeroId heroId) => heroItems.GetItems(heroId);

            // ----- the doubling -----

            /// <summary>
            /// Inner Strength's Skill, run through the production effect frame over the catalog this
            /// fixture composed - so the Active effect is created by the capability the card names
            /// rather than by a line written here.
            /// </summary>
            internal void PlayInnerStrength() =>
                RunSkill(
                    MinCardContent.InnerStrength,
                    MinEffectContent.InnerStrengthSkillEffect(MinCardContent.DisciplineRevision),
                    Array.Empty<string>());

            /// <summary>
            /// The second doubling declaration standing beside the first - the case p.15 makes
            /// impossible at the table and which this build must still answer as a doubling.
            /// </summary>
            internal void PlayPerfectSelfDoublingDirectly() =>
                RunSkill(
                    MinCardContent.PerfectSelf,
                    MinEffectContent.PerfectSelfSkillEffect(MinCardContent.DisciplineRevision),
                    new[] { MinEffectContent.PerfectSelfDoubleArmNode });

            internal int LiveEffectCount(string effectKind) => timing.Effects.Count(effect =>
                effect.Phase == ActiveEffectPhase.Active &&
                string.Equals(effect.Definition.EffectKind, effectKind, StringComparison.Ordinal));

            internal void EndTheRound() => timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.RoundEnd, 1, 1),
                new TieBreakerState(TeamSide.Red));

            internal RuleModificationSet Modifications() => Build(Board());

            // ----- the eight readings -----

            internal ValueResult AttackValueOf(CardDefinitionId cardId) => AttackValueQuery.Calculate(
                AttackOffer(cardId), AllDefinitions, heroItems, Modifications());

            internal int AttackTotalOf(CardDefinitionId cardId) => AttackValueOf(cardId).Total;

            internal int RadiusTotalOf(CardDefinitionId cardId) => RadiusValueQuery
                .Calculate(AttackOffer(cardId), AllDefinitions, heroItems, Modifications()).Total;

            internal int RangeTotalOf(CardDefinitionId cardId) => RangeValueQuery
                .Calculate(
                    cardId.Equals(RowennaCardContent.ProtectTheWeak)
                        ? SkillOffer(cardId, RowennaCardContent.PrimarySkill)
                        : AttackOffer(cardId),
                    AllDefinitions,
                    heroItems,
                    Modifications())
                .Total;

            internal int MovementTotalOfInnerStrength() => MovementValueQuery
                .Calculate(
                    SecondaryOffer(
                        MinCardContent.InnerStrength,
                        MinCardContent.DisciplineSecondaryMovement,
                        ActionKind.Movement),
                    AllDefinitions,
                    heroItems,
                    Modifications())
                .Total;

            internal int DefenseTotalOfInnerStrength() => DefenseValueQuery
                .Calculate(
                    SecondaryOffer(
                        MinCardContent.InnerStrength,
                        MinCardContent.DisciplineSecondaryDefense,
                        ActionKind.Defense),
                    AllDefinitions,
                    Min,
                    participants,
                    Board(),
                    Modifications(),
                    heroItems)
                .Total;

            internal int InitiativeTotalOfInnerStrength() => InitiativeCandidates()
                .Single(candidate => candidate.Key.Equals(Min))
                .Value;

            internal bool HighestInitiativeCandidateIsMin()
            {
                var candidates = InitiativeCandidates();
                var highest = candidates.Max(candidate => candidate.Value);
                return candidates.Count(candidate => candidate.Value == highest) == 1 &&
                    candidates.Single(candidate => candidate.Value == highest).Key.Equals(Min);
            }

            /// <summary>
            /// One channel's total on whichever card of Min's prints that icon, so the case that reads
            /// the item Perfect Self made need not know which icon it turned out to be.
            /// </summary>
            internal int TotalOnChannel(PrintedValueChannel channel) =>
                channel == PrintedValueChannel.Attack ? AttackTotalOf(ColouredCard) :
                channel == PrintedValueChannel.Radius ? RadiusTotalOf(ColouredCard) :
                channel == PrintedValueChannel.Range
                    ? RangeTotalOf(RowennaCardContent.ProtectTheWeak)
                    : channel == PrintedValueChannel.Movement
                        ? MovementTotalOfInnerStrength()
                        : channel == PrintedValueChannel.Defense
                            ? DefenseTotalOfInnerStrength()
                            : InitiativeTotalOfInnerStrength();

            internal int PrintedOnChannel(PrintedValueChannel channel) =>
                channel == PrintedValueChannel.Attack ? PrintedAttack :
                channel == PrintedValueChannel.Radius ? PrintedRadius :
                channel == PrintedValueChannel.Range ? PrintedRange :
                channel == PrintedValueChannel.Movement ? PrintedMovement :
                channel == PrintedValueChannel.Defense ? PrintedDefense : InnerStrengthInitiative;

            /// <summary>
            /// Whether an Active effect whose printed Radius is 2 reaches an enemy standing
            /// <paramref name="distance"/> spaces away - the disk the modification set itself measures,
            /// which is where the doubling has to be read off the lifecycle rather than off a set.
            /// </summary>
            internal bool PetrifiedEnemiesWithin(int distance)
            {
                var probe = new ActiveEffectLifecycle();
                probe.Create(
                    new ActiveEffectId("min-radius-probe"),
                    RadiusProbeDefinition,
                    Min,
                    TeamSide.Blue,
                    default,
                    Instance(Min, MinCardContent.InnerStrength),
                    MinCardContent.DisciplinePrimarySkill,
                    1,
                    1);
                foreach (var standing in timing.Effects.Where(effect =>
                    effect.Phase == ActiveEffectPhase.Active))
                {
                    probe.Create(
                        new ActiveEffectId("min-radius-probe-" + standing.Id.Value),
                        standing.Definition,
                        standing.ControllerHeroId,
                        standing.ControllerTeamSide,
                        default,
                        standing.SourceCardInstanceId,
                        standing.SourceActionDefinitionId,
                        1,
                        1);
                }

                probe.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(TimingCheckpoint.ActionStarted, 1, 1),
                    new TieBreakerState(TeamSide.Red));

                var board = new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Min, minAt),
                        new HeroOccupancy(Enemy, new HexCoordinate(distance, 0))
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                return RuleModificationQuery
                    .Build(
                        probe,
                        board,
                        participants,
                        new TieBreakerState(TeamSide.Red),
                        UnlockedUltimateSet.None,
                        RuleObject.None,
                        heroItems,
                        null)
                    .For(RuleObject.ForHero(Enemy))
                    .Any();
            }

            /// <summary>The badge's own reading, from the facts a seat holds.</summary>
            internal int ProjectedBonusPerItem() =>
                GuardsDigital.Presentation.Match.ActiveEffectVisibility.ItemBonusPerItem(
                    Min, ActiveEffectSourceCardIds(), instances, AllDefinitions, capabilities);

            internal int ProjectedBonusPerItemWithoutACatalog() =>
                GuardsDigital.Presentation.Match.ActiveEffectVisibility.ItemBonusPerItem(
                    Min, ActiveEffectSourceCardIds(), instances, AllDefinitions, null);

            // ----- Perfect Self -----

            internal IReadOnlyList<CardInstanceId> PerfectSelfCandidates() =>
                ((IEffectSelectionCapabilityHandler)TakeDeckCardHandler())
                    .AnticipateCandidates(TakeDeckCardContext())
                    .Select(value => value.CardInstanceId)
                    .ToArray();

            internal bool PerfectSelfTakeArmIsFeasible() =>
                TakeDeckCardHandler().Anticipate(TakeDeckCardContext()) !=
                EffectExecutionResultKind.ImpossibleMandatory;

            internal bool PerfectSelfDoubleArmIsFeasible() =>
                capabilities.TryLoad(
                    MinComponentContent.PerfectSelfDoubleItemBonusesV1, out var handler) &&
                handler.Anticipate(TakeDeckCardContext()) !=
                EffectExecutionResultKind.ImpossibleMandatory;

            internal EffectChoiceRequest PerfectSelfDeckChoiceRequest()
            {
                var result = TakeDeckCardHandler().Execute(TakeDeckCardContext());
                Assert.That(
                    result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                    "p.10 gives the decision to the hero performing the card.");
                return result.Choice;
            }

            internal bool PerfectSelfAnswerIsAccepted(CardInstanceId cardId) =>
                TakeDeckCardHandler()
                    .Resume(TakeDeckCardContext(), new[] { EffectValue.Card(cardId) })
                    .Kind == EffectExecutionResultKind.Applied;

            /// <summary>
            /// Perfect Self run through the production frame, taking the named bullets in the order
            /// given, and answering the deck choice with the first card the offer admits. Returns the
            /// card that became the item, or the default when the deck bullet was not taken.
            /// </summary>
            internal CardInstanceId ResolvePerfectSelf(params string[] armNodeIds) =>
                RunSkill(
                    MinCardContent.PerfectSelf,
                    MinEffectContent.PerfectSelfSkillEffect(MinCardContent.DisciplineRevision),
                    armNodeIds);

            internal IReadOnlyList<CardInstanceId> DeckCardIds() => instances
                .Where(card => card.OwnerId.Equals(Min) && card.State.Location == CardLocation.Deck)
                .Select(card => card.Id)
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray();

            internal IReadOnlyList<CardInstanceId> EnemyDeckCardIds() => instances
                .Where(card => card.OwnerId.Equals(Enemy))
                .Select(card => card.Id)
                .ToArray();

            /// <summary>
            /// The Tier II card this fixture starts in her hand - a card in a hand is not a card in a
            /// deck, which is the negative the sentence draws.
            /// </summary>
            internal CardInstanceId TierTwoCardInHand()
            {
                var card = CardOf(Instance(Min, MinCardContent.InnerStrength));
                Assert.That(DefinitionOf(card).Progression.Tier, Is.EqualTo(CardTier.II));
                Assert.That(card.State.Location, Is.EqualTo(CardLocation.Hand));
                return card.Id;
            }

            internal CardTier TierOf(CardInstanceId cardId) =>
                DefinitionOf(CardOf(cardId)).Progression.Tier;

            internal PrintedValueChannel ItemIconOf(CardInstanceId cardId) =>
                DefinitionOf(CardOf(cardId)).Progression.ItemIcon;

            internal CardLocation LocationOf(CardInstanceId cardId) => CardOf(cardId).State.Location;

            internal HeroId OwnerOf(CardInstanceId cardId) => CardOf(cardId).OwnerId;

            /// <summary>
            /// p.15's Tier III upgrade, which "returns it to the deck" - the case MI-Q5 asks about,
            /// staged by moving a Tier II instance back into the deck the way the page does.
            /// </summary>
            internal CardInstanceId ReturnATierTwoCardToTheDeckByUpgrading()
            {
                var card = CardOf(Instance(Min, RowennaCardContent.FairShare));
                Assert.That(DefinitionOf(card).Progression.Tier, Is.EqualTo(CardTier.II));
                Assert.That(CardLifecycleAuthority.TryReturnToDeck(card), Is.True);
                return card.Id;
            }

            internal void EmptyTheDeckOfTierTwoCards()
            {
                foreach (var card in instances
                    .Where(card => card.OwnerId.Equals(Min) &&
                        card.State.Location == CardLocation.Deck &&
                        DefinitionOf(card).Progression.Tier == CardTier.II)
                    .ToArray())
                {
                    Assert.That(CardLifecycleAuthority.TryTakeFromDeckIntoHand(card), Is.True);
                }
            }

            // ----- Flurry of Blows -----

            internal void UnlockFlurryOfBlows() => Unlock(Min, MinCardContent.FlurryOfBlows);

            internal void UnlockCloakAndDaggersForTheEnemy() =>
                Unlock(Enemy, TigerclawCardContent.CloakAndDaggers);

            internal IReadOnlyList<ContinuousTriggeredEffectSource> TriggeredSourcesAfterAttackOn(
                CardDefinitionId cardId) => Derive(Min, cardId, ActionKind.Attack);

            internal IReadOnlyList<ContinuousTriggeredEffectSource> TriggeredSourcesAfter(
                CardDefinitionId cardId, ActionKind kind) => Derive(Min, cardId, kind);

            internal IReadOnlyList<ContinuousTriggeredEffectSource>
                TriggeredSourcesAfterTheEnemysAttack() =>
                Derive(Enemy, RowennaCardContent.CodeOfChivalry, ActionKind.Attack);

            internal IReadOnlyList<ContinuousTriggeredEffectSource>
                TriggeredSourcesAfterAttackWithMinOffTheBoard()
            {
                var board = new AdjacentMoveAuthority(
                    map,
                    new[] { new HeroOccupancy(Enemy, EnemyAt) },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                return UltimateTriggeredEffectSourceQuery.Derive(
                    UltimateUnlockQuery.Derive(AllDefinitions, instances),
                    board,
                    participants,
                    default,
                    default,
                    default,
                    AttackFact(Min, BasicCard, false));
            }

            internal bool CloakAndDaggersFiresAfterTheEnemysBasicSkill() =>
                UltimateTriggeredEffectSourceQuery.Derive(
                        UltimateUnlockQuery.Derive(AllDefinitions, instances),
                        Board(),
                        participants,
                        PerformedBasicAction.Of(
                            Enemy,
                            Instance(Enemy, TigerclawCardContent.BlendIntoShadows),
                            SomeActionDefinitionId,
                            ActionKind.Skill))
                    .Any(source => source.ControllerHeroId.Equals(Enemy));

            /// <summary>
            /// The trigger run through the production authority, which is what turns a derived source
            /// into the decision the player answers. Returns the opened choice, or <c>null</c> when the
            /// node found nothing to offer.
            /// </summary>
            internal PendingEffectChoice RunTheTriggerAfterAttack(bool wasRepeatedInstance)
            {
                var authority = new TriggeredEffectResolutionAuthority(
                    new TriggeredEffectCatalog(new[]
                    {
                        MinEffectContent.FlurryOfBlowsTriggeredEffect(
                            MinCardContent.DisciplineRevision)
                    }),
                    capabilities);
                authority.ApplyCheckpoint(
                    new ActiveEffectLifecycle(),
                    new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                    new TieBreakerState(TeamSide.Red),
                    AllDefinitions,
                    instances,
                    participants,
                    RuleModificationSet.Empty,
                    heroItems,
                    UltimateTriggeredEffectSourceQuery.Derive(
                        UltimateUnlockQuery.Derive(AllDefinitions, instances),
                        Board(),
                        participants,
                        default,
                        default,
                        default,
                        AttackFact(Min, ColouredCard, wasRepeatedInstance)),

                    // The board would admit the second instance; what this case is about is whether
                    // the node is offered at all.
                    _ => true);
                return authority.PendingResolution?.PendingChoice;
            }

            // ----- composition -----

            private IReadOnlyList<ContinuousTriggeredEffectSource> Derive(
                HeroId actorId, CardDefinitionId cardId, ActionKind kind) =>
                UltimateTriggeredEffectSourceQuery.Derive(
                    UltimateUnlockQuery.Derive(AllDefinitions, instances),
                    Board(),
                    participants,
                    default,
                    default,
                    default,
                    PerformedAttackAction.ForCompletedAction(
                        actorId, Instance(actorId, cardId), SomeActionDefinitionId, kind));

            private PerformedAttackAction AttackFact(
                HeroId actorId, CardDefinitionId cardId, bool wasRepeatedInstance) =>
                PerformedAttackAction.ForCompletedAction(
                    actorId,
                    Instance(actorId, cardId),
                    SomeActionDefinitionId,
                    ActionKind.Attack,
                    wasRepeatedInstance);

            private void Unlock(HeroId heroId, CardDefinitionId cardId)
            {
                var card = CardOf(Instance(heroId, cardId));
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(card), Is.True);
            }

            private IEffectCapabilityHandler TakeDeckCardHandler()
            {
                Assert.That(
                    capabilities.TryLoad(
                        SyntheticRosterCapabilities.TakeDeckCardAsItemV1, out var handler),
                    Is.True,
                    "The fixture composes the deferred handler onto its own catalog.");
                return handler;
            }

            private EffectCapabilityContext TakeDeckCardContext() =>
                new EffectCapabilityContext(
                    new EffectNodeId(MinEffectContent.PerfectSelfTakeArmNode),
                    EffectFailurePolicy.Mandatory,
                    Min,
                    Min,
                    Instance(Min, MinCardContent.PerfectSelf),
                    Reference(MinCardContent.PerfectSelf),
                    MinCardContent.DisciplinePrimarySkill,
                    new Dictionary<string, EffectValue>
                    {
                        { "hero", EffectValue.Hero(Min) },
                        {
                            SyntheticRosterCapabilities.ItemCardTierParameter,
                            EffectValue.Integer((int)CardTier.II)
                        }
                    },
                    1,
                    1,
                    participants,
                    timing,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(AllDefinitions, instances),
                    ActionKind.Skill,
                    heroItems: heroItems);

            /// <summary>
            /// One of her Skills run through the production frame: start it, answer the modal with the
            /// arms named, answer the deck choice with the first card the offer admits. Returns the card
            /// that became an item, or the default when none did.
            /// </summary>
            private CardInstanceId RunSkill(
                CardDefinitionId cardId, EffectDefinition effect, IReadOnlyList<string> armNodeIds)
            {
                var cardInstanceId = Instance(Min, cardId);
                var context = new EffectResolutionContext(
                    effect,
                    capabilities,
                    participants,
                    timing,
                    1,
                    1,
                    Modifications(),
                    EffectCardState.Of(AllDefinitions, instances),
                    ActionKind.Skill,
                    heroItems: heroItems);
                var step = EffectResolutionSession.Start(
                    new EffectResolutionId($"min-discipline-{++createdEffects}"),
                    cardInstanceId,
                    Reference(cardId),
                    MinCardContent.DisciplinePrimarySkill,
                    Min,
                    Min,
                    ActionInvocationContext.PrintedPrimary,
                    context);
                Assert.That(step.Accepted, Is.True);

                var taken = default(CardInstanceId);
                var frame = step.Frame;
                var guard = 0;
                while (frame != null)
                {
                    Assert.That(++guard, Is.LessThan(8), "A composed Skill settles in a few answers.");
                    var choice = frame.PendingChoice;
                    var picked = choice.Candidates
                        .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Option)
                        .Any()
                        ? armNodeIds
                            .Select(nodeId => choice.Candidates.Single(candidate =>
                                candidate.Kind == EffectChoiceCandidateKind.Option &&
                                candidate.OptionNodeId.Value == nodeId))
                            .ToArray()
                        : new[] { choice.Candidates.First() };

                    if (picked.Any(candidate =>
                        candidate.Kind == EffectChoiceCandidateKind.Value &&
                        candidate.Value.Kind == EffectValueKind.CardInstance))
                    {
                        taken = picked[0].Value.CardInstanceId;
                    }

                    foreach (var candidate in picked)
                    {
                        var selected = EffectResolutionSession.Answer(
                            frame,
                            new EffectChoiceCommand(
                                choice.ResolutionId, choice.ChoiceId,
                                EffectChoiceCommandKind.Select, candidate, choice.ChooserId),
                            context);
                        Assert.That(selected.Accepted, Is.True, selected.RejectionReason.ToString());
                        frame = selected.Frame;
                        if (frame == null || !frame.IsAwaitingChoice ||
                            !frame.PendingChoice.ChoiceId.Equals(choice.ChoiceId))
                        {
                            break;
                        }

                        choice = frame.PendingChoice;
                    }

                    if (frame == null || !frame.IsAwaitingChoice ||
                        !frame.PendingChoice.ChoiceId.Equals(choice.ChoiceId))
                    {
                        continue;
                    }

                    var confirmed = EffectResolutionSession.Answer(
                        frame,
                        new EffectChoiceCommand(
                            choice.ResolutionId, choice.ChoiceId,
                            EffectChoiceCommandKind.Confirm, default, choice.ChooserId),
                        context);
                    Assert.That(confirmed.Accepted, Is.True, confirmed.RejectionReason.ToString());
                    frame = confirmed.Frame;
                }

                return taken;
            }

            private IReadOnlyList<KeyValuePair<HeroId, int>> InitiativeCandidates()
            {
                var modifications = Modifications();
                var results = new List<KeyValuePair<HeroId, int>>();
                foreach (var pair in new[]
                {
                    new KeyValuePair<HeroId, CardDefinitionId>(Min, MinCardContent.InnerStrength),
                    new KeyValuePair<HeroId, CardDefinitionId>(
                        Enemy, RowennaCardContent.OpeningVolley)
                })
                {
                    var printed = PrintedValueQuery.CalculatePrintedInitiative(
                        CardOf(Instance(pair.Key, pair.Value)), AllDefinitions);
                    results.Add(new KeyValuePair<HeroId, int>(
                        pair.Key,
                        ItemBonusQuery.Apply(printed, heroItems, pair.Key, modifications).Total));
                }

                return results;
            }

            private IReadOnlyList<CardInstanceId> ActiveEffectSourceCardIds() => timing.Effects
                .Where(effect => effect.Phase == ActiveEffectPhase.Active)
                .Select(effect => effect.SourceCardInstanceId)
                .Distinct()
                .ToArray();

            private RuleModificationSet Build(AdjacentMoveAuthority board) =>
                RuleModificationQuery.Build(
                    timing,
                    board,
                    participants,
                    new TieBreakerState(TeamSide.Red),
                    UltimateUnlockQuery.Derive(AllDefinitions, instances),
                    RuleObject.None,
                    heroItems,
                    null);

            private AdjacentMoveAuthority Board() => new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Min, minAt), new HeroOccupancy(Enemy, EnemyAt) },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            private ActionOffer AttackOffer(CardDefinitionId cardId) => new ActionOffer(
                Instance(Min, cardId),
                Reference(cardId),
                RowennaCardContent.PrimaryAttack,
                Min,
                ActionInvocationContext.PrintedPrimary,
                true,
                AttackRangeKind.NonRanged,
                default);

            private ActionOffer SkillOffer(CardDefinitionId cardId, ActionDefinitionId actionId) =>
                new ActionOffer(
                    Instance(Min, cardId),
                    Reference(cardId),
                    actionId,
                    Min,
                    ActionInvocationContext.PrintedPrimary,
                    true);

            private ActionOffer SecondaryOffer(
                CardDefinitionId cardId, ActionDefinitionId actionId, ActionKind kind) =>
                new ActionOffer(
                    Instance(Min, cardId),
                    Reference(cardId),
                    actionId,
                    Min,
                    kind == ActionKind.Defense
                        ? ActionInvocationContext.Reaction
                        : ActionInvocationContext.PrintedSecondary,
                    true);

            private CardInstance CardOf(CardInstanceId cardId) =>
                instances.Single(card => card.Id.Equals(cardId));

            private static CardDefinition DefinitionOf(CardInstance card) => AllDefinitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference));

            private static CardDefinitionReference Reference(CardDefinitionId cardId) =>
                DefinitionOf(cardId).Reference;

            private static CardInstanceId Instance(HeroId ownerId, CardDefinitionId cardId) =>
                new CardInstanceId($"{ownerId.Value}-{cardId.Value}");

            private static SpaceId SpaceIdAt(HexCoordinate at) =>
                new SpaceId($"q{at.Q}_r{at.R}");

            private static BoardMapDefinition BuildMap()
            {
                var spaces = new List<BoardMapSpace>();
                for (var q = -6; q <= 6; q++)
                {
                    for (var r = -6; r <= 6; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, MinAt) > 6)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            SpaceIdAt(coordinate), coordinate, BoardMapSpaceKind.Playable, Zone));
                    }
                }

                return new BoardMapDefinition(
                    Map, 1, "Min Items And Flurry Map",
                    spaces,
                    new[] { new BoardMapZone(Zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());
            }
        }

        /// <summary>
        /// MI-Q5's level-up: p.15's Upgrade a Card step composed directly over states this fixture
        /// owns, with one colour's ladder holding exactly ONE next-Tier candidate in the deck.
        ///
        /// <para>It uses Rowenna's composed Green ladder rather than Min's, because the rule is p.15's
        /// and is hero-neutral - and Min's own ladder is not whole until 8.61i names both Green
        /// pairs.</para>
        /// </summary>
        private sealed class OneCandidateLevelUp
        {
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly IReadOnlyList<CardDefinition> definitions =
                RowennaCardContent.AllDefinitions;
            private readonly MatchParticipants participants = StagedBoardParticipants.Create();
            private readonly List<HeroLevelUpOutcome> outcomes = new List<HeroLevelUpOutcome>();
            private readonly GuardsDigital.Core.Rules.Consequences.HeroCoinLedger coins;
            private readonly HeroLevelUpStepRules rules;
            private readonly SystemTransitionAuthority transitions;

            private OneCandidateLevelUp()
            {
                // Her Green Tier I in hand, and exactly ONE of its two Tier II candidates in the deck.
                instances.Add(CardInstance.CreateInHand(
                    Instance(RowennaCardContent.CloseQuarters),
                    Reference(RowennaCardContent.CloseQuarters),
                    Min));
                instances.Add(CardInstance.CreateInDeck(
                    Instance(RowennaCardContent.Melee), Reference(RowennaCardContent.Melee), Min));

                coins = new GuardsDigital.Core.Rules.Consequences.HeroCoinLedger(participants);
                var awarded = coins.Award(new GuardsDigital.Core.Rules.Consequences.DefeatOutcome(
                    new GuardsDigital.Core.Rules.Consequences.DefeatOutcomeId("mi-q5-award"),
                    new GuardsDigital.Core.Rules.Consequences.DefeatTargetId("mi-q5-target"),
                    participants.Teams.Single(team => !team.Id.Equals(
                        participants.GetTeamForHero(Min).Id)).Id,
                    Min,
                    HeroLevelSchedule.LevelUpCost(HeroLevel.Starting),
                    0));
                Assert.That(awarded.Accepted, Is.True, awarded.RejectionReason.ToString());

                Items = new HeroItemState(participants);
                rules = new HeroLevelUpStepRules(participants, definitions, instances);
                rules.Bind(new HeroProgressionState(participants), Items, coins);
                transitions = new SystemTransitionAuthority(
                    MatchSystemTransitions.CreateCatalog(),
                    MatchSystemTransitions.CreateRules(null, rules));
            }

            internal HeroItemState Items { get; }

            internal static OneCandidateLevelUp Create() => new OneCandidateLevelUp();

            internal IReadOnlyList<SystemTransitionChoiceCommand> Options =>
                transitions.PendingStep?.Invocations ?? Array.Empty<SystemTransitionChoiceCommand>();

            internal CardInstanceId TakenCardId => Instance(RowennaCardContent.Melee);

            internal int SetAsideCount => instances
                .Count(card => card.State.Location == CardLocation.SetAside);

            internal int ItemsGained => outcomes
                .Count(outcome => outcome.Kind == HeroLevelUpOutcomeKind.ItemGained);

            internal SystemTransitionResult Begin()
            {
                var result = transitions.TryBegin(
                    HeroLevelUpSystemTransition.DefinitionId,
                    new SystemTransitionInstanceId("mi-q5-level-up"));
                outcomes.AddRange(rules.DrainOutcomes());
                return result;
            }

            internal void Answer(int optionIndex)
            {
                var answered = transitions.TryAnswer(Options[optionIndex]);
                Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
                outcomes.AddRange(rules.DrainOutcomes());
            }

            internal CardLocation LocationOf(CardInstanceId cardId) => instances
                .Single(card => card.Id.Equals(cardId)).State.Location;

            private static CardInstanceId Instance(CardDefinitionId cardId) =>
                new CardInstanceId($"mi-q5-{cardId.Value}");

            private static CardDefinitionReference Reference(CardDefinitionId cardId) =>
                RowennaCardContent.AllDefinitions.Single(definition => definition.Id.Equals(cardId))
                    .Reference;
        }
    }
}
