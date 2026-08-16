using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
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
    /// Milestone 6.8b. Blue Feint's printed Primary Attack is replaced by one two-mode Skill whose
    /// source is the card and whose affected object is always the acting hero: Push immunity under
    /// `Next Turn, After Playing Cards`, or effectively counting as a minion for `This Round`.
    ///
    /// Everything the modes do comes from milestone 6.8's registered contracts; this fixture proves the
    /// binding and, above all, the consumers. That is why the milestone was reordered behind Object
    /// Push: until 6.4 existed the immunity named an effect no rule performed, and until 6.8g the
    /// effective type reached no production caller, so printing either mode onto a card a player holds
    /// would have repeated the defect 6.8c was written to correct. Here the Red hero's configured Push
    /// really does refuse an immune target, and the effective-type queries really do see a minion.
    ///
    /// The fixture line is real official-map geography: the Red hero stands on its own spawn point
    /// `q-2_r-5` and the Blue hero adjacent at `q-1_r-5`, with `q0_r-5` playable beyond it, which is the
    /// same line milestone 6.4's displacement fixture uses.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class ConfiguredImmunitySkillTests
    {
        private static readonly CardInstanceId FeintCardId = new CardInstanceId("blue-feint-card");

        private static readonly ActionDefinitionId FeintSkill =
            new ActionDefinitionId("primary-immunity-skill");

        private static readonly ActionDefinitionId DisplacementSkill =
            new ActionDefinitionId("primary-displacement-skill");

        private static readonly ActionDefinitionId NeedleSkill =
            new ActionDefinitionId("primary-push-place-skill");

        private static readonly EffectNodeId ImmunityMode = new EffectNodeId("feint-push-immunity");
        private static readonly EffectNodeId CountsAsMode = new EffectNodeId("feint-counts-as-minion");
        private static readonly EffectNodeId PushBranch = new EffectNodeId("displacement-object-push");
        private static readonly EffectNodeId NeedlePushBranch = new EffectNodeId("needle-push-branch");
        private static readonly EffectNodeId NeedlePlaceBranch = new EffectNodeId("needle-place-branch");

        private static readonly HexCoordinate RedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate BlueAt = new HexCoordinate(-1, -5);
        private static readonly HexCoordinate BeyondBlue = new HexCoordinate(0, -5);

        // ---------------------------------------------------------------------------------------
        // Definition
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BlueFeint_ReplacesOnlyItsPrintedPrimaryAttack()
        {
            var hand = PlaceholderMovementHand.CreateBlue(new HeroId("blue-synthetic-hero"));
            var definition = hand.Definitions.Single(candidate =>
                candidate.DisplayName == "TEST - Blue Feint");
            var instance = hand.Instances[0];

            Assert.That(definition.Id, Is.EqualTo(new CardDefinitionId("placeholder-blue-feint")),
                "The conversion advances the revision, never the card's identity.");
            Assert.That(definition.ContentRevision, Is.EqualTo("phase-6-8b-rev-1"));
            Assert.That(definition.ContentAuthority,
                Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(definition.Initiative.Value, Is.EqualTo(12));
            Assert.That(instance.DefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(hand.Definitions, Has.Count.EqualTo(5), "The hand keeps its five-card shape.");

            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var movement = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedSecondary);
            var defense = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.Reaction);

            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(primary.Id, Is.EqualTo(FeintSkill));
            Assert.That(definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Attack),
                Is.False, "The printed Attack is replaced, not kept beside the Skill.");
            Assert.That(movement.Kind, Is.EqualTo(ActionKind.Movement));
            Assert.That(movement.BaseValues.Single().Amount, Is.EqualTo(3));
            Assert.That(defense.Kind, Is.EqualTo(ActionKind.Defense));
            Assert.That(defense.BaseValues.Single().Amount, Is.EqualTo(2));
        }

        [Test]
        public void TheSkill_DeclaresTwoModesEachNamingOneRegisteredCapability()
        {
            var effect = FeintEffect();

            var root = effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(root.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "The acting player picks the mode; the resolver never picks for a player.");
            Assert.That(root.MinimumSelections, Is.EqualTo(1));
            Assert.That(root.MaximumSelections, Is.EqualTo(1));
            Assert.That(root.ChildNodeIds, Is.EqualTo(new[] { ImmunityMode, CountsAsMode }));

            var capabilities = new Dictionary<EffectNodeId, EffectCapabilityReference>
            {
                [ImmunityMode] = SyntheticModificationCapabilities.GrantPushImmunityV1,
                [CountsAsMode] = SyntheticModificationCapabilities.CountAsMinionV1
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
        public void TheImmunityMode_NamesObjectPushUnderItsOwnDistinctLifetimePolicy()
        {
            var definition = SyntheticModificationCapabilities.PushImmunityDefinition;

            Assert.That(definition.Modifications, Has.Count.EqualTo(1),
                "No mode combines two rule-source families to save a mode.");
            var modification = definition.Modifications.Single();
            Assert.That(modification.Kind, Is.EqualTo(RuleModificationKind.Immunity));
            Assert.That(modification.Activity, Is.EqualTo(RuleActivity.ObjectPush),
                "The immunity names the originating effect family, which milestone 6.4 implements.");
            Assert.That(definition.DurationKind,
                Is.EqualTo(ActiveEffectDurationKind.NextTurnAfterPlayingCards));
            Assert.That(definition.ActivationCheckpoint,
                Is.EqualTo(TimingCheckpoint.CardsPlayedAndRevealed),
                "p.17 applies this keyword once next turn, after all players have played and revealed " +
                "their cards - not at the start of the turn, which is ordinary `Next Turn`.");
            Assert.That(definition.ActivationCheckpoint,
                Is.Not.EqualTo(SyntheticModificationCapabilities.MoveThroughPermissionDefinition
                    .ActivationCheckpoint),
                "The two next-turn forms must not share an anchor, or the distinction is decorative.");
            Assert.That(definition.CarriesAcrossRoundBoundary, Is.True);
            Assert.That(definition.AffectedScope,
                Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero),
                "The affected object is the acting hero, never the source card.");
            Assert.That(definition.CancellationConditions,
                Has.Member(ActiveEffectCancellationCondition.AffectedHeroDefeated),
                "p.17 cancels an Active effect on your card when you are defeated.");
        }

        [Test]
        public void TheCountsAsMode_DeclaresOneEffectiveTypeReplacementForTheRound()
        {
            var definition = SyntheticModificationCapabilities.CountsAsMinionDefinition;

            Assert.That(definition.Modifications, Has.Count.EqualTo(1));
            var modification = definition.Modifications.Single();
            Assert.That(modification.Kind, Is.EqualTo(RuleModificationKind.CountsAs));
            Assert.That(modification.EffectiveUnitKind, Is.EqualTo(EffectiveUnitKind.Minion));
            Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisRound));
            Assert.That(definition.ExpirationCheckpoint, Is.EqualTo(TimingCheckpoint.RoundEnd));
            Assert.That(definition.CarriesAcrossRoundBoundary, Is.False);
            Assert.That(definition.AffectedScope,
                Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero));
        }

        // ---------------------------------------------------------------------------------------
        // Mode selection through the ordinary match
        // ---------------------------------------------------------------------------------------

        [Test]
        public void StartingTheSkill_OpensAChooserOwnedSelectionAmongItsTwoModes()
        {
            var session = StartFeintSkill(out var actorId);

            var choice = session.Projection.PendingEffectChoice;

            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(choice.ChooserId, Is.EqualTo(actorId));
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { ImmunityMode, CountsAsMode }),
                "Modes are offered in the definition's own order.");
        }

        [TestCase("feint-push-immunity", "synthetic-push-immunity-effect")]
        [TestCase("feint-counts-as-minion", "synthetic-counts-as-minion-effect")]
        public void EachMode_CreatesItsOneEffectOnTheActingHeroSourcedToTheCard(
            string modeNodeId,
            string expectedDefinitionId)
        {
            var session = StartFeintSkill(out var actorId);

            ChooseMode(session, new EffectNodeId(modeNodeId));

            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
            var effect = RestoreTiming(session).Effects.Single();
            Assert.That(effect.Definition.DefinitionId, Is.EqualTo(expectedDefinitionId));
            Assert.That(effect.ControllerHeroId, Is.EqualTo(actorId),
                "The acting hero is the affected rules object.");
            Assert.That(effect.SourceCardInstanceId, Is.EqualTo(FeintCardId),
                "The card stays the stable source of the effect it created.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(FeintCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        // ---------------------------------------------------------------------------------------
        // `Next Turn, After Playing Cards`
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheImmunity_StaysDormantThroughTheTurnItWasCreatedIn()
        {
            var session = StartFeintSkill(out var blueId);

            ChooseMode(session, ImmunityMode);

            Assert.That(RestoreTiming(session).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Dormant),
                "Cards were already revealed this turn, so the keyword's checkpoint is the next one.");
            Assert.That(IsImmuneToPush(session, blueId), Is.False,
                "A dormant effect supplies no rule at all.");
        }

        [Test]
        public void TheImmunity_ComesIntoEffectWhenTheNextTurnsCardsAreRevealed()
        {
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, ImmunityMode);
            CompleteCurrentTurn(session);

            CommitCards(session, redCardFragment: "shove", blueCardFragment: "sentinel");

            Assert.That(RestoreTiming(session).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Active),
                "p.17 applies the keyword once next turn, after all players have played and revealed.");
            Assert.That(IsImmuneToPush(session, blueId), Is.True);
        }

        [Test]
        public void TheImmunity_KeepsItsDeclaredPhaseThroughASkippedTurnAndRestoreAndReplay()
        {
            // The Blue hero does nothing on the turn its Skill created the effect beyond the Skill
            // itself, and the effect must still be exactly Dormant when the turn ends - carried across
            // the snapshot, a replay of the same commands, and a retained-prefix Undo.
            var session = StartFeintSkill(out _);
            ChooseMode(session, ImmunityMode);
            CompleteCurrentTurn(session);

            var snapshot = session.Projection.AuthoritativeSnapshot;
            var commands = session.Projection.AcceptedCommands.ToArray();
            Assert.That(RestoreTiming(session).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Dormant),
                "A turn that passed without the checkpoint leaves the declared phase exactly as it was.");

            var restored = CreateAdjacentSkillSession();
            Assert.That(restored.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(RestoreTiming(restored).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Dormant),
                "Restore rebinds the instance to the trusted definition and keeps its phase.");

            var replayed = CreateAdjacentSkillSession();
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    "Every accepted command must remain acceptable on replay.");
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));

            Assert.That(session.Undo().Accepted, Is.True);
            var reExecuted = session.Execute(commands.Last());
            Assert.That(reExecuted.Accepted, Is.True, reExecuted.Detail);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot),
                "The retained prefix replays to exactly the state it produced.");
        }

        // ---------------------------------------------------------------------------------------
        // The immunity's consumer: milestone 6.4's configured Object Push
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheImmuneHero_IsNotAPushTargetAndTheEnemyPushModeIsNotOffered()
        {
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, ImmunityMode);
            CompleteCurrentTurn(session);
            CommitCards(session, redCardFragment: "shove", blueCardFragment: "sentinel");

            StartDisplacementSkill(session);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Has.Member(PushBranch),
                "p.18: an immune unit cannot be targeted, so the only adjacent object is no target at " +
                "all, and milestone 6.5ac refuses the mode rather than letting a player spend the " +
                "card discovering it.");
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Has.No.Member(PushBranch));

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, blueId), Is.EqualTo(BlueAt), "Nothing moved.");
            Assert.That(RestoreTiming(session).Effects.Single().SourceCardInstanceId,
                Is.EqualTo(FeintCardId),
                "The source of the immunity is still Blue Feint's resolved card definition.");
        }

        [Test]
        public void RedNeedle_DoesNotOfferItsPushImmuneOpponentAsATarget()
        {
            var session = StartFeintSkill(out _);
            ChooseMode(session, ImmunityMode);
            CompleteCurrentTurn(session);
            CommitCards(session, redCardFragment: "needle", blueCardFragment: "sentinel");

            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(NeedleSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { NeedlePushBranch }),
                "The exact Push immunity is applied while candidates are derived, so the branch that " +
                "would consume them can do nothing and is refused - while the Place branch beside it " +
                "stays selectable, which is what shows the refusal measures feasibility rather than " +
                "disabling the whole card.");
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { NeedlePlaceBranch }));
        }

        [Test]
        public void TheSameHeroWithTheOtherMode_IsPushedNormally()
        {
            // The same card, the same turn shape, the same push - with the mode that declares no
            // immunity. It is the control that shows the case above measures the immunity rather than
            // the fixture geography.
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, CountsAsMode);
            CompleteCurrentTurn(session);
            CommitCards(session, redCardFragment: "shove", blueCardFragment: "sentinel");

            StartDisplacementSkill(session);
            ChooseMode(session, PushBranch);

            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BeyondBlue),
                "A push of 2 travels 1 here: the next space along the file is terrain.");
        }

        [Test]
        public void TargetingRefusesTheImmuneHeroWhilePresenceStillCountsIt()
        {
            // p.18 keeps the two questions apart in the same paragraph: "If a unit is immune, you
            // cannot target that unit ... Effects that check for unit presence will count immune units."
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, ImmunityMode);
            CompleteCurrentTurn(session);
            CommitCards(session, redCardFragment: "shove", blueCardFragment: "sentinel");

            var board = RestoreBoard(session);
            var modifications = Modifications(session);
            var redId = session.Projection.Participants.Heroes
                .Select(hero => hero.Id).Single(hero => !hero.Equals(blueId));

            Assert.That(
                TargetQuery.FindTargetsWithin(
                    RedAt, 1, false, RuleObject.ForHero(redId), RuleActivity.ObjectPush,
                    board, session.Projection.Participants, modifications),
                Is.Empty,
                "Targeting for the activity the immunity names refuses the hero.");
            Assert.That(
                TargetQuery.FindTargetsWithin(
                    RedAt, 1, false, RuleObject.ForHero(redId), RuleActivity.Attack,
                    board, session.Projection.Participants, modifications).Select(target => target.HeroId),
                Has.Member(blueId),
                "An immunity to a specific activity leaves every other activity alone.");
            Assert.That(
                UnitPresenceQuery.IsPresentWithin(
                    RedAt, 1, EffectiveUnitKind.Hero, board, modifications, RuleObject.ForHero(redId)),
                Is.True,
                "A presence question over the same space still counts the immune unit.");
        }

        // ---------------------------------------------------------------------------------------
        // The `Counts As` mode's consumer: the effective-type queries
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheCountsAsHero_IsEffectivelyAMinionWhileStillBeingTheHeroMiniatureItIs()
        {
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, CountsAsMode);

            var board = RestoreBoard(session);
            var modifications = Modifications(session);
            var blue = RuleObject.ForHero(blueId);

            Assert.That(EffectiveUnitKindQuery.Is(blue, EffectiveUnitKind.Minion, modifications),
                Is.True, "p.17 replaces the object's rules and qualities with the type it counts as.");
            Assert.That(EffectiveUnitKindQuery.Is(blue, EffectiveUnitKind.Hero, modifications),
                Is.False, "The replacement is maximal, so it stops satisfying a hero-typed query.");
            Assert.That(IntrinsicKindQuery.IntrinsicUnitKind(blue), Is.EqualTo(EffectiveUnitKind.Hero),
                "Intrinsic representation is untouched: a hero miniature is standing there.");
            Assert.That(IntrinsicKindQuery.IsMinionMiniature(blue), Is.False);

            Assert.That(board.Occupancies.Select(occupancy => occupancy.HeroId), Has.Member(blueId),
                "Identity, ownership, and occupancy kind are exactly what they were.");
            Assert.That(board.Minions.Count, Is.EqualTo(12),
                "No minion miniature was created; the board's minion collection is untouched.");
            Assert.That(board.FindUnitSupply(), Is.Empty,
                "Supply rules are untouched: counting as a minion draws nothing from the supply.");
            Assert.That(
                UnitPresenceQuery.FindUnitsWithin(
                    BlueAt, 0, EffectiveUnitKind.Minion, board, modifications)
                    .Select(unit => unit.HeroId),
                Has.Member(blueId),
                "A presence question for a minion sees it, which is the effective type's consumer.");
        }

        [Test]
        public void TheCountsAsHero_IsNeverASubjectOfTheDisplacedMinionCorrection()
        {
            // p.18 states the exception outright: the correction "is only relevant for minions
            // represented by minion miniatures; objects that count as minions are unaffected!"
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, CountsAsMode);

            var board = RestoreBoard(session);
            Assert.That(
                BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, BlueAt), Is.False,
                "The Blue hero really is outside the Battle Zone, so this is not a vacuous check.");
            Assert.That(
                BattleZoneQuery.FindMinionsOutside(board).Select(minion => minion.MinionId.Value),
                Has.No.Member(blueId.Value),
                "The correction reads minion miniatures, so a counts-as hero cannot join its subjects.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null,
                "Completing the Skill opened no correction over the hero it retyped.");
        }

        [Test]
        public void TheCountsAsDeclaration_EndsAtRoundEndAndNeverCrossesTheBoundary()
        {
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, CountsAsMode);
            CompleteCurrentTurn(session);
            AdvanceToNextRound(session);

            Assert.That(RestoreTiming(session).Effects.Single().Phase,
                Is.EqualTo(ActiveEffectPhase.Ended));
            Assert.That(
                EffectiveUnitKindQuery.Is(
                    RuleObject.ForHero(blueId), EffectiveUnitKind.Minion, Modifications(session)),
                Is.False,
                "A `This Round` declaration ends at Round End and never crosses the boundary.");
        }

        // ---------------------------------------------------------------------------------------
        // Determinism and rejection
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AChosenMode_SurvivesSnapshotRestoreReplayAndRetainedPrefixUndo()
        {
            var session = StartFeintSkill(out var blueId);
            ChooseMode(session, ImmunityMode);
            CompleteCurrentTurn(session);
            CommitCards(session, redCardFragment: "shove", blueCardFragment: "sentinel");

            var snapshot = session.Projection.AuthoritativeSnapshot;
            var diagnostic = session.Projection.DiagnosticSnapshot;
            var commands = session.Projection.AcceptedCommands.ToArray();

            var restored = CreateAdjacentSkillSession();
            Assert.That(restored.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(IsImmuneToPush(restored, blueId), Is.True,
                "Restore rebinds the effect to the trusted catalog and it supplies the same rule.");

            var replayed = CreateAdjacentSkillSession();
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
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
        }

        [Test]
        public void AStaleOrForgedModeAnswer_RejectsWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartFeintSkill(out _);
            var choice = session.Projection.PendingEffectChoice;
            var snapshot = session.Projection.AuthoritativeSnapshot;
            var commandCount = session.Projection.AcceptedCommands.Count;

            var stale = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                new EffectChoiceId("stale-mode-choice"),
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(ImmunityMode))));
            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(new EffectNodeId("feint-no-such-mode")))));

            Assert.That(stale.Accepted, Is.False);
            Assert.That(forged.Accepted, Is.False);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(commandCount));
            Assert.That(session.Projection.PendingEffectChoice.Candidates,
                Is.EqualTo(choice.Candidates));
            Assert.That(RestoreTiming(session).Effects, Is.Empty,
                "A refused mode answer creates no effect.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(FeintCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static EffectDefinition FeintEffect() =>
            PlaceholderMovementHand.CreateBlue(new HeroId("blue-synthetic-hero"))
                .Definitions.Single(candidate => candidate.DisplayName == "TEST - Blue Feint")
                .ActionDefinitions.Single(action => action.Id.Equals(FeintSkill))
                .EffectRoot;

        /// <summary>
        /// Commits Blue Feint at Initiative 12 against Red Bombard at 5, so the Blue hero acts first
        /// and its Skill is the current actor's offer.
        /// </summary>
        private static MatchSession StartFeintSkill(out HeroId blueId)
        {
            var session = CreateAdjacentSkillSession();
            CommitCards(session, redCardFragment: "bombard", blueCardFragment: "feint");
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(FeintCardId));
            blueId = session.Projection.SelectedActor.ActorId;
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(FeintSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static void StartDisplacementSkill(MatchSession session)
        {
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(DisplacementSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        private static void CommitCards(
            MatchSession session, string redCardFragment, string blueCardFragment)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            foreach (var pair in new[]
            {
                new { Side = TeamSide.Red, Fragment = redCardFragment },
                new { Side = TeamSide.Blue, Fragment = blueCardFragment }
            })
            {
                var card = HandCards(session, pair.Side)
                    .Single(candidate => candidate.Id.Value.Contains(pair.Fragment));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "The committed Initiatives differ, so no tie has to be steered.");
        }

        private static void ChooseMode(MatchSession session, EffectNodeId mode)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(mode));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

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

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static CardInstance[] HandCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand)
                .ToArray();

        private static bool IsImmuneToPush(MatchSession session, HeroId heroId) =>
            Modifications(session).IsImmuneTo(
                RuleObject.ForHero(heroId),
                RuleActivity.ObjectPush,
                RuleObject.ForHero(session.Projection.Participants.Heroes
                    .Select(hero => hero.Id).Single(hero => !hero.Equals(heroId))));

        /// <summary>
        /// The live set, rebuilt from the authoritative snapshot rather than read from a projection
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

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>
        /// The Skill-fixture configuration on the official map, with the Blue hero adjacent to the Red
        /// hero so an Object Push has a legal target to be refused.
        /// </summary>
        private static MatchSession CreateAdjacentSkillSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, RedAt),
                    new HeroOccupancy(blueHeroId, BlueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
