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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.114b. One configured round on a radius-5 board with one of Wuk's faces committed
    /// and selected to act - the harness Mrak's block tests use, shared by every Wuk block rather than
    /// copied into each. The enemy commits a one-Movement hold and holds a Defense 9 wall.
    /// </summary>
    internal sealed class WukRound
    {
        internal static readonly HeroId Wuk = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        internal static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        internal static readonly HeroId Ally = new HeroId("wuk-red-ally");
        private static readonly MapId Map = new MapId("wuk-round");

        internal static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        internal static MinionId EnemyMinion(int index) => new MinionId($"wuk-blue-melee-{index}");
        internal static MinionId FriendlyMinion(int index) => new MinionId($"wuk-red-melee-{index}");

        private readonly CardDefinition[] definitions;
        private readonly CardInstance source;
        private readonly CardInstance[] instances;
        private readonly MatchContentCatalogs catalogs;
        private readonly AdjacentMoveAuthority board;
        private readonly RoundResolutionAuthority round;
        private readonly QuickGameMinionSetupState[] minions;

        internal WukRound(
            CardDefinition face,
            HexCoordinate? wukAt = null,
            HexCoordinate? enemyAt = null,
            HexCoordinate? enemyAllyAt = null,
            HexCoordinate? allyAt = null,
            int wukDiscards = 0,
            int allyDiscards = 0,
            IReadOnlyList<HexCoordinate> enemyMinionsAt = null,
            IReadOnlyList<HexCoordinate> friendlyMinionsAt = null,
            IReadOnlyList<HexCoordinate> treesAt = null,
            IReadOnlyList<HexCoordinate> enemyRocksAt = null)
        {
            var participants = Participants(enemyAllyAt.HasValue, allyAt.HasValue);
            var spaces = (from q in Enumerable.Range(-5, 11)
                          from r in Enumerable.Range(-5, 11)
                          where Math.Abs(q + r) <= 5
                          select new HexCoordinate(q, r)).ToArray();
            var heroes = new List<HeroOccupancy>
            {
                new HeroOccupancy(Wuk, wukAt ?? H(0, 0)),
                new HeroOccupancy(Enemy, enemyAt ?? H(-5, 5))
            };
            if (enemyAllyAt.HasValue) heroes.Add(new HeroOccupancy(EnemyAlly, enemyAllyAt.Value));
            if (allyAt.HasValue) heroes.Add(new HeroOccupancy(Ally, allyAt.Value));

            var placed = new List<QuickGameMinionSetupState>();
            var index = 0;
            foreach (var at in enemyMinionsAt ?? Array.Empty<HexCoordinate>())
            {
                placed.Add(Minion(EnemyMinion(index++), StagedBoardParticipants.BlueTeamIdValue,
                    TeamSide.Blue, at));
            }

            index = 0;
            foreach (var at in friendlyMinionsAt ?? Array.Empty<HexCoordinate>())
            {
                placed.Add(Minion(FriendlyMinion(index++), StagedBoardParticipants.RedTeamIdValue,
                    TeamSide.Red, at));
            }

            minions = placed.ToArray();
            board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, spaces), heroes,
                Array.Empty<ObstacleOccupancy>(), minions);
            foreach (var at in treesAt ?? Array.Empty<HexCoordinate>())
            {
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId($"wuk-standing-tree-{at.Q}-{at.R}"),
                    WukComponentContent.Tree, Wuk, Wuk, new CardInstanceId("wuk-earlier-card"), at));
            }

            foreach (var at in enemyRocksAt ?? Array.Empty<HexCoordinate>())
            {
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId($"enemy-rock-{at.Q}-{at.R}"),
                    MrakComponentContent.Rock, Enemy, Enemy, new CardInstanceId("enemy-earlier-card"), at));
            }

            source = CardInstance.CreateInHand(new CardInstanceId("wuk-source"), face.Reference, Wuk);
            var hold = CardInstance.CreateInHand(new CardInstanceId("wuk-enemy-hold"),
                EnemyHold.Reference, Enemy);
            var wall = CardInstance.CreateInHand(new CardInstanceId("wuk-enemy-wall"),
                EnemyWall.Reference, Enemy);
            var discardable = CardInstance.CreateInHand(new CardInstanceId("wuk-enemy-spare"),
                EnemyWall.Reference, Enemy);
            definitions = new[] { face, EnemyHold, EnemyWall };
            var all = new List<CardInstance> { source, hold, wall, discardable };
            var holds = new List<(HeroId Hero, CardInstance Card)> { (Enemy, hold) };
            foreach (var other in heroes.Select(value => value.HeroId)
                         .Where(hero => !hero.Equals(Wuk) && !hero.Equals(Enemy)))
            {
                var otherHold = CardInstance.CreateInHand(
                    new CardInstanceId($"wuk-hold-{other.Value}"), EnemyHold.Reference, other);
                all.Add(otherHold);
                holds.Add((other, otherHold));
            }

            for (var discard = 0; discard < wukDiscards; discard++)
            {
                all.Add(new CardInstance(new CardInstanceId($"wuk-discarded-{discard}"),
                    EnemyWall.Reference, Wuk, CardStateFacts.DiscardedUnplayed()));
            }

            for (var discard = 0; discard < allyDiscards; discard++)
            {
                all.Add(new CardInstance(new CardInstanceId($"wuk-ally-discarded-{discard}"),
                    EnemyWall.Reference, Ally, CardStateFacts.DiscardedUnplayed()));
            }

            instances = all.ToArray();
            catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(WukComponentContent.DeferredHandlers()),
                new ComponentDefinitionCatalog(
                    WukComponentContent.Tokens().Concat(MrakComponentContent.Tokens()),
                    Array.Empty<MarkerDefinition>()),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var markers = new MarkerAttachmentRegister();
            var actions = new CardActionResolutionAuthority(catalogs, board, markers);
            round = new RoundResolutionAuthority(
                definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                new RoundResolutionComposition(actions, board, new ActiveEffectLifecycle(),
                    markers, null, null, null, null, null, null));
            Assert.That(round.TryCommitCard(Wuk, source.Id).Accepted, Is.True);
            foreach (var (hero, card) in holds)
            {
                Assert.That(round.TryCommitCard(hero, card.Id).Accepted, Is.True, hero.Value + " commits");
            }

            Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Wuk));
        }

        /// <summary>Red: Wuk and, when present, his friendly ally; Blue: the enemy and their ally.</summary>
        private static MatchParticipants Participants(bool withEnemyAlly, bool withAlly)
        {
            var red = new TeamId(StagedBoardParticipants.RedTeamIdValue);
            var blue = new TeamId(StagedBoardParticipants.BlueTeamIdValue);
            var players = new List<Player>
            {
                new Player(new PlayerId(StagedBoardParticipants.RedPlayerIdValue), red),
                new Player(new PlayerId(StagedBoardParticipants.BluePlayerIdValue), blue)
            };
            var heroes = new List<Hero>
            {
                new Hero(Wuk, new PlayerId(StagedBoardParticipants.RedPlayerIdValue), HeroDashboard.RulebookLayout),
                new Hero(Enemy, new PlayerId(StagedBoardParticipants.BluePlayerIdValue), HeroDashboard.RulebookLayout)
            };
            if (withEnemyAlly)
            {
                var player = new PlayerId(StagedBoardParticipants.BlueAllyPlayerIdValue);
                players.Add(new Player(player, blue));
                heroes.Add(new Hero(EnemyAlly, player, HeroDashboard.RulebookLayout));
            }

            if (withAlly)
            {
                var player = new PlayerId("wuk-red-ally-player");
                players.Add(new Player(player, red));
                heroes.Add(new Hero(Ally, player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(red, TeamSide.Red), new Team(blue, TeamSide.Blue) }, players, heroes);
        }

        /// <summary>Where each of a hero's cards is now.</summary>
        internal CardLocation[] LocationsOf(HeroId owner) =>
            instances.Where(card => card.OwnerId.Equals(owner))
                .Select(card => card.State.Location).ToArray();

        private static QuickGameMinionSetupState Minion(
            MinionId id, string team, TeamSide side, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                id, new TeamId(team), side, StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId($"spawn-{id.Value}")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        internal static readonly CardDefinition EnemyHold = new CardDefinition(
            new CardDefinitionId("wuk-enemy-hold"), "rev-1", "TEST - Hold",
            new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        internal static readonly CardDefinition EnemyWall = new CardDefinition(
            new CardDefinitionId("wuk-enemy-wall"), "rev-1", "TEST - Wall",
            new InitiativeValue(2), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        internal TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

        internal string PendingNode => Turn.PendingEffectChoice?.NodeId.Value;

        internal int AttackTotal => Turn.PendingDefenseChoice.AttackTotal;

        internal bool ActionEnded =>
            Turn.PendingEffectChoice == null && Turn.PendingDefenseChoice == null &&
            Turn.PendingAttackTargetChoice == null;

        internal HexCoordinate[] Trees() =>
            board.PlacedTokens
                .Where(token => token.Definition.Id.Equals(WukComponentContent.TreeId))
                .Select(token => token.Coordinate).ToArray();

        internal bool Stands(MinionId minion) =>
            board.TryGetObjectCoordinate(BoardObjectReference.ForMinion(minion), out _);

        internal HexCoordinate SpaceOf(HeroId hero)
        {
            Assert.That(board.TryGetObjectCoordinate(BoardObjectReference.ForHero(hero), out var at), Is.True);
            return at;
        }

        internal HexCoordinate[] OfferedSpaces() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate).ToArray();

        internal HeroId[] OfferedHeroes() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                .Select(candidate => candidate.Value.HeroId).ToArray();

        internal MinionId[] OfferedMinions() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                .Select(candidate => candidate.Value.MinionId).ToArray();

        internal string[] AvailableBullets() =>
            Turn.PendingEffectChoice.SelectInvocations
                .Select(command => command.Candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        internal string[] UnavailableBullets() =>
            Turn.PendingEffectChoice.UnavailableCandidates
                .Select(candidate => candidate.OptionNodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        /// <summary>p.14's target question: the heroes the open Attack may name.</summary>
        internal HeroId[] LegalTargetHeroes() =>
            Turn.PendingAttackTargetChoice?.LegalTargetHeroIds.ToArray() ?? Array.Empty<HeroId>();

        /// <summary>p.14's target question: the minions the open Attack may name.</summary>
        internal MinionId[] LegalTargetMinions() =>
            Turn.PendingAttackTargetChoice?.LegalTargetMinionIds.ToArray() ?? Array.Empty<MinionId>();

        private ActionOffer Offer(ActionDefinitionId id) =>
            ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, Wuk, source.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(id));

        internal bool IsOffered(ActionDefinitionId id) =>
            Turn.GetTurnActionOffers().Any(offer => offer.ActionDefinitionId.Equals(id) && offer.IsAvailable);

        internal void StartPrimaryAttack() =>
            Assert.That(round.TryStartAttack(new ActionResolutionId("wuk-attack"),
                Offer(WukCardContent.PrimaryRangedAttack)).Accepted, Is.True);

        internal void StartPrimarySkill() =>
            Assert.That(round.TryStartSkill(new ActionResolutionId("wuk-skill"),
                Offer(WukCardContent.PrimarySkill)).Accepted, Is.True);

        /// <summary>Answers the open modal with the named bullets, then confirms if the window asks.</summary>
        internal void TakeBullets(params string[] optionNodes)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            foreach (var node in optionNodes)
            {
                var open = Turn.PendingEffectChoice;
                Answer(open.SelectInvocations.Single(command => command.Candidate.OptionNodeId.Value == node));
            }

            ConfirmIfStillOpen(choice);
        }

        internal void Target(HeroId hero)
        {
            var choice = Turn.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "p.14's target question is open");
            Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                choice.ResolutionId, choice.ChoiceId, hero)).Accepted, Is.True);
        }

        internal void Target(MinionId minion)
        {
            var choice = Turn.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "p.14's target question is open");
            Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                choice.ResolutionId, choice.ChoiceId, minion)).Accepted, Is.True);
        }

        internal void Defend() => Defend(round);

        internal void PickHero(HeroId hero)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Answer(choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                invocation.Candidate.Value.HeroId.Equals(hero)));
            ConfirmIfStillOpen(choice);
        }

        internal void PickMinion(MinionId minion)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Answer(choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                invocation.Candidate.Value.MinionId.Equals(minion)));
            ConfirmIfStillOpen(choice);
        }

        internal void PickSpaces(params HexCoordinate[] spaces)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            foreach (var space in spaces)
            {
                var open = Turn.PendingEffectChoice;
                Answer(open.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Equals(EffectValue.Space(space))));
            }

            ConfirmIfStillOpen(choice);
        }

        internal CardInstanceId[] OfferedCards() =>
            Turn.PendingEffectChoice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                .Select(candidate => candidate.Value.CardInstanceId).ToArray();

        /// <summary>The hero the open effect question is addressed to.</summary>
        internal HeroId Chooser => Turn.PendingEffectChoice.ChooserId;

        internal void PickCard(CardInstanceId card)
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Answer(choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.CardInstance &&
                invocation.Candidate.Value.CardInstanceId.Equals(card)));
            ConfirmIfStillOpen(choice);
        }

        /// <summary>Takes an open "you may" step.</summary>
        internal void Accept()
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice?.ConfirmInvocation, Is.Not.Null, "an optional step can be taken");
            Answer(choice.ConfirmInvocation);
        }

        internal void Decline()
        {
            var choice = Turn.PendingEffectChoice;
            Assert.That(choice?.DeclineInvocation, Is.Not.Null, "an optional step can be declined");
            Answer(choice.DeclineInvocation);
        }

        private void ConfirmIfStillOpen(PendingEffectChoice asked)
        {
            var open = Turn.PendingEffectChoice;
            if (open != null && open.ChoiceId.Equals(asked.ChoiceId) && open.ConfirmInvocation != null)
            {
                Answer(open.ConfirmInvocation);
            }
        }

        private void Answer(EffectChoiceCommand command) =>
            Assert.That(round.TryAnswerEffectChoice(command).Accepted, Is.True);

        /// <summary>
        /// Snapshot at the open question, restore it, answer both the same way, and require the same
        /// next question and the same authoritative bytes - then once more from the retained prefix.
        /// </summary>
        internal void AssertRestoreReplays(params HexCoordinate[] answer)
        {
            var pending = Turn.PendingEffectChoice;
            Assert.That(pending, Is.Not.Null);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
            Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())));

            PickSpaces(answer);
            AnswerSpaces(restored.Round, answer);
            var live = AuthoritativeSnapshotSerializer.Serialize(round, board);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(live), "the restored pause replays to the same authoritative bytes");

            var prefix = Restore(bytes);
            AnswerSpaces(prefix.Round, answer);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(prefix.Round, prefix.Board),
                Is.EqualTo(live), "re-executing from the retained prefix matches the forward result");
        }

        /// <summary>Restores at an open card question and requires the same pick to converge.</summary>
        internal void AssertRestoreRepliesWithCard(CardInstanceId card)
        {
            var pending = Turn.PendingEffectChoice;
            Assert.That(pending, Is.Not.Null);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
            Assert.That(resumed.ChooserId, Is.EqualTo(pending.ChooserId));
            PickCard(card);
            var command = resumed.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.CardInstance &&
                invocation.Candidate.Value.CardInstanceId.Equals(card));
            Assert.That(restored.Round.TryAnswerEffectChoice(command).Accepted, Is.True);
            var still = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
            if (still != null && still.ChoiceId.Equals(resumed.ChoiceId) && still.ConfirmInvocation != null)
            {
                Assert.That(restored.Round.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
            }

            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
        }

        /// <summary>Restores at p.14's target question and requires the same answer to converge.</summary>
        internal void AssertTargetRestoreReplays(HeroId hero)
        {
            var pending = Turn.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null);
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var resumed = restored.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.LegalTargetHeroIds, Is.EqualTo(pending.LegalTargetHeroIds));
            Assert.That(resumed.LegalTargetMinionIds, Is.EqualTo(pending.LegalTargetMinionIds));
            Target(hero);
            Assert.That(restored.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                resumed.ResolutionId, resumed.ChoiceId, hero)).Accepted, Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
        }

        internal void AssertDefenseRestoreReplays()
        {
            var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = Restore(bytes);
            var pending = restored.Round.CurrentTurnAuthority.PendingDefenseChoice;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.AttackTotal, Is.EqualTo(Turn.PendingDefenseChoice.AttackTotal));
            Defend(round);
            Defend(restored.Round);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
        }

        private static void Defend(RoundResolutionAuthority target)
        {
            var defense = target.CurrentTurnAuthority.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, "p.14's Defense question is open");
            var wall = defense.LegalDefenseOffers.First(offer =>
                offer.SourceDefinitionReference.Equals(EnemyWall.Reference));
            Assert.That(target.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                defense.ResolutionId, defense.ChoiceId, wall.SourceCardInstanceId)).Accepted, Is.True);
        }

        private RestoredAuthoritativeMatch Restore(string bytes) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                catalogs, bytes, definitions, new[] { board.MapDefinition }, minions);

        private static void AnswerSpaces(RoundResolutionAuthority target, HexCoordinate[] spaces)
        {
            var asked = target.CurrentTurnAuthority.PendingEffectChoice;
            foreach (var space in spaces)
            {
                var open = target.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(target.TryAnswerEffectChoice(open.SelectInvocations.Single(
                    invocation => invocation.Candidate.Value.Equals(EffectValue.Space(space)))).Accepted,
                    Is.True);
            }

            var still = target.CurrentTurnAuthority.PendingEffectChoice;
            if (still != null && still.ChoiceId.Equals(asked.ChoiceId) && still.ConfirmInvocation != null)
            {
                Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
            }
        }
    }
}
