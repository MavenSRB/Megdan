using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class NebKherIllusionFoundationTests
    {
        [Test]
        public void IllusionsCountAsMinionsOnlyDuringControllerActions_NotAtRoundBoundary()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 6).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(2, 0))
                }, Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
            var illusion = new PlacedToken(
                new TokenInstanceId("scoped-illusion"), NebKherComponentContent.Illusion,
                enemy, enemy, new CardInstanceId("scoped-illusion-source"),
                new HexCoordinate(1, 0));
            board.CommitValidatedTokenPlacement(illusion);
            var effect = new ActiveEffectInstance(
                new ActiveEffectId("scoped-illusion-effect"),
                NebKherComponentContent.IllusionsCountAsMinions,
                actor, TeamSide.Red, default,
                new CardInstanceId("scoped-illusion-source"),
                new ActionDefinitionId("scoped-illusion-action"),
                1, 1, 0, ActiveEffectPhase.Active, 1, 1);
            var timing = ActiveEffectLifecycle.Restore(new ActiveEffectLifecycleRestoreState(
                new[] { effect }, Array.Empty<ContinuousRuleSource>()));
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            RuleModificationSet At(RuleObject acting) => RuleModificationQuery.Build(
                timing, board, participants, TieBreakerState.Unrevealed,
                UnlockedUltimateSet.None, acting, null, new MarkerAttachmentRegister());
            var token = RuleObject.ForToken(illusion.Id);
            Assert.That(EffectiveUnitKindQuery.Is(token, EffectiveUnitKind.Minion,
                At(RuleObject.ForHero(actor))), Is.True);
            Assert.That(EffectiveUnitKindQuery.Is(token, EffectiveUnitKind.Minion,
                At(RuleObject.ForHero(enemy))), Is.False);
            Assert.That(EffectiveUnitKindQuery.Is(token, EffectiveUnitKind.Minion,
                At(RuleObject.None)), Is.False,
                "The end-of-round Minion Battle has no hero performing an action.");
            var acting = RosterPresenceQuery.FindUnitsWithin(
                new HexCoordinate(0, 0), 1, RosterUnitKind.Minion,
                SourceTeamRelation.AlliedWithSource, actor, board, participants,
                At(RuleObject.ForHero(actor)));
            Assert.That(acting, Is.EqualTo(new[]
            {
                BoardObjectReference.ForToken(illusion.Id)
            }));
            Assert.That(RosterPresenceQuery.OfferUnits(acting, board, false),
                Is.EqualTo(new[] { EffectValue.Space(illusion.Coordinate) }));
            Assert.That(RosterPresenceQuery.FindUnitsWithin(
                new HexCoordinate(0, 0), 1, RosterUnitKind.Minion,
                SourceTeamRelation.AlliedWithSource, actor, board, participants,
                At(RuleObject.None)), Is.Empty);
            Assert.That(DefenseValueQuery.CalculateMinionContributions(
                enemy, participants, board, At(RuleObject.ForHero(actor)))
                .Sum(value => value.Amount), Is.EqualTo(-1));
            Assert.That(DefenseValueQuery.CalculateMinionContributions(
                enemy, participants, board, At(RuleObject.None))
                .Sum(value => value.Amount), Is.Zero);
            var actorPiece = BoardObjectReference.ForHero(actor);
            var illusionPiece = BoardObjectReference.ForToken(illusion.Id);
            Assert.That(SwapOperation.Swap(
                SwapEligiblePieceKind.Unit, board, board, actorPiece, illusionPiece,
                At(RuleObject.None)).RejectionReason,
                Is.EqualTo(SwapRejectionReason.IneligiblePiece),
                "Outside an action the physical token is not a unit.");
            Assert.That(SwapOperation.Swap(
                SwapEligiblePieceKind.Unit, board, board, actorPiece, illusionPiece,
                At(RuleObject.ForHero(actor))).Accepted, Is.True,
                "During NebKher's action it counts as a friendly melee minion and hence a unit.");
            var placePolicy = new PlacePolicy(
                PlaceSourceContainer.Board, PlaceEligiblePieceKind.MinionMiniature,
                PlaceDestinationScope.EmptySpacesWithinRadius, 4,
                PlaceChooserKind.ActingHero, PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.PlacedPiece);
            var destination = new HexCoordinate(3, 0);
            Assert.That(PlaceOperation.PlaceFromBoard(
                placePolicy, board, board, illusionPiece, illusion.Coordinate,
                destination, Array.Empty<HexCoordinate>(), At(RuleObject.ForHero(actor)))
                .RejectionReason, Is.EqualTo(PlaceRejectionReason.IneligiblePiece),
                "The p.18 miniature-only correction must not acquire token eligibility.");
            var action = new ActionDefinition(
                new ActionDefinitionId("illusion-place-action"), ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, ProbeText());
            var definition = new CardDefinition(
                new CardDefinitionId("illusion-place-card"), "rev-1", "Illusion Place",
                new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated, new[] { action });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("illusion-place-instance"), definition.Reference, actor);
            var context = new EffectCapabilityContext(
                new EffectNodeId("illusion-place-node"), EffectFailurePolicy.Mandatory,
                actor, actor, card.Id, card.DefinitionReference, action.Id,
                new Dictionary<string, EffectValue>
                {
                    ["subject"] = EffectValue.Space(new HexCoordinate(0, 0)),
                    ["radius"] = EffectValue.Integer(4),
                    [SyntheticPlacementCapabilities.EligiblePieceParameterName] =
                        EffectValue.Integer((int)PlaceEligiblePieceKind.MinionMiniature),
                    [SyntheticPlacementCapabilities.DestinationAnchorParameterName] =
                        EffectValue.Integer((int)PlaceDestinationAnchor.PlacedPiece)
                },
                1, 1, participants, timing, At(RuleObject.ForHero(actor)),
                EffectCardState.Of(new[] { definition }, new[] { card }), ActionKind.Skill,
                new MatchComponents(
                    new ComponentDefinitionCatalog(
                        new[] { NebKherComponentContent.Illusion },
                        Array.Empty<MarkerDefinition>()),
                    board, new MarkerAttachmentRegister(), board, board, board, board),
                new HeroItemState(participants));
            var place = SyntheticPlacementCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(SyntheticPlacementCapabilities.PlaceV2));
            Assert.That(place.Execute(context).Choice.Candidates,
                Has.Member(EffectValue.Space(destination)));
            Assert.That(place.Resume(context, new[] { EffectValue.Space(destination) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "Card text naming a minion may Place an Illusion during the controller's action.");

            var chosenSpace = new EffectOutputBinding(
                SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate);
            var rosterContext = new EffectCapabilityContext(
                new EffectNodeId("illusion-roster-target"), EffectFailurePolicy.Mandatory,
                actor, actor, card.Id, card.DefinitionReference, action.Id,
                new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.OriginParameter] =
                        EffectValue.Integer((int)RosterQueryOrigin.Actor),
                    [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(4),
                    [SyntheticRosterCapabilities.UnitKindParameter] =
                        EffectValue.Integer((int)RosterUnitKind.Minion),
                    [SyntheticRosterCapabilities.RelationParameter] =
                        EffectValue.Integer((int)SourceTeamRelation.AlliedWithSource),
                    ["activity"] = EffectValue.Integer((int)RuleActivity.Skill)
                },
                1, 1, participants, timing, At(RuleObject.ForHero(actor)),
                EffectCardState.Of(new[] { definition }, new[] { card }), ActionKind.Skill,
                context.Components, new HeroItemState(participants),
                declaredOutputs: new[] { chosenSpace });
            var rosterTarget = SyntheticRosterCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(SyntheticRosterCapabilities.ChooseTargetV2));
            Assert.That(rosterTarget.Execute(rosterContext).Choice.Candidates,
                Is.EqualTo(new[] { EffectValue.Space(destination) }));
            var named = rosterTarget.Resume(
                rosterContext, new[] { EffectValue.Space(destination) });
            Assert.That(named.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(named.Outcomes.Single().OutputName,
                Is.EqualTo(SyntheticRosterCapabilities.ChosenSpaceOutput));
            Assert.That(named.Outcomes.Single().Value, Is.EqualTo(EffectValue.Space(destination)));
        }

        [Test]
        public void BorrowedPlacementUsesIllusionSupplyAndReclaimsIllusionsNotOriginalKind()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var sourceToken = new TokenDefinition(
                new TokenDefinitionId("borrow-original-token"), 1, "Original", 2,
                Array.Empty<TokenPropertyDefinition>(),
                TokenRemovalCondition.EndOfRoundCleanup);
            var illusion = NebKherComponentContent.Illusion;
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-2, 8).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(-1, 0))
                }, Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
            var catalog = new ComponentDefinitionCatalog(
                new[] { sourceToken, illusion }, Array.Empty<MarkerDefinition>());
            var action = new ActionDefinition(
                new ActionDefinitionId("borrow-placement-skill"), ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, ProbeText());
            var cardDefinition = new CardDefinition(
                new CardDefinitionId("borrow-placement-card"), "rev-1", "Borrow placement",
                new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated, new[] { action });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("borrow-placement-instance"), cardDefinition.Reference, actor);
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var context = new EffectCapabilityContext(
                new EffectNodeId("borrow-placement-node"), EffectFailurePolicy.Mandatory,
                actor, actor, card.Id, card.DefinitionReference, action.Id,
                new System.Collections.Generic.Dictionary<string, EffectValue>(),
                1, 2, participants, null, RuleModificationSet.Empty,
                EffectCardState.Of(new[] { cardDefinition }, new[] { card }),
                ActionKind.Skill,
                new MatchComponents(catalog, board, new MarkerAttachmentRegister(),
                    board, board, board, board),
                new HeroItemState(participants),
                enclosingBorrow: BorrowRequest.ForPreviousSlotPrintedAction(
                    card.Id, 0, illusion.Id));
            var place = SyntheticTokenCapabilities.PlaceHandler(
                new EffectCapabilityReference(new EffectCapabilityId("borrow-place"), 1),
                sourceToken, PrintedValueChannel.Radius);
            var reclaim = SyntheticTokenCapabilities.ReclaimHandler(
                new EffectCapabilityReference(new EffectCapabilityId("borrow-reclaim"), 1),
                sourceToken);

            foreach (var q in new[] { 2, 3, 4 })
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("borrow-illusion-" + q), illusion,
                    actor, actor, card.Id, new HexCoordinate(q, 0)));
            Assert.That(place.Execute(context).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(reclaim.Execute(context).Choice.Candidates,
                Is.EqualTo(new[]
                {
                    EffectValue.Space(new HexCoordinate(2, 0)),
                    EffectValue.Space(new HexCoordinate(3, 0)),
                    EffectValue.Space(new HexCoordinate(4, 0))
                }));
            Assert.That(reclaim.Resume(context,
                new[] { EffectValue.Space(new HexCoordinate(2, 0)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(place.Resume(context,
                new[] { EffectValue.Space(new HexCoordinate(1, 0)) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens.Single(token =>
                token.Coordinate.Equals(new HexCoordinate(1, 0))).Definition.Reference,
                Is.EqualTo(illusion.Reference));
            Assert.That(catalog.CountAvailableTokens(sourceToken.Reference, board.PlacedTokens),
                Is.EqualTo(2), "The original kind was never consumed.");
        }

        [Test]
        public void TwoIllusionsCanBeChosenAsMinionsOrUnitsByCoordinateBackedPairActions()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var first = new HexCoordinate(1, 0);
            var second = new HexCoordinate(0, 1);
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 6).Select(q => new HexCoordinate(q, 0))
                    .Concat(new[] { second }).ToArray(),
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(4, 0))
                }, Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
            foreach (var space in new[] { first, second })
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("pair-illusion-" + space.Q + "-" + space.R),
                    NebKherComponentContent.Illusion, actor, actor,
                    new CardInstanceId("pair-illusion-source"), space));
            var effect = new ActiveEffectInstance(
                new ActiveEffectId("pair-illusion-effect"),
                NebKherComponentContent.IllusionsCountAsMinions,
                actor, TeamSide.Red, default,
                new CardInstanceId("pair-illusion-source"),
                new ActionDefinitionId("pair-illusion-action"),
                1, 1, 0, ActiveEffectPhase.Active, 1, 1);
            var timing = ActiveEffectLifecycle.Restore(new ActiveEffectLifecycleRestoreState(
                new[] { effect }, Array.Empty<ContinuousRuleSource>()));
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            var modifications = RuleModificationQuery.Build(
                timing, board, participants, TieBreakerState.Unrevealed,
                UnlockedUltimateSet.None, RuleObject.ForHero(actor), null,
                new MarkerAttachmentRegister());
            var action = new ActionDefinition(
                new ActionDefinitionId("pair-illusion-action"), ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, ProbeText());
            var definition = new CardDefinition(
                new CardDefinitionId("pair-illusion-card"), "rev-1", "Pair Illusions",
                new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated, new[] { action });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("pair-illusion-instance"), definition.Reference, actor);
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(
                    new[] { NebKherComponentContent.Illusion }, Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board, board);
            var outputs = new[]
            {
                new EffectOutputBinding("firstSpace", EffectValueKind.Coordinate),
                new EffectOutputBinding("secondSpace", EffectValueKind.Coordinate)
            };
            EffectCapabilityContext Context(
                string node, Dictionary<string, EffectValue> inputs) =>
                new EffectCapabilityContext(
                    new EffectNodeId(node), EffectFailurePolicy.Mandatory,
                    actor, actor, card.Id, card.DefinitionReference, action.Id,
                    inputs, 1, 1, participants, timing, modifications,
                    EffectCardState.Of(new[] { definition }, new[] { card }),
                    ActionKind.Skill, components, new HeroItemState(participants),
                    declaredOutputs: outputs);
            var pair = SyntheticRosterCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(SyntheticRosterCapabilities.ChooseTargetPairV1));
            var pairContext = Context("pair-minions", new Dictionary<string, EffectValue>
            {
                [SyntheticRosterCapabilities.OriginParameter] =
                    EffectValue.Integer((int)RosterQueryOrigin.Actor),
                [SyntheticRosterCapabilities.DistanceParameter] = EffectValue.Integer(3),
                [SyntheticRosterCapabilities.RelationParameter] =
                    EffectValue.Integer((int)SourceTeamRelation.AlliedWithSource)
            });
            Assert.That(pair.Execute(pairContext).Choice.Candidates,
                Is.EquivalentTo(new[] { EffectValue.Space(first), EffectValue.Space(second) }));
            var chosen = new[] { EffectValue.Space(first), EffectValue.Space(second) };
            var pairResult = pair.Resume(pairContext, chosen);
            Assert.That(pairResult.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(pairResult.Outcomes.Select(fact => fact.OutputName),
                Is.EquivalentTo(new[] { "firstSpace", "secondSpace" }));

            var units = SyntheticRosterCapabilities.Handlers().Single(handler =>
                handler.Reference.Equals(
                    SyntheticRosterCapabilities.ChooseUnitPairAtEqualDistanceInRadiusV1));
            var unitContext = Context("pair-units", new Dictionary<string, EffectValue>());
            Assert.That(units.Execute(unitContext).Choice.Candidates,
                Is.EquivalentTo(chosen));
            var unitResult = units.Resume(unitContext, chosen);
            Assert.That(unitResult.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(unitResult.Outcomes.Select(fact => fact.OutputName),
                Is.EquivalentTo(new[] { "firstSpace", "secondSpace" }));
        }

        [Test]
        public void BorrowedCustomWakeAndRockPlacementsUseIllusionPiecesAndTheirSharedSupply()
        {
            var actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 7).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(actor, new HexCoordinate(2, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(-1, 0))
                }, Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
            var catalog = new ComponentDefinitionCatalog(new[]
            {
                NebKherComponentContent.Illusion,
                IgnatiaMagmaContent.MagmaTokenDefinition,
                MrakComponentContent.Rock
            }, Array.Empty<MarkerDefinition>());
            var action = new ActionDefinition(
                new ActionDefinitionId("borrow-custom-place-action"), ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, ProbeText());
            var definition = new CardDefinition(
                new CardDefinitionId("borrow-custom-place-card"), "rev-1", "Custom placement",
                new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated, new[] { action });
            var card = CardInstance.CreateInHand(
                new CardInstanceId("borrow-custom-place-instance"), definition.Reference, actor);
            var participants = StagedBoardParticipants.CreateWithBlueAlly();
            EffectCapabilityContext Context(string node, Dictionary<string, EffectValue> inputs) =>
                new EffectCapabilityContext(
                    new EffectNodeId(node), EffectFailurePolicy.Mandatory,
                    actor, actor, card.Id, card.DefinitionReference, action.Id,
                    inputs, 1, 2, participants, null, RuleModificationSet.Empty,
                    EffectCardState.Of(new[] { definition }, new[] { card }),
                    ActionKind.Skill,
                    new MatchComponents(catalog, board, new MarkerAttachmentRegister(),
                        board, board, board, board),
                    new HeroItemState(participants),
                    enclosingBorrow: BorrowRequest.ForPreviousSlotPrintedAction(
                        card.Id, 0, NebKherComponentContent.Illusion.Id));

            var wake = IgnatiaMagmaContent.Handlers().Single(handler =>
                handler.Reference.Equals(IgnatiaMagmaContent.PlaceMagmaWakeV1));
            var wakeResult = wake.Execute(Context("borrow-magma-wake",
                new Dictionary<string, EffectValue>
                {
                    [IgnatiaMagmaContent.WakeOriginParameterName] =
                        EffectValue.Space(new HexCoordinate(0, 0)),
                    [IgnatiaMagmaContent.WakeDestinationParameterName] =
                        EffectValue.Space(new HexCoordinate(2, 0))
                }));
            Assert.That(wakeResult.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens.Select(token => token.Coordinate),
                Is.EquivalentTo(new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) }));

            var rock = MrakRockPlacementContent.Handlers().Single(handler =>
                handler.Reference.Equals(MrakRockPlacementContent.PlaceInSpaceV1));
            var rockResult = rock.Execute(Context("borrow-rock-place",
                new Dictionary<string, EffectValue>
                {
                    [MrakRockPlacementContent.SpaceParameterName] =
                        EffectValue.Space(new HexCoordinate(3, 0))
                }));
            Assert.That(rockResult.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(3));
            Assert.That(board.PlacedTokens.All(token =>
                    token.Definition.Reference.Equals(NebKherComponentContent.Illusion.Reference)),
                Is.True, "Every custom token path consumes the borrowed action's Illusion supply.");
            Assert.That(catalog.CountAvailableTokens(
                NebKherComponentContent.Illusion.Reference, board.PlacedTokens), Is.Zero);
            Assert.That(catalog.CountAvailableTokens(
                IgnatiaMagmaContent.MagmaTokenDefinition.Reference, board.PlacedTokens),
                Is.EqualTo(IgnatiaMagmaContent.MagmaTokenDefinition.SupplyCount));
            Assert.That(catalog.CountAvailableTokens(
                MrakComponentContent.Rock.Reference, board.PlacedTokens),
                Is.EqualTo(MrakComponentContent.Rock.SupplyCount));
        }

        private static EffectDefinition ProbeText()
        {
            var node = new EffectNodeId("borrow-placement-root");
            return new EffectDefinition(
                new EffectDefinitionId("borrow-placement-text"), "rev-1", node,
                new[]
                {
                    new EffectNodeDefinition(
                        node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                        })
                });
        }

        [Test]
        public void ThreeIllusions_ShareOneFiniteSupplyAndOrdinaryRoundCleanup()
        {
            var definition = NebKherComponentContent.Illusion;
            Assert.That(definition.SupplyCount, Is.EqualTo(3));
            Assert.That(definition.RemovalCondition,
                Is.EqualTo(TokenRemovalCondition.EndOfRoundCleanup));
            Assert.That(definition.Properties, Is.Empty,
                "The action-scoped minion reading belongs to the live action, not an always-on token property.");
            var handlers = NebKherComponentContent.Handlers().ToArray();
            Assert.That(handlers.Select(handler => handler.Reference), Is.SupersetOf(new[]
            {
                NebKherComponentContent.PlaceIllusionV1,
                NebKherComponentContent.ReclaimIllusionV1,
                NebKherComponentContent.IllusionsCountAsMinionsV1
            }));

            var nebKher = new HeroId("nebkher");
            var enemy = new HeroId("enemy");
            var source = new CardInstanceId("illusion-source");
            var board = new AdjacentMoveAuthority(
                Enumerable.Range(-1, 8).Select(q => new HexCoordinate(q, 0)).ToArray(),
                new[]
                {
                    new HeroOccupancy(nebKher, new HexCoordinate(0, 0)),
                    new HeroOccupancy(enemy, new HexCoordinate(-1, 0))
                }, Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
            var catalog = new ComponentDefinitionCatalog(
                new[] { definition }, Array.Empty<MarkerDefinition>());
            foreach (var q in new[] { 2, 3, 4 })
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("illusion-" + q), definition,
                    nebKher, nebKher, source, new HexCoordinate(q, 0)));

            Assert.That(catalog.CountAvailableTokens(definition.Reference, board.PlacedTokens),
                Is.Zero);
            Assert.That(ComponentCleanupRules.TokensRemovedAtEndOfTurn(board.PlacedTokens),
                Is.Empty);
            Assert.That(ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens).Count,
                Is.EqualTo(3));

            var swapped = SwapOperation.Swap(SwapEligiblePieceKind.GamePiece,
                board, board, BoardObjectReference.ForHero(nebKher),
                BoardObjectReference.ForToken(new TokenInstanceId("illusion-3")),
                RuleModificationSet.Empty);
            Assert.That(swapped.Accepted, Is.True);
            Assert.That(board.Occupancies.Single(hero => hero.HeroId.Equals(nebKher)).Coordinate,
                Is.EqualTo(new HexCoordinate(3, 0)));
            var moved = board.PlacedTokens.Single(token => token.Id.Value == "illusion-3");
            Assert.That(moved.Coordinate, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(moved.OwnerHeroId, Is.EqualTo(nebKher));
            Assert.That(moved.ControllerHeroId, Is.EqualTo(nebKher));
            Assert.That(moved.SourceCardInstanceId, Is.EqualTo(source));

            board.CommitValidatedTokenReclamation(moved.Id);
            Assert.That(catalog.CountAvailableTokens(definition.Reference, board.PlacedTokens),
                Is.EqualTo(1), "p.18 short supply reclaims the physical token, not a fresh copy.");
        }
    }
}
