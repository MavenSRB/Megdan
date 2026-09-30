using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using Round = GuardsDigital.Tests.EditMode.RazzleStuntDoublesTests.Round;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21g. Razzle's counting faces on 9.21d's staged round (<see cref="RazzleStuntDoublesTests"/>):
    /// Crowd Control discarded to defend, "+2 Defense for each other you in radius" measured from the
    /// ATTACKED body (RZ-Q3); Rummage, "If there's another you in radius, you may retrieve a discarded
    /// card" (a printed "If": false is RULING 53's do-nothing); and Ransack, "For every other you in
    /// radius you may retrieve a discarded card", each retrieval its own "may". All three read the
    /// card's printed Radius 3, raised by a Radius item (R1). Nothing is registered: the round composes
    /// Razzle's handlers into its own catalog.
    ///
    /// <para><b>The board.</b> 9.21d's radius-5 disk: body 1 at the origin with an enemy minion beside
    /// it at (-1, 0) unless a case moves it; further bodies where a case stands them; the enemy hero
    /// far away unless a case brings it beside the body it attacks. Razzle's discarded cards are the
    /// round's plain Movement cards (<see cref="Round.DiscardedCard"/>).</para>
    /// </summary>
    public sealed class RazzleCountingFacesTests
    {
        private static readonly HeroId Razzle = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        private static readonly HexCoordinate Origin = H(0, 0);

        // Three spaces from the origin (inside Radius 3), and one just outside it.
        private static readonly HexCoordinate ThreeOut = H(3, -3);
        private static readonly HexCoordinate ThreeOutSouth = H(-3, 3);
        private static readonly HexCoordinate ThreeOutEast = H(3, 0);
        private static readonly HexCoordinate FourOut = H(4, -4);

        // --- The printed faces ----------------------------------------------------------------------

        [Test]
        public void Rummage_IsTheVerifiedRedTierTwo_WithTheIfGateOverOneOptionalRetrieval()
        {
            var face = RazzleCardContent.RummageDefinition;
            AssertCountingAttack(face, 9, 4, CardTier.II, PrintedValueChannel.Initiative,
                "Target a unit adjacent to you. After the attack: If there's another you in radius, you " +
                "may retrieve a discarded card. (All of you share cards in hand, deck, and discard).");
            Assert.That(face.Progression.NextTierCandidateIds.Select(id => id.Value),
                Is.EquivalentTo(new[] { "razzle-into-thin-air", "razzle-ransack" }));
            AssertGatedRetrievals(face, 1);
        }

        [Test]
        public void Ransack_IsTheVerifiedRedTierThree_WithOneGatedOptionalRetrievalPerPossibleOtherBody()
        {
            var face = RazzleCardContent.RansackDefinition;
            AssertCountingAttack(face, 10, 5, CardTier.III, PrintedValueChannel.Movement,
                "Target a unit adjacent to you. After the attack: For every other you in radius you may " +
                "retrieve a discarded card.");
            Assert.That(face.Progression.NextTierCandidateIds, Is.Empty);
            AssertGatedRetrievals(face, RazzleEffectContent.RansackMostOtherBodies);
            Assert.That(RazzleEffectContent.RansackMostOtherBodies, Is.EqualTo(3),
                "RZ-Q1 (a): four bodies in all, so at most three are \"other\".");
        }

        [Test]
        public void TheUpgradeLines_NameTheseTwoFaces()
        {
            Assert.That(RazzleCardContent.PhantomStrikeDefinition.Progression.NextTierCandidateIds,
                Does.Contain(RazzleCardContent.Rummage));
            Assert.That(RazzleCardContent.HitAndGoneDefinition.Progression.NextTierCandidateIds,
                Does.Contain(RazzleCardContent.Ransack));
            Assert.That(RazzleCardContent.AllDefinitions.Select(face => face.Id),
                Is.SupersetOf(new[] { RazzleCardContent.Rummage, RazzleCardContent.Ransack }));
        }

        [Test]
        public void CrowdControl_TheDefenseCarriesPlusTwoPerOtherBodyInRadius_AndTheSkillKeepsItsRemoval()
        {
            var face = RazzleCardContent.CrowdControlDefinition;
            var defense = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense);
            Assert.That(defense.RosterValueContributions, Is.EqualTo(new[]
            {
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Defense, RosterValueQueryKind.DefenderOtherBodiesInPrintedRadius, 2)
            }), "\"+2 Defense for each other you in radius\" - the printed 1+.");
            Assert.That(defense.DefaultPolicy.CardTextApplication, Is.EqualTo(CardTextApplicationPolicy.Applies),
                "p.14: a Defense/Skill card's primary Defense applies its text.");
            Assert.That(defense.EffectRoot, Is.Null, "a value, not an effect: nothing is asked.");

            var skill = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            Assert.That(skill.RosterValueContributions, Is.Empty,
                "\"When used as a defense action\": the Skill is never raised.");
            Assert.That(skill.EffectRoot, Is.Not.Null, "9.21e's removal is unchanged.");
        }

        [Test]
        public void TheOtherBodiesQuery_IsADefenseQueryOnly()
        {
            Assert.That((int)RosterValueQueryKind.DefenderOtherBodiesInPrintedRadius, Is.EqualTo(12));
            Assert.That(() => new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack, RosterValueQueryKind.DefenderOtherBodiesInPrintedRadius, 2),
                Throws.ArgumentException, "the defender's bodies are a Defense's question.");
            Assert.That(() => new RosterValueContributionDefinition(
                    PrintedValueChannel.Defense, RosterValueQueryKind.DefenderOtherBodiesInPrintedRadius, 2),
                Throws.Nothing);
            Assert.That(() => new RosterValueContributionDefinition(
                    PrintedValueChannel.Defense, RosterValueQueryKind.EnemyUnitsAdjacentToActor, 2),
                Throws.ArgumentException, "the Defense channel still admits only its verified queries.");
        }

        // --- Crowd Control's Defense ----------------------------------------------------------------

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void CrowdControlsDefense_IsOnePlusTwoForEachOtherBodyInRadiusOfTheAttackedBody(int others)
        {
            var bodies = new[] { ThreeOut, ThreeOutSouth, ThreeOutEast }.Take(others).ToArray();
            var round = DefendingRound(bodies, enemyAt: H(-1, 0));
            round.EnemyAttacksBody(1);

            var total = round.DefendWithCrowdControl(out var preview);
            Assert.That(total, Is.EqualTo(1 + 2 * others), $"{others} other bodies within 3 of body 1.");
            Assert.That(preview.Total, Is.EqualTo(total), "the preview is the resolution's own value.");
            var text = preview.Contributions.Where(value => value.Kind == ValueContributionKind.CardTextModifier)
                .ToArray();
            Assert.That(text.Select(value => value.Amount),
                others == 0 ? Is.Empty : Is.EqualTo(new[] { 2 * others }),
                "the card text names itself as one contribution, or none when nothing stands in radius.");
        }

        [Test]
        public void CrowdControlsDefense_IsMeasuredFromTheAttackedBody_NotTheActingOne()
        {
            // Body 2 at (3, -3) is attacked from (4, -3). Around it: body 1 (three away) and body 3 at
            // (5, -5) (two away). Around body 1, the default and the acting body: body 2 only.
            var round = DefendingRound(new[] { ThreeOut, H(5, -5) }, enemyAt: H(4, -3));
            round.EnemyAttacksBody(2);
            Assert.That(round.DefendWithCrowdControl(out _), Is.EqualTo(1 + 2 * 2),
                "RZ-Q3: \"other you\" around the attacked body, which is itself excluded.");

            var fromBodyOne = DefendingRound(new[] { ThreeOut, H(5, -5) }, enemyAt: H(-1, 0));
            fromBodyOne.EnemyAttacksBody(1);
            Assert.That(fromBodyOne.DefendWithCrowdControl(out _), Is.EqualTo(1 + 2 * 1),
                "the same board attacked at body 1 counts only body 2.");
        }

        [Test]
        public void ABodyJustOutsideTheRadius_IsNotCounted_UntilARadiusItemWidensIt()
        {
            var round = DefendingRound(new[] { FourOut }, enemyAt: H(-1, 0));
            round.EnemyAttacksBody(1);
            Assert.That(round.DefendWithCrowdControl(out _), Is.EqualTo(1), "four spaces out is outside Radius 3.");

            var widened = DefendingRound(new[] { FourOut }, enemyAt: H(-1, 0), radiusItems: 1);
            widened.EnemyAttacksBody(1);
            Assert.That(widened.DefendWithCrowdControl(out _), Is.EqualTo(3),
                "R1: p.16's Radius item raises the printed icon the sentence reads.");
        }

        [Test]
        public void TheDefendQuestion_RestoresAndReplaysToTheSameTotalAndBytes()
        {
            var round = DefendingRound(new[] { ThreeOut, H(5, -5) }, enemyAt: H(4, -3));
            round.EnemyAttacksBody(2);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            var restored = round.Restore(bytes);
            var pending = round.Turn.PendingDefenseChoice;
            var resumed = restored.Round.CurrentTurnAuthority.PendingDefenseChoice;
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.LegalDefenseOffers, Is.EqualTo(pending.LegalDefenseOffers));
            Assert.That(restored.Round.CurrentTurnAuthority.PendingAttackResolution.Target,
                Is.EqualTo(AttackTarget.HeroBodyOf(Razzle, 2)));

            var live = round.DefendWithCrowdControl(out _);
            var replayed = AnswerCrowdControl(restored.Round);
            Assert.That(replayed, Is.EqualTo(live).And.EqualTo(5));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board)));
        }

        // --- Rummage ------------------------------------------------------------------------------

        [Test]
        public void Rummage_WithOneBody_IsOffered_AndItsFalseIfDoesNothing()
        {
            var round = new Round(face: RazzleCardContent.RummageDefinition, discarded: 2);
            Assert.That(round.IsOffered(RazzleCardContent.PrimaryAttack), Is.True,
                "the Attack is judged on its target; a false \"If\" is a legal do-nothing (RULING 53).");
            round.AttackTheMinionBesideHim();
            Assert.That(round.ActionEnded, Is.True, "no other you in radius: nothing is asked.");
            Assert.That(Discarded(round), Is.EqualTo(2));
        }

        [Test]
        public void Rummage_WithAnotherBodyInRadius_RetrievesTheChosenCard()
        {
            var round = TwoBodyRound(RazzleCardContent.RummageDefinition, ThreeOut, discarded: 2);
            round.AttackTheMinionBesideHim();
            Assert.That(round.PendingNode,
                Is.EqualTo(RazzleEffectContent.MayRetrieveNode(RazzleCardContent.Rummage, 1)), "the \"may\".");
            Accept(round);
            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.RetrieveNode(RazzleCardContent.Rummage, 1)));
            Assert.That(OfferedCards(round), Is.EqualTo(new[] { Round.DiscardedCard(1), Round.DiscardedCard(2) }));
            PickCard(round, Round.DiscardedCard(2));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.LocationOf(Round.DiscardedCard(2)), Is.EqualTo(CardLocation.Hand));
            Assert.That(Discarded(round), Is.EqualTo(1), "one retrieval: the \"If\" is not a count.");
        }

        [Test]
        public void Rummage_Declined_RetrievesNothing()
        {
            var round = TwoBodyRound(RazzleCardContent.RummageDefinition, ThreeOut, discarded: 1);
            round.AttackTheMinionBesideHim();
            round.Decline();
            Assert.That(round.ActionEnded, Is.True);
            Assert.That(Discarded(round), Is.EqualTo(1));
        }

        [Test]
        public void Rummage_WithNothingDiscarded_AsksNothing()
        {
            var round = TwoBodyRound(RazzleCardContent.RummageDefinition, ThreeOut, discarded: 0);
            Assert.That(round.IsOffered(RazzleCardContent.PrimaryAttack), Is.True);
            round.AttackTheMinionBesideHim();
            Assert.That(round.ActionEnded, Is.True, "a \"may\" with nothing to retrieve opens no question.");
        }

        [Test]
        public void Rummage_ABodyJustOutsideTheRadius_IsNotAnotherYouInRadius_UntilARadiusItem()
        {
            var round = TwoBodyRound(RazzleCardContent.RummageDefinition, FourOut, discarded: 1);
            round.AttackTheMinionBesideHim();
            Assert.That(round.ActionEnded, Is.True, "four spaces out: the \"If\" is false.");

            var widened = TwoBodyRound(RazzleCardContent.RummageDefinition, FourOut, discarded: 1, radiusItems: 1);
            widened.AttackTheMinionBesideHim();
            Assert.That(widened.PendingNode,
                Is.EqualTo(RazzleEffectContent.MayRetrieveNode(RazzleCardContent.Rummage, 1)),
                "R1: the Radius item widens \"in radius\".");
        }

        [Test]
        public void Rummage_IsMeasuredFromTheActingBody()
        {
            // Body 2 at (4, -4) is four from body 1; body 3 at (5, -4) stands beside body 2 only.
            // A minion stands beside each of body 1 and body 2.
            var bodies = new[] { FourOut, H(5, -4) };
            var minions = new[] { H(-1, 0), H(4, -3) };

            var fromOne = new Round(face: RazzleCardContent.RummageDefinition, bodiesAt: bodies,
                enemyMinionsAt: minions, discarded: 1);
            fromOne.PickActingBody(1);
            fromOne.StartPrimaryAttack();
            fromOne.Target(Round.EnemyMinion(1));
            Assert.That(fromOne.ActionEnded, Is.True, "no other you within 3 of body 1.");

            var fromTwo = new Round(face: RazzleCardContent.RummageDefinition, bodiesAt: bodies,
                enemyMinionsAt: minions, discarded: 1);
            fromTwo.PickActingBody(2);
            fromTwo.StartPrimaryAttack();
            fromTwo.Target(Round.EnemyMinion(2));
            Assert.That(fromTwo.PendingNode,
                Is.EqualTo(RazzleEffectContent.MayRetrieveNode(RazzleCardContent.Rummage, 1)),
                "body 3 stands beside body 2 (RZ-Q4: the acting body measures).");
        }

        // --- Ransack ------------------------------------------------------------------------------

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Ransack_RetrievesOncePerOtherBodyInRadius(int others)
        {
            var bodies = new[] { ThreeOut, ThreeOutSouth, ThreeOutEast }.Take(others).ToArray();
            var round = others == 0
                ? new Round(face: RazzleCardContent.RansackDefinition, discarded: 3)
                : TwoBodyRound(RazzleCardContent.RansackDefinition, bodies, discarded: 3);
            Assert.That(round.IsOffered(RazzleCardContent.PrimaryAttack), Is.True);
            round.AttackTheMinionBesideHim();

            for (var retrieval = 1; retrieval <= others; retrieval++)
            {
                Assert.That(round.PendingNode,
                    Is.EqualTo(RazzleEffectContent.MayRetrieveNode(RazzleCardContent.Ransack, retrieval)));
                Accept(round);
                PickCard(round, OfferedCards(round).First());
            }

            Assert.That(round.ActionEnded, Is.True, $"exactly {others} retrievals.");
            Assert.That(Discarded(round), Is.EqualTo(3 - others));
        }

        [Test]
        public void Ransack_EachRetrievalIsItsOwnMay()
        {
            var round = TwoBodyRound(RazzleCardContent.RansackDefinition,
                new[] { ThreeOut, ThreeOutSouth, ThreeOutEast }, discarded: 3);
            round.AttackTheMinionBesideHim();
            Accept(round);
            PickCard(round, Round.DiscardedCard(1));
            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.MayRetrieveNode(RazzleCardContent.Ransack, 2)));
            round.Decline();
            Assert.That(round.PendingNode, Is.EqualTo(RazzleEffectContent.MayRetrieveNode(RazzleCardContent.Ransack, 3)),
                "declining one retrieval leaves the next.");
            Accept(round);
            PickCard(round, Round.DiscardedCard(3));

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(round.LocationOf(Round.DiscardedCard(2)), Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(Discarded(round), Is.EqualTo(1));
        }

        [Test]
        public void Ransack_WithFewerDiscardsThanBodies_RetrievesWhatThereIs()
        {
            var round = TwoBodyRound(RazzleCardContent.RansackDefinition,
                new[] { ThreeOut, ThreeOutSouth, ThreeOutEast }, discarded: 1);
            round.AttackTheMinionBesideHim();
            Accept(round);
            PickCard(round, Round.DiscardedCard(1));
            Assert.That(round.ActionEnded, Is.True, "an empty discard asks no further \"may\".");
        }

        [Test]
        public void Ransack_CountsFromTheActingBody_OneFromBodyOne_TwoFromBodyTwo()
        {
            // Body 2 at (3, 0) is three from body 1; body 3 at (5, 0) is two from body 2, five from body 1.
            var bodies = new[] { ThreeOutEast, H(5, 0) };
            var minions = new[] { H(-1, 0), H(3, 1) };
            foreach (var (body, minion, expected) in new[] { (1, 1, 1), (2, 2, 2) })
            {
                var round = new Round(face: RazzleCardContent.RansackDefinition, bodiesAt: bodies,
                    enemyMinionsAt: minions, discarded: 3);
                round.PickActingBody(body);
                round.StartPrimaryAttack();
                round.Target(Round.EnemyMinion(minion));
                var retrieved = 0;
                while (round.PendingNode != null)
                {
                    Accept(round);
                    PickCard(round, OfferedCards(round).First());
                    retrieved++;
                }

                Assert.That(retrieved, Is.EqualTo(expected), $"from body {body}");
            }
        }

        [Test]
        public void EveryOfferedCard_IsRetrievedWhenAnswered()
        {
            var round = TwoBodyRound(RazzleCardContent.RansackDefinition, new[] { ThreeOut }, discarded: 3);
            round.AttackTheMinionBesideHim();
            Accept(round);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            var offered = OfferedCards(round);
            Assert.That(offered, Has.Length.EqualTo(3));
            foreach (var card in offered)
            {
                var copy = round.Restore(bytes);
                PickCard(copy.Round, card);
                Assert.That(copy.Round.CurrentTurnAuthority.PendingEffectChoice, Is.Null, $"{card} closes it");
                Assert.That(copy.Cards.Single(value => value.Id.Equals(card)).State.Location,
                    Is.EqualTo(CardLocation.Hand), $"{card} is back in hand");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void APausedRansack_RestoresAndReplaysToTheSameBytes(bool atTheCardPick)
        {
            var round = TwoBodyRound(RazzleCardContent.RansackDefinition,
                new[] { ThreeOut, ThreeOutSouth }, discarded: 3);
            round.AttackTheMinionBesideHim();
            Accept(round);
            PickCard(round, Round.DiscardedCard(1));
            if (atTheCardPick)
            {
                Accept(round);
            }

            var pending = round.Turn.PendingEffectChoice;
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board);
            var restored = round.Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
            Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));

            foreach (var target in new[] { round.Authority, restored.Round })
            {
                if (!atTheCardPick)
                {
                    Accept(target);
                }

                PickCard(target, Round.DiscardedCard(3));
            }

            Assert.That(round.ActionEnded, Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board)));

            var prefix = round.Restore(bytes);
            if (!atTheCardPick)
            {
                Accept(prefix.Round);
            }

            PickCard(prefix.Round, Round.DiscardedCard(3));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round.Authority, round.Board)));
        }

        // --- The capability -----------------------------------------------------------------------

        [Test]
        public void TheCountingRead_IsReadOnlyAndAnswersAnOutcomeToTheOfferWalk()
        {
            var handler = SyntheticHeroBodyCountCapabilities.ActorOtherBodiesInRadiusHandler(
                RazzleEffectContent.OtherBodiesInRadiusV1);
            Assert.That(handler, Is.InstanceOf<IEffectAnticipatedOutcomeCapabilityHandler>(),
                "the offer walk reads what the gate decides (milestone 9.6).");
            Assert.That(((IEffectAnticipatedOutcomeCapabilityHandler)handler).PublishesWithoutApplying, Is.True);
            Assert.That(RazzleEffectContent.Handlers().Select(value => value.Reference),
                Does.Contain(RazzleEffectContent.OtherBodiesInRadiusV1));
        }

        // --- Staging ------------------------------------------------------------------------------

        /// <summary>
        /// Razzle with Crowd Control in hand: he holds his turn (picking body 1 when several stand),
        /// and the enemy, beside the body it will attack, strikes (Attack 9).
        /// </summary>
        private static DefenceRound DefendingRound(
            IReadOnlyList<HexCoordinate> bodies, HexCoordinate enemyAt, int radiusItems = 0) =>
            new DefenceRound(new Round(bodiesAt: bodies, enemyMinionsAt: Array.Empty<HexCoordinate>(),
                enemyAt: enemyAt, enemyStrikes: true, radiusItems: radiusItems,
                hand: new[] { RazzleCardContent.CrowdControlDefinition }));

        private sealed class DefenceRound
        {
            private readonly Round round;

            public DefenceRound(Round round)
            {
                this.round = round;
                if (round.Bodies().Length > 1)
                {
                    round.PickActingBody(1);
                }

                Assert.That(round.Authority.TryPerformHold(round.Source).Accepted, Is.True);
                round.ReleaseTurn();
            }

            public RoundResolutionAuthority Authority => round.Authority;
            public GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority Board => round.Board;
            public TurnResolutionAuthority Turn => round.Turn;

            public GuardsDigital.Core.Snapshots.RestoredAuthoritativeMatch Restore(string bytes) => round.Restore(bytes);

            public void EnemyAttacksBody(int body)
            {
                round.EnemyStrikes();
                round.TargetBody(body);
                Assert.That(round.Turn.PendingDefenseChoice, Is.Not.Null, "p.14's Defense question is open");
            }

            /// <summary>
            /// The Defense preview (the value <c>MatchSession</c>'s preview and the resolution both ask
            /// of <see cref="DefenseValueQuery"/>, handed the attacked body), then the answer and the
            /// Defense Total the resolution recorded.
            /// </summary>
            public int DefendWithCrowdControl(out ValueResult preview)
            {
                var defense = round.Turn.PendingDefenseChoice;
                var offer = CrowdControlOffer(round.Turn);
                preview = DefenseValueQuery.Calculate(
                    offer, round.Definitions, Razzle,
                    round.Turn.PendingAttackResolution.Target.HeroBody,
                    StagedBoardParticipants.Create(), round.Board, RuleModificationSet.Empty,
                    round.Authority.HeroItems);
                Assert.That(defense, Is.Not.Null);
                return AnswerCrowdControl(round.Authority);
            }
        }

        private static ActionOffer CrowdControlOffer(TurnResolutionAuthority turn) =>
            turn.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                offer.SourceCardInstanceId.Equals(Round.HandCard(1)) &&
                offer.ActionDefinitionId.Equals(RazzleCardContent.PrimaryDefense));

        private static int AnswerCrowdControl(RoundResolutionAuthority target)
        {
            var defense = target.CurrentTurnAuthority.PendingDefenseChoice;
            var offer = CrowdControlOffer(target.CurrentTurnAuthority);
            var answered = target.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                defense.ResolutionId, defense.ChoiceId, offer.SourceCardInstanceId));
            Assert.That(answered.Accepted, Is.True);
            return answered.Events
                .Select(value => value.TurnEvent.CardActionEvent)
                .Single(value => value.Kind == CardActionResolutionEventKind.DefenseTotalCalculated)
                .DefenseTotal;
        }

        private static Round TwoBodyRound(
            CardDefinition face, HexCoordinate other, int discarded, int radiusItems = 0) =>
            TwoBodyRound(face, new[] { other }, discarded, radiusItems);

        private static Round TwoBodyRound(
            CardDefinition face, IReadOnlyList<HexCoordinate> others, int discarded, int radiusItems = 0)
        {
            var round = new Round(face: face, bodiesAt: others, discarded: discarded, radiusItems: radiusItems);
            round.PickActingBody(1);
            return round;
        }

        private static int Discarded(Round round) =>
            Enumerable.Range(1, 3).Count(index => round.HasCard(Round.DiscardedCard(index)) &&
                round.LocationOf(Round.DiscardedCard(index)) == CardLocation.DiscardPile);

        private static CardInstanceId[] OfferedCards(Round round) =>
            round.Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => candidate.Value.CardInstanceId).ToArray();

        private static void Accept(Round round) => Accept(round.Authority);

        /// <summary>Takes the open "you may" (p.10's optional step).</summary>
        private static void Accept(RoundResolutionAuthority target)
        {
            var asked = target.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, "the \"may\" is open");
            Assert.That(asked.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            var take = asked.ConfirmInvocation ?? asked.SelectInvocations.Single();
            Assert.That(target.TryAnswerEffectChoice(take).Accepted, Is.True);
        }

        private static void PickCard(Round round, CardInstanceId card) => PickCard(round.Authority, card);

        private static void PickCard(RoundResolutionAuthority target, CardInstanceId card)
        {
            var asked = target.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, "the retrieval asks which card");
            Assert.That(target.TryAnswerEffectChoice(asked.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Equals(EffectValue.Card(card)))).Accepted, Is.True);
            var still = target.CurrentTurnAuthority.PendingEffectChoice;
            if (still != null && still.ChoiceId.Equals(asked.ChoiceId) && still.ConfirmInvocation != null)
            {
                Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
            }
        }

        private static ActionDefinition Attack(CardDefinition face) =>
            face.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);

        private static void AssertCountingAttack(
            CardDefinition face, int initiative, int attack, CardTier tier, PrintedValueChannel item, string text)
        {
            Assert.That(face.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(face.ContentRevision, Is.EqualTo(RazzleCardContent.ContentRevision));
            Assert.That((face.Progression.Color, face.Progression.Tier, face.Progression.ItemIcon),
                Is.EqualTo((CardColor.Red, tier, item)));
            Assert.That(face.SourceContent.PrintedText, Is.EqualTo(text));
            var primary = Attack(face);
            Assert.That(primary.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(primary.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged),
                "p.12: adjacent, even with a Radius icon.");
            Assert.That(primary.BaseValues.Select(value => (value.Channel, value.Amount)),
                Is.EqualTo(new[] { (PrintedValueChannel.Attack, attack), (PrintedValueChannel.Radius, 3) }));
            Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(Secondary(face, ActionKind.Movement), Is.EqualTo(4));
            Assert.That(Secondary(face, ActionKind.Defense), Is.EqualTo(4));
        }

        private static void AssertGatedRetrievals(CardDefinition face, int gates)
        {
            var text = Attack(face).EffectRoot;
            var root = text.Nodes.Single(node => node.Id.Equals(text.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Sequence));
            Assert.That(root.ChildNodeIds, Has.Count.EqualTo(gates));
            for (var index = 1; index <= gates; index++)
            {
                var gate = text.Nodes.Single(node => node.Id.Equals(root.ChildNodeIds[index - 1]));
                Assert.That(gate.Kind, Is.EqualTo(EffectNodeKind.Conditional), "\"If\" / \"for every\"");
                Assert.That(gate.Capability, Is.EqualTo(RazzleEffectContent.OtherBodiesInRadiusV1),
                    "one generic count of the actor's other bodies, never a per-card capability.");
                Assert.That(gate.Inputs.Single().Literal, Is.EqualTo(EffectValue.Integer(index)));
                Assert.That(gate.ChildNodeIds, Has.Count.EqualTo(1),
                    "no else-child: a false gate is RULING 53's legal do-nothing.");

                var may = text.Nodes.Single(node => node.Id.Equals(gate.ChildNodeIds[0]));
                Assert.That((may.Kind, may.FailurePolicy), Is.EqualTo((EffectNodeKind.Optional, EffectFailurePolicy.May)),
                    "\"you may\"");
                var retrieve = text.Nodes.Single(node => node.Id.Equals(may.ChildNodeIds.Single()));
                Assert.That(retrieve.Capability, Is.EqualTo(SyntheticRosterCapabilities.RetrieveCardV1),
                    "the roster's retrieval, as every \"retrieve a discarded card\" composes.");
                Assert.That(retrieve.Inputs.Single(input => input.ParameterName == "kind").Literal,
                    Is.EqualTo(EffectValue.Integer((int)RosterCardRetrievalKind.OwnedDiscardedCard)));
            }
        }

        private static int Secondary(CardDefinition face, ActionKind kind) =>
            face.ActionDefinitions.Single(action => action.Kind == kind &&
                action.Origin != ActionDefinitionOrigin.PrintedPrimary).BaseValues.Single().Amount;
    }
}
