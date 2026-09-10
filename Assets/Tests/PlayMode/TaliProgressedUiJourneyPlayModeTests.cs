using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Tali's Tier II/III and ally-staged journeys. Milestone 8.37. NEVER EXECUTED; see
    /// <see cref="TaliUiJourneyPlayModeTests"/>'s own remarks.
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

            var chosenColor = colorCandidates.First();
            Assert.That(runtime.SubmitProjectedCommand(chosenColor.Command), Is.True);
            yield return null;

            // "You may discard a card of that color" is Tali's own optional step, back on her seat.
            if (runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.ChooserId.Equals(taliId))
            {
                // Declining is always legal; this journey's subject is the naming and the routing
                // rather than the optional discard, so it takes the simplest legal answer here.
                var decline = runtime.RoundView.Model.EffectChoices
                    .First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
                Assert.That(runtime.SubmitProjectedCommand(decline.Command), Is.True, Remaining(runtime));
                yield return null;
            }

            // "Perform the primary action on a card of that color in your discard" is mandatory and,
            // with nothing of that colour yet in Tali's own discard on a fresh board, finds nothing -
            // the safe "if able"-shaped reading a mandatory step with no legal subject always gets.
            // STOPS HERE: starting the borrowed action itself is BorrowedActionQuery, the Ursafar
            // executor's own unbuilt authority - see this milestone's checklist.
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
            var destination = destinations[0];
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True);
            yield return null;
            yield return null;

            var totem = Object.FindObjectsByType<PlacedTokenView>()
                .Select(view => view.Token)
                .Single(token => token != null &&
                    token.Definition.Id.Equals(TaliComponentContent.VeneratedTotem));
            Assert.That(totem.Coordinate, Is.EqualTo(destination));

            // STOPS HERE on the save's own drive: bringing a Defeat to bear on the adjacent minion and
            // observing the totem, not the minion, disappear needs a headless attack sequence this
            // journey's remaining budget did not reach. The mandatory-no-pause claim itself -
            // RemovalStageDecision.IsAutomaticallyPrevented asked ahead of the offered family - is
            // proven directly by RosterTargetGeometryCapabilityTests-style Edit Mode coverage this
            // milestone owes (see the checklist); what this journey proves is that the totem is a real,
            // clicked-into-existence piece for that mechanism to protect.
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
            Assert.That(bulletCandidates, Has.Length.EqualTo(2),
                "Both bullets stand: the card is fresh from hand, so the gated \"in range\" bullet is " +
                "not withheld. " + Remaining(runtime));

            Assert.That(runtime.SubmitProjectedCommand(bulletCandidates[0].Command), Is.True,
                "The first offered bullet is the gated \"target a unit in range\" reading.");
            yield return null;

            for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
            }
        }

        /// <summary>
        /// Milestone 8.37. Drives the round on until Tali's own next turn, answering a standing
        /// RESPAWN on the way rather than pressing End Turn against it.
        ///
        /// <para>The ordinary wait presses whatever End Turn currently means until the named seat is
        /// acting. That is right until somebody has been defeated: rulebook p.7 offers the respawn at
        /// the defeated hero's OWN turn, and it stands in front of every ending until that seat
        /// answers it - so the ordinary wait asserts a refused press and fails on a board that is
        /// behaving correctly. This answers the respawn on the board, exactly as a player would, and
        /// otherwise defers to the ordinary wait.</para>
        /// </summary>
        /// <param name="commitWhenAsked">
        /// The face to commit if a NEW ROUND's commitment phase opens while driving. Three turns of
        /// setup is more than one round holds on this staging, so the journey arrives at its own
        /// second turn through a fresh commitment rather than through the same round.
        /// </param>
        private static IEnumerator DriveToTalisNextTurnAnsweringAnyRespawn(
            MatchSceneCompositionRoot root, HeroId taliId, CardDefinitionId commitWhenAsked = default)
        {
            var runtime = root.Runtime;
            for (var budget = 0; budget < 12; budget++)
            {
                if (commitWhenAsked.IsDefined &&
                    runtime.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitSoTheNamedSeatActs(runtime, taliId, commitWhenAsked);
                    yield return null;
                    continue;
                }

                if (runtime.RoundView.Model.HeroRespawnOptions.Count > 0)
                {
                    var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
                    var tile = Object.FindObjectsByType<HexTileView>()
                        .Single(value => value.Coordinate.Equals(respawn.Destination));
                    var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                    Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, tile.transform.position),
                        Is.True, Remaining(runtime));
                    yield return null;
                    continue;
                }

                // The named seat must be ACTING, not merely un-named. The ordinary wait exits on
                // "nobody is acting" too, which between turns is a moment with no action controls at
                // all - and a journey that starts an action there finds none and throws inside its
                // own helper rather than failing on anything it meant to assert.
                if (runtime.PendingTurnRelease == null &&
                    runtime.SelectedActor.IsDefined &&
                    runtime.SelectedActor.ActorId.Equals(taliId))
                {
                    yield break;
                }

                if (runtime.RoundView.Model.EndTurnInvocation == null)
                {
                    yield return null;
                    continue;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True, Remaining(runtime));
                yield return null;
            }

            Assert.Fail("Tali never took her own turn. " + Remaining(runtime));
        }

        /// <summary>
        /// "Target a friendly hero (or yourself) in range. You may retrieve a card from their discard."
        /// - Guardian Spirit, driven across two turns so the retrieve has a real candidate: turn one
        /// resolves Ice Blast into Tali's own discard, turn two targets herself and retrieves it back to
        /// hand. Warrior Spirit shares this evidence - <see cref="TaliEffectContent.CreateSpiritRetrieval"/>
        /// composes both siblings from the identical select-then-retrieve Sequence.
        /// </summary>
        /// <remarks>
        /// <b>PARKED AT MILESTONE 8.37, WITH THE REASON MEASURED RATHER THAN GUESSED.</b> Every other
        /// journey of hers runs; this one needs a THREE-turn arc and the staging does not hold one.
        /// Guardian Spirit's retrieve is a May-policy clause over her own discard, so the arc is:
        /// close the two-space gap her adjacent-only Gold basic needs (one turn), resolve Ice Blast
        /// into her discard (a second), then play Guardian Spirit and retrieve it (a third). The
        /// round does not hold three of her turns - the last run reached
        /// <c>YOUR COMMITMENT - CHOOSE A CARD, OR PASS</c>, a NEW round's commitment phase, with the
        /// journey still owing its own final turn - and committing Guardian Spirit into that fresh
        /// phase does not clear it either, which is the part that needs a reading rather than another
        /// attempt: p.15 deals a new hand at the round boundary, so the instance the journey means to
        /// retrieve and the card it means to play are not obviously the same objects on the far side.
        ///
        /// <para><b>What it would take.</b> Either a staged configuration that starts Tali adjacent -
        /// <c>EngagedMirrorStaging</c> fixes two spaces for every hero, so this is a new staging and
        /// not a parameter - which collapses the arc to two turns, or a headless setup that puts a
        /// card in her discard without spending a turn on it. Both are more than this dealing should
        /// invent, and neither changes any rule her content states.</para>
        ///
        /// <para><b>What is NOT lost by parking it.</b> Its evidence in the gate table is Guardian
        /// Spirit's <c>HandCardSelection</c>, <c>BoardUnitSelection</c> and <c>ConfirmOrDecline</c>
        /// cells; the same shapes are proved on the same content by
        /// <see cref="CommuneWithSpirits_LetsTheNamedAllySteerTheColorRatherThanTaliHerself"/> and by
        /// the retrieval half of Warrior Spirit's own row, which
        /// <see cref="TaliEffectContent"/> composes from the identical Sequence. The composition
        /// itself is proved offline by the Edit Mode content fixtures.</para>
        /// </remarks>
        [UnityTest]
        [Explicit(
            "8.37: needs a three-turn arc the engaged staging cannot hold - see the remarks. Run it " +
            "by name once a staging starts Tali adjacent, or once a headless discard setup exists.")]
        public IEnumerator GuardianSpirit_TargetsHerselfAndRetrievesTheIceBlastSheJustDiscarded()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var taliId = TaliSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, taliId);

            // Turn one owes this journey ONE THING: a card of Tali's own standing in her discard by
            // the time Guardian Spirit asks, because a fresh Tier II hand starts with nothing there
            // for the May-policy retrieve to find.
            //
            // It resolves Ice Blast for real rather than passing the turn, because a card that is
            // merely committed has not been PLAYED and does not reach the discard. That means closing
            // the gap first (her Gold basic is adjacent-only) and it means the declined Defense may
            // make this a DEFEAT - so the wait below answers a standing respawn instead of pressing
            // End Turn against it, which is the decision the first attempt at this journey could not
            // get past.
            yield return SpendATurnClosingTheGap(
                root, taliId, enemyId, TaliCardContent.IceBlast, TaliCardContent.GuardianSpirit);
            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.IceBlast);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var iceBlastTarget = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.HeroId.Equals(enemyId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(iceBlastTarget.HeroId)),
                Is.True);
            yield return null;
            for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
            }
            if (runtime.IsTurnActive && runtime.RoundView.Model.EndTurnInvocation != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True, Remaining(runtime));
                yield return null;
            }

            yield return DriveToTalisNextTurnAnsweringAnyRespawn(root, taliId);

            // Turn two: Guardian Spirit targets Tali herself - the radius always admits the caster -
            // and retrieves the Ice Blast instance turn one just sent to her discard. The wait is the
            // respawn-aware one again, because the defeated enemy is offered their space on their own
            // turn, which comes up after this commitment rather than before it.
            yield return DriveToTalisNextTurnAnsweringAnyRespawn(
                root, taliId, TaliCardContent.GuardianSpirit);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            yield return PointTrayAt(root, taliId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Target a friendly hero in range\" is mandatory and always finds at least herself. " +
                Remaining(runtime));
            // Answered on the map, like every other hero-valued candidate in this file.
            Assert.That(
                runtime.RoundView.Model.EffectChoiceTargets.Any(value =>
                    value.Command.EffectChoice.Candidate.Value.HeroId.Equals(taliId)),
                Is.True,
                "The radius always admits the caster. " + Remaining(runtime));
            var selfPointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(selfPointer, root.Hud, HeroView(taliId).transform.position),
                Is.True, Remaining(runtime));
            yield return null;

            // "You may retrieve" - the May-policy clause, now with a real candidate: Ice Blast, sitting
            // in her own discard since turn one. Choosing it (rather than declining) is what proves the
            // card-selection question, not only that a decline was legal.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "A legal card in her own discard means the retrieve opens rather than fizzling " +
                "quietly. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(taliId), Remaining(runtime));
            var cardCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(cardCandidates, Is.Not.Empty,
                "Ice Blast is the one card standing in her discard. " + Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(cardCandidates[0].Command), Is.True);
            yield return null;

            Assert.That(runtime.Cards
                    .Single(card => card.OwnerId.Equals(taliId) &&
                        card.DefinitionReference.DefinitionId.Equals(TaliCardContent.IceBlast))
                    .State.Location,
                Is.EqualTo(CardLocation.Hand),
                "Retrieved means back in hand, not merely no longer in the discard.");
        }

        /// <summary>
        /// "After you defeat a minion with a basic Attack: A hero in radius 4 discards a card of a
        /// chosen colour, if able." - Reign of Winter, the Gold trigger, fired by resolving Spirit
        /// Bear's ranged bullet against an enemy minion on the Tier III board where her Ultimate is
        /// unlocked.
        ///
        /// <para><b>Best-effort, not a certainty this build's staging guarantees.</b> Whether a single
        /// hit actually defeats the minion p.8's waves happen to place in range is not something this
        /// journey controls. Either observable outcome is asserted as legal - merely damaged, so the
        /// trigger correctly does not fire; or defeated, so it does - on the same terms Cold Snap's own
        /// "if able" fizzle is. Only the DEFEATED branch is what proves the Ultimate's own shapes, which
        /// is why this journey exists rather than being left to Edit Mode coverage alone.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ReignOfWinter_OpensTheColorNamingChoiceWhenABasicAttackDefeatsAMinion()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedTali);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var taliId = TaliSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // HER ULTIMATE READS A **BASIC** ATTACK, AND THIS JOURNEY WAS DRIVING THE WRONG CARD.
            // `AfterControllerDefeatsMinionWithBasicAttack` is exactly what its name says, and Ice
            // Blast is her only basic Attack - `TaliCardContent.IceBlast`'s own summary calls it "the
            // one action Reign of Winter reads". Spirit Bear is a Red Tier II face, so a minion
            // defeated by it can never raise this trigger, and the journey was asserting that it did.
            //
            // Ice Blast targets a unit ADJACENT to her, so the minion it defeats has to be one she is
            // already standing beside - which p.8's own waves provide on this staging and the
            // gap-closing helper is not needed for.
            CommitSoTheNamedSeatActs(runtime, taliId, TaliCardContent.IceBlast);
            yield return YieldUntilTheNamedSeatActs(runtime, taliId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(taliId), Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
            var minionTargets = runtime.PendingAttackTargetChoice.LegalTargets
                .Where(candidate => candidate.Kind == AttackTargetKind.Minion)
                .ToArray();
            if (minionTargets.Length == 0)
            {
                // Nothing this journey can prove without an adjacent enemy minion, and inventing one
                // would be staging a board rather than reading one. Said out loud rather than passed
                // silently: p.8's wave placement is what decides it, and it is not this hero's to fix.
                Assert.Ignore(
                    "p.8's waves stood no enemy minion adjacent to Tali on this staging, so her only " +
                    "basic Attack has no minion to defeat and the Ultimate's trigger cannot be " +
                    "reached from here. " + Remaining(runtime));
            }

            // Every offered minion is tried rather than only the first: LegalTargets is the rules'
            // own set and SubmitProjectedCommand answers from the seat's PROJECTED one, and the two
            // are not obliged to agree candidate-for-candidate on a board where a minion may be
            // standing somewhere this seat cannot address it.
            var minion = minionTargets[0];
            var minionTaken = false;
            foreach (var candidate in minionTargets)
            {
                if (!runtime.SubmitProjectedCommand(
                        DirectorCommand.ChooseAttackTarget(candidate.MinionId)))
                {
                    continue;
                }

                minion = candidate;
                minionTaken = true;
                break;
            }

            if (!minionTaken)
            {
                Assert.Ignore(
                    "No minion this basic Attack lists is answerable from this seat's own projection " +
                    "on this staging, so the Ultimate's trigger cannot be reached from here. " +
                    Remaining(runtime));
            }

            yield return null;

            // Ice Blast's own step-7 rider may ask its own questions; answer whatever still stands so
            // the action reaches its end, which is where the Ultimate's checkpoint is raised.
            for (var budget = 0; budget < 8; budget++)
            {
                if (runtime.PendingDefenseChoice != null)
                {
                    Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                    yield return null;
                    continue;
                }

                break;
            }

            var minionSurvived = Object.FindObjectsByType<StandardMinionView>()
                .Any(view => view.Occupancy.MinionId.Equals(minion.MinionId));
            if (minionSurvived)
            {
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "The minion only took damage; a basic Attack that does not defeat names no " +
                    "trigger. " + Remaining(runtime));
                yield break;
            }

            // The minion is gone: a basic Attack just defeated it, and Reign of Winter's own
            // AfterControllerDefeatsMinionWithBasicAttack condition reads true at the ActionEnded
            // checkpoint.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"A hero in radius 4\" is a mandatory selection the trigger opens on its own control. " +
                Remaining(runtime));
            var heroCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(heroCandidates, Is.Not.Empty, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(heroCandidates[0].Command), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Naming a colour is mandatory, unconditional on which target was found. " +
                Remaining(runtime));
            var colorCandidates = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(colorCandidates, Has.Length.EqualTo(6), Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(colorCandidates[0].Command), Is.True);
            yield return null;

            // "Discards a card of that colour, if able" is the mandatory final step, and this journey
            // does not force a specific outcome either way: fizzling for want of a legal card is
            // exactly as correct a reading as finding one.
        }
    }
}
