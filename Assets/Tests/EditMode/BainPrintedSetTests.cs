using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40i. Bain the Bounty Hunter's nineteen printed faces as composed content: the values
    /// the inventory recorded, the declarations his text makes, the effect bodies it composes to, and
    /// the branches of his that can be driven with no match staged at all.
    ///
    /// <para><b>This is the cheap tier, and it runs before his player-path gate for the reason
    /// POST_PITCH.md records twice over:</b> the focused tier finds engine defects because it can build
    /// the board a printed clause needs, and the expensive tier proves the player path over content
    /// that already works.</para>
    ///
    /// <para><b>What this tier can and cannot reach, stated rather than implied.</b> He is not dealt
    /// until milestone 8.40j, so no configured match holds his cards and no staged board can drive one
    /// of his faces through a real control. What it CAN reach is everything a definition is - the
    /// printed values against the inventory table, the legality of every composed body against the
    /// contracts it names, and the exact node ORDER each printed sentence dictates - and, for every
    /// clause whose capability takes its whole world as an argument, the real behaviour of the composed
    /// nodes themselves against a board built here. The restore, replay and player-driven arms are
    /// milestone 8.40k's.</para>
    ///
    /// <para><b>Where the behavioural arms get their board.</b> They run HIS OWN composed effect roots
    /// through the production resolution session, against a catalog that is the build's registration
    /// list plus the six declarations milestone 7.9's gate still withholds. That is the same catalog
    /// <c>DealtCorpusCompositionValidationTests</c> validates against, and it is what lets a
    /// composition milestone drive content a dealing has not yet registered.</para>
    /// </summary>
    [TestFixture]
    public sealed class BainPrintedSetTests
    {
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
        /// <c>Docs/AI/Rules/HERO_BAIN.md</c>, which is the document the extracted faces were
        /// transcribed into.</para>
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "bain-dead-or-alive|Dead or Alive|Init 11|SecDef 2|SecMov 1|primary-basic-attack Attack NonRanged [Attack 4, Radius 4]|Gold Basic|item -",
            "bain-side-quest|Side Quest|Init 10|SecDef 1|SecMov 1|primary-basic-skill Skill None [Radius 4]|Gold Basic|item -",
            "bain-get-over-here|Get over here!|Init 13|SecDef 2|SecMov -|primary-basic-skill Skill None [Range 4]|Silver Basic|item -",
            "bain-light-crossbow|Light Crossbow|Init 8|SecDef 5|SecMov 4|primary-ranged-attack Attack Ranged [Attack 5, Range 2]|Red I|item -",
            "bain-heavy-crossbow|Heavy Crossbow|Init 8|SecDef 5|SecMov 4|primary-ranged-attack Attack Ranged [Attack 5, Range 3]|Red II|item Defense",
            "bain-hand-crossbow|Hand Crossbow|Init 8|SecDef 6|SecMov 4|primary-ranged-attack Attack Ranged [Attack 4, Range 3]|Red II|item Initiative",
            "bain-arbalest|Arbalest|Init 9|SecDef 6|SecMov 4|primary-ranged-attack Attack Ranged [Attack 6, Range 4]|Red III|item Defense",
            "bain-hunter-seeker|Hunter-Seeker|Init 9|SecDef 7|SecMov 4|primary-ranged-attack Attack Ranged [Attack 5, Range 3]|Red III|item Movement",
            "bain-a-game-of-chance|A Game of Chance|Init 9|SecDef 4|SecMov 3|primary-skill Skill None [Radius 2]|Blue I|item -",
            "bain-dead-mans-hand|Dead Man's Hand|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Defense",
            "bain-drinking-buddies|Drinking Buddies|Init 10|SecDef 4|SecMov 3|primary-skill Skill None [Radius 3]|Blue II|item Attack",
            "bain-were-not-done-yet|We're Not Done Yet!|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Range",
            "bain-another-one|Another One!|Init 10|SecDef 5|SecMov 3|primary-skill Skill None [Radius 3]|Blue III|item Attack",
            "bain-close-call|Close Call|Init 4|SecDef -|SecMov 2|primary-block Defense None []|Green I|item -",
            "bain-narrow-escape|Narrow Escape|Init 4|SecDef -|SecMov 2|primary-block Defense None []|Green II|item Initiative",
            "bain-vantage-point|Vantage Point|Init 4|SecDef 3|SecMov -|primary-movement Movement None [Movement 2]|Green II|item Attack",
            "bain-perfect-getaway|Perfect Getaway|Init 3|SecDef -|SecMov 2|primary-block Defense None []|Green III|item Initiative",
            "bain-high-ground|High Ground|Init 3|SecDef 4|SecMov -|primary-movement Movement None [Movement 2]|Green III|item Radius",
            "bain-a-complicated-profession|A Complicated Profession|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                BainCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory milestone " +
                "8.40 verified against the extracted faces. Correct the definition, not this table - " +
                "the table is the source.");
        }

        /// <summary>
        /// Nineteen faces, eighteen dealt, one boxed - and the colour shape p.5 gives every hero: one
        /// Gold basic, one Silver basic, five of each of Red, Blue and Green, one Purple Ultimate and
        /// one Gold Handicap.
        /// </summary>
        [Test]
        public void HisSet_IsNineteenFacesInThePrintedColourShape()
        {
            Assert.That(BainCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(BainCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(
                BainCardContent.ReturnedToBoxAtSetup,
                Is.EquivalentTo(new[] { BainCardContent.SideQuest }));

            var byColour = BainCardContent.AllDefinitions
                .GroupBy(definition => definition.Progression.Color)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.That(byColour[CardColor.Gold], Is.EqualTo(2), "Dead or Alive and Side Quest.");
            Assert.That(byColour[CardColor.Silver], Is.EqualTo(1));
            Assert.That(byColour[CardColor.Red], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Blue], Is.EqualTo(5));
            Assert.That(byColour[CardColor.Green], Is.EqualTo(5));
            Assert.That(byColour[CardColor.None], Is.EqualTo(1), "A Complicated Profession.");
            Assert.That(
                BainCardContent.Definition(BainCardContent.AComplicatedProfession)
                    .Progression.Classification,
                Is.EqualTo(CardClassification.Ultimate));
        }

        /// <summary>
        /// p.6's five starting cards, and the thirteen the deck keeps. The two sets partition his dealt
        /// eighteen exactly, so no card is both and none is neither.
        /// </summary>
        [Test]
        public void TheStartingHandAndDeck_PartitionHisDealtEighteen()
        {
            Assert.That(BainCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(BainCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(BainCardContent.StartingHand.Intersect(BainCardContent.Deck), Is.Empty);
            Assert.That(
                BainCardContent.StartingHand.Concat(BainCardContent.Deck),
                Is.EquivalentTo(BainCardContent.DealtDefinitions.Select(card => card.Id)));

            Assert.That(
                BainCardContent.StartingHand
                    .Select(id => BainCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.6 deals one of each colour, and his are the Gold, the Silver and the three Tier I.");
        }

        /// <summary>
        /// BA-05 at the largest scale the roster has produced: SIX faces print exactly one secondary
        /// icon, all six draw it in the SAME band, and three of them are shields while three are boots.
        /// The glyph is what was read; the slot carries no information at all.
        /// </summary>
        [Test]
        public void HisSixSingleSecondaryFaces_PrintThreeShieldsAndThreeBoots()
        {
            var shields = new[]
            {
                BainCardContent.GetOverHere, BainCardContent.VantagePoint, BainCardContent.HighGround
            };
            var boots = new[]
            {
                BainCardContent.CloseCall, BainCardContent.NarrowEscape, BainCardContent.PerfectGetaway
            };

            foreach (var cardId in shields)
            {
                Assert.That(SecondaryDefense(cardId), Is.Not.Null, cardId.Value + " prints a shield.");
                Assert.That(
                    SecondaryMovement(cardId), Is.Null,
                    cardId.Value + " prints no boot, so no secondary Movement action exists on it.");
            }

            foreach (var cardId in boots)
            {
                Assert.That(SecondaryMovement(cardId), Is.Not.Null, cardId.Value + " prints a boot.");
                Assert.That(
                    SecondaryDefense(cardId), Is.Null,
                    cardId.Value + " prints no shield, so no secondary Defense action exists on it.");
            }

            Assert.That(
                BainCardContent.AllDefinitions.Count(definition =>
                    !definition.Progression.IsUltimate &&
                    (SecondaryDefense(definition.Id) == null) != (SecondaryMovement(definition.Id) == null)),
                Is.EqualTo(6),
                "Six single-secondary faces, and no seventh - the Ultimate prints no ribbon at all and " +
                "every other face prints both icons.");
        }

        /// <summary>
        /// His Green line is shaped like nobody else's: three Blocks, two Movement primaries, and no
        /// Skill at all - and none of the five prints a reach glyph, because none of their clauses
        /// measures a distance (BA-08).
        /// </summary>
        [Test]
        public void HisGreenLine_IsThreeBlocksAndTwoMovementsWithNoReachGlyph()
        {
            var greens = BainCardContent.AllDefinitions
                .Where(definition => definition.Progression.Color == CardColor.Green)
                .ToArray();

            Assert.That(greens.Length, Is.EqualTo(5));
            Assert.That(
                greens.Count(definition => Primary(definition.Id).Kind == ActionKind.Defense),
                Is.EqualTo(3));
            Assert.That(
                greens.Count(definition => Primary(definition.Id).Kind == ActionKind.Movement),
                Is.EqualTo(2));
            Assert.That(
                greens.Any(definition => Primary(definition.Id).Kind == ActionKind.Skill), Is.False,
                "His Green colour prints no Skill at all, which no other hero's does.");

            foreach (var definition in greens)
            {
                Assert.That(
                    definition.ActionDefinitions.SelectMany(action => action.BaseValues)
                        .Select(value => value.Channel),
                    Has.No.Member(PrintedValueChannel.Range), definition.Id.Value);
                Assert.That(
                    definition.ActionDefinitions.SelectMany(action => action.BaseValues)
                        .Select(value => value.Channel),
                    Has.No.Member(PrintedValueChannel.Radius), definition.Id.Value);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Legality against the contracts his bodies name
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition -
        /// the check that would otherwise fault mid-resolution, on a player's turn, with the card
        /// already spent. It is run at the moment each body declares, because milestone 8.10 made the
        /// moment part of the question.
        ///
        /// <para>His two triggered bodies are walked here too, and that matters: the corpus-wide case
        /// beside this file walks ACTION effect roots, and neither p.16's permanent ability nor an
        /// End-of-turn effect is an action.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogItNames()
        {
            var catalog = Catalog();
            var composed = 0;

            foreach (var definition in BainCardContent.AllDefinitions)
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

            Assert.That(composed, Is.EqualTo(13),
                "Nineteen faces compose and thirteen of them attach a body: his Gold basic, his Silver " +
                "basic, the two modal Reds, all five Blues and the four Greens that print text. The " +
                "three plain crossbows state their whole text as a targeting declaration, Perfect " +
                "Getaway's whole text is its Block condition, the Ultimate composes as an Ultimate " +
                "rule rather than as an action, and Side Quest - p.20's boxed Handicap - attaches none.");

            foreach (var triggered in BainEffectContent.TriggeredEffects(BainCardContent.ContentRevision))
            {
                Assert.That(
                    EffectDefinitionValidator.Validate(triggered, catalog), Is.Empty,
                    triggered.Id.Value + " is legal too, which no action-walking case would say.");
            }
        }

        /// <summary>
        /// Every capability his composed bodies name resolves in the catalog above, listed so the set is
        /// visible rather than implied. Six of them are contracts this series minted or widened and the
        /// build deliberately does not register yet.
        /// </summary>
        [Test]
        public void EveryCapabilityHeNames_ResolvesAndIsListed()
        {
            var catalog = Catalog();
            var named = BainCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Concat(BainEffectContent
                    .TriggeredEffects(BainCardContent.ContentRevision)
                    .SelectMany(definition => definition.Nodes))
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability.ToString())
                .Distinct()
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(named, Is.EqualTo(new[]
            {
                "attack-sequence@1",
                "bain-bounty-marker-held@1",
                "bain-give-bounty-marker@1",
                "bain-return-bounty-marker@1",
                "bain-transfer-bounty-marker@1",
                "bain-vantage-point-ignore-obstacles@1",
                "bain-vantage-point-movement@1",
                "bain-high-ground-ignore-obstacles@1",
                "bain-high-ground-movement@1",
                "bain-another-one-end-of-turn-repeat@1",
                "gain-resource@1",
                "name-color@1",
                "retrieve-card@1",
                "synthetic-direct-move@3",
                "synthetic-discard-chosen-card@1",
                "synthetic-hidden-card-choice@1",
                "synthetic-is-flagged@1",
                "synthetic-radius-hero-target@1",
                "synthetic-range-obstacle-target@1",
                "synthetic-reveal-chosen-card@1"
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "His composed set names twenty-one contracts and mints none: every one was built by a " +
                "lettered block of this series, is hero content of his own, or was already carried by " +
                "an earlier hero.");

            foreach (var reference in named)
            {
                Assert.That(
                    catalog.Registered.Any(value => value.ToString() == reference), Is.True,
                    reference + " must resolve in the catalog his faces are validated against.");
            }
        }

        /// <summary>
        /// The contracts this series minted as new handler TYPES are all in the build's registration
        /// list and none is left on a deferred one, asserted as a SET rather than one membership at a
        /// time - so milestone 8.40j could not register one and forget another, and no later edit can
        /// move one back. Milestone 8.40i wrote this case as the deferral and milestone 8.40j inverted
        /// it in place, which is the shape milestone 8.39h and Cutter's dealing set at two; this is
        /// that shape at six.
        /// </summary>
        [Test]
        public void HisSixNewContracts_AreRegisteredTogetherNowThatHeIsDealt()
        {
            var his = BainComponentContent.Handlers()
                .Concat(SyntheticHiddenChoiceCapabilities.Handlers())
                .Select(handler => handler.Reference.ToString())
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(his, Is.EqualTo(new[]
            {
                "bain-bounty-marker-held@1",
                "bain-give-bounty-marker@1",
                "bain-return-bounty-marker@1",
                "bain-transfer-bounty-marker@1",
                "synthetic-hidden-card-choice@1",
                "synthetic-reveal-chosen-card@1"
            }.OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "The six his faces start. Two of them run on TYPES milestone 7.9's gate had already " +
                "seen showcased - the give and the return - and four are new types that took a " +
                "showcase consumer at his dealing: the transfer, the holder predicate, the hidden " +
                "choice and the reveal.");

            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                .Select(value => value.ToString())
                .ToArray();
            foreach (var reference in his)
            {
                Assert.That(registered, Has.Member(reference),
                    reference + " is his and he is dealt, so the build must register it.");
            }

            Assert.That(
                BainComponentContent.DeferredHandlers(), Is.Empty,
                "and nothing of his is left on a deferred list, on both sides of the seam.");
            Assert.That(SyntheticHiddenChoiceCapabilities.DeferredHandlers(), Is.Empty);
            Assert.That(BainActiveEffectContent.DeferredHandlers(), Is.Empty);
            Assert.That(
                BainActiveEffectContent.RegisteredHandlers().Count(), Is.EqualTo(5),
                "His five Active-effect creations moved across with them - two obstacle permissions, " +
                "two Movement raises and the End-of-turn repeat - on a handler TYPE this build has " +
                "registered since milestone 6.8, so they owed the gate no showcase consumer.");
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
            var awaiting = BainCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions
                    .Any(action => action.TextComposition.IsAwaited))
                .Select(definition => definition.Id)
                .ToArray();

            Assert.That(awaiting, Is.EquivalentTo(new[] { BainCardContent.SideQuest }));
            Assert.That(
                BainCardContent.DealtDefinitions.Select(definition => definition.Id),
                Has.No.Member(BainCardContent.SideQuest),
                "and it is not dealt, so no dealt face of his carries one at all.");
        }

        /// <summary>
        /// Every dealt face that prints text composes a body for it, or states that text as a
        /// declaration. The case is written over the whole set rather than over a list, so a face added
        /// later cannot slip through with its text unimplemented and no deferral recorded.
        /// </summary>
        [Test]
        public void EveryDealtFaceWithPrintedText_ComposesABodyOrDeclaresIt()
        {
            foreach (var definition in BainCardContent.DealtDefinitions)
            {
                var primary = definition.ActionDefinitions
                    .SingleOrDefault(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                if (primary == null)
                {
                    Assert.That(definition.Id, Is.EqualTo(BainCardContent.AComplicatedProfession),
                        "The Ultimate is the only dealt face with no printed action.");
                    Assert.That(definition.UltimateRule, Is.Not.Null);
                    continue;
                }

                Assert.That(primary.TextComposition.IsAwaited, Is.False, definition.Id.Value);
                if (primary.EffectRoot != null)
                {
                    continue;
                }

                // The four faces whose whole printed text IS a declaration: three clear-line
                // crossbows, whose sentence is their targeting policy, and Perfect Getaway, whose
                // sentence is its Block condition and nothing else.
                Assert.That(
                    primary.AttackTargetPolicy.HasValue &&
                    primary.AttackTargetPolicy.Value.Line ==
                        AttackTargetLine.InStraightLineClearOfUnitsAndTerrain ||
                    primary.BlockCondition == PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker,
                    Is.True,
                    definition.Id.Value + " composes no body, so its whole text must be a declaration.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Gold line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Dead or Alive's clause in the printed order: the "may" is the clause's own, the recipient is
        /// a printed-Radius enemy hero selection, and the GIVE IS THE LAST STEP - which milestone
        /// 8.40h's trigger makes a bound rather than a preference, because a trigger standing at the
        /// card-text altitude stops the page there.
        /// </summary>
        [Test]
        public void DeadOrAlive_MaySelectAnEnemyHeroInRadiusAndGivesLast()
        {
            var action = Primary(BainCardContent.DeadOrAlive);
            var root = action.EffectRoot;

            Assert.That(action.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack),
                "\"After the attack:\" in as many words.");
            Assert.That(NodeOf(root, "bounty-may").Kind, Is.EqualTo(EffectNodeKind.Optional));
            Assert.That(
                NodeOf(root, "bounty-may").FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                "\"You MAY give\" - p.10 gives an unassigned decision to the card's controller, and " +
                "the Optional is what lets him decline without first naming a hero.");
            Assert.That(
                ChildOrder(root, "bounty-sequence"),
                Is.EqualTo(new[] { "bounty-select", "bounty-give" }),
                "The sentence names its recipient and then gives, and the give is the LAST step of " +
                "the clause.");
            Assert.That(
                NodeOf(root, "bounty-select").Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.RadiusHeroTargetV1),
                "\"an enemy hero in radius\" is the printed icon's own disk, which BA-07 settled as a " +
                "Radius rather than a Range.");
            Assert.That(
                (SourceTeamRelation)IntegerInput(
                    NodeOf(root, "bounty-select"),
                    SyntheticTargetingCapabilities.TargetRelationParameterName),
                Is.EqualTo(SourceTeamRelation.OpposingSource));
            Assert.That(
                BooleanInput(
                    NodeOf(root, "bounty-give"),
                    SyntheticComponentCapabilities.MarkerGivingHandler
                        .MovesWhenSupplyIsEmptyParameterName),
                Is.True,
                "BA-D1: his supply is ONE, so from the second give onwards the give moves the marker " +
                "rather than reporting itself impossible.");
        }

        /// <summary>
        /// The second sentence of both marker faces is composed NOWHERE, and that is the marker's own
        /// design rather than a gap: "a hero with a bounty marker spends 1 additional life counter when
        /// defeated" is a property of the attachment, so it is true while it is held, of whoever holds
        /// it, without a node of any card saying so.
        /// </summary>
        [Test]
        public void TheAdditionalDefeatCost_IsAPropertyOfTheMarkerAndOfNoCardNode()
        {
            Assert.That(
                BainComponentContent.BountyDefinition.Properties
                    .Select(property => property.Modification.Kind),
                Has.Member(RuleModificationKind.AdditionalDefeatLifeCostWhenDefeated),
                "BA-13: the twelfth family, about the DEFEATED holder rather than about the attacker.");
            Assert.That(BainComponentContent.BountySupplyCount, Is.EqualTo(1), "BA-D1.");

            var named = BainCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Select(node => node.Capability)
                .Where(capability => capability.IsDefined)
                .ToArray();
            foreach (var creation in SyntheticModificationCapabilities.Handlers())
            {
                Assert.That(
                    named.Any(capability => capability.Equals(creation.Reference)), Is.False,
                    "No face of his declares a rule modification as an effect of its own, because " +
                    "the marker carries the one rule his two giving faces print.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Red line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// BA-03, asserted on the faces themselves: THREE of his five Reds print the clear line, and the
        /// two modal ones print no line clause at all - which is the count 8.40d corrected from the
        /// five this row originally claimed.
        /// </summary>
        [Test]
        public void HisRedLine_SplitsThreeOnTheClearLineAndTwoOnTheMarkerModal()
        {
            foreach (var cardId in new[]
                     {
                         BainCardContent.LightCrossbow, BainCardContent.HeavyCrossbow,
                         BainCardContent.Arbalest
                     })
            {
                var attack = Primary(cardId);
                Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.Ranged), cardId.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.Line,
                    Is.EqualTo(AttackTargetLine.InStraightLineClearOfUnitsAndTerrain), cardId.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.Binding,
                    Is.EqualTo(AttackTargetBinding.AtActionStart), cardId.Value,
                    "The sentence narrows p.13's own targeting rather than naming a target in text.");
                Assert.That(attack.EffectRoot, Is.Null, cardId.Value + " composes no body at all.");
            }

            foreach (var cardId in new[]
                     {
                         BainCardContent.HandCrossbow, BainCardContent.HunterSeeker
                     })
            {
                var attack = Primary(cardId);
                Assert.That(
                    attack.AttackTargetPolicy.Value.Line, Is.EqualTo(AttackTargetLine.AnyLine),
                    cardId.Value + " prints a marker modal and no line clause.");
                Assert.That(
                    attack.AttackTargetPolicy.Value.Binding,
                    Is.EqualTo(AttackTargetBinding.ByCardText), cardId.Value);
                Assert.That(
                    attack.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack), cardId.Value);
            }

            Assert.That(
                Primary(BainCardContent.DeadOrAlive).AttackTargetPolicy.Value
                    .NarrowsTheDefault(AttackRangeKind.NonRanged),
                Is.False,
                "and his Gold basic narrows nothing: \"Target a unit adjacent to you\" is p.13's own " +
                "sentence for a non-Ranged Attack word for word. A Ranged or non-Ranged Attack always " +
                "carries a policy - ForClassification is p.13's own default and replaces a null - so " +
                "\"this face narrows nothing\" is read off NarrowsTheDefault and never off a null.");
        }

        /// <summary>
        /// The two modal Reds differ in exactly what their printed sentences differ in - the modal's
        /// cardinality and the "(on different targets)" clause - and both read the printed Range through
        /// the BULLET rather than through a written distance, which is what BA-15's <c>+1 Range</c> item
        /// exists to raise.
        /// </summary>
        [Test]
        public void TheTwoMarkerModals_DifferOnlyInTheirCardinalityAndTheirDistinctTargetsClause()
        {
            foreach (var cardId in new[]
                     {
                         BainCardContent.HandCrossbow, BainCardContent.HunterSeeker
                     })
            {
                var root = Primary(cardId).EffectRoot;
                var marker = NodeOf(root, "crossbow-marker-bullet");
                var adjacent = NodeOf(root, "crossbow-adjacent-bullet");

                Assert.That(
                    marker.Capability, Is.EqualTo(SyntheticSequenceCapabilities.AttackSequenceV1),
                    cardId.Value + " composes its bullets as attack-sequence leaves, which is the only " +
                    "shape carrying BOTH the printed Range and the marker narrowing.");
                Assert.That(
                    (AttackTargetReach)IntegerInput(
                        marker, SyntheticSequenceCapabilities.ReachParameter),
                    Is.EqualTo(AttackTargetReach.PrintedRange), cardId.Value);
                Assert.That(
                    (AttackSequenceSubjectNarrowing)IntegerInput(
                        marker, SyntheticSequenceCapabilities.SubjectNarrowingParameter),
                    Is.EqualTo(AttackSequenceSubjectNarrowing.HoldsTheActorsMarker), cardId.Value);
                Assert.That(
                    (AttackTargetableUnits)IntegerInput(
                        marker, SyntheticSequenceCapabilities.TargetableUnitsParameter),
                    Is.EqualTo(AttackTargetableUnits.HeroesOnly),
                    cardId.Value + " bullet one prints \"a hero\".");

                Assert.That(
                    (AttackTargetReach)IntegerInput(
                        adjacent, SyntheticSequenceCapabilities.ReachParameter),
                    Is.EqualTo(AttackTargetReach.AdjacentOnly), cardId.Value);
                Assert.That(
                    (AttackTargetableUnits)IntegerInput(
                        adjacent, SyntheticSequenceCapabilities.TargetableUnitsParameter),
                    Is.EqualTo(AttackTargetableUnits.HeroesAndMinions),
                    cardId.Value + " bullet two prints \"a unit\", which is p.19's pair.");
                Assert.That(
                    adjacent.Inputs.Select(input => input.ParameterName),
                    Has.No.Member(SyntheticSequenceCapabilities.SubjectNarrowingParameter),
                    cardId.Value + " bullet two qualifies nobody, and says so by declaring nothing.");
            }

            var hand = Primary(BainCardContent.HandCrossbow).EffectRoot;
            var seeker = Primary(BainCardContent.HunterSeeker).EffectRoot;

            Assert.That(NodeOf(hand, "crossbow-modal").MaximumSelections, Is.EqualTo(1),
                "\"Choose one —\" is 1..1.");
            Assert.That(NodeOf(seeker, "crossbow-modal").MaximumSelections, Is.EqualTo(2),
                "\"Choose one, or both\" is 1..2, which is p.10's Modal Actions read literally.");
            Assert.That(NodeOf(hand, "crossbow-modal").MinimumSelections, Is.EqualTo(1));
            Assert.That(NodeOf(seeker, "crossbow-modal").MinimumSelections, Is.EqualTo(1));

            foreach (var bullet in new[] { "crossbow-marker-bullet", "crossbow-adjacent-bullet" })
            {
                Assert.That(
                    BooleanInput(
                        NodeOf(seeker, bullet),
                        SyntheticSequenceCapabilities.DistinctTargetsParameter),
                    Is.True,
                    "Hunter-Seeker prints \"(on different targets)\" across its bullets, so BOTH " +
                    "declare it - a one-bullet answer then says the same thing whichever it took.");
                Assert.That(
                    BooleanInput(
                        NodeOf(hand, bullet),
                        SyntheticSequenceCapabilities.DistinctTargetsParameter),
                    Is.False,
                    "and Hand Crossbow prints no such parenthetical.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Silver basic
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Get over here! narrows what may be NAMED and never what BLOCKS, which is BA-03's whole point,
        /// and it moves the TARGET - so it reads the actor-side approach space milestone 8.40e published
        /// rather than Cutter's obstacle-side one.
        /// </summary>
        [Test]
        public void GetOverHere_TargetsAUnitOrTokenAndPullsItToTheActorsOwnApproach()
        {
            var root = Primary(BainCardContent.GetOverHere).EffectRoot;
            var target = NodeOf(root, "pull-target");
            var move = NodeOf(root, "pull-move");

            Assert.That(
                target.Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.RangeObstacleTargetV1));
            Assert.That(
                BooleanInput(target, SyntheticTargetingCapabilities.ExcludeTerrainParameterName),
                Is.True,
                "\"a unit or a token\" - and the same sentence still prints \"no obstacles\", which is " +
                "p.18's full list, so a file whose first obstacle is terrain offers nothing.");
            Assert.That(
                target.ImmunityOverrides, Is.Empty,
                "The face overrides no immunity, unlike Cutter's Grappling Bolt, so p.18's keyword " +
                "stands.");

            Assert.That(
                move.Inputs.Single(input => input.ParameterName == "mover").SourceOutputName,
                Is.EqualTo(SyntheticTargetingCapabilities.ChosenObstacleSpaceOutputName),
                "The MOVER is the target, which is the mirror of Grappling Bolt.");
            Assert.That(
                move.Inputs.Single(input => input.ParameterName ==
                    SyntheticDisplacementCapabilities.NamedDestinationParameterName).SourceOutputName,
                Is.EqualTo(SyntheticTargetingCapabilities.ActorApproachSpaceOutputName));
            Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                "A target already adjacent is published its own space, and a Move to where its mover " +
                "already stands is a step not taken rather than a Skill that stops.");
            Assert.That(
                ChildOrder(root, "pull-root"), Is.EqualTo(new[] { "pull-target", "pull-move" }),
                "The sentence targets, then moves.");
        }

        // -------------------------------------------------------------------------------------------
        // The Blue line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The guessing clause in the printed order, on all three faces: select, hide, guess, reveal,
        /// branch. A composition that revealed before it guessed would make the guess free, which is
        /// what the printed semicolons order.
        /// </summary>
        [Test]
        public void HisThreeGuessingFaces_ComposeTheClauseInThePrintedOrder()
        {
            foreach (var cardId in GuessingFaces)
            {
                var root = Primary(cardId).EffectRoot;

                Assert.That(
                    ChildOrder(root, "guess-root"),
                    Is.EqualTo(new[]
                    {
                        "guess-select", "guess-hide", "guess-name-colour", "guess-reveal", "guess-branch"
                    }),
                    cardId.Value);
                Assert.That(
                    IntegerInput(
                        NodeOf(root, "guess-select"),
                        SyntheticTargetingCapabilities.MinimumCardsInHandParameterName),
                    Is.EqualTo(2),
                    cardId.Value + " prints \"with two or more cards in hand\", which narrows the " +
                    "SELECTION rather than gating the clause.");
                Assert.That(
                    NodeOf(root, "guess-name-colour").Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticBorrowedActionCapabilities.RestrictToColorsOwnedByParameter)
                        .SourceNodeId,
                    Is.EqualTo(new EffectNodeId("guess-select")),
                    cardId.Value + " binds BA-D2's domain to the chosen hero's own public deck.");
                Assert.That(
                    NodeOf(root, "guess-discard").Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticCardStateCapabilities.RestrictToCardParameter)
                        .SourceNodeId,
                    Is.EqualTo(new EffectNodeId("guess-hide")),
                    cardId.Value + " discards THAT card rather than opening a second prompt.");
            }
        }

        /// <summary>
        /// The three faces differ in exactly three printed places - the Radius, the reward, and whether
        /// the failure branch is a modal - and BA-11 records that the "the"/"that" wording difference
        /// carries no rule.
        /// </summary>
        [Test]
        public void TheThreeGuessingFaces_DifferInTheRewardAndTheFailureBranchAlone()
        {
            Assert.That(CoinsOf(BainCardContent.AGameOfChance), Is.EqualTo(1));
            Assert.That(CoinsOf(BainCardContent.DeadMansHand), Is.EqualTo(2));
            Assert.That(CoinsOf(BainCardContent.WereNotDoneYet), Is.EqualTo(2));

            Assert.That(
                RadiusOf(BainCardContent.AGameOfChance), Is.EqualTo(2),
                "and the disk is the printed icon rather than a number a body carries, so p.16's item " +
                "raises it.");
            Assert.That(RadiusOf(BainCardContent.DeadMansHand), Is.EqualTo(3));
            Assert.That(RadiusOf(BainCardContent.WereNotDoneYet), Is.EqualTo(3));

            foreach (var cardId in new[] { BainCardContent.AGameOfChance, BainCardContent.DeadMansHand })
            {
                var root = Primary(cardId).EffectRoot;
                Assert.That(
                    ChildOrder(root, "guess-branch"),
                    Is.EqualTo(new[] { "guess-discard", "guess-coin-bullet" }),
                    cardId.Value + " branches straight to its coins on a wrong guess.");
                Assert.That(
                    root.Nodes.Any(node => node.Kind == EffectNodeKind.RepeatAction), Is.False,
                    cardId.Value + " prints no repeat.");
            }

            var tierThree = Primary(BainCardContent.WereNotDoneYet).EffectRoot;
            Assert.That(
                ChildOrder(tierThree, "guess-branch"),
                Is.EqualTo(new[] { "guess-discard", "guess-modal" }),
                "We're Not Done Yet! reaches its modal only on the failure branch.");
            Assert.That(
                ChildOrder(tierThree, "guess-modal"),
                Is.EqualTo(new[] { "guess-repeat", "guess-coin-bullet" }));
            Assert.That(NodeOf(tierThree, "guess-repeat").MaximumRepeats, Is.EqualTo(1),
                "\"may repeat ONCE\" is the node's own declared bound.");
        }

        /// <summary>
        /// BA-12: Drinking Buddies and Another One! print byte-identical first sentences at the SAME
        /// Radius, so - unlike Cutter's Bombardment and Barrage - the reach does not separate them. The
        /// composition must not collapse them, and what separates them is the keyword sentence alone.
        /// </summary>
        [Test]
        public void TheTwoRetrievalFaces_AreNotCollapsedAndDifferOnlyInTheKeywordSentence()
        {
            var buddies = Primary(BainCardContent.DrinkingBuddies).EffectRoot;
            var another = Primary(BainCardContent.AnotherOne).EffectRoot;

            Assert.That(RadiusOf(BainCardContent.DrinkingBuddies), Is.EqualTo(3));
            Assert.That(RadiusOf(BainCardContent.AnotherOne), Is.EqualTo(3));
            Assert.That(buddies.Id, Is.Not.EqualTo(another.Id),
                "Two definitions, because they are two cards - a shared definition would let p.16's " +
                "item raise the wrong face.");

            Assert.That(
                ChildOrder(buddies, "buddies-root"),
                Is.EqualTo(new[] { "buddies-select", "buddies-retrieve", "buddies-also" }));
            Assert.That(
                ChildOrder(another, "buddies-root"),
                Is.EqualTo(new[]
                {
                    "buddies-select", "buddies-retrieve", "buddies-also", "buddies-arm-repeat"
                }),
                "Another One! arms its End-of-turn effect as the last step, exactly as Tali's Blizzard " +
                "arms hers.");
            Assert.That(
                NodeOf(another, "buddies-arm-repeat").Capability,
                Is.EqualTo(BainActiveEffectContent.AnotherOneEndOfTurnRepeatV1));
            Assert.That(
                BainActiveEffectContent.AnotherOneRepeatDefinition.DurationKind,
                Is.EqualTo(ActiveEffectDurationKind.EndOfTurn));
            Assert.That(
                BainActiveEffectContent.AnotherOneRepeatDefinition.TriggerCheckpoints,
                Is.EquivalentTo(new[] { TimingCheckpoint.TurnEnded }));
        }

        /// <summary>
        /// BA-Q1's reading, declared: the first hero is ANY hero on the board except Bain himself. The
        /// selection names no side and excludes the actor, and the board arm below proves the same
        /// reading against the live candidate set.
        /// </summary>
        [Test]
        public void TheRetrievalSelection_NamesAnySideAndExcludesTheActor()
        {
            foreach (var cardId in new[]
                     {
                         BainCardContent.DrinkingBuddies, BainCardContent.AnotherOne
                     })
            {
                var select = NodeOf(Primary(cardId).EffectRoot, "buddies-select");

                Assert.That(
                    (SourceTeamRelation)IntegerInput(
                        select, SyntheticTargetingCapabilities.TargetRelationParameterName),
                    Is.EqualTo(SourceTeamRelation.AnyTeam),
                    cardId.Value + " prints \"a hero\" with no side-word, and the absence is the " +
                    "answer.");
                Assert.That(
                    BooleanInput(select, SyntheticTargetingCapabilities.IncludeSelfParameterName),
                    Is.False,
                    "and the second sentence's \"you may ALSO retrieve\" is what takes Bain out of " +
                    "the first - a reading that let him pick himself would make the word redundant.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Green line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// All three Blocks declare milestone 8.40g's board condition, and the two with consequences run
        /// them at p.14's step 4 - where every Defense text runs - while Perfect Getaway composes
        /// nothing at all, which is what makes it the control.
        /// </summary>
        [Test]
        public void HisThreeBlocks_DeclareTheBoardConditionAndOnlyTwoCarryAConsequence()
        {
            foreach (var cardId in new[]
                     {
                         BainCardContent.CloseCall, BainCardContent.NarrowEscape,
                         BainCardContent.PerfectGetaway
                     })
            {
                var block = Primary(cardId);
                Assert.That(
                    block.DefenseResolution, Is.EqualTo(PrintedDefenseResolution.Block), cardId.Value);
                Assert.That(
                    block.BlockCondition,
                    Is.EqualTo(PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker), cardId.Value);
            }

            Assert.That(
                Primary(BainCardContent.CloseCall).CardTextTiming,
                Is.EqualTo(CardTextTiming.DefenseAction));
            Assert.That(
                Primary(BainCardContent.NarrowEscape).CardTextTiming,
                Is.EqualTo(CardTextTiming.DefenseAction));
            Assert.That(
                Primary(BainCardContent.PerfectGetaway).EffectRoot, Is.Null,
                "Perfect Getaway prints the condition and no consequence at all, which is why it is " +
                "the control that proves the condition alone.");
            Assert.That(
                Primary(BainCardContent.PerfectGetaway).CardTextTiming,
                Is.EqualTo(CardTextTiming.None));

            var closeCall = Primary(BainCardContent.CloseCall).EffectRoot;
            Assert.That(
                NodeOf(closeCall, "block-root").Capability,
                Is.EqualTo(BainComponentContent.BountyHeldV1),
                "The predicate IS the Conditional's capability: it publishes a Boolean itself, and " +
                "neither of these two branches names \"that hero\".");
            Assert.That(
                NodeOf(closeCall, "block-consequence").Capability,
                Is.EqualTo(BainComponentContent.TransferBountyV1));
            Assert.That(
                NodeOf(closeCall, "block-consequence").Inputs
                    .Single(input => input.ParameterName ==
                        SyntheticComponentCapabilities.MarkerTransferHandler.HolderParameterName)
                    .Role,
                Is.EqualTo(EffectRoleKind.Actor),
                "\"that hero gives the marker to YOU\" - the holder parameter is the DESTINATION, and " +
                "the hero it moves from is whoever wears the one attachment.");

            var narrow = Primary(BainCardContent.NarrowEscape).EffectRoot;
            Assert.That(
                NodeOf(narrow, "block-consequence").Capability,
                Is.EqualTo(BainComponentContent.ReturnBountyV1),
                "\"retrieve the marker\" is the rulebook's word for returning one to the supply, and " +
                "this face gave none - so it is 8.40b's definition-bound reading.");
        }

        /// <summary>
        /// Vantage Point and High Ground compose two Active effects each, because their two sentences
        /// are gated differently: the obstacle permission is unconditional and the raise is not. Both
        /// are read before the walk, which is p.11's default and the only reading under which either
        /// does anything.
        /// </summary>
        [Test]
        public void HisTwoMovementFaces_GrantAnUnconditionalPermissionAndAGatedRaise()
        {
            foreach (var pair in new[]
                     {
                         (Card: BainCardContent.VantagePoint, Amount: 1),
                         (Card: BainCardContent.HighGround, Amount: 2)
                     })
            {
                var action = Primary(pair.Card);
                var root = action.EffectRoot;

                Assert.That(action.Kind, Is.EqualTo(ActionKind.Movement), pair.Card.Value);
                Assert.That(
                    action.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheMovement),
                    pair.Card.Value + " raises the allowance, so it must be read before it is spent.");
                Assert.That(
                    ChildOrder(root, "stride-root"),
                    Is.EqualTo(new[] { "stride-ignore-obstacles", "stride-gate" }),
                    "\"Ignore obstacles.\" is the first printed sentence and it is ungated.");
                Assert.That(
                    NodeOf(root, "stride-gate").Capability,
                    Is.EqualTo(BainComponentContent.BountyHeldV1));
                Assert.That(
                    ChildOrder(root, "stride-gate"), Is.EqualTo(new[] { "stride-raise" }));

                var permission = BainActiveEffectContent.IgnoreObstaclesDefinitionFor(pair.Card);
                Assert.That(
                    permission.Modifications.Single().Permission,
                    Is.EqualTo(RulePermissionKind.MoveThroughObstacles), pair.Card.Value);
                Assert.That(
                    permission.Modifications.Single().Activity, Is.EqualTo(RuleActivity.Movement),
                    "The clause is printed on a Movement primary, so the permission says nothing " +
                    "about his Attacks.");

                var raise = BainActiveEffectContent.MovementRaiseDefinitionFor(pair.Card);
                Assert.That(
                    raise.Modifications.Single().Channel, Is.EqualTo(PrintedValueChannel.Movement));
                Assert.That(
                    raise.Modifications.Single().Amount, Is.EqualTo(pair.Amount), pair.Card.Value);
                Assert.That(raise.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn));
                Assert.That(
                    raise.AffectedScope, Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero));
            }
        }

        /// <summary>
        /// BA-Q3, settled and stated: the raise is the TURN's, so a SECOND Movement action in the same
        /// turn is raised too - which is Ursafar's Cold Ire answer inherited rather than re-decided.
        ///
        /// <para><b>The reason is structural rather than preferential, and this case asserts the
        /// structure rather than the preference.</b> The modification a raise declares is about the
        /// HERO and names no action at all, and <see cref="ActiveEffectDurationKind"/> carries no
        /// per-action window for it to have used instead - so the shortest scope this build can express
        /// for a value raise is the turn. The board arm below reads the composed effect's own
        /// contribution off the modification set, which is the number every Movement allowance in the
        /// turn is composed from.</para>
        /// </summary>
        [Test]
        public void TheMovementRaise_IsTheTurnsAndReachesASecondMovementActionInIt()
        {
            var raise = BainActiveEffectContent.MovementRaiseDefinitionFor(
                BainCardContent.VantagePoint);

            Assert.That(raise.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn),
                "There is no per-ACTION duration in this build, which is what settles BA-Q3 the way " +
                "Ursafar's is settled.");
            Assert.That(
                Enum.GetValues(typeof(ActiveEffectDurationKind)).Cast<ActiveEffectDurationKind>()
                    .Select(value => value.ToString()),
                Has.No.Member("ThisAction"),
                "Stated rather than assumed: the vocabulary has no narrower window to have chosen.");
            Assert.That(
                raise.Modifications.Single().ValueApplicability,
                Is.EqualTo(RuleValueApplicability.Always),
                "and the modification qualifies no action either - \"Always\" is what an unqualified " +
                "declaration means - so it is the hero's for the turn, which is exactly what makes a " +
                "second Movement action in the same turn raised.");

            var board = new BainBoard();
            board.CreateActiveEffect(BainActiveEffectContent.MovementRaiseFor(
                BainCardContent.VantagePoint));

            Assert.That(
                board.MovementContributions(), Is.EqualTo(1),
                "The composed effect contributes the printed +1 to the hero, and every Movement " +
                "allowance p.11 composes in this turn reads that same contribution.");
        }

        // -------------------------------------------------------------------------------------------
        // The Ultimate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.16's permanent ability: milestone 8.40h's condition and moment, and a consequence that
        /// names its subject through the frame rather than by asking.
        /// </summary>
        [Test]
        public void AComplicatedProfession_WatchesTheMomentAMarkerIsGivenAndDiscardsFromTheRecipient()
        {
            var rule = BainCardContent.Definition(BainCardContent.AComplicatedProfession).UltimateRule;

            Assert.That(rule, Is.Not.Null);
            Assert.That(
                rule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerGivesMarker));
            Assert.That(
                rule.TriggerCheckpoints,
                Is.EquivalentTo(new[] { TimingCheckpoint.MarkerGivenToHero }));
            Assert.That(rule.AfterPush.IsDeclared, Is.False);
            Assert.That(rule.BeforeAction.IsDeclared, Is.False);

            var triggered = BainEffectContent.AComplicatedProfessionTriggeredEffect(
                BainCardContent.ContentRevision);
            var discard = NodeOf(triggered, "profession-discard");

            Assert.That(
                discard.Capability, Is.EqualTo(SyntheticCardStateCapabilities.DiscardChosenCardV1),
                "\"discards a card, IF ABLE\" is revision 1's own contract - the arm that declines " +
                "silently.");
            Assert.That(discard.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble));
            Assert.That(
                discard.Inputs.Single().Role, Is.EqualTo(EffectRoleKind.OpposingHero),
                "The give already picked the hero out, so the recipient is a ROLE the frame carries " +
                "rather than a one-candidate prompt.");
        }

        // -------------------------------------------------------------------------------------------
        // p.15's upgrade pairs, and the hands they produce
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.15 upgrades by colour and Tier, so each Tier I face names its own Tier II pair and each
        /// Tier II face names the Tier III pair - transcribed from the shard's upgrade table rather than
        /// derived from the definitions.
        /// </summary>
        [Test]
        public void HisThreeUpgradeLines_MatchThePrintedTierStructure()
        {
            AssertSuccessors(
                BainCardContent.LightCrossbow,
                BainCardContent.HeavyCrossbow, BainCardContent.HandCrossbow);
            AssertSuccessors(
                BainCardContent.HeavyCrossbow, BainCardContent.Arbalest, BainCardContent.HunterSeeker);
            AssertSuccessors(
                BainCardContent.HandCrossbow, BainCardContent.Arbalest, BainCardContent.HunterSeeker);
            AssertSuccessors(BainCardContent.Arbalest);
            AssertSuccessors(BainCardContent.HunterSeeker);

            AssertSuccessors(
                BainCardContent.AGameOfChance,
                BainCardContent.DeadMansHand, BainCardContent.DrinkingBuddies);
            AssertSuccessors(
                BainCardContent.DeadMansHand,
                BainCardContent.WereNotDoneYet, BainCardContent.AnotherOne);
            AssertSuccessors(
                BainCardContent.DrinkingBuddies,
                BainCardContent.WereNotDoneYet, BainCardContent.AnotherOne);

            AssertSuccessors(
                BainCardContent.CloseCall,
                BainCardContent.NarrowEscape, BainCardContent.VantagePoint);
            AssertSuccessors(
                BainCardContent.NarrowEscape,
                BainCardContent.PerfectGetaway, BainCardContent.HighGround);
            AssertSuccessors(
                BainCardContent.VantagePoint,
                BainCardContent.PerfectGetaway, BainCardContent.HighGround);
        }

        /// <summary>
        /// The four progressed hands, and the printed lineages they follow: the first branch keeps the
        /// plain clear-line shot and the second keeps the marker modal, which is what makes both
        /// branches worth staging rather than one standing for the other.
        /// </summary>
        [Test]
        public void HisProgressedHands_StageTheTwoPrintedLineages()
        {
            var owner = new HeroId("bain-progression");

            Assert.That(
                Upgrades(owner, 2, BainUpgradeBranch.FirstSibling),
                Is.EquivalentTo(new[]
                {
                    BainCardContent.HeavyCrossbow, BainCardContent.DeadMansHand,
                    BainCardContent.NarrowEscape
                }));
            Assert.That(
                Upgrades(owner, 2, BainUpgradeBranch.SecondSibling),
                Is.EquivalentTo(new[]
                {
                    BainCardContent.HandCrossbow, BainCardContent.DrinkingBuddies,
                    BainCardContent.VantagePoint
                }));
            Assert.That(
                Upgrades(owner, 3, BainUpgradeBranch.FirstSibling),
                Is.EquivalentTo(new[]
                {
                    BainCardContent.Arbalest, BainCardContent.WereNotDoneYet,
                    BainCardContent.PerfectGetaway
                }));
            Assert.That(
                Upgrades(owner, 3, BainUpgradeBranch.SecondSibling),
                Is.EquivalentTo(new[]
                {
                    BainCardContent.HunterSeeker, BainCardContent.AnotherOne,
                    BainCardContent.HighGround
                }));

            var hand = BainProgressedHands.CreateTierThreeHand(
                owner, "bain-branch", BainUpgradeBranch.FirstSibling);
            Assert.That(
                hand.Instances.Concat(hand.DeckInstances).Count(), Is.EqualTo(18),
                "The Handicap card is already out of the box at p.6's setup, so a progressed hand is " +
                "built from his dealt eighteen.");
            Assert.That(
                hand.DeckInstances.Select(card => card.DefinitionReference.DefinitionId),
                Has.Member(BainCardContent.AComplicatedProfession),
                "and the Ultimate is dealt to the deck, which is what p.6 and the initial-setup " +
                "validator require.");
        }

        // -------------------------------------------------------------------------------------------
        // Behavioural arms, driven from his own composed nodes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The give, run through the production session from Dead or Alive's own composed clause: a
        /// fresh marker lands on the hero the selection named, and the moment reaches the frame.
        ///
        /// <para><b>The walk STOPS at the give, and that is the bound this composition inherits rather
        /// than a defect.</b> Milestone 8.40h's trigger stands at the card-text altitude and the page
        /// is not resumed past it, which is why the give is the last step of the clause and why both
        /// of his giving sentences end on it.</para>
        /// </summary>
        [Test]
        public void TheGive_PutsTheMarkerOnTheChosenHeroAndReportsTheMoment()
        {
            var board = new BainBoard();

            var step = board.RunGive(BainBoard.Enemy);

            Assert.That(
                board.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(BainBoard.Enemy));
            Assert.That(step.MarkerGivenTo, Is.EqualTo(BainBoard.Enemy),
                "and the moment milestone 8.40h minted is reported to the frame, which is what stops " +
                "the page so his Ultimate can run.");
            Assert.That(step.IsComplete, Is.False,
                "The page stops at the moment rather than running on, which is the inherited bound - " +
                "and the whole reason the give is composed as its clause's last step.");
            Assert.That(
                AfterMarkerGivenTriggerQuery.Derive(
                    board.UnlockedUltimates(), board.Board, BainBoard.Bain, BainBoard.Enemy).Count,
                Is.EqualTo(1),
                "and his unlocked Ultimate is what the moment reaches: the giver IS the controller.");
        }

        /// <summary>
        /// BA-D1's move, driven rather than reasoned about: with the supply of one already out, a second
        /// give MOVES the marker - identity kept - and is still a give he performed, so it still reports
        /// the moment.
        /// </summary>
        [Test]
        public void ASecondGive_MovesTheOneMarkerAndIsStillAGive()
        {
            var board = new BainBoard();
            board.RunGive(BainBoard.Enemy);
            var first = board.Markers.Attachments.Single().Id;

            var step = board.RunGive(BainBoard.OtherEnemy);

            Assert.That(board.Markers.Attachments.Count, Is.EqualTo(1),
                "One marker, because the supply is one and the give moved it rather than minting a " +
                "second.");
            Assert.That(
                board.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(BainBoard.OtherEnemy));
            Assert.That(board.Markers.Attachments.Single().Id, Is.EqualTo(first),
                "and the identity is the one the first give minted, which is what a restore rebinds " +
                "against.");
            Assert.That(step.MarkerGivenTo, Is.EqualTo(BainBoard.OtherEnemy),
                "BA-D5's edge: a give that moved an already-held marker is still a give he performed.");
        }

        /// <summary>
        /// The other half of BA-D5, and it is structural rather than a query's opinion: Close Call's
        /// transfer moves the same marker and reports NO give at all, so the Ultimate reaches nothing.
        /// </summary>
        [Test]
        public void TheTransfer_MovesTheMarkerToTheDefenderAndReportsNoGive()
        {
            var board = new BainBoard();
            board.RunGive(BainBoard.Enemy);

            var step = board.RunDefenseText(BainCardContent.CloseCall);

            Assert.That(step.IsComplete, Is.True);
            Assert.That(board.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(BainBoard.Bain),
                "\"that hero gives the marker to you\" - and \"you\" is the defender, who is this " +
                "frame's own actor.");
            Assert.That(step.MarkerGivenTo.IsDefined, Is.False,
                "A transfer is not a give, so his Ultimate cannot fire on another hero handing him " +
                "the marker - which is what BA-D5's second half asks for.");
            Assert.That(
                AfterMarkerGivenTriggerQuery.Derive(
                        board.UnlockedUltimates(), board.Board, BainBoard.Enemy, BainBoard.Bain),
                Is.Empty,
                "and the query refuses it from the other side too: the giver is not the controller.");
        }

        /// <summary>
        /// Narrow Escape's arm: the marker goes back to the supply wherever it was sitting, which is
        /// 8.40b's definition-bound reading of a return - this face gave nothing.
        /// </summary>
        [Test]
        public void TheReturn_TakesTheMarkerBackWhoeverWasWearingIt()
        {
            var board = new BainBoard();
            board.RunGive(BainBoard.Enemy);

            var step = board.RunDefenseText(BainCardContent.NarrowEscape);

            Assert.That(step.IsComplete, Is.True);
            Assert.That(board.Markers.Attachments, Is.Empty);
            Assert.That(
                board.Markers.CountAvailable(BainComponentContent.BountyDefinition), Is.EqualTo(1),
                "and the supply has its one copy back.");
        }

        /// <summary>
        /// The Block's condition, asked of the one query both its call sites read: true while any hero
        /// wears his marker and false on a board with none, whatever the attack was.
        /// </summary>
        [Test]
        public void TheBlockCondition_IsTrueOnlyWhileAHeroWearsHisMarker()
        {
            var board = new BainBoard();

            Assert.That(
                board.BlockVerdict(BainCardContent.PerfectGetaway),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "No marker is out, so the printed condition is false and p.14's step 6 gets the " +
                "numeric comparison it would have had.");

            board.RunGive(BainBoard.Enemy);

            Assert.That(
                board.BlockVerdict(BainCardContent.PerfectGetaway),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "and with the marker out the attack is blocked, which is the whole of what Perfect " +
                "Getaway prints.");
        }

        /// <summary>
        /// The pull, driven on a board that poses all three of its printed positions: a unit on a clear
        /// file is offered and travels, a target already adjacent takes no step, and a file whose first
        /// obstacle is terrain offers nothing at all.
        /// </summary>
        [Test]
        public void ThePull_OffersAClearFileAndBringsTheTargetAdjacent()
        {
            var board = new BainBoard();

            var offered = board.PullCandidates();

            Assert.That(offered, Has.Member(BainBoard.EnemySpace),
                "The enemy two spaces east stands on a clear file and is offered.");
            Assert.That(offered, Has.No.Member(BainBoard.TerrainShadowSpace),
                "and the hero standing behind a terrain space is not - \"no obstacles\" is p.18's " +
                "full list, whatever the narrowing admits as a NAME.");

            var step = board.RunPull(BainBoard.EnemySpace);

            Assert.That(step.IsComplete, Is.True);
            Assert.That(board.SpaceOf(BainBoard.Enemy), Is.EqualTo(BainBoard.PullApproachSpace),
                "\"until you are adjacent\" - the target ends on the space beside Bain, and he has " +
                "not moved.");
            Assert.That(board.SpaceOf(BainBoard.Bain), Is.EqualTo(BainBoard.BainSpace));
        }

        /// <summary>
        /// The third pull position, which is the one the <c>if able</c> policy exists for: a target
        /// already adjacent is published its own space, and the Move then takes no step rather than
        /// stopping the Skill.
        /// </summary>
        [Test]
        public void ThePull_TakesNoStepForATargetAlreadyAdjacent()
        {
            var board = new BainBoard();

            var step = board.RunPull(BainBoard.AllySpace);

            Assert.That(step.IsComplete, Is.True,
                "The clause resolves rather than stopping on a Move with nowhere to go.");
            Assert.That(board.SpaceOf(BainBoard.Ally), Is.EqualTo(BainBoard.AllySpace));
        }

        /// <summary>
        /// The guessing game end to end, from his own composed clause: a correct guess discards that one
        /// card and pays nothing, and the reveal names the card to the table.
        /// </summary>
        [Test]
        public void ACorrectGuess_DiscardsThatCardAndOnlyThatCard()
        {
            var board = new BainBoard();
            var chosen = board.EnemyHand[1];

            var step = board.RunGuess(BainCardContent.AGameOfChance, chosen, board.ColorOf(chosen));

            Assert.That(step.IsComplete, Is.True);
            Assert.That(chosen.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(
                board.EnemyHand.Where(card => !card.Id.Equals(chosen.Id))
                    .All(card => card.State.Location == CardLocation.Hand),
                Is.True,
                "\"discard THAT card\" names the card; a prompt here would let the chooser spend a " +
                "different one after the guess had already earned this one.");
            Assert.That(board.Coins.GetCoins(BainBoard.Bain), Is.Zero);
            Assert.That(
                step.Events.SelectMany(value => value.RevealedCards).Select(card => card.CardInstanceId),
                Has.Member(chosen.Id),
                "and the reveal names the card, which is what makes it public in every seat.");
        }

        /// <summary>
        /// A wrong guess takes nothing and pays the printed coins - one on the Tier I face and two on
        /// the Tier II one, which is one of the three places BA-11 says the three faces differ.
        /// </summary>
        [Test]
        public void AWrongGuess_PaysThePrintedCoinsAndTakesNothing()
        {
            foreach (var pair in new[]
                     {
                         (Card: BainCardContent.AGameOfChance, Coins: 1),
                         (Card: BainCardContent.DeadMansHand, Coins: 2)
                     })
            {
                var board = new BainBoard();
                var chosen = board.EnemyHand[1];

                var step = board.RunGuess(pair.Card, chosen, board.WrongColorFor(chosen));

                Assert.That(step.IsComplete, Is.True, pair.Card.Value);
                Assert.That(chosen.State.Location, Is.EqualTo(CardLocation.Hand), pair.Card.Value);
                Assert.That(
                    board.Coins.GetCoins(BainBoard.Bain), Is.EqualTo(pair.Coins), pair.Card.Value);
            }
        }

        /// <summary>
        /// The Tier III failure modal, reached only on the failure branch: both bullets are offered, the
        /// coin bullet pays two, and the repeat asks for exactly one more instance of the whole clause.
        /// </summary>
        [Test]
        public void TheFailureModal_OffersBothBulletsAndOnlyOnTheFailureBranch()
        {
            var board = new BainBoard();
            var chosen = board.EnemyHand[1];

            var modal = board.RunGuessToTheModal(chosen, board.WrongColorFor(chosen));

            Assert.That(modal.PendingChoice, Is.Not.Null, "The failure branch pauses on the modal.");
            Assert.That(modal.PendingChoice.NodeId, Is.EqualTo(new EffectNodeId("guess-modal")));
            Assert.That(modal.PendingChoice.Candidates.Count, Is.EqualTo(2),
                "Both printed bullets - repeat once, or take the coins.");

            var coins = board.TakeBullet(modal, new EffectNodeId("guess-coin-bullet"));
            Assert.That(coins.IsComplete, Is.True);
            Assert.That(board.Coins.GetCoins(BainBoard.Bain), Is.EqualTo(2));

            var correct = new BainBoard();
            var right = correct.EnemyHand[1];
            var step = correct.RunGuess(
                BainCardContent.WereNotDoneYet, right, correct.ColorOf(right));
            Assert.That(step.IsComplete, Is.True);
            Assert.That(step.Frame, Is.Null,
                "A correct guess takes the discard branch and finishes; the modal belongs to the " +
                "other arm of the Conditional and is not offered at all.");
            Assert.That(correct.Coins.GetCoins(BainBoard.Bain), Is.Zero);
        }

        /// <summary>
        /// The repeat bullet, taken: it asks for exactly one further instance of the whole clause, which
        /// is BA-Q2's settled reading - a second run re-asks every question the first one asked.
        /// </summary>
        [Test]
        public void TheRepeatBullet_AsksForExactlyOneMoreInstanceOfTheWholeClause()
        {
            var board = new BainBoard();
            var chosen = board.EnemyHand[1];
            var modal = board.RunGuessToTheModal(chosen, board.WrongColorFor(chosen));

            var taken = board.TakeBullet(modal, new EffectNodeId("guess-repeat"));

            Assert.That(taken.RepeatRequested.IsRequested, Is.True);
            Assert.That(taken.RepeatRequested.Continuation.IsRepeatedInstance, Is.True,
                "and the instance it asks for is a REPEATED one, which is what carries p.10's bound.");
            Assert.That(board.Coins.GetCoins(BainBoard.Bain), Is.Zero,
                "A Choose of one is one bullet, so the coins were not also taken.");
        }

        /// <summary>
        /// The guess domain never reads a hand (BA-D2): the offer is identical whichever card the enemy
        /// hid, asserted by running the same board twice with two different answers.
        /// </summary>
        [Test]
        public void TheGuessDomain_IsTheSameWhicheverCardWasHidden()
        {
            var first = new BainBoard();
            var second = new BainBoard();

            var one = first.OfferedColorsAfterHiding(first.EnemyHand[0]);
            var two = second.OfferedColorsAfterHiding(second.EnemyHand[1]);

            Assert.That(one, Is.EqualTo(two),
                "Two different hidden cards, one offer - asserted through the live resolver on his " +
                "own composed node rather than against the handler alone.");
            Assert.That(one, Has.No.Member(CardColor.Purple),
                "and Purple is excluded twice over: no printed definition in the build carries a " +
                "Purple progression, and the narrowing names it anyway.");
        }

        /// <summary>
        /// BA-Q1 on a live board, where the three readings give three different candidate sets: the
        /// first hero may be an enemy, may be an ally, and is never Bain himself.
        /// </summary>
        [Test]
        public void TheRetrievalSelection_OffersEnemiesAndAlliesAndNeverBainHimself()
        {
            var board = new BainBoard();

            var offered = board.RetrievalCandidates();

            Assert.That(offered, Has.Member(BainBoard.Enemy),
                "\"a hero\" with no side-word reaches an enemy, which is the reading RULING 8 gave " +
                "the same grammar on Silverarrow's face.");
            Assert.That(offered, Has.Member(BainBoard.Ally),
                "and an ally, which is what p.21's Healer role describes.");
            Assert.That(offered, Has.No.Member(BainBoard.Bain),
                "and never himself - \"you may ALSO retrieve\" is a word that means nothing unless " +
                "the first retrieval was somebody else's.");
        }

        /// <summary>
        /// "If they do", driven: his own retrieval is offered after a hero retrieved, and is not offered
        /// at all after one declined - which is milestone 8.29e's rule doing the condition's work.
        /// </summary>
        [Test]
        public void HisOwnRetrieval_IsOfferedOnlyAfterTheOtherHeroActuallyRetrieved()
        {
            var accepted = new BainBoard();
            var declined = new BainBoard();

            var afterRetrieval = accepted.RunSharedRetrieval(declineTheFirstRetrieval: false);
            var afterDecline = declined.RunSharedRetrieval(declineTheFirstRetrieval: true);

            Assert.That(afterRetrieval.OfferedHisOwn, Is.True,
                "The retrieval published its own success, so the Conditional branched and his own " +
                "retrieval opened its card choice.");
            Assert.That(afterDecline.OfferedHisOwn, Is.False,
                "and a decline published nothing, which an unbound flag reads as false - so the " +
                "branch is not taken at all, which is the whole of \"if they do\".");
            Assert.That(afterDecline.Completed, Is.True,
                "and the clause still finished rather than stopping on a refused step, which is why " +
                "the condition is a Conditional: Another One! has its keyword sentence to arm " +
                "afterwards.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static readonly CardDefinitionId[] GuessingFaces =
        {
            BainCardContent.AGameOfChance, BainCardContent.DeadMansHand,
            BainCardContent.WereNotDoneYet
        };

        private static EffectCapabilityCatalog Catalog() =>
            new EffectCapabilityCatalog(
                MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                    .Select(MatchContentCatalogs.ForThisBuild().Capabilities.Load)
                    .Concat(BainComponentContent.Handlers())
                    .Concat(BainActiveEffectContent.Handlers())
                    .Concat(SyntheticHiddenChoiceCapabilities.Handlers())
                    .GroupBy(handler => handler.Reference)
                    .Select(group => group.First()));

        private static string Describe(CardDefinition definition)
        {
            var primary = definition.ActionDefinitions
                .FirstOrDefault(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            var movement = SecondaryMovement(definition.Id);
            var defense = SecondaryDefense(definition.Id);
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
                BainCardContent.Definition(cardId).Progression.NextTierCandidateIds,
                Is.EquivalentTo(expected), cardId.Value);

        private static IReadOnlyList<CardDefinitionId> Upgrades(
            HeroId owner, int tier, BainUpgradeBranch branch)
        {
            var hand = tier == 2
                ? BainProgressedHands.CreateTierTwoHand(owner, "bain-branch", branch)
                : BainProgressedHands.CreateTierThreeHand(owner, "bain-branch", branch);
            return hand.Instances
                .Select(card => card.DefinitionReference.DefinitionId)
                .Where(id => !id.Equals(BainCardContent.DeadOrAlive) &&
                    !id.Equals(BainCardContent.GetOverHere))
                .ToArray();
        }

        private static ActionDefinition Primary(CardDefinitionId cardId) =>
            BainCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        private static ActionDefinition SecondaryMovement(CardDefinitionId cardId) =>
            BainCardContent.Definition(cardId).ActionDefinitions
                .FirstOrDefault(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary &&
                    action.Kind == ActionKind.Movement);

        private static ActionDefinition SecondaryDefense(CardDefinitionId cardId) =>
            BainCardContent.Definition(cardId).ActionDefinitions
                .FirstOrDefault(action =>
                    action.Origin == ActionDefinitionOrigin.Reaction &&
                    action.Kind == ActionKind.Defense);

        private static int RadiusOf(CardDefinitionId cardId) =>
            Primary(cardId).BaseValues
                .Single(value => value.Channel == PrintedValueChannel.Radius).Amount;

        private static int CoinsOf(CardDefinitionId cardId) =>
            IntegerInput(NodeOf(Primary(cardId).EffectRoot, "guess-coin-bullet"), "amount");

        private static EffectNodeDefinition NodeOf(EffectDefinition root, string nodeId) =>
            root.Nodes.Single(node => node.Id.Equals(new EffectNodeId(nodeId)));

        private static string[] ChildOrder(EffectDefinition root, string nodeId) =>
            NodeOf(root, nodeId).ChildNodeIds.Select(child => child.Value).ToArray();

        private static int IntegerInput(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal.IntegerValue;

        private static bool BooleanInput(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal.BooleanValue;

        // -------------------------------------------------------------------------------------------
        // The board every behavioural arm above is driven on
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One board, one marker register, one coin ledger and one set of hands - built fresh per case
        /// so a card one case discards cannot leak into the next. Every walk here runs one of BAIN'S
        /// OWN composed effect roots through the production resolution session, against the catalog
        /// this file validates his faces with.
        ///
        /// <para>The layout poses all three of the pull's printed positions at once: an enemy two
        /// spaces east on a clear file, an ally already adjacent, and a second enemy two spaces west
        /// standing behind a terrain space.</para>
        /// </summary>
        private sealed class BainBoard
        {
            public static readonly HeroId Bain = new HeroId("bain-hunter");
            public static readonly HeroId Ally = new HeroId("bain-ally");
            public static readonly HeroId Enemy = new HeroId("bain-enemy");
            public static readonly HeroId OtherEnemy = new HeroId("bain-other-enemy");

            public static readonly HexCoordinate BainSpace = new HexCoordinate(0, 0);
            public static readonly HexCoordinate AllySpace = new HexCoordinate(0, -1);
            public static readonly HexCoordinate EnemySpace = new HexCoordinate(2, 0);
            public static readonly HexCoordinate PullApproachSpace = new HexCoordinate(1, 0);
            public static readonly HexCoordinate TerrainShadowSpace = new HexCoordinate(-2, 0);

            private static readonly HexCoordinate TerrainSpace = new HexCoordinate(-1, 0);
            private static readonly TeamId RedTeam = new TeamId("bain-red");
            private static readonly TeamId BlueTeam = new TeamId("bain-blue");
            private static readonly ZoneId Battle = new ZoneId("bain-printed-set-zone");

            private readonly MatchParticipants participants;
            private readonly MatchComponents components;
            private readonly List<CardInstance> cards = new List<CardInstance>();
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();
            private readonly EffectCapabilityCatalog catalog = Catalog();

            public BainBoard()
            {
                participants = BuildParticipants();
                Board = new AdjacentMoveAuthority(
                    Map(),
                    new[]
                    {
                        new HeroOccupancy(Bain, BainSpace),
                        new HeroOccupancy(Ally, AllySpace),
                        new HeroOccupancy(Enemy, EnemySpace),
                        new HeroOccupancy(OtherEnemy, TerrainShadowSpace)
                    },
                    Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                components = new MatchComponents(
                    new ComponentDefinitionCatalog(
                        new[]
                        {
                            SyntheticComponentDefinitions.HazardDefinition,
                            SyntheticComponentDefinitions.WardDefinition
                        },
                        new[] { SyntheticComponentDefinitions.FocusDefinition }
                            .Concat(BainComponentContent.Markers())),
                    Board,
                    Markers,
                    Board,
                    Board,
                    Board);
                Coins = new HeroCoinLedger(participants);

                // The enemy's HAND carries three colours, which is what the guess is played against,
                // and the fourth colour of their deck sits in the discard - so BA-D2's domain is the
                // three the open zones cannot account for.
                EnemyHand = new[]
                {
                    Deal("enemy-red", BainCardContent.LightCrossbow, Enemy),
                    Deal("enemy-blue", BainCardContent.AGameOfChance, Enemy),
                    Deal("enemy-green", BainCardContent.CloseCall, Enemy)
                };
                EnemyDiscard = Discard("enemy-discard", BainCardContent.DeadOrAlive, Enemy);
                BainDiscard = Discard("bain-discard", BainCardContent.NarrowEscape, Bain);
                Discard("ally-discard", BainCardContent.DeadMansHand, Ally);
                Deal("other-enemy-hand", BainCardContent.HighGround, OtherEnemy);
                Deal("bain-hand", BainCardContent.DrinkingBuddies, Bain);

                var ultimate = new CardInstance[]
                {
                    CardInstance.CreateInDeck(
                        new CardInstanceId("bain-ultimate"),
                        BainCardContent.Definition(BainCardContent.AComplicatedProfession).Reference,
                        Bain)
                }.Single();
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
                cards.Add(ultimate);
            }

            public AdjacentMoveAuthority Board { get; }

            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();

            public HeroCoinLedger Coins { get; }

            public IReadOnlyList<CardInstance> EnemyHand { get; }

            public CardInstance EnemyDiscard { get; }

            public CardInstance BainDiscard { get; }

            // ---- p.16's unlocked ability, for the trigger query's own reading

            public UnlockedUltimateSet UnlockedUltimates() =>
                UltimateUnlockQuery.Derive(BainCardContent.AllDefinitions, cards);

            // ---- The marker clauses

            public EffectResolutionStepResult RunGive(HeroId recipient)
            {
                var step = Start(
                    BainCardContent.DeadOrAlive,
                    Primary(BainCardContent.DeadOrAlive).EffectRoot,
                    BainCardContent.PrimaryBasicAttack,
                    ActionKind.Attack,
                    watchesMarkerGives: true);

                step = Confirm(step);
                return Answer(step, EffectValue.Hero(recipient));
            }

            public EffectResolutionStepResult RunDefenseText(CardDefinitionId cardId) =>
                Start(
                    cardId,
                    Primary(cardId).EffectRoot,
                    BainCardContent.PrimaryBlock,
                    ActionKind.Defense,
                    watchesMarkerGives: true);

            public DefenseBlockResult BlockVerdict(CardDefinitionId blockCardId) =>
                DefenseBlockQuery.Evaluate(
                    new ActionOffer(
                        new CardInstanceId("bain-block-instance"),
                        BainCardContent.Definition(blockCardId).Reference,
                        BainCardContent.PrimaryBlock,
                        Bain,
                        ActionInvocationContext.PrintedPrimary,
                        true),
                    BainCardContent.AllDefinitions,
                    BainCardContent.Definition(BainCardContent.LightCrossbow).Reference,
                    BainCardContent.PrimaryRangedAttack,
                    Markers);

            // ---- The pull

            public IReadOnlyList<HexCoordinate> PullCandidates() =>
                StartPull().Frame.PendingChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate)
                    .ToArray();

            public EffectResolutionStepResult RunPull(HexCoordinate target) =>
                Answer(StartPull(), EffectValue.Space(target));

            public HexCoordinate SpaceOf(HeroId hero)
            {
                Assert.That(Board.TryGetHeroCoordinate(hero, out var coordinate), Is.True);
                return coordinate;
            }

            // ---- The guessing clause

            public CardColor ColorOf(CardInstance card) =>
                BainCardContent.AllDefinitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Progression.Color;

            public CardColor WrongColorFor(CardInstance card) =>
                OfferedColorsAfterHiding(card).First(color => color != ColorOf(card));

            public IReadOnlyList<CardColor> OfferedColorsAfterHiding(CardInstance hidden) =>
                RunToTheColourPrompt(BainCardContent.AGameOfChance, hidden)
                    .Frame.PendingChoice.Candidates
                    .Select(candidate => (CardColor)candidate.Value.IntegerValue)
                    .ToArray();

            public EffectResolutionStepResult RunGuess(
                CardDefinitionId cardId, CardInstance hidden, CardColor named) =>
                Answer(RunToTheColourPrompt(cardId, hidden), EffectValue.Integer((int)named));

            public PendingResolution RunGuessToTheModal(CardInstance hidden, CardColor named) =>
                RunGuess(BainCardContent.WereNotDoneYet, hidden, named).Frame;

            public EffectResolutionStepResult TakeBullet(PendingResolution modal, EffectNodeId bullet)
            {
                var choice = modal.PendingChoice;
                var select = choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(bullet));
                // Milestone 8.46b: "Choose one" admits exactly one bullet, so taking it is the whole
                // answer and the completion is asked for rather than assumed.
                var step = EffectResolutionSession.Answer(modal, select, Context(ContextState));
                if (StillOwesItsCompletion(step))
                {
                    step = Confirm(step);
                }

                // p.10's confirm-or-decline stands on the repeat itself once its branch is chosen.
                return step.Frame?.PendingChoice != null ? Confirm(step) : step;
            }

            // ---- The shared retrieval

            public IReadOnlyList<HeroId> RetrievalCandidates() =>
                StartSkill(BainCardContent.DrinkingBuddies).Frame.PendingChoice.Candidates
                    .Select(candidate => candidate.Value.HeroId)
                    .ToArray();

            public (bool OfferedHisOwn, bool Completed) RunSharedRetrieval(
                bool declineTheFirstRetrieval)
            {
                var step = Answer(
                    StartSkill(BainCardContent.DrinkingBuddies), EffectValue.Hero(Enemy));
                step = declineTheFirstRetrieval
                    ? Decline(step)
                    : Answer(step, EffectValue.Card(EnemyDiscard.Id));

                var offered = step.Frame?.PendingChoice != null &&
                    step.Frame.PendingChoice.NodeId.Equals(
                        new EffectNodeId("buddies-also-retrieve"));
                if (offered)
                {
                    step = Answer(step, EffectValue.Card(BainDiscard.Id));
                }

                return (offered, step.IsComplete);
            }

            // ---- The Movement raise

            public void CreateActiveEffect(EffectCapabilityReference reference)
            {
                var handler = BainActiveEffectContent.Handlers()
                    .Single(candidate => candidate.Reference.Equals(reference));
                var result = handler.Execute(new EffectCapabilityContext(
                    new EffectNodeId("stride-raise"),
                    EffectFailurePolicy.Mandatory,
                    Bain,
                    Bain,
                    new CardInstanceId("bain-stride-instance"),
                    BainCardContent.Definition(BainCardContent.VantagePoint).Reference,
                    BainCardContent.PrimaryMovement,
                    new Dictionary<string, EffectValue>(),
                    1,
                    1,
                    participants,
                    timing,
                    RuleModificationSet.Empty,
                    EffectCardState.Absent,
                    ActionKind.Movement,
                    components: components));

                Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            }

            public int MovementContributions() =>
                RuleModificationQuery.Build(
                        timing,
                        Board,
                        participants,
                        TieBreakerState.Unrevealed,
                        UnlockedUltimateSet.None,
                        RuleObject.None,
                        null,
                        Markers)
                    .ValueContributions(RuleObject.ForHero(Bain), PrintedValueChannel.Movement)
                    .Sum(value => value.Amount);

            // ---- The walk

            private EffectResolutionStepResult StartPull() =>
                StartSkill(BainCardContent.GetOverHere);

            private EffectResolutionStepResult StartSkill(CardDefinitionId cardId) =>
                Start(
                    cardId,
                    Primary(cardId).EffectRoot,
                    Primary(cardId).Id,
                    ActionKind.Skill,
                    watchesMarkerGives: false);

            private EffectResolutionStepResult RunToTheColourPrompt(
                CardDefinitionId cardId, CardInstance hidden)
            {
                var step = Answer(StartSkill(cardId), EffectValue.Hero(Enemy));
                Assert.That(step.Frame?.PendingChoice, Is.Not.Null,
                    "The clause opens the enemy's own card choice.");
                Assert.That(step.Frame.PendingChoice.ChooserId, Is.EqualTo(Enemy),
                    "and the hidden choice belongs to the hero the selection named.");
                return Answer(step, EffectValue.Card(hidden.Id));
            }

            private EffectResolutionStepResult Start(
                CardDefinitionId cardId,
                EffectDefinition definition,
                ActionDefinitionId actionId,
                ActionKind actionKind,
                bool watchesMarkerGives)
            {
                ContextState = new WalkState(definition, actionKind, watchesMarkerGives);
                return EffectResolutionSession.Start(
                    new EffectResolutionId("bain-printed-set-" + cardId.Value),
                    SourceInstanceOf(cardId).Id,
                    BainCardContent.Definition(cardId).Reference,
                    actionId,
                    Bain,
                    Bain,
                    ActionInvocationContext.PrintedPrimary,
                    Context(ContextState));
            }

            private WalkState ContextState { get; set; }

            private EffectResolutionContext Context(WalkState state) =>
                new EffectResolutionContext(
                    state.Definition,
                    catalog,
                    participants,
                    timing,
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(BainCardContent.AllDefinitions, cards),
                    state.ActionKind,
                    components: components,
                    rosterState: EffectRosterState.Of(
                        Array.Empty<RoundTurnSlot>(),
                        new RoundHeroDefeatHistory(participants),
                        Coins),
                    watchesMarkerGives: state.WatchesMarkerGives);

            /// <summary>
            /// Names one candidate and closes the choice where closing it is still a separate
            /// command - the commands a real seat submits, however many that is.
            ///
            /// <para>Milestone 8.46b. It was always two. A choice admitting exactly one selection is
            /// now answered by the select alone, inside the resolution session, so the second
            /// command is asked for rather than assumed: a standing choice holding the selection and
            /// offering a completion still gets one, and Bain's own wider selections still take the
            /// pair.</para>
            /// </summary>
            private EffectResolutionStepResult Answer(
                EffectResolutionStepResult step, EffectValue value)
            {
                var choice = step.Frame.PendingChoice;
                var select = choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Equals(value));
                var selected = EffectResolutionSession.Answer(step.Frame, select, Context(ContextState));
                return StillOwesItsCompletion(selected) ? Confirm(selected) : selected;
            }

            /// <summary>
            /// Milestone 8.46b. Whether a selection just made left a completion command owed: a
            /// standing choice that HOLDS the selection - a choice with nothing selected is the next
            /// question, opened by the same command - and that offers one.
            /// </summary>
            private static bool StillOwesItsCompletion(EffectResolutionStepResult step) =>
                step.Frame?.PendingChoice != null &&
                step.Frame.PendingChoice.SelectedCandidates.Count > 0 &&
                step.Frame.PendingChoice.ConfirmInvocation != null;

            private EffectResolutionStepResult Confirm(EffectResolutionStepResult step) =>
                EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, Context(ContextState));

            /// <summary>
            /// The other answer a `may` opens, which is not the same command as confirming with
            /// nothing named: milestone 8.35z5 keeps Confirm gated on the handler's real floor and
            /// offers Decline beside it.
            /// </summary>
            private EffectResolutionStepResult Decline(EffectResolutionStepResult step) =>
                EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.DeclineInvocation, Context(ContextState));

            // ---- Staging

            private CardInstance SourceInstanceOf(CardDefinitionId cardId)
            {
                var existing = cards.FirstOrDefault(card =>
                    card.OwnerId.Equals(Bain) &&
                    card.DefinitionReference.DefinitionId.Equals(cardId));
                if (existing != null)
                {
                    return existing;
                }

                return Deal("bain-source-" + cardId.Value, cardId, Bain);
            }

            private CardInstance Deal(string instanceId, CardDefinitionId cardId, HeroId owner)
            {
                var card = CardInstance.CreateInHand(
                    new CardInstanceId(instanceId),
                    BainCardContent.Definition(cardId).Reference,
                    owner);
                cards.Add(card);
                return card;
            }

            private CardInstance Discard(string instanceId, CardDefinitionId cardId, HeroId owner)
            {
                var card = Deal(instanceId, cardId, owner);
                Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
                return card;
            }

            private static MatchParticipants BuildParticipants()
            {
                var players = new List<Player>();
                var heroes = new List<Hero>();
                foreach (var pair in new[]
                {
                    (Hero: Bain, Team: RedTeam),
                    (Hero: Ally, Team: RedTeam),
                    (Hero: Enemy, Team: BlueTeam),
                    (Hero: OtherEnemy, Team: BlueTeam)
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

            private static BoardMapDefinition Map() =>
                new BoardMapDefinition(
                    new MapId("bain-printed-set-map"),
                    1,
                    "Bain Printed Set Map",
                    Disk(BainSpace, 4)
                        .Select(coordinate =>
                        {
                            // p.19: "Any board space without a full grid outline counts as terrain and
                            // is an obstacle." One such space sits between Bain and the enemy to his
                            // west, which is the file the pull must offer nothing on. Terrain belongs
                            // to no zone, which the map's own constructor enforces.
                            var isTerrain = coordinate.Q == TerrainSpace.Q &&
                                coordinate.R == TerrainSpace.R;
                            return new BoardMapSpace(
                                new SpaceId("s" + coordinate.Q + "_" + coordinate.R),
                                coordinate,
                                isTerrain ? BoardMapSpaceKind.Terrain : BoardMapSpaceKind.Playable,
                                isTerrain ? (ZoneId?)null : Battle);
                        })
                        .ToArray(),
                    new[] { new BoardMapZone(Battle, "Battle", "lane", null, false, true) },
                    Array.Empty<BoardMapSpawnPoint>(),
                    Array.Empty<ZoneId>());

            private static IEnumerable<HexCoordinate> Disk(HexCoordinate centre, int radius)
            {
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = Math.Max(-radius, -q - radius);
                        r <= Math.Min(radius, -q + radius);
                        r++)
                    {
                        yield return new HexCoordinate(centre.Q + q, centre.R + r);
                    }
                }
            }

            /// <summary>
            /// Which definition a walk is running and under which action, so a resumed pass builds the
            /// same context the first one did.
            /// </summary>
            private sealed class WalkState
            {
                public WalkState(
                    EffectDefinition definition, ActionKind actionKind, bool watchesMarkerGives)
                {
                    Definition = definition;
                    ActionKind = actionKind;
                    WatchesMarkerGives = watchesMarkerGives;
                }

                public EffectDefinition Definition { get; }

                public ActionKind ActionKind { get; }

                public bool WatchesMarkerGives { get; }
            }
        }
    }
}
