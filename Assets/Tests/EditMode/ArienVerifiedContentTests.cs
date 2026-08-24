using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.20. Arien the Tidemaster's nineteen printed faces as definition content, checked
    /// against the source inventory milestone 8.18a verified rather than against themselves.
    ///
    /// The printed-value assertion below is deliberately a literal table in the shard's own column
    /// order, for the reason <see cref="WaspVerifiedContentTests"/> records: a test that derived its
    /// expectations from the definitions would pass whatever the definitions happened to say. This one
    /// fails if a single number, colour, Tier, icon or name drifts from
    /// <c>Docs/AI/Rules/HERO_ARIEN.md</c>, which is the document the extracted faces were transcribed
    /// into. Reading the two side by side is the whole point.
    /// </summary>
    public sealed class ArienVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the shard's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "arien-noble-blade|Noble Blade|Init 11|SecDef 2|SecMov 1|primary-attack Attack NonRanged [Attack 4]|Gold Basic|item -",
            "arien-ceremonial-blade|Ceremonial Blade|Init 10|SecDef 1|SecMov 1|primary-attack Attack NonRanged [Attack 3]|Gold Basic|item -",
            "arien-spell-break|Spell Break|Init 13|SecDef 3|SecMov -|primary-skill Skill None [Radius 3]|Silver Basic|item -",
            "arien-dangerous-current|Dangerous Current|Init 8|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 6]|Red I|item -",
            "arien-raging-stream|Raging Stream|Init 8|SecDef 7|SecMov 4|primary-attack Attack NonRanged [Attack 7]|Red II|item Initiative",
            "arien-rogue-wave|Rogue Wave|Init 8|SecDef 4|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 2]|Red II|item Defense",
            "arien-violent-torrent|Violent Torrent|Init 9|SecDef 7|SecMov 4|primary-attack Attack NonRanged [Attack 7]|Red III|item Initiative",
            "arien-tidal-blast|Tidal Blast|Init 9|SecDef 4|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 2]|Red III|item Movement",
            "arien-aspiring-duelist|Aspiring Duelist|Init 9|SecDef -|SecMov 3|primary-defense Defense Value [Defense 5]|Blue I|item -",
            "arien-expert-duelist|Expert Duelist|Init 10|SecDef -|SecMov 3|primary-defense Defense Value [Defense 6]|Blue II|item Initiative",
            "arien-slippery-ground|Slippery Ground|Init 10|SecDef 6|SecMov -|primary-movement Movement None [Movement 3]|Blue II|item Attack",
            "arien-master-duelist|Master Duelist|Init 10|SecDef -|SecMov 3|primary-defense Defense Value [Defense 6]|Blue III|item Range",
            "arien-deluge|Deluge|Init 10|SecDef 6|SecMov -|primary-movement Movement None [Movement 3, Radius 1]|Blue III|item Attack",
            "arien-liquid-leap|Liquid Leap|Init 4|SecDef 3|SecMov 2|primary-skill Skill None [Range 2]|Green I|item -",
            "arien-magical-current|Magical Current|Init 4|SecDef 3|SecMov 2|primary-skill Skill None [Range 3]|Green II|item Attack",
            "arien-arcane-whirlpool|Arcane Whirlpool|Init 4|SecDef 3|SecMov 2|primary-skill Skill None [Range 4]|Green II|item Defense",
            "arien-stranger-tide|Stranger Tide|Init 3|SecDef 4|SecMov 2|primary-skill Skill None [Range 3]|Green III|item Radius",
            "arien-ebb-and-flow|Ebb and Flow|Init 3|SecDef 4|SecMov 2|primary-skill Skill None [Range 4]|Green III|item Defense",
            "arien-living-tsunami|Living Tsunami|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                ArienCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.18a verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        [Test]
        public void TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(ArienCardContent.AllDefinitions, Has.Count.EqualTo(19),
                "p.6's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(ArienCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(ArienCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { ArienCardContent.CeremonialBlade }),
                "p.6 returns the Handicap card to the box at setup.");

            Assert.That(
                ArienCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    ArienCardContent.NobleBlade,
                    ArienCardContent.SpellBreak,
                    ArienCardContent.DangerousCurrent,
                    ArienCardContent.AspiringDuelist,
                    ArienCardContent.LiquidLeap
                }),
                "p.6 step 11 deals the three Tier I cards and the two basic cards with no Tier, and " +
                "printed p.6 illustrates these exact five together.");

            Assert.That(
                ArienCardContent.StartingHand
                    .Select(id => ArienCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.15: \"exactly 5 cards, 1 of each color, in your hand\".");

            Assert.That(ArienCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                ArienCardContent.Deck.Concat(ArienCardContent.StartingHand)
                    .Concat(ArienCardContent.ReturnedToBoxAtSetup)
                    .OrderBy(id => id.Value, StringComparer.Ordinal),
                Is.EqualTo(ArienCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .OrderBy(id => id.Value, StringComparer.Ordinal)),
                "Every face belongs to exactly one of the three places p.6 puts it.");

            foreach (var colour in new[] { CardColor.Red, CardColor.Green, CardColor.Blue })
            {
                var set = ArienCardContent.AllDefinitions
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
                "arien-dangerous-current -> arien-raging-stream, arien-rogue-wave",
                "arien-raging-stream -> arien-violent-torrent, arien-tidal-blast",
                "arien-rogue-wave -> arien-violent-torrent, arien-tidal-blast",
                "arien-aspiring-duelist -> arien-expert-duelist, arien-slippery-ground",
                "arien-expert-duelist -> arien-master-duelist, arien-deluge",
                "arien-slippery-ground -> arien-master-duelist, arien-deluge",
                "arien-liquid-leap -> arien-magical-current, arien-arcane-whirlpool",
                "arien-magical-current -> arien-stranger-tide, arien-ebb-and-flow",
                "arien-arcane-whirlpool -> arien-stranger-tide, arien-ebb-and-flow"
            };

            Assert.That(
                ArienCardContent.AllDefinitions
                    .Where(definition => definition.Progression.NextTierCandidateIds.Count > 0)
                    .Select(definition => definition.Id.Value + " -> " + string.Join(
                        ", ",
                        definition.Progression.NextTierCandidateIds.Select(id => id.Value)))
                    .ToArray(),
                Is.EqualTo(expected),
                "p.15 upgrades a card into one of the pair its own colour prints at the next Tier.");

            Assert.DoesNotThrow(
                () => CardProgressionValidator.Validate(ArienCardContent.AllDefinitions),
                "The whole set is what a configured match validates, not one card at a time.");
        }

        /// <summary>
        /// Arien is the first hero whose printed primaries span five shapes, and the first whose set
        /// uses two of rulebook p.14's three card-text moments plus p.11's own Movement moment. What is
        /// checked here is the pairing every <see cref="ActionDefinition"/> refuses to be constructed
        /// without: a composed root and the moment it is applied at arrive together, or neither does
        /// and the card states the milestone that owes it.
        /// </summary>
        [Test]
        public void EveryPrintedPrimary_PairsItsComposedTextWithTheMomentItsFacePrints()
        {
            var expected = new[]
            {
                "arien-noble-blade|primary-attack|root|BeforeTheAttack|composed",
                "arien-ceremonial-blade|primary-attack|-|None|owed",
                "arien-spell-break|primary-skill|root|None|composed",
                "arien-dangerous-current|primary-attack|root|BeforeTheAttack|composed",
                "arien-raging-stream|primary-attack|root|BeforeTheAttack|composed",
                "arien-rogue-wave|primary-ranged-attack|root|AfterTheAttack|composed",
                "arien-violent-torrent|primary-attack|root|BeforeTheAttack|composed",
                "arien-tidal-blast|primary-ranged-attack|root|AfterTheAttack|composed",
                "arien-aspiring-duelist|primary-defense|-|None|composed",
                "arien-expert-duelist|primary-defense|root|DefenseAction|composed",
                "arien-slippery-ground|primary-movement|root|BeforeTheMovement|composed",
                "arien-master-duelist|primary-defense|root|DefenseAction|composed",
                "arien-deluge|primary-movement|root|BeforeTheMovement|composed",
                "arien-liquid-leap|primary-skill|root|None|composed",
                "arien-magical-current|primary-skill|root|None|composed",
                "arien-arcane-whirlpool|primary-skill|root|None|composed",
                "arien-stranger-tide|primary-skill|root|None|composed",
                "arien-ebb-and-flow|primary-skill|root|None|composed"
            };

            Assert.That(
                ArienCardContent.AllDefinitions
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
                "A face that prints \"Before the attack:\" must not run at step 7, and a card whose " +
                "composition is owed must carry the debt and no root.");

            // Aspiring Duelist is the one composed primary with no effect root, and its reason is the
            // whole of its printed text: "Ignore all minion defense modifiers" is a statement about
            // which contributions the Defense Total takes rather than a step that happens.
            var aspiring = ArienCardContent.Definition(ArienCardContent.AspiringDuelist)
                .ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(
                aspiring.MinionDefenseContributions,
                Is.EqualTo(MinionDefenseContributionPolicy.SuppressAll));
        }

        /// <summary>
        /// p.14's "Primary Defense Value", which that page illustrates beside the secondary one and
        /// which Arien is the first hero in this build to print.
        /// </summary>
        [Test]
        public void TheThreeDuelistCards_ArePrimaryDefenseValuesThatSuppressMinionModifiers()
        {
            var duelists = new[]
            {
                ArienCardContent.AspiringDuelist,
                ArienCardContent.ExpertDuelist,
                ArienCardContent.MasterDuelist
            };

            foreach (var id in duelists)
            {
                var definition = ArienCardContent.Definition(id);
                var primary = definition.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);

                Assert.That(primary.Kind, Is.EqualTo(ActionKind.Defense));
                Assert.That(primary.DefenseResolution, Is.EqualTo(PrintedDefenseResolution.Value),
                    id.Value + " prints a number where p.14's Block prints a shield.");
                Assert.That(
                    primary.MinionDefenseContributions,
                    Is.EqualTo(MinionDefenseContributionPolicy.SuppressAll),
                    "\"Ignore all minion defense modifiers\" is printed on all three.");
                Assert.That(
                    primary.SourceCardStateTransition.RequiredLocation,
                    Is.EqualTo(CardLocation.Hand),
                    "p.14 gives one rule for every Defense action: it is performed by discarding " +
                    "exactly one card from your hand.");
                Assert.That(
                    definition.ActionDefinitions.Any(action =>
                        action.Kind == ActionKind.Defense &&
                        action.Origin == ActionDefinitionOrigin.Reaction),
                    Is.False,
                    id.Value + " prints no secondary Defense icon; its Defense is the primary.");
            }
        }

        /// <summary>
        /// p.20's Handicap card ships as inventory with its behaviour deferred under the 2026-08-21
        /// user decision, exactly as Wasp's Crackling Dagger does.
        /// </summary>
        [Test]
        public void CeremonialBladeShipsAsData_WithItsBehaviorDeferredUnderItsNamedTrigger()
        {
            var handicap = ArienCardContent.Definition(ArienCardContent.CeremonialBlade);
            var primary = handicap.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(primary.TextComposition.IsAwaited, Is.True);
            Assert.That(
                primary.TextComposition.ToString(),
                Does.Contain("odd-player-count"),
                "p.20 brings the Handicap card back only for an odd player count, which is the " +
                "trigger the deferral names.");
            Assert.That(primary.EffectRoot, Is.Null,
                "A card that states a debt carries no root, and one that carries a root states none.");
            Assert.That(
                ArienCardContent.DealtDefinitions.Any(definition => definition.Id.Equals(handicap.Id)),
                Is.False,
                "and it is not dealt, because p.6 puts it in the box.");
        }

        [Test]
        public void EveryDefinitionIsVerifiedOfficialAtOneRevision_WithItsTranscriptionCarried()
        {
            foreach (var definition in ArienCardContent.AllDefinitions)
            {
                Assert.That(
                    definition.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial),
                    definition.Id.Value + " is transcribed from a printed face.");
                Assert.That(
                    definition.ContentRevision, Is.EqualTo(ArienCardContent.ContentRevision),
                    "One hero's set moves as one revision.");
                Assert.That(
                    definition.SourceContent.PrintedText, Is.Not.Null.And.Not.Empty,
                    definition.Id.Value + " carries the words its face prints.");
            }

            // The two rulebook/face wording differences milestone 8.18a recorded, which the authority
            // hierarchy settles in the face's favour. They are asserted rather than described so that
            // nobody "corrects" either back to the rulebook's wording.
            Assert.That(
                ArienCardContent.Definition(ArienCardContent.AspiringDuelist)
                    .SourceContent.PrintedText,
                Does.Contain("as well as the enemy minions"),
                "The face keeps its article where the rulebook drops it.");
            Assert.That(
                ArienCardContent.Definition(ArienCardContent.SpellBreak).SourceContent.PrintedText,
                Does.Contain("cannot perform Skill actions, except on the gold cards"),
                "Spell Break's operative text is the face's, not the rulebook's shorter wording - " +
                "and this one is card text rather than reminder text.");
        }

        /// <summary>
        /// p.16's Ultimate. Unlike Wasp's High Voltage it prints neither Range nor Radius, so its whole
        /// printed content is the permanent ability - which milestone 8.19a's before-action declaration
        /// is the shape of.
        /// </summary>
        [Test]
        public void LivingTsunamiPrintsNoIconAndNoInitiative_AndDeclaresItsBeforeAttackAbility()
        {
            var ultimate = ArienCardContent.Definition(ArienCardContent.LivingTsunami);

            Assert.That(ultimate.Initiative.IsDefined, Is.False,
                "p.16's Ultimate is placed beside the dashboard and never played into a turn slot.");
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "and it is not an action at all.");
            Assert.That(ultimate.UltimateRule, Is.Not.Null);
            Assert.That(ultimate.UltimateRule.PrintedValues, Is.Empty,
                "Ledger question 12 has no Arien instance: this card prints neither icon.");
            Assert.That(
                ultimate.UltimateRule.BeforeAction.Action, Is.EqualTo(ActionKind.Attack),
                "\"before performing an Attack action\" names the action kind.");
            Assert.That(
                ultimate.UltimateRule.BeforeAction.Frequency,
                Is.EqualTo(BeforeActionTriggerFrequency.OncePerTurn),
                "\"Once per turn\" names the frequency.");
        }

        /// <summary>
        /// The five persistent texts, checked against what their faces say rather than against
        /// themselves. Every field here is a printed clause.
        /// </summary>
        [Test]
        public void TheFiveActiveEffectDeclarations_SayWhatTheirFacesSay()
        {
            var expected = new[]
            {
                "arien-spell-break-effect|HeroesInRadius|ThisTurn|ActionStarted|" +
                    "TurnOnlyCancellationBeforeTurnEnded|OpposingSource|printed Radius 3|None|" +
                    "Cannot Skill, except Gold",
                "arien-expert-duelist-effect|ControllerHero|ThisTurn|DefenseResponse|" +
                    "TurnOnlyCancellationBeforeTurnEnded|None|no radius|EnclosingAttacker|" +
                    "Immune to Attack by HeroesOpposingSource, except the bound actor",
                "arien-master-duelist-effect|ControllerHero|ThisRound|DefenseResponse|" +
                    "RoundEnd|None|no radius|EnclosingAttacker|" +
                    "Immune to Attack by HeroesOpposingSource, except the bound actor",
                "arien-slippery-ground-effect|HeroesInRadius|ThisTurn|ActionStarted|" +
                    "TurnOnlyCancellationBeforeTurnEnded|OpposingSource|radius 1 written in the text|" +
                    "None|Cannot FastTravel; No more than 1 with a Movement action",
                "arien-deluge-effect|HeroesInRadius|ThisTurn|ActionStarted|" +
                    "TurnOnlyCancellationBeforeTurnEnded|OpposingSource|printed Radius 1|None|" +
                    "Cannot FastTravel; No more than 1 with a Movement action"
            };

            Assert.That(
                ArienActiveEffectContent.Definitions.Select(definition => string.Join("|", new[]
                {
                    definition.DefinitionId,
                    definition.AffectedScope.ToString(),
                    definition.DurationKind.ToString(),
                    definition.CreationCheckpoint.ToString(),
                    definition.ExpirationCheckpoint.ToString(),
                    definition.AffectedUnitRelation.ToString(),
                    definition.Radius.ToString(),
                    definition.BoundActorSource.ToString(),
                    string.Join("; ", definition.Modifications.Select(value => value.ToString()))
                })).ToArray(),
                Is.EqualTo(expected),
                "A scope, duration, disk, carve-out or prohibition differs from the printed sentence.");

            foreach (var definition in ArienActiveEffectContent.Definitions)
            {
                Assert.That(definition.CarriesAcrossRoundBoundary, Is.False,
                    "No Arien text says \"next turn\"; the longest is Master Duelist's \"This round\".");
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
            foreach (var definition in ArienCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.EffectRoot != null)
                    {
                        roots.Add(action.EffectRoot);
                    }
                }
            }

            roots.Add(ArienEffectContent.LivingTsunamiTriggeredEffect(
                ArienCardContent.ContentRevision));

            Assert.That(roots, Has.Count.EqualTo(17),
                "Sixteen composed printed primaries plus the Ultimate's triggered effect. Two of the " +
                "nineteen faces carry no root: Ceremonial Blade's composition is deferred with p.20's " +
                "Handicap card, and Aspiring Duelist's whole text is the minion-modifier suppression " +
                "its action declares.");

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
            var owner = new HeroId("arien-content-hero");
            var hand = ArienCardContent.CreateHand(owner, "blue");

            Assert.DoesNotThrow(() => CardInitialSetupValidator.Validate(
                hand.Definitions.ToArray(), hand.AllInstances.ToArray()));
            Assert.That(hand.Instances, Has.Count.EqualTo(5));
            Assert.That(hand.DeckInstances, Has.Count.EqualTo(13));
            Assert.That(
                hand.AllInstances.All(card => card.OwnerId.Equals(owner)), Is.True);
        }

        [Test]
        public void TwoSeatsDealingArien_GetDistinctInstanceIdentities()
        {
            var red = ArienCardContent.CreateHand(new HeroId("red-arien"), "red");
            var blue = ArienCardContent.CreateHand(new HeroId("blue-arien"), "blue");

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
        // Description helpers. Deliberately Arien's own rather than shared with Wasp's: his set is the
        // first with a printed *primary* Movement and a printed *primary* Defense, so "the secondary
        // Movement" cannot be found by action kind alone.
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
