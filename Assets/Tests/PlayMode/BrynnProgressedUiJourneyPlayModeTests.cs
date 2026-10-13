using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
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
    /// Brynn the Seeker at rulebook p.15's Tier III stage with p.16's Over the Top unlocked, plus the
    /// session operations every hero owes. Milestone 8.69h.
    ///
    /// <para>Milestone 8.69g's two Tier III arms carry the two ladders: the FIRST sibling of every
    /// colour is Peak Precision, Hide Traces and Deadfall Trap, and the SECOND is Split Throw,
    /// Expedition Leader and Die Hard. Over the Top is faceup beside his dashboard on both.</para>
    ///
    /// <para><b>EVERY TIER III JOURNEY BELOW STANDS THE ENEMY HERO IN THE OPEN, and that is the point
    /// rather than an economy.</b> His Purple prints "While you are performing actions all enemy heroes
    /// in play count as adjacent to 3 or more obstacles", so at this stage a clause of his that reads
    /// the predicate reads TRUE wherever the enemy is standing - and the only honest way to show that is
    /// to put him somewhere the geometry says no. Each journey therefore asserts the obstacle count
    /// beside the target FIRST, measures it as fewer than the printed three, and then drives the clause
    /// that fires anyway. <see cref="OverTheTopWidensHisOwnClauseOnABoardThatRefusedItOneTierEarlier"/>
    /// is the control: the same geometry at Tier II, where the card is still in his deck.</para>
    /// </summary>
    public sealed class BrynnProgressedUiJourneyPlayModeTests
    {
        /// <summary>The p.15 upgrade candidate's own control, addressed the way the HUD names it.</summary>
        private static Button UpgradeControl(
            MatchSceneCompositionRoot root, SystemTransitionOptionViewModel option) =>
            UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip ==
                    (option.Detail.Length > 0 ? option.Label + " - " + option.Detail : option.Label));

        // -------------------------------------------------------------------------------------------
        // p.15 and p.16: the whole upgrade face, and the item the untaken sibling becomes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.15's Level Up is offered as whole printed FACES out of his own deck, and p.16's
        /// item comes from the sibling the Level Up did not take - read on his own dashboard rail
        /// afterwards, which is the only route by which an item of his ever exists.
        ///
        /// <para>His twelve upgrade faces print six channels between them (Initiative, Defense and
        /// Attack three times each, and Radius, Range and Movement once), so the candidate this journey
        /// takes is deliberately one whose SIBLING carries an icon: what is being proved is that the
        /// channel the untaken sibling prints is the channel that rises.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem()
        {
            yield return StageBrynn(SceneCardContent.VerifiedEngagedStagedBrynn);
            var root = Root();
            var runtime = root.Runtime;
            var his = Brynn(runtime);

            for (var budget = 0; budget < 200 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                    runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(his));
                budget++)
            {
                yield return NextRoundDecision(root);
            }

            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade),
                "The rounds reached his own p.15 Level Up. " + Remaining(runtime));
            Assert.That(runtime.Projection.PendingSystemTransitionDescription.HeroId,
                Is.EqualTo(his));

            var options = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .Where(option => option.Card != null)
                .ToArray();
            Assert.That(options, Is.Not.Empty,
                "p.15 offers whole faces rather than anonymous ordinals. " +
                UiPlayer.Describe(root.Hud));
            Assert.That(
                options.All(option => BrynnCardContent.Deck.Any(id =>
                    runtime.Cards.Single(card => card.Id.Equals(option.Card.CardInstanceId))
                        .DefinitionReference.DefinitionId.Equals(id))),
                Is.True,
                "and every candidate is one of his own thirteen deck cards.");

            var taken = options.First(option => option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(his))
                .ItemBonuses
                .Where(value => value.Key == taken.ItemIcon)
                .Select(value => value.Value)
                .SingleOrDefault();

            UiPlayer.EnsureVisible(UpgradeControl(root, taken));
            yield return null;
            UiPlayer.Click(UpgradeControl(root, taken));
            yield return null;
            yield return null;
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(taken.Card.CardInstanceId))
                    .State.Location,
                Is.EqualTo(CardLocation.Hand),
                "The chosen face is the face that reaches his hand. " + Remaining(runtime));

            for (var budget = 0; budget < 60 &&
                runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                yield return NextRoundDecision(root);
            }

            var after = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(his))
                .ItemBonuses.Single(value => value.Key == taken.ItemIcon).Value;
            Assert.That(after, Is.EqualTo(before + 1),
                "p.16's item comes from the sibling the Level Up did not take.");
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(his))
                    .ItemBonuses,
                Is.Not.Empty,
                "and a player reads it on his own dashboard rail.");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Red ladder's Tier III
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Peak Precision: "Target a unit adjacent to you. If you target a hero adjacent to 3 or more
        /// obstacles, +2 attack and After the attack: You may retrieve this card."
        ///
        /// <para><b>One printed condition governs both halves</b>, and both are driven against a hero
        /// standing in the OPEN, which only Over the Top makes a legal subject of either. The +2 is read
        /// in the Attack total on the decision window; the retrieval is p.14's step-7 pause on the card
        /// that is still resolving, and BOTH its answers are taken - confirmed, the card is back in his
        /// HAND while the Attack completes (milestone 8.69b's
        /// <c>RosterCardRetrievalKind.OwnedResolvingSourceCard</c>); declined, it is left played and
        /// goes where a spent card goes.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PeakPrecisionAddsItsTwoAndTakesItselfBackIntoHisHand()
        {
            foreach (var retrieve in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierThreeEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.PeakPrecision, BrynnCardContent.Decoy },
                    enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                    enemySpace: InTheOpen);
                var root = Root();
                var runtime = root.Runtime;
                var his = Brynn(runtime);
                var enemy = Foes(runtime)[0];
                Assert.That(ObstaclesBeside(runtime, InTheOpen), Is.LessThan(3),
                    "Arrange: the enemy hero stands in the open, so only Over the Top can make " +
                    "either half of the printed condition true. " + Remaining(runtime));

                yield return BeginBrynn(
                    root, BrynnCardContent.PeakPrecision, ActionKind.Attack,
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
                Assert.That(breakdown.AttackTotal, Is.EqualTo(7),
                    "His printed Attack 5 plus the printed 2, against a hero the geometry says is " +
                    "NOT boxed in - which is the Ultimate reaching the +2. Rows: " +
                    string.Join(", ", breakdown.AttackRows.Select(row =>
                        row.SourceLabel + " " + row.SignedAmount)) + ". " + Remaining(runtime));

                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                yield return null;

                // MEASURED at 8.69h, and this is the journey that found the milestone's one shipped
                // defect. His printed Attack 5 plus the printed 2 beats the defender's printed
                // Defense 6, so rulebook p.14's step 6 takes the target off the board BEFORE the
                // step-7 retrieval reads the same printed condition - which is precisely the case
                // BR-Q14b was written to protect, and precisely the case that failed: the overlay
                // this target qualified through was derived per enemy hero ON THE BOARD, so removing
                // him removed the answer, and the retrieval was refused exactly when the attack
                // worked. The fix is in RuleModificationQuery, where "all enemy heroes in play" now
                // reads the match's heroes; this assertion is what proves it at the player's tier.
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                    "Arrange: the attack defeated its target, so the clause below is being asked " +
                    "about a hero p.14 has already removed. " + Remaining(runtime));

                var may = runtime.PendingEffectChoice;
                Assert.That(may, Is.Not.Null,
                    "\"After the attack: You may retrieve this card\" stops the page at p.14's " +
                    "step 7, about the targeting rather than about a hero still standing. " +
                    Remaining(runtime));
                Assert.That(may.NodeId.Value, Is.EqualTo("brynn-split-retrieve-may"));
                Assert.That(may.DeclineInvocation, Is.Not.Null);
                yield return May(root, accept: retrieve);
                yield return null;

                Assert.That(
                    HeldBy(runtime, his).Contains(BrynnCardContent.PeakPrecision.Value),
                    Is.EqualTo(retrieve),
                    retrieve
                        ? "BR-Q7: the resolving card comes out of its played state and back into his " +
                          "hand, and the action still completes. "
                        : "and a declined retrieval leaves it played. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, retrieve ? "the retrieval" : "the declined retrieval");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Split Throw: "Target a unit in range. If you target a hero who is adjacent to 3 or more
        /// obstacles, repeat once on a different target unit."
        ///
        /// <para><b>Split Attack's sentence without the printed "may"</b>, and that is the whole of what
        /// this journey adds: BR-Q15b composes the repeat as <c>IfAble</c>, so nobody is asked anything -
        /// the second instance's own target question simply opens once the first has completed. Its
        /// legal set is every unit his printed Range 2 still reaches EXCEPT the one already taken, which
        /// is <c>RepeatTargetBinding.DifferentTarget</c> rather than Split Attack's narrower ring.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SplitThrowRepeatsOnADifferentTargetUnitWithoutAskingHim()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.SplitThrow, BrynnCardContent.Decoy },
                enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                enemySpace: InTheOpen);
            var root = Root();
            var runtime = root.Runtime;
            var enemy = Foes(runtime)[0];
            Assert.That(ObstaclesBeside(runtime, InTheOpen), Is.LessThan(3),
                "Arrange: the enemy hero stands in the open. " + Remaining(runtime));

            yield return BeginBrynn(
                root, BrynnCardContent.SplitThrow, ActionKind.Attack,
                WaspCardContent.KineticRepulse);
            var first = runtime.PendingAttackTargetChoice;
            Assert.That(first.LegalTargetHeroIds, Contains.Item(enemy));
            var reachable = first.LegalTargetMinionIds.Select(value => value.Value).ToArray();
            Assert.That(reachable.Length, Is.GreaterThan(1),
                "Arrange: his printed Range 2 reaches more than one enemy unit. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "BR-Q15b: the printed sentence carries no \"may\", so nobody is asked whether to " +
                "repeat. " + Remaining(runtime));
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            yield return null;

            var second = runtime.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null,
                "and the repeated instance opens its own target question. " + Remaining(runtime));
            Assert.That(second.LegalTargetHeroIds, Is.Empty,
                "\"on a DIFFERENT target unit\" - the hero the first instance took is not offered " +
                "again. " + Remaining(runtime));
            Assert.That(
                second.LegalTargetMinionIds.Select(value => value.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(reachable.OrderBy(value => value, StringComparer.Ordinal).ToArray())
                    .AsCollection,
                "and it keeps the printed Range 2 rather than Split Attack's ring of units " +
                "adjacent to him. " + Remaining(runtime));

            var minion = second.LegalTargetMinionIds.First();
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    MinionView(minion).transform.position),
                Is.True,
                "and the repeat's own target is answered on the board. " + Remaining(runtime));
            yield return null;
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "p.10: \"You cannot repeat a repeated action\". " + Remaining(runtime));
            SeatsAgree(runtime, "the mandatory repeat");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Blue ladder's Tier III
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Hide Traces: "Swap either with a unit adjacent to you, or with an enemy hero in radius who is
        /// adjacent to 3 or more obstacles. Move up to 2 spaces."
        ///
        /// <para><b>The radius reach alone, measured by emptying the other one.</b> Brynn stands at
        /// (-6,0), where nothing whatsoever is in p.19's ring, and the enemy hero three spaces off at
        /// (-6,3) in the open - so the ONLY candidate the selection can hold is the one Over the Top
        /// admits, and the assertion is an exact set of one. The trailing "Move up to 2" is an
        /// <c>UpTo</c> whose optionality lives inside its own destination offer (milestone 8.39j), so a
        /// player answers it by clicking a space rather than by declining, and the space he stood on is
        /// one of the ones offered.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HideTracesSwapsWithAHeroInRadiusAndThenMovesHimUpToTwo()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedTierThreeEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.HideTraces, BrynnCardContent.Decoy },
                hisSpace: Alone,
                enemySpace: FarEnemy);
            var root = Root();
            var runtime = root.Runtime;
            var his = Brynn(runtime);
            var enemy = Foes(runtime)[0];
            Assert.That(ObstaclesBeside(runtime, FarEnemy), Is.LessThan(3),
                "Arrange: the enemy hero stands in the open. " + Remaining(runtime));
            Assert.That(ObstaclesBeside(runtime, Alone), Is.Zero,
                "and nothing at all stands in Brynn's own ring, so the adjacent reach is empty. " +
                Remaining(runtime));

            yield return BeginBrynn(root, BrynnCardContent.HideTraces, ActionKind.Skill);
            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("tracking-swap-subject"));
            Assert.That(
                subject.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "One candidate, and it is the enemy hero the Ultimate widened the radius arm onto. " +
                Remaining(runtime));

            var stood = Position(runtime, his);
            var theirs = Position(runtime, enemy);
            yield return Pick(root, subject.Candidates.Single());
            Assert.That(Position(runtime, his), Is.EqualTo(theirs),
                "p.19's Swap exchanges the two positions. " + Remaining(runtime));
            Assert.That(Position(runtime, enemy), Is.EqualTo(stood));

            var walk = runtime.PendingEffectChoice;
            Assert.That(walk, Is.Not.Null,
                "\"Move up to 2 spaces\" follows the swap. " + Remaining(runtime));
            Assert.That(walk.NodeId.Value, Is.EqualTo("tracking-swap-move"));
            Assert.That(walk.DeclineInvocation, Is.Null,
                "An \"up to\" direct move carries its own nought inside the offer rather than as a " +
                "decline beside it. " + Remaining(runtime));
            Assert.That(
                OfferedSpaces(runtime).All(space => Distance(space, theirs) <= 2),
                Is.True,
                "and every destination is within the printed two of where the swap put him. " +
                Remaining(runtime));
            Assert.That(OfferedSpaces(runtime), Contains.Item(theirs),
                "including the space he is standing on, which is how \"up to\" says nought. " +
                Remaining(runtime));
            var destination = OfferedSpaces(runtime)
                .First(space => Distance(space, theirs) == 2);
            yield return Space(root, destination);
            Assert.That(Position(runtime, his), Is.EqualTo(destination), Remaining(runtime));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            SeatsAgree(runtime, "Hide Traces");
            Healthy(runtime);
        }

        /// <summary>
        /// Expedition Leader: Mountain Guide's two sentences at "up to 3 spaces", driven against an
        /// enemy hero standing in the open - so the presence the second sentence reads is true only
        /// because Over the Top says it is.
        /// </summary>
        [UnityTest]
        public IEnumerator ExpeditionLeaderMovesTheUnitBesideHimAndADifferentOneInRadius()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.ExpeditionLeader, BrynnCardContent.Decoy },
                enemySpace: InTheOpen);
            var root = Root();
            var runtime = root.Runtime;
            Assert.That(ObstaclesBeside(runtime, InTheOpen), Is.LessThan(3),
                "Arrange: the enemy hero stands in the open. " + Remaining(runtime));

            yield return BeginBrynn(root, BrynnCardContent.ExpeditionLeader, ActionKind.Skill);
            var first = runtime.PendingEffectChoice;
            Assert.That(first.NodeId.Value, Is.EqualTo("guide-first-unit"));
            Assert.That(
                first.Candidates.Select(candidate => candidate.Value.MinionId.Value).ToArray(),
                Is.EqualTo(new[] { BlueMelee.Value }).AsCollection, Remaining(runtime));
            var movedFrom = MinionView(BlueMelee).Occupancy.Coordinate;
            yield return Pick(root, first.Candidates.Single());

            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("guide-first-walk"));
            Assert.That(
                OfferedSpaces(runtime).All(space => Distance(space, movedFrom) <= 3),
                Is.True,
                "\"up to 3 spaces\", which is one further than Mountain Guide's. " +
                Remaining(runtime));
            var firstTo = OfferedSpaces(runtime).First(space => Distance(space, movedFrom) == 3);
            yield return Space(root, firstTo);
            Assert.That(MinionView(BlueMelee).Occupancy.Coordinate, Is.EqualTo(firstTo),
                Remaining(runtime));

            var second = runtime.PendingEffectChoice;
            Assert.That(second, Is.Not.Null,
                "Over the Top makes the enemy hero in radius count as adjacent to 3 or more " +
                "obstacles wherever he stands, so the second printed sentence runs. " +
                Remaining(runtime));
            Assert.That(second.NodeId.Value, Is.EqualTo("guide-second-unit"));
            Assert.That(
                second.Candidates.Select(candidate => candidate.Value.MinionId.Value).ToArray(),
                Is.EqualTo(new[] { BlueRanged.Value }).AsCollection,
                "\"a DIFFERENT friendly unit in radius\". " + Remaining(runtime));
            var secondFrom = MinionView(BlueRanged).Occupancy.Coordinate;
            yield return Pick(root, second.Candidates.Single());
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("guide-second-walk"));
            var secondTo = OfferedSpaces(runtime).First(space => Distance(space, secondFrom) == 3);
            yield return Space(root, secondTo);
            Assert.That(MinionView(BlueRanged).Occupancy.Coordinate, Is.EqualTo(secondTo),
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            SeatsAgree(runtime, "Expedition Leader's two sentences");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Green ladder's Tier III
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Deadfall Trap: "Choose one - (1) An enemy hero adjacent to you discards a card, or is
        /// defeated. (2) An enemy hero in radius who is adjacent to 3 or more obstacles discards a card,
        /// or is defeated."
        ///
        /// <para><b>BR-Q11's escalation, both sides of it.</b> A hero holding a card gives one up and
        /// stands; a hero whose hand rulebook p.7's own commitment emptied cannot pay and is DEFEATED
        /// where he stands - and p.13's coins land on Brynn for it, read on his own rail. It is the one
        /// face of his that removes a hero from the board, and the difference between the two runs is
        /// one card in the enemy's staged hand.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DeadfallTrapDefeatsTheHeroWhoCannotDiscardAndPaysHimThePageThirteenCoins()
        {
            foreach (var withCards in new[] { true, false })
            {
                yield return StageBrynn(
                    SceneCardContent.VerifiedTierThreeEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.DeadfallTrap, BrynnCardContent.Decoy },
                    enemyHand: withCards
                        ? new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse }
                        : new[] { WaspCardContent.Electrocute },
                    enemySpace: InTheOpen);
                var root = Root();
                var runtime = root.Runtime;
                var his = Brynn(runtime);
                var enemy = Foes(runtime)[0];
                Assert.That(ObstaclesBeside(runtime, InTheOpen), Is.LessThan(3),
                    "Arrange: the enemy hero stands in the open, so the radius bullet is offered " +
                    "only because Over the Top widened it. " + Remaining(runtime));

                yield return BeginBrynn(root, BrynnCardContent.DeadfallTrap, ActionKind.Skill);
                Assert.That(OfferedBullets(runtime),
                    Is.EqualTo(new[] { "trap-adjacent", "trap-radius" }).AsCollection,
                    "Both printed bullets name the one enemy hero - the first because he is " +
                    "adjacent, the second because the Ultimate says he is boxed in. " +
                    Remaining(runtime));
                var coinsBefore = runtime.Projection.HeroProgression
                    .Single(hero => hero.HeroId.Equals(his)).Coins;
                yield return Bullet(root, "trap-radius");

                var select = runtime.PendingEffectChoice;
                Assert.That(select.NodeId.Value, Is.EqualTo("trap-radius-select"));
                Assert.That(
                    select.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection, Remaining(runtime));
                yield return Pick(root, select.Candidates.Single());

                if (withCards)
                {
                    var discard = runtime.PendingEffectChoice;
                    Assert.That(discard, Is.Not.Null, Remaining(runtime));
                    Assert.That(discard.NodeId.Value, Is.EqualTo("trap-radius-discard"));
                    Assert.That(discard.ChooserId, Is.EqualTo(enemy),
                        "The chosen hero picks the card they lose. " + Remaining(runtime));
                    yield return Pick(root, discard.Candidates.First());
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                        "A hero who could pay is not defeated. " + Remaining(runtime));
                    Assert.That(
                        runtime.Projection.HeroProgression
                            .Single(hero => hero.HeroId.Equals(his)).Coins,
                        Is.EqualTo(coinsBefore),
                        "and nothing is paid for a discard. " + Remaining(runtime));
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "An empty hand has nothing to offer, so no discard is asked for at all. " +
                        Remaining(runtime));
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                        "\"or is defeated\" - the printed escalation. " + Remaining(runtime));
                    Assert.That(
                        Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                            .Count(view => view.Occupancy.HeroId.Equals(enemy) &&
                                view.gameObject.activeSelf),
                        Is.Zero,
                        "and the piece leaves the board. " + Remaining(runtime));
                    Assert.That(
                        runtime.Projection.HeroProgression
                            .Single(hero => hero.HeroId.Equals(his)).Coins,
                        Is.GreaterThan(coinsBefore),
                        "and p.13 pays the hero who defeated him. " + Remaining(runtime));
                    Assert.That(
                        runtime.HeroOverviewsView.Model.Heroes
                            .Single(hero => hero.HeroId.Equals(his)).Coins,
                        Is.EqualTo(runtime.Projection.HeroProgression
                            .Single(hero => hero.HeroId.Equals(his)).Coins),
                        "and his own rail reads the same balance. " + Remaining(runtime));
                }

                SeatsAgree(runtime, withCards ? "the deadfall paid" : "the deadfall's defeat");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Die Hard: True Grit's sentence at "up to 4 spaces". The retrieval is DECLINED here - the
        /// half True Grit's journey does not take - and the conditional move runs anyway, which is what
        /// says the two printed sentences are independent.
        /// </summary>
        [UnityTest]
        public IEnumerator DieHardDeclinesItsRetrievalAndMovesHimUpToFour()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.DieHard, BrynnCardContent.Decoy },
                enemySpace: InTheOpen);
            var root = Root();
            var runtime = root.Runtime;
            var his = Brynn(runtime);
            Assert.That(ObstaclesBeside(runtime, InTheOpen), Is.LessThan(3),
                "Arrange: the enemy hero stands in the open. " + Remaining(runtime));
            Assert.That(DiscardsOf(runtime, his),
                Contains.Item(BrynnCardContent.FamiliarGround.Value),
                "Arrange: a staged hand discards the dealt cards it does not name, and his Gold is " +
                "an Attack card. " + Remaining(runtime));

            yield return BeginBrynn(root, BrynnCardContent.DieHard, ActionKind.Skill);
            var retrieve = runtime.PendingEffectChoice;
            Assert.That(retrieve, Is.Not.Null, Remaining(runtime));
            Assert.That(retrieve.NodeId.Value, Is.EqualTo("grit-retrieve"));
            Assert.That(retrieve.DeclineInvocation, Is.Not.Null);
            var heldBefore = HeldBy(runtime, his).Length;
            yield return May(root, accept: false);
            Assert.That(HeldBy(runtime, his), Has.Length.EqualTo(heldBefore),
                "A declined `may` retrieves nothing. " + Remaining(runtime));

            var walk = runtime.PendingEffectChoice;
            Assert.That(walk, Is.Not.Null,
                "and the conditional move runs regardless, because Over the Top makes the presence " +
                "hold. " + Remaining(runtime));
            Assert.That(walk.NodeId.Value, Is.EqualTo("grit-move"));
            var stood = Position(runtime, his);
            Assert.That(
                OfferedSpaces(runtime).All(space => Distance(space, stood) <= 4),
                Is.True,
                "\"move up to 4 spaces\", which is one further than True Grit's. " +
                Remaining(runtime));
            var destination = OfferedSpaces(runtime).First(space => Distance(space, stood) == 4);
            yield return Space(root, destination);
            Assert.That(Position(runtime, his), Is.EqualTo(destination), Remaining(runtime));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination));
            SeatsAgree(runtime, "Die Hard");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // p.16's Ultimate, read as the one thing that changed
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Over the Top: "While you are performing actions all enemy heroes in play count as adjacent
        /// to 3 or more obstacles."
        ///
        /// <para><b>The same board twice, and the only difference is whether p.16 has turned the card
        /// faceup.</b> At Tier II his rail draws no Ultimate at all and Familiar Ground is GREYED on
        /// that board - nothing stands beside him and the one enemy hero in range has a single obstacle
        /// neighbour, so neither printed bullet names anybody. At Tier III the card is faceup beside his
        /// dashboard, the very same face is offered, its second bullet has a subject, and p.14's target
        /// question admits the hero standing in the open. That is the whole of BR-Q2, read where a
        /// player reads it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OverTheTopWidensHisOwnClauseOnABoardThatRefusedItOneTierEarlier()
        {
            foreach (var unlocked in new[] { false, true })
            {
                yield return StageBrynn(
                    unlocked
                        ? SceneCardContent.VerifiedTierThreeEngagedStagedBrynn
                        : SceneCardContent.VerifiedTierTwoEngagedStagedBrynn,
                    hand: new[] { BrynnCardContent.FamiliarGround, BrynnCardContent.Decoy },
                    enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                    hisSpace: Alone,
                    enemySpace: FarEnemy);
                var root = Root();
                var runtime = root.Runtime;
                var his = Brynn(runtime);
                var enemy = Foes(runtime)[0];
                Assert.That(ObstaclesBeside(runtime, FarEnemy), Is.LessThan(3),
                    "Arrange: the same geometry on both runs - the enemy hero in the open. " +
                    Remaining(runtime));
                Assert.That(ObstaclesBeside(runtime, Alone), Is.Zero,
                    "and nothing at all in Brynn's own ring. " + Remaining(runtime));
                Assert.That(
                    runtime.HeroOverviewsView.Model.Heroes
                        .Single(hero => hero.HeroId.Equals(his)).UltimateCard,
                    unlocked ? Is.Not.Null : Is.Null,
                    unlocked
                        ? "p.16 turns the unlocked Ultimate faceup where a player can read it. "
                        : "p.16 unlocks at Tier IV, so his Tier II rail draws none. " +
                          Remaining(runtime));

                yield return CommitEverySeatOfHis(
                    root, BrynnCardContent.FamiliarGround, WaspCardContent.KineticRepulse);
                yield return WaitForActor(root, his);
                yield return PointTrayAt(root, his);

                if (!unlocked)
                {
                    Assert.That(
                        PrimaryIsOffered(runtime, BrynnCardContent.FamiliarGround), Is.False,
                        "Without the Ultimate neither printed bullet names anybody on this board, " +
                        "so milestone 8.35l greys the whole printed primary. " + Remaining(runtime));
                    Healthy(runtime);
                    continue;
                }

                Assert.That(PrimaryIsOffered(runtime, BrynnCardContent.FamiliarGround), Is.True,
                    "With Over the Top faceup the same face on the same board is offered, and the " +
                    "only thing that moved is the card beside his dashboard. " + Remaining(runtime));
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(OfferedBullets(runtime),
                    Contains.Item("familiar-ground-obstacle-bullet"), Remaining(runtime));
                yield return Bullet(root, "familiar-ground-obstacle-bullet");
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                    "and the bullet that named nobody one tier earlier reaches p.14's own target " +
                    "question. " + Remaining(runtime));
                Assert.That(
                    runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "whose legal set the overlay widened to the hero standing in the open - read " +
                    "at TARGET SELECTION rather than at composition. " + Remaining(runtime));
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True);
                yield return null;
                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                SeatsAgree(runtime, "the widened bullet");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The session half every hero owes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.13's Defeat takes his piece off the board, p.13's respawn puts it back on a space
        /// a player clicks, and the restart diagnostic rebuilds the session from its initial state.
        /// </summary>
        [UnityTest]
        public IEnumerator HisDefeatedPieceRespawnsAndRestartRebuildsTheSession()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedTierThreeEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.Decoy, BrynnCardContent.HideTraces },
                enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse });
            var root = Root();
            var runtime = root.Runtime;
            var his = Brynn(runtime);
            var enemy = Foes(runtime)[0];

            yield return CommitEverySeatOfHis(
                root, BrynnCardContent.Decoy, WaspCardContent.Electrocute);
            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(his).transform.position),
                Is.True,
                "The enemy's printed Attack reaches him where he stands. " + Remaining(runtime));
            yield return null;
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(his)), Is.False,
                "An undefended Attack defeats him and the piece leaves the board. " +
                Remaining(runtime));
            SeatsAgree(runtime, "his own defeat");

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, his, BrynnCardContent.HideTraces);
            yield return YieldUntilTheNamedSeatActs(runtime, his);
            var destination = runtime.RoundView.Model.HeroRespawnOptions.First().Destination;
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    Tile(destination).transform.position),
                Is.True,
                "p.13's respawn is answered by clicking the space. " + Remaining(runtime));
            yield return null;
            Assert.That(Position(runtime, his), Is.EqualTo(destination));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination),
                "and the rebuilt piece is drawn where the authority put it.");
            Healthy(runtime);

            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "Restart reconstruction returns the initial state with no accepted history. " + Remaining(runtime));
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                    .Count(view => view.Occupancy.HeroId.Equals(his)),
                Is.EqualTo(1),
                "One hero, one piece - a rebuild that leaves a stale view behind fails here.");
            Assert.That(
                runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(his)).TrayCards,
                Is.Not.Empty,
                "and his tray is reconstructed rather than left blank. " + Remaining(runtime));
            Healthy(runtime);
        }

        /// <summary>
        /// A pause of his survives the Director's retained-prefix Undo, the replay of that prefix
        /// reproduces exactly the offer that was there, and the answer is given again on the same
        /// surface.
        ///
        /// <para>The pause is DECOY's second performance - the hardest of his to bring back, because
        /// what has to come back is not only the question but the EXCLUSION the first performance
        /// carried: a rebuild that re-derived the second modal from the printed text alone would offer
        /// the hero bullet again, and nothing on screen would say so.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HisOwnDecoyPauseUndoesAndTheReplayReproducesTheExclusion()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedTierThreeEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.Decoy, BrynnCardContent.HideTraces });
            var root = Root();
            var runtime = root.Runtime;
            var enemy = Foes(runtime)[0];

            yield return BeginBrynn(root, BrynnCardContent.Decoy, ActionKind.Skill);
            yield return May(root, accept: true);
            yield return Bullet(root, "decoy-1-hero");
            var target = runtime.PendingEffectChoice;
            Assert.That(target.NodeId.Value, Is.EqualTo("decoy-1-hero-target"));
            yield return Pick(root, target.Candidates.Single());

            var move = runtime.PendingEffectChoice;
            Assert.That(move.NodeId.Value, Is.EqualTo("decoy-1-hero-move"), Remaining(runtime));
            var offered = OfferedSpaces(runtime);
            var stood = Position(runtime, enemy);

            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("decoy-1-hero-target"),
                "One press gives the target question back unanswered - milestone 8.46b made a pick " +
                "of exactly one ONE accepted command. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;

            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("decoy-1-hero-move"),
                "Replaying the retained prefix reproduces the same pause. " + Remaining(runtime));
            Assert.That(OfferedSpaces(runtime), Is.EqualTo(offered).AsCollection,
                "and the offer that comes back is the offer that was there. " + Remaining(runtime));

            var destination = offered.First();
            yield return Space(root, destination);
            Assert.That(Position(runtime, enemy), Is.EqualTo(destination), Remaining(runtime));
            Assert.That(Distance(destination, stood), Is.EqualTo(1));

            var second = runtime.PendingEffectChoice;
            Assert.That(second.NodeId.Value, Is.EqualTo("decoy-2-after-hero-may"),
                Remaining(runtime));
            yield return May(root, accept: true);
            Assert.That(OfferedBullets(runtime),
                Is.EqualTo(new[] { "decoy-2-after-hero-minion" }).AsCollection,
                "and the exclusion the first performance carried survived the replay - the hero " +
                "bullet is not offered a second time. " + Remaining(runtime));
            SeatsAgree(runtime, "the replayed exclusion");
            Healthy(runtime);
        }

        /// <summary>
        /// A pause of his survives a snapshot restore and is answered afterwards on the same surface.
        ///
        /// <para>The pause is DEADFALL TRAP's discard, which is the hardest of his to bring back
        /// because it belongs to somebody else: the chooser is the hero the trap named, and a restore
        /// that re-derived the chooser from the acting seat would look identical from Brynn's side of
        /// the table.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HisOwnTrapPauseSurvivesASnapshotRestore()
        {
            yield return StageBrynn(
                SceneCardContent.VerifiedTierThreeEngagedStagedBrynn,
                hand: new[] { BrynnCardContent.DeadfallTrap, BrynnCardContent.Decoy },
                enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse });
            var root = Root();
            var runtime = root.Runtime;
            var enemy = Foes(runtime)[0];

            yield return BeginBrynn(root, BrynnCardContent.DeadfallTrap, ActionKind.Skill);
            yield return Bullet(root, "trap-adjacent");
            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());

            var discard = runtime.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null, Remaining(runtime));
            Assert.That(discard.NodeId.Value, Is.EqualTo("trap-adjacent-discard"));
            Assert.That(discard.ChooserId, Is.EqualTo(enemy));
            var offered = discard.Candidates
                .Select(candidate => candidate.Value.CardInstanceId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var captured = runtime.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null.And.Not.Empty);

            yield return Pick(root, discard.Candidates.First());
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));

            Assert.That(runtime.RestoreSnapshot(captured), Is.True,
                "The captured pause restores. " + Remaining(runtime));
            yield return null;
            yield return null;
            var restored = runtime.PendingEffectChoice;
            Assert.That(restored, Is.Not.Null,
                "A pause survives a snapshot restore. " + Remaining(runtime));
            Assert.That(restored.NodeId.Value, Is.EqualTo("trap-adjacent-discard"));
            Assert.That(restored.ChooserId, Is.EqualTo(enemy),
                "and it is still the CHOSEN hero's decision rather than the acting seat's. " +
                Remaining(runtime));
            Assert.That(
                restored.Candidates
                    .Select(candidate => candidate.Value.CardInstanceId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray(),
                Is.EqualTo(offered).AsCollection,
                "and the offer that comes back is the offer that was there. " + Remaining(runtime));
            SeatsAgree(runtime, "the restored discard");

            yield return Pick(root, restored.Candidates.First());
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "and answering it on the same surface carries on. " + Remaining(runtime));
            Healthy(runtime);
        }
    }
}
