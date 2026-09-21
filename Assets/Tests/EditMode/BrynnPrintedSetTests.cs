using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
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
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.69f. Brynn the Seeker's printed set as a SET: nineteen faces, the five p.6 deals
    /// into his hand, the thirteen its deck keeps, the one p.6 returns to the box, the three coloured
    /// ladders p.15 climbs, the twelve items p.16 earns from the faces he does not choose, and the one
    /// revision every definition and every body carries.
    ///
    /// <para><b>What this fixture is for, and what it leaves to the five block fixtures.</b> 8.69a
    /// through 8.69e each proved one group of clauses at the effect frame -
    /// <c>BrynnObstacleAdjacencyTests</c>, <c>BrynnSplitAttackTests</c>,
    /// <c>BrynnDecoyAndGuidesTests</c>, <c>BrynnTrackingSwapTests</c> and
    /// <c>BrynnTrapAndGritTests</c> - and nothing here repeats one of those bodies. What none of them
    /// could say is anything about the SET, because until this milestone there was no set: no inventory
    /// order, no starting hand, no deck, no completed ladders, no boxed Handicap and no
    /// <c>DealtDefinitions</c>. Nor could they reach the behaviours that need TWO blocks' faces at
    /// once, which is the other half of this file: his Ultimate widening a trap's second bullet, a
    /// swap's radius arm and a guide's presence on ONE board at ONE instant; True Grit's attack-card
    /// filter reading his whole set; and Peak Precision coming back into his hand as a card he can
    /// play again.</para>
    ///
    /// <para><b>Nothing of his is registered yet, and that is asserted rather than assumed.</b>
    /// Milestone 7.9's gate holds his content out of <see cref="MatchContentCatalogs"/> until 8.69g
    /// deals him, so the catalog these cases validate and drive against is the build's registration
    /// list PLUS exactly the declarations his series deliberately withholds - 8.57f's precedent, which
    /// is what makes eighteen dealt faces sayable at a composition milestone at all. The gate is
    /// asserted from the other side too, by naming the exact set of capability references this build
    /// refuses and the exact faces that name one.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class BrynnPrintedSetTests
    {
        private static readonly HeroId Brynn = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MapId Map = new MapId("brynn-printed-set");

        private static readonly HexCoordinate BrynnAt = new HexCoordinate(0, 0);

        /// <summary>One space East of him - inside p.19's ring, so the swap's first reach holds it.</summary>
        private static readonly HexCoordinate Adjacent = new HexCoordinate(1, 0);

        /// <summary>
        /// Three spaces East - outside the ring and inside every printed Radius his set uses, which is
        /// what makes the Ultimate's widening visible rather than a repetition of the ring.
        /// </summary>
        private static readonly HexCoordinate InTheDisk = new HexCoordinate(3, 0);

        // -------------------------------------------------------------------------------------------
        // The printed inventory
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One row per face, in his shard's own table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item.
        ///
        /// <para>A dash is what the face prints nothing for, which is BR-06 written out nineteen times:
        /// a missing icon is absent, never zero, and an absent secondary is an action that does not
        /// exist rather than one whose value is 0. His Silver prints no Movement; High Ground, Elevated
        /// Ambush and Peak Precision print neither Range nor Radius; the Ultimate prints nothing at
        /// all.</para>
        ///
        /// <para>It is deliberately a literal table rather than something derived, for the reason
        /// <c>WaspVerifiedContentTests</c> and every set after it record: a test that derived its
        /// expectations from the definitions would pass whatever the definitions happened to say. This
        /// one fails if a single number, colour, Tier, icon or name drifts from
        /// <c>Docs/AI/Rules/HERO_BRYNN.md</c>, which is the document the extracted faces were
        /// transcribed into - and the case after it reads that document back independently, so the two
        /// transcriptions have to agree with each other as well as with the content.</para>
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "brynn-familiar-ground|Familiar Ground|Init 12|SecDef 2|SecMov 1|primary-ranged-attack Attack Ranged [Attack 2, Range 3]|Gold Basic|item -",
            "brynn-decoy|Decoy|Init 1|SecDef 2|SecMov -|primary-basic-skill Skill None [Radius 4]|Silver Basic|item -",
            "brynn-high-ground|High Ground|Init 9|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 4]|Red I|item -",
            "brynn-elevated-ambush|Elevated Ambush|Init 9|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 5]|Red II|item Initiative",
            "brynn-split-attack|Split Attack|Init 9|SecDef 3|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red II|item Defense",
            "brynn-peak-precision|Peak Precision|Init 10|SecDef 7|SecMov 4|primary-attack Attack NonRanged [Attack 5]|Red III|item Radius",
            "brynn-split-throw|Split Throw|Init 10|SecDef 4|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red III|item Defense",
            "brynn-tread-lightly|Tread Lightly|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 2]|Blue I|item -",
            "brynn-cover-tracks|Cover Tracks|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Defense",
            "brynn-mountain-guide|Mountain Guide|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 4]|Blue II|item Attack",
            "brynn-hide-traces|Hide Traces|Init 11|SecDef 6|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Range",
            "brynn-expedition-leader|Expedition Leader|Init 11|SecDef 6|SecMov 3|primary-skill Skill None [Radius 4]|Blue III|item Attack",
            "brynn-bear-trap|Bear Trap|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green I|item -",
            "brynn-log-trap|Log Trap|Init 2|SecDef 3|SecMov 2|primary-skill Skill None [Radius 4]|Green II|item Initiative",
            "brynn-true-grit|True Grit|Init 2|SecDef 3|SecMov 2|primary-skill Skill None [Radius 4]|Green II|item Attack",
            "brynn-deadfall-trap|Deadfall Trap|Init 2|SecDef 4|SecMov 2|primary-skill Skill None [Radius 4]|Green III|item Initiative",
            "brynn-die-hard|Die Hard|Init 2|SecDef 4|SecMov 2|primary-skill Skill None [Radius 4]|Green III|item Movement",
            "brynn-over-the-top|Over the Top|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -",
            "brynn-home-turf|Home Turf|Init 11|SecDef 1|SecMov 1|primary-ranged-attack Attack Ranged [Attack 2, Range 2]|Gold Basic|item -"
        };

        /// <summary>
        /// Every capability reference his composed bodies name that this build does NOT register, as the
        /// build spells them - milestone 7.9's gate, written out.
        ///
        /// <para>8.69g registers every one of them, and all THREE are new handler TYPES, so each
        /// additionally owes a showcase consumer - the gate's second half. The case below asserts this
        /// list as a SET, so a registration that arrives early and a deferral nobody noticed both fail
        /// here.</para>
        ///
        /// <para><b>A fourth declaration of his series is deliberately NOT here, because no composed
        /// sentence reaches it</b> - 8.69a's <c>brynn-hero-adjacent-to-obstacles@1</c>, minted for the
        /// split faces' gate and superseded by 8.69b's, which asks the question of the ATTACK rather
        /// than of a hero. A capability nothing can start is a registration this milestone DROPPED
        /// rather than one 8.69g makes reachable by nothing, and the declaration is gone from the
        /// tree.</para>
        /// </summary>
        private static readonly string[] WithheldUntilTheDealing =
        {
            "brynn-attack-target-was-adjacent-to-obstacles@1",
            "brynn-enemy-hero-in-radius-adjacent-to-obstacles@1",
            "brynn-tracking-swap-subject@1"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                BrynnCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.69 verified against the extracted faces. Correct the definition, not this table - " +
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
            Assert.That(BrynnCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(BrynnCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                BrynnCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { BrynnCardContent.HomeTurf }));
            Assert.That(
                BrynnCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(BrynnCardContent.HomeTurf),
                "and the dealt eighteen are the nineteen minus exactly that one.");

            var byColour = BrynnCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2),
                "Familiar Ground and the boxed Home Turf.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1), "Decoy.");
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5),
                "Two raised attacks, two splits and Peak Precision.");
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5), "Three swaps and two guides.");
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5), "Three traps and two grit faces.");
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "Over the Top.");

            var ultimate = BrynnCardContent.Definition(BrynnCardContent.OverTheTop);
            Assert.That(ultimate.Progression.Classification, Is.EqualTo(CardClassification.Ultimate));
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "p.16's permanent ability prints no action at all, so he cannot even discard it to " +
                "defend - which is the absence BR-06 protects, stated for a whole card.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);
        }

        /// <summary>
        /// p.6's five starting cards and the thirteen the deck keeps partition his dealt eighteen
        /// exactly, so no card is both and none is neither - and the five are one of each colour, which
        /// is the shape p.6 states rather than a fact about him in particular.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHisDealtEighteen()
        {
            Assert.That(BrynnCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(BrynnCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(BrynnCardContent.StartingHand.Intersect(BrynnCardContent.Deck), Is.Empty);
            Assert.That(
                BrynnCardContent.StartingHand.Concat(BrynnCardContent.Deck),
                Is.EquivalentTo(BrynnCardContent.DealtDefinitions.Select(card => card.Id)));

            Assert.That(BrynnCardContent.StartingHand, Is.EqualTo(new[]
            {
                BrynnCardContent.FamiliarGround,
                BrynnCardContent.Decoy,
                BrynnCardContent.HighGround,
                BrynnCardContent.TreadLightly,
                BrynnCardContent.BearTrap
            }));

            Assert.That(
                BrynnCardContent.StartingHand
                    .Select(id => BrynnCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.6 deals one of each colour, and his are the Gold, the Silver and the three Tier I.");

            Assert.That(
                BrynnCardContent.Deck
                    .Select(BrynnCardContent.Definition)
                    .Count(definition => definition.Progression.IsUltimate),
                Is.EqualTo(1),
                "p.16's card starts in the deck and is reached by the unlock rather than by a draw.");
            Assert.That(
                BrynnCardContent.Deck
                    .Select(BrynnCardContent.Definition)
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
        /// One seat's whole setup, built the way a configured match will build it at 8.69g: eighteen
        /// instances, five of them in hand and thirteen in the deck, with the boxed Handicap nowhere at
        /// all.
        /// </summary>
        [Test]
        public void HisDealtHand_IsFiveInHandAndThirteenInTheDeck_AndNeverTheHandicap()
        {
            var hand = BrynnCardContent.CreateHand(Brynn, "brynn-printed-set");

            Assert.That(hand.AllInstances.Count, Is.EqualTo(18));
            Assert.That(
                hand.AllInstances.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(BrynnCardContent.HomeTurf));

            var cards = BrynnCardContent.CreateDealtCards(Brynn, "brynn-printed-set");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(BrynnCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(BrynnCardContent.Deck));
            Assert.That(cards.Select(card => card.Id.Value).Distinct().Count(), Is.EqualTo(18),
                "and each instance identity is distinct, which is what the per-seat prefix is for.");
            Assert.That(cards.All(card => card.OwnerId.Equals(Brynn)), Is.True);

            Assert.That(
                () => BrynnCardContent.CreateDealtCards(default, "brynn-printed-set"),
                Throws.ArgumentException,
                "A card without an owner is not a card p.6 dealt to anybody.");
            Assert.That(
                () => BrynnCardContent.CreateDealtCards(Brynn, "  "),
                Throws.ArgumentException,
                "and the per-seat prefix is the caller's to name, so an empty one is refused rather " +
                "than silently shared between two seats.");
        }

        // -------------------------------------------------------------------------------------------
        // The upgrade ladders and the items
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.15's three ladders, each forking twice. Four of the six pairs cross a block boundary -
        /// High Ground and Elevated Ambush name a split face, Tread Lightly and Cover Tracks name a
        /// guide - which is exactly why they could not be stated until the set was composed.
        /// </summary>
        [Test]
        public void EveryUpgradePair_IsThePrintedLadder()
        {
            AssertSuccessors(
                BrynnCardContent.HighGround,
                BrynnCardContent.ElevatedAmbush, BrynnCardContent.SplitAttack);
            AssertSuccessors(
                BrynnCardContent.ElevatedAmbush,
                BrynnCardContent.PeakPrecision, BrynnCardContent.SplitThrow);
            AssertSuccessors(
                BrynnCardContent.SplitAttack,
                BrynnCardContent.PeakPrecision, BrynnCardContent.SplitThrow);

            AssertSuccessors(
                BrynnCardContent.TreadLightly,
                BrynnCardContent.CoverTracks, BrynnCardContent.MountainGuide);
            AssertSuccessors(
                BrynnCardContent.CoverTracks,
                BrynnCardContent.HideTraces, BrynnCardContent.ExpeditionLeader);
            AssertSuccessors(
                BrynnCardContent.MountainGuide,
                BrynnCardContent.HideTraces, BrynnCardContent.ExpeditionLeader);

            AssertSuccessors(
                BrynnCardContent.BearTrap,
                BrynnCardContent.LogTrap, BrynnCardContent.TrueGrit);
            AssertSuccessors(
                BrynnCardContent.LogTrap,
                BrynnCardContent.DeadfallTrap, BrynnCardContent.DieHard);
            AssertSuccessors(
                BrynnCardContent.TrueGrit,
                BrynnCardContent.DeadfallTrap, BrynnCardContent.DieHard);

            foreach (var tierThree in BrynnCardContent.AllDefinitions
                .Where(definition => definition.Progression.Tier == CardTier.III))
            {
                AssertSuccessors(tierThree.Id);
            }

            // The four faces that sit on no ladder at all, and for three different printed reasons:
            // p.6 deals his Gold and Silver basics and p.15 never upgrades a basic, p.20 keeps the
            // Handicap in the box, and p.16 UNLOCKS the Ultimate rather than upgrading into it.
            foreach (var offLadder in new[]
            {
                BrynnCardContent.FamiliarGround,
                BrynnCardContent.Decoy,
                BrynnCardContent.OverTheTop,
                BrynnCardContent.HomeTurf
            })
            {
                AssertSuccessors(offLadder);
                Assert.That(
                    BrynnCardContent.AllDefinitions.SelectMany(definition =>
                        definition.Progression.NextTierCandidateIds),
                    Has.No.Member(offLadder),
                    offLadder.Value + " is named by no upgrade pair either.");
            }
        }

        /// <summary>
        /// p.16's items, read off the faces he does not choose: every one of the twelve Tier II and
        /// Tier III faces prints one, every Tier I, basic and the Ultimate print none, and the twelve
        /// are the six channels in the counts his shard's table records - three Initiative, three
        /// Defense, three Attack, and one each of Radius, Range and Movement.
        /// </summary>
        [Test]
        public void EveryUnchosenFace_BecomesItsPrintedItem()
        {
            foreach (var definition in BrynnCardContent.AllDefinitions)
            {
                var printsAnItem = definition.Progression.ItemIcon != PrintedValueChannel.None;
                var isUpgradeTier =
                    definition.Progression.Tier == CardTier.II ||
                    definition.Progression.Tier == CardTier.III;

                Assert.That(printsAnItem, Is.EqualTo(isUpgradeTier), definition.Id.Value +
                    ": an item is what the face he did NOT choose becomes, so exactly the twelve " +
                    "faces a level-up can leave behind print one.");
            }

            var byChannel = BrynnCardContent.AllDefinitions
                .Select(definition => definition.Progression.ItemIcon)
                .Where(channel => channel != PrintedValueChannel.None)
                .GroupBy(channel => channel)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byChannel[PrintedValueChannel.Initiative], Is.EqualTo(3),
                "Elevated Ambush, Log Trap and Deadfall Trap.");
            Assert.That(byChannel[PrintedValueChannel.Defense], Is.EqualTo(3),
                "Split Attack, Split Throw and Cover Tracks.");
            Assert.That(byChannel[PrintedValueChannel.Attack], Is.EqualTo(3),
                "Mountain Guide, Expedition Leader and True Grit.");
            Assert.That(byChannel[PrintedValueChannel.Radius], Is.EqualTo(1), "Peak Precision.");
            Assert.That(byChannel[PrintedValueChannel.Range], Is.EqualTo(1), "Hide Traces.");
            Assert.That(byChannel[PrintedValueChannel.Movement], Is.EqualTo(1), "Die Hard.");
            Assert.That(byChannel.Values.Sum(), Is.EqualTo(12));
        }

        /// <summary>
        /// BR-06 as a claim about the whole set: a missing icon is an action or a printed value that
        /// does not exist, never a zero. The inventory table above says it row by row; this says it
        /// where a reader of the shard would look for it, by the groups the shard names.
        /// </summary>
        [Test]
        public void AbsentIcons_StayAbsentRatherThanBecomingZero()
        {
            Assert.That(
                BrynnCardContent.Definition(BrynnCardContent.Decoy).ActionDefinitions
                    .Any(action => action.Kind == ActionKind.Movement),
                Is.False,
                "His Silver prints Defense 2 and no Movement icon at all, so there is no Movement " +
                "action on it rather than one printing zero.");

            foreach (var cardId in new[]
            {
                BrynnCardContent.HighGround,
                BrynnCardContent.ElevatedAmbush,
                BrynnCardContent.PeakPrecision
            })
            {
                Assert.That(
                    PrintedChannels(cardId),
                    Is.EquivalentTo(new[] { PrintedValueChannel.Attack }),
                    cardId.Value + " prints an Attack value and neither a Range nor a Radius icon, " +
                    "which is why its \"adjacent to you\" is p.13's word rather than a number p.16 " +
                    "raises - and why Peak Precision's Radius ITEM is a fact about somebody else's " +
                    "dashboard rather than about this action.");
                Assert.That(
                    BrynnCardContent.Definition(cardId).ActionDefinitions
                        .Single(action => action.Kind == ActionKind.Attack).AttackRange,
                    Is.EqualTo(AttackRangeKind.NonRanged));
            }

            var ultimate = BrynnCardContent.Definition(BrynnCardContent.OverTheTop);
            Assert.That(ultimate.Initiative.IsDefined, Is.False,
                "and p.16's card prints no Initiative either, which is an undefined value rather " +
                "than a zero.");
            Assert.That(ultimate.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.None));
        }

        /// <summary>
        /// The printed "+" beside a conditional bonus - "A 4+" on High Ground and "A 5+" on Elevated
        /// Ambush and Peak Precision - is ONE value and ONE contribution rather than two numbers. p.14
        /// says the notation outright, and BR-Q6 is the reading of when it is read.
        ///
        /// <para>The three faces share <c>BrynnCardContent.RaisedAttack()</c>, which is what makes the
        /// claim a set-level one: 8.69a composed two of them and 8.69b the third, on different
        /// branches, and a second copy of the contribution would have let the two halves drift.</para>
        /// </summary>
        [Test]
        public void ThePrintedPlus_IsOneContributionOnExactlyTheThreeFacesThatPrintOne()
        {
            var raised = new Dictionary<CardDefinitionId, int>
            {
                [BrynnCardContent.HighGround] = 4,
                [BrynnCardContent.ElevatedAmbush] = 5,
                [BrynnCardContent.PeakPrecision] = 5
            };

            foreach (var definition in BrynnCardContent.AllDefinitions)
            {
                var attack = definition.ActionDefinitions
                    .FirstOrDefault(action => action.Kind == ActionKind.Attack);
                var contributions = attack == null
                    ? Array.Empty<RosterValueContributionDefinition>()
                    : attack.RosterValueContributions.ToArray();

                if (!raised.TryGetValue(definition.Id, out var printedAttack))
                {
                    Assert.That(contributions, Is.Empty,
                        definition.Id.Value + " prints no \"+\", so it declares no contribution - " +
                        "which is why neither split face and no Skill of his has a +2 of its own.");
                    continue;
                }

                Assert.That(
                    attack.BaseValues.Single(value =>
                        value.Channel == PrintedValueChannel.Attack).Amount,
                    Is.EqualTo(printedAttack),
                    definition.Id.Value + " prints the number beside the plus, and the plus is the " +
                    "contribution rather than a second number.");
                Assert.That(contributions, Has.Length.EqualTo(1));
                Assert.That(contributions[0].Channel, Is.EqualTo(PrintedValueChannel.Attack));
                Assert.That(contributions[0].AmountPerMatch, Is.EqualTo(2));
                Assert.That(
                    contributions[0].Query,
                    Is.EqualTo(RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles),
                    "and all three read the one query 8.69a minted, so his Ultimate widens them " +
                    "together or not at all.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The source corpus
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The source sweep: every composed face maps to exactly one <c>brynn*__front.png</c> stem in
        /// his shard's inventory table, and no stem is left over.
        ///
        /// <para>The table is read back out of <c>Docs/AI/Rules/HERO_BRYNN.md</c> rather than
        /// transcribed a second time here, which is what makes this case independent of the literal
        /// table above: that one is this fixture's own transcription of the extracted faces and this
        /// one is the shard's, so a drift in either is a disagreement rather than two copies of one
        /// mistake.</para>
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
                    row.Stem.StartsWith("brynn", StringComparison.Ordinal) && row.Stem.Contains("__"),
                    Is.True,
                    row.Stem + " is not a stem under his source directory.");
            }

            Assert.That(
                rows.Select(row => row.Name).OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(BrynnCardContent.AllDefinitions
                    .Select(definition => definition.DisplayName)
                    .OrderBy(name => name, StringComparer.Ordinal)),
                "and the nineteen named faces are exactly the nineteen composed ones - a bijection " +
                "rather than a containment, so an extracted face nobody composed fails here too.");

            Assert.That(
                rows.Select(row => row.Name),
                Is.EqualTo(BrynnCardContent.AllDefinitions.Select(definition => definition.DisplayName)),
                "and in the same ORDER, which is what BrynnCardContent.InventoryOrder claims to be.");

            foreach (var row in rows)
            {
                var definition = BrynnCardContent.AllDefinitions
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
        /// <para>His shard writes one bullet for High Ground and Elevated Ambush together, and one for
        /// Bear Trap and Log Trap, because two cards printing one sentence at two sets of numbers are
        /// one bullet; it names the Ultimate's and the Handicap's role in parentheses, which is
        /// stripped.</para>
        ///
        /// <para><b>One notation is normalized, loudly and in one place.</b> His shard writes the
        /// bullets of a p.10 modal as <c>(1)</c> and <c>(2)</c> where the card prints dots and where
        /// every other hero's shard writes <c>•</c> - Bain's, Brogan's, Cutter's and Dodger's tables all
        /// do, and Min's Perfect Self is the one earlier bullet written the other way. The content
        /// transcribes the dots, which is what the corpus image shows, so the two spellings are mapped
        /// onto each other here rather than one of them being bent: the comparison is then about the
        /// WORDS, which is what a transcription check is for. Five faces go through it - Familiar
        /// Ground, Decoy, Bear Trap, Log Trap, Deadfall Trap and Home Turf - and nothing else in the
        /// sentence is touched.</para>
        /// </summary>
        [Test]
        public void EveryPrintedRule_IsTranscribedFromTheShard()
        {
            var bullets = PrintedRuleBullets();
            Assert.That(bullets.Count, Is.EqualTo(19),
                "Every one of the nineteen faces has a bullet of its own in the shard's rules list.");

            foreach (var definition in BrynnCardContent.AllDefinitions)
            {
                Assert.That(bullets.ContainsKey(definition.DisplayName), Is.True,
                    definition.DisplayName + " has no printed-rule bullet in the shard.");
                Assert.That(
                    definition.SourceContent.PrintedText,
                    Is.EqualTo(bullets[definition.DisplayName]),
                    definition.Id.Value + "'s transcription differs from the shard's.");
            }

            Assert.That(
                bullets.Count(pair => pair.Value.Contains("•")), Is.EqualTo(6),
                "and exactly six of his faces print p.10's bullets - the two Gold modals, Decoy and " +
                "the three traps - which is what the notation mapping above covers and all it covers.");
        }

        // -------------------------------------------------------------------------------------------
        // Legality, the deferral, and what this build refuses
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition,
        /// asked at the rulebook p.14 or p.11 moment the face itself prints - the check that would
        /// otherwise fault mid-resolution, on a player's turn, with the card already spent.
        ///
        /// <para>Fifteen of the nineteen attach a body. The four that do not are High Ground and
        /// Elevated Ambush, whose whole text is p.13's own reading of the band plus a value
        /// contribution; p.16's Ultimate, whose printed content is a standing rule declaration; and
        /// p.20's Handicap, whose sentence stands under the established deferral.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogHisDealingWillRegister()
        {
            var catalog = Catalog();
            var composed = 0;

            foreach (var definition in BrynnCardContent.AllDefinitions)
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

            Assert.That(composed, Is.EqualTo(15),
                "Fifteen bodies: his Gold basic, his Silver basic, the three split Reds, the three " +
                "swaps, the two guides, the three traps and the two grit faces.");

            var rule = BrynnCardContent.Definition(BrynnCardContent.OverTheTop).UltimateRule;
            Assert.That(rule, Is.Not.Null);
            Assert.That(rule.TriggeredEffect, Is.Null,
                "and his Ultimate names a modification family rather than an effect, which is why he " +
                "has no triggered-effect list at all: the whole of Over the Top is a standing " +
                "declaration a rule query derives.");
            Assert.That(rule.Modifications, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// EXACTLY these capability references his composed bodies name are still withheld, and EXACTLY
        /// these faces name one - milestone 7.9's gate read from both sides, so that a registration
        /// arriving early, a deferral arriving unnoticed and a face that quietly stopped asking for a
        /// contract each fail here rather than at his dealing.
        /// </summary>
        [Test]
        public void ExactlyTheseContractsAreWithheld_AndExactlyTheseFacesNameThem()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            var named = BrynnCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();

            Assert.That(
                named.Where(capability => !registered.IsRegistered(capability))
                    .Select(capability => capability.ToString())
                    .Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(WithheldUntilTheDealing.OrderBy(value => value, StringComparer.Ordinal)),
                "The set of contracts this build refuses to start for him is exactly the series' own " +
                "deferral list. 8.69g registers every one of them.");

            Assert.That(
                named.Count(capability => registered.IsRegistered(capability)),
                Is.GreaterThan(0),
                "and his bodies genuinely reach registered contracts too - the modal Attack, the " +
                "direct move, the swap, the discard, the Defeat and the retrieval are the build's " +
                "own - so the case above is not passing because the walk found nothing.");

            Assert.That(
                BrynnComponentContent.DeferredHandlers()
                    .Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(WithheldUntilTheDealing.OrderBy(value => value, StringComparer.Ordinal)),
                "and the declaration list and the walk over his faces agree, which is what says the " +
                "dropped fourth declaration is dropped from BOTH sides rather than just unreferenced.");

            var refusedFaces = new List<string>();
            foreach (var definition in BrynnCardContent.AllDefinitions)
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
                Is.EqualTo(new[]
                {
                    "brynn-cover-tracks",
                    "brynn-die-hard",
                    "brynn-expedition-leader",
                    "brynn-hide-traces",
                    "brynn-mountain-guide",
                    "brynn-peak-precision",
                    "brynn-split-attack",
                    "brynn-split-throw",
                    "brynn-tread-lightly",
                    "brynn-true-grit"
                }),
                "TEN of his fifteen composed bodies are refused by what this build registers, and the " +
                "five that are not - Familiar Ground, Decoy and the three traps - name only contracts " +
                "the build already owns, because a modal Attack, a printed-Radius selection, a direct " +
                "move, a discard and a Defeat are the roster's own vocabulary. That asymmetry is the " +
                "whole of what registration buys at 8.69g.");
        }

        /// <summary>
        /// The only deferral anywhere on his set is p.20's, on the one boxed face. No dealt face of his
        /// owes a composition to a milestone, and none carries the odd-player trigger.
        /// </summary>
        [Test]
        public void OnlyTheBoxedHandicap_DefersItsPrintedTextComposition()
        {
            foreach (var definition in BrynnCardContent.DealtDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    Assert.That(action.TextComposition.IsAwaited, Is.False,
                        definition.Id.Value + " action '" + action.Id.Value + "' must owe nothing: " +
                        "an engine deferral on a dealt face is a card a match would grey out.");
                }
            }

            var handicap = BrynnCardContent.Definition(BrynnCardContent.HomeTurf);
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

            // BR-05, stated where a reader would look for it: the card's BAND reads "Basic Skill -
            // Ranged" and its numbers are an Attack of 2 over a Range icon of 2, which is the mirror
            // of Familiar Ground's "Basic Attack - Ranged". A Skill has no Attack value to print and
            // reaches nobody through a Range icon, so the band is the printing slip and the action is
            // the Ranged Attack both the numbers and the sentence describe.
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(primary.AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
            Assert.That(primary.Id, Is.EqualTo(BrynnCardContent.PrimaryRangedAttack));
            Assert.That(
                primary.BaseValues.Select(value => (value.Channel, value.Amount)),
                Is.EquivalentTo(new[]
                {
                    (PrintedValueChannel.Attack, 2), (PrintedValueChannel.Range, 2)
                }));

            Assert.That(
                BrynnCardContent.DealtDefinitions.SelectMany(definition => definition.ActionDefinitions)
                    .Count(action => action.EffectRoot != null),
                Is.EqualTo(15),
                "and all fifteen composed bodies in his set sit on DEALT faces: the one face that is " +
                "not dealt is one of the four that compose nothing.");
        }

        /// <summary>
        /// One revision string covers the whole set - every definition and every body reached from one.
        /// Milestone 8.69f minted it; the five composition blocks carried five deliberately malformed
        /// placeholders between them, and no face or body may be left on one.
        /// </summary>
        [Test]
        public void OneRealRevision_CoversEveryDefinitionAndEveryBody()
        {
            Assert.That(BrynnCardContent.ContentRevision, Is.EqualTo("brynn-8-69f-rev-1"));
            Assert.That(BrynnCardContent.ContentSetId, Is.EqualTo("brynn"));

            Assert.That(
                BrynnCardContent.AllDefinitions
                    .Select(definition => definition.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { BrynnCardContent.ContentRevision }));

            Assert.That(
                BrynnCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .Select(action => action.EffectRoot.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { BrynnCardContent.ContentRevision }),
                "and the bodies carry it too, which is the half a card-only check would miss.");

            Assert.That(
                BrynnCardContent.AllDefinitions.All(definition =>
                    definition.Id.Value.StartsWith("brynn-", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                BrynnCardContent.AllDefinitions
                    .Select(definition => definition.ContentAuthority).Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
        }

        /// <summary>
        /// Which of rulebook p.14's moments each face's rider is applied at, as a set. His two modal
        /// Attacks and both split faces carry step 1's, because choosing a bullet or reading the
        /// condition is how the target is bound at all; Peak Precision carries step 7's, which is where
        /// its "After the attack:" retrieval lives; and no Skill of his declares a moment, because a
        /// Skill's text IS its action.
        /// </summary>
        [Test]
        public void EveryFacesPrintedMoment_IsTheOneItsOwnWordsName()
        {
            Assert.That(
                BrynnCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions
                        .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .Select(action => definition.DisplayName + " " + action.CardTextTiming))
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Familiar Ground BeforeTheAttack",
                    "Decoy None",
                    "High Ground None",
                    "Elevated Ambush None",
                    "Split Attack BeforeTheAttack",
                    "Peak Precision AfterTheAttack",
                    "Split Throw BeforeTheAttack",
                    "Tread Lightly None",
                    "Cover Tracks None",
                    "Mountain Guide None",
                    "Hide Traces None",
                    "Expedition Leader None",
                    "Bear Trap None",
                    "Log Trap None",
                    "True Grit None",
                    "Deadfall Trap None",
                    "Die Hard None",
                    "Home Turf None"
                }),
                "Eighteen primaries - the Ultimate prints none - each at the moment its own sentence " +
                "names, with the boxed Handicap's uncomposed and therefore at none.");

            Assert.That(
                BrynnCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Any(action => action.SecondCardTextRoot != null),
                Is.False,
                "and no face of his prints a SECOND rider: Peak Precision's +2 and its retrieval are " +
                "a value and one body rather than two bodies at two moments.");
        }

        /// <summary>
        /// The set's own aggregates, which are what 8.69g moves across in one change: his three
        /// contracts in one list, nothing registered, no token and no triggered effect at all, and the
        /// decision labels his faces owe measured against the controls they actually raise.
        /// </summary>
        [Test]
        public void HisContractsAndDecisionLabels_AreOneListEach()
        {
            Assert.That(BrynnComponentContent.RegisteredHandlers(), Is.Empty,
                "Milestone 7.9's gate holds until 8.69g deals him.");
            Assert.That(
                BrynnComponentContent.Handlers().Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(BrynnComponentContent.DeferredHandlers()
                    .Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "so the registered-or-deferred list is the deferred one, exactly.");
            Assert.That(
                BrynnComponentContent.Handlers().Select(handler => handler.Reference).Distinct()
                    .Count(),
                Is.EqualTo(3),
                "Three handler types, one per block that minted one; 8.69c and 8.69e minted none.");

            var declarations = BrynnEffectContent.DecisionLabels().ToArray();
            Assert.That(declarations, Has.Length.EqualTo(19),
                "Decoy's three performances (an optional step and two modal arms apiece), Familiar " +
                "Ground's two p.10 bullets, the two bullets each of the three traps prints, Split " +
                "Attack's printed \"may repeat\" and Peak Precision's \"you may retrieve\".");
            Assert.That(
                declarations.Select(declaration => declaration.CardId.Value).Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    BrynnCardContent.BearTrap.Value,
                    BrynnCardContent.DeadfallTrap.Value,
                    BrynnCardContent.Decoy.Value,
                    BrynnCardContent.FamiliarGround.Value,
                    BrynnCardContent.LogTrap.Value,
                    BrynnCardContent.PeakPrecision.Value,
                    BrynnCardContent.SplitAttack.Value
                }),
                "Seven of his eighteen dealt faces open a window that needs words; the eleven that " +
                "do not either ask for a unit, a card or a destination - whose control names what it " +
                "would pick - or never ask at all.");

            var required = DecisionLabelRequirementQuery
                .For(BrynnCardContent.DealtDefinitions, Catalogs())
                .ToArray();
            Assert.That(
                required.Select(requirement =>
                        requirement.CardId.Value + " / " + requirement.NodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(declarations.Select(declaration =>
                        declaration.CardId.Value + " / " + declaration.NodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "and the controls his DEALT faces raise are exactly the nineteen he declares words " +
                "for - derived from the resolver's own rule rather than from a second list, so " +
                "8.69g's one line into DecisionLabelCatalog cannot land a face short. It was this " +
                "derivation that found the ten 8.69c could not have known about.");

            Assert.That(
                required.Count(requirement => requirement.OpensConfirmOrDecline), Is.EqualTo(5),
                "Five of them are p.10's confirm-or-decline - Decoy's three performances, Split " +
                "Attack's printed \"may repeat\" and Peak Precision's retrieval - and each of those " +
                "five declares what declining does as well as what accepting does.");
            foreach (var requirement in required.Where(value => value.OpensConfirmOrDecline))
            {
                Assert.That(
                    declarations.Single(declaration =>
                        declaration.CardId.Equals(requirement.CardId) &&
                        declaration.NodeId.Equals(requirement.NodeId)).Label.Declined,
                    Is.Not.Null.And.Not.Empty,
                    requirement + " opens a decline and must say what declining does.");
            }

            foreach (var declaration in declarations)
            {
                Assert.That(
                    DecisionLabelCatalog.TryGetLabel(
                        declaration.CardId, declaration.NodeId, out _),
                    Is.False,
                    declaration.NodeId.Value + " must NOT be in the catalog yet: every hero's table " +
                    "joins it at the milestone that DEALS them, which for him is 8.69g.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // What only the whole set exposes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE SET'S HEADLINE MEASUREMENT. One board with no obstacle on it at all, one instant, three
        /// faces from three different blocks: Deadfall Trap's second bullet, Hide Traces' radius arm and
        /// Expedition Leader's presence all read the printed phrase, and his unlocked Ultimate widens
        /// all three at once because the overlay lives inside the one query rather than beside each of
        /// them.
        ///
        /// <para>Each block proved its own clause against its own board. What none of them could do is
        /// ask three of his faces the same question on one board, which is the only way to see that one
        /// declaration reaches a trap, a swap and a guide together.</para>
        /// </summary>
        [Test]
        public void OverTheTop_WidensATrapBullet_ASwapArm_AndAGuidesPresence_AtOnce()
        {
            var board = OpenBoard();
            var ultimate = UnlockedUltimate(board);

            Assert.That(
                TrapBulletTwo(board, RuleModificationSet.Empty), Is.Empty,
                "Nobody is adjacent to three obstacles on an empty board, so Deadfall Trap's second " +
                "bullet offers nobody at all.");
            Assert.That(
                SwapSubjects(board, RuleModificationSet.Empty),
                Is.EqualTo(new[] { Enemy.Value }),
                "and Hide Traces reaches only p.19's ring, which is the arm the printed phrase does " +
                "not narrow.");
            Assert.That(
                GuidePresence(board, RuleModificationSet.Empty), Is.False,
                "and Expedition Leader's second sentence is not reached.");

            Assert.That(
                TrapBulletTwo(board, ultimate),
                Is.EquivalentTo(new[] { Enemy.Value, SecondEnemy.Value }),
                "\"While you are performing actions all enemy heroes in play count as adjacent to 3 " +
                "or more obstacles\" - so the trap's narrowed bullet reaches every enemy hero in its " +
                "printed Radius.");
            Assert.That(
                SwapSubjects(board, ultimate),
                Is.EquivalentTo(new[] { Enemy.Value, SecondEnemy.Value }),
                "the swap's radius arm reaches the hero three spaces away who was outside the ring,");
            Assert.That(
                GuidePresence(board, ultimate), Is.True,
                "and the guide's presence holds - three faces, three blocks, one declaration none of " +
                "them names.");
        }

        /// <summary>
        /// True Grit's "a discarded attack card" reads his WHOLE set, which is the only place the
        /// filter 8.69e added can be measured against the faces 8.69a and 8.69b compose: the six faces
        /// whose printed primary is an Attack are offered and the twelve Skills are not, including
        /// Split Attack lying resolved in the discard exactly where a repeat leaves it.
        ///
        /// <para>p.9 is the whole rule - "a card with an Attack primary action is referred to as an
        /// Attack card" - and p.10 is the other half: another hero's discard is not his.</para>
        /// </summary>
        [Test]
        public void TheGritsRetrieval_OffersHisSixAttackFacesAndNoSkillOfHis()
        {
            var offered = RetrievablePool();

            Assert.That(offered, Is.EquivalentTo(new[]
            {
                BrynnCardContent.FamiliarGround.Value,
                BrynnCardContent.HighGround.Value,
                BrynnCardContent.ElevatedAmbush.Value,
                BrynnCardContent.SplitAttack.Value,
                BrynnCardContent.PeakPrecision.Value,
                BrynnCardContent.SplitThrow.Value
            }),
                "His Gold basic and his five Reds are the six faces p.9 calls Attack cards; Decoy, " +
                "the three swaps, the two guides and the five Greens print a Skill primary, and Over " +
                "the Top prints no action at all.");

            Assert.That(offered, Has.No.Member(BrynnCardContent.OverTheTop.Value));
            Assert.That(offered, Has.No.Member(BrynnCardContent.TrueGrit.Value),
                "and the card being performed is not in its own pool either, because it is in the " +
                "turn area rather than the discard.");
        }

        /// <summary>
        /// Peak Precision, driven through a whole turn against a qualifying hero, comes back into his
        /// HAND at p.14's step 7 - and what comes back is an ordinary unplayed card, which the offer
        /// query then offers again. That is the "play it again" half of BR-Q7, and it needs the set:
        /// the card is retrieved into a hand of his own eighteen rather than into a fixture's.
        /// </summary>
        [Test]
        public void PeakPrecision_RetrievedIntoHisHand_IsAnUnplayedCardTheOfferQueryOffersAgain()
        {
            var run = DrivePeakPrecision();

            Assert.That(run.AttackTotal, Is.EqualTo(7),
                "Printed 5 with the +2 the condition adds, read where p.14 computes the total.");
            Assert.That(run.RetrievalWasOffered, Is.True);
            Assert.That(run.SourceCardLocation, Is.EqualTo(CardLocation.Hand));
            Assert.That(run.SourceCardHasBeenPlayed, Is.False);
            Assert.That(run.OfferedAgain, Is.True,
                "and the offer query gives him its Attack again off the retrieved instance, in the " +
                "same window every other card in his hand is offered through: what the text changed " +
                "is where the card is, so a card back in hand is a card p.7 can commit in a later " +
                "round exactly as any other.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers - the catalog
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The bounded catalog his content runs against: everything this build registers, plus exactly
        /// the three contracts his series withholds.
        ///
        /// <para>The <c>GroupBy</c> is milestone 8.40j's BA-18 lesson: handing a registered handler
        /// back in as content is what threw "Capability references must be unique" at Bain's
        /// dealing.</para>
        /// </summary>
        private static EffectCapabilityCatalog Catalog()
        {
            var build = MatchContentCatalogs.ForThisBuild().Capabilities;
            return new EffectCapabilityCatalog(
                build.Registered.Select(build.Load)
                    .Concat(BrynnComponentContent.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
        }

        private static MatchContentCatalogs Catalogs()
        {
            var build = MatchContentCatalogs.ForThisBuild();
            return new MatchContentCatalogs(Catalog(), build.Components, build.TriggeredEffects);
        }

        // -------------------------------------------------------------------------------------------
        // Helpers - the board
        // -------------------------------------------------------------------------------------------

        private static MatchParticipants Participants() =>
            StagedBoardParticipants.CreateWithBlueAlly();

        /// <summary>
        /// Three heroes and no obstacle at all: Brynn at the origin, one enemy hero inside p.19's ring
        /// and one three spaces away - inside every printed Radius his set uses and outside the ring,
        /// which is what separates the swap's two reaches.
        /// </summary>
        private static AdjacentMoveAuthority OpenBoard() =>
            new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    Map, 1, Disk(6).ToArray(), Array.Empty<HexCoordinate>()),
                new[]
                {
                    new HeroOccupancy(Brynn, BrynnAt),
                    new HeroOccupancy(Enemy, Adjacent),
                    new HeroOccupancy(SecondEnemy, InTheDisk)
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius) yield return new HexCoordinate(q, r);
                }
            }
        }

        /// <summary>The live set his unlocked Ultimate supplies while he is the acting object.</summary>
        private static RuleModificationSet UnlockedUltimate(AdjacentMoveAuthority board) =>
            RuleModificationQuery.Build(
                null,
                board,
                Participants(),
                TieBreakerState.Unrevealed,
                UltimateUnlockQuery.Derive(
                    new[] { BrynnCardContent.Definition(BrynnCardContent.OverTheTop) },
                    new[]
                    {
                        new CardInstance(
                            new CardInstanceId("brynn-set-over-the-top"),
                            BrynnCardContent.Definition(BrynnCardContent.OverTheTop).Reference,
                            Brynn,
                            new CardStateFacts(
                                CardLocation.UltimateArea, false, CardResolutionStatus.None,
                                CardFaceOrientation.FaceUp))
                    }),
                RuleObject.ForHero(Brynn),
                null,
                null);

        // -------------------------------------------------------------------------------------------
        // Helpers - one node, asked of its own declared inputs
        // -------------------------------------------------------------------------------------------

        private static string[] TrapBulletTwo(
            AdjacentMoveAuthority board, RuleModificationSet modifications) =>
            Heroes(Run(
                BrynnCardContent.DeadfallTrap, BrynnEffectContent.TrapRadiusSelectNode, board,
                modifications));

        private static string[] SwapSubjects(
            AdjacentMoveAuthority board, RuleModificationSet modifications) =>
            Heroes(Run(
                BrynnCardContent.HideTraces, BrynnEffectContent.TrackingSwapSubjectNode, board,
                modifications));

        private static bool GuidePresence(
            AdjacentMoveAuthority board, RuleModificationSet modifications)
        {
            var result = Run(
                BrynnCardContent.ExpeditionLeader, BrynnEffectContent.GuidePresenceNode, board,
                modifications);
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "The presence predicate can be asked on this board, so it answers rather than " +
                "refusing.");
            return result.Outcomes
                .Single(fact => fact.OutputName == BrynnComponentContent.PresentOutputName)
                .Value.BooleanValue;
        }

        private static string[] Heroes(EffectExecutionResult result)
        {
            Assert.That(
                result.Kind == EffectExecutionResultKind.NeedsChoice ||
                result.Kind == EffectExecutionResultKind.ImpossibleMandatory,
                Is.True,
                "A selection either offers candidates or has none to offer, and " + result.Kind +
                " is neither.");
            return result.Kind != EffectExecutionResultKind.NeedsChoice
                ? Array.Empty<string>()
                : result.Choice.Candidates
                    .Where(candidate => candidate.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.HeroId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
        }

        /// <summary>
        /// One composed node of one of his faces, executed against a board with the node's OWN declared
        /// inputs - read off the definition rather than written again here, so a case that passes is a
        /// case about the card rather than about the probe.
        /// </summary>
        private static EffectExecutionResult Run(
            CardDefinitionId cardId,
            string nodeId,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications,
            IReadOnlyList<CardDefinition> definitions = null,
            IReadOnlyList<CardInstance> instances = null)
        {
            var face = BrynnCardContent.Definition(cardId);
            var action = face.ActionDefinitions.Single(candidate => candidate.EffectRoot != null);
            var node = action.EffectRoot.Nodes.Single(
                candidate => candidate.Id.Value == nodeId);
            var handler = Catalog().Load(node.Capability);
            var participants = Participants();
            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-set-probe"), face.Reference, Brynn);

            var inputs = new Dictionary<string, EffectValue>();
            foreach (var input in node.Inputs)
            {
                if (input.SourceKind == EffectBindingSourceKind.Literal)
                {
                    inputs[input.ParameterName] = input.Literal;
                }
                else if (input.SourceKind == EffectBindingSourceKind.Role &&
                    input.ValueKind == EffectValueKind.Hero)
                {
                    // Every role a face of his binds on a node this fixture drives is the ACTOR: the
                    // owner and the chooser of a retrieval, which a resolution session supplies from
                    // the frame and a probe has to state.
                    inputs[input.ParameterName] = EffectValue.Hero(Brynn);
                }
            }

            return handler.Execute(new EffectCapabilityContext(
                new EffectNodeId(nodeId),
                EffectFailurePolicy.Mandatory,
                Brynn,
                Brynn,
                card.Id,
                card.DefinitionReference,
                action.Id,
                inputs,
                1,
                1,
                participants,
                null,
                modifications,
                EffectCardState.Of(
                    definitions ?? new[] { face },
                    instances ?? new[] { card }),
                action.Kind,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                new HeroItemState(participants)));
        }

        /// <summary>
        /// What True Grit's retrieval offers with his whole dealt set lying in his discard, plus one of
        /// his own faces in another hero's - p.10's other half.
        /// </summary>
        private static string[] RetrievablePool()
        {
            var definitions = BrynnCardContent.AllDefinitions.ToArray();
            var grit = BrynnCardContent.Definition(BrynnCardContent.TrueGrit);
            var performing = new CardInstance(
                new CardInstanceId("brynn-set-probe"),
                grit.Reference,
                Brynn,
                new CardStateFacts(
                    CardLocation.TurnArea, true, CardResolutionStatus.Unresolved,
                    CardFaceOrientation.FaceUp));

            var instances = new List<CardInstance> { performing };
            foreach (var definition in BrynnCardContent.DealtDefinitions
                .Where(candidate => !candidate.Id.Equals(BrynnCardContent.TrueGrit)))
            {
                instances.Add(Discarded(
                    "brynn-discard-" + definition.Id.Value, definition.Reference, Brynn));
            }

            // p.10: another hero's discard is never his to retrieve from, and his Gold basic is the
            // face that makes the negative visible.
            instances.Add(Discarded(
                "enemy-discard-familiar-ground",
                BrynnCardContent.Definition(BrynnCardContent.FamiliarGround).Reference,
                Enemy));

            var result = Run(
                BrynnCardContent.TrueGrit, BrynnEffectContent.GritRetrieveNode, OpenBoard(),
                RuleModificationSet.Empty, definitions, instances);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice),
                "His discard holds attack cards, so the retrieval asks which one.");
            return result.Choice.Candidates
                .Select(candidate => instances
                    .Single(instance => instance.Id.Equals(candidate.CardInstanceId))
                    .DefinitionReference.DefinitionId.Value)
                .ToArray();
        }

        private static CardInstance Discarded(
            string id, CardDefinitionReference reference, HeroId owner) =>
            new CardInstance(
                new CardInstanceId(id),
                reference,
                owner,
                new CardStateFacts(
                    CardLocation.DiscardPile, true, CardResolutionStatus.Resolved,
                    CardFaceOrientation.FaceUp));

        // -------------------------------------------------------------------------------------------
        // Helpers - one whole turn
        // -------------------------------------------------------------------------------------------

        private sealed class PeakPrecisionRun
        {
            public int AttackTotal;
            public bool RetrievalWasOffered;
            public CardLocation SourceCardLocation;
            public bool SourceCardHasBeenPlayed;
            public bool OfferedAgain;
        }

        /// <summary>
        /// Peak Precision played against a hero adjacent to three obstacles, with the retrieval taken -
        /// 8.69b's driver, run over a hand built from his own eighteen dealt faces rather than from one
        /// composed card.
        /// </summary>
        private static PeakPrecisionRun DrivePeakPrecision()
        {
            var configuration =
                MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
            var face = BrynnCardContent.Definition(BrynnCardContent.PeakPrecision);
            var attack = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            // p.6 leaves every Tier III face in the deck, so the hand staged here is the one he
            // holds after p.15 has levelled the Red line twice: his eighteen instances with the deck
            // copy of Peak Precision replaced by one in hand.
            var dealt = BrynnCardContent.CreateDealtCards(Brynn, "brynn-set-turn").ToArray();
            var played = CardInstance.CreateInHand(
                new CardInstanceId("brynn-set-turn-peak-in-hand"),
                face.Reference,
                Brynn);
            var hand = dealt
                .Where(card => !card.DefinitionReference.DefinitionId.Equals(
                    BrynnCardContent.PeakPrecision))
                .Concat(new[] { played })
                .ToArray();

            var walls = new[] { Enemy, SecondEnemy }.Select(hero => CardInstance.CreateInHand(
                new CardInstanceId("brynn-set-wall-" + hero.Value), DefenceWall.Reference, hero))
                .ToArray();
            var definitions = configuration.CardDefinitions
                .Concat(BrynnCardContent.AllDefinitions)
                .Concat(new[] { DefenceWall })
                .ToArray();
            var instances = configuration.CardInstances
                .Concat(hand)
                .Concat(walls)
                .ToArray();

            // Adjacent, because Peak Precision is a non-Ranged Attack. Brynn himself is one of the
            // three obstacles, exactly as his Gold's reminder says, so two terrain spaces are staged.
            var terrain = new[] { new HexCoordinate(2, 0), new HexCoordinate(2, -1) };
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    Map, 1, Disk(5).Where(space => !terrain.Contains(space)).ToArray(), terrain),
                new[]
                {
                    new HeroOccupancy(Brynn, BrynnAt),
                    new HeroOccupancy(Enemy, Adjacent),
                    new HeroOccupancy(SecondEnemy, new HexCoordinate(-3, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

            var cardActions = new CardActionResolutionAuthority(Catalogs(), board);

            foreach (var committed in new[]
            {
                played,
                instances.First(value =>
                    value.OwnerId.Equals(Enemy) && value.State.Location == CardLocation.Hand),
                instances.First(value =>
                    value.OwnerId.Equals(SecondEnemy) && value.State.Location == CardLocation.Hand)
            })
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
            }

            var turn = new TurnResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(), 1, 1);
            Assert.That(turn.TryStart().Accepted, Is.True);

            var run = new PeakPrecisionRun();
            var started = false;
            for (var step = 0; step < 80 && !turn.IsEnded; step++)
            {
                if (!started)
                {
                    if (turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        Assert.That(turn.TryChooseTiedActor(
                                tie.OwningSide,
                                tie.CandidateCardInstanceIds.Contains(played.Id)
                                    ? played.Id
                                    : tie.CandidateCardInstanceIds[0]).Accepted,
                            Is.True);
                        continue;
                    }

                    if (turn.PendingTurnRelease != null)
                    {
                        Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (!turn.SelectedActor.CardInstanceId.IsDefined)
                    {
                        break;
                    }

                    if (!turn.SelectedActor.CardInstanceId.Equals(played.Id))
                    {
                        Assert.That(
                            turn.TryPerformHold(turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                        continue;
                    }

                    var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                            definitions, instances, Brynn, played.Id)
                        .Single(value => value.ActionDefinitionId.Equals(attack.Id));
                    Assert.That(
                        turn.TryStartAttack(new ActionResolutionId("brynn-set-peak"), offer).Accepted,
                        Is.True);
                    started = true;
                    continue;
                }

                if (turn.PendingAttackTargetChoice != null)
                {
                    var choice = turn.PendingAttackTargetChoice;
                    Assert.That(turn.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                            choice.ResolutionId, choice.ChoiceId,
                            choice.LegalTargets.Single(value =>
                                value.Kind == AttackTargetKind.Hero &&
                                value.HeroId.Equals(Enemy)))).Accepted,
                        Is.True);
                    continue;
                }

                if (turn.PendingDefenseChoice != null)
                {
                    var defense = turn.PendingDefenseChoice;
                    run.AttackTotal = defense.AttackTotal;
                    var wall = defense.LegalDefenseOffers.FirstOrDefault(offer =>
                        offer.SourceDefinitionReference.Equals(DefenceWall.Reference));
                    Assert.That(turn.TryAnswerDefenseResponse(
                        DefenseResponseChoiceCommand.SelectCards(
                            defense.ResolutionId,
                            defense.ChoiceId,
                            wall.IsAvailable
                                ? wall.SourceCardInstanceId
                                : defense.LegalDefenseOffers[0].SourceCardInstanceId)).Accepted,
                        Is.True);
                    continue;
                }

                if (turn.PendingEffectChoice != null)
                {
                    var effect = turn.PendingEffectChoice;
                    run.RetrievalWasOffered = true;
                    Assert.That(
                        turn.TryAnswerEffectChoice(effect.ConfirmInvocation).Accepted, Is.True);
                    continue;
                }

                break;
            }

            Assert.That(started, Is.True, "Peak Precision never reached its own action.");
            run.SourceCardLocation = played.State.Location;
            run.SourceCardHasBeenPlayed = played.State.HasBeenPlayed;
            // p.7's own window rather than the turn one: a card in the TURN AREA is what
            // GetAvailableTurnActionOffers answers about, and the whole claim here is that the
            // retrieved card is back in HAND, where the normal-play offers are the ones that matter.
            run.OfferedAgain = ActionOfferQuery
                .GetAvailableOffers(definitions, instances, Brynn)
                .Any(value =>
                    value.SourceCardInstanceId.Equals(played.Id) &&
                    value.ActionDefinitionId.Equals(attack.Id));
            return run;
        }

        /// <summary>
        /// A probe card in each enemy hero's hand whose only action is a Defense of 9, so that p.14's
        /// step 6 never removes anybody: what is under test is the retrieval rather than p.13's
        /// consequences, and Peak Precision's raised total is 7.
        /// </summary>
        private static readonly CardDefinition DefenceWall = new CardDefinition(
            new CardDefinitionId("brynn-set-defence-wall"),
            "rev-1",
            "TEST - Brynn set defence wall",
            new InitiativeValue(20),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"),
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        // -------------------------------------------------------------------------------------------
        // Helpers - describing a face
        // -------------------------------------------------------------------------------------------

        private static void AssertSuccessors(
            CardDefinitionId cardId, params CardDefinitionId[] expected) =>
            Assert.That(
                BrynnCardContent.Definition(cardId).Progression.NextTierCandidateIds,
                Is.EquivalentTo(expected), cardId.Value);

        private static IEnumerable<PrintedValueChannel> PrintedChannels(CardDefinitionId cardId) =>
            BrynnCardContent.Definition(cardId).ActionDefinitions
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
        /// The amount a face's SECONDARY icon carries, or "-" where it prints none. BR-06 is the whole
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
                if (!line.StartsWith("| brynn", StringComparison.Ordinal))
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
        /// same way - "- **BR-Q1:**" and its siblings - and swallowing those would turn a count of
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
            File.ReadAllLines(Path.Combine(RepositoryRoot(), "Docs/AI/Rules/HERO_BRYNN.md"));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null &&
                !File.Exists(Path.Combine(directory.FullName, "Docs/AI/Rules/HERO_BRYNN.md")))
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
                "Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Brynn");
            return Directory.Exists(path) ? path : null;
        }
    }
}
