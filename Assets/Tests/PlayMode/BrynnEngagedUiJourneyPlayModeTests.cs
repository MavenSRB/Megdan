using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.BrynnUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Brynn the Seeker at rulebook p.15's Tier II stage: the two Red siblings, the two Blue siblings
    /// and the two Green ones, driven through real pointer events and camera rays. Milestone 8.69h.
    ///
    /// <para>The staging is milestone 8.69g's two Tier II arms, and which arm a journey loads is decided
    /// by the face it drives: the FIRST sibling of every colour is Elevated Ambush, Cover Tracks and Log
    /// Trap, and the SECOND is Split Attack, Mountain Guide and True Grit. Both keep his Gold and Silver
    /// basics in hand.</para>
    ///
    /// <para><b>TIER II IS THE LAST STAGE AT WHICH THE NEGATIVE ARM OF HIS PREDICATE EXISTS AT ALL</b>,
    /// and that is a fact about p.16 rather than about these journeys. Milestone 8.69g's Tier III
    /// configurations unlock Over the Top on his seat, and its permanent ability makes EVERY enemy hero
    /// count as adjacent to 3 or more obstacles while he acts (BR-Q2) - so no configured Tier III match
    /// can show a clause of his refusing. Each Tier II journey below therefore drives BOTH arms of the
    /// clause its face prints, and the Tier III fixture drives the widened arm and says so.</para>
    /// </summary>
    public sealed class BrynnEngagedUiJourneyPlayModeTests
    {
        // -------------------------------------------------------------------------------------------
        // The Red ladder's Tier II
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Elevated Ambush: "Target a unit adjacent to you. If you target a hero adjacent to 3 or more
        /// obstacles, +2 attack." The same printed sentence High Ground carries, one attack value
        /// higher - and the journey reads the printed 5 and the printed plus separately in the Attack
        /// total on the decision window, so a bonus folded into the base would fail here.
        /// </summary>
        [UnityTest]
        public IEnumerator ElevatedAmbushAddsItsPrintedTwoBesideThreeObstacles()
        {
            foreach (var boxedIn in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierTwoEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.ElevatedAmbush, BrynnCardContent.Decoy },
                    enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                    enemySpace: boxedIn ? BoxedIn : InTheOpen);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Foes(runtime)[0];
                Assert.That(
                    ObstaclesBeside(runtime, boxedIn ? BoxedIn : InTheOpen),
                    boxedIn ? Is.EqualTo(3) : Is.LessThan(3), Remaining(runtime));

                yield return BeginBrynn(
                    root, BrynnCardContent.ElevatedAmbush, ActionKind.Attack,
                    WaspCardContent.KineticRepulse);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True,
                    "\"a unit adjacent to you\". " + Remaining(runtime));
                yield return null;

                var breakdown = runtime.RoundView.Model.DefenseChoices
                    .Select(choice => choice.DefenseBreakdown)
                    .FirstOrDefault(value => value != null);
                Assert.That(breakdown, Is.Not.Null, Remaining(runtime));
                Assert.That(breakdown.AttackTotal, Is.EqualTo(boxedIn ? 7 : 5),
                    "His printed Attack 5, plus the printed 2 exactly when the target is boxed in. " +
                    "Rows: " + string.Join(", ", breakdown.AttackRows.Select(row =>
                        row.SourceLabel + " " + row.SignedAmount)) + ". " + Remaining(runtime));

                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                SeatsAgree(runtime, boxedIn ? "Elevated Ambush's plus" : "Elevated Ambush plain");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Split Attack: "Target a unit in range. If you target a hero who is adjacent to 3 or more
        /// obstacles, may repeat once on a different unit adjacent to you."
        ///
        /// <para><b>The repeat is offered BEFORE the attack lands, and that is the composition rather
        /// than an accident</b> - BR-Q16b puts both split faces at <c>CardTextTiming.BeforeTheAttack</c>,
        /// so the printed "may" is answered on the same page that bound the target and the repeated
        /// instance runs once the first one has completed. What the journey proves is the whole chain:
        /// the confirm is drawn, the repeat's own target question opens after p.14 has finished with the
        /// first target, and its legal set is exactly the enemy units ADJACENT TO BRYNN other than the
        /// one already taken - which is milestone 8.69b's
        /// <c>RepeatTargetBinding.DifferentUnitAdjacentToActor</c>.</para>
        ///
        /// <para>The refusal is the same card on the same board with the enemy hero standing in the
        /// open: the gate reads the FIRST instance's target (BR-Q5), it fails, and no confirm is drawn
        /// at all - which is milestone 8.35z15's rule that an offer with no legal answer is not an
        /// offer.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SplitAttackRepeatsOnADifferentUnitBesideHimAndIsNotOfferedInTheOpen()
        {
            foreach (var boxedIn in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.SplitAttack, BrynnCardContent.Decoy },
                    enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                    enemySpace: boxedIn ? BoxedIn : InTheOpen);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Foes(runtime)[0];
                Assert.That(
                    ObstaclesBeside(runtime, boxedIn ? BoxedIn : InTheOpen),
                    boxedIn ? Is.EqualTo(3) : Is.LessThan(3), Remaining(runtime));

                yield return BeginBrynn(
                    root, BrynnCardContent.SplitAttack, ActionKind.Attack,
                    WaspCardContent.KineticRepulse);
                Assert.That(
                    runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Count,
                    Is.GreaterThan(1),
                    "Arrange: his printed Range 2 reaches more than one enemy unit, so a repeat has " +
                    "somewhere else to go. " + Remaining(runtime));
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True);
                yield return null;

                if (!boxedIn)
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "BR-Q5: the gate reads the FIRST instance's target, and a hero in the open " +
                        "fails it - so the printed `may` is not drawn at all. " + Remaining(runtime));
                    yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                    yield return null;
                    Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                        "and nothing repeats. " + Remaining(runtime));
                    SeatsAgree(runtime, "Split Attack with no repeat");
                    Healthy(runtime);
                    continue;
                }

                var repeat = runtime.PendingEffectChoice;
                Assert.That(repeat, Is.Not.Null,
                    "\"may repeat once\" stops the page. " + Remaining(runtime));
                Assert.That(repeat.NodeId.Value, Is.EqualTo("brynn-split-repeat"));
                Assert.That(repeat.DeclineInvocation, Is.Not.Null,
                    "and a printed `may` can always simply be declined.");
                yield return May(root, accept: true);

                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                yield return null;

                var second = runtime.PendingAttackTargetChoice;
                Assert.That(second, Is.Not.Null,
                    "and the repeated instance asks for its own target once the first has " +
                    "completed. " + Remaining(runtime));
                Assert.That(second.LegalTargetHeroIds, Is.Empty,
                    "\"on a DIFFERENT unit adjacent to you\" - the hero the first instance took is " +
                    "not offered again, and no other hero stands beside him. " + Remaining(runtime));
                Assert.That(
                    second.LegalTargetMinionIds.Select(value => value.Value).ToArray(),
                    Is.EqualTo(new[] { RedRanged.Value }).AsCollection,
                    "and the repeat reaches only units ADJACENT to him, not everything his printed " +
                    "Range 2 covered. " + Remaining(runtime));
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        MinionView(RedRanged).transform.position),
                    Is.True,
                    "and the repeat's own target is answered on the board. " + Remaining(runtime));
                yield return null;
                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "p.10: \"You cannot repeat a repeated action\" - the second instance offers no " +
                    "third. " + Remaining(runtime));
                SeatsAgree(runtime, "the split repeat");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Blue ladder's Tier II
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Cover Tracks: "Swap either with a unit adjacent to you, or with an enemy hero in radius who
        /// is adjacent to 3 or more obstacles. You may move 1 space."
        ///
        /// <para>The swap is Tread Lightly's, one Radius wider; what this face adds is the printed
        /// "You may move 1 space", and BOTH of its answers are driven. Taken, the destinations are read
        /// from the space the EXCHANGE published rather than from where he started (BR-Q20d) and he ends
        /// one step off it; declined, he stays exactly where the swap put him and the action ends.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CoverTracksSwapsAndThenMovesHimOneSpaceOrLeavesHimWhereItPutHim()
        {
            foreach (var move in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierTwoEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.CoverTracks, BrynnCardContent.Decoy });
                var root = Root();
                var runtime = root.Runtime;
                var his = Brynn(runtime);
                var enemy = Foes(runtime)[0];
                Assert.That(ObstaclesBeside(runtime, BoxedIn), Is.EqualTo(3), Remaining(runtime));

                yield return BeginBrynn(root, BrynnCardContent.CoverTracks, ActionKind.Skill);
                var subject = runtime.PendingEffectChoice;
                Assert.That(subject.NodeId.Value, Is.EqualTo("tracking-swap-subject"));
                var stood = Position(runtime, his);
                var enemyStood = Position(runtime, enemy);
                yield return Pick(
                    root,
                    subject.Candidates.Single(candidate =>
                        candidate.Value.Kind == EffectValueKind.Hero));
                Assert.That(Position(runtime, his), Is.EqualTo(enemyStood),
                    "p.19's Swap put him where the enemy hero stood. " + Remaining(runtime));
                Assert.That(Position(runtime, enemy), Is.EqualTo(stood));

                var trailing = runtime.PendingEffectChoice;
                Assert.That(trailing, Is.Not.Null,
                    "\"You may move 1 space\" stops the page. " + Remaining(runtime));
                Assert.That(trailing.NodeId.Value, Is.EqualTo("tracking-swap-move"));
                Assert.That(trailing.DeclineInvocation, Is.Not.Null,
                    "and a printed `may` can always simply be declined.");
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, enemyStood) == 1),
                    Is.True,
                    "BR-Q20d: the move is measured from the space the exchange published, which is " +
                    "where he is standing now. " + Remaining(runtime));

                if (move)
                {
                    var step = OfferedSpaces(runtime).First();
                    yield return Space(root, step);
                    Assert.That(Position(runtime, his), Is.EqualTo(step));
                    Assert.That(HeroView(his).Coordinate, Is.EqualTo(step));
                }
                else
                {
                    yield return May(root, accept: false);
                    Assert.That(Position(runtime, his), Is.EqualTo(enemyStood),
                        "A declined `may` leaves him exactly where the swap put him. " +
                        Remaining(runtime));
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, move ? "Cover Tracks' trailing step" : "Cover Tracks declined");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Mountain Guide: "You may move a friendly unit adjacent to you up to 2 spaces. If an enemy
        /// hero in radius is adjacent to 3 or more obstacles, move a different friendly unit in radius
        /// up to 2 spaces."
        ///
        /// <para><b>Both printed sentences, and the condition between them.</b> The first is a `may`
        /// over the one friendly unit in p.19's ring; the second opens only when the presence predicate
        /// holds, names a DIFFERENT friendly unit, and is mandatory once it does - with "up to" allowing
        /// a walk of nought, which is the occupied origin among the destinations rather than a decline
        /// button (BR-Q9). The same card on a board where the enemy hero stands in the open runs the
        /// first sentence and stops, which is the negative arm the Tier III fixture cannot reach.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MountainGuideMovesASecondFriendlyUnitOnlyWhenAHeroInRadiusIsBoxedIn()
        {
            foreach (var boxedIn in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.MountainGuide, BrynnCardContent.Decoy },
                    enemySpace: boxedIn ? BoxedIn : InTheOpen);
                var root = Root();
                var runtime = root.Runtime;
                Assert.That(
                    ObstaclesBeside(runtime, boxedIn ? BoxedIn : InTheOpen),
                    boxedIn ? Is.EqualTo(3) : Is.LessThan(3), Remaining(runtime));

                yield return BeginBrynn(root, BrynnCardContent.MountainGuide, ActionKind.Skill);
                var first = runtime.PendingEffectChoice;
                Assert.That(first, Is.Not.Null, Remaining(runtime));
                Assert.That(first.NodeId.Value, Is.EqualTo("guide-first-unit"));
                Assert.That(first.DeclineInvocation, Is.Not.Null,
                    "\"You MAY move a friendly unit adjacent to you\". " + Remaining(runtime));
                Assert.That(
                    first.Candidates.Select(candidate => candidate.Value.MinionId.Value).ToArray(),
                    Is.EqualTo(new[] { BlueMelee.Value }).AsCollection,
                    "p.17's Friendly covers a minion and never Brynn himself, and the engagement " +
                    "leaves exactly one friendly unit in his ring. " + Remaining(runtime));
                var movedFrom = MinionView(BlueMelee).Occupancy.Coordinate;
                yield return Pick(root, first.Candidates.Single());

                var firstWalk = runtime.PendingEffectChoice;
                Assert.That(firstWalk.NodeId.Value, Is.EqualTo("guide-first-walk"));
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, movedFrom) <= 2),
                    Is.True,
                    "\"up to 2 spaces\". " + Remaining(runtime));
                Assert.That(OfferedSpaces(runtime), Contains.Item(movedFrom),
                    "and \"up to\" includes nought, drawn as the unit's own occupied space rather " +
                    "than as a decline. " + Remaining(runtime));
                // MEASURED, and the choice is load-bearing: the friendly minion at (-3,2) is ONE of
                // the three obstacles that box the enemy hero in, so a walk that took it out of his
                // ring would switch off the presence the second sentence reads - and the journey
                // would then be measuring its own staging rather than the printed condition. The
                // destination taken is therefore two spaces from where it stood AND still beside the
                // enemy hero.
                var boxedInAt = Position(runtime, Foes(runtime)[0]);
                var firstTo = OfferedSpaces(runtime).First(space =>
                    Distance(space, movedFrom) == 2 &&
                    (!boxedIn || Distance(space, boxedInAt) == 1));
                yield return Space(root, firstTo);
                Assert.That(MinionView(BlueMelee).Occupancy.Coordinate, Is.EqualTo(firstTo),
                    "and the friendly unit stands where Brynn walked it, two spaces off. " +
                    Remaining(runtime));
                Assert.That(
                    ObstaclesBeside(runtime, boxedInAt),
                    boxedIn ? Is.EqualTo(3) : Is.LessThan(3),
                    "and the walk left the enemy hero's own obstacle count where it was. " +
                    Remaining(runtime));

                if (!boxedIn)
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "No enemy hero in radius is adjacent to 3 or more obstacles, so the second " +
                        "printed sentence never opens. " + Remaining(runtime));
                    SeatsAgree(runtime, "Mountain Guide's first sentence alone");
                    Healthy(runtime);
                    continue;
                }

                var second = runtime.PendingEffectChoice;
                Assert.That(second, Is.Not.Null,
                    "The presence holds, so the second printed sentence runs. " + Remaining(runtime));
                Assert.That(second.NodeId.Value, Is.EqualTo("guide-second-unit"));
                Assert.That(second.DeclineInvocation, Is.Null,
                    "and it is mandatory once the condition holds - the \"up to\" is on the WALK. " +
                    Remaining(runtime));
                Assert.That(
                    second.Candidates.Select(candidate => candidate.Value.MinionId.Value).ToArray(),
                    Is.EqualTo(new[] { BlueRanged.Value }).AsCollection,
                    "\"a DIFFERENT friendly unit in radius\" - the one the first sentence moved is " +
                    "excluded whether or not it moved (BR-Q9). " + Remaining(runtime));
                var secondFrom = MinionView(BlueRanged).Occupancy.Coordinate;
                yield return Pick(root, second.Candidates.Single());
                Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo("guide-second-walk"));
                Assert.That(OfferedSpaces(runtime), Contains.Item(secondFrom),
                    "and its own \"up to 2\" admits standing still. " + Remaining(runtime));
                // MILESTONE 8.86. This step used to ANSWER with that nought, and a mandatory second
                // sentence proved by a walk of nought is a sentence no journey has ever seen move a
                // piece: the "up to" floor is asserted as OFFERED above, and the answer taken below
                // is a real one. The zero arm of the same node is BR-Q9's own reading and is owned
                // offline by the composition's Edit Mode contract.
                var elsewhere = OfferedSpaces(runtime)
                    .Where(space => !space.Equals(secondFrom))
                    .ToArray();
                Assert.That(elsewhere, Is.Not.Empty,
                    "\"move a different friendly unit in radius up to 2 spaces\" - the second " +
                    "sentence has to be able to send it somewhere, or the printed distance is " +
                    "drawing nothing. Offer was: " + string.Join(", ",
                        OfferedSpaces(runtime).Select(space => space.ToString())) + ". " +
                    Remaining(runtime));
                var secondTo = elsewhere[0];
                yield return Space(root, secondTo);
                Assert.That(MinionView(BlueRanged).Occupancy.Coordinate, Is.EqualTo(secondTo),
                    "and the second friendly unit stands where Brynn walked it. " +
                    Remaining(runtime));
                Assert.That(secondTo, Is.Not.EqualTo(secondFrom),
                    "THE ASSERTION THIS STEP WAS MISSING: it really left " + secondFrom + ". " +
                    Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, "Mountain Guide's two sentences");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Green ladder's Tier II
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Log Trap: Bear Trap's printed sentence at Radius 4. What this journey adds to his Tier I
        /// trap is the other half of "if able": a hero whose hand rulebook p.7's own commitment emptied
        /// is a legal pick who gives nothing at all, which is BR-Q17e's reading and why Deadfall Trap
        /// needs a printed escalation to do anything about it.
        /// </summary>
        [UnityTest]
        public IEnumerator LogTrapTakesACardFromTheChosenHeroAndAsksNothingOfAnEmptyHand()
        {
            foreach (var withCards in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierTwoEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.LogTrap, BrynnCardContent.Decoy },
                    enemyHand: withCards
                        ? new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse }
                        : new[] { WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Foes(runtime)[0];

                yield return BeginBrynn(root, BrynnCardContent.LogTrap, ActionKind.Skill);
                Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(withCards ? 1 : 0),
                    "Arrange: p.7's commitment spends one card, so a seat staged with one holds " +
                    "nothing by the time the trap asks. " + Remaining(runtime));
                yield return Bullet(root, "trap-adjacent");
                var select = runtime.PendingEffectChoice;
                Assert.That(select.NodeId.Value, Is.EqualTo("trap-adjacent-select"));
                Assert.That(
                    select.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "BR-Q17e: an empty-handed hero is still a legal pick - the discard is not the " +
                    "selection's consumer. " + Remaining(runtime));
                yield return Pick(root, select.Candidates.Single());

                if (withCards)
                {
                    var discard = runtime.PendingEffectChoice;
                    Assert.That(discard, Is.Not.Null, Remaining(runtime));
                    Assert.That(discard.ChooserId, Is.EqualTo(enemy));
                    yield return Pick(root, discard.Candidates.First());
                    Assert.That(HandCountOf(runtime, enemy), Is.Zero, Remaining(runtime));
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "\"if able\" makes an empty hand nothing at all, and the hero is not " +
                        "defeated - only Deadfall Trap prints that. " + Remaining(runtime));
                }

                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                    "and the hero stands either way. " + Remaining(runtime));
                SeatsAgree(runtime, withCards ? "the log trap paid" : "the empty hand");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// True Grit: "You may retrieve a discarded attack card. If an enemy hero in radius is adjacent
        /// to 3 or more obstacles, move up to 3 spaces."
        ///
        /// <para><b>BR-Q8's filter is asserted as an exact set, and the staging is what makes it mean
        /// something:</b> the staged hand leaves three of his dealt five in the discard pile - Familiar
        /// Ground and Split Attack, whose printed primaries are Attacks, and Mountain Guide, whose is a
        /// Skill - and the retrieval offers exactly the first two. p.9's "a card with an Attack primary
        /// action is referred to as an Attack card" is the whole of the rule, and a pool that had
        /// widened to every discard would offer three faces here.</para>
        ///
        /// <para>The conditional move is driven on both boards: mandatory with "up to 3" once an enemy
        /// hero in radius is boxed in, and absent entirely when none is.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TrueGritRetrievesOnlyAnAttackCardAndMovesHimOnlyWhenAHeroInRadiusIsBoxedIn()
        {
            foreach (var boxedIn in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.TrueGrit, BrynnCardContent.Decoy },
                    enemySpace: boxedIn ? BoxedIn : InTheOpen);
                var root = Root();
                var runtime = root.Runtime;
                var his = Brynn(runtime);
                Assert.That(
                    DiscardsOf(runtime, his),
                    Is.EqualTo(new[]
                    {
                        BrynnCardContent.FamiliarGround.Value,
                        BrynnCardContent.MountainGuide.Value,
                        BrynnCardContent.SplitAttack.Value
                    }).AsCollection,
                    "Arrange: a staged hand discards the dealt cards it does not name, so his pile " +
                    "holds two Attack cards and one Skill. " + Remaining(runtime));

                yield return BeginBrynn(root, BrynnCardContent.TrueGrit, ActionKind.Skill);
                var retrieve = runtime.PendingEffectChoice;
                Assert.That(retrieve, Is.Not.Null, Remaining(runtime));
                Assert.That(retrieve.NodeId.Value, Is.EqualTo("grit-retrieve"));
                Assert.That(retrieve.DeclineInvocation, Is.Not.Null,
                    "\"You MAY retrieve\". " + Remaining(runtime));
                Assert.That(
                    retrieve.Candidates
                        .Select(candidate => runtime.Cards
                            .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                            .DefinitionReference.DefinitionId.Value)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray(),
                    Is.EqualTo(new[]
                    {
                        BrynnCardContent.FamiliarGround.Value, BrynnCardContent.SplitAttack.Value
                    }).AsCollection,
                    "BR-Q8: \"a discarded ATTACK card\" is p.9's Attack primary, so his discarded " +
                    "Skill is not offered. " + Remaining(runtime));

                var taken = retrieve.Candidates.Single(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .DefinitionReference.DefinitionId.Equals(BrynnCardContent.FamiliarGround));
                yield return Pick(root, taken);
                Assert.That(HeldBy(runtime, his),
                    Contains.Item(BrynnCardContent.FamiliarGround.Value),
                    "and the retrieved face is back in his hand. " + Remaining(runtime));

                if (!boxedIn)
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "No enemy hero in radius is boxed in, so the conditional move never " +
                        "opens. " + Remaining(runtime));
                    SeatsAgree(runtime, "the grit's retrieval alone");
                    Healthy(runtime);
                    continue;
                }

                var walk = runtime.PendingEffectChoice;
                Assert.That(walk, Is.Not.Null,
                    "The presence holds, so the printed move runs. " + Remaining(runtime));
                Assert.That(walk.NodeId.Value, Is.EqualTo("grit-move"));
                var stood = Position(runtime, his);
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, stood) <= 3),
                    Is.True,
                    "\"move up to 3 spaces\". " + Remaining(runtime));
                Assert.That(OfferedSpaces(runtime), Contains.Item(stood),
                    "and \"up to\" includes nought. " + Remaining(runtime));
                var destination = OfferedSpaces(runtime)
                    .First(space => Distance(space, stood) == 3);
                yield return Space(root, destination);
                Assert.That(Position(runtime, his), Is.EqualTo(destination), Remaining(runtime));
                Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination));
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, "the grit's retrieval and move");
                Healthy(runtime);
            }
        }
    }
}
