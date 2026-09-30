using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.11e's application-level transition inventory, closed over every projected decision
    /// family by milestone 8.25e4. Individual authority fixtures own rule permutations; this fixture
    /// owns the shared claim that every answer a projection exposes has one semantic family, one
    /// player-facing surface, one exact command, and an observable accepted result.
    ///
    /// <para><b>This is deliberately not <c>PlayerInteractionShape</c>.</b> That Play Mode vocabulary
    /// describes whole player journeys. <see cref="DecisionFamilyInventory"/> is a test-owned oracle
    /// over the immutable application projection, and neither introduces a production choice base nor
    /// claims that <see cref="EffectChoiceCommandKind"/> is a universal or closed decision enum.</para>
    /// </summary>
    public sealed class ConfiguredPendingChoiceProjectionTests
    {
        [Test]
        public void EveryConfiguredChoiceAnswerPublishesTheCurrentProjectionOrCommitsNothing()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var exercisedFamilies = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < 160 && session.Projection.Phase != RoundResolutionPhase.GameEnded; index++)
            {
                var before = session.Projection;
                var family = DecisionFamilyInventory.CurrentFamily(before);
                var offered = DeterministicMatchLoop.EnumerateLegalCommands(before);
                Assert.That(offered, Is.Not.Empty,
                    $"The configured match stalled at {before.Phase}, round {before.RoundIndex}, turn {before.TurnIndex}.");
                if (family != null)
                {
                    Assert.That(offered,
                        Is.EqualTo(DecisionFamilyInventory.ProjectedCommands(before, includeDeselect: false)),
                        $"The deterministic driver must forward exactly the projected {family} answers.");
                }

                var command = offered[0];
                var result = session.Execute(command);

                Assert.That(result.Accepted, Is.True,
                    $"The projection offered {command.Kind}, but authority refused it: {result.Detail}");
                Assert.That(result.Projection, Is.SameAs(session.Projection));
                Assert.That(result.Projection, Is.Not.SameAs(before));
                Assert.That(result.Projection.Version, Is.EqualTo(before.Version + 1));
                Assert.That(result.Projection.AcceptedCommands.Last(), Is.EqualTo(command));
                Assert.That(result.Projection.AuthoritativeSnapshot,
                    Is.Not.EqualTo(before.AuthoritativeSnapshot),
                    "An accepted configured input must publish the state it actually committed.");

                if (family == null)
                {
                    continue;
                }

                exercisedFamilies.Add(family);
                var after = session.Projection;
                var stale = session.Execute(command);
                Assert.That(stale.Accepted, Is.False,
                    $"The previous {family} invocation remained consumable after it was accepted.");
                Assert.That(stale.Projection, Is.SameAs(after));
                Assert.That(session.Projection, Is.SameAs(after));
            }

            Assert.That(exercisedFamilies, Does.Contain("Movement"));
            Assert.That(exercisedFamilies, Does.Contain("Attack target"));
            Assert.That(exercisedFamilies, Does.Contain("Defense"));
            Assert.That(exercisedFamilies, Does.Contain("Effect"));
        }

        /// <summary>
        /// The exact 8.25e4 closing check. Reflection finds the projection members so adding an
        /// eleventh <c>Pending*</c> property cannot leave this inventory silently at ten; the rows then
        /// bind every current answer shape without changing production types.
        /// </summary>
        [Test]
        public void EveryChoiceProjectionMemberMapsToExactlyOneDecisionFamilyAndAnswerRows()
        {
            var errors = DecisionFamilyInventory.AuditProductionInventory();
            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));

            Assert.That(DecisionFamilyInventory.Families.Length, Is.EqualTo(12),
                "The bounded current claim is twelve pending decision families, not all possible " +
                "states; milestone 8.35g's turn release is the eleventh and 9.21b's acting-body pick " +
                "the twelfth.");
            Assert.That(DecisionFamilyInventory.Families.Select(value => value.Family).Distinct().Count(),
                Is.EqualTo(12), "Every pending family has one distinct semantic identity.");
            Assert.That(DecisionFamilyInventory.Families.Sum(value => value.Answers.Count),
                Is.EqualTo(32),
                "Every current legal answer shape has one inventory row. Thirty until milestone " +
                "8.35g, whose turn release adds exactly one: the release itself is the whole of " +
                "that family's contract, so the pause can be answered one way and no other. " +
                "Milestone 9.21b's acting-body pick adds one more: naming a standing body.");

            AssertEnumKindsCovered<EffectChoiceCommandKind>("Effect/Skill", "Effect/Attack or Block");
            AssertEnumKindsCovered<DisplacedMinionCorrectionCommandKind>("Displaced minion correction");
            AssertEnumKindsCovered<MinionBattleChoiceCommandKind>("Minion Battle");
            AssertEnumKindsCovered<HeroRespawnCommandKind>("Hero respawn");

            var removal = DecisionFamilyInventory.Families.Single(value =>
                value.ProjectionMember == nameof(MatchSessionProjection.PendingRemovalPrevention));
            Assert.That(removal.Family, Is.EqualTo("Removal prevention"));
            Assert.That(removal.Answers.Select(value => value.Answer),
                Is.EqualTo(new[] { "Projected payment", "Decline" }));
            Assert.That(removal.Answers.All(value =>
                    value.CommandKind == DirectorCommandKind.AnswerRemovalPrevention),
                Is.True,
                "Removal prevention owns its exact command and does not converge on EffectChoice.");

            Assert.That(DecisionFamilyInventory.NonPendingOffers.Select(value => value.Offer),
                Is.EqualTo(new[]
                {
                    "Commitments", "Ordinary action selection", "Hold", "Fast Travel", "Clear",
                    "End-of-round advance"
                }),
                "General offers are recorded beside, never disguised as, the ten Pending families.");
        }

        /// <summary>
        /// A bounded simulated eleventh family. It is not a production projection member; its purpose
        /// is to prove the detector itself fails until classification, a family owner, an invocation
        /// source, a visible surface, an exact command, and an accepted result are all supplied.
        /// </summary>
        [Test]
        public void AnEleventhFamilyCannotHideAsACompanionOrReuseAnOwnedProjection()
        {
            const string probe = "PendingProbeChoice";
            var reflected = DecisionFamilyInventory.ReflectedPendingMembers.Concat(new[] { probe }).ToArray();
            var choices = DecisionFamilyInventory.ChoiceProjectionMembers.Concat(new[] { probe }).ToArray();

            AssertAuditFailsWith(
                DecisionFamilyInventory.Audit(
                    reflected,
                    DecisionFamilyInventory.ChoiceProjectionMembers,
                    DecisionFamilyInventory.CompanionProjectionMembers,
                    DecisionFamilyInventory.Families),
                probe + " is classified 0 times");

            AssertAuditFailsWith(
                DecisionFamilyInventory.Audit(
                    reflected, choices, DecisionFamilyInventory.CompanionProjectionMembers,
                    DecisionFamilyInventory.Families),
                probe + " maps to 0 decision families");

            AssertAuditFailsWith(
                DecisionFamilyInventory.Audit(
                    reflected, choices,
                    DecisionFamilyInventory.CompanionProjectionMembers.Concat(new[] { probe }),
                    DecisionFamilyInventory.Families),
                probe + " is classified 2 times");

            var reused = new DecisionFamilyDefinition(
                "Bounded probe", nameof(MatchSessionProjection.PendingInitiativeChoice),
                projection => false,
                ProbeRow("Projected probe control", DirectorCommandKind.AnswerEffectChoice));
            AssertAuditFailsWith(
                DecisionFamilyInventory.Audit(
                    reflected, choices, DecisionFamilyInventory.CompanionProjectionMembers,
                    DecisionFamilyInventory.Families.Concat(new[] { reused })),
                probe + " maps to 0 decision families");

            var noSurface = new DecisionFamilyDefinition(
                "Bounded probe", probe, projection => false,
                ProbeRow(string.Empty, DirectorCommandKind.AnswerEffectChoice));
            AssertAuditFailsWith(
                DecisionFamilyInventory.Audit(
                    reflected, choices, DecisionFamilyInventory.CompanionProjectionMembers,
                    DecisionFamilyInventory.Families.Concat(new[] { noSurface })),
                "Bounded probe/Probe/Answer has no visible surface");

            var noCommand = new DecisionFamilyDefinition(
                "Bounded probe", probe, projection => false,
                ProbeRow("Projected probe control", null));
            AssertAuditFailsWith(
                DecisionFamilyInventory.Audit(
                    reflected, choices, DecisionFamilyInventory.CompanionProjectionMembers,
                    DecisionFamilyInventory.Families.Concat(new[] { noCommand })),
                "Bounded probe/Probe/Answer has no DirectorCommand owner");

            var complete = new DecisionFamilyDefinition(
                "Bounded probe", probe, projection => false,
                ProbeRow("Projected probe control", DirectorCommandKind.AnswerEffectChoice));
            Assert.That(
                DecisionFamilyInventory.Audit(
                    reflected, choices, DecisionFamilyInventory.CompanionProjectionMembers,
                    DecisionFamilyInventory.Families.Concat(new[] { complete })),
                Is.Empty,
                "The deliberate family is admitted only after its whole bounded contract is present.");
        }

        /// <summary>
        /// Fortify is reached through the configured Tier III Brogan hand and a real Attack. Each case
        /// starts from a fresh session so both payments and declining are independently accepted once.
        /// </summary>
        [TestCase(0, "first projected payment")]
        [TestCase(1, "second projected payment")]
        [TestCase(-1, "decline")]
        public void ConfiguredFortifyPause_EveryPaymentAndDeclineCompletesExactlyOnce(
            int answerIndex, string answerName)
        {
            var fixture = ConfiguredBroganPause.Create();
            var before = fixture.Session.Projection;
            var prompt = before.PendingRemovalPrevention;
            Assert.That(prompt, Is.Not.Null, answerName + " needs the configured pause standing.");
            Assert.That(prompt.Payments, Has.Count.EqualTo(2),
                "Fortify accepts Brogan's two Basic cards on this progressed configured path.");
            Assert.That(prompt.Payments.Select(payment => DefinitionId(before, payment.PaymentCardInstanceId)),
                Is.EquivalentTo(new[] { BroganCardContent.Bulwark, BroganCardContent.Onslaught }));

            var projected = prompt.Payments
                .Select(payment => DirectorCommand.AnswerRemovalPrevention(payment.Invocation))
                .Concat(new[]
                {
                    DirectorCommand.AnswerRemovalPrevention(prompt.DeclineInvocation)
                })
                .ToArray();
            var oracle = DecisionFamilyInventory.ProjectedCommands(before, includeDeselect: false);
            var driven = DeterministicMatchLoop.EnumerateLegalCommands(before);
            Assert.That(oracle, Is.EqualTo(projected),
                "The independent family inventory forwards every payment in projection order, then decline.");
            Assert.That(driven, Is.EqualTo(projected),
                "The deterministic loop forwards the same complete removal-prevention answer set.");
            Assert.That(driven.All(command =>
                    command.Kind == DirectorCommandKind.AnswerRemovalPrevention), Is.True,
                "No unrelated command may be offered beside an exclusive removal decision.");
            Assert.That(driven.Distinct().Count(), Is.EqualTo(driven.Count),
                "Each projected payment and decline appears exactly once.");

            var paymentLocations = prompt.Payments.ToDictionary(
                payment => payment.PaymentCardInstanceId,
                payment => before.Cards.Single(card =>
                    card.Id.Equals(payment.PaymentCardInstanceId)).State.Location);
            var subject = prompt.SubjectMinionId;
            var acceptedBefore = before.AcceptedCommands.Count;
            var command = answerIndex < 0 ? projected.Last() : projected[answerIndex];
            var result = fixture.Session.Execute(command);

            Assert.That(result.Accepted, Is.True, answerName + " was projected and must be accepted.");
            Assert.That(result.Projection.AcceptedCommands, Has.Count.EqualTo(acceptedBefore + 1));
            Assert.That(result.Projection.AcceptedCommands.Last(), Is.EqualTo(command));
            Assert.That(result.Projection.PendingRemovalPrevention, Is.Null,
                answerName + " must complete the standing decision.");

            if (answerIndex >= 0)
            {
                var paid = prompt.Payments[answerIndex].PaymentCardInstanceId;
                Assert.That(CardLocationOf(result.Projection, paid), Is.EqualTo(CardLocation.DiscardPile),
                    "The named eligible card is the price that was spent.");
                foreach (var other in prompt.Payments
                    .Select(payment => payment.PaymentCardInstanceId)
                    .Where(card => !card.Equals(paid)))
                {
                    Assert.That(CardLocationOf(result.Projection, other),
                        Is.EqualTo(paymentLocations[other]),
                        "A payment spends only its own named card.");
                }

                Assert.That(result.Projection.Minions.Any(minion =>
                        minion.MinionId.Equals(subject)), Is.True,
                    "Paying prevents the defeated minion's removal.");
            }
            else
            {
                foreach (var payment in prompt.Payments)
                {
                    Assert.That(CardLocationOf(result.Projection, payment.PaymentCardInstanceId),
                        Is.EqualTo(paymentLocations[payment.PaymentCardInstanceId]),
                        "Declining spends no eligible card.");
                }

                Assert.That(result.Projection.Minions.Any(minion =>
                        minion.MinionId.Equals(subject)), Is.False,
                    "Declining completes the defeat by removing the minion.");
            }

            var after = fixture.Session.Projection;
            var afterSnapshot = after.AuthoritativeSnapshot;
            var stale = fixture.Session.Execute(command);
            Assert.That(stale.Accepted, Is.False,
                "The accepted projected invocation is one-shot and stale afterwards.");
            Assert.That(stale.Projection, Is.SameAs(after));
            Assert.That(fixture.Session.Projection, Is.SameAs(after),
                "A refused stale answer republishes no replacement projection.");
            Assert.That(fixture.Session.Projection.AuthoritativeSnapshot, Is.EqualTo(afterSnapshot),
                "A refused stale answer leaves the authoritative snapshot byte-identical.");
        }

        [Test]
        public void FixedSeedDriver_ReachesTheConfiguredBroganFamilyAndDoesNotStall()
        {
            var fixture = ConfiguredBroganPause.Create();
            Assert.That(DecisionFamilyInventory.CurrentFamily(fixture.Session.Projection),
                Is.EqualTo("Removal prevention"),
                "The registered Tier III Brogan content path reaches the family before the driver acts.");

            var record = DeterministicMatchLoop.Play(fixture.Session, seed: 82504, maximumCommands: 1);

            Assert.That(record.Stalled, Is.False);
            Assert.That(record.ExhaustedItsBudget, Is.True,
                "One command deliberately bounds this probe; it is not expected to finish the match.");
            Assert.That(record.Commands, Has.Count.EqualTo(1));
            Assert.That(record.Commands[0].Kind,
                Is.EqualTo(DirectorCommandKind.AnswerRemovalPrevention));
            Assert.That(record.Observed(DirectorCommandKind.AnswerRemovalPrevention), Is.True);
            Assert.That(fixture.Session.Projection.PendingRemovalPrevention, Is.Null,
                "The fixed-seed run drove the content-derived family rather than stalling on it.");
        }

        private static DecisionAnswerRow ProbeRow(
            string surface, DirectorCommandKind? commandKind) =>
            new DecisionAnswerRow(
                "Probe", "Answer", "PendingProbeChoice.Invocation", surface, commandKind,
                "The probe decision completes");

        private static void AssertAuditFailsWith(
            IReadOnlyList<string> errors, string expectedFragment)
        {
            Assert.That(errors.Any(error => error.Contains(expectedFragment)), Is.True,
                $"Expected an audit error containing '{expectedFragment}', but got: " +
                string.Join(" | ", errors));
        }

        private static void AssertEnumKindsCovered<T>(params string[] contexts) where T : Enum
        {
            var expected = Enum.GetValues(typeof(T)).Cast<T>()
                .Where(value => !string.Equals(value.ToString(), "None", StringComparison.Ordinal))
                .Select(value => value.ToString())
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            foreach (var context in contexts)
            {
                var actual = DecisionFamilyInventory.Families
                    .SelectMany(value => value.Answers)
                    .Where(value => value.Context == context)
                    .Select(value => value.Answer)
                    .Where(expected.Contains)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                Assert.That(actual, Is.EqualTo(expected),
                    $"{context} must deliberately own every non-None {typeof(T).Name} value.");
            }
        }

        private static CardDefinitionId DefinitionId(
            MatchSessionProjection projection, CardInstanceId instanceId) =>
            projection.Cards.Single(card => card.Id.Equals(instanceId))
                .DefinitionReference.DefinitionId;

        private static CardLocation CardLocationOf(
            MatchSessionProjection projection, CardInstanceId instanceId) =>
            projection.Cards.Single(card => card.Id.Equals(instanceId)).State.Location;

        internal static MatchSession CreateConfiguredRemovalPreventionPause() =>
            ConfiguredBroganPause.Create().Session;

        /// <summary>A configured Tier III Brogan match paused at Fortify's Attack-origin decision.</summary>
        private sealed class ConfiguredBroganPause
        {
            private ConfiguredBroganPause(MatchSession session, HeroId brogan, HeroId wasp)
            {
                Session = session;
                Brogan = brogan;
                Wasp = wasp;
            }

            public MatchSession Session { get; }
            public HeroId Brogan { get; }
            public HeroId Wasp { get; }

            public static ConfiguredBroganPause Create()
            {
                var configuration = MatchConfiguration.CreateStagedBroganTierThreeForInternalTest(
                    TeamSide.Red, BroganUpgradeBranch.FirstSibling);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusBroganOneVsOne(),
                    configuration.Participants, map, configuration.TieBreakerState);
                setup = EngagedMirrorStaging.Restage(map, setup, out _);
                var scenario = new InitialScenarioDescription(
                    configuration.Participants,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.TieBreakerState,
                    map.Reference,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                    Array.Empty<ObstacleOccupancy>());
                var fixture = new ConfiguredBroganPause(
                    new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map })),
                    setup.Heroes.Single(hero => hero.ContentId.Value == "brogan").HeroId,
                    setup.Heroes.Single(hero => hero.ContentId.Value == "wasp").HeroId);

                fixture.Commit(fixture.Brogan, BroganCardContent.Fortify);
                fixture.CommitLowest(fixture.Wasp);
                Assert.That(fixture.Session.Projection.SelectedActor.ActorId,
                    Is.EqualTo(fixture.Brogan),
                    "Fortify's Initiative 5 acts before Wasp's lowest card.");
                fixture.StartPrintedPrimary();
                Assert.That(fixture.Session.Projection.PendingEffectChoice, Is.Null,
                    "Fortify raises its guard without asking an effect choice.");
                fixture.HoldRemainingActor();
                Assert.That(fixture.Session.Projection.Phase,
                    Is.EqualTo(RoundResolutionPhase.AwaitingCommitments),
                    "The next selected-card turn must remain in Fortify's round.");

                fixture.Commit(fixture.Wasp, WaspCardContent.Electroblast);
                fixture.Commit(fixture.Brogan, BroganCardContent.FuriousCharge);
                Assert.That(fixture.Session.Projection.SelectedActor.ActorId,
                    Is.EqualTo(fixture.Wasp),
                    "Electroblast's Initiative 9 acts before Furious Charge's 8.");
                fixture.StartPrintedPrimary();
                var target = fixture.Session.Projection.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null, "Electroblast opens its printed Attack target choice.");
                var guarded = target.LegalTargetMinionIds.Single();
                fixture.Accept(DirectorCommand.ChooseAttackTarget(
                    target.ResolutionId, target.ChoiceId, guarded));

                var prompt = fixture.Session.Projection.PendingRemovalPrevention;
                Assert.That(prompt, Is.Not.Null,
                    "The configured Attack must pause at the guarded minion's removal stage.");
                Assert.That(prompt.Origin, Is.EqualTo(RemovalPreventionOrigin.AttackMinionDefeat));
                Assert.That(prompt.DecidingHeroId, Is.EqualTo(fixture.Brogan));
                Assert.That(prompt.SubjectMinionId, Is.EqualTo(guarded));
                return fixture;
            }

            private void Commit(HeroId heroId, CardDefinitionId definitionId)
            {
                var card = Session.Projection.Cards.Single(value =>
                    value.OwnerId.Equals(heroId) &&
                    value.State.Location == CardLocation.Hand &&
                    value.DefinitionReference.DefinitionId.Equals(definitionId));
                Accept(DirectorCommand.CommitCard(heroId, card.Id));
            }

            private void CommitLowest(HeroId heroId)
            {
                var card = Session.Projection.Cards
                    .Where(value => value.OwnerId.Equals(heroId) &&
                        value.State.Location == CardLocation.Hand)
                    .OrderBy(value => Session.Projection.Definitions
                        .Single(definition => definition.Reference.Equals(value.DefinitionReference))
                        .Initiative.Value)
                    .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                    .First();
                Accept(DirectorCommand.CommitCard(heroId, card.Id));
            }

            private void StartPrintedPrimary()
            {
                var offer = Session.Projection.AvailableTurnActions.Single(value =>
                    value.IsAvailable && OriginOf(value) == ActionDefinitionOrigin.PrintedPrimary);
                Accept(DirectorCommand.StartAction(offer.ActionDefinitionId));
            }

            private void HoldRemainingActor()
            {
                // Milestone 8.35g. The hero before Wasp finished its own resolution, so the
                // turn waits for that player's release before Wasp is selected at all.
                if (Session.Projection.PendingTurnRelease != null)
                {
                    Accept(DirectorCommand.ReleaseTurn());
                }

                Assert.That(Session.Projection.IsTurnActive, Is.True,
                    "Wasp's selected card remains after Fortify resolves.");
                Assert.That(Session.Projection.SelectedActor.ActorId, Is.EqualTo(Wasp));
                Accept(DirectorCommand.PerformHold(Session.Projection.SelectedActor.CardInstanceId));

                // And again on the way out: Wasp's own resolution has now finished, and the
                // round does not move on until that player hands the turn over either.
                if (Session.Projection.PendingTurnRelease != null)
                {
                    Accept(DirectorCommand.ReleaseTurn());
                }
            }

            private ActionDefinitionOrigin OriginOf(ActionOffer offer) =>
                Session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Origin;

            private void Accept(DirectorCommand command)
            {
                var result = Session.Execute(command);
                Assert.That(result.Accepted, Is.True,
                    $"Configured Brogan setup command {command.Kind} was refused: {result.Detail}");
            }
        }
    }

    /// <summary>
    /// The test-owned semantic inventory for the current application projection. Family names are
    /// strings deliberately: this is a bounded reflection guard, not a production enum or a universal
    /// base-choice contract. Milestone 8.25e4.
    /// </summary>
    internal static class DecisionFamilyInventory
    {
        internal static readonly string[] ChoiceProjectionMembers =
        {
            nameof(MatchSessionProjection.PendingInitiativeChoice),
            nameof(MatchSessionProjection.PendingMovementChoice),
            nameof(MatchSessionProjection.PendingAttackTargetChoice),
            nameof(MatchSessionProjection.PendingDefenseChoice),
            nameof(MatchSessionProjection.PendingEffectChoice),
            nameof(MatchSessionProjection.PendingDisplacedMinionCorrection),
            nameof(MatchSessionProjection.PendingMinionBattleChoice),
            nameof(MatchSessionProjection.PendingHeroRespawnChoice),
            nameof(MatchSessionProjection.PendingSystemTransitionStep),
            nameof(MatchSessionProjection.PendingRemovalPrevention),
            nameof(MatchSessionProjection.PendingTurnRelease),
            nameof(MatchSessionProjection.PendingActingBodyChoice)
        };

        internal static readonly string[] CompanionProjectionMembers =
        {
            nameof(MatchSessionProjection.PendingMovementResolution),
            nameof(MatchSessionProjection.PendingAttackResolution),
            nameof(MatchSessionProjection.PendingAttackTargetDefensePreviews),
            nameof(MatchSessionProjection.PendingSkillResolution),
            nameof(MatchSessionProjection.PendingEffectChoicePreview),
            nameof(MatchSessionProjection.PendingDefensePreview),
            nameof(MatchSessionProjection.PendingLanePushRequest),
            nameof(MatchSessionProjection.PendingSystemTransitionDescription),

            // Milestone 8.35u. The same companion, once per open item, because rulebook p.15 asks
            // every hero that bought a level at the same time and each of them is asking a different
            // question. It is a companion and not a family for exactly the reason the singular one
            // is: it describes the pause the system-transition family already stands on, and adds no
            // decision of its own.
            nameof(MatchSessionProjection.PendingSystemTransitionDescriptions),
            nameof(MatchSessionProjection.PendingImmediateLanePush)
        };

        internal static readonly DecisionFamilyDefinition[] Families =
        {
            Family("Initiative", nameof(MatchSessionProjection.PendingInitiativeChoice),
                projection => projection.PendingInitiativeChoice != null,
                Row("Initiative", "Choose tied actor",
                    "PendingInitiativeChoice.CandidateCardInstanceIds[*]",
                    "Initiative prompt control", DirectorCommandKind.ChooseTiedActor,
                    "The chosen tied actor becomes selected")),

            Family("Movement", nameof(MatchSessionProjection.PendingMovementChoice),
                projection => projection.PendingMovementChoice != null,
                Row("Movement", "Continue",
                    "PendingMovementChoice.LegalDestinations[*].Destination",
                    "Board destination cue", DirectorCommandKind.ChooseMovementDestination,
                    "Movement commits the projected destination and continues"),
                Row("Movement", "Stop", "PendingMovementChoice.CanStop + choice identity",
                    "End Turn / stop control", DirectorCommandKind.StopMovement,
                    "Movement completes at the current space")),

            Family("Attack target", nameof(MatchSessionProjection.PendingAttackTargetChoice),
                projection => projection.PendingAttackTargetChoice != null,
                Row("Attack target", "Hero",
                    "PendingAttackTargetChoice.LegalTargetHeroIds[*]",
                    "Board hero target cue", DirectorCommandKind.ChooseAttackTarget,
                    "The Attack advances against the projected hero"),
                Row("Attack target", "Minion",
                    "PendingAttackTargetChoice.LegalTargetMinionIds[*]",
                    "Board minion target cue", DirectorCommandKind.ChooseAttackTarget,
                    "The Attack advances against the projected minion")),

            Family("Defense", nameof(MatchSessionProjection.PendingDefenseChoice),
                projection => projection.PendingDefenseChoice != null,
                Row("Defense", "Numeric Defense", "PendingDefenseChoice.LegalDefenseOffers[*]",
                    "Defense card face", DirectorCommandKind.ChooseDefenseCard,
                    "The named card pays the numeric Defense response"),
                Row("Defense", "Fulfilled Block", "PendingDefenseChoice.LegalDefenseOffers[*]",
                    "Defense card face", DirectorCommandKind.ChooseDefenseCard,
                    "The named card fulfills its Block response"),
                Row("Defense", "Unfulfilled Block", "PendingDefenseChoice.LegalDefenseOffers[*]",
                    "Defense card face", DirectorCommandKind.ChooseDefenseCard,
                    "The named card resolves its unfulfilled Block response"),
                Row("Defense", "Decline", "PendingDefenseChoice.CanDecline + choice identity",
                    "Defense decline control", DirectorCommandKind.DeclineDefense,
                    "The defender declines and combat advances")),

            Family("Effect", nameof(MatchSessionProjection.PendingEffectChoice),
                projection => projection.PendingEffectChoice != null,
                EffectRows("Effect/Skill"),
                EffectRows("Effect/Attack or Block")),

            Family("Correction", nameof(MatchSessionProjection.PendingDisplacedMinionCorrection),
                projection => projection.PendingDisplacedMinionCorrection != null,
                Row("Displaced minion correction", DisplacedMinionCorrectionCommandKind.SelectSubject.ToString(),
                    "PendingDisplacedMinionCorrection.SelectInvocations[*]",
                    "Correction subject board cue", DirectorCommandKind.ChooseCorrectionSubject,
                    "The projected displaced subject is selected"),
                Row("Displaced minion correction", DisplacedMinionCorrectionCommandKind.SelectDestination.ToString(),
                    "PendingDisplacedMinionCorrection.SelectInvocations[*]",
                    "Correction destination board cue", DirectorCommandKind.ChooseCorrectionDestination,
                    "The projected legal return space is selected")),

            Family("Minion Battle", nameof(MatchSessionProjection.PendingMinionBattleChoice),
                projection => projection.PendingMinionBattleChoice != null,
                Row("Minion Battle", MinionBattleChoiceCommandKind.Select.ToString(),
                    "PendingMinionBattleChoice.SelectInvocations[*]",
                    "Minion Battle board candidate", DirectorCommandKind.SelectMinionBattleRemoval,
                    "The projected unit is selected for removal"),
                Row("Minion Battle", MinionBattleChoiceCommandKind.Deselect.ToString(),
                    "PendingMinionBattleChoice.DeselectInvocations[*]",
                    "Selected Minion Battle board candidate", DirectorCommandKind.DeselectMinionBattleRemoval,
                    "The projected selected unit is deselected"),
                Row("Minion Battle", MinionBattleChoiceCommandKind.Confirm.ToString(),
                    "PendingMinionBattleChoice.ConfirmInvocation",
                    "Minion Battle confirm control", DirectorCommandKind.ConfirmMinionBattleRemovals,
                    "The complete projected removal set is committed")),

            Family("Hero respawn", nameof(MatchSessionProjection.PendingHeroRespawnChoice),
                projection => projection.PendingHeroRespawnChoice != null,
                Row("Hero respawn", HeroRespawnCommandKind.Respawn.ToString(),
                    "PendingHeroRespawnChoice.RespawnInvocations[*]",
                    "Respawn destination board cue", DirectorCommandKind.RespawnHero,
                    "The hero respawns at the projected destination"),
                Row("Hero respawn", HeroRespawnCommandKind.ResolveWithoutAction.ToString(),
                    "PendingHeroRespawnChoice.DeclineInvocation",
                    "Resolve-without-action control", DirectorCommandKind.ResolveCardWithoutRespawn,
                    "The selected card resolves without a respawn action")),

            Family("System transition", nameof(MatchSessionProjection.PendingSystemTransitionStep),
                projection => projection.PendingSystemTransitionStep != null,
                Row("System transition", "Lane-Push respawn/Place",
                    "PendingSystemTransitionStep.Invocations[*]",
                    "Paused transition option control", DirectorCommandKind.AnswerSystemTransitionStep,
                    "The selected Lane-Push transition step commits"),
                Row("System transition", "Level-Up upgrade",
                    "PendingSystemTransitionStep.Invocations[*]",
                    "Paused transition option control", DirectorCommandKind.AnswerSystemTransitionStep,
                    "The selected Level-Up transition step commits")),

            Family("Removal prevention", nameof(MatchSessionProjection.PendingRemovalPrevention),
                projection => projection.PendingRemovalPrevention != null,
                Row("Removal prevention", "Projected payment",
                    "PendingRemovalPrevention.Payments[*].Invocation",
                    "Payment card face", DirectorCommandKind.AnswerRemovalPrevention,
                    "The named eligible card is spent and the defeated minion remains"),
                Row("Removal prevention", "Decline",
                    "PendingRemovalPrevention.DeclineInvocation",
                    "Removal-prevention decline control", DirectorCommandKind.AnswerRemovalPrevention,
                    "Nothing is spent and the defeated minion is removed")),
            // Milestone 8.35g. The eleventh, and the only one whose whole content is a consent:
            // one command, no candidates, and the same control that finished the resolution.
            Family("Turn release", nameof(MatchSessionProjection.PendingTurnRelease),
                projection => projection.PendingTurnRelease != null,
                Row("Turn release", "Release the finished turn",
                    "PendingTurnRelease",
                    "End Turn control", DirectorCommandKind.ReleaseTurn,
                    "End-of-turn timing runs and the turn is handed to the next actor")),
            // Milestone 9.21b. The twelfth: "which of you acts" (RZ-Q4), one answer per standing body
            // of a hero on several spaces, before his action is chosen. Its visible surface - the
            // standee click - is owed by 9.21i and parked in the conformance fixture until then.
            Family("Acting body", nameof(MatchSessionProjection.PendingActingBodyChoice),
                projection => projection.PendingActingBodyChoice != null,
                Row("Acting body", "Pick a standing body",
                    "PendingActingBodyChoice.Invocations[*]",
                    "Standee pick (owed by 9.21i)", DirectorCommandKind.ChooseActingBody,
                    "The named body performs the card's action"))
        };

        internal static readonly NonPendingOfferDefinition[] NonPendingOffers =
        {
            new NonPendingOfferDefinition(
                "Commitments", "Commitments[*] + Cards in Hand", "Commitment card faces / pass",
                new[] { DirectorCommandKind.CommitCard, DirectorCommandKind.Pass },
                "One awaiting seat commits a card or passes"),
            new NonPendingOfferDefinition(
                "Ordinary action selection", "AvailableTurnActions[*]", "Turn action controls",
                new[] { DirectorCommandKind.StartAction }, "The selected action starts"),
            new NonPendingOfferDefinition(
                "Hold", "SelectedActor + GeneralRuleActionOfferQuery", "End Turn control",
                new[] { DirectorCommandKind.PerformHold }, "The selected card resolves as Hold"),
            new NonPendingOfferDefinition(
                "Fast Travel", "AvailableFastTravelOptions[*].LegalDestinations[*]",
                "Fast Travel destination surface", new[] { DirectorCommandKind.PerformFastTravel },
                "The replacement Movement reaches the projected destination"),
            new NonPendingOfferDefinition(
                "Clear", "AvailableClearOptions[*].SubsetInvocations[*]", "Clear token selection surface",
                new[] { DirectorCommandKind.PerformClear }, "The exact projected token subset is cleared"),
            new NonPendingOfferDefinition(
                "End-of-round advance", "Phase == EndOfRound", "Continue end-of-round control",
                new[] { DirectorCommandKind.AdvanceEndOfRound }, "The next end-of-round stage advances")
        };

        internal static string[] ReflectedPendingMembers => typeof(MatchSessionProjection)
            .GetProperties()
            .Select(property => property.Name)
            .Where(name => name.StartsWith("Pending", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        internal static IReadOnlyList<string> AuditProductionInventory() =>
            Audit(ReflectedPendingMembers, ChoiceProjectionMembers, CompanionProjectionMembers, Families);

        internal static IReadOnlyList<string> Audit(
            IEnumerable<string> reflectedPendingMembers,
            IEnumerable<string> choiceProjectionMembers,
            IEnumerable<string> companionProjectionMembers,
            IEnumerable<DecisionFamilyDefinition> definitions)
        {
            var errors = new List<string>();
            var reflected = reflectedPendingMembers.ToArray();
            var choices = choiceProjectionMembers.ToArray();
            var companions = companionProjectionMembers.ToArray();
            var families = definitions.ToArray();

            foreach (var member in reflected.Distinct(StringComparer.Ordinal))
            {
                var classifications = choices.Count(value => value == member) +
                    companions.Count(value => value == member);
                if (classifications != 1)
                {
                    errors.Add($"{member} is classified {classifications} times as choice or companion");
                }
            }

            foreach (var classified in choices.Concat(companions).Distinct(StringComparer.Ordinal))
            {
                var occurrences = reflected.Count(value => value == classified);
                if (occurrences != 1)
                {
                    errors.Add($"{classified} appears {occurrences} times in reflected Pending members");
                }
            }

            foreach (var choice in choices.Distinct(StringComparer.Ordinal))
            {
                var owners = families.Count(value => value.ProjectionMember == choice);
                if (owners != 1)
                {
                    errors.Add($"{choice} maps to {owners} decision families");
                }
            }

            foreach (var family in families)
            {
                if (choices.Count(value => value == family.ProjectionMember) != 1)
                {
                    errors.Add($"{family.Family} maps to a companion or unclassified member " +
                        family.ProjectionMember);
                }

                if (family.IsStanding == null)
                {
                    errors.Add(family.Family + " has no standing-family detector");
                }

                if (family.Answers.Count == 0)
                {
                    errors.Add(family.Family + " has no answer rows");
                }

                foreach (var answer in family.Answers)
                {
                    var key = family.Family + "/" + answer.Context + "/" + answer.Answer;
                    if (string.IsNullOrWhiteSpace(answer.ProjectedInvocation))
                        errors.Add(key + " has no projected invocation");
                    if (string.IsNullOrWhiteSpace(answer.VisibleSurface))
                        errors.Add(key + " has no visible surface");
                    if (!answer.CommandKind.HasValue)
                        errors.Add(key + " has no DirectorCommand owner");
                    if (string.IsNullOrWhiteSpace(answer.AcceptedResult))
                        errors.Add(key + " has no accepted result");
                }

                if (family.Answers.Select(answer => answer.Context + "/" + answer.Answer)
                    .Distinct(StringComparer.Ordinal).Count() != family.Answers.Count)
                {
                    errors.Add(family.Family + " has ambiguous duplicate answer rows");
                }
            }

            if (families.Select(value => value.Family).Distinct(StringComparer.Ordinal).Count() !=
                families.Length)
            {
                errors.Add("Semantic decision-family identities are not unique");
            }

            foreach (var offer in NonPendingOffers)
            {
                if (offer.ProjectionSource.StartsWith("Pending", StringComparison.Ordinal))
                    errors.Add(offer.Offer + " is incorrectly recorded as a Pending family");
                if (string.IsNullOrWhiteSpace(offer.VisibleSurface) ||
                    offer.CommandKinds.Count == 0 ||
                    string.IsNullOrWhiteSpace(offer.AcceptedResult))
                    errors.Add(offer.Offer + " has an incomplete non-Pending offer contract");
            }

            return errors;
        }

        internal static string CurrentFamily(MatchSessionProjection projection)
        {
            var standing = Families.Where(value => value.IsStanding(projection)).ToArray();
            Assert.That(standing.Length, Is.LessThanOrEqualTo(1),
                "One immutable projection may expose at most one exclusive pending decision family.");
            return standing.SingleOrDefault()?.Family;
        }

        /// <summary>
        /// Independently enumerates the exact commands minted by whichever pending family stands.
        /// Deselect can be included for inventory checks and excluded for a progressing deterministic
        /// run; no other advancing answer is omitted.
        /// </summary>
        internal static IReadOnlyList<DirectorCommand> ProjectedCommands(
            MatchSessionProjection projection, bool includeDeselect)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var commands = new List<DirectorCommand>();

            var initiative = projection.PendingInitiativeChoice;
            if (initiative != null)
            {
                commands.AddRange(initiative.CandidateCardInstanceIds.Select(card =>
                    DirectorCommand.ChooseTiedActor(initiative.OwningSide, card)));
                return commands;
            }

            var movement = projection.PendingMovementChoice;
            if (movement != null)
            {
                commands.AddRange(movement.LegalDestinations.Select(destination =>
                    DirectorCommand.ChooseMovementDestination(
                        movement.ResolutionId, movement.ChoiceId, destination.Destination)));
                if (movement.CanStop)
                    commands.Add(DirectorCommand.StopMovement(movement.ResolutionId, movement.ChoiceId));
                return commands;
            }

            var attack = projection.PendingAttackTargetChoice;
            if (attack != null)
            {
                commands.AddRange(attack.LegalTargetHeroIds.Select(hero =>
                    DirectorCommand.ChooseAttackTarget(attack.ResolutionId, attack.ChoiceId, hero)));
                commands.AddRange(attack.LegalTargetMinionIds.Select(minion =>
                    DirectorCommand.ChooseAttackTarget(attack.ResolutionId, attack.ChoiceId, minion)));
                return commands;
            }

            var defense = projection.PendingDefenseChoice;
            if (defense != null)
            {
                commands.AddRange(defense.LegalDefenseOffers.Select(offer =>
                    DirectorCommand.ChooseDefenseCard(
                        defense.ResolutionId, defense.ChoiceId, offer.SourceCardInstanceId)));
                if (defense.CanDecline)
                    commands.Add(DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
                return commands;
            }

            var effect = projection.PendingEffectChoice;
            if (effect != null)
            {
                commands.AddRange(effect.SelectInvocations.Select(DirectorCommand.AnswerEffectChoice));
                if (includeDeselect)
                    commands.AddRange(effect.DeselectInvocations.Select(DirectorCommand.AnswerEffectChoice));
                if (effect.ConfirmInvocation != null)
                    commands.Add(DirectorCommand.AnswerEffectChoice(effect.ConfirmInvocation));
                if (effect.DeclineInvocation != null)
                    commands.Add(DirectorCommand.AnswerEffectChoice(effect.DeclineInvocation));
                if (effect.StopInvocation != null)
                    commands.Add(DirectorCommand.AnswerEffectChoice(effect.StopInvocation));
                return commands;
            }

            var correction = projection.PendingDisplacedMinionCorrection;
            if (correction != null)
            {
                commands.AddRange(correction.SelectInvocations.Select(invocation =>
                    invocation.Kind == DisplacedMinionCorrectionCommandKind.SelectSubject
                        ? DirectorCommand.ChooseCorrectionSubject(invocation)
                        : DirectorCommand.ChooseCorrectionDestination(invocation)));
                return commands;
            }

            var battle = projection.PendingMinionBattleChoice;
            if (battle != null)
            {
                commands.AddRange(battle.SelectInvocations.Select(
                    DirectorCommand.SelectMinionBattleRemoval));
                if (includeDeselect)
                    commands.AddRange(battle.DeselectInvocations.Select(
                        DirectorCommand.DeselectMinionBattleRemoval));
                if (battle.CanConfirm)
                    commands.Add(DirectorCommand.ConfirmMinionBattleRemovals(battle.ConfirmInvocation));
                return commands;
            }

            var respawn = projection.PendingHeroRespawnChoice;
            if (respawn != null)
            {
                commands.AddRange(respawn.RespawnInvocations.Select(DirectorCommand.RespawnHero));
                commands.Add(DirectorCommand.ResolveCardWithoutRespawn(respawn.DeclineInvocation));
                return commands;
            }

            var transition = projection.PendingSystemTransitionStep;
            if (transition != null)
            {
                commands.AddRange(transition.Invocations.Select(
                    DirectorCommand.AnswerSystemTransitionStep));
                return commands;
            }

            var removal = projection.PendingRemovalPrevention;
            if (removal != null)
            {
                commands.AddRange(removal.Payments.Select(payment =>
                    DirectorCommand.AnswerRemovalPrevention(payment.Invocation)));
                commands.Add(DirectorCommand.AnswerRemovalPrevention(removal.DeclineInvocation));
                return commands;
            }

            // Milestone 8.35g. One command and no candidates: the finished turn is handed on, or
            // it is not handed on yet. There is nothing else a consent can offer.
            if (projection.PendingTurnRelease != null)
            {
                commands.Add(DirectorCommand.ReleaseTurn());
                return commands;
            }

            // Milestone 9.21b. One answer per standing body, in body order.
            var acting = projection.PendingActingBodyChoice;
            if (acting != null)
            {
                commands.AddRange(acting.Invocations.Select(DirectorCommand.ChooseActingBody));
                return commands;
            }

            return commands;
        }

        private static DecisionFamilyDefinition Family(
            string family, string projectionMember, Func<MatchSessionProjection, bool> isStanding,
            params object[] answerGroups)
        {
            var answers = answerGroups
                .SelectMany(group => group is DecisionAnswerRow row
                    ? new[] { row }
                    : (DecisionAnswerRow[])group)
                .ToArray();
            return new DecisionFamilyDefinition(family, projectionMember, isStanding, answers);
        }

        private static DecisionAnswerRow[] EffectRows(string context) => new[]
        {
            Row(context, EffectChoiceCommandKind.Select.ToString(),
                "PendingEffectChoice.SelectInvocations[*]", "Effect candidate surface",
                DirectorCommandKind.AnswerEffectChoice, "The projected candidate becomes selected"),
            Row(context, EffectChoiceCommandKind.Deselect.ToString(),
                "PendingEffectChoice.DeselectInvocations[*]", "Selected effect candidate surface",
                DirectorCommandKind.AnswerEffectChoice, "The projected candidate becomes deselected"),
            Row(context, EffectChoiceCommandKind.Confirm.ToString(),
                "PendingEffectChoice.ConfirmInvocation", "Effect confirm control",
                DirectorCommandKind.AnswerEffectChoice, "The current effect selection commits"),
            Row(context, EffectChoiceCommandKind.Decline.ToString(),
                "PendingEffectChoice.DeclineInvocation", "Effect decline control",
                DirectorCommandKind.AnswerEffectChoice, "The optional effect step declines"),
            Row(context, EffectChoiceCommandKind.Stop.ToString(),
                "PendingEffectChoice.StopInvocation", "Effect stop control",
                DirectorCommandKind.AnswerEffectChoice, "The repeatable effect step stops")
        };

        private static DecisionAnswerRow Row(
            string context, string answer, string projectedInvocation, string visibleSurface,
            DirectorCommandKind commandKind, string acceptedResult) =>
            new DecisionAnswerRow(
                context, answer, projectedInvocation, visibleSurface, commandKind, acceptedResult);
    }

    internal sealed class DecisionFamilyDefinition
    {
        internal DecisionFamilyDefinition(
            string family, string projectionMember,
            Func<MatchSessionProjection, bool> isStanding,
            params DecisionAnswerRow[] answers)
        {
            Family = family;
            ProjectionMember = projectionMember;
            IsStanding = isStanding;
            Answers = Array.AsReadOnly(answers ?? Array.Empty<DecisionAnswerRow>());
        }

        internal string Family { get; }
        internal string ProjectionMember { get; }
        internal Func<MatchSessionProjection, bool> IsStanding { get; }
        internal IReadOnlyList<DecisionAnswerRow> Answers { get; }
    }

    internal sealed class DecisionAnswerRow
    {
        internal DecisionAnswerRow(
            string context, string answer, string projectedInvocation, string visibleSurface,
            DirectorCommandKind? commandKind, string acceptedResult)
        {
            Context = context;
            Answer = answer;
            ProjectedInvocation = projectedInvocation;
            VisibleSurface = visibleSurface;
            CommandKind = commandKind;
            AcceptedResult = acceptedResult;
        }

        internal string Context { get; }
        internal string Answer { get; }
        internal string ProjectedInvocation { get; }
        internal string VisibleSurface { get; }
        internal DirectorCommandKind? CommandKind { get; }
        internal string AcceptedResult { get; }
    }

    internal sealed class NonPendingOfferDefinition
    {
        internal NonPendingOfferDefinition(
            string offer, string projectionSource, string visibleSurface,
            IEnumerable<DirectorCommandKind> commandKinds, string acceptedResult)
        {
            Offer = offer;
            ProjectionSource = projectionSource;
            VisibleSurface = visibleSurface;
            CommandKinds = Array.AsReadOnly(commandKinds.ToArray());
            AcceptedResult = acceptedResult;
        }

        internal string Offer { get; }
        internal string ProjectionSource { get; }
        internal string VisibleSurface { get; }
        internal IReadOnlyList<DirectorCommandKind> CommandKinds { get; }
        internal string AcceptedResult { get; }
    }
}
