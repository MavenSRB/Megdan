using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class NebKherLaughterFoundationTests
    {
        [Test]
        public void OnlyAnAppliedLaughLeafProducesTheTypedActionFact()
        {
            var laugh = new EffectNodeId("laugh");
            var definition = new EffectDefinition(
                new EffectDefinitionId("laughter-foundation"), "rev-1", laugh,
                new[] { new EffectNodeDefinition(
                    laugh, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    LaughterCapabilities.LaughV1) });
            Assert.That(LaughterOutcomeQuery.Laughed(
                definition, new[] { new EffectNodeOutcome(
                    laugh, EffectExecutionResultKind.Applied) }), Is.True);
            Assert.That(LaughterOutcomeQuery.Laughed(
                definition, new[] { new EffectNodeOutcome(
                    laugh, EffectExecutionResultKind.DeclinedOptional) }), Is.False);
            Assert.That(LaughterOutcomeQuery.Laughed(definition,
                Array.Empty<EffectNodeOutcome>()), Is.False);
        }

        [Test]
        public void EachLaughStopsBeforeTheNextEffectNode()
        {
            var root = new EffectNodeId("root");
            var first = new EffectNodeId("first-laugh");
            var second = new EffectNodeId("second-laugh");
            var definition = new EffectDefinition(
                new EffectDefinitionId("two-laughs"), "rev-1", root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { first, second }),
                    new EffectNodeDefinition(first, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, LaughterCapabilities.LaughV1),
                    new EffectNodeDefinition(second, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, LaughterCapabilities.LaughV1)
                });
            var context = new EffectResolutionContext(
                definition, new EffectCapabilityCatalog(new[] { LaughterCapabilities.Handler }),
                StagedBoardParticipants.Create(), null, 1, 1,
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                EffectCardState.Absent, ActionKind.Skill, watchesLaughter: true);
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var started = EffectResolutionSession.Start(
                new EffectResolutionId("two-laugh-run"),
                new CardInstanceId("two-laugh-card"), default,
                new ActionDefinitionId("two-laugh-action"),
                actor, actor, ActionInvocationContext.PrintedPrimary, context);
            Assert.That(started.IsComplete, Is.False);
            Assert.That(started.LaughMoment, Is.True);
            Assert.That(started.Frame.CompletedOutcomes.Count(outcome =>
                outcome.NodeId.Equals(first)), Is.EqualTo(1));
            Assert.That(started.Frame.CompletedOutcomes.Any(outcome =>
                outcome.NodeId.Equals(second)), Is.False,
                "The Ultimate must run before the second effect node.");

            var resumed = EffectResolutionSession.Continue(started.Frame, context);
            Assert.That(resumed.IsComplete, Is.False);
            Assert.That(resumed.LaughMoment, Is.True);
            Assert.That(resumed.Frame.CompletedOutcomes.Any(outcome =>
                outcome.NodeId.Equals(second)), Is.True);

            var finished = EffectResolutionSession.Continue(resumed.Frame, context);
            Assert.That(finished.IsComplete, Is.True);
            Assert.That(finished.LaughedDiabolically, Is.True);
            Assert.That(finished.LaughMoment, Is.False);
        }

        [Test]
        public void DeclinedOptionalLaughCreatesNoMomentButTakingItCreatesOne()
        {
            var root = new EffectNodeId("may-laugh");
            var leaf = new EffectNodeId("actual-laugh");
            var definition = new EffectDefinition(
                new EffectDefinitionId("optional-laugh"), "rev-1", root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Optional,
                        EffectFailurePolicy.May, childNodeIds: new[] { leaf },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(leaf, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, LaughterCapabilities.LaughV1)
                });
            var context = new EffectResolutionContext(
                definition, new EffectCapabilityCatalog(new[] { LaughterCapabilities.Handler }),
                StagedBoardParticipants.Create(), null, 1, 1,
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                EffectCardState.Absent, ActionKind.Skill, watchesLaughter: true);
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            EffectResolutionStepResult Start(string id) => EffectResolutionSession.Start(
                new EffectResolutionId(id), new CardInstanceId("optional-laugh-card"),
                default, new ActionDefinitionId("optional-laugh-action"), actor, actor,
                ActionInvocationContext.PrintedPrimary, context);

            var declinedStart = Start("optional-laugh-declined");
            Assert.That(declinedStart.LaughMoment, Is.False);
            var declined = EffectResolutionSession.Answer(
                declinedStart.Frame, declinedStart.Frame.PendingChoice.DeclineInvocation,
                context);
            Assert.That(declined.IsComplete, Is.True);
            Assert.That(declined.LaughMoment, Is.False);
            Assert.That(declined.LaughedDiabolically, Is.False);

            var takenStart = Start("optional-laugh-taken");
            var taken = EffectResolutionSession.Answer(
                takenStart.Frame, takenStart.Frame.PendingChoice.ConfirmInvocation,
                context);
            Assert.That(taken.IsComplete, Is.False);
            Assert.That(taken.LaughMoment, Is.True);
            Assert.That(EffectResolutionSession.Continue(taken.Frame, context)
                .LaughedDiabolically, Is.True);
        }

        [Test]
        public void AuthorityRunsEachLaughTriggerBeforeTheFollowingSkillNode()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var participants = StagedBoardParticipants.Create();
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    new MapId("laugh-authority-map"), 1,
                    new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) }),
                new[] { new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(1, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var root = new EffectNodeId("laugh-action-root");
            var first = new EffectNodeId("laugh-first");
            var second = new EffectNodeId("laugh-second");
            var actionEffect = new EffectDefinition(
                new EffectDefinitionId("laugh-action-effect"), "rev-1", root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { first, second }),
                    new EffectNodeDefinition(first, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, LaughterCapabilities.LaughV1),
                    new EffectNodeDefinition(second, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, LaughterCapabilities.LaughV1)
                });
            var triggerRoot = new EffectNodeId("laugh-trigger-choice");
            var triggerLeaf = new EffectNodeId("laugh-trigger-note");
            var triggerEffect = new EffectDefinition(
                new EffectDefinitionId("laugh-trigger-effect"), "rev-1", triggerRoot,
                new[]
                {
                    new EffectNodeDefinition(triggerRoot, EffectNodeKind.Optional,
                        EffectFailurePolicy.May, childNodeIds: new[] { triggerLeaf },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(triggerLeaf, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.NoteActorV1,
                        outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) })
                });
            var actionId = new ActionDefinitionId("laugh-primary");
            var skill = new CardDefinition(
                new CardDefinitionId("laugh-skill-card"), "rev-1", "Laugh Skill",
                new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(actionId, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, actionEffect)
                });
            var ultimate = new CardDefinition(
                new CardDefinitionId("laugh-ultimate-card"), "rev-1", "Laugh Ultimate",
                default, CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.TriggeredAbility(
                    "laugh-ultimate-rule", "LaughUltimate", triggerEffect.Reference,
                    UltimateTriggerCondition.AfterControllerLaughsDiabolically,
                    new[] { TimingCheckpoint.LaughedDiabolically },
                    Array.Empty<PrintedValueDefinition>()));
            var skillCard = CardInstance.CreateInHand(
                new CardInstanceId("laugh-skill-instance"), skill.Reference, actor);
            var ultimateCard = CardInstance.CreateInDeck(
                new CardInstanceId("laugh-ultimate-instance"), ultimate.Reference, actor);
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimateCard), Is.True);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(skillCard), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(skillCard), Is.True);
            var definitions = new[] { skill, ultimate };
            var instances = new[] { skillCard, ultimateCard };
            var defaults = MatchContentCatalogs.ForThisBuild();
            var catalogs = new MatchContentCatalogs(
                new EffectCapabilityCatalog(
                    defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)),
                defaults.Components,
                new TriggeredEffectCatalog(
                    defaults.TriggeredEffects.Definitions.Concat(new[] { triggerEffect })));
            var authority = new CardActionResolutionAuthority(
                catalogs, board, new MarkerAttachmentRegister(),
                new HeroProgressionState(participants),
                new GuardsDigital.Core.Rules.Consequences.TeamLifeState(participants, 8));
            var timing = new ActiveEffectLifecycle();
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                definitions, instances, actor, skillCard.Id)
                .Single(candidate => candidate.ActionDefinitionId.Equals(actionId));
            var started = authority.StartTurnSkill(
                new ActionResolutionId("laugh-action-run"), offer,
                definitions, instances, participants, timing,
                GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                TieBreakerState.Unrevealed, 1, 1);
            Assert.That(started.Accepted, Is.True, started.RejectionReason.ToString());
            Assert.That(authority.PendingEffectInterrupt?.Kind,
                Is.EqualTo(EffectInterruptKind.AfterLaughTrigger));

            var boundary = new ActionBoundaryContext(
                definitions, instances, participants, timing,
                TieBreakerState.Unrevealed, 1, 1);
            var firstChoice = authority.PendingEffectChoice;
            Assert.That(firstChoice.ChooserId, Is.EqualTo(actor));
            var afterFirst = authority.AnswerEffectInterruptChoice(
                firstChoice.DeclineInvocation, boundary);
            Assert.That(afterFirst.Accepted, Is.True);
            Assert.That(authority.PendingEffectInterrupt?.Kind,
                Is.EqualTo(EffectInterruptKind.AfterLaughTrigger),
                "The second laugh is reached only after the first trigger completes.");
            Assert.That(authority.PendingEffectChoice.ResolutionId,
                Is.Not.EqualTo(firstChoice.ResolutionId),
                "The same choice node is a new occurrence on the second laugh.");

            var afterSecond = authority.AnswerEffectInterruptChoice(
                authority.PendingEffectChoice.DeclineInvocation, boundary);
            Assert.That(afterSecond.Accepted, Is.True);
            Assert.That(authority.PendingEffectInterrupt, Is.Null);
            Assert.That(authority.PendingEffectChoice, Is.Null);
        }

        [Test]
        public void RepeatedAndBorrowedBasicSkillFactsEachCarryTheirOwnLaugh()
        {
            var actor = new HeroId("laughter-actor");
            var card = new CardInstanceId("laughter-card");
            var action = new ActionDefinitionId("laughter-action");
            var first = PerformedBasicAction.Of(
                actor, card, action, ActionKind.Skill,
                laughedDiabolically: true);
            var repeated = PerformedBasicAction.Of(
                actor, card, action, ActionKind.Skill,
                wasRepeatedInstance: true, laughedDiabolically: true);
            var borrowed = PerformedBasicAction.Of(
                actor, card, action, ActionKind.Skill,
                laughedDiabolically: true, wasCardResolution: false);
            var declined = PerformedBasicAction.Of(
                actor, card, action, ActionKind.Skill);
            Assert.That(first.LaughedDiabolically, Is.True);
            Assert.That(repeated.LaughedDiabolically, Is.True);
            Assert.That(borrowed.LaughedDiabolically, Is.True);
            Assert.That(declined.LaughedDiabolically, Is.False);
            Assert.That((int)UltimateTriggerCondition.AfterControllerLaughsDiabolically,
                Is.EqualTo(13));
        }

        [Test]
        public void LaughMomentArmsTheUltimateAndActionEndCannotFireItAgain()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var definition = new CardDefinition(
                new CardDefinitionId("laughter-ultimate"), "rev-1", "TEST - Laughter Ultimate",
                default, CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.TriggeredAbility(
                    "laughter-ultimate-rule", "LaughterUltimate",
                    SyntheticShowcaseHand.AfterMarkerGivenTriggeredEffect.Reference,
                    UltimateTriggerCondition.AfterControllerLaughsDiabolically,
                    new[] { TimingCheckpoint.LaughedDiabolically },
                    Array.Empty<PrintedValueDefinition>()));
            var card = CardInstance.CreateInDeck(
                new CardInstanceId("laughter-ultimate-instance"), definition.Reference, actor);
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(card), Is.True);
            var unlocked = UltimateUnlockQuery.Derive(new[] { definition }, new[] { card });
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("laughter-ultimate-map"), 1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) });
            var board = new AdjacentMoveAuthority(map,
                new[] { new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(other, new HexCoordinate(1, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var participants = StagedBoardParticipants.Create();
            var sourceCard = new CardInstanceId("laughter-action-card");
            var action = new ActionDefinitionId("laughter-action");

            Assert.That(AfterLaughTriggerQuery.Derive(unlocked, board, other), Is.Empty,
                "Another hero's laugh is not NebKher's.");
            Assert.That(AfterLaughTriggerQuery.AnyWatching(unlocked), Is.True);
            Assert.That(AfterLaughTriggerQuery.Derive(unlocked, board, actor).Count,
                Is.EqualTo(1));
            Assert.That(UltimateTriggeredEffectSourceQuery.Derive(
                unlocked, board, participants,
                PerformedBasicAction.Of(actor, sourceCard, action, ActionKind.Skill,
                    laughedDiabolically: true)), Is.Empty,
                "The completed-action fact must not trigger a second time.");
        }
    }
}
