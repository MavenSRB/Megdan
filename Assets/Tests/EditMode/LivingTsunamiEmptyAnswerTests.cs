using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.EffectResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z5. The queued corrective the 8.35l worker found while re-driving the
    /// seeds-12/20 report on the official six-player fixture: Arien's Living Tsunami - "Once per turn,
    /// before performing an Attack action, you may move 1 space" - projected BOTH a Confirm carrying
    /// an empty selection and a Decline for the one "no" answer, and the rules accepted the Decline
    /// while refusing the Confirm as <c>TargetUnavailable</c>. Seed 114 at budget 4,000 on
    /// <c>CreateOfficialSixPlayerSession</c> reaches this node and takes the offered Confirm, which the
    /// existing seed sweep never did - <c>AnswerRemainingText</c> in <see cref="ResumableContinuationTests"/>
    /// and <see cref="StagedHeroBoard.Decline"/>-first helpers all prefer Decline whenever it stands,
    /// which is exactly why nothing already checked in had noticed.
    ///
    /// <para><b>The printed reading that decides which side owns the truth.</b> "You may move 1 space"
    /// leaves the distance exact - p.11: an unqualified number is exact, and only the move itself is
    /// optional ("if the move itself is optional... you must move the exact number of spaces, or stop
    /// performing the action at that step"). So <c>direct-move@3</c>'s own floor of one destination is
    /// correct and untouched; the empty answer belongs to Decline alone, never to a Confirm the
    /// handler was never going to accept. <see cref="EffectResolutionSessionTests
    /// .MayNodeWhoseHandlerNeedsOne_OffersOnlyDeclineAsTheEmptyAnswer"/> proves the general seam this
    /// fixes; this one proves it on the reported card.</para>
    ///
    /// <para><b>The class, not the instance.</b> Grepping every <c>EffectNodeKind.Capability</c> node
    /// marked <c>EffectFailurePolicy.May</c> finds the identical shape wherever a handler needs more
    /// than nothing once it actually opens a choice: Tigerclaw's five "you may move 1 space" faces
    /// (Hit and Run step 7, Combat Reflexes steps 1 and 7, Leaping Strike steps 1 and 7, Sidestep's
    /// Defense rider - all <c>direct-move@3</c> again, through the same <c>OptionalStepNode</c>
    /// builder), his Evade's "you may retrieve" clause, and Sabina's and Brogan's own `may`-wrapped
    /// Retrieve and owned-card Discard (<c>retrieve-card@1</c> and <c>discard-owned-card@1</c>, both
    /// floor one once a candidate exists). None of those handlers special-cased an empty Resume either,
    /// so the fix at the projection seam - <c>EffectResolutionSession.ApplyCapabilityResult</c> and
    /// <c>PendingEffectChoice</c>'s new <c>declinableRegardlessOfMinimum</c> - repairs every one of them
    /// without a single content edit.</para>
    /// </summary>
    public sealed class LivingTsunamiEmptyAnswerTests
    {
        [Test]
        public void TsunamiMoveWithALegalDestination_OffersOnlyDeclineAsTheEmptyAnswerAndStillMoves()
        {
            var board = ArienBoard.Create(
                tier: 3,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent,
                    ["red-melee-02"] = StagedHeroBoard.SecondAdjacent
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", new HexCoordinate(2, 0)),
                banishedMinions: AllMinions.Where(minion =>
                    minion != "red-melee-01" && minion != "red-melee-02").ToArray());

            board.StartPrimary(ArienCardContent.ViolentTorrent);

            var choice = board.Choice;
            Assert.That(choice, Is.Not.Null,
                "Living Tsunami's before-action trigger stands before the Attack itself starts.");
            Assert.That(choice.NodeId.Value, Does.Contain("tsunami"), "and it is the Ultimate's own node.");
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget),
                "The move is one capability node carrying its own `may` policy, not a separate " +
                "confirm-or-decline gate in front of it.");
            Assert.That(choice.Candidates, Is.Not.Empty,
                "The board must actually offer a destination, or the case proves nothing about the " +
                "path a player would take instead of declining.");

            Assert.That(choice.DeclineInvocation, Is.Not.Null,
                "Declining the whole move must remain legal - this is the affordance milestone 8.20a " +
                "added and this fix must not take back.");
            Assert.That(choice.ConfirmInvocation, Is.Null,
                "Milestone 8.35z5: with nothing named, Confirm must NOT stand beside Decline - " +
                "before this fix it did, and a player taking it was refused as TargetUnavailable for " +
                "choosing nothing when choosing nothing was legal (just spelled differently, by Decline).");

            // Milestone 8.46b. What a NAMED destination does changed, and the change is the point of
            // the pairing this case draws. Until this milestone naming one made Confirm legal, and
            // the move happened on that second press; the printed move takes exactly one space, so
            // naming it is now the whole answer and the move happens in that command. Nothing about
            // the EMPTY answer moved - Decline is still the only way to give it, asserted above -
            // which is what this case has always been for.
            Assert.That(choice.ResolvesOnSelection, Is.True,
                "\"you may move 1 space\" admits exactly one destination.");

            var before = board.HeroSpace(board.Hero);
            board.SelectFirst();

            Assert.That(
                board.Choice == null || !board.Choice.ChoiceId.Equals(choice.ChoiceId), Is.True,
                "so naming one answers it outright rather than making a Confirm legal behind it.");
            Assert.That(board.HeroSpace(board.Hero), Is.Not.EqualTo(before),
                "and naming a destination still performs the move - the fix narrows the empty " +
                "answer, not the complete one.");
        }

        /// <summary>
        /// Milestone 9.6, found by the offer-against-outcome sweep (the seat walk on the official
        /// six-player fixture, seeds 5, 31 and 40 - the same family the 8.35l worker reported on seeds
        /// 12 and 20 and left unclaimed): a destination that walks Arien away from the only unit his
        /// Attack could take was offered, and naming it was REFUSED as TargetUnavailable with the move
        /// rolled back. RULING 45 offers every destination the move allows and makes the dead end
        /// p.10's stop, so the step is taken, the Attack ends with no target, and the card is spent.
        /// </summary>
        [Test]
        public void TsunamiStepAwayFromTheOnlyTarget_IsTaken_AndTheAttackEndsWithoutOne()
        {
            var board = ArienBoard.Create(
                tier: 3,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", new HexCoordinate(2, 0)),
                banishedMinions: AllMinions.Where(minion => minion != "red-melee-01").ToArray());

            board.StartPrimary(ArienCardContent.ViolentTorrent);

            var away = board.CandidateSpaces(board.Choice)
                .First(space => HexDistanceQuery.Calculate(space, StagedHeroBoard.FirstAdjacent) > 1);

            board.Select(away);

            Assert.That(board.HeroSpace(board.Hero), Is.EqualTo(away),
                "The step is kept: the dead end it leads to is p.10's stop, not a refusal.");
            Assert.That(board.Choice, Is.Null);
            Assert.That(board.Session.Projection.PendingAttackResolution, Is.Null,
                "\"Target a unit adjacent to you\" names nobody from there, so the Attack ends.");
            Assert.That(board.Session.Projection.Cards
                    .Single(card => card.OwnerId.Equals(board.Hero) &&
                        card.DefinitionReference.DefinitionId.Equals(ArienCardContent.ViolentTorrent))
                    .State.ResolutionStatus,
                Is.EqualTo(GuardsDigital.Core.Cards.CardResolutionStatus.Resolved),
                "and the card is spent.");
        }

        private static readonly string[] AllMinions =
        {
            "red-melee-01", "red-melee-02", "red-melee-03", "red-melee-04",
            "red-ranged-01", "red-heavy-01",
            "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
            "blue-ranged-01", "blue-heavy-01"
        };
    }
}
