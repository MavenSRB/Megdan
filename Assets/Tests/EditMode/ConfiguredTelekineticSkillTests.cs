using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.6: Wasp's five telekinetic Skills resolve their complete printed text - the first
    /// verified official card text this project runs.
    ///
    /// <para>Every case that can be driven through a real <see cref="MatchSession"/> is, because the
    /// subject is what a player is offered and what the board looks like afterwards, not what a content
    /// class returns when asked directly. The composition inventory below is the exception, and it is
    /// about the definitions themselves.</para>
    ///
    /// <para>The synthetic twins of these compositions - the orbit and kinesis fixture Skills - carry
    /// the same four new declarations on content that is nobody's card, which is the discipline every
    /// general seam in this project has followed since 6.4. They are exercised by
    /// <c>BoundedRepeatActionTests</c>, <c>DisplacementCapabilityTests</c> and
    /// <c>PlacementCapabilityTests</c>; what is here is Wasp.</para>
    /// </summary>
    public sealed class ConfiguredTelekineticSkillTests
    {
        // ---------------------------------------------------------------------------------------
        // The compositions themselves
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheThreeOrbitSkills_ComposeOneRadiusSelectionAndOneEquidistantMove()
        {
            foreach (var cardId in new[]
            {
                WaspCardContent.LiftUp, WaspCardContent.ControlGravity, WaspCardContent.CenterOfMass
            })
            {
                var effect = SkillEffect(cardId);
                var select = effect.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticTargetingCapabilities.RadiusTargetV1));
                var move = effect.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));

                Assert.That(effect.Root.Kind, Is.EqualTo(EffectNodeKind.Sequence),
                    cardId + " prints one sentence in order, not a choice.");
                Assert.That(
                    select.Inputs.Single(input => input.ParameterName == "includeSelf")
                        .Literal.BooleanValue,
                    Is.False, "The card moves something else, so Wasp is not among her own targets.");
                Assert.That(
                    select.Inputs.Single(input => input.ParameterName == "activity")
                        .Literal.IntegerValue,
                    Is.EqualTo((int)RuleActivity.Skill),
                    "Card-text direct Move has no activity of its own and reads the enclosing Skill's.");
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "distance").Literal.IntegerValue,
                    Is.EqualTo(1), "`1 space` is card text, which p.16 never raises.");
                Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                    "p.11: a distance the card does not make optional must be walked exactly.");
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName ==
                        SyntheticDisplacementCapabilities.DestinationPolicyParameterName)
                        .Literal.IntegerValue,
                    Is.EqualTo((int)DirectMoveDestinationPolicy.EquidistantFromActor),
                    "`without moving it away from you or closer to you` is the orbit policy and " +
                    "nothing else in the node table.");
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "mover").SourceNodeId,
                    Is.EqualTo(select.Id),
                    "A coordinate reaches a capability only through an earlier node's output.");
            }
        }

        [Test]
        public void OnlyCenterOfMass_DeclaresARepeatMaximumAboveOne()
        {
            Assert.That(
                new[]
                {
                    WaspCardContent.LiftUp, WaspCardContent.ControlGravity,
                    WaspCardContent.CenterOfMass, WaspCardContent.MassTelekinesis
                }.Select(cardId => cardId.Value + "=" + Repeat(cardId).MaximumRepeats +
                    "/" + Repeat(cardId).RepeatTargetBinding),
                Is.EqualTo(new[]
                {
                    "wasp-lift-up=1/SameTarget",
                    "wasp-control-gravity=1/SameTarget",
                    "wasp-center-of-mass=2/SameTarget",
                    "wasp-mass-telekinesis=1/Unbound"
                }),
                "`May repeat once on the same target`, `up to two times on the same target`, and " +
                "Mass Telekinesis's bare `May repeat once`, which leaves p.10's different-choices " +
                "default standing.");

            Assert.That(
                SkillEffect(WaspCardContent.Telekinesis).Nodes
                    .Any(node => node.Kind == EffectNodeKind.RepeatAction),
                Is.False, "Telekinesis prints no repeat at all.");
        }

        [Test]
        public void BothTelekineses_SelectThroughThePrintedRangeWithTheStraightLineComplement()
        {
            foreach (var cardId in new[] { WaspCardContent.Telekinesis, WaspCardContent.MassTelekinesis })
            {
                var effect = SkillEffect(cardId);
                var select = effect.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticTargetingCapabilities.RangeTargetV1));
                var place = effect.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticPlacementCapabilities.PlaceV2));

                Assert.That(
                    select.Inputs.Single(input => input.ParameterName ==
                        SyntheticTargetingCapabilities.ExcludeStraightLineParameterName)
                        .Literal.BooleanValue,
                    Is.True, "`which is not in a straight line` is p.19's rule read as a complement.");
                Assert.That(
                    place.Inputs.Single(input => input.ParameterName ==
                        SyntheticPlacementCapabilities.EligiblePieceParameterName)
                        .Literal.IntegerValue,
                    Is.EqualTo((int)PlaceEligiblePieceKind.UnitOrToken),
                    "`a unit or a token` is the card's own breadth, not p.18's `game piece`.");
                Assert.That(
                    place.Inputs.Single(input => input.ParameterName ==
                        SyntheticPlacementCapabilities.DestinationAnchorParameterName)
                        .Literal.IntegerValue,
                    Is.EqualTo((int)PlaceDestinationAnchor.ActingHero),
                    "`into a space adjacent to you` is measured from Wasp, not from the piece.");
                Assert.That(
                    place.Inputs.Single(input => input.ParameterName == "radius").Literal.IntegerValue,
                    Is.EqualTo(1), "`adjacent` is one space, and it is card text.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // The five primaries are no longer greyed
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheFiveTelekineticPrimaries_NoLongerOweACompositionAndAreNoLongerGreyedForOne()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                Action(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                Action(session, candidate).Kind == ActionKind.Skill);

            Assert.That(Action(session, offer).TextComposition.IsAwaited, Is.False);
            Assert.That(Action(session, offer).EffectRoot, Is.Not.Null);
            Assert.That(offer.IsAvailable, Is.True,
                "Lift Up owes no composition and has a minion standing beside Wasp to lift, so the " +
                "one rule that greyed it - DEV-006's honesty rule - no longer applies to it.");
            Assert.That(
                session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted,
                Is.True);
        }

        // ---------------------------------------------------------------------------------------
        // Lift Up, resolved
        // ---------------------------------------------------------------------------------------

        [Test]
        public void LiftUp_MovesTheChosenPieceOneSpaceAroundWaspAndNeverTowardsOrAwayFromHer()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);
            var actorAt = HeroSpace(session, TeamSide.Red);
            var before = HeroSpace(session, TeamSide.Blue);

            StartSkill(session);
            var subject = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(invocation => invocation.Candidate.Value.Coordinate.Equals(before));
            Answer(session, subject);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var destinations = session.Projection.PendingEffectChoice.SelectInvocations
                .Select(invocation => invocation.Candidate.Value.Coordinate)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(
                destinations.All(space =>
                    HexDistanceQuery.Calculate(actorAt, space) ==
                    HexDistanceQuery.Calculate(actorAt, before)),
                Is.True,
                "Every offered space is exactly as far from Wasp as the piece already was, which is " +
                "what `without moving it away from you or closer to you` admits and all it admits.");
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(before, space) == 1), Is.True,
                "and each is one space away, because the card writes the distance down.");

            var chosen = destinations[0];
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(HeroSpace(session, TeamSide.Blue), Is.EqualTo(chosen));
            Assert.That(HeroSpace(session, TeamSide.Red), Is.EqualTo(actorAt),
                "Wasp herself never moves; the orbit is the subject's.");
        }

        [Test]
        public void LiftUpsRepeat_MovesTheSamePieceAgainWithoutAskingWhichPiece()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            CommitAgainstStopProjectiles(session, WaspCardContent.LiftUp);
            var actorAt = HeroSpace(session, TeamSide.Red);

            StartSkill(session);
            OrbitOnce(session, HeroSpace(session, TeamSide.Blue));

            Assert.That(session.Projection.PendingEffectChoice.Kind,
                Is.EqualTo(EffectChoiceKind.ConfirmOrDecline),
                "`May repeat once` is p.10's confirm-or-decline and nothing else.");
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            Assert.That(session.Projection.PendingSkillResolution.InvocationContext,
                Is.EqualTo(ActionInvocationContext.Repeated));
            Assert.That(
                session.Projection.PendingEffectChoice.NodeId.Value, Is.EqualTo("orbit-move"),
                "The repeated instance opens on the destination, because `on the same target` left " +
                "it nothing to choose about the subject.");

            var secondFrom = HeroSpace(session, TeamSide.Blue);
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);

            var secondTo = HeroSpace(session, TeamSide.Blue);
            Assert.That(secondTo, Is.Not.EqualTo(secondFrom), "The same piece moved a second time.");
            Assert.That(
                HexDistanceQuery.Calculate(actorAt, secondTo),
                Is.EqualTo(HexDistanceQuery.Calculate(actorAt, secondFrom)),
                "still on its own ring around Wasp.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "and `once` ends the chain: the second instance is offered no repeat of its own.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static EffectDefinition SkillEffect(CardDefinitionId cardId) =>
            WaspCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Id.Equals(WaspCardContent.PrimarySkill))
                .EffectRoot;

        private static EffectNodeDefinition Repeat(CardDefinitionId cardId) =>
            SkillEffect(cardId).Nodes.Single(node => node.Kind == EffectNodeKind.RepeatAction);

        private static ActionDefinition Action(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        /// <summary>
        /// Red commits the named card and Blue commits Stop Projectiles, whose printed Initiative 3 is
        /// the lowest in the starting hand, so the Red card acts first on its printed value alone -
        /// the same arrangement <c>ConfiguredVerifiedHandTests</c> uses for the same reason.
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

        private static void StartSkill(MatchSession session)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                Action(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                Action(session, candidate).Kind == ActionKind.Skill);
            Assert.That(offer.IsAvailable, Is.True,
                "The composed Skill must be takeable in this position, or the case holds over nothing.");
            Assert.That(session.Execute(
                DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted, Is.True);
        }

        /// <summary>Names the piece standing at <paramref name="subjectAt"/>, then orbits it once.</summary>
        private static void OrbitOnce(MatchSession session, HexCoordinate subjectAt)
        {
            var subject = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(invocation => invocation.Candidate.Value.Coordinate.Equals(subjectAt));
            Answer(session, subject);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);
            Answer(session, session.Projection.PendingEffectChoice.SelectInvocations[0]);
            Answer(session, session.Projection.PendingEffectChoice.ConfirmInvocation);
        }

        private static void Answer(MatchSession session, EffectChoiceCommand invocation) =>
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.AnswerEffectChoice(invocation));

        private static HexCoordinate HeroSpace(MatchSession session, TeamSide side) =>
            session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side == side)
                .Coordinate;

        private static CardInstance HandCard(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));
    }
}
