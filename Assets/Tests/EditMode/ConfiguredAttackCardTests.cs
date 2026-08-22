using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.10: Wasp's five printed Attacks resolve their complete printed text in the
    /// configured mirror.
    ///
    /// <para>Two of the five say what they may target and nothing else. Both Boomerangs print "Target a
    /// unit in range and not in a straight line", which is a sentence about the Attack's own target set
    /// rather than a rider hanging off it, so it is composed as the action's
    /// <see cref="PrintedAttackTargetPolicy"/> and is read by the offer, the target choice and the
    /// resolution from that one declaration.</para>
    ///
    /// <para>The geometry itself is not proved here. <c>StraightLineQueryTests</c> owns rulebook p.19's
    /// sentence and its complement, and milestone 8.6 already runs that complement from card text.
    /// What is here is Wasp: her declarations, her numbers, and her cards run through a real
    /// <see cref="MatchSession"/> on a board where the complement is what decides the answer.</para>
    /// </summary>
    public sealed class ConfiguredAttackCardTests
    {
        // ---------------------------------------------------------------------------------------
        // The declarations themselves
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BothBoomerangs_DeclareThePageNineteenComplementAsTheirOwnPrintedTargeting()
        {
            foreach (var cardId in new[]
                     {
                         WaspCardContent.ChargedBoomerang, WaspCardContent.ThunderBoomerang
                     })
            {
                var attack = PrimaryOf(cardId, WaspCardContent.PrimaryRangedAttack);

                Assert.That(attack.AttackTargetPolicy, Is.Not.Null);
                Assert.That(attack.AttackTargetPolicy.Value.Line,
                    Is.EqualTo(AttackTargetLine.NotInStraightLine),
                    $"{cardId.Value} prints \"not in a straight line\".");
                Assert.That(attack.AttackTargetPolicy.Value.Reach,
                    Is.EqualTo(AttackTargetReach.PrintedRange),
                    "and reaches by p.13's Ranged sentence, on the printed Range icon.");
                Assert.That(attack.AttackTargetPolicy.Value.TargetableUnits,
                    Is.EqualTo(AttackTargetableUnits.HeroesAndMinions),
                    "The face says \"a unit\", which is p.13's default union and not a narrowing.");
                Assert.That(
                    attack.AttackTargetPolicy.Value.NarrowsTheDefault(AttackRangeKind.Ranged),
                    Is.True,
                    "so the card does narrow the page's default, and says so as typed content.");
            }
        }

        /// <summary>
        /// Charged Boomerang is the one composed Wasp Attack with no rider at all: its whole printed
        /// text is the targeting sentence, so the composition it owes is discharged by the target
        /// policy and it carries no effect root. Milestone 8.4's greying rule reads the debt, so a card
        /// left claiming one would be drawn unplayable with nothing left to implement.
        /// </summary>
        [Test]
        public void ChargedBoomerang_OwesNoCompositionAndCarriesNoEffectRoot()
        {
            var attack = PrimaryOf(
                WaspCardContent.ChargedBoomerang, WaspCardContent.PrimaryRangedAttack);

            Assert.That(attack.TextComposition.IsAwaited, Is.False);
            Assert.That(attack.EffectRoot, Is.Null,
                "There is no rider to compose; the sentence is the targeting.");
            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.None),
                "and with no root there is no moment of p.14 to apply one at.");
        }

        // ---------------------------------------------------------------------------------------
        // The complement in the configured mirror
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The discriminating board: the Blue Wasp stands beside the Red one and a Blue melee minion
        /// stands inside Range 3 on no file through her. Both are enemy units inside the printed reach,
        /// so distance decides nothing here and the complement decides everything.
        /// </summary>
        [Test]
        public void ChargedBoomerang_OffersTheUnitOffEveryFileAndNotTheEnemyStandingBesideHer()
        {
            var session = ConfiguredMatchFixtures.CreateBoomerangSession(
                WaspCardContent.ChargedBoomerang, out var outOfLineMinionId);
            var actor = HeroOn(session, TeamSide.Red);
            var adjacentEnemy = HeroOn(session, TeamSide.Blue);

            Assert.That(
                HexDistanceQuery.Calculate(CoordinateOf(session, actor), CoordinateOf(session, adjacentEnemy)),
                Is.LessThanOrEqualTo(3),
                "The excluded hero has to be inside the printed Range, or the case proves only that " +
                "the Attack cannot reach that far.");

            StartTheBoomerang(session, WaspCardContent.ChargedBoomerang);
            var choice = session.Projection.PendingAttackTargetChoice;

            Assert.That(choice.LegalTargetMinionIds, Does.Contain(outOfLineMinionId),
                "The minion on no file through the actor is what the card admits.");
            Assert.That(choice.LegalTargetHeroIds, Has.No.Member(adjacentEnemy),
                "and p.19 counts the space beside her as a straight line, which the face's own " +
                "reminder states: \"( Adjacent units are in a straight line as well. )\".");
            Assert.That(choice.LegalTargetHeroIds, Is.Empty,
                "The Blue hero is the only enemy hero on this board.");

            var origin = CoordinateOf(session, actor);
            Assert.That(
                choice.LegalTargetMinionIds.All(minionId =>
                    !StraightLineQuery.AreInStraightLine(origin, CoordinateOf(session, minionId))),
                Is.True,
                "and the rule holds over the whole offered set rather than over one placed unit.");
        }

        /// <summary>
        /// The same board, asked of an ordinary Ranged Attack instead. It is the control the case above
        /// needs: without it, an empty hero offer could equally be a board with no reachable hero on it.
        /// </summary>
        [Test]
        public void AnAttackDeclaringNoComplement_StillOffersTheEnemyStandingBesideHerOnTheSameBoard()
        {
            var session = ConfiguredMatchFixtures.CreateBoomerangSession(
                WaspCardContent.ChargedBoomerang, out var outOfLineMinionId);
            var actor = HeroOn(session, TeamSide.Red);
            var adjacentEnemy = HeroOn(session, TeamSide.Blue);

            var ordinary = AttackTargetQuery.FindLegalEnemyTargets(
                actor,
                PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged),
                3,
                session.Projection.Participants,
                Restore(session).Board,
                RuleModificationSet.Empty);

            Assert.That(
                ordinary.Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => target.HeroId),
                Does.Contain(adjacentEnemy),
                "p.13's default Ranged Attack reaches the hero standing beside her.");
            Assert.That(
                ordinary.Where(target => target.Kind == AttackTargetKind.Minion)
                    .Select(target => target.MinionId),
                Does.Contain(outOfLineMinionId),
                "and the minion too, so the Boomerang's offer differs by the complement alone.");
        }

        // ---------------------------------------------------------------------------------------
        // The radius discard rider, and the pause it opens inside rulebook p.14
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The case this whole milestone exists for: card text applied inside an Attack asks the
        /// player a question, twice, and the attack finishes afterwards.
        ///
        /// <para>Before milestone 8.10 this composition could not even be loaded - the effect-definition
        /// validator refused any card-text root that could pause, because p.14's remaining steps were
        /// produced in the command that began them. Both questions here are p.10's own worked example:
        /// the actor picks which enemy hero the sentence lands on, and that hero picks which card to
        /// give.</para>
        /// </summary>
        [Test]
        public void ShocksRider_AsksTheActorForAHeroAndThatHeroForACard_ThenFinishesTheAttack()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            var actor = HeroOn(session, TeamSide.Red);
            var enemy = HeroOn(session, TeamSide.Blue);

            AttackTheAdjacentMinion(session, WaspCardContent.Shock, minionId);

            var heroChoice = session.Projection.PendingEffectChoice;
            Assert.That(heroChoice, Is.Not.Null,
                "p.14's step 7 has been reached and the rider stopped the attack on its first " +
                "question, which nothing in this build could do before this milestone.");
            Assert.That(heroChoice.ChooserId, Is.EqualTo(actor),
                "p.10 gives an unassigned choice to the card's controller, and this one is unassigned.");
            Assert.That(
                heroChoice.SelectInvocations.Select(invocation => invocation.Candidate.Value.HeroId),
                Is.EqualTo(new[] { enemy }),
                "The one enemy hero inside Radius 2 and off the adjacent ring is the whole offer.");
            Assert.That(
                HexDistanceQuery.Calculate(CoordinateOf(session, actor), CoordinateOf(session, enemy)),
                Is.EqualTo(2),
                "and it is inside the disk by distance rather than by having been placed there.");

            Answer(session, heroChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var cardChoice = session.Projection.PendingEffectChoice;
            Assert.That(cardChoice, Is.Not.Null, "The chosen hero is now asked for a card.");
            Assert.That(cardChoice.ChooserId, Is.EqualTo(enemy),
                "The card is the chosen hero's own decision, out of their own hand.");

            // Measured here rather than before the round began: the defender committed a card of their
            // own to this turn, so a count taken at setup would be reporting p.7's commitment as well
            // as the discard.
            var handBefore = HandCount(session, enemy);
            Assert.That(
                cardChoice.SelectInvocations.Select(invocation =>
                    invocation.Candidate.Value.CardInstanceId),
                Is.EquivalentTo(HandCards(session, enemy).Select(card => card.Id)),
                "and the candidates are that hero's own hand, read through the same query the hero " +
                "would see.");

            var chosen = cardChoice.SelectInvocations.First().Candidate.Value.CardInstanceId;
            Answer(session, cardChoice.SelectInvocations.First());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Both questions are answered, so the page runs on.");
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(chosen)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(HandCount(session, enemy), Is.EqualTo(handBefore - 1));
            Assert.That(
                session.Projection.Cards
                    .Single(card => card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Shock) &&
                        card.OwnerId.Equals(actor))
                    .State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved),
                "and the attack that was holding p.14 open finished, which is what the steps rebuilt " +
                "from the paused continuation carry.");
        }

        /// <summary>
        /// The same rider stopped mid-question and put through a snapshot. The continuation persists
        /// identity alone - the paused frame, the moment, and the attack - so what comes back has to be
        /// a match still holding the same question, with the rest of p.14 still owed.
        /// </summary>
        [Test]
        public void ThePauseInsideThePage_SurvivesASnapshotRestore()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            var enemy = HeroOn(session, TeamSide.Blue);
            AttackTheAdjacentMinion(session, WaspCardContent.Shock, minionId);

            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var beforeRestore = session.Projection.PendingEffectChoice;
            Assert.That(beforeRestore.ChooserId, Is.EqualTo(enemy));

            var restored = Restore(session);
            Assert.That(restored.CardActions.PendingCardTextResolution, Is.Not.Null,
                "The restored authority is still holding rulebook p.14 open.");
            Assert.That(restored.CardActions.PendingCardTextResolution.Moment,
                Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(restored.CardActions.PendingEffectChoice.ChooserId, Is.EqualTo(enemy),
                "and the same hero is still the one being asked, re-derived rather than remembered.");
        }

        // ---------------------------------------------------------------------------------------
        // Electroblast's escalation
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Ledger question 13's reading, run: "discards a card, or is defeated" is one mandatory step
        /// with two ways to complete, so a chosen hero holding a card gives one and a chosen hero
        /// holding none is defeated. Neither is a decision either player makes, and this case is the
        /// arm Shock's `if able` does not have.
        /// </summary>
        [Test]
        public void ElectroblastsRider_DefeatsTheChosenHeroWhoHasNoCardToGive()
        {
            var session = ConfiguredMatchFixtures.CreateEscalatingRiderSession(out var minionId);
            var enemy = HeroOn(session, TeamSide.Blue);

            AttackTheAdjacentMinion(session, WaspCardContent.Electroblast, minionId);

            var heroChoice = session.Projection.PendingEffectChoice;
            Assert.That(heroChoice, Is.Not.Null,
                "The rider still stops for the actor's own choice; the escalation changes what happens " +
                "after it, not who is asked.");
            Assert.That(HandCount(session, enemy), Is.Zero,
                "The chosen hero committed their last card to this turn, which is the position the " +
                "second arm is about.");

            Answer(session, heroChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Nobody is asked which card to give, because there is none and the page asks nothing " +
                "about the second arm - it is a branch, not a question.");
            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(enemy)),
                Is.False,
                "p.13's first line: \"Remove the defeated hero from the board.\"");
        }

        /// <summary>
        /// The other arm of the same card, on a hero who does hold something. It is the control the
        /// case above needs: without it, a defeat could be what Electroblast does to anybody.
        /// </summary>
        [Test]
        public void ElectroblastsRider_TakesACardAndLeavesTheHeroStandingWhenTheyHaveOne()
        {
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            var enemy = HeroOn(session, TeamSide.Blue);

            AttackTheAdjacentMinion(session, WaspCardContent.Shock, minionId);
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var cardChoice = session.Projection.PendingEffectChoice;
            Assert.That(cardChoice, Is.Not.Null,
                "A hero who holds cards is asked for one rather than removed.");
            Answer(session, cardChoice.SelectInvocations.First());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(enemy)),
                Is.True,
                "and is still standing afterwards.");
        }

        // ---------------------------------------------------------------------------------------
        // Thunder Boomerang's conditional repeat
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// "If you targeted a hero may repeat once on a different target." All three printed clauses
        /// are observed here: the condition is true because the Attack took a hero, the repeat is
        /// offered rather than taken because "may" is p.10's confirm-or-decline, and the second
        /// instance may not take the hero the first one did.
        /// </summary>
        [Test]
        public void ThunderBoomerang_OffersASecondInstanceThatMayNotTakeTheFirstsTarget()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out var minionId);
            var enemy = HeroOn(session, TeamSide.Blue);

            StartTheBoomerang(session, WaspCardContent.ThunderBoomerang);
            var firstChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(firstChoice.LegalTargetHeroIds, Does.Contain(enemy),
                "The enemy hero stands off every file inside Range 3, so the card admits them.");
            Assert.That(firstChoice.LegalTargetMinionIds, Does.Contain(minionId),
                "and so is the minion, which is what the repeat will have left to take.");

            ConfiguredMatchFixtures.Accept(
                session,
                DirectorCommand.ChooseAttackTarget(
                    firstChoice.ResolutionId, firstChoice.ChoiceId, enemy));
            ResolveTheDefenseResponse(session);

            var repeatChoice = session.Projection.PendingEffectChoice;
            Assert.That(repeatChoice, Is.Not.Null,
                "The Attack took a hero, so p.14's step 7 reaches the repeat and `may` asks for it.");
            Assert.That(repeatChoice.ChooserId, Is.EqualTo(HeroOn(session, TeamSide.Red)),
                "p.10 gives an unassigned decision to the card's controller.");

            Answer(session, repeatChoice.ConfirmInvocation);

            var second = session.Projection.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null, "Confirming the repeat starts a second Attack instance.");
            Assert.That(second.LegalTargetHeroIds, Has.No.Member(enemy),
                "\"on a different target\": the unit the first instance took is out of the set.");
            Assert.That(second.LegalTargetMinionIds, Does.Contain(minionId),
                "and the one it did not take is still in it.");
        }

        /// <summary>
        /// The condition's other answer, on the same card: an Attack that took a minion reaches the
        /// same text and the repeat is never offered, because "If you targeted a hero" is false.
        /// </summary>
        [Test]
        public void ThunderBoomerang_OffersNoRepeatWhenTheAttackTookAMinion()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out var minionId);

            StartTheBoomerang(session, WaspCardContent.ThunderBoomerang);
            var choice = session.Projection.PendingAttackTargetChoice;
            ConfiguredMatchFixtures.Accept(
                session,
                DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, minionId));

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "p.13 defeats a targeted minion outright, and the text that follows asks nothing " +
                "because the Attack took no hero.");
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "so no second instance starts either.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The defender answers rulebook p.14's step 2 by declining to defend, which is the shortest
        /// route through the page to the step 7 text these cases are about.
        /// </summary>
        private static void ResolveTheDefenseResponse(MatchSession session)
        {
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "p.14 gives a targeted hero the chance to defend before the attack is checked.");
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.DeclineDefense());
        }

        private static void AttackTheAdjacentMinion(
            MatchSession session, CardDefinitionId cardId, MinionId minionId)
        {
            StartTheBoomerang(session, cardId);
            var choice = session.Projection.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargetMinionIds, Does.Contain(minionId),
                "The minion the fixture placed beside Wasp has to be a legal target, or the case " +
                "holds over nothing.");
            ConfiguredMatchFixtures.Accept(
                session,
                DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, minionId));
        }

        private static void Answer(MatchSession session, EffectChoiceCommand invocation) =>
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(invocation));

        private static int HandCount(MatchSession session, HeroId heroId) =>
            HandCards(session, heroId).Count;

        private static IReadOnlyList<CardInstance> HandCards(MatchSession session, HeroId heroId) =>
            session.Projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand)
                .ToArray();

        private static void StartTheBoomerang(MatchSession session, CardDefinitionId cardId)
        {
            var red = HandCard(session, TeamSide.Red, cardId);
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The tested card has to be the one acting, or every assertion below is about Blue.");

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                Action(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                Action(session, candidate).Kind == ActionKind.Attack);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        private static ActionDefinition PrimaryOf(CardDefinitionId cardId, ActionDefinitionId actionId) =>
            WaspCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Id.Equals(actionId));

        private static ActionDefinition Action(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static CardInstance HandCard(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));

        private static HeroId HeroOn(MatchSession session, TeamSide side) =>
            session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side == side)
                .HeroId;

        private static HexCoordinate CoordinateOf(MatchSession session, HeroId heroId) =>
            session.Projection.Heroes.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static HexCoordinate CoordinateOf(MatchSession session, MinionId minionId) =>
            session.Projection.Minions.Single(occupancy => occupancy.MinionId.Equals(minionId)).Coordinate;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
    }
}
