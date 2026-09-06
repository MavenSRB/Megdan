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
    /// <para><b>How it was reached, and why nothing reaches it now.</b> The step-6 case needed a
    /// defender's card text. This one was reached by the ATTACKER's own printed body: p.14 applied
    /// "Before the attack:" text AFTER the target was named, so a face printing "Before the attack:
    /// You may move 1 space. Target a unit adjacent to you." could legally step out of contact
    /// between naming its target and the defender answering.</para>
    ///
    /// <para><b>Milestone 8.35z25 closed that.</b> The user's 2026-09-06 ruling is that a
    /// card's PRINTED READING ORDER governs, and both faces print the move before the targeting -
    /// so the target is now named last, from the space the move left him in, and the interval the
    /// attacker used to out-run his own target no longer exists. The guards below are still right
    /// and are still driven; what changed is that their out-of-contact variant is no longer
    /// reachable from dealt content, which
    /// <see cref="NoDealtFaceCanOpenTheDefenseResponseOutOfContact"/> asserts and
    /// <see cref="EveryFaceThatCanLeaveContactMidAttack_IsAccountedFor"/> keeps true face by
    /// face.</para>
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
        /// The staged position: p.14 stopped at its Defense response, reached through the whole of
        /// one of these two printed bodies.
        ///
        /// <para><b>Milestone 8.35z25 restaged this, and the reason is the point of that
        /// milestone.</b> It used to drive the attacker OUT OF CONTACT with a target he had already
        /// named - p.14 opened its target question first, the printed "Before the attack:" move was
        /// applied after it, and the attacker stepped away inside his own Attack. That ordering was
        /// against the card's own print and the user's 2026-09-06 ruling reversed it: the move is
        /// offered first and "a unit adjacent to you" is named from the space it left him in, so
        /// there is no longer any interval in which an attacker can out-run his own target.
        /// <see cref="NoDealtFaceCanOpenTheDefenseResponseOutOfContact"/> is that closure, asserted
        /// over the content rather than described here.</para>
        ///
        /// <para><b>What the cases below still drive, and what they no longer can.</b> They still
        /// drive the whole pause through the two guards this fixture is about - every scored answer
        /// and the Decline beside them, the outcome the rules then produce, and the snapshot restore
        /// cross-check. What no dealt content can put them in any more is the OUT-OF-CONTACT variant,
        /// which is the narrowed predicate's own case; re-establishing driven coverage of that
        /// predicate needs a face with the retired shape and is recorded as integration-owed rather
        /// than minted here, because the only place to mint one is the showcase hand the configured
        /// card matrix pins.</para>
        ///
        /// <para>Nothing here is hand-placed into an engine state: Tigerclaw commits the face and
        /// plays it, answers the printed move, and names the one enemy hero beside him out of the
        /// candidates the node itself published.</para>
        /// </summary>
        private static StagedHeroBoard AnAttackerAtThePauseHisPrintedBodyOpens(
            CardDefinitionId face, int tier)
        {
            var board = TigerclawBoard.Create(
                tier: tier,
                heroPlacements: StagedHeroBoard.At(EnemyHeroId, StagedHeroBoard.ThirdAdjacent));

            Assert.That(
                Distance(board.HeroSpace(board.Hero), board.HeroSpace(board.Enemy)), Is.EqualTo(1),
                "The Attack is non-Ranged, so the case starts with the two heroes in contact.");

            board.StartPrimary(face);
            Assert.That(
                board.Session.Projection.PendingAttackTargetChoice, Is.Null,
                "The printed order asks the move before anything is targeted, so p.14's ordinary " +
                "target question is not what opens this Attack.");

            var move = board.Session.Projection.PendingEffectChoice;
            Assert.That(move?.NodeId.Value, Is.EqualTo("before-move"),
                "\"Before the attack: You may move 1 space.\" is the first thing the face prints " +
                "and the first thing it asks.");
            Assert.That(
                board.CandidateSpaces(move), Has.Member(AwayFromTheTarget),
                "The move is unconstrained, so the space that would strand him is offered too - " +
                "this case simply does not take it.");
            board.Decline();

            var victim = board.Session.Projection.PendingEffectChoice;
            Assert.That(victim?.NodeId.Value, Is.EqualTo("before-target"),
                "\"Target a unit adjacent to you.\" - the second printed sentence.");
            Assert.That(board.CandidateHeroes(victim), Has.Member(board.Enemy));
            board.Select(board.Enemy);
            board.Confirm();

            Assert.That(
                Distance(board.HeroSpace(board.Hero), board.HeroSpace(board.Enemy)), Is.EqualTo(1),
                "He declined the move, so he names his neighbour from where he already stood.");
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
            var probe = AnAttackerAtThePauseHisPrintedBodyOpens(TigerclawCardContent.CombatReflexes, 2);
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
                var board = AnAttackerAtThePauseHisPrintedBodyOpens(TigerclawCardContent.CombatReflexes, 2);
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
            var board = AnAttackerAtThePauseHisPrintedBodyOpens(TigerclawCardContent.CombatReflexes, 2);
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
            var board = AnAttackerAtThePauseHisPrintedBodyOpens(TigerclawCardContent.CombatReflexes, 2);
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
            var board = AnAttackerAtThePauseHisPrintedBodyOpens(TigerclawCardContent.CombatReflexes, 2);
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
            var board = AnAttackerAtThePauseHisPrintedBodyOpens(TigerclawCardContent.LeapingStrike, 3);
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

                // Milestone 8.35z25 moved these two out of the exposed family, and moving
                // them is the whole milestone. They print "You may move 1 space." BEFORE "Target a
                // unit adjacent to you.", and 8.23 composed only the first sentence - leaving the
                // second to p.14's ordinary target question, which is asked when the action starts.
                // The user's 2026-09-06 ruling put the two sentences back in printed order, so the
                // move is answered first and the target is named from the space it left him in.
                // A revert would flip these rows back to `|keeps` and fail this case, which is what
                // makes the table the structural guard for the reordering itself.
                "tigerclaw-combat-reflexes|moves|rebinds",
                "tigerclaw-leaping-strike|moves|rebinds"

                // `moves|keeps`: THE EXPOSED FAMILY, and it is now EMPTY - see
                // NoDealtFaceCanOpenTheDefenseResponseOutOfContact, which says so as an assertion
                // rather than as the absence of rows here.
            }),
                "A face printing \"Before the attack:\" text that this table does not name has never " +
                "been driven through p.14's step 3 after its body ran. Add the row; and if it is a " +
                "`moves|keeps` row, drive its out-of-contact Defense pause before shipping the face.");
        }

        /// <summary>
        /// THE CLOSURE, asserted rather than described. Milestone 8.35z25.
        ///
        /// <para>An attacker can only be out of contact with his own target at p.14's step 3 if the
        /// text that moved him ran AFTER the target was named. Every dealt Attack that moves its
        /// attacker at step 1 now names its target inside that same text, from the space the move
        /// produced - so the naming is last and there is no interval left to move in.</para>
        ///
        /// <para><b>Why this is a case and not a comment.</b> The narrowed "still standing" predicate
        /// this fixture is about stays correct and stays driven, but its out-of-contact variant is no
        /// longer reachable from content - so a future face that re-opens the class would silently
        /// restore an unguarded path. This fails the moment one does, and names what to do about it.
        /// It is deliberately derived from the same two columns as the table above rather than from a
        /// hand-kept list.</para>
        /// </summary>
        [Test]
        public void NoDealtFaceCanOpenTheDefenseResponseOutOfContact()
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var exposed = configuration.CardDefinitions
                .SelectMany(definition => definition.ActionDefinitions
                    .Where(action =>
                        action.Kind == ActionKind.Attack &&
                        action.CardTextTiming == CardTextTiming.BeforeTheAttack &&
                        MovesTheAttacker(action) &&
                        !(action.AttackTargetPolicy?.TargetIsBoundByCardText ?? false))
                    .Select(_ => definition.Reference.DefinitionId.Value))
                .Distinct()
                .OrderBy(id => id, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(exposed, Is.Empty,
                "This face moves its attacker at p.14's step 1 and lets the page name the target " +
                "before that move, so it can open step 3 with the two out of contact - the class " +
                "the 2026-09-06 reading-order ruling closed. Either compose its targeting inside " +
                "its own text (the move-then-target seam Brogan's charges established), or drive " +
                "its out-of-contact Defense pause and restore the coverage this closure retired: " +
                string.Join(", ", exposed));
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
