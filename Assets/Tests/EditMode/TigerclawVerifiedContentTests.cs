using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.23. Tigerclaw the Cutpurse's nineteen printed faces as definition content, checked
    /// against the source inventory milestone 8.18d verified rather than against themselves.
    ///
    /// The printed-value assertion below is deliberately a literal table in the shard's own column
    /// order, for the reason the four verified sets before his record: a test that derived its
    /// expectations from the definitions would pass whatever the definitions happened to say. This one
    /// fails if a single number, colour, Tier, icon or name drifts from
    /// <c>Docs/AI/Rules/HERO_TIGERCLAW.md</c>, which is the document the extracted faces were
    /// transcribed into.
    ///
    /// <para>His edition answer is the strongest of the five: printed p.15 reproduces Light-Fingered,
    /// Pick Pocket and Poisoned Dagger and uses them for its whole Upgrade example, and printed p.16
    /// reproduces Pick Pocket, Poisoned Dagger, Cloak and Daggers and the dashboard. Four unique faces
    /// and the dashboard match the extraction exactly, which is a check no assertion here can make and
    /// the shard records instead.</para>
    /// </summary>
    public sealed class TigerclawVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the shard's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "tigerclaw-blink-strike|Blink Strike|Init 13|SecDef 1|SecMov 1|primary-attack Attack NonRanged [Attack 3]|Gold Basic|item -",
            "tigerclaw-nimble-strike|Nimble Strike|Init 12|SecDef 1|SecMov 1|primary-attack Attack NonRanged [Attack 2]|Gold Basic|item -",
            "tigerclaw-blend-into-shadows|Blend Into Shadows|Init 6|SecDef 2|SecMov -|primary-skill Skill None [Radius 2]|Silver Basic|item -",
            "tigerclaw-hit-and-run|Hit and Run|Init 9|SecDef 3|SecMov 4|primary-attack Attack NonRanged [Attack 3]|Red I|item -",
            "tigerclaw-combat-reflexes|Combat Reflexes|Init 9|SecDef 3|SecMov 4|primary-attack Attack NonRanged [Attack 4]|Red II|item Initiative",
            "tigerclaw-backstab|Backstab|Init 9|SecDef 5|SecMov 5|primary-attack Attack NonRanged [Attack 5]|Red II|item Defense",
            "tigerclaw-leaping-strike|Leaping Strike|Init 10|SecDef 4|SecMov 4|primary-attack Attack NonRanged [Attack 4]|Red III|item Radius",
            "tigerclaw-backstab-with-a-ballista|Backstab with a Ballista|Init 10|SecDef 6|SecMov 5|primary-ranged-attack Attack Ranged [Attack 5, Range 1]|Red III|item Defense",
            "tigerclaw-dodge|Dodge|Init 10|SecDef -|SecMov 3|primary-block Defense Block []|Blue I|item -",
            "tigerclaw-sidestep|Sidestep|Init 11|SecDef -|SecMov 3|primary-block Defense Block []|Blue II|item Attack",
            "tigerclaw-parry|Parry|Init 11|SecDef -|SecMov 3|primary-block Defense Block []|Blue II|item Defense",
            "tigerclaw-evade|Evade|Init 11|SecDef -|SecMov 3|primary-block Defense Block []|Blue III|item Attack",
            "tigerclaw-riposte|Riposte|Init 11|SecDef -|SecMov 3|primary-block Defense Block []|Blue III|item Range",
            "tigerclaw-light-fingered|Light-Fingered|Init 2|SecDef 1|SecMov 3|primary-skill Skill None []|Green I|item -",
            "tigerclaw-pick-pocket|Pick Pocket|Init 2|SecDef 1|SecMov 3|primary-skill Skill None []|Green II|item Attack",
            "tigerclaw-poisoned-dagger|Poisoned Dagger|Init 2|SecDef 1|SecMov 3|primary-skill Skill None [Range 3]|Green II|item Initiative",
            "tigerclaw-master-thief|Master Thief|Init 1|SecDef 2|SecMov 3|primary-skill Skill None []|Green III|item Movement",
            "tigerclaw-poisoned-dart|Poisoned Dart|Init 1|SecDef 2|SecMov 3|primary-skill Skill None [Range 3]|Green III|item Initiative",
            "tigerclaw-cloak-and-daggers|Cloak and Daggers|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                TigerclawCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.18d verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        [Test]
        public void TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(TigerclawCardContent.AllDefinitions, Has.Count.EqualTo(19),
                "p.6's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(TigerclawCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(TigerclawCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { TigerclawCardContent.NimbleStrike }),
                "p.6 returns the Handicap card to the box at setup.");

            Assert.That(
                TigerclawCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    TigerclawCardContent.BlinkStrike,
                    TigerclawCardContent.BlendIntoShadows,
                    TigerclawCardContent.HitAndRun,
                    TigerclawCardContent.Dodge,
                    TigerclawCardContent.LightFingered
                }),
                "p.6 step 11 deals the three Tier I cards and the two basic cards with no Tier.");

            Assert.That(
                TigerclawCardContent.StartingHand
                    .Select(id => TigerclawCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.15: \"exactly 5 cards, 1 of each color, in your hand\".");

            Assert.That(TigerclawCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                TigerclawCardContent.Deck.Concat(TigerclawCardContent.StartingHand)
                    .Concat(TigerclawCardContent.ReturnedToBoxAtSetup)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal),
                Is.EqualTo(TigerclawCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal)),
                "Every face belongs to exactly one of the three places p.6 puts it.");

            foreach (var colour in new[] { CardColor.Red, CardColor.Green, CardColor.Blue })
            {
                var set = TigerclawCardContent.AllDefinitions
                    .Where(definition => definition.Progression.Color == colour)
                    .ToArray();
                Assert.That(
                    set.Select(definition => definition.Progression.Tier).OrderBy(tier => tier),
                    Is.EqualTo(new[] { CardTier.I, CardTier.II, CardTier.II, CardTier.III, CardTier.III }),
                    "Each colour set is one Tier I, two Tier II and two Tier III; " + colour + " is not.");
            }
        }

        /// <summary>
        /// The supertype split the shard records without exception, and the two things that make his
        /// set different from the four before it: every Blue card is a p.14 Block printed as a Defense
        /// primary, and every non-Ultimate face but one prints a Movement icon. Blend Into Shadows is
        /// that one exception: milestone 8.35z20 corrected its secondary ribbon icon from a phantom
        /// Movement 2 to the Defense 2 the face actually prints (the one-icon slot-position trap - see
        /// <c>Docs/AI/Rules/HERO_TIGERCLAW.md</c>'s settled-corrections block), so it now joins the
        /// numeric-secondary-Defense group instead of the secondary-Movement group.
        /// </summary>
        [Test]
        public void EveryBlueCardIsABlock_AndEveryNonUltimateFacePrintsMovement()
        {
            foreach (var definition in TigerclawCardContent.AllDefinitions)
            {
                var primary = Primary(definition);
                if (definition.Progression.IsUltimate)
                {
                    Assert.That(primary, Is.Null, "p.16's Ultimate is not an action.");
                    continue;
                }

                var isBlendIntoShadows = definition.Id.Equals(TigerclawCardContent.BlendIntoShadows);
                Assert.That(
                    definition.ActionDefinitions.Any(action =>
                        action.Kind == ActionKind.Movement &&
                        action.Origin == ActionDefinitionOrigin.PrintedSecondary),
                    Is.EqualTo(!isBlendIntoShadows),
                    isBlendIntoShadows
                        ? definition.Id.Value + " prints no secondary Movement icon; it is the one " +
                          "exception in the set, corrected at milestone 8.35z20."
                        : definition.Id.Value + " prints a secondary Movement icon; every non-Ultimate " +
                          "face but Blend Into Shadows does.");

                Assert.That(
                    definition.ActionDefinitions.Any(action =>
                        action.Kind == ActionKind.Defense &&
                        action.Origin == ActionDefinitionOrigin.Reaction),
                    Is.EqualTo(isBlendIntoShadows || definition.Progression.Color != CardColor.Blue),
                    definition.Id.Value + " must print a numeric secondary Defense unless it is a " +
                    "Blue Block, and Blend Into Shadows now does too.");

                var expected =
                    definition.Progression.Color == CardColor.Blue ? ActionKind.Defense
                    : definition.Progression.Color == CardColor.Green ||
                        definition.Id.Equals(TigerclawCardContent.BlendIntoShadows) ? ActionKind.Skill
                    : ActionKind.Attack;
                Assert.That(primary.Kind, Is.EqualTo(expected),
                    definition.Id.Value + " prints a " + expected + " primary.");

                Assert.That(
                    primary.DefenseResolution == PrintedDefenseResolution.Block,
                    Is.EqualTo(definition.Progression.Color == CardColor.Blue),
                    "All five Blue cards are p.14 Blocks and nothing else of his is.");
            }

            Assert.That(
                TigerclawCardContent.AllDefinitions.Count(definition =>
                    Primary(definition)?.DefenseResolution == PrintedDefenseResolution.Block),
                Is.EqualTo(5));
        }

        /// <summary>
        /// The Block conditions, which is what milestone 8.23 had to move onto the card: p.14 resolves
        /// a Block by its own printed condition, and his is the first set to print both arms.
        /// </summary>
        [Test]
        public void ThreeBlocksStopARangedAttackAndTwoStopANonRangedOne()
        {
            var byCondition = TigerclawCardContent.AllDefinitions
                .Select(definition => new
                {
                    definition.Id,
                    Primary = Primary(definition)
                })
                .Where(value => value.Primary?.DefenseResolution == PrintedDefenseResolution.Block)
                .GroupBy(value => value.Primary.BlockCondition)
                .ToDictionary(group => group.Key, group => group.Select(value => value.Id).ToArray());

            Assert.That(
                byCondition[PrintedBlockCondition.AttackIsRanged],
                Is.EqualTo(new[]
                {
                    TigerclawCardContent.Dodge,
                    TigerclawCardContent.Sidestep,
                    TigerclawCardContent.Evade
                }),
                "\"Block a ranged attack\" - the arm every Block before his printed.");

            Assert.That(
                byCondition[PrintedBlockCondition.AttackIsNotRanged],
                Is.EqualTo(new[] { TigerclawCardContent.Parry, TigerclawCardContent.Riposte }),
                "\"Block a non-ranged attack\" - the arm no earlier verified face prints, and the " +
                "reason the condition had to become the card's own word.");

            Assert.That(
                TigerclawCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.DefenseResolution != PrintedDefenseResolution.Block)
                    .All(action => action.BlockCondition == PrintedBlockCondition.None),
                Is.True,
                "Nothing that is not a Block states a Block condition.");
        }

        [Test]
        public void TheUpgradePairsArePageFifteens_AndTheWholeSetPassesTheProgressionValidator()
        {
            var pairs = new (CardDefinitionId From, CardDefinitionId[] To)[]
            {
                (TigerclawCardContent.HitAndRun,
                    new[] { TigerclawCardContent.CombatReflexes, TigerclawCardContent.Backstab }),
                (TigerclawCardContent.CombatReflexes,
                    new[] { TigerclawCardContent.LeapingStrike, TigerclawCardContent.BackstabWithABallista }),
                (TigerclawCardContent.Backstab,
                    new[] { TigerclawCardContent.LeapingStrike, TigerclawCardContent.BackstabWithABallista }),
                (TigerclawCardContent.Dodge,
                    new[] { TigerclawCardContent.Sidestep, TigerclawCardContent.Parry }),
                (TigerclawCardContent.Sidestep,
                    new[] { TigerclawCardContent.Evade, TigerclawCardContent.Riposte }),
                (TigerclawCardContent.Parry,
                    new[] { TigerclawCardContent.Evade, TigerclawCardContent.Riposte }),
                (TigerclawCardContent.LightFingered,
                    new[] { TigerclawCardContent.PickPocket, TigerclawCardContent.PoisonedDagger }),
                (TigerclawCardContent.PickPocket,
                    new[] { TigerclawCardContent.MasterThief, TigerclawCardContent.PoisonedDart }),
                (TigerclawCardContent.PoisonedDagger,
                    new[] { TigerclawCardContent.MasterThief, TigerclawCardContent.PoisonedDart })
            };

            foreach (var (from, to) in pairs)
            {
                Assert.That(
                    TigerclawCardContent.Definition(from).Progression.NextTierCandidateIds,
                    Is.EqualTo(to),
                    from.Value + " upgrades into the two cards of its colour one Tier higher (p.15).");
            }

            foreach (var tierThree in new[]
                     {
                         TigerclawCardContent.LeapingStrike,
                         TigerclawCardContent.BackstabWithABallista,
                         TigerclawCardContent.Evade,
                         TigerclawCardContent.Riposte,
                         TigerclawCardContent.MasterThief,
                         TigerclawCardContent.PoisonedDart
                     })
            {
                Assert.That(
                    TigerclawCardContent.Definition(tierThree).Progression.NextTierCandidateIds,
                    Is.Empty,
                    "A Tier III card names none: the only card above it is the Ultimate, which p.16 " +
                    "unlocks rather than upgrades into.");
            }

            Assert.DoesNotThrow(
                () => CardProgressionValidator.Validate(TigerclawCardContent.AllDefinitions),
                "The whole set satisfies p.15's colour, Tier and pairing rules.");
        }

        /// <summary>
        /// p.16's item icons, and the fact about his set the shard singles out: every non-Ultimate card
        /// but the Silver basic carries Movement. Milestone 8.35z20 corrected Blend Into Shadows'
        /// secondary from a phantom Movement 2 to the Defense 2 it actually prints (see
        /// <c>Docs/AI/Rules/HERO_TIGERCLAW.md</c>'s settled-corrections block), which puts his Silver
        /// basic in the same shape every other verified hero's Silver basic already has - a Defense-only
        /// secondary, no Movement - rather than the unique full reach the pre-correction record claimed.
        /// </summary>
        [Test]
        public void HisTwelveItemIconsReachAllFourDashboardSlots_WithTheWidestMovementReach()
        {
            var icons = TigerclawCardContent.AllDefinitions
                .Where(definition => definition.Progression.HasItemIcon)
                .GroupBy(definition => definition.Progression.ItemIcon)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(icons.Values.Sum(), Is.EqualTo(12),
                "p.16 gives every Tier II and Tier III card an item icon; he has twelve of them.");
            Assert.That(icons[PrintedValueChannel.Attack], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Defense], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Initiative], Is.EqualTo(3));
            Assert.That(icons[PrintedValueChannel.Movement], Is.EqualTo(1));
            Assert.That(icons[PrintedValueChannel.Range], Is.EqualTo(1));
            Assert.That(icons[PrintedValueChannel.Radius], Is.EqualTo(1));

            Assert.That(
                TigerclawCardContent.DealtDefinitions.Count(definition =>
                    definition.ActionDefinitions.Any(action =>
                        action.BaseValues.Any(value =>
                            value.Channel == PrintedValueChannel.Movement))),
                Is.EqualTo(16),
                "Sixteen of the seventeen non-Ultimate dealt cards print Movement; Blend Into Shadows " +
                "(the Silver basic) is the one exception, corrected at milestone 8.35z20 - the same " +
                "shape every other verified hero's Silver basic already has.");

            Assert.That(
                TigerclawCardContent.Definition(TigerclawCardContent.CloakAndDaggers)
                    .UltimateRule.PrintedValues,
                Is.Empty,
                "Cloak and Daggers prints no Range or Radius icon, so ledger question 12's " +
                "Ultimate-item interpretation has no Tigerclaw instance.");
        }

        /// <summary>
        /// Which of rulebook p.14's moments each composed body is applied at - and the two faces that
        /// name two, which is what milestone 8.23 widened the model for.
        /// </summary>
        [Test]
        public void TwoFacesPrintCardTextAtBothOfPageFourteensAttackerMoments()
        {
            var twoMoment = new[]
            {
                TigerclawCardContent.CombatReflexes,
                TigerclawCardContent.LeapingStrike
            };

            foreach (var definition in TigerclawCardContent.AllDefinitions)
            {
                var primary = Primary(definition);
                if (primary == null) continue;

                var hasSecond = twoMoment.Contains(definition.Id);
                Assert.That(primary.SecondCardTextRoot != null, Is.EqualTo(hasSecond),
                    definition.Id.Value + " prints " + (hasSecond ? "two bodies" : "at most one") +
                    " of card text.");

                if (!hasSecond) continue;

                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack));
                Assert.That(primary.SecondCardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
                Assert.That(
                    primary.CardTextAt(CardTextTiming.BeforeTheAttack),
                    Is.SameAs(primary.EffectRoot));
                Assert.That(
                    primary.CardTextAt(CardTextTiming.AfterTheAttack),
                    Is.SameAs(primary.SecondCardTextRoot));
                Assert.That(primary.CardTextAt(CardTextTiming.DefenseAction), Is.Null,
                    "p.14's third moment belongs to the defender's own card.");
            }

            Assert.That(
                Primary(TigerclawCardContent.Definition(TigerclawCardContent.HitAndRun))
                    .CardTextTiming,
                Is.EqualTo(CardTextTiming.AfterTheAttack),
                "Hit and Run prints one clause and declares it as its first body: the pair is \"the " +
                "moments this face prints\", not \"before and after\".");
        }

        /// <summary>
        /// The two faces printing p.14's <c>+</c> convention, and the one face restricting the origin
        /// of a Defense action. Both are declarations on the Attack rather than effect nodes.
        /// </summary>
        [Test]
        public void TwoBackstabsRaiseTheirAttack_AndOnlyTheBallistaForbidsAPrimaryDefense()
        {
            foreach (var definition in TigerclawCardContent.AllDefinitions)
            {
                var primary = Primary(definition);
                if (primary == null) continue;

                var raises = definition.Id.Equals(TigerclawCardContent.Backstab) ||
                    definition.Id.Equals(TigerclawCardContent.BackstabWithABallista);
                Assert.That(primary.RosterValueContributions.Count, Is.EqualTo(raises ? 1 : 0),
                    definition.Id.Value + (raises ? " prints 5+." : " prints a plain value."));

                if (raises)
                {
                    var contribution = primary.RosterValueContributions.Single();
                    Assert.That(contribution.Channel, Is.EqualTo(PrintedValueChannel.Attack));
                    Assert.That(contribution.AmountPerMatch, Is.EqualTo(2), "\"+2 Attack\".");
                    Assert.That(
                        contribution.Query,
                        Is.EqualTo(RosterValueQueryKind.FriendlyUnitAdjacentToTargetExcludingActor),
                        "\"If a friendly unit is adjacent to the target\", with the parenthesised " +
                        "reminder's own exclusion of Tigerclaw himself.");
                }

                Assert.That(
                    primary.DefenseOfferRestriction.IsDefined,
                    Is.EqualTo(definition.Id.Equals(TigerclawCardContent.BackstabWithABallista)),
                    definition.Id.Value + " restricts the origin of a Defense action only where its " +
                    "face says so.");
            }

            Assert.That(
                Primary(TigerclawCardContent.Definition(TigerclawCardContent.BackstabWithABallista))
                    .DefenseOfferRestriction.ForbiddenOrigin,
                Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary),
                "\"the target cannot perform a primary action to defend\" - p.14's ordinary secondary " +
                "Defense remains legal, which is what naming the origin rather than the card keeps.");
        }

        /// <summary>
        /// The Poison marker: two strengths of one physical supply, whose rule is on the marker rather
        /// than on the card that gave it.
        /// </summary>
        [Test]
        public void ThePoisonMarkerCarriesItsOwnValues_AtTwoStrengthsOnOneSupply()
        {
            var strengths = new[]
            {
                (Definition: TigerclawComponentContent.PoisonOneDefinition, Amount: -1),
                (Definition: TigerclawComponentContent.PoisonTwoDefinition, Amount: -2)
            };

            foreach (var (definition, amount) in strengths)
            {
                Assert.That(definition.SupplyCount,
                    Is.EqualTo(TigerclawComponentContent.PoisonSupplyCount),
                    "Both strengths declare the one physical stack; p.15's Level Up happens after p.8 " +
                    "has returned every marker, so they can never both be out from his own deck.");
                Assert.That(definition.ReturnCondition,
                    Is.EqualTo(MarkerReturnCondition.GeneralCleanup),
                    "p.8 returns all markers at end of round and p.13 returns a defeated hero's.");

                Assert.That(
                    definition.Properties.Select(property => property.Modification.Channel),
                    Is.EqualTo(new[]
                    {
                        PrintedValueChannel.Initiative,
                        PrintedValueChannel.Attack,
                        PrintedValueChannel.Defense
                    }),
                    "The face names three channels and no other value.");
                Assert.That(
                    definition.Properties.Select(property => property.Modification.Amount).Distinct(),
                    Is.EqualTo(new[] { amount }),
                    "All three move by the same printed amount.");
                Assert.That(
                    definition.Properties.All(property =>
                        property.Modification.Kind == RuleModificationKind.ValueModifier),
                    Is.True);
            }
        }

        [Test]
        public void EveryDefinitionIsVerifiedOfficialAtOneRevision_WithItsTranscriptionCarried()
        {
            foreach (var definition in TigerclawCardContent.AllDefinitions)
            {
                Assert.That(definition.ContentAuthority,
                    Is.EqualTo(CardContentAuthority.VerifiedOfficial));
                Assert.That(definition.ContentRevision,
                    Is.EqualTo(TigerclawCardContent.ContentRevision));
                Assert.That(definition.SourceContent.PrintedText, Is.Not.Empty,
                    definition.Id.Value + " carries its transcribed face.");
            }

            Assert.That(
                Primary(TigerclawCardContent.Definition(TigerclawCardContent.NimbleStrike))
                    .TextComposition.IsAwaited,
                Is.True,
                "p.6 returns the Handicap card to the box, so its composition is deferred - and a " +
                "deferred action carries no effect root, which is why its words ship here and its " +
                "nodes do not.");
            Assert.That(
                Primary(TigerclawCardContent.Definition(TigerclawCardContent.NimbleStrike)).EffectRoot,
                Is.Null);

            foreach (var definition in TigerclawCardContent.DealtDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    Assert.That(action.TextComposition.IsAwaited, Is.False,
                        definition.Id.Value + " is dealt, so nothing about it is owed.");
                }
            }
        }

        /// <summary>
        /// Every composed body validates against the catalog this build registers - both bodies, where
        /// a face prints two.
        /// </summary>
        [Test]
        public void EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Capabilities;

            foreach (var definition in TigerclawCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    foreach (var root in new[] { action.EffectRoot, action.SecondCardTextRoot })
                    {
                        if (root == null) continue;
                        Assert.That(
                            EffectDefinitionValidator.Validate(root, catalog),
                            Is.Empty,
                            definition.Id.Value + " composes a body this build cannot run.");
                    }
                }
            }

            Assert.That(
                EffectDefinitionValidator.Validate(
                    TigerclawEffectContent.CloakAndDaggersTriggeredEffect(
                        TigerclawCardContent.ContentRevision),
                    catalog),
                Is.Empty,
                "p.16's permanent ability runs on the same registered contracts.");
        }

        [Test]
        public void TheDealtCards_PassTheInitialSetupValidator()
        {
            var owner = new HeroId("tigerclaw-content-hero");
            var cards = TigerclawCardContent.CreateDealtCards(owner, "tigerclaw-content");

            Assert.That(cards, Has.Count.EqualTo(18));
            Assert.DoesNotThrow(
                () => CardInitialSetupValidator.Validate(
                    TigerclawCardContent.DealtDefinitions.ToArray(), cards.ToArray()),
                "p.6's deal is five in hand and thirteen in the deck, each unplayed and unresolved.");
            Assert.That(
                cards.Count(card => card.State.Location == CardLocation.Hand),
                Is.EqualTo(5));
        }

        [Test]
        public void TwoSeatsDealingTigerclaw_GetDistinctInstanceIdentities()
        {
            var first = TigerclawCardContent.CreateDealtCards(new HeroId("red"), "red-tigerclaw");
            var second = TigerclawCardContent.CreateDealtCards(new HeroId("blue"), "blue-tigerclaw");

            Assert.That(
                first.Select(card => card.Id.Value)
                    .Intersect(second.Select(card => card.Id.Value)),
                Is.Empty,
                "Card instance identity is per seat, so one printed set can be dealt twice.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers, in the shape the four earlier verified-content fixtures use
        // -------------------------------------------------------------------------------------------

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
