using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z29. Misa's composed faces, measured against the catalog that will run them
    /// and against the five user rulings that decided their shape.
    ///
    /// <para>What each card <em>prints</em> is <see cref="MisaVerifiedContentTests"/>' and is not
    /// repeated here. This file asks what that file cannot: whether every composed body is legal
    /// against a catalog holding every contract it names, and whether each ruling actually reached the
    /// composition rather than only the document that records it.</para>
    ///
    /// <para><b>Milestone 8.35z38 deals her, and the sentence that stood here has been replaced rather
    /// than deleted.</b> It read "she is not dealt in this phase, so there is no staged match to
    /// drive". There is one now - <c>MatchConfiguration.CreateStagedMisaForInternalTest</c> - and the
    /// two cases at the end of this file are what that milestone owed this file: that her contracts
    /// are registered, and that a configured match deals the cards which is what earns the
    /// registration. The capability behaviours her rulings turn on are still measured directly, in
    /// <see cref="MisaCapabilityTests"/>; this file still measures the definitions.</para>
    /// </summary>
    public sealed class MisaPrintedSetTests
    {
        /// <summary>
        /// The catalog her composed faces are legal against: everything the build registers, plus
        /// whatever is still declared as deferred, plus her own Active-effect creations.
        ///
        /// <para>It is composed by naming the declared LISTS rather than by naming handlers here, and
        /// milestone 8.35z38 is the proof that this was worth doing: dealing her moved eight of her
        /// contracts out of those lists and into the registered set, and this expression did not need
        /// one character changed. What it reads today is the four contracts other undealt heroes still
        /// defer plus her six creations, and it composed her eight registered ones yesterday - the same
        /// sentence either way, because the sentence is "everything a definition of hers may
        /// name".</para>
        /// </summary>
        private static EffectCapabilityCatalog Catalog() =>
            SyntheticEffectCapabilities.CreateCatalog(
                SyntheticPlacementCapabilities.DeferredHandlers()
                    .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                    .Concat(MisaActiveEffectContent.Handlers()));

        /// <summary>
        /// Every composed body passes the same validation a configured match applies at composition -
        /// the check that would otherwise fault mid-resolution, on a player's turn, with the card
        /// already spent.
        ///
        /// <para>It is run at the moment each body declares, because milestone 8.10 made the moment
        /// part of the question. All five of Misa's Attacks declare p.14's step 7, and none of her
        /// Skills declares a moment at all.</para>
        /// </summary>
        [Test]
        public void EveryComposedBody_IsLegalAgainstTheCatalogThatRunsIt()
        {
            var catalog = Catalog();
            var checkedBodies = 0;

            foreach (var definition in MisaCardContent.AllDefinitions)
            {
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.EffectRoot == null)
                    {
                        continue;
                    }

                    checkedBodies++;
                    Assert.That(
                        EffectDefinitionValidator.ValidateCardText(
                            action.EffectRoot, catalog, action.CardTextTiming),
                        Is.Empty,
                        definition.Id.Value + " composed a body the validator refuses.");
                }
            }

            foreach (var triggered in MisaEffectContent.TriggeredDefinitions(
                MisaCardContent.ContentRevision))
            {
                checkedBodies++;
                Assert.That(
                    EffectDefinitionValidator.Validate(triggered, catalog),
                    Is.Empty,
                    triggered.Id.Value + " is a triggered body the validator refuses.");
            }

            // Seventeen card bodies - eighteen faces with a primary, less the parked Handicap - plus
            // the four triggered definitions. Asserted so a composition silently dropped from the
            // switch cannot leave this case passing over nothing.
            Assert.That(checkedBodies, Is.EqualTo(21));
        }

        /// <summary>
        /// The eighteen faces whose text composes, and the one that does not. A face whose printed text
        /// is neither composed nor parked would be a card the engine silently does nothing for, which
        /// is what <see cref="ActionDefinition"/> refuses and what this asserts from the outside.
        /// </summary>
        [Test]
        public void EveryFaceWithText_EitherComposesOrNamesItsDeferral()
        {
            foreach (var definition in MisaCardContent.AllDefinitions)
            {
                if (definition.Progression.IsUltimate)
                {
                    Assert.That(definition.UltimateRule, Is.Not.Null);
                    continue;
                }

                var primary = definition.ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                var composed = primary.EffectRoot != null;
                var parked = primary.TextComposition.IsAwaited;

                Assert.That(composed || parked, Is.True,
                    definition.Id.Value + " prints text that is neither composed nor parked.");
                Assert.That(composed && parked, Is.False,
                    definition.Id.Value + " cannot both compose and owe its text.");
            }
        }

        /// <summary>
        /// RULING 1, at the two places it reaches a composition: "at maximum range" is a ring-shaped
        /// Place, and "at maximum radius" is a ring-shaped pair selection. Both are separate contracts
        /// from the "in range" and "in radius" ones the roster already had, which is what makes the
        /// difference auditable from a definition alone.
        /// </summary>
        [Test]
        public void Ruling1_PutsTheExactDistanceContractsOnTheFourFacesThatPrintTheWords()
        {
            Assert.That(
                CapabilitiesOf(MisaCardContent.WatchHowISoar),
                Does.Contain(SyntheticPlacementCapabilities.PlaceAtMaximumRangeV1),
                "Watch How I Soar's first bullet places at exactly the printed Range.");
            Assert.That(
                CapabilitiesOf(MisaCardContent.WatchHowISoar),
                // Has.No.Member and not Does.Not.Contain: over a collection of a value type, the
                // latter binds to NUnit's STRING overload under Unity's compiler and fails the Edit
                // Mode build while the single-assembly dotnet harness compiles it happily. Recorded
                // at milestone 8.35z31 and hit again here - the harness is structurally blind to it.
                Has.No.Member(SyntheticPlacementCapabilities.PlaceInRangeV1),
                "The disk-shaped Place is the reading RULING 1 rejected.");

            Assert.That(
                CapabilitiesOf(MisaCardContent.LivingTornado),
                Does.Contain(SyntheticRosterCapabilities.ChooseUnitPairAtMaximumRadiusV1));
            Assert.That(
                CapabilitiesOf(MisaCardContent.StormSpirit),
                Does.Contain(SyntheticRosterCapabilities.ChooseUnitPairAtEqualDistanceInRadiusV1),
                "Storm Spirit's ring is any ring, not the outermost one - which is what makes the " +
                "Tier III card a widening of the Tier II rather than a narrowing.");

            Assert.That(
                CapabilitiesOf(MisaCardContent.PowerShot),
                Does.Contain(SyntheticRosterCapabilities.TargetWasAtMaximumRangeV1));
        }

        /// <summary>
        /// RULING 28: the Ultimate's first clause is a standing modification of how many options a
        /// modal on her own card admits, and the number it declares is the two the card prints.
        ///
        /// <para>The modal itself is untouched. Watch How I Soar still declares one bullet minimum and
        /// one maximum, because that is what it prints; the widening is a fact about the board that the
        /// offer asks for, which is the whole reason a p.16 permanent ability can reach a card printed
        /// without it.</para>
        /// </summary>
        [Test]
        public void Ruling28_MakesTheUltimatesFirstClauseAChoiceCardinalityModification()
        {
            var rule = MisaCardContent.Definition(MisaCardContent.PowerOverwhelming).UltimateRule;

            Assert.That(rule.Modifications, Has.Count.EqualTo(1));
            Assert.That(rule.Modifications[0].Kind,
                Is.EqualTo(RuleModificationKind.ChoiceCardinality));
            Assert.That(rule.Modifications[0].Amount, Is.EqualTo(2),
                "\"two different options\" is the printed number.");

            var modal = MisaCardContent.Definition(MisaCardContent.WatchHowISoar)
                .ActionDefinitions.Single(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .EffectRoot;
            var choose = modal.Nodes.Single(node => node.Kind == EffectNodeKind.Choose);
            Assert.That(choose.MinimumSelections, Is.EqualTo(1));
            Assert.That(choose.MaximumSelections, Is.EqualTo(1),
                "The printed cardinality stays printed; the Ultimate raises the ceiling at offer time.");
            Assert.That(choose.ChildNodeIds, Has.Count.EqualTo(2),
                "Two bullets, which is why \"two different options\" means both.");
        }

        /// <summary>
        /// RULING 29, and the one thing about it worth asserting on the definition: the crossing is
        /// declared on the movement rather than granted to Misa, so the obstacle she just crossed stops
        /// her again on her very next Move.
        ///
        /// <para>Where the line may END is not declared anywhere here, and that absence is the ruling
        /// working: p.18's endpoint rule is offered no permission by anybody, so "as long as hero ends
        /// up on a playable space" is the movement query's own answer. That half is measured on a board
        /// in <see cref="MisaCapabilityTests"/> rather than asserted here.</para>
        /// </summary>
        [Test]
        public void Ruling29_DeclaresTheCrossingOnTheMovementAndGrantsHerNothing()
        {
            foreach (var blue in new[]
            {
                MisaCardContent.SuddenBreeze, MisaCardContent.GustOfWind,
                MisaCardContent.DashAndSlash, MisaCardContent.CrushingSquall,
                MisaCardContent.DeathFromAbove
            })
            {
                var body = SkillBody(blue);
                var move = body.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));
                var crossing = move.Inputs.Single(input => input.ParameterName ==
                    SyntheticDisplacementCapabilities.MovesThroughObstaclesParameterName);

                Assert.That(crossing.Literal.BooleanValue, Is.True, blue.Value);
                Assert.That(
                    body.Nodes.Any(node => node.Capability.Equals(
                        SyntheticModificationCapabilities.PermitMoveThroughObstaclesV1)),
                    Is.False,
                    blue.Value + " must not grant Misa a standing Move-Through permission: the " +
                    "sentence is about this movement, so the next one is stopped again.");
            }
        }

        /// <summary>
        /// RULING 30: the Ultimate's second clause fires on her own self-placements and nothing else.
        ///
        /// <para>The declaration is the trigger condition, and the two exclusions the ruling names are
        /// structural rather than declared. A placement by another hero's card cannot reach it because
        /// the fact behind the condition is derived from the definition the acting hero is resolving;
        /// a Swap cannot reach it because no Swap capability contributes to that derivation, which is
        /// asserted directly below.</para>
        /// </summary>
        [Test]
        public void Ruling30_HangsTheUltimatesSecondClauseOnHerOwnSelfPlacements()
        {
            var rule = MisaCardContent.Definition(MisaCardContent.PowerOverwhelming).UltimateRule;

            Assert.That(rule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerPlacesSelf));
            Assert.That(rule.TriggeredEffect.HasValue, Is.True);
            Assert.That(rule.TriggeredEffect.Value.DefinitionId,
                Is.EqualTo(MisaEffectContent.PowerOverwhelmingTriggerId));

            // Her three self-placing faces are exactly the three the derivation can see, and both Swap
            // cards are invisible to it. Measured through the query itself rather than asserted about
            // capability names, because the query is what the engine asks.
            var revision = MisaCardContent.ContentRevision;
            foreach (var placing in new[]
            {
                MisaCardContent.WatchHowISoar, MisaCardContent.SwoopIn
            })
            {
                var body = SkillBody(placing);
                Assert.That(
                    SelfPlacementQuery.Placed(body, AppliedOutcomesFor(body)),
                    Is.True,
                    placing.Value + " places Misa herself.");
            }

            foreach (var notPlacing in new[]
            {
                MisaCardContent.LivingTornado, MisaCardContent.StormSpirit,
                MisaCardContent.SuddenBreeze, MisaCardContent.CrushingSquall,
                MisaCardContent.Focus
            })
            {
                var body = SkillBody(notPlacing);
                Assert.That(
                    SelfPlacementQuery.Placed(body, AppliedOutcomesFor(body)),
                    Is.False,
                    notPlacing.Value + " is not a placement of herself - a Swap is neither movement " +
                    "nor placement (p.18), and a straight-line Move is movement.");
            }

            Assert.That(revision, Is.EqualTo(MisaCardContent.ContentRevision));
        }

        /// <summary>
        /// RULING 31: the free step is a <c>Next turn:</c> Active effect whose window is declared as a
        /// trigger checkpoint, and its frequency is once. The three cards differ only in how far.
        /// </summary>
        [Test]
        public void Ruling31_GivesTheThreeGreenCardsOneFreeStepBeforeOnePrimaryAction()
        {
            foreach (var pair in new[]
            {
                new { Card = MisaCardContent.Focus, Effect = MisaActiveEffectContent.FocusDefinition,
                    Distance = 1, Policy = EffectFailurePolicy.May },
                new { Card = MisaCardContent.Discipline,
                    Effect = MisaActiveEffectContent.DisciplineDefinition,
                    Distance = 2, Policy = EffectFailurePolicy.UpTo },
                new { Card = MisaCardContent.Mastery,
                    Effect = MisaActiveEffectContent.MasteryDefinition,
                    Distance = 3, Policy = EffectFailurePolicy.UpTo }
            })
            {
                Assert.That(pair.Effect.DurationKind,
                    Is.EqualTo(ActiveEffectDurationKind.NextTurn), pair.Card.Value);
                Assert.That(pair.Effect.CarriesAcrossRoundBoundary, Is.True,
                    pair.Card.Value + " is raised on one turn and applies on the next.");
                Assert.That(pair.Effect.TriggerCheckpoints,
                    Is.EqualTo(new[] { TimingCheckpoint.BeforePrimaryAction }), pair.Card.Value);
                Assert.That(pair.Effect.CancellationConditions,
                    Does.Contain(ActiveEffectCancellationCondition.CardsRetrieved),
                    pair.Card.Value + " dies at p.7's Retrieve Cards step, so one played on the " +
                    "round's fourth turn never takes effect.");

                var move = MisaEffectContent.TriggeredDefinitions(MisaCardContent.ContentRevision)
                    .Single(definition => definition.Id.Equals(
                        new EffectDefinitionId(pair.Effect.TriggeredEffect.Value.DefinitionId.Value)))
                    .Nodes
                    .Single(node => node.Capability.Equals(
                        SyntheticDisplacementCapabilities.DirectMoveV3));

                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "distance")
                        .Literal.IntegerValue,
                    Is.EqualTo(pair.Distance), pair.Card.Value);
                Assert.That(move.FailurePolicy, Is.EqualTo(pair.Policy),
                    pair.Card.Value + ": Focus prints an exact step and the other two print \"up to\".");
            }
        }

        /// <summary>
        /// RULING 32: the tense difference between Power Shot and Thunder Shot is two different
        /// moments, so the two faces name two different capabilities. Everything else about the two
        /// compositions is identical, which is asserted here so a future edit cannot quietly make them
        /// differ in something the cards do not.
        /// </summary>
        [Test]
        public void Ruling32_GivesTheRangedPairTwoConditionsAndOneShove()
        {
            var power = AttackBody(MisaCardContent.PowerShot);
            var thunder = AttackBody(MisaCardContent.ThunderShot);

            Assert.That(
                power.Nodes.Any(node => node.Capability.Equals(
                    SyntheticRosterCapabilities.TargetWasAtMaximumRangeV1)),
                Is.True, "Power Shot's \"was\" is judged at targeting time.");
            Assert.That(
                thunder.Nodes.Any(node => node.Capability.Equals(
                    SyntheticRosterCapabilities.TargetIsNotAdjacentV1)),
                Is.True, "Thunder Shot's \"is\" is judged at p.14's step 7.");

            foreach (var body in new[] { power, thunder })
            {
                var move = body.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticDisplacementCapabilities.DirectMoveV3));
                Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May),
                    "Both faces print \"you may move it\".");
                Assert.That(
                    move.Inputs.Single(input => input.ParameterName == "distance")
                        .Literal.IntegerValue,
                    Is.EqualTo(1));
                Assert.That(
                    (DirectMoveDestinationPolicy)move.Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName)
                        .Literal.IntegerValue,
                    Is.EqualTo(DirectMoveDestinationPolicy.AwayFromActor),
                    "\"to a space farther away from you\".");
            }

            Assert.That(
                MisaEffectContent.AttackTextTimingFor(MisaCardContent.PowerShot),
                Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(
                MisaEffectContent.AttackTextTimingFor(MisaCardContent.ThunderShot),
                Is.EqualTo(CardTextTiming.AfterTheAttack));
        }

        /// <summary>
        /// The three Red melee riders, which are one sentence at three numbers. What separates them is
        /// the Defense they grant and nothing else about the rule - and each is its own Active-effect
        /// definition, because a hero who plays two of them across a match is owed two rules.
        /// </summary>
        [Test]
        public void TheThreeMeleeRiders_GrantTwoThreeAndFiveDefenseForTheTurn()
        {
            foreach (var pair in new[]
            {
                new { Effect = MisaActiveEffectContent.ChallengeAcceptedDefinition, Defense = 2 },
                new { Effect = MisaActiveEffectContent.MatterOfHonorDefinition, Defense = 3 },
                new { Effect = MisaActiveEffectContent.WorthyOpponentDefinition, Defense = 5 }
            })
            {
                Assert.That(pair.Effect.DurationKind, Is.EqualTo(ActiveEffectDurationKind.ThisTurn));
                Assert.That(pair.Effect.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero));
                Assert.That(pair.Effect.Modifications, Has.Count.EqualTo(1));
                Assert.That(pair.Effect.Modifications[0].Kind,
                    Is.EqualTo(RuleModificationKind.ValueModifier));
                Assert.That(pair.Effect.Modifications[0].Channel,
                    Is.EqualTo(PrintedValueChannel.Defense));
                Assert.That(pair.Effect.Modifications[0].Amount, Is.EqualTo(pair.Defense));
                Assert.That(pair.Effect.Modifications[0].ValueApplicability,
                    Is.EqualTo(RuleValueApplicability.Always),
                    "The face prints \"Gain +N Defense\" with no clause after it, so it reaches " +
                    "every Defense value she composes for the rest of the turn.");
            }

            Assert.That(
                MisaActiveEffectContent.AllDefinitions
                    .Select(definition => definition.UniquenessKey).Distinct().Count(),
                Is.EqualTo(6),
                "Six declarations means six uniqueness keys: one card's occurrence must not suppress " +
                "another's.");
        }

        /// <summary>
        /// The Blue line's two riders, which differ in exactly the printed word - "an enemy unit" for
        /// three of them and "an enemy hero" for two - and in nothing else.
        /// </summary>
        [Test]
        public void TheBlueRiders_DifferOnlyInWhichUnitKindTheyName()
        {
            foreach (var pair in new[]
            {
                new { Card = MisaCardContent.SuddenBreeze, Distance = 3, Kind = RosterUnitKind.Unit },
                new { Card = MisaCardContent.GustOfWind, Distance = 4, Kind = RosterUnitKind.Unit },
                new { Card = MisaCardContent.DashAndSlash, Distance = 4, Kind = RosterUnitKind.Hero },
                new { Card = MisaCardContent.CrushingSquall, Distance = 5, Kind = RosterUnitKind.Unit },
                new { Card = MisaCardContent.DeathFromAbove, Distance = 5, Kind = RosterUnitKind.Hero }
            })
            {
                var body = SkillBody(pair.Card);
                var line = body.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseLineDestinationV1));
                var traversed = body.Nodes.Single(node =>
                    node.Capability.Equals(SyntheticRosterCapabilities.ChooseTraversedUnitV1));

                Assert.That(
                    line.Inputs.Single(input => input.ParameterName == "maximumDistance")
                        .Literal.IntegerValue,
                    Is.EqualTo(pair.Distance), pair.Card.Value);
                Assert.That(line.FailurePolicy, Is.EqualTo(EffectFailurePolicy.UpTo),
                    pair.Card.Value + " prints \"up to\", so a hero with no straight line available " +
                    "has moved zero of them legally rather than failed.");
                Assert.That(
                    line.Inputs.Any(input => input.ParameterName == "minimumDistance" &&
                        input.Literal.IntegerValue == 0),
                    Is.True,
                    pair.Card.Value + ": milestone 9.35 (W5) - \"up to\" makes 0 a real choice, so the " +
                    "floor is bound to nought and her own space is offered beside the line.");
                Assert.That(
                    (RosterUnitKind)traversed.Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticRosterCapabilities.UnitKindParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo(pair.Kind), pair.Card.Value);
                Assert.That(
                    (SourceTeamRelation)traversed.Inputs
                        .Single(input => input.ParameterName ==
                            SyntheticRosterCapabilities.RelationParameter)
                        .Literal.IntegerValue,
                    Is.EqualTo(SourceTeamRelation.OpposingSource),
                    pair.Card.Value + " names an ENEMY unit or hero.");

                // The traversal is re-derived from both ends of the line, never from a captured
                // record: the origin is the actor-space bridge's and the destination is the line's.
                var origin = traversed.Inputs.Single(input => input.ParameterName == "origin");
                var destination = traversed.Inputs
                    .Single(input => input.ParameterName == "destination");
                Assert.That(
                    body.Nodes.Single(node => node.Id.Equals(origin.SourceNodeId)).Capability,
                    Is.EqualTo(SyntheticPlacementCapabilities.ActorSpaceV1));
                Assert.That(destination.SourceNodeId, Is.EqualTo(line.Id));
            }
        }

        /// <summary>
        /// The generic minion subject does not override p.13's explicit friendly-Defeat prohibition.
        /// </summary>
        [Test]
        public void TheGoldCardsSecondBullet_RespectsTheFriendlyDefeatProhibition()
        {
            var body = SkillBody(MisaCardContent.WatchHowISoar);
            var select = body.Nodes.Single(node =>
                node.Capability.Equals(SyntheticRosterCapabilities.ChooseTargetV2));

            Assert.That(
                (SourceTeamRelation)select.Inputs
                    .Single(input => input.ParameterName ==
                        SyntheticRosterCapabilities.RelationParameter)
                    .Literal.IntegerValue,
                Is.EqualTo(SourceTeamRelation.OpposingSource));
            Assert.That(
                (RosterUnitKind)select.Inputs
                    .Single(input => input.ParameterName ==
                        SyntheticRosterCapabilities.UnitKindParameter)
                    .Literal.IntegerValue,
                Is.EqualTo(RosterUnitKind.Minion));
            Assert.That(
                body.Nodes.Any(node => node.Capability.Equals(
                    SyntheticConsequenceCapabilities.DefeatMinionV1)),
                Is.True);
        }

        /// <summary>
        /// Swoop In's "If you do", which composes to the order rather than to a condition: the Place is
        /// mandatory, so p.10's stop-and-skip already means the retrieve is reached exactly when the
        /// Place happened. A composed condition in front of it would be a second way of saying the same
        /// thing.
        /// </summary>
        [Test]
        public void SwoopIn_MakesThePlaceMandatoryAndTheRetrieveOptional()
        {
            var body = SkillBody(MisaCardContent.SwoopIn);
            var place = body.Nodes.Single(node => node.Capability.Equals(
                SyntheticPlacementCapabilities.PlaceInRadiusBesideTwoOrMoreEnemiesV1));
            var retrieve = body.Nodes.Single(node =>
                node.Capability.Equals(SyntheticRosterCapabilities.RetrieveCardV1));

            Assert.That(place.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory));
            Assert.That(retrieve.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May));
            Assert.That(
                body.Nodes.Any(node => node.Kind == EffectNodeKind.Conditional),
                Is.False,
                "\"If you do\" is p.10's stop-and-skip restated, not a composed condition.");
            Assert.That(
                (PlaceSpawnPointExclusion)place.Inputs
                    .Single(input => input.ParameterName ==
                        SyntheticPlacementCapabilities.SpawnPointExclusionParameterName)
                    .Literal.IntegerValue,
                Is.EqualTo(PlaceSpawnPointExclusion.None),
                "The face names no Spawn-point clause.");
        }

        private static IEnumerable<EffectCapabilityReference> CapabilitiesOf(CardDefinitionId cardId)
        {
            var definition = MisaCardContent.Definition(cardId);
            return definition.ActionDefinitions
                .Where(action => action.EffectRoot != null)
                .SelectMany(action => action.EffectRoot.Nodes)
                .Select(node => node.Capability)
                .Where(capability => capability.IsDefined)
                .ToArray();
        }

        private static EffectDefinition SkillBody(CardDefinitionId cardId) =>
            MisaCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.Kind == ActionKind.Skill)
                .EffectRoot;

        private static EffectDefinition AttackBody(CardDefinitionId cardId) =>
            MisaCardContent.Definition(cardId).ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.Kind == ActionKind.Attack)
                .EffectRoot;

        /// <summary>
        /// Every node of a body, reported as having applied. It is the input
        /// <see cref="SelfPlacementQuery"/> reads, and stating it here rather than running a match is
        /// what lets the ruling be measured before Misa is dealt: the query's question is structural -
        /// did an applied Place name the actor-space bridge as its subject - so the strongest test is
        /// the one where every node applied and the answer still separates her placing faces from her
        /// moving and swapping ones.
        /// </summary>
        private static IReadOnlyList<EffectNodeOutcome> AppliedOutcomesFor(EffectDefinition body) =>
            body.Nodes
                .Select(node => new EffectNodeOutcome(
                    node.Id,
                    EffectExecutionResultKind.Applied,
                    Array.Empty<EffectOutcomeFact>()))
                .ToArray();

        // -----------------------------------------------------------------------------------------
        // Milestone 8.35z38: the two halves of milestone 7.9's deferral gate, asserted against each
        // other so neither can move alone
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The fourteen contracts milestone 8.35z29 minted are now IN the build's registration list,
        /// and the reason is the same rule that kept them out of it: milestone 7.9's deferral gate
        /// refuses a registration no printed card a match deals can start, and milestone 8.35z38 deals
        /// her.
        ///
        /// <para>The phase-1 worker's finding F-8 is what this case exists to keep true. Registering
        /// eagerly does not produce one clean refusal - it produces two pinned-byte failures that read
        /// like a serialization defect, because the registered capability list is recorded in every
        /// snapshot. So the registration and the dealing are one change, and this case cannot claim the
        /// registration without <see cref="SheIsDealtByAConfiguredMatch"/> below proving there is a
        /// match that deals the cards.</para>
        ///
        /// <para>The gate's second half did real work again. It is asked of a <em>handler type</em>
        /// rather than of a capability id, so each new type owed a <c>SyntheticShowcaseHand</c>
        /// consumer before it could register - and milestone 8.35z38 added three showcase cards
        /// carrying four types between them. Her two Place declarations and her six Active-effect
        /// creations owed none, running on <c>PlaceHandler</c> and
        /// <c>ActiveEffectCreationHandler</c>, types this build has had since milestones 6.6 and
        /// 6.8.</para>
        /// </summary>
        [Test]
        public void TheNewContracts_AreRegisteredNowSheIsDealt()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                .Select(value => value.ToString())
                .ToArray();

            Assert.That(registered, Has.Member("synthetic-place-at-maximum-range@1"));
            Assert.That(registered, Has.Member(
                "synthetic-place-in-radius-beside-two-or-more-enemies@1"));
            Assert.That(registered, Has.Member("choose-line-destination@1"));
            Assert.That(registered, Has.Member("choose-traversed-unit@1"));
            Assert.That(registered, Has.Member("choose-unit-pair-at-maximum-radius@1"));
            Assert.That(registered, Has.Member("choose-unit-pair-at-equal-distance-in-radius@1"));
            Assert.That(registered, Has.Member("target-was-at-maximum-range@1"));
            Assert.That(registered, Has.Member("target-is-not-adjacent@1"));

            foreach (var handler in MisaActiveEffectContent.Handlers())
            {
                Assert.That(registered, Has.Member(handler.Reference.ToString()),
                    "Her Active-effect creations register with the rest of her contracts, in the " +
                    "same change, because they are reached by the same dealt cards.");
            }

            // And the other side of the move: none of them is still declared as deferred. Stated per
            // contract rather than over the whole list, and deliberately - the content wave leaves
            // four other undealt heroes' contracts in the roster list, so an exhaustive assertion here
            // would be an assertion about them.
            var deferred = SyntheticRosterCapabilities.DeferredHandlers()
                .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                .Select(handler => handler.Reference.ToString())
                .ToArray();

            Assert.That(deferred, Has.No.Member("synthetic-place-at-maximum-range@1"));
            Assert.That(deferred, Has.No.Member(
                "synthetic-place-in-radius-beside-two-or-more-enemies@1"));
            Assert.That(deferred, Has.No.Member("choose-line-destination@1"));
            Assert.That(deferred, Has.No.Member("choose-traversed-unit@1"));
            Assert.That(deferred, Has.No.Member("choose-unit-pair-at-maximum-radius@1"));
            Assert.That(deferred, Has.No.Member("choose-unit-pair-at-equal-distance-in-radius@1"));
            Assert.That(deferred, Has.No.Member("target-was-at-maximum-range@1"));
            Assert.That(deferred, Has.No.Member("target-is-not-adjacent@1"));
        }

        /// <summary>
        /// A configured match deals her eighteen cards, which is the half of milestone 7.9's gate the
        /// registration above rests on. Asserted through the configuration rather than through the
        /// content, because "she is dealt" is a statement about a match and not about a card file.
        /// </summary>
        [Test]
        public void SheIsDealtByAConfiguredMatch()
        {
            var configuration = MatchConfiguration.CreateStagedMisaForInternalTest(TeamSide.Red);

            var hers = configuration.CardInstances
                .Where(card => card.DefinitionReference.DefinitionId.Value
                    .StartsWith(MisaCardContent.ContentSetId + "-", StringComparison.Ordinal))
                .ToArray();

            Assert.That(hers, Has.Length.EqualTo(18),
                "p.6 deals her eighteen of her nineteen faces; Leaf on the Wind is p.20's Handicap " +
                "card and is in the box.");
            Assert.That(
                hers.Count(card => card.State.Location == CardLocation.Hand), Is.EqualTo(5),
                "p.6 step 11's starting hand: the three Tier I cards and the two basics.");
            Assert.That(
                hers.All(card => string.Equals(
                    card.DefinitionReference.ContentRevision,
                    MisaCardContent.ContentRevision,
                    StringComparison.Ordinal)),
                Is.True,
                "Every dealt instance carries the revision this milestone pinned her identity at.");
        }

        /// <summary>
        /// Both staged upgrade branches deal a legal p.15 position: the two basics plus one Tier card
        /// of each colour in hand, and the Ultimate in the deck where p.6's validator requires it.
        ///
        /// <para>It asserts both branches rather than one, because
        /// <see cref="MisaProgressedHands"/> records that every ruling of hers above Tier I lives on
        /// exactly one branch - so a fixture that only ever staged one would leave half her printed set
        /// unreachable by any journey and say nothing about it.</para>
        /// </summary>
        [Test]
        public void BothProgressedBranches_DealOneTierCardOfEachColour(
            [Values(MisaUpgradeBranch.FirstSibling, MisaUpgradeBranch.SecondSibling)]
            MisaUpgradeBranch branch)
        {
            foreach (var configuration in new[]
            {
                MatchConfiguration.CreateStagedMisaTierTwoForInternalTest(TeamSide.Red, branch),
                MatchConfiguration.CreateStagedMisaTierThreeForInternalTest(TeamSide.Red, branch)
            })
            {
                var hers = configuration.CardInstances
                    .Where(card => card.DefinitionReference.DefinitionId.Value
                        .StartsWith(MisaCardContent.ContentSetId + "-", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(hers, Has.Length.EqualTo(18));
                Assert.That(
                    hers.Count(card => card.State.Location == CardLocation.Hand), Is.EqualTo(5),
                    "The two basics plus one Tier card of each of her three colours.");
            }
        }
    }
}
