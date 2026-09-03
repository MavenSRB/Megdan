using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.30a. Every one of the ten pending families reaches exactly the seats that own it.
    /// </summary>
    /// <remarks>
    /// <para>Seven families are observed in a real seeded six-player match rather than staged, which
    /// is the stronger evidence: the ownership rule has to hold wherever the game actually pauses,
    /// not only where a fixture arranges for it to. The three a bounded walk does not reach - a
    /// Defense response, a hero respawn, and a removal-prevention payment - come from the configured
    /// pauses that already exist for them.</para>
    /// <para>The walk is deliberately bounded and is not a complete-match gate; milestone 8.30c owns
    /// playing six seats through a whole match.</para>
    /// </remarks>
    public sealed class PendingDecisionTransportContractTests
    {
        [Test]
        public void EveryDeclaredFamilyReachesExactlyTheSeatsThatOwnIt()
        {
            var observed = new HashSet<string>(StringComparer.Ordinal);

            foreach (var family in WalkTheOfficialMatch())
            {
                observed.Add(family);
            }

            foreach (var staged in StagedPauses())
            {
                var host = new MatchHost(staged.Value());
                Assert.That(host.Projection.DecisionContract, Is.Not.Null,
                    staged.Key + " must be standing when the fixture hands the session over.");
                observed.Add(AssertOwnershipAtThisPause(host));
            }

            Assert.That(observed.OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(DeclaredFamilies().OrderBy(value => value, StringComparer.Ordinal)),
                "Every family a projection can publish needs seat-ownership evidence, or some " +
                "future pause reaches a client that was never authorized to answer it.");
        }

        /// <summary>
        /// Milestone 8.30a. A family declared but never owned cannot slip through: the ownership rule
        /// must be seen reading the very projection member 8.25e4b's inventory says that family
        /// stands on.
        /// </summary>
        /// <remarks>
        /// Compiled code rather than a list this fixture keeps in step by hand. An eleventh family
        /// added to the inventory without an ownership rule fails here immediately, rather than
        /// waiting for a match to pause on it and find that no seat can answer.
        /// </remarks>
        [Test]
        public void TheOwnershipRuleReadsEveryDeclaredFamilysOwnProjectionMember()
        {
            var declared = DeclaredFamilies();
            Assert.That(declared.Count, Is.EqualTo(10),
                "The ten families are the declared set this milestone authorizes against.");
            Assert.That(declared.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(declared.Count));

            var rule = new[] { typeof(SeatProjectionFactory) };
            foreach (var family in DecisionFamilyInventory.Families)
            {
                var member = ILConformanceScanner.Property(
                    typeof(MatchSessionProjection), family.ProjectionMember);
                Assert.That(ILConformanceScanner.References(rule, member.GetMethod), Is.True,
                    "The seat-ownership rule must read " + family.ProjectionMember + " to decide " +
                    "who may answer the " + family.Family + " family.");
            }
        }

        /// <summary>
        /// Walks one seeded official six-player match, stopping at every pause to check ownership,
        /// and answers each pause through the seat that owns it.
        /// </summary>
        private static IEnumerable<string> WalkTheOfficialMatch()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            // Milestone 8.35d re-derived the seed: with the round's own advance no longer drawn
            // by this walk, seed 2 reaches the Attack target and the Minion Battle far outside
            // this budget and seed 3 reaches every walked family inside 268 steps.
            var random = new MatchLoopRandom(3);
            var observed = new List<string>();
            var target = new[]
            {
                "initiative", "movement", "attack-target", "effect",
                "correction", "minion-battle", "system-transition"
            };

            for (var step = 0; step < 1600; step++)
            {
                var projection = host.Projection;
                if (projection.DecisionContract != null)
                {
                    var family = AssertOwnershipAtThisPause(host);
                    if (!observed.Contains(family))
                    {
                        observed.Add(family);
                        if (target.All(observed.Contains))
                        {
                            break;
                        }
                    }
                }

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var command = legal[random.Next(legal.Count)];
                if (!SubmitThroughItsSeat(host, command, "walk-" + step))
                {
                    break;
                }
            }

            Assert.That(observed, Is.SupersetOf(target),
                "Seed 3 of the official six-player board reaches these seven families; a change " +
                "that reroutes it must be understood before this list is edited.");
            return observed;
        }

        /// <summary>
        /// Submits one command as whichever seat is authorized for it, which is also the assertion
        /// that some seat always is.
        /// </summary>
        private static bool SubmitThroughItsSeat(IMatchHost host, DirectorCommand command, string id)
        {
            if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(host, command, id + "-agreement"))
                return true;

            var seat = host.Seats.Seats.FirstOrDefault(value =>
                host.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
            Assert.That(seat, Is.Not.Null,
                "No seat is authorized for a command the Director could take: " + command);
            var client = host.ClientForSeat(seat.Id);
            var result = client.Submit(new SeatSubmission(
                new SubmissionId(id), client.Projection.Version, command));
            Assert.That(result.Accepted, Is.True, result.Detail);
            return true;
        }

        /// <summary>
        /// The whole ownership claim, checked against one standing decision, and returning the family
        /// it checked so the caller can record coverage.
        /// </summary>
        private static string AssertOwnershipAtThisPause(IMatchHost host)
        {
            var projection = host.Projection;
            var contract = projection.DecisionContract;
            var family = contract.FamilyId;
            var expected = IndependentChooser(projection, family);
            var owner = SeatProjectionFactory.OwnerOfStandingDecision(projection);

            Assert.That(owner.FamilyId, Is.EqualTo(family));
            if (expected.HeroId.IsDefined)
            {
                Assert.That(owner.IsTeamOwned, Is.False, family.Value + " is answered by one hero.");
                Assert.That(owner.HeroId, Is.EqualTo(expected.HeroId),
                    family.Value + " must be owned by the chooser the authority recorded.");
            }
            else
            {
                Assert.That(owner.IsTeamOwned, Is.True, family.Value + " is answered by a team.");
                Assert.That(owner.TeamSide, Is.EqualTo(expected.TeamSide),
                    family.Value + " must be owned by the side the authority recorded.");
            }

            var owning = new List<MatchSeat>();
            foreach (var seat in host.Seats.Seats)
            {
                var seatProjection = host.ProjectionForSeat(seat.Id);
                if (owner.IsOwnedBy(seat))
                {
                    owning.Add(seat);
                    Assert.That(seatProjection.DecisionContract, Is.SameAs(contract),
                        "An owning seat receives the exact answers the authority published.");
                    var directCommands = contract.Commands.Where(command =>
                        !TeamAgreementTestDriver.IsFinalAgreementCommand(command)).ToArray();
                    Assert.That(seatProjection.AuthorizedCommands, Is.EqualTo(directCommands));
                    if (contract.Commands.Any(TeamAgreementTestDriver.IsFinalAgreementCommand))
                        Assert.That(seatProjection.TeamAgreement, Is.Not.Null);
                    Assert.That(seatProjection.IsWaitingForAnotherSeat, Is.False);
                }
                else
                {
                    Assert.That(seatProjection.DecisionContract, Is.Null,
                        "A seat that cannot answer " + family.Value + " is not shown the answers.");
                    Assert.That(seatProjection.AuthorizedCommands, Is.Empty);
                    Assert.That(seatProjection.IsWaitingForAnotherSeat, Is.True);
                    Assert.That(seatProjection.DecisionOwner.FamilyId, Is.EqualTo(family),
                        "Who we are waiting for is public even where the question is not.");
                }
            }

            Assert.That(owning, Is.Not.Empty, family.Value + " must be answerable by somebody.");
            if (!owner.IsTeamOwned)
            {
                Assert.That(owning.Count, Is.EqualTo(1),
                    "A hero-owned decision belongs to exactly one seat.");
            }
            else
            {
                Assert.That(owning.Select(seat => seat.TeamSide).Distinct().Single(),
                    Is.EqualTo(owner.TeamSide));
            }

            var directAnswer = contract.Commands.FirstOrDefault(command =>
                !TeamAgreementTestDriver.IsFinalAgreementCommand(command));
            if (directAnswer != null)
                AssertANonOwnerCannotAnswer(host, owning, directAnswer, family.Value);
            return family.Value;
        }

        private static void AssertANonOwnerCannotAnswer(
            IMatchHost host, IReadOnlyList<MatchSeat> owning, DirectorCommand answer, string family)
        {
            var intruder = host.Seats.Seats.FirstOrDefault(seat =>
                !owning.Any(value => value.Id.Equals(seat.Id)));
            if (intruder == null)
            {
                return;
            }

            var version = host.Version;
            var sequence = host.Sequence;
            var result = host.ClientForSeat(intruder.Id).Submit(
                new SeatSubmission(new SubmissionId("intruder-" + family + "-" + version), version, answer));

            Assert.That(result.Accepted, Is.False,
                "A seat that does not own " + family + " must not be able to answer it.");
            Assert.That(result.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand)
                    .Or.EqualTo(SeatSubmissionRejectionReason.OwnershipMismatch),
                "It is refused as an authorization failure, never as a rules answer.");
            Assert.That(result.SessionRejectionReason, Is.EqualTo(MatchSessionRejectionReason.None));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));
        }

        /// <summary>
        /// The chooser this fixture reads for itself, off the same authoritative pending object the
        /// production rule reads. Stated independently on purpose: a production rule edited to read
        /// the wrong field would still agree with itself.
        /// </summary>
        private static (HeroId HeroId, TeamSide TeamSide) IndependentChooser(
            MatchSessionProjection projection, PendingDecisionFamilyId family)
        {
            if (family.Equals(PendingDecisionFamilies.Initiative))
            {
                return (default, projection.PendingInitiativeChoice.OwningSide);
            }

            if (family.Equals(PendingDecisionFamilies.Movement))
            {
                return (projection.PendingMovementChoice.ChooserId, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.AttackTarget))
            {
                return (projection.PendingAttackTargetChoice.ChooserId, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.Defense))
            {
                return (projection.PendingDefenseChoice.ChooserId, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.Effect))
            {
                return (projection.PendingEffectChoice.ChooserId, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.Correction))
            {
                return (default, projection.PendingDisplacedMinionCorrection.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.MinionBattle))
            {
                return (default, projection.PendingMinionBattleChoice.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.HeroRespawn))
            {
                return (projection.PendingHeroRespawnChoice.HeroId, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.SystemTransition))
            {
                // Milestone 8.35d. p.15's Upgrade a Card is the one transition pause a single
                // player owns, and the authority records that in the paused item rather than in
                // the step's side: the item names the hero whose level it is. Restated here from
                // the same source the production rule reads, so the two agree by derivation
                // rather than by both being written down twice.
                var step = projection.PendingSystemTransitionStep;
                return step.DefinitionId.Equals(HeroLevelUpSystemTransition.DefinitionId) &&
                    step.ItemId.IsDefined
                    ? (HeroLevelUpItem.Read(step.ItemId).HeroId, TeamSide.None)
                    : (default, step.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.RemovalPrevention))
            {
                return (projection.PendingRemovalPrevention.DecidingHeroId, TeamSide.None);
            }

            Assert.Fail("This fixture has no independent chooser for family " + family.Value + ".");
            return (default, TeamSide.None);
        }

        /// <summary>The three families a bounded seeded walk does not reach.</summary>
        private static IEnumerable<KeyValuePair<string, Func<MatchSession>>> StagedPauses() =>
            new[]
            {
                new KeyValuePair<string, Func<MatchSession>>(
                    "defense", ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse),
                new KeyValuePair<string, Func<MatchSession>>(
                    "hero-respawn", HeroRespawnTests.CreatePendingRespawnSession),
                new KeyValuePair<string, Func<MatchSession>>(
                    "removal-prevention",
                    ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause)
            };

        private static IReadOnlyList<string> DeclaredFamilies() =>
            typeof(PendingDecisionFamilies)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(PendingDecisionFamilyId))
                .Select(field => ((PendingDecisionFamilyId)field.GetValue(null)).Value)
                .ToArray();
    }
}
