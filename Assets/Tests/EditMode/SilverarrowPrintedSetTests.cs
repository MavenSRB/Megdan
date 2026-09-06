using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z30. Silverarrow's nineteen printed faces as definition content,
    /// checked against the transcription rather than against itself.
    ///
    /// <para>The tables here mirror the inventory's printed-value table column for column, so a
    /// reviewer can audit one against the other without holding anything in their head - which is the
    /// same discipline every hero's own file states about its own rows.</para>
    /// </summary>
    public sealed class SilverarrowPrintedSetTests
    {
        /// <summary>
        /// The printed values, transcribed a second time here from the same inventory the content file
        /// was written from. Two independent transcriptions of one table is what makes a typo in either
        /// a failure rather than a shared assumption.
        /// </summary>
        private static readonly PrintedFace[] Faces =
        {
            new PrintedFace("shoot-and-scoot", 12, 1, 1, CardColor.Gold, null, PrintedValueChannel.Range, 2, 1),
            new PrintedFace("keep-the-distance", 11, 1, 2, CardColor.Gold, null, PrintedValueChannel.Range, 2, 1),
            new PrintedFace("trailblazer", 13, 1, 0, CardColor.Silver, null, PrintedValueChannel.Radius, 4, 0),
            new PrintedFace("snap-shot", 9, 5, 5, CardColor.Red, CardTier.I, PrintedValueChannel.Range, 2, 3),
            new PrintedFace("opportunity-shot", 9, 5, 5, CardColor.Red, CardTier.II, PrintedValueChannel.Range, 3, 4),
            new PrintedFace("long-shot", 9, 4, 5, CardColor.Red, CardTier.II, PrintedValueChannel.Range, 3, 3),
            new PrintedFace("clear-shot", 10, 6, 5, CardColor.Red, CardTier.III, PrintedValueChannel.Range, 4, 5),
            new PrintedFace("rain-of-arrows", 10, 5, 5, CardColor.Red, CardTier.III, PrintedValueChannel.Range, 3, 3),
            new PrintedFace("grasping-roots", 10, 3, 3, CardColor.Blue, CardTier.I, PrintedValueChannel.Radius, 2, 0),
            new PrintedFace("entangling-vines", 10, 3, 3, CardColor.Blue, CardTier.II, PrintedValueChannel.Radius, 3, 0),
            new PrintedFace("warning-shot", 10, 3, 3, CardColor.Blue, CardTier.II, PrintedValueChannel.Radius, 3, 0),
            new PrintedFace("grappling-branches", 11, 4, 3, CardColor.Blue, CardTier.III, PrintedValueChannel.Radius, 4, 0),
            new PrintedFace("treetop-sentinel", 11, 4, 3, CardColor.Blue, CardTier.III, PrintedValueChannel.Radius, 3, 0),
            new PrintedFace("disorient", 3, 1, 2, CardColor.Green, CardTier.I, PrintedValueChannel.None, 0, 0),
            new PrintedFace("divert-attention", 2, 2, 2, CardColor.Green, CardTier.II, PrintedValueChannel.None, 0, 0),
            new PrintedFace("fae-healing", 2, 2, 2, CardColor.Green, CardTier.II, PrintedValueChannel.Radius, 4, 0),
            new PrintedFace("lead-astray", 2, 2, 2, CardColor.Green, CardTier.III, PrintedValueChannel.None, 0, 0),
            new PrintedFace("natures-blessing", 2, 2, 2, CardColor.Green, CardTier.III, PrintedValueChannel.Radius, 4, 0),
            new PrintedFace("wild-hunt", 0, 0, 0, CardColor.None, CardTier.IV, PrintedValueChannel.None, 0, 0)
        };

        [Test]
        public void EveryPrintedFace_IsDefinedWithItsTranscribedValues()
        {
            Assert.That(SilverarrowCardContent.AllDefinitions, Has.Count.EqualTo(19));

            foreach (var face in Faces)
            {
                var definition = SilverarrowCardContent.Definition(
                    new CardDefinitionId($"silverarrow-{face.Id}"));
                Assert.That(definition.ContentRevision,
                    Is.EqualTo(SilverarrowCardContent.ContentRevision), face.Id);
                Assert.That(definition.Progression.Color, Is.EqualTo(face.Color), face.Id);

                if (face.Tier.HasValue)
                {
                    Assert.That(definition.Progression.Tier, Is.EqualTo(face.Tier.Value), face.Id);
                }

                if (!definition.Progression.IsUltimate)
                {
                    Assert.That(definition.Initiative.Value, Is.EqualTo(face.Initiative), face.Id);
                }

                AssertSecondary(
                    definition, ActionKind.Defense, PrintedValueChannel.Defense,
                    face.SecondaryDefense, face.Id);
                AssertSecondary(
                    definition, ActionKind.Movement, PrintedValueChannel.Movement,
                    face.SecondaryMovement, face.Id);

                var primary = definition.ActionDefinitions.SingleOrDefault(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                if (face.Attack > 0)
                {
                    Assert.That(primary.Kind, Is.EqualTo(ActionKind.Attack), face.Id);
                    Assert.That(
                        primary.BaseValues.Single(value =>
                            value.Channel == PrintedValueChannel.Attack).Amount,
                        Is.EqualTo(face.Attack), face.Id);
                }

                if (face.IconChannel != PrintedValueChannel.None)
                {
                    var icons = definition.ActionDefinitions
                        .SelectMany(action => action.BaseValues)
                        .Where(value => value.Channel == face.IconChannel)
                        .ToArray();
                    Assert.That(icons, Is.Not.Empty, face.Id);
                    Assert.That(icons.All(value => value.Amount == face.IconValue), Is.True, face.Id);
                }
            }
        }

        /// <summary>
        /// p.9's supertype rule without exception: her two Gold cards and her one Silver card are basic
        /// and every other card is non-basic. Her only basic Attack is Shoot and Scoot's and her only
        /// basic Skill is Trailblazer's.
        /// </summary>
        [Test]
        public void TheSupertypes_FollowThePrintedColoursExactly()
        {
            var basics = SilverarrowCardContent.AllDefinitions
                .Where(definition => definition.Progression.IsBasic)
                .Select(definition => definition.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(basics, Is.EqualTo(new[]
            {
                "silverarrow-keep-the-distance",
                "silverarrow-shoot-and-scoot",
                "silverarrow-trailblazer"
            }));
        }

        /// <summary>
        /// p.5's deck shape: five in the starting hand, thirteen in the deck, one boxed Handicap card.
        /// Every card is in exactly one of the three, which is what makes the eighteen dealt.
        /// </summary>
        [Test]
        public void TheDeckShape_IsPageFivesWithoutException()
        {
            Assert.That(SilverarrowCardContent.StartingHand, Has.Count.EqualTo(5));
            Assert.That(SilverarrowCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(SilverarrowCardContent.ReturnedToBoxAtSetup, Has.Count.EqualTo(1));
            Assert.That(SilverarrowCardContent.DealtDefinitions, Has.Count.EqualTo(18));

            var everywhere = SilverarrowCardContent.StartingHand
                .Concat(SilverarrowCardContent.Deck)
                .Concat(SilverarrowCardContent.ReturnedToBoxAtSetup)
                .ToArray();
            Assert.That(everywhere.Distinct().Count(), Is.EqualTo(19));
            Assert.That(
                everywhere.Select(id => id.Value).OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(SilverarrowCardContent.AllDefinitions
                    .Select(definition => definition.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)));
        }

        /// <summary>
        /// The one face this milestone deliberately does not compose, and the deferral that covers it -
        /// the standing p.20 decision every hero's Handicap card takes.
        /// </summary>
        [Test]
        public void TheHandicapCard_IsTheOnlyDeferredComposition()
        {
            var deferred = SilverarrowCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.TextComposition.IsAwaited))
                .Select(definition => definition.Id.Value)
                .ToArray();

            Assert.That(deferred, Is.EqualTo(new[] { "silverarrow-keep-the-distance" }));
        }

        /// <summary>
        /// Every face that prints a step composes one, and every face that prints only a targeting
        /// sentence composes none - because a targeting sentence is the Attack's own declaration rather
        /// than an effect. The two lists together are the milestone's whole composition claim.
        /// </summary>
        [Test]
        public void EveryFaceThatPrintsAStep_ComposesOne()
        {
            var composed = SilverarrowCardContent.DealtDefinitions
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.EffectRoot != null))
                .Select(definition => definition.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(composed, Is.EqualTo(new[]
            {
                "silverarrow-disorient",
                "silverarrow-divert-attention",
                "silverarrow-entangling-vines",
                "silverarrow-fae-healing",
                "silverarrow-grappling-branches",
                "silverarrow-grasping-roots",
                "silverarrow-lead-astray",
                "silverarrow-natures-blessing",
                "silverarrow-rain-of-arrows",
                "silverarrow-shoot-and-scoot",
                "silverarrow-trailblazer",
                "silverarrow-treetop-sentinel",
                "silverarrow-warning-shot"
            }));

            var targetingOnly = new[]
            {
                SilverarrowCardContent.SnapShot,
                SilverarrowCardContent.OpportunityShot,
                SilverarrowCardContent.ClearShot,
                SilverarrowCardContent.LongShot
            };
            foreach (var id in targetingOnly)
            {
                var attack = SilverarrowCardContent.Definition(id).ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Attack);
                Assert.That(attack.EffectRoot, Is.Null, id.Value);
                Assert.That(attack.AttackTargetPolicy.HasValue, Is.True, id.Value);
                Assert.That(
                    attack.AttackTargetPolicy.Value.NarrowsTheDefault(AttackRangeKind.Ranged),
                    Is.True,
                    $"{id.Value} prints a sentence p.13's default does not say.");
            }
        }

        /// <summary>
        /// RULING 1 reached the four faces that print it and no others, and RULING 39's union reached
        /// the three modals. Asserted as a whole-set statement rather than card by card, because a
        /// reach applied to one face too many is exactly the defect this shape exists to prevent.
        /// </summary>
        [Test]
        public void Ruling1AndTheModal_ReachExactlyTheFacesThatPrintThem()
        {
            var band = FacesWhere(policy =>
                policy.Reach == AttackTargetReach.ExactlyPrintedRange);
            Assert.That(band, Is.EqualTo(new[]
            {
                "silverarrow-keep-the-distance",
                "silverarrow-long-shot",
                "silverarrow-rain-of-arrows",
                "silverarrow-shoot-and-scoot"
            }));

            var modal = FacesWhere(policy =>
                policy.TargetableUnits == AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou);
            Assert.That(modal, Is.EqualTo(new[]
            {
                "silverarrow-clear-shot",
                "silverarrow-opportunity-shot",
                "silverarrow-snap-shot"
            }));

            var heroesOnly = FacesWhere(policy =>
                policy.TargetableUnits == AttackTargetableUnits.HeroesOnly);
            Assert.That(heroesOnly, Is.EqualTo(new[] { "silverarrow-keep-the-distance" }),
                "Only the Handicap card narrows to heroes - the same hero-only narrowing that " +
                "separates Wasp's Crackling Dagger from her Magnetic Dagger.");
        }

        /// <summary>
        /// Three structural absences the inventory records, asserted so they stay absences: she prints
        /// no Block and no Defense primary anywhere, and every Attack she prints is Ranged.
        /// </summary>
        [Test]
        public void HerSetPrintsNoBlock_NoDefensePrimary_AndNoMeleeAttack()
        {
            foreach (var definition in SilverarrowCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    Assert.That(action.BlockCondition, Is.EqualTo(PrintedBlockCondition.None),
                        definition.Id.Value);
                    Assert.That(
                        action.Kind == ActionKind.Defense &&
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary,
                        Is.False,
                        definition.Id.Value);
                    if (action.Kind == ActionKind.Attack)
                    {
                        Assert.That(action.AttackRange, Is.EqualTo(AttackRangeKind.Ranged),
                            definition.Id.Value);
                    }
                }
            }
        }

        /// <summary>
        /// The first hero for whom ledger question 12's item-reaches-the-Ultimate reading has nothing
        /// to reach. Asserted explicitly rather than left as an accident of the row, exactly as the
        /// inventory asks.
        /// </summary>
        [Test]
        public void TheUltimate_PrintsNeitherRangeNorRadius_SoNoItemCanModifyIt()
        {
            var wildHunt = SilverarrowCardContent.Definition(SilverarrowCardContent.WildHunt);

            Assert.That(wildHunt.Progression.IsUltimate, Is.True);
            Assert.That(wildHunt.ActionDefinitions, Is.Empty);
            Assert.That(wildHunt.UltimateRule, Is.Not.Null);
            Assert.That(
                wildHunt.UltimateRule.PrintedValues.Any(value =>
                    value.Channel == PrintedValueChannel.Range ||
                    value.Channel == PrintedValueChannel.Radius),
                Is.False);
            Assert.That(
                wildHunt.UltimateRule.BeforeAction.Frequency,
                Is.EqualTo(BeforeActionTriggerFrequency.EveryAction));
        }

        /// <summary>
        /// The three Blue restriction cards declare Arien's two modifications and nothing else - the
        /// reuse that made the forecast's highest-risk item cost nothing. Measured against Arien's own
        /// Deluge rather than against a copy of the expectation.
        /// </summary>
        [Test]
        public void TheBlueTrio_DeclaresExactlyArienSownDelugeModifications()
        {
            var deluge = ArienActiveEffectContent.DelugeDefinition;
            var trio = new[]
            {
                SilverarrowActiveEffectContent.GraspingRootsDefinition,
                SilverarrowActiveEffectContent.EntanglingVinesDefinition,
                SilverarrowActiveEffectContent.GrapplingBranchesDefinition
            };

            foreach (var definition in trio)
            {
                Assert.That(definition.AffectedScope, Is.EqualTo(deluge.AffectedScope));
                Assert.That(definition.DurationKind, Is.EqualTo(deluge.DurationKind));
                Assert.That(definition.AffectedUnitRelation, Is.EqualTo(deluge.AffectedUnitRelation));
                Assert.That(
                    definition.Modifications.Select(value => value.Kind),
                    Is.EqualTo(deluge.Modifications.Select(value => value.Kind)));
                Assert.That(
                    definition.Modifications.Select(value => string.Join(
                        "+", value.Activities.Select(activity => activity.ToString()))),
                    Is.EqualTo(deluge.Modifications.Select(value => string.Join(
                        "+", value.Activities.Select(activity => activity.ToString())))));
            }

            Assert.That(
                trio.Select(definition => definition.Radius.Value),
                Is.EqualTo(new[] { 2, 3, 4 }),
                "The three faces differ from each other in nothing but the disk.");
        }

        /// <summary>
        /// Trailblazer's permission carries the scope its own sentence prints, and Xargatha's does not
        /// carry one - which is the whole of RULING 47 as content.
        /// </summary>
        [Test]
        public void Trailblazer_ScopesItsPermissionToMovementActions()
        {
            var permission = SilverarrowActiveEffectContent.TrailblazerDefinition.Modifications
                .Single(value => value.Kind == RuleModificationKind.Permission);
            Assert.That(permission.Permission, Is.EqualTo(RulePermissionKind.MoveThroughObstacles));
            Assert.That(permission.Activities, Is.EqualTo(new[] { RuleActivity.Movement }));

            var unscoped = XargathaEffectContent.MetamorphosisModifications
                .Single(value => value.Kind == RuleModificationKind.Permission);
            Assert.That(
                unscoped.Activities.All(activity => activity == RuleActivity.None), Is.True,
                "Metamorphosis prints \"You may move through obstacles\" and qualifies nothing.");

            Assert.That(
                SilverarrowActiveEffectContent.TrailblazerDefinition.AffectedScope,
                Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerAndHeroesInRadius));
            Assert.That(
                SilverarrowActiveEffectContent.TrailblazerDefinition.AffectedUnitRelation,
                Is.EqualTo(SourceTeamRelation.AlliedWithSource));
        }

        /// <summary>
        /// RULING 8 as content: the two healing faces admit heroes of any side and admit her. Both
        /// halves are asserted, because the user answered them in two steps and either could be lost
        /// separately.
        /// </summary>
        [Test]
        public void Ruling8_TheHealingFaces_AdmitAnyHeroIncludingHerself()
        {
            foreach (var id in new[]
            {
                SilverarrowCardContent.FaeHealing,
                SilverarrowCardContent.NaturesBlessing
            })
            {
                var select = SkillNodes(id).Single(node =>
                    node.Capability.Equals(SyntheticTargetingCapabilities.RadiusHeroTargetV1));

                Assert.That(
                    LiteralOf(select, SyntheticTargetingCapabilities.IncludeSelfParameterName)
                        .BooleanValue,
                    Is.True, id.Value);
                Assert.That(
                    (SourceTeamRelation)LiteralOf(
                        select, SyntheticTargetingCapabilities.TargetRelationParameterName)
                        .IntegerValue,
                    Is.EqualTo(SourceTeamRelation.AnyTeam), id.Value);
            }
        }

        /// <summary>
        /// RULING 37 as content: the second Move's bound is bound from the first Move's realised
        /// distance rather than from the printed maximum. It is asserted on the binding itself, because
        /// a composition that quietly used a literal here would look identical from every other angle
        /// and would be reading 2 of SL-03 - the reading the user rejected.
        /// </summary>
        [Test]
        public void Ruling37_TheSelfMovesBound_IsTheFirstMovesRealisedDistance()
        {
            foreach (var id in new[]
            {
                SilverarrowCardContent.DivertAttention,
                SilverarrowCardContent.LeadAstray
            })
            {
                var nodes = SkillNodes(id);
                var shove = nodes.Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3) &&
                    node.Outputs.Any(output => output.OutputName ==
                        SyntheticDisplacementCapabilities.RealisedDistanceOutputName));
                var line = nodes.Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1));

                var bound = line.Inputs.Single(input => input.ParameterName == "maximumDistance");
                Assert.That(bound.SourceKind, Is.EqualTo(EffectBindingSourceKind.NodeOutput), id.Value);
                Assert.That(bound.SourceNodeId, Is.EqualTo(shove.Id), id.Value);
                Assert.That(
                    bound.SourceOutputName,
                    Is.EqualTo(SyntheticDisplacementCapabilities.RealisedDistanceOutputName),
                    id.Value);
            }
        }

        /// <summary>
        /// Disorient's two printed differences from its siblings, asserted because they are the kind of
        /// thing a composer smooths over: its first Move is exact and mandatory where theirs is "up to",
        /// and its self-move is optional and walks no line.
        /// </summary>
        [Test]
        public void Disorient_IsNotASmallerDivertAttention()
        {
            var nodes = SkillNodes(SilverarrowCardContent.Disorient);

            Assert.That(
                nodes.Any(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1)),
                Is.False,
                "Disorient prints no straight line.");

            var moves = nodes
                .Where(node => node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3))
                .ToArray();
            Assert.That(moves, Has.Length.EqualTo(2));
            Assert.That(
                moves.Select(node => node.FailurePolicy),
                Is.EqualTo(new[] { EffectFailurePolicy.Mandatory, EffectFailurePolicy.May }),
                "The enemy's move is mandatory at an exact distance; her own is optional at one.");
        }

        /// <summary>
        /// The Rain of Arrows shortfall, asserted rather than left to a note: the hero repeat is
        /// composed and the minion repeat is not. A test that says so is what stops the gap being
        /// mistaken for a completed composition by whoever picks the card up next.
        /// </summary>
        [Test]
        public void RainOfArrows_ComposesItsHeroRepeatAndNotYetItsMinionRepeat()
        {
            var attack = SilverarrowCardContent
                .Definition(SilverarrowCardContent.RainOfArrows)
                .ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            var repeats = attack.EffectRoot.Nodes
                .Where(node => node.Kind == EffectNodeKind.RepeatAction)
                .ToArray();

            Assert.That(repeats, Has.Length.EqualTo(1));
            Assert.That(
                repeats[0].RepeatTargetBinding,
                Is.EqualTo(RepeatTargetBinding.DifferentEnemyHero));
            Assert.That(
                repeats[0].FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                "RULING 38: no printed \"may\", so nobody is asked - it happens where it can and is " +
                "skipped where it cannot.");
            Assert.That(repeats[0].Chooser, Is.EqualTo(EffectChooserRole.None),
                "Which is the observable difference from Wasp's Thunder Boomerang, whose face does " +
                "print a may.");

            Assert.That(
                attack.EffectRoot.Nodes.Any(node =>
                    node.Kind == EffectNodeKind.RepeatAction &&
                    node.RepeatTargetBinding != RepeatTargetBinding.DifferentEnemyHero),
                Is.False,
                "The minion clause is a named engine item, not a silent half-composition.");
        }

        /// <summary>
        /// Every capability her content names is one this build can load, and the two she mints are
        /// deliberately not registered until she is dealt - which is the law milestone 8.35z29
        /// recorded, because the registered list is serialized and eager registration moves pinned
        /// snapshot bytes.
        /// </summary>
        [Test]
        public void HerMintedCapabilities_AreDeferredUntilSheIsDealt()
        {
            var registered = SyntheticRosterCapabilities.RegisteredHandlers()
                .Select(handler => handler.Reference)
                .ToArray();
            var deferred = SyntheticRosterCapabilities.DeferredHandlers()
                .Select(handler => handler.Reference)
                .ToArray();

            // Stated as "hers are among the deferred" rather than as the whole deferred list: the
            // six-hero content wave puts five other undealt heroes' contracts into this same list, so
            // an exhaustive assertion here would be an assertion about them. The claim this milestone
            // owns is that BOTH of hers are deferred and NEITHER is registered, and the intersection
            // check below is what makes the second half exact.
            Assert.That(deferred, Is.SupersetOf(new[]
            {
                SyntheticRosterCapabilities.ChooseLineDestinationV1,
                SyntheticRosterCapabilities.FastTravelV1
            }));
            Assert.That(registered.Intersect(deferred), Is.Empty);
        }

        private static IReadOnlyList<string> FacesWhere(Func<PrintedAttackTargetPolicy, bool> predicate) =>
            SilverarrowCardContent.AllDefinitions
                .Where(definition => definition.ActionDefinitions.Any(action =>
                    action.AttackTargetPolicy.HasValue &&
                    predicate(action.AttackTargetPolicy.Value)))
                .Select(definition => definition.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static IReadOnlyList<EffectNodeDefinition> SkillNodes(CardDefinitionId cardId) =>
            SilverarrowCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Kind == ActionKind.Skill)
                .EffectRoot.Nodes;

        private static EffectValue LiteralOf(EffectNodeDefinition node, string parameterName) =>
            node.Inputs.Single(input => input.ParameterName == parameterName).Literal;

        private static void AssertSecondary(
            CardDefinition definition,
            ActionKind kind,
            PrintedValueChannel channel,
            int printed,
            string cardId)
        {
            var action = definition.ActionDefinitions.SingleOrDefault(candidate =>
                candidate.Kind == kind &&
                candidate.Origin != ActionDefinitionOrigin.PrintedPrimary);
            if (printed == 0)
            {
                Assert.That(action, Is.Null,
                    $"{cardId} prints no secondary {kind}, which is expressed by the action not " +
                    "existing rather than by a zero.");
                return;
            }

            Assert.That(action, Is.Not.Null, cardId);
            Assert.That(
                action.BaseValues.Single(value => value.Channel == channel).Amount,
                Is.EqualTo(printed), cardId);
        }

        private sealed class PrintedFace
        {
            public PrintedFace(
                string id,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                CardColor color,
                CardTier? tier,
                PrintedValueChannel iconChannel,
                int iconValue,
                int attack)
            {
                Id = id;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Color = color;
                Tier = tier;
                IconChannel = iconChannel;
                IconValue = iconValue;
                Attack = attack;
            }

            public string Id { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public CardColor Color { get; }
            public CardTier? Tier { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public int Attack { get; }
        }
    }
}
