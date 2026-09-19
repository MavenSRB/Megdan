using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.DodgerUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Dodger's starting-hand cards played the way a player plays them, from the engaged staging -
    /// Wasp and Dodger two spaces apart, with an enemy Ranged minion beside her and a friendly Melee
    /// minion beside her too. Milestone 8.35z34.
    ///
    /// <para>Every decision each journey is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the walk that puts her where a printed sentence can reach, the enemy's own turn, the enemy's
    /// own Attack - ride the headless command surface, exactly as milestones 8.15 through 8.24 do,
    /// and are marked where they appear.</para>
    ///
    /// <para><b>THE STAGED GEOMETRY IS HERO-INDEPENDENT, and this fixture reuses it rather than
    /// re-deriving it.</b> <c>EngagedMirrorStaging</c> restages whichever two heroes it is given onto
    /// the same pair of spaces, so the contact squares milestone 8.24 measured for Xargatha are the
    /// same squares for Dodger: the Blue seat stands at (-3, 1) and the Red seat at (-2, 2), whoever
    /// they are. Milestone 8.35z34 re-measured the board offline and confirmed it before reusing a
    /// single constant.</para>
    ///
    /// <para><b>What her kit does NOT do on this board, measured before these journeys were
    /// written.</b> Every minion Spawn point inside the Battle Zone is occupied at the staging, so all
    /// nine of her spawn-point faces read false here: Dread Razor's ranged bullet is not bought, Dark
    /// Ritual pays no coins, and her Defense values are their plain printed numbers. The journeys in
    /// this fixture are about the faces that work without a spawn point; the ones whose subject IS the
    /// clause live on the Tier III arm in <see cref="DodgerProgressedUiJourneyPlayModeTests"/>, where
    /// Tide of Darkness makes every empty space count. That division is content rather than
    /// convenience, and stating it is what stops a journey from silently proving nothing.</para>
    /// </summary>
    public sealed class DodgerEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Dread Razor: "Choose one — • Target a unit adjacent to you. • If you are adjacent to an
        /// empty spawn point in the battlezone, target a unit in range."
        ///
        /// <para>Rulebook p.10's modal form on a basic Attack, and the first face of hers a player
        /// meets. The staged enemy Ranged minion stands beside her, so the first bullet has a subject;
        /// the second is gated on a board condition the staging does not satisfy, which is what makes
        /// this journey a measurement rather than a formality.</para>
        ///
        /// <para><b>The modal IS the before-attack text.</b> Her three composed modal Attacks bind
        /// their target at p.14's step 1, so choosing a bullet and naming a unit are one question and
        /// its answer rather than two separate pauses.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DreadRazor_OffersItsTwoBulletsAndTakesTheUnitTheChosenOneAdmits()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Dread Razor's printed 12 ties Magnetic Dagger's, so the enemy commits their lowest and
            // she takes the turn outright rather than on a tie.
            yield return CommitEverySeat(root, dodgerId, NameOf(runtime, DodgerCardContent.DreadRazor));
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(dodgerId),
                "The journey needs her turn open. " + Remaining(runtime));

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "p.10's modal asks which bullet before p.14 asks anything. " + Remaining(runtime));
            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2),
                "Both printed bullets are drawn as their own controls - the modal offers what the " +
                "card prints, and the second one's board condition is about what it can then do " +
                "rather than about whether it is a printed option. " + Remaining(runtime));

            // The first bullet: "Target a unit adjacent to you", which the staged enemy Ranged minion
            // beside her answers.
            UiPlayer.ClickEffectAnswer(runtime, hud, bullets[0].Command);
            yield return null;
            yield return null;

            // THE TARGET QUESTION IS AN EFFECT SELECTION, NOT p.14's OWN, and that is content rather
            // than a quirk of this journey. Her three composed modal Attacks BIND their target at
            // p.14's step 1 through card text, so what a player answers is the selection the chosen
            // bullet opened - `PendingAttackTargetChoice` stays null because the Attack never gets to
            // ask. Milestone 8.35z34's first Play Mode run is what established this: the run reported
            // the prompt open with one offered target and that property empty, which is exactly the
            // shape a card-text binding produces.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The chosen bullet opens its own target selection. " + Remaining(runtime));

            var offered = MinionsAt(runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space));
            Assert.That(offered, Is.Not.Empty,
                "An enemy minion stands beside her on the staged board, and \"a unit adjacent to " +
                "you\" admits it. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, offered[0].transform.position), Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The named target closes the step-1 binding and the Attack resolves against it. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Dark Ritual: "If there are 2 or more empty spawn points in radius in the battle zone, gain
        /// 1 coin."
        ///
        /// <para>Her Green Tier I, started from its own Skill control. What it proves beyond the
        /// control is the honest half of her design on this board: the condition is FALSE at the
        /// engaged staging, because every Spawn point in the Battle Zone is occupied, so the ritual
        /// resolves and pays nothing. That is rulebook p.10's own reading of a false condition - it
        /// skips and the card continues - and it is the user's RULING 3 applied to a face rather than
        /// asserted about one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DarkRitual_IsStartedFromItsOwnSkillControlAndPaysOnTheBoardItReads()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var dodgerId = DodgerSeat(runtime).HeroId;

            // Dark Ritual prints Initiative 3, so the enemy has to commit something lower for her to
            // act first. CommitSoTheNamedSeatActs picks that card rather than guessing one.
            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.DarkRitual);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(dodgerId),
                "The journey needs her turn open on Dark Ritual. " + Remaining(runtime));

            var coinsBefore = runtime.CoinsOf(dodgerId);

            yield return PointTrayAt(root, dodgerId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The ritual asks a player nothing: its whole sentence is a board reading and a " +
                "payment. " + Remaining(runtime));

            var coinsAfter = runtime.CoinsOf(dodgerId);
            Assert.That(coinsAfter, Is.EqualTo(coinsBefore),
                "Every minion Spawn point in the Battle Zone is occupied at this staging, so \"2 or " +
                "more empty spawn points in radius\" is false and the clause pays nothing. The card " +
                "still resolves - p.10 skips a false condition rather than stopping the card - which " +
                "is what makes an unchanged balance the correct outcome instead of a failure.");
        }

        /// <summary>
        /// p.14's secondary Defense icon, spent as a whole face out of her own hand, with the Attack
        /// surviving rather than defeating her.
        /// </summary>
        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndDodgerSurvivesTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, dodgerId);

            // Setup, headlessly: she walks into contact and the enemy attacks her on the next turn.
            // Littlefinger of Death prints Movement 4, which is what reaches the contact square.
            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.LittlefingerOfDeath);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);
            yield return WalkHeadlesslyTo(runtime, dodgerId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's Attack is setup for this journey. " + Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(pending.LegalTargetHeroIds, Has.Member(dodgerId),
                "She walked into contact, so she is a legal target.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    pending.ResolutionId, pending.ChoiceId, dodgerId)),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "p.14 asks the defender before it compares anything. " + Remaining(runtime));

            // The answer is the player's own, made on the offered FACE rather than headlessly - and a
            // defense face is drawn as a card element rather than as a labelled button, which is what
            // milestone 8.35z34's first Play Mode run corrected here: the prompt showed three
            // `gd-defense-card` elements and no button carrying the invocation's label.
            var surviving = SurvivingDefense(runtime);
            Assert.That(surviving, Is.Not.Null,
                "She holds a face whose Defense answers this Attack. " + Remaining(runtime));

            var survivingFace = NameOfInstance(
                runtime, surviving.DefenseBreakdown.CardInstanceId.Value);
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-defense-card", survivingFace));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", survivingFace));
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(dodgerId)), Is.True,
                "The spent Defense face answered the Attack, so she is still standing.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(dodgerId)), Is.True,
                "and her token is still on the board.");
            Assert.That(DiscardOf(runtime, dodgerId), Is.Not.Empty,
                "The face she answered with is in her discard, which is p.14's own cost - and the " +
                "pile Middlefinger of Death's second bullet would one day read.");
        }

        /// <summary>
        /// p.13's Defeat and respawn, with the piece as its subject: the token leaves the board when
        /// the answer is a decline, and comes back on a clicked spawn space at her own next turn. It
        /// is also the half of the spawn claim the engaged staging leaves open - the space she returns
        /// to is the map's own Blue hero spawn area, not the space the staging moved her to.
        /// </summary>
        [UnityTest]
        public IEnumerator Dodger_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedDodger);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var dodgerId = DodgerSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, dodgerId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.LittlefingerOfDeath);
            yield return YieldUntilTheNamedSeatActs(runtime, dodgerId);
            yield return WalkHeadlesslyTo(runtime, dodgerId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null, Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    pending.ResolutionId, pending.ChoiceId, dodgerId)),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat; spending a face is " +
                "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndDodgerSurvivesTheAttack's subject.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(dodgerId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(dodgerId)), Is.False,
                "and her token is gone with her rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // p.13 offers the respawn at the defeated hero's own next turn, so the card she commits
            // for it has to take that turn: Dread Razor's printed 12 is the highest she holds.
            CommitSoTheNamedSeatActs(runtime, dodgerId, DodgerCardContent.DreadRazor);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(dodgerId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));
            yield return PointTrayAt(root, enemyId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Dodger must be offered her respawn at her own turn. Status: " +
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
                .Where(view => view.Occupancy.HeroId.Equals(dodgerId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Dodger token stands after the respawn, not a second one beside a stale.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True,
                "and it is her own official token that came back.");
        }
    }
}
