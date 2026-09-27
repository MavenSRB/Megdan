using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Ignatia the Mad's piece, her tray, her two basics and her three Tier I faces, driven through real
    /// pointer events and camera rays. Milestone 8.75c, and the TWENTY-SECOND hero to take the permanent
    /// player-path gate.
    ///
    /// <para><b>EVERY FACE OF HERS HAS TWO TEXTS, SO EVERY JOURNEY HAS TWO BOARDS.</b> Her whole set is
    /// composed as one <c>Conditional</c> on <c>ignatia-coin-sun@1</c>, and which arm runs is the live
    /// face of rulebook p.7's Tie Breaker coin - sun on Blue, moon on Red (ID-Q5, user-corrected). The
    /// staged configurations take that face as an argument, so
    /// <see cref="SceneCardContent.VerifiedEngagedStagedIgnatia"/> and
    /// <see cref="SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon"/> are the SAME board under the
    /// two faces, and a journey that drives both arms of one card loads both.</para>
    ///
    /// <para><b>A STAGED HAND REACHES A CARD THE STAGED CONFIGURATION DID NOT DEAL</b>
    /// (<c>SandboxStagingAuthority.StageHands</c> takes a card out of its owner's own deck), which is why
    /// every card journey of hers - including the Tier II and Tier III fixtures beside this one - stands
    /// on these two boards rather than on the six progressed ones. The progressed configurations are
    /// needed for exactly two things a staged hand cannot do: p.15's Level Up transaction and p.16's
    /// UNLOCKED Ultimate, and those two are where they are used.</para>
    ///
    /// <para><b>THE MEASURED BOARD.</b> Milestone 8.75b's engagement stands her at (-3,1) and the one
    /// enemy hero at (-2,2) - separation two - with the friendly Blue melee minion at (-3,2) and the
    /// enemy Red ranged minion at (-4,2) in her ring, enemy Red melee minions at (-1,-1) (ON one of her
    /// six files) and (-1,0) (OFF every file), an enemy Red melee at (-4,4), and the enemy heavy at
    /// (-7,6) and melee at (-8,4) standing at EXACTLY five. Her free ring-1 spaces are (-2,1), (-2,0),
    /// (-3,0) and (-4,1); free straight files leave her square through (-2,1) and (-4,1) for four
    /// spaces, through (-3,0) for two. Milestone 8.75c re-measured all of it offline before a line of
    /// this file was written, including the three restagings the journeys use: the enemy hero moved to
    /// (-1,1) (two away and ON a file, which is what the straight-line pair's moon arm and the whole
    /// discard line need), to (-2,1) (adjacent, for the "adjacent to you" arms beside the ranged minion
    /// already there), and the enemy ranged minion moved to (-5,3) (which empties her ring of enemies,
    /// so the sun arm of her Gold names nobody and the attack is greyed).</para>
    /// </summary>
    public sealed class IgnatiaUiJourneyPlayModeTests
    {
        /// <summary>Where the sandbox stands her: the engagement's own space for the Blue hero.</summary>
        internal static readonly HexCoordinate HerSpace = new HexCoordinate(-3, 1);

        /// <summary>The engagement's own enemy space - MEASURED two away and OFF every one of her files.</summary>
        internal static readonly HexCoordinate EnemySpace = new HexCoordinate(-2, 2);

        /// <summary>MEASURED two away and ON one of her six files, with the enemy melee at (-1,0) beside it.</summary>
        internal static readonly HexCoordinate EnemyOnHerFile = new HexCoordinate(-1, 1);

        /// <summary>MEASURED adjacent to her, which is what every "adjacent to you" arm needs of a hero.</summary>
        internal static readonly HexCoordinate AdjacentEnemy = new HexCoordinate(-2, 1);

        /// <summary>
        /// MEASURED adjacent to her and inside every printed Radius her Blue line carries, with exactly
        /// ONE free straight file of two leaving it - through (-3,0) to (-4,0). Both pushes name a
        /// destination "2 spaces in a straight line", so a subject whose other five directions are
        /// closed is what makes that offer an exact set rather than a sample.
        /// </summary>
        internal static readonly HexCoordinate AllyBesideHer = new HexCoordinate(-2, 0);

        /// <summary>MEASURED two away, with free files of two through (1,1) and (-1,3) and of three through (2,1).</summary>
        internal static readonly HexCoordinate AllyOnHerFile = new HexCoordinate(-1, 1);

        /// <summary>The friendly melee minion the engaged staging leaves inside her ring.</summary>
        internal static readonly MinionId BlueMelee = new MinionId("blue-melee-01");

        /// <summary>The friendly ranged minion at (-3,4) - three away, and swappable where the heavy is not.</summary>
        internal static readonly MinionId BlueRanged = new MinionId("blue-ranged-01");

        /// <summary>The friendly heavy at (0,0). Rulebook p.18 makes it immune to being swapped or placed.</summary>
        internal static readonly MinionId BlueHeavy = new MinionId("blue-heavy-01");

        /// <summary>The enemy ranged minion in her ring at (-4,2) - MEASURED on one of her six files.</summary>
        internal static readonly MinionId RedRanged = new MinionId("red-ranged-01");

        /// <summary>The enemy melee at (-1,-1) - two away and ON one of her files.</summary>
        internal static readonly MinionId RedMeleeOnAFile = new MinionId("red-melee-01");

        /// <summary>The enemy melee at (-1,0) - two away and OFF every file, and beside (-1,1).</summary>
        internal static readonly MinionId RedMeleeOffEveryFile = new MinionId("red-melee-02");

        /// <summary>The enemy melee at (-4,4) - three away and off every file.</summary>
        internal static readonly MinionId RedMeleeAtThree = new MinionId("red-melee-04");

        /// <summary>The enemy melee at (-8,4) - MEASURED at exactly five.</summary>
        internal static readonly MinionId RedMeleeAtFive = new MinionId("red-melee-03");

        /// <summary>The enemy heavy at (-7,6) - MEASURED at exactly five.</summary>
        internal static readonly MinionId RedHeavyAtFive = new MinionId("red-heavy-01");

        /// <summary>
        /// The staged seat dealt Ignatia's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering. A staged arm missing from
        /// <c>SelectSetupConfiguration</c> deals her cards to a seat whose content identity is somebody
        /// else's, which is milestone 8.38f's defect and what this assertion exists to catch.
        /// </summary>
        internal static HeroId Ignatia(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == IgnatiaCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Ignatia to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0].HeroId;
        }

        /// <summary>Her ally seat, where a configuration deals one - Arien, on the ally boards.</summary>
        internal static HeroId Ally(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == ArienCardContent.ContentSetId)
                .Select(hero => hero.HeroId)
                .SingleOrDefault();

        /// <summary>Every enemy seat, nearest first.</summary>
        internal static HeroId[] Enemies(ConfiguredMatchSceneRuntime runtime)
        {
            var hers = Ignatia(runtime);
            var ally = Ally(runtime);
            return runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(hers) && !hero.HeroId.Equals(ally))
                .Select(hero => hero.HeroId)
                .ToArray();
        }

        /// <summary>The match seat that owns this hero, for the seat-scoped readings a rewind needs.</summary>
        internal static SeatId SeatOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            var seat = runtime.Host.Seats.Seats.SingleOrDefault(value => value.Owns(heroId));
            Assert.That(seat, Is.Not.Null, "No seat owns " + heroId.Value + ".");
            return seat.Id;
        }

        /// <summary>The live face of the match's own Tie Breaker coin - what selects her card text.</summary>
        internal static PrintedTextFace Coin(ConfiguredMatchSceneRuntime runtime) =>
            CoinSelectedTextQuery.LiveFace(runtime.Projection.TieBreakerState);

        /// <summary>
        /// Loads the named staged scene, reads the seats off it, and reloads the same scene with the
        /// Director's sandbox standing the pieces and dealing the hands this journey needs.
        ///
        /// <para>Two loads rather than one because a staging description names heroes by id and the ids
        /// only exist once a configuration has been composed - the shape milestone 8.37a's Guardian
        /// Spirit journey minted and every hero since has reused.</para>
        /// </summary>
        internal static IEnumerator StageIgnatia(
            SceneCardContent content,
            IEnumerable<CardDefinitionId> hand = null,
            IEnumerable<CardDefinitionId> enemyHand = null,
            IEnumerable<CardDefinitionId> allyHand = null,
            HexCoordinate? herSpace = null,
            HexCoordinate? enemySpace = null,
            HexCoordinate? allySpace = null,
            IEnumerable<KeyValuePair<MinionId, HexCoordinate>> movedMinions = null)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var hers = Ignatia(runtime);
            var ally = Ally(runtime);
            var enemies = Enemies(runtime);

            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(hers, herSpace ?? HerSpace, hand: hand),
                new SandboxHeroStaging(enemies[0], enemySpace ?? EnemySpace, hand: enemyHand)
            };
            if (ally.IsDefined)
            {
                heroes.Add(new SandboxHeroStaging(ally, allySpace ?? AllyBesideHer, hand: allyHand));
            }

            for (var index = 1; index < enemies.Length; index++)
            {
                heroes.Add(new SandboxHeroStaging(enemies[index], new HexCoordinate(-5, 1)));
            }

            MatchSceneCompositionRoot.NextComposedSandboxStaging =
                new SandboxStagingDescription(heroes: heroes, movedMinions: movedMinions);
            yield return LoadStagedScene(content);
        }

        /// <summary>
        /// Her own tray card, addressed by the face it prints rather than by any text that face
        /// MENTIONS. Min's 8.61k lesson: the shared <c>UiPlayer.NamedElementWithText</c> matches a tray
        /// card's assistive tooltip by SUBSTRING, and the tooltip is composed as
        /// "{DisplayName} - {ColorName} - ..." (<c>TrayCardViewModel.AssistiveLabel</c>), so a face is
        /// named by what its tooltip STARTS with - which is the card and never the sentence. HER SET
        /// NEEDS IT TWICE OVER: "Path of Ashes", "Path of Cinders" and "Path of Flames" share two words,
        /// "Chaos Bolt", "Chaos Gate" and "Chaos Incarnate" share one, and every face of hers prints the
        /// words of BOTH its texts.
        /// </summary>
        internal static VisualElement HerTrayFace(ConfiguredMatchHudRenderer hud, string printedName)
        {
            var matches = UiPlayer.Descendants(hud.RootElement)
                .Where(candidate => candidate.name == "gd-card-choice" &&
                    UiPlayer.IsShown(candidate) &&
                    UiPlayer.Descendants(candidate).Prepend(candidate).Any(element =>
                        !string.IsNullOrEmpty(element.tooltip) &&
                        element.tooltip.StartsWith(
                            printedName, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1),
                "Expected exactly one shown tray card whose face is '" + printedName +
                "'; the shown controls are: " + UiPlayer.Describe(hud));
            return matches[0];
        }

        /// <summary>
        /// THE TIE BREAKER BANNER the rail draws - since milestone 8.75e the ONE surface that says
        /// which of a two-text card's printed lines is live.
        /// </summary>
        internal static VisualElement TieBreakerBanner(ConfiguredMatchHudRenderer hud)
        {
            var matches = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-tie-breaker-rail-banner")
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1),
                "The rail draws exactly one tie-breaker banner. " + UiPlayer.Describe(hud));
            return matches[0];
        }

        /// <summary>
        /// The banner is on screen and its own geometry puts it on the side the coin is showing -
        /// which is the whole of the reading, on the strip the user kept.
        /// </summary>
        internal static void AssertBannerShows(
            MatchSceneCompositionRoot root, PrintedTextFace face)
        {
            var banner = TieBreakerBanner(root.Hud);
            Assert.That(UiPlayer.IsShown(banner), Is.True,
                "The coin strip is on screen. " + UiPlayer.Describe(root.Hud));
            var expected = CoinSelectedTextQuery.SideShowing(face) == TeamSide.Red
                ? root.Hud.RedTieBreakerRailBannerRect
                : root.Hud.BlueTieBreakerRailBannerRect;
            Assert.That(banner.style.left.value.value, Is.EqualTo(expected.x).Within(0.5f),
                "The banner's own geometry names the side the coin is showing - " +
                PrintedTextFaceLabels.SideWord(CoinSelectedTextQuery.SideShowing(face)) +
                ", which is the " + PrintedTextFaceLabels.Word(face) +
                " face - and that is where a player looks to find out why a card reads the way it " +
                "does.");
        }

        /// <summary>
        /// THE LIVE FACE, read before a player presses a card, because on this hero the answer to
        /// "what will this card do" is not on the card alone.
        ///
        /// <para><b>Milestone 8.75e re-stated this.</b> Milestone 8.75c read it off a corner badge
        /// milestone 8.75b drew on every two-text card; the user dropped that badge after the first
        /// live playtest ("Drop the badge and rely on the coin strip alone"), so the reading is taken
        /// where a player now takes it: the match's own coin, through
        /// <see cref="CoinSelectedTextQuery.LiveFace"/> over the seat's projected
        /// <c>TieBreakerState</c>, and the rail's tie-breaker banner, which is the coin ON SCREEN.
        /// Both halves are asserted - the authority's fact and the pixels that carry it - so a journey
        /// still proves which text is live before it presses, and cannot pass because something is
        /// missing: the card itself is found first, and <see cref="HerTrayFace"/> fails unless exactly
        /// one shown tray card is printing that name.</para>
        /// </summary>
        internal static void AssertLiveFace(
            MatchSceneCompositionRoot root, string printedName, PrintedTextFace face, bool chooses)
        {
            var runtime = root.Runtime;

            // The card is found on screen first. A read that passed because the face was absent
            // would prove nothing at all, and `HerTrayFace` itself fails unless exactly one SHOWN
            // tray card is printing that name.
            HerTrayFace(root.Hud, printedName);
            Assert.That(face, Is.Not.EqualTo(PrintedTextFace.None),
                "A revealed coin selects one of '" + printedName + "'s two printed texts; an " +
                "unrevealed one selects neither, and no journey of hers is driven under one. " +
                Remaining(runtime));

            // The coin the seat is projected, and the strip that carries it. Comparing the two is the
            // whole read: the authority says which face is up and the rail's banner has to be on that
            // team's side of the screen, or a player is reading the wrong half of every card of hers.
            Assert.That(
                CoinSelectedTextQuery.LiveFace(runtime.Projection.TieBreakerState),
                Is.EqualTo(face),
                "The coin shows the " + PrintedTextFaceLabels.Word(face) + " face, which is the " +
                "half of '" + printedName + "' that will run. " + Remaining(runtime));
            AssertBannerShows(root, face);

            if (!chooses)
            {
                return;
            }

            // Equilibrium's round-long grant is standing, so the coin above is NOT what decides this
            // card: she is asked on each primary action, on the decision window's own two controls.
            // What is asserted here is WHICH card freed the round - a strictly stronger statement than
            // the query's "some card of hers declaring `FreesTheCoinChoice` is live", because exactly
            // one face of her dealt set declares it and a second one appearing would be a defect this
            // read would otherwise pass straight over.
            var freeing = runtime.Projection.ActiveEffectSourceCardInstanceIds
                .Select(id => runtime.Cards.FirstOrDefault(value => value.Id.Equals(id)))
                .Where(value => value != null && value.OwnerId.Equals(Ignatia(runtime)))
                .Select(value => value.DefinitionReference.DefinitionId)
                .ToArray();
            Assert.That(
                freeing.Any(id => id.Equals(IgnatiaCardContent.Equilibrium)),
                Is.True,
                "A freed round is HER EQUILIBRIUM standing and nothing else - it is the one face of " +
                "her set that frees the choice. Her live Active effect sources are: " +
                string.Join(", ", freeing.Select(id => id.Value)) + ". " + Remaining(runtime));
        }

        /// <summary>
        /// The same reading, taken off the CARD's own declaration: a face whose printed text the coin
        /// selects is read under the coin, and a face that does not - her Silver, which FREES the
        /// choice rather than being subject to it, and her Ultimate, which prints no action at all -
        /// is read as a card the coin does not speak for. Asserting the second half is half the claim:
        /// reading Equilibrium under the coin would be saying the coin picks which of its texts
        /// applies, and Equilibrium prints one.
        /// </summary>
        internal static void AssertLiveFaceIfDeclared(
            MatchSceneCompositionRoot root, CardDefinitionId face)
        {
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);
            var printedName = NameOf(runtime, face);
            var declared = IgnatiaCardContent.Definition(face).PrintedTextSelection;
            if (declared != PrintedTextSelection.TieBreakerCoin)
            {
                // The card is still found on screen - the read must never pass by absence - and the
                // claim made about it is the one its own declaration supports: the coin selects
                // nothing on this face.
                HerTrayFace(root.Hud, printedName);
                Assert.That(
                    new[] { IgnatiaCardContent.Equilibrium, IgnatiaCardContent.ChaosIncarnate }
                        .Any(value => value.Equals(face)),
                    Is.True,
                    "'" + printedName + "' is read without the coin, and exactly two dealt faces of " +
                    "hers may be: her Silver, which FREES the choice, and her Ultimate, which " +
                    "prints no action at all. Every other face prints two texts the coin selects, " +
                    "so meeting one here means a declaration drifted and a journey stopped reading " +
                    "the coin without anybody saying so.");
                return;
            }

            AssertLiveFace(
                root,
                printedName,
                Coin(runtime),
                CoinSelectedTextQuery.MayChooseTheFace(
                    hers,
                    runtime.Projection.ActiveEffectSourceCardInstanceIds,
                    runtime.Cards,
                    runtime.Definitions));
        }

        /// <summary>Commits one named face out of the viewed seat's own tray, through p.7's control.</summary>
        internal static IEnumerator CommitHerFaceThroughTray(
            MatchSceneCompositionRoot root, string printedName)
        {
            UiPlayer.EnsureVisible(HerTrayFace(root.Hud, printedName));
            yield return null;
            UiPlayer.Click(HerTrayFace(root.Hud, printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "CONFIRM FACEDOWN"));
            yield return null;
        }

        /// <summary>
        /// Commits her named face and one card from every other seat, through each seat's own tray. A
        /// named face is committed where the seat holds it; otherwise the seat's lowest printed
        /// Initiative, which acts last.
        /// </summary>
        internal static IEnumerator CommitEverySeatOfHers(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            CardDefinitionId enemyFace = default,
            CardDefinitionId allyFace = default)
        {
            var runtime = root.Runtime;
            yield return PointTrayAt(root, Ignatia(runtime));
            yield return CommitHerFaceThroughTray(root, NameOf(runtime, face));
            foreach (var seat in runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(Ignatia(runtime)))
                .Select(hero => hero.HeroId)
                .ToArray())
            {
                yield return PointTrayAt(root, seat);
                var wanted = seat.Equals(Ally(runtime)) ? allyFace : enemyFace;
                var named = wanted.IsDefined && HandOf(runtime, seat)
                    .Any(definition => definition.Id.Equals(wanted));
                yield return CommitHerFaceThroughTray(
                    root,
                    named ? NameOf(runtime, wanted) : LowestInitiativeCardOf(runtime, seat));
            }
        }

        /// <summary>
        /// Reads the live face of the coin - on the seat's projection and on the rail's own strip -
        /// before the card is committed, commits every seat, drives the turn to her and starts the
        /// printed action.
        /// </summary>
        internal static IEnumerator BeginIgnatia(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            ActionKind kind,
            CardDefinitionId enemyFace = default,
            CardDefinitionId allyFace = default,
            bool readMarker = true)
        {
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);
            if (readMarker)
            {
                yield return PointTrayAt(root, hers);
                AssertLiveFaceIfDeclared(root, face);
            }

            yield return CommitEverySeatOfHers(root, face, enemyFace, allyFace);
            yield return WaitForActor(root, hers);
            yield return PointTrayAt(root, hers);
            yield return StartActionThroughControl(root, kind);
            Healthy(runtime);
        }

        /// <summary>
        /// Whether the named face's printed primary action is drawn as an AVAILABLE control on her own
        /// tray, which is milestone 8.35l's greying read where a player reads it. On HER it is the
        /// reading milestone 8.75b's anticipated-outcome declaration made honest: an Attack of hers is
        /// offered exactly when the text the coin is SHOWING names somebody, and greyed when it does
        /// not - even where the other printed line would find a target.
        /// </summary>
        internal static bool PrimaryIsOffered(
            ConfiguredMatchSceneRuntime runtime, CardDefinitionId face) =>
            PrimaryOffersFor(runtime, face).Any(offer => offer.IsAvailable);

        /// <summary>Every offer the projection publishes for this face's printed primary, available or not.</summary>
        internal static ActionOffer[] PrimaryOffersFor(
            ConfiguredMatchSceneRuntime runtime, CardDefinitionId face) =>
            runtime.Projection.AvailableTurnActions.Where(offer =>
                runtime.Cards.Single(card => card.Id.Equals(offer.SourceCardInstanceId))
                    .DefinitionReference.DefinitionId.Equals(face) &&
                runtime.Definitions
                    .Single(definition =>
                        definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Any(action => action.Id.Equals(offer.ActionDefinitionId) &&
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary))
                .ToArray();

        /// <summary>
        /// Every seat's own published projection agrees about the board, about whose decision is
        /// standing, about the round - and about THE COIN, which is the half no other hero's journey has
        /// ever had to compare: her whole hand reads differently on the two faces, so a seat holding a
        /// stale coin would be drawing a different game.
        /// </summary>
        internal static void SeatsAgree(ConfiguredMatchSceneRuntime runtime, string moment)
        {
            var seats = runtime.Host.Seats.Seats.ToArray();
            Assert.That(seats, Is.Not.Empty, "A composed match has seats.");
            var readings = seats
                .Select(seat => runtime.Host.ProjectionForSeat(seat.Id))
                .Select(projection =>
                    projection.DecisionOwner + " | " +
                    projection.TieBreakerState.RevealedSide + " | " +
                    string.Join(",", projection.Heroes
                        .OrderBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                        .Select(hero => hero.HeroId.Value + "@" + hero.Coordinate)) + " | " +
                    string.Join(",", projection.Minions
                        .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                        .Select(minion => minion.MinionId.Value + "@" + minion.Coordinate)) + " | " +
                    string.Join(",", projection.PlacedTokens
                        .OrderBy(token => token.Id.Value, StringComparer.Ordinal)
                        .Select(token => token.Id.Value + "@" + token.Coordinate)) + " | " +
                    projection.RoundIndex + "/" + projection.TurnIndex)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(readings, Has.Length.EqualTo(1),
                "Every seat's projection must agree at " + moment + "; they read: " +
                string.Join(" || ", readings));
        }

        /// <summary>Every magma token standing on the board, as the authority holds it.</summary>
        internal static HexCoordinate[] MagmaSpaces(ConfiguredMatchSceneRuntime runtime) =>
            runtime.BoardView.Model.PlacedTokens
                .Where(token => token.Definition.Id.Equals(IgnatiaMagmaContent.Magma))
                .Select(token => token.Coordinate)
                .OrderBy(value => value.Q)
                .ThenBy(value => value.R)
                .ToArray();

        /// <summary>The units a standing selection is offering, as their own identities.</summary>
        internal static string[] OfferedUnits(ConfiguredMatchSceneRuntime runtime)
        {
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "No selection stands. " + Remaining(runtime));
            return choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero ||
                    candidate.Value.Kind == EffectValueKind.Minion)
                .Select(candidate => candidate.Value.Kind == EffectValueKind.Hero
                    ? candidate.Value.HeroId.Value
                    : candidate.Value.MinionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>The candidate naming this minion, for a pick that must be the printed one.</summary>
        internal static EffectChoiceCandidate CandidateFor(
            ConfiguredMatchSceneRuntime runtime, MinionId minionId) =>
            runtime.PendingEffectChoice.Candidates.Single(candidate =>
                candidate.Value.Kind == EffectValueKind.Minion &&
                candidate.Value.MinionId.Equals(minionId));

        /// <summary>The candidate naming this hero.</summary>
        internal static EffectChoiceCandidate CandidateFor(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.PendingEffectChoice.Candidates.Single(candidate =>
                candidate.Value.Kind == EffectValueKind.Hero &&
                candidate.Value.HeroId.Equals(heroId));

        /// <summary>
        /// Presses the coin-face control Equilibrium's round frees - the one pause in her set whose
        /// candidates are a labelled VOCABULARY rather than a board pick, so it is answered by the
        /// card's printed symbols in the decision window and never by an integer a player never sees.
        /// </summary>
        internal static IEnumerator PressFace(MatchSceneCompositionRoot root, PrintedTextFace face)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "No coin-face choice stands. " + Remaining(runtime));
            Assert.That(choice.NodeId.Value, Is.EqualTo(IgnatiaEffectContent.CoinGateNode),
                "The pause Equilibrium frees is the coin predicate's own. " + Remaining(runtime));
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Value.Kind == EffectValueKind.Integer &&
                command.EffectChoice.Candidate.Value.IntegerValue == (int)face);
            Assert.That(control, Is.Not.Null,
                "The decision window offers no control for the " + face + " text. The shown " +
                "controls are: " + UiPlayer.Describe(root.Hud));
            Assert.That(
                control.Label,
                Is.EqualTo(face == PrintedTextFace.Sun
                    ? PrintedTextFaceLabels.ApplySunTextLabel
                    : PrintedTextFaceLabels.ApplyMoonTextLabel),
                "and the control uses the card's own symbol for the text it applies.");
            Assert.That(control.Label, Does.Not.Contain("SUN").And.Not.Contain("MOON"),
                "the card's printed symbols must replace the confusing face names.");
            UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
            yield return null;
            yield return null;
            Healthy(runtime);
        }

        /// <summary>One card of this hero's, by the face it prints - the handle an Active effect is read by.</summary>
        internal static CardInstanceId CardIdOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definition) =>
            runtime.Cards
                .Single(card => card.OwnerId.Equals(heroId) &&
                    card.DefinitionReference.DefinitionId.Equals(definition))
                .Id;

        /// <summary>Every card of this hero's currently in hand, as definition ids.</summary>
        internal static string[] HeldBy(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// Ends the WHOLE turn - every actor in rulebook p.7's Initiative order, each on the control the
        /// round strip draws them - and stops the moment a pause opens or the turn is over. It is the
        /// whole turn and not her own because a ROUND of hers holds several turns, and what Equilibrium
        /// frees is every later one of them.
        /// </summary>
        internal static IEnumerator EndTheWholeTurn(
            MatchSceneCompositionRoot root, int presses = 10)
        {
            var runtime = root.Runtime;
            for (var press = 0; press < presses; press++)
            {
                if (runtime.PendingEffectChoice != null ||
                    runtime.Phase != RoundResolutionPhase.ResolvingTurn)
                {
                    yield break;
                }

                if (runtime.RoundView.Model.EndTurnInvocation != null)
                {
                    UiPlayer.Click(
                        UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                }
                else if (!UiPlayer.ReleaseFinishedTurn(runtime))
                {
                    yield break;
                }

                yield return null;
                yield return null;
            }
        }

        /// <summary>
        /// One filler decision, taken the way <c>NextRoundDecision</c> takes one - except that a
        /// COMMITMENT is submitted through the projected command rather than through the tray, because
        /// the shared filler commits by printed NAME and three faces of hers share two words.
        /// </summary>
        internal static IEnumerator NextDecision(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var awaiting = runtime.Projection.Commitments
                .FirstOrDefault(value =>
                    value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            var held = !awaiting.HeroId.IsDefined
                ? null
                : runtime.Cards.FirstOrDefault(card => card.OwnerId.Equals(awaiting.HeroId) &&
                    card.State.Location == CardLocation.Hand);
            if (held != null)
            {
                Assert.That(
                    runtime.SubmitProjectedCommand(
                        DirectorCommand.CommitCard(awaiting.HeroId, held.Id)),
                    Is.True,
                    "A seat awaiting p.7's commitment can always commit a card it holds. " +
                    Remaining(runtime));
                yield return null;
                yield break;
            }

            if (awaiting.HeroId.IsDefined)
            {
                Assert.That(
                    runtime.SubmitProjectedCommand(DirectorCommand.Pass(awaiting.HeroId)),
                    Is.True,
                    "A seat holding nothing can always pass. " + Remaining(runtime));
                yield return null;
                yield break;
            }

            yield return NextRoundDecision(root);
        }

        /// <summary>Drives whole rounds until the predicate holds, or fails naming what stood.</summary>
        internal static IEnumerator DriveUntil(
            MatchSceneCompositionRoot root, Func<bool> until, string what, int budget = 320)
        {
            for (var step = 0; step < budget && !until(); step++)
            {
                yield return NextDecision(root);
            }

            Assert.That(until(), Is.True,
                "The filler never reached " + what + ". " + Remaining(root.Runtime));
        }

        /// <summary>
        /// Commits every seat again and drives the turn back to her, for the SECOND turn of a round -
        /// which is where Equilibrium's round-long free choice is read and where a magma token she laid
        /// on her first turn is still standing.
        /// </summary>
        internal static IEnumerator HerNextTurnOfTheSameRound(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            ActionKind kind,
            CardDefinitionId enemyFace = default,
            CardDefinitionId allyFace = default,
            bool startAction = true)
        {
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);
            var round = runtime.Projection.RoundIndex;
            yield return EndTheWholeTurn(root);

            // Only turn-ending controls are pressed here, never a commitment: the shared filler
            // commits for whichever seat is awaiting, and on this path that seat may be HERS - which
            // would spend the very card the journey is about on a turn nobody was watching.
            bool AwaitingHerCommitment() => runtime.Projection.Commitments.Any(value =>
                value.HeroId.Equals(hers) &&
                value.Status == TurnCardCommitmentStatus.AwaitingResponse);
            for (var budget = 0; budget < 30 && !AwaitingHerCommitment(); budget++)
            {
                if (runtime.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    if (runtime.RoundView.Model.EndTurnInvocation != null)
                    {
                        UiPlayer.Click(UiPlayer.Button(
                            root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                    }
                    else
                    {
                        UiPlayer.ReleaseFinishedTurn(runtime);
                    }
                }

                yield return null;
            }

            Assert.That(AwaitingHerCommitment(), Is.True,
                "p.7 opens the next turn of the same round with her own commitment. " +
                Remaining(runtime));
            Assert.That(runtime.Projection.RoundIndex, Is.EqualTo(round),
                "Arrange: the next turn is in the SAME round, which is what \"This round:\" and a " +
                "token's end-of-round cleanup are both about. " + Remaining(runtime));
            yield return PointTrayAt(root, Ignatia(runtime));
            AssertLiveFaceIfDeclared(root, face);
            yield return CommitEverySeatOfHers(root, face, enemyFace, allyFace);
            yield return WaitForActor(root, Ignatia(runtime));
            yield return PointTrayAt(root, Ignatia(runtime));
            if (startAction)
            {
                yield return StartActionThroughControl(root, kind);
            }

            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The component half her piece owns, on her own dealt board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her own token stands at the configured Blue spawn as Ignatia and only once, a camera ray on
        /// it points the tray at her, the coin strip says which half of her two-text cards is live, and
        /// her tray commits a face through rulebook p.7's own reveal.
        ///
        /// <para>The other half of her identity - that the 8.35z86 picker finds her by her FOUR stars
        /// and each of her FIVE printed roles, draws her own pick card on hover, hides her for a role
        /// she does not carry and takes her through PICK HERO - lives in the lobby's own fixture, which
        /// is a different scene. Her piece row names both cases beside this one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerPieceSpawnsAsIgnatiaAndHerTrayCommitsAndRevealsACard()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[]
                {
                    IgnatiaCardContent.ChaosBolt, IgnatiaCardContent.Equilibrium,
                    IgnatiaCardContent.PlayingWithFire, IgnatiaCardContent.PathOfAshes
                });
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);

            var pieces = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                .Where(view => view.Occupancy.HeroId.Equals(hers))
                .ToArray();
            Assert.That(pieces, Has.Length.EqualTo(1),
                "One hero, one piece - a second would be a view the rebuild failed to retire.");
            Assert.That(pieces[0].Coordinate,
                Is.EqualTo(runtime.Heroes.Single(hero => hero.HeroId.Equals(hers)).Coordinate),
                "and it stands where the authority says she does.");
            Assert.That(
                runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(hers)).SpawnPoint.ToString(),
                Does.Contain("blue-hero-01"),
                "The staged engagement preserves her configured spawn point, which is what p.13's " +
                "respawn later returns her to.");

            yield return PointTrayAt(root, hers);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(hers),
                "A camera ray on her own piece points the tray at her.");
            Assert.That(Coin(runtime), Is.EqualTo(PrintedTextFace.Sun),
                "Arrange: this staging reveals the coin's SUN face, which is Blue (ID-Q5). " +
                Remaining(runtime));
            foreach (var face in new[]
            {
                IgnatiaCardContent.ChaosBolt, IgnatiaCardContent.PlayingWithFire,
                IgnatiaCardContent.PathOfAshes
            })
            {
                AssertLiveFace(root, NameOf(runtime, face), PrintedTextFace.Sun, chooses: false);
            }

            yield return CommitHerFaceThroughTray(
                root, NameOf(runtime, IgnatiaCardContent.ChaosBolt));
            Assert.That(runtime.GetCommitmentStatus(hers),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown).Or
                    .EqualTo(TurnCardCommitmentStatus.Revealed),
                "CONFIRM FACEDOWN puts her card in p.7's turn area. " + Remaining(runtime));
            foreach (var seat in Enemies(runtime))
            {
                yield return PointTrayAt(root, seat);
                yield return CommitHerFaceThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }

            Assert.That(runtime.GetCommitmentStatus(hers),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed),
                "and the reveal follows the last seat's commitment. " + Remaining(runtime));
            SeatsAgree(runtime, "the reveal");
            Healthy(runtime);
        }

        /// <summary>
        /// Every face she prints that carries rulebook p.10's secondary Movement ribbon walks the board
        /// through its own card face, and every face she prints that carries p.14's secondary Defense
        /// shield is offered in the defender's own prompt.
        ///
        /// <para>IG-06 splits her set exactly once: <b>Equilibrium prints Defense 7 and NO Movement
        /// ribbon at all</b>, so the walk half skips it and the defend half does not. Chaos Incarnate
        /// prints no values and no action whatsoever - it cannot be committed, walked or discarded to
        /// defend - so the loop runs over the seventeen dealt faces that print an action.</para>
        ///
        /// <para>The loop runs on the Tier I scene for every face, including her Tier II and Tier III
        /// ones: a staged hand may take a card out of its owner's own DECK, so the secondary of a face
        /// she has not levelled into is still her own printed secondary on her own card.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace()
        {
            foreach (var definition in IgnatiaCardContent.DealtDefinitions
                .Where(card => card.ActionDefinitions.Count > 0))
            {
                var walks = definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary);
                var defends = definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction);
                Assert.That(defends, Is.True,
                    definition.DisplayName + " prints p.14's secondary Defense shield, which every " +
                    "dealt face of hers printing an action does.");
                Assert.That(
                    walks,
                    definition.Id.Equals(IgnatiaCardContent.Equilibrium) ? Is.False : Is.True,
                    "IG-06: Equilibrium is the one dealt face of hers printing no Movement ribbon, " +
                    "and a missing icon is absent rather than zero.");

                // The other card in hand is never the one under test and always acts after the
                // enemy's Shock (printed Initiative 8), so the attack she answers below is the
                // enemy's own turn. Equilibrium prints 1, the lowest in her set.
                var committed = definition.Id.Equals(IgnatiaCardContent.Equilibrium)
                    ? IgnatiaCardContent.PathOfAshes
                    : IgnatiaCardContent.Equilibrium;

                if (walks)
                {
                    yield return StageIgnatia(
                        SceneCardContent.VerifiedEngagedStagedIgnatia,
                        hand: new[] { definition.Id, committed });
                    var root = Root();
                    var runtime = root.Runtime;
                    var hers = Ignatia(runtime);
                    yield return BeginIgnatia(
                        root, definition.Id, ActionKind.Movement, readMarker: false);
                    Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                        definition.DisplayName + "'s printed boot opens a walk. " +
                        Remaining(runtime));
                    var step = FirstClickableSpace(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        runtime.PendingMovementChoice.LegalDestinations
                            .Where(value => value.Cost == 1)
                            .Select(value => value.Destination)
                            .ToArray());
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            Tile(step).transform.position),
                        Is.True,
                        definition.DisplayName + " must accept a board click on a lit step.");
                    yield return null;
                    Assert.That(Position(runtime, hers), Is.EqualTo(step),
                        "and her piece follows the authoritative space. " + Remaining(runtime));
                    Assert.That(HeroView(hers).Coordinate, Is.EqualTo(step));
                    Healthy(runtime);
                }

                yield return StageIgnatia(
                    SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[] { definition.Id, committed },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    enemySpace: AdjacentEnemy);
                var defenseRoot = Root();
                var defenseRuntime = defenseRoot.Runtime;
                var defender = Ignatia(defenseRuntime);
                var enemy = Enemies(defenseRuntime)[0];
                yield return PointTrayAt(defenseRoot, defender);
                yield return CommitHerFaceThroughTray(
                    defenseRoot, NameOf(defenseRuntime, committed));
                yield return PointTrayAt(defenseRoot, enemy);
                yield return CommitHerFaceThroughTray(
                    defenseRoot, NameOf(defenseRuntime, WaspCardContent.Shock));
                yield return YieldUntilTheNamedSeatActs(defenseRuntime, enemy);
                yield return PointTrayAt(defenseRoot, enemy);
                yield return StartActionThroughControl(defenseRoot, ActionKind.Attack);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        defenseRoot.Hud,
                        HeroView(defender).transform.position),
                    Is.True);
                yield return null;
                Assert.That(
                    defenseRuntime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                        offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)),
                    Is.True,
                    definition.DisplayName + " is offered as a p.14 Defense. " +
                    Remaining(defenseRuntime));
                yield return ClickDefenseFace(defenseRoot, definition.Id);
                Assert.That(defenseRuntime.PendingDefenseChoice, Is.Null);
                Assert.That(
                    defenseRuntime.Cards.Single(card =>
                        card.OwnerId.Equals(defender) &&
                        card.DefinitionReference.DefinitionId.Equals(definition.Id))
                        .State.Location,
                    Is.EqualTo(CardLocation.DiscardPile),
                    "and answering with it spends it into her own discard pile.");
                Healthy(defenseRuntime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Gold: the coin picks the target itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Chaos Bolt: "☀: Target a minion adjacent to you. ☾: Target a hero in range."
        ///
        /// <para><b>One card, one board, two faces - and the two texts name DISJOINT sets.</b> Under the
        /// sun the only candidate is the enemy ranged minion the engagement leaves in her ring (the
        /// friendly melee minion beside her is not an enemy, and p.17's Friendly is never a target of
        /// an Attack of hers); under the moon it is the enemy HERO two away, inside the printed Range 3
        /// and standing in nobody's ring.</para>
        ///
        /// <para><b>And the third board is the one the offer REFUSES.</b> With the enemy ranged minion
        /// staged out of her ring, the sun text names nobody at all - so milestone 8.35z15's rule, read
        /// through milestone 8.75b's anticipated-outcome declaration, greys her printed Attack rather
        /// than letting a player spend the card on an empty sentence. The same board under the MOON
        /// offers it, which is what says the greying is about the LIVE text and not about the card.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ChaosBoltTakesTheAdjacentMinionUnderTheSunAndTheHeroInRangeUnderTheMoon()
        {
            // ☀ "a minion adjacent to you".
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.ChaosBolt, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var sunRoot = Root();
            var sunRuntime = sunRoot.Runtime;
            Assert.That(Coin(sunRuntime), Is.EqualTo(PrintedTextFace.Sun), Remaining(sunRuntime));

            yield return BeginIgnatia(
                sunRoot, IgnatiaCardContent.ChaosBolt, ActionKind.Attack, WaspCardContent.Shock);
            var sun = sunRuntime.PendingEffectChoice;
            Assert.That(sun, Is.Not.Null,
                "Her Attack reaches p.14's step 1 with no target, so the card text is what names " +
                "one. " + Remaining(sunRuntime));
            Assert.That(sun.NodeId.Value, Is.EqualTo("ignatia-attack-sun-select"),
                "and the arm the coin chose is the SUN one. " + Remaining(sunRuntime));
            Assert.That(OfferedUnits(sunRuntime),
                Is.EqualTo(new[] { RedRanged.Value }).AsCollection,
                "\"a MINION adjacent to you\" - the enemy ranged minion in her ring is the whole " +
                "offer, and the friendly melee minion beside her is not an enemy. " +
                Remaining(sunRuntime));
            yield return Pick(sunRoot, CandidateFor(sunRuntime, RedRanged));
            yield return null;
            yield return null;
            Assert.That(sunRuntime.PendingEffectChoice, Is.Null,
                "Binding the target finishes the card text. " + Remaining(sunRuntime));
            // THE ATTACK'S OWN OUTCOME (milestone 8.86). This journey bound the target and then
            // asserted only that nothing more was asked, so an Attack whose card text named the
            // right minion and then applied nothing would have passed it.
            Assert.That(
                sunRuntime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                Is.False,
                "☀ \"Target a minion adjacent to you\" - p.14 defeats an attacked minion without a " +
                "Defense step, so the piece her card text named leaves the board. " +
                Remaining(sunRuntime));
            SeatsAgree(sunRuntime, "Chaos Bolt's sun text");
            Healthy(sunRuntime);

            // ☾ "a hero in range".
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[] { IgnatiaCardContent.ChaosBolt, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var moonRoot = Root();
            var moonRuntime = moonRoot.Runtime;
            var moonEnemy = Enemies(moonRuntime)[0];
            Assert.That(Coin(moonRuntime), Is.EqualTo(PrintedTextFace.Moon),
                "Arrange: the same board with the coin's MOON face up, which is Red (ID-Q5). " +
                Remaining(moonRuntime));

            yield return BeginIgnatia(
                moonRoot, IgnatiaCardContent.ChaosBolt, ActionKind.Attack, WaspCardContent.Shock);
            var moon = moonRuntime.PendingEffectChoice;
            Assert.That(moon, Is.Not.Null, Remaining(moonRuntime));
            Assert.That(moon.NodeId.Value, Is.EqualTo("ignatia-attack-moon-select"),
                "The coin turned over is the only difference between the two runs, and the other " +
                "arm is what ran. " + Remaining(moonRuntime));
            Assert.That(OfferedUnits(moonRuntime),
                Is.EqualTo(new[] { moonEnemy.Value }).AsCollection,
                "\"a HERO in range\" - the enemy hero two away, and no minion at all. " +
                Remaining(moonRuntime));
            yield return Pick(moonRoot, CandidateFor(moonRuntime, moonEnemy));
            yield return null;
            yield return ClickDefenseFace(moonRoot, WaspCardContent.Electrocute);
            yield return null;
            yield return null;
            // THE ATTACK'S OWN OUTCOME (milestone 8.86): the hero her moon text named really answered
            // p.14, and the face they answered with is spent into their own discard pile.
            Assert.That(
                moonRuntime.Cards.Single(card =>
                    card.OwnerId.Equals(moonEnemy) &&
                    card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "☾ \"Target a hero in range\" - the Attack reached p.14's Defense window and the " +
                "defender spent the face they answered with. " + Remaining(moonRuntime));
            Assert.That(moonRuntime.PendingDefenseChoice, Is.Null, Remaining(moonRuntime));
            SeatsAgree(moonRuntime, "Chaos Bolt's moon text");
            Healthy(moonRuntime);

            // The refusal: the same sun board with her ring emptied of enemies.
            foreach (var moonFace in new[] { false, true })
            {
                yield return StageIgnatia(
                    moonFace
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[] { IgnatiaCardContent.ChaosBolt, IgnatiaCardContent.Equilibrium },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    movedMinions: new[]
                    {
                        new KeyValuePair<MinionId, HexCoordinate>(
                            RedRanged, new HexCoordinate(-5, 3))
                    });
                var greyRoot = Root();
                var greyRuntime = greyRoot.Runtime;
                var her = Ignatia(greyRuntime);
                var herSide = greyRuntime.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(her)).TeamSide;
                Assert.That(
                    greyRuntime.ReadModel.Minions.Any(minion =>
                        Distance(minion.Coordinate, Position(greyRuntime, her)) == 1 &&
                        minion.TeamSide != herSide),
                    Is.False,
                    "Arrange: no ENEMY minion stands in her ring, so the sun text names nobody. " +
                    Remaining(greyRuntime));

                yield return CommitEverySeatOfHers(
                    greyRoot, IgnatiaCardContent.ChaosBolt, WaspCardContent.Shock);
                yield return WaitForActor(greyRoot, her);
                yield return PointTrayAt(greyRoot, her);

                Assert.That(PrimaryOffersFor(greyRuntime, IgnatiaCardContent.ChaosBolt),
                    Is.Not.Empty,
                    "The card is revealed and its printed primary is DRAWN either way - milestone " +
                    "8.35l greys an action rather than hiding it. " + Remaining(greyRuntime));
                Assert.That(
                    PrimaryIsOffered(greyRuntime, IgnatiaCardContent.ChaosBolt),
                    moonFace ? Is.True : Is.False,
                    moonFace
                        ? "\"a hero in range\" still names the enemy hero two away, so the SAME " +
                            "board offers the same card under the other face. "
                        : "\"a minion adjacent to you\" names nobody on this board, and milestone " +
                            "8.75b's anticipated outcome makes the offer read the LIVE text alone " +
                            "rather than either printed line. " + Remaining(greyRuntime));
                Healthy(greyRuntime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Silver: the round that takes the choice away from the coin
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Equilibrium: "This round: Each time you perform or repeat a primary action you may apply
        /// either ☀ or ☾ card text, regardless of the Tie Breaker coin."
        ///
        /// <para><b>A ROUND OF HERS HOLDS SEVERAL TURNS, which is what makes the sentence reachable.</b>
        /// Rulebook p.7 commits and resolves one card per TURN and p.8 ends the ROUND when the hands
        /// are spent, so the marker Equilibrium stands at its own ActionEnded covers every later turn of
        /// that round. This journey plays it on one turn and reads the choice on the next - measured
        /// offline before it was written, over sixteen searched self-play runs of the staged Tier III
        /// board, where the coin-gate pause opens on exactly those turns and on no others.</para>
        ///
        /// <para><b>The card is read on the tray before it is pressed.</b> While the marker stands, the
        /// card that raised it is lying faceup wearing <c>ACTIVE</c> - which, since milestone 8.75e
        /// dropped the corner badge, is the difference a player sees on screen between a freed round
        /// and an ordinary one. The face itself is then named in the decision window, on its own two
        /// controls, each time she performs or repeats a primary action.</para>
        ///
        /// <para><b>And the face she picks is the one that runs, against the coin.</b> On the sun board
        /// she applies the MOON text and the in-a-straight-line subjects are what she is offered; on the
        /// moon board she applies the SUN text and gets their complement. Either printed line stays
        /// reachable whatever the coin shows, which is the whole of "regardless of the Tie Breaker
        /// coin".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EquilibriumFreesTheRoundAndTheFaceSheAppliesIsTheOneThatRuns()
        {
            foreach (var onTheMoon in new[] { false, true })
            {
                var applied = onTheMoon ? PrintedTextFace.Sun : PrintedTextFace.Moon;
                yield return StageIgnatia(
                    onTheMoon
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.Equilibrium, IgnatiaCardContent.PlayingWithFire,
                        IgnatiaCardContent.ChaosBolt
                    },
                    enemyHand: new[]
                    {
                        WaspCardContent.Shock, WaspCardContent.Electrocute,
                        WaspCardContent.KineticBlast
                    });
                var root = Root();
                var runtime = root.Runtime;
                var hers = Ignatia(runtime);
                var round = runtime.Projection.RoundIndex;
                Assert.That(Coin(runtime),
                    Is.EqualTo(onTheMoon ? PrintedTextFace.Moon : PrintedTextFace.Sun),
                    Remaining(runtime));

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.Equilibrium, ActionKind.Skill, WaspCardContent.Shock);
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "Equilibrium asks nobody anything: the whole sentence is the marker it stands. " +
                    Remaining(runtime));
                Assert.That(
                    runtime.IsActiveEffectSource(
                        CardIdOf(runtime, hers, IgnatiaCardContent.Equilibrium)),
                    Is.True,
                    "and the card that printed it carries the Active effect that holds it, where a " +
                    "player reads it. " + Remaining(runtime));
                SeatsAgree(runtime, "Equilibrium's marker");

                yield return HerNextTurnOfTheSameRound(
                    root, IgnatiaCardContent.PlayingWithFire, ActionKind.Attack,
                    WaspCardContent.Electrocute, startAction: false);
                Assert.That(runtime.Projection.RoundIndex, Is.EqualTo(round));
                Assert.That(
                    runtime.IsActiveEffectSource(
                        CardIdOf(runtime, hers, IgnatiaCardContent.Equilibrium)),
                    Is.True,
                    "\"This round:\" - the marker is still standing on the next turn of the same " +
                    "round. " + Remaining(runtime));

                yield return StartActionThroughControl(root, ActionKind.Attack);
                var gate = runtime.PendingEffectChoice;
                Assert.That(gate, Is.Not.Null,
                    "and the coin predicate PAUSES instead of reading the coin. " +
                    Remaining(runtime));
                Assert.That(gate.NodeId.Value, Is.EqualTo(IgnatiaEffectContent.CoinGateNode));
                Assert.That(gate.ChooserId, Is.EqualTo(hers),
                    "\"you may apply\" - the choice is the acting hero's own. " + Remaining(runtime));
                Assert.That(
                    gate.Candidates.Select(candidate => candidate.Value.IntegerValue)
                        .OrderBy(value => value).ToArray(),
                    Is.EqualTo(new[] { (int)PrintedTextFace.Sun, (int)PrintedTextFace.Moon }
                        .OrderBy(value => value).ToArray()).AsCollection,
                    "Both printed lines stay reachable whatever the coin shows. " +
                    Remaining(runtime));
                Assert.That(
                    runtime.DecisionWindowView.Model, Is.Not.Null,
                    "THE RENDERED TIER DRAWS IT, which is the half a headless case cannot see. " +
                    Remaining(runtime));

                yield return PressFace(root, applied);
                var subject = runtime.PendingEffectChoice;
                Assert.That(subject, Is.Not.Null,
                    "Applying a face runs that printed line, which asks for its own subject. " +
                    Remaining(runtime));
                Assert.That(
                    subject.NodeId.Value,
                    Is.EqualTo(applied == PrintedTextFace.Sun
                        ? "ignatia-attack-sun-select"
                        : "ignatia-attack-moon-select"),
                    "and the arm that ran is the one she APPLIED rather than the one the coin shows " +
                    "- which is the whole of \"regardless of the Tie Breaker coin\". " +
                    Remaining(runtime));
                Assert.That(
                    OfferedUnits(runtime),
                    applied == PrintedTextFace.Sun
                        ? Is.EqualTo(new[]
                            {
                                Enemies(runtime)[0].Value, RedMeleeOffEveryFile.Value
                            }.OrderBy(value => value, StringComparer.Ordinal).ToArray())
                            .AsCollection
                        : Is.EqualTo(new[]
                            {
                                RedMeleeOnAFile.Value, RedRanged.Value
                            }.OrderBy(value => value, StringComparer.Ordinal).ToArray())
                            .AsCollection,
                    "Playing with Fire's two printed lines name DISJOINT sets inside its Range 2 - " +
                    "\"not in a straight line\" and \"in a straight line\" are complements. " +
                    Remaining(runtime));
                SeatsAgree(runtime, "the applied " + applied + " text");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Red Tier I: the straight line, and its complement
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Playing with Fire: "☀: Target a unit in range not in a straight line. ☾: Target a unit in
        /// range in a straight line."
        ///
        /// <para><b>The two printed lines are COMPLEMENTS, so a board proving one by its refusal proves
        /// nothing.</b> Milestone 8.75b measured the engagement for exactly this: inside the printed
        /// Range 2 the enemy melee at (-1,-1) and the enemy ranged minion at (-4,2) stand ON one of her
        /// six files, and the enemy hero at (-2,2) and the enemy melee at (-1,0) stand OFF every one of
        /// them. So each arm has a candidate the other cannot name, and the two offers below are exact
        /// sets rather than samples.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PlayingWithFireTakesTheUnitOffEveryFileAndTheOneOnAFile()
        {
            foreach (var onTheMoon in new[] { false, true })
            {
                yield return StageIgnatia(
                    onTheMoon
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.PlayingWithFire, IgnatiaCardContent.Equilibrium
                    },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Enemies(runtime)[0];

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.PlayingWithFire, ActionKind.Attack,
                    WaspCardContent.Shock);
                var choice = runtime.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, Remaining(runtime));
                Assert.That(
                    choice.NodeId.Value,
                    Is.EqualTo(onTheMoon
                        ? "ignatia-attack-moon-select"
                        : "ignatia-attack-sun-select"),
                    Remaining(runtime));
                Assert.That(
                    OfferedUnits(runtime),
                    onTheMoon
                        ? Is.EqualTo(new[] { RedMeleeOnAFile.Value, RedRanged.Value }
                            .OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection
                        : Is.EqualTo(new[] { enemy.Value, RedMeleeOffEveryFile.Value }
                            .OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection,
                    (onTheMoon ? "☾ \"in a straight line\"" : "☀ \"not in a straight line\"") +
                    ", inside the printed Range 2 and on the opposing side alone. " +
                    Remaining(runtime));

                if (onTheMoon)
                {
                    yield return Pick(root, CandidateFor(runtime, RedRanged));
                    yield return null;
                    yield return null;
                    // THE ATTACK'S OWN OUTCOME (milestone 8.86): the straight-line arm's target is
                    // defeated, so the arm did something rather than only offering a set.
                    Assert.That(
                        runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(RedRanged)),
                        Is.False,
                        "☾ the minion on her file is defeated by the Attack that named it. " +
                        Remaining(runtime));
                }
                else
                {
                    yield return Pick(root, CandidateFor(runtime, enemy));
                    yield return null;
                    yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                    yield return null;
                    yield return null;
                    Assert.That(
                        runtime.Cards.Single(card =>
                            card.OwnerId.Equals(enemy) &&
                            card.DefinitionReference.DefinitionId.Equals(
                                WaspCardContent.Electrocute)).State.Location,
                        Is.EqualTo(CardLocation.DiscardPile),
                        "☀ the hero off every file really answered p.14, and the face is spent. " +
                        Remaining(runtime));
                }

                yield return null;
                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "Playing with Fire prints no second clause on either line. " + Remaining(runtime));
                SeatsAgree(runtime, onTheMoon ? "the straight line" : "its complement");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Blue Tier I: the discard, and the minion beside the hero
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Abrupt Combustion: "☀: An enemy hero in radius, adjacent to a token, or a minion, discards a
        /// card, if able. ☾: Remove an enemy minion in radius adjacent to an enemy hero."
        ///
        /// <para><b>The board is restaged once, and the reason is measured.</b> Milestone 8.75b recorded
        /// that at setup no enemy minion stands in the enemy hero's ring - the waves begin in their
        /// lanes - so the MOON line names nobody on the untouched engagement. Standing the enemy hero at
        /// (-1,1) puts him beside the enemy melee minion already at (-1,0) and leaves him inside the
        /// printed Radius 3, which gives both printed lines a subject on ONE board.</para>
        ///
        /// <para><b>ID-Q9c, settled here: the "adjacent to a TOKEN" half of the sun clause is not
        /// reachable through a configured match's own play, and it is not asserted as if it were.</b>
        /// The only tokens on this board are her own magma, her Green line is the only thing that places
        /// them, a hero performs one primary action per turn, and p.19 removes every token at the end of
        /// the round - so placing a token beside an enemy hero and then playing a discard card is two
        /// primary actions inside one turn, which no rule of this build grants. The clause is a
        /// DISJUNCTION and its minion half is what a player reaches; the token half is proved at the
        /// authoritative tier, on a board built by hand, by
        /// <c>IgnatiaDiscardTests.TheSunLineNamesOnlyAnEnemyHeroBesideATokenOrAMinion</c> and
        /// <c>AForeignTokenSatisfiesThePrintedWordJustAsHerMagmaDoes</c>.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AbruptCombustionMakesTheQualifyingHeroDiscardAndRemovesTheQualifyingMinion()
        {
            foreach (var onTheMoon in new[] { false, true })
            {
                yield return StageIgnatia(
                    onTheMoon
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.AbruptCombustion, IgnatiaCardContent.Equilibrium
                    },
                    enemyHand: new[]
                    {
                        WaspCardContent.Shock, WaspCardContent.Electrocute,
                        WaspCardContent.KineticBlast
                    },
                    enemySpace: EnemyOnHerFile);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Enemies(runtime)[0];
                Assert.That(
                    Distance(
                        Position(runtime, enemy),
                        runtime.ReadModel.Minions.Single(minion =>
                            minion.MinionId.Equals(RedMeleeOffEveryFile)).Coordinate),
                    Is.EqualTo(1),
                    "Arrange: the enemy melee minion stands in the enemy HERO's ring, which is what " +
                    "both printed lines read. " + Remaining(runtime));

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.AbruptCombustion, ActionKind.Skill,
                    WaspCardContent.Shock);
                var choice = runtime.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, Remaining(runtime));

                if (onTheMoon)
                {
                    Assert.That(choice.NodeId.Value, Is.EqualTo("ignatia-discard-moon-select"));
                    Assert.That(OfferedUnits(runtime),
                        Is.EqualTo(new[] { RedMeleeOffEveryFile.Value }).AsCollection,
                        "☾ \"an enemy MINION in radius adjacent to an enemy hero\" - one candidate, " +
                        "and the enemy hero himself is not one of them. " + Remaining(runtime));
                    var coins = runtime.CoinsOf(Ignatia(runtime));
                    yield return Pick(root, CandidateFor(runtime, RedMeleeOffEveryFile));
                    yield return null;
                    Assert.That(
                        runtime.ReadModel.Minions.Any(minion =>
                            minion.MinionId.Equals(RedMeleeOffEveryFile)),
                        Is.False,
                        "and p.19's Remove takes it off the board. " + Remaining(runtime));
                    Assert.That(runtime.CoinsOf(Ignatia(runtime)), Is.EqualTo(coins),
                        "A REMOVE is not p.13's Defeat, so nobody is paid for it. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(choice.NodeId.Value, Is.EqualTo("ignatia-discard-sun-select"));
                    Assert.That(OfferedUnits(runtime),
                        Is.EqualTo(new[] { enemy.Value }).AsCollection,
                        "☀ \"an enemy HERO in radius, adjacent to a token, or a minion\" - the one " +
                        "hero standing beside the minion. " + Remaining(runtime));
                    var held = HeldBy(runtime, enemy).Length;
                    yield return Pick(root, CandidateFor(runtime, enemy));
                    var discard = runtime.PendingEffectChoice;
                    Assert.That(discard, Is.Not.Null,
                        "\"discards a card\" - WHICH card is the discarding hero's own decision. " +
                        Remaining(runtime));
                    Assert.That(discard.ChooserId, Is.EqualTo(enemy),
                        "and it is asked of that hero's seat rather than of hers. " +
                        Remaining(runtime));
                    yield return ClickACardFace(root);
                    Assert.That(HeldBy(runtime, enemy), Has.Length.EqualTo(held - 1),
                        "and exactly one card left the named hero's hand. " + Remaining(runtime));
                }

                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, onTheMoon ? "the removed minion" : "the forced discard");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Her Green Tier I: the wake, and the scatter
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Path of Ashes, ☀: "Move up to 2 spaces in a straight line. Place a magma token in each empty
        /// space you moved through, or out of."
        ///
        /// <para><b>Three things a player can see, and the third is what makes a magma token a token.</b>
        /// The straight-line offer lights only the ends of her own free files - never a space reachable
        /// by turning a corner, which is what "in a straight line" means and what milestone 8.75b's
        /// <c>subjectSpace</c> correction is about; the wake then drops a token in the space she moved
        /// OUT OF and in each space she moved THROUGH, leaving the one she now stands on clear; and on
        /// the NEXT TURN OF THE SAME ROUND those spaces are closed to her own Movement, because p.18
        /// makes every token an obstacle and a magma token declares no move-through clause at all.</para>
        ///
        /// <para>And p.19's own sentence ends it: "Unless the card text says otherwise, tokens are
        /// removed at the end of round." Path of Ashes says nothing otherwise, so the round's end takes
        /// them back to her supply of three.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PathOfAshesLaysItsWakeAndTheMagmaClosesTheFileUntilTheRoundEnds()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[]
                {
                    IgnatiaCardContent.PathOfAshes, IgnatiaCardContent.PlayingWithFire,
                    IgnatiaCardContent.Equilibrium
                },
                enemyHand: new[]
                {
                    WaspCardContent.Shock, WaspCardContent.Electrocute, WaspCardContent.KineticBlast
                });
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);
            var round = runtime.Projection.RoundIndex;
            Assert.That(MagmaSpaces(runtime), Is.Empty, "Arrange: no token stands yet.");

            yield return BeginIgnatia(
                root, IgnatiaCardContent.PathOfAshes, ActionKind.Skill, WaspCardContent.Shock);
            var line = runtime.PendingEffectChoice;
            Assert.That(line, Is.Not.Null, Remaining(runtime));
            Assert.That(line.NodeId.Value, Is.EqualTo("ignatia-magma-sun-line"));
            Assert.That(
                OfferedSpaces(runtime).OrderBy(value => value.Q).ThenBy(value => value.R).ToArray(),
                Is.EqualTo(new[]
                    {
                        new HexCoordinate(-5, 1), new HexCoordinate(-4, 1),
                        new HexCoordinate(-3, -1), new HexCoordinate(-3, 0),
                        new HexCoordinate(-2, 0), new HexCoordinate(-2, 1),
                        new HexCoordinate(-1, 1)
                    }.OrderBy(value => value.Q).ThenBy(value => value.R).ToArray()).AsCollection,
                "\"up to 2 spaces IN A STRAIGHT LINE\" - the four free spaces in her own ring and " +
                "the three ends of her free files of two, and nothing that needs a corner turned. " +
                Remaining(runtime));

            yield return Space(root, new HexCoordinate(-1, 1));
            yield return null;
            Assert.That(Position(runtime, hers), Is.EqualTo(new HexCoordinate(-1, 1)),
                "The Move walks the file whole. " + Remaining(runtime));
            Assert.That(HeroView(hers).Coordinate, Is.EqualTo(new HexCoordinate(-1, 1)));
            Assert.That(
                MagmaSpaces(runtime),
                Is.EqualTo(new[] { new HexCoordinate(-3, 1), new HexCoordinate(-2, 1) })
                    .AsCollection,
                "\"in each empty space you moved through, OR OUT OF\" - the space she left and the " +
                "space she crossed, and never the one she is standing on. " + Remaining(runtime));
            Assert.That(
                runtime.BoardView.Model.PlacedTokens.Count(token =>
                    token.Definition.Id.Equals(IgnatiaMagmaContent.Magma)),
                Is.EqualTo(2),
                "and the board DRAWS both of them. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The wake asks nobody anything: p.10's \"each empty space\" is decided by the Move " +
                "she already made. " + Remaining(runtime));
            SeatsAgree(runtime, "the wake");
            Healthy(runtime);

            // The next turn of the SAME round: the trail is an obstacle to her own walk.
            yield return HerNextTurnOfTheSameRound(
                root, IgnatiaCardContent.PlayingWithFire, ActionKind.Movement,
                WaspCardContent.Electrocute);
            Assert.That(runtime.Projection.RoundIndex, Is.EqualTo(round));
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            var destinations = runtime.PendingMovementChoice.LegalDestinations
                .Select(value => value.Destination)
                .ToArray();
            Assert.That(destinations, Has.No.Member(new HexCoordinate(-3, 1)));
            Assert.That(destinations, Has.No.Member(new HexCoordinate(-2, 1)),
                "p.18: \"All tokens are obstacles\", and a magma token declares no move-through " +
                "clause - so neither space is a destination. " + Remaining(runtime));
            Assert.That(destinations, Has.No.Member(new HexCoordinate(-4, 1)),
                "and the space BEYOND the trail is out of reach of her printed Movement 3, which is " +
                "what says the tokens are impassable rather than merely occupied: every route round " +
                "them is longer than the walk she has. " + Remaining(runtime));
            Healthy(runtime);

            // p.19's own cleanup.
            yield return DriveUntil(
                root,
                () => runtime.Projection.RoundIndex > round,
                "the end of the round the tokens were placed in");
            Assert.That(MagmaSpaces(runtime), Is.Empty,
                "\"Unless the card text says otherwise, tokens are removed at the end of round\" - " +
                "and Path of Ashes says nothing otherwise. " + Remaining(runtime));
            Healthy(runtime);
        }

        /// <summary>
        /// Path of Ashes, ☾: "Place up to 2 magma tokens in radius" - and its "up to", which is a
        /// DECLINE a player presses rather than an offer that quietly does nothing.
        ///
        /// <para>Each placement is its own node, because a stateless supply Place cannot tell a second
        /// drop from a first, so the two clicks are two decisions and either of them may be refused.
        /// The sun board below refuses the whole straight-line step instead, which is the same "up to"
        /// read on the other printed line: a Move that did not happen lays no wake.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PathOfAshesScattersItsTokensInRadiusAndEitherStepMayBeRefused()
        {
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[] { IgnatiaCardContent.PathOfAshes, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var hers = Ignatia(runtime);

            yield return BeginIgnatia(
                root, IgnatiaCardContent.PathOfAshes, ActionKind.Skill, WaspCardContent.Shock);
            var first = runtime.PendingEffectChoice;
            Assert.That(first, Is.Not.Null, Remaining(runtime));
            Assert.That(first.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-1"));
            Assert.That(first.DeclineInvocation, Is.Not.Null,
                "\"up to\" is a refusal a player presses. " + Remaining(runtime));
            Assert.That(
                OfferedSpaces(runtime).All(space =>
                    Distance(space, Position(runtime, hers)) <= 3),
                Is.True,
                "and every offered space is inside the printed Radius 3. " + Remaining(runtime));

            yield return Space(root);
            var firstSpace = MagmaSpaces(runtime).Single();
            var second = runtime.PendingEffectChoice;
            Assert.That(second, Is.Not.Null,
                "\"up to 2\" - a second placement is a second decision. " + Remaining(runtime));
            Assert.That(second.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-2"));
            Assert.That(OfferedSpaces(runtime), Has.No.Member(firstSpace),
                "and the space the first token took is no longer empty. " + Remaining(runtime));
            yield return Space(root);

            Assert.That(MagmaSpaces(runtime), Has.Length.EqualTo(2),
                "Two magma tokens, out of a supply of four. " + Remaining(runtime));
            Assert.That(MagmaSpaces(runtime), Has.Member(firstSpace));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            SeatsAgree(runtime, "the scattered tokens");
            Healthy(runtime);

            // The same printed "up to", refused - on the sun line, where the whole Move is the step.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.PathOfAshes, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var declineRoot = Root();
            var declineRuntime = declineRoot.Runtime;
            var stood = Position(declineRuntime, Ignatia(declineRuntime));

            yield return BeginIgnatia(
                declineRoot, IgnatiaCardContent.PathOfAshes, ActionKind.Skill,
                WaspCardContent.Shock);
            Assert.That(declineRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-magma-sun-line"));
            Assert.That(declineRuntime.PendingEffectChoice.DeclineInvocation, Is.Not.Null,
                "\"Move UP TO 2 spaces\" is refusable. " + Remaining(declineRuntime));
            yield return May(declineRoot, accept: false);
            Assert.That(Position(declineRuntime, Ignatia(declineRuntime)), Is.EqualTo(stood),
                "and she stands exactly where she stood. " + Remaining(declineRuntime));
            Assert.That(MagmaSpaces(declineRuntime), Is.Empty,
                "A Move that did not happen has no wake, so nothing is placed. " +
                Remaining(declineRuntime));
            Assert.That(declineRuntime.PendingEffectChoice, Is.Null, Remaining(declineRuntime));
            SeatsAgree(declineRuntime, "the refused line");
            Healthy(declineRuntime);
        }

        // -------------------------------------------------------------------------------------------
        // The whole printed set, drawn
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.20c's standard: every verified face she prints, drawn at the validated player
        /// viewport, is exactly one <c>CardFootprint</c> and shows its title, values, action, rules text
        /// and history whole. Measured over all NINETEEN, the boxed Handicap included - a card nobody
        /// can play is still a card somebody can look at - and hers are the first faces in the build
        /// that carry a corner marker over the artwork as well.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryIgnatiaFaceUsesTheStandardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedIgnatia);
            var root = Root();
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Ignatia", IgnatiaCardContent.AllDefinitions, root.Hud);
        }
    }
}
