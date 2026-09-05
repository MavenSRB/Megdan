using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Tier-0.5 greedy-nudge-bot pass (user-approved amendment to the THIRTEENTH's random-legal
    /// <see cref="PrivateSeatDriver"/>). Each case stages a minimal fake <see cref="IMatchReadModel"/>
    /// - a "staged projection" - directly and calls one of the driver's <c>internal static</c>
    /// preference sub-selectors (or the top-level <see cref="PrivateSeatDriver.SelectGreedyCommand"/>)
    /// with a hand-built command list, rather than driving a full <c>MatchSession</c>: the selection
    /// methods under test read only a handful of <see cref="IMatchReadModel"/> members and take the
    /// command list as a plain argument, so nothing here needs the rules engine to be live.
    /// </summary>
    /// <remarks>
    /// Deliberately its own file rather than an addition to <c>PrivateSeatDriverTests.cs</c>: that
    /// file's lobby-presenter cases pull in <c>PrivateOnlineWireHarness</c>, which in turn pulls in
    /// several other test files' own helper types - a dependency chain this file has no reason to
    /// carry. Both files are harness-excluded for the same reason (each touches
    /// <c>GuardsDigital.Presentation.Interaction</c>), matching the <c>HostConsoleCommandsTests.cs</c>
    /// precedent.
    /// </remarks>
    public sealed class PrivateSeatDriverGreedyPreferenceTests
    {
        private static (HeroId RedHero, HeroId BlueHeroA, HeroId BlueHeroB, MatchParticipants Participants)
            CreateOneRedTwoBlueParticipants()
        {
            var redTeam = new Team(new TeamId("red-team"), TeamSide.Red);
            var blueTeam = new Team(new TeamId("blue-team"), TeamSide.Blue);
            var redPlayer = new Player(new PlayerId("red-player"), redTeam.Id);
            var bluePlayerA = new Player(new PlayerId("blue-player-a"), blueTeam.Id);
            var bluePlayerB = new Player(new PlayerId("blue-player-b"), blueTeam.Id);
            var redHero = new Hero(new HeroId("red-hero"), redPlayer.Id, HeroDashboard.RulebookLayout);
            var blueHeroA = new Hero(new HeroId("blue-hero-a"), bluePlayerA.Id, HeroDashboard.RulebookLayout);
            var blueHeroB = new Hero(new HeroId("blue-hero-b"), bluePlayerB.Id, HeroDashboard.RulebookLayout);
            var participants = new MatchParticipants(
                new[] { redTeam, blueTeam },
                new[] { redPlayer, bluePlayerA, bluePlayerB },
                new[] { redHero, blueHeroA, blueHeroB });
            return (redHero.Id, blueHeroA.Id, blueHeroB.Id, participants);
        }

        private static MinionOccupancy Minion(string id, TeamId teamId, TeamSide side, HexCoordinate coordinate)
        {
            var mapId = new MapId("greedy-nudge-fixture");
            var setup = new QuickGameMinionSetupState(
                new MinionId(id), teamId, side, StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId(id + "-spawn")),
                new ConfiguredSpaceReference(mapId, new SpaceId(id + "-space")),
                coordinate);
            return MinionOccupancy.FromSetup(setup);
        }

        private static AttackTargetDefensePreviewProjection DefenseBonusPreview(
            HeroId heroId, params int[] minionAmounts)
        {
            var contributions = minionAmounts.Select((amount, index) => new ValueContributionProjection(
                new ValueContribution(
                    ValueContributionKind.MinionDefenseModifier,
                    new MinionId("preview-minion-" + index),
                    PrintedValueChannel.Defense,
                    amount),
                "Preview Minion " + index));
            return new AttackTargetDefensePreviewProjection(heroId, contributions);
        }

        private static PendingDefensePreviewProjection BuildDefensePreview(
            int attackTotal, params (CardInstanceId? CardId, int? DefenseTotal)[] choices)
        {
            var attackContribution = new ValueContributionProjection(
                new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    new CardInstanceId("attack-source-card"),
                    new ActionDefinitionId("attack-source-action"),
                    PrintedValueChannel.Attack,
                    attackTotal),
                "Attack");
            var previewChoices = choices.Select(choice =>
            {
                var outcome = AttackOutcomeComparison.Create(
                    attackTotal, choice.DefenseTotal, DefenseBlockResult.None).Outcome;
                var label = choice.CardId.HasValue ? "Defense " + choice.CardId.Value.Value : "Decline";
                var defenseContributions = choice.DefenseTotal.HasValue
                    ? new[]
                    {
                        new ValueContributionProjection(
                            new ValueContribution(
                                ValueContributionKind.PrintedIcon, choice.CardId.Value,
                                new ActionDefinitionId(choice.CardId.Value.Value + "-action"),
                                PrintedValueChannel.Defense, choice.DefenseTotal.Value),
                            label)
                    }
                    : Array.Empty<ValueContributionProjection>();
                return new DefenseChoicePreviewProjection(
                    choice.CardId, label, defenseContributions, choice.DefenseTotal,
                    DefenseBlockResult.None, outcome);
            }).ToArray();
            return new PendingDefensePreviewProjection(
                new ActionResolutionId("probe-resolution"), new ActionResolutionStepId("probe-step"),
                new[] { attackContribution }, attackTotal, previewChoices,
                Array.Empty<ValueContributionProjection>());
        }

        // --- Preference 1a: prefer starting an Attack over any other offered action ----------------

        [Test]
        public void PreferAttackStart_PicksTheAttackOverOtherOfferedActions()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
            var heroId = configuration.RedHand.Instances[0].OwnerId;
            var hand = configuration.RedHand.Instances.Where(instance => instance.OwnerId.Equals(heroId)).ToArray();
            var starts = new List<DirectorCommand>();
            var attackActionIds = new HashSet<string>();
            foreach (var instance in hand)
            {
                var definition = configuration.CardDefinitions.Single(
                    value => value.Reference.Equals(instance.DefinitionReference));
                foreach (var action in definition.ActionDefinitions)
                {
                    starts.Add(DirectorCommand.StartAction(instance.Id, action.Id));
                    if (action.Kind == ActionKind.Attack)
                    {
                        attackActionIds.Add(action.Id.Value);
                    }
                }
            }

            Assert.That(attackActionIds, Is.Not.Empty,
                "The fixture hand must offer at least one Attack action, or this proves nothing.");
            Assert.That(starts.Count, Is.GreaterThan(attackActionIds.Count),
                "The fixture hand must also offer at least one non-Attack action, or this proves nothing.");

            var readModel = new FakeMatchReadModel
            {
                Cards = configuration.CardInstances,
                Definitions = configuration.CardDefinitions
            };

            var selected = PrivateSeatDriver.PreferAttackStart(starts, readModel, new Random(1));

            Assert.That(selected, Is.Not.Null);
            Assert.That(attackActionIds, Has.Member(selected.ActionDefinitionId.Value));
        }

        [Test]
        public void PreferAttackStart_WithNoAttackAmongTheOffers_FallsBackToNull()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
            var heroId = configuration.RedHand.Instances[0].OwnerId;
            var instance = configuration.RedHand.Instances.First(value => value.OwnerId.Equals(heroId));
            var definition = configuration.CardDefinitions.Single(
                value => value.Reference.Equals(instance.DefinitionReference));
            var nonAttack = definition.ActionDefinitions.First(action => action.Kind != ActionKind.Attack);
            var commands = new[] { DirectorCommand.StartAction(instance.Id, nonAttack.Id) };
            var readModel = new FakeMatchReadModel
            {
                Cards = configuration.CardInstances,
                Definitions = configuration.CardDefinitions
            };

            var selected = PrivateSeatDriver.PreferAttackStart(commands, readModel, new Random(1));

            Assert.That(selected, Is.Null);
        }

        // --- Preference 1b: among legal targets, prefer the "lowest-Life/killable" one -------------

        [Test]
        public void PreferKillableAttackTarget_PrefersTheEnemyOnTheTeamWithFewerLifeCounters()
        {
            var redTeam = new Team(new TeamId("red-team"), TeamSide.Red);
            var lowLifeTeam = new Team(new TeamId("enemy-team-low"), TeamSide.Blue);
            var highLifeTeam = new Team(new TeamId("enemy-team-high"), TeamSide.Blue);
            var redPlayer = new Player(new PlayerId("red-player"), redTeam.Id);
            var lowPlayer = new Player(new PlayerId("low-player"), lowLifeTeam.Id);
            var highPlayer = new Player(new PlayerId("high-player"), highLifeTeam.Id);
            var redHero = new Hero(new HeroId("red-hero"), redPlayer.Id, HeroDashboard.RulebookLayout);
            var lowLifeHero = new Hero(new HeroId("low-life-hero"), lowPlayer.Id, HeroDashboard.RulebookLayout);
            var highLifeHero = new Hero(new HeroId("high-life-hero"), highPlayer.Id, HeroDashboard.RulebookLayout);
            var participants = new MatchParticipants(
                new[] { redTeam, lowLifeTeam, highLifeTeam },
                new[] { redPlayer, lowPlayer, highPlayer },
                new[] { redHero, lowLifeHero, highLifeHero });
            var readModel = new FakeMatchReadModel
            {
                Participants = participants,
                TeamLifeCounters = new[]
                {
                    new KeyValuePair<TeamId, int>(redTeam.Id, 4),
                    new KeyValuePair<TeamId, int>(lowLifeTeam.Id, 1),
                    new KeyValuePair<TeamId, int>(highLifeTeam.Id, 4)
                }
            };
            var commands = new[]
            {
                DirectorCommand.ChooseAttackTarget(highLifeHero.Id),
                DirectorCommand.ChooseAttackTarget(lowLifeHero.Id)
            };

            var selected = PrivateSeatDriver.PreferKillableAttackTarget(commands, readModel, new Random(1));

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.HeroId, Is.EqualTo(lowLifeHero.Id));
        }

        [Test]
        public void PreferKillableAttackTarget_TiedOnLife_PrefersTheLowerMinionDefenseBonus()
        {
            var (_, blueHeroA, blueHeroB, participants) = CreateOneRedTwoBlueParticipants();
            var blueTeamId = participants.GetTeamForHero(blueHeroA).Id;
            var readModel = new FakeMatchReadModel
            {
                Participants = participants,
                TeamLifeCounters = new[] { new KeyValuePair<TeamId, int>(blueTeamId, 3) },
                PendingAttackTargetDefensePreviews = new[]
                {
                    DefenseBonusPreview(blueHeroA, 1, 1),  // +2 total: well defended
                    DefenseBonusPreview(blueHeroB, -1)     // -1 total: weaker defense, more killable
                }
            };
            var commands = new[]
            {
                DirectorCommand.ChooseAttackTarget(blueHeroA),
                DirectorCommand.ChooseAttackTarget(blueHeroB)
            };

            var selected = PrivateSeatDriver.PreferKillableAttackTarget(commands, readModel, new Random(1));

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.HeroId, Is.EqualTo(blueHeroB));
        }

        [Test]
        public void PreferKillableAttackTarget_WithOnlyMinionTargets_FallsBackToNull()
        {
            var readModel = new FakeMatchReadModel();
            var commands = new[] { DirectorCommand.ChooseAttackTarget(new MinionId("some-minion")) };

            var selected = PrivateSeatDriver.PreferKillableAttackTarget(commands, readModel, new Random(1));

            Assert.That(selected, Is.Null);
        }

        // --- Preference 2: prefer the movement destination closest to the fight ---------------------

        [Test]
        public void PreferAdvancingMovement_PrefersTheDestinationClosestToTheNearestEnemyOrMinion()
        {
            var (redHero, blueHeroA, _, participants) = CreateOneRedTwoBlueParticipants();
            var enemyCoordinate = new HexCoordinate(10, 0);
            var minionCoordinate = new HexCoordinate(0, 3);
            var readModel = new FakeMatchReadModel
            {
                Participants = participants,
                Heroes = new[]
                {
                    new HeroOccupancy(redHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blueHeroA, enemyCoordinate)
                },
                Minions = new[]
                {
                    Minion("lane-minion", participants.GetTeamForHero(blueHeroA).Id, TeamSide.Blue, minionCoordinate)
                }
            };
            var near = new HexCoordinate(0, 2);
            var mid = new HexCoordinate(5, 0);
            var far = new HexCoordinate(0, -5);
            var commands = new[]
            {
                DirectorCommand.ChooseMovementDestination(far),
                DirectorCommand.ChooseMovementDestination(near),
                DirectorCommand.ChooseMovementDestination(mid)
            };

            var selected = PrivateSeatDriver.PreferAdvancingMovement(commands, readModel, redHero, new Random(1));

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.Coordinate, Is.EqualTo(near),
                "The minion is only 1 hex from 'near' but 5+ hexes from the other two candidates, so " +
                "the minion - not just the enemy hero - has to count as an objective for this to pick it.");
        }

        [Test]
        public void PreferAdvancingMovement_WithNoEnemiesOrMinionsOnBoard_FallsBackToNull()
        {
            var (redHero, _, _, participants) = CreateOneRedTwoBlueParticipants();
            var readModel = new FakeMatchReadModel
            {
                Participants = participants,
                Heroes = new[] { new HeroOccupancy(redHero, new HexCoordinate(0, 0)) }
            };
            var commands = new[] { DirectorCommand.ChooseMovementDestination(new HexCoordinate(1, 0)) };

            var selected = PrivateSeatDriver.PreferAdvancingMovement(commands, readModel, redHero, new Random(1));

            Assert.That(selected, Is.Null);
        }

        // --- Preference 3: play a defense card only when declining would be lethal ------------------

        [Test]
        public void PreferSufficientDefense_PlaysTheCheapestCardThatSurvivesALethalHit()
        {
            var cheapCard = new CardInstanceId("cheap-defense-card");
            var expensiveCard = new CardInstanceId("expensive-defense-card");
            var weakCard = new CardInstanceId("insufficient-defense-card");
            var preview = BuildDefensePreview(
                5,
                (null, null),
                (cheapCard, 5),
                (expensiveCard, 8),
                (weakCard, 3));
            var readModel = new FakeMatchReadModel { PendingDefensePreview = preview };
            var commands = new[]
            {
                DirectorCommand.DeclineDefense(),
                DirectorCommand.ChooseDefenseCard(cheapCard),
                DirectorCommand.ChooseDefenseCard(expensiveCard),
                DirectorCommand.ChooseDefenseCard(weakCard)
            };

            var selected = PrivateSeatDriver.PreferSufficientDefense(commands, readModel, new Random(1));

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.Kind, Is.EqualTo(DirectorCommandKind.ChooseDefenseCard));
            Assert.That(selected.CardInstanceId, Is.EqualTo(cheapCard),
                "The expensive card also survives, but the cheap one is sufficient and must win.");
        }

        [Test]
        public void PreferSufficientDefense_WithNoPendingDefensePreview_FallsBackToNull()
        {
            var readModel = new FakeMatchReadModel();
            var commands = new[]
            {
                DirectorCommand.DeclineDefense(),
                DirectorCommand.ChooseDefenseCard(new CardInstanceId("some-card"))
            };

            var selected = PrivateSeatDriver.PreferSufficientDefense(commands, readModel, new Random(1));

            Assert.That(selected, Is.Null);
        }

        [Test]
        public void PreferSufficientDefense_WhenNoCardWouldSurviveTheHit_FallsBackToNull()
        {
            var weakCard = new CardInstanceId("insufficient-defense-card");
            var preview = BuildDefensePreview(10, (null, null), (weakCard, 3));
            var readModel = new FakeMatchReadModel { PendingDefensePreview = preview };
            var commands = new[]
            {
                DirectorCommand.DeclineDefense(),
                DirectorCommand.ChooseDefenseCard(weakCard)
            };

            var selected = PrivateSeatDriver.PreferSufficientDefense(commands, readModel, new Random(1));

            Assert.That(selected, Is.Null,
                "Never illegal, never a wasted card either: with nothing sufficient, random-legal decides.");
        }

        // --- Durable non-goals: commitments, leveling, and agreement convergence stay untouched -----

        [Test]
        public void SelectGreedyCommand_ForCommitmentOffers_AlwaysDefersToRandomLegal()
        {
            // Card SELECTION at a commitment is a durable non-goal (card choice IS the game's skill):
            // no sub-selector recognizes CommitCard/Pass, so every call must return null here.
            var readModel = new FakeMatchReadModel();
            var commands = new[]
            {
                DirectorCommand.CommitCard(new HeroId("some-hero"), new CardInstanceId("card-a")),
                DirectorCommand.CommitCard(new HeroId("some-hero"), new CardInstanceId("card-b")),
                DirectorCommand.Pass(new HeroId("some-hero"))
            };

            var selected = PrivateSeatDriver.SelectGreedyCommand(
                commands, readModel, new HeroId("some-hero"), new Random(1));

            Assert.That(selected, Is.Null);
        }

        // --- Determinism: the same seed must decide a tie the same way every time -------------------

        [Test]
        public void SelectGreedyCommand_WithTheSameSeed_PicksTheSameTiedChoiceTwice()
        {
            var (_, blueHeroA, blueHeroB, participants) = CreateOneRedTwoBlueParticipants();
            var blueTeamId = participants.GetTeamForHero(blueHeroA).Id;
            var readModel = new FakeMatchReadModel
            {
                Participants = participants,
                TeamLifeCounters = new[] { new KeyValuePair<TeamId, int>(blueTeamId, 3) }
            };
            var commands = new[]
            {
                DirectorCommand.ChooseAttackTarget(blueHeroA),
                DirectorCommand.ChooseAttackTarget(blueHeroB)
            };

            var first = PrivateSeatDriver.SelectGreedyCommand(
                commands, readModel, new HeroId("red-hero"), new Random(7));
            var second = PrivateSeatDriver.SelectGreedyCommand(
                commands, readModel, new HeroId("red-hero"), new Random(7));

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.EqualTo(first),
                "Both targets are tied (same team, no preview data), so the die decides - but the " +
                "same seed must decide the same way both times.");
        }

        /// <summary>
        /// A minimal, hand-staged <see cref="IMatchReadModel"/>: every member the four sub-selectors
        /// under test never read is a safe, cheap default; the handful they DO read
        /// (<see cref="Participants"/>, <see cref="Heroes"/>, <see cref="Minions"/>,
        /// <see cref="TeamLifeCounters"/>, <see cref="PendingAttackTargetDefensePreviews"/>,
        /// <see cref="PendingDefensePreview"/>, <see cref="Cards"/>, <see cref="Definitions"/>) are
        /// plain settable properties. Modeled on <c>MatchFeaturePresenterTests.DelegatingReadModel</c>'s
        /// member list (same interface, verified there against the live build) rather than guessed.
        /// </summary>
        private sealed class FakeMatchReadModel : IMatchReadModel
        {
            public long Version => 0;
            public bool IsInputEnabled => true;
            public bool IsSeatUndoAvailable => false;
            public PendingDecisionOwner DecisionOwner => PendingDecisionOwner.None;
            public IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog =>
                Array.Empty<GuardsDigital.Core.Diagnostics.MatchLogEntry>();
            public MatchUndoMarker UndoMarker => MatchUndoMarker.None;
            public QuickGameSetupState Setup => null;
            public BoardMapDefinition SelectedMap => null;
            public MatchParticipants Participants { get; set; }
            public IReadOnlyList<CardDefinition> Definitions { get; set; } = Array.Empty<CardDefinition>();
            public IReadOnlyList<HeroOccupancy> Heroes { get; set; } = Array.Empty<HeroOccupancy>();
            public IReadOnlyList<MinionOccupancy> Minions { get; set; } = Array.Empty<MinionOccupancy>();
            public IReadOnlyList<ObstacleOccupancy> Obstacles => Array.Empty<ObstacleOccupancy>();
            public IReadOnlyList<PlacedToken> PlacedTokens => Array.Empty<PlacedToken>();
            public IReadOnlyList<MarkerAttachment> HeldMarkers => Array.Empty<MarkerAttachment>();
            public IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId) =>
                Array.Empty<MarkerAttachment>();
            public int RoundIndex => 0;
            public int TurnIndex => 0;
            public RoundResolutionPhase Phase => RoundResolutionPhase.ResolvingTurn;
            public TieBreakerState TieBreakerState => default;
            public IReadOnlyList<RoundTurnSlot> TurnSlots => Array.Empty<RoundTurnSlot>();
            public IReadOnlyList<CompletedRoundRecord> CompletedRounds => Array.Empty<CompletedRoundRecord>();
            public EndOfRoundStage? CurrentEndOfRoundStage => null;
            public IReadOnlyList<MatchCommitmentProjection> Commitments => Array.Empty<MatchCommitmentProjection>();
            public MatchCommitmentProjection GetCommitment(HeroId heroId) => default;
            public SelectedInitiativeActor SelectedActor => default;
            public bool IsTurnActive => false;
            public IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters { get; set; } =
                Array.Empty<KeyValuePair<TeamId, int>>();
            public IReadOnlyList<HeroProgressionProjection> HeroProgression =>
                Array.Empty<HeroProgressionProjection>();
            public GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome => default;
            public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds =>
                Array.Empty<CardInstanceId>();
            public bool IsActiveEffectSource(CardInstanceId cardInstanceId) => false;
            public TeamAgreementProjection TeamAgreement => null;
            public IReadOnlyList<CardInstance> Cards { get; set; } = Array.Empty<CardInstance>();
            public PendingDecisionContract DecisionContract => null;
            public PendingInitiativeChoice PendingInitiativeChoice => null;
            public PendingCardActionResolution PendingMovementResolution => null;
            public bool HasPendingMovementResolution => false;
            public PendingMovementChoice PendingMovementChoice => null;
            public PendingAttackResolution PendingAttackResolution => null;
            public bool HasPendingAttackResolution => false;
            public PendingAttackTargetChoice PendingAttackTargetChoice => null;
            public IReadOnlyList<AttackTargetDefensePreviewProjection> PendingAttackTargetDefensePreviews
            {
                get;
                set;
            } = Array.Empty<AttackTargetDefensePreviewProjection>();
            public PendingDefenseResponseChoice PendingDefenseChoice => null;
            public PendingDefensePreviewProjection PendingDefensePreview { get; set; }
            public PendingResolution PendingSkillResolution => null;
            public bool HasPendingSkillResolution => false;
            public PendingEffectChoice PendingEffectChoice => null;
            public IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview =>
                Array.Empty<EffectChoiceCandidatePreview>();
            public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
                PendingDisplacedMinionCorrection => null;
            public PendingMinionBattleChoice PendingMinionBattleChoice => null;
            public PendingHeroRespawnChoice PendingHeroRespawnChoice => null;
            public GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
                PendingSystemTransitionStep => null;
            public RemovalPreventionPrompt PendingRemovalPrevention => null;
            public GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease => null;
            public GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
                PendingSystemTransitionDescription => default;
            public IReadOnlyList<ActionOffer> AvailableTurnActions => Array.Empty<ActionOffer>();
            public IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions =>
                Array.Empty<FastTravelOptionProjection>();
            public IReadOnlyList<ClearOptionProjection> AvailableClearOptions =>
                Array.Empty<ClearOptionProjection>();
            public ActionOffer? AvailableHoldOffer => null;
        }
    }
}
