using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.DodgerSpawnPointBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z28's two new contracts, and the two candidate narrowings beside them, driven
    /// through the handlers themselves rather than read off a definition.
    ///
    /// <para>The four pieces answer one printed phrase in the three grammars Dodger's faces use it in -
    /// a counted gate, an adjacency gate, and a narrowing on who may be picked - so they are exercised
    /// together, on the one board built to make the user's two rulings measurable.</para>
    /// </summary>
    public sealed class EmptyMinionSpawnPointCapabilityTests
    {
        private static readonly HeroId Dodger = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        private static readonly EffectCapabilityCatalog RosterCatalog =
            new EffectCapabilityCatalog(SyntheticRosterCapabilities.Handlers());

        private static readonly EffectCapabilityCatalog TargetingCatalog =
            new EffectCapabilityCatalog(SyntheticTargetingCapabilities.Handlers());

        // -----------------------------------------------------------------------------------------
        // `empty-minion-spawn-points-adjacent@1` - the two Gold cards' clause
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The adjacency arm publishes the count and the answer, and its default threshold is one -
        /// which is what "adjacent to an empty spawn point" asks for without printing a number.
        /// </summary>
        [Test]
        public void TheAdjacencyArm_AnswersPresentBesideOneQualifyingPoint()
        {
            var board = CreateBoard(Dodger, Origin);
            var applied = Handler(
                    RosterCatalog, SyntheticRosterCapabilities.EmptyMinionSpawnPointsAdjacentV1)
                .Execute(Context(board, Origin, new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.MinimumPresentParameter] = EffectValue.Integer(1)
                }));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(Fact(applied, SyntheticRosterCapabilities.CountOutput).IntegerValue,
                Is.EqualTo(2),
                "The melee point on (1, 0) and the ranged point on (-1, 0). The hero spawn point on " +
                "(0, 1) is also one space away and is not one of them.");
            Assert.That(Fact(applied, SyntheticRosterCapabilities.PresentOutput).BooleanValue,
                Is.True);
        }

        /// <summary>
        /// Away from every qualifying point the answer is false rather than a failure - which is what
        /// the user's RULING 3 needs from it: an "If X" clause whose condition is false skips, and the
        /// card continues rather than stopping.
        /// </summary>
        [Test]
        public void TheAdjacencyArm_AnswersFalseRatherThanFailing()
        {
            var perch = new HexCoordinate(0, -2);
            var board = CreateBoard(Dodger, perch);
            var applied = Handler(
                    RosterCatalog, SyntheticRosterCapabilities.EmptyMinionSpawnPointsAdjacentV1)
                .Execute(Context(board, perch, new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.MinimumPresentParameter] = EffectValue.Integer(1)
                }));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "A condition that is not satisfied is an answer, not an impossibility.");
            Assert.That(Fact(applied, SyntheticRosterCapabilities.PresentOutput).BooleanValue,
                Is.False);
            Assert.That(Fact(applied, SyntheticRosterCapabilities.CountOutput).IntegerValue,
                Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------------------------
        // `empty-minion-spawn-points-in-radius@1` - the three rituals' clause
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The radius arm reads the card's own printed icon. Dark Ritual prints Radius 3, so from a
        /// space where a Radius of 2 would reach one point it reaches three - and the threshold it is
        /// measured against is the "2 or more" the sentence writes down.
        /// </summary>
        [Test]
        public void TheRadiusArm_ReadsTheCardsOwnPrintedIcon()
        {
            var perch = new HexCoordinate(0, -1);
            var board = CreateBoard(Dodger, perch, RangedSpawnNear);
            var applied = Handler(
                    RosterCatalog, SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1)
                .Execute(RitualContext(board, perch, DodgerCardContent.DarkRitual, minimum: 2));

            Assert.That(Fact(applied, SyntheticRosterCapabilities.CountOutput).IntegerValue,
                Is.EqualTo(3),
                "Dark Ritual prints Radius 3. A Radius of 2 from (0, -1) would reach one point, so " +
                "this number can only come from the card's own icon.");
            Assert.That(Fact(applied, SyntheticRosterCapabilities.PresentOutput).BooleanValue,
                Is.True);
        }

        /// <summary>
        /// Necromastery prints Radius 1, which is p.9's smallest disk - so the same board answers
        /// differently for a different card, and the reach is the card's rather than the query's.
        /// </summary>
        [Test]
        public void TheRadiusArm_AnswersPerCardAndNotPerBoard()
        {
            var board = CreateBoard(Dodger, Origin);

            var wide = Handler(
                    RosterCatalog, SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1)
                .Execute(RitualContext(board, Origin, DodgerCardContent.DarkRitual, minimum: 2));
            var narrow = Handler(
                    RosterCatalog, SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1)
                .Execute(RitualContext(board, Origin, DodgerCardContent.Necromastery, minimum: 2));

            Assert.That(Fact(wide, SyntheticRosterCapabilities.CountOutput).IntegerValue,
                Is.EqualTo(4), "Radius 3 reaches every qualifying point on this board.");
            Assert.That(Fact(narrow, SyntheticRosterCapabilities.CountOutput).IntegerValue,
                Is.EqualTo(2), "Radius 1 reaches only the innermost ring.");
        }

        /// <summary>
        /// A card printing no Radius icon has no disk, and p.16 does not invent one - so the count is
        /// zero rather than an adjacency by default. Necromancy is the face that prints none.
        /// </summary>
        [Test]
        public void TheRadiusArm_CountsNothingForACardThatPrintsNoIcon()
        {
            var board = CreateBoard(Dodger, Origin);
            var applied = Handler(
                    RosterCatalog, SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1)
                .Execute(RitualContext(board, Origin, DodgerCardContent.Necromancy, minimum: 2));

            Assert.That(Fact(applied, SyntheticRosterCapabilities.CountOutput).IntegerValue,
                Is.EqualTo(0));
            Assert.That(Fact(applied, SyntheticRosterCapabilities.PresentOutput).BooleanValue,
                Is.False);
        }

        /// <summary>
        /// An actor who is not standing anywhere cannot be asked the question at all, which is a
        /// different answer from "nothing qualifies" and is reported as one.
        /// </summary>
        [Test]
        public void AnActorWhoIsNotOnTheBoard_MakesTheQuestionImpossibleRatherThanFalse()
        {
            var board = CreateBoard(Enemy, Origin);
            var applied = Handler(
                    RosterCatalog, SyntheticRosterCapabilities.EmptyMinionSpawnPointsAdjacentV1)
                .Execute(Context(board, Origin, new Dictionary<string, EffectValue>()));

            Assert.That(applied.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        // -----------------------------------------------------------------------------------------
        // The two candidate narrowings
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Death Trap's clause, measured on the offer: an enemy hero in radius who is NOT beside a
        /// qualifying spawn point is not offered at all, rather than offered and then refused.
        ///
        /// <para>The enemy stands on (0, -2) in the first board and on (1, 1) in the second. Both are
        /// well inside the card's printed Radius of 4 from Dodger at the origin; only the second is
        /// beside a qualifying point, which is the melee point on (1, 0).</para>
        /// </summary>
        [Test]
        public void TheSpawnPointNarrowing_OffersOnlyHeroesStandingBesideAQualifyingPoint()
        {
            var awayAt = new HexCoordinate(0, -2);
            var besideAt = new HexCoordinate(1, 1);
            var away = TwoHeroBoard(Origin, awayAt);
            var beside = TwoHeroBoard(Origin, besideAt);

            // Asserted rather than eyeballed, because the whole case is about which of the four
            // qualifying points each hero is beside - and (0, -1) would have looked away from the
            // melee point while standing beside the ranged one.
            Assert.That(
                EmptyMinionSpawnPointQuery.AnyWithin(away, away, awayAt, 1), Is.False,
                "The first enemy is beside none of the four qualifying points.");
            Assert.That(
                EmptyMinionSpawnPointQuery.AnyWithin(beside, beside, besideAt, 1), Is.True,
                "The second is beside the melee point on (1, 0).");
            Assert.That(
                HexDistanceQuery.Calculate(Origin, awayAt), Is.LessThanOrEqualTo(4),
                "Both stand inside Death Trap's printed Radius of 4, so only the narrowing can " +
                "separate them.");

            Assert.That(
                RadiusHeroCandidates(away, adjacentToAQualifyingPoint: true), Is.Empty,
                "\"who is adjacent to an empty spawn point in the battle zone\" is part of who may be " +
                "picked, so a hero standing alone is not one to offer and then refuse.");
            Assert.That(
                RadiusHeroCandidates(beside, adjacentToAQualifyingPoint: true),
                Is.EqualTo(new[] { Enemy }));
            Assert.That(
                RadiusHeroCandidates(away, adjacentToAQualifyingPoint: false),
                Is.EqualTo(new[] { Enemy }),
                "Undeclared, the narrowing is absent - which is what keeps every hero selection " +
                "composed before Dodger at its exact candidate set.");
        }

        /// <summary>
        /// The narrowing is measured from each candidate and not from the actor, which is the whole
        /// reason it cannot be expressed by moving the selection's origin: Dodger here is nowhere near
        /// a qualifying point and the enemy beside one is still offered.
        /// </summary>
        [Test]
        public void TheSpawnPointNarrowing_IsMeasuredFromTheCandidateAndNotTheActor()
        {
            var board = TwoHeroBoard(new HexCoordinate(0, -2), new HexCoordinate(1, 1));

            Assert.That(
                EmptyMinionSpawnPointQuery.AnyWithin(board, board, new HexCoordinate(0, -2), 1),
                Is.False,
                "Dodger is adjacent to no qualifying point.");
            Assert.That(
                RadiusHeroCandidates(board, adjacentToAQualifyingPoint: true),
                Is.EqualTo(new[] { Enemy }),
                "The clause qualifies the hero being picked, so hers does not enter it.");
        }

        /// <summary>
        /// Littlefinger of Death's clause: "a hero in range who has one or more cards in the discard".
        /// The pile is the named hero's own, and a hero holding everything in hand is not offered.
        /// </summary>
        [Test]
        public void TheDiscardPileNarrowing_OffersOnlyHeroesWhoHaveDiscarded()
        {
            var board = TwoHeroBoard(Origin, new HexCoordinate(1, 0));

            Assert.That(
                RangeHeroCandidates(board, holdsACardInDiscard: false, requireDiscard: true),
                Is.Empty,
                "A hero whose cards are all in hand has an empty pile, and \"who has\" is part of who " +
                "may be picked.");
            Assert.That(
                RangeHeroCandidates(board, holdsACardInDiscard: true, requireDiscard: true),
                Is.EqualTo(new[] { Enemy }));
            Assert.That(
                RangeHeroCandidates(board, holdsACardInDiscard: false, requireDiscard: false),
                Is.EqualTo(new[] { Enemy }),
                "Undeclared, the narrowing is absent.");
        }

        // -----------------------------------------------------------------------------------------
        // Fixture plumbing
        // -----------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler Handler(
            EffectCapabilityCatalog catalog, EffectCapabilityReference reference) =>
            catalog.Load(reference);

        private static EffectValue Fact(EffectExecutionResult result, string outputName) =>
            result.Outcomes.Single(outcome => outcome.OutputName == outputName).Value;

        private static IReadOnlyList<HeroId> RadiusHeroCandidates(
            AdjacentMoveAuthority board, bool adjacentToAQualifyingPoint)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.Skill),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                [SyntheticTargetingCapabilities.ExcludeAdjacentParameterName] =
                    EffectValue.Boolean(false)
            };
            if (adjacentToAQualifyingPoint)
            {
                inputs[SyntheticTargetingCapabilities.AdjacentToEmptyMinionSpawnPointParameterName] =
                    EffectValue.Boolean(true);
            }

            var handler = (IEffectSelectionCapabilityHandler)Handler(
                TargetingCatalog, SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            return handler
                .AnticipateCandidates(CardContext(board, DodgerCardContent.DeathTrap, inputs))
                .Select(value => value.HeroId)
                .ToArray();
        }

        private static IReadOnlyList<HeroId> RangeHeroCandidates(
            AdjacentMoveAuthority board, bool holdsACardInDiscard, bool requireDiscard)
        {
            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.Attack),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                [SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName] =
                    EffectValue.Boolean(false)
            };
            if (requireDiscard)
            {
                inputs[SyntheticTargetingCapabilities.HasCardInDiscardParameterName] =
                    EffectValue.Boolean(true);
            }

            var handler = (IEffectSelectionCapabilityHandler)Handler(
                TargetingCatalog, SyntheticTargetingCapabilities.RangeHeroTargetV1);
            return handler
                .AnticipateCandidates(CardContext(
                    board,
                    DodgerCardContent.LittlefingerOfDeath,
                    inputs,
                    EnemyCards(holdsACardInDiscard)))
                .Select(value => value.HeroId)
                .ToArray();
        }

        /// <summary>
        /// The enemy's own eighteen cards, all in hand or with one moved to the discard - which is the
        /// only difference the discard narrowing may read.
        /// </summary>
        private static IReadOnlyList<CardInstance> EnemyCards(bool holdsACardInDiscard)
        {
            var cards = DodgerCardContent.CreateDealtCards(Enemy, "enemy").ToList();
            if (!holdsACardInDiscard)
            {
                return cards;
            }

            var spent = cards[0];
            cards[0] = new CardInstance(
                spent.Id,
                spent.DefinitionReference,
                spent.OwnerId,
                CardStateFacts.DiscardedUnplayed());
            return cards;
        }

        private static AdjacentMoveAuthority TwoHeroBoard(HexCoordinate dodgerAt, HexCoordinate enemyAt) =>
            new AdjacentMoveAuthority(
                CreateMap(),
                new[] { new HeroOccupancy(Dodger, dodgerAt), new HeroOccupancy(Enemy, enemyAt) },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                Array.Empty<GuardsDigital.Core.Setup.QuickGameMinionSetupState>());

        private static EffectCapabilityContext RitualContext(
            AdjacentMoveAuthority board,
            HexCoordinate actorAt,
            CardDefinitionId card,
            int minimum) =>
            CardContext(
                board,
                card,
                new Dictionary<string, EffectValue>
                {
                    [SyntheticRosterCapabilities.MinimumPresentParameter] =
                        EffectValue.Integer(minimum)
                });

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            HexCoordinate actorAt,
            IReadOnlyDictionary<string, EffectValue> inputs) =>
            Build(board, EffectCardState.Absent, default, default, inputs);

        /// <summary>
        /// A context whose source card is one of Dodger's own, so a handler that reads a printed icon
        /// reads a real one rather than a number this fixture invented.
        /// </summary>
        private static EffectCapabilityContext CardContext(
            AdjacentMoveAuthority board,
            CardDefinitionId card,
            IReadOnlyDictionary<string, EffectValue> inputs,
            IReadOnlyList<CardInstance> extraCards = null)
        {
            var definition = DodgerCardContent.Definition(card);
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("dodger-source-instance"), definition.Reference, Dodger);
            var instances = new List<CardInstance> { instance };
            if (extraCards != null)
            {
                instances.AddRange(extraCards);
            }

            var action = definition.ActionDefinitions.First(value =>
                value.Origin == ActionDefinitionOrigin.PrintedPrimary);
            return Build(
                board,
                EffectCardState.Of(DodgerCardContent.AllDefinitions, instances),
                definition.Reference,
                action.Id,
                inputs,
                instance.Id);
        }

        private static EffectCapabilityContext Build(
            AdjacentMoveAuthority board,
            EffectCardState cardState,
            CardDefinitionReference sourceDefinition,
            ActionDefinitionId sourceAction,
            IReadOnlyDictionary<string, EffectValue> inputs,
            CardInstanceId sourceInstance = default) =>
            new EffectCapabilityContext(
                new EffectNodeId("dodger-spawn-point-node"),
                EffectFailurePolicy.Mandatory,
                Dodger,
                Dodger,
                sourceInstance.IsDefined
                    ? sourceInstance
                    : new CardInstanceId("dodger-spawn-point-card"),
                sourceDefinition.IsDefined
                    ? sourceDefinition
                    : new CardDefinitionReference(
                        new CardDefinitionId("dodger-spawn-point-definition"), "rev-1"),
                sourceAction.IsDefined
                    ? sourceAction
                    : new ActionDefinitionId("dodger-spawn-point-action"),
                inputs,
                3,
                2,
                StagedBoardParticipants.Create(),
                null,
                RuleModificationSet.Empty,
                cardState,
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board));
    }
}
