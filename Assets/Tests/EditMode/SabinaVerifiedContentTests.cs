using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.22. Sabina the Gunslinger's nineteen printed faces as definition content, checked
    /// against the source inventory milestone 8.18c verified rather than against themselves.
    ///
    /// The printed-value assertion below is deliberately a literal table in the shard's own column
    /// order, for the reason <see cref="WaspVerifiedContentTests"/>,
    /// <see cref="ArienVerifiedContentTests"/> and <see cref="BroganVerifiedContentTests"/> record: a
    /// test that derived its expectations from the definitions would pass whatever the definitions
    /// happened to say. This one fails if a single number, colour, Tier, icon or name drifts from
    /// <c>Docs/AI/Rules/HERO_SABINA.md</c>, which is the document the extracted faces were transcribed
    /// into.
    ///
    /// <para>Sabina's edition answer is the weakest of the four verified heroes and this file cannot
    /// improve it: no card of hers is illustrated anywhere in the second-printing rulebook, so unlike
    /// the three sets before hers there is no rulebook reproduction to check a face against. What can
    /// be pinned is what the faces themselves say, which is what this file does. The shard records the
    /// gap as question S-04.</para>
    ///
    /// <para><b>Her epithet appears nowhere in this file, deliberately.</b> Question S-01 was answered
    /// by user decision on 2026-08-25 in favour of the rulebook's <c>Sabina the Gunslinger</c>, and the
    /// answer governs a display label. Nothing in the content branches on it - the stable identity is
    /// <c>sabina</c> - so a test that asserted the epithet here would be asserting about the wrong
    /// layer.</para>
    /// </summary>
    public sealed class SabinaVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the shard's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "sabina-point-blank-shot|Point Blank Shot|Init 12|SecDef 2|SecMov 1|primary-ranged-attack Attack Ranged [Attack 2, Range 1]|Gold Basic|item -",
            "sabina-bang|Bang|Init 11|SecDef 1|SecMov 1|primary-attack Attack NonRanged [Attack 1]|Gold Basic|item -",
            "sabina-back-to-back|Back to Back|Init 8|SecDef 2|SecMov -|primary-skill Skill None [Radius 2]|Silver Basic|item -",
            "sabina-quickdraw|Quickdraw|Init 8|SecDef 6|SecMov 4|primary-ranged-attack Attack Ranged [Attack 2, Range 2]|Red I|item -",
            "sabina-gunslinger|Gunslinger|Init 9|SecDef 6|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red II|item Initiative",
            "sabina-shootout|Shootout|Init 9|SecDef 6|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red II|item Defense",
            "sabina-dead-shot|Dead Shot|Init 9|SecDef 7|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red III|item Movement",
            "sabina-bullet-hell|Bullet Hell|Init 9|SecDef 7|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red III|item Radius",
            "sabina-listen-up|Listen Up|Init 9|SecDef 3|SecMov 3|primary-skill Skill None [Radius 1]|Blue I|item -",
            "sabina-roger-roger|Roger Roger|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 2]|Blue II|item Attack",
            "sabina-steady-advance|Steady Advance|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 2]|Blue II|item Defense",
            "sabina-ready-and-waiting|Ready and Waiting|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 2]|Blue III|item Attack",
            "sabina-unwavering-resolve|Unwavering Resolve|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 2]|Blue III|item Defense",
            "sabina-troop-movement|Troop Movement|Init 4|SecDef 2|SecMov 2|primary-skill Skill None [Radius 2]|Green I|item -",
            "sabina-marching-orders|Marching Orders|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green II|item Attack",
            "sabina-close-support|Close Support|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green II|item Initiative",
            "sabina-path-to-victory|Path to Victory|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green III|item Range",
            "sabina-covering-fire|Covering Fire|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green III|item Initiative",
            "sabina-big-sodding-gun|Big Sodding Gun|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                SabinaCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.18c verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        [Test]
        public void TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(SabinaCardContent.AllDefinitions, Has.Count.EqualTo(19),
                "p.6's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(SabinaCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(SabinaCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { SabinaCardContent.Bang }),
                "p.6 returns the Handicap card to the box at setup.");

            Assert.That(
                SabinaCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    SabinaCardContent.PointBlankShot,
                    SabinaCardContent.BackToBack,
                    SabinaCardContent.Quickdraw,
                    SabinaCardContent.ListenUp,
                    SabinaCardContent.TroopMovement
                }),
                "p.6 step 11 deals the three Tier I cards and the two basic cards with no Tier.");

            Assert.That(
                SabinaCardContent.StartingHand
                    .Select(id => SabinaCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.15: \"exactly 5 cards, 1 of each color, in your hand\".");

            Assert.That(SabinaCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                SabinaCardContent.Deck.Concat(SabinaCardContent.StartingHand)
                    .Concat(SabinaCardContent.ReturnedToBoxAtSetup)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal),
                Is.EqualTo(SabinaCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal)),
                "Every face belongs to exactly one of the three places p.6 puts it.");

            foreach (var colour in new[] { CardColor.Red, CardColor.Green, CardColor.Blue })
            {
                var set = SabinaCardContent.AllDefinitions
                    .Where(definition => definition.Progression.Color == colour)
                    .ToArray();
                Assert.That(
                    set.Select(definition => definition.Progression.Tier).OrderBy(tier => tier),
                    Is.EqualTo(new[] { CardTier.I, CardTier.II, CardTier.II, CardTier.III, CardTier.III }),
                    "Each colour set is one Tier I, two Tier II and two Tier III; " + colour + " is not.");
            }
        }

        /// <summary>
        /// The supertype split the shard records without exception: two Gold cards and one Silver are
        /// basic, every other card is non-basic, every Red card is an Attack and every Blue and Green
        /// card is a Skill.
        ///
        /// <para>It is asserted rather than described because the absences are what make her set
        /// different. She prints no Defense primary, no Movement primary, no Block and no combined
        /// Defense/Skill card, so no text of hers ever applies while defending - and the one basic
        /// Attack a match deals is Ranged, which is a first for this build.</para>
        /// </summary>
        [Test]
        public void EveryRedCardIsAnAttack_EveryBlueAndGreenCardIsASkill_AndNoFacePrintsADefensePrimary()
        {
            foreach (var definition in SabinaCardContent.AllDefinitions)
            {
                var primary = Primary(definition);

                // The Ultimate is the one face with no primary at all, so it is the one face this
                // rule has nothing to say about - p.16 places it beside the dashboard.
                if (primary == null) continue;

                var colour = definition.Progression.Color;
                var expected = colour == CardColor.Red || colour == CardColor.Gold
                    ? ActionKind.Attack
                    : ActionKind.Skill;
                Assert.That(primary.Kind, Is.EqualTo(expected),
                    definition.Id.Value + " prints a " + expected + " primary on its face.");
            }

            Assert.That(
                SabinaCardContent.AllDefinitions.SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .Any(action => action.Kind == ActionKind.Defense ||
                        action.Kind == ActionKind.Movement),
                Is.False,
                "Sabina prints no Defense primary and no Movement primary at all.");

            Assert.That(
                SabinaCardContent.AllDefinitions.SelectMany(definition => definition.ActionDefinitions)
                    .Any(action => action.DefenseResolution == PrintedDefenseResolution.Block),
                Is.False,
                "and no Block, which is why no card text of hers is ever applied while defending.");

            // The first ranged basic attack in the verified inventory, and the melee Handicap card
            // beside it that question S-02 turns on. Both are asserted rather than described, because
            // the asymmetry is content: the Ultimate grants "+2 Range" to a basic attack, and one of
            // these two faces prints a Range to add it to.
            var gold = Primary(SabinaCardContent.Definition(SabinaCardContent.PointBlankShot));
            Assert.That(gold.AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
            Assert.That(
                gold.BaseValues.Any(value => value.Channel == PrintedValueChannel.Range), Is.True,
                "Point Blank Shot is `Basic Attack - Ranged` and prints a Range 1 icon.");

            var handicap = Primary(SabinaCardContent.Definition(SabinaCardContent.Bang));
            Assert.That(handicap.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged));
            Assert.That(
                handicap.BaseValues.Any(value => value.Channel == PrintedValueChannel.Range), Is.False,
                "Bang is a plain melee Basic Attack printing no Range icon, which is question S-02.");
        }

        /// <summary>
        /// p.15's upgrade pairs, read from the shard's own table rather than from the definitions, and
        /// then handed to the progression validator that a configured match runs.
        /// </summary>
        [Test]
        public void TheUpgradePairsArePageFifteens_AndTheWholeSetPassesTheProgressionValidator()
        {
            var expected = new[]
            {
                "sabina-quickdraw -> sabina-gunslinger, sabina-shootout",
                "sabina-gunslinger -> sabina-dead-shot, sabina-bullet-hell",
                "sabina-shootout -> sabina-dead-shot, sabina-bullet-hell",
                "sabina-listen-up -> sabina-roger-roger, sabina-steady-advance",
                "sabina-roger-roger -> sabina-ready-and-waiting, sabina-unwavering-resolve",
                "sabina-steady-advance -> sabina-ready-and-waiting, sabina-unwavering-resolve",
                "sabina-troop-movement -> sabina-marching-orders, sabina-close-support",
                "sabina-marching-orders -> sabina-path-to-victory, sabina-covering-fire",
                "sabina-close-support -> sabina-path-to-victory, sabina-covering-fire"
            };

            Assert.That(
                SabinaCardContent.AllDefinitions
                    .Where(definition => definition.Progression.NextTierCandidateIds.Count > 0)
                    .Select(definition => definition.Id.Value + " -> " + string.Join(
                        ", ",
                        definition.Progression.NextTierCandidateIds.Select(id => id.Value)))
                    .ToArray(),
                Is.EqualTo(expected),
                "p.15 upgrades a card into one of the pair its own colour prints at the next Tier.");

            Assert.DoesNotThrow(
                () => CardProgressionValidator.Validate(SabinaCardContent.AllDefinitions),
                "The whole set is what a configured match validates, not one card at a time.");
        }

        /// <summary>
        /// p.16's item reach, which for Sabina is the widest Radius and the narrowest Attack in the
        /// verified roster so far. Both follow from her shape rather than from a decision, and both are
        /// pinned because p.16 gives an item no effect on a card printing no matching icon - so the
        /// count of cards carrying each icon is the item's whole reach.
        /// </summary>
        [Test]
        public void HerTwelveItemIconsReachAllFourDashboardSlots_WithTheWidestRadiusInTheRoster()
        {
            var icons = SabinaCardContent.AllDefinitions
                .Where(definition => definition.Progression.HasItemIcon)
                .GroupBy(definition => definition.Progression.ItemIcon)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(icons.Values.Sum(), Is.EqualTo(12),
                "Twelve of her faces become items: the untaken card of each of the six Level Up pairs, " +
                "counted here as the icon each printed card carries.");
            Assert.That(icons.Keys.OrderBy(icon => icon), Is.EqualTo(new[]
                {
                    PrintedValueChannel.Attack,
                    PrintedValueChannel.Defense,
                    PrintedValueChannel.Movement,
                    PrintedValueChannel.Range,
                    PrintedValueChannel.Radius,
                    PrintedValueChannel.Initiative
                }.OrderBy(icon => icon)),
                "All six of p.16's icons, which is what reaches all four dashboard slots.");
            Assert.That(icons[PrintedValueChannel.Attack], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Defense], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Initiative], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Movement], Is.EqualTo(1));
            Assert.That(icons[PrintedValueChannel.Range], Is.EqualTo(1));
            Assert.That(icons[PrintedValueChannel.Radius], Is.EqualTo(1));

            Assert.That(
                SabinaCardContent.AllDefinitions.Count(definition =>
                    definition.ActionDefinitions.Any(action =>
                        action.BaseValues.Any(value =>
                            value.Channel == PrintedValueChannel.Radius))),
                Is.EqualTo(11),
                "Eleven of her cards print a Radius icon, which is the widest reach a +1 Radius item " +
                "has in the roster so far - her whole Blue and Green kit is minion command.");
            Assert.That(
                SabinaCardContent.AllDefinitions.Count(definition =>
                    definition.ActionDefinitions.Any(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                        action.Kind == ActionKind.Attack)),
                Is.EqualTo(7),
                "and seven print an Attack, which is the narrowest reach a +1 Attack item has.");

            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.BigSoddingGun)
                    .UltimateRule.PrintedValues,
                Is.Empty,
                "Her Ultimate prints neither icon, so ledger question 12 has no Sabina instance.");
        }

        /// <summary>
        /// Every printed primary's composed text paired with the moment its own face prints, which is
        /// the pairing <see cref="ActionDefinition"/> refuses to be constructed without.
        ///
        /// <para>Three of her Attacks reach rulebook p.14's step 7 and none reaches step 1: every one
        /// of them says "Target a unit in range", which is p.13's own sentence for a Ranged Attack
        /// restated on the face rather than a narrowing, so no card text of hers binds a target. The
        /// three <c>+</c> Attacks compose nothing at all, which is the row this table exists to make
        /// visible.</para>
        /// </summary>
        [Test]
        public void EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints()
        {
            var expected = new[]
            {
                "sabina-point-blank-shot|primary-ranged-attack|root|AfterTheAttack|composed",
                "sabina-bang|primary-attack|-|None|owed",
                "sabina-back-to-back|primary-skill|root|None|composed",
                "sabina-quickdraw|primary-ranged-attack|-|None|composed",
                "sabina-gunslinger|primary-ranged-attack|-|None|composed",
                "sabina-shootout|primary-ranged-attack|root|AfterTheAttack|composed",
                "sabina-dead-shot|primary-ranged-attack|-|None|composed",
                "sabina-bullet-hell|primary-ranged-attack|root|AfterTheAttack|composed",
                "sabina-listen-up|primary-skill|root|None|composed",
                "sabina-roger-roger|primary-skill|root|None|composed",
                "sabina-steady-advance|primary-skill|root|None|composed",
                "sabina-ready-and-waiting|primary-skill|root|None|composed",
                "sabina-unwavering-resolve|primary-skill|root|None|composed",
                "sabina-troop-movement|primary-skill|root|None|composed",
                "sabina-marching-orders|primary-skill|root|None|composed",
                "sabina-close-support|primary-skill|root|None|composed",
                "sabina-path-to-victory|primary-skill|root|None|composed",
                "sabina-covering-fire|primary-skill|root|None|composed"
            };

            Assert.That(
                SabinaCardContent.AllDefinitions
                    .Select(definition => new
                    {
                        definition.Id,
                        Primary = Primary(definition)
                    })
                    .Where(row => row.Primary != null)
                    .Select(row => string.Join("|", new[]
                    {
                        row.Id.Value,
                        row.Primary.Id.Value,
                        row.Primary.EffectRoot == null ? "-" : "root",
                        row.Primary.CardTextTiming.ToString(),
                        row.Primary.TextComposition.IsAwaited ? "owed" : "composed"
                    }))
                    .ToArray(),
                Is.EqualTo(expected),
                "Point Blank Shot, Shootout and Bullet Hell print \"After the attack:\" and are at " +
                "step 7. The three `+` Attacks compose no text because a conditional addition to the " +
                "Attack Total is not one of the page's three text moments, and Bang states no moment " +
                "because its composition is deferred with p.20's Handicap card.");
        }

        /// <summary>
        /// p.14's printed <c>+</c>, which Sabina's set is the first in this build to carry.
        ///
        /// <para>The page states the convention next to its own worked example: "The <c>+</c> sign
        /// signifies that the value can be increased by the card text." So the three faces printing it
        /// are exactly the three declaring a card-text contribution, and the sixteen printing a plain
        /// number are exactly the sixteen declaring none. That biconditional is the assertion - a
        /// definition that declared the contribution without printing the sign, or printed it without
        /// the contribution, would be a face this build draws differently from the card.</para>
        /// </summary>
        [Test]
        public void ThreeRedAttacksDeclareTheRaisedValuePageFourteensPlusSignMarks_AndNoOtherFaceDoes()
        {
            var raised = SabinaCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions
                    .Where(action => action.RosterValueContributions.Count > 0)
                    .Select(action => definition.Id.Value + " " + string.Join(
                        ", ",
                        action.RosterValueContributions.Select(value =>
                            value.Channel + " +" + value.AmountPerMatch + " on " + value.Query))))
                .ToArray();

            Assert.That(raised, Is.EqualTo(new[]
                {
                    "sabina-quickdraw Attack +3 on TargetPlayedAttackCardThisTurn",
                    "sabina-gunslinger Attack +3 on TargetPlayedAttackCardThisTurn",
                    "sabina-dead-shot Attack +4 on TargetPlayedAttackCardThisTurn"
                }),
                "Quickdraw prints `2+` with \"+3 Attack\", Gunslinger `3+` with \"+3\", and Dead Shot " +
                "`3+` with \"+4\". No other face of hers prints the sign, so no other face declares a " +
                "contribution.");

            foreach (var id in new[]
            {
                SabinaCardContent.Quickdraw, SabinaCardContent.Gunslinger, SabinaCardContent.DeadShot
            })
            {
                Assert.That(
                    SabinaCardContent.Definition(id).SourceContent.PrintedText,
                    Does.Contain("if the target played an attack card this turn"),
                    id.Value + " states the condition its declaration asks.");
                Assert.That(Primary(SabinaCardContent.Definition(id)).EffectRoot, Is.Null,
                    id.Value + " composes no card text: the clause is a value, not a step.");
            }
        }

        [Test]
        public void EveryDefinitionIsVerifiedOfficialAtOneRevision_WithItsTranscriptionCarried()
        {
            foreach (var definition in SabinaCardContent.AllDefinitions)
            {
                Assert.That(
                    definition.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial),
                    definition.Id.Value + " is transcribed from a printed face.");
                Assert.That(
                    definition.ContentRevision, Is.EqualTo(SabinaCardContent.ContentRevision),
                    "One hero's set moves as one revision.");
                Assert.That(
                    definition.SourceContent.PrintedText, Is.Not.Null.And.Not.Empty,
                    definition.Id.Value + " carries the words its face prints.");
            }

            // Ready and Waiting is the first verified card overriding p.13's heavy minion immunity, and
            // the clause is the whole of what its Tier III printing buys over its two siblings. The
            // three are asserted rather than described so that nobody "tidies" the three otherwise
            // identical sentences into one wording and erases the upgrade.
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.ListenUp).SourceContent.PrintedText,
                Is.EqualTo("Swap two minions in radius."));
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.RogerRoger).SourceContent.PrintedText,
                Is.EqualTo("Swap two minions in radius."));
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.ReadyAndWaiting).SourceContent.PrintedText,
                Is.EqualTo("Swap two minions in radius, ignoring heavy minion immunity."),
                "The three differ only in Radius and in this clause, so the clause is the upgrade.");

            // Ledger question 13's construction and the `if able` contrast beside it, which is the same
            // Tier-scaled pair Wasp's Shock and Electroblast print.
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.CloseSupport).SourceContent.PrintedText,
                Does.Contain("discards a card, if able"));
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.CoveringFire).SourceContent.PrintedText,
                Does.Contain("discards a card, or is defeated"));

            // p.19's Remove reminder, which Shootout prints and Bullet Hell does not. Nothing turns on
            // it - p.19's own entry says the same in general terms - and it is transcribed because the
            // face prints it.
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.Shootout).SourceContent.PrintedText,
                Does.Contain("( You gain no coins for removing a minion, only for defeating. )"));
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.BulletHell).SourceContent.PrintedText,
                Does.Not.Contain("no coins"));

            // The two tenses her two adjacency riders print, which is what makes them two questions.
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.PointBlankShot).SourceContent.PrintedText,
                Does.Contain("If the target is adjacent to you"));
            Assert.That(
                SabinaCardContent.Definition(SabinaCardContent.Shootout).SourceContent.PrintedText,
                Does.Contain("If the target was adjacent to you"));
        }

        /// <summary>
        /// She is the first verified hero printing no rulebook p.19 Active effect of any kind, and the
        /// absence is content rather than an omission: Wasp prints three <c>This turn:</c> cards, Arien
        /// three plus a <c>This round:</c>, Brogan one plus three <c>This round:</c>, and Sabina none.
        /// Every card of hers resolves immediately.
        /// </summary>
        [Test]
        public void NoFacePrintsAnActiveEffect_WhichIsAFirstForThisBuild()
        {
            foreach (var definition in SabinaCardContent.AllDefinitions)
            {
                Assert.That(
                    definition.SourceContent.PrintedText, Does.Not.Contain("This turn:"),
                    definition.Id.Value + " prints no p.19 Active effect.");
                Assert.That(
                    definition.SourceContent.PrintedText, Does.Not.Contain("This round:"),
                    definition.Id.Value + " prints no p.19 Active effect.");
            }

            var created = SabinaCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability.CapabilityId.Value)
                .Where(id => id.Contains("guard-allies") || id.Contains("shadowed") ||
                    id.Contains("petrify") || id.Contains("scale-with") ||
                    id.Contains("shield-friendly") || id.Contains("constrict"))
                .ToArray();
            Assert.That(created, Is.Empty,
                "and no composition of hers creates one either, which is the same statement made " +
                "about the code rather than about the words.");
        }

        /// <summary>
        /// p.16's Ultimate, and the first in this build printing two sentences: two standing value
        /// declarations and one trigger.
        /// </summary>
        [Test]
        public void BigSoddingGunDeclaresTwoStandingRulesAndAnAfterPushTrigger()
        {
            var ultimate = SabinaCardContent.Definition(SabinaCardContent.BigSoddingGun);

            Assert.That(ultimate.Initiative.IsDefined, Is.False,
                "p.16's Ultimate is placed beside the dashboard and never played into a turn slot.");
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "and it is not an action at all.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);
            Assert.That(ultimate.UltimateRule.PrintedValues, Is.Empty,
                "This card prints neither of p.9's icons.");

            Assert.That(
                ultimate.UltimateRule.Modifications
                    .Select(value => value.Kind + " " + value.Channel + " +" + value.Amount +
                        " " + value.ValueApplicability)
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "ValueModifier Attack +2 ControllerBasicAttackAction",
                    "ValueModifier Range +2 ControllerBasicAttackAction"
                }),
                "\"Your basic attack has +2 Range and +2 Attack\" is two channels of one sentence, " +
                "each qualified to the action class the sentence names - without the qualification " +
                "her five non-basic Red Attacks would gain two Attack the card does not give them.");

            Assert.That(
                ultimate.UltimateRule.AfterPush.Subject,
                Is.EqualTo(AfterPushTriggerSubject.EnemyHero),
                "\"If you push an enemy hero\" names the pushed object the trigger fires for.");
            Assert.That(ultimate.UltimateRule.TriggeredEffect.HasValue, Is.True,
                "and the second sentence is what it runs.");
            Assert.That(ultimate.UltimateRule.MinionBattleRule, Is.Null);
            Assert.That(ultimate.UltimateRule.BeforeAction.IsDeclared, Is.False);
            Assert.That(ultimate.UltimateRule.TriggerCheckpoints, Is.Empty,
                "One Ultimate declares one trigger: this one's is the moment a Push it performed " +
                "applied, so it declares neither a checkpoint nor a start before an action.");
        }

        /// <summary>
        /// The trigger's own text, which is the first printing of ledger question 13's construction
        /// whose subject is not selected: the push has already named the hero.
        /// </summary>
        [Test]
        public void TheAfterPushTriggerNamesThePushedHeroByRole_RatherThanSelectingOne()
        {
            var effect = SabinaEffectContent.BigSoddingGunTriggeredEffect(
                SabinaCardContent.ContentRevision);

            var roleBindings = effect.Nodes
                .SelectMany(node => node.Inputs)
                .Where(binding => binding.SourceKind == EffectBindingSourceKind.Role)
                .Select(binding => binding.Role)
                .Distinct()
                .ToArray();

            Assert.That(roleBindings, Is.EqualTo(new[] { EffectRoleKind.OpposingHero }),
                "\"that hero\" is the one the push moved, which the frame already holds - so the " +
                "text names them rather than opening a selection with one answer in it.");

            Assert.That(
                effect.Nodes.Any(node =>
                    node.Capability.IsDefined &&
                    node.Capability.CapabilityId.Value == "synthetic-discard-chosen-card"),
                Is.True,
                "\"discards a card\" is the ordinary discard.");
            Assert.That(
                effect.Nodes.Any(node =>
                    node.Capability.IsDefined &&
                    node.Capability.CapabilityId.Value == "synthetic-defeat-hero"),
                Is.True,
                "\"or is defeated\" is the second way the one mandatory step completes.");
            Assert.That(
                effect.Nodes.Any(node => node.Kind == EffectNodeKind.Conditional),
                Is.True,
                "and the Defeat is reached only through the discard's own inability, rather than by " +
                "asking the board a second question.");
        }

        [Test]
        public void EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var roots = new List<EffectDefinition>();
            foreach (var definition in SabinaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.EffectRoot != null)
                    {
                        roots.Add(action.EffectRoot);
                    }
                }
            }

            Assert.That(roots, Has.Count.EqualTo(14),
                "Fourteen composed printed primaries. Five of the nineteen faces carry no root: Bang's " +
                "composition is deferred with p.20's Handicap card, the three `+` Attacks state a " +
                "value rather than a step, and Big Sodding Gun is an Ultimate rather than an action.");

            roots.Add(SabinaEffectContent.BigSoddingGunTriggeredEffect(
                SabinaCardContent.ContentRevision));

            foreach (var root in roots)
            {
                var rejections = EffectDefinitionValidator.Validate(root, catalogs.Capabilities);
                Assert.That(
                    rejections, Is.Empty,
                    root.Id.Value + " is malformed: " + string.Join(
                        ", ", rejections.Select(value => value.ToString())));
            }

            Assert.That(
                catalogs.TriggeredEffects.TryGet(
                    SabinaEffectContent.BigSoddingGunTriggeredEffect(
                        SabinaCardContent.ContentRevision).Reference,
                    out _),
                Is.True,
                "The Ultimate's effect has to be in the build's own catalog, or the trigger fires " +
                "into nothing when it reaches milestone 8.19c1's after-push window.");
        }

        [Test]
        public void TheDealtCards_PassTheInitialSetupValidatorAndRunTheOrdinaryLifecycle()
        {
            var owner = new HeroId("sabina-content-hero");
            var hand = SabinaCardContent.CreateHand(owner, "blue");

            Assert.DoesNotThrow(() => CardInitialSetupValidator.Validate(
                hand.Definitions.ToArray(), hand.AllInstances.ToArray()));
            Assert.That(hand.Instances, Has.Count.EqualTo(5));
            Assert.That(hand.DeckInstances, Has.Count.EqualTo(13));
            Assert.That(
                hand.AllInstances.All(card => card.OwnerId.Equals(owner)), Is.True);
        }

        [Test]
        public void TwoSeatsDealingSabina_GetDistinctInstanceIdentities()
        {
            var red = SabinaCardContent.CreateHand(new HeroId("red-sabina"), "red");
            var blue = SabinaCardContent.CreateHand(new HeroId("blue-sabina"), "blue");

            Assert.That(
                red.AllInstances.Select(card => card.Id.Value)
                    .Intersect(blue.AllInstances.Select(card => card.Id.Value)),
                Is.Empty,
                "Instance identity is per seat even when the definition is shared.");
            Assert.That(
                red.Definitions.Select(definition => definition.Reference),
                Is.EqualTo(blue.Definitions.Select(definition => definition.Reference)),
                "and one printed set is one set however many seats deal it.");
        }

        // -----------------------------------------------------------------------------------------
        // Description helpers. Sabina's own rather than shared, for the reason Brogan's and Arien's
        // are their own: one hero's set decides which action kind can be found in which slot, and
        // hers prints no Defense primary and no Movement primary at all.
        // -----------------------------------------------------------------------------------------

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
