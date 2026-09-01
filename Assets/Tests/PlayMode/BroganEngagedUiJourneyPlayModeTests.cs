using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BroganUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Brogan's starting-hand cards played the way a player plays them, from the engaged staging -
    /// Wasp and Brogan two spaces apart around one Blue melee minion, with a Red ranged minion beside
    /// Brogan. Milestone 8.21.
    ///
    /// <para>Every decision each journey is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the walk that puts a piece where a printed sentence can reach it, the enemy's own turn - ride
    /// the headless command surface, exactly as milestones 8.15 and 8.20a do, and are marked where
    /// they appear.</para>
    ///
    /// <para><b>The staged board is what makes these cards reachable.</b> The Blue melee minion the
    /// staging engages both heroes around is <em>friendly</em> to Brogan, which is what puts it inside
    /// Shield's printed Radius 1 and makes the enemy's attack on it open his removal-prevention
    /// decision; the Red ranged minion standing beside him is what "target a unit adjacent to you"
    /// finds. Neither is arranged by a fixture - both are where p.6's setup and milestone 8.15's
    /// restaging put them.</para>
    /// </summary>
    public sealed class BroganEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Onslaught: "Target a unit adjacent to you. After the attack: Move into the space it
        /// occupied."
        ///
        /// <para>The after-attack half is milestone 8.21's <c>NamedSpace</c> direct Move, and what makes
        /// it worth a journey is that it asks nobody anything: the space is the one rulebook p.14's
        /// step 6 has just emptied, so the capability applies rather than opening a prompt with one
        /// answer in it. What a player sees is the token standing where the miniature was.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Onslaught_AttacksTheAdjacentMinionAndWalksIntoTheSpaceItLeft()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.Onslaught);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "Onslaught's printed Initiative 11 acts before the lowest card the enemy holds.");

            yield return PointTrayAt(root, broganId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question.");
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            var minionView = MinionView(targetId);
            var vacated = minionView.Occupancy.Coordinate;
            Assert.That(minionView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(Tile(vacated).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "The one legal Attack target carries blood-red corners before a player clicks it.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "One board click answers the whole card: no step of Onslaught asks anything else. " +
                Remaining(runtime));
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(targetId),
                "The attacked minion was defeated and removed at p.14's step 6.");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate,
                Is.EqualTo(vacated),
                "\"Move into the space it occupied\" put Brogan on the space the miniature left.");
            Assert.That(HeroView(broganId).Coordinate, Is.EqualTo(vacated),
                "and the token followed him there rather than staying where he attacked from.");

            yield return CaptureGameView("onslaught-walks-into-the-vacated-space");
        }

        /// <summary>
        /// Mad Dash: "Before the attack: Move 2 spaces in a straight line to a space adjacent to an
        /// enemy unit, then target that unit."
        ///
        /// <para>One node, two board clicks, and they are two different questions: where the charge
        /// ends, and which of the units beside that space this Attack then takes. That is why
        /// <c>choose-charge-line</c> is the one capability in milestone 8.21's table contributing both
        /// a space selection and a unit selection.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MadDash_ChargesInAStraightLineAndTargetsTheUnitBesideWhereItStops()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.MadDash);
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate;
            yield return PointTrayAt(root, broganId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            // Question one: where the charge ends. It is a space, so it arrives as a destination.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed before-attack text must stop the Attack before its target is asked for.");
            var lines = LitDestinations(runtime);
            Assert.That(lines, Is.Not.Empty,
                "The charge must offer at least one straight two-space line ending beside an enemy. " +
                Remaining(runtime));
            var destination = FirstClickableSpace(pointerInput, lines);
            Assert.That(HexDistanceQuery.Calculate(destination, startedAt), Is.EqualTo(2),
                "Every offered line is the two spaces the face prints.");
            Assert.That(Tile(destination).IsHighlighted, Is.False);
            Assert.That(Tile(destination).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Movement),
                "A before-Attack Move destination uses the same green language as every Move.");
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root);

            // Question two, on the same node: which unit beside it this Attack takes. A minion is
            // chosen rather than the enemy hero, because a hero would open p.14's Defense response and
            // that is another journey's subject.
            //
            // Brogan is not standing on the clicked space yet, and that is the node rather than the
            // click: one node asks both questions and the charge applies when both are answered, so
            // between them the board still shows him where he started. The candidates offered are the
            // proof the space took - they are the neighbours of the space he is about to occupy.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The charge's second question - which unit - must stand on the same node. " +
                Remaining(runtime));
            var candidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(candidates, Is.Not.Empty,
                "A unit selection is answered on the map, never as a prompt list. " +
                Remaining(runtime));
            var hero = Object.FindObjectsByType<PrimitiveHeroView>()
                .FirstOrDefault(view => candidates.Any(candidate =>
                    candidate.Space.Equals(view.Occupancy.Coordinate)));
            Assert.That(hero, Is.Not.Null,
                "This charge staging offers the adjacent enemy hero as well as the minion; the " +
                "hero arm is the regression for its pre-target Defense-contribution hover.");
            var heroPreview = runtime.ReadModel.PendingAttackTargetDefensePreviews
                .Single(value => value.HeroId.Equals(hero.Occupancy.HeroId));
            Assert.That(heroPreview.MinionContributions, Is.Not.Empty,
                "The staged charge hero needs a minion modifier so its hover proves a visible cue.");
            Physics.SyncTransforms();
            yield return null;
            Assert.That(pointerInput.RefreshHoverFromPointerRay(
                    new Ray(hero.transform.position + (Vector3.up * 10f), Vector3.down)), Is.True,
                "Hovering the Attack-intent hero candidate must be claimed without selecting it.");
            // RefreshHoverFromPointerRay redraws synchronously. Do not yield to the component's live
            // Update here: the automated pointer is deliberately elsewhere and would describe a
            // different hover before these assertions can inspect the supplied regression ray.
            Assert.That(Tile(hero.Occupancy.Coordinate).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "The charged hero remains a blood-red Attack target while its Defense is previewed.");
            foreach (var contribution in heroPreview.MinionContributions)
            {
                var state = runtime.BoardView.Model.Minions.Single(value =>
                    value.MinionId.Equals(contribution.SourceMinionId));
                Assert.That(Tile(state.Coordinate).CornerHighlight.ContributionKind,
                    Is.EqualTo(contribution.Amount > 0
                        ? HexCornerHighlightKind.DefensePositive
                        : HexCornerHighlightKind.DefenseNegative));
            }
            runtime.BoardView.SetHoveredCoordinate(null);
            yield return null;
            var minion = Object.FindObjectsByType<StandardMinionView>()
                .FirstOrDefault(view => view.Occupancy != null &&
                    candidates.Any(candidate => candidate.Space.Equals(view.Occupancy.Coordinate)));
            Assert.That(minion, Is.Not.Null,
                "The charge must end beside a miniature this journey can click. Offered: " +
                string.Join(", ", candidates.Select(candidate => candidate.Space.ToString())));
            var attackedId = minion.Occupancy.MinionId;
            Assert.That(Tile(minion.Occupancy.Coordinate).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "The second half names the Attack target, so it is blood red rather than inheriting " +
                "the earlier Move's colour or a generic effect colour.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, minion.transform.position), Is.True,
                "The charged-into unit is chosen by clicking it where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Both of the charge's questions are answered. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate,
                Is.EqualTo(destination),
                "and the charge applied: Brogan is standing on the space he clicked.");
            Assert.That(HeroView(broganId).Coordinate, Is.EqualTo(destination),
                "with his token there rather than where he started.");
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(attackedId),
                "and the Attack the charge set up resolved against the unit it named.");
        }

        [UnityTest]
        public IEnumerator MadDashWithoutACompleteChargeBinding_IsGreyedAndCannotBeStarted()
        {
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;

            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.MadDash);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId));
            yield return PointTrayAt(root, broganId);
            yield return null;

            var unavailable = runtime.RoundView.Model.UnavailableActions.Single(action =>
                KindOf(runtime, action.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundView.Model.Actions.Any(action =>
                    action.Command.ActionDefinitionId.Equals(unavailable.ActionDefinitionId)),
                Is.False,
                "The mandatory charge with no complete text-to-target continuation has no command.");

            var control = UiPlayer.Button(hud, unavailable.Label);
            Assert.That(control.enabledInHierarchy, Is.False,
                "The rendered Mad Dash Attack region is visibly disabled rather than silently inert.");

            var snapshot = runtime.CaptureSnapshot();
            var commandCount = runtime.AcceptedCommands.Count;
            var stale = new TypedInvocationViewModel(
                unavailable.Label, DirectorCommand.StartAction(unavailable.ActionDefinitionId));
            Assert.That(runtime.RoundPresenter.Submit(stale), Is.False,
                "A stale invocation cannot bypass the same live feasibility check.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandCount));
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            yield return CaptureGameView("mad-dash-unavailable");
        }

        /// <summary>
        /// Brutal Jab: "You may move 1 space. Push an enemy unit or a token adjacent to you up to 1
        /// space."
        ///
        /// <para>Three real controls in one Skill: a printed "may" answered beside its destinations, a
        /// pushed piece clicked where it stands, and the printed distance range answered on its own
        /// numeric controls. The last of those is the one milestone 8.21's gate found missing - the
        /// node declared its zero floor beside a mandatory policy, which
        /// <c>AdmittedDistances.ForDeclared</c> reads only under <c>UpTo</c>, so all three of Brogan's
        /// shove faces pushed their declared distance exactly and never asked.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BrutalJab_MovesOneSpaceAndPushesTheAdjacentPieceByItsOwnDistanceControls()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.BrutalJab);
            yield return PointTrayAt(root, broganId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // "You may move 1 space" - a decline beside the destinations, which is what the printed
            // `may` is, and this journey takes the move rather than declining it.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Skill must stop on its optional step.");
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline), Is.True,
                "A printed `may` is declinable, and the control has to be there. " +
                UiPlayer.Describe(hud));
            var steps = LitDestinations(runtime);
            Assert.That(steps, Is.Not.Empty, "The optional move offers its own destinations.");
            // A player takes the step that puts somebody shovable beside them, because the sentence's
            // second half is the shove: the destinations with a miniature adjacent who has somewhere
            // to go come first, and the rest are the fallback so this reads a board rather than
            // asserting one. Rulebook p.18 pushes directly away from the pusher and admits a push that
            // travels nowhere as a legal result, so a journey that named a blocked piece would be
            // asserting about a distance the board never let it take.
            var stepTo = FirstClickableSpace(
                pointerInput,
                steps.Where(space => ShovableFrom(space).HasValue)
                    .Concat(steps.Where(space => !ShovableFrom(space).HasValue))
                    .ToArray());
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(stepTo).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate,
                Is.EqualTo(stepTo), "Brogan took the optional step to the clicked space.");

            // "Push an enemy unit or a token adjacent to you" - the piece, clicked where it stands.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The push must ask which piece. " + Remaining(runtime));
            var shovable = ShovableFrom(stepTo);
            Assert.That(shovable.HasValue, Is.True,
                "The step this journey took must leave a miniature with somewhere to be pushed to. " +
                Remaining(runtime));
            var pushedFrom = shovable.Value;
            var offered = runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .Concat(runtime.RoundView.Model.EffectChoiceTargets.Select(value => value.Space))
                .ToArray();
            Assert.That(offered, Has.Member(pushedFrom),
                "and the rules must be offering that miniature. Offered: " +
                string.Join(", ", offered.Select(space => space.ToString())));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, PieceAt(pushedFrom)), Is.True,
                "The pushed piece is chosen by clicking it where it stands.");
            yield return null;

            // "up to 1 space" - the distance, on its own controls. Nought and one are both offered,
            // because "up to" is the actor's decision and not the declaration's.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The named piece must be followed by the printed distance question. " +
                Remaining(runtime));
            var distances = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(distances, Has.Length.EqualTo(2),
                "\"up to 1 space\" offers nought and one, as two controls. Shown: " +
                UiPlayer.Describe(hud));
            var farthest = distances.Last();
            UiPlayer.Click(UiPlayer.Button(hud, farthest.Label));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The Skill's three questions are answered. " + Remaining(runtime));
            Assert.That(
                Object.FindObjectsByType<StandardMinionView>()
                    .Any(view => view.Occupancy != null &&
                        view.Occupancy.Coordinate.Equals(pushedFrom)),
                Is.False,
                "The pushed miniature left the space it was standing on, which is the one space the " +
                "chosen distance sends it.");

            yield return CaptureGameView("brutal-jab-asks-for-its-push-distance");
        }

        /// <summary>
        /// Bulwark: "You may retrieve a discarded card. This turn: You and friendly units in radius
        /// cannot be moved, pushed, swapped or placed by enemy heroes."
        ///
        /// <para>The retrieve is answered on the whole printed face of the card it takes back, under
        /// milestone 8.15a's contract, and it needs a discard to have something to offer - which is why
        /// turn one is the enemy's Shock, whose rider makes Brogan spend a card. A Bulwark played over
        /// an empty discard asks nothing at all, which the offline probe for this milestone confirmed
        /// and which is the honest behaviour: p.10 does not offer a choice with no candidates.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Bulwark_RetrievesTheCardTheEnemysRiderTookAndPinsHisOwnRadius()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, broganId);

            // Setup, headlessly: the enemy's Shock takes a card out of Brogan's hand, because the
            // journey's subject is the retrieve and a discard has to exist for one.
            Commit(runtime, enemyId, WaspCardContent.Shock);
            Commit(runtime, broganId, BroganCardContent.MadDash);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Shock's 8 acts before Mad Dash's 7.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single())),
                Is.True);
            yield return DrainEffectPausesHeadlessly(runtime, 10);
            var discarded = DiscardOf(runtime, broganId);
            Assert.That(discarded, Has.Length.EqualTo(1),
                "The enemy's rider must have taken exactly one card out of Brogan's hand.");

            // Mad Dash's 7 acts after Shock's 8, so Brogan's own turn one is still owed; he holds it,
            // because what this journey is about is the card he plays on turn two.
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, "Brogan holds turn one.");
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);

            // The subject: Bulwark's printed retrieve, answered on the card's own face.
            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.Bulwark);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "Bulwark's printed Initiative 12 acts before the lowest card the enemy holds.");
            yield return PointTrayAt(root, broganId);
            var bulwarkCardId = runtime.SelectedActor.CardInstanceId;
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed `may retrieve` must stop the Skill and offer the discard.");
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline), Is.True,
                "and declining must be drawn, because the face prints `may`.");
            var faceName = NameOf(runtime, discarded[0]);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            yield return ConfirmIfStillStanding(root);

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The retrieve is answered. " + Remaining(runtime));
            Assert.That(DiscardOf(runtime, broganId), Is.Empty,
                "The card a player clicked came back out of the discard.");
            Assert.That(
                runtime.Cards.Single(card =>
                        card.OwnerId.Equals(broganId) &&
                        card.DefinitionReference.DefinitionId.Equals(discarded[0]))
                    .State.Location,
                Is.EqualTo(CardLocation.Hand),
                "and it is in his hand, which is where p.10's retrieve puts it.");

            // "This turn:" - the printed Active effect, read on his own tray as the played card's slot
            // wearing the Active outline.
            Assert.That(runtime.IsActiveEffectSource(bulwarkCardId), Is.True,
                "The performed Basic Skill is the live source of the printed radius lock.");
            yield return PointTrayAt(root, broganId);
            yield return null;
            var slot = UiPlayer.NamedElementWithText(hud, "gd-card-slot", "BULWARK");
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "The card's slot wears the Active-effect outline while the lock lives.");
        }

        /// <summary>
        /// Shield: "This round: When any friendly minion in radius is defeated you may discard a silver
        /// card. If you do, the minion is not removed."
        ///
        /// <para><b>This is milestone 8.19c1's removal-prevention decision reaching a player for the
        /// first time.</b> That milestone built the rule and recorded that no configured content could
        /// open it; Shield is the content that can. The decision belongs to Brogan while the enemy is
        /// the acting hero, so the pause has to be drawn to somebody who is not taking the turn - and
        /// because <c>HasPendingResolution</c> refuses every other turn command while it stands, a
        /// build that could open it and not answer it would be a stopped match rather than a rule.</para>
        ///
        /// <para>The effect is also read on a turn that did not raise it, which is the round-long half:
        /// Shield goes up on turn one and the question it asks arrives on turn two.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Shield_PaysASilverCardOnItsOwnFaceToKeepTheMinionAnAttackWouldRemove()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, broganId);

            yield return RaiseShieldThenAttackTheGuardedMinion(root, broganId, enemyId);

            // The pause, as the projection carries it and as the region draws it.
            var prompt = runtime.PendingRemovalPrevention;
            Assert.That(prompt, Is.Not.Null,
                "The enemy's attack on the guarded minion must stop at its removal stage. " +
                Remaining(runtime));
            Assert.That(prompt.DecidingHeroId, Is.EqualTo(broganId),
                "p.10 gives the decision to the hero whose source is offering, not to the actor.");
            Assert.That(prompt.Origin, Is.EqualTo(RemovalPreventionOrigin.AttackMinionDefeat),
                "This altitude is rulebook p.14's own Attack.");
            Assert.That(prompt.Payments, Has.Count.EqualTo(1),
                "Bulwark is the one Silver card p.6 deals him, and Shield charges a silver card.");

            var drawn = runtime.RoundView.Model.RemovalPreventionPrompt;
            Assert.That(drawn, Is.Not.Null, "and the region must have been given it to draw.");
            Assert.That(drawn.SubjectMinionId, Is.EqualTo(prompt.SubjectMinionId));
            Assert.That(drawn.Payments, Has.Count.EqualTo(prompt.Payments.Count),
                "Presentation composes no option of its own: one control per projected payment.");
            Assert.That(drawn.Decline, Is.Not.Null, "and declining is always drawn.");
            var subjectMinionView = MinionView(prompt.SubjectMinionId);
            Assert.That(subjectMinionView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(Tile(subjectMinionView.Occupancy.Coordinate).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "The threatened minion is identified by blood-red removal corners on its hex.");
            Assert.That(UiPlayer.HasButton(hud, "END TURN"), Is.False,
                "No End Turn is drawn beside an exclusive decision.");

            yield return CaptureGameView("shield-offers-to-keep-the-minion");

            // Answered by clicking the payment's whole printed face.
            var payingCard = runtime.Cards.Single(card =>
                card.Id.Equals(prompt.Payments[0].PaymentCardInstanceId));
            var faceName = NameOf(runtime, payingCard.DefinitionReference.DefinitionId);
            var subject = prompt.SubjectMinionId;
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingRemovalPrevention, Is.Null,
                "The clicked face answers the decision.");
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.Member(subject),
                "The paid-for miniature is still standing, which is the whole of what it bought.");
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(payingCard.Id)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and the silver card that paid is in his discard. The instance is re-read from the " +
                "projection rather than from the reference captured before the answer, because the " +
                "projection is rebuilt around the accepted command.");
        }

        /// <summary>
        /// The same pause, taken back and given again: an Undo across the answered decision, then a
        /// snapshot restored onto it, then the replay both of those rest on.
        ///
        /// <para>Milestone 8.19c1's own coverage could not reach this, for the reason it recorded: no
        /// configured content could open the decision, so there was no answered command to Undo across.
        /// The Undo half is proved on the live session before the capture, because a restore adopts a
        /// state rather than a history and clears the retained prefix behind it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ShieldsRemovalPreventionPause_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, broganId);

            yield return RaiseShieldThenAttackTheGuardedMinion(root, broganId, enemyId);
            Assert.That(runtime.PendingRemovalPrevention, Is.Not.Null,
                "The journey needs the decision standing. " + Remaining(runtime));
            var subject = runtime.PendingRemovalPrevention.SubjectMinionId;
            var commandsAtPause = runtime.AcceptedCommands.Count;

            // Answered, then taken back. The answer is an ordinary recorded command, so the retained
            // prefix replays every command before it and stops on the same question.
            var faceName = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(
                    runtime.PendingRemovalPrevention.Payments[0].PaymentCardInstanceId))
                .DefinitionReference.DefinitionId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause + 1),
                "One click on a payment is one accepted command.");
            Assert.That(runtime.PendingRemovalPrevention, Is.Null);

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the answer it retained the prefix of.");
            Assert.That(runtime.PendingRemovalPrevention, Is.Not.Null,
                "and the decision is standing again, replayed rather than remembered. " +
                Remaining(runtime));
            Assert.That(runtime.PendingRemovalPrevention.SubjectMinionId,
                Is.EqualTo(subject),
                "about the same miniature.");
            Assert.That(runtime.PendingRemovalPrevention.Payments, Is.Not.Empty,
                "and offering the payment it offered before, which is the re-derivation the pause " +
                "performs rather than a stored option list.");

            // A snapshot taken on the stopped decision and restored onto the same session.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);
            UiPlayer.Click(UiPlayer.Button(hud, RemovalPreventionPromptLabels.DeclineLabel));
            yield return null;
            Assert.That(runtime.PendingRemovalPrevention, Is.Null,
                "The declined decision is gone and the miniature was removed.");
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(subject));

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;
            Assert.That(runtime.PendingRemovalPrevention, Is.Not.Null,
                "The restored match is stopped on the same decision it was captured on.");
            Assert.That(runtime.PendingRemovalPrevention.Payments, Is.Not.Empty,
                "and it offers what the live pause offered, because nothing about the options is " +
                "persisted. " + Remaining(runtime));

            // and it is answerable on the same visible surface, by clicking the same real control.
            var restoredFace = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(
                    runtime.PendingRemovalPrevention.Payments[0].PaymentCardInstanceId))
                .DefinitionReference.DefinitionId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", restoredFace));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", restoredFace));
            yield return null;
            Assert.That(runtime.PendingRemovalPrevention, Is.Null,
                "The click a player made after the restore answered the restored decision.");
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.Member(subject),
                "and the miniature it paid for is standing again.");
        }

        /// <summary>
        /// p.10's secondary Defense icon, offered inside somebody else's Attack and spent on its own
        /// numeric face. Every one of Brogan's eighteen dealt cards prints one, so this is one shape on
        /// eighteen faces rather than eighteen shapes.
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndBroganSurvivesTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, broganId);

            yield return CloseTheGapAndAttackBrogan(root, broganId, enemyId);

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response. " + Remaining(runtime));
            var offers = runtime.PendingDefenseChoice.LegalDefenseOffers;
            Assert.That(offers, Is.Not.Empty,
                "Brogan always has something to discard: every dealt card of his prints a secondary " +
                "Defense value.");
            Assert.That(
                offers.All(offer => OriginOf(runtime, offer) == ActionDefinitionOrigin.Reaction),
                Is.True,
                "and every one of them is the secondary icon, because he prints no Defense primary " +
                "anywhere in his set.");

            var strongest = offers
                .OrderByDescending(offer => DefenseValueOf(runtime, offer))
                .First();
            var faceName = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(strongest.SourceCardInstanceId))
                .DefinitionReference.DefinitionId);
            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(broganId) && card.State.Location == CardLocation.Hand);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers p.14's response. Shown: " + UiPlayer.Describe(hud));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(broganId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(handBefore - 1),
                "A Defense is paid by discarding the card from hand.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(broganId)), Is.True,
                "and the printed value answered the Attack, so Brogan is still standing.");
        }

        /// <summary>
        /// p.13's Defeat and respawn, with the piece as its subject: the token leaves the board when
        /// the answer is a decline, and comes back on a clicked spawn space at his own next turn.
        /// </summary>
        [UnityTest]
        public IEnumerator Brogan_IsDefeatedByAnAttackAndRespawnsOnHisOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, broganId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return CloseTheGapAndAttackBrogan(root, broganId, enemyId);
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat; spending a face is " +
                "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndBroganSurvivesTheAttack's subject.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(broganId)), Is.False,
                "The defeated hero is off the board until he respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(broganId)), Is.False,
                "and his token is gone with him rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.Onslaught);
            yield return PointTrayAt(root, enemyId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Brogan must be offered his respawn at his own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Empty,
                "The answered respawn withdraws its options.");
            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(broganId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Brogan token stands after the respawn, not a second one beside a stale.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True,
                "and it is his own official token that came back.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Turn one raises Shield through its own control; turn two is the enemy's Attack on the Blue
        /// minion both heroes are engaged around, which is what opens the decision. Both journeys about
        /// that decision start here, and the setup half rides the headless surface.
        /// </summary>
        private static IEnumerator RaiseShieldThenAttackTheGuardedMinion(
            MatchSceneCompositionRoot root, HeroId broganId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.Shield);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "Shield's printed Initiative 6 acts before the enemy's lowest card.");
            yield return PointTrayAt(root, broganId);
            var shieldCardId = runtime.SelectedActor.CardInstanceId;
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Shield asks nothing when it is raised; its whole text is the guard it puts up.");
            Assert.That(runtime.IsActiveEffectSource(shieldCardId), Is.True,
                "The performed Skill is the live source of the printed round-long guard.");

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);

            // Turn two, and the round-long half of the reading: the guard raised last turn is still
            // the live source, and the question it asks arrives on a turn nobody raised it on.
            Commit(runtime, enemyId, WaspCardContent.Shock);
            Commit(runtime, broganId, BroganCardContent.MadDash);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Shock's 8 acts before Mad Dash's 7, so the enemy attacks inside the same round.");
            Assert.That(runtime.IsActiveEffectSource(shieldCardId), Is.True,
                "\"This round\" outlives the turn that raised it, and this is the turn that reads it.");

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var guarded = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(guarded)), Is.True,
                "The enemy's own target choice is setup: this journey's subject is what the Defeat " +
                "then asks Brogan.");
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Turn one walks the enemy into contact with her own secondary Movement; turn two is her
        /// Attack on Brogan, stopped at p.14's Defense response. The attacker closes rather than the
        /// defender, because it is the attacker who has to be able to reach.
        /// </summary>
        private static IEnumerator CloseTheGapAndAttackBrogan(
            MatchSceneCompositionRoot root, HeroId broganId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            Commit(runtime, enemyId, WaspCardContent.LiftUp);
            Commit(runtime, broganId, BroganCardContent.Shield);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Lift Up's 10 acts before Shield's 6, so the enemy walks first.");

            var broganAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .First(value => HexDistanceQuery.Calculate(value.Destination, broganAt) == 1);
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, "Brogan holds turn one.");
            }

            yield return AdvanceToNextTurn(runtime);

            Commit(runtime, enemyId, WaspCardContent.Shock);
            Commit(runtime, broganId, BroganCardContent.MadDash);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Shock's 8 acts before Mad Dash's 7.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(broganId),
                "The walked-in enemy must have Brogan as a legal target.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(broganId)), Is.True);
            yield return null;
            yield return null;
        }

        internal static HeroId OtherHeroId(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => !hero.HeroId.Equals(heroId)).HeroId;

        /// <summary>
        /// Commits the named card out of the named seat's hand and the lowest-Initiative card out of
        /// every other seat's, headlessly. The tray-clicked commitment has its own journey in
        /// <see cref="BroganUiJourneyPlayModeTests"/>, and rulebook p.7 resolves the higher printed
        /// Initiative first, so the other seats yielding is what makes the turn deterministic.
        /// </summary>
        internal static void CommitSoTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId actingHeroId, CardDefinitionId definitionId)
        {
            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray())
            {
                if (seat.Equals(actingHeroId))
                {
                    Commit(runtime, seat, definitionId);
                    continue;
                }

                var lowest = runtime.Cards
                    .Where(card => card.OwnerId.Equals(seat) &&
                        card.State.Location == CardLocation.Hand)
                    .OrderBy(card => runtime.Definitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value)
                    .ThenBy(card => card.Id.Value, System.StringComparer.Ordinal)
                    .First();
                Assert.That(runtime.SelectCommitmentCard(lowest.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }
        }

        internal static void Commit(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definitionId)
        {
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from {heroId.Value}'s hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        /// <summary>The selected actor's printed primary action, for a setup step that is not the subject.</summary>
        internal static ActionDefinitionId PrimaryActionOf(ConfiguredMatchSceneRuntime runtime) =>
            runtime.AvailableTurnActions.Single(candidate =>
                candidate.IsAvailable && OriginOf(runtime, candidate) ==
                    ActionDefinitionOrigin.PrintedPrimary).ActionDefinitionId;

        /// <summary>Answers whatever effect pauses stand, headlessly, for a setup step.</summary>
        internal static IEnumerator DrainEffectPausesHeadlessly(
            ConfiguredMatchSceneRuntime runtime, int budget)
        {
            for (var step = 0; step < budget; step++)
            {
                var choice = runtime.PendingEffectChoice;
                if (choice == null)
                {
                    yield break;
                }

                if (choice.SelectInvocations.Count > 0)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(choice.SelectInvocations[0])), Is.True);
                }
                else if (choice.ConfirmInvocation != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation)), Is.True);
                }
                else if (choice.DeclineInvocation != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation)), Is.True);
                }
                else
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("An effect pause did not close within its budget. " + Remaining(runtime));
        }

        /// <summary>
        /// Presses the pause's own confirm when one is still standing on the same step. A selection at
        /// its printed maximum is confirmed by the presenter behind the click; one below it is not, and
        /// the two are told apart by asking rather than by assuming.
        /// </summary>
        internal static IEnumerator ConfirmIfStillStanding(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm == null)
            {
                yield break;
            }

            UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
            yield return null;
        }

        /// <summary>Drains whatever end-of-round pauses stand until the next turn can be committed.</summary>
        internal static IEnumerator AdvanceToNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "The end of round must complete within a bounded number of answers.");
                AnswerAnyEndOfRoundPause(runtime);
                yield return null;
            }

            yield return null;
        }

        private static void AnswerAnyEndOfRoundPause(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                {
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId),
                        Is.True);
                }

                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                return;
            }

            if (runtime.PausedSystemTransitionOptions.Count > 0)
            {
                Assert.That(
                    runtime.AnswerPausedSystemTransition(runtime.PausedSystemTransitionOptions[0]),
                    Is.True);
                return;
            }

            Assert.That(runtime.ContinueEndOfRound(), Is.True);
        }

        internal static ActionDefinitionOrigin OriginOf(
            ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            runtime.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .Origin;

        internal static int DefenseValueOf(ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            runtime.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .BaseValues.Sum(value => value.Amount);

        internal static CardDefinitionId[] DiscardOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();

        /// <summary>
        /// The spaces a paused effect is offering, in board order. Space-valued candidates come through
        /// <c>EffectChoiceDestinations</c> and object-valued ones through <c>EffectChoiceTargets</c>;
        /// reading the wrong one is how a board-answered question looks answerless.
        /// </summary>
        internal static HexCoordinate[] LitDestinations(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();

        /// <summary>
        /// What a paused frame is still asking, for a failure message that says what the player was
        /// actually looking at rather than only that something was left over.
        /// </summary>
        internal static string Remaining(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.RoundView.Model;
            return $"status='{model.Status}' choices=[" +
                string.Join(", ", model.EffectChoices.Select(value =>
                    $"{value.Command.EffectChoice.Kind}:{value.Label}")) +
                "] targets=[" +
                string.Join(", ", model.EffectChoiceTargets.Select(value => value.Space.ToString())) +
                "] destinations=[" +
                string.Join(", ", model.EffectChoiceDestinations.Select(value =>
                    value.Command.EffectChoice.Candidate.Value.Coordinate.ToString())) +
                "] prevention=" +
                (runtime.PendingRemovalPrevention == null ? "none" : "standing");
        }

        /// <summary>
        /// Where a miniature standing beside <paramref name="pusher"/> is, when rulebook p.18's push
        /// would actually move it: the page pushes directly away, so the space it would travel to is
        /// the reflection of the pusher through the piece, and that space has to be on the map and
        /// empty. Returns the first such miniature's space, or nothing when the board offers none.
        /// </summary>
        internal static HexCoordinate? ShovableFrom(HexCoordinate pusher)
        {
            foreach (var view in Object.FindObjectsByType<StandardMinionView>())
            {
                if (view.Occupancy == null)
                {
                    continue;
                }

                var at = view.Occupancy.Coordinate;
                if (HexDistanceQuery.Calculate(at, pusher) != 1)
                {
                    continue;
                }

                var beyond = new HexCoordinate(
                    at.Q + (at.Q - pusher.Q), at.R + (at.R - pusher.R));
                // Playable rather than merely drawn: the board draws a tile for terrain too, and a
                // piece pushed against a rock travels nowhere - which is a legal p.18 result and an
                // impossible thing for this journey to assert about.
                var onTheMap = Object.FindObjectsByType<HexTileView>()
                    .Any(tile => tile.Coordinate.Equals(beyond) && tile.IsPlayable);
                var occupied = Object.FindObjectsByType<StandardMinionView>()
                        .Any(other => other.Occupancy != null &&
                            other.Occupancy.Coordinate.Equals(beyond)) ||
                    Object.FindObjectsByType<PrimitiveHeroView>()
                        .Any(hero => hero.Coordinate.Equals(beyond));
                if (onTheMap && !occupied)
                {
                    return at;
                }
            }

            return null;
        }

        /// <summary>The world position of whichever piece stands on this space.</summary>
        internal static Vector3 PieceAt(HexCoordinate space)
        {
            var minion = Object.FindObjectsByType<StandardMinionView>()
                .FirstOrDefault(view => view.Occupancy != null &&
                    view.Occupancy.Coordinate.Equals(space));
            if (minion != null)
            {
                return minion.transform.position;
            }

            var hero = Object.FindObjectsByType<PrimitiveHeroView>()
                .FirstOrDefault(view => view.Coordinate.Equals(space));
            Assert.That(hero, Is.Not.Null, $"No piece is drawn on {space}.");
            return hero.transform.position;
        }

        /// <summary>Whether any hero or minion now stands on this space.</summary>
        internal static bool OccupantAt(ConfiguredMatchSceneRuntime runtime, HexCoordinate space) =>
            runtime.Heroes.Any(hero => hero.Coordinate.Equals(space)) ||
            Object.FindObjectsByType<StandardMinionView>()
                .Any(view => view.Occupancy != null && view.Occupancy.Coordinate.Equals(space));

        /// <summary>
        /// The first offered space whose own camera ray reaches the board rather than a hero piece
        /// standing over it. <c>PrimitiveHeroPointerInput</c> offers a ray to the hero selectors before
        /// the tiles, so a space drawn behind a piece is answered by clicking elsewhere on it.
        /// </summary>
        internal static HexCoordinate FirstClickableSpace(
            PrimitiveHeroPointerInput pointerInput, IReadOnlyList<HexCoordinate> spaces)
        {
            var camera = pointerInput.EventCamera;
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>()
                .Select(view => view.GetComponent<CapsuleCollider>())
                .Where(capsule => capsule != null)
                .ToArray();
            foreach (var space in spaces)
            {
                var tile = Tile(space);
                var screen = camera.WorldToScreenPoint(tile.transform.position);
                var ray = camera.ScreenPointToRay(new Vector2(screen.x, screen.y));
                if (!heroes.Any(capsule => capsule.Raycast(ray, out _, 500f)))
                {
                    return space;
                }
            }

            Assert.Fail(
                "Every offered space is drawn behind a hero piece, so none of them can be clicked: " +
                string.Join(", ", spaces.Select(space => space.ToString())));
            return default;
        }

        /// <summary>
        /// One targeted Game View capture of what is on screen right now, for the non-batch pass the
        /// gate requires. A batch run skips it, so the same journeys serve both tiers.
        /// </summary>
        internal static IEnumerator CaptureGameView(string name)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var path = System.IO.Path.GetFullPath($"tmp/8-21-{name}.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(System.IO.File.Exists(path), Is.True,
                $"The targeted Game View evidence must be captured under tmp/ as {name}.png.");
        }

        internal static PrimitiveHeroView HeroView(HeroId heroId) =>
            Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(heroId));

        internal static StandardMinionView MinionView(MinionId minionId) =>
            Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null && view.Occupancy.MinionId.Equals(minionId));

        internal static HexTileView Tile(HexCoordinate coordinate) =>
            Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(coordinate));

        internal static IEnumerator LoadStagedScene(SceneCardContent content)
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent = content;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }
    }
}
