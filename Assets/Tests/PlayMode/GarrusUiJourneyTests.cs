using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Garrus's real-input journeys. Staging supplies positions, never effect outcomes.</summary>
    public sealed class GarrusUiJourneyTests
    {
        internal static HeroId Garrus(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "garrus").HeroId;
        internal static int Distance(HexCoordinate a, HexCoordinate b) =>
            (Math.Abs(a.Q-b.Q) + Math.Abs(a.R-b.R) + Math.Abs(a.Q+a.R-b.Q-b.R))/2;
        internal static HexCoordinate Position(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        /// <summary>
        /// Where the piece a candidate names is standing, read off the authority. MILESTONE 8.86: a
        /// journey that drives a Move, a Push, a Place or a Swap reads this BEFORE and AFTER the
        /// press, because nothing else can tell a displacement from a step that never ran.
        /// </summary>
        internal static HexCoordinate SpaceOf(ConfiguredMatchSceneRuntime runtime, EffectValue unit) =>
            unit.Kind == EffectValueKind.Hero
                ? Position(runtime, unit.HeroId)
                : runtime.ReadModel.Minions
                    .Single(value => value.MinionId.Equals(unit.MinionId)).Coordinate;

        /// <summary>
        /// The one answer a journey NAMES out of an offer - MILESTONE 8.86's replacement for taking
        /// whatever the resolver happened to put first. The pick is by the candidate's own identity
        /// in ordinal order, so the journey can say afterwards which piece, card or space it answered
        /// with and assert what that answer DID; and the offer is asserted non-empty, so a step can
        /// never pass by having nothing to answer.
        /// </summary>
        internal static EffectChoiceCandidate Named(
            IEnumerable<EffectChoiceCandidate> offer, string what)
        {
            var candidates = offer.ToArray();
            Assert.That(candidates, Is.Not.Empty,
                "MILESTONE 8.86: a journey names its own answer, and the offer it is naming one " +
                "out of is empty - " + what);
            return candidates
                .OrderBy(candidate => CandidateName(candidate.Value), StringComparer.Ordinal)
                .First();
        }

        /// <summary>The identity any candidate names, for an ordering a journey can repeat.</summary>
        internal static string CandidateName(EffectValue value) =>
            value.Kind == EffectValueKind.Hero ? "hero:" + value.HeroId.Value
            : value.Kind == EffectValueKind.Minion ? "minion:" + value.MinionId.Value
            : value.Kind == EffectValueKind.CardInstance ? "card:" + value.CardInstanceId.Value
            : value.Kind == EffectValueKind.Coordinate ? "space:" + value.Coordinate
            : value.Kind == EffectValueKind.Integer
                ? "int:" + value.IntegerValue.ToString("D4")
                : value.Kind.ToString();

        /// <summary>The identity a unit-valued candidate names, for an ordering a journey can repeat.</summary>
        internal static string UnitName(EffectValue unit) =>
            unit.Kind == EffectValueKind.Hero ? unit.HeroId.Value
            : unit.Kind == EffectValueKind.Minion ? unit.MinionId.Value
            : unit.Kind.ToString();

        /// <summary>Whether this minion is still standing on the board at all.</summary>
        internal static bool MinionStands(ConfiguredMatchSceneRuntime runtime, MinionId minionId) =>
            runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(minionId));
        internal static void Healthy(ConfiguredMatchSceneRuntime runtime)
        {
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            foreach (var hero in runtime.Heroes)
                Assert.That(HeroView(hero.HeroId).Coordinate, Is.EqualTo(hero.Coordinate));
        }

        internal static IEnumerator Stage(CardDefinitionId face, bool friendlyTargets = false,
            bool ultimate = false, CardDefinitionId[] enemyHand = null, int? coins = null,
            CardDefinitionId[] ownHand = null)
        {
            var content = ultimate ? SceneCardContent.VerifiedTierThreeEngagedStagedGarrus
                : SceneCardContent.VerifiedEngagedStagedGarrus;
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var garrus = Garrus(runtime);
            var hand = ownHand ?? new[] { face, GarrusCardContent.AngryStrike, GarrusCardContent.ChillingHowl,
                GarrusCardContent.Menace, GarrusCardContent.Trace, GarrusCardContent.HoldGround }.Distinct().Take(5).ToArray();
            if (friendlyTargets)
            {
                // Heroes stay where the setup puts them. Off-lane minions would be corrected at
                // the action boundary before the repeat, changing the fixture's subject.
                var roster = RegisteredHeroRoster.ForThisBuild;
                var own = roster.Single(hero => hero.ContentId.Value == "garrus");
                var allies = new[] { "sabina", "tigerclaw" }.Select(id => roster.Single(hero => hero.ContentId.Value == id)).ToArray();
                var enemies = new[] { "wasp", "xargatha", "arien" }.Select(id => roster.Single(hero => hero.ContentId.Value == id)).ToArray();
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
                MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(enemies, new[] { own }.Concat(allies));
                MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[] {
                    new SandboxHeroStaging(new HeroId(own.HeroIdValue),new HexCoordinate(-3,1),hand:hand),
                    new SandboxHeroStaging(new HeroId(allies[0].HeroIdValue),new HexCoordinate(-5,1)),
                    new SandboxHeroStaging(new HeroId(allies[1].HeroIdValue),new HexCoordinate(-4,-1)) });
                yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
                yield break;
            }
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[] { new SandboxHeroStaging(garrus, new HexCoordinate(-3,1), coins: coins, hand: hand),
                    new SandboxHeroStaging(OtherHeroId(runtime,garrus), new HexCoordinate(-2,1), hand: enemyHand) });
            yield return LoadStagedScene(content);
        }

        internal static IEnumerator Begin(MatchSceneCompositionRoot root, CardDefinitionId face, ActionKind kind)
        {
            var actor = Garrus(root.Runtime);
            yield return CommitEverySeat(root, actor, NameOf(root.Runtime, face));
            yield return WaitForActor(root, actor);
            yield return PointTrayAt(root, actor);
            yield return StartActionThroughControl(root, kind);
            Healthy(root.Runtime);
        }

        internal static IEnumerator WaitForActor(MatchSceneCompositionRoot root, HeroId actor)
        {
            var runtime = root.Runtime;
            for (var budget = 0; budget < 24; budget++)
            {
                if (runtime.PendingTurnRelease == null && runtime.SelectedActor.IsDefined &&
                    runtime.SelectedActor.ActorId.Equals(actor)) yield break;
                // Milestone 8.46e: a tied initiative is drawn on the decision window as card faces,
                // so the first face is pressed through the window's own element for it.
                var initiative = UiPlayer.DecisionControl(runtime, command =>
                    command.Kind == DirectorCommandKind.ChooseTiedActor);
                if (initiative != null) UiPlayer.ClickDecisionControl(runtime, root.Hud, initiative);
                else
                {
                    Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null, Remaining(runtime));
                    UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                }
                yield return null;
            }
            Assert.Fail("Garrus never became the acting seat. " + Remaining(runtime));
        }

        internal static IEnumerator May(MatchSceneCompositionRoot root, bool accept = true)
        {
            var choice = root.Runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, Remaining(root.Runtime));
            var kind = accept ? EffectChoiceCommandKind.Confirm : EffectChoiceCommandKind.Decline;
            // Milestone 8.46e: a printed `may` is answered on the decision window's own pair, whose
            // labels name the consequence rather than ACCEPT/DECLINE, so the control is found by its
            // command kind on the window's model and never by typed words.
            UiPlayer.ClickEffectControl(root.Runtime, root.Hud, kind);
            yield return null;
            yield return null;
            Healthy(root.Runtime);
        }

        internal static IEnumerator Pick(MatchSceneCompositionRoot root, EffectChoiceCandidate candidate)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            var node = choice.NodeId;
            var oldCount = runtime.AcceptedCommands.Count;
            var oldVersion = runtime.Projection.Version;
            var stale = choice.SelectInvocations.Single(value => value.Candidate.Equals(candidate));
            var value = candidate.Value;
            if (value.Kind == EffectValueKind.CardInstance)
            {
                var card = runtime.RoundView.Model.EffectChoices.Single(row =>
                    row.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    row.Command.EffectChoice.Candidate.Equals(candidate));
                var element = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", card.Card.DisplayName.ToUpperInvariant());
                UiPlayer.EnsureVisible(element);
                yield return null;
                UiPlayer.Click(UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", card.Card.DisplayName.ToUpperInvariant()));
            }
            else
            {
                var location = value.Kind == EffectValueKind.Hero ? HeroView(value.HeroId).transform.position
                    : value.Kind == EffectValueKind.Minion ? MinionView(value.MinionId).transform.position
                    : Tile(value.Coordinate).transform.position;
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud, location), Is.True);
            }
            yield return null;
            yield return ConfirmIfStillStanding(root, node);
            Assert.That(runtime.Projection.Version - oldVersion, Is.InRange(1L,2L),
                "One answer, plus its projected confirm when required: " + node.Value + " / " + value.Kind + "; " + Remaining(runtime));
            if (runtime.Projection.IsHistoryAvailable)
                Assert.That(runtime.AcceptedCommands.Count - oldCount, Is.EqualTo(runtime.Projection.Version - oldVersion));
            var after = runtime.CaptureSnapshot();
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(stale)), Is.False, "The consumed input is stale.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after), "Stale input is atomic.");
            Healthy(runtime);
        }

        internal static IEnumerator Space(MatchSceneCompositionRoot root, HexCoordinate? exact = null)
        {
            var candidates = root.Runtime.PendingEffectChoice.Candidates.Where(value => value.Value.Kind == EffectValueKind.Coordinate).ToArray();
            var coordinate = exact ?? FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                candidates.Select(value => value.Value.Coordinate).ToArray());
            yield return Pick(root, candidates.Single(value => value.Value.Coordinate.Equals(coordinate)));
        }

        [UnityTest]
        public IEnumerator BlueFacesMoveTheChosenUnitExactlyOneAndOfferTheirPrintedRepeats()
        {
            var faces = new[] { GarrusCardContent.Menace, GarrusCardContent.Threaten, GarrusCardContent.FormUp,
                GarrusCardContent.Terrify, GarrusCardContent.Testudo };
            foreach (var face in faces)
            {
                var friendly = face.Equals(GarrusCardContent.FormUp) || face.Equals(GarrusCardContent.Testudo);
                var repeats = face.Equals(GarrusCardContent.Menace) ? 0
                    : face.Equals(GarrusCardContent.Threaten) || face.Equals(GarrusCardContent.FormUp) ? 1 : 2;
                yield return Stage(face, friendlyTargets: friendly);
                var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
                yield return Begin(root, face, ActionKind.Skill);
                for (var iteration = 0; iteration <= repeats; iteration++)
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null, face.Value + " execution " + iteration + "; " + Remaining(runtime));
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("target"), face.Value);
                    // MILESTONE 8.86, THE NAMED SUBJECT. This used to take whatever the offer put
                    // first, which is an answer a journey cannot state afterwards. Every candidate is
                    // now asserted to be a PIECE the printed sentence can move, and the one driven is
                    // picked by its own identity in ordinal order - so the before-and-after reading
                    // below is about a unit this journey can name.
                    Assert.That(
                        runtime.PendingEffectChoice.Candidates.All(value =>
                            value.Value.Kind == EffectValueKind.Hero ||
                            value.Value.Kind == EffectValueKind.Minion),
                        Is.True,
                        face.Value + ": \"move a unit\" names a piece on the board, and every " +
                        "candidate is one. " + Remaining(runtime));
                    var candidate = friendly
                        ? runtime.PendingEffectChoice.Candidates
                            .Where(value => value.Value.Kind == EffectValueKind.Hero)
                            .OrderBy(value => UnitName(value.Value), StringComparer.Ordinal).First()
                        : runtime.PendingEffectChoice.Candidates
                            .OrderBy(value => UnitName(value.Value), StringComparer.Ordinal).First();
                    var target = candidate.Value;
                    var before = SpaceOf(runtime, target);
                    yield return Pick(root, candidate);
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("unit-move"));
                    var destinations = OfferedSpaces(runtime);
                    Assert.That(destinations.All(at => Distance(at,before) == 1 &&
                        Distance(at,Position(runtime,actor)) == Distance(before,Position(runtime,actor)) + (friendly ? -1 : 1)), Is.True);
                    var landing = FirstClickableSpace(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), destinations);
                    yield return Space(root, landing);
                    // THE ASSERTION THIS JOURNEY WAS MISSING (milestone 8.86). It proved the SHAPE of
                    // the offer and then never read the board again, so a move that never ran would
                    // have passed it exactly as a move that did.
                    Assert.That(SpaceOf(runtime, target), Is.EqualTo(landing),
                        face.Value + ": the named unit is standing on the space that was clicked. " +
                        Remaining(runtime));
                    Assert.That(Distance(SpaceOf(runtime, target), before), Is.EqualTo(1),
                        face.Value + ": \"1 space\" is a count, so the piece really left the space " +
                        "it stood on. " + Remaining(runtime));
                    if (iteration < repeats)
                    {
                        Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("repeat"));
                        yield return May(root);
                    }
                }
                Assert.That(runtime.PendingEffectChoice, Is.Null, face.Value);
                Assert.That(runtime.PendingTurnRelease, Is.Not.Null);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator BothPushAttacksAcceptZeroMoveAndPushTheSeparatelyChosenHero()
        {
            foreach (var face in new[] { GarrusCardContent.BluntForce, GarrusCardContent.SendFlying })
            {
                yield return Stage(face);
                var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
                var enemy = OtherHeroId(runtime, actor); var start = Position(runtime,actor);
                var enemyStart = Position(runtime,enemy);
                yield return Begin(root, face, ActionKind.Attack);
                // MILESTONE 8.86: the target is named by its own id in ordinal order rather than
                // taken first off the offer, so the defeat asserted below is about a piece this
                // journey can state.
                var target = runtime.PendingAttackTargetChoice.LegalTargets
                    .Where(value => value.Kind == AttackTargetKind.Minion)
                    .OrderBy(value => value.MinionId.Value, StringComparer.Ordinal)
                    .First();
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    MinionView(target.MinionId).transform.position), Is.True);
                yield return null;
                // THE ATTACK'S OWN OUTCOME (milestone 8.86): p.14 defeats an attacked minion without
                // a Defense step, so the piece this Attack named is off the board before its printed
                // push is ever offered.
                Assert.That(MinionStands(runtime, target.MinionId), Is.False,
                    face.Value + ": the attacked minion is defeated and its piece leaves the board. " +
                    Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
                yield return May(root);
                Assert.That(OfferedSpaces(runtime), Does.Contain(start), "GA-Q1: selecting zero is a real choice.");
                yield return Space(root, start);
                Assert.That(Position(runtime,actor), Is.EqualTo(start));
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value => value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(enemy)));
                Assert.That(Distance(Position(runtime,enemy),enemyStart), Is.EqualTo(3));
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator PursuitFacesTakeBothBulletsAndMoveBeforeSelectingTheHero()
        {
            foreach (var face in new[] { GarrusCardContent.Trace, GarrusCardContent.Chase, GarrusCardContent.HuntDown })
            foreach (var advance in new[] { true, false })
            {
                yield return Stage(face, ownHand: new[] { face, GarrusCardContent.AngryStrike, GarrusCardContent.ChillingHowl });
                var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
                var start = Position(runtime,actor); var enemy = OtherHeroId(runtime,actor);
                Assert.That(runtime.Cards.Any(card => card.OwnerId.Equals(actor) && card.State.Location == CardLocation.DiscardPile), Is.True);
                yield return Begin(root,face,ActionKind.Attack);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("mode"));
                yield return PressEffectControl(root,BulletLabelFor(runtime,advance ? "advance" : "ordinary"));
                if (advance)
                {
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("move"));
                    yield return Space(root,new HexCoordinate(-2,0));
                    Assert.That(Position(runtime,actor), Is.EqualTo(new HexCoordinate(-2,0)));
                    yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value =>
                        value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(enemy)));
                    Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, "The Move and binding precede the Attack response.");
                    // Milestone 8.46e: the Defense decline is the decision window's corner, read off its model.
                    UiPlayer.ClickDefenseDecline(runtime,root.Hud);
                    yield return null;
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("unit-target"));
                    // MILESTONE 8.86: named by id, and the Attack's own outcome read afterwards.
                    var struck = runtime.PendingEffectChoice.Candidates
                        .Where(value => value.Value.Kind == EffectValueKind.Minion)
                        .OrderBy(value => value.Value.MinionId.Value, StringComparer.Ordinal)
                        .First();
                    var struckId = struck.Value.MinionId;
                    yield return Pick(root, struck);
                    Assert.That(MinionStands(runtime, struckId), Is.False,
                        face.Value + ": the ordinary bullet's own Attack defeats the minion it " +
                        "named. " + Remaining(runtime));
                    Assert.That(Position(runtime,actor), Is.EqualTo(start));
                }
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                Assert.That(runtime.PendingTurnRelease, Is.Not.Null);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator PilumsAskTheOpponentForACardThenOfferTheirPrintedMove()
        {
            foreach (var face in new[] { GarrusCardContent.LightPilum, GarrusCardContent.HeavyPilum })
            foreach (var empty in new[] { false,true })
            {
                yield return Stage(face,enemyHand: empty ? new[] { WaspCardContent.MagneticDagger } : null);
                var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime); var enemy = OtherHeroId(runtime,actor);
                yield return Begin(root,face,ActionKind.Skill);
                yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.HeroId.Equals(enemy)));
                if (!empty)
                {
                    Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemy));
                    // MILESTONE 8.86: every face offered is a card the NAMED hero is holding - the
                    // printed sentence is about their hand and nobody else's - and the card driven is
                    // named by its own instance id rather than taken first off the offer.
                    Assert.That(
                        runtime.PendingEffectChoice.Candidates.All(value => runtime.Cards
                            .Single(held => held.Id.Equals(value.Value.CardInstanceId))
                            .OwnerId.Equals(enemy)),
                        Is.True,
                        "\"the target discards a card\" offers that hero's own hand. " + Remaining(runtime));
                    var heldBefore = runtime.Cards.Count(value =>
                        value.OwnerId.Equals(enemy) && value.State.Location == CardLocation.Hand);
                    var card = runtime.PendingEffectChoice.Candidates
                        .OrderBy(value => value.Value.CardInstanceId.Value, StringComparer.Ordinal)
                        .First();
                    yield return Pick(root,card);
                    Assert.That(runtime.Cards.Single(value => value.Id.Equals(card.Value.CardInstanceId)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                    // THE PRINTED NUMBER (milestone 8.86): one card, out of the hand it was asked of.
                    Assert.That(
                        runtime.Cards.Count(value =>
                            value.OwnerId.Equals(enemy) && value.State.Location == CardLocation.Hand),
                        Is.EqualTo(heldBefore - 1),
                        "and the named hero's hand is exactly one card shorter. " + Remaining(runtime));
                }
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("may-move"));
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)),
                    Is.EqualTo(!empty || face.Equals(GarrusCardContent.LightPilum)), "Heavy's defeat is complete BEFORE its optional Move.");
                yield return May(root);
                var before = Position(runtime,actor);
                var heavy = face.Equals(GarrusCardContent.HeavyPilum);
                Assert.That(OfferedSpaces(runtime).All(at => heavy ? Distance(at,before) <= 2 : Distance(at,before) == 1), Is.True);
                yield return Space(root,new HexCoordinate(-2,0));
                Assert.That(Position(runtime,actor), Is.EqualTo(new HexCoordinate(-2,0)));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator HowlDiscardsAResolvedCardAndBattleFuryResumesThroughUndoAndRestore()
        {
            foreach (var accept in new[] { true,false })
            {
                yield return Stage(GarrusCardContent.Threaten,ultimate:true);
                var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
                yield return CommitEverySeat(root,actor,NameOf(runtime,GarrusCardContent.Threaten));
                yield return YieldUntilTheNamedSeatActs(runtime,actor);
                UiPlayer.Click(UiPlayer.Button(root.Hud,runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
                yield return AdvanceToNextTurn(runtime);
                var source = runtime.Cards.Single(card => card.OwnerId.Equals(actor) && card.DefinitionReference.DefinitionId.Equals(GarrusCardContent.Threaten));
                Assert.That(source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
                yield return Begin(root,GarrusCardContent.ChillingHowl,ActionKind.Skill);
                yield return May(root);
                var discardChoice = runtime.PendingEffectChoice.Candidates.Single(value => value.Value.CardInstanceId.Equals(source.Id));
                yield return Pick(root,discardChoice);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("may-perform-primary"));
                Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(actor)).UltimateCard.DisplayName, Is.EqualTo("Battle Fury"));
                Assert.That(UiPlayer.Descendants(root.Hud.RootElement).Any(element => element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True);
                yield return UiPlayer.CaptureGameView("8-41f", "battle-fury-pause");
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty, "The aura follows Battle Fury, never precedes it.");
                var pause = runtime.CaptureSnapshot();
                OpenDirectorDrawer(root.Hud); yield return null;
                // Milestone 8.46b: the pick of exactly one card is ONE accepted command - the select
                // carries its own confirm - so one UNDO gives the discard question back unanswered.
                UiPlayer.Click(UiPlayer.Button(root.Hud,"UNDO")); yield return null;
                Assert.That(runtime.Cards.Single(card => card.Id.Equals(source.Id)).State.Location, Is.EqualTo(CardLocation.TurnArea));
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("discard"));
                Assert.That(runtime.PendingEffectChoice.SelectedCandidates, Is.Empty);
                UiPlayer.Click(UiPlayer.Button(root.Hud,"DIRECTOR ▾")); yield return null;
                yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.CardInstanceId.Equals(source.Id)));
                Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause), "Replaying the retained prefix reproduces the exact pause.");
                Assert.That(runtime.RestoreSnapshot(pause), Is.True);
                yield return null;
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("may-perform-primary"));
                yield return May(root,accept);
                if (accept)
                {
                    for (var iteration=0; iteration<2; iteration++)
                    {
                        Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("target"));
                        // MILESTONE 8.86: Battle Fury's own performance moves a unit, and this
                        // journey used to press through both instances without ever reading the
                        // board. The subject is named, its space read, and the move asserted.
                        var moved = runtime.PendingEffectChoice.Candidates
                            .OrderBy(value => UnitName(value.Value), StringComparer.Ordinal).First();
                        var unit = moved.Value;
                        var stood = SpaceOf(runtime, unit);
                        yield return Pick(root, moved);
                        var to = FirstClickableSpace(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            OfferedSpaces(runtime));
                        yield return Space(root, to);
                        Assert.That(SpaceOf(runtime, unit), Is.EqualTo(to),
                            "Battle Fury's performance " + iteration + " really moved the unit it " +
                            "named. " + Remaining(runtime));
                        Assert.That(Distance(SpaceOf(runtime, unit), stood), Is.EqualTo(1),
                            "exactly the one printed space. " + Remaining(runtime));
                        if (iteration == 0) yield return May(root);
                    }
                }
                Assert.That(runtime.Cards.Single(card => card.Id.Equals(source.Id)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                var howl = runtime.Cards.Single(card => card.OwnerId.Equals(actor) && card.DefinitionReference.DefinitionId.Equals(GarrusCardContent.ChillingHowl));
                Assert.That(runtime.IsActiveEffectSource(howl.Id), Is.True);
                Assert.That(runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(actor)).TrayCards.Single(card => card.CardInstanceId.Equals(howl.Id)).IsActiveEffectSource, Is.True);
                Assert.That(runtime.BoardView.LitAuraTiles.Count, Is.GreaterThan(0));
                Healthy(runtime);
            }
        }
    }
}
