using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.8a1: an Attack the rules can prove has no legal target is offered unavailable, the
    /// way milestone 6.5af already offers an unstartable Skill.
    ///
    /// Every case here drives a real <see cref="MatchSession"/>, because the thing under test is what a
    /// player is offered in a position a match can actually be in. Two cases live elsewhere on purpose,
    /// and both are about keeping this fixture on the cross-platform tier: the p.16 Range-item case needs
    /// an item nothing in a live match can grant this early and sits beside the composition that can
    /// place one, in <see cref="DashboardSlotsAndComposedItemTotalsTests"/>; and the greyed control the
    /// unavailable offer produces is drawn by Unity presentation, so it sits in
    /// <c>MatchHudPresentationTests</c> rather than dragging every case here behind the harness exclude
    /// list.
    /// </summary>
    public sealed class AttackOfferAvailabilityTests
    {
        [Test]
        public void AnAttackWithNothingInRange_IsOfferedUnavailableAndStillRefusesAStart()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            CommitAttackCards(session);

            var attack = AttackOffer(session);
            Assert.That(attack.IsAvailable, Is.False,
                "The configured setup starts the heroes far apart, so this Attack reaches nobody.");
            Assert.That(LegalTargetsOf(session, attack), Is.Zero,
                "and the reason is the target set the start would build, not a separate reading.");

            // The rule did not move to the offer set. The start still refuses, and still refuses
            // atomically: nothing is left pending and the turn is exactly where it was.
            var before = session.Projection;
            var start = session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId));
            Assert.That(start.Accepted, Is.False);
            Assert.That(start.TurnRejectionReason,
                Is.EqualTo(TurnResolutionRejectionReason.AttackActionRejected));
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null);
            Assert.That(session.Projection.PendingAttackResolution, Is.Null);
            Assert.That(session.Projection.IsTurnActive, Is.True);
            Assert.That(session.Projection.AcceptedCommands,
                Has.Count.EqualTo(before.AcceptedCommands.Count));
        }

        [Test]
        public void AnAttackWithAnEnemyInRange_StaysAvailableAndStarts()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithAnAdjacentEnemyMinion(out _, out var target);
            CommitAttackCards(session);

            var attack = AttackOffer(session);
            Assert.That(attack.IsAvailable, Is.True,
                "An enemy minion stands beside the attacker, so the Attack has somebody.");
            Assert.That(LegalTargetsOf(session, attack), Is.GreaterThan(0));

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            Assert.That(session.Projection.PendingAttackTargetChoice.LegalTargetMinionIds,
                Has.Member(target),
                "and the offer and the start agree about which position they are describing.");
        }

        [Test]
        public void TheOfferSetAndTheStart_AgreeOnEveryAttackOfferInBothPositions()
        {
            foreach (var session in new[]
            {
                ConfiguredMatchFixtures.CreateConfiguredSession(),
                ConfiguredMatchFixtures.CreateSessionWithAnAdjacentEnemyMinion(out _, out _)
            })
            {
                CommitAttackCards(session);
                foreach (var offer in session.Projection.AvailableTurnActions.Where(IsAttack(session)))
                {
                    var hasTarget = LegalTargetsOf(session, offer) > 0;
                    Assert.That(offer.IsAvailable, Is.EqualTo(hasTarget),
                        "One statement of the judgement: the offer says what the start would find.");
                }
            }
        }

        [Test]
        public void NoOtherOfferKindIsTouched()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            CommitAttackCards(session);

            var others = session.Projection.AvailableTurnActions
                .Where(offer => !IsAttack(session)(offer))
                .ToArray();
            Assert.That(others, Is.Not.Empty,
                "The committed card prints more than an Attack, or this proves nothing.");
            Assert.That(others.All(offer => offer.IsAvailable), Is.True,
                "A Movement offer keeps its availability; only a Skill has a judgement of its own, and " +
                "this position gives it no reason to refuse one.");
        }

        /// <summary>
        /// The consequence this milestone had to be careful not to cause, caught by the complete Edit
        /// Mode suite: a Clear replaces an Attack, but rulebook p.13 gives it its own legality and lets
        /// it take any number of adjacent tokens including none. An Attack with nobody to target is
        /// still an Attack a Clear may replace, so stamping the Attack unavailable must not take the
        /// replacement away with it.
        ///
        /// Milestone 8.3 closes the 7.9a review's F-TEST-03 here. The subject set used to be allowed to
        /// be empty through an <c>Assert.Pass</c>, which would have retired this regression silently the
        /// first time the configured content changed shape - and milestone 8.4 changes exactly that. The
        /// 7.9a diagnostic recorded the escape as latent rather than live (six of six passing with no
        /// escape message in the trx), so the guard below pins the position that was already true.
        /// </summary>
        [Test]
        public void AnUnavailableAttack_StillOffersTheClearThatReplacesIt()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            CommitAttackCards(session);

            var replaceable = session.Projection.AvailableTurnActions
                .Where(IsAttack(session))
                .Where(offer => session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .DefaultPolicy.CanBeReplaced)
                .ToArray();
            Assert.That(replaceable, Is.Not.Empty,
                "The acting card prints a replaceable Attack in this position, or the assertions below " +
                "hold over nothing and this regression protects nothing.");

            foreach (var offer in replaceable)
            {
                Assert.That(offer.IsAvailable, Is.False,
                    "This position gives the Attack nobody to target.");
                Assert.That(
                    session.Projection.AvailableClearOptions.Any(option =>
                        option.ReplacedAttackActionDefinitionId.Equals(offer.ActionDefinitionId)),
                    Is.True,
                    "and the Clear that replaces it is still offered.");
            }
        }

        [Test]
        public void AvailabilityIsReprojected_AcrossRestoreReplayAndUndo()
        {
            var session = ConfiguredMatchFixtures.CreateSessionWithAnAdjacentEnemyMinion(out _, out _);
            CommitAttackCards(session);
            var before = Describe(session);
            Assert.That(before, Does.Contain("True"), "This position offers at least one usable Attack.");

            var snapshot = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateSessionWithAnAdjacentEnemyMinion(out _, out _);
            Assert.That(restored.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(Describe(restored), Is.EqualTo(before),
                "A restored position derives the same availability rather than carrying it on the wire.");

            var attack = AttackOffer(session);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(Describe(session), Is.EqualTo(before),
                "and retained-prefix Undo re-derives it from the truncated history.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>Every Attack offer with its availability, as a stable string to compare positions by.</summary>
        private static string Describe(MatchSession session) =>
            string.Join("|", session.Projection.AvailableTurnActions
                .Where(IsAttack(session))
                .OrderBy(offer => offer.ActionDefinitionId.Value, StringComparer.Ordinal)
                .Select(offer => $"{offer.ActionDefinitionId}:{offer.IsAvailable}"));

        private static Func<ActionOffer, bool> IsAttack(MatchSession session) => offer =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .Kind == ActionKind.Attack;

        private static ActionOffer AttackOffer(MatchSession session) =>
            session.Projection.AvailableTurnActions.First(IsAttack(session));

        /// <summary>
        /// How many targets the start would find, read by starting the Attack on a throwaway session at
        /// the same position and counting what it opened. The point of the milestone is that this and
        /// the offer flag cannot disagree, so the test may not compute it a third way.
        /// </summary>
        private static int LegalTargetsOf(MatchSession session, ActionOffer offer)
        {
            var probe = session.CaptureSnapshot();
            var replica = ConfiguredMatchFixtures.CreateConfiguredSession();
            if (!replica.RestoreSnapshot(probe).Accepted)
            {
                replica = ConfiguredMatchFixtures.CreateSessionWithAnAdjacentEnemyMinion(out _, out _);
                Assert.That(replica.RestoreSnapshot(probe).Accepted, Is.True);
            }

            var started = replica.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId));
            if (!started.Accepted)
            {
                return 0;
            }

            var choice = replica.Projection.PendingAttackTargetChoice;
            return choice == null ? 0 : choice.LegalTargets.Count;
        }

        /// <summary>
        /// Commits a card carrying an Attack for each side and settles any Initiative tie, so the turn
        /// under test is resolving a card that prints one.
        /// </summary>
        private static void CommitAttackCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.First(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand &&
                    session.Projection.Definitions
                        .Single(definition => definition.Reference.Equals(candidate.DefinitionReference))
                        .ActionDefinitions.Any(action => action.Kind == ActionKind.Attack));
                ConfiguredMatchFixtures.Accept(
                    session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            Assert.That(session.Projection.AvailableTurnActions.Any(IsAttack(session)), Is.True,
                "The acting card must print an Attack, or these cases prove nothing.");
        }
    }
}
