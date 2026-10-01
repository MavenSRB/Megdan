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
                Assert.That(
                    line.Inputs.Any(input => input.ParameterName == "minimumDistance" &&
                        input.Literal.IntegerValue == 0),
                    Is.True,
                    id.Value + ": milestone 9.35 (W5) - \"up to that number\" makes 0 a real choice, so " +
                    "the floor is bound to nought and her own space is offered beside the line.");
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
        /// Rain of Arrows' whole sentence, composed. Milestone 8.35z30 shipped the hero clause and
        /// parked the minion clause on a measured seam limit, and asserted the shortfall here so it
        /// could not be mistaken for a finished composition; milestone 8.35z46 closed it,
        /// and this case is the same assertion turned round - what was proved absent is now proved
        /// present, in the printed order that decides which instance may ask which.
        ///
        /// <para>Both clauses are `if able` and name no chooser, which is RULING 38's "no printed may"
        /// and the observable difference from Wasp's Thunder Boomerang. The chain itself - three
        /// instances, each narrowed to its own clause - is driven on a board in
        /// <c>SilverarrowRepeatChainTests</c>; this is the composition it is driven from.</para>
        /// </summary>
        [Test]
        public void RainOfArrows_ComposesBothOfItsPrintedRepeatClauses()
        {
            var attack = SilverarrowCardContent
                .Definition(SilverarrowCardContent.RainOfArrows)
                .ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            var repeats = attack.EffectRoot.Nodes
                .Where(node => node.Kind == EffectNodeKind.RepeatAction)
                .ToArray();

            Assert.That(
                repeats.Select(node => node.RepeatTargetBinding).ToArray(),
                Is.EqualTo(new[]
                {
                    RepeatTargetBinding.DifferentEnemyHero,
                    RepeatTargetBinding.DifferentEnemyMinion
                }),
                "\"repeat once on a different hero; if you do, repeat once on a minion\", in the " +
                "order the card prints them - which is the order the chain advances along.");

            foreach (var repeat in repeats)
            {
                Assert.That(
                    repeat.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                    "RULING 38: no printed \"may\", so nobody is asked - it happens where it can and " +
                    "is skipped where it cannot.");
                Assert.That(repeat.Chooser, Is.EqualTo(EffectChooserRole.None),
                    "Which is the observable difference from Wasp's Thunder Boomerang, whose face " +
                    "does print a may.");
            }

            Assert.That(
                attack.EffectRoot.Nodes.Any(node =>
                    node.Kind == EffectNodeKind.Conditional &&
                    node.Capability.Equals(SyntheticConsequenceCapabilities.AttackTookAHeroV1)),
                Is.True,
                "\"If you target a hero\" gates the whole chain and is the only predicate it needs: " +
                "\"if you do\" is the chain's own shape, because the minion clause is printed second " +
                "and a chain reaches a second clause only from an instance the first one produced.");
        }

        /// <summary>
        /// Milestone 8.35z46 deals her, which is the change that moves her one remaining minted
        /// contract from <c>DeferredHandlers</c> to <c>RegisteredHandlers</c>: milestone 7.9's gate
        /// refuses a registration no printed card a match deals can start, and a configured match now
        /// deals her.
        ///
        /// <para>This case replaces <c>HerMintedCapabilities_AreDeferredUntilSheIsDealt</c>, on exactly
        /// the correction <c>MisaPrintedSetTests.TheNewContracts_AreRegisteredNowSheIsDealt</c>
        /// records for the same reason: the deferral gate asks whether a contract is reachable from a
        /// card a match DEALS, and the day the hero is dealt the old assertion is backwards rather than
        /// merely stale.</para>
        ///
        /// <para><b>The gate's second half did real work once.</b> <c>fast-travel@1</c> is a handler
        /// TYPE this build had never registered, so it owed a <c>SyntheticShowcaseHand</c> consumer
        /// before it could register - milestone 8.35z46 added one. Her other new-to-her contract,
        /// <c>choose-line-destination@1</c>, registered already at Misa's dealing and owes nothing
        /// here: her three straight-line faces reach the same registered contract Misa's five Blue
        /// Skills do, which is the ordinary case of two heroes sharing one rule.</para>
        /// </summary>
        [Test]
        public void TheNewContract_IsRegisteredNowSheIsDealt()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                .Select(value => value.ToString())
                .ToArray();

            Assert.That(registered, Has.Member("fast-travel@1"));
            Assert.That(registered, Has.Member("choose-line-destination@1"));

            foreach (var handler in SilverarrowActiveEffectContent.Handlers())
            {
                Assert.That(registered, Has.Member(handler.Reference.ToString()),
                    "Her Active-effect creations register with the rest of her contracts, in the " +
                    "same change, because they are reached by the same dealt cards.");
            }

            // And the other side of the move: her own contract is no longer declared as deferred.
            // `choose-line-destination@1` was never hers to claim here in the first place - it left
            // this list at Misa's dealing, and MisaPrintedSetTests asserts that side.
            var deferred = SyntheticRosterCapabilities.DeferredHandlers()
                .Select(handler => handler.Reference.ToString())
                .ToArray();

            Assert.That(deferred, Has.No.Member("fast-travel@1"));
        }

        /// <summary>
        /// A configured match deals her eighteen cards, which is the half of milestone 7.9's gate the
        /// registration above rests on. Asserted through the configuration rather than through the
        /// content, because "she is dealt" is a statement about a match and not about a card file.
        /// </summary>
        [Test]
        public void SheIsDealtByAConfiguredMatch()
        {
            var configuration = MatchConfiguration.CreateStagedSilverarrowForInternalTest(TeamSide.Red);

            var hers = configuration.CardInstances
                .Where(card => card.DefinitionReference.DefinitionId.Value
                    .StartsWith(SilverarrowCardContent.ContentSetId + "-", StringComparison.Ordinal))
                .ToArray();

            Assert.That(hers, Has.Length.EqualTo(SilverarrowCardContent.DealtDefinitions.Count));
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
