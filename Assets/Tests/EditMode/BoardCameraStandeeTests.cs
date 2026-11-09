using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class BoardCameraStandeeTests
    {
        [TestCase(1f)]
        [TestCase(8f)]
        public void IllustratedAttackFramingKeepsStandeesUprightAcrossDistances(float separation)
        {
            var frame = AttackSpotlightFraming.Frame(Vector3.zero, Vector3.right * separation,
                Vector3.right, Vector3.forward, 16f / 9f, 1f, 20f, illustratedStandees: true);
            var elevation = Mathf.Asin(-frame.ViewDirection.y) * Mathf.Rad2Deg;
            Assert.That(elevation, Is.LessThan(IllustratedStandeeView.StandeeAngle));
            Assert.That(elevation, Is.EqualTo(45f).Within(0.01f));
            Assert.That(frame.Focus.y, Is.EqualTo(0.75f));
        }
        [TestCase(true, true, 0f)]
        [TestCase(true, true, 135f)]
        [TestCase(true, false, 270f)]
        [TestCase(false, true, 45f)]
        public void AttackLeansAndBumpsOnlyStandingLandedHits_AndRestoresOnCancel(bool standing, bool landed, float yaw)
        {
            var attackerHost = new GameObject("attacker");
            var defenderHost = new GameObject("defender");
            var cameraHost = new GameObject("camera", typeof(Camera));
            try
            {
                var camera = cameraHost.GetComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(standing ? 40f : 90f, yaw, 0f);
                var attacker = attackerHost.AddComponent<PrimitiveHeroView>();
                var defender = defenderHost.AddComponent<PrimitiveHeroView>();
                attacker.Configure(new HeroOccupancy(new HeroId("attacker"), new HexCoordinate(0, 0)), 1f);
                defender.Configure(new HeroOccupancy(new HeroId("defender"), new HexCoordinate(1, 0)), 1f);
                SetIdentity(attacker, "misa", TeamSide.Red);
                SetIdentity(defender, "rowenna", TeamSide.Blue);
                attacker.Standee.Refresh(camera); defender.Standee.Refresh(camera);
                Assert.That(attacker.Standee.IsStanding, Is.EqualTo(standing));
                var attackStart = attacker.transform.localPosition;
                var defendStart = defender.transform.localPosition;
                var attackerSubject = KineticSubject.ForHero(attacker.Occupancy.HeroId);
                var defenderSubject = KineticSubject.ForHero(defender.Occupancy.HeroId);
                var board = new BoardInteractionUnityView(new[] { attacker, defender }, new HexTileView[0], null);
                var view = new KineticCueUnityView(board, cameraSource: () => camera);
                var lunge = new KineticCue(1, KineticCueKind.Lunge, attackerSubject, defenderSubject,
                    subjectSpace: attacker.Coordinate, targetSpace: defender.Coordinate);
                var flash = new KineticCue(1, KineticCueKind.Flash, defenderSubject, subjectSpace: defender.Coordinate);
                view.Playback.Begin(landed ? new[] { lunge, flash } : new[] { lunge });
                view.Tick(0.10f);
                var offset = attacker.transform.localPosition - attackStart;
                Assert.That(offset.x, Is.GreaterThan(0f));
                Assert.That(offset.magnitude, Is.LessThanOrEqualTo(standing ? 0.301f : 0.601f));
                Assert.That(attacker.transform.rotation, Is.EqualTo(Quaternion.identity), "Base stays level.");
                if (standing)
                {
                    Assert.That(attacker.Standee.PanelRenderer.transform.up.x, Is.GreaterThan(0.05f));
                    Assert.That(Vector3.Distance(defender.transform.localPosition, defendStart) > 0.01f, Is.EqualTo(landed));
                    Assert.That(defender.transform.localScale, Is.EqualTo(Vector3.one), "No standee ballooning.");
                }
                else Assert.That(defender.transform.localPosition, Is.EqualTo(defendStart));

                view.Render(new KineticCueViewModel(new KineticCue[0], false, new HeroId[0], snapToAuthoritative: true));
                view.Tick(0.01f);
                attacker.Standee.Refresh(camera); defender.Standee.Refresh(camera);
                Assert.That(attacker.transform.localPosition, Is.EqualTo(attackStart));
                Assert.That(defender.transform.localPosition, Is.EqualTo(defendStart));
                if (standing)
                    Assert.That(Vector3.Dot(attacker.Standee.PanelRenderer.transform.up, Vector3.up), Is.GreaterThan(0.999f));

                // A discarded Defense card has its own flash; it must never invent an impact bump.
                view.Playback.Begin(new[] { flash });
                view.Tick(0.10f);
                Assert.That(defender.transform.localPosition, Is.EqualTo(defendStart));
            }
            finally
            {
                Object.DestroyImmediate(attackerHost); Object.DestroyImmediate(defenderHost); Object.DestroyImmediate(cameraHost);
            }
        }

        [TestCase(20f, 0f)]
        [TestCase(55f, 135f)]
        [TestCase(70f, 270f)]
        [TestCase(90f, 45f)]
        public void DefenseShieldStaysFlatOnTheBaseAndFollowsCameraHeading(float pitch, float yaw)
        {
            var host = new GameObject("flat shield");
            var cameraHost = new GameObject("view", typeof(Camera));
            try
            {
                host.transform.SetPositionAndRotation(new Vector3(3f, 0.4f, -2f), Quaternion.Euler(0f, 37f, 0f));
                var shield = HeroDefenseModifierShield.GetOrCreate(host.transform);
                shield.SetVisible(-2, 0.81f, 0.16f);
                var camera = cameraHost.GetComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                shield.RefreshFacing(camera);
                var expected = host.transform.position + Vector3.up * (0.16f + HeroDefenseModifierShield.SurfaceOffset)
                    - camera.transform.right * (0.81f * HeroDefenseModifierShield.EdgeDistanceFactor);
                Assert.That(Vector3.Distance(shield.transform.position, expected), Is.LessThan(0.001f));
                Assert.That(Vector3.Dot(shield.transform.up, Vector3.up), Is.GreaterThan(0.999f));
                Assert.That(Vector3.Dot(shield.ValueText.transform.forward, Vector3.down), Is.GreaterThan(0.999f));
                var screenUp = Vector3.ProjectOnPlane(camera.transform.up, Vector3.up).normalized;
                Assert.That(Vector3.Dot(shield.ValueText.transform.up, screenUp), Is.GreaterThan(0.999f));
                var mesh = shield.GetComponent<MeshFilter>().sharedMesh;
                var heights = mesh.vertices.Select(v => shield.transform.TransformPoint(v).y).ToArray();
                Assert.That(heights.Max() - heights.Min(), Is.LessThan(0.002f), "Only the outline/face lift separates shield vertex heights.");
                Assert.That(heights.Min(), Is.GreaterThan(host.transform.position.y + 0.16f));
                Assert.That(shield.ValueText.text, Is.EqualTo("-2"));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(cameraHost);
            }
        }

        [TestCase(TeamSide.Red)]
        [TestCase(TeamSide.Blue)]
        public void StandingAndFlatRepresentationsKeepIdentityBaseAndPicking(TeamSide team)
        {
            var host = new GameObject("standee");
            var cameraHost = new GameObject("view", typeof(Camera));
            try
            {
                var hero = host.AddComponent<PrimitiveHeroView>();
                hero.Configure(new HeroOccupancy(new HeroId("same-hero"), new HexCoordinate(0, 0)), 1f);
                SetIdentity(hero, "misa", team);
                host.AddComponent<PrimitiveHeroPointerSelector>();
                var camera = cameraHost.GetComponent<Camera>();
                var occupancy = hero.Occupancy;
                var size = host.GetComponent<MeshFilter>().sharedMesh.bounds.size;
                Assert.That(hero.Standee.HasArtwork, Is.True);
                camera.transform.rotation = Quaternion.Euler(55f, 135f, 0);
                hero.Standee.Refresh(camera);
                Assert.That(hero.Standee.IsStanding, Is.True);
                Assert.That(Vector3.Dot(host.transform.Find("Illustrated standee panel").right, camera.transform.right),
                    Is.GreaterThan(0.99f), "Portrait artwork must retain its supplied left/right orientation.");
                Assert.That(hero.SelectionHeight, Is.GreaterThan(1.8f));
                var materials = host.GetComponent<MeshRenderer>().sharedMaterials;
                Assert.That(materials[0], Is.SameAs(materials[1]), "Solid team base replaces only the printed disc.");
                Assert.That(materials[0].color, Is.EqualTo(HeroTokenArtwork.TeamRim(team)));
                hero.SetIdentityAccent(Color.yellow);
                Assert.That(materials[0].color, Is.EqualTo(HeroTokenArtwork.TeamRim(team)), "Standee bases express team, not seat accent.");
                var capsule = host.GetComponent<CapsuleCollider>();
                Assert.That(capsule.height, Is.EqualTo(hero.SelectionHeight));
                Physics.SyncTransforms();
                Assert.That(capsule.Raycast(new Ray(host.transform.position + new Vector3(0, 1f, -5f), Vector3.forward), out _, 10f), Is.True);
                hero.EnsureVisual();
                Assert.That(hero.Standee.IsStanding, Is.True, "Projection redraw must retain the representation.");
                camera.transform.rotation = Quaternion.Euler(70.1f, 0, 0);
                hero.Standee.Refresh(camera);
                Assert.That(hero.Standee.IsStanding, Is.False);
                Assert.That(host.GetComponent<MeshRenderer>().sharedMaterials[1], Is.Not.SameAs(materials[0]));
                Assert.That(hero.Occupancy, Is.EqualTo(occupancy));
                Assert.That(host.GetComponent<MeshFilter>().sharedMesh.bounds.size, Is.EqualTo(size));
            }
            finally { Object.DestroyImmediate(host); Object.DestroyImmediate(cameraHost); }
        }

        [Test]
        public void SwitchingHasAHysteresisBandAndAbsentResourcesRetainTheirFallback()
        {
            var host = new GameObject("hero");
            var cameraHost = new GameObject("view", typeof(Camera));
            try
            {
                var hero = host.AddComponent<PrimitiveHeroView>();
                SetIdentity(hero, "brogan", TeamSide.Red);
                var camera = cameraHost.GetComponent<Camera>();
                foreach (var item in new[] { (90f, false), (68f, true), (69.5f, true), (70.1f, false), (69.5f, false), (68f, true) })
                {
                    camera.transform.rotation = Quaternion.Euler(item.Item1, 0, 0);
                    hero.Standee.Refresh(camera);
                    Assert.That(hero.Standee.IsStanding, Is.EqualTo(item.Item2), item.Item1.ToString());
                }
                SetIdentity(hero, "wasp", TeamSide.Red);
                Assert.That(hero.Standee.HasArtwork, Is.False);
                Assert.That(hero.HasTokenVisual, Is.True, "Other heroes keep their regular token.");
            }
            finally { Object.DestroyImmediate(host); Object.DestroyImmediate(cameraHost); }
        }

        private static void SetIdentity(PrimitiveHeroView view, string identity, TeamSide team)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("heroContentId").stringValue = identity;
            serialized.FindProperty("teamSide").intValue = (int)team;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.EnsureVisual();
        }

        [Test]
        public void PrivatePortraitPanelsHaveTrueThicknessAndBoundedSize()
        {
            foreach (var identity in new[] { "brogan", "misa", "rowenna", "tigerclaw", "gydion", "tali" })
            {
                var data = Resources.Load<TextAsset>("Private/Megdan/Standees/" + identity);
                Assert.That(data, Is.Not.Null, identity);
                var mesh = IllustratedStandeeView.BuildPanel(JsonUtility.FromJson<IllustratedStandeeView.Outline>(data.text));
                try
                {
                    Assert.That(mesh.bounds.size.z, Is.EqualTo(IllustratedStandeeView.PanelThickness).Within(0.0001f));
                    Assert.That(mesh.bounds.size.x, Is.LessThanOrEqualTo(1.551f));
                    Assert.That(mesh.bounds.size.y, Is.LessThanOrEqualTo(1.851f));
                    Assert.That(mesh.GetTriangles(1).Length, Is.GreaterThan(100));
                    Assert.That(mesh.vertices.All(v => v.y >= 0), Is.True);
                }
                finally { Object.DestroyImmediate(mesh); }
            }
        }

        [Test]
        public void NavigationIsBoundedSuspendedAndRestorableAcrossReframes()
        {
            var host = new GameObject("camera", typeof(Camera));
            try
            {
                var camera = host.GetComponent<Camera>();
                camera.transform.SetPositionAndRotation(new Vector3(2, 60, 3), Quaternion.Euler(90f, 0, 0));
                camera.orthographic = true; camera.orthographicSize = 20f;
                var navigation = host.AddComponent<BoardCameraController>();
                bool suspended = false;
                navigation.Configure(null, () => suspended, null);
                var map = new object();
                var bounds = new Bounds(Vector3.zero, new Vector3(30, 0, 20));
                navigation.AcceptHomeFrame(map, bounds);
                navigation.Navigate(Vector2.one, new Vector2(30, 35), 2f, 1f);
                var pose = camera.transform.position;
                var rotation = camera.transform.rotation;
                Assert.That(navigation.Elevation, Is.EqualTo(55f));
                Assert.That(camera.orthographicSize, Is.LessThan(20f));
                camera.transform.SetPositionAndRotation(new Vector3(2, 60, 3), Quaternion.Euler(90f, 0, 0));
                camera.orthographicSize = 20f;
                navigation.AcceptHomeFrame(map, bounds);
                Assert.That(camera.transform.position, Is.EqualTo(pose));
                Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
                suspended = true;
                navigation.Navigate(Vector2.one, Vector2.one * 200, 100, 1);
                navigation.ResetView();
                Assert.That(camera.transform.position, Is.EqualTo(pose));
                suspended = false;
                navigation.Navigate(Vector2.one * 100, Vector2.one * 1000, 100, 100);
                Assert.That(navigation.Elevation, Is.EqualTo(BoardCameraController.MinimumPitch));
                Assert.That(camera.orthographicSize, Is.EqualTo(2.4f).Within(0.001f));
                navigation.ResetView();
                Assert.That(navigation.HasManualPose, Is.False);
                Assert.That(camera.transform.position, Is.EqualTo(new Vector3(2, 60, 3)));
                Assert.That(camera.orthographicSize, Is.EqualTo(20));
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void CreepBodyIsCircularAndThickWithoutChangingThePrintedFace()
        {
            var mesh = StandardMinionView.CreateTokenBody(0.5f, BoardPieceDepth.Standard / StandardMinionView.DefaultScale);
            try
            {
                Assert.That(mesh.bounds.size.x, Is.EqualTo(1f).Within(0.001f));
                Assert.That(mesh.bounds.size.y, Is.EqualTo(1f).Within(0.001f));
                Assert.That(mesh.bounds.size.z * StandardMinionView.DefaultScale,
                    Is.EqualTo(BoardPieceDepth.Standard - 0.001f * StandardMinionView.DefaultScale).Within(0.0001f));
                Assert.That(mesh.vertices.Max(v => v.z), Is.LessThan(0f), "The body stays beneath the untouched sprite.");
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
