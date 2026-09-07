using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
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
            var decline = DeclineControl(runtime);
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
            var destinations = LitDestinations(runtime);
            if (destinations.Length == 0)
            {
                UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
                yield return null;
                yield return null;
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

            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, hud, Tile(destinations[0]).transform.position),
                Is.True,
                "The shove's destination is answered on the board.");
            yield return null;
            yield return null;
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

            // THE WALK IS NOT DRIVEN HERE, AND THAT IS RECORDED RATHER THAN PAPERED OVER. Milestone
            // 8.35z38 spent SEVEN complete Play Mode runs trying to travel this line and the hero
            // never left her staged space. What the runs narrowed, in order: it is not the reach (a
            // two-space end fails here and the identical two-space end at (-5, 3) succeeds in Sudden
            // Breeze's journey, on the same two contracts); it is not the driver (a projected command
            // and a camera-ray board click behave the same, and the click is what works at Tier I);
            // and it is not the offer (the end is offered, which is what the assertion above holds).
            // The one difference left standing is the STAGING: every space-clicking journey that
            // works loads the Tier I engaged arm, and both that do not load a progressed arm.
            //
            // MILESTONE 8.35z48 DOES NOT UNPARK THIS ONE EITHER, and it is now clear that the two
            // parked clicks on this staging share one cause and it is not the pointer. That milestone
            // removed a real defect in the shared pointer chain - a hero's pick volume swallowing
            // grazing board clicks - but this journey had already recorded that a PROJECTED COMMAND
            // behaves the same here, and a projected command never touches the pointer chain at all.
            //
            // Power Overwhelming's parked placement, driven to its end under 8.35z48, printed the
            // matching measurement from the live scene: the ray passes through NO hero's pick volume,
            // the click IS accepted (the offered ring narrows to the clicked space), and the Confirm
            // the prompt then offers does nothing at all when pressed four times running. Both are
            // model-side, in effect resolution rather than in input. See that journey's note for the
            // full reproduction; the cheap discriminator is `UiPlayer.DescribeBoardRay`, whose empty
            // answer rules absorption out in one run.
            //
            // That is a well-isolated observation and not a diagnosis, so it is a NAMED FOLLOW-UP
            // rather than a fix smuggled into a dealing milestone - and this journey asserts what it
            // genuinely proves rather than asserting an outcome it cannot reach. RULING 29 is a claim
            // about which ends are ADMITTED, and both assertions above hold it: the four-space line
            // through a water space and two units is offered, and so is the two-space line through a
            // unit. The rider behind the walk is proved on the same contracts by Sudden Breeze's
            // journey, and the discard half offline by MisaPrintedSetTests.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The line's own question is still the standing one, which is what this journey " +
                "reached and what it reports. " + Remaining(runtime));

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
            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));

            // RULING 28: with the Ultimate up the ceiling is TWO, so choosing one bullet must NOT
            // close the prompt. That is the whole observable difference from the printed card, and it
            // is what the ceiling being a live board question rather than a definition constant buys.
            UiPlayer.Click(UiPlayer.Button(hud, bullets[0].Label));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "RULING 28 raised the ceiling to two, so one chosen bullet leaves the modal " +
                "standing rather than closing it. A closed prompt here is the printed ceiling, " +
                "which would mean the Ultimate's first clause reached nothing. " + Remaining(runtime));

            // THE OTHER BULLET, chosen by its own label rather than by position. The standing prompt
            // still draws both controls after one selection - a selection is cumulative, not
            // consuming - so taking the first offered control a second time would re-answer the
            // bullet already chosen rather than add the second one.
            var second = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .Where(value => value.Label != bullets[0].Label)
                .ToArray();
            Assert.That(second, Is.Not.Empty,
                "and the other bullet is still offered. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, second[0].Label));
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

            // THE PLACEMENT STAYS PARKED - AND MILESTONE 8.35z48 NARROWED IT FROM A MYSTERY TO A
            // LOCATED DEFECT, WHICH IS WHY THIS NOTE IS WORTH ITS LENGTH.
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
            // NOT FIXED HERE, and deliberately: 8.35z48 is a presentation-input corrective, an
            // offered-control-that-does-nothing in effect resolution is a rules-side defect with its
            // own measurement to do, and the two are not the same milestone. Recorded as a NAMED
            // FOLLOW-UP with a reproduction that now costs one filtered run rather than eight full
            // ones. What this journey is FOR is RULING 28, and that is proved above independently of
            // the placement: one chosen bullet left the modal STANDING, the other was still offered,
            // and it was taken. RULING 30's arming is proved offline by
            // MisaPrintedSetTests.Ruling30_HangsTheUltimatesSecondClauseOnHerOwnSelfPlacements.
            Assert.That(ring, Has.Length.GreaterThan(1),
                "The Place bullet RULING 28 let her take offers its own ring, which is what makes " +
                "the second selection a real bullet rather than a control that does nothing. " +
                Remaining(runtime));

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

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, bullets[0].Label));
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
            Assert.That(
                runtime.RoundView.Model.EffectChoices.Count(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.EqualTo(2),
                "with BOTH bullets offered again - an Undo that came back offering one would have " +
                "kept the selection it claimed to drop. " + Remaining(runtime));

            // THE RESTORE HALF, taken from the pause the Undo returned to.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);

            var again = runtime.RoundView.Model.EffectChoices
                .First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            UiPlayer.Click(UiPlayer.Button(hud, again.Label));
            yield return null;
            yield return null;

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the decision it was captured on - milestone " +
                "8.35i's corrective holding for a choice paused inside an enclosing action. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node));
            Assert.That(
                runtime.RoundView.Model.EffectChoices.Count(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.EqualTo(2),
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
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "A printed `you may` is answerable both ways. " + Remaining(runtime));

            var destinations = LitDestinations(runtime);
            if (destinations.Length > 0)
            {
                Assert.That(
                    destinations.All(space =>
                        HexDistanceQuery.Calculate(space, stoodAt) > chosenDistance),
                    Is.True,
                    "Every offered space is strictly further from her than the target stands.");
                Assert.That(
                    UiPlayer.ClickBoard(
                        pointerInput, hud, Tile(destinations[0]).transform.position),
                    Is.True);
            }
            else
            {
                UiPlayer.Click(UiPlayer.Button(hud, DeclineControl(runtime).Label));
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
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(first).transform.position), Is.True);
            yield return null;

            var partner = offered.FirstOrDefault(space =>
                !space.Equals(first) &&
                HexDistanceQuery.Calculate(space, stoodAt) == firstDistance);
            Assert.That(partner, Is.Not.EqualTo(default(HexCoordinate)),
                "A unit at the same distance as the first must exist for the pair to close - the " +
                "offline measurement found seven such pairs. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(partner).transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Two selections at one distance answer the prompt. " + Remaining(runtime));
        }
    }
}
