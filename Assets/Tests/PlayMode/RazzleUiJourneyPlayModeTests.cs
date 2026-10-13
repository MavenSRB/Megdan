using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.21k. Real pointer and control journeys for Razzle the Ringmaster, on boards measured
    /// headlessly before any was written. Razzle stands on (-3, 1) and Wasp on (-2, 1); his own Blue
    /// melee minion stands on (-3, 2) and Red's ranged minion on (-4, 2), both beside him, so the three
    /// spaces (-4, 1), (-3, 0) and (-2, 0) are the empty ones Stunt Doubles' Radius 1 reaches. Where a
    /// face needs his other bodies they are staged on (-4, 1) and (-3, 0) through the Director's body
    /// seam (9.21i), and every card of his then asks "which of you acts" first - answered here, as a
    /// player answers it, by clicking a standee. The Alleyoop faces need a friendly hero, so they play
    /// a 2v2 line-up with Arien beside him. Every journey reads what its answer changed on the board or
    /// the HUD: a standee appearing, fading or moving, a card coming back to his hand, the Defense
    /// total the window shows, or the acting cue moving to the body that performs a repeat.
    ///
    /// <para>The shared journey helpers read one view per hero (<c>HeroView</c>, <c>PointTrayAt</c>),
    /// which a hero on several spaces breaks by design, so the pointer steps that touch Razzle are
    /// his own here and address a standee by its body.</para>
    /// </summary>
    public sealed class RazzleUiJourneyPlayModeTests
    {
        internal static readonly HexCoordinate RazzleSpace = new HexCoordinate(-3, 1);
        internal static readonly HexCoordinate WaspSpace = new HexCoordinate(-2, 1);
        internal static readonly HexCoordinate West = new HexCoordinate(-4, 1);
        internal static readonly HexCoordinate North = new HexCoordinate(-3, 0);
        private static readonly HexCoordinate FarSpace = new HexCoordinate(0, 1);
        private static readonly HexCoordinate ArienSpace = new HexCoordinate(-1, 1);
        private static readonly MinionId RedRanged = new MinionId("red-ranged-01");
        private static readonly MinionId RedMelee = new MinionId("red-melee-02");

        // -------------------------------------------------------------------------------------------
        // Reading the board
        // -------------------------------------------------------------------------------------------

        internal static HeroId Razzle(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == RazzleCardContent.ContentSetId).HeroId;

        private static HeroId HeroOf(ConfiguredMatchSceneRuntime runtime, string content) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == content).HeroId;

        /// <summary>His standing bodies, in body order, as (body, space).</summary>
        internal static (int Body, HexCoordinate Space)[] Bodies(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Heroes.Where(row => row.HeroId.Equals(Razzle(runtime)))
                .OrderBy(row => row.Body)
                .Select(row => (row.Body, row.Coordinate))
                .ToArray();

        private static HexCoordinate[] BodySpaces(ConfiguredMatchSceneRuntime runtime) =>
            Bodies(runtime).Select(value => value.Space).ToArray();

        /// <summary>The drawn standee of one body, which exists only while that body stands.</summary>
        internal static PrimitiveHeroView Standee(HeroId hero, int body) =>
            Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(hero) && view.Body == body &&
                    view.gameObject.activeInHierarchy);

        /// <summary>Every standee of this hero drawn on the board, in body order.</summary>
        internal static PrimitiveHeroView[] DrawnStandees(HeroId hero) =>
            Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(hero) && view.gameObject.activeInHierarchy)
                .OrderBy(view => view.Body)
                .ToArray();

        private static HexCoordinate SpaceOf(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        private static bool OnBoard(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Heroes.Any(value => value.HeroId.Equals(hero));

        private static bool MinionStands(ConfiguredMatchSceneRuntime runtime, MinionId minion) =>
            runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(minion));

        private static HexCoordinate MinionAt(ConfiguredMatchSceneRuntime runtime, MinionId minion) =>
            runtime.ReadModel.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate;

        private static CardInstance Card(ConfiguredMatchSceneRuntime runtime, HeroId owner, CardDefinitionId face) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        private static int HandCount(ConfiguredMatchSceneRuntime runtime, HeroId owner) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);

        private static PrimitiveHeroPointerInput Pointer() => Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

        /// <summary>
        /// Every projected body stands drawn, one standee each, where the projection puts it, with its
        /// own mark while he stands on several spaces; every other hero is drawn once; the rail names
        /// him once; nothing faulted.
        /// </summary>
        internal static void Healthy(ConfiguredMatchSceneRuntime runtime)
        {
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            foreach (var row in runtime.Heroes)
            {
                var view = Standee(row.HeroId, row.Body);
                Assert.That(view.Coordinate, Is.EqualTo(row.Coordinate), row.ToString());
            }

            var razzle = Razzle(runtime);
            var bodies = Bodies(runtime);
            Assert.That(DrawnStandees(razzle).Select(view => view.Body), Is.EqualTo(bodies.Select(value => value.Body)),
                "One standee per standing body and none for a body that left.");
            if (bodies.Length > 1)
                Assert.That(DrawnStandees(razzle).Select(view => view.BodyMark.VisiblePips),
                    Is.EqualTo(bodies.Select(value => value.Body)), "Body N wears N pips while he stands on several spaces.");
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Count(value => value.HeroId.Equals(razzle)),
                Is.EqualTo(1), "The rail shows one hero however many bodies stand.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging and pointer steps
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads <paramref name="scene"/>, then reloads it staged: Razzle on (-3, 1) holding
        /// <paramref name="hand"/> (the rest of his set in his discard), his further bodies on
        /// <paramref name="bodies"/>, Wasp where asked; minions on those spaces are cleared.
        /// </summary>
        internal static IEnumerator Stage(SceneCardContent scene, CardDefinitionId[] hand,
            HexCoordinate? wasp = null, HexCoordinate[] bodies = null, int? coins = null)
        {
            yield return LoadStagedScene(scene);
            var runtime = Root().Runtime;
            var razzle = Razzle(runtime);
            var waspId = HeroOf(runtime, "wasp");
            var waspAt = wasp ?? WaspSpace;
            var extra = bodies ?? Array.Empty<HexCoordinate>();
            var occupied = new List<HexCoordinate> { RazzleSpace, waspAt };
            occupied.AddRange(extra);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(razzle, RazzleSpace, coins: coins, hand: hand),
                    new SandboxHeroStaging(waspId, waspAt, hand: new[]
                    {
                        WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles,
                        WaspCardContent.Shock, WaspCardContent.LiftUp
                    })
                },
                removedMinions: runtime.ReadModel.Minions
                    .Where(value => occupied.Contains(value.Coordinate))
                    .Select(value => value.MinionId));
            StageBodies(razzle, extra);
            yield return LoadStagedScene(scene);
        }

        private static void StageBodies(HeroId razzle, IReadOnlyList<HexCoordinate> bodies)
        {
            if (bodies.Count == 0) return;
            MatchSceneCompositionRoot.NextComposedStagedHeroBodies =
                bodies.Select((space, index) => new HeroOccupancy(razzle, index + 2, space)).ToArray();
            MatchSceneCompositionRoot.NextComposedStagedHeroBodySupplies = new[]
            {
                new HeroBodySupply(razzle, QuickGameSetupConfiguration.RazzleBodySupply)
            };
        }

        /// <summary>
        /// A 2v2 line-up - Wasp and Brogan against Razzle and Arien - staged with Arien on (-1, 1),
        /// within the Alleyoop faces' Range of Razzle, Wasp out of reach on (0, 1) and Brogan far off.
        /// </summary>
        private static IEnumerator StageWithArien(CardDefinitionId[] hand, HexCoordinate[] bodies)
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            RegisteredHero Hero(string id) => roster.Single(hero => hero.ContentId.Value == id);
            var razzle = new HeroId(Hero("razzle").HeroIdValue);
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.TwoVersusTwoQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { Hero("wasp"), Hero("brogan") }, new[] { Hero("razzle"), Hero("arien") });
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(razzle, RazzleSpace, hand: hand),
                new SandboxHeroStaging(new HeroId(Hero("arien").HeroIdValue), ArienSpace),
                new SandboxHeroStaging(new HeroId(Hero("wasp").HeroIdValue), FarSpace),
                new SandboxHeroStaging(new HeroId(Hero("brogan").HeroIdValue), new HexCoordinate(2, -3))
            });
            StageBodies(razzle, bodies);
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
        }

        /// <summary>Points the tray at Razzle by clicking his first standing standee.</summary>
        private static IEnumerator PointTrayAtRazzle(MatchSceneCompositionRoot root)
        {
            var razzle = Razzle(root.Runtime);
            if (root.Runtime.ViewedHeroId.Equals(razzle)) yield break;
            var view = DrawnStandees(razzle).First();
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(razzle));
        }

        private static IEnumerator PointTrayAtHero(MatchSceneCompositionRoot root, HeroId hero)
        {
            if (hero.Equals(Razzle(root.Runtime)))
            {
                yield return PointTrayAtRazzle(root);
                yield break;
            }

            yield return PointTrayAt(root, hero);
        }

        /// <summary>Commits <paramref name="face"/> from Razzle's tray and every other seat's lowest card.</summary>
        private static IEnumerator CommitRazzleFirst(MatchSceneCompositionRoot root, CardDefinitionId face)
        {
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            yield return PointTrayAtRazzle(root);
            yield return CommitThroughTray(root, NameOf(runtime, face));
            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId)
                .Where(hero => !hero.Equals(razzle)).ToArray())
            {
                yield return PointTrayAtHero(root, seat);
                yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }
        }

        /// <summary>
        /// Answers "which of you acts" by clicking the standee of <paramref name="body"/>, and proves the
        /// question, its cues and the acting cue as a player meets them.
        /// </summary>
        internal static IEnumerator PickActingBody(MatchSceneCompositionRoot root, int body)
        {
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            var pick = runtime.ReadModel.PendingActingBodyChoice;
            Assert.That(pick, Is.Not.Null, "Several bodies stand, so his card asks which of him acts. " + Remaining(runtime));
            Assert.That(runtime.RoundView.Model.Status, Does.Contain("CHOOSE WHICH OF YOU ACTS"));
            Assert.That(runtime.BoardView.Model.PieceTargetCues
                    .Where(cue => cue.Target.HeroId.Equals(razzle)).Select(cue => cue.Target.HeroBody),
                Is.EquivalentTo(Bodies(runtime).Select(value => value.Body)), "Every standing body is cued as an answer.");
            var stale = pick.Invocations.Single(value => value.Body == body);
            var view = Standee(razzle, body);
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, view.transform.position), Is.True,
                UiPlayer.DescribeBoardRay(Pointer(), view.transform.position));
            yield return null;
            Assert.That(runtime.ReadModel.PendingActingBodyChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.ReadModel.ActingBodies.Single(), Is.EqualTo(new HeroActingBody(razzle, body)));
            Assert.That(runtime.BoardView.Model.FocusedHeroBody, Is.EqualTo(body), "The acting cue stands on the picked body.");
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseActingBody(stale)), Is.False,
                "The answered pick is stale.");
            Healthy(runtime);
        }

        /// <summary>
        /// Commits <paramref name="face"/>, waits for Razzle's turn, answers the pick with
        /// <paramref name="body"/> if one stands, and starts the action of <paramref name="kind"/>.
        /// </summary>
        private static IEnumerator BeginRazzle(MatchSceneCompositionRoot root, CardDefinitionId face,
            ActionKind kind, int body = 1)
        {
            var runtime = root.Runtime;
            yield return CommitRazzleFirst(root, face);
            yield return GarrusUiJourneyTests.WaitForActor(root, Razzle(runtime));
            if (runtime.ReadModel.PendingActingBodyChoice != null) yield return PickActingBody(root, body);
            yield return PointTrayAtRazzle(root);
            yield return StartActionThroughControl(root, kind);
        }

        /// <summary>Clicks a space on the board: the standee standing on it if one does, else the tile.</summary>
        private static IEnumerator ClickSpace(MatchSceneCompositionRoot root, HexCoordinate space)
        {
            yield return UiPlayer.WaitForDecisionWindow(root.Runtime, root.Hud);
            var standee = Object.FindObjectsByType<PrimitiveHeroView>()
                .FirstOrDefault(view => view.gameObject.activeInHierarchy && view.Coordinate.Equals(space));
            var at = standee != null ? standee.transform.position : Tile(space).transform.position;
            if (standee != null)
            {
                var camera = Pointer().EventCamera;
                foreach (var offset in new[]
                {
                    Vector3.zero,
                    Vector3.right * (standee.SelectionRadius * 0.7f),
                    Vector3.left * (standee.SelectionRadius * 0.7f),
                    Vector3.forward * (standee.SelectionRadius * 0.7f),
                    Vector3.back * (standee.SelectionRadius * 0.7f)
                })
                {
                    var candidate = standee.transform.position + offset;
                    var screen = camera.WorldToScreenPoint(candidate);
                    if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width ||
                        screen.y < 0f || screen.y > Screen.height ||
                        root.Hud.BlocksPointerAt(new Vector2(screen.x, screen.y)))
                        continue;
                    at = candidate;
                    break;
                }
            }
            var screenPoint = Pointer().EventCamera.WorldToScreenPoint(at);
            var minimised = false;
            if (root.Hud.BlocksPointerAt(new Vector2(screenPoint.x, screenPoint.y)) &&
                UiPlayer.HasButton(root.Hud, "MINIMISE"))
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "MINIMISE"));
                minimised = true;
                yield return null;
            }
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, at), Is.True, UiPlayer.DescribeBoardRay(Pointer(), at));
            yield return null;
            if (minimised && UiPlayer.HasButton(root.Hud, "EXPAND"))
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "EXPAND"));
                yield return null;
            }
        }

        /// <summary>
        /// Answers one candidate of the standing effect choice through the surface a player uses - a
        /// standee or tile on the board, a piece, or a card face - confirming it when it is still
        /// standing, and proves the answer was one accepted command and the consumed input is stale.
        /// </summary>
        private static IEnumerator Pick(MatchSceneCompositionRoot root, Func<EffectValue, bool> which, string why)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, why + " " + Remaining(runtime));
            var stale = choice.SelectInvocations.FirstOrDefault(value => which(value.Candidate.Value));
            Assert.That(stale, Is.Not.Null, why + " " + Remaining(runtime));
            var node = choice.NodeId;
            var oldVersion = runtime.Projection.Version;
            var value = stale.Candidate.Value;
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            if (value.Kind == EffectValueKind.CardInstance)
            {
                var card = runtime.RoundView.Model.EffectChoices.Single(row =>
                    row.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    row.Command.EffectChoice.Candidate.Equals(stale.Candidate));
                UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card",
                    card.Card.DisplayName.ToUpperInvariant()));
                yield return null;
                UiPlayer.Click(UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card",
                    card.Card.DisplayName.ToUpperInvariant()));
                yield return null;
            }
            else if (value.Kind == EffectValueKind.Coordinate)
            {
                yield return ClickSpace(root, value.Coordinate);
            }
            else
            {
                var at = value.Kind == EffectValueKind.Hero ? Standee(value.HeroId, 1).transform.position
                    : value.Kind == EffectValueKind.Minion ? MinionView(value.MinionId).transform.position
                    : throw new InvalidOperationException("No board answer for " + value.Kind);
                Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, at), Is.True);
                yield return null;
            }

            yield return ConfirmIfStillStanding(root, node);
            Assert.That(runtime.Projection.Version - oldVersion, Is.InRange(1L, 2L),
                "One answer, plus its confirm when required: " + node.Value + " / " + value.Kind + "; " + Remaining(runtime));
            var after = runtime.CaptureSnapshot();
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(stale)), Is.False,
                "The consumed input is stale.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after), "Stale input is atomic.");
            Healthy(runtime);
        }

        /// <summary>
        /// The first space the standing choice offers whose tile a camera ray reaches past every
        /// standee (<c>FirstClickableSpace</c>), optionally narrowed.
        /// </summary>
        private static HexCoordinate ClickableCandidateSpace(MatchSceneCompositionRoot root,
            Func<HexCoordinate, bool> which = null)
        {
            var runtime = root.Runtime;
            var spaces = runtime.PendingEffectChoice.Candidates
                .Where(value => value.Value.Kind == EffectValueKind.Coordinate)
                .Select(value => value.Value.Coordinate)
                .Where(space => which == null || which(space))
                .ToArray();
            Assert.That(spaces, Is.Not.Empty, Remaining(runtime));
            var camera = Pointer().EventCamera;
            var exposed = spaces.Where(space =>
            {
                var screen = camera.WorldToScreenPoint(Tile(space).transform.position);
                return screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width &&
                    screen.y >= 0f && screen.y <= Screen.height &&
                    !root.Hud.BlocksPointerAt(new Vector2(screen.x, screen.y));
            }).ToArray();
            Assert.That(exposed, Is.Not.Empty,
                "Every offered tile centre is covered by the HUD. " + Remaining(runtime));
            return FirstClickableSpace(Pointer(), exposed);
        }

        private static IEnumerator PickSpace(MatchSceneCompositionRoot root, HexCoordinate space, string why) =>
            Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space), why);

        /// <summary>Presses the window's confirm when a selection below its maximum still stands.</summary>
        private static IEnumerator ConfirmIfAsked(MatchSceneCompositionRoot root)
        {
            if (root.Runtime.PendingEffectChoice?.ConfirmInvocation == null) yield break;
            yield return UiPlayer.WaitForDecisionWindow(root.Runtime, root.Hud);
            UiPlayer.ClickEffectControl(root.Runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
        }

        /// <summary>
        /// Answers a printed "may" on the decision window's own pair (8.46e), found by command kind.
        /// The shared Garrus helper reads one view per hero, which his bodies break by design.
        /// </summary>
        private static IEnumerator May(MatchSceneCompositionRoot root, bool accept)
        {
            Assert.That(root.Runtime.PendingEffectChoice, Is.Not.Null, Remaining(root.Runtime));
            yield return UiPlayer.WaitForDecisionWindow(root.Runtime, root.Hud);
            UiPlayer.ClickEffectControl(root.Runtime, root.Hud,
                accept ? EffectChoiceCommandKind.Confirm : EffectChoiceCommandKind.Decline);
            yield return null;
            yield return null;
            Healthy(root.Runtime);
        }

        /// <summary>Names a unit as the Attack's target by clicking its piece: a body's standee, a hero or a minion.</summary>
        private static IEnumerator TargetStandee(MatchSceneCompositionRoot root, HeroId hero, int body = 1)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
            var view = Standee(hero, body);
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, view.transform.position), Is.True,
                UiPlayer.DescribeBoardRay(Pointer(), view.transform.position));
            yield return null;
        }

        private static IEnumerator TargetMinion(MatchSceneCompositionRoot root, MinionId minion)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetMinionIds, Has.Member(minion), Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, MinionView(minion).transform.position), Is.True);
            yield return null;
        }

        private static IEnumerator TakeTheHit(MatchSceneCompositionRoot root)
        {
            Assert.That(root.Runtime.PendingDefenseChoice, Is.Not.Null, Remaining(root.Runtime));
            UiPlayer.ClickDefenseDecline(root.Runtime, root.Hud);
            yield return null;
        }

        /// <summary>Declines every optional spawn still asked.</summary>
        private static IEnumerator DeclineSpawns(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            for (var budget = 0; budget < 4 && runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.NodeId.Value.StartsWith("stunt-doubles-spawn", StringComparison.Ordinal); budget++)
                yield return May(root, false);
        }

        /// <summary>Wasp attacks Razzle's body 1 with Magnetic Dagger this turn and waits at his Defense.</summary>
        private static IEnumerator WaspAttacksRazzle(MatchSceneCompositionRoot root, CardDefinitionId razzleCommits)
        {
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return PointTrayAtRazzle(root);
            yield return CommitThroughTray(root, NameOf(runtime, razzleCommits));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.MagneticDagger));
            yield return YieldUntilTheNamedSeatActs(runtime, wasp);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return TargetStandee(root, Razzle(runtime), 1);
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
        }

        // -------------------------------------------------------------------------------------------
        // Gold: the spawn
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator StuntDoublesSpawnsTwoBodiesBesideHimAndDeclinesTheThird()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.StuntDoubles, RazzleCardContent.CrowdControl });
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            var wasp = HeroOf(runtime, "wasp");
            yield return BeginRazzle(root, RazzleCardContent.StuntDoubles, ActionKind.Attack);
            yield return TargetStandee(root, wasp);
            yield return TakeTheHit(root);
            Assert.That(OnBoard(runtime, wasp), Is.False, "The basic Attack defeats Wasp. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate),
                Is.EquivalentTo(new[] { West, North, new HexCoordinate(-2, 0), WaspSpace }),
                "After the attack each spawn offers the empty spaces in Radius 1. " + Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            var first = ClickableCandidateSpace(root);
            yield return PickSpace(root, first, "The first body.");
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            var second = ClickableCandidateSpace(root);
            yield return PickSpace(root, second, "The second body.");
            yield return May(root, false);
            Assert.That(runtime.PendingEffectChoice, Is.Null, "The third spawn was declined. " + Remaining(runtime));
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, first, second }));
            var standees = DrawnStandees(razzle);
            Assert.That(standees.Select(view => view.Coordinate), Is.EqualTo(new[] { RazzleSpace, first, second }),
                "A standee stands on each spawned body.");
            Assert.That(standees.Select(view => view.HeroContentId).Distinct().Count(), Is.EqualTo(1),
                "One hero, one team face on every standee.");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Red: removals and the counting faces
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator RemovalFacesTakeBodiesOffTheBoardWithoutADefeat()
        {
            foreach (var face in new[] { RazzleCardContent.PhantomStrike, RazzleCardContent.HitAndGone, RazzleCardContent.IntoThinAir })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                    new[] { face, RazzleCardContent.CrowdControl }, bodies: new[] { West, North });
                var root = Root();
                var runtime = root.Runtime;
                var razzle = Razzle(runtime);
                var wasp = HeroOf(runtime, "wasp");
                var lives = runtime.ReadModel.TeamLifeCounters.ToArray();
                yield return BeginRazzle(root, face, ActionKind.Attack);
                yield return TargetStandee(root, wasp);
                yield return TakeTheHit(root);
                var lifeAfterWasp = runtime.ReadModel.TeamLifeCounters.ToArray();
                Assert.That(runtime.PendingEffectChoice?.NodeId.Value, Is.EqualTo(RazzleEffectContent.RemovalNode(face)),
                    face.Value + " " + Remaining(runtime));
                if (face.Equals(RazzleCardContent.PhantomStrike))
                {
                    // "If there is more than one of you in play, you may remove one of you."
                    yield return PickSpace(root, West, "One of him.");
                    Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, North }), face.Value);
                }
                else if (face.Equals(RazzleCardContent.HitAndGone))
                {
                    // "... one or more of you in play, except the last one of you": two of three, then confirm.
                    yield return ClickSpace(root, West);
                    yield return ClickSpace(root, North);
                    yield return ConfirmIfAsked(root);
                    Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace }), face.Value);
                }
                else
                {
                    // "... including all of you. (If all are removed, you are not defeated.)"
                    yield return ClickSpace(root, West);
                    yield return ClickSpace(root, North);
                    yield return ClickSpace(root, RazzleSpace);
                    if (runtime.PendingEffectChoice != null)
                    {
                        yield return ConfirmIfAsked(root);
                    }

                    Assert.That(OnBoard(runtime, razzle), Is.False, "Every body is removed. " + Remaining(runtime));
                    Assert.That(DrawnStandees(razzle), Is.Empty, "No standee of his is left drawn.");
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null, face.Value + " " + Remaining(runtime));
                Assert.That(runtime.ReadModel.TeamLifeCounters, Is.EqualTo(lifeAfterWasp),
                    face.Value + ": a removal is not a Defeat, so no Life counter moves.");
                Assert.That(lives, Is.Not.EqualTo(lifeAfterWasp), "Wasp's own Defeat did cost her side a Life.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator CrowdControlAsASkillRemovesEveryOtherBodyFromThePickedOne()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.CrowdControl, RazzleCardContent.StuntDoubles }, wasp: FarSpace,
                bodies: new[] { West, North });
            var root = Root();
            var runtime = root.Runtime;
            yield return BeginRazzle(root, RazzleCardContent.CrowdControl, ActionKind.Skill, body: 2);
            Assert.That(runtime.PendingEffectChoice, Is.Null, "\"Remove all other you\" asks nothing. " + Remaining(runtime));
            Assert.That(Bodies(runtime), Is.EqualTo(new[] { (2, West) }),
                "Every body but the one that acts leaves the board.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator CrowdControlDiscardedToDefendAddsTwoForEachOtherBodyInRadius()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.Tightrope, RazzleCardContent.CrowdControl }, bodies: new[] { West, North });
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            yield return WaspAttacksRazzle(root, RazzleCardContent.Tightrope);
            var crowd = Card(runtime, razzle, RazzleCardContent.CrowdControl);
            var offered = runtime.RoundView.Model.DefenseChoices.Single(value =>
                value.DefenseBreakdown != null && value.DefenseBreakdown.CardInstanceId.HasValue &&
                value.DefenseBreakdown.CardInstanceId.Value.Equals(crowd.Id));
            Assert.That(offered.DefenseBreakdown.DefenseTotal, Is.EqualTo(5),
                "Defense 1, +2 for each of the two other bodies within Radius 3 of the attacked body. " +
                offered.DefenseBreakdown.Text);
            Assert.That(offered.DefenseBreakdown.DefenseRows.Select(row => row.Amount), Has.Member(4),
                "Crowd Control's own +2 per other body is a row of the breakdown the window shows.");
            Assert.That(offered.DefenseBreakdown.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                offered.DefenseBreakdown.ResultStatement);
            yield return ClickDefenseFace(root, RazzleCardContent.CrowdControl);
            Assert.That(runtime.PendingDefenseChoice, Is.Null, Remaining(runtime));
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, West, North }),
                "The Defense holds: every body still stands.");
            Assert.That(Card(runtime, razzle, RazzleCardContent.CrowdControl).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator RummageAndRansackRetrieveACardForEachOtherBodyInRadius()
        {
            foreach (var face in new[] { RazzleCardContent.Rummage, RazzleCardContent.Ransack })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                    new[] { face, RazzleCardContent.CrowdControl }, bodies: new[] { West, North });
                var root = Root();
                var runtime = root.Runtime;
                var razzle = Razzle(runtime);
                var wasp = HeroOf(runtime, "wasp");
                yield return BeginRazzle(root, face, ActionKind.Attack);
                yield return TargetStandee(root, wasp);
                yield return TakeTheHit(root);
                var before = HandCount(runtime, razzle);
                // Rummage: one retrieve, if another body is in radius; Ransack: one per other body (two).
                var retrieves = face.Equals(RazzleCardContent.Rummage) ? 1 : 2;
                for (var retrieve = 0; retrieve < retrieves; retrieve++)
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null, face.Value + " retrieve " + retrieve + " " + Remaining(runtime));
                    yield return May(root, true);
                    yield return Pick(root, value => value.Kind == EffectValueKind.CardInstance, "A discarded card of his.");
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    face.Value + " asks no further retrieve with two other bodies in radius. " + Remaining(runtime));
                Assert.That(HandCount(runtime, razzle), Is.EqualTo(before + retrieves), face.Value);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Blue: swaps and pushes
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator AlleyoopFacesSwapWithArienAndMoveAnotherBody()
        {
            foreach (var face in new[] { RazzleCardContent.Alleyoop, RazzleCardContent.GroupPerformance, RazzleCardContent.TeamSpirit })
            {
                yield return StageWithArien(new[] { face, RazzleCardContent.CrowdControl }, new[] { West, North });
                var root = Root();
                var runtime = root.Runtime;
                var arien = HeroOf(runtime, "arien");
                yield return BeginRazzle(root, face, ActionKind.Skill);
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                    "Arien, the friendly hero in range.");
                Assert.That(SpaceOf(runtime, arien), Is.EqualTo(RazzleSpace), face.Value + ": Arien takes the acting body's space.");
                Assert.That(Bodies(runtime).First().Space, Is.EqualTo(ArienSpace), face.Value);
                yield return PickSpace(root, West, "Another body of his.");
                var destination = ClickableCandidateSpace(root, space => HexDistanceQuery.Calculate(space, West) == 1);
                yield return PickSpace(root, destination, "One space on.");
                Assert.That(Bodies(runtime).Single(value => value.Body == 2).Space, Is.EqualTo(destination),
                    face.Value + " moves the other body. " + Remaining(runtime));
                Assert.That(Bodies(runtime).Single(value => value.Body == 3).Space, Is.EqualTo(North));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator MagicTrickFacesPushWaspAndRetreatThisBodyTheSameDistance()
        {
            foreach (var face in new[] { RazzleCardContent.MagicTrick, RazzleCardContent.AaaandItsGone })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                    new[] { face, RazzleCardContent.CrowdControl });
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                var distance = face.Equals(RazzleCardContent.MagicTrick) ? 2 : 3;
                yield return BeginRazzle(root, face, ActionKind.Skill);
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    "Wasp, adjacent.");
                Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.IntegerValue),
                    Is.EqualTo(Enumerable.Range(0, distance + 1)), face.Value + " asks up to " + distance + ". " + Remaining(runtime));
                var farthest = runtime.RoundView.Model.EffectChoices.Single(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    value.Command.EffectChoice.Candidate.Value.Kind == EffectValueKind.Integer &&
                    value.Command.EffectChoice.Candidate.Value.IntegerValue == distance);
                var distanceNode = runtime.PendingEffectChoice.NodeId;
                UiPlayer.ClickEffectAnswer(runtime, root.Hud, farthest.Command);
                yield return null;
                yield return ConfirmIfStillStanding(root, distanceNode);
                yield return null;
                Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(new HexCoordinate(-2 + distance, 1)),
                    face.Value + " pushes Wasp straight away. " + Remaining(runtime));
                Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { new HexCoordinate(-3 - distance, 1) }),
                    face.Value + ": this one of him moves the same distance the other way.");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Green: moves and swaps, and the repeat by a different body
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TightropeFacesMoveAnotherBodyAfterTheWalk()
        {
            foreach (var face in new[] { RazzleCardContent.Tightrope, RazzleCardContent.HighWire, RazzleCardContent.WireDancers })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                    new[] { face, RazzleCardContent.CrowdControl }, wasp: FarSpace, bodies: new[] { West, North });
                var root = Root();
                var runtime = root.Runtime;
                yield return BeginRazzle(root, face, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, face.Value + " " + Remaining(runtime));
                // The whole Movement 2 in one click: the walk ends by itself and p.11 reaches the text
                // after it. END TURN would walk away from the optional step instead (8.35v).
                var step = FirstClickableSpace(Pointer(), runtime.PendingMovementChoice.LegalDestinations
                    .Where(value => value.Cost == 2).Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(step).transform.position), Is.True);
                yield return null;
                Assert.That(runtime.PendingMovementChoice, Is.Null, "The Movement is spent. " + Remaining(runtime));

                Assert.That(Bodies(runtime).First().Space, Is.EqualTo(step), face.Value + " walks the acting body.");
                Assert.That(runtime.PendingEffectChoice?.NodeId.Value, Does.EndWith("-other-body"),
                    "After you move you may move another one of you. " + Remaining(runtime));
                if (face.Equals(RazzleCardContent.HighWire))
                {
                    // Declined: the walk stands and no other body moves.
                    yield return May(root, false);
                    Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { step, West, North }), face.Value);
                }
                else
                {
                    yield return PickSpace(root, West, "Another body of his.");
                    var destination = ClickableCandidateSpace(root, space =>
                        HexDistanceQuery.Calculate(space, West) == 1 && !space.Equals(RazzleSpace));
                    yield return PickSpace(root, destination, "One space on.");
                    Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { step, destination, North }), face.Value);
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator TheatricsSwapsThePickedBodyWithAMinionInItsRange()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.Theatrics, RazzleCardContent.CrowdControl }, wasp: FarSpace,
                bodies: new[] { West, North });
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            yield return BeginRazzle(root, RazzleCardContent.Theatrics, ActionKind.Skill, body: 2);
            var marker = runtime.BoardView.ActingHeroMarker;
            Assert.That(marker != null && marker.IsVisible, Is.True, "The acting cue is drawn.");
            Assert.That(Flat(marker.transform.position), Is.EqualTo(Flat(Standee(razzle, 2).transform.position)),
                "The acting cue stands on the standee that was clicked.");
            yield return Pick(root, value => value.Kind == EffectValueKind.Minion && value.MinionId.Equals(RedRanged),
                "Red's ranged minion, within Range 2 of body 2.");
            Assert.That(Bodies(runtime).Single(value => value.Body == 2).Space, Is.EqualTo(new HexCoordinate(-4, 2)),
                "The picked body swaps places with the minion.");
            Assert.That(MinionAt(runtime, RedRanged), Is.EqualTo(West));
            Assert.That(Bodies(runtime).Single(value => value.Body == 1).Space, Is.EqualTo(RazzleSpace));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator SpectacleRepeatsItsSwapFromTheOtherBodyWithADifferentMinion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.Spectacle, RazzleCardContent.CrowdControl }, wasp: FarSpace,
                bodies: new[] { West });
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            yield return BeginRazzle(root, RazzleCardContent.Spectacle, ActionKind.Skill);
            yield return Pick(root, value => value.Kind == EffectValueKind.Minion && value.MinionId.Equals(RedMelee),
                "Red's melee minion on (-1, 0), in Range 2 of body 1.");
            Assert.That(Bodies(runtime).Single(value => value.Body == 1).Space, Is.EqualTo(new HexCoordinate(-1, 0)));
            Assert.That(MinionAt(runtime, RedMelee), Is.EqualTo(RazzleSpace));
            Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate),
                Is.EqualTo(new[] { West }), "May repeat once by a different one of you: only body 2 is offered. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Not.Null, "The repeat is a \"may\".");
            yield return PickSpace(root, West, "Body 2 performs the repeat.");
            Assert.That(runtime.BoardView.Model.FocusedHeroBody, Is.EqualTo(2), "The acting cue moves to body 2.");
            Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.MinionId),
                Has.No.Member(RedMelee), "Targeting a different minion, though the first stands beside body 2.");
            yield return Pick(root, value => value.Kind == EffectValueKind.Minion && value.MinionId.Equals(RedRanged),
                "Red's ranged minion beside body 2.");
            Assert.That(Bodies(runtime).Single(value => value.Body == 2).Space, Is.EqualTo(new HexCoordinate(-4, 2)));
            Assert.That(MinionAt(runtime, RedRanged), Is.EqualTo(West));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator TwinStrikeRepeatsTheBasicAttackFromAnotherBodyAgainstADifferentUnit()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedRazzle,
                new[] { RazzleCardContent.StuntDoubles, RazzleCardContent.CrowdControl }, bodies: new[] { West });
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            var wasp = HeroOf(runtime, "wasp");
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "Twin Strike stands face up on Razzle's rail.");
            yield return BeginRazzle(root, RazzleCardContent.StuntDoubles, ActionKind.Attack);
            yield return TargetStandee(root, wasp);
            yield return TakeTheHit(root);
            yield return DeclineSpawns(root);
            Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate),
                Is.EqualTo(new[] { West }),
                "After the basic attack action a different him may repeat it: body 2 is offered. " + Remaining(runtime));
            if (UiPlayer.HasButton(root.Hud, "MINIMISE"))
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "MINIMISE"));
                yield return null;
            }
            yield return PickSpace(root, West, "Body 2 performs the repeat.");
            Assert.That(runtime.BoardView.Model.FocusedHeroBody, Is.EqualTo(2), "The acting cue moves to body 2.");
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetMinionIds, Is.EqualTo(new[] { RedRanged }),
                "The repeated Attack reaches the unit beside body 2. " + Remaining(runtime));
            yield return TargetMinion(root, RedRanged);
            Assert.That(MinionStands(runtime, RedRanged), Is.False, "The repeated Attack defeats the minion.");
            Assert.That(runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate),
                Has.Member(new HexCoordinate(-5, 1)),
                "The whole action repeats: its spawn is measured from body 2 (RZ-Q11). " + Remaining(runtime));
            yield return DeclineSpawns(root);
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, West }));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Every face's ribbon
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesHimThroughItsCardControl()
        {
            foreach (var definition in RazzleCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                    new[] { definition.Id, RazzleCardContent.CrowdControl }, wasp: FarSpace);
                var root = Root();
                var runtime = root.Runtime;
                var razzle = Razzle(runtime);
                yield return BeginRazzle(root, definition.Id, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value);
                var target = FirstClickableSpace(Pointer(), runtime.PendingMovementChoice.LegalDestinations
                    .Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(target).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, razzle), Is.EqualTo(target), definition.Id.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in RazzleCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction)))
            {
                var committed = definition.Id.Equals(RazzleCardContent.Tightrope)
                    ? RazzleCardContent.WireDancers : RazzleCardContent.Tightrope;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle, new[] { definition.Id, committed });
                var root = Root();
                var runtime = root.Runtime;
                var razzle = Razzle(runtime);
                yield return WaspAttacksRazzle(root, committed);
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True, definition.Id.Value);
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(Card(runtime, razzle, definition.Id).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                    definition.Id.Value);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Session continuity, lifecycle and components
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator APausedHitAndGoneRemovalRestoresAtItsQuestion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.HitAndGone, RazzleCardContent.CrowdControl }, bodies: new[] { West, North });
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            // Body 2 acts: its Attack reaches the ranged minion beside it, not Wasp.
            yield return BeginRazzle(root, RazzleCardContent.HitAndGone, ActionKind.Attack, body: 2);
            yield return TargetMinion(root, RedRanged);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked?.NodeId.Value, Is.EqualTo(RazzleEffectContent.RemovalNode(RazzleCardContent.HitAndGone)), Remaining(runtime));
            var pause = runtime.CaptureSnapshot();
            yield return ClickSpace(root, North);
            yield return ConfirmIfAsked(root);
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, West }));
            Assert.That(runtime.RestoreSnapshot(pause), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId),
                "The restored match waits at the removal. " + Remaining(runtime));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, West, North }),
                "Every body is read back from the snapshot and drawn again.");
            Assert.That(runtime.ReadModel.ActingBodies.Single(), Is.EqualTo(new HeroActingBody(razzle, 2)),
                "The settled acting body is restored with it.");
            Healthy(runtime);
            yield return ClickSpace(root, RazzleSpace);
            yield return ConfirmIfAsked(root);
            Assert.That(Bodies(runtime), Is.EqualTo(new[] { (2, West), (3, North) }), "The restored removal resolves.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator AStuntDoublesSpawnPauseUndoesToItsRetainedPrefixAndReplaysExactly()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.StuntDoubles, RazzleCardContent.CrowdControl });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return BeginRazzle(root, RazzleCardContent.StuntDoubles, ActionKind.Attack);
            yield return TargetStandee(root, wasp);
            yield return TakeTheHit(root);
            var first = runtime.PendingEffectChoice.NodeId;
            var pause = runtime.CaptureSnapshot();
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            var spawnAt = ClickableCandidateSpace(root);
            yield return PickSpace(root, spawnAt, "The first body.");
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, spawnAt }));
            if (UiPlayer.HasButton(root.Hud, "EXPAND"))
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "EXPAND"));
                yield return null;
            }
            bool DirectorReady() => UiPlayer.HasButton(root.Hud, "DIRECTOR") &&
                UiPlayer.Button(root.Hud, "DIRECTOR").worldBound.width > 0f &&
                UiPlayer.Button(root.Hud, "DIRECTOR").worldBound.height > 0f;
            for (var frame = 0; frame < 600 && !DirectorReady(); frame++)
            {
                if (UiPlayer.HasButton(root.Hud, "CONTINUE"))
                {
                    var release = UiPlayer.Button(root.Hud, "CONTINUE");
                    if (release.enabledInHierarchy && release.worldBound.width > 0f)
                        UiPlayer.Click(release);
                }
                yield return null;
            }
            OpenDirectorDrawer(root.Hud);
            yield return null;
            for (var press = 0; press < 3 && !(runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.NodeId.Equals(first)); press++)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(first),
                "Undo gives the first spawn back. " + Remaining(runtime));
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace }), "The spawned body is taken back.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause), "The retained prefix is the same pause.");
            Healthy(runtime);
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            yield return PickSpace(root, spawnAt, "The same space again.");
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, spawnAt }));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator EveryBodyLeavesOnHisDefeatAndHeRespawnsAsOneBeforeARestart()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.Tightrope, RazzleCardContent.CrowdControl }, bodies: new[] { West, North });
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            var standees = DrawnStandees(razzle);
            Assert.That(standees.Select(view => view.Body), Is.EqualTo(new[] { 1, 2, 3 }));
            yield return WaspAttacksRazzle(root, RazzleCardContent.Tightrope);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(OnBoard(runtime, razzle), Is.False, "One Defeat removes every body (RZ-Q3). " + Remaining(runtime));
            foreach (var standee in standees)
            {
                Assert.That(standee.gameObject.activeSelf, Is.False, "Body " + standee.Body + " leaves the board.");
                Assert.That(runtime.BoardView.HeroDefeatGhostFor(standee), Is.Not.Null, "Body " + standee.Body + " fades.");
            }

            Healthy(runtime);
            // His turn 1 card resolves off the board; at his next action p.7 offers the respawn.
            yield return YieldUntilTheNamedSeatActs(runtime, razzle);
            CommitSoTheNamedSeatActs(runtime, razzle, RazzleCardContent.CrowdControl);
            yield return YieldUntilTheNamedSeatActs(runtime, razzle);
            var respawn = FirstClickableSpace(Pointer(),
                runtime.RoundView.Model.HeroRespawnOptions.Select(option => option.Destination).ToArray());
            var camera = Pointer().EventCamera;
            var tile = Tile(respawn);
            var depth = camera.WorldToScreenPoint(tile.transform.position).z;
            camera.transform.position += tile.transform.position -
                camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(Bodies(runtime), Is.EqualTo(new[] { (1, respawn) }), "He respawns as one body.");
            Healthy(runtime);
            for (var press = 0; press < 6 && !UiPlayer.HasButton(root.Hud, "DIRECTOR"); press++)
            {
                var next = new[] { "CONTINUE", "END TURN" }
                    .Where(label => UiPlayer.HasButton(root.Hud, label))
                    .Select(label => UiPlayer.Button(root.Hud, label))
                    .FirstOrDefault(button => button.enabledInHierarchy);
                Assert.That(next, Is.Not.Null, UiPlayer.Describe(root.Hud));
                UiPlayer.Click(next);
                yield return null;
            }

            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(BodySpaces(runtime), Is.EqualTo(new[] { RazzleSpace, West, North }),
                "Restart reconstructs the staged board, every body with its standee.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedRazzleSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedRazzle);
            var root = Root();
            var razzle = Razzle(root.Runtime);
            var view = Standee(razzle, 1);
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo(RazzleCardContent.ContentSetId));
            Assert.That(view.HasTokenVisual, Is.True, "His prepared standee face is drawn.");
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(razzle));
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator EveryRazzleFaceKeepsTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedRazzle);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Razzle", RazzleCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator RazzleLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedRazzle,
                new[] { RazzleCardContent.PhantomStrike, RazzleCardContent.CrowdControl }, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var razzle = Razzle(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(razzle));
                budget++)
                yield return RoundStep(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(razzle));
            var previous = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon)
                .Select(value => value.Value).SingleOrDefault();
            UiPlayer.EnsureVisible(UpgradeButton(root, upgrade));
            yield return null;
            UiPlayer.Click(UpgradeButton(root, upgrade));
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId)).State.Location,
                Is.EqualTo(CardLocation.Hand));
            for (var budget = 0; budget < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return RoundStep(root);
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(razzle));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value, Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(razzle)).ItemBonuses,
                Is.Not.Empty);
            Healthy(runtime);
        }

        /// <summary>One step toward the next round, answering a Minion Battle removal where one is asked.</summary>
        private static IEnumerator RoundStep(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId), Is.True);
                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                yield return null;
                yield break;
            }

            yield return NextRoundDecision(root);
        }

        private static Vector2 Flat(Vector3 position) =>
            new Vector2(Mathf.Round(position.x * 1000f) / 1000f, Mathf.Round(position.z * 1000f) / 1000f);
    }
}
