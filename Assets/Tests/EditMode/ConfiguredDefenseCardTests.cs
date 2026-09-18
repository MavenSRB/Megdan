using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.11: Wasp's three Green cards resolve their complete printed text as rulebook p.14
    /// Block cards.
    ///
    /// <para>p.14 gives a Defense card two shapes, and until this milestone only one of them resolved.
    /// The numeric Defense calculates a total and enters the page's step 5 comparison; a Block prints
    /// a shield with an exclamation mark, no number at all, and resolves by a condition instead - "If
    /// the Block condition is fulfilled, you successfully defend, regardless of the attack value. If
    /// not, you are defeated." Milestone 5.8 reserved the seam, 8.3 declared the shape on these three
    /// faces, and this is where the condition is evaluated.</para>
    ///
    /// <para>All three print the same condition, "Block a ranged attack", so what these cases separate
    /// is the condition's two answers and the two riders that hang off its yes.</para>
    /// </summary>
    public sealed class ConfiguredDefenseCardTests
    {
        // ---------------------------------------------------------------------------------------
        // The declarations themselves
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AllThreeBlocks_PrintNoDefenseValueAndAreOfferedOnlyInReaction()
        {
            foreach (var cardId in new[]
                     {
                         WaspCardContent.StopProjectiles,
                         WaspCardContent.DeflectProjectiles,
                         WaspCardContent.ReflectProjectiles
                     })
            {
                var block = PrimaryOf(cardId, WaspCardContent.PrimaryBlock);

                Assert.That(block.DefenseResolution, Is.EqualTo(PrintedDefenseResolution.Block),
                    $"{cardId.Value} prints p.14's shield with an exclamation mark.");
                Assert.That(
                    block.BaseValues.Any(value => value.Channel == PrintedValueChannel.Defense),
                    Is.False,
                    "and no Defense number, which is what makes the condition the whole resolution.");
                Assert.That(block.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary),
                    "It is the card's printed primary, which is where the face prints it.");
                Assert.That(
                    block.SourceCardStateTransition.RequiredLocation, Is.EqualTo(CardLocation.Hand),
                    "p.14: a Defense action is performed \"by discarding exactly one card from your " +
                    "hand\", and a Block is a Defense action.");
                Assert.That(
                    block.SourceCardStateTransition.DestinationLocation,
                    Is.EqualTo(CardLocation.DiscardPile));
            }
        }

        /// <summary>
        /// p.14: "You cannot perform a Defense action on an already played card!" The Block is the
        /// card's printed primary, so the rule that keeps it out of a turn's own offers cannot be the
        /// origin. Two independent rules keep it out, and this case pins the outcome they produce
        /// together rather than pretending to separate them.
        ///
        /// <para>The first is the offer window: a printed Block is available in Reaction and nowhere
        /// else. The second is the state transition this milestone gives the Block - it requires the
        /// card in hand, and a card offering turn actions has already been played into the turn area.
        /// Either rule alone would produce the assertion below, which is why the assertion is written
        /// as the outcome and not as a claim about which rule did it. The window's other half - that
        /// the same Block *is* offered once an Attack is being answered - is what every case below
        /// stands on, so between them both halves are covered.</para>
        /// </summary>
        [Test]
        public void ABlock_IsNeverOfferedAsATurnActionAndNeverFromAPlayedCard()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            var red = HandCard(session, TeamSide.Red, WaspCardContent.Shock);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));

            // The played Block, asked of the same query a turn's offers are built from. p.14: "You
            // cannot perform a Defense action on an already played card!"
            var offers = ActionOfferQuery.GetAvailableTurnActionOffers(
                session.Projection.Definitions, session.Projection.Cards, blue.OwnerId, blue.Id);

            Assert.That(offers, Is.Not.Empty,
                "The played card still offers something, or this proves only that it offers nothing.");
            Assert.That(
                offers.Select(offer => ActionOf(session, offer))
                    .Any(action => action.DefenseResolution == PrintedDefenseResolution.Block),
                Is.False,
                "and what it does not offer is the Block, which is the card's own printed primary.");
            Assert.That(
                offers.Select(offer => ActionOf(session, offer)).Any(action =>
                    action.Kind == ActionKind.Movement),
                Is.True,
                "What is left is the secondary p.14 leaves takeable.");
        }

        /// <summary>
        /// Milestone 8.4's honesty rule, asked on the Defense path for the first time - the refusal
        /// case milestone 8.10 recorded as owed here.
        ///
        /// <para>Until this milestone no Defense action had ever carried card text, so nothing on
        /// p.14's step 3 asked whether a card's printed sentence could be carried out; Wasp's two Block
        /// riders are the first that could have been offered with the sentence silently dropped. The
        /// rule is the same one that greys a turn action, and p.14 makes refusing one card costless the
        /// same way p.12 does - a defender may always decline to defend.</para>
        ///
        /// <para>Its subject is synthetic, and deliberately so: after this milestone no verified
        /// Defense owes a composition, so a case built on Wasp's own cards would be asserting over
        /// nothing. The pair below is the discriminating one - the same card, offered when its text is
        /// composed and withheld when it is not.</para>
        /// </summary>
        [Test]
        public void ADefenseWhoseCardTextHasNoComposition_IsNotOfferedAsADefence()
        {
            var composed = DefenseCard(PrintedTextComposition.None);
            var owed = DefenseCard(PrintedTextComposition.AwaitedAt("some-later-milestone"));
            var defender = new HeroId("defense-honesty-hero");

            Assert.That(
                DefenseOfferQuery.Offers(
                    new[] { composed.Definition }, new[] { composed.Instance(defender) },
                    defender, RuleModificationSet.Empty),
                Is.Not.Empty,
                "A Defense whose printed text this build can carry out is offered.");

            Assert.That(
                DefenseOfferQuery.Offers(
                    new[] { owed.Definition }, new[] { owed.Instance(defender) },
                    defender, RuleModificationSet.Empty),
                Is.Empty,
                "and the same card is withheld while its text has no composition, rather than " +
                "performed with the sentence beside its values silently dropped.");
        }

        // ---------------------------------------------------------------------------------------
        // The condition, and its two answers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.11a, and a gap milestone 8.11 left: the defence *preview* the prompt is drawn
        /// from had never been asked about a Block.
        ///
        /// <para>Every offer was previewed by calculating a Defense value for it, which is exactly what
        /// a Block does not have. The prompt then looked for the offer's printed Defense icon to label
        /// it with, found none, and threw where it was drawn - so an attack answered by a Block asked
        /// the defender nothing at all and the match stopped. This is the case that says the preview
        /// describes a Block as a Block.</para>
        /// </summary>
        [Test]
        public void ABlockOffer_IsPreviewedAsABlockRatherThanAsADefenseValue()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out _);
            var defender = HeroOn(session, TeamSide.Blue);
            CommitRedAgainst(session, WaspCardContent.ThunderBoomerang, WaspCardContent.Shock);
            AttackTheHero(session, defender);

            var defense = session.Projection.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null);
            var block = BlockOffer(session, defense, WaspCardContent.StopProjectiles);

            var preview = session.Projection.PendingDefensePreview;
            Assert.That(preview, Is.Not.Null,
                "The prompt is drawn from the preview, so no preview is no prompt.");
            var blockPreview = preview.Choices.SingleOrDefault(value =>
                value.CardInstanceId.HasValue &&
                value.CardInstanceId.Value.Equals(block.SourceCardInstanceId));
            Assert.That(blockPreview, Is.Not.Null,
                "A Block is an offer like any other, so it is previewed like any other.");

            Assert.That(blockPreview.DefenseTotal, Is.Null,
                "A Block prints no Defense value, so it previews none - p.14 skips the comparison " +
                "rather than feeding it a number.");
            Assert.That(blockPreview.DefenseContributions, Is.Empty,
                "and it contributes nothing, so nothing may look for its printed icon.");
            Assert.That(blockPreview.BlockResult, Is.EqualTo(DefenseBlockResult.Blocked),
                "What it previews instead is the verdict: this attack is ranged, so the Block holds.");
            Assert.That(blockPreview.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "and the outcome that verdict produces, which is what the defender is choosing between.");
        }

        /// <summary>
        /// Regression found by selecting a hero Attack target in the configured scene: a nearby
        /// minion contributes to every ordinary numeric Defense offer, but never to a Block. The
        /// projection used to require the Block's deliberately empty contribution list to equal the
        /// shared numeric-minion list and threw after the target command had already advanced the
        /// authority, leaving the board looking as though the click did nothing.
        /// </summary>
        [Test]
        public void ABlockPreview_DoesNotBorrowNearbyMinionContributionsFromNumericDefense()
        {
            var attack = new ValueContributionProjection(
                new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    new CardInstanceId("block-preview-attack-card"),
                    new ActionDefinitionId("block-preview-attack"),
                    PrintedValueChannel.Attack,
                    3),
                "Attack Card");
            var printedDefense = new ValueContributionProjection(
                new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    new CardInstanceId("numeric-defense-card"),
                    new ActionDefinitionId("numeric-defense"),
                    PrintedValueChannel.Defense,
                    2),
                "Numeric Defense Card");
            var supportingMinion = new ValueContributionProjection(
                new ValueContribution(
                    ValueContributionKind.MinionDefenseModifier,
                    new MinionId("nearby-friendly-minion"),
                    PrintedValueChannel.Defense,
                    1),
                "Nearby Friendly Minion");
            var numeric = new DefenseChoicePreviewProjection(
                printedDefense.Contribution.SourceCardInstanceId,
                "Numeric Defense Card",
                new[] { printedDefense, supportingMinion },
                3,
                DefenseBlockResult.None,
                AttackResolutionOutcome.SuccessfullyDefended);
            var block = new DefenseChoicePreviewProjection(
                new CardInstanceId("block-defense-card"),
                "Block Card",
                Array.Empty<ValueContributionProjection>(),
                null,
                DefenseBlockResult.Blocked,
                AttackResolutionOutcome.SuccessfullyDefended);

            PendingDefensePreviewProjection preview = null;
            Assert.DoesNotThrow(() => preview = new PendingDefensePreviewProjection(
                new ActionResolutionId("block-preview-resolution"),
                new ActionResolutionStepId("block-preview-choice"),
                new[] { attack },
                3,
                new[] { numeric, block },
                new[] { supportingMinion }));

            Assert.That(preview.MinionContributions, Is.EqualTo(new[] { supportingMinion }),
                "The shared numeric-defense explanation still names the nearby minion.");
            Assert.That(preview.Choices.Single(choice =>
                    choice.CardInstanceId.Equals(numeric.CardInstanceId)).DefenseContributions,
                Does.Contain(supportingMinion),
                "The ordinary Defense still composes the shared minion modifier.");
            Assert.That(preview.Choices.Single(choice =>
                    choice.CardInstanceId.Equals(block.CardInstanceId)).DefenseContributions,
                Is.Empty,
                "A Block skips the numeric comparison, so it neither borrows nor displays that bonus.");
        }

        /// <summary>
        /// The application-boundary regression for the configured-scene failure above. The real
        /// target command used to advance the Attack authority and then throw while publishing the
        /// mixed Block/numeric preview, so the click appeared unclaimed and the target prompt stayed
        /// on screen. This fixture carries both response shapes and a real adjacent minion modifier
        /// through one legal hero target submission.
        /// </summary>
        [Test]
        public void HeroAttackTarget_WithBlockAndNumericMinionDefense_IsAcceptedAndPublishesDefensePreview()
        {
            var session = CreateHeroDefensePreviewSession(
                out var defender, out var supportingMinion);
            CommitRedAgainst(session, WaspCardContent.MagneticDagger, WaspCardContent.Shock);
            var attack = session.Projection.AvailableTurnActions.Single(candidate =>
                ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, candidate).Kind == ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(targetChoice.LegalTargetHeroIds, Does.Contain(defender),
                "The regression must submit the hero target the board click names.");
            var result = session.Execute(DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                defender));

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "The accepted target command clears the Attack target prompt.");
            Assert.That(session.Projection.PendingAttackResolution, Is.Not.Null,
                "The Attack itself remains pending while the defender answers p.14 step 3.");
            var defense = session.Projection.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null,
                "A hero Attack continues to Defense instead of resolving like a minion Attack.");
            var preview = session.Projection.PendingDefensePreview;
            Assert.That(preview, Is.Not.Null,
                "The same accepted command publishes the response surface the HUD renders.");
            Assert.That(preview.MinionContributions.Select(value => value.SourceMinionId),
                Does.Contain(supportingMinion),
                "The adjacent friendly melee minion is a real shared numeric modifier.");

            var block = BlockOffer(session, defense, WaspCardContent.StopProjectiles);
            var blockPreview = preview.Choices.Single(value =>
                value.CardInstanceId.HasValue &&
                value.CardInstanceId.Value.Equals(block.SourceCardInstanceId));
            Assert.That(blockPreview.DefenseTotal, Is.Null,
                "A Block still has no numeric total when numeric choices share minion modifiers.");
            Assert.That(blockPreview.DefenseContributions, Is.Empty,
                "and the Block never borrows the shared numeric contribution list.");

            var numericPreview = preview.Choices.First(value =>
                value.CardInstanceId.HasValue && value.DefenseTotal.HasValue);
            Assert.That(
                numericPreview.DefenseContributions
                    .Where(value => value.Kind == ValueContributionKind.MinionDefenseModifier)
                    .Select(value => value.SourceMinionId),
                Is.EquivalentTo(preview.MinionContributions.Select(value => value.SourceMinionId)),
                "An ordinary Defense choice still carries every shared minion contribution.");
        }

        /// <summary>
        /// p.14's Block, fulfilled: the attack being answered is Ranged, so the defence succeeds
        /// "regardless of the attack value" - and Thunder Boomerang's Attack 4 is higher than any
        /// Defense total Stop Projectiles could ever have contributed, because it prints none.
        /// </summary>
        [Test]
        public void StopProjectiles_BlocksARangedAttackWhateverTheAttackTotal()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out _);
            var defender = HeroOn(session, TeamSide.Blue);
            CommitRedAgainst(session, WaspCardContent.ThunderBoomerang, WaspCardContent.Shock);
            AttackTheHero(session, defender);

            var defense = session.Projection.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, "p.14 step 3 offers the defender their hand.");
            var block = BlockOffer(session, defense, WaspCardContent.StopProjectiles);

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(block.SourceCardInstanceId));

            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.True,
                "\"If the Block condition is fulfilled, you successfully defend, regardless of the " +
                "attack value.\"");
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(block.SourceCardInstanceId))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and the card was discarded to perform it, exactly as a numeric Defense is.");
        }

        /// <summary>
        /// The condition's other answer, on the same card: p.14's "If not, you are defeated." The
        /// attack is non-Ranged, so the Block cannot be fulfilled and no total is compared - which is
        /// the case that keeps a Block from quietly becoming a Defense of zero.
        /// </summary>
        [Test]
        public void StopProjectiles_DefeatsItsOwnHeroAgainstANonRangedAttack()
        {
            // The mirrored Wasps standing beside each other, so Magnetic Dagger's "Target a unit
            // adjacent to you" has the defending hero to take - and it is p.13's non-Ranged subtype,
            // which is the whole of what makes the Block condition false.
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var defender = HeroOn(session, TeamSide.Blue);
            CommitRedAgainst(session, WaspCardContent.MagneticDagger, WaspCardContent.Shock);
            AttackTheHero(session, defender);

            var defense = session.Projection.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null);
            var block = BlockOffer(session, defense, WaspCardContent.StopProjectiles);

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(block.SourceCardInstanceId));

            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.False,
                "p.14: \"If not, you are defeated.\" - and p.13's list removed the hero.");
        }

        // ---------------------------------------------------------------------------------------
        // The two riders, and the one clause that separates them
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Reflect Projectiles: "Block a ranged attack; if you do, an enemy hero in range discards a
        /// card, if able." The rider is the defender's own - their printed Range, their choice - and
        /// on this board the attacker is the one enemy hero in it.
        /// </summary>
        [Test]
        public void ReflectProjectiles_LetsTheDefenderTakeACardFromAnEnemyHeroInRange()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.ReflectProjectiles);
            var defender = HeroOn(session, TeamSide.Blue);
            var attacker = HeroOn(session, TeamSide.Red);
            BlockTheRangedAttack(session, WaspCardContent.ReflectProjectiles);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null,
                "The Block succeeded, so \"if you do\" is true and p.14's step 4 text asks its " +
                "question.");
            Assert.That(choice.ChooserId, Is.EqualTo(defender),
                "p.10 gives the unassigned choice to the card's controller, who here is the defender.");
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.Value.HeroId),
                Is.EqualTo(new[] { attacker }),
                "and Reflect excludes nobody, so the attacker is in range and offered.");

            var handBefore = HandCount(session, attacker);
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, choice.SelectInvocations.Single());

            var cardChoice = session.Projection.PendingEffectChoice;
            Assert.That(cardChoice.ChooserId, Is.EqualTo(attacker),
                "The card is the chosen hero's own decision, out of their own hand.");
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, cardChoice.SelectInvocations.First());

            Assert.That(HandCount(session, attacker), Is.EqualTo(handBefore - 1));
            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.True,
                "and the Block that let the rider run still defended.");
        }

        /// <summary>
        /// Deflect Projectiles prints the same sentence with three words more - "other than the
        /// attacker" - and on this board those three words are the whole difference: the attacker is
        /// the only enemy hero in range, so the rider finds nobody and nothing is asked. The defence
        /// still stands, because the Block was decided before the rider ran.
        /// </summary>
        [Test]
        public void DeflectProjectiles_ExcludesTheAttackerAndSoAsksNothingOnThisBoard()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.DeflectProjectiles);
            var defender = HeroOn(session, TeamSide.Blue);
            var attacker = HeroOn(session, TeamSide.Red);
            var handBefore = HandCount(session, attacker);
            BlockTheRangedAttack(session, WaspCardContent.DeflectProjectiles);

            // Something is pending, and it is deliberately not this card's: p.14's step 7 applies the
            // attacker's own "After the attack:" text whether or not the attack succeeded, so Thunder
            // Boomerang asks the attacker about its repeat. What matters here is that the *defender*
            // was never asked, which is the rider finding nobody.
            Assert.That(
                session.Projection.PendingEffectChoice?.ChooserId, Is.Not.EqualTo(defender),
                "The one enemy hero in range is the attacker, whom this card's three extra words " +
                "exclude, so there is nobody for the defender to be asked about.");
            Assert.That(HandCount(session, attacker), Is.EqualTo(handBefore - 1),
                "The attacker lost only the card they committed to this turn, not one to the rider.");
            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.True,
                "and the Block still defended, because its condition was answered before the rider.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Runs the Ranged attack this fixture exists for and answers it with the named Block.
        /// </summary>
        internal static void BlockTheRangedAttack(MatchSession session, CardDefinitionId blockCard)
        {
            var defender = HeroOn(session, TeamSide.Blue);
            CommitRedAgainst(session, WaspCardContent.ThunderBoomerang, WaspCardContent.Shock);
            AttackTheHero(session, defender);

            var defense = session.Projection.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null);
            var block = BlockOffer(session, defense, blockCard);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(block.SourceCardInstanceId));
        }

        private static void Answer(MatchSession session, EffectChoiceCommand invocation) =>
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(invocation));

        private static int HandCount(MatchSession session, HeroId heroId) =>
            session.Projection.Cards.Count(card =>
                card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand);

        /// <summary>
        /// Red commits <paramref name="redCard"/> and Blue commits <paramref name="blueCard"/>, which
        /// is deliberately not the Block the defender will later discard: a card committed to this turn
        /// is no longer in the hand p.14's step 3 reads.
        /// </summary>
        internal static void CommitRedAgainst(
            MatchSession session, CardDefinitionId redCard, CardDefinitionId blueCard)
        {
            var red = HandCard(session, TeamSide.Red, redCard);
            var blue = HandCard(session, TeamSide.Blue, blueCard);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The attacking card has to be the one acting, or every assertion below is about Blue.");
        }

        internal static void AttackTheHero(MatchSession session, HeroId target)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                ActionOf(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                ActionOf(session, candidate).Kind == ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(offer.ActionDefinitionId));

            var choice = session.Projection.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargetHeroIds, Does.Contain(target),
                "The defending hero has to be a legal target, or the case holds over nothing.");
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, target));
        }

        private static ActionOffer BlockOffer(
            MatchSession session, PendingDefenseResponseChoice defense, CardDefinitionId cardId)
        {
            var offer = defense.LegalDefenseOffers.SingleOrDefault(candidate =>
                candidate.SourceDefinitionReference.DefinitionId.Equals(cardId));
            Assert.That(offer.SourceCardInstanceId.IsDefined, Is.True,
                $"{cardId.Value} has to be offered as a defence, or the Block is unreachable.");
            return offer;
        }

        /// <summary>
        /// One fixture card whose only action is a secondary Defense, built here rather than dealt so
        /// the composition statement is the single thing that varies between the two cases above.
        /// </summary>
        private static (CardDefinition Definition, Func<HeroId, CardInstance> Instance) DefenseCard(
            PrintedTextComposition composition)
        {
            var suffix = composition.IsAwaited ? "owed" : "composed";
            var definition = new CardDefinition(
                new CardDefinitionId($"defense-honesty-{suffix}"),
                "defense-honesty-rev-1",
                "TEST - Defense Honesty",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-defense"),
                        ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        null,
                        composition)
                });

            return (definition, owner => CardInstance.CreateInHand(
                new CardInstanceId($"defense-honesty-{suffix}-card"), definition.Reference, owner));
        }

        private static MatchSession CreateHeroDefensePreviewSession(
            out HeroId defender, out MinionId supportingMinion)
        {
            var configuration =
                MatchConfiguration.CreateVerifiedMirrorForInternalTest(TeamSide.Red);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("block-minion-defense-preview"),
                1,
                new[]
                {
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(1, 1)
                });
            var redHero = configuration.RedHand.Instances[0].OwnerId;
            defender = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHero);
            var blueTeam = configuration.Participants.GetTeamForHero(defender);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var minionCoordinate = new HexCoordinate(1, 1);
            supportingMinion = new MinionId("block-preview-blue-support");
            var minion = new QuickGameMinionSetupState(
                supportingMinion,
                blueTeam.Id,
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(
                    map.Id, new SpawnPointId("block-preview-blue-support-spawn")),
                new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r1")),
                minionCoordinate);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(
                        redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(
                        blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHero,
                        new HeroContentId("red-block-preview"),
                        "Red Wasp",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(
                            map.Id, new SpawnPointId("red-block-preview-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        defender,
                        new HeroContentId("blue-block-preview"),
                        "Blue Wasp",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(
                            map.Id, new SpawnPointId("blue-block-preview-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                        blueCoordinate)
                },
                new[] { minion });
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHero, redCoordinate),
                    new HeroOccupancy(defender, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());

            return new MatchSession(new MatchSessionInitialState(
                scenario, setup, new[] { map }));
        }

        private static ActionDefinition PrimaryOf(CardDefinitionId cardId, ActionDefinitionId actionId) =>
            WaspCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Id.Equals(actionId));

        private static ActionDefinition ActionOf(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static CardInstance HandCard(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));

        internal static HeroId HeroOn(MatchSession session, TeamSide side) =>
            session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side == side)
                .HeroId;
    }
}
