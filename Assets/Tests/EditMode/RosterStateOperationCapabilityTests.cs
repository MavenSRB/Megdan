using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19b's card retrieval and resource operations. Candidate derivation is pure, every
    /// chooser is exact, and every stale/insufficient resource refusal is atomic.
    /// </summary>
    public sealed class RosterStateOperationCapabilityTests
    {
        private static readonly HeroId Red =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("roster-operation-map");
        private static readonly EffectCapabilityCatalog RosterCatalog =
            new EffectCapabilityCatalog(SyntheticRosterCapabilities.Handlers());

        [Test]
        public void RetrieveCard_OffersCanonicalOwnedVariantsToTheExactChooserAndRevalidatesStaleState()
        {
            var basicSkill = Definition(
                "basic-skill", CardProgressionProfile.BasicCard(CardColor.Gold), SkillAction("basic-skill"));
            var ordinarySkill = Definition(
                "ordinary-skill", CardProgressionProfile.NotStated, SkillAction("ordinary-skill"));
            var basicAttack = Definition(
                "basic-attack", CardProgressionProfile.BasicCard(CardColor.Silver), AttackAction());
            var discardedBasic = Card(
                "b-discarded-basic", basicSkill, Red, CardStateFacts.DiscardedUnplayed());
            var resolvedBasic = Card(
                "a-resolved-basic", basicSkill, Red,
                new CardStateFacts(
                    CardLocation.TurnArea, true, CardResolutionStatus.Resolved,
                    CardFaceOrientation.FaceUp));
            var discardedOrdinary = Card(
                "c-discarded-ordinary", ordinarySkill, Red, CardStateFacts.DiscardedUnplayed());
            var discardedAttack = Card(
                "d-discarded-attack", basicAttack, Red, CardStateFacts.DiscardedUnplayed());
            var otherOwner = Card(
                "e-other-owner", basicSkill, Blue, CardStateFacts.DiscardedUnplayed());
            var malformedDiscard = Card(
                "f-malformed-discard", basicSkill, Red,
                new CardStateFacts(
                    CardLocation.DiscardPile, true, CardResolutionStatus.Unresolved,
                    CardFaceOrientation.FaceUp));
            var definitions = new[] { basicSkill, ordinarySkill, basicAttack };
            var instances = new[]
            {
                discardedBasic, resolvedBasic, discardedOrdinary, discardedAttack, otherOwner,
                malformedDiscard
            };
            var handler = Handler(SyntheticRosterCapabilities.RetrieveCardV1);

            var discarded = handler.Execute(Context(
                RetrievalInputs(RosterCardRetrievalKind.OwnedDiscardedCard),
                EffectCardState.Of(definitions, instances)));
            var basic = handler.Execute(Context(
                RetrievalInputs(RosterCardRetrievalKind.OwnedResolvedOrDiscardedBasicSkill),
                EffectCardState.Of(definitions, instances)));

            Assert.That(discarded.Choice.ChooserHeroId, Is.EqualTo(Blue),
                "The declaration's exact chooser wins over actor/controller roles.");
            Assert.That(discarded.Choice.Candidates.Select(value => value.CardInstanceId.Value),
                Is.EqualTo(new[]
                {
                    "b-discarded-basic", "c-discarded-ordinary", "d-discarded-attack"
                }));
            Assert.That(basic.Choice.Candidates.Select(value => value.CardInstanceId.Value),
                Is.EqualTo(new[] { "a-resolved-basic", "b-discarded-basic" }),
                "The Basic-Skill variant admits resolved or discarded Basics, and no other card.");

            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(discardedBasic), Is.True);
            var stale = handler.Resume(
                Context(
                    RetrievalInputs(RosterCardRetrievalKind.OwnedResolvedOrDiscardedBasicSkill),
                    EffectCardState.Of(definitions, instances)),
                new[] { EffectValue.Card(discardedBasic.Id) });
            Assert.That(stale.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(discardedBasic.State, Is.EqualTo(CardStateFacts.InitialInHand()));

            var applied = handler.Resume(
                Context(
                    RetrievalInputs(RosterCardRetrievalKind.OwnedResolvedOrDiscardedBasicSkill),
                    EffectCardState.Of(definitions, instances)),
                new[] { EffectValue.Card(resolvedBasic.Id) });
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(resolvedBasic.State, Is.EqualTo(CardStateFacts.InitialInHand()));
        }

        [Test]
        public void GainResource_AnticipationIsPure_AndOverflowRefusesWithoutMutation()
        {
            var participants = StagedBoardParticipants.Create();
            var ledger = new HeroCoinLedger(participants);
            var roster = Roster(participants, ledger);
            var inputs = new Dictionary<string, EffectValue>
            {
                ["recipient"] = EffectValue.Hero(Red),
                ["amount"] = EffectValue.Integer(2)
            };
            var handler = Handler(SyntheticRosterCapabilities.GainResourceV1);

            Assert.That(handler.Anticipate(Context(inputs, roster: roster)),
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(ledger.GetCoins(Red), Is.Zero,
                "Feasibility reads the ledger and performs no grant.");
            Assert.That(handler.Execute(Context(inputs, roster: roster)).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(ledger.GetCoins(Red), Is.EqualTo(2));

            var full = HeroCoinLedger.Restore(
                participants,
                new HeroCoinLedgerRestoreState(
                    new[]
                    {
                        new KeyValuePair<HeroId, int>(Red, int.MaxValue),
                        new KeyValuePair<HeroId, int>(Blue, 0)
                    },
                    Array.Empty<DefeatOutcomeId>()));
            var fullRoster = Roster(participants, full);
            Assert.That(handler.Anticipate(Context(inputs, roster: fullRoster)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(handler.Execute(Context(inputs, roster: fullRoster)).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(full.GetCoins(Red), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void TransferResource_UsesAnExactChooserAndEveryStaleOrOverflowRefusalIsAtomic()
        {
            var participants = StagedBoardParticipants.Create();
            var ledger = new HeroCoinLedger(participants);
            Assert.That(ledger.TryGrant(Red, 3, CoinGrantKind.CardEffect, out _), Is.True);
            var roster = Roster(participants, ledger);
            var inputs = TransferInputs(1, 3);
            var handler = Handler(SyntheticRosterCapabilities.TransferResourceV1);
            var offered = handler.Execute(Context(inputs, roster: roster));

            Assert.That(offered.Choice.ChooserHeroId, Is.EqualTo(Blue));
            Assert.That(offered.Choice.Candidates.Select(value => value.IntegerValue),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(ledger.GetCoins(Red), Is.EqualTo(3));
            Assert.That(ledger.GetCoins(Blue), Is.Zero,
                "Opening the amount choice performs no half-transfer.");

            Assert.That(ledger.TryTransfer(Red, Blue, 3, out _), Is.True);
            var beforeRed = ledger.GetCoins(Red);
            var beforeBlue = ledger.GetCoins(Blue);
            var stale = handler.Resume(
                Context(inputs, roster: roster), new[] { EffectValue.Integer(2) });
            Assert.That(stale.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(ledger.GetCoins(Red), Is.EqualTo(beforeRed));
            Assert.That(ledger.GetCoins(Blue), Is.EqualTo(beforeBlue));

            var overflow = HeroCoinLedger.Restore(
                participants,
                new HeroCoinLedgerRestoreState(
                    new[]
                    {
                        new KeyValuePair<HeroId, int>(Red, 1),
                        new KeyValuePair<HeroId, int>(Blue, int.MaxValue)
                    },
                    Array.Empty<DefeatOutcomeId>()));
            var overflowResult = handler.Execute(Context(
                TransferInputs(1, 1), roster: Roster(participants, overflow)));
            Assert.That(overflowResult.Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(overflow.GetCoins(Red), Is.EqualTo(1));
            Assert.That(overflow.GetCoins(Blue), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void StateOperations_RejectUnknownChoosersAndLedgerHeroesDuringPureFeasibility()
        {
            var participants = StagedBoardParticipants.Create();
            var unknown = new HeroId("roster-unknown-hero");
            var skill = Definition(
                "unknown-chooser-skill",
                CardProgressionProfile.BasicCard(CardColor.Gold),
                SkillAction("unknown-chooser-skill"));
            var card = Card(
                "unknown-chooser-card", skill, Red, CardStateFacts.DiscardedUnplayed());
            var retrievalInputs = RetrievalInputs(RosterCardRetrievalKind.OwnedDiscardedCard);
            retrievalInputs["chooser"] = EffectValue.Hero(unknown);
            var retrieval = Context(
                retrievalInputs,
                EffectCardState.Of(new[] { skill }, new[] { card }));

            Assert.That(Handler(SyntheticRosterCapabilities.RetrieveCardV1).Anticipate(retrieval),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(Handler(SyntheticRosterCapabilities.RetrieveCardV1).Execute(retrieval).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));

            var ledger = new HeroCoinLedger(participants);
            Assert.That(ledger.TryGrant(Red, 2, CoinGrantKind.CardEffect, out _), Is.True);
            var roster = Roster(participants, ledger);
            var transferInputs = TransferInputs(1, 2);
            transferInputs["chooser"] = EffectValue.Hero(unknown);
            var transfer = Context(transferInputs, roster: roster);
            Assert.That(Handler(SyntheticRosterCapabilities.TransferResourceV1).Anticipate(transfer),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(Handler(SyntheticRosterCapabilities.TransferResourceV1).Execute(transfer).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));

            var gainInputs = new Dictionary<string, EffectValue>
            {
                ["recipient"] = EffectValue.Hero(unknown),
                ["amount"] = EffectValue.Integer(1)
            };
            Assert.That(Handler(SyntheticRosterCapabilities.GainResourceV1)
                .Anticipate(Context(gainInputs, roster: roster)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));

            var unknownSource = TransferInputs(1, 1);
            unknownSource["source"] = EffectValue.Hero(unknown);
            Assert.That(Handler(SyntheticRosterCapabilities.TransferResourceV1)
                .Anticipate(Context(unknownSource, roster: roster)),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(ledger.GetCoins(Red), Is.EqualTo(2));
            Assert.That(ledger.GetCoins(Blue), Is.Zero);
        }

        [Test]
        public void PendingTransfer_RoundTripsAndRederivesFromExistingSnapshotStateWithoutASchemaField()
        {
            var participants = StagedBoardParticipants.Create();
            var transferDefinition = TransferSkillDefinition();
            var blueDefinition = MovementDefinition();
            var definitions = new[] { transferDefinition, blueDefinition };
            var redCard = CardInstance.CreateInHand(
                new CardInstanceId("roster-transfer-card"), transferDefinition.Reference, Red);
            var blueCard = CardInstance.CreateInHand(
                new CardInstanceId("roster-blue-card"), blueDefinition.Reference, Blue);
            var instances = new[] { redCard, blueCard };
            var ledger = new HeroCoinLedger(participants);
            Assert.That(ledger.TryGrant(Red, 2, CoinGrantKind.CardEffect, out _), Is.True);
            var history = new RoundHeroDefeatHistory(participants);
            var roster = EffectRosterState.Of(
                Array.Empty<RoundTurnSlot>(), history, ledger);
            // Milestone 8.20 registered three of this vocabulary's handlers into the build's own
            // catalog, because Arien's printed faces reach them. The rest are still unregistered, so
            // this fixture adds only those - concatenating the whole set would now claim a capability
            // revision twice, which the catalog refuses by construction.
            var registered = new EffectCapabilityCatalog(
                SyntheticRosterCapabilities.RegisteredHandlers());
            var catalog = new EffectCapabilityCatalog(DefaultHandlers()
                .Concat(SyntheticRosterCapabilities.Handlers()
                    .Where(handler => !registered.Registered.Any(value =>
                        value.Equals(handler.Reference)))));
            var catalogs = new MatchContentCatalogs(
                catalog, MatchContentCatalogs.ForThisBuild().Components);
            var effect = transferDefinition.ActionDefinitions.Single().EffectRoot;
            var live = EffectResolutionSession.Start(
                new EffectResolutionId("roster-transfer-resolution"),
                redCard.Id,
                transferDefinition.Reference,
                transferDefinition.ActionDefinitions.Single().Id,
                Red,
                Red,
                ActionInvocationContext.PrintedPrimary,
                new EffectResolutionContext(
                    effect,
                    catalog,
                    participants,
                    null,
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Skill,
                    rosterState: roster));
            Assert.That(live.Frame.PendingChoice.ChooserId, Is.EqualTo(Blue));
            Assert.That(live.Frame.PendingChoice.Candidates.Select(value => value.Value.IntegerValue),
                Is.EqualTo(new[] { 1, 2 }));

            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    Map, 3, new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) }),
                new[]
                {
                    new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Blue, new HexCoordinate(1, 0))
                },
                Array.Empty<ObstacleOccupancy>());
            var cardActions = CardActionResolutionAuthority.Restore(
                catalogs,
                board,
                CardActionResolutionRestoreState.WithPendingSkill(
                    live.Frame,
                    HeroCoinLedgerRestoreStateHolder.Present(ledger.CaptureRestoreState())),
                null,
                null,
                null,
                definitions,
                instances,
                participants,
                new MarkerAttachmentRegister(),
                RuleModificationSet.Empty,
                rosterState: roster);
            var round = new RoundResolutionAuthority(
                definitions,
                instances,
                participants,
                new TieBreakerState(TeamSide.Red),
                1,
                RoundResolutionComposition.ForCardActionsOnly(cardActions));
            Assert.That(round.TryCommitCard(Red, redCard.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Blue, blueCard.Id).Accepted, Is.True);

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                catalogs,
                payload,
                definitions,
                new[] { board.MapDefinition },
                Array.Empty<QuickGameMinionSetupState>());
            var restoredChoice = restored.Round.CurrentTurnAuthority.PendingEffectChoice;

            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(43));
            Assert.That(restoredChoice.ChooserId, Is.EqualTo(Blue));
            Assert.That(restoredChoice.Candidates.Select(value => value.Value.IntegerValue),
                Is.EqualTo(new[] { 1, 2 }),
                "Restore re-asks the transfer against slots/history/ledger already on schema v27.");
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload));

            // Milestone 8.46b: the transfer asks for exactly one number, so naming it is the whole
            // answer and the restored question resolves on that command.
            var selectTwo = restoredChoice.SelectInvocations.Single(value =>
                value.Candidate.Value.IntegerValue == 2);
            Assert.That(restored.Round.TryAnswerEffectChoice(selectTwo).Accepted, Is.True);
            Assert.That(restored.CardActions.CoinLedger.GetCoins(Red), Is.Zero);
            Assert.That(restored.CardActions.CoinLedger.GetCoins(Blue), Is.EqualTo(2));
        }

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            RosterCatalog.Load(reference);

        private static EffectCapabilityContext Context(
            IReadOnlyDictionary<string, EffectValue> inputs,
            EffectCardState cards = null,
            EffectRosterState roster = null) =>
            new EffectCapabilityContext(
                new EffectNodeId("roster-operation-node"),
                EffectFailurePolicy.Mandatory,
                Red,
                Red,
                new CardInstanceId("roster-operation-card"),
                new CardDefinitionReference(new CardDefinitionId("roster-operation-definition"), "rev-1"),
                new ActionDefinitionId("roster-operation-action"),
                inputs,
                1,
                1,
                StagedBoardParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                cards ?? EffectCardState.Absent,
                ActionKind.Skill,
                rosterState: roster);

        private static EffectRosterState Roster(
            MatchParticipants participants,
            HeroCoinLedger ledger) =>
            EffectRosterState.Of(
                Array.Empty<RoundTurnSlot>(),
                new RoundHeroDefeatHistory(participants),
                ledger);

        private static Dictionary<string, EffectValue> RetrievalInputs(
            RosterCardRetrievalKind kind) =>
            new Dictionary<string, EffectValue>
            {
                ["owner"] = EffectValue.Hero(Red),
                ["chooser"] = EffectValue.Hero(Blue),
                ["kind"] = EffectValue.Integer((int)kind)
            };

        private static Dictionary<string, EffectValue> TransferInputs(int minimum, int maximum) =>
            new Dictionary<string, EffectValue>
            {
                ["source"] = EffectValue.Hero(Red),
                ["recipient"] = EffectValue.Hero(Blue),
                ["chooser"] = EffectValue.Hero(Blue),
                ["minimum"] = EffectValue.Integer(minimum),
                ["maximum"] = EffectValue.Integer(maximum)
            };

        private static CardInstance Card(
            string id,
            CardDefinition definition,
            HeroId owner,
            CardStateFacts state) =>
            new CardInstance(new CardInstanceId(id), definition.Reference, owner, state);

        private static CardDefinition Definition(
            string id,
            CardProgressionProfile progression,
            params ActionDefinition[] actions) =>
            new CardDefinition(
                new CardDefinitionId(id),
                "rev-1",
                $"TEST - {id}",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                progression,
                actions);

        private static ActionDefinition SkillAction(string id, EffectDefinition effect = null) =>
            new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                effect ?? NoteEffect(id));

        private static EffectDefinition NoteEffect(string id)
        {
            var node = new EffectNodeId($"{id}-note");
            return new EffectDefinition(
                new EffectDefinitionId($"{id}-effect"),
                "rev-1",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node,
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.NoteActorV1)
                });
        }

        private static ActionDefinition AttackAction() =>
            new ActionDefinition(
                new ActionDefinitionId("basic-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 2) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged,
                default);

        private static CardDefinition TransferSkillDefinition()
        {
            var node = new EffectNodeId("roster-transfer-node");
            var effect = new EffectDefinition(
                new EffectDefinitionId("roster-transfer-effect"),
                "rev-1",
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node,
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.TransferResourceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("source", EffectValue.Hero(Red)),
                            EffectInputBinding.FromLiteral("recipient", EffectValue.Hero(Blue)),
                            EffectInputBinding.FromLiteral("chooser", EffectValue.Hero(Blue)),
                            EffectInputBinding.FromLiteral("minimum", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("maximum", EffectValue.Integer(2))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("source", EffectValueKind.Hero),
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding("amount", EffectValueKind.Integer)
                        })
                });
            return new CardDefinition(
                new CardDefinitionId("roster-transfer-definition"),
                "rev-1",
                "TEST - Roster Transfer",
                new InitiativeValue(14),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { SkillAction("primary-transfer", effect) });
        }

        private static CardDefinition MovementDefinition() =>
            new CardDefinition(
                new CardDefinitionId("roster-blue-movement"),
                "rev-1",
                "TEST - Roster Blue Movement",
                new InitiativeValue(3),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-movement"),
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                });

        private static IEnumerable<IEffectCapabilityHandler> DefaultHandlers()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog();
            return catalog.Registered.Select(catalog.Load).ToArray();
        }
    }
}
