using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.24. Xargatha the Changed's nineteen printed faces as definition content, checked
    /// against the source inventory milestone 8.18e verified rather than against themselves.
    ///
    /// The printed-value assertion below is deliberately a literal table in the shard's own column
    /// order, for the reason the five verified sets before hers record: a test that derived its
    /// expectations from the definitions would pass whatever the definitions happened to say. This one
    /// fails if a single number, colour, Tier, icon or name drifts from
    /// <c>Docs/AI/Rules/HERO_XARGATHA.md</c>, which is the document the extracted faces were
    /// transcribed into.
    ///
    /// <para>Her edition answer is confirmed with no conflicting current-face wording: printed p.13
    /// reproduces Long Thrust and matches the extracted face in every visible name, colour, Tier,
    /// Initiative, secondary value, action, Range, item icon and word - a check no assertion here can
    /// make and the shard records instead.</para>
    /// </summary>
    public sealed class XargathaVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the shard's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "xargatha-cleave|Cleave|Init 11|SecDef 2|SecMov 1|primary-attack Attack NonRanged [Attack 4]|Gold Basic|item -",
            "xargatha-desperate-cleave|Desperate Cleave|Init 10|SecDef 1|SecMov 1|primary-attack Attack NonRanged [Attack 3]|Gold Basic|item -",
            "xargatha-sirens-call|Siren's Call|Init 3|SecDef -|SecMov 3|primary-skill Skill None [Range 3]|Silver Basic|item -",
            "xargatha-dangerous-slash|Dangerous Slash|Init 7|SecDef 6|SecMov 5|primary-attack Attack NonRanged [Attack 5]|Red I|item -",
            "xargatha-deadly-swipe|Deadly Swipe|Init 8|SecDef 7|SecMov 5|primary-attack Attack NonRanged [Attack 5]|Red II|item Initiative",
            "xargatha-long-thrust|Long Thrust|Init 8|SecDef 4|SecMov 4|primary-ranged-attack Attack Ranged [Attack 3, Range 1]|Red II|item Defense",
            "xargatha-lethal-spin|Lethal Spin|Init 8|SecDef 7|SecMov 5|primary-attack Attack NonRanged [Attack 5]|Red III|item Radius",
            "xargatha-rapid-thrusts|Rapid Thrusts|Init 8|SecDef 4|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 1]|Red III|item Movement",
            "xargatha-stone-gaze|Stone Gaze|Init 9|SecDef 5|SecMov 3|primary-skill Skill None [Radius 2]|Blue I|item -",
            "xargatha-petrifying-stare|Petrifying Stare|Init 10|SecDef 6|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Defense",
            "xargatha-fresh-converts|Fresh Converts|Init 10|SecDef 6|SecMov 3|primary-skill Skill None []|Blue II|item Attack",
            "xargatha-turn-into-statues|Turn Into Statues|Init 10|SecDef 6|SecMov 3|primary-skill Skill None [Radius 4]|Blue III|item Defense",
            "xargatha-devoted-followers|Devoted Followers|Init 10|SecDef 6|SecMov 3|primary-skill Skill None []|Blue III|item Range",
            "xargatha-charm|Charm|Init 5|SecDef 3|SecMov -|primary-movement Movement None [Movement 2, Radius 2]|Green I|item -",
            "xargatha-control|Control|Init 4|SecDef 3|SecMov -|primary-movement Movement None [Movement 2, Radius 2]|Green II|item Attack",
            "xargatha-constrict|Constrict|Init 4|SecDef 3|SecMov 2|primary-skill Skill None []|Green II|item Initiative",
            "xargatha-dominate|Dominate|Init 4|SecDef 4|SecMov -|primary-movement Movement None [Movement 2, Radius 2]|Green III|item Attack",
            "xargatha-final-embrace|Final Embrace|Init 4|SecDef 4|SecMov 2|primary-skill Skill None []|Green III|item Initiative",
            "xargatha-metamorphosis|Metamorphosis|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                XargathaCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.18e verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        [Test]
        public void TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(XargathaCardContent.AllDefinitions, Has.Count.EqualTo(19),
                "p.6's eighteen-card deck plus p.20's Handicap card.");
            Assert.That(XargathaCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(XargathaCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { XargathaCardContent.DesperateCleave }),
                "p.6 returns the Handicap card to the box at setup.");

            Assert.That(
                XargathaCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    XargathaCardContent.Cleave,
                    XargathaCardContent.SirensCall,
                    XargathaCardContent.DangerousSlash,
                    XargathaCardContent.StoneGaze,
                    XargathaCardContent.Charm
                }),
                "p.6 step 11 deals the three Tier I cards and the two basic cards with no Tier.");

            Assert.That(
                XargathaCardContent.StartingHand
                    .Select(id => XargathaCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.15: \"exactly 5 cards, 1 of each color, in your hand\".");

            Assert.That(XargathaCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                XargathaCardContent.Deck.Concat(XargathaCardContent.StartingHand)
                    .Concat(XargathaCardContent.ReturnedToBoxAtSetup)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal),
                Is.EqualTo(XargathaCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .OrderBy(id => id.Value, System.StringComparer.Ordinal)),
                "Every face belongs to exactly one of the three places p.6 puts it.");

            foreach (var colour in new[] { CardColor.Red, CardColor.Green, CardColor.Blue })
            {
                var set = XargathaCardContent.AllDefinitions
                    .Where(definition => definition.Progression.Color == colour)
                    .ToArray();
                Assert.That(
                    set.Select(definition => definition.Progression.Tier).OrderBy(tier => tier),
                    Is.EqualTo(new[] { CardTier.I, CardTier.II, CardTier.II, CardTier.III, CardTier.III }),
                    "Each colour set is one Tier I, two Tier II and two Tier III; " + colour + " is not.");
            }
        }

        /// <summary>
        /// The colour-to-action split her shard records without exception, and the absence that is
        /// content: she prints no Defense primary and no p.14 Block anywhere in the set.
        /// </summary>
        [Test]
        public void EveryColourHasItsOwnActionKind_AndSheNeverPrintsADefensePrimary()
        {
            foreach (var definition in XargathaCardContent.AllDefinitions
                .Where(value => value.Progression.Color == CardColor.Red))
            {
                Assert.That(Primary(definition).Kind, Is.EqualTo(ActionKind.Attack),
                    definition.DisplayName + " is Red, so its primary is an Attack.");
            }

            foreach (var definition in XargathaCardContent.AllDefinitions
                .Where(value => value.Progression.Color == CardColor.Blue))
            {
                Assert.That(Primary(definition).Kind, Is.EqualTo(ActionKind.Skill),
                    definition.DisplayName + " is Blue, so its primary is a Skill.");
            }

            Assert.That(
                XargathaCardContent.AllDefinitions
                    .Where(value => value.Progression.Color == CardColor.Green)
                    .Select(value => Primary(value).Kind)
                    .OrderBy(kind => kind.ToString(), System.StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    ActionKind.Movement, ActionKind.Movement, ActionKind.Movement,
                    ActionKind.Skill, ActionKind.Skill
                }.OrderBy(kind => kind.ToString(), System.StringComparer.Ordinal)),
                "Her Green branch splits three Movement primaries from two Skills.");

            Assert.That(
                XargathaCardContent.AllDefinitions
                    .Select(Primary)
                    .Where(action => action != null)
                    .Any(action => action.Kind == ActionKind.Defense),
                Is.False,
                "Xargatha prints no Defense primary and no p.14 Block. The absence is content: " +
                "Rules/HERO_XARGATHA.md records it, and it is what makes her set differ from " +
                "Tigerclaw's five Blocks.");

            Assert.That(
                XargathaCardContent.AllDefinitions
                    .Where(definition => !definition.Progression.IsUltimate)
                    .Count(definition => definition.ActionDefinitions.Any(action =>
                        action.Kind == ActionKind.Defense &&
                        action.Origin == ActionDefinitionOrigin.Reaction)),
                Is.EqualTo(17),
                "Seventeen faces can be discarded to perform a Defense action - every non-Ultimate " +
                "except Siren's Call, which is the only card in her set printing Movement without it.");
        }

        /// <summary>
        /// The three faces that place their own text either side of the Movement they print - the
        /// first verified content to say otherwise to p.11's before-movement default.
        /// </summary>
        [Test]
        public void TheThreeGreenMovements_PlaceTheirTextAroundTheAction()
        {
            foreach (var id in new[]
            {
                XargathaCardContent.Charm,
                XargathaCardContent.Control,
                XargathaCardContent.Dominate
            })
            {
                var primary = Primary(XargathaCardContent.Definition(id));
                Assert.That(primary.Kind, Is.EqualTo(ActionKind.Movement));
                Assert.That(primary.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.AroundTheAction),
                    id.Value + " prints \"before or after movement\", which is p.11's \"unless it " +
                    "says otherwise\" being said otherwise.");
                Assert.That(primary.EffectRoot, Is.Not.Null);
            }

            foreach (var id in new[] { XargathaCardContent.Charm, XargathaCardContent.Control })
            {
                Assert.That(
                    XargathaCardContent.Definition(id).ActionDefinitions
                        .Any(action => action.Origin == ActionDefinitionOrigin.PrintedSecondary),
                    Is.False,
                    id.Value + " prints its Movement icon as the primary, so the face carries no " +
                    "secondary Movement to print a dash for.");
            }
        }

        /// <summary>
        /// The five faces printing the <c>+</c>, and the word that decides which query each one reads.
        /// </summary>
        [Test]
        public void TheFiveScaledValues_ReadTheQueryTheirWordingNames()
        {
            var expected = new Dictionary<CardDefinitionId, string>
            {
                [XargathaCardContent.DangerousSlash] =
                    "Attack EnemyUnitsAdjacentToActorExcludingTarget 1",
                [XargathaCardContent.DeadlySwipe] =
                    "Attack EnemyUnitsAdjacentToActorExcludingTarget 2",
                [XargathaCardContent.LethalSpin] =
                    "Attack EnemyUnitsAdjacentToActorExcludingTarget 3",
                [XargathaCardContent.LongThrust] = "Range EnemyUnitsAdjacentToActor 1",
                [XargathaCardContent.RapidThrusts] = "Range EnemyUnitsAdjacentToActor 1"
            };

            foreach (var pair in expected)
            {
                var contributions = Primary(XargathaCardContent.Definition(pair.Key))
                    .RosterValueContributions;
                Assert.That(
                    contributions.Select(value =>
                        value.Channel + " " + value.Query + " " + value.AmountPerMatch),
                    Is.EqualTo(new[] { pair.Value }),
                    pair.Key.Value + " scales the wrong channel, query or amount. The three melee " +
                    "Attacks say \"each OTHER enemy unit\" and exclude the target; the two Ranged " +
                    "Attacks omit that word and count every adjacent enemy.");
            }

            foreach (var definition in XargathaCardContent.AllDefinitions
                .Where(value => !expected.ContainsKey(value.Id)))
            {
                var primary = Primary(definition);
                Assert.That(primary == null || primary.RosterValueContributions.Count == 0, Is.True,
                    definition.DisplayName + " prints a plain value, so it declares no contribution.");
            }
        }

        /// <summary>
        /// The repeat clause two of her Attacks print, and the narrowing that separates it from every
        /// repeat built before her.
        /// </summary>
        [Test]
        public void CleaveAndRapidThrusts_RepeatOnADifferentEnemyHero()
        {
            foreach (var id in new[] { XargathaCardContent.Cleave, XargathaCardContent.RapidThrusts })
            {
                var primary = Primary(XargathaCardContent.Definition(id));
                Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack),
                    id.Value + " prints \"After the attack:\".");

                var repeats = primary.EffectRoot.Nodes
                    .Where(node => node.Kind == EffectNodeKind.RepeatAction)
                    .ToArray();
                Assert.That(repeats, Has.Length.EqualTo(1));
                Assert.That(repeats[0].RepeatTargetBinding,
                    Is.EqualTo(RepeatTargetBinding.DifferentEnemyHero),
                    "\"on a different enemy hero\" both excludes the unit this instance took and " +
                    "narrows what the repeated one may take.");
                Assert.That(repeats[0].MaximumRepeats, Is.EqualTo(1), "\"once\".");
                Assert.That(repeats[0].FailurePolicy, Is.EqualTo(EffectFailurePolicy.May), "\"May\".");
            }

            Assert.That(
                Primary(XargathaCardContent.Definition(XargathaCardContent.Cleave)).AttackTargetPolicy,
                Is.Null,
                "Cleave's printed instance targets \"a unit\", which is p.13's own word - the " +
                "narrowing to heroes is on the repeat, which is why its parenthesis can say the " +
                "first target may have been a minion.");
        }

        /// <summary>
        /// p.16's Ultimate, and the shape that makes it the first standing declaration in the build.
        /// </summary>
        [Test]
        public void Metamorphosis_IsAStandingDeclarationOfThreeRules()
        {
            var ultimate = XargathaCardContent
                .Definition(XargathaCardContent.Metamorphosis).UltimateRule;

            Assert.That(ultimate, Is.Not.Null);
            Assert.That(ultimate.RuleId, Is.EqualTo(XargathaEffectContent.MetamorphosisRuleId));
            Assert.That(ultimate.TriggeredEffect.HasValue, Is.False,
                "Metamorphosis runs nothing at a moment; it is simply true while she is on the board.");
            Assert.That(ultimate.TriggerCheckpoints, Is.Empty);
            Assert.That(ultimate.PrintedValues, Is.Empty,
                "Its face prints no Range or Radius icon - its inline Movement and Initiative icons " +
                "accompany numbers in card text, which p.16 expressly excludes from item increases.");

            Assert.That(
                ultimate.Modifications.Select(Describe),
                Is.EqualTo(new[]
                {
                    "DerivedValue Movement EnemyUnitsAdjacentToAffectedObject 1",
                    "DerivedValue Initiative EnemyUnitsAdjacentToAffectedObject 1",
                    "Permission MoveThroughObstacles"
                }),
                "Three declarations because the card prints three: two channels that scale with the " +
                "same live count, and a permission that scales with nothing.");
        }

        /// <summary>
        /// Her five p.17 Active effects: three petrification disks and two end-of-round sources.
        /// </summary>
        [Test]
        public void HerFiveActiveEffects_DeclareWhatTheirFacesPrint()
        {
            var petrifications = new Dictionary<string, int>
            {
                ["xargatha-stone-gaze-effect"] = 2,
                ["xargatha-petrifying-stare-effect"] = 3,
                ["xargatha-turn-into-statues-effect"] = 4
            };

            foreach (var pair in petrifications)
            {
                var effect = XargathaActiveEffectContent.Definitions
                    .Single(value => value.DefinitionId == pair.Key);

                Assert.That(effect.DurationKind, Is.EqualTo(ActiveEffectDurationKind.NextTurn),
                    pair.Key + " prints \"Next turn:\".");
                Assert.That(effect.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.HeroesInRadius));
                Assert.That(effect.AffectedUnitRelation,
                    Is.EqualTo(SourceTeamRelation.OpposingSource), "\"Enemy heroes in radius\".");
                Assert.That(effect.Radius.Source, Is.EqualTo(DeclaredRadiusSource.PrintedIcon),
                    "The face prints a Radius icon and its text says \"in radius\", so p.9 makes the " +
                    "icon the value and p.16's Radius item raises it.");
                Assert.That(effect.Radius.Value, Is.EqualTo(pair.Value));

                Assert.That(
                    effect.Modifications.Select(Describe),
                    Is.EqualTo(new[]
                    {
                        "EffectiveTypeOverlay Terrain",
                        "Restriction Movement"
                    }),
                    "One printed sentence, two declarations: heroes keep every hero rule and gain " +
                    "terrain as a second effective type, and they cannot perform a Movement action.");
            }

            foreach (var id in new[] { "xargatha-constrict-effect", "xargatha-final-embrace-effect" })
            {
                var effect = XargathaActiveEffectContent.Definitions
                    .Single(value => value.DefinitionId == id);

                Assert.That(effect.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisRound),
                    "The moment is inside the round the Skill was played in.");
                Assert.That(effect.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero),
                    "\"adjacent to you\" measures from her.");
                Assert.That(effect.TriggerCheckpoints,
                    Is.EqualTo(new[] { TimingCheckpoint.EndOfRoundBeforeMinionBattle }),
                    "Constrict's reminder names the position outright, and p.9 makes reminder text " +
                    "clarification rather than a rule Final Embrace lacks.");
                Assert.That(effect.TriggeredEffect.HasValue, Is.True);
                Assert.That(effect.Modifications, Is.Empty,
                    "Neither card changes a value; each runs a step at a moment.");
            }
        }

        [Test]
        public void EveryDefinitionIsVerifiedOfficialAtOneRevision_WithItsTranscriptionCarried()
        {
            foreach (var definition in XargathaCardContent.AllDefinitions)
            {
                Assert.That(definition.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial),
                    definition.DisplayName);
                Assert.That(definition.ContentRevision,
                    Is.EqualTo(XargathaCardContent.ContentRevision), definition.DisplayName);
                Assert.That(definition.SourceContent.PrintedText, Is.Not.Null.And.Not.Empty,
                    definition.DisplayName + " carries its transcription.");
            }
        }

        /// <summary>
        /// The one face p.6 puts back in the box carries its words and values and composes no body -
        /// the same deferral the five earlier sets record for their own Handicap card.
        /// </summary>
        [Test]
        public void TheHandicapCard_ShipsItsTranscriptionAndDefersItsComposition()
        {
            var handicap = XargathaCardContent.Definition(XargathaCardContent.DesperateCleave);
            var primary = Primary(handicap);

            Assert.That(handicap.SourceContent.PrintedText, Does.Contain("different enemy hero"));
            Assert.That(primary.TextComposition.IsAwaited, Is.True,
                "p.6 returns it to the box and the six-player pitch never brings it back.");
            Assert.That(primary.EffectRoot, Is.Null,
                "An action carrying its effect root owes no composition.");
            Assert.That(primary.AttackTargetPolicy.HasValue, Is.True);
            Assert.That(primary.AttackTargetPolicy.Value.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesOnly),
                "It prints \"Target a hero adjacent to you\" where the ordinary Gold card prints " +
                "\"a unit\" - which is on the card rather than in the deferral.");
        }

        [Test]
        public void EveryComposedEffect_PassesTheDefinitionValidatorAgainstTheBuildsCatalog()
        {
            var catalogs = MatchContentCatalogs.ForThisBuild();
            foreach (var definition in XargathaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    foreach (var effect in new[] { action.EffectRoot, action.SecondCardTextRoot })
                    {
                        if (effect == null)
                        {
                            continue;
                        }

                        Assert.That(
                            EffectDefinitionValidator.Validate(effect, catalogs.Capabilities),
                            Is.Empty,
                            definition.DisplayName + "'s composed text must bind to contracts this " +
                            "build registers.");
                    }
                }
            }

            foreach (var effect in new[]
            {
                XargathaActiveEffectContent.ConstrictTriggeredEffect.Definition,
                XargathaActiveEffectContent.FinalEmbraceTriggeredEffect.Definition
            })
            {
                Assert.That(
                    EffectDefinitionValidator.Validate(effect, catalogs.Capabilities),
                    Is.Empty,
                    effect.Id.Value + " runs from an Active effect's own trigger and binds to the " +
                    "same registered contracts.");
            }
        }

        [Test]
        public void TheDealtCards_PassTheInitialSetupValidator()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration
                .CreateStagedXargathaForInternalTest(TeamSide.Red);

            Assert.That(configuration.CardInstances
                    .Count(card => card.DefinitionReference.DefinitionId.Value
                        .StartsWith("xargatha-", System.StringComparison.Ordinal)),
                Is.EqualTo(18),
                "One seat deals p.6's eighteen cards.");
        }

        [Test]
        public void TwoSeatsDealingXargatha_GetDistinctInstanceIdentities()
        {
            var owner = new HeroId("hero-one");
            var other = new HeroId("hero-two");
            var first = XargathaCardContent.CreateDealtCards(owner, "red");
            var second = XargathaCardContent.CreateDealtCards(other, "blue");

            Assert.That(
                first.Select(card => card.Id.Value)
                    .Intersect(second.Select(card => card.Id.Value)),
                Is.Empty,
                "Card instance identity is per seat, so one printed set can be dealt twice.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers, in the shape the five earlier verified-content fixtures use
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

        private static string Describe(RuleModification modification) =>
            modification.Kind == RuleModificationKind.Permission
                ? "Permission " + modification.Permission
                : modification.Kind == RuleModificationKind.Restriction
                    ? "Restriction " + string.Join("+", modification.Activities)
                    : modification.Kind == RuleModificationKind.EffectiveTypeOverlay
                        ? "EffectiveTypeOverlay " + modification.EffectiveUnitKind
                        : "DerivedValue " + modification.Channel + " " + modification.DerivedQuery +
                            " " + modification.Amount;

        private static string Values(IReadOnlyList<PrintedValueDefinition> values) =>
            "[" + string.Join(", ", values.Select(value => value.Channel + " " + value.Amount)) + "]";

        private static string Amount(ActionDefinition action, PrintedValueChannel channel) =>
            action == null
                ? "-"
                : action.BaseValues.Single(value => value.Channel == channel).Amount.ToString();
    }
}
