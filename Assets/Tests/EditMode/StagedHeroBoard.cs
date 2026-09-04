using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Which hero a <see cref="StagedHeroBoard"/> is staging, and how to build the one-versus-one
    /// match that stages them. Milestone 8.22.
    ///
    /// <para>Everything a hero brings to the fixture is here and everything else is shared, which is
    /// the whole point of the split: the board geometry, the commands and the readings belong to
    /// <see cref="EngagedMirrorStaging"/>'s position rather than to whoever is standing on it, so a
    /// second hero costs a spec rather than a second copy of the fixture.</para>
    ///
    /// <para>The upgrade branch is an ordinary <c>bool</c> here because each hero spells its own -
    /// <c>SabinaUpgradeBranch</c>, <c>BroganUpgradeBranch</c> - and a shared fixture cannot name all
    /// of them. Each hero's own facade takes its own enum and hands the answer over as "the second
    /// sibling or not", which is the only thing this needs to know.</para>
    /// </summary>
    internal sealed class StagedHeroSpec
    {
        internal StagedHeroSpec(
            string contentId,
            string enemyContentId,
            Func<QuickGameSetupConfiguration> setup,
            Func<int, bool, MatchConfiguration> configuration,
            CardDefinitionId ultimateCardId)
        {
            if (string.IsNullOrWhiteSpace(contentId) || string.IsNullOrWhiteSpace(enemyContentId))
            {
                throw new ArgumentException("A staged hero names both seats by content identity.");
            }

            ContentId = contentId;
            EnemyContentId = enemyContentId;
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            UltimateCardId = ultimateCardId;
        }

        /// <summary>The staged hero's content identity, which is what the seat is found by.</summary>
        internal string ContentId { get; }

        /// <summary>The other seat's, which every fixture attacks, is attacked by, or empties.</summary>
        internal string EnemyContentId { get; }

        internal Func<QuickGameSetupConfiguration> Setup { get; }

        /// <summary>The configuration for a p.15 stage and an upgrade branch.</summary>
        internal Func<int, bool, MatchConfiguration> Configuration { get; }

        /// <summary>The card p.16's unlock moves out of the deck.</summary>
        internal CardDefinitionId UltimateCardId { get; }
    }

    /// <summary>
    /// One configured one-versus-one match on the official map, with the pieces put where the printed
    /// clause under test can be asked a question, plus the handful of commands every hero's focused
    /// fixtures submit. Milestone 8.22, generalised from Sabina's before milestone 8.23.
    ///
    /// <para><b>Why the boards are built rather than staged.</b> The staged engaged position is one
    /// position, and a hero's set asks about several: one adjacent enemy minion, two of them, an
    /// immune Heavy, two friendly minions inside a Radius 2 disk, an enemy hero inside Range 1. On the
    /// staged board most of those are false, so a fixture that used it would have its questions
    /// answered by the staging instead of by the card. Each caller names the pieces it needs moved and
    /// everything else stays where the configured setup put it - the same affordance
    /// <see cref="BroganOneManArmyTests"/> takes, on the same terms: a starting position is setup
    /// content, no rule sees it, and nothing here invents card state.</para>
    ///
    /// <para><b>Why it is hero-neutral.</b> Every coordinate below is a fact about
    /// <see cref="EngagedMirrorStaging"/>'s position rather than about Sabina: the staging names
    /// neither hero, so one hero on each side is the same engagement whichever printed sets they hold.
    /// The staged hero's space is <see cref="HeroAt"/> on every board and <see cref="Create"/> asserts
    /// it, so a staging change fails loudly here instead of quietly moving every fixture built on
    /// this.</para>
    /// </summary>
    internal sealed class StagedHeroBoard
    {
        /// <summary>The staged hero's space under <see cref="EngagedMirrorStaging"/>, on the official map.</summary>
        internal static readonly HexCoordinate HeroAt = new HexCoordinate(-3, 1);

        /// <summary>The three free playable Battle-Zone spaces adjacent to <see cref="HeroAt"/>.</summary>
        internal static readonly HexCoordinate FirstAdjacent = new HexCoordinate(-2, 1);
        internal static readonly HexCoordinate SecondAdjacent = new HexCoordinate(-3, 0);
        internal static readonly HexCoordinate ThirdAdjacent = new HexCoordinate(-4, 1);

        /// <summary>The fourth free playable space adjacent to the hero, on the other side.</summary>
        internal static readonly HexCoordinate FourthAdjacent = new HexCoordinate(-2, 0);

        /// <summary>Two spaces from the hero: inside a Range or Radius 2, outside every "adjacent".</summary>
        internal static readonly HexCoordinate TwoAway = new HexCoordinate(-1, 1);
        internal static readonly HexCoordinate SecondTwoAway = new HexCoordinate(-4, 0);

        /// <summary>Three spaces from the hero: inside a Radius 3, outside a Radius 2.</summary>
        internal static readonly HexCoordinate ThreeAway = new HexCoordinate(-1, 2);
        internal static readonly HexCoordinate SecondThreeAway = new HexCoordinate(-6, 3);

        /// <summary>
        /// Adjacent to the hero with the terrain space (-3, 3) directly behind it, so a push away from
        /// them has nowhere to put the pushed piece.
        /// </summary>
        internal static readonly HexCoordinate AdjacentAgainstTerrain = new HexCoordinate(-3, 2);

        /// <summary>
        /// The two pieces the engaged staging leaves standing inside the staged hero's own disks: a friendly
        /// Melee minion on (-3, 2) and the enemy Ranged minion on (-4, 2), both adjacent to the hero.
        /// A fixture whose subject is <em>which</em> pieces a card offers has to say where these two
        /// are, or the answer includes them for a reason that has nothing to do with the card.
        /// </summary>
        internal static readonly string[] StagedBesideTheHero = { "blue-melee-01", "red-ranged-01" };

        /// <summary>Two free playable spaces five from the hero, outside every disk a printed set uses.</summary>
        private static readonly HexCoordinate FarFromTheHero = new HexCoordinate(2, 0);
        private static readonly HexCoordinate SecondFarFromTheHero = new HexCoordinate(2, 1);

        private readonly List<MinionId> standingBeforeTheAction = new List<MinionId>();
        private readonly List<MinionId> removedInOrder = new List<MinionId>();

        private StagedHeroBoard(MatchSession session, HeroId hero, HeroId enemy)
        {
            Session = session;
            Hero = hero;
            Enemy = enemy;
        }

        internal MatchSession Session { get; }
        internal HeroId Hero { get; }
        internal HeroId Enemy { get; }

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
        /// <param name="clearTheNeighbourhood">
        /// Whether to park <see cref="StagedBesideTheHero"/> five spaces away, which is the default. Every
        /// fixture here is about which pieces a printed clause names, and those two stand inside her
        /// disks for a reason that belongs to the staging rather than to any card. A caller placing
        /// either of them explicitly overrides the parking for that piece.
        /// </param>
        internal static StagedHeroBoard Create(
            StagedHeroSpec spec,
            int tier = 1,
            bool secondSibling = false,
            IReadOnlyDictionary<string, HexCoordinate> minionPlacements = null,
            IReadOnlyDictionary<string, HexCoordinate> heroPlacements = null,
            IReadOnlyCollection<string> banishedMinions = null,
            bool unlockUltimate = false,
            int? enemyHandSize = null,
            IReadOnlyCollection<CardDefinitionId> heroDiscards = null,
            bool clearTheNeighbourhood = true)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            var configuration = spec.Configuration(tier, secondSibling);

            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var staged = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    spec.Setup(),
                    configuration.Participants, map, configuration.TieBreakerState),
                out _);

            var heroSeat = staged.Heroes.Single(seat => seat.ContentId.Value == spec.ContentId);
            var hero = heroSeat.HeroId;
            var enemy = staged.Heroes
                .Single(seat => seat.ContentId.Value == spec.EnemyContentId).HeroId;
            Assert.That(
                heroSeat.Coordinate, Is.EqualTo(HeroAt),
                $"Every space a caller names is relative to the staged hero's own, so a staging " +
                $"that moved {spec.ContentId} would silently move every fixture built on this.");

            if (unlockUltimate)
            {
                Assert.That(
                    CardLifecycleAuthority.TryUnlockUltimateFromDeck(
                        configuration.CardInstances.Single(card =>
                            card.OwnerId.Equals(hero) &&
                            card.DefinitionReference.DefinitionId.Equals(spec.UltimateCardId))),
                    Is.True,
                    "p.16 unlocks by moving the card out of the deck, and the card has to be there.");
            }

            if (enemyHandSize.HasValue)
            {
                foreach (var card in configuration.CardInstances
                    .Where(card =>
                        card.OwnerId.Equals(enemy) && card.State.Location == CardLocation.Hand)
                    .OrderBy(card => configuration.CardDefinitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value)
                    .Skip(enemyHandSize.Value)
                    .ToArray())
                {
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
                }
            }

            if (heroDiscards != null)
            {
                // A retrieve has nothing to name in a match's first turn, because nothing has been
                // discarded yet. This is p.7's own discard pile reached in one step rather than by
                // playing a round first, which would prove the round instead of the card.
                foreach (var cardId in heroDiscards)
                {
                    Assert.That(
                        CardLifecycleAuthority.TryDiscardFromHand(
                            configuration.CardInstances.Single(card =>
                                card.OwnerId.Equals(hero) &&
                                card.State.Location == CardLocation.Hand &&
                                card.DefinitionReference.DefinitionId.Equals(cardId))),
                        Is.True);
                }
            }

            var placements = minionPlacements;
            if (clearTheNeighbourhood)
            {
                var cleared = new Dictionary<string, HexCoordinate>
                {
                    [StagedBesideTheHero[0]] = FarFromTheHero,
                    [StagedBesideTheHero[1]] = SecondFarFromTheHero
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
            return new StagedHeroBoard(
                new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map })),
                hero, enemy);
        }

        /// <summary>A one-entry placement map, for the common case of moving a single piece.</summary>
        internal static IReadOnlyDictionary<string, HexCoordinate> At(
            string id, HexCoordinate coordinate) =>
            new Dictionary<string, HexCoordinate> { [id] = coordinate };

        // -------------------------------------------------------------------------------------
        // Playing a card
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Commits every seat so the hero's named card is the one that acts, then starts its printed
        /// primary action.
        ///
        /// <para>The other seats yield with their lowest-Initiative card, which p.7 resolves last.
        /// A hero's lowest-Initiative faces can lose that race - Sabina's Green cards print 3 and 4, so this drains the
        /// enemy turn rather than asserting they went first: what the fixture needs is their turn, not a
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
                var card = hero.Equals(Hero)
                    ? HandCard(hero, cardId)
                    : enemyCard.IsDefined
                        ? HandCard(hero, enemyCard)
                        : LowestInitiativeHandCard(hero);
                Accept(DirectorCommand.CommitCard(hero, card.Id));
            }

            DrainUntilTheHeroActs();
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
        /// Hero committed. Milestone 7.8a1's rule greys an action with nothing to act on, so a face
        /// whose text names nobody on this board is unavailable rather than startable-and-empty.
        /// </summary>
        internal bool IsPrimaryAvailable(CardDefinitionId cardId)
        {
            foreach (var hero in Session.Projection.Participants.Heroes.Select(value => value.Id))
            {
                var card = hero.Equals(Hero)
                    ? HandCard(hero, cardId)
                    : LowestInitiativeHandCard(hero);
                Accept(DirectorCommand.CommitCard(hero, card.Id));
            }

            DrainUntilTheHeroActs();
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
        /// Spends the hero's printed secondary Movement walking them as close to the named space as the
        /// allowance reaches. p.7 gives a turn one action, so a fixture that needs them somewhere else
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

                var here = HeroSpace(Hero);
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

        /// <summary>
        /// Whether one node applied at all. Milestone 8.23: rulebook p.10 stops an action at a
        /// mandatory step it cannot complete and skips the rest, so "this node never ran" is an
        /// ordinary printed outcome rather than a fault - and <see cref="SequenceOfAppliedNode"/>
        /// asserts the opposite.
        ///
        /// <para><b>Two altitudes write a node's outcome and they spell it differently</b>, which is
        /// why both are read here. Card text applied inside rulebook p.14 records
        /// <c>step=&lt;moment&gt;:&lt;node&gt;:Applied</c> on one event type; a Skill's own effect
        /// records <c>step=&lt;node&gt;</c> and puts the outcome in the event type itself. A matcher
        /// that knew only the first answered "no" for every Skill node ever run, so an assertion that
        /// a Skill's step did NOT run was true whatever the match did - which is exactly the shape of
        /// vacuous evidence this project does not keep.</para>
        /// </summary>
        internal bool NodeApplied(string nodeId) =>
            Session.Projection.DiagnosticEvents.Any(entry => RecordsApplied(entry, nodeId));

        internal int SequenceOfAppliedNode(string nodeId)
        {
            var sequence = Session.Projection.DiagnosticEvents
                .Where(entry => RecordsApplied(entry, nodeId))
                .Select(entry => entry.Sequence)
                .DefaultIfEmpty(0)
                .Max();
            Assert.That(sequence, Is.GreaterThan(0),
                $"No rule event records node '{nodeId}' applying, so it never ran.");
            return sequence;
        }

        private static bool RecordsApplied(GuardsDigital.Core.Diagnostics.RuleEventLogEntry entry, string nodeId) =>
            entry.Detail.Contains(":" + nodeId + ":Applied") ||
            (entry.EventType.EndsWith("EffectNodeApplied", StringComparison.Ordinal) &&
                entry.Detail == "step=" + nodeId);

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

        /// <summary>
        /// Answers standing questions until one offers a set of amounts rather than pieces or spaces -
        /// which is what a transfer whose card names two amounts opens. Milestone 8.23.
        /// </summary>
        internal void AnswerUntilAmountChoice(int budget = 12)
        {
            for (var step = 0; step < budget; step++)
            {
                var choice = Choice;
                if (choice == null || choice.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Integer))
                {
                    return;
                }

                var invocation = choice.SelectInvocations.FirstOrDefault()
                    ?? choice.ConfirmInvocation ?? choice.DeclineInvocation ?? choice.StopInvocation;
                Assert.That(invocation, Is.Not.Null,
                    "A pause with no answer would stop the match, which no printed face does.");
                Accept(DirectorCommand.AnswerEffectChoice(invocation));
            }

            Assert.Fail("No amount question was reached inside the budget.");
        }

        /// <summary>
        /// The markers one hero is holding, read back through a restore for the reason milestone 6.12's
        /// cleanup cases read them that way: the attachment register is round-owned state rather than a
        /// projection field, and a restored register is the one every rule sees. Milestone 8.23.
        /// </summary>
        internal IReadOnlyList<MarkerAttachment> MarkersHeldBy(HeroId heroId) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    Session.Projection.AuthoritativeSnapshot,
                    Session.Projection.Definitions,
                    new[] { Session.Projection.SelectedMap },
                    Session.Projection.Setup.Minions)
                .Round.Markers.HeldBy(heroId)
                .ToArray();

        /// <summary>
        /// Whether one hero is currently forbidden an activity by a live rule source, and whether that
        /// hero currently counts as an additional effective type. Milestone 8.24, for the petrification
        /// sentence whose whole content is one of each.
        ///
        /// <para>Both read through a restore for the reason <see cref="ValueModifierFor"/> does: the
        /// sources are round-owned state rather than projection fields, and a restored set is the one
        /// every rule sees. They are one method because the card is one sentence - a build that ended
        /// the restriction without ending the overlay, or the other way round, is exactly the failure
        /// worth catching, and two separate readings taken at two moments could not see it.</para>
        /// </summary>
        internal (bool Restricted, bool CountsAsTerrain) PetrificationReadingFor(
            HeroId heroId, RuleActivity activity)
        {
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                Session.Projection.AuthoritativeSnapshot,
                Session.Projection.Definitions,
                new[] { Session.Projection.SelectedMap },
                Session.Projection.Setup.Minions);
            var modifications = RuleModificationQuery.Build(
                null,
                restored.Board,
                restored.Participants,
                restored.Round.TieBreakerState,
                UltimateUnlockQuery.Derive(Session.Projection.Definitions, restored.Cards),
                RuleObject.None,
                null,
                restored.Round.Markers);
            var subject = RuleObject.ForHero(heroId);
            return (
                modifications.IsRestricted(subject, activity),
                EffectiveUnitKindQuery.Is(
                    subject, EffectiveUnitKind.Terrain, modifications));
        }

        /// <summary>
        /// What one hero's value on a channel is currently modified by, summed over every live source.
        /// Milestone 8.23, for the marker whose whole rule is three of these.
        /// </summary>
        internal int ValueModifierFor(HeroId heroId, PrintedValueChannel channel)
        {
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                Session.Projection.AuthoritativeSnapshot,
                Session.Projection.Definitions,
                new[] { Session.Projection.SelectedMap },
                Session.Projection.Setup.Minions);
            return RuleModificationQuery.Build(
                    null,
                    restored.Board,
                    restored.Participants,
                    restored.Round.TieBreakerState,
                    UltimateUnlockQuery.Derive(Session.Projection.Definitions, restored.Cards),
                    RuleObject.None,
                    null,
                    restored.Round.Markers)
                .ValueContributions(
                    RuleObject.ForHero(heroId), channel, RuleValueApplicability.Always)
                .Sum(contribution => contribution.Amount);
        }


        /// <summary>
        /// Stops rulebook p.14 at the Defense response with the staged hero as its target: the enemy
        /// commits the named Attack, the hero commits the named card, and whichever seat p.7 gives the
        /// turn to first yields until the attacker is the one acting.
        ///
        /// <para><b>The card the hero commits is not the card they will answer with.</b> p.7 puts a
        /// committed card in the turn area, so a Defense the hero committed cannot be offered back to
        /// them - which is why a Block case commits something else and keeps the Block in hand.</para>
        /// </summary>
        internal void TakeTheEnemysAttack(CardDefinitionId enemyAttack, CardDefinitionId heroCommits)
        {
            Accept(DirectorCommand.CommitCard(Enemy, HandCard(Enemy, enemyAttack).Id));
            Accept(DirectorCommand.CommitCard(Hero, HandCard(Hero, heroCommits).Id));

            for (var budget = 0; budget < 4; budget++)
            {
                // Milestone 8.35g. A finished resolution waits for its own player to hand the
                // turn on, and acknowledging it is what clears the selection read just below.
                if (Session.Projection.PendingTurnRelease != null)
                {
                    Accept(DirectorCommand.ReleaseTurn());
                    continue;
                }

                var selected = Session.Projection.SelectedActor;
                Assert.That(selected.IsDefined, Is.True,
                    "Nobody is acting, so the enemy's Attack cannot be reached.");
                if (selected.ActorId.Equals(Enemy))
                {
                    break;
                }

                // p.12's Hold: the staged hero yields their own turn rather than spending it, so the
                // card they are holding is still in hand when the Attack arrives.
                Accept(DirectorCommand.PerformHold(selected.CardInstanceId));
            }

            var offer = Session.Projection.AvailableTurnActions.SingleOrDefault(candidate =>
                candidate.IsAvailable &&
                OriginOf(candidate) == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(offer.ActionDefinitionId.IsDefined, Is.True,
                $"'{enemyAttack.Value}' offers the enemy no available primary action on this board.");
            Accept(DirectorCommand.StartAction(offer.ActionDefinitionId));

            var choice = Session.Projection.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "An Attack opens p.14's target question.");
            Assert.That(choice.LegalTargetHeroIds, Has.Member(Hero),
                "The staged hero has to be inside the enemy Attack's reach, or the case proves " +
                "something about range instead.");
            Accept(DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, Hero));
        }

        /// <summary>Answers p.14's Defense response with the named card out of the hero's own hand.</summary>
        internal void DefendWith(CardDefinitionId cardId)
        {
            var choice = Session.Projection.PendingDefenseChoice;
            Assert.That(choice, Is.Not.Null, "No Defense response is standing to answer.");
            Accept(DirectorCommand.ChooseDefenseCard(HandCard(Hero, cardId).Id));
        }

        /// <summary>
        /// Captures the whole match, reads it back into this same session, and captures again - so a
        /// payload that cannot be read is a failure here rather than an exception at the next rollback.
        /// </summary>
        internal void RoundTripThroughASnapshot()
        {
            var captured = Session.CaptureSnapshot();
            Assert.That(Session.RestoreSnapshot(captured).Accepted, Is.True,
                "The captured payload has to be readable by the build that wrote it.");
            Assert.That(Session.CaptureSnapshot(), Is.EqualTo(captured),
                "and reading it back has to produce the same state it was written from.");
        }
        /// <summary>
        /// Ends every remaining turn and drives the round through its own boundary, so a caller can
        /// reach what rulebook p.8 does at the end of a round. Milestone 8.23a, for the marker whose
        /// whole lifetime ends at "Remove Tokens, Return Markers".
        /// </summary>
        /// <summary>
        /// Turns held until the staged hero's own next turn begins - which is what rulebook p.19's
        /// <c>Next turn:</c> waits for, and the reason crossing the round boundary is the wrong way to
        /// reach it. Milestone 8.24.
        ///
        /// <para>A Next Turn effect becomes applicable at its controller's next <c>TurnStarted</c> and
        /// is cancelled by p.8's end-of-round card retrieval if that turn never comes. So a fixture
        /// that ends the round to "advance time" reads the effect's <em>cancellation</em> and calls it
        /// an absence. This walks the intervening turns instead, holding each one, and stops the
        /// moment the staged hero is the actor again.</para>
        /// </summary>
        internal void HoldUntilHerNextTurn(int budget = 8)
        {
            for (var step = 0; step < budget; step++)
            {
                if (Session.Projection.Phase != RoundResolutionPhase.ResolvingTurn)
                {
                    Assert.Fail(
                        "The round left its turn phase before the staged hero's next turn began, so " +
                        "p.19's Next Turn window was never reached.");
                }

                // Milestone 8.35g. The release, before the selection this reads is asked for.
                if (Session.Projection.PendingTurnRelease != null)
                {
                    Accept(DirectorCommand.ReleaseTurn());
                    continue;
                }

                var actor = Session.Projection.SelectedActor;
                Assert.That(actor.CardInstanceId.IsDefined, Is.True,
                    "A turn with no selected actor cannot be held on.");
                Accept(DirectorCommand.PerformHold(actor.CardInstanceId));

                if (Session.Projection.PendingTurnRelease != null)
                {
                    Accept(DirectorCommand.ReleaseTurn());
                }

                var next = Session.Projection.SelectedActor;
                if (Session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                    next.ActorId.Equals(Hero))
                {
                    return;
                }
            }

            Assert.Fail("The staged hero's next turn did not begin inside the budget.");
        }

        /// <summary>
        /// The round crossed while answering whatever stops it on the way. Milestone 8.24.
        ///
        /// <para><see cref="CrossTheRoundBoundary"/> assumes nothing interrupts p.8, which was true of
        /// every hero before Xargatha. Her Constrict and Final Embrace are the first verified content
        /// to declare milestone 8.19c1's pre-Minion-Battle source, and that source stops the round on
        /// her own decision - so a fixture that drove p.8's stage list straight through would submit
        /// its next command into a paused round and be refused. This answers each pause where it
        /// stands and then goes on, which is what a player does.</para>
        /// </summary>
        /// <param name="stopWhen">
        /// An optional condition that ends the drive early. It exists for the end-of-round sources
        /// whose subject is <em>when</em> something happened rather than that the round finished:
        /// Constrict's Defeat lands at p.8's pre-battle moment, and a caller that only checked after
        /// the whole page had run could not tell that moment from any later one. Stopping the instant
        /// the board changes is the stronger claim, and it is also the only one available when the
        /// Defeat leaves p.8 in a state this driver has no command for.
        /// </param>
        /// <param name="budget">
        /// How many drive steps the crossing may spend. Raised from 24 at milestone 8.35g: a finished
        /// resolution now waits for its own player, so every turn this drives costs one command more
        /// than it did, and the old bound stopped mid-round rather than at a stall.
        /// </param>
        internal void CrossTheRoundBoundaryAnsweringPauses(
            int budget = 40, Func<bool> stopWhen = null)
        {
            var startedIn = Session.Projection.RoundIndex;
            for (var step = 0; step < budget && Session.Projection.RoundIndex == startedIn; step++)
            {
                if (stopWhen != null && stopWhen())
                {
                    return;
                }

                if (Choice != null)
                {
                    AnswerEveryStandingChoice();
                    continue;
                }

                if (Session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    // Milestone 8.35g. The release, before the selection this reads is asked for.
                    if (Session.Projection.PendingTurnRelease != null)
                    {
                        Accept(DirectorCommand.ReleaseTurn());
                        continue;
                    }

                    var actor = Session.Projection.SelectedActor;
                    Assert.That(actor.CardInstanceId.IsDefined, Is.True,
                        "A turn with no selected actor cannot be held on.");
                    Accept(DirectorCommand.PerformHold(actor.CardInstanceId));
                    continue;
                }

                // p.7's commitment step comes round again inside the same round index, and a fixture
                // that only advanced end-of-round stages would spin here forever.
                if (Session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    ConfiguredMatchFixtures.CommitBothHands(Session);
                    continue;
                }

                // p.8's Minion Battle asks its own question, and a hero who has just defeated a minion
                // before the battle is precisely the hero who makes it ask: the Battle Zone she left
                // behind is the one being counted. It is not an effect choice and has its own
                // commands, which is why the pause check above walks past it.
                var battle = Session.Projection.PendingMinionBattleChoice;
                if (battle != null)
                {
                    while (Session.Projection.PendingMinionBattleChoice.SelectInvocations.Count > 0)
                    {
                        Accept(DirectorCommand.SelectMinionBattleRemoval(
                            Session.Projection.PendingMinionBattleChoice.SelectInvocations[0]));
                    }

                    Accept(DirectorCommand.ConfirmMinionBattleRemovals(
                        Session.Projection.PendingMinionBattleChoice.ConfirmInvocation));
                    continue;
                }

                // ONE stage per iteration, and a refusal is not a failure here. A round stopped by a
                // pausing source refuses its next stage until somebody answers, and which of the two
                // states the round is in is exactly what this loop is discovering - so it offers the
                // stage, and comes back round to the pause check when the offer is declined.
                // ConfiguredMatchFixtures.FinishEndOfRound cannot be used for this: its inner loop
                // advances every stage without ever looking for a pause between them.
                if (!Session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted &&
                    Choice == null)
                {
                    Assert.Fail(
                        "The round refused its next end-of-round stage with no effect choice " +
                        "standing, so nothing this driver knows how to answer would move it on. " +
                        $"Phase={Session.Projection.Phase}, " +
                        $"IsTurnActive={Session.Projection.IsTurnActive}, " +
                        $"PendingSkill={Session.Projection.PendingSkillResolution != null}, " +
                        $"PendingMinionBattle={Session.Projection.PendingMinionBattleChoice != null}, " +
                        $"PendingLanePush={Session.Projection.PendingLanePushRequest != null}, " +
                        $"PendingRespawn={Session.Projection.PendingHeroRespawnChoice != null}");
                }
            }

            if (stopWhen != null && stopWhen())
            {
                return;
            }

            Assert.That(Session.Projection.RoundIndex, Is.GreaterThan(startedIn),
                "The round has to actually end for p.8's own stages to have run; a caller that " +
                "stopped short would read an absence this never caused.");
        }

        internal void CrossTheRoundBoundary()
        {
            var startedIn = Session.Projection.RoundIndex;

            // Every remaining turn of this round, then the whole of p.8's stage list. Both halves are
            // ConfiguredMatchFixtures' own, because a second implementation of "play out a round"
            // that drifted from theirs would cross a different boundary than the rest of the suite
            // measures against.
            ConfiguredMatchFixtures.PlayOutTheRestOfTheRound(Session);
            ConfiguredMatchFixtures.FinishEndOfRound(Session);

            Assert.That(Session.Projection.RoundIndex, Is.GreaterThan(startedIn),
                "The round has to actually end for p.8's Return Markers to have run; a caller that " +
                "stopped short would read an absence this never caused.");
        }

        internal void Accept(DirectorCommand command)
        {
            var result = Session.Execute(command);
            Assert.That(result.Accepted, Is.True,
                $"'{command.Kind}' was refused: {result.RejectionReason}. {result.Detail}");
        }

        // -------------------------------------------------------------------------------------
        // Internals
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Plays out any earlier seat's turn until the staged hero's own starts. The lowest-Initiative faces print the
        /// lowest Initiative in the match, so they can act second.
        /// </summary>
        private void DrainUntilTheHeroActs()
        {
            for (var budget = 0; budget < 16; budget++)
            {
                var projection = Session.Projection;
                if (projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(Hero))
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
                    // The staged hero has to survive the earlier seat's turn for their own to be worth
                    // reaching, so this pays for a Defense whenever one of their cards answers.
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

                // Milestone 8.35g. The release, before the selection this reads is asked for.
                if (projection.PendingTurnRelease != null)
                {
                    Accept(DirectorCommand.ReleaseTurn());
                    continue;
                }

                Accept(DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
            }

            Assert.Fail("The staged hero's turn never started.");
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
