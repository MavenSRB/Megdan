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
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5b. Red Bombard's printed Primary Attack is replaced by the modal Skill
    /// `Choose one: directly Move another adjacent unit up to 2 spaces; or Spawn one unit from your
    /// supply onto its own Spawn point`.
    ///
    /// Everything both branches do comes from milestones 6.4, 6.5 and 6.8k; this fixture proves the
    /// binding and the player-visible behaviour. It runs on real official-map geography rather than a
    /// synthetic board, because that is where the interesting cases are free: the Red hero on its own
    /// spawn point at `q-2_r-5`, the Blue hero adjacent at `q-1_r-5`, `q0_r-5` playable, and `q1_r-5`
    /// a declared terrain cell.
    ///
    /// The Spawn branch needs a supply, and at setup there is none - every declared miniature stands.
    /// That is not a gap in the fixture but the rule: p.18's Spawn takes only from the supply, so the
    /// cases here are the empty-supply stop and the state after a Minion Battle has put a miniature
    /// back, which is how an ordinary match reaches one.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class ConfiguredDirectMoveSpawnSkillTests
    {
        private static readonly CardInstanceId BombardCardId = new CardInstanceId("red-bombard-card");

        private static readonly ActionDefinitionId BombardSkill =
            new ActionDefinitionId("primary-direct-move-spawn-skill");

        private static readonly EffectNodeId MoveBranch = new EffectNodeId("bombard-move-branch");
        private static readonly EffectNodeId SelectTarget = new EffectNodeId("bombard-select-target");
        private static readonly EffectNodeId Move = new EffectNodeId("bombard-direct-move");
        private static readonly EffectNodeId Spawn = new EffectNodeId("bombard-spawn");

        private static readonly ActionDefinitionId ComponentSkill =
            new ActionDefinitionId("primary-component-skill");

        private static readonly EffectNodeId TokenPlaceBranch =
            new EffectNodeId("component-place-branch");

        private static readonly HexCoordinate RedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate BlueAt = new HexCoordinate(-1, -5);
        private static readonly HexCoordinate TerrainBeyond = new HexCoordinate(1, -5);

        /// <summary>
        /// red-melee-01 and its declared Spawn point, plus a space beside it. Every configured minion
        /// starts on its own Spawn point, so this is both where that miniature stands at setup and
        /// where it Spawns back to.
        /// </summary>
        private static readonly MinionId RemovedMinion = new MinionId("red-melee-01");

        private static readonly HexCoordinate RedMeleeSpawnAt = new HexCoordinate(-1, -1);

        private static readonly HexCoordinate BesideRedMeleeSpawn = new HexCoordinate(0, -1);

        // ---------------------------------------------------------------------------------------
        // Definition
        // ---------------------------------------------------------------------------------------

        [Test]
        public void RedBombard_ReplacesOnlyItsPrintedPrimaryAttack()
        {
            var hand = PlaceholderMovementHand.CreateRed(new HeroId("red-synthetic-hero"));
            var definition = hand.Definitions.Single(candidate =>
                candidate.DisplayName == "TEST - Red Bombard");

            Assert.That(definition.Id, Is.EqualTo(new CardDefinitionId("placeholder-red-bombard")),
                "The conversion advances the revision, never the card's identity.");
            Assert.That(definition.ContentRevision, Is.EqualTo("phase-6-8l-rev-1"));
            Assert.That(definition.ContentAuthority,
                Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(definition.Initiative.Value, Is.EqualTo(5));
            Assert.That(hand.Instances[4].DefinitionReference, Is.EqualTo(definition.Reference),
                "It keeps its five-card-hand slot as well as its identity.");
            Assert.That(hand.Definitions, Has.Count.EqualTo(5), "The hand keeps its five-card shape.");

            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(primary.Id, Is.EqualTo(BombardSkill));
            Assert.That(primary.BaseValues, Is.Empty,
                "A Skill prints no value channel, so the replaced Attack and Range say nothing.");
            Assert.That(definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Attack),
                Is.False, "The printed Attack is replaced, not kept beside the Skill.");
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedSecondary)
                    .BaseValues.Single().Amount,
                Is.EqualTo(2));
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.Reaction)
                    .BaseValues.Single().Amount,
                Is.EqualTo(2));
        }

        [Test]
        public void TheRedHand_StillKeepsOnePrintedNonRangedAndOneRangedAttack()
        {
            // This is the conversion that takes the Red side to the showcase schedule's floor: with
            // Red Bombard's Ranged Attack gone, Red Lancer is the only one left.
            var attacks = PlaceholderMovementHand.CreateRed(new HeroId("red-synthetic-hero"))
                .Definitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .ToArray();

            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.NonRanged), Is.EqualTo(1));
            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.Ranged), Is.EqualTo(1));
        }

        [Test]
        public void TheSkill_DeclaresTheTwoBranchesTheCardTextNames()
        {
            var effect = BombardEffect();

            var root = effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(root.Chooser, Is.EqualTo(EffectChooserRole.Actor));
            Assert.That(root.ChildNodeIds, Is.EqualTo(new[] { MoveBranch, Spawn }));

            var selectTarget = effect.Nodes.Single(node => node.Id.Equals(SelectTarget));
            Assert.That(selectTarget.Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.ChooseTargetV3),
                "Card-text direct Move has no activity of its own to declare - p.11 says it is " +
                "expressly not a Movement action - so the offer uses the revision already bound to " +
                "the enclosing action's activity rather than naming Skill a second time.");
            Assert.That(
                selectTarget.Inputs.Any(input => input.ParameterName == "activity"), Is.False);
            Assert.That(
                selectTarget.Inputs.Single(input => input.ParameterName == "range").Literal.IntegerValue,
                Is.EqualTo(1), "`another adjacent unit` is Radius 1.");
            Assert.That(
                selectTarget.Inputs.Single(input => input.ParameterName == "includeSelf")
                    .Literal.BooleanValue,
                Is.False, "`another` excludes the acting hero.");

            var move = effect.Nodes.Single(node => node.Id.Equals(Move));
            Assert.That(move.Capability, Is.EqualTo(SyntheticDisplacementCapabilities.DirectMoveV2));
            Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                "`up to 2 spaces` is p.11's optional distance, carried by the node's own policy.");
            Assert.That(
                move.Inputs.Single(input => input.ParameterName == "distance").Literal.IntegerValue,
                Is.EqualTo(2));
            Assert.That(
                move.Inputs.Single(input => input.ParameterName == "towardActor").Literal.BooleanValue,
                Is.False, "A `towards` bound would need verified card text to read it from.");
            Assert.That(
                move.Inputs.Single(input => input.ParameterName == "mover").SourceNodeId,
                Is.EqualTo(SelectTarget),
                "A coordinate reaches a capability only through an earlier node's published output.");

            var spawn = effect.Nodes.Single(node => node.Id.Equals(Spawn));
            Assert.That(spawn.Capability, Is.EqualTo(SyntheticPlacementCapabilities.SpawnV1));
            Assert.That(spawn.Inputs, Is.Empty,
                "Which miniature Spawns is the capability's own one decision, and its destination is " +
                "declared by the miniature's Spawn point rather than chosen, so nothing binds in.");
            Assert.That(
                spawn.Outputs.Select(output => output.OutputName).OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[] { "at", "spawned" }));
            Assert.That(effect.Nodes.Any(node =>
                node.Capability.Equals(SyntheticPlacementCapabilities.PlaceV1)), Is.False,
                "p.18 keeps Place and Spawn distinct operations, and no card offers both.");

            Assert.That(
                EffectDefinitionValidator.Validate(effect, SyntheticEffectCapabilities.CreateCatalog()),
                Is.Empty);
        }

        // ---------------------------------------------------------------------------------------
        // The direct Move branch
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheMoveBranch_OffersExactlyTheOtherAdjacentUnits()
        {
            var session = StartBombardSkill();

            ChooseBranch(session, MoveBranch);

            var selection = session.Projection.PendingEffectChoice;
            Assert.That(selection, Is.Not.Null);
            Assert.That(selection.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            Assert.That(
                selection.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { BlueAt }),
                "`another adjacent unit`: the actor's own space is not among them.");
        }

        [Test]
        public void TheMoveBranch_OffersEveryDestinationUpToTwoSpacesAndMovesTheChosenUnit()
        {
            var session = StartBombardSkill();
            var blueId = BlueHero(session);

            ChooseBranch(session, MoveBranch);
            SelectSpace(session, BlueAt);

            var destinations = session.Projection.PendingEffectChoice;
            Assert.That(destinations, Is.Not.Null, "`up to 2` makes the distance the second decision.");
            var offered = destinations.Candidates.Select(candidate => candidate.Value.Coordinate).ToArray();
            Assert.That(offered, Has.Length.GreaterThan(1),
                "`up to 2` admits every destination at or below the bound, not only the exact one.");
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(BlueAt, space) <= 2), Is.True);
            Assert.That(offered, Has.No.Member(RedAt), "The acting hero's space is occupied.");
            Assert.That(offered, Has.No.Member(TerrainBeyond),
                "p.11 follows the Movement action's rules, so terrain is never an endpoint.");
            var chosen = offered.First(space => HexDistanceQuery.Calculate(BlueAt, space) == 2);

            SelectSpace(session, chosen);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, blueId), Is.EqualTo(chosen),
                "The named unit is the one that moves.");
            Assert.That(Coordinate(board, RedHero(session)), Is.EqualTo(RedAt),
                "The acting hero moves nothing of its own.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void TheMoveBranch_LetsThePlayerMoveTheChosenUnitNoSpacesAtAll()
        {
            // Milestone 6.5ad, through the card the defect was found on. `up to 2 spaces` is p.10's
            // non-mandatory wording, and p.11 requires the exact number only where the distance is not
            // optional, so none at all is one of the distances this branch offers - which is what the
            // adjacency-derived Push on Red Needle has offered since milestone 6.4.
            var session = StartBombardSkill();
            var blueId = BlueHero(session);

            ChooseBranch(session, MoveBranch);
            SelectSpace(session, BlueAt);

            var offered = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(offered, Has.Member(BlueAt),
                "The chosen unit's own space is the destination that moves it nowhere.");

            SelectSpace(session, BlueAt);

            Assert.That(Coordinate(RestoreBoard(session), blueId), Is.EqualTo(BlueAt),
                "Choosing it relocates nothing.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "It is an applied result rather than a decline or a stop, so the Skill completes.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.Not.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void AMinionThatStaysWhereItIs_RaisesNoCorrectionAndNoMovementFact()
        {
            // The consequence check. p.18 corrects a minion miniature that ends up outside the Battle
            // Zone after the action; one that never left raises nothing, and neither the zero distance
            // nor the ordinary one may add a Movement, Fast-Travel, or Lane-Push consequence of its own.
            var session = StartBombardSkillBesideAMinion(out var minionId, out var minionAt);

            ChooseBranch(session, MoveBranch);
            SelectSpace(session, minionAt);
            SelectSpace(session, minionAt);

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(session.Projection.PendingMovementChoice, Is.Null);
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null);
            Assert.That(
                RestoreBoard(session).Minions.Single(minion => minion.MinionId.Equals(minionId)).Coordinate,
                Is.EqualTo(minionAt));
            Assert.That(
                SkillFacts(session).Select(fact => fact.Kind).Distinct(),
                Is.SubsetOf(new[]
                {
                    CardActionResolutionEventKind.SkillActionStarted,
                    CardActionResolutionEventKind.SkillChoiceRequested,
                    CardActionResolutionEventKind.SkillEffectNodeApplied,
                    CardActionResolutionEventKind.SkillActionCompleted
                }));
        }

        [Test]
        public void TheMoveBranch_IsNeitherAMovementActionNorFastTravel()
        {
            // p.11: card-text movement "cannot be used to Fast Travel", and it is not a Movement
            // action, so no Movement fact is published and no Movement choice is opened.
            var session = StartBombardSkill();
            ChooseBranch(session, MoveBranch);
            SelectSpace(session, BlueAt);
            SelectSpace(session, session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate);

            Assert.That(session.Projection.PendingMovementChoice, Is.Null);
            Assert.That(
                SkillFacts(session).Select(fact => fact.Kind).Distinct(),
                Is.SubsetOf(new[]
                {
                    CardActionResolutionEventKind.SkillActionStarted,
                    CardActionResolutionEventKind.SkillChoiceRequested,
                    CardActionResolutionEventKind.SkillEffectNodeApplied,
                    CardActionResolutionEventKind.SkillActionCompleted
                }),
                "The stream is a Skill's, with no Movement or Fast-Travel fact anywhere in it.");
        }

        [Test]
        public void TheMoveBranch_ConsumesNoSupply()
        {
            // p.18 separates the two operations by their source container: a direct Move relocates a
            // piece already on the board and can never take one from the supply.
            var session = StartBombardSkill();
            var before = RestoreBoard(session).FindUnitSupply().Count;

            ChooseBranch(session, MoveBranch);
            SelectSpace(session, BlueAt);
            SelectSpace(session, session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate);

            Assert.That(RestoreBoard(session).FindUnitSupply().Count, Is.EqualTo(before));
        }

        [Test]
        public void WhereNeitherModeCanBeTaken_TheSkillCannotBeStartedAndTheMovementStillCan()
        {
            // Milestone 6.5ac. `another adjacent unit` names nothing when the acting hero stands
            // alone, so the Move branch can do nothing. Here the Spawn mode cannot be performed either
            // - every declared miniature still stands - so no option of this card can be taken at all.
            //
            // Milestone 6.5af. 6.5ae gave that position a way out; this stops the player entering it.
            // The Skill is judged before it is offered, so the turn is still theirs to spend - and
            // Red Bombard prints a Secondary Movement 2 beside the Skill, which is exactly what was
            // being lost. Rulebook p.12: a player is never forced to perform an action.
            var session = StartBombardTurnWithoutStarting(blueAt: new HexCoordinate(-3, 10));

            var skill = session.Projection.AvailableTurnActions
                .Single(offer => offer.ActionDefinitionId.Equals(BombardSkill));
            Assert.That(skill.IsAvailable, Is.False,
                "The Skill can apply nothing, so it is projected as an offer that cannot be started.");
            Assert.That(
                session.Projection.AvailableTurnActions
                    .Where(offer => offer.IsAvailable)
                    .Select(offer => offer.ActionDefinitionId),
                Is.Not.Empty,
                "Every other action the card prints is untouched - the Movement above all, which is " +
                "the turn the old behaviour spent.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Nothing is started and nothing is paused: the decision is still the player's.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved),
                "And the card is unspent, which is the whole point.");
        }

        [Test]
        public void AStartNamingTheUnstartableSkill_IsRefusedWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartBombardTurnWithoutStarting(blueAt: new HexCoordinate(-3, 10));
            var before = session.Projection;

            var refused = session.Execute(DirectorCommand.StartAction(BombardSkill));

            Assert.That(refused.Accepted, Is.False,
                "The refusal is on the command path too: the dispatcher revalidates a start against " +
                "the card-state offer query, which cannot see the board.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "Nothing was created to roll back - no frame, card, occupancy, continuation, or event.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void ASkillWithOnePerformableMode_IsStartableAndItsRefusedModeStaysGreyed()
        {
            // The control that keeps milestone 6.5af measured rather than blanket, and the position
            // milestone 6.5ac's greyed surface still lives in: one mode can be taken, so the action is
            // offered and the other is greyed inside it.
            var session = StartBombardSkillAfterAMinionBattleRemoval(
                out _, blueAt: new HexCoordinate(-3, 10));

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { MoveBranch }),
                "The supply is no longer empty, so the Spawn mode can be taken and the Skill starts.");
            Assert.That(choice.StopInvocation, Is.Null,
                "There is a decision to make, so milestone 6.5ae offers no stop.");

            // Milestone 6.5ae's acceptance check 5, in the direction this card can still reach: the
            // stop is derived from the refusals and the node's policy rather than stored, so a payload
            // restored where a mode can be taken carries no stop either.
            var payload = session.CaptureSnapshot();
            var revived = StartBombardSkillAfterAMinionBattleRemoval(
                out _, blueAt: new HexCoordinate(-3, 10));
            Assert.That(revived.RestoreSnapshot(payload).Accepted, Is.True);
            var restored = revived.Projection.PendingEffectChoice;
            Assert.That(restored.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { MoveBranch }),
                "The refusals are re-derived against the state this payload restored, never read " +
                "from it.");
            Assert.That(restored.StopInvocation, Is.Null,
                "And the stop is re-derived with them rather than carried.");
        }

        [Test]
        public void TheMoveBranch_IsProjectedGreyedWhileTheSpawnModeStaysSelectable()
        {
            // The surface this milestone owes, in the one state where both readings are visible at
            // once: a miniature has left the board, so Spawn can be performed, and nothing stands
            // beside the acting hero, so the Move cannot. The unperformable mode stays in the
            // projected candidate set - a player sees the mode exists - and carries no invocation.
            var session = StartBombardSkillAfterAMinionBattleRemoval(
                out _, blueAt: new HexCoordinate(-3, 10));

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { MoveBranch, Spawn }),
                "Both modes are still projected; refusing one is not hiding it.");
            Assert.That(
                choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { MoveBranch }));
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { Spawn }),
                "The mode that can do nothing carries no way to select it.");
        }

        [Test]
        public void SelectingAGreyedMode_IsRefusedWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartBombardSkillAfterAMinionBattleRemoval(
                out _, blueAt: new HexCoordinate(-3, 10));

            var before = session.Projection;
            var refused = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(MoveBranch))));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal touches no card, occupancy, continuation, or event.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void TheMoveBranch_RefusesATargetTheSelectionNeverOffered()
        {
            var session = StartBombardSkill();
            ChooseBranch(session, MoveBranch);

            var before = session.Projection;
            var illegal = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(new HexCoordinate(0, -5))))));

            Assert.That(illegal.Accepted, Is.False, "That space holds no unit at all.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal touches no card, occupancy, continuation, or event.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void MovingAMinionOutOfTheBattleZone_ReachesTheCorrectionBeforeTheSkillResolves()
        {
            var session = StartBombardSkillBesideAMinion(out var minionId, out var minionAt);

            ChooseBranch(session, MoveBranch);
            SelectSpace(session, minionAt);
            var map = session.Projection.SelectedMap;
            var outside = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .First(space => !BattleZoneQuery.IsInStartingBattleZone(map, space));
            SelectSpace(session, outside);

            var correction = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(correction, Is.Not.Null,
                "p.18 corrects a minion miniature that ends up outside the Battle Zone after the " +
                "action, whichever displacement carried it there.");
            Assert.That(correction.SubjectMinionId, Is.EqualTo(minionId));
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved),
                "The action is not over until the correction is, so the source card stays unresolved.");

            Accept(session, DirectorCommand.ChooseCorrectionDestination(correction.SelectInvocations[0]));

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "Only now does the whole action complete and the card resolve.");
        }

        // ---------------------------------------------------------------------------------------
        // The Spawn branch
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheSpawnBranch_IsNotOfferedWhileEveryDeclaredMiniatureStands()
        {
            // p.18's Spawn takes only from the supply, and at setup the supply is empty because every
            // declared miniature is on the board. Milestone 6.5ac: the mode is therefore refused
            // rather than offered, which is the defect this fixture used to pin - choosing it spent
            // the card, ended the turn, and changed nothing.
            var session = StartBombardSkill();
            Assert.That(RestoreBoard(session).FindUnitSupply(), Is.Empty,
                "Fixture precondition: nothing is in the supply yet.");

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { Spawn }),
                "An empty supply is exactly what makes this mode unperformable; the Move mode beside " +
                "it stays selectable, so the refusal is measured rather than blanket.");
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { MoveBranch }));
            Assert.That(RestoreBoard(session).Minions, Has.Count.EqualTo(12),
                "Nothing was spawned, and every declared miniature is still exactly where it was.");
        }

        [Test]
        public void TheSpawnBranch_BecomesSelectableOnceAMiniatureLeavesTheBoard()
        {
            // The same card in the same session, judged again against the state as it now stands:
            // feasibility is never written down, so a mode that becomes performable becomes
            // selectable.
            var session = StartBombardSkillAfterAMinionBattleRemoval(out var removed);

            Assert.That(
                RestoreBoard(session).FindUnitSupply().Select(unit => unit.MinionId),
                Is.EqualTo(new[] { removed }));
            Assert.That(session.Projection.PendingEffectChoice.UnavailableCandidates, Is.Empty,
                "A supply miniature and an adjacent unit make both modes performable at once.");
        }

        [Test]
        public void TheSpawnBranch_TakesOneSupplyMiniatureOntoItsOwnSpawnPoint()
        {
            var session = StartBombardSkillAfterAMinionBattleRemoval(out var removed);
            var beforeBoard = RestoreBoard(session);
            var reserved = beforeBoard.FindUnitSupply().Single();
            Assert.That(reserved.MinionId, Is.EqualTo(removed),
                "The Minion Battle put exactly that miniature back, which is where a Spawn draws from.");
            Assert.That(
                beforeBoard.TryGetSpawnPointCoordinate(reserved.SpawnPoint, out var spawnAt), Is.True);

            ChooseBranch(session, Spawn);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "Which miniature Spawns is the branch's one decision.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.Kind).Distinct(),
                Is.EqualTo(new[] { EffectValueKind.Minion }),
                "The Spawn branch names supply miniatures; it can take no object off the board, " +
                "which is exactly what separates it from Place.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.Value.MinionId),
                Is.EqualTo(new[] { removed }));

            SelectMinion(session, removed);

            var board = RestoreBoard(session);
            Assert.That(board.FindUnitSupply(), Is.Empty, "The supply is derived, so it simply shrank.");
            Assert.That(
                board.Minions.Single(minion => minion.MinionId.Equals(removed)).Coordinate,
                Is.EqualTo(spawnAt),
                "p.19: a Spawn point is the space a minion Spawns onto, and this capability declares " +
                "the miniature's own rather than offering a destination.");
            Assert.That(session.Projection.PendingMovementChoice, Is.Null,
                "Spawning is not movement: no Movement action was started and none is pending.");
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(BombardCardId))
                .State.ResolutionStatus, Is.Not.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void TheSpawnBranch_ClearsATokenCoveringItsSpawnPointInsideTheSameTransaction()
        {
            // p.19: "If a unit Spawns in a Spawn Point covered by a token, remove the token first,
            // then spawn the unit." Reached from the card rather than from the capability: the Red
            // hero stands beside red-melee-01's declared Spawn point, the Minion Battle puts that
            // miniature into the supply, the hand's token Skill then covers the emptied point, and
            // Red Bombard Spawns onto it the round after.
            var session = StartBombardSkillOverATokenCoveredSpawnPoint();
            Assert.That(
                RestoreBoard(session).PlacedTokens.Select(token => token.Coordinate),
                Has.Member(RedMeleeSpawnAt),
                "Fixture precondition: a token really covers that Spawn point.");

            ChooseBranch(session, Spawn);
            SelectMinion(session, RemovedMinion);

            var board = RestoreBoard(session);
            Assert.That(
                board.PlacedTokens.Any(token => token.Coordinate.Equals(RedMeleeSpawnAt)), Is.False,
                "The covering token went back to its supply as the Spawn's own first step.");
            Assert.That(
                board.Minions.Single(minion => minion.MinionId.Equals(RemovedMinion)).Coordinate,
                Is.EqualTo(RedMeleeSpawnAt),
                "One transaction: the clearance and the miniature standing up are not two commands a " +
                "player could be interrupted between.");
        }

        [Test]
        public void TheSpawnBranch_RefusesAMiniatureItNeverOffered()
        {
            var session = StartBombardSkillAfterAMinionBattleRemoval(out var removed);
            ChooseBranch(session, Spawn);

            var before = session.Projection;
            var standing = RestoreBoard(session).Minions
                .First(minion => !minion.MinionId.Equals(removed));
            var illegal = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Minion(standing.MinionId)))));

            Assert.That(illegal.Accepted, Is.False,
                "A miniature already standing on the board is not in the supply, so no Spawn may " +
                "take it - p.18 rules that out by name.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot));
        }

        // ---------------------------------------------------------------------------------------
        // Determinism
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheMoveBranch_SurvivesSnapshotRestoreReplayAndRetainedPrefixUndo()
        {
            var session = StartBombardSkill();
            ChooseBranch(session, MoveBranch);

            var paused = session.Projection.PendingEffectChoice;
            var payload = session.CaptureSnapshot();
            var revived = StartBombardSkill();
            Assert.That(revived.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(
                revived.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(paused.Candidates.Select(candidate => candidate.Value.Coordinate)),
                "Candidates are never written; the restored handler re-derives the same offer.");

            SelectSpace(session, BlueAt);
            var destination = session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate;
            var selection = session.Projection.PendingEffectChoice.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.Coordinate.Equals(destination));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void TheSpawnBranch_SurvivesSnapshotRestoreAndRetainedPrefixUndo()
        {
            var session = StartBombardSkillAfterAMinionBattleRemoval(out var removed);
            ChooseBranch(session, Spawn);

            var paused = session.Projection.PendingEffectChoice;
            var payload = session.CaptureSnapshot();
            var revived = StartBombardSkillAfterAMinionBattleRemoval(out _);
            Assert.That(revived.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(
                revived.Projection.PendingEffectChoice.Candidates.Select(candidate => candidate.Value.MinionId),
                Is.EqualTo(paused.Candidates.Select(candidate => candidate.Value.MinionId)),
                "The supply is derived on restore, so the offer is re-asked rather than replayed.");

            var selection = paused.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.MinionId.Equals(removed));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Assert.That(
                RestoreBoard(session).FindUnitSupply().Select(unit => unit.MinionId), Has.Member(removed),
                "Retained-prefix Undo puts the miniature back in the supply.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void AStaleOrForgedModeAnswer_RejectsWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartBombardSkill();
            var choice = session.Projection.PendingEffectChoice;
            var snapshot = session.Projection.AuthoritativeSnapshot;

            var stale = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                new EffectChoiceId("stale-bombard-choice"),
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(MoveBranch))));
            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(new EffectNodeId("bombard-no-such-branch")))));

            Assert.That(stale.Accepted, Is.False);
            Assert.That(forged.Accepted, Is.False);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(session.Projection.PendingEffectChoice.Candidates, Is.EqualTo(choice.Candidates));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static EffectDefinition BombardEffect() =>
            PlaceholderMovementHand.CreateRed(new HeroId("red-synthetic-hero"))
                .Definitions.Single(candidate => candidate.DisplayName == "TEST - Red Bombard")
                .ActionDefinitions.Single(action => action.Id.Equals(BombardSkill))
                .EffectRoot;

        private static HeroId BlueHero(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero => session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

        private static HeroId RedHero(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero => session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red)
                .Id;

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        /// <summary>
        /// Commits Red Bombard at Initiative 5 against Blue Longbow at 4, so the Red hero acts first
        /// and its Skill is the current actor's offer. Blue's is the only card below Red Bombard's
        /// Initiative, which is why every fixture here spends it on the Bombard round.
        /// </summary>
        private static MatchSession StartBombardSkill(
            HexCoordinate? redAt = null, HexCoordinate? blueAt = null)
        {
            var session = StartBombardTurnWithoutStarting(redAt, blueAt);
            StartTheSkill(session);
            return session;
        }

        /// <summary>
        /// The same turn with the Skill left unstarted, for the positions milestone 6.5af refuses to
        /// start at all. Everything up to the action choice is identical.
        /// </summary>
        private static MatchSession StartBombardTurnWithoutStarting(
            HexCoordinate? redAt = null, HexCoordinate? blueAt = null)
        {
            var session = CreateBombardSession(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                redAt ?? RedAt,
                blueAt ?? BlueAt);
            CommitCards(session, "bombard", "longbow");
            return session;
        }

        /// <summary>
        /// The corpus fixture's correction geography: the Red hero at (0,-1) with red-melee-01 adjacent
        /// at (-1,-1), where a direct Move of 2 can carry the minion out of the Battle Zone.
        /// </summary>
        private static MatchSession StartBombardSkillBesideAMinion(
            out MinionId minionId, out HexCoordinate minionAt)
        {
            minionId = new MinionId("red-melee-01");
            minionAt = new HexCoordinate(-1, -1);
            return StartBombardSkill(new HexCoordinate(0, -1), new HexCoordinate(-3, 10));
        }

        /// <summary>
        /// A session that has played one round to its Minion Battle and removed a Red miniature, so
        /// the supply a Spawn draws from is not empty. Red is the losing side by construction - its
        /// setup declares one fewer miniature - because p.8 has the losing team remove theirs, and the
        /// Red hero is the one whose Skill this fixture reaches.
        /// </summary>
        private static MatchSession StartBombardSkillAfterAMinionBattleRemoval(
            out MinionId removed, HexCoordinate? blueAt = null)
        {
            var session = CreateBombardSession(RedOutnumberedSetup(), RedAt, blueAt ?? BlueAt);
            removed = PlayFillerRoundAndRemove(session, RemovedMinion);
            CommitCards(session, "bombard", "longbow");
            StartTheSkill(session);
            return session;
        }

        /// <summary>
        /// The three-round route to a token-covered Spawn point, all of it through ordinary commands.
        /// It needs the Skill-fixture hand, because the token placement lives on milestone 6.2's Cache
        /// card and nothing in the five-card hand can put a token anywhere.
        ///
        /// The geography is the reason this is reachable at all: every configured minion starts on its
        /// own declared Spawn point, so the point is covered while its miniature stands there, and the
        /// Red hero is stood at (0,-1) beside red-melee-01's point at (-1,-1) so that the Cache card's
        /// Radius 1 placement can reach it once the Minion Battle has emptied it.
        /// </summary>
        private static MatchSession StartBombardSkillOverATokenCoveredSpawnPoint()
        {
            var session = CreateBombardSession(
                RedOutnumberedSetup(),
                BesideRedMeleeSpawn,
                new HexCoordinate(-3, 10),
                withSkillCards: true);

            PlayFillerRoundAndRemove(session, RemovedMinion);
            Assert.That(
                RestoreBoard(session).FindUnitSupply().Select(unit => unit.MinionId),
                Is.EqualTo(new[] { RemovedMinion }),
                "The Minion Battle emptied the Spawn point this fixture is about.");

            CommitCards(session, "cache", "cache");
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(ComponentSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            ChooseBranch(session, TokenPlaceBranch);
            SelectSpace(session, RedMeleeSpawnAt);
            AdvanceToNextCommitments(session);

            CommitCards(session, "bombard", "longbow");
            StartTheSkill(session);
            return session;
        }

        /// <summary>
        /// Plays a whole round out on cards neither side needs later, every hero holding, and answers
        /// the Minion Battle it ends in by removing the named miniature. Hands are taken back at the
        /// round boundary, so the cards spent here are available again afterwards.
        ///
        /// Red Bombard, Blue Longbow and the Cache cards are the ones deliberately not spent: Blue
        /// Longbow at 4 is the only Blue Initiative below Red Bombard's 5, and so the one card that
        /// makes the Red hero act first on the round these fixtures care about.
        /// </summary>
        private static MinionId PlayFillerRoundAndRemove(MatchSession session, MinionId preferred)
        {
            for (var step = 0; step < 64; step++)
            {
                var battle = session.Projection.PendingMinionBattleChoice;
                if (battle != null)
                {
                    var invocation = battle.SelectInvocations
                        .FirstOrDefault(candidate => candidate.MinionId.Equals(preferred));
                    if (!invocation.MinionId.IsDefined)
                    {
                        invocation = battle.SelectInvocations[0];
                    }

                    Accept(session, DirectorCommand.SelectMinionBattleRemoval(invocation));
                    Accept(session, DirectorCommand.ConfirmMinionBattleRemovals(
                        session.Projection.PendingMinionBattleChoice.ConfirmInvocation));
                    AdvanceToNextCommitments(session);
                    return invocation.MinionId;
                }

                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitFillerCards(session);
                    continue;
                }

                if (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
                {
                    Accept(session, DirectorCommand.AdvanceEndOfRound());
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            Assert.Fail("The fixture never reached a Minion Battle.");
            return default;
        }

        /// <summary>Finishes whatever the current round has left, stopping at the next commitments.</summary>
        private static void AdvanceToNextCommitments(MatchSession session)
        {
            for (var step = 0; step < 64; step++)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    return;
                }

                if (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
                {
                    Accept(session, DirectorCommand.AdvanceEndOfRound());
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }

            Assert.Fail("The fixture never reached the next commitment phase.");
        }

        private static void CommitFillerCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = HandCards(session, side)
                    .Where(candidate => !candidate.Id.Value.Contains("bombard") &&
                        !candidate.Id.Value.Contains("longbow") &&
                        !candidate.Id.Value.Contains("cache"))
                    .OrderBy(candidate => Initiative(session, candidate))
                    .First();
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static IEnumerable<CardInstance> HandCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand)
                .ToArray();

        private static int Initiative(MatchSession session, CardInstance card) =>
            session.Projection.Definitions
                .Single(value => value.Reference.Equals(card.DefinitionReference)).Initiative.Value;

        private static void StartTheSkill(MatchSession session)
        {
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(BombardCardId));
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(BombardSkill));
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
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == pair.Side &&
                    candidate.Id.Value.Contains(pair.Fragment) &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "The committed Initiatives differ, so no tie has to be steered.");
        }

        private static void ChooseBranch(MatchSession session, EffectNodeId branch)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.OptionNodeId.Equals(branch))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void SelectSpace(MatchSession session, HexCoordinate coordinate)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.Value.Coordinate.Equals(coordinate))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void SelectMinion(MatchSession session, MinionId minionId)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations.Single(
                candidate => candidate.Candidate.Value.MinionId.Equals(minionId))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static CardActionResolutionEvent[] SkillFacts(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value => value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.SkillActionProgressed)
                .Select(value => value.TurnEvent.CardActionEvent)
                .ToArray();

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions).Board;

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static QuickGameSetupConfiguration RedOutnumberedSetup()
        {
            var full = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var dropped = full.Minions.First(value =>
                value.TeamSide == TeamSide.Red && value.MinionType == StandardMinionType.Ranged);
            return new QuickGameSetupConfiguration(
                full.RequiredMap,
                full.WaveCount,
                full.LifeCountPerTeam,
                full.Heroes,
                full.Minions.Where(value => !value.MinionId.Equals(dropped.MinionId)));
        }

        private static MatchSession CreateBombardSession(
            QuickGameSetupConfiguration configured,
            HexCoordinate redAt,
            HexCoordinate blueAt,
            bool withSkillCards = false)
        {
            var configuration = withSkillCards
                ? PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Red)
                : PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                configured, configuration.Participants, map, configuration.TieBreakerState);
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
                    new HeroOccupancy(redHeroId, redAt),
                    new HeroOccupancy(blueHeroId, blueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
