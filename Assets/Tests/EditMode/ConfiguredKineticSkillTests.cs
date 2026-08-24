using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.8: Wasp's two kinetic Skills resolve their complete printed text, discard rider
    /// included.
    ///
    /// <para>"Push up to 2 enemy units adjacent to you 3 spaces; if a pushed hero is stopped by an
    /// obstacle, that hero discards a card, if able." Kinetic Blast prints the same sentence with "3 or
    /// 4 spaces", and that is the whole of the difference between them - which is why the compositions
    /// below differ in two integers and nothing else.</para>
    ///
    /// <para>The capability-level cases live in <c>DisplacementCapabilityTests</c>, where the batch's
    /// new declarations are exercised on content that is nobody's card, exactly as every general seam in
    /// this project has been since 6.4. What is here is Wasp: the composition, and the two Skills run
    /// through a real <see cref="MatchSession"/> to the card the pushed hero gives up.</para>
    /// </summary>
    public sealed class ConfiguredKineticSkillTests
    {
        // ---------------------------------------------------------------------------------------
        // The compositions themselves
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BothKineticSkills_ComposeOneBatchPushOfUpToTwoAdjacentEnemyUnits()
        {
            foreach (var cardId in new[]
            {
                WaspCardContent.KineticRepulse, WaspCardContent.KineticBlast
            })
            {
                var push = PushNode(cardId);

                Assert.That(SkillEffect(cardId).Root.Kind, Is.EqualTo(EffectNodeKind.Sequence),
                    cardId + " prints one sentence and then a rider, in that order.");
                Assert.That(push.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    "p.10 makes `up to` non-mandatory, and here it is over the units.");
                Assert.That(
                    Input(push, SyntheticDisplacementCapabilities.MaximumTargetsParameterName),
                    Is.EqualTo(2), "`up to 2`.");
                Assert.That(
                    Input(push, SyntheticExchangeCapabilities.EligiblePieceParameterName),
                    Is.EqualTo((int)PushEligiblePieceKind.Unit),
                    "`units`, which p.19 separates from tokens by name.");
                Assert.That(
                    Input(push, SyntheticDisplacementCapabilities.TargetRelationParameterName),
                    Is.EqualTo((int)SourceTeamRelation.OpposingSource),
                    "`enemy`, read from the card's perspective, which is the acting hero's.");
                Assert.That(
                    push.Inputs.Single(input => input.ParameterName == "ignoresObstacles")
                        .Literal.BooleanValue,
                    Is.False,
                    "Neither card says `ignoring obstacles`, and p.18 makes that the effect's own " +
                    "property rather than a default.");
            }
        }

        [Test]
        public void OnlyKineticBlast_WritesItsDistanceAsAChoice()
        {
            Assert.That(
                new[] { WaspCardContent.KineticRepulse, WaspCardContent.KineticBlast }
                    .Select(cardId => cardId.Value + "=" +
                        Input(PushNode(cardId),
                            SyntheticDisplacementCapabilities.LeastDistanceParameterName) +
                        "-" + Input(PushNode(cardId), "distance")),
                Is.EqualTo(new[]
                {
                    "wasp-kinetic-repulse=3-3",
                    "wasp-kinetic-blast=3-4"
                }),
                "`3 spaces` names one number and `3 or 4 spaces` names two; the upgrade's whole " +
                "difference from the card below it is the second number.");
        }

        [Test]
        public void TheDiscardRider_IsOneConditionalPerPushedHero_OverTheFactThePushPublishes()
        {
            foreach (var cardId in new[]
            {
                WaspCardContent.KineticRepulse, WaspCardContent.KineticBlast
            })
            {
                var effect = SkillEffect(cardId);
                var push = PushNode(cardId);
                var riders = effect.Nodes
                    .Where(node => node.Kind == EffectNodeKind.Conditional)
                    .ToArray();

                Assert.That(riders, Has.Length.EqualTo(2),
                    cardId + ": the sentence is about a pushed hero, and the card pushes up to two.");
                Assert.That(
                    riders.Select(rider => rider.Inputs.Single().SourceOutputName),
                    Is.EqualTo(new[]
                    {
                        SyntheticDisplacementCapabilities.FirstHeroWasStoppedOutputName,
                        SyntheticDisplacementCapabilities.SecondHeroWasStoppedOutputName
                    }),
                    "Each rider reads the stop cause the push already decided, so the board's edge is " +
                    "judged once rather than by two riders that could disagree.");
                Assert.That(
                    riders.All(rider => rider.Inputs.Single().SourceNodeId.Equals(push.Id)),
                    Is.True);

                foreach (var rider in riders)
                {
                    var discard = effect.GetNode(rider.ChildNodeIds.Single());
                    Assert.That(rider.ChildNodeIds, Has.Count.EqualTo(1),
                        "The card prints no else-branch: a hero nothing stopped simply does nothing.");
                    Assert.That(
                        discard.Capability,
                        Is.EqualTo(SyntheticCardStateCapabilities.DiscardChosenCardV1),
                        "`that hero discards a card` is milestone 7.1a's in-hand discard, whose " +
                        "chooser is the hero it names rather than the actor.");
                    Assert.That(discard.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                        "`if able` is honoured at the node: an empty hand gives nothing and the " +
                        "action goes on.");
                    Assert.That(
                        discard.Inputs.Single().SourceNodeId, Is.EqualTo(push.Id),
                        "and the hero who discards is the one the push named, not a role.");
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // Kinetic Repulse, resolved
        // ---------------------------------------------------------------------------------------

        [Test]
        public void KineticRepulse_PushesTheChosenEnemyHeroThreeSpacesAwayFromWasp()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticRepulse, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticRepulse);
            var actorAt = HeroSpace(session, TeamSide.Red);
            var before = HeroSpace(session, TeamSide.Blue);

            StartSkill(session);
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.Value.Coordinate),
                Is.EqualTo(new[] { before }),
                "The one enemy unit standing beside her is the whole of what `up to 2 enemy units " +
                "adjacent to you` offers in this position.");
            Assert.That(choice.MinimumSelections, Is.EqualTo(0));
            Assert.That(choice.ConfirmInvocation, Is.Not.Null,
                "`up to 2` includes none, so the decision may be closed with nothing named.");

            Answer(session, choice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "`3 spaces` is one number, so nobody is asked how far.");
            Assert.That(HeroSpace(session, TeamSide.Red), Is.EqualTo(actorAt),
                "Wasp herself never moves; p.18 pushes directly away from her.");
            Assert.That(
                HexDistanceQuery.Calculate(actorAt, HeroSpace(session, TeamSide.Blue)),
                Is.EqualTo(4),
                "The target began adjacent and travelled the three spaces the card names, along the " +
                "one file p.18 gives it.");
            Assert.That(
                StraightLineQuery.AreInStraightLine(actorAt, HeroSpace(session, TeamSide.Blue)),
                Is.True,
                "and it is the same file it started on.");
        }

        [Test]
        public void AHeroStoppedByAnObstacle_DiscardsACardOfTheirOwnChoosing()
        {
            // The one case the whole milestone is for: p.18's push produces a rules fact, the card's
            // second sentence reads it, and the hero it happened to answers - which is p.10's own
            // worked example, where the actor picks the hero and the hero picks the card.
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticRepulse, stoppedShort: true);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticRepulse);
            var actorAt = HeroSpace(session, TeamSide.Red);
            var before = HeroSpace(session, TeamSide.Blue);
            var blueHeroId = HeroId(session, TeamSide.Blue);
            var handBefore = HandCount(session, blueHeroId);

            StartSkill(session);
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var after = HeroSpace(session, TeamSide.Blue);
            Assert.That(
                HexDistanceQuery.Calculate(actorAt, after), Is.LessThan(4),
                "The fixture's line runs into the map's terrain ring inside three spaces, so the " +
                "push is short - which is the position the rider is about.");
            Assert.That(after, Is.Not.EqualTo(before), "It still travelled as far as it could.");

            var discard = session.Projection.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null,
                "An obstacle stopped a pushed hero, so the card's second sentence applies.");
            Assert.That(discard.ChooserId, Is.EqualTo(blueHeroId),
                "The actor's card names no chooser for the card, so it is the stopped hero's own " +
                "decision, out of their own hand.");
            Assert.That(discard.NodeId.Value, Is.EqualTo("kinetic-first-discard"));
            Assert.That(
                discard.SelectInvocations.Select(invocation =>
                    invocation.Candidate.Value.CardInstanceId),
                Is.EquivalentTo(HandCards(session, blueHeroId).Select(card => card.Id)),
                "and the candidates are that hero's own hand, read through the same query with the " +
                "same suppression the hero would see.");

            var chosen = discard.SelectInvocations.First().Candidate.Value.CardInstanceId;
            Answer(session, discard.SelectInvocations.First());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(chosen)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(HandCount(session, blueHeroId), Is.EqualTo(handBefore - 1));
            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "The second rider reads a stop cause for a hero this push never had, so it opens " +
                "nothing at all.");
        }

        [Test]
        public void AHeroWhoTravelledTheWholeDistance_IsAskedForNoCard()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticRepulse, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticRepulse);
            var blueHeroId = HeroId(session, TeamSide.Blue);
            var handBefore = HandCount(session, blueHeroId);

            StartSkill(session);
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Nothing stopped this push, so the rider's predicate reads false and the branch " +
                "behind it never runs.");
            Assert.That(HandCount(session, blueHeroId), Is.EqualTo(handBefore),
                "and the hero it would have addressed keeps every card.");
        }

        [Test]
        public void PushingNobodyAtAll_IsALegalResultThatEndsTheAction()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticRepulse, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticRepulse);
            var before = HeroSpace(session, TeamSide.Blue);

            StartSkill(session);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(HeroSpace(session, TeamSide.Blue), Is.EqualTo(before),
                "p.10: an `up to` step nobody takes is not taken.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "and the action is over: the riders have no stopped hero to read.");
        }

        // ---------------------------------------------------------------------------------------
        // Kinetic Blast, resolved
        // ---------------------------------------------------------------------------------------

        [Test]
        public void KineticBlast_AsksForOneDistanceOutOfThreeOrFourAfterNamingItsUnits()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticBlast, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticBlast);
            var actorAt = HeroSpace(session, TeamSide.Red);

            StartSkill(session);
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var distance = session.Projection.PendingEffectChoice;
            Assert.That(distance, Is.Not.Null,
                "p.18: with the units settled, the distance is the one decision left.");
            Assert.That(
                distance.SelectInvocations.Select(invocation =>
                    invocation.Candidate.Value.IntegerValue),
                Is.EqualTo(new[] { 3, 4 }),
                "The card names two numbers and nothing between them and zero.");

            Answer(
                session,
                distance.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.IntegerValue == 4));
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(
                HexDistanceQuery.Calculate(actorAt, HeroSpace(session, TeamSide.Blue)),
                Is.EqualTo(5),
                "The unit the first answer named travelled the distance the second one chose.");
        }

        /// <summary>
        /// Milestone 8.20a. The printed `up to` is over the units, and the distance is exact - so the
        /// second question this one node asks keeps the floor its handler set, and cannot be closed or
        /// refused with nothing named.
        ///
        /// <para>The milestone that added it needed the opposite for the node's <em>first</em>
        /// question: a printed `may` had no way to be declined at all, and a card that says "you may"
        /// must be refusable. Making that true node-wide would have reached here too, because a node
        /// carries one failure policy and this is the one node in verified content that asks twice.
        /// The floor is therefore keyed on the question rather than on the node, and this is the case
        /// that says so.</para>
        /// </summary>
        [Test]
        public void KineticBlastsDistance_KeepsItsOwnFloorAndCannotBeClosedWithNothingNamed()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticBlast, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticBlast);

            StartSkill(session);
            var units = session.Projection.PendingEffectChoice;
            Assert.That(units.MinimumSelections, Is.EqualTo(0),
                "\"up to\" is over the units, so naming none of them is an answer.");
            Assert.That(units.DeclineInvocation, Is.Not.Null,
                "and refusing the whole optional node is another.");

            Answer(session, units.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var distance = session.Projection.PendingEffectChoice;
            Assert.That(distance, Is.Not.Null,
                "p.18: with the units settled, the distance is the one decision left.");
            Assert.That(distance.MinimumSelections, Is.EqualTo(1),
                "The card names 3 or 4 and nothing between them and zero, so the follow-up question " +
                "keeps its own floor rather than inheriting the node's printed optionality.");
            Assert.That(distance.ConfirmInvocation, Is.Null,
                "so it cannot be closed before a number is named,");
            Assert.That(distance.DeclineInvocation, Is.Null,
                "and it cannot be refused - the player took the option when they named the unit.");
        }

        [Test]
        public void ThePauseBetweenTheTwoQuestions_SurvivesASnapshotUndoAndAReplay()
        {
            // Kinetic Blast is the first content in this build whose capability asks twice, so the
            // answer to the first question has to be somewhere a restore can find it. It is: the
            // resolution's own accepted choices, which the frame already carried and the payload
            // already wrote. Nothing new is persisted, and this is what says so.
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticBlast, stoppedShort: false);
            CommitAgainstStopProjectiles(session, WaspCardContent.KineticBlast);
            var actorAt = HeroSpace(session, TeamSide.Red);

            StartSkill(session);
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations.Single());
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);
            var betweenQuestions = session.CaptureSnapshot();

            Answer(
                session,
                session.Projection.PendingEffectChoice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.IntegerValue == 3));
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);
            var afterThePush = session.CaptureSnapshot();
            var pushedTo = HeroSpace(session, TeamSide.Blue);
            Assert.That(HexDistanceQuery.Calculate(actorAt, pushedTo), Is.EqualTo(4));

            // Two commands closed the distance question - naming a number and confirming it - so two
            // are undone to stand between the questions again.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(betweenQuestions),
                "Retained-prefix Undo puts the resolution back between its two questions.");

            var restored = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticBlast, stoppedShort: false);
            Assert.That(restored.RestoreSnapshot(betweenQuestions).Accepted, Is.True);
            Assert.That(
                restored.Projection.PendingEffectChoice.SelectInvocations.Select(invocation =>
                    invocation.Candidate.Value.IntegerValue),
                Is.EqualTo(new[] { 3, 4 }),
                "The restored pause is the distance question, re-derived rather than replayed from a " +
                "stored candidate list.");

            Answer(
                restored,
                restored.Projection.PendingEffectChoice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.IntegerValue == 3));
            Answer(restored, restored.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(HeroSpace(restored, TeamSide.Blue), Is.EqualTo(pushedTo),
                "and the same unit is pushed, because the answer the first question gave came back " +
                "with the frame.");
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(afterThePush));
        }

        // ---------------------------------------------------------------------------------------
        // The two primaries are no longer greyed
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BothKineticPrimaries_NoLongerOweACompositionAndAreNoLongerGreyedForOne()
        {
            foreach (var cardId in new[]
            {
                WaspCardContent.KineticRepulse, WaspCardContent.KineticBlast
            })
            {
                var session = ConfiguredMatchFixtures.CreateKineticSession(cardId, stoppedShort: true);
                CommitAgainstStopProjectiles(session, cardId);
                var offer = SkillOffer(session);

                Assert.That(Action(session, offer).TextComposition.IsAwaited, Is.False, cardId.Value);
                Assert.That(Action(session, offer).EffectRoot, Is.Not.Null, cardId.Value);
                Assert.That(offer.IsAvailable, Is.True,
                    cardId + " owes no composition and has an enemy hero standing beside Wasp to " +
                    "push, so DEV-006's honesty rule no longer greys it.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static EffectDefinition SkillEffect(CardDefinitionId cardId) =>
            WaspCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Id.Equals(WaspCardContent.PrimarySkill))
                .EffectRoot;

        private static EffectNodeDefinition PushNode(CardDefinitionId cardId) =>
            SkillEffect(cardId).Nodes.Single(node =>
                node.Capability.Equals(SyntheticDisplacementCapabilities.ObjectPushV3));

        private static int Input(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal.IntegerValue;

        private static ActionDefinition Action(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        /// <summary>
        /// Red commits the named card and Blue commits Stop Projectiles, whose printed Initiative 3 is
        /// the lowest in the starting hand, so the Red card acts first on its printed value alone.
        /// </summary>
        private static void CommitAgainstStopProjectiles(MatchSession session, CardDefinitionId cardId)
        {
            var red = HandCard(session, TeamSide.Red, cardId);
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The tested card has to be the one acting, or every assertion below is about Blue.");
        }

        private static ActionOffer SkillOffer(MatchSession session) =>
            session.Projection.AvailableTurnActions.Single(candidate =>
                Action(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                Action(session, candidate).Kind == ActionKind.Skill);

        private static void StartSkill(MatchSession session)
        {
            var offer = SkillOffer(session);
            Assert.That(offer.IsAvailable, Is.True,
                "The composed Skill must be takeable in this position, or the case holds over nothing.");
            Assert.That(session.Execute(
                DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted, Is.True);
        }

        private static void Answer(MatchSession session, EffectChoiceCommand invocation) =>
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(invocation));

        private static HexCoordinate HeroSpace(MatchSession session, TeamSide side) =>
            session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side == side)
                .Coordinate;

        private static HeroId HeroId(MatchSession session, TeamSide side) =>
            session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side == side)
                .HeroId;

        private static CardInstance HandCard(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));

        private static CardInstance[] HandCards(MatchSession session, HeroId ownerId) =>
            session.Projection.Cards
                .Where(card =>
                    card.OwnerId.Equals(ownerId) &&
                    card.State.Location == CardLocation.Hand &&
                    !card.State.HasBeenPlayed)
                .ToArray();

        private static int HandCount(MatchSession session, HeroId ownerId) =>
            HandCards(session, ownerId).Length;
    }
}
