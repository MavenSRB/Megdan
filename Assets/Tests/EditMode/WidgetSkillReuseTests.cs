using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class WidgetSkillReuseTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly ActionDefinitionId Skill = new ActionDefinitionId("widget-reuse-skill");
        private static readonly ActionDefinitionId Movement = new ActionDefinitionId("widget-reuse-movement");
        private static HexCoordinate At(int q, int r = 0) => new HexCoordinate(q, r);

        [TestCase(false)]
        [TestCase(true)]
        public void PoolsSeparateResolvedSkillsFromAllPublicFaceUpSkills(bool faceUp)
        {
            var f = new Fixture(start: false);
            CardInstance C(string id, CardLocation location, bool played, CardResolutionStatus resolution,
                CardFaceOrientation orientation, HeroId owner = default, CardDefinitionReference definition = default) =>
                new CardInstance(new CardInstanceId(id), definition.IsDefined ? definition : f.Definitions[0].Reference,
                    owner.IsDefined ? owner : Actor, new CardStateFacts(location, played, resolution, orientation));
            var cards = new[]
            {
                C("resolved", CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp),
                C("current", CardLocation.TurnArea, true, CardResolutionStatus.Unresolved, CardFaceOrientation.FaceUp),
                C("discarded", CardLocation.DiscardPile, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp),
                C("hand-discarded", CardLocation.DiscardPile, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp),
                C("hand", CardLocation.Hand, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp),
                C("deck", CardLocation.Deck, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp),
                C("face-down", CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceDown),
                C("foreign", CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp, Enemy),
                C("ultimate", CardLocation.UltimateArea, false, CardResolutionStatus.None, CardFaceOrientation.FaceUp,
                    definition: f.Definitions[1].Reference)
            };
            var eligible = BorrowedPrimaryActionQuery.EligibleCards(faceUp ? BorrowedPrimaryActionDeclaration.FromFaceUpSkills() :
                BorrowedPrimaryActionDeclaration.FromResolvedSkills(), Actor, f.Definitions, cards,
                Array.Empty<ActiveEffectInstance>(), f.Catalogs.Capabilities);
            Assert.That(eligible.Select(c => c.Id.Value), Is.EquivalentTo(faceUp
                ? new[] { "resolved", "current", "discarded", "hand-discarded" } : new[] { "resolved" }));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void DragonKnightFiresAfterActualMovement_AndCanReuseTheCurrentSkill(int distance)
        {
            var f = new Fixture();
            f.Move(distance);
            Assert.That(f.Choice, Is.Not.Null);
            Assert.That(f.Choice.Candidates.Select(c => c.Value.CardInstanceId), Has.Member(f.Source.Id));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            f.RoundTrip();
            f.Pick(EffectValue.Card(f.Source.Id));
            Assert.That(f.Choice.SelectInvocations.Any(c => c.Candidate.OptionNodeId.Value == "place-pyro"), Is.True);
            f.RoundTrip();
            f.Option("place-pyro");
            f.Pick(EffectValue.Space(At(2)));
            Assert.That(f.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(2)));
            Assert.That(f.Choice, Is.Null, "The granted Skill's Place does not recursively fire a Movement trigger.");
            Assert.That(f.Source.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void DragonKnightCanBeDeclinedWithoutBorrowingOrMovingItsCard()
        {
            var f = new Fixture(); f.Move(0);
            Assert.That(f.Round.TryAnswerEffectChoice(f.Choice.DeclineInvocation).Accepted, Is.True);
            Assert.That(f.Board.PlacedTokens, Is.Empty);
            Assert.That(f.Choice, Is.Null);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void LockedAndForeignUltimatesDoNotGrant(bool unlocked, bool foreign)
        {
            var f = new Fixture(unlocked: unlocked, foreign: foreign); f.Move(0);
            Assert.That(f.Choice, Is.Null);
        }

        [Test]
        public void OnlyActualMovementKindsProduceTheFact_AndOwnerMustBeOnBoard()
        {
            var f = new Fixture(start: false);
            foreach (ActionKind kind in Enum.GetValues(typeof(ActionKind)))
            {
                var fact = PerformedMovementAction.ForCompletedAction(Actor, f.Source.Id, Movement, kind);
                Assert.That(fact.IsDefined, Is.EqualTo(kind == ActionKind.Movement));
            }
            var offBoard = new AdjacentMoveAuthority(f.Board.MapDefinition,
                new[] { new HeroOccupancy(Enemy, At(-4)) }, Array.Empty<ObstacleOccupancy>());
            var movement = PerformedMovementAction.ForCompletedAction(Actor, f.Source.Id, Movement, ActionKind.Movement);
            Assert.That(UltimateTriggeredEffectSourceQuery.Derive(UltimateUnlockQuery.Derive(f.Definitions, f.Cards), offBoard,
                StagedBoardParticipants.Create(), default, movementAction: movement), Is.Empty);
        }

        [Test]
        public void FightAsOneExcludesTheAttackedHeroAcrossBorrowedChoicesAndRestore()
        {
            var f = new AttackFixture();
            f.Defend();
            Assert.That(f.Choice, Is.Not.Null);
            f.RoundTrip();
            f.Pick(EffectValue.Card(f.Borrowed.Id));
            Assert.That(f.Choice, Is.Not.Null, "After-attack text must actually start the selected Skill.");
            Assert.That(f.Choice.Candidates.Select(c => c.Value), Is.EquivalentTo(new[] { EffectValue.Hero(AttackFixture.Other) }));
            Assert.That(f.Round.CurrentTurnAuthority.CurrentModifications().ExcludesTarget(
                RuleObject.ForHero(Enemy), RuleObject.ForHero(Actor)), Is.True);
            f.RoundTrip();
            f.Pick(EffectValue.Hero(AttackFixture.Other));
            Assert.That(f.Choice.ChooserId, Is.EqualTo(AttackFixture.Other));
            f.RoundTrip();
            f.Pick(EffectValue.Card(f.Cards.Single(c => c.Id.Value == "other-hand").Id));
            Assert.That(f.Cards.Single(c => c.Id.Value == "other-hand").State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(f.Cards.Single(c => c.Id.Value == "enemy-hand").State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(f.Borrowed.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Borrowed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Round.CurrentTurnAuthority.CurrentModifications().TargetExclusions, Is.Empty);
        }

        [Test]
        public void FightAsOneAllowsTokenOnlySkills_AndDecliningDoesNotChangeTheSource()
        {
            var f = new AttackFixture(tokenOnly: true); f.Defend();
            f.Pick(EffectValue.Card(f.Borrowed.Id));
            f.Option("place-pyro");
            f.RoundTrip();
            f.Pick(EffectValue.Space(At(-1)));
            Assert.That(f.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(-1)));
            Assert.That(f.Borrowed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            var declined = new AttackFixture(); declined.Defend();
            Assert.That(declined.Round.TryAnswerEffectChoice(declined.Choice.DeclineInvocation).Accepted, Is.True);
            Assert.That(declined.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(declined.Borrowed.State.Location, Is.EqualTo(CardLocation.TurnArea));
        }

        /// <summary>
        /// Absent: no resolved Skill, so the pool is empty and the may-step asks nothing. Inert: the
        /// Breath is resolved but Pyro is off the board, so its primary can act on nothing - since
        /// milestone 9.38 it is not offered at all (before, it was offered, picked and finished empty).
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void MissingAndInertResolvedSkillsFinishWithoutChangingTheBorrowedCard(bool absent)
        {
            var f = new AttackFixture(absentSkill: absent, pyro: false); f.Defend();
            Assert.That(f.Choice, Is.Null, absent ? "An empty pool asks nothing." : "A Skill that can act on nothing is not offered (9.38).");
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Borrowed.State.Location, Is.EqualTo(absent ? CardLocation.DiscardPile : CardLocation.TurnArea));
        }

        [TestCase(RuleActivity.Skill, false)]
        [TestCase(RuleActivity.Attack, true)]
        public void BorrowedSkillUsesItsOwnActivity_NotTheParentAttackImmunity(RuleActivity immunity, bool eligible)
        {
            var f = new AttackFixture(immunity: immunity); f.Defend();
            if (eligible)
            {
                f.Pick(EffectValue.Card(f.Borrowed.Id));
                Assert.That(f.Choice, Is.Not.Null);
                Assert.That(f.Choice.Candidates.Single().Value, Is.EqualTo(EffectValue.Hero(AttackFixture.Other)));
            }
            else
            {
                // Milestone 9.38: the only other hero is immune to Skills, so the Breath can act on
                // nothing and the borrow does not offer it (before, it was offered and finished empty).
                Assert.That(f.Choice, Is.Null, "A Skill immune everywhere but the excluded hero is not offered.");
                Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            }
        }

        [Test]
        public void FightAsOneCommandsAndRetainedPrefixRestoreReproduceChoicesAndOrderedEvents()
        {
            var live = new AttackFixture(); live.Defend();
            live.Pick(EffectValue.Card(live.Borrowed.Id));
            live.Pick(EffectValue.Hero(AttackFixture.Other));
            live.Pick(EffectValue.Card(new CardInstanceId("other-hand")));
            for (var prefix = 0; prefix <= live.Commands.Count; prefix++)
            {
                var replay = new AttackFixture(); replay.Defend();
                for (var i = 0; i < live.Commands.Count; i++)
                {
                    if (i == prefix) replay.RoundTrip();
                    replay.Answer(live.Commands[i]);
                    Assert.That(replay.Snapshots[i], Is.EqualTo(live.Snapshots[i]), "retained prefix " + prefix);
                    Assert.That(replay.EventPrefixes[i], Is.EqualTo(live.EventPrefixes[i]));
                }
            }
        }

        [Test]
        public void ExclusionSurvivesNestedBorrow_ThenResumesItsParentAndRejectsStaleAnswers()
        {
            var f = new AttackFixture(nested: true); f.Defend();
            f.Pick(EffectValue.Card(f.Borrowed.Id));
            f.RoundTrip();
            f.Pick(EffectValue.Card(new CardInstanceId("inner")));
            f.RoundTrip();
            var stale = f.Choice.SelectInvocations.Single();
            Assert.That(stale.Candidate.Value, Is.EqualTo(EffectValue.Hero(AttackFixture.Other)));
            var before = AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board);
            Assert.That(f.Round.TryAnswerEffectChoice(new EffectChoiceCommand(stale.ResolutionId, stale.ChoiceId,
                stale.Kind, stale.Candidate, answeringHeroId: Enemy)).Accepted, Is.False);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board), Is.EqualTo(before));
            f.Pick(EffectValue.Hero(AttackFixture.Other));
            f.Pick(EffectValue.Card(new CardInstanceId("other-hand")));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
            Assert.That(f.Choice.Candidates.Select(c => c.Value), Is.EquivalentTo(new[] { EffectValue.Hero(AttackFixture.Other) }));
            f.RoundTrip();
            before = AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board);
            Assert.That(f.Round.TryAnswerEffectChoice(stale).Accepted, Is.False);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board), Is.EqualTo(before));
            f.Pick(EffectValue.Hero(AttackFixture.Other));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Round.CurrentTurnAuthority.CurrentModifications().TargetExclusions, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnitExclusionsAreActorScoped_NotImmunityOrPresence_AndCannotBeOverridden(bool minion)
        {
            var f = new AttackFixture();
            var target = minion ? RuleObject.ForMinion(new MinionId("unit:arbitrary/é")) : RuleObject.ForHero(Enemy);
            var keys = new[] { "unrelated", BorrowedSkillTargetScope.Key(Actor, target) };
            var rules = RuleModificationSet.Empty.WithTargetExclusions(BorrowedSkillTargetScope.Read(keys));
            Assert.That(rules.ExcludesTarget(target, RuleObject.ForHero(Actor)), Is.True);
            Assert.That(rules.ExcludesTarget(target, RuleObject.ForHero(AttackFixture.Other)), Is.False);
            Assert.That(ImmunityQuery.IsImmuneTo(target, RuleActivity.Skill, RuleObject.ForHero(Actor), f.Board,
                StagedBoardParticipants.CreateWithBlueAlly(), rules), Is.False);
            Assert.That(TargetQuery.IsPrevented(target, RuleActivity.Skill, RuleObject.ForHero(Actor), f.Board,
                StagedBoardParticipants.CreateWithBlueAlly(), rules, new[]
                { new ImmunityOverrideDeclaration(RuleActivity.Skill, ImmunityOverrideReason.PrintedUnqualifiedImmunity) }), Is.True);
            if (!minion) Assert.That(UnitPresenceQuery.FindUnitsWithin(At(0), 2, EffectiveUnitKind.Hero, f.Board, rules), Has.Member(target));
            Assert.Throws<ArgumentException>(() => BorrowedSkillTargetScope.Read(new[] { BorrowedSkillTargetScope.Prefix + "bad" }));
            Assert.Throws<FormatException>(() => BorrowedSkillTargetScope.Read(new[] { BorrowedSkillTargetScope.Prefix + "bad:unit:identity" }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExcludedMinionCannotBeRemovedOrDefeated_AndPyroRemains(bool defeat)
        {
            var rules = RuleModificationSet.Empty.WithTargetExclusions(new[]
            { new ActionTargetExclusion(Actor, RuleObject.ForMinion(WidgetAnchoredSkillTests.Minion)) });
            var p = new WidgetAnchoredSkillTests.Probe(WidgetEffectContent.Nibble(WidgetAnchoredSkillTests.Card, "rev-1", defeat),
                At(1), At(-4), minion: At(2), rules: rules);
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Choices, Is.Empty);
            Assert.That(p.Board.Minions, Has.Count.EqualTo(1));
            Assert.That(p.Board.PlacedTokens, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Milestone 9.38, the 2026-10-02 playtester's report, on Widget's real faces. Fight as One
        /// (Gold: "After the attack: You may perform the primary action on your resolved Skill card,
        /// targeting a different unit.") attacks the only enemy hero both in Widget's Range 2 and in
        /// a straight line from Pyro while Fiery Breath ("An enemy hero both in range and in a
        /// straight line from Pyro discards a card, if able.") is resolved. The Breath's only subject
        /// is the unit the restriction excludes (WI-07), so the borrow must not offer it - a card-text
        /// step with no legal subject is not offered (8.35z15, W7) - and with nothing else to borrow
        /// the "may" asks nothing. On the base the Breath was offered and then canceled.
        /// </summary>
        [Test]
        public void FightAsOne_DoesNotOfferFieryBreath_WhenTheOnlyEnemyInRangeAndInPyrosLineIsTheAttackedHero()
        {
            var f = new RealFacesFixture(RealFacesFixture.SecondFarAway);
            f.AttackAndDeclineDefense();
            Assert.That(f.OfferedCards(), Has.No.Member(f.Breath.Id),
                "Fiery Breath can act on no unit other than the one Fight as One attacked, so the borrow must not offer it.");
            Assert.That(f.Choice, Is.Null, "With no borrowable Skill the may-step asks nothing.");
            Assert.That(f.Fight.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Breath.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Breath.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.HandOf(RealFacesFixture.Attacked).Count, Is.EqualTo(1), "No discard was asked of the attacked hero.");
        }

        /// <summary>
        /// The other half of the same offer: with a second enemy hero in Range and in Pyro's line the
        /// Breath is offered, and the borrowed copy still excludes the hero Fight as One attacked.
        /// </summary>
        [Test]
        public void FightAsOne_OffersFieryBreath_WhenAnotherEnemyStandsInRangeAndInPyrosLine_AndExcludesOnlyTheAttackedHero()
        {
            var f = new RealFacesFixture(RealFacesFixture.SecondInPyrosLine);
            f.AttackAndDeclineDefense();
            Assert.That(f.OfferedCards(), Has.Member(f.Breath.Id));
            f.PickBorrowedBreath();
            Assert.That(f.OfferedHeroes(), Is.EquivalentTo(new[] { RealFacesFixture.Second }),
                "The borrowed Breath targets a different unit (WI-07): the second enemy, never the attacked one.");
        }

        /// <summary>
        /// Widget's real faces on a synthetic board: Widget at the origin, Pyro one space west, the
        /// attacked enemy hero adjacent east and in Pyro's line, the second enemy hero wherever the
        /// case puts him. Each enemy holds one hand card so "discards a card" has something to ask.
        /// </summary>
        private sealed class RealFacesFixture
        {
            internal static readonly HeroId Widget = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId Attacked = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly HeroId Second = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            private static readonly HexCoordinate WidgetAt = new HexCoordinate(0, 0);
            private static readonly HexCoordinate AttackedAt = new HexCoordinate(1, 0);
            private static readonly HexCoordinate PyroAt = new HexCoordinate(-1, 0);
            /// <summary>Two from Pyro on a straight line and two from Widget: a legal Breath subject.</summary>
            internal static readonly HexCoordinate SecondInPyrosLine = new HexCoordinate(-1, -1);
            /// <summary>Out of Widget's Range and off every line from Pyro.</summary>
            internal static readonly HexCoordinate SecondFarAway = new HexCoordinate(-4, 4);

            private static readonly CardDefinition Hold = new CardDefinition(new CardDefinitionId("real-faces-hold"), "rev-1",
                "TEST - hold", new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver), Array.Empty<ActionDefinition>());
            private readonly CardDefinition[] definitions = WidgetCardContent.AllDefinitions.Concat(new[] { Hold }).ToArray();
            private readonly CardInstance[] cards;
            internal readonly RoundResolutionAuthority Round;

            internal RealFacesFixture(HexCoordinate secondAt)
            {
                var fight = WidgetCardContent.Definition(WidgetCardContent.FightAsOne);
                var breath = WidgetCardContent.Definition(WidgetCardContent.FieryBreath);
                cards = new[]
                {
                    CardInstance.CreateInHand(new CardInstanceId("fight"), fight.Reference, Widget),
                    new CardInstance(new CardInstanceId("breath"), breath.Reference, Widget,
                        new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp)),
                    CardInstance.CreateInHand(new CardInstanceId("attacked-played"), Hold.Reference, Attacked),
                    CardInstance.CreateInHand(new CardInstanceId("second-played"), Hold.Reference, Second),
                    CardInstance.CreateInHand(new CardInstanceId("attacked-hand"), Hold.Reference, Attacked),
                    CardInstance.CreateInHand(new CardInstanceId("second-hand"), Hold.Reference, Second)
                };
                var board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(new MapId("widget-real-faces"), 1,
                        Enumerable.Range(-5, 11).SelectMany(q => Enumerable.Range(-5, 11).Select(r => new HexCoordinate(q, r)))),
                    new[] { new HeroOccupancy(Widget, WidgetAt), new HeroOccupancy(Attacked, AttackedAt), new HeroOccupancy(Second, secondAt) },
                    Array.Empty<ObstacleOccupancy>());
                board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("pyro"),
                    WidgetComponentContent.PyroDefinition, Widget, Widget, cards[1].Id, PyroAt));
                Round = new RoundResolutionAuthority(definitions, cards, StagedBoardParticipants.CreateWithBlueAlly(),
                    new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForCardActionsOnly(new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board)));
            }

            internal CardInstance Fight => cards.Single(card => card.Id.Value == "fight");
            internal CardInstance Breath => cards.Single(card => card.Id.Value == "breath");
            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority?.PendingEffectChoice;

            internal void AttackAndDeclineDefense()
            {
                foreach (var (hero, id) in new[] { (Widget, Fight.Id), (Attacked, cards[2].Id), (Second, cards[3].Id) })
                {
                    var committed = Round.TryCommitCard(hero, id);
                    Assert.That(committed.Accepted, Is.True, hero.Value + ": " + committed.RejectionReason + "/" + committed.TurnRejectionReason);
                }

                var attack = WidgetCardContent.Definition(WidgetCardContent.FightAsOne).ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(definitions, cards, Widget, Fight.Id)
                    .Single(value => value.ActionDefinitionId.Equals(attack.Id));
                Assert.That(Round.TryStartAttack(new ActionResolutionId("real-fight"), offer).Accepted, Is.True);
                var target = Round.CurrentTurnAuthority.PendingAttackTargetChoice;
                Assert.That(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Attacked)).Accepted, Is.True);
                var defense = Round.CurrentTurnAuthority.PendingDefenseChoice;
                if (defense != null)
                {
                    Assert.That(Round.TryAnswerDefenseResponse(
                        DefenseResponseChoiceCommand.Decline(defense.ResolutionId, defense.ChoiceId)).Accepted, Is.True);
                }
            }

            internal void PickBorrowedBreath()
            {
                var select = Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.CardInstance &&
                    invocation.Candidate.Value.CardInstanceId.Equals(Breath.Id));
                var id = Choice.ChoiceId;
                Answer(select);
                if (Choice != null && Choice.ChoiceId.Equals(id) && Choice.ConfirmInvocation != null)
                {
                    Answer(Choice.ConfirmInvocation);
                }
            }

            internal IReadOnlyList<CardInstanceId> OfferedCards() => Choice == null
                ? Array.Empty<CardInstanceId>()
                : Choice.Candidates.Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                    .Select(candidate => candidate.Value.CardInstanceId).ToArray();

            internal IReadOnlyList<HeroId> OfferedHeroes() => Choice == null
                ? Array.Empty<HeroId>()
                : Choice.Candidates.Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.Value.HeroId).ToArray();

            internal IReadOnlyList<CardInstance> HandOf(HeroId hero) =>
                cards.Where(card => card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand).ToArray();

            private void Answer(EffectChoiceCommand command)
            {
                var result = Round.TryAnswerEffectChoice(command);
                Assert.That(result.Accepted, Is.True, result.RejectionReason + "/" + result.TurnRejectionReason);
            }
        }

        private sealed class AttackFixture
        {
            internal static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            internal readonly CardDefinition[] Definitions;
            internal readonly MatchContentCatalogs Catalogs;
            internal CardInstance[] Cards;
            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;
            internal CardInstance Source => Cards.Single(c => c.Id.Value == "attack-source");
            internal CardInstance Borrowed => Cards.Single(c => c.Id.Value == "borrowed");
            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority?.PendingEffectChoice;
            internal readonly List<EffectChoiceCommand> Commands = new List<EffectChoiceCommand>();
            internal readonly List<string> Snapshots = new List<string>();
            internal readonly List<RuleEventLogEntry[]> EventPrefixes = new List<RuleEventLogEntry[]>();
            private readonly RuleEventLogProjection log = new RuleEventLogProjection();
            internal AttackFixture(bool tokenOnly = false, bool absentSkill = false, bool pyro = true, bool nested = false,
                RuleActivity immunity = RuleActivity.None)
            {
                var id = new CardDefinitionId("widget-fight-probe");
                var effect = WidgetEffectContent.FightAsOne(id, "rev-1");
                var attack = new ActionDefinition(new ActionDefinitionId("fight"), ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.NonRanged, default,
                    effect, cardTextTiming: CardTextTiming.AfterTheAttack);
                var source = new CardDefinition(id, "rev-1", "TEST - Fight as One", new InitiativeValue(11),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Gold), new[] { attack });
                var skillId = new CardDefinitionId("widget-borrow-probe");
                var skillEffect = tokenOnly ? WidgetEffectContent.DragonBond(skillId, "rev-1") : WidgetEffectContent.Breath(skillId, "rev-1", false);
                var skill = new CardDefinition(skillId, "rev-1", "TEST - Borrowed Skill", new InitiativeValue(4),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[]
                    {
                        new ActionDefinition(Skill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(tokenOnly ? PrintedValueChannel.Radius : PrintedValueChannel.Range, tokenOnly ? 2 : 3) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.None, default, skillEffect)
                    });
                var defense = GarrusCardContent.Definition(GarrusCardContent.SendFlying);
                var wrapperEffect = new EffectDefinition(new EffectDefinitionId("nested-skill-probe"), "rev-1", new EffectNodeId("root"), new[]
                {
                    new EffectNodeDefinition(new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { new EffectNodeId("borrow"), new EffectNodeId("parent-target") }),
                    new EffectNodeDefinition(new EffectNodeId("borrow"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1, inputs: new[]
                        { EffectInputBinding.FromClosedVocabulary(SyntheticBorrowedActionCapabilities.PoolParameter, BorrowedActionPool.OwnedResolvedSkills),
                            EffectInputBinding.FromClosedVocabulary(SyntheticBorrowedActionCapabilities.ColorParameter, CardColor.None) },
                        outputs: new[] { new EffectOutputBinding(SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValueKind.CardInstance) }),
                    new EffectNodeDefinition(new EffectNodeId("parent-target"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2, inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                            EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(3)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                        }, outputs: new[] { new EffectOutputBinding("chosenHero", EffectValueKind.Hero) })
                });
                var wrapper = new CardDefinition(new CardDefinitionId("nested-skill-card"), "rev-1", "TEST - Borrow parent", new InitiativeValue(3),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[]
                    { new ActionDefinition(Skill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary, Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.None, default, wrapperEffect) });
                var ward = new CardDefinition(new CardDefinitionId("immunity-probe"), "rev-1", "TEST - immunity", default,
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(),
                    UltimateRuleDefinition.PermanentAbility("immunity-probe", "ImmunityProbe",
                        new[] { RuleModification.ForImmunity(immunity == RuleActivity.None ? RuleActivity.Skill : immunity) }));
                Definitions = new[] { source, skill, defense, wrapper, ward };
                var defaults = MatchContentCatalogs.ForThisBuild();
                Catalogs = new MatchContentCatalogs(new EffectCapabilityCatalog(defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)
                    .Concat(WidgetComponentContent.DeferredHandlers())),
                    new ComponentDefinitionCatalog(WidgetComponentContent.Tokens(), Array.Empty<MarkerDefinition>()));
                Assert.That(EffectDefinitionValidator.Validate(effect, Catalogs.Capabilities), Is.Empty);
                Assert.That(EffectDefinitionValidator.Validate(wrapperEffect, Catalogs.Capabilities), Is.Empty);
                Cards = new[]
                {
                    CardInstance.CreateInHand(new CardInstanceId("attack-source"), source.Reference, Actor),
                    new CardInstance(new CardInstanceId("borrowed"), nested ? wrapper.Reference : skill.Reference, Actor,
                        new CardStateFacts(absentSkill ? CardLocation.DiscardPile : CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp)),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-source"), skill.Reference, Enemy),
                    CardInstance.CreateInHand(new CardInstanceId("other-source"), skill.Reference, Other),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-defense"), defense.Reference, Enemy),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-hand"), skill.Reference, Enemy),
                    CardInstance.CreateInHand(new CardInstanceId("other-hand"), skill.Reference, Other)
                }.Concat(nested ? new[] { new CardInstance(new CardInstanceId("inner"), skill.Reference, Actor,
                    new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp)) } : Array.Empty<CardInstance>()).ToArray();
                if (immunity != RuleActivity.None)
                {
                    var wardCard = CardInstance.CreateInDeck(new CardInstanceId("ward"), ward.Reference, Other);
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(wardCard), Is.True);
                    Cards = Cards.Concat(new[] { wardCard }).ToArray();
                }
                Board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(new MapId("widget-fight-map"), 1,
                    Enumerable.Range(-5, 11).SelectMany(q => Enumerable.Range(-5, 11).Select(r => At(q, r)))),
                    new[] { new HeroOccupancy(Actor, At(0)), new HeroOccupancy(Enemy, At(1)), new HeroOccupancy(Other, At(1, -1)) },
                    Array.Empty<ObstacleOccupancy>());
                if (pyro) Board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("pyro"), WidgetComponentContent.PyroDefinition,
                    Actor, Actor, Borrowed.Id, At(1, 1)));
                Round = new RoundResolutionAuthority(Definitions, Cards, StagedBoardParticipants.CreateWithBlueAlly(), new TieBreakerState(TeamSide.Red),
                    1, RoundResolutionComposition.ForCardActionsOnly(new CardActionResolutionAuthority(Catalogs, Board)));
                Assert.That(Round.TryCommitCard(Actor, Source.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Enemy, Cards[2].Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Other, Cards[3].Id).Accepted, Is.True);
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(Definitions, Cards, Actor, Source.Id).Single(o => o.ActionDefinitionId.Equals(attack.Id));
                Assert.That(Round.TryStartAttack(new ActionResolutionId("widget-fight"), offer).Accepted, Is.True);
                var choice = Round.CurrentTurnAuthority.PendingAttackTargetChoice;
                Assert.That(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, Enemy)).Accepted, Is.True);
            }
            internal void Defend()
            {
                var choice = Round.CurrentTurnAuthority.PendingDefenseChoice;
                Assert.That(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(choice.ResolutionId, choice.ChoiceId,
                    new[] { Cards.Single(c => c.Id.Value == "enemy-defense").Id })).Accepted, Is.True);
            }
            internal void Pick(EffectValue value) => Select(Choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(value)));
            internal void Option(string id) => Select(Choice.SelectInvocations.Single(c => c.Candidate.OptionNodeId.Value == id));
            private void Select(EffectChoiceCommand command)
            {
                var id = Choice.ChoiceId;
                Answer(command);
                // Milestone 8.46b: the completion is asked for rather than assumed - a choice admitting
                // exactly one selection is closed by the select itself, and the same node asking again
                // is a fresh question rather than this one still open.
                if (Choice != null && Choice.ChoiceId.Equals(id) && Choice.ConfirmInvocation != null) Answer(Choice.ConfirmInvocation);
            }
            internal void Answer(EffectChoiceCommand command)
            {
                var result = Round.TryAnswerEffectChoice(command);
                Assert.That(result.Accepted, Is.True, result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                Commands.Add(command);
                Snapshots.Add(AuthoritativeSnapshotSerializer.Serialize(Round, Board));
                log.Append(result.Events);
                EventPrefixes.Add(log.Entries.ToArray());
            }
            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(Catalogs, bytes, Definitions,
                    new[] { Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(bytes));
                Round = restored.Round; Board = restored.Board; Cards = restored.Cards.ToArray();
            }
        }

        private sealed class Fixture
        {
            internal readonly CardDefinition[] Definitions;
            internal CardInstance[] Cards;
            internal readonly MatchContentCatalogs Catalogs;
            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;
            internal CardInstance Source => Cards.Single(c => c.Id.Value == "source");
            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority?.PendingEffectChoice;

            internal Fixture(bool start = true, bool unlocked = true, bool foreign = false)
            {
                var card = new CardDefinitionId("widget-reuse-source");
                var effect = WidgetEffectContent.DragonBond(card, "rev-1");
                var source = new CardDefinition(card, "rev-1", "TEST - Skill with Movement", new InitiativeValue(8),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[]
                    {
                        new ActionDefinition(Skill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 2) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.None, default, effect),
                        new ActionDefinition(Movement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, true, true), AttackRangeKind.None, default)
                    });
                var ultimateId = new CardDefinitionId("widget-knight-fixture");
                var trigger = WidgetEffectContent.DragonKnight(ultimateId, "rev-1");
                var ultimate = new CardDefinition(ultimateId, "rev-1", "TEST - Dragon Knight", default,
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(),
                    UltimateRuleDefinition.TriggeredAbility("dragon-knight-test", "WidgetMovementSkill", trigger.Reference,
                        UltimateTriggerCondition.AfterControllerPerformsMovementAction, new[] { TimingCheckpoint.ActionEnded },
                        Array.Empty<PrintedValueDefinition>()));
                Definitions = new[] { source, ultimate };
                var ult = CardInstance.CreateInDeck(new CardInstanceId("ultimate"), ultimate.Reference, foreign ? Enemy : Actor);
                if (unlocked) Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ult), Is.True);
                Cards = new[] { CardInstance.CreateInHand(new CardInstanceId("source"), source.Reference, Actor),
                    CardInstance.CreateInHand(new CardInstanceId("enemy"), source.Reference, Enemy), ult };
                var defaults = MatchContentCatalogs.ForThisBuild();
                var caps = new EffectCapabilityCatalog(defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)
                    .Concat(WidgetComponentContent.DeferredHandlers()));
                Catalogs = new MatchContentCatalogs(caps,
                    new ComponentDefinitionCatalog(WidgetComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(defaults.TriggeredEffects.Definitions.Concat(new[] { trigger })));
                Assert.That(EffectDefinitionValidator.Validate(trigger, caps), Is.Empty);
                Board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(new MapId("widget-reuse-map"), 1,
                    Enumerable.Range(-5, 11).SelectMany(q => Enumerable.Range(-5, 11).Select(r => At(q, r)))),
                    new[] { new HeroOccupancy(Actor, At(0)), new HeroOccupancy(Enemy, At(-4)) }, Array.Empty<ObstacleOccupancy>());
                Round = new RoundResolutionAuthority(Definitions, Cards, StagedBoardParticipants.Create(), new TieBreakerState(TeamSide.Red),
                    1, RoundResolutionComposition.ForCardActionsOnly(new CardActionResolutionAuthority(Catalogs, Board)));
                if (!start) return;
                Assert.That(Round.TryCommitCard(Actor, Source.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Enemy, Cards[1].Id).Accepted, Is.True);
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(Definitions, Cards, Actor, Source.Id)
                    .Single(o => o.ActionDefinitionId.Equals(Movement));
                Assert.That(Round.TryStartMovement(new ActionResolutionId("widget-movement"), offer).Accepted, Is.True);
            }

            internal void Move(int distance)
            {
                var choice = Round.CurrentTurnAuthority.PendingMovementChoice;
                var command = distance == 0 ? MovementChoiceCommand.Stop(choice.ResolutionId, choice.ChoiceId) :
                    MovementChoiceCommand.SelectDestination(choice.ResolutionId, choice.ChoiceId, At(distance));
                Assert.That(Round.TryAnswerMovementChoice(command).Accepted, Is.True);
                choice = Round.CurrentTurnAuthority.PendingMovementChoice;
                if (choice != null) Assert.That(Round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(choice.ResolutionId, choice.ChoiceId)).Accepted, Is.True);
            }
            internal void Pick(EffectValue value) => Select(Choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(value)));
            internal void Option(string id) => Select(Choice.SelectInvocations.Single(c => c.Candidate.OptionNodeId.Value == id));
            private void Select(EffectChoiceCommand command)
            {
                var id = Choice.ChoiceId;
                Assert.That(Round.TryAnswerEffectChoice(command).Accepted, Is.True);
                // Milestone 8.46b: the completion is asked for rather than assumed - a choice admitting
                // exactly one selection is closed by the select itself.
                if (Choice != null && Choice.ChoiceId.Equals(id) && Choice.ConfirmInvocation != null) Assert.That(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation).Accepted, Is.True);
            }
            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(Catalogs, bytes, Definitions,
                    new[] { Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(bytes));
                Round = restored.Round; Board = restored.Board; Cards = restored.Cards.ToArray();
            }
        }
    }
}
