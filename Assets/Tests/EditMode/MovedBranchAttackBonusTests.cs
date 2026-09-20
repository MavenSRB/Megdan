using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39g2. The `+2 Attack` Cutter's three lunges print on the branch that moves, as the
    /// seventh <see cref="RosterValueQueryKind"/>.
    ///
    /// <para><b>Why a query kind and not something cheaper.</b> Daring Strike, Bold Thrust and Fearless
    /// Lunge each print "<b>Choose one —</b> • Before the attack: Move 1[/2/3] space[s] in a straight
    /// line. Target a hero adjacent to you in the direction of the move; <b>+2 Attack</b>. • Target a
    /// unit adjacent to you." The bonus belongs to one bullet. Every printed <c>+N</c> in this build is
    /// a <see cref="RosterValueContributionDefinition"/> declared on the ACTION, which cannot be
    /// arm-conditional, and there is no node-level value grant at all - so before this milestone the
    /// clause had nowhere to go. The cheaper route, an Active effect created by the bullet, was
    /// rejected: <see cref="RuleValueApplicability"/> has no member scoped to one action, so it would
    /// also raise a repeat of this Attack or one granted later in the same turn.</para>
    ///
    /// <para><b>The question is `actor-moved-this-action@1`'s, asked in a second place rather than
    /// re-decided</b>, and <see cref="TheCapabilityAndTheValueQuery_AnswerTheSameQuestion"/> is that
    /// claim asserted rather than assumed.</para>
    /// </summary>
    [TestFixture]
    public sealed class MovedBranchAttackBonusTests
    {
        private static readonly HeroId Red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("moved-branch-bonus");

        private static readonly HexCoordinate Start = new HexCoordinate(0, 0);
        private static readonly HexCoordinate Lunged = new HexCoordinate(1, 0);
        private static readonly HexCoordinate TargetAt = new HexCoordinate(2, 0);

        /// <summary>The printed amount on all three of her lunges.</summary>
        private const int PrintedBonus = 2;

        // -------------------------------------------------------------------------------------------
        // The clause
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The moving bullet. She began the action on one space and is standing on another, so the
        /// printed amount is contributed once - the arithmetic every predicate member here shares.
        /// </summary>
        [Test]
        public void TheBonusApplies_WhenTheActorHasLeftTheSpaceTheAttackStartedFrom()
        {
            var contributions = Contributions(actorAt: Lunged, startedAt: Start);

            Assert.That(contributions.Count, Is.EqualTo(1));
            Assert.That(contributions.Single().Amount, Is.EqualTo(PrintedBonus));
            Assert.That(
                contributions.Single().Kind, Is.EqualTo(ValueContributionKind.CardTextModifier),
                "p.14's card text raised the total, which is what the printed + announces.");
        }

        /// <summary>
        /// The second bullet - "Target a unit adjacent to you" - performs no move, so the hero is
        /// standing where the action started and the clause contributes nothing. This is the case that
        /// makes the bonus belong to one bullet rather than to the card.
        /// </summary>
        [Test]
        public void TheBonusDoesNotApply_WhenTheActorStandsWhereTheAttackStarted()
        {
            Assert.That(Contributions(actorAt: Start, startedAt: Start), Is.Empty);
        }

        /// <summary>
        /// An Attack whose resolution recorded no starting space never asked the question, and answers
        /// nothing rather than guessing from a current position with nothing to compare against. That
        /// is `actor-moved-this-action@1`'s own reading of the same absence.
        /// </summary>
        [Test]
        public void AnAttackThatRecordedNoStartingSpace_ContributesNothing()
        {
            Assert.That(Contributions(actorAt: Lunged, startedAt: null), Is.Empty);
        }

        /// <summary>
        /// THE CLAIM THIS BLOCK RESTS ON, ASSERTED RATHER THAN ARGUED. Milestone 8.23's capability and
        /// this query are the same question, so they must agree on every board - two readings of "did
        /// you move" is exactly the divergence a shared label must never hide.
        /// </summary>
        [Test]
        public void TheCapabilityAndTheValueQuery_AnswerTheSameQuestion()
        {
            foreach (var at in new[] { Start, Lunged })
            {
                foreach (var startedAt in new HexCoordinate?[] { Start, null })
                {
                    var viaQuery = Contributions(at, startedAt).Count > 0;
                    var viaCapability = MovedAccordingToTheCapability(at, startedAt);

                    Assert.That(
                        viaQuery, Is.EqualTo(viaCapability),
                        $"at {at.Q},{at.R} started {startedAt?.Q.ToString() ?? "none"}: the value " +
                        "pipeline and the effect frame ask one question.");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // What it leaves alone
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A declaration binding one of the six earlier query kinds is unaffected by the new parameter,
        /// whether it is supplied or not - which is what makes this an addition rather than a change.
        /// </summary>
        [Test]
        public void TheExistingQueries_AreUnchangedWhetherTheNewFactIsSuppliedOrNot()
        {
            var withFact = Contributions(
                actorAt: Lunged,
                startedAt: Start,
                query: RosterValueQueryKind.EnemyUnitsAdjacentToActor);
            var withoutFact = Contributions(
                actorAt: Lunged,
                startedAt: null,
                query: RosterValueQueryKind.EnemyUnitsAdjacentToActor);

            Assert.That(
                withFact.Select(value => value.Amount),
                Is.EqualTo(withoutFact.Select(value => value.Amount)),
                "The action-start space is read by exactly one query and by no other.");
        }

        /// <summary>
        /// The Range channel cannot reach this query, and the refusal is EARLIER and stronger than this
        /// block expected: <see cref="RosterValueContributionDefinition"/>'s own constructor admits
        /// only <see cref="RosterValueQueryKind.EnemyUnitsAdjacentToActor"/> on that channel, because
        /// dynamic Range is derived before an Attack target exists. So a Range declaration naming the
        /// new query cannot be CONSTRUCTED, let alone evaluated - the closed-vocabulary-at-runtime
        /// discipline milestone 8.39b recorded, already in place here.
        ///
        /// <para>That is why the Range call site needs no starting space and passes none: the
        /// combination it would have to defend against is unrepresentable.</para>
        /// </summary>
        [Test]
        public void TheRangeChannel_RefusesThisQueryAtTheDeclarationRatherThanAtTheCallSite()
        {
            Assert.That(
                () => new RosterValueContributionDefinition(
                    PrintedValueChannel.Range, RosterValueQueryKind.ActorMovedThisAction,
                    PrintedBonus),
                Throws.ArgumentException,
                "Dynamic Range admits one query and this is not it.");

            Assert.That(
                () => new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack, RosterValueQueryKind.ActorMovedThisAction,
                    PrintedBonus),
                Throws.Nothing,
                "while the Attack channel, which is the one her lunges print on, admits it.");
        }

        /// <summary>
        /// The enum grew by one and the six before it kept their values, which is what stops a payload
        /// written before this milestone from decoding as a different query.
        /// </summary>
        [Test]
        public void TheSeventhQueryKind_IsAddedWithoutRenumberingTheSix()
        {
            Assert.That((int)RosterValueQueryKind.TargetPlayedAttackCardThisTurn, Is.EqualTo(1));
            Assert.That((int)RosterValueQueryKind.SourceCardNotInDiscard, Is.EqualTo(6));
            Assert.That((int)RosterValueQueryKind.ActorMovedThisAction, Is.EqualTo(7));
            Assert.That(
                Enum.GetValues(typeof(RosterValueQueryKind)).Cast<RosterValueQueryKind>()
                    .Count(value => value != RosterValueQueryKind.None),
                Is.EqualTo(9),
                "The nine queries preserve the original seven identities.");
            Assert.That((int)RosterValueQueryKind.OwnedDiscardCount, Is.EqualTo(8));

            // Milestone 8.69a. Brynn the Seeker's "+2 attack if you target a hero adjacent to 3 or
            // more obstacles" is the ninth, appended without renumbering any of the eight above -
            // which is the whole of what this case guards.
            Assert.That(
                (int)RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                Is.EqualTo(9));
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<ValueContribution> Contributions(
            HexCoordinate actorAt,
            HexCoordinate? startedAt,
            RosterValueQueryKind query = RosterValueQueryKind.ActorMovedThisAction)
        {
            var action = AttackAction(
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack, query, PrintedBonus));
            var card = Card(action);

            return RosterDynamicValueQuery.Contributions(
                action,
                Offer(card, action),
                AttackTarget.Hero(Blue),
                PrintedValueChannel.Attack,
                new[] { Definition(action) },
                new[] { card },
                Participants(),
                Board(actorAt),
                RuleModificationSet.Empty,
                Roster(),
                1,
                1,
                startedAt);
        }

        /// <summary>
        /// The same board and the same two facts put to milestone 8.23's capability, so the two answers
        /// can be compared rather than assumed equal.
        /// </summary>
        private static bool MovedAccordingToTheCapability(
            HexCoordinate actorAt, HexCoordinate? startedAt)
        {
            var board = Board(actorAt);
            var handler = SyntheticRosterCapabilities.Handlers()
                .Single(candidate => candidate.Reference.Equals(
                    SyntheticRosterCapabilities.ActorMovedThisActionV1));
            var action = AttackAction();
            var card = Card(action);

            var result = handler.Execute(new EffectCapabilityContext(
                new EffectNodeId("moved-branch-probe"),
                EffectFailurePolicy.Mandatory,
                Red,
                Red,
                card.Id,
                card.DefinitionReference,
                action.Id,
                new Dictionary<string, EffectValue>(),
                1,
                1,
                Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Attack,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                enclosingAttack: new EnclosingAttackFacts(
                    Red, Blue, default, AttackRangeKind.NonRanged, null, startedAt),
                declaredOutputs: new[]
                {
                    new EffectOutputBinding("moved", EffectValueKind.Boolean),
                    new EffectOutputBinding("didNotMove", EffectValueKind.Boolean)
                }));

            return result.Outcomes
                .Single(fact => fact.OutputName == "moved")
                .Value.BooleanValue;
        }

        private static MatchParticipants Participants() => StagedBoardParticipants.Create();

        private static EffectRosterState Roster() =>
            EffectRosterState.Of(
                Array.Empty<RoundTurnSlot>(),
                new RoundHeroDefeatHistory(Participants()),
                new HeroCoinLedger(Participants()));

        private static AdjacentMoveAuthority Board(HexCoordinate redAt) =>
            new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, Radius(3)),
                new[] { new HeroOccupancy(Red, redAt), new HeroOccupancy(Blue, TargetAt) },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

        private static IEnumerable<HexCoordinate> Radius(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius) yield return new HexCoordinate(q, r);
                }
            }
        }

        private static ActionDefinition AttackAction(
            RosterValueContributionDefinition? contribution = null) =>
            new ActionDefinition(
                new ActionDefinitionId("moved-branch-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 4) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged,
                default,
                rosterValueContributions: contribution.HasValue
                    ? new[] { contribution.Value }
                    : null);

        private static CardDefinition Definition(ActionDefinition action) =>
            new CardDefinition(
                new CardDefinitionId("moved-branch-card"),
                "rev-1",
                "TEST - moved branch",
                new InitiativeValue(8),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { action });

        private static CardInstance Card(ActionDefinition action) =>
            CardInstance.CreateInHand(
                new CardInstanceId("moved-branch-instance"), Definition(action).Reference, Red);

        private static ActionOffer Offer(CardInstance card, ActionDefinition action) =>
            new ActionOffer(
                card.Id,
                card.DefinitionReference,
                action.Id,
                Red,
                ActionInvocationContext.PrintedPrimary,
                true,
                AttackRangeKind.NonRanged,
                default);
    }
}
