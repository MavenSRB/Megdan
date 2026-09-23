using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Swift the Sharpshooter's piece, her tray and her starting hand, driven through real pointer
    /// events and camera rays. Milestone 8.38f, and the THIRTEENTH hero to take the player-path gate.
    ///
    /// <para><b>Why this fixture's scene is an engaged one.</b> Rulebook p.5's six official seats are
    /// what <see cref="SceneCardContent.ConfiguredMatch"/> deals, and Swift is not among them - the
    /// roster-expansion amendment has not placed her in an official line-up. Every arm that deals her
    /// restages both heroes into the engagement milestone 8.15 built, exactly as the twelve fixtures
    /// before this one do.</para>
    ///
    /// <para><b>She needs no ally arm at all</b>, which is worth saying because Brogan's and Tali's
    /// stagings both did. Nothing her nineteen faces select names a FRIENDLY hero: her Red line
    /// targets units at range, her Blue line places herself, and her Green line qualifies an enemy by
    /// that enemy's own footing. A one-Blue-hero board offers every selection she makes.</para>
    ///
    /// <para>The driving helpers are shared rather than copied, on milestone 8.35z34's precedent: what
    /// is Swift's own is <see cref="SwiftSeat"/> and the journeys.</para>
    /// </summary>
    public sealed class SwiftUiJourneyPlayModeTests
    {
        /// <summary>
        /// The staged seat dealt Swift's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering.
        /// </summary>
        internal static QuickGameHeroSetupState SwiftSeat(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == SwiftCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Swift to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0];
        }

        [UnityTest]
        public IEnumerator HerTrayCommitsACard_RevealsIt_AndWalksItsPrintedBoot()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var swiftId = SwiftSeat(runtime).HeroId;

            // The highest secondary Movement in her starting hand. Bounce prints NO boot at all - its
            // one ribbon icon is a Defense shield in the boot's slot, which is milestone 8.35z20's
            // slot-position trap and the hardest single reading of her inventory pass - so the helper
            // choosing by printed value is what keeps this journey off it.
            yield return CommitEverySeat(root, swiftId, MovingCardOf(runtime, swiftId));

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(swiftId));
            Assert.That(runtime.GetCommitmentStatus(swiftId),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed));

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(hud, movement.Label));
            yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);

            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var destination = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .First();
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, tile.transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(destination));
            // MILESTONE 8.86: the walk is a walk. A one-cost destination cannot be the space she is
            // standing on, and this says so rather than leaving it to the cost to imply.
            Assert.That(destination, Is.Not.EqualTo(startedAt),
                "Her printed boot really carried her off the space she started on.");
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>()
                    .Single(view => view.Occupancy.HeroId.Equals(swiftId)).Coordinate,
                Is.EqualTo(destination),
                "and her token is drawn where she now is, not where she was.");

            if (runtime.PendingMovementChoice != null)
            {
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                UiPlayer.Click(UiPlayer.Button(hud, stop.Label));
                yield return null;
            }

            var played = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(swiftId)).TrayCards
                .Single(card => card.Group == TrayCardGroup.Played);
            Assert.That(played.HistoryLabel, Is.EqualTo("PLAYED · TURN 1"));
        }

        [UnityTest]
        public IEnumerator HerDefenseAnswersAnAttack_AndHerPieceRespawnsWhenItDoesNot()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSwift);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var swiftId = SwiftSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, swiftId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // The engagement is two spaces wide, so the ATTACKER closes it first. Snipe is reserved
            // because her own file journey needs it and this filler must not spend it.
            yield return SpendATurnClosingTheGap(
                root, enemyId, swiftId, SwiftCardContent.Snipe);

            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, swiftId, LowestInitiativeIdOf(runtime, swiftId, SwiftCardContent.Snipe));
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy's Attack must act before Swift's lowest card. " + Remaining(runtime));

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(swiftId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);

            // p.14's Defense response standing IS the SecondaryDefenseOffer shape every row of hers
            // rests here: sixteen of her nineteen faces print a Defense secondary, so the offer is
            // what a player actually reads at this moment.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers, Is.Not.Empty,
                "Her hand holds Defense secondaries, so the offer is not empty.");
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat regardless of either printed value.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(swiftId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(swiftId)), Is.False);

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, swiftId, SwiftCardContent.Snipe);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(swiftId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Swift must be offered her respawn at her own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            var respawnTile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(respawn.Destination));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, respawnTile.transform.position), Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(swiftId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Swift token stands after the respawn, not a second beside a stale one.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True);
        }
    }
}
