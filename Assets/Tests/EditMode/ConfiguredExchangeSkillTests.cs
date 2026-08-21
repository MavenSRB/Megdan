using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
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
    /// Milestone 6.6a. Blue Sentinel's printed Primary Attack is replaced by the modal Skill
    /// `Choose one: Swap your hero with another adjacent unit; or Replace an adjacent Hazard token
    /// with a Ward from your supply; or Remove an adjacent token to its supply`.
    ///
    /// Everything the three branches do comes from milestones 6.6, 6.8k and 6.8l; what this fixture
    /// proves is the binding, the three declared subjects, and the player-visible behaviour. It runs
    /// on real official-map geography, because the Battle Zone the Swap's correction depends on is map
    /// content and a synthetic coordinate board declares none.
    ///
    /// Two of the three branches need a token standing beside the acting hero, and no initial scenario
    /// can seed one: a token arrives by being placed during play. So those fixtures play a first turn
    /// in which the Blue hero places a Hazard from milestone 6.2's Cache Skill and reach the Sentinel
    /// on the turn after, which is how an ordinary match reaches one too.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class ConfiguredExchangeSkillTests
    {
        private static readonly CardInstanceId SentinelCardId = new CardInstanceId("blue-sentinel-card");

        private static readonly ActionDefinitionId SentinelSkill =
            new ActionDefinitionId("primary-exchange-skill");

        private static readonly ActionDefinitionId ComponentSkill =
            new ActionDefinitionId("primary-component-skill");

        private static readonly EffectNodeId TokenPlaceBranch =
            new EffectNodeId("component-place-branch");

        private static readonly EffectNodeId SwapBranch = new EffectNodeId("sentinel-swap-branch");
        private static readonly EffectNodeId SwapSelect = new EffectNodeId("sentinel-swap-select");
        private static readonly EffectNodeId Swap = new EffectNodeId("sentinel-swap");
        private static readonly EffectNodeId ReplaceBranch = new EffectNodeId("sentinel-replace-branch");
        private static readonly EffectNodeId ReplaceSelect = new EffectNodeId("sentinel-replace-select");
        private static readonly EffectNodeId Replace = new EffectNodeId("sentinel-replace");
        private static readonly EffectNodeId RemoveBranch = new EffectNodeId("sentinel-remove-branch");
        private static readonly EffectNodeId RemoveSelect = new EffectNodeId("sentinel-remove-select");
        private static readonly EffectNodeId Remove = new EffectNodeId("sentinel-remove");

        /// <summary>The Red hero's own spawn point, well away from the Battle Zone and from anything.</summary>
        private static readonly HexCoordinate Elsewhere = new HexCoordinate(-2, -5);

        /// <summary>The far corner the Blue hero stands in when the case is about standing alone.</summary>
        private static readonly HexCoordinate Alone = new HexCoordinate(-3, 10);

        // ---------------------------------------------------------------------------------------
        // Definition
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BlueSentinel_ReplacesOnlyItsPrintedPrimaryAttack()
        {
            var hand = PlaceholderMovementHand.CreateBlue(new HeroId("blue-synthetic-hero"));
            var definition = hand.Definitions.Single(candidate =>
                candidate.DisplayName == "TEST - Blue Sentinel");

            Assert.That(definition.Id, Is.EqualTo(new CardDefinitionId("placeholder-blue-sentinel")),
                "The conversion advances the revision, never the card's identity.");
            Assert.That(definition.ContentRevision, Is.EqualTo("phase-7-1-rev-1"));
            Assert.That(definition.ContentAuthority,
                Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(definition.Initiative.Value, Is.EqualTo(11));
            Assert.That(hand.Instances[1].DefinitionReference, Is.EqualTo(definition.Reference),
                "It keeps its five-card-hand slot as well as its identity.");
            Assert.That(hand.DealtDefinitions, Has.Count.EqualTo(5), "The hand keeps its five-card shape.");

            var primary = definition.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill));
            Assert.That(primary.Id, Is.EqualTo(SentinelSkill));
            Assert.That(primary.BaseValues, Is.Empty,
                "A Skill prints no value channel, so the replaced Attack says nothing.");
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
                Is.EqualTo(4),
                "The Defense reaction is untouched, which is what keeps this card the Defense 4 " +
                "half of every equal-Attack-and-Defense fixture.");
        }

        [Test]
        public void TheBlueHand_StillKeepsItsPrintedNonRangedAndRangedAttacks()
        {
            var attacks = PlaceholderMovementHand.CreateBlue(new HeroId("blue-synthetic-hero"))
                .Definitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Attack &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .ToArray();

            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.NonRanged), Is.EqualTo(1),
                "Blue Hammer is the team's one remaining printed non-Ranged Attack.");
            Assert.That(
                attacks.Count(action => action.AttackRange == AttackRangeKind.Ranged),
                Is.GreaterThanOrEqualTo(1),
                "Blue Scout Shot prints the team's Ranged Attack; 6.7a converted Blue Longbow.");
        }

        [Test]
        public void TheSkill_DeclaresTheThreeBranchesTheCardTextNames()
        {
            var effect = SentinelEffect();

            var root = effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(root.Chooser, Is.EqualTo(EffectChooserRole.Actor));
            Assert.That(root.ChildNodeIds, Is.EqualTo(new[] { SwapBranch, ReplaceBranch, RemoveBranch }));

            foreach (var pair in new[]
            {
                new { Selection = SwapSelect, Consumer = Swap },
                new { Selection = ReplaceSelect, Consumer = Replace },
                new { Selection = RemoveSelect, Consumer = Remove }
            })
            {
                var selection = effect.Nodes.Single(node => node.Id.Equals(pair.Selection));
                Assert.That(selection.Capability,
                    Is.EqualTo(SyntheticTargetingCapabilities.ChooseTargetV3),
                    "p.19 gives a Swap, a Replace and a Remove no activity of their own, so each " +
                    "offer asks the enclosing Skill's rather than naming one a second time.");
                Assert.That(
                    selection.Inputs.Single(input => input.ParameterName == "range").Literal.IntegerValue,
                    Is.EqualTo(1), "`adjacent` is Radius 1.");
                Assert.That(
                    selection.Inputs.Single(input => input.ParameterName == "includeSelf")
                        .Literal.BooleanValue,
                    Is.False, "`another adjacent` excludes the acting hero.");
                Assert.That(
                    selection.Inputs.Single(input =>
                        input.ParameterName == EffectSelectionDeclaration.ConsumerParameterName)
                        .Literal.NodeId,
                    Is.EqualTo(pair.Consumer),
                    "Each selection names the step its chosen object is for, so milestone 6.8l can " +
                    "judge the branch by what that step could do.");
            }

            // `your hero` is neither a decision nor a step: the Swap declaration whose first object is
            // the actor reads it from the frame, so the consumer is answerable when the branch is
            // judged as well as when the choice is offered. A bridge node publishing the actor's space
            // could only be answerable at the second of those.
            var swapBranch = effect.Nodes.Single(node => node.Id.Equals(SwapBranch));
            Assert.That(swapBranch.Kind, Is.EqualTo(EffectNodeKind.Sequence));
            Assert.That(swapBranch.ChildNodeIds, Is.EqualTo(new[] { SwapSelect, Swap }));
            Assert.That(
                effect.Nodes.Any(node =>
                    node.Capability.Equals(SyntheticPlacementCapabilities.ActorSpaceV1)), Is.False);

            var swap = effect.Nodes.Single(node => node.Id.Equals(Swap));
            Assert.That(swap.Capability, Is.EqualTo(SyntheticExchangeCapabilities.SwapWithActorV1));
            Assert.That(EligiblePiece(swap), Is.EqualTo((int)SwapEligiblePieceKind.Unit),
                "`another adjacent unit` is narrower than p.19's two objects, and the operation is " +
                "where the card says so.");
            Assert.That(
                swap.Inputs.Single(input => input.ParameterName == "subject").SourceNodeId,
                Is.EqualTo(SwapSelect));

            var replace = effect.Nodes.Single(node => node.Id.Equals(Replace));
            Assert.That(replace.Capability, Is.EqualTo(SyntheticExchangeCapabilities.ReplaceTokenV1));
            Assert.That(
                replace.Inputs.Single(input => input.ParameterName == "subject").SourceNodeId,
                Is.EqualTo(ReplaceSelect));
            Assert.That(replace.Inputs, Has.Count.EqualTo(1),
                "Which token comes in, where the outgoing one goes, that nothing transfers and that " +
                "observers see one replacement are the capability's declaration; a card restating " +
                "them would be stating a rule of its own.");

            var remove = effect.Nodes.Single(node => node.Id.Equals(Remove));
            Assert.That(remove.Capability, Is.EqualTo(SyntheticExchangeCapabilities.RemoveV2));
            Assert.That(EligiblePiece(remove), Is.EqualTo((int)RemoveEligiblePieceKind.Token),
                "Turn-time Remove of the last minion out of the Battle Zone is one of p.8's two " +
                "causes and nothing can emit that push until milestone 6.9, so no configured card " +
                "may reach it.");

            Assert.That(
                EffectDefinitionValidator.Validate(effect, SyntheticEffectCapabilities.CreateCatalog()),
                Is.Empty);
        }

        // ---------------------------------------------------------------------------------------
        // The Swap branch
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheSwapBranch_ExchangesTheActingHeroWithTheAdjacentUnitItChose()
        {
            var session = StartSentinelSkill(out var actorAt, out var minion, out var minionAt);
            var actorId = session.Projection.SelectedActor.ActorId;

            ChooseBranch(session, SwapBranch);
            SelectSpace(session, minionAt);

            var board = RestoreBoard(session);
            Assert.That(Coordinate(board, actorId), Is.EqualTo(minionAt));
            Assert.That(
                board.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate,
                Is.EqualTo(actorAt),
                "p.19: two objects swap places with each other, in one transaction.");
            Assert.That(board.Minions.Count, Is.EqualTo(12), "Nothing left the board.");
            Assert.That(session.Projection.PendingMovementChoice, Is.Null,
                "A Swap does not count as movement or placement (p.19), so no Movement action began.");
            Assert.That(
                SkillFacts(session).Select(fact => fact.Kind).Distinct(),
                Is.SubsetOf(new[]
                {
                    CardActionResolutionEventKind.SkillActionStarted,
                    CardActionResolutionEventKind.SkillChoiceRequested,
                    CardActionResolutionEventKind.SkillEffectNodeApplied,
                    CardActionResolutionEventKind.SkillActionCompleted
                }),
                "The stream is a Skill's, with no Movement or Place fact anywhere in it.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null, "The Skill completes.");
        }

        [Test]
        public void TheSwapBranch_IsRefusedWhereTheOnlyThingAdjacentIsATokenItCannotTake()
        {
            // The case the Swap's declaration was chosen for. A token stands beside the acting hero
            // and nothing else does, so the mode has a legal target and no piece it may exchange -
            // and because this Swap's first object is the actor rather than a space a prior node
            // publishes, the option walk can ask that question before the mode is ever offered.
            var session = StartSentinelSkillBesideOnlyAToken(out var tokenAt);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.UnavailableCandidates, Has.Member(EffectChoiceCandidate.Option(SwapBranch)),
                "Offering it would spend the action on a stop, which is the defect this route has " +
                "corrected in five other forms.");
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { ReplaceBranch, RemoveBranch }),
                "The two modes that do work on a token are offered, so the refusal is measured " +
                "rather than blanket.");
            Assert.That(RestoreBoard(session).PlacedTokens.Single().Coordinate, Is.EqualTo(tokenAt));
        }

        [Test]
        public void TheSwapBranch_NamesTheAdjacentUnitAndNeverTheAdjacentToken()
        {
            // The narrowing a player meets. p.19 exchanges any two objects, this card says unit, and
            // the token standing beside the hero is a legal target the Swap cannot act on - which is
            // exactly the pairing milestone 6.8l lets a selection state.
            var session = StartSentinelSkillBesideAToken(out var minionAt, out var tokenAt);

            ChooseBranch(session, SwapBranch);

            var offered = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(offered, Has.Member(minionAt));
            Assert.That(offered, Has.No.Member(tokenAt),
                "p.19 states that tokens are not units, so the Swap declaring unit names none.");
        }

        [Test]
        public void ASwapLeavingAMinionOutsideTheBattleZone_ReachesTheAfterActionCorrection()
        {
            // The acting hero stands outside the Battle Zone beside a minion inside it, so exchanging
            // the two puts an intrinsic minion miniature where p.18 says it may not stay. Milestone
            // 6.4a's correction runs at the action boundary and every Skill reaches it, so a Swap
            // inherits it rather than declaring one of its own.
            var session = StartSentinelSkill(
                true, out var actorAt, out var minion, out var minionAt);
            var board = RestoreBoard(session);
            Assert.That(BattleZoneQuery.IsInCurrentBattleZone(board, minionAt), Is.True);
            Assert.That(BattleZoneQuery.IsInCurrentBattleZone(board, actorAt), Is.False,
                "The fixture is only meaningful if the exchange really strands the minion.");

            ChooseBranch(session, SwapBranch);
            SelectSpace(session, minionAt);

            var corrected = RestoreBoard(session);
            var correction = session.Projection.PendingDisplacedMinionCorrection;
            var landed = corrected.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate;
            Assert.That(
                correction != null ||
                    BattleZoneQuery.IsInCurrentBattleZone(corrected, landed),
                Is.True,
                "p.18 corrects a displaced minion at the after-action checkpoint: either the owning " +
                "team is being asked where it returns, or a deterministic correction already ran.");
        }

        // ---------------------------------------------------------------------------------------
        // The Replace branch
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheReplaceBranch_SubstitutesAWardForTheAdjacentHazardInOneOperation()
        {
            var session = StartSentinelSkillBesideAToken(out _, out var tokenAt);
            var catalog = SyntheticComponentDefinitions.CreateCatalog();

            ChooseBranch(session, ReplaceBranch);
            SelectSpace(session, tokenAt);

            var tokens = RestoreBoard(session).PlacedTokens;
            var standing = tokens.Single(token => token.Coordinate.Equals(tokenAt));
            Assert.That(
                standing.Definition.Reference, Is.EqualTo(SyntheticComponentDefinitions.WardV1),
                "p.19 requires a different object, so a Ward stands where the Hazard did.");
            Assert.That(
                catalog.CountAvailableTokens(SyntheticComponentDefinitions.HazardV4, tokens),
                Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount),
                "The outgoing Hazard went back to its own supply.");
            Assert.That(
                catalog.CountAvailableTokens(SyntheticComponentDefinitions.WardV1, tokens),
                Is.EqualTo(0),
                "And the incoming one came out of its finite supply, which is now empty.");
            Assert.That(standing.Definition.Properties, Is.Empty,
                "No property transfers: p.19 ties a token's properties to the instance, so ending " +
                "the Hazard ends the continuous source it supplied.");
            Assert.That(RestoreBoard(session).IsEmptyPlayableSpace(tokenAt), Is.False,
                "One operation rather than a hidden Remove and Spawn: the space is never empty.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
        }

        [Test]
        public void TheReplaceBranch_IsRefusedWhileNoHazardItCanReplaceStands()
        {
            // The position milestone 6.8l was written for, on the card the amendment named. The
            // Replace needs an adjacent Hazard and none stands anywhere, and its selection can always
            // pause, so before a selection could say what it selects for the mode was offered and the
            // action and the card were spent on it.
            var session = StartSentinelSkill(out _, out _, out _);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Has.No.Member(ReplaceBranch),
                "A branch whose selection can name nothing its consumer accepts carries no " +
                "invocation, which is what stops the card being spent on it.");
            Assert.That(
                choice.UnavailableCandidates, Has.Member(EffectChoiceCandidate.Option(ReplaceBranch)),
                "It stays greyed among the options rather than disappearing: the mode exists.");
            Assert.That(RestoreBoard(session).PlacedTokens, Is.Empty, "Nothing was placed to find out.");
        }

        [Test]
        public void TheReplaceBranch_IsRefusedOnceItsIncomingSupplyIsExhausted()
        {
            // The Ward supply is one, so the second Replace has nothing to bring in - which is the
            // finite-supply half of the declaration, read against live state rather than stored.
            var session = StartSentinelSkillBesideAToken(out _, out var tokenAt);
            ChooseBranch(session, ReplaceBranch);
            SelectSpace(session, tokenAt);

            var judged = EffectOptionFeasibility(session, ReplaceBranch);

            Assert.That(judged, Is.False,
                "A Ward already stands in the only space this branch could work in, and none is left " +
                "in the supply either.");
        }

        // ---------------------------------------------------------------------------------------
        // The Remove branch
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheRemoveBranch_TakesTheAdjacentTokenBackToItsDeclaredSupply()
        {
            var session = StartSentinelSkillBesideAToken(out _, out var tokenAt);
            var catalog = SyntheticComponentDefinitions.CreateCatalog();

            ChooseBranch(session, RemoveBranch);
            SelectSpace(session, tokenAt);

            var board = RestoreBoard(session);
            Assert.That(board.PlacedTokens, Is.Empty);
            Assert.That(board.IsEmptyPlayableSpace(tokenAt), Is.True);
            Assert.That(
                catalog.CountAvailableTokens(SyntheticComponentDefinitions.HazardV4, board.PlacedTokens),
                Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount),
                "The declared destination is the supply, and the piece is genuinely back in it.");
            Assert.That(board.Minions.Count, Is.EqualTo(12),
                "A Remove is not a Defeat and not a Clear: nothing else left the board.");
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null,
                "Removing a token empties nothing p.8 counts.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
        }

        [Test]
        public void TheRemoveSelection_NamesOnlyTokensAndNeverTheAdjacentMinion()
        {
            // The declared narrowing that keeps a turn-time last-minion Remove unexposed until
            // milestone 6.9 can emit p.8's immediate Lane-Push request. Both objects are legal
            // targets; only one of them is a piece this Remove may take.
            var session = StartSentinelSkillBesideAToken(out var minionAt, out var tokenAt);

            ChooseBranch(session, RemoveBranch);

            var offered = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(offered, Is.EqualTo(new[] { tokenAt }));
            Assert.That(offered, Has.No.Member(minionAt),
                "The miniature is targetable and unremovable by this card, which is the difference " +
                "the selection can state since milestone 6.8l.");
        }

        // ---------------------------------------------------------------------------------------
        // The action as a whole
        // ---------------------------------------------------------------------------------------

        [Test]
        public void WhereNoModeCanBeTaken_TheSkillCannotBeStartedAndTheMovementStillCan()
        {
            // Milestone 6.5af. Nothing stands beside the acting hero, so the Swap names nothing, and
            // no token stands anywhere, so neither does the Replace or the Remove. The Skill is judged
            // before it is offered, so the turn is still the player's to spend - and this card prints
            // a Secondary Movement 2 beside the Skill. Rulebook p.12: a player is never forced to act.
            var session = StartSentinelTurnWithoutStarting(actorAt: Alone);

            var skill = session.Projection.AvailableTurnActions
                .Single(offer => offer.ActionDefinitionId.Equals(SentinelSkill));
            Assert.That(skill.IsAvailable, Is.False);
            Assert.That(
                session.Projection.AvailableTurnActions
                    .Where(offer => offer.IsAvailable)
                    .Select(offer => offer.ActionDefinitionId),
                Is.Not.Empty);
            Assert.That(session.Execute(DirectorCommand.StartAction(SentinelSkill)).Accepted, Is.False,
                "It cannot be started, so the turn is not spent on it.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(SentinelCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void EveryModeIsProjected_AndOnlyTheOnesWithSomethingToDoCarryAnInvocation()
        {
            var session = StartSentinelSkill(out _, out _, out _);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { SwapBranch, ReplaceBranch, RemoveBranch }),
                "All three modes are projected; refusing one is not hiding it.");
            Assert.That(
                choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { SwapBranch }),
                "Only the Swap has anything to work on here: no token stands anywhere on the board.");
            Assert.That(choice.StopInvocation, Is.Null,
                "There is a decision to make, so milestone 6.5ae offers no stop.");
        }

        // ---------------------------------------------------------------------------------------
        // Determinism
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheSwapBranch_SurvivesSnapshotRestoreReplayAndRetainedPrefixUndo()
        {
            var session = StartSentinelSkill(out var actorAt, out _, out var minionAt);
            var actorId = session.Projection.SelectedActor.ActorId;
            ChooseBranch(session, SwapBranch);

            var paused = session.Projection.PendingEffectChoice;
            var payload = session.CaptureSnapshot();
            var revived = StartSentinelSkill(out _, out _, out _);
            Assert.That(revived.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(
                revived.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(paused.Candidates.Select(candidate => candidate.Value.Coordinate)),
                "Candidates are never written; the restored handler re-derives the same offer, and " +
                "the consumer re-narrows it.");

            var selection = paused.SelectInvocations
                .Single(candidate => candidate.Candidate.Value.Coordinate.Equals(minionAt));
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(minionAt));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Assert.That(Coordinate(RestoreBoard(session), actorId), Is.EqualTo(actorAt),
                "Retained-prefix Undo puts both objects back.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same occupancy and the same ordered facts.");
        }

        [Test]
        public void TheReplaceBranch_SurvivesSnapshotRestoreAndRetainedPrefixUndo()
        {
            var session = StartSentinelSkillBesideAToken(out _, out var tokenAt);
            ChooseBranch(session, ReplaceBranch);

            var paused = session.Projection.PendingEffectChoice;
            Assert.That(
                paused.Candidates.Select(candidate => candidate.Value.Coordinate),
                Is.EqualTo(new[] { tokenAt }),
                "Only the Hazard is a piece this Replace can act on.");

            var selection = paused.SelectInvocations.Single();
            Accept(session, DirectorCommand.AnswerEffectChoice(selection));
            var beforeConfirm = session.Projection.DiagnosticSnapshot;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterCommit = session.Projection.DiagnosticSnapshot;

            var restored = Restore(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A committed Replace round-trips with no field the writer and reader disagree on.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeConfirm));
            Assert.That(
                RestoreBoard(session).PlacedTokens.Single().Definition.Reference,
                Is.EqualTo(SyntheticComponentDefinitions.HazardV4),
                "Retained-prefix Undo puts the Hazard back and the Ward into its supply.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterCommit),
                "Replaying the same answer reproduces the same components and the same ordered facts.");
        }

        [Test]
        public void AStaleOrForgedAnswer_RejectsWithoutTouchingTheFrameOrTheCard()
        {
            var session = StartSentinelSkill(out _, out _, out _);
            var choice = session.Projection.PendingEffectChoice;
            var snapshot = session.Projection.AuthoritativeSnapshot;

            var stale = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                new EffectChoiceId("stale-sentinel-choice"),
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(SwapBranch))));
            var forged = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(new EffectNodeId("sentinel-no-such-branch")))));
            var greyed = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                choice.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(RemoveBranch))));

            Assert.That(stale.Accepted, Is.False);
            Assert.That(forged.Accepted, Is.False);
            Assert.That(greyed.Accepted, Is.False,
                "A mode the authority refused carries no invocation, and naming it anyway is refused " +
                "on the command path too.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot));
            Assert.That(session.Projection.PendingEffectChoice.Candidates, Is.EqualTo(choice.Candidates));
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(SentinelCardId))
                .State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void ASwapTargetTheSelectionNeverOffered_IsRefusedAtomically()
        {
            var session = StartSentinelSkillBesideAToken(out _, out var tokenAt);
            ChooseBranch(session, SwapBranch);

            var before = session.Projection;
            var illegal = session.Execute(DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                before.PendingEffectChoice.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(tokenAt)))));

            Assert.That(illegal.Accepted, Is.False,
                "The token is standing there and is still not a unit.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "The refusal touches no card, occupancy, continuation, or event.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static EffectDefinition SentinelEffect() =>
            PlaceholderMovementHand.CreateBlue(new HeroId("blue-synthetic-hero"))
                .Definitions.Single(candidate => candidate.DisplayName == "TEST - Blue Sentinel")
                .ActionDefinitions.Single(action => action.Id.Equals(SentinelSkill))
                .EffectRoot;

        private static int EligiblePiece(EffectNodeDefinition node) =>
            node.Inputs
                .Single(input =>
                    input.ParameterName == SyntheticExchangeCapabilities.EligiblePieceParameterName)
                .Literal.IntegerValue;

        private static HexCoordinate Coordinate(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        /// <summary>Whether the pending mode choice still offers this branch.</summary>
        private static bool EffectOptionFeasibility(MatchSession session, EffectNodeId branch) =>
            session.Projection.PendingEffectChoice != null &&
            session.Projection.PendingEffectChoice.SelectInvocations
                .Any(invocation => invocation.Candidate.OptionNodeId.Equals(branch));

        private static MatchSession StartSentinelSkill(
            out HexCoordinate actorAt, out MinionId minion, out HexCoordinate minionAt) =>
            StartSentinelSkill(false, out actorAt, out minion, out minionAt);

        /// <summary>
        /// A session whose Blue hero stands beside a non-Heavy minion, with the Sentinel Skill started
        /// and its mode choice pending.
        ///
        /// The minion is non-Heavy on purpose: p.13 makes a Heavy immune while friendly non-Heavy
        /// minions stand in the Battle Zone, which would make every case here an immunity case.
        /// </summary>
        private static MatchSession StartSentinelSkill(
            bool actorOutsideBattleZone,
            out HexCoordinate actorAt,
            out MinionId minion,
            out HexCoordinate minionAt)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = CreateSetup(map, out var configuration);
            var candidate = BesideANonHeavyMinion(map, setup, actorOutsideBattleZone);
            minion = candidate.Minion.MinionId;
            minionAt = candidate.Minion.Coordinate;
            actorAt = candidate.Beside;

            var session = CreateSession(map, setup, configuration, candidate.Beside);
            CommitCards(session, "pursuit", "sentinel");
            StartTheSkill(session);
            return session;
        }

        /// <summary>
        /// The same board, one turn later, with a Hazard token standing beside the acting hero.
        ///
        /// No initial scenario can seed a token, so this places one the way a match does: the Blue
        /// hero spends the earlier turn on milestone 6.2's Cache Skill, whose placement takes an empty
        /// space adjacent to the actor. Red commits its lowest card each time, so the Blue hero acts
        /// second on that turn and first on the Sentinel's.
        /// </summary>
        private static MatchSession StartSentinelSkillBesideAToken(
            out HexCoordinate minionAt, out HexCoordinate tokenAt)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = CreateSetup(map, out var configuration);
            var candidate = BesideANonHeavyMinion(map, setup, false);
            minionAt = candidate.Minion.Coordinate;

            var session = CreateSession(map, setup, configuration, candidate.Beside, withSkillCards: true);
            CommitCards(session, "bombard", "cache");
            Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));

            var offer = session.Projection.AvailableTurnActions
                .Single(value => value.ActionDefinitionId.Equals(ComponentSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            ChooseBranch(session, TokenPlaceBranch);
            tokenAt = session.Projection.PendingEffectChoice.Candidates
                .Select(value => value.Value.Coordinate)
                .First(space => !space.Equals(candidate.Minion.Coordinate));
            SelectSpace(session, tokenAt);

            CommitCards(session, "pursuit", "sentinel");
            StartTheSkill(session);
            return session;
        }

        /// <summary>
        /// The acting hero alone in a far corner of the map with one Hazard beside it, placed the
        /// same way and for the same reason as in the fixture above.
        /// </summary>
        private static MatchSession StartSentinelSkillBesideOnlyAToken(out HexCoordinate tokenAt)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = CreateSetup(map, out var configuration);
            var session = CreateSession(map, setup, configuration, Alone, withSkillCards: true);

            CommitCards(session, "bombard", "cache");
            Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            var offer = session.Projection.AvailableTurnActions
                .Single(value => value.ActionDefinitionId.Equals(ComponentSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            ChooseBranch(session, TokenPlaceBranch);
            tokenAt = session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate;
            SelectSpace(session, tokenAt);

            CommitCards(session, "pursuit", "sentinel");
            StartTheSkill(session);
            return session;
        }

        /// <summary>The same turn with the Skill left unstarted, for the position 6.5af refuses.</summary>
        private static MatchSession StartSentinelTurnWithoutStarting(HexCoordinate actorAt)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = CreateSetup(map, out var configuration);
            var session = CreateSession(map, setup, configuration, actorAt);
            CommitCards(session, "pursuit", "sentinel");
            return session;
        }

        private static QuickGameSetupState CreateSetup(
            BoardMapDefinition map, out PlaceholderOneVsOneTestConfiguration configuration)
        {
            configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            return QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
        }

        private sealed class MinionNeighbourhood
        {
            public MinionNeighbourhood(QuickGameMinionSetupState minion, HexCoordinate beside)
            {
                Minion = minion;
                Beside = beside;
            }

            public QuickGameMinionSetupState Minion { get; }
            public HexCoordinate Beside { get; }
        }

        /// <summary>
        /// A non-Heavy minion inside the Battle Zone and an empty playable space beside it, on the
        /// requested side of the zone boundary. The space also has an empty neighbour of its own, so
        /// a token has somewhere to be placed that is not the minion's space.
        /// </summary>
        private static MinionNeighbourhood BesideANonHeavyMinion(
            BoardMapDefinition map, QuickGameSetupState setup, bool outsideBattleZone)
        {
            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(value => value.Coordinate));
            var found = setup.Minions
                .Where(value =>
                    value.MinionType != StandardMinionType.Heavy &&
                    BattleZoneQuery.IsInStartingBattleZone(map, value.Coordinate))
                .SelectMany(value => map.Spaces
                    .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                    .Where(space =>
                        HexDistanceQuery.Calculate(space.Coordinate, value.Coordinate) == 1 &&
                        !occupied.Contains(space.Coordinate) &&
                        BattleZoneQuery.IsInStartingBattleZone(map, space.Coordinate) !=
                            outsideBattleZone &&
                        map.Spaces.Any(neighbour =>
                            neighbour.Kind == BoardMapSpaceKind.Playable &&
                            !occupied.Contains(neighbour.Coordinate) &&
                            !neighbour.Coordinate.Equals(space.Coordinate) &&
                            HexDistanceQuery.Calculate(neighbour.Coordinate, space.Coordinate) == 1))
                    .Select(space => new MinionNeighbourhood(value, space.Coordinate)))
                .First();
            return found;
        }

        private static MatchSession CreateSession(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            PlaceholderOneVsOneTestConfiguration configuration,
            HexCoordinate blueAt,
            bool withSkillCards = false)
        {
            var built = withSkillCards
                ? PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue)
                : configuration;
            var occupancies = setup.Heroes
                .Select(hero => new HeroOccupancy(
                    hero.HeroId, hero.TeamSide == TeamSide.Blue ? blueAt : Elsewhere))
                .ToArray();
            var scenario = new InitialScenarioDescription(
                built.Participants,
                built.CardDefinitions,
                built.CardInstances,
                built.TieBreakerState,
                map.Reference,
                occupancies,
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// Commits one card per side by instance-id fragment. Blue Sentinel sits at Initiative 11 and
        /// Red Pursuit at 7, the highest Red card below it that no other case here needs, so the Blue
        /// hero acts first without any tie having to be steered.
        /// </summary>
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

        private static void StartTheSkill(MatchSession session)
        {
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(SentinelCardId),
                "The hero the board was built around must be the one whose turn this is.");
            var offer = session.Projection.AvailableTurnActions
                .Single(candidate => candidate.ActionDefinitionId.Equals(SentinelSkill));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
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

        private static CardActionResolutionEvent[] SkillFacts(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value => value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.SkillActionProgressed)
                .Select(value => value.TurnEvent.CardActionEvent)
                .ToArray();

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) => Restore(session).Board;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }
    }
}
