using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z6 (PLAN.md's SIXTEENTH amendment: "Defeat: shrink-and-fade rather than
    /// vanish"), delivered pooling-safely per <see cref="DefeatFadeGhostView"/>'s own doc comment.
    ///
    /// <para>The five removal guards these fixtures must never disturb -
    /// <c>MinimalHexGridPlayModeTests.AttackAndRemovalOfMinionsAndHeroes</c> (its two
    /// <c>minion.gameObject.activeSelf</c> assertions and its Minion-Battle hero-removal assertion),
    /// <see cref="StandardMinionViewTests.PassiveBoardView_UsesProjectedMinionIdentityForCuePointerAndRemoval"/>,
    /// and
    /// <see cref="ConfiguredPointerSelectionTests.PassiveBoardView_HidesAnAbsentHeroAndReactivatesItAtReturnedOccupancy"/>
    /// - all assert a defeated piece's OWN <c>GameObject</c> is inactive the instant
    /// <c>BoardInteractionUnityView.Render</c>/<c>RenderMinions</c> drops it from the projection, some of
    /// them with no frame or Update in between. Every case below constructs the piece and the fade ghost
    /// the same way those guards do and re-asserts the piece's own <c>activeSelf</c> alongside the new
    /// ghost behaviour, so a regression here shows up here rather than only at Unity's own suite.</para>
    /// </summary>
    public sealed class DefeatFadeGhostTests
    {
        [Test]
        public void HeroDefeat_SpawnsANonPooledGhost_WhileTheRealHeroDeactivatesSynchronously()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var view = new BoardInteractionUnityView(new[] { hero }, Array.Empty<HexTileView>(), material);

                view.Render(BoardModel(1, new[] { occupancy }, Array.Empty<MinionOccupancy>()));
                Assert.That(hero.gameObject.activeSelf, Is.True);
                var scaleWhileDrawn = hero.transform.localScale;
                var positionWhileDrawn = hero.transform.position;

                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));

                // The removal guard itself, re-asserted: the real piece deactivates in the SAME call,
                // exactly as it did before this milestone.
                Assert.That(hero.gameObject.activeSelf, Is.False,
                    "A hero removed from authoritative occupancy must not remain visible on the board - " +
                    "the pre-existing removal guard this milestone must never weaken.");

                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(1),
                    "The removal is a genuine edge (the hero WAS drawn), so it owes exactly one ghost.");
                var ghost = view.HeroDefeatGhostFor(hero);
                Assert.That(ghost, Is.Not.Null);
                Assert.That(ghost.IsFading, Is.True);
                Assert.That(ghost.gameObject, Is.Not.SameAs(hero.gameObject),
                    "The fade plays on a spawned echo, never the pooled hero GameObject itself.");
                Assert.That(ghost.GetComponent<PrimitiveHeroView>(), Is.Null,
                    "The ghost carries no identity component - it must be invisible to every " +
                    "FindObjectsByType<PrimitiveHeroView>() query the test suite runs.");
                Assert.That(ghost.GetComponent<Collider>(), Is.Null,
                    "The ghost must never intercept a click meant for the tile or piece beneath it.");
                Assert.That(ghost.transform.position, Is.EqualTo(positionWhileDrawn),
                    "The echo starts exactly where the real piece stood the instant it left.");
                Assert.That(ghost.transform.localScale, Is.EqualTo(scaleWhileDrawn),
                    "Progress zero draws the piece at its own resting scale, not already shrunk.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void HeroDefeatGhost_ShrinksAcrossItsDuration_AndReleasesExactlyOnceFinished()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var view = new BoardInteractionUnityView(new[] { hero }, Array.Empty<HexTileView>(), material);
                view.Render(BoardModel(1, new[] { occupancy }, Array.Empty<MinionOccupancy>()));
                var restingScale = hero.transform.localScale;
                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));
                var ghost = view.HeroDefeatGhostFor(hero);

                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds / 2f);

                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(1),
                    "Halfway through its own duration, the ghost has not finished.");
                Assert.That(ghost.Progress01, Is.EqualTo(0.5f).Within(0.001f));
                var midScale = ghost.transform.localScale.x;
                Assert.That(midScale, Is.LessThan(restingScale.x),
                    "The echo has visibly shrunk by the midpoint.");
                Assert.That(midScale, Is.GreaterThan(restingScale.x * DefeatFadeGhostView.ShrinkToScale),
                    "and has not yet reached its final shrunk scale.");

                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds);

                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(0),
                    "The ghost releases itself once its own duration has fully elapsed.");
                Assert.That(view.HeroDefeatGhostFor(hero), Is.Null);
                Assert.That(ghost.IsFading, Is.False);
                Assert.That(ghost.gameObject.activeSelf, Is.False,
                    "A finished ghost is deactivated (pooled), never left drawn at its shrunk scale.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void HeroDefeatGhost_WaitsForTheStrikeWithoutKeepingTheRealPieceActive()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var hero = CreateHero(objects, occupancy);
                var held = true;
                var view = new BoardInteractionUnityView(new[] { hero }, Array.Empty<HexTileView>(), material)
                {
                    DeferDefeatFades = () => held
                };
                view.Render(BoardModel(1, new[] { occupancy }, Array.Empty<MinionOccupancy>()));
                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));
                var ghost = view.HeroDefeatGhostFor(hero);
                view.AdvanceDefeatFades(1f);
                Assert.That(hero.gameObject.activeSelf, Is.False);
                Assert.That(ghost.gameObject.activeSelf, Is.True);
                Assert.That(ghost.Progress01, Is.EqualTo(0f));
                held = false;
                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds / 2f);
                Assert.That(ghost.Progress01, Is.EqualTo(0.5f).Within(0.001f));
                held = true;
                view.Render(BoardModel(3, new[] { occupancy }, Array.Empty<MinionOccupancy>()));
                Assert.That(view.ActiveDefeatFadeCount, Is.Zero, "A restored hero cancels even a held echo.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void HeroDefeatGhost_IsCancelledImmediately_WhenTheHeroReappearsBeforeItsFadeFinishes()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var occupancy = new HeroOccupancy(new HeroId("red-hero"), new HexCoordinate(0, 0));
                var returned = new HeroOccupancy(occupancy.HeroId, new HexCoordinate(2, -1));
                var hero = CreateHero(objects, occupancy);
                var view = new BoardInteractionUnityView(new[] { hero }, Array.Empty<HexTileView>(), material);
                view.Render(BoardModel(1, new[] { occupancy }, Array.Empty<MinionOccupancy>()));
                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));
                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(1));
                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds * 0.25f);

                // Undo, restore, or Respawn - a real authoritative projection putting the hero back
                // before its own decorative fade finished on its own.
                view.Render(BoardModel(3, new[] { returned }, Array.Empty<MinionOccupancy>()));

                Assert.That(hero.gameObject.activeSelf, Is.True,
                    "Respawn, Undo, and restore must be able to reactivate the retained hero view - " +
                    "the pre-existing removal guard this milestone must never weaken.");
                Assert.That(hero.Occupancy, Is.EqualTo(returned));
                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(0),
                    "A piece visible again must never carry a stale echo describing its own removal.");
                Assert.That(view.HeroDefeatGhostFor(hero), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        [Test]
        public void MinionDefeat_SpawnsANonPooledGhost_WhileTheRealMinionDeactivatesSynchronously()
        {
            var minionObject = new GameObject("Projected Minion");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var state = MinionState("fading-minion", new HexCoordinate(1, 0));
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(), Array.Empty<HexTileView>(), material, new[] { minion });

                view.Render(BoardModel(1, Array.Empty<HeroOccupancy>(), new[] { state }));
                Assert.That(minion.gameObject.activeSelf, Is.True);
                var restingScale = minion.transform.localScale;
                var positionWhileDrawn = minion.transform.position;
                var spriteWhileDrawn = minion.GetComponent<SpriteRenderer>().sprite;

                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));

                Assert.That(minion.gameObject.activeSelf, Is.False,
                    "The pre-existing removal guard this milestone must never weaken.");
                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(1));
                var ghost = view.MinionDefeatGhostFor(minion);
                Assert.That(ghost, Is.Not.Null);
                Assert.That(ghost.IsFading, Is.True);
                Assert.That(ghost.gameObject, Is.Not.SameAs(minion.gameObject));
                Assert.That(ghost.GetComponent<StandardMinionView>(), Is.Null,
                    "The ghost carries no identity component - invisible to every " +
                    "FindObjectsByType<StandardMinionView>() query the test suite runs.");
                Assert.That(ghost.GetComponent<Collider>(), Is.Null);
                Assert.That(ghost.GetComponent<SpriteRenderer>().sprite, Is.SameAs(spriteWhileDrawn),
                    "The echo shows the same sprite the real minion was drawing.");
                Assert.That(ghost.transform.position, Is.EqualTo(positionWhileDrawn));
                Assert.That(ghost.transform.localScale, Is.EqualTo(restingScale));
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(minionObject);
            }
        }

        [Test]
        public void HeavyMinionDefeat_HoldsThePushedWaveUntilAttackGatedDeathFinishes()
        {
            var heavyObject = new GameObject("Defeated heavy minion");
            var waveObject = new GameObject("Pushed wave minion");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var heavy = MinionState("heavy", new HexCoordinate(0, 0), StandardMinionType.Heavy);
                var before = MinionState("wave", new HexCoordinate(1, 0));
                var after = MinionState("wave", new HexCoordinate(2, 0));
                var heavyView = heavyObject.AddComponent<StandardMinionView>();
                var waveView = waveObject.AddComponent<StandardMinionView>();
                heavyView.Configure(heavy, HexWorldLayout.DefaultOuterRadius);
                waveView.Configure(before, HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(), Array.Empty<HexTileView>(), material,
                    new[] { heavyView, waveView });
                var attackStillPlaying = true;
                view.DeferDefeatFades = () => attackStillPlaying;

                view.Render(BoardModel(1, Array.Empty<HeroOccupancy>(), new[] { heavy, before }));
                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), new[] { after }));

                Assert.That(heavyView.gameObject.activeSelf, Is.False,
                    "The authoritative defeat still removes the real heavy minion immediately.");
                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(1));
                Assert.That(waveView.Occupancy.Coordinate, Is.EqualTo(before.Coordinate),
                    "The pushed wave must not move before the heavy minion's death is shown.");

                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds);
                Assert.That(waveView.Occupancy.Coordinate, Is.EqualTo(before.Coordinate),
                    "The attack gate holds both the death and the consequent wave movement.");

                attackStillPlaying = false;
                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds);

                Assert.That(view.ActiveDefeatFadeCount, Is.Zero);
                Assert.That(waveView.Occupancy.Coordinate, Is.EqualTo(after.Coordinate),
                    "After the heavy minion dies, the view catches up to the authoritative Lane Push.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(heavyObject);
                Object.DestroyImmediate(waveObject);
            }
        }
        [Test]
        public void MinionDefeatGhost_FadesItsOwnOpacity_WithoutMutatingTheRealMinionsSpriteRenderer()
        {
            var minionObject = new GameObject("Projected Minion");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var state = MinionState("fading-minion", new HexCoordinate(1, 0));
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var minionRenderer = minion.GetComponent<SpriteRenderer>();
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(), Array.Empty<HexTileView>(), material, new[] { minion });
                view.Render(BoardModel(1, Array.Empty<HeroOccupancy>(), new[] { state }));
                var restingColor = minionRenderer.color;

                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));
                var ghost = view.MinionDefeatGhostFor(minion);
                var ghostRenderer = ghost.GetComponent<SpriteRenderer>();
                var opaqueAlpha = ghostRenderer.color.a;

                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds / 2f);

                Assert.That(ghostRenderer.color.a, Is.LessThan(opaqueAlpha),
                    "The ghost's own SpriteRenderer fades its opacity across the duration.");
                Assert.That(minionRenderer.color, Is.EqualTo(restingColor),
                    "The real (inactive, pooled) minion's own SpriteRenderer is never touched by the " +
                    "fade - it is restored exactly as drawn the moment it reappears.");

                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds);
                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(minionObject);
            }
        }

        [Test]
        public void LastMinionDefeat_ThatRespawnsTheSameIdentity_DiesBeforeTheNewWaveAppears()
        {
            var targetObject = new GameObject("Last defeated minion");
            var waveObject = new GameObject("Pushed wave minion");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var targetBefore = MinionState("last", new HexCoordinate(0, 0));
                var targetRespawned = MinionState("last", new HexCoordinate(3, 0));
                var waveBefore = MinionState("wave", new HexCoordinate(1, 0));
                var waveAfter = MinionState("wave", new HexCoordinate(2, 0));
                var targetView = targetObject.AddComponent<StandardMinionView>();
                var waveView = waveObject.AddComponent<StandardMinionView>();
                targetView.Configure(targetBefore, HexWorldLayout.DefaultOuterRadius);
                waveView.Configure(waveBefore, HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(), Array.Empty<HexTileView>(), material,
                    new[] { targetView, waveView });
                var attackStillPlaying = true;
                view.DeferDefeatFades = () => attackStillPlaying;
                view.IsDefeatedMinionAwaitingPresentation = id => id.Equals(targetBefore.MinionId);

                view.Render(BoardModel(
                    1, Array.Empty<HeroOccupancy>(), new[] { targetBefore, waveBefore }));
                var oldPosition = targetView.transform.position;
                view.Render(BoardModel(
                    2, Array.Empty<HeroOccupancy>(), new[] { targetRespawned, waveAfter }));

                Assert.That(targetView.gameObject.activeSelf, Is.False,
                    "The respawned authoritative miniature must not appear in the new zone yet.");
                var ghost = view.MinionDefeatGhostFor(targetView);
                Assert.That(ghost, Is.Not.Null,
                    "A same-identity remove+respawn still owes the killed minion its death echo.");
                Assert.That(ghost.transform.position, Is.EqualTo(oldPosition),
                    "The strike and death remain at the space where the minion was actually killed.");
                Assert.That(waveView.Occupancy.Coordinate, Is.EqualTo(waveBefore.Coordinate),
                    "The rest of the pushed wave also waits behind the causal death.");

                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds);
                Assert.That(ghost.Progress01, Is.Zero,
                    "The attack gate holds the old-space death echo until the strike starts.");
                Assert.That(targetView.gameObject.activeSelf, Is.False);

                attackStillPlaying = false;
                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds);

                Assert.That(view.ActiveDefeatFadeCount, Is.Zero);
                Assert.That(targetView.gameObject.activeSelf, Is.True);
                Assert.That(targetView.Occupancy.Coordinate, Is.EqualTo(targetRespawned.Coordinate),
                    "Only after the death finishes may the reused miniature appear at its respawn.");
                Assert.That(waveView.Occupancy.Coordinate, Is.EqualTo(waveAfter.Coordinate),
                    "The consequent Lane Push catches up in the same release.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(waveObject);
            }
        }

        [Test]
        public void SameIdentityRespawnHold_UndoBackToTheOldSpaceCancelsTheDeathAndRespawn()
        {
            var targetObject = new GameObject("Last defeated minion");
            var waveObject = new GameObject("Pushed wave minion");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var targetBefore = MinionState("last", new HexCoordinate(0, 0));
                var targetRespawned = MinionState("last", new HexCoordinate(3, 0));
                var waveBefore = MinionState("wave", new HexCoordinate(1, 0));
                var waveAfter = MinionState("wave", new HexCoordinate(2, 0));
                var targetView = targetObject.AddComponent<StandardMinionView>();
                var waveView = waveObject.AddComponent<StandardMinionView>();
                targetView.Configure(targetBefore, HexWorldLayout.DefaultOuterRadius);
                waveView.Configure(waveBefore, HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(), Array.Empty<HexTileView>(), material,
                    new[] { targetView, waveView })
                {
                    DeferDefeatFades = () => true,
                    IsDefeatedMinionAwaitingPresentation = id => id.Equals(targetBefore.MinionId)
                };

                view.Render(BoardModel(
                    1, Array.Empty<HeroOccupancy>(), new[] { targetBefore, waveBefore }));
                view.Render(BoardModel(
                    2, Array.Empty<HeroOccupancy>(), new[] { targetRespawned, waveAfter }));
                Assert.That(targetView.gameObject.activeSelf, Is.False);
                Assert.That(view.MinionDefeatGhostFor(targetView), Is.Not.Null);

                view.Render(BoardModel(
                    3, Array.Empty<HeroOccupancy>(), new[] { targetBefore, waveBefore }));

                Assert.That(view.MinionDefeatGhostFor(targetView), Is.Null,
                    "Undo/restore to the old board truth cancels the decorative death immediately.");
                Assert.That(targetView.gameObject.activeSelf, Is.True);
                Assert.That(targetView.Occupancy.Coordinate, Is.EqualTo(targetBefore.Coordinate));
                Assert.That(waveView.Occupancy.Coordinate, Is.EqualTo(waveBefore.Coordinate),
                    "No deferred pushed-wave position survives the reverted projection.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(waveObject);
            }
        }

        [Test]
        public void MinionDefeatGhost_IsCancelledImmediately_WhenTheMinionReappearsBeforeItsFadeFinishes()
        {
            var minionObject = new GameObject("Projected Minion");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var state = MinionState("fading-minion", new HexCoordinate(1, 0));
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(), Array.Empty<HexTileView>(), material, new[] { minion });
                view.Render(BoardModel(1, Array.Empty<HeroOccupancy>(), new[] { state }));
                view.Render(BoardModel(2, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));
                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds * 0.25f);

                view.Render(BoardModel(3, Array.Empty<HeroOccupancy>(), new[] { state }));

                Assert.That(minion.gameObject.activeSelf, Is.True,
                    "The pre-existing removal guard this milestone must never weaken.");
                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(0));
                Assert.That(view.MinionDefeatGhostFor(minion), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(minionObject);
            }
        }

        [Test]
        public void DefeatFadeGhosts_ArePooled_ReusingTheSameGameObjectAcrossConsecutiveDefeats()
        {
            var objects = new List<GameObject>();
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var first = new HeroOccupancy(new HeroId("red-hero-a"), new HexCoordinate(0, 0));
                var second = new HeroOccupancy(new HeroId("red-hero-b"), new HexCoordinate(1, 0));
                var heroA = CreateHero(objects, first);
                var heroB = CreateHero(objects, second);
                var view = new BoardInteractionUnityView(
                    new[] { heroA, heroB }, Array.Empty<HexTileView>(), material);

                view.Render(BoardModel(1, new[] { first, second }, Array.Empty<MinionOccupancy>()));
                view.Render(BoardModel(2, new[] { second }, Array.Empty<MinionOccupancy>()));
                var ghostA = view.HeroDefeatGhostFor(heroA);
                Assert.That(ghostA, Is.Not.Null);

                view.AdvanceDefeatFades(DefeatFadeGhostView.DefaultFadeSeconds);
                Assert.That(view.ActiveDefeatFadeCount, Is.EqualTo(0),
                    "The first ghost has finished and released itself back to the free pool.");

                view.Render(BoardModel(3, Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>()));
                var ghostB = view.HeroDefeatGhostFor(heroB);

                Assert.That(ghostB, Is.SameAs(ghostA),
                    "A released ghost is reused for the next defeat rather than instantiating a new " +
                    "GameObject every time - the same pooling discipline every other piece in this file " +
                    "already follows.");
            }
            finally
            {
                Object.DestroyImmediate(material);
                DestroyAll(objects);
            }
        }

        private static BoardInteractionViewModel BoardModel(
            long version,
            IReadOnlyList<HeroOccupancy> heroes,
            IReadOnlyList<MinionOccupancy> minions)
        {
            return new BoardInteractionViewModel(
                version,
                heroes,
                minions,
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default);
        }

        private static PrimitiveHeroView CreateHero(ICollection<GameObject> objects, HeroOccupancy occupancy)
        {
            var gameObject = new GameObject($"Hero {occupancy.HeroId}");
            objects.Add(gameObject);
            var hero = gameObject.AddComponent<PrimitiveHeroView>();
            hero.Configure(occupancy, HexWorldLayout.DefaultOuterRadius);
            // Milestone 8.35z9 (integration). Found by the first complete Edit Mode run this feature
            // ever had - the authoring worktree could not launch Unity - and it was the FIXTURE that
            // was wrong, not the code. `Configure` alone leaves `MeshFilter.sharedMesh` null, because
            // `BoardInteractionUnityView.Render` never builds a hero's visual: the scene composition
            // root does, through `EnsureVisual`. `SpawnHeroDefeatGhost` then declined to echo a piece
            // with no mesh - which is CORRECT and deliberate, its own comment saying a missing visual
            // costs the flourish and nothing else - so all four hero cases measured zero ghosts
            // against a hero the real board never has. The minion cases passed throughout, because
            // `StandardMinionView` builds its own renderer. Building the visual here is what makes
            // this fixture a board piece rather than a bare component.
            hero.EnsureVisual();
            return hero;
        }

        private static MinionOccupancy MinionState(
            string minionId,
            HexCoordinate coordinate,
            StandardMinionType type = StandardMinionType.Melee) =>
            MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                new MinionId(minionId),
                new TeamId("blue-team"),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(new MapId("test-map"), new SpawnPointId(minionId + "-spawn")),
                new ConfiguredSpaceReference(new MapId("test-map"), new SpaceId("q1_r0")),
                coordinate));

        private static void DestroyAll(IEnumerable<GameObject> objects)
        {
            foreach (var value in objects)
            {
                if (value != null)
                {
                    Object.DestroyImmediate(value);
                }
            }
        }
    }
}
