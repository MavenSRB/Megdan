using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.61i. Min the Dragonmonk's printed set as a SET: nineteen faces, the five p.6 deals
    /// into her hand, the thirteen its deck keeps, the one p.6 returns to the box, the three coloured
    /// ladders p.15 climbs, the twelve items p.16 earns from the faces she does not choose, and the one
    /// revision every definition, every body and every triggered effect carries.
    ///
    /// <para><b>What this fixture is for, and what it leaves to the eight block fixtures.</b> 8.61a
    /// through 8.61h each proved one group of clauses at the effect frame - <c>MinSmokeBombTests</c>,
    /// <c>MinVanishingBlockTests</c>, <c>MinStanceTests</c>, <c>MinMineTests</c>,
    /// <c>MinMineTrapTests</c>, <c>MinGrenadeTests</c>, <c>MinBorrowedRiderTests</c> and
    /// <c>MinItemAndFlurryTests</c> - and nothing here repeats one of those bodies. What none of them
    /// could say is anything about the SET, because until this milestone there was no set: no
    /// inventory order, no starting hand, no deck, no completed ladders, no boxed Handicap and no
    /// <c>DealtDefinitions</c>. Nor could they reach the behaviours that need TWO blocks' faces at
    /// once, which is the other half of this file: her Gold basic borrowing a real Stance's rider, her
    /// Ultimate offering to repeat a real Stance's attack, the permanent item Perfect Self takes out of
    /// her own deck raising a real Stance's Attack, and Ruse's re-placed Smoke bomb being exactly where
    /// Viper's swap finds it. Every one of those four ran on a stand-in until this milestone, because
    /// the face on the other side of the seam was being written on another branch.</para>
    ///
    /// <para><b>Nothing of hers is registered yet, and that is asserted rather than assumed.</b>
    /// Milestone 7.9's gate holds her content out of <see cref="MatchContentCatalogs"/> until 8.61j
    /// deals her, so the catalog these cases validate and drive against is the build's registration
    /// list PLUS exactly the declarations her series deliberately withholds. The gate is asserted from
    /// the other side too, by naming the exact set of capability references this build refuses and the
    /// exact faces that name one - which is how a registration that arrives early, or a deferral that
    /// arrives unnoticed, fails here rather than at her dealing.</para>
    /// </summary>
    [TestFixture]
    public sealed class MinPrintedSetTests
    {
        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly HexCoordinate MinAt = new HexCoordinate(0, 0);

        /// <summary>The attack's target: one space East, which is p.13's "adjacent to you".</summary>
        private static readonly HexCoordinate TargetAt = new HexCoordinate(1, 0);

        /// <summary>Where the far seat stands, out of every reach any of these cases measures.</summary>
        private static readonly HexCoordinate FarAway = new HexCoordinate(-5, 0);

        // -------------------------------------------------------------------------------------------
        // The printed inventory
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One row per face, in her shard's own table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item.
        ///
        /// <para>A dash is what the face prints nothing for, which is MI-05 written out nineteen
        /// times: a missing icon is absent, never zero, and an absent secondary is an action that does
        /// not exist rather than one whose value is 0. The three Blocks print no Defense ribbon at all
        /// and their Block IS the primary; her Silver prints no Movement; three Reds print no Radius;
        /// Inner Strength and Perfect Self print neither Range nor Radius; the Ultimate prints
        /// nothing.</para>
        ///
        /// <para>It is deliberately a literal table rather than something derived, for the reason
        /// <c>WaspVerifiedContentTests</c> and every set after it record: a test that derived its
        /// expectations from the definitions would pass whatever the definitions happened to say. This
        /// one fails if a single number, colour, Tier, icon or name drifts from
        /// <c>Docs/AI/Rules/HERO_MIN.md</c>, which is the document the extracted faces were
        /// transcribed into - and the case after it reads that document back independently, so the two
        /// transcriptions have to agree with each other as well as with the content.</para>
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "min-fast-as-lightning|Fast as Lightning|Init 13|SecDef 1|SecMov 1|primary-ranged-attack Attack Ranged [Attack 2, Range 1]|Gold Basic|item -",
            "min-smoke-bomb|Smoke Bomb|Init 12|SecDef 1|SecMov -|primary-basic-skill Skill None [Radius 3]|Silver Basic|item -",
            "min-crane-stance|Crane Stance|Init 9|SecDef 5|SecMov 5|primary-attack Attack NonRanged [Attack 3]|Red I|item -",
            "min-tiger-stance|Tiger Stance|Init 9|SecDef 5|SecMov 5|primary-attack Attack NonRanged [Attack 4]|Red II|item Initiative",
            "min-viper-stance|Viper Stance|Init 9|SecDef 5|SecMov 5|primary-attack Attack NonRanged [Attack 5, Radius 4]|Red II|item Defense",
            "min-dragon-stance|Dragon Stance|Init 10|SecDef 6|SecMov 5|primary-attack Attack NonRanged [Attack 4]|Red III|item Movement",
            "min-cobra-stance|Cobra Stance|Init 10|SecDef 6|SecMov 5|primary-attack Attack NonRanged [Attack 5, Radius 4]|Red III|item Defense",
            "min-trip-mine|Trip Mine|Init 10|SecDef 4|SecMov 2|primary-skill Skill None [Radius 3]|Blue I|item -",
            "min-cluster-mine|Cluster Mine|Init 11|SecDef 4|SecMov 2|primary-skill Skill None [Radius 3]|Blue II|item Attack",
            "min-death-grenade|Death Grenade|Init 11|SecDef 4|SecMov 2|primary-skill Skill None [Radius 2]|Blue II|item Initiative",
            "min-mine-field|Mine Field|Init 11|SecDef 5|SecMov 2|primary-skill Skill None [Radius 3]|Blue III|item Attack",
            "min-holy-death-grenade|Holy Death Grenade|Init 11|SecDef 5|SecMov 2|primary-skill Skill None [Radius 2]|Blue III|item Initiative",
            "min-poof|Poof!|Init 2|SecDef -|SecMov 3|primary-block Defense None [Range 3]|Green I|item -",
            "min-vanish|Vanish|Init 2|SecDef -|SecMov 3|primary-block Defense None [Range 4]|Green II|item Defense",
            "min-inner-strength|Inner Strength|Init 2|SecDef 2|SecMov 3|primary-skill Skill None []|Green II|item Attack",
            "min-ruse|Ruse|Init 1|SecDef -|SecMov 3|primary-block Defense None [Range 4]|Green III|item Radius",
            "min-perfect-self|Perfect Self|Init 1|SecDef 3|SecMov 3|primary-skill Skill None []|Green III|item Range",
            "min-flurry-of-blows|Flurry of Blows|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -",
            "min-chop-down|Chop Down|Init 12|SecDef 1|SecMov 1|primary-ranged-attack Attack Ranged [Attack 1, Range 1]|Gold Basic|item -"
        };

        /// <summary>
        /// Every capability reference her composed bodies name that milestone 8.61j MOVED INTO the
        /// build's registration list - milestone 7.9's gate, written out and then discharged.
        ///
        /// <para>Until 8.61j this list was <c>WithheldUntilTheDealing</c> and the case below asserted
        /// that the build refused every one of them. The dealing registers all nineteen, so the same
        /// case now asserts the opposite from the same walk: nothing her composed bodies name is
        /// withheld, and these are the ids the dealing was responsible for. The two that are new
        /// handler TYPES - <c>borrow-after-attack-text@1</c> and <c>take-deck-card-as-item@1</c> -
        /// additionally owed a showcase consumer, which is the gate's second half and which
        /// <c>ConfiguredCardMatrixTests</c> is what actually holds.</para>
        ///
        /// <para><b>Two of her series' declarations were never here, because no composed sentence
        /// reaches them</b> - <c>min-reclaim-grenade@1</c>, which 8.61f recorded as unreachable when
        /// the presence branch relocates instead of running the pile short, and
        /// <c>min-place-smoke-bomb-in-range@1</c>, 8.61a's Range-reaching supply Place, which her only
        /// in-range clause (Ruse's) does not use: that clause RELOCATES a standing bomb through the
        /// registered <c>synthetic-place-in-range@1</c>. A capability nothing can start is a
        /// registration 8.61j DROPPED rather than made reachable by nothing, and both declarations are
        /// gone from the tree.</para>
        /// </summary>
        private static readonly string[] RegisteredByTheDealing =
        {
            "borrow-after-attack-text@1",
            "enemy-hero-beside-grenade@1",
            "min-death-grenade-at-end-of-turn@1",
            "min-holy-death-grenade-at-end-of-turn@1",
            "min-inner-strength-double-item-bonuses@1",
            "min-perfect-self-double-item-bonuses@1",
            "min-place-grenade@1",
            "min-place-mine-blast@1",
            "min-place-mine-dud@1",
            "min-place-smoke-bomb-in-radius@1",
            "min-grenade-present@1",
            "min-grenade-space@1",
            "min-reclaim-mine@1",
            "min-smoke-bomb-in-radius@1",
            "min-smoke-bomb-in-range@1",
            "min-smoke-bomb-present@1",
            "min-smoke-bomb-space@1",
            "second-enemy-hero-beside-grenade@1",
            "take-deck-card-as-item@1"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                MinCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.61 verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        /// <summary>
        /// Nineteen faces, eighteen dealt, one boxed - and the colour shape p.5 gives every hero: two
        /// Gold basics, one Silver basic, five each of Red, Blue and Green, and the Purple Ultimate,
        /// which carries no <see cref="CardColor"/> of its own because p.16 unlocks it rather than
        /// upgrading into it.
        /// </summary>
        [Test]
        public void HerSet_IsNineteenFacesInThePrintedColourShape()
        {
            Assert.That(MinCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(MinCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                MinCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { MinCardContent.ChopDown }));
            Assert.That(
                MinCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(MinCardContent.ChopDown),
                "and the dealt eighteen are the nineteen minus exactly that one.");

            var byColour = MinCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2),
                "Fast as Lightning and the boxed Chop Down.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1), "Smoke Bomb.");
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5), "The five Stances.");
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5), "Three mines and two Grenades.");
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5), "Three Blocks and two Skills.");
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "Flurry of Blows.");

            var ultimate = MinCardContent.Definition(MinCardContent.FlurryOfBlows);
            Assert.That(ultimate.Progression.Classification, Is.EqualTo(CardClassification.Ultimate));
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "p.16's permanent ability prints no action at all, so she cannot even discard it to " +
                "defend - which is the absence MI-05 protects, stated for a whole card.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);
        }

        /// <summary>
        /// p.6's five starting cards and the thirteen the deck keeps partition her dealt eighteen
        /// exactly, so no card is both and none is neither - and the five are one of each colour,
        /// which is the shape p.6 states rather than a fact about her in particular.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHerDealtEighteen()
        {
            Assert.That(MinCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(MinCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(MinCardContent.StartingHand.Intersect(MinCardContent.Deck), Is.Empty);
            Assert.That(
                MinCardContent.StartingHand.Concat(MinCardContent.Deck),
                Is.EquivalentTo(MinCardContent.DealtDefinitions.Select(card => card.Id)));

            Assert.That(MinCardContent.StartingHand, Is.EqualTo(new[]
            {
                MinCardContent.FastAsLightning,
                MinCardContent.SmokeBomb,
                MinCardContent.CraneStance,
                MinCardContent.TripMine,
                MinCardContent.Poof
            }));

            Assert.That(
                MinCardContent.StartingHand
                    .Select(id => MinCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.6 deals one of each colour, and hers are the Gold, the Silver and the three Tier I.");

            Assert.That(
                MinCardContent.Deck
                    .Select(MinCardContent.Definition)
                    .Count(definition => definition.Progression.IsUltimate),
                Is.EqualTo(1),
                "p.16's card starts in the deck and is reached by the unlock rather than by a draw.");
            Assert.That(
                MinCardContent.Deck
                    .Select(MinCardContent.Definition)
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
        /// One seat's whole setup, built the way a configured match will build it at 8.61j: eighteen
        /// instances, five of them in hand and thirteen in the deck, with the boxed Handicap nowhere at
        /// all.
        /// </summary>
        [Test]
        public void HerDealtHand_IsFiveInHandAndThirteenInTheDeck_AndNeverTheHandicap()
        {
            var hand = MinCardContent.CreateHand(Min, "min-printed-set");

            Assert.That(hand.AllInstances.Count, Is.EqualTo(18));
            Assert.That(
                hand.AllInstances.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(MinCardContent.ChopDown));

            var cards = MinCardContent.CreateDealtCards(Min, "min-printed-set");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MinCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(MinCardContent.Deck));
            Assert.That(cards.Select(card => card.Id.Value).Distinct().Count(), Is.EqualTo(18),
                "and each instance identity is distinct, which is what the per-seat prefix is for.");
            Assert.That(cards.All(card => card.OwnerId.Equals(Min)), Is.True);

            Assert.That(
                () => MinCardContent.CreateDealtCards(default, "min-printed-set"),
                Throws.ArgumentException,
                "A card without an owner is not a card p.6 dealt to anybody.");
            Assert.That(
                () => MinCardContent.CreateDealtCards(Min, "  "),
                Throws.ArgumentException,
                "and the per-seat prefix is the caller's to name, so an empty one is refused rather " +
                "than silently shared between two seats.");
        }

        // -------------------------------------------------------------------------------------------
        // The upgrade ladders and the items
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.15's three ladders, each forking twice. Four of the six pairs cross a block boundary -
        /// Trip Mine and Cluster Mine name a Grenade, Poof! and Vanish name one of 8.61h's Skills -
        /// which is exactly why they could not be stated until the set was composed.
        /// </summary>
        [Test]
        public void EveryUpgradePair_IsThePrintedLadder()
        {
            AssertSuccessors(
                MinCardContent.CraneStance,
                MinCardContent.TigerStance, MinCardContent.ViperStance);
            AssertSuccessors(
                MinCardContent.TigerStance,
                MinCardContent.DragonStance, MinCardContent.CobraStance);
            AssertSuccessors(
                MinCardContent.ViperStance,
                MinCardContent.DragonStance, MinCardContent.CobraStance);

            AssertSuccessors(
                MinCardContent.TripMine,
                MinCardContent.ClusterMine, MinCardContent.DeathGrenade);
            AssertSuccessors(
                MinCardContent.ClusterMine,
                MinCardContent.MineField, MinCardContent.HolyDeathGrenade);
            AssertSuccessors(
                MinCardContent.DeathGrenade,
                MinCardContent.MineField, MinCardContent.HolyDeathGrenade);

            AssertSuccessors(
                MinCardContent.Poof,
                MinCardContent.Vanish, MinCardContent.InnerStrength);
            AssertSuccessors(
                MinCardContent.Vanish,
                MinCardContent.Ruse, MinCardContent.PerfectSelf);
            AssertSuccessors(
                MinCardContent.InnerStrength,
                MinCardContent.Ruse, MinCardContent.PerfectSelf);

            foreach (var tierThree in MinCardContent.AllDefinitions
                .Where(definition => definition.Progression.Tier == CardTier.III))
            {
                AssertSuccessors(tierThree.Id);
            }

            // The four faces that sit on no ladder at all, and for three different printed reasons:
            // p.6 deals her two Gold and Silver basics and p.15 never upgrades a basic, p.20 keeps the
            // Handicap in the box, and p.16 UNLOCKS the Ultimate rather than upgrading into it.
            foreach (var offLadder in new[]
            {
                MinCardContent.FastAsLightning,
                MinCardContent.SmokeBomb,
                MinCardContent.FlurryOfBlows,
                MinCardContent.ChopDown
            })
            {
                AssertSuccessors(offLadder);
                Assert.That(
                    MinCardContent.AllDefinitions.SelectMany(definition =>
                        definition.Progression.NextTierCandidateIds),
                    Has.No.Member(offLadder),
                    offLadder.Value + " is named by no upgrade pair either.");
            }
        }

        /// <summary>
        /// p.16's items, read off the faces she does not choose: every one of the twelve Tier II and
        /// Tier III faces prints one, every Tier I, basic and the Ultimate print none, and the twelve
        /// are the six channels in the counts her shard's table records - three Initiative, three
        /// Defense, three Attack, and one each of Movement, Radius and Range.
        /// </summary>
        [Test]
        public void EveryUnchosenFace_BecomesItsPrintedItem()
        {
            foreach (var definition in MinCardContent.AllDefinitions)
            {
                var printsAnItem = definition.Progression.ItemIcon != PrintedValueChannel.None;
                var isUpgradeTier =
                    definition.Progression.Tier == CardTier.II ||
                    definition.Progression.Tier == CardTier.III;

                Assert.That(printsAnItem, Is.EqualTo(isUpgradeTier), definition.Id.Value +
                    ": an item is what the face she did NOT choose becomes, so exactly the twelve " +
                    "faces a level-up can leave behind print one.");
            }

            var byChannel = MinCardContent.AllDefinitions
                .Select(definition => definition.Progression.ItemIcon)
                .Where(channel => channel != PrintedValueChannel.None)
                .GroupBy(channel => channel)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byChannel[PrintedValueChannel.Initiative], Is.EqualTo(3),
                "Tiger Stance, Death Grenade and Holy Death Grenade.");
            Assert.That(byChannel[PrintedValueChannel.Defense], Is.EqualTo(3),
                "Viper Stance, Cobra Stance and Vanish.");
            Assert.That(byChannel[PrintedValueChannel.Attack], Is.EqualTo(3),
                "Cluster Mine, Mine Field and Inner Strength.");
            Assert.That(byChannel[PrintedValueChannel.Movement], Is.EqualTo(1), "Dragon Stance.");
            Assert.That(byChannel[PrintedValueChannel.Radius], Is.EqualTo(1), "Ruse.");
            Assert.That(byChannel[PrintedValueChannel.Range], Is.EqualTo(1), "Perfect Self.");
            Assert.That(byChannel.Values.Sum(), Is.EqualTo(12));
        }

        /// <summary>
        /// MI-05 as a claim about the whole set: a missing icon is an action or a printed value that
        /// does not exist, never a zero. The inventory table above says it row by row; this says it
        /// where a reader of the shard would look for it, by the four groups the shard names.
        /// </summary>
        [Test]
        public void AbsentIcons_StayAbsentRatherThanBecomingZero()
        {
            foreach (var cardId in new[]
            {
                MinCardContent.Poof, MinCardContent.Vanish, MinCardContent.Ruse
            })
            {
                var definition = MinCardContent.Definition(cardId);
                Assert.That(
                    definition.ActionDefinitions.Any(action =>
                        action.Origin == ActionDefinitionOrigin.Reaction),
                    Is.False,
                    cardId.Value + " prints NO Defense value at all - its only defense is the Block, " +
                    "so a failed one is p.14's defeat rather than a comparison against zero.");
                Assert.That(
                    definition.ActionDefinitions
                        .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .Kind,
                    Is.EqualTo(ActionKind.Defense),
                    "and the Block is the card's printed PRIMARY.");
            }

            Assert.That(
                MinCardContent.Definition(MinCardContent.SmokeBomb).ActionDefinitions
                    .Any(action => action.Kind == ActionKind.Movement),
                Is.False,
                "Her Silver prints Defense 1 and no Movement icon at all.");

            foreach (var cardId in new[]
            {
                MinCardContent.CraneStance, MinCardContent.TigerStance, MinCardContent.DragonStance
            })
            {
                Assert.That(
                    PrintedChannels(cardId),
                    Is.EquivalentTo(new[] { PrintedValueChannel.Attack }),
                    cardId.Value + " prints an Attack value and neither a Range nor a Radius icon, " +
                    "which is why its \"adjacent to you\" is a word rather than a number p.16 raises.");
            }

            foreach (var cardId in new[]
            {
                MinCardContent.InnerStrength, MinCardContent.PerfectSelf
            })
            {
                Assert.That(
                    PrintedChannels(cardId), Is.Empty,
                    cardId.Value + "'s Skill reaches nothing at a distance, because neither sentence " +
                    "names a unit - so its primary carries no printed value at all.");
            }

            Assert.That(
                MinCardContent.Definition(MinCardContent.FlurryOfBlows).Initiative.IsDefined,
                Is.False,
                "and p.16's card prints no Initiative either, which is an undefined value rather " +
                "than a zero.");
        }

        // -------------------------------------------------------------------------------------------
        // The source corpus
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The source sweep: every composed face maps to exactly one <c>min*__front.png</c> stem in her
        /// shard's inventory table, and no stem is left over.
        ///
        /// <para>The table is read back out of <c>Docs/AI/Rules/HERO_MIN.md</c> rather than transcribed
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
                "Her shard records nineteen printed faces and the set composes nineteen.");
            Assert.That(rows.Select(row => row.Stem).Distinct().Count(), Is.EqualTo(19),
                "and each face has a stem of its own - one front per definition, never shared.");

            foreach (var row in rows)
            {
                Assert.That(
                    row.Stem.StartsWith("min", StringComparison.Ordinal) && row.Stem.Contains("__"),
                    Is.True,
                    row.Stem + " is not a stem under her source directory.");
            }

            Assert.That(
                rows.Select(row => row.Name).OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(MinCardContent.AllDefinitions
                    .Select(definition => definition.DisplayName)
                    .OrderBy(name => name, StringComparer.Ordinal)),
                "and the nineteen named faces are exactly the nineteen composed ones - a bijection " +
                "rather than a containment, so an extracted face nobody composed fails here too.");

            Assert.That(
                rows.Select(row => row.Name),
                Is.EqualTo(MinCardContent.AllDefinitions.Select(definition => definition.DisplayName)),
                "and in the same ORDER, which is what MinCardContent.InventoryOrder claims to be.");

            foreach (var row in rows)
            {
                var definition = MinCardContent.AllDefinitions
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
        /// paraphrased - which is the claim <c>CardSourceContent.Transcribed</c> makes on nineteen
        /// definitions and this is what checks it.
        ///
        /// <para>Her shard writes one bullet for Poof! and Vanish together, because two cards printing
        /// one sentence at two Ranges are one bullet, and it names the Ultimate's and the Handicap's
        /// role in parentheses, which is stripped. Token glyphs are written in brackets - <c>[Smoke
        /// bomb]</c>, <c>[Mine]</c>, <c>[Grenade]</c> - and a transcription that dropped one is a
        /// transcription of something other than the card, which is how the three mine faces' missing
        /// <c>[Mine]</c> glyphs were found at this milestone.</para>
        /// </summary>
        [Test]
        public void EveryPrintedRule_IsTranscribedFromTheShard()
        {
            var bullets = PrintedRuleBullets();
            Assert.That(bullets.Count, Is.EqualTo(19),
                "Every one of the nineteen faces has a bullet of its own in the shard's rules list.");

            foreach (var definition in MinCardContent.AllDefinitions)
            {
                Assert.That(bullets.ContainsKey(definition.DisplayName), Is.True,
                    definition.DisplayName + " has no printed-rule bullet in the shard.");
                Assert.That(
                    definition.SourceContent.PrintedText,
                    Is.EqualTo(bullets[definition.DisplayName]),
                    definition.Id.Value + "'s transcription differs from the shard's.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Legality, the deferral, and what this build refuses
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition,
        /// asked at the rulebook p.14 or p.11 moment the face itself prints - the check that would
        /// otherwise fault mid-resolution, on a player's turn, with the card already spent.
        ///
        /// <para>Seventeen of the nineteen attach a body. The two that do not are p.16's Ultimate,
        /// whose whole printed content is a typed rule with a triggered effect of its own, and p.20's
        /// Handicap, whose sentence stands under the established deferral.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogHerDealingWillRegister()
        {
            var catalog = Catalog();
            var composed = 0;

            foreach (var definition in MinCardContent.AllDefinitions)
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
                "Seventeen bodies: her Silver, her five Reds, her three mines, both Grenades, her " +
                "three Blocks, Inner Strength, Perfect Self and Fast as Lightning. Flurry of Blows " +
                "composes a typed rule and Chop Down composes nothing at all.");

            foreach (var triggered in MinComponentContent.TriggeredEffects)
            {
                Assert.That(
                    EffectDefinitionValidator.Validate(triggered, catalog), Is.Empty,
                    triggered.Id.Value + " is an effect a catalog must hold for a dealt face of hers " +
                    "to resolve, so it is validated on the same terms as a card's own body.");
            }

            var rule = MinCardContent.Definition(MinCardContent.FlurryOfBlows).UltimateRule;
            Assert.That(rule, Is.Not.Null);
            Assert.That(rule.TriggeredEffect, Is.Not.Null,
                "and her Ultimate names an effect rather than a modification family, which is what " +
                "puts min-flurry-of-blows-effect in the triggered list above.");
        }

        /// <summary>
        /// NOTHING her composed bodies name is withheld any more, and every face of hers composes
        /// against what this build registers - milestone 7.9's gate read from both sides after
        /// milestone 8.61j discharged it. Before the dealing this case asserted the exact refusal
        /// list; the same walk now asserts the empty one, so a registration silently dropped back
        /// out and a face that quietly started naming an unregistered contract both fail here.
        /// </summary>
        [Test]
        public void NothingSheNamesIsWithheld_AndEveryFaceOfHersComposes()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            var named = MinCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Concat(MinComponentContent.TriggeredEffects.SelectMany(effect => effect.Nodes))
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();

            Assert.That(
                named.Where(capability => !registered.IsRegistered(capability))
                    .Select(capability => capability.ToString())
                    .Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.Empty,
                "Milestone 8.61j deals her, so this build starts every contract her bodies name.");

            Assert.That(
                RegisteredByTheDealing.Where(id => !registered.Registered
                        .Any(capability => capability.ToString() == id))
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.Empty,
                "and each of the nineteen ids the dealing was responsible for is in the build's " +
                "catalog by name, so an empty walk above cannot pass this case by finding nothing.");

            Assert.That(
                named.Select(capability => capability.ToString())
                    .Intersect(RegisteredByTheDealing),
                Is.EquivalentTo(RegisteredByTheDealing),
                "and her own faces are what reach them - the gate's first half, which is about a " +
                "DEALT card starting the contract rather than about the id existing.");

            var refusedFaces = new List<string>();
            foreach (var definition in MinCardContent.AllDefinitions)
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
                "Before milestone 8.61j FOURTEEN of her seventeen composed bodies were refused by " +
                "what this build registered, and the three that were not - Crane, Tiger and Dragon " +
                "Stance - named only contracts the build already owned, because a push and a direct " +
                "move are the roster's own vocabulary. That asymmetry is exactly what registration " +
                "bought, and none of the fourteen may come back.");
        }

        /// <summary>
        /// The only deferral anywhere on her set is p.20's, on the one boxed face. No dealt face of
        /// hers owes a composition to a milestone, and none carries the odd-player trigger.
        /// </summary>
        [Test]
        public void OnlyTheBoxedHandicap_DefersItsPrintedTextComposition()
        {
            foreach (var definition in MinCardContent.DealtDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    Assert.That(action.TextComposition.IsAwaited, Is.False,
                        definition.Id.Value + " action '" + action.Id.Value + "' must owe nothing: " +
                        "an engine deferral on a dealt face is a card a match would grey out.");
                }
            }

            var handicap = MinCardContent.Definition(MinCardContent.ChopDown);
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

            Assert.That(
                MinCardContent.DealtDefinitions.SelectMany(definition => definition.ActionDefinitions)
                    .Count(action => action.EffectRoot != null),
                Is.EqualTo(17),
                "and all seventeen composed bodies in her set sit on DEALT faces: the one face that " +
                "is not dealt is the one face that composes nothing.");
        }

        /// <summary>
        /// One revision string covers the whole set - every definition, every body reached from one,
        /// and every triggered effect a catalog holds on her behalf. Milestone 8.61i minted it; the
        /// eight composition blocks carried seven deliberately malformed placeholders between them, and
        /// no face, body or effect may be left on one.
        /// </summary>
        [Test]
        public void OneRealRevision_CoversEveryDefinitionEveryBodyAndEveryTriggeredEffect()
        {
            Assert.That(MinCardContent.ContentRevision, Is.EqualTo("min-8-61i-rev-1"));
            Assert.That(MinCardContent.ContentSetId, Is.EqualTo("min"));

            Assert.That(
                MinCardContent.AllDefinitions
                    .Select(definition => definition.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { MinCardContent.ContentRevision }));

            Assert.That(
                MinCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .Select(action => action.EffectRoot.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { MinCardContent.ContentRevision }),
                "and the bodies carry it too, which is the half a card-only check would miss.");

            Assert.That(
                MinComponentContent.TriggeredEffects
                    .Select(effect => effect.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { MinCardContent.ContentRevision }),
                "and so do the seven effects a catalog holds for her - the four mine-trap bodies, " +
                "both Grenades' end-of-turn text and Flurry's repeat.");

            Assert.That(
                MinComponentContent.MineTrapConsequences()
                    .Select(reference => reference.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { MinCardContent.ContentRevision }),
                "and the blast's own references name the same string as the definitions they point " +
                "at, or the link stops resolving the moment a revision moves.");

            Assert.That(
                MinCardContent.AllDefinitions.All(definition =>
                    definition.Id.Value.StartsWith("min-", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                MinCardContent.AllDefinitions
                    .Select(definition => definition.ContentAuthority).Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
        }

        /// <summary>
        /// Which of rulebook p.14's moments each face's rider is applied at, as a set. Her five Reds
        /// and her Gold basic carry step 7's; her three Blocks carry step 4's, which is where p.14 puts
        /// a Defense text; and no Skill of hers declares a moment at all, because a Skill's text IS its
        /// action.
        /// </summary>
        [Test]
        public void EveryFacesPrintedMoment_IsTheOneItsOwnWordsName()
        {
            Assert.That(
                MinCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions
                        .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .Select(action => definition.DisplayName + " " + action.CardTextTiming))
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Fast as Lightning AfterTheAttack",
                    "Smoke Bomb None",
                    "Crane Stance AfterTheAttack",
                    "Tiger Stance AfterTheAttack",
                    "Viper Stance AfterTheAttack",
                    "Dragon Stance AfterTheAttack",
                    "Cobra Stance AfterTheAttack",
                    "Trip Mine None",
                    "Cluster Mine None",
                    "Death Grenade None",
                    "Mine Field None",
                    "Holy Death Grenade None",
                    "Poof! DefenseAction",
                    "Vanish DefenseAction",
                    "Inner Strength None",
                    "Ruse DefenseAction",
                    "Perfect Self None",
                    "Chop Down None"
                }),
                "Eighteen primaries - the Ultimate prints none - each at the moment its own sentence " +
                "names.");

            Assert.That(
                MinCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Any(action => action.SecondCardTextRoot != null),
                Is.False,
                "and no face of hers prints a SECOND rider: Cobra's re-place and Ruse's are inside " +
                "one body rather than beside it.");
        }

        /// <summary>
        /// The set's own component aggregates, which are what milestone 8.61j moved across in one
        /// change: five token definitions for three printed kinds, every handler in one list, the
        /// seven triggered effects, and the ten decision windows her faces open.
        /// </summary>
        [Test]
        public void HerComponentsAndContracts_AreOneListEach()
        {
            Assert.That(
                MinComponentContent.Tokens().Select(token => token.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "min-grenade", "min-mine-blast", "min-mine-dud", "min-mine-facedown",
                    "min-smoke-bomb-token"
                }),
                "Three printed kinds and the facedown face every seat but hers is handed.");

            Assert.That(MinComponentContent.DeferredHandlers(), Is.Empty,
                "Milestone 8.61j deals her, so nothing of hers waits any more - milestone 7.9's " +
                "gate, discharged.");
            Assert.That(
                MinComponentContent.Handlers().Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(MinComponentContent.RegisteredHandlers()
                    .Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "so the registered-or-deferred list is the registered one, exactly.");

            Assert.That(
                MinComponentContent.RegisteredHandlers().Count(), Is.EqualTo(17),
                "Five Smoke bomb operations, two mine placements and a group reclaim, three grenade " +
                "operations and two anchored hero selections, and four Active-effect creations. " +
                "SEVENTEEN rather than the nineteen her blocks declared: the two no composed " +
                "sentence reaches were dropped rather than registered.");

            var build = MatchContentCatalogs.ForThisBuild();
            foreach (var handler in MinComponentContent.RegisteredHandlers())
            {
                Assert.That(
                    build.Capabilities.Registered, Has.Member(handler.Reference),
                    handler.Reference + " is content of hers and must be in the build's catalog.");
            }

            foreach (var dropped in new[] { "min-place-smoke-bomb-in-range@1", "min-reclaim-grenade@1" })
            {
                Assert.That(
                    build.Capabilities.Registered.Select(capability => capability.ToString()),
                    Has.No.Member(dropped),
                    dropped + " is reached by no composed sentence of hers, so the dealing dropped " +
                    "it rather than registering a contract nothing can start.");
            }

            foreach (var token in MinComponentContent.Tokens())
            {
                Assert.That(
                    build.Components.IsRegistered(token.Reference), Is.True,
                    token.Id.Value + " is a piece her dealt cards put on a board, the facedown face " +
                    "included - a client loads a masked token's definition by id from this catalog.");
            }

            foreach (var effect in MinComponentContent.TriggeredEffects)
            {
                Assert.That(
                    build.TriggeredEffects.Definitions.Select(definition => definition.Id),
                    Has.Member(effect.Id),
                    effect.Id.Value + " is named BY REFERENCE - by a token definition, by an Active " +
                    "effect or by a p.16 permanent ability - and the owner walk resolves that " +
                    "reference against this catalog.");
            }

            Assert.That(
                MinEffectContent.DecisionLabels().Count(), Is.EqualTo(11),
                "Her two swapping Stances and Dragon's step, Cobra's and Ruse's re-places, both " +
                "Grenades' \"up to N\" passes and Perfect Self's one-or-both modal.");
            foreach (var declaration in MinEffectContent.DecisionLabels())
            {
                Assert.That(
                    DecisionLabelCatalog.TryGetLabel(
                        declaration.CardId, declaration.NodeId, out var label),
                    Is.True,
                    declaration.CardId.Value + " / " + declaration.NodeId.Value +
                    " is declared beside her content and missing from the catalog.");
                Assert.That(label, Is.EqualTo(declaration.Label));
            }
        }

        // -------------------------------------------------------------------------------------------
        // What only the whole set exposes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her Gold basic borrows a REAL Stance's rider. Milestone 8.61g proved the borrow against a
        /// stand-in Red card of its own making, because 8.61c's five were being composed on another
        /// branch; this is the same sentence with the card the set actually prints - Crane Stance,
        /// resolved in her turn area - and what arrives is Crane's own push node, pushing the enemy the
        /// attack just hit.
        /// </summary>
        [Test]
        public void FastAsLightning_BorrowsARealStancesRider_AndPushesWithIt()
        {
            var turn = new Turn(
                MinCardContent.FastAsLightning,
                MinCardContent.PrimaryRangedAttack,
                resolvedRedCard: MinCardContent.CraneStance);

            turn.PlayAgainstTheEnemyHero();

            Assert.That(turn.PendingChoice, Is.Not.Null,
                "Step 7 borrowed a body and that body stopped to ask something.");
            Assert.That(turn.PendingChoice.NodeId.Value, Is.EqualTo("crane-subject"),
                "and the question is Crane Stance's own push-subject node, run inside Fast as " +
                "Lightning's frame - which is what 'apply the After the attack text of your resolved " +
                "red card' means.");

            turn.AnswerFirstCandidate();
            Assert.That(turn.PendingChoice, Is.Not.Null, "The subject chosen, the push asks how far.");
            Assert.That(turn.PendingChoice.NodeId.Value, Is.EqualTo("crane-push"));
            turn.Answer(EffectValue.Integer(3));

            Assert.That(turn.EnemySpace, Is.EqualTo(new HexCoordinate(4, 0)),
                "and the borrowed push moved the enemy the printed three spaces straight away from " +
                "her, which is Crane Stance's own clause run off Fast as Lightning's board.");
        }

        /// <summary>
        /// The negative the same seam owes: a Red card still in her HAND is no pool at all, so the
        /// borrowed rider applies nothing and the attack is untouched (p.10's impossible step).
        /// </summary>
        [Test]
        public void FastAsLightning_BorrowsNothing_WhenHerStanceIsStillInHand()
        {
            var turn = new Turn(
                MinCardContent.FastAsLightning,
                MinCardContent.PrimaryRangedAttack,
                resolvedRedCard: MinCardContent.CraneStance,
                theRedCardIsStillInHand: true);

            turn.PlayAgainstTheEnemyHero();

            Assert.That(turn.PendingChoice, Is.Null,
                "Nothing was borrowed, so nothing stopped to ask - and the attack itself resolved.");
            Assert.That(turn.EnemySpace, Is.EqualTo(TargetAt),
                "and nobody was pushed, because the clause found no card to read.");
        }

        /// <summary>
        /// Her Ultimate offers to repeat a REAL Stance's attack. Milestone 8.61h proved the trigger
        /// against other heroes' cards standing in for hers; this asks it with Flurry of Blows unlocked
        /// out of her own deck and Crane Stance as the attack that was performed - and asks the
        /// negative p.10 prints in the same breath, that a repeated instance never triggers it again.
        /// </summary>
        [Test]
        public void FlurryOfBlows_OffersToRepeatHerOwnStanceAttack_ButNeverARepeatedOne()
        {
            var world = TriggerWorld.WithFlurryUnlocked();

            var offered = world.RunTheTriggerAfter(
                MinCardContent.CraneStance, wasRepeatedInstance: false);
            Assert.That(offered, Is.Not.Null,
                "\"Each time after you perform an attack action you may repeat it once\" - and her " +
                "Red Stance IS an attack action, which is the whole reason 8.61h minted a trigger " +
                "condition rather than reusing Tigerclaw's basic-action one.");
            Assert.That(offered.NodeId.Value, Is.EqualTo("flurry-of-blows-repeat"));
            Assert.That(offered.DeclineInvocation, Is.Not.Null, "\"you MAY repeat it\".");

            Assert.That(
                world.RunTheTriggerAfter(MinCardContent.CraneStance, wasRepeatedInstance: true),
                Is.Null,
                "p.10: \"You cannot repeat a repeated action\", so the second instance's own " +
                "ActionEnded offers nothing at all.");

            Assert.That(
                world.RunTheTriggerAfter(MinCardContent.SmokeBomb, wasRepeatedInstance: false),
                Is.Null,
                "and a Skill is not an attack action, so her Silver's own ending offers nothing " +
                "either.");
        }

        /// <summary>
        /// Perfect Self's second bullet takes a Tier II card out of HER OWN deck and the item it
        /// becomes raises a REAL Stance's Attack on the next card - the p.15/p.16 path end to end,
        /// across three blocks: 8.61h's handler, 8.61d's Cluster Mine and 8.61c's Crane Stance.
        ///
        /// <para>Both bullets are taken, which is what "Choose one or both" admits, so the doubling the
        /// first one creates is standing when the item is read: p.16's +1 becomes +2 for the rest of
        /// the round, and Crane Stance's printed 3 reads 5.</para>
        /// </summary>
        [Test]
        public void PerfectSelfsPermanentItem_ComesFromHerOwnDeck_AndRaisesAStancesAttack()
        {
            var world = ItemWorld.Create();

            Assert.That(world.AttackTotalOf(MinCardContent.CraneStance), Is.EqualTo(3),
                "Crane Stance's printed Attack, before she has any item at all.");

            var taken = world.PlayPerfectSelfTakingBothBullets(MinCardContent.ClusterMine);

            Assert.That(taken, Is.EqualTo(MinCardContent.ClusterMine),
                "The offer admits her own Tier II deck cards and this is one of the six.");
            Assert.That(
                world.Items().Select(item => item.Icon),
                Is.EquivalentTo(new[] { PrintedValueChannel.Attack }),
                "and what a set-aside card grants is its own printed Item icon - Cluster Mine's is " +
                "Attack.");
            Assert.That(world.CardLocationOf(MinCardContent.ClusterMine),
                Is.EqualTo(CardLocation.ItemSlot),
                "and it went through p.15's set-aside state into the dashboard slot a permanent item " +
                "lives in, so the card has left her deck for good - which is what makes it an ITEM " +
                "rather than a card she could still play.");

            Assert.That(world.AttackTotalOf(MinCardContent.CraneStance), Is.EqualTo(5),
                "p.16's item adds 1 and Perfect Self's first bullet doubles what an item adds for " +
                "the round, so her Tier I Stance hits at 3 + 2 while both bullets stand.");

            world.EndTheRound();
            Assert.That(world.AttackTotalOf(MinCardContent.CraneStance), Is.EqualTo(4),
                "\"THIS round\" expires and the permanent item does not: the same Stance reads 3 + 1 " +
                "from the next round on.");
        }

        /// <summary>
        /// Ruse's re-placed Smoke bomb is exactly where Viper Stance's swap finds it, in the same
        /// round. Two faces from two blocks and one piece: 8.61b's Block moves the bomb as the last
        /// clause of a defence, and 8.61c's rider reads it off the board on her own turn afterwards.
        /// </summary>
        [Test]
        public void RusesRePlacedBomb_IsWhereVipersSwapFindsIt_InTheSameRound()
        {
            var round = new TokenRound();

            var rePlacedTo = round.DefendWithRuse();
            Assert.That(rePlacedTo, Is.Not.EqualTo(round.BombStartedAt),
                "Ruse's third sentence moved the piece, which is the state the next face reads.");
            Assert.That(round.BombSpace, Is.EqualTo(rePlacedTo));
            Assert.That(round.MinSpace, Is.EqualTo(round.BombStartedAt),
                "and its first sentence moved HER, to the space the bomb was standing on - p.19's " +
                "Swap, which is what makes the two spaces the case measures different ones.");

            var standingAt = round.MinSpace;
            round.RunVipersRider();
            Assert.That(round.PendingNodeId, Is.EqualTo("viper"),
                "\"You may swap with a [Smoke bomb] Smoke bomb in radius\" is offered at all, which " +
                "on this board is only true because the bomb is where Ruse left it: the offer is the " +
                "bounded fact, and a bomb out of radius publishes nothing to swap with.");

            round.TakeTheSwap();
            Assert.That(round.MinSpace, Is.EqualTo(rePlacedTo),
                "Taking it puts her on the space RUSE chose - the second face found the piece exactly " +
                "where the first one left it, in the same round.");
            Assert.That(round.BombSpace, Is.EqualTo(standingAt),
                "and the bomb took her space, which is the other half of one Swap.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The build's own catalog, which since milestone 8.61j holds every contract of hers.
        ///
        /// <para>The three deferred lists are still concatenated and are all empty now, which is
        /// deliberate rather than dead: they are the shape this fixture used before the dealing, and
        /// keeping the concat means a contract that fell BACK out of the build and into a deferred
        /// list would still be composed here and would fail in the cases that ask the build
        /// directly, instead of failing everywhere at once with a composition error. The
        /// <c>GroupBy</c> is milestone 8.40j's BA-18 lesson: handing a registered handler back in as
        /// content is what threw "Capability references must be unique" at Bain's dealing.</para>
        /// </summary>
        private static EffectCapabilityCatalog Catalog()
        {
            var build = MatchContentCatalogs.ForThisBuild().Capabilities;
            return new EffectCapabilityCatalog(
                build.Registered.Select(build.Load)
                    .Concat(MinComponentContent.DeferredHandlers())
                    .Concat(SyntheticBorrowedCardTextCapabilities.DeferredHandlers())
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
        }

        private static ComponentDefinitionCatalog Components() =>
            new ComponentDefinitionCatalog(
                MinComponentContent.Tokens(), Array.Empty<MarkerDefinition>());

        private static MatchContentCatalogs Catalogs() =>
            new MatchContentCatalogs(
                Catalog(),
                Components(),
                new TriggeredEffectCatalog(MinComponentContent.TriggeredEffects));

        private static void AssertSuccessors(
            CardDefinitionId cardId, params CardDefinitionId[] expected) =>
            Assert.That(
                MinCardContent.Definition(cardId).Progression.NextTierCandidateIds,
                Is.EquivalentTo(expected), cardId.Value);

        private static IEnumerable<PrintedValueChannel> PrintedChannels(CardDefinitionId cardId) =>
            MinCardContent.Definition(cardId).ActionDefinitions
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
        /// The amount a face's SECONDARY icon carries, or "-" where it prints none. MI-05 is the whole
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

        // ----- the shard, read back -----

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
        /// Her shard's printed-inventory table, parsed into rows. The Ultimate's colour cell reads
        /// Purple and the Handicap's Tier cell reads "Handicap", both of which are normalized here to
        /// the words the progression record uses - and nowhere else, so a change to either is visible.
        /// </summary>
        private static IReadOnlyList<InventoryRow> InventoryRows()
        {
            var rows = new List<InventoryRow>();
            foreach (var line in ShardLines())
            {
                if (!line.StartsWith("| min", StringComparison.Ordinal))
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
        /// Her shard's "Exact printed rules" list, keyed by face name. The bullet naming two faces with
        /// a slash is one sentence printed on two cards, so it is recorded under both names; the
        /// Ultimate and the Handicap name their role in parentheses, which is stripped.
        ///
        /// <para>Only that section is read. The shard's open-verification list below it is bulleted the
        /// same way - "- **MI-Q1 (user, settled 2026-09-19):**" and its siblings - and swallowing those
        /// would turn a count of nineteen into a larger one and quietly weaken the case.</para>
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
                var text = line.Substring(end + ":**".Length).Trim();
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
            File.ReadAllLines(Path.Combine(RepositoryRoot(), "Docs/AI/Rules/HERO_MIN.md"));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null &&
                !File.Exists(Path.Combine(directory.FullName, "Docs/AI/Rules/HERO_MIN.md")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null,
                "Her shard is the source of every printed value and must be reachable from the " +
                "working directory this suite runs in.");
            return directory.FullName;
        }

        private static string SourceCorpusDirectory()
        {
            var path = Path.Combine(
                RepositoryRoot(),
                "Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Min");
            return Directory.Exists(path) ? path : null;
        }

        // ----- boards -----

        private static BoardMapDefinition Map()
        {
            var playable = new List<HexCoordinate>();
            for (var q = -6; q <= 6; q++)
            {
                for (var r = -6; r <= 6; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    if (HexDistanceQuery.Calculate(MinAt, coordinate) <= 6)
                    {
                        playable.Add(coordinate);
                    }
                }
            }

            return BoardMapDefinition.CreateSynthetic(
                new MapId("min-printed-set-map"), 1, playable, Array.Empty<HexCoordinate>());
        }

        /// <summary>
        /// A card the other two seats commit and defend with: Initiative 1, so hers is always the
        /// highest committed and the turn under test is hers, and Defense 9 against her printed 2 or 3,
        /// so p.14's comparison leaves the target on the board for a rider to name.
        /// </summary>
        private static readonly CardDefinition Wall = new CardDefinition(
            new CardDefinitionId("min-printed-set-wall"), "min-printed-set-fixture-rev-1",
            "TEST - Min printed-set wall", new InitiativeValue(1),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("wall-move"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)),
                new ActionDefinition(
                    new ActionDefinitionId("wall-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        /// <summary>
        /// One of Min's turns, driven through the production round: one of her real faces committed and
        /// started, the enemy beside her answering with a card that holds, and whatever the rider asks
        /// answered by the case.
        /// </summary>
        private sealed class Turn
        {
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] cards;
            private readonly RoundResolutionAuthority round;
            private readonly ActionDefinitionId actionId;
            private readonly CardInstanceId sourceId = new CardInstanceId("min-printed-set-card");

            internal Turn(
                CardDefinitionId cardId,
                ActionDefinitionId actionDefinitionId,
                CardDefinitionId resolvedRedCard = default,
                bool theRedCardIsStillInHand = false)
            {
                actionId = actionDefinitionId;
                var card = MinCardContent.Definition(cardId);
                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                var instances = new List<CardInstance>
                {
                    CardInstance.CreateInHand(sourceId, card.Reference, Min),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-commit"), Wall.Reference, Enemy),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-shield"), Wall.Reference, Enemy),
                    CardInstance.CreateInHand(
                        new CardInstanceId("enemy-ally-commit"), Wall.Reference, EnemyAlly)
                };

                var composed = new List<CardDefinition> { card, Wall };
                if (resolvedRedCard.IsDefined)
                {
                    var red = MinCardContent.Definition(resolvedRedCard);
                    composed.Add(red);
                    instances.Add(new CardInstance(
                        new CardInstanceId("min-printed-set-red"),
                        red.Reference,
                        Min,
                        theRedCardIsStillInHand
                            ? CardStateFacts.InitialInHand()
                            : new CardStateFacts(
                                CardLocation.TurnArea, true, CardResolutionStatus.Resolved,
                                CardFaceOrientation.FaceUp)));
                }

                definitions = composed.ToArray();
                cards = instances.ToArray();
                Board = new AdjacentMoveAuthority(
                    Map(),
                    new[]
                    {
                        new HeroOccupancy(Min, MinAt),
                        new HeroOccupancy(Enemy, TargetAt),
                        new HeroOccupancy(EnemyAlly, FarAway)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                var markers = new MarkerAttachmentRegister();
                round = new RoundResolutionAuthority(
                    definitions, cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForMatch(
                        new CardActionResolutionAuthority(Catalogs(), Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null,
                        startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));

                Accept(round.TryCommitCard(Min, sourceId));
                Accept(round.TryCommitCard(Enemy, new CardInstanceId("enemy-commit")));
                Accept(round.TryCommitCard(EnemyAlly, new CardInstanceId("enemy-ally-commit")));
            }

            internal AdjacentMoveAuthority Board { get; }

            internal PendingEffectChoice PendingChoice =>
                round.CurrentTurnAuthority?.PendingEffectChoice;

            internal HexCoordinate EnemySpace =>
                Board.Occupancies.Single(value => value.HeroId.Equals(Enemy)).Coordinate;

            internal void PlayAgainstTheEnemyHero()
            {
                Assert.That(round.CurrentTurnAuthority.SelectedActor.ActorId, Is.EqualTo(Min),
                    "The two other seats commit Initiative 1, so her face acts first.");

                var offer = ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, cards, Min, sourceId)
                    .Single(candidate => candidate.ActionDefinitionId.Equals(actionId));
                Accept(round.TryStartAttack(new ActionResolutionId("min-printed-set"), offer));

                var target = round.CurrentTurnAuthority.PendingAttackTargetChoice;
                Accept(round.TryAnswerAttackTarget(
                    new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));

                var defence = round.CurrentTurnAuthority.PendingDefenseChoice;
                Accept(round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defence.ResolutionId, defence.ChoiceId,
                    defence.LegalDefenseOffers
                        .Single(value => value.SourceCardInstanceId.Value == "enemy-shield")
                        .SourceCardInstanceId)));
            }

            internal void AnswerFirstCandidate()
            {
                var choice = PendingChoice;
                Assert.That(choice, Is.Not.Null, "Nothing is waiting to be answered.");
                Accept(round.TryAnswerEffectChoice(
                    choice.SelectInvocations.Count > 0
                        ? choice.SelectInvocations[0]
                        : choice.ConfirmInvocation));
            }

            /// <summary>Answer the pending question with the named candidate.</summary>
            internal void Answer(EffectValue value)
            {
                var choice = PendingChoice;
                Assert.That(choice, Is.Not.Null, "Nothing is waiting to be answered.");
                Accept(round.TryAnswerEffectChoice(choice.SelectInvocations.Single(
                    invocation => invocation.Candidate.Value.Equals(value))));
            }

            private void Accept(RoundResolutionResult result) =>
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" +
                    result.CardActionRejectionReason);
        }

        /// <summary>
        /// Her Ultimate unlocked out of her own deck, and the production path from "she performed an
        /// attack action" to the decision a player answers.
        /// </summary>
        private sealed class TriggerWorld
        {
            private readonly MatchParticipants participants =
                StagedBoardParticipants.CreateWithBlueAlly();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly EffectCapabilityCatalog capabilities = Catalog();
            private readonly HeroItemState heroItems;

            private TriggerWorld()
            {
                heroItems = new HeroItemState(participants);
                foreach (var definition in MinCardContent.DealtDefinitions)
                {
                    instances.Add(CardInstance.CreateInDeck(
                        Instance(definition.Id), definition.Reference, Min));
                }
            }

            internal static TriggerWorld WithFlurryUnlocked()
            {
                var world = new TriggerWorld();
                var card = world.instances.Single(instance =>
                    instance.DefinitionReference.DefinitionId.Equals(MinCardContent.FlurryOfBlows));
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(card), Is.True,
                    "p.16 unlocks her Purple card out of the deck rather than upgrading into it.");
                return world;
            }

            internal PendingEffectChoice RunTheTriggerAfter(
                CardDefinitionId cardId, bool wasRepeatedInstance)
            {
                var authority = new TriggeredEffectResolutionAuthority(
                    new TriggeredEffectCatalog(MinComponentContent.TriggeredEffects), capabilities);
                authority.ApplyCheckpoint(
                    new ActiveEffectLifecycle(),
                    new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                    new TieBreakerState(TeamSide.Red),
                    MinCardContent.DealtDefinitions,
                    instances,
                    participants,
                    RuleModificationSet.Empty,
                    heroItems,
                    UltimateTriggeredEffectSourceQuery.Derive(
                        UltimateUnlockQuery.Derive(MinCardContent.DealtDefinitions, instances),
                        Board(),
                        participants,
                        default,
                        default,
                        default,
                        PerformedAttackAction.ForCompletedAction(
                            Min,
                            Instance(cardId),
                            MinCardContent.PrimaryAttack,
                            MinCardContent.Definition(cardId).ActionDefinitions
                                .Single(action =>
                                    action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                                .Kind,
                            wasRepeatedInstance)),
                    _ => true);
                return authority.PendingResolution?.PendingChoice;
            }

            private static CardInstanceId Instance(CardDefinitionId cardId) =>
                new CardInstanceId("min-trigger-" + cardId.Value);

            private AdjacentMoveAuthority Board() => new AdjacentMoveAuthority(
                Map(),
                new[] { new HeroOccupancy(Min, MinAt), new HeroOccupancy(Enemy, TargetAt) },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
        }

        /// <summary>
        /// Her whole dealt set in one seat, so Perfect Self's second bullet reads the deck p.6 actually
        /// leaves her with, and the item it takes is measured against a real Stance's Attack.
        /// </summary>
        private sealed class ItemWorld
        {
            private readonly MatchParticipants participants =
                StagedBoardParticipants.CreateWithBlueAlly();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly EffectCapabilityCatalog capabilities = Catalog();
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly HeroItemState heroItems;

            private ItemWorld()
            {
                heroItems = new HeroItemState(participants);
                foreach (var definition in MinCardContent.DealtDefinitions)
                {
                    instances.Add(
                        definition.Id.Equals(MinCardContent.PerfectSelf)
                            ? CardInstance.CreateInHand(
                                Instance(definition.Id), definition.Reference, Min)
                            : CardInstance.CreateInDeck(
                                Instance(definition.Id), definition.Reference, Min));
                }
            }

            internal static ItemWorld Create() => new ItemWorld();

            internal IReadOnlyList<HeroItem> Items() => heroItems.GetItems(Min);

            internal CardLocation CardLocationOf(CardDefinitionId cardId) => instances
                .Single(card => card.DefinitionReference.DefinitionId.Equals(cardId))
                .State.Location;

            internal int AttackTotalOf(CardDefinitionId cardId) => AttackValueQuery.Calculate(
                new ActionOffer(
                    Instance(cardId),
                    MinCardContent.Definition(cardId).Reference,
                    MinCardContent.PrimaryAttack,
                    Min,
                    ActionInvocationContext.PrintedPrimary,
                    true,
                    AttackRangeKind.NonRanged,
                    default),
                MinCardContent.DealtDefinitions,
                heroItems,
                Modifications()).Total;

            internal void EndTheRound() => timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.RoundEnd, 1, 1),
                new TieBreakerState(TeamSide.Red));

            /// <summary>
            /// Perfect Self's Skill run through the production frame: both arms of the modal, then the
            /// named Tier II card out of her deck. Returns the card that became an item.
            /// </summary>
            internal CardDefinitionId PlayPerfectSelfTakingBothBullets(CardDefinitionId wanted)
            {
                var context = new EffectResolutionContext(
                    MinEffectContent.PerfectSelfSkillEffect(MinCardContent.ContentRevision),
                    capabilities,
                    participants,
                    timing,
                    1,
                    1,
                    Modifications(),
                    EffectCardState.Of(MinCardContent.DealtDefinitions, instances),
                    ActionKind.Skill,
                    heroItems: heroItems);
                var step = EffectResolutionSession.Start(
                    new EffectResolutionId("min-printed-set-perfect-self"),
                    Instance(MinCardContent.PerfectSelf),
                    MinCardContent.Definition(MinCardContent.PerfectSelf).Reference,
                    MinCardContent.PrimarySkill,
                    Min,
                    Min,
                    ActionInvocationContext.PrintedPrimary,
                    context);
                Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());

                var taken = default(CardDefinitionId);
                var frame = step.Frame;
                var guard = 0;
                while (frame != null && frame.IsAwaitingChoice)
                {
                    Assert.That(++guard, Is.LessThan(10),
                        "A composed Skill settles in a few answers.");
                    var choice = frame.PendingChoice;
                    var options = choice.Candidates
                        .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Option)
                        .ToArray();
                    if (options.Length > 0)
                    {
                        Assert.That(options.Length, Is.EqualTo(2),
                            "\"Choose one or both\" offers exactly the two bullets the card prints.");
                        foreach (var option in options)
                        {
                            frame = Answer(frame, choice, option, context);
                            if (frame == null || !frame.IsAwaitingChoice ||
                                !frame.PendingChoice.ChoiceId.Equals(choice.ChoiceId))
                            {
                                break;
                            }

                            choice = frame.PendingChoice;
                        }

                        if (frame != null && frame.IsAwaitingChoice &&
                            frame.PendingChoice.ChoiceId.Equals(choice.ChoiceId))
                        {
                            frame = Confirm(frame, choice, context);
                        }

                        continue;
                    }

                    var cards = choice.Candidates
                        .Where(candidate =>
                            candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.CardInstance)
                        .ToArray();
                    Assert.That(cards, Is.Not.Empty, "The deck choice offers her Tier II cards.");
                    Assert.That(
                        cards.Select(candidate => DefinitionOf(candidate.Value.CardInstanceId)),
                        Is.EquivalentTo(new[]
                        {
                            MinCardContent.TigerStance, MinCardContent.ViperStance,
                            MinCardContent.ClusterMine, MinCardContent.DeathGrenade,
                            MinCardContent.Vanish, MinCardContent.InnerStrength
                        }),
                        "and they are exactly the six Tier II faces p.6 left in her deck.");

                    var picked = cards.Single(candidate =>
                        DefinitionOf(candidate.Value.CardInstanceId).Equals(wanted));
                    taken = wanted;
                    frame = Answer(frame, choice, picked, context);
                }

                Assert.That(taken.IsDefined, Is.True, "The second bullet took a card.");
                return taken;
            }

            /// <summary>
            /// One answer. The card state a taken card moves through is written on the instance itself
            /// by <c>CardLifecycleAuthority</c>, which is why this fixture reads
            /// <see cref="CardLocationOf"/> straight off its own list afterwards.
            /// </summary>
            private PendingResolution Answer(
                PendingResolution frame,
                PendingEffectChoice choice,
                EffectChoiceCandidate candidate,
                EffectResolutionContext context)
            {
                var result = EffectResolutionSession.Answer(
                    frame,
                    new EffectChoiceCommand(
                        choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Select,
                        candidate, choice.ChooserId),
                    context);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                return result.Frame;
            }

            private PendingResolution Confirm(
                PendingResolution frame,
                PendingEffectChoice choice,
                EffectResolutionContext context)
            {
                var result = EffectResolutionSession.Answer(
                    frame,
                    new EffectChoiceCommand(
                        choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Confirm,
                        default, choice.ChooserId),
                    context);
                Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
                return result.Frame;
            }

            private RuleModificationSet Modifications() => RuleModificationQuery.Build(
                timing,
                Board(),
                participants,
                new TieBreakerState(TeamSide.Red),
                UltimateUnlockQuery.Derive(MinCardContent.DealtDefinitions, instances),
                RuleObject.None,
                heroItems,
                null);

            private AdjacentMoveAuthority Board() => new AdjacentMoveAuthority(
                Map(),
                new[] { new HeroOccupancy(Min, MinAt), new HeroOccupancy(Enemy, TargetAt) },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            private CardDefinitionId DefinitionOf(CardInstanceId cardId) => instances
                .Single(card => card.Id.Equals(cardId))
                .DefinitionReference.DefinitionId;

            private static CardInstanceId Instance(CardDefinitionId cardId) =>
                new CardInstanceId("min-item-" + cardId.Value);
        }

        /// <summary>
        /// One round, one Smoke bomb, and two of her faces reading it in turn: Ruse's defence text and
        /// then Viper Stance's rider, on one board.
        /// </summary>
        private sealed class TokenRound
        {
            private readonly MatchParticipants participants =
                StagedBoardParticipants.CreateWithBlueAlly();
            private readonly EffectCapabilityCatalog capabilities = Catalog();
            private readonly AdjacentMoveAuthority board;
            private readonly CardInstance[] cards;
            private readonly CardDefinition[] definitions;
            private EffectResolutionContext context;
            private EffectResolutionStepResult step;

            internal TokenRound()
            {
                definitions = MinCardContent.DealtDefinitions.ToArray();
                cards = new[]
                {
                    CardInstance.CreateInHand(
                        Instance(MinCardContent.Ruse),
                        MinCardContent.Definition(MinCardContent.Ruse).Reference, Min),
                    CardInstance.CreateInHand(
                        Instance(MinCardContent.ViperStance),
                        MinCardContent.Definition(MinCardContent.ViperStance).Reference, Min)
                };

                board = new AdjacentMoveAuthority(
                    Map(),
                    new[]
                    {
                        new HeroOccupancy(Min, MinAt),
                        new HeroOccupancy(Enemy, TargetAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("min-printed-set-bomb"),
                    MinComponentContent.SmokeBombDefinition,
                    Min, Min, Instance(MinCardContent.SmokeBomb), BombStartedAt));
            }

            /// <summary>Two spaces North-West of her, inside Ruse's printed Range 4.</summary>
            internal HexCoordinate BombStartedAt { get; } = new HexCoordinate(-2, 0);

            internal HexCoordinate BombSpace => board.PlacedTokens.Single().Coordinate;

            internal HexCoordinate MinSpace =>
                board.Occupancies.Single(value => value.HeroId.Equals(Min)).Coordinate;

            internal string PendingNodeId => step.Frame?.PendingChoice?.NodeId.Value;

            /// <summary>
            /// Ruse's Defense text, at p.14's step 4: the mandatory swap with her bomb, and then the
            /// re-place its third sentence prints. Returns the space the bomb ended on.
            /// </summary>
            internal HexCoordinate DefendWithRuse()
            {
                Start(
                    MinCardContent.Ruse,
                    MinCardContent.PrimaryBlock,
                    MinCardContent.Definition(MinCardContent.Ruse).ActionDefinitions
                        .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .EffectRoot,
                    ActionKind.Defense,
                    enclosing: default);

                var guard = 0;
                while (step.Frame != null && step.Frame.IsAwaitingChoice)
                {
                    Assert.That(++guard, Is.LessThan(8), "Her Block settles in a few answers.");
                    var choice = step.Frame.PendingChoice;

                    // The re-place is a "you may", so its own question is an option to take rather
                    // than a candidate to pick; the destination that follows is a coordinate, and the
                    // one this case wants is a space she is not standing on.
                    var destination = choice.SelectInvocations
                        .FirstOrDefault(invocation =>
                            invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                            !invocation.Candidate.Value.Coordinate.Equals(MinSpace));
                    Answer(
                        destination ??
                        (choice.SelectInvocations.Count > 0
                            ? choice.SelectInvocations[0]
                            : choice.ConfirmInvocation));
                }

                return BombSpace;
            }

            /// <summary>Viper Stance's "After the attack:" body, inside a staged Attack.</summary>
            internal void RunVipersRider()
            {
                Start(
                    MinCardContent.ViperStance,
                    MinCardContent.PrimaryAttack,
                    MinEffectContent.StanceTextFor(
                        MinCardContent.ViperStance, MinCardContent.ContentRevision),
                    ActionKind.Attack,
                    new EnclosingAttackFacts(
                        Min, Enemy, default, AttackRangeKind.NonRanged, TargetAt, MinSpace, default,
                        false));
                Assert.That(step.Frame, Is.Not.Null, "The optional swap is offered.");
            }

            /// <summary>Take the optional swap, and whatever it asks after that.</summary>
            internal void TakeTheSwap()
            {
                var guard = 0;
                while (step.Frame != null && step.Frame.IsAwaitingChoice)
                {
                    Assert.That(++guard, Is.LessThan(6), "Her rider settles in a few answers.");
                    var choice = step.Frame.PendingChoice;
                    Answer(
                        choice.SelectInvocations.Count > 0
                            ? choice.SelectInvocations[0]
                            : choice.ConfirmInvocation);
                }
            }

            private void Start(
                CardDefinitionId cardId,
                ActionDefinitionId actionId,
                EffectDefinition effect,
                ActionKind kind,
                EnclosingAttackFacts enclosing)
            {
                context = new EffectResolutionContext(
                    effect,
                    capabilities,
                    participants,
                    null,
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(definitions, cards),
                    kind,
                    components: new MatchComponents(
                        Components(), board, new MarkerAttachmentRegister(), board, board, board,
                        board),
                    enclosingAttack: enclosing);
                step = EffectResolutionSession.Start(
                    new EffectResolutionId("min-printed-set-" + cardId.Value),
                    Instance(cardId),
                    MinCardContent.Definition(cardId).Reference,
                    actionId,
                    Min,
                    Min,
                    ActionInvocationContext.PrintedPrimary,
                    context);
                Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            }

            private void Answer(EffectChoiceCommand command)
            {
                step = EffectResolutionSession.Answer(step.Frame, command, context);
                Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            }

            private static CardInstanceId Instance(CardDefinitionId cardId) =>
                new CardInstanceId("min-token-round-" + cardId.Value);
        }
    }
}
