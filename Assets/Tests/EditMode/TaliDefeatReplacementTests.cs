using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
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
    /// <summary>Printed Totem replaces a would-be Defeat, whereas Bolster only prevents removal.</summary>
    public sealed class TaliDefeatReplacementTests
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void TotemReplacesAttackAndSkillDefeatBeforeAnyRewardFactOrLaneCause(bool venerated, bool skill)
        {
            var f = new Fixture(venerated, skill);
            var facts = skill ? f.Skill() : f.Attack("first").Events;
            {
                Assert.That(f.Board.Minions.Any(x => x.MinionId.Equals(f.Target)), Is.True);
                Assert.That(f.Board.PlacedTokens, Is.Empty, "The source pays even when immune to enemy actions.");
                Assert.That(f.Authority.CoinLedger?.GetCoins(f.Actor) ?? 0, Is.Zero);
                Assert.That(facts.Any(x => x.MinionDefeatFactKind == MinionDefeatFactKind.DefeatRecorded), Is.False);
                Assert.That(facts.Any(x => x.MinionDefeatFactKind == MinionDefeatFactKind.RewardAwarded), Is.False);
                Assert.That(f.Authority.PendingRemovalPrevention, Is.Null);
                Assert.That(f.Authority.PendingImmediateLanePush, Is.Null, "The last enemy minion was never defeated.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConsumedSourceCannotReplaceTheFollowingAttack(bool venerated)
        {
            var f = new Fixture(venerated, false);
            f.Attack("first");
            var second = f.Attack("second");
            {
                Assert.That(f.Board.Minions.Any(x => x.MinionId.Equals(f.Target)), Is.False);
                Assert.That(f.Authority.CoinLedger.GetCoins(f.Actor), Is.EqualTo(2),
                    "Only the second attack defeats and pays; a replaced first attack pays nothing.");
                Assert.That(second.Events.Count(x => x.Kind == CardActionResolutionEventKind.MinionDefeatRecorded), Is.EqualTo(1));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlainRemoveDoesNotSpendATotemOrSaveItsMinion(bool venerated)
        {
            var f = new Fixture(venerated, false);
            var result = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.MinionMiniature, RemoveDestination.Supply),
                f.Board, f.Board, BoardObjectReference.ForMinion(f.Target),
                new RemovalStageDecision(RuleActivity.Skill, RuleObject.ForHero(f.Actor), f.Board,
                    f.Board, f.Configuration.Participants, f.Modifications(), LanePushCauseKind.Removal));
            {
                Assert.That(result.ResultKind, Is.EqualTo(RemoveResultKind.Removed));
                Assert.That(f.Board.PlacedTokens, Has.Count.EqualTo(1));
                Assert.That(f.Authority.CoinLedger?.GetCoins(f.Actor) ?? 0, Is.Zero);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReplacedDefeatDoesNotSpendALimitedBounty_TheNextRealDefeatDoes(bool venerated)
        {
            var f = new Fixture(venerated, false);
            f.ArmOneBounty();
            var cached = f.Modifications();
            f.Attack("saved", cached);
            Assert.That(f.LiveBounties, Is.EqualTo(1));
            Assert.That(f.Authority.CoinLedger.GetCoins(f.Actor), Is.Zero);
            f.Attack("real", cached);
            Assert.That(f.LiveBounties, Is.Zero, "Only the actual Defeat spends the limited promise.");
            Assert.That(f.Authority.CoinLedger.GetCoins(f.Actor), Is.EqualTo(3),
                "The real Defeat pays its two coins and the retained one-coin bounty.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoredTotemStillReplacesDefeatAndPaysItsSourceExactlyOnce(bool venerated)
        {
            var f = new Fixture(venerated, false, restored: true);
            var cached = f.Modifications();
            var first = f.Attack("restored-save", cached);
            Assert.That(first.Events.Any(x => x.Kind == CardActionResolutionEventKind.MinionDefeatRecorded), Is.False);
            Assert.That(f.Board.PlacedTokens, Is.Empty);
            var second = f.Attack("restored-real", cached);
            Assert.That(second.Events.Count(x => x.Kind == CardActionResolutionEventKind.MinionDefeatRecorded), Is.EqualTo(1));
            Assert.That(f.Authority.CoinLedger.GetCoins(f.Actor), Is.EqualTo(2));
        }

        private sealed class Fixture
        {
            public readonly MatchConfiguration Configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            public readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            public readonly MinionId Target = new MinionId("blue-target");
            public readonly AdjacentMoveAuthority Board;
            public readonly CardActionResolutionAuthority Authority;
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private CardDefinition[] definitions;
            private CardInstance[] instances;
            private readonly ActionOffer attackOffer;
            private readonly CardInstance skillCard;

            public Fixture(bool venerated, bool skill, bool restored = false)
            {
                var mapId = new MapId("totem-defeat-replacement-map");
                var zone = new BoardMapZone(new ZoneId("battle"), "Battle", "lane", null, false, true);
                var coordinates = new List<HexCoordinate>();
                for (var q = -3; q <= 3; q++)
                for (var r = -3; r <= 3; r++)
                    if (Math.Abs(q + r) <= 3) coordinates.Add(new HexCoordinate(q, r));
                var map = new BoardMapDefinition(mapId, 1, "Replacement probe",
                    coordinates.Select(p => new BoardMapSpace(new SpaceId($"q{p.Q}_r{p.R}"), p,
                        BoardMapSpaceKind.Playable, zone.Id)), new[] { zone },
                    Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
                var defender = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                var minions = new[] { Minion(mapId, Target.Value, TeamSide.Blue, new HexCoordinate(-1, 0),
                        venerated ? StandardMinionType.Ranged : StandardMinionType.Melee),
                    Minion(mapId, "red-holder", TeamSide.Red, new HexCoordinate(2, 0), StandardMinionType.Melee) };
                Board = new AdjacentMoveAuthority(map,
                    new[] { new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(defender, new HexCoordinate(-3, 0)) },
                    Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                    minions);
                Board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("source-totem"), venerated ? TaliComponentContent.VeneratedTotemDefinition :
                        TaliComponentContent.AncestralTotemDefinition, defender, defender,
                    new CardInstanceId("totem-source-card"), new HexCoordinate(-2, 0)));
                if (restored)
                    Board = AdjacentMoveAuthority.Restore(Board.CaptureRestoreState(), map, minions);
                var hand = MisaCardContent.CreateHand(Actor, "replacement-misa");
                definitions = Configuration.CardDefinitions.Concat(hand.Definitions)
                    .GroupBy(x => x.Reference).Select(x => x.First()).ToArray();
                instances = Configuration.CardInstances.Concat(hand.AllInstances).ToArray();
                skillCard = instances.Single(x => x.OwnerId.Equals(Actor) &&
                    x.DefinitionReference.DefinitionId.Equals(MisaCardContent.WatchHowISoar));
                attackOffer = ActionOfferQuery.GetAvailableOffers(Configuration.CardDefinitions,
                    Configuration.CardInstances, Actor, ActionOfferWindow.NormalPlay)
                    .Single(x => x.SourceCardInstanceId.Value == "red-pursuit-card" &&
                        x.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
                Authority = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), Board);
            }

            public RuleModificationSet Modifications() => RuleModificationQuery.Build(
                timing, Board, Configuration.Participants, new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None, RuleObject.ForHero(Actor), null, null);

            public int LiveBounties => timing.Effects.Count(x =>
                x.Definition.EffectKind == MinionDefeatBountyQuery.EffectKind && x.Phase != ActiveEffectPhase.Ended);

            public void ArmOneBounty()
            {
                var action = new ActionDefinitionId("probe-bounty");
                var cardId = new CardInstanceId("probe-bounty-card");
                var definition = new CardDefinition(new CardDefinitionId("replacement-bounty"),
                    "probe-rev-1", "Bounty", new InitiativeValue(4), CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.BasicCard(CardColor.Silver), new[] {
                        new ActionDefinition(action, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default, null,
                            PrintedTextComposition.DeferredUntil("test-only bounty radius source")) });
                definitions = definitions.Concat(new[] { definition }).ToArray();
                instances = instances.Concat(new[] { CardInstance.CreateInHand(cardId, definition.Reference, Actor) }).ToArray();
                var round = new RoundResolutionAuthority(definitions, instances, Configuration.Participants,
                    new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(Authority, Board, timing, null, null, null, null, null, null, null));
                foreach (var group in Configuration.CardInstances.GroupBy(x => x.OwnerId))
                {
                    var commitment = group.First(x => x.Id.Value != "red-pursuit-card");
                    Assert.That(round.TryCommitCard(group.Key, commitment.Id).Accepted, Is.True);
                }
                timing.Create(new ActiveEffectId("limited-bounty"), new ActiveEffectDefinition(
                    "limited-bounty", ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.ThisTurn, TimingCheckpoint.ActionEnded, TimingCheckpoint.ActionEnded,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    new[] { ActiveEffectCancellationCondition.Explicit }, false,
                    MinionDefeatBountyQuery.EffectKind, "limited-bounty", Array.Empty<TimingCheckpoint>()),
                    Actor, TeamSide.Red, Actor, cardId, action, 1, 1);
                timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                    TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Red));
                Assert.That(LiveBounties, Is.EqualTo(1));
            }

            public AttackActionResolutionResult Attack(string id, RuleModificationSet cached = null)
            {
                var rules = cached ?? Modifications();
                var start = Authority.StartAttackResolution(new ActionResolutionId(id), attackOffer,
                    definitions, instances, Configuration.Participants, rules);
                Assert.That(start.Accepted, Is.True, start.RejectionReason.ToString());
                var result = Authority.AnswerAttackTarget(new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId, start.PendingTargetChoice.ChoiceId, Target),
                    definitions, instances, Configuration.Participants, rules);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                return result;
            }

            public IReadOnlyList<CardActionResolutionEvent> Skill()
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(skillCard), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(skillCard), Is.True);
                var offer = new ActionOffer(skillCard.Id, skillCard.DefinitionReference,
                    new ActionDefinitionId("primary-skill"), Actor, ActionInvocationContext.PrintedPrimary, true);
                var result = Authority.StartTurnSkill(new ActionResolutionId("skill-replacement"), offer,
                    definitions, instances, Configuration.Participants, timing, Modifications(),
                    new TieBreakerState(TeamSide.Red), 1, 1);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                var facts = new List<CardActionResolutionEvent>(result.Events);
                for (var i = 0; i < 8 && Authority.PendingEffectChoice != null; i++)
                {
                    var choice = Authority.PendingEffectChoice;
                    var command = choice.SelectInvocations.FirstOrDefault(x =>
                        x.Candidate.Value.MinionId.Equals(Target)) ?? choice.SelectInvocations.LastOrDefault();
                    Assert.That(command, Is.Not.Null, "The printed Misa Defeat bullet and target are explicit choices.");
                    result = Authority.AnswerEffectChoice(command, definitions, instances,
                        Configuration.Participants, timing, Modifications(), new TieBreakerState(TeamSide.Red), 1, 1);
                    Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                    facts.AddRange(result.Events);
                }
                Assert.That(Authority.PendingEffectChoice, Is.Null);
                return facts;
            }

            private static QuickGameMinionSetupState Minion(MapId map, string id, TeamSide side,
                HexCoordinate at, StandardMinionType type) => new QuickGameMinionSetupState(
                new MinionId(id), new TeamId(side == TeamSide.Red ? StagedBoardParticipants.RedTeamIdValue :
                    StagedBoardParticipants.BlueTeamIdValue), side, type,
                new ConfiguredSpawnPointReference(map, new SpawnPointId("spawn-" + id)),
                new ConfiguredSpaceReference(map, new SpaceId($"q{at.Q}_r{at.R}")), at);
        }
    }
}
