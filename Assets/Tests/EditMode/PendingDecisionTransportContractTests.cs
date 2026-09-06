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
            Assert.That(declared.Count, Is.EqualTo(11),
                "The eleven families are the declared set this milestone authorizes against; " +
                "milestone 8.35g's turn release is the eleventh.");
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
            // Milestone 8.35u re-derived it again. p.15 now pauses once for every hero that bought
            // a level rather than once per hero in turn, so the number of standing answers at an
            // end of round changed and every seeded trajectory diverges after the first level-up:
            // seed 1 stopped reaching Minion Battle at all inside 2,600 steps. An offline scan of
            // twenty-four seeds (`tmp/8-35d-8-35e-multiplayer-corrections/seed-scan-results.txt`)
            // put every walked family on SEED 6, the last of them at step 408.
            //
            // Milestone 8.35z20 re-derived it again: correcting Blend Into Shadows' secondary
            // rerouted this trajectory and seed 6 no longer reaches Minion Battle at all inside
            // 2,600 steps. A fresh offline scan of seeds 1-30 put every walked family on SEED 22,
            // the last of them (minion-battle) at step 319 - well inside this method's 1,900-step
            // walk.
            var random = new MatchLoopRandom(22);
            var observed = new List<string>();
            var target = new[]
            {
                "initiative", "movement", "attack-target", "effect",
                "correction", "minion-battle", "system-transition"
            };

            for (var step = 0; step < 1900; step++)
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
                "Seed 22 of the official six-player board reaches these seven families; a change " +
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
            if (expected.HeroIds.Count > 0)
            {
                Assert.That(owner.IsTeamOwned, Is.False, family.Value + " is answered by heroes.");
                Assert.That(owner.HeroIds, Is.EqualTo(expected.HeroIds),
                    family.Value + " must be owned by the choosers the authority recorded.");
            }
            else
            {
                Assert.That(owner.IsTeamOwned, Is.True, family.Value + " is answered by a team.");
                Assert.That(owner.TeamSide, Is.EqualTo(expected.TeamSide),
                    family.Value + " must be owned by the side the authority recorded.");
            }

            // Milestone 8.35u AMENDED this guard rather than relaxing it. It used to say "an owning
            // seat receives the exact answers the authority published", which was the same statement
            // as "the whole contract" only while one seat owned all of it. p.15's simultaneous
            // levelling publishes one contract holding several heroes' answers, so the claim becomes
            // two, and together they are STRICTER than what they replaced: each owning seat receives
            // exactly the answers that are ITS OWN, and the owning seats' shares partition the
            // contract - nothing published reaches nobody, and nothing reaches a seat twice.
            var owning = new List<MatchSeat>();
            var sharedOut = new List<DirectorCommand>();
            foreach (var seat in host.Seats.Seats)
            {
                var seatProjection = host.ProjectionForSeat(seat.Id);
                if (owner.IsOwnedBy(seat))
                {
                    owning.Add(seat);
                    Assert.That(seatProjection.DecisionContract, Is.Not.Null,
                        "An owning seat receives the answers it may give.");
                    Assert.That(seatProjection.DecisionContract.FamilyId, Is.EqualTo(family));
                    Assert.That(contract.Commands, Is.SupersetOf(seatProjection.DecisionContract.Commands),
                        "A seat is never shown an answer the authority did not publish.");
                    sharedOut.AddRange(seatProjection.DecisionContract.Commands);

                    var directCommands = seatProjection.DecisionContract.Commands.Where(command =>
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
            if (owner.IsTeamOwned)
            {
                // A team decision is one question the whole side is looking at, so every owning
                // seat is shown the same whole contract. Replication, not partition.
                Assert.That(sharedOut.Count, Is.EqualTo(owning.Count * contract.Commands.Count));
                Assert.That(sharedOut.Distinct(), Is.EquivalentTo(contract.Commands));
            }
            else
            {
                Assert.That(sharedOut, Is.EquivalentTo(contract.Commands),
                    "A hero-owned decision's shares PARTITION the published contract: every answer " +
                    "the authority published reaches exactly one seat, and none reaches two. That " +
                    "is the property milestone 8.35u had to keep true once one contract could hold " +
                    "several heroes' answers at the same time.");
            }
            if (!owner.IsTeamOwned)
            {
                Assert.That(owning.Count, Is.EqualTo(owner.HeroIds.Count),
                    "A hero-owned decision belongs to exactly one seat per awaited hero.");
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

            // Milestone 8.35u. When every seat owns something - which p.15's simultaneous levelling
            // makes ordinary - the intruder check above finds nobody outside `owning` and silently
            // proves nothing. The refusal that matters then is one OWNER submitting ANOTHER owner's
            // answer, which is the shape of the defect 8.35d fixed and the shape it could recur in.
            foreach (var seat in owning)
            {
                var seatProjection = host.ProjectionForSeat(seat.Id);
                var foreign = contract.Commands.FirstOrDefault(command =>
                    !TeamAgreementTestDriver.IsFinalAgreementCommand(command) &&
                    !seatProjection.DecisionContract.Commands.Contains(command));
                if (foreign == null)
                {
                    continue;
                }

                var version = host.Version;
                var refused = host.SubmitForSeat(seat.Id, new SeatSubmission(
                    new SubmissionId("foreign-" + family.Value + "-" + seat.Id.Value),
                    version,
                    foreign));
                Assert.That(refused.RejectionReason,
                    Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand),
                    "Seat " + seat.Id + " owns a question of its own in " + family.Value + " and " +
                    "still may not answer another seat's.");
                Assert.That(host.Version, Is.EqualTo(version));
            }

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
        private static (IReadOnlyList<HeroId> HeroIds, TeamSide TeamSide) IndependentChooser(
            MatchSessionProjection projection, PendingDecisionFamilyId family)
        {
            if (family.Equals(PendingDecisionFamilies.Initiative))
            {
                return (Array.Empty<HeroId>(), projection.PendingInitiativeChoice.OwningSide);
            }

            if (family.Equals(PendingDecisionFamilies.Movement))
            {
                return (new[] { projection.PendingMovementChoice.ChooserId }, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.AttackTarget))
            {
                return (new[] { projection.PendingAttackTargetChoice.ChooserId }, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.Defense))
            {
                return (new[] { projection.PendingDefenseChoice.ChooserId }, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.Effect))
            {
                return (new[] { projection.PendingEffectChoice.ChooserId }, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.Correction))
            {
                return (Array.Empty<HeroId>(), projection.PendingDisplacedMinionCorrection.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.MinionBattle))
            {
                return (Array.Empty<HeroId>(), projection.PendingMinionBattleChoice.ChoosingSide);
            }

            if (family.Equals(PendingDecisionFamilies.HeroRespawn))
            {
                return (new[] { projection.PendingHeroRespawnChoice.HeroId }, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.SystemTransition))
            {
                // Milestone 8.35d. p.15's Upgrade a Card is the one transition pause a single
                // player owns, and the authority records that in the paused item rather than in
                // the step's side: the item names the hero whose level it is. Restated here from
                // the same source the production rule reads, so the two agree by derivation
                // rather than by both being written down twice.
                //
                // Milestone 8.35u. Once per open item, because p.15 asks every hero that bought a
                // level at the same time and there is then no single chooser to restate.
                var step = projection.PendingSystemTransitionStep;
                return step.DefinitionId.Equals(HeroLevelUpSystemTransition.DefinitionId) &&
                    step.OpenItems.All(open => open.ItemId.IsDefined)
                    ? (step.OpenItems
                        .Select(open => HeroLevelUpItem.Read(open.ItemId).HeroId)
                        .ToArray(), TeamSide.None)
                    : (Array.Empty<HeroId>(), step.AwaitedSide);
            }

            if (family.Equals(PendingDecisionFamilies.RemovalPrevention))
            {
                return (new[] { projection.PendingRemovalPrevention.DecidingHeroId }, TeamSide.None);
            }

            if (family.Equals(PendingDecisionFamilies.TurnRelease))
            {
                // Milestone 8.35g. The turn belongs to the hero who just finished resolving it,
                // and so does handing it on. Read from the paused record itself, which is where
                // the production rule reads it, so the two agree by derivation rather than by
                // being written down twice.
                return (new[] { projection.PendingTurnRelease.ActorId }, TeamSide.None);
            }

            Assert.Fail("This fixture has no independent chooser for family " + family.Value + ".");
            return (Array.Empty<HeroId>(), TeamSide.None);
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
