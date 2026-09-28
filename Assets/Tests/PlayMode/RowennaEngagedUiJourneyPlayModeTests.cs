using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.RowennaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Rowenna's coloured Tier I and Tier II faces on the staged boards their printed sentences need.
    /// Milestone 8.57h.
    ///
    /// <para><b>Which board each journey stands on, and why.</b> Her three Red riders name "a friendly
    /// hero in radius" - composed with <c>includeSelf</c> false at milestone 8.57a - so they run on
    /// <c>VerifiedStagedRowennaWithAlly</c>, where Arien holds the second Blue seat. Everything whose
    /// subject is the enemy hero, an enemy minion or a friendly minion runs on the ordinary engagement.
    /// Both are re-stood by the Director's sandbox through
    /// <see cref="RowennaUiJourneyPlayModeTests.StageRowenna"/>, whose own remarks record the measured
    /// spaces.</para>
    /// </summary>
    public sealed class RowennaEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Token of Gratitude, Fair Share and Feat of Bravery: "Target a unit adjacent to you. After
        /// the attack: A friendly hero in radius gains 1 coin" - and, on the third, "may retrieve a
        /// discarded card" instead.
        ///
        /// <para>The three differ only in the printed Radius and in what the chosen hero is given, so
        /// they are one journey over three faces: the selection is the same board click on the same
        /// hero, and the difference the gate cares about is that Feat of Bravery's grant is a SECOND
        /// cross-seat prompt - the retrieve is answered by the hero who was chosen, out of THEIR OWN
        /// discard pile, and they may decline it.</para>
        ///
        /// <para><b>The negative is on the same page as the positive</b>: the identical face on the
        /// one-Blue-hero engagement offers nobody at all, because "a friendly hero in radius" excludes
        /// the actor - which is the whole reason milestone 8.57g composed an ally board and is exactly
        /// Brogan's War Drummer problem.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheRewardRedsGiveTheChosenFriendlyHeroACoinOrTheirOwnRetrieval()
        {
            foreach (var face in new[]
            {
                RowennaCardContent.TokenOfGratitude,
                RowennaCardContent.FairShare,
                RowennaCardContent.FeatOfBravery
            })
            {
                var retrieves = face.Equals(RowennaCardContent.FeatOfBravery);
                yield return StageRowenna(
                    SceneCardContent.VerifiedStagedRowennaWithAlly,
                    hand: new[] { face, RowennaCardContent.StandGuard },
                    allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak });
                var root = Root();
                var runtime = root.Runtime;
                var her = Rowenna(runtime);
                var ally = Ally(runtime);
                var enemy = Enemies(runtime)[0];
                Assert.That(Distance(Position(runtime, her), Position(runtime, ally)),
                    Is.EqualTo(1),
                    "Arrange: the ally stands inside even Token of Gratitude's printed Radius 1.");
                var herSpace = Position(runtime, her);
                var allySpace = Position(runtime, ally);

                yield return BeginRowenna(root, face, ActionKind.Attack);

                // MILESTONE 8.86: named by its own id rather than taken first off the offer.
                var minion = runtime.PendingAttackTargetChoice.LegalTargetMinionIds
                    .OrderBy(value => value.Value, StringComparer.Ordinal).First();
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        MinionView(minion).transform.position),
                    Is.True,
                    "\"a unit adjacent to you\" is answered on the board. " + Remaining(runtime));
                yield return null;
                yield return null;
                if (runtime.PendingDefenseChoice != null)
                {
                    UiPlayer.ClickDefenseDecline(runtime, root.Hud);
                    yield return null;
                }

                // THE ATTACK'S OWN OUTCOME (milestone 8.86): the reward this journey is about is a
                // rider on an Attack, so the Attack has to have happened. p.14 defeats an attacked
                // minion without a Defense step.
                Assert.That(
                    runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(minion)),
                    Is.False,
                    face.Value + ": the minion she attacked is defeated, which is what makes the " +
                    "reward below a rider rather than a card on its own. " + Remaining(runtime));

                var reward = runtime.PendingEffectChoice;
                Assert.That(reward, Is.Not.Null,
                    face.Value + " stops after p.14 has been applied. " + Remaining(runtime));
                Assert.That(reward.NodeId.Value, Is.EqualTo("reward-ally"));
                Assert.That(reward.ChooserId, Is.EqualTo(her),
                    "SHE names which friendly hero is rewarded.");
                Assert.That(
                    reward.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { ally }).AsCollection,
                    "and she is not a candidate for her own reward - 8.57a composed the selection " +
                    "with includeSelf false. " + Remaining(runtime));

                // Measured HERE rather than before the Attack, because p.14 has already run: whatever
                // rulebook p.13 paid for the unit she just attacked is in the purse, and what this
                // journey is about is the CARD's own grant. The first Unity run of this journey read
                // the two together and could not tell them apart.
                var coinsBefore = CoinsOf(runtime, ally);
                var discardsBefore = DiscardsOf(runtime, ally).Length;
                yield return Pick(root, reward.Candidates.Single());

                if (retrieves)
                {
                    var theirs = runtime.PendingEffectChoice;
                    Assert.That(theirs, Is.Not.Null,
                        "\"may retrieve a discarded card\" follows the naming. " +
                        Remaining(runtime));
                    Assert.That(theirs.NodeId.Value, Is.EqualTo("reward-retrieve"));
                    Assert.That(theirs.ChooserId, Is.EqualTo(ally),
                        "and it is the CHOSEN hero's own decision, on their own seat - RO-Q7d. " +
                        Remaining(runtime));
                    Assert.That(
                        theirs.Candidates.All(candidate => runtime.Cards
                            .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                            .OwnerId.Equals(ally)),
                        Is.True,
                        "Nobody retrieves another hero's card.");
                    SeatsAgree(runtime, "the rewarded hero's own retrieve");
                    yield return Pick(root, Named(theirs.Candidates, "theirs"));
                    Assert.That(DiscardsOf(runtime, ally), Has.Length.EqualTo(discardsBefore - 1),
                        "and taking it moves exactly one card out of THEIR pile.");
                    Assert.That(CoinsOf(runtime, ally), Is.EqualTo(coinsBefore),
                        "Feat of Bravery replaces the coin with the retrieval, so nothing is paid.");
                }
                else
                {
                    Assert.That(CoinsOf(runtime, ally), Is.EqualTo(coinsBefore + 1),
                        "\"gains 1 coin\" pays exactly one, to the hero she named. " +
                        Remaining(runtime));
                    Assert.That(DiscardsOf(runtime, ally), Has.Length.EqualTo(discardsBefore),
                        "and nothing of theirs moves - the two Tier I/II Reds pay a coin OR a " +
                        "retrieval, never both.");
                    Assert.That(RailCoinsOf(runtime, ally), Is.EqualTo(CoinsOf(runtime, ally)),
                        "and that hero's own rail draws the number the projection holds.");
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, face.Value + " resolved");
                Healthy(runtime);
                // MILESTONE 8.86: what stood here was `Position(enemy) != Position(her)`, which is
                // true of every board two pieces can stand on and proved nothing about this card.
                // What the three Reward Reds do NOT do is move anybody, and that is the assertion
                // worth making beside the grant.
                Assert.That(Position(runtime, her), Is.EqualTo(herSpace),
                    face.Value + " grants a coin or a retrieval and moves nobody, her included. " +
                    Remaining(runtime));
                Assert.That(Position(runtime, ally), Is.EqualTo(allySpace),
                    "and the hero it rewards stands where they stood. " + Remaining(runtime));
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                    "and the enemy hero, whom none of the three Reds names, is untouched. " +
                    Remaining(runtime));
            }

            // The negative, on the board that has no friendly hero at all.
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna,
                hand: new[]
                {
                    RowennaCardContent.TokenOfGratitude, RowennaCardContent.StandGuard
                });
            var aloneRoot = Root();
            var aloneRuntime = aloneRoot.Runtime;
            yield return BeginRowenna(
                aloneRoot, RowennaCardContent.TokenOfGratitude, ActionKind.Attack);
            var target = aloneRuntime.PendingAttackTargetChoice.LegalTargetMinionIds
                .OrderBy(value => value.Value, StringComparer.Ordinal).First();
            var aloneHer = Rowenna(aloneRuntime);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    aloneRoot.Hud,
                    MinionView(target).transform.position),
                Is.True);
            yield return null;
            yield return null;
            if (aloneRuntime.PendingDefenseChoice != null)
            {
                UiPlayer.ClickDefenseDecline(aloneRuntime, aloneRoot.Hud);
                yield return null;
            }

            // MILESTONE 8.86, SO THE NEGATIVE IS NOT AN ABSENCE. Three things are asserted rather
            // than one: the Attack really ran, there really is no friendly hero for the reward to
            // name on this board, and only THEN is the silence read as the printed stop.
            Assert.That(
                aloneRuntime.ReadModel.Minions.Any(value => value.MinionId.Equals(target)),
                Is.False,
                "The Attack the reward rides on really happened. " + Remaining(aloneRuntime));
            Assert.That(
                aloneRuntime.Heroes.Count(hero =>
                    aloneRuntime.Setup.Heroes.Single(seat => seat.HeroId.Equals(hero.HeroId))
                        .TeamSide ==
                    aloneRuntime.Setup.Heroes.Single(seat => seat.HeroId.Equals(aloneHer)).TeamSide),
                Is.EqualTo(1),
                "and she is the only hero on her own team, so \"a friendly hero in radius\" names " +
                "nobody by the board rather than by a bug. " + Remaining(aloneRuntime));
            Assert.That(aloneRuntime.PendingEffectChoice, Is.Null,
                "RO-Q7b: nobody in radius is p.10's stop, not a refusal and not a prompt with an " +
                "empty offer. " + Remaining(aloneRuntime));
            Healthy(aloneRuntime);
        }

        /// <summary>
        /// Paragon of Grace and Paragon of Valor: "...If they do, repeat once on a different target."
        ///
        /// <para><b>The repeat is gated on the rider's own performed output</b>, which is the seam
        /// milestone 8.57a built and the one thing about these two faces that no earlier hero's content
        /// has. Grace's rider is a coin and always performs, so its repeat is always offered; Valor's is
        /// the chosen hero's OWN optional retrieve, so the same journey drives it twice - once taken,
        /// where the repeat is offered, and once DECLINED on the ally's own seat, where nothing stands
        /// afterwards at all.</para>
        ///
        /// <para>And the repeat's own target set is asserted rather than assumed: the first target is
        /// excluded by name, which is Thunder Boomerang's <c>RepeatTargetBinding.DifferentTarget</c>
        /// declaration doing its work on new content.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheParagonsRepeatOnADifferentTargetOnlyWhenTheRiderWasPerformed()
        {
            foreach (var declines in new[] { false, true })
            {
                var face = declines
                    ? RowennaCardContent.ParagonOfValor
                    : RowennaCardContent.ParagonOfGrace;
                yield return StageRowenna(
                    SceneCardContent.VerifiedStagedRowennaWithAlly,
                    hand: new[] { face, RowennaCardContent.StandGuard },
                    allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak });
                var root = Root();
                var runtime = root.Runtime;
                var her = Rowenna(runtime);
                var ally = Ally(runtime);
                var enemy = Enemies(runtime)[0];

                yield return BeginRowenna(root, face, ActionKind.Attack);
                Assert.That(
                    runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True);
                yield return null;
                yield return null;
                UiPlayer.ClickDefenseDecline(runtime, root.Hud);
                yield return null;
                yield return null;
                // THE FIRST INSTANCE'S OWN OUTCOME (milestone 8.86). The journey pressed its way
                // past p.14 without reading what it did; the Attack is now asserted to have RUN -
                // the Defense window she declined on the defender's behalf is closed, and either
                // p.14 took his piece or he is standing there having answered it.
                Assert.That(runtime.PendingDefenseChoice, Is.Null,
                    face.Value + ": the declined Defense closed the window rather than stalling " +
                    "the Attack. " + Remaining(runtime));
                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                    face.Value + " prints an Attack of 6 or more and the defender answered with " +
                    "nothing at all, so p.13's Defeat takes his piece - which is also why the " +
                    "repeat below is offered no hero. " + Remaining(runtime));

                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("reward-ally"),
                    Remaining(runtime));
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());

                if (declines)
                {
                    var theirs = runtime.PendingEffectChoice;
                    Assert.That(theirs.NodeId.Value, Is.EqualTo("reward-retrieve"));
                    Assert.That(theirs.ChooserId, Is.EqualTo(ally));
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "A declined rider makes \"if they do\" false, so the repeat is never " +
                        "offered and nothing stands. " + Remaining(runtime));
                    Assert.That(runtime.PendingTurnRelease, Is.Not.Null,
                        "and the turn is hers to release. " + Remaining(runtime));
                    SeatsAgree(runtime, "the declined rider");
                    Healthy(runtime);
                    continue;
                }

                // Milestone 9.1, ruling W1: "repeat once" prints no "may", so the performed rider is
                // followed straight by the repeat - no confirm-or-decline stands between them.
                Assert.That(runtime.PendingEffectChoice?.NodeId.Value, Is.Not.EqualTo("reward-repeat"),
                    "W1: the Paragons' repeat is automatic. " + Remaining(runtime));
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                    "The rider was performed, so the repeat re-opens p.13's own target question. " +
                    Remaining(runtime));
                Assert.That(
                    runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Is.Empty,
                    "\"on a different target\" excludes the hero the first instance took. " +
                    Remaining(runtime));
                // MILESTONE 8.86: named by its own id rather than taken first off the offer.
                var second = runtime.PendingAttackTargetChoice.LegalTargetMinionIds
                    .OrderBy(value => value.Value, StringComparer.Ordinal).First();
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        MinionView(second).transform.position),
                    Is.True);
                yield return null;
                yield return null;
                if (runtime.PendingDefenseChoice != null)
                {
                    UiPlayer.ClickDefenseDecline(runtime, root.Hud);
                    yield return null;
                }

                // THE REPEATED INSTANCE'S OWN OUTCOME (milestone 8.86): the repeat performed a
                // second Attack rather than only re-asking the question.
                Assert.That(
                    runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(second)),
                    Is.False,
                    "The repeat's own target is defeated by it. " + Remaining(runtime));

                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("reward-ally"),
                    "The second instance runs its own rider. " + Remaining(runtime));
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "and there is no third instance: the printed repeat is \"once\". " +
                    Remaining(runtime));
                SeatsAgree(runtime, "the repeat resolved");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Stand Guard, Protect the Weak and Defend the Innocent: "Swap with a friendly unit in range
        /// which is adjacent to an enemy hero, or has a card in the discard."
        ///
        /// <para><b>Both printed qualifiers are driven, and each is attributable to exactly one
        /// candidate</b>, which is the measurement that makes the assertion worth making. On the ally
        /// board the offer holds exactly two units and they qualify for different halves of the printed
        /// "or": the friendly melee minion at (-3,2) is adjacent to the enemy hero and owns no cards at
        /// all, and the ally hero at (-4,1) is two spaces from the enemy and has cards in the discard
        /// because the staging put them there. The engagement holds only the first, so it is the board
        /// the adjacency half is taken on.</para>
        ///
        /// <para>Defend the Innocent adds HER own retrieve after the swap - a third decision on the
        /// actor's own seat, offering only her own pile.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EverySwapTakesEitherPrintedQualifierAndDefendTheInnocentRetrieves()
        {
            // The adjacency qualifier, on the board where it is the only one.
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna,
                hand: new[] { RowennaCardContent.StandGuard, RowennaCardContent.CodeOfChivalry });
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemy = Enemies(runtime)[0];
            var herSpace = Position(runtime, her);

            yield return BeginRowenna(root, RowennaCardContent.StandGuard, ActionKind.Skill);
            var subject = runtime.PendingEffectChoice;
            Assert.That(subject.NodeId.Value, Is.EqualTo("guard-subject"), Remaining(runtime));
            Assert.That(
                subject.Candidates.Select(candidate => candidate.Value.Kind).Distinct().ToArray(),
                Is.EqualTo(new[] { EffectValueKind.Minion }).AsCollection,
                "On the one-Blue-hero engagement only a MINION qualifies, and only by adjacency: a " +
                "minion owns no cards, so the discard half of the printed \"or\" can never be its " +
                "reason. " + Remaining(runtime));
            var swapped = subject.Candidates.Single().Value.MinionId;
            var minionSpace = runtime.ReadModel.Minions
                .Single(minion => minion.MinionId.Equals(swapped)).Coordinate;
            Assert.That(Distance(minionSpace, Position(runtime, enemy)), Is.EqualTo(1),
                "and it is adjacent to an enemy hero, which is the printed qualifier it meets.");
            yield return Pick(root, subject.Candidates.Single());
            Assert.That(Position(runtime, her), Is.EqualTo(minionSpace),
                "A swap exchanges the two spaces. " + Remaining(runtime));
            Assert.That(
                runtime.ReadModel.Minions.Single(minion => minion.MinionId.Equals(swapped))
                    .Coordinate,
                Is.EqualTo(herSpace));
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Healthy(runtime);

            // The discard qualifier, on the board that has a friendly HERO to carry one.
            yield return StageRowenna(
                SceneCardContent.VerifiedStagedRowennaWithAlly,
                hand: new[]
                {
                    RowennaCardContent.ProtectTheWeak, RowennaCardContent.CodeOfChivalry
                },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak });
            var allyRoot = Root();
            var allyRuntime = allyRoot.Runtime;
            var allyHer = Rowenna(allyRuntime);
            var ally = Ally(allyRuntime);
            var allyEnemy = Enemies(allyRuntime)[0];
            var allySpace = Position(allyRuntime, ally);
            Assert.That(DiscardsOf(allyRuntime, ally), Is.Not.Empty,
                "Arrange: the staged hand left cards in the ally's own pile.");
            Assert.That(Distance(allySpace, Position(allyRuntime, allyEnemy)), Is.EqualTo(2),
                "and the ally is NOT adjacent to an enemy hero, so the discard is the only half of " +
                "the printed \"or\" that can admit them.");

            yield return BeginRowenna(
                allyRoot, RowennaCardContent.ProtectTheWeak, ActionKind.Skill);
            var wide = allyRuntime.PendingEffectChoice;
            Assert.That(wide.NodeId.Value, Is.EqualTo("guard-subject"));
            Assert.That(
                wide.Candidates.Count(candidate => candidate.Value.Kind == EffectValueKind.Hero),
                Is.EqualTo(1),
                "The friendly hero is admitted by their own discard. " + Remaining(allyRuntime));
            Assert.That(
                wide.Candidates.Count(candidate => candidate.Value.Kind == EffectValueKind.Minion),
                Is.EqualTo(1),
                "and the minion beside the enemy hero is still admitted by adjacency, which is what " +
                "makes this ONE reach with two qualifiers rather than two reaches. " +
                Remaining(allyRuntime));
            yield return Pick(
                allyRoot,
                wide.Candidates.Single(candidate => candidate.Value.Kind == EffectValueKind.Hero));
            Assert.That(Position(allyRuntime, allyHer), Is.EqualTo(allySpace));
            Assert.That(Position(allyRuntime, ally), Is.EqualTo(new HexCoordinate(-3, 1)));
            SeatsAgree(allyRuntime, "the swap with a friendly hero");
            Healthy(allyRuntime);

            // The Tier III face, whose own retrieve follows the swap.
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna,
                hand: new[]
                {
                    RowennaCardContent.DefendTheInnocent, RowennaCardContent.CodeOfChivalry
                });
            var tierThreeRoot = Root();
            var tierThreeRuntime = tierThreeRoot.Runtime;
            var tierThreeHer = Rowenna(tierThreeRuntime);
            var pileBefore = DiscardsOf(tierThreeRuntime, tierThreeHer).Length;
            Assert.That(pileBefore, Is.EqualTo(4),
                "Arrange: a staged hand naming one DEALT card and one DECK card discards the other " +
                "four p.6 dealt her, which is what leaves this retrieve something to choose from.");

            yield return BeginRowenna(
                tierThreeRoot, RowennaCardContent.DefendTheInnocent, ActionKind.Skill);
            yield return Pick(
                tierThreeRoot, tierThreeRuntime.PendingEffectChoice.Candidates.Single());
            var retrieve = tierThreeRuntime.PendingEffectChoice;
            Assert.That(retrieve, Is.Not.Null,
                "\"you may retrieve a discarded card\" follows the swap. " +
                Remaining(tierThreeRuntime));
            Assert.That(retrieve.NodeId.Value, Is.EqualTo("guard-retrieve"));
            Assert.That(retrieve.ChooserId, Is.EqualTo(tierThreeHer),
                "and \"you\" is the actor, not the unit she swapped with.");
            Assert.That(
                retrieve.Candidates.All(candidate => tierThreeRuntime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(tierThreeHer)),
                Is.True);
            yield return Pick(tierThreeRoot, Named(retrieve.Candidates, "retrieve"));
            Assert.That(
                DiscardsOf(tierThreeRuntime, tierThreeHer), Has.Length.EqualTo(pileBefore - 1));
            Assert.That(tierThreeRuntime.PendingEffectChoice, Is.Null);
            Healthy(tierThreeRuntime);
        }

        /// <summary>
        /// Accept Surrender: "Defeat an enemy hero adjacent to you with no cards in hand." And Glorious
        /// Triumph, which adds "your friendly heroes gain triple assist coins".
        ///
        /// <para><b>The empty hand is reached the way a match reaches it</b>: the enemy is staged
        /// holding one card and then COMMITS it, so the card is in rulebook p.7's turn area and their
        /// hand really is empty - which is RO-Q9, hand means hand. The same board with a card still in
        /// that hand offers nobody, and the Skill is greyed rather than offered and refused.</para>
        ///
        /// <para><b>The tripled assist is measured as a comparison on one board</b>, which is the only
        /// honest way to read a multiplier: the same Defeat of the same hero by the same actor pays the
        /// ally ONE assist coin from Accept Surrender and THREE from Glorious Triumph, and p.13's own
        /// Defeat coins to Rowenna are unmoved between them - RO-Q3 end to end.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AcceptSurrenderDefeatsAnEmptyHandedHeroAndGloriousTriumphTriplesTheAssist()
        {
            var assists = new System.Collections.Generic.List<int>();
            var defeatCoins = new System.Collections.Generic.List<int>();
            foreach (var face in new[]
            {
                RowennaCardContent.AcceptSurrender, RowennaCardContent.GloriousTriumph
            })
            {
                yield return StageRowenna(
                    SceneCardContent.VerifiedStagedRowennaWithAlly,
                    hand: new[] { face, RowennaCardContent.CodeOfChivalry },
                    enemyHand: new[] { WaspCardContent.MagneticDagger });
                var root = Root();
                var runtime = root.Runtime;
                var her = Rowenna(runtime);
                var ally = Ally(runtime);
                var enemy = Enemies(runtime)[0];

                yield return BeginRowenna(root, face, ActionKind.Skill);
                Assert.That(HandCountOf(runtime, enemy), Is.Zero,
                    "Arrange: their one card is committed, so p.7's turn area holds it and the hand " +
                    "is empty. " + Remaining(runtime));

                var victim = runtime.PendingEffectChoice;
                Assert.That(victim, Is.Not.Null, Remaining(runtime));
                Assert.That(victim.NodeId.Value, Is.EqualTo("surrender-victim"));
                Assert.That(
                    victim.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "The one adjacent enemy hero holding nothing. " + Remaining(runtime));
                yield return Pick(root, victim.Candidates.Single());

                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                    "p.13's Defeat takes the piece off the board. " + Remaining(runtime));
                assists.Add(CoinsOf(runtime, ally));
                defeatCoins.Add(CoinsOf(runtime, her));
                Assert.That(RailCoinsOf(runtime, ally), Is.EqualTo(CoinsOf(runtime, ally)),
                    "and the ally's own rail draws the assist they were paid.");
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                SeatsAgree(runtime, face.Value + "'s defeat");
                Healthy(runtime);
            }

            Assert.That(assists[0], Is.GreaterThan(0),
                "Accept Surrender pays p.13's ordinary assist coins.");
            Assert.That(assists[1], Is.EqualTo(assists[0] * 3),
                "and Glorious Triumph pays exactly three times as many to the same hero for the " +
                "same Defeat - RO-Q3. Measured: " + assists[0] + " then " + assists[1]);
            Assert.That(defeatCoins[1], Is.EqualTo(defeatCoins[0]),
                "while her own Defeat coins are untouched: the clause multiplies ONE grant, it does " +
                "not pay a second time.");

            // The narrowing itself: a hand with a card in it removes the only candidate, and the
            // Skill is GREYED rather than offered and then refused.
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna,
                hand: new[]
                {
                    RowennaCardContent.AcceptSurrender, RowennaCardContent.CodeOfChivalry
                });
            var heldRoot = Root();
            var heldRuntime = heldRoot.Runtime;
            var held = Rowenna(heldRuntime);
            yield return PointTrayAt(heldRoot, held);
            yield return CommitThroughTray(
                heldRoot, NameOf(heldRuntime, RowennaCardContent.AcceptSurrender));
            foreach (var seat in Enemies(heldRuntime))
            {
                yield return PointTrayAt(heldRoot, seat);
                yield return CommitThroughTray(heldRoot, LowestInitiativeCardOf(heldRuntime, seat));
            }

            yield return WaitForActor(heldRoot, held);
            yield return PointTrayAt(heldRoot, held);
            Assert.That(HandCountOf(heldRuntime, Enemies(heldRuntime)[0]), Is.GreaterThan(0),
                "Arrange: this enemy still holds cards after committing one.");
            var skill = heldRuntime.AvailableTurnActions.Single(offer =>
                KindOf(heldRuntime, offer.ActionDefinitionId) == ActionKind.Skill);
            Assert.That(skill.IsAvailable, Is.False,
                "RO-Q9a: nobody qualifying greys the Skill rather than opening a prompt with an " +
                "empty offer. " + Remaining(heldRuntime));
            Assert.That(
                heldRuntime.RoundView.Model.Actions.Any(action =>
                    action.Command.ActionDefinitionId.Equals(skill.ActionDefinitionId)),
                Is.False,
                "and the strip draws no startable control for it. Shown: " +
                UiPlayer.Describe(heldRoot.Hud));
            Healthy(heldRuntime);
        }

        /// <summary>
        /// The same Defeat on a board whose enemy team is down to its last Life counter: rulebook p.13
        /// spends it, the match ends, and milestone 8.53's victory screen replaces the HUD.
        ///
        /// <para>It is a separate journey from the one above because what it proves is a TRANSITION
        /// rather than a reward, and because a journey that ended the match could not then go on to
        /// read a rail.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AcceptSurrenderSpendsTheLastLifeCounterAndEndsTheMatch()
        {
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna,
                hand: new[]
                {
                    RowennaCardContent.AcceptSurrender, RowennaCardContent.CodeOfChivalry
                },
                enemyHand: new[] { WaspCardContent.MagneticDagger },
                enemyLife: 1);
            var root = Root();
            var runtime = root.Runtime;
            Assert.That(runtime.VictoryScreenView.Model, Is.Null,
                "Arrange: nothing of the screen is drawn while the match is being played.");

            yield return BeginRowenna(root, RowennaCardContent.AcceptSurrender, ActionKind.Skill);
            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());

            Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                "The last Life counter was spent by her own Skill. " + Remaining(runtime));
            Assert.That(runtime.ReadModel.Outcome.Cause,
                Is.EqualTo(MatchVictoryCause.LifeCountersExhausted));
            Assert.That(runtime.VictoryScreenView.Model, Is.Not.Null,
                "and the end-of-match screen is drawn. " + UiPlayer.Describe(root.Hud));
            SeatsAgree(runtime, "the ended match");
        }

        /// <summary>
        /// Opening Shots: "If both you and an enemy hero in radius have no cards in the discard, that
        /// hero discards a card, if able."
        ///
        /// <para><b>Her third cross-seat prompt, and the only one whose candidate list is decided by
        /// two different piles.</b> The positive runs on the Tier II staging that deals the face, with
        /// no hand staged at all - because a staged hand is what fills a discard pile, and this
        /// sentence needs both piles empty. The enemy then chooses which of their OWN cards to spend,
        /// on their own seat.</para>
        ///
        /// <para><b>Both negatives are driven and they fail differently</b>, which is the measurement
        /// milestone 8.57d recorded: HER pile empties the whole offer, so the Skill is greyed outright;
        /// a CANDIDATE's pile removes only that candidate, and on a one-enemy board that also leaves
        /// the Skill greyed - so the distinction is asserted on the offer that is gone rather than on
        /// two different-looking controls.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OpeningShotsMakesTheNamedEnemyDiscardAndIsGreyedWhenEitherPileIsNot()
        {
            yield return StageRowenna(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedRowenna);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemy = Enemies(runtime)[0];
            Assert.That(DiscardsOf(runtime, her), Is.Empty,
                "Arrange: no hand was staged, so nothing of hers was discarded to make room.");
            Assert.That(DiscardsOf(runtime, enemy), Is.Empty);
            Assert.That(Distance(Position(runtime, her), Position(runtime, enemy)),
                Is.LessThanOrEqualTo(3),
                "and the enemy hero stands inside the printed Radius 3.");

            yield return BeginRowenna(root, RowennaCardContent.OpeningShots, ActionKind.Skill);
            var target = runtime.PendingEffectChoice;
            Assert.That(target.NodeId.Value, Is.EqualTo("volley-target"), Remaining(runtime));
            Assert.That(target.ChooserId, Is.EqualTo(her));
            Assert.That(
                target.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection);
            yield return Pick(root, target.Candidates.Single());

            var discard = runtime.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null, Remaining(runtime));
            Assert.That(discard.NodeId.Value, Is.EqualTo("volley-discard"));
            Assert.That(discard.ChooserId, Is.EqualTo(enemy),
                "\"that hero discards a card\" is that hero's own choice, on their own seat. " +
                Remaining(runtime));
            Assert.That(
                discard.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(enemy)),
                Is.True);
            SeatsAgree(runtime, "the opponent's own discard");
            var handBefore = HandCountOf(runtime, enemy);
            yield return Pick(root, Named(discard.Candidates, "discard"));
            Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(handBefore - 1));
            Assert.That(DiscardsOf(runtime, enemy), Has.Length.EqualTo(1),
                "and the card they chose is the one card now in their pile.");
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Opening Shots prints no repeat. " + Remaining(runtime));
            SeatsAgree(runtime, "the discard answered");
            Healthy(runtime);

            // HER pile: the actor's half empties the whole offer.
            yield return StageRowenna(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedRowenna,
                hand: new[]
                {
                    RowennaCardContent.OpeningShots, RowennaCardContent.CodeOfChivalry
                });
            yield return AssertOpeningShotsIsGreyed(
                "her own pile holds cards, and her half of the both-parties clause empties the " +
                "whole offer");

            // A CANDIDATE's pile: on a one-enemy board, removing the only candidate greys it too.
            yield return StageRowenna(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedRowenna,
                enemyHand: new[] { WaspCardContent.MagneticDagger, WaspCardContent.Shock });
            yield return AssertOpeningShotsIsGreyed(
                "the one enemy hero's own pile holds cards, which removes that candidate and leaves " +
                "the offer empty");
        }

        private static IEnumerator AssertOpeningShotsIsGreyed(string because)
        {
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            yield return PointTrayAt(root, her);
            yield return CommitThroughTray(
                root, NameOf(runtime, RowennaCardContent.OpeningShots));
            foreach (var seat in Enemies(runtime))
            {
                yield return PointTrayAt(root, seat);
                yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }

            yield return WaitForActor(root, her);
            yield return PointTrayAt(root, her);
            var skill = runtime.AvailableTurnActions.Single(offer =>
                KindOf(runtime, offer.ActionDefinitionId) == ActionKind.Skill);
            Assert.That(skill.IsAvailable, Is.False,
                "Opening Shots must be greyed because " + because + ". " + Remaining(runtime));
            Assert.That(
                runtime.RoundView.Model.Actions.Any(action =>
                    action.Command.ActionDefinitionId.Equals(skill.ActionDefinitionId)),
                Is.False,
                "and no startable control is drawn for it. Shown: " + UiPlayer.Describe(root.Hud));
            Healthy(runtime);
        }
    }
}
