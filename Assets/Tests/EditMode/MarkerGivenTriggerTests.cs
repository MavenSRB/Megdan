using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40h. The moment Bain's A Complicated Profession watches - "After you give a hero the
    /// [bounty] bounty marker that hero discards a card, if able" - built as vocabulary before any of
    /// his content exists.
    ///
    /// <para><b>MILESTONE 8.40b MEASURED THAT NOTHING WATCHED A GIVE, AND THIS IS WHAT THAT COST.</b>
    /// No <see cref="TimingCheckpoint"/> member was a marker moment, and the give declared no
    /// checkpoint on its result, so the channel this milestone needs was empty at both ends. What it
    /// adds is one member of that enum, one member of
    /// <see cref="UltimateTriggerCondition"/>, and one typed report on the give's own execution result -
    /// and NOT a property on <see cref="UltimateRuleDefinition"/>, which milestone 8.38d measured as the
    /// one change that moves every dealt hero's corpus digest at once.</para>
    ///
    /// <para><b>THE THREE EXCLUSIONS ARE STRUCTURAL, AND THE CASES SAY WHICH GUARD MAKES EACH ONE.</b>
    /// A transfer and a return report no recipient, so the resolver never stops for them and no query is
    /// ever asked - two exclusions decided by which capability ran. A give another hero performed is
    /// decided by <see cref="AfterMarkerGivenTriggerQuery"/>, because the marker and the moment are
    /// identical and only the giver differs. BA-D5.</para>
    ///
    /// <para><b>The board is featureless and the content is synthetic.</b> Bain is not composed until
    /// 8.40i, so the watching Ultimate is the showcase hand's own after-marker-given shape and the give
    /// is the showcase component Skill milestone 6.2 already deals - which offers every hero as a
    /// recipient, so "a hero" can be shown to mean an enemy, an ally, and the giver himself.</para>
    /// </summary>
    public sealed class MarkerGivenTriggerTests
    {
        // -------------------------------------------------------------------------------------------
        // The declaration: where the moment lives, and what it cost
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheDeclaration_IsAConditionAndACheckpointRatherThanANewProperty()
        {
            var rule = UltimateRuleDefinition.AfterMarkerGivenAbility(
                "probe-rule",
                "ProbeAfterMarkerGiven",
                SyntheticShowcaseHand.AfterMarkerGivenTriggeredEffect.Reference,
                Array.Empty<PrintedValueDefinition>());

            Assert.That(rule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerGivesMarker));
            Assert.That(rule.TriggerCheckpoints,
                Has.Member(TimingCheckpoint.MarkerGivenToHero),
                "The moment is carried in the collection this type already publishes, so the " +
                "declaration adds no public property and no hero's digest moves.");
            Assert.That(rule.AfterPush.IsDeclared, Is.False);
            Assert.That(rule.BeforeAction.IsDeclared, Is.False);
            Assert.That(rule.MinionBattleRule, Is.Null);
            Assert.That(rule.GrantedTargetExclusion, Is.EqualTo(GrantedTargetExclusion.None),
                "and 8.38d's property is untouched, which is what makes the measurement in this " +
                "milestone's evidence a comparison rather than a claim.");
        }

        [Test]
        public void ADeclarationThatNamesTheWrongMoment_IsRefused()
        {
            Assert.That(
                () => UltimateRuleDefinition.TriggeredAbility(
                    "wrong-moment", "ProbeWrongMoment",
                    SyntheticShowcaseHand.AfterMarkerGivenTriggeredEffect.Reference,
                    UltimateTriggerCondition.AfterControllerGivesMarker,
                    new[] { TimingCheckpoint.ActionEnded },
                    Array.Empty<PrintedValueDefinition>()),
                Throws.ArgumentException,
                "A give is not an action ending, and a declaration pairing the two would be a " +
                "trigger that could never fire.");

            Assert.That(
                () => UltimateRuleDefinition.TriggeredAbility(
                    "wrong-condition", "ProbeWrongCondition",
                    SyntheticShowcaseHand.AfterMarkerGivenTriggeredEffect.Reference,
                    UltimateTriggerCondition.AfterControllerPerformsBasicAction,
                    new[] { TimingCheckpoint.MarkerGivenToHero },
                    Array.Empty<PrintedValueDefinition>()),
                Throws.ArgumentException,
                "and the mirror is refused too, so the pairing is a rule rather than a convention.");
        }

        [Test]
        public void ACompletedAction_NeverDerivesAMarkerGivenSource()
        {
            var fixture = new MarkerGiveFixture(unlockTheUltimate: true);

            var derived = UltimateTriggeredEffectSourceQuery.Derive(
                fixture.Ultimates,
                fixture.Board,
                fixture.Participants,
                PerformedBasicAction.Of(
                    fixture.Giver,
                    new CardInstanceId("probe-card"),
                    new ActionDefinitionId("probe-action"),
                    ActionKind.Skill));

            Assert.That(derived, Is.Empty,
                "The after-action rails and this moment are different channels: a Skill of the " +
                "watching hero's that gave nothing must reach nothing, and a source built here " +
                "would additionally be refused by ContinuousTriggeredEffectSource's own checkpoint " +
                "guard - which is the second guard keeping the two apart.");
        }

        // -------------------------------------------------------------------------------------------
        // The producer: which operations report a give, and how often
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AGive_ReportsTheHeroItGaveTo_AndATransferAndAReturnReportNothing()
        {
            var operations = new MarkerOperationFixture();

            var given = operations.Give(to: operations.Enemy);
            Assert.That(given.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(given.MarkerGivenTo, Is.EqualTo(operations.Enemy),
                "The capability that performed the give is what names the moment's subject; nothing " +
                "downstream asks a board that cannot say who put the marker there.");

            var transferred = operations.Transfer(to: operations.Actor);
            Assert.That(transferred.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(transferred.MarkerGivenTo.IsDefined, Is.False,
                "Close Call's transfer is another hero giving the marker to Bain - BA-D5 - so it " +
                "reports no give and the moment is never raised for it.");
            Assert.That(
                transferred.Outcomes.Any(fact => fact.OutputName == "holder"), Is.True,
                "and it publishes `holder` exactly as the give does, which is why the exclusion has " +
                "to be a separate channel rather than a reading of the published facts.");

            var returned = operations.ReturnAny();
            Assert.That(returned.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(returned.MarkerGivenTo.IsDefined, Is.False,
                "A marker returning to the supply is not a give.");
        }

        [Test]
        public void AGiveThatMovedAnAlreadyHeldMarker_IsStillAGive()
        {
            var operations = new MarkerOperationFixture();
            operations.Give(to: operations.Enemy);
            Assert.That(
                operations.AvailableBounties, Is.Zero,
                "The supply of one is spent, which is the configuration BA-D1's arm exists for.");

            var moved = operations.Give(
                to: operations.Ally, movesWhenSupplyIsEmpty: true, nodeSuffix: "move");

            Assert.That(moved.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(moved.MarkerGivenTo, Is.EqualTo(operations.Ally),
                "BA-D1's move is still a give the actor performed, so it fires the trigger - the " +
                "edge this block states rather than leaves to fall out.");
        }

        [Test]
        public void ReExecutingAGiveNodeInsideItsOwnResolution_ReportsNoSecondGive()
        {
            var operations = new MarkerOperationFixture();

            var first = operations.Give(to: operations.Enemy, nodeSuffix: "once");
            var again = operations.Give(to: operations.Enemy, nodeSuffix: "once");

            Assert.That(first.MarkerGivenTo, Is.EqualTo(operations.Enemy));
            Assert.That(again.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "Re-executing a node inside its own resolution is a legal no-op.");
            Assert.That(again.MarkerGivenTo.IsDefined, Is.False,
                "Once per GIVE and not once per execution: the marker did not change hands the " +
                "second time, so nothing watching the moment may fire a second time.");
            Assert.That(operations.Attachments, Has.Count.EqualTo(1));
        }

        [Test]
        public void OnlyAHandlerThatApplied_MayReportAGive()
        {
            Assert.That(
                () => EffectExecutionResult.Applied(
                    markerGivenTo: new HeroId("someone")),
                Throws.Nothing);
            Assert.That(
                () => EffectExecutionResult.DeclinedOptional(),
                Throws.Nothing,
                "A handler that declined gave nothing, which is the ordinary case.");
        }

        // -------------------------------------------------------------------------------------------
        // The resolver: stopping at the give, and only when something is watching
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheResolverStopsAtAGiveOnlyWhenSomethingIsWatching()
        {
            var unwatched = new MarkerOperationFixture().RunGiveThenPlaceAToken(watching: false);

            Assert.That(unwatched.IsComplete, Is.True,
                "A match with no watching source walks exactly as it did before this milestone, " +
                "which is what keeps every Tigerclaw Poison give in this build byte-identical.");
            Assert.That(unwatched.MarkerGivenTo.IsDefined, Is.False);

            var watched = new MarkerOperationFixture().RunGiveThenPlaceAToken(watching: true);

            Assert.That(watched.IsComplete, Is.False,
                "A watched give stops the walk at the node it finished, so the ability runs before " +
                "the next node does.");
            Assert.That(watched.MarkerGivenTo, Is.EqualTo(MarkerOperationFixture.RecipientOfTheRun));
            Assert.That(
                watched.Events.Single(recorded =>
                    recorded.NodeId.Equals(MarkerOperationFixture.GiveNodeId)).ResultKind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The node is finished and its facts are recorded before the walk stops, so a later " +
                "node binding the give's published holder still finds it.");
            Assert.That(
                watched.Events.Select(recorded => recorded.NodeId),
                Has.No.Member(MarkerOperationFixture.LaterNodeId),
                "and the node after it has not run yet, which is what \"immediately after\" means.");
        }

        // -------------------------------------------------------------------------------------------
        // The whole moment, driven through the authority that owns it
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnUnlockedUltimate_FiresAfterItsControllersGive_AndTheDiscardReachesTheRecipient()
        {
            var fixture = new MarkerGiveFixture(unlockTheUltimate: true);

            var result = fixture.GiveTo(fixture.Enemy);

            Assert.That(result.Accepted, Is.True);
            var standing = fixture.Authority.PendingEffectInterrupt;
            Assert.That(standing, Is.Not.Null,
                "The ability ran immediately after the give and stopped on its own question.");
            Assert.That(standing.Kind,
                Is.EqualTo(EffectInterruptKind.AfterMarkerGivenTrigger));
            Assert.That(standing.Subject, Is.EqualTo(RuleObject.ForHero(fixture.Enemy)),
                "It learned WHICH hero received the marker, which is the whole of what the window " +
                "carries beyond the moment itself.");
            Assert.That(standing.RemainingSubjects, Is.Empty,
                "One give gives one marker to one hero, so nothing is ever left owed.");

            var question = fixture.Authority.PendingEffectChoice;
            Assert.That(question, Is.Not.Null);
            Assert.That(question.ChooserId, Is.EqualTo(fixture.Enemy),
                "\"that hero discards a card\" - the decision belongs to the hero who received it, " +
                "never to the hero who gave it.");
            Assert.That(
                question.Candidates.All(candidate =>
                    fixture.Instances.Single(card =>
                            card.Id.Equals(candidate.Value.CardInstanceId))
                        .OwnerId.Equals(fixture.Enemy)),
                Is.True,
                "and the candidates are that hero's own cards.");

            var discarded = question.Candidates[0].Value.CardInstanceId;
            fixture.Answer(question.SelectInvocations.Single(candidate =>
                candidate.Candidate.Value.CardInstanceId.Equals(discarded)));
            fixture.Answer(fixture.Authority.PendingEffectChoice.ConfirmInvocation);

            Assert.That(fixture.Authority.PendingEffectInterrupt, Is.Null,
                "The ability finished and the effect it interrupted was carried on.");
            Assert.That(
                fixture.Instances.Single(card => card.Id.Equals(discarded)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The consequence reached the hero the marker was given to.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy),
                "and the marker the give attached is still where the give put it.");
        }

        [Test]
        public void AGiveToTheGiverHimself_FiresTheSameTrigger()
        {
            var fixture = new MarkerGiveFixture(unlockTheUltimate: true);

            fixture.GiveTo(fixture.Giver);

            var standing = fixture.Authority.PendingEffectInterrupt;
            Assert.That(standing, Is.Not.Null,
                "The printed sentence says \"a hero\", not \"an enemy hero\": BA-D5 puts every hero " +
                "on the board inside that phrase, the giver included.");
            Assert.That(standing.Subject, Is.EqualTo(RuleObject.ForHero(fixture.Giver)));
            Assert.That(fixture.Authority.PendingEffectChoice.ChooserId, Is.EqualTo(fixture.Giver));
        }

        [Test]
        public void ARecipientWithNoCardToGive_LeavesNothingStanding()
        {
            var fixture = new MarkerGiveFixture(unlockTheUltimate: true);
            fixture.EmptyTheHandOf(fixture.Enemy);

            var result = fixture.GiveTo(fixture.Enemy);

            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingEffectInterrupt, Is.Null,
                "\"discards a card, IF ABLE\" - revision 1 of the discard declines in silence when " +
                "the hand is empty, so the trigger finished in one pass and the sentence ends there.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy),
                "and the give itself stands, because the rider failing is not the give failing.");
        }

        [Test]
        public void AnUltimateThatIsNotUnlocked_FiresNotAtAll()
        {
            var fixture = new MarkerGiveFixture(unlockTheUltimate: false);

            var result = fixture.GiveTo(fixture.Enemy);

            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingEffectInterrupt, Is.Null,
                "Rulebook p.16 gives the permanent ability to a card beside the dashboard. A card " +
                "still in the deck has no ability, and no moment can wake one.");
            Assert.That(fixture.Authority.PendingEffectChoice, Is.Null);
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Enemy));
        }

        [Test]
        public void AGiveAnotherHeroPerformed_FiresNothing()
        {
            var fixture = new MarkerGiveFixture(unlockTheUltimate: true);

            var result = fixture.GiveAsTheOtherHero(to: fixture.Giver);

            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingEffectInterrupt, Is.Null,
                "The card says \"after YOU give\". Somebody else's give that put the marker on the " +
                "watching hero himself is still somebody else's give - BA-D5, and the reason " +
                "milestone 8.40b's transfer is its own operation rather than a return and a give.");
            Assert.That(fixture.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(fixture.Giver));
        }

        [Test]
        public void TheQuery_AnswersTheThreeGiverQuestionsOnItsOwn()
        {
            var fixture = new MarkerGiveFixture(unlockTheUltimate: true);

            var mine = AfterMarkerGivenTriggerQuery.Derive(
                fixture.Ultimates, fixture.Board, fixture.Giver, fixture.Enemy);
            Assert.That(mine.Count, Is.EqualTo(1));
            Assert.That(mine[0].RecipientId, Is.EqualTo(fixture.Enemy));
            Assert.That(mine[0].ControllerId, Is.EqualTo(fixture.Giver));
            Assert.That(mine[0].ResolutionIdValue, Does.StartWith("after-marker-given:"),
                "The trigger's frame is identified by its source and the hero it is running for, so " +
                "two gives in one resolution cannot share one identity.");

            Assert.That(
                AfterMarkerGivenTriggerQuery.Derive(
                    fixture.Ultimates, fixture.Board, fixture.Enemy, fixture.Giver),
                Is.Empty,
                "A give by the other hero is not this hero's give.");
            Assert.That(
                AfterMarkerGivenTriggerQuery.Derive(
                    fixture.Ultimates, fixture.Board, fixture.Giver, default),
                Is.Empty,
                "and a moment with no recipient is no moment at all.");

            Assert.That(
                AfterMarkerGivenTriggerQuery.AnyWatching(UnlockedUltimateSet.None), Is.False,
                "The coarse question the resolver is told the answer to is false when nobody is " +
                "watching, which is what keeps an ordinary give the walk it always was.");
            Assert.That(AfterMarkerGivenTriggerQuery.AnyWatching(fixture.Ultimates), Is.True);
        }

        [Test]
        public void TheNewInterruptKind_CrossesTheWireWithoutASchemaMove()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write((int)EffectInterruptKind.AfterMarkerGivenTrigger);
                }

                stream.Position = 0;
                using (var reader = new BinaryReader(stream))
                {
                    Assert.That(
                        SnapshotEnumReader.Read<EffectInterruptKind>(reader, "effect interrupt kind"),
                        Is.EqualTo(EffectInterruptKind.AfterMarkerGivenTrigger),
                        "The interrupt payload has written this enum as an int since milestone " +
                        "8.19c1 and the decode boundary admits any declared member, so a second " +
                        "reason an effect frame can be standing costs no schema version - which is " +
                        "why v42 and wire /14 are unmoved by this milestone.");
                }
            }

            Assert.That(
                new PendingEffectInterrupt(
                        ProbeFrame(),
                        EffectInterruptKind.AfterMarkerGivenTrigger,
                        RuleObject.ForHero(new HeroId("recipient")),
                        new EffectResolutionId("interrupted"))
                    .RemainingSubjects,
                Is.Empty,
                "and nothing is ever owed a second run, because one give gives one marker.");
        }

        private static PendingResolution ProbeFrame() =>
            new PendingResolution(
                new EffectResolutionId("marker-given-probe-frame"),
                new CardInstanceId("probe-card"),
                new CardDefinitionReference(new CardDefinitionId("probe-definition"), "probe-rev"),
                new ActionDefinitionId("probe-action"),
                SyntheticShowcaseHand.AfterMarkerGivenTriggeredEffect.Reference,
                new HeroId("probe-hero"),
                new HeroId("probe-hero"),
                ActionInvocationContext.PrintedPrimary,
                Array.Empty<EffectContinuationFrame>());

        // -------------------------------------------------------------------------------------------
        // The altitude Bain's own face uses: a give printed in rulebook p.14's card text
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Dead or Alive is a basic ATTACK whose give sits in an "After the attack:" clause, so his
        /// Ultimate's moment happens inside rulebook p.14's card text rather than inside a Skill - and
        /// the clause SELECTS its recipient, so the page pauses on that question and the give lands on
        /// the resume pass. This drives that exact shape through a whole match session.
        /// </summary>
        [Test]
        public void AGivePrintedInCardText_FiresTheSameTrigger_OnThePassTheAnswerArrives()
        {
            var session = CardTextSession();
            CommitTheGivingAttack(session);

            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.ActionDefinitionId.Value == "primary-attack-text");
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single()));
            var defense = session.Projection.PendingDefenseChoice;
            Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));

            var recipients = session.Projection.PendingEffectChoice;
            Assert.That(recipients, Is.Not.Null,
                "p.14's step 7 ran the text and the give asked who receives the marker, which is the " +
                "question Dead or Alive's own clause opens.");
            var actor = session.Projection.SelectedActor.ActorId;
            var recipient = recipients.Candidates
                .Select(candidate => candidate.Value.HeroId)
                .First(hero => !hero.Equals(actor));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                recipients.SelectInvocations.Single(candidate =>
                    candidate.Candidate.Value.HeroId.Equals(recipient))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var question = session.Projection.PendingEffectChoice;
            Assert.That(question, Is.Not.Null,
                "The give applied on THIS pass, so the moment his Ultimate watches is produced here - " +
                "a start-half-only wiring would have watched every composition except the printed one.");
            Assert.That(question.ChooserId, Is.EqualTo(recipient),
                "and the ability learned which hero received the marker at this altitude too.");
            Assert.That(
                question.Candidates.All(candidate =>
                    session.Projection.Cards
                        .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                        .OwnerId.Equals(recipient)),
                Is.True);

            var discarded = question.Candidates[0].Value.CardInstanceId;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                question.SelectInvocations.Single(candidate =>
                    candidate.Candidate.Value.CardInstanceId.Equals(discarded))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(discarded)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The consequence reached the hero the marker was given to.");
        }

        private static MatchSession CardTextSession()
        {
            var configuration =
                MatchConfiguration.CreateForMarkerGivingAttackTextFixture(TeamSide.Red);

            // p.16 unlocks the Ultimate by moving the card beside the dashboard. It is done on the
            // configured instance before the session reads it, because nothing short of p.15's whole
            // Level Up stage puts one there mid-match and what is under test is the moment rather than
            // the unlock.
            var ultimate = configuration.CardInstances.Single(card =>
                configuration.CardDefinitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .UltimateRule != null &&
                configuration.Participants
                    .GetTeamForHero(card.OwnerId).Side == TeamSide.Red);
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);

            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var heroes = BlueBesideRed(map, setup);
            var staged = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, heroes,
                setup.Minions);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, staged, new[] { map }));
        }

        private static IReadOnlyList<QuickGameHeroSetupState> BlueBesideRed(
            BoardMapDefinition map, QuickGameSetupState setup)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var beside = redHero.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));
            var besideSpaceId = map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.Coordinate.Equals(beside)).Id;

            return setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, besideSpaceId), beside)
                    : hero)
                .ToArray();
        }

        private static void CommitTheGivingAttack(MatchSession session)
        {
            var red = session.Projection.Cards.Single(card =>
                card.Id.Value == "red-bounty-card" && card.State.Location == CardLocation.Hand);
            var blue = session.Projection.Cards.Single(card =>
                card.Id.Value == "blue-longbow-card" && card.State.Location == CardLocation.Hand);
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide, red.Id));
            }

            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The giving Attack must be the card acting, or this case watches the wrong turn.");
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        // -------------------------------------------------------------------------------------------
        // The seven declarations this milestone must not have touched
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryUltimateTriggerDeclarationInTheCorpus_IsUnchanged()
        {
            var declared = ConfiguredUltimates()
                .Where(rule => rule.TriggerCondition != UltimateTriggerCondition.None)
                .ToArray();

            Assert.That(
                declared.Select(rule => rule.TriggerCondition).Distinct().OrderBy(value => (int)value),
                Is.EquivalentTo(new[]
                {
                    UltimateTriggerCondition.AfterControllerPerformsBasicSkill,
                    UltimateTriggerCondition.AfterControllerPerformsBasicAction,
                    UltimateTriggerCondition.AfterControllerPlacesSelf,
                    UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack,
                    UltimateTriggerCondition.AfterControllerResolvesBasicCard,
                    UltimateTriggerCondition.AfterControllerPerformsPrimaryAction,

                    // Milestone 8.40i. Bain's A Complicated Profession is the one face in the build
                    // that declares this milestone's own condition, which is what 8.40h built it for -
                    // and the case below is what keeps the other six naming the action window.
                    UltimateTriggerCondition.AfterControllerGivesMarker
                }),
                "The six conditions dealt heroes declared before this series still are what they " +
                "were, and the seventh is Bain's own.");
            Assert.That(
                declared.All(rule =>
                    rule.TriggerCondition == UltimateTriggerCondition.AfterControllerGivesMarker
                        ? rule.TriggerCheckpoints.Count == 1 &&
                            rule.TriggerCheckpoints[0] == TimingCheckpoint.MarkerGivenToHero
                        : rule.TriggerCheckpoints.Count == 1 &&
                            rule.TriggerCheckpoints[0] == TimingCheckpoint.ActionEnded),
                Is.True,
                "and every one of the six still names the action window, which is the half the widened " +
                "constructor guard could have loosened and did not - while the seventh names the " +
                "moment a marker is given, which is the pairing that guard now enforces in both " +
                "directions.");

            var momentWatchers = ConfiguredUltimates()
                .Where(rule => rule.AfterPush.IsDeclared || rule.BeforeAction.IsDeclared)
                .ToArray();
            Assert.That(momentWatchers.Count(rule => rule.AfterPush.IsDeclared), Is.EqualTo(1),
                "Sabina's Big Sodding Gun is still the one after-push declaration in the build.");
            Assert.That(momentWatchers.Count(rule => rule.BeforeAction.IsDeclared), Is.EqualTo(2),
                "and Arien's Living Tsunami and Silverarrow's Wild Hunt are still the two before-action " +
                "declarations - measured rather than transcribed, because this block's own paragraph " +
                "said \"both existing moment-watching declarations\" and meant the two FAMILIES.");
            Assert.That(
                momentWatchers.All(rule =>
                    rule.TriggerCondition == UltimateTriggerCondition.None &&
                    rule.TriggerCheckpoints.Count == 0),
                Is.True,
                "Both express their moment through their own declaration rather than through the " +
                "checkpoint collection, which is exactly what this milestone did NOT do - and the " +
                "reason it did not is the digest those two properties already cost.");
        }

        /// <summary>
        /// Every Ultimate rule the build's verified content declares, read off the same content classes
        /// the corpus sweep walks rather than a list written here.
        /// </summary>
        private static IEnumerable<UltimateRuleDefinition> ConfiguredUltimates() =>
            typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type => type.IsSealed && type.IsAbstract &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .Select(type => type.GetProperty("AllDefinitions"))
                .Where(property => property != null &&
                    typeof(IEnumerable<CardDefinition>).IsAssignableFrom(property.PropertyType))
                .SelectMany(property => (IEnumerable<CardDefinition>)property.GetValue(null, null))
                .Select(definition => definition.UltimateRule)
                .Where(rule => rule != null);

        // -------------------------------------------------------------------------------------------
        // Fixtures
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The three marker operations driven as production handlers on a featureless board carrying
        /// two marker definitions, so "whose marker" and "which operation" are both real questions.
        /// </summary>
        private sealed class MarkerOperationFixture
        {
            public static readonly EffectNodeId GiveNodeId = new EffectNodeId("run-give");
            public static readonly EffectNodeId LaterNodeId = new EffectNodeId("run-later");

            private const string CardIdValue = "marker-give-card";
            private const string ActionIdValue = "primary-skill";

            private readonly MatchParticipants participants =
                StagedBoardParticipants.CreateWithBlueAlly();
            private readonly ComponentDefinitionCatalog catalog;
            private readonly MatchComponents components;
            private readonly IEffectCapabilityHandler give;
            private readonly IEffectCapabilityHandler transfer;
            private readonly IEffectCapabilityHandler returnAny;

            public MarkerOperationFixture()
            {
                Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
                var board = new AdjacentMoveAuthority(
                    FeaturelessDisk(3),
                    new[]
                    {
                        new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(1, 0)),
                        new HeroOccupancy(Ally, new HexCoordinate(0, 1))
                    });
                catalog = new ComponentDefinitionCatalog(
                    new[]
                    {
                        SyntheticComponentDefinitions.HazardDefinition,
                        SyntheticComponentDefinitions.WardDefinition
                    },
                    new[] { SyntheticComponentDefinitions.FocusDefinition }
                        .Concat(TigerclawComponentContent.Markers())
                        .Concat(BainComponentContent.Markers()));
                components = new MatchComponents(catalog, board, Markers, board, board, board);

                var handlers = BainComponentContent.Handlers().ToArray();
                give = handlers.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.GiveBountyV1));
                transfer = handlers.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.TransferBountyV1));
                returnAny = handlers.Single(handler =>
                    handler.Reference.Equals(BainComponentContent.ReturnBountyV1));
            }

            /// <summary>The hero the whole-resolution run below gives to.</summary>
            public static HeroId RecipientOfTheRun =>
                new HeroId(StagedBoardParticipants.BlueHeroIdValue);

            public HeroId Actor { get; }
            public HeroId Enemy { get; }
            public HeroId Ally { get; }
            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();

            public IReadOnlyList<MarkerAttachment> Attachments => Markers.Attachments;

            public int AvailableBounties =>
                catalog.CountAvailableMarkers(
                    BainComponentContent.BountyDefinition.Reference, Markers.Attachments);

            public EffectExecutionResult Give(
                HeroId to, bool movesWhenSupplyIsEmpty = false, string nodeSuffix = "one") =>
                give.Resume(
                    Context(
                        new EffectNodeId("give-" + nodeSuffix),
                        new Dictionary<string, EffectValue>
                        {
                            {
                                SyntheticComponentCapabilities.MarkerGivingHandler
                                    .MovesWhenSupplyIsEmptyParameterName,
                                EffectValue.Boolean(movesWhenSupplyIsEmpty)
                            }
                        },
                        HolderOutputs()),
                    new[] { EffectValue.Hero(to) });

            public EffectExecutionResult Transfer(HeroId to) =>
                transfer.Resume(
                    Context(new EffectNodeId("transfer"), null, TransferOutputs()),
                    new[] { EffectValue.Hero(to) });

            public EffectExecutionResult ReturnAny() =>
                returnAny.Execute(
                    Context(
                        new EffectNodeId("return"),
                        null,
                        new[] { new EffectOutputBinding("returned", EffectValueKind.Hero) }));

            /// <summary>
            /// A give and a following node as one resolution, run through the production session so the
            /// stop is read off a recorded walk rather than off a handler call.
            /// </summary>
            public EffectResolutionStepResult RunGiveThenPlaceAToken(bool watching)
            {
                var root = new EffectNodeId("run-root");
                var definition = new EffectDefinition(
                    new EffectDefinitionId("marker-given-trigger-fixture"),
                    "rev-1",
                    root,
                    new[]
                    {
                        new EffectNodeDefinition(
                            root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { GiveNodeId, LaterNodeId }),
                        new EffectNodeDefinition(
                            GiveNodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            BainComponentContent.GiveBountyV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromLiteral(
                                    SyntheticComponentCapabilities.MarkerGivingHandler
                                        .HolderParameterName,
                                    EffectValue.Hero(Enemy))
                            },
                            outputs: HolderOutputs()),
                        new EffectNodeDefinition(
                            LaterNodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            BainComponentContent.TransferBountyV1,
                            inputs: new[]
                            {
                                EffectInputBinding.FromLiteral(
                                    SyntheticComponentCapabilities.MarkerTransferHandler
                                        .HolderParameterName,
                                    EffectValue.Hero(Actor))
                            },
                            outputs: TransferOutputs())
                    });

                return EffectResolutionSession.Start(
                    new EffectResolutionId("marker-given-trigger-run"),
                    new CardInstanceId(CardIdValue),
                    default,
                    new ActionDefinitionId(ActionIdValue),
                    Actor,
                    Actor,
                    ActionInvocationContext.PrintedPrimary,
                    new EffectResolutionContext(
                        definition,
                        new EffectCapabilityCatalog(BainComponentContent.Handlers()),
                        participants,
                        null,
                        1,
                        1,
                        RuleModificationSet.Empty,
                        EffectCardState.Absent,
                        ActionKind.Skill,
                        components: components,
                        watchesMarkerGives: watching));
            }

            private static EffectOutputBinding[] HolderOutputs() =>
                new[] { new EffectOutputBinding("holder", EffectValueKind.Hero) };

            private static EffectOutputBinding[] TransferOutputs() => new[]
            {
                new EffectOutputBinding("holder", EffectValueKind.Hero),
                new EffectOutputBinding(
                    SyntheticComponentCapabilities.MarkerTransferHandler.PreviousHolderOutputName,
                    EffectValueKind.Hero)
            };

            private EffectCapabilityContext Context(
                EffectNodeId nodeId,
                IReadOnlyDictionary<string, EffectValue> inputs,
                IReadOnlyList<EffectOutputBinding> declaredOutputs) =>
                new EffectCapabilityContext(
                    nodeId,
                    EffectFailurePolicy.Mandatory,
                    Actor,
                    Actor,
                    new CardInstanceId(CardIdValue),
                    new CardDefinitionReference(
                        new CardDefinitionId("marker-give-fixture"), "fixture-rev-1"),
                    new ActionDefinitionId(ActionIdValue),
                    inputs,
                    1,
                    1,
                    participants,
                    null,
                    RuleModificationSet.Empty,
                    EffectCardState.Absent,
                    ActionKind.Skill,
                    components,
                    declaredOutputs: declaredOutputs);
        }

        /// <summary>
        /// The whole moment, driven through <see cref="CardActionResolutionAuthority"/>: the showcase
        /// hand whose Ultimate watches a give, the showcase component Skill that performs one, and the
        /// other hero's copy of that Skill so a give somebody else performs can be driven too.
        /// </summary>
        private sealed class MarkerGiveFixture
        {
            private static readonly ActionDefinitionId ComponentSkill =
                new ActionDefinitionId("primary-component-skill");

            private static readonly EffectNodeId GiveBranch =
                new EffectNodeId("component-give-marker");

            private readonly List<CardDefinition> definitions = new List<CardDefinition>();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private int resolutions;

            public MarkerGiveFixture(bool unlockTheUltimate)
            {
                Participants = StagedBoardParticipants.CreateWithBlueAlly();
                Giver = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Other = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
                Board = new AdjacentMoveAuthority(
                    FeaturelessDisk(3),
                    new[]
                    {
                        new HeroOccupancy(Giver, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(1, 0)),
                        new HeroOccupancy(Other, new HexCoordinate(0, 1))
                    });

                var watcher = SyntheticShowcaseHand.CreateRedWithAfterMarkerGivenUltimate(Giver);
                var enemy = SyntheticShowcaseHand.CreateBlueWithMarkerGivingSkill(Enemy);
                foreach (var hand in new[] { watcher, enemy })
                {
                    definitions.AddRange(hand.Definitions);
                    instances.AddRange(hand.AllInstances);
                }

                if (unlockTheUltimate)
                {
                    var ultimate = watcher.AllInstances.Single(card =>
                        watcher.Definitions
                            .Single(definition =>
                                definition.Reference.Equals(card.DefinitionReference))
                            .UltimateRule != null);
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True,
                        "p.16 unlocks the Ultimate by moving the card beside the dashboard.");
                }

                Authority = new CardActionResolutionAuthority(
                    MatchContentCatalogs.ForThisBuild(),
                    Board,
                    Markers,
                    new HeroProgressionState(Participants),
                    new TeamLifeState(Participants, 8));
            }

            public MatchParticipants Participants { get; }
            public AdjacentMoveAuthority Board { get; }
            public HeroId Giver { get; }
            public HeroId Enemy { get; }
            public HeroId Other { get; }
            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();
            public CardActionResolutionAuthority Authority { get; }

            public IReadOnlyList<CardInstance> Instances => instances;

            public UnlockedUltimateSet Ultimates =>
                UltimateUnlockQuery.Derive(definitions, instances);

            /// <summary>The watching hero's own give, taken to the hero named.</summary>
            public CardActionResolutionResult GiveTo(HeroId recipient) =>
                PerformTheGive(Giver, recipient);

            /// <summary>The other hero's give, which is a give this Ultimate did not perform.</summary>
            public CardActionResolutionResult GiveAsTheOtherHero(HeroId to) =>
                PerformTheGive(Enemy, to);

            public void EmptyTheHandOf(HeroId hero)
            {
                foreach (var card in instances
                    .Where(card => card.OwnerId.Equals(hero) &&
                        card.State.Location == CardLocation.Hand)
                    .ToArray())
                {
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
                }
            }

            public void Answer(EffectChoiceCommand command)
            {
                var result = Authority.PendingEffectInterrupt != null
                    ? Authority.AnswerEffectInterruptChoice(
                        command,
                        new ActionBoundaryContext(
                            definitions, instances, Participants, timing,
                            TieBreakerState.Unrevealed, 1, 1))
                    : Authority.AnswerEffectChoice(
                        command, definitions, instances, Participants, timing,
                        RuleModificationSet.Empty, TieBreakerState.Unrevealed, 1, 1);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
            }

            private CardActionResolutionResult PerformTheGive(HeroId actor, HeroId recipient)
            {
                var card = instances.Single(instance =>
                    instance.OwnerId.Equals(actor) && instance.Id.Value.Contains("cache"));
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);

                // p.7 commits a card face down and reveals it; the turn's own offers are derived from
                // the card standing in the turn area, which is the window a turn Skill starts from.
                var offer = ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, instances, actor, card.Id)
                    .Single(candidate => candidate.ActionDefinitionId.Equals(ComponentSkill));

                var started = Authority.StartTurnSkill(
                    new ActionResolutionId("marker-give-" + ++resolutions),
                    offer,
                    definitions,
                    instances,
                    Participants,
                    timing,
                    RuleModificationSet.Empty,
                    TieBreakerState.Unrevealed,
                    1,
                    1);
                Assert.That(started.Accepted, Is.True, started.RejectionReason.ToString());

                ChooseOption(GiveBranch);
                var recipients = Authority.PendingEffectChoice;
                Assert.That(recipients, Is.Not.Null,
                    "Who receives the marker is the attachment decision, and the showcase give binds " +
                    "no holder, so every hero is offered.");
                Answer(recipients.SelectInvocations.Single(candidate =>
                    candidate.Candidate.Value.HeroId.Equals(recipient)));
                var confirmation = Authority.PendingEffectChoice;
                Answer(confirmation.ConfirmInvocation);
                return started;
            }

            private void ChooseOption(EffectNodeId option)
            {
                var choice = Authority.PendingEffectChoice;
                Answer(choice.SelectInvocations.Single(candidate =>
                    candidate.Candidate.OptionNodeId.Equals(option)));
                Answer(Authority.PendingEffectChoice.ConfirmInvocation);
            }
        }

        private static HexCoordinate[] FeaturelessDisk(int radius)
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius)
                    {
                        coordinates.Add(new HexCoordinate(q, r));
                    }
                }
            }

            return coordinates.ToArray();
        }
    }
}
