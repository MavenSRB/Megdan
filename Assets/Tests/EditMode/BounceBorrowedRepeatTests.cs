using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.48. Bounce: "Move 2 spaces in a straight line, ignoring obstacles; if this card is
    /// already resolved as you perform this action, may repeat once." Reload!: "Perform the primary
    /// action of your rightmost resolved card."
    ///
    /// <para><b>Found at the table, in the 2026-09-18 playtest:</b> "Swift's gold didn't let me play
    /// her silver twice." The player resolved Bounce, then played Reload! and borrowed it - the one
    /// path Bounce's own sentence exists for - and the repeat never came. The gate asked
    /// <c>synthetic-source-card-in-discard@1</c> on the NARROW reading, which is true only of a card
    /// in the discard pile; a card resolved this round sits in the turn area, and Reload!'s pool names
    /// precisely such a card, so the clause could never hold through Reload!. Blizzard's "in your
    /// discard" met the same shape at milestone 8.37a and opted into the wide reading
    /// (<see cref="SyntheticConditionCapabilities.CountResolvedPlayedParameter"/>); Bounce now does the
    /// same, at its one call site.</para>
    ///
    /// <para><b>What stays as it was:</b> on Bounce's own turn the card is mid-resolution - played,
    /// revealed, not yet resolved - so the wide reading is still false and the repeat is still not
    /// offered, which is the printed reading the Play Mode journey already pins.</para>
    /// </summary>
    [TestFixture]
    public sealed class BounceBorrowedRepeatTests
    {
        [Test]
        public void ReloadsBorrowOfBounce_OffersBouncesRepeat_BecauseBounceIsAlreadyResolved()
        {
            var fixture = new SwiftTurn(borrowThroughReload: true);

            fixture.Drive();

            Assert.That(fixture.BorrowCandidates, Has.Member(fixture.BounceInstanceId),
                fixture.Trace() + " | Reload!'s pool is the rightmost resolved card, and Bounce is the " +
                "card resolved in the turn before.");
            Assert.That(fixture.RepeatOffered, Is.True,
                fixture.Trace() + " | Performed from Reload!, Bounce IS already resolved, so its " +
                "\"may repeat once\" has to be asked.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(2),
                fixture.Trace() + " | The borrowed performance and its one repeat: the silver played " +
                "twice, which is what the gold buys.");
        }

        [Test]
        public void BounceOnItsOwnTurn_StillOffersNoRepeat()
        {
            var fixture = new SwiftTurn(borrowThroughReload: false);

            fixture.Drive();

            Assert.That(fixture.RepeatOffered, Is.False,
                fixture.Trace() + " | On the turn Bounce itself resolves it is not yet resolved, " +
                "and the wide reading does not change that.");
            Assert.That(fixture.PerformedMoves, Is.EqualTo(1), fixture.Trace());
        }

        /// <summary>
        /// One of Swift's turns on a bare board. With <c>borrowThroughReload</c> the round's first slot
        /// holds Bounce, already resolved, and the turn being played is Reload!'s; without it the
        /// turn being played is Bounce's own.
        /// </summary>
        private sealed class SwiftTurn
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] instances;
            private readonly CardInstance bounce;
            private readonly CardInstance reload;
            private readonly CardInstance acting;
            private readonly bool borrowThroughReload;
            private readonly List<string> trace = new List<string>();
            private readonly List<CardInstanceId> borrowCandidates = new List<CardInstanceId>();
            private readonly TurnResolutionAuthority turn;

            public SwiftTurn(bool borrowThroughReload)
            {
                this.borrowThroughReload = borrowThroughReload;
                var configuration =
                    MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
                var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                var others = configuration.Participants.Heroes
                    .Select(hero => hero.Id)
                    .Where(id => !id.Equals(actor))
                    .ToArray();
                var bounceDefinition = SwiftCardContent.Definition(SwiftCardContent.Bounce);
                var reloadDefinition = SwiftCardContent.Definition(SwiftCardContent.Reload);
                bounce = CardInstance.CreateInHand(
                    new CardInstanceId("swift-bounce"), bounceDefinition.Reference, actor);
                reload = CardInstance.CreateInHand(
                    new CardInstanceId("swift-reload"), reloadDefinition.Reference, actor);
                acting = borrowThroughReload ? reload : bounce;

                definitions = configuration.CardDefinitions
                    .Concat(new[] { bounceDefinition, reloadDefinition })
                    .GroupBy(value => value.Reference).Select(group => group.First()).ToArray();
                instances = configuration.CardInstances.Concat(new[] { bounce, reload }).ToArray();

                var coordinates = new List<HexCoordinate>();
                for (var q = -5; q <= 5; q++)
                for (var r = -5; r <= 5; r++)
                {
                    if (Math.Abs(q + r) <= 5) coordinates.Add(new HexCoordinate(q, r));
                }

                var stands = new[] { new HexCoordinate(4, 0), new HexCoordinate(0, 4), new HexCoordinate(-4, 4) };
                var board = new AdjacentMoveAuthority(
                    coordinates,
                    new[] { new HeroOccupancy(actor, new HexCoordinate(0, 0)) }
                        .Concat(others.Select((id, index) => new HeroOccupancy(id, stands[index]))),
                    Enumerable.Empty<ObstacleOccupancy>(),
                    Enumerable.Empty<QuickGameMinionSetupState>());

                var cardActions = new CardActionResolutionAuthority(
                    new MatchContentCatalogs(
                        SyntheticEffectCapabilities.CreateCatalog(
                            SyntheticPlacementCapabilities.DeferredHandlers()
                                .Concat(SyntheticRosterCapabilities.DeferredHandlers())),
                        SyntheticComponentDefinitions.CreateCatalog(),
                        new TriggeredEffectCatalog(
                            SwiftEffectContent.TriggeredEffects(SwiftCardContent.ContentRevision))),
                    board);

                // The turn being played: the acting card and one card from each other hero, all
                // committed and revealed the way p.7 leaves them when the turn starts.
                var committed = new List<CardInstance> { acting };
                foreach (var owner in others)
                {
                    committed.Add(configuration.CardInstances.First(value =>
                        value.OwnerId.Equals(owner) && value.State.Location == CardLocation.Hand));
                }

                foreach (var card in committed)
                {
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                }

                var slots = new List<RoundTurnSlot>();
                var turnIndex = 1;
                if (borrowThroughReload)
                {
                    // The turn before: Bounce, played, revealed and RESOLVED - which p.6's turn area
                    // keeps until the end of the round. This is the card the table's player had just
                    // played when the gold refused to play it again.
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(bounce), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(bounce), Is.True);
                    Assert.That(CardLifecycleAuthority.TryResolveAfterAction(bounce), Is.True);
                    Assert.That(bounce.State.Location, Is.EqualTo(CardLocation.TurnArea),
                        "A card resolved this round is in the turn area, not the discard pile.");
                    // A slot names one card per configured hero, so the others resolved one too.
                    var earlier = new List<TurnCardAssignment>
                    {
                        new TurnCardAssignment(actor, bounce.Id, false)
                    };
                    foreach (var owner in others)
                    {
                        var card = configuration.CardInstances.First(value =>
                            value.OwnerId.Equals(owner) && value.State.Location == CardLocation.Hand);
                        Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                        Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                        Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
                        earlier.Add(new TurnCardAssignment(owner, card.Id, false));
                    }

                    slots.Add(new RoundTurnSlot(1, 1, earlier, instances, configuration.Participants));
                    turnIndex = 2;
                }

                slots.Add(new RoundTurnSlot(
                    1, turnIndex,
                    committed.Select(card => new TurnCardAssignment(card.OwnerId, card.Id, false)),
                    instances,
                    configuration.Participants));

                typeof(CardActionResolutionAuthority)
                    .GetMethod("BindRosterState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(cardActions, new object[]
                    {
                        EffectRosterState.Of(
                            slots,
                            new RoundHeroDefeatHistory(configuration.Participants),
                            new HeroCoinLedger(configuration.Participants))
                    });

                turn = new TurnResolutionAuthority(
                    definitions, instances, configuration.Participants,
                    new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(),
                    1, turnIndex);
                Assert.That(turn.TryStart().Accepted, Is.True);
            }

            public CardInstanceId BounceInstanceId => bounce.Id;

            public bool RepeatOffered { get; private set; }

            public int PerformedMoves { get; private set; }

            public IReadOnlyList<CardInstanceId> BorrowCandidates => borrowCandidates;

            public string Trace() => string.Join(",", trace);

            public void Drive()
            {
                var started = false;
                for (var step = 0; step < 80 && !turn.IsEnded; step++)
                {
                    if (!started && turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        trace.Add("tie");
                        Assert.That(turn.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(acting.Id)
                                ? acting.Id
                                : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }

                    if (!started && turn.PendingTurnRelease != null)
                    {
                        trace.Add("release");
                        Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (turn.PendingEffectChoice != null)
                    {
                        var effect = turn.PendingEffectChoice;
                        trace.Add("choice:" + effect.NodeId.Value);
                        if (effect.NodeId.Value == "reload-borrow")
                        {
                            borrowCandidates.AddRange(effect.Candidates
                                .Where(candidate =>
                                    candidate.Value.Kind == EffectValueKind.CardInstance)
                                .Select(candidate => candidate.Value.CardInstanceId));
                        }

                        if (effect.NodeId.Value == "bounce-repeat")
                        {
                            RepeatOffered = true;
                        }

                        // One line choice is visited twice - once to select the destination and once
                        // to confirm it - so the performance is counted at its first visit.
                        if (effect.NodeId.Value == "bounce-line" &&
                            effect.SelectedCandidates.Count == 0)
                        {
                            PerformedMoves++;
                        }

                        // Reload!'s modal is answered on its borrow arm, never its defeat arm; every
                        // other question takes its first candidate, or its confirm when it has none -
                        // which is what accepts the repeat when it is asked.
                        var borrowArm = effect.SelectInvocations.FirstOrDefault(invocation =>
                            invocation.Candidate.OptionNodeId.IsDefined &&
                            invocation.Candidate.OptionNodeId.Value == "reload-borrow-arm");
                        var answer = borrowArm ??
                            (effect.SelectInvocations.Count > 0
                                ? effect.SelectInvocations[0]
                                : effect.ConfirmInvocation);
                        Assert.That(turn.TryAnswerEffectChoice(answer).Accepted, Is.True,
                            Trace() + " | " + effect.NodeId.Value + " refused its answer.");
                        continue;
                    }

                    if (!started && turn.SelectedActor.CardInstanceId.IsDefined)
                    {
                        if (!turn.SelectedActor.CardInstanceId.Equals(acting.Id))
                        {
                            trace.Add("hold");
                            Assert.That(turn.TryPerformHold(
                                turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                            continue;
                        }

                        trace.Add("start");
                        var offer = ActionOfferQuery
                            .GetAvailableTurnActionOffers(
                                definitions, instances, acting.OwnerId, acting.Id)
                            .Single(value => value.ActionDefinitionId.Equals(
                                SwiftCardContent.PrimaryBasicSkill));
                        Assert.That(turn.TryStartSkill(
                            new ActionResolutionId("swift-own"), offer).Accepted, Is.True);
                        started = true;
                        continue;
                    }

                    trace.Add("break:ended=" + turn.IsEnded);
                    break;
                }
            }
        }
    }
}
