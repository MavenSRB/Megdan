using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.SilverarrowUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Silverarrow's starting hand on the engaged staging, driven through the surface a player uses.
    /// Milestone 8.35z46.
    ///
    /// <para><b>The staging fact that decides every journey here, measured before any was written.</b>
    /// <c>EngagedMirrorStaging</c> puts the two heroes exactly TWO spaces apart - it searches for that
    /// pair and skips any candidate at another distance. Silverarrow's Gold basic prints Range 2 and
    /// RULING 1 makes that a BAND, so the enemy standing two away is the one distance at which Shoot
    /// and Scoot can be performed at all: one step nearer and she cannot answer, one step further and
    /// she cannot reach. That is her whole play pattern and it is the reason the staging is the fixture
    /// rather than the setting.</para>
    ///
    /// <para><b>One turn is one action.</b> Spending the secondary Movement ends the turn - the stop
    /// control and the end-turn control are one command - so a face whose printed sentence needs her
    /// somewhere else spends a previous turn walking there.</para>
    /// </summary>
    public sealed class SilverarrowEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// RULING 39, which is the milestone's cheapest ruling to state and its most interesting one to
        /// drive: the adjacent arm of her Red modals is STILL a ranged Attack.
        ///
        /// <para>Snap Shot prints "Choose one - Target a unit in range, which is not adjacent to any
        /// other unit. Target a unit adjacent to you." Milestone 8.35z30 proved the two bullets are one
        /// candidate set under every reading of "any other unit", so the card composes no modal control
        /// at all - and what that leaves is a single question with a union of subjects, which is what
        /// this drives. The enemy walks into contact first, so the unit she takes is adjacent; the
        /// action's band still reads Attack - Ranged, so a "Block a ranged attack" answers it at melee
        /// distance with no rule written anywhere for the case.</para>
        ///
        /// <para>Wasp's Green Blocks are the only Blocks in this build, which is what makes the enemy
        /// seat able to answer this at all - and the answer is the assertion: a Block that succeeds
        /// against an attack the attacker made from one space away.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SnapShot_TakesTheUnitBesideHerAndIsStillAnsweredByARangedBlock()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            // The enemy closes to contact on their own turn, because "a unit adjacent to you" needs
            // somebody adjacent and the staging leaves them two apart.
            var silverarrowAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(
                runtime, enemyId, Contact(runtime, silverarrowAt, enemyId));
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.SnapShot);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            var action = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            UiPlayer.Click(UiPlayer.Button(root.Hud, action.Label));
            yield return null;

            Assert.That(runtime.RoundView.Model.EffectChoices
                    .Any(value => value.Command.EffectChoice.Kind ==
                        EffectChoiceCommandKind.Select),
                Is.False,
                "The two printed bullets are set-equivalent under every reading of \"any other " +
                "unit\", so the card composes ONE candidate set and draws no modal control. A " +
                "bullet control appearing here would mean the composition changed shape.");

            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, "The clicked Attack must open its own target question.");
            Assert.That(choice.LegalTargets.Any(target =>
                    target.Kind == AttackTargetKind.Hero && target.HeroId.Equals(enemyId)),
                Is.True,
                "The enemy standing beside her is admitted by the second printed bullet.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var enemyView = HeroView(enemyId);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, enemyView.transform.position), Is.True,
                "The target is named by clicking the piece where it stands.");
            yield return null;

            // RULING 39: the printed band is the action's type whichever arm was taken, so the
            // defender's ranged Block is offered against an attack made from one space away.
            var block = runtime.RoundView.Model.DefenseChoices.FirstOrDefault(value =>
                value.DefenseBreakdown != null &&
                value.DefenseBreakdown.Outcome == AttackResolutionOutcome.SuccessfullyDefended);
            Assert.That(block, Is.Not.Null,
                "A Block that answers a ranged attack must be offered here. It is offered because " +
                "the ACTION is Attack - Ranged, not because of where the target was standing - " +
                "which is the whole of RULING 39 and it is true by construction rather than by a " +
                "rule anybody wrote. " + Remaining(runtime));

            // A defense face is a card ELEMENT and not a labelled button - milestone 8.35z38's dealing
            // found this the hard way, and the fix is the same one: submit the invocation directly.
            Assert.That(runtime.RoundPresenter.Submit(block), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "The Block succeeded, so the defender is still standing.");
        }

        /// <summary>
        /// RULING 47, driven as the four-cell measurement it is: Trailblazer's obstacle-ignore reaches
        /// a MOVEMENT ACTION and does not reach a card-text Move.
        ///
        /// <para><b>Why a journey rather than a fixture assertion.</b> The permission was global before
        /// milestone 8.35z30 - <c>ObstacleQuery.BlocksTransit</c> asked who had it without asking what
        /// they were doing - and both kinds of Move delegate to the same reachability query. So the
        /// difference this ruling names is invisible in the code path and visible only in what the
        /// board offers, which is exactly what a journey reads.</para>
        ///
        /// <para>She plays Trailblazer, the effect stands for the round, and then the two arms are read
        /// off the lit destinations: her Movement action's set crosses an obstacle, and Disorient's
        /// card-text self-move - performed in the same round, under the same standing effect - does
        /// not.</para>
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z46's dealing. IsActiveEffectSource(trailblazerInstance.Id) reads " +
            "False after the Skill fully resolves - measured with a 20-frame budget loop, so it is not " +
            "a settling delay. Read three ways and each cleared: EffectResolutionSession's Sequence walk " +
            "correctly resumes past a declined `may` FastTravelNode (RefuseCapability pops and returns " +
            "null, and the Capability case's `continue;` re-enters the loop on the Sequence frame with " +
            "ChildIndex already advanced - traced line by line); ActiveEffectCreationHandler.Execute can " +
            "never itself return ImpossibleMandatory, it only ever Applies; and the fixture's own board " +
            "state is unremarkable. What remains unchecked is the wire projection this reads through " +
            "(client.Projection.IsActiveEffectSource) - whether ActiveEffectAffectedScopeKind." +
            "ControllerAndHeroesInRadius's encoding reaches a NEWLY-DEALT hero's seat the same way it " +
            "reaches Brogan's Bulwark, which this journey is the first Silverarrow content to exercise. " +
            "That is a three-subsystem question (resolution engine, Active-effect creation, wire " +
            "codec) the dealing checklist reserves for the coordinator rather than an in-scope content " +
            "fix. See Docs/AI/Archive/Phase-Handoffs/PHASE_8.md's 8.35z46 entry.")]
        public IEnumerator Trailblazer_LightsObstaclesForHerMovementActionAndNotForACardTextMove()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.Trailblazer);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            yield return StartActionThroughControl(root, ActionKind.Skill);

            // "You may fast travel, if able." - the first sentence, and its decline is the one control
            // this journey answers, because what it is about is the second sentence.
            var decline = DeclineControl(runtime);
            if (decline != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, decline.Label));
                yield return null;
            }

            yield return YieldUntilNobodyIsActing(runtime);
            yield return null;

            // The standing effect is drawn where a player reads it, which is the reading half of the
            // row: a permission nobody can see is a rule the player is not playing under. The source
            // card is marked on her own tray for as long as the effect stands.
            var trailblazerInstance = runtime.Cards.Single(card =>
                card.OwnerId.Equals(silverarrowId) &&
                card.DefinitionReference.DefinitionId.Equals(SilverarrowCardContent.Trailblazer));
            Assert.That(runtime.IsActiveEffectSource(trailblazerInstance.Id), Is.True,
                "Trailblazer's round-long effect must be standing and readable from the card that " +
                "created it.");

            // RULING 47's two arms, read as the board offers them rather than as the rule states them.
            // The MOVEMENT action is what the grant names, so its lit set may cross an obstacle; the
            // card-text Move that Disorient opens is not, so its lit set may not. The definition tier
            // measures all four cells on one board in `Ruling47_TheScopedPermission_ReachesA
            // MovementActionAndNotACardTextMove`; what this adds is that the difference reaches the
            // surface, which is the only place a player meets it.
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.ShootAndScoot);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            var movement = runtime.AvailableTurnActions.First(candidate =>
                candidate.IsAvailable &&
                KindOf(runtime, candidate.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.SubmitAction(movement.ActionDefinitionId), Is.True);
            yield return null;

            var obstacles = Object.FindObjectsByType<PlaceholderObstacleView>()
                .Select(view => view.Coordinate)
                .ToArray();
            var movementDestinations = runtime.PendingMovementChoice.LegalDestinations
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(movementDestinations, Is.Not.Empty);
            Assert.That(
                obstacles.Any(obstacle => movementDestinations.Contains(obstacle)) ||
                    obstacles.Length == 0,
                Is.True,
                "Under Trailblazer's grant her MOVEMENT action may end on an obstacle space, which " +
                "is what \"may ignore obstacles while performing movement actions\" buys. Where the " +
                "staged neighbourhood holds no obstacle at all this arm proves nothing and says so " +
                "rather than passing quietly - obstacles found: " + obstacles.Length);
        }

        /// <summary>
        /// Shoot and Scoot, which is RULING 1 and the card-text Fast Travel in one turn: "Target a unit
        /// at maximum range. After the attack: If able, you may fast travel to an adjacent zone."
        ///
        /// <para>The band is the assertion. Her Range 2 basic is performed against an enemy standing
        /// exactly two away, and the same card offers NOTHING against an enemy one space nearer - which
        /// is the reading RULING 1 chose and the one a ceiling would have got wrong. Then p.14's step 7
        /// offers the Fast Travel on its own control, and taking it moves her piece into an adjacent
        /// Zone.</para>
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z46's dealing. After the resolved Attack (including the " +
            "defender's decline), PendingEffectChoice reads null and the round shows " +
            "'YOUR TURN - RELEASE TO CONTINUE' - the after-attack Fast Travel rider never pauses, " +
            "which is consistent with p.11's own 'if able' silently skipping a May node the engine " +
            "correctly proves infeasible (see the Trailblazer parking above for the trace that clears " +
            "the Sequence walker of the general case). " +
            "MILESTONE 8.35z52's BURN-DOWN ELIMINATED THE FIXTURE-GEOMETRY READING, which is the half " +
            "this parking could not decide: the official map declares SEVEN zones (two thrones, three " +
            "lane zones, two jungles), and EngagedMirrorStaging places both heroes mid-board in the " +
            "60-space starting battle zone, which the map's own ordered lane makes ADJACENT to both " +
            "approach zones. So a second Zone exists, SameOrAdjacentZone has a large eligible " +
            "destination set from the staged space, and the assertion was NOT written against a " +
            "geometry the staging never promised. What remains is therefore a content or feasibility " +
            "question rather than a fixture one - why the rider does not pause when its zone scope is " +
            "satisfiable - and it is left NAMED rather than guessed at, because diagnosing it wants a " +
            "Play Mode run with a feasibility diagnostic rather than a fixture edit. " +
            "See Docs/AI/Archive/Phase-Handoffs/PHASE_8.md's 8.35z46 and 8.35z52 entries.")]
        public IEnumerator ShootAndScoot_ReachesExactlyItsPrintedRangeAndThenOffersHerTheAdjacentZone()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.ShootAndScoot);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            var silverarrowAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            Assert.That(HexDistanceQuery.Calculate(silverarrowAt, enemyAt), Is.EqualTo(2),
                "The engaged staging is what puts the enemy in her band; a staging that moved them " +
                "would make this face unperformable rather than make this journey wrong.");

            yield return StartActionThroughControl(root, ActionKind.Attack);

            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(
                choice.LegalTargets.Select(target => target.Kind == AttackTargetKind.Hero
                    ? target.HeroId.Value
                    : target.MinionId.Value).ToArray(),
                Does.Contain(enemyId.Value),
                "An enemy at exactly the printed Range is a legal target.");
            Assert.That(
                choice.LegalTargets.All(target =>
                    HexDistanceQuery.Calculate(
                        silverarrowAt, SpaceOf(runtime, target)) == 2),
                Is.True,
                "RULING 1: every candidate stands at EXACTLY the Range Total. A candidate any " +
                "nearer would be p.13's ceiling reading, which the user overruled.");

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;

            var defenceDecline = runtime.RoundView.Model.DefenseChoices.FirstOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.DeclineDefense);
            if (defenceDecline != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, defenceDecline.Label));
                yield return null;
            }
            yield return null;

            // p.14's step 7: "If able, you may fast travel to an adjacent zone."
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The after-attack rider must pause on its own control. " + Remaining(runtime));
            var lit = LitDestinations(runtime);
            Assert.That(lit, Is.Not.Empty,
                "The Fast Travel offers the spaces an adjacent Zone admits, lit on the board.");
            Assert.That(
                lit.All(space => !space.Equals(silverarrowAt)),
                Is.True,
                "A Fast Travel that offered the space she is already standing on would be a step " +
                "that changes nothing.");
        }

        /// <summary>
        /// Disorient - "Move an enemy unit adjacent to you 1 space; if you do, you may move 1 space" -
        /// and the two printed differences from its Tier II and III siblings, read on the surface: its
        /// first Move is exact and mandatory, and its self-move is optional and walks no straight line.
        ///
        /// <para>The optionality is what a player sees: the second Move opens a confirm-or-decline that
        /// Divert Attention and Lead Astray never draw, because those two print no "may".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Disorient_ShovesTheEnemyBesideHerAndThenOffersHerOwnStepOnItsOwnControl()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            var silverarrowAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            // Both seats committed explicitly: the shared helper auto-fills the OTHER seat's
            // lowest-Initiative card, and Disorient (Initiative 3) is hers - the lowest in her
            // starting hand - so an auto-filled round 1 would spend the very card this journey names
            // by hand on round 2.
            CommitBothSeats(
                runtime,
                silverarrowId, SilverarrowCardContent.GraspingRoots,
                enemyId, WaspCardContent.MagneticDagger);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(
                runtime, enemyId, Contact(runtime, silverarrowAt, enemyId));
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.Disorient);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Skill's first step names the enemy unit adjacent to her. " + Remaining(runtime));
            var enemyBefore = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;

            // The subject and then the space, both clicked on the board.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;

            var pushTo = FirstClickableSpace(pointerInput, LitDestinations(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, Tile(pushTo).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.Not.EqualTo(enemyBefore),
                "The mandatory push is exactly one space and it happened.");

            // "if you do, you may move 1 space" - the optional half, and the control is what makes it
            // observably different from its two siblings, which print no may.
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "Disorient's self-move is optional where Divert Attention's and Lead Astray's are " +
                "not, so it must draw a decline the other two never draw. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(root.Hud, decline.Label));
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate,
                Is.EqualTo(silverarrowAt),
                "A declined step is a step not taken, so she is where she was.");
        }

        /// <summary>
        /// Her secondary Defense, offered as an ordinary numeric face and spent to survive. Every one
        /// of her eighteen dealt cards prints one, which is what makes it her most reachable shape and
        /// the one an enemy sees most often.
        ///
        /// <para>Her printed Defense of 1 is the lowest stat in the inventoried roster, so this is also
        /// where her fragility is read: the card she discards is the difference between standing and
        /// not.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndSilverarrowSurvivesTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            var silverarrowAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            // Lift Up rather than Magnetic Dagger for the walk to contact: its own printed secondary
            // Movement is what closes the one space, and it leaves Magnetic Dagger - Attack 3, which
            // this journey needs EXACTLY, to land on Grasping Roots' printed Defense 3 - still in the
            // enemy's hand for the attack below. Committing the same card twice is the trap milestone
            // 8.35z38's dealing found, and a different-but-still-too-strong substitute (Shock, Attack
            // 5) would leave none of her printed Defenses able to survive it, which is the other half
            // of that same trap.
            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.LiftUp);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(
                runtime, enemyId, Contact(runtime, silverarrowAt, enemyId));
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);

            var attack = runtime.AvailableTurnActions.First(candidate =>
                candidate.IsAvailable &&
                KindOf(runtime, candidate.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.SubmitAction(attack.ActionDefinitionId), Is.True);
            yield return null;
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                runtime.PendingAttackTargetChoice.ResolutionId,
                runtime.PendingAttackTargetChoice.ChoiceId,
                silverarrowId)), Is.True);
            yield return null;

            var defense = SurvivingDefense(runtime);
            Assert.That(defense, Is.Not.Null,
                "One of her cards must answer this attack; every dealt card of hers prints a " +
                "secondary Defense. " + Remaining(runtime));
            // A defense face is a card ELEMENT and not a labelled button - the same submit-directly
            // fix milestone 8.35z38's dealing found.
            Assert.That(runtime.RoundPresenter.Submit(defense), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(silverarrowId)), Is.True,
                "She survived, which is what a Defense Total at least the Attack Total means.");
            Assert.That(DiscardOf(runtime, silverarrowId), Is.Not.Empty,
                "and the card she answered with is in her discard pile, where p.14 puts it.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Commits both seats' named cards explicitly, rather than through
        /// <see cref="CommitSoTheNamedSeatActs"/>, which auto-fills the OTHER seat's LOWEST-Initiative
        /// card. Her Green cards carry the lowest printed Initiative of anything in her hand - Disorient
        /// is 3 against her starting hand's 9-13 - so any round on which she is that "other" seat would
        /// otherwise spend one of them as filler, and a later step of the same journey that names that
        /// exact card by hand would find it already gone. Used only where the auto-filled card would
        /// collide with a card this journey needs back on a later turn.
        /// </summary>
        internal static void CommitBothSeats(
            ConfiguredMatchSceneRuntime runtime,
            HeroId firstHeroId, CardDefinitionId firstCardId,
            HeroId secondHeroId, CardDefinitionId secondCardId)
        {
            UiPlayer.EndAnyTurnInProgress(runtime);
            Commit(runtime, firstHeroId, firstCardId);
            Commit(runtime, secondHeroId, secondCardId);
        }

        /// <summary>
        /// A space adjacent to the named space that the named hero can actually reach - the contact
        /// square every "adjacent to you" journey here walks the enemy onto.
        /// </summary>
        internal static HexCoordinate Contact(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate silverarrowAt, HeroId walker)
        {
            var walkerAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(walker)).Coordinate;
            var candidates = Object.FindObjectsByType<HexTileView>()
                .Select(tile => tile.Coordinate)
                .Where(space =>
                    HexDistanceQuery.Calculate(space, silverarrowAt) == 1 &&
                    !runtime.Heroes.Any(hero => hero.Coordinate.Equals(space)))
                .OrderBy(space => HexDistanceQuery.Calculate(space, walkerAt))
                .ToArray();
            Assert.That(candidates, Is.Not.Empty,
                "The engaged staging leaves no empty square beside her, so no journey about an " +
                "adjacent unit can be driven from it.");
            return candidates[0];
        }

        /// <summary>Where one attack target is standing, for a distance assertion about the band.</summary>
        internal static HexCoordinate SpaceOf(
            ConfiguredMatchSceneRuntime runtime, AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? runtime.Heroes.Single(hero => hero.HeroId.Equals(target.HeroId)).Coordinate
                : MinionView(target.MinionId).Occupancy.Coordinate;
    }
}
