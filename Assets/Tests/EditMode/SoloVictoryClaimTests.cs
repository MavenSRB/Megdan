using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39g1. The seam a printed card claims milestone 8.39g's solo victory through -
    /// Cutter's A Fistful of Coins, "You gain 3 coins. If you have 13 or more coins, you alone win the
    /// game."
    ///
    /// <para><b>Why this block exists at all.</b> 8.39g built the ending and its non-goal read "the
    /// face that reaches this is composed at 8.39h", as though composing the face would connect the
    /// two. It does not: <c>RoundResolutionAuthority.TryRecordSoloVictory</c> is an instance method on
    /// the round, and a capability handler is handed the roster state, the board, the participants and
    /// its own frame - no path from any of them reaches the round. The probe 8.39h ordered before
    /// composing is what found it.</para>
    ///
    /// <para><b>And the boundary was blind to the fact 8.39g added.</b>
    /// <c>AnUnrecordedEndingExists</c> called <c>MatchOutcomeQuery.Read</c>'s four-argument overload,
    /// with no <c>soloVictoryHeroId</c> - <c>ReadOutcome</c> passed the field and this did not - so a
    /// solo victory recorded mid-turn was invisible to the one seam that notices every other mid-turn
    /// ending. <see cref="AClaimedVictoryEndsTheMatchAtTheNextTurnCommandBoundary"/> is that defect,
    /// observed failing before it was fixed.</para>
    /// </summary>
    [TestFixture]
    public sealed class SoloVictoryClaimTests
    {
        private const int PrintedThreshold = 13;

        private static readonly CardDefinitionId ProbeCard = new CardDefinitionId("probe-hoard-card");
        private static readonly CardInstanceId ProbeInstance =
            new CardInstanceId("probe-hoard-instance");
        private static readonly ActionDefinitionId ProbeAction =
            new ActionDefinitionId("probe-hoard-action");

        private static readonly TeamId RedTeam = new TeamId("hoard-red");
        private static readonly TeamId BlueTeam = new TeamId("hoard-blue");
        private static readonly HeroId Claimant = new HeroId("hoard-claimant");
        private static readonly HeroId TeamMate = new HeroId("hoard-team-mate");
        private static readonly HeroId Enemy = new HeroId("hoard-enemy");

        // -------------------------------------------------------------------------------------------
        // Where the declaration sits
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate, both halves - 8.39b's position exactly. It is a NEW handler type and
        /// no dealt card starts it until milestone 8.39i deals Cutter, so it is deferred rather than
        /// registered: a capability registered before a dealt card can start it moves pinned bytes.
        /// </summary>
        [Test]
        public void TheClaim_IsBuiltAndDeferredRatherThanRegistered()
        {
            Assert.That(
                SyntheticRosterCapabilities.DeferredHandlers()
                    .Select(handler => handler.Reference),
                Does.Contain(SyntheticRosterCapabilities.ClaimSoloVictoryV1),
                "A Fistful of Coins is the only face in the corpus that claims an ending, and she is " +
                "not dealt yet.");
            Assert.That(
                SyntheticRosterCapabilities.RegisteredHandlers()
                    .Any(handler => handler.Reference.Equals(
                        SyntheticRosterCapabilities.ClaimSoloVictoryV1)),
                Is.False,
                "and registering it before then would move pinned bytes.");
        }

        // -------------------------------------------------------------------------------------------
        // "If you have 13 or more coins"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The printed arithmetic, asserted at the boundary and on both sides of it. Twelve is not
        /// thirteen; "13 or more" is what the card prints, so fourteen claims as readily as thirteen.
        /// </summary>
        [TestCase(12, false)]
        [TestCase(PrintedThreshold, true)]
        [TestCase(14, true)]
        public void TheClaimIsTakenAtThePrintedThresholdAndNotBelowIt(int held, bool expected)
        {
            var claim = new RecordingClaim();
            var result = Execute(claim, held);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "The clause is reached whatever the balance - a hero holding twelve has resolved the " +
                "whole face correctly, which is what a condition on a clause means.");
            Assert.That(Claimed(result), Is.EqualTo(expected));
            Assert.That(claim.Claimed, Is.EqualTo(expected ? Claimant : default(HeroId)));
        }

        /// <summary>
        /// The count is read from the live ledger at the moment the branch resolves, which is milestone
        /// 8.39g's own requirement of it: a balance that reached thirteen and was spent back down at
        /// p.15's Level Up is twelve when the branch asks, and twelve does not win.
        /// </summary>
        [Test]
        public void TheCountIsReadFromTheLedgerAtTheBranch()
        {
            var claim = new RecordingClaim();
            var ledger = Ledger(PrintedThreshold + 2);
            Assert.That(ledger.TrySpend(Claimant, 4, CoinSpendKind.LevelUp, out _), Is.True);

            Assert.That(Claimed(Execute(claim, ledger)), Is.False,
                "Fifteen coins spent down to eleven is eleven when the card asks.");
            Assert.That(claim.Claimed.IsDefined, Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // Who wins
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "You alone win the game" - "you" on a card is the hero whose card it is. A borrowed or
        /// granted performance is still that hero winning, and reading the actor would hand the match
        /// to whoever happened to be holding the frame.
        /// </summary>
        [Test]
        public void TheWinnerIsTheControllerAndNotTheActor()
        {
            var claim = new RecordingClaim();
            var result = Execute(claim, Ledger(PrintedThreshold), actorId: Enemy);

            Assert.That(Claimed(result), Is.True);
            Assert.That(claim.Claimed, Is.EqualTo(Claimant),
                "The controller owns the card, so the controller is the one the sentence names.");
            Assert.That(
                result.Outcomes.Single(fact => fact.OutputName == "winner").Value.HeroId,
                Is.EqualTo(Claimant));
        }

        // -------------------------------------------------------------------------------------------
        // What it refuses
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A walk judging whether a branch can be taken must not TAKE it, or merely offering the card
        /// would end the match. <c>Anticipate</c> asks whether the question is answerable and touches
        /// nothing.
        /// </summary>
        [Test]
        public void AWalkJudgingTheBranchDoesNotTakeIt()
        {
            var claim = new RecordingClaim();

            Assert.That(
                Handler().Anticipate(Context(claim, Ledger(PrintedThreshold))),
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(claim.Claimed.IsDefined, Is.False,
                "Judging the option is not taking it.");
            Assert.That(claim.Attempts, Is.Zero);
        }

        /// <summary>
        /// A composition with no round to claim through - a fixture that never supplied one - reports
        /// itself infeasible rather than applying and quietly winning nothing.
        /// </summary>
        [Test]
        public void ACompositionWithNoRoundToClaimThrough_ReportsAnImpossibleStep()
        {
            var context = Context(soloVictory: null, ledger: Ledger(PrintedThreshold));

            Assert.That(
                Handler().Execute(context).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(
                Handler().Anticipate(context),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        /// <summary>
        /// Two endings cannot both be the ending. The second claim is refused on the authority's own
        /// terms, and the node that made it reports that it claimed nothing rather than throwing at a
        /// card that did nothing wrong.
        /// </summary>
        [Test]
        public void ASecondClaimInTheSameMatch_IsRefusedRatherThanLayered()
        {
            var fixture = new BoundaryFixture();
            var first = fixture.Heroes[0];
            var second = fixture.Heroes[1];

            Assert.That(fixture.Round.TryClaim(first), Is.True);

            Assert.That(fixture.Round.TryClaim(second), Is.False,
                "The match is already claimed, and the first claim stands.");
            Assert.That(fixture.Round.SoloVictoryHeroId, Is.EqualTo(first));
            Assert.That(fixture.Round.HasBeenClaimed, Is.True);

            Assert.That(Claimed(Execute(fixture.Round, Ledger(PrintedThreshold))), Is.False,
                "and a node claiming into an already-claimed match publishes that it took nothing.");
        }

        /// <summary>
        /// A hero this match is not playing cannot win it - the same refusal
        /// <c>MatchOutcomeQuery</c> already makes of a payload claiming one, made here before the fact
        /// is ever written.
        /// </summary>
        [Test]
        public void AClaimByAHeroTheMatchIsNotPlaying_IsRefused()
        {
            var fixture = new BoundaryFixture();

            Assert.That(fixture.Round.TryClaim(new HeroId("hoard-nobody")), Is.False);
            Assert.That(fixture.Round.TryClaim(default), Is.False);
            Assert.That(fixture.Round.HasBeenClaimed, Is.False);
            Assert.That(fixture.Round.TryClaim(fixture.Heroes[0]), Is.True,
                "while a hero it IS playing claims it, so the refusal above is about the roster " +
                "rather than about the claim being closed.");
        }

        // -------------------------------------------------------------------------------------------
        // The boundary - this block's pre-fix observation
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE DEFECT THIS BLOCK FOUND, STATED AS THE BEHAVIOUR IT BREAKS. A claim records the winner
        /// and deliberately does not end the match - ending it from inside a live effect frame would
        /// call <c>EndGame</c>, which stops the resolution still running. The match is stopped instead
        /// by <c>ExecuteTurnCommand</c>'s post-command check, which milestone 8.16 widened to notice
        /// "every ending the state can already describe".
        ///
        /// <para>It did not notice this one. That check called <c>MatchOutcomeQuery.Read</c> without
        /// the solo winner, so before the fix this case fails with the phase still resolving a turn -
        /// a match won by a hero and still asking them for their next command.</para>
        /// </summary>
        [Test]
        public void AClaimedVictoryEndsTheMatchAtTheNextTurnCommandBoundary()
        {
            var fixture = new BoundaryFixture();
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn),
                "The premise: a turn is under way and the match has not ended.");

            Assert.That(fixture.Round.TryClaim(fixture.ActingHeroId), Is.True);
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.ResolvingTurn),
                "The claim writes the winner and does NOT end the match, which is what keeps " +
                "EndGame out of a running resolution.");

            var held = fixture.Round.TryPerformHold(fixture.ActingCardInstanceId);

            Assert.That(held.Accepted, Is.True);
            Assert.That(fixture.Round.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                "and the boundary that notices every other mid-turn ending notices this one.");
            Assert.That(fixture.Round.Outcome.HasSoloWinner, Is.True);
            Assert.That(fixture.Round.Outcome.WinningHeroId, Is.EqualTo(fixture.ActingHeroId));
            Assert.That(
                fixture.Round.Outcome.Cause, Is.EqualTo(MatchVictoryCause.CoinHoardClaimed));
        }

        /// <summary>
        /// The three team endings are untouched by the widening, asserted rather than assumed: a round
        /// nobody has claimed still ends exactly when it did and not one command sooner.
        /// </summary>
        [Test]
        public void AnUnclaimedRound_IsUnaffectedByTheWidenedBoundary()
        {
            var fixture = new BoundaryFixture();

            Assert.That(fixture.Round.TryPerformHold(fixture.ActingCardInstanceId).Accepted, Is.True);
            Assert.That(fixture.Round.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded));
            Assert.That(fixture.Round.Outcome.HasEnded, Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler() =>
            SyntheticRosterCapabilities.DeferredHandlers()
                .Single(handler =>
                    handler.Reference.Equals(SyntheticRosterCapabilities.ClaimSoloVictoryV1));

        private static EffectExecutionResult Execute(ISoloVictoryClaim claim, int held) =>
            Execute(claim, Ledger(held));

        private static EffectExecutionResult Execute(
            ISoloVictoryClaim claim, HeroCoinLedger ledger, HeroId actorId = default) =>
            Handler().Execute(Context(claim, ledger, actorId));

        private static bool Claimed(EffectExecutionResult result) =>
            result.Kind == EffectExecutionResultKind.Applied &&
            result.Outcomes.Single(fact =>
                    fact.OutputName == SyntheticRosterCapabilities.SoloVictoryClaimedOutput)
                .Value.BooleanValue;

        private static EffectCapabilityContext Context(
            ISoloVictoryClaim soloVictory, HeroCoinLedger ledger, HeroId actorId = default) =>
            new EffectCapabilityContext(
                new EffectNodeId("probe-hoard-node"),
                EffectFailurePolicy.Mandatory,
                actorId.IsDefined ? actorId : Claimant,
                Claimant,
                ProbeInstance,
                new CardDefinitionReference(ProbeCard, "probe-rev-1"),
                ProbeAction,
                new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.SoloVictoryThresholdParameter] =
                        EffectValue.Integer(PrintedThreshold)
                },
                1,
                1,
                Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                rosterState: EffectRosterState.Of(
                    Array.Empty<RoundTurnSlot>(),
                    new RoundHeroDefeatHistory(Participants()),
                    ledger,
                    soloVictory),
                declaredOutputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticRosterCapabilities.SoloVictoryClaimedOutput,
                        EffectValueKind.Boolean),
                    new EffectOutputBinding("winner", EffectValueKind.Hero)
                });

        private static HeroCoinLedger Ledger(int claimantCoins)
        {
            var ledger = new HeroCoinLedger(Participants());
            if (claimantCoins > 0)
            {
                Assert.That(
                    ledger.TryGrant(Claimant, claimantCoins, CoinGrantKind.CardEffect, out _),
                    Is.True);
            }

            return ledger;
        }

        /// <summary>A 2v2, so the claimant has a team-mate the printed sentence never mentions.</summary>
        private static MatchParticipants Participants()
        {
            var players = new List<Player>();
            var heroes = new List<Hero>();
            foreach (var pair in new[]
            {
                (Hero: Claimant, Team: RedTeam),
                (Hero: TeamMate, Team: RedTeam),
                (Hero: Enemy, Team: BlueTeam)
            })
            {
                var player = new PlayerId(pair.Hero.Value + "-player");
                players.Add(new Player(player, pair.Team));
                heroes.Add(new Hero(pair.Hero, player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                players,
                heroes);
        }

        /// <summary>
        /// A stand-in for the round, so the capability's own cases can assert what it asked for without
        /// composing a match - which is the whole reason the claim is an interface.
        /// </summary>
        private sealed class RecordingClaim : ISoloVictoryClaim
        {
            public HeroId Claimed { get; private set; }

            public int Attempts { get; private set; }

            public bool HasBeenClaimed => Claimed.IsDefined;

            public bool TryClaim(HeroId winningHeroId)
            {
                Attempts++;
                if (HasBeenClaimed || !winningHeroId.IsDefined)
                {
                    return false;
                }

                Claimed = winningHeroId;
                return true;
            }
        }

        /// <summary>
        /// The smallest composition with a turn actually under way, so a turn command has a boundary to
        /// cross. Both heroes commit, which is what starts the round's first turn.
        /// </summary>
        private sealed class BoundaryFixture
        {
            public BoundaryFixture()
            {
                var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                Round = new RoundResolutionAuthority(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    new TieBreakerState(TeamSide.Red),
                    1,
                    RoundResolutionComposition.ForCardActionsOnly(null));

                foreach (var hero in configuration.Participants.Heroes)
                {
                    var card = configuration.CardInstances.First(value =>
                        value.OwnerId.Equals(hero.Id) &&
                        value.State.Location == CardLocation.Hand);
                    Assert.That(Round.TryCommitCard(hero.Id, card.Id).Accepted, Is.True);
                }

                Assert.That(Round.CurrentTurnAuthority, Is.Not.Null);
                ActingCardInstanceId = Round.CurrentTurnAuthority.SelectedActor.CardInstanceId;
                ActingHeroId = configuration.CardInstances
                    .Single(value => value.Id.Equals(ActingCardInstanceId))
                    .OwnerId;
                Heroes = configuration.Participants.Heroes.Select(hero => hero.Id).ToArray();
            }

            public RoundResolutionAuthority Round { get; }

            public CardInstanceId ActingCardInstanceId { get; }

            public HeroId ActingHeroId { get; }

            /// <summary>The heroes this match is actually playing, which is what a claim is checked against.</summary>
            public IReadOnlyList<HeroId> Heroes { get; }
        }
    }
}
