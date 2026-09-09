using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class WhisperAttackRepeatTests
    {
        [TestCase(false, 2, false, false)]
        [TestCase(true, 6, false, false)]
        [TestCase(false, 2, true, false)]
        [TestCase(true, 6, true, false)]
        [TestCase(false, 1, false, true, TestName = "SwiftJustice_AdjacentBulletDoesNotRunTheRangedBulletsRider")]
        [TestCase(false, 1, true, true, TestName = "SwiftJustice_AdjacentBulletKeepsItsRiderScopeAfterRestore")]
        public void DiscardGatedAttack_PerformsItsPrintedRepeatCountOnDistinctTargets(
            bool frenzy, int expectedAttacks, bool restoreAtEveryPause, bool swiftJustice)
        {
            // Exact printed Whisper definitions; the six adjacent enemy units make every
            // permitted repeat possible. Surviving heroes expose illegal target reuse.
            var configuration = MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            var card = WhisperCardContent.Definition(swiftJustice ? WhisperCardContent.SwiftJustice :
                frenzy ? WhisperCardContent.BloodFrenzy : WhisperCardContent.BloodFury);
            var defense = WhisperCardContent.Definition(WhisperCardContent.BloodFrenzy);
            var played = CardInstance.CreateInHand(new CardInstanceId("repeat-source"), card.Reference, actor);
            var extra = new List<CardInstance> { played };
            foreach (var owner in new[] { enemy, ally })
            {
                for (var i = 0; i < 6; i++)
                    extra.Add(CardInstance.CreateInHand(new CardInstanceId(owner.Value + "-defense-" + i),
                        defense.Reference, owner));
            }
            var discarded = extra.First(value => value.OwnerId.Equals(enemy));
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(discarded), Is.True);
            var definitions = configuration.CardDefinitions.Concat(new[] { card, defense })
                .GroupBy(value => value.Reference).Select(group => group.First()).ToArray();
            var instances = configuration.CardInstances.Concat(extra).ToArray();
            var adjacent = new[]
            {
                new HexCoordinate(1, 0), new HexCoordinate(0, 1), new HexCoordinate(-1, 1),
                new HexCoordinate(-1, 0), new HexCoordinate(0, -1), new HexCoordinate(1, -1)
            };
            var coordinates = new List<HexCoordinate>();
            for (var q = -3; q <= 3; q++)
            for (var r = -3; r <= 3; r++)
                if (Math.Abs(q + r) <= 3) coordinates.Add(new HexCoordinate(q, r));
            var board = new AdjacentMoveAuthority(coordinates,
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, adjacent[0]), new HeroOccupancy(ally, adjacent[1])
                }, Enumerable.Empty<ObstacleOccupancy>(),
                Enumerable.Range(2, 4).Select(i => new QuickGameMinionSetupState(
                    new MinionId("repeat-minion-" + i), new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                    TeamSide.Blue, StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(new MapId("synthetic-coordinate-board"), new SpawnPointId("repeat-spawn-" + i)),
                    new ConfiguredSpaceReference(new MapId("synthetic-coordinate-board"),
                        new SpaceId($"q{adjacent[i].Q}_r{adjacent[i].R}")), adjacent[i])));
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(WhisperActiveEffectContent.Handlers()
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())),
                SyntheticComponentDefinitions.CreateCatalog(), TriggeredEffectCatalog.Empty);
            var actions = new CardActionResolutionAuthority(catalogs, board);
            foreach (var committed in new[] { played }.Concat(new[] { enemy, ally }.Select(owner =>
                configuration.CardInstances.First(value => value.OwnerId.Equals(owner) && value.State.Location == CardLocation.Hand))))
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
            }
            var timing = new ActiveEffectLifecycle();
            var turn = new TurnResolutionAuthority(definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), actions, timing, 1, 1);
            Assert.That(turn.TryStart().Accepted, Is.True);
            var started = false;
            var taken = new List<AttackTarget>();
            for (var step = 0; step < 100 && !turn.IsEnded; step++)
            {
                if (restoreAtEveryPause && started && actions.PendingAttackResolution != null)
                {
                    var exclusions = actions.PendingAttackResolution.ExcludedTargets.ToArray();
                    var turnState = typeof(TurnResolutionAuthority).GetMethod("CaptureRestoreState", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(turn, null);
                    using (var buffer = new MemoryStream())
                    {
                        var writer = new BinaryWriter(buffer);
                        typeof(AuthoritativeSnapshotSerializer).GetMethod("WriteCardAction", BindingFlags.Static | BindingFlags.NonPublic)
                            .Invoke(null, new object[] { writer, actions.CaptureRestoreState() });
                        writer.Flush();
                        buffer.Position = 0;
                        actions = (CardActionResolutionAuthority)typeof(AuthoritativeSnapshotSerializer)
                            .GetMethod("ReadCardAction", BindingFlags.Static | BindingFlags.NonPublic)
                            .Invoke(null, new object[] { new BinaryReader(buffer), board, definitions, instances,
                                configuration.Participants, catalogs, new MarkerAttachmentRegister(), timing, RuleModificationSet.Empty,
                                null, turn.TieBreakerState, 1, 1, null, actor });
                        Assert.That(buffer.Position, Is.EqualTo(buffer.Length));
                    }
                    Assert.That(actions.PendingAttackResolution.ExcludedTargets, Is.EqualTo(exclusions));
                    turn = (TurnResolutionAuthority)typeof(TurnResolutionAuthority).GetMethod("Restore", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new[] { definitions, (object)instances, configuration.Participants, actions, timing, turnState, null, false });
                }
                if (!started && turn.PendingChoice != null)
                {
                    var choice = turn.PendingChoice;
                    Assert.That(turn.TryChooseTiedActor(choice.OwningSide,
                        choice.CandidateCardInstanceIds.Contains(played.Id) ? played.Id : choice.CandidateCardInstanceIds[0]).Accepted, Is.True);
                }
                else if (!started && turn.PendingTurnRelease != null)
                    Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                else if (!started && turn.SelectedActor.CardInstanceId.IsDefined)
                {
                    if (!turn.SelectedActor.CardInstanceId.Equals(played.Id))
                        Assert.That(turn.TryPerformHold(turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                    else
                    {
                        var offer = ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, actor, played.Id)
                            .Single(value => value.ActionDefinitionId.Equals(card.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack).Id));
                        Assert.That(turn.TryStartAttack(new ActionResolutionId("whisper-repeat"), offer).Accepted, Is.True);
                        started = true;
                    }
                }
                else if (turn.PendingAttackTargetChoice != null)
                {
                    var choice = turn.PendingAttackTargetChoice;
                    Assert.That(choice.LegalTargets.Intersect(taken), Is.Empty, "Ruling 34: no earlier target may reappear.");
                    var target = choice.LegalTargets.OrderBy(value => value.Kind == AttackTargetKind.Hero ? 0 : 1).First();
                    taken.Add(target);
                    Assert.That(turn.TryAnswerAttackTarget(new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, target)).Accepted, Is.True);
                }
                else if (turn.PendingDefenseChoice != null)
                {
                    var choice = turn.PendingDefenseChoice;
                    var offer = choice.LegalDefenseOffers.First(value => extra.Any(instance => instance.Id.Equals(value.SourceCardInstanceId)));
                    Assert.That(turn.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                        choice.ResolutionId, choice.ChoiceId, offer.SourceCardInstanceId)).Accepted, Is.True);
                }
                else if (turn.PendingEffectChoice != null)
                {
                    var choice = turn.PendingEffectChoice;
                    if (swiftJustice)
                    {
                        Assert.That(taken, Is.Empty, "The adjacent bullet has no after-Attack forced-movement rider.");
                        if (choice.SelectedCandidates.Count > 0)
                            Assert.That(turn.TryAnswerEffectChoice(choice.ConfirmInvocation).Accepted, Is.True);
                        else
                        {
                            Assert.That(choice.SelectInvocations, Has.Count.EqualTo(2), "Both printed options must be available before selecting the adjacent option.");
                            Assert.That(turn.TryAnswerEffectChoice(choice.SelectInvocations.Last()).Accepted, Is.True);
                        }
                    }
                    else Assert.That(turn.TryAnswerEffectChoice(choice.SelectInvocations.Count > 0
                        ? choice.SelectInvocations[0] : choice.ConfirmInvocation).Accepted, Is.True);
                }
                else break;
            }
            Assert.That(started, Is.True);
            Assert.That(taken.Count, Is.EqualTo(expectedAttacks), "The maximum is printed on the card, not a universal single-repeat cap.");
        }

        [Test]
        public void RestoringAnUpgradedConfiguration_PreservesItsCardInstanceOrder()
        {
            var configuration = MatchConfiguration.CreateStagedWhisperTierThreeForInternalTest(
                TeamSide.Red, WhisperUpgradeBranch.SecondSibling);
            var restored = MatchConfiguration.Restore(new MatchConfigurationRestoreState(
                configuration.Participants, configuration.CardDefinitions, configuration.CardInstances,
                configuration.TieBreakerState, configuration.SetupSeed));
            Assert.That(restored.CardInstances.Select(card => card.Id),
                Is.EqualTo(configuration.CardInstances.Select(card => card.Id)));
        }
    }
}
