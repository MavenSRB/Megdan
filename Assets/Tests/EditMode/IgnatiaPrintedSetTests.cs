using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75c. Ignatia the Mad's printed set as a SET: nineteen faces, the five p.6 deals into
    /// her hand, the thirteen its deck keeps, the one p.6 returns to the box, the three coloured ladders
    /// p.15 climbs, the twelve items p.16 earns from the faces she does not choose, and the one revision
    /// every definition and every body carries.
    ///
    /// <para><b>What this fixture is for, and what it leaves to the six behaviour fixtures.</b>
    /// <c>IgnatiaCoinTests</c>, <c>IgnatiaAttackTests</c>, <c>IgnatiaPushTests</c>,
    /// <c>IgnatiaDiscardTests</c>, <c>IgnatiaSwapTests</c> and <c>IgnatiaMagmaTests</c> each prove what
    /// one group of her printed clauses DOES at an effect frame, and nothing here repeats one of those
    /// bodies. What none of them can say is anything about the set: the inventory order, the starting
    /// hand, the deck, the completed ladders, the boxed Handicap, and - the half a composition
    /// milestone exists to state - exactly which contracts the build still withholds and exactly which
    /// faces it therefore refuses.</para>
    ///
    /// <para><b>Nothing of hers is registered, and that is asserted rather than assumed.</b> Milestone
    /// 7.9's gate holds her content out of <see cref="MatchContentCatalogs"/> until her dealing block
    /// flips it, so the catalog these cases validate against is the build's registration list PLUS
    /// exactly the declarations her series deliberately withholds - 8.57f's precedent, which is what
    /// makes eighteen dealt faces sayable at a composition milestone at all. The gate is asserted from
    /// the other side too, by naming the exact set of capability references this build refuses and the
    /// exact faces that name one.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaPrintedSetTests
    {
        /// <summary>
        /// One row per face, in her shard's own table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item.
        ///
        /// <para>A dash is what the face prints nothing for: a missing icon is ABSENT, never zero, and
        /// an absent secondary is an action that does not exist rather than one whose value is 0. Her
        /// Silver prints Defense 7 and no Movement; her Skills print a Radius and no Range; her Attacks
        /// print a Range and no Radius; the Ultimate prints nothing at all.</para>
        ///
        /// <para>It is deliberately a literal table rather than something derived, for the reason
        /// <c>WaspVerifiedContentTests</c> and every set after it record: a test that derived its
        /// expectations from the definitions would pass whatever the definitions happened to say. This
        /// one was transcribed from the extracted fronts under
        /// <c>Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Ignatia</c>, and the
        /// stem-to-face case below maps every row of it back onto one of those files.</para>
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "ignatia-chaos-bolt|Chaos Bolt|Init 11|SecDef 2|SecMov 1|primary-ranged-attack Attack Ranged [Attack 2, Range 3]|Gold Basic|item -",
            "ignatia-equilibrium|Equilibrium|Init 1|SecDef 7|SecMov -|primary-skill Skill None []|Silver Basic|item -",
            "ignatia-playing-with-fire|Playing with Fire|Init 7|SecDef 4|SecMov 3|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red I|item -",
            "ignatia-erratic-fireblast|Erratic Fireblast|Init 7|SecDef 5|SecMov 3|primary-ranged-attack Attack Ranged [Attack 3, Range 3]|Red II|item Defense",
            "ignatia-crack-of-doom|Crack of Doom|Init 7|SecDef 6|SecMov 3|primary-ranged-attack Attack Ranged [Attack 5, Range 5]|Red II|item Initiative",
            "ignatia-loosely-aimed-firebolts|Loosely-Aimed Firebolts|Init 8|SecDef 5|SecMov 3|primary-ranged-attack Attack Ranged [Attack 4, Range 3]|Red III|item Radius",
            "ignatia-imminent-eruption|Imminent Eruption|Init 8|SecDef 6|SecMov 3|primary-ranged-attack Attack Ranged [Attack 6, Range 5]|Red III|item Initiative",
            "ignatia-abrupt-combustion|Abrupt Combustion|Init 9|SecDef 3|SecMov 3|primary-skill Skill None [Radius 3]|Blue I|item -",
            "ignatia-spontaneous-immolation|Spontaneous Immolation|Init 9|SecDef 4|SecMov 3|primary-skill Skill None [Radius 4]|Blue II|item Attack",
            "ignatia-searing-heat|Searing Heat|Init 9|SecDef 4|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Initiative",
            "ignatia-violent-conflagration|Violent Conflagration|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 4]|Blue III|item Range",
            "ignatia-scorching-blaze|Scorching Blaze|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Movement",
            "ignatia-path-of-ashes|Path of Ashes|Init 5|SecDef 2|SecMov 2|primary-skill Skill None [Radius 3]|Green I|item -",
            "ignatia-path-of-cinders|Path of Cinders|Init 5|SecDef 2|SecMov 2|primary-skill Skill None [Radius 4]|Green II|item Defense",
            "ignatia-unstable-portal|Unstable Portal|Init 5|SecDef 2|SecMov 2|primary-skill Skill None [Radius 4]|Green II|item Attack",
            "ignatia-path-of-flames|Path of Flames|Init 4|SecDef 3|SecMov 2|primary-skill Skill None [Radius 4]|Green III|item Defense",
            "ignatia-chaos-gate|Chaos Gate|Init 4|SecDef 3|SecMov 2|primary-skill Skill None [Radius 4]|Green III|item Attack",
            "ignatia-chaos-incarnate|Chaos Incarnate|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -",
            "ignatia-unreliable-jolt|Unreliable Jolt|Init 10|SecDef 1|SecMov 1|primary-ranged-attack Attack Ranged [Attack 1, Range 2]|Gold Basic|item -"
        };

        /// <summary>
        /// The extracted front each face was transcribed from, in the same order. The fronts live under
        /// a git-ignored directory, so a clean checkout has this table and not the images - the
        /// stem-to-file half of the sweep therefore runs only where the corpus is present and says so
        /// rather than passing silently.
        /// </summary>
        private static readonly string[] SourceStems =
        {
            "ignatia__7635279",
            "ignatia2__7694856",
            "ignatia3__7729010",
            "ignatia4__7729009",
            "ignatia5__7729008",
            "ignatia6__7729007",
            "ignatia7__7729004",
            "ignatia8__7641669",
            "ignatia9__7729011",
            "ignatia10__7729013",
            "ignatia11__7641666",
            "ignatia12__7641665",
            "ignatia13__7641664",
            "ignatia14__7641663",
            "ignatia15__7641662",
            "ignatia16__7641661",
            "ignatia17__7641660",
            "ignatia18__7590071",
            "ignatia19__7635273"
        };

        /// <summary>
        /// The ELEVEN capability references that are her own - the contracts her series minted and that no
        /// other hero's content names - as the build spells them, and the whole of what her dealing
        /// must register.
        ///
        /// <para>FIVE of them are new handler TYPES, so the dealing owes each a showcase consumer as
        /// well as a registration - the gate's second half. The remaining five are one
        /// <c>ActiveEffectCreationHandler</c> registration over Equilibrium's marker, one
        /// <c>PlaceHeroTokenHandler</c> registration over the magma token, and three more declarations
        /// of the ONE range-attack handler type, which is the "one implementation, many declarations"
        /// shape the printed-icon family already has.</para>
        /// </summary>
        private static readonly string[] HerOwnContracts =
        {
            "ignatia-coin-sun@1",
            "ignatia-enemy-hero-adjacent-to-magma-or-minion@1",
            "ignatia-enemy-minion-adjacent-to-enemy-hero@1",
            "ignatia-equilibrium@1",
            // Milestone 8.75a: Chaos Incarnate's flip, named by the Ultimate's TRIGGERED body alone.
            "ignatia-flip-tie-breaker@1",
            "ignatia-place-magma-in-radius@1",
            "ignatia-place-magma-wake@1",
            "ignatia-range-hero-not-adjacent@1",
            "ignatia-range-unit-at-maximum-range@1",
            "ignatia-range-unit-in-straight-line@1",
            "ignatia-range-unit-not-in-straight-line@1"
        };

        /// <summary>
        /// The handler TYPES her series minted, as an exact set. Derived from her declaration lists
        /// rather than written out, then compared against this table - so a type arriving unnoticed and
        /// a type that quietly stopped being hers both fail here, and the dealing knows exactly how
        /// many showcase consumers milestone 7.9's gate asks it for.
        /// </summary>
        private static readonly string[] HerOwnHandlerTypes =
        {
            "CoinSunHandler",
            "EnemyHeroAdjacentToMagmaOrMinionHandler",
            "EnemyMinionAdjacentToEnemyHeroHandler",
            "FlipTieBreakerHandler",
            "IgnatiaRangeAttackTargetHandler",
            "PlaceMagmaWakeHandler"
        };

        /// <summary>
        /// The faces this build REFUSES - NONE, since milestone 8.75b dealt her.
        ///
        /// <para>Before the dealing this table held ALL EIGHTEEN of her composed bodies, which was
        /// unlike every hero before her: her seventeen coin-branched faces each name
        /// <c>ignatia-coin-sun@1</c> at their very root and the eighteenth is Equilibrium, which
        /// names the marker that frees it, so not one of her composed faces was built from registered
        /// vocabulary alone. The list is kept as an empty table rather than deleted, because what it
        /// asserts is still the load-bearing claim: an emptiness this build can LOSE the day a later
        /// slice composes a face on a contract nobody registered.</para>
        /// </summary>
        private static readonly string[] FacesThisBuildRefuses = { };

        // -------------------------------------------------------------------------------------------
        // The printed inventory
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                IgnatiaCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the table milestone 8.75 " +
                "transcribed from the extracted faces. Correct the definition, not this table - the " +
                "table is the source.");
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
            Assert.That(IgnatiaCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(IgnatiaCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                IgnatiaCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { IgnatiaCardContent.UnreliableJolt }));
            Assert.That(
                IgnatiaCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(IgnatiaCardContent.UnreliableJolt),
                "and the dealt eighteen are the nineteen minus exactly that one.");

            var byColour = IgnatiaCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2),
                "Chaos Bolt and the boxed Unreliable Jolt.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1), "Equilibrium.");
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5), "The five card-text Ranged Attacks.");
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5),
                "Three discard faces and two pushes.");
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5), "Three magma paths and two swaps.");
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "Chaos Incarnate.");

            var ultimate = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate);
            Assert.That(ultimate.Progression.Classification, Is.EqualTo(CardClassification.Ultimate));
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "p.16's permanent ability prints no action at all, so she cannot even discard it to " +
                "defend.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);

            Assert.That(
                () => IgnatiaCardContent.Definition(new CardDefinitionId("ignatia-not-a-face")),
                Throws.ArgumentException,
                "and the set refuses an identity it does not print rather than answering null.");
        }

        /// <summary>
        /// p.6's five starting cards and the thirteen the deck keeps partition her dealt eighteen
        /// exactly, so no card is both and none is neither - and the five are one of each colour, which
        /// is the shape p.6 states rather than a fact about her in particular.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHerDealtEighteen()
        {
            Assert.That(IgnatiaCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(IgnatiaCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(
                IgnatiaCardContent.StartingHand.Intersect(IgnatiaCardContent.Deck), Is.Empty);
            Assert.That(
                IgnatiaCardContent.StartingHand.Concat(IgnatiaCardContent.Deck),
                Is.EquivalentTo(IgnatiaCardContent.DealtDefinitions.Select(card => card.Id)));

            Assert.That(IgnatiaCardContent.StartingHand, Is.EqualTo(new[]
            {
                IgnatiaCardContent.ChaosBolt,
                IgnatiaCardContent.Equilibrium,
                IgnatiaCardContent.PlayingWithFire,
                IgnatiaCardContent.AbruptCombustion,
                IgnatiaCardContent.PathOfAshes
            }));
            Assert.That(
                IgnatiaCardContent.StartingHand
                    .Select(id => IgnatiaCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.6 deals one of each colour, and hers are the Gold, the Silver and the three " +
                "Tier I.");

            Assert.That(
                IgnatiaCardContent.Deck
                    .Select(IgnatiaCardContent.Definition)
                    .Count(definition => definition.Progression.IsUltimate),
                Is.EqualTo(1),
                "p.16's card starts in the deck and is reached by the unlock rather than by a draw.");
            Assert.That(
                IgnatiaCardContent.Deck
                    .Select(IgnatiaCardContent.Definition)
                    .Where(definition => !definition.Progression.IsUltimate)
                    .Select(definition => definition.Progression.Tier)
                    .OrderBy(tier => tier),
                Is.EqualTo(new[]
                {
                    CardTier.II, CardTier.II, CardTier.II, CardTier.II, CardTier.II, CardTier.II,
                    CardTier.III, CardTier.III, CardTier.III, CardTier.III, CardTier.III, CardTier.III
                }),
                "and the other twelve are the six Tier II and the six Tier III faces.");

            // The whole set partitions three ways with nothing counted twice.
            Assert.That(
                IgnatiaCardContent.StartingHand
                    .Concat(IgnatiaCardContent.Deck)
                    .Concat(IgnatiaCardContent.ReturnedToBoxAtSetup)
                    .Distinct()
                    .Count(),
                Is.EqualTo(19));
        }

        // -------------------------------------------------------------------------------------------
        // The upgrade ladders and the items
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.15's three ladders, each forking twice: the Tier I offers both Tier IIs of its colour and
        /// EITHER Tier II offers both Tier IIIs, which is the shape every hero's deck has because a
        /// level-up deals the two cards of the next Tier in that colour.
        ///
        /// <para>Corrected at this block: the inventory pass named ONE successor apiece on the Blue and
        /// Green ladders - Spontaneous Immolation offering only Violent Conflagration, Searing Heat only
        /// Scorching Blaze, Unstable Portal only Chaos Gate and Path of Cinders only Path of Flames -
        /// which would have handed a levelling seat one card where the rulebook deals two. Her Red
        /// ladder already named both, and so does every one of the twenty-one heroes before her.</para>
        /// </summary>
        [Test]
        public void EveryUpgradePair_IsThePrintedLadder()
        {
            AssertSuccessors(
                IgnatiaCardContent.PlayingWithFire,
                IgnatiaCardContent.ErraticFireblast, IgnatiaCardContent.CrackOfDoom);
            AssertSuccessors(
                IgnatiaCardContent.ErraticFireblast,
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaCardContent.ImminentEruption);
            AssertSuccessors(
                IgnatiaCardContent.CrackOfDoom,
                IgnatiaCardContent.LooselyAimedFirebolts, IgnatiaCardContent.ImminentEruption);

            AssertSuccessors(
                IgnatiaCardContent.AbruptCombustion,
                IgnatiaCardContent.SpontaneousImmolation, IgnatiaCardContent.SearingHeat);
            AssertSuccessors(
                IgnatiaCardContent.SpontaneousImmolation,
                IgnatiaCardContent.ViolentConflagration, IgnatiaCardContent.ScorchingBlaze);
            AssertSuccessors(
                IgnatiaCardContent.SearingHeat,
                IgnatiaCardContent.ViolentConflagration, IgnatiaCardContent.ScorchingBlaze);

            AssertSuccessors(
                IgnatiaCardContent.PathOfAshes,
                IgnatiaCardContent.PathOfCinders, IgnatiaCardContent.UnstablePortal);
            AssertSuccessors(
                IgnatiaCardContent.PathOfCinders,
                IgnatiaCardContent.PathOfFlames, IgnatiaCardContent.ChaosGate);
            AssertSuccessors(
                IgnatiaCardContent.UnstablePortal,
                IgnatiaCardContent.PathOfFlames, IgnatiaCardContent.ChaosGate);

            foreach (var tierThree in IgnatiaCardContent.AllDefinitions
                .Where(definition => definition.Progression.Tier == CardTier.III))
            {
                AssertSuccessors(tierThree.Id);
            }

            // The four faces that sit on no ladder at all, and for three different printed reasons:
            // p.6 deals her Gold and Silver basics and p.15 never upgrades a basic, p.20 keeps the
            // Handicap in the box, and p.16 UNLOCKS the Ultimate rather than upgrading into it.
            foreach (var offLadder in new[]
            {
                IgnatiaCardContent.ChaosBolt,
                IgnatiaCardContent.Equilibrium,
                IgnatiaCardContent.ChaosIncarnate,
                IgnatiaCardContent.UnreliableJolt
            })
            {
                AssertSuccessors(offLadder);
                Assert.That(
                    IgnatiaCardContent.AllDefinitions.SelectMany(definition =>
                        definition.Progression.NextTierCandidateIds),
                    Has.No.Member(offLadder),
                    offLadder.Value + " is named by no upgrade pair either.");
            }

            // The shape, stated once over the whole set rather than pair by pair.
            foreach (var definition in IgnatiaCardContent.AllDefinitions
                .Where(candidate =>
                    candidate.Progression.Tier == CardTier.I ||
                    candidate.Progression.Tier == CardTier.II))
            {
                Assert.That(
                    definition.Progression.NextTierCandidateIds, Has.Count.EqualTo(2),
                    definition.Id.Value + " offers BOTH cards of the next Tier in its colour, which " +
                    "is what p.15's level-up deals.");
            }
        }

        /// <summary>
        /// p.16's items, read off the faces she does not choose: every one of the twelve Tier II and
        /// Tier III faces prints one, every Tier I, basic and the Ultimate print none, and the twelve
        /// are the six channels in the counts her printed table records.
        /// </summary>
        [Test]
        public void EveryUnchosenFace_BecomesItsPrintedItem()
        {
            foreach (var definition in IgnatiaCardContent.AllDefinitions)
            {
                var printsAnItem = definition.Progression.ItemIcon != PrintedValueChannel.None;
                var isUpgradeTier =
                    definition.Progression.Tier == CardTier.II ||
                    definition.Progression.Tier == CardTier.III;

                Assert.That(printsAnItem, Is.EqualTo(isUpgradeTier), definition.Id.Value +
                    ": an item is what the face she did NOT choose becomes, so exactly the twelve " +
                    "faces a level-up can leave behind print one.");
            }

            var byChannel = IgnatiaCardContent.AllDefinitions
                .Select(definition => definition.Progression.ItemIcon)
                .Where(channel => channel != PrintedValueChannel.None)
                .GroupBy(channel => channel)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byChannel[PrintedValueChannel.Initiative], Is.EqualTo(3),
                "Crack of Doom, Imminent Eruption and Searing Heat.");
            Assert.That(byChannel[PrintedValueChannel.Defense], Is.EqualTo(3),
                "Erratic Fireblast, Path of Cinders and Path of Flames.");
            Assert.That(byChannel[PrintedValueChannel.Attack], Is.EqualTo(3),
                "Spontaneous Immolation, Unstable Portal and Chaos Gate.");
            Assert.That(byChannel[PrintedValueChannel.Radius], Is.EqualTo(1),
                "Loosely-Aimed Firebolts.");
            Assert.That(byChannel[PrintedValueChannel.Range], Is.EqualTo(1),
                "Violent Conflagration.");
            Assert.That(byChannel[PrintedValueChannel.Movement], Is.EqualTo(1), "Scorching Blaze.");
            Assert.That(byChannel.Values.Sum(), Is.EqualTo(12));
        }

        /// <summary>
        /// A missing icon is an action or a printed value that does not exist, never a zero. The
        /// inventory table above says it row by row; this says it where a reader of the set would look
        /// for it, by the groups the set forms.
        /// </summary>
        [Test]
        public void AbsentIcons_StayAbsentRatherThanBecomingZero()
        {
            var silver = IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium);
            Assert.That(
                silver.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement),
                Is.False,
                "Her Silver prints Defense 7 and no Movement icon at all, so there is no Movement " +
                "action on it rather than one printing zero.");
            Assert.That(
                PrintedChannels(IgnatiaCardContent.Equilibrium), Is.Empty,
                "and its Skill prints neither Range nor Radius: \"This round: ...\" reaches nobody " +
                "on the board, so it has no disk to draw.");

            foreach (var attack in new[]
            {
                IgnatiaCardContent.ChaosBolt,
                IgnatiaCardContent.PlayingWithFire,
                IgnatiaCardContent.ErraticFireblast,
                IgnatiaCardContent.CrackOfDoom,
                IgnatiaCardContent.LooselyAimedFirebolts,
                IgnatiaCardContent.ImminentEruption,
                IgnatiaCardContent.UnreliableJolt
            })
            {
                Assert.That(
                    PrintedChannels(attack),
                    Is.EquivalentTo(new[] { PrintedValueChannel.Attack, PrintedValueChannel.Range }),
                    attack.Value + " prints an Attack value and a Range icon and NO Radius - and " +
                    "Loosely-Aimed Firebolts' Radius ITEM is a fact about somebody else's dashboard " +
                    "rather than about this action.");
                Assert.That(
                    IgnatiaCardContent.Definition(attack).ActionDefinitions
                        .Single(action => action.Kind == ActionKind.Attack).AttackRange,
                    Is.EqualTo(AttackRangeKind.Ranged));
            }

            foreach (var skill in IgnatiaCardContent.AllDefinitions
                .Where(definition => !definition.Progression.IsUltimate)
                .Where(definition => definition.ActionDefinitions
                    .Any(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                        action.Kind == ActionKind.Skill))
                .Where(definition => !definition.Id.Equals(IgnatiaCardContent.Equilibrium)))
            {
                Assert.That(
                    PrintedChannels(skill.Id),
                    Is.EquivalentTo(new[] { PrintedValueChannel.Radius }),
                    skill.Id.Value + " prints a Radius icon and no Range one.");
            }

            var ultimate = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate);
            Assert.That(ultimate.Initiative.IsDefined, Is.False,
                "and p.16's card prints no Initiative either, which is an undefined value rather " +
                "than a zero.");
            Assert.That(ultimate.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.None));
        }

        // -------------------------------------------------------------------------------------------
        // The two texts
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// HER IDENTITY, STATED OVER THE WHOLE SET. SEVENTEEN of her nineteen faces print TWO texts -
        /// a sun (☀) line and a moon (☾) line - and the coin picks which applies. The other two are the
        /// two that are ABOUT the coin rather than selected by it: Equilibrium, whose one sentence
        /// names both symbols as a free choice ("you may apply either ☀ or ☾ card text, regardless of
        /// the Tie Breaker coin"), and Chaos Incarnate, which FLIPS the coin and prints neither symbol.
        ///
        /// <para>No hero before her has a card with two texts at all, so this is the claim that says
        /// the transcription is of HER rather than of a set of ordinary faces.</para>
        /// </summary>
        [Test]
        public void SeventeenFacesPrintTwoTexts_AndTheTwoAboutTheCoinDoNot()
        {
            var withActions = IgnatiaCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions.Any(
                    action => action.Origin == ActionDefinitionOrigin.PrintedPrimary))
                .ToArray();

            Assert.That(withActions, Has.Length.EqualTo(18),
                "Eighteen faces print a primary action; the nineteenth is the Ultimate.");

            foreach (var definition in IgnatiaCardContent.AllDefinitions)
            {
                Assert.That(
                    definition.SourceContent.Kind,
                    Is.EqualTo(CardSourceContentKind.Transcribed),
                    definition.Id.Value + "'s sentence is transcribed from the printed face rather " +
                    "than paraphrased.");
            }

            var coinSelected = withActions
                .Where(definition => !definition.Id.Equals(IgnatiaCardContent.Equilibrium))
                .ToArray();
            Assert.That(coinSelected, Has.Length.EqualTo(17));

            foreach (var definition in coinSelected)
            {
                var text = definition.SourceContent.PrintedText;
                Assert.That(text, Does.Contain("☀"), definition.Id.Value + " prints a sun line.");
                Assert.That(text, Does.Contain("☾"), definition.Id.Value + " prints a moon line.");
            }

            var silver = IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium);
            Assert.That(
                silver.SourceContent.PrintedText,
                Does.Contain("regardless of the Tie Breaker coin"),
                "Her Silver is not one of the seventeen: it prints ONE sentence about the coin " +
                "rather than two lines the coin chooses between, which is why its body is the only " +
                "one of hers that is not coin-branched.");

            var ultimate = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate);
            Assert.That(ultimate.SourceContent.PrintedText, Does.Not.Contain("☀"));
            Assert.That(ultimate.SourceContent.PrintedText, Does.Not.Contain("☾"));
            Assert.That(
                ultimate.SourceContent.PrintedText,
                Does.Contain("flip the Tie Breaker coin"),
                "and it is the one face that MOVES the coin rather than reading it, which is why it " +
                "needs no symbols of its own.");
        }

        /// <summary>
        /// Which of rulebook p.14's moments each face's rider is applied at, as a set. Her seven
        /// Attacks carry step 1's, because naming the target is how it is bound at all; no Skill of hers
        /// declares a moment, because a Skill's text IS its action.
        /// </summary>
        [Test]
        public void EveryFacesPrintedMoment_IsTheOneItsOwnWordsName()
        {
            Assert.That(
                IgnatiaCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions
                        .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                        .Select(action => definition.DisplayName + " " + action.CardTextTiming))
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Chaos Bolt BeforeTheAttack",
                    "Equilibrium None",
                    "Playing with Fire BeforeTheAttack",
                    "Erratic Fireblast BeforeTheAttack",
                    "Crack of Doom BeforeTheAttack",
                    "Loosely-Aimed Firebolts BeforeTheAttack",
                    "Imminent Eruption BeforeTheAttack",
                    "Abrupt Combustion None",
                    "Spontaneous Immolation None",
                    "Searing Heat None",
                    "Violent Conflagration None",
                    "Scorching Blaze None",
                    "Path of Ashes None",
                    "Path of Cinders None",
                    "Unstable Portal None",
                    "Path of Flames None",
                    "Chaos Gate None",
                    "Unreliable Jolt BeforeTheAttack"
                }),
                "Eighteen primaries - the Ultimate prints none - each at the moment its own sentence " +
                "names.");

            foreach (var attack in IgnatiaCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Attack))
            {
                Assert.That(
                    attack.AttackTargetPolicy.HasValue &&
                    attack.AttackTargetPolicy.Value.Binding == AttackTargetBinding.ByCardText,
                    Is.True,
                    "Every Attack of hers reaches p.14's step 1 with NO target: the card names it, " +
                    "and the coin picks which of the two names applies.");
            }

            Assert.That(
                IgnatiaCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Any(action => action.SecondCardTextRoot != null),
                Is.False,
                "and no face of hers prints a SECOND rider: the two texts are two arms of one body, " +
                "not two moments.");

            Assert.That(
                IgnatiaCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.Origin != ActionDefinitionOrigin.PrintedPrimary)
                    .All(action =>
                        action.DefaultPolicy.CardTextApplication ==
                        CardTextApplicationPolicy.Suppressed),
                Is.True,
                "and p.11 suppresses every ribbon's own card text on every face of hers - the two " +
                "printed lines belong to the primary on all eighteen.");
        }

        // -------------------------------------------------------------------------------------------
        // The source corpus
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The source sweep: every composed face maps to exactly one <c>ignatia*__front.png</c> stem,
        /// and no stem is left over.
        ///
        /// <para>The fronts live under a git-ignored directory, so a clean checkout has the table and
        /// not the images. The stem-to-file half therefore runs only where the corpus is actually
        /// present, and says so rather than passing silently.</para>
        /// </summary>
        [Test]
        public void EveryFace_MapsToOneSourceFront()
        {
            Assert.That(SourceStems, Has.Length.EqualTo(19),
                "Nineteen printed faces and nineteen stems.");
            Assert.That(SourceStems.Distinct().Count(), Is.EqualTo(19),
                "and each face has a stem of its own - one front per definition, never shared.");

            foreach (var stem in SourceStems)
            {
                Assert.That(
                    stem.StartsWith("ignatia", StringComparison.Ordinal) && stem.Contains("__"),
                    Is.True,
                    stem + " is not a stem under her source directory.");
            }

            var corpus = SourceCorpusDirectory();
            if (corpus == null)
            {
                Assert.Pass(
                    "The stem-to-front half is skipped: this checkout does not carry the git-ignored " +
                    "Tabletopia corpus, so there is nothing on disk to map onto. The table above ran " +
                    "and is the claim that matters.");
                return;
            }

            for (var index = 0; index < SourceStems.Length; index++)
            {
                Assert.That(
                    File.Exists(Path.Combine(corpus, SourceStems[index] + "__front.png")), Is.True,
                    IgnatiaCardContent.AllDefinitions[index].DisplayName + " must resolve to " +
                    SourceStems[index] + "__front.png under the source directory.");
            }

            Assert.That(
                Directory.GetFiles(corpus, "*__front.png").Length, Is.EqualTo(19),
                "and the directory holds nineteen fronts and no twentieth - a bijection rather than " +
                "a containment, so an extracted face nobody composed fails here too.");
        }

        // -------------------------------------------------------------------------------------------
        // Legality, the deferral, and what this build refuses
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition,
        /// asked at the rulebook p.14 or p.11 moment the face itself prints - the check that would
        /// otherwise fault mid-resolution, on a player's turn, with the card already spent.
        ///
        /// <para>EIGHTEEN of the nineteen attach a body, which is one more than every hero before her:
        /// p.20's Handicap is composed here rather than deferred, because its two printed lines are the
        /// same two subjects her Gold basic's are with one word changed and there was nothing left to
        /// owe. The one face that composes none is p.16's Ultimate, whose printed content is a standing
        /// rule declaration.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogHerDealingWillRegister()
        {
            var catalog = IgnatiaBoard.Capabilities();
            var composed = 0;

            foreach (var definition in IgnatiaCardContent.AllDefinitions)
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

            Assert.That(composed, Is.EqualTo(18),
                "Eighteen bodies: her Gold basic, her Silver basic, the five Reds, the five Blues, " +
                "the five Greens and the boxed Handicap.");

            var rule = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate).UltimateRule;
            Assert.That(rule, Is.Not.Null);
            // Milestone 8.75a composed Chaos Incarnate: its flip-and-repeat is a triggered body on
            // Cutter's once-per-turn after-primary-action condition, owed to nobody any more.
            Assert.That(rule.TextComposition.IsAwaited, Is.False,
                "Chaos Incarnate's flip-and-repeat is composed (8.75a); nothing of hers is owed.");
            Assert.That(rule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerPerformsPrimaryAction));
            foreach (var body in IgnatiaEffectContent.TriggeredEffects(IgnatiaCardContent.ContentRevision))
            {
                Assert.That(EffectDefinitionValidator.Validate(body, catalog), Is.Empty,
                    "the Ultimate's triggered body '" + body.Id.Value + "' is legal against the " +
                    "catalog her dealing registers.");
            }
        }

        /// <summary>
        /// The only composition her set still owes is the Ultimate's. NO DEALT FACE of hers carries an
        /// engine deferral, which is what would grey a card out in a match.
        ///
        /// <para>p.20's Handicap is the one place this differs from every hero before her: her boxed
        /// face is COMPOSED rather than deferred, so <see cref="PrintedTextComposition.DeferredUntil"/>
        /// appears nowhere on her set at all. What p.20 still governs is whether the card is DEALT,
        /// which <see cref="IgnatiaCardContent.ReturnedToBoxAtSetup"/> states and which the case above
        /// measures.</para>
        /// </summary>
        [Test]
        public void NoFaceOfHers_OwesAnEngineDeferral_AndOnlyTheUltimateOwesAComposition()
        {
            foreach (var definition in IgnatiaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    Assert.That(action.TextComposition.IsAwaited, Is.False,
                        definition.Id.Value + " action '" + action.Id.Value + "' must owe nothing: " +
                        "an engine deferral on a face is a card a match would grey out.");
                    Assert.That(action.TextComposition.NamedTrigger, Is.Null,
                        definition.Id.Value + " action '" + action.Id.Value + "' declares no " +
                        "open-ended deferral either.");
                }
            }

            var handicap = IgnatiaCardContent.Definition(IgnatiaCardContent.UnreliableJolt);
            var primary = handicap.ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(primary.EffectRoot, Is.Not.Null,
                "Her Handicap's two printed lines are composed, which is what makes the p.20 " +
                "deferral every other hero's boxed face carries unnecessary on hers.");
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(primary.AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
            Assert.That(
                primary.AttackTargetPolicy.HasValue &&
                primary.AttackTargetPolicy.Value.TargetableUnits ==
                    AttackTargetableUnits.HeroesOnly,
                Is.True,
                "and BOTH of its lines name a hero, which is the one word p.20's face changes from " +
                "her Gold basic's.");

            Assert.That(
                IgnatiaCardContent.DealtDefinitions.SelectMany(d => d.ActionDefinitions)
                    .Count(action => action.EffectRoot != null),
                Is.EqualTo(17),
                "Seventeen of the eighteen composed bodies sit on DEALT faces; the eighteenth is the " +
                "boxed Handicap's.");
        }

        /// <summary>
        /// EXACTLY these ten capability references are her own, EXACTLY these eighteen faces are refused
        /// by this build, and her declaration lists hold the same ten - milestone 7.9's gate read from
        /// three sides at once, so that a contract arriving unnoticed, a face that quietly stopped
        /// asking for one, and a declaration nothing reaches each fail here.
        /// </summary>
        [Test]
        public void ExactlyTheseContractsAreHerOwn_AndThisBuildRefusesExactlyTheseFaces()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            var named = IgnatiaCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                // Milestone 8.75a: the Ultimate names its flip from its TRIGGERED body, which no
                // action carries - walk it beside the action bodies.
                .Concat(IgnatiaEffectContent.TriggeredEffects(IgnatiaCardContent.ContentRevision)
                    .SelectMany(body => body.Nodes))
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();

            Assert.That(
                named.Select(capability => capability.ToString())
                    .Where(value => value.StartsWith("ignatia-", StringComparison.Ordinal))
                    .Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(HerOwnContracts),
                "Exactly eleven of the contracts her bodies name are HERS (8.75a added the flip, named " +
                "by the Ultimate's triggered body) - a contract of hers that no body names, and a " +
                "twelfth one appearing, both fail here.");

            Assert.That(
                named.Count(capability =>
                    !capability.ToString().StartsWith("ignatia-", StringComparison.Ordinal)),
                Is.GreaterThan(0),
                "and her bodies genuinely reach the roster's own contracts too - the adjacent roster " +
                "reach, the printed-Range hero arm, the attack-target binding, the discard, the " +
                "Defeats, the Remove, the Swap, the straight-line offer and the direct Move - so the " +
                "case above is not passing because the walk found nothing.");

            Assert.That(
                HerDeclaredHandlers().Select(handler => handler.Reference.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(HerOwnContracts),
                "and the declaration lists and the walk over her bodies agree, which is what says " +
                "the list her dealing registers is neither short nor long by one.");
            Assert.That(
                HerDeclaredHandlers().Select(handler => handler.Reference).Distinct().Count(),
                Is.EqualTo(11),
                "each reference ONCE - 8.40j's BA-18, which is what a second concat would break.");

            Assert.That(
                IgnatiaComponentContent.RegisteredHandlers().Count(), Is.EqualTo(11),
                "MILESTONE 8.75b FLIPPED MILESTONE 7.9's GATE. All eleven are registered, which is " +
                "the one edit that turns her printed set into a hero a configured match can deal.");
            Assert.That(
                named.Where(capability =>
                        capability.ToString().StartsWith("ignatia-", StringComparison.Ordinal))
                    .All(capability => registered.IsRegistered(capability)),
                Is.True,
                "and every one of the eleven reaches production registration - read from the build's " +
                "own catalog rather than from her declaration list, so a registration that names a " +
                "contract no body reaches and a body that names an unregistered contract both fail.");

            var refusedFaces = new List<string>();
            foreach (var definition in IgnatiaCardContent.AllDefinitions)
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
                Is.EqualTo(FacesThisBuildRefuses),
                "NOT ONE of her composed bodies is refused by this build any more, which is exactly " +
                "what her dealing bought: before milestone 8.75b all eighteen were refused, because " +
                "seventeen name the coin predicate at their very ROOT and the eighteenth is the " +
                "Silver that frees it. The boxed Handicap is in this walk too, so p.20's face is " +
                "loadable the day an odd player count asks for it. Refused now: " +
                string.Join(", ", refusedFaces.OrderBy(value => value, StringComparer.Ordinal)));
        }

        /// <summary>
        /// The FIVE handler types her series minted, as an exact set - derived from her declaration
        /// lists by asking which handler implementations live beside her content rather than in the
        /// roster's shared vocabulary.
        ///
        /// <para>Each one owes a showcase consumer at the dealing, which is milestone 7.9's gate's
        /// second half; the other five registrations are reuses of types the build already ships, and
        /// owe nothing.</para>
        /// </summary>
        [Test]
        public void ExactlyFiveHandlerTypesAreHers_AndEachOwesAShowcaseConsumerAtTheDealing()
        {
            var minted = HerDeclaredHandlers()
                .Select(handler => handler.GetType())
                .Where(type => type.Namespace == typeof(IgnatiaCardContent).Namespace)
                .Select(type => type.Name)
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(minted, Is.EqualTo(HerOwnHandlerTypes));

            var reused = HerDeclaredHandlers()
                .Select(handler => handler.GetType())
                .Where(type => type.Namespace != typeof(IgnatiaCardContent).Namespace)
                .Select(type => type.Name)
                .Distinct()
                .ToArray();

            Assert.That(reused, Has.Length.EqualTo(2),
                "and exactly two of her ten registrations are reuses of types this build already " +
                "ships - Equilibrium's Active-effect creation and the magma radius Place - which is " +
                "why ten contracts cost five new types rather than ten.");

            Assert.That(
                HerDeclaredHandlers().Count(handler =>
                    handler.GetType().Name == "IgnatiaRangeAttackTargetHandler"),
                Is.EqualTo(4),
                "One of the five carries FOUR declarations - the two straight-line readings, the " +
                "maximum-range equality and the adjacency exclusion - which is the same " +
                "\"one implementation, many declarations\" shape the printed-icon family has.");
        }

        // -------------------------------------------------------------------------------------------
        // The revision, and the words her controls owe
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One revision string covers the whole set - every definition and every composed body. Her
        /// blocks carried their own placeholders between them, and no face or body may be left on one.
        /// </summary>
        [Test]
        public void OneRealRevision_CoversEveryDefinitionAndEveryBody()
        {
            Assert.That(IgnatiaCardContent.ContentRevision, Is.EqualTo("ignatia-8-75-rev-1"));
            Assert.That(IgnatiaCardContent.ContentSetId, Is.EqualTo("ignatia"));

            Assert.That(
                IgnatiaCardContent.AllDefinitions
                    .Select(definition => definition.ContentRevision).Distinct(),
                Is.EqualTo(new[] { IgnatiaCardContent.ContentRevision }));

            Assert.That(
                IgnatiaCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .Select(action => action.EffectRoot.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { IgnatiaCardContent.ContentRevision }),
                "and the printed bodies carry it too, which is the half a card-only check would " +
                "miss.");

            Assert.That(
                IgnatiaCardContent.AllDefinitions.All(definition =>
                    definition.Id.Value.StartsWith("ignatia-", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                IgnatiaCardContent.AllDefinitions
                    .Select(definition => definition.ContentAuthority).Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
        }

        /// <summary>
        /// The decision labels her dealt faces owe, measured against the controls they actually raise -
        /// DERIVED from the resolver's own rule rather than from a second list, which is what found the
        /// ten Brynn's 8.69c could not have known about.
        ///
        /// <para>Her whole set raises exactly TWO, and both are the printed "may repeat once" clauses
        /// on her two Tier III Reds. Nothing else of hers asks with words: every face is a
        /// <c>Conditional</c> on the coin rather than a p.10 <c>Choose</c>, so no modal arm is ever
        /// drawn, and every other pause her set opens is a selection whose own control names what it
        /// would pick.</para>
        /// </summary>
        [Test]
        public void HerDecisionLabels_AreExactlyTheControlsHerDealtFacesRaise()
        {
            var declarations = IgnatiaEffectContent.DecisionLabels().ToArray();
            Assert.That(declarations, Has.Length.EqualTo(2),
                "The two printed \"may repeat once\" clauses, and nothing else.");

            var required = DecisionLabelRequirementQuery
                .For(IgnatiaCardContent.DealtDefinitions, Catalogs())
                .ToArray();

            Assert.That(
                required.Select(requirement =>
                        requirement.CardId.Value + " / " + requirement.NodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(declarations.Select(declaration =>
                        declaration.CardId.Value + " / " + declaration.NodeId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)),
                "and the controls her DEALT faces raise are exactly the two she declares words for - " +
                "derived from the resolver's own rule, so her dealing's one line into " +
                "DecisionLabelCatalog cannot land a face short.");

            Assert.That(
                required.All(requirement => requirement.OpensConfirmOrDecline), Is.True,
                "BOTH of her controls are p.10's confirm-or-decline, and NOT ONE is a modal arm - " +
                "which is the shape her coin gives the whole set: the card picks the line, never the " +
                "player.");

            foreach (var declaration in declarations)
            {
                Assert.That(declaration.Label.Declined, Is.Not.Null.And.Not.Empty,
                    declaration.NodeId.Value + " opens a decline, so it owes words for it.");
                Assert.That(declaration.Label.Chosen.Length, Is.LessThanOrEqualTo(60),
                    declaration.NodeId.Value + " must fit the sixty-character bound the decision " +
                    "window draws in - milestone 8.69g's measurement on Brynn's traps.");
                Assert.That(declaration.Label.Declined.Length, Is.LessThanOrEqualTo(60),
                    declaration.NodeId.Value + "'s decline must fit it too.");
            }

            foreach (var declaration in declarations)
            {
                Assert.That(
                    DecisionLabelCatalog.TryGetLabel(
                        declaration.CardId, declaration.NodeId, out var label),
                    Is.True,
                    declaration.NodeId.Value + " IS in the catalog: every hero's table joins it at " +
                    "the milestone that DEALS them, and milestone 8.75b is hers.");
                Assert.That(label.Chosen, Is.EqualTo(declaration.Label.Chosen));
                Assert.That(label.Declined, Is.EqualTo(declaration.Label.Declined));
            }

            // Chaos Incarnate's two controls live on the TRIGGERED body a p.16 permanent ability
            // names by reference rather than on any dealt face, so the requirement walk above cannot
            // see them and they arrive as a second declaration list. The catalog holds both, which is
            // what the dealing's one line into `DecisionLabelCatalog` is for.
            foreach (var declaration in IgnatiaEffectContent.UltimateDecisionLabels())
            {
                Assert.That(
                    DecisionLabelCatalog.TryGetLabel(
                        declaration.CardId, declaration.NodeId, out _),
                    Is.True,
                    declaration.NodeId.Value + " is the Ultimate's own control and joins the catalog " +
                    "in the same edit.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every handler her series declares, across the four content classes that hold them. The
        /// dealing moves exactly this list into the registered-capability catalog.
        /// </summary>
        private static IEnumerable<IEffectCapabilityHandler> HerDeclaredHandlers() =>
            IgnatiaComponentContent.RegisteredHandlers();

        private static MatchContentCatalogs Catalogs() =>
            new MatchContentCatalogs(
                IgnatiaBoard.Capabilities(), MatchContentCatalogs.ForThisBuild().Components);

        private static void AssertSuccessors(
            CardDefinitionId cardId, params CardDefinitionId[] expected) =>
            Assert.That(
                IgnatiaCardContent.Definition(cardId).Progression.NextTierCandidateIds,
                Is.EquivalentTo(expected), cardId.Value);

        private static IEnumerable<PrintedValueChannel> PrintedChannels(CardDefinitionId cardId) =>
            IgnatiaCardContent.Definition(cardId).ActionDefinitions
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
        /// The amount a face's SECONDARY icon carries, or "-" where it prints none. A string, because
        /// an absent icon is an action that does not exist - a different statement from an action whose
        /// value is zero.
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

        /// <summary>
        /// The extracted fronts, found by walking up from the working directory - which reaches them
        /// from a worktree as well as from the main checkout, because the corpus is git-ignored and
        /// lives in exactly one of them. <c>null</c> where this checkout carries none.
        /// </summary>
        private static string SourceCorpusDirectory()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                var candidate = Path.Combine(
                    directory.FullName,
                    "Docs",
                    "Guards of Atlantis II - Tabletopia Cards",
                    "active-hero-cards",
                    "Ignatia");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
