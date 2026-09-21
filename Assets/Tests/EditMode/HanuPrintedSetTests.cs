using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.71g. Hanu the Trickster's printed set as a SET: nineteen faces, the five p.6 deals
    /// into his hand, the thirteen its deck keeps, the one p.6 returns to the box, the three coloured
    /// ladders p.15 climbs, the twelve items p.16 earns from the faces he does not choose, and the one
    /// revision every definition and every body carries.
    ///
    /// <para><b>What this fixture is for, and what it leaves to the six block fixtures.</b> 8.71a
    /// through 8.71f each proved one group of clauses at the effect frame - <c>HanuHurryUpTests</c>,
    /// <c>HanuUltimateTrickTests</c>, <c>HanuRedsTests</c>, <c>HanuJourneyTests</c>,
    /// <c>HanuCoMoveTests</c> and <c>HanuGreensTests</c> - and nothing here repeats one of those
    /// bodies. What none of them could say is anything about the SET, because until this milestone
    /// there was no set: no inventory order, no starting hand, no deck, no completed Blue ladder, no
    /// boxed Handicap and no <c>DealtDefinitions</c>. Nor could they reach the behaviours that need TWO
    /// blocks' faces at once, which is the other half of this file: Hurry up! rewriting a card of HIS
    /// OWN printed set and reordering p.7 against his own printed numbers; the delegation his Ultimate
    /// derives offering the delegated hero exactly that hero's own actions; and the immunity a Blue
    /// journey grants closing a Red's second bullet.</para>
    ///
    /// <para><b>Nothing of his is registered yet, and that is asserted rather than assumed.</b>
    /// Milestone 7.9's gate holds his content out of <see cref="MatchContentCatalogs"/> until 8.71h
    /// deals him, so the catalog these cases validate against is the build's registration list PLUS
    /// exactly the declarations his series deliberately withholds - 8.57f's precedent, which is what
    /// makes eighteen dealt faces sayable at a composition milestone at all. The gate is asserted from
    /// the other side too, by naming the exact set of capability references this build refuses and the
    /// exact faces that name one.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class HanuPrintedSetTests
    {
        private static readonly TeamId BlueTeam = new TeamId("hanu-set-blue-team");
        private static readonly TeamId RedTeam = new TeamId("hanu-set-red-team");

        private static readonly HeroId Hanu = new HeroId("hanu-set-trickster");
        private static readonly HeroId Teammate = new HeroId("hanu-set-teammate");
        private static readonly HeroId Enemy = new HeroId("hanu-set-enemy");
        private static readonly HeroId SecondEnemy = new HeroId("hanu-set-second-enemy");

        private static readonly MapId Map = new MapId("hanu-printed-set");

        // -------------------------------------------------------------------------------------------
        // The printed inventory
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One row per face, in his shard's own table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item.
        ///
        /// <para>A dash is what the face prints nothing for, which is HA-06 written out nineteen times:
        /// a missing icon is absent, never zero, and an absent secondary is an action that does not
        /// exist rather than one whose value is 0. His Silver prints no Movement; Fight and Flight, the
        /// boxed Handicap and all five Greens print neither Range nor Radius on an Attack that has one
        /// or a Skill that has the other; the Ultimate prints nothing at all.</para>
        ///
        /// <para>It is deliberately a literal table rather than something derived, for the reason
        /// <c>WaspVerifiedContentTests</c> and every set after it record: a test that derived its
        /// expectations from the definitions would pass whatever the definitions happened to say. This
        /// one fails if a single number, colour, Tier, icon or name drifts from
        /// <c>Docs/AI/Rules/HERO_HANU.md</c>, which is the document the extracted faces were
        /// transcribed into - and the case after it reads that document back independently, so the two
        /// transcriptions have to agree with each other as well as with the content.</para>
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "hanu-fight-and-flight|Fight and Flight|Init 13|SecDef 1|SecMov 2|primary-basic-attack Attack NonRanged [Attack 2]|Gold Basic|item -",
            "hanu-hurry-up|Hurry up!|Init 12|SecDef 1|SecMov -|primary-basic-skill Skill None [Range 4]|Silver Basic|item -",
            "hanu-helping-hand|Helping Hand|Init 9|SecDef 4|SecMov 5|primary-ranged-attack Attack Ranged [Attack 3, Range 3]|Red I|item -",
            "hanu-even-the-odds|Even the Odds|Init 9|SecDef 4|SecMov 5|primary-ranged-attack Attack Ranged [Attack 4, Range 4]|Red II|item Initiative",
            "hanu-outnumber|Outnumber|Init 9|SecDef 5|SecMov 5|primary-ranged-attack Attack Ranged [Attack 5, Range 4]|Red II|item Defense",
            "hanu-trusted-sidekick|Trusted Sidekick|Init 10|SecDef 5|SecMov 5|primary-ranged-attack Attack Ranged [Attack 4, Range 4]|Red III|item Movement",
            "hanu-pile-on|Pile On|Init 10|SecDef 6|SecMov 5|primary-ranged-attack Attack Ranged [Attack 5, Range 4]|Red III|item Radius",
            "hanu-unexpected-journey|Unexpected Journey|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 2]|Blue I|item -",
            "hanu-there-and-back-again|There and Back Again|Init 11|SecDef 4|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Attack",
            "hanu-this-way|This Way!|Init 11|SecDef 4|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Initiative",
            "hanu-safe-travels|Safe Travels|Init 11|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Attack",
            "hanu-that-way|That Way!|Init 11|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Initiative",
            "hanu-monkey-trick|Monkey Trick|Init 2|SecDef 1|SecMov 2|primary-skill Skill None [Radius 1]|Green I|item -",
            "hanu-monkey-twist|Monkey Twist|Init 2|SecDef 2|SecMov 2|primary-skill Skill None [Radius 2]|Green II|item Defense",
            "hanu-hear-nothing|Hear Nothing|Init 2|SecDef 2|SecMov 2|primary-skill Skill None [Radius 3]|Green II|item Attack",
            "hanu-monkey-business|Monkey Business|Init 1|SecDef 2|SecMov 2|primary-skill Skill None [Radius 2]|Green III|item Defense",
            "hanu-see-nothing|See Nothing|Init 1|SecDef 2|SecMov 2|primary-skill Skill None [Radius 3]|Green III|item Range",
            "hanu-the-ultimate-trick|The Ultimate Trick|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -",
            "hanu-fight-or-flight|Fight or Flight|Init 12|SecDef 1|SecMov 1|primary-basic-attack Attack NonRanged [Attack 1]|Gold Basic|item -"
        };

        /// <summary>
        /// The SIX capability references that are his own - the contracts his series minted and that no
        /// other hero's content names - as the build spells them, and the whole of what 8.71h must
        /// register.
        ///
        /// <para>THREE of them are new handler TYPES, so 8.71h owes each a showcase consumer as well as
        /// a registration - the gate's second half: <c>hanu-hurry-up-initiative@1</c> (8.71a),
        /// <c>hanu-journey-partner@1</c> (8.71d) and <c>hanu-friendly-unit-pair@1</c> (8.71f). The other
        /// three are <c>ActiveEffectCreationHandler</c> registrations over the three definitions in
        /// <c>HanuActiveEffectContent</c> - one per journey face, because that handler is one
        /// registration per definition rather than a type of its own.</para>
        ///
        /// <para>The case below asserts this list as a SET from three directions at once: what the build
        /// refuses, what his declaration list holds, and what a walk over his faces AND his three
        /// triggered bodies names. The triggered half is load-bearing:
        /// <c>hanu-journey-partner@1</c> is named by no card of his at all - it is the lookup the
        /// end-of-turn body performs - so a walk over the nineteen faces alone would find five and call
        /// the sixth an unregistered stranger at the dealing.</para>
        /// </summary>
        private static readonly string[] HisOwnContracts =
        {
            "hanu-friendly-unit-pair@1",
            "hanu-hurry-up-initiative@1",
            "hanu-journey-partner@1",
            "hanu-safe-travels-effect@1",
            "hanu-there-and-back-again-effect@1",
            "hanu-unexpected-journey-effect@1"
        };

        /// <summary>
        /// The faces this build REFUSES, because each reaches a contract 8.71h has yet to register.
        /// Seven of the seventeen composed bodies; the other ten name only the roster's own vocabulary -
        /// a modal Attack, a printed-Range or printed-Radius selection, a swap, a coupled move, a direct
        /// move and a straight line - which is exactly the difference registration will buy.
        /// </summary>
        private static readonly string[] FacesThisBuildRefuses =
        {
            "hanu-hurry-up",
            "hanu-monkey-business",
            "hanu-monkey-trick",
            "hanu-monkey-twist",
            "hanu-safe-travels",
            "hanu-there-and-back-again",
            "hanu-unexpected-journey"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                HanuCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.71 verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        /// <summary>
        /// Nineteen faces, eighteen dealt, one boxed - and the colour shape p.5 gives every hero: two
        /// Gold basics, one Silver basic, five each of Red, Blue and Green, and the Purple Ultimate,
        /// which carries no <see cref="CardColor"/> of its own because p.16 unlocks it rather than
        /// upgrading into it.
        /// </summary>
        [Test]
        public void HisSet_IsNineteenFacesInThePrintedColourShape()
        {
            Assert.That(HanuCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(HanuCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                HanuCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { HanuCardContent.FightOrFlight }));
            Assert.That(
                HanuCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(HanuCardContent.FightOrFlight),
                "and the dealt eighteen are the nineteen minus exactly that one.");

            var byColour = HanuCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2),
                "Fight and Flight and the boxed Fight or Flight - two faces printing one sentence " +
                "with one word changed, and only the first of them dealt.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1), "Hurry up!.");
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5), "The five modal Ranged Attacks.");
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5), "Three journeys and two co-moves.");
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5),
                "Three friendly-pair swaps and two enemy-hero swaps.");
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "The Ultimate Trick.");

            var ultimate = HanuCardContent.Definition(HanuCardContent.TheUltimateTrick);
            Assert.That(ultimate.Progression.Classification, Is.EqualTo(CardClassification.Ultimate));
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "p.16's permanent ability prints no action at all, so he cannot even discard it to " +
                "defend - which is the absence HA-06 protects, stated for a whole card.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);

            Assert.That(
                () => HanuCardContent.Definition(new CardDefinitionId("hanu-not-a-face")),
                Throws.ArgumentException,
                "and the set refuses an identity it does not print rather than answering null.");
        }

        /// <summary>
        /// p.6's five starting cards and the thirteen the deck keeps partition his dealt eighteen
        /// exactly, so no card is both and none is neither - and the five are one of each colour, which
        /// is the shape p.6 states rather than a fact about him in particular.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHisDealtEighteen()
        {
            Assert.That(HanuCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(HanuCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(HanuCardContent.StartingHand.Intersect(HanuCardContent.Deck), Is.Empty);
            Assert.That(
                HanuCardContent.StartingHand.Concat(HanuCardContent.Deck),
                Is.EquivalentTo(HanuCardContent.DealtDefinitions.Select(card => card.Id)));

            Assert.That(HanuCardContent.StartingHand, Is.EqualTo(new[]
            {
                HanuCardContent.FightAndFlight,
                HanuCardContent.HurryUp,
                HanuCardContent.HelpingHand,
                HanuCardContent.UnexpectedJourney,
                HanuCardContent.MonkeyTrick
            }));

            Assert.That(
                HanuCardContent.StartingHand
                    .Select(id => HanuCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.6 deals one of each colour, and his are the Gold, the Silver and the three Tier I.");

            Assert.That(
                HanuCardContent.Deck
                    .Select(HanuCardContent.Definition)
                    .Count(definition => definition.Progression.IsUltimate),
                Is.EqualTo(1),
                "p.16's card starts in the deck and is reached by the unlock rather than by a draw.");
            Assert.That(
                HanuCardContent.Deck
                    .Select(HanuCardContent.Definition)
                    .Where(definition => !definition.Progression.IsUltimate)
                    .Select(definition => definition.Progression.Tier)
                    .OrderBy(tier => tier),
                Is.EqualTo(new[]
                {
                    CardTier.II, CardTier.II, CardTier.II, CardTier.II, CardTier.II, CardTier.II,
                    CardTier.III, CardTier.III, CardTier.III, CardTier.III, CardTier.III, CardTier.III
                }),
                "and the other twelve are the six Tier II and the six Tier III faces.");
        }

        /// <summary>
        /// One seat's whole setup, built the way a configured match will build it at 8.71h: eighteen
        /// instances, five of them in hand and thirteen in the deck, with the boxed Handicap nowhere at
        /// all.
        /// </summary>
        [Test]
        public void HisDealtHand_IsFiveInHandAndThirteenInTheDeck_AndNeverTheHandicap()
        {
            var hand = HanuCardContent.CreateHand(Hanu, "hanu-printed-set");

            Assert.That(hand.AllInstances.Count, Is.EqualTo(18));
            Assert.That(
                hand.AllInstances.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(HanuCardContent.FightOrFlight));

            var cards = HanuCardContent.CreateDealtCards(Hanu, "hanu-printed-set");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(HanuCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(HanuCardContent.Deck));
            Assert.That(cards.Select(card => card.Id.Value).Distinct().Count(), Is.EqualTo(18),
                "and each instance identity is distinct, which is what the per-seat prefix is for.");
            Assert.That(cards.All(card => card.OwnerId.Equals(Hanu)), Is.True);

            Assert.That(
                () => HanuCardContent.CreateDealtCards(default, "hanu-printed-set"),
                Throws.ArgumentException,
                "A card without an owner is not a card p.6 dealt to anybody.");
            Assert.That(
                () => HanuCardContent.CreateDealtCards(Hanu, "  "),
                Throws.ArgumentException,
                "and the per-seat prefix is the caller's to name, so an empty one is refused rather " +
                "than silently shared between two seats.");
        }

        // -------------------------------------------------------------------------------------------
        // The upgrade ladders and the items
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.15's three ladders, each forking twice. THREE of the six Blue pairs crossed a block
        /// boundary - Unexpected Journey and There and Back Again are 8.71d's faces and name 8.71e's
        /// co-moves, and This Way! names 8.71d's Safe Travels - which is exactly why they could not be
        /// stated until the set was composed. The Red and the Green ladders were whole from their own
        /// blocks.
        /// </summary>
        [Test]
        public void EveryUpgradePair_IsThePrintedLadder()
        {
            AssertSuccessors(
                HanuCardContent.HelpingHand,
                HanuCardContent.EvenTheOdds, HanuCardContent.Outnumber);
            AssertSuccessors(
                HanuCardContent.EvenTheOdds,
                HanuCardContent.TrustedSidekick, HanuCardContent.PileOn);
            AssertSuccessors(
                HanuCardContent.Outnumber,
                HanuCardContent.TrustedSidekick, HanuCardContent.PileOn);

            AssertSuccessors(
                HanuCardContent.UnexpectedJourney,
                HanuCardContent.ThereAndBackAgain, HanuCardContent.ThisWay);
            AssertSuccessors(
                HanuCardContent.ThereAndBackAgain,
                HanuCardContent.SafeTravels, HanuCardContent.ThatWay);
            AssertSuccessors(
                HanuCardContent.ThisWay,
                HanuCardContent.SafeTravels, HanuCardContent.ThatWay);

            AssertSuccessors(
                HanuCardContent.MonkeyTrick,
                HanuCardContent.MonkeyTwist, HanuCardContent.HearNothing);
            AssertSuccessors(
                HanuCardContent.MonkeyTwist,
                HanuCardContent.MonkeyBusiness, HanuCardContent.SeeNothing);
            AssertSuccessors(
                HanuCardContent.HearNothing,
                HanuCardContent.MonkeyBusiness, HanuCardContent.SeeNothing);

            foreach (var tierThree in HanuCardContent.AllDefinitions
                .Where(definition => definition.Progression.Tier == CardTier.III))
            {
                AssertSuccessors(tierThree.Id);
            }

            // The four faces that sit on no ladder at all, and for three different printed reasons:
            // p.6 deals his Gold and Silver basics and p.15 never upgrades a basic, p.20 keeps the
            // Handicap in the box, and p.16 UNLOCKS the Ultimate rather than upgrading into it.
            foreach (var offLadder in new[]
            {
                HanuCardContent.FightAndFlight,
                HanuCardContent.HurryUp,
                HanuCardContent.TheUltimateTrick,
                HanuCardContent.FightOrFlight
            })
            {
                AssertSuccessors(offLadder);
                Assert.That(
                    HanuCardContent.AllDefinitions.SelectMany(definition =>
                        definition.Progression.NextTierCandidateIds),
                    Has.No.Member(offLadder),
                    offLadder.Value + " is named by no upgrade pair either.");
            }
        }

        /// <summary>
        /// p.16's items, read off the faces he does not choose: every one of the twelve Tier II and
        /// Tier III faces prints one, every Tier I, basic and the Ultimate print none, and the twelve
        /// are the six channels in the counts his shard's table records - three Initiative, three
        /// Defense, three Attack, and one each of Movement, Radius and Range.
        /// </summary>
        [Test]
        public void EveryUnchosenFace_BecomesItsPrintedItem()
        {
            foreach (var definition in HanuCardContent.AllDefinitions)
            {
                var printsAnItem = definition.Progression.ItemIcon != PrintedValueChannel.None;
                var isUpgradeTier =
                    definition.Progression.Tier == CardTier.II ||
                    definition.Progression.Tier == CardTier.III;

                Assert.That(printsAnItem, Is.EqualTo(isUpgradeTier), definition.Id.Value +
                    ": an item is what the face he did NOT choose becomes, so exactly the twelve " +
                    "faces a level-up can leave behind print one.");
            }

            var byChannel = HanuCardContent.AllDefinitions
                .Select(definition => definition.Progression.ItemIcon)
                .Where(channel => channel != PrintedValueChannel.None)
                .GroupBy(channel => channel)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byChannel[PrintedValueChannel.Initiative], Is.EqualTo(3),
                "Even the Odds, This Way! and That Way!.");
            Assert.That(byChannel[PrintedValueChannel.Defense], Is.EqualTo(3),
                "Outnumber, Monkey Twist and Monkey Business.");
            Assert.That(byChannel[PrintedValueChannel.Attack], Is.EqualTo(3),
                "There and Back Again, Safe Travels and Hear Nothing.");
            Assert.That(byChannel[PrintedValueChannel.Movement], Is.EqualTo(1), "Trusted Sidekick.");
            Assert.That(byChannel[PrintedValueChannel.Radius], Is.EqualTo(1), "Pile On.");
            Assert.That(byChannel[PrintedValueChannel.Range], Is.EqualTo(1), "See Nothing.");
            Assert.That(byChannel.Values.Sum(), Is.EqualTo(12));
        }

        /// <summary>
        /// HA-06 as a claim about the whole set: a missing icon is an action or a printed value that
        /// does not exist, never a zero. The inventory table above says it row by row; this says it
        /// where a reader of the shard would look for it, by the groups the shard names.
        /// </summary>
        [Test]
        public void AbsentIcons_StayAbsentRatherThanBecomingZero()
        {
            Assert.That(
                HanuCardContent.Definition(HanuCardContent.HurryUp).ActionDefinitions
                    .Any(action => action.Kind == ActionKind.Movement),
                Is.False,
                "His Silver prints Defense 1 and no Movement icon at all, so there is no Movement " +
                "action on it rather than one printing zero.");

            foreach (var cardId in new[]
            {
                HanuCardContent.FightAndFlight,
                HanuCardContent.FightOrFlight
            })
            {
                Assert.That(
                    PrintedChannels(cardId),
                    Is.EquivalentTo(new[] { PrintedValueChannel.Attack }),
                    cardId.Value + " prints an Attack value and no Range icon, which is why its " +
                    "\"adjacent to you\" is p.13's word rather than a number p.16 raises.");
                Assert.That(
                    HanuCardContent.Definition(cardId).ActionDefinitions
                        .Single(action => action.Kind == ActionKind.Attack).AttackRange,
                    Is.EqualTo(AttackRangeKind.NonRanged));
            }

            foreach (var green in new[]
            {
                HanuCardContent.MonkeyTrick, HanuCardContent.MonkeyTwist,
                HanuCardContent.HearNothing, HanuCardContent.MonkeyBusiness,
                HanuCardContent.SeeNothing
            })
            {
                Assert.That(
                    PrintedChannels(green),
                    Is.EquivalentTo(new[] { PrintedValueChannel.Radius }),
                    green.Value + " prints a Radius icon and no Range one - and See Nothing's Range " +
                    "ITEM is a fact about somebody else's dashboard rather than about this action.");
            }

            var ultimate = HanuCardContent.Definition(HanuCardContent.TheUltimateTrick);
            Assert.That(ultimate.Initiative.IsDefined, Is.False,
                "and p.16's card prints no Initiative either, which is an undefined value rather " +
                "than a zero.");
            Assert.That(ultimate.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.None));
        }

        // -------------------------------------------------------------------------------------------
        // The source corpus
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The source sweep: every composed face maps to exactly one <c>hanu*__front.png</c> stem in his
        /// shard's inventory table, and no stem is left over.
        ///
        /// <para>The table is read back out of <c>Docs/AI/Rules/HERO_HANU.md</c> rather than transcribed
        /// a second time here, which is what makes this case independent of the literal table above:
        /// that one is this fixture's own transcription of the extracted faces and this one is the
        /// shard's, so a drift in either is a disagreement rather than two copies of one mistake.</para>
        ///
        /// <para>The fronts themselves live under a git-ignored directory, so a clean checkout has the
        /// table and not the images. The stem-to-file half therefore runs only where the corpus is
        /// actually present, and says so rather than passing silently.</para>
        /// </summary>
        [Test]
        public void TheShardsInventoryTable_MapsEveryFaceToOneSourceFront()
        {
            var rows = InventoryRows();

            Assert.That(rows.Count, Is.EqualTo(19),
                "His shard records nineteen printed faces and the set composes nineteen.");
            Assert.That(rows.Select(row => row.Stem).Distinct().Count(), Is.EqualTo(19),
                "and each face has a stem of its own - one front per definition, never shared.");

            foreach (var row in rows)
            {
                Assert.That(
                    row.Stem.StartsWith("hanu", StringComparison.Ordinal) && row.Stem.Contains("__"),
                    Is.True,
                    row.Stem + " is not a stem under his source directory.");
            }

            Assert.That(
                rows.Select(row => row.Name).OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(HanuCardContent.AllDefinitions
                    .Select(definition => definition.DisplayName)
                    .OrderBy(name => name, StringComparer.Ordinal)),
                "and the nineteen named faces are exactly the nineteen composed ones - a bijection " +
                "rather than a containment, so an extracted face nobody composed fails here too.");

            Assert.That(
                rows.Select(row => row.Name),
                Is.EqualTo(HanuCardContent.AllDefinitions.Select(definition => definition.DisplayName)),
                "and in the same ORDER, which is what HanuCardContent.InventoryOrder claims to be.");

            foreach (var row in rows)
            {
                var definition = HanuCardContent.AllDefinitions
                    .Single(candidate => candidate.DisplayName == row.Name);
                var progression = definition.Progression;

                Assert.That(
                    progression.IsUltimate ? "-" : definition.Initiative.Value.ToString(),
                    Is.EqualTo(row.Initiative), row.Name + " Initiative");
                Assert.That(SecondaryAmount(definition, PrintedValueChannel.Defense),
                    Is.EqualTo(row.Defense), row.Name + " secondary Defense");
                Assert.That(SecondaryAmount(definition, PrintedValueChannel.Movement),
                    Is.EqualTo(row.Movement), row.Name + " secondary Movement");
                Assert.That(
                    progression.ItemIcon == PrintedValueChannel.None
                        ? "-"
                        : progression.ItemIcon.ToString(),
                    Is.EqualTo(row.Item), row.Name + " item");
                Assert.That(DescribeColourAndTier(definition), Is.EqualTo(row.ColourAndTier),
                    row.Name + " colour and Tier");
            }

            var corpus = SourceCorpusDirectory();
            if (corpus == null)
            {
                Assert.Pass(
                    "The stem-to-front half is skipped: this checkout does not carry the git-ignored " +
                    "Tabletopia corpus, so there is nothing on disk to map onto. The bijection above " +
                    "ran and is the claim that matters.");
                return;
            }

            foreach (var row in rows)
            {
                Assert.That(
                    File.Exists(Path.Combine(corpus, row.Stem + "__front.png")), Is.True,
                    row.Name + " must resolve to " + row.Stem + "__front.png under the source " +
                    "directory.");
            }

            Assert.That(
                Directory.GetFiles(corpus, "*__front.png").Length, Is.EqualTo(19),
                "and the directory holds nineteen fronts and no twentieth.");
        }

        /// <summary>
        /// Every face's printed text is the shard's own sentence for it, transcribed rather than
        /// paraphrased - which is the claim <see cref="CardSourceContent.Transcribed"/> makes on
        /// nineteen definitions and this is what checks it.
        ///
        /// <para>His shard writes one bullet for Helping Hand and Even the Odds together, one for
        /// Unexpected Journey and There and Back Again, and one for Monkey Trick and Monkey Twist,
        /// because two cards printing one sentence at two sets of numbers are one bullet; it names the
        /// Ultimate's and the Handicap's role in parentheses, which is stripped.</para>
        ///
        /// <para><b>One notation is normalized, loudly and in one place.</b> His shard writes the
        /// bullets of a p.10 modal as <c>(1)</c> and <c>(2)</c> where the card prints dots and where
        /// most heroes' shards write <c>•</c>. The content transcribes the dots, which is what the
        /// corpus image shows, so the two spellings are mapped onto each other here rather than one of
        /// them being bent: the comparison is then about the WORDS, which is what a transcription check
        /// is for. Five faces go through it - his five Reds - and nothing else in the sentence is
        /// touched.</para>
        /// </summary>
        [Test]
        public void EveryPrintedRule_IsTranscribedFromTheShard()
        {
            var bullets = PrintedRuleBullets();
            Assert.That(bullets.Count, Is.EqualTo(19),
                "Every one of the nineteen faces has a bullet of its own in the shard's rules list.");

            foreach (var definition in HanuCardContent.AllDefinitions)
            {
                Assert.That(bullets.ContainsKey(definition.DisplayName), Is.True,
                    definition.DisplayName + " has no printed-rule bullet in the shard.");
                Assert.That(
                    definition.SourceContent.PrintedText,
                    Is.EqualTo(bullets[definition.DisplayName]),
                    definition.Id.Value + "'s transcription differs from the shard's.");
            }

            Assert.That(
                bullets.Count(pair => pair.Value.Contains("•")), Is.EqualTo(5),
                "and exactly five of his faces print p.10's bullets - his five Reds - which is what " +
                "the notation mapping above covers and all it covers.");

            Assert.That(
                HanuCardContent.FightOrFlightText,
                Is.EqualTo(HanuCardContent.FightAndFlightText.Replace(
                    "Target a unit adjacent", "Target a hero adjacent")),
                "and the boxed Handicap's sentence is his Gold basic's with one word changed, which " +
                "is HA-05's mirror stated in the two strings themselves.");
        }

        // -------------------------------------------------------------------------------------------
        // Legality, the deferral, and what this build refuses
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition,
        /// asked at the rulebook p.14 or p.11 moment the face itself prints - the check that would
        /// otherwise fault mid-resolution, on a player's turn, with the card already spent. The three
        /// end-of-turn bodies his journeys arm go through it too, because a triggered body is validated
        /// where it is registered rather than where it is armed.
        ///
        /// <para>Seventeen of the nineteen attach a body. The two that do not are p.16's Ultimate,
        /// whose printed content is a standing rule declaration, and p.20's Handicap, whose sentence
        /// stands under the established deferral.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogHisDealingWillRegister()
        {
            var catalog = Catalog();
            var composed = 0;

            foreach (var definition in HanuCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions
                    .Where(action => action.EffectRoot != null))
                {
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

            Assert.That(composed, Is.EqualTo(17),
                "Seventeen bodies: his Gold basic, his Silver basic, the five Reds, the three " +
                "journeys, the two co-moves and the five Greens.");

            foreach (var triggered in HanuEffectContent.JourneyTriggeredEffects())
            {
                Assert.That(
                    EffectDefinitionValidator.Validate(triggered, catalog), Is.Empty,
                    triggered.Id.Value + " is a body the catalog will hold, so it is validated where " +
                    "it is registered rather than where the journey arms it.");
            }

            var rule = HanuCardContent.Definition(HanuCardContent.TheUltimateTrick).UltimateRule;
            Assert.That(rule, Is.Not.Null);
            Assert.That(rule.TriggeredEffect.HasValue, Is.False,
                "and his Ultimate names neither an effect nor a modification: the whole of The " +
                "Ultimate Trick is a standing declaration a rule query derives, because what it " +
                "changes is WHO CHOOSES.");
            Assert.That(rule.Modifications, Is.Empty);
            Assert.That(rule.RuleKind, Is.EqualTo(DelegatedActionQuery.DelegatingRuleKind));
        }

        /// <summary>
        /// EXACTLY these six capability references are his own, EXACTLY these seven faces were refused
        /// before the dealing and NONE is refused now, and his declaration list holds the same six -
        /// milestone 7.9's gate read from three sides at once, so that a contract arriving unnoticed,
        /// a face that quietly stopped asking for one, and a declaration nothing reaches each fail
        /// here.
        ///
        /// <para><b>Milestone 8.71h flipped it</b>, and the list below is what the registration
        /// bought: the seven faces this case used to name as refused - Hurry up!, the three journeys
        /// and the three friendly-pair Greens - are refused by nothing now, while the six contracts
        /// and the seven names stay exactly where they were. Each half of that is asserted, because a
        /// registration that quietly admitted a face nobody meant to register would look the same
        /// from one side.</para>
        /// </summary>
        [Test]
        public void ExactlyTheseContractsAreHisOwn_AndNoFaceOfHisIsRefusedAnyMore()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            var named = HanuCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .Select(action => action.EffectRoot)
                .Concat(HanuEffectContent.JourneyTriggeredEffects())
                .SelectMany(root => root.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();

            Assert.That(
                named.Select(capability => capability.ToString())
                    .Where(value => value.StartsWith("hanu-", StringComparison.Ordinal))
                    .Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(HisOwnContracts),
                "Exactly six of the contracts his bodies name are HIS - a contract of his that no " +
                "body names, and a seventh one appearing, both fail here.");

            Assert.That(
                named.Count(capability =>
                    !capability.ToString().StartsWith("hanu-", StringComparison.Ordinal)),
                Is.GreaterThan(0),
                "and his bodies genuinely reach the roster's own contracts too - the modal Attack, " +
                "the swap, the coupled move, the direct move and the straight line - so the case " +
                "above is not passing because the walk found nothing.");

            Assert.That(
                HanuComponentContent.RegisteredHandlers()
                    .Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(HisOwnContracts),
                "and the declaration list and the walk over his bodies agree, which is what says the " +
                "list 8.71h registered is neither short nor long by one.");
            Assert.That(HanuComponentContent.DeferredHandlers(), Is.Empty,
                "with nothing of his withheld since 8.71h dealt him.");
            Assert.That(
                HanuComponentContent.Handlers().Select(handler => handler.Reference).Distinct().Count(),
                Is.EqualTo(6),
                "and the registered-or-deferred list holds each reference ONCE - 8.40j's BA-18, " +
                "which is what a second concat of the three Active-effect creations would break.");

            Assert.That(
                named.Where(capability => registered.IsRegistered(capability))
                    .Select(capability => capability.ToString())
                    .Where(value => value.StartsWith("hanu-", StringComparison.Ordinal))
                    .Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(HisOwnContracts),
                "and every one of them reaches production registration since the dealing - the same " +
                "six, from the build's own catalog rather than from his declaration list.");

            var refusedFaces = new List<string>();
            foreach (var definition in HanuCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions
                    .Where(action => action.EffectRoot != null))
                {
                    var rejections = action.CardTextTiming != CardTextTiming.None
                        ? EffectDefinitionValidator.ValidateCardText(
                            action.EffectRoot, registered, action.CardTextTiming)
                        : EffectDefinitionValidator.Validate(action.EffectRoot, registered);
                    if (rejections.Count > 0)
                    {
                        refusedFaces.Add(definition.Id.Value);
                    }
                }
            }

            Assert.That(
                refusedFaces.OrderBy(value => value, StringComparer.Ordinal),
                Is.Empty,
                "NONE of his seventeen composed bodies is refused by this build any more. Until " +
                "8.71h these SEVEN were - Hurry up! names the Initiative rewrite, the three " +
                "journeys each name their own Active-effect creation, and the three friendly-pair " +
                "Greens name the pair selection - and `FacesThisBuildRefuses` still records which " +
                "seven, because what the registration bought is exactly that list becoming empty. " +
                "Refused now: " +
                string.Join(", ", refusedFaces.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(
                FacesThisBuildRefuses, Has.Length.EqualTo(7),
                "and the seven are still named, so a face that quietly stopped naming a contract of " +
                "his would move this list rather than pass unnoticed.");
        }

        /// <summary>
        /// The only deferral anywhere on his set is p.20's, on the one boxed face. No dealt face of his
        /// owes a composition to a milestone, and none carries the odd-player trigger.
        /// </summary>
        [Test]
        public void OnlyTheBoxedHandicap_DefersItsPrintedTextComposition()
        {
            foreach (var definition in HanuCardContent.DealtDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    Assert.That(action.TextComposition.IsAwaited, Is.False,
                        definition.Id.Value + " action '" + action.Id.Value + "' must owe nothing: " +
                        "an engine deferral on a dealt face is a card a match would grey out.");
                }
            }

            var handicap = HanuCardContent.Definition(HanuCardContent.FightOrFlight);
            var primary = handicap.ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(primary.TextComposition.IsAwaited, Is.True);
            Assert.That(primary.TextComposition.NamedTrigger,
                Is.EqualTo("first odd-player-count configuration"),
                "the established trigger, spelled exactly as every other roster Handicap spells it.");
            Assert.That(primary.TextComposition.OwningMilestoneId, Is.Null,
                "and it is a deferral rather than a debt: no milestone owes this composition, because " +
                "p.6 returns the card to the box and p.20 is the only thing that brings it back.");
            Assert.That(primary.EffectRoot, Is.Null,
                "A deferred composition and an attached body are mutually exclusive.");
            Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.None),
                "and an uncomposed rider declares no p.14 moment to be applied at.");

            // HA-05, stated where a reader would look for it: the card's BAND reads "Basic Skill" and
            // its number is an ATTACK of 1, over Fight and Flight's own sentence with "a unit" narrowed
            // to "a hero". A Skill has no Attack value to print, so the band is the printing slip and
            // the action is the adjacent Attack both the number and the sentence describe.
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(primary.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged));
            Assert.That(primary.Id, Is.EqualTo(HanuCardContent.PrimaryBasicAttack));
            Assert.That(
                primary.BaseValues.Select(value => (value.Channel, value.Amount)),
                Is.EquivalentTo(new[] { (PrintedValueChannel.Attack, 1) }));

            Assert.That(
                HanuCardContent.DealtDefinitions.SelectMany(definition => definition.ActionDefinitions)
                    .Count(action => action.EffectRoot != null),
                Is.EqualTo(17),
                "and all seventeen composed bodies in his set sit on DEALT faces: the one face that " +
                "is not dealt is one of the two that compose nothing.");
        }

        /// <summary>
        /// One revision string covers the whole set - every definition, every printed body, the three
        /// Active-effect definitions his journeys create and the three end-of-turn bodies those arm.
        /// Milestone 8.71g minted it; the six composition blocks carried six deliberately malformed
        /// placeholders between them, and no face or body may be left on one.
        /// </summary>
        [Test]
        public void OneRealRevision_CoversEveryDefinitionAndEveryBody()
        {
            Assert.That(HanuCardContent.ContentRevision, Is.EqualTo("hanu-8-71g-rev-1"));
            Assert.That(HanuCardContent.ContentSetId, Is.EqualTo("hanu"));

            Assert.That(
                HanuCardContent.AllDefinitions
                    .Select(definition => definition.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { HanuCardContent.ContentRevision }));

            Assert.That(
                HanuCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .Select(action => action.EffectRoot.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { HanuCardContent.ContentRevision }),
                "and the printed bodies carry it too, which is the half a card-only check would miss.");

            Assert.That(
                HanuEffectContent.JourneyTriggeredEffects()
                    .Select(body => body.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { HanuCardContent.ContentRevision }),
                "and so do the three end-of-turn bodies - a triggered body and the card that arms it " +
                "have to name one revision or the catalog cannot resolve the reference.");

            Assert.That(
                HanuCardContent.AllDefinitions.All(definition =>
                    definition.Id.Value.StartsWith("hanu-", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                HanuCardContent.AllDefinitions
                    .Select(definition => definition.ContentAuthority).Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
        }

        /// <summary>
        /// Which of rulebook p.14's moments each face's rider is applied at, as a set. His five modal
        /// Reds carry step 1's, because choosing a bullet is how the target is bound at all; Fight and
        /// Flight carries step 7's, which is where its "After the attack:" straight line lives; and no
        /// Skill of his declares a moment, because a Skill's text IS its action.
        /// </summary>
        [Test]
        public void EveryFacesPrintedMoment_IsTheOneItsOwnWordsName()
        {
            Assert.That(
                HanuCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions
                        .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .Select(action => definition.DisplayName + " " + action.CardTextTiming))
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Fight and Flight AfterTheAttack",
                    "Hurry up! None",
                    "Helping Hand BeforeTheAttack",
                    "Even the Odds BeforeTheAttack",
                    "Outnumber BeforeTheAttack",
                    "Trusted Sidekick BeforeTheAttack",
                    "Pile On BeforeTheAttack",
                    "Unexpected Journey None",
                    "There and Back Again None",
                    "This Way! None",
                    "Safe Travels None",
                    "That Way! None",
                    "Monkey Trick None",
                    "Monkey Twist None",
                    "Hear Nothing None",
                    "Monkey Business None",
                    "See Nothing None",
                    "Fight or Flight None"
                }),
                "Eighteen primaries - the Ultimate prints none - each at the moment its own sentence " +
                "names, with the boxed Handicap's uncomposed and therefore at none.");

            Assert.That(
                HanuCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Any(action => action.SecondCardTextRoot != null),
                Is.False,
                "and no face of his prints a SECOND rider: Safe Travels' trailing move is inside the " +
                "end-of-turn body rather than a second moment on the card.");

            Assert.That(
                HanuCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.Origin != ActionDefinitionOrigin.PrintedPrimary)
                    .All(action =>
                        action.DefaultPolicy.CardTextApplication == CardTextApplicationPolicy.Suppressed),
                Is.True,
                "and p.11 suppresses every ribbon's own card text on every face of his - the sentence " +
                "belongs to the primary on all nineteen, which is why 8.71g drops the conventional " +
                "MovementTextEffectId rather than declaring one nothing names.");
        }

        /// <summary>
        /// The set's own aggregates, which are what 8.71h moves across in one change: his six contracts
        /// in one list, nothing registered, no token and one triggered body per journey, and the
        /// decision labels his faces owe measured against the controls they actually raise.
        /// </summary>
        [Test]
        public void HisContractsAndDecisionLabels_AreOneListEach()
        {
            Assert.That(
                HanuComponentContent.TriggeredEffects.Select(body => body.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "hanu-safe-travels-triggered",
                    "hanu-there-and-back-again-triggered",
                    "hanu-unexpected-journey-triggered"
                }),
                "One end-of-turn body per journey face, which is the whole of what a " +
                "TriggeredEffectCatalog must hold for him: his Ultimate names none.");

            var declarations = HanuEffectContent.DecisionLabels().ToArray();
            Assert.That(declarations, Has.Length.EqualTo(15),
                "The two printed bullets of each of his five Reds, and one per printed distance " +
                "across This Way! and That Way!.");
            Assert.That(
                declarations.Select(declaration => declaration.CardId.Value).Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    HanuCardContent.EvenTheOdds.Value,
                    HanuCardContent.HelpingHand.Value,
                    HanuCardContent.Outnumber.Value,
                    HanuCardContent.PileOn.Value,
                    HanuCardContent.ThatWay.Value,
                    HanuCardContent.ThisWay.Value,
                    HanuCardContent.TrustedSidekick.Value
                }),
                "Seven of his eighteen dealt faces open a window that needs words; the eleven that " +
                "do not either ask for a unit, a hero or a destination - whose control names what it " +
                "would pick - or never ask at all.");

            var required = DecisionLabelRequirementQuery
                .For(HanuCardContent.DealtDefinitions, Catalogs())
                .Concat(DecisionLabelRequirementQuery.For(
                    HanuEffectContent.JourneyTriggeredEffects()))
                .ToArray();
            Assert.That(
                required.Select(requirement =>
                        requirement.CardId.Value + " / " + requirement.NodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(declarations.Select(declaration =>
                        declaration.CardId.Value + " / " + declaration.NodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "and the controls his DEALT faces and his three end-of-turn bodies raise are exactly " +
                "the fifteen he declares words for - derived from the resolver's own rule rather than " +
                "from a second list, so 8.71h's one line into DecisionLabelCatalog cannot land a face " +
                "short. It was this derivation that found the ten Brynn's 8.69c could not have known " +
                "about; his six blocks each derived their own half, which is why it found none.");

            Assert.That(
                required.Count(requirement => requirement.OpensConfirmOrDecline), Is.EqualTo(0),
                "and NOT ONE of his controls is p.10's confirm-or-decline: every window his set opens " +
                "is a modal arm. Fight and Flight's straight line is `if able`, the journeys' and the " +
                "Greens' trailing moves are `up to`, and neither shape ever asks.");
            foreach (var declaration in declarations)
            {
                Assert.That(declaration.Label.Declined, Is.Null.Or.Empty,
                    declaration.NodeId.Value + " opens no decline, so it says nothing about one.");
                Assert.That(declaration.Label.Chosen.Length, Is.LessThanOrEqualTo(60),
                    declaration.NodeId.Value + " must fit the sixty-character bound the decision " +
                    "window draws in - milestone 8.69g's measurement on Brynn's traps.");
            }

            foreach (var declaration in declarations)
            {
                Assert.That(
                    DecisionLabelCatalog.TryGetLabel(
                        declaration.CardId, declaration.NodeId, out var label),
                    Is.True,
                    declaration.NodeId.Value + " is in the catalog since milestone 8.71h: every " +
                    "hero's table joins it at the milestone that DEALS them, which is what the one " +
                    "line into `DecisionLabelCatalog` buys and what this case now holds.");
                Assert.That(label, Is.EqualTo(declaration.Label),
                    declaration.NodeId.Value + " must reach the catalog with the words it declares " +
                    "beside his content, not a copy that has drifted.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // What only the whole set exposes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE SET'S FIRST HEADLINE MEASUREMENT. Hurry up! rewrites a card of HIS OWN printed set and
        /// p.7 reorders around it - which needs two blocks at once and a set to draw the numbers from.
        ///
        /// <para>8.71a proved the rewrite against cards the fixture invented. What it could not do is
        /// ask the question of his own printed numbers: Monkey Trick prints Initiative <b>2</b>, the
        /// lowest in his set, and Helping Hand prints <b>9</b>, so an enemy holding the Green acts after
        /// him - until the 11 the Silver writes puts that card first. The card is his own because the
        /// twenty-first hero is the first whose Silver can be aimed at a face from his own deck, and
        /// because a composed set is what makes "the printed value" a fact rather than a fixture
        /// parameter.</para>
        /// </summary>
        [Test]
        public void HurryUp_RewritesAFaceOfHisOwnSet_AndPSevenReordersAroundIt()
        {
            var order = new Order();
            order.Play(Hanu, HanuCardContent.HelpingHand);
            order.Play(Enemy, HanuCardContent.MonkeyTrick);

            Assert.That(order.Totals()[Hanu], Is.EqualTo(9), "Helping Hand prints Initiative 9.");
            Assert.That(order.Totals()[Enemy], Is.EqualTo(2), "Monkey Trick prints Initiative 2.");
            Assert.That(order.NextActor(), Is.EqualTo(Hanu),
                "so p.7 gives the turn to the higher of his own two printed numbers.");

            order.Reset();
            order.Rewrite(Enemy, 11);

            Assert.That(order.Totals()[Enemy], Is.EqualTo(11),
                "\"Set the printed Initiative value ... to 11\" REPLACES the printed icon rather " +
                "than adding to it, so a 2 becomes an 11 and not a 13.");
            Assert.That(order.NextActor(), Is.EqualTo(Enemy),
                "and p.7's own check, asked fresh, hands the turn to the card the Silver rewrote.");
        }

        /// <summary>
        /// THE SET'S SECOND HEADLINE MEASUREMENT. The Ultimate's delegation, derived over HIS OWN dealt
        /// cards, offers Hanu's seat exactly the actions the delegated hero's own card prints - which
        /// needs 8.71a's rewrite, 8.71b's standing declaration and a composed set at one instant.
        /// </summary>
        [Test]
        public void TheUltimateTrick_RidingHurryUp_HandsHimTheOtherHerosOwnPrintedActions()
        {
            var world = DelegationWorld.Staged();

            Assert.That(world.DelegationFor(Enemy).IsDefined, Is.False,
                "Nothing is delegated before the Silver is performed.");

            world.Rewrite();
            var delegation = world.DelegationFor(Enemy);

            Assert.That(delegation.IsDefined, Is.True);
            Assert.That(delegation.DecidingHeroId, Is.EqualTo(Hanu));
            Assert.That(delegation.DelegatedHeroId, Is.EqualTo(Enemy));
            Assert.That(
                DelegatedActionQuery.DecidingHeroFor(
                    world.Definitions, world.Instances, world.Occupancies, Hanu),
                Is.EqualTo(Hanu),
                "and nothing about his own action moved: the delegation is about the hero he aimed " +
                "the Silver at and nobody else.");

            var offers = ActionOfferQuery
                .GetAvailableTurnActionOffers(
                    world.Definitions, world.Instances, Enemy, world.DelegatedCardId)
                .Where(offer => offer.IsAvailable)
                .Select(offer => offer.ActionDefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offers, Is.EqualTo(new[] { "primary-skill", "secondary-movement" }),
                "The offers Hanu's seat now answers are THAT card's - Monkey Trick's Skill and its " +
                "Movement ribbon - rather than anything from Hanu's own hand: HA-Q3's reading is " +
                "that he chooses among exactly the offers that hero's seat would have had.");

            world.Resolve();
            Assert.That(world.DelegationFor(Enemy).IsDefined, Is.False,
                "and it ends when the rewritten card changes state, which is p.10's own sentence " +
                "and the whole of HA-Q4's lapse list.");
        }

        /// <summary>
        /// THE SET'S THIRD HEADLINE MEASUREMENT. The immunity a Blue journey grants closes a Red's
        /// second bullet - 8.71d's <see cref="ActiveEffectDefinition"/> read by 8.71c's narrowing, on
        /// one board at one instant, which neither block could stage.
        ///
        /// <para>The modification is taken from the journey's OWN definition rather than invented here,
        /// so a change to what "That hero is immune" declares is visible in the Red's offer.</para>
        /// </summary>
        [Test]
        public void AJourneysImmunity_ClosesHisOwnRedsSecondBullet()
        {
            var board = RedBulletBoard.Staged();

            Assert.That(
                board.Subjects(RuleModificationSet.Empty),
                Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }),
                "Even the Odds' second bullet reaches both enemy heroes standing beside his teammate.");

            Assert.That(
                board.Subjects(board.JourneyImmunityOn(Enemy)),
                Is.EqualTo(new[] { SecondEnemy.Value }),
                "and the hero Unexpected Journey named is gone from it - p.19 makes a bullet's " +
                "subject a TARGET and p.18 bars targeting an immune unit, so the immunity his own " +
                "Blue grants is one his own Red has to obey.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers - the catalog
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The bounded catalog his content runs against: everything this build registers, plus his own
        /// six contracts, which 8.71h moves into the first list.
        ///
        /// <para>The <c>GroupBy</c> is milestone 8.40j's BA-18 lesson: handing a registered handler back
        /// in as content is what threw "Capability references must be unique" at Bain's dealing, and it
        /// is what keeps this concat harmless on the day the dealing lands.</para>
        /// </summary>
        // MILESTONE 8.71h. Both of these were a concat of the build's catalogs with his withheld
        // declarations, de-duplicated by reference. The build registers his six and holds his three
        // end-of-turn bodies since the dealing, so the concat is now the same reference twice - which
        // the capability catalog refuses as 8.40j's BA-18 and the triggered-effect catalog refuses in
        // its own words. What both methods answer is the same question they always did: the catalog a
        // configured match would load.
        private static EffectCapabilityCatalog Catalog() =>
            MatchContentCatalogs.ForThisBuild().Capabilities;

        private static MatchContentCatalogs Catalogs() => MatchContentCatalogs.ForThisBuild();

        // -------------------------------------------------------------------------------------------
        // Helpers - describing a face
        // -------------------------------------------------------------------------------------------

        private static void AssertSuccessors(
            CardDefinitionId cardId, params CardDefinitionId[] expected) =>
            Assert.That(
                HanuCardContent.Definition(cardId).Progression.NextTierCandidateIds,
                Is.EquivalentTo(expected), cardId.Value);

        private static IEnumerable<PrintedValueChannel> PrintedChannels(CardDefinitionId cardId) =>
            HanuCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .BaseValues
                .Select(value => value.Channel);

        private static string Describe(CardDefinition definition)
        {
            var primary = definition.ActionDefinitions
                .FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var progression = definition.Progression;

            var primaryText = primary == null
                ? "- Ultimate []"
                : $"{primary.Id.Value} {primary.Kind} {primary.AttackRange} [" +
                    string.Join(", ", primary.BaseValues
                        .OrderBy(value => value.Channel)
                        .Select(value => $"{value.Channel} {value.Amount}")) + "]";

            return string.Join("|", new[]
            {
                definition.Id.Value,
                definition.DisplayName,
                "Init " + (progression.IsUltimate ? "-" : definition.Initiative.Value.ToString()),
                "SecDef " + SecondaryAmount(definition, PrintedValueChannel.Defense),
                "SecMov " + SecondaryAmount(definition, PrintedValueChannel.Movement),
                primaryText,
                DescribeColourAndTier(definition),
                "item " + (progression.ItemIcon == PrintedValueChannel.None
                    ? "-"
                    : progression.ItemIcon.ToString())
            });
        }

        /// <summary>
        /// The amount a face's SECONDARY icon carries, or "-" where it prints none. HA-06 is the whole
        /// reason this is a string: an absent icon is an action that does not exist, which is a
        /// different statement from an action whose value is zero.
        /// </summary>
        private static string SecondaryAmount(CardDefinition definition, PrintedValueChannel channel)
        {
            var action = definition.ActionDefinitions.FirstOrDefault(candidate =>
                candidate.Origin ==
                    (channel == PrintedValueChannel.Defense
                        ? ActionDefinitionOrigin.Reaction
                        : ActionDefinitionOrigin.PrintedSecondary) &&
                candidate.Kind ==
                    (channel == PrintedValueChannel.Defense
                        ? ActionKind.Defense
                        : ActionKind.Movement));
            return action == null
                ? "-"
                : action.BaseValues.Single(value => value.Channel == channel).Amount.ToString();
        }

        private static string DescribeColourAndTier(CardDefinition definition)
        {
            var progression = definition.Progression;
            return progression.IsUltimate
                ? "Ultimate IV"
                : progression.Tier == CardTier.None
                    ? $"{progression.Color} Basic"
                    : $"{progression.Color} {progression.Tier}";
        }

        // -------------------------------------------------------------------------------------------
        // Helpers - the shard, read back
        // -------------------------------------------------------------------------------------------

        private sealed class InventoryRow
        {
            internal InventoryRow(
                string stem, string name, string colourAndTier, string initiative, string defense,
                string movement, string item)
            {
                Stem = stem;
                Name = name;
                ColourAndTier = colourAndTier;
                Initiative = initiative;
                Defense = defense;
                Movement = movement;
                Item = item;
            }

            internal string Stem { get; }
            internal string Name { get; }
            internal string ColourAndTier { get; }
            internal string Initiative { get; }
            internal string Defense { get; }
            internal string Movement { get; }
            internal string Item { get; }
        }

        /// <summary>
        /// His shard's printed-inventory table, parsed into rows. The Ultimate's colour cell reads
        /// Purple and the Handicap's Tier cell reads "Handicap", both of which are normalized here to
        /// the words the progression record uses - and nowhere else, so a change to either is visible.
        /// </summary>
        private static IReadOnlyList<InventoryRow> InventoryRows()
        {
            var rows = new List<InventoryRow>();
            foreach (var line in ShardLines())
            {
                if (!line.StartsWith("| hanu", StringComparison.Ordinal))
                {
                    continue;
                }

                var cells = line.Split('|').Select(cell => cell.Trim()).ToArray();
                var face = cells[2].Split('/').Select(part => part.Trim()).ToArray();
                var colour = face[1];
                var tier = face[2];

                rows.Add(new InventoryRow(
                    cells[1],
                    face[0],
                    colour == "Purple"
                        ? "Ultimate IV"
                        : tier == "Basic" || tier == "Handicap"
                            ? colour + " Basic"
                            : colour + " " + tier,
                    cells[3],
                    cells[4],
                    cells[5],
                    cells[7]));
            }

            return rows;
        }

        /// <summary>
        /// His shard's "Exact printed rules" list, keyed by face name. A bullet naming two faces with a
        /// slash is one sentence printed on two cards, so it is recorded under both names; the Ultimate
        /// and the Handicap name their role in parentheses, which is stripped; and the shard's
        /// <c>(1)</c>/<c>(2)</c> modal bullets are mapped onto the dots the cards print and the content
        /// transcribes - see the calling case for why that mapping is here and what it does not touch.
        ///
        /// <para>Only that section is read. The shard's open-verification list below it is bulleted the
        /// same way - "- **HA-Q1:**" and its siblings - and swallowing those would turn a count of
        /// nineteen into a larger one and quietly weaken the case.</para>
        /// </summary>
        private static IReadOnlyDictionary<string, string> PrintedRuleBullets()
        {
            var bullets = new Dictionary<string, string>(StringComparer.Ordinal);
            var inSection = false;
            foreach (var line in ShardLines())
            {
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    inSection = line.Trim() == "### Exact printed rules";
                    continue;
                }

                if (!inSection || !line.StartsWith("- **", StringComparison.Ordinal))
                {
                    continue;
                }

                var end = line.IndexOf(":**", StringComparison.Ordinal);
                if (end < 0)
                {
                    continue;
                }

                var names = line.Substring("- **".Length, end - "- **".Length);
                var text = line.Substring(end + ":**".Length).Trim()
                    .Replace("(1) ", "• ")
                    .Replace("(2) ", "• ");
                foreach (var name in names.Split('/').Select(part => part.Trim()))
                {
                    bullets[name
                        .Replace(" (Ultimate)", string.Empty)
                        .Replace(" (Handicap)", string.Empty)] = text;
                }
            }

            return bullets;
        }

        private static string[] ShardLines() =>
            File.ReadAllLines(Path.Combine(RepositoryRoot(), "Docs/AI/Rules/HERO_HANU.md"));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null &&
                !File.Exists(Path.Combine(directory.FullName, "Docs/AI/Rules/HERO_HANU.md")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null,
                "His shard is the source of every printed value and must be reachable from the " +
                "working directory this suite runs in.");
            return directory.FullName;
        }

        private static string SourceCorpusDirectory()
        {
            var path = Path.Combine(
                RepositoryRoot(),
                "Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Hanu");
            return Directory.Exists(path) ? path : null;
        }

        // -------------------------------------------------------------------------------------------
        // Helpers - participants and boards
        // -------------------------------------------------------------------------------------------

        private static MatchParticipants Participants()
        {
            var hanuPlayer = new PlayerId("hanu-set-hanu-player");
            var teammatePlayer = new PlayerId("hanu-set-teammate-player");
            var enemyPlayer = new PlayerId("hanu-set-enemy-player");
            var secondEnemyPlayer = new PlayerId("hanu-set-second-enemy-player");

            return new MatchParticipants(
                new[] { new Team(BlueTeam, TeamSide.Blue), new Team(RedTeam, TeamSide.Red) },
                new[]
                {
                    new Player(hanuPlayer, BlueTeam),
                    new Player(teammatePlayer, BlueTeam),
                    new Player(enemyPlayer, RedTeam),
                    new Player(secondEnemyPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Hanu, hanuPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Teammate, teammatePlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(SecondEnemy, secondEnemyPlayer, HeroDashboard.RulebookLayout)
                });
        }

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius)
                    {
                        yield return new HexCoordinate(q, r);
                    }
                }
            }
        }

        /// <summary>
        /// p.7's turn order over cards from HIS OWN set, with no match around it: the smallest thing
        /// <see cref="InitiativeResolutionAuthority"/> is a function of.
        /// </summary>
        private sealed class Order
        {
            private readonly MatchParticipants participants = Participants();
            private readonly TieBreakerState tieBreaker = new TieBreakerState(TeamSide.Blue);
            private readonly HeroItemState items;
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly Dictionary<HeroId, CardDefinitionId> played =
                new Dictionary<HeroId, CardDefinitionId>();
            private InitiativeResolutionAuthority authority;

            internal Order()
            {
                items = new HeroItemState(participants);
                authority = new InitiativeResolutionAuthority(tieBreaker);
            }

            internal void Play(HeroId hero, CardDefinitionId face)
            {
                var definition = HanuCardContent.Definition(face);
                var card = CardInstance.CreateInHand(
                    new CardInstanceId("hanu-set-order-" + hero.Value), definition.Reference, hero);
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
                instances.Add(card);
                played[hero] = face;
            }

            internal void Rewrite(HeroId hero, int value) =>
                Assert.That(
                    CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                        instances.Single(card => card.OwnerId.Equals(hero)),
                        value,
                        new CardInstanceId("hanu-set-order-source")),
                    Is.True,
                    "The staged rewrite is milestone 8.71a's own transition, not a field poke.");

            internal HeroId NextActor()
            {
                var result = authority.TrySelectNextActor(
                    HanuCardContent.AllDefinitions, instances, participants, items,
                    RuleModificationSet.Empty);
                Assert.That(result.Accepted, Is.True);
                Assert.That(authority.PendingChoice, Is.Null,
                    "This board is built so that one card is highest on its own side.");
                return authority.SelectedActor.ActorId;
            }

            internal Dictionary<HeroId, int> Totals() =>
                instances
                    .Where(CardLifecycleAuthority.IsResolvingCardInTurnArea)
                    .ToDictionary(
                        card => card.OwnerId,
                        card => ItemBonusQuery.Apply(
                                PrintedValueQuery.CalculatePrintedInitiative(
                                    card, HanuCardContent.AllDefinitions),
                                items,
                                card.OwnerId,
                                RuleModificationSet.Empty)
                            .Total);

            /// <summary>The same two faces, played again with no rewrite on either.</summary>
            internal void Reset()
            {
                var replay = played.ToArray();
                instances.Clear();
                played.Clear();
                authority = new InitiativeResolutionAuthority(tieBreaker);
                foreach (var entry in replay)
                {
                    Play(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// One board of HIS OWN dealt cards with The Ultimate Trick unlocked beside his dashboard - the
        /// smallest thing <see cref="DelegatedActionQuery"/> is a function of, composed from the set
        /// rather than from faces a fixture invented.
        /// </summary>
        private sealed class DelegationWorld
        {
            private readonly List<CardInstance> instances = new List<CardInstance>();

            private DelegationWorld()
            {
                HanuCard = new CardInstanceId("hanu-set-delegation-hanu");
                DelegatedCardId = new CardInstanceId("hanu-set-delegation-enemy");

                instances.Add(Played(
                    HanuCard, Hanu, HanuCardContent.Definition(HanuCardContent.HurryUp)));
                instances.Add(Played(
                    DelegatedCardId, Enemy,
                    HanuCardContent.Definition(HanuCardContent.MonkeyTrick)));
                instances.Add(new CardInstance(
                    new CardInstanceId("hanu-set-delegation-ultimate"),
                    HanuCardContent.Definition(HanuCardContent.TheUltimateTrick).Reference,
                    Hanu,
                    new CardStateFacts(
                        CardLocation.UltimateArea, false, CardResolutionStatus.None,
                        CardFaceOrientation.FaceUp)));

                Occupancies = new[]
                {
                    new HeroOccupancy(Hanu, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Enemy, new HexCoordinate(2, 0))
                };
            }

            internal static DelegationWorld Staged() => new DelegationWorld();

            internal CardInstanceId HanuCard { get; }
            internal CardInstanceId DelegatedCardId { get; }
            internal IReadOnlyList<CardDefinition> Definitions => HanuCardContent.AllDefinitions;
            internal IReadOnlyList<CardInstance> Instances => instances;
            internal IReadOnlyList<HeroOccupancy> Occupancies { get; }

            internal void Rewrite() =>
                Assert.That(
                    CardLifecycleAuthority.TrySetPrintedInitiativeOverride(
                        instances.Single(card => card.Id.Equals(DelegatedCardId)), 11, HanuCard),
                    Is.True,
                    "The staged rewrite is milestone 8.71a's own transition, not a field poke.");

            internal void Resolve() =>
                Assert.That(
                    CardLifecycleAuthority.TryResolveAfterAction(
                        instances.Single(card => card.Id.Equals(DelegatedCardId))),
                    Is.True);

            internal DelegatedAction DelegationFor(HeroId actorId) =>
                DelegatedActionQuery.ForActor(Definitions, instances, Occupancies, actorId);

            private static CardInstance Played(
                CardInstanceId id, HeroId owner, CardDefinition definition) =>
                new CardInstance(
                    id,
                    definition.Reference,
                    owner,
                    new CardStateFacts(
                        CardLocation.TurnArea, true, CardResolutionStatus.Unresolved,
                        CardFaceOrientation.FaceUp));
        }

        /// <summary>
        /// One board carrying Even the Odds' second bullet: Hanu at the origin, his teammate two spaces
        /// off, and two enemy heroes standing beside that teammate and inside the face's printed Range
        /// of 4.
        /// </summary>
        private sealed class RedBulletBoard
        {
            private static readonly CardInstanceId FaceInstance =
                new CardInstanceId("hanu-set-red-face");

            private readonly MatchParticipants participants = Participants();
            private readonly AdjacentMoveAuthority board;
            private readonly MarkerAttachmentRegister markers = new MarkerAttachmentRegister();
            private readonly HeroItemState heroItems;
            private readonly IReadOnlyList<CardInstance> instances;

            private RedBulletBoard()
            {
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, Disk(6).ToArray()),
                    new[]
                    {
                        new HeroOccupancy(Hanu, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Teammate, new HexCoordinate(2, 0)),
                        new HeroOccupancy(Enemy, new HexCoordinate(3, 0)),
                        new HeroOccupancy(SecondEnemy, new HexCoordinate(2, 1))
                    },
                    Array.Empty<ObstacleOccupancy>());
                heroItems = new HeroItemState(participants);
                instances = new[]
                {
                    CardInstance.CreateInHand(
                        FaceInstance,
                        HanuCardContent.Definition(HanuCardContent.EvenTheOdds).Reference,
                        Hanu)
                };
            }

            internal static RedBulletBoard Staged() => new RedBulletBoard();

            /// <summary>
            /// The modification set Unexpected Journey's OWN Active-effect definition carries, applied
            /// to <paramref name="hero"/> with Hanu as its controller - which is where "This turn: That
            /// hero is immune" puts it.
            /// </summary>
            internal RuleModificationSet JourneyImmunityOn(HeroId hero)
            {
                var declared = HanuActiveEffectContent.UnexpectedJourneyDefinition.Modifications;
                Assert.That(declared, Has.Count.EqualTo(1),
                    "The journey declares exactly one modification, and it is the immunity.");

                return new RuleModificationSet(
                    declared.Select((modification, index) =>
                        AppliedRuleModification.FromActiveEffect(
                            modification,
                            RuleObject.ForHero(hero),
                            new ActiveEffectId("hanu-set-journey-immunity"),
                            Hanu,
                            TeamSide.Blue,
                            index)),
                    Array.Empty<AppliedObstacleOverlay>(),
                    TieBreakerState.Unrevealed);
            }

            /// <summary>Every unit Even the Odds' second bullet admits right now, sorted.</summary>
            internal string[] Subjects(RuleModificationSet modifications)
            {
                var face = HanuCardContent.Definition(HanuCardContent.EvenTheOdds);
                var action = face.ActionDefinitions.Single(value => value.Kind == ActionKind.Attack);
                var body = action.CardTextAt(action.CardTextTiming);
                var bullet = RequestedAttackSequenceQuery.Declared(body)
                    .Single(value => value.BulletId == HanuEffectContent.RedFriendlyBulletNode);

                return AttackSequenceBulletSubjectQuery.FindLegalSubjects(
                        Hanu,
                        new ActionOffer(
                            FaceInstance,
                            face.Reference,
                            action.Id,
                            Hanu,
                            ActionInvocationContext.PrintedPrimary,
                            isAvailable: true,
                            attackRange: action.AttackRange,
                            sourceCardStateTransition: default),
                        bullet,
                        HanuCardContent.AllDefinitions,
                        instances,
                        participants,
                        board,
                        board,
                        markers,
                        heroItems,
                        EffectRosterState.Absent,
                        modifications)
                    .Select(target => target.Kind == AttackTargetKind.Hero
                        ? target.HeroId.Value
                        : target.MinionId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
            }
        }
    }
}
