using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.3. Wasp's nineteen printed faces as definition content, checked against the source
    /// inventory milestone 8.1 verified rather than against themselves.
    ///
    /// The printed-value assertion below is deliberately a literal table in the shard's own column
    /// order. A test that derived its expectations from the definitions would pass whatever the
    /// definitions happened to say; this one fails if a single number, colour, Tier, icon or name drifts
    /// from <c>Docs/AI/Rules/HERO_WASP.md</c>, which is the document the extracted faces were
    /// transcribed into. Reading the two side by side is the whole point.
    /// </summary>
    public sealed class WaspVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the shard's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | printed values | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "wasp-magnetic-dagger|Magnetic Dagger|Init 12|SecDef 2|SecMov 1|primary-attack Attack NonRanged [Attack 3, Radius 3]|Gold Basic|item -",
            "wasp-crackling-dagger|Crackling Dagger|Init 11|SecDef 1|SecMov 1|primary-attack Attack NonRanged [Attack 2, Radius 3]|Gold Basic|item -",
            "wasp-static-barrier|Static Barrier|Init 13|SecDef 2|SecMov -|primary-skill Skill None [Radius 2]|Silver Basic|item -",
            "wasp-shock|Shock|Init 8|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 5, Radius 2]|Red I|item -",
            "wasp-electrocute|Electrocute|Init 9|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 5, Radius 3]|Red II|item Initiative",
            "wasp-charged-boomerang|Charged Boomerang|Init 9|SecDef 3|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 3]|Red II|item Defense",
            "wasp-electroblast|Electroblast|Init 9|SecDef 7|SecMov 4|primary-attack Attack NonRanged [Attack 6, Radius 3]|Red III|item Initiative",
            "wasp-thunder-boomerang|Thunder Boomerang|Init 9|SecDef 4|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 3]|Red III|item Movement",
            "wasp-lift-up|Lift Up|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 2]|Blue I|item -",
            "wasp-control-gravity|Control Gravity|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Defense",
            "wasp-kinetic-repulse|Kinetic Repulse|Init 10|SecDef 5|SecMov 3|primary-skill Skill None []|Blue II|item Attack",
            "wasp-center-of-mass|Center of Mass|Init 11|SecDef 6|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Defense",
            "wasp-kinetic-blast|Kinetic Blast|Init 11|SecDef 6|SecMov 3|primary-skill Skill None []|Blue III|item Range",
            "wasp-stop-projectiles|Stop Projectiles|Init 3|SecDef -|SecMov 2|primary-block Defense Block []|Green I|item -",
            "wasp-deflect-projectiles|Deflect Projectiles|Init 3|SecDef -|SecMov 2|primary-block Defense Block [Range 3]|Green II|item Initiative",
            "wasp-telekinesis|Telekinesis|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Range 3]|Green II|item Attack",
            "wasp-reflect-projectiles|Reflect Projectiles|Init 2|SecDef -|SecMov 2|primary-block Defense Block [Range 3]|Green III|item Radius",
            "wasp-mass-telekinesis|Mass Telekinesis|Init 2|SecDef 4|SecMov 2|primary-skill Skill None [Range 3]|Green III|item Attack",
            "wasp-high-voltage|High Voltage|Init -|SecDef -|SecMov -|- Ultimate [Radius 3]|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                WaspCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone 8.1 " +
                "verified against the extracted faces. Correct the definition, not this table - the " +
                "table is the source.");
        }

        [Test]
        public void TheDeckIsPageFivesEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(WaspCardContent.AllDefinitions, Has.Count.EqualTo(19),
                "p.5's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(WaspCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(WaspCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { WaspCardContent.CracklingDagger }),
                "p.5 returns the Handicap card to the box at setup.");

            Assert.That(
                WaspCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    WaspCardContent.MagneticDagger,
                    WaspCardContent.StaticBarrier,
                    WaspCardContent.Shock,
                    WaspCardContent.LiftUp,
                    WaspCardContent.StopProjectiles
                }),
                "p.5 deals the three Tier I cards and the two basic cards with no Tier.");

            Assert.That(
                WaspCardContent.StartingHand
                    .Select(id => WaspCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.15: \"exactly 5 cards, 1 of each color, in your hand\".");

            Assert.That(WaspCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                WaspCardContent.Deck.Concat(WaspCardContent.StartingHand)
                    .Concat(WaspCardContent.ReturnedToBoxAtSetup)
                    .OrderBy(id => id.Value, StringComparer.Ordinal),
                Is.EqualTo(WaspCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .OrderBy(id => id.Value, StringComparer.Ordinal)),
                "Every face belongs to exactly one of the three places p.5 puts it.");

            foreach (var colour in new[] { CardColor.Red, CardColor.Green, CardColor.Blue })
            {
                var set = WaspCardContent.AllDefinitions
                    .Where(definition => definition.Progression.Color == colour)
                    .ToArray();
                Assert.That(
                    set.Select(definition => definition.Progression.Tier).OrderBy(tier => tier),
                    Is.EqualTo(new[] { CardTier.I, CardTier.II, CardTier.II, CardTier.III, CardTier.III }),
                    $"Each colour set is one Tier I, two Tier II and two Tier III; {colour} is not.");
            }
        }

        /// <summary>
        /// p.9's supertype, and the one fact milestone 8.13's Ultimate trigger will read: Static
        /// Barrier's primary is the only basic skill action in Wasp's whole set.
        /// </summary>
        [Test]
        public void TheSupertypeFollowsPageNine_AndOnlyStaticBarriersPrimaryIsABasicSkill()
        {
            var basics = WaspCardContent.AllDefinitions
                .Where(definition => definition.Progression.IsBasic)
                .Select(definition => definition.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(basics, Is.EqualTo(new[]
            {
                "wasp-crackling-dagger", "wasp-magnetic-dagger", "wasp-static-barrier"
            }), "p.9: only Gold and Silver cards are basic, and Wasp prints two Gold and one Silver.");

            var basicSkills = WaspCardContent.AllDefinitions
                .Where(definition => definition.Progression.IsBasic)
                .SelectMany(definition => definition.ActionDefinitions
                    .Where(action => action.Kind == ActionKind.Skill)
                    .Select(action => $"{definition.Id.Value}/{action.Id.Value}"))
                .ToArray();

            Assert.That(basicSkills, Is.EqualTo(new[] { "wasp-static-barrier/primary-skill" }),
                "Every action on a basic card is a basic action, so this is the only trigger High " +
                "Voltage's \"each time after you perform a basic skill\" can ever read.");

            Assert.That(
                WaspCardContent.AllDefinitions.Count(definition => definition.Progression.IsUltimate),
                Is.EqualTo(1),
                "p.16 gives a hero one Ultimate.");
        }

        [Test]
        public void TheUpgradePairsArePageFifteens_AndTheWholeSetPassesTheProgressionValidator()
        {
            Assert.DoesNotThrow(() => CardProgressionValidator.Validate(WaspCardContent.AllDefinitions),
                "p.15's own rules over the set: same colour, one Tier higher, two candidates from " +
                "Tier I and Tier II, none from Tier III.");

            Assert.That(
                WaspCardContent.AllDefinitions
                    .Where(definition => definition.Progression.NextTierCandidateIds.Count > 0)
                    .Select(definition =>
                        $"{definition.Id.Value} -> " +
                        string.Join(
                            ", ",
                            definition.Progression.NextTierCandidateIds.Select(id => id.Value)))
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "wasp-shock -> wasp-electrocute, wasp-charged-boomerang",
                    "wasp-electrocute -> wasp-electroblast, wasp-thunder-boomerang",
                    "wasp-charged-boomerang -> wasp-electroblast, wasp-thunder-boomerang",
                    "wasp-lift-up -> wasp-control-gravity, wasp-kinetic-repulse",
                    "wasp-control-gravity -> wasp-center-of-mass, wasp-kinetic-blast",
                    "wasp-kinetic-repulse -> wasp-center-of-mass, wasp-kinetic-blast",
                    "wasp-stop-projectiles -> wasp-deflect-projectiles, wasp-telekinesis",
                    "wasp-deflect-projectiles -> wasp-reflect-projectiles, wasp-mass-telekinesis",
                    "wasp-telekinesis -> wasp-reflect-projectiles, wasp-mass-telekinesis"
                }),
                "Both Tier II cards of a colour name the same Tier III pair, because p.15 sends the " +
                "player back to the same deck and the other Tier II card is already an item by then.");
        }

        /// <summary>
        /// The milestone's central discipline: verified definitions ship without composition, and every
        /// printed primary says which milestone owes it. Without the statement, an Attack whose
        /// After-the-attack rider is unimplemented is indistinguishable from one that prints nothing.
        /// </summary>
        [Test]
        public void NoActionCarriesAnEffectRoot_AndEveryPrintedPrimaryNamesWhatOwesItsText()
        {
            // Milestone 8.6 attached the first five compositions, so the question changed shape: a card
            // either carries its effect root or names the slice that owes one, and no card may do
            // neither or both. ActionDefinition already refuses "both"; this states which are which, so
            // a composition landing without its row being cleared is visible here. Milestone 8.8 added
            // the two kinetic Skills and advanced the revision every one of them carries; milestone 8.9
            // added the two basic cards, and the first of them is the first root on an Attack rather
            // than on a Skill - which is why the order below is the table order rather than the Skills
            // alone.
            Assert.That(
                WaspCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .Select(action => $"{action.EffectRoot.Id.Value}@{action.EffectRoot.ContentRevision}"),
                Is.EqualTo(new[]
                {
                    "wasp-magnetic-dagger-attack-text@wasp-8-11-rev-1",
                    "wasp-static-barrier-skill@wasp-8-11-rev-1",
                    "wasp-shock-attack-text@wasp-8-11-rev-1",
                    "wasp-electrocute-attack-text@wasp-8-11-rev-1",
                    "wasp-electroblast-attack-text@wasp-8-11-rev-1",
                    "wasp-thunder-boomerang-attack-text@wasp-8-11-rev-1",
                    "wasp-lift-up-skill@wasp-8-11-rev-1",
                    "wasp-control-gravity-skill@wasp-8-11-rev-1",
                    "wasp-kinetic-repulse-skill@wasp-8-11-rev-1",
                    "wasp-center-of-mass-skill@wasp-8-11-rev-1",
                    "wasp-kinetic-blast-skill@wasp-8-11-rev-1",
                    "wasp-deflect-projectiles-defense-text@wasp-8-11-rev-1",
                    "wasp-telekinesis-skill@wasp-8-11-rev-1",
                    "wasp-reflect-projectiles-defense-text@wasp-8-11-rev-1",
                    "wasp-mass-telekinesis-skill@wasp-8-11-rev-1"
                }),
                "The two basic cards, the five telekinetic Skills, the two kinetic ones and milestone " +
                "8.10's two radius riders are composed; every other card's composition lands at its " +
                "own slice under a content-revision advance, so no half-implemented card text can run " +
                "in between.");

            var owedByCard = WaspCardContent.AllDefinitions
                .Select(definition => new
                {
                    Card = definition.Id.Value,
                    Owed = definition.Progression.IsUltimate
                        ? definition.UltimateRule.TextComposition
                        : definition.ActionDefinitions
                            .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                            .TextComposition
                })
                .ToArray();

            Assert.That(
                owedByCard.Select(entry => entry.Owed.IsAwaited
                    ? $"{entry.Card} -> {entry.Owed.OwningMilestoneId ?? entry.Owed.NamedTrigger}"
                    : $"{entry.Card} -> composed"),
                Is.EqualTo(new[]
                {
                    "wasp-magnetic-dagger -> composed",
                    "wasp-crackling-dagger -> first odd-player-count configuration",
                    "wasp-static-barrier -> composed",
                    "wasp-shock -> composed",
                    "wasp-electrocute -> composed",
                    "wasp-charged-boomerang -> composed",
                    "wasp-electroblast -> composed",
                    "wasp-thunder-boomerang -> composed",
                    "wasp-lift-up -> composed",
                    "wasp-control-gravity -> composed",
                    "wasp-kinetic-repulse -> composed",
                    "wasp-center-of-mass -> composed",
                    "wasp-kinetic-blast -> composed",
                    "wasp-stop-projectiles -> composed",
                    "wasp-deflect-projectiles -> composed",
                    "wasp-telekinesis -> composed",
                    "wasp-reflect-projectiles -> composed",
                    "wasp-mass-telekinesis -> composed",
                    "wasp-high-voltage -> 8.13"
                }),
                "Each card is either composed or owned by the slice milestone 8.2's capability matrix " +
                "assigned it.");
        }

        [Test]
        public void CracklingDaggerShipsAsData_WithItsBehaviorDeferredUnderItsNamedTrigger()
        {
            var handicap = WaspCardContent.Definition(WaspCardContent.CracklingDagger);

            Assert.That(handicap.SourceContent.Kind, Is.EqualTo(CardSourceContentKind.Transcribed),
                "The 2026-08-21 decision deferred the composition, not the inventory.");
            Assert.That(
                handicap.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary).TextComposition.NamedTrigger,
                Is.EqualTo("first odd-player-count configuration"));

            Assert.That(
                WaspCardContent.CreateDealtCards(new HeroId("wasp-red"), "red")
                    .Any(card => card.DefinitionReference.DefinitionId.Equals(
                        WaspCardContent.CracklingDagger)),
                Is.False,
                "p.5 puts it in the box, so a match never holds an instance of it.");
        }

        [Test]
        public void EveryDefinitionIsVerifiedOfficialAtOneRevision_WithItsTranscriptionCarried()
        {
            Assert.That(
                WaspCardContent.AllDefinitions.Select(definition => definition.ContentAuthority).Distinct(),
                Is.EqualTo(new[] { CardContentAuthority.VerifiedOfficial }),
                "No synthetic test value stands in for Wasp authority.");

            Assert.That(
                WaspCardContent.AllDefinitions.Select(definition => definition.ContentRevision).Distinct(),
                Is.EqualTo(new[] { WaspCardContent.ContentRevision }));

            Assert.That(
                WaspCardContent.AllDefinitions.All(definition =>
                    definition.SourceContent.Kind == CardSourceContentKind.Transcribed),
                Is.True,
                "All nineteen faces print rules text.");

            Assert.That(
                WaspCardContent.Definition(WaspCardContent.StopProjectiles).SourceContent.PrintedText,
                Is.EqualTo("Block a ranged attack."));
            Assert.That(
                WaspCardContent.Definition(WaspCardContent.ChargedBoomerang).SourceContent.PrintedText,
                Is.EqualTo(
                    "Target a unit in range and not in a straight line. ( Adjacent units are in a " +
                    "straight line as well. )"),
                "The face's reminder text, not the rulebook p.9 illustration's different wording: card " +
                "text outranks the rulebook under the ledger's authority hierarchy.");
            Assert.That(
                WaspCardContent.Definition(WaspCardContent.ThunderBoomerang).SourceContent.PrintedText,
                Does.Contain("If you targeted a hero may repeat once"),
                "The face prints no comma after \"hero\" and the transcription keeps its punctuation.");
        }

        [Test]
        public void HighVoltagePrintsItsRadiusAndNoInitiative_AndOwesItsAbilityToItsMilestone()
        {
            var ultimate = WaspCardContent.Definition(WaspCardContent.HighVoltage);

            Assert.That(ultimate.Initiative.IsDefined, Is.False,
                "p.16's Ultimate is never played, so it prints no Initiative.");
            Assert.That(ultimate.ActionDefinitions, Is.Empty,
                "It is placed beside the dashboard rather than performed.");
            Assert.That(ultimate.UltimateRule.Modification, Is.Null);
            Assert.That(ultimate.UltimateRule.TextComposition.OwningMilestoneId, Is.EqualTo("8.13"),
                "Its ability is a trigger, which none of RuleModification's five families expresses; " +
                "the debt is stated rather than filled with a stand-in rule.");
            Assert.That(
                ultimate.UltimateRule.PrintedValues.Single(),
                Is.EqualTo(new PrintedValueDefinition(PrintedValueChannel.Radius, 3)),
                "The Radius its ability reads, and the icon the 2026-08-21 answer to ledger question " +
                "12 lets an item raise at milestone 8.14.");
        }

        /// <summary>
        /// The observable outcome: verified definitions and instances use the lifecycle and setup
        /// machinery the synthetic content already runs on, with no path of their own.
        /// </summary>
        [Test]
        public void TheDealtCards_PassTheInitialSetupValidatorAndRunTheOrdinaryLifecycle()
        {
            var owner = new HeroId("wasp-red");
            var cards = WaspCardContent.CreateDealtCards(owner, "red");

            Assert.That(cards, Has.Count.EqualTo(18));
            Assert.That(cards.Count(card => card.State.IsInitialInHand), Is.EqualTo(5));
            Assert.That(cards.Count(card => card.State.IsInitialInDeck), Is.EqualTo(13));
            Assert.That(cards.Select(card => card.Id.Value).Distinct().Count(), Is.EqualTo(18));

            Assert.DoesNotThrow(() =>
                CardInitialSetupValidator.Validate(WaspCardContent.DealtDefinitions, cards));

            var handCard = cards.First(card => card.State.IsInitialInHand);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(handCard), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(handCard), Is.True);
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(handCard), Is.True);
            Assert.That(handCard.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(handCard.State.HasBeenPlayed, Is.True);

            var deckCard = cards.First(card => card.State.IsInitialInDeck);
            Assert.That(CardLifecycleAuthority.TryTakeFromDeckIntoHand(deckCard), Is.True);
            Assert.That(deckCard.State.Location, Is.EqualTo(CardLocation.Hand));

            var ultimate = cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.HighVoltage));
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "p.16 places an unlocked Ultimate faceup beside the dashboard.");
        }

        [Test]
        public void TwoMirroredSeatsGetDistinctInstanceIdentities()
        {
            var red = WaspCardContent.CreateDealtCards(new HeroId("wasp-red"), "red");
            var blue = WaspCardContent.CreateDealtCards(new HeroId("wasp-blue"), "blue");

            Assert.That(
                red.Select(card => card.Id.Value).Intersect(blue.Select(card => card.Id.Value)),
                Is.Empty,
                "Two mirrored Wasps deal the same eighteen definitions and hold thirty-six distinct " +
                "instances - the count milestone 8.4's smoke marker re-verifies.");
            Assert.That(red.Count + blue.Count, Is.EqualTo(36));
        }

        private static string Describe(CardDefinition definition)
        {
            var primary = definition.ActionDefinitions
                .FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var secondaryDefense = definition.ActionDefinitions
                .FirstOrDefault(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction);
            var secondaryMovement = definition.ActionDefinitions
                .FirstOrDefault(action => action.Kind == ActionKind.Movement);

            var progression = definition.Progression;
            var colourAndTier = progression.IsUltimate
                ? "Ultimate IV"
                : progression.IsBasic
                    ? $"{progression.Color} Basic"
                    : $"{progression.Color} {progression.Tier}";

            return string.Join("|", new[]
            {
                definition.Id.Value,
                definition.DisplayName,
                $"Init {(definition.Initiative.IsDefined ? definition.Initiative.Value.ToString() : "-")}",
                $"SecDef {Amount(secondaryDefense, PrintedValueChannel.Defense)}",
                $"SecMov {Amount(secondaryMovement, PrintedValueChannel.Movement)}",
                DescribePrimary(definition, primary),
                colourAndTier,
                $"item {(progression.HasItemIcon ? progression.ItemIcon.ToString() : "-")}"
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
            return $"{primary.Id.Value} {primary.Kind} {subtype} {Values(primary.BaseValues)}";
        }

        private static string Values(IReadOnlyList<PrintedValueDefinition> values) =>
            "[" + string.Join(", ", values.Select(value => $"{value.Channel} {value.Amount}")) + "]";

        private static string Amount(ActionDefinition action, PrintedValueChannel channel) =>
            action == null
                ? "-"
                : action.BaseValues.Single(value => value.Channel == channel).Amount.ToString();
    }
}
