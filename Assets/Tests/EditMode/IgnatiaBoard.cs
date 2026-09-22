using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Ignatia the Mad on a hand-built board - milestone 8.75c, the completion block's shared staging.
    ///
    /// <para><b>Why a hand-built board rather than <c>StagedHeroBoard</c>.</b> Every other hero's
    /// behaviour fixtures reach a CONFIGURED match through a staged quick-game, and a configured match
    /// cannot deal a hero this build does not register - milestone 7.9's gate, which her dealing block
    /// flips and this one deliberately does not. So the completion block proves her seventeen composed
    /// faces the way Brynn's, Min's and Hanu's composition blocks proved theirs: one board, one effect
    /// frame, and the catalog her dealing WILL register, concatenated here.</para>
    ///
    /// <para><b>The coin is a parameter of the frame, not of the board.</b> Her whole set branches on
    /// <c>ignatia-coin-sun@1</c>, which reads
    /// <see cref="EffectResolutionContext.TieBreakerRevealedSide"/>; every case that means anything
    /// about her runs the SAME board under both faces, so the difference is the coin and nothing
    /// else.</para>
    /// </summary>
    internal static class IgnatiaBoard
    {
        internal static readonly HeroId Ignatia = new HeroId("ignatia-the-mad");
        internal static readonly HeroId Ally = new HeroId("ignatia-ally");
        internal static readonly HeroId Enemy = new HeroId("ignatia-enemy");
        internal static readonly HeroId SecondEnemy = new HeroId("ignatia-second-enemy");

        internal static readonly MinionId EnemyMinion = new MinionId("ignatia-enemy-minion");
        internal static readonly MinionId SecondEnemyMinion =
            new MinionId("ignatia-second-enemy-minion");
        internal static readonly MinionId FriendlyMinion = new MinionId("ignatia-friendly-minion");

        internal static readonly TeamId BlueTeam = new TeamId("ignatia-blue-team");
        internal static readonly TeamId RedTeam = new TeamId("ignatia-red-team");

        internal static readonly MapId Map = new MapId("ignatia-probe-map");

        /// <summary>Where she stands in every case, so every distance below is read off the origin.</summary>
        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        /// <summary>
        /// The card instance her frames resolve off. One identity for the whole fixture family, because
        /// a frame runs one card at a time.
        /// </summary>
        internal static readonly CardInstanceId FaceInstance = new CardInstanceId("ignatia-face");

        /// <summary>
        /// The sun face of the Tie Breaker coin, and the moon - USER-SETTLED 2026-09-22, read from her
        /// own content so a fixture and the predicate can never disagree about which is which.
        /// </summary>
        internal static TeamSide Sun => IgnatiaComponentContent.SunFace;

        internal static TeamSide Moon => IgnatiaComponentContent.MoonFace;

        /// <summary>
        /// Four seats: Ignatia and one ally on Blue, two enemies on Red. Two enemies because "a
        /// different hero" and "adjacent to an enemy hero" both need a second one, and an ally because
        /// every sun face of her Blue push line and her Green swap line names a FRIENDLY unit - and
        /// p.17 never counts herself as one.
        /// </summary>
        internal static MatchParticipants Participants()
        {
            var ignatiaPlayer = new PlayerId("ignatia-player");
            var allyPlayer = new PlayerId("ignatia-ally-player");
            var enemyPlayer = new PlayerId("ignatia-enemy-player");
            var secondEnemyPlayer = new PlayerId("ignatia-second-enemy-player");

            return new MatchParticipants(
                new[] { new Team(BlueTeam, TeamSide.Blue), new Team(RedTeam, TeamSide.Red) },
                new[]
                {
                    new Player(ignatiaPlayer, BlueTeam),
                    new Player(allyPlayer, BlueTeam),
                    new Player(enemyPlayer, RedTeam),
                    new Player(secondEnemyPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Ignatia, ignatiaPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, allyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(SecondEnemy, secondEnemyPlayer, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>
        /// The catalog her dealing will register: this build's whole synthetic vocabulary plus the TEN
        /// contracts milestone 7.9's gate still withholds - the coin predicate and Equilibrium's marker,
        /// the four card-text Attack subjects, the two discard narrowings and the two magma placements.
        ///
        /// <para>Naming the synthetic vocabulary through <c>CreateCatalog</c> rather than concatenating
        /// the build's registered list is milestone 8.40j's BA-18: handing a registered handler back in
        /// as content is refused for a duplicate reference, and none of the ten below is registered.</para>
        /// </summary>
        internal static EffectCapabilityCatalog Capabilities() =>
            SyntheticEffectCapabilities.CreateCatalog(
                IgnatiaComponentContent.RegisteredHandlers());

        /// <summary>Her one token definition - the magma supply of three, and nothing else.</summary>
        internal static ComponentDefinitionCatalog Components() =>
            new ComponentDefinitionCatalog(
                IgnatiaMagmaContent.MagmaTokens(), Array.Empty<MarkerDefinition>());

        internal static CardDefinition Face(CardDefinitionId cardId) =>
            IgnatiaCardContent.Definition(cardId);

        /// <summary>The printed primary of a face, which is the only action of hers that carries text.</summary>
        internal static ActionDefinition Primary(CardDefinitionId cardId) =>
            Face(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        internal static EffectDefinition PrimaryText(CardDefinitionId cardId)
        {
            var body = Primary(cardId).EffectRoot;
            Assert.That(body, Is.Not.Null,
                cardId.Value + " composes a printed body; a null one means the face has been " +
                "re-parked and every case about it is about a card that no longer exists.");
            return body;
        }

        /// <summary>
        /// A disk of <paramref name="radius"/> around the origin with the named units on it. Terrain is
        /// taken OUT of the playable set (p.19's "any board space without a full grid outline"), while
        /// an obstacle is a piece standing on a playable one - the two ways a space stops a Move.
        /// </summary>
        internal static AdjacentMoveAuthority Board(
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<(MinionId Id, TeamSide Side, HexCoordinate At)> minions = null,
            IEnumerable<HexCoordinate> terrain = null,
            IEnumerable<HexCoordinate> obstacles = null,
            int radius = 7)
        {
            var terrainSpaces = (terrain ?? Array.Empty<HexCoordinate>()).ToArray();
            var playable = Disk(radius).Where(space => !terrainSpaces.Contains(space)).ToArray();

            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, playable, terrainSpaces),
                heroes.ToArray(),
                (obstacles ?? Array.Empty<HexCoordinate>())
                    .Select((at, index) => new ObstacleOccupancy(
                        new ObstacleId($"ignatia-obstacle-{index:00}"), at))
                    .ToArray(),
                (minions ?? Array.Empty<(MinionId, TeamSide, HexCoordinate)>())
                    .Select(minion => new QuickGameMinionSetupState(
                        minion.Id,
                        minion.Side == TeamSide.Blue ? BlueTeam : RedTeam,
                        minion.Side,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("ignatia-spawn")),
                        new ConfiguredSpaceReference(Map, SpaceIdOf(minion.At)),
                        minion.At))
                    .ToArray());
        }

        internal static HeroOccupancy At(HeroId hero, HexCoordinate coordinate) =>
            new HeroOccupancy(hero, coordinate);

        internal static (MinionId, TeamSide, HexCoordinate) RedMinion(
            MinionId id, HexCoordinate at) => (id, TeamSide.Red, at);

        internal static (MinionId, TeamSide, HexCoordinate) BlueMinion(
            MinionId id, HexCoordinate at) => (id, TeamSide.Blue, at);

        /// <summary>
        /// One token standing on <paramref name="coordinate"/>. Her own magma by default, or any other
        /// definition a caller names - which is what ID-Q8c's "a token" needs measuring against, since
        /// the printed word carries no qualifier and a foreign token has to satisfy it too.
        /// </summary>
        internal static PlacedToken PlaceToken(
            AdjacentMoveAuthority board,
            HexCoordinate coordinate,
            TokenDefinition definition = null,
            HeroId owner = default)
        {
            var controller = owner.IsDefined ? owner : Ignatia;
            var token = new PlacedToken(
                new TokenInstanceId(
                    $"ignatia-fixture-token-{coordinate.Q}-{coordinate.R}"),
                definition ?? IgnatiaMagmaContent.MagmaTokenDefinition,
                controller,
                controller,
                new CardInstanceId("ignatia-fixture-token-source"),
                coordinate);
            board.CommitValidatedTokenPlacement(token);
            return token;
        }

        /// <summary>
        /// A token definition that is NOT hers - an ordinary one-supply obstacle token, for the case
        /// that measures whether "a token" means any token or only magma.
        /// </summary>
        internal static TokenDefinition ForeignTokenDefinition { get; } = new TokenDefinition(
            new TokenDefinitionId("ignatia-fixture-foreign-token"),
            1,
            "TEST - foreign token",
            1,
            Array.Empty<TokenPropertyDefinition>(),
            TokenRemovalCondition.EndOfRoundCleanup,
            "ignatia-fixture-foreign-tokens");

        internal static SpaceId SpaceIdOf(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

        internal static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius)
                    {
                        yield return new HexCoordinate(q, r);
                    }
                }
            }
        }

        /// <summary>A space at <paramref name="distance"/> along one of p.19's six files from the origin.</summary>
        internal static HexCoordinate Along(HexDirection direction, int distance) =>
            StraightLineQuery.Step(Origin, direction, distance);

        /// <summary>p.16's item on one channel, which is what makes a printed icon's TOTAL testable.</summary>
        internal static HeroItemState Items(
            MatchParticipants participants, PrintedValueChannel channel, int amount)
        {
            if (amount <= 0)
            {
                return new HeroItemState(participants);
            }

            return HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
            {
                new HeroItem(
                    Ignatia,
                    new CardInstanceId("ignatia-item"),
                    HeroDashboard.RulebookLayout.Slots[3].Position,
                    channel,
                    amount)
            }));
        }

        /// <summary>
        /// A live set carrying ONE immunity, built the way a match builds it - from an Active effect in
        /// its active phase - rather than hand-assembled, so what the Attack obeys is p.18's own rule.
        /// </summary>
        internal static RuleModificationSet ImmunityFor(
            MatchParticipants participants, HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"ignatia-fixture-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"IgnatiaFixture{activity}Immunity",
                $"ignatia-fixture-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"ignatia-fixture-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Red,
                default,
                new CardInstanceId("ignatia-fixture-immunity-source"),
                new ActionDefinitionId("ignatia-fixture-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Blue));

            var set = RuleModificationQuery.Build(
                timing, null, participants, new TieBreakerState(TeamSide.Blue),
                UnlockedUltimateSet.None, RuleObject.None, null, null);
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Ignatia)),
                Is.True,
                "The fixture must actually hold the immunity, or the case using it proves nothing.");
            return set;
        }
    }

    /// <summary>
    /// One of Ignatia's composed bodies, walked over one board under one face of the Tie Breaker coin.
    ///
    /// <para>The coin is the one thing a caller almost always varies, so it is the second constructor
    /// argument: the same board under <see cref="IgnatiaBoard.Sun"/> and then
    /// <see cref="IgnatiaBoard.Moon"/> is the shape nearly every case of hers takes.</para>
    /// </summary>
    internal sealed class IgnatiaFrame
    {
        private readonly EffectResolutionContext context;
        private readonly AdjacentMoveAuthority board;
        private readonly List<EffectResolutionEvent> events = new List<EffectResolutionEvent>();

        internal IgnatiaFrame(
            CardDefinitionId cardId,
            TeamSide coin,
            AdjacentMoveAuthority board,
            MatchParticipants participants = null,
            HeroItemState items = null,
            RuleModificationSet modifications = null,
            IEnumerable<CardInstance> extraCards = null,
            ActiveEffectLifecycle timing = null,
            int roundIndex = 1,
            bool startImmediately = true,
            // Milestone 8.75d. What the frame may ask about the Attack it is running inside - which
            // for a REPEATED instance carries the clause that produced it. Default for every case
            // before that milestone, which is text asking nothing about the page around it.
            //
            // The frame's own invocation context stays PrintedPrimary, exactly as a live card-text
            // resolution's does whatever instance of the Attack it belongs to: p.10's repeat is a
            // fact about the ACTION, and `RepeatContinuation` is the effect's own counter.
            EnclosingAttackFacts enclosingAttack = default)
        {
            CardId = cardId;
            this.board = board;
            Participants = participants ?? IgnatiaBoard.Participants();
            Timing = timing ?? new ActiveEffectLifecycle();

            var definition = IgnatiaBoard.Face(cardId);
            var primary = IgnatiaBoard.Primary(cardId);
            Cards = new[] { CardInstance.CreateInHand(IgnatiaBoard.FaceInstance, definition.Reference, IgnatiaBoard.Ignatia) }
                .Concat(extraCards ?? Array.Empty<CardInstance>())
                .ToArray();

            context = new EffectResolutionContext(
                IgnatiaBoard.PrimaryText(cardId),
                IgnatiaBoard.Capabilities(),
                Participants,
                Timing,
                roundIndex,
                1,
                modifications ?? RuleModificationSet.Empty,
                EffectCardState.Of(IgnatiaCardContent.AllDefinitions, Cards),
                primary.Kind,
                components: new MatchComponents(
                    IgnatiaBoard.Components(), board, new MarkerAttachmentRegister(),
                    board, board, board, board),
                heroItems: items ?? new HeroItemState(Participants),
                enclosingAttack: enclosingAttack,
                rosterState: EffectRosterState.Absent,
                tieBreakerRevealedSide: coin);

            if (startImmediately)
            {
                Start();
            }
        }

        internal CardDefinitionId CardId { get; }

        /// <summary>The context this frame walks, for a case that asks a query of it directly.</summary>
        internal EffectResolutionContext Context => context;

        internal MatchParticipants Participants { get; }

        internal ActiveEffectLifecycle Timing { get; }

        internal CardInstance[] Cards { get; }

        internal EffectResolutionStepResult Step { get; private set; }

        internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;

        internal EffectResolutionStepResult Start()
        {
            Step = EffectResolutionSession.Start(
                new EffectResolutionId("ignatia-" + CardId.Value),
                IgnatiaBoard.FaceInstance,
                IgnatiaBoard.Face(CardId).Reference,
                IgnatiaBoard.Primary(CardId).Id,
                IgnatiaBoard.Ignatia,
                IgnatiaBoard.Ignatia,
                ActionInvocationContext.PrintedPrimary,
                context);
            Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            events.AddRange(Step.Events ?? Array.Empty<EffectResolutionEvent>());
            return Step;
        }

        /// <summary>Every candidate the open question offers, as identity strings in the offer's order.</summary>
        internal string[] Candidates()
        {
            if (Choice == null)
            {
                return Array.Empty<string>();
            }

            return Choice.Candidates.Select(candidate => Describe(candidate.Value)).ToArray();
        }

        internal HexCoordinate[] Spaces() =>
            Choice == null
                ? Array.Empty<HexCoordinate>()
                : Choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                    .Select(candidate => candidate.Value.Coordinate)
                    .ToArray();

        internal static string Describe(EffectValue value)
        {
            switch (value.Kind)
            {
                case EffectValueKind.Hero: return value.HeroId.Value;
                case EffectValueKind.Minion: return value.MinionId.Value;
                case EffectValueKind.Coordinate:
                    return $"({value.Coordinate.Q},{value.Coordinate.R})";
                case EffectValueKind.Integer: return value.IntegerValue.ToString();
                case EffectValueKind.CardInstance: return value.CardInstanceId.Value;
                default: return value.Kind.ToString();
            }
        }

        /// <summary>Answers the open question with a named value, confirming it where the node then asks.</summary>
        internal void Pick(EffectValue value)
        {
            Assert.That(Choice, Is.Not.Null, CardId.Value + " has no open question to answer.");
            var choiceId = Choice.ChoiceId;
            Answer(Choice.SelectInvocations.Single(
                invocation => invocation.Candidate.Value.Equals(value)));
            if (Choice != null && Choice.ChoiceId.Equals(choiceId))
            {
                Answer(Choice.ConfirmInvocation);
            }
        }

        internal void Pick(HeroId hero) => Pick(EffectValue.Hero(hero));

        internal void Pick(MinionId minion) => Pick(EffectValue.Minion(minion));

        internal void Pick(HexCoordinate space) => Pick(EffectValue.Space(space));

        internal void Confirm() => Answer(Choice.ConfirmInvocation);

        internal void Decline() => Answer(Choice.DeclineInvocation);

        internal void Answer(EffectChoiceCommand command)
        {
            Step = EffectResolutionSession.Answer(Step.Frame, command, context);
            Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            events.AddRange(Step.Events ?? Array.Empty<EffectResolutionEvent>());
        }

        /// <summary>What a named node reported, read off the events every step emits.</summary>
        internal EffectExecutionResultKind Outcome(string nodeId)
        {
            var recorded = events.Where(value => value.NodeId.Value == nodeId).ToArray();
            return recorded.Length == 0
                ? EffectExecutionResultKind.None
                : recorded[recorded.Length - 1].ResultKind;
        }

        internal HexCoordinate Where(HeroId hero) =>
            board.Occupancies.Single(value => value.HeroId.Equals(hero)).Coordinate;

        internal HexCoordinate Where(MinionId minion) =>
            board.TryGetObjectCoordinate(BoardObjectReference.ForMinion(minion), out var at)
                ? at
                : throw new InvalidOperationException(minion.Value + " stands nowhere.");

        internal bool Holds(MinionId minion) =>
            board.TryGetObjectCoordinate(BoardObjectReference.ForMinion(minion), out _);

        internal IReadOnlyList<PlacedToken> Tokens => board.PlacedTokens;

        internal HexCoordinate[] TokenSpaces() =>
            board.PlacedTokens
                .Select(token => token.Coordinate)
                .OrderBy(space => space.Q)
                .ThenBy(space => space.R)
                .ToArray();
    }
}
