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
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8k's target-selection capability, executed as the production handler the catalog
    /// registers. It offers board objects as an authoritative chooser-owned selection, publishes the
    /// chosen space as a typed coordinate output, and mutates nothing - the consuming node does
    /// whatever the effect does.
    /// </summary>
    public sealed class ChooseTargetCapabilityTests
    {
        private static readonly HeroId Actor = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
        private static readonly MapId Map = new MapId("synthetic-choose-target-map");

        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, 0);

        [Test]
        public void TheSelection_OffersTheActorAndEveryAdjacentObjectAsSpaces()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));
            PlaceToken(board, new HexCoordinate(-1, 0));

            var offered = Handler().Execute(Context(board, range: 1, includeSelf: true));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offered.Choice.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "The selection is chooser-owned; the resolver never picks for a player.");
            Assert.That(
                offered.Choice.Candidates.Select(candidate => candidate.Coordinate),
                Is.EqualTo(new[] { new HexCoordinate(-1, 0), ActorAt, new HexCoordinate(1, 0) }),
                "The token, the actor itself, and the adjacent hero, in canonical axial order.");
        }

        [Test]
        public void ASingleCandidate_StillPausesOnTheChooser()
        {
            // The uniform capability precedent: direct Move pauses on one destination and an
            // `up to 0` push pauses on one distance. A selection with one candidate is still the
            // chooser's to make, which is what keeps "chooser-owned" honest.
            var board = CreateBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler().Execute(Context(board, range: 1, includeSelf: true));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offered.Choice.Candidates.Single().Coordinate, Is.EqualTo(ActorAt));
        }

        [Test]
        public void ResumingWithAnOfferedSpace_PublishesItAsTheChosenOutput()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var applied = Handler().Resume(
                Context(board, range: 1, includeSelf: false),
                new[] { EffectValue.Space(new HexCoordinate(1, 0)) });

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                applied.Outcomes.Single(outcome => outcome.OutputName == "chosen").Value.Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)),
                "The chosen space is the typed coordinate output a later node binds.");
            Assert.That(
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Other)).Coordinate,
                Is.EqualTo(new HexCoordinate(1, 0)),
                "Selecting mutates nothing.");
        }

        [Test]
        public void AStaleOrIllegalSelection_IsRefusedAgainstAFreshDerivation()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var empty = Handler().Resume(
                Context(board, range: 1, includeSelf: false),
                new[] { EffectValue.Space(new HexCoordinate(-1, 0)) });
            var wrongKind = Handler().Resume(
                Context(board, range: 1, includeSelf: false),
                new[] { EffectValue.Hero(Other) });

            Assert.That(empty.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "An empty space is not an offered candidate; the set is re-derived, not remembered.");
            Assert.That(wrongKind.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "A non-coordinate answer never matches a coordinate selection.");
        }

        [Test]
        public void AnImmuneUnit_IsExcludedFromTheOfferedCandidates()
        {
            // The capability declares its candidates through TargetQuery, so p.18's targeting immunity
            // reaches the offer itself; TargetQueryTests holds the presence half of the pair.
            var board = CreateBoard(otherAt: new HexCoordinate(1, 0));

            var offered = Handler().Execute(Context(
                board, range: 1, includeSelf: false, modifications: SkillImmunityFor(Other)));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "The one adjacent object is immune to the targeting Skill, so nothing is offerable.");
        }

        [Test]
        public void NoCandidatesAtAll_ReportsImpossibleRatherThanAnEmptyChoice()
        {
            var board = CreateBoard(otherAt: new HexCoordinate(3, 0));

            var offered = Handler().Execute(Context(board, range: 1, includeSelf: false));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "Nothing stands adjacent and self is excluded; the enclosing node's policy decides " +
                "what that means.");
        }

        [Test]
        public void WithoutABoard_TheSelectionReportsItselfUnableToAct()
        {
            var offered = Handler().Execute(new EffectCapabilityContext(
                new EffectNodeId("choose-target-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                new CardInstanceId("choose-target-card"),
                new ActionDefinitionId("choose-target-action"),
                new Dictionary<string, EffectValue>
                {
                    ["range"] = EffectValue.Integer(1),
                    ["includeSelf"] = EffectValue.Boolean(true)
                },
                1,
                1,
                PlaceholderOneVsOneParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), null, new MarkerAttachmentRegister(),
                    null, null, null)));

            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "An absent board is being unable to act, the honest rules answer over a silent success.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler() =>
            SyntheticEffectCapabilities.CreateCatalog().Load(SyntheticTargetingCapabilities.ChooseTargetV3);

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board, int range, bool includeSelf,
            RuleModificationSet modifications = null) =>
            new EffectCapabilityContext(
                new EffectNodeId("choose-target-node"),
                EffectFailurePolicy.Mandatory,
                Actor,
                Actor,
                new CardInstanceId("choose-target-card"),
                new ActionDefinitionId("choose-target-action"),
                new Dictionary<string, EffectValue>
                {
                    ["range"] = EffectValue.Integer(range),
                    ["includeSelf"] = EffectValue.Boolean(includeSelf)
                },
                1,
                1,
                PlaceholderOneVsOneParticipants.Create(),
                null,
                modifications ?? RuleModificationSet.Empty,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board, new MarkerAttachmentRegister(),
                    board, board, board));

        private static AdjacentMoveAuthority CreateBoard(HexCoordinate otherAt)
        {
            var coordinates = Enumerable.Range(-3, 8).Select(q => new HexCoordinate(q, 0)).ToArray();
            var map = BoardMapDefinition.CreateSynthetic(Map, 1, coordinates);
            return new AdjacentMoveAuthority(
                map,
                new[] { new HeroOccupancy(Actor, ActorAt), new HeroOccupancy(Other, otherAt) },
                Array.Empty<ObstacleOccupancy>());
        }

        private static void PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate) =>
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId($"choose-target-token-{coordinate.Q}-{coordinate.R}"),
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(SyntheticComponentDefinitions.HazardV3),
                Actor,
                Actor,
                new CardInstanceId("choose-target-token-source"),
                coordinate));

        /// <summary>
        /// A live set carrying one Skill immunity, built the way the match builds it - from an Active
        /// effect in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet SkillImmunityFor(HeroId heroId)
        {
            var definition = new ActiveEffectDefinition(
                "choose-target-skill-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                "ChooseTargetSkillImmunity",
                "choose-target-skill-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(RuleActivity.Skill) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"choose-target-immunity-{heroId.Value}"),
                definition,
                heroId,
                TeamSide.Blue,
                default,
                new CardInstanceId("choose-target-immunity-source"),
                new ActionDefinitionId("choose-target-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing, null, PlaceholderOneVsOneParticipants.Create(),
                new TieBreakerState(TeamSide.Red));
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), RuleActivity.Skill, RuleObject.ForHero(Actor)),
                Is.True,
                "The fixture must actually hold the immunity, or the case above proves nothing.");
            return set;
        }
    }
}
