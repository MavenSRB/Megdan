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
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
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
    /// Milestone 8.57f. Rowenna the Vanguard's printed set as a SET: nineteen faces, the five p.6
    /// deals into her hand, the thirteen its deck keeps, the one p.6 returns to the box, the three
    /// coloured ladders p.15 climbs, the twelve items p.16 earns from the faces she does not choose,
    /// and the one revision every definition and every body carries.
    ///
    /// <para><b>What this fixture is for, and what it deliberately leaves to the five block
    /// fixtures.</b> 8.57a through 8.57e each proved one group of faces at the effect frame -
    /// <c>RowennaRewardTests</c>, <c>RowennaSwapAndGauntletTests</c>, <c>RowennaSurrenderTests</c>,
    /// <c>RowennaCloseQuartersTests</c> and <c>RowennaFabledLanceTests</c> - and nothing here repeats
    /// one of those bodies. What none of them could say is anything about the SET, because until this
    /// milestone there was no set: no starting hand, no deck, no boxed Handicap and no
    /// <c>DealtDefinitions</c>. So the claims below are the ones that need all nineteen faces at once -
    /// the printed inventory against her shard, the partition, the ladders, the items, the deferral,
    /// the exact list of contracts this build still refuses - plus the behaviours that are about the
    /// set rather than a face: that every card p.6 deals into her hand can actually be played, that
    /// every Tier I climbs to either printed sibling through the production query, and that a Red
    /// rider still composes once her Ultimate has changed what her Attacks are.</para>
    ///
    /// <para><b>Nothing of hers is registered yet, and that is asserted rather than assumed.</b>
    /// Milestone 7.9's gate holds her content out of <see cref="MatchContentCatalogs"/> until 8.57g
    /// deals her, so the catalog these cases validate and drive against is the build's registration
    /// list PLUS the two Place declarations her series deliberately withholds. The gate itself is
    /// asserted from the other side, by naming the exact set of capability references this build
    /// refuses - which is how a registration that arrives early, or a third deferral that arrives
    /// unnoticed, fails here rather than at her dealing.</para>
    /// </summary>
    [TestFixture]
    public sealed class RowennaPrintedSetTests
    {
        private static readonly HeroId Rowenna = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);

        private static readonly HexCoordinate RowennaAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);
        private static readonly HexCoordinate TwoAway = new HexCoordinate(2, 0);
        private static readonly HexCoordinate Beside = new HexCoordinate(0, 1);

        /// <summary>
        /// The two declarations milestone 7.9's gate keeps out of the registration list until 8.57g
        /// deals her, spelled as the build spells them. 8.57b minted the printed-Range one for Throw
        /// the Gauntlet and 8.57d its printed-Radius twin for her three Green Movement faces; 8.57e's
        /// Ultimate added NOTHING to this list, because a permanent ability that changes what p.13
        /// makes of an Attack is a typed rule rather than a capability a node names.
        /// </summary>
        private static readonly string[] DeferredCapabilityReferences =
        {
            "synthetic-place-in-radius-beside-a-named-hero@1",
            "synthetic-place-in-range-beside-a-named-hero@1"
        };

        /// <summary>
        /// The four faces whose bodies name one of those two, and therefore the exact four this build
        /// refuses to start. Every other composed face of hers names only registered contracts.
        /// </summary>
        private static IEnumerable<CardDefinitionId> FacesNamingADeferredPlace() => new[]
        {
            RowennaCardContent.ThrowTheGauntlet,
            RowennaCardContent.CloseQuarters,
            RowennaCardContent.Melee,
            RowennaCardContent.GrandMelee
        };

        // -------------------------------------------------------------------------------------------
        // The printed inventory
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One row per face, in composition order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item.
        /// A dash is what the face prints nothing for, which is RO-05 written out nineteen times: a
        /// missing icon is absent, never zero, and an absent secondary is an action that does not
        /// exist rather than one whose value is 0.
        ///
        /// <para>It is deliberately a literal table rather than something derived, for the reason
        /// <c>WaspVerifiedContentTests</c> and every set after it record: a test that derived its
        /// expectations from the definitions would pass whatever the definitions happened to say. This
        /// one fails if a single number, colour, Tier, icon or name drifts from
        /// <c>Docs/AI/Rules/HERO_ROWENNA.md</c>, which is the document the extracted faces were
        /// transcribed into - and the case after it reads that document back independently, so the two
        /// transcriptions have to agree with each other as well as with the content.</para>
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "rowenna-code-of-chivalry|Code of Chivalry|Init 11|SecDef 2|SecMov 1|primary-attack Attack NonRanged [Attack 4]|Gold Basic|item -",
            "rowenna-token-of-gratitude|Token of Gratitude|Init 8|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 5, Radius 1]|Red I|item -",
            "rowenna-fair-share|Fair Share|Init 8|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 6, Radius 3]|Red II|item Initiative",
            "rowenna-feat-of-bravery|Feat of Bravery|Init 8|SecDef 7|SecMov 4|primary-attack Attack NonRanged [Attack 7, Radius 4]|Red II|item Defense",
            "rowenna-paragon-of-grace|Paragon of Grace|Init 9|SecDef 7|SecMov 4|primary-attack Attack NonRanged [Attack 6, Radius 3]|Red III|item Range",
            "rowenna-paragon-of-valor|Paragon of Valor|Init 9|SecDef 8|SecMov 4|primary-attack Attack NonRanged [Attack 7, Radius 4]|Red III|item Defense",
            "rowenna-throw-the-gauntlet|Throw the Gauntlet|Init 5|SecDef 3|SecMov -|primary-basic-skill Skill None [Range 4]|Silver Basic|item -",
            "rowenna-stand-guard|Stand Guard|Init 9|SecDef 6|SecMov 2|primary-skill Skill None [Range 3]|Blue I|item -",
            "rowenna-protect-the-weak|Protect the Weak|Init 10|SecDef 6|SecMov 2|primary-skill Skill None [Range 4]|Blue II|item Attack",
            "rowenna-accept-surrender|Accept Surrender|Init 10|SecDef 6|SecMov 2|primary-skill Skill None []|Blue II|item Defense",
            "rowenna-defend-the-innocent|Defend the Innocent|Init 10|SecDef 7|SecMov 2|primary-skill Skill None [Range 4]|Blue III|item Radius",
            "rowenna-glorious-triumph|Glorious Triumph|Init 10|SecDef 7|SecMov 2|primary-skill Skill None []|Blue III|item Movement",
            "rowenna-close-quarters|Close Quarters|Init 4|SecDef 3|SecMov -|primary-movement Movement None [Movement 2, Radius 2]|Green I|item -",
            "rowenna-melee|Melee|Init 4|SecDef 4|SecMov -|primary-movement Movement None [Movement 2, Radius 3]|Green II|item Attack",
            "rowenna-opening-shots|Opening Shots|Init 4|SecDef 4|SecMov 2|primary-skill Skill None [Radius 3]|Green II|item Initiative",
            "rowenna-grand-melee|Grand Melee|Init 3|SecDef 4|SecMov -|primary-movement Movement None [Movement 2, Radius 3]|Green III|item Attack",
            "rowenna-opening-volley|Opening Volley|Init 3|SecDef 4|SecMov 2|primary-skill Skill None [Radius 3]|Green III|item Initiative",
            "rowenna-fabled-lance|Fabled Lance|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -",
            "rowenna-earn-the-spurs|Earn the Spurs|Init 10|SecDef 1|SecMov 1|primary-attack Attack NonRanged [Attack 4]|Gold Basic|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                RowennaCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.57 verified against the extracted faces. Correct the definition, not this table - " +
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
            Assert.That(RowennaCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(RowennaCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                RowennaCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { RowennaCardContent.EarnTheSpurs }));
            Assert.That(
                RowennaCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(RowennaCardContent.EarnTheSpurs),
                "and the dealt eighteen are the nineteen minus exactly that one.");

            var byColour = RowennaCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2), "Code of Chivalry and Earn the Spurs.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1), "Throw the Gauntlet.");
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5));
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "Fabled Lance.");

            Assert.That(
                RowennaCardContent.Definition(RowennaCardContent.FabledLance)
                    .Progression.Classification,
                Is.EqualTo(CardClassification.Ultimate));
            Assert.That(
                RowennaCardContent.Definition(RowennaCardContent.FabledLance).ActionDefinitions,
                Is.Empty,
                "p.16's permanent ability prints no action at all, so she cannot even discard it to " +
                "defend - which is the absence RO-05 protects, stated for a whole card.");
        }

        /// <summary>
        /// p.6's five starting cards and the thirteen the deck keeps partition her dealt eighteen
        /// exactly, so no card is both and none is neither - and the five are one of each colour,
        /// which is the shape p.6 states rather than a fact about her in particular.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHerDealtEighteen()
        {
            Assert.That(RowennaCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(RowennaCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(RowennaCardContent.StartingHand.Intersect(RowennaCardContent.Deck), Is.Empty);
            Assert.That(
                RowennaCardContent.StartingHand.Concat(RowennaCardContent.Deck),
                Is.EquivalentTo(RowennaCardContent.DealtDefinitions.Select(card => card.Id)));

            Assert.That(RowennaCardContent.StartingHand, Is.EqualTo(new[]
            {
                RowennaCardContent.CodeOfChivalry,
                RowennaCardContent.ThrowTheGauntlet,
                RowennaCardContent.TokenOfGratitude,
                RowennaCardContent.StandGuard,
                RowennaCardContent.CloseQuarters
            }));

            Assert.That(
                RowennaCardContent.StartingHand
                    .Select(id => RowennaCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.6 deals one of each colour, and hers are the Gold, the Silver and the three Tier I.");

            Assert.That(
                RowennaCardContent.Deck
                    .Select(id => RowennaCardContent.Definition(id))
                    .Count(definition => definition.Progression.IsUltimate),
                Is.EqualTo(1),
                "p.16's card starts in the deck and is reached by the unlock rather than by a draw.");
            Assert.That(
                RowennaCardContent.Deck
                    .Select(id => RowennaCardContent.Definition(id))
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
        /// One seat's whole setup, built the way a configured match will build it at 8.57g: eighteen
        /// instances, five of them in hand and thirteen in the deck, with the boxed Handicap nowhere at
        /// all.
        /// </summary>
        [Test]
        public void HerDealtHand_IsFiveInHandAndThirteenInTheDeck_AndNeverTheHandicap()
        {
            var hand = RowennaCardContent.CreateHand(Rowenna, "rowenna-printed-set");

            Assert.That(hand.AllInstances.Count, Is.EqualTo(18));
            Assert.That(
                hand.AllInstances.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(RowennaCardContent.EarnTheSpurs));

            var cards = RowennaCardContent.CreateDealtCards(Rowenna, "rowenna-printed-set");
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(RowennaCardContent.StartingHand));
            Assert.That(
                cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(RowennaCardContent.Deck));
            Assert.That(cards.Select(card => card.Id.Value).Distinct().Count(), Is.EqualTo(18),
                "and each instance identity is distinct, which is what the per-seat prefix is for.");
            Assert.That(cards.All(card => card.OwnerId.Equals(Rowenna)), Is.True);

            Assert.That(
                () => RowennaCardContent.CreateDealtCards(default, "rowenna-printed-set"),
                Throws.ArgumentException,
                "A card without an owner is not a card p.6 dealt to anybody.");
            Assert.That(
                () => RowennaCardContent.CreateDealtCards(Rowenna, "  "),
                Throws.ArgumentException,
                "and the per-seat prefix is the caller's to name, so an empty one is refused rather " +
                "than silently shared between two seats.");
        }

        // -------------------------------------------------------------------------------------------
        // The upgrade ladders and the items
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.15's ladders, under RO-03's working assumption. Each coloured Tier I names both of its
        /// colour's Tier II faces, each Tier II names both of its colour's Tier III faces, and each
        /// Tier III names none.
        ///
        /// <para><b>RO-03 is open on four of these faces and this is the case it would land in.</b>
        /// Accept Surrender and Glorious Triumph carry blue ribbons and a corner G; Melee and Grand
        /// Melee carry green ribbons and a corner B. The shard's working assumption is the RIBBON,
        /// which puts the first two on the Blue ladder and the second two on the Green one, and that is
        /// exactly what this case states. Nothing else in her set moves either way: their text, values,
        /// Initiative, item and Tier are printed and read, and only which colour's ladder they sit on
        /// is in question.</para>
        /// </summary>
        [Test]
        public void EveryUpgradePair_IsThePrintedLadderUnderTheRibbonAssumption()
        {
            AssertSuccessors(
                RowennaCardContent.TokenOfGratitude,
                RowennaCardContent.FairShare, RowennaCardContent.FeatOfBravery);
            AssertSuccessors(
                RowennaCardContent.FairShare,
                RowennaCardContent.ParagonOfGrace, RowennaCardContent.ParagonOfValor);
            AssertSuccessors(
                RowennaCardContent.FeatOfBravery,
                RowennaCardContent.ParagonOfGrace, RowennaCardContent.ParagonOfValor);

            AssertSuccessors(
                RowennaCardContent.StandGuard,
                RowennaCardContent.ProtectTheWeak, RowennaCardContent.AcceptSurrender);
            AssertSuccessors(
                RowennaCardContent.ProtectTheWeak,
                RowennaCardContent.DefendTheInnocent, RowennaCardContent.GloriousTriumph);
            AssertSuccessors(
                RowennaCardContent.AcceptSurrender,
                RowennaCardContent.DefendTheInnocent, RowennaCardContent.GloriousTriumph);

            AssertSuccessors(
                RowennaCardContent.CloseQuarters,
                RowennaCardContent.Melee, RowennaCardContent.OpeningShots);
            AssertSuccessors(
                RowennaCardContent.Melee,
                RowennaCardContent.GrandMelee, RowennaCardContent.OpeningVolley);
            AssertSuccessors(
                RowennaCardContent.OpeningShots,
                RowennaCardContent.GrandMelee, RowennaCardContent.OpeningVolley);

            foreach (var tierThree in RowennaCardContent.AllDefinitions
                .Where(definition => definition.Progression.Tier == CardTier.III))
            {
                AssertSuccessors(tierThree.Id);
            }

            // The two faces that sit on no ladder at all, and for two different printed reasons: p.20
            // keeps the Handicap in the box, and p.16 UNLOCKS the Ultimate rather than upgrading into
            // it. Neither names a successor and neither is named by one.
            foreach (var offLadder in new[]
            {
                RowennaCardContent.FabledLance,
                RowennaCardContent.EarnTheSpurs,
                RowennaCardContent.CodeOfChivalry,
                RowennaCardContent.ThrowTheGauntlet
            })
            {
                AssertSuccessors(offLadder);
                Assert.That(
                    RowennaCardContent.AllDefinitions.SelectMany(definition =>
                        definition.Progression.NextTierCandidateIds),
                    Has.No.Member(offLadder),
                    offLadder.Value + " is named by no upgrade pair either.");
            }
        }

        /// <summary>
        /// p.16's items, read off the faces she does not choose: every one of the twelve Tier II and
        /// Tier III faces prints one, every Tier I, basic and the Ultimate print none, and the twelve
        /// are the six channels in the counts her shard's table records - three Initiative, three
        /// Defense, three Attack, and one each of Range, Radius and Movement.
        /// </summary>
        [Test]
        public void EveryUnchosenFace_BecomesItsPrintedItem()
        {
            foreach (var definition in RowennaCardContent.AllDefinitions)
            {
                var printsAnItem = definition.Progression.ItemIcon != PrintedValueChannel.None;
                var isUpgradeTier =
                    definition.Progression.Tier == CardTier.II ||
                    definition.Progression.Tier == CardTier.III;

                Assert.That(printsAnItem, Is.EqualTo(isUpgradeTier), definition.Id.Value +
                    ": an item is what the face she did NOT choose becomes, so exactly the twelve " +
                    "faces a level-up can leave behind print one.");
            }

            var byChannel = RowennaCardContent.AllDefinitions
                .Select(definition => definition.Progression.ItemIcon)
                .Where(channel => channel != PrintedValueChannel.None)
                .GroupBy(channel => channel)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byChannel[PrintedValueChannel.Initiative], Is.EqualTo(3),
                "Fair Share, Opening Shots and Opening Volley.");
            Assert.That(byChannel[PrintedValueChannel.Defense], Is.EqualTo(3),
                "Feat of Bravery, Paragon of Valor and Accept Surrender.");
            Assert.That(byChannel[PrintedValueChannel.Attack], Is.EqualTo(3),
                "Protect the Weak, Melee and Grand Melee.");
            Assert.That(byChannel[PrintedValueChannel.Range], Is.EqualTo(1), "Paragon of Grace.");
            Assert.That(byChannel[PrintedValueChannel.Radius], Is.EqualTo(1), "Defend the Innocent.");
            Assert.That(byChannel[PrintedValueChannel.Movement], Is.EqualTo(1), "Glorious Triumph.");
            Assert.That(byChannel.Values.Sum(), Is.EqualTo(12));
        }

        // -------------------------------------------------------------------------------------------
        // The source corpus
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The source sweep: every composed face maps to exactly one <c>rowenna*__front.png</c> stem in
        /// her shard's inventory table, and no stem is left over.
        ///
        /// <para>The table is read back out of <c>Docs/AI/Rules/HERO_ROWENNA.md</c> rather than
        /// transcribed a second time here, which is what makes this case independent of the literal
        /// table above: that one is this fixture's own transcription of the extracted faces and this
        /// one is the shard's, so a drift in either is a disagreement rather than two copies of one
        /// mistake. What it reads is the columns a line can carry unambiguously - the stem, the face
        /// name, the colour, the Tier, the Initiative and the two secondary icons - and it leaves the
        /// primary column to the literal table, because "Basic Attack / A 4" is prose.</para>
        ///
        /// <para>The fronts themselves live under a git-ignored directory, so a clean checkout has the
        /// table and not the images. The stem-to-file half therefore runs only where the corpus is
        /// actually present, and says so rather than passing silently - which is the same tolerance
        /// <c>ConfiguredCardArtworkCatalogTests</c>' clean-checkout walk is built on.</para>
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
                    row.Stem.StartsWith("rowenna", StringComparison.Ordinal) &&
                    row.Stem.Contains("__"),
                    Is.True,
                    row.Stem + " is not a stem under her source directory.");
            }

            Assert.That(
                rows.Select(row => row.Name).OrderBy(name => name, StringComparer.Ordinal),
                Is.EqualTo(RowennaCardContent.AllDefinitions
                    .Select(definition => definition.DisplayName)
                    .OrderBy(name => name, StringComparer.Ordinal)),
                "and the nineteen named faces are exactly the nineteen composed ones - a bijection " +
                "rather than a containment, so an extracted face nobody composed fails here too.");

            foreach (var row in rows)
            {
                var definition = RowennaCardContent.AllDefinitions
                    .Single(candidate => candidate.DisplayName == row.Name);
                var progression = definition.Progression;

                Assert.That(
                    progression.IsUltimate
                        ? "-"
                        : definition.Initiative.Value.ToString(),
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

                // RO-03's working assumption, read off the shard's own colour cell: the four faces
                // whose corner letter contradicts their ribbon carry the RIBBON colour here, and the
                // shard's cell names it with the conflict in parentheses beside it.
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
        /// <para>Her shard writes six of its bullets for two faces at once, because two cards printing
        /// one sentence at two Radii are one bullet - Token of Gratitude / Fair Share, Stand Guard /
        /// Protect the Weak, Close Quarters / Melee - and it writes Grand Melee's two bullets as "(the
        /// same two bullets)". That shorthand is expanded here explicitly rather than tolerated, so the
        /// only text this case will accept for Grand Melee is Close Quarters' bullet pair word for
        /// word after its own printed opening.</para>
        /// </summary>
        [Test]
        public void EveryPrintedRule_IsTranscribedFromTheShard()
        {
            var bullets = PrintedRuleBullets();
            Assert.That(bullets.Count, Is.EqualTo(19),
                "Every one of the nineteen faces has a bullet of its own in the shard's rules list.");

            foreach (var definition in RowennaCardContent.AllDefinitions)
            {
                Assert.That(bullets.ContainsKey(definition.DisplayName), Is.True,
                    definition.DisplayName + " has no printed-rule bullet in the shard.");

                var expected = bullets[definition.DisplayName];
                if (definition.Id.Equals(RowennaCardContent.GrandMelee))
                {
                    var closeQuarters = bullets["Close Quarters"];
                    const string Opening = "you may Choose one - ";
                    expected = expected.Replace(
                        "(the same two bullets).",
                        closeQuarters.Substring(
                            closeQuarters.IndexOf(Opening, StringComparison.Ordinal) +
                            Opening.Length));
                }

                Assert.That(definition.SourceContent.PrintedText, Is.EqualTo(expected),
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
        /// whose whole printed content is a typed rule and which prints no action to hang one on, and
        /// p.20's Handicap, whose sentence stands under the established deferral.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogHerDealingWillRegister()
        {
            var catalog = Catalog();
            var composed = 0;

            foreach (var definition in RowennaCardContent.AllDefinitions)
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
                "Seventeen bodies: 8.57a's six - Token of Gratitude and Fair Share are two cards " +
                "printing one sentence at two Radii and each carries its own - 8.57b's four, 8.57c's " +
                "two Blue Defeats and 8.57d's five Greens. Fabled Lance composes as a typed rule and " +
                "Earn the Spurs composes nothing at all.");

            // And the one face that composes NO body still carries its printed content, which is the
            // half an action-walking case cannot say: p.16's ability is a typed rule that names no
            // effect definition at all, so there is nothing for a catalog to fail to load - which is
            // also why 8.57e cost the registration list nothing.
            var rule = RowennaCardContent.Definition(RowennaCardContent.FabledLance).UltimateRule;
            Assert.That(rule, Is.Not.Null);
            Assert.That(rule.TriggeredEffect, Is.Null,
                "A permanent ability that changes what p.13 makes of an Attack names a modification " +
                "family rather than an effect, so no catalog entry is owed for it.");
            Assert.That(rule.Modifications.Single().Kind,
                Is.EqualTo(RuleModificationKind.AttackActionsCountAsRangedAtPrintedRange));
            Assert.That(
                rule.PrintedValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Range));
            Assert.That(
                rule.PrintedValues.Single().Amount,
                Is.EqualTo(RowennaEffectContent.FabledLancePrintedRange),
                "and p.9's one icon on the card is the same printed number its sentence names.");
        }

        /// <summary>
        /// The exact set of capability references her composed bodies name that this build does NOT
        /// register - the observable form of milestone 7.9's gate, asserted as a SET so that a
        /// registration arriving early and a third deferral arriving unnoticed both fail here.
        ///
        /// <para>It is two, both of them Places, and 8.57e added neither: her Ultimate's sentence is a
        /// <c>RuleModificationKind</c> rather than a capability a node names, so a permanent ability
        /// costs the registration list nothing.</para>
        /// </summary>
        [Test]
        public void ExactlyTwoPlaceDeclarations_AreStillHeldBackFromTheRegistrationList()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            var named = RowennaCardContent.AllDefinitions
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
                Is.EqualTo(DeferredCapabilityReferences),
                "Exactly two, and they are the two her series minted: 8.57b's printed-Range Place " +
                "beside a named hero and 8.57d's printed-Radius twin. Every other contract her set " +
                "names is already in the build's registration list.");

            Assert.That(
                SyntheticPlacementCapabilities.DeferredHandlers()
                    .Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(DeferredCapabilityReferences),
                "and the build's own deferred list is exactly those two, read from the other side: " +
                "nothing else in this build is being withheld for her, and nothing of hers is being " +
                "withheld anywhere else.");

            Assert.That(
                named.Count(capability => registered.IsRegistered(capability)),
                Is.GreaterThan(0),
                "and the rest genuinely resolve, so the case above is not passing because the walk " +
                "found nothing.");
        }

        /// <summary>
        /// And the four faces that name one of those two are the four this build refuses, each for the
        /// deferral rather than for anything about its shape. Milestone 8.57g's registration flip is
        /// what turns this assertion around.
        /// </summary>
        [Test]
        public void OnlyTheFourFacesNamingADeferredPlace_AreRefusedByWhatThisBuildRegisters()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;
            var refused = new List<CardDefinitionId>();

            foreach (var definition in RowennaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions
                    .Where(action => action.EffectRoot != null))
                {
                    var rejections = EffectDefinitionValidator.ValidateCardText(
                        action.EffectRoot, registered, action.CardTextTiming);
                    if (rejections.Count == 0)
                    {
                        continue;
                    }

                    refused.Add(definition.Id);
                    Assert.That(
                        rejections.Select(rejection => rejection.Reason).Distinct(),
                        Is.EqualTo(new[] { EffectDefinitionRejectionReason.UnregisteredCapability }),
                        definition.Id.Value + " is refused for the deferral and for nothing else.");
                }
            }

            Assert.That(refused, Is.EquivalentTo(FacesNamingADeferredPlace()),
                "Her fifteen other bodies name only capabilities this build already registers.");
        }

        /// <summary>
        /// The only deferral anywhere on her set is p.20's, on the one boxed face. No dealt face of
        /// hers owes a composition to a milestone, and none carries the odd-player trigger.
        /// </summary>
        [Test]
        public void OnlyTheBoxedHandicap_DefersItsPrintedTextComposition()
        {
            foreach (var definition in RowennaCardContent.DealtDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    Assert.That(action.TextComposition.IsAwaited, Is.False,
                        definition.Id.Value + " action '" + action.Id.Value + "' must owe nothing: " +
                        "an engine deferral on a dealt face is a card a match would grey out.");
                }
            }

            var handicap = RowennaCardContent.Definition(RowennaCardContent.EarnTheSpurs);
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
        }

        /// <summary>
        /// One revision string covers the whole set - every definition and every body reached from one.
        /// Milestone 8.57f minted it; the five composition blocks carried a deliberately malformed
        /// placeholder, and no face may be left on it.
        /// </summary>
        [Test]
        public void OneRealRevision_CoversEveryDefinitionAndEveryBody()
        {
            Assert.That(RowennaCardContent.ContentRevision, Is.EqualTo("rowenna-8-57f-rev-1"));
            Assert.That(RowennaCardContent.ContentSetId, Is.EqualTo("rowenna"));

            Assert.That(
                RowennaCardContent.AllDefinitions
                    .Select(definition => definition.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { RowennaCardContent.ContentRevision }));

            Assert.That(
                RowennaCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .Select(action => action.EffectRoot.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { RowennaCardContent.ContentRevision }),
                "and the bodies carry it too, which is the half a card-only check would miss.");

            Assert.That(
                RowennaCardContent.AllDefinitions.All(definition =>
                    definition.Id.Value.StartsWith("rowenna-", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                RowennaCardContent.AllDefinitions.Select(definition => definition.ContentAuthority).Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
        }

        /// <summary>
        /// Which of rulebook p.14's and p.11's moments each face's rider is applied at, as a set. Her
        /// Gold basic is the only face in her whole set that carries step 1's text; every one of her
        /// five Reds carries step 7's; her three Green Movement faces print "After movement" in so many
        /// words; and no Skill of hers declares a moment at all, because a Skill's text is its action.
        ///
        /// <para>None of the nineteen prints a SECOND rider, which is the other half of "branch
        /// orders": a face with two moments would carry a second root and a second timing, and hers
        /// never do - the Paragons' repeat is inside one body rather than beside it.</para>
        /// </summary>
        [Test]
        public void EveryFacesPrintedMoment_IsTheOneItsOwnWordsName()
        {
            Assert.That(
                RowennaCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions
                        .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .Select(action => definition.DisplayName + " " + action.CardTextTiming))
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Code of Chivalry BeforeTheAttack",
                    "Token of Gratitude AfterTheAttack",
                    "Fair Share AfterTheAttack",
                    "Feat of Bravery AfterTheAttack",
                    "Paragon of Grace AfterTheAttack",
                    "Paragon of Valor AfterTheAttack",
                    "Throw the Gauntlet None",
                    "Stand Guard None",
                    "Protect the Weak None",
                    "Accept Surrender None",
                    "Defend the Innocent None",
                    "Glorious Triumph None",
                    "Close Quarters AfterTheMovement",
                    "Melee AfterTheMovement",
                    "Opening Shots None",
                    "Grand Melee AfterTheMovement",
                    "Opening Volley None",
                    "Earn the Spurs None"
                }),
                "Eighteen primaries - the Ultimate prints none - each at the moment its own sentence " +
                "names.");

            foreach (var action in RowennaCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions))
            {
                Assert.That(action.SecondCardTextRoot, Is.Null,
                    "No face of hers prints two riders, so none carries a second root.");
                Assert.That(action.SecondCardTextTiming, Is.EqualTo(CardTextTiming.None));
            }

            foreach (var definition in RowennaCardContent.AllDefinitions
                .Where(definition => !definition.Progression.IsUltimate))
            {
                var defense = definition.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Defense);
                Assert.That(defense.Origin, Is.EqualTo(ActionDefinitionOrigin.Reaction));
                Assert.That(
                    defense.DefaultPolicy.CardTextApplication,
                    Is.EqualTo(CardTextApplicationPolicy.Suppressed),
                    definition.Id.Value + ": p.9's secondary shield performs no card text, which is " +
                    "why a Defense of hers never runs the rider its face prints.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The set, driven
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every card p.6 deals into her hand can actually be played: committed for the round, offered
        /// by the production offer query, and started as the action its band prints.
        ///
        /// <para><b>This is the claim only a whole set can make.</b> The five block fixtures each drive
        /// one body from an effect frame, which says nothing at all about whether a match could reach
        /// that body from a card in a hand - and three of her five starting cards print three different
        /// bands, so "the starting hand plays" is five different production paths rather than one
        /// repeated. The board is staged so that every one of the five has a legal subject: the enemy
        /// hero stands beside her, and her ally stands beside the enemy, which is what Stand Guard's
        /// "friendly unit ... adjacent to an enemy hero" needs and what Throw the Gauntlet places her
        /// next to.</para>
        /// </summary>
        [TestCase("rowenna-code-of-chivalry", ActionKind.Attack)]
        [TestCase("rowenna-throw-the-gauntlet", ActionKind.Skill)]
        [TestCase("rowenna-token-of-gratitude", ActionKind.Attack)]
        [TestCase("rowenna-stand-guard", ActionKind.Skill)]
        [TestCase("rowenna-close-quarters", ActionKind.Movement)]
        public void EveryStartingCard_IsCommittedOfferedAndStartedFromADealtHand(
            string cardId, ActionKind kind)
        {
            var turn = new TurnFixture(new CardDefinitionId(cardId));

            Assert.That(turn.PrimaryOffer, Is.Not.Null,
                cardId + " must offer its printed primary from a hand p.6 dealt.");
            Assert.That(turn.PrimaryOffer.ActionDefinitionId, Is.EqualTo(turn.PrimaryActionId));
            Assert.That(turn.PrimaryKind, Is.EqualTo(kind),
                "and the band it is offered in is the one p.9 draws it in.");

            var result = turn.StartPrimary();
            Assert.That(result.Accepted, Is.True,
                cardId + " must start: " + result.RejectionReason + "/" +
                result.CardActionRejectionReason);
        }

        /// <summary>
        /// And the thirteen the deck keeps are offered nothing at all, from the same hand and the same
        /// query - p.15's Level Up and p.16's unlock are how a card leaves that deck, and a turn is
        /// not.
        /// </summary>
        [Test]
        public void NoDeckCard_IsOfferedFromADealtHand()
        {
            var turn = new TurnFixture(RowennaCardContent.CodeOfChivalry);

            foreach (var deckCard in RowennaCardContent.Deck)
            {
                Assert.That(turn.OffersFor(deckCard), Is.Empty,
                    deckCard.Value + " sits in the deck at setup and no turn action reaches it.");
            }
        }

        /// <summary>
        /// p.15's Upgrade a Card step, asked of the production query rather than of the progression
        /// record: each of her three Tier I faces offers both of its printed siblings, and taking
        /// either one leads to both of that colour's Tier III faces. That is the ladder walked rather
        /// than declared, which is the difference between this case and the pairs case above.
        /// </summary>
        [Test]
        public void EveryTierOneFace_ClimbsToEitherSibling_AndFromEitherToBothTierThrees()
        {
            var definitions = RowennaCardContent.AllDefinitions;

            foreach (var tierOne in new[]
            {
                RowennaCardContent.TokenOfGratitude,
                RowennaCardContent.StandGuard,
                RowennaCardContent.CloseQuarters
            })
            {
                var siblings = CardCharacteristicQuery.FindUpgradeCandidates(tierOne, definitions);
                Assert.That(siblings.Count, Is.EqualTo(2), tierOne.Value);
                Assert.That(
                    siblings.Select(candidate => candidate.Progression.Tier).Distinct(),
                    Is.EqualTo(new[] { CardTier.II }));
                Assert.That(
                    siblings.Select(candidate => candidate.Progression.Color).Distinct(),
                    Is.EqualTo(new[]
                    {
                        RowennaCardContent.Definition(tierOne).Progression.Color
                    }),
                    tierOne.Value + ": p.15 upgrades into the same colour one Tier higher, which is " +
                    "exactly the line RO-03 would move if the user reads the corner letters instead.");

                foreach (var sibling in siblings)
                {
                    var tierThrees = CardCharacteristicQuery.FindUpgradeCandidates(
                        sibling.Id, definitions);
                    Assert.That(tierThrees.Count, Is.EqualTo(2), sibling.Id.Value);
                    Assert.That(
                        tierThrees.Select(candidate => candidate.Progression.Tier).Distinct(),
                        Is.EqualTo(new[] { CardTier.III }));

                    foreach (var tierThree in tierThrees)
                    {
                        Assert.That(
                            CardCharacteristicQuery.FindUpgradeCandidates(tierThree.Id, definitions),
                            Is.Empty,
                            tierThree.Id.Value + " is the top of its ladder and p.16 unlocks the " +
                            "Ultimate rather than upgrading into it.");
                    }
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Ultimate composed with the rest of the set
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A Red rider composes with Fabled Lance's targeting. Milestone 8.57e proved the RULE - that
        /// her attacks become Ranged and read a printed Range of 2 - over every Attack face she prints;
        /// what this case proves is the COMPOSITION on one of them, which is a different question and
        /// the one only a whole set can ask: Paragon of Grace reaches two spaces because the Ultimate
        /// says so, and the sentence printed on that same card still pays its coin and still asks for
        /// its repeat with the Attack now Ranged and taken at a distance.
        ///
        /// <para><b>Why the two halves are one case.</b> The rule changes what p.13 makes of the
        /// action; the rider reads the card's own printed RADIUS through a different query entirely.
        /// Splitting them would let each half pass while the pair disagreed, which is exactly the
        /// failure a composition milestone exists to find - so the second half is run with the
        /// enclosing attack recorded as <see cref="AttackRangeKind.Ranged"/>, which is what the overlay
        /// in the first half makes it.</para>
        /// </summary>
        [Test]
        public void AParagonsRider_StillPaysAndStillRepeats_WithFabledLanceMakingItsAttackRanged()
        {
            var lance = new UltimateFixture();

            Assert.That(
                lance.TargetsOf(RowennaCardContent.ParagonOfGrace, EnemyAt),
                Is.EqualTo(new[] { Enemy }),
                "Before the unlock she prints 'Target a unit adjacent to you' and means it.");
            Assert.That(lance.TargetsOf(RowennaCardContent.ParagonOfGrace, TwoAway), Is.Empty);

            lance.Unlock();

            Assert.That(
                lance.ClassificationOf(RowennaCardContent.ParagonOfGrace),
                Is.EqualTo(AttackRangeKind.Ranged));
            Assert.That(
                lance.TargetsOf(RowennaCardContent.ParagonOfGrace, TwoAway),
                Is.EqualTo(new[] { Enemy }),
                "and the Ultimate's own printed Range of 2 is what she now reaches with it.");

            var world = RiderWorld.ForRangedAttack(RowennaCardContent.ParagonOfGrace);
            var pick = world.RunToPause();

            Assert.That(
                pick.SelectInvocations
                    .Select(invocation => invocation.Candidate.Value.HeroId)
                    .Where(heroId => heroId.IsDefined),
                Is.EqualTo(new[] { Ally }),
                "The rider reads the card's own printed Radius 3 through RadiusValueQuery, which the " +
                "Ultimate's Range does not touch - so the same ally is the same candidate.");

            var repeat = world.Answer(
                pick,
                pick.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                    invocation.Candidate.Value.HeroId.Equals(Ally)));

            Assert.That(world.Coins(Ally), Is.EqualTo(1), "The coin is paid on a Ranged attack too.");
            Assert.That(repeat.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));

            var finished = world.Submit(repeat.ConfirmInvocation);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(finished.RepeatRequested.IsRequested, Is.True);
            Assert.That(
                finished.RepeatRequested.Binding, Is.EqualTo(RepeatTargetBinding.DifferentTarget),
                "'on a different target' is the action's own state, and a Ranged action has one too.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The build's registration list plus the two declarations her series deliberately withholds -
        /// the catalog milestone 8.57g's dealing will make the registered one, and the same catalog
        /// <c>DealtCorpusCompositionValidationTests</c> validates the whole corpus against.
        /// </summary>
        private static EffectCapabilityCatalog Catalog()
        {
            var build = MatchContentCatalogs.ForThisBuild().Capabilities;
            return new EffectCapabilityCatalog(
                build.Registered.Select(build.Load)
                    .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
        }

        private static MatchContentCatalogs DealingCatalogs() =>
            new MatchContentCatalogs(Catalog(), SyntheticComponentDefinitions.CreateCatalog());

        private static void AssertSuccessors(
            CardDefinitionId cardId, params CardDefinitionId[] expected) =>
            Assert.That(
                RowennaCardContent.Definition(cardId).Progression.NextTierCandidateIds,
                Is.EquivalentTo(expected), cardId.Value);

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
        /// The amount a face's SECONDARY icon carries, or "-" where it prints none. RO-05 is the whole
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
        /// Her shard's printed-inventory table, parsed into rows. The colour cell carries RO-03's
        /// conflict in parentheses on four faces and the Handicap's Tier cell reads "Handicap", both of
        /// which are normalized here to the words the progression record uses - and nowhere else, so a
        /// change to either is visible.
        /// </summary>
        private static IReadOnlyList<InventoryRow> InventoryRows()
        {
            var rows = new List<InventoryRow>();
            foreach (var line in ShardLines())
            {
                if (!line.StartsWith("| rowenna", StringComparison.Ordinal))
                {
                    continue;
                }

                var cells = line.Split('|').Select(cell => cell.Trim()).ToArray();
                var face = cells[2].Split('/').Select(part => part.Trim()).ToArray();
                var colour = face[1]
                    .Replace(" (corner G, RO-03)", string.Empty)
                    .Replace(" (corner B, RO-03)", string.Empty);
                var tier = face[2];

                rows.Add(new InventoryRow(
                    cells[1],
                    face[0],
                    colour == "Purple" ? "Ultimate IV" : tier == "Basic" || tier == "Handicap"
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
        /// Her shard's "Exact printed rules" list, keyed by face name. A bullet naming two faces with a
        /// slash is one sentence printed on two cards, so it is recorded under both names; the Ultimate
        /// and the Handicap name their role in parentheses, which is stripped.
        ///
        /// <para>Only that section is read. The shard's open-verification list below it is bulleted the
        /// same way - "- **RO-Q1 (user):**" and its five siblings - and swallowing those would turn a
        /// count of nineteen into a count of twenty-five and quietly weaken the case.</para>
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
            File.ReadAllLines(Path.Combine(RepositoryRoot(), "Docs/AI/Rules/HERO_ROWENNA.md"));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null &&
                !File.Exists(Path.Combine(directory.FullName, "Docs/AI/Rules/HERO_ROWENNA.md")))
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
                "Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Rowenna");
            return Directory.Exists(path) ? path : null;
        }

        // ----- a dealt hand on a staged board -----

        /// <summary>
        /// One round with Rowenna's dealt eighteen in her seat, an enemy hero beside her and an ally
        /// beside the enemy - the smallest board on which every card p.6 puts in her hand has a legal
        /// subject.
        ///
        /// <para>The two other seats commit a synthetic fixture card of Initiative 1 so that hers is
        /// always the highest committed Initiative and the turn is always hers: every one of her five
        /// starting cards prints 4 or more, so the ordering is a property of the board rather than of
        /// which card the case is driving.</para>
        /// </summary>
        private sealed class TurnFixture
        {
            private static readonly CardDefinition Opponent = new CardDefinition(
                new CardDefinitionId("rowenna-printed-set-opponent"),
                "rev-1",
                "TEST - Low initiative",
                new InitiativeValue(1),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("fixture-move"),
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                });

            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] cards;
            private readonly CardInstance source;
            private readonly ActionDefinition primary;
            private readonly RoundResolutionAuthority round;

            internal TurnFixture(CardDefinitionId cardId)
            {
                definitions = RowennaCardContent.AllDefinitions
                    .Concat(new[] { Opponent })
                    .ToArray();

                var hers = RowennaCardContent.CreateDealtCards(Rowenna, "rowenna-turn").ToArray();
                cards = hers
                    .Concat(new[]
                    {
                        CardInstance.CreateInHand(
                            new CardInstanceId("enemy-source"), Opponent.Reference, Enemy),
                        CardInstance.CreateInHand(
                            new CardInstanceId("ally-source"), Opponent.Reference, Ally)
                    })
                    .ToArray();

                source = cards.Single(card =>
                    card.OwnerId.Equals(Rowenna) &&
                    card.DefinitionReference.DefinitionId.Equals(cardId));
                primary = RowennaCardContent.Definition(cardId).ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

                var map = BoardMapDefinition.CreateSynthetic(
                    new MapId("rowenna-printed-set-map"),
                    1,
                    Enumerable.Range(-6, 13).SelectMany(q =>
                        Enumerable.Range(-6, 13).Select(r => new HexCoordinate(q, r))));
                var board = new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(Ally, TwoAway)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                var markers = new MarkerAttachmentRegister();
                round = new RoundResolutionAuthority(
                    definitions,
                    cards,
                    participants,
                    new TieBreakerState(TeamSide.Red),
                    1,
                    RoundResolutionComposition.ForMatch(
                        new CardActionResolutionAuthority(DealingCatalogs(), board, markers),
                        board,
                        new ActiveEffectLifecycle(),
                        markers,
                        null,
                        null,
                        startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));

                Assert.That(round.TryCommitCard(Rowenna, source.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(
                    Enemy, new CardInstanceId("enemy-source")).Accepted, Is.True);
                Assert.That(round.TryCommitCard(
                    Ally, new CardInstanceId("ally-source")).Accepted, Is.True);
            }

            internal ActionDefinitionId PrimaryActionId => primary.Id;

            internal ActionKind PrimaryKind => primary.Kind;

            internal ActionOffer PrimaryOffer =>
                ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, cards, Rowenna, source.Id)
                    .FirstOrDefault(offer => offer.ActionDefinitionId.Equals(primary.Id));

            internal IReadOnlyList<ActionOffer> OffersFor(CardDefinitionId cardId)
            {
                var card = cards.Single(candidate =>
                    candidate.OwnerId.Equals(Rowenna) &&
                    candidate.DefinitionReference.DefinitionId.Equals(cardId));
                return ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, cards, Rowenna, card.Id)
                    .ToArray();
            }

            internal RoundResolutionResult StartPrimary()
            {
                var offer = PrimaryOffer;
                var resolutionId = new ActionResolutionId("rowenna-printed-set");
                switch (primary.Kind)
                {
                    case ActionKind.Attack:
                        return round.TryStartAttack(resolutionId, offer);
                    case ActionKind.Movement:
                        return round.TryStartMovement(resolutionId, offer);
                    default:
                        return round.TryStartSkill(resolutionId, offer);
                }
            }
        }

        // ----- the Ultimate's rule, live -----

        /// <summary>
        /// Rowenna, one enemy hero and her whole set in card instances - the smallest world in which
        /// p.16's unlock condition can be met and its consequence read through the production queries.
        /// It is a compact twin of <c>RowennaFabledLanceTests</c>' own board, kept here rather than
        /// shared because what this fixture asks of it is one composition rather than the rule's whole
        /// surface.
        /// </summary>
        private sealed class UltimateFixture
        {
            private static readonly MapId Map = new MapId("rowenna-printed-set-ultimate-map");
            private static readonly ZoneId Zone = new ZoneId("rowenna-printed-set-ultimate-zone");

            private readonly MatchParticipants participants = StagedBoardParticipants.Create();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly CardDefinition[] definitions;
            private readonly BoardMapDefinition map;
            private readonly HeroItemState heroItems;

            internal UltimateFixture()
            {
                definitions = RowennaCardContent.AllDefinitions.ToArray();
                map = BuildMap();
                heroItems = new HeroItemState(participants);

                foreach (var cardId in RowennaCardContent.StartingHand)
                {
                    instances.Add(CardInstance.CreateInHand(
                        new CardInstanceId("hand-" + cardId.Value), Reference(cardId), Rowenna));
                }

                foreach (var cardId in RowennaCardContent.Deck)
                {
                    instances.Add(CardInstance.CreateInDeck(
                        new CardInstanceId("deck-" + cardId.Value), Reference(cardId), Rowenna));
                }
            }

            internal void Unlock()
            {
                foreach (var card in instances
                    .Where(card => card.State.Location == CardLocation.Hand)
                    .ToArray())
                {
                    var definition = definitions
                        .Single(candidate => candidate.Reference.Equals(card.DefinitionReference));
                    if (!definition.Progression.IsNonBasic)
                    {
                        continue;
                    }

                    while (definition.Progression.Tier != CardTier.III)
                    {
                        definition = CardCharacteristicQuery
                            .FindUpgradeCandidates(definition.Id, definitions)
                            .First();
                    }

                    instances[instances.IndexOf(card)] = new CardInstance(
                        card.Id, definition.Reference, card.OwnerId, card.State);
                }

                var ultimate = instances.Single(card =>
                    card.DefinitionReference.DefinitionId.Equals(RowennaCardContent.FabledLance));
                Assert.That(
                    UltimateUnlockQuery.TryUnlock(
                        ultimate,
                        UltimateUnlockQuery.UpgradeCompletionReading(Rowenna),
                        definitions,
                        instances).IsUnlocked,
                    Is.True,
                    "p.16's condition is every non-Basic card in hand at Tier III, and the walk above " +
                    "climbs each ladder through the production query.");
            }

            internal AttackRangeKind ClassificationOf(CardDefinitionId cardId) =>
                RangedAttackOverlayQuery.Classify(
                    Offer(cardId).AttackRange, Rowenna, Modifications(Board(EnemyAt)));

            internal IReadOnlyList<HeroId> TargetsOf(CardDefinitionId cardId, HexCoordinate enemyAt)
            {
                var board = Board(enemyAt);
                var modifications = Modifications(board);
                var offer = Offer(cardId);
                var action = definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(candidate => candidate.Id.Equals(offer.ActionDefinitionId));
                var classification = RangedAttackOverlayQuery.Classify(
                    offer.AttackRange, Rowenna, modifications);
                var rangeTotal = classification == AttackRangeKind.Ranged
                    ? RangeValueQuery.Calculate(offer, definitions, heroItems, modifications).Total
                    : 1;
                return AttackTargetQuery.FindLegalEnemyTargets(
                        Rowenna,
                        RangedAttackOverlayQuery.Overlay(
                            action.AttackTargetPolicy ??
                                PrintedAttackTargetPolicy.ForClassification(offer.AttackRange),
                            offer.AttackRange,
                            Rowenna,
                            modifications),
                        rangeTotal,
                        participants,
                        board,
                        modifications)
                    .Where(target => target.Kind == AttackTargetKind.Hero)
                    .Select(target => target.HeroId)
                    .ToArray();
            }

            private RuleModificationSet Modifications(AdjacentMoveAuthority board) =>
                RuleModificationQuery.Build(
                    null,
                    board,
                    participants,
                    new TieBreakerState(TeamSide.Red),
                    UltimateUnlockQuery.Derive(definitions, instances),
                    RuleObject.None,
                    heroItems,
                    null);

            /// <summary>
            /// The instance a case means when it names a face: the one in her HAND where the unlock's
            /// ladder walk has put it, and the deck's copy before that. Both exist at once past the
            /// unlock - a hand card whose definition climbed to Paragon of Grace and the deck's own
            /// Paragon of Grace - and an offer is about the card she would actually play.
            /// </summary>
            private CardInstanceId InstanceOf(CardDefinitionId cardId)
            {
                var matches = instances
                    .Where(card => card.DefinitionReference.DefinitionId.Equals(cardId))
                    .ToArray();
                Assert.That(matches, Is.Not.Empty, cardId.Value);
                return (matches.FirstOrDefault(card =>
                    card.State.Location == CardLocation.Hand) ?? matches[0]).Id;
            }

            private ActionOffer Offer(CardDefinitionId cardId) =>
                new ActionOffer(
                    InstanceOf(cardId),
                    Reference(cardId),
                    RowennaCardContent.PrimaryAttack,
                    Rowenna,
                    ActionInvocationContext.PrintedPrimary,
                    true,
                    AttackRangeKind.NonRanged,
                    default);

            private AdjacentMoveAuthority Board(HexCoordinate enemyAt) =>
                new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Enemy, enemyAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

            private CardDefinitionReference Reference(CardDefinitionId cardId) => definitions
                .Single(definition => definition.Id.Equals(cardId))
                .Reference;

            private static BoardMapDefinition BuildMap()
            {
                var spaces = new List<BoardMapSpace>();
                var index = 0;
                for (var q = -6; q <= 6; q++)
                {
                    for (var r = -6; r <= 6; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, RowennaAt) > 6)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            new SpaceId("s" + index++), coordinate,
                            BoardMapSpaceKind.Playable, Zone));
                    }
                }

                return new BoardMapDefinition(
                    Map, 1, "Rowenna Printed Set Ultimate Map",
                    spaces,
                    new[] { new BoardMapZone(Zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());
            }
        }

        // ----- one rider, run with the attack recorded as Ranged -----

        /// <summary>
        /// One of her after-attack riders walked at the effect frame, with the enclosing Attack
        /// recorded exactly as Fabled Lance's overlay makes it: <c>Ranged</c>, against a hero. The ally
        /// stands inside the card's printed Radius, which is the disk the rider reads and the one thing
        /// the Ultimate's Range does not touch.
        /// </summary>
        private sealed class RiderWorld
        {
            private readonly CardDefinitionId cardId;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly MatchParticipants participants;
            private readonly HeroCoinLedger ledger;
            private readonly MatchComponents components;
            private EffectResolutionContext context;
            private PendingResolution frame;

            private RiderWorld(CardDefinitionId cardId)
            {
                this.cardId = cardId;
                participants = StagedBoardParticipants.CreateWithBlueAlly();
                ledger = new HeroCoinLedger(participants);

                var pile = new CardDefinition(
                    new CardDefinitionId("rowenna-printed-set-pile-card"),
                    "rev-1",
                    "TEST - Rowenna Printed Set Pile Card",
                    new InitiativeValue(7),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NotStated,
                    new[]
                    {
                        new ActionDefinition(
                            new ActionDefinitionId("pile-movement"),
                            ActionKind.Movement,
                            ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                            new ActionDefinitionPolicy(
                                CardTextApplicationPolicy.Applies, false, true))
                    });

                definitions = RowennaCardContent.AllDefinitions.Concat(new[] { pile }).ToArray();
                instances = new[]
                {
                    new CardInstance(
                        new CardInstanceId("ally-discard-a"), pile.Reference, Ally,
                        CardStateFacts.DiscardedUnplayed())
                };

                components = BuildComponents();
                context = BuildContext();
            }

            internal static RiderWorld ForRangedAttack(CardDefinitionId cardId) =>
                new RiderWorld(cardId);

            internal int Coins(HeroId hero) => ledger.GetCoins(hero);

            internal PendingEffectChoice RunToPause()
            {
                var definition = RowennaCardContent.Definition(cardId);
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("rowenna-printed-set-rider"),
                    new CardInstanceId("rowenna-printed-set-source"),
                    definition.Reference,
                    RowennaCardContent.PrimaryAttack,
                    Rowenna,
                    Rowenna,
                    ActionInvocationContext.PrintedPrimary,
                    context,
                    RepeatContinuation.FirstInstance);

                Assert.That(result.Accepted, Is.True, "The rider must start.");
                Assert.That(result.IsComplete, Is.False, "and must ask who is paid.");
                frame = result.Frame;
                return result.Frame.PendingChoice;
            }

            internal PendingEffectChoice Answer(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal EffectResolutionStepResult Submit(EffectChoiceCommand command)
            {
                var result = EffectResolutionSession.Answer(frame, command, context);
                if (result.Accepted)
                {
                    frame = result.Frame;
                }

                return result;
            }

            private EffectResolutionContext BuildContext() =>
                new EffectResolutionContext(
                    RowennaEffectContent.AttackTextFor(cardId, RowennaCardContent.ContentRevision),
                    MatchContentCatalogs.ForThisBuild().Capabilities,
                    participants,
                    null,
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Attack,
                    components: components,

                    // The whole point of this world: the Attack the rider hangs off is the one Fabled
                    // Lance made, which is Ranged and was taken two spaces away.
                    enclosingAttack: new EnclosingAttackFacts(
                        Rowenna, Enemy, AttackRangeKind.Ranged),
                    rosterState: EffectRosterState.Of(
                        Array.Empty<RoundTurnSlot>(),
                        new RoundHeroDefeatHistory(participants),
                        ledger));

            private MatchComponents BuildComponents()
            {
                var zone = new ZoneId("rowenna-printed-set-rider-zone");
                var spaces = new List<BoardMapSpace>();
                var index = 0;
                for (var q = -6; q <= 6; q++)
                {
                    for (var r = -6; r <= 6; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, RowennaAt) > 6)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            new SpaceId("s" + index++), coordinate,
                            BoardMapSpaceKind.Playable, zone));
                    }
                }

                var map = new BoardMapDefinition(
                    new MapId("rowenna-printed-set-rider-map"), 1, "Rowenna Printed Set Rider Map",
                    spaces,
                    new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());

                var board = new AdjacentMoveAuthority(
                    map,
                    new[]
                    {
                        new HeroOccupancy(Rowenna, RowennaAt),
                        new HeroOccupancy(Enemy, TwoAway),
                        new HeroOccupancy(Ally, Beside)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());

                return new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board);
            }
        }
    }
}
