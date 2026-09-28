using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39h. Cutter the Sky Pirate's nineteen printed faces as composed content: the values
    /// the inventory recorded, the declarations her text makes, the effect bodies it composes to, and
    /// the one branch of hers that can be driven with no match staged at all.
    ///
    /// <para><b>This is the cheap tier, and it runs before her player-path gate for the reason
    /// POST_PITCH.md records twice over:</b> the focused tier finds engine defects because it can build
    /// the board a printed clause needs, and the expensive tier proves the player path over content
    /// that already works. Milestone 8.22's forty-seven-case tier found and fixed all four of its
    /// engine defects before a pointer met the content.</para>
    ///
    /// <para><b>What this tier can and cannot reach, stated rather than implied.</b> She is not dealt
    /// until milestone 8.39i, so no configured match holds her cards and no staged board can drive one
    /// of her Skills end to end. What it CAN reach is everything a definition is: the printed values
    /// against the inventory table, the legality of every composed body against the contracts it names,
    /// the exact node ORDER each printed sentence dictates, and - for the one clause whose capability
    /// takes its whole world as an argument - the real behaviour of the composed node itself. The
    /// restore, replay and player-driven arms are milestone 8.39j's, which drives every one of these
    /// faces through real controls.</para>
    /// </summary>
    [TestFixture]
    public sealed class CutterPrintedSetTests
    {
        private const int PrintedSoloVictoryThreshold = 13;

        private static readonly TeamId RedTeam = new TeamId("cutter-red");
        private static readonly TeamId BlueTeam = new TeamId("cutter-blue");
        private static readonly HeroId Cutter = new HeroId("cutter-claimant");
        private static readonly HeroId TeamMate = new HeroId("cutter-team-mate");
        private static readonly HeroId Enemy = new HeroId("cutter-enemy");

        // -------------------------------------------------------------------------------------------
        // The set itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One row per face, in the shard's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | icon.
        /// A dash is what the face prints nothing for.
        ///
        /// <para>It is deliberately a literal table rather than something derived, for the reason
        /// <see cref="WaspVerifiedContentTests"/> and every set after it record: a test that derived
        /// its expectations from the definitions would pass whatever the definitions happened to say.
        /// This one fails if a single number, colour, Tier, icon or name drifts from
        /// <c>Docs/AI/Rules/HERO_CUTTER.md</c>, which is the document the extracted faces were
        /// transcribed into.</para>
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "cutter-walk-the-plank|Walk the Plank|Init 12|SecDef 2|SecMov 1|primary-basic-skill Skill None []|Gold Basic|item -",
            "cutter-few-more-steps|Few More Steps|Init 12|SecDef 1|SecMov 1|primary-basic-skill Skill None []|Gold Basic|item -",
            "cutter-grappling-bolt|Grappling Bolt|Init 5|SecDef 2|SecMov -|primary-basic-skill Skill None [Range 5]|Silver Basic|item -",
            "cutter-daring-strike|Daring Strike|Init 8|SecDef 6|SecMov 3|primary-attack Attack NonRanged [Attack 4]|Red I|item -",
            "cutter-bold-thrust|Bold Thrust|Init 9|SecDef 6|SecMov 3|primary-attack Attack NonRanged [Attack 5]|Red II|item Initiative",
            "cutter-evasive-shot|Evasive Shot|Init 9|SecDef 4|SecMov 3|primary-ranged-attack Attack Ranged [Attack 4, Range 2]|Red II|item Defense",
            "cutter-fearless-lunge|Fearless Lunge|Init 9|SecDef 7|SecMov 3|primary-attack Attack NonRanged [Attack 5]|Red III|item Movement",
            "cutter-tumble-shot|Tumble Shot|Init 9|SecDef 5|SecMov 3|primary-ranged-attack Attack Ranged [Attack 4, Range 2]|Red III|item Radius",
            "cutter-bombardment|Bombardment|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue I|item -",
            "cutter-barrage|Barrage|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 4]|Blue II|item Initiative",
            "cutter-x-marks-the-spot|X Marks the Spot|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Attack",
            "cutter-broadside|Broadside|Init 11|SecDef 6|SecMov 3|primary-skill Skill None [Radius 4]|Blue III|item Initiative",
            "cutter-a-fistful-of-coins|A Fistful of Coins|Init 11|SecDef 6|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Attack",
            "cutter-brace-for-impact|Brace for Impact|Init 3|SecDef 3|SecMov 2|primary-skill Skill None []|Green I|item -",
            "cutter-ramming-speed|Ramming Speed|Init 3|SecDef 3|SecMov 2|primary-skill Skill None []|Green II|item Attack",
            "cutter-outmaneuver|Outmaneuver|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Radius 3]|Green II|item Defense",
            "cutter-crashland|Crashland|Init 2|SecDef 4|SecMov 2|primary-skill Skill None []|Green III|item Range",
            "cutter-outsmart|Outsmart|Init 2|SecDef 4|SecMov 2|primary-skill Skill None [Radius 3]|Green III|item Defense",
            "cutter-legend-of-the-skies|Legend of the Skies|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                CutterCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.39 verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        /// <summary>
        /// Nineteen faces, eighteen dealt, one boxed - and the colour shape p.5 gives every hero: one
        /// Gold basic, one Silver basic, five of each of Red, Blue and Green, one Purple Ultimate and
        /// one Gold Handicap.
        /// </summary>
        [Test]
        public void HerSet_IsNineteenFacesInThePrintedColourShape()
        {
            Assert.That(CutterCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(CutterCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                CutterCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { CutterCardContent.FewMoreSteps }));

            var byColour = CutterCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2), "Walk the Plank and Few More Steps.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1));
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5));

            // The face prints p.9's sixth colour and the PROGRESSION carries None, which is the
            // engine's deliberate reading rather than a gap: CardProgressionProfile.UltimateCard
            // states progression without a colour, and CardColor.Purple was minted at milestone 8.37
            // for RULING 21's naming step rather than for an Ultimate's own progression. The
            // inventory's printed-value table still records her Ultimate's swatch as Purple, because
            // that is what the card prints; the two are different facts and both are right.
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "Legend of the Skies.");
            Assert.That(
                CutterCardContent.Definition(CutterCardContent.LegendOfTheSkies)
                    .Progression.Classification,
                Is.EqualTo(CardClassification.Ultimate));
        }

        /// <summary>
        /// p.6's five starting cards, and the thirteen the deck keeps. The two sets partition her dealt
        /// eighteen exactly, so no card is both and none is neither.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHerDealtEighteen()
        {
            Assert.That(CutterCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(CutterCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(CutterCardContent.StartingHand.Intersect(CutterCardContent.Deck), Is.Empty);
            Assert.That(
                CutterCardContent.StartingHand.Concat(CutterCardContent.Deck),
                Is.EquivalentTo(CutterCardContent.DealtDefinitions.Select(card => card.Id)));

            // p.6 deals one of each colour, and hers are the Gold, the Silver and the three Tier I.
            Assert.That(
                CutterCardContent.StartingHand
                    .Select(id => CutterCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5));
        }

        /// <summary>
        /// The one single-secondary face, and milestone 8.35z20's slot-position trap: Grappling Bolt's
        /// only icon is a Defense, drawn in the band where sixteen of her faces put a boot. The glyph
        /// is what was read, never the slot.
        /// </summary>
        [Test]
        public void GrapplingBolt_PrintsADefenseSecondaryAndNoMovementSecondary()
        {
            var bolt = CutterCardContent.Definition(CutterCardContent.GrapplingBolt);

            Assert.That(
                bolt.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense), Is.True,
                "Its single glyph is a riveted shield.");
            Assert.That(
                bolt.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement), Is.False,
                "and it prints no boot at all, which is why no Movement action exists on it.");
            Assert.That(
                CutterCardContent.AllDefinitions.Count(definition =>
                    definition.ActionDefinitions.All(action => action.Kind != ActionKind.Movement)),
                Is.EqualTo(2),
                "Grappling Bolt, whose single glyph is a shield, and the Ultimate, which prints no " +
                "ribbon at all. Every other face of hers prints a boot.");
        }

        /// <summary>
        /// She prints no Movement primary and no Defense primary: every printed primary in her set is a
        /// Skill, an Attack, or nothing at all.
        /// </summary>
        [Test]
        public void NoFaceOfHers_PrintsAMovementOrDefensePrimary()
        {
            var primaries = CutterCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .Select(action => action.Kind)
                .Distinct()
                .ToArray();

            Assert.That(primaries, Is.EquivalentTo(new[] { ActionKind.Skill, ActionKind.Attack }));
        }

        // -------------------------------------------------------------------------------------------
        // Legality against the contracts her bodies name
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition -
        /// the check that would otherwise fault mid-resolution, on a player's turn, with the card
        /// already spent. It is run at the moment each body declares, because milestone 8.10 made the
        /// moment part of the question: text applied at p.14's step 1 has to finish inside the command
        /// that reached it, and a Skill's own effect may pause freely.
        ///
        /// <para>Her Ultimate's triggered body is walked here too, and that matters: the corpus-wide
        /// case this file's own build runs walks ACTION effect roots, and p.16's permanent ability is
        /// not an action.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogItNames()
        {
            var catalog = Catalog();
            var composed = 0;

            foreach (var definition in CutterCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.EffectRoot == null)
                    {
                        continue;
                    }

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
                "Nineteen faces compose and seventeen of them attach a body: her Gold basic, her " +
                "Silver basic, all five Reds and all ten Blue and Green Skills. The Ultimate " +
                "composes as an Ultimate rule rather than as an action, and Few More Steps - p.20's " +
                "boxed Handicap - attaches none.");

            Assert.That(
                EffectDefinitionValidator.Validate(
                    CutterEffectContent.LegendOfTheSkiesTriggeredEffect(
                        CutterCardContent.ContentRevision),
                    catalog),
                Is.Empty,
                "and p.16's permanent ability is legal too, which no action-walking case would say.");
        }

        /// <summary>
        /// Every capability her composed bodies name resolves in the catalog above, listed so the set
        /// is visible rather than implied. Two of them are contracts this series minted and the build
        /// deliberately does not register yet.
        /// </summary>
        [Test]
        public void EveryCapabilitySheNames_ResolvesAndIsListed()
        {
            var catalog = Catalog();
            var named = CutterCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Concat(CutterEffectContent
                    .LegendOfTheSkiesTriggeredEffect(CutterCardContent.ContentRevision).Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability.ToString())
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(named, Is.EqualTo(new[]
            {
                "synthetic-actor-space@1",
                "attacked-target-space@1",
                "borrow-primary-action@1",
                "choose-charge-line@1",
                "choose-line-destination@1",
                "choose-target@2",
                "claim-solo-victory@1",
                "gain-resource@1",
                "set-attack-target@1",
                "synthetic-defeat-hero@1",
                "synthetic-defeat-minion@1",
                "synthetic-direct-move@3",
                "synthetic-discard-chosen-card@1",
                "synthetic-discard-chosen-card@2",
                "synthetic-is-flagged@1",
                "synthetic-note-actor@1",
                "synthetic-place-in-radius@1",
                "synthetic-push-target@3",
                "synthetic-radius-minion-target@1",
                "synthetic-range-obstacle-target@1",
                "synthetic-swap-actor@1"
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "Her composed set names twenty-one contracts and mints none: every one was built by a " +
                "lettered block of this series or was already carried by an earlier hero, which is " +
                "what those thirteen blocks were for.");

            foreach (var reference in named)
            {
                Assert.That(
                    catalog.Registered.Any(value => value.ToString() == reference), Is.True,
                    reference + " must resolve in the catalog her faces are validated against.");
            }
        }

        /// <summary>
        /// The two contracts this series minted as new handler TYPES are in the build's registration
        /// list now that she is dealt, and neither is still deferred. Milestone 8.39h wrote this case
        /// the other way round - the debt asserted from her own side, as a PAIR rather than as two
        /// memberships - specifically so that milestone 8.39i could not register one and forget the
        /// other; it did the work it was written for, and the inversion is what discharges it.
        ///
        /// <para>THE PAIR IS STILL ASSERTED AS A PAIR. What has to keep being true is that these two
        /// sit together in exactly one list, whichever list that is.</para>
        /// </summary>
        [Test]
        public void HerTwoNewContracts_AreRegisteredNowThatSheIsDealt()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                .Select(value => value.ToString())
                .ToArray();

            Assert.That(registered, Has.Member("synthetic-range-obstacle-target@1"));
            Assert.That(registered, Has.Member("claim-solo-victory@1"));

            var deferred = SyntheticRosterCapabilities.DeferredHandlers()
                .Concat(SyntheticTargetingCapabilities.DeferredHandlers())
                .Select(handler => handler.Reference.ToString())
                .ToArray();

            Assert.That(deferred, Has.No.Member("synthetic-range-obstacle-target@1"));
            Assert.That(deferred, Has.No.Member("claim-solo-victory@1"));
        }

        // -------------------------------------------------------------------------------------------
        // Composition debt
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Exactly one face carries a composition debt, and it is p.20's Handicap card - which p.6
        /// returns to the box and the supported even-player formats never deal.
        /// </summary>
        [Test]
        public void OnlyTheHandicapCard_CarriesAComposition()
        {
            var awaiting = CutterCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions
                    .Any(action => action.TextComposition.IsAwaited))
                .Select(definition => definition.Id)
                .ToArray();

            Assert.That(awaiting, Is.EquivalentTo(new[] { CutterCardContent.FewMoreSteps }));
            Assert.That(
                CutterCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(CutterCardContent.FewMoreSteps),
                "and it is not dealt, so no dealt face of hers carries one at all.");
        }

        /// <summary>
        /// Every dealt face that prints text composes a body for it. The case is written over the whole
        /// set rather than over a list, so a face added later cannot slip through with its text
        /// unimplemented and no deferral recorded.
        ///
        /// <para>No face of hers states its whole text as a targeting declaration the way three of
        /// Swift's Reds do: her two Ranged shots print a targeting sentence AND a retreat, so both
        /// declare a policy and compose a body.</para>
        /// </summary>
        [Test]
        public void EveryDealtFaceWithPrintedText_ComposesABodyForIt()
        {
            foreach (var definition in CutterCardContent.DealtDefinitions)
            {
                var primary = definition.ActionDefinitions
                    .SingleOrDefault(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                if (primary == null)
                {
                    Assert.That(definition.Id, Is.EqualTo(CutterCardContent.LegendOfTheSkies),
                        "The Ultimate is the only dealt face with no printed action.");
                    Assert.That(definition.UltimateRule, Is.Not.Null);
                    continue;
                }

                Assert.That(
                    primary.EffectRoot, Is.Not.Null,
                    definition.Id.Value + " prints text and must compose it.");
                Assert.That(primary.TextComposition.IsAwaited, Is.False, definition.Id.Value);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Gold line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Walk the Plank is a real modal: its two bullets do different things, so no union could state
        /// both. The push bullet names a HERO - which is why it selects its object rather than letting
        /// p.18's batch narrow by side and count, whose kinds are "unit" and "unit or token".
        /// </summary>
        [Test]
        public void WalkThePlank_ComposesAModalWithAHeroPushArmAndAMinionDefeatArm()
        {
            var root = Primary(CutterCardContent.WalkThePlank).EffectRoot;

            Assert.That(
                root.Nodes.Single(node => node.Id.Equals(root.RootNodeId)).Kind,
                Is.EqualTo(EffectNodeKind.Choose),
                "\"Choose one —\" is a decision, and p.10 gives it to the card's controller.");
            Assert.That(
                Capabilities(root),
                Does.Contain(SyntheticDisplacementCapabilities.PushTargetV3));
            Assert.That(
                Capabilities(root),
                Does.Contain(SyntheticConsequenceCapabilities.DefeatMinionV1));

            Assert.That(
                UnitKindOf(root, "plank-victim"), Is.EqualTo(RosterUnitKind.Hero),
                "\"an enemy HERO adjacent to you\".");
            Assert.That(
                UnitKindOf(root, "plank-minion"), Is.EqualTo(RosterUnitKind.Minion),
                "\"a minion adjacent to you\".");
        }

        /// <summary>
        /// "Up to 4 spaces" is the DISTANCE's optionality and not the step's, which is p.11's own
        /// separation - so the push declares a floor of nought and an "up to" policy while the
        /// selection above it stays mandatory, because the bullet prints no "may".
        /// </summary>
        [Test]
        public void WalkThePlank_PushesUpToFourSpacesAndNotExactlyFour()
        {
            var push = NodeOf(Primary(CutterCardContent.WalkThePlank).EffectRoot, "plank-push");

            Assert.That(push.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo));
            Assert.That(IntegerInput(push, "distance"), Is.EqualTo(4));
            Assert.That(
                IntegerInput(push, SyntheticDisplacementCapabilities.LeastDistanceParameterName),
                Is.EqualTo(0));
            Assert.That(
                NodeOf(Primary(CutterCardContent.WalkThePlank).EffectRoot, "plank-victim")
                    .FailurePolicy,
                Is.EqualTo(EffectFailurePolicy.Mandatory));
        }

        /// <summary>
        /// The zone consequence reads the push's OWN published pair - milestone 8.39c's correction to
        /// an audit row that had named <c>after-push-trigger@1</c>, which is how a p.16 permanent
        /// ability watches a push some other card performed.
        ///
        /// <para>And the ordered arm: the crossing gate reads a fact the push produces, so it stands
        /// after the push, which stands after the selection that names its object. A definition whose
        /// order the printed sentence does not dictate would be the defect here.</para>
        /// </summary>
        [Test]
        public void WalkThePlank_ReadsTheZoneCrossingOffThePushAndInThePrintedOrder()
        {
            var root = Primary(CutterCardContent.WalkThePlank).EffectRoot;
            var push = NodeOf(root, "plank-push");
            var gate = NodeOf(root, "plank-crossing");

            Assert.That(
                push.Outputs.Select(output => output.OutputName),
                Does.Contain(SyntheticDisplacementCapabilities.FirstZoneCrossingHeroOutputName));
            Assert.That(
                gate.Inputs.Single(input => input.ParameterName == "flag").SourceNodeId,
                Is.EqualTo(push.Id));
            Assert.That(
                gate.Inputs.Single(input => input.ParameterName == "flag").SourceOutputName,
                Is.EqualTo(SyntheticDisplacementCapabilities.FirstHeroCrossedAZoneOutputName));

            Assert.That(
                ChildOrder(root, "plank-push-arm"),
                Is.EqualTo(new[] { "plank-victim", "plank-push", "plank-crossing" }),
                "The sentence names its object, pushes it, and then asks where it ended.");
        }

        /// <summary>
        /// "Discards a card, or is defeated" is rule-system ledger question 13's construction: ONE
        /// mandatory step with two ways to complete, the Defeat reached only where the discard could
        /// not be. Her Blue and Green discards print "if able" instead, and this is the case that keeps
        /// the two apart.
        /// </summary>
        [Test]
        public void OnlyHerPushRider_EscalatesAnImpossibleDiscardToADefeat()
        {
            var plank = Primary(CutterCardContent.WalkThePlank).EffectRoot;
            var discard = NodeOf(plank, "plank-discard");

            Assert.That(
                discard.Capability,
                Is.EqualTo(SyntheticCardStateCapabilities.DiscardChosenCardV2),
                "Revision 2 is the one that publishes whether the hero could not discard.");
            Assert.That(discard.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory));
            Assert.That(
                Capabilities(plank), Does.Contain(SyntheticConsequenceCapabilities.DefeatHeroV1));

            foreach (var cardId in new[]
                     {
                         CutterCardContent.Bombardment, CutterCardContent.Barrage,
                         CutterCardContent.Broadside, CutterCardContent.BraceForImpact,
                         CutterCardContent.RammingSpeed, CutterCardContent.Crashland
                     })
            {
                var root = Primary(cardId).EffectRoot;
                Assert.That(
                    Capabilities(root),
                    Has.No.Member(SyntheticConsequenceCapabilities.DefeatHeroV1), cardId.Value);
                Assert.That(
                    root.Nodes
                        .Single(node => node.Capability.Equals(
                            SyntheticCardStateCapabilities.DiscardChosenCardV1))
                        .FailurePolicy,
                    Is.EqualTo(EffectFailurePolicy.IfAble),
                    cardId.Value + " prints \"if able\" and stops there.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Silver basic
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The only face in the corpus that targets an obstacle, and the only one that overrides
        /// p.18's immunity without a qualifier. Both are declarations on the selection node rather than
        /// branches inside it, which is what keeps the greyed offer, the choice and the resolution
        /// reading one answer.
        /// </summary>
        [Test]
        public void GrapplingBolt_TargetsAnObstacleAndDeclaresTheUnqualifiedImmunityOverride()
        {
            var root = Primary(CutterCardContent.GrapplingBolt).EffectRoot;
            var obstacle = NodeOf(root, "grapple-obstacle");

            Assert.That(
                obstacle.Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.RangeObstacleTargetV1));
            Assert.That(
                obstacle.ImmunityOverrides.Select(declaration => declaration.Activity),
                Is.EquivalentTo(new[] { RuleActivity.All }),
                "p.18's Immune keyword is written to be overridden in exactly these words - " +
                "\"unless the card text says otherwise\" - and this sentence carries no qualifier.");
            Assert.That(
                obstacle.ImmunityOverrides.Select(declaration => declaration.Reason),
                Is.EquivalentTo(new[] { ImmunityOverrideReason.PrintedUnqualifiedImmunity }));

            Assert.That(
                CutterCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .SelectMany(action => action.EffectRoot.Nodes)
                    .Count(node => node.ImmunityOverrides.Count > 0),
                Is.EqualTo(1),
                "and no other face of hers overrides immunity at all.");
        }

        /// <summary>
        /// "Move in a straight line towards that obstacle until you are adjacent" reads the approach
        /// the selection published rather than re-deriving the line, and it is "if able" for the one
        /// position where arriving is not a step: an obstacle already adjacent publishes the actor's
        /// own space, and a Move to where its mover already stands has no destination to offer.
        /// </summary>
        [Test]
        public void GrapplingBolt_MovesToTheApproachTheSelectionPublished()
        {
            var root = Primary(CutterCardContent.GrapplingBolt).EffectRoot;
            var move = NodeOf(root, "grapple-move");
            var destination = move.Inputs.Single(input =>
                input.ParameterName ==
                SyntheticDisplacementCapabilities.NamedDestinationParameterName);

            Assert.That(destination.SourceNodeId, Is.EqualTo(new EffectNodeId("grapple-obstacle")));
            Assert.That(
                destination.SourceOutputName,
                Is.EqualTo(SyntheticTargetingCapabilities.ObstacleApproachSpaceOutputName));
            Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble));
            Assert.That(
                ChildOrder(root, "grapple-root"),
                Is.EqualTo(new[] { "grapple-obstacle", "grapple-actor-space", "grapple-move" }),
                "The sentence targets, then moves.");
        }

        // -------------------------------------------------------------------------------------------
        // The Red line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her three melee Reds bind their target in their own text, which is
        /// <see cref="AttackTargetBinding.ByCardText"/>: the action reaches p.14's step 1 with no
        /// target and each bullet names one. Her two Ranged shots do not - they narrow p.13's own
        /// target set by milestone 8.38a's line member, which is a declaration and not an effect.
        /// </summary>
        [Test]
        public void HerRedLine_SplitsThreeOnCardTextAndTwoOnTheStraightLine()
        {
            foreach (var cardId in new[]
                     {
                         CutterCardContent.DaringStrike, CutterCardContent.BoldThrust,
                         CutterCardContent.FearlessLunge
                     })
            {
                var attack = Primary(cardId);

                Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged), cardId.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.Binding,
                    Is.EqualTo(AttackTargetBinding.ByCardText), cardId.Value);
                Assert.That(
                    attack.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack), cardId.Value);
            }

            foreach (var cardId in new[]
                     {
                         CutterCardContent.EvasiveShot, CutterCardContent.TumbleShot
                     })
            {
                var attack = Primary(cardId);

                // A Ranged Attack always carries a policy - ForClassification is p.13's own default -
                // so what a face narrows is read off NarrowsTheDefault rather than off a null.
                Assert.That(attack.AttackTargetPolicy, Is.Not.Null, cardId.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.NarrowsTheDefault(AttackRangeKind.Ranged),
                    Is.True, cardId.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.Line,
                    Is.EqualTo(AttackTargetLine.InStraightLine), cardId.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.Binding,
                    Is.EqualTo(AttackTargetBinding.AtActionStart), cardId.Value);
                Assert.That(
                    attack.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack), cardId.Value);
            }
        }

        /// <summary>
        /// The printed distances, each face's own: one space, one or two, one, two or three. They are
        /// the chooser's <c>maximumDistance</c> and nothing else, because
        /// <c>choose-line-destination@1</c> walks 1..N and never offers standing still - so "Move 1
        /// space" is exactly one without a floor being declared for it.
        /// </summary>
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EachLunge_WalksItsOwnPrintedDistance(int spaces)
        {
            var cardId = spaces == 1
                ? CutterCardContent.DaringStrike
                : spaces == 2
                    ? CutterCardContent.BoldThrust
                    : CutterCardContent.FearlessLunge;
            var root = Primary(cardId).EffectRoot;

            Assert.That(IntegerInput(NodeOf(root, "lunge-line"), "maximumDistance"), Is.EqualTo(spaces));
            Assert.That(IntegerInput(NodeOf(root, "lunge-move"), "distance"), Is.EqualTo(spaces));
            Assert.That(
                NodeOf(root, "lunge-move").FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                "The bullet prints no \"may\", and the mandatory Move is what makes milestone " +
                "8.39g2's value query equivalent to \"this bullet was taken\".");
        }

        /// <summary>
        /// "In the direction of the move" narrows the victim's own selection by the heading the Move
        /// just travelled - milestone 8.39a's <c>continuedLineFrom</c>, bound to the space the bullet
        /// started on. A radius-1 disk admits six neighbours and this admits the one that continues the
        /// line.
        ///
        /// <para>And the ordered arm, which is milestone 8.35p's rule: the Move stands BEFORE the
        /// selection, so the player names a victim from where the hero is now rather than from where
        /// she was.</para>
        /// </summary>
        [Test]
        public void EachLunge_NarrowsItsVictimToTheHeadingTheMoveTravelled()
        {
            foreach (var cardId in new[]
                     {
                         CutterCardContent.DaringStrike, CutterCardContent.BoldThrust,
                         CutterCardContent.FearlessLunge
                     })
            {
                var root = Primary(cardId).EffectRoot;
                var victim = NodeOf(root, "lunge-victim");
                var bound = victim.Inputs.Single(input =>
                    input.ParameterName == SyntheticRosterCapabilities.ContinuedLineFromParameter);

                Assert.That(bound.SourceNodeId, Is.EqualTo(new EffectNodeId("lunge-actor-space")),
                    cardId.Value + ": the line runs from where this bullet started.");
                Assert.That(
                    IntegerInput(victim, SyntheticRosterCapabilities.DistanceParameter),
                    Is.EqualTo(1), cardId.Value + ": \"adjacent to you\".");
                Assert.That(
                    UnitKindOf(root, "lunge-victim"), Is.EqualTo(RosterUnitKind.Hero),
                    cardId.Value + ": \"a HERO adjacent to you\".");
                Assert.That(
                    UnitKindOf(root, "lunge-plain-victim"), Is.EqualTo(RosterUnitKind.Unit),
                    cardId.Value + ": and the other bullet prints \"a unit\".");

                Assert.That(
                    ChildOrder(root, "lunge-move-arm"),
                    Is.EqualTo(new[]
                    {
                        "lunge-actor-space", "lunge-line", "lunge-move", "lunge-victim", "lunge-bind"
                    }),
                    cardId.Value + ": they move as soon as they select, and then they attack.");
            }
        }

        /// <summary>
        /// The printed <c>+2 Attack</c> is an action-level value declaration scaled by milestone
        /// 8.39g2's seventh query kind, and it is on the three melee faces alone - the two Ranged shots
        /// print a bare Attack 4 and declare nothing.
        /// </summary>
        [Test]
        public void OnlyHerThreeLunges_RaiseTheirAttackOnTheMovedBranch()
        {
            foreach (var cardId in new[]
                     {
                         CutterCardContent.DaringStrike, CutterCardContent.BoldThrust,
                         CutterCardContent.FearlessLunge
                     })
            {
                var contributions = Primary(cardId).RosterValueContributions;

                Assert.That(contributions.Count, Is.EqualTo(1), cardId.Value);
                Assert.That(contributions[0].Channel, Is.EqualTo(PrintedValueChannel.Attack));
                Assert.That(
                    contributions[0].Query,
                    Is.EqualTo(RosterValueQueryKind.ActorMovedThisAction),
                    cardId.Value + ": the question actor-moved-this-action@1 asks, from the value " +
                    "pipeline, which is equivalent to \"the first bullet was taken\" because that " +
                    "bullet's Move is mandatory.");
                Assert.That(contributions[0].AmountPerMatch, Is.EqualTo(2));
            }

            foreach (var cardId in new[]
                     {
                         CutterCardContent.EvasiveShot, CutterCardContent.TumbleShot,
                         CutterCardContent.WalkThePlank, CutterCardContent.Bombardment
                     })
            {
                Assert.That(
                    Primary(cardId).RosterValueContributions, Is.Empty, cardId.Value);
            }
        }

        /// <summary>
        /// "Move up to N spaces in the opposite direction" continues the line the Attack itself drew -
        /// from the space she attacked, through where she stands - so the chooser binds
        /// <c>continuedLineFrom</c> to <c>attacked-target-space@1</c> rather than asking for a heading.
        ///
        /// <para>The attacked SPACE and not the target: p.14's step 6 may have removed the defender
        /// before step 7 runs, and a piece standing nowhere has no space.</para>
        /// </summary>
        [TestCase(2)]
        [TestCase(3)]
        public void EachRangedShot_RetreatsAlongTheReverseOfItsOwnLine(int spaces)
        {
            var cardId = spaces == 2 ? CutterCardContent.EvasiveShot : CutterCardContent.TumbleShot;
            var root = Primary(cardId).EffectRoot;
            var line = NodeOf(root, "retreat-line");
            var bound = line.Inputs.Single(input =>
                input.ParameterName == SyntheticRosterCapabilities.ContinuedLineFromParameter);

            Assert.That(
                NodeOf(root, "retreat-target-space").Capability,
                Is.EqualTo(SyntheticRosterCapabilities.FormerTargetSpaceV1));
            Assert.That(bound.SourceNodeId, Is.EqualTo(new EffectNodeId("retreat-target-space")));
            Assert.That(IntegerInput(line, "maximumDistance"), Is.EqualTo(spaces));
            Assert.That(line.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo));
            Assert.That(
                NodeOf(root, "retreat-move").FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                "\"up to\" is p.10's non-mandatory wording, so a blocked file is a step not taken.");
        }

        // -------------------------------------------------------------------------------------------
        // The Blue line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Bombardment, Barrage and Broadside print ONE sentence and it carries TWO narrowings measured
        /// from two different places: the adjacency from each candidate, for a unit of the candidate's
        /// own side, and the ring around Cutter herself that "not adjacent to you" removes.
        ///
        /// <para><b>The second is milestone 8.39h's own correction.</b> 8.39d recorded it as already
        /// registered against <c>target-is-not-adjacent@1</c>, and so did her shard's mapping row -
        /// but that capability asks about an enclosing ATTACK's bound target and these three are
        /// Skills. This case is what would have caught a composition that quietly dropped half a
        /// sentence.</para>
        /// </summary>
        [Test]
        public void HerThreeBlueDiscards_NarrowBothHalvesOfOneSentence()
        {
            foreach (var pair in new[]
                     {
                         (Card: CutterCardContent.Bombardment, Radius: 3),
                         (Card: CutterCardContent.Barrage, Radius: 4),
                         (Card: CutterCardContent.Broadside, Radius: 4)
                     })
            {
                var subject = NodeOf(Primary(pair.Card).EffectRoot, "bombard-subject");

                // Milestone 9.2: the disk is the card's printed Radius (pair.Radius) with p.16's
                // items, read by the selection itself rather than written as a literal.
                Assert.That(
                    BooleanInput(subject, SyntheticRosterCapabilities.MeasuresPrintedRadiusParameter),
                    Is.True, pair.Card.Value + ": Radius " + pair.Radius + " is the icon, not a number.");
                Assert.That(
                    subject.Inputs.Any(input =>
                        input.ParameterName == SyntheticRosterCapabilities.DistanceParameter),
                    Is.False, pair.Card.Value + ": no written reach beside it.");
                Assert.That(
                    Primary(pair.Card).BaseValues.Any(value =>
                        value.Channel == PrintedValueChannel.Radius && value.Amount == pair.Radius),
                    Is.True, pair.Card.Value + " prints Radius " + pair.Radius + ".");
                Assert.That(
                    BooleanInput(
                        subject, SyntheticRosterCapabilities.AdjacentToAnotherEnemyUnitParameter),
                    Is.True, pair.Card.Value + ": \"adjacent to another enemy unit\".");
                Assert.That(
                    BooleanInput(subject, SyntheticRosterCapabilities.ExcludeAdjacentParameter),
                    Is.True, pair.Card.Value + ": \"and not adjacent to you\".");
                Assert.That(
                    (RosterUnitKind)IntegerInput(
                        subject, SyntheticRosterCapabilities.UnitKindParameter),
                    Is.EqualTo(RosterUnitKind.Hero), pair.Card.Value);
            }
        }

        /// <summary>
        /// Bombardment and Barrage print byte-identical text and compose as two definitions all the
        /// same - Whisper's Shadow Step and Shadow Walk precedent. The Radius is an icon rather than a
        /// word, and a shared definition would let a p.16 Radius item raise the wrong face.
        /// </summary>
        [Test]
        public void BombardmentAndBarrage_PrintOneSentenceAndComposeTwoDefinitions()
        {
            var bombardment = CutterCardContent.Definition(CutterCardContent.Bombardment);
            var barrage = CutterCardContent.Definition(CutterCardContent.Barrage);

            Assert.That(
                barrage.SourceContent.PrintedText,
                Is.EqualTo(bombardment.SourceContent.PrintedText),
                "The two texts are the same string, which is CU-08.");
            Assert.That(
                Primary(CutterCardContent.Barrage).EffectRoot.Id,
                Is.Not.EqualTo(Primary(CutterCardContent.Bombardment).EffectRoot.Id));
            Assert.That(
                Primary(CutterCardContent.Bombardment).BaseValues
                    .Single(value => value.Channel == PrintedValueChannel.Radius).Amount,
                Is.EqualTo(3));
            Assert.That(
                Primary(CutterCardContent.Barrage).BaseValues
                    .Single(value => value.Channel == PrintedValueChannel.Radius).Amount,
                Is.EqualTo(4));
        }

        /// <summary>
        /// Only Broadside repeats, and its repeat excludes the subject the first performance took -
        /// milestone 8.39d1's fifth binding, which names a SELECTION node because a Skill has no attack
        /// target to exclude from.
        /// </summary>
        [Test]
        public void OnlyBroadside_RepeatsOnADifferentSelectionSubject()
        {
            var root = Primary(CutterCardContent.Broadside).EffectRoot;
            var repeat = NodeOf(root, "bombard-repeat");

            Assert.That(repeat.Kind, Is.EqualTo(EffectNodeKind.RepeatAction));
            Assert.That(repeat.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May));
            Assert.That(repeat.MaximumRepeats, Is.EqualTo(1), "\"May repeat ONCE\".");
            Assert.That(
                repeat.RepeatTargetBinding,
                Is.EqualTo(RepeatTargetBinding.DifferentSelectionSubject));
            Assert.That(
                repeat.BoundSelectionNodeId, Is.EqualTo(new EffectNodeId("bombard-subject")),
                "and it names the node whose subject it excludes.");

            foreach (var cardId in new[] { CutterCardContent.Bombardment, CutterCardContent.Barrage })
            {
                Assert.That(
                    Primary(cardId).EffectRoot.Nodes
                        .Any(node => node.Kind == EffectNodeKind.RepeatAction),
                    Is.False, cardId.Value + " prints no repeat.");
            }
        }

        /// <summary>
        /// Her two "chooses one" faces put the choice in the ENEMY's hands, which is Whisper's seam
        /// rather than a new one: the selection names an enemy hero and the modal binds that hero as
        /// its chooser. A composition that left the chooser as the actor would be the card saying
        /// something it does not say.
        ///
        /// <para>Both bullets are still the ACTOR's to carry out - "YOU place that hero", "YOU gain N
        /// coins" - and the Place is measured from the acting hero, which is what its anchor states.</para>
        /// </summary>
        [Test]
        public void HerTwoEnemyChosenModals_HandTheChoiceToTheChosenEnemyHero()
        {
            foreach (var pair in new[]
                     {
                         (Card: CutterCardContent.XMarksTheSpot, Coins: 2),
                         (Card: CutterCardContent.AFistfulOfCoins, Coins: 3)
                     })
            {
                var root = Primary(pair.Card).EffectRoot;
                var modal = NodeOf(root, "chosen-modal");

                Assert.That(modal.Kind, Is.EqualTo(EffectNodeKind.Choose), pair.Card.Value);
                Assert.That(
                    modal.Chooser, Is.EqualTo(EffectChooserRole.BoundHero), pair.Card.Value);
                Assert.That(
                    modal.Inputs
                        .Single(input => input.ParameterName ==
                            EffectNodeDefinition.ChooserParameterName)
                        .SourceNodeId,
                    Is.EqualTo(new EffectNodeId("chosen-victim")), pair.Card.Value);
                Assert.That(modal.MinimumSelections, Is.EqualTo(1), pair.Card.Value);
                Assert.That(modal.MaximumSelections, Is.EqualTo(1), pair.Card.Value);

                Assert.That(
                    IntegerInput(NodeOf(root, "chosen-gain"), "amount"),
                    Is.EqualTo(pair.Coins), pair.Card.Value);
                Assert.That(
                    (PlaceDestinationAnchor)IntegerInput(
                        NodeOf(root, "chosen-place"),
                        SyntheticPlacementCapabilities.DestinationAnchorParameterName),
                    Is.EqualTo(PlaceDestinationAnchor.ActingHero),
                    pair.Card.Value + ": \"in radius\" is measured from the hero whose card it is.");
            }
        }

        /// <summary>
        /// Only A Fistful of Coins claims the ending, and the number it claims at is the CARD's rather
        /// than the engine's - a declared parameter, which is the discipline every printed distance in
        /// this catalog follows.
        /// </summary>
        [Test]
        public void OnlyAFistfulOfCoins_ClaimsTheSoloVictoryAndAtItsOwnPrintedThreshold()
        {
            var claim = NodeOf(Primary(CutterCardContent.AFistfulOfCoins).EffectRoot, "chosen-claim");

            Assert.That(
                claim.Capability, Is.EqualTo(SyntheticRosterCapabilities.ClaimSoloVictoryV1));
            Assert.That(
                IntegerInput(claim, SyntheticRosterCapabilities.SoloVictoryThresholdParameter),
                Is.EqualTo(PrintedSoloVictoryThreshold));
            Assert.That(
                ChildOrder(Primary(CutterCardContent.AFistfulOfCoins).EffectRoot, "chosen-coin-arm"),
                Is.EqualTo(new[] { "chosen-actor", "chosen-gain", "chosen-claim" }),
                "The coins are gained and THEN counted, which is what \"You gain 3 coins. If you " +
                "have 13 or more\" says: a hero on ten wins with this card.");

            Assert.That(
                CutterCardContent.AllDefinitions
                    .SelectMany(definition => definition.ActionDefinitions)
                    .Where(action => action.EffectRoot != null)
                    .SelectMany(action => action.EffectRoot.Nodes)
                    .Count(node => node.Capability.Equals(
                        SyntheticRosterCapabilities.ClaimSoloVictoryV1)),
                Is.EqualTo(1),
                "and X Marks the Spot prints the same modal without the sentence.");
        }

        // -------------------------------------------------------------------------------------------
        // A Fistful of Coins, driven from the composed node itself
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The victory branch exercised from COMPOSED CONTENT rather than from milestone 8.39g's own
        /// fixtures: the node is read off her card, its declared inputs and outputs are the ones the
        /// handler is handed, and the claim is taken at the printed threshold and not below it.
        ///
        /// <para>Twelve is the negative arm and thirteen the boundary; fourteen is the other side of
        /// it, because "13 or more" is what the card prints. In every one of them the clause APPLIES -
        /// a hero holding twelve has resolved the whole face correctly, which is what a condition on a
        /// clause means.</para>
        /// </summary>
        [TestCase(11, false)]
        [TestCase(12, false)]
        [TestCase(PrintedSoloVictoryThreshold, true)]
        [TestCase(14, true)]
        public void AFistfulOfCoins_ClaimsFromItsOwnComposedNode(int held, bool expectedClaim)
        {
            var recorder = new RecordingClaim();
            var result = ExecuteComposedClaim(recorder, held);

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                result.Outcomes
                    .Single(fact => fact.OutputName ==
                        SyntheticRosterCapabilities.SoloVictoryClaimedOutput)
                    .Value.BooleanValue,
                Is.EqualTo(expectedClaim));
            Assert.That(recorder.Claimed, Is.EqualTo(expectedClaim ? Cutter : default(HeroId)));
        }

        /// <summary>
        /// Merely OFFERING the card must not end the match. A walk that judged whether the branch can
        /// be taken runs <c>Anticipate</c>, and milestone 8.39g1 asserted that it claims nothing; this
        /// is that rule asked of the composed node, because the composition is what a real offer would
        /// be walking.
        /// </summary>
        [Test]
        public void AnticipatingAFistfulOfCoins_ClaimsNothing()
        {
            var recorder = new RecordingClaim();
            var handler = ClaimHandler();

            Assert.That(
                handler.Anticipate(ComposedClaimContext(recorder, PrintedSoloVictoryThreshold + 5)),
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(recorder.Claimed.IsDefined, Is.False);
            Assert.That(recorder.Attempts, Is.EqualTo(0));
        }

        /// <summary>
        /// The count is read at the branch and not at its high-water mark, driven from her own node: a
        /// hero who reached the threshold and spent back down below it - which p.15's Level Up does -
        /// does not win.
        /// </summary>
        [Test]
        public void AFistfulOfCoins_ReadsTheLedgerAtTheBranchRatherThanItsHighWaterMark()
        {
            var recorder = new RecordingClaim();
            var ledger = new HeroCoinLedger(Participants());

            Assert.That(
                ledger.TryGrant(Cutter, PrintedSoloVictoryThreshold, CoinGrantKind.CardEffect, out _),
                Is.True);
            Assert.That(ledger.TrySpend(Cutter, 4, CoinSpendKind.LevelUp, out _), Is.True);

            var result = ClaimHandler().Execute(ComposedClaimContext(recorder, ledger));

            Assert.That(
                result.Outcomes
                    .Single(fact => fact.OutputName ==
                        SyntheticRosterCapabilities.SoloVictoryClaimedOutput)
                    .Value.BooleanValue,
                Is.False);
            Assert.That(recorder.Claimed.IsDefined, Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // The Green line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her three charges are Brogan's charge with milestone 8.39e's one optional parameter bound:
        /// the printed distances are the declaration's own minimum and maximum, the noun is p.19's
        /// hero, and "ignoring obstacles" is declared on the charge AND on the Move that travels it.
        /// </summary>
        [Test]
        public void HerThreeCharges_DeclareTheirPrintedDistancesAndCrossObstacles()
        {
            foreach (var face in new[]
                     {
                         (Card: CutterCardContent.BraceForImpact, Least: 3, Most: 3),
                         (Card: CutterCardContent.RammingSpeed, Least: 3, Most: 4),
                         (Card: CutterCardContent.Crashland, Least: 3, Most: 5)
                     })
            {
                var root = Primary(face.Card).EffectRoot;
                var line = NodeOf(root, "charge-destination");

                Assert.That(
                    line.Capability, Is.EqualTo(SyntheticRosterCapabilities.ChooseChargeLineV1),
                    face.Card.Value);
                Assert.That(
                    IntegerInput(line, "minimumDistance"), Is.EqualTo(face.Least), face.Card.Value);
                Assert.That(
                    IntegerInput(line, "maximumDistance"), Is.EqualTo(face.Most), face.Card.Value);
                Assert.That(
                    (RosterUnitKind)IntegerInput(
                        line, SyntheticRosterCapabilities.UnitKindParameter),
                    Is.EqualTo(RosterUnitKind.Hero),
                    face.Card.Value + ": \"adjacent to an enemy HERO\".");
                Assert.That(
                    BooleanInput(line, SyntheticRosterCapabilities.IgnoresObstaclesParameter),
                    Is.True, face.Card.Value);
                Assert.That(
                    BooleanInput(
                        NodeOf(root, "charge-move"),
                        SyntheticDisplacementCapabilities.MovesThroughObstaclesParameterName),
                    Is.True, face.Card.Value + ": the charge offers it and the Move travels it.");
            }
        }

        /// <summary>
        /// "That hero" is named AFTER the Move by a separate radius-1 selection, which is milestone
        /// 8.39e's settled reading and a correction to her shard's own row. The charge offers a set of
        /// DESTINATIONS and collapses the qualifying heroes away, so there is no hero on it for a
        /// consequence to read - and on a destination beside two, the selection that follows offers
        /// both and the player chooses.
        /// </summary>
        [Test]
        public void HerThreeCharges_NameTheirSubjectAfterTheMoveAndNotInsideTheChargeNode()
        {
            foreach (var cardId in new[]
                     {
                         CutterCardContent.BraceForImpact, CutterCardContent.RammingSpeed,
                         CutterCardContent.Crashland
                     })
            {
                var root = Primary(cardId).EffectRoot;

                Assert.That(
                    ChildOrder(root, "charge-root"),
                    Is.EqualTo(new[]
                    {
                        "charge-actor-space", "charge-destination", "charge-move", "charge-victim",
                        "charge-discard"
                    }),
                    cardId.Value + ": milestone 8.35p's order - they move as soon as they select.");
                Assert.That(
                    NodeOf(root, "charge-destination").Outputs.Select(output => output.OutputName),
                    Is.EqualTo(new[] { "destination" }),
                    cardId.Value + ": the charge publishes a space and NOTHING else.");
                Assert.That(
                    NodeOf(root, "charge-discard").Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticCardStateCapabilities.HeroParameter)
                        .SourceNodeId,
                    Is.EqualTo(new EffectNodeId("charge-victim")),
                    cardId.Value + ": the discard reads the selection, never the charge.");
            }
        }

        /// <summary>
        /// Her two Swap faces exchange with an enemy minion in the printed Radius and then may move the
        /// minion the exchange displaced.
        ///
        /// <para><b>Her shard's row for these two named the wrong pair, and reading the schema is what
        /// found it.</b> It cited <c>choose-swap-subject@1</c> - Whisper's UNION selection, whose whole
        /// schema is that union's three parameters and which has no reach of its own to set. This is
        /// Sabina's Back to Back with the side reversed, and that pair was already registered.</para>
        /// </summary>
        [TestCase(2)]
        [TestCase(3)]
        public void EachSwapFace_ExchangesWithAnEnemyMinionAndMayMoveIt(int spaces)
        {
            var cardId = spaces == 2 ? CutterCardContent.Outmaneuver : CutterCardContent.Outsmart;
            var root = Primary(cardId).EffectRoot;
            var subject = NodeOf(root, "swap-subject");
            var swap = NodeOf(root, "swap-exchange");
            var move = NodeOf(root, "swap-move");

            Assert.That(
                subject.Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.RadiusMinionTargetV1),
                "\"in radius\" is the printed icon, so p.16's items raise it.");
            Assert.That(
                (SourceTeamRelation)IntegerInput(
                    subject, SyntheticTargetingCapabilities.TargetRelationParameterName),
                Is.EqualTo(SourceTeamRelation.OpposingSource));
            Assert.That(
                swap.Capability, Is.EqualTo(SyntheticExchangeCapabilities.SwapWithActorV1),
                "The actor is one of the two objects by the sentence's own construction.");
            Assert.That(
                move.Inputs.Single(input => input.ParameterName == "mover").SourceOutputName,
                Is.EqualTo("subjectAt"),
                "The minion the move takes is where the Swap just put it.");
            Assert.That(IntegerInput(move, "distance"), Is.EqualTo(spaces));
            Assert.That(
                move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                "\"you MAY move that minion UP TO N spaces\" carries both of p.11's optionalities.");
        }

        // -------------------------------------------------------------------------------------------
        // The Ultimate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The Ultimate declares milestone 8.39f1's sixth condition and milestone 8.39f's fourth borrow
        /// pool, and neither is the nearest-looking member of its own enum.
        ///
        /// <para>Tigerclaw's <c>AfterControllerPerformsBasicAction</c> - which her shard's own mapping
        /// row named until 8.39f measured it - cannot reach this clause at all: every one of the five
        /// conditions before 8.39f1's is matched against a fact that is undefined unless the source
        /// card is p.9-Basic, and her Tier IV is unlocked when most of what she plays is not. And
        /// Swift's <c>OwnedRightmostResolvedCard</c> names a position in an ORDER where hers names a
        /// SLOT, which part company on a slot holding no card of hers.</para>
        /// </summary>
        [Test]
        public void LegendOfTheSkies_FiresAfterAPrimaryActionAndBorrowsThePreviousSlot()
        {
            var ultimate = CutterCardContent
                .Definition(CutterCardContent.LegendOfTheSkies).UltimateRule;

            Assert.That(ultimate, Is.Not.Null);
            Assert.That(
                ultimate.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerPerformsPrimaryAction));
            Assert.That(
                ultimate.TriggerCheckpoints,
                Is.EquivalentTo(new[] { TimingCheckpoint.ActionEnded }));
            Assert.That(
                ultimate.GrantedTargetExclusion, Is.EqualTo(GrantedTargetExclusion.None),
                "Her clause prints no restriction on what the granted action may target.");

            var borrow = CutterEffectContent
                .LegendOfTheSkiesTriggeredEffect(CutterCardContent.ContentRevision).Nodes
                .Single(node => node.Capability.Equals(
                    SyntheticBorrowedActionCapabilities.BorrowPrimaryActionV1));

            Assert.That(
                (BorrowedActionPool)IntegerInput(
                    borrow, SyntheticBorrowedActionCapabilities.PoolParameter),
                Is.EqualTo(BorrowedActionPool.PreviousTurnSlotCard));
            Assert.That(
                (CardColor)IntegerInput(
                    borrow, SyntheticBorrowedActionCapabilities.ColorParameter),
                Is.EqualTo(CardColor.None),
                "The face prints no colour.");
            Assert.That(
                borrow.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                "\"you MAY perform the primary action\".");
        }

        /// <summary>
        /// She declares exactly one triggered effect and no Active effect at all - an explicit empty
        /// contribution rather than a missing one. No face of hers prints "This turn:", "Next turn:" or
        /// "End of turn:", which is the whole reason there is no <c>CutterActiveEffectContent</c>.
        /// </summary>
        [Test]
        public void SheDeclaresOneTriggeredEffectAndNoActiveEffect()
        {
            Assert.That(
                CutterEffectContent.TriggeredEffects(CutterCardContent.ContentRevision).Count,
                Is.EqualTo(1));
            Assert.That(
                CutterCardContent.AllDefinitions.Count(definition => definition.UltimateRule != null),
                Is.EqualTo(1));
            Assert.That(
                CutterCardContent.AllDefinitions
                    .Select(definition => definition.SourceContent.PrintedText)
                    .Any(text =>
                        text.Contains("This turn:") || text.Contains("Next turn:") ||
                        text.Contains("End of turn:")),
                Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // p.15's progression
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Her three upgrade lines match the printed Tier structure the inventory recorded: each Tier I
        /// card names its colour's Tier II pair, and each Tier II card names the same Tier III pair.
        /// The Tier III cards name none, because p.15 stops there.
        /// </summary>
        [Test]
        public void HerThreeUpgradeLines_MatchThePrintedTierStructure()
        {
            AssertSuccessors(
                CutterCardContent.DaringStrike,
                CutterCardContent.BoldThrust, CutterCardContent.EvasiveShot);
            AssertSuccessors(
                CutterCardContent.BoldThrust,
                CutterCardContent.FearlessLunge, CutterCardContent.TumbleShot);
            AssertSuccessors(
                CutterCardContent.EvasiveShot,
                CutterCardContent.FearlessLunge, CutterCardContent.TumbleShot);

            AssertSuccessors(
                CutterCardContent.Bombardment,
                CutterCardContent.Barrage, CutterCardContent.XMarksTheSpot);
            AssertSuccessors(
                CutterCardContent.Barrage,
                CutterCardContent.Broadside, CutterCardContent.AFistfulOfCoins);
            AssertSuccessors(
                CutterCardContent.XMarksTheSpot,
                CutterCardContent.Broadside, CutterCardContent.AFistfulOfCoins);

            AssertSuccessors(
                CutterCardContent.BraceForImpact,
                CutterCardContent.RammingSpeed, CutterCardContent.Outmaneuver);
            AssertSuccessors(
                CutterCardContent.RammingSpeed,
                CutterCardContent.Crashland, CutterCardContent.Outsmart);
            AssertSuccessors(
                CutterCardContent.Outmaneuver,
                CutterCardContent.Crashland, CutterCardContent.Outsmart);

            foreach (var cardId in new[]
                     {
                         CutterCardContent.FearlessLunge, CutterCardContent.TumbleShot,
                         CutterCardContent.Broadside, CutterCardContent.AFistfulOfCoins,
                         CutterCardContent.Crashland, CutterCardContent.Outsmart
                     })
            {
                Assert.That(
                    CutterCardContent.Definition(cardId).Progression.NextTierCandidateIds, Is.Empty,
                    cardId.Value + " is Tier III and p.15 offers nothing beyond it.");
            }
        }

        /// <summary>
        /// Her four progressed arms compose, and each holds p.15's five: the two basics plus the three
        /// upgraded cards of that Tier. The case walks all four rather than sampling one, because the
        /// branches divide her kit along the lines her new vocabulary falls on and neither stands for
        /// the other.
        /// </summary>
        [Test]
        public void HerFourProgressedArms_EachHoldTheTwoBasicsAndThreeUpgrades()
        {
            var owner = new HeroId("cutter-progressed-owner");

            foreach (var branch in new[]
                     {
                         CutterUpgradeBranch.FirstSibling, CutterUpgradeBranch.SecondSibling
                     })
            {
                foreach (var tier in new[] { 2, 3 })
                {
                    var hand = tier == 2
                        ? CutterProgressedHands.CreateTierTwoHand(owner, "cutter-progressed", branch)
                        : CutterProgressedHands.CreateTierThreeHand(owner, "cutter-progressed", branch);
                    var label = $"{branch} tier {tier}";

                    Assert.That(hand.Instances.Count, Is.EqualTo(5), label);
                    Assert.That(hand.DeckInstances.Count, Is.EqualTo(13), label);

                    var held = hand.Instances
                        .Select(card => card.DefinitionReference.DefinitionId)
                        .ToArray();

                    Assert.That(held, Has.Member(CutterCardContent.WalkThePlank), label);
                    Assert.That(held, Has.Member(CutterCardContent.GrapplingBolt), label);
                    Assert.That(
                        held, Has.No.Member(CutterCardContent.FewMoreSteps),
                        label + ": p.6 has already boxed the Handicap card.");
                    Assert.That(
                        held, Has.No.Member(CutterCardContent.LegendOfTheSkies),
                        label + ": p.6 deals the Ultimate to the deck, and p.16's unlock is what " +
                        "moves it.");

                    var upgrades = held
                        .Where(id => !id.Equals(CutterCardContent.WalkThePlank) &&
                            !id.Equals(CutterCardContent.GrapplingBolt))
                        .ToArray();
                    Assert.That(upgrades.Length, Is.EqualTo(3), label);
                    Assert.That(
                        upgrades.Select(id => CutterCardContent.Definition(id).Progression.Tier)
                            .Distinct()
                            .Single(),
                        Is.EqualTo(tier == 2 ? CardTier.II : CardTier.III),
                        label + ": all three upgrades are of that Tier.");
                    Assert.That(
                        upgrades.Select(id => CutterCardContent.Definition(id).Progression.Color)
                            .Distinct()
                            .Count(),
                        Is.EqualTo(3),
                        label + ": one of each colour, which is what p.15's per-colour pair means.");
                }
            }
        }

        /// <summary>
        /// The two branches are disjoint at both Tiers, and each Tier III card is a successor the Tier
        /// II card of the same branch names - which is what makes the branches her printed lineages
        /// rather than an arbitrary split.
        /// </summary>
        [Test]
        public void HerTwoBranches_ShareNoUpgradedCardAndFollowThePrintedLineages()
        {
            var owner = new HeroId("cutter-branch-owner");

            foreach (var tier in new[] { 2, 3 })
            {
                var first = Upgrades(owner, tier, CutterUpgradeBranch.FirstSibling);
                var second = Upgrades(owner, tier, CutterUpgradeBranch.SecondSibling);

                Assert.That(first.Intersect(second), Is.Empty, "tier " + tier);
                Assert.That(first.Concat(second).Distinct().Count(), Is.EqualTo(6), "tier " + tier);
            }

            foreach (var branch in new[]
                     {
                         CutterUpgradeBranch.FirstSibling, CutterUpgradeBranch.SecondSibling
                     })
            {
                var second = Upgrades(owner, 2, branch);
                var third = Upgrades(owner, 3, branch);

                foreach (var tierTwo in second)
                {
                    Assert.That(
                        CutterCardContent.Definition(tierTwo).Progression.NextTierCandidateIds
                            .Intersect(third),
                        Is.Not.Empty,
                        branch + ": " + tierTwo.Value + " names a Tier III card this branch takes.");
                }
            }
        }

        /// <summary>
        /// One seat's setup composes: eighteen instances, five in hand and thirteen in the deck, each
        /// owned by the seat that dealt them.
        /// </summary>
        [Test]
        public void HerHand_ComposesAtPageSixSetup()
        {
            var owner = new HeroId("cutter-printed-set-owner");
            var hand = CutterCardContent.CreateHand(owner, "cutter-printed-set");

            Assert.That(hand.Definitions.Count, Is.EqualTo(18));
            Assert.That(hand.Instances.Count, Is.EqualTo(5));
            Assert.That(hand.DeckInstances.Count, Is.EqualTo(13));
            Assert.That(hand.AllInstances.All(card => card.OwnerId.Equals(owner)), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The catalog her composed faces are legal against: everything the build registers, plus the
        /// two contracts this series minted and deliberately left unregistered. It is composed by
        /// concatenating the declared lists rather than by naming handlers here, so a contract that
        /// moves from one list to the other needs no edit in this file.
        /// </summary>
        private static EffectCapabilityCatalog Catalog()
        {
            var build = MatchContentCatalogs.ForThisBuild().Capabilities;
            return new EffectCapabilityCatalog(
                build.Registered.Select(build.Load)
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                    .Concat(SyntheticTargetingCapabilities.DeferredHandlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));
        }

        private static string Describe(CardDefinition definition)
        {
            var primary = definition.ActionDefinitions
                .FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var movement = definition.ActionDefinitions
                .FirstOrDefault(action => action.Kind == ActionKind.Movement);
            var defense = definition.ActionDefinitions
                .FirstOrDefault(action => action.Kind == ActionKind.Defense);
            var progression = definition.Progression;

            var primaryText = primary == null
                ? "- Ultimate []"
                : $"{primary.Id.Value} {primary.Kind} {primary.AttackRange} [" +
                    string.Join(", ", primary.BaseValues
                        .OrderBy(value => value.Channel)
                        .Select(value => $"{value.Channel} {value.Amount}")) + "]";

            var tierText = progression.IsUltimate
                ? "Ultimate IV"
                : progression.Tier == CardTier.None
                    ? $"{progression.Color} Basic"
                    : $"{progression.Color} {progression.Tier}";

            return string.Join("|", new[]
            {
                definition.Id.Value,
                definition.DisplayName,
                "Init " + (progression.IsUltimate ? "-" : definition.Initiative.Value.ToString()),
                "SecDef " + (defense == null
                    ? "-"
                    : defense.BaseValues
                        .Single(value => value.Channel == PrintedValueChannel.Defense)
                        .Amount.ToString()),
                "SecMov " + (movement == null
                    ? "-"
                    : movement.BaseValues
                        .Single(value => value.Channel == PrintedValueChannel.Movement)
                        .Amount.ToString()),
                primaryText,
                tierText,
                "item " + (progression.ItemIcon == PrintedValueChannel.None
                    ? "-"
                    : progression.ItemIcon.ToString())
            });
        }

        private static void AssertSuccessors(
            CardDefinitionId cardId, params CardDefinitionId[] expected) =>
            Assert.That(
                CutterCardContent.Definition(cardId).Progression.NextTierCandidateIds,
                Is.EquivalentTo(expected), cardId.Value);

        private static IReadOnlyList<CardDefinitionId> Upgrades(
            HeroId owner, int tier, CutterUpgradeBranch branch)
        {
            var hand = tier == 2
                ? CutterProgressedHands.CreateTierTwoHand(owner, "cutter-branch", branch)
                : CutterProgressedHands.CreateTierThreeHand(owner, "cutter-branch", branch);
            return hand.Instances
                .Select(card => card.DefinitionReference.DefinitionId)
                .Where(id => !id.Equals(CutterCardContent.WalkThePlank) &&
                    !id.Equals(CutterCardContent.GrapplingBolt))
                .ToArray();
        }

        private static ActionDefinition Primary(CardDefinitionId cardId) =>
            CutterCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        private static IReadOnlyList<EffectCapabilityReference> Capabilities(EffectDefinition root) =>
            root == null
                ? Array.Empty<EffectCapabilityReference>()
                : root.Nodes
                    .Select(node => node.Capability)
                    .Where(capability => capability.IsDefined)
                    .ToArray();

        private static EffectNodeDefinition NodeOf(EffectDefinition root, string nodeId) =>
            root.Nodes.Single(node => node.Id.Equals(new EffectNodeId(nodeId)));

        private static string[] ChildOrder(EffectDefinition root, string nodeId) =>
            NodeOf(root, nodeId).ChildNodeIds.Select(child => child.Value).ToArray();

        private static int IntegerInput(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal.IntegerValue;

        private static bool BooleanInput(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal.BooleanValue;

        private static RosterUnitKind UnitKindOf(EffectDefinition root, string nodeId) =>
            (RosterUnitKind)IntegerInput(
                NodeOf(root, nodeId), SyntheticRosterCapabilities.UnitKindParameter);

        // -------------------------------------------------------------------------------------------
        // Driving A Fistful of Coins' own composed claim node
        // -------------------------------------------------------------------------------------------

        private static IEffectCapabilityHandler ClaimHandler() =>
            SyntheticRosterCapabilities.Handlers()
                .Single(handler =>
                    handler.Reference.Equals(SyntheticRosterCapabilities.ClaimSoloVictoryV1));

        private static EffectExecutionResult ExecuteComposedClaim(ISoloVictoryClaim claim, int held)
        {
            var ledger = new HeroCoinLedger(Participants());
            if (held > 0)
            {
                Assert.That(
                    ledger.TryGrant(Cutter, held, CoinGrantKind.CardEffect, out _), Is.True);
            }

            return ClaimHandler().Execute(ComposedClaimContext(claim, ledger));
        }

        private static EffectCapabilityContext ComposedClaimContext(
            ISoloVictoryClaim claim, int held)
        {
            var ledger = new HeroCoinLedger(Participants());
            if (held > 0)
            {
                Assert.That(
                    ledger.TryGrant(Cutter, held, CoinGrantKind.CardEffect, out _), Is.True);
            }

            return ComposedClaimContext(claim, ledger);
        }

        /// <summary>
        /// The context the composed node itself describes: its own node id, its own failure policy,
        /// its own literal inputs and its own declared outputs, all read off A Fistful of Coins rather
        /// than written here. That is what makes this the composition under test rather than a second
        /// copy of milestone 8.39g1's fixture.
        /// </summary>
        private static EffectCapabilityContext ComposedClaimContext(
            ISoloVictoryClaim claim, HeroCoinLedger ledger)
        {
            var card = CutterCardContent.Definition(CutterCardContent.AFistfulOfCoins);
            var action = Primary(CutterCardContent.AFistfulOfCoins);
            var node = NodeOf(action.EffectRoot, "chosen-claim");

            return new EffectCapabilityContext(
                node.Id,
                node.FailurePolicy,
                Cutter,
                Cutter,
                new CardInstanceId("cutter-fistful-instance"),
                card.Reference,
                action.Id,
                node.Inputs.ToDictionary(
                    input => input.ParameterName, input => input.Literal),
                1,
                1,
                Participants(),
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                rosterState: EffectRosterState.Of(
                    Array.Empty<RoundTurnSlot>(),
                    new RoundHeroDefeatHistory(Participants()),
                    ledger,
                    claim),
                declaredOutputs: node.Outputs);
        }

        private static MatchParticipants Participants()
        {
            var players = new List<Player>();
            var heroes = new List<Hero>();
            foreach (var pair in new[]
            {
                (Hero: Cutter, Team: RedTeam),
                (Hero: TeamMate, Team: RedTeam),
                (Hero: Enemy, Team: BlueTeam)
            })
            {
                var player = new PlayerId(pair.Hero.Value + "-player");
                players.Add(new Player(player, pair.Team));
                heroes.Add(new Hero(pair.Hero, player, HeroDashboard.RulebookLayout));
            }

            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                players,
                heroes);
        }

        private sealed class RecordingClaim : ISoloVictoryClaim
        {
            public HeroId Claimed { get; private set; }

            public int Attempts { get; private set; }

            public bool HasBeenClaimed => Claimed.IsDefined;

            public bool TryClaim(HeroId winningHeroId)
            {
                Attempts++;
                if (HasBeenClaimed || !winningHeroId.IsDefined)
                {
                    return false;
                }

                Claimed = winningHeroId;
                return true;
            }
        }
    }
}
