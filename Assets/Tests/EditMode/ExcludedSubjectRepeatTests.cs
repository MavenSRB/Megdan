using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.39d1. The card-text repeat that excludes the subject a SELECTION already took -
    /// Cutter's Broadside, "An enemy hero adjacent to another enemy unit and not adjacent to you
    /// discards a card, if able. <b>May repeat once on a different target.</b>"
    ///
    /// <para><b>Why it is a fifth <see cref="RepeatTargetBinding"/> member and not one of the three
    /// that already say "different".</b> <c>DifferentTarget</c>, <c>DifferentEnemyHero</c> and
    /// <c>DifferentEnemyMinion</c> are all about an ATTACK's own target - the first one's remarks say
    /// the exclusion "is carried by the repeated Attack instance rather than bound to a node id", and
    /// the store is <c>PendingAttackResolution.ExcludedTargets</c>. Broadside is a Skill, which has no
    /// attack target to exclude and nowhere to keep the exclusion.</para>
    ///
    /// <para><b>It is <c>SameTarget</c>'s capture with the opposite use.</b> That member records the
    /// piece the bound selection named and hands it to the repeated instance's own <c>Resume</c>;
    /// supplying a value can be capability-agnostic because it passes the value over. Removing one
    /// cannot - only a selection knows its candidates - so the exclusion is applied where every other
    /// generic narrowing already is, in <see cref="EffectSelectionEligibilityQuery"/>, which has an
    /// offer half and an answer half that read one method.</para>
    ///
    /// <para>These cases sit at the frame, as <see cref="BoundedRepeatActionTests"/> does: a board
    /// appears only where a case is about matching a piece to the coordinate it stands on.</para>
    /// </summary>
    public sealed class ExcludedSubjectRepeatTests
    {
        private static readonly HeroId Actor = new HeroId("excluded-subject-actor");
        private static readonly HeroId Controller = new HeroId("excluded-subject-controller");

        private static readonly HeroId Taken = new HeroId("excluded-subject-taken");
        private static readonly HeroId Spare = new HeroId("excluded-subject-spare");

        private static readonly EffectNodeId SelectionNodeId = new EffectNodeId("subject-selection");
        private static readonly EffectNodeId RepeatNodeId = new EffectNodeId("subject-repeat");
        private static readonly EffectNodeId HoldNodeId = new EffectNodeId("subject-hold");

        private static readonly MapId Map = new MapId("excluded-subject-map");
        private static readonly ZoneId Battle = new ZoneId("excluded-subject-zone");
        private static readonly HexCoordinate TakenAt = new HexCoordinate(1, 0);
        private static readonly HexCoordinate SpareAt = new HexCoordinate(2, 0);
        private static readonly HexCoordinate TakenSecondBodyAt = new HexCoordinate(3, 0);

        // -------------------------------------------------------------------------------------------
        // What the declaration is
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// It names its selection node, on the terms <c>SameTarget</c> already does - because the piece
        /// it excludes is that node's published subject - while the three Attack-target arms still
        /// refuse one, because what they exclude is the action's own state.
        /// </summary>
        [Test]
        public void TheExcludingRepeat_NamesItsSelectionAndTheAttackArmsStillNameNone()
        {
            Assert.That(
                () => RepeatNode(RepeatTargetBinding.DifferentSelectionSubject, bound: default),
                Throws.ArgumentException,
                "The piece it excludes is a selection's subject, so it must say which selection.");
            Assert.That(
                () => RepeatNode(RepeatTargetBinding.DifferentSelectionSubject, bound: RepeatNodeId),
                Throws.ArgumentException,
                "and it cannot bind itself, on the rule that already holds for the same-target arm.");
            Assert.That(
                () => RepeatNode(RepeatTargetBinding.DifferentTarget, bound: SelectionNodeId),
                Throws.ArgumentException,
                "while a different-TARGET repeat still names none: what it excludes is the Attack's.");

            Assert.That(
                RepeatNode(RepeatTargetBinding.DifferentSelectionSubject, bound: SelectionNodeId)
                    .BoundSelectionNodeId,
                Is.EqualTo(SelectionNodeId));
        }

        /// <summary>
        /// Both binding arms arm the capture, because both need the piece recorded at the moment the
        /// selection names it - and only one of them hands it back, which is what
        /// <see cref="RepeatedActionQuery.BoundBy"/> exists to keep apart.
        /// </summary>
        [Test]
        public void BothBindingArms_ArmTheCaptureAndOnlyOneSuppliesThePiece()
        {
            var excluding = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var supplying = DefinitionWith(RepeatTargetBinding.SameTarget);
            var selection = excluding.Nodes.Single(node => node.Id.Equals(SelectionNodeId));

            Assert.That(RepeatedActionQuery.BindsSelection(excluding, selection), Is.True);
            Assert.That(
                RepeatedActionQuery.BindsSelection(
                    supplying, supplying.Nodes.Single(node => node.Id.Equals(SelectionNodeId))),
                Is.True);

            Assert.That(
                RepeatedActionQuery.BoundBy(
                    excluding, selection, RepeatTargetBinding.DifferentSelectionSubject),
                Is.True);
            Assert.That(
                RepeatedActionQuery.BoundBy(excluding, selection, RepeatTargetBinding.SameTarget),
                Is.False,
                "and the supply path must not see it, or the repeated instance would be handed the " +
                "very subject the clause tells it to avoid.");
        }

        /// <summary>
        /// The supply path refuses it outright, which is the same claim read from the other end.
        /// </summary>
        [Test]
        public void TheSupplyPath_RefusesTheExcludingArm()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var frame = RepeatedFrame(definition, BoardObjectReference.ForHero(Taken));

            Assert.That(
                RepeatedActionQuery.TryReadBoundTarget(
                    definition,
                    definition.Nodes.Single(node => node.Id.Equals(SelectionNodeId)),
                    frame,
                    Components(),
                    out _),
                Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // The exclusion
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The offer, narrowed: the piece the asking instance took is gone and every other candidate
        /// stands, matched by identity because the selection publishes pieces.
        /// </summary>
        [Test]
        public void TheOffer_LosesTheSubjectTheAskingInstanceTook()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var frame = RepeatedFrame(definition, BoardObjectReference.ForHero(Taken));
            var offered = new[] { EffectValue.Hero(Taken), EffectValue.Hero(Spare) };

            Assert.That(
                Eligible(definition, frame, offered).Select(value => value.HeroId),
                Is.EqualTo(new[] { Spare }));
        }

        /// <summary>
        /// And matched by SPACE where the selection publishes coordinates instead, because comparing an
        /// identity against a coordinate would silently exclude nobody - which is the failure the one
        /// method exists to make impossible.
        /// </summary>
        [Test]
        public void TheOffer_MatchesByTheSpaceWhereItPublishesCoordinates()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var frame = RepeatedFrame(definition, BoardObjectReference.ForHero(Taken));
            var offered = new[] { EffectValue.Space(TakenAt), EffectValue.Space(SpareAt) };

            Assert.That(
                Eligible(definition, frame, offered, Components()).Select(value => value.Coordinate),
                Is.EqualTo(new[] { SpareAt }));
        }

        /// <summary>
        /// Milestone 9.21c (RZ-Q2). A hero standing on several spaces is ONE unit for "a different
        /// target": where the selection offers spaces, every space he stands on is taken out with the
        /// one the asking instance took him on - his second body is not a different target.
        /// </summary>
        [Test]
        public void TheOffer_LosesEveryBodyOfAManyBodiedSubject_WhereItPublishesCoordinates()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            // Captured at hero grain, as RepeatedActionQuery captures a many-bodied hero's space.
            var frame = RepeatedFrame(definition, BoardObjectReference.ForHero(Taken));
            var offered = new[]
            {
                EffectValue.Space(TakenAt), EffectValue.Space(TakenSecondBodyAt), EffectValue.Space(SpareAt)
            };

            Assert.That(
                Eligible(definition, frame, offered, Components(takenHasTwoBodies: true))
                    .Select(value => value.Coordinate),
                Is.EqualTo(new[] { SpareAt }));
        }

        /// <summary>
        /// The answer half reads the same method, so a piece left out of the offer cannot be submitted
        /// to it. That is what keeps "on a different target" a rule rather than a presentation.
        /// </summary>
        [Test]
        public void TheAnswer_RefusesTheExcludedSubjectAndAcceptsAnother()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var frame = RepeatedFrame(definition, BoardObjectReference.ForHero(Taken));

            Assert.That(StillEligible(definition, frame, EffectValue.Hero(Taken)), Is.False);
            Assert.That(StillEligible(definition, frame, EffectValue.Hero(Spare)), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.39j. What ARMS the capture, which is the half nothing written at 8.39d1 drove
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>A selection that publishes only a PIECE still named one.</b> Milestone 8.39j's Play Mode
        /// run is what found this: Broadside's subject node declares one output - the chosen hero -
        /// because nothing downstream of it consumes a space, so the capture found no coordinate fact,
        /// reported nothing captured, and the exclusion below silently narrowed nothing. The live
        /// consequence was the printed rule not happening: the repeated instance offered the very hero
        /// the first instance had taken, and one player lost two cards to "a different target".
        ///
        /// <para>Every case at milestone 8.39d1 handed the frame a bound piece directly, which is why
        /// the whole exclusion could be green while nothing ever armed it.</para>
        /// </summary>
        [Test]
        public void TheCapture_ArmsFromTheHeroASelectionPublished_WhenItPublishesNoSpace()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var selection = definition.Nodes.Single(node => node.Id.Equals(SelectionNodeId));
            var applied = EffectExecutionResult.Applied(new[]
            {
                new EffectOutcomeFact("chosenHero", EffectValue.Hero(Taken))
            });

            Assert.That(
                RepeatedActionQuery.TryCaptureBoundTarget(
                    definition, selection, applied, Components(), out var captured),
                Is.True,
                "A hero-valued selection names a piece, and a capture that reads only coordinates " +
                "records nothing for it - which is the whole of what Broadside got wrong.");
            Assert.That(captured.Kind, Is.EqualTo(BoardObjectKind.Hero));
            Assert.That(captured.HeroId, Is.EqualTo(Taken));
        }

        /// <summary>The same for a minion-valued selection, which no content names yet and might.</summary>
        [Test]
        public void TheCapture_ArmsFromAMinionASelectionPublished()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var selection = definition.Nodes.Single(node => node.Id.Equals(SelectionNodeId));
            var minion = new MinionId("excluded-subject-minion");
            var applied = EffectExecutionResult.Applied(new[]
            {
                new EffectOutcomeFact("chosenMinion", EffectValue.Minion(minion))
            });

            Assert.That(
                RepeatedActionQuery.TryCaptureBoundTarget(
                    definition, selection, applied, Components(), out var captured),
                Is.True);
            Assert.That(captured.Kind, Is.EqualTo(BoardObjectKind.Minion));
            Assert.That(captured.MinionId, Is.EqualTo(minion));
        }

        /// <summary>
        /// <b>And the SPACE still wins where a selection publishes one</b>, which is what keeps every
        /// bound selection written before milestone 8.39j capturing byte-identically: Wasp's Orbit, its
        /// showcase twin and Dodger's rebinding pair all publish a coordinate, so none of them reaches
        /// the fallback above at all.
        /// </summary>
        [Test]
        public void TheCapture_StillPrefersTheSpaceWhereASelectionPublishesOne()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var selection = definition.Nodes.Single(node => node.Id.Equals(SelectionNodeId));
            var applied = EffectExecutionResult.Applied(new[]
            {
                new EffectOutcomeFact("chosen", EffectValue.Space(SpareAt)),
                new EffectOutcomeFact("chosenHero", EffectValue.Hero(Taken))
            });

            Assert.That(
                RepeatedActionQuery.TryCaptureBoundTarget(
                    definition, selection, applied, Components(), out var captured),
                Is.True);
            Assert.That(captured.HeroId, Is.EqualTo(Spare),
                "The coordinate names whoever is standing there, which is the rule every existing " +
                "bound selection has always been read by.");
        }

        /// <summary>
        /// An excluding repeat whose asking instance captured nothing narrows nothing, and advances
        /// rather than refusing: a repeat of an unnarrowed set is exactly p.10's default, so there is
        /// nothing here for the card to be saying that it does not say.
        /// </summary>
        [Test]
        public void AnExcludingRepeatWithNoCapturedPiece_NarrowsNothingAndStillAdvances()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var frame = RepeatedFrame(definition, boundTarget: default);
            var offered = new[] { EffectValue.Hero(Taken), EffectValue.Hero(Spare) };

            Assert.That(
                Eligible(definition, frame, offered).Select(value => value.HeroId),
                Is.EqualTo(new[] { Taken, Spare }));
        }

        // -------------------------------------------------------------------------------------------
        // Nothing written before this milestone moves
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A selection this definition's repeat does not bind is not narrowed at all, which is what
        /// every selection in the build gets: the exclusion is bound to a node, not to a frame.
        /// </summary>
        [Test]
        public void ASelectionTheRepeatDoesNotBind_IsNotNarrowed()
        {
            var definition = DefinitionWith(RepeatTargetBinding.DifferentSelectionSubject);
            var frame = RepeatedFrame(definition, BoardObjectReference.ForHero(Taken));
            var offered = new[] { EffectValue.Hero(Taken), EffectValue.Hero(Spare) };

            Assert.That(
                EffectSelectionEligibilityQuery.EligibleCandidates(
                        definition.Nodes.Single(node => node.Id.Equals(RepeatNodeId)),
                        offered,
                        frame,
                        Array.Empty<EffectNodeOutcome>(),
                        Context(definition))
                    .Select(value => value.HeroId),
                Is.EqualTo(new[] { Taken, Spare }));
        }

        /// <summary>
        /// A <c>SameTarget</c> repeat's request is byte-identical to what Wasp and Dodger have always
        /// seen - it carries the piece and reports <see cref="RepeatTargetBinding.Unbound"/>, because
        /// <c>RepeatRequest.Binding</c> exists so a dispatcher can honour "on a different target"
        /// without reading nodes, and putting a value where those two have always seen none would be a
        /// behaviour change they never asked for.
        /// </summary>
        [Test]
        public void TheSameTargetRequest_IsUnchanged()
        {
            var same = Requested(RepeatTargetBinding.SameTarget, BoardObjectReference.ForHero(Taken));
            Assert.That(same.IsRequested, Is.True);
            Assert.That(same.Continuation.HasBoundTarget, Is.True);
            Assert.That(same.Binding, Is.EqualTo(RepeatTargetBinding.Unbound));

            var excluding = Requested(
                RepeatTargetBinding.DifferentSelectionSubject, BoardObjectReference.ForHero(Taken));
            Assert.That(excluding.Continuation.HasBoundTarget, Is.True,
                "The excluding arm carries the identical piece by the identical route.");
            Assert.That(
                excluding.Binding, Is.EqualTo(RepeatTargetBinding.DifferentSelectionSubject),
                "and names itself, because a dispatcher that could not tell the two apart would " +
                "supply what this one excludes.");
        }

        /// <summary>
        /// And a <c>SameTarget</c> repeat whose selection named nobody still refuses, which is milestone
        /// 8.6's conservative answer and is the one place the two arms deliberately differ.
        /// </summary>
        [Test]
        public void ASameTargetRequestWithNoPiece_StillRefuses()
        {
            Assert.That(
                Requested(RepeatTargetBinding.SameTarget, boundTarget: default).IsRequested,
                Is.False);
            Assert.That(
                Requested(RepeatTargetBinding.DifferentSelectionSubject, boundTarget: default)
                    .IsRequested,
                Is.True,
                "while an excluding repeat with nothing to exclude is an ordinary p.10 repeat.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<EffectValue> Eligible(
            EffectDefinition definition,
            PendingResolution frame,
            IReadOnlyList<EffectValue> offered,
            MatchComponents components = null) =>
            EffectSelectionEligibilityQuery.EligibleCandidates(
                definition.Nodes.Single(node => node.Id.Equals(SelectionNodeId)),
                offered,
                frame,
                Array.Empty<EffectNodeOutcome>(),
                Context(definition, components));

        private static bool StillEligible(
            EffectDefinition definition, PendingResolution frame, EffectValue chosen) =>
            EffectSelectionEligibilityQuery.AreStillEligible(
                definition.Nodes.Single(node => node.Id.Equals(SelectionNodeId)),
                new[] { chosen },
                frame,
                Array.Empty<EffectNodeOutcome>(),
                Context(definition));

        private static RepeatRequest Requested(
            RepeatTargetBinding binding, BoardObjectReference boundTarget)
        {
            var definition = DefinitionWith(binding);
            var frame = PrintedFrame(definition, boundTarget);
            var outcomes = new[]
            {
                new EffectNodeOutcome(RepeatNodeId, EffectExecutionResultKind.Applied)
            };

            return RepeatedActionQuery.Requested(definition, frame, outcomes);
        }

        /// <summary>
        /// A definition shaped like Broadside's: a selection, and a repeat that binds it.
        /// </summary>
        private static EffectDefinition DefinitionWith(RepeatTargetBinding binding) =>
            new EffectDefinition(
                new EffectDefinitionId("excluded-subject-definition"),
                "rev-1",
                new EffectNodeId("subject-root"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("subject-root"), EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { HoldNodeId, SelectionNodeId, RepeatNodeId }),

                    // A pause, so both instances hand these cases a frame to read. A repeated
                    // instance skips its own repeat node in silence - p.10's bar - so a definition
                    // that only paused there would complete before a case could look at it.
                    new EffectNodeDefinition(
                        HoldNodeId, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1,
                        childNodeIds: new[]
                        {
                            new EffectNodeId("subject-hold-a"), new EffectNodeId("subject-hold-b")
                        }),
                    new EffectNodeDefinition(
                        new EffectNodeId("subject-hold-a"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.NoteActorV1),
                    new EffectNodeDefinition(
                        new EffectNodeId("subject-hold-b"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.NoteActorV1),
                    // An ordinary applied leaf standing in for the selection. These cases are about
                    // what the exclusion does to a candidate list, and the narrowing is applied
                    // before any consumer is read - so a node that pauses nothing keeps the fixture
                    // at the frame, which is where BoundedRepeatActionTests works too.
                    new EffectNodeDefinition(
                        SelectionNodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.NoteActorV1),
                    RepeatNode(binding, SelectionNodeId)
                });

        private static EffectNodeDefinition RepeatNode(
            RepeatTargetBinding binding, EffectNodeId bound) =>
            new EffectNodeDefinition(
                RepeatNodeId, EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                chooser: EffectChooserRole.Actor,
                repeatTargetBinding: binding,
                boundSelectionNodeId: bound);

        /// <summary>The printed instance, with the piece its selection named already captured.</summary>
        private static PendingResolution PrintedFrame(
            EffectDefinition definition, BoardObjectReference boundTarget) =>
            Frame(definition, ActionInvocationContext.PrintedPrimary,
                boundTarget.IsDefined
                    ? RepeatContinuation.FirstInstance.Binding(boundTarget)
                    : RepeatContinuation.FirstInstance);

        /// <summary>The repeated instance, carrying whatever the printed one captured.</summary>
        private static PendingResolution RepeatedFrame(
            EffectDefinition definition, BoardObjectReference boundTarget) =>
            Frame(definition, ActionInvocationContext.Repeated,
                boundTarget.IsDefined
                    ? RepeatContinuation.NextBoundTo(1, boundTarget)
                    : RepeatContinuation.Next(1));

        private static PendingResolution Frame(
            EffectDefinition definition,
            ActionInvocationContext invocationContext,
            RepeatContinuation repeat) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("excluded-subject-fixture"),
                new CardInstanceId("excluded-subject-card"),
                default,
                new ActionDefinitionId("excluded-subject-action"),
                Actor,
                Controller,
                invocationContext,
                Context(definition),
                repeat).Frame;

        private static EffectResolutionContext Context(
            EffectDefinition definition, MatchComponents components = null) =>
            new EffectResolutionContext(
                definition, SyntheticEffectCapabilities.CreateCatalog(), null, null, 1, 1,
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill,
                components: components);

        /// <summary>
        /// A two-space board carrying the two heroes these cases name, for the one case that is about
        /// matching a piece to the coordinate it stands on.
        /// </summary>
        private static MatchComponents Components(bool takenHasTwoBodies = false)
        {
            var map = new BoardMapDefinition(
                Map, 1, "Excluded Subject Map",
                new[]
                {
                    new BoardMapSpace(new SpaceId("s0"), new HexCoordinate(0, 0),
                        BoardMapSpaceKind.Playable, Battle),
                    new BoardMapSpace(new SpaceId("s1"), TakenAt, BoardMapSpaceKind.Playable, Battle),
                    new BoardMapSpace(new SpaceId("s2"), SpareAt, BoardMapSpaceKind.Playable, Battle),
                    new BoardMapSpace(new SpaceId("s3"), TakenSecondBodyAt, BoardMapSpaceKind.Playable, Battle)
                },
                new[] { new BoardMapZone(Battle, "Battle", "lane", null, false, true) },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());

            var board = new GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Taken, TakenAt),
                    new HeroOccupancy(Spare, SpareAt)
                }.Concat(takenHasTwoBodies
                    ? new[] { new HeroOccupancy(Taken, 2, TakenSecondBodyAt) }
                    : Array.Empty<HeroOccupancy>()),
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>(),
                takenHasTwoBodies ? new[] { new HeroBodySupply(Taken, 4) } : Array.Empty<HeroBodySupply>());

            return new MatchComponents(
                SyntheticComponentDefinitions.CreateCatalog(),
                board,
                new MarkerAttachmentRegister(),
                board,
                board,
                board,
                board);
        }
    }
}
