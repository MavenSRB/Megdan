using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.21. Brogan the Destroyer's nineteen printed faces as definition content, checked
    /// against the source inventory milestone 8.18b verified rather than against themselves.
    ///
    /// The printed-value assertion below is deliberately a literal table in the shard's own column
    /// order, for the reason <see cref="WaspVerifiedContentTests"/> and
    /// <see cref="ArienVerifiedContentTests"/> record: a test that derived its expectations from the
    /// definitions would pass whatever the definitions happened to say. This one fails if a single
    /// number, colour, Tier, icon or name drifts from <c>Docs/AI/Rules/HERO_BROGAN.md</c>, which is the
    /// document the extracted faces were transcribed into.
    ///
    /// <para>Brogan is the first verified hero whose edition answer carries no caveat at all - the
    /// second-printing rulebook illustrates three of his faces and all three match exactly - so unlike
    /// the two sets before his there is no rulebook/face wording difference to pin here.</para>
    /// </summary>
    public sealed class BroganVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the shard's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "brogan-onslaught|Onslaught|Init 11|SecDef 3|SecMov 1|primary-attack Attack NonRanged [Attack 4]|Gold Basic|item -",
            "brogan-leap-forward|Leap Forward|Init 10|SecDef 3|SecMov 1|primary-attack Attack NonRanged [Attack 3]|Gold Basic|item -",
            "brogan-bulwark|Bulwark|Init 12|SecDef 4|SecMov -|primary-skill Skill None [Radius 4]|Silver Basic|item -",
            "brogan-mad-dash|Mad Dash|Init 7|SecDef 7|SecMov 3|primary-attack Attack NonRanged [Attack 6]|Red I|item -",
            "brogan-bullrush|Bullrush|Init 7|SecDef 8|SecMov 3|primary-attack Attack NonRanged [Attack 6]|Red II|item Initiative",
            "brogan-throwing-axe|Throwing Axe|Init 7|SecDef 7|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 2]|Red II|item Defense",
            "brogan-furious-charge|Furious Charge|Init 8|SecDef 8|SecMov 3|primary-attack Attack NonRanged [Attack 7]|Red III|item Movement",
            "brogan-throwing-spear|Throwing Spear|Init 8|SecDef 7|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 2]|Red III|item Radius",
            "brogan-brutal-jab|Brutal Jab|Init 8|SecDef 5|SecMov 2|primary-skill Skill None []|Blue I|item -",
            "brogan-mighty-punch|Mighty Punch|Init 9|SecDef 6|SecMov 2|primary-skill Skill None []|Blue II|item Initiative",
            "brogan-shield-bash|Shield Bash|Init 9|SecDef 6|SecMov 2|primary-skill Skill None []|Blue II|item Attack",
            "brogan-savage-kick|Savage Kick|Init 9|SecDef 6|SecMov 2|primary-skill Skill None []|Blue III|item Initiative",
            "brogan-counterattack|Counterattack|Init 9|SecDef 6|SecMov 2|primary-skill Skill None []|Blue III|item Range",
            "brogan-shield|Shield|Init 6|SecDef 4|SecMov 2|primary-skill Skill None [Radius 1]|Green I|item -",
            "brogan-bolster|Bolster|Init 5|SecDef 5|SecMov 2|primary-skill Skill None [Radius 2]|Green II|item Attack",
            "brogan-war-drummer|War Drummer|Init 5|SecDef 5|SecMov 2|primary-skill Skill None [Range 4]|Green II|item Defense",
            "brogan-fortify|Fortify|Init 5|SecDef 5|SecMov 2|primary-skill Skill None [Radius 2]|Green III|item Attack",
            "brogan-master-skald|Master Skald|Init 5|SecDef 5|SecMov 2|primary-skill Skill None [Range 4]|Green III|item Defense",
            "brogan-one-man-army|One Man Army|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                BroganCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.18b verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        [Test]
        public void TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(BroganCardContent.AllDefinitions, Has.Count.EqualTo(19),
                "p.6's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(BroganCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(BroganCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { BroganCardContent.LeapForward }),
                "p.6 returns the Handicap card to the box at setup.");

            Assert.That(
                BroganCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    BroganCardContent.Onslaught,
                    BroganCardContent.Bulwark,
                    BroganCardContent.MadDash,
                    BroganCardContent.BrutalJab,
                    BroganCardContent.Shield
                }),
                "p.6 step 11 deals the three Tier I cards and the two basic cards with no Tier.");

            Assert.That(
                BroganCardContent.StartingHand
                    .Select(id => BroganCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.15: \"exactly 5 cards, 1 of each color, in your hand\".");

            Assert.That(BroganCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                BroganCardContent.Deck.Concat(BroganCardContent.StartingHand)
                    .Concat(BroganCardContent.ReturnedToBoxAtSetup)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal),
                Is.EqualTo(BroganCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal)),
                "Every face belongs to exactly one of the three places p.6 puts it.");

            foreach (var colour in new[] { CardColor.Red, CardColor.Green, CardColor.Blue })
            {
                var set = BroganCardContent.AllDefinitions
                    .Where(definition => definition.Progression.Color == colour)
                    .ToArray();
                Assert.That(
                    set.Select(definition => definition.Progression.Tier).OrderBy(tier => tier),
                    Is.EqualTo(new[] { CardTier.I, CardTier.II, CardTier.II, CardTier.III, CardTier.III }),
                    "Each colour set is one Tier I, two Tier II and two Tier III; " + colour + " is not.");
            }
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
                "brogan-mad-dash -> brogan-bullrush, brogan-throwing-axe",
                "brogan-bullrush -> brogan-furious-charge, brogan-throwing-spear",
                "brogan-throwing-axe -> brogan-furious-charge, brogan-throwing-spear",
                "brogan-brutal-jab -> brogan-mighty-punch, brogan-shield-bash",
                "brogan-mighty-punch -> brogan-savage-kick, brogan-counterattack",
                "brogan-shield-bash -> brogan-savage-kick, brogan-counterattack",
                "brogan-shield -> brogan-bolster, brogan-war-drummer",
                "brogan-bolster -> brogan-fortify, brogan-master-skald",
                "brogan-war-drummer -> brogan-fortify, brogan-master-skald"
            };

            Assert.That(
                BroganCardContent.AllDefinitions
                    .Where(definition => definition.Progression.NextTierCandidateIds.Count > 0)
                    .Select(definition => definition.Id.Value + " -> " + string.Join(
                        ", ",
                        definition.Progression.NextTierCandidateIds.Select(id => id.Value)))
                    .ToArray(),
                Is.EqualTo(expected),
                "p.15 upgrades a card into one of the pair its own colour prints at the next Tier.");

            Assert.DoesNotThrow(
                () => CardProgressionValidator.Validate(BroganCardContent.AllDefinitions),
                "The whole set is what a configured match validates, not one card at a time.");
        }

        /// <summary>
        /// Brogan is the first hero whose set reaches rulebook p.14's step 7 with something to do
        /// there, and the first to print p.10's modal form. What is checked here is the pairing every
        /// <see cref="ActionDefinition"/> refuses to be constructed without: a composed root and the
        /// moment it is applied at arrive together, or neither does and the card states the milestone
        /// that owes it.
        /// </summary>
        [Test]
        public void EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints()
        {
            var expected = new[]
            {
                "brogan-onslaught|primary-attack|root|AfterTheAttack|composed",
                "brogan-leap-forward|primary-attack|-|None|owed",
                "brogan-bulwark|primary-skill|root|None|composed",
                "brogan-mad-dash|primary-attack|root|BeforeTheAttack|composed",
                "brogan-bullrush|primary-attack|root|BeforeTheAttack|composed",
                "brogan-throwing-axe|primary-ranged-attack|root|BeforeTheAttack|composed",
                "brogan-furious-charge|primary-attack|root|BeforeTheAttack|composed",
                "brogan-throwing-spear|primary-ranged-attack|root|BeforeTheAttack|composed",
                "brogan-brutal-jab|primary-skill|root|None|composed",
                "brogan-mighty-punch|primary-skill|root|None|composed",
                "brogan-shield-bash|primary-skill|root|None|composed",
                "brogan-savage-kick|primary-skill|root|None|composed",
                "brogan-counterattack|primary-skill|root|None|composed",
                "brogan-shield|primary-skill|root|None|composed",
                "brogan-bolster|primary-skill|root|None|composed",
                "brogan-war-drummer|primary-skill|root|None|composed",
                "brogan-fortify|primary-skill|root|None|composed",
                "brogan-master-skald|primary-skill|root|None|composed"
            };

            Assert.That(
                BroganCardContent.AllDefinitions
                    .Select(definition => new
                    {
                        definition.Id,
                        Primary = definition.ActionDefinitions.FirstOrDefault(action =>
                            action.Origin == ActionDefinitionOrigin.PrintedPrimary)
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
                "Onslaught's \"After the attack:\" is step 7 and the three charges' \"Before the " +
                "attack:\" is step 1; the two modal Ranged Attacks print no keyword and are at step 1 " +
                "because their chosen bullet is the targeting sentence itself.");
        }

        /// <summary>
        /// The five faces whose own text names the Attack's target, and the one that narrows which
        /// units it may take.
        ///
        /// <para>Every other printed Attack in this build chooses its target when the action starts,
        /// which is p.14's ordinary reading. Brogan's three charges name the unit their move ends
        /// beside, and his two modal Ranged Attacks name it in whichever bullet was chosen - so all
        /// five reach step 1 with no target at all, and text that binds none stops the action there
        /// under p.10's mandatory-step rule.</para>
        /// </summary>
        [Test]
        public void TheChargesAndModalAttacks_BindTheirTargetByCardText()
        {
            var boundByText = new[]
            {
                BroganCardContent.MadDash,
                BroganCardContent.Bullrush,
                BroganCardContent.ThrowingAxe,
                BroganCardContent.FuriousCharge,
                BroganCardContent.ThrowingSpear
            };

            foreach (var definition in BroganCardContent.AllDefinitions)
            {
                var primary = definition.ActionDefinitions.FirstOrDefault(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.Kind == ActionKind.Attack);
                if (primary == null)
                {
                    continue;
                }

                var policy = primary.AttackTargetPolicy;
                var expected = boundByText.Contains(definition.Id);
                Assert.That(
                    policy.HasValue && policy.Value.TargetIsBoundByCardText, Is.EqualTo(expected),
                    definition.Id.Value + " states the wrong moment for when its target is bound.");
            }

            // "Target a hero adjacent to you", which is the one narrowing any Brogan face states and
            // the only thing Leap Forward says that Onslaught does not.
            var leapForward = BroganCardContent.Definition(BroganCardContent.LeapForward)
                .ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(
                leapForward.AttackTargetPolicy.HasValue &&
                leapForward.AttackTargetPolicy.Value.TargetableUnits ==
                    AttackTargetableUnits.HeroesOnly,
                Is.True,
                "p.20's Handicap card narrows its target to a hero where Onslaught takes any unit.");
            // Onslaught narrows nothing, so what it carries is p.13's own reading for its subtype -
            // every enemy unit, adjacent, in any file, chosen when the action starts. The definition
            // fills that in rather than leaving the policy absent, which is why this compares against
            // the page's default instead of against nothing.
            var onslaught = BroganCardContent.Definition(BroganCardContent.Onslaught)
                .ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(
                onslaught.AttackTargetPolicy,
                Is.EqualTo(PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                "Onslaught narrows nothing, so p.13's own reading for a non-Ranged Attack stands.");
        }

        [Test]
        public void EveryDefinitionIsVerifiedOfficialAtOneRevision_WithItsTranscriptionCarried()
        {
            foreach (var definition in BroganCardContent.AllDefinitions)
            {
                Assert.That(
                    definition.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial),
                    definition.Id.Value + " is transcribed from a printed face.");
                Assert.That(
                    definition.ContentRevision, Is.EqualTo(BroganCardContent.ContentRevision),
                    "One hero's set moves as one revision.");
                Assert.That(
                    definition.SourceContent.PrintedText, Is.Not.Null.And.Not.Empty,
                    definition.Id.Value + " carries the words its face prints.");
            }

            // The Green trio prints the colour-versus-supertype distinction on the cards themselves,
            // which is independent printed corroboration of the 2026-08-23 ruling recorded as Arien's
            // A-03. They are asserted rather than described so that nobody "tidies" the three
            // otherwise identical sentences into one wording and erases the upgrade.
            Assert.That(
                BroganCardContent.Definition(BroganCardContent.Shield).SourceContent.PrintedText,
                Does.Contain("discard a silver card"));
            Assert.That(
                BroganCardContent.Definition(BroganCardContent.Bolster).SourceContent.PrintedText,
                Does.Contain("discard a silver card"));
            Assert.That(
                BroganCardContent.Definition(BroganCardContent.Fortify).SourceContent.PrintedText,
                Does.Contain("discard a basic card"),
                "The Tier III card is a genuine upgrade only because it names p.15's supertype where " +
                "its two siblings name a colour.");

            // p.9's reminder text, which Mad Dash prints and its two higher-Tier siblings do not.
            // Nothing turns on it - p.10's mandatory-step rule reaches the same result for all three -
            // and it is transcribed because the face prints it.
            Assert.That(
                BroganCardContent.Definition(BroganCardContent.MadDash).SourceContent.PrintedText,
                Does.Contain("( If you cannot make this move, you cannot attack. )"));
            Assert.That(
                BroganCardContent.Definition(BroganCardContent.Bullrush).SourceContent.PrintedText,
                Does.Not.Contain("cannot attack"));
        }

        /// <summary>
        /// p.16's Ultimate. Its whole printed content is a rule about rulebook p.8's Minion Battle,
        /// which is what makes it a declared rule rather than an effect that runs - and the user's
        /// 2026-08-23 decision on B-02 is what its second clause means.
        /// </summary>
        [Test]
        public void OneManArmyPrintsNoIconAndNoInitiative_AndDeclaresItsMinionBattleRule()
        {
            var ultimate = BroganCardContent.Definition(BroganCardContent.OneManArmy);

            Assert.That(ultimate.Initiative.IsDefined, Is.False,
                "p.16's Ultimate is placed beside the dashboard and never played into a turn slot.");
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "and it is not an action at all.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);
            Assert.That(ultimate.UltimateRule.PrintedValues, Is.Empty,
                "Ledger question 12 has no Brogan instance: this card prints neither icon.");

            var rule = ultimate.UltimateRule.MinionBattleRule;
            Assert.That(rule, Is.Not.Null,
                "Both printed sentences are about p.8's Minion Battle, so both are one declaration.");
            Assert.That(
                rule.Reference, Is.EqualTo(OneManArmyMinionBattleHandler.RuleReference),
                "The declaration names the registered rule rather than a Brogan branch.");
            Assert.That(
                rule.Strength, Is.EqualTo(MinionBattleStrength.HeavyMinion),
                "\"During minion battle you count as a heavy minion\".");
            Assert.That(
                rule.RemovalSubstitution, Is.EqualTo(MinionBattleRemovalSubstitution.PushLost),
                "\"if you would be removed, lose the push instead\" - the user's approved reading of " +
                "B-02, which is p.8's ordinary Lane Push run in full with Brogan left standing.");
        }

        /// <summary>
        /// The four persistent texts, checked against what their faces say rather than against
        /// themselves. Every field here is a printed clause.
        /// </summary>
        [Test]
        public void TheFourActiveEffectDeclarations_SayWhatTheirFacesSay()
        {
            var expected = new[]
            {
                "brogan-bulwark-effect|ControllerAndUnitsInRadius|ThisTurn|ActionStarted|" +
                    "TurnOnlyCancellationBeforeTurnEnded|AlliedWithSource|printed Radius 4",
                "brogan-shield-effect|MinionsInRadius|ThisRound|ActionStarted|RoundEnd|" +
                    "AlliedWithSource|printed Radius 1",
                "brogan-bolster-effect|MinionsInRadius|ThisRound|ActionStarted|RoundEnd|" +
                    "AlliedWithSource|printed Radius 2",
                "brogan-fortify-effect|MinionsInRadius|ThisRound|ActionStarted|RoundEnd|" +
                    "AlliedWithSource|printed Radius 2"
            };

            Assert.That(
                BroganActiveEffectContent.Definitions.Select(definition => string.Join("|", new[]
                {
                    definition.DefinitionId,
                    definition.AffectedScope.ToString(),
                    definition.DurationKind.ToString(),
                    definition.ActivationCheckpoint.ToString(),
                    definition.ExpirationCheckpoint.ToString(),
                    definition.AffectedUnitRelation.ToString(),
                    definition.Radius.ToString()
                })).ToArray(),
                Is.EqualTo(expected),
                "A scope, duration, disk or side differs from the printed sentence.");

            // Bulwark's four printed verbs, and the side its sentence bars.
            var bulwark = BroganActiveEffectContent.BulwarkDefinition.Modifications.Single();
            Assert.That(bulwark.Kind, Is.EqualTo(RuleModificationKind.Immunity),
                "p.18 makes immunity a statement about what may be done to a unit, which is what " +
                "\"cannot be moved, pushed, swapped or placed\" says.");
            Assert.That(
                bulwark.Activities,
                Is.EquivalentTo(new[]
                {
                    RuleActivity.DirectMove,
                    RuleActivity.ObjectPush,
                    RuleActivity.Swap,
                    RuleActivity.Place
                }),
                "Four printed verbs, where the only comparable verified card declares two.");
            Assert.That(
                bulwark.RestrictedActors.Relations,
                Is.EquivalentTo(new[] { RestrictedActorRelation.HeroesOpposingSource }),
                "\"by enemy heroes\" is who is barred.");

            // The three prices, which are the whole difference between the Green trio's sentences.
            AssertPrice(
                BroganActiveEffectContent.ShieldDefinition,
                new[] { CardColor.Silver }, CardClassification.None);
            AssertPrice(
                BroganActiveEffectContent.BolsterDefinition,
                new[] { CardColor.Silver }, CardClassification.None);
            AssertPrice(
                BroganActiveEffectContent.FortifyDefinition,
                System.Array.Empty<CardColor>(), CardClassification.Basic);

            foreach (var definition in BroganActiveEffectContent.Definitions)
            {
                Assert.That(definition.CarriesAcrossRoundBoundary, Is.False,
                    "No Brogan text says \"next turn\"; the longest is the Green trio's \"This round\".");
                Assert.That(
                    definition.CancellationConditions,
                    Is.EquivalentTo(new[]
                    {
                        ActiveEffectCancellationCondition.ControllerHeroDefeated,
                        ActiveEffectCancellationCondition.CardsRetrieved
                    }),
                    "p.17's two cancellations that apply, and not the source card resolving - which " +
                    "the page signals the effect is still running by.");
            }
        }

        /// <summary>
        /// Every composed effect this hero registers is well formed against the catalog a match runs
        /// on. It is the validator a configured match already runs at composition, asked here of one
        /// hero's whole set so a malformed node fails in a named test rather than at the table.
        /// </summary>
        [Test]
        public void EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();
            var roots = new List<EffectDefinition>();
            foreach (var definition in BroganCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.EffectRoot != null)
                    {
                        roots.Add(action.EffectRoot);
                    }
                }
            }

            Assert.That(roots, Has.Count.EqualTo(17),
                "Seventeen composed printed primaries. Two of the nineteen faces carry no root: Leap " +
                "Forward's composition is deferred with p.20's Handicap card, and One Man Army is a " +
                "declared Minion-Battle rule rather than an effect that runs.");

            foreach (var root in roots)
            {
                var rejections = EffectDefinitionValidator.Validate(root, catalogs.Capabilities);
                Assert.That(
                    rejections, Is.Empty,
                    root.Id.Value + " is malformed: " + string.Join(
                        ", ", rejections.Select(value => value.ToString())));
            }
        }

        [Test]
        public void TheDealtCards_PassTheInitialSetupValidatorAndRunTheOrdinaryLifecycle()
        {
            var owner = new HeroId("brogan-content-hero");
            var hand = BroganCardContent.CreateHand(owner, "blue");

            Assert.DoesNotThrow(() => CardInitialSetupValidator.Validate(
                hand.Definitions.ToArray(), hand.AllInstances.ToArray()));
            Assert.That(hand.Instances, Has.Count.EqualTo(5));
            Assert.That(hand.DeckInstances, Has.Count.EqualTo(13));
            Assert.That(
                hand.AllInstances.All(card => card.OwnerId.Equals(owner)), Is.True);
        }

        [Test]
        public void TwoSeatsDealingBrogan_GetDistinctInstanceIdentities()
        {
            var red = BroganCardContent.CreateHand(new HeroId("red-brogan"), "red");
            var blue = BroganCardContent.CreateHand(new HeroId("blue-brogan"), "blue");

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

        private static void AssertPrice(
            ActiveEffectDefinition definition,
            IReadOnlyList<CardColor> colours,
            CardClassification classification)
        {
            var modification = definition.Modifications.Single();
            Assert.That(
                modification.Kind, Is.EqualTo(RuleModificationKind.RemovalPrevention),
                definition.DefinitionId + " buys a minion out of its removal at a price.");
            Assert.That(
                modification.RemovalPreventionCost.FromLocation, Is.EqualTo(CardLocation.Hand),
                "\"discard a ... card\" is a question about the cards a hero is holding.");
            Assert.That(
                modification.RemovalPreventionCost.AdmittedColors, Is.EqualTo(colours),
                definition.DefinitionId + " admits the colours its face names.");
            Assert.That(
                modification.RemovalPreventionCost.AdmittedClassification, Is.EqualTo(classification),
                definition.DefinitionId + " admits the classification its face names.");
        }

        // -----------------------------------------------------------------------------------------
        // Description helpers. Brogan's own rather than shared, for the reason Arien's are his own:
        // one hero's set decides which action kind can be found in which slot, and his prints no
        // Defense primary and no Movement primary at all.
        // -----------------------------------------------------------------------------------------

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
