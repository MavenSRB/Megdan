using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.9: Wasp's two basic cards resolve their complete printed text in the configured
    /// mirror, and every action performed on a basic card records rulebook p.9's supertype.
    ///
    /// <para>Magnetic Dagger - "Target a unit adjacent to you. After the attack: <b>This turn:</b>
    /// Enemy units in radius cannot be swapped or placed by themselves, or by enemy heroes." Its Attack
    /// is p.13's ordinary non-Ranged one and nothing about it is declared; what this milestone composed
    /// is the rider, which hangs on milestone 8.5's card-text seam at p.14's step 7 and creates the
    /// Active effect that carries the restriction.</para>
    ///
    /// <para>Static Barrier - "<b>This turn:</b> While an enemy hero outside of radius is performing an
    /// action, spaces in radius count as obstacles. While an enemy hero in radius is performing an
    /// action, spaces outside of radius count as obstacles." Its Basic Skill creates the conditional
    /// obstacle overlay milestone 8.7 proved with no numbers in it.</para>
    ///
    /// <para>The general contracts both texts run on were built and proved elsewhere -
    /// <c>RuleModificationTests</c> owns the actor-qualified restriction and the overlay's arms,
    /// <c>BoardObjectExchangeTests</c> and the placement fixtures own the Swap and Place consumers.
    /// What is here is Wasp: the compositions, her numbers, and both cards run through a real
    /// <see cref="MatchSession"/>.</para>
    /// </summary>
    public sealed class ConfiguredBasicCardTests
    {
        // ---------------------------------------------------------------------------------------
        // The compositions themselves
        // ---------------------------------------------------------------------------------------

        [Test]
        public void MagneticDagger_HangsOneActiveEffectNodeOnThePageSevenAttackTextMoment()
        {
            var attack = PrimaryOf(WaspCardContent.MagneticDagger, WaspCardContent.PrimaryAttack);

            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack),
                "The face prints \"After the attack:\", which p.14 numbers step 7.");
            Assert.That(attack.AttackTargetPolicy.Value.NarrowsTheDefault(AttackRangeKind.NonRanged),
                Is.False,
                "\"Target a unit adjacent to you\" is p.13's own sentence for a non-Ranged Attack, so " +
                "the card narrows nothing and its targeting is the general contract's.");
            Assert.That(
                attack.BaseValues.Select(value => $"{value.Channel}={value.Amount}"),
                Is.EqualTo(new[] { "Attack=3", "Radius=3" }),
                "and the printed values are the face's, unchanged by the composition.");

            Assert.That(attack.EffectRoot.Nodes.Count, Is.EqualTo(1),
                "Everything the sentence says is definition content on the Active effect, so an " +
                "effect tree with branches would be stating the same rule twice.");
            Assert.That(
                attack.EffectRoot.Nodes.Single().Capability,
                Is.EqualTo(WaspActiveEffectContent.PinEnemyUnitsInRadiusV1));
            Assert.That(
                attack.EffectRoot.Nodes.Single().FailurePolicy,
                Is.EqualTo(EffectFailurePolicy.Mandatory),
                "Printed text is not optional, and creating an Active effect cannot fail.");
        }

        [Test]
        public void MagneticDaggersEffect_DeclaresBothRepositioningsAgainstBothNamedActors()
        {
            var definition = WaspActiveEffectContent.PinnedEnemyUnitsDefinition;

            Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn),
                "p.17's printed keyword.");
            Assert.That(definition.AffectedScope,
                Is.EqualTo(ActiveEffectAffectedScopeKind.UnitsInRadius));
            Assert.That(definition.AffectedUnitRelation,
                Is.EqualTo(SourceTeamRelation.OpposingSource), "\"Enemy units\", read from Wasp's side.");
            Assert.That(definition.Radius, Is.EqualTo(DeclaredRadius.PrintedIcon(3)),
                "p.9: card text that mentions Radius refers to the printed icon, which this card " +
                "prints as 3.");
            Assert.That(definition.CreationCheckpoint, Is.EqualTo(TimingCheckpoint.AfterAttack));
            Assert.That(definition.ActivationCheckpoint, Is.EqualTo(TimingCheckpoint.AfterAttack),
                "Creation and activation are the same moment, so the lifecycle activates it on the " +
                "spot rather than leaving it dormant for a checkpoint that has already passed.");

            Assert.That(
                definition.Modifications.Select(modification =>
                    $"{modification.Activity}:{string.Join("+", modification.RestrictedActors.Relations)}"),
                Is.EqualTo(new[]
                {
                    "Swap:AffectedObjectItself+HeroesOpposingSource",
                    "Place:AffectedObjectItself+HeroesOpposingSource"
                }),
                "\"cannot be swapped or placed\" is two effect families - p.19's Swap and p.18's " +
                "Place - and \"by themselves, or by enemy heroes\" is the actor scope on each.");
            Assert.That(definition.ObstacleOverlays, Is.Empty);
        }

        [Test]
        public void StaticBarriersSkill_DeclaresOneOverlayWithBothPrintedArms()
        {
            var skill = PrimaryOf(WaspCardContent.StaticBarrier, WaspCardContent.PrimarySkill);
            var definition = WaspActiveEffectContent.StaticBarrierDefinition;

            Assert.That(skill.CardTextTiming, Is.EqualTo(CardTextTiming.None),
                "p.12: a Skill's effect is the action, so it names none of p.14's Attack moments.");
            Assert.That(
                skill.EffectRoot.Nodes.Single().Capability,
                Is.EqualTo(WaspActiveEffectContent.CloseBarrierInRadiusV1));

            Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn));
            Assert.That(definition.Radius, Is.EqualTo(DeclaredRadius.PrintedIcon(2)),
                "the printed icon on this face.");
            Assert.That(definition.Modifications, Is.Empty,
                "The card changes what counts as an obstacle, which names no object at all.");

            var overlay = definition.ObstacleOverlays.Single();
            Assert.That(overlay.ActingHeroRelation, Is.EqualTo(SourceTeamRelation.OpposingSource),
                "\"an enemy hero\" - Wasp and her teammates walk through their own barrier.");
            Assert.That(
                overlay.Arms,
                Is.EqualTo(new[]
                {
                    ObstacleOverlayArm.SpacesInsideWhileActorOutside,
                    ObstacleOverlayArm.SpacesOutsideWhileActorInside
                }),
                "Two printed sentences, one declaration: they are two halves of one boundary rather " +
                "than two rules, which is what makes the side an acting hero stands on stable for the " +
                "whole of its own action.");
        }

        // ---------------------------------------------------------------------------------------
        // Magnetic Dagger in the configured mirror
        // ---------------------------------------------------------------------------------------

        [Test]
        public void MagneticDagger_RunsItsAttackThenPinsTheEnemyUnitsLeftInsideItsRadius()
        {
            var session = ConfiguredMatchFixtures.CreateMagneticDaggerSession(out var minionId);
            var red = HeroOn(session, TeamSide.Red);
            var blue = HeroOn(session, TeamSide.Blue);
            CommitAgainstStopProjectiles(session, WaspCardContent.MagneticDagger);

            var attack = AttackOffer(session);
            Assert.That(attack.IsAvailable, Is.True,
                "A minion stands beside Wasp and the card owes no composition.");
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            ChooseMinionTarget(session, minionId);

            // p.13's minion arm: no Defense response, "it is simply defeated", so one command carries
            // the attack through p.14's step 7 and the card text lands inside it.
            Assert.That(session.Projection.PendingAttackResolution, Is.Null);
            Assert.That(
                session.Projection.Minions.Any(minion => minion.MinionId.Equals(minionId)),
                Is.False,
                "The target was removed before the After-the-attack text ran, which is what p.14 step " +
                "7 means by \"after the target either defends, or is defeated and removed\".");

            var pinned = Modifications(session);
            Assert.That(
                pinned.Modifications.Select(applied =>
                    $"{applied.AffectedObject}:{applied.Modification.Activity}"),
                Is.EqualTo(new[]
                {
                    $"Hero {blue.Value}:Swap",
                    $"Hero {blue.Value}:Place"
                }),
                "The disk holds one enemy unit - the Blue hero two spaces away - and it is pinned " +
                "against both repositionings. The minion the Attack removed is not in it, and Wasp " +
                "herself is on the wrong side of \"enemy\".");

            var participants = session.Projection.Participants;
            var pinnedUnit = RuleObject.ForHero(blue);
            foreach (var activity in new[] { RuleActivity.Swap, RuleActivity.Place })
            {
                Assert.That(
                    RestrictionQuery.IsRestricted(
                        pinnedUnit, activity, pinnedUnit, participants, pinned),
                    Is.True,
                    "\"by themselves\" - the pinned unit acting on itself.");
                Assert.That(
                    RestrictionQuery.IsRestricted(
                        pinnedUnit, activity, RuleObject.ForHero(red), participants, pinned),
                    Is.False,
                    "Wasp's own side is unrestricted, which is the half of the sentence a blanket " +
                    "prohibition would lose.");
                Assert.That(
                    RestrictionQuery.IsRestricted(
                        RuleObject.ForHero(red), activity, RuleObject.ForHero(blue), participants,
                        pinned),
                    Is.False,
                    "and the restriction lands on the enemy units rather than on the hero who played " +
                    "the card.");
            }

            Assert.That(
                RestrictionQuery.IsRestricted(
                    pinnedUnit, RuleActivity.Movement, pinnedUnit, participants, pinned),
                Is.False,
                "The card names two effect families and says nothing about a hero moving itself.");
        }

        [Test]
        public void ThePinnedSet_IsWhoeverStandsInsideTheDiskWhenTheRuleIsAsked()
        {
            var session = ConfiguredMatchFixtures.CreateMagneticDaggerSession(out var minionId);
            var red = HeroOn(session, TeamSide.Red);
            var blue = HeroOn(session, TeamSide.Blue);
            AttackTheAdjacentMinion(session, minionId);

            var restored = Restore(session);
            var anchor = Space(restored.Board, red);
            Assert.That(
                HexDistanceQuery.Calculate(anchor, Space(restored.Board, blue)), Is.EqualTo(2));
            Assert.That(Pinned(restored, blue), Is.True);

            // p.9's disk is inclusive to its edge and counts spaces through obstacles, so a unit at
            // exactly the printed 3 is inside and one at 4 is out. Nothing is stored, so walking the
            // hero out of the disk takes it out of the rule with no effect being changed.
            Move(restored.Board, blue, SpaceAt(restored.Board, anchor, 3));
            Assert.That(Pinned(restored, blue), Is.True, "Radius 3 includes its outermost ring.");
            Move(restored.Board, blue, SpaceAt(restored.Board, anchor, 4));
            Assert.That(Pinned(restored, blue), Is.False, "and a unit one space beyond it is out.");

            // p.17: an Active effect's Radius moves with its hero. The disk is re-anchored on the next
            // ask rather than updated, so moving Wasp towards the unit catches it again.
            Move(restored.Board, red, SpaceAt(restored.Board, Space(restored.Board, blue), 2));
            Assert.That(Pinned(restored, blue), Is.True,
                "The centre moved, and the same unit standing in the same space is inside again.");
        }

        [Test]
        public void ThePinnedDisk_IsThePrintedIconAndPageSixteensItemRaisesIt()
        {
            var session = ConfiguredMatchFixtures.CreateMagneticDaggerSession(out var minionId);
            var red = HeroOn(session, TeamSide.Red);
            var blue = HeroOn(session, TeamSide.Blue);
            AttackTheAdjacentMinion(session, minionId);

            var restored = Restore(session);
            var anchor = Space(restored.Board, red);
            Move(restored.Board, blue, SpaceAt(restored.Board, anchor, 4));
            Assert.That(Pinned(restored, blue), Is.False,
                "Four spaces is outside the printed Radius 3, or the case below proves nothing.");

            // p.16: "Each item increases the value of the matching icon on each of your Action cards by
            // +1", and p.9 says the card text that mentions Radius refers to that icon. So the disk an
            // Active effect draws is raised by the same item that raises the number the card was played
            // with - the distinction milestone 7.2b exists to make representable, on the other side of
            // an action boundary.
            var items = OneRadiusItem(
                restored, red, restored.Round.Timing.Effects.Single().SourceCardInstanceId);
            Assert.That(
                Pinned(restored, blue, items), Is.True,
                "A +1 Radius item widens the pinning field to 4, exactly as it widens the icon the " +
                "card printed.");
        }

        [Test]
        public void ThePinnedField_SurvivesSnapshotAndReplayAndEndsWithTheTurn()
        {
            var session = ConfiguredMatchFixtures.CreateMagneticDaggerSession(out var minionId);
            var blue = HeroOn(session, TeamSide.Blue);
            AttackTheAdjacentMinion(session, minionId);

            var snapshot = session.CaptureSnapshot();
            Assert.That(Pinned(Restore(session), blue), Is.True,
                "The restored match rebinds the instance to the catalog definition and produces the " +
                "same rule again; no payload restates what the effect does.");

            var target = ConfiguredMatchFixtures.CreateMagneticDaggerSession(out _);
            Assert.That(target.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(target.CaptureSnapshot(), Is.EqualTo(snapshot),
                "and a match restored from it captures the same payload byte for byte.");
            Assert.That(Pinned(Restore(target), blue), Is.True,
                "carrying the pinning field with it.");

            ConfiguredMatchFixtures.PlayOutTheRestOfTheRound(session);
            Assert.That(
                Restore(session).Round.Timing.Effects.All(effect =>
                    effect.Phase == ActiveEffectPhase.Ended),
                Is.True,
                "p.17's \"This turn\" ends at the turn's own cancellation checkpoint.");
        }

        // ---------------------------------------------------------------------------------------
        // Static Barrier in the configured mirror
        // ---------------------------------------------------------------------------------------

        [Test]
        public void StaticBarrier_ClosesBothSidesOfItsDiskToAnEnemyHeroAndNeitherToItsOwn()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var red = HeroOn(session, TeamSide.Red);
            var blue = HeroOn(session, TeamSide.Blue);
            CommitAgainstStopProjectiles(session, WaspCardContent.StaticBarrier);
            var skill = SkillOffer(session);
            Assert.That(skill.IsAvailable, Is.True);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(skill.ActionDefinitionId));

            var restored = Restore(session);
            var anchor = Space(restored.Board, red);

            // Four empty spaces, two on each side of the printed Radius 2, chosen before anything
            // moves: the two the arms are asked about have to stay empty, because p.18 already makes a
            // space holding a unit an obstacle and a probe standing on its own answer proves nothing.
            var insideEdge = SpaceAt(restored.Board, anchor, 2);
            var farOutside = SpaceAt(restored.Board, anchor, 4);
            var standInside = SpaceAt(restored.Board, anchor, 1);
            var standOutside = SpaceAt(restored.Board, anchor, 3);

            // The two printed sentences, each asked with the hero its condition names standing where
            // that sentence puts it. p.18 lists obstacles once, so this is the same classification
            // movement, a push and a Place all read.
            Move(restored.Board, blue, standOutside);
            Assert.That(IsObstacle(restored, insideEdge, blue), Is.True,
                "\"While an enemy hero outside of radius is performing an action, spaces in radius " +
                "count as obstacles.\"");
            Assert.That(IsObstacle(restored, farOutside, blue), Is.False,
                "and only one arm's condition is met at a time.");

            Move(restored.Board, blue, standInside);
            Assert.That(IsObstacle(restored, farOutside, blue), Is.True,
                "\"While an enemy hero in radius is performing an action, spaces outside of radius " +
                "count as obstacles.\"");
            Assert.That(IsObstacle(restored, insideEdge, blue), Is.False,
                "and the disk it is standing in stays open to it, which is the same one arm at a time " +
                "read from the other side.");

            Assert.That(IsObstacle(restored, insideEdge, red), Is.False,
                "Wasp's own side acts through her barrier untouched.");
            Assert.That(IsObstacle(restored, farOutside, red), Is.False);
            Assert.That(
                ObstacleQuery.IsObstacle(
                    restored.Board,
                    insideEdge,
                    Modifications(restored, RuleObject.None, null)),
                Is.False,
                "and a moment with nobody performing an action has nobody for the condition to hold " +
                "of - p.18's own after-action correction asks exactly this way.");
        }

        [Test]
        public void StaticBarriersDisk_IsThePrintedIconAndPageSixteensItemRaisesIt()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var red = HeroOn(session, TeamSide.Red);
            var blue = HeroOn(session, TeamSide.Blue);
            CommitAgainstStopProjectiles(session, WaspCardContent.StaticBarrier);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(SkillOffer(session).ActionDefinitionId));

            var restored = Restore(session);
            var anchor = Space(restored.Board, red);
            var justOutside = SpaceAt(restored.Board, anchor, 3);
            Move(restored.Board, blue, SpaceAt(restored.Board, anchor, 5));

            Assert.That(IsObstacle(restored, justOutside, blue), Is.False,
                "Three spaces is outside the printed Radius 2, or the case below proves nothing.");

            var items = OneRadiusItem(
                restored, red, restored.Round.Timing.Effects.Single().SourceCardInstanceId);
            Assert.That(
                ObstacleQuery.IsObstacle(
                    restored.Board, justOutside, Modifications(restored, RuleObject.ForHero(blue), items)),
                Is.True,
                "A +1 Radius item widens the barrier to 3, which is the same p.9/p.16 reading the " +
                "pinning field takes.");
        }

        // ---------------------------------------------------------------------------------------
        // Rulebook p.9's supertype
        // ---------------------------------------------------------------------------------------

        [Test]
        public void PerformingStaticBarriersPrimary_RecordsTheOneBasicSkillThisSetupCanRecord()
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            var red = HeroOn(session, TeamSide.Red);
            var card = HandCard(session, TeamSide.Red, WaspCardContent.StaticBarrier);
            CommitAgainstStopProjectiles(session, WaspCardContent.StaticBarrier);

            var result = session.Execute(
                DirectorCommand.StartAction(SkillOffer(session).ActionDefinitionId));
            Assert.That(result.Accepted, Is.True);

            var recorded = BasicActions(result);
            Assert.That(recorded, Has.Length.EqualTo(1));
            Assert.That(recorded.Single().Kind, Is.EqualTo(ActionKind.Skill),
                "Static Barrier's primary is Wasp's only basic skill action, which is the one trigger " +
                "High Voltage's continuous ability can ever read.");
            Assert.That(recorded.Single().ActorId, Is.EqualTo(red));
            Assert.That(recorded.Single().SourceCardInstanceId, Is.EqualTo(card.Id));
            Assert.That(
                recorded.Single().ActionDefinitionId, Is.EqualTo(WaspCardContent.PrimarySkill));
        }

        [Test]
        public void EveryActionOnABasicCardIsBasic_AndNoActionOnAnUpgradedOneIs()
        {
            // p.9 states the supertype and its consequence together: only the Gold and Silver cards are
            // basic, and every action performed on a basic card is a basic action. The discriminating
            // pair is therefore one action kind performed on two cards: p.7's Hold, which every card in
            // the hand offers and none prints an icon for, spent once from a Gold basic card and once
            // from a Tier I one. Same action, opposite answers, and the only difference is the card.
            var basic = HoldOne(WaspCardContent.MagneticDagger, out var basicCardId);
            Assert.That(basic.Single().Kind, Is.EqualTo(ActionKind.Hold),
                "p.7's Hold is an action a hero performs, and this one was performed on a basic card.");
            Assert.That(basic.Single().SourceCardInstanceId, Is.EqualTo(basicCardId));

            Assert.That(HoldOne(WaspCardContent.LiftUp, out _), Is.Empty,
                "Lift Up is a Tier I card, so nothing performed on it is basic - the classification " +
                "is the card's and never the action's.");
        }

        /// <summary>Red commits the named card and Holds it, in a mirror of its own.</summary>
        private static PerformedBasicAction[] HoldOne(
            CardDefinitionId cardId, out CardInstanceId heldCardId)
        {
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorSession();
            heldCardId = HandCard(session, TeamSide.Red, cardId).Id;
            CommitAgainstStopProjectiles(session, cardId);
            var result = session.Execute(DirectorCommand.PerformHold(heldCardId));
            Assert.That(result.Accepted, Is.True);
            return BasicActions(result);
        }

        [Test]
        public void TheCompletionCheckpoint_CannotBeReachedWithoutNamingTheActionItCompletes()
        {
            // The structural half of "every action on a basic card records the fact". The plain
            // checkpoint factory refuses this one checkpoint, so a resolver cannot end an action
            // without handing over what p.9's supertype is decided from, and a new action family
            // inherits the rule rather than having to remember it.
            Assert.That(
                () => ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionEnded),
                Throws.ArgumentException);

            var step = ResolutionStep.AtActionEnded(
                new CardInstanceId("card"),
                new ActionDefinitionId("action"),
                new HeroId("hero"),
                ActionKind.Skill);
            Assert.That(step.Checkpoint, Is.EqualTo(TimingCheckpoint.ActionEnded));
            Assert.That(step.CompletedActionKind, Is.EqualTo(ActionKind.Skill));
            Assert.That(
                () => ResolutionStep.AtActionEnded(
                    new CardInstanceId("card"),
                    new ActionDefinitionId("action"),
                    new HeroId("hero"),
                    ActionKind.None),
                Throws.ArgumentException,
                "and the kind is required, because \"a basic skill\" narrows by it.");
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static ActionDefinition PrimaryOf(CardDefinitionId cardId, ActionDefinitionId actionId) =>
            WaspCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Id.Equals(actionId));

        private static PerformedBasicAction[] BasicActions(MatchCommandResult result) =>
            result.Events
                .Select(roundEvent => roundEvent.TurnEvent)
                .Where(turnEvent => turnEvent.Kind == TurnResolutionEventKind.BasicActionPerformed)
                .Select(turnEvent => turnEvent.BasicAction)
                .ToArray();

        /// <summary>
        /// One p.16 item supplying the Radius icon, held by the named hero. Placed through the restore
        /// path rather than through p.15's Level Up, because what is under test is the composition an
        /// item already held produces - not how it was earned, which milestone 7.2a owns.
        /// </summary>
        private static HeroItemState OneRadiusItem(
            RestoredAuthoritativeMatch restored, HeroId ownerId, CardInstanceId itemCardId) =>
            HeroItemState.Restore(
                restored.Participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(
                        ownerId,
                        itemCardId,
                        HeroDashboard.RulebookLayout.Slots
                            .Single(slot => slot.Accepts(PrintedValueChannel.Radius)).Position,
                        PrintedValueChannel.Radius,
                        1)
                }));

        private static void AttackTheAdjacentMinion(MatchSession session, MinionId minionId)
        {
            CommitAgainstStopProjectiles(session, WaspCardContent.MagneticDagger);
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(AttackOffer(session).ActionDefinitionId));
            ChooseMinionTarget(session, minionId);
        }

        private static void ChooseMinionTarget(MatchSession session, MinionId minionId)
        {
            var choice = session.Projection.PendingAttackTargetChoice;
            Assert.That(choice.LegalTargetMinionIds, Does.Contain(minionId),
                "The minion the fixture placed beside Wasp has to be a legal target of the printed " +
                "Attack, or the case holds over nothing.");
            ConfiguredMatchFixtures.Accept(
                session,
                DirectorCommand.ChooseAttackTarget(choice.ResolutionId, choice.ChoiceId, minionId));
        }

        /// <summary>
        /// Red commits the named card and Blue commits Stop Projectiles, whose printed Initiative 3 is
        /// the lowest in the starting hand, so the Red card acts first on its printed value alone.
        /// </summary>
        private static void CommitAgainstStopProjectiles(MatchSession session, CardDefinitionId cardId)
        {
            var red = HandCard(session, TeamSide.Red, cardId);
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The tested card has to be the one acting, or every assertion below is about Blue.");
        }

        private static ActionOffer AttackOffer(MatchSession session) => PrimaryOffer(session, ActionKind.Attack);

        private static ActionOffer SkillOffer(MatchSession session) => PrimaryOffer(session, ActionKind.Skill);

        private static ActionOffer PrimaryOffer(MatchSession session, ActionKind kind) =>
            session.Projection.AvailableTurnActions.Single(candidate =>
                Action(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary &&
                Action(session, candidate).Kind == kind);

        private static ActionDefinition Action(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static CardInstance HandCard(
            MatchSession session, TeamSide side, CardDefinitionId cardId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(cardId));

        private static HeroId HeroOn(MatchSession session, TeamSide side) =>
            session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side == side)
                .HeroId;

        private static HexCoordinate Space(AdjacentMoveAuthority board, HeroId heroId)
        {
            Assert.That(board.TryGetHeroCoordinate(heroId, out var coordinate), Is.True);
            return coordinate;
        }

        /// <summary>
        /// One empty playable space exactly <paramref name="distance"/> from <paramref name="anchor"/>,
        /// chosen in canonical order so two runs of the same state pick the same one.
        ///
        /// It is asked of the board rather than computed, because the official map is not a plane: a
        /// coordinate at the right distance can be terrain, off the play area, or occupied, and a
        /// fixture that moved a hero onto one would be testing the board's refusal instead of the card.
        /// </summary>
        private static HexCoordinate SpaceAt(
            AdjacentMoveAuthority board, HexCoordinate anchor, int distance)
        {
            var space = board.BoardCoordinates
                .Where(candidate =>
                    HexDistanceQuery.Calculate(anchor, candidate) == distance &&
                    board.IsEmptyPlayableSpace(candidate))
                .OrderBy(candidate => candidate.Q)
                .ThenBy(candidate => candidate.R)
                .ToArray();
            Assert.That(space, Is.Not.Empty,
                $"The official map has to offer a free playable space {distance} from {anchor}, or " +
                "this case proves nothing.");
            return space[0];
        }

        private static void Move(AdjacentMoveAuthority board, HeroId heroId, HexCoordinate destination) =>
            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(heroId), destination).Accepted,
                Is.True,
                $"The fixture needs {heroId.Value} standing on {destination}.");

        private static bool Pinned(
            RestoredAuthoritativeMatch restored, HeroId heroId, HeroItemState items = null) =>
            Modifications(restored, RuleObject.None, items)
                .IsRestricted(RuleObject.ForHero(heroId), RuleActivity.Swap);

        private static bool IsObstacle(
            RestoredAuthoritativeMatch restored, HexCoordinate space, HeroId actingHeroId) =>
            ObstacleQuery.IsObstacle(
                restored.Board, space, Modifications(restored, RuleObject.ForHero(actingHeroId), null));

        private static RuleModificationSet Modifications(MatchSession session) =>
            Modifications(Restore(session), RuleObject.None, null);

        /// <summary>
        /// The live set, read back through the authoritative snapshot rather than from a projection
        /// field: milestone 6.8 deliberately keeps rules out of the payload, so rebuilding them from a
        /// restored match is what proves the definition still supplies them.
        /// </summary>
        private static RuleModificationSet Modifications(
            RestoredAuthoritativeMatch restored, RuleObject actingObject, HeroItemState items) =>
            RuleModificationQuery.Build(
                restored.Round.Timing,
                restored.Board,
                restored.Participants,
                restored.Round.TieBreakerState,
                UnlockedUltimateSet.None,
                actingObject,
                items,
                null);

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
    }
}
