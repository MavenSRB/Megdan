using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.66. Treetop Sentinel's escalation, driven: "<b>End of turn:</b> An enemy hero in
    /// radius discards a card, <b>or is defeated</b>."
    ///
    /// <para><b>The defect this file closes, in one sentence: the face could never defeat
    /// anybody.</b> Its selection declared the discard as its consumer, a consumer declaration narrows
    /// the offer to candidates the consumer can act on, and a hero with an empty hand cannot discard -
    /// so the one hero the second half of the sentence exists for was the one hero the first half
    /// refused to offer. Warning Shot, whose text ends "if able", is CORRECT to narrow: an
    /// empty-handed hero gives nothing and p.17's "if able" ends the step. The two faces are one
    /// helper with one boolean, and this file is what keeps the two readings apart.</para>
    ///
    /// <para><b>It is a live defect on dealt content, found by another hero's dealing.</b> Milestone
    /// 8.61f drove Min's Death Grenade - the same printed escalation at the same moment - measured the
    /// narrowing on her own body, removed the declaration there, and reported Silverarrow's. Widget's
    /// Scorching Breath declares none for the same shape, and Swift's Killing Ground had the identical
    /// one-line defect and is corrected in the same milestone.</para>
    ///
    /// <para>The drive is composed by hand for the reason <c>SilverarrowEndOfTurnDriveTests</c>
    /// documents - a staged configuration cannot name her deferred contracts - and through a ROUND
    /// rather than a turn, because p.13's Defeat is priced out of the Levels, Life counters and coin
    /// balances a round owns.</para>
    /// </summary>
    public sealed class SilverarrowTreetopSentinelTests
    {
        private static readonly HeroId Silverarrow = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId FarEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static HexCoordinate At(int q) => new HexCoordinate(q, 0);

        /// <summary>
        /// The whole milestone. An enemy hero inside the disk whose hand is empty is OFFERED - which
        /// is the assertion that fails on the shipped composition - and is then defeated, with p.13's
        /// coins going to the hero whose card did it and the 8.53 scoreboard recording her Kill.
        /// </summary>
        [Test]
        public void AnEmptyHandedHeroInRadiusIsOfferedAndDefeated_WithP13sCoinsAndTheScoreboardsKill()
        {
            var drive = Drive(Face.TreetopSentinel, enemyHasCards: false);

            Assert.That(drive.PauseNodes, Does.Contain("sentinel-select"),
                "An empty-handed hero is the candidate the second half of the sentence exists for, " +
                "so the selection must still offer them.");
            Assert.That(drive.OfferedAtTheSelection, Is.EqualTo(new[] { Enemy }),
                "and they are the offer, rather than an offer narrowed to nobody.");
            Assert.That(drive.EnemyOnBoardAfter, Is.False, "\"or is defeated\" - p.13 removes them.");
            Assert.That(drive.CoinsAfter, Is.GreaterThan(drive.CoinsBefore),
                "The defeating hero collects p.13's coins.");
            Assert.That(drive.KillsAfter, Is.EqualTo(drive.KillsBefore + 1));
        }

        /// <summary>
        /// The other half of the escalation, and the reason it is not a choice: a hero who CAN discard
        /// does, and stands.
        /// </summary>
        [Test]
        public void AHeroHoldingCardsDiscardsOneAndIsNotDefeated()
        {
            var drive = Drive(Face.TreetopSentinel, enemyHasCards: true);

            Assert.That(drive.PauseNodes,
                Is.EqualTo(new[] { "sentinel-select", "sentinel-discard" }));
            Assert.That(drive.EnemyHandBefore - drive.EnemyHandAfter, Is.EqualTo(1));
            Assert.That(drive.EnemyOnBoardAfter, Is.True);
            Assert.That(drive.KillsAfter, Is.EqualTo(drive.KillsBefore));
        }

        /// <summary>
        /// Warning Shot keeps its narrowing, which is the half of this correction that is NOT a
        /// change. "discards a card, if able" gives an empty-handed hero nothing to do, so offering
        /// them would be an offer admitting more than the answer can perform - and nothing is asked of
        /// anybody, nothing happens, and the turn closes.
        /// </summary>
        [Test]
        public void WarningShotStillNarrowsTheEmptyHandedHeroOutOfItsOwnOffer()
        {
            var drive = Drive(Face.WarningShot, enemyHasCards: false);

            Assert.That(drive.PauseNodes, Is.Empty,
                "The consumer narrows the only candidate out, so the mandatory selection has no " +
                "legal subject and p.10 stops the effect there.");
            Assert.That(drive.EnemyOnBoardAfter, Is.True,
                "\"if able\" ends the sentence - there is no second half to escalate into.");
            Assert.That(drive.TurnEnded, Is.True,
                "and an end-of-turn face with no subject must never hang the close.");
        }

        /// <summary>The same face with a hand to take from: the narrowing is a filter, not a refusal.</summary>
        [Test]
        public void WarningShotTakesACardWhenTheHeroInRadiusHasOne()
        {
            var drive = Drive(Face.WarningShot, enemyHasCards: true);

            Assert.That(drive.PauseNodes, Is.EqualTo(new[] { "warning-select", "warning-discard" }));
            Assert.That(drive.EnemyHandBefore - drive.EnemyHandAfter, Is.EqualTo(1));
        }

        /// <summary>
        /// Her two "End of turn:" faces standing at one checkpoint, which milestone 8.67 made
        /// reachable: each asks its own two questions and each takes a card. Before that milestone the
        /// second was dropped the moment the first paused, so this pair could not be driven at all.
        /// </summary>
        [Test]
        public void BothOfHerEndOfTurnFacesRunAtOneCheckpoint()
        {
            var drive = Drive(Face.Both, enemyHasCards: true, enemySpareCards: 2);

            Assert.That(drive.PauseNodes, Is.EqualTo(new[]
                {
                    "warning-select", "warning-discard", "sentinel-select", "sentinel-discard"
                }),
                "Warning Shot is created first here, so it is the one the checkpoint reaches first.");
            Assert.That(drive.EnemyHandBefore - drive.EnemyHandAfter, Is.EqualTo(2));
            Assert.That(drive.EnemyOnBoardAfter, Is.True,
                "The hero could pay both times, so the escalation is never reached.");
        }

        /// <summary>
        /// The same pair against a hand that runs out between them: Warning Shot takes the last card
        /// and finds nothing the second time, and Treetop Sentinel then defeats the hero it is
        /// printed to defeat. One checkpoint, two faces, both halves of the pair's difference.
        /// </summary>
        [Test]
        public void TheEscalatingFaceDefeatsTheHeroTheIfAbleFaceJustEmptied()
        {
            var drive = Drive(Face.Both, enemyHasCards: true, enemySpareCards: 1);

            Assert.That(drive.PauseNodes, Is.EqualTo(new[]
                {
                    "warning-select", "warning-discard", "sentinel-select"
                }),
                "Warning Shot takes the last card; the Sentinel then offers the hero it emptied and " +
                "has no discard to ask about.");
            Assert.That(drive.EnemyOnBoardAfter, Is.False);
            Assert.That(drive.KillsAfter, Is.EqualTo(drive.KillsBefore + 1));
        }

        /// <summary>
        /// The correction itself, read off the composition rather than inferred from the drives above.
        /// One helper builds both bodies, and the declaration is the one thing the boolean decides:
        /// "if able" names its consumer, "or is defeated" names none.
        /// </summary>
        [Test]
        public void OnlyTheIfAbleFaceDeclaresItsDiscardAsTheSelectionsConsumer()
        {
            Assert.That(ConsumerOfTheSelection("warning"), Is.EqualTo("warning-discard"),
                "\"discards a card, if able\" gives an empty-handed hero nothing to do, so the " +
                "offer is narrowed to the heroes who can.");
            Assert.That(ConsumerOfTheSelection("sentinel"), Is.Null,
                "\"discards a card, or is defeated\" is one step with two ways to complete, so " +
                "every enemy hero in the disk is a legal subject.");
        }

        private static string ConsumerOfTheSelection(string prefix)
        {
            var definition = SilverarrowEffectContent
                .TriggeredEffects(SilverarrowCardContent.ContentRevision)
                .Single(effect => effect.Nodes.Any(node => node.Id.Value == prefix + "-select"));
            var select = definition.Nodes.Single(node => node.Id.Value == prefix + "-select");
            return EffectSelectionDeclaration.TryReadConsumer(select, out var consumer)
                ? consumer.Value
                : null;
        }

        // -----------------------------------------------------------------------------------------
        // The drive.
        // -----------------------------------------------------------------------------------------

        private enum Face
        {
            WarningShot,
            TreetopSentinel,
            Both
        }

        private sealed class DriveResult
        {
            internal AdjacentMoveAuthority Board;
            internal CardInstance[] Instances;
            internal readonly List<string> PauseNodes = new List<string>();
            internal HeroId[] OfferedAtTheSelection = Array.Empty<HeroId>();
            internal int EnemyHandBefore;
            internal int EnemyHandAfter;
            internal bool EnemyOnBoardAfter;
            internal int CoinsBefore;
            internal int CoinsAfter;
            internal int KillsBefore;
            internal int KillsAfter;
            internal bool TurnEnded;
        }

        /// <summary>
        /// One turn's close with the named "End of turn:" occurrences standing. Silverarrow stands at
        /// (0,0), the enemy two spaces away - inside her printed Radius 3 - and the second enemy five
        /// spaces away, outside it, so the offer this file measures is exactly one hero wide.
        /// </summary>
        private static DriveResult Drive(
            Face face, bool enemyHasCards, int enemySpareCards = 1)
        {
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var warningShot = SilverarrowCardContent.Definition(SilverarrowCardContent.WarningShot);
            var sentinel = SilverarrowCardContent.Definition(SilverarrowCardContent.TreetopSentinel);
            var warningShotSkill = warningShot.ActionDefinitions.Single(
                action => action.Kind == ActionKind.Skill);
            var sentinelSkill = sentinel.ActionDefinitions.Single(
                action => action.Kind == ActionKind.Skill);

            var commit = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-commit"), warningShot.Reference, Silverarrow);
            var warningShotSource = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-warning-shot-source"), warningShot.Reference,
                Silverarrow);
            var sentinelSource = CardInstance.CreateInHand(
                new CardInstanceId("silverarrow-treetop-sentinel-source"), sentinel.Reference,
                Silverarrow);

            var enemyCards = new List<CardInstance>
            {
                CardInstance.CreateInHand(
                    new CardInstanceId("enemy-commit"), warningShot.Reference, Enemy)
            };
            if (enemyHasCards)
            {
                for (var spare = 0; spare < enemySpareCards; spare++)
                {
                    enemyCards.Add(CardInstance.CreateInHand(
                        new CardInstanceId("enemy-spare-" + spare), warningShot.Reference, Enemy));
                }
            }

            var farEnemyCards = new[]
            {
                CardInstance.CreateInHand(
                    new CardInstanceId("far-enemy-commit"), warningShot.Reference, FarEnemy),
                CardInstance.CreateInHand(
                    new CardInstanceId("far-enemy-spare"), warningShot.Reference, FarEnemy)
            };

            var instances = new[] { commit, warningShotSource, sentinelSource }
                .Concat(enemyCards).Concat(farEnemyCards)
                .ToArray();
            var definitions = new[] { warningShot, sentinel };

            var spaces = Enumerable.Range(0, 6).Select(At).ToArray();
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("silverarrow-treetop-sentinel-map"), 1, spaces);
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Silverarrow, At(0)),
                    new HeroOccupancy(Enemy, At(2)),
                    new HeroOccupancy(FarEnemy, At(5))
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            var build = MatchContentCatalogs.ForThisBuild();
            var capabilities = new EffectCapabilityCatalog(
                build.Capabilities.Registered.Select(build.Capabilities.Load)
                    .Concat(SilverarrowActiveEffectContent.Handlers())
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
            var catalogs = new MatchContentCatalogs(
                capabilities,
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(
                    SilverarrowEffectContent.TriggeredEffects(
                        SilverarrowCardContent.ContentRevision)));

            var markers = new MarkerAttachmentRegister();
            var timing = new ActiveEffectLifecycle();
            if (face == Face.WarningShot || face == Face.Both)
            {
                timing.Create(
                    new ActiveEffectId("silverarrow-warning-shot-standing"),
                    SilverarrowActiveEffectContent.WarningShotDefinition,
                    Silverarrow, TeamSide.Red, Silverarrow, warningShotSource.Id,
                    warningShotSkill.Id, 1, 1);
            }

            if (face == Face.TreetopSentinel || face == Face.Both)
            {
                timing.Create(
                    new ActiveEffectId("silverarrow-treetop-sentinel-standing"),
                    SilverarrowActiveEffectContent.TreetopSentinelDefinition,
                    Silverarrow, TeamSide.Red, Silverarrow, sentinelSource.Id,
                    sentinelSkill.Id, 1, 1);
            }

            var result = new DriveResult { Board = board, Instances = instances };

            var round = new RoundResolutionAuthority(
                definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                RoundResolutionComposition.ForMatch(
                    new CardActionResolutionAuthority(catalogs, board, markers),
                    board, timing, markers, null, null,
                    startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));
            result.CoinsBefore = round.CoinBalances
                .Single(balance => balance.Key.Equals(Silverarrow)).Value;
            result.KillsBefore = round.Scoreboard.For(Silverarrow).Kills;

            Assert.That(round.TryCommitCard(Silverarrow, commit.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Enemy, enemyCards[0].Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(FarEnemy, farEnemyCards[0].Id).Accepted, Is.True);
            result.EnemyHandBefore = HandCount(instances, Enemy);

            for (var step = 0; step < 60; step++)
            {
                var turn = round.CurrentTurnAuthority;
                if (turn == null || turn.IsEnded) break;

                if (turn.PendingChoice != null)
                {
                    var tie = turn.PendingChoice;
                    Assert.That(round.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(commit.Id)
                                ? commit.Id
                                : tie.CandidateCardInstanceIds[0]).Accepted,
                        Is.True);
                    continue;
                }

                var choice = turn.PendingEffectChoice;
                if (choice != null)
                {
                    result.PauseNodes.Add(choice.NodeId.Value);
                    if (choice.NodeId.Value.EndsWith("-select"))
                    {
                        result.OfferedAtTheSelection = choice.Candidates
                            .Select(candidate => candidate.Value.HeroId).ToArray();
                    }

                    var answered = round.TryAnswerEffectChoice(
                        choice.SelectInvocations.Count > 0
                            ? choice.SelectInvocations[0]
                            : choice.ConfirmInvocation);
                    Assert.That(answered.Accepted, Is.True,
                        "An end-of-turn trigger's question must be answerable when it is asked.");
                    if (answered.Events != null)
                    {
                        foreach (var value in answered.Events) round.Scoreboard.Observe(value);
                    }

                    continue;
                }

                if (turn.PendingTurnRelease != null)
                {
                    var released = round.TryReleaseTurn();
                    Assert.That(released.Accepted, Is.True);
                    if (released.Events != null)
                    {
                        foreach (var value in released.Events) round.Scoreboard.Observe(value);
                    }

                    continue;
                }

                if (!turn.SelectedActor.CardInstanceId.IsDefined) break;
                var held = round.TryPerformHold(turn.SelectedActor.CardInstanceId);
                Assert.That(held.Accepted, Is.True);
                if (held.Events != null)
                {
                    foreach (var value in held.Events) round.Scoreboard.Observe(value);
                }
            }

            result.EnemyHandAfter = HandCount(instances, Enemy);
            result.EnemyOnBoardAfter = board.Occupancies.Any(hero => hero.HeroId.Equals(Enemy));
            result.CoinsAfter = round.CoinBalances
                .Single(balance => balance.Key.Equals(Silverarrow)).Value;
            result.KillsAfter = round.Scoreboard.For(Silverarrow).Kills;
            result.TurnEnded = round.CurrentTurnAuthority == null ||
                round.CurrentTurnAuthority.IsEnded;
            return result;
        }

        private static int HandCount(IEnumerable<CardInstance> instances, HeroId hero) =>
            instances.Count(card =>
                card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);
    }
}
