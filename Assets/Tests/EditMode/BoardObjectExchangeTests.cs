using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.6's three board-object operations and the capabilities that declare them: rulebook
    /// p.19's Swap, its Replace, and its Remove, plus p.8's last-minion Lane-Push cause.
    ///
    /// The synthetic line board carries the mechanics, because a controlled row of spaces makes an
    /// exchange and a substitution readable. Everything that depends on the Battle Zone runs on the
    /// official map instead, because a Battle Zone is map content and a synthetic coordinate board
    /// declares none.
    /// </summary>
    public sealed class BoardObjectExchangeTests
    {
        private static readonly MapId Map = new MapId("synthetic-exchange-map");

        private static readonly HeroId RedActor =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly HeroId BlueActor =
            new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate NextTo = new HexCoordinate(1, 0);
        private static readonly HexCoordinate Beyond = new HexCoordinate(2, 0);

        // ---------------------------------------------------------------------------------------
        // Swap
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Swap_ExchangesBothObjectsAgainstOnePreEffectStateInOneTransaction()
        {
            var board = CreateLineBoard();
            var minion = PlaceMinion(board, NextTo);

            var applied = Handler(SyntheticExchangeCapabilities.SwapV2)
                .Execute(Context(board, RedActor, SwapInputs(ActorAt, NextTo)));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(HeroAt(board, RedActor), Is.EqualTo(NextTo));
            Assert.That(MinionAt(board, minion), Is.EqualTo(ActorAt),
                "Both objects move at once. Read one after relocating the other and the second " +
                "resolves to the space the first just left, which is a Swap written as two moves.");
            Assert.That(board.Minions.Count, Is.EqualTo(1), "Nothing left the board.");
        }

        [Test]
        public void Swap_PublishesTheSpacesTheObjectsEndedIn_AndNoMovementFact()
        {
            var board = CreateLineBoard();
            PlaceMinion(board, NextTo);

            var applied = Handler(SyntheticExchangeCapabilities.SwapV2)
                .Execute(Context(board, RedActor, SwapInputs(ActorAt, NextTo)));

            Assert.That(
                applied.Outcomes.Select(outcome => outcome.OutputName),
                Is.EquivalentTo(new[] { "firstAt", "secondAt" }),
                "p.19 says a Swap does not count as movement or placement, so no route, distance, or " +
                "placement fact is published.");
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "firstAt").Value.Coordinate,
                Is.EqualTo(NextTo));
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "secondAt").Value.Coordinate,
                Is.EqualTo(ActorAt));
        }

        [Test]
        public void Swap_RefusesAnObjectPairedWithItself()
        {
            var board = CreateLineBoard();

            var refused = Handler(SyntheticExchangeCapabilities.SwapV2)
                .Execute(Context(board, RedActor, SwapInputs(ActorAt, ActorAt)));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "p.19 requires two objects; an object exchanging places with itself is not a Swap.");
            Assert.That(HeroAt(board, RedActor), Is.EqualTo(ActorAt));
            Assert.That(
                board.CommitValidatedSwap(
                        BoardObjectReference.ForHero(RedActor),
                        BoardObjectReference.ForHero(RedActor))
                    .RejectionReason,
                Is.EqualTo(BoardObjectSwapRejectionReason.SameObject),
                "The port states the same refusal, so no caller can reach past the operation to get it.");
        }

        [Test]
        public void Swap_RefusesAnEmptySpaceAndAnObjectThatHasLeftTheBoard()
        {
            var board = CreateLineBoard();

            var refused = Handler(SyntheticExchangeCapabilities.SwapV2)
                .Execute(Context(board, RedActor, SwapInputs(ActorAt, NextTo)));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "An empty space names no object to exchange with.");
            Assert.That(
                board.CommitValidatedSwap(
                        BoardObjectReference.ForHero(RedActor),
                        BoardObjectReference.ForMinion(new MinionId("no-such-minion")))
                    .RejectionReason,
                Is.EqualTo(BoardObjectSwapRejectionReason.UnknownObject));
        }

        [Test]
        public void Swap_RefusesWhileEitherObjectIsImmuneToTheActivityPerformingIt()
        {
            // p.13: a Heavy minion is immune while friendly non-Heavy minions stand in the Battle Zone,
            // and p.18 makes immunity a targeting rule. A Swap affects both objects, so both are asked.
            var board = CreateOfficialBoard();
            var heavy = ProtectedHeavyMinion(board);
            var actor = HeroBesideCoordinate(board, MinionAt(board, heavy));

            var refused = Handler(SyntheticExchangeCapabilities.SwapV2).Execute(
                Context(board, actor, SwapInputs(HeroAt(board, actor), MinionAt(board, heavy))));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(MinionAt(board, heavy), Is.EqualTo(MinionAt(board, heavy)));
            Assert.That(board.Minions.Any(minion => minion.MinionId.Equals(heavy)), Is.True,
                "A refused Swap moves nothing at all.");
        }

        [Test]
        public void Swap_AdmitsOnlyTheKindOfPieceItsNodeDeclares()
        {
            // Milestone 6.6a. p.19 exchanges two objects and Blue Sentinel's card text says unit, so
            // the narrowing is the node's declaration rather than a rule this operation invented. The
            // same board answers both ways, which is what makes it the declaration doing the work.
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);

            var asObject = Handler(SyntheticExchangeCapabilities.SwapV2).Anticipate(
                Context(board, RedActor, SwapInputs(ActorAt, NextTo, SwapEligiblePieceKind.GamePiece)));
            var asUnit = Handler(SyntheticExchangeCapabilities.SwapV2).Execute(
                Context(board, RedActor, SwapInputs(ActorAt, NextTo, SwapEligiblePieceKind.Unit)));

            Assert.That(asObject, Is.EqualTo(EffectExecutionResultKind.Applied),
                "p.19's own breadth exchanges a hero with a token.");
            Assert.That(asUnit.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "p.19 states that tokens are not units, so a Swap declaring unit names no token.");
            Assert.That(HeroAt(board, RedActor), Is.EqualTo(ActorAt), "And it exchanged nothing.");
            Assert.That(board.PlacedTokens.Single().Coordinate, Is.EqualTo(NextTo));
        }

        [Test]
        public void Swap_RefusesADeclarationItCannotRead()
        {
            var board = CreateLineBoard();
            PlaceMinion(board, NextTo);

            var refused = Handler(SyntheticExchangeCapabilities.SwapV2).Execute(
                Context(board, RedActor, SwapInputs(ActorAt, NextTo, (SwapEligiblePieceKind)99)));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A definition that says nothing legible about what it may exchange exchanges nothing, " +
                "rather than quietly falling back to the widest reading.");
            Assert.That(HeroAt(board, RedActor), Is.EqualTo(ActorAt));
            Assert.Throws<ArgumentException>(() => SwapOperation.Swap(
                SwapEligiblePieceKind.None,
                board,
                board,
                BoardObjectReference.ForHero(RedActor),
                BoardObjectReference.ForHero(BlueActor)),
                "The operation refuses to run without the declaration at all.");
        }

        [Test]
        public void Swap_ProbesWithoutExchangingAnything()
        {
            var board = CreateLineBoard();
            PlaceMinion(board, NextTo);

            var verdict = Handler(SyntheticExchangeCapabilities.SwapV2)
                .Anticipate(Context(board, RedActor, SwapInputs(ActorAt, NextTo)));

            Assert.That(verdict, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(HeroAt(board, RedActor), Is.EqualTo(ActorAt),
                "Asking whether a Swap can be performed must never perform it.");
        }

        // ---------------------------------------------------------------------------------------
        // Replace
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Replace_SubstitutesADifferentObjectInTheSameSpaceAsOneTransaction()
        {
            var board = CreateLineBoard();
            var hazard = PlaceHazard(board, NextTo);

            var applied = Handler(SyntheticExchangeCapabilities.ReplaceTokenV1)
                .Execute(Context(board, RedActor, SubjectInput(NextTo)));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1),
                "One object stands where one stood; the space is never empty, so this is neither a " +
                "Remove nor a Place and nothing could have taken the space in between.");
            var standing = board.PlacedTokens.Single();
            Assert.That(standing.Coordinate, Is.EqualTo(NextTo));
            Assert.That(
                standing.Definition.Reference, Is.EqualTo(SyntheticComponentDefinitions.WardV1),
                "p.19 requires a different object.");
            Assert.That(board.PlacedTokens.Any(token => token.Id.Equals(hazard)), Is.False);
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "replaced").Value.Coordinate,
                Is.EqualTo(NextTo),
                "One space, not an origin and a destination: a Replace relocates nothing.");
        }

        [Test]
        public void Replace_ReturnsTheOutgoingObjectToItsSupplyAndTakesTheIncomingFromItsOwn()
        {
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);
            var catalog = SyntheticComponentDefinitions.CreateCatalog();
            var hazardsBefore = catalog.CountAvailableTokens(
                SyntheticComponentDefinitions.HazardV3, board.PlacedTokens);

            Handler(SyntheticExchangeCapabilities.ReplaceTokenV1)
                .Execute(Context(board, RedActor, SubjectInput(NextTo)));

            Assert.That(
                catalog.CountAvailableTokens(SyntheticComponentDefinitions.HazardV3, board.PlacedTokens),
                Is.EqualTo(hazardsBefore + 1),
                "The replaced object goes back to the supply it came from and can be placed again.");
            Assert.That(
                catalog.CountAvailableTokens(SyntheticComponentDefinitions.WardV1, board.PlacedTokens),
                Is.EqualTo(0),
                "The substitute is drawn from its own finite supply (p.19).");
        }

        [Test]
        public void Replace_TransfersNoProperty_SoTheOutgoingObjectsContinuousSourceEnds()
        {
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);
            var participants = PlaceholderOneVsOneParticipants.Create();
            var before = RuleModificationQuery.Build(
                null, board, participants, TieBreakerState.Unrevealed);
            Assert.That(before.Modifications.Count, Is.EqualTo(1),
                "The Hazard supplies its declared property while it remains placed (p.19).");

            Handler(SyntheticExchangeCapabilities.ReplaceTokenV1)
                .Execute(Context(board, RedActor, SubjectInput(NextTo)));

            var after = RuleModificationQuery.Build(
                null, board, participants, TieBreakerState.Unrevealed);
            Assert.That(after.Modifications, Is.Empty,
                "p.19 ties a token's properties to the instance, so a no-transfer substitution ends " +
                "them rather than handing them to an object whose definition never declared them.");
        }

        [Test]
        public void Replace_RefusesASpaceHoldingNothingItCanReplace()
        {
            var board = CreateLineBoard();

            var refused = Handler(SyntheticExchangeCapabilities.ReplaceTokenV1)
                .Execute(Context(board, RedActor, SubjectInput(NextTo)));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(board.PlacedTokens, Is.Empty);
        }

        [Test]
        public void Replace_RefusesWhenTheIncomingSupplyIsExhausted()
        {
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);
            PlaceHazard(board, Beyond);
            Handler(SyntheticExchangeCapabilities.ReplaceTokenV1)
                .Execute(Context(board, RedActor, SubjectInput(NextTo)));

            var refused = Handler(SyntheticExchangeCapabilities.ReplaceTokenV1)
                .Execute(Context(board, RedActor, SubjectInput(Beyond), node: "exchange-second"));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The one Ward is already standing, so there is no different object left to bring in.");
            Assert.That(
                board.PlacedTokens.Single(token => token.Coordinate.Equals(Beyond))
                    .Definition.Reference,
                Is.EqualTo(SyntheticComponentDefinitions.HazardV3));
        }

        [Test]
        public void Replace_IsALegalNoOpWhenItsOwnSubstituteIsAlreadyStanding()
        {
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);
            var context = Context(board, RedActor, SubjectInput(NextTo));
            Handler(SyntheticExchangeCapabilities.ReplaceTokenV1).Execute(context);

            var again = Handler(SyntheticExchangeCapabilities.ReplaceTokenV1).Execute(context);

            Assert.That(again.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "Re-executing a node inside its own resolution is the no-op milestone 6.8d " +
                "established, not a second Replace that finds an empty supply.");
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1));
        }

        [Test]
        public void Replace_RefusesAnObservationPolicyItCannotHonour()
        {
            var board = CreateLineBoard();
            var hazard = PlaceHazard(board, NextTo);
            var twoStage = new ReplacePolicy(
                ReplaceIncomingSource.Supply,
                ReplaceOutgoingDestination.Supply,
                ReplacePropertyTransferPolicy.NoTransfer,
                ReplaceObservationPolicy.RemovalAndPlacement);

            var refused = ReplaceOperation.ReplaceToken(
                twoStage,
                board,
                board,
                SyntheticComponentDefinitions.CreateCatalog(),
                hazard,
                Ward(NextTo));

            Assert.That(refused.RejectionReason,
                Is.EqualTo(ReplaceRejectionReason.UnsupportedObservation),
                "A declaration promising two observable stages is refused rather than quietly " +
                "satisfied by the single-stage commit, which is what would make the declaration false.");
            Assert.That(board.PlacedTokens.Single().Id, Is.EqualTo(hazard));
        }

        [Test]
        public void Replace_RefusesASubstituteOfTheSameDefinitionAndOneNamingAnotherSpace()
        {
            var board = CreateLineBoard();
            var hazard = PlaceHazard(board, NextTo);
            var policy = new ReplacePolicy(
                ReplaceIncomingSource.Supply,
                ReplaceOutgoingDestination.Supply,
                ReplacePropertyTransferPolicy.NoTransfer,
                ReplaceObservationPolicy.ReplacementOnly);

            Assert.That(
                ReplaceOperation.ReplaceToken(
                    policy, board, board, SyntheticComponentDefinitions.CreateCatalog(), hazard,
                    Hazard(NextTo, "same-definition")).RejectionReason,
                Is.EqualTo(ReplaceRejectionReason.IncomingNotDifferent),
                "p.19 requires a different object.");
            Assert.That(
                ReplaceOperation.ReplaceToken(
                    policy, board, board, SyntheticComponentDefinitions.CreateCatalog(), hazard,
                    Ward(Beyond)).RejectionReason,
                Is.EqualTo(ReplaceRejectionReason.ReplacementUnavailable),
                "A substitute standing somewhere else would be a Remove and a Place.");
        }

        [Test]
        public void ReplacePolicy_RefusesToExistWithoutAnyOfItsFourDeclarations()
        {
            Assert.Throws<ArgumentException>(() => new ReplacePolicy(
                ReplaceIncomingSource.None, ReplaceOutgoingDestination.Supply,
                ReplacePropertyTransferPolicy.NoTransfer, ReplaceObservationPolicy.ReplacementOnly));
            Assert.Throws<ArgumentException>(() => new ReplacePolicy(
                ReplaceIncomingSource.Supply, ReplaceOutgoingDestination.None,
                ReplacePropertyTransferPolicy.NoTransfer, ReplaceObservationPolicy.ReplacementOnly));
            Assert.Throws<ArgumentException>(() => new ReplacePolicy(
                ReplaceIncomingSource.Supply, ReplaceOutgoingDestination.Supply,
                ReplacePropertyTransferPolicy.None, ReplaceObservationPolicy.ReplacementOnly));
            Assert.Throws<ArgumentException>(() => new ReplacePolicy(
                ReplaceIncomingSource.Supply, ReplaceOutgoingDestination.Supply,
                ReplacePropertyTransferPolicy.NoTransfer, ReplaceObservationPolicy.None));
        }

        // ---------------------------------------------------------------------------------------
        // Remove
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Remove_TakesATokenOffTheBoardAndRecordsItsDeclaredDestination()
        {
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);
            var catalog = SyntheticComponentDefinitions.CreateCatalog();

            var applied = Handler(SyntheticExchangeCapabilities.RemoveV2)
                .Execute(Context(board, RedActor, RemoveInputs(NextTo)));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens, Is.Empty);
            Assert.That(board.IsEmptyPlayableSpace(NextTo), Is.True);
            Assert.That(
                catalog.CountAvailableTokens(SyntheticComponentDefinitions.HazardV3, board.PlacedTokens),
                Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount),
                "The declared destination is the supply, and the piece is genuinely back in it.");
        }

        [Test]
        public void Remove_ReturnsAMinionMiniatureToTheReserveWithoutAwardingAnything()
        {
            var board = CreateLineBoard();
            var minion = PlaceMinion(board, NextTo);

            var applied = Handler(SyntheticExchangeCapabilities.RemoveV2)
                .Execute(Context(board, RedActor, RemoveInputs(NextTo)));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.Minions, Is.Empty);
            Assert.That(
                board.FindUnitSupply().Select(reserved => reserved.MinionId), Has.Member(minion),
                "p.19's Respawn ordering depends on a removed miniature being back in the supply.");
            Assert.That(
                applied.Outcomes.Select(outcome => outcome.OutputName),
                Is.EquivalentTo(new[] { "removed", "lanePush" }),
                "p.19 says outright not to collect any coins, so no reward or Defeat fact exists here.");
        }

        [Test]
        public void Remove_RefusesAHeroBecauseAHeroLeavesTheBoardThroughDefeat()
        {
            var board = CreateLineBoard();

            var refused = Handler(SyntheticExchangeCapabilities.RemoveV2)
                .Execute(Context(board, RedActor, RemoveInputs(ActorAt)));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(
                board.CommitValidatedRemoval(BoardObjectReference.ForHero(RedActor)).RejectionReason,
                Is.EqualTo(BoardObjectRemovalRejectionReason.HeroRequiresDefeat),
                "The port refuses one too, so no caller can reach past the declared eligibility to " +
                "take a hero off the board without p.13's consequences.");
            Assert.That(board.Occupancies.Count, Is.EqualTo(2));
        }

        [Test]
        public void Remove_ReportsRemovalPreventedAndChangesNothingWhenTheTargetBecameImmune()
        {
            // The subject was a legal candidate when it was chosen and is immune by the time the
            // removal stage runs, which is why cross-cutting rule 6 asks immunity in both places.
            var board = CreateOfficialBoard();
            var heavy = ProtectedHeavyMinion(board);
            var at = MinionAt(board, heavy);
            var actor = HeroBesideCoordinate(board, at);

            var outcome = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.GamePiece, RemoveDestination.Supply),
                board,
                board,
                BoardObjectReference.ForMinion(heavy),
                new RemovalStageDecision(
                    RuleActivity.Skill, RuleObject.ForHero(actor), board, board,
                    PlaceholderOneVsOneParticipants.Create(), RuleModificationSet.Empty,
                    LanePushCauseKind.Removal));

            Assert.That(outcome.Accepted, Is.True,
                "The operation did everything the rules asked; the rules answered that the piece stays.");
            Assert.That(outcome.ResultKind, Is.EqualTo(RemoveResultKind.RemovalPrevented));
            Assert.That(outcome.Origin, Is.EqualTo(at));
            Assert.That(board.Minions.Any(minion => minion.MinionId.Equals(heavy)), Is.True);
            Assert.That(outcome.LanePush.Qualifies, Is.False,
                "p.8 qualifies removing the last minion; a prevented removal removed nothing.");
        }

        [Test]
        public void Remove_ProbesWithoutTakingAnythingOffTheBoard()
        {
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);

            var verdict = Handler(SyntheticExchangeCapabilities.RemoveV2)
                .Anticipate(Context(board, RedActor, RemoveInputs(NextTo)));

            Assert.That(verdict, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1),
                "Executing to find out whether a Remove can do anything would take the piece off.");
        }

        [Test]
        public void RemovePolicy_RefusesToExistWithoutItsPieceKindOrDestination()
        {
            Assert.Throws<ArgumentException>(() =>
                new RemovePolicy(RemoveEligiblePieceKind.None, RemoveDestination.Supply));
            Assert.Throws<ArgumentException>(() =>
                new RemovePolicy(RemoveEligiblePieceKind.Token, RemoveDestination.None));
        }

        [Test]
        public void Remove_ReadsItsDeclaredKindTheSameWayWhenItProbesAsWhenItRemoves()
        {
            // Milestone 6.6a's closing check for the reading that had two homes. The handler resolved
            // its subject by refusing a hero, which agrees with the operation only while the declared
            // kind is p.19's widest; a Remove declaring token would have reported an adjacent minion
            // removable and then refused it, and milestone 6.8l reads that probe as the offer a player
            // is shown.
            var board = CreateLineBoard();
            PlaceMinion(board, NextTo);
            var inputs = RemoveInputs(NextTo, RemoveEligiblePieceKind.Token);

            var probed = Handler(SyntheticExchangeCapabilities.RemoveV2)
                .Anticipate(Context(board, RedActor, inputs));
            var executed = Handler(SyntheticExchangeCapabilities.RemoveV2)
                .Execute(Context(board, RedActor, inputs));

            Assert.That(probed, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(executed.Kind, Is.EqualTo(probed),
                "One declaration, one reading: what the probe answers is what the removal does.");
            Assert.That(board.Minions.Count, Is.EqualTo(1), "And the miniature is still standing.");

            var token = RemoveInputs(NextTo, RemoveEligiblePieceKind.GamePiece);
            Assert.That(
                Handler(SyntheticExchangeCapabilities.RemoveV2).Anticipate(Context(board, RedActor, token)),
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The same board and the same space, under the declaration that admits a miniature.");
        }

        [Test]
        public void Remove_RefusesADeclarationItCannotRead()
        {
            var board = CreateLineBoard();
            PlaceHazard(board, NextTo);

            var refused = Handler(SyntheticExchangeCapabilities.RemoveV2).Execute(
                Context(board, RedActor, RemoveInputs(NextTo, (RemoveEligiblePieceKind)99)));

            Assert.That(refused.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(1),
                "An unreadable declaration takes nothing off the board rather than taking anything.");
        }

        [Test]
        public void RemovePolicy_AdmitsOnlyTheKindItDeclares()
        {
            var board = CreateLineBoard();
            PlaceMinion(board, NextTo);

            var refused = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.Token, RemoveDestination.Supply),
                board,
                board,
                board.TryGetObjectAt(NextTo, out var subject) ? subject : default,
                NeverPrevented(board));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason, Is.EqualTo(RemoveRejectionReason.IneligiblePiece));
            Assert.That(board.Minions.Count, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------------------------------
        // p.8's last-minion Lane-Push cause
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Remove_RecordsAQualifyingCauseWhenItTakesTheLastMinionOfASideOutOfTheBattleZone()
        {
            var board = CreateOfficialBoard();
            var last = ClearBattleZoneExceptOne(board, TeamSide.Red);

            var outcome = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.GamePiece, RemoveDestination.Supply),
                board,
                board,
                BoardObjectReference.ForMinion(last),
                NeverPrevented(board));

            Assert.That(outcome.ResultKind, Is.EqualTo(RemoveResultKind.Removed));
            Assert.That(outcome.LanePush.Qualifies, Is.True);
            Assert.That(outcome.LanePush.Cause, Is.EqualTo(LanePushCauseKind.Removal));
            Assert.That(outcome.LanePush.LosingSide, Is.EqualTo(TeamSide.Red));
            Assert.That(
                outcome.LanePush.BattleZoneId,
                Is.EqualTo(BattleZoneQuery.GetStartingBattleZone(board.MapDefinition).Id));
        }

        [Test]
        public void Remove_RecordsNoCauseWhileThatSideStillHasAMinionInTheBattleZone()
        {
            var board = CreateOfficialBoard();
            // Non-Heavy, so p.13's accompaniment is not what this case is about: a Heavy standing
            // beside friendly minions is immune, and the removal would be prevented before p.8's
            // question was ever reached.
            var inside = BattleZoneQuery.FindMinionsInside(board)
                .Where(minion =>
                    minion.TeamSide == TeamSide.Red && minion.MinionType != StandardMinionType.Heavy)
                .ToArray();
            Assert.That(inside.Length, Is.GreaterThan(1), "The fixture needs a side with several.");

            var outcome = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.GamePiece, RemoveDestination.Supply),
                board,
                board,
                BoardObjectReference.ForMinion(inside[0].MinionId),
                NeverPrevented(board));

            Assert.That(outcome.ResultKind, Is.EqualTo(RemoveResultKind.Removed));
            Assert.That(outcome.LanePush.Qualifies, Is.False);
            Assert.That(outcome.LanePush, Is.EqualTo(LanePushQualification.None));
        }

        [Test]
        public void Remove_RecordsNoCauseForAPieceTakenFromOutsideTheBattleZone()
        {
            var board = CreateOfficialBoard();
            var last = ClearBattleZoneExceptOne(board, TeamSide.Red);
            var outside = BattleZoneQuery.FindStartingBattleZoneCoordinates(board.MapDefinition);
            var elsewhere = board.MapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate)
                .First(coordinate =>
                    !outside.Contains(coordinate) && board.IsEmptyPlayableSpace(coordinate));
            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForMinion(last), elsewhere).Accepted, Is.True);

            var outcome = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.GamePiece, RemoveDestination.Supply),
                board,
                board,
                BoardObjectReference.ForMinion(last),
                NeverPrevented(board));

            Assert.That(outcome.ResultKind, Is.EqualTo(RemoveResultKind.Removed));
            Assert.That(outcome.LanePush.Qualifies, Is.False,
                "p.8 asks about the last remaining minion in the Battle Zone; taking a piece from " +
                "outside it leaves the zone exactly as empty as it already was.");
        }

        [Test]
        public void Remove_RecordsNoCauseForAToken_BecauseATokenIsNotAUnit()
        {
            var board = CreateOfficialBoard();
            ClearBattleZoneExceptOne(board, TeamSide.Red);
            var empty = BattleZoneQuery.FindStartingBattleZoneCoordinates(board.MapDefinition)
                .First(board.IsEmptyPlayableSpace);
            PlaceHazard(board, empty);

            var outcome = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.GamePiece, RemoveDestination.Supply),
                board,
                board,
                board.TryGetObjectAt(empty, out var token) ? token : default,
                NeverPrevented(board));

            Assert.That(outcome.ResultKind, Is.EqualTo(RemoveResultKind.Removed));
            Assert.That(outcome.LanePush.Qualifies, Is.False,
                "p.19 leaves tokens outside the unit rules, so removing one empties nothing p.8 counts.");
        }

        [Test]
        public void TheLastMinionCount_IsTheEffectiveOneTheMinionBattleCounts()
        {
            // p.8 pushes the lane "in the same way as it would during a Minion Battle", so the two
            // rules count one set. p.17 replaces a counting object's rules and qualities, so a hero
            // counting as a minion is one of the minions standing in the zone.
            var board = CreateOfficialBoard();
            var last = ClearBattleZoneExceptOne(board, TeamSide.Red);
            var redHero = board.Occupancies
                .Select(occupancy => occupancy.HeroId)
                .First(hero => hero.Equals(RedActor));
            Assert.That(
                board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(redHero),
                        BattleZoneQuery.FindStartingBattleZoneCoordinates(board.MapDefinition)
                            .First(board.IsEmptyPlayableSpace))
                    .Accepted,
                Is.True);

            var outcome = RemoveOperation.Remove(
                new RemovePolicy(RemoveEligiblePieceKind.GamePiece, RemoveDestination.Supply),
                board,
                board,
                BoardObjectReference.ForMinion(last),
                new RemovalStageDecision(
                    RuleActivity.Skill,
                    RuleObject.ForHero(BlueActor),
                    board,
                    board,
                    PlaceholderOneVsOneParticipants.Create(),
                    CountsAsMinion(redHero),
                    LanePushCauseKind.Removal));

            Assert.That(outcome.ResultKind, Is.EqualTo(RemoveResultKind.Removed));
            Assert.That(outcome.LanePush.Qualifies, Is.False,
                "A Red hero counting as a minion still stands in the zone, so Red has not lost its " +
                "last minion - the same answer the Minion Battle gives about the same board.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticEffectCapabilities.CreateCatalog().Load(reference);

        /// <summary>
        /// A Swap node's inputs. The eligible kind defaults to p.19's own unrestricted breadth, so a
        /// case that is not about the narrowing reads exactly as it did before milestone 6.6a made the
        /// declaration explicit.
        /// </summary>
        private static Dictionary<string, EffectValue> SwapInputs(
            HexCoordinate first,
            HexCoordinate second,
            SwapEligiblePieceKind eligible = SwapEligiblePieceKind.GamePiece) =>
            new Dictionary<string, EffectValue>
            {
                ["first"] = EffectValue.Space(first),
                ["second"] = EffectValue.Space(second),
                [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                    EffectValue.Integer((int)eligible)
            };

        private static Dictionary<string, EffectValue> SubjectInput(HexCoordinate subject) =>
            new Dictionary<string, EffectValue> { ["subject"] = EffectValue.Space(subject) };

        /// <summary>A Remove node's inputs, on the same terms as <see cref="SwapInputs"/>.</summary>
        private static Dictionary<string, EffectValue> RemoveInputs(
            HexCoordinate subject,
            RemoveEligiblePieceKind eligible = RemoveEligiblePieceKind.GamePiece) =>
            new Dictionary<string, EffectValue>
            {
                ["subject"] = EffectValue.Space(subject),
                [SyntheticExchangeCapabilities.EligiblePieceParameterName] =
                    EffectValue.Integer((int)eligible)
            };

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            HeroId actor,
            IReadOnlyDictionary<string, EffectValue> inputs,
            string node = "exchange-node") =>
            new EffectCapabilityContext(
                new EffectNodeId(node),
                EffectFailurePolicy.Mandatory,
                actor,
                actor,
                new CardInstanceId("exchange-card"),
                new ActionDefinitionId("exchange-action"),
                inputs,
                1,
                1,
                PlaceholderOneVsOneParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board, new MarkerAttachmentRegister(),
                    board, board, board));

        /// <summary>A removal stage nothing intercepts, for the cases that are about p.8 rather than p.18.</summary>
        private static RemovalStageDecision NeverPrevented(AdjacentMoveAuthority board) =>
            new RemovalStageDecision(
                RuleActivity.Skill,
                RuleObject.ForHero(BlueActor),
                board,
                board,
                PlaceholderOneVsOneParticipants.Create(),
                RuleModificationSet.Empty,
                LanePushCauseKind.Removal);

        /// <summary>A live `Counts As` minion declaration for one hero, as an Active-effect source would supply it.</summary>
        private static RuleModificationSet CountsAsMinion(HeroId heroId) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromActiveEffect(
                        RuleModification.ForCountsAs(EffectiveUnitKind.Minion),
                        RuleObject.ForHero(heroId),
                        new GuardsDigital.Core.Timing.ActiveEffectId("exchange-counts-as"),
                        heroId,
                        TeamSide.Red,
                        1)
                },
                TieBreakerState.Unrevealed);

        private static HexCoordinate HeroAt(AdjacentMoveAuthority board, HeroId heroId) =>
            board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroId)).Coordinate;

        private static HexCoordinate MinionAt(AdjacentMoveAuthority board, MinionId minionId) =>
            board.Minions.Single(minion => minion.MinionId.Equals(minionId)).Coordinate;

        private static MinionId PlaceMinion(AdjacentMoveAuthority board, HexCoordinate at)
        {
            var minion = board.Minions.Single().MinionId;
            Assert.That(board.CommitValidatedDisplacement(
                BoardObjectReference.ForMinion(minion), at).Accepted, Is.True);
            return minion;
        }

        private static TokenInstanceId PlaceHazard(AdjacentMoveAuthority board, HexCoordinate at)
        {
            var id = new TokenInstanceId($"exchange-hazard-{at.Q}-{at.R}");
            board.CommitValidatedTokenPlacement(new PlacedToken(
                id,
                SyntheticComponentDefinitions.CreateCatalog()
                    .LoadToken(SyntheticComponentDefinitions.HazardV3),
                RedActor,
                RedActor,
                new CardInstanceId("exchange-token-source"),
                at));
            return id;
        }

        private static PlacedToken Ward(HexCoordinate at) =>
            new PlacedToken(
                new TokenInstanceId($"exchange-ward-{at.Q}-{at.R}"),
                SyntheticComponentDefinitions.CreateCatalog()
                    .LoadToken(SyntheticComponentDefinitions.WardV1),
                RedActor,
                RedActor,
                new CardInstanceId("exchange-token-source"),
                at);

        private static PlacedToken Hazard(HexCoordinate at, string id) =>
            new PlacedToken(
                new TokenInstanceId(id),
                SyntheticComponentDefinitions.CreateCatalog()
                    .LoadToken(SyntheticComponentDefinitions.HazardV3),
                RedActor,
                RedActor,
                new CardInstanceId("exchange-token-source"),
                at);

        /// <summary>
        /// A Heavy minion the p.13 accompaniment protects, asserted rather than assumed so the immunity
        /// cases are about immunity and not about a fixture that happened to have none.
        /// </summary>
        private static MinionId ProtectedHeavyMinion(AdjacentMoveAuthority board)
        {
            var inside = BattleZoneQuery.FindMinionsInside(board);
            var heavy = inside.First(minion => minion.MinionType == StandardMinionType.Heavy);
            Assert.That(
                inside.Any(minion =>
                    minion.TeamSide == heavy.TeamSide &&
                    minion.MinionType != StandardMinionType.Heavy),
                Is.True,
                "p.13 protects the Heavy only while friendly non-Heavy minions stand in the zone.");
            return heavy.MinionId;
        }

        /// <summary>
        /// A hero standing beside the named space, moved there so the immunity cases run at the range
        /// a real Swap or Remove would.
        /// </summary>
        private static HeroId HeroBesideCoordinate(AdjacentMoveAuthority board, HexCoordinate at)
        {
            var beside = board.MapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate)
                .First(coordinate =>
                    HexDistanceQuery.Calculate(coordinate, at) == 1 &&
                    board.IsEmptyPlayableSpace(coordinate));
            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(BlueActor), beside).Accepted, Is.True);
            return BlueActor;
        }

        /// <summary>
        /// Removes every Battle-Zone minion of one side except one, through the port, and answers which
        /// one is left. Fixture setup rather than the behaviour under test.
        /// </summary>
        private static MinionId ClearBattleZoneExceptOne(AdjacentMoveAuthority board, TeamSide side)
        {
            var inside = BattleZoneQuery.FindMinionsInside(board)
                .Where(minion => minion.TeamSide == side)
                .ToArray();
            // The survivor is non-Heavy on purpose. p.13 makes a Heavy immune while friendly non-Heavy
            // minions stand in the zone, and a counts-as friendly minion restores that immunity, so a
            // Heavy survivor would turn every one of these cases into an immunity case.
            var survivor = inside.First(minion => minion.MinionType != StandardMinionType.Heavy);
            foreach (var minion in inside.Where(candidate => !candidate.MinionId.Equals(survivor.MinionId)))
            {
                Assert.That(
                    board.CommitValidatedRemoval(
                        BoardObjectReference.ForMinion(minion.MinionId)).Accepted, Is.True);
            }

            return survivor.MinionId;
        }

        /// <summary>A playable East-West file with the Red hero at (0,0) and one minion off to the side.</summary>
        private static AdjacentMoveAuthority CreateLineBoard()
        {
            var map = BoardMapDefinition.CreateSynthetic(
                Map, 1, Enumerable.Range(0, 6).Select(q => new HexCoordinate(q, 0)).ToArray());
            return new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(RedActor, ActorAt),
                    new HeroOccupancy(BlueActor, new HexCoordinate(5, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                new[]
                {
                    new QuickGameMinionSetupState(
                        new MinionId("exchange-minion-01"),
                        new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue),
                        TeamSide.Red,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(Map, new SpawnPointId("spawn-exchange")),
                        new ConfiguredSpaceReference(Map, new SpaceId("q4_r0")),
                        new HexCoordinate(4, 0))
                });
        }

        /// <summary>The official map with the configured setup, which is the only board with a Battle Zone.</summary>
        private static AdjacentMoveAuthority CreateOfficialBoard()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            return new AdjacentMoveAuthority(
                map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)).ToArray(),
                Array.Empty<ObstacleOccupancy>(),
                setup.Minions);
        }
    }
}
