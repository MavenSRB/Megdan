using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.SabinaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.SabinaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TargetingCuePhotographPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.80. The Defense window as a JOINED SEAT draws it, which is the screen the user
    /// was looking at when they reported "Defense window does not show attacking card anymroe".
    /// </summary>
    /// <remarks>
    /// <para><b>Why this fixture exists beside 8.63's.</b> Milestone 8.63's own Defense journey draws
    /// this same window at three viewports and stayed green throughout, because a scene composed for
    /// the DIRECTOR reads <c>MatchSessionProjection</c>, whose Attack frame is a whole object. A
    /// network seat does not: the host sends it the 8.40f-narrowed frame, but
    /// <c>NetworkMatchClient</c> materialises none of it, so the window's old
    /// <c>HasPendingAttackResolution ? PendingAttackResolution : null</c> read <c>null</c> on every
    /// joined seat and on the hosting player's own seat, which reads through the same client. The
    /// only Play Mode drawing that can see that is one fed by a NETWORK read model.</para>
    ///
    /// <para><b>What is real here.</b> The scene, its scaled HUD panel and its own
    /// <c>DecisionWindowUnityView</c>; a real p.14 Defense driven on real configured content; and
    /// the defending seat's projection taken through <c>NetworkSeatProjectionCodec</c>'s actual
    /// bytes, which is what a joined player's client is handed. What is NOT exercised is the lobby's
    /// admission handshake - the seat is fed the same decoded record that handshake would deliver -
    /// because the lobby composes its own match and this journey needs a STAGED attack to be
    /// deterministic at all. <c>DecisionWindowPresenterTests</c> puts the same position on the real
    /// wire end to end.</para>
    /// </remarks>
    public sealed class DefenseWindowOnTheDefendersSeatPlayModeTests
    {
        [UnityTest]
        public IEnumerator AJoinedSeatsOwnHud_DrawsTheAttackersCardInsideTheDefenseWindow()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;

            // Arrange: the enemy attacks Sabina, exactly as milestone 8.63's own journey stages it.
            var sabinaId = SabinaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, sabinaId);
            yield return WalkIntoContact(root, sabinaId, enemyId);
            Commit(runtime, enemyId, WaspCardContent.Shock);
            Commit(runtime, sabinaId, SabinaCardContent.BackToBack);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Arrange: Shock's 8 takes the turn, so the attack is the enemy's.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(sabinaId)),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "Arrange: the attack reaches p.14's Defense response. " + Remaining(runtime));

            // The defending seat, as its own client receives it: the host's seat projection, through
            // the wire's own bytes, into the client the shipped HUD attaches on a join.
            var host = runtime.Host;
            var defenderSeat = host.Seats.Seats.Single(seat => seat.Owns(sabinaId));
            var seatProjection = host.ProjectionForSeat(defenderSeat.Id);
            Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(seatProjection), out var record),
                Is.True,
                "The defending seat's own projection must cross the wire.");
            var joined = new NetworkMatchReadModel(
                runtime.ReadModel, runtime.ContentCatalogs, record, true);

            Assert.That(joined.PendingDefenseChoice, Is.Not.Null,
                "The question reaches the joined seat - it always did.");
            Assert.That(joined.PendingAttackResolution, Is.Null,
                "THE CAUSE this journey exists for: the frame does not, as an object, however " +
                "loudly HasPendingAttackResolution (" + joined.HasPendingAttackResolution +
                ") says a frame stands.");

            // The drawing: the scene's own window view, on the scene's own scaled panel.
            var model = DecisionWindowPresenter.CreateViewModel(joined, defenderSeat.HeroId);
            Assert.That(model, Is.Not.Null, "The joined seat owns the standing decision.");
            Assert.That(model.Family, Is.EqualTo(DecisionWindowFamily.Defense));
            runtime.DecisionWindowView.Render(model);
            yield return null;
            yield return null;

            var drawn = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-decision-attack-card")
                .ToArray();
            Assert.That(drawn, Has.Length.EqualTo(1),
                "The attacker's card is drawn on the joined seat's own HUD - the user's report. " +
                "Found " + drawn.Length + " of them.");
            Assert.That(UiPlayer.IsShown(drawn[0]), Is.True,
                "and it is on screen rather than laid out and hidden.");
            Assert.That(drawn[0].worldBound.width, Is.GreaterThan(0f),
                "with a real rectangle on the live panel.");

            var chip = UiPlayer.Descendants(drawn[0]).OfType<Label>()
                .Select(label => label.text)
                .FirstOrDefault(text => !string.IsNullOrEmpty(text) && text.StartsWith("ATTACK "));
            Assert.That(chip, Is.Not.Null,
                "and it carries the mockup's own chip rather than a bare face.");
            Assert.That(chip,
                Does.StartWith("ATTACK " + joined.PendingDefenseChoice.AttackTotal),
                "naming the composed Attack Total the standing choice published.");

            var directorModel = DecisionWindowPresenter.CreateViewModel(
                runtime.ReadModel, defenderSeat.HeroId);
            Assert.That(model.Title, Is.EqualTo(directorModel.Title),
                "The joined seat and the Director ask the very same question,");
            Assert.That(model.ContextCards[0].Caption,
                Is.EqualTo(directorModel.ContextCards[0].Caption),
                "and print the same chip, which is what 'on every seat' means.");
        }
    }
}
