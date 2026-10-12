using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GuardsDigital.Pve.Presentation
{
    public sealed partial class PveSharedLobby
    {
        private const string SmokeMarker = "GUARDS_PVE_COOP_SMOKE: ";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartUnifiedSmoke()
        {
            if (!Environment.GetCommandLineArgs().Contains(PveCoopLobby.ArtifactSmokeFlag) ||
                SceneManager.GetActiveScene().name != PveLauncher.MatchSceneName) return;
            var runner = new GameObject("Unified PvE artifact check").AddComponent<PveSharedLobby>();
            DontDestroyOnLoad(runner.gameObject);
            runner.StartCoroutine(runner.GuardedUnifiedSmoke());
        }

        private IEnumerator GuardedUnifiedSmoke()
        {
            var run = UnifiedSmoke();
            while (true)
            {
                object current = null;
                var more = false;
                Exception failure = null;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception exception) { failure = exception; }
                if (failure != null)
                {
                    Debug.LogError(SmokeMarker + "FAIL " + failure);
                    UnityEngine.Application.Quit(1);
                    yield break;
                }
                if (!more) yield break;
                yield return current;
            }
        }

        private IEnumerator UnifiedSmoke()
        {
            UnityEngine.Application.runInBackground = true;
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, PveCoopLobby.ArtifactSmokeFlag);
            var number = int.Parse(args[index + 1]);
            var folder = Path.GetFullPath(args[index + 2]);
            var port = args[index + 3];
            var countIndex = Array.IndexOf(args, "-guardsPvePlayers");
            var playerCount = countIndex < 0 ? 3 : int.Parse(args[countIndex + 1]);
            SmokeRequire(playerCount >= 1 && playerCount <= 3 && number <= playerCount, "Invalid player count");
            var localSeats = (number == 1 ? Enumerable.Range(1, 3).Where(n => n == 1 || n > playerCount) : new[] { number })
                .Select(n => "blue-player-0" + n).ToArray();
            var seat = "blue-player-0" + number;
            var hero = new[] { "hero-wasp", "hero-arien", "hero-brogan" }[number - 1];
            var deadline = Time.realtimeSinceStartup + 150;
            Debug.Log(SmokeMarker + "BEGIN seat=" + seat + " stamp=" + BuildIdentityStamp.Current);
            yield return null;
            yield return null;
            var driver = FindAnyObjectByType<PrivateLobbyDriver>();
            SmokeRequire(driver?.Presenter != null, "Shared front door missing");
            if (number == 1)
            {
                var front = driver.View;
                front.ShowPage(PrivateLobbyEntryPage.Host);
                front.Root.Q<DropdownField>("host-map-selector").value = PrivateLobbyMode.PveLabel;
                front.Root.Q<TextField>(PrivateLobbyUnityView.HostPortFieldName).value = port;
                front.Root.Q<TextField>(PrivateLobbyUnityView.HostKeyFieldName).value = "w045-local-smoke";
                front.Root.Q<TextField>(PrivateLobbyUnityView.PlayerNameFieldName).value = "Host";
                yield return null;
                PveCoopLobby.SmokeClick(front.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "HOST"));
            }
            else
            {
                SmokeRequire(driver.Presenter.Press(PrivateLobbyControl.Join,
                    new PrivateLobbyEntry("127.0.0.1", port, "w045-local-smoke", default, "Guest " + number)), "Shared Join unavailable");
            }
            PveSharedLobby opened = null;
            while (opened == null && Time.realtimeSinceStartup < deadline)
            {
                opened = FindObjectsByType<PveSharedLobby>(FindObjectsSortMode.None).FirstOrDefault(l => l != this && l.active != null);
                yield return null;
            }
            SmokeRequire(opened != null, "PvE mode/seed adoption failed");
            var readied = false;
            var picked = new HashSet<string>();
            var placedVersion = new Dictionary<string, long>();
            while (Time.realtimeSinceStartup < deadline)
            {
                var client = opened.active;
                SmokeRequire(client.State != PrivateSessionClientState.Rejected, "Admission refused: " + client.RejectionDetail);
                if (opened.model?.Offers(PrivateLobbyControl.ClaimSeat) == true)
                {
                    var wanted = localSeats.FirstOrDefault(id => !opened.connections.Any(c => c.Client.TargetSeat.Value == id));
                    if (wanted != null)
                        opened.Press(PrivateLobbyControl.ClaimSeat, new PrivateLobbyEntry("", "", "", new SeatId(wanted)));
                }
                if (!readied && localSeats.All(id => opened.connections.Any(c => c.Client.TargetSeat.Value == id && c.Client.GrantedSeat != null)))
                {
                    opened.Press(PrivateLobbyControl.Ready, null);
                    readied = true;
                    Debug.Log(SmokeMarker + "SEATED " + seat);
                }
                if (opened.model?.Selection.IsOwnPick == true && !picked.Contains(client.TargetSeat.Value))
                {
                    var chosen = new[] { "hero-wasp", "hero-arien", "hero-brogan" }[int.Parse(client.TargetSeat.Value.Substring("blue-player-".Length)) - 1];
                    var option = opened.view.Root.Query<Button>(PrivateLobbyUnityView.HeroOptionName).ToList()
                        .Single(b => ((PrivateLobbyHeroOption)b.userData).HeroIdValue == chosen);
                    PveCoopLobby.SmokeClick(option);
                    PveCoopLobby.SmokeClick(opened.view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "PICK HERO"));
                    picked.Add(client.TargetSeat.Value);
                }
                if (opened.runtime != null && client.LatestProjection != null)
                {
                    var projection = client.LatestProjection;
                    foreach (var owner in opened.connections.Select(c => c.Client))
                    {
                        var own = owner.LatestProjection;
                        if (own == null) continue;
                        var place = own.AuthorizedCommands.FirstOrDefault(c => c.Kind == DirectorCommandKind.AnswerSystemTransitionStep);
                        if (place != null && (!placedVersion.TryGetValue(owner.TargetSeat.Value, out var last) || last != own.Version))
                        {
                            SmokeRequire(owner.TrySubmitCommand(place, out _), "Owner placement refused");
                            placedVersion[owner.TargetSeat.Value] = own.Version;
                        }
                    }
                    var commits = projection.AuthorizedCommands.Where(c => c.Kind == DirectorCommandKind.CommitCard).ToArray();
                    if (commits.Length > 0 && opened.connections.All(c => c.Client.LatestProjection?.AuthorizedCommands.Any(a => a.Kind == DirectorCommandKind.CommitCard) == true))
                    {
                        SmokeRequire(opened.runtime.Host.Seats.Find(new SeatId(seat)).HeroId.Value == hero &&
                            opened.runtime.Seat.Id.Equals(client.TargetSeat), "Chosen identity mismatch");
                        SmokeRequire(commits.All(c => c.HeroId.Equals(opened.runtime.Seat.HeroId)), "Foreign control leaked");
                        SmokeRequire(((IExperimentalReadModel)opened.runtime.ReadModel).Experiments.TeammateCoordination, "Coordination disabled");
                        foreach (var owner in opened.connections.Select(c => c.Client))
                        {
                            var ownedHero = opened.runtime.Host.Seats.Find(owner.TargetSeat).HeroId;
                            var tray = opened.runtime.HeroOverviewsView.Root.Query<VisualElement>("gd-hero-rail").ToList()
                                .Single(r => r.userData is HeroId id && id.Equals(ownedHero));
                            using (var click = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                                mousePosition = tray.Q("gd-hero-medallion").worldBound.center, button = 0, clickCount = 1 }))
                            {
                                click.target = tray.Q("gd-hero-medallion");
                                ((VisualElement)click.target).SendEvent(click);
                            }
                            SmokeRequire(opened.runtime.Seat.Id.Equals(owner.TargetSeat), "Local seat switch failed");
                            SmokeRequire(owner.LatestProjection.AuthorizedCommands.Where(c => c.Kind == DirectorCommandKind.CommitCard)
                                .All(c => c.HeroId.Equals(opened.runtime.Seat.HeroId)), "Foreign multi-seat command");
                        }
                        for (var frame = 0; frame < 30; frame++) yield return null;
                        var overlay = FindAnyObjectByType<PveEncounterView>();
                        SmokeRequire(overlay.GetComponent<UIDocument>().rootVisualElement.Q("gd-hero-rail-red")?.childCount == 6, "Enemy rail missing");
                        var rules = opened.runtime.InitialState.ModeRules;
                        var future = rules.RoundStartPlacements.Where(p => p.Round > 1).Select(p => p.HeroId).ToArray();
                        SmokeRequire(!opened.runtime.ReadModel.Commitments.Any(c => future.Contains(c.HeroId)), "Unspawned enemy committed");
                        PveContentLoader.LoadFromDirectory(PveDataPaths.Directory, out var catalog, out var shippedEncounter);
                        SmokeRequire(shippedEncounter.Spawns.Count(s => s.Round == 1 && s.EnemyTypeId == "heavy") == 1,
                            "First wave must include one Heavy");
                        var faces = PveEnemyArtwork.ResourcesFor(catalog);
                        SmokeRequire(faces.Count == 14, "Expected fourteen generated faces");
                        foreach (var face in faces)
                        {
                            SmokeRequire(ConfiguredCardArtwork.TryLoadTexture(face.Key, out var texture, out _), "Missing face: " + face.Value);
                            SmokeRequire(Math.Abs((float)texture.width / texture.height - CardFootprint.StandardWidth / CardFootprint.StandardHeight) < .01f,
                                "Distorted face: " + face.Value);
                        }
                        Debug.Log(SmokeMarker + "FACES 14 portrait; future-enemy commitments=0");
                        PveCoopLobby.CaptureArtifactBoard(Path.Combine(folder, "board-" + number + ".png"));
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder, "screen-" + number + ".png"));
                        Debug.Log(SmokeMarker + "READY seat=" + seat + " hero=" + hero + " seed=" + opened.seed);
                        File.WriteAllText(Path.Combine(folder, "ready-" + number), BuildIdentityStamp.Current);
                        break;
                    }
                }
                yield return null;
            }
            SmokeRequire(File.Exists(Path.Combine(folder, "ready-" + number)), "Timed out before commitments");
            while (!Enumerable.Range(1, playerCount).All(n => File.Exists(Path.Combine(folder, "ready-" + n))) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            SmokeRequire(Enumerable.Range(1, playerCount).All(n => File.Exists(Path.Combine(folder, "ready-" + n))), "Missing peer");
            if (number == 1) yield return new WaitForSecondsRealtime(2);
            Debug.Log(SmokeMarker + "PASS " + seat);
            UnityEngine.Application.Quit(0);
        }

        private static void SmokeRequire(bool value, string reason)
        { if (!value) throw new InvalidOperationException(reason); }
    }
}
