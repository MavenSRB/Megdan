using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.40j. The bounty marker on the hero rail, proved on a loaded scene rather than by
    /// reading the view model - which is the one thing this dealing owes the presentation and the one
    /// thing no offline tier can answer.
    ///
    /// <para><b>His marker is the SECOND this build renders and the first with no value channel at
    /// all.</b> Tigerclaw's Poison declares three <c>MarkerPropertyDefinition</c>s that each name a
    /// printed value channel, so its rail row draws a caption and three signed chips -
    /// <c>POISON INI -1 ATK -1 DEF -1</c>. Bain's bounty declares ONE property, rulebook p.13's
    /// additional Life cost, and that property names <c>PrintedValueChannel.None</c>: there is no
    /// number to print. So the rail must draw the caption alone. A row reading <c>BOUNTY - 0</c>
    /// would be this surface inventing a rule the component does not state, which is exactly what
    /// <c>HeroMarkerViewModel</c>'s own remarks say it must not do - and a row that threw, or drew
    /// nothing at all, would leave the only fact three of his five Green faces read invisible to the
    /// player who has to decide from it.</para>
    ///
    /// <para><b>The marker is given by playing the card that gives it.</b> Dead or Alive is his Gold
    /// basic and its "After the attack:" clause is p.10's <c>may</c>, so the sequence is a real
    /// commitment, a real basic Attack on an adjacent unit and a real answer to the optional give.
    /// The decisions are submitted through the runtime's own projected-command surface, which is the
    /// surface every control in the HUD builds its buttons from; the pointer-driven journeys over the
    /// same clause belong to milestone 8.40k's player-path gate and are not duplicated here.</para>
    ///
    /// <para><b>BA-17's corpus face is NOT prepared, and that is recorded rather than left to be
    /// noticed.</b> The extraction holds a real <c>BountyMarker</c> element (1421888, a "WANTED"
    /// poster) beside Tigerclaw's <c>PoisonMarker</c>, and this build draws a held marker as a rail
    /// row rather than as a board sprite - a caption and its chips, with no image anywhere in the
    /// row. So there is nothing for that face to be prepared INTO, and preparing it would add an
    /// unused resource. Tigerclaw's own marker is drawn the same way and his face is unprepared too.
    /// </para>
    /// </summary>
    public sealed class BainMarkerRailPlayModeTests
    {
        /// <summary>
        /// The reading his bounty must leave: the marker's own display name, in the rail's caption
        /// case, and nothing else. Written as a literal on <see cref="HeroRailMarkerCheck"/>'s own
        /// terms - a check that composed its expectation from the same content the screen was built
        /// from would agree with the screen however wrong both were.
        /// </summary>
        private const string BountyReading = "BOUNTY";

        [UnityTest]
        public IEnumerator TheHeldBounty_DrawsItsNameOnTheHoldersRailWithNoEmptyValueChip()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = runtime.Setup.Heroes.Single(hero => !hero.HeroId.Equals(bainId)).HeroId;

            // Nobody holds anything before the card is played, on either rail. Without this the
            // journey could not tell a marker it caused from one the staging had already dealt.
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the staged position");
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the staged position");

            CommitSoTheNamedSeatActs(runtime, bainId, BainCardContent.DeadOrAlive);
            yield return YieldUntilTheNamedSeatActs(runtime, bainId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(bainId),
                "His card has to open the turn for this journey to drive it. " + Detail(runtime));

            Assert.That(runtime.SubmitAction(BainCardContent.PrimaryBasicAttack), Is.True,
                "Dead or Alive's printed primary is a basic Attack. " + Detail(runtime));
            yield return null;

            // The whole of p.14 plus the "After the attack:" clause behind it, answered one standing
            // decision at a time. The budget is generous and the failure names what was still
            // standing, because a journey that silently ran out would look like a marker that was
            // never drawn.
            var trace = new System.Collections.Generic.List<string>();
            for (var step = 0; step < 24; step++)
            {
                if (runtime.Cards != null &&
                    HeroRailMarkerCheck.MarkerReadingsOn(hud, TeamSide.Red).Count > 0)
                {
                    trace.Add("marker drawn");
                    break;
                }

                if (!AnswerOneStandingDecision(runtime, enemyId, trace))
                {
                    trace.Add("nothing standing " + Detail(runtime));
                    break;
                }

                yield return null;
            }

            // THE WHOLE OF THIS CHECK, IN ONE ASSERTION: the hero the marker was given to reads its
            // name on the rail that is theirs, and the row carries no chip at all.
            Assert.That(runtime.Projection.HeldMarkers.Select(marker =>
                    marker.Definition.DisplayName + "->" + marker.HolderHeroId.Value),
                Is.EqualTo(new[] { "Bounty->" + enemyId.Value }),
                "The give has to have happened before the rail can draw it. Drive trace: " +
                string.Join(" | ", trace));
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Red, "the answered Dead or Alive", BountyReading);
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "the answered Dead or Alive");

            // And a redrawn HUD reconstructs the row from the projection alone, with no second row
            // left behind by the one it replaced.
            runtime.RefreshAll();
            yield return null;
            HeroRailMarkerCheck.AssertReadings(
                hud, TeamSide.Red, "a rebuilt presentation", BountyReading);
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Blue, "a rebuilt presentation");

            Assert.That(enemyId, Is.Not.EqualTo(bainId),
                "and the two seats this journey reads are two heroes, which is what makes the " +
                "assertion above about the HOLDER's rail rather than about any rail at all.");
        }

        /// <summary>
        /// Answers whatever one decision is standing, preferring the answers Dead or Alive's own
        /// sentence asks for: the adjacent unit it targets, no defence, the optional give TAKEN, and
        /// the enemy hero in radius it gives to. Returns false when nothing is standing.
        /// </summary>
        private static bool AnswerOneStandingDecision(
            ConfiguredMatchSceneRuntime runtime,
            HeroId enemyId,
            System.Collections.Generic.List<string> trace)
        {
            var attackTarget = runtime.PendingAttackTargetChoice;
            if (attackTarget != null)
            {
                var command = runtime.EnumerateProjectedCommands()
                    .FirstOrDefault(value => value.Kind == DirectorCommandKind.ChooseAttackTarget);
                Assert.That(command, Is.Not.Null,
                    "\"Target a unit adjacent to you\" must offer one. " + Detail(runtime));
                Assert.That(runtime.SubmitProjectedCommand(command), Is.True, Detail(runtime));
                trace.Add("attack target");
                return true;
            }

            if (runtime.PendingDefenseChoice != null)
            {
                Assert.That(runtime.DeclineDefense(), Is.True,
                    "The defender declines, which is not this check's subject. " + Detail(runtime));
                trace.Add("decline defense");
                return true;
            }

            var choice = runtime.PendingEffectChoice;
            if (choice != null)
            {
                // "You may give an enemy hero in radius a bounty marker" - the confirm-or-decline is
                // TAKEN, because declining it is the one answer that would make this check pass
                // vacuously with no marker on any rail.
                if (choice.Kind == EffectChoiceKind.ConfirmOrDecline)
                {
                    Assert.That(runtime.ConfirmEffectChoice(), Is.True, Detail(runtime));
                    trace.Add("confirmed " + choice.NodeId.Value);
                    return true;
                }

                var offered = runtime.EnumerateProjectedCommands()
                    .Where(value => value.Kind == DirectorCommandKind.AnswerEffectChoice)
                    .ToArray();
                var hero = offered.FirstOrDefault(value =>
                    value.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    value.EffectChoice.Candidate.Value.Kind == EffectValueKind.Hero &&
                    value.EffectChoice.Candidate.Value.HeroId.Equals(enemyId));
                if (hero != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(hero), Is.True, Detail(runtime));
                    trace.Add("selected " + enemyId.Value + " at " + choice.NodeId.Value);
                    return true;
                }

                var select = offered.FirstOrDefault(value =>
                    value.EffectChoice.Kind == EffectChoiceCommandKind.Select);
                if (select != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(select), Is.True, Detail(runtime));
                    trace.Add("selected first at " + choice.NodeId.Value);
                    return true;
                }

                var confirm = offered.FirstOrDefault(value =>
                    value.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
                if (confirm != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(confirm), Is.True, Detail(runtime));
                    trace.Add("confirmed " + choice.NodeId.Value);
                    return true;
                }

                trace.Add("unanswerable pause " + choice.NodeId.Value + " kind " + choice.Kind +
                    " offers " + offered.Length);
            }

            return false;
        }

        private static QuickGameHeroSetupState BainSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == BainCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Bain to exactly one seat; it dealt him to " +
                seats.Length + ". A staged arm missing from SelectSetupConfiguration deals his cards " +
                "to a seat whose content identity is somebody else's, which is precisely what this " +
                "assertion catches.");
            return seats[0];
        }

        /// <summary>
        /// Commits the named card out of the named seat's hand and the lowest-Initiative card out of
        /// every other seat's, headlessly - rulebook p.7 resolves the higher printed Initiative
        /// first, so the other seats yielding is what makes the turn deterministic.
        /// </summary>
        private static void CommitSoTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId actingHeroId, CardDefinitionId definitionId)
        {
            UiPlayer.EndAnyTurnInProgress(runtime);
            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray())
            {
                var card = seat.Equals(actingHeroId)
                    ? runtime.Cards.Single(value =>
                        value.OwnerId.Equals(seat) &&
                        value.State.Location == CardLocation.Hand &&
                        value.DefinitionReference.DefinitionId.Equals(definitionId))
                    : runtime.Cards
                        .Where(value => value.OwnerId.Equals(seat) &&
                            value.State.Location == CardLocation.Hand)
                        .OrderBy(value => runtime.Definitions
                            .Single(definition =>
                                definition.Reference.Equals(value.DefinitionReference))
                            .Initiative.Value)
                        .ThenBy(value => value.Id.Value, System.StringComparer.Ordinal)
                        .First();
                Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True, Detail(runtime));
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True, Detail(runtime));
            }
        }

        private static IEnumerator YieldUntilTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            for (var budget = 0; budget < 8; budget++)
            {
                if (runtime.PendingTurnRelease == null &&
                    (!runtime.SelectedActor.IsDefined ||
                        runtime.SelectedActor.ActorId.Equals(heroId)))
                {
                    yield break;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The enemy's turn is setup for this journey and must be endable. " +
                    Detail(runtime));
                yield return null;
            }

            Assert.Fail("The named seat never took a turn. " + Detail(runtime));
        }

        private static string Detail(ConfiguredMatchSceneRuntime runtime) =>
            $"[round {runtime.RoundIndex} turn {runtime.TurnIndex}; actor " +
            $"{(runtime.SelectedActor.IsDefined ? runtime.SelectedActor.ActorId.Value : "none")}; " +
            $"pending choice {(runtime.PendingEffectChoice == null ? "none" : runtime.PendingEffectChoice.NodeId.Value)}; " +
            $"last '{runtime.LastOperationDetail}']";

        private static IEnumerator LoadStagedScene(SceneCardContent content)
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent = content;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
