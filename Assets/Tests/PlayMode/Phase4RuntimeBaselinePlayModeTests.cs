using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class Phase4RuntimeBaselinePlayModeTests
    {
        private const long MaximumSteadyStateGcBytesPerFrame = 64 * 1024;
        private const double MaximumP95FrameMilliseconds = 10.0;
        private const double MaximumAverageMapRebuildMilliseconds = 25.0;
        private const double MaximumAverageSnapshotMilliseconds = 5.0;
        private const double MaximumAverageAcceptedCommandRefreshMilliseconds = 25.0;
        private const double MaximumUndoReplayMilliseconds = 250.0;

        [UnityTest]
        public IEnumerator CompleteMapAndTwoRoundDirectorFlow_StayWithinRecordedPhase4Thresholds()
        {
            var load = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            while (!load.isDone)
            {
                yield return null;
            }

            for (var index = 0; index < 8; index++)
            {
                yield return null;
            }

            var root = UnityEngine.Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            var runtime = root?.Runtime;
            var mapView = UnityEngine.Object.FindAnyObjectByType<PlaceholderTestGridView>();
            Assert.That(runtime, Is.Not.Null);
            Assert.That(mapView, Is.Not.Null);
            Assert.That(mapView.RenderedDefinition.Spaces, Has.Count.EqualTo(292));

            var steadyGc = new List<long>();
            var steadyFrameMilliseconds = new List<double>();
            using (var gcRecorder = ProfilerRecorder.StartNew(
                       ProfilerCategory.Memory,
                       "GC Allocated In Frame",
                       128))
            using (var mainThreadRecorder = ProfilerRecorder.StartNew(
                       ProfilerCategory.Internal,
                       "Main Thread",
                       128))
            {
                Assert.That(gcRecorder.Valid, Is.True, "GC allocation ProfilerRecorder is unavailable.");
                Assert.That(mainThreadRecorder.Valid, Is.True, "Main Thread ProfilerRecorder is unavailable.");
                for (var frame = 0; frame < 60; frame++)
                {
                    yield return null;
                    steadyGc.Add(gcRecorder.LastValue);
                    steadyFrameMilliseconds.Add(mainThreadRecorder.LastValue * 0.000001d);
                }
            }

            var initialOwnedResources = mapView.OwnedNativeResourceCount;
            var initialOwnedMaterials = CountOwnedMaterials();
            var initialOwnedMeshes = CountOwnedMeshes();
            var initialTileCount = mapView.GetComponentsInChildren<HexTileView>(true).Length;
            var initialMarkerCount = mapView.GetComponentsInChildren<BoardMapSpawnMarkerView>(true).Length;
            var initialMinionCount = mapView.GetComponentsInChildren<StandardMinionView>(true).Length;
            var rebuild = Stopwatch.StartNew();
            const int rebuildCount = 5;
            for (var index = 0; index < rebuildCount; index++)
            {
                mapView.Rebuild(mapView.RenderedDefinition, runtime.Setup);
                yield return null;
                Assert.That(mapView.OwnedNativeResourceCount, Is.EqualTo(initialOwnedResources));
                Assert.That(CountOwnedMaterials(), Is.EqualTo(initialOwnedMaterials));
                Assert.That(CountOwnedMeshes(), Is.EqualTo(initialOwnedMeshes));
                Assert.That(mapView.GetComponentsInChildren<HexTileView>(true), Has.Length.EqualTo(initialTileCount));
                Assert.That(mapView.GetComponentsInChildren<BoardMapSpawnMarkerView>(true), Has.Length.EqualTo(initialMarkerCount));
                Assert.That(mapView.GetComponentsInChildren<StandardMinionView>(true), Has.Length.EqualTo(initialMinionCount));
            }

            rebuild.Stop();
            var averageRebuildMilliseconds = rebuild.Elapsed.TotalMilliseconds / rebuildCount;

            var acceptedCommandRefreshMilliseconds = new List<double>();
            for (var round = 0; round < 2; round++)
            {
                for (var turn = 0; turn < 4; turn++)
                {
                    Commit(runtime, acceptedCommandRefreshMilliseconds);
                    ResolveTurn(runtime, acceptedCommandRefreshMilliseconds);
                    yield return null;
                }

                Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                while (runtime.Phase == RoundResolutionPhase.EndOfRound)
                {
                    // Milestone 7.6. p.8's Pity Coin leaves every hero able to afford a level, so from
                    // the second round on this flow meets p.15's Upgrade a Card choice. It is answered
                    // through the runtime's director command surface, because no presenter draws a
                    // paused system transition yet, and it is measured like every other accepted command
                    // here: it is one, and leaving it out would sample a flow the director no longer has.
                    var options = runtime.PausedSystemTransitionOptions;
                    if (options.Count > 0)
                    {
                        var optionId = options[0];
                        MeasureAcceptedCommandRefresh(
                            acceptedCommandRefreshMilliseconds,
                            () => runtime.AnswerPausedSystemTransition(optionId));
                        yield return null;
                        continue;
                    }

                    MeasureAcceptedCommandRefresh(
                        acceptedCommandRefreshMilliseconds,
                        runtime.ContinueEndOfRound);
                    yield return null;
                }
            }

            Assert.That(runtime.CompletedRounds.Select(value => value.RoundIndex),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(acceptedCommandRefreshMilliseconds, Has.Count.GreaterThanOrEqualTo(32));
            var averageAcceptedCommandRefreshMilliseconds =
                acceptedCommandRefreshMilliseconds.Average();
            var snapshot = Stopwatch.StartNew();
            const int snapshotCount = 20;
            for (var index = 0; index < snapshotCount; index++)
            {
                Assert.That(runtime.CaptureSnapshot(), Is.Not.Empty);
            }

            snapshot.Stop();
            var averageSnapshotMilliseconds = snapshot.Elapsed.TotalMilliseconds / snapshotCount;
            var undo = Stopwatch.StartNew();
            Assert.That(runtime.Undo(), Is.True);
            undo.Stop();

            var sortedGc = steadyGc.OrderBy(value => value).ToArray();
            var sortedFrames = steadyFrameMilliseconds.OrderBy(value => value).ToArray();
            var p95FrameMilliseconds = Percentile(sortedFrames, 0.95);
            var maxGcBytes = sortedGc[sortedGc.Length - 1];
            var screenshotPath = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath,
                "..",
                "tmp",
                "milestone-4-4d-visual.png"));
            CaptureCamera(UnityEngine.Object.FindAnyObjectByType<Camera>(), screenshotPath);

            TestContext.WriteLine(
                "PHASE4_BASELINE " +
                $"frames=60 maxGcBytesPerFrame={maxGcBytes} p95FrameMs={p95FrameMilliseconds:F3} " +
                $"mapRebuilds={rebuildCount} avgMapRebuildMs={averageRebuildMilliseconds:F3} " +
                $"ownedResources={initialOwnedResources} ownedResourceGrowth=0 " +
                $"snapshots={snapshotCount} avgSnapshotMs={averageSnapshotMilliseconds:F3} " +
                $"acceptedCommandRefreshes={acceptedCommandRefreshMilliseconds.Count} " +
                $"avgAcceptedCommandRefreshMs={averageAcceptedCommandRefreshMilliseconds:F3} " +
                $"undoReplayMs={undo.Elapsed.TotalMilliseconds:F3}");

            Assert.That(maxGcBytes, Is.LessThanOrEqualTo(MaximumSteadyStateGcBytesPerFrame));
            Assert.That(p95FrameMilliseconds, Is.LessThanOrEqualTo(MaximumP95FrameMilliseconds));
            Assert.That(averageRebuildMilliseconds, Is.LessThanOrEqualTo(MaximumAverageMapRebuildMilliseconds));
            Assert.That(averageSnapshotMilliseconds, Is.LessThanOrEqualTo(MaximumAverageSnapshotMilliseconds));
            Assert.That(averageAcceptedCommandRefreshMilliseconds,
                Is.LessThanOrEqualTo(MaximumAverageAcceptedCommandRefreshMilliseconds));
            Assert.That(undo.Elapsed.TotalMilliseconds, Is.LessThanOrEqualTo(MaximumUndoReplayMilliseconds));
            Assert.That(File.Exists(screenshotPath), Is.True, "The Phase 4 visual capture was not written.");
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            Assert.That(camera, Is.Not.Null);
            const int width = 1920;
            const int height = 1080;
            var priorTarget = camera.targetTexture;
            var priorActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Phase 4 Baseline Capture"
            };
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                name = "Phase 4 Baseline Readback"
            };
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = priorTarget;
                RenderTexture.active = priorActive;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        /// <summary>
        /// Commits one card for every hand the turn is still waiting on.
        ///
        /// <para>Milestone 8.25. It used to name the Red hand and the Blue hand, which was every hand
        /// there was; rulebook p.5's official setup deals six, so the loop asks the projection which
        /// ones are still awaiting a response instead of counting on two. That is also what makes this
        /// flow measure a six-hero turn rather than a two-hero one, which is what
        /// <c>RISK-SCALE-001</c>'s payload half is measured on.</para>
        /// </summary>
        private static void Commit(
            ConfiguredMatchSceneRuntime runtime,
            ICollection<double> acceptedCommandRefreshMilliseconds)
        {
            while (true)
            {
                var awaiting = runtime.HandsView.Model.Hands.FirstOrDefault(hand =>
                    hand.CommitmentStatus == TurnCardCommitmentStatus.AwaitingResponse &&
                    hand.Cards.Count > 0);
                if (awaiting == null)
                {
                    break;
                }

                Assert.That(runtime.SelectCommitmentCard(awaiting.Cards[0].Id), Is.True);
                MeasureAcceptedCommandRefresh(
                    acceptedCommandRefreshMilliseconds,
                    runtime.ConfirmSelectedCommitment);
            }
        }

        /// <summary>
        /// Resolves the open turn: every tied Initiative decision the reveal produced, then one End Turn
        /// for each hero that has to act. Milestone 8.25, for the reason <see cref="Commit"/> records -
        /// six heroes act where two used to, and ties between six revealed cards are ordinary.
        /// </summary>
        private static void ResolveTurn(
            ConfiguredMatchSceneRuntime runtime,
            ICollection<double> acceptedCommandRefreshMilliseconds)
        {
            var budget = (runtime.Setup.Heroes.Count * 4) + 8;
            while (runtime.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                Assert.That(budget--, Is.GreaterThan(0), "The turn did not resolve inside its budget.");
                if (runtime.PendingInitiativeChoice != null)
                {
                    var candidate = runtime.PendingInitiativeChoice.CandidateCardInstanceIds[0];
                    MeasureAcceptedCommandRefresh(
                        acceptedCommandRefreshMilliseconds,
                        () => runtime.SubmitInitiativeChoice(candidate));
                    continue;
                }

                MeasureAcceptedCommandRefresh(
                    acceptedCommandRefreshMilliseconds,
                    runtime.SubmitEndTurn);
            }
        }

        private static void MeasureAcceptedCommandRefresh(
            ICollection<double> samples,
            Func<bool> submit)
        {
            var refresh = Stopwatch.StartNew();
            var accepted = submit();
            refresh.Stop();
            Assert.That(accepted, Is.True);
            samples.Add(refresh.Elapsed.TotalMilliseconds);
        }

        private static int CountOwnedMaterials()
        {
            return Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.hideFlags == HideFlags.DontSave &&
                (value.name.EndsWith(" Debug", StringComparison.Ordinal) ||
                 value.name.EndsWith(" Placeholder", StringComparison.Ordinal) ||
                 value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName));
        }

        private static int CountOwnedMeshes()
        {
            return Resources.FindObjectsOfTypeAll<Mesh>().Count(value =>
                value != null && value.name == "Owned Placeholder Grid Hex Mesh");
        }

        private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            var index = Math.Min(sortedValues.Count - 1,
                Math.Max(0, (int)Math.Ceiling(sortedValues.Count * percentile) - 1));
            return sortedValues[index];
        }
    }
}
