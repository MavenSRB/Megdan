using System;
using System.Collections;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Application;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Pve.Presentation
{
    // Opt-in packaged-player validation only. Normal launch never starts these routines.
    public sealed partial class PveCoopLobby
    {
        public const string ArtifactSmokeFlag = "-guardsPveCoopSmoke";
        private const string ArtifactMarker = "GUARDS_PVE_COOP_SMOKE:";
        private bool artifactFailed;

        public static void StartArtifactSmoke(PveLauncher launcher) => launcher.StartCoroutine(OpenArtifactSmoke(launcher));

        private static IEnumerator OpenArtifactSmoke(PveLauncher launcher)
        {
            UnityEngine.Application.runInBackground = true;
            yield return null;
            yield return null;
            SmokeClick(launcher.GetComponent<UIDocument>().rootVisualElement.Q<Button>("pve-coop"));
            var opened = FindAnyObjectByType<PveCoopLobby>();
            if (opened == null) throw new InvalidOperationException("PvE Host/Join did not open.");
            opened.StartCoroutine(opened.GuardedArtifactSmoke());
        }

        private IEnumerator GuardedArtifactSmoke()
        {
            var routine = ArtifactSmoke();
            while (true)
            {
                Exception failure = null;
                bool more = false;
                object current = null;
                try { more = routine.MoveNext(); if (more) current = routine.Current; }
                catch (Exception exception) { failure = exception; }
                if (failure != null)
                {
                    Debug.Log(ArtifactMarker + " FAIL " + failure.Message);
                    UnityEngine.Application.Quit(1);
                    yield break;
                }
                if (!more) yield break;
                yield return current;
            }
        }

        private IEnumerator ArtifactSmoke()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, ArtifactSmokeFlag);
            var seatNumber = int.Parse(args[index + 1]);
            var folder = Path.GetFullPath(args[index + 2]);
            var smokePort = args[index + 3];
            if (seatNumber < 1 || seatNumber > 3 || !Directory.Exists(folder))
                throw new InvalidOperationException("Invalid artifact-smoke arguments.");
            var seatId = "blue-player-0" + seatNumber;
            var pickedHero = new[] { "hero-wasp", "hero-arien", "hero-brogan" }[seatNumber - 1];
            var deadline = Time.realtimeSinceStartup + 150;
            UnityEngine.Application.LogCallback onError = (message, stack, type) =>
            {
                if (artifactFailed || (type != LogType.Error && type != LogType.Exception)) return;
                artifactFailed = true;
                Debug.Log(ArtifactMarker + " FAIL " + message);
                UnityEngine.Application.Quit(1);
            };
            UnityEngine.Application.logMessageReceived += onError;
            try
            {
                Debug.Log(ArtifactMarker + " BEGIN seat=" + seatId + " stamp=" + BuildIdentityStamp.Current);
                port.value = smokePort;
                key.value = "w035-local-smoke";
                seedInput.value = "20261012";
                playerName.value = "Smoke " + seatNumber;
                yield return null;
                var root = document.rootVisualElement;
                SmokeClick(root.Q<Button>(seatNumber == 1 ? "pve-coop-host" : "pve-coop-join"));
                var claimed = false;
                var readied = false;
                var picked = false;
                long submittedVersion = -1;
                while (Time.realtimeSinceStartup < deadline && !artifactFailed)
                {
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                    if (client != null && client.State == PrivateSessionClientState.Admitted && !claimed)
                    {
                        var take = root.Query<Button>().ToList().FirstOrDefault(button => button.text == "Take " + seatId);
                        if (take != null) { SmokeClick(take); claimed = true; }
                    }
                    if (client?.GrantedSeat != null && !readied)
                    {
                        var ready = root.Q<Button>("pve-coop-ready");
                        if (ready != null)
                        {
                            Require(client.TargetSeat.Value == seatId, "Wrong assigned seat");
                            SmokeClick(ready); readied = true;
                            Debug.Log(ArtifactMarker + " SEATED " + seatId);
                        }
                    }
                    var pick = root.Q<Button>("pve-coop-pick-" + pickedHero);
                    if (readied && !picked && pick != null && pick.enabledInHierarchy)
                    { SmokeClick(pick); picked = true; }
                    if (runtime != null && client.LatestProjection != null)
                    {
                        var projection = client.LatestProjection;
                        var place = projection.AuthorizedCommands.FirstOrDefault(command =>
                            command.Kind == DirectorCommandKind.AnswerSystemTransitionStep);
                        if (place != null && submittedVersion != projection.Version)
                        {
                            Require(client.TrySubmitCommand(place, out _), "Placement submission refused");
                            submittedVersion = projection.Version;
                        }
                        var blue = runtime.ReadModel.Heroes.Count(hero =>
                            runtime.ReadModel.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Blue);
                        var commitments = projection.AuthorizedCommands.Where(command => command.Kind == DirectorCommandKind.CommitCard).ToArray();
                        if (blue == 3 && commitments.Length > 0)
                        {
                            Require(runtime.Seat.Id.Value == seatId && runtime.Seat.HeroId.Value == pickedHero,
                                "Chosen hero did not reach owner scene");
                            Require(commitments.All(command => command.HeroId.Equals(runtime.Seat.HeroId)),
                                "Foreign commitment authorization");
                            Require(runtime.ReadModel.Cards.Where(card => runtime.ReadModel.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue)
                                .All(card => card.OwnerId.Equals(runtime.Seat.HeroId)), "Foreign private cards leaked");
                            for (var frame = 0; frame < 30; frame++) yield return null;
                            var rail = overlay.GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>("pve-enemy-rail");
                            var column = rail?.Q("gd-hero-rail-red");
                            Require(column != null && column.childCount == 7, "Seven live enemy rows required");
                            CaptureArtifactBoard(Path.Combine(folder, "board-" + seatNumber + ".png"));
                            Debug.Log(ArtifactMarker + " READY seat=" + seatId + " hero=" + pickedHero +
                                " version=" + projection.Version + " commits=" + commitments.Length + " enemies=7");
                            var readyPath = Path.Combine(folder, "ready-" + seatNumber);
                            File.WriteAllText(readyPath + ".pending", BuildIdentityStamp.Current);
                            File.Move(readyPath + ".pending", readyPath);
                            break;
                        }
                    }
                    yield return null;
                }
                Require(File.Exists(Path.Combine(folder, "ready-" + seatNumber)), "Timed out before owner commitment window");
                while (!Enumerable.Range(1, 3).All(number => File.Exists(Path.Combine(folder, "ready-" + number))) &&
                    Time.realtimeSinceStartup < deadline && !artifactFailed) yield return null;
                Require(Enumerable.Range(1, 3).All(number => File.Exists(Path.Combine(folder, "ready-" + number)) &&
                    File.ReadAllText(Path.Combine(folder, "ready-" + number)) == BuildIdentityStamp.Current), "Peers missing or build identities differ");
                if (seatNumber == 1) yield return new WaitForSecondsRealtime(2);
                Require(!artifactFailed, "Player reported an error");
                Debug.Log(ArtifactMarker + " PASS " + seatId);
                UnityEngine.Application.Quit(0);
            }
            finally { UnityEngine.Application.logMessageReceived -= onError; }
        }

        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException(ArtifactMarker + " " + reason); }

        private static void SmokeClick(Button button)
        {
            Require(button != null && button.enabledInHierarchy, "Unavailable smoke control");
            button.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
            { submit.target = button; button.SendEvent(submit); }
        }

        private static void CaptureArtifactBoard(string path)
        {
            var camera = Camera.main;
            Require(camera != null, "No board camera");
            var target = new RenderTexture(1920, 1080, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log(ArtifactMarker + " BOARD position=" + camera.transform.position + " size=" + camera.orthographicSize);
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                target.Release(); Destroy(target); Destroy(texture);
            }
        }
    }
}
