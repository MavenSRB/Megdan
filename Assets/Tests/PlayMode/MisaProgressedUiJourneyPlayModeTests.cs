using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.DodgerUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.MisaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Misa's Tier II and Tier III faces played the way a player plays them, from the engaged staging
    /// at rulebook p.15's later stages. Milestone 8.35z38.
    ///
    /// <para><b>WHICH BRANCH A JOURNEY NAMES IS CONTENT RATHER THAN CONVENIENCE, and that is the
    /// single most important thing about this fixture.</b> Every ruling the user settled for Misa is
    /// observable on exactly one of p.15's two upgrade branches above Tier I -
    /// <see cref="MisaProgressedHands"/> records the whole split - so a fixture that let one branch
    /// stand for the other would leave half her printed set unreachable by any journey and say nothing
    /// about it. The first branch carries RULING 31's escalating free move and RULING 29's
    /// place-what-you-passed rider; the second carries RULING 32's tense pair, RULING 29's discard
    /// rider and both Swaps.</para>
    ///
    /// <para><b>The two spaces between the heroes are what makes her ranged pair measurable at
    /// all.</b> Power Shot and Thunder Shot both print Range 2, and the engaged staging holds the
    /// enemy hero at exactly two - so "the target WAS at maximum range" is TRUE on the untouched board
    /// rather than something a journey has to manufacture. Milestone 8.35z38 measured that offline
    /// before either journey was written.</para>
    ///
    /// <para><b>Her unlocked Ultimate is the only one in this build that changes the SHAPE of a
    /// decision.</b> RULING 28 raises the ceiling on her own modal from one option to two, so on the
    /// Tier III arms Watch How I Soar stops being "choose one" and becomes "choose both, in whichever
    /// order she picks them" - and RULING 30 arms a discard on her own self-placements. Both are here
    /// and nowhere else.</para>
    /// </summary>
    public sealed class MisaProgressedUiJourneyPlayModeTests
    {

        /// <summary>
        /// Milestone 8.46e. p.10's bullets are the decision window's own option controls now - the
        /// strip no longer draws a family the window has taken - so they are counted here, and each
        /// is pressed by the branch node it stands for rather than by position or typed words.
        /// </summary>
        private static DecisionWindowControlViewModel[] OfferedBullets(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.DecisionWindowView.Model;
            return model == null
                ? new DecisionWindowControlViewModel[0]
                : model.Controls.Where(control =>
                    control.Command?.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    control.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    control.Command.EffectChoice.Candidate.OptionNodeId.IsDefined).ToArray();
        }

        /// <summary>
        /// Power Shot: "Target a unit in range. After the attack: If the target was at maximum range,
        /// you may move it 1 space, to a space farther away from you."
        ///
        /// <para>RULING 32's PAST tense, judged at targeting time. Her printed Range is 2 and the
        /// engaged staging holds the enemy hero at exactly two, so the condition is satisfied by the
        /// board rather than by a step this journey takes.</para>
        ///
        /// <para><b>The shove is p.10's own `may`, and the destination is constrained rather than
        /// free:</b> "to a space farther away from you" is milestone 8.35z29's
        /// <c>DirectMoveDestinationPolicy.AwayFromActor</c>, so every offered space is strictly
        /// further from her than the target stands - which is asserted over the whole offer rather
        /// than over the one space clicked.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PowerShot_ShovesTheTargetItBoundAtMaximumRangeAwayFromHer()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.PowerShot);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Power Shot. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "p.14's step 2 asks whom, and her Red ranged pair prints \"a unit in range\". " +
                Remaining(runtime));

            // The target at EXACTLY the printed Range of two, which is what makes the past-tense
            // condition true. Preferred rather than assumed: if the staging has moved, the journey
            // takes the furthest candidate it is offered and says so.
            var candidates = TargetsWithSpaces(runtime);
            Assert.That(candidates, Is.Not.Empty, Remaining(runtime));
            // THE TARGET IS THE ENEMY HERO RATHER THAN THE FURTHEST UNIT, and milestone 8.35z38's
            // second Play Mode run is what settled that. A rider that reads "the target" needs a
            // target still standing at p.14's step 7: her printed Attack of 3 defeats a Melee minion
            // at step 6, and the after-attack text then has nothing to be about and is silently
            // skipped - which read as "the condition was false" and was nothing of the kind. The
            // enemy hero stands at exactly her printed Range of two and survives, so he is the only
            // honest subject for RULING 32 on this board.
            var chosen = candidates
                .Where(candidate => candidate.Target.Kind == AttackTargetKind.Hero)
                .OrderByDescending(candidate =>
                    HexDistanceQuery.Calculate(candidate.Space, stoodAt))
                .First();
            var chosenDistance = HexDistanceQuery.Calculate(chosen.Space, stoodAt);
            Assert.That(chosenDistance, Is.EqualTo(2),
                "The engaged staging holds a unit at exactly her printed Range of two, measured " +
                "offline before this journey was written, and RULING 32's past tense is about that " +
                "unit. Candidates: " + string.Join(", ", candidates.Select(candidate =>
                    $"({candidate.Space.Q},{candidate.Space.R})@" +
                    HexDistanceQuery.Calculate(candidate.Space, stoodAt))));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, ViewOf(chosen.Target).transform.position),
                Is.True,
                "The target is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            // p.14's DEFENSE WINDOW stands between the named target and the after-attack text, and it
            // belongs to the TARGET's own player rather than to hers. Milestone 8.35z38's first Play
            // Mode run is what put this step here: the journey asserted the rider's pause and found
            // the defense prompt instead, which is the page working in order.
            //
            // THE TARGET DEFENDS AND SURVIVES, and the third Play Mode run is what settled that it
            // must. A rider reading "the target" needs a target still on the board at step 7, and
            // declining on the enemy's behalf let her printed Attack of 3 defeat him at step 6 - after
            // which the after-attack text had nothing to be about and was silently skipped. That read
            // exactly like "the condition was false" and was nothing of the kind. So the enemy spends
            // the face that survives, which is setup rather than this journey's subject.
            for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
            {
                var surviving = SurvivingDefense(runtime);
                if (surviving == null)
                {
                    Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                }
                else
                {
                    var face = NameOfInstance(
                        runtime, surviving.DefenseBreakdown.CardInstanceId.Value);
                    UiPlayer.EnsureVisible(
                        UiPlayer.NamedElementWithText(hud, "gd-defense-card", face));
                    yield return null;
                    UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", face));
                }

                yield return null;
                yield return null;
            }

            Assert.That(
                runtime.Heroes.Any(hero => hero.HeroId.Equals(chosen.Target.HeroId)), Is.True,
                "The target has to survive p.14's step 6 for its own after-attack rider to have a " +
                "subject - a defeated target makes the whole clause silently skip. " +
                Remaining(runtime));

            // p.14's step 7. The condition read the spaces the page captured at step 2, so the shove
            // is offered.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"After the attack: If the target was at maximum range\" was true at targeting " +
                "time, so its `may` opens. " + Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            // Milestone 8.46e: the printed may is declined on the decision window, read off its model.
            var decline = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline);
            Assert.That(decline, Is.Not.Null,
                "A printed `you may` is answerable both ways, so the pause draws its own decline. " +
                Remaining(runtime));

            // THE POLICY IS ASSERTED OVER THE WHOLE OFFER, and an EMPTY offer is a legitimate answer
            // rather than a failure - "to a space farther away from you" can be satisfied by nothing
            // when every such space is occupied or off the map. The journey therefore asserts the
            // policy where there is an offer and the DECLINE where there is not, because a printed
            // `you may` whose offer is empty must still be closable: an unanswerable prompt is the
            // defect milestone 8.35z24 recorded, and leaving the table stuck on one is what this arm
            // rules out.
            var stoodOn = SpaceOfTarget(runtime, chosen.Target);
            Assert.That(stoodOn, Is.Not.Null,
                "Arrange: the target is still on the board for its own rider to move. " +
                Remaining(runtime));

            var destinations = LitDestinations(runtime);
            if (destinations.Length == 0)
            {
                // MILESTONE 8.86: this arm used to conclude from the ABSENCE of an offer. It now
                // asserts why the offer is empty - the board leaves "a space farther away from you"
                // nowhere to be - and then that the target really stayed put.
                Assert.That(
                    FreeSpacesFartherFrom(runtime, stoodOn.Value, stoodAt), Is.Empty,
                    "An empty shove offer is only honest when the board has nowhere to shove to: " +
                    "every space beside the target and farther from her must be occupied or off " +
                    "the map. " + Remaining(runtime));
                UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
                yield return null;
                yield return null;
                Assert.That(SpaceOfTarget(runtime, chosen.Target), Is.EqualTo(stoodOn),
                    "and the target is standing exactly where it stood. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "A `may` whose offer is empty is still answerable by declining it, and the card " +
                    "finishes. " + Remaining(runtime));
                yield break;
            }

            Assert.That(
                destinations.All(space =>
                    HexDistanceQuery.Calculate(space, stoodAt) > chosenDistance),
                Is.True,
                "\"To a space farther away from you\" is milestone 8.35z29's AwayFromActor policy: " +
                "every offered space is strictly further from her than the target stood. Offered: " +
                string.Join(", ", destinations.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, stoodAt)}")) +
                " against the target at " + chosenDistance);

            var shoveTo = destinations[0];
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, hud, Tile(shoveTo).transform.position),
                Is.True,
                "The shove's destination is answered on the board.");
            yield return null;
            yield return null;
            // THE ASSERTION THIS JOURNEY WAS MISSING (milestone 8.86). It proved the POLICY over the
            // offer and then clicked and stopped, so a shove that lit the right ring and moved
            // nobody would have passed it.
            Assert.That(SpaceOfTarget(runtime, chosen.Target), Is.EqualTo(shoveTo),
                "\"you may move it 1 space, to a space farther away from you\" - the target is " +
                "standing on the space that was clicked. " + Remaining(runtime));
            Assert.That(
                HexDistanceQuery.Calculate(shoveTo, stoodAt), Is.GreaterThan(chosenDistance),
                "and it really is farther from her than it stood. " + Remaining(runtime));
        }

        /// <summary>
        /// Dash and Slash: "Move up to 4 spaces in a straight line, ignoring obstacles; an enemy hero
        /// you moved through discards a card, if able."
        ///
        /// <para>Her Blue Tier II on the second branch, and the one of her two Blue riders that
        /// reaches a HAND rather than the board. RULING 29 governs the line - through any obstacle,
        /// ending on a playable space - and the rider narrows the crossed unit to a HERO, which is the
        /// only difference between her two Blue riders.</para>
        ///
        /// <para><b>The obstacle crossing is reachable at this reach and not at Tier I's.</b> The
        /// offline measurement found a four-space line out of her staged space whose interior crosses
        /// ONE terrain space and ends playable - which is RULING 29's own sentence, and which a
        /// three-space line cannot reach.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DashAndSlash_OffersTheLineThroughTerrainAndUnitsThatRuling29Admits()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.DashAndSlash);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Dash and Slash. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;
            // MILESTONE 8.86: read for the rider's own assertion at the end of this journey.
            var discardedBefore = runtime.Cards.Count(card =>
                !card.OwnerId.Equals(misaId) && card.State.Location == CardLocation.DiscardPile);

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The line asks where it ends. " + Remaining(runtime));
            var ends = LitDestinations(runtime);
            Assert.That(ends, Is.Not.Empty, Remaining(runtime));
            Assert.That(
                ends.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 4), Is.True,
                "\"Up to 4 spaces\" bounds the line at four.");

            // The measured line that crosses BOTH a terrain space and a unit: (-3, 5), four spaces
            // from her staged space, whose interior holds the friendly Melee minion, one water space
            // and the friendly Ranged minion. Preferred where offered - it is RULING 29's own
            // sentence drawn on the live board - and the journey reports rather than passes if the
            // staging no longer offers it.
            var crossing = new HexCoordinate(-3, 5);
            Assert.That(ends, Does.Contain(crossing),
                "RULING 29's own sentence on the live board: a four-space straight line whose " +
                "interior holds a water space AND two units must be offered, because \"ignoring " +
                "obstacles\" is what the card prints and p.17's playable-space bound is the only " +
                "one. Offered ends: " +
                string.Join(", ", ends.Select(space => $"({space.Q},{space.R})")));

            // The two-space crossing line as well, which the same ruling admits at this longer reach.
            var chosen = new HexCoordinate(-5, 3);
            Assert.That(ends, Does.Contain(chosen),
                "The two-space crossing line must be offered at reach four as well as at reach " +
                "three. Offered: " +
                string.Join(", ", ends.Select(space => $"({space.Q},{space.R})")));

            // UN-PARKED AT MILESTONE 8.35z51, AND THE CAUSE WAS NEVER THE ONE THE PARKING SUSPECTED.
            // Milestone 8.35z38 spent SEVEN complete Play Mode runs trying to travel this line and
            // the hero never left her staged space; the runs correctly cleared the reach, the driver
            // and the offer, and were left pointing at the STAGING, because every space-clicking
            // journey that worked loaded the Tier I engaged arm and both that did not loaded a
            // progressed arm. Milestone 8.35z48 then cleared the pointer chain too, by measurement
            // rather than by argument: `UiPlayer.DescribeBoardRay` reported that the ray passes
            // through no hero's pick volume at all, so nothing was ever absorbing the click.
            //
            // What actually stood in the way was model-side, and the same one defect stopped both of
            // this staging's parked walks: `synthetic-discard-chosen-card` built its card criteria
            // from a `hero` input that an `IfAble` selection is allowed to leave unbound, and
            // `CardCharacteristicCriteria` refuses an undefined owner by construction - so the
            // capability THREW. A throw is not a failure result, so the node's own `IfAble` policy
            // could not absorb it: it escaped the resolution, faulted the command at the aggregate's
            // commit boundary, and the match rolled back to a state byte-identical to the one already
            // on screen. Same prompt, same offered ends, piece unmoved - indistinguishable at the
            // surface from a click that never landed, which is exactly why eight runs read it as an
            // input problem. Milestone 8.35z51 makes `Execute` decline an unnamed subject before it
            // builds the criteria, and the walk below now completes.
            //
            // The staging correlation the parking recorded was real and is now explained rather than
            // dismissed: it is the progressed arms that reach the rider whose selection can leave its
            // subject unbound, which is why only they faulted.
            var chosenEnd = chosen;

            // A card-text straight-line Move is p.11's own movement session - the hero TRAVELS it -
            // so the named end may take more than one click to reach, exactly as Sudden Breeze's
            // journey measured. Bounded by the printed four.
            for (var step = 0;
                step < 6 &&
                !runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate.Equals(chosenEnd);
                step++)
            {
                var remaining = LitDestinations(runtime);
                if (remaining.Length == 0)
                {
                    break;
                }

                var tile = Object.FindObjectsByType<HexTileView>()
                    .Single(value => value.Coordinate.Equals(
                        remaining.Contains(chosenEnd) ? chosenEnd : remaining[0]));
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
                yield return null;
                yield return null;
            }

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(chosenEnd),
                "The Move walks her to the end of the line she named - which is the assertion this " +
                "journey could not reach while the discard rider faulted the whole command. " +
                Remaining(runtime));
            Assert.That(
                HexDistanceQuery.Calculate(chosenEnd, stoodAt), Is.GreaterThan(0),
                "and she really travelled: a zero-space line is not a walked one. " +
                Remaining(runtime));

            // THE PRINTED RIDER, asserted rather than left unmentioned (milestone 8.86). "An enemy
            // HERO you moved through discards a card, if able" is the one half of the sentence this
            // line cannot satisfy: its interior holds a MINION and no hero at all, so the printed
            // "if able" is a step NOT TAKEN - and that is asserted here rather than allowed to pass
            // as an absence nobody named. The hero-crossing half is owned at the authoritative tier;
            // what a configured match on this staging can reach is this negative.
            var travelled = HexDistanceQuery.Calculate(chosenEnd, stoodAt);
            Assert.That(
                runtime.Heroes.Any(hero =>
                    !hero.HeroId.Equals(misaId) &&
                    HexDistanceQuery.Calculate(stoodAt, hero.Coordinate) > 0 &&
                    HexDistanceQuery.Calculate(stoodAt, hero.Coordinate) +
                        HexDistanceQuery.Calculate(hero.Coordinate, chosenEnd) == travelled),
                Is.False,
                "Arrange: no enemy HERO stands on the line she walked, which is what the rider " +
                "reads. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "\"an enemy hero you moved through discards a card, IF ABLE\" - the line crossed no " +
                "hero at all, so the step is not taken and no question is drawn. " +
                Remaining(runtime));
            Assert.That(
                runtime.Cards.Count(card =>
                    !card.OwnerId.Equals(misaId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.EqualTo(discardedBefore),
                "and nobody's pile grew by it. " + Remaining(runtime));
        }

        /// <summary>
        /// Living Tornado: "Swap two units at maximum radius."
        ///
        /// <para>RULING 1's second reach, on a SELECTION rather than a Place: "at maximum radius" is
        /// EXACTLY the printed three, so the pair must both stand on the ring. Three units do at the
        /// engaged staging, measured offline before this journey was written.</para>
        ///
        /// <para><b>It asks ONE question with two answers rather than two questions.</b> The pair
        /// selection requests exactly two selections in one prompt, which is why this journey clicks
        /// twice into one standing pause rather than watching a second pause open - and why it is not
        /// the two-question shape milestone 8.35z34's restore finding is about.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LivingTornado_SwapsTwoUnitsStandingAtExactlyItsPrintedRadius()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.LivingTornado);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Living Tornado. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Swap asks which two units. " + Remaining(runtime));
            var offered = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(offered, Has.Length.GreaterThanOrEqualTo(2),
                "Three units stand at exactly Radius 3 from her staged space, measured offline " +
                "before this journey was written, and a pair needs two of them. " +
                Remaining(runtime));
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(space, stoodAt) == 3), Is.True,
                "RULING 1: \"at maximum radius\" is EXACTLY the printed three, so every candidate " +
                "stands on the ring. A unit nearer than three in this set would be the disk reading " +
                "the ruling overruled. Offered: " + string.Join(", ", offered.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, stoodAt)}")));

            var first = offered[0];
            var second = offered[1];
            // MILESTONE 8.86: WHO stands on each of the two spaces is read BEFORE the pair is
            // answered, because "Swap two units" is a promise about where those two pieces end up
            // and this journey used to assert only that the prompt had closed.
            var firstUnit = UnitOn(runtime, first);
            var secondUnit = UnitOn(runtime, second);
            Assert.That(firstUnit, Is.Not.Null.And.Not.Empty,
                "Arrange: a piece is standing on " + first + ". " + Remaining(runtime));
            Assert.That(secondUnit, Is.Not.Null.And.Not.Empty,
                "Arrange: a piece is standing on " + second + ". " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(first).transform.position), Is.True);
            yield return null;
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(second).transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Two selections answer one prompt: the pair is asked once with a floor and a " +
                "ceiling of two, which is what closes it. " + Remaining(runtime));
            // THE ASSERTION THIS JOURNEY WAS MISSING (milestone 8.86): p.19's Swap EXCHANGES the two
            // pieces, so each of them is now standing on the space the other one stood on.
            Assert.That(UnitOn(runtime, first), Is.EqualTo(secondUnit),
                "p.19's Swap exchanges the two units the pair named: " + secondUnit +
                " is standing on " + first + ". " + Remaining(runtime));
            Assert.That(UnitOn(runtime, second), Is.EqualTo(firstUnit),
                "and " + firstUnit + " on " + second + ". " + Remaining(runtime));
        }

        /// <summary>
        /// Discipline: "Next turn: Before you perform a primary action, you may move up to 2 spaces."
        ///
        /// <para>RULING 31 at the Tier II step, and the branch this face lives on is the first one.
        /// The user's words settle it: "It says action, not actions. So just the once." What this
        /// journey proves is that the grant SURVIVES the turn that raised it - milestone 8.35z29's
        /// finding F-6 records that copying an ordinary `ThisTurn` cancellation here would have
        /// expired it at the wrong boundary.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Discipline_ArmsItsFreeStepAndCarriesItAcrossTheTurnBoundary()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var misaId = MisaSeat(runtime).HeroId;

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.Discipline);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Discipline. " + Remaining(runtime));

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var disciplineCardId = runtime.Cards
                .Single(card => card.OwnerId.Equals(misaId) &&
                    card.DefinitionReference.DefinitionId.Equals(MisaCardContent.Discipline))
                .Id;
            // ONE TURN, not the whole round - see Focus's journey, which records why: a `Next turn:`
            // grant is not a live source on the turn that raised it, and p.7's Retrieve Cards step
            // cancels it at the ROUND boundary, so draining the round would test its cancellation
            // rather than its carry.
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, Remaining(runtime));
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);

            // The next turn is OPENED before it is read - see Focus's journey, which records why: an
            // effect that applies on a turn is not a live source while p.7's commitment phase stands.
            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.SwoopIn);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);

            Assert.That(runtime.IsActiveEffectSource(disciplineCardId), Is.True,
                "\"Next turn:\" stands on the turn AFTER the one that raised it. Its absence here is " +
                "the cancellation-window defect milestone 8.35z29's finding F-6 records - a NextTurn " +
                "effect expires at TurnOnlyCancellationBeforeTurnEnded, not at TurnEnded. " +
                Remaining(runtime));

            // Regression 8.95: the real game offered no step when the next primary was a Skill.
            // The older journey stopped at "active" and never clicked that primary.
            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Discipline must interrupt the next turn's Skill before that Skill starts. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("discipline-step-move"),
                "The first choice is Discipline's optional free Movement, not Swoop In's text.");
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "Discipline must offer reachable map spaces.");
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var chosen = destinations[0];
            Assert.That(UiPlayer.ClickBoard(
                pointerInput, root.Hud, Tile(chosen).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(chosen), "The free step changes the authoritative board.");
            Assert.That(runtime.PendingEffectChoice?.NodeId.Value,
                Is.Not.EqualTo("discipline-step-move"),
                "Answering the free step resumes the held Skill instead of offering it again.");
        }

        [UnityTest]
        public IEnumerator FocusAndMastery_AlsoOfferTheirStepBeforeTheNextSkill()
        {
            var cases = new[]
            {
                (SceneCardContent.VerifiedEngagedStagedMisa,
                    MisaCardContent.Focus, "focus-step-move"),
                (SceneCardContent.VerifiedTierThreeEngagedStagedMisa,
                    MisaCardContent.Mastery, "mastery-step-move")
            };
            foreach (var (staged, greenCard, expectedNode) in cases)
            {
                yield return LoadStagedScene(staged);
                var root = Root();
                var runtime = root.Runtime;
                var misaId = MisaSeat(runtime).HeroId;
                CommitSoTheNamedSeatActs(runtime, misaId, greenCard);
                yield return YieldUntilTheNamedSeatActs(runtime, misaId);
                yield return PointTrayAt(root, misaId);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                if (runtime.IsTurnActive)
                {
                    Assert.That(runtime.SubmitEndTurn(), Is.True, Remaining(runtime));
                    yield return null;
                }
                yield return AdvanceToNextTurn(runtime);
                CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.SwoopIn);
                yield return YieldUntilTheNamedSeatActs(runtime, misaId);
                yield return PointTrayAt(root, misaId);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                Assert.That(runtime.PendingEffectChoice?.NodeId.Value,
                    Is.EqualTo(expectedNode),
                    "Each Green tier must offer its own next-turn free step before a Skill. " +
                    Remaining(runtime));

                // Milestone 8.117: the step is TAKEN, not only offered. Focus prints an exact "move 1
                // space" behind its "you may", so its refusal stands beside the offer; the "up to"
                // tiers carry their nought inside the offer instead.
                if (greenCard.Equals(MisaCardContent.Focus))
                {
                    Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Not.Null,
                        "\"you may move 1 space\" - a May a player cannot refuse is not a May. " +
                        Remaining(runtime));
                }

                var destinations = LitDestinations(runtime);
                if (destinations.Length == 0)
                {
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
                    yield return null;
                    destinations = LitDestinations(runtime);
                }

                var from = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;
                var away = destinations.Where(space => !space.Equals(from)).ToArray();
                Assert.That(away, Is.Not.Empty,
                    "The step offers a space she is not already on. " + Remaining(runtime));
                var chosen = away[0];
                Assert.That(UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                    Tile(chosen).transform.position), Is.True);
                yield return null;
                yield return null;
                Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                    Is.EqualTo(chosen), greenCard.Value + "'s free step changes the board.");
                Assert.That(runtime.PendingEffectChoice?.NodeId.Value, Is.Not.EqualTo(expectedNode),
                    "and answering it resumes the held Skill rather than offering it again.");
            }
        }

        /// <summary>
        /// Power Overwhelming: "Whenever you choose one, you may choose two different options instead,
        /// in any order. Each time after you place yourself, an enemy hero adjacent to you discards a
        /// card, if able."
        ///
        /// <para><b>RULING 28 and RULING 30 on one face, and this journey drives both.</b> The first
        /// clause raises the ceiling on her own modal from one option to two, so Watch How I Soar
        /// offers BOTH bullets and takes both; the second arms a discard on her own self-placements,
        /// which the first bullet's Place is.</para>
        ///
        /// <para><b>Watch How I Soar is the only modal in her nineteen faces</b>, so the raiser has
        /// exactly one trigger in her whole set - and in a handicapped game, where p.20 substitutes
        /// Leaf on the Wind, it has none at all. RULING 28 records that as printed rather than as a
        /// defect, and this journey is the only place the clause is reachable.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PowerOverwhelming_RaisesHerModalCeilingSoBothPrintedBulletsAreTaken()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // The rail half: p.16 draws the unlocked Ultimate faceup where a player reads it.
            var ultimate = runtime.Cards.Single(card => card.OwnerId.Equals(misaId) &&
                card.DefinitionReference.DefinitionId.Equals(MisaCardContent.PowerOverwhelming));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "This staging unlocks her Ultimate, which is what makes RULING 28 reachable at all. " +
                Remaining(runtime));

            yield return CommitEverySeat(
                root, misaId, NameOf(runtime, MisaCardContent.WatchHowISoar));
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Watch How I Soar. " + Remaining(runtime));

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "p.10's modal asks which bullets. " + Remaining(runtime));
            // Milestone 8.46e: the bullets are the decision window's own controls now.
            Assert.That(OfferedBullets(runtime), Has.Length.EqualTo(2), Remaining(runtime));

            // RULING 28: with the Ultimate up the ceiling is TWO, so choosing one bullet must NOT
            // close the prompt. That is the whole observable difference from the printed card, and it
            // is what the ceiling being a live board question rather than a definition constant buys.
            // Milestone 8.46e: the Place arm is pressed by the branch node it stands for.
            UiPlayer.ClickEffectOption(runtime, hud, "soar-place-arm");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "RULING 28 raised the ceiling to two, so one chosen bullet leaves the modal " +
                "standing rather than closing it. A closed prompt here is the printed ceiling, " +
                "which would mean the Ultimate's first clause reached nothing. " + Remaining(runtime));

            // THE OTHER BULLET, chosen by its own label rather than by position. The standing prompt
            // still draws both controls after one selection - a selection is cumulative, not
            // consuming - so taking the first offered control a second time would re-answer the
            // bullet already chosen rather than add the second one.
            // Milestone 8.46e: the other bullet is the window's control for its own branch node.
            Assert.That(UiPlayer.EffectOption(runtime, "soar-defeat-arm"), Is.Not.Null,
                "and the other bullet is still offered. " + Remaining(runtime));
            UiPlayer.ClickEffectOption(runtime, hud, "soar-defeat-arm");
            yield return null;
            yield return null;

            // BOTH BULLETS NOW RUN, in the order she picked them, so the Place she chose first is the
            // next question. It is answered on the board exactly as Watch How I Soar's own journey
            // answers it - the same ring, the same click - because this is the one place in this
            // fixture where the printed bullet and the player's input are the subject rather than
            // setup.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The first chosen bullet opens its own destination question. " + Remaining(runtime));
            var ring = LitDestinations(runtime);
            Assert.That(ring, Is.Not.Empty,
                "RULING 28 let her take the Place bullet, so its ring must be offered. " +
                Remaining(runtime));

            var landing = ring[0];
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(landing));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True,
                "The destination is chosen by clicking the space on the board. " +
                UiPlayer.DescribeBoardRay(pointerInput, tile.transform.position) +
                " - which is the first thing to read if this ever stops being claimed, because a " +
                "piece on the ray and no piece on the ray are different defects entirely.");
            yield return null;
            yield return null;

            // THE PLACEMENT IS UN-PARKED AT MILESTONE 8.35z51. This note keeps its full length,
            // because the sequence of eliminations below is what MADE the fix findable and each step
            // of it was paid for in complete Play Mode runs.
            //
            // Milestone 8.35z38 spent seven complete Play Mode runs here: the board click is accepted
            // and the piece does not move, where the identical click on the Tier I arm - Watch How I
            // Soar's own journey, same ring, same contract - moves her. Integration batch B then
            // diagnosed a real defect in the shared pointer chain (a hero's pick volume swallowing
            // grazing board clicks, fixed by 8.35z48) which produces EXACTLY that symptom, so this
            // was the obvious place to prove that fix on the live surface.
            //
            // IT IS NOT THAT, AND THAT IS NOW MEASURED RATHER THAN SUSPECTED. Driving the placement
            // to its end across three runs printed all of the following from the live scene:
            //
            //   * `UiPlayer.DescribeBoardRay` reports "the ray passes through no hero's pick volume"
            //     - so no piece was ever absorbing THIS click, before 8.35z48 or after it;
            //   * the click IS accepted: the offered destination list narrows from the whole lit ring
            //     to exactly the one space that was clicked, `[(-7, 1)]`;
            //   * the prompt then stands, reading `YOUR SPACE CHOICE - Choose 1 of 22 spaces`, with a
            //     Confirm control labelled DONE offered;
            //   * and pressing that DONE control - the way a player presses it, through
            //     `UiPlayer.Button` on the shown control - FOUR TIMES IN A ROW changes nothing at
            //     all: same status, same single offered destination, same Confirm still offered, and
            //     she is still on her staged space.
            //
            // So the click reaches the model and the model accepts it; what does not happen is the
            // confirm. That is model-side, and it fits the one other thing z38 recorded and could not
            // place - that a PROJECTED COMMAND behaved identically here, which no pointer change can
            // explain. Dash and Slash's parked walk on this same staging carries the same signature.
            //
            // AND THE CAUSE, FOUND BY MILESTONE 8.35z51 EXACTLY WHERE THAT MEASUREMENT POINTED:
            // `synthetic-discard-chosen-card` built its card criteria from a `hero` input an `IfAble`
            // selection may leave unbound, and `CardCharacteristicCriteria` refuses an undefined owner
            // by construction - so the capability THREW. A throw is not a failure result, so the
            // node's own `IfAble` could not absorb it; it escaped the resolution, faulted the command
            // at the aggregate's commit boundary, and the match rolled back to a state byte-identical
            // to the one on screen. That is precisely the four observations above, and it explains the
            // one thing no pointer change ever could: a PROJECTED COMMAND behaves identically,
            // because the fault is downstream of every input path.
            //
            // Her own Power Overwhelming is what leaves the input unbound: RULING 30 composes as an
            // `IfAble` selection of an adjacent enemy hero feeding a discard that reads its output,
            // so placing herself into open ground names nobody. `AnticipateCandidates` has stated the
            // same rule since milestone 8.21, which is why every OFFER was already correct and only
            // the executing path faulted - the same class as milestone 8.35z15's unwired probe.
            // `Execute` now declines an unnamed subject before building the criteria, publishing no
            // fact, and the placement below completes.
            Assert.That(ring, Has.Length.GreaterThan(1),
                "The Place bullet RULING 28 let her take offers its own ring, which is what makes " +
                "the second selection a real bullet rather than a control that does nothing. " +
                Remaining(runtime));

            // The click NAMES the space and the Confirm COMMITS it - which is what the parked runs
            // were pressing when nothing happened. Both halves are driven the way a player drives
            // them, and the press is conditional because a selection that has already committed
            // offers no Confirm to press.
            yield return BroganEngagedUiJourneyPlayModeTests.ConfirmIfStillStanding(root);
            yield return null;

            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate,
                Is.EqualTo(landing),
                "The Place bullet puts her on the space she clicked - the assertion eight complete " +
                "Play Mode runs could not reach while the discard rider faulted the whole command " +
                "and rolled the match back onto her staged space. " + Remaining(runtime));

            // THE SECOND BULLET'S OWN CONSEQUENCE (milestone 8.86). RULING 28 let her take both, and
            // this journey used to assert only the first one's. "Defeat a minion adjacent to you" is
            // asked from the space the Place has just put her on - so either it opens and a named
            // minion leaves the board, or no enemy minion stands beside her landing space and the
            // printed sentence has nobody, which is asserted rather than assumed.
            var besideHer = runtime.ReadModel.Minions
                .Where(minion => HexDistanceQuery.Calculate(minion.Coordinate, landing) == 1)
                .ToArray();
            var doomed = string.Empty;
            for (var budget = 0; budget < 6 && runtime.PendingEffectChoice != null; budget++)
            {
                var minions = runtime.PendingEffectChoice.SelectInvocations
                    .Where(invocation => invocation.Candidate.Value.Kind == EffectValueKind.Minion)
                    .OrderBy(
                        invocation => invocation.Candidate.Value.MinionId.Value,
                        System.StringComparer.Ordinal)
                    .ToArray();
                if (minions.Length > 0)
                {
                    doomed = minions[0].Candidate.Value.MinionId.Value;
                    Assert.That(
                        runtime.SubmitProjectedCommand(
                            DirectorCommand.AnswerEffectChoice(minions[0])),
                        Is.True,
                        "The second bullet's own question is answerable. " + Remaining(runtime));
                }
                else if (!AnswerStandingChoiceHeadlessly(runtime))
                {
                    break;
                }

                yield return null;
                yield return null;
                if (doomed.Length > 0)
                {
                    break;
                }
            }

            if (doomed.Length > 0)
            {
                Assert.That(
                    runtime.ReadModel.Minions.Any(minion => minion.MinionId.Value == doomed),
                    Is.False,
                    "\"Defeat a minion adjacent to you\" - the piece the second bullet named leaves " +
                    "the board. " + Remaining(runtime));
            }
            else
            {
                Assert.That(besideHer, Is.Empty,
                    "\"Defeat a minion adjacent to you\" named nobody, and the only honest reason " +
                    "is that no minion stands beside the space the Place put her on. Standing " +
                    "beside " + landing + ": " + string.Join(", ",
                        besideHer.Select(minion => minion.MinionId.Value)) + ". " +
                    Remaining(runtime));
            }

            // RULING 30's half: the trigger is armed against HER OWN self-placements and no others.
            // What a journey can hold it to is that the Ultimate is the source of a rule the match
            // carries, which is the reading the composition asserts from the other side.
            Assert.That(runtime.IsActiveEffectSource(ultimate.Id), Is.False,
                "p.16's permanent ability is an Ultimate RULE rather than an Active effect, so it " +
                "must not appear as one - that distinction is what keeps a permanent from being " +
                "cancelled at a turn boundary. " + Remaining(runtime));
        }

        /// <summary>
        /// The choose-BOTH modal, undone and then restored - Misa's own session-operations journey,
        /// and the pause chosen for it rather than defaulted to.
        ///
        /// <para><b>Why this pause and no other.</b> Milestone 8.35z34 found that a node asking TWO
        /// questions was not restore-safe, because a restored pause re-enters through <c>Execute</c>
        /// rather than through <c>Resume</c>. Milestone 8.35z38 checked every pause Misa's journeys
        /// cross and found NONE of that shape - her pair selection asks once with a floor and ceiling
        /// of two, and every other node of hers asks once. What she has instead is the shape no other
        /// hero has: a Choose node whose ceiling RULING 28 raised to two, which resumes into TWO
        /// CHILDREN in selection order, each able to pause on its own. A capture taken inside the
        /// second child must not come back asking the modal again.</para>
        ///
        /// <para>The Undo is driven first and the restore second, and the order is forced rather than
        /// chosen: a restore permanently disables history, so an Undo attempted after one is testing
        /// nothing. Milestone 8.35z34 measured that too.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheChooseBothModal_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;

            yield return CommitEverySeat(
                root, misaId, NameOf(runtime, MisaCardContent.WatchHowISoar));
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId), Remaining(runtime));

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var node = runtime.PendingEffectChoice.NodeId;
            var commandsAtPause = runtime.AcceptedCommands.Count;

            // Milestone 8.46e: the bullets are the decision window's own controls now, and the Place
            // arm is pressed by the branch node it stands for.
            Assert.That(OfferedBullets(runtime), Has.Length.EqualTo(2), Remaining(runtime));
            UiPlayer.ClickEffectOption(runtime, hud, "soar-place-arm");
            yield return null;

            Assert.That(runtime.AcceptedCommands.Count, Is.GreaterThan(commandsAtPause),
                "The chosen bullet has to have been accepted for an Undo to have anything to drop. " +
                Remaining(runtime));

            // THE UNDO HALF: one command at a time, back to the pause the modal opened on.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            for (var press = 0; press < 4 && runtime.AcceptedCommands.Count > commandsAtPause; press++)
            {
                UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the answer it retained the prefix of.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the modal is standing again, replayed rather than remembered. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node),
                "on the same Choose node.");
            // Milestone 8.46e: the bullets are counted on the decision window, where they stand.
            Assert.That(OfferedBullets(runtime), Has.Length.EqualTo(2),
                "with BOTH bullets offered again - an Undo that came back offering one would have " +
                "kept the selection it claimed to drop. " + Remaining(runtime));

            // THE RESTORE HALF, taken from the pause the Undo returned to.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);

            // Milestone 8.46e: a bullet is pressed on the decision window, by its branch node.
            UiPlayer.ClickEffectOption(runtime, hud, "soar-place-arm");
            yield return null;
            yield return null;

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the decision it was captured on - milestone " +
                "8.35i's corrective holding for a choice paused inside an enclosing action. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node));
            // Milestone 8.46e: the bullets are counted on the decision window, where they stand.
            Assert.That(OfferedBullets(runtime), Has.Length.EqualTo(2),
                "and it is the MODAL that is standing with both bullets, rather than a child the " +
                "restore resumed into. " + Remaining(runtime));
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restore adopts the restored state with no accepted history, which is why the " +
                "Undo half had to run first.");
        }

        /// <summary>
        /// Thunder Shot: "Target a unit in range. After the attack: If the target is not adjacent to
        /// you, you may move it 1 space, to a space farther away from you."
        ///
        /// <para>RULING 32's PRESENT tense, judged at the after-attack step rather than at targeting
        /// time. It is the same rider Power Shot prints behind a different condition, and the user's
        /// ruling is that the difference is deliberate: the target may have moved between the two
        /// moments.</para>
        ///
        /// <para><b>What this journey can and cannot show.</b> On the untouched staged board the two
        /// tenses agree - the target is at Range 2 and is not adjacent - so what a journey proves here
        /// is that the PRESENT-tense arm reads the live board and opens its rider. That the two can
        /// DISAGREE is proved where it can be constructed: on milestone 8.35z38's own showcase Attack,
        /// which prints both questions on one card, and which is the only content in the build that
        /// does.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ThunderShot_ReadsTheLiveBoardAtItsAfterAttackStepAndShovesAnUnadjacentTarget()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.ThunderShot);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Thunder Shot. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            var chosen = TargetsWithSpaces(runtime)
                .OrderByDescending(candidate =>
                    HexDistanceQuery.Calculate(candidate.Space, stoodAt))
                .First();
            var chosenDistance = HexDistanceQuery.Calculate(chosen.Space, stoodAt);
            Assert.That(chosenDistance, Is.GreaterThan(1),
                "\"Is not adjacent to you\" needs a target that is not, and the staging offers one " +
                "at exactly her printed Range of two. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, ViewOf(chosen.Target).transform.position),
                Is.True,
                "The target is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            // p.14's DEFENSE WINDOW stands between the named target and the after-attack text, and it
            // belongs to the TARGET's own player rather than to hers. Milestone 8.35z38's first Play
            // Mode run is what put this step here: the journey asserted the rider's pause and found
            // the defense prompt instead, which is the page working in order.
            //
            // THE TARGET DEFENDS AND SURVIVES, and the third Play Mode run is what settled that it
            // must. A rider reading "the target" needs a target still on the board at step 7, and
            // declining on the enemy's behalf let her printed Attack of 3 defeat him at step 6 - after
            // which the after-attack text had nothing to be about and was silently skipped. That read
            // exactly like "the condition was false" and was nothing of the kind. So the enemy spends
            // the face that survives, which is setup rather than this journey's subject.
            for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
            {
                var surviving = SurvivingDefense(runtime);
                if (surviving == null)
                {
                    Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                }
                else
                {
                    var face = NameOfInstance(
                        runtime, surviving.DefenseBreakdown.CardInstanceId.Value);
                    UiPlayer.EnsureVisible(
                        UiPlayer.NamedElementWithText(hud, "gd-defense-card", face));
                    yield return null;
                    UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", face));
                }

                yield return null;
                yield return null;
            }

            Assert.That(
                runtime.Heroes.Any(hero => hero.HeroId.Equals(chosen.Target.HeroId)), Is.True,
                "The target has to survive p.14's step 6 for its own after-attack rider to have a " +
                "subject - a defeated target makes the whole clause silently skip. " +
                Remaining(runtime));

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The present-tense condition read the LIVE board at p.14's step 7 and found the " +
                "target still not adjacent, so its `may` opens. " + Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            // Milestone 8.46e: the decline is the decision window control, read off its model.
            Assert.That(UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline), Is.Not.Null,
                "A printed `you may` is answerable both ways. " + Remaining(runtime));

            var thunderStoodOn = SpaceOfTarget(runtime, chosen.Target);
            Assert.That(thunderStoodOn, Is.Not.Null,
                "Arrange: the target is still on the board for its own rider to move. " +
                Remaining(runtime));
            var destinations = LitDestinations(runtime);
            if (destinations.Length > 0)
            {
                Assert.That(
                    destinations.All(space =>
                        HexDistanceQuery.Calculate(space, stoodAt) > chosenDistance),
                    Is.True,
                    "Every offered space is strictly further from her than the target stands.");
                var shoveTo = destinations[0];
                Assert.That(
                    UiPlayer.ClickBoard(
                        pointerInput, hud, Tile(shoveTo).transform.position),
                    Is.True);
                yield return null;
                yield return null;
                // THE ASSERTION THIS JOURNEY WAS MISSING (milestone 8.86): the present-tense arm
                // opened its rider AND the rider moved the piece it was about.
                Assert.That(SpaceOfTarget(runtime, chosen.Target), Is.EqualTo(shoveTo),
                    "\"you may move it 1 space, to a space farther away from you\" - the target is " +
                    "standing on the space that was clicked. " + Remaining(runtime));
            }
            else
            {
                // MILESTONE 8.86: the empty arm asserts WHY it is empty rather than concluding from
                // the absence, and then that the target really stayed put.
                Assert.That(
                    FreeSpacesFartherFrom(runtime, thunderStoodOn.Value, stoodAt), Is.Empty,
                    "An empty shove offer is only honest when the board has nowhere to shove to. " +
                    Remaining(runtime));
                // Milestone 8.46e: the printed may is declined on the decision window, read off its model.
                UiPlayer.ClickEffectControl(runtime, hud, EffectChoiceCommandKind.Decline);
                yield return null;
                yield return null;
                Assert.That(SpaceOfTarget(runtime, chosen.Target), Is.EqualTo(thunderStoodOn),
                    "and the target is standing exactly where it stood. " + Remaining(runtime));
            }

            yield return null;
            yield return null;
        }

        /// <summary>
        /// Storm Spirit: "Swap two units in radius and at equal distance from you."
        ///
        /// <para>The build's FIRST relational pair constraint - a condition about the two candidates
        /// with respect to each other and to her, rather than about each of them separately. Seven
        /// equal-distance pairs stand within her printed Radius 3 at the engaged staging, measured
        /// offline before this journey was written.</para>
        ///
        /// <para>It runs on the same handler as Living Tornado and asks the player for the same thing
        /// - two pieces - which is why the gate rows for the two faces declare the same shape. What
        /// differs is which pairs are admitted, and that is what this journey measures.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator StormSpirit_SwapsAPairStandingAtEqualDistanceFromHer()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMisa);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var misaId = MisaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, misaId, MisaCardContent.StormSpirit);
            yield return YieldUntilTheNamedSeatActs(runtime, misaId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(misaId),
                "The journey needs her turn open on Storm Spirit. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(misaId)).Coordinate;

            yield return PointTrayAt(root, misaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Swap asks which two units. " + Remaining(runtime));
            var offered = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(offered, Has.Length.GreaterThanOrEqualTo(2),
                "Seven equal-distance pairs stand within her printed Radius 3 at this staging, " +
                "measured offline before this journey was written. " + Remaining(runtime));
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 3), Is.True,
                "\"In radius\" is p.9's disk at the printed three - which is the difference from " +
                "Living Tornado's ring on the same handler.");

            // The relational half: the first click narrows the second, because "at equal distance
            // from you" is a condition about the PAIR. This is what makes it the build's first
            // relational constraint rather than two independent selections.
            var first = offered[0];
            var firstDistance = HexDistanceQuery.Calculate(first, stoodAt);
            // MILESTONE 8.86: the two pieces are read BEFORE the pair is answered, so the exchange
            // can be asserted afterwards. This journey used to end at "the prompt closed".
            var firstUnit = UnitOn(runtime, first);
            Assert.That(firstUnit, Is.Not.Null.And.Not.Empty,
                "Arrange: a piece is standing on " + first + ". " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(first).transform.position), Is.True);
            yield return null;

            var partner = offered.FirstOrDefault(space =>
                !space.Equals(first) &&
                HexDistanceQuery.Calculate(space, stoodAt) == firstDistance);
            Assert.That(partner, Is.Not.EqualTo(default(HexCoordinate)),
                "A unit at the same distance as the first must exist for the pair to close - the " +
                "offline measurement found seven such pairs. " + Remaining(runtime));
            var partnerUnit = UnitOn(runtime, partner);
            Assert.That(partnerUnit, Is.Not.Null.And.Not.Empty,
                "Arrange: a piece is standing on " + partner + ". " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(partner).transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Two selections at one distance answer the prompt. " + Remaining(runtime));
            // THE ASSERTION THIS JOURNEY WAS MISSING (milestone 8.86): "Swap two units in radius and
            // at equal distance from you" exchanges them, and both are still at that one distance.
            Assert.That(UnitOn(runtime, first), Is.EqualTo(partnerUnit),
                "p.19's Swap exchanges the equal-distance pair: " + partnerUnit +
                " is standing on " + first + ". " + Remaining(runtime));
            Assert.That(UnitOn(runtime, partner), Is.EqualTo(firstUnit),
                "and " + firstUnit + " on " + partner + ". " + Remaining(runtime));
            Assert.That(
                HexDistanceQuery.Calculate(partner, stoodAt), Is.EqualTo(firstDistance),
                "and the two spaces the exchange used are the same distance from her, which is the " +
                "relational constraint the pair was admitted by. " + Remaining(runtime));
        }
    }
}
