using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z24. Rulebook p.14 names its target at step 2 and never names it
    /// again, so what step 3's Defense response needs of that target is that it is still STANDING -
    /// not that the attacker can still reach it.
    ///
    /// <para><b>The report.</b> The seat projection offered a Defense card the rules then refused:
    /// <see cref="MatchSessionProjection.PendingDefensePreview"/> listed it, the seat's
    /// <see cref="PendingDecisionContract"/> carried the command, the preview scored it
    /// <see cref="AttackResolutionOutcome.SuccessfullyDefended"/>, and playing it came back
    /// <c>AttackCommandRejected: InvalidChoice</c>.</para>
    ///
    /// <para><b>What it actually was, which is worse than the report.</b> The guard that refused sits
    /// above BOTH of p.14's answers, so it refused the whole pause: every scored Defense card AND the
    /// Decline beside them. A pause no answer can leave is a dead end rather than an infeasible
    /// offer - which is why a deterministic practice bot met it as a livelock and re-submitted the
    /// same refused answer 11,000 times, and why a human meets it as a match that cannot continue.
    /// <see cref="EveryAnswerTheDeadEndPauseRefused_IsNowAccepted"/> is that reading.</para>
    ///
    /// <para><b>The rule, and it was already written down.</b> Milestone 8.23 hit the same thing one
    /// step later, at p.14's step 6, when a Defense text moved the DEFENDER out of contact: it
    /// narrowed the outcome resolver's prevalidation from "still reachable" to "still standing" and
    /// recorded the reasoning on <see cref="AttackDefenseCardActionStrategyBackend.ResolveOutcome"/>
    /// at length. Two sibling sites re-derive the same question and neither was moved onto it - the
    /// step-3 answer path and the snapshot restore cross-check. That is milestone 8.35z15's unwired
    /// probe exactly: a rule recorded once and implemented by one of its callers.</para>
    ///
    /// <para><b>Why this needs no defender card text at all.</b> The step-6 case needed one. This one
    /// is reached by the ATTACKER's own printed body: p.14 applies "Before the attack:" text AFTER
    /// the target is named (see <c>ContinueAfterBeforeAttackText</c>, which revalidates a target the
    /// text BOUND and leaves an ordinarily-chosen one alone), so a face printing "Before the attack:
    /// You may move 1 space. Target a unit adjacent to you." can legally step out of contact between
    /// naming its target and the defender answering.
    /// <see cref="EveryFaceThatCanLeaveContactMidAttack_IsAccountedFor"/> is the roster table of the
    /// faces that reach it.</para>
    ///
    /// <para><b>Nothing is greyed, deliberately.</b> <c>PLAN.md</c> RULING 17 greys only an option
    /// that CANNOT be performed. Under the rule above every one of these answers can be performed, so
    /// the offer set was right all along and the enforcement was wrong; reconciling the other way -
    /// withholding what this guard refused - would have withheld the Decline too and left the pause
    /// with no answer at all.</para>
    /// </summary>
    [TestFixture]
    public class DefenseOfferFeasibilityTests
    {
        /// <summary>
        /// The staged position, which is p.14 stopped at its Defense response with the attacker no
        /// longer beside his target.
        ///
        /// <para>Nothing here is hand-placed into an engine state: Tigerclaw commits Combat Reflexes
        /// and plays it, p.14 opens its target question, he names the one enemy hero adjacent to him,
        /// and his own printed "Before the attack:" body then offers him the move that breaks the
        /// contact. <see cref="AwayFromTheTarget"/> is the only destination the case chooses, and it
        /// is chosen out of the candidates the node itself published.</para>
        /// </summary>
        private static StagedHeroBoard AnAttackerWhoSteppedOutOfContact(
            CardDefinitionId face, int tier)
        {
            var board = TigerclawBoard.Create(
                tier: tier,
                heroPlacements: StagedHeroBoard.At(EnemyHeroId, StagedHeroBoard.ThirdAdjacent));

            Assert.That(
                Distance(board.HeroSpace(board.Hero), board.HeroSpace(board.Enemy)), Is.EqualTo(1),
                "The Attack is non-Ranged, so the case starts with the two heroes in contact.");

            board.StartPrimary(face);
            var target = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null, "An Attack opens p.14's target question first.");
            Assert.That(target.LegalTargetHeroIds, Has.Member(board.Enemy));
            board.Accept(DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, board.Enemy));

            var move = board.Session.Projection.PendingEffectChoice;
            Assert.That(move, Is.Not.Null,
                "\"Before the attack: You may move 1 space\" is applied after the target is named, " +
                "which is the whole reason the attacker can leave contact inside his own Attack.");
            Assert.That(
                board.CandidateSpaces(move), Has.Member(AwayFromTheTarget),
                "The node has to be offering the space that breaks contact, or the case proves " +
                "something about reachability instead.");
            board.Select(AwayFromTheTarget);
            board.Confirm();

            Assert.That(
                Distance(board.HeroSpace(board.Hero), board.HeroSpace(board.Enemy)), Is.EqualTo(2),
                "The attacker has legally stepped out of contact with the target he already named.");
            Assert.That(board.Session.Projection.PendingDefensePreview, Is.Not.Null,
                "and p.14 is standing at its Defense response.");
            return board;
        }

        /// <summary>
        /// THE DEFECT, stated as the whole pause rather than as the one card that was reported.
        ///
        /// <para>Every answer the pause published is submitted into its OWN copy of the position, so
        /// this is five independent commands rather than one walk that stops at the first. Before the
        /// fix all five came back <c>AttackCommandRejected: InvalidChoice</c> - the two Defense cards
        /// the preview scored as a successful defence, the two it scored as an unsuccessful one, and
        /// the Decline that names no card at all.</para>
        /// </summary>
        [Test]
        public void EveryAnswerTheDeadEndPauseRefused_IsNowAccepted()
        {
            var answers = new List<string>();
            var probe = AnAttackerWhoSteppedOutOfContact(TigerclawCardContent.CombatReflexes, 2);
            var offered = probe.Session.Projection.PendingDefensePreview.Choices
                .Select(choice => choice.IsDecline ? null : (CardInstanceId?)choice.CardInstanceId.Value)
                .ToArray();

            Assert.That(offered.Length, Is.GreaterThanOrEqualTo(3),
                "The defending hand has to be offering several cards and a Decline for this to be " +
                "a statement about the pause rather than about one card.");
            Assert.That(offered.Any(card => card == null), Is.True, "p.14 always allows a Decline.");

            // The report named two publishers and both have to be saying it, or the defect is one
            // renderer's rather than the projection's: the preview a player reads AND the exact
            // commands the seat is authorised to send.
            var contract = probe.Session.Projection.DecisionContract;
            Assert.That(contract, Is.Not.Null, "A standing pause publishes its command contract.");
            Assert.That(
                contract.Commands.Count(command =>
                    command.Kind == DirectorCommandKind.ChooseDefenseCard ||
                    command.Kind == DirectorCommandKind.DeclineDefense),
                Is.EqualTo(offered.Length),
                "The contract publishes exactly the answers the preview scored, so the offer this " +
                "case submits is the offer a seat would actually send.");

            foreach (var card in offered)
            {
                var board = AnAttackerWhoSteppedOutOfContact(TigerclawCardContent.CombatReflexes, 2);
                var result = board.Session.Execute(card.HasValue
                    ? DirectorCommand.ChooseDefenseCard(card.Value)
                    : DirectorCommand.DeclineDefense());
                answers.Add((card?.Value ?? "decline") + "=" + result.Accepted +
                    (result.Accepted ? string.Empty : " (" + result.Detail + ")"));
            }

            Assert.That(
                answers.All(answer => answer.EndsWith("=True")), Is.True,
                "Rulebook p.14 offered these and p.14 has to take them: " +
                string.Join(", ", answers));
        }

        /// <summary>
        /// The offer and the rules agree about the OUTCOME too, not merely about acceptance. A
        /// preview that says <see cref="AttackResolutionOutcome.SuccessfullyDefended"/> and an engine
        /// that then defeats the defender would be the same defect wearing a different result.
        /// </summary>
        [Test]
        public void ThePreviewsScoredOutcome_IsTheOutcomeTheRulesProduce()
        {
            var board = AnAttackerWhoSteppedOutOfContact(TigerclawCardContent.CombatReflexes, 2);
            var survives = board.Session.Projection.PendingDefensePreview.Choices.First(choice =>
                !choice.IsDecline &&
                choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended);

            board.Accept(DirectorCommand.ChooseDefenseCard(survives.CardInstanceId.Value));
            board.AnswerEveryStandingChoice();

            Assert.That(board.IsOnBoard(board.Enemy), Is.True,
                "The preview scored this answer as a successful defence, so the defender stands.");
            Assert.That(
                board.RuleEventTypes(), Has.Member("Action.AttackSuccessfullyDefended"),
                "and p.14's own step 6 has to have said so.");
        }

        /// <summary>
        /// Declining is the answer that proves the guard was above BOTH arms: it names no card at
        /// all, so no card-feasibility reading of the defect can explain its refusal.
        /// </summary>
        [Test]
        public void DecliningTheSamePause_IsAccepted_AndDefeatsTheDefender()
        {
            var board = AnAttackerWhoSteppedOutOfContact(TigerclawCardContent.CombatReflexes, 2);
            board.Accept(DirectorCommand.DeclineDefense());
            board.AnswerEveryStandingChoice();

            Assert.That(board.RuleEventTypes(), Has.Member("Action.DefenseDeclined"));
            Assert.That(board.RuleEventTypes(), Has.Member("Action.AttackSuccessful"));
            Assert.That(board.IsOnBoard(board.Enemy), Is.False,
                "Nothing answered the Attack Total, so p.13 removes the defeated hero.");
        }

        /// <summary>
        /// THE SECOND UNWIRED SITE. A capture taken at this pause is a payload of a legitimate
        /// position, and the restore cross-check asked it the same "still reachable" question - so
        /// the session that wrote the payload could not read it back. This is the restore door onto
        /// milestone 8.23's finding, and it bears directly on 8.35f's Undo and 8.35i's restore.
        /// </summary>
        [Test]
        public void ThePauseSurvivesASnapshotRoundTrip()
        {
            var board = AnAttackerWhoSteppedOutOfContact(TigerclawCardContent.CombatReflexes, 2);
            board.RoundTripThroughASnapshot();

            var survives = board.Session.Projection.PendingDefensePreview.Choices.First(choice =>
                !choice.IsDecline &&
                choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended);
            Assert.That(
                board.Session.Execute(
                    DirectorCommand.ChooseDefenseCard(survives.CardInstanceId.Value)).Accepted,
                Is.True,
                "and the restored pause is answerable, not merely readable.");
        }

        /// <summary>
        /// The same dead end reached through the OTHER face that prints the clause, so the fix is
        /// about the page rather than about Combat Reflexes. Leaping Strike is Tier III and prints
        /// the identical step-1 body with an unconditional step 7.
        /// </summary>
        [Test]
        public void LeapingStrikeReachesTheSamePause_AndItIsAnswerableToo()
        {
            var board = AnAttackerWhoSteppedOutOfContact(TigerclawCardContent.LeapingStrike, 3);
            var survives = board.Session.Projection.PendingDefensePreview.Choices.First(choice =>
                !choice.IsDecline &&
                choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended);

            Assert.That(
                board.Session.Execute(
                    DirectorCommand.ChooseDefenseCard(survives.CardInstanceId.Value)).Accepted,
                Is.True);
        }

        /// <summary>
        /// THE CLASS, derived from the content rather than listed by hand - milestone 8.35z15's
        /// two-direction discipline. Every dealt face that prints a body at p.14's
        /// <see cref="CardTextTiming.BeforeTheAttack"/> moment can move its attacker between naming a
        /// target and the defender answering, so every one of them can reach the pause above.
        ///
        /// <para>Two columns decide the family and both are read off the composed nodes: whether the
        /// body can relocate the ATTACKER (see <see cref="MovesTheAttacker"/>, and the measurement
        /// that corrected a guess is recorded there), and whether the text binds the target itself
        /// afterwards. Only <c>moves|keeps</c> can open p.14's step 3 out of contact.</para>
        ///
        /// <para>A face that arrives printing that clause and is NOT in this table is a face nobody
        /// has driven through step 3 after its body ran.</para>
        /// </summary>
        [Test]
        public void EveryFaceThatCanLeaveContactMidAttack_IsAccountedFor()
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var rows = configuration.CardDefinitions
                .SelectMany(definition => definition.ActionDefinitions
                    .Where(action =>
                        action.Kind == ActionKind.Attack &&
                        action.CardTextTiming == CardTextTiming.BeforeTheAttack)
                    .Select(action => definition.Reference.DefinitionId.Value +
                        (MovesTheAttacker(action) ? "|moves" : "|stays") +
                        ((action.AttackTargetPolicy?.TargetIsBoundByCardText ?? false)
                            ? "|rebinds" : "|keeps")))
                .Distinct()
                // Grouped by what decides the family, then by name inside the group, so a new face
                // lands beside the rows it has to be read against rather than wherever its id sorts.
                .OrderBy(row => row.Contains("|stays|") ? 0 : row.EndsWith("|rebinds") ? 1 : 2)
                .ThenBy(row => row, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(rows, Is.EqualTo(new[]
            {
                // `stays`: the body does not relocate the ATTACKER, so he cannot leave contact
                // inside it. Arien's three Greens discard and defeat; his Noble Blade moves a third
                // piece and says so ("another unit" never includes you); Brogan's two throws bind a
                // ranged target out of a discard without the attacker stepping anywhere.
                "arien-dangerous-current|stays|keeps",
                "arien-noble-blade|stays|keeps",
                "arien-raging-stream|stays|keeps",
                "arien-violent-torrent|stays|keeps",
                "brogan-throwing-axe|stays|rebinds",
                "brogan-throwing-spear|stays|rebinds",

                // `moves|rebinds`: the text moves the attacker AND names the target afterwards, so
                // ContinueAfterBeforeAttackText revalidates the target against the post-move board
                // and an unreachable one is never bound in the first place. Safe by construction -
                // Brogan's three charges are milestone 8.35p's, and they move before they ask.
                "brogan-bullrush|moves|rebinds",
                "brogan-furious-charge|moves|rebinds",
                "brogan-mad-dash|moves|rebinds",
                "tigerclaw-blink-strike|moves|rebinds",

                // `moves|keeps`: THE EXPOSED FAMILY. p.14 names the target the ordinary way at step
                // 2 and the printed body then moves the attacker away from it, so step 3 opens with
                // the two out of contact. Both are driven above.
                "tigerclaw-combat-reflexes|moves|keeps",
                "tigerclaw-leaping-strike|moves|keeps"
            }),
                "A face printing \"Before the attack:\" text that this table does not name has never " +
                "been driven through p.14's step 3 after its body ran. Add the row; and if it is a " +
                "`moves|keeps` row, drive its out-of-contact Defense pause before shipping the face.");
        }

        /// <summary>
        /// THE REPORTED REPROS, replayed. The defect was found by self-play rather than by a fixture:
        /// three seeded matches on the engaged six-player bench with the greedy brain on both sides,
        /// each of which offered a Defense card and was refused. <c>BotSelfPlay</c> captures such a
        /// refusal instead of throwing, so the assertion is that its channel stays empty.
        ///
        /// <para><b>The command ordinals moved and the seeds are still the seeds.</b> The report named
        /// seed 1 command 525, seed 3 command 280 and seed 14 command 750, measured before milestones
        /// 8.35z20 through 8.35z22 landed; a content revision, the push-timing change and the driver
        /// change all move where a deterministic walk is at command N. Re-measured on this tree, seed
        /// 3 reaches the pause at command 516 and the other two no longer walk into it at all. The
        /// ordinal is therefore NOT asserted - only that no seed meets an offered-then-refused
        /// command - because pinning a number that three unrelated milestones can move would make
        /// this case a tripwire for their churn rather than for this defect.</para>
        ///
        /// <para><b>What makes it worth keeping anyway.</b> Random-legal play never met this in sixty
        /// runs and the greedy brain met it in three, which is milestone 8.35f's lesson again - a
        /// state a random walk passes through cannot be found by waiting for the walk to stop there -
        /// so the preference driver is the instrument, not the die.</para>
        /// </summary>
        [Test]
        public void TheThreeReportedSelfPlaySeeds_MeetNoOfferedThenRefusedCommand()
        {
            var found = new List<string>();
            foreach (var seed in new[] { 1, 3, 14 })
            {
                var session = ConfiguredMatchFixtures.CreateEngagedOfficialSixPlayerSession(2);
                var result = BotSelfPlay.Play(session, seed, 2400, _ => BotTier.Greedy);
                found.AddRange(result.Refusals.Select(refusal => refusal.ToString()));
            }

            Assert.That(found, Is.Empty,
                "An offered command the rules refused is an offer-feasibility defect wherever it " +
                "turns up: " + string.Join(" || ", found));
        }

        // -----------------------------------------------------------------------------------------
        // The board vocabulary this fixture needs
        // -----------------------------------------------------------------------------------------

        /// <summary>The seat Tigerclaw's staged one-versus-one opposes.</summary>
        private const string EnemyHeroId = "primitive-hero";

        /// <summary>
        /// One step from the attacker's own staged space and two from the target's, which is the
        /// whole geometry the case needs: a legal single move that ends out of contact.
        /// </summary>
        private static readonly HexCoordinate AwayFromTheTarget = StagedHeroBoard.FirstAdjacent;

        /// <summary>
        /// Whether an Attack's "Before the attack:" body can relocate the ATTACKER, read off the
        /// composed nodes rather than off the transcription.
        ///
        /// <para><b>A direct-move node alone is not the test, and measuring it that way misread a
        /// face.</b> Arien's Noble Blade composes <c>synthetic-direct-move</c> too - "Before the
        /// attack: You may move another unit that is adjacent to the target 1 space" - and its
        /// parenthesised reminder says in as many words that "another unit" never includes you. It
        /// moves a third piece, so the attacker and his target stay exactly as far apart as they
        /// were, and it cannot reach this milestone's pause at all. What separates the two shapes is
        /// the move's ORIGIN: a body that relocates the attacker names his own space first, through
        /// <c>synthetic-actor-space</c>, and one that relocates somebody else chooses a subject
        /// instead.</para>
        /// </summary>
        private static bool MovesTheAttacker(ActionDefinition action)
        {
            var body = action.CardTextAt(CardTextTiming.BeforeTheAttack);
            if (body == null)
            {
                return false;
            }

            var capabilities = body.Nodes
                .Where(node => node.Kind == EffectNodeKind.Capability)
                .Select(node => node.Capability.CapabilityId.Value)
                .ToArray();
            return capabilities.Contains("synthetic-direct-move") &&
                capabilities.Contains("synthetic-actor-space");
        }

        private static int Distance(HexCoordinate first, HexCoordinate second)
        {
            var q = first.Q - second.Q;
            var r = first.R - second.R;
            return (System.Math.Abs(q) + System.Math.Abs(r) + System.Math.Abs(q + r)) / 2;
        }
    }
}
