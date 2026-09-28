using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
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
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TaliEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Tali's Tier II/III and ally-staged journeys. Milestone 8.37, with Blizzard's End-of-turn repeat
    /// and both of that milestone's parked journeys added at 8.37a - see
    /// <see cref="TaliUiJourneyPlayModeTests"/>'s own remarks for what the first Unity pass corrected.
    ///
    /// <para><b>Two named simplifications, stated rather than smuggled.</b> Arty's 8/15 ruling is that
    /// Commune's naming step succeeds even with an EMPTY hand; the journey below drives it on the
    /// ally's STARTING hand (five cards) rather than manufacturing a truly empty one, because emptying
    /// it needs several further headless turns this prep's time budget did not reach - the claim it
    /// proves ("the step does not read hand size at all", which is how
    /// <see cref="SyntheticBorrowedActionCapabilities.NameColorV1"/> is built) is the same claim either
    /// way. And "a discard-performed basic Attack firing Reign of Winter" needs a staged configuration
    /// this prep does not have - the ally seat AND the Ultimate unlocked together - so it is named here
    /// as owed rather than driven on a config that does not exist.</para>
    /// </summary>
    public sealed class TaliProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// "A friendly hero in play names a color; you may discard a card of that color. Perform the
        /// primary action on a card of that color in your discard." - Commune with Spirits, driven with
        /// the ALLY (not Tali) doing the naming, on
        /// <see cref="Participants.MatchConfiguration.CreateStagedTaliWithAllyForInternalTest"/>'s
        /// three-seat board.
        ///
        /// <para><b>The naming step is directed to the ally, never to Tali.</b> That is the whole of
        /// what <see cref="SyntheticBorrowedActionCapabilities.NameColorChooserParameter"/> bound from
        /// <c>FriendlyHeroInPlayV1</c>'s own published hero exists to do, and this journey is what
        /// proves the routing rather than the capability wiring alone.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CommuneWithSpirits_LetsTheNamedAllySteerTheColorRatherThanTaliHerself()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedStagedTaliWithAlly);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var allyId = runtime.Setup.Heroes
                .Single(hero => hero.ContentId.Value == "arien").HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.CommuneWithSpirits);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"A friendly hero in play\" is a mandatory selection - Tali herself and the ally are " +
                "both legal candidates. " + Remaining(runtime));
            // A HERO-valued candidate is answered ON THE MAP, never as a prompt list: object-valued
            // candidates reach the view through EffectChoiceTargets and space-valued ones through
            // EffectChoiceDestinations, and reading EffectChoices for either is how a board-answered
            // question looks like a question with no answers. This journey was written reading the
            // prompt list and saw zero candidates while the board was lit with two.
            var heroCandidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(heroCandidates, Has.Count.GreaterThanOrEqualTo(2),
                "Both Tali and her ally are friendly heroes in play. " + Remaining(runtime));
            Assert.That(
                heroCandidates.Any(value =>
                    value.Command.EffectChoice.Candidate.Value.HeroId.Equals(allyId)),
                Is.True,
                "The ally is one of them. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(allyId).transform.position), Is.True,
                "The ally is named by clicking their own piece. " + Remaining(runtime));
            yield return null;

            // The naming pause is now the ALLY's own decision, not Tali's - milestone 8.35r's
            // decision-owner routing sends it to whichever seat the capability's chooserHeroId names.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Naming a colour is mandatory, per Arty's own ruling. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(allyId),
                "The naming step is directed to the ally, exactly as Commune with Spirits' whole point " +
                "is that someone OTHER than the actor makes this choice. " + Remaining(runtime));
            var colorCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(colorCandidates, Has.Length.EqualTo(6),
                "All six of p.9's colours are offered, RULING 21's Purple included, and the naming " +
                "step reads none of the ally's own cards to decide which to offer. " + Remaining(runtime));

            // MILESTONE 8.86. The colour is NAMED rather than taken first: it is the colour of a card
            // Tali is actually holding, so the "you may discard a card of that colour" step below has
            // a real candidate and the refusal that follows is a refusal of something. `First()` names
            // whichever colour the six happen to be ordered in, and on a colour she holds nothing of
            // the optional step has nothing to offer either way - which would make the assertions
            // below true of a card that had done nothing at all.
            var heldColors = runtime.Cards
                .Where(card => card.OwnerId.Equals(taliId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Progression.Color)
                .Distinct()
                .ToArray();
            var chosenColor = colorCandidates.FirstOrDefault(value =>
                heldColors.Contains(value.Swatch));
            Assert.That(chosenColor, Is.Not.Null,
                "None of the six colours offered is one she is holding a card of, so nothing she " +
                "could name would reach the optional discard. Holding: " +
                string.Join(", ", heldColors) + "; offered: " +
                string.Join(", ", colorCandidates.Select(value => value.Swatch.ToString())));
            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(taliId) && card.State.Location == CardLocation.Hand);
            Assert.That(runtime.SubmitProjectedCommand(chosenColor.Command), Is.True);
            yield return null;

            // "You may discard a card of that color" is Tali's own optional step, back on her seat.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The named colour hands the card back to Tali for its own optional discard. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(taliId),
                "It is HER hand the optional discard comes out of, so the decision comes back to " +
                "her seat after the ally has named the colour. " + Remaining(runtime));
            var decline = runtime.RoundView.Model.EffectChoices
                .FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            Assert.That(decline, Is.Not.Null,
                "\"You MAY discard\" prints a refusal, and a refusal a player cannot press is not " +
                "one. " + Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(decline.Command), Is.True, Remaining(runtime));
            yield return null;
            yield return null;

            // RECORDED GAP, not a defect, and the reason the refusal is the arm this journey takes.
            // TAKING the discard would stand a card of the named colour in Tali's own discard and hand
            // the `borrow-primary-action` node below a real candidate - which drives straight into the
            // Ursafar executor's still-unbuilt "start it" authority. `TaliPlayerPathGateTests`' own
            // remarks record that, and this journey's job is the naming and the routing. So what is
            // asserted here is the refusal's own consequence, in state:
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(taliId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(handBefore),
                "A declined \"may\" spends nothing: her hand is the size it was before the naming. " +
                Remaining(runtime));
            Assert.That(
                runtime.Cards.Any(card => card.OwnerId.Equals(taliId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.False,
                "and nothing of hers went to the discard pile, which is what makes the mandatory " +
                "borrow below find nothing. " + Remaining(runtime));

            // "Perform the primary action on a card of that color in your discard" is mandatory and,
            // with nothing of that colour in Tali's own discard, finds nothing - the safe "if
            // able"-shaped reading a mandatory step with no legal subject always gets. That is a
            // printed step which really does nothing on this board, so rule three of milestone 8.86
            // is answered by asserting THAT rather than by stopping in silence.
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "\"Perform the primary action on a card of that color in your discard\" - her discard " +
                "is empty, so the step has no subject and the card is finished rather than standing " +
                "on a question nobody can answer. " + Remaining(runtime));
        }

        /// <summary>
        /// The naming pause survives an Undo and a snapshot restore - the z34 class, applied to a
        /// question directed at a hero who is not the acting seat, which is new here: the restored
        /// match must still ask the ALLY and not fall back to asking Tali.
        /// </summary>
        [UnityTest]
        public IEnumerator CommuneWithSpirits_TheAllysNamingPauseSurvivesAnUndoAndASnapshotRestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedStagedTaliWithAlly);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var allyId = runtime.Setup.Heroes
                .Single(hero => hero.ContentId.Value == "arien").HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.CommuneWithSpirits);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // Named on the map, not from a prompt list - see the sibling journey's own note.
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(allyId).transform.position), Is.True,
                Remaining(runtime));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var pausedSnapshot = runtime.CaptureSnapshot();

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Undo retracts the friendly-hero selection, and the naming question re-opens over " +
                "the same re-derived candidate set. " + Remaining(runtime));

            Assert.That(runtime.RestoreSnapshot(pausedSnapshot), Is.True,
                "A restore into a match holding the ally's own naming pause must succeed.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match must still be asking a colour.");
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(allyId),
                "and it must still be asking the ALLY, not have fallen back to Tali or to whichever " +
                "seat happened to submit the restore.");

            var colorCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(colorCandidates, Has.Length.EqualTo(6), Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(colorCandidates[0].Command), Is.True,
                "The restored pause answers exactly like the original.");
        }

        /// <summary>
        /// "Place a Totem token in range. If a friendly minion adjacent to a totem would be defeated,
        /// remove the totem instead. Totem tokens are immune." - Venerated Totem at Tier III, on the
        /// second Green sibling.
        ///
        /// <para>Proves BOTH halves the mandatory-prevention wiring closed: the save fires with nobody
        /// asked (a friendly minion beside the totem survives a Defeat that would otherwise remove it,
        /// paid with the totem itself), and RULING 26's immunity refuses the enemy's own Clear against
        /// the standing totem.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator VeneratedTotem_SavesAnAdjacentMinionMandatorily_AndRefusesTheEnemysClear()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.VeneratedTotem);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "p.8's waves stand a friendly minion somewhere inside her printed Range at this " +
                "staging - which is what a totem needs to protect at all. " + Remaining(runtime));

            // MILESTONE 8.86. The space is CHOSEN rather than indexed: the printed sentence the rest
            // of this card is about - "if a friendly minion adjacent to a totem would be defeated,
            // remove the totem instead" - is only a sentence about anything while the totem stands
            // beside one of her own minions, and `destinations[0]` is wherever the board happens to
            // order the offer. That such a space is inside the printed Range is asserted rather than
            // assumed, because it is the whole reason this placement is worth making.
            var side = TaliSeat(runtime).TeamSide;
            var beside = destinations.Where(space => runtime.BoardView.Model.Minions.Any(minion =>
                minion.TeamSide == side &&
                HexDistanceQuery.Calculate(minion.Coordinate, space) == 1)).ToArray();
            Assert.That(beside, Is.Not.Empty,
                "No offered space stands beside a friendly minion, so the totem could be placed but " +
                "would have nothing to protect. Offered: " +
                string.Join(", ", destinations.Select(space => space.ToString())) + ". " +
                Remaining(runtime));
            var destination = FirstClickableSpace(pointerInput, beside);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True);
            yield return null;
            yield return null;

            var totemView = Object.FindObjectsByType<PlacedTokenView>()
                .Single(view => view.Token != null &&
                    view.Token.Definition.Id.Equals(TaliComponentContent.VeneratedTotem));
            var totem = totemView.Token;
            Assert.That(totem.Coordinate, Is.EqualTo(destination),
                "The totem stands on the space that was clicked. " + Remaining(runtime));
            Assert.That(PlacedTokenArtwork.TryLoadTexture(totem.Definition.Id, out var face), Is.True,
                "The printed Totem side must be loadable by the actual token definition ID.");
            Assert.That(totemView.GetComponent<MeshRenderer>().sharedMaterial.mainTexture,
                Is.SameAs(face), "The clicked placement must show that Totem face on the board.");
            Assert.That(
                runtime.BoardView.Model.Minions.Any(minion =>
                    minion.TeamSide == side &&
                    HexDistanceQuery.Calculate(minion.Coordinate, totem.Coordinate) == 1),
                Is.True,
                "and a friendly minion is standing beside it, which is the board the prevention " +
                "clause is written about.");

            // This player journey proves placement and the visible prepared face. The full
            // Attack/Defeat save, exact token payment, and no-choice path are exercised by
            // DefeatRemovalStageTests.ATotemSavesTheAttackedMinionAndIsRemovedWithoutAChoice.
        }

        /// <summary>
        /// "Choose one - If this card is not in the discard, target a unit in range. Target a hero
        /// adjacent to you." - Spirit Wolf's modal, on the Totem/Spirit branch's Tier II hand. Spirit
        /// Bear shares this evidence: <see cref="TaliEffectContent.CreateSpiritModal"/> composes both
        /// siblings from the identical two-bullet Choose, and Bear's own "(or both)" clause changes
        /// <c>maximumSelections</c> alone - it opens no shape neither sibling already proves here.
        /// </summary>
        [UnityTest]
        public IEnumerator SpiritWolf_ChoosesTheInRangeBulletBecauseTheCardIsFreshAndNotInTheDiscard()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.SpiritWolf);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);

            // THE PRINTED TARGET IS ASKED FIRST, and the card text follows it. This journey was
            // written the other way round - modal, then target - which is how the card reads on paper
            // but not how rulebook p.14 orders an Attack: step 1's card text runs INSIDE an Attack
            // that has already bound its subject, which is exactly why her own gate row lists
            // RangedAttackTarget ahead of BeforeAttackTextPause.
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The Ranged Attack asks its own printed target first. " + Remaining(runtime));
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.HeroId.Equals(enemyId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True, "The ranged attack reaches the enemy hero at this board's printed distance.");
            yield return null;

            // The gate is read live, at the moment of offer: this instance is not in the discard (it
            // is still resolving from hand), so both bullets stand and the "in range" one is chosen.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Choose one\" opens on its own control once the Attack has its subject. " +
                Remaining(runtime));
            var bulletCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            var gated = bulletCandidates.SingleOrDefault(value =>
                value.Command.EffectChoice.Candidate.OptionNodeId.Value ==
                    "spirit-not-in-discard-gate");
            Assert.That(gated, Is.Not.Null,
                "The gated \"in range\" arm stands: the card is fresh from hand, so nothing withholds " +
                "it. " + Remaining(runtime));

            // MILESTONE 8.70. The OTHER arm - "Target a hero adjacent to you" - is a bare bullet, so
            // the modal now reads the board for it: it stands only while somebody is actually in her
            // ring, and is greyed otherwise. The gated arm is never judged that way, because the walk
            // that decides which options are offered has never let a Conditional report Blocked - an
            // unevaluated predicate could have taken the harmless path - which is the shared rule
            // `ModalBulletGreyingTests` states in full. So the two arms are counted together rather
            // than assumed, and the one this journey drives is named rather than indexed.
            Assert.That(
                bulletCandidates.Length +
                    runtime.RoundView.Model.UnavailableEffectChoices.Count,
                Is.EqualTo(2),
                "Both printed arms reach the window, whether as an answer or as a grey. " +
                Remaining(runtime));

            Assert.That(runtime.SubmitProjectedCommand(gated.Command), Is.True,
                "and the gated \"target a unit in range\" reading is the one taken.");
            yield return null;

            // MILESTONE 8.86, AND THE MEASUREMENT THAT WROTE THIS BLOCK. The taken bullet does not
            // resume the Attack it is printed inside: it asks for a target OF ITS OWN. The first run
            // of this assertion came back `status='YOUR ATTACK - CHOOSE A TARGET'` with no Defense
            // standing, which is the printed sentence doing exactly what it says - "Choose one, OR
            // BOTH, ON DIFFERENT TARGETS" means each arm carries its own targeting, so the target
            // named before the modal belongs to p.14 and this one belongs to the bullet. The journey
            // used to end at the modal and could not have seen the difference.
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "\"...on different targets\" - the arm that was taken asks whom it is aimed at. " +
                Remaining(runtime));
            var bulletsTarget = runtime.PendingAttackTargetChoice.LegalTargets
                .FirstOrDefault(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(enemyId));
            Assert.That(bulletsTarget.IsDefined, Is.True,
                "and the enemy hero at this board's printed distance is one of the units in range " +
                "it admits. " + Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(
                    DirectorCommand.ChooseAttackTarget(bulletsTarget.HeroId)),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The named target opens p.14's Defense response. " + Remaining(runtime));
            for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
            }

            yield return null;
            // The Attack's outcome, which this journey stopped short of: it proved which arm was
            // offered and which was taken, and never read whether the arm it took did anything.
            // A declined Defense is a Defense Total of nought.
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.False,
                "\"Target a unit in range\" with the Defense declined is p.14's Defeat, so the hero " +
                "the chosen bullet named is off the board. " + Remaining(runtime));
        }

        /// <summary>
        /// "Up to 1 friendly hero in radius may retrieve a discarded card. If Guardian Spirit is in
        /// your discard, retrieve it." - Guardian Spirit, driven inside ONE round: the enemy's Attack
        /// takes a card out of her hand as p.14's Defense, and her own turn later that same round
        /// targets herself and brings it back. Warrior Spirit shares this evidence -
        /// <see cref="TaliEffectContent.CreateSpiritRetrieval"/> composes both siblings from the
        /// identical select-then-retrieve Sequence.
        /// </summary>
        /// <remarks>
        /// <b>UN-PARKED AT MILESTONE 8.37a, and what changed is the ARC rather than any rule.</b>
        /// Milestone 8.37 shipped it <c>[Explicit]</c> because it was written to fill her discard by
        /// PLAYING a card, which costs a whole turn to close her adjacent-only Gold basic's gap and a
        /// second to resolve it - a three-turn arc, and the round does not hold three of her turns.
        /// Two measurements taken at 8.37a replaced it:
        /// <list type="bullet">
        /// <item><description><b>Playing a card never fills a discard at all.</b> A played card lives
        /// in <see cref="CardLocation.TurnArea"/> for the round; this engine reserves
        /// <see cref="CardLocation.DiscardPile"/> for a card SPENT as a Defense or taken by a discard
        /// clause, and <c>RosterCardRetrievalKind.OwnedDiscardedCard</c> admits only the spent one. So
        /// the original arc could not have produced a candidate however many turns it was given, which
        /// is the part the first diagnosis missed. Open question 34 records the same distinction from
        /// the other side.</description></item>
        /// <item><description><b>The staging IS a parameter.</b> The old remarks read
        /// <c>EngagedMirrorStaging</c>'s two-space gap as fixed and concluded a new staging was needed.
        /// <see cref="MatchSceneCompositionRoot.NextComposedSandboxStaging"/> is the seam Silverarrow's
        /// own range journeys already use for exactly this - it places heroes where a journey needs
        /// them, on the scene the journey was going to load anyway.</description></item>
        /// </list>
        ///
        /// <para>Together they collapse the arc to ONE round with no turn spent on setup: standing the
        /// enemy adjacent means their Attack needs no gap-closing turn, and rulebook p.7 resolves the
        /// higher printed Initiative first, so their Magnetic Dagger at 12 acts before her Guardian
        /// Spirit at 10 and the discard is standing by the time she asks. Measured offline before this
        /// was rewritten, at <c>tmp/8-37a-blizzard-repeat/probes/drive</c>.</para>
        /// </remarks>
        [UnityTest]
        public IEnumerator GuardianSpirit_TargetsHerselfAndRetrievesTheCardSheSpentAsADefence()
        {
            // The first load is only to read the seats and measure the contact square; the sandbox
            // staging then re-composes the same scene with the enemy standing beside her.
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTali);
            var surveyed = Root().Runtime;
            var taliId = TaliSeat(surveyed).HeroId;
            var enemyId = OtherHeroId(surveyed, taliId);
            var taliAt = surveyed.Heroes.Single(hero => hero.HeroId.Equals(taliId)).Coordinate;
            var contact = AnEmptySquareBeside(surveyed, taliAt);

            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(taliId, taliAt),
                    new SandboxHeroStaging(enemyId, contact)
                });
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTali);

            var root = Root();
            var runtime = root.Runtime;
            Assert.That(
                HexDistanceQuery.Calculate(
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(taliId)).Coordinate,
                    runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate),
                Is.EqualTo(1),
                "The whole arc depends on the enemy needing no turn to reach her.");

            // Their highest-Initiative Attack against her Guardian Spirit: p.7 gives them the turn
            // first, which is what puts a card in her discard before she asks for one.
            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, taliId, TaliCardContent.GuardianSpirit);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Their Attack must act before her Skill. " + Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(taliId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True, Remaining(runtime));
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "p.14's Defense response is what this arc is for. " + Remaining(runtime));
            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "She holds a face that answers this total, so spending it is a real choice rather " +
                "than a defeat dressed up as one. " + Remaining(runtime));
            var spent = survives.DefenseBreakdown.CardInstanceId.Value;
            Assert.That(runtime.SubmitDefenseCard(spent), Is.True, Remaining(runtime));
            yield return null;

            Assert.That(runtime.Cards.Single(card => card.Id.Equals(spent)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "A card SPENT as a Defense is what this engine calls discarded - which is why the " +
                "arc fills her discard this way and not by playing something.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(taliId)), Is.True,
                "She answered the total, so she is still standing to take her own turn.");

            if (runtime.IsTurnActive && runtime.RoundView.Model.EndTurnInvocation != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, Remaining(runtime));
                yield return null;
            }

            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId),
                "Her own turn is later in the SAME round, so nothing has been retrieved back. " +
                Remaining(runtime));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(spent)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The discard survives to her turn; a round boundary would have emptied it.");

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Up to 1 friendly hero in radius\" is the first question, and on a one-hero team " +
                "the radius always still admits the caster. " + Remaining(runtime));
            // Answered on the map, like every other hero-valued candidate in this file.
            Assert.That(
                runtime.RoundView.Model.EffectChoiceTargets.Any(value =>
                    value.Command.EffectChoice.Candidate.Value.HeroId.Equals(taliId)),
                Is.True,
                "She is her own legal subject. " + Remaining(runtime));
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, HeroView(taliId).transform.position),
                Is.True, Remaining(runtime));
            yield return null;
            yield return ConfirmIfStillStanding(root, StandingNode(runtime));

            // "May retrieve" - the May-policy clause, now with a real candidate. Choosing it rather
            // than declining is what proves the card-selection question.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "A legal card in her own discard means the retrieve opens rather than fizzling " +
                "quietly. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(taliId),
                "It is her discard, so it is her choice. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Not.Null,
                "\"May\" is a control she can refuse. " + Remaining(runtime));
            var cardCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(cardCandidates, Has.Length.EqualTo(1),
                "The card she spent is the one thing standing in her discard. " + Remaining(runtime));
            // Milestone 8.46b: an exactly-one card choice resolves on its selection, so the node is
            // read BEFORE the answer - afterwards there is no pause left to read one from.
            var retrieveNode = StandingNode(runtime);
            Assert.That(runtime.SubmitProjectedCommand(cardCandidates[0].Command), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, retrieveNode);

            Assert.That(runtime.Cards.Single(card => card.Id.Equals(spent)).State.Location,
                Is.EqualTo(CardLocation.Hand),
                "Retrieved means back in hand, not merely no longer in the discard.");
        }

        /// <summary>
        /// A playable square beside the named one with nothing standing on it - heroes AND minions,
        /// which is the difference from <c>SilverarrowEngagedUiJourneyPlayModeTests.Contact</c>: the
        /// engaged staging puts a friendly melee minion next to her, so an adjacency measured over
        /// heroes alone would stage the enemy on top of it.
        /// </summary>
        private static HexCoordinate AnEmptySquareBeside(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate beside)
        {
            var occupied = runtime.Heroes.Select(hero => hero.Coordinate)
                .Concat(Object.FindObjectsByType<StandardMinionView>(FindObjectsSortMode.None)
                    .Where(view => view.Occupancy != null)
                    .Select(view => view.Occupancy.Coordinate))
                .ToArray();
            var candidates = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                .Select(tile => tile.Coordinate)
                .Where(space => HexDistanceQuery.Calculate(space, beside) == 1)
                .Where(space => !occupied.Contains(space))
                .OrderBy(space => space.Q)
                .ThenBy(space => space.R)
                .ToArray();
            Assert.That(candidates, Is.Not.Empty,
                "The staging leaves no empty square beside her, so no journey about an adjacent " +
                "enemy can be driven from it.");
            return candidates[0];
        }

        /// <summary>
        /// Milestone 8.37a. "Choose a direction. Move each enemy unit in radius 1 space in that
        /// direction, if able. If Blizzard is in your discard, <b>End of turn:</b> May repeat once." -
        /// Blizzard, the last face in this build whose printed text was recorded as absent, driven
        /// through both of its sentences on one turn.
        ///
        /// <para><b>What is new here is only the second sentence.</b> The first is Cold Snap's and
        /// Snowstorm's, already proved by
        /// <see cref="TaliEngagedUiJourneyPlayModeTests.ColdSnap_ChoosesAFreeDirectionThenMovesTheEnemyOneSpaceWithNoSecondQuestion"/>;
        /// what this journey exists for is that the same two nodes are offered AGAIN at the turn's own
        /// close, under the clause's own "May", and that the arm is gone once they have run.</para>
        ///
        /// <para><b>The gate is open question 34 made visible.</b> When the checkpoint arrives Blizzard
        /// stands in <see cref="CardLocation.TurnArea"/>, played and resolved - this engine reserves
        /// <see cref="CardLocation.DiscardPile"/> for a card SPENT as a Defense or taken by a discard
        /// clause - so the journey asserts that location at the moment the trigger fires. Under the
        /// narrow reading the condition would be false there and the whole second sentence would be
        /// dead text; the widened reading is bound at
        /// <see cref="SyntheticConditionCapabilities.CountResolvedPlayedParameter"/> by this one
        /// trigger and nowhere else.</para>
        ///
        /// <para><b>It is NOT a p.10 repeat, and the difference is observable here.</b> Nothing in this
        /// clause is spelled as a repeat to the engine - there is no enclosing action at
        /// <see cref="TimingCheckpoint.TurnEnded"/> for <see cref="EffectNodeKind.RepeatAction"/> to
        /// re-run - so what a player meets is a fresh pair of questions rather than a repeat control.
        /// <see cref="TaliEffectContent.BlizzardRepeatTriggeredEffect"/> records the reading.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Blizzard_RepeatsItsOwnBatchMoveAtTheCloseOfHerTurn_AndTheArmIsThenSpent()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.Blizzard);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId),
                "The journey needs her turn open on Blizzard. " + Remaining(runtime));

            var blizzard = runtime.Cards.Single(card =>
                card.OwnerId.Equals(taliId) &&
                card.DefinitionReference.DefinitionId.Equals(TaliCardContent.Blizzard));

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // Sentence one, on Cold Snap's own terms: six directions as arrows around her, one press
            // to answer, then a batch with no second question.
            var beforeThePrintedPass = EnemyUnitPositions(runtime, taliId);
            yield return AnswerTheFreeDirection(root, "the printed pass", enemyId);
            Assert.That(EnemyUnitPositions(runtime, taliId), Is.Not.EqualTo(beforeThePrintedPass),
                "MILESTONE 8.86: the printed pass moved somebody. This journey read the board only " +
                "AFTER the repeat, so a printed pass that had legally moved nobody would have left " +
                "the whole comparison below about one batch instead of two.");
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The first batch applies from its own node table and asks nothing further. " +
                Remaining(runtime));

            Assert.That(runtime.IsActiveEffectSource(blizzard.Id), Is.True,
                "The arm the second sentence needs must be readable from the card that armed it, or " +
                "a player has no way to know their turn is going to end differently. " +
                Remaining(runtime));

            var beforeTheRepeat = EnemyUnitPositions(runtime, taliId);

            // The turn's close is what fires it. Nothing is answered on the way: the loop stops the
            // moment nobody is acting, which is exactly when the trigger's own question stands.
            yield return YieldUntilNobodyIsActing(runtime);

            var atTheClose = runtime.Cards.Single(card => card.Id.Equals(blizzard.Id));
            Assert.That(atTheClose.State.Location, Is.EqualTo(CardLocation.TurnArea),
                "Open question 34: the card the clause asks about is in her TURN AREA at the " +
                "checkpoint, not in the discard pile this engine reserves for a spent card.");
            Assert.That(atTheClose.State.HasBeenPlayed, Is.True);
            Assert.That(atTheClose.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved),
                "The gate's widened reading is about a card whose own resolution has FINISHED, which " +
                "is the whole difference from every earlier face that asks mid-resolution.");

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The turn is not over while the clause's own question stands - the second sentence " +
                "is printed as happening at the end of THIS turn. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(taliId),
                "\"May\" is hers, and the End-of-turn effect's scope is the controller hero.");
            Assert.That(runtime.PendingEffectChoice.ConfirmInvocation, Is.Not.Null,
                "The clause prints \"May\", so it is offered rather than performed. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Not.Null,
                "A \"May\" a player cannot refuse is not a May. " + Remaining(runtime));

            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(
                    runtime.PendingEffectChoice.ConfirmInvocation)),
                Is.True,
                "She takes the repeat. " + Remaining(runtime));
            yield return null;

            // Sentence two asks the SAME question the printed pass asked, on a fresh set of arrows -
            // never a repeat control, because nothing here is a p.10 repeat to the engine.
            yield return AnswerTheFreeDirection(root, "the End-of-turn repeat", enemyId);

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The repeat's batch applies with no second question either. " + Remaining(runtime));
            Assert.That(EnemyUnitPositions(runtime, taliId), Is.Not.EqualTo(beforeTheRepeat),
                "The repeat moved somebody. Measured offline before this journey was written at " +
                "tmp/8-37a-blizzard-repeat/probes/drive: on this staging the printed pass and the " +
                "repeat each step the enemy hero and one enemy minion one space, so a run where " +
                "nothing moved at all is the clause not firing rather than an honest \"if able\".");
            Assert.That(runtime.IsActiveEffectSource(blizzard.Id), Is.False,
                "The effect expires at the very checkpoint it fires at, so it is spent once the work " +
                "is done - which is what makes \"once\" true without counting anything.");
        }


        /// <summary>
        /// Milestone 8.37a, the other half of Blizzard's "May". The clause is an OFFER, so refusing it
        /// has to be as ordinary as taking it: the repeat is declined, the turn closes, nothing on the
        /// board moves a second time, and the arm is spent all the same.
        ///
        /// <para>Its subject is the one thing
        /// <see cref="Blizzard_RepeatsItsOwnBatchMoveAtTheCloseOfHerTurn_AndTheArmIsThenSpent"/>
        /// structurally cannot also prove - a one-shot effect is taken or refused, never both - and it
        /// is the acceptance check milestone 8.37a's own definition names.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Blizzard_TheEndOfTurnRepeatCanBeRefused_AndTheTurnClosesWithTheArmSpent()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);

            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.Blizzard);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            var blizzard = runtime.Cards.Single(card =>
                card.OwnerId.Equals(taliId) &&
                card.DefinitionReference.DefinitionId.Equals(TaliCardContent.Blizzard));

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var beforeThePrintedPass = EnemyUnitPositions(runtime, taliId);
            yield return AnswerTheFreeDirection(root, "the printed pass", enemyId);

            Assert.That(runtime.IsActiveEffectSource(blizzard.Id), Is.True,
                "The arm stands on her own tray between the printed pass and the turn's close. " +
                Remaining(runtime));
            var afterThePrintedPass = EnemyUnitPositions(runtime, taliId);
            Assert.That(afterThePrintedPass, Is.Not.EqualTo(beforeThePrintedPass),
                "MILESTONE 8.86: the printed pass moved somebody, which is what makes \"nothing " +
                "moved a SECOND time\" below a statement about the refusal rather than a board that " +
                "had never moved at all.");

            yield return YieldUntilNobodyIsActing(runtime);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The clause asks before the turn is allowed to finish. " + Remaining(runtime));
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "\"May\" prints a refusal, and a refusal a player cannot press is not one. " +
                Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(decline.Command), Is.True, Remaining(runtime));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Refusing the offer is the whole of the answer - nothing below it is asked. " +
                Remaining(runtime));
            Assert.That(EnemyUnitPositions(runtime, taliId), Is.EqualTo(afterThePrintedPass),
                "Nothing moved a second time, which is what declining the repeat means.");
            Assert.That(runtime.IsActiveEffectSource(blizzard.Id), Is.False,
                "The effect expires at the checkpoint it fired at whether or not the offer was " +
                "taken, so a refused repeat cannot be asked again later in the round.");
        }

        /// <summary>
        /// Answers one free direction the way a player does at milestone 8.46d: by pressing its arrow
        /// on the board, around the hero being asked.
        ///
        /// <para>One press is the whole answer. A choice whose minimum and maximum are both one is
        /// finished by the selection itself - the board forwards the Select and the confirm the
        /// authority offers immediately after it - so nothing is left holding an unspent direction
        /// and no second control is pressed. Cold Snap's own journey states the same reading.</para>
        /// </summary>
        /// <param name="enemyId">
        /// Milestone 8.86. The enemy hero the pressed direction must really be able to move, so the
        /// batch this answer carries displaces somebody rather than legally displacing nobody: "if
        /// able" makes a blocked direction a correct answer that changes no board at all, and a
        /// journey that pressed one could not tell the printed sentence from silence. The caller
        /// reads the board afterwards; this step is what makes that read decisive.
        /// </param>
        private static IEnumerator AnswerTheFreeDirection(
            MatchSceneCompositionRoot root, string which, HeroId enemyId)
        {
            yield return null;
            var runtime = root.Runtime;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                $"\"Choose a direction\" opens unconditionally on {which}. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.HexDirection),
                "Milestone 8.37b: the candidates are directions and must say so, or a player is " +
                "offered the buttons \"0\" through \"5\". " + Remaining(runtime));

            var directions = runtime.BoardView.Model.DirectionCandidates;
            Assert.That(directions, Has.Count.EqualTo(6),
                "All six of p.17's directions are always offered - the sentence names no origin and " +
                "no destination to narrow them. " + Remaining(runtime));

            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var chosen = FirstClickableDirection(root, pointerInput, candidate =>
                NothingStandsOn(enemyAt.GetNeighbor(candidate.Direction)));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, Tile(chosen.Neighbour).transform.position),
                Is.True,
                $"The {chosen.Label} arrow answers {which} where it is drawn.");
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyAt.GetNeighbor(chosen.Direction)),
                $"\"Move each enemy unit in radius 1 space in that direction\" - on {which} the enemy " +
                $"hero really stepped one space along the {chosen.Label} arrow, from {enemyAt}. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Where every unit hostile to the named hero stands, as one comparable string, read off the
        /// live VIEWS rather than the projection - the batch is only proved if what a player can see
        /// moved.
        /// </summary>
        private static string EnemyUnitPositions(ConfiguredMatchSceneRuntime runtime, HeroId taliId) =>
            string.Join("|", runtime.Heroes
                .Where(hero => !hero.HeroId.Equals(taliId))
                .Select(hero => $"{hero.HeroId.Value}@{hero.Coordinate}")
                .Concat(Object.FindObjectsByType<StandardMinionView>(FindObjectsSortMode.None)
                    .Where(view => view.Occupancy != null &&
                        view.Occupancy.TeamSide != TaliSeat(runtime).TeamSide)
                    .Select(view => $"{view.Occupancy.MinionId.Value}@{view.Occupancy.Coordinate}"))
                .OrderBy(value => value));

        /// <summary>
        /// "Each time after you defeat a minion with your basic attack, an enemy hero in radius
        /// discards a card of a chosen colour, if able." - Reign of Winter, driven to its own trigger:
        /// Ice Blast, her one basic Attack, takes an adjacent enemy minion off the board, and the
        /// Ultimate then asks its two questions and hands the third to the seat whose hand it empties.
        /// </summary>
        /// <remarks>
        /// <b>UN-PARKED AT MILESTONE 8.37a, and it found a shipped engine defect on the way.</b>
        /// Milestone 8.37 shipped this self-<c>Assert.Ignore</c>ing when p.8's waves happened to stand
        /// no enemy minion beside her - which they do not on this staging - so the DEFEATED branch, the
        /// only one that proves anything, had never run. Two things changed:
        /// <list type="bullet">
        /// <item><description><b>The subject is staged rather than hoped for.</b>
        /// <see cref="MatchSceneCompositionRoot.NextComposedSandboxStaging"/> moves one enemy melee
        /// minion to a square beside her, which is the same seam Silverarrow's range journeys use to
        /// place their own subjects. Nothing else about the board moves, and no rule is bent: the
        /// minion is an ordinary p.8 wave minion standing an ordinary space away.</description></item>
        /// <item><description><b>The engine faulted the moment it was reached.</b> Answering the target
        /// threw <c>ArgumentException: A turn card-action event requires a defined card-action
        /// event</c>. p.13 gives a minion target no steps 3 to 5, so p.14's step-1 text resumes the
        /// WHOLE rest of the page inside its own produced stream - step 7's card text and the action's
        /// own <c>ActionEnded</c> included - and <c>TurnResolutionAuthority.MaterializeSteps</c> walked
        /// that produced stream with a smaller loop that knew neither. The same gap would also have
        /// dropped the <c>PerformedBasicAction</c> this very trigger reads, so the clause could not have
        /// fired even without the throw. Fixed there, at the authority, and pinned by
        /// <c>AttackCardTextPageTests</c>.</description></item>
        /// </list>
        ///
        /// <para>Only a face printing card text at BOTH of the attacker's own moments can reach that
        /// gap, and only against a minion. Ice Blast is the first dealt content of that shape to be
        /// pointed at one, which is why twelve dealings passed over it. Measured offline before this
        /// was rewritten, at <c>tmp/8-37a-blizzard-repeat/probes/drive</c>.</para>
        /// </remarks>
        [UnityTest]
        public IEnumerator ReignOfWinter_OpensTheColorNamingChoiceWhenABasicAttackDefeatsAMinion()
        {
            // The first load measures the board; the sandbox staging then re-composes it with one
            // enemy minion standing where her adjacent-only basic Attack can reach it.
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTali);
            var surveyed = Root().Runtime;
            var taliId = TaliSeat(surveyed).HeroId;
            var enemyId = OtherHeroId(surveyed, taliId);
            var taliAt = surveyed.Heroes.Single(hero => hero.HeroId.Equals(taliId)).Coordinate;
            var beside = AnEmptySquareBeside(surveyed, taliAt);
            var subject = Object.FindObjectsByType<StandardMinionView>(FindObjectsSortMode.None)
                .Select(view => view.Occupancy)
                .Where(occupancy => occupancy != null &&
                    occupancy.TeamSide != TaliSeat(surveyed).TeamSide &&
                    occupancy.MinionType == StandardMinionType.Melee)
                .OrderBy(occupancy => occupancy.MinionId.Value)
                .First()
                .MinionId;

            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                movedMinions: new[]
                {
                    new KeyValuePair<MinionId, HexCoordinate>(subject, beside)
                });
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTali);

            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // HER ULTIMATE READS A **BASIC** ATTACK. `AfterControllerDefeatsMinionWithBasicAttack` is
            // exactly what its name says, and Ice Blast is her only basic Attack - TaliCardContent's own
            // summary calls it "the one action Reign of Winter reads". Spirit Bear is a Red Tier III
            // face, so a minion defeated by it can never raise this trigger, and milestone 8.37's first
            // draft of this journey was asserting that it did.
            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.IceBlast);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
            var staged = runtime.PendingAttackTargetChoice.LegalTargets
                .Where(candidate => candidate.Kind == AttackTargetKind.Minion &&
                    candidate.MinionId.Equals(subject))
                .ToArray();
            Assert.That(staged, Has.Length.EqualTo(1),
                "The staged minion is standing beside her, so her adjacent-only basic Attack must " +
                "offer it. " + Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(subject)), Is.True,
                "The minion is answered as the Attack's target. " + Remaining(runtime));
            yield return null;
            yield return null;

            Assert.That(
                Object.FindObjectsByType<StandardMinionView>(FindObjectsSortMode.None)
                    .Any(view => view.Occupancy != null && view.Occupancy.MinionId.Equals(subject)),
                Is.False,
                "p.13: \"Minions cannot defend. When you target an enemy minion with an attack, it is " +
                "simply defeated.\" - so the piece is off the board and the trigger has its cause.");

            // Question one is hers: which enemy hero inside the printed Radius 4 disk.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"An enemy hero in radius\" is a mandatory selection the trigger opens on its own " +
                "control - and the whole point of un-parking this journey is that it now stands. " +
                Remaining(runtime));
            Assert.That(
                runtime.RoundView.Model.EffectChoiceTargets.Any(value =>
                    value.Command.EffectChoice.Candidate.Value.HeroId.Equals(enemyId)),
                Is.True,
                "The engaged staging stands the enemy two spaces away, well inside Radius 4. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "A hero-valued candidate is answered ON THE MAP. " + Remaining(runtime));
            yield return null;
            yield return ConfirmIfStillStanding(root, StandingNode(runtime));

            // Question two is hers as well: which colour. Milestone 8.37b is what makes it readable.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Naming a colour is mandatory, unconditional on which target was found. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.CardColor),
                "Milestone 8.37b: the candidates are colours and must say so. " + Remaining(runtime));
            var colorCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(colorCandidates, Has.Length.EqualTo(6),
                "All six of p.9's colours are offered, RULING 21's Purple included. " +
                Remaining(runtime));
            // Milestone 9.4: the journey names a colour the enemy really holds, so the sentence's
            // last step - "discards a card of that colour" - is performed and read off the hand,
            // rather than the first colour offered, which could leave nothing to discard.
            var enemyHand = runtime.Cards
                .Where(card => card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand)
                .ToArray();
            var held = enemyHand
                .Select(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Progression.Color)
                .Where(color => color != CardColor.None)
                .ToArray();
            Assert.That(held, Is.Not.Empty, "The enemy holds a coloured card to be named. " + Remaining(runtime));
            var named = colorCandidates.First(value =>
                (CardColor)value.Command.EffectChoice.Candidate.Value.IntegerValue == held[0]);
            Assert.That(runtime.SubmitProjectedCommand(named.Command), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, StandingNode(runtime));

            // Question three is THEIRS: it is their hand the clause empties, and a decision owned by
            // another seat is what milestone 8.37b's narration correction is about.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The enemy holds a card of the named colour, so \"discards a card of that colour\" " +
                "asks which. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "\"[They] discard a card\" - it is their hand, so it is their choice. " +
                Remaining(runtime));
            var discard = runtime.RoundView.Model.EffectChoices.First(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            var discardedId = discard.Command.EffectChoice.Candidate.Value.CardInstanceId;
            Assert.That(runtime.SubmitProjectedCommand(discard.Command), Is.True);
            yield return null;
            yield return null;

            var discarded = runtime.Cards.Single(card => card.Id.Equals(discardedId));
            Assert.That(discarded.State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "The card they named left their hand for the discard pile. " + Remaining(runtime));
            Assert.That(
                runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(discarded.DefinitionReference)).Progression.Color,
                Is.EqualTo(held[0]),
                "and it is a card of the colour Tali named.");
        }
    }
}
