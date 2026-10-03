using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.IgnatiaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Ignatia the Mad's six Tier III faces, rulebook p.15's Level Up, p.16's Chaos Incarnate, and the
    /// session operations every hero owes. Milestone 8.75c.
    ///
    /// <para><b>CHAOS INCARNATE IS THE ONE FACE THAT CHANGES WHAT EVERY OTHER CARD SAYS.</b> "The first
    /// time each turn after you perform a primary action you may flip the Tie Breaker coin; if you do,
    /// you may perform that action again, choosing different targets." Turning the coin over turns over
    /// the live text of her whole hand, so the repeat runs the OTHER printed line of the card it repeats
    /// - and what a journey has to show is not a flag but a board: the rail's tie-breaker banner eases
    /// to the other side, every card in her tray reads the other word, and the second instance of one
    /// Attack names a subject the first instance could not.</para>
    /// </summary>
    public sealed class IgnatiaProgressedUiJourneyPlayModeTests
    {
        /// <summary>The p.15 upgrade candidate's own control, addressed the way the HUD names it.</summary>
        private static Button UpgradeControl(
            MatchSceneCompositionRoot root, SystemTransitionOptionViewModel option) =>
            UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip ==
                    (option.Detail.Length > 0 ? option.Label + " - " + option.Detail : option.Label));

        /// <summary>
        /// The words her own content declares for a confirm-or-decline, asserted on the controls the
        /// decision window actually draws. Milestone 8.46a's rule is that a control names the
        /// CONSEQUENCE, and hers are the only two her dealt set raises plus the Ultimate's own pair.
        /// </summary>
        private static void AssertDeclaredLabels(
            MatchSceneCompositionRoot root, string nodeId, IEnumerable<DecisionLabelDeclaration> table)
        {
            var runtime = root.Runtime;
            var declared = table.Single(value => value.NodeId.Value == nodeId).Label;
            var confirm = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm);
            var decline = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline);
            Assert.That(confirm, Is.Not.Null,
                "'" + nodeId + "' offers an accept. " + UiPlayer.Describe(root.Hud));
            Assert.That(decline, Is.Not.Null,
                "and a refusal, which is what makes a printed \"may\" a decision. " +
                UiPlayer.Describe(root.Hud));
            // The window draws every declared label in upper case, exactly as it draws every other
            // control's words; what the content owns is the SENTENCE, which is what is compared.
            Assert.That(confirm.Label, Is.EqualTo(declared.Chosen.ToUpperInvariant()),
                "The window says what taking it does, in the card's own words.");
            Assert.That(decline.Label, Is.EqualTo(declared.Declined.ToUpperInvariant()),
                "and what refusing it does.");
        }

        // Milestone 8.75e. `TieBreakerBanner` and `AssertBannerShows` moved to
        // `IgnatiaUiJourneyPlayModeTests` (reached here through its `using static`): with the corner
        // badge dropped, the rail's banner is the ONE surface the live face is read on, so every
        // journey of hers reads it and the helper belongs beside the rest of her shared reading.

        // -------------------------------------------------------------------------------------------
        // p.15 and p.16: the whole upgrade face, and the item the untaken sibling becomes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.15's Level Up is offered as whole printed FACES out of her own deck, and p.16's
        /// item comes from the sibling the Level Up did not take - read on her own dashboard rail
        /// afterwards, which is the only route by which an item of hers ever exists.
        ///
        /// <para><b>HER LADDERS FORK TWICE EACH, and this is the case that says so.</b> The 8.75
        /// inventory pass named ONE successor apiece on her Blue and Green ladders, which would have
        /// offered a levelling seat one card where the rulebook deals two; milestone 8.75c's own
        /// fixtures corrected it. So her three coloured Tier I faces offer SIX candidates between them,
        /// and the count is asserted rather than sampled.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem()
        {
            yield return StageIgnatia(SceneCardContent.VerifiedEngagedStagedIgnatia);
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);

            yield return DriveUntil(
                root,
                () => runtime.SystemTransitionPrompt?.Kind ==
                        SystemTransitionPromptKind.LevelUpUpgrade &&
                    runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(hers),
                "her own p.15 Level Up");

            var options = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .Where(option => option.Card != null)
                .ToArray();
            Assert.That(options, Is.Not.Empty,
                "p.15 offers whole faces rather than anonymous ordinals. " +
                UiPlayer.Describe(root.Hud));
            var offered = options
                .Select(option => runtime.Cards
                    .Single(card => card.Id.Equals(option.Card.CardInstanceId))
                    .DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(
                offered,
                Is.EqualTo(new[]
                    {
                        IgnatiaCardContent.ErraticFireblast.Value,
                        IgnatiaCardContent.CrackOfDoom.Value,
                        IgnatiaCardContent.SpontaneousImmolation.Value,
                        IgnatiaCardContent.SearingHeat.Value,
                        IgnatiaCardContent.PathOfCinders.Value,
                        IgnatiaCardContent.UnstablePortal.Value
                    }.OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection,
                "Her three coloured Tier I faces each name BOTH of their colour's Tier II siblings, " +
                "so the first Level Up deals six candidates. " + Remaining(runtime));

            var taken = options.First(option => option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(hers))
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
                "The chosen face is the face that reaches her hand. " + Remaining(runtime));

            for (var budget = 0; budget < 60 &&
                runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                yield return NextDecision(root);
            }

            var after = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(hers))
                .ItemBonuses.Single(value => value.Key == taken.ItemIcon).Value;
            Assert.That(after, Is.EqualTo(before + 1),
                "p.16's item comes from the sibling the Level Up did not take.");
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(hers))
                    .ItemBonuses,
                Is.Not.Empty,
                "and a player reads it on her own dashboard rail.");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Red ladder's Tier III: the two printed repeats
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Loosely-Aimed Firebolts, ☾: "Target a unit in range in a straight line. May repeat once on a
        /// different hero."
        ///
        /// <para><b>The repeat is on ONE of the two printed lines, and a player meets it there.</b> The
        /// sun line prints no repeat at all, so the clause is reached only under the moon - which is why
        /// this journey runs once on the moon BOARD and once under Equilibrium's free choice on the sun
        /// board, and the second run is what proves the printed "or repeat" half of Equilibrium's own
        /// sentence: the freed choice is asked AGAIN inside the repeated instance.</para>
        ///
        /// <para>The enemy hero is stood at (-1,1), two away and ON one of her six files, so the moon
        /// line names him as well as the two enemy minions on her files - and the first instance
        /// deliberately takes a MINION, so that "on a different hero" has a hero left to name.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LooselyAimedFirebolstsRepeatsOnADifferentHero()
        {
            // The printed clause, under the coin's own moon.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[]
                {
                    IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaCardContent.Equilibrium
                },
                enemyHand: new[] { WaspCardContent.Telekinesis, WaspCardContent.Electrocute },
                enemySpace: EnemyOnHerFile);
            var root = Root();
            var runtime = root.Runtime;
            var enemy = Enemies(runtime)[0];

            // Her Tier III Red prints Initiative 8, which is also Shock's - and p.7 breaks a tie
            // with the coin and then flips it. The enemy commits a 3 so the staged face is the one
            // that runs, which is the whole point of loading the moon board.
            yield return BeginIgnatia(
                root, IgnatiaCardContent.LooselyAimedFirebolts, ActionKind.Attack,
                WaspCardContent.Telekinesis);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-attack-moon-select"), Remaining(runtime));
            Assert.That(OfferedUnits(runtime), Has.Member(enemy.Value),
                "Arrange: the enemy hero stands ON one of her files, so the moon line can name him " +
                "- which is what \"on a different hero\" needs left over. " + Remaining(runtime));
            yield return Pick(root, CandidateFor(runtime, RedRanged));

            var repeat = runtime.PendingEffectChoice;
            Assert.That(repeat, Is.Not.Null,
                "\"May repeat once\" opens after its own arm has bound a target. " +
                Remaining(runtime));
            Assert.That(repeat.NodeId.Value, Is.EqualTo("ignatia-attack-moon-repeat"));
            AssertDeclaredLabels(
                root, "ignatia-attack-moon-repeat", IgnatiaEffectContent.DecisionLabels());
            yield return May(root);
            yield return null;
            yield return null;

            var second = runtime.PendingEffectChoice;
            Assert.That(second, Is.Not.Null,
                "Taking the repeat performs the whole action again, card text and all. " +
                Remaining(runtime));
            Assert.That(second.NodeId.Value, Is.EqualTo("ignatia-attack-moon-select"),
                "and the coin has not moved, so the same printed line runs. " + Remaining(runtime));
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                Is.False,
                "Her printed Attack 5 defeated the ranged minion the first instance took, so it is " +
                "off the board rather than merely excluded. " + Remaining(runtime));
            Assert.That(OfferedUnits(runtime), Has.Member(enemy.Value),
                "and \"a different hero\" has a hero to name. " + Remaining(runtime));

            // CLOSED at milestone 8.75d, and recorded as a gap by 8.75c's own run of this line -
            // ID-Q10c. "On a different HERO" narrows the repeated instance to p.19's heroes, and
            // the clause now reaches a ByCardText subject, so the enemy melee minion standing on
            // one of her files is NOT offered although the identical printed sentence offered it to
            // the first instance a moment ago.
            Assert.That(OfferedUnits(runtime), Has.No.Member(RedMeleeOnAFile.Value),
                "\"on a different HERO\": the repeated instance's card-text subject is narrowed to " +
                "p.19's heroes by the clause that produced it. " + Remaining(runtime));
            yield return Pick(root, CandidateFor(runtime, enemy));
            yield return null;
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            yield return null;
            SeatsAgree(runtime, "the repeated attack");
            Healthy(runtime);

            // The same clause under Equilibrium: the face is asked on the primary AND on the repeat.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[]
                {
                    IgnatiaCardContent.Equilibrium, IgnatiaCardContent.LooselyAimedFirebolts
                },
                enemyHand: new[]
                {
                    WaspCardContent.Shock, WaspCardContent.Electrocute, WaspCardContent.KineticBlast
                },
                enemySpace: EnemyOnHerFile);
            var freeRoot = Root();
            var freeRuntime = freeRoot.Runtime;
            var freeEnemy = Enemies(freeRuntime)[0];

            yield return BeginIgnatia(
                freeRoot, IgnatiaCardContent.Equilibrium, ActionKind.Skill, WaspCardContent.Shock);
            yield return HerNextTurnOfTheSameRound(
                freeRoot, IgnatiaCardContent.LooselyAimedFirebolts, ActionKind.Attack,
                WaspCardContent.Electrocute);

            Assert.That(freeRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo(IgnatiaEffectContent.CoinGateNode),
                "\"Each time you PERFORM ... a primary action\". " + Remaining(freeRuntime));
            yield return PressFace(freeRoot, PrintedTextFace.Moon);
            Assert.That(freeRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-attack-moon-select"), Remaining(freeRuntime));
            yield return Pick(freeRoot, CandidateFor(freeRuntime, RedRanged));
            Assert.That(freeRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-attack-moon-repeat"), Remaining(freeRuntime));
            yield return May(freeRoot);
            yield return null;
            yield return null;

            Assert.That(freeRuntime.PendingEffectChoice, Is.Not.Null, Remaining(freeRuntime));
            Assert.That(freeRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo(IgnatiaEffectContent.CoinGateNode),
                "\"... OR REPEAT a primary action\" - the repeated instance asks the face again, " +
                "which is the second half of Equilibrium's printed sentence. " +
                Remaining(freeRuntime));
            yield return PressFace(freeRoot, PrintedTextFace.Moon);
            Assert.That(freeRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-attack-moon-select"), Remaining(freeRuntime));
            Assert.That(
                freeRuntime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                Is.False,
                "and the unit the first instance took is off the board. " + Remaining(freeRuntime));
            yield return Pick(freeRoot, CandidateFor(freeRuntime, freeEnemy));
            yield return null;
            yield return ClickDefenseFace(freeRoot, WaspCardContent.KineticBlast);
            yield return null;
            yield return null;
            SeatsAgree(freeRuntime, "the freed repeat");
            Healthy(freeRuntime);
        }

        /// <summary>
        /// Imminent Eruption, ☀: "Target a unit adjacent to you. May repeat once on a minion."
        ///
        /// <para>Its twin at the other end of p.19's unit kinds, on the SUN line this time - so the
        /// board stands the enemy hero in her own ring beside the enemy ranged minion already there,
        /// the first instance takes the HERO, and the repeat has the minion to name. The second run
        /// refuses it, which is the half that makes "may" a decision rather than a step.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ImminentEruptionRepeatsOnAMinionOrStopsAfterTheFirstAttack()
        {
            foreach (var takeTheRepeat in new[] { true, false })
            {
                yield return StageIgnatia(
                    SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.ImminentEruption, IgnatiaCardContent.Equilibrium
                    },
                    enemyHand: new[]
                    {
                        WaspCardContent.Telekinesis, WaspCardContent.Electrocute
                    },
                    enemySpace: AdjacentEnemy);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Enemies(runtime)[0];

                // MEASURED at this milestone's first Unity run: Imminent Eruption prints Initiative
                // 8 and so does Wasp's Shock, and p.7 breaks a tie with the Tie Breaker coin and
                // then FLIPS it - so committing Shock against this face turns the coin over before
                // she acts and runs her MOON line. The enemy commits a 3 instead, which leaves the
                // coin where the staging put it and her acting first.
                yield return BeginIgnatia(
                    root, IgnatiaCardContent.ImminentEruption, ActionKind.Attack,
                    WaspCardContent.Telekinesis);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo("ignatia-attack-sun-select"), Remaining(runtime));
                Assert.That(
                    OfferedUnits(runtime),
                    Is.EqualTo(new[] { enemy.Value, RedRanged.Value }
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection,
                    "☀ \"a unit adjacent to you\" - p.19's units, so the enemy hero standing in her " +
                    "ring is one of them. " + Remaining(runtime));
                yield return Pick(root, CandidateFor(runtime, enemy));

                var repeat = runtime.PendingEffectChoice;
                Assert.That(repeat, Is.Not.Null, Remaining(runtime));
                Assert.That(repeat.NodeId.Value, Is.EqualTo("ignatia-attack-sun-repeat"));
                AssertDeclaredLabels(
                    root, "ignatia-attack-sun-repeat", IgnatiaEffectContent.DecisionLabels());
                yield return May(root, accept: takeTheRepeat);
                yield return null;
                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                yield return null;
                // THE FIRST INSTANCE'S OWN OUTCOME (milestone 8.86): the hero it named answered
                // p.14, and the face they answered with is spent - an Attack that asked the target
                // question and applied nothing would have passed this journey before.
                Assert.That(
                    runtime.Cards.Single(card =>
                        card.OwnerId.Equals(enemy) &&
                        card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                        .State.Location,
                    Is.EqualTo(CardLocation.DiscardPile),
                    "☀ the adjacent hero the first instance took really answered the Attack. " +
                    Remaining(runtime));

                if (takeTheRepeat)
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                        "The second instance performs the whole action again. " +
                        Remaining(runtime));
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                        Is.EqualTo("ignatia-attack-sun-select"), Remaining(runtime));
                    Assert.That(OfferedUnits(runtime), Has.Member(RedRanged.Value),
                        "\"on a MINION\" - the minion in her ring is what the printed clause names, " +
                        "and the repeated instance is offered it. " + Remaining(runtime));

                    // CLOSED at milestone 8.75d - ID-Q10c, the gap milestone 8.75c's first Unity run
                    // measured on this very line. "May repeat once ON A MINION" narrows what the
                    // second instance may TAKE, and `FindLegalAttackTargets` has applied that
                    // narrowing (and the exclusion of the unit the asking instance took) to rulebook
                    // p.13's own target question since milestone 8.24. Her Attacks reach p.14's step
                    // 1 with NO target and name one in card text instead, and until 8.75d the
                    // card-text selection never asked: it re-ran the printed subject whole, so the
                    // hero this instance just attacked was offered again and pressing him stopped
                    // the Attack without a target - the offer-admits-more-than-the-answer-can-perform
                    // class of milestones 8.29d, 8.29e, 8.35z5 and 8.35z15. The clause now reaches
                    // the subject, so the offer is the admissible set and nothing else.
                    Assert.That(OfferedUnits(runtime), Has.No.Member(enemy.Value),
                        "\"on a MINION\": the hero the first instance took is neither the kind the " +
                        "clause names nor a different unit, so the repeat does not offer him. " +
                        Remaining(runtime));
                    yield return Pick(root, CandidateFor(runtime, RedRanged));
                    yield return null;
                    yield return null;
                    // THE REPEATED INSTANCE'S OWN OUTCOME (milestone 8.86): "on a MINION" was
                    // asserted over the offer; the minion it took is now asserted defeated.
                    Assert.That(
                        runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                        Is.False,
                        "\"May repeat once on a minion\" - the repeated Attack really ran. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "Refusing the repeat finishes the card. " + Remaining(runtime));
                    Assert.That(
                        runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                        Is.True,
                        "and the minion the refused repeat would have taken is still standing - " +
                        "which is what makes the refusal a decision rather than an absence. " +
                        Remaining(runtime));
                }

                SeatsAgree(runtime, takeTheRepeat ? "the minion repeat" : "the refused repeat");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Blue ladder's Tier III: the escalation, and the defeat that pays
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Violent Conflagration: "☀: An enemy hero in radius, adjacent to a token, or a minion,
        /// discards a card, or is defeated. ☾: Defeat an enemy minion in radius adjacent to an enemy
        /// hero."
        ///
        /// <para><b>"Or is defeated" is one mandatory step with two ways to complete</b> - ledger
        /// question 13's reading - so nobody is given a decision between them and which way it goes is
        /// decided by whether the named hero can discard. The two runs below differ in exactly one
        /// staged fact: how many cards the enemy was dealt. With two in hand he discards one; with ONE,
        /// p.7's own commitment has already taken it into the turn area by the time she acts, so the
        /// hero the sentence names cannot discard and the second arm defeats him.</para>
        ///
        /// <para>And the moon line is the Tier III word: where the lower two REMOVE the minion, this one
        /// runs p.13's Defeat, which pays its bounty into her purse.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ViolentConflagrationDiscardsOrDefeatsAndItsMoonDefeatPaysACoin()
        {
            foreach (var emptyHanded in new[] { false, true })
            {
                yield return StageIgnatia(
                    SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.ViolentConflagration, IgnatiaCardContent.Equilibrium
                    },
                    enemyHand: emptyHanded
                        ? new[] { WaspCardContent.Shock }
                        : new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    enemySpace: EnemyOnHerFile);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Enemies(runtime)[0];

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.ViolentConflagration, ActionKind.Skill,
                    WaspCardContent.Shock);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo("ignatia-discard-sun-select"), Remaining(runtime));
                Assert.That(OfferedUnits(runtime), Is.EqualTo(new[] { enemy.Value }).AsCollection,
                    "p.17's \"if able\" is gone from this Tier, but the SUBJECT is unchanged: a " +
                    "hero with nothing to discard is still named, and defeated instead. " +
                    Remaining(runtime));
                Assert.That(HeldBy(runtime, enemy), emptyHanded ? Is.Empty : Is.Not.Empty,
                    "Arrange: p.7's commitment has taken his one card into the turn area. " +
                    Remaining(runtime));

                yield return Pick(root, CandidateFor(runtime, enemy));
                if (emptyHanded)
                {
                    yield return null;
                    yield return null;
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "Nobody is asked anything: the escalation is a branch rather than a " +
                        "decision. " + Remaining(runtime));
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                        "\"... or is defeated\" - p.13 takes his piece off the board. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
                    Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemy),
                        Remaining(runtime));
                    yield return ClickACardFace(root);
                    yield return null;
                    Assert.That(HeldBy(runtime, enemy), Is.Empty, Remaining(runtime));
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                        "A hero who DID discard is not defeated. " + Remaining(runtime));
                }

                SeatsAgree(runtime, emptyHanded ? "the escalation" : "the discard");
                Healthy(runtime);
            }

            // ☾ the Tier III word: Defeat, which pays.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[]
                {
                    IgnatiaCardContent.ViolentConflagration, IgnatiaCardContent.Equilibrium
                },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                enemySpace: EnemyOnHerFile);
            var moonRoot = Root();
            var moonRuntime = moonRoot.Runtime;
            var moonHer = Ignatia(moonRuntime);

            yield return BeginIgnatia(
                moonRoot, IgnatiaCardContent.ViolentConflagration, ActionKind.Skill,
                WaspCardContent.Shock);
            Assert.That(moonRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-discard-moon-select"), Remaining(moonRuntime));
            Assert.That(OfferedUnits(moonRuntime),
                Is.EqualTo(new[] { RedMeleeOffEveryFile.Value }).AsCollection,
                Remaining(moonRuntime));
            var purse = moonRuntime.CoinsOf(moonHer);
            yield return Pick(moonRoot, CandidateFor(moonRuntime, RedMeleeOffEveryFile));
            yield return null;
            Assert.That(
                moonRuntime.ReadModel.Minions.Any(minion =>
                    minion.MinionId.Equals(RedMeleeOffEveryFile)),
                Is.False,
                Remaining(moonRuntime));
            Assert.That(moonRuntime.CoinsOf(moonHer), Is.GreaterThan(purse),
                "p.13's DEFEAT pays a bounty where the lower Tiers' Remove pays nothing, which is " +
                "the whole difference this Tier's printed word makes. " + Remaining(moonRuntime));
            SeatsAgree(moonRuntime, "the defeated minion");
            Healthy(moonRuntime);
        }

        /// <summary>
        /// Scorching Blaze: "☀: Move a friendly hero in radius 2 or 3 spaces in a straight line.
        /// ☾: Move an enemy hero in radius 2 or 3 spaces in a straight line."
        ///
        /// <para>Searing Heat's pair with a FLOOR below its distance, which is milestone 8.59's
        /// correction read on a second subject: "2 or 3" lights the ends of the files at two AND at
        /// three and nothing nearer, so a file blocked at three still offers its second space and a
        /// file open to three offers both.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ScorchingBlazePushesTwoOrThreeSpacesInAStraightLine()
        {
            // ☀ a friendly hero, on the ally board, standing where both distances are open.
            yield return StageIgnatia(
                SceneCardContent.VerifiedStagedIgnatiaWithAlly,
                hand: new[] { IgnatiaCardContent.ScorchingBlaze, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                allySpace: AllyOnHerFile);
            var sunRoot = Root();
            var sunRuntime = sunRoot.Runtime;
            var ally = Ally(sunRuntime);

            yield return BeginIgnatia(
                sunRoot, IgnatiaCardContent.ScorchingBlaze, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            Assert.That(sunRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-sun-select"), Remaining(sunRuntime));
            var allyStood = Position(sunRuntime, ally);
            yield return Pick(sunRoot, CandidateFor(sunRuntime, ally));

            Assert.That(sunRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-sun-line"), Remaining(sunRuntime));
            Assert.That(
                OfferedSpaces(sunRuntime)
                    .OrderBy(value => value.Q).ThenBy(value => value.R).ToArray(),
                Is.EqualTo(new[]
                    {
                        new HexCoordinate(-1, 3), new HexCoordinate(1, 1),
                        new HexCoordinate(2, 1)
                    }).AsCollection,
                "Two of his six files hold two free spaces and one of those holds three, so the " +
                "printed \"2 or 3\" lights three candidates. " + Remaining(sunRuntime));
            Assert.That(
                OfferedSpaces(sunRuntime).All(space =>
                    Distance(space, allyStood) == 2 || Distance(space, allyStood) == 3),
                Is.True,
                "and nothing nearer than the printed floor. " + Remaining(sunRuntime));
            yield return Space(sunRoot, new HexCoordinate(2, 1));
            yield return null;
            Assert.That(Distance(Position(sunRuntime, ally), allyStood), Is.EqualTo(3),
                "The longer of the two printed distances, walked whole. " + Remaining(sunRuntime));
            SeatsAgree(sunRuntime, "the friendly three-space push");
            Healthy(sunRuntime);

            // ☾ an enemy hero.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[] { IgnatiaCardContent.ScorchingBlaze, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var moonRoot = Root();
            var moonRuntime = moonRoot.Runtime;
            var enemy = Enemies(moonRuntime)[0];

            yield return BeginIgnatia(
                moonRoot, IgnatiaCardContent.ScorchingBlaze, ActionKind.Skill,
                WaspCardContent.Shock);
            Assert.That(moonRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-moon-select"), Remaining(moonRuntime));
            var enemyStood = Position(moonRuntime, enemy);
            yield return Pick(moonRoot, CandidateFor(moonRuntime, enemy));
            Assert.That(
                OfferedSpaces(moonRuntime)
                    .OrderBy(value => value.Q).ThenBy(value => value.R).ToArray(),
                Is.EqualTo(new[]
                    {
                        new HexCoordinate(-2, -1), new HexCoordinate(-2, 0),
                        new HexCoordinate(-2, 4), new HexCoordinate(-2, 5),
                        new HexCoordinate(0, 2)
                    }).AsCollection,
                "Three of the enemy hero's files hold two free spaces and two of those hold three. " +
                Remaining(moonRuntime));
            yield return Space(moonRoot, new HexCoordinate(-2, 5));
            yield return null;
            Assert.That(Distance(Position(moonRuntime, enemy), enemyStood), Is.EqualTo(3),
                Remaining(moonRuntime));
            Assert.That(HeroView(enemy).Coordinate, Is.EqualTo(new HexCoordinate(-2, 5)));
            SeatsAgree(moonRuntime, "the enemy three-space push");
            Healthy(moonRuntime);
        }

        // -------------------------------------------------------------------------------------------
        // The Green ladder's Tier III: the supply that runs out, and the gate that moves twice
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Path of Flames: "☀: Move up to 4 spaces in a straight line. Place a magma token in each empty
        /// space you moved through, or out of. ☾: Place up to 4 magma tokens in radius."
        ///
        /// <para><b>THE ONE FACE OF HERS THAT SPENDS HER WHOLE PILE.</b> Her supply is FOUR
        /// (USER-CORRECTED 2026-09-27 at milestone 8.113, superseding the 2026-09-22 count of three) and
        /// both printed lines say four: a four-space walk lays a token in every one of its four wake
        /// spaces, and the moon line's four placements each land. With the pile full, p.18's
        /// short-supply step before each placement stands aside and asks nothing - which is why every
        /// question below is a placement.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PathOfFlamesSpendsHerWholePileOfFour()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.PathOfFlames, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var sunRoot = Root();
            var sunRuntime = sunRoot.Runtime;
            var hers = Ignatia(sunRuntime);

            yield return BeginIgnatia(
                sunRoot, IgnatiaCardContent.PathOfFlames, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(sunRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-magma-sun-line"), Remaining(sunRuntime));
            Assert.That(OfferedSpaces(sunRuntime), Has.Member(new HexCoordinate(1, 1)),
                "\"up to 4 spaces in a straight line\" reaches the end of her longest free file. " +
                Remaining(sunRuntime));
            yield return Space(sunRoot, new HexCoordinate(1, 1));
            yield return null;

            Assert.That(Position(sunRuntime, hers), Is.EqualTo(new HexCoordinate(1, 1)),
                Remaining(sunRuntime));
            Assert.That(
                MagmaSpaces(sunRuntime),
                Is.EqualTo(new[]
                    {
                        new HexCoordinate(-3, 1), new HexCoordinate(-2, 1),
                        new HexCoordinate(-1, 1), new HexCoordinate(0, 1)
                    }).AsCollection,
                "The wake names FOUR empty spaces and she owns four tokens, so every one of them " +
                "takes one. " + Remaining(sunRuntime));
            Assert.That(sunRuntime.PendingEffectChoice, Is.Null,
                "Nothing needed taking back, so nobody is asked and the card completes. " +
                Remaining(sunRuntime));
            SeatsAgree(sunRuntime, "the four-token wake");
            Healthy(sunRuntime);

            // ☾ the same pile as four separate decisions.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[] { IgnatiaCardContent.PathOfFlames, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var moonRoot = Root();
            var moonRuntime = moonRoot.Runtime;

            yield return BeginIgnatia(
                moonRoot, IgnatiaCardContent.PathOfFlames, ActionKind.Skill, WaspCardContent.Shock);
            for (var placement = 1; placement <= 4; placement++)
            {
                Assert.That(moonRuntime.PendingEffectChoice, Is.Not.Null, Remaining(moonRuntime));
                Assert.That(moonRuntime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo("ignatia-magma-moon-place-" + placement),
                    "a placement, never a reclamation, while the pile holds a token. " +
                    Remaining(moonRuntime));
                yield return Space(moonRoot);
            }

            Assert.That(MagmaSpaces(moonRuntime), Has.Length.EqualTo(4),
                "Four tokens, which is her whole pile. " + Remaining(moonRuntime));
            Assert.That(moonRuntime.PendingEffectChoice, Is.Null,
                "and the card completes. " + Remaining(moonRuntime));
            SeatsAgree(moonRuntime, "the spent pile");
            Healthy(moonRuntime);

            // And the printed "up to", refused outright - a step a player declines rather than a
            // supply that ran out, which are two different reasons for the same empty board.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.PathOfFlames, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var declineRoot = Root();
            var declineRuntime = declineRoot.Runtime;
            var stood = Position(declineRuntime, Ignatia(declineRuntime));

            yield return BeginIgnatia(
                declineRoot, IgnatiaCardContent.PathOfFlames, ActionKind.Skill,
                WaspCardContent.Shock);
            Assert.That(declineRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-magma-sun-line"), Remaining(declineRuntime));
            Assert.That(declineRuntime.PendingEffectChoice.DeclineInvocation, Is.Not.Null,
                Remaining(declineRuntime));
            yield return May(declineRoot, accept: false);
            Assert.That(Position(declineRuntime, Ignatia(declineRuntime)), Is.EqualTo(stood),
                Remaining(declineRuntime));
            Assert.That(MagmaSpaces(declineRuntime), Is.Empty,
                "A Move that did not happen has no wake. " + Remaining(declineRuntime));
            Assert.That(declineRuntime.PendingEffectChoice, Is.Null, Remaining(declineRuntime));
            SeatsAgree(declineRuntime, "the refused four-space line");
            Healthy(declineRuntime);
        }

        /// <summary>
        /// Rulebook p.18's short supply, reached the one way her dealt hand can reach it: Chaos
        /// Incarnate repeats Path of Flames in the same turn. "If you need to place more tokens than are
        /// available in the supply, first remove up to that number of tokens of the same type from the
        /// board, if able." Milestone 8.113.
        ///
        /// <para>The sun wake spends all four tokens; the flip turns the coin to the moon; the repeated
        /// action's first placement finds the pile empty, so the board asks which standing magma token
        /// comes back - a real click on a lit space - and the placement then puts it somewhere else.
        /// Declining the next reclamation declines that token, and the empty pile ends the card with
        /// four magma still on the board and never five.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ChaosIncarnateRepeatsPathOfFlamesAndTheEmptyPileTakesMagmaBack()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedTierThreeEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.PathOfFlames, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);
            Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Sun), Remaining(runtime));

            yield return BeginIgnatia(
                root, IgnatiaCardContent.PathOfFlames, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-magma-sun-line"), Remaining(runtime));
            var from = Position(runtime, hers);
            var fourAway = OfferedSpaces(runtime)
                .Where(space => Distance(space, from) == 4)
                .ToArray();
            Assert.That(fourAway, Is.Not.Empty,
                "the staged board leaves her a free four-space file. " + Remaining(runtime));
            yield return Space(root, fourAway[0]);
            yield return null;
            yield return null;

            var wake = MagmaSpaces(runtime);
            Assert.That(wake, Has.Length.EqualTo(4),
                "The four-space wake spends her whole pile. " + Remaining(runtime));

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateFlipNode), Remaining(runtime));
            yield return May(root);
            yield return null;
            yield return null;
            Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Moon), Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateRepeatNode), Remaining(runtime));
            yield return May(root);
            yield return null;
            yield return null;

            var reclaim = runtime.PendingEffectChoice;
            Assert.That(reclaim, Is.Not.Null, Remaining(runtime));
            Assert.That(reclaim.NodeId.Value, Is.EqualTo("ignatia-magma-moon-reclaim-1"),
                "The repeated moon line finds the pile empty, so p.18 asks first which standing " +
                "token comes back. " + Remaining(runtime));
            Assert.That(OfferedSpaces(runtime), Is.EquivalentTo(wake),
                "and every candidate is a magma token standing on the board. " + Remaining(runtime));
            Assert.That(reclaim.DeclineInvocation, Is.Not.Null, Remaining(runtime));
            var takenBack = wake[0];
            yield return Space(root, takenBack);
            yield return null;

            Assert.That(MagmaSpaces(runtime), Has.No.Member(takenBack),
                "the token the player clicked has left the board. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-magma-moon-place-1"), Remaining(runtime));
            yield return Space(root);
            yield return null;
            Assert.That(MagmaSpaces(runtime), Has.Length.EqualTo(4),
                "it came back into the pile and went straight out again. " + Remaining(runtime));

            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-magma-moon-reclaim-2"), Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Not.Null, Remaining(runtime));
            yield return May(root, accept: false);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Declining the reclamation declines its pass, and W10 (milestone 9.39) closes the " +
                "\"up to 4\" with it: the third and fourth are not asked and the card completes. " +
                Remaining(runtime));
            Assert.That(MagmaSpaces(runtime), Has.Length.EqualTo(4),
                "four magma on the board, never five. " + Remaining(runtime));
            SeatsAgree(runtime, "the reclaimed magma");
            Healthy(runtime);
        }

        /// <summary>
        /// Chaos Gate: "☀: Swap with a friendly unit in radius; you may move that unit 1 space.
        /// ☾: Swap with an enemy unit in radius; you may move 1 space."
        ///
        /// <para><b>The two printed lines move DIFFERENT pieces, and the semicolon is why.</b> On the
        /// sun face "that unit" is the one she exchanged with, and it moves from the space she was
        /// standing on; on the moon face the unnamed subject is p.10's default - the acting hero - and
        /// she moves from the space the Swap left her on. Both are "you MAY move", so this journey takes
        /// the step on one line and refuses it on the other.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ChaosGateSwapsAndThenMovesOneSpaceOrNone()
        {
            // ☀ swap with a friendly unit, then decline its one space.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.ChaosGate, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var sunRoot = Root();
            var sunRuntime = sunRoot.Runtime;
            var hers = Ignatia(sunRuntime);
            var herStood = Position(sunRuntime, hers);

            yield return BeginIgnatia(
                sunRoot, IgnatiaCardContent.ChaosGate, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(sunRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-gate-sun-select"), Remaining(sunRuntime));
            var minionStood = MinionView(BlueMelee).Occupancy.Coordinate;
            yield return Pick(sunRoot, CandidateFor(sunRuntime, BlueMelee));

            Assert.That(Position(sunRuntime, hers), Is.EqualTo(minionStood),
                "p.19's Swap first. " + Remaining(sunRuntime));
            Assert.That(MinionView(BlueMelee).Occupancy.Coordinate, Is.EqualTo(herStood));
            var trailing = sunRuntime.PendingEffectChoice;
            Assert.That(trailing, Is.Not.Null,
                "\"; you may move that unit 1 space\" is the semicolon's own step. " +
                Remaining(sunRuntime));
            Assert.That(trailing.NodeId.Value, Is.EqualTo("ignatia-gate-sun-move"));
            Assert.That(trailing.DeclineInvocation, Is.Not.Null, Remaining(sunRuntime));
            Assert.That(
                OfferedSpaces(sunRuntime).All(space => Distance(space, herStood) == 1),
                Is.True,
                "and the piece it offers to move is the SWAPPED UNIT, from the space she vacated. " +
                Remaining(sunRuntime));
            yield return May(sunRoot, accept: false);
            Assert.That(MinionView(BlueMelee).Occupancy.Coordinate, Is.EqualTo(herStood),
                "Refused, the unit stands where the Swap put it. " + Remaining(sunRuntime));
            Assert.That(sunRuntime.PendingEffectChoice, Is.Null, Remaining(sunRuntime));
            SeatsAgree(sunRuntime, "the refused trailing move");
            Healthy(sunRuntime);

            // ☾ swap with an enemy unit, then take her own one space.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[] { IgnatiaCardContent.ChaosGate, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var moonRoot = Root();
            var moonRuntime = moonRoot.Runtime;
            var moonHer = Ignatia(moonRuntime);
            var enemy = Enemies(moonRuntime)[0];
            var moonHerStood = Position(moonRuntime, moonHer);
            var enemyStood = Position(moonRuntime, enemy);

            yield return BeginIgnatia(
                moonRoot, IgnatiaCardContent.ChaosGate, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(moonRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-gate-moon-select"), Remaining(moonRuntime));
            yield return Pick(moonRoot, CandidateFor(moonRuntime, enemy));
            Assert.That(Position(moonRuntime, moonHer), Is.EqualTo(enemyStood),
                Remaining(moonRuntime));
            Assert.That(Position(moonRuntime, enemy), Is.EqualTo(moonHerStood));

            var move = moonRuntime.PendingEffectChoice;
            Assert.That(move, Is.Not.Null, Remaining(moonRuntime));
            Assert.That(move.NodeId.Value, Is.EqualTo("ignatia-gate-moon-move"));
            Assert.That(
                OfferedSpaces(moonRuntime).All(space => Distance(space, enemyStood) == 1),
                Is.True,
                "\"; you may move 1 space\" names nobody, so p.10's default is the acting hero - " +
                "walking from the space the Swap left her on. " + Remaining(moonRuntime));
            yield return Space(moonRoot);
            yield return null;
            var step = Position(moonRuntime, moonHer);
            Assert.That(Distance(step, enemyStood), Is.EqualTo(1),
                "and it is SHE who moved. " + Remaining(moonRuntime));
            Assert.That(HeroView(moonHer).Coordinate, Is.EqualTo(step));
            Assert.That(Position(moonRuntime, enemy), Is.EqualTo(moonHerStood),
                "while the unit she exchanged with stands where the Swap put it. " +
                Remaining(moonRuntime));
            SeatsAgree(moonRuntime, "the taken trailing move");
            Healthy(moonRuntime);
        }

        // -------------------------------------------------------------------------------------------
        // p.16's Chaos Incarnate: the coin turned over, and the action performed on the other face
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Chaos Incarnate: "The first time each turn after you perform a primary action you may flip
        /// the Tie Breaker coin; if you do, you may perform that action again, choosing different
        /// targets."
        ///
        /// <para><b>Chaos Bolt is the card it repeats, because its two printed lines name different
        /// KINDS.</b> ☀ takes a minion adjacent to her and ☾ takes a hero in range, so a repeat on the
        /// flipped face is not a second attack on a differently-chosen target - it is a second attack
        /// the first face could not have made at all, which is the clearest thing the flip can be shown
        /// to do.</para>
        ///
        /// <para><b>The flip is read on both surfaces that carry it.</b> The match's own coin, in the
        /// seat's projection, and the rail's tie-breaker banner - which eases to the other team's side
        /// and is, since milestone 8.75e, the only place on screen that says which of a two-text card's
        /// lines is live. Both are asserted on both sides of the flip, because the coin is the only
        /// reason her hand means what it means.</para>
        ///
        /// <para>And the once-per-turn bound is the condition's own: "the FIRST time each turn". After
        /// the repeated instance has performed a primary action of its own, no second flip is
        /// offered.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ChaosIncarnateFlipsTheCoinAndRepeatsTheActionOnTheOppositeFace()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedTierThreeEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.ChaosBolt, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);
            var enemy = Enemies(runtime)[0];

            // p.16 stands the unlocked Ultimate faceup in her own area, where a player reads it.
            var ultimate = runtime.Cards.Single(card =>
                card.OwnerId.Equals(hers) &&
                card.DefinitionReference.DefinitionId.Equals(IgnatiaCardContent.ChaosIncarnate));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "Her unlocked Ultimate stands in her own area rather than in the deck or the hand.");
            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(hers));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "and it is DRAWN on her rail, which is the reading half of p.16.");
            Assert.That(rail.UltimateCard.DisplayName.ToUpperInvariant(),
                Is.EqualTo("CHAOS INCARNATE"));
            Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Sun), Remaining(runtime));
            AssertBannerShows(root, PrintedTextFace.Sun);

            yield return BeginIgnatia(
                root, IgnatiaCardContent.ChaosBolt, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-attack-sun-select"), Remaining(runtime));
            Assert.That(OfferedUnits(runtime), Is.EqualTo(new[] { RedRanged.Value }).AsCollection,
                "☀ \"a minion adjacent to you\". " + Remaining(runtime));
            yield return Pick(root, CandidateFor(runtime, RedRanged));
            yield return null;
            yield return null;
            // THE FIRST ACTION'S OWN OUTCOME (milestone 8.86): p.16's flip is only reached because a
            // primary action was PERFORMED, and what performing it did is the minion leaving the
            // board.
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                Is.False,
                "☀ \"a minion adjacent to you\" - the Attack that opens the Ultimate's offer really " +
                "defeated the piece it named. " + Remaining(runtime));

            var flip = runtime.PendingEffectChoice;
            Assert.That(flip, Is.Not.Null,
                "Performing a primary action opens p.16's own offer. " + Remaining(runtime));
            Assert.That(flip.NodeId.Value, Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateFlipNode));
            Assert.That(flip.ChooserId, Is.EqualTo(hers),
                "\"you MAY flip\" - p.10 gives an unassigned decision to the card's controller. " +
                Remaining(runtime));
            AssertDeclaredLabels(
                root,
                IgnatiaEffectContent.ChaosIncarnateFlipNode,
                IgnatiaEffectContent.UltimateDecisionLabels());
            yield return May(root);
            yield return null;
            yield return null;

            Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Moon),
                "The coin is turned over, and with it the live text of her whole hand. " +
                Remaining(runtime));
            SeatsAgree(runtime, "the flipped coin");
            AssertBannerShows(root, PrintedTextFace.Moon);

            var repeat = runtime.PendingEffectChoice;
            Assert.That(repeat, Is.Not.Null,
                "\"; if you do, you may perform that action again\" - the second offer lives INSIDE " +
                "the first, so it is reached only by a flip that happened. " + Remaining(runtime));
            Assert.That(repeat.NodeId.Value,
                Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateRepeatNode));
            AssertDeclaredLabels(
                root,
                IgnatiaEffectContent.ChaosIncarnateRepeatNode,
                IgnatiaEffectContent.UltimateDecisionLabels());
            yield return May(root);
            yield return null;
            yield return null;

            var second = runtime.PendingEffectChoice;
            Assert.That(second, Is.Not.Null,
                "The repeated instance performs the whole action again, card text and all. " +
                Remaining(runtime));
            Assert.That(second.NodeId.Value, Is.EqualTo("ignatia-attack-moon-select"),
                "and it runs the OTHER printed line, because the coin it reads has moved. " +
                Remaining(runtime));
            Assert.That(OfferedUnits(runtime), Is.EqualTo(new[] { enemy.Value }).AsCollection,
                "☾ \"a hero in range\" - a subject the sun line could not have named at all. " +
                Remaining(runtime));
            yield return Pick(root, CandidateFor(runtime, enemy));
            yield return null;
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            yield return null;
            // THE REPEATED ACTION'S OWN OUTCOME (milestone 8.86): the hero the flipped face could
            // name answered p.14 and spent the face they answered with, so the repeat performed a
            // second attack rather than merely re-asking a question.
            Assert.That(
                runtime.Cards.Single(card =>
                    card.OwnerId.Equals(enemy) &&
                    card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "☾ \"a hero in range\" - the second attack the sun face could not have made really " +
                "reached him. " + Remaining(runtime));

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "\"The FIRST time each turn\" - the repeated instance is a primary action too, and " +
                "the ability's own once-per-turn key has already been spent. " + Remaining(runtime));
            Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Moon),
                "and the coin stays where the flip left it. " + Remaining(runtime));
            SeatsAgree(runtime, "the repeated action");
            Healthy(runtime);
        }

        /// <summary>
        /// The other two answers to the same pair of offers: the flip refused, which reaches no repeat
        /// at all, and the flip taken with the repeat refused, which leaves the coin turned over.
        ///
        /// <para>The second is the reading the sentence's semicolon settles: the text offers the flip on
        /// its own terms and never takes one back, so a player who turns the coin over and then declines
        /// the second action has changed what every card in the game says for the rest of the
        /// match.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ChaosIncarnatesTwoOffersMayEachBeRefused()
        {
            foreach (var takeTheFlip in new[] { false, true })
            {
                yield return StageIgnatia(
                    SceneCardContent.VerifiedTierThreeEngagedStagedIgnatia,
                    hand: new[] { IgnatiaCardContent.ChaosBolt, IgnatiaCardContent.Equilibrium },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.ChaosBolt, ActionKind.Attack, WaspCardContent.Shock);
                yield return Pick(root, CandidateFor(runtime, RedRanged));
                yield return null;
                yield return null;
                // MILESTONE 8.86: the action whose performance opens p.16's offer really performed.
                Assert.That(
                    runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                    Is.False,
                    "☀ the Attack that reaches the Ultimate's offer defeated the minion it named. " +
                    Remaining(runtime));

                Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateFlipNode), Remaining(runtime));
                yield return May(root, accept: takeTheFlip);
                yield return null;
                yield return null;

                if (!takeTheFlip)
                {
                    Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Sun),
                        "\"Leave the coin\" leaves it. " + Remaining(runtime));
                    AssertBannerShows(root, PrintedTextFace.Sun);
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "and a declined flip reaches no repeat, because the repeat lives inside the " +
                        "offer rather than beside it. " + Remaining(runtime));
                }
                else
                {
                    Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Moon), Remaining(runtime));
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                        Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateRepeatNode),
                        Remaining(runtime));
                    yield return May(root, accept: false);
                    yield return null;
                    yield return null;
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "\"Stop here\" finishes the turn's text. " + Remaining(runtime));
                    Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Moon),
                        "and the coin STAYS turned over - the text offers the flip on its own terms " +
                        "and never takes one back. " + Remaining(runtime));
                    AssertBannerShows(root, PrintedTextFace.Moon);
                }

                SeatsAgree(runtime, takeTheFlip ? "the refused repeat" : "the refused flip");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The session half every hero owes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.13's Defeat takes her piece off the board, p.13's respawn puts it back on a space
        /// a player clicks, and the Director's RESET rebuilds the whole session from its initial state.
        /// </summary>
        [UnityTest]
        public IEnumerator HerDefeatedPieceRespawnsAndRestartRebuildsTheSession()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedTierThreeEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.PathOfFlames, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                enemySpace: AdjacentEnemy);
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);
            var enemy = Enemies(runtime)[0];

            yield return CommitEverySeatOfHers(
                root, IgnatiaCardContent.PathOfFlames, WaspCardContent.Electrocute);
            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(hers).transform.position),
                Is.True,
                "The enemy's printed Attack reaches her where she stands. " + Remaining(runtime));
            yield return null;
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(hers)), Is.False,
                "An undefended Attack defeats her and the piece leaves the board. " +
                Remaining(runtime));
            SeatsAgree(runtime, "her own defeat");

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, hers, IgnatiaCardContent.Equilibrium);
            yield return YieldUntilTheNamedSeatActs(runtime, hers);
            var destination = runtime.RoundView.Model.HeroRespawnOptions.First().Destination;
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    Tile(destination).transform.position),
                Is.True,
                "p.13's respawn is answered by clicking the space. " + Remaining(runtime));
            yield return null;
            Assert.That(Position(runtime, hers), Is.EqualTo(destination));
            Assert.That(HeroView(hers).Coordinate, Is.EqualTo(destination),
                "and the rebuilt piece is drawn where the authority put it.");
            Healthy(runtime);

            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "RESET returns the initial state with no accepted history. " + Remaining(runtime));
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                    .Count(view => view.Occupancy.HeroId.Equals(hers)),
                Is.EqualTo(1),
                "One hero, one piece - a rebuild that leaves a stale view behind fails here.");
            Assert.That(
                runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(hers)).TrayCards,
                Is.Not.Empty,
                "and her tray is reconstructed rather than left blank. " + Remaining(runtime));
            Healthy(runtime);
        }

        /// <summary>
        /// Three pauses of hers survive the Director's retained-prefix Undo, and replaying the retained
        /// prefix reproduces exactly the offer that was there.
        ///
        /// <para>The three are chosen because each one is rebuilt from a different thing: the COIN GATE
        /// from a capability whose candidates are a vocabulary nothing on the card carries, the MAGMA
        /// SPACE from a live board reading of what is still empty, and the ATTACK SUBJECT from a
        /// card-text narrowing that has to be re-derived against the coin the restored turn was showing.
        /// Each also spends an input that must go STALE, which is asserted where every pick is.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerOwnPausesUndoAndTheReplayReproducesTheirOffers()
        {
            foreach (var face in new[]
            {
                IgnatiaCardContent.PathOfFlames,
                IgnatiaCardContent.LooselyAimedFirebolts
            })
            {
                yield return StageIgnatia(
                    SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                    hand: new[] { face, IgnatiaCardContent.Equilibrium },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    enemySpace: EnemyOnHerFile);
                var root = Root();
                var runtime = root.Runtime;
                var isAttack = face.Equals(IgnatiaCardContent.LooselyAimedFirebolts);

                yield return BeginIgnatia(
                    root, face, isAttack ? ActionKind.Attack : ActionKind.Skill,
                    WaspCardContent.Shock);
                var pause = runtime.PendingEffectChoice;
                Assert.That(pause, Is.Not.Null, face.Value + ". " + Remaining(runtime));
                var node = pause.NodeId;
                var offered = pause.Candidates.Select(candidate => candidate.Value.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray();

                string expectedNextNode = null;
                if (isAttack)
                {
                    // This case proves Undo/replay of the exact offer, not Loosely-Aimed
                    // Firebolts' target geometry (its dedicated journey owns that). Take one of
                    // the offer we just captured so the replay check stays independent of which
                    // eligible unit the corrected coin face exposes on this board.
                    yield return Pick(root, pause.Candidates.First(candidate =>
                        candidate.Value.Kind == EffectValueKind.Minion));
                    expectedNextNode = runtime.PendingEffectChoice?.NodeId.Value;
                }
                else
                {
                    yield return Space(root);
                }

                OpenDirectorDrawer(root.Hud);
                yield return null;
                UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
                yield return null;
                yield return null;
                UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
                yield return null;

                var restored = runtime.PendingEffectChoice;
                Assert.That(restored, Is.Not.Null,
                    "One press gives the question back unanswered. " + face.Value + ". " +
                    Remaining(runtime));
                Assert.That(restored.NodeId, Is.EqualTo(node), face.Value + ". " + Remaining(runtime));
                Assert.That(
                    restored.Candidates.Select(candidate => candidate.Value.ToString())
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    Is.EqualTo(offered).AsCollection,
                    "and the offer that comes back is the offer that was there - which for HER has " +
                    "to be re-derived against the coin the restored turn was showing. " +
                    face.Value + ". " + Remaining(runtime));

                if (isAttack)
                {
                    yield return Pick(root, restored.Candidates.First(candidate =>
                        candidate.Value.Kind == EffectValueKind.Minion));
                    Assert.That(runtime.PendingEffectChoice?.NodeId.Value, Is.EqualTo(expectedNextNode),
                        "Replaying the retained prefix reaches the same next question, or the same " +
                        "completed branch when this coin face prints no follow-up. " + Remaining(runtime));
                }
                else
                {
                    yield return Space(root);
                }

                SeatsAgree(runtime, face.Value + " replayed");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// The pause of hers that a snapshot could not carry, carried: Equilibrium's coin-face choice.
        ///
        /// <para><b>This is milestone 8.75b's own finding, proved where a player meets it.</b> A
        /// capability's candidates are never written into a snapshot - they are re-derived on restore by
        /// asking the handler again - and <c>ignatia-coin-sun@1</c> answers out of the resolving turn's
        /// Tie Breaker state. Until 8.75b that state was not threaded into <c>MaterializePendingChoice</c>,
        /// so the re-ask arrived with an unrevealed coin and the payload came back describing a pause
        /// the content no longer requested: milestone 8.35i's failure exactly, with the coin in place of
        /// the enclosing Attack. The restore below is taken at that pause and answered afterwards on
        /// the same surface.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerOwnCoinPauseSurvivesASnapshotRestore()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[]
                {
                    IgnatiaCardContent.Equilibrium, IgnatiaCardContent.PlayingWithFire
                },
                enemyHand: new[]
                {
                    WaspCardContent.Shock, WaspCardContent.Electrocute, WaspCardContent.KineticBlast
                });
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);

            yield return BeginIgnatia(
                root, IgnatiaCardContent.Equilibrium, ActionKind.Skill, WaspCardContent.Shock);
            yield return HerNextTurnOfTheSameRound(
                root, IgnatiaCardContent.PlayingWithFire, ActionKind.Attack,
                WaspCardContent.Electrocute);

            var gate = runtime.PendingEffectChoice;
            Assert.That(gate, Is.Not.Null, Remaining(runtime));
            Assert.That(gate.NodeId.Value, Is.EqualTo(IgnatiaEffectContent.CoinGateNode));
            var offered = gate.Candidates.Select(candidate => candidate.Value.ToString())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var captured = runtime.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null.And.Not.Empty);

            yield return PressFace(root, PrintedTextFace.Moon);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-attack-moon-select"),
                "Arrange: the board has moved past the captured moment. " + Remaining(runtime));

            Assert.That(runtime.RestoreSnapshot(captured), Is.True,
                "The captured pause restores. " + Remaining(runtime));
            yield return null;
            yield return null;
            var restored = runtime.PendingEffectChoice;
            Assert.That(restored, Is.Not.Null,
                "A pause survives a snapshot restore. " + Remaining(runtime));
            Assert.That(restored.NodeId.Value, Is.EqualTo(IgnatiaEffectContent.CoinGateNode),
                "and it is still the coin predicate's own question rather than a re-ask the " +
                "content no longer requested. " + Remaining(runtime));
            Assert.That(restored.ChooserId, Is.EqualTo(hers), Remaining(runtime));
            Assert.That(
                restored.Candidates.Select(candidate => candidate.Value.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(offered).AsCollection,
                "with BOTH printed faces still offered, which is what the restored Tie Breaker " +
                "state is for. " + Remaining(runtime));
            SeatsAgree(runtime, "the restored coin pause");

            yield return PressFace(root, PrintedTextFace.Sun);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-attack-sun-select"),
                "and answering it on the same surface carries on - onto the OTHER printed line " +
                "this time, which is what says the restored pause was a live question rather than " +
                "a replayed answer. " + Remaining(runtime));
            Healthy(runtime);
        }
    }
}
