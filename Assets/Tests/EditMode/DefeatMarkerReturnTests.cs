using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.12's second cleanup checkpoint: rulebook p.13, "When a Hero is Defeated" -
    /// "Defeated hero's actions with Active effects are canceled. Any markers they had are returned."
    ///
    /// Only the marker clause is this milestone's. The hero's removal from the board, the effect
    /// cancellation beside it, the Life counters, the coins, and the respawn all remain deferred, and
    /// the cases below assert the return without asserting any of them - which is the honest shape,
    /// because the Defeat outcome is recorded today and those consequences are not.
    ///
    /// The token half of the page is a negative and it is asserted too: p.19 says outright that "tokens
    /// are not removed when the hero who placed them is defeated", so a defeat that cleared the board
    /// would be the one outcome the rules rule out.
    /// </summary>
    public sealed class DefeatMarkerReturnTests
    {
        [Test]
        public void ADefeatedHero_ReturnsEveryMarkerItHeldToTheSupply()
        {
            var fixture = CreateFixture(out var markers, out var board);
            var attachment = GiveFocusTo(markers, fixture.DefenderId, fixture.AttackerId);
            Assert.That(markers.CountAvailable(SyntheticComponentDefinitions.FocusDefinition), Is.Zero);

            var outcome = DefeatTheDefender(fixture);

            Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
            Assert.That(outcome.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackSuccessful,
                CardActionResolutionEventKind.HeroDefeatRecorded,
                CardActionResolutionEventKind.MarkerReturnedOnHeroDefeat,
                CardActionResolutionEventKind.AttackResolutionCompleted
            }), "p.13 returns the markers after the Defeat it is a consequence of.");

            var returned = outcome.Events.Single(ruleEvent =>
                ruleEvent.Kind == CardActionResolutionEventKind.MarkerReturnedOnHeroDefeat);
            Assert.That(returned.ReturnedMarkerInstanceId, Is.EqualTo(attachment.Id));
            Assert.That(returned.ReturnedMarkerDefinitionReference,
                Is.EqualTo(SyntheticComponentDefinitions.FocusV1));
            Assert.That(returned.TargetHeroId, Is.EqualTo(fixture.DefenderId),
                "The event names the holder the marker left, which p.13 calls the defeated hero.");

            Assert.That(markers.Attachments, Is.Empty);
            Assert.That(markers.CountAvailable(SyntheticComponentDefinitions.FocusDefinition),
                Is.EqualTo(SyntheticComponentDefinitions.FocusDefinition.SupplyCount),
                "p.13: the marker is returned, which means back into the supply.");
            Assert.That(board.PlacedTokens, Is.Empty);
        }

        [Test]
        public void ADefeatedHero_LeavesMarkersItGaveAwayAndTokensItPlacedAlone()
        {
            var fixture = CreateFixture(out var markers, out var board);

            // The defeated hero is this marker's owner but not its holder. p.13 returns what the hero
            // "had", and the attachment records holder and owner separately for exactly that reason.
            var givenAway = GiveFocusTo(markers, fixture.AttackerId, owner: fixture.DefenderId);
            var placed = PlaceHazardFor(board, fixture.DefenderId, new HexCoordinate(2, 0));

            var outcome = DefeatTheDefender(fixture);

            Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
            Assert.That(outcome.Events.Select(ruleEvent => ruleEvent.Kind),
                Has.No.Member(CardActionResolutionEventKind.MarkerReturnedOnHeroDefeat));
            Assert.That(markers.Attachments.Single().Id, Is.EqualTo(givenAway.Id),
                "A marker the defeated hero gave to someone else is still held by that someone else.");
            Assert.That(board.PlacedTokens.Single().Id, Is.EqualTo(placed.Id),
                "Rulebook p.19: \"Tokens are not removed when the hero who placed them is defeated.\"");
        }

        [Test]
        public void ASuccessfullyDefendedAttack_ReturnsNothing()
        {
            var fixture = CreateFixture(out var markers, out _);
            GiveFocusTo(markers, fixture.DefenderId, fixture.AttackerId);

            var outcome = ResolveAgainstDefense(fixture, strongest: true);

            Assert.That(outcome.Outcome, Is.Not.EqualTo(AttackResolutionOutcome.AttackSuccessful));
            Assert.That(outcome.Events.Select(ruleEvent => ruleEvent.Kind),
                Has.No.Member(CardActionResolutionEventKind.MarkerReturnedOnHeroDefeat),
                "No Defeat, so p.13 is never reached and the holder keeps the marker.");
            Assert.That(markers.Attachments, Has.Count.EqualTo(1));
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static MarkerAttachment GiveFocusTo(
            MarkerAttachmentRegister markers,
            HeroId holder,
            HeroId owner)
        {
            var attachment = new MarkerAttachment(
                new MarkerInstanceId($"defeat-return-focus-{holder.Value}"),
                SyntheticComponentDefinitions.FocusDefinition,
                owner,
                holder,
                new CardInstanceId("defeat-return-source"),
                new ActionDefinitionId("primary-component-skill"));
            markers.Give(attachment);
            return attachment;
        }

        private static PlacedToken PlaceHazardFor(
            AdjacentMoveAuthority board,
            HeroId owner,
            HexCoordinate coordinate)
        {
            var token = new PlacedToken(
                new TokenInstanceId("defeat-return-hazard"),
                SyntheticComponentDefinitions.CreateCatalog()
                    .LoadToken(SyntheticComponentDefinitions.HazardV4),
                owner,
                owner,
                new CardInstanceId("defeat-return-source"),
                coordinate);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        /// <summary>
        /// Drives the Attack to a successful outcome through the lowest Defense the defender holds,
        /// which is the same route <c>AttackActionResolutionAuthorityTests</c> uses to record a Defeat.
        /// </summary>
        private static AttackActionResolutionResult DefeatTheDefender(DefeatFixture fixture) =>
            ResolveAgainstDefense(fixture, strongest: false);

        /// <summary>
        /// Resolves the Attack against either the weakest or the strongest Defense the defender can
        /// still play. The offers are chosen by their printed Defense total rather than by card name,
        /// because the showcase conversions move which card prints what and this fixture cares only
        /// about which side of the comparison the total lands on.
        /// </summary>
        private static AttackActionResolutionResult ResolveAgainstDefense(
            DefeatFixture fixture,
            bool strongest)
        {
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("defeat-return-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);
            var opened = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);
            var ordered = opened.PendingDefenseChoice.LegalDefenseOffers
                .OrderBy(offer => PrintedValueQuery.CalculatePrintedValue(
                    offer, fixture.Configuration.CardDefinitions, PrintedValueChannel.Defense).Total)
                .ToArray();
            var defenseOffer = strongest ? ordered[ordered.Length - 1] : ordered[0];
            fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);

            return fixture.Authority.ResolveAttackOutcome(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty);
        }

        private static DefeatFixture CreateFixture(
            out MarkerAttachmentRegister markers,
            out AdjacentMoveAuthority board)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            board = new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(1, 0))
                });
            markers = new MarkerAttachmentRegister();
            var offer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    attackerId,
                    ActionOfferWindow.NormalPlay)
                .Single(candidate =>
                    candidate.SourceCardInstanceId.Value == "red-pursuit-card" &&
                    candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            return new DefeatFixture(
                configuration,
                attackerId,
                defenderId,
                new CardActionResolutionAuthority(board, markers),
                offer);
        }

        private static HexCoordinate[] CreateRadiusThreeCoordinates()
        {
            var coordinates = new System.Collections.Generic.List<HexCoordinate>();
            for (var q = -3; q <= 3; q++)
            {
                for (var r = -3; r <= 3; r++)
                {
                    if (Math.Abs(q + r) <= 3)
                    {
                        coordinates.Add(new HexCoordinate(q, r));
                    }
                }
            }

            return coordinates.ToArray();
        }

        private sealed class DefeatFixture
        {
            public DefeatFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                HeroId attackerId,
                HeroId defenderId,
                CardActionResolutionAuthority authority,
                ActionOffer attackOffer)
            {
                Configuration = configuration;
                AttackerId = attackerId;
                DefenderId = defenderId;
                Authority = authority;
                AttackOffer = attackOffer;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public HeroId AttackerId { get; }
            public HeroId DefenderId { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer AttackOffer { get; }
        }
    }
}
