using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8a. Red Bulwark's printed Primary Attack is replaced by one three-mode Skill whose
    /// source is the card and whose affected object is always the acting hero.
    ///
    /// Everything the modes do comes from milestone 6.8's registered contracts; this fixture proves the
    /// binding, not the contracts. What is new here is that the three delayed lifetimes now happen in an
    /// ordinary match, driven by a card a player holds: a `This Turn` value modifier that a later enemy
    /// Attack in the same turn must see, a `This Round` restriction that outlives the turn but not the
    /// round, and a `Next Turn` permission that lies dormant until its turn arrives.
    ///
    /// A turn is one Initiative pass in which every committed card resolves, and a round is four such
    /// turns (<see cref="RoundResolutionAuthority.TurnsPerRound"/>). That is why an enemy can Attack the
    /// acting hero inside the same turn its Skill resolved, and why a `This Round` effect can span
    /// several turns.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class ConfiguredModificationSkillTests
    {
        private static readonly CardInstanceId BulwarkCardId = new CardInstanceId("red-bulwark-card");

        private static readonly ActionDefinitionId BulwarkSkill =
            new ActionDefinitionId("primary-modification-skill");

        private static readonly EffectNodeId DefenseMode = new EffectNodeId("bulwark-defense-bonus");
        private static readonly EffectNodeId RestrictionMode =
            new EffectNodeId("bulwark-fast-travel-restriction");
        private static readonly EffectNodeId PermissionMode =
            new EffectNodeId("bulwark-move-through-permission");

        // ---------------------------------------------------------------------------------------
        // Definition
        // ---------------------------------------------------------------------------------------

        [Test]
        public void RedBulwark_ReplacesOnlyItsPrintedPrimaryAttack()
        {
            var hand = PlaceholderMovementHand.CreateRed(new HeroId("red-synthetic-hero"));
            var definition = hand.Definitions.Single(candidate =>
                candidate.DisplayName == "TEST - Red Bulwark");
            var instance = hand.Instances[1];

            Assert.That(definition.Id, Is.EqualTo(new CardDefinitionId("placeholder-red-bulwark")),
                "The conversion advances the revision, never the card's identity.");
            Assert.That(definition.ContentRevision, Is.EqualTo("phase-7-1-rev-1"));
            Assert.That(definition.ContentAuthority,
                Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(definition.Initiative.Value, Is.EqualTo(11));
            Assert.That(instance.DefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(hand.DealtDefinitions, Has.Count.EqualTo(5), "The hand keeps its five-card shape.");

            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var movement = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedSecondary);
            var defense = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.Reaction);

            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(primary.Id, Is.EqualTo(BulwarkSkill));
            Assert.That(definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Attack),
                Is.False, "The printed Attack is replaced, not kept beside the Skill.");
            Assert.That(movement.Kind, Is.EqualTo(ActionKind.Movement));
            Assert.That(movement.BaseValues.Single().Amount, Is.EqualTo(2));
            Assert.That(defense.Kind, Is.EqualTo(ActionKind.Defense));
            Assert.That(defense.BaseValues.Single().Amount, Is.EqualTo(3));
        }

        [Test]
        public void TheSkill_DeclaresThreeModesEachNamingOneRegisteredCapability()
        {
            var hand = PlaceholderMovementHand.CreateRed(new HeroId("red-synthetic-hero"));
            var effect = hand.Definitions
                .Single(candidate => candidate.DisplayName == "TEST - Red Bulwark")
                .ActionDefinitions.Single(action => action.Id.Equals(BulwarkSkill))
                .EffectRoot;

            var root = effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(root.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "The acting player picks the mode; the resolver never picks for a player.");
            Assert.That(root.MinimumSelections, Is.EqualTo(1));
            Assert.That(root.MaximumSelections, Is.EqualTo(1));
            Assert.That(root.ChildNodeIds,
                Is.EqualTo(new[] { DefenseMode, RestrictionMode, PermissionMode }));

            var capabilities = new Dictionary<EffectNodeId, EffectCapabilityReference>
            {
                [DefenseMode] = SyntheticModificationCapabilities.GrantDefenseBonusV1,
                [RestrictionMode] = SyntheticModificationCapabilities.RestrictFastTravelV1,
                [PermissionMode] = SyntheticModificationCapabilities.PermitMoveThroughObstaclesV1
            };
            foreach (var pair in capabilities)
            {
                var mode = effect.Nodes.Single(node => node.Id.Equals(pair.Key));
                Assert.That(mode.Kind, Is.EqualTo(EffectNodeKind.Capability));
                Assert.That(mode.Capability, Is.EqualTo(pair.Value),
                    "A mode names a capability milestone 6.8 registered; it declares no rule itself.");
                Assert.That(mode.Outputs.Single().OutputName, Is.EqualTo("affected"));
                Assert.That(mode.Outputs.Single().ValueKind, Is.EqualTo(EffectValueKind.Hero));
            }

            Assert.That(
                EffectDefinitionValidator.Validate(effect, SyntheticEffectCapabilities.CreateCatalog()),
                Is.Empty);
        }

        [Test]
        public void EachMode_DeclaresExactlyOneModificationOfOneFamilyUnderItsOwnDuration()
        {
            var expected = new[]
            {
                new
                {
                    Definition = SyntheticModificationCapabilities.DefenseBonusDefinition,
                    Kind = RuleModificationKind.ValueModifier,
                    Duration = ActiveEffectDurationKind.ThisTurn
                },
                new
                {
                    Definition = SyntheticModificationCapabilities.FastTravelRestrictionDefinition,
                    Kind = RuleModificationKind.Restriction,
                    Duration = ActiveEffectDurationKind.ThisRound
                },
                new
                {
                    Definition = SyntheticModificationCapabilities.MoveThroughPermissionDefinition,
                    Kind = RuleModificationKind.Permission,
                    Duration = ActiveEffectDurationKind.NextTurn
                }
            };

            foreach (var mode in expected)
            {
                Assert.That(mode.Definition.Modifications, Has.Count.EqualTo(1),
                    "No mode combines two rule-source families to save a mode.");
                Assert.That(mode.Definition.Modifications.Single().Kind, Is.EqualTo(mode.Kind));
                Assert.That(mode.Definition.DurationKind, Is.EqualTo(mode.Duration));
                Assert.That(mode.Definition.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero),
                    "The affected object is the acting hero, never the source card.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // Mode selection through the ordinary match
        // ---------------------------------------------------------------------------------------

        [Test]
        public void StartingTheSkill_OpensAChooserOwnedSelectionAmongItsThreeModes()
        {
            var session = StartBulwarkSkill(CreateConfiguredSession(), out var actorId);

            var choice = session.Projection.PendingEffectChoice;

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(choice.ChooserId, Is.EqualTo(actorId));
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { DefenseMode, RestrictionMode, PermissionMode }),
                "Modes are offered in the definition's own order.");
        }

        [TestCase("bulwark-defense-bonus", "synthetic-defense-bonus-effect")]
        [TestCase("bulwark-fast-travel-restriction", "synthetic-fast-travel-restriction-effect")]
        [TestCase("bulwark-move-through-permission", "synthetic-move-through-permission-effect")]
        public void EachMode_CreatesItsOneEffectOnTheActingHeroSourcedToTheCard(
            string modeNodeId,
            string expectedDefinitionId)
        {
            var session = StartBulwarkSkill(CreateConfiguredSession(), out var actorId);

            ChooseMode(session, new EffectNodeId(modeNodeId));

            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
            var effect = RestoreTiming(session).Effects.Single();
            Assert.That(effect.Definition.DefinitionId, Is.EqualTo(expectedDefinitionId));
            Assert.That(effect.ControllerHeroId, Is.EqualTo(actorId),
                "The acting hero is the affected rules object.");
            Assert.That(effect.SourceCardInstanceId, Is.EqualTo(BulwarkCardId),
                "The card stays the stable source of the effect it created.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BulwarkCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        // ---------------------------------------------------------------------------------------
        // The three delayed lifetimes
        // ---------------------------------------------------------------------------------------

        [Test]
        public void DefenseMode_RaisesTheActingHerosDefenseAgainstALaterEnemyAttackInTheSameTurn()
        {
            // Red Bulwark at Initiative 11 outranks Blue Hammer at 6, so Red resolves its Skill first
            // and the Blue hero then Attacks it inside the same turn, which is where a `This Turn`
            // modifier on the acting hero has to be visible.
            var session = CreateAdjacentSession();
            var actorId = CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, DefenseMode);

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                ActionKindOf(session.Projection, offer) == ActionKind.Attack);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, actorId));

            var defenseChoice = session.Projection.PendingDefenseChoice;
            Assert.That(defenseChoice.ChooserId, Is.EqualTo(actorId),
                "The hero whose Skill granted the bonus is the one defending.");
            var defenseOffer = defenseChoice.LegalDefenseOffers[0];
            var printed = PrintedValueQuery.CalculatePrintedValue(
                defenseOffer, session.Projection.Definitions, PrintedValueChannel.Defense);

            // The projected preview is what a passive presenter reads, so asserting there proves the
            // modifier reaches presentation and the resolver through the same composed value.
            var preview = session.Projection.PendingDefensePreview.Choices.Single(choice =>
                choice.CardInstanceId.HasValue &&
                choice.CardInstanceId.Value.Equals(defenseOffer.SourceCardInstanceId));
            Assert.That(preview.DefenseTotal, Is.EqualTo(printed.Total + 1),
                "The Skill's own `This Turn` modifier reaches a later enemy Attack in that turn.");
            var contribution = preview.DefenseContributions.Single(value =>
                value.Kind == ValueContributionKind.ActiveEffectModifier);
            Assert.That(contribution.Amount, Is.EqualTo(1));
            Assert.That(contribution.Contribution.SourceActiveEffectId.Value,
                Does.Contain(BulwarkCardId.Value),
                "The contribution still names the card that supplied it.");

            var answered = session.Execute(DirectorCommand.ChooseDefenseCard(
                defenseChoice.ResolutionId,
                defenseChoice.ChoiceId,
                defenseOffer.SourceCardInstanceId));

            Assert.That(answered.Accepted, Is.True, answered.Detail);
            Assert.That(
                answered.Events
                    .Select(ruleEvent => ruleEvent.TurnEvent.CardActionEvent)
                    .Where(cardEvent =>
                        cardEvent.Kind == CardActionResolutionEventKind.DefenseTotalCalculated)
                    .Select(cardEvent => cardEvent.DefenseTotal),
                Is.EqualTo(new[] { printed.Total + 1 }),
                "The recorded Defense Total is the modified one, not the printed icon alone.");
        }

        [Test]
        public void RestrictionMode_WithholdsFastTravelForTheRestOfTheRoundAndReturnsItAfterRoundEnd()
        {
            var session = CreateConfiguredSession();
            var actorId = CommitBulwarkTurn(session, "blue-hammer-card");
            Assert.That(FastTravelOfferCount(session, actorId), Is.GreaterThan(0),
                "The fixture must have a Fast-Travel offer to withhold, or it proves nothing.");

            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, RestrictionMode);
            CompleteCurrentTurn(session);

            Assert.That(IsRestricted(session, actorId, RuleActivity.FastTravel), Is.True);
            CommitOrdinaryTurn(session, actorId);
            Assert.That(FastTravelOfferCount(session, actorId), Is.EqualTo(0),
                "A restriction naming Fast Travel withdraws the offer on a later turn of the round.");
            Assert.That(session.Projection.AvailableTurnActions, Is.Not.Empty,
                "Only the named activity is withheld; the hero's other offers remain.");

            AdvanceToNextRound(session);

            Assert.That(IsRestricted(session, actorId, RuleActivity.FastTravel), Is.False,
                "A `This Round` restriction ends at Round End and never crosses the boundary.");
            CommitOrdinaryTurn(session, actorId);
            Assert.That(FastTravelOfferCount(session, actorId), Is.GreaterThan(0));
        }

        [Test]
        public void PermissionMode_StaysDormantUntilTheNextTurnAndPermitsTransitButNeverAnEndpoint()
        {
            var session = CreateConfiguredSession();
            var actorId = CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, PermissionMode);

            Assert.That(RestoreTiming(session).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Dormant));
            Assert.That(HasMoveThroughPermission(session, actorId), Is.False,
                "A dormant `Next Turn` effect supplies no rule during the turn that created it.");

            CompleteCurrentTurn(session);
            CommitOrdinaryTurn(session, actorId);

            Assert.That(RestoreTiming(session).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Active));
            Assert.That(HasMoveThroughPermission(session, actorId), Is.True,
                "It comes into effect at the start of that hero's next turn.");

            // Another unit is an obstacle (p.18), so the enemy hero's own space is the exact question
            // the permission answers.
            var board = RestoreBoard(session);
            var mover = RuleObject.ForHero(actorId);
            var enemyId = session.Projection.Participants.Heroes
                .Select(hero => hero.Id)
                .First(heroId => !heroId.Equals(actorId));
            Assert.That(board.TryGetHeroCoordinate(enemyId, out var occupied), Is.True);
            var modifications = Modifications(session);
            Assert.That(ObstacleQuery.IsObstacle(board, occupied), Is.True);
            Assert.That(ObstacleQuery.BlocksTransit(board, occupied, mover, modifications), Is.False,
                "Rulebook p.18: a unit ignoring obstacles may enter and leave an occupied space.");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(board, occupied), Is.True,
                "p.18 allows no unit to end its movement on an obstacle, permission or not.");

            CompleteCurrentTurn(session);
            CommitOrdinaryTurn(session, actorId);

            Assert.That(HasMoveThroughPermission(session, actorId), Is.False,
                "It expires with that one turn rather than persisting.");
        }

        /// <summary>
        /// Milestone 6.8c's acceptance: the configured card's `Next Turn` mode is what a player can
        /// actually observe. Until 6.8c the permission had no Movement consumer at all, so selecting
        /// this mode changed nothing anyone could see.
        /// </summary>
        [Test]
        public void PermissionMode_LetsTheHeroMoveThroughAnOccupiedSpaceOnItsNextTurn()
        {
            var session = CreateBlockedLineSession();
            var actorId = CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, PermissionMode);
            CompleteCurrentTurn(session);
            CommitOrdinaryTurn(session, actorId);

            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                ActionKindOf(session.Projection, offer) == ActionKind.Movement);
            Accept(session, DirectorCommand.StartAction(movement.ActionDefinitionId));

            var choice = session.Projection.PendingMovementChoice;
            var beyond = new HexCoordinate(2, 0);
            var occupied = new HexCoordinate(1, 0);
            Assert.That(choice.LegalDestinations.Select(value => value.Destination),
                Has.Member(beyond),
                "The space beyond the enemy is offered only because the Skill granted the permission.");
            Assert.That(choice.LegalDestinations.Select(value => value.Destination),
                Has.No.Member(occupied),
                "The occupied space itself is never offered as somewhere to stop.");

            Accept(session, DirectorCommand.ChooseMovementDestination(
                choice.ResolutionId, choice.ChoiceId, beyond));

            var board = RestoreBoard(session);
            Assert.That(board.Occupancies.Single(value => value.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(beyond), "The hero really passed through and stopped beyond.");
            Assert.That(board.IsEmptyPlayableSpace(occupied), Is.False,
                "The unit it passed through is still standing there.");
        }

        [Test]
        public void WithoutTheSkill_TheSameHeroCannotReachBeyondTheOccupiedSpace()
        {
            // The same board and the same turn shape, with the mode never chosen. It is the control
            // that shows the previous case measures the permission rather than the fixture.
            var session = CreateBlockedLineSession();
            var actorId = CommitBulwarkTurn(session, "blue-hammer-card");
            CompleteCurrentTurn(session);
            CommitOrdinaryTurn(session, actorId);

            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                ActionKindOf(session.Projection, offer) == ActionKind.Movement);
            Accept(session, DirectorCommand.StartAction(movement.ActionDefinitionId));

            Assert.That(
                session.Projection.PendingMovementChoice.LegalDestinations
                    .Select(value => value.Destination),
                Has.No.Member(new HexCoordinate(2, 0)));
        }

        // ---------------------------------------------------------------------------------------
        // Determinism across the delayed boundary
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AChosenMode_SurvivesSnapshotRestoreReplayAndRetainedPrefixUndo()
        {
            var session = CreateConfiguredSession();
            var actorId = CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, PermissionMode);
            CompleteCurrentTurn(session);
            CommitOrdinaryTurn(session, actorId);

            var snapshot = session.Projection.AuthoritativeSnapshot;
            var diagnostic = session.Projection.DiagnosticSnapshot;
            var commands = session.Projection.AcceptedCommands.ToArray();

            var restored = CreateConfiguredSession();
            Assert.That(restored.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(HasMoveThroughPermission(restored, actorId), Is.True,
                "Restore rebinds the effect to the trusted catalog and it supplies the same rule.");

            var replayed = CreateConfiguredSession();
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    "Every accepted command must remain acceptable on replay.");
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(replayed.Projection.DiagnosticSnapshot, Is.EqualTo(diagnostic));

            Assert.That(session.Undo().Accepted, Is.True);
            var reExecuted = session.Execute(commands.Last());
            Assert.That(reExecuted.Accepted, Is.True, reExecuted.Detail);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot),
                "The retained prefix replays to exactly the state it produced.");
        }

        [Test]
        public void AStaleOrForgedModeAnswer_RejectsWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartBulwarkSkill(CreateConfiguredSession(), out _);
            var choice = session.Projection.PendingEffectChoice;
            var snapshot = session.Projection.AuthoritativeSnapshot;
            var commandCount = session.Projection.AcceptedCommands.Count;

            var stale = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                new EffectChoiceId("stale-mode-choice"),
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(DefenseMode))));
            Assert.That(stale.Accepted, Is.False);

            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(new EffectNodeId("bulwark-no-such-mode")))));
            Assert.That(forged.Accepted, Is.False);

            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(commandCount));
            Assert.That(session.Projection.PendingEffectChoice.Candidates,
                Is.EqualTo(choice.Candidates));
            Assert.That(RestoreTiming(session).Effects, Is.Empty,
                "A refused mode answer creates no effect.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BulwarkCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        // ---------------------------------------------------------------------------------------
        // Milestone 6.8d: a replay of the same mode is a fresh activation
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Round End retrieval cancels the round-1 effect, so round 2 starts from a spent instance.
        /// Before 6.8d the capability composed its instance ID from card, node, and definition alone,
        /// all of which repeat here, and the lifecycle refuses a duplicate ID including a spent one -
        /// so the second play of the same mode silently created nothing.
        /// </summary>
        [TestCase("bulwark-defense-bonus")]
        [TestCase("bulwark-fast-travel-restriction")]
        [TestCase("bulwark-move-through-permission")]
        public void PlayingTheSameModeInALaterRound_CreatesAFreshEffectInstance(string modeNodeId)
        {
            var mode = new EffectNodeId(modeNodeId);
            var session = CreateConfiguredSession();
            CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, mode);
            CompleteCurrentTurn(session);
            AdvanceToNextRound(session);

            Assert.That(RestoreTiming(session).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Ended),
                "The round-1 instance must be spent before the replay, or this proves nothing " +
                "about spent-instance collisions.");

            CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, mode);

            var effects = RestoreTiming(session).Effects;
            Assert.That(effects, Has.Count.EqualTo(2),
                "Playing the card again is a new activation and must create a fresh instance; a " +
                "spent instance from round 1 is a different activation and must not suppress it.");
            Assert.That(effects.Select(effect => effect.Id).Distinct().Count(), Is.EqualTo(2),
                "The two activations carry distinct instance identities.");
            var replayed = effects.Single(effect => effect.CreatedRoundIndex == 2);
            Assert.That(replayed.Phase, Is.Not.EqualTo(ActiveEffectPhase.Ended),
                "The fresh instance begins its own lifecycle rather than inheriting the spent one.");
            Assert.That(replayed.SourceCardInstanceId, Is.EqualTo(BulwarkCardId));
        }

        [Test]
        public void ASecondActivation_SurvivesSnapshotRestoreReplayAndRetainedPrefixUndo()
        {
            var session = CreateConfiguredSession();
            CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, RestrictionMode);
            CompleteCurrentTurn(session);
            AdvanceToNextRound(session);
            CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            ChooseMode(session, RestrictionMode);

            var snapshot = session.Projection.AuthoritativeSnapshot;
            var diagnostic = session.Projection.DiagnosticSnapshot;
            var commands = session.Projection.AcceptedCommands.ToArray();

            var restored = CreateConfiguredSession();
            Assert.That(restored.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(RestoreTiming(restored).Effects, Has.Count.EqualTo(2),
                "Both instances cross the snapshot boundary and restore rebinds both.");

            var replayed = CreateConfiguredSession();
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    "Every accepted command must remain acceptable on replay.");
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(replayed.Projection.DiagnosticSnapshot, Is.EqualTo(diagnostic));

            Assert.That(session.Undo().Accepted, Is.True);
            var reExecuted = session.Execute(commands.Last());
            Assert.That(reExecuted.Accepted, Is.True, reExecuted.Detail);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot),
                "The retained prefix replays to exactly the state it produced.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static MatchSession StartBulwarkSkill(MatchSession session, out HeroId actorId)
        {
            actorId = CommitBulwarkTurn(session, "blue-hammer-card");
            StartBulwarkSkillOnCurrentTurn(session);
            return session;
        }

        /// <summary>
        /// Commits Red Bulwark against a lower-Initiative Blue card, so the Red hero acts first and the
        /// Blue hero still has an unresolved card to act with later in the same turn.
        /// </summary>
        private static HeroId CommitBulwarkTurn(MatchSession session, string blueCardInstanceId)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            var bulwark = session.Projection.Cards.Single(card => card.Id.Equals(BulwarkCardId));
            var blue = session.Projection.Cards.Single(card =>
                card.Id.Value == blueCardInstanceId);
            Accept(session, DirectorCommand.CommitCard(bulwark.OwnerId, bulwark.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "The committed Initiatives differ, so no tie has to be steered.");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(BulwarkCardId));
            return bulwark.OwnerId;
        }

        /// <summary>Commits one card per side so the named hero acts, for a later turn of the round.</summary>
        private static void CommitOrdinaryTurn(MatchSession session, HeroId actorId)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            var actorSide = session.Projection.Participants.GetTeamForHero(actorId).Side;
            var opposingSide = actorSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;
            var actorCard = HandCards(session, actorSide)
                .OrderByDescending(card => DefinitionOf(session, card).Initiative.Value)
                .First();
            var opposingCard = HandCards(session, opposingSide)
                .OrderBy(card => DefinitionOf(session, card).Initiative.Value)
                .First();
            Accept(session, DirectorCommand.CommitCard(actorCard.OwnerId, actorCard.Id));
            Accept(session, DirectorCommand.CommitCard(opposingCard.OwnerId, opposingCard.Id));
            ResolveInitiativeTie(session);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(actorId));
        }

        /// <summary>Holds with every remaining unresolved card, which ends the turn.</summary>
        private static void CompleteCurrentTurn(MatchSession session)
        {
            var turnIndex = session.Projection.TurnIndex;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                session.Projection.TurnIndex == turnIndex)
            {
                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }
        }

        private static void AdvanceToNextRound(MatchSession session)
        {
            var round = session.Projection.RoundIndex;
            while (session.Projection.RoundIndex == round)
            {
                Assert.That(session.Projection.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded));
                switch (session.Projection.Phase)
                {
                    case RoundResolutionPhase.AwaitingCommitments:
                        CommitAnyEligibleCards(session);
                        break;
                    case RoundResolutionPhase.ResolvingTurn:
                        Accept(session, DirectorCommand.PerformHold(
                            session.Projection.SelectedActor.CardInstanceId));
                        break;
                    default:
                        Accept(session, DirectorCommand.AdvanceEndOfRound());
                        break;
                }
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
        }

        private static void CommitAnyEligibleCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = HandCards(session, side).FirstOrDefault();
                if (card != null)
                {
                    Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
                    continue;
                }

                var hero = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
                Accept(session, DirectorCommand.Pass(hero));
            }

            ResolveInitiativeTie(session);
        }

        private static void StartBulwarkSkillOnCurrentTurn(MatchSession session)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(BulwarkSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        /// <summary>Selects one mode and confirms it, the two-command shape every selection has.</summary>
        private static void ChooseMode(MatchSession session, EffectNodeId mode)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(mode));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static int FastTravelOfferCount(MatchSession session, HeroId actorId)
        {
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(actorId));
            return session.Projection.AvailableFastTravelOptions.Count;
        }

        private static bool IsRestricted(MatchSession session, HeroId heroId, RuleActivity activity) =>
            Modifications(session).IsRestricted(RuleObject.ForHero(heroId), activity);

        private static bool HasMoveThroughPermission(MatchSession session, HeroId heroId) =>
            Modifications(session).HasPermission(
                RuleObject.ForHero(heroId), RulePermissionKind.MoveThroughObstacles);

        /// <summary>
        /// The live set, read back through the authoritative snapshot rather than from a projection
        /// field: milestone 6.8 deliberately keeps rules out of the payload, so rebuilding them from a
        /// restored match is what proves the definition still supplies them.
        /// </summary>
        private static RuleModificationSet Modifications(MatchSession session)
        {
            var restored = Restore(session);
            return RuleModificationQuery.Build(
                restored.Round.Timing,
                restored.Board,
                restored.Participants,
                restored.Round.TieBreakerState);
        }

        private static ActiveEffectLifecycle RestoreTiming(MatchSession session) =>
            Restore(session).Round.Timing;

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) => Restore(session).Board;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static CardInstance[] HandCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand)
                .ToArray();

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(card.DefinitionReference));

        private static ActionKind ActionKindOf(MatchSessionProjection projection, ActionOffer offer) =>
            projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind;

        private static void ResolveInitiativeTie(MatchSession session)
        {
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>The ordinary configured match on the official map.</summary>
        private static MatchSession CreateConfiguredSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// A straight line with the Blue hero standing between the Red hero and open space, so
        /// "can this hero reach beyond that unit" has different answers with and without the
        /// permission. Red starts at (0,0), Blue occupies (1,0), and (2,0) is the space only a
        /// pass-through reaches.
        /// </summary>
        private static MatchSession CreateBlockedLineSession() =>
            CreateLineSession(new[]
            {
                new HexCoordinate(0, 0),
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)
            });

        /// <summary>
        /// Two adjacent heroes on a two-space synthetic map, so an enemy Attack has a legal target and
        /// the Defense reaction is reachable inside one turn.
        /// </summary>
        private static MatchSession CreateAdjacentSession() =>
            CreateLineSession(new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) });

        private static MatchSession CreateLineSession(IReadOnlyList<HexCoordinate> coordinates)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId($"modification-skill-line-{coordinates.Count}"),
                1,
                coordinates);
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("red-modification"),
                        "Red Modification",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("blue-modification"),
                        "Blue Modification",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                        blueCoordinate)
                },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
