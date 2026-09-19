using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Rowenna the Vanguard's piece, her tray and her two basic faces, driven through real pointer
    /// events and camera rays. Milestone 8.57h, and the EIGHTEENTH hero to take the permanent
    /// player-path gate.
    ///
    /// <para><b>THE STAGED BOARD WAS MEASURED BEFORE ANY OF THESE WERE WRITTEN</b>, which is the lesson
    /// milestone 8.38f records and the reason the offers below are asserted as exact sets. Milestone
    /// 8.57g's engagement stands her at (-3,1) and the one enemy hero at (-2,2) on a BENT line, with a
    /// friendly Blue melee minion at (-3,2), an enemy Red ranged minion at (-4,2), enemy Red melee
    /// minions at (-1,-1) and (-1,0), and terrain at (-3,3), (-4,3) and (-3,-2).</para>
    ///
    /// <para><b>Every journey of hers re-stands the two heroes through the Director's own sandbox</b>
    /// rather than taking the engagement as it falls, and the reason is her printed text rather than
    /// convenience: five of her eighteen faces name an enemy hero <em>adjacent</em> to her and the
    /// engagement's gap is two. <see cref="StageRowenna"/> therefore puts her at (-3,1) and the enemy
    /// at (-2,1) - measured adjacent, with the friendly melee minion at (-3,2) adjacent to BOTH, which
    /// is what gives her Blue swap its printed adjacency qualifier a candidate.</para>
    ///
    /// <para><b>The ALLY board is not a convenience either.</b> Milestone 8.57a composed "a friendly
    /// hero in radius" with <c>includeSelf</c> false, so her three Red riders and the two Paragons'
    /// gated repeat have no candidate at all on a one-Blue-hero board. That board is
    /// <c>VerifiedStagedRowennaWithAlly</c>, and it is deliberately NOT restaged - it deals the three
    /// heroes at their spawns fifteen spaces apart - so the sandbox stands all three where the printed
    /// sentences can reach each other.</para>
    ///
    /// <para><b>A staged hand is how a discard pile gets filled</b>, and that matters for four of her
    /// faces: <c>SandboxStagingAuthority</c> discards every dealt hand card the description does not
    /// name, so a two-card hand leaves three cards in the pile for a retrieve to choose from - and,
    /// conversely, a hand naming all five dealt cards plus one drawn from the deck leaves the pile
    /// EMPTY, which is what Opening Shots' both-parties predicate needs.</para>
    /// </summary>
    public sealed class RowennaUiJourneyPlayModeTests
    {
        /// <summary>Where the sandbox stands her: the engagement's own space for the Blue hero.</summary>
        internal static readonly HexCoordinate HerSpace = new HexCoordinate(-3, 1);

        /// <summary>Measured adjacent to <see cref="HerSpace"/> and to the friendly melee minion.</summary>
        internal static readonly HexCoordinate EnemySpace = new HexCoordinate(-2, 1);

        /// <summary>Measured adjacent to her, so a printed Radius 1 reaches the ally.</summary>
        internal static readonly HexCoordinate AllySpace = new HexCoordinate(-4, 1);

        /// <summary>Rulebook p.6's five, which a staged hand must name to leave her pile empty.</summary>
        internal static readonly CardDefinitionId[] DealtHand =
        {
            RowennaCardContent.CodeOfChivalry, RowennaCardContent.ThrowTheGauntlet,
            RowennaCardContent.TokenOfGratitude, RowennaCardContent.StandGuard,
            RowennaCardContent.CloseQuarters
        };

        /// <summary>p.6's five plus one drawn from her own deck - six in hand, nothing discarded.</summary>
        internal static CardDefinitionId[] DealtHandPlus(CardDefinitionId face) =>
            DealtHand.Concat(new[] { face }).Distinct().ToArray();

        /// <summary>
        /// The staged seat dealt Rowenna's printed set, found by the content identity the configuration
        /// bound to it rather than by team colour or by seat ordering. A staged arm missing from
        /// <c>SelectSetupConfiguration</c> deals her cards to a seat whose content identity is somebody
        /// else's, which is milestone 8.38f's defect and what this assertion exists to catch.
        /// </summary>
        internal static HeroId Rowenna(ConfiguredMatchSceneRuntime runtime)
        {
            var seats = runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == RowennaCardContent.ContentSetId)
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(1),
                "The staged configuration deals Rowenna to exactly one seat; it dealt her to " +
                seats.Length + ".");
            return seats[0].HeroId;
        }

        /// <summary>Her ally seat, where a configuration deals one.</summary>
        internal static HeroId Ally(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes
                .Where(hero => hero.ContentId.Value == ArienCardContent.ContentSetId)
                .Select(hero => hero.HeroId)
                .SingleOrDefault();

        /// <summary>Every enemy seat, nearest first.</summary>
        internal static HeroId[] Enemies(ConfiguredMatchSceneRuntime runtime)
        {
            var her = Rowenna(runtime);
            var ally = Ally(runtime);
            return runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(her) && !hero.HeroId.Equals(ally))
                .Select(hero => hero.HeroId)
                .ToArray();
        }

        /// <summary>
        /// Loads the named staged scene, reads the seats off it, and reloads the same scene with the
        /// Director's sandbox standing the pieces and dealing the hands this journey needs.
        ///
        /// <para>Two loads rather than one because a staging description names heroes by id and the ids
        /// only exist once a configuration has been composed - the shape milestone 8.37a's Guardian
        /// Spirit journey minted and every hero since has reused.</para>
        /// </summary>
        internal static IEnumerator StageRowenna(
            SceneCardContent content,
            IEnumerable<CardDefinitionId> hand = null,
            IEnumerable<CardDefinitionId> enemyHand = null,
            IEnumerable<CardDefinitionId> allyHand = null,
            HexCoordinate? herSpace = null,
            HexCoordinate? enemySpace = null,
            int? enemyLife = null)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var her = Rowenna(runtime);
            var ally = Ally(runtime);
            var enemies = Enemies(runtime);
            var enemySide = runtime.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(enemies[0])).TeamSide;

            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(her, herSpace ?? HerSpace, hand: hand),
                new SandboxHeroStaging(enemies[0], enemySpace ?? EnemySpace, hand: enemyHand)
            };
            if (ally.IsDefined)
            {
                heroes.Add(new SandboxHeroStaging(ally, AllySpace, hand: allyHand));
            }

            for (var index = 1; index < enemies.Length; index++)
            {
                heroes.Add(new SandboxHeroStaging(enemies[index], new HexCoordinate(-4, 0)));
            }

            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: heroes,
                teamLifeCounts: enemyLife.HasValue
                    ? new[] { new KeyValuePair<TeamSide, int>(enemySide, enemyLife.Value) }
                    : null);
            yield return LoadStagedScene(content);
        }

        /// <summary>
        /// Commits her named face out of her own tray and one named card out of every other seat's,
        /// then drives the turn to her and starts the printed action through its own control.
        ///
        /// <para>The enemy's card is NAMED rather than taken by Initiative, because two of her journeys
        /// care what is left in that hand afterwards: a committed card is in the turn area rather than
        /// the hand, so Accept Surrender's empty-handed clause is reached by committing the enemy's one
        /// remaining card, and Fabled Lance's Ranged Block has to still be IN the hand to be offered.
        /// </para>
        /// </summary>
        internal static IEnumerator BeginRowenna(
            MatchSceneCompositionRoot root,
            CardDefinitionId face,
            ActionKind kind,
            CardDefinitionId enemyFace = default)
        {
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            yield return PointTrayAt(root, her);
            yield return CommitThroughTray(root, NameOf(runtime, face));
            foreach (var seat in runtime.Setup.Heroes
                .Where(hero => !hero.HeroId.Equals(her))
                .Select(hero => hero.HeroId)
                .ToArray())
            {
                yield return PointTrayAt(root, seat);
                var named = enemyFace.IsDefined && HandOf(runtime, seat)
                    .Any(definition => definition.Id.Equals(enemyFace));
                yield return CommitThroughTray(
                    root,
                    named ? NameOf(runtime, enemyFace) : LowestInitiativeCardOf(runtime, seat));
            }

            yield return WaitForActor(root, her);
            yield return PointTrayAt(root, her);
            yield return StartActionThroughControl(root, kind);
            Healthy(runtime);
        }

        /// <summary>
        /// Every seat's own published projection agrees about the board, about whose decision is
        /// standing and about the round - which is the half of a cross-seat prompt no single-seat
        /// journey can see.
        ///
        /// <para>Three of her faces put a question to a seat that is not the acting one - Code of
        /// Chivalry's second retrieve, Throw the Gauntlet's move and Opening Shots' discard - and the
        /// thing that could silently be wrong about all three is not whether the prompt opens but
        /// whether every seat is told the same thing about it. <c>SeatProjection.DecisionOwner</c> is
        /// the public half ("we are waiting for these players"), so it is read from every seat and
        /// compared, together with the occupancies each seat draws its board from.</para>
        /// </summary>
        internal static void SeatsAgree(ConfiguredMatchSceneRuntime runtime, string moment)
        {
            var seats = runtime.Host.Seats.Seats.ToArray();
            Assert.That(seats, Is.Not.Empty, "A composed match has seats.");
            var readings = seats
                .Select(seat => runtime.Host.ProjectionForSeat(seat.Id))
                .Select(projection => new
                {
                    Owner = projection.DecisionOwner.ToString(),
                    Board = string.Join(",", projection.Heroes
                        .OrderBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                        .Select(hero => hero.HeroId.Value + "@" + hero.Coordinate)),
                    Round = projection.RoundIndex + "/" + projection.TurnIndex,
                    Lives = string.Join(",", projection.TeamLifeCounters
                        .OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
                        .Select(entry => entry.Key.Value + "=" + entry.Value))
                })
                .Select(reading =>
                    reading.Owner + " | " + reading.Board + " | " + reading.Round + " | " +
                    reading.Lives)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(readings, Has.Length.EqualTo(1),
                "Every seat's projection must agree at " + moment + "; they read: " +
                string.Join(" || ", readings));
        }

        /// <summary>Every card of this hero's currently in the discard pile, as definition ids.</summary>
        internal static string[] DiscardsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        /// <summary>That hero's coin balance, read off the projection the rails are drawn from.</summary>
        internal static int CoinsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(heroId)).Coins;

        /// <summary>The same balance as the player reads it, off that hero's own side rail.</summary>
        internal static int RailCoinsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(heroId)).Coins;

        // -------------------------------------------------------------------------------------------
        // The component half her piece owns, on her own dealt board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her own token stands at the configured Blue spawn as Rowenna and only once, a camera ray on
        /// it points the tray at her, and her tray commits a face through rulebook p.7's own reveal.
        /// </summary>
        [UnityTest]
        public IEnumerator HerPieceSpawnsAsRowennaAndHerTrayCommitsAndRevealsACard()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedRowenna);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);

            var pieces = Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                .Where(view => view.Occupancy.HeroId.Equals(her))
                .ToArray();
            Assert.That(pieces, Has.Length.EqualTo(1),
                "One hero, one piece - a second would be a view the rebuild failed to retire.");
            Assert.That(pieces[0].Coordinate,
                Is.EqualTo(runtime.Heroes.Single(hero => hero.HeroId.Equals(her)).Coordinate),
                "and it stands where the authority says she does.");
            Assert.That(
                runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(her)).SpawnPoint.ToString(),
                Does.Contain("blue-hero-01"),
                "The staged engagement preserves her configured spawn point, which is what p.13's " +
                "respawn later returns her to.");

            yield return PointTrayAt(root, her);
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(her),
                "A camera ray on her own piece points the tray at her.");

            yield return CommitThroughTray(root, NameOf(runtime, RowennaCardContent.CodeOfChivalry));
            Assert.That(runtime.GetCommitmentStatus(her),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown).Or
                    .EqualTo(TurnCardCommitmentStatus.Revealed),
                "CONFIRM FACEDOWN puts her card in p.7's turn area. " + Remaining(runtime));
            foreach (var seat in Enemies(runtime))
            {
                yield return PointTrayAt(root, seat);
                yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat));
            }

            Assert.That(runtime.GetCommitmentStatus(her),
                Is.EqualTo(TurnCardCommitmentStatus.Revealed),
                "and the reveal follows the last seat's commitment. " + Remaining(runtime));
            SeatsAgree(runtime, "the reveal");
            Healthy(runtime);
        }

        /// <summary>
        /// Every face she prints that carries rulebook p.10's secondary Movement ribbon walks the board
        /// through its own card face, and every face she prints at all defends p.14's Attack through
        /// its own card face.
        ///
        /// <para>Four of her dealt faces print NO Movement ribbon - Throw the Gauntlet, Close Quarters,
        /// Melee and Grand Melee, which is RO-05's rule that a missing icon is absent rather than zero
        /// - so the walk half is asked of the thirteen that do. The Defense half is asked of all
        /// seventeen faces that print an action at all: <b>Fabled Lance is a dealt definition and
        /// prints NO action whatsoever</b>, not even a secondary Defense shield, so she cannot discard
        /// it to defend and a loop that asked it to would be asking for a control the card does not
        /// have. That absence is measured here rather than assumed - it is exactly what the first
        /// Unity run of this journey found.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace()
        {
            foreach (var definition in RowennaCardContent.DealtDefinitions
                .Where(card => card.ActionDefinitions.Count > 0))
            {
                var walks = definition.ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary);
                if (walks)
                {
                    yield return StageRowenna(
                        SceneCardContent.VerifiedEngagedStagedRowenna,
                        hand: new[] { definition.Id, RowennaCardContent.StandGuard });
                    var root = Root();
                    var runtime = root.Runtime;
                    var her = Rowenna(runtime);
                    yield return BeginRowenna(root, definition.Id, ActionKind.Movement);
                    Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                        definition.DisplayName + "'s printed boot opens a walk. " + Remaining(runtime));
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
                    Assert.That(Position(runtime, her), Is.EqualTo(step),
                        "and her piece follows the authoritative space. " + Remaining(runtime));
                    Assert.That(HeroView(her).Coordinate, Is.EqualTo(step));
                    Healthy(runtime);
                }

                var committed = definition.Id.Equals(RowennaCardContent.StandGuard)
                    ? RowennaCardContent.CodeOfChivalry
                    : RowennaCardContent.StandGuard;
                yield return StageRowenna(
                    SceneCardContent.VerifiedEngagedStagedRowenna,
                    hand: new[] { definition.Id, committed });
                var defenseRoot = Root();
                var defenseRuntime = defenseRoot.Runtime;
                var defender = Rowenna(defenseRuntime);
                var enemy = Enemies(defenseRuntime)[0];
                yield return PointTrayAt(defenseRoot, defender);
                yield return CommitThroughTray(defenseRoot, NameOf(defenseRuntime, committed));
                yield return PointTrayAt(defenseRoot, enemy);
                yield return CommitThroughTray(
                    defenseRoot, NameOf(defenseRuntime, WaspCardContent.MagneticDagger));
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
        // Her two basic faces
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Code of Chivalry: "Target a unit adjacent to you. Before the attack: If you target a hero,
        /// both you and the target may retrieve a discarded card."
        ///
        /// <para><b>HER FIRST CROSS-SEAT PROMPT, and it is the one a single-seat journey cannot tell
        /// from a working one.</b> The second retrieve's chooser is the TARGET, so
        /// <c>EffectChoiceRequest.ChooserHeroId</c> names the enemy - the field
        /// <c>SeatProjection.OwnerOfStandingDecision</c> reads - and a chooser that fell back to the
        /// actor would look exactly like a working prompt from her side of the table. The journey
        /// therefore asserts whose question each one is, that the faces offered are that hero's OWN
        /// discards, and that every seat's projection agrees about who is being waited for.</para>
        ///
        /// <para>The rider is gated on "if you target a hero", so the same face is driven a second time
        /// against the adjacent enemy MINION and nothing at all stands afterwards - which is the
        /// negative that makes the gate real rather than the absence of an assertion.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CodeOfChivalryOffersBothRetrievesOnAHeroTargetAndNeitherOnAMinion()
        {
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna,
                hand: new[] { RowennaCardContent.CodeOfChivalry, RowennaCardContent.StandGuard },
                enemyHand: new[] { WaspCardContent.MagneticDagger, WaspCardContent.Shock });
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemy = Enemies(runtime)[0];

            Assert.That(DiscardsOf(runtime, her), Has.Length.EqualTo(3),
                "Arrange: a staged two-card hand leaves three of her dealt five in the pile.");
            Assert.That(DiscardsOf(runtime, enemy), Has.Length.EqualTo(3));

            yield return BeginRowenna(root, RowennaCardContent.CodeOfChivalry, ActionKind.Attack);
            Assert.That(
                runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "The one enemy hero the sandbox stands adjacent to her. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True);
            yield return null;
            yield return null;

            var mine = runtime.PendingEffectChoice;
            Assert.That(mine, Is.Not.Null,
                "\"Before the attack\" stops the page before p.14 checks it. " + Remaining(runtime));
            Assert.That(mine.NodeId.Value, Is.EqualTo("chivalry-retrieve-mine"));
            Assert.That(mine.ChooserId, Is.EqualTo(her), "Her own retrieve is hers.");
            Assert.That(
                mine.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(her)),
                Is.True,
                "and nobody retrieves another hero's card. " + Remaining(runtime));
            SeatsAgree(runtime, "her own retrieve");
            var hersBefore = DiscardsOf(runtime, her).Length;
            yield return Pick(root, mine.Candidates.First());
            Assert.That(DiscardsOf(runtime, her), Has.Length.EqualTo(hersBefore - 1),
                "and taking it moves exactly one card out of her pile.");

            var theirs = runtime.PendingEffectChoice;
            Assert.That(theirs, Is.Not.Null,
                "\"and the target\" is the second half of the same sentence. " + Remaining(runtime));
            Assert.That(theirs.NodeId.Value, Is.EqualTo("chivalry-retrieve-theirs"));
            Assert.That(theirs.ChooserId, Is.EqualTo(enemy),
                "The TARGET's retrieve is the TARGET's decision - the field " +
                "SeatProjection.OwnerOfStandingDecision reads, and a chooser that fell back to the " +
                "actor would hand the enemy's own choice to her. " + Remaining(runtime));
            Assert.That(
                theirs.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(enemy)),
                Is.True);
            SeatsAgree(runtime, "the target's retrieve");
            var theirsBefore = DiscardsOf(runtime, enemy).Length;
            yield return Pick(root, theirs.Candidates.First());
            Assert.That(DiscardsOf(runtime, enemy), Has.Length.EqualTo(theirsBefore - 1));
            SeatsAgree(runtime, "both retrieves answered");

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "and only then does p.14 ask the defender. " + Remaining(runtime));
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Healthy(runtime);

            // The negative: the same face, the same board, a MINION target and no rider at all.
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna,
                hand: new[] { RowennaCardContent.CodeOfChivalry, RowennaCardContent.StandGuard },
                enemyHand: new[] { WaspCardContent.MagneticDagger, WaspCardContent.Shock });
            var minionRoot = Root();
            var minionRuntime = minionRoot.Runtime;
            yield return BeginRowenna(
                minionRoot, RowennaCardContent.CodeOfChivalry, ActionKind.Attack);
            var minion = minionRuntime.PendingAttackTargetChoice.LegalTargetMinionIds.First();
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    minionRoot.Hud,
                    MinionView(minion).transform.position),
                Is.True);
            yield return null;
            yield return null;
            Assert.That(minionRuntime.PendingEffectChoice, Is.Null,
                "\"If you target a hero\" is false of a minion, so neither retrieve is offered. " +
                Remaining(minionRuntime));
            Healthy(minionRuntime);
        }

        /// <summary>
        /// Throw the Gauntlet: "Place yourself into a space in range adjacent to an enemy hero in
        /// range; that hero may move 1 space; if they do, gain 2 coins."
        ///
        /// <para><b>The only face in the corpus that places its own hero beside an enemy and then hands
        /// that enemy a decision.</b> Three measured facts are asserted as exact sets: the hero offer
        /// holds the one enemy inside her printed Range 4; the place offer holds exactly the FREE
        /// neighbours of the space that hero stands on - (-2,0), (-2,2) and (-1,1), with (-3,2) excluded
        /// because the friendly melee minion is on it and (-1,0) because an enemy melee minion is; and
        /// the move is the ENEMY'S question, on their seat, over the spaces one step from where they
        /// stand.</para>
        ///
        /// <para>Both halves of the printed "if they do" are driven: the accepted move pays her the 2
        /// coins and a declined one pays nothing, read on her own side rail rather than only in the
        /// projection.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ThrowTheGauntletPlacesHerBesideTheEnemyWhoThenOwnsTheMove()
        {
            foreach (var moves in new[] { true, false })
            {
                yield return StageRowenna(SceneCardContent.VerifiedEngagedStagedRowenna);
                var root = Root();
                var runtime = root.Runtime;
                var her = Rowenna(runtime);
                var enemy = Enemies(runtime)[0];
                Assert.That(CoinsOf(runtime, her), Is.Zero, "Arrange: an empty purse.");

                yield return BeginRowenna(
                    root, RowennaCardContent.ThrowTheGauntlet, ActionKind.Skill);
                var hero = runtime.PendingEffectChoice;
                Assert.That(hero.NodeId.Value, Is.EqualTo("gauntlet-hero"), Remaining(runtime));
                Assert.That(
                    hero.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "One enemy hero stands inside her printed Range 4. " + Remaining(runtime));
                yield return Pick(root, hero.Candidates.Single());

                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("gauntlet-place"));
                Assert.That(
                    OfferedSpaces(runtime),
                    Is.EqualTo(new[]
                    {
                        new HexCoordinate(-2, 2), new HexCoordinate(-2, 0), new HexCoordinate(-1, 1)
                    }.OrderBy(space => space.Q).ThenBy(space => space.R).ToArray()).AsCollection,
                    "MEASURED: the free neighbours of (-2,1), with the friendly melee minion's own " +
                    "space and the enemy melee minion's excluded. " + Remaining(runtime));
                yield return Space(root, new HexCoordinate(-2, 0));
                Assert.That(Position(runtime, her), Is.EqualTo(new HexCoordinate(-2, 0)),
                    "She placed HERSELF, which is what \"place yourself\" means.");
                Assert.That(Distance(Position(runtime, her), Position(runtime, enemy)),
                    Is.EqualTo(1));

                var move = runtime.PendingEffectChoice;
                Assert.That(move, Is.Not.Null, Remaining(runtime));
                Assert.That(move.NodeId.Value, Is.EqualTo("gauntlet-move"));
                Assert.That(move.ChooserId, Is.EqualTo(enemy),
                    "\"that hero may move 1 space\" is the ENEMY's decision, answered on their own " +
                    "seat - RO-Q8c. " + Remaining(runtime));
                Assert.That(move.DeclineInvocation, Is.Not.Null,
                    "and a printed `may` can always simply be declined.");
                Assert.That(
                    OfferedSpaces(runtime).All(space =>
                        Distance(space, Position(runtime, enemy)) == 1),
                    Is.True,
                    "One space, so every candidate is one step from where they stand.");
                SeatsAgree(runtime, "the opponent's own move");

                if (moves)
                {
                    var before = Position(runtime, enemy);
                    yield return Space(root);
                    Assert.That(Distance(Position(runtime, enemy), before), Is.EqualTo(1));
                    Assert.That(CoinsOf(runtime, her), Is.EqualTo(2),
                        "\"if they do, gain 2 coins\" reads the rider's own performed output.");
                    Assert.That(RailCoinsOf(runtime, her), Is.EqualTo(2),
                        "and the player reads the same number on her own rail.");
                }
                else
                {
                    var before = Position(runtime, enemy);
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                    Assert.That(Position(runtime, enemy), Is.EqualTo(before),
                        "A declined `may` moves nobody.");
                    Assert.That(CoinsOf(runtime, her), Is.Zero,
                        "and \"if they do\" is false, so no coin changes hands.");
                    Assert.That(RailCoinsOf(runtime, her), Is.Zero);
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, "the gauntlet resolved");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Milestone 8.20c's standard: every verified face she prints, drawn at the validated player
        /// viewport, is exactly one <c>CardFootprint</c> and shows its title, values, action, rules text
        /// and history whole. Measured over all NINETEEN, the boxed Handicap included - a card nobody
        /// can play is still a card somebody can look at.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryRowennaFaceUsesTheStandardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedRowenna);
            var root = Root();
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Rowenna", RowennaCardContent.AllDefinitions, root.Hud);
        }
    }
}
