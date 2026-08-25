using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.22. One configured Wasp-versus-Sabina match, with the pieces put where the printed
    /// clause under test can be asked a question, plus the handful of commands every Sabina fixture
    /// submits.
    ///
    /// <para><b>Why the boards are built rather than staged.</b> The staged engaged position is one
    /// position, and Sabina's set asks about several: one adjacent enemy minion, two of them, an
    /// immune Heavy, two friendly minions inside a Radius 2 disk, an enemy hero inside Range 1. On the
    /// staged board most of those are false, so a fixture that used it would have its questions
    /// answered by the staging instead of by the card. Each caller names the pieces it needs moved and
    /// everything else stays where the configured setup put it - the same affordance
    /// <see cref="BroganOneManArmyTests"/> takes, on the same terms: a starting position is setup
    /// content, no rule sees it, and nothing here invents card state.</para>
    ///
    /// <para>Sabina's staged space is <see cref="SabinaAt"/> on every board, and every coordinate a
    /// caller names is relative to it - which <see cref="Create"/> asserts, so a staging that moved
    /// her would fail loudly rather than quietly moving every fixture with her.</para>
    /// </summary>
    internal sealed class SabinaBoardFixture
    {
        /// <summary>Sabina's space under <see cref="EngagedMirrorStaging"/>, on the official map.</summary>
        internal static readonly HexCoordinate SabinaAt = new HexCoordinate(-3, 1);

        /// <summary>The three free playable Battle-Zone spaces adjacent to <see cref="SabinaAt"/>.</summary>
        internal static readonly HexCoordinate FirstAdjacent = new HexCoordinate(-2, 1);
        internal static readonly HexCoordinate SecondAdjacent = new HexCoordinate(-3, 0);
        internal static readonly HexCoordinate ThirdAdjacent = new HexCoordinate(-4, 1);

        /// <summary>The fourth free playable space adjacent to her, on the other side.</summary>
        internal static readonly HexCoordinate FourthAdjacent = new HexCoordinate(-2, 0);

        /// <summary>Two spaces from Sabina: inside a Range or Radius 2, outside every "adjacent".</summary>
        internal static readonly HexCoordinate TwoAway = new HexCoordinate(-1, 1);
        internal static readonly HexCoordinate SecondTwoAway = new HexCoordinate(-4, 0);

        /// <summary>Three spaces from Sabina: inside a Radius 3, outside a Radius 2.</summary>
        internal static readonly HexCoordinate ThreeAway = new HexCoordinate(-1, 2);
        internal static readonly HexCoordinate SecondThreeAway = new HexCoordinate(-6, 3);

        /// <summary>
        /// Adjacent to Sabina with the terrain space (-3, 3) directly behind it, so a push away from
        /// her has nowhere to put the pushed piece.
        /// </summary>
        internal static readonly HexCoordinate AdjacentAgainstTerrain = new HexCoordinate(-3, 2);

        /// <summary>
        /// The two pieces the engaged staging leaves standing inside Sabina's own disks: a friendly
        /// Melee minion on (-3, 2) and the enemy Ranged minion on (-4, 2), both adjacent to her.
        /// A fixture whose subject is <em>which</em> pieces a card offers has to say where these two
        /// are, or the answer includes them for a reason that has nothing to do with the card.
        /// </summary>
        internal static readonly string[] StagedBesideHer = { "blue-melee-01", "red-ranged-01" };

        /// <summary>Two free playable spaces five from Sabina, outside every disk her set prints.</summary>
        private static readonly HexCoordinate FarFromHer = new HexCoordinate(2, 0);
        private static readonly HexCoordinate SecondFarFromHer = new HexCoordinate(2, 1);

        private readonly List<MinionId> standingBeforeTheAction = new List<MinionId>();
        private readonly List<MinionId> removedInOrder = new List<MinionId>();

        private SabinaBoardFixture(MatchSession session, HeroId sabina, HeroId wasp)
        {
            Session = session;
            Sabina = sabina;
            Wasp = wasp;
        }

        internal MatchSession Session { get; }
        internal HeroId Sabina { get; }
        internal HeroId Wasp { get; }

        /// <summary>
        /// The configured match at the named p.15 stage, with the pieces the caller names relocated.
        /// </summary>
        /// <param name="minionPlacements">Minion id to the space it should start on.</param>
        /// <param name="heroPlacements">Hero id to the space they should start on.</param>
        /// <param name="banishedMinions">
        /// Minion ids to leave out of the setup entirely, which is how a fixture makes a side short in
        /// the Battle Zone or lets p.18's Heavy immunity lapse.
        /// </param>
        /// <param name="unlockUltimate">
        /// Whether p.16's unlock has happened. It is recorded by moving the configuration's own card
        /// instance, because the derived set reads that position and a projection hands out copies.
        /// </param>
        /// <param name="enemyHandSize">
        /// How many cards to leave the enemy seat holding, lowest-Initiative first, or null to leave
        /// the dealt hand alone. It is how a fixture reaches a hero holding nothing at the moment a
        /// card asks them to discard: p.7 spends one card on the commitment and p.14's Defense spends
        /// another, so a seat that has to commit, defend and then be unable to pay is dealt two.
        /// </param>
        /// <param name="clearHerNeighbourhood">
        /// Whether to park <see cref="StagedBesideHer"/> five spaces away, which is the default. Every
        /// fixture here is about which pieces a printed clause names, and those two stand inside her
        /// disks for a reason that belongs to the staging rather than to any card. A caller placing
        /// either of them explicitly overrides the parking for that piece.
        /// </param>
        internal static SabinaBoardFixture Create(
            int tier = 1,
            SabinaUpgradeBranch branch = SabinaUpgradeBranch.FirstSibling,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> sabinaDiscards = null,
            bool clearHerNeighbourhood = true)
        {
            var configuration = tier == 3
                ? PlaceholderOneVsOneTestConfiguration.CreateStagedSabinaTierThreeForInternalTest(
                    TeamSide.Red, branch)
                : tier == 2
                    ? PlaceholderOneVsOneTestConfiguration.CreateStagedSabinaTierTwoForInternalTest(
                        TeamSide.Red, branch)
                    : PlaceholderOneVsOneTestConfiguration.CreateStagedSabinaForInternalTest(
                        TeamSide.Red);

            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var staged = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusSabinaOneVsOne(),
                    configuration.Participants, map, configuration.TieBreakerState),
                out _);

            var sabina = staged.Heroes.Single(hero => hero.ContentId.Value == "sabina").HeroId;
            var wasp = staged.Heroes.Single(hero => hero.ContentId.Value == "wasp").HeroId;
            Assert.That(
                staged.Heroes.Single(hero => hero.HeroId.Equals(sabina)).Coordinate,
                Is.EqualTo(SabinaAt),
                "Every space a caller names is relative to Sabina's staged one, so a staging that " +
                "moved her would silently move every fixture built on this.");

            if (unlockUltimate)
            {
                Assert.That(
                    CardLifecycleAuthority.TryUnlockUltimateFromDeck(
                        configuration.CardInstances.Single(card =>
                            card.OwnerId.Equals(sabina) &&
                            card.DefinitionReference.DefinitionId.Equals(
                                SabinaCardContent.BigSoddingGun))),
                    Is.True,
                    "p.16 unlocks by moving the card out of the deck, and the card has to be there.");
            }

            if (enemyHandSize.HasValue)
            {
                foreach (var card in configuration.CardInstances
                    .Where(card =>
                        card.OwnerId.Equals(wasp) && card.State.Location == CardLocation.Hand)
                    .OrderBy(card => configuration.CardDefinitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value)
                    .Skip(enemyHandSize.Value)
                    .ToArray())
                {
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
                }
            }

            if (sabinaDiscards != null)
            {
                // A retrieve has nothing to name in a match's first turn, because nothing has been
                // discarded yet. This is p.7's own discard pile reached in one step rather than by
                // playing a round first, which would prove the round instead of the card.
                foreach (var cardId in sabinaDiscards)
                {
                    Assert.That(
                        CardLifecycleAuthority.TryDiscardFromHand(
                            configuration.CardInstances.Single(card =>
                                card.OwnerId.Equals(sabina) &&
                                card.State.Location == CardLocation.Hand &&
                                card.DefinitionReference.DefinitionId.Equals(cardId))),
                        Is.True);
                }
            }

            var placements = minionPlacements;
            if (clearHerNeighbourhood)
            {
                var cleared = new Dictionary<string, HexCoordinate>
                {
                    [StagedBesideHer[0]] = FarFromHer,
                    [StagedBesideHer[1]] = SecondFarFromHer
                };
                foreach (var placement in
                    minionPlacements ?? (IReadOnlyDictionary<string, HexCoordinate>)
                        new Dictionary<string, HexCoordinate>())
                {
                    cleared[placement.Key] = placement.Value;
                }

                placements = cleared;
            }

            var setup = Relocate(map, staged, placements, heroPlacements, banishedMinions);
            var scenario = new InitialScenarioDescription(
                configuration.Participants, configuration.CardDefinitions,
                configuration.CardInstances, configuration.TieBreakerState, map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new SabinaBoardFixture(
                new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map })),
                sabina, wasp);
        }

        /// <summary>A one-entry placement map, for the common case of moving a single piece.</summary>
        internal static IReadOnlyDictionary<string, HexCoordinate> At(
            string id, HexCoordinate coordinate) =>
            new Dictionary<string, HexCoordinate> { [id] = coordinate };

        // -------------------------------------------------------------------------------------
        // Playing a card
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Commits every seat so Sabina's named card is the one that acts, then starts its printed
        /// primary action.
        ///
        /// <para>The other seats yield with their lowest-Initiative card, which p.7 resolves last.
        /// Sabina's Green cards print Initiative 3 and 4 and can lose that race, so this drains the
        /// enemy turn rather than asserting she went first: what the fixture needs is her turn, not a
        /// particular turn order.</para>
        /// </summary>
        /// <param name="enemyCard">
        /// The card the enemy seat should commit, when the case is about what that seat played -
        /// Sabina's three conditional Red Attacks ask whether their target played an Attack card this
        /// turn, and p.10 makes the commitment the play. Left null, the enemy seat yields with its
        /// lowest-Initiative card.
        /// </param>
        internal void StartPrimary(CardDefinitionId cardId, CardDefinitionId enemyCard = default)
        {
            foreach (var hero in Session.Projection.Participants.Heroes.Select(value => value.Id))
            {
                var card = hero.Equals(Sabina)
                    ? HandCard(hero, cardId)
                    : enemyCard.IsDefined
                        ? HandCard(hero, enemyCard)
                        : LowestInitiativeHandCard(hero);
                Accept(DirectorCommand.CommitCard(hero, card.Id));
            }

            DrainUntilSabinaActs();
            var offer = Session.Projection.AvailableTurnActions.SingleOrDefault(candidate =>
                candidate.IsAvailable &&
                OriginOf(candidate) == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(offer.ActionDefinitionId.IsDefined, Is.True,
                $"'{cardId.Value}' offers no available primary action on this board.");
            standingBeforeTheAction.Clear();
            standingBeforeTheAction.AddRange(MinionIds());
            Accept(DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        /// <summary>
        /// Whether the named card's printed primary action is offered as available once it is the one
        /// Sabina committed. Milestone 7.8a1's rule greys an action with nothing to act on, so a face
        /// whose text names nobody on this board is unavailable rather than startable-and-empty.
        /// </summary>
        internal bool IsPrimaryAvailable(CardDefinitionId cardId)
        {
            foreach (var hero in Session.Projection.Participants.Heroes.Select(value => value.Id))
            {
                var card = hero.Equals(Sabina)
                    ? HandCard(hero, cardId)
                    : LowestInitiativeHandCard(hero);
                Accept(DirectorCommand.CommitCard(hero, card.Id));
            }

            DrainUntilSabinaActs();
            return Session.Projection.AvailableTurnActions.Any(candidate =>
                candidate.IsAvailable &&
                OriginOf(candidate) == ActionDefinitionOrigin.PrintedPrimary);
        }

        /// <summary>Starts the named Attack and answers p.14's target question with a minion.</summary>
        internal void PlayAttackAt(CardDefinitionId cardId, MinionId target)
        {
            StartPrimary(cardId);
            var choice = Session.Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "An Attack opens p.14's target question.");
            Assert.That(choice.LegalTargetMinionIds, Has.Member(target),
                $"'{target.Value}' is the piece this board was built around and the Attack has to " +
                "be able to reach it, or the case proves something about range instead.");
            Accept(DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, target));
        }

        /// <summary>
        /// Starts the named Attack and answers p.14's target question with the enemy hero, then -
        /// unless the caller is reading the preview instead - answers the Defense question with a
        /// card that keeps the hero on the board.
        /// </summary>
        internal void PlayAttackAt(
            CardDefinitionId cardId, HeroId target, bool defendSuccessfully = true,
            CardDefinitionId enemyCard = default)
        {
            StartPrimary(cardId, enemyCard);
            var choice = Session.Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "An Attack opens p.14's target question.");
            Assert.That(choice.LegalTargetHeroIds, Has.Member(target),
                "The enemy hero has to be inside the Attack's range on this board.");
            Accept(DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, target));
            if (defendSuccessfully)
            {
                DefendSuccessfully();
            }
        }

        /// <summary>
        /// Discards the card that keeps the attacked hero standing. p.13 applies an Attack's "After
        /// the attack:" text "after the target either defends, or is defeated and removed", so a
        /// defended target is the one a rider about the target can still be asked about.
        /// </summary>
        internal void DefendSuccessfully()
        {
            var preview = Session.Projection.PendingDefensePreview;
            Assert.That(preview, Is.Not.Null, "A hero target opens p.14's Defense question.");
            var survives = preview.Choices.FirstOrDefault(choice =>
                !choice.IsDecline &&
                choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended);
            Assert.That(survives, Is.Not.Null,
                "No card in the defending hand answers this Attack Total, so the board would prove " +
                "the Defeat arm instead of whatever the case is about.");
            Accept(DirectorCommand.ChooseDefenseCard(survives.CardInstanceId.Value));
        }

        // -------------------------------------------------------------------------------------
        // Answering what a card asks
        // -------------------------------------------------------------------------------------

        internal PendingEffectChoice Choice => Session.Projection.PendingEffectChoice;

        /// <summary>Names each subject in turn, then closes the selection if it offers a confirm.</summary>
        internal void SelectAndConfirm(params MinionId[] minions)
        {
            foreach (var minion in minions)
            {
                Select(minion);
            }

            Confirm();
        }

        internal void Select(MinionId minion)
        {
            var choice = Choice;
            Assert.That(choice, Is.Not.Null,
                $"The selection closed before '{minion.Value}' could be named.");
            var invocation = choice.SelectInvocations.SingleOrDefault(candidate =>
                candidate.Candidate.Value.Kind == EffectValueKind.Minion &&
                candidate.Candidate.Value.MinionId.Equals(minion));
            Assert.That(invocation, Is.Not.Null,
                $"'{minion.Value}' is not among the candidates this selection is offering.");
            Accept(DirectorCommand.AnswerEffectChoice(invocation));
        }

        internal void Select(HexCoordinate space)
        {
            var choice = Choice;
            Assert.That(choice, Is.Not.Null, $"No selection is standing to name {space} in.");
            var invocation = choice.SelectInvocations.SingleOrDefault(candidate =>
                candidate.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                candidate.Candidate.Value.Coordinate.Equals(space));
            Assert.That(invocation, Is.Not.Null,
                $"{space} is not among the destinations this selection is offering.");
            Accept(DirectorCommand.AnswerEffectChoice(invocation));
        }

        internal void SelectFirst()
        {
            var choice = Choice;
            Assert.That(choice?.SelectInvocations, Is.Not.Null.And.Not.Empty,
                "No selection is standing with anything to name.");
            Accept(DirectorCommand.AnswerEffectChoice(choice.SelectInvocations[0]));
        }

        /// <summary>Closes whatever selection is standing, if it offers a confirm.</summary>
        internal void Confirm()
        {
            var open = Choice;
            if (open?.ConfirmInvocation != null)
            {
                Accept(DirectorCommand.AnswerEffectChoice(open.ConfirmInvocation));
            }

            RecordRemovals();
        }

        /// <summary>Declines whatever optional step is standing.</summary>
        internal void Decline()
        {
            var open = Choice;
            Assert.That(open?.DeclineInvocation, Is.Not.Null,
                "This step offers no decline, so the card is not printing a `may`.");
            Accept(DirectorCommand.AnswerEffectChoice(open.DeclineInvocation));
            RecordRemovals();
        }

        /// <summary>Answers whatever stands, taking the first offer at every step.</summary>
        internal void AnswerEveryStandingChoice(int budget = 12)
        {
            for (var step = 0; step < budget; step++)
            {
                var choice = Choice;
                if (choice == null)
                {
                    RecordRemovals();
                    return;
                }

                var invocation = choice.SelectInvocations.FirstOrDefault()
                    ?? choice.ConfirmInvocation ?? choice.DeclineInvocation ?? choice.StopInvocation;
                Assert.That(invocation, Is.Not.Null,
                    "A pause with no answer would stop the match, which no printed face does.");
                Accept(DirectorCommand.AnswerEffectChoice(invocation));
            }

            Assert.Fail("The card asked more questions than any printed face of hers prints.");
        }

        // -------------------------------------------------------------------------------------
        // Reading the board
        // -------------------------------------------------------------------------------------

        internal MinionId MinionAt(HexCoordinate coordinate) =>
            Session.Projection.Minions.Single(minion => minion.Coordinate.Equals(coordinate)).MinionId;

        internal IReadOnlyList<MinionId> MinionIds() =>
            Session.Projection.Minions.Select(minion => minion.MinionId).ToArray();

        internal HexCoordinate SpaceOf(MinionId minionId) =>
            Session.Projection.Minions.Single(minion => minion.MinionId.Equals(minionId)).Coordinate;

        internal TeamSide SideOf(MinionId minionId) =>
            Session.Projection.Minions.Single(minion => minion.MinionId.Equals(minionId)).TeamSide;

        internal HexCoordinate HeroSpace(HeroId heroId) =>
            Session.Projection.Heroes.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        internal bool IsOnBoard(HeroId heroId) =>
            Session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(heroId));

        internal int Coins(HeroId heroId) =>
            Session.Projection.HeroProgression
                .Single(progression => progression.HeroId.Equals(heroId)).Coins;

        internal int Lives(TeamSide side) =>
            Session.Projection.TeamLifeCounters
                .Single(entry => Session.Projection.Participants.Teams
                    .Single(team => team.Id.Equals(entry.Key)).Side == side)
                .Value;

        internal IReadOnlyList<CardDefinitionId> DiscardPile(HeroId heroId) =>
            Session.Projection.Cards
                .Where(card =>
                    card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();

        internal IReadOnlyList<CardDefinitionId> Hand(HeroId heroId) =>
            Session.Projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();

        internal int AttackTotal() => Session.Projection.PendingDefensePreview.AttackTotal;

        /// <summary>The minions this fixture watched leave the board during the action.</summary>
        internal IReadOnlyList<MinionId> LeftTheBoard() => removedInOrder;

        /// <summary>
        /// The sequence number of the rule event recording that this effect node applied. Two of them
        /// compared is what "applied in the order the card composes them" means as something a fixture
        /// can read - the removals themselves name no piece in the log, so the nodes that read the
        /// selection's first and second published spaces are what carry the order.
        /// </summary>
        /// <summary>
        /// Spends Sabina's printed secondary Movement walking her as close to the named space as the
        /// allowance reaches. p.7 gives a turn one action, so a fixture that needs her somewhere else
        /// spends a whole turn getting there - which is what a player does.
        /// </summary>
        internal void WalkTowards(HexCoordinate destination)
        {
            var offer = Session.Projection.AvailableTurnActions.FirstOrDefault(candidate =>
                candidate.IsAvailable && KindOf(candidate) == ActionKind.Movement);
            if (!offer.ActionDefinitionId.IsDefined)
            {
                return;
            }

            Accept(DirectorCommand.StartAction(offer.ActionDefinitionId));
            for (var budget = 0; budget < 8; budget++)
            {
                var movement = Session.Projection.PendingMovementChoice;
                if (movement == null)
                {
                    return;
                }

                var here = HeroSpace(Sabina);
                var step = movement.LegalDestinations
                    .OrderBy(value => HexDistanceQuery.Calculate(value.Destination, destination))
                    .FirstOrDefault();
                if (here.Equals(destination) ||
                    step.Destination.Equals(default(HexCoordinate)) ||
                    HexDistanceQuery.Calculate(step.Destination, destination) >=
                        HexDistanceQuery.Calculate(here, destination))
                {
                    if (movement.CanStop)
                    {
                        Accept(DirectorCommand.StopMovement(movement.ResolutionId, movement.ChoiceId));
                    }

                    return;
                }

                Accept(DirectorCommand.ChooseMovementDestination(
                    movement.ResolutionId, movement.ChoiceId, step.Destination));
            }
        }

        private ActionKind KindOf(ActionOffer offer) =>
            Session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind;

        /// <summary>The distinct rule-event types this match has logged, oldest first.</summary>
        internal IReadOnlyList<string> RuleEventTypes() =>
            Session.Projection.DiagnosticEvents
                .Select(entry => entry.EventType)
                .Distinct()
                .ToArray();

        internal int SequenceOfAppliedNode(string nodeId)
        {
            var sequence = Session.Projection.DiagnosticEvents
                .Where(entry => entry.Detail.Contains(":" + nodeId + ":Applied"))
                .Select(entry => entry.Sequence)
                .DefaultIfEmpty(0)
                .Max();
            Assert.That(sequence, Is.GreaterThan(0),
                $"No rule event records node '{nodeId}' applying, so it never ran.");
            return sequence;
        }

        internal MinionId[] CandidateMinions(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                .Select(candidate => candidate.Value.MinionId)
                .ToArray();

        internal HexCoordinate[] CandidateSpaces(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();

        internal HeroId[] CandidateHeroes(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                .Select(candidate => candidate.Value.HeroId)
                .ToArray();

        internal void Accept(DirectorCommand command)
        {
            var result = Session.Execute(command);
            Assert.That(result.Accepted, Is.True,
                $"'{command.Kind}' was refused: {result.RejectionReason}.");
        }

        // -------------------------------------------------------------------------------------
        // Internals
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Plays out any earlier seat's turn until Sabina's own starts. Her Green cards print the
        /// lowest Initiative in the match, so they can act second.
        /// </summary>
        private void DrainUntilSabinaActs()
        {
            for (var budget = 0; budget < 16; budget++)
            {
                var projection = Session.Projection;
                if (projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(Sabina))
                {
                    return;
                }

                if (projection.PendingEffectChoice != null)
                {
                    AnswerEveryStandingChoice(6);
                    continue;
                }

                if (projection.PendingAttackTargetChoice != null)
                {
                    var attack = projection.PendingAttackTargetChoice;
                    Accept(DirectorCommand.ChooseAttackTarget(
                        attack.ResolutionId, attack.ChoiceId,
                        attack.LegalTargetMinionIds.First()));
                    continue;
                }

                if (projection.PendingDefenseChoice != null)
                {
                    // Sabina has to survive the earlier seat's turn for her own to be worth
                    // reaching, so this pays for a Defense whenever one of her cards answers.
                    var preview = projection.PendingDefensePreview;
                    var survives = preview?.Choices.FirstOrDefault(choice =>
                        !choice.IsDecline &&
                        choice.Outcome == AttackResolutionOutcome.SuccessfullyDefended);
                    Accept(survives == null
                        ? DirectorCommand.DeclineDefense()
                        : DirectorCommand.ChooseDefenseCard(survives.CardInstanceId.Value));
                    continue;
                }

                var movement = projection.PendingMovementChoice;
                if (movement != null && movement.CanStop)
                {
                    Accept(DirectorCommand.StopMovement(movement.ResolutionId, movement.ChoiceId));
                    continue;
                }

                Accept(DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
            }

            Assert.Fail("Sabina's turn never started.");
        }

        private void RecordRemovals()
        {
            var standing = new HashSet<MinionId>(MinionIds());
            removedInOrder.Clear();
            removedInOrder.AddRange(
                standingBeforeTheAction.Where(minion => !standing.Contains(minion)));
        }

        private ActionDefinitionOrigin OriginOf(ActionOffer offer) =>
            Session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Origin;

        private CardInstance HandCard(HeroId ownerId, CardDefinitionId cardId) =>
            Session.Projection.Cards.SingleOrDefault(card =>
                card.OwnerId.Equals(ownerId) &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId))
            ?? throw new InvalidOperationException(
                $"'{ownerId.Value}' is not holding '{cardId.Value}'.");

        private CardInstance LowestInitiativeHandCard(HeroId ownerId) =>
            Session.Projection.Cards
                .Where(card =>
                    card.OwnerId.Equals(ownerId) && card.State.Location == CardLocation.Hand)
                .OrderBy(card => Session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();

        /// <summary>
        /// Moves the pieces the caller names and leaves every other one where the configured setup
        /// put it, refusing a placement the board itself would refuse.
        /// </summary>
        private static QuickGameSetupState Relocate(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements,
            IReadOnlyCollection<string> banishedMinions)
        {
            var minionsAt = minionPlacements ?? new Dictionary<string, HexCoordinate>();
            var banished = banishedMinions ?? Array.Empty<string>();
            var heroes = setup.Heroes.ToDictionary(hero => hero.HeroId, hero => hero.Coordinate);
            if (heroPlacements != null)
            {
                foreach (var placement in heroPlacements)
                {
                    var heroId = heroes.Keys.Single(hero => hero.Value == placement.Key);
                    heroes[heroId] = placement.Value;
                }
            }

            var minions = setup.Minions
                .Where(minion => !banished.Contains(minion.MinionId.Value))
                .Select(minion => minionsAt.TryGetValue(minion.MinionId.Value, out var moved)
                    ? Placed(map, minion, moved)
                    : minion)
                .ToArray();

            Assert.That(
                minionsAt.Keys.All(id =>
                    banished.Contains(id) ||
                    minions.Any(minion => minion.MinionId.Value == id)),
                Is.True,
                "A placement named a minion this setup neither deals nor banishes, which would put " +
                "the piece nowhere rather than where the fixture meant.");

            var occupied = new HashSet<HexCoordinate>();
            foreach (var coordinate in minions.Select(minion => minion.Coordinate)
                .Concat(heroes.Values))
            {
                Assert.That(occupied.Add(coordinate), Is.True,
                    $"Two pieces were placed on {coordinate}, and the board refuses a scenario with " +
                    "two units on one space before any rule is asked.");
                Assert.That(
                    map.Spaces.Any(space =>
                        space.Coordinate.Equals(coordinate) &&
                        space.Kind == BoardMapSpaceKind.Playable),
                    Is.True,
                    $"{coordinate} is not a playable space on the official map.");
            }

            return new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams,
                setup.Heroes.Select(hero => Placed(map, hero, heroes[hero.HeroId])),
                minions);
        }

        private static QuickGameMinionSetupState Placed(
            BoardMapDefinition map, QuickGameMinionSetupState minion, HexCoordinate coordinate) =>
            new QuickGameMinionSetupState(
                minion.MinionId, minion.TeamId, minion.TeamSide, minion.MinionType,
                minion.SpawnPoint, SpaceAt(map, coordinate), coordinate);

        private static QuickGameHeroSetupState Placed(
            BoardMapDefinition map, QuickGameHeroSetupState hero, HexCoordinate coordinate) =>
            new QuickGameHeroSetupState(
                hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                hero.SpawnPoint, SpaceAt(map, coordinate), coordinate);

        private static ConfiguredSpaceReference SpaceAt(
            BoardMapDefinition map, HexCoordinate coordinate) =>
            new ConfiguredSpaceReference(
                map.Id, map.Spaces.Single(space => space.Coordinate.Equals(coordinate)).Id);
    }
}
