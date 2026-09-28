using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone <c>8.35z33</c>. Whisper the Outcast's nineteen printed faces as definition
    /// content, checked against the source inventory a read-only worker verified against the
    /// extraction rather than against themselves.
    ///
    /// <para>The printed-value assertion below is deliberately a literal table in the inventory's own
    /// column order, for the reason the six verified sets before hers record: a test that derived its
    /// expectations from the definitions would pass whatever the definitions happened to say.</para>
    ///
    /// <para>Her edition answer is the weakest of the roster and is a ruling rather than an assertion:
    /// the second-printing rulebook names neither her nor any of her nineteen cards, because she is a
    /// Devoted Hero pack hero, and the 2026-09-06 pack-wide ruling makes the Tabletopia extraction the
    /// sole authority for a pack hero's faces. Nothing here can check that; it is recorded so the
    /// absence is not mistaken for an omission.</para>
    /// </summary>
    public sealed class WhisperVerifiedContentTests
    {
        /// <summary>
        /// One row per face, in the inventory's table order:
        /// id | name | Init | Sec Def | Sec Mov | primary | colour/Tier | item icon.
        /// A dash is what the face prints nothing for.
        /// </summary>
        private static readonly string[] SourceInventory =
        {
            "whisper-swift-justice|Swift Justice|Init 12|SecDef 2|SecMov 1|primary-ranged-attack Attack Ranged [Attack 2, Range 3]|Gold Basic|item -",
            "whisper-vile-accusation|Vile Accusation|Init 11|SecDef 1|SecMov 1|primary-ranged-attack Attack Ranged [Attack 1, Range 2]|Gold Basic|item -",
            "whisper-death-seeker|Death Seeker|Init 13|SecDef 2|SecMov -|primary-skill Skill None [Radius 3]|Silver Basic|item -",
            "whisper-crimson-trail|Crimson Trail|Init 9|SecDef 4|SecMov 4|primary-attack Attack NonRanged [Attack 3, Radius 2]|Red I|item -",
            "whisper-sanguine-path|Sanguine Path|Init 9|SecDef 4|SecMov 4|primary-attack Attack NonRanged [Attack 3, Radius 2]|Red II|item Defense",
            "whisper-blood-fury|Blood Fury|Init 9|SecDef 6|SecMov 4|primary-attack Attack NonRanged [Attack 6, Radius 5]|Red II|item Initiative",
            "whisper-blood-pilgrimage|Blood Pilgrimage|Init 10|SecDef 5|SecMov 4|primary-attack Attack NonRanged [Attack 4, Radius 2]|Red III|item Movement",
            "whisper-blood-frenzy|Blood Frenzy|Init 10|SecDef 7|SecMov 4|primary-attack Attack NonRanged [Attack 7, Radius 5]|Red III|item Range",
            "whisper-seeds-of-fear|Seeds of Fear|Init 10|SecDef 5|SecMov 2|primary-skill Skill None [Radius 2]|Blue I|item -",
            "whisper-sprouts-of-panic|Sprouts of Panic|Init 10|SecDef 5|SecMov 2|primary-skill Skill None [Radius 3]|Blue II|item Initiative",
            "whisper-lesser-evil|Lesser Evil|Init 10|SecDef 5|SecMov 2|primary-skill Skill None [Range 3]|Blue II|item Attack",
            "whisper-blooming-nightmare|Blooming Nightmare|Init 11|SecDef 6|SecMov 2|primary-skill Skill None [Radius 3]|Blue III|item Initiative",
            "whisper-greater-good|Greater Good|Init 11|SecDef 6|SecMov 2|primary-skill Skill None [Range 3]|Blue III|item Attack",
            "whisper-shadow-step|Shadow Step|Init 3|SecDef 3|SecMov 2|primary-skill Skill None [Range 3]|Green I|item -",
            "whisper-shadow-walk|Shadow Walk|Init 2|SecDef 3|SecMov 2|primary-skill Skill None [Range 4]|Green II|item Attack",
            "whisper-cruel-twist|Cruel Twist|Init 2|SecDef 3|SecMov 2|primary-skill Skill None [Range 4]|Green II|item Defense",
            "whisper-creeping-shadow|Creeping Shadow|Init 2|SecDef 4|SecMov 2|primary-skill Skill None [Range 4]|Green III|item Radius",
            "whisper-sealed-fate|Sealed Fate|Init 2|SecDef 4|SecMov 2|primary-skill Skill None [Range 4]|Green III|item Defense",
            "whisper-grim-reaper|Grim Reaper|Init -|SecDef -|SecMov -|- Ultimate []|Ultimate IV|item -"
        };

        [Test]
        public void EveryPrintedFace_MatchesTheSourceInventory()
        {
            Assert.That(
                WhisperCardContent.AllDefinitions.Select(Describe).ToArray(),
                Is.EqualTo(SourceInventory),
                "A printed value, colour, Tier, icon or name differs from the inventory verified " +
                "against the extracted faces. Correct the definition, not this table - the table is " +
                "the source.");
        }

        [Test]
        public void TheDeckIsPageSixsEighteenCards_PlusThePageTwentyHandicapCard()
        {
            Assert.That(WhisperCardContent.AllDefinitions, Has.Count.EqualTo(19));
            Assert.That(WhisperCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(WhisperCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { WhisperCardContent.VileAccusation }),
                "p.6 returns the Handicap card to the box at setup.");

            Assert.That(
                WhisperCardContent.StartingHand,
                Is.EqualTo(new[]
                {
                    WhisperCardContent.SwiftJustice,
                    WhisperCardContent.DeathSeeker,
                    WhisperCardContent.CrimsonTrail,
                    WhisperCardContent.SeedsOfFear,
                    WhisperCardContent.ShadowStep
                }),
                "p.6 step 11 deals the three Tier I cards and the two basic cards with no Tier.");

            Assert.That(
                WhisperCardContent.StartingHand
                    .Select(id => WhisperCardContent.Definition(id).Progression.Color)
                    .Distinct()
                    .Count(),
                Is.EqualTo(5),
                "p.15: \"exactly 5 cards, 1 of each color, in your hand\".");

            Assert.That(WhisperCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(
                WhisperCardContent.Deck.Concat(WhisperCardContent.StartingHand)
                    .Concat(WhisperCardContent.ReturnedToBoxAtSetup)
                    .OrderBy(id => id.Value, StringComparer.Ordinal),
                Is.EqualTo(WhisperCardContent.AllDefinitions
                    .Select(definition => definition.Id)
                    .OrderBy(id => id.Value, StringComparer.Ordinal)),
                "Every face belongs to exactly one of the three places p.6 puts it.");

            foreach (var colour in new[] { CardColor.Red, CardColor.Green, CardColor.Blue })
            {
                var set = WhisperCardContent.AllDefinitions
                    .Where(definition => definition.Progression.Color == colour)
                    .ToArray();
                Assert.That(
                    set.Select(definition => definition.Progression.Tier).OrderBy(tier => tier),
                    Is.EqualTo(new[] { CardTier.I, CardTier.II, CardTier.II, CardTier.III, CardTier.III }),
                    "Each colour set is one Tier I, two Tier II and two Tier III; " + colour + " is not.");
            }
        }

        /// <summary>
        /// The shape the inventory records without exception, and it is the same shape Sabina's set
        /// has: every Red card is an Attack, every Blue and Green card is a Skill, and there is no
        /// Defense primary, no Movement primary and no p.14 Block anywhere.
        /// </summary>
        [Test]
        public void EveryRedFaceIsAnAttack_EveryBlueAndGreenFaceIsASkill_AndNothingElseIsPrinted()
        {
            foreach (var definition in WhisperCardContent.AllDefinitions)
            {
                var primary = Primary(definition);
                if (definition.Progression.IsUltimate)
                {
                    Assert.That(primary, Is.Null, "Grim Reaper prints no action at all.");
                    continue;
                }

                Assert.That(primary.Kind, Is.Not.EqualTo(ActionKind.Defense),
                    definition.DisplayName + " - Whisper prints no Defense primary.");
                Assert.That(primary.Kind, Is.Not.EqualTo(ActionKind.Movement),
                    definition.DisplayName + " - Whisper prints no Movement primary.");
                Assert.That(primary.DefenseResolution, Is.EqualTo(PrintedDefenseResolution.None),
                    definition.DisplayName + " - Whisper prints no p.14 Block.");

                var colour = definition.Progression.Color;
                if (colour == CardColor.Red || colour == CardColor.Gold)
                {
                    Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack), definition.DisplayName);
                }
                else
                {
                    Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill), definition.DisplayName);
                }
            }
        }

        /// <summary>
        /// p.9's supertype without exception: the two Gold cards and the one Silver card are basic and
        /// every other face is not. It is what makes Grim Reaper's reach exactly two dealt cards.
        /// </summary>
        [Test]
        public void OnlyTheTwoGoldFacesAndTheSilverOne_AreBasic()
        {
            var basics = WhisperCardContent.AllDefinitions
                .Where(definition => definition.Progression.Classification == CardClassification.Basic)
                .Select(definition => definition.Id)
                .ToArray();

            Assert.That(basics, Is.EquivalentTo(new[]
            {
                WhisperCardContent.SwiftJustice,
                WhisperCardContent.VileAccusation,
                WhisperCardContent.DeathSeeker
            }));

            Assert.That(
                basics.Intersect(WhisperCardContent.DealtDefinitions.Select(d => d.Id)).Count(),
                Is.EqualTo(2),
                "Only two of the three are dealt, because p.6 boxes the Handicap card - which is " +
                "exactly how many cards Grim Reaper's \"basic actions\" can reach in a normal game.");
        }

        // -----------------------------------------------------------------------------------------
        // Death Seeker
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "This turn: You are immune to enemy actions." composes with no new vocabulary at all: the
        /// every-activity arm the inventory's forecast predicted would be needed already existed.
        /// </summary>
        [Test]
        public void DeathSeeker_IsImmuneToEveryActivity_OfEnemyHeroesOnly()
        {
            var definition = WhisperActiveEffectContent.DeathSeekerDefinition;

            Assert.That(definition.AffectedScope, Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero),
                "The face says \"You\", so no disk is drawn and no ally is covered.");
            Assert.That(definition.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn));
            Assert.That(definition.Modifications, Has.Count.EqualTo(1),
                "One printed clause, one declaration.");

            var immunity = definition.Modifications.Single();
            Assert.That(immunity.Kind, Is.EqualTo(RuleModificationKind.Immunity));
            Assert.That(immunity.Activities, Is.EqualTo(new[] { RuleActivity.All }),
                "The face names no activity list, so it names them all - which RuleActivity.All has " +
                "said since the enum was written.");
            Assert.That(
                immunity.RestrictedActors.Relations,
                Is.EqualTo(new[] { RestrictedActorRelation.HeroesOpposingSource }),
                "\"enemy actions\" is scoped by who is acting. A minion battle, a lane push and the " +
                "round's own cleanup have no acting hero, so none of them is filtered by this.");
        }

        /// <summary>
        /// Every activity the build names is reached, which is what makes the single member above a
        /// statement rather than a shorthand - and it is asserted through the same reach test the
        /// rules pipeline uses rather than by reading the list back.
        /// </summary>
        [Test]
        public void DeathSeekersImmunity_ReachesEveryNamedActivity()
        {
            var immunity = WhisperActiveEffectContent.DeathSeekerDefinition.Modifications.Single();
            foreach (RuleActivity activity in Enum.GetValues(typeof(RuleActivity)))
            {
                if (activity == RuleActivity.None)
                {
                    continue;
                }

                Assert.That(immunity.Covers(activity), Is.True,
                    "An enemy hero's " + activity + " must not reach her this turn.");
            }
        }

        /// <summary>
        /// The gate the whole card hangs on, and the two bullets behind it. The gate is why Death
        /// Seeker is unofferable on the first turn of round 1, which the 2026-09-06 ruling accepts as
        /// product behaviour.
        /// </summary>
        [Test]
        public void DeathSeeker_GatesItsModalOnAnEnemyDiscard()
        {
            var root = SkillRoot(WhisperCardContent.DeathSeeker);
            var gate = NodesOf(root).Single(node => node.Kind == EffectNodeKind.Conditional);

            Assert.That(gate.Capability,
                Is.EqualTo(SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1));
            Assert.That(gate.ChildNodeIds, Has.Count.EqualTo(1),
                "An \"If\" with no else-branch: an unsatisfied condition is a step that does not " +
                "happen, and the card carries on.");

            var modal = NodesOf(root).Single(node => node.Kind == EffectNodeKind.Choose);
            Assert.That(modal.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "\"choose one\" with no other hero named is Whisper's own choice.");
            Assert.That(modal.MinimumSelections, Is.EqualTo(1));
            Assert.That(modal.MaximumSelections, Is.EqualTo(1),
                "The printed maximum is one; Grim Reaper is what raises it.");
            Assert.That(modal.ChildNodeIds, Has.Count.EqualTo(2));
        }

        // -----------------------------------------------------------------------------------------
        // Swift Justice, and Grim Reaper's consumption of the modal-Attack contract
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Her Gold basic is a modal Attack whose bullets are the targeting sentences, so its target is
        /// bound by its own card text and p.14's opening question does not ask it.
        /// </summary>
        [Test]
        public void SwiftJustice_BindsItsTargetInCardText_AndDeclaresTwoSequenceBullets()
        {
            var action = Primary(WhisperCardContent.Definition(WhisperCardContent.SwiftJustice));

            Assert.That(action.AttackTargetPolicy.HasValue, Is.True);
            Assert.That(action.AttackTargetPolicy.Value.TargetIsBoundByCardText, Is.True,
                "Two bullets are two targeting sentences, so there is no single sentence for p.14's " +
                "opening question to ask.");
            Assert.That(action.CardTextAt(CardTextTiming.BeforeTheAttack), Is.Not.Null);

            var bullets = NodesOf(action.CardTextAt(CardTextTiming.BeforeTheAttack))
                .Where(node => node.Capability.Equals(SyntheticSequenceCapabilities.AttackSequenceV1))
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2));
        }

        /// <summary>
        /// The 2026-09-06 rider, composed rather than argued: the face prints no "(on different
        /// targets)" clause, so both bullets may point at one unit.
        /// </summary>
        [Test]
        public void SwiftJustice_ConstrainsNeitherBulletToADifferentTarget()
        {
            var bullets = NodesOf(AttackRoot(WhisperCardContent.SwiftJustice, CardTextTiming.BeforeTheAttack))
                .Where(node => node.Capability.Equals(SyntheticSequenceCapabilities.AttackSequenceV1))
                .ToArray();

            foreach (var bullet in bullets)
            {
                var distinct = bullet.Inputs.Single(input =>
                    input.ParameterName == SyntheticSequenceCapabilities.DistinctTargetsParameter);
                Assert.That(distinct.Literal.BooleanValue, Is.False,
                    "Middlefinger of Death and Spirit Bear print the clause; this face does not.");
            }
        }

        /// <summary>
        /// The two bullets' printed sentences, which is the whole of what each leaf publishes: a hero
        /// at the printed Range, and a unit at p.19's innermost ring.
        /// </summary>
        [Test]
        public void SwiftJusticesBullets_PublishTheirOwnPrintedSentences()
        {
            var bullets = NodesOf(AttackRoot(WhisperCardContent.SwiftJustice, CardTextTiming.BeforeTheAttack))
                .Where(node => node.Capability.Equals(SyntheticSequenceCapabilities.AttackSequenceV1))
                .ToArray();

            Assert.That(
                bullets.Select(bullet => Vocabulary<AttackTargetableUnits>(
                    bullet, SyntheticSequenceCapabilities.TargetableUnitsParameter)),
                Is.EqualTo(new[]
                {
                    AttackTargetableUnits.HeroesOnly,
                    AttackTargetableUnits.HeroesAndMinions
                }));
            Assert.That(
                bullets.Select(bullet => Vocabulary<AttackTargetReach>(
                    bullet, SyntheticSequenceCapabilities.ReachParameter)),
                Is.EqualTo(new[]
                {
                    AttackTargetReach.PrintedRange,
                    AttackTargetReach.AdjacentOnly
                }));
        }

        /// <summary>
        /// The step-7 rider is one node and it is "if able", so a defender the page removed, one who
        /// declined to defend and one whose card prints no Movement value all leave it doing nothing.
        /// </summary>
        [Test]
        public void SwiftJustice_ComposesItsRiderAsAnIfAbleForcedMove()
        {
            var root = AttackRoot(WhisperCardContent.SwiftJustice, CardTextTiming.AfterTheAttack);
            var rider = NodesOf(root).Single(node => node.Kind == EffectNodeKind.Capability);

            Assert.That(rider.Capability,
                Is.EqualTo(SyntheticRosterCapabilities.DefendedCardForcedMoveV1));
            Assert.That(rider.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                "\"If able\" opens no decision of the actor's and skips silently.");
            Assert.That(rider.Chooser, Is.EqualTo(EffectChooserRole.None),
                "The decision the rider opens belongs to the defender and is named by the handler, " +
                "not by a role on the node - which is why the node names no chooser at all.");
        }

        /// <summary>
        /// Whisper is the first verified hero to print a p.14 timing keyword inside a modal bullet, so
        /// her Gold card is the first to carry two bodies at two moments on one Attack.
        /// </summary>
        [Test]
        public void SwiftJustice_IsTheOnlyFaceCarryingTwoCardTextMoments()
        {
            foreach (var definition in WhisperCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    var moments = new[]
                        {
                            CardTextTiming.BeforeTheAttack,
                            CardTextTiming.AfterTheAttack,
                            CardTextTiming.DefenseAction
                        }
                        .Count(moment => action.CardTextAt(moment) != null);

                    var expected = definition.Id.Equals(WhisperCardContent.SwiftJustice) &&
                        action.Kind == ActionKind.Attack ? 2 : moments;
                    Assert.That(moments, Is.LessThanOrEqualTo(expected), definition.DisplayName);
                }
            }

            var gold = Primary(WhisperCardContent.Definition(WhisperCardContent.SwiftJustice));
            Assert.That(gold.CardTextAt(CardTextTiming.BeforeTheAttack), Is.Not.Null);
            Assert.That(gold.CardTextAt(CardTextTiming.AfterTheAttack), Is.Not.Null);
        }

        // -----------------------------------------------------------------------------------------
        // Grim Reaper
        // -----------------------------------------------------------------------------------------

        [Test]
        public void GrimReaper_RaisesChoiceCardinalityToTwo_OnBasicActionsOnly()
        {
            Assert.That(WhisperEffectContent.GrimReaperModifications, Has.Count.EqualTo(1),
                "One printed sentence, one declaration - it adds no value, no permission and no " +
                "trigger.");

            var declared = WhisperEffectContent.GrimReaperModifications.Single();
            Assert.That(declared.Kind, Is.EqualTo(RuleModificationKind.ChoiceCardinality));
            Assert.That(declared.Amount, Is.EqualTo(2), "\"or both\" on a two-bullet modal is two.");
            Assert.That(declared.ValueApplicability,
                Is.EqualTo(RuleValueApplicability.ControllerBasicAction),
                "\"When performing basic actions\", which p.9 defines by the card: all actions on a " +
                "basic card are basic actions.");
        }

        /// <summary>
        /// The Ultimate raises a modal on her own basic card and leaves every other modal alone. All
        /// four arms are asked of the same query the resolver asks.
        /// </summary>
        [Test]
        public void GrimReaper_WidensOnlyAModalItsOwnHeroIsChoosingOnABasicCard()
        {
            var whisper = new HeroId("whisper");
            var enemy = new HeroId("enemy");
            var standing = Standing(whisper);

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, whisper, whisper, standing, CardClassification.Basic),
                Is.EqualTo(2),
                "Her own modal on a basic card of hers: the printed one becomes two.");

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, whisper, whisper, standing, CardClassification.NonBasic),
                Is.EqualTo(1),
                "Lesser Evil and Greater Good are Blue cards, so the Ultimate does not reach them " +
                "even before asking who is choosing.");

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, whisper, enemy, standing, CardClassification.Basic),
                Is.EqualTo(1),
                "A modal of hers that hands the decision to an opponent is not one she is choosing, " +
                "so \"when performing\" does not reach it.");

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, enemy, enemy, standing, CardClassification.Basic),
                Is.EqualTo(1),
                "An enemy's own basic modal is not widened by her Ultimate.");
        }

        /// <summary>
        /// It never invents a bullet and never lowers a ceiling - the two bounds that keep a
        /// declaration from rewriting a card.
        /// </summary>
        [Test]
        public void ChoiceCardinality_IsBoundedByThePrintedOptions_AndNeverLowers()
        {
            var whisper = new HeroId("whisper");
            var standing = Standing(whisper);

            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, whisper, whisper, standing, CardClassification.Basic),
                Is.EqualTo(2));
            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(3, 3, whisper, whisper, standing, CardClassification.Basic),
                Is.EqualTo(3),
                "A printed ceiling already above the declared one is left alone.");
            Assert.That(
                ChoiceCardinalityQuery.MaximumOptions(1, 2, whisper, whisper, RuleModificationSet.Empty, CardClassification.Basic),
                Is.EqualTo(1),
                "Before the Ultimate is unlocked the same definition offers one bullet.");
        }

        // -----------------------------------------------------------------------------------------
        // The Red line
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// All three step-1 movers compose the printed reading order - move, then an actor-origin
        /// adjacency selection, then the target binding - which is the seam milestone 8.19a built and
        /// milestone 8.35z25 corrected two of Tigerclaw's faces onto.
        /// </summary>
        [Test]
        public void TheThreeRedMovers_ComposeMoveThenTarget_AndBindTheirTargetInCardText()
        {
            foreach (var cardId in new[]
            {
                WhisperCardContent.CrimsonTrail,
                WhisperCardContent.SanguinePath,
                WhisperCardContent.BloodPilgrimage
            })
            {
                var action = Primary(WhisperCardContent.Definition(cardId));
                Assert.That(action.AttackTargetPolicy.HasValue, Is.True, cardId.Value);
                Assert.That(action.AttackTargetPolicy.Value.TargetIsBoundByCardText, Is.True,
                    cardId.Value + " prints its move BEFORE its targeting sentence, so the target is " +
                    "named from the space the move left her in.");

                var root = AttackRoot(cardId, CardTextTiming.BeforeTheAttack);
                var order = NodesOf(root)
                    .Single(node => node.Kind == EffectNodeKind.Sequence && node.Id.Equals(root.RootNodeId))
                    .ChildNodeIds
                    .Select(id => NodesOf(root).Single(node => node.Id.Equals(id)))
                    .ToArray();

                Assert.That(order.Last().Capability,
                    Is.EqualTo(SyntheticContinuationCapabilities.SetAttackTargetV1),
                    cardId.Value + " binds its target last, so nothing can move between the naming " +
                    "and p.14's step 3.");

                var selection = order[order.Length - 2];
                Assert.That(selection.Capability, Is.EqualTo(SyntheticRosterCapabilities.ChooseTargetV2),
                    cardId.Value + " selects from the actor's own adjacency after the move.");
            }
        }

        /// <summary>
        /// Blood Fury and Blood Frenzy print their targeting sentence FIRST, so they take p.14's
        /// ordinary question and narrow nothing - which is what separates them from the three above.
        /// </summary>
        [Test]
        public void TheTwoRedRepeaters_TakeThePagesOwnTargetQuestion()
        {
            foreach (var cardId in new[]
            {
                WhisperCardContent.BloodFury,
                WhisperCardContent.BloodFrenzy
            })
            {
                var action = Primary(WhisperCardContent.Definition(cardId));
                Assert.That(action.AttackTargetPolicy.Value.TargetIsBoundByCardText, Is.False,
                    cardId.Value + " prints \"Target a unit adjacent to you\" first and its rider " +
                    "second, so p.14's opening question asks it and the card text does not.");
                Assert.That(action.AttackTargetPolicy.Value.NarrowsTheDefault(AttackRangeKind.NonRanged),
                    Is.False,
                    cardId.Value + " narrows nothing: \"a unit\" is p.13's own word.");
                Assert.That(WhisperEffectContent.AttackTextTimingFor(cardId),
                    Is.EqualTo(CardTextTiming.AfterTheAttack),
                    "A card printing one moment has no second to name.");
            }
        }

        /// <summary>
        /// The counted budget: the board supplies the count and the card supplies the ceiling, and the
        /// clamp travels with the answer rather than being applied by whoever reads it.
        /// </summary>
        [Test]
        public void SanguinePathAndBloodPilgrimage_DeclareTheirPrintedMaximum()
        {
            foreach (var pair in new[]
            {
                (WhisperCardContent.SanguinePath, 2),
                (WhisperCardContent.BloodPilgrimage, 3)
            })
            {
                var root = AttackRoot(pair.Item1, CardTextTiming.BeforeTheAttack);
                var count = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1));

                Assert.That(
                    count.Inputs.Single(input =>
                            input.ParameterName == SyntheticRosterCapabilities.MaximumCountedParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo(pair.Item2),
                    pair.Item1.Value + " prints \"up to a maximum of " + pair.Item2 + " spaces\".");

                var move = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));
                Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    "\"up to\" is a budget, so moving nowhere is a legal outcome.");
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "distance").SourceNodeId,
                    Is.EqualTo(count.Id),
                    "The distance is bound from the count rather than written down.");
            }
        }

        /// <summary>
        /// Crimson Trail is the one of the three that prints a condition instead of a count, and the
        /// condition gates the move alone - a false condition leaves the targeting sentence untouched.
        /// </summary>
        [Test]
        public void CrimsonTrail_GatesOnlyItsStep_OnThePresenceOfASpawnPoint()
        {
            var root = AttackRoot(WhisperCardContent.CrimsonTrail, CardTextTiming.BeforeTheAttack);
            var gate = NodesOf(root).Single(node => node.Kind == EffectNodeKind.Conditional);

            Assert.That(gate.Capability,
                Is.EqualTo(SyntheticRosterCapabilities.EmptyMinionSpawnPointsInRadiusV1));
            Assert.That(gate.ChildNodeIds, Has.Count.EqualTo(1),
                "Only the move is inside the condition; the targeting sentence is its sibling.");

            var gated = NodesOf(root).Single(node => node.Id.Equals(gate.ChildNodeIds[0]));
            Assert.That(gated.Capability, Is.EqualTo(SyntheticDisplacementCapabilities.DirectMoveV3));
            Assert.That(gated.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo));
        }

        /// <summary>
        /// Both repeats are gated on the live enemy-discard condition. Blood Fury's plain "repeat
        /// once" is automatic (ruling W1); Blood Frenzy's "repeat UP TO five times" is Whisper's to stop
        /// after any instance (ruling W2, milestone 9.1 - it was forced before).
        /// </summary>
        [Test]
        public void BloodFuryRepeatsAutomatically_AndBloodFrenzyRepeatsUntilWhisperStops()
        {
            foreach (var (card, maximum, playerMayStop) in new[]
            {
                (WhisperCardContent.BloodFury, 1, false),
                (WhisperCardContent.BloodFrenzy, 5, true)
            })
            {
                var root = AttackRoot(card, CardTextTiming.AfterTheAttack);
                var gate = NodesOf(root).Single(node => node.Kind == EffectNodeKind.Conditional);
                Assert.That(gate.Capability,
                    Is.EqualTo(SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1));

                var repeat = NodesOf(root).Single(node => node.Kind == EffectNodeKind.RepeatAction);
                Assert.That(repeat.MaximumRepeats, Is.EqualTo(maximum), card.Value);
                Assert.That(repeat.FailurePolicy,
                    Is.EqualTo(playerMayStop ? EffectFailurePolicy.May : EffectFailurePolicy.IfAble),
                    card.Value + (playerMayStop
                        ? ": W2 - \"up to\" makes the count Whisper's."
                        : ": W1 - no \"may\", so it is performed wherever a legal target exists."));
                Assert.That(repeat.Chooser,
                    Is.EqualTo(playerMayStop ? EffectChooserRole.Actor : EffectChooserRole.None));
                Assert.That(repeat.RepeatTargetBinding, Is.EqualTo(RepeatTargetBinding.DifferentTarget));
            }
        }

        /// <summary>
        /// THE MEASURED SHORTFALL, asserted so it cannot be forgotten. The 2026-09-06 ruling reads
        /// "on different targets" as PAIRWISE distinct across the whole chain;
        /// <c>CardActionResolutionAuthority.owedAttackRepeatExclusion</c> is a single
        /// <c>AttackTarget</c>, reset at each instance, so it excludes only the instance immediately
        /// before. At Blood Fury's maximum of one the two readings coincide exactly and that card is
        /// correct; at Blood Frenzy's five they diverge.
        ///
        /// <para>This case pins what makes that a real divergence rather than a theoretical one:
        /// among <b>Attack</b> repeats, every distinct-target one shipped before her is at a maximum
        /// of one, so no shipped face has ever asked the question. Wasp's Center of Mass reaches two
        /// but binds <c>SameTarget</c>.</para>
        /// </summary>
        [Test]
        public void BloodFrenzy_IsTheFirstAttackRepeat_WhoseDistinctTargetMaximumIsAboveOne()
        {
            foreach (var repeat in ShippedSets().SelectMany(EveryRepeatNode))
            {
                if (repeat.Item1 == ActionKind.Attack && IsDistinctTarget(repeat.Item2))
                {
                    Assert.That(repeat.Item2.MaximumRepeats, Is.EqualTo(1),
                        "A shipped hero's distinct-target ATTACK repeat reaches more than one " +
                        "instance, which would mean the single-slot chain exclusion is already " +
                        "insufficient somewhere it was not measured.");
                }
            }

            var frenzy = WhisperCardContent.Definition(WhisperCardContent.BloodFrenzy);
            Assert.That(
                EveryRepeatNode(frenzy).Single().Item2.MaximumRepeats,
                Is.EqualTo(5),
                "Blood Frenzy is the first printed face where pairwise-distinct and " +
                "distinct-from-the-previous are different readings.");
        }

        /// <summary>
        /// THE OBSERVATION ABOVE, NOW RESOLVED - and this case is inverted rather than deleted,
        /// exactly as its own failure message instructed ("update the note above rather than deleting
        /// the case").
        ///
        /// <para><b>What was recorded.</b> Sabina's Troop Movement, Marching Orders and Path to
        /// Victory declared <c>DifferentTarget</c> on Skills, and the engine derived no exclusion for
        /// them at all: <c>CardActionResolutionAuthority.ExclusionFor</c> and its card-text twin
        /// <c>CardTextRepeatExclusionFor</c> both yield the completed target only for
        /// <see cref="ActionKind.Attack"/>, and that exclusion is the only route a
        /// non-<c>SameTarget</c> binding has into a repeated instance. The declaration was therefore
        /// inert.</para>
        ///
        /// <para><b>How it was resolved, and which way round.</b> The note above said to read the
        /// printed text first, and the printed text decided it. All three faces read "Move a friendly
        /// minion in radius 1 space, to a space in radius. May repeat once / up to two times." - no
        /// "different target" clause on any of them. So the CONTENT was wrong, not the engine: routing
        /// the exclusion to Skill repeats would have INTRODUCED a rules defect, barring a re-move of
        /// the same minion that no face bars. The declaration was removed instead, and this case now
        /// guards the resolved state.</para>
        ///
        /// <para><b>Why the guard is worth keeping.</b> A non-Attack repeat declaring a distinct-target
        /// binding is, on today's engine, a declaration that does nothing - it reads as a rule and is
        /// not one. If a later hero composes such a node, this fails and forces the question: either
        /// the face prints the clause and the engine owes it a route, or it does not and the
        /// declaration is another phantom.</para>
        /// </summary>
        [Test]
        public void NoSkillRepeat_DeclaresADistinctTargetBindingNothingCanEnforce()
        {
            var skillRepeats = ShippedSets()
                .SelectMany(EveryRepeatNode)
                .Where(repeat => repeat.Item1 != ActionKind.Attack && IsDistinctTarget(repeat.Item2))
                .ToArray();

            Assert.That(
                skillRepeats, Is.Empty,
                "A repeat of a non-Attack action declares a distinct-target binding, which no code " +
                "path can enforce - only Attacks derive an exclusion from it. Read the card's " +
                "printed face: if it prints no 'different target' clause the declaration is a " +
                "phantom and belongs off the content, which is how Sabina's three were resolved.");
        }

        private static IEnumerable<CardDefinition> ShippedSets() =>
            WaspCardContent.AllDefinitions
                .Concat(ArienCardContent.AllDefinitions)
                .Concat(BroganCardContent.AllDefinitions)
                .Concat(SabinaCardContent.AllDefinitions)
                .Concat(TigerclawCardContent.AllDefinitions)
                .Concat(XargathaCardContent.AllDefinitions);

        private static bool IsDistinctTarget(EffectNodeDefinition node) =>
            node.RepeatTargetBinding == RepeatTargetBinding.DifferentTarget ||
            node.RepeatTargetBinding == RepeatTargetBinding.DifferentEnemyHero;

        // -----------------------------------------------------------------------------------------
        // The Blue line
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The opponent-chosen modal, which is what WH-03 asked and the 2026-09-06 ruling answered:
        /// p.10's other-hero choice extends from choosing a card to choosing a bullet.
        /// </summary>
        [Test]
        public void LesserEvilAndGreaterGood_PutTheirModalToTheHeroWhisperNamed()
        {
            foreach (var cardId in new[]
            {
                WhisperCardContent.LesserEvil,
                WhisperCardContent.GreaterGood
            })
            {
                var root = SkillRoot(cardId);
                var victim = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticTargetingCapabilities.RangeHeroTargetV1));
                var modal = NodesOf(root).Single(node => node.Kind == EffectNodeKind.Choose);

                Assert.That(modal.Chooser, Is.EqualTo(EffectChooserRole.BoundHero),
                    cardId.Value + " - \"An enemy hero in range chooses one\".");

                var chooser = modal.Inputs.Single(input =>
                    input.ParameterName == EffectNodeDefinition.ChooserParameterName);
                Assert.That(chooser.SourceNodeId, Is.EqualTo(victim.Id),
                    "The chooser is the hero the selection above named, not a role: with three " +
                    "opponents on a side there is no single \"the\" opposing hero.");
                Assert.That(modal.MinimumSelections, Is.EqualTo(1));
                Assert.That(modal.MaximumSelections, Is.EqualTo(1));
            }
        }

        /// <summary>
        /// Greater Good's first bullet escalates where Lesser Evil's does not - "discards a card, or is
        /// defeated" against "discards a card, if able" - which is the same pair of readings Arien's
        /// three Currents already carry, composed the same way.
        /// </summary>
        [Test]
        public void GreaterGood_EscalatesToADefeat_WhereLesserEvilSkipsSilently()
        {
            var lesser = SkillRoot(WhisperCardContent.LesserEvil);
            var greater = SkillRoot(WhisperCardContent.GreaterGood);

            var lesserDiscard = NodesOf(lesser).Single(node =>
                node.Capability.Equals(SyntheticCardStateCapabilities.DiscardChosenCardV2));
            Assert.That(lesserDiscard.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                "\"if able\" - a hero holding nothing simply does not discard.");
            Assert.That(
                NodesOf(lesser).Any(node =>
                    node.Capability.Equals(SyntheticConsequenceCapabilities.DefeatHeroV1)),
                Is.False,
                "Lesser Evil defeats nobody.");

            var greaterDiscard = NodesOf(greater).Single(node =>
                node.Capability.Equals(SyntheticCardStateCapabilities.DiscardChosenCardV2));
            Assert.That(greaterDiscard.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                "One mandatory step with two ways to complete - ledger question 13's reading.");
            Assert.That(
                NodesOf(greater).Any(node =>
                    node.Capability.Equals(SyntheticConsequenceCapabilities.DefeatHeroV1)),
                Is.True);
        }

        /// <summary>
        /// Milestone 8.35z35 (Ruling 17) at the card the ruling was written about. These two
        /// faces are the canonical opponent-chosen modal, and their first bullets sit on OPPOSITE sides
        /// of the two-tier line - by their printed words, not by anything the resolver decides.
        ///
        /// <para>Lesser Evil's discard is <c>if able</c>, so against a hero holding nothing the bullet
        /// applies nothing and STOPS nothing: the feasibility walk reads it empty rather than blocked,
        /// and Ruling 17 offers an empty option. That is the escape hatch the decision exists to give -
        /// the card hands the choice to the enemy, and the enemy is entitled to the bullet that costs
        /// them least. Greater Good's discard is mandatory with an escalation behind it, so it is one
        /// step with two ways to complete and is never merely empty.</para>
        ///
        /// <para>The offer rule itself is proven over the synthetic vocabulary in
        /// <c>EffectOptionFeasibilityTests</c>, where nothing reads a card. What is pinned here is the
        /// declaration that puts THIS card on THIS side of it, so a later content edit that made the
        /// discard mandatory would fail here rather than silently deleting the escape hatch.</para>
        /// </summary>
        [Test]
        public void LesserEvilsFirstBullet_IsEmptyRatherThanBlocked_SoRuling17OffersIt()
        {
            var lesser = SkillRoot(WhisperCardContent.LesserEvil);
            var lesserModal = NodesOf(lesser).Single(node => node.Kind == EffectNodeKind.Choose);
            var lesserPrice = lesser.GetNode(lesserModal.ChildNodeIds[0]);

            Assert.That(lesserPrice.Kind, Is.EqualTo(EffectNodeKind.Sequence));
            Assert.That(
                lesserPrice.ChildNodeIds.Select(id => lesser.GetNode(id).FailurePolicy),
                Is.EqualTo(new[] { EffectFailurePolicy.IfAble }),
                "The bullet's every step is `if able`, so nothing in it can stop the resolution: the " +
                "branch is empty, never blocked, and an empty option is offered.");

            var greater = SkillRoot(WhisperCardContent.GreaterGood);
            var greaterModal = NodesOf(greater).Single(node => node.Kind == EffectNodeKind.Choose);
            var greaterPrice = greater.GetNode(greaterModal.ChildNodeIds[0]);

            Assert.That(
                greater.GetNode(greaterPrice.ChildNodeIds[0]).FailurePolicy,
                Is.EqualTo(EffectFailurePolicy.Mandatory),
                "Greater Good prints \"or is defeated\" instead, so its first step is mandatory and " +
                "this bullet is not the empty one.");
        }

        /// <summary>
        /// Milestone 9.2, ruling W4: "a minion" and "3 minions" name no side, but "defeat" never
        /// reaches a friendly unit - the rulebook's Defeat entry, "No exceptions." (The 2026-09-06
        /// reading admitted either team; the user confirmed the rulebook on 2026-09-28.)
        /// </summary>
        [Test]
        public void BothOpponentChosenFaces_AdmitOnlyEnemyMinions()
        {
            foreach (var cardId in new[]
            {
                WhisperCardContent.LesserEvil,
                WhisperCardContent.GreaterGood
            })
            {
                var root = SkillRoot(cardId);
                var selection = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseTargetV2) ||
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseTargetTrioV1));

                Assert.That(
                    Vocabulary<SourceTeamRelation>(
                        selection, SyntheticRosterCapabilities.RelationParameter),
                    Is.EqualTo(SourceTeamRelation.OpposingSource),
                    cardId.Value + " - W4: a Defeat never reaches a friendly minion.");
                Assert.That(
                    Vocabulary<RosterUnitKind>(
                        selection, SyntheticRosterCapabilities.UnitKindParameter),
                    Is.EqualTo(RosterUnitKind.Minion));
                Assert.That(
                    selection.Inputs.Single(input =>
                        input.ParameterName == SyntheticRosterCapabilities.DistanceParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo(1),
                    "\"adjacent to you\" is p.19's innermost ring.");
            }
        }

        /// <summary>
        /// "up to 3", with zero a legal answer and three Defeats each reading one slot - Sabina's
        /// Bullet Hell shape with one more slot.
        /// </summary>
        [Test]
        public void GreaterGood_ChoosesUpToThreeMinions_AndZeroIsAnAnswer()
        {
            var root = SkillRoot(WhisperCardContent.GreaterGood);
            var set = NodesOf(root).Single(node =>
                node.Capability.Equals(SyntheticRosterCapabilities.ChooseTargetTrioV1));

            Assert.That(
                set.Inputs.Single(input => input.ParameterName == "minimum").Literal.IntegerValue,
                Is.EqualTo(0),
                "\"up to\" floors at nothing, and the count is Whisper's even though the bullet was " +
                "not.");
            Assert.That(
                set.Inputs.Single(input => input.ParameterName == "maximum").Literal.IntegerValue,
                Is.EqualTo(3));

            var defeats = NodesOf(root)
                .Where(node => node.Capability.Equals(SyntheticConsequenceCapabilities.DefeatMinionV1))
                .ToArray();
            Assert.That(defeats, Has.Length.EqualTo(3));
            foreach (var defeat in defeats)
            {
                Assert.That(defeat.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                    "A slot the selection left unfilled is skipped by its own node.");
            }
        }

        /// <summary>
        /// Lesser Evil keeps its "you may" and Greater Good does not, which the inventory records as a
        /// deliberate difference between the two printings rather than an accident.
        /// </summary>
        [Test]
        public void LesserEvilKeepsItsElection_AndGreaterGoodDoesNot()
        {
            Assert.That(
                NodesOf(SkillRoot(WhisperCardContent.LesserEvil))
                    .Any(node => node.Kind == EffectNodeKind.Optional &&
                        node.FailurePolicy == EffectFailurePolicy.May),
                Is.True,
                "\"You MAY defeat a minion adjacent to you\" - having had the bullet chosen for her, " +
                "Whisper still elects whether to defeat.");

            Assert.That(
                NodesOf(SkillRoot(WhisperCardContent.GreaterGood))
                    .Any(node => node.Kind == EffectNodeKind.Optional &&
                        node.FailurePolicy == EffectFailurePolicy.May),
                Is.False,
                "\"You defeat up to 3 minions\" - the election is gone and the count carries it.");
        }

        /// <summary>
        /// The three displacement Skills narrow their targets to Spawn-point occupants, and they do it
        /// through the broad p.19 term rather than the Red and Green lines' narrow one.
        /// </summary>
        [Test]
        public void TheThreeBlueDisplacements_NarrowToSpawnPointOccupants()
        {
            foreach (var pair in new[]
            {
                (WhisperCardContent.SeedsOfFear, 1, false),
                (WhisperCardContent.SproutsOfPanic, 2, true),
                (WhisperCardContent.BloomingNightmare, 3, true)
            })
            {
                var root = SkillRoot(pair.Item1);
                var selection = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticTargetingCapabilities.RadiusUnitTargetV1));

                Assert.That(
                    selection.Inputs.Single(input => input.ParameterName ==
                            SyntheticTargetingCapabilities.OccupiesSpawnPointParameterName)
                        .Literal.BooleanValue,
                    Is.True,
                    pair.Item1.Value + " prints \"occupying a spawn point\".");

                var move = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "distance").Literal.IntegerValue,
                    Is.EqualTo(pair.Item2));
                Assert.That(
                    move.FailurePolicy,
                    Is.EqualTo(pair.Item3 ? EffectFailurePolicy.UpTo : EffectFailurePolicy.Mandatory),
                    "Seeds of Fear says \"1 space\" and its two upgrades say \"up to\" - the one " +
                    "difference in the family besides the disk.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // The Green line
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The three placements admit only the Battle Zone's empty minion Spawn points, and Creeping
        /// Shadow's Tier III printing buys the dilation by one ring.
        /// </summary>
        [Test]
        public void TheThreeGreenPlaces_AdmitOnlyBattleZoneMinionSpawnPoints()
        {
            foreach (var pair in new[]
            {
                (WhisperCardContent.ShadowStep,
                    PlaceSpawnPointExclusion.SpacesThatAreNotAnEmptyMinionSpawnPointInTheBattleZone),
                (WhisperCardContent.ShadowWalk,
                    PlaceSpawnPointExclusion.SpacesThatAreNotAnEmptyMinionSpawnPointInTheBattleZone),
                (WhisperCardContent.CreepingShadow,
                    PlaceSpawnPointExclusion.SpacesThatAreNeitherSuchAPointNorAdjacentToOne)
            })
            {
                var place = NodesOf(SkillRoot(pair.Item1)).Single(node =>
                    node.Capability.Equals(SyntheticPlacementCapabilities.PlaceInRangeV1));

                Assert.That(
                    Vocabulary<PlaceSpawnPointExclusion>(
                        place, SyntheticPlacementCapabilities.SpawnPointExclusionParameterName),
                    Is.EqualTo(pair.Item2),
                    pair.Item1.Value);
                Assert.That(place.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                    "No face prints \"may\": a hero with no legal destination stops the Skill there.");
            }
        }

        /// <summary>
        /// Shadow Step and Shadow Walk print byte-for-byte identical text, so they compose byte-for-
        /// byte identical bodies - the Tier II printing buys one point of Range, one of Initiative and
        /// an Attack item icon and nothing else. It is the same pair shape Wasp's Shock and Electrocute
        /// have.
        /// </summary>
        [Test]
        public void ShadowStepAndShadowWalk_ComposeTheIdenticalBody()
        {
            var step = SkillRoot(WhisperCardContent.ShadowStep);
            var walk = SkillRoot(WhisperCardContent.ShadowWalk);

            Assert.That(Shape(step), Is.EqualTo(Shape(walk)),
                "One printed sentence, one composition - the difference between the two faces is " +
                "entirely in their printed icons.");
            Assert.That(
                WhisperCardContent.Definition(WhisperCardContent.ShadowStep).SourceContent.PrintedText,
                Is.EqualTo(WhisperCardContent.Definition(WhisperCardContent.ShadowWalk)
                    .SourceContent.PrintedText));
        }

        /// <summary>
        /// The two swaps compose one selection over a union of two differently reached sets, because
        /// the face offers one choice and not a choice of which kind of choice to make.
        /// </summary>
        [Test]
        public void CruelTwistAndSealedFate_ComposeOneSelectionOverTheUnion_ThenAnUpToMove()
        {
            foreach (var pair in new[]
            {
                (WhisperCardContent.CruelTwist, 2),
                (WhisperCardContent.SealedFate, 3)
            })
            {
                var root = SkillRoot(pair.Item1);

                Assert.That(
                    NodesOf(root).Count(node => node.Kind == EffectNodeKind.Choose),
                    Is.EqualTo(0),
                    pair.Item1.Value + " asks no \"which kind of swap?\" question, because the card " +
                    "asks none.");

                var subject = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseSwapSubjectV1));
                Assert.That(
                    Vocabulary<RosterUnitKind>(
                        subject, SyntheticRosterCapabilities.AdjacentUnitKindParameter),
                    Is.EqualTo(RosterUnitKind.Unit),
                    "\"a unit adjacent to you\" names no side.");
                Assert.That(
                    Vocabulary<SourceTeamRelation>(
                        subject, SyntheticRosterCapabilities.RangedHeroRelationParameter),
                    Is.EqualTo(SourceTeamRelation.OpposingSource));
                Assert.That(
                    subject.Inputs.Single(input => input.ParameterName ==
                            SyntheticRosterCapabilities.RangedHeroRequiresDiscardParameter)
                        .Literal.BooleanValue,
                    Is.True,
                    "\"with a card in the discard\".");

                var move = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));
                Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    "\"You may then move up to N spaces\" - both whether and how far are hers.");
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "distance").Literal.IntegerValue,
                    Is.EqualTo(pair.Item2));

                var swap = NodesOf(root).Single(node =>
                    node.Capability.Equals(SyntheticExchangeCapabilities.SwapWithActorV1));
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "mover").SourceNodeId,
                    Is.EqualTo(swap.Id),
                    "\"then\" - the move starts from where the Swap put her.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Deferral and well-formedness
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The Handicap card composes nothing, by the same 2026-08-21 decision that parks one face of
        /// every verified hero: p.6 returns it to the box and the six-player pitch never brings it
        /// back. Its printed values and words ship all the same.
        /// </summary>
        [Test]
        public void VileAccusation_ShipsItsValuesAndComposesNothing()
        {
            var definition = WhisperCardContent.Definition(WhisperCardContent.VileAccusation);
            var action = Primary(definition);

            Assert.That(action.CardTextAt(CardTextTiming.BeforeTheAttack), Is.Null);
            Assert.That(action.CardTextAt(CardTextTiming.AfterTheAttack), Is.Null);
            Assert.That(action.AttackTargetPolicy.Value.TargetIsBoundByCardText, Is.False,
                "A deferred face composes no step-1 text for a binding to be about, so its target " +
                "binding ships with its composition - exactly as Nimble Strike's does.");
            Assert.That(definition.SourceContent.PrintedText, Does.Contain("Choose one"),
                "The words ship as inventory even though nothing is composed from them.");
        }

        /// <summary>
        /// THE DEFERRAL DEBT, asserted so the completion milestone fails this case if it registers
        /// without dealing her. Milestone 7.9's gate has two halves and this phase can satisfy neither:
        /// a registered capability must be reachable from a card a match DEALS, and a new handler type
        /// must additionally be reachable from a showcase card.
        ///
        /// <para><b>NARROWED at milestone 8.35z34, which is the first time this list was wrong.</b> It
        /// named <c>empty-minion-spawn-points-in-radius@1</c>, and that contract is not hers alone: it
        /// is the ONE declaration the content wave unified out of two branches minting the same id, and
        /// Dodger's three Blue Defense primaries and three Green rituals name it too. Her dealing
        /// milestone registers it because SHE is dealt, which is the gate working rather than failing -
        /// so an assertion that it is unregistered while Whisper is undealt is an assertion about
        /// Dodger, not about Whisper.
        ///
        /// <para>This is the same correction the wave made twice for exhaustive deferred-list
        /// assertions, arriving from a third direction: there the claim was about the other five
        /// heroes' entries in one shared LIST, and here it is about one shared CONTRACT. The rule that
        /// covers both: a hero's fixture may claim only what that hero alone owns. What remains below
        /// is exactly the set no other hero's content names.</para>
        /// </summary>
        /// <para><b>INVERTED at milestone 8.35z53, which is the dealing this case was written to
        /// wait for.</b> The assertion is not deleted, because the debt it tracked is exactly the
        /// thing the dealing has to discharge: every contract that was named here as unregistered is
        /// named here now as registered, so the case keeps testing the same six references and simply
        /// swaps which answer is correct. A dealing that flipped the roster lists but missed one of
        /// the six - the shape milestone 8.35z50 hit when a registration flip silently invalidated
        /// every reader of <c>DeferredHandlers()</c> - fails here by name rather than in a Play Mode
        /// journey.</para>
        [Test]
        public void HerNewContracts_AreRegisteredNowThatSheIsDealt()
        {
            // THE CATALOG HAD TO CHANGE WITH THE DIRECTION OF THE CLAIM, and getting it wrong is the
            // first thing this milestone's own run caught. The undealt form asked the argument-less
            // `SyntheticEffectCapabilities.CreateCatalog()`, which registers the synthetic vocabulary
            // and NOTHING a verified hero contributes - so it could answer "not registered" about
            // `whisper-death-seeker-immunity@1` for a reason that had nothing to do with Whisper being
            // undealt, and would have gone on answering it after she was dealt. The dealt form must ask
            // the catalog the BUILD composes, which is Ursafar's own dealing template
            // (`EverythingOfHisIsRegisteredNow_BecauseThisBuildDealsHim`).
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;

            foreach (var reference in new[]
            {
                SyntheticRosterCapabilities.ChooseTargetTrioV1,
                SyntheticRosterCapabilities.DefendedCardForcedMoveV1,
                SyntheticRosterCapabilities.EnemyHeroWithDiscardPresentV1,
                SyntheticRosterCapabilities.ChooseSwapSubjectV1,
                SyntheticTargetingCapabilities.RadiusUnitTargetV1,
                WhisperActiveEffectContent.DeathSeekerImmunityV1
            })
            {
                Assert.That(registered.IsRegistered(reference), Is.True,
                    reference + " is NOT registered though Whisper is dealt. Milestone 8.35z53 deals " +
                    "her, so every contract her printed set names must be reachable from a card a " +
                    "configured match puts in a hand - which is the half of milestone 7.9's gate this " +
                    "list exists to keep honest.");
            }
        }

        /// <summary>
        /// Every composed effect of hers is well formed against a catalog carrying the contracts she
        /// names. It is the validator a configured match runs at composition, asked here of one hero's
        /// whole set so a malformed node fails in a named test rather than at the table.
        ///
        /// <para>The catalog is built for this test rather than taken from the build, because the
        /// build's own catalog deliberately does not register her contracts yet - see the case above.
        /// That is the whole shape of a phase-one hero: composed, validated, and not yet dealt.</para>
        /// </summary>
        [Test]
        public void EveryComposedEffect_PassesTheDefinitionValidator()
        {
            var catalog = WhisperCatalog();
            var roots = new List<EffectDefinition>();
            foreach (var definition in WhisperCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    foreach (var moment in new[]
                    {
                        CardTextTiming.None,
                        CardTextTiming.BeforeTheAttack,
                        CardTextTiming.AfterTheAttack,
                        CardTextTiming.DefenseAction
                    })
                    {
                        var body = moment == CardTextTiming.None
                            ? action.EffectRoot
                            : action.CardTextAt(moment);
                        if (body != null && !roots.Contains(body))
                        {
                            roots.Add(body);
                        }
                    }
                }
            }

            Assert.That(roots, Is.Not.Empty, "Her set composes something.");
            foreach (var root in roots)
            {
                Assert.That(
                    EffectDefinitionValidator.Validate(root, catalog),
                    Is.Empty,
                    root.Id + " is not well formed against the catalog it names.");
            }
        }

        /// <summary>
        /// A modal that names a bound chooser must say which node named them. Without it a definition
        /// could claim a decision belongs to somebody it cannot identify, and the resolver would have
        /// nowhere to send the question.
        /// </summary>
        [Test]
        public void ABoundChooserModal_MustBindTheHeroItNames()
        {
            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("modal"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("a"), new EffectNodeId("b") },
                    chooser: EffectChooserRole.BoundHero,
                    minimumSelections: 1,
                    maximumSelections: 1),
                Throws.ArgumentException,
                "A bound-hero modal with no chooser binding is refused at content load.");
        }

        // -----------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------

        private static EffectCapabilityCatalog WhisperCatalog() =>
            SyntheticEffectCapabilities.CreateCatalog(
                SyntheticRosterCapabilities.DeferredHandlers()
                    .Concat(SyntheticTargetingCapabilities.DeferredHandlers())
                    .Concat(WhisperActiveEffectContent.Handlers()));

        /// <summary>
        /// Grim Reaper standing, as the rules pipeline would see it: p.16's permanent ability is a
        /// continuous source rather than an Active effect, which is what
        /// <see cref="AppliedRuleModification.FromContinuousSource"/> exists to say.
        /// </summary>
        private static RuleModificationSet Standing(HeroId hero) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromContinuousSource(
                        WhisperEffectContent.GrimReaperModifications.Single(),
                        RuleObject.ForHero(hero),
                        new ContinuousRuleSource(
                            WhisperEffectContent.GrimReaperRuleId,
                            ContinuousRuleSourceKind.Ultimate,
                            hero,
                            "WhisperGrimReaper",
                            "Always"),
                        TeamSide.Blue,
                        1)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        /// <summary>
        /// Every repeat node one face declares, paired with the kind of action carrying it - which is
        /// the half that decides whether the engine derives an exclusion for its binding at all.
        /// </summary>
        private static IEnumerable<Tuple<ActionKind, EffectNodeDefinition>> EveryRepeatNode(
            CardDefinition definition) =>
            definition.ActionDefinitions
                .SelectMany(action => new[]
                    {
                        action.EffectRoot,
                        action.CardTextAt(CardTextTiming.BeforeTheAttack),
                        action.CardTextAt(CardTextTiming.AfterTheAttack),
                        action.CardTextAt(CardTextTiming.DefenseAction)
                    }
                    .Where(root => root != null)
                    .Distinct()
                    .SelectMany(NodesOf)
                    .Where(node => node.Kind == EffectNodeKind.RepeatAction)
                    .Select(node => Tuple.Create(action.Kind, node)));

        private static EffectDefinition SkillRoot(CardDefinitionId cardId) =>
            Primary(WhisperCardContent.Definition(cardId)).EffectRoot;

        private static EffectDefinition AttackRoot(CardDefinitionId cardId, CardTextTiming moment) =>
            Primary(WhisperCardContent.Definition(cardId)).CardTextAt(moment);

        private static IReadOnlyList<EffectNodeDefinition> NodesOf(EffectDefinition definition) =>
            definition.Nodes;

        private static string Shape(EffectDefinition definition) =>
            string.Join(";", definition.Nodes.Select(node =>
                node.Id.Value + ":" + node.Kind + ":" + node.FailurePolicy + ":" + node.Capability));

        private static T Vocabulary<T>(EffectNodeDefinition node, string parameterName) =>
            (T)Enum.ToObject(
                typeof(T),
                node.Inputs
                    .Single(input => input.ParameterName == parameterName)
                    .Literal.IntegerValue);

        private static ActionDefinition Primary(CardDefinition definition) =>
            definition.ActionDefinitions.FirstOrDefault(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);

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
