using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.6's two fixture Skills, played in a real match: the orbit card, whose repeat may ask
    /// twice and binds one target across all three instances, and the kinesis card, whose printed-Range
    /// selection excludes everything in a straight line from the actor and whose Place puts the piece
    /// beside the actor rather than beside itself.
    ///
    /// <para>They exist so that this milestone's four new declarations are proven on content that is
    /// nobody's card before Wasp composes them, which is the discipline every general seam here has
    /// followed since 6.4. Two of the four are only observable on this side: Wasp's repeat count above
    /// one is printed on Center of Mass and her straight-line complement on Telekinesis, and both are
    /// Tier II or III cards that p.6's setup leaves in the deck - so a configured match cannot play
    /// either until p.15's Level Up puts one in a hand, which is milestone 8.14's business.
    /// <c>ConfiguredTelekineticSkillTests</c> holds what her starting five can show.</para>
    /// </summary>
    public sealed class ConfiguredOrbitAndKinesisSkillTests
    {
        // ---------------------------------------------------------------------------------------
        // The bounded repeat
        // ---------------------------------------------------------------------------------------

        [Test]
        public void ARepeatDeclaringTwo_RunsThreeInstancesAndAsksTheThirdNothing()
        {
            var session = StartOrbit(out var actorAt);
            var subject = HeroSpace(session, TeamSide.Blue);

            // First instance: which piece, then where it orbits to, then p.10's confirm-or-decline.
            NameSubject(session, subject);
            var firstTo = OrbitDestination(session);
            Assert.That(session.Projection.PendingEffectChoice.Kind,
                Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            // Second instance: no subject question at all, then the repeat this milestone made
            // expressible - `up to two times` is the first declared maximum above one in this build.
            Assert.That(session.Projection.PendingSkillResolution.InvocationContext,
                Is.EqualTo(ActionInvocationContext.Repeated));
            Assert.That(HeroSpace(session, TeamSide.Blue), Is.EqualTo(firstTo));
            var secondTo = OrbitDestination(session);
            Assert.That(session.Projection.PendingEffectChoice.Kind,
                Is.EqualTo(EffectChoiceKind.ConfirmOrDecline),
                "A repeat that has run once of two may still ask; before this milestone it could not.");
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            // Third instance: the destination, and then the chain simply ends.
            Assert.That(HeroSpace(session, TeamSide.Blue), Is.EqualTo(secondTo));
            var thirdTo = OrbitDestination(session);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "Three instances, two repeats, and the third is offered none - which is what the " +
                "declared maximum means and what the structural bound before it could not express.");
            Assert.That(HeroSpace(session, TeamSide.Blue), Is.EqualTo(thirdTo));
            Assert.That(HexDistanceQuery.Calculate(actorAt, thirdTo),
                Is.EqualTo(HexDistanceQuery.Calculate(actorAt, subject)),
                "and every instance kept the piece on the ring it started on.");
        }

        [Test]
        public void EachRepeatedInstance_BindsTheSamePieceRatherThanTheSpaceItLeft()
        {
            var session = StartOrbit(out _);
            var subject = HeroSpace(session, TeamSide.Blue);
            NameSubject(session, subject);
            var firstTo = OrbitDestination(session);
            Assert.That(firstTo, Is.Not.EqualTo(subject), "The orbit has to have moved it.");
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingEffectChoice.NodeId.Value, Is.EqualTo("orbit-move"),
                "`on the same target` leaves the repeated instance nothing to choose about the " +
                "subject, so it opens on the destination.");
            Assert.That(
                session.Projection.PendingEffectChoice.SelectInvocations
                    .Select(invocation => invocation.Candidate.Value.Coordinate)
                    .All(space => HexDistanceQuery.Calculate(firstTo, space) == 1),
                Is.True,
                "and every space it offers is one step from where the piece now stands - which is " +
                "the whole reason the binding names the piece and not the space it was standing on.");
        }

        // ---------------------------------------------------------------------------------------
        // The printed-Range selection and its straight-line complement
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheKinesisSelection_ExcludesEveryPieceInAStraightLineIncludingTheAdjacentOne()
        {
            var session = ConfiguredMatchFixtures.CreateKinesisSession(out var outOfLine);
            var actorAt = StartKinesis(session);
            var minionAt = MinionSpace(session, outOfLine);
            var enemyHeroAt = HeroSpace(session, TeamSide.Blue);

            var offered = session.Projection.PendingEffectChoice.SelectInvocations
                .Select(invocation => invocation.Candidate.Value.Coordinate)
                .ToArray();

            Assert.That(offered, Has.Member(minionAt),
                "The minion is inside the printed Range 3 and on no file through the actor.");
            Assert.That(offered, Has.No.Member(enemyHeroAt),
                "The enemy hero is adjacent, and p.19 says adjacent units are in a straight line as " +
                "well - so a card that excludes straight lines excludes the piece standing next to it.");
            // The acting card's own icon, not either hand's: both sides hold a kinesis card, and p.9
            // puts the bound on the card being played.
            var acting = session.Projection.Cards.Single(card =>
                card.Id.Equals(session.Projection.SelectedActor.CardInstanceId));
            var printedRange = session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(acting.DefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(new ActionDefinitionId("primary-kinesis-skill")))
                .BaseValues.Single(value => value.Channel == PrintedValueChannel.Range).Amount;
            Assert.That(
                offered.All(space =>
                    !StraightLineQuery.AreInStraightLine(actorAt, space) &&
                    HexDistanceQuery.Calculate(actorAt, space) <= printedRange),
                Is.True,
                "and nothing offered is either in line or beyond the icon printed on the card, which " +
                "is where p.9 puts the bound rather than in the node table.");
        }

        [Test]
        public void TheKinesisPlace_OffersOnlyEmptySpacesAdjacentToTheActorAndPutsThePieceThere()
        {
            var session = ConfiguredMatchFixtures.CreateKinesisSession(out var outOfLine);
            var actorAt = StartKinesis(session);
            var minionAt = MinionSpace(session, outOfLine);
            Assert.That(HexDistanceQuery.Calculate(actorAt, minionAt), Is.GreaterThan(1),
                "The piece starts more than one space away, or `adjacent to you` proves nothing.");

            NameSubject(session, minionAt);

            var destinations = session.Projection.PendingEffectChoice.SelectInvocations
                .Select(invocation => invocation.Candidate.Value.Coordinate)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(actorAt, space) == 1), Is.True,
                "Every destination is measured from the actor, not from the piece - which is the " +
                "declaration milestone 8.6 added to the Place, and no radius around the piece could " +
                "have expressed it.");

            var chosen = destinations[0];
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            // Where the piece ends up is not necessarily where the Place put it, and that is p.18
            // working rather than the Place failing: a minion miniature displaced out of the Battle
            // Zone is returned by the after-action correction, which runs once this action's own nodes
            // are finished. So the claim is that the Place landed on the space the actor's own
            // adjacency admitted - either it is still standing there, or the correction picked it up
            // from there.
            var landed = MinionSpace(session, outOfLine);
            Assert.That(
                landed.Equals(chosen) ||
                session.Projection.DiagnosticEvents.Any(entry =>
                    entry.EventType.StartsWith("Action.DisplacedMinionCorrection", StringComparison.Ordinal) &&
                    entry.Detail.Contains("correction=" + chosen + "->")),
                Is.True,
                "The piece was placed on the chosen space beside the actor; if it is elsewhere now, " +
                "p.18's after-action correction is what moved it and says so.");
            Assert.That(HeroSpace(session, TeamSide.Red), Is.EqualTo(actorAt),
                "Placing is not movement (p.18) and the actor never moved at all.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static MatchSession StartOrbit(out HexCoordinate actorAt)
        {
            var session = ConfiguredMatchFixtures.CreateOrbitSession();
            actorAt = Start(session, "orbit", new ActionDefinitionId("primary-orbit-skill"));
            return session;
        }

        private static HexCoordinate StartKinesis(MatchSession session) =>
            Start(session, "kinesis", new ActionDefinitionId("primary-kinesis-skill"));

        /// <summary>
        /// Both sides commit the fixture card and the actor starts it. The Red card's Initiative is
        /// above every Blue one in these hands, so Red acts on its printed value alone.
        /// </summary>
        private static HexCoordinate Start(
            MatchSession session, string cardIdFragment, ActionDefinitionId actionId)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains(cardIdFragment) &&
                    candidate.State.Location == CardLocation.Hand);
                ConfiguredMatchFixtures.Accept(
                    session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(actionId));
            Assert.That(offer.IsAvailable, Is.True,
                "The fixture Skill must be takeable in this position, or the case holds over nothing.");
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return HeroSpace(session, TeamSide.Red);
        }

        private static void NameSubject(MatchSession session, HexCoordinate subjectAt)
        {
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations
                .Single(invocation => invocation.Candidate.Value.Coordinate.Equals(subjectAt)));
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);
        }

        /// <summary>Answers the orbit's destination choice and reports where the piece landed.</summary>
        private static HexCoordinate OrbitDestination(MatchSession session)
        {
            var chosen = session.Projection.PendingEffectChoice.SelectInvocations[0]
                .Candidate.Value.Coordinate;
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);
            return chosen;
        }

        private static void Answer(MatchSession session, EffectChoiceCommand invocation) =>
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerEffectChoice(invocation));

        private static HexCoordinate HeroSpace(MatchSession session, TeamSide side) =>
            session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side == side)
                .Coordinate;

        private static HexCoordinate MinionSpace(MatchSession session, MinionId minionId) =>
            session.Projection.Minions
                .Single(minion => minion.MinionId.Equals(minionId)).Coordinate;
    }
}
