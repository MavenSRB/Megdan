using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z28. Dodger the Warlock's nineteen printed faces as definition content, checked
    /// against the source inventory the read-only worker verified rather than against themselves.
    ///
    /// <para>The printed-value assertion below is deliberately a literal table in the inventory's own
    /// column order, for the reason the six verified sets before hers record: a test that derived its
    /// expectations from the definitions would pass whatever the definitions happened to say. This one
    /// fails if a single number, colour, Tier, icon or name drifts from
    /// <c>tmp/8-35d-8-35e-multiplayer-corrections/worker-hero-dodger/INVENTORY.md</c>, which is the
    /// document the extracted faces were transcribed into and which her governed shard is created
    /// from.</para>
    ///
    /// <para><b>Her edition answer is caveat-free and no assertion here can make it.</b> The
    /// second-printing rulebook illustrates two of her faces and both reproduce the extracted faces
    /// exactly - Burning Skull on printed p.12, and Vampiric Shield twice on printed p.14, once as the
    /// anatomy callout for the <c>+</c> notation and once in the worked example. There is no wording
    /// difference of any kind, which makes Dodger the second caveat-free edition after Brogan. The
    /// rulebook does not reproduce Darkest Ritual, so its printed <c>wtih</c> has no second source; it
    /// is transcribed as printed and asserted below as a literal.</para>
    /// </summary>
    public sealed class DodgerVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the inventory's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "dodger-dread-razor|Dread Razor|Init 12|SecDef 1|SecMov 1|primary-ranged-attack Attack Ranged [Attack 2, Range 2]|Gold Basic|item -",
            "dodger-rusty-razor|Rusty Razor|Init 11|SecDef 1|SecMov 1|primary-ranged-attack Attack Ranged [Attack 1, Range 2]|Gold Basic|item -",
            "dodger-death-trap|Death Trap|Init 7|SecDef 1|SecMov -|primary-skill Skill None [Radius 4]|Silver Basic|item -",
            "dodger-littlefinger-of-death|Littlefinger of Death|Init 9|SecDef 5|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 2]|Red I|item -",
            "dodger-finger-of-death|Finger of Death|Init 9|SecDef 6|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 3]|Red II|item Initiative",
            "dodger-burning-skull|Burning Skull|Init 9|SecDef 5|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red II|item Defense",
            "dodger-middlefinger-of-death|Middlefinger of Death|Init 10|SecDef 6|SecMov 4|primary-ranged-attack Attack Ranged [Attack 5, Range 3]|Red III|item Movement",
            "dodger-blazing-skull|Blazing Skull|Init 10|SecDef 5|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 2]|Red III|item Radius",
            "dodger-shield-of-decay|Shield of Decay|Init 10|SecDef -|SecMov 3|primary-defense Defense Value [Defense 3, Radius 2]|Blue I|item -",
            "dodger-vampiric-shield|Vampiric Shield|Init 10|SecDef -|SecMov 3|primary-defense Defense Value [Defense 4, Radius 2]|Blue II|item Initiative",
            "dodger-weakness|Weakness|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 4]|Blue II|item Attack",
            "dodger-aegis-of-doom|Aegis of Doom|Init 11|SecDef -|SecMov 3|primary-defense Defense Value [Defense 4, Radius 2]|Blue III|item Initiative",
            "dodger-enfeeblement|Enfeeblement|Init 11|SecDef 4|SecMov 3|primary-skill Skill None [Radius 4]|Blue III|item Attack",
            "dodger-dark-ritual|Dark Ritual|Init 3|SecDef 2|SecMov 2|primary-skill Skill None [Radius 3]|Green I|item -",
            "dodger-darker-ritual|Darker Ritual|Init 3|SecDef 2|SecMov 2|primary-skill Skill None [Radius 3]|Green II|item Defense",
            "dodger-necromancy|Necromancy|Init 3|SecDef 2|SecMov 2|primary-skill Skill None []|Green II|item Attack",
            "dodger-darkest-ritual|Darkest Ritual|Init 2|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green III|item Defense",
            "dodger-necromastery|Necromastery|Init 2|SecDef 3|SecMov 2|primary-skill Skill None [Radius 1]|Green III|item Range",
            "dodger-tide-of-darkness|Tide of Darkness|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                DodgerCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory the read-only " +
                "worker verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        /// <summary>
        /// Every printed sentence, verbatim. It is a separate case from the value table because the two
        /// fail for different reasons and a reader chasing one should not have to read the other.
        ///
        /// <para><b>Three of these strings look wrong and are not.</b> Darkest Ritual prints
        /// <c>wtih</c>, prints a capital <c>If</c> after its semicolon, and ends its reminder without a
        /// full stop where Weakness's reminder has one; both Gold cards print <c>battlezone</c> as one
        /// word where all nine other spawn-point faces print it as two. Under the ledger's authority
        /// hierarchy the face wins, and the standing "cards are the definition" ruling forbids
        /// normalising any of it.</para>
        /// </summary>
        [Test]
        public void EveryPrintedSentence_IsTranscribedExactlyAsThefaceCarriesIt()
        {
            var transcribed = DodgerCardContent.AllDefinitions
                .ToDictionary(
                    definition => definition.Id.Value,
                    definition => definition.SourceContent.PrintedText);

            Assert.That(transcribed["dodger-dread-razor"], Is.EqualTo(
                "Choose one — • Target a unit adjacent to you. • If you are adjacent to an empty " +
                "spawn point in the battlezone, target a unit in range."));
            Assert.That(transcribed["dodger-rusty-razor"], Is.EqualTo(
                "Choose one — • Target a hero adjacent to you. • If you are adjacent to an empty " +
                "spawn point in the battlezone, target a hero in range."));
            Assert.That(transcribed["dodger-death-trap"], Is.EqualTo(
                "An enemy hero in radius who is adjacent to an empty spawn point in the battle zone " +
                "discards a card, if able."));
            Assert.That(transcribed["dodger-littlefinger-of-death"], Is.EqualTo(
                "Choose one — • Target a unit adjacent to you. • Target a hero in range who has one " +
                "or more cards in the discard."));
            Assert.That(transcribed["dodger-finger-of-death"],
                Is.EqualTo(transcribed["dodger-littlefinger-of-death"]),
                "The Tier I and Tier II faces print the identical sentence and differ only in " +
                "Initiative, printed Range and item icon.");
            Assert.That(transcribed["dodger-burning-skull"], Is.EqualTo(
                "Target a unit in range. After the attack: Move up to 1 minion adjacent to you 1 " +
                "space to a space not adjacent to you."));
            Assert.That(transcribed["dodger-middlefinger-of-death"], Is.EqualTo(
                "Choose one or both (on different targets) — • Target a unit adjacent to you. • " +
                "Target a hero in range who has one or more cards in the discard."));
            Assert.That(transcribed["dodger-blazing-skull"], Is.EqualTo(
                "Target a unit in range. After the attack: Move each of up to 2 minions adjacent to " +
                "you 1 space to a space not adjacent to you."));
            Assert.That(transcribed["dodger-shield-of-decay"], Is.EqualTo(
                "+2 [Defense] Defense if there are 2 or more empty spawn points in radius in the " +
                "battle zone."));
            Assert.That(transcribed["dodger-vampiric-shield"],
                Is.EqualTo(transcribed["dodger-shield-of-decay"]),
                "The Tier I and Tier II shields print the identical sentence; only the printed " +
                "Defense value differs.");
            Assert.That(transcribed["dodger-aegis-of-doom"], Is.EqualTo(
                "+4 [Defense] Defense if there are 2 or more empty spawn points in radius in the " +
                "battle zone."));
            Assert.That(transcribed["dodger-weakness"], Is.EqualTo(
                "This turn: Enemy heroes in radius have -4 [Attack] Attack. ( They can still attack, " +
                "even with a negative attack value. )"));
            Assert.That(transcribed["dodger-enfeeblement"], Is.EqualTo(
                "This turn: Enemy heroes in radius have -6 [Attack] Attack and cannot repeat actions."),
                "Weakness prints the no-floor reminder and Enfeeblement does not, despite its -6 " +
                "making a negative result far likelier. Under the standing ruling that is reminder " +
                "text being clarification only, and nothing turns on its absence.");
            Assert.That(transcribed["dodger-dark-ritual"], Is.EqualTo(
                "If there are 2 or more empty spawn points in radius in the battle zone, gain 1 coin."));
            Assert.That(transcribed["dodger-darker-ritual"], Is.EqualTo(
                "If there are 2 or more empty spawn points in radius in the battle zone, gain 2 coins."));
            Assert.That(transcribed["dodger-necromancy"], Is.EqualTo(
                "Respawn a friendly minion in an empty friendly spawn point adjacent to you in the " +
                "battle zone."));
            Assert.That(transcribed["dodger-darkest-ritual"], Is.EqualTo(
                "If there are 2 or more empty spawn points in radius in the battle zone, gain 2 " +
                "coins; If you have Ultimate, gain +1 [Attack] Attack item. ( Use any spare card " +
                "from the box wtih the corresponding icon )"),
                "`wtih` is printed, the `If` after the semicolon is printed as a capital, and the " +
                "reminder ends without a full stop. All three are the face's, not this file's.");
            Assert.That(transcribed["dodger-necromastery"], Is.EqualTo(
                "Respawn a friendly minion in an empty friendly spawn point in radius in the battle " +
                "zone."));
            Assert.That(transcribed["dodger-tide-of-darkness"], Is.EqualTo(
                "While you are performing an action, all spaces count as if they were in the battle " +
                "zone and had a friendly minion spawn point."));
        }

        [Test]
        public void TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(DodgerCardContent.AllDefinitions, Has.Count.EqualTo(19),
                "p.6's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(DodgerCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(DodgerCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { DodgerCardContent.RustyRazor }),
                "p.6 returns the Handicap card to the box at setup.");

            Assert.That(
                DodgerCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    DodgerCardContent.DreadRazor,
                    DodgerCardContent.DeathTrap,
                    DodgerCardContent.LittlefingerOfDeath,
                    DodgerCardContent.ShieldOfDecay,
                    DodgerCardContent.DarkRitual
                }),
                "p.6 step 11: the three Tier I cards and the two basic cards with no Tier.");
            Assert.That(DodgerCardContent.Deck, Has.Count.EqualTo(13),
                "Six Tier II cards, six Tier III cards, and the Ultimate.");
            Assert.That(
                DodgerCardContent.StartingHand.Concat(DodgerCardContent.Deck)
                    .Concat(DodgerCardContent.ReturnedToBoxAtSetup).Distinct().Count(),
                Is.EqualTo(19),
                "Every face belongs to exactly one of the three memberships.");
        }

        /// <summary>
        /// The colour sets have p.15's standard shape exactly: one Gold basic, one Silver basic, one
        /// Tier I with two Tier II and two Tier III in each of Red, Blue and Green, and one Purple Tier
        /// IV Ultimate - plus p.20's Handicap card, which is a second Gold.
        /// </summary>
        [Test]
        public void TheColourSets_HaveThePrintedShape()
        {
            var byColour = DodgerCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2), "Dread Razor and the Handicap card.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1), "Death Trap.");
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5));

            foreach (var colour in new[] { CardColor.Red, CardColor.Blue, CardColor.Green })
            {
                var tiers = DodgerCardContent.AllDefinitions
                    .Where(definition => definition.Progression.Color == colour)
                    .GroupBy(definition => definition.Progression.Tier)
                    .ToDictionary(group => group.Key, group => group.Count());
                Assert.That(tiers[CardTier.I], Is.EqualTo(1), colour + " prints one Tier I card.");
                Assert.That(tiers[CardTier.II], Is.EqualTo(2), colour + " prints two Tier II cards.");
                Assert.That(tiers[CardTier.III], Is.EqualTo(2), colour + " prints two Tier III cards.");
            }
        }

        /// <summary>
        /// p.15 upgrades by colour and Tier rather than by named card, so each Tier I and Tier II face
        /// names the pair one Level Up offers - and the card not taken becomes the item p.16 tucks
        /// under the dashboard.
        /// </summary>
        [Test]
        public void EveryUpgradeCandidatePair_IsTheInventorysOwnPair()
        {
            var pairs = DodgerCardContent.AllDefinitions
                .Where(definition => definition.Progression.NextTierCandidateIds.Count > 0)
                .ToDictionary(
                    definition => definition.Id.Value,
                    definition => string.Join(
                        "+", definition.Progression.NextTierCandidateIds.Select(id => id.Value)));

            Assert.That(pairs, Is.EqualTo(new Dictionary<string, string>
            {
                ["dodger-littlefinger-of-death"] = "dodger-finger-of-death+dodger-burning-skull",
                ["dodger-finger-of-death"] = "dodger-middlefinger-of-death+dodger-blazing-skull",
                ["dodger-burning-skull"] = "dodger-middlefinger-of-death+dodger-blazing-skull",
                ["dodger-shield-of-decay"] = "dodger-vampiric-shield+dodger-weakness",
                ["dodger-vampiric-shield"] = "dodger-aegis-of-doom+dodger-enfeeblement",
                ["dodger-weakness"] = "dodger-aegis-of-doom+dodger-enfeeblement",
                ["dodger-dark-ritual"] = "dodger-darker-ritual+dodger-necromancy",
                ["dodger-darker-ritual"] = "dodger-darkest-ritual+dodger-necromastery",
                ["dodger-necromancy"] = "dodger-darkest-ritual+dodger-necromastery"
            }));
        }

        /// <summary>
        /// EVERY Attack she owns is Ranged - a first in the verified roster, and what her Sniper role is
        /// made of. It also means every one of her attacks is answerable by a "Block a ranged attack"
        /// card such as Wasp's three Green Blocks, which is a real consequence for any match that deals
        /// them together.
        /// </summary>
        [Test]
        public void EveryAttackSheOwns_IsRanged()
        {
            var attacks = DodgerCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Attack)
                .ToArray();

            Assert.That(attacks, Has.Length.EqualTo(7),
                "Both Gold cards and all five Red cards print an Attack primary.");
            Assert.That(
                attacks.Select(action => action.AttackRange).Distinct(),
                Is.EqualTo(new[] { AttackRangeKind.Ranged }),
                "There is no non-Ranged Attack anywhere in her set.");
        }

        /// <summary>
        /// p.14's printed <c>+</c> on a Defense value, declared on exactly the three faces that print
        /// one and on nothing else. The amounts are the cards' own; the condition is one query, because
        /// all three print the identical sentence.
        /// </summary>
        [Test]
        public void TheThreeConditionalDefenseValues_DeclareTheirOwnAmountAgainstOneQuery()
        {
            var declared = DodgerCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions
                    .SelectMany(action => action.RosterValueContributions
                        .Select(value => (Card: definition.Id.Value, Value: value))))
                .ToArray();

            Assert.That(
                declared.Select(entry => entry.Card),
                Is.EqualTo(new[]
                {
                    "dodger-shield-of-decay", "dodger-vampiric-shield", "dodger-aegis-of-doom"
                }),
                "Only the three Blue Defense primaries print a `+`, and each declares exactly one.");
            Assert.That(
                declared.Select(entry => entry.Value.AmountPerMatch),
                Is.EqualTo(new[] { 2, 2, 4 }),
                "Shield of Decay and Vampiric Shield print +2; Aegis of Doom prints +4.");
            Assert.That(
                declared.Select(entry => entry.Value.Channel).Distinct(),
                Is.EqualTo(new[] { PrintedValueChannel.Defense }));
            Assert.That(
                declared.Select(entry => entry.Value.Query).Distinct(),
                Is.EqualTo(new[]
                {
                    RosterValueQueryKind.TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius
                }),
                "All three print \"if there are 2 or more empty spawn points in radius in the battle " +
                "zone\", so all three read one query.");
        }

        /// <summary>
        /// The Defense primaries print their Radius icon beside the number, which is p.9's sentence -
        /// "Some primary actions have a Range or Radius icon and a value on the right side of the card"
        /// - and which is the disk their own text asks about. p.16's Radius item therefore widens the
        /// question these three cards ask, and it is the only channel that can.
        /// </summary>
        [Test]
        public void TheDefensePrimaries_PrintPageNinesRadiusIconBesideTheirValue()
        {
            foreach (var id in new[]
                     {
                         DodgerCardContent.ShieldOfDecay,
                         DodgerCardContent.VampiricShield,
                         DodgerCardContent.AegisOfDoom
                     })
            {
                var primary = Primary(DodgerCardContent.Definition(id));
                Assert.That(primary.Kind, Is.EqualTo(ActionKind.Defense));
                Assert.That(primary.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
                Assert.That(primary.DefenseResolution, Is.EqualTo(PrintedDefenseResolution.Value));
                Assert.That(
                    primary.BaseValues.Single(value =>
                        value.Channel == PrintedValueChannel.Radius).Amount,
                    Is.EqualTo(2),
                    id.Value + " prints Radius 2.");
                Assert.That(primary.EffectRoot, Is.Null,
                    "Their whole sentence is the value declaration, so there is no step to compose - " +
                    "the same reading Arien's three Duelist cards carry for their own declaration.");
                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.None));
                Assert.That(
                    primary.MinionDefenseContributions,
                    Is.EqualTo(MinionDefenseContributionPolicy.IncludeAll),
                    "No face of hers suppresses p.14's minion Defense modifiers.");
            }
        }

        /// <summary>
        /// Which faces compose and which owe a composition, as one list. It is asserted rather than
        /// left to a reader because the parked set is the whole of what this phase deliberately did not
        /// do, and a face that quietly gained or lost a composition would otherwise be invisible.
        /// </summary>
        [Test]
        public void TheParkedFaces_AreExactlyTheHandicapAndTheModalSecondSequence()
        {
            var owed = DodgerCardContent.AllDefinitions
                .Select(definition => new
                {
                    Card = definition.Id.Value,
                    Trigger = definition.ActionDefinitions
                        .Select(action => action.TextComposition.NamedTrigger)
                        .Concat(definition.UltimateRule == null
                            ? Enumerable.Empty<string>()
                            : new[] { definition.UltimateRule.TextComposition.NamedTrigger })
                        .FirstOrDefault(value => value != null)
                })
                .Where(entry => entry.Trigger != null)
                .ToDictionary(entry => entry.Card, entry => entry.Trigger);

            Assert.That(owed, Is.EqualTo(new Dictionary<string, string>
            {
                ["dodger-rusty-razor"] = "first odd-player-count configuration",
                ["dodger-middlefinger-of-death"] =
                    "the modal second-sequence seam - one Attack action driving two complete p.14 " +
                    "orders without being p.10's Repeat"
            }),
                "Two faces owe a composition, and neither owes an answer. p.20's Handicap card is " +
                "parked under the standing 2026-08-21 user decision. Middlefinger of Death is parked " +
                "on a seam rather than a question: the user's 2026-09-06 ruling on D-06 settled that " +
                "its two bullets resolve two complete p.14 orders, and the only shape in this build " +
                "that produces a second complete order is p.10's Repeat - which Enfeeblement, composed " +
                "in the same milestone, is ruled to forbid. Composing it as a repeat would hand one " +
                "card a restriction neither card prints. Every other face composes.");
        }

        /// <summary>
        /// A parked face parks whole. <c>PrintedTextComposition</c> refuses a deferral beside an
        /// attached effect root, and this is the other half of that statement read from the outside: no
        /// deferred face carries a composed body, and no composed face carries a deferral.
        /// </summary>
        [Test]
        public void NoParkedFace_ShipsHalfASentence()
        {
            foreach (var definition in DodgerCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (!action.TextComposition.IsAwaited) continue;

                    Assert.That(action.EffectRoot, Is.Null, definition.Id.Value);
                    Assert.That(action.SecondCardTextRoot, Is.Null, definition.Id.Value);
                    Assert.That(action.CardTextTiming, Is.EqualTo(CardTextTiming.None),
                        definition.Id.Value);
                    Assert.That(
                        action.AttackTargetPolicy.HasValue &&
                        action.AttackTargetPolicy.Value.TargetIsBoundByCardText,
                        Is.False,
                        definition.Id.Value + ": a deferred face composes no step-1 text for a target " +
                        "binding to be about, which is the disposition Tigerclaw's Nimble Strike " +
                        "records.");
                }
            }
        }

        /// <summary>
        /// The three modal Attacks whose bullets are the targeting sentence bind their target through
        /// card text at p.14's step 1 - and Middlefinger of Death, which prints the same shape, does
        /// not, because it is parked.
        /// </summary>
        [Test]
        public void TheComposedModalAttacks_BindTheirTargetAtPageFourteensStepOne()
        {
            foreach (var id in new[]
                     {
                         DodgerCardContent.DreadRazor,
                         DodgerCardContent.LittlefingerOfDeath,
                         DodgerCardContent.FingerOfDeath
                     })
            {
                var primary = Primary(DodgerCardContent.Definition(id));
                Assert.That(primary.AttackTargetPolicy.HasValue, Is.True, id.Value);
                Assert.That(
                    primary.AttackTargetPolicy.Value.TargetIsBoundByCardText, Is.True, id.Value);
                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack),
                    id.Value + " applies its bullets before the Attack Total is announced.");
                Assert.That(primary.EffectRoot, Is.Not.Null, id.Value);
                Assert.That(
                    primary.EffectRoot.Nodes.First(node =>
                        node.Id.Equals(primary.EffectRoot.RootNodeId)).Kind,
                    Is.EqualTo(EffectNodeKind.Choose),
                    id.Value + " composes p.10's modal as a chooser-owned node kind.");
            }
        }

        /// <summary>
        /// Rusty Razor keeps the printed narrowing its bullets carry - both name a hero - and drops only
        /// the card-text binding, which is composition rather than transcription.
        /// </summary>
        [Test]
        public void TheHandicapCard_KeepsItsPrintedNarrowingAndDropsOnlyItsBinding()
        {
            var primary = Primary(DodgerCardContent.Definition(DodgerCardContent.RustyRazor));

            Assert.That(primary.AttackTargetPolicy.HasValue, Is.True);
            Assert.That(
                primary.AttackTargetPolicy.Value.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesOnly),
                "Both of its bullets print \"a hero\" where Dread Razor's print \"a unit\".");
            Assert.That(primary.AttackTargetPolicy.Value.TargetIsBoundByCardText, Is.False);
            Assert.That(primary.EffectRoot, Is.Null);
        }

        /// <summary>
        /// The two step-7 riders shove minions out of her innermost ring, and the destination policy
        /// they name is the one this milestone minted for the sentence. The selection admits any
        /// minion, because the faces print no side-word.
        /// </summary>
        [Test]
        public void TheTwoSkullRiders_ShoveAnyAdjacentMinionOutOfHerRing()
        {
            foreach (var entry in new[]
                     {
                         (Card: DodgerCardContent.BurningSkull, Maximum: 1, Moves: 1),
                         (Card: DodgerCardContent.BlazingSkull, Maximum: 2, Moves: 2)
                     })
            {
                var primary = Primary(DodgerCardContent.Definition(entry.Card));
                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack),
                    entry.Card.Value + " prints \"After the attack:\".");
                Assert.That(
                    primary.AttackTargetPolicy.Value.TargetIsBoundByCardText, Is.False,
                    "\"Target a unit in range\" is p.13's own reading and needs no binding.");

                var nodes = primary.EffectRoot.Nodes;
                var selection = nodes.Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseTargetSetV1));
                Assert.That(
                    selection.Inputs.Single(input =>
                        input.ParameterName == "maximum").Literal.IntegerValue,
                    Is.EqualTo(entry.Maximum),
                    entry.Card.Value + " names \"up to " + entry.Maximum + "\".");
                Assert.That(
                    selection.Inputs.Single(input =>
                        input.ParameterName == SyntheticRosterCapabilities.RelationParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo((int)SourceTeamRelation.AnyTeam),
                    "The faces print \"minion\" with no side-word, and p.19 makes \"friendly\" the " +
                    "word that would have narrowed it.");

                var moves = nodes.Where(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3)).ToArray();
                Assert.That(moves, Has.Length.EqualTo(entry.Moves));
                foreach (var move in moves)
                {
                    Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                        "A named minion that cannot be shoved, and a minion never named at all, are " +
                        "the same silence under the sentence's own \"up to\".");
                    Assert.That(
                        move.Inputs.Single(input => input.ParameterName == "distance")
                            .Literal.IntegerValue,
                        Is.EqualTo(1),
                        "\"1 space\" exactly - the \"up to\" is over the minions, not the distance.");
                    Assert.That(
                        move.Inputs.Single(input => input.ParameterName ==
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName)
                            .Literal.IntegerValue,
                        Is.EqualTo((int)DirectMoveDestinationPolicy.NotAdjacentToActor),
                        "\"to a space not adjacent to you\".");
                }
            }
        }

        /// <summary>
        /// The four composed faces that read the board question, and which of the two reaches each one
        /// names. This is the case that fails if a ritual is ever pointed at the adjacency twin, or a
        /// Gold card at the printed-Radius one - which would silently make an item widen a clause it
        /// must not.
        /// </summary>
        [Test]
        public void EachSpawnPointClause_NamesTheReachItsOwnSentencePrints()
        {
            Assert.That(
                CapabilitiesNamedBy(DodgerCardContent.DreadRazor),
                Has.Member(SyntheticRosterCapabilities.EmptyMinionSpawnPointsAdjacentV1),
                "\"adjacent to an empty spawn point\" is a distance the sentence writes down, and the " +
                "card prints Range 2 with no Radius icon at all.");

            foreach (var ritual in new[]
                     {
                         DodgerCardContent.DarkRitual, DodgerCardContent.DarkerRitual
                     })
            {
                Assert.That(
                    CapabilitiesNamedBy(ritual),
                    Has.Member(SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1),
                    ritual.Value + " prints \"in radius\", which p.9 makes the printed icon's value.");
            }

            var deathTrap = DodgerCardContent.Definition(DodgerCardContent.DeathTrap);
            var selection = Primary(deathTrap).EffectRoot.Nodes.Single(node =>
                node.Capability.Equals(SyntheticTargetingCapabilities.RadiusHeroTargetV1));
            Assert.That(
                selection.Inputs.Single(input => input.ParameterName ==
                    SyntheticTargetingCapabilities.AdjacentToEmptyMinionSpawnPointParameterName)
                    .Literal.BooleanValue,
                Is.True,
                "Death Trap's clause qualifies the enemy hero being picked, not Dodger - so it is a " +
                "candidate narrowing rather than a gate, and no counting capability appears at all.");
            Assert.That(
                CapabilitiesNamedBy(DodgerCardContent.DeathTrap),
                Has.No.Member(SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1));
        }

        /// <summary>
        /// The two rituals gate their coin gain on the number the card prints, and the gate has no
        /// else-child - which is the user's RULING 3 composed: an "If X" clause whose condition is false
        /// simply skips and the card continues.
        /// </summary>
        [Test]
        public void TheTwoComposedRituals_GateTheirCoinsOnTheThresholdTheyPrint()
        {
            foreach (var entry in new[]
                     {
                         (Card: DodgerCardContent.DarkRitual, Coins: 1),
                         (Card: DodgerCardContent.DarkerRitual, Coins: 2)
                     })
            {
                var nodes = Primary(DodgerCardContent.Definition(entry.Card)).EffectRoot.Nodes;
                var count = nodes.Single(node => node.Capability.Equals(
                    SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1));
                Assert.That(
                    count.Inputs.Single(input =>
                        input.ParameterName == SyntheticRosterCapabilities.MinimumPresentParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo(2),
                    entry.Card.Value + " prints \"2 or more\".");

                var gate = nodes.Single(node => node.Kind == EffectNodeKind.Conditional);
                Assert.That(gate.ChildNodeIds, Has.Count.EqualTo(1),
                    "No else-child: RULING 3 makes a false condition skip, and the card continues.");

                var gain = nodes.Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.GainResourceV1));
                Assert.That(
                    gain.Inputs.Single(input => input.ParameterName == "amount").Literal.IntegerValue,
                    Is.EqualTo(entry.Coins));
            }
        }

        /// <summary>
        /// Death Trap is her only basic Skill, so any future card keying off "a basic skill" reads this
        /// action and no other - and it prints no Movement icon at all, which is a real constraint on
        /// play because the card sits in her opening hand.
        /// </summary>
        [Test]
        public void DeathTrap_IsHerOnlyBasicSkill_AndOffersNoMovement()
        {
            var basicSkills = DodgerCardContent.AllDefinitions
                .Where(definition => definition.Progression.IsBasic)
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Kind == ActionKind.Skill)
                .ToArray();
            Assert.That(basicSkills, Has.Length.EqualTo(1));

            var deathTrap = DodgerCardContent.Definition(DodgerCardContent.DeathTrap);
            Assert.That(
                deathTrap.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement),
                Is.False,
                "The Silver secondary block was verified four independent ways under the 2026-09-06 " +
                "user quality directive: one Defense icon of value 1, and no Movement icon. p.9 makes " +
                "a secondary available only where its icon is printed.");
            Assert.That(
                deathTrap.ActionDefinitions.Single(action =>
                    action.Kind == ActionKind.Defense).BaseValues
                    .Single(value => value.Channel == PrintedValueChannel.Defense).Amount,
                Is.EqualTo(1));
        }

        /// <summary>
        /// She owns no rules token and no marker: no face of hers names, places, gives or targets one,
        /// so this phase adds no component content at all - which ledger question 14 already rules is an
        /// answer rather than a gap.
        /// </summary>
        [Test]
        public void SheOwnsNoTokenAndNoMarker()
        {
            Assert.That(
                DodgerCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .SelectMany(action => action.EffectRoot.Nodes)
                    .Where(node => node.Capability.IsDefined)
                    .Select(node => node.Capability.CapabilityId.Value)
                    .Where(id => id.Contains("token") || id.Contains("marker")),
                Is.Empty);
        }

        /// <summary>
        /// Every definition carries the one content revision, which is what makes the set move together.
        /// </summary>
        [Test]
        public void EveryFace_CarriesTheOneContentRevision()
        {
            Assert.That(
                DodgerCardContent.AllDefinitions
                    .Select(definition => definition.ContentRevision).Distinct(),
                Is.EqualTo(new[] { DodgerCardContent.ContentRevision }));
            // Moved at milestone 8.35z28, which composed six faces the user's 2026-09-06 rulings
            // unparked. The bump owes no ConfiguredCardArtworkCatalog.json rows - milestone 8.35l's
            // trap - because that catalog names the six dealt heroes explicitly and no phase has dealt
            // her; her rows and her identity pin arrive at her dealing milestone.
            Assert.That(DodgerCardContent.ContentRevision, Is.EqualTo("dodger-8-35x-rev-2"));
            Assert.That(
                DodgerCardContent.AllDefinitions
                    .Select(definition => definition.ContentAuthority).Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
        }

        private static IEnumerable<EffectCapabilityReference> CapabilitiesNamedBy(CardDefinitionId id) =>
            DodgerCardContent.Definition(id).ActionDefinitions
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .ToArray();

        private static ActionDefinition Primary(CardDefinition definition) =>
            definition.ActionDefinitions
                .FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        private static string Describe(CardDefinition definition)
        {
            var primary = Primary(definition);
            var secondaryDefense = definition.ActionDefinitions
                .FirstOrDefault(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction);
            var secondaryMovement = definition.ActionDefinitions
                .FirstOrDefault(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary);

            var progression = definition.Progression;
            var colourAndTier = progression.IsUltimate
                ? "Ultimate IV"
                : progression.IsBasic
                    ? progression.Color + " Basic"
                    : progression.Color + " " + progression.Tier;

            return string.Join("|", new[]
            {
                definition.Id.Value,
                definition.DisplayName,
                "Init " + (definition.Initiative.IsDefined
                    ? definition.Initiative.Value.ToString()
                    : "-"),
                "SecDef " + Amount(secondaryDefense, PrintedValueChannel.Defense),
                "SecMov " + Amount(secondaryMovement, PrintedValueChannel.Movement),
                DescribePrimary(definition, primary),
                colourAndTier,
                "item " + (progression.HasItemIcon ? progression.ItemIcon.ToString() : "-")
            });
        }

        private static string DescribePrimary(CardDefinition definition, ActionDefinition primary)
        {
            if (primary == null)
            {
                return "- Ultimate " + Values(definition.UltimateRule.PrintedValues);
            }

            var subtype = primary.Kind == ActionKind.Attack
                ? primary.AttackRange.ToString()
                : primary.Kind == ActionKind.Defense
                    ? primary.DefenseResolution.ToString()
                    : "None";
            return primary.Id.Value + " " + primary.Kind + " " + subtype + " " +
                Values(primary.BaseValues);
        }

        private static string Values(IReadOnlyList<PrintedValueDefinition> values) =>
            "[" + string.Join(", ", values.Select(value => value.Channel + " " + value.Amount)) + "]";

        private static string Amount(ActionDefinition action, PrintedValueChannel channel) =>
            action == null
                ? "-"
                : action.BaseValues.Single(value => value.Channel == channel).Amount.ToString();
    }
}
