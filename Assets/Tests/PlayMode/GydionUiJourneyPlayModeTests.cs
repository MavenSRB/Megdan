using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.30j. Real pointer and control journeys for Gydion the Archwizard, on boards measured
    /// headlessly before any was written (scenes 142 and 143, driven with DirectorCommands): Gydion
    /// stands on (-3, 1) and Wasp on (-2, 2), two spaces away; his own Blue melee minion stands on
    /// (-3, 2), beside both, and Red's ranged minion on (-4, 2), beside him. Red's melee minions stand on
    /// (-1, 0), (-1, -1) and (-4, 4), the last exactly three spaces from him. Wasp's lowest card is Stop
    /// Projectiles (3), so every front of his (4 to 13) acts first; her Magnetic Dagger (12) is her basic
    /// Attack and Shock (8) her non-basic one.
    ///
    /// <para>Where a journey needs spells already removed, a Wish count or a spent Life, it stages them
    /// through the authorities' own transitions on a restored snapshot (<see cref="StageTheBook"/>):
    /// 9.30b's faceup removal, 9.30g's tally, p.13's Life spend - no sandbox seam stages a spellbook.
    /// Every journey reads what its answer changed: the SPELLBOOK chip, the faceup pile on every seat,
    /// the board, a hand, coins, the Life hearts, the plaque's Wish count or the victory screen.</para>
    /// </summary>
    public sealed class GydionUiJourneyPlayModeTests
    {
        internal static readonly HexCoordinate GydionSpace = new HexCoordinate(-3, 1);
        internal static readonly HexCoordinate WaspSpace = new HexCoordinate(-2, 2);

        /// <summary>Beside him, and beside his Blue melee minion on (-3, 2).</summary>
        internal static readonly HexCoordinate WaspBesideHim = new HexCoordinate(-2, 1);

        internal static readonly MinionId BlueMelee = new MinionId("blue-melee-01");
        internal static readonly MinionId RedRanged = new MinionId("red-ranged-01");
        internal static readonly MinionId RedMeleeInTheMiddle = new MinionId("red-melee-02");
        internal static readonly MinionId RedMeleeThreeAway = new MinionId("red-melee-04");

        internal const string HisName = "Gydion the Archwizard";

        // -------------------------------------------------------------------------------------------
        // Reading the board and the book
        // -------------------------------------------------------------------------------------------

        internal static HeroId Gydion(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == GydionCardContent.ContentSetId).HeroId;

        internal static HeroId Wasp(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == WaspCardContent.ContentSetId).HeroId;

        internal static SeatId SeatOf(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Host.Seats.Seats.Single(seat => seat.HeroId.Equals(hero)).Id;

        internal static PrimitiveHeroPointerInput Pointer() => Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

        internal static bool OnBoard(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Heroes.Any(value => value.HeroId.Equals(hero));

        internal static HexCoordinate SpaceOf(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        internal static bool MinionStands(ConfiguredMatchSceneRuntime runtime, MinionId minion) =>
            runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(minion));

        internal static HexCoordinate MinionAt(ConfiguredMatchSceneRuntime runtime, MinionId minion) =>
            runtime.ReadModel.Minions.Single(value => value.MinionId.Equals(minion)).Coordinate;

        /// <summary>His one instance of a spell, wherever it lies - in the book or in the faceup pile.</summary>
        internal static CardInstance Spell(ConfiguredMatchSceneRuntime runtime, CardDefinitionId spell) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(Gydion(runtime)) &&
                card.DefinitionReference.DefinitionId.Equals(spell));

        internal static CardInstance Card(ConfiguredMatchSceneRuntime runtime, HeroId owner, CardDefinitionId face) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        internal static int HandCount(ConfiguredMatchSceneRuntime runtime, HeroId owner) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);

        internal static int DiscardCount(ConfiguredMatchSceneRuntime runtime, HeroId owner) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.DiscardPile);

        internal static int InTheBook(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(Gydion(runtime)) &&
                card.State.Location == CardLocation.Spellbook);

        /// <summary>
        /// The plaque's "SPELLBOOK n | m" chip as the player reads it: the one shown chip inside an
        /// items strip (only a hero with a spellbook draws one).
        /// </summary>
        internal static string Chip(MatchSceneCompositionRoot root)
        {
            var chips = UiPlayer.ItemStripDescendants(root.Hud).OfType<Button>()
                .Where(element => element.name == "gd-spellbook-chip" && UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(chips, Has.Length.EqualTo(1), "His plaque draws one SPELLBOOK chip in its items strip.");
            return chips[0].text;
        }

        /// <summary>
        /// Every seat - his own and every other one - names each spell faceup in his removed pile, and
        /// that seat's SPELLBOOK reading draws it as a face: milestone 8.40k's public reveal, read on
        /// the seats that did not choose it.
        /// </summary>
        internal static void AssertFaceupOnEverySeat(ConfiguredMatchSceneRuntime runtime, params CardDefinitionId[] spells)
        {
            var gydion = Gydion(runtime);
            foreach (var seat in runtime.Host.Seats.Seats)
            {
                var view = runtime.Host.ProjectionForSeat(seat.Id);
                var book = SpellbookPresentation.ForHero(view, gydion, HisName);
                Assert.That(book, Is.Not.Null, seat.HeroId.Value + " reads his spellbook.");
                foreach (var spell in spells)
                {
                    Assert.That(view.Cards.Any(card => card.OwnerId.Equals(gydion) &&
                            card.DefinitionReference.DefinitionId.Equals(spell) &&
                            card.State.Location == CardLocation.RemovedFromSpellbook &&
                            card.State.FaceOrientation == CardFaceOrientation.FaceUp), Is.True,
                        spell.Value + " is faceup in his removed pile on " + seat.HeroId.Value + "'s seat.");
                    Assert.That(book.RemovedFaces.Select(face => face.DefinitionReference.DefinitionId),
                        Has.Member(spell), spell.Value + " is drawn faceup in " + seat.HeroId.Value + "'s SPELLBOOK.");
                }
            }
        }

        /// <summary>
        /// Every hero stands drawn where the projection puts it, his plaque draws its chip, and nothing
        /// faulted.
        /// </summary>
        internal static void Healthy(ConfiguredMatchSceneRuntime runtime)
        {
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            foreach (var row in runtime.Heroes)
            {
                Assert.That(HeroView(row.HeroId).Coordinate, Is.EqualTo(row.Coordinate), row.ToString());
            }

            Assert.That(runtime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(Gydion(runtime))).Spellbook,
                Is.Not.Null, "His plaque reads his spellbook.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static HeroId? cachedGydion;
        private static HeroId? cachedWasp;
        private static (MinionId Id, HexCoordinate At)[] cachedMinions;

        /// <summary>
        /// Loads <paramref name="scene"/> staged: Gydion on (-3, 1) holding <paramref name="hand"/> (the
        /// rest of his p.6 hand in his discard), Wasp where asked holding <paramref name="waspHand"/> or
        /// her own; a minion on either space is cleared. The scene's hero ids and minion layout are read
        /// once and kept, so a staged load costs one scene load.
        /// </summary>
        internal static IEnumerator Stage(SceneCardContent scene, CardDefinitionId[] hand,
            HexCoordinate? wasp = null, CardDefinitionId[] waspHand = null, int? coins = null,
            SeatId? seat = null)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            if (!cachedGydion.HasValue)
            {
                yield return LoadStagedScene(scene);
                var plain = Root().Runtime;
                cachedGydion = Gydion(plain);
                cachedWasp = Wasp(plain);
                cachedMinions = plain.ReadModel.Minions.Select(value => (value.MinionId, value.Coordinate)).ToArray();
            }

            var waspAt = wasp ?? WaspSpace;
            var occupied = new[] { GydionSpace, waspAt };
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(cachedGydion.Value, GydionSpace, coins: coins, hand: hand),
                    new SandboxHeroStaging(cachedWasp.Value, waspAt, hand: waspHand)
                },
                removedMinions: cachedMinions.Where(value => occupied.Contains(value.At)).Select(value => value.Id));
            MatchSceneCompositionRoot.NextComposedSeat = seat;
            yield return LoadStagedScene(scene);
            Assert.That(Gydion(Root().Runtime), Is.EqualTo(cachedGydion.Value));
            Assert.That(SpaceOf(Root().Runtime, cachedGydion.Value), Is.EqualTo(GydionSpace));
        }

        /// <summary>
        /// Stages his spellbook and his team's standing through the authorities' own transitions, on the
        /// match's own restored snapshot: each spell removed faceup (9.30b), the Wish count advanced once
        /// per Wish already cast (9.30g), a Life counter spent (p.13). The restore begins a fresh
        /// history, so a journey staging here asks nothing of Undo.
        /// </summary>
        internal static void StageTheBook(ConfiguredMatchSceneRuntime runtime,
            IEnumerable<CardDefinitionId> removed, int wishesCast = 0, int spentLives = 0)
        {
            var gydion = Gydion(runtime);
            var payload = runtime.CaptureSnapshot();
            var authoritative = RuleEventLogSnapshotEnvelope.TryDeserialize(payload, out var envelope)
                ? envelope.AuthoritativePayload
                : payload;
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                runtime.ContentCatalogs, authoritative, runtime.InitialState.Scenario.CardDefinitions,
                runtime.InitialState.AvailableMaps, runtime.InitialState.Setup.Minions);
            foreach (var spell in removed)
            {
                var card = restored.Cards.Single(value => value.OwnerId.Equals(gydion) &&
                    value.DefinitionReference.DefinitionId.Equals(spell));
                Assert.That(CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(card), Is.True,
                    "The staged removal is 9.30b's own transition: " + spell.Value);
            }

            for (var wish = 0; wish < wishesCast; wish++)
            {
                Assert.That(restored.Round.TryAdvanceTeamVictoryTally(gydion, GydionCardContent.WishesToWin), Is.True,
                    "The staged Wish count is the round's own.");
            }

            if (spentLives > 0)
            {
                var team = restored.Participants.GetPlayerForHero(gydion).TeamId;
                Assert.That(restored.Round.TeamLives.TrySpend(team, spentLives).Spend.Spent, Is.EqualTo(spentLives),
                    "The staged spent Life is p.13's own spend.");
            }

            Assert.That(runtime.RestoreSnapshot(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board)),
                Is.True, runtime.LastOperationDetail);
        }

        // -------------------------------------------------------------------------------------------
        // Steps
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Commits <paramref name="front"/> from his tray and every other seat's lowest card through
        /// theirs, waits for his turn and starts the front's Skill from its control.
        /// </summary>
        internal static IEnumerator Begin(MatchSceneCompositionRoot root, CardDefinitionId front)
        {
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            yield return CommitEverySeat(root, gydion, NameOf(runtime, front));
            yield return GarrusUiJourneyTests.WaitForActor(root, gydion);
            yield return PointTrayAt(root, gydion);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;
        }

        /// <summary>The decision window's option drawn as <paramref name="spell"/>'s face, or null.</summary>
        internal static DecisionWindowControlViewModel SpellOption(ConfiguredMatchSceneRuntime runtime, CardDefinitionId spell) =>
            runtime.DecisionWindowView.Model?.Controls.FirstOrDefault(control =>
                control.Spell != null && control.Spell.DefinitionReference.DefinitionId.Equals(spell));

        /// <summary>
        /// Clicks <paramref name="spell"/>'s face where the decision window draws it (9.30h's
        /// <c>gd-spell-option</c>) and proves the answer was taken: the face is gone from the question.
        /// </summary>
        internal static IEnumerator PickSpellFace(MatchSceneCompositionRoot root, CardDefinitionId spell)
        {
            var runtime = root.Runtime;
            var option = SpellOption(runtime, spell);
            Assert.That(option, Is.Not.Null, spell.Value + " is drawn as a face in the decision window. " +
                UiPlayer.Describe(root.Hud) + " " + Remaining(runtime));
            Assert.That(option.IsAnswerable, Is.True, spell.Value + " can be picked.");
            var element = runtime.DecisionWindowView.ElementFor(option);
            Assert.That(element?.name, Is.EqualTo("gd-spell-option"), "The answer is the spell's own face.");
            Assert.That(UiPlayer.Descendants(element).Any(child => child.name == "gd-spell-face"), Is.True);
            var version = runtime.Projection.Version;
            UiPlayer.ClickDecisionControl(runtime, root.Hud, option);
            yield return null;
            yield return null;
            Assert.That(runtime.Projection.Version, Is.GreaterThan(version), "The face's click was an accepted answer.");
        }

        /// <summary>
        /// Answers one candidate of the standing effect choice where a player does - the piece, the
        /// space, or a card face - confirming it when it still stands, and proves the consumed input is
        /// stale.
        /// </summary>
        internal static IEnumerator Pick(MatchSceneCompositionRoot root, Func<EffectValue, bool> which, string why)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, why + " " + Remaining(runtime));
            var stale = choice.SelectInvocations.FirstOrDefault(value => which(value.Candidate.Value));
            Assert.That(stale, Is.Not.Null, why + " " + Remaining(runtime));
            var node = choice.NodeId;
            var value = stale.Candidate.Value;
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
            else
            {
                var at = value.Kind == EffectValueKind.Hero ? HeroView(value.HeroId).transform.position
                    : value.Kind == EffectValueKind.Minion ? MinionView(value.MinionId).transform.position
                    : value.Kind == EffectValueKind.Coordinate ? Tile(value.Coordinate).transform.position
                    : throw new InvalidOperationException("No board answer for " + value.Kind);
                Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, at), Is.True,
                    why + " " + UiPlayer.DescribeBoardRay(Pointer(), at));
                yield return null;
            }

            yield return ConfirmIfStillStanding(root, node);
            var after = runtime.CaptureSnapshot();
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(stale)), Is.False,
                "The consumed input is stale.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after), "Stale input is atomic.");
        }

        /// <summary>
        /// The first of <paramref name="spaces"/> a player can click: on screen and not under the HUD,
        /// preferring one no hero piece is drawn in front of. Since 8.35z48 a piece in front of a tile
        /// claims the click only when that hero is itself an answer, so an occluded tile is the
        /// fallback rather than a failure.
        /// </summary>
        internal static HexCoordinate ReachableSpace(MatchSceneCompositionRoot root, IEnumerable<HexCoordinate> spaces)
        {
            var camera = Pointer().EventCamera;
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>()
                .Select(view => view.GetComponent<CapsuleCollider>())
                .Where(capsule => capsule != null)
                .ToArray();
            var onScreen = new List<HexCoordinate>();
            foreach (var space in spaces)
            {
                var screen = camera.WorldToScreenPoint(Tile(space).transform.position);
                if (screen.z <= 0f || screen.x < 4f || screen.y < 4f ||
                    screen.x > Screen.width - 4f || screen.y > Screen.height - 4f ||
                    root.Hud.BlocksPointerAt(new Vector2(screen.x, screen.y)))
                {
                    continue;
                }

                onScreen.Add(space);
                var ray = camera.ScreenPointToRay(new Vector2(screen.x, screen.y));
                if (!heroes.Any(capsule => capsule.Raycast(ray, out _, 500f)))
                {
                    return space;
                }
            }

            Assert.That(onScreen, Is.Not.Empty, "No offered space can be clicked: " + string.Join(", ", spaces));
            return onScreen[0];
        }

        /// <summary>Clicks a hero's piece to name it as the standing Attack's target.</summary>
        internal static IEnumerator TargetHero(MatchSceneCompositionRoot root, HeroId hero)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetHeroIds, Has.Member(hero), Remaining(runtime));
            var at = HeroView(hero).transform.position;
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, at), Is.True, UiPlayer.DescribeBoardRay(Pointer(), at));
            yield return null;
        }

        /// <summary>Clicks a minion's piece to name it as the standing Attack's target.</summary>
        internal static IEnumerator TargetMinion(MatchSceneCompositionRoot root, MinionId minion)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetMinionIds, Has.Member(minion), Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, MinionView(minion).transform.position), Is.True);
            yield return null;
        }

        /// <summary>
        /// Commits both seats' named cards headlessly - the commitment is setup here, its tray journey is
        /// every front's own - so the enemy's card decides who acts first.
        /// </summary>
        internal static void CommitBoth(ConfiguredMatchSceneRuntime runtime, CardDefinitionId his, CardDefinitionId hers)
        {
            UiPlayer.EndAnyTurnInProgress(runtime);
            Commit(runtime, Gydion(runtime), his);
            Commit(runtime, Wasp(runtime), hers);
        }

        /// <summary>Wasp, acting first, starts her Attack through its control.</summary>
        internal static IEnumerator WaspStartsHerAttack(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(Wasp(runtime)), Remaining(runtime));
            yield return PointTrayAt(root, Wasp(runtime));
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
        }

        // -------------------------------------------------------------------------------------------
        // The cast, the refill, the counting spells and the Familiar
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator CantripCastsMagicMissileFromItsFaceAndTheDefenderSeesTheSpell()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.Cantrip, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            var wasp = Wasp(runtime);
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"), "All twenty-two spells start in the book (GY-Q1).");
            yield return Begin(root, GydionCardContent.Cantrip);

            // Approved mockup A: the played card, then the three listed spells as faces.
            var window = runtime.DecisionWindowView.Model;
            Assert.That(window?.Eyebrow, Is.EqualTo(SpellbookPresentation.SpellChoiceEyebrow), Remaining(runtime));
            Assert.That(window.Controls.Where(control => control.Spell != null)
                    .Select(control => control.Spell.DefinitionReference.DefinitionId),
                Is.EqualTo(new[] { GydionCardContent.MagicMissile, GydionCardContent.ExpeditiousRetreat, GydionCardContent.ShockingGrasp }),
                "Cantrip's three spells, in printed order.");
            Assert.That(window.ContextCards.Single().Card.DisplayName, Is.EqualTo("Cantrip"));
            yield return PickSpellFace(root, GydionCardContent.MagicMissile);

            Assert.That(Spell(runtime, GydionCardContent.MagicMissile).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook),
                "\"As the spell is cast, remove it from the spellbook faceup.\"");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 21 | 1"), "The chip counts one out of the book.");
            AssertFaceupOnEverySeat(runtime, GydionCardContent.MagicMissile);

            // Magic Missile is performed by him: a unit in range 3 and not adjacent - Wasp, two away.
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetHeroIds, Is.EqualTo(new[] { wasp }), Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Has.No.Member(RedRanged),
                "Red's ranged minion stands beside him, so it is not a target.");
            yield return TargetHero(root, wasp);

            // The defender's window names and shows the SPELL, not the card that cast it.
            var defense = runtime.DecisionWindowView.Model;
            Assert.That(defense?.Family, Is.EqualTo(DecisionWindowFamily.Defense), Remaining(runtime));
            Assert.That(defense.Title, Does.Contain("MAGIC MISSILE"));
            Assert.That(defense.ContextCards.Single().Card.DisplayName, Is.EqualTo("Magic Missile"));
            Assert.That(defense.ContextCards.Single().Caption, Does.StartWith("ATTACK 1"));
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                .Any(element => element.name == "gd-decision-attack-card" && UiPlayer.IsShown(element)), Is.True,
                "The attacking spell's face is drawn in the defender's window.");
            var redLives = root.Hud.Header.RedLifeLabel;
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            yield return null;
            Assert.That(OnBoard(runtime, wasp), Is.False, "Attack 1 against no Defense defeats Wasp. " + Remaining(runtime));
            Assert.That(root.Hud.Header.RedLifeLabel, Is.Not.EqualTo(redLives), "Her team's Life heart drops.");
            Assert.That(Spell(runtime, GydionCardContent.MagicMissile).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(Card(runtime, gydion, GydionCardContent.Cantrip).State.Location, Is.Not.EqualTo(CardLocation.Hand),
                "The played card is Cantrip; the spell never entered his hand.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator PrepareSpellsRefillsTheBookAndEndsARunningShield()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.ElementaryAbjuration, GydionCardContent.PrepareSpells, GydionCardContent.Cantrip });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);

            // Turn 1: "Cast the Shield spell" names one spell, so nothing is asked.
            yield return Begin(root, GydionCardContent.ElementaryAbjuration);
            var shield = Spell(runtime, GydionCardContent.Shield);
            Assert.That(shield.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook), Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(shield.Id), Is.True, "This round: he is immune to basic attacks.");
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Has.Member(shield.Id));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 21 | 1"));
            AssertFaceupOnEverySeat(runtime, GydionCardContent.Shield);

            // Turn 2: Prepare Spells adds every spell back; the returned Shield's effect ends (designer).
            UiPlayer.EndAnyTurnInProgress(runtime);
            yield return null;
            yield return Begin(root, GydionCardContent.PrepareSpells);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Without The Archwizard the refill asks nothing. " + Remaining(runtime));
            Assert.That(Spell(runtime, GydionCardContent.Shield).State.Location, Is.EqualTo(CardLocation.Spellbook),
                "\"Add all spell cards to the spellbook.\"");
            Assert.That(InTheBook(runtime), Is.EqualTo(22));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"), "The count returns to 22.");
            Assert.That(runtime.IsActiveEffectSource(shield.Id), Is.False,
                "The card changed state - it became facedown - so Shield's Active effect ended (lesson 26).");
            Assert.That(Card(runtime, gydion, GydionCardContent.PrepareSpells).State.Location, Is.Not.EqualTo(CardLocation.Hand));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator GreaterEvocationCaptionsSunburstAndHitsAUnitAtExactlyThatRange()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedGydion,
                new[] { GydionCardContent.GreaterEvocation, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            StageTheBook(runtime, new[] { GydionCardContent.Fireball, GydionCardContent.Shield, GydionCardContent.Suggestion });
            yield return null;
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 19 | 3"), "Three spells staged out of the book.");
            yield return Begin(root, GydionCardContent.GreaterEvocation);

            // Mockup A: Burning Hands, Fireball greyed REMOVED, Sunburst captioned with its counted values.
            var fireball = SpellOption(runtime, GydionCardContent.Fireball);
            Assert.That(fireball, Is.Not.Null, Remaining(runtime));
            Assert.That(fireball.IsAnswerable, Is.False, "A removed spell cannot be cast.");
            Assert.That(fireball.Spell.Caption, Is.EqualTo("FIREBALL - REMOVED"));
            var sunburst = SpellOption(runtime, GydionCardContent.Sunburst);
            Assert.That(sunburst.Spell.Caption, Does.StartWith("SUNBURST - 3 OUT"),
                "+1 Attack and +1 Range for each other spell card removed: three.");
            Assert.That(sunburst.Spell.Caption, Does.Contain("ATTACK 3"));
            Assert.That(sunburst.Spell.Caption, Does.Contain("RANGE 3"));
            var caption = UiPlayer.Descendants(runtime.DecisionWindowView.ElementFor(sunburst)).OfType<Label>()
                .Single(label => label.name == "gd-spell-option-caption");
            Assert.That(caption.text, Is.EqualTo(sunburst.Spell.Caption), "The caption is drawn under the face.");
            yield return PickSpellFace(root, GydionCardContent.Sunburst);

            Assert.That(Spell(runtime, GydionCardContent.Sunburst).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 18 | 4"));
            AssertFaceupOnEverySeat(runtime, GydionCardContent.Sunburst);

            // "Target a unit at maximum range": exactly three, and only Red's melee minion stands there.
            var target = runtime.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null, Remaining(runtime));
            Assert.That(target.LegalTargetHeroIds, Is.Empty, "Wasp stands two away, not three.");
            Assert.That(target.LegalTargetMinionIds, Is.EqualTo(new[] { RedMeleeThreeAway }));
            Assert.That(HexDistanceQuery.Calculate(GydionSpace, MinionAt(runtime, RedMeleeThreeAway)), Is.EqualTo(3));
            yield return TargetMinion(root, RedMeleeThreeAway);
            Assert.That(MinionStands(runtime, RedMeleeThreeAway), Is.False, "Attack 3 defeats the minion three away.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator FindFamiliarPlacesTheOwlAndRemovesUpToThreeSpellsFaceup()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserConjuration, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            yield return Begin(root, GydionCardContent.LesserConjuration);
            yield return PickSpellFace(root, GydionCardContent.FindFamiliar);

            // "Place a [Familiar] token in radius": a space clicked on the board, Radius 3.
            var offered = runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate).ToArray();
            Assert.That(offered, Is.Not.Empty, Remaining(runtime));
            Assert.That(offered.All(space => HexDistanceQuery.Calculate(GydionSpace, space) <= 3), Is.True);
            var at = ReachableSpace(root, offered);
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(at), "The owl's space.");
            Assert.That(runtime.ReadModel.PlacedTokens.Where(token => token.Definition.Id.Equals(GydionComponentContent.FamiliarId))
                .Select(token => token.Coordinate), Is.EqualTo(new[] { at }), "One Familiar, where he put it.");
            var owl = Object.FindObjectsByType<PlacedTokenView>(FindObjectsSortMode.None)
                .Where(view => view.Token != null && view.Token.Definition.Id.Equals(GydionComponentContent.FamiliarId))
                .ToArray();
            Assert.That(owl.Select(view => view.Coordinate), Is.EqualTo(new[] { at }), "Its token is drawn on that space.");

            // "You may remove up to three other spell cards ... faceup": the in-book faces, three picked, CONFIRM.
            var window = runtime.DecisionWindowView.Model;
            Assert.That(window?.Question, Does.StartWith("Remove up to 3"), Remaining(runtime));
            Assert.That(window.Controls.Count(control => control.Spell != null), Is.EqualTo(21),
                "Every other spell, all still in the book - Wish included - as faces.");
            Assert.That(SpellOption(runtime, GydionCardContent.FindFamiliar), Is.Null, "\"Other\" spells only.");
            var picked = new[] { GydionCardContent.MagicMissile, GydionCardContent.Shield, GydionCardContent.Fireball };
            foreach (var spell in picked)
            {
                yield return PickSpellFace(root, spell);
                Assert.That(SpellOption(runtime, spell)?.IsChosen, Is.True, spell.Value + " wears the chosen outline.");
            }

            Assert.That(runtime.DecisionWindowView.Model.Count, Is.EqualTo("3 of 3 chosen"));
            Assert.That(Spell(runtime, GydionCardContent.MagicMissile).State.Location, Is.EqualTo(CardLocation.Spellbook),
                "Nothing is removed until he confirms.");
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            foreach (var spell in picked.Concat(new[] { GydionCardContent.FindFamiliar }))
            {
                Assert.That(Spell(runtime, spell).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook), spell.Value);
            }

            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 18 | 4"));
            AssertFaceupOnEverySeat(runtime, picked.Concat(new[] { GydionCardContent.FindFamiliar }).ToArray());

            // The drawer, opened from the chip as a player opens it, shows them faceup.
            var chip = UiPlayer.ItemStripDescendants(root.Hud).OfType<Button>().Single(element => element.name == "gd-spellbook-chip");
            UiPlayer.Click(chip);
            yield return null;
            yield return null;
            Assert.That(runtime.HeroOverviewsView.IsSpellbookOpen, Is.True);
            var removedRow = UiPlayer.Descendants(runtime.HeroOverviewsView.SpellbookDrawer)
                .Single(element => element.name == "gd-spellbook-removed");
            Assert.That(UiPlayer.Descendants(removedRow).Where(element => element.name == "gd-spell-face")
                    .Select(element => ((CardDefinitionReference)element.userData).DefinitionId),
                Is.EquivalentTo(picked.Concat(new[] { GydionCardContent.FindFamiliar })),
                "REMOVED, FACEUP: the three he chose and Find Familiar.");
            runtime.HeroOverviewsView.CloseSpellbook();
            yield return null;
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator DimensionDoorPlacesHimAtTheCountedRadius()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.LesserConjuration, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            StageTheBook(runtime, new[] { GydionCardContent.Fireball, GydionCardContent.Shield });
            yield return null;
            yield return Begin(root, GydionCardContent.LesserConjuration);
            var door = SpellOption(runtime, GydionCardContent.DimensionDoor);
            Assert.That(door?.Spell.Caption, Does.StartWith("DIMENSION DOOR - 2 OUT"), Remaining(runtime));
            yield return PickSpellFace(root, GydionCardContent.DimensionDoor);

            // "Place yourself into a space at maximum radius. +1 Radius for each other spell removed": two.
            var offered = runtime.PendingEffectChoice?.Candidates.Select(value => value.Value.Coordinate).ToArray();
            Assert.That(offered, Is.Not.Empty, Remaining(runtime));
            Assert.That(offered.Select(space => HexDistanceQuery.Calculate(GydionSpace, space)).Distinct(),
                Is.EqualTo(new[] { 2 }), "Every offered space is exactly two away.");
            var at = ReachableSpace(root, offered);
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(at), "His space.");
            Assert.That(SpaceOf(runtime, gydion), Is.EqualTo(at), "He is placed there. " + Remaining(runtime));
            Assert.That(HeroView(gydion).Coordinate, Is.EqualTo(at), "His standee stands there.");
            Assert.That(Spell(runtime, GydionCardContent.DimensionDoor).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 19 | 3"));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Suggestion, on the moved hero's own seat (GY-Q6)
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SuggestionsDirectionIsPickedOnTheMovedEnemysOwnSeat()
        {
            var hand = new[] { GydionCardContent.ElementaryEnchantment, GydionCardContent.PrepareSpells };
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion, hand);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            var wasp = Wasp(runtime);

            // His half, on his controls: "Cast Suggestion" names one spell; he picks the enemy hero.
            yield return Begin(root, GydionCardContent.ElementaryEnchantment);
            Assert.That(Spell(runtime, GydionCardContent.Suggestion).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            AssertFaceupOnEverySeat(runtime, GydionCardContent.Suggestion);
            yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "An enemy hero in radius 3: Wasp.");
            var direction = runtime.PendingEffectChoice;
            Assert.That(direction?.ChooserId, Is.EqualTo(wasp), "Her own seat picks the direction (GY-Q6). " + Remaining(runtime));
            Assert.That(runtime.Host.ProjectionForSeat(SeatOf(runtime, gydion)).AuthorizedCommands
                    .Any(command => command.Kind == DirectorCommandKind.AnswerEffectChoice), Is.False,
                "His seat is offered no answer to her question.");
            var lines = direction.Candidates.Select(value => value.Value.Coordinate).ToArray();
            Assert.That(lines.Select(space => HexDistanceQuery.Calculate(WaspSpace, space)).Distinct(),
                Is.EqualTo(new[] { 3 }), "Exactly three spaces in a straight line, or not at all.");
            var paused = runtime.CaptureSnapshot();
            var waspSeat = SeatOf(runtime, wasp);

            // Her half, on HER seat's HUD: the same staged match, restored at her question.
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion, hand, seat: waspSeat);
            root = Root();
            runtime = root.Runtime;
            Assert.That(runtime.Seat?.HeroId, Is.EqualTo(wasp), "The HUD is composed for Wasp's seat.");
            Assert.That(runtime.RestoreSnapshot(paused), Is.True, runtime.LastOperationDetail);
            yield return null;
            yield return null;
            Assert.That(runtime.ReadModel.PendingEffectChoice?.ChooserId, Is.EqualTo(wasp), Remaining(runtime));
            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations
                    .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate),
                Is.EquivalentTo(lines), "Her own board lights the lines she may be moved along.");
            var end = ReachableSpace(root, lines);
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(end).transform.position), Is.True,
                UiPlayer.DescribeBoardRay(Pointer(), Tile(end).transform.position));
            yield return null;
            yield return null;
            Assert.That(runtime.ReadModel.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(SpaceOf(runtime, wasp), Is.EqualTo(end), "She moved three spaces where she chose.");
            Assert.That(HeroView(wasp).Coordinate, Is.EqualTo(end));
            Assert.That(HexDistanceQuery.Calculate(WaspSpace, SpaceOf(runtime, wasp)), Is.EqualTo(3));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Energy Drain: the classified discard and the regained Life
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EnergyDrainTakesANonBasicCardAndRegainsASpentLife()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.GreaterNecromancy, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            StageTheBook(runtime, Array.Empty<CardDefinitionId>(), spentLives: 1);
            yield return null;
            var blueLives = root.Hud.Header.BlueLifeLabel;
            Assert.That(blueLives, Is.EqualTo("3"), "His team has spent one of its four Lives.");
            yield return Begin(root, GydionCardContent.GreaterNecromancy);
            yield return PickSpellFace(root, GydionCardContent.EnergyDrain);
            Assert.That(Spell(runtime, GydionCardContent.EnergyDrain).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            AssertFaceupOnEverySeat(runtime, GydionCardContent.EnergyDrain);
            yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                "An enemy hero in range 3: Wasp.");

            // "... discards a non-basic card, if able": her choice, among her non-basic cards only.
            var discard = runtime.PendingEffectChoice;
            Assert.That(discard?.ChooserId, Is.EqualTo(wasp), Remaining(runtime));
            var offered = discard.Candidates.Select(value => runtime.Cards.Single(card => card.Id.Equals(value.Value.CardInstanceId))).ToArray();
            Assert.That(offered.Select(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Progression.Classification).Distinct(),
                Is.EqualTo(new[] { CardClassification.NonBasic }), "Only her non-basic cards.");
            var hand = HandCount(runtime, wasp);
            var taken = discard.Candidates.First().Value.CardInstanceId;
            yield return Pick(root, value => value.Kind == EffectValueKind.CardInstance && value.CardInstanceId.Equals(taken),
                "Her first non-basic card.");
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(taken)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(HandCount(runtime, wasp), Is.EqualTo(hand - 1));

            // "Your team regains 1 spent Life counter" (GY-Q11), on the status strip's blue heart.
            Assert.That(root.Hud.Header.BlueLifeLabel, Is.EqualTo("4"), "His team regained its spent Life.");
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-life-counter-value" && UiPlayer.IsShown(element))
                .OfType<Label>().Select(label => label.text), Has.Member("4"));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Archwizard and Wish (GY-Q7, GY-Q9, GY-Q10)
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator TheArchwizardOffersWishInsteadAndWishCastsASpellNoHeldCardLists()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedGydion,
                new[] { GydionCardContent.PrepareSpells, GydionCardContent.Cantrip, GydionCardContent.GreaterEvocation,
                    GydionCardContent.GreaterTransmutation, GydionCardContent.GreaterConjuration });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            var plaque = runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(gydion));
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "The Archwizard stands faceup as the purple card in his items strip (9.30a).");
            Assert.That(plaque.UltimateTallyLabel, Is.Null, "No Wish is cast yet.");
            yield return Begin(root, GydionCardContent.PrepareSpells);

            // GY-Q7: a full book still "would add cards", so he may cast Wish instead.
            var window = runtime.DecisionWindowView.Model;
            Assert.That(window?.Controls.Any(control => control.Spell == null && control.Label.StartsWith("Add all spell cards")),
                Is.True, Remaining(runtime));
            Assert.That(SpellOption(runtime, GydionCardContent.Wish)?.IsAnswerable, Is.True, "Cast Wish instead, on Wish's face.");
            yield return PickSpellFace(root, GydionCardContent.Wish);
            Assert.That(Spell(runtime, GydionCardContent.Wish).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook),
                "Wish is removed as it is cast (GY-Q2).");
            AssertFaceupOnEverySeat(runtime, GydionCardContent.Wish);

            // GY-Q9: any spell in the book, Invulnerability among them - none of his held cards lists it.
            var listed = new[] { GydionCardContent.Cantrip, GydionCardContent.GreaterEvocation,
                    GydionCardContent.GreaterTransmutation, GydionCardContent.GreaterConjuration }
                .SelectMany(card => GydionCardContent.SpellsListedOn(card)).ToArray();
            Assert.That(listed, Has.No.Member(GydionCardContent.Invulnerability));
            Assert.That(runtime.DecisionWindowView.Model.Controls.Count(control => control.Spell != null && control.IsAnswerable),
                Is.EqualTo(21), "Every other spell, all in the book.");
            yield return PickSpellFace(root, GydionCardContent.Invulnerability);
            var invulnerability = Spell(runtime, GydionCardContent.Invulnerability);
            Assert.That(invulnerability.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(runtime.IsActiveEffectSource(invulnerability.Id), Is.True, "Its round-long immunity runs.");
            AssertFaceupOnEverySeat(runtime, GydionCardContent.Wish, GydionCardContent.Invulnerability);
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            yield return null;

            // The plaque reads the team's Wish count beside the purple card.
            plaque = runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(gydion));
            Assert.That(plaque.UltimateTallyLabel, Is.EqualTo("1/3"));
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement).OfType<Label>()
                .Where(label => label.name == "gd-ultimate-tally" && UiPlayer.IsShown(label))
                .Select(label => label.text), Is.EqualTo(new[] { "1/3" }));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 20 | 2"));
            Assert.That(runtime.ReadModel.Outcome.HasEnded, Is.False);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator AThirdWishWinsTheGameForHisTeam()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedGydion,
                new[] { GydionCardContent.PrepareSpells, GydionCardContent.Cantrip });
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            StageTheBook(runtime, Array.Empty<CardDefinitionId>(), wishesCast: 2);
            yield return null;
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(gydion)).UltimateTallyLabel,
                Is.EqualTo("2/3"), "Two Wishes already cast.");
            yield return Begin(root, GydionCardContent.PrepareSpells);
            yield return PickSpellFace(root, GydionCardContent.Wish);
            Assert.That(Spell(runtime, GydionCardContent.Wish).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(runtime.ReadModel.Outcome.HasEnded, Is.False,
                "The team wins once the third Wish's whole action has resolved (GY-Q10).");
            yield return PickSpellFace(root, GydionCardContent.Shield);
            yield return null;
            var shield = Spell(runtime, GydionCardContent.Shield);
            Assert.That(shield.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));
            Assert.That(runtime.IsActiveEffectSource(shield.Id), Is.True, "The inner spell resolved before the claim.");

            var outcome = runtime.ReadModel.Outcome;
            Assert.That(outcome.Cause, Is.EqualTo(GuardsDigital.Core.Rules.Victory.MatchVictoryCause.WishesCast), Remaining(runtime));
            Assert.That(outcome.WinningTeamId, Is.EqualTo(runtime.ReadModel.Participants.GetPlayerForHero(gydion).TeamId));
            yield return null;
            Assert.That(runtime.VictoryScreenView.Model, Is.Not.Null, "The match ends on the victory screen. " + UiPlayer.Describe(root.Hud));
            Assert.That(VictoryScreenUiJourneyPlayModeTests.Text(root.Hud, "gd-victory-cause"),
                Is.EqualTo("THREE WISHES WERE CAST"));
            Assert.That(UiPlayer.IsShown(VictoryScreenUiJourneyPlayModeTests.Screen_(root.Hud)), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Shield and Invulnerability, by attack classification
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ShieldRefusesABasicAttackButNotANonBasicOne()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.ElementaryAbjuration, GydionCardContent.Cantrip, GydionCardContent.ElementaryEvocation },
                wasp: WaspBesideHim);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            var wasp = Wasp(runtime);

            yield return Begin(root, GydionCardContent.ElementaryAbjuration);
            var shield = Spell(runtime, GydionCardContent.Shield);
            Assert.That(shield.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook), Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(shield.Id), Is.True);
            AssertFaceupOnEverySeat(runtime, GydionCardContent.Shield);

            // Turn 2: her basic Magnetic Dagger (12) acts first and cannot target him.
            CommitBoth(runtime, GydionCardContent.Cantrip, WaspCardContent.MagneticDagger);
            yield return null;
            yield return WaspStartsHerAttack(root);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Has.No.Member(gydion),
                "This round he is immune to basic attacks. " + Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(shield.Id), Is.True, "Shield still runs on her turn (round-long).");
            UiPlayer.ClickBoard(Pointer(), root.Hud, HeroView(gydion).transform.position);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, "Clicking him answers nothing: he is refused.");
            Assert.That(OnBoard(runtime, gydion), Is.True);
            yield return TargetMinion(root, BlueMelee);
            Assert.That(MinionStands(runtime, BlueMelee), Is.False, "Her attack takes the minion instead.");

            // Turn 3: her non-basic Shock (8) acts before his Elementary Evocation (7) and may target him.
            CommitBoth(runtime, GydionCardContent.ElementaryEvocation, WaspCardContent.Shock);
            yield return null;
            yield return WaspStartsHerAttack(root);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Has.Member(gydion),
                "Shield covers basic attacks only. " + Remaining(runtime));
            yield return TargetHero(root, gydion);
            Assert.That(runtime.DecisionWindowView.Model?.Family, Is.EqualTo(DecisionWindowFamily.Defense), Remaining(runtime));
            var blueLives = root.Hud.Header.BlueLifeLabel;
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            yield return null;
            Assert.That(OnBoard(runtime, gydion), Is.False, "Shock's 5 defeats him: the non-basic attack was not refused.");
            Assert.That(root.Hud.Header.BlueLifeLabel, Is.Not.EqualTo(blueLives));
        }

        [UnityTest]
        public IEnumerator InvulnerabilityRefusesANonBasicAttackButNotABasicOne()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.GreaterAbjuration, GydionCardContent.ElementaryEvocation, GydionCardContent.Cantrip },
                wasp: WaspBesideHim);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);

            yield return Begin(root, GydionCardContent.GreaterAbjuration);
            yield return PickSpellFace(root, GydionCardContent.Invulnerability);
            var invulnerability = Spell(runtime, GydionCardContent.Invulnerability);
            Assert.That(invulnerability.State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook), Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(invulnerability.Id), Is.True);
            AssertFaceupOnEverySeat(runtime, GydionCardContent.Invulnerability);

            // Turn 2: her non-basic Shock (8) acts first and cannot target him.
            CommitBoth(runtime, GydionCardContent.ElementaryEvocation, WaspCardContent.Shock);
            yield return null;
            yield return WaspStartsHerAttack(root);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Has.No.Member(gydion),
                "This round he is immune to non-basic attacks. " + Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(invulnerability.Id), Is.True, "It still runs on her turn.");
            yield return TargetMinion(root, BlueMelee);
            Assert.That(MinionStands(runtime, BlueMelee), Is.False);

            // Turn 3: her basic Magnetic Dagger (12) acts before his Cantrip (11) and may target him.
            CommitBoth(runtime, GydionCardContent.Cantrip, WaspCardContent.MagneticDagger);
            yield return null;
            yield return WaspStartsHerAttack(root);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Has.Member(gydion), Remaining(runtime));
            yield return TargetHero(root, gydion);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            yield return null;
            Assert.That(OnBoard(runtime, gydion), Is.False, "The basic attack was not refused.");
        }

        // -------------------------------------------------------------------------------------------
        // Every face's ribbon
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesHimThroughItsCardControl()
        {
            foreach (var definition in GydionCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                    new[] { definition.Id, GydionCardContent.PrepareSpells });
                var root = Root();
                var runtime = root.Runtime;
                var gydion = Gydion(runtime);
                yield return CommitEverySeat(root, gydion, NameOf(runtime, definition.Id));
                yield return GarrusUiJourneyTests.WaitForActor(root, gydion);
                yield return PointTrayAt(root, gydion);
                yield return StartActionThroughControl(root, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value + " " + Remaining(runtime));
                var step = ReachableSpace(root, runtime.PendingMovementChoice.LegalDestinations
                    .Where(value => value.Cost == 1).Select(value => value.Destination));
                Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(step).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, gydion), Is.EqualTo(step), definition.Id.Value);
                Assert.That(HeroView(gydion).Coordinate, Is.EqualTo(step), definition.Id.Value);
                Assert.That(InTheBook(runtime), Is.EqualTo(22), "A walk casts nothing.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in GydionCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction)))
            {
                var committed = definition.Id.Equals(GydionCardContent.ElementaryAbjuration)
                    ? GydionCardContent.LesserAbjuration
                    : GydionCardContent.ElementaryAbjuration;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                    new[] { definition.Id, committed }, wasp: WaspBesideHim);
                var root = Root();
                var runtime = root.Runtime;
                var gydion = Gydion(runtime);
                CommitBoth(runtime, committed, WaspCardContent.MagneticDagger);
                yield return null;
                yield return WaspStartsHerAttack(root);
                yield return TargetHero(root, gydion);
                Assert.That(runtime.PendingDefenseChoice?.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True,
                    definition.Id.Value + " " + Remaining(runtime));
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(Card(runtime, gydion, definition.Id).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                    definition.Id.Value + " is spent from his hand to defend.");
                Assert.That(InTheBook(runtime), Is.EqualTo(22), "A Defense casts nothing.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Session continuity, lifecycle and components
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator APausedSpellChoiceRestoresAtItsQuestion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.Cantrip, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = Wasp(runtime);
            yield return Begin(root, GydionCardContent.Cantrip);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked?.NodeId, Is.EqualTo(GydionCardContent.CastChoice(GydionCardContent.Cantrip)), Remaining(runtime));
            var pause = runtime.CaptureSnapshot();
            yield return PickSpellFace(root, GydionCardContent.MagicMissile);
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 21 | 1"));

            Assert.That(runtime.RestoreSnapshot(pause), Is.True, runtime.LastOperationDetail);
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId), "The restored match waits at the spell choice.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            Assert.That(Spell(runtime, GydionCardContent.MagicMissile).State.Location, Is.EqualTo(CardLocation.Spellbook),
                "Magic Missile is read back into the book.");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"), "The chip is drawn again from the restored book.");
            Assert.That(runtime.DecisionWindowView.Model.Controls.Count(control => control.Spell != null), Is.EqualTo(3),
                "The window draws the three faces again.");
            Healthy(runtime);

            yield return PickSpellFace(root, GydionCardContent.MagicMissile);
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetHeroIds, Has.Member(wasp), "The restored cast resolves.");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 21 | 1"));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator ACastUndoesToItsRetainedPrefixAndReplaysExactly()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.Cantrip, GydionCardContent.PrepareSpells });
            var root = Root();
            var runtime = root.Runtime;
            yield return Begin(root, GydionCardContent.Cantrip);
            var asked = runtime.PendingEffectChoice.NodeId;
            var pause = runtime.CaptureSnapshot();
            yield return PickSpellFace(root, GydionCardContent.MagicMissile);
            var cast = runtime.CaptureSnapshot();
            Assert.That(Spell(runtime, GydionCardContent.MagicMissile).State.Location, Is.EqualTo(CardLocation.RemovedFromSpellbook));

            OpenDirectorDrawer(root.Hud);
            yield return null;
            for (var press = 0; press < 4 && !(runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.NodeId.Equals(asked) &&
                runtime.PendingEffectChoice.SelectedCandidates.Count == 0); press++)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked), "Undo gives the spell choice back. " + Remaining(runtime));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause), "The retained prefix is the same pause.");
            Assert.That(Spell(runtime, GydionCardContent.MagicMissile).State.Location, Is.EqualTo(CardLocation.Spellbook),
                "The cast spell is back in the book.");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;

            yield return PickSpellFace(root, GydionCardContent.MagicMissile);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(cast), "The same answer replays into the same match.");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 21 | 1"));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator HeLeavesOnHisDefeatRespawnsByAClickAndARestartRebuildsTheBoard()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.ElementaryAbjuration, GydionCardContent.Cantrip, GydionCardContent.PrepareSpells },
                wasp: WaspBesideHim);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            var standee = HeroView(gydion);
            Assert.That(standee.HeroContentId.Value.Value, Is.EqualTo(GydionCardContent.ContentSetId));

            CommitBoth(runtime, GydionCardContent.ElementaryAbjuration, WaspCardContent.MagneticDagger);
            yield return null;
            yield return WaspStartsHerAttack(root);
            yield return TargetHero(root, gydion);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(OnBoard(runtime, gydion), Is.False, "He is defeated. " + Remaining(runtime));
            Assert.That(standee.gameObject.activeSelf, Is.False, "His standee leaves the board.");
            Assert.That(runtime.BoardView.HeroDefeatGhostFor(standee), Is.Not.Null, "and fades.");
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"), "A Defeat removes no spell.");

            // His turn-1 card resolves off the board; at his next action p.13 offers the respawn.
            yield return YieldUntilTheNamedSeatActs(runtime, gydion);
            CommitSoTheNamedSeatActs(runtime, gydion, GydionCardContent.Cantrip);
            yield return YieldUntilTheNamedSeatActs(runtime, gydion);
            var respawn = ReachableSpace(root,
                runtime.RoundView.Model.HeroRespawnOptions.Select(option => option.Destination).ToArray());
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(respawn).transform.position), Is.True);
            yield return null;
            Assert.That(SpaceOf(runtime, gydion), Is.EqualTo(respawn), "He respawns where he was clicked.");
            Assert.That(HeroView(gydion).Coordinate, Is.EqualTo(respawn));
            Healthy(runtime);

            // The Defeat's spotlight may still be entering: wait for the drawer's control as a player does.
            yield return UiPlayer.WaitForControl(root.Hud, "DIRECTOR");
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(SpaceOf(runtime, gydion), Is.EqualTo(GydionSpace), "Restart reconstructs the staged board.");
            Assert.That(HeroView(gydion).Coordinate, Is.EqualTo(GydionSpace));
            Assert.That(Chip(root), Is.EqualTo("SPELLBOOK 22 | 0"));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedGydionSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
            var root = Root();
            var gydion = Gydion(root.Runtime);
            var view = HeroView(gydion);
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo(GydionCardContent.ContentSetId));
            Assert.That(view.HasTokenVisual, Is.True, "His prepared standee face is drawn.");
            Assert.That(view.Coordinate, Is.EqualTo(GydionSpace));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(gydion));
            Assert.That(root.Runtime.HeroOverviewsView.Model.Heroes.Count(hero => hero.HeroId.Equals(gydion)), Is.EqualTo(1));
            Healthy(root.Runtime);
        }

        /// <summary>
        /// His nineteen fronts at the standard card footprint (8.20c), and his twenty-two spells at the
        /// landscape spell footprint every spell surface draws them at (9.30h): a spell is never drawn as
        /// a portrait card, so its conformance is the face <see cref="SpellCardFaceView"/> builds.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryGydionFaceKeepsItsFootprint()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGydion);
            var hud = Root().Hud;
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Gydion", GydionCardContent.ActionCardDefinitions, hud);

            var stage = new VisualElement { name = "gd-spell-footprint-stage" };
            stage.style.position = Position.Absolute;
            stage.style.left = 0f;
            stage.style.top = 0f;
            stage.style.width = SpellCardFootprint.Width + 240f;
            stage.style.height = SpellCardFootprint.Height + 240f;
            stage.style.alignItems = Align.FlexStart;
            stage.pickingMode = PickingMode.Ignore;
            hud.RootElement.Add(stage);
            var failures = new List<string>();
            try
            {
                foreach (var spell in GydionCardContent.SpellDefinitions)
                {
                    stage.Clear();
                    var face = SpellCardFaceView.Build(new SpellCardViewModel(spell));
                    stage.Add(face);
                    yield return null;
                    var box = face.resolvedStyle;
                    var snapping = UiPlayer.PixelSnapping();
                    if (Mathf.Abs(box.width - SpellCardFootprint.Width) > snapping ||
                        Mathf.Abs(box.height - SpellCardFootprint.Height) > snapping)
                    {
                        failures.Add($"{spell.DisplayName}: {box.width}x{box.height}, not " +
                            $"{SpellCardFootprint.Width}x{SpellCardFootprint.Height}.");
                    }

                    var bounds = face.worldBound;
                    foreach (var label in UiPlayer.Descendants(face).OfType<Label>()
                        .Where(label => !string.IsNullOrEmpty(label.text)))
                    {
                        var drawn = label.worldBound;
                        if (drawn.xMin < bounds.xMin - 0.5f || drawn.xMax > bounds.xMax + 0.5f ||
                            drawn.yMin < bounds.yMin - 0.5f || drawn.yMax > bounds.yMax + 0.5f)
                        {
                            failures.Add($"{spell.DisplayName}: \"{label.text}\" is drawn outside the face.");
                        }
                    }
                }
            }
            finally
            {
                stage.RemoveFromHierarchy();
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        [UnityTest]
        public IEnumerator GydionLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedGydion,
                new[] { GydionCardContent.Cantrip, GydionCardContent.PrepareSpells }, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var gydion = Gydion(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(gydion));
                budget++)
                yield return RoundStep(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(gydion));
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
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(gydion));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value, Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(gydion)).ItemBonuses,
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
    }
}
