using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriRuneAssignmentTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        [Test]
        public void InscribeOffersAllGlyphSlotPairs_ThenCommitsOnlyACompletePermutation()
        {
            var components = Components();
            var handler = new SnorriComponentContent.InscribeRunesHandler();
            var context = Context(components, 1, 2);
            var offer = handler.Execute(context);

            Assert.That(offer.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offer.Choice.ValueVocabulary, Is.EqualTo(EffectChoiceValueVocabulary.RuneTurnSlot));
            Assert.That(offer.Choice.Candidates.Select(value => value.IntegerValue),
                Is.EquivalentTo(Enumerable.Range(1, 16)));
            Assert.That(offer.Choice.MinimumSelections, Is.EqualTo(4));
            Assert.That(offer.Choice.MaximumSelections, Is.EqualTo(4));
            Assert.That(components.Markers.Attachments, Is.Empty,
                "Nothing leaves the supply while a player is dragging an unfinished arrangement.");

            var invalid = handler.Resume(context, Values(1, 5, 12, 14));
            Assert.That(invalid.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Two glyphs cannot share turn slot one.");
            Assert.That(components.Markers.Attachments, Is.Empty);

            var applied = handler.Resume(context, Values(3, 5, 12, 14));
            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(components.Markers.Attachments.Count, Is.EqualTo(4));
            Assert.That(components.Markers.Attachments.Select(value => value.TurnSlotIndex),
                Is.EquivalentTo(new int?[] { 1, 2, 3, 4 }));
            Assert.That(components.Markers.Attachments.Select(value => value.Definition.Reference).Distinct().Count(),
                Is.EqualTo(4));
            Assert.That(components.Markers.Attachments.All(value =>
                value.ReturnCondition == MarkerReturnCondition.RetainedAtRoundEndAndDefeat), Is.True);
            Assert.That(ComponentCleanupRules.MarkersReturnedAtEndOfRound(
                components.Markers.Attachments), Is.Empty);
            Assert.That(ComponentCleanupRules.MarkersReturnedOnDefeat(
                components.Markers.Attachments, Actor), Is.Empty);
            Assert.That(SnorriComponentContent.ActiveRune(components.Markers, Actor, 1)
                .Definition.Reference, Is.EqualTo(SnorriComponentContent.Runes[1].Reference));
            Assert.That(SnorriComponentContent.ActiveRune(components.Markers, Actor, 2)
                .Definition.Reference, Is.EqualTo(SnorriComponentContent.Runes[3].Reference));
            Assert.That(SnorriComponentContent.ActiveRune(components.Markers, Actor, 3)
                .Definition.Reference, Is.EqualTo(SnorriComponentContent.Runes[0].Reference));
            Assert.That(SnorriComponentContent.ActiveRune(components.Markers, Actor, 4)
                .Definition.Reference, Is.EqualTo(SnorriComponentContent.Runes[2].Reference));
        }

        [Test]
        public void ReinscribeMovesTheSameFourPiecesAcrossRoundAndTurn()
        {
            var components = Components();
            var handler = new SnorriComponentContent.InscribeRunesHandler();
            Assert.That(handler.Resume(Context(components, 1, 2), Values(3, 5, 12, 14)).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            var firstIds = components.Markers.Attachments.Select(value => value.Id).ToArray();

            Assert.That(handler.Resume(Context(components, 2, 1), Values(1, 6, 11, 16)).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(components.Markers.Attachments.Select(value => value.Id),
                Is.EquivalentTo(firstIds));
            Assert.That(components.Markers.OfDefinition(SnorriComponentContent.Runes[0].Reference)
                .Single().TurnSlotIndex, Is.EqualTo(1));
            Assert.That(MarkerAttachmentRegister.Restore(
                components.Markers.CaptureRestoreState()).Attachments.Count, Is.EqualTo(4));
            Assert.That(SnorriComponentContent.ActiveRune(components.Markers, Actor, 1)
                .Definition.Reference, Is.EqualTo(SnorriComponentContent.Runes[0].Reference));
        }

        [Test]
        public void SilverFaceHasVerifiedPrimarySkillAndPrintedDefense()
        {
            var silver = SnorriSilverFace.Silver;
            Assert.That(silver.Id, Is.EqualTo(SnorriSilverFace.InscribeTheRunes));
            Assert.That(silver.Initiative.Value, Is.EqualTo(1));
            Assert.That(silver.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill)
                .EffectRoot, Is.Not.Null);
            Assert.That(silver.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                .BaseValues.Single().Amount, Is.EqualTo(3));
        }

        [Test]
        public void SnapshotRoundTripPreservesFourRuneSlotsAndLiveTurnSelection()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var redHero = configuration.RedHand.Instances[0].OwnerId;
            var blueHero = configuration.BlueHand.Instances[0].OwnerId;
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) },
                new[] { new HeroOccupancy(redHero, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blueHero, new HexCoordinate(1, 0)) });
            var baseline = MatchContentCatalogs.ForThisBuild();
            var components = new ComponentDefinitionCatalog(
                baseline.Components.RegisteredTokens.Select(baseline.Components.LoadToken),
                baseline.Components.RegisteredMarkers.Select(baseline.Components.LoadMarker)
                    .Concat(SnorriComponentContent.Markers()));
            var catalogs = new MatchContentCatalogs(baseline.Capabilities, components,
                baseline.TriggeredEffects);
            var markers = new MarkerAttachmentRegister();
            for (var ordinal = 1; ordinal <= 4; ordinal++)
            {
                markers.Give(new MarkerAttachment(new MarkerInstanceId("rune-" + ordinal),
                    SnorriComponentContent.Runes[ordinal - 1], redHero, redHero,
                    new CardInstanceId("silver"), SnorriSilverFace.PrimarySkill,
                    5 - ordinal));
            }
            var actions = new CardActionResolutionAuthority(catalogs, board, markers);
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions, configuration.CardInstances,
                configuration.Participants, configuration.TieBreakerState, 1,
                RoundResolutionComposition.ForMatch(actions, board,
                    new GuardsDigital.Core.Timing.ActiveEffectLifecycle(), markers,
                    null, null));
            var snapshot = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(catalogs, snapshot,
                configuration.CardDefinitions, new[] { board.MapDefinition },
                Enumerable.Empty<QuickGameMinionSetupState>());

            Assert.That(restored.Round.Markers.Attachments.Select(marker => marker.TurnSlotIndex),
                Is.EquivalentTo(new int?[] { 1, 2, 3, 4 }));
            Assert.That(SnorriComponentContent.ActiveRune(restored.Round.Markers, redHero, 1)
                .Definition.Reference, Is.EqualTo(SnorriComponentContent.Runes[3].Reference));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(snapshot));
        }

        [Test]
        public void SilverSkillWaitsForFourDraggedPairsBeforeCommittingMarkers()
        {
            var card = SnorriSilverFace.Silver;
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var components = Components();
            var context = new EffectResolutionContext(
                skill.EffectRoot,
                SyntheticEffectCapabilities.CreateCatalog(SnorriComponentContent.Handlers()),
                StagedBoardParticipants.Create(), null, 1, 1,
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill,
                components: components);
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("snorri-silver-skill"),
                new CardInstanceId("snorri-silver"), card.Reference, skill.Id,
                Actor, Actor, ActionInvocationContext.PrintedPrimary, context);
            Assert.That(step.Accepted, Is.True);
            Assert.That(step.Frame.PendingChoice.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.RuneTurnSlot));

            foreach (var value in new[] { 3, 5, 12, 14 })
            {
                var select = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                    command.Candidate.Value.Equals(EffectValue.Integer(value)));
                step = EffectResolutionSession.Answer(step.Frame, select, context);
                Assert.That(step.Accepted, Is.True);
                Assert.That(components.Markers.Attachments, Is.Empty,
                    "The physical pieces must stay unassigned until DONE.");
            }

            Assert.That(step.Frame.PendingChoice.ConfirmInvocation, Is.Not.Null);
            step = EffectResolutionSession.Answer(step.Frame,
                step.Frame.PendingChoice.ConfirmInvocation, context);
            Assert.That(step.Accepted, Is.True);
            Assert.That(components.Markers.Attachments.Count, Is.EqualTo(4));
        }

        private static IReadOnlyList<EffectValue> Values(params int[] values) =>
            values.Select(EffectValue.Integer).ToArray();

        private static MatchComponents Components()
        {
            var board = new AdjacentMoveAuthority(
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) },
                new[] { new HeroOccupancy(Actor, new HexCoordinate(0, 0)) });
            return new MatchComponents(
                new ComponentDefinitionCatalog(Array.Empty<TokenDefinition>(),
                    SnorriComponentContent.Markers()),
                board, new MarkerAttachmentRegister(), board, board, board);
        }

        private static EffectCapabilityContext Context(
            MatchComponents components, int round, int turn) =>
            new EffectCapabilityContext(
                new EffectNodeId("inscribe-runes"),
                EffectFailurePolicy.Mandatory,
                Actor, Actor,
                new CardInstanceId("snorri-silver"),
                new CardDefinitionReference(new CardDefinitionId("snorri-silver"), "fixture-1"),
                new ActionDefinitionId("inscribe-runes"),
                null, round, turn, StagedBoardParticipants.Create(), null,
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill, components);
    }
}
