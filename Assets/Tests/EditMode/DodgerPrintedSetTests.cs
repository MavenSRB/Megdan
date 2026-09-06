using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.DodgerSpawnPointBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z28. Dodger's composed faces measured through the machinery that will run them,
    /// rather than read back off the definitions.
    ///
    /// <para>What each card composes - which node, which capability, which literal - is
    /// <see cref="DodgerVerifiedContentTests"/>' and is not repeated. This file asks two things that
    /// file cannot: whether the compositions are legal against a catalog that holds every contract they
    /// name, and what number p.14 actually arrives at for the three faces whose printed <c>+</c> is a
    /// value rather than a step.</para>
    ///
    /// <para><b>She is not dealt in this phase, so there is no staged match to drive.</b> The Defense
    /// total is therefore measured through <see cref="DefenseValueQuery"/> on a purpose-built board -
    /// which is a stronger instrument for these three cards than a staged position would be, because
    /// the two user rulings turn on a board the official map cannot pose. Her completion milestone is
    /// what owes the staged journeys and the player-path gate.</para>
    /// </summary>
    public sealed class DodgerPrintedSetTests
    {
        private static readonly HeroId Dodger = new HeroId("dodger-hero");
        private static readonly HeroId Attacker = new HeroId("dodger-attacker");

        /// <summary>
        /// The catalog her composed faces are legal against: everything the build registers, plus the
        /// two contracts this phase minted and deliberately left unregistered. It is composed by
        /// concatenating the two declared lists rather than by naming handlers here, so a contract that
        /// moved from one list to the other needs no edit in this file.
        /// </summary>
        /// <summary>
        /// The catalog her faces are validated against: this build's, plus the contracts her own
        /// milestones minted and deliberately did not register.
        ///
        /// <para>That is the phase boundary written as a fixture. Milestone 7.9's deferral gate refuses
        /// a registration no dealt card can start, so the build offers none of them until she is dealt;
        /// but a composed body still has to be legal against the contracts it actually names, or the
        /// composition would be unreviewable until the dealing milestone. Passing them here checks the
        /// bodies now and leaves the registration debt to
        /// <see cref="TheNewContracts_AreNotRegisteredWhileSheIsUndealt"/>, which asserts it from the
        /// other side so neither half can move on its own.</para>
        /// </summary>
        private static EffectCapabilityCatalog Catalog() =>
            SyntheticEffectCapabilities.CreateCatalog(
                SyntheticRosterCapabilities.DeferredHandlers()
                    .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                    .Concat(DodgerActiveEffectContent.Handlers()));

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition -
        /// the check that would otherwise fault mid-resolution, on a player's turn, with the card
        /// already spent.
        ///
        /// <para>It is run at the moment each body declares, because milestone 8.10 made the moment part
        /// of the question: text applied at p.14's step 1 has to finish inside the command that reached
        /// it, and a Skill's own effect may pause freely. Her three modal Attacks are step-1 text, so
        /// this is the stricter rule for exactly the faces that need it.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogItNames()
        {
            var catalog = Catalog();
            var composed = 0;

            foreach (var definition in DodgerCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.EffectRoot == null) continue;
                    composed++;

                    var rejections = action.CardTextTiming != CardTextTiming.None
                        ? EffectDefinitionValidator.ValidateCardText(
                            action.EffectRoot, catalog, action.CardTextTiming)
                        : EffectDefinitionValidator.Validate(action.EffectRoot, catalog);

                    Assert.That(rejections, Is.Empty,
                        definition.Id.Value + " action '" + action.Id.Value + "' carries an effect " +
                        "definition a configured match would refuse: " +
                        string.Join("; ", rejections.Select(rejection => rejection.ToString())));
                }
            }

            Assert.That(composed, Is.EqualTo(13),
                "Seventeen faces compose and thirteen of them attach a body: three modal Attacks, two " +
                "step-7 shove riders, and eight Skills (Death Trap, the three rituals, the two " +
                "Active-effect drains and the two Respawns). The three Blue Defense primaries compose " +
                "without a body - their whole sentence is a value declaration, so there is no step to " +
                "attach - and Tide of Darkness composes as an Ultimate rule rather than as an action, " +
                "which is why seventeen composed faces carry thirteen bodies. Rusty Razor and " +
                "Middlefinger of Death attach none.");
        }

        /// <summary>
        /// Every capability her composed bodies name resolves in the catalog above, listed so the set is
        /// visible rather than implied. Two of them are this milestone's own.
        /// </summary>
        [Test]
        public void EveryCapabilitySheNames_ResolvesAndIsListed()
        {
            var catalog = Catalog();
            var named = DodgerCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability.ToString())
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(named, Is.EqualTo(new[]
            {
                "choose-target@2",
                "dodger-enfeeble-enemy-heroes-in-radius@1",
                "dodger-weaken-enemy-heroes-in-radius@1",
                "empty-minion-spawn-points-adjacent@1",
                "empty-minion-spawn-points-in-radius@1",
                "gain-item-from-the-box@1",
                "gain-resource@1",
                "set-attack-target@1",
                "synthetic-direct-move@3",
                "synthetic-discard-chosen-card@1",
                "synthetic-is-flagged@1",
                "synthetic-radius-hero-target@1",
                "synthetic-range-hero-target@1",
                "synthetic-range-unit-target@1",
                "synthetic-respawn-friendly-minion-adjacent@1",
                "synthetic-respawn-friendly-minion-in-radius@1",
                "ultimate-unlocked@1",
                "choose-target-set@1"
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "Her composed set names eighteen contracts, of which her two milestones minted eight - " +
                "phase one's two spawn-point counts, and this one's Ultimate predicate, item grant, " +
                "two Respawns and two Active-effect raisers.");

            // The eight her own milestones minted resolve in the CATALOG rather than in its registered
            // list, and that split is the whole of the phase boundary. Milestone 7.9's deferral gate
            // refuses a registration no dealt card can start, and no phase has dealt her - so the
            // contracts exist, validate and serialize, and the build does not offer them. Registration
            // is her dealing milestone's, and `TheNewContracts_AreNotRegisteredWhileSheIsUndealt`
            // asserts the debt from the other side so neither half can move alone.
            foreach (var reference in named)
            {
                Assert.That(
                    catalog.Registered.Any(value => value.ToString() == reference), Is.True,
                    reference + " must resolve in the catalog her faces are validated against.");
            }
        }

        /// <summary>
        /// The contracts her milestones minted are now IN the build's registration list, and the
        /// reason is the same rule that kept them out of it: milestone 7.9's deferral gate refuses a
        /// registration no printed card a match deals can start, and milestone 8.35z34 deals her.
        ///
        /// <para><b>This case was written inverted, on purpose, and is the inversion.</b> Milestone
        /// 8.35z28 asserted that none of her eight contracts was registered while she was undealt, and
        /// said in its own words that "her dealing milestone fails this case when it registers them
        /// without dealing her". That is what a debt asserted from both sides is for: the registration
        /// could not move without this file moving with it, and this file cannot claim the
        /// registration without <see cref="SheIsDealtByAConfiguredMatch"/> below proving there is a
        /// match that deals the cards.</para>
        ///
        /// <para>The gate's second half was the one that did real work here. It is asked of a
        /// <em>handler type</em>, not of a capability id, so each new type owed a
        /// <c>SyntheticShowcaseHand</c> consumer before it could register - and milestone 8.35z34 added
        /// two Skills carrying four types between them. Her Active-effect pair owed none, because it
        /// runs on <c>ActiveEffectCreationHandler</c>, the type Arien's five persistent texts already
        /// proved.</para>
        /// </summary>
        [Test]
        public void TheNewContracts_AreRegisteredNowSheIsDealt()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                .Select(value => value.ToString())
                .ToArray();

            Assert.That(registered, Has.Member("empty-minion-spawn-points-in-radius@1"));
            Assert.That(registered, Has.Member("empty-minion-spawn-points-adjacent@1"));
            Assert.That(registered, Has.Member("ultimate-unlocked@1"));
            Assert.That(registered, Has.Member("gain-item-from-the-box@1"));
            Assert.That(registered, Has.Member("synthetic-respawn-friendly-minion-adjacent@1"));
            Assert.That(registered, Has.Member("synthetic-respawn-friendly-minion-in-radius@1"));
            Assert.That(registered, Has.Member("dodger-weaken-enemy-heroes-in-radius@1"));
            Assert.That(registered, Has.Member("dodger-enfeeble-enemy-heroes-in-radius@1"));

            // And the other side of the move: none of them is still declared as deferred. Stated per
            // contract rather than over the whole list, and deliberately - the content wave leaves five
            // other undealt heroes' contracts in both lists, so an exhaustive assertion here would be
            // an assertion about them.
            var deferred = SyntheticRosterCapabilities.DeferredHandlers()
                .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                .Select(handler => handler.Reference.ToString())
                .ToArray();

            Assert.That(deferred, Has.No.Member("empty-minion-spawn-points-in-radius@1"));
            Assert.That(deferred, Has.No.Member("empty-minion-spawn-points-adjacent@1"));
            Assert.That(deferred, Has.No.Member("ultimate-unlocked@1"));
            Assert.That(deferred, Has.No.Member("gain-item-from-the-box@1"));
            Assert.That(deferred, Has.No.Member("synthetic-respawn-friendly-minion-adjacent@1"));
            Assert.That(deferred, Has.No.Member("synthetic-respawn-friendly-minion-in-radius@1"));
        }

        /// <summary>
        /// A configured match deals her eighteen cards, which is the half of milestone 7.9's gate the
        /// registration above rests on. Asserted through the configuration rather than through the
        /// content, because "she is dealt" is a statement about a match and not about a card file.
        /// </summary>
        [Test]
        public void SheIsDealtByAConfiguredMatch()
        {
            var configuration = MatchConfiguration.CreateStagedDodgerForInternalTest(TeamSide.Red);

            var hers = configuration.CardInstances
                .Where(card => card.DefinitionReference.DefinitionId.Value
                    .StartsWith(DodgerCardContent.ContentSetId + "-", StringComparison.Ordinal))
                .ToArray();

            Assert.That(hers, Has.Length.EqualTo(18),
                "p.6 deals her eighteen of her nineteen faces; Rusty Razor is p.20's Handicap card " +
                "and is in the box.");
            Assert.That(
                hers.Count(card => card.State.Location == CardLocation.Hand), Is.EqualTo(5),
                "p.6 step 11's starting hand: the three Tier I cards and the two basics.");
            Assert.That(
                hers.All(card => string.Equals(
                    card.DefinitionReference.ContentRevision,
                    DodgerCardContent.ContentRevision,
                    StringComparison.Ordinal)),
                Is.True,
                "Every dealt instance carries the revision this milestone pinned her identity at.");
        }

        // -----------------------------------------------------------------------------------------
        // p.14's printed `+` on a Defense value
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Printed p.14's own worked example, arithmetic included. Dodger defends with Vampiric Shield
        /// beside two empty minion spawn points: "since Defense is the Primary action of 'Vampiric
        /// Shield', the card text is applied, giving Dodger +2 Defense for the 2 empty minion spawn
        /// points within a radius of 2 spaces from her space." Printed 4 plus 2 is 6, which is what that
        /// page's Attack Total of 6 is defended by under its "equal or higher".
        /// </summary>
        [Test]
        public void VampiricShield_AddsItsPrintedPlusTwo_ExactlyAsPageFourteenWorksIt()
        {
            // The two occupying minions stand on the far pair, at distance 2. p.14 gives the defender a
            // modifier for an ADJACENT friendly melee minion and for an enemy ranged one within radius
            // 2, and these are neither - so the total below is the printed value and the card's own
            // text with nothing else in it, which is what makes it comparable to the page's worked
            // example line for line.
            var board = CreateBoard(Dodger, Origin, MeleeSpawnFar, HeavySpawnFar);

            Assert.That(
                EmptyMinionSpawnPointQuery.CountWithin(board, board, Origin, 2), Is.EqualTo(2),
                "Two of the four qualifying points are occupied here, leaving exactly the two the " +
                "page's example counts.");
            Assert.That(
                MinionDefenseContribution(board), Is.EqualTo(0),
                "Stated rather than assumed: if a staged minion ever moved next to her, the total " +
                "below would stop being the page's arithmetic and this case would be measuring " +
                "something else.");
            Assert.That(
                DefenseTotal(board, DodgerCardContent.VampiricShield), Is.EqualTo(6),
                "Printed Defense 4 plus the card's own +2 - and printed p.14 defends an Attack Total " +
                "of 6 with exactly this number under its \"equal or higher\".");
        }

        /// <summary>
        /// The threshold is a threshold and not a scale: four qualifying points give the same +2 as two
        /// do. Composing this sentence as a per-match contribution would have made Aegis of Doom's +4
        /// grow to +16 on this board, which is a card it does not print.
        /// </summary>
        [Test]
        public void TheConditionalDefense_IsAThresholdAndNotAScale()
        {
            var board = CreateBoard(Dodger, Origin);

            Assert.That(EmptyMinionSpawnPointQuery.CountWithin(board, board, Origin, 2), Is.EqualTo(4));
            Assert.That(DefenseTotal(board, DodgerCardContent.ShieldOfDecay), Is.EqualTo(5),
                "Printed 3 plus a flat 2, with four points in radius rather than two.");
            Assert.That(DefenseTotal(board, DodgerCardContent.VampiricShield), Is.EqualTo(6));
            Assert.That(DefenseTotal(board, DodgerCardContent.AegisOfDoom), Is.EqualTo(8),
                "Printed 4 plus a flat 4 - the top of the game's Defense range, and the number the " +
                "designer was asked about in the Discord ruling this milestone reads.");
        }

        /// <summary>
        /// Below the printed threshold the card is its plain printed number - the boundary case that
        /// separates a conditional value from an unconditional one, and the reason the printed
        /// <c>3+</c> is a 3 rather than a 5.
        /// </summary>
        [Test]
        public void BelowItsPrintedThreshold_TheCardTextAddsNothing()
        {
            // Three of the four qualifying points hold a minion, leaving one. Two of those minions are
            // adjacent to her and p.14 gives her a modifier for each, so the card-text contribution is
            // read on its own here rather than off the total - which is the only number this case is
            // about.
            var board = CreateBoard(Dodger, Origin, MeleeSpawnNear, MeleeSpawnFar, RangedSpawnNear);

            Assert.That(EmptyMinionSpawnPointQuery.CountWithin(board, board, Origin, 2), Is.EqualTo(1));
            Assert.That(CardTextDefenseBonus(board, DodgerCardContent.ShieldOfDecay), Is.EqualTo(0),
                "One qualifying point is short of the printed \"2 or more\", which is the boundary " +
                "that makes the printed `3+` a 3 rather than a 5.");
            Assert.That(CardTextDefenseBonus(board, DodgerCardContent.VampiricShield), Is.EqualTo(0));
            Assert.That(CardTextDefenseBonus(board, DodgerCardContent.AegisOfDoom), Is.EqualTo(0));
        }

        /// <summary>
        /// THE DESIGNER RULING, measured on the value itself rather than on the query beneath it: Dodger
        /// standing in a Throne Zone still gets her bonus, because the clause qualifies the spawn points
        /// and not her.
        /// </summary>
        [Test]
        public void TheBonus_AppliesWithDodgerHerselfOutsideTheBattleZone()
        {
            var board = CreateBoard(Dodger, ThroneStandingSpace);

            Assert.That(
                BattleZoneQuery.IsInCurrentBattleZone(board, ThroneStandingSpace), Is.False);
            Assert.That(
                CardTextDefenseBonus(board, DodgerCardContent.AegisOfDoom),
                Is.EqualTo(4),
                "Official designer ruling, \"rules-questions-goa2\" Discord, 2026-07-25, on this very " +
                "card: asked whether Dodger can be outside the battle zone and still get the +4 as " +
                "long as the empty spawns are in the battle zone, the designer answered \"Sure, why " +
                "not.\" Two qualifying points are in her Radius-2 disk from (2, 0).");
        }

        /// <summary>
        /// RULING 4, measured on the value: the hero spawn point one space from her does not push the
        /// count over the line. This is the case that would silently pass under the rejected reading of
        /// D-01 if the query beneath were ever widened.
        /// </summary>
        [Test]
        public void TheBonus_IsNotBoughtByAHeroSpawnPoint()
        {
            // Every minion spawn point in reach is occupied; only the hero spawn point on (0, 1) and
            // the Throne Zone's minion point remain empty, and neither qualifies.
            var board = CreateBoard(
                Dodger, Origin, MeleeSpawnNear, MeleeSpawnFar, RangedSpawnNear, HeavySpawnFar);

            Assert.That(EmptyMinionSpawnPointQuery.CountWithin(board, board, Origin, 2), Is.EqualTo(0));
            Assert.That(CardTextDefenseBonus(board, DodgerCardContent.AegisOfDoom), Is.EqualTo(0),
                "Under the rejected reading of D-01 the empty hero spawn point beside her would count " +
                "and the Throne Zone's would not, which is one - still short of two. Under a reading " +
                "that also dropped the Zone clause it would be two, and this would read 4.");
        }

        /// <summary>
        /// p.9's disk is the card's printed Radius icon, so p.16's Radius item widens the question these
        /// three cards ask. It is the one channel that can: p.16 raises a printed icon and never a
        /// number a sentence writes down, which is why the "2 or more" is untouched by any item.
        /// </summary>
        [Test]
        public void ARadiusItem_WidensTheQuestionTheDefenseCardAsks()
        {
            // Two qualifying points sit at distance 2 and two at distance 1. Standing one step further
            // out puts both far points at distance 3, so a printed Radius of 2 reaches only one.
            var perch = new HexCoordinate(0, -1);
            var board = CreateBoard(Dodger, perch, RangedSpawnNear);

            Assert.That(
                EmptyMinionSpawnPointQuery.CountWithin(board, board, perch, 2), Is.EqualTo(1),
                "From (0, -1) only the melee point on (1, 0) is within two.");
            Assert.That(
                EmptyMinionSpawnPointQuery.CountWithin(board, board, perch, 3), Is.EqualTo(3),
                "A Radius of 3 reaches the two far Battle-Zone points as well - which is exactly what " +
                "one p.16 Radius item would buy her.");
            Assert.That(CardTextDefenseBonus(board, DodgerCardContent.AegisOfDoom), Is.EqualTo(0),
                "At the printed Radius of 2 the condition is unmet, so the card's text adds nothing " +
                "and the printed 4 stands. The count above says a Radius item would change that, and " +
                "nothing else on the card could - p.16 never raises the \"2 or more\", because that " +
                "is a number the sentence writes down rather than an icon.");
        }

        /// <summary>
        /// The Defense total for one of her three Blue cards, composed the way a live match composes
        /// it - printed icon, p.14's minion modifiers, this card's own text, and every typed modifier.
        /// </summary>
        private static int DefenseTotal(AdjacentMoveAuthority board, CardDefinitionId card) =>
            Defense(board, card).Total;

        /// <summary>
        /// Only the amount this card's own printed text supplied, isolated from everything else in the
        /// total.
        ///
        /// <para>It exists because p.14 builds a Defense Total from several separately-named
        /// contributions and this milestone added exactly one of them. A case that asserted the total
        /// would fail whenever a fixture's occupying minion happened to stand beside her - and would
        /// fail with a number, saying nothing about which half moved. <see cref="ValueContribution"/>
        /// keeps each contribution's provenance for exactly this reason.</para>
        /// </summary>
        private static int CardTextDefenseBonus(AdjacentMoveAuthority board, CardDefinitionId card) =>
            Defense(board, card).Contributions
                .Where(value => value.Kind == ValueContributionKind.CardTextModifier)
                .Sum(value => value.Amount);

        /// <summary>What p.14's minion Defense modifiers add on this board, for a case that must be free of them.</summary>
        private static int MinionDefenseContribution(AdjacentMoveAuthority board) =>
            DefenseValueQuery
                .CalculateMinionContributions(Dodger, Participants(), board)
                .Sum(value => value.Amount);

        private static ValueResult Defense(AdjacentMoveAuthority board, CardDefinitionId card)
        {
            var definition = DodgerCardContent.Definition(card);
            var offer = new ActionOffer(
                new CardInstanceId("dodger-defense-instance"),
                definition.Reference,
                DodgerCardContent.PrimaryDefense,
                Dodger,
                ActionInvocationContext.PrintedPrimary,
                true);

            return DefenseValueQuery.Calculate(
                offer,
                DodgerCardContent.AllDefinitions,
                Dodger,
                Participants(),
                board,
                RuleModificationSet.Empty,
                null);
        }

        private static MatchParticipants Participants()
        {
            var red = new TeamId("dodger-red");
            var blue = new TeamId("dodger-blue");
            var defenderPlayer = new PlayerId("dodger-player");
            var attackerPlayer = new PlayerId("dodger-attacker-player");
            return new MatchParticipants(
                new[] { new Team(red, TeamSide.Red), new Team(blue, TeamSide.Blue) },
                new[]
                {
                    new Player(defenderPlayer, red),
                    new Player(attackerPlayer, blue)
                },
                new[]
                {
                    new Hero(Dodger, defenderPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Attacker, attackerPlayer, HeroDashboard.RulebookLayout)
                });
        }
    }
}
