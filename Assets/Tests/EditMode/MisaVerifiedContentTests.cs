using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z29. Misa the Samurai's nineteen printed faces as definition content, checked
    /// against the source inventory the read-only worker verified rather than against themselves.
    ///
    /// <para>The printed-value assertion below is deliberately a literal table in the inventory's own
    /// column order, for the reason the seven verified sets before hers record: a test that derived its
    /// expectations from the definitions would pass whatever the definitions happened to say. This one
    /// fails if a single number, colour, Tier, icon or name drifts from
    /// <c>tmp/8-35d-8-35e-multiplayer-corrections/worker-hero-misa/INVENTORY.md</c>, which is the
    /// document the extracted faces were transcribed into and which her governed shard is created
    /// from.</para>
    ///
    /// <para><b>Her edition answer is "by convention only" and no assertion here can make it.</b> The
    /// second-printing rulebook names her nowhere, illustrates none of her faces, and prints none of
    /// her nineteen card names - the weakest edition evidence of the verified roster. What holds is
    /// every p.9/p.14/p.16/p.17/p.20/p.21 convention, and the deck-shape case below asserts exactly
    /// those: nothing here claims the rulebook corroborates a value.</para>
    /// </summary>
    public sealed class MisaVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the inventory's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "misa-watch-how-i-soar|Watch How I Soar|Init 12|SecDef 2|SecMov 1|primary-skill Skill None [Range 4]|Gold Basic|item -",
            "misa-leaf-on-the-wind|Leaf on the Wind|Init 11|SecDef 1|SecMov -|primary-skill Skill None [Range 4]|Gold Basic|item -",
            "misa-swoop-in|Swoop In|Init 6|SecDef 3|SecMov -|primary-skill Skill None [Radius 3]|Silver Basic|item -",
            "misa-challenge-accepted|Challenge Accepted|Init 9|SecDef 6|SecMov 3|primary-attack Attack NonRanged [Attack 5]|Red I|item -",
            "misa-matter-of-honor|Matter of Honor|Init 9|SecDef 7|SecMov 3|primary-attack Attack NonRanged [Attack 6]|Red II|item Initiative",
            "misa-power-shot|Power Shot|Init 9|SecDef 4|SecMov 3|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red II|item Defense",
            "misa-worthy-opponent|Worthy Opponent|Init 10|SecDef 7|SecMov 3|primary-attack Attack NonRanged [Attack 6]|Red III|item Movement",
            "misa-thunder-shot|Thunder Shot|Init 10|SecDef 4|SecMov 3|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red III|item Defense",
            "misa-sudden-breeze|Sudden Breeze|Init 10|SecDef 5|SecMov 1|primary-skill Skill None []|Blue I|item -",
            "misa-gust-of-wind|Gust of Wind|Init 10|SecDef 6|SecMov 1|primary-skill Skill None []|Blue II|item Attack",
            "misa-dash-and-slash|Dash and Slash|Init 10|SecDef 6|SecMov 1|primary-skill Skill None []|Blue II|item Defense",
            "misa-crushing-squall|Crushing Squall|Init 11|SecDef 6|SecMov 1|primary-skill Skill None []|Blue III|item Radius",
            "misa-death-from-above|Death from Above|Init 11|SecDef 6|SecMov 1|primary-skill Skill None []|Blue III|item Range",
            "misa-focus|Focus|Init 3|SecDef 3|SecMov 2|primary-skill Skill None []|Green I|item -",
            "misa-discipline|Discipline|Init 2|SecDef 3|SecMov 2|primary-skill Skill None []|Green II|item Initiative",
            "misa-living-tornado|Living Tornado|Init 2|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green II|item Attack",
            "misa-mastery|Mastery|Init 2|SecDef 4|SecMov 2|primary-skill Skill None []|Green III|item Initiative",
            "misa-storm-spirit|Storm Spirit|Init 2|SecDef 4|SecMov 2|primary-skill Skill None [Radius 3]|Green III|item Attack",
            "misa-power-overwhelming|Power Overwhelming|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                MisaCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory the read-only " +
                "worker verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        /// <summary>
        /// Every printed sentence, verbatim. It is a separate case from the value table because the two
        /// fail for different reasons and a reader chasing one should not have to read the other.
        ///
        /// <para>Misa prints no reminder text anywhere in the set - not one parenthesised italic line -
        /// so unlike the six earlier heroes there is no clarification here to keep apart from a rule.
        /// The Ultimate's two printed paragraphs are joined with a single space, exactly as every other
        /// printed line break in this build is.</para>
        /// </summary>
        [Test]
        public void EveryPrintedSentence_IsTranscribedExactlyAsTheFaceCarriesIt()
        {
            var transcribed = MisaCardContent.AllDefinitions
                .ToDictionary(
                    definition => definition.Id.Value,
                    definition => definition.SourceContent.PrintedText);

            Assert.That(transcribed["misa-watch-how-i-soar"], Is.EqualTo(
                "Choose one — • Place yourself into a space at maximum range. • Defeat a minion " +
                "adjacent to you."));
            Assert.That(transcribed["misa-leaf-on-the-wind"], Is.EqualTo(
                "Place yourself into a space at maximum range."));
            Assert.That(transcribed["misa-swoop-in"], Is.EqualTo(
                "Place yourself into a space in radius adjacent to two or more enemy units. If you " +
                "do, you may retrieve a discarded card."));
            Assert.That(transcribed["misa-challenge-accepted"], Is.EqualTo(
                "Target a unit adjacent to you. After the attack: This turn: Gain +2 [Defense] " +
                "Defense."));
            Assert.That(transcribed["misa-matter-of-honor"], Is.EqualTo(
                "Target a unit adjacent to you. After the attack: This turn: Gain +3 [Defense] " +
                "Defense."));
            Assert.That(transcribed["misa-worthy-opponent"], Is.EqualTo(
                "Target a unit adjacent to you. After the attack: This turn: Gain +5 [Defense] " +
                "Defense."));
            Assert.That(transcribed["misa-power-shot"], Is.EqualTo(
                "Target a unit in range. After the attack: If the target was at maximum range, you " +
                "may move it 1 space, to a space farther away from you."));
            Assert.That(transcribed["misa-thunder-shot"], Is.EqualTo(
                "Target a unit in range. After the attack: If the target is not adjacent to you, you " +
                "may move it 1 space, to a space farther away from you."));
            Assert.That(transcribed["misa-sudden-breeze"], Is.EqualTo(
                "Move up to 3 spaces in a straight line, ignoring obstacles; you may place an enemy " +
                "unit you moved through into a space adjacent to you."));
            Assert.That(transcribed["misa-gust-of-wind"], Is.EqualTo(
                "Move up to 4 spaces in a straight line, ignoring obstacles; you may place an enemy " +
                "unit you moved through into a space adjacent to you."));
            Assert.That(transcribed["misa-dash-and-slash"], Is.EqualTo(
                "Move up to 4 spaces in a straight line, ignoring obstacles; an enemy hero you moved " +
                "through discards a card, if able."));
            Assert.That(transcribed["misa-crushing-squall"], Is.EqualTo(
                "Move up to 5 spaces in a straight line, ignoring obstacles; you may place an enemy " +
                "unit you moved through into a space adjacent to you."));
            Assert.That(transcribed["misa-death-from-above"], Is.EqualTo(
                "Move up to 5 spaces in a straight line, ignoring obstacles; an enemy hero you moved " +
                "through discards a card, if able."));
            Assert.That(transcribed["misa-focus"], Is.EqualTo(
                "Next turn: Before you perform a primary action, you may move 1 space."));
            Assert.That(transcribed["misa-discipline"], Is.EqualTo(
                "Next turn: Before you perform a primary action, you may move up to 2 spaces."));
            Assert.That(transcribed["misa-mastery"], Is.EqualTo(
                "Next turn: Before you perform a primary action, you may move up to 3 spaces."));
            Assert.That(transcribed["misa-living-tornado"], Is.EqualTo(
                "Swap two units at maximum radius."));
            Assert.That(transcribed["misa-storm-spirit"], Is.EqualTo(
                "Swap two units in radius and at equal distance from you."));
            Assert.That(transcribed["misa-power-overwhelming"], Is.EqualTo(
                "Whenever you choose one, you may choose two different options instead, in any " +
                "order. Each time after you place yourself, an enemy hero adjacent to you discards a " +
                "card, if able."));
        }

        /// <summary>
        /// Rulebook p.5's deck shape, which Misa satisfies without exception: one Gold basic, one
        /// Silver basic, one Gold Handicap, one Tier I with two Tier II and two Tier III in each of
        /// Red, Blue and Green, and one Purple Tier IV Ultimate.
        ///
        /// <para>This is the whole of what her "by convention only" edition answer rests on, so it is
        /// asserted rather than assumed.</para>
        /// </summary>
        [Test]
        public void TheDeckShape_IsTheStandardNineteenFaceShape()
        {
            var byColour = MisaCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.IsUltimate
                    ? "Ultimate"
                    : definition.Progression.Color.ToString())
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(MisaCardContent.AllDefinitions, Has.Count.EqualTo(19));
            Assert.That(byColour["Gold"], Is.EqualTo(2), "The Gold basic and the Gold Handicap.");
            Assert.That(byColour["Silver"], Is.EqualTo(1));
            Assert.That(byColour["Red"], Is.EqualTo(5));
            Assert.That(byColour["Blue"], Is.EqualTo(5));
            Assert.That(byColour["Green"], Is.EqualTo(5));
            Assert.That(byColour["Ultimate"], Is.EqualTo(1));

            foreach (var colour in new[] { CardColor.Red, CardColor.Blue, CardColor.Green })
            {
                var tiers = MisaCardContent.AllDefinitions
                    .Where(definition => !definition.Progression.IsUltimate &&
                        definition.Progression.Color == colour)
                    .GroupBy(definition => definition.Progression.Tier)
                    .ToDictionary(group => group.Key, group => group.Count());
                Assert.That(tiers[CardTier.I], Is.EqualTo(1), colour + " prints one Tier I.");
                Assert.That(tiers[CardTier.II], Is.EqualTo(2), colour + " prints two Tier II.");
                Assert.That(tiers[CardTier.III], Is.EqualTo(2), colour + " prints two Tier III.");
            }
        }

        /// <summary>
        /// p.16's item icons, which Misa's twelve non-Tier-I upgrade cards carry. Her set is the first
        /// verified one to reach all six icons, and the counts below are the inventory's.
        ///
        /// <para>Two of them are the observation the inventory flagged for the ceremony: Crushing
        /// Squall mints a +1 Radius item and Death from Above a +1 Range item, and neither card prints
        /// the icon it becomes. That is ordinary - an item icon says what a card becomes when set
        /// aside, not what it uses - and it is asserted here so a reader who notices it finds it
        /// already answered.</para>
        /// </summary>
        [Test]
        public void TheTwelveItemIcons_CoverAllSixOfPage16s()
        {
            var icons = MisaCardContent.AllDefinitions
                .Where(definition => definition.Progression.HasItemIcon)
                .GroupBy(definition => definition.Progression.ItemIcon)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(icons.Values.Sum(), Is.EqualTo(12));
            Assert.That(icons[PrintedValueChannel.Initiative], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Defense], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Attack], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Movement], Is.EqualTo(1));
            Assert.That(icons[PrintedValueChannel.Range], Is.EqualTo(1));
            Assert.That(icons[PrintedValueChannel.Radius], Is.EqualTo(1));

            Assert.That(
                MisaCardContent.Definition(MisaCardContent.CrushingSquall).Progression.ItemIcon,
                Is.EqualTo(PrintedValueChannel.Radius),
                "Crushing Squall mints a Radius item while printing no Radius icon of its own.");
            Assert.That(
                MisaCardContent.Definition(MisaCardContent.CrushingSquall).ActionDefinitions
                    .Any(action => action.BaseValues.Any(value =>
                        value.Channel == PrintedValueChannel.Radius)),
                Is.False);
        }

        /// <summary>
        /// p.15's upgrade pairs, colour by colour and Tier by Tier. Each Tier I and Tier II card names
        /// the two cards its Level Up offers, and the one not taken becomes p.16's item.
        /// </summary>
        [Test]
        public void EveryUpgradePair_IsTheInventorysPair()
        {
            AssertPair(MisaCardContent.ChallengeAccepted,
                MisaCardContent.MatterOfHonor, MisaCardContent.PowerShot);
            AssertPair(MisaCardContent.MatterOfHonor,
                MisaCardContent.WorthyOpponent, MisaCardContent.ThunderShot);
            AssertPair(MisaCardContent.PowerShot,
                MisaCardContent.WorthyOpponent, MisaCardContent.ThunderShot);

            AssertPair(MisaCardContent.SuddenBreeze,
                MisaCardContent.GustOfWind, MisaCardContent.DashAndSlash);
            AssertPair(MisaCardContent.GustOfWind,
                MisaCardContent.CrushingSquall, MisaCardContent.DeathFromAbove);
            AssertPair(MisaCardContent.DashAndSlash,
                MisaCardContent.CrushingSquall, MisaCardContent.DeathFromAbove);

            AssertPair(MisaCardContent.Focus,
                MisaCardContent.Discipline, MisaCardContent.LivingTornado);
            AssertPair(MisaCardContent.Discipline,
                MisaCardContent.Mastery, MisaCardContent.StormSpirit);
            AssertPair(MisaCardContent.LivingTornado,
                MisaCardContent.Mastery, MisaCardContent.StormSpirit);

            foreach (var tierThree in new[]
            {
                MisaCardContent.WorthyOpponent, MisaCardContent.ThunderShot,
                MisaCardContent.CrushingSquall, MisaCardContent.DeathFromAbove,
                MisaCardContent.Mastery, MisaCardContent.StormSpirit
            })
            {
                Assert.That(
                    MisaCardContent.Definition(tierThree).Progression.NextTierCandidateIds,
                    Is.Empty,
                    "A Tier III card is the top of its colour and offers no further upgrade.");
            }
        }

        /// <summary>
        /// p.6 step 11's starting hand and the thirteen cards the deck keeps, plus p.5's Handicap card
        /// which setup returns to the box.
        /// </summary>
        [Test]
        public void TheDealtSetup_IsPage6s()
        {
            Assert.That(MisaCardContent.StartingHand, Is.EquivalentTo(new[]
            {
                MisaCardContent.WatchHowISoar,
                MisaCardContent.SwoopIn,
                MisaCardContent.ChallengeAccepted,
                MisaCardContent.SuddenBreeze,
                MisaCardContent.Focus
            }), "The three Tier I cards plus the two untiered basic cards.");

            Assert.That(MisaCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(MisaCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { MisaCardContent.LeafOnTheWind }));
            Assert.That(MisaCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(
                MisaCardContent.DealtDefinitions.Any(definition =>
                    definition.Id.Equals(MisaCardContent.LeafOnTheWind)),
                Is.False,
                "The Handicap card is inventory rather than deck content.");
            Assert.That(
                MisaCardContent.StartingHand.Concat(MisaCardContent.Deck).Distinct().Count(),
                Is.EqualTo(18));
        }

        /// <summary>
        /// The headline observation of her source inventory, asserted rather than left in a document:
        /// <b>Misa prints no basic Attack anywhere.</b> Her Gold basic, her Silver basic and her Gold
        /// Handicap are all Basic Skills, which is a first for the verified roster.
        ///
        /// <para>Its consequence is the reason the assertion exists rather than the observation alone.
        /// Any rule that reads "after you perform a basic attack" can never fire for Misa, and any
        /// fixture, gate table or offer assertion that assumes a hero's Gold card yields a basic Attack
        /// will meet its first counterexample in her. This case is the cheap early warning the
        /// capability forecast asked for.</para>
        /// </summary>
        [Test]
        public void NoBasicCardOfHers_PrintsAnAttack()
        {
            var basics = MisaCardContent.AllDefinitions
                .Where(definition => definition.Progression.IsBasic)
                .ToArray();

            Assert.That(basics, Has.Length.EqualTo(3),
                "Two Gold cards - the basic and the Handicap - and one Silver.");
            Assert.That(
                basics.SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .Select(action => action.Kind)
                    .Distinct(),
                Is.EqualTo(new[] { ActionKind.Skill }),
                "Every primary on a basic card of Misa's is a Skill.");

            Assert.That(
                MisaCardContent.AllDefinitions
                    .Where(definition => definition.ActionDefinitions.Any(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                        action.Kind == ActionKind.Attack))
                    .All(definition => definition.Progression.Color == CardColor.Red),
                Is.True,
                "Her only Attacks are the five non-basic Red cards.");
        }

        /// <summary>
        /// Every Red card is an Attack, every Blue and Green card is a Skill, and there is no Defense
        /// primary and no p.14 Block anywhere in the set - the same shape Sabina has, recorded here as
        /// an absence that is content.
        /// </summary>
        [Test]
        public void TheColourSplit_IsAttacksInRedAndSkillsEverywhereElse()
        {
            foreach (var definition in MisaCardContent.AllDefinitions
                .Where(definition => !definition.Progression.IsUltimate))
            {
                var primary = definition.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                var expected = definition.Progression.Color == CardColor.Red
                    ? ActionKind.Attack
                    : ActionKind.Skill;
                Assert.That(primary.Kind, Is.EqualTo(expected), definition.Id.Value);
            }

            Assert.That(
                MisaCardContent.AllDefinitions.SelectMany(definition => definition.ActionDefinitions)
                    .Any(action => action.Kind == ActionKind.Defense &&
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary),
                Is.False,
                "She prints no Defense primary and no Block.");
        }

        /// <summary>
        /// The one face whose composition is parked, and the named trigger it is parked on. Everything
        /// else in her set composes; the standing p.20 Handicap deferral holds Leaf on the Wind alone.
        /// </summary>
        [Test]
        public void OnlyTheHandicapCard_HasAnAwaitedComposition()
        {
            var awaited = MisaCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions
                    .Select(action => new { definition, action }))
                .Where(pair => pair.action.TextComposition.IsAwaited)
                .ToArray();

            Assert.That(awaited.Select(pair => pair.definition.Id.Value).Distinct(),
                Is.EqualTo(new[] { "misa-leaf-on-the-wind" }));
            Assert.That(awaited.Single().action.TextComposition.NamedTrigger,
                Is.EqualTo("first odd-player-count configuration"));
        }

        /// <summary>
        /// Every face carries one content revision, which is what the identity oracle measures a hero
        /// by, and every definition is authored rather than synthetic.
        /// </summary>
        [Test]
        public void TheWholeSet_MovesAsOneVerifiedContentIdentity()
        {
            Assert.That(
                MisaCardContent.AllDefinitions.Select(definition => definition.ContentRevision)
                    .Distinct(),
                Is.EqualTo(new[] { MisaCardContent.ContentRevision }));
            Assert.That(
                MisaCardContent.AllDefinitions.Select(definition => definition.ContentAuthority)
                    .Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }));
            Assert.That(MisaCardContent.ContentRevision, Does.StartWith("misa-"));
        }

        private static void AssertPair(
            CardDefinitionId lower, CardDefinitionId first, CardDefinitionId second) =>
            Assert.That(
                MisaCardContent.Definition(lower).Progression.NextTierCandidateIds,
                Is.EqualTo(new[] { first, second }),
                lower.Value + " offers the wrong Level Up pair.");

        private static string Describe(CardDefinition definition)
        {
            var primary = definition.ActionDefinitions
                .FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
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
