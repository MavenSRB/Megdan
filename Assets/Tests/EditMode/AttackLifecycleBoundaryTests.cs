using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.29f. An Attack that has reached rulebook p.14's end resolves its source card and
    /// hands Initiative on, and one that has not exposes no second action on that card.
    ///
    /// <para><b>What was wrong.</b> A player performed Arien's Red Tier I Dangerous Current - "Target a
    /// unit adjacent to you. Before the attack: Up to 1 enemy hero ... discards a card, or is defeated"
    /// - against an adjacent enemy minion, the minion was defeated and removed, and the same card's
    /// printed secondary Movement was then accepted. p.7 gives a card one action.</para>
    ///
    /// <para><b>Why.</b> Milestone 8.19a let p.14 stop at its step 1, so
    /// <c>AnswerAttackTarget</c> returns the moment it has emitted the Before-the-Attack text, with no
    /// Defeat on the result; the rest of the page is produced afterwards, by <c>MaterializeSteps</c> in
    /// the very same command when the text asks nothing. The turn then decided whether the Attack was
    /// over by reading <c>MinionDefeat</c> off that result - a field written before the minion was
    /// defeated - so the whole page ran, the log recorded <c>AttackResolutionCompleted</c>, and the card
    /// stayed in the turn area with its actor still selected. This is the shape milestone 8.25e named on
    /// the after-action repeat path: what decides whether the turn is still waiting is what actually
    /// stands once the steps have been applied, not what the resolver reported before they ran.</para>
    ///
    /// <para><b>And the other half.</b> While an Attack is genuinely paused - on p.14's Defense
    /// question, or inside its own card text - the offer set still reported the card's other printed
    /// action as available, because <c>CanStart</c> had never asked whether an action was already under
    /// way. Since milestone 8.29b1 that offer is a luminous region on the resolving card, so it is a
    /// control a player can press.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class AttackLifecycleBoundaryTests
    {
        /// <summary>A free playable space far from the engagement, for the seat a case needs out of it.</summary>
        private static readonly HexCoordinate FarFromTheEngagement = new HexCoordinate(2, 0);

        private const string AttackCompleted = "Action.AttackResolutionCompleted";
        private const string ActorSelected = "Initiative.NextActorSelected";
        private const string CardResolved = "Turn.CardResolved";

        // -----------------------------------------------------------------------------------------
        // The reported sequence
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The player's own sequence, with the stale invocation a presenter would still be holding.
        ///
        /// <para>The Movement is captured from the offer set <em>before</em> the Attack starts, which is
        /// exactly what milestone 8.29b1's resolving-card regions do: the printed secondary Movement's
        /// region is bound while the card is exposed, and the region outlives the Attack performed from
        /// the same face. Submitting it afterwards has to be refused by the rules rather than by the
        /// region not being drawn.</para>
        /// </summary>
        [Test]
        public void ACompletedAttack_CannotStartASecondActionOnItsUnresolvedSourceCard()
        {
            var board = DangerousCurrentReadyToAct();
            var card = SourceCard(board, ArienCardContent.DangerousCurrent);

            var staleMovement = board.Session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                offer.InvocationContext == ActionInvocationContext.PrintedSecondary);
            Assert.That(staleMovement.SourceCardInstanceId, Is.EqualTo(card),
                "The invocation a presenter holds names the card it was drawn on.");

            StartThePrintedAttack(board);
            var target = board.MinionAt(StagedHeroBoard.FirstAdjacent);
            var choice = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargetMinionIds, Has.Member(target),
                "The adjacent enemy minion is the reported target.");
            board.Accept(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, target));

            // p.14 step 1 asked nothing: "up to 1 enemy hero in any of the 2 spaces in a straight line
            // directly behind the target" names nobody on this board, so the zero-hero outcome is the
            // one the text follows. The page then ran to its end over the minion.
            Assert.That(board.Session.Projection.PendingEffectChoice, Is.Null,
                "The Before-the-Attack text asked nothing, which is the reported run.");
            Assert.That(board.MinionIds(), Has.No.Member(target),
                "and the minion Defeat and removal completed.");
            Assert.That(EventTypesSince(board, 0), Has.Member(AttackCompleted));

            Assert.That(ResolutionStatusOf(board, card), Is.EqualTo(CardResolutionStatus.Resolved),
                "The completed Attack's source card resolves, which is what p.7 spends.");
            Assert.That(CountOf(board, CardResolved, card), Is.EqualTo(1),
                "exactly once, and the log says so.");
            Assert.That(
                board.Session.Projection.SelectedActor.IsDefined &&
                    board.Session.Projection.SelectedActor.CardInstanceId.Equals(card),
                Is.False,
                "and Initiative advanced off it.");

            var before = board.Session.Projection;
            var refused = board.Session.Execute(DirectorCommand.StartAction(
                staleMovement.SourceCardInstanceId, staleMovement.ActionDefinitionId));

            Assert.That(refused.Accepted, Is.False,
                "The stale Movement invocation is refused by the turn authority, not by the region " +
                "no longer being drawn.");
            var after = board.Session.Projection;
            Assert.That(after.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "Atomically: a refused command changes no authoritative state.");
            Assert.That(after.AcceptedCommands.Count, Is.EqualTo(before.AcceptedCommands.Count),
                "and does not enter accepted history.");
            Assert.That(
                after.DiagnosticEvents.Select(entry => entry.DisplayText),
                Is.EqualTo(before.DiagnosticEvents.Select(entry => entry.DisplayText)),
                "so there is no second action start and no extra card transition.");
        }

        /// <summary>
        /// The same board and the same refusal after the state has crossed a snapshot, because
        /// acceptance is about the reconstructed match as much as about the live one.
        /// </summary>
        [Test]
        public void TheOneActionBoundary_SurvivesASnapshotRoundTripAndAReplay()
        {
            var board = DangerousCurrentReadyToAct();
            var card = SourceCard(board, ArienCardContent.DangerousCurrent);
            var staleMovement = board.Session.Projection.AvailableTurnActions
                .Single(offer =>
                    offer.IsAvailable &&
                    offer.InvocationContext == ActionInvocationContext.PrintedSecondary)
                .ActionDefinitionId;
            StartThePrintedAttack(board);
            var choice = board.Session.Projection.PendingAttackTargetChoice;
            board.Accept(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId,
                board.MinionAt(StagedHeroBoard.FirstAdjacent)));

            board.RoundTripThroughASnapshot();
            Assert.That(ResolutionStatusOf(board, card), Is.EqualTo(CardResolutionStatus.Resolved),
                "A restored match agrees the card is spent, because the boundary is the card's own " +
                "state rather than a flag the turn remembers.");
            Assert.That(
                board.Session.Execute(DirectorCommand.StartAction(card, staleMovement)).Accepted,
                Is.False,
                "and the restored match refuses the same stale invocation.");
        }

        // -----------------------------------------------------------------------------------------
        // The invariant, over every terminal path a played match reaches
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Every Attack a played match performs either ends with its source card resolved and Initiative
        /// handed on, or names the decision that is still holding it - and while it is held, no
        /// unrelated ordinary action is offered on that card.
        /// </summary>
        /// <remarks>
        /// This is the check that prevents a card-specific repair: the rows are the shapes rulebook
        /// p.14 ends in, not the cards that happen to print them, and each is played to the end it
        /// names. Every row asserts the same pair - a terminal path has resolved its source card
        /// exactly once and handed Initiative on, a paused one has resolved nothing and names the
        /// decision that is holding it - and both assert that no unrelated ordinary action is offered
        /// on that card. The table is deterministic rather than swept, because a seed that stopped
        /// reaching one of these shapes would quietly narrow the claim;
        /// <see cref="NoPlayedMatchOffersASecondActionOnACardThatIsAlreadyActing"/> is the breadth
        /// beside it.
        /// </remarks>
        [Test]
        public void EveryTerminalAttackPath_ResolvesItsSourceCardExactlyOnceOrNamesItsPendingContinuation()
        {
            foreach (var path in AttackPaths())
            {
                var board = path.Play();
                var card = SourceCard(board, path.Card);
                var resolutions = CountOf(board, CardResolved, card);

                Assert.That(
                    board.Session.Projection.AvailableTurnActions.Any(offer =>
                        offer.IsAvailable && offer.SourceCardInstanceId.Equals(card)),
                    Is.False,
                    $"'{path.Name}': rulebook p.7 gives this card one action and it is spent or under " +
                    "way, so no ordinary action may be offered on it - which is what a printed region " +
                    "a player can press means since milestone 8.29b1.");

                if (path.IsTerminal)
                {
                    Assert.That(resolutions, Is.EqualTo(1),
                        $"'{path.Name}' reached the end of p.14, so its source card resolved once. " +
                        $"Standing decision: {StandingDecision(board.Session.Projection) ?? "none"}.");
                    Assert.That(
                        ResolutionStatusOf(board, card), Is.EqualTo(CardResolutionStatus.Resolved));
                    Assert.That(
                        board.Session.Projection.SelectedActor.IsDefined &&
                            board.Session.Projection.SelectedActor.CardInstanceId.Equals(card),
                        Is.False,
                        $"'{path.Name}': and Initiative advanced off the spent card.");
                    continue;
                }

                Assert.That(resolutions, Is.Zero,
                    $"'{path.Name}' is still holding its action, so its card has not resolved.");
                Assert.That(StandingDecision(board.Session.Projection), Is.EqualTo(path.Continuation),
                    $"'{path.Name}' names the decision that is holding it.");
            }
        }

        /// <summary>
        /// The same pair over played matches, so the rows above are not the only Attacks it is true of.
        /// </summary>
        /// <remarks>
        /// The four seeds are the first four rather than a chosen set, for the reason milestone 8.25e's
        /// sweep takes the first six of its own: the property is about every run, so choosing which runs
        /// to make it about would be choosing the answer. Measured over these four, the official
        /// six-player board reaches 14 completed Attacks - 6 hero Defeats, 7 minion Defeats with the
        /// removal completed, one successful Defense, and 25 applications of Before- and
        /// After-the-Attack text - so the invariant is asked of real p.14 endings rather than of a quiet
        /// stretch of match.
        /// </remarks>
        [Test]
        public void NoPlayedMatchOffersASecondActionOnACardThatIsAlreadyActing()
        {
            foreach (var seed in SweepSeeds)
            {
                Sweep(ConfiguredMatchFixtures.CreateEngagedOfficialSixPlayerSession(3),
                    seed, "official six-player");
            }
        }

        /// <summary>
        /// Milestone 8.35u re-derived the set, {1, 2, 3, 4} -> {1, 2, 8, 12}. Simultaneous levelling
        /// asks rulebook p.15's questions in a different order, so every trajectory diverges after the
        /// first level-up: seed 3 now completes NO Attack inside the budget, which this sweep's own
        /// counter caught and reported rather than passing vacuously, and seed 4 completes only one.
        /// A thirty-seed offline scan
        /// (`tmp/8-35d-8-35e-multiplayer-corrections/seed-scan-attacks.txt`) kept the two that still
        /// carry the property and replaced the two that stopped, choosing the richest available rather
        /// than the next that merely passes: seeds 1, 2, 8 and 12 take four, five, four and five
        /// Attack targets. The sweep is about real p.14 endings, so more of them is more evidence.
        ///
        /// <para>Milestone 8.35z5 re-derived it again, {1, 2, 8, 12} -> {1, 2, 12, 15}, after
        /// closing Living Tsunami's empty-Confirm defect: an offer seed 8's walk used to be able to take
        /// is gone, so its trajectory reroutes and it now completes no Attack at all inside the budget -
        /// the same shape 8.35u's own re-derivation describes, one milestone later. A thirty-seed offline
        /// scan found seed 15 the richest replacement available (five Attack targets, tying seeds 1 and
        /// 12 rather than merely passing), so it is what took seed 8's place; seeds 1, 2 and 12 all
        /// survived unmoved.</para>
        /// </summary>
        private static readonly int[] SweepSeeds = { 1, 2, 12, 15 };

        private const int SweepBudget = 1200;

        private static void Sweep(MatchSession session, int seed, string board)
        {
            var random = new MatchLoopRandom(seed);
            var count = 0;
            while (session.Projection.Phase != RoundResolutionPhase.GameEnded && count < SweepBudget)
            {
                AssertNoSecondActionIsOfferedOnACardThatIsAlreadyActing(
                    session.Projection, board, seed, count);

                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                if (candidates.Count == 0)
                {
                    Assert.Fail(
                        $"The {board} board on seed {seed} reached a live match the projection " +
                        $"offered no legal command for, after {count} commands.");
                }

                var chosen = candidates[random.Next(candidates.Count)];
                var result = session.Execute(chosen);
                Assert.That(result.Accepted, Is.True,
                    $"Seed {seed} command {count + 1} ({chosen.Kind}) was offered and refused: " +
                    result.Detail);
                count++;
            }

            AssertEveryCompletedAttackResolvedItsCardExactlyOnce(session.Projection, board, seed);
        }

        /// <summary>
        /// The offer set and the start command give one answer: no ordinary action is offered while the
        /// selected card's one action is already being performed, or has already finished.
        /// </summary>
        private static void AssertNoSecondActionIsOfferedOnACardThatIsAlreadyActing(
            MatchSessionProjection projection, string board, int seed, int step)
        {
            if (!projection.AvailableTurnActions.Any(offer => offer.IsAvailable))
            {
                return;
            }

            var where = $"{board} seed {seed}, after {step} commands";
            Assert.That(StandingDecision(projection), Is.Null,
                $"{where}: an ordinary action is offered while a decision is standing.");
            Assert.That(projection.PendingMovementChoice, Is.Null,
                $"{where}: an ordinary action is offered while a Movement is being spent.");

            var selected = projection.SelectedActor;
            if (!selected.IsDefined)
            {
                return;
            }

            Assert.That(
                AnAttackCompletedOnThisSelection(projection, selected.CardInstanceId), Is.False,
                $"{where}: '{selected.CardInstanceId.Value}' has already completed an Attack this " +
                "turn and is still being offered an ordinary action, which p.7 gives it one of.");
        }

        /// <summary>
        /// Every Attack the run completed resolved its own source card, once. Read from the log at the
        /// end of a run rather than per step, because the two events land in one command.
        /// </summary>
        private static void AssertEveryCompletedAttackResolvedItsCardExactlyOnce(
            MatchSessionProjection projection, string board, int seed)
        {
            var attacksPerTurn = CountPerTurn(projection, AttackCompleted);
            var resolutionsPerTurn = CountPerTurn(projection, CardResolved);
            Assert.That(attacksPerTurn, Is.Not.Empty,
                $"The {board} board on seed {seed} completed no Attack at all, so it proves nothing " +
                "about one.");

            foreach (var turn in attacksPerTurn)
            {
                resolutionsPerTurn.TryGetValue(turn.Key, out var resolutions);
                Assert.That(resolutions, Is.EqualTo(1),
                    $"{board} seed {seed}: {turn.Key} completed {turn.Value} Attack instance(s) and " +
                    $"recorded {resolutions} card resolution(s). Rulebook p.7 spends the card on the " +
                    "action, so a turn that completed an Attack resolved its card exactly once - a " +
                    "repeat is a second instance of one action and does not resolve the card twice.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Reading the log
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Whether an Attack has completed since Initiative last selected somebody, on this card. The
        /// selection is the boundary because a card instance is committed again in a later round, and a
        /// record from that round's play is not about this turn.
        /// </summary>
        private static bool AnAttackCompletedOnThisSelection(
            MatchSessionProjection projection, CardInstanceId card)
        {
            var selectedAt = projection.DiagnosticEvents
                .Where(entry => entry.EventType == ActorSelected)
                .Select(entry => entry.Sequence)
                .DefaultIfEmpty(0)
                .Max();
            return projection.DiagnosticEvents.Any(entry =>
                entry.Sequence > selectedAt &&
                entry.EventType == AttackCompleted &&
                CardOf(entry) == card.Value);
        }

        // -----------------------------------------------------------------------------------------
        // The paths
        // -----------------------------------------------------------------------------------------

        /// <summary>One end rulebook p.14 can reach, and the board that reaches it.</summary>
        private sealed class AttackPath
        {
            internal AttackPath(
                string name,
                CardDefinitionId card,
                Func<StagedHeroBoard> play,
                string continuation = null)
            {
                Name = name;
                Card = card;
                Play = play;
                Continuation = continuation;
            }

            internal string Name { get; }
            internal CardDefinitionId Card { get; }
            internal Func<StagedHeroBoard> Play { get; }

            /// <summary>The decision that holds this path open, or <c>null</c> when it is terminal.</summary>
            internal string Continuation { get; }

            internal bool IsTerminal => Continuation == null;
        }

        private static IEnumerable<AttackPath> AttackPaths()
        {
            yield return new AttackPath(
                "before-the-attack text that asks nothing, minion Defeat",
                ArienCardContent.DangerousCurrent,
                () =>
                {
                    var board = DangerousCurrentReadyToAct();
                    StartThePrintedAttack(board);
                    AnswerTheTarget(board, board.MinionAt(StagedHeroBoard.FirstAdjacent));
                    return board;
                });

            yield return new AttackPath(
                "before-the-attack text standing",
                ArienCardContent.NobleBlade,
                NobleBladeWithItsTextStanding,
                "effect choice");

            yield return new AttackPath(
                "before-the-attack text answered, minion Defeat",
                ArienCardContent.NobleBlade,
                () =>
                {
                    var board = NobleBladeWithItsTextStanding();
                    board.Decline();
                    return board;
                });

            yield return new AttackPath(
                "hero target, Defense standing",
                ArienCardContent.NobleBlade,
                NobleBladeAtTheEnemyHero,
                "defense");

            yield return new AttackPath(
                "hero target, successfully defended",
                ArienCardContent.NobleBlade,
                () =>
                {
                    var board = NobleBladeAtTheEnemyHero();
                    board.DefendSuccessfully();
                    return board;
                });

            yield return new AttackPath(
                "hero target, Defense declined and the hero defeated",
                ArienCardContent.NobleBlade,
                () =>
                {
                    var board = NobleBladeAtTheEnemyHero();
                    board.Accept(DirectorCommand.DeclineDefense());
                    return board;
                });

            yield return new AttackPath(
                "after-the-attack text standing",
                ArienCardContent.RogueWave,
                RogueWaveWithItsPushStanding,
                "effect choice");

            yield return new AttackPath(
                "after-the-attack text answered",
                ArienCardContent.RogueWave,
                () =>
                {
                    var board = RogueWaveWithItsPushStanding();
                    board.Decline();
                    return board;
                });

            yield return new AttackPath(
                "card-text repeat offered, the action still open",
                ArienCardContent.ViolentTorrent,
                ViolentTorrentWithItsRepeatOffered,
                "effect choice");

            yield return new AttackPath(
                "card-text repeat declined, the asking instance ends the action",
                ArienCardContent.ViolentTorrent,
                () =>
                {
                    var board = ViolentTorrentWithItsRepeatOffered();
                    board.Decline();
                    return board;
                });

            yield return new AttackPath(
                "card-text repeat taken, the final instance",
                ArienCardContent.ViolentTorrent,
                () =>
                {
                    var board = ViolentTorrentWithItsRepeatOffered();
                    board.Confirm();
                    AnswerTheTarget(board, board.MinionAt(StagedHeroBoard.SecondAdjacent));
                    return board;
                });
        }

        /// <summary>The decision standing right now, named, or <c>null</c> when the turn is free.</summary>
        private static string StandingDecision(MatchSessionProjection projection) =>
            projection.PendingAttackTargetChoice != null ? "attack target"
            : projection.PendingDefensePreview != null ? "defense"
            : projection.PendingEffectChoice != null ? "effect choice"
            : projection.PendingRemovalPrevention != null ? "removal prevention"
            : projection.PendingDisplacedMinionCorrection != null ? "displaced minion correction"
            : projection.PendingLanePushRequest != null ? "lane push"
            : projection.PendingMinionBattleChoice != null ? "minion battle"
            : projection.PendingHeroRespawnChoice != null ? "hero respawn"
            : null;

        /// <summary>
        /// How many events of this type each (round, turn, card) saw. A card instance is committed
        /// again in a later round, so the turn is what a "one action" claim is about.
        /// </summary>
        private static Dictionary<string, int> CountPerTurn(
            MatchSessionProjection projection, string eventType)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in projection.DiagnosticEvents.Where(
                value => value.EventType == eventType))
            {
                var key = TurnKeyOf(entry);
                if (key == null)
                {
                    continue;
                }

                counts.TryGetValue(key, out var seen);
                counts[key] = seen + 1;
            }

            return counts;
        }

        /// <summary>"round=R;turn=T;card=C", or <c>null</c> when the entry names no card.</summary>
        private static string TurnKeyOf(RuleEventLogEntry entry)
        {
            var round = FieldOf(entry, "round");
            var turn = FieldOf(entry, "turn");
            var card = FieldOf(entry, "card");
            return card == null || round == null || turn == null
                ? null
                : $"round={round};turn={turn};card={card}";
        }

        private static string CardOf(RuleEventLogEntry entry) => FieldOf(entry, "card");

        private static string FieldOf(RuleEventLogEntry entry, string name)
        {
            var key = name + "=";
            foreach (var part in entry.StableReferences.Split(';'))
            {
                if (part.StartsWith(key, StringComparison.Ordinal))
                {
                    return part.Substring(key.Length);
                }
            }

            return null;
        }

        private static IReadOnlyList<string> EventTypesSince(StagedHeroBoard board, int sequence) =>
            board.Session.Projection.DiagnosticEvents
                .Where(entry => entry.Sequence > sequence)
                .Select(entry => entry.EventType)
                .ToArray();

        private static int CountOf(StagedHeroBoard board, string eventType, CardInstanceId card) =>
            board.Session.Projection.DiagnosticEvents.Count(entry =>
                entry.EventType == eventType && CardOf(entry) == card.Value);

        private static CardResolutionStatus ResolutionStatusOf(
            StagedHeroBoard board, CardInstanceId card) =>
            board.Session.Projection.Cards.Single(value => value.Id.Equals(card))
                .State.ResolutionStatus;

        private static CardInstanceId SourceCard(StagedHeroBoard board, CardDefinitionId cardId) =>
            board.Session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(board.Hero) &&
                card.DefinitionReference.DefinitionId.Equals(cardId)).Id;

        // -----------------------------------------------------------------------------------------
        // The reported board
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Arien holding Dangerous Current with one enemy minion beside him and the enemy hero out of
        /// the engagement, at the moment before any action starts - which is when milestone 8.29b1's
        /// resolving card has every printed region bound and a presenter holds both invocations.
        /// </summary>

        // -----------------------------------------------------------------------------------------
        // Milestone 8.37a. The whole page inside one command
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.37a. A face printing card text at BOTH of the attacker's own moments, pointed
        /// at a MINION, runs the whole of rulebook p.14 inside one command - and every step of it has
        /// to be materialized by the same code that materializes a step found at the top of the stream.
        ///
        /// <para><b>What was wrong.</b> p.13 gives a minion target no steps 3 to 5 - "Minions cannot
        /// defend. When you target an enemy minion with an attack, it is simply defeated." - so
        /// <c>ContinueAfterBeforeAttackText</c> resolves the Defeat and hands back the rest of the page
        /// INSIDE the step-1 text's own produced stream: step 7's card-text step, and the action's own
        /// <c>ActionEnded</c>. <c>MaterializeSteps</c> walked that produced stream with a second,
        /// smaller loop that knew neither - it read the card-text step as a fact, whose
        /// <see cref="CardActionResolutionEventKind"/> is <c>None</c>, and the engine threw
        /// <c>ArgumentException: A turn card-action event requires a defined card-action event</c>.
        /// The same gap raised <c>ActionEnded</c> through the plain timing path, which carries no
        /// <c>PerformedBasicAction</c>, so a trigger reading "after you defeat a minion with a basic
        /// attack" could not have fired even had the throw not happened.</para>
        ///
        /// <para><b>Why no earlier hero found it.</b> Against a HERO the page stops at the Defense
        /// window, so step 7 arrives in a later command through the outer loop, which has always been
        /// complete. Arien's Dangerous Current above prints ONE body and reaches the minion arm safely.
        /// It takes both at once, and Tali's Ice Blast is the first dealt face of that shape - its step-1
        /// body exists only because <c>ActionDefinition</c> refuses a second body without an explicit
        /// first, which is what makes this the quietest possible way to reach the gap.</para>
        /// </summary>
        [Test]
        public void ATwoBodyAttackAtAMinion_RunsTheWholePageInOneCommand_WithoutFaultingTheEngine()
        {
            var board = IceBlastBesideAMinion();
            var card = SourceCard(board, TaliCardContent.IceBlast);
            var target = board.MinionAt(StagedHeroBoard.FirstAdjacent);

            StartThePrintedAttack(board);
            AnswerTheTarget(board, target);

            Assert.That(
                board.Session.Projection.Minions.Any(minion => minion.MinionId.Equals(target)),
                Is.False,
                "p.13's minion arm: the piece is simply defeated, in the same command that named it.");
            Assert.That(ResolutionStatusOf(board, card), Is.EqualTo(CardResolutionStatus.Resolved),
                "The whole page ran, so the card that carried it is spent - which is the claim the " +
                "throw used to make impossible to check.");
        }

        /// <summary>
        /// Milestone 8.37a, the second half of the same gap and the one a throw would have hidden: the
        /// <c>ActionEnded</c> the page raises from inside step 1's produced stream carries rulebook
        /// p.9's performed-basic-action fact, so a trigger hanging on it can read what happened.
        ///
        /// <para>Reign of Winter is the content that asks - "Each time after you defeat a minion with
        /// your basic attack" - and Ice Blast is her only basic Attack. Driven here at the authority
        /// rather than only in her Play Mode journey, because a fact carried in the wrong shape is a
        /// question about the event stream and not about the screen.</para>
        /// </summary>
        [Test]
        public void TheActionEndedItRaises_CarriesTheBasicAttackThatDefeatedTheMinion()
        {
            var board = IceBlastBesideAMinion(withHerUltimate: true);
            var target = board.MinionAt(StagedHeroBoard.FirstAdjacent);

            StartThePrintedAttack(board);
            AnswerTheTarget(board, target);

            Assert.That(board.Session.Projection.PendingEffectChoice, Is.Not.Null,
                "Reign of Winter's own selection stands, which is only true if the ActionEnded the " +
                "page raised knew a basic Attack had just defeated a minion.");
            Assert.That(board.Session.Projection.PendingEffectChoice.ChooserId, Is.EqualTo(board.Hero),
                "\"An enemy hero in radius\" is hers to name.");
        }

        /// <summary>
        /// Tali holding Ice Blast with one enemy minion beside her and the enemy hero out of the
        /// engagement - the two-body face pointed at the arm that runs the whole page at once.
        /// </summary>
        private static StagedHeroBoard IceBlastBesideAMinion(bool withHerUltimate = false)
        {
            // Her Tier III configuration already unlocks Reign of Winter, exactly as Misa's unlocks
            // Power Overwhelming, so nothing here passes `unlockUltimate` - measured, not assumed:
            // passing it fails on the card no longer being in the deck to move.
            var board = TaliBoard.Create(
                tier: withHerUltimate ? 3 : 1,
                minionPlacements: StagedHeroBoard.At(
                    "red-melee-01", StagedHeroBoard.FirstAdjacent),
                // The enemy hero is put out of the engagement for the plain case and left where the
                // staging stands them - two spaces away, inside Radius 4 - for the Ultimate's, which
                // needs the trigger to find a subject.
                heroPlacements: withHerUltimate
                    ? null
                    : StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01"));
            Assert.That(board.IsPrimaryAvailable(TaliCardContent.IceBlast), Is.True,
                "The case starts from an Attack the board offers.");
            return board;
        }

        private static StagedHeroBoard DangerousCurrentReadyToAct()
        {
            var board = ArienBoard.Create(
                minionPlacements: StagedHeroBoard.At(
                    "red-melee-01", StagedHeroBoard.FirstAdjacent),
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01"));
            Assert.That(board.IsPrimaryAvailable(ArienCardContent.DangerousCurrent), Is.True,
                "The reported sequence starts from an Attack the board offers.");
            return board;
        }

        private static void StartThePrintedAttack(StagedHeroBoard board)
        {
            var attack = board.Session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable &&
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
            board.Accept(DirectorCommand.StartAction(
                attack.SourceCardInstanceId, attack.ActionDefinitionId));
        }

        private static void AnswerTheTarget(StagedHeroBoard board, MinionId target)
        {
            var choice = board.Session.Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "An Attack opens rulebook p.14's target question.");
            Assert.That(choice.LegalTargetMinionIds, Has.Member(target),
                $"'{target.Value}' has to be reachable, or the case proves something about range.");
            board.Accept(DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, target));
        }

        /// <summary>
        /// Noble Blade at an adjacent minion with another unit beside that minion, so its
        /// "Before the attack: You may move another unit that is adjacent to the target 1 space"
        /// opens rulebook p.10's confirm-or-decline and stops the page at step 1.
        /// </summary>
        private static StagedHeroBoard NobleBladeWithItsTextStanding()
        {
            var board = ArienBoard.Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent,
                    ["red-melee-02"] = StagedHeroBoard.TwoAway
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01", "red-melee-02"));
            board.PlayAttackAt(
                ArienCardContent.NobleBlade, board.MinionAt(StagedHeroBoard.FirstAdjacent));
            return board;
        }

        /// <summary>
        /// Noble Blade at the enemy hero standing beside him, stopped at p.14's Defense response. The
        /// board is otherwise empty, so the step-1 text has no other unit to offer to move.
        /// </summary>
        private static StagedHeroBoard NobleBladeAtTheEnemyHero()
        {
            var board = ArienBoard.Create(
                heroPlacements: StagedHeroBoard.At(
                    "primitive-hero", StagedHeroBoard.FirstAdjacent),
                banishedMinions: AllMinions);
            board.PlayAttackAt(
                ArienCardContent.NobleBlade, board.Enemy, defendSuccessfully: false);
            return board;
        }

        /// <summary>
        /// Rogue Wave fired at a minion two spaces off with one enemy minion beside him, so its
        /// "After the attack: You may push an enemy unit adjacent to you up to 2 spaces" opens at
        /// p.14's step 7.
        /// </summary>
        private static StagedHeroBoard RogueWaveWithItsPushStanding()
        {
            var board = ArienBoard.Create(
                tier: 2,
                branch: ArienUpgradeBranch.SecondSibling,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.TwoAway,
                    ["red-melee-02"] = StagedHeroBoard.SecondAdjacent
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01", "red-melee-02"));
            board.PlayAttackAt(
                ArienCardContent.RogueWave, board.MinionAt(StagedHeroBoard.TwoAway));
            return board;
        }

        /// <summary>
        /// Violent Torrent - "May repeat once on a different unit" - played at the first of two
        /// adjacent enemy minions and stopped on the offer to repeat. p.10 makes each instance a
        /// separate action on the same card, so the card is still unresolved while the offer stands
        /// and whichever way it is answered.
        /// </summary>
        private static StagedHeroBoard ViolentTorrentWithItsRepeatOffered()
        {
            var board = ArienBoard.Create(
                tier: 3,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent,
                    ["red-melee-02"] = StagedHeroBoard.SecondAdjacent
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: EveryMinionExcept("red-melee-01", "red-melee-02"));
            board.StartPrimary(ArienCardContent.ViolentTorrent);

            // p.15 has taken him to Tier III, so p.16's Ultimate is out of the deck and Living
            // Tsunami's `before-action-trigger` intercepts the start (milestone 8.19a). It is declined
            // rather than avoided: the trigger is a fact about this stage of the fixture, and an
            // Attack held in front of one is a state this row would otherwise never cross.
            var ultimate = board.Choice;
            Assert.That(ultimate, Is.Not.Null,
                "Tier III opens the Ultimate's before-action trigger before the Attack starts.");
            Assert.That(ultimate.NodeId.Value, Does.Contain("tsunami"),
                "and it is the Ultimate's own node: " + ultimate.NodeId.Value);
            board.Decline();

            Assert.That(board.Session.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "Violent Torrent opens p.14's target question. Standing instead: " +
                (StandingDecision(board.Session.Projection) ?? "nothing") + "; log tail: " +
                string.Join(" | ", board.Session.Projection.DiagnosticEvents
                    .Skip(Math.Max(0, board.Session.Projection.DiagnosticEvents.Count - 8))
                    .Select(entry => entry.DisplayText)));
            AnswerTheTarget(board, board.MinionAt(StagedHeroBoard.FirstAdjacent));
            return board;
        }

        private static IReadOnlyCollection<string> EveryMinionExcept(params string[] kept) =>
            AllMinions.Where(minion => !kept.Contains(minion)).ToArray();

        private static readonly string[] AllMinions =
        {
            "red-melee-01", "red-melee-02", "red-melee-03", "red-melee-04",
            "red-ranged-01", "red-heavy-01",
            "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
            "blue-ranged-01", "blue-heavy-01"
        };
    }
}
